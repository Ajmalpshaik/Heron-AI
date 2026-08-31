// NOT STANDALONE. Assumes `doc`, `elements`, `view`, `axis` and `offsetMm` are
// in scope, and leaves `dimensionId`, `dimensionedCount`, `noReference` and
// `problem` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE ONE THING THAT MAKES THIS WORK, AND IT IS ONE FLAG.
//
// A duct or pipe publishes no built-in centre references - that was measured,
// and the first conclusion drawn was "MEP cannot be dimensioned". Too broad. A
// run's CENTRELINE is a NON-VISIBLE OBJECT, so it is reachable through the
// geometry - but only when the geometry is requested with
// IncludeNonVisibleObjects set alongside ComputeReferences. Ask the ordinary
// way and a duct really does offer nothing to dimension to, which is exactly
// how the wrong conclusion was reached.
//
// So this fragment is the geometry walk, and DIMENSION_FAMILY_INSTANCES is the
// reference route for terminals and equipment. Neither works for the other's
// subject, and both carry the same table saying so.

ElementId dimensionId = ElementId.InvalidElementId;
int dimensionedCount = 0;
var noReference = new List<ElementId>();
string problem = "";

var wantedAxis = (axis ?? "").Trim().ToLowerInvariant();
if (wantedAxis != "x" && wantedAxis != "y")
{
    problem = "axis must be 'x' or 'y' - guessing one would silently measure "
            + "the wrong direction and the drawing would still look plausible";
}

if (problem.Length == 0)
{
    // THE FLAG. Both parts are required: ComputeReferences to get a Reference
    // at all, IncludeNonVisibleObjects because the centreline is not visible
    // geometry. Either alone returns nothing usable.
    var options = new Options();
    options.ComputeReferences = true;
    options.IncludeNonVisibleObjects = true;
    options.View = view;

    var references = new ReferenceArray();
    XYZ firstPoint = null;
    XYZ lastPoint = null;

    // Ordered along the axis so the chain reads the way a drafter would place
    // it, using each run's own curve midpoint as its position.
    var positioned = new List<KeyValuePair<double, Element>>();
    foreach (var element in elements)
    {
        var curve = element.Location as LocationCurve;
        if (curve == null || curve.Curve == null)
        {
            noReference.Add(element.Id);
            continue;
        }
        var mid = curve.Curve.Evaluate(0.5, true);
        positioned.Add(new KeyValuePair<double, Element>(
            wantedAxis == "x" ? mid.X : mid.Y, element));
    }
    positioned.Sort(delegate (KeyValuePair<double, Element> a,
                              KeyValuePair<double, Element> b)
    {
        return a.Key.CompareTo(b.Key);
    });

    foreach (var entry in positioned)
    {
        var element = entry.Value;
        Reference picked = null;

        var geometry = element.get_Geometry(options);
        if (geometry != null)
        {
            foreach (var item in geometry)
            {
                var line = item as Line;
                if (line != null && line.Reference != null)
                {
                    picked = line.Reference;
                    break;
                }
            }
        }

        if (picked == null)
        {
            // Reported by id. A run whose centreline could not be reached is a
            // fact about that element, not a reason to abandon the string.
            noReference.Add(element.Id);
            continue;
        }

        references.Append(picked);
        var curve = element.Location as LocationCurve;
        var mid = curve.Curve.Evaluate(0.5, true);
        if (firstPoint == null) firstPoint = mid;
        lastPoint = mid;
    }

    if (references.Size < 2)
    {
        problem = "fewer than two of these gave up a centreline to dimension "
                + "to, so nothing was drawn - a one-reference dimension is "
                + "degenerate and Revit drops it at commit. If these are air "
                + "terminals or equipment, DIMENSION_FAMILY_INSTANCES is the "
                + "route that works";
    }
    else
    {
        // Millimetres to internal feet: plain arithmetic, no units API.
        double offsetFeet = offsetMm / 304.8;

        Line line = wantedAxis == "x"
            ? Line.CreateBound(
                  new XYZ(firstPoint.X, firstPoint.Y + offsetFeet, firstPoint.Z),
                  new XYZ(lastPoint.X, firstPoint.Y + offsetFeet, firstPoint.Z))
            : Line.CreateBound(
                  new XYZ(firstPoint.X + offsetFeet, firstPoint.Y, firstPoint.Z),
                  new XYZ(firstPoint.X + offsetFeet, lastPoint.Y, firstPoint.Z));

        Dimension made = null;
        try
        {
            made = doc.Create.NewDimension(view, line, references);
        }
        catch (Exception ex)
        {
            problem = "Revit refused the dimension: " + ex.Message;
        }

        // THE READ-BACK, and NOT via Dimension.Curve - that throws "The input
        // curve is not bound" on a dimension this new.
        if (made != null)
        {
            dimensionId = made.Id;
            dimensionedCount = made.References == null ? 0 : made.References.Size;
            if (dimensionedCount < 2)
            {
                problem = "the dimension was created holding fewer than two "
                        + "references, which will not survive a commit";
            }
        }
        else if (problem.Length == 0)
        {
            problem = "no dimension was created, and Revit reported no reason";
        }
    }
}
