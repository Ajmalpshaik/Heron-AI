// NOT STANDALONE. Assumes `doc`, `elements`, `axis` and `angleRadians` are in
// scope; leaves `rotated`, `didNotMove`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The axis is in internal FEET and
// the angle is in RADIANS - both converted at the edge (D-20). Converting again
// here would rotate by a factor of 57.
//
// ROTATE_ELEMENTS IS THE VERTICAL AXIS AND SAYS SO. This one takes the axis as a
// LINE, because "rotate about X" is meaningless until somebody says where that
// line is: rotating two elements about their own centres and about a shared line
// put them in different places.
//
// IT CHECKS RATHER THAN ASSUMING. A pinned element is refused outright; a
// constrained one is accepted and does not move, with no error. Reading the
// position before and after is the only way to tell those apart.

var rotated = new List<ElementId>();
var didNotMove = new List<ElementId>();
var refused = new List<ElementId>();
var findings = new List<string>();

if (axis == null)
{
    findings.Add("No axis was given. An axis is a LINE in space - naming X or Y "
        + "does not say where that line is, and two elements turned about "
        + "different lines end up in different places");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given, so there is nothing to turn");
}
else if (Math.Abs(angleRadians) < 1e-9)
{
    findings.Add("The angle is zero, so nothing would move. Nothing was "
        + "attempted, which is not the same as nothing being turnable");
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        // WHERE IT IS NOW, so "it did not move" can be told from "it moved".
        XYZ before = null;
        var locationBefore = element.Location as LocationPoint;
        if (locationBefore != null) before = locationBefore.Point;

        BoundingBoxXYZ boxBefore = null;
        if (before == null) boxBefore = element.get_BoundingBox(null);

        try
        {
            ElementTransformUtils.RotateElement(doc, element.Id, axis, angleRadians);
        }
        catch
        {
            refused.Add(element.Id);
            continue;
        }

        var moved = false;
        var locationAfter = element.Location as LocationPoint;
        if (before != null && locationAfter != null)
        {
            moved = !locationAfter.Point.IsAlmostEqualTo(before);
        }
        else if (boxBefore != null)
        {
            var boxAfter = element.get_BoundingBox(null);
            if (boxAfter != null)
            {
                moved = !boxAfter.Min.IsAlmostEqualTo(boxBefore.Min)
                     || !boxAfter.Max.IsAlmostEqualTo(boxBefore.Max);
            }
            else
            {
                // Nothing to compare - counted as turned rather than invented
                // either way, and said out loud below.
                moved = true;
            }
        }
        else
        {
            moved = true;
        }

        if (moved) rotated.Add(element.Id);
        else didNotMove.Add(element.Id);
    }

    findings.Add(string.Format(
        "{0} turned, {1} accepted the call and did not move (pinned, or held by "
        + "a constraint), {2} Revit would not turn at all",
        rotated.Count, didNotMove.Count, refused.Count));
}
