// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `offsetMm` are in
// scope; leaves `created`, `refused` and `notLinear` behind.
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
if (references.Count >= 2 && runDirection != null)
{
    var across = view.ViewDirection.CrossProduct(runDirection);

    // Zero length means the runs point straight at the viewer - ducts seen
    // end-on in a section. There is no direction across them that can be drawn
    // on this view, so the whole string is refused rather than drawn along an
    // arbitrary axis.
    if (across.GetLength() < 1e-9)
    {
        refused.AddRange(accepted);
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

        // The string sits offset ALONG the runs, clear of the elements it
        // measures, and spans from the outermost reference to the outermost
        // reference so every witness line has something to land on.
        var basePoint = anchors[0] - across * anchors[0].DotProduct(across) + runDirection * offset;

        var from = basePoint + across * lowest;
        var to = basePoint + across * highest;

        if ((to - from).GetLength() > 1e-6)
        {
            var dimensionLine = Line.CreateBound(from, to);

            var array = new ReferenceArray();
            foreach (var reference in references) array.Append(reference);

            var dimension = doc.Create.NewDimension(view, dimensionLine, array);
            if (dimension != null) created = dimension.Id;
        }
        else
        {
            // Every run sits on the same line across - they are collinear, so
            // the gap being dimensioned is zero and there is nothing to draw.
            refused.AddRange(accepted);
        }
    }
}
else
{
    // Fewer than two usable references. One reference is not a short
    // dimension, it is not a dimension.
    refused.AddRange(accepted);
}
