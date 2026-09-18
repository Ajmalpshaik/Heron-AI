// NOT STANDALONE. Assumes `doc`, `elements` and `target` are in scope; leaves
// `moved`, `notAtPoint`, `noLocationPoint` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The target is internal FEET,
// converted at the edge (D-20) - converting again here divides by 304.8.
//
// MOVE_ELEMENTS SHIFTS BY AN OFFSET. This one computes the offset from where the
// element actually is, which is the whole difference and is why it belongs in
// one call: the subtraction cannot go stale between reading the position and
// using it.
//
// A CURVE-DRIVEN ELEMENT HAS NO SINGLE ANSWER. Start, end and middle of a wall
// are three different places, so those are counted rather than moved to a
// number somebody invented.

var moved = new List<ElementId>();
var notAtPoint = new List<ElementId>();
var noLocationPoint = new List<ElementId>();
var findings = new List<string>();

if (target == null)
{
    findings.Add("No target point was given");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given, so there is nothing to move");
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        var spot = element.Location as LocationPoint;
        if (spot == null)
        {
            noLocationPoint.Add(element.Id);
            continue;
        }

        var from = spot.Point;
        var shift = target - from;

        if (shift.IsZeroLength())
        {
            // Already there. Counted as moved rather than as a failure - the
            // element IS at the point asked for, which is what was wanted.
            moved.Add(element.Id);
            continue;
        }

        try
        {
            ElementTransformUtils.MoveElement(doc, element.Id, shift);
        }
        catch
        {
            notAtPoint.Add(element.Id);
            continue;
        }

        var after = element.Location as LocationPoint;
        if (after != null && after.Point.IsAlmostEqualTo(target)) moved.Add(element.Id);
        else notAtPoint.Add(element.Id);
    }

    findings.Add(string.Format(
        "{0} are now at the point, {1} accepted the move and did not arrive "
        + "(a constraint holds them), {2} are positioned by a curve rather than "
        + "a point and have no single answer to this",
        moved.Count, notAtPoint.Count, noLocationPoint.Count));
}
