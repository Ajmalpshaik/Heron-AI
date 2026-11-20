// NOT STANDALONE. Assumes `doc`, `elements`, `containerKind` and `includeLinks`
// are in scope; leaves `findings`, `groupNames`, `groupCounts`, `placed`,
// `unplaced`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// GROUP_AND_COUNT CANNOT DO THIS. It groups on a parameter, and most MEP
// elements have NO Room or Space parameter at all - a duct does not know which
// room it passes through. Asking through a lookup returns one large group of
// blanks and looks like an answer.
//
// PHASE IS NOT OFFERED HERE ON PURPOSE. "Count by phase created" looks like the
// same question; phase IS an ordinary parameter, so GROUP_AND_COUNT already does
// it, faster and exactly.
//
// THE TEST POINT TAKES THE CONTAINER'S Z, NOT THE ELEMENT'S. A room answers its
// containment test at its own level, and a diffuser sits at ceiling height - so
// testing the element's own Z reports everything as in no room.
//
// VERSION 3 (2026-11-20, row 5b-1858, issue #1060): ON STOREYS STACKED OVER ONE
// ANOTHER, THE ELEMENT'S OWN STOREY. That test is a test IN PLAN, so a space on
// Level 2 also said yes to a damper on Level 1 directly below it, and version 2
// took the FIRST container that said yes, in element-id order. Now every
// container is asked - ASSIGN_LOCATION_DATA version 3's rule: one is taken
// when the element is INSIDE it (Revit's test at the element's own point), or
// in plan of it and on its storey - not below its floor, not at or above the
// next STOREY level up (Building Story ticked; with none ticked, every level).
// Inside beats on-its-storey; between two on their storey, the one whose floor
// is nearest below. Two edges keep a single-storey model as it was: a
// container with no storey level above it has no top, and one with no storey
// level below it has no bottom - so with one level every container is the
// plan test version 2 made. Two equally good are counted in the first, as
// version 2 did, and findings counts them. An element in plan only of
// another storey's containers is counted in none, and findings counts those.
// The host's containers are still asked first; a link's only where none of the
// host's was taken.
//
// VERSION 3: A ROOM'S NAME IS ITS NAME PARAMETER. Revit's `Name` of a room or
// space already carries its number, so version 2's label read "Room 1 1"
// (its own proof). The label is ROOM_NAME and the number, as
// ASSIGN_LOCATION_DATA reads them. And with includeLinks, a link that is NOT
// LOADED is named in linkedMatches - version 2 skipped it without a word.
//
// A ZONE IS ONE HOP FURTHER: the containing space is found first, then that
// space's zone. "No space" and "the space has no zone" are two different
// failures for two different people, so they are two groups.
//
// AN UNPLACED OR UNENCLOSED CONTAINER IS SKIPPED, NOT ASKED. Its test cannot
// mean anything, and asking it is how an element gets filed under a room that is
// not on the drawing.
//
// LINKED ROOMS AND SPACES ARE CONTAINERS ONLY WHEN ASKED FOR - D-59. This is
// the fragment a federated job needs most: the ducts are in this model and the
// ROOMS ARE IN THE ARCHITECTURAL LINK, so host-only answers "in no room" for
// every one of them. Absent `includeLinks` means host only, which is what this
// did before and what its proof measured.
//
// When it is set, each loaded link's placed and enclosed rooms (or spaces) are
// asked AFTER the host's own, so a host container still wins where both
// answer. The element's point is moved into the link through each placement's
// inverse transform, and the container's own Z is then taken there, as for a
// host container. A linked group is named "<link> :: <room>", so two models
// with a room of the same name stay two groups. The ELEMENTS are still only
// the host's own, handed in as before - nothing from a link is selected or
// carried (FRAGMENT-ISSUES row 75).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.

var findings = new List<string>();
var groupNames = new List<string>();
var groupCounts = new List<int>();
var placed = 0;
var unplaced = 0;

var kind = (containerKind ?? "").Trim().ToLower();

var rooms = new List<Room>();
var spaces = new List<Space>();
var skippedContainers = 0;

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();
var notLoaded = new List<string>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null)
        {
            // VERSION 3: named, never skipped silently (row 5b-1858 (4)).
            var unloadedName = "";
            try { var linkType = doc.GetElement(typeId); unloadedName = linkType == null ? instance.Name : linkType.Name; }
            catch (Exception) { unloadedName = instance.Name; }
            if (!notLoaded.Contains(unloadedName)) notLoaded.Add(unloadedName);
            continue;
        }

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

// Containers from the links: the element, the transform INTO its link, and
// the link's name - one row per placement, because a file placed twice
// occupies two places.
var linkedRooms = new List<Room>();
var linkedRoomsTo = new List<Transform>();
var linkedRoomsFrom = new List<string>();
var linkedSpaces = new List<Space>();
var linkedSpacesTo = new List<Transform>();
var linkedSpacesFrom = new List<string>();
var linkedSkipped = 0;
var linkedLanded = 0;

if (kind == "room")
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType())
    {
        var room = element as Room;
        if (room == null) continue;
        // Area is inherited from SpatialElement. Zero means unplaced or
        // unenclosed, and such a room's containment test means nothing.
        if (room.Area <= 0.0) { skippedContainers++; continue; }
        rooms.Add(room);
    }
}
else if (kind == "space" || kind == "zone")
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_MEPSpaces).WhereElementIsNotElementType())
    {
        var space = element as Space;
        if (space == null) continue;
        if (space.Area <= 0.0) { skippedContainers++; continue; }
        spaces.Add(space);
    }
}

// An unknown container kind reads nothing anywhere - the findings say why -
// so no link is claimed as read.
if (kind != "room" && kind != "space" && kind != "zone" && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no container kind to look for - see findings";

for (var i = 0; i < linkDocs.Count && linkBlocked.Length == 0; i++)
{
    var linked = linkDocs[i];
    var usable = 0;

    var inLink = new List<SpatialElement>();
    try
    {
        if (kind == "room")
        {
            foreach (var element in new FilteredElementCollector(linked)
                .OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType())
            {
                var room = element as Room;
                if (room == null) continue;
                if (room.Area <= 0.0) { linkedSkipped++; continue; }
                inLink.Add(room);
            }
        }
        else if (kind == "space" || kind == "zone")
        {
            foreach (var element in new FilteredElementCollector(linked)
                .OfCategory(BuiltInCategory.OST_MEPSpaces).WhereElementIsNotElementType())
            {
                var space = element as Space;
                if (space == null) continue;
                if (space.Area <= 0.0) { linkedSkipped++; continue; }
                inLink.Add(space);
            }
        }
    }
    catch (Exception) { }

    foreach (var placement in linkPlacements[i])
    {
        Transform toLink = null;
        try { toLink = placement.GetTotalTransform().Inverse; }
        catch (Exception) { toLink = null; }
        if (toLink == null) continue;

        foreach (var container in inLink)
        {
            var room = container as Room;
            var space = container as Space;
            if (room != null)
            {
                linkedRooms.Add(room); linkedRoomsTo.Add(toLink); linkedRoomsFrom.Add(linked.Title);
            }
            else if (space != null)
            {
                linkedSpaces.Add(space); linkedSpacesTo.Add(toLink); linkedSpacesFrom.Add(linked.Title);
            }
        }
    }

    usable = inLink.Count;
    linksSearched++;
    linkedMatches.Add(string.Format("{0}: {1} placed and enclosed {2}(s) asked", linked.Title,
        usable, kind == "room" ? "room" : "space"));
}

if (linkedSkipped > 0)
    linkedMatches.Add(string.Format("{0} linked {1}(s) are UNPLACED or UNENCLOSED and were not asked",
        linkedSkipped, kind == "room" ? "room" : "space"));

Func<Element, XYZ> pointOf = element =>
{
    var location = element.Location as LocationPoint;
    if (location != null && location.Point != null) return location.Point;

    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch (Exception) { }
    if (box == null) return null;
    return (box.Min + box.Max) * 0.5;
};

// VERSION 3: a room's or space's label - its Name PARAMETER and its number.
Func<SpatialElement, string> labelOf = container =>
{
    var shownName = "";
    try
    {
        var p = container.get_Parameter(BuiltInParameter.ROOM_NAME);
        shownName = p == null ? "" : (p.AsString() ?? "");
    }
    catch (Exception) { shownName = ""; }
    var shownNumber = "";
    try { shownNumber = container.Number ?? ""; } catch (Exception) { }
    return (shownName + " " + shownNumber).Trim();
};

// VERSION 3: THE STOREY each container's floor sits on, in its own document.
// The levels with Building Story ticked - every level where none is - and for
// a container the height to the next one up and whether one is below. A
// DIFFERENCE of two elevations, so it means the same in a link.
var tolerance = 1.0 / 304.8;
Func<Document, List<double>> storeyElevationsIn = inDoc =>
{
    var every = new List<double>();
    var storeys = new List<double>();
    try
    {
        foreach (var level in new FilteredElementCollector(inDoc).OfClass(typeof(Level)).Cast<Level>())
        {
            every.Add(level.ProjectElevation);
            Parameter flag = null;
            try { flag = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY); } catch (Exception) { flag = null; }
            if (flag != null && flag.HasValue && flag.AsInteger() == 1) storeys.Add(level.ProjectElevation);
        }
    }
    catch (Exception) { }
    return storeys.Count > 0 ? storeys : every;
};
var storeyCache = new Dictionary<Document, List<double>>();
// Height up to the next storey level (-1: none above) and whether a storey
// level is below - for one container.
Func<SpatialElement, Tuple<double, bool>> storeyOf = container =>
{
    Level own = null;
    try { own = container.Level; } catch (Exception) { own = null; }
    if (own == null) return Tuple.Create(-1.0, false);
    List<double> levels;
    if (!storeyCache.TryGetValue(container.Document, out levels))
    {
        levels = storeyElevationsIn(container.Document);
        storeyCache[container.Document] = levels;
    }
    var at = own.ProjectElevation;
    double next = double.MaxValue;
    var below = false;
    foreach (var e in levels)
    {
        if (e > at + tolerance && e < next) next = e;
        if (e < at - tolerance) below = true;
    }
    return Tuple.Create(next == double.MaxValue ? -1.0 : next - at, below);
};

var tiesTakenFirst = 0;
var otherStoreyOnly = 0;

// The container for one point among a list of them, each with the transform
// INTO its document (null for this model's own): -1 when none is taken.
// otherStorey is set when one was in plan of it and turned down for its height.
Func<XYZ, List<SpatialElement>, List<Transform>, Tuple<int, bool>> chooseAmong = (point, containers, intoDocs) =>
{
    var best = -1;
    var bestRank = 9;
    var bestGap = double.MaxValue;
    var tie = false;
    var otherStorey = false;
    for (var c = 0; c < containers.Count; c++)
    {
        var container = containers[c];
        var local = intoDocs == null || intoDocs[c] == null ? point : intoDocs[c].OfPoint(point);
        var asRoom = container as Room;
        var asSpace = container as Space;
        if (asRoom == null && asSpace == null) continue;
        var at = container.Location as LocationPoint;
        var floorZ = at != null && at.Point != null ? at.Point.Z : local.Z;

        bool inside, inPlan;
        try
        {
            inside = asRoom != null ? asRoom.IsPointInRoom(local) : asSpace.IsPointInSpace(local);
            var atFloor = new XYZ(local.X, local.Y, floorZ);
            inPlan = inside || (asRoom != null ? asRoom.IsPointInRoom(atFloor) : asSpace.IsPointInSpace(atFloor));
        }
        catch (Exception) { continue; }
        if (!inPlan) continue;

        var rank = 0;
        var gap = 0.0;
        if (!inside)
        {
            var storey = storeyOf(container);
            var above = local.Z - floorZ;
            if (above < -tolerance && storey.Item2) { otherStorey = true; continue; }        // below its floor
            if (storey.Item1 >= 0 && above >= storey.Item1 - tolerance) { otherStorey = true; continue; } // above its storey
            rank = above >= -tolerance ? 1 : 2;      // 2: below the lowest storey's floor
            gap = Math.Abs(above);
        }
        if (rank < bestRank || (rank == bestRank && gap < bestGap - tolerance))
        {
            best = c; bestRank = rank; bestGap = gap; tie = false;
        }
        else if (rank == bestRank && Math.Abs(gap - bestGap) <= tolerance) tie = true;
    }
    if (best >= 0 && tie) tiesTakenFirst++;
    return Tuple.Create(best, otherStorey);
};

Func<Element, string> groupFor = element =>
{
    var point = pointOf(element);
    if (point == null) return "(no location Revit can read)";

    if (kind == "room")
    {
        var inHost = chooseAmong(point, rooms.Cast<SpatialElement>().ToList(), null);
        if (inHost.Item1 >= 0) return labelOf(rooms[inHost.Item1]);

        // The links' rooms, only where none of the host's was taken.
        var fromLink = chooseAmong(point, linkedRooms.Cast<SpatialElement>().ToList(), linkedRoomsTo);
        if (fromLink.Item1 >= 0)
        {
            linkedLanded++;
            return linkedRoomsFrom[fromLink.Item1] + " :: " + labelOf(linkedRooms[fromLink.Item1]);
        }
        if (inHost.Item2 || fromLink.Item2) otherStoreyOnly++;
        return "(in no room)";
    }

    Space containing = null;
    var containingFrom = "";
    var hostSpace = chooseAmong(point, spaces.Cast<SpatialElement>().ToList(), null);
    var turnedDown = hostSpace.Item2;
    if (hostSpace.Item1 >= 0) containing = spaces[hostSpace.Item1];
    else
    {
        // The links' spaces, only where none of the host's was taken.
        var linkedSpace = chooseAmong(point, linkedSpaces.Cast<SpatialElement>().ToList(), linkedSpacesTo);
        turnedDown = turnedDown || linkedSpace.Item2;
        if (linkedSpace.Item1 >= 0)
        {
            containing = linkedSpaces[linkedSpace.Item1];
            containingFrom = linkedSpacesFrom[linkedSpace.Item1] + " :: ";
            linkedLanded++;
        }
    }
    if (containing == null && turnedDown) otherStoreyOnly++;

    if (kind == "space")
    {
        return containing != null
            ? containingFrom + labelOf(containing)
            : "(in no space)";
    }

    // "zone" - and the two ways it can miss are different jobs for different
    // people, so they are different groups.
    if (containing == null) return "(in no space, so no zone either)";
    var zone = containing.Zone;
    return zone != null ? containingFrom + zone.Name : "(the space it is in has no zone)";
};

if (kind != "room" && kind != "space" && kind != "zone")
{
    findings.Add("'" + (containerKind ?? "") + "' is not a container this fragment knows. It counts by "
        + "\"room\", \"space\" or \"zone\", and NOTHING was counted - a default here would answer a "
        + "question nobody asked");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were handed in, so there is nothing to count. That is not a model with "
        + "no elements in any " + kind);
}
else if ((kind == "room" && rooms.Count + linkedRooms.Count == 0)
         || (kind != "room" && spaces.Count + linkedSpaces.Count == 0))
{
    findings.Add("This model has no placed and enclosed " + (kind == "room" ? "room" : "space")
        + " at all" + (skippedContainers > 0
            ? " - " + skippedContainers + " were found but are unplaced or unenclosed, so their "
                + "containment test means nothing"
            : "")
        + ". EVERY element would report as being in none, and that says nothing about the elements");
}
else
{
    var index = new Dictionary<string, int>();

    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        var group = groupFor(element);
        var known = 0;
        if (index.TryGetValue(group, out known))
        {
            groupCounts[known] = groupCounts[known] + 1;
        }
        else
        {
            index[group] = groupNames.Count;
            groupNames.Add(group);
            groupCounts.Add(1);
        }

        if (group.StartsWith("(")) unplaced++; else placed++;
    }

    // Largest group first, by a plain selection pass over the two lists together
    // - which keeps them honestly in step at these sizes.
    var taken = new List<bool>();
    for (var i = 0; i < groupNames.Count; i++) taken.Add(false);

    while (true)
    {
        var best = -1;
        for (var i = 0; i < groupNames.Count; i++)
        {
            if (taken[i]) continue;
            if (best < 0 || groupCounts[i] > groupCounts[best]) best = i;
        }
        if (best < 0) break;
        taken[best] = true;
        findings.Add(string.Format("  {0,-46} {1}", groupNames[best], groupCounts[best]));
    }
}

if (otherStoreyOnly > 0)
    findings.Add(string.Format("{0} element(s) were in plan only of a {1} on ANOTHER STOREY - below its floor or "
        + "above its storey - and are counted in none: a {1} is taken only for an element inside it or on its "
        + "own storey (version 3, row 5b-1858)", otherStoreyOnly, kind == "room" ? "room" : "space"));
if (tiesTakenFirst > 0)
    findings.Add(string.Format("{0} element(s) were equally in two {1}s - overlapping ones - and were counted in "
        + "the first", tiesTakenFirst, kind == "room" ? "room" : "space"));

if (skippedContainers > 0)
{
    findings.Add(string.Format("{0} {1}(s) in this model are UNPLACED or UNENCLOSED and were not asked. "
        + "Anything actually sitting in one of those reports as being in none, and a model full of them "
        + "is the real finding here",
        skippedContainers, kind == "room" ? "room" : "space"));
}

findings.Insert(0, string.Format("{0} element(s) counted by {1}: {2} landed in one, {3} did not. "
    + "Containment is tested GEOMETRICALLY - in plan at the container's own height, then on the element's "
    + "own storey (version 3) - these elements carry no "
    + "{1} parameter to group by, which is why a parameter lookup returns one large group of blanks",
    placed + unplaced,
    kind == "room" || kind == "space" || kind == "zone" ? kind : "(nothing)",
    placed, unplaced));

linkedTotal = linkedLanded;

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} element(s) landed in a linked "
        + "container", linksSearched, linkedTotal));

if (includeLinks && notLoaded.Count > 0)
    linkedMatches.Add(string.Format("{0} link(s) NOT LOADED, so their rooms and spaces were NOT asked: {1} - "
        + "an element in one of those is counted in none", notLoaded.Count, string.Join(", ", notLoaded)));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked rooms and spaces are used only as containers - every element "
        + "counted is this model's own, handed in as before");
