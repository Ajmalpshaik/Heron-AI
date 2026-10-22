// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `rebarSets`, `bars`, `systems`, `hostsWithRebar`, `report`, `barList`,
// `notHosts`, `withoutRebar` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE READ-BACK EVERY REBAR WRITE IS CHECKED AGAINST. Host by host: whether
// Revit takes it as a rebar host, its cover on each face by Revit's own label,
// every rebar set and every area, path and fabric system in it. Every value is
// read off the model - nothing is computed from what a placing tool was asked.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);
Func<RebarBarType, double> nominalOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarNominalDiameter;
#endif
};
Func<Element, BuiltInParameter, Parameter> param = (e, bip) =>
{
    try { return e.get_Parameter(bip); } catch (Exception) { return null; }
};
Func<ElementId, string> nameOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var found = doc.GetElement(id);
    return found == null ? "none" : "'" + found.Name + "'";
};
Func<Element, string> label = e => (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

var rebarSets = 0;
var bars = 0;
var systems = 0;
var hostsWithRebar = 0;
var withoutRebar = 0;
var notHosts = new List<string>();
var findings = new List<string>();
var hostLines = new List<string>();

// The bar list: bar type name -> bars, total length in feet.
var listCount = new Dictionary<string, int>(StringComparer.Ordinal);
var listLength = new Dictionary<string, double>(StringComparer.Ordinal);
Action<string, int, double> tally = (typeName, count, length) =>
{
    if (!listCount.ContainsKey(typeName)) { listCount[typeName] = 0; listLength[typeName] = 0; }
    listCount[typeName] += count;
    listLength[typeName] += length;
};

var coverFaces = new[]
{
    BuiltInParameter.CLEAR_COVER_TOP, BuiltInParameter.CLEAR_COVER_BOTTOM, BuiltInParameter.CLEAR_COVER_OTHER,
    BuiltInParameter.CLEAR_COVER_EXTERIOR, BuiltInParameter.CLEAR_COVER_INTERIOR
};

// Each area reinforcement layer: on/off, bar type, spacing, number of lines.
// Floors use TOP/BOTTOM, walls FRONT/BACK; the label printed is Revit's own.
var layers = new[]
{
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_TOP_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_TOP_DIR_2 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BOTTOM_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BOTTOM_DIR_2 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_FRONT_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_FRONT_DIR_2 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BACK_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BACK_DIR_2 },
};

// The bars Revit made inside an area or path system, counted into the bar list.
Action<IList<ElementId>> tallySystemBars = ids =>
{
    foreach (var id in ids)
    {
        var inSystem = doc.GetElement(id) as RebarInSystem;
        if (inSystem == null) continue;
        var type = doc.GetElement(inSystem.GetTypeId()) as RebarBarType;
        var lengthParameter = param(inSystem, BuiltInParameter.REBAR_ELEM_TOTAL_LENGTH);
        tally(type == null ? "(no bar type)" : type.Name, inSystem.Quantity,
              lengthParameter == null ? 0 : lengthParameter.AsDouble());
    }
};

foreach (var host in elements)
{
    if (host == null) continue;
    if (!RebarHostData.IsValidHost(host))
    {
        notHosts.Add(label(host) + " is not a rebar host - Revit hosts rebar only in concrete structural "
            + "columns, framing, foundations, floors and walls, and in a structural part.");
        continue;
    }
    var data = RebarHostData.GetRebarHostData(host);
    var parts = new List<string>();

    var coverParts = new List<string>();
    foreach (var bip in coverFaces)
    {
        var p = param(host, bip);
        if (p == null || p.StorageType != StorageType.ElementId) continue;
        var cover = doc.GetElement(p.AsElementId()) as RebarCoverType;
        coverParts.Add(p.Definition.Name + " " + (cover == null ? "none" : "'" + cover.Name + "' " + mm(cover.CoverDistance) + " mm"));
    }
    parts.Add("cover: " + (coverParts.Count == 0 ? "no cover parameters" : string.Join(", ", coverParts)));

    var found = 0;
    foreach (var bar in data.GetRebarsInHost())
    {
        found++;
        rebarSets++;
        var type = doc.GetElement(bar.GetTypeId()) as RebarBarType;
        RebarShape shape = null;
        try { shape = doc.GetElement(bar.GetShapeId()) as RebarShape; } catch (Exception) { }
        var quantity = bar.Quantity;
        bars += quantity;
        var text = "rebar id " + bar.Id + ": " + quantity + " x " + (type == null ? "(no bar type)" : "'" + type.Name + "' " + mm(nominalOf(type)) + " mm")
            + ", " + (shape == null ? "free form" : (shape.RebarStyle == RebarStyle.StirrupTie ? "stirrup/tie" : "standard") + " shape '" + shape.Name + "'")
            + ", layout " + bar.LayoutRule;
        if (bar.LayoutRule != RebarLayoutRule.Single)
        {
            var spacing = param(bar, BuiltInParameter.REBAR_ELEM_BAR_SPACING);
            if (spacing != null && spacing.StorageType == StorageType.Double && spacing.AsDouble() > 0)
                text += ", spacing " + mm(spacing.AsDouble()) + " mm";
        }
        string startHook = "none", endHook = "none";
        try { startHook = nameOf(bar.GetHookTypeId(0)); endHook = nameOf(bar.GetHookTypeId(1)); } catch (Exception) { }
        text += ", hooks " + startHook + " / " + endHook + ", total length " + mm(bar.TotalLength) + " mm";
        parts.Add(text);
        tally(type == null ? "(no bar type)" : type.Name, quantity, bar.TotalLength);
    }

    foreach (var area in data.GetAreaReinforcementsInHost())
    {
        found++;
        systems++;
        var on = new List<string>();
        foreach (var layer in layers)
        {
            var active = param(area, layer[0]);
            if (active == null || active.StorageType != StorageType.Integer || active.AsInteger() != 1) continue;
            var barType = param(area, layer[1]);
            var spacing = param(area, layer[2]);
            var lines = param(area, layer[3]);
            on.Add(active.Definition.Name + " " + (barType == null ? "?" : nameOf(barType.AsElementId()))
                + (spacing == null ? "" : " @ " + mm(spacing.AsDouble()) + " mm")
                + (lines == null || lines.StorageType != StorageType.Integer ? "" : " (" + lines.AsInteger() + " lines)"));
        }
        parts.Add("area reinforcement id " + area.Id + ": " + (on.Count == 0 ? "no layer on" : string.Join("; ", on)));
        tallySystemBars(area.GetRebarInSystemIds());
    }

    foreach (var path in data.GetPathReinforcementsInHost())
    {
        found++;
        systems++;
        var barType = param(path, BuiltInParameter.PATH_REIN_TYPE_1);
        var spacing = param(path, BuiltInParameter.PATH_REIN_SPACING);
        var count = param(path, BuiltInParameter.PATH_REIN_NUMBER_OF_BARS);
        var length = param(path, BuiltInParameter.PATH_REIN_LENGTH_1);
        var face = param(path, BuiltInParameter.PATH_REIN_FACE_SLAB) ?? param(path, BuiltInParameter.PATH_REIN_FACE_WALL);
        parts.Add("path reinforcement id " + path.Id + ": " + (barType == null ? "?" : nameOf(barType.AsElementId()))
            + (spacing == null ? "" : " @ " + mm(spacing.AsDouble()) + " mm")
            + (count == null ? "" : ", " + count.AsInteger() + " bars")
            + (length == null ? "" : ", bar length " + mm(length.AsDouble()) + " mm")
            + (face == null ? "" : ", face " + face.AsValueString()));
        tallySystemBars(path.GetRebarInSystemIds());
    }

    foreach (var fabricArea in data.GetFabricAreasInHost())
    {
        found++;
        systems++;
        parts.Add("fabric area id " + fabricArea.Id + ": sheet type " + nameOf(fabricArea.FabricSheetTypeId)
            + ", location " + (fabricArea.FabricLocation == FabricLocation.TopOrExternal ? "top/exterior" : "bottom/interior")
            + ", " + fabricArea.GetFabricSheetElementIds().Count + " sheet(s)");
    }
    var looseSheets = data.GetFabricSheetsInHost();
    if (looseSheets.Count > 0)
    {
        found += looseSheets.Count;
        systems += looseSheets.Count;
        var byType = looseSheets.GroupBy(s => nameOf(s.GetTypeId())).Select(g => g.Count() + " x " + g.Key);
        parts.Add("fabric sheets: " + string.Join(", ", byType));
    }

    if (found == 0) { withoutRebar++; parts.Add("no reinforcement"); }
    else hostsWithRebar++;
    hostLines.Add(label(host) + " - " + string.Join(" | ", parts));
}

var report = string.Join(" || ", hostLines);
var barList = listCount.Count == 0 ? "no bars" : string.Join("; ", listCount.Keys.OrderBy(k => k, StringComparer.Ordinal)
    .Select(k => "'" + k + "' " + listCount[k] + " bars, " + Math.Round(listLength[k] * 0.3048, 2).ToString("0.##", invariant) + " m"));

findings.Add(hostsWithRebar + " host(s) with reinforcement, " + withoutRebar + " without, " + notHosts.Count
    + " not a rebar host: " + rebarSets + " rebar set(s) holding " + bars + " bar(s), and " + systems
    + " area/path/fabric element(s).");
findings.AddRange(notHosts);
