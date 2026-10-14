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
// point of a point-based element - and, for an element Revit gives no location
// at all, the centre of its extent in this view. Readable placement is a
// drafting judgement and a different job; putting every tag somewhere
// predictable is the honest starting point, and it is easier to move a tag that
// exists than to find one that was never placed.
//
// THE EXTENT IS FOR A DOOR THAT IS A CURTAIN-WALL PANEL (version 2, 5b-454).
// Measured on Revit 2020.2, Project1, 2026-10-13: such a door has `Location`
// null where a wall-hosted door has a point, so version 1 refused every one of
// them. IndependentTag.Create at the centre of its extent in the plan made a
// tag there. TAG_ELEMENTS_IN_VIEW falls back the same way.

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

    // Where to put it, from the element's own geometry. An element with no
    // point, no curve and no extent gives us nowhere to place a tag, and
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

    // No location: the centre of its extent - in this view first, because that
    // is where the tag is drawn, and the model's extent when the view gives
    // none. An extent that cannot be read leaves the point empty, and the
    // element is refused below exactly as before.
    if (point == null)
    {
        BoundingBoxXYZ extent = null;
        try { extent = element.get_BoundingBox(view) ?? element.get_BoundingBox(null); }
        catch (Exception) { extent = null; }
        if (extent != null) point = (extent.Min + extent.Max) * 0.5;
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
