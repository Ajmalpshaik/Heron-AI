// NOT STANDALONE. Assumes `doc`, `elements` and `target` are in scope; leaves
// `moved`, `notMovable` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The target is internal FEET.
//
// EVERY OTHER TAG FRAGMENT HERE DECIDES THE POSITION ITSELF - arrange, stack,
// centre, park at the edges. Not one takes a position, which is why "move this
// tag" had no route at all.
//
// A TAG MOVES BY ITS HEAD. IndependentTag.TagHeadPosition is the head; moving
// the ELEMENT drags the whole tag including its leader anchor, which is a
// different result from the one somebody asking means.
//
// THE LEADER IS LEFT ALONE. What the tag points at does not change - only where
// its head sits. Re-anchoring while moving would quietly re-tag things.

var moved = new List<ElementId>();
var notMovable = new List<ElementId>();
var findings = new List<string>();

if (target == null)
{
    findings.Add("No target point was given");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No annotation was given, so there is nothing to move");
}
else
{
    var byHead = 0;
    var byLocation = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        // A TAG MOVES BY ITS HEAD - see the header.
        var tag = element as IndependentTag;
        if (tag != null)
        {
            try
            {
                tag.TagHeadPosition = target;
                moved.Add(element.Id);
                byHead++;
            }
            catch
            {
                notMovable.Add(element.Id);
            }
            continue;
        }

        var spot = element.Location as LocationPoint;
        if (spot == null)
        {
            notMovable.Add(element.Id);
            continue;
        }

        var shift = target - spot.Point;
        if (shift.IsZeroLength())
        {
            moved.Add(element.Id);
            byLocation++;
            continue;
        }

        try
        {
            ElementTransformUtils.MoveElement(doc, element.Id, shift);
            moved.Add(element.Id);
            byLocation++;
        }
        catch
        {
            notMovable.Add(element.Id);
        }
    }

    findings.Add(string.Format(
        "{0} moved - {1} by its tag head, {2} by its location point - and {3} "
        + "had neither. What each tag POINTS AT is unchanged",
        moved.Count, byHead, byLocation, notMovable.Count));
}
