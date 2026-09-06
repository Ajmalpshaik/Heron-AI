// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `maximized`, `lengthsAfter`, `unchanged`, `hiddenByViewExtent`, `notADatum`
// and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A grid set, one undo.
//
// THE MEASURE-IT-YOURSELF ROUTE IS A TRAP TWICE OVER.
//
// Setting a grid's curve is refused with "the curve is unbound or not
// coincident with the original" - and it is refused when the grid is handed
// back its own curve, untouched. So that message is not about your geometry,
// and there is no version of the maths that satisfies it. Revit's own maximize
// call is the one that works.
//
// And a whole-model bounding box is not the building: sheets carry a nominal
// box in model space and cameras sit outside, so grids extended to it come out
// half as long again as anything real. Nothing here measures the model.
//
// LENGTH IS READ THROUGH THE GRID TYPE, NOT THE DATUM TYPE.
//
// Only a grid has a curve. A level is a horizontal plane and has none, so
// asking the shared type for one does not fail at runtime - it fails to
// COMPILE, which is how a fragment that was verified against a real model can
// still stop building on every release.

int maximized = 0;
var lengthsAfter = new Dictionary<ElementId, double>();
var unchanged = new List<ElementId>();
var hiddenByViewExtent = new List<ElementId>();
var notADatum = new List<ElementId>();
var refused = new List<ElementId>();

Func<DatumPlane, double> lengthOf = datum =>
{
    var grid = datum as Grid;
    if (grid == null) return 0;
    try
    {
        var curve = grid.Curve;
        return curve != null && curve.IsBound ? curve.Length : 0;
    }
    catch { return 0; }
};

// The views that can override an end. Templates cannot show anything, so they
// are left out; a view with no id-bearing crop still answers the question.
var views = new FilteredElementCollector(doc)
    .OfClass(typeof(View))
    .Cast<View>()
    .Where(v => !v.IsTemplate)
    .ToList();

foreach (var element in elements)
{
    var datum = element as DatumPlane;
    if (datum == null || !(datum is Grid || datum is Level))
    {
        if (element != null) notADatum.Add(element.Id);
        continue;
    }

    double before = lengthOf(datum);

    try { datum.Maximize3DExtents(); }
    catch { refused.Add(datum.Id); continue; }

    double after = lengthOf(datum);
    lengthsAfter[datum.Id] = after;

    // A grid that was already at the extent is not a change. Counting it would
    // report every grid maximized on a set where nothing moved.
    if (datum is Grid && Math.Abs(after - before) < 1e-6) unchanged.Add(datum.Id);
    else maximized++;

    // Where an end was dragged short in a view, that view keeps its own
    // extent and the change will not appear there. That is the report anybody
    // looking at that view needs, because on screen this looks like nothing
    // happened.
    bool overridden = false;
    foreach (var view in views)
    {
        try
        {
            if (datum.GetDatumExtentTypeInView(DatumEnds.End0, view) == DatumExtentType.ViewSpecific ||
                datum.GetDatumExtentTypeInView(DatumEnds.End1, view) == DatumExtentType.ViewSpecific)
            {
                overridden = true;
                break;
            }
        }
        catch { }
    }
    if (overridden) hiddenByViewExtent.Add(datum.Id);
}
