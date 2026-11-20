// NOT STANDALONE. Assumes `room` and `elements` are in scope; leaves
// `elements`, `outside`, `noPoint` and `doorsAndWindows` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// REVIT'S OWN POINT-IN-ROOM TEST, NOT A BOUNDING BOX. A room is a VOLUME
// following its boundary, and an L-shaped room's bounding box covers ground
// that is not in the room. A box test therefore puts the neighbouring room's
// diffuser into this room's schedule - quietly, and it looks right until
// somebody counts on site.
//
// ONE POINT PER ELEMENT, AND THAT IS A REAL LIMIT. A point-based element uses
// its point; a curve-based one uses its MIDPOINT. A duct crossing three rooms
// belongs to whichever contains its midpoint. Deciding how much of a run must
// be inside to count is a question about the JOB - a takeoff wants length per
// room, a terminal count wants the point - so this reports the point test and
// leaves that judgement to the caller rather than inventing a rule.
//
// THE ROOM'S LEVEL IS NOT USED AS A FILTER. A room has a height, so services
// above the ceiling are genuinely inside it, and filtering by level would drop
// exactly the elements somebody is asking about.
//
// THE NARROWED SET REPLACES `elements`, which is the pipeline's currency.
//
// VERSION 2 (2026-11-20, row 5b-1485): A DOOR OR WINDOW IN A WALL IS JUDGED BY
// ITS FROM ROOM AND TO ROOM, NOT BY ITS POINT. Its point sits in the wall, on
// the room's boundary, so the point test put all 13 windows of a Revit 2024
// room "outside" it. A door or window (a family instance of Doors or Windows
// hosted in a wall) now counts as in the room when either its From Room or its
// To Room is that room, asked in the ROOM'S OWN PHASE (its Phase property,
// ROOM_PHASE_ID) - this card names no phase of its own, and the point test
// always asked the room itself - and `doorsAndWindows` says which side,
// "window W12 (id 351204): its To Room". Where that cannot be asked - a
// Space, since From Room and To Room name rooms, not spaces; a room whose
// phase cannot be read; a room in another model; a From and To Revit will not
// give - the door or window keeps the point test, and its line says why.
// Every other element keeps the point test.

var inside = new List<Element>();
var outside = new List<ElementId>();
var noPoint = new List<ElementId>();
// VERSION 2: each door and window in a wall, and how it was judged.
var doorsAndWindows = new List<string>();

// ROOM OR SPACE, the same as READ_ROOM_GEOMETRY. An MEP model carries Spaces
// where the architectural model carries Rooms, and a fragment that handled only
// one would answer nothing on half the jobs it is asked about. Each has its own
// containment test and they are not interchangeable.
var asRoom = room as Room;
var asSpace = room as Space;

// VERSION 2: THE ROOM'S OWN PHASE, the one a door's From Room and To Room are
// asked in. Null when it cannot be read; those doors keep the point test.
Phase roomPhase = null;
if (asRoom != null)
{
    try
    {
        var phaseParameter = asRoom.get_Parameter(BuiltInParameter.ROOM_PHASE_ID);
        var phaseId = phaseParameter == null ? null : phaseParameter.AsElementId();
        if (phaseId != null && phaseId != ElementId.InvalidElementId)
            roomPhase = asRoom.Document.GetElement(phaseId) as Phase;
    }
    catch (Exception) { roomPhase = null; }
}
var doorsCategory = new ElementId(BuiltInCategory.OST_Doors);
var windowsCategory = new ElementId(BuiltInCategory.OST_Windows);

foreach (var element in elements)
{
    if (element == null) continue;

    // VERSION 2: A DOOR OR WINDOW IN A WALL - by its From Room and To Room.
    var opening = element as FamilyInstance;
    string openingLabel = null;
    try
    {
        if (opening != null && opening.Category != null && opening.Host is Wall
            && (opening.Category.Id == doorsCategory || opening.Category.Id == windowsCategory))
        {
            var markParameter = opening.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            var mark = markParameter == null ? "" : (markParameter.AsString() ?? "").Trim();
            openingLabel = (opening.Category.Id == doorsCategory ? "door " : "window ")
                + (mark.Length > 0 ? mark + " (id " + opening.Id + ")" : "id " + opening.Id + " (no Mark)");
        }
    }
    catch (Exception) { openingLabel = null; }

    string byPointBecause = null;
    if (openingLabel != null)
    {
        if (asRoom == null)
            byPointBecause = "From Room and To Room name rooms, not spaces";
        else if (roomPhase == null)
            byPointBecause = "the room's phase could not be read";
        else if (!asRoom.Document.Equals(opening.Document))
            byPointBecause = "the room is in another model";
        else
        {
            Room fromRoom = null;
            Room toRoom = null;
            var asked = false;
            try
            {
                fromRoom = opening.get_FromRoom(roomPhase);
                toRoom = opening.get_ToRoom(roomPhase);
                asked = true;
            }
            catch (Exception) { asked = false; }
            if (!asked)
                byPointBecause = "Revit would not give its From Room and To Room";
            else
            {
                var fromHere = fromRoom != null && fromRoom.Id == asRoom.Id;
                var toHere = toRoom != null && toRoom.Id == asRoom.Id;
                if (fromHere || toHere)
                {
                    inside.Add(element);
                    doorsAndWindows.Add(openingLabel + ": its " + (fromHere && toHere ? "From Room and its To Room"
                        : fromHere ? "From Room" : "To Room"));
                }
                else
                {
                    outside.Add(element.Id);
                    doorsAndWindows.Add(openingLabel + ": neither its From Room nor its To Room is this room");
                }
                continue;
            }
        }
    }

    XYZ point = null;

    var atPoint = element.Location as LocationPoint;
    if (atPoint != null)
    {
        point = atPoint.Point;
    }
    else
    {
        var atCurve = element.Location as LocationCurve;
        if (atCurve != null && atCurve.Curve != null)
        {
            point = atCurve.Curve.Evaluate(0.5, true);
        }
    }

    if (point == null || (asRoom == null && asSpace == null))
    {
        // Nothing to test with. Named rather than assumed outside - assuming
        // would drop it from a schedule with no trace, which is the failure
        // that only shows up when somebody counts on site.
        noPoint.Add(element.Id);
        continue;
    }

    bool within;
    try
    {
        within = asRoom != null
            ? asRoom.IsPointInRoom(point)
            : asSpace.IsPointInSpace(point);
    }
    catch (Exception)
    {
        noPoint.Add(element.Id);
        continue;
    }

    if (within) inside.Add(element);
    else outside.Add(element.Id);
    if (byPointBecause != null)
        doorsAndWindows.Add(openingLabel + ": judged by its point, " + (within ? "inside" : "outside") + " - "
            + byPointBecause);
}

elements = inside;
