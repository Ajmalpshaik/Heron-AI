// NOT STANDALONE. Assumes `doc`, `level`, `points` and `asSpace` are in scope,
// and leaves `created`, `unenclosed` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A POINT OUTSIDE AN ENCLOSED REGION STILL MAKES A ROOM.
//
// Revit creates it, gives it no area, and reports nothing. Those rooms then
// sit in the schedule looking like real ones - so every room placed here is
// measured afterwards and the ones with no area are named. That is the same
// state the model-wide sweep looks for; this reports it for what it just made.
//
// A SPACE IS NOT A ROOM.
//
// Loads, airflow and every space-based schedule read Spaces; Rooms are the
// architectural element. They are indistinguishable on screen, which is why
// the choice is a stated input rather than a guess from context.
//
// ONLY THE PLAN POSITION OF EACH POINT IS USED. The level decides the height,
// and a Z handed in here would be silently ignored - so it is dropped
// deliberately rather than half-used.

var created = new List<ElementId>();
var unenclosed = new List<ElementId>();
int refused = 0;

foreach (var point in points)
{
    if (point == null) { refused++; continue; }

    var where = new UV(point.X, point.Y);

    Element placed = null;
    try
    {
        placed = asSpace
            ? (Element)doc.Create.NewSpace(level, where)
            : (Element)doc.Create.NewRoom(level, where);
    }
    catch { }

    if (placed == null) { refused++; continue; }

    created.Add(placed.Id);

    // Measured, not assumed. A room with no area was placed somewhere the
    // boundaries do not close, and it looks like every other room in the
    // schedule.
    var spatial = placed as SpatialElement;
    if (spatial == null) continue;

    double area = 0;
    try { area = spatial.Area; } catch { }
    if (area <= 0) unenclosed.Add(placed.Id);
}
