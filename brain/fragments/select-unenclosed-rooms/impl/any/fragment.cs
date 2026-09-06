// NOT STANDALONE. Assumes `doc`, `includeRooms` and `includeSpaces` are in
// scope, and leaves `elements`, `unplaced`, `unenclosed` and `roomsChecked`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// COLLECTED BY CATEGORY, READ THROUGH THE BASE TYPE.
//
// Rooms and Spaces are two categories and one job: number, name, area and
// location are all declared on the type they share. Collecting by category and
// reading through that base means one pass covers both, and the fragment never
// has to name the category-specific classes.
//
// ZERO AREA IS TWO DIFFERENT FAULTS.
//
//   no location    NEVER PLACED. It exists in the schedule and is not in the
//                  model. Deleted, or placed
//   a location     NOT ENCLOSED. Placed where the boundaries do not close.
//                  A wall, a gap, or a room-bounding setting
//
// Both read as "zero area" in a schedule, and telling somebody to go looking
// for the walls around a room that was never placed wastes the afternoon that
// this fragment exists to save.
//
// REDUNDANT ROOMS ARE NOT DETECTED HERE. Two rooms in one enclosed region is a
// third fault, and Revit reports it as a warning - which is a better answer
// than anything that could be re-derived from geometry, and it already has a
// fragment.

var elements = new List<Element>();
var unplaced = new List<ElementId>();
var unenclosed = new List<ElementId>();

var candidates = new List<SpatialElement>();

if (includeRooms)
{
    candidates.AddRange(new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Rooms)
        .WhereElementIsNotElementType()
        .OfType<SpatialElement>());
}

if (includeSpaces)
{
    candidates.AddRange(new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_MEPSpaces)
        .WhereElementIsNotElementType()
        .OfType<SpatialElement>());
}

int roomsChecked = candidates.Count;

foreach (var spatial in candidates)
{
    if (spatial == null) continue;

    double area = 0;
    try { area = spatial.Area; } catch { }
    if (area > 0) continue;

    bool placed = false;
    try { placed = spatial.Location != null; } catch { }

    if (placed) unenclosed.Add(spatial.Id); else unplaced.Add(spatial.Id);
    elements.Add(spatial);
}
