// NOT STANDALONE. Assumes `doc`, `elements`, `reference`, `alignX`, `alignY`
// and `alignZ` are in scope; leaves `aligned`, `blocked` and `unverified`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Coordinates are internal FEET.
//
// WHAT "ITS POSITION" MEANS, AND WHY IT MATTERS MOST FOR A DUCT.
//
// A placed family has a point. A duct or pipe has a curve, and the point used
// here is its MIDPOINT - so two runs of different lengths aligned in X share a
// centre and their ENDS DO NOT LINE UP. That is right for equipment and wrong
// for somebody who meant "make the ends meet", and the difference does not show
// until it is looked at.
//
// FOR MEP LEVELLED BY TOP OR BOTTOM, THIS IS THE WRONG FRAGMENT.
// ALIGN_MEP_ELEVATION reads each run's own size so a 600 duct and a 100 pipe
// can share a soffit. Aligning them here in Z would share their centrelines and
// leave the soffits 250 apart - which is the thing a coordinated corridor needs
// to be equal.
//
// THE MOVE IS VERIFIED, for the reason every move here is: Revit returns
// normally and moves nothing for a group member.

Func<Element, XYZ> positionOf = e =>
{
    if (e == null) return null;

    var point = e.Location as LocationPoint;
    if (point != null) return point.Point;

    var curve = e.Location as LocationCurve;
    if (curve != null && curve.Curve != null) return curve.Curve.Evaluate(0.5, true);

    try
    {
        var box = e.get_BoundingBox(null);
        if (box != null) return (box.Min + box.Max) * 0.5;
    }
    catch
    {
        // No geometry in the current view. Reported, never assumed.
    }
    return null;
};

var aligned = 0;
var blocked = new List<ElementId>();
var unverified = new List<ElementId>();

var tolerance = 1.0 / 304.8;
var target = positionOf(reference);

// With no reference position there is nothing to align TO, and moving
// everything to an invented coordinate would be worse than doing nothing.
if (target != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (element.Id == reference.Id) continue;      // the reference stays put

        var at = positionOf(element);
        if (at == null) { unverified.Add(element.Id); continue; }

        var shift = new XYZ(alignX ? target.X - at.X : 0,
                            alignY ? target.Y - at.Y : 0,
                            alignZ ? target.Z - at.Z : 0);

        // Already in line. Counted as aligned, because it is where it should be
        // - and a zero move would otherwise read as blocked below.
        if (shift.GetLength() < tolerance) { aligned++; continue; }

        ElementTransformUtils.MoveElement(doc, element.Id, shift);

        var now = positionOf(element);
        if (now == null || now.DistanceTo(at) < tolerance) blocked.Add(element.Id);
        else aligned++;
    }
}
