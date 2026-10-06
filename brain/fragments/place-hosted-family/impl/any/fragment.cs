// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `toRoomAt`, `hingeAt`,
// `level`, `sillHeight` and `rotation` are in scope; leaves `placed`,
// `alreadyThere`, `failed`, `noWall`, `twoWalls`, `noHost`, `twoHosts`,
// `hostedIn`, `offPoint`, `flipped`, `handFlipped`, `toRoomSwapped`,
// `turnedOut`, `wrongRoom`, `wrongHinge`, `toRoomWrong`, `facingWrong`,
// `rotationWrong`, `sills`, `sillWrong`, `noSillParameter`, `sillTooHigh`,
// `readBack` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// VERSION 4: THE HOST IS WHATEVER THE FAMILY SAYS IT IS - a wall, a floor, a
// ceiling or a roof - and it is found at each point. Read off the family's own
// Host setting (Family Category and Parameters), never off its category or the
// caller's words: a skylight is a WINDOWS family that goes in a ROOF, so the
// category cannot tell, and Revit stores the answer on the family. A family
// that is face-based goes ON a face, not into a host, and is refused here with
// PLACE_FAMILY_ON_FACE named; one that is not hosted at all is refused with
// PLACE_FAMILY_INSTANCES named. Nothing is placed either way.
//
// IN A WALL - doors, windows and every other wall-hosted family, as version 3.
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
// re-running is also how a door placed earlier is corrected. The same rule
// holds in a roof, a floor and a ceiling: the same category, in the same host,
// within 50 mm.
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
// ONLY A DOOR SWINGS INTO A ROOM. A WINDOW FACES OUT. Version 3 gave a window
// the door's swing rule, and on 2026-10-06 in "Heron loads test" (Revit 2024,
// session 51820) it turned all eight M_Fixed 0915 x 1220mm windows to face
// their rooms - FacingFlipped True, every one opposite its wall's exterior
// (row 5b-325). So a window keeps its FacingOrientation along its wall's
// exterior, Wall.Orientation: flipped once if it faces in, read back, and
// named in `facingWrong` if it still does. Its To Room is still set toward the
// room named for it, as a door's is, and no hinge is read - a window draws no
// swing arc, and version 3 named every one in `wrongHinge` (row 5b-293). Any
// other wall-hosted family - a wall-based sign or socket - is turned to face
// the room named, which is the side of the wall it sits on, and has no hinge
// and no To Room to set.
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
// IN A FLOOR, A CEILING OR A ROOF - version 4. The host is found the way a
// modeller would look for it: straight down through the point in plan. Every
// floor, ceiling or roof whose TOP face (a ceiling's UNDERSIDE, which is where
// a fitting hangs) a vertical line through the point crosses is a candidate -
// so a point in a shaft or an opening has none, and a point over two slabs,
// two ceilings or a canopy under the main roof has two.
//   - FLOORS AND CEILINGS ON `level` ONLY. Every storey has its own, so any
//     level would always find several.
//   - ROOFS ON EVERY LEVEL. A roof is drawn on a level of its own - "Roof",
//     the top storey - that is seldom the one a modeller names, so it is
//     found by what is over the point. Where two roofs are, the one on `level`
//     is taken when exactly one of them is on it; otherwise neither. The reply
//     names each roof's own level.
// One host, and the family goes into it at the point where that line meets the
// face. None, or two, and the point is named and skipped - never guessed.
// A point's own height is not read: the host's face decides it.
//   - ONE ALREADY THERE IS LOOKED FOR FIRST, before any face: it has cut its
//     own opening in the host, so a vertical line through its point meets no
//     face at all, and a re-run would otherwise be told there is no host.
//   - IT IS PLACED THROUGH THE HOST'S OWN LEVEL, so its Level reads the
//     host's - for a roof, its own level, not the storey named.
//   - `rotation` turns it in plan: degrees anticlockwise from the way the
//     family is drawn (its own X axis along the project's east), one for every
//     point or one per point. BLANK leaves it as Revit places it - what it
//     reads is reported. Given, the instance's own X axis is read back after
//     placing and, if it points anywhere else, it is turned about its own
//     vertical line to the angle asked and read again; more than half a
//     degree out is named in `rotationWrong`. One already there is turned too
//     when a rotation is given - how it is turned is what a re-run brings into
//     line - and left alone when none is.
//   - EVERY ONE IS READ BACK: its host must be the one found, or it is removed
//     again inside the same undo, and `hostedIn` names the host's id,
//     category, type and level, where the instance stands and how it is
//     turned. One more than 1 mm from its point in plan is named in
//     `offPoint`.
//   - NO OFFSET, AND NO SILL. A family hosted by a floor, a ceiling or a roof
//     sits on that host's face - the host decides its height, which is the
//     point of hosting it there. One that must stand off a face is face-based,
//     and PLACE_FAMILY_ON_FACE places those. A sill height given for one is
//     refused before anything is placed.
//   - `toRoomAt` and `hingeAt` are a door's and a window's, and are not read
//     here. The add-in refuses an empty point list, so a caller sends the
//     points again.
//
// NonStructural is passed deliberately - a door is not a structural member, and
// PLACE_STRUCTURAL_FAMILY is where that decision belongs.

var placed = new List<ElementId>();
var alreadyThere = new List<string>();
var failed = 0;
var noWall = new List<string>();
var twoWalls = new List<string>();
var noHost = new List<string>();
var twoHosts = new List<string>();
var hostedIn = new List<string>();
var offPoint = new List<string>();
var flipped = 0;
var handFlipped = 0;
var toRoomSwapped = 0;
var turnedOut = 0;
var wrongRoom = new List<string>();
var wrongHinge = new List<string>();
var toRoomWrong = new List<string>();
var facingWrong = new List<string>();
var rotationWrong = new List<string>();
var sills = new List<string>();
var sillWrong = new List<string>();
var noSillParameter = new List<string>();
var sillTooHigh = new List<string>();
var findings = new List<string>();

// One millimetre, and fifty, in the feet everything here is measured in.
const double Slack = 1.0 / 304.8;
const double SamePlace = 50.0 / 304.8;

string firstRefusal = null;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

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

// THE ROTATION ASKED FOR, in degrees, or null for "as Revit places it". One
// number for every point, or one per point separated by semicolons - read
// before anything is placed, like the sill.
var rotationText = (rotation ?? "").Trim();
List<double> turns = null;
string rotationRefusal = null;
if (rotationText.Length > 0)
{
    turns = new List<double>();
    foreach (var piece in rotationText.Split(';'))
    {
        var word = piece.Trim();
        if (word.Length == 0) continue;
        double degrees;
        if (!double.TryParse(word, System.Globalization.NumberStyles.Float, invariant, out degrees)
            || double.IsNaN(degrees) || double.IsInfinity(degrees))
        {
            rotationRefusal = "'" + word + "' is not a rotation. Give degrees, turned anticlockwise "
                + "in plan - 90 - one for every point or one per point with semicolons between, or "
                + "leave it blank. Nothing was placed";
            break;
        }
        turns.Add(degrees);
    }
    if (rotationRefusal == null && turns.Count == 0)
        turns = null;
    else if (rotationRefusal == null && points != null && turns.Count != 1
             && turns.Count != points.Count)
        rotationRefusal = string.Format("{0} rotation(s) were given for {1} point(s) - give one for "
            + "every point, or one per point. Nothing was placed", turns.Count, points.Count);
}
Func<int, double?> turnAsked = index =>
    turns == null ? (double?)null : turns.Count == 1 ? turns[0] : turns[index];

// THE HOST, FROM THE FAMILY. Its placement says whether it is hosted at all;
// its Host setting says by what. Neither is guessed from the category, the
// type's name or the points.
var hostKind = FamilyHostingBehavior.None;
string hostRefusal = null;
var typeName = symbol == null ? "" : "'" + symbol.FamilyName + ": " + symbol.Name + "'";
if (symbol != null && symbol.Category != null)
{
    Family family = null;
    try { family = symbol.Family; } catch { }
    var placement = FamilyPlacementType.Invalid;
    try { if (family != null) placement = family.FamilyPlacementType; } catch { }
    int? setting = null;
    try
    {
        var hostParameter = family == null ? null
            : family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR);
        if (hostParameter != null && hostParameter.StorageType == StorageType.Integer)
            setting = hostParameter.AsInteger();
    }
    catch { }

    if (placement == FamilyPlacementType.WorkPlaneBased || setting == (int)FamilyHostingBehavior.Face)
        hostRefusal = typeName + " is face-based - it goes ON a face, not INTO a wall, a floor, a "
            + "ceiling or a roof - so PLACE_FAMILY_ON_FACE is what places it. Nothing was placed";
    else if (placement != FamilyPlacementType.OneLevelBasedHosted)
        hostRefusal = typeName + " is not a hosted family - its placement reads " + placement
            + " - so it goes into no wall, floor, ceiling or roof; PLACE_FAMILY_INSTANCES places a "
            + "family at points on a level. Nothing was placed";
    else if (setting == (int)FamilyHostingBehavior.Wall || setting == (int)FamilyHostingBehavior.Floor
             || setting == (int)FamilyHostingBehavior.Ceiling || setting == (int)FamilyHostingBehavior.Roof)
        hostKind = (FamilyHostingBehavior)setting.Value;
    else
        hostRefusal = typeName + " is hosted, but its family's Host setting "
            + (setting.HasValue ? "reads " + setting.Value : "could not be read")
            + ", so whether it goes into a wall, a floor, a ceiling or a roof is not guessed. "
            + "Nothing was placed";
}
var inWall = hostKind == FamilyHostingBehavior.Wall;

// Where an instance stands, in millimetres, for the read-back lines.
Func<XYZ, string> mm = at => at == null ? "nowhere"
    : string.Format(invariant, "({0:0}, {1:0}, {2:0}) mm", at.X * 304.8, at.Y * 304.8, at.Z * 304.8);
Func<ElementId, string> levelName = id =>
{
    var named = id == null ? null : doc.GetElement(id) as Level;
    return named == null ? "(no level)" : "'" + named.Name + "'";
};
Func<Element, string> typeOf = element =>
{
    var type = element == null ? null : doc.GetElement(element.GetTypeId());
    return type == null ? "" : " '" + type.Name + "'";
};

if (sillRefusal != null)
{
    findings.Add(sillRefusal);
}
else if (rotationRefusal != null)
{
    findings.Add(rotationRefusal);
}
else if (points == null || points.Count == 0)
{
    findings.Add("No points were given, so there is nothing to place");
}
else if (symbol == null || symbol.Category == null)
{
    findings.Add("No family type was given");
}
else if (level == null)
{
    findings.Add("No level was given");
}
else if (hostRefusal != null)
{
    findings.Add(hostRefusal);
}
else if (inWall && (toRoomAt == null || toRoomAt.Count != points.Count))
{
    findings.Add(string.Format("Every point needs a second point inside the room it should "
        + "open into - {0} point(s) were given and {1} room point(s). Nothing was placed",
        points.Count, toRoomAt == null ? 0 : toRoomAt.Count));
}
else if (inWall && (hingeAt == null || hingeAt.Count != points.Count))
{
    findings.Add(string.Format("Every point needs a third point on the side its hinge belongs - "
        + "{0} point(s) were given and {1} hinge point(s). Nothing was placed",
        points.Count, hingeAt == null ? 0 : hingeAt.Count));
}
else if (inWall && turns != null && turns.Any(turn => Math.Abs(Math.IEEERemainder(turn, 360.0)) > 1e-9))
{
    findings.Add(typeName + " goes into a wall, and a door, a window or anything else in a wall "
        + "turns with its wall - leave rotation blank for it. Nothing was placed");
}
else if (!inWall && sillAsked.HasValue)
{
    findings.Add("A sill height is a door's or a window's in a wall, and " + typeName + " goes into "
        + "a " + hostKind.ToString().ToLowerInvariant() + ", whose face decides its height - leave "
        + "sillHeight blank for it. Nothing was placed");
}
else if (inWall)
{
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    var isDoor = symbol.Category.Id == new ElementId(BuiltInCategory.OST_Doors);
    var isWindow = symbol.Category.Id == new ElementId(BuiltInCategory.OST_Windows);

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

        // A WINDOW FACES OUT, whatever room it is given and whether or not
        // there is one: along its wall's exterior, Wall.Orientation. Flipped
        // once if it faces in, then read again (row 5b-325).
        var facingRead = "";
        if (isWindow)
        {
            var outward = host.Orientation;
            Func<double> outwardness = () =>
            {
                var facing = made.FacingOrientation;
                return facing.X * outward.X + facing.Y * outward.Y;
            };
            var outness = outwardness();
            if (outness < 0 && made.CanFlipFacing)
            {
                made.flipFacing();
                doc.Regenerate();
                turnedOut++;
                outness = outwardness();
            }
            facingRead = string.Format(invariant, ", faces {0}: FacingOrientation ({1:0.###}, {2:0.###}), "
                + "its wall's exterior ({3:0.###}, {4:0.###}), FacingFlipped {5}",
                outness > 0 ? "out" : "IN", made.FacingOrientation.X, made.FacingOrientation.Y,
                outward.X, outward.Y, made.FacingFlipped);
            if (outness <= 0)
                facingWrong.Add(label + " - faces into the building, against its wall's exterior "
                    + "(FacingFlipped " + made.FacingFlipped + ")");
        }

        // WHERE IT IS, AND IN WHAT - read back, for every one kept.
        XYZ standsAt = null;
        try { standsAt = ((LocationPoint)made.Location).Point; } catch { }
        hostedIn.Add(label + " - " + (isNew ? "placed" : "already there") + " in wall " + host.Id
            + typeOf(host) + ", at " + mm(standsAt) + facingRead);
        if (isNew && (standsAt == null
                      || Math.Sqrt((standsAt.X - onLine.X) * (standsAt.X - onLine.X)
                                   + (standsAt.Y - onLine.Y) * (standsAt.Y - onLine.Y)) > Slack))
            offPoint.Add(label + " - reads " + mm(standsAt) + ", not on the wall's line at "
                + mm(onLine));

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
            wrongRoom.Add(label + (isWindow
                ? " - no room at the point given for it, so its To Room was left as Revit set it"
                : " - no room at the point it should open into, so it was left as Revit placed it"));
            continue;
        }

        // 1. THE SWING - the facing side. A door's, and the side of the wall
        // any other wall-hosted family sits on; never a window's.
        if (!isWindow)
        {
            var faced = roomFaced(made, host);
            if ((faced == null || faced.Id != wanted.Id) && made.CanFlipFacing)
            {
                made.flipFacing();
                doc.Regenerate();
                flipped++;
                faced = roomFaced(made, host);
            }
            if (faced == null || faced.Id != wanted.Id)
                wrongRoom.Add(label + (isDoor ? " - swings into " : " - faces ")
                    + (faced == null ? "no room" : "'" + faced.Name + "'")
                    + ", not '" + wanted.Name + "'");
        }

        // 2. THE HINGE - the end of the opening named for it. Read off the plan
        // swing arc, whose centre IS the hinge; a flip of the hand moves it to
        // the other jamb and leaves the swing side alone. A door's alone.
        if (isDoor)
        {
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
        }

        // 3. THE TO ROOM - a separate setting the swing does not move. A
        // door's and a window's; no other family has one.
        if (isDoor || isWindow)
        {
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
    }

    findings.Add(string.Format(
        "{0} placed INTO their walls and {1} already there, as '{2}' on '{3}'. Swing turned on {4}, "
        + "hinge moved on {5}, To Room swapped on {6}. Not placed: {7} with no wall there, {8} "
        + "between two walls, {9} refused by Revit. Still wrong: {10} swinging into the wrong room, "
        + "{11} hinged at the wrong jamb, {12} with the wrong To Room. Rooms read for phase '{13}'",
        placed.Count, alreadyThere.Count, symbol.Name, level.Name, flipped, handFlipped,
        toRoomSwapped, noWall.Count, twoWalls.Count, failed, wrongRoom.Count, wrongHinge.Count,
        toRoomWrong.Count, phase == null ? "(none)" : phase.Name));

    if (isWindow)
        findings.Add(string.Format(
            "Windows face OUT, along their wall's exterior - only a door swings into a room: {0} "
            + "turned to face out, {1} still facing in. No hinge is read for a window - it draws "
            + "no swing arc", turnedOut, facingWrong.Count));

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
}
else
{
    // ---- IN A FLOOR, A CEILING OR A ROOF ---------------------------------
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    var kindWord = hostKind == FamilyHostingBehavior.Roof ? "roof"
        : hostKind == FamilyHostingBehavior.Floor ? "floor" : "ceiling";
    var ceiling = hostKind == FamilyHostingBehavior.Ceiling;
    var hostCategory = hostKind == FamilyHostingBehavior.Roof ? BuiltInCategory.OST_Roofs
        : hostKind == FamilyHostingBehavior.Floor ? BuiltInCategory.OST_Floors
        : BuiltInCategory.OST_Ceilings;

    // Every one of that kind in the model, from the whole model - never
    // through a view. An in-place roof or floor is a family, not a host
    // object, and nothing can be hosted in one, so it is not collected.
    var everyHost = new List<HostObject>();
    foreach (var element in new FilteredElementCollector(doc).OfCategory(hostCategory)
                                                             .WhereElementIsNotElementType())
    {
        var candidate = element as HostObject;
        if (candidate != null) everyHost.Add(candidate);
    }

    // Where a vertical line through (x, y) meets this host's top face - a
    // ceiling's underside - or null where it does not: outside the outline,
    // or in an opening through it. The highest top face, the lowest underside.
    Func<HostObject, double, double, double?> faceAt = (candidate, x, y) =>
    {
        BoundingBoxXYZ box = null;
        try { box = candidate.get_BoundingBox(null); } catch { }
        if (box == null || x < box.Min.X - Slack || x > box.Max.X + Slack
            || y < box.Min.Y - Slack || y > box.Max.Y + Slack) return null;

        IList<Reference> faces = null;
        try
        {
            faces = ceiling ? HostObjectUtils.GetBottomFaces(candidate)
                            : HostObjectUtils.GetTopFaces(candidate);
        }
        catch { }
        if (faces == null) return null;

        var down = Line.CreateBound(new XYZ(x, y, box.Max.Z + 1.0), new XYZ(x, y, box.Min.Z - 1.0));
        double? met = null;
        foreach (var reference in faces)
        {
            Face face = null;
            try { face = candidate.GetGeometryObjectFromReference(reference) as Face; } catch { }
            if (face == null) continue;
            IntersectionResultArray crossings = null;
            var crossing = SetComparisonResult.Disjoint;
            try { crossing = face.Intersect(down, out crossings); } catch { continue; }
            if (crossing != SetComparisonResult.Overlap || crossings == null) continue;
            for (var k = 0; k < crossings.Size; k++)
            {
                var height = crossings.get_Item(k).XYZPoint.Z;
                if (met == null || (ceiling ? height < met.Value : height > met.Value)) met = height;
            }
        }
        return met;
    };

    // How an instance is turned: its own X axis, in plan, in degrees from
    // east - 0 to 360 - or null when that axis stands upright.
    Func<FamilyInstance, double?> turnOf = instance =>
    {
        try
        {
            var x = instance.GetTransform().BasisX;
            if (Math.Abs(x.X) < 1e-9 && Math.Abs(x.Y) < 1e-9) return null;
            var degrees = Math.Atan2(x.Y, x.X) * 180.0 / Math.PI;
            return degrees < 0 ? degrees + 360.0 : degrees;
        }
        catch { return null; }
    };
    // The shorter way round from one angle to another, -180 to 180.
    Func<double, double> shortest = degrees => ((degrees % 360.0) + 540.0) % 360.0 - 180.0;

    var roofsOffLevel = 0;
    var throughDirection = 0;

    for (var i = 0; i < points.Count; i++)
    {
        var point = points[i];
        if (point == null) { failed++; continue; }

        var label = string.Format("point {0} ({1:0}, {2:0})", i + 1,
            point.X * 304.8, point.Y * 304.8);

        // ALREADY THERE? Looked for FIRST, and not through the host's face: one
        // already in a floor, a ceiling or a roof has cut its own opening, so a
        // vertical line through its point meets no face there at all. Measured
        // 2026-10-06 in Project2 (Revit 2024): re-running a kept skylight's own
        // call answered "no roof on any level is over it". Same category, in a
        // host of this kind - on the level given, for a floor or a ceiling -
        // within 50 mm in plan; the nearest, when two sit in the same host.
        FamilyInstance made = null;
        HostObject host = null;
        XYZ spot = null;
        var nearby = new List<Tuple<FamilyInstance, double>>();
        foreach (var element in new FilteredElementCollector(doc)
                                    .OfClass(typeof(FamilyInstance))
                                    .OfCategoryId(symbol.Category.Id))
        {
            var existing = element as FamilyInstance;
            HostObject existingHost = null;
            try { existingHost = existing == null ? null : existing.Host as HostObject; } catch { }
            if (existingHost == null || existingHost.Category == null
                || existingHost.Category.Id != new ElementId(hostCategory)) continue;
            if (hostKind != FamilyHostingBehavior.Roof && existingHost.LevelId != level.Id) continue;
            var at = existing.Location as LocationPoint;
            if (at == null) continue;
            var ex = at.Point.X - point.X;
            var ey = at.Point.Y - point.Y;
            var apart = Math.Sqrt(ex * ex + ey * ey);
            if (apart <= SamePlace) nearby.Add(Tuple.Create(existing, apart));
        }
        var nearbyHosts = nearby.Select(n => n.Item1.Host.Id).Distinct().ToList();
        if (nearbyHosts.Count > 1)
        {
            twoHosts.Add(label + " - one is already there in each of " + nearbyHosts.Count + " "
                + kindWord + "s (" + string.Join(", ", nearbyHosts) + "), so which one is meant "
                + "is not guessed");
            continue;
        }
        if (nearby.Count > 0)
        {
            made = nearby.OrderBy(n => n.Item2).First().Item1;
            host = (HostObject)made.Host;
            spot = ((LocationPoint)made.Location).Point;
        }
        else
        {
            // WHICH HOST: those of the kind the vertical line through the point
            // meets. Floors and ceilings on the level given; roofs on any level.
            var covering = new List<Tuple<HostObject, double>>();
            var elsewhere = new List<string>();
            foreach (var candidate in everyHost)
            {
                var met = faceAt(candidate, point.X, point.Y);
                if (!met.HasValue) continue;
                if (hostKind != FamilyHostingBehavior.Roof && candidate.LevelId != level.Id)
                {
                    var other = levelName(candidate.LevelId);
                    if (!elsewhere.Contains(other)) elsewhere.Add(other);
                    continue;
                }
                covering.Add(Tuple.Create(candidate, met.Value));
            }
            if (covering.Count > 1 && hostKind == FamilyHostingBehavior.Roof)
            {
                var onLevel = covering.Where(c => c.Item1.LevelId == level.Id).ToList();
                if (onLevel.Count == 1) covering = onLevel;
            }

            if (covering.Count == 0)
            {
                noHost.Add(label + " - no " + kindWord + (hostKind == FamilyHostingBehavior.Roof
                        ? " on any level is over it"
                        : (ceiling ? " on '" + level.Name + "' is over it" : " on '" + level.Name + "' is under it")
                          + (elsewhere.Count > 0 ? " (one on " + string.Join(", ", elsewhere) + " is)" : ""))
                    + ", so nothing was placed there");
                continue;
            }
            if (covering.Count > 1)
            {
                twoHosts.Add(label + " - " + covering.Count + " " + kindWord + "s are there ("
                    + string.Join(", ", covering.Select(c => c.Item1.Id + " on " + levelName(c.Item1.LevelId)))
                    + "), so which one is not guessed");
                continue;
            }

            host = covering[0].Item1;
            spot = new XYZ(point.X, point.Y, covering[0].Item2);
        }
        if (hostKind == FamilyHostingBehavior.Roof && host.LevelId != level.Id) roofsOffLevel++;

        var isNew = made == null;
        var asked = turnAsked(i);
        if (!isNew)
        {
            alreadyThere.Add(label);
        }
        else
        {
            // Placed through the HOST'S OWN LEVEL, the overload walls use - not the
            // level named, which for a roof is usually a storey below it. Measured
            // 2026-10-06 in Project2 (Revit 2024), M_Skylight-Flat into a flat
            // roof on Level 2: this way the skylight's Level reads 'Level 2'; the
            // overload that takes a direction instead left it reading no level at
            // all, in the same place. So the direction is set by turning it after,
            // below. Should Revit refuse this one, the direction overload is tried
            // once, and the reply says how many went that way.
            var hostLevel = doc.GetElement(host.LevelId) as Level;
            string refusedHere = null;
            try
            {
                made = doc.Create.NewFamilyInstance(spot, symbol, host, hostLevel ?? level,
                                                    StructuralType.NonStructural);
            }
            catch (Exception failure)
            {
                refusedHere = failure.Message;
            }
            if (made == null && refusedHere != null)
            {
                var turn = (asked ?? 0.0) * Math.PI / 180.0;
                try
                {
                    made = doc.Create.NewFamilyInstance(spot, symbol,
                        new XYZ(Math.Cos(turn), Math.Sin(turn), 0), host, StructuralType.NonStructural);
                    if (made != null) throughDirection++;
                }
                catch (Exception second)
                {
                    refusedHere += " - and with a direction instead: " + second.Message;
                }
            }
            if (refusedHere != null && firstRefusal == null) firstRefusal = label + ": " + refusedHere;

            if (made == null) { failed++; continue; }

            doc.Regenerate();

            // HOSTED IN THE ONE FOUND - read back, not assumed.
            Element hostNow = null;
            try { hostNow = made.Host; } catch { }
            if (hostNow == null || hostNow.Id != host.Id)
            {
                try { doc.Delete(made.Id); } catch { }
                failed++;
                if (firstRefusal == null)
                    firstRefusal = label + ": Revit did not host it in the " + kindWord
                        + " found there, so it was removed again";
                continue;
            }
            placed.Add(made.Id);
        }

        // THE ROTATION, when one is asked: read the instance's own X axis, turn
        // it about its own vertical line if Revit set it anywhere else, and
        // read it again.
        var turnNow = turnOf(made);
        if (asked.HasValue)
        {
            if (turnNow.HasValue && Math.Abs(shortest(asked.Value - turnNow.Value)) > 0.05)
            {
                try
                {
                    var pivot = ((LocationPoint)made.Location).Point;
                    ElementTransformUtils.RotateElement(doc, made.Id,
                        Line.CreateBound(pivot, pivot + XYZ.BasisZ),
                        shortest(asked.Value - turnNow.Value) * Math.PI / 180.0);
                    doc.Regenerate();
                }
                catch (Exception failure)
                {
                    if (firstRefusal == null) firstRefusal = label + ": " + failure.Message;
                }
                turnNow = turnOf(made);
            }
            if (!turnNow.HasValue || Math.Abs(shortest(asked.Value - turnNow.Value)) > 0.5)
                rotationWrong.Add(string.Format(invariant, "{0} - rotation asked {1:0.#} degrees, reads {2}",
                    label, asked.Value,
                    turnNow.HasValue ? turnNow.Value.ToString("0.#", invariant) + " degrees" : "nothing"));
        }

        // WHERE IT IS, AND IN WHAT - read back, for every one kept.
        XYZ standsAt = null;
        try { standsAt = ((LocationPoint)made.Location).Point; } catch { }
        Category hostCategoryNow = null;
        try { hostCategoryNow = made.Host == null ? null : made.Host.Category; } catch { }
        hostedIn.Add(string.Format(invariant, "{0} - {1} in {2} {3}{4} on {5}, at {6}{7}; turned {8}; "
            + "its Level reads {9}",
            label, isNew ? "placed" : "already there",
            hostCategoryNow == null ? kindWord : hostCategoryNow.Name, host.Id, typeOf(host),
            levelName(host.LevelId), mm(standsAt),
            isNew ? string.Format(invariant, ", its {0} face at {1:0} mm", ceiling ? "under" : "top",
                                  spot.Z * 304.8) : "",
            turnNow.HasValue ? turnNow.Value.ToString("0.#", invariant) + " degrees" : "(no plan direction)",
            levelName(made.LevelId)));
        if (isNew && (standsAt == null
                      || Math.Sqrt((standsAt.X - spot.X) * (standsAt.X - spot.X)
                                   + (standsAt.Y - spot.Y) * (standsAt.Y - spot.Y)) > Slack))
            offPoint.Add(label + " - reads " + mm(standsAt) + ", not at the point asked");
    }

    findings.Add(string.Format(
        "{0} placed INTO their {1}s and {2} already there, as {3}. Not placed: {4} with no {1} "
        + "there, {5} where two {1}s are, {6} refused by Revit. Read back in its {1}: every one in "
        + "`hostedIn`; {7} more than 1 mm from its point. {8}",
        placed.Count, kindWord, alreadyThere.Count, typeName, noHost.Count, twoHosts.Count, failed,
        offPoint.Count,
        turns == null ? "No rotation was asked for, so each one is as Revit placed it - how each is "
                        + "turned is in `hostedIn`"
                      : "Rotation asked for: still wrong on " + rotationWrong.Count));

    if (hostKind == FamilyHostingBehavior.Roof)
        findings.Add(string.Format("Roofs are found by what is over the point, on every level, and "
            + "the one on '{0}' is taken only where two are: {1} of these were in a roof on another "
            + "level", level.Name, roofsOffLevel));
    else
        findings.Add(string.Format("Only {0}s on '{1}' were looked for - every storey has its own",
            kindWord, level.Name));

    if (throughDirection > 0)
        findings.Add(throughDirection + " were placed with a direction instead of through the host's "
            + "level, after Revit refused that - their Level and rotation are read back like every "
            + "other");
}

// THE READ-BACK IN FULL, ON ONE LINE. The reply prints a list's first three
// lines cut at 60 characters, and these lines are the evidence - the same
// reason REPORT_FAMILY_TEMPLATE answers on one line.
var readBack = (hostRefusal == null && hostKind != FamilyHostingBehavior.None
        ? "Host setting read off the family: " + hostKind + ". " : "")
    + string.Join(" | ", hostedIn);

if (firstRefusal != null) findings.Add("Revit's first refusal said: " + firstRefusal);
