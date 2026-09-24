// NOT STANDALONE. Assumes `doc`, `elements`, `roomParameter`, `levelParameter`
// and `useSpaces` are in scope; leaves `written`, `noRoomFound` and `refused`
// behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// IT IS NOT WRITE_ELEMENT_PARAMETERS. That writes a value somebody supplies.
// This READS each element's position and derives what to write, per element -
// nobody can hand in four hundred different room names.
//
// A TARGET NAME TWO PARAMETERS SHARE IS NOT FILLED. A shared or project
// parameter can be bound beside a built-in one of the same name, and
// LookupParameter then returns one of them - "determined at random", in
// Autodesk's own reference - so the room name would land in a field no schedule
// reads. Each target is checked on its own and counted apart (D-54 s3,
// FRAGMENT-ISSUES 5b-203).
//
// ROOM AND SPACE ARE NOT THE SAME THING and this is the commonest way a location
// register comes out wrong. Architecture puts people in ROOMS; MEP calculates in
// SPACES. A model can hold both, in the same place, with different names.
//
// THE PROBE POINT IS NUDGED TO THE ROOM'S OWN MID-HEIGHT. The room test needs a
// point inside the room's VOLUME, and a ceiling-mounted diffuser sits above the
// room's upper limit - so from its own position it tests false, and a register
// built the obvious way has every terminal blank.
//
// ROOMS AND SPACES ARE TOLD APART BY CATEGORY. The wrapper imports the base
// namespace and the two spatial types are not both available as names.

var written = 0;
var noRoomFound = new List<ElementId>();
var refused = new List<string>();

var wantedCategory = useSpaces ? BuiltInCategory.OST_MEPSpaces : BuiltInCategory.OST_Rooms;
var label = useSpaces ? "SPACE" : "ROOM";

// The spatial elements, with the volume each one occupies.
var spatial = new List<Element>();
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(wantedCategory).WhereElementIsNotElementType())
{
    var box = element.get_BoundingBox(null);
    if (box == null) continue;      // unplaced or unbounded: no volume to test against
    spatial.Add(element);
}

if (spatial.Count == 0)
{
    refused.Add(string.Format("this model has no placed {0}S with a boundary. If the {1}s are in a "
        + "LINK, nothing here can see them and every element below will come back blank",
        label, useSpaces ? "space" : "room"));
}

var missingRoomParameter = 0;
var missingLevelParameter = 0;
var ambiguousRoomParameter = 0;
var ambiguousLevelParameter = 0;

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    XYZ point = null;
    var location = element.Location as LocationPoint;
    if (location != null) point = location.Point;
    if (point == null)
    {
        var box = element.get_BoundingBox(null);
        if (box != null) point = (box.Min + box.Max) / 2.0;
    }
    if (point == null) continue;

    // Which spatial element contains it. The probe Z is the ROOM's mid-height,
    // not the element's - see the header.
    Element found = null;
    foreach (var candidate in spatial)
    {
        var box = candidate.get_BoundingBox(null);
        if (box == null) continue;
        if (point.X < box.Min.X || point.X > box.Max.X) continue;
        if (point.Y < box.Min.Y || point.Y > box.Max.Y) continue;

        var midHeight = (box.Min.Z + box.Max.Z) / 2.0;
        var probe = new XYZ(point.X, point.Y, midHeight);

        var inside = false;
        try
        {
            var room = candidate as Room;
            if (room != null) inside = room.IsPointInRoom(probe);
            else inside = probe.Z >= box.Min.Z && probe.Z <= box.Max.Z;
        }
        catch
        {
            inside = probe.Z >= box.Min.Z && probe.Z <= box.Max.Z;
        }

        if (inside) { found = candidate; break; }
    }

    var touched = false;

    if (!string.IsNullOrEmpty(roomParameter))
    {
        var twice = element.GetParameters(roomParameter).Count > 1;
        var target = twice ? null : element.LookupParameter(roomParameter);
        if (twice) ambiguousRoomParameter++;
        else if (target == null || target.IsReadOnly) missingRoomParameter++;
        else if (found == null) noRoomFound.Add(element.Id);
        else
        {
            try { target.Set(found.Name); touched = true; }
            catch { }
        }
    }

    if (!string.IsNullOrEmpty(levelParameter))
    {
        var twice = element.GetParameters(levelParameter).Count > 1;
        var target = twice ? null : element.LookupParameter(levelParameter);
        if (twice) ambiguousLevelParameter++;
        else if (target == null || target.IsReadOnly) missingLevelParameter++;
        else
        {
            var levelId = element.LevelId;
            var level = levelId == ElementId.InvalidElementId ? null : doc.GetElement(levelId);
            if (level != null)
            {
                try { target.Set(level.Name); touched = true; }
                catch { }
            }
        }
    }

    if (touched) written++;
}

if (missingRoomParameter > 0)
{
    refused.Add(string.Format("{0} element(s) have no writable '{1}' parameter. This fills parameters; "
        + "it does not create them - bind a project or shared parameter first",
        missingRoomParameter, roomParameter));
}
if (missingLevelParameter > 0)
{
    refused.Add(string.Format("{0} element(s) have no writable '{1}' parameter",
        missingLevelParameter, levelParameter));
}
if (ambiguousRoomParameter > 0)
{
    refused.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}' - usually a "
        + "shared or project parameter bound beside a built-in one of the same name. Revit would pick "
        + "one of them at random, so NEITHER was filled", ambiguousRoomParameter, roomParameter));
}
if (ambiguousLevelParameter > 0)
{
    refused.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}', so NEITHER "
        + "was filled", ambiguousLevelParameter, levelParameter));
}
if (noRoomFound.Count > 0)
{
    refused.Add(string.Format("{0} element(s) sit in no {1} at all - outside the boundaries, or in an "
        + "area where none is placed", noRoomFound.Count, label));
}

refused.Add(string.Format("{0} element(s) written, using {1}S. Architecture puts people in ROOMS and "
    + "MEP calculates in SPACES; a model can hold both in the same place with different names, and "
    + "picking the wrong one is the commonest way a location register comes out wrong", written, label));
