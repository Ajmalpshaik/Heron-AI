// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `toRoomAt`, `hingeAt` and
// `level` are in scope; leaves `placed`, `alreadyThere`, `failed`, `noWall`,
// `twoWalls`, `flipped`, `handFlipped`, `toRoomSwapped`, `wrongRoom`,
// `wrongHinge`, `toRoomWrong` and `findings` behind.
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
var findings = new List<string>();

// One millimetre, and fifty, in the feet everything here is measured in.
const double Slack = 1.0 / 304.8;
const double SamePlace = 50.0 / 304.8;

string firstRefusal = null;

if (points == null || points.Count == 0)
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

            placed.Add(made.Id);
        }

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

    if (firstRefusal != null) findings.Add("Revit's first refusal said: " + firstRefusal);
}
