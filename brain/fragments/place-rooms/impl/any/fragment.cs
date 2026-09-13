// NOT STANDALONE. Assumes `doc`, `levelId`, `placeKind`, `phaseId` and
// `planViewId` are in scope; leaves `created`, `refused` and `unbounded`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ROOMS AND SPACES ARE NOT PLACED THE SAME WAY, and this fragment was written
// assuming they were. The compile gate refused it on all eight releases:
//
//   NewRooms2(Level)                  a room needs only the level
//   NewSpaces2(Level, Phase, View)    a space needs a PHASE and a PLAN VIEW
//
// Identical on 2020 and on 2026, so it is not a version difference - it is what
// the two elements ARE. A space belongs to a phase and is placed from a view; a
// room can be placed without either being named. Worth keeping as a modelling
// fact and not just as a signature.
//
// THE PHASE IS ASKED FOR EITHER WAY. Rooms have the shorter overload, and using
// it takes the document's current phase silently - which on a refurbishment job
// puts every room in "Existing" when the work is "New Construction", and every
// room-based schedule then reports on the wrong half of the building. So the
// phase is required and the two-argument overload is used for rooms too.
//
// ROOM OR SPACE IS ASKED FOR, NEVER GUESSED. They look identical on a plan and
// do different jobs: the architect's model carries Rooms, MEP loads and airflow
// run on Spaces. Placing the wrong kind gives a drawing that looks finished and
// a load calculation with nothing to run on.
//
// AN UNBOUNDED RESULT IS THE USEFUL HALF OF THE ANSWER. A room that comes out
// "Not Enclosed" is a GAP IN THE WALLS - and the same gap is why the room next
// door came out enormous, because Revit ran the boundary through it and
// swallowed the corridor. That list is a snag list.

var created = new List<ElementId>();
var unbounded = new List<ElementId>();
string refused = null;

var kind = (placeKind ?? "").Trim().ToLowerInvariant();

var wantSpaces = kind.Contains("space");
var wantRooms = kind.Contains("room");

var level = doc.GetElement(levelId) as Level;
var phase = doc.GetElement(phaseId) as Phase;

if (!wantSpaces && !wantRooms)
{
    refused = "say which - rooms or spaces. They look the same on a plan and are "
            + "different elements: MEP loads and airflow run on SPACES, and placing "
            + "rooms instead gives a drawing that looks finished with nothing to "
            + "calculate on";
}
else if (wantSpaces && wantRooms)
{
    refused = "rooms and spaces are placed separately - ask for one, then the other";
}
else if (level == null)
{
    refused = "that is not a level in this project, and these are placed level by level";
}
else if (phase == null)
{
    refused = "name the phase. Taking the document's current one silently puts every "
            + "room into Existing on a refurbishment job, and every room-based schedule "
            + "then reports on the wrong half of the building";
}
else
{
    ICollection<ElementId> placed = null;

    if (wantSpaces)
    {
        // A space is placed FROM a view - Revit's own Space tool needs an
        // active plan, and the API says the same thing by requiring one.
        var view = doc.GetElement(planViewId) as ViewPlan;

        if (view == null)
        {
            refused = "spaces are placed from a PLAN VIEW and none was given. That is "
                    + "not true of rooms, which need only the level - it is a real "
                    + "difference between the two elements, not a missing argument";
        }
        else
        {
            placed = doc.Create.NewSpaces2(level, phase, view);
        }
    }
    else
    {
        placed = doc.Create.NewRooms2(level, phase);
    }

    if (refused == null && (placed == null || placed.Count == 0))
    {
        refused = "nothing was placed - there is no enclosed area on this level. Rooms "
                + "and spaces need bounding elements, and walls that do not meet "
                + "enclose nothing";
    }
    else if (placed != null)
    {
        // A ROOM HAS NO AREA UNTIL REVIT WORKS ONE OUT, and it does that on
        // regeneration rather than on creation. Reading ROOM_AREA in the same
        // breath as NewRooms2 therefore returns 0 for every room just placed -
        // including the ones sitting correctly inside four walls - and 0 is
        // precisely the test below for "Not Enclosed". So a healthy run reported
        // every room it made as unbounded, which is the opposite of the truth.
        //
        // MEASURED 2026-09-13 on `test projject`: a 7200 x 5200 x 3000 box built
        // on Level 2, read back and confirmed at Z 4000 to 7000, gave
        // `created 1, unbounded 1`.
        doc.Regenerate();

        foreach (var id in placed)
        {
            if (id == null || id == ElementId.InvalidElementId) continue;

            created.Add(id);

            var element = doc.GetElement(id);
            if (element == null) continue;

            // Zero area is exactly the "Not Enclosed" condition - the same test
            // Revit's own schedules use, and it needs no assumption about which
            // elements are bounding.
            var area = element.get_Parameter(BuiltInParameter.ROOM_AREA);
            if (area != null && area.AsDouble() <= 1e-9) unbounded.Add(id);
        }
    }
}
