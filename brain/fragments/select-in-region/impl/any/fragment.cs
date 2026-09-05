// NOT STANDALONE. Assumes `doc`, `minMm`, `maxMm`, `exact` and `categories` are
// in scope; leaves `elements`, `withoutGeometry` and `findings` behind.
//
// TWO TESTS, AND THE FAST ONE OVER-REPORTS SILENTLY. A bounding box is
// axis-aligned, so a sloped drainage pipe or a rotated duct has a box far
// larger than the pipe and is reported inside a volume it never enters. On
// level work the two agree; on anything sloped they do not, and nothing warns.
// So the report always names which test ran.
//
// THE EXACT TEST HAS ITS OWN TRAP: an element with no solid geometry cannot
// intersect anything and drops out - much annotation, and the analytical
// elements. Those are COUNTED, so a small exact answer beside a large fast one
// can be read rather than guessed at.
//
// MILLIMETRES TO FEET BY 304.8, PLAIN ARITHMETIC. No units API - that is what
// changed shape at 2021. The report states the box in millimetres so a value
// handed in already converted shows up as an absurd volume, not as a quiet zero.

const double MillimetresPerFoot = 304.8;

var elements = new List<Element>();
var withoutGeometry = 0;
var findings = new List<string>();

if (minMm == null || maxMm == null)
{
    findings.Add("Both corners of the volume are needed - give the minimum and maximum in millimetres");
}
else
{
    // Either order works. A box given max-first is not a mistake anybody should
    // have to think about.
    var lowMm = new XYZ(Math.Min(minMm.X, maxMm.X), Math.Min(minMm.Y, maxMm.Y), Math.Min(minMm.Z, maxMm.Z));
    var highMm = new XYZ(Math.Max(minMm.X, maxMm.X), Math.Max(minMm.Y, maxMm.Y), Math.Max(minMm.Z, maxMm.Z));

    var width = highMm.X - lowMm.X;
    var depth = highMm.Y - lowMm.Y;
    var height = highMm.Z - lowMm.Z;

    if (width <= 0 || depth <= 0 || height <= 0)
    {
        findings.Add(string.Format("That is not a volume: {0:0} by {1:0} by {2:0} mm. Two corners "
            + "have to differ in all three directions", width, depth, height));
    }
    else
    {
        var low = new XYZ(lowMm.X / MillimetresPerFoot, lowMm.Y / MillimetresPerFoot, lowMm.Z / MillimetresPerFoot);
        var high = new XYZ(highMm.X / MillimetresPerFoot, highMm.Y / MillimetresPerFoot, highMm.Z / MillimetresPerFoot);

        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        var failed = "";

        if (exact)
        {
            try
            {
                var corner = low;
                var dx = high.X - low.X;
                var dy = high.Y - low.Y;
                var dz = high.Z - low.Z;

                var profile = new List<Curve>
                {
                    Line.CreateBound(corner, corner + new XYZ(dx, 0, 0)),
                    Line.CreateBound(corner + new XYZ(dx, 0, 0), corner + new XYZ(dx, dy, 0)),
                    Line.CreateBound(corner + new XYZ(dx, dy, 0), corner + new XYZ(0, dy, 0)),
                    Line.CreateBound(corner + new XYZ(0, dy, 0), corner),
                };

                var loops = new List<CurveLoop> { CurveLoop.Create(profile) };
                var box = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, dz);

                collector = collector.WherePasses(new ElementIntersectsSolidFilter(box));
            }
            catch (Exception ex)
            {
                failed = ex.Message;
            }
        }
        else
        {
            collector = collector.WherePasses(new BoundingBoxIntersectsFilter(new Outline(low, high)));
        }

        if (failed.Length > 0)
        {
            findings.Add(string.Format("The exact test could not build its volume: {0}. Nothing was "
                + "returned - a silent fall back to the bounding-box test would answer a different "
                + "question under the same name", failed));
        }
        else
        {
            foreach (var element in collector) elements.Add(element);

            if (exact)
            {
                // Only meaningful for the exact test: the fast one never asks
                // for geometry, so nothing can drop out of it this way.
                var wider = new FilteredElementCollector(doc).WhereElementIsNotElementType();
                if (categories != null && categories.Count > 0)
                    wider = wider.WherePasses(new ElementMulticategoryFilter(categories));
                wider = wider.WherePasses(new BoundingBoxIntersectsFilter(new Outline(low, high)));

                var loose = 0;
                foreach (var element in wider) loose++;
                withoutGeometry = loose - elements.Count;
                if (withoutGeometry < 0) withoutGeometry = 0;
            }

            findings.Add(string.Format("{0} element(s) in a {1:0} x {2:0} x {3:0} mm volume, by the "
                + "{4} test{5}",
                elements.Count, width, depth, height,
                exact ? "EXACT geometry" : "fast BOUNDING BOX",
                categories == null || categories.Count == 0
                    ? ", across every category"
                    : string.Format(", within {0} category/categories", categories.Count)));

            if (!exact)
                findings.Add("A bounding box is axis-aligned, so a sloped or rotated element is "
                    + "reported inside a volume its geometry never enters. Ask for the exact test "
                    + "where that matters");
            else if (withoutGeometry > 0)
                findings.Add(string.Format("{0} element(s) whose bounding box overlaps the volume are "
                    + "NOT in this answer - either their real geometry misses it, or they have no "
                    + "solid geometry to test at all", withoutGeometry));
        }
    }
}
