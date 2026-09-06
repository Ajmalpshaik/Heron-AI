// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `marginMm` and `gapMm`
// are in scope, and leaves `parked`, `perSide`, `usedTagExtent`, `notTags` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// EACH TAG GOES TO THE SIDE ITS OWN ELEMENT IS ON.
//
// One edge for everything doubles the leader lengths and drags every leader
// across the drawing - which is the thing being cleared in the first place.
//
// THE COLUMN ORDER FOLLOWS THE ELEMENTS UP THE VIEW.
//
// Placed in any other order, the same tags in the same places produce crossing
// leaders. This is the same rule the stacking fragment uses and it is the
// difference between a tidy edge and a plate of spaghetti.
//
// PAPER MILLIMETRES, SCALED.
//
// The margin and the gap are judged on the sheet. A model-distance margin puts
// the tags off the paper at 1:200 and inside the building at 1:20.
//
// THE CROP IS THE EDGE, WHEN THERE IS ONE.
//
// An uncropped view has no edges to park against, so the extent of the tagged
// elements is used and the columns sit outside it. Same intent, different
// number - and the report says which was used, because the result looks
// different and nothing else would explain why.

const double MmToFeet = 1.0 / 304.8;

int parked = 0;
var perSide = new Dictionary<string, int>();
perSide["left"] = 0;
perSide["right"] = 0;
bool usedTagExtent = false;
var notTags = new List<ElementId>();
var refused = new List<ElementId>();

int scale = 1;
try { scale = Math.Max(1, view.Scale); } catch { }

double margin = marginMm * MmToFeet * scale;
double gap = gapMm * MmToFeet * scale;

var right = view.RightDirection;
var up = view.UpDirection;

Func<Element, XYZ> headOf = element =>
{
    var independent = element as IndependentTag;
    if (independent != null) { try { return independent.TagHeadPosition; } catch { return null; } }
    var spatial = element as SpatialElementTag;
    if (spatial != null) { try { return spatial.TagHeadPosition; } catch { return null; } }
    return null;
};

var tags = new List<Element>();
foreach (var element in elements)
{
    if (element == null) continue;
    if (element is IndependentTag || element is SpatialElementTag) tags.Add(element);
    else notTags.Add(element.Id);
}

// The edges. The crop when the view has one, otherwise the extent of the tags
// themselves.
double leftEdge = double.MaxValue, rightEdge = double.MinValue;

bool cropped = false;
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
    foreach (var tag in tags)
    {
        var head = headOf(tag);
        if (head == null) continue;
        double along = head.DotProduct(right);
        if (along < leftEdge) leftEdge = along;
        if (along > rightEdge) rightEdge = along;
    }
}

if (leftEdge == double.MaxValue || rightEdge == double.MinValue)
{
    // Nothing to measure against and nothing to park.
    foreach (var tag in tags) refused.Add(tag.Id);
}
else
{
    double middle = (leftEdge + rightEdge) / 2.0;

    var goingLeft = new List<KeyValuePair<double, Element>>();
    var goingRight = new List<KeyValuePair<double, Element>>();

    foreach (var tag in tags)
    {
        var head = headOf(tag);
        if (head == null) { refused.Add(tag.Id); continue; }

        // Which side, and where up the view - both from the tag's own head,
        // which is where its element is until this fragment moves it.
        double along = head.DotProduct(right);
        double height = head.DotProduct(up);

        if (along <= middle) goingLeft.Add(new KeyValuePair<double, Element>(height, tag));
        else goingRight.Add(new KeyValuePair<double, Element>(height, tag));
    }

    Action<List<KeyValuePair<double, Element>>, double, string> park =
        (column, edge, side) =>
        {
            // Highest first, so the column reads down the sheet the way the
            // elements sit up the view and the leaders do not cross.
            int index = 0;
            foreach (var entry in column.OrderByDescending(e => e.Key))
            {
                var tag = entry.Value;
                var head = headOf(tag);
                if (head == null) { refused.Add(tag.Id); continue; }

                double top = column.Count > 0 ? column.Max(e => e.Key) : head.DotProduct(up);
                var target = head
                    + right * (edge - head.DotProduct(right))
                    + up * (top - gap * index - head.DotProduct(up));

                var independent = tag as IndependentTag;

                XYZ freeEnd = null;
                if (independent != null)
                {
                    try
                    {
                        if (independent.HasLeader &&
                            independent.LeaderEndCondition == LeaderEndCondition.Free)
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

                var now = headOf(tag);
                if (now == null || !now.IsAlmostEqualTo(target)) { refused.Add(tag.Id); continue; }

                parked++;
                perSide[side] = perSide[side] + 1;
                index++;
            }
        };

    park(goingLeft, leftEdge - margin, "left");
    park(goingRight, rightEdge + margin, "right");
}
