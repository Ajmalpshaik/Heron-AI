// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `tagCategories`,
// `offsetRightMm`, `offsetUpMm`, `addLeader`, `orientHorizontal`,
// `orientVertical`, `orientModel`, `orientToHost` and `keepHead` are in
// scope, and leaves `moved`, `turned`, `leadersOn`, `report`, `viewportKept`,
// `alreadyThere`, `fromBoxCentre`, `noHostLocation`, `noDirection`,
// `offAxis`, `leaderNotAsAsked`, `notTags`, `otherCategory`, `notInView`,
// `notSupported` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A whole view's tags, one undo.
//
// VERSION 2 TURNS A TAG AS WELL AS MOVING IT. The four orient* switches are
// ONE choice - Horizontal, Vertical, Model, or the way the tag's own element
// runs in this view - and none of them said leaves each tag's orientation as
// it is, exactly as version 1 did. keepHead leaves each head where it is, so
// a tag can be turned, or its leader changed, without moving it. Every one is
// an optional bool, so a caller that names none of them binds false and runs
// version 1 unchanged: the binder refuses any other absent value.
//
// "THE WAY ITS ELEMENT RUNS" IS READ IN THE VIEW'S OWN PLANE. A family's hand
// direction - which runs along the wall for a wall-hosted opening or door -
// else its facing turned a quarter, else the line of a duct, pipe or wall,
// else the wall an opening is cut in. Along the view's right is Horizontal,
// along its up is Vertical, and the nearer of the two is taken for anything
// between; more than a degree off it is named in offAxis, and exactly 45 is
// left as it was. A room, a riser seen end-on, or an element with none of
// those has no direction here: named in noDirection, orientation left.
//
// MODEL IS AN ELEMENT TAG's FROM 2022 ONLY. TagOrientation has no Model value
// on 2020 and 2021 (the reference assemblies, 2026-10-15), so it is asked for
// by NUMBER and only where Enum.IsDefined finds it - written by name, the
// fragment would not compile on those releases. A room, space or area tag's
// own orientation (SpatialElementTagOrientation) has Horizontal, Vertical and
// Model on every release 2020 to 2027.
//
// THE ORIENTATION IS READ BACK, never assumed from the call returning. A tag
// Revit keeps the other way - a tag family that turns with its element, for
// one - is put back as it was, whole, and named in refused.
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
// 2022, and the split is made AT RUN TIME by looking the member up on the tag,
// NOT with a version #if: the add-in compiles fragments with no release
// symbols, so an #if always takes its #else (row 5b-181).
//
// A FREE LEADER END TRAVELS WITH THE HEAD. When the leader is left as it is, a
// free end is read before and written back after, so the arrow does not come
// off what it points at.
//
// MOVING TAGS HAS MOVED THE VIEW'S VIEWPORT ON ITS SHEET. Seen twice on
// 2026-09-27 - 30 and 43 mm, with the box the same size. So each viewport of
// this view is read before, read again after a regenerate, and put back if it
// moved more than half a millimetre on the paper with its box the SAME SIZE -
// inside this same undo. A box that changed size is reported and left: its
// centre moves when it grows, while the plan inside it does not.

const double MmToFeet = 1.0 / 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

int moved = 0;
int turned = 0;
int leadersOn = 0;
int fromBoxCentre = 0;
string report = "";
string viewportKept = "";
var alreadyThere = new List<ElementId>();
var noHostLocation = new List<ElementId>();
var noDirection = new List<ElementId>();
var offAxis = new List<ElementId>();
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

// -1 each left as it is, 0 Horizontal, 1 Vertical, 2 Model, 3 the way its
// element runs. 0, 1 and 2 are both enums' own numbers on every release.
var orientationsGiven = new List<string>();
if (orientHorizontal) orientationsGiven.Add("orientHorizontal");
if (orientVertical) orientationsGiven.Add("orientVertical");
if (orientModel) orientationsGiven.Add("orientModel");
if (orientToHost) orientationsGiven.Add("orientToHost");
int orientMode = orientHorizontal ? 0 : orientVertical ? 1 : orientModel ? 2 : orientToHost ? 3 : -1;
bool elementTagHasModel = Enum.IsDefined(typeof(TagOrientation), 2);

Func<int, string> orientationName = value =>
    value == 0 ? "Horizontal" : value == 1 ? "Vertical" : value == 2 ? "Model" : "unreadable";

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
else if (orientationsGiven.Count > 1)
    refusal = string.Join(" and ", orientationsGiven.ToArray()) + " were each given as true, and they are one "
        + "choice - give one of orientHorizontal, orientVertical, orientModel or orientToHost, or none to "
        + "leave each tag turned as it is";
else if (keepHead && (offsetRightMm != 0 || offsetUpMm != 0))
    refusal = "keepHead leaves each tag's head where it is, so an offset of " + number(offsetRightMm)
        + " mm right and " + number(offsetUpMm) + " mm up cannot also be kept - give both offsets as 0 "
        + "with keepHead, or leave keepHead out to move the tags";
else if (keepHead && orientMode == -1 && leaderMode == -1)
    refusal = "keepHead with no orientation and addLeader keep asks for no change - say which way to turn "
        + "the tags (orientHorizontal, orientVertical, orientModel or orientToHost) or what to do with the leader";
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

    // A model direction as it shows in the view's plane: { along right, along
    // up }, one long, or null when less than a fifth of it shows - a thing
    // seen end-on has no way it runs in this view.
    Func<XYZ, Transform, double[]> inView = (vector, toHost) =>
    {
        if (vector == null) return null;
        var turnedVector = toHost == null ? vector : toHost.OfVector(vector);
        double alongRightPart = turnedVector.DotProduct(right);
        double alongUpPart = turnedVector.DotProduct(up);
        double shown = Math.Sqrt(alongRightPart * alongRightPart + alongUpPart * alongUpPart);
        if (shown < 1e-9 || shown < 0.2 * turnedVector.GetLength()) return null;
        return new[] { alongRightPart / shown, alongUpPart / shown };
    };

    Func<Element, Transform, double[]> lineOf = (runner, toHost) =>
    {
        Location location = null;
        try { location = runner.Location; } catch { }
        var asCurve = location as LocationCurve;
        if (asCurve == null) return null;
        try { return inView(asCurve.Curve.ComputeDerivatives(0.5, true).BasisX, toHost); } catch { return null; }
    };

    // The way the element runs in this view, or null. A family's hand first -
    // along the wall for anything hosted in one - then its facing turned a
    // quarter, then its own line, then the wall an opening or a family is
    // hosted in.
    Func<Element, Transform, double[]> directionOf = (host, toHost) =>
    {
        if (host == null) return null;
        double[] found = null;
        var instance = host as FamilyInstance;
        if (instance != null)
        {
            try { found = inView(instance.HandOrientation, toHost); } catch { }
            if (found == null)
            {
                double[] facing = null;
                try { facing = inView(instance.FacingOrientation, toHost); } catch { }
                if (facing != null) found = new[] { -facing[1], facing[0] };
            }
        }
        if (found == null) found = lineOf(host, toHost);
        if (found == null)
        {
            Element wall = null;
            var opening = host as Opening;
            if (opening != null) { try { wall = opening.Host; } catch { } }
            else if (instance != null) { try { wall = instance.Host; } catch { } }
            if (wall != null) found = lineOf(wall, toHost);
        }
        return found;
    };

    Func<IndependentTag, SpatialElementTag, int> orientationOf = (independentTag, spatialTag) =>
    {
        try { return independentTag != null ? (int)independentTag.TagOrientation : (int)spatialTag.TagOrientation; }
        catch { return -1; }
    };

    Action<IndependentTag, SpatialElementTag, int> setOrientation = (independentTag, spatialTag, value) =>
    {
        if (independentTag != null) independentTag.TagOrientation = (TagOrientation)value;
        else spatialTag.TagOrientation = (SpatialElementTagOrientation)value;
    };

    int horizontalNow = 0, verticalNow = 0, modelNow = 0;
    Action<int> countOrientation = value =>
    {
        if (value == 0) horizontalNow++;
        else if (value == 1) verticalNow++;
        else if (value == 2) modelNow++;
    };

    // ---- the 2022 split, looked up at run time ----
    //
    // NO VERSION #if. The add-in compiles a fragment with no release symbols,
    // so an #if always takes its #else - which on 2020 and 2021 calls members
    // those releases do not have (row 5b-181). Each member is found by name on
    // the tag in front of it instead: the 2022-and-later method where Revit
    // has it, the 2020/2021 property where it does not. A missing member is
    // null here, never a compile error.
    Func<IndependentTag, List<LinkElementId>> taggedIdsOf = tag =>
    {
        var found = new List<LinkElementId>();
        var many = tag.GetType().GetMethod("GetTaggedElementIds", Type.EmptyTypes);
        if (many != null)
        {
            var all = many.Invoke(tag, null) as IEnumerable<LinkElementId>;
            if (all != null) found.AddRange(all);
            return found;
        }
        var single = tag.GetType().GetProperty("TaggedElementId");
        var one = single == null ? null : single.GetValue(tag, null) as LinkElementId;
        if (one != null) found.Add(one);
        return found;
    };

    // The first tagged reference, which the 2022-and-later leader-end methods
    // take. Null on 2020 and 2021, where the property needs none.
    Func<IndependentTag, Reference> firstReferenceOf = tag =>
    {
        var method = tag.GetType().GetMethod("GetTaggedReferences", Type.EmptyTypes);
        if (method == null) return null;
        var references = method.Invoke(tag, null) as IList<Reference>;
        return references != null && references.Count > 0 ? references[0] : null;
    };

    Func<IndependentTag, XYZ> leaderEndOf = tag =>
    {
        var method = tag.GetType().GetMethod("GetLeaderEnd", new[] { typeof(Reference) });
        if (method != null)
        {
            var reference = firstReferenceOf(tag);
            return reference == null ? null : method.Invoke(tag, new object[] { reference }) as XYZ;
        }
        var property = tag.GetType().GetProperty("LeaderEnd");
        return property == null ? null : property.GetValue(tag, null) as XYZ;
    };

    // True when the end was written; false when no member could take it.
    Func<IndependentTag, XYZ, bool> setLeaderEnd = (tag, end) =>
    {
        var method = tag.GetType().GetMethod("SetLeaderEnd", new[] { typeof(Reference), typeof(XYZ) });
        if (method != null)
        {
            var reference = firstReferenceOf(tag);
            if (reference == null) return false;
            method.Invoke(tag, new object[] { reference, end });
            return true;
        }
        var property = tag.GetType().GetProperty("LeaderEnd");
        if (property == null || !property.CanWrite) return false;
        property.SetValue(tag, end, null);
        return true;
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
    var sizeBefore = new Dictionary<ElementId, XYZ>();
    foreach (var port in viewports)
    {
        try { centreBefore[port.Id] = port.GetBoxCenter(); } catch { }
        try { var box = port.GetBoxOutline(); sizeBefore[port.Id] = box.MaximumPoint - box.MinimumPoint; } catch { }
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
            try { ids = taggedIdsOf(independent); } catch { }
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

        // With keepHead the target is where the head already is, so the
        // element's point is not needed - an orphan tag can still be turned.
        XYZ anchor = null;
        if (!keepHead)
        {
            anchor = pointOf(host, toHost);
            if (anchor == null) { noHostLocation.Add(element.Id); continue; }
            if (usedBox) fromBoxCentre++;
        }

        // ---- the orientation asked of THIS tag ----
        int orientationBefore = orientationOf(independent, spatial);
        int orientationWanted = -1;   // left as it is
        if (orientMode == 0 || orientMode == 1)
        {
            orientationWanted = orientMode;
        }
        else if (orientMode == 2)
        {
            if (independent != null && !elementTagHasModel)
            {
                refused.Add(element.Id);
                reasons.Add(element.Id + " is an element tag, and Revit gives an element tag no Model orientation "
                    + "before 2022 - it was left as it was");
                continue;
            }
            orientationWanted = 2;
        }
        else if (orientMode == 3)
        {
            double[] runs = null;
            try { runs = directionOf(host, toHost); } catch { }
            if (runs == null)
            {
                noDirection.Add(element.Id);
                reasons.Add(element.Id + (host == null ? "'s element could not be found"
                    : "'s element has no direction in this view - a room, or a run seen end-on")
                    + ", so it was left turned as it was");
            }
            else
            {
                // 0 along the view's right, 90 along its up, whichever way the
                // element was drawn.
                double angle = Math.Atan2(Math.Abs(runs[1]), Math.Abs(runs[0])) * 180.0 / Math.PI;
                if (Math.Abs(angle - 45.0) <= 0.5)
                {
                    noDirection.Add(element.Id);
                    reasons.Add(element.Id + "'s element runs at 45 degrees in this view, where neither Horizontal "
                        + "nor Vertical is nearer, so it was left turned as it was");
                }
                else
                {
                    orientationWanted = angle < 45.0 ? 0 : 1;
                    double offBy = angle < 45.0 ? angle : 90.0 - angle;
                    if (offBy > 1.0)
                    {
                        offAxis.Add(element.Id);
                        reasons.Add(element.Id + "'s element runs " + number(offBy) + " degrees off the view's "
                            + (angle < 45.0 ? "right" : "up") + ", so it was turned " + orientationName(orientationWanted)
                            + ", the nearer - for an angle, orientModel sets Revit's own Model orientation "
                            + "(element tags from Revit 2022)");
                    }
                }
            }
        }
        bool orientationWasRight = orientationWanted == -1 || orientationBefore == orientationWanted;

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

        double wantRight = keepHead ? head.DotProduct(right) : anchor.DotProduct(right) + alongRight;
        double wantUp = keepHead ? head.DotProduct(up) : anchor.DotProduct(up) + alongUp;

        bool leaderWasRight = leaderMode == -1
            || (leaderMode == 0 && !hadLeader)
            || (leaderMode == 1 && hadLeader && wasAttached);

        if (missBy(head, wantRight, wantUp) <= tolerance && leaderWasRight && orientationWasRight)
        {
            alreadyThere.Add(element.Id);
            if (hadLeader) leadersOn++;
            countOrientation(orientationBefore);
            continue;
        }

        XYZ target = head + right * (wantRight - head.DotProduct(right)) + up * (wantUp - head.DotProduct(up));
        XYZ freeEnd = null;

        // What this tag had, put back. Used when Revit refuses the move AND
        // when it accepts it but the head reads back somewhere else - a
        // refused tag is left as it was either way, never half-moved.
        Action putBack = () =>
        {
            // The orientation first and on its own: turning may carry the
            // head, which is put back after it.
            try
            {
                if (orientationBefore >= 0 && orientationOf(independent, spatial) != orientationBefore)
                    setOrientation(independent, spatial, orientationBefore);
            }
            catch { }
            try
            {
                if (independent != null)
                {
                    if (independent.HasLeader != hadLeader) independent.HasLeader = hadLeader;
                    if (hadLeader && independent.LeaderEndCondition != endBefore) independent.LeaderEndCondition = endBefore;
                    independent.TagHeadPosition = head;
                    if (freeEnd != null) setLeaderEnd(independent, freeEnd);
                }
                else
                {
                    if (spatial.HasLeader != hadLeader) spatial.HasLeader = hadLeader;
                    spatial.TagHeadPosition = head;
                }
            }
            catch { }
        };

        // ---- move it ----
        try
        {
            if (independent != null)
            {
                if (leaderMode == -1 && hadLeader && !wasAttached) { try { freeEnd = leaderEndOf(independent); } catch { } }
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

                // Turned BEFORE the head is placed, so a head that turning
                // carried is set where it was asked for.
                if (!orientationWasRight) setOrientation(independent, null, orientationWanted);

                independent.TagHeadPosition = target;

                // Put the free end back. Whether it took is read back below,
                // not assumed from the call returning.
                if (freeEnd != null) { try { setLeaderEnd(independent, freeEnd); } catch { } }
            }
            else
            {
                // Leader first: switched on after the move, a room tag's leader
                // would start from the head it had just been moved to.
                if (leaderMode == 1 && !spatial.HasLeader) spatial.HasLeader = true;
                else if (leaderMode == 0 && spatial.HasLeader) spatial.HasLeader = false;
                if (!orientationWasRight) setOrientation(null, spatial, orientationWanted);
                spatial.TagHeadPosition = target;
            }
        }
        catch (Exception ex)
        {
            putBack();
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
            putBack();
            refused.Add(element.Id);
            reasons.Add(element.Id + " was sent but its head read back "
                + (now == null ? "as nothing" : number(miss / scale / MmToFeet) + " mm from the target on paper")
                + ", so it was put back as it was");
            continue;
        }

        // The orientation, read back. Kept the other way - a tag family that
        // turns with its element, for one - is a refusal, put back whole.
        int orientationNow = orientationOf(independent, spatial);
        if (!orientationWasRight && orientationNow != orientationWanted)
        {
            putBack();
            refused.Add(element.Id);
            reasons.Add(element.Id + " was turned " + orientationName(orientationWanted) + " but reads back "
                + orientationName(orientationNow) + ", so it was put back as it was");
            continue;
        }

        worstMiss = Math.Max(worstMiss, miss);
        moved++;
        if (!orientationWasRight) turned++;
        if (leaderNow) leadersOn++;
        countOrientation(orientationNow);

        // A kept free end must still be where it pointed. Read back, never
        // assumed from the call returning - a swallowed failure would let the
        // arrow travel with the head, which is what keeping it is for.
        bool freeEndHeld = true;
        if (freeEnd != null)
        {
            XYZ endNow = null;
            try { endNow = leaderEndOf(independent); } catch { }
            freeEndHeld = endNow != null && endNow.DistanceTo(freeEnd) <= tolerance;
        }

        bool leaderIsRight = (leaderMode == -1 && freeEndHeld)
            || (leaderMode == 0 && !leaderNow)
            || (leaderMode == 1 && leaderNow && attachedNow);
        if (!leaderIsRight)
        {
            leaderNotAsAsked.Add(element.Id);
            reasons.Add(element.Id + " moved, but its leader reads back "
                + (!freeEndHeld ? "with its free end dragged off where it pointed"
                   : leaderNow ? (attachedNow ? "on" : "on with a FREE end - Revit would not attach it") : "off"));
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

            // ONLY A MOVE IS PUT BACK, NEVER A GROWTH. A box that grew on one
            // side has a new centre while the plan inside it has not moved,
            // and recentring it would shift the plan by half the growth. The
            // same size before and after is the evidence it was translated;
            // anything else is reported, and the sheet is the place to look.
            XYZ before2 = null, after2 = null;
            sizeBefore.TryGetValue(port.Id, out before2);
            try { var box = port.GetBoxOutline(); after2 = box.MaximumPoint - box.MinimumPoint; } catch { }
            bool sameSize = before2 != null && after2 != null
                && Math.Abs(before2.X - after2.X) / MmToFeet <= 0.5
                && Math.Abs(before2.Y - after2.Y) / MmToFeet <= 0.5;
            if (!sameSize)
            {
                portLines.Add("Viewport on " + sheetName + ": box centre " + at(before) + " before and " + at(after)
                    + " after, and the box changed size"
                    + (before2 != null && after2 != null
                        ? " from " + number(before2.X / MmToFeet) + " x " + number(before2.Y / MmToFeet) + " to "
                          + number(after2.X / MmToFeet) + " x " + number(after2.Y / MmToFeet) + " mm"
                        : "")
                    + ". NOT put back: a box that grew moves its centre without moving the plan. Check the sheet");
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

    string orientationSaid = orientMode == -1 ? "each turned as it was"
        : orientMode == 3 ? "each turned the way its own element runs in the view"
        : "turned " + orientationName(orientMode);

    string whereSaid = keepHead
        ? string.Format(invariant, "{0} tag(s) changed with each head left where it was", moved)
        : string.Format(invariant,
            "{0} tag(s) moved to {1} mm right and {2} mm up on paper from their own element ({3} and {4} mm in the model at 1:{5})",
            moved, number(offsetRightMm), number(offsetUpMm), number(alongRight / MmToFeet), number(alongUp / MmToFeet), scale);

    report = string.Format(invariant,
        "{0}, {1}, {2}. {3} turned. "
        + "{4} already there. Read back: every moved head within {5} mm of its target on paper; {6} with a leader on; "
        + "{7} Horizontal, {8} Vertical and {9} Model among the tags moved or already there. "
        + "Left alone: {10} of a category not asked for, {11} in another view, {12} not tags. "
        + "No element location: {13}. Measured from the element's box centre: {14}. "
        + "No direction to follow: {15}. Turned to the nearer of Horizontal and Vertical while running off both: {16}. "
        + "Not supported: {17}. Leader not as asked: {18}. Refused: {19}. {20}",
        whereSaid, leaderSaid, orientationSaid, turned,
        alreadyThere.Count, number(worstMiss / scale / MmToFeet), leadersOn,
        horizontalNow, verticalNow, modelNow,
        otherCategory.Count, notInView.Count, notTags.Count,
        noHostLocation.Count, fromBoxCentre,
        noDirection.Count, offAxis.Count,
        notSupported.Count, leaderNotAsAsked.Count, refused.Count, viewportKept);

    if (reasons.Count > 0)
        report += " Why: " + string.Join("; ", reasons.Take(5).ToArray())
            + (reasons.Count > 5 ? "; and " + (reasons.Count - 5) + " more" : "");
}
