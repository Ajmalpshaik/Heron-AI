// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `axis` and `offsetMm` are
// in scope; leaves `created`, `refused`, `notLinear`, `dimensionedCount` and
// `problem` behind.
//
// VERSION 2 (2026-11-01) - DIMENSION_MEP_RUNS MERGED IN, AND WHAT IT HAD:
//
//   AN AXIS MAY BE NAMED. Empty measures ACROSS the runs, worked out from them
//   as version 1 did - right at any angle. `x` or `y` measures along that axis
//   of the model, which is what reaches RISERS: vertical pipes seen end-on in
//   a plan have no direction across them in the view, and version 1 refused
//   them; along a named axis their centrelines dimension like any other.
//
//   IT READS BACK. `dimensionedCount` is the references the dimension Revit
//   made actually holds - never `Dimension.Curve`, which throws "The input
//   curve is not bound" on a dimension this new. Fewer than two will not
//   survive a commit, and `problem` says so.
//
//   A REFUSAL BY REVIT IS CAUGHT. NewDimension throwing is one dimension's
//   problem and is said in `problem`; let out of here, it would abort the
//   caller's transaction and every change made before it (Golden Rule 16).
//
//   `problem` SAYS WHY NOTHING WAS DRAWN, in one sentence - and when nothing
//   handed in was a run, that DIMENSION_FAMILY_INSTANCES is the route for air
//   terminals, fixtures and equipment.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// A REFERENCE ONLY EXISTS IF IT WAS ASKED FOR, and this is the fact the whole
// fragment turns on. Reading an element's geometry the ordinary way returns
// shapes with NO Reference on them - null, silently - and NewDimension needs
// References, not points. Two options have to be set:
//
//   ComputeReferences        without it every Reference is null
//   IncludeNonVisibleObjects an MEP centreline is non-visible geometry, so
//                            without it a duct yields its solid and no line
//
// Set neither and the code runs, finds nothing, draws nothing, and reports no
// error. That is why the refusals here are counted and returned rather than
// letting an empty result look like a quiet success.
//
// THE DIMENSION LINE'S DIRECTION IS WHAT IS BEING MEASURED. NewDimension does
// not measure the gap between references in the abstract; it measures ALONG
// the line it is given. A line parallel to the ducts measures nothing useful.
// So the measuring axis is built perpendicular to the runs and in the view's
// plane: the view's own normal crossed with the run direction, which is the
// only direction that is both across the runs and drawable on this sheet.
//
// EVERY RUN MUST BE PARALLEL TO THE FIRST. A dimension string across ducts
// that are not parallel measures the distance to a point that depends on where
// along the run you happen to look - a number that changes with nothing
// changing. Elements out of parallel go to `refused` rather than being folded
// into the string.

var offset = offsetMm / 304.8;

ElementId created = null;
var refused = new List<ElementId>();
var notLinear = new List<ElementId>();
int dimensionedCount = 0;
string problem = "";

// Left out (bound as "across") or empty is across the runs; x and y are the
// model's own axes.
var wantedAxis = (axis ?? "").Trim().ToLowerInvariant();
if (wantedAxis == "across") wantedAxis = "";
XYZ namedAxis = null;
if (wantedAxis == "x") namedAxis = XYZ.BasisX;
else if (wantedAxis == "y") namedAxis = XYZ.BasisY;
else if (wantedAxis.Length > 0)
    problem = "axis must be x, y or left empty to measure across the runs - guessing one would "
            + "measure the wrong direction and the drawing would still look plausible, so NOTHING "
            + "WAS DRAWN";

var options = new Options();
options.ComputeReferences = true;
options.IncludeNonVisibleObjects = true;
options.View = view;

XYZ runDirection = null;

var references = new List<Reference>();
var positions = new List<double>();
var anchors = new List<XYZ>();

// The ids that made it into the string, kept so that abandoning the string
// later refuses exactly those and not the ones already refused for their own
// reason. An id appearing twice in `refused` would report more failures than
// there were elements.
var accepted = new List<ElementId>();

// Collected first, measured second: the measuring axis cannot be built until
// the first run's direction is known, and it has to apply to all of them.
var centrelines = new List<KeyValuePair<ElementId, Line>>();

foreach (var element in elements)
{
    if (problem.Length > 0) break;
    if (element == null) continue;

    // A quick structural test before touching geometry. An element with no
    // location curve has no centreline to dimension and is a different job -
    // an air terminal, a piece of equipment - not a failure of this one.
    var location = element.Location as LocationCurve;
    if (location == null || !(location.Curve is Line)) { notLinear.Add(element.Id); continue; }

    var geometry = element.get_Geometry(options);
    if (geometry == null) { refused.Add(element.Id); continue; }

    Line centreline = null;

    foreach (var part in geometry)
    {
        // The centreline comes back as a Line carrying a Reference. Anything
        // without one is unusable here however right it looks: NewDimension
        // takes References and a Line with a null Reference cannot be given
        // to it.
        var line = part as Line;
        if (line == null) continue;
        if (line.Reference == null) continue;

        centreline = line;
        break;
    }

    if (centreline == null) { refused.Add(element.Id); continue; }

    centrelines.Add(new KeyValuePair<ElementId, Line>(element.Id, centreline));
}

foreach (var entry in centrelines)
{
    var centreline = entry.Value;

    var along = centreline.GetEndPoint(1) - centreline.GetEndPoint(0);
    if (along.GetLength() < 1e-9) { refused.Add(entry.Key); continue; }
    along = along.Normalize();

    if (runDirection == null) runDirection = along;
    else if (Math.Abs(along.DotProduct(runDirection)) < 0.999)
    {
        // Out of parallel by more than about two and a half degrees. The
        // distance between two non-parallel runs is not one number, so there
        // is nothing honest to put in the string.
        refused.Add(entry.Key);
        continue;
    }

    references.Add(centreline.Reference);
    anchors.Add(centreline.GetEndPoint(0));
    accepted.Add(entry.Key);
}

// Two references is the minimum a dimension can hold. One is not a short
// dimension, it is not a dimension.
if (problem.Length > 0)
{
    // An axis that is not one: nothing was looked at, nothing is drawn.
}
else if (references.Count >= 2 && runDirection != null)
{
    // ACROSS THE RUNS, unless an axis was named. The view's own normal crossed
    // with the run direction is the only direction that is both across the
    // runs and drawable on this sheet.
    var across = namedAxis ?? view.ViewDirection.CrossProduct(runDirection);

    // Zero length means the runs point straight at the viewer - ducts seen
    // end-on in a section, risers in a plan. There is no direction across
    // them that can be drawn on this view, so the whole string is refused
    // rather than drawn along an arbitrary axis - unless an axis was named.
    if (across.GetLength() < 1e-9)
    {
        refused.AddRange(accepted);
        problem = "these runs point straight at the viewer - risers in a plan, or ducts seen end-on "
                + "- so there is no direction across them on this view. Name the axis, x or y, to "
                + "dimension them along it. NOTHING WAS DRAWN";
    }
    else if (namedAxis != null && Math.Abs(namedAxis.DotProduct(view.ViewDirection)) > 0.999)
    {
        // The axis named points straight out of this view - y in a section
        // looking north. A dimension line along it cannot be drawn on the view.
        refused.AddRange(accepted);
        problem = string.Format("the axis named, {0}, points straight out of this view, so a "
                + "dimension along it cannot be drawn here. Name the other axis, or leave it out. "
                + "NOTHING WAS DRAWN", wantedAxis);
    }
    else if (namedAxis != null && Math.Abs(namedAxis.DotProduct(runDirection)) > 0.999)
    {
        // The axis named runs ALONG the runs. A dimension measured along
        // parallel lines has nothing to measure between them.
        refused.AddRange(accepted);
        problem = string.Format("the axis named, {0}, runs along these runs rather than across them, "
                + "and a dimension along parallel runs has nothing to measure between them. Name the "
                + "other axis, or leave it out. NOTHING WAS DRAWN", wantedAxis);
    }
    else
    {
        across = across.Normalize();

        foreach (var anchor in anchors) positions.Add(anchor.DotProduct(across));

        double lowest = positions[0], highest = positions[0];
        for (int i = 1; i < positions.Count; i++)
        {
            lowest = Math.Min(lowest, positions[i]);
            highest = Math.Max(highest, positions[i]);
        }

        // The string sits OFFSET clear of the elements it measures: along the
        // runs when measuring across them, and square to the named axis in the
        // view's plane when one was named - for risers, the run direction is
        // the view's own and an offset along it would not move the line on
        // the drawing. It spans from the outermost reference to the outermost
        // so every witness line has something to land on.
        var aside = runDirection;
        if (namedAxis != null)
        {
            var square = view.ViewDirection.CrossProduct(across);
            if (square.GetLength() > 1e-9) aside = square.Normalize();
        }
        var basePoint = anchors[0] - across * anchors[0].DotProduct(across) + aside * offset;

        var from = basePoint + across * lowest;
        var to = basePoint + across * highest;

        if ((to - from).GetLength() > 1e-6)
        {
            var dimensionLine = Line.CreateBound(from, to);

            var array = new ReferenceArray();
            foreach (var reference in references) array.Append(reference);

            Dimension dimension = null;
            try
            {
                dimension = doc.Create.NewDimension(view, dimensionLine, array);
            }
            catch (Exception ex)
            {
                problem = "Revit refused the dimension: " + ex.Message + ". NOTHING WAS DRAWN";
            }

            // THE READ-BACK, and NOT via Dimension.Curve - that throws "The
            // input curve is not bound" on a dimension this new.
            if (dimension != null)
            {
                created = dimension.Id;
                try { dimensionedCount = dimension.References == null ? 0 : dimension.References.Size; }
                catch { dimensionedCount = 0; }
                if (dimensionedCount < 2)
                {
                    // Degenerate, and it would not survive the commit. Taken
                    // out again rather than left in the caller's transaction
                    // while every run is reported refused - two answers that
                    // contradict each other.
                    try { doc.Delete(dimension.Id); } catch { }
                    created = null;
                    dimensionedCount = 0;
                    problem = "Revit made a dimension holding fewer than two references, which will "
                            + "not survive a commit, so it was taken out again. NOTHING WAS DRAWN";
                }
                else if (dimensionedCount < references.Count)
                {
                    problem = string.Format("the dimension holds {0} of the {1} centrelines asked for - "
                            + "Revit dropped {2}. Check which runs it measures before relying on it",
                            dimensionedCount, references.Count, references.Count - dimensionedCount);
                }
            }
            else if (problem.Length == 0)
            {
                problem = "no dimension was created, and Revit reported no reason";
            }

            if (dimensionedCount < 2) refused.AddRange(accepted);
        }
        else
        {
            // Every run sits on the same line across - they are collinear, so
            // the gap being dimensioned is zero and there is nothing to draw.
            refused.AddRange(accepted);
            problem = "every run sits on one line across, so the gap to dimension is zero. NOTHING "
                    + "WAS DRAWN";
        }
    }
}
else
{
    // Fewer than two usable references. One reference is not a short
    // dimension, it is not a dimension.
    refused.AddRange(accepted);
    problem = notLinear.Count > 0 && accepted.Count == 0 && refused.Count == 0
        ? "nothing handed in is a straight run - a duct, pipe, conduit or cable tray - so there "
          + "is no centreline to dimension. If these are air terminals, fixtures or equipment, "
          + "DIMENSION_FAMILY_INSTANCES is the route that works. NOTHING WAS DRAWN"
        : "fewer than two of these gave up a centreline parallel to the first, so nothing was "
          + "drawn - a one-reference dimension is degenerate and Revit drops it at commit. If "
          + "these are air terminals or equipment, DIMENSION_FAMILY_INSTANCES is the route that "
          + "works";
}
