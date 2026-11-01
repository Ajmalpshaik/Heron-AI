// NOT STANDALONE. Assumes `doc`, `levelId`, `placeKind`, `phaseId`,
// `planViewId` and `points` are in scope; leaves `created`, `refused`,
// `unbounded`, `untagged`, `pointsRefused` and `refusedPoints` behind.
//
// VERSION 2 (2026-11-01) - PLACE_ROOM_AT_POINT MERGED IN. As well as one in
// every enclosed area, a room or a space can now go AT THE POINTS CHOSEN -
// `points`, "x,y,z; x,y,z" in millimetres, only the plan position used, the
// level deciding the height. What that card had, and its rules made safer:
//
//   A POINT OUTSIDE AN ENCLOSED REGION STILL MAKES A ROOM, with no area and
//   no error from Revit. So everything is placed, Revit regenerates once, and
//   each is measured: one with no area is named in `unbounded`.
//
//   A POINT REVIT REFUSES IS COUNTED in `pointsRefused`, named in
//   `refusedPoints`, and the
//   other points are still placed - that card's rule.
//
//   THE PHASE AND THE KIND ARE REQUIRED AT A POINT TOO - this card's rules,
//   the safer ones. PLACE_ROOM_AT_POINT used the calls that take no phase.
//   A space goes in with its phase named (NewSpace with a Phase). A room has
//   no call that takes a point AND a phase, so it is placed at the point and
//   its phase READ BACK; one that came out in another phase undoes the whole
//   call - which phase Revit gives it is NEEDS REAL REVIT.
//
//   NO PLAN VIEW IS NEEDED AT A POINT. NewSpace with a point takes none;
//   only filling every area of a level from a plan does.
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
string untagged = null;
// VERSION 2, from PLACE_ROOM_AT_POINT.
int pointsRefused = 0;
string refusedPoints = "";
var refusedAt = new List<string>();

// THE POINTS, IF ANY WERE CHOSEN - three real numbers in millimetres inside
// 100 km each, to feet. Only the plan position is used.
var pointsText = (points ?? "").Trim();
var atPoints = pointsText.Length > 0 && !string.Equals(pointsText, "none", StringComparison.OrdinalIgnoreCase);
var chosen = new List<XYZ>();
string pointProblem = null;
if (atPoints)
{
    foreach (var piece in pointsText.Split(';'))
    {
        var one = piece.Trim();
        if (one.Length == 0) continue;
        var bits = one.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var v = new double[3];
        var ok = bits.Length == 3;
        for (var i = 0; ok && i < 3; i++)
            ok = double.TryParse(bits[i].Trim(), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out v[i])
                 && !double.IsNaN(v[i]) && !double.IsInfinity(v[i]) && Math.Abs(v[i]) <= 100000000.0;
        if (!ok)
        {
            pointProblem = "\"" + one + "\" is not a point - three numbers in MILLIMETRES, with a semicolon "
                + "between points: \"3000,2000,0; 9000,2000,0\"";
            break;
        }
        chosen.Add(new XYZ(v[0] / 304.8, v[1] / 304.8, v[2] / 304.8));
    }
    if (pointProblem == null && chosen.Count == 0)
        pointProblem = "points were asked for and none was given";
}

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
else if (pointProblem != null)
{
    refused = pointProblem + ". Nothing was placed";
}
else
{
    ICollection<ElementId> placed = null;

    if (atPoints)
    {
        // VERSION 2, PLACE_ROOM_AT_POINT'S JOB - one at each point chosen.
        var one = new List<ElementId>();
        var wrongPhase = new List<string>();
        for (var i = 0; i < chosen.Count; i++)
        {
            var where = new UV(chosen[i].X, chosen[i].Y);
            var label = string.Format("point {0} ({1:0}, {2:0})", i + 1, chosen[i].X * 304.8, chosen[i].Y * 304.8);
            Element made = null;
            try
            {
                made = wantSpaces
                    ? (Element)doc.Create.NewSpace(level, phase, where)
                    : (Element)doc.Create.NewRoom(level, where);
            }
            catch (Exception failure)
            {
                refusedAt.Add(label + " - " + failure.Message);
                made = null;
            }
            if (made == null)
            {
                pointsRefused++;
                if (refusedAt.Count < pointsRefused) refusedAt.Add(label + " - Revit placed nothing");
                continue;
            }
            one.Add(made.Id);

            // A ROOM HAS NO CALL THAT TAKES A POINT AND A PHASE, so its phase is
            // read back, and one in another phase undoes the call (below).
            if (!wantSpaces)
            {
                var held = made.get_Parameter(BuiltInParameter.ROOM_PHASE_ID);
                var heldId = held == null ? ElementId.InvalidElementId : held.AsElementId();
                if (heldId != phase.Id)
                {
                    var other = heldId == ElementId.InvalidElementId ? null : doc.GetElement(heldId);
                    wrongPhase.Add(string.Format("{0} came out in phase '{1}'", label,
                        other == null ? "none Revit names" : other.Name));
                }
            }
        }
        if (wrongPhase.Count > 0)
            throw new InvalidOperationException(string.Format(
                "Asked for rooms in phase '{0}': {1}. A room in the wrong phase reports on the wrong half of "
                + "the building in every room schedule, so nothing this call did is kept", phase.Name,
                string.Join("; ", wrongPhase)));
        placed = one;
        if (wantSpaces && one.Count > 0)
            untagged = "spaces placed at points are placed without tags - TAG_ROOMS tags them";
    }
    else if (wantSpaces)
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
            // NO SPACE TAGS FAMILY, NO NewSpaces2. It tags every space it makes
            // in the view, and with no Space Tags type loaded it THROWS ("No
            // Space Tags family is loaded in the project") instead of placing
            // them untagged - and an MEP model started from an architectural
            // template has none. MEASURED 2026-10-19 on Project4, Revit 2020:
            // both legs of the first proof threw before anything was placed.
            //
            // So with none loaded, each enclosed area of the level's plan
            // topology gets its own NewSpace, which makes no tag, and an area a
            // space already sits in is passed over - as NewSpaces2 does. The
            // point is tested a foot above the level because a space starts AT
            // its level and the level plane itself is its boundary.
            var spaceTag = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaceTags)
                .WhereElementIsElementType()
                .FirstElement();

            if (spaceTag != null)
            {
                placed = doc.Create.NewSpaces2(level, phase, view);
            }
            else
            {
                var one = new List<ElementId>();

                foreach (PlanCircuit circuit in doc.get_PlanTopology(level, phase).Circuits)
                {
                    var inside = circuit.GetPointInside();
                    var probe = new XYZ(inside.U, inside.V, level.ProjectElevation + 1.0);

                    if (doc.GetSpaceAtPoint(probe, phase) != null) continue;

                    var space = doc.Create.NewSpace(level, phase, inside);
                    if (space != null) one.Add(space.Id);
                }

                placed = one;
                if (one.Count > 0)
                    untagged = "no Space Tags family is loaded in this project, so the spaces "
                             + "were placed without tags. Load one (Insert > Load Family > "
                             + "Annotations) and TAG_ROOMS tags them";
            }
        }
    }
    else
    {
        placed = doc.Create.NewRooms2(level, phase);
    }

    if (refused == null && atPoints && (placed == null || placed.Count == 0))
    {
        refused = "nothing was placed - Revit refused every point: " + string.Join("; ", refusedAt);
    }
    else if (refused == null && (placed == null || placed.Count == 0))
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

// VERSION 2: a point Revit refused is named, the others still placed.
refusedPoints = string.Join("; ", refusedAt);
