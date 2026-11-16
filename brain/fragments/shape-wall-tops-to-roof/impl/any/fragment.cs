// NOT STANDALONE. Assumes `doc`, `editScopeFailures`, `elements` and `redraw`
// are in scope; leaves `shaped`, `shapedIds`, `noRoof`, `leftAsTheyWere`,
// `keptInserts`, `report` and `findings` behind.
//
// EACH WALL'S TOP SHAPED TO THE ROOF ABOVE IT, BY EDITING ITS PROFILE - Edit
// Profile in the ribbon - on Revit 2022 to 2024, where Revit lets no add-in
// attach a wall (Wall.AddAttachment arrived in 2025; ATTACH_WALLS does the
// real attach there). The wall is EDITED, never rebuilt: its doors, windows
// and openings stay in it, and every one is read back afterwards.
//
// THE TOP IS A SKETCH. It does not follow the roof when the roof is moved,
// re-pitched or rebuilt - Revit's Attach Top does, this cannot. Every answer
// that shapes a wall says so, and a wall already shaped is drawn to the roof
// again only when `redraw` is true.
//
// ===========================================================================
// THIS FRAGMENT OPENS ITS OWN TRANSACTIONS, AND ONLY BECAUSE IT HAS TO - D-112.
// ===========================================================================
//
// A wall's profile is edited only inside a SketchEditScope, and Revit's own
// reference says, on SketchEditScope.Start for 2022 to 2024: "SketchEditScope
// can only be started when there is no transaction active". NOT MEASURED in
// Revit - read off RevitAPI.xml. Declaring `editScopeFailures` makes the add-in
// run this inside its ONE named TransactionGroup with no transaction of its
// own open, so the whole job is still one undo entry and Discard still takes
// all of it back. Every transaction here takes `editScopeFailures`, so what
// Revit says reaches the answer as it would from the add-in's own.
//
// PER WALL, IN ORDER:
//   1. "Heron: wall profile" - Wall.CreateProfileSketch (a redraw first takes
//      the old one away with RemoveProfileSketch), committed: Revit's own
//      remark is that the sketch's loop "cannot be obtained until
//      regeneration". Its rectangle gives the wall's base, its own top and
//      the plane the profile is drawn on.
//   2. A SketchEditScope on that sketch, and inside it "Heron: wall top to
//      the roof": the rectangle's lines deleted and the new outline drawn on
//      the sketch's own plane - the bottom and the two ends where they were,
//      the top following the roof.
//
// THE ROOF ABOVE IS FOUND PER WALL, never guessed across the building. Along
// the wall, every 100 mm, a vertical line is cut through every roof whose
// plan extent overlaps the wall - just inside each of the wall's two faces.
// A roof counts there when it reaches above the wall's own top less 300 mm,
// so a lower roof the wall rises past (a lean-to) is not one; its underside
// there is the wall's new top. The top takes the LOWER of the two faces, so
// the wall never pokes through the roof; a roof sloping ACROSS the wall (an
// eave wall) leaves a small wedge on the high face that Revit's Attach would
// fill. Where no roof is over the wall the top stays where it was. Straight
// runs between samples become one line, and a ridge between two runs is put
// where their lines meet, so a planar roof gives an exact gable.
//
// THE WALL'S OWN BASE AND TOP come from its parameters - base level and base
// offset, top level and top offset, or its unconnected height - so a wall
// shaped before is judged by the rectangle Revit gives it back, not by its
// old gable.
//
// LEFT AS IT WAS, AND NAMED - decided before anything is changed:
//   not a wall; in a group; owned by somebody else (worksharing); its top
//   already ATTACHED in Revit (it follows a roof already); Revit says it can
//   have no profile (curved, tapered, a curtain wall or a panel in one); its
//   profile already edited and `redraw` not asked; a profile with openings
//   drawn in it, which a redraw would lose; no roof over it (`noRoof`); a
//   FLOOR between its top and the roof - a wall is shaped only to the roof
//   right above it; a roof coming down to less than 300 mm above its base;
//   its top already meeting the roof.
//
// ALL OR NOTHING ONCE THE FIRST WALL IS CHANGED. A Revit refusal after that -
// a sketch it will not make, an outline it will not keep, a door or window it
// drops - THROWS, naming the wall, in Revit's words. The add-in rolls a
// fragment that throws back whole, group and all; nothing is undone by hand.
//
// MILLIMETRES become feet by / 304.8 (D-20).

const double MmPerFoot = 304.8;
var shaped = 0;
var shapedIds = new List<ElementId>();
var noRoof = new List<ElementId>();
var leftAsTheyWere = new List<string>();
var keptInserts = 0;
var lines = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MmPerFoot).ToString(invariant);
var step = 100.0 / MmPerFoot;          // one sample along the wall every 100 mm
var fitTolerance = 2.0 / MmPerFoot;    // a sample this close to a line is on it
var sameTop = 3.0 / MmPerFoot;         // a top this close to the roof already meets it
var shortest = 5.0 / MmPerFoot;        // no line of the new outline shorter than this
var lowest = 300.0 / MmPerFoot;        // no top lower than this above the wall's base
var keptNothing = " The call failed, and Heron rolls the whole call back - no wall is changed.";
var sketchNote = "The top of a shaped wall is a sketch: it does NOT follow the roof if the roof is changed "
    + "later - ask again after a roof change, with redraw, to draw it to the roof again.";

Action<Transaction> discipline = t =>
{
    var options = t.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(editScopeFailures);
    options.SetClearAfterRollback(true);
    t.SetFailureHandlingOptions(options);
};

// REVIT'S OWN WORDS FOR A REFUSAL - the failure discipline the add-in hands in
// says what Revit posted (row 5b-1240); an older add-in answers with its type
// name, which is not quoted.
Func<string> revitSaid = () =>
{
    var text = editScopeFailures == null ? null : editScopeFailures.ToString();
    return text != null && text.StartsWith("Revit would not let it through", StringComparison.Ordinal)
        ? " - " + text : "";
};
Func<Exception, string> words = error => (error.Message ?? "").TrimEnd('.', ' ');
Func<Element, string> named = e => (e.Category == null ? "element" : e.Category.Name) + " " + e.Id;

// THE WALL'S OWN BASE AND TOP, from its parameters (the header). The box is
// the fallback when a level cannot be read.
Func<Wall, BoundingBoxXYZ, double[]> baseAndTop = (w, b) =>
{
    var level = doc.GetElement(w.LevelId) as Level;
    var baseOffset = w.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
    if (level == null || baseOffset == null) return new[] { b.Min.Z, b.Max.Z };
    var bottom = level.ProjectElevation + baseOffset.AsDouble();
    var topConstraint = w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
    var topId = topConstraint == null ? ElementId.InvalidElementId : topConstraint.AsElementId();
    var upTo = topId == null || topId == ElementId.InvalidElementId ? null : doc.GetElement(topId) as Level;
    if (upTo != null)
    {
        var topOffset = w.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
        return new[] { bottom, upTo.ProjectElevation + (topOffset == null ? 0.0 : topOffset.AsDouble()) };
    }
    var height = w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
    return new[] { bottom, height == null ? b.Max.Z : bottom + height.AsDouble() };
};

// ---------------------------------------------------------------------------
// 0. Every roof and floor once, with its box; solids read when first needed.
// ---------------------------------------------------------------------------
var roofs = new List<Tuple<Element, BoundingBoxXYZ>>();
var floors = new List<Tuple<Element, BoundingBoxXYZ>>();
foreach (var each in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType())
{
    var box = each.get_BoundingBox(null);
    if (box != null) roofs.Add(Tuple.Create(each, box));
}
foreach (var each in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors).WhereElementIsNotElementType())
{
    var box = each.get_BoundingBox(null);
    if (box != null) floors.Add(Tuple.Create(each, box));
}

var solidsCache = new Dictionary<ElementId, List<Solid>>();
Func<Element, List<Solid>> solidsOf = e =>
{
    List<Solid> found;
    if (solidsCache.TryGetValue(e.Id, out found)) return found;
    found = new List<Solid>();
    var options = new Options();
    options.DetailLevel = ViewDetailLevel.Fine;
    var geometry = e.get_Geometry(options);
    if (geometry != null)
        foreach (var item in geometry)
        {
            var solid = item as Solid;
            if (solid != null) { if (solid.Volume > 1e-9) found.Add(solid); continue; }
            var instance = item as GeometryInstance;
            if (instance == null) continue;
            foreach (var inner in instance.GetInstanceGeometry())
            {
                var innerSolid = inner as Solid;
                if (innerSolid != null && innerSolid.Volume > 1e-9) found.Add(innerSolid);
            }
        }
    solidsCache[e.Id] = found;
    return found;
};

// The lowest point at which a vertical line from `rayFrom` to `rayTo` at
// (x, y) is inside one of these elements, counting only a stretch inside that
// reaches above `reachAbove` - and which element. NaN when there is none.
var inside = new SolidCurveIntersectionOptions();
Func<List<Element>, double, double, double, double, double, Tuple<double, Element>> firstHit =
    (hosts, x, y, rayFrom, rayTo, reachAbove) =>
{
    var best = double.NaN;
    Element which = null;
    if (rayTo - rayFrom < shortest) return Tuple.Create(best, which);
    var ray = Line.CreateBound(new XYZ(x, y, rayFrom), new XYZ(x, y, rayTo));
    foreach (var host in hosts)
        foreach (var solid in solidsOf(host))
        {
            SolidCurveIntersection hit;
            try { hit = solid.IntersectWithCurve(ray, inside); }
            catch (Exception) { continue; }
            if (hit == null) continue;
            for (var i = 0; i < hit.SegmentCount; i++)
            {
                var segment = hit.GetCurveSegment(i);
                var low = Math.Min(segment.GetEndPoint(0).Z, segment.GetEndPoint(1).Z);
                var high = Math.Max(segment.GetEndPoint(0).Z, segment.GetEndPoint(1).Z);
                if (high <= reachAbove) continue;
                if (double.IsNaN(best) || low < best) { best = low; which = host; }
            }
        }
    return Tuple.Create(best, which);
};

// ---------------------------------------------------------------------------
// 1. EVERY WALL READ, AND EVERY REFUSAL DECIDED, BEFORE ANYTHING CHANGES.
// ---------------------------------------------------------------------------
// One wall to shape: the wall, its location line's start and direction, the
// stations along it and the roof's underside at each (NaN where no roof is
// over it), and the roofs it follows.
var toShape = new List<Tuple<Wall, XYZ, XYZ, double[], double[], List<Element>>>();
string refusedAll = null;
if (editScopeFailures == null)
    refusedAll = "This needs Heron's add-in to hand it Revit's failure handling (D-112), and none came. NOTHING WAS CHANGED.";
else if (elements == null || elements.Count == 0)
    refusedAll = "No walls were handed in. Select the walls, or name them, and ask again. NOTHING WAS CHANGED.";
else if (roofs.Count == 0)
    refusedAll = "This model has no roof, so there is nothing to shape a wall to. NOTHING WAS CHANGED.";

if (refusedAll == null)
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;
        var wall = element as Wall;
        if (wall == null) { leftAsTheyWere.Add(named(element) + ": not a wall."); continue; }
        var who = "wall " + wall.Id;

        if (wall.GroupId != null && wall.GroupId != ElementId.InvalidElementId)
        { leftAsTheyWere.Add(who + ": it is in group " + wall.GroupId + " - Edit Group in Revit to shape it."); continue; }

        if (doc.IsWorkshared)
        {
            string owner;
            if (WorksharingUtils.GetCheckoutStatus(doc, wall.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
            { leftAsTheyWere.Add(who + ": owned by " + owner + " - left as it is."); continue; }
        }

        var attachedTop = wall.get_Parameter(BuiltInParameter.WALL_TOP_IS_ATTACHED);
        if (attachedTop != null && attachedTop.AsInteger() == 1)
        {
            leftAsTheyWere.Add(who + ": its top is already attached in Revit (Attach Top/Base), so it already "
                + "follows what it is attached to - left as it is. Detach Top in Revit first to shape it instead.");
            continue;
        }

        if (!wall.CanHaveProfileSketch())
        {
            leftAsTheyWere.Add(who + ": Revit cannot edit this wall's profile - it is curved, tapered, a curtain "
                + "wall, or a panel or infill in one.");
            continue;
        }

        var hasSketch = wall.SketchId != null && wall.SketchId != ElementId.InvalidElementId;
        if (hasSketch && !redraw)
        {
            leftAsTheyWere.Add(who + ": its profile is already edited - by an earlier run or by hand - so it is left "
                + "as it is. Ask again with redraw to draw its top to the roof again; that replaces its outline.");
            continue;
        }
        if (hasSketch)
        {
            var old = doc.GetElement(wall.SketchId) as Sketch;
            if (old != null && old.Profile != null && old.Profile.Size > 1)
            {
                leftAsTheyWere.Add(who + ": its profile has " + old.Profile.Size + " outlines - openings drawn in it by "
                    + "hand - which a redraw would lose, so it is left as it is. Reset Profile in Revit first if they can go.");
                continue;
            }
        }

        var location = wall.Location as LocationCurve;
        var line = location == null ? null : location.Curve as Line;
        var box = wall.get_BoundingBox(null);
        if (line == null || box == null)
        { leftAsTheyWere.Add(who + ": Heron could not read its location line - left as it is."); continue; }

        var start = line.GetEndPoint(0);
        var end = line.GetEndPoint(1);
        var flat = new XYZ(end.X - start.X, end.Y - start.Y, 0);
        var length = flat.GetLength();
        if (length < lowest) { leftAsTheyWere.Add(who + ": shorter than 300 mm - left as it is."); continue; }
        var along = flat.Normalize();
        var across = new XYZ(-along.Y, along.X, 0);

        // THE TWO FACES, from Revit's own side faces - wherever the location line is.
        var offsets = new List<double>();
        foreach (var side in new[] { ShellLayerType.Exterior, ShellLayerType.Interior })
        {
            IList<Reference> references = null;
            try { references = HostObjectUtils.GetSideFaces(wall, side); }
            catch (Exception) { }
            if (references == null) continue;
            foreach (var reference in references)
            {
                var face = wall.GetGeometryObjectFromReference(reference) as PlanarFace;
                if (face == null) continue;
                offsets.Add(new XYZ(face.Origin.X - start.X, face.Origin.Y - start.Y, 0).DotProduct(across));
                break;
            }
        }
        if (offsets.Count != 2)
        { leftAsTheyWere.Add(who + ": Heron could not read its two faces - left as it is."); continue; }
        var faceLow = Math.Min(offsets[0], offsets[1]);
        var faceHigh = Math.Max(offsets[0], offsets[1]);
        var inset = Math.Min(5.0 / MmPerFoot, (faceHigh - faceLow) / 4);
        var faceLines = new[] { faceLow + inset, faceHigh - inset };

        var span = baseAndTop(wall, box);
        var baseZ = span[0];
        var topNow = span[1];
        var rayFrom = baseZ + lowest;

        // The roofs and floors whose plan extent overlaps this wall's.
        Func<BoundingBoxXYZ, bool> overlaps = b =>
            b.Min.X < box.Max.X && b.Max.X > box.Min.X && b.Min.Y < box.Max.Y && b.Max.Y > box.Min.Y;
        var over = roofs.Where(r => overlaps(r.Item2) && r.Item2.Max.Z > topNow - lowest).Select(r => r.Item1).ToList();
        if (over.Count == 0) { noRoof.Add(wall.Id); continue; }
        var rayTo = roofs.Where(r => overlaps(r.Item2)).Max(r => r.Item2.Max.Z) + lowest;
        var between = floors.Where(f => overlaps(f.Item2) && f.Item2.Max.Z > topNow - lowest && f.Item2.Min.Z < rayTo)
            .Select(f => f.Item1).ToList();

        var count = Math.Max(9, Math.Min(401, (int)Math.Ceiling(length / step) + 1));
        var stations = new double[count];
        var undersides = new double[count];
        var followed = new List<Element>();
        Element blockedBy = null;
        var any = false;
        for (var k = 0; k < count && blockedBy == null; k++)
        {
            var s = 1.0 / MmPerFoot + (length - 2.0 / MmPerFoot) * k / (count - 1);
            stations[k] = s;
            var lowestHere = double.NaN;
            var missed = false;
            foreach (var t in faceLines)
            {
                var x = start.X + along.X * s + across.X * t;
                var y = start.Y + along.Y * s + across.Y * t;
                var roof = firstHit(over, x, y, rayFrom, rayTo, topNow - lowest);
                if (double.IsNaN(roof.Item1)) { missed = true; continue; }
                if (!followed.Contains(roof.Item2)) followed.Add(roof.Item2);
                if (between.Count > 0)
                {
                    var floor = firstHit(between, x, y, topNow - lowest, roof.Item1 - shortest, double.MinValue);
                    if (!double.IsNaN(floor.Item1)) { blockedBy = floor.Item2; break; }
                }
                if (double.IsNaN(lowestHere) || roof.Item1 < lowestHere) lowestHere = roof.Item1;
            }
            // A roof over one face only: the top is not raised past where it is.
            if (missed && !double.IsNaN(lowestHere)) lowestHere = Math.Min(lowestHere, topNow);
            undersides[k] = lowestHere;
            if (!double.IsNaN(lowestHere)) any = true;
        }

        if (blockedBy != null)
        {
            leftAsTheyWere.Add(who + ": " + named(blockedBy) + " lies between its top and the roof - Heron shapes a "
                + "wall only to the roof right above it. Left as it is.");
            continue;
        }
        if (!any) { noRoof.Add(wall.Id); continue; }

        var tooLow = false;
        var fits = true;
        for (var k = 0; k < count; k++)
        {
            var z = double.IsNaN(undersides[k]) ? topNow : undersides[k];
            if (z < baseZ + lowest) tooLow = true;
            if (Math.Abs(z - topNow) > sameTop) fits = false;
        }
        if (tooLow)
        {
            leftAsTheyWere.Add(who + ": the roof comes down to less than 300 mm above its base, so shaping it "
                + "there would leave almost no wall - left as it is.");
            continue;
        }
        if (fits && !hasSketch)
        {
            leftAsTheyWere.Add(who + ": its top already meets the roof - nothing to shape.");
            continue;
        }

        toShape.Add(Tuple.Create(wall, start, along, stations, undersides, followed));
    }
}

// ---------------------------------------------------------------------------
// 2. THE NEW TOP, as points along the wall: straight runs merged, ridges put
//    where the runs' lines meet, then carried to the profile's own ends.
// ---------------------------------------------------------------------------
Func<double[], double[], double, double, List<double[]>> topLine = (s, z, sFrom, sTo) =>
{
    var n = s.Length;
    Func<int, int, bool> straight = (a, b) =>
    {
        for (var m = a + 1; m < b; m++)
        {
            var onLine = z[a] + (z[b] - z[a]) * (s[m] - s[a]) / (s[b] - s[a]);
            if (Math.Abs(z[m] - onLine) > fitTolerance) return false;
        }
        return true;
    };
    var runs = new List<int[]>();
    var first = 0;
    while (first < n - 1)
    {
        var last = first + 1;
        while (last + 1 < n && straight(first, last + 1)) last++;
        runs.Add(new[] { first, last });
        first = last;
    }
    Func<int[], double> slope = r => (z[r[1]] - z[r[0]]) / (s[r[1]] - s[r[0]]);

    var vs = new List<double> { s[runs[0][0]] };
    var vz = new List<double> { z[runs[0][0]] };
    for (var r = 0; r < runs.Count; r++)
    {
        var a = runs[r][0];
        var b = runs[r][1];
        var merged = false;
        // A RIDGE: a one-step run between two longer ones is where their lines meet.
        if (b - a == 1 && r > 0 && r < runs.Count - 1)
        {
            var p = runs[r - 1];
            var q = runs[r + 1];
            if (p[1] - p[0] >= 2 && q[1] - q[0] >= 2)
            {
                var m1 = slope(p);
                var m3 = slope(q);
                if (Math.Abs(m1 - m3) > 1e-6)
                {
                    var meet = (z[q[0]] - z[p[0]] + m1 * s[p[0]] - m3 * s[q[0]]) / (m1 - m3);
                    if (meet > s[a] - 1e-9 && meet < s[b] + 1e-9)
                    {
                        vs[vs.Count - 1] = meet;
                        vz[vz.Count - 1] = z[p[0]] + m1 * (meet - s[p[0]]);
                        merged = true;
                    }
                }
            }
        }
        if (!merged) { vs.Add(s[b]); vz.Add(z[b]); }
    }

    // The height at any point along the wall, the end runs carried on past the
    // first and last samples to the profile's own ends.
    Func<double, double> at = x =>
    {
        var c = vs.Count;
        if (x <= vs[0]) return vz[0] + (vz[1] - vz[0]) * (x - vs[0]) / (vs[1] - vs[0]);
        if (x >= vs[c - 1]) return vz[c - 1] + (vz[c - 1] - vz[c - 2]) * (x - vs[c - 1]) / (vs[c - 1] - vs[c - 2]);
        for (var i = 1; i < c; i++)
            if (x <= vs[i]) return vz[i - 1] + (vz[i] - vz[i - 1]) * (x - vs[i - 1]) / (vs[i] - vs[i - 1]);
        return vz[c - 1];
    };

    var points = new List<double[]> { new[] { sFrom, at(sFrom) } };
    for (var i = 0; i < vs.Count; i++)
    {
        if (vs[i] <= sFrom + shortest || vs[i] >= sTo - shortest) continue;
        var previous = points[points.Count - 1];
        if (Math.Abs(vs[i] - previous[0]) < shortest) continue;
        points.Add(new[] { vs[i], vz[i] });
    }
    points.Add(new[] { sTo, at(sTo) });
    return points;
};

// ---------------------------------------------------------------------------
// 3. THE CHANGE, wall by wall. From the first commit on, a refusal THROWS.
// ---------------------------------------------------------------------------
var changedAny = false;
foreach (var job in toShape)
{
    var wall = job.Item1;
    var start = job.Item2;
    var along = job.Item3;
    var who = "wall " + wall.Id;
    string stopped = null;

    var inserts = wall.FindInserts(true, false, true, true).ToList();

    // 3a. A profile sketch - the old one taken away first on a redraw.
    using (var making = new Transaction(doc, "Heron: wall profile"))
    {
        making.Start();
        discipline(making);
        try
        {
            if (wall.SketchId != null && wall.SketchId != ElementId.InvalidElementId)
            {
                wall.RemoveProfileSketch();
                doc.Regenerate();
            }
            wall.CreateProfileSketch();
        }
        catch (Exception ex) { stopped = "Revit would not give " + who + " a profile to edit: " + words(ex) + "."; }
        if (stopped != null) making.RollBack();
        else if (making.Commit() != TransactionStatus.Committed)
            stopped = "Revit would not keep the profile it made for " + who + revitSaid() + ".";
    }
    if (stopped != null)
    {
        if (changedAny) throw new InvalidOperationException(stopped + keptNothing);
        // Nothing is changed yet: this wall is named and the rest still go ahead.
        leftAsTheyWere.Add(who + ": " + stopped + " Left as it is.");
        continue;
    }
    changedAny = true;

    // 3b. Its rectangle: the base, its own top and the plane it lies on.
    var sketch = doc.GetElement(wall.SketchId) as Sketch;
    if (sketch == null || sketch.Profile == null || sketch.Profile.Size == 0 || sketch.SketchPlane == null)
        throw new InvalidOperationException("Revit made a profile for " + who + " and it cannot be read back." + keptNothing);
    var plane = sketch.SketchPlane;
    var normal = plane.GetPlane().Normal;
    if (Math.Abs(normal.Z) > 1e-6 || Math.Abs(normal.DotProduct(along)) > 1e-6)
        throw new InvalidOperationException("The profile Revit made for " + who + " is not upright along the wall, "
            + "so Heron will not draw on it." + keptNothing);

    double sLow = double.MaxValue, sHigh = double.MinValue, zLow = double.MaxValue, zHigh = double.MinValue;
    XYZ corner = null;
    foreach (CurveArray loop in sketch.Profile)
        foreach (Curve curve in loop)
            for (var end = 0; end < 2; end++)
            {
                var point = curve.GetEndPoint(end);
                if (corner == null) corner = point;
                var along0 = new XYZ(point.X - start.X, point.Y - start.Y, 0).DotProduct(along);
                sLow = Math.Min(sLow, along0); sHigh = Math.Max(sHigh, along0);
                zLow = Math.Min(zLow, point.Z); zHigh = Math.Max(zHigh, point.Z);
            }
    // A point of the plane level with the wall's start, at height 0: every new
    // point is this moved along the wall and up, so it lies on the sketch's
    // own plane.
    var cornerAlong = new XYZ(corner.X - start.X, corner.Y - start.Y, 0).DotProduct(along);
    var origin = new XYZ(corner.X - along.X * cornerAlong, corner.Y - along.Y * cornerAlong, 0);
    Func<double, double, XYZ> onPlane = (sAt, zAt) => new XYZ(origin.X + along.X * sAt, origin.Y + along.Y * sAt, zAt);

    var stations = job.Item4;
    var heights = new double[stations.Length];
    for (var k = 0; k < stations.Length; k++)
        heights[k] = double.IsNaN(job.Item5[k]) ? zHigh : job.Item5[k];
    var top = topLine(stations, heights, sLow, sHigh);
    if (top.Any(v => v[1] < zLow + lowest))
        throw new InvalidOperationException("The roof over " + who + " comes down to less than 300 mm above its base "
            + "at the ends of its profile, so Heron will not draw it." + keptNothing);

    // The new outline, closed: along the bottom, up the far end, back along
    // the top, down the near end.
    var outline = new List<XYZ> { onPlane(sLow, zLow), onPlane(sHigh, zLow) };
    for (var i = top.Count - 1; i >= 0; i--) outline.Add(onPlane(top[i][0], top[i][1]));
    var curves = new List<Curve>();
    for (var i = 0; i < outline.Count; i++)
    {
        var here = outline[i];
        var next = outline[(i + 1) % outline.Count];
        if (here.DistanceTo(next) < shortest) continue;
        curves.Add(Line.CreateBound(here, next));
    }
    var highest = top.Max(v => v[1]);

    // 3c. The outline drawn, inside the sketch's own edit mode.
    using (var scope = new SketchEditScope(doc, "Heron: wall top to the roof"))
    {
        if (!scope.IsPermitted || !scope.IsSketchEditingSupported(sketch.Id))
            stopped = "Revit will not open the profile of " + who + " for editing at this moment - another edit "
                + "mode is active, or a transaction is open around this fragment (D-112)";
        else
        {
            scope.Start(sketch.Id);
            using (var drawing = new Transaction(doc, "Heron: wall top to the roof"))
            {
                drawing.Start();
                discipline(drawing);
                try
                {
                    foreach (var id in sketch.GetAllElements())
                        if (doc.GetElement(id) is CurveElement) doc.Delete(id);
                    foreach (var curve in curves) doc.Create.NewModelCurve(curve, plane);
                }
                catch (Exception ex) { stopped = "Revit refused the new top of " + who + ": " + words(ex); }
                if (stopped != null) drawing.RollBack();
                else if (drawing.Commit() != TransactionStatus.Committed)
                    stopped = "Revit would not keep the new top of " + who + revitSaid();
            }
            if (stopped == null)
            {
                try { scope.Commit(editScopeFailures); }
                catch (Exception ex) { stopped = "Revit would not close the profile of " + who + ": " + words(ex) + revitSaid(); }
            }
            if (stopped != null && scope.IsActive) scope.Cancel();
        }
    }
    if (stopped != null) throw new InvalidOperationException(stopped + "." + keptNothing);

    // 3d. READ BACK: the profile's top, and every door, window and opening.
    var after = doc.GetElement(wall.SketchId) as Sketch;
    var readTop = double.MinValue;
    var loops = 0;
    if (after != null && after.Profile != null)
        foreach (CurveArray loop in after.Profile)
        {
            loops++;
            foreach (Curve curve in loop)
                readTop = Math.Max(readTop, Math.Max(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z));
        }
    if (loops != 1 || Math.Abs(readTop - highest) > sameTop)
        throw new InvalidOperationException("The new top of " + who + " does not read back - its profile's highest "
            + "point is " + (readTop == double.MinValue ? "not readable" : mm(readTop - zLow) + " mm above its base")
            + " where " + mm(highest - zLow) + " mm was drawn" + revitSaid() + "." + keptNothing);
    var lost = new List<string>();
    foreach (var id in inserts)
    {
        var insert = doc.GetElement(id);
        var hosted = insert as FamilyInstance;
        if (insert == null || (hosted != null && (hosted.Host == null || hosted.Host.Id != wall.Id)))
            lost.Add(id.ToString());
    }
    if (lost.Count > 0)
        throw new InvalidOperationException("Revit took " + string.Join(", ", lost) + " out of " + who + " when its "
            + "top was shaped" + revitSaid() + "." + keptNothing);

    shaped++;
    shapedIds.Add(wall.Id);
    keptInserts += inserts.Count;
    lines.Add(who + ": top now follows " + string.Join(", ", job.Item6.Select(r => named(r)))
        + " - highest point " + mm(highest - zLow) + " mm above its base, " + (top.Count - 1) + " line(s) along "
        + "the top; " + inserts.Count + " door(s), window(s) and opening(s) kept in it.");
}

if (refusedAll != null) lines.Add(refusedAll);
if (noRoof.Count > 0)
    lines.Add("No roof over: " + string.Join(", ", noRoof.Select(i => "wall " + i)) + " - left as they were.");
lines.AddRange(leftAsTheyWere);

findings.Add(shaped + " wall(s) shaped so the top follows the roof above, read back; " + noRoof.Count
    + " with no roof over them; " + leftAsTheyWere.Count + " left as they were.");
if (shaped > 0) findings.Add(sketchNote);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
