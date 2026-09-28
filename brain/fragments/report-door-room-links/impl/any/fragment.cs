// NOT STANDALONE. Assumes `doc`, `elements`, `phaseName`, `probeDistanceMm` and
// `includeLinks` are in scope; leaves `findings`, `doorsChecked`,
// `disagreements`, `noRoomEitherSide`, `phaseUsed`, `linksSearched` and
// `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// FromRoom AND ToRoom ARE DECIDED BY THE DOOR'S FACING, NOT BY THE ROOMS. Flip
// a door and the two swap: the plan still looks right and the schedule reads
// backwards, with no warning. So each is CHECKED against geometry - a point a
// short way either side along the door's own facing normal, asked of each room.
// WHERE THE GEOMETRY DISAGREES, THE GEOMETRY WINS AND THE DISAGREEMENT IS
// REPORTED, NEVER SILENTLY CORRECTED: a flipped door is a modelling problem
// worth knowing about, not just a reporting one.
//
// FROM AND TO ARE PHASE-DEPENDENT and the bare property answers for the LAST
// phase only. On a refurbishment that is wrong for every phase but one, so the
// phase used is resolved explicitly and named in the summary.
//
// A DOOR WITH ONE ROOM IS CORRECT - an entrance has outside on one side, and
// that is "(outside)". A door with NEITHER side in a room is a different thing
// and is listed separately.
//
// mm TO INTERNAL FEET BY /304.8. Plain arithmetic at the edge, never a units
// API - that is the call that breaks at Revit 2021.
//
// LINKED DOORS ARE CHECKED ONLY WHEN ASKED FOR - D-59, and absent
// `includeLinks` means host only, which is what this did before. In a
// federated job the doors AND their rooms are in the architectural link, and
// neither a selection nor a chain can hand a linked door in (FRAGMENT-ISSUES
// row 75). So when it is set, EVERY door in each loaded link is checked
// against THAT link's own rooms, on the link's phase of the same name, with
// the same probe - all inside the link, so no transform is involved. The
// result is TEXT in `linkedMatches`; the counts and lists above stay the
// host's own, so a proof of the host behaviour still reads the same.
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.

var findings = new List<string>();
var doorsChecked = 0;
var disagreements = new List<string>();
var noRoomEitherSide = new List<string>();
var phaseUsed = "";

// Resolve the phase once. Named, so a reader can see which answer they got.
Phase phase = null;
foreach (Phase candidate in doc.Phases)
{
    if (candidate == null) continue;
    phase = candidate;                                   // ends on the last one
    if (!string.IsNullOrEmpty(phaseName)
        && string.Equals(candidate.Name, phaseName, StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
}

if (phase == null)
{
    findings.Add("This document has no phases, so FromRoom and ToRoom cannot be asked for one. "
        + "Nothing was checked");
}
else
{
    phaseUsed = phase.Name;

    if (!string.IsNullOrEmpty(phaseName)
        && !string.Equals(phaseUsed, phaseName, StringComparison.OrdinalIgnoreCase))
    {
        findings.Add(string.Format("No phase called '{0}' - used '{1}' instead. On a refurbishment "
            + "the rooms either side of a door change BY PHASE, so check this is the one you meant",
            phaseName, phaseUsed));
    }

    var probeFt = (probeDistanceMm > 0 ? probeDistanceMm : 300.0) / 304.8;

    // Rooms, collected once for the geometry check.
    var rooms = new List<Room>();
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType())
    {
        var room = element as Room;
        if (room != null && room.Area > 0) rooms.Add(room);
    }

    foreach (var element in elements)
    {
        var door = element as FamilyInstance;
        if (door == null) continue;

        doorsChecked++;
        var label = string.Format("Door {0} ('{1}')", door.Id, door.Name);

        Room fromRoom = null;
        Room toRoom = null;
        try
        {
            fromRoom = door.get_FromRoom(phase);
            toRoom = door.get_ToRoom(phase);
        }
        catch (Exception) { }

        // The geometry check. A point either side along the door's own facing.
        Room geomFacing = null;
        Room geomBehind = null;
        var probed = false;
        try
        {
            var location = door.Location as LocationPoint;
            if (location != null)
            {
                var facing = door.FacingOrientation;
                var ahead = location.Point + facing.Multiply(probeFt);
                var behind = location.Point - facing.Multiply(probeFt);

                foreach (var room in rooms)
                {
                    // One guard per room: an unenclosed room throws here.
                    try
                    {
                        if (geomFacing == null && room.IsPointInRoom(ahead)) geomFacing = room;
                        if (geomBehind == null && room.IsPointInRoom(behind)) geomBehind = room;
                    }
                    catch (Exception) { }
                }
                probed = true;
            }
        }
        catch (Exception) { }

        var fromName = fromRoom == null ? "(outside)" : fromRoom.Name;
        var toName = toRoom == null ? "(outside)" : toRoom.Name;

        if (fromRoom == null && toRoom == null)
        {
            noRoomEitherSide.Add(string.Format("{0} - no room on either side. Usually the rooms are "
                + "not placed, or their boundaries do not close", label));
            findings.Add(string.Format("{0}: (outside) -> (outside)", label));
            continue;
        }

        // ToRoom is the room the door FACES. Compare with what the probe found.
        var agrees = true;
        if (probed)
        {
            var toMatches = (toRoom == null && geomFacing == null)
                || (toRoom != null && geomFacing != null && toRoom.Id == geomFacing.Id);
            var fromMatches = (fromRoom == null && geomBehind == null)
                || (fromRoom != null && geomBehind != null && fromRoom.Id == geomBehind.Id);
            agrees = toMatches && fromMatches;
        }

        if (!probed)
        {
            findings.Add(string.Format("{0}: {1} -> {2}   [NOT CHECKED - the door has no point "
                + "location, so the geometry could not be probed]", label, fromName, toName));
        }
        else if (agrees)
        {
            findings.Add(string.Format("{0}: {1} -> {2}", label, fromName, toName));
        }
        else
        {
            var geomFrom = geomBehind == null ? "(outside)" : geomBehind.Name;
            var geomTo = geomFacing == null ? "(outside)" : geomFacing.Name;
            disagreements.Add(string.Format("{0} - Revit says {1} -> {2}, the GEOMETRY says {3} -> "
                + "{4}. The door is very likely flipped; the geometry is the one to believe",
                label, fromName, toName, geomFrom, geomTo));
            findings.Add(string.Format("{0}: {1} -> {2}   <-- DISAGREES with geometry ({3} -> {4})",
                label, fromName, toName, geomFrom, geomTo));
        }
    }

    findings.Insert(0, string.Format("{0} door(s) checked on phase '{1}'. {2} disagree with the "
        + "geometry; {3} have no room on either side",
        doorsChecked, phaseUsed, disagreements.Count, noRoomEitherSide.Count));
}

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
        if (linked == null) continue;

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

// The phase a link is asked on: the one asked for, else the host's, by NAME -
// a link's phases are its own elements.
var linkPhaseWanted = !string.IsNullOrEmpty(phaseName) ? phaseName : phaseUsed;
var linkProbeFt = (probeDistanceMm > 0 ? probeDistanceMm : 300.0) / 304.8;

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];

    Phase linkPhase = null;
    foreach (Phase candidate in linked.Phases)
    {
        if (candidate == null) continue;
        linkPhase = candidate;                            // ends on the last one
        if (!string.IsNullOrEmpty(linkPhaseWanted)
            && string.Equals(candidate.Name, linkPhaseWanted, StringComparison.OrdinalIgnoreCase))
        {
            break;
        }
    }

    linksSearched++;

    if (linkPhase == null)
    {
        linkedMatches.Add(string.Format("{0}: no phases, so no door could be asked", linked.Title));
        continue;
    }

    var linkRooms = new List<Room>();
    var linkDoors = new List<FamilyInstance>();
    try
    {
        foreach (var element in new FilteredElementCollector(linked)
            .OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType())
        {
            var room = element as Room;
            if (room != null && room.Area > 0) linkRooms.Add(room);
        }

        foreach (var element in new FilteredElementCollector(linked)
            .OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType())
        {
            var door = element as FamilyInstance;
            if (door != null) linkDoors.Add(door);
        }
    }
    catch (Exception) { }

    var linkDisagree = new List<string>();
    var linkNoRoom = 0;
    var linkNotProbed = 0;

    foreach (var door in linkDoors)
    {
        Room fromRoom = null;
        Room toRoom = null;
        try
        {
            fromRoom = door.get_FromRoom(linkPhase);
            toRoom = door.get_ToRoom(linkPhase);
        }
        catch (Exception) { }

        if (fromRoom == null && toRoom == null) { linkNoRoom++; continue; }

        Room geomFacing = null;
        Room geomBehind = null;
        var probed = false;
        try
        {
            var location = door.Location as LocationPoint;
            if (location != null)
            {
                var facing = door.FacingOrientation;
                var ahead = location.Point + facing.Multiply(linkProbeFt);
                var behind = location.Point - facing.Multiply(linkProbeFt);

                foreach (var room in linkRooms)
                {
                    try
                    {
                        if (geomFacing == null && room.IsPointInRoom(ahead)) geomFacing = room;
                        if (geomBehind == null && room.IsPointInRoom(behind)) geomBehind = room;
                    }
                    catch (Exception) { }
                }
                probed = true;
            }
        }
        catch (Exception) { }

        if (!probed) { linkNotProbed++; continue; }

        var toMatches = (toRoom == null && geomFacing == null)
            || (toRoom != null && geomFacing != null && toRoom.Id == geomFacing.Id);
        var fromMatches = (fromRoom == null && geomBehind == null)
            || (fromRoom != null && geomBehind != null && fromRoom.Id == geomBehind.Id);

        if (!(toMatches && fromMatches))
        {
            linkDisagree.Add(string.Format("  {0}: Door {1} ('{2}') - Revit says {3} -> {4}, the "
                + "GEOMETRY says {5} -> {6}", linked.Title, door.Id, door.Name,
                fromRoom == null ? "(outside)" : fromRoom.Name,
                toRoom == null ? "(outside)" : toRoom.Name,
                geomBehind == null ? "(outside)" : geomBehind.Name,
                geomFacing == null ? "(outside)" : geomFacing.Name));
        }
    }

    linkedTotal += linkDisagree.Count;
    linkedMatches.Add(string.Format("{0}: {1} door(s) on phase '{2}'{3}; {4} disagree with the "
        + "geometry, {5} have no room on either side{6}", linked.Title, linkDoors.Count,
        linkPhase.Name,
        !string.IsNullOrEmpty(linkPhaseWanted)
            && !string.Equals(linkPhase.Name, linkPhaseWanted, StringComparison.OrdinalIgnoreCase)
            ? string.Format(" (it has no phase '{0}')", linkPhaseWanted)
            : "",
        linkDisagree.Count, linkNoRoom,
        linkNotProbed > 0 ? string.Format(", {0} could not be probed", linkNotProbed) : ""));
    linkedMatches.AddRange(linkDisagree);
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} linked door(s) disagree",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked doors are EVERY door in each link, reported here and never "
        + "selected - a selection cannot reach into a link, and a chain cannot carry one");
