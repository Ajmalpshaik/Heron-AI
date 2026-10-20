// NOT STANDALONE. Assumes `doc`, `elements`, `majorDirection`, `topMajor`,
// `topMinor`, `bottomMajor`, `bottomMinor`, `hook` and
// `areaReinforcementType` are in scope; leaves `placed`, `hostsReinforced`,
// `skipped`, `readBackWrong`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// AREA REINFORCEMENT OVER THE WHOLE HOST: a slab, a foundation slab or a wall,
// to its own boundary, inside its own cover. Four layers - top/exterior and
// bottom/interior, each in the major and minor direction - each set by Revit's
// own parameter id and read back with Revit's own label for it. "Top" on a wall
// is its EXTERIOR face and "bottom" its INTERIOR face, as Revit names them
// ("Exterior Major Direction" is REBAR_SYSTEM_ACTIVE_FRONT_DIR_1).

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);
Func<RebarBarType, double> nominalOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarNominalDiameter;
#endif
};

var placed = new List<ElementId>();
var hostsReinforced = new List<ElementId>();
var skipped = new List<string>();
var readBackWrong = new List<string>();
var findings = new List<string>();
var refused = "";
var lines = new List<string>();

var barTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
var hookTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>().ToList();
var areaTypes = new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcementType)).Cast<AreaReinforcementType>().ToList();
Func<string> barList = () => barTypes.Count == 0 ? "none" : string.Join(", ", barTypes.OrderBy(t => nominalOf(t))
    .Select(t => "'" + t.Name + "' " + mm(nominalOf(t)) + " mm"));

// A bar type by its exact name, or a diameter in millimetres matched to the ONE
// type of that nominal diameter. Null with `refused` set.
Func<string, string, RebarBarType> barTypeFrom = (text, role) =>
{
    var asked = (text ?? "").Trim();
    var named = barTypes.FirstOrDefault(t => string.Equals(t.Name, asked, StringComparison.Ordinal));
    if (named != null) return named;
    var number = asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase) ? asked.Substring(0, asked.Length - 2).Trim() : asked;
    double diameter;
    if (!double.TryParse(number, styleFloat, invariant, out diameter) || diameter <= 0)
    {
        refused = "No Rebar Bar Type is named '" + asked + "' (the " + role + " asked). Loaded: " + barList()
            + ". Give one of these names, or the diameter in millimetres. If '" + asked + "' is a bar mark from the "
            + "drawings, say which type or diameter it means. Nothing was placed";
        return null;
    }
    var matching = barTypes.Where(t => Math.Abs(nominalOf(t) * 304.8 - diameter) < 0.05).ToList();
    if (matching.Count == 1) return matching[0];
    refused = matching.Count > 1
        ? matching.Count + " bar types are " + diameter.ToString("0.#", invariant) + " mm: "
            + string.Join(", ", matching.Select(t => "'" + t.Name + "'")) + ". Say which, by its name. Nothing was placed"
        : "No Rebar Bar Type of " + diameter.ToString("0.#", invariant) + " mm is loaded. Loaded: " + barList()
            + ". CREATE_REBAR_BAR_TYPE makes one from the spec's diameter and bend diameters. Nothing was placed";
    return null;
};
// "<bar type> @ <spacing>" or "none". Spacing in feet.
Func<string, string, Tuple<RebarBarType, double>> typeAndSpacing = (text, role) =>
{
    var asked = (text ?? "").Trim();
    if (asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return Tuple.Create((RebarBarType)null, 0.0);
    var at = asked.LastIndexOf('@');
    double spacing;
    var spacingText = at < 0 ? "" : asked.Substring(at + 1).Trim();
    if (spacingText.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) spacingText = spacingText.Substring(0, spacingText.Length - 2).Trim();
    if (at <= 0 || !double.TryParse(spacingText, styleFloat, invariant, out spacing) || spacing <= 0)
    {
        refused = "'" + asked + "' is not a bar type and spacing. Say it as <bar type> @ <spacing in mm> - 12mm @ 200 "
            + "- or none. Nothing was placed";
        return null;
    }
    var type = barTypeFrom(asked.Substring(0, at), role);
    return type == null ? null : Tuple.Create(type, spacing / 304.8);
};

// ---- the request, read once ------------------------------------------------------------
var layerAsks = new[]
{
    Tuple.Create("top major", topMajor), Tuple.Create("top minor", topMinor),
    Tuple.Create("bottom major", bottomMajor), Tuple.Create("bottom minor", bottomMinor),
};
var layerValues = new List<Tuple<RebarBarType, double>>();
foreach (var ask in layerAsks)
{
    if (refused.Length > 0) break;
    layerValues.Add(typeAndSpacing(ask.Item2, ask.Item1 + " layer"));
}

RebarHookType hookType = null;
var hookText = (hook ?? "").Trim();
if (refused.Length == 0 && hookText.Length > 0 && !hookText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    hookType = hookTypes.FirstOrDefault(h => string.Equals(h.Name, hookText, StringComparison.Ordinal));
    if (hookType == null || hookType.Style != RebarStyle.Standard)
        refused = (hookType == null ? "No Rebar Hook Type is named '" + hookText + "'" : "'" + hookText + "' is a hook for stirrups and ties")
            + ". Hooks for these bars: " + string.Join(", ", hookTypes.Where(h => h.Style == RebarStyle.Standard).Select(h => "'" + h.Name + "'"))
            + " - or none. Nothing was placed";
}
if (refused.Length == 0 && layerValues.All(v => v.Item1 == null))
    refused = "Every layer was asked as none, so there is nothing to place. Nothing was placed";

AreaReinforcementType areaType = null;
var areaTypeMade = false;
var areaTypeText = (areaReinforcementType ?? "").Trim();
if (refused.Length == 0)
{
    if (areaTypeText.Length == 0 || areaTypeText.Equals("only", StringComparison.OrdinalIgnoreCase))
    {
        if (areaTypes.Count == 1) areaType = areaTypes[0];
        else if (areaTypes.Count > 1)
            refused = areaTypes.Count + " Area Reinforcement Types are loaded: " + string.Join(", ", areaTypes.Select(t => "'" + t.Name + "'"))
                + ". Say which, by its name. Nothing was placed";
    }
    else
    {
        areaType = areaTypes.FirstOrDefault(t => string.Equals(t.Name, areaTypeText, StringComparison.Ordinal));
        if (areaType == null)
            refused = "No Area Reinforcement Type is named '" + areaTypeText + "'. Loaded: "
                + (areaTypes.Count == 0 ? "none" : string.Join(", ", areaTypes.Select(t => "'" + t.Name + "'"))) + ". Nothing was placed";
    }
}
var direction = (majorDirection ?? "").Trim().ToLowerInvariant();

// The four layers by Revit's id: floors TOP/BOTTOM, walls FRONT (exterior) /
// BACK (interior). Active, bar type, spacing, hook, number of lines - and the
// GENERIC id ("Top/Exterior Major Direction") a host may carry instead.
var floorIds = new[]
{
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_TOP_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_TOP_DIR_2 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BOTTOM_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BOTTOM_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BOTTOM_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BOTTOM_DIR_2 },
};
var wallIds = new[]
{
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_FRONT_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_FRONT_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_FRONT_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_FRONT_DIR_2 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_SPACING_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BACK_DIR_1, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BACK_DIR_1 },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_SPACING_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BACK_DIR_2, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BACK_DIR_2 },
};
var genericIds = new[]
{
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_1_GENERIC, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_1_GENERIC, BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_1_GENERIC, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_TOP_DIR_1_GENERIC },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_2_GENERIC, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_2_GENERIC, BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_2_GENERIC, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_TOP_DIR_2_GENERIC },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_1_GENERIC, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_1_GENERIC, BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_1_GENERIC, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BOTTOM_DIR_1_GENERIC },
    new[] { BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_2_GENERIC, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_2_GENERIC, BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_2_GENERIC, BuiltInParameter.REBAR_SYSTEM_NUMBER_OF_LINES_BOTTOM_DIR_2_GENERIC },
};
Func<Element, BuiltInParameter, Parameter> param = (e, bip) =>
{
    try { return e.get_Parameter(bip); } catch (Exception) { return null; }
};
Func<Element, BuiltInParameter, BuiltInParameter, Parameter> either = (e, first, second) => param(e, first) ?? param(e, second);

if (refused.Length == 0)
{
    var firstBar = layerValues.First(v => v.Item1 != null).Item1;
    foreach (var host in elements)
    {
        if (host == null) continue;
        var name = (host.Category == null ? "element" : host.Category.Name) + " '" + host.Name + "' (id " + host.Id + ")";
        var isWall = host is Wall;
        if (!(host is Floor) && !isWall)
        {
            skipped.Add(name + ": not a slab or a wall - columns take PLACE_COLUMN_REBAR, beams PLACE_BEAM_REBAR, "
                + "pad footings PLACE_FOOTING_REBAR");
            continue;
        }
        if (!RebarHostData.IsValidHost(host))
        {
            skipped.Add(name + ": not a rebar host - its type is not structural, or its material is not concrete");
            continue;
        }
        var data = RebarHostData.GetRebarHostData(host);
        var already = data.GetAreaReinforcementsInHost().Count;
        if (already > 0)
        {
            skipped.Add(name + ": already holds " + already + " area reinforcement - run again it would double it. "
                + "REPORT_HOST_REBAR reads it");
            continue;
        }

        // The major direction, in the host's own plane.
        XYZ major = null;
        string directionProblem = null;
        if (isWall)
        {
            var line = ((host as Wall).Location as LocationCurve) == null ? null : ((host as Wall).Location as LocationCurve).Curve as Line;
            if (direction == "horizontal")
            {
                if (line == null) directionProblem = "a curved wall has no single horizontal direction";
                else major = line.Direction.Normalize();
            }
            else if (direction == "vertical") major = XYZ.BasisZ;
            else directionProblem = "a wall's major direction is horizontal or vertical, not '" + majorDirection + "'";
        }
        else
        {
            double degrees;
            if (direction == "x") major = XYZ.BasisX;
            else if (direction == "y") major = XYZ.BasisY;
            else if (double.TryParse(direction.Replace("deg", "").Trim(), styleFloat, invariant, out degrees))
                major = new XYZ(Math.Cos(degrees * Math.PI / 180), Math.Sin(degrees * Math.PI / 180), 0);
            else directionProblem = "a slab's major direction is x, y, or an angle in degrees from the model's x axis, not '" + majorDirection + "'";
        }
        if (directionProblem != null)
        {
            skipped.Add(name + ": " + directionProblem + " - nothing placed in it");
            continue;
        }

        if (areaType == null && !areaTypeMade)
        {
            areaType = doc.GetElement(AreaReinforcementType.CreateDefaultAreaReinforcementType(doc)) as AreaReinforcementType;
            areaTypeMade = true;
            findings.Add("No Area Reinforcement Type was loaded, so Revit's default one was made"
                + (areaType == null ? "" : ": '" + areaType.Name + "'") + " - it holds no bar or spacing of its own.");
        }

        AreaReinforcement area = null;
        string failure = null;
        var ids = isWall ? wallIds : floorIds;
        var written = new List<string>();
        try
        {
            area = AreaReinforcement.Create(doc, host, major, areaType.Id, firstBar.Id,
                hookType == null ? ElementId.InvalidElementId : hookType.Id);
            if (area == null) throw new InvalidOperationException("Revit made no area reinforcement");
            for (var i = 0; i < 4; i++)
            {
                var asked = layerValues[i];
                var active = either(area, ids[i][0], genericIds[i][0]);
                if (active == null || active.IsReadOnly) throw new InvalidOperationException("its " + layerAsks[i].Item1 + " layer cannot be switched");
                active.Set(asked.Item1 == null ? 0 : 1);
                if (asked.Item1 == null) continue;
                var barType = either(area, ids[i][1], genericIds[i][1]);
                var spacing = either(area, ids[i][2], genericIds[i][2]);
                if (barType == null || spacing == null) throw new InvalidOperationException("its " + layerAsks[i].Item1 + " layer has no bar type or spacing to set");
                barType.Set(asked.Item1.Id);
                spacing.Set(asked.Item2);
                // The hook was given to Create for every layer; it is written per
                // layer only when there is one, never as "none", which a layer's
                // parameter may not take.
                var hookParameter = param(area, ids[i][3]);
                if (hookType != null && hookParameter != null && !hookParameter.IsReadOnly)
                    hookParameter.Set(hookType.Id);
                written.Add(layerAsks[i].Item1);
            }
            doc.Regenerate();
        }
        catch (Exception ex)
        {
            failure = ex.Message;
        }
        if (failure != null)
        {
            if (area != null && area.IsValidObject)
            {
                try { doc.Delete(area.Id); } catch (Exception) { }
            }
            skipped.Add(name + ": Revit refused the area reinforcement - " + failure + " - it was removed again");
            continue;
        }

        // ---- read back, layer by layer, with Revit's own label -------------------------
        var layerLines = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var asked = layerValues[i];
            var active = either(area, ids[i][0], genericIds[i][0]);
            var on = active != null && active.AsInteger() == 1;
            var label = active == null ? layerAsks[i].Item1 : active.Definition.Name;
            if (asked.Item1 == null)
            {
                if (on) readBackWrong.Add(name + ": " + label + " reads on where none was asked");
                continue;
            }
            var barType = either(area, ids[i][1], genericIds[i][1]);
            var spacing = either(area, ids[i][2], genericIds[i][2]);
            var count = either(area, ids[i][4], genericIds[i][3]);
            var typeNow = barType == null ? ElementId.InvalidElementId : barType.AsElementId();
            var spacingNow = spacing == null ? 0 : spacing.AsDouble();
            layerLines.Add(label + " " + (on ? "" : "OFF ") + "'" + (doc.GetElement(typeNow) == null ? "?" : doc.GetElement(typeNow).Name) + "' @ "
                + mm(spacingNow) + " mm" + (count == null ? "" : ", " + count.AsInteger() + " lines"));
            if (!on || typeNow != asked.Item1.Id || Math.Abs(spacingNow - asked.Item2) > 0.05 / 304.8)
                readBackWrong.Add(name + ": " + label + " reads back other than '" + asked.Item1.Name + "' @ " + mm(asked.Item2) + " mm");
        }
        var rule = param(area, BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE);
        var barsMade = area.GetRebarInSystemIds().Count;
        placed.Add(area.Id);
        hostsReinforced.Add(host.Id);
        lines.Add(name + ": area reinforcement id " + area.Id + ", major direction " + direction
            + (rule == null ? "" : ", layout " + rule.AsValueString()) + ", " + barsMade + " bar set(s) in it; "
            + string.Join("; ", layerLines) + (hookType == null ? "" : "; hooks '" + hookType.Name + "'"));
    }
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(placed.Count + " area reinforcement(s) placed and read back in " + hostsReinforced.Count + " host(s); "
        + skipped.Count + " skipped" + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " layer(s) read back other than asked" : "")
        + ". The bars follow each host's own boundary and rebar cover.");
    findings.AddRange(skipped);
    findings.AddRange(readBackWrong);
}
