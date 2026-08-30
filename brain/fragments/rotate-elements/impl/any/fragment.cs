// NOT STANDALONE. Assumes `doc`, `elements`, `angleRadians` and `pivot` are in
// scope; leaves `rotated`, `blocked` and `unverified` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The angle is in RADIANS.
//
// `pivot` MAY BE NULL, and that is the common case: each element then turns
// about its own position. A given pivot turns the whole set about one point,
// which is a different job - "spin that damper round" against "swing this row
// about the corner" - and the two must not be conflated by defaulting.
//
// THE SAME SILENT NO-OP AS MOVING, MEASURED THE SAME WAY.
//
// Revit's rotate call returns normally and rotates NOTHING for a pinned element
// or a member of a group. Five grouped air terminals stayed at 0.00 degrees
// while code that trusted the call reported "rotated 5, skipped 0". Only the
// model can confirm a rotation.
//
// The signature below is not a position - it is whatever CHANGES when a thing
// turns, which differs by how the element is placed:
//
//   a placed family   its own Rotation angle
//   a duct or pipe    an endpoint of its curve, which swings even though the
//                     midpoint may not move at all
//   anything else     the corners of its bounding box
//
// The middle one is the one worth knowing: a run rotated about its own centre
// has the SAME midpoint afterwards, so a position probe would call every
// rotation blocked. Rotation needs its own evidence, not the move fragment's.

Func<Element, string> orientationOf = e =>
{
    if (e == null) return null;

    var placed = e.Location as LocationPoint;
    if (placed != null) return "R" + placed.Rotation.ToString("F6");

    var line = e.Location as LocationCurve;
    if (line != null && line.Curve != null)
    {
        var end = line.Curve.GetEndPoint(0);
        return "P" + end.X.ToString("F6") + "," + end.Y.ToString("F6");
    }

    try
    {
        var box = e.get_BoundingBox(null);
        if (box != null)
            return "B" + box.Min.X.ToString("F6") + "," + box.Min.Y.ToString("F6") +
                   "," + box.Max.X.ToString("F6") + "," + box.Max.Y.ToString("F6");
    }
    catch
    {
        // No geometry in the current view. Reported as unverified, never as turned.
    }
    return null;
};

var rotated = 0;
var blocked = new List<ElementId>();
var unverified = new List<ElementId>();

// A whole turn changes nothing, and neither does no turn at all. Either is a
// legitimate request and neither must be reported as blocked.
var full = 2.0 * Math.PI;
var normalised = Math.Abs(angleRadians % full);
var askedForNothing = normalised < 1e-9 || Math.Abs(normalised - full) < 1e-9;

var before = new Dictionary<ElementId, string>();
foreach (var element in elements)
{
    var was = orientationOf(element);
    if (was != null) before[element.Id] = was;
}

var attempted = new List<Element>();
foreach (var element in elements)
{
    var about = pivot;
    if (about == null)
    {
        var placed = element.Location as LocationPoint;
        if (placed == null)
        {
            // Nothing to turn it about, and this fragment will not invent one.
            unverified.Add(element.Id);
            continue;
        }
        about = placed.Point;
    }

    // A vertical axis through the pivot. Revit needs a bounded line, and its
    // length is irrelevant to the rotation.
    var axis = Line.CreateBound(about, about + XYZ.BasisZ);

    try
    {
        ElementTransformUtils.RotateElement(doc, element.Id, axis, angleRadians);
        attempted.Add(element);
    }
    catch
    {
        blocked.Add(element.Id);
    }
}

doc.Regenerate();

foreach (var element in attempted)
{
    if (askedForNothing) { rotated++; continue; }

    string was;
    var now = orientationOf(element);
    if (now == null || !before.TryGetValue(element.Id, out was))
    {
        unverified.Add(element.Id);
        continue;
    }

    if (now == was) blocked.Add(element.Id);
    else rotated++;
}
