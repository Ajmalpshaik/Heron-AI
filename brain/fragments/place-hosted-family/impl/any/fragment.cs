// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `toRoomAt`, `hingeAt`,
// `level` and `sillHeight` are in scope; leaves `placed`, `alreadyThere`,
// `failed`, `noWall`, `twoWalls`, `flipped`, `handFlipped`, `toRoomSwapped`,
// `wrongRoom`, `wrongHinge`, `toRoomWrong`, `sills`, `sillWrong`,
// `noSillParameter`, `sillTooHigh` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// THE WALL IS FOUND AT EACH POINT, NEVER NAMED. Version 1 took the host as one
// element, and a wall instance has no name anybody could type - the add-in
// refuses a typed name for one particular element - so it was never run, and on
// 2026-09-22, in Project4, a door sent through PLACE_FAMILY_INSTANCES instead
// answered "placed 1, failed 0" and went in FREE-STANDING: right in plan,
// cutting nothing. So each point is asked which wall it sits in: the
// straight walls on the level whose location line passes within half their
// width of the point, away from their ends. One wall, and the door goes into
// that wall. None, or two equally near, and that point is named and skipped -
// a door is never guessed into a wall.
//
// A DOOR ALREADY AT THE POINT IS NOT DOUBLED. One of this category, hosted in
// that same wall within 50 mm of the point, is taken as the door asked for:
// nothing new is placed, and it is brought into line below instead. So a
// re-run cannot stack a second door on the first - Revit would only warn - and
// re-running is also how a door placed earlier is corrected.
//
// WHICH ROOM IT OPENS INTO IS STATED, NEVER ASSUMED (D-33), AND IT IS TWO
// SETTINGS, NOT ONE. Each point comes with a second point inside the room the
// door should open into.
//   1. THE SWING. Measured 2026-09-24 in Project1 (Revit 2024) on
//      M_Single-Flush: the leaf's plan swing arc lies on the door's FACING
//      side. So the room a short way out along FacingOrientation must be the
//      room asked for, or the door is flipped once and asked again.
//   2. THE HINGE. A third point per door, on the side of the jamb the hinge
//      belongs at - "against the wall", so the open leaf lies along it. Read
//      off the door's own plan swing arc, whose centre IS the hinge, in a floor
//      plan of the level; flipping the hand moves it to the other jamb and
//      leaves the swing side alone. A door with no swing arc in any plan, or
//      two arcs a jamb apart (a pair of leaves), is named rather than guessed.
//   3. THE TO ROOM - the room door schedules and door numbers read. Revit sets
//      it when the door is placed and a facing flip does NOT move it: the same
//      day, four doors flipped to swing into their offices still read "To
//      Corridor". So it is read separately and, when wrong, swapped with
//      Revit's own From/To flip.
// A door still wrong on any count is named, never counted as right.
//
// EVERY DOOR IS READ BACK. Its host must be the wall that was found - a door
// Revit hosted somewhere else, or nowhere, is removed again inside the same
// undo and its point named. A count of calls that returned is not evidence
// that a door is in a wall.
//
// VERSION 3: THE SILL HEIGHT, ASKED FOR OR READ - NEVER TRUSTED. `sillHeight`
// is millimetres above the level, or BLANK. Blank changes nothing about
// version 2: the sill is left as Revit placed it, and what it reads is still
// reported per door, because a window placed through the API has been said to
// land with its sill at 0 - a hypothesis nobody here has measured, so the
// reply carries the reading that settles it. Given, it is:
//   1. CHECKED BEFORE PLACING, where it is cheap: the level's elevation + the
//      sill + the TYPE's Height (FAMILY_HEIGHT_PARAM, the built-in Doors and
//      Windows both use) against the top of the wall's own bounding box. A
//      head above the wall's top skips that point - `sillTooHigh` - and
//      nothing is placed or touched there. A type whose Height cannot be read
//      (an instance Height) is not refused: the reply says the head went
//      unchecked.
//   2. SET on INSTANCE_SILL_HEIGHT_PARAM, the built-in, and nothing else. A
//      family without one, or with it read-only, is NOT given another
//      parameter found by name - a typed name may repeat (row 5b-203). A door
//      this call placed is removed again inside the same undo and named in
//      `noSillParameter`; a door already there is left as it was and named.
//   3. READ BACK after a regenerate, in millimetres, and set beside what was
//      asked in `sills`. More than 1 mm out is named in `sillWrong`.
//
// NonStructural is passed deliberately - a door is not a structural member, and
// PLACE_STRUCTURAL_FAMILY is where that decision belongs.

var placed = new List<ElementId>();
var alreadyThere = new List<string>();
var failed = 0;
var noWall = new List<string>();
var twoWalls = new List<string>();
var flipped = 0;
var handFlipped = 0;
var toRoomSwapped = 0;
var wrongRoom = new List<string>();
var wrongHinge = new List<string>();
var toRoomWrong = new List<string>();
var sills = new List<string>();
var sillWrong = new List<string>();
var noSillParameter = new List<string>();
var sillTooHigh = new List<string>();
var findings = new List<string>();

// One millimetre, and fifty, in the feet everything here is measured in.
const double Slack = 1.0 / 304.8;
const double SamePlace = 50.0 / 304.8;

string firstRefusal = null;

// THE SILL ASKED FOR, in feet, or null for "leave it as Revit places it".
// Read before anything is placed, so a sill that cannot be read stops the run
// whole rather than after the first door.
var sillText = (sillHeight ?? "").Trim();
double? sillAsked = null;
string sillRefusal = null;
if (sillText.Length > 0)
{
    double sillMm;
    if (!double.TryParse(sillText, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out sillMm))
        sillRefusal = "'" + sillText + "' is not a sill height. Give it in millimetres - 900 - "
            + "or leave it blank to keep the sill Revit places. Nothing was placed";
    else if (sillMm < 0)
        sillRefusal = "A sill of " + sillText + " mm is below the level, and that is not guessed "
            + "at. Nothing was placed";
    else
        sillAsked = sillMm / 304.8;
}

if (sillRefusal != null)
{
    findings.Add(sillRefusal);
}
else if (points == null || points.Count == 0)
{
    findings.Add("No points were given, so there is nothing to place");
}
else if (toRoomAt == null || toRoomAt.Count != points.Count)
{
    findings.Add(string.Format("Every point needs a second point inside the room it should "
        + "open into - {0} point(s) were given and {1} room point(s). Nothing was placed",
        points.Count, toRoomAt == null ? 0 : toRoomAt.Count));
}
else if (hingeAt == null || hingeAt.Count != points.Count)
{
    findings.Add(string.Format("Every point needs a third point on the side its hinge belongs - "
        + "{0} point(s) were given and {1} hinge point(s). Nothing was placed",
        points.Count, hingeAt == null ? 0 : hingeAt.Count));
}
else if (symbol == null || symbol.Category == null)
{
    findings.Add("No family type was given");
}
else if (level == null)
{
    findings.Add("No level was given");
}
else
{
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    // The bare To Room answers for the LAST phase, so that is the one asked -
    // named in the summary so a refurbishment model is not read as a new one.
    Phase phase = null;
    foreach (Phase candidate in doc.Phases)
    {
        if (candidate != null) phase = candidate;
    }

    // The walls a door could go into: on this level, with a straight location
    // line. Collected once, from the whole model - never through a view, which
    // returns only what its Visibility/Graphics shows.
    var walls = new List<Wall>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        var wall = element as Wall;
        if (wall == null || wall.LevelId != level.Id) continue;
        var along = wall.Location as LocationCurve;
        if (along == null || !(along.Curve is Line)) continue;
        walls.Add(wall);
    }

    var z = level.ProjectElevation;

    // The type's Height, for the head check before placing. Doors and
    // Windows both keep it in FAMILY_HEIGHT_PARAM; a family that keeps its
    // height on the instance has none here, and the check is then reported
    // as not made rather than guessed.
    double? typeHeight = null;
    if (sillAsked.HasValue)
    {
        try
        {
            var heightParameter = symbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM);
            if (heightParameter != null && heightParameter.StorageType == StorageType.Double
                && heightParameter.AsDouble() > 0)
                typeHeight = heightParameter.AsDouble();
        }
        catch { }
    }
    var headUnchecked = 0;

    // The room a short way out along the door's facing - clear of the wall's
    // half-thickness, well short of the far side of a corridor.
    Func<FamilyInstance, Wall, Room> roomFaced = (door, wall) =>
    {
        var at = door.Location as LocationPoint;
        if (at == null || phase == null) return null;
        var reach = wall.Width / 2 + 300.0 / 304.8;
        var facing = door.FacingOrientation;
        try
        {
            return doc.GetRoomAtPoint(new XYZ(at.Point.X + facing.X * reach,
                                              at.Point.Y + facing.Y * reach, z + 1.0), phase);
        }
        catch { return null; }
    };

    // The floor plans of this level, where a door draws its swing.
    var plans = new List<ViewPlan>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
    {
        var plan = element as ViewPlan;
        if (plan == null || plan.IsTemplate || plan.ViewType != ViewType.FloorPlan) continue;
        if (plan.GenLevel == null || plan.GenLevel.Id != level.Id) continue;
        plans.Add(plan);
    }

    // The hinge: the centre of the door's plan swing arc. Why none could be
    // read is left in `hingeProblem` for the caller of this to name.
    string hingeProblem = null;
    Func<FamilyInstance, XYZ> hingeOf = door =>
    {
        hingeProblem = "no swing arc was found for it in any floor plan of '" + level.Name
            + "', so its hinge side could not be read";
        foreach (var plan in plans)
        {
            GeometryElement drawn = null;
            try
            {
                var options = new Options();
                options.View = plan;
                drawn = door.get_Geometry(options);
            }
            catch { }
            if (drawn == null) continue;

            XYZ first = null;
            foreach (var item in drawn)
            {
                var instance = item as GeometryInstance;
                if (instance == null) continue;
                foreach (var piece in instance.GetInstanceGeometry())
                {
                    var arc = piece as Arc;
                    if (arc == null) continue;
                    if (first == null) { first = arc.Center; continue; }
                    if (first.DistanceTo(arc.Center) > SamePlace)
                    {
                        hingeProblem = "it draws two swing arcs a jamb apart - a pair of leaves "
                            + "has no one hinge side";
                        return null;
                    }
                }
            }
            if (first != null) { hingeProblem = null; return first; }
        }
        return null;
    };

    for (var i = 0; i < points.Count; i++)
    {
        var point = points[i];
        var into = toRoomAt[i];
        if (point == null || into == null) { failed++; continue; }

        var label = string.Format("point {0} ({1:0}, {2:0})", i + 1,
            point.X * 304.8, point.Y * 304.8);

        // WHICH WALL. Plan distance to each location line, only where the
        // point falls between the line's ends.
        Wall host = null;
        XYZ onLine = null;
        var nearest = double.MaxValue;
        var tie = false;

        foreach (var wall in walls)
        {
            var line = (Line)((LocationCurve)wall.Location).Curve;
            var a = line.GetEndPoint(0);
            var b = line.GetEndPoint(1);
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 0) continue;

            var t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared;
            if (t <= 0 || t >= 1) continue;

            var footX = a.X + t * dx;
            var footY = a.Y + t * dy;
            var distance = Math.Sqrt((point.X - footX) * (point.X - footX)
                                     + (point.Y - footY) * (point.Y - footY));
            if (distance > wall.Width / 2 + Slack) continue;

            if (distance < nearest - Slack)
            {
                host = wall;
                onLine = new XYZ(footX, footY, z);
                nearest = distance;
                tie = false;
            }
            else if (Math.Abs(distance - nearest) <= Slack)
            {
                tie = true;
            }
        }

        if (host == null)
        {
            noWall.Add(label + " - no wall on this level passes through it");
            continue;
        }
        if (tie)
        {
            twoWalls.Add(label + " - two walls are equally near, so which one is not guessed");
            continue;
        }

        // THE HEAD, BEFORE ANYTHING IS PLACED OR TOUCHED. Level + sill + the
        // type's Height against the top of the wall's own bounding box.
        if (sillAsked.HasValue)
        {
            BoundingBoxXYZ wallBox = null;
            try { wallBox = host.get_BoundingBox(null); } catch { }
            if (typeHeight.HasValue && wallBox != null)
            {
                var head = z + sillAsked.Value + typeHeight.Value;
                if (head > wallBox.Max.Z + Slack)
                {
                    sillTooHigh.Add(string.Format("{0} - a {1:0} mm sill under a {2:0} mm high "
                        + "type puts its head {3:0} mm above the level, over the wall's top at "
                        + "{4:0} mm; nothing placed or changed there", label,
                        sillAsked.Value * 304.8, typeHeight.Value * 304.8,
                        (head - z) * 304.8, (wallBox.Max.Z - z) * 304.8));
                    continue;
                }
            }
            else
            {
                headUnchecked++;
            }
        }

        // ALREADY THERE? Same category, same wall, within 50 mm of the point.
        FamilyInstance made = null;
        foreach (var element in new FilteredElementCollector(doc)
                                    .OfClass(typeof(FamilyInstance))
                                    .OfCategoryId(symbol.Category.Id))
        {
            var existing = element as FamilyInstance;
            if (existing == null || existing.Host == null || existing.Host.Id != host.Id) continue;
            var at = existing.Location as LocationPoint;
            if (at == null) continue;
            var ex = at.Point.X - onLine.X;
            var ey = at.Point.Y - onLine.Y;
            if (Math.Sqrt(ex * ex + ey * ey) <= SamePlace) { made = existing; break; }
        }

        if (made != null)
        {
            alreadyThere.Add(label);
        }
        else
        {
            try
            {
                made = doc.Create.NewFamilyInstance(onLine, symbol, host, level,
                                                    StructuralType.NonStructural);
            }
            catch (Exception failure)
            {
                if (firstRefusal == null) firstRefusal = failure.Message;
            }

            if (made == null) { failed++; continue; }

            doc.Regenerate();

            // HOSTED IN THE WALL THAT WAS FOUND - read back, not assumed.
            Element hostNow = null;
            try { hostNow = made.Host; } catch { }
            if (hostNow == null || hostNow.Id != host.Id)
            {
                try { doc.Delete(made.Id); } catch { }
                failed++;
                if (firstRefusal == null)
                    firstRefusal = label + ": Revit did not host it in the wall found there, so "
                        + "it was removed again";
                continue;
            }

        }

        // THE SILL. Asked for: set on the built-in, regenerated and read back.
        // Not asked for: read, so the reply says where Revit put it.
        var isNew = !alreadyThere.Contains(label);
        Parameter sill = null;
        try { sill = made.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM); } catch { }
        var sillUsable = sill != null && sill.StorageType == StorageType.Double;

        if (sillAsked.HasValue)
        {
            if (!sillUsable || sill.IsReadOnly)
            {
                var why = !sillUsable ? "its family has no Sill Height parameter"
                                      : "its Sill Height is read-only";
                if (isNew)
                {
                    try { doc.Delete(made.Id); } catch { }
                    noSillParameter.Add(label + " - " + why + ", so it was removed again rather "
                        + "than left at a sill nobody asked for. No other parameter is tried");
                    continue;
                }
                noSillParameter.Add(label + " - the one already there: " + why
                    + ", so its sill was left as it is. No other parameter is tried");
            }
            else if (!isNew)
            {
                // ONE ALREADY THERE IS NOT CHANGED. This call did not place it, and
                // the card promises it is left as it was (review of PR #410: the
                // sill used to be written on it too). Its sill is read and said.
                sills.Add(label + " - already there, its sill was left as it is: reads "
                    + (sill.AsDouble() * 304.8).ToString("0") + " mm");
            }
            else
            {
                try
                {
                    sill.Set(sillAsked.Value);
                    doc.Regenerate();
                }
                catch (Exception failure)
                {
                    if (firstRefusal == null) firstRefusal = label + ": " + failure.Message;
                }
                var readMm = double.NaN;
                try
                {
                    sill = made.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                    if (sill != null) readMm = sill.AsDouble() * 304.8;
                }
                catch { }
                var askedMm = sillAsked.Value * 304.8;
                var reads = double.IsNaN(readMm) ? "nothing" : readMm.ToString("0") + " mm";
                sills.Add(string.Format("{0} - sill asked {1:0} mm, reads {2}", label, askedMm, reads));
                if (double.IsNaN(readMm) || Math.Abs(readMm - askedMm) > 1.0)
                    sillWrong.Add(string.Format("{0} - sill asked {1:0} mm, reads {2}",
                        label, askedMm, reads));
            }
        }
        else
        {
            sills.Add(label + " - sill not asked for, left as Revit placed it: reads "
                + (sillUsable ? (sill.AsDouble() * 304.8).ToString("0") + " mm"
                              : "nothing - its family has no Sill Height parameter"));
        }

        if (isNew) placed.Add(made.Id);

        // THE ROOM IT OPENS INTO. A point a foot above the floor, so it is
        // inside the room's height whatever the room's limits are.
        Room wanted = null;
        try
        {
            if (phase != null) wanted = doc.GetRoomAtPoint(new XYZ(into.X, into.Y, z + 1.0), phase);
        }
        catch { }

        if (wanted == null)
        {
            wrongRoom.Add(label + " - no room at the point it should open into, so it was left as "
                + "Revit placed it");
            continue;
        }

        // 1. THE SWING - the facing side.
        var faced = roomFaced(made, host);
        if ((faced == null || faced.Id != wanted.Id) && made.CanFlipFacing)
        {
            made.flipFacing();
            doc.Regenerate();
            flipped++;
            faced = roomFaced(made, host);
        }
        if (faced == null || faced.Id != wanted.Id)
            wrongRoom.Add(label + " - swings into "
                + (faced == null ? "no room" : "'" + faced.Name + "'")
                + ", not '" + wanted.Name + "'");

        // 2. THE HINGE - the end of the opening named for it. Read off the plan
        // swing arc, whose centre IS the hinge; a flip of the hand moves it to
        // the other jamb and leaves the swing side alone.
        var alongWall = ((Line)((LocationCurve)host.Location).Curve).Direction;
        var doorAt = ((LocationPoint)made.Location).Point;
        var wantSide = (hingeAt[i].X - doorAt.X) * alongWall.X + (hingeAt[i].Y - doorAt.Y) * alongWall.Y;
        var hinge = hingeOf(made);
        if (hinge == null)
        {
            wrongHinge.Add(label + " - " + hingeProblem);
        }
        else if (Math.Abs(wantSide) <= Slack)
        {
            wrongHinge.Add(label + " - the hinge point is level with the door's centre, so which "
                + "jamb is meant is not guessed");
        }
        else
        {
            var side = (hinge.X - doorAt.X) * alongWall.X + (hinge.Y - doorAt.Y) * alongWall.Y;
            if (side * wantSide < 0 && made.CanFlipHand)
            {
                made.flipHand();
                doc.Regenerate();
                handFlipped++;
                hinge = hingeOf(made);
                doorAt = ((LocationPoint)made.Location).Point;
                side = hinge == null ? 0
                    : (hinge.X - doorAt.X) * alongWall.X + (hinge.Y - doorAt.Y) * alongWall.Y;
            }
            if (side * wantSide <= 0)
                wrongHinge.Add(label + " - hinged at the other jamb from the one named");
        }

        // 3. THE TO ROOM - a separate setting the swing does not move.
        Room to = null;
        try { to = made.get_ToRoom(phase); } catch { }
        if (to == null || to.Id != wanted.Id)
        {
            try
            {
                made.FlipFromToRoom();
                doc.Regenerate();
                toRoomSwapped++;
            }
            catch (Exception failure)
            {
                if (firstRefusal == null) firstRefusal = failure.Message;
            }
            try { to = made.get_ToRoom(phase); } catch { to = null; }
        }
        if (to == null || to.Id != wanted.Id)
            toRoomWrong.Add(label + " - To Room is "
                + (to == null ? "no room" : "'" + to.Name + "'")
                + ", not '" + wanted.Name + "'");
    }

    findings.Add(string.Format(
        "{0} placed INTO their walls and {1} already there, as '{2}' on '{3}'. Swing turned on {4}, "
        + "hinge moved on {5}, To Room swapped on {6}. Not placed: {7} with no wall there, {8} "
        + "between two walls, {9} refused by Revit. Still wrong: {10} swinging into the wrong room, "
        + "{11} hinged at the wrong jamb, {12} with the wrong To Room. Rooms read for phase '{13}'",
        placed.Count, alreadyThere.Count, symbol.Name, level.Name, flipped, handFlipped,
        toRoomSwapped, noWall.Count, twoWalls.Count, failed, wrongRoom.Count, wrongHinge.Count,
        toRoomWrong.Count, phase == null ? "(none)" : phase.Name));

    if (sillAsked.HasValue)
        findings.Add(string.Format(
            "Sill asked for: {0:0} mm. Read back as asked on {1}, reading otherwise on {2}, "
            + "no usable Sill Height on {3}, {4} point(s) skipped because the head would stand "
            + "above the wall. Head not checked at {5} point(s) - the type's Height or the "
            + "wall's top could not be read",
            sillAsked.Value * 304.8, sills.Count - sillWrong.Count, sillWrong.Count,
            noSillParameter.Count, sillTooHigh.Count, headUnchecked));
    else
        findings.Add("No sill height was asked for, so every sill was left as Revit placed it - "
            + "what each one reads is in `sills`");

    if (firstRefusal != null) findings.Add("Revit's first refusal said: " + firstRefusal);
}
