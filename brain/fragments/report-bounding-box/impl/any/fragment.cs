// NOT STANDALONE. Assumes `doc`, `elements` and `maxRows` are in scope; leaves
// `findings`, `measured`, `noBoundingBox` and `combinedSizeMm` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE BOX IS ALIGNED TO PROJECT X AND Y. It is NOT the element's own size. For
// anything square to the project the two agree; for anything at an angle the box
// is bigger than the object, in a shape the object does not have. That is why
// "how big is this room" belongs to MEASURE_ROOM_DIMENSIONS and not here.
//
// `null` IS PASSED AS THE VIEW ON PURPOSE - that asks for the MODEL box. Passing
// a view gives the box as cropped by that view, which is not the element's
// extent and would change answer depending on which view was open.
//
// THE COMBINED BOX IS COMPUTED FROM EVERY ELEMENT, NOT ONLY THE LISTED ONES.
// The row list is capped for readability; the combined extent is the answer to
// "will it fit", and capping that would silently answer about a subset.
//
// AN ELEMENT WITH NO BOX IS REPORTED, NOT SKIPPED. Skipping makes the combined
// box look like it covers everything that was asked about.
//
// A FAMILY INSTANCE'S BOX CAN BE WIDER THAN WHAT IT SHOWS, AND THE ANSWER SAYS
// SO - FOR THE INSTANCES IT IS TRUE OF, AND NO OTHERS (version 2,
// FRAGMENT-ISSUES 5b-352). Measured 2026-10-07, Revit 2024: a nested C-channel
// family holding an upright body and a horizontal one, each switched by its
// own Visible parameter, read 200 x 1075 x 1100 mm - the hidden body and the
// shown one together.
//
// The figure reported is still Revit's own box; nothing here replaces it.
// Beside it, each family instance's GEOMETRY is read the way Revit hands it
// out by default - Fine detail, objects not set as Visible left out - and its
// extent taken. Where Revit's box reaches more than a millimetre past that
// extent on any side, the instance is named with both sizes. Where the two
// agree - an ordinary fitting, a terminal, a door - NOTHING is said: a caveat
// on every family instance would be read once and skipped for ever. The
// first 200 family instances are compared and the answer says when there
// were more.

var findings = new List<string>();
var measured = 0;
var familyInstances = 0;
var comparedInstances = 0;
var widerThanShown = new List<string>();
const int compareAtMost = 200;
var oneMillimetre = 1.0 / 304.8;

// The extent of what an element's geometry shows, or null when it gives none.
Func<Element, BoundingBoxXYZ> shownBoxOf = element =>
{
    var options = new Options();
    options.DetailLevel = ViewDetailLevel.Fine;
    GeometryElement geometry = null;
    try { geometry = element.get_Geometry(options); }
    catch (Exception) { geometry = null; }
    if (geometry == null) return null;

    var any = false;
    double x0 = 0, y0 = 0, z0 = 0, x1 = 0, y1 = 0, z1 = 0;
    Action<XYZ> take = p =>
    {
        if (p == null) return;
        if (!any) { x0 = x1 = p.X; y0 = y1 = p.Y; z0 = z1 = p.Z; any = true; return; }
        x0 = Math.Min(x0, p.X); y0 = Math.Min(y0, p.Y); z0 = Math.Min(z0, p.Z);
        x1 = Math.Max(x1, p.X); y1 = Math.Max(y1, p.Y); z1 = Math.Max(z1, p.Z);
    };

    Action<GeometryElement> walk = null;
    walk = pieces =>
    {
        foreach (GeometryObject piece in pieces)
        {
            var instance = piece as GeometryInstance;
            if (instance != null)
            {
                // In the model's own coordinates already.
                var inner = instance.GetInstanceGeometry();
                if (inner != null) walk(inner);
                continue;
            }

            var solid = piece as Solid;
            if (solid != null)
            {
                if (solid.Faces.Size == 0) continue;
                var box = solid.GetBoundingBox();
                if (box == null) continue;
                // A solid's box sits in its own transform: all eight corners.
                foreach (var x in new[] { box.Min.X, box.Max.X })
                    foreach (var y in new[] { box.Min.Y, box.Max.Y })
                        foreach (var z in new[] { box.Min.Z, box.Max.Z })
                            take(box.Transform.OfPoint(new XYZ(x, y, z)));
                continue;
            }

            var curve = piece as Curve;
            if (curve != null) { foreach (var p in curve.Tessellate()) take(p); continue; }

            var polyline = piece as PolyLine;
            if (polyline != null) { foreach (var p in polyline.GetCoordinates()) take(p); continue; }

            var mesh = piece as Mesh;
            if (mesh != null) { foreach (XYZ p in mesh.Vertices) take(p); continue; }

            var point = piece as Point;
            if (point != null) take(point.Coord);
        }
    };

    try { walk(geometry); }
    catch (Exception) { return null; }
    if (!any) return null;

    var shown = new BoundingBoxXYZ();
    shown.Min = new XYZ(x0, y0, z0);
    shown.Max = new XYZ(x1, y1, z1);
    return shown;
};
var noBoundingBox = new List<string>();
var combinedSizeMm = "";

var cap = maxRows > 0 ? maxRows : 50;

double minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;
var haveCombined = false;
var listed = 0;

foreach (var element in elements)
{
    if (element == null) continue;

    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); }
    catch (Exception) { box = null; }

    if (box == null)
    {
        noBoundingBox.Add(string.Format("{0} (id {1}) - no bounding box; a view-specific, annotation "
            + "or empty element", element.Name, element.Id));
        continue;
    }

    measured++;
    if (element is FamilyInstance)
    {
        familyInstances++;
        if (comparedInstances < compareAtMost)
        {
            var shown = shownBoxOf(element);
            if (shown != null)
            {
                comparedInstances++;
                var past = Math.Max(Math.Max(Math.Max(shown.Min.X - box.Min.X, box.Max.X - shown.Max.X),
                    Math.Max(shown.Min.Y - box.Min.Y, box.Max.Y - shown.Max.Y)),
                    Math.Max(shown.Min.Z - box.Min.Z, box.Max.Z - shown.Max.Z));
                if (past > oneMillimetre)
                    widerThanShown.Add(string.Format("{0} (id {1}): Revit's box {2:0} x {3:0} x {4:0} mm, what "
                        + "its geometry shows {5:0} x {6:0} x {7:0} mm",
                        element.Name, element.Id,
                        (box.Max.X - box.Min.X) * 304.8, (box.Max.Y - box.Min.Y) * 304.8,
                        (box.Max.Z - box.Min.Z) * 304.8,
                        (shown.Max.X - shown.Min.X) * 304.8, (shown.Max.Y - shown.Min.Y) * 304.8,
                        (shown.Max.Z - shown.Min.Z) * 304.8));
            }
        }
    }

    // Combined first, so it covers EVERY element and not just the listed ones.
    if (!haveCombined)
    {
        minX = box.Min.X; minY = box.Min.Y; minZ = box.Min.Z;
        maxX = box.Max.X; maxY = box.Max.Y; maxZ = box.Max.Z;
        haveCombined = true;
    }
    else
    {
        minX = Math.Min(minX, box.Min.X); minY = Math.Min(minY, box.Min.Y);
        minZ = Math.Min(minZ, box.Min.Z);
        maxX = Math.Max(maxX, box.Max.X); maxY = Math.Max(maxY, box.Max.Y);
        maxZ = Math.Max(maxZ, box.Max.Z);
    }

    if (listed < cap)
    {
        findings.Add(string.Format("{0} (id {1}): {2:0} x {3:0} x {4:0} mm, lowest corner at "
            + "({5:0}, {6:0}, {7:0}) mm",
            element.Name, element.Id,
            (box.Max.X - box.Min.X) * 304.8,
            (box.Max.Y - box.Min.Y) * 304.8,
            (box.Max.Z - box.Min.Z) * 304.8,
            box.Min.X * 304.8, box.Min.Y * 304.8, box.Min.Z * 304.8));
        listed++;
    }
}

if (measured > listed)
{
    findings.Add(string.Format("... {0} more measured but not listed - the COMBINED box below still "
        + "covers all {1}", measured - listed, measured));
}

if (haveCombined)
{
    combinedSizeMm = string.Format("{0:0} x {1:0} x {2:0} mm",
        (maxX - minX) * 304.8, (maxY - minY) * 304.8, (maxZ - minZ) * 304.8);

    findings.Add(string.Format("COMBINED across {0} element(s): {1}, from ({2:0}, {3:0}, {4:0}) to "
        + "({5:0}, {6:0}, {7:0}) mm. Aligned to project X and Y, so anything at an angle reads "
        + "larger than it is",
        measured, combinedSizeMm,
        minX * 304.8, minY * 304.8, minZ * 304.8,
        maxX * 304.8, maxY * 304.8, maxZ * 304.8));
}
else
{
    findings.Add("Nothing handed in has a bounding box, so there is no combined extent to report");
}

if (widerThanShown.Count > 0)
{
    findings.Add(string.Format("{0} of the {1} family instance(s) compared have a box LARGER than the geometry "
        + "they show - the family holds something that is not shown, as a body switched off with a Visible "
        + "parameter is. The sizes above and the COMBINED box are Revit's own boxes, the larger ones: {2}{3}",
        widerThanShown.Count, comparedInstances, string.Join("; ", widerThanShown.Take(20)),
        widerThanShown.Count > 20 ? string.Format("; and {0} more", widerThanShown.Count - 20) : ""));

    if (familyInstances > comparedInstances)
        findings.Add(string.Format("{0} more family instance(s) were not compared with what they show - "
            + "the first {1} are, and any whose geometry Revit does not give is left out",
            familyInstances - comparedInstances, compareAtMost));
}

if (noBoundingBox.Count > 0)
{
    findings.Add(string.Format("{0} element(s) have no bounding box and are NOT in the combined "
        + "figure above", noBoundingBox.Count));
}

findings.Insert(0, string.Format("{0} of {1} element(s) measured", measured, elements.Count));
