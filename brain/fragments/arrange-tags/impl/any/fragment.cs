// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `minGapMm`, `maxPasses`,
// `fallbackTagWidthMm`, `fallbackTagHeightMm` and `roomTagsStayPut` are in
// scope, and leaves `moved`, `measuredFrom`, `bothHadToMove`,
// `stillOverlapping`, `notTags`, `refused` and `keptInPlace` behind.
//
// VERSION 2 (2026-11-16) - ROOM, SPACE AND AREA TAGS TOO (row 5b-1732, issue
// #854 item c, villa 2 on Revit 2024). Version 1 kept an element only as an
// IndependentTag, so a room tag - a SpatialElementTag - went to `notTags`,
// was never moved and was not even an obstacle: the commonest clash on a
// plan, a door tag over a room tag, could not be pushed apart. A
// SpatialElementTag is now arranged by its TagHeadPosition like the rest; its
// leader, if it has one, is not written to. With `roomTagsStayPut`
// true, room, space and area tags stay exactly where they are - counted as
// fixed obstacles the other tags move away from, in both phases, and named in
// `keptInPlace`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One tidy-up, one undo.
//
// THE BOUNDING BOX OF A LEADERED TAG IS NOT THE SIZE OF THE TAG.
//
// It wraps the head AND the leader line. On a congested plan that box is
// metres across, and spacing worked out from it throws tags off the sheet. So
// the size is measured from LEADERLESS tags only and the median is used for
// the rest. When every tag in the set has a leader there is nothing to measure
// from, and the caller's stated size is used - which is a number somebody
// chose, rather than a measured one that is silently wrong.
//
// WORK IN THE VIEW'S OWN PLANE.
//
// Tags are annotation: they live on the view's plane, and a separation vector
// computed in model space can push one out of it. Every position is reduced to
// two numbers - along the view's right and up directions - and put back the
// same way, so nothing moves in the direction the reader cannot see.
//
// A BENT LEADER STAYS PUT, AND THAT PREFERENCE IS NOT UNCONDITIONAL.
//
// An elbow means the tag was already threaded past something. Moving it
// reopens a solved problem, so when one tag of a clashing pair is straight,
// that one takes the whole separation. A tag boxed in by bent neighbours
// cannot escape alone, though, which is why the first phase has a budget and
// the second drops the preference for whatever is still clashing - and names
// it.
//
// A FREE LEADER END TRAVELS WITH THE HEAD.
//
// An attached end follows the element it points at and needs nothing. A free
// one is its own point, and moving the head drags the arrow off the thing it
// was pointing at - across the whole drawing, quietly. Free ends are read
// before and written back after.

const double MmToFeet = 1.0 / 304.8;

int moved = 0;
int measuredFrom = 0;
var bothHadToMove = new List<ElementId>();
var stillOverlapping = new List<ElementId>();
var notTags = new List<ElementId>();
var refused = new List<ElementId>();
// VERSION 2 (row 5b-1732): room, space and area tags held still on request.
var keptInPlace = new List<ElementId>();

double gap = minGapMm * MmToFeet;

var right = view.RightDirection;
var up = view.UpDirection;

// VERSION 2: an IndependentTag OR a room, space or area tag.
var tags = new List<Element>();
foreach (var element in elements)
{
    if (element == null) continue;
    if (element is IndependentTag || element is SpatialElementTag) tags.Add(element);
    else notTags.Add(element.Id);
}

Func<Element, XYZ> headOf = element =>
{
    var independent = element as IndependentTag;
    if (independent != null) { try { return independent.TagHeadPosition; } catch { return null; } }
    var spatial = element as SpatialElementTag;
    if (spatial != null) { try { return spatial.TagHeadPosition; } catch { return null; } }
    return null;
};
var fixedTag = new Dictionary<ElementId, bool>();

// ---- what each tag is, and where it is ----
var atX = new Dictionary<ElementId, double>();   // along the view's right
var atY = new Dictionary<ElementId, double>();   // along the view's up
var heads = new Dictionary<ElementId, XYZ>();
var straight = new Dictionary<ElementId, bool>();
var freeEnds = new Dictionary<ElementId, XYZ>();

var measuredWidths = new List<double>();
var measuredHeights = new List<double>();

foreach (var anyTag in tags)
{
    XYZ head = headOf(anyTag);
    if (head == null) { refused.Add(anyTag.Id); continue; }

    heads[anyTag.Id] = head;
    atX[anyTag.Id] = head.DotProduct(right);
    atY[anyTag.Id] = head.DotProduct(up);

    // VERSION 2: a room, space or area tag - straight, and held still when
    // asked. Only a leaderless one is measured, as for the rest.
    var spatialTag = anyTag as SpatialElementTag;
    if (spatialTag != null)
    {
        fixedTag[anyTag.Id] = roomTagsStayPut;
        if (roomTagsStayPut) keptInPlace.Add(anyTag.Id);
        straight[anyTag.Id] = true;
        bool spatialLeader = false;
        try { spatialLeader = spatialTag.HasLeader; } catch { }
        if (!spatialLeader)
        {
            try
            {
                var spatialBox = spatialTag.get_BoundingBox(view);
                if (spatialBox != null)
                {
                    var spatialSize = spatialBox.Max - spatialBox.Min;
                    double spatialWidth = Math.Abs(spatialSize.DotProduct(right));
                    double spatialHeight = Math.Abs(spatialSize.DotProduct(up));
                    if (spatialWidth > 0 && spatialHeight > 0)
                    {
                        measuredWidths.Add(spatialWidth);
                        measuredHeights.Add(spatialHeight);
                    }
                }
            }
            catch { }
        }
        continue;
    }
    fixedTag[anyTag.Id] = false;
    var tag = (IndependentTag)anyTag;

    bool hasLeader = false;
    try { hasLeader = tag.HasLeader; } catch { }

    // Straight or bent, read fresh from the elbow. No elbow - or no leader at
    // all - counts as straight, which is the kind that is allowed to move.
    bool bent = false;
    XYZ freeEnd = null;

    if (hasLeader)
    {
        bool endIsFree = false;
        try { endIsFree = tag.LeaderEndCondition == LeaderEndCondition.Free; } catch { }

#if REVIT2020 || REVIT2021
        XYZ elbow = null, end = null;
        try { elbow = tag.LeaderElbow; } catch { }
        try { end = tag.LeaderEnd; } catch { }
#else
        XYZ elbow = null, end = null;
        Reference taggedReference = null;
        try
        {
            var references = tag.GetTaggedReferences();
            if (references != null && references.Count > 0) taggedReference = references[0];
        }
        catch { }
        if (taggedReference != null)
        {
            try { elbow = tag.GetLeaderElbow(taggedReference); } catch { }
            try { end = tag.GetLeaderEnd(taggedReference); } catch { }
        }
#endif

        if (elbow != null && end != null)
        {
            // Bent means the elbow is off the straight line from head to end.
            var line = end - head;
            var toElbow = elbow - head;
            double length = line.GetLength();
            if (length > 1e-9)
            {
                double away = line.CrossProduct(toElbow).GetLength() / length;
                bent = away > 0.01;   // a hundredth of a foot, about 3 mm
            }
        }

        if (endIsFree && end != null) freeEnd = end;
    }

    straight[tag.Id] = !bent;
    if (freeEnd != null) freeEnds[tag.Id] = freeEnd;

    // Only a LEADERLESS tag can be measured - see the note above.
    if (!hasLeader)
    {
        try
        {
            var box = tag.get_BoundingBox(view);
            if (box != null)
            {
                var size = box.Max - box.Min;
                double width = Math.Abs(size.DotProduct(right));
                double height = Math.Abs(size.DotProduct(up));
                if (width > 0 && height > 0)
                {
                    measuredWidths.Add(width);
                    measuredHeights.Add(height);
                }
            }
        }
        catch { }
    }
}

measuredFrom = measuredWidths.Count;

Func<List<double>, double> medianOf = numbers =>
{
    var sorted = numbers.OrderBy(n => n).ToList();
    return sorted[sorted.Count / 2];
};

double halfWidth = measuredFrom > 0
    ? medianOf(measuredWidths) / 2.0
    : fallbackTagWidthMm * MmToFeet / 2.0;
double halfHeight = measuredFrom > 0
    ? medianOf(measuredHeights) / 2.0
    : fallbackTagHeightMm * MmToFeet / 2.0;

double needX = 2 * halfWidth + gap;
double needY = 2 * halfHeight + gap;

// ---- separate ----
var working = heads.Keys.ToList();

Func<bool, int> separate = allowBothToMove =>
{
    int pairsTouched = 0;

    for (int pass = 0; pass < Math.Max(1, maxPasses); pass++)
    {
        bool anyMovedThisPass = false;

        for (int i = 0; i < working.Count; i++)
        {
            for (int j = i + 1; j < working.Count; j++)
            {
                var one = working[i];
                var two = working[j];

                double dx = atX[two] - atX[one];
                double dy = atY[two] - atY[one];
                double overlapX = needX - Math.Abs(dx);
                double overlapY = needY - Math.Abs(dy);

                // Boxes only clash when they overlap on BOTH axes.
                if (overlapX <= 0 || overlapY <= 0) continue;

                // Push along whichever axis needs the least movement: it is
                // the shortest way out, so tags stay nearest where they were.
                bool pushAlongX = overlapX <= overlapY;
                double push = pushAlongX ? overlapX : overlapY;
                double direction = pushAlongX
                    ? (dx >= 0 ? 1.0 : -1.0)
                    : (dy >= 0 ? 1.0 : -1.0);

                bool oneCanMove = straight[one];
                bool twoCanMove = straight[two];

                if (!allowBothToMove && !oneCanMove && !twoCanMove) continue;

                if (allowBothToMove) { oneCanMove = true; twoCanMove = true; }

                // VERSION 2: a tag held still never moves, in either phase.
                if (fixedTag[one]) oneCanMove = false;
                if (fixedTag[two]) twoCanMove = false;
                if (!oneCanMove && !twoCanMove) continue;

                double shareOne, shareTwo;
                if (oneCanMove && twoCanMove) { shareOne = push / 2.0; shareTwo = push / 2.0; }
                else if (oneCanMove) { shareOne = push; shareTwo = 0; }
                else { shareOne = 0; shareTwo = push; }

                if (pushAlongX)
                {
                    atX[one] -= direction * shareOne;
                    atX[two] += direction * shareTwo;
                }
                else
                {
                    atY[one] -= direction * shareOne;
                    atY[two] += direction * shareTwo;
                }

                anyMovedThisPass = true;
                pairsTouched++;

                if (allowBothToMove && oneCanMove && twoCanMove)
                {
                    if (!bothHadToMove.Contains(one)) bothHadToMove.Add(one);
                    if (!bothHadToMove.Contains(two)) bothHadToMove.Add(two);
                }
            }
        }

        if (!anyMovedThisPass) break;
    }

    return pairsTouched;
};

separate(false);

// Anything still clashing had nowhere to go under the preference. Phase two
// drops it for those pairs only, and every tag it applies to is named.
bool anyLeft = false;
for (int i = 0; i < working.Count && !anyLeft; i++)
{
    for (int j = i + 1; j < working.Count; j++)
    {
        double dx = Math.Abs(atX[working[j]] - atX[working[i]]);
        double dy = Math.Abs(atY[working[j]] - atY[working[i]]);
        if (dx < needX && dy < needY) { anyLeft = true; break; }
    }
}

if (anyLeft) separate(true);

// ---- write it back ----
foreach (var id in working)
{
    var head = heads[id];
    double alongRight = atX[id] - head.DotProduct(right);
    double alongUp = atY[id] - head.DotProduct(up);
    if (Math.Abs(alongRight) < 1e-9 && Math.Abs(alongUp) < 1e-9) continue;

    XYZ target = head + right * alongRight + up * alongUp;

    // VERSION 2: a room, space or area tag, by its head.
    var spatialToMove = doc.GetElement(id) as SpatialElementTag;
    if (spatialToMove != null)
    {
        try { spatialToMove.TagHeadPosition = target; }
        catch { refused.Add(id); continue; }
        XYZ spatialNow = null;
        try { spatialNow = spatialToMove.TagHeadPosition; } catch { }
        if (spatialNow == null || spatialNow.IsAlmostEqualTo(head)) { refused.Add(id); continue; }
        moved++;
        continue;
    }

    var tag = doc.GetElement(id) as IndependentTag;
    if (tag == null) { refused.Add(id); continue; }

    try { tag.TagHeadPosition = target; }
    catch { refused.Add(id); continue; }

    // Put the free leader end back where it was pointing.
    if (freeEnds.ContainsKey(id))
    {
        var end = freeEnds[id];
#if REVIT2020 || REVIT2021
        try { tag.LeaderEnd = end; } catch { }
#else
        try
        {
            var references = tag.GetTaggedReferences();
            if (references != null && references.Count > 0) tag.SetLeaderEnd(references[0], end);
        }
        catch { }
#endif
    }

    XYZ now = null;
    try { now = tag.TagHeadPosition; } catch { }
    if (now == null || now.IsAlmostEqualTo(head)) { refused.Add(id); continue; }

    moved++;
}

// What is still on top of something, after everything. Reported rather than
// left for the eye to find on the sheet.
for (int i = 0; i < working.Count; i++)
{
    for (int j = i + 1; j < working.Count; j++)
    {
        double dx = Math.Abs(atX[working[j]] - atX[working[i]]);
        double dy = Math.Abs(atY[working[j]] - atY[working[i]]);
        if (dx >= needX || dy >= needY) continue;
        if (!stillOverlapping.Contains(working[i])) stillOverlapping.Add(working[i]);
        if (!stillOverlapping.Contains(working[j])) stillOverlapping.Add(working[j]);
    }
}
