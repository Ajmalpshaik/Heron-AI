// NOT STANDALONE. Assumes `elements`, `flipHand`, `flipFacing` and
// `fromToRoomOnly` are in scope; leaves `flipped`, `cannotFlip`,
// `notFamilyInstance`, `roomsSwapped` and `roomsLeft` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A FLIP IS NOT A ROTATION, and reaching for rotation instead is the mistake
// this fragment exists to prevent. A flip uses the family's OWN control, so it
// respects how the family was authored and keeps a hosted element correct
// against its host. Rotating a door 180 degrees moves geometry and can leave it
// in the wall the wrong way round, which looks right in plan and is wrong on
// site.
//
// ONLY A FAMILY INSTANCE CAN FLIP AT ALL, and not every family instance can
// flip in a given direction - whether a flip control exists is a decision the
// family AUTHOR made. So both are asked before either is attempted:
//
//   notFamilyInstance   a wall, a duct, a system element. Nothing to flip
//   cannotFlip          a family instance whose author gave it no such control
//
// Kept apart because they need different answers. The first is the wrong
// selection; the second is a family that would have to be edited.
//
// ASKING FIRST RATHER THAN CATCHING. `CanFlipHand` and `CanFlipFacing` are
// cheap and exact, so an unsupported flip is a reported outcome rather than an
// exception - and a batch of forty does not stop at the eleventh.
//
// VERSION 2 - A DOOR'S FROM AND TO ROOM FOLLOW ITS SWING. Revit does NOT swap
// From Room and To Room when a door's facing is flipped (seen 2026-10-19 on
// Revit 2020: FacingFlipped True, the room on the facing side right, get_ToRoom
// still the room it used to swing into). So after a facing flip every DOOR is
// read on the last phase - the one the bare To Room answers for - and its To
// Room is swapped with FlipFromToRoom ONLY WHEN THE GEOMETRY SAYS BOTH SIDES
// ARE THE WRONG WAY ROUND: the room a short way out along its facing is its
// From Room, and the room behind is its To Room. Anything less certain is left
// and said, never guessed. `fromToRoomOnly` does the same for every door
// handed in and flips nothing - for doors already wrong.
//
// A WINDOW IS NOT SWAPPED. Its To Room is not tied to the way it faces -
// PLACE_HOSTED_FAMILY sets it to the room inside while the window faces out -
// so it is named in `roomsLeft` instead.

var flipped = 0;
var cannotFlip = new List<ElementId>();
var notFamilyInstance = new List<ElementId>();
var roomsSwapped = new List<string>();
var roomsLeft = new List<string>();

var toCheck = new List<FamilyInstance>();
var doorCategory = new ElementId(BuiltInCategory.OST_Doors);
var windowCategory = new ElementId(BuiltInCategory.OST_Windows);
Func<FamilyInstance, bool> isDoor = fi => fi.Category != null && fi.Category.Id == doorCategory;
Func<FamilyInstance, string> labelOf = fi =>
    string.Format("{0} {1} ('{2}')", isDoor(fi) ? "Door" : "Window", fi.Id, fi.Name);

foreach (var element in elements)
{
    var instance = element as FamilyInstance;
    if (instance == null)
    {
        if (element != null) notFamilyInstance.Add(element.Id);
        continue;
    }

    // Rooms only: nothing is flipped, every door is checked.
    if (fromToRoomOnly)
    {
        if (isDoor(instance)) toCheck.Add(instance);
        else roomsLeft.Add(string.Format("{0} ('{1}') - not a door, so it has no From and To Room "
            + "to set", instance.Id, instance.Name));
        continue;
    }

    var did = false;
    var facingDone = false;

    if (flipHand)
    {
        if (instance.CanFlipHand) { instance.flipHand(); did = true; }
    }

    if (flipFacing)
    {
        if (instance.CanFlipFacing) { instance.flipFacing(); did = true; facingDone = true; }
    }

    if (did) flipped++;
    else cannotFlip.Add(instance.Id);

    if (facingDone && instance.Category != null)
    {
        if (isDoor(instance)) toCheck.Add(instance);
        else if (instance.Category.Id == windowCategory)
            roomsLeft.Add(labelOf(instance) + " - a window's To Room is not tied to the way it "
                + "faces, so From and To Room were left as they were");
    }
}

if (toCheck.Count > 0)
{
    var doc = toCheck[0].Document;
    doc.Regenerate();                                    // the facing just flipped

    // The bare To Room answers for the LAST phase, so that is the one asked.
    Phase phase = null;
    foreach (Phase candidate in doc.Phases)
    {
        if (candidate != null) phase = candidate;
    }

    Func<Room, string> nameOf = r => r == null ? "(outside)" : "'" + r.Name + "'";
    Func<Room, Room, bool> same = (a, b) => a == null ? b == null : b != null && a.Id == b.Id;

    foreach (var door in toCheck)
    {
        var label = labelOf(door);
        if (phase == null)
        {
            roomsLeft.Add(label + " - this model has no phases, so From and To Room cannot be read");
            continue;
        }

        Room from = null, to = null;
        try { from = door.get_FromRoom(phase); to = door.get_ToRoom(phase); } catch { }

        if (from == null && to == null)
        {
            roomsLeft.Add(label + " - no room on either side, so nothing was swapped");
            continue;
        }

        // The rooms a short way either side along its facing - clear of the
        // wall's half-thickness, well short of the far side of a corridor - a
        // foot above where it stands, so inside the room's height.
        Room ahead = null, behind = null;
        var at = door.Location as LocationPoint;
        if (at == null)
        {
            roomsLeft.Add(label + " - it has no point location, so the sides could not be read and "
                + "From and To Room were left as " + nameOf(from) + " -> " + nameOf(to));
            continue;
        }
        var wall = door.Host as Wall;
        var reach = (wall == null ? 0 : wall.Width / 2) + 300.0 / 304.8;
        var facing = door.FacingOrientation;
        var z = at.Point.Z + 1.0;
        try
        {
            ahead = doc.GetRoomAtPoint(new XYZ(at.Point.X + facing.X * reach,
                                               at.Point.Y + facing.Y * reach, z), phase);
            behind = doc.GetRoomAtPoint(new XYZ(at.Point.X - facing.X * reach,
                                                at.Point.Y - facing.Y * reach, z), phase);
        }
        catch { }

        if (same(to, ahead) && same(from, behind))
        {
            roomsLeft.Add(label + " - already right: " + nameOf(from) + " -> " + nameOf(to)
                + " on phase '" + phase.Name + "'");
            continue;
        }

        if (!(same(from, ahead) && same(to, behind)))
        {
            roomsLeft.Add(label + " - Revit says " + nameOf(from) + " -> " + nameOf(to)
                + ", the rooms found are " + nameOf(behind) + " behind and " + nameOf(ahead)
                + " on the side it faces. A swap would not make them agree, so it was left");
            continue;
        }

        string refused = null;
        try
        {
            door.FlipFromToRoom();
            doc.Regenerate();
        }
        catch (Exception failure) { refused = failure.Message; }

        Room nowFrom = null, nowTo = null;
        try { nowFrom = door.get_FromRoom(phase); nowTo = door.get_ToRoom(phase); } catch { }

        if (refused == null && same(nowTo, ahead) && same(nowFrom, behind))
            roomsSwapped.Add(label + " - From -> To was " + nameOf(from) + " -> " + nameOf(to)
                + ", now " + nameOf(nowFrom) + " -> " + nameOf(nowTo) + " on phase '" + phase.Name
                + "'");
        else
            roomsLeft.Add(label + " - the swap did not take: To Room reads " + nameOf(nowTo)
                + ", not " + nameOf(ahead) + (refused == null ? "" : ". Revit said: " + refused));
    }
}
