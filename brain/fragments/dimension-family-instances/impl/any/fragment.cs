// NOT STANDALONE. Assumes `doc`, `elements`, `view`, `axis` and `offsetMm` are
// in scope, and leaves `dimensionId`, `dimensionedCount`, `noReference` and
// `problem` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHY THIS IS THE FAMILY-INSTANCE ROUTE AND NOT THE ONLY ROUTE.
//
// A dimension needs a geometry REFERENCE, not a point. A family instance
// publishes built-in centre references and stock content really does carry
// them. Duct fittings and accessories publish NONE - all four reference types
// come back empty - and the first conclusion drawn from that measurement was
// "MEP cannot be dimensioned". That was too broad. Ducts, pipes, conduit and
// tray ARE dimensionable, by walking their geometry with non-visible objects
// included, because a run's centreline is a non-visible object. That is
// DIMENSION_MEP_RUNS. A zero here means "use that one", never "impossible".
//
// WHY FEWER THAN TWO REFERENCES CREATES NOTHING.
//
// A dimension with one reference is degenerate. Revit may accept it and then
// DROP IT AT COMMIT - a change that appears to work and is not there
// afterwards, which is the failure this whole library is written against. So
// the survivors are counted first and nothing is attempted below two.
//
// WHY THE READ-BACK DOES NOT USE Dimension.Curve.
//
// It throws "The input curve is not bound" on a dimension that was just
// created. The obvious way to verify the result is the one way that cannot be
// used, which is worth knowing before spending an afternoon on it.
// NumberOfSegments and References are read instead.

ElementId dimensionId = ElementId.InvalidElementId;
int dimensionedCount = 0;
var noReference = new List<ElementId>();
string problem = "";

var wantedAxis = (axis ?? "").Trim().ToLowerInvariant();
if (wantedAxis != "x" && wantedAxis != "y")
{
    problem = "axis must be 'x' or 'y' - 'x' dimensions horizontal spacing in "
            + "plan, 'y' vertical-in-plan. Guessing one would silently measure "
            + "the wrong direction";
}

if (problem.Length == 0)
{
    // The reference types worth asking for, in the order that produces the
    // dimension a drafter would place. Centre references are what makes a
    // dimension HOLD when the element later moves; a strong reference on the
    // body does not.
    var wantedTypes = wantedAxis == "x"
        ? new[] { FamilyInstanceReferenceType.CenterLeftRight,
                  FamilyInstanceReferenceType.StrongReference }
        : new[] { FamilyInstanceReferenceType.CenterFrontBack,
                  FamilyInstanceReferenceType.StrongReference };

    // Sorted along the axis first, so the chain reads left-to-right or
    // bottom-to-top the way somebody would draw it by hand.
    var candidates = new List<FamilyInstance>();
    foreach (var element in elements)
    {
        var instance = element as FamilyInstance;
        if (instance == null) { noReference.Add(element.Id); continue; }
        candidates.Add(instance);
    }

    var ordered = new List<FamilyInstance>(candidates);
    ordered.Sort(delegate (FamilyInstance a, FamilyInstance b)
    {
        var pa = a.Location as LocationPoint;
        var pb = b.Location as LocationPoint;
        if (pa == null || pb == null) return 0;
        double va = wantedAxis == "x" ? pa.Point.X : pa.Point.Y;
        double vb = wantedAxis == "x" ? pb.Point.X : pb.Point.Y;
        return va.CompareTo(vb);
    });

    var references = new ReferenceArray();
    XYZ firstPoint = null;
    XYZ lastPoint = null;

    foreach (var instance in ordered)
    {
        Reference picked = null;
        foreach (var type in wantedTypes)
        {
            var found = instance.GetReferences(type);
            if (found != null && found.Count > 0) { picked = found[0]; break; }
        }

        if (picked == null)
        {
            // No usable reference. Reported by id rather than dropped: a short
            // dimension string with no explanation reads as the fragment
            // choosing, when in fact the content had nothing to hold on to.
            noReference.Add(instance.Id);
            continue;
        }

        references.Append(picked);
        var point = instance.Location as LocationPoint;
        if (point != null)
        {
            if (firstPoint == null) firstPoint = point.Point;
            lastPoint = point.Point;
        }
    }

    if (references.Size < 2)
    {
        problem = "fewer than two of these expose a reference a dimension can "
                + "hold on to, so nothing was drawn - a one-reference dimension "
                + "is degenerate and Revit drops it at commit. If these are "
                + "ducts or pipes, DIMENSION_MEP_RUNS is the route that works";
    }
    else if (firstPoint == null || lastPoint == null)
    {
        problem = "these have no readable location, so there is no line to put "
                + "the dimension on";
    }
    else
    {
        // Millimetres to Revit's internal feet: plain arithmetic, exact on
        // every release, and no units API to break at 2021.
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
