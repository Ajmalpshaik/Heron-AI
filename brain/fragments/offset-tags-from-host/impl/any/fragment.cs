// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `tagCategories`,
// `offsetRightMm`, `offsetUpMm` and `addLeader` are in scope, and leaves
// `moved`, `leadersOn`, `report`, `viewportKept`, `alreadyThere`,
// `fromBoxCentre`, `noHostLocation`, `leaderNotAsAsked`, `notTags`,
// `otherCategory`, `notInView`, `notSupported` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A whole view's tags, one undo.
//
// MEASURED FROM THE TAG'S OWN ELEMENT, NEVER FROM WHERE THE HEAD IS NOW.
//
// The target is the element's location plus the offset, so a second run
// computes the same target and finds every tag already there. Measuring from
// the head would walk every tag another 8 mm on every run.
//
// The element's location is its LocationPoint - an air terminal, a door, a
// piece of equipment, a room - or the middle of its LocationCurve - a duct, a
// pipe, a wall. Anything with neither is measured from the centre of its box,
// and COUNTED, because a box centre is a guess about where a thing "is".
//
// THE OFFSET IS ON PAPER AND IN THE VIEW'S OWN PLANE.
//
// 8 mm right on the sheet is 600 mm in the model at 1:75, and "right" is the
// view's right, which is not the model's X on a rotated plan. Only the two
// in-plane components are set; the head keeps its own depth, so an air
// terminal 2.7 m up does not drag its tag off the view's plane.
//
// TWO KINDS OF TAG, AND BOTH ARE HANDLED.
//
// An IndependentTag - a duct, pipe, air terminal, equipment, door or window
// tag - and a room, space or area tag share no base class carrying a head and a
// leader. Casting to the first alone skips every room tag silently. A room tag
// has no leader END CONDITION to set on any release, so for it "leader on"
// means the leader is switched on and its end stays where the tag is anchored
// in the room.
//
// FROM 2022 A TAG CAN POINT AT SEVERAL ELEMENTS, and which one it should sit
// beside is not answerable here. Such a tag is named in notSupported and left
// alone. The 2020 and 2021 properties are singular and were REMOVED after
// 2022, so the split is a compile-time one, as in ARRANGE_TAGS.
//
// A FREE LEADER END TRAVELS WITH THE HEAD. When the leader is left as it is, a
// free end is read before and written back after, so the arrow does not come
// off what it points at.
//
// MOVING TAGS HAS MOVED THE VIEW'S VIEWPORT ON ITS SHEET. Seen twice on
// 2026-09-27 - 30 and 43 mm, with the box the same size. So each viewport of
// this view is read before, read again after a regenerate, and put back if it
// moved more than half a millimetre on the paper - inside this same undo - and
// the report says what happened.

const double MmToFeet = 1.0 / 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

int moved = 0;
int leadersOn = 0;
int fromBoxCentre = 0;
string report = "";
string viewportKept = "";
var alreadyThere = new List<ElementId>();
var noHostLocation = new List<ElementId>();
var leaderNotAsAsked = new List<ElementId>();
var notTags = new List<ElementId>();
var otherCategory = new List<ElementId>();
var notInView = new List<ElementId>();
var notSupported = new List<ElementId>();
var refused = new List<ElementId>();
var reasons = new List<string>();

Func<double, string> number = value => Math.Round(value, 2).ToString(invariant);

// ---- what was asked ----
string leaderWord = (addLeader ?? "").Trim().ToLowerInvariant();
int leaderMode;   // 1 on and attached, 0 off, -1 left as it is
if (leaderWord == "true" || leaderWord == "yes" || leaderWord == "on") leaderMode = 1;
else if (leaderWord == "false" || leaderWord == "no" || leaderWord == "off") leaderMode = 0;
else if (leaderWord == "keep" || leaderWord == "as is" || leaderWord == "leave") leaderMode = -1;
else leaderMode = -2;

var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
bool everyCategory = false;
if (tagCategories != null)
{
    foreach (var name in tagCategories)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) continue;
        if (trimmed.Equals("all", StringComparison.OrdinalIgnoreCase)) everyCategory = true;
        else wanted.Add(trimmed);
    }
}

string refusal = null;
if (view == null) refusal = "No view was given";
else if (view.IsTemplate) refusal = "'" + view.Name + "' is a view template, which holds no tags";
else if (leaderMode == -2)
    refusal = "addLeader '" + addLeader + "' is not understood - say true (leader on, end attached), "
        + "false (no leader) or keep (leave each leader as it is)";
else if (!everyCategory && wanted.Count == 0)
    refusal = "No tag category was named - say which, for example Air Terminal Tags, or all";
else if (elements == null || elements.Count == 0)
    refusal = "No tags were handed in, so there was nothing to move";

int scale = 0;
if (refusal == null)
{
    try { scale = view.Scale; } catch { }
    if (scale <= 0) refusal = "'" + view.Name + "' has no scale, so a paper offset cannot be turned into a model one";
}

if (refusal != null)
{
    report = refusal + ". Nothing was moved.";
}
else
{
    var right = view.RightDirection;
    var up = view.UpDirection;
    double alongRight = offsetRightMm * scale * MmToFeet;
    double alongUp = offsetUpMm * scale * MmToFeet;
    // Half a millimetre on the paper: close enough to be "there", and loose
    // enough that a read-back's rounding never reads as a miss (row 5b-237).
    double tolerance = 0.5 * scale * MmToFeet;
    double worstMiss = 0;

    Func<XYZ, double, double, double> missBy = (point, wantRight, wantUp) =>
    {
        double dx = point.DotProduct(right) - wantRight;
        double dy = point.DotProduct(up) - wantUp;
        return Math.Sqrt(dx * dx + dy * dy);
    };

    // The element's own point: location point, curve middle, then box centre.
    // `usedBox` says which answered, because the last one is counted.
    bool usedBox = false;
    Func<Element, Transform, XYZ> pointOf = (host, toHost) =>
    {
        usedBox = false;
        if (host == null) return null;
        XYZ point = null;
        Location location = null;
        try { location = host.Location; } catch { }
        var asPoint = location as LocationPoint;
        if (asPoint != null) { try { point = asPoint.Point; } catch { } }
        if (point == null)
        {
            var asCurve = location as LocationCurve;
            if (asCurve != null) { try { point = asCurve.Curve.Evaluate(0.5, true); } catch { } }
        }
        if (point == null)
        {
            BoundingBoxXYZ box = null;
            try { box = host.get_BoundingBox(null); } catch { }
            if (box != null)
            {
                point = (box.Min + box.Max) / 2.0;
                try { if (box.Transform != null) point = box.Transform.OfPoint(point); } catch { }
                usedBox = true;
            }
        }
        if (point == null) return null;
        return toHost == null ? point : toHost.OfPoint(point);
    };

    // ---- the viewport, before anything moves ----
    var viewports = new List<Viewport>();
    try
    {
        foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
        {
            var port = candidate as Viewport;
            if (port != null && port.ViewId == view.Id) viewports.Add(port);
        }
    }
    catch { }
    var centreBefore = new Dictionary<ElementId, XYZ>();
    foreach (var port in viewports)
    {
        try { centreBefore[port.Id] = port.GetBoxCenter(); } catch { }
    }

    foreach (var element in elements)
    {
        if (element == null) continue;

        var independent = element as IndependentTag;
        var spatial = element as SpatialElementTag;
        if (independent == null && spatial == null) { notTags.Add(element.Id); continue; }

        string categoryName = element.Category == null ? "" : element.Category.Name;
        if (!everyCategory && !wanted.Contains(categoryName)) { otherCategory.Add(element.Id); continue; }

        ElementId ownerView = ElementId.InvalidElementId;
        try { ownerView = element.OwnerViewId; } catch { }
        if (ownerView != view.Id) { notInView.Add(element.Id); continue; }

        // ---- the element this tag belongs to ----
        Element host = null;
        Transform toHost = null;
        LinkElementId taggedId = null;

        if (independent != null)
        {
            var ids = new List<LinkElementId>();
#if REVIT2020 || REVIT2021
            try { var one = independent.TaggedElementId; if (one != null) ids.Add(one); } catch { }
#else
            try { var all = independent.GetTaggedElementIds(); if (all != null) ids.AddRange(all); } catch { }
#endif
            if (ids.Count > 1)
            {
                notSupported.Add(element.Id);
                reasons.Add(element.Id + " points at " + ids.Count + " elements - which to sit beside is not answerable");
                continue;
            }
            if (ids.Count == 1) taggedId = ids[0];
        }
        else
        {
            var roomTag = spatial as RoomTag;
            var spaceTag = spatial as SpaceTag;
            var areaTag = spatial as AreaTag;
            if (roomTag != null)
            {
                try { taggedId = roomTag.TaggedRoomId; } catch { }
                if (taggedId == null || taggedId.LinkInstanceId == ElementId.InvalidElementId)
                {
                    taggedId = null;
                    try { host = roomTag.Room; } catch { }
                }
            }
            else if (spaceTag != null) { try { host = spaceTag.Space; } catch { } }
            else if (areaTag != null) { try { host = areaTag.Area; } catch { } }
        }

        if (taggedId != null)
        {
            if (taggedId.LinkInstanceId != ElementId.InvalidElementId)
            {
                RevitLinkInstance link = null;
                try { link = doc.GetElement(taggedId.LinkInstanceId) as RevitLinkInstance; } catch { }
                Document linked = null;
                try { if (link != null) linked = link.GetLinkDocument(); } catch { }
                if (linked != null)
                {
                    try { host = linked.GetElement(taggedId.LinkedElementId); } catch { }
                    try { toHost = link.GetTotalTransform(); } catch { host = null; }
                }
                else
                {
                    reasons.Add(element.Id + " tags an element in a link that is not loaded");
                }
            }
            else
            {
                try { host = doc.GetElement(taggedId.HostElementId); } catch { }
            }
        }

        var anchor = pointOf(host, toHost);
        if (anchor == null) { noHostLocation.Add(element.Id); continue; }
        if (usedBox) fromBoxCentre++;

        double wantRight = anchor.DotProduct(right) + alongRight;
        double wantUp = anchor.DotProduct(up) + alongUp;

        // ---- where it is now ----
        XYZ head = null;
        bool hadLeader = false;
        bool wasAttached = true;   // a room tag has no end condition to be free
        LeaderEndCondition endBefore = LeaderEndCondition.Attached;
        try { head = independent != null ? independent.TagHeadPosition : spatial.TagHeadPosition; } catch { }
        try { hadLeader = independent != null ? independent.HasLeader : spatial.HasLeader; } catch { }
        if (independent != null && hadLeader)
        {
            try { endBefore = independent.LeaderEndCondition; } catch { }
            wasAttached = endBefore == LeaderEndCondition.Attached;
        }
        if (head == null)
        {
            refused.Add(element.Id);
            reasons.Add(element.Id + " has no head position Revit will report");
            continue;
        }

        bool leaderWasRight = leaderMode == -1
            || (leaderMode == 0 && !hadLeader)
            || (leaderMode == 1 && hadLeader && wasAttached);

        if (missBy(head, wantRight, wantUp) <= tolerance && leaderWasRight)
        {
            alreadyThere.Add(element.Id);
            if (hadLeader) leadersOn++;
            continue;
        }

        XYZ target = head + right * (wantRight - head.DotProduct(right)) + up * (wantUp - head.DotProduct(up));

        // ---- move it ----
        try
        {
            if (independent != null)
            {
                XYZ freeEnd = null;
#if REVIT2020 || REVIT2021
                if (leaderMode == -1 && hadLeader && !wasAttached) { try { freeEnd = independent.LeaderEnd; } catch { } }
#else
                Reference taggedReference = null;
                try
                {
                    var references = independent.GetTaggedReferences();
                    if (references != null && references.Count > 0) taggedReference = references[0];
                }
                catch { }
                if (leaderMode == -1 && hadLeader && !wasAttached && taggedReference != null)
                {
                    try { freeEnd = independent.GetLeaderEnd(taggedReference); } catch { }
                }
#endif
                if (leaderMode == 1)
                {
                    if (!independent.HasLeader) independent.HasLeader = true;
                    if (independent.LeaderEndCondition != LeaderEndCondition.Attached
                        && independent.CanLeaderEndConditionBeAssigned(LeaderEndCondition.Attached))
                        independent.LeaderEndCondition = LeaderEndCondition.Attached;
                }
                else if (leaderMode == 0 && independent.HasLeader)
                {
                    independent.HasLeader = false;
                }

                independent.TagHeadPosition = target;

                if (freeEnd != null)
                {
#if REVIT2020 || REVIT2021
                    try { independent.LeaderEnd = freeEnd; } catch { }
#else
                    try { independent.SetLeaderEnd(taggedReference, freeEnd); } catch { }
#endif
                }
            }
            else
            {
                // Leader first: switched on after the move, a room tag's leader
                // would start from the head it had just been moved to.
                if (leaderMode == 1 && !spatial.HasLeader) spatial.HasLeader = true;
                else if (leaderMode == 0 && spatial.HasLeader) spatial.HasLeader = false;
                spatial.TagHeadPosition = target;
            }
        }
        catch (Exception ex)
        {
            // Put back what this tag had, so a refusal leaves it as it was.
            try
            {
                if (independent != null)
                {
                    if (independent.HasLeader != hadLeader) independent.HasLeader = hadLeader;
                    if (hadLeader && independent.LeaderEndCondition != endBefore) independent.LeaderEndCondition = endBefore;
                    independent.TagHeadPosition = head;
                }
                else
                {
                    if (spatial.HasLeader != hadLeader) spatial.HasLeader = hadLeader;
                    spatial.TagHeadPosition = head;
                }
            }
            catch { }
            refused.Add(element.Id);
            reasons.Add(element.Id + ": " + ex.Message);
            continue;
        }

        // ---- read it back ----
        XYZ now = null;
        bool leaderNow = false;
        bool attachedNow = true;
        try { now = independent != null ? independent.TagHeadPosition : spatial.TagHeadPosition; } catch { }
        try { leaderNow = independent != null ? independent.HasLeader : spatial.HasLeader; } catch { }
        if (independent != null && leaderNow)
        {
            try { attachedNow = independent.LeaderEndCondition == LeaderEndCondition.Attached; } catch { attachedNow = false; }
        }

        double miss = now == null ? double.MaxValue : missBy(now, wantRight, wantUp);
        if (miss > tolerance)
        {
            refused.Add(element.Id);
            reasons.Add(element.Id + " was sent but its head reads back "
                + (now == null ? "as nothing" : number(miss / scale / MmToFeet) + " mm from the target on paper"));
            continue;
        }

        worstMiss = Math.Max(worstMiss, miss);
        moved++;
        if (leaderNow) leadersOn++;

        bool leaderIsRight = leaderMode == -1
            || (leaderMode == 0 && !leaderNow)
            || (leaderMode == 1 && leaderNow && attachedNow);
        if (!leaderIsRight)
        {
            leaderNotAsAsked.Add(element.Id);
            reasons.Add(element.Id + " moved, but its leader reads back "
                + (leaderNow ? (attachedNow ? "on" : "on with a FREE end - Revit would not attach it") : "off"));
        }
    }

    // ---- the viewport, after ----
    var portLines = new List<string>();
    if (viewports.Count == 0)
    {
        portLines.Add("The view is on no sheet, so no viewport could move");
    }
    else
    {
        try { doc.Regenerate(); } catch { }
        foreach (var port in viewports)
        {
            string sheetName = "a sheet";
            try
            {
                var sheet = doc.GetElement(port.SheetId) as ViewSheet;
                if (sheet != null) sheetName = sheet.SheetNumber;
            }
            catch { }

            XYZ before;
            if (!centreBefore.TryGetValue(port.Id, out before) || before == null)
            {
                portLines.Add("The viewport on " + sheetName + " could not be read before the move, so it was left alone");
                continue;
            }

            Func<XYZ, string> at = point =>
                "(" + number(point.X / MmToFeet) + ", " + number(point.Y / MmToFeet) + ") mm";
            Func<XYZ, XYZ, double> apartMm = (a, b) =>
                Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)) / MmToFeet;

            XYZ after = null;
            try { after = port.GetBoxCenter(); } catch { }
            if (after == null)
            {
                portLines.Add("The viewport on " + sheetName + " could not be read after the move - check it on the sheet");
                continue;
            }

            double shift = apartMm(before, after);
            if (shift <= 0.5)
            {
                portLines.Add("Viewport on " + sheetName + ": box centre " + at(before) + " before and "
                    + at(after) + " after - it did not move");
                continue;
            }

            try { port.SetBoxCenter(before); doc.Regenerate(); } catch { }
            XYZ restored = null;
            try { restored = port.GetBoxCenter(); } catch { }
            if (restored != null && apartMm(before, restored) <= 0.5)
                portLines.Add("Viewport on " + sheetName + ": box centre " + at(before) + " before; moving the tags pushed it "
                    + number(shift) + " mm to " + at(after) + ", and it was PUT BACK to " + at(restored) + " in this same undo");
            else
                portLines.Add("Viewport on " + sheetName + ": box centre " + at(before) + " before; moving the tags pushed it "
                    + number(shift) + " mm to " + at(after) + " and it COULD NOT be put back"
                    + (restored == null ? "" : " - it reads " + at(restored)) + ". Move it back by hand");
        }
    }
    viewportKept = string.Join(". ", portLines.ToArray());

    string leaderSaid = leaderMode == 1 ? "leader on with its end attached"
        : leaderMode == 0 ? "no leader" : "each leader left as it was";

    report = string.Format(invariant,
        "{0} tag(s) moved to {1} mm right and {2} mm up on paper from their own element ({3} and {4} mm in the model at 1:{5}), {6}. "
        + "{7} already there. Read back: every moved head within {8} mm of its target on paper; {9} with a leader on. "
        + "Left alone: {10} of a category not asked for, {11} in another view, {12} not tags. "
        + "No element location: {13}. Measured from the element's box centre: {14}. "
        + "Not supported: {15}. Leader not as asked: {16}. Refused: {17}. {18}",
        moved, number(offsetRightMm), number(offsetUpMm), number(alongRight / MmToFeet), number(alongUp / MmToFeet), scale, leaderSaid,
        alreadyThere.Count, number(worstMiss / scale / MmToFeet), leadersOn,
        otherCategory.Count, notInView.Count, notTags.Count,
        noHostLocation.Count, fromBoxCentre,
        notSupported.Count, leaderNotAsAsked.Count, refused.Count, viewportKept);

    if (reasons.Count > 0)
        report += " Why: " + string.Join("; ", reasons.Take(5).ToArray())
            + (reasons.Count > 5 ? "; and " + (reasons.Count - 5) + " more" : "");
}
