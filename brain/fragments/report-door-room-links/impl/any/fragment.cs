// NOT STANDALONE. Assumes `doc`, `elements`, `phaseName` and `probeDistanceMm`
// are in scope; leaves `findings`, `doorsChecked`, `disagreements`,
// `noRoomEitherSide` and `phaseUsed` behind.
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
