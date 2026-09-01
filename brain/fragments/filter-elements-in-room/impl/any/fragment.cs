// NOT STANDALONE. Assumes `room` and `elements` are in scope; leaves
// `elements`, `outside` and `noPoint` behind.
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

var inside = new List<Element>();
var outside = new List<ElementId>();
var noPoint = new List<ElementId>();

// ROOM OR SPACE, the same as READ_ROOM_GEOMETRY. An MEP model carries Spaces
// where the architectural model carries Rooms, and a fragment that handled only
// one would answer nothing on half the jobs it is asked about. Each has its own
// containment test and they are not interchangeable.
var asRoom = room as Room;
var asSpace = room as Space;

foreach (var element in elements)
{
    if (element == null) continue;

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
}

elements = inside;
