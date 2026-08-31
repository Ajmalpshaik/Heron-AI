// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `tagTypeId`, `addLeader`
// and `alreadyTagged` are in scope; leaves `tagged`, `skipped` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SECOND TAG ON AN ALREADY-TAGGED ELEMENT IS THE FAILURE THIS GUARDS.
// Re-running a tagging pass is the normal way to pick up what was modelled
// since the last one. Without the skip, the second run stacks a duplicate tag
// on every element - and it lands exactly on top of the first, so the drawing
// looks correct until somebody drags a tag and finds another underneath. The
// list arrives as an input rather than being worked out here, because what
// counts as already tagged depends on which tag types the office cares about.
//
// THE TAG TYPE IS NOT CHECKED AGAINST THE CATEGORY, BECAUSE REVIT WILL NOT
// TELL US. Placing a duct tag on a pipe throws nothing and reports nothing -
// it places a tag that shows no text and prints blank. This fragment cannot
// see that and does not pretend to: the type is the caller's to get right,
// which is why it is a stated input and not a default.
//
// WHERE THE TAG GOES. At the host's own location - the midpoint of a curve, the
// point of a point-based element. Readable placement is a drafting judgement
// and a different job; putting every tag somewhere predictable is the honest
// starting point, and it is easier to move a tag that exists than to find one
// that was never placed.

var tagged = 0;
var skipped = new List<ElementId>();
var refused = new List<ElementId>();

var have = new HashSet<ElementId>(alreadyTagged ?? new List<ElementId>());

foreach (var element in elements)
{
    if (element == null) continue;

    if (have.Contains(element.Id))
    {
        skipped.Add(element.Id);
        continue;
    }

    // Where to put it, from the element's own geometry. An element with
    // neither a point nor a curve gives us nowhere to place a tag, and
    // inventing the origin would put every such tag on top of each other at
    // 0,0,0 - which reads on the drawing as a bug in Revit rather than in this.
    XYZ point = null;

    var atCurve = element.Location as LocationCurve;
    if (atCurve != null && atCurve.Curve != null)
    {
        point = atCurve.Curve.Evaluate(0.5, true);
    }
    else
    {
        var atPoint = element.Location as LocationPoint;
        if (atPoint != null) point = atPoint.Point;
    }

    if (point == null)
    {
        refused.Add(element.Id);
        continue;
    }

    try
    {
        var reference = new Reference(element);
        var tag = IndependentTag.Create(doc, tagTypeId, view.Id, reference,
                                        addLeader, TagOrientation.Horizontal,
                                        point);

        // A tag object came back, so something was placed. That is as far as
        // this can honestly go: whether the tag SHOWS anything depends on the
        // type matching the category, and Revit reports no difference between
        // a tag full of text and one that prints blank.
        if (tag != null) tagged++;
        else refused.Add(element.Id);
    }
    catch (Exception)
    {
        // Recorded and stepped over, so a batch of two hundred does not end at
        // the eleventh with nothing reported. Never treated as a success.
        refused.Add(element.Id);
    }
}
