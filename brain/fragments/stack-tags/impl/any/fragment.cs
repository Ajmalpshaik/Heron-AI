// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `stackAt` and `gapMm` are
// in scope, and leaves `stacked`, `noHead`, `notTags` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// NEAREST ELEMENT FIRST, OR THE LEADERS CROSS.
//
// Sorting by whatever order the set arrived in produces a stack whose leaders
// weave through each other - the same tags, the same places, and a drawing
// that looks like the tool is broken. Sorted by where each tag's own element
// sits along the stack axis, the leaders fan out cleanly.
//
// THE GAP IS PAPER MILLIMETRES AND MUST BE SCALED.
//
// 5 mm between tags is 500 model mm at 1:100 and 250 at 1:50. Every clearance
// in tag work is a paper number, and using it as a model distance is the
// commonest mistake in the subject: the stack comes out unreadable or off the
// sheet.
//
// TAG CLASSES SHARE NO BASE THAT CARRIES A HEAD POSITION.
//
// An independent tag and a room, space or area tag each declare their own.
// Casting to the independent kind silently skips every room tag in the set -
// so both are handled explicitly, which the compiler can check and a reflected
// property name cannot.

const double MmToFeet = 1.0 / 304.8;

int stacked = 0;
var noHead = new List<ElementId>();
var notTags = new List<ElementId>();
var refused = new List<ElementId>();

int scale = 1;
try { scale = Math.Max(1, view.Scale); } catch { }

double gap = gapMm * MmToFeet * scale;
var down = view.UpDirection.Negate();

Func<Element, XYZ> headOf = element =>
{
    var independent = element as IndependentTag;
    if (independent != null) { try { return independent.TagHeadPosition; } catch { return null; } }
    var spatial = element as SpatialElementTag;
    if (spatial != null) { try { return spatial.TagHeadPosition; } catch { return null; } }
    return null;
};

// What each tag points AT, used only for the ordering. A tag whose target
// cannot be found keeps its own head position as its key, which puts it near
// where it already is rather than at an arbitrary end of the stack.
Func<Element, XYZ> targetOf = element =>
{
    var independent = element as IndependentTag;
    if (independent != null)
    {
        try
        {
#if REVIT2020 || REVIT2021
            var pair = independent.TaggedElementId;
            if (pair != null)
            {
                var id = pair.HostElementId;
                var target = id != ElementId.InvalidElementId ? doc.GetElement(id) : null;
                if (target != null)
                {
                    var box = target.get_BoundingBox(view);
                    if (box != null) return (box.Min + box.Max) / 2.0;
                }
            }
#else
            foreach (var id in independent.GetTaggedLocalElementIds())
            {
                var target = doc.GetElement(id);
                if (target == null) continue;
                var box = target.get_BoundingBox(view);
                if (box != null) return (box.Min + box.Max) / 2.0;
            }
#endif
        }
        catch { }
    }

    var spatial = element as SpatialElementTag;
    if (spatial != null)
    {
        try
        {
            var room = spatial.Location as LocationPoint;
            if (room != null) return room.Point;
        }
        catch { }
    }

    return headOf(element);
};

var movable = new List<KeyValuePair<double, Element>>();

foreach (var element in elements)
{
    if (element == null) continue;

    bool isTag = element is IndependentTag || element is SpatialElementTag;
    if (!isTag) { notTags.Add(element.Id); continue; }

    var head = headOf(element);
    if (head == null) { noHead.Add(element.Id); continue; }

    var target = targetOf(element) ?? head;

    // The key: how far along the stack axis this tag's own element sits.
    movable.Add(new KeyValuePair<double, Element>(target.DotProduct(down), element));
}

int index = 0;
foreach (var entry in movable.OrderBy(m => m.Key))
{
    var tag = entry.Value;
    var target = stackAt + down * (gap * index);

    // A free leader end travels with the head, so it is put back afterwards.
    XYZ freeEnd = null;
    var independent = tag as IndependentTag;
    if (independent != null)
    {
        try
        {
            if (independent.HasLeader && independent.LeaderEndCondition == LeaderEndCondition.Free)
            {
#if REVIT2020 || REVIT2021
                freeEnd = independent.LeaderEnd;
#else
                var references = independent.GetTaggedReferences();
                if (references != null && references.Count > 0)
                    freeEnd = independent.GetLeaderEnd(references[0]);
#endif
            }
        }
        catch { }
    }

    try
    {
        if (independent != null) independent.TagHeadPosition = target;
        else ((SpatialElementTag)tag).TagHeadPosition = target;
    }
    catch { refused.Add(tag.Id); continue; }

    if (freeEnd != null && independent != null)
    {
#if REVIT2020 || REVIT2021
        try { independent.LeaderEnd = freeEnd; } catch { }
#else
        try
        {
            var references = independent.GetTaggedReferences();
            if (references != null && references.Count > 0)
                independent.SetLeaderEnd(references[0], freeEnd);
        }
        catch { }
#endif
    }

    // Read back: a head that did not move is not a stacked tag.
    var now = headOf(tag);
    if (now == null || !now.IsAlmostEqualTo(target)) { refused.Add(tag.Id); continue; }

    stacked++;
    index++;
}
