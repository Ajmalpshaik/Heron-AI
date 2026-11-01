// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `stackAt`, `gapMm`,
// `layout` and `marginMm` are in scope, and leaves `stacked`, `noHead`,
// `notTags`, `refused`, `perSide`, `usedTagExtent` and `findings` behind.
//
// VERSION 2 (2026-11-01) - ARRANGE_TAGS_TO_VIEW_EDGES MERGED IN, on the
// owner's instruction ("make sure there is no duplicate tools"). Both gather
// tags into tidy columns, each still leadered to its own element. `layout`
// `edges` parks them in TWO columns just outside the left and right edges of
// the view - the "I cannot read the drawing for tags" fix - where `point`, the
// default, stacks one column at `stackAt` as version 1 did. What that card
// had that this lacked:
//
//   EACH TAG GOES TO THE SIDE ITS OWN ELEMENT IS ON. One edge for everything
//   doubles the leader lengths and drags every leader across the drawing.
//   Which half of the view the element sits in decides - its element's
//   middle, the tag's head when the element cannot be found.
//
//   THE ORDER DOWN EACH COLUMN FOLLOWS THE ELEMENTS UP THE VIEW, highest
//   first, so the leaders fan out without crossing - this card's own rule.
//
//   THE MARGIN, LIKE THE GAP, IS PAPER MILLIMETRES, multiplied by the view
//   scale: a model-distance margin puts the tags off the paper at 1:200 and
//   inside the building at 1:20. `marginMm` must be given for `edges`.
//
//   THE CROP IS THE EDGE WHEN THERE IS ONE. An uncropped view has no edges to
//   park against, so the extent of the tags' own heads stands in and the
//   columns sit outside it - and `usedTagExtent` says so, because the result
//   looks different and nothing else would explain why. `perSide` says how
//   many went left and right.
//
//   A STACK POINT LEFT OUT reads 0,0,0 - the project origin, never a place a
//   column of tags belongs - so on the `point` road it is refused, in
//   `findings`, rather than every tag being sent there.
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
var perSide = new Dictionary<string, int>();
var usedTagExtent = false;
var findings = new List<string>();

// VERSION 2: which road. `point` (left out) is version 1's one column.
var layoutWord = (layout ?? "").Trim().ToLowerInvariant();
if (layoutWord.Length == 0) layoutWord = "point";
var edges = layoutWord == "edges" || layoutWord == "sides" || layoutWord == "edge";
var layoutProblem = "";
double edgeMarginMm = 0;
if (!edges && layoutWord != "point")
    layoutProblem = "\"" + layout + "\" is not a layout - point, one column at stackAt, or edges, two columns at the "
        + "view's left and right edges. Nothing was moved.";
else if (edges && !double.TryParse((marginMm ?? "").Trim(), System.Globalization.NumberStyles.Float,
             System.Globalization.CultureInfo.InvariantCulture, out edgeMarginMm))
    layoutProblem = "edges needs marginMm - the paper millimetres between the view's edge and the column, such as "
        + "10. Nothing was moved.";
else if (!edges && (stackAt == null || stackAt.IsZeroLength()))
    layoutProblem = "No stack point was given - left out it reads 0,0,0, the project origin, and a column of tags "
        + "does not belong there. Give stackAt, or layout edges. Nothing was moved.";
if (edges)
{
    perSide["left"] = 0;
    perSide["right"] = 0;
}

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

if (layoutProblem.Length > 0)
{
    findings.Add(layoutProblem);
}
else foreach (var element in elements)
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

// VERSION 2 - THE EDGES ROAD, from ARRANGE_TAGS_TO_VIEW_EDGES. Each tag to
// the side its element is on, the columns ordered by the elements up the view.
if (edges && layoutProblem.Length == 0)
{
    var margin = edgeMarginMm * MmToFeet * scale;
    var right = view.RightDirection;
    var up = view.UpDirection;

    // The edges: the crop when the view has one, otherwise the tags' own extent.
    double leftEdge = double.MaxValue, rightEdge = double.MinValue;
    var cropped = false;
    try { cropped = view.CropBoxActive; } catch { }
    if (cropped)
    {
        try
        {
            var box = view.CropBox;
            var lower = box.Transform.OfPoint(box.Min);
            var upper = box.Transform.OfPoint(box.Max);
            double a = lower.DotProduct(right), b = upper.DotProduct(right);
            leftEdge = Math.Min(a, b);
            rightEdge = Math.Max(a, b);
        }
        catch { cropped = false; }
    }
    if (!cropped)
    {
        usedTagExtent = true;
        foreach (var entry in movable)
        {
            var head = headOf(entry.Value);
            if (head == null) continue;
            var along = head.DotProduct(right);
            if (along < leftEdge) leftEdge = along;
            if (along > rightEdge) rightEdge = along;
        }
    }

    if (leftEdge == double.MaxValue || rightEdge == double.MinValue)
    {
        // Nothing to measure against and nothing to park.
        foreach (var entry in movable) refused.Add(entry.Value.Id);
    }
    else
    {
        var middle = (leftEdge + rightEdge) / 2.0;
        var goingLeft = new List<KeyValuePair<double, Element>>();
        var goingRight = new List<KeyValuePair<double, Element>>();
        foreach (var entry in movable)
        {
            var where = targetOf(entry.Value) ?? headOf(entry.Value);
            if (where.DotProduct(right) <= middle) goingLeft.Add(new KeyValuePair<double, Element>(where.DotProduct(up), entry.Value));
            else goingRight.Add(new KeyValuePair<double, Element>(where.DotProduct(up), entry.Value));
        }

        Action<List<KeyValuePair<double, Element>>, double, string> park = (column, edge, side) =>
        {
            // Highest first, so the column reads down the sheet the way the
            // elements sit up the view and the leaders do not cross.
            var top = column.Count > 0 ? column.Max(e => e.Key) : 0.0;
            var place = 0;
            foreach (var entry in column.OrderByDescending(e => e.Key))
            {
                var tag = entry.Value;
                var head = headOf(tag);
                if (head == null) { noHead.Add(tag.Id); continue; }
                var goal = head + right * (edge - head.DotProduct(right)) + up * (top - gap * place - head.DotProduct(up));

                // A free leader end travels with the head, so it is put back.
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
                    if (independent != null) independent.TagHeadPosition = goal;
                    else ((SpatialElementTag)tag).TagHeadPosition = goal;
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

                // Read back: a head that did not move is not a parked tag.
                var now = headOf(tag);
                if (now == null || !now.IsAlmostEqualTo(goal)) { refused.Add(tag.Id); continue; }

                stacked++;
                perSide[side] = perSide[side] + 1;
                place++;
            }
        };

        park(goingLeft, leftEdge - margin, "left");
        park(goingRight, rightEdge + margin, "right");
        findings.Add(string.Format("{0} tag(s) parked - {1} at the left edge, {2} at the right - against {3}",
            stacked, perSide["left"], perSide["right"],
            usedTagExtent ? "the tags' own extent, because the view is not cropped" : "the view's crop"));
    }
    movable.Clear();
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
