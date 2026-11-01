// NOT STANDALONE. Assumes `doc`, `levelId`, `placeKind`, `phaseId`,
// `planViewId`, `points`, `areaPlanName`, `names` and `numbers` are in scope;
// leaves `created`, `refused`, `unbounded`, `untagged`, `pointsRefused` and
// `refusedPoints` behind.
//
// VERSION 3 (2026-11-01) - PLACE_AREAS MERGED IN. A THIRD KIND, `area`:
// Revit's Area tool for many at once. One area at each point, placed with
// NewArea in an AREA PLAN CHOSEN BY NAME (`areaPlanName`, matched as the
// Project Browser shows it, never a floor plan), each inside a region the
// area boundary lines enclose; `names` and `numbers`, comma separated, go on
// in the same order as the points, and an empty value leaves Revit's. What
// that card had, all of it here:
//
//   AN AREA PLACED WHERE NO BOUNDARY ENCLOSES IT HAS NO SIZE. Everything is
//   placed, Revit regenerates once, and each is measured; one with no size is
//   named in `unbounded` (that card's `unenclosed`), left for the modeller.
//
//   A POINT REVIT REFUSES is counted in `pointsRefused` and named in
//   `refusedPoints`, the others still placed - that card's rule and this
//   card's. A point of two numbers - "x,y" - is taken, as that card took it.
//
//   AN AREA HAS NO PHASE. The phase requirement is for rooms and spaces only,
//   and is unchanged for them: `phaseId` may now be left out ("none"), and a
//   room or a space with none named is still refused. The KIND must still be
//   said - an area is never inferred from an area plan being named.
//
//   NEW FOR THE MERGE, THE SAFER SIDE: a name or a number that does not read
//   back as given undoes the whole call (that card set them and did not look);
//   a level named that is not the area plan's own is refused; names or
//   numbers handed in for rooms or spaces are refused rather than ignored.
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
        // Version 3: two numbers are taken too, as PLACE_AREAS took them -
        // only the plan position is used either way.
        var ok = bits.Length == 3 || bits.Length == 2;
        for (var i = 0; ok && i < bits.Length; i++)
            ok = double.TryParse(bits[i].Trim(), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out v[i])
                 && !double.IsNaN(v[i]) && !double.IsInfinity(v[i]) && Math.Abs(v[i]) <= 100000000.0;
        if (!ok)
        {
            pointProblem = "\"" + one + "\" is not a point - two or three numbers in MILLIMETRES, with a "
                + "semicolon between points: \"3000,2000,0; 9000,2000,0\"";
            break;
        }
        chosen.Add(new XYZ(v[0] / 304.8, v[1] / 304.8, v[2] / 304.8));
    }
    if (pointProblem == null && chosen.Count == 0)
        pointProblem = "points were asked for and none was given";
}

// What was placed, measured below whichever kind it was.
ICollection<ElementId> placed = null;

var kind = (placeKind ?? "").Trim().ToLowerInvariant();

var wantSpaces = kind.Contains("space");
var wantRooms = kind.Contains("room");
// VERSION 3, from PLACE_AREAS.
var wantAreas = kind.Contains("area");
var kindsSaid = (wantSpaces ? 1 : 0) + (wantRooms ? 1 : 0) + (wantAreas ? 1 : 0);

// Left out ("none"), either one is InvalidElementId and reads back null.
var level = levelId == null || levelId == ElementId.InvalidElementId ? null : doc.GetElement(levelId) as Level;
var phase = phaseId == null || phaseId == ElementId.InvalidElementId ? null : doc.GetElement(phaseId) as Phase;

// VERSION 3. Names and numbers in point order, for areas; "none" or empty is
// none, which leaves Revit's.
Func<string, List<string>> commaList = text =>
{
    var whole = (text ?? "").Trim();
    if (whole.Length == 0 || string.Equals(whole, "none", StringComparison.OrdinalIgnoreCase)) return new List<string>();
    return whole.Split(',').Select(each => each.Trim()).Where(each => each.Length > 0).ToList();
};
var nameList = commaList(names);
var numberList = commaList(numbers);
var planText = (areaPlanName ?? "").Trim();
if (string.Equals(planText, "none", StringComparison.OrdinalIgnoreCase)) planText = "";

if (kindsSaid == 0)
{
    refused = "say which - rooms, spaces or areas. Rooms and spaces look the same on a plan "
            + "and are different elements: MEP loads and airflow run on SPACES, and placing "
            + "rooms instead gives a drawing that looks finished with nothing to "
            + "calculate on. Areas go in an area plan, for the area schemes";
}
else if (kindsSaid > 1)
{
    refused = "rooms, spaces and areas are placed separately - ask for one, then the other";
}
else if (!wantAreas && (nameList.Count > 0 || numberList.Count > 0))
{
    // VERSION 3. Names and numbers are placed on AREAS here, as PLACE_AREAS
    // placed them; refused rather than silently dropped for rooms or spaces.
    refused = "names and numbers are given to areas as they are placed; rooms and spaces are "
            + "placed here without them. Nothing was placed";
}
else if (wantAreas)
{
    // ---- VERSION 3: AREAS, PLACE_AREAS' JOB -----------------------------
    // An AREA PLAN chosen by name - never a floor plan of the same name.
    ViewPlan areaPlan = null;
    if (planText.Length > 0)
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
        {
            var candidate = element as ViewPlan;
            if (candidate != null && !candidate.IsTemplate && candidate.ViewType == ViewType.AreaPlan
                && string.Equals(candidate.Name, planText, StringComparison.OrdinalIgnoreCase)) { areaPlan = candidate; break; }
        }
    }

    if (planText.Length == 0)
        refused = "areas are placed in an AREA PLAN - name it (areaPlanName). Nothing was placed";
    else if (areaPlan == null)
        refused = "No area plan is named \"" + planText + "\". Nothing was placed";
    else if (levelId != null && levelId != ElementId.InvalidElementId
             && (level == null || areaPlan.GenLevel == null || level.Id != areaPlan.GenLevel.Id))
        refused = "the level named is not the level of the area plan \"" + areaPlan.Name + "\" - an area is "
                + "placed on its area plan's own level. Leave the level out, or name that plan's. Nothing was placed";
    else if (!atPoints)
        refused = "areas are placed AT POINTS - one inside each region the area boundary lines enclose. "
                + "Nothing was placed";
    else if (pointProblem != null)
        refused = pointProblem + ". Nothing was placed";
    else if (nameList.Count > chosen.Count || numberList.Count > chosen.Count)
        // Names and numbers go on in point order; more of them than points
        // means the two lists do not line up, and a guess would misname.
        refused = string.Format("{0} point(s), {1} name(s) and {2} number(s) were given - names and numbers go on "
                + "in point order, so there cannot be more of them than points. Nothing was placed",
                chosen.Count, nameList.Count, numberList.Count);
    else
    {
        var one = new List<ElementId>();
        var wrongLabel = new List<string>();
        for (var i = 0; i < chosen.Count; i++)
        {
            var where = new UV(chosen[i].X, chosen[i].Y);
            var label = string.Format("point {0} ({1:0}, {2:0})", i + 1, chosen[i].X * 304.8, chosen[i].Y * 304.8);
            Area made = null;
            try { made = doc.Create.NewArea(areaPlan, where); }
            catch (Exception failure) { refusedAt.Add(label + " - " + failure.Message); made = null; }
            if (made == null)
            {
                pointsRefused++;
                if (refusedAt.Count < pointsRefused) refusedAt.Add(label + " - Revit placed nothing");
                continue;
            }
            one.Add(made.Id);

            // Name and number in point order, READ BACK: one that does not
            // read as given undoes the call (below).
            if (i < nameList.Count)
            {
                var named = made.get_Parameter(BuiltInParameter.ROOM_NAME);
                if (named != null && !named.IsReadOnly) named.Set(nameList[i]);
                if (named == null || named.AsString() != nameList[i])
                    wrongLabel.Add(label + " did not take the name '" + nameList[i] + "'");
            }
            if (i < numberList.Count)
            {
                made.Number = numberList[i];
                if (made.Number != numberList[i])
                    wrongLabel.Add(label + " did not take the number '" + numberList[i] + "'");
            }
        }
        if (wrongLabel.Count > 0)
            throw new InvalidOperationException("Areas placed in \"" + areaPlan.Name + "\": "
                + string.Join("; ", wrongLabel) + ". An area named or numbered wrongly reports under the wrong "
                + "name in every area schedule, so nothing this call did is kept");
        placed = one;
    }
}
else if (level == null)
{
    refused = levelId == null || levelId == ElementId.InvalidElementId
        ? "name the level - rooms and spaces are placed level by level"
        : "that is not a level in this project, and these are placed level by level";
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
}

// MEASURED, WHICHEVER KIND WAS PLACED (version 3 moved this out of the room
// and space branch so areas are measured by the same rule).
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
        // VERSION 3: an AREA is measured as PLACE_AREAS measured it, by its
        // own Area - no size is no boundary enclosing it.
        if (wantAreas)
        {
            var spatial = element as SpatialElement;
            if (spatial == null || spatial.Area <= 1e-9) unbounded.Add(id);
            continue;
        }
        var area = element.get_Parameter(BuiltInParameter.ROOM_AREA);
        if (area != null && area.AsDouble() <= 1e-9) unbounded.Add(id);
    }
}

// VERSION 2: a point Revit refused is named, the others still placed.
refusedPoints = string.Join("; ", refusedAt);
