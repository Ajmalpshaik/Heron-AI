// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `symbolType` are in
// scope, and leaves `placed`, `newSymbols`, `alreadyHere`, `notReinforcement`,
// `notPlaced`, `typesLoaded`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One view of symbols is one
// undo entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > SYMBOL > AREA, PATH AND FABRIC REINFORCEMENT SYMBOLS -
// three classes, one Create signature each (Document, ElementId viewId,
// LinkElementId host, XYZ head, ElementId typeId), 2020 to 2027
// (tools/api-surface --members, 2026-11-02):
//   area reinforcement   RebarSystemSpanSymbol.Create   OST_AreaReinSpanSymbol
//   path reinforcement   PathReinSpanSymbol.Create      OST_PathReinSpanSymbol
//   fabric sheet         FabricReinSpanSymbol.Create    OST_FabricReinSpanSymbol
// A fabric AREA handed in gives a symbol to each of its sheets, as Revit's own
// Fabric Reinforcement Symbol tags a sheet.
//
// THE VIEW. RevitAPI.xml, all three: "only floor plan, reflected ceiling plans
// and elevations are permitted". A structural plan and a section are passed on
// and Revit's answer, if it refuses, is reported (row 5b-1455); a 3D view, a
// sheet, a schedule, a drafting view or a template is refused by name first.
//
// THE TYPE. `symbolType` named: it is used for the kind whose symbol category
// it is, and a name no symbol type has refuses the run. Every other kind - or
// every kind, left out - takes the type the PROJECT has set as its default for
// that symbol (Document.GetDefaultFamilyTypeId), the one Revit's own button
// places, and the answer names it. A kind with no default set is named and
// given nothing; the loaded types come back in `typesLoaded`.
//
// ONE SYMBOL PER HOST PER VIEW: a symbol of that kind already in this view on
// the host means the host is left alone and named. A re-run stacks nothing.
//
// AT THE MIDDLE OF THE HOST as this view bounds it. Laying symbols out is
// another job (MOVE_ANNOTATION).
//
// EVERY SYMBOL IS READ BACK - its type, its view and its host - and one that is
// wrong is removed again inside the same undo, its host named.

var placed = 0;
var newSymbols = new List<ElementId>();
var alreadyHere = new List<ElementId>();
var notReinforcement = new List<ElementId>();
var notPlaced = new List<ElementId>();
var typesLoaded = new List<string>();
var lines = new List<string>();
var findings = new List<string>();
string refused = "";

var areaCategory = new ElementId(BuiltInCategory.OST_AreaReinSpanSymbol);
var pathCategory = new ElementId(BuiltInCategory.OST_PathReinSpanSymbol);
var fabricCategory = new ElementId(BuiltInCategory.OST_FabricReinSpanSymbol);
var kinds = new[] { "area", "path", "fabric" };
var categoryOf = new Dictionary<string, ElementId> { { "area", areaCategory }, { "path", pathCategory }, { "fabric", fabricCategory } };
var wordOf = new Dictionary<string, string>
{
    { "area", "Area Reinforcement Symbol" }, { "path", "Path Reinforcement Symbol" }, { "fabric", "Fabric Reinforcement Symbol" },
};

// How a type reads in the Properties palette - "M_Path Reinforcement Symbol: Standard".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { family = null; }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};
Func<ElementType, string, bool> isNamed = (t, text) =>
{
    if (string.Equals(t.Name, text, StringComparison.Ordinal)) return true;
    return string.Equals(typeLabel(t), text.Replace(" : ", ": "), StringComparison.Ordinal);
};

// The symbol's own host, read back - the 2022 method, or the 2020-2021 property.
Func<IndependentTag, HashSet<ElementId>> taggedOf = tag =>
{
    var ids = new HashSet<ElementId>();
#if REVIT2020 || REVIT2021
    var one = tag.TaggedLocalElementId;
    if (one != null && one != ElementId.InvalidElementId) ids.Add(one);
#else
    foreach (var id in tag.GetTaggedLocalElementIds()) ids.Add(id);
#endif
    return ids;
};

// ---- the types, per kind -------------------------------------------------------
var typesOf = new Dictionary<string, List<ElementType>>();
foreach (var kind in kinds)
{
    typesOf[kind] = new FilteredElementCollector(doc).OfCategoryId(categoryOf[kind]).WhereElementIsElementType()
        .Cast<ElementType>().OrderBy(t => typeLabel(t), StringComparer.Ordinal).ToList();
    typesLoaded.Add(wordOf[kind] + " types: " + (typesOf[kind].Count == 0
        ? "none loaded - load one (Insert > Load Family, Annotations, Structural) first"
        : string.Join(", ", typesOf[kind].Select(t => "'" + typeLabel(t) + "'").ToArray())));
}

var typeText = (symbolType ?? "").Trim();
if (string.Equals(typeText, "none", StringComparison.OrdinalIgnoreCase)) typeText = "";

// ---- refused before anything is placed -----------------------------------------
var chosen = new Dictionary<string, ElementType>();
var chosenHow = new Dictionary<string, string>();
if (view == null)
    refused = "No view was named. Reinforcement symbols go in a floor plan, a structural plan, a ceiling plan "
        + "or an elevation; name it as the Project Browser shows it. Nothing was placed.";
else if (view.IsTemplate)
    refused = "\"" + view.Name + "\" is a view template, and a template holds no symbols. Name the view "
        + "itself. Nothing was placed.";
else if (view.ViewType != ViewType.FloorPlan && view.ViewType != ViewType.EngineeringPlan
         && view.ViewType != ViewType.CeilingPlan && view.ViewType != ViewType.Elevation
         && view.ViewType != ViewType.Section)
    refused = "\"" + view.Name + "\" is a " + view.ViewType + " view. Revit places reinforcement symbols in floor "
        + "plans, ceiling plans and elevations - name one. Nothing was placed.";
else
{
    string namedKind = null;
    if (typeText.Length > 0)
    {
        var hits = kinds.SelectMany(k => typesOf[k].Where(t => isNamed(t, typeText)).Select(t => Tuple.Create(k, t))).ToList();
        if (hits.Count == 0)
            refused = "No area, path or fabric reinforcement symbol type is called '" + typeText + "' - the name has "
                + "to match the Project Browser, capitals included. The loaded ones are in typesLoaded. Nothing was placed.";
        else if (hits.Count > 1)
            refused = hits.Count + " reinforcement symbol types are called '" + typeText + "' - "
                + string.Join(", ", hits.Select(h => "'" + typeLabel(h.Item2) + "'").ToArray())
                + " - so the name does not say which. Name it with its family. Nothing was placed.";
        else
        {
            namedKind = hits[0].Item1;
            chosen[namedKind] = hits[0].Item2;
            chosenHow[namedKind] = "named";
        }
    }
    foreach (var kind in kinds)
    {
        if (chosen.ContainsKey(kind)) continue;
        var defaultId = doc.GetDefaultFamilyTypeId(categoryOf[kind]);
        var fallback = defaultId == null || defaultId == ElementId.InvalidElementId ? null : doc.GetElement(defaultId) as ElementType;
        if (fallback != null)
        {
            chosen[kind] = fallback;
            chosenHow[kind] = "the project's default";
        }
    }
}

if (refused.Length == 0)
{
    // EACH HOST, BY KIND. A fabric area gives its sheets.
    var hosts = new List<Tuple<string, Element, ElementId>>();   // kind, host, what was handed in
    var seen = new HashSet<ElementId>();
    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null || !seen.Add(element.Id)) continue;
        if (element is AreaReinforcement) hosts.Add(Tuple.Create("area", element, element.Id));
        else if (element is PathReinforcement) hosts.Add(Tuple.Create("path", element, element.Id));
        else if (element is FabricSheet) hosts.Add(Tuple.Create("fabric", element, element.Id));
        else if (element is FabricArea)
        {
            var sheets = ((FabricArea)element).GetFabricSheetElementIds();
            if (sheets.Count == 0)
            {
                notPlaced.Add(element.Id);
                lines.Add(element.Id + ": a fabric area with no fabric sheets - nothing to put a symbol on.");
            }
            foreach (var sheetId in sheets)
            {
                var sheet = doc.GetElement(sheetId);
                if (sheet != null && seen.Add(sheetId)) hosts.Add(Tuple.Create("fabric", sheet, element.Id));
            }
        }
        else notReinforcement.Add(element.Id);
    }

    // What is already shown in this view, per kind.
    var shown = new Dictionary<string, HashSet<ElementId>>();
    foreach (var kind in kinds)
    {
        shown[kind] = new HashSet<ElementId>();
        foreach (var tag in new FilteredElementCollector(doc, view.Id).OfCategoryId(categoryOf[kind])
                     .WhereElementIsNotElementType().OfType<IndependentTag>())
            foreach (var id in taggedOf(tag)) shown[kind].Add(id);
    }

    var activated = new HashSet<ElementId>();
    foreach (var entry in hosts)
    {
        var kind = entry.Item1;
        var host = entry.Item2;
        var label = (host.Category == null ? kind : host.Category.Name) + " id " + host.Id
            + (entry.Item3 != host.Id ? " (of fabric area id " + entry.Item3 + ")" : "");

        if (shown[kind].Contains(host.Id))
        {
            alreadyHere.Add(host.Id);
            lines.Add(label + ": already shows a " + wordOf[kind] + " in this view - left alone.");
            continue;
        }
        ElementType type;
        if (!chosen.TryGetValue(kind, out type))
        {
            notPlaced.Add(host.Id);
            lines.Add(label + ": no " + wordOf[kind] + " type was named and the project has none set as its default "
                + "- name one; the loaded ones are in typesLoaded.");
            continue;
        }
        var symbol = type as FamilySymbol;
        if (symbol != null && !symbol.IsActive && activated.Add(symbol.Id)) symbol.Activate();

        var box = host.get_BoundingBox(view) ?? host.get_BoundingBox(null);
        if (box == null)
        {
            notPlaced.Add(host.Id);
            lines.Add(label + ": Revit gives it no extent to put a symbol at - left alone.");
            continue;
        }
        var head = (box.Min + box.Max) / 2.0;

        IndependentTag made = null;
        try
        {
            var link = new LinkElementId(host.Id);
            if (kind == "area") made = RebarSystemSpanSymbol.Create(doc, view.Id, link, head, type.Id);
            else if (kind == "path") made = PathReinSpanSymbol.Create(doc, view.Id, link, head, type.Id);
            else made = FabricReinSpanSymbol.Create(doc, view.Id, link, head, type.Id);
        }
        catch (Exception ex)
        {
            notPlaced.Add(host.Id);
            lines.Add(label + ": Revit would not place a " + wordOf[kind] + " here - " + ex.Message);
            continue;
        }
        if (made == null)
        {
            notPlaced.Add(host.Id);
            lines.Add(label + ": Revit returned no symbol.");
            continue;
        }

        // READ BACK: the type, the view and the host Revit holds.
        var heldType = made.GetTypeId();
        var heldHost = taggedOf(made);
        if (heldType != type.Id || made.OwnerViewId != view.Id || !heldHost.Contains(host.Id))
        {
            var why = heldType != type.Id ? "its type read back as '" + typeLabel(doc.GetElement(heldType) as ElementType) + "'"
                : made.OwnerViewId != view.Id ? "it read back in another view" : "it read back on another element";
            try { doc.Delete(made.Id); } catch (Exception) { }
            notPlaced.Add(host.Id);
            lines.Add(label + ": " + why + ", so it was removed again.");
            continue;
        }
        newSymbols.Add(made.Id);
        shown[kind].Add(host.Id);
        placed++;
        lines.Add(label + ": " + wordOf[kind] + " id " + made.Id + " of '" + typeLabel(type) + "' (" + chosenHow[kind] + ").");
    }

    findings.Add(placed + " reinforcement symbol(s) placed in \"" + view.Name + "\""
        + (alreadyHere.Count > 0 ? ", " + alreadyHere.Count + " host(s) already showing one" : "")
        + (notReinforcement.Count > 0 ? ", " + notReinforcement.Count + " element(s) not area, path or fabric reinforcement" : "")
        + (notPlaced.Count > 0 ? ", " + notPlaced.Count + " not placed" : "") + ".");
    foreach (var kind in kinds)
        if (chosen.ContainsKey(kind) && hosts.Any(h => h.Item1 == kind))
            findings.Add(wordOf[kind] + ": '" + typeLabel(chosen[kind]) + "', " + chosenHow[kind] + ".");
    if (hosts.Count == 0 && refused.Length == 0)
        findings.Add("None of what was handed in is area reinforcement, path reinforcement, a fabric sheet or a fabric area.");
}
else
{
    findings.Add(refused);
}

var report = string.Join(" | ", lines.ToArray());
