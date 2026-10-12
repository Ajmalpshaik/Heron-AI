// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves `spots`,
// `spotCount`, `onNothing` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY SPOT ELEVATION, SPOT COORDINATE AND SPOT SLOPE, AS IT READS ON THE
// DRAWING. In one view when one is given, otherwise in every view. For each:
// the view it sits in, its kind (the category name Revit gives it, so no enum
// is relied on), the text it shows - prefix, value and suffix as printed -
// and the element it measures.
//
// THE TEXT IS REVIT'S, NOT A NUMBER WORKED OUT HERE. A spot's value is shown
// in the project's units and the spot type's own format; ValueString is that
// text. Re-deriving it from geometry would print a figure the drawing does
// not show.
//
// A SPOT ON NOTHING IS THE FINDING. A spot whose reference no longer names an
// element - the floor it measured was deleted or replaced - still prints a
// value on the sheet. Those are listed apart, by id.

var lines = new List<string>();
var onNothing = new List<ElementId>();
var findings = new List<string>();
var spotCount = 0;

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

var collector = view == null
    ? new FilteredElementCollector(doc)
    : new FilteredElementCollector(doc, view.Id);

var rows = new List<Tuple<string, string>>();
foreach (var element in collector.OfClass(typeof(SpotDimension)))
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

rows.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase));
foreach (var row in rows) lines.Add(row.Item2);

if (wantedView != "" && view == null)
{
    lines.Clear();
    spotCount = 0;
    onNothing.Clear();
    findings.Add((viewProblem ?? "No view is named \"" + wantedView + "\".") + " Nothing was read. Give "
        + "the name as the Project Browser shows it, or leave it empty for every view.");
}
else
{
    findings.Add(spotCount + " spot dimension(s)" + (view == null ? " in the model" : " in \"" + view.Name + "\"")
        + ", " + onNothing.Count + " measuring nothing.");
}
var spots = string.Join("\n", lines);
