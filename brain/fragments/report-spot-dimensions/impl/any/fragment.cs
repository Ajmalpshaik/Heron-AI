// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves `spots`,
// `spotCount`, `dimensions`, `dimensionCount`, `onNothing` and `findings`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY DIMENSION IN A VIEW, AS IT READS ON THE DRAWING. In one view when one
// is given, otherwise in every view.
//
// SPOTS - spot elevations, coordinates and slopes - go in `spots`, exactly as
// version 1 listed them: the view, the kind (the category name Revit gives
// it, so no enum is relied on), the text it shows and the element it
// measures.
//
// VERSION 2: EVERY OTHER DIMENSION goes in `dimensions` - linear, angular,
// radial, diameter and arc length. For each: the view, the kind and its
// shape, the type, the text it shows (each segment of a string, in order),
// and what it measures between - a grid or level by its name, a reference
// plane by its name, an element by its category, family, type and id, a
// face in a linked model by the link. Asked on 2026-10-15 as "list the
// dimensions in this view with their values and references", which reached
// CREATE_LINEAR_DIMENSION, a write, because no read could answer it (5b-483).
//
// THE TEXT IS REVIT'S, NOT A NUMBER WORKED OUT HERE. A value is shown in the
// project's units and the dimension type's own format; ValueString is that
// text. Re-deriving it from geometry would print a figure the drawing does
// not show. A value TYPED OVER - "EQ", or a number somebody wrote in - is
// printed as typed, with what it actually measures beside it, because the
// drawing shows the first and the model holds the second.
//
// A DIMENSION ON NOTHING IS A FINDING. One whose references no longer name an
// element - what it measured was deleted or replaced - still prints a value.
// Those are listed apart, by id, spot or not.
//
// A LOCK OR AN ALIGNMENT IS A DIMENSION UNDERNEATH, in the Constraints
// category. Nobody drew it as a dimension and the drawing shows none, so it
// is left out and counted.

var lines = new List<string>();
var dimensionLines = new List<string>();
var onNothing = new List<ElementId>();
var findings = new List<string>();
var spotCount = 0;
var dimensionCount = 0;
var constraintsLeftOut = 0;

// THE VIEW BY NAME, AND EMPTY MEANS EVERY VIEW. A View need cannot be left
// out - the binder refuses an absent one - so the name is a string, looked up
// here, and "" is the whole model.
// "Level 1" can name a floor plan AND a ceiling plan, so the kind may be
// written in front - "FloorPlan: Level 1", as the rest of Heron writes views -
// and a name that still answers to two views is refused, never guessed.
View view = null;
string viewProblem = null;
var wantedView = (viewName ?? "").Trim();
if (wantedView != "")
{
    var wantedKind = "";
    var bare = wantedView;
    var colon = wantedView.IndexOf(':');
    if (colon > 0)
    {
        wantedKind = wantedView.Substring(0, colon).Replace(" ", "").Trim();
        bare = wantedView.Substring(colon + 1).Trim();
    }
    var answering = new List<View>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var v = candidate as View;
        if (v == null || v.IsTemplate) continue;
        if (!string.Equals(v.Name, bare, StringComparison.OrdinalIgnoreCase)) continue;
        if (wantedKind != "" && !string.Equals(v.ViewType.ToString(), wantedKind, StringComparison.OrdinalIgnoreCase)) continue;
        answering.Add(v);
    }
    if (answering.Count == 1) view = answering[0];
    else if (answering.Count > 1)
        viewProblem = answering.Count + " views are named \"" + bare + "\" ("
            + string.Join(", ", answering.Select(a => a.ViewType.ToString())) + "). Write it as \""
            + answering[0].ViewType + ": " + bare + "\".";
}

// Two collectors: one is used up by being walked. Spots are collected by their
// own class exactly as version 1 collected them, so version 2 cannot change
// what `spots` holds.
Func<FilteredElementCollector> inScope = () => view == null
    ? new FilteredElementCollector(doc)
    : new FilteredElementCollector(doc, view.Id);

var constraintsCategory = new ElementId(BuiltInCategory.OST_Constraints);

// What one reference measures to, in the words a modeller would use, or null
// when it names no element.
Func<Reference, string> describe = reference =>
{
    if (reference == null) return null;
    Element target = null;
    try { target = doc.GetElement(reference.ElementId); } catch { }
    if (target == null) return null;
    if (target is Grid) return "grid " + target.Name;
    if (target is Level) return "level " + target.Name;
    if (target is ReferencePlane)
        return "plane " + (string.IsNullOrEmpty(target.Name) ? target.Id.ToString() : target.Name);
    if (target is RevitLinkInstance)
        return "a face in link " + target.Name
            + (reference.LinkedElementId != ElementId.InvalidElementId ? " (its element " + reference.LinkedElementId + ")" : "");
    var category = target.Category == null ? "element" : target.Category.Name;
    var instance = target as FamilyInstance;
    var named = "";
    try
    {
        if (instance != null && instance.Symbol != null) named = " " + instance.Symbol.FamilyName + ": " + instance.Name;
    }
    catch { }
    var mark = "";
    try
    {
        var markParameter = target.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        var markText = markParameter == null ? null : markParameter.AsString();
        if (!string.IsNullOrEmpty(markText)) mark = " Mark " + markText;
    }
    catch { }
    return category + named + mark + " " + target.Id;
};

// One value as the drawing shows it. A typed-over value is printed as typed,
// with the measured value beside it.
Func<string, string, string, string, string> shownText = (prefix, value, suffix, typed) =>
{
    if (!string.IsNullOrEmpty(typed))
        return "\"" + (prefix ?? "") + typed + (suffix ?? "") + "\" TYPED OVER, measures " + (value ?? "?");
    return (prefix ?? "") + (value ?? "") + (suffix ?? "");
};

var rows = new List<Tuple<string, string>>();
foreach (var element in inScope().OfClass(typeof(SpotDimension)))
{
    var spot = element as SpotDimension;
    if (spot == null) continue;
    spotCount++;

    var ownerName = "";
    try
    {
        var owner = doc.GetElement(spot.OwnerViewId) as View;
        ownerName = owner == null ? "(no view)" : owner.Name;
    }
    catch { }

    var kind = spot.Category == null ? "Spot" : spot.Category.Name;

    var shown = "";
    try
    {
        shown = (spot.Prefix ?? "") + (spot.ValueString ?? "") + (spot.Suffix ?? "");
    }
    catch { }
    if (shown == "") shown = "(no value shown)";

    var measured = "";
    try
    {
        var references = spot.References;
        if (references != null && references.Size > 0)
        {
            var target = doc.GetElement(references.get_Item(0).ElementId);
            if (target != null)
                measured = (target.Category == null ? "element" : target.Category.Name) + " " + target.Id;
        }
    }
    catch { }
    if (measured == "")
    {
        onNothing.Add(spot.Id);
        measured = "NOTHING - its reference names no element";
    }

    rows.Add(Tuple.Create(ownerName, ownerName + " | " + kind + " | " + shown + " | on " + measured
        + " | spot " + spot.Id));
}

var dimensionRows = new List<Tuple<string, string>>();
foreach (var element in inScope().OfClass(typeof(Dimension)))
{
    var dimension = element as Dimension;
    if (dimension == null || dimension is SpotDimension) continue;

    if (dimension.Category != null && dimension.Category.Id == constraintsCategory)
    {
        constraintsLeftOut++;
        continue;
    }
    dimensionCount++;

    var ownerName = "";
    try
    {
        var owner = doc.GetElement(dimension.OwnerViewId) as View;
        ownerName = owner == null ? "(no view)" : owner.Name;
    }
    catch { }

    var shape = "";
    try { shape = dimension.DimensionShape.ToString(); } catch { }
    var dimensionKind = (dimension.Category == null ? "Dimensions" : dimension.Category.Name)
        + (shape == "" ? "" : " (" + shape + ")");

    var typeName = "";
    try
    {
        var type = doc.GetElement(dimension.GetTypeId());
        if (type != null) typeName = type.Name;
    }
    catch { }

    var values = new List<string>();
    try
    {
        if (dimension.NumberOfSegments > 1)
        {
            foreach (DimensionSegment segment in dimension.Segments)
                values.Add(shownText(segment.Prefix, segment.ValueString, segment.Suffix, segment.ValueOverride));
        }
        else
        {
            values.Add(shownText(dimension.Prefix, dimension.ValueString, dimension.Suffix, dimension.ValueOverride));
        }
    }
    catch { }
    var reads = values.Count == 0 ? "(no value shown)" : string.Join(" + ", values);

    var between = new List<string>();
    var anyTarget = false;
    try
    {
        var references = dimension.References;
        if (references != null)
        {
            foreach (Reference reference in references)
            {
                var said = describe(reference);
                if (said == null) between.Add("(nothing)");
                else
                {
                    anyTarget = true;
                    between.Add(said);
                }
            }
        }
    }
    catch { }
    var measures = string.Join(" / ", between);
    if (!anyTarget)
    {
        onNothing.Add(dimension.Id);
        measures = "NOTHING - its references name no element";
    }

    dimensionRows.Add(Tuple.Create(ownerName, ownerName + " | " + dimensionKind + " | " + typeName + " | reads "
        + reads + " | between " + measures + " | dimension " + dimension.Id));
}

rows.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase));
foreach (var row in rows) lines.Add(row.Item2);
dimensionRows.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase));
foreach (var row in dimensionRows) dimensionLines.Add(row.Item2);

if (wantedView != "" && view == null)
{
    lines.Clear();
    dimensionLines.Clear();
    spotCount = 0;
    dimensionCount = 0;
    onNothing.Clear();
    findings.Add((viewProblem ?? "No view is named \"" + wantedView + "\".") + " Nothing was read. Give "
        + "the name as the Project Browser shows it, or leave it empty for every view.");
}
else
{
    var inWhat = view == null ? " in the model" : " in \"" + view.Name + "\"";
    findings.Add(spotCount + " spot dimension(s)" + inWhat + ", " + dimensionCount + " other dimension(s), "
        + onNothing.Count + " measuring nothing.");
    if (constraintsLeftOut > 0)
        findings.Add(constraintsLeftOut + " lock(s) or alignment(s) left out - a constraint is a dimension "
            + "underneath, but the drawing shows none.");
}
var spots = string.Join("\n", lines);
var dimensions = string.Join("\n", dimensionLines);
