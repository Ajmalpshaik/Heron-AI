// NOT STANDALONE. Assumes `doc`, `elements`, `direction`, `lineAt`,
// `segmentsAt` and `action` are in scope; leaves `changed`, `notFound`,
// `notCurtain`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit keeps lengths in feet;
// the modeller's positions are MILLIMETRES (D-71), turned into feet once, here,
// and every length read back is turned into millimetres once, to be said.
//
// ===========================================================================
// REVIT'S ADD/REMOVE SEGMENTS, TYPED - ON ONE CURTAIN WALL'S OWN GRID.
// ===========================================================================
//
// A curtain grid line is cut into SEGMENTS by the lines that cross it - one
// segment per panel edge. Revit keeps the ones drawn in
// `CurtainGridLine.ExistingSegmentCurves` and the ones taken out in
// `SkippedSegmentCurves`; `RemoveSegment` and `AddSegment` move one between
// the two, and `AddAllSegments` puts every one back. A WHOLE line is taken
// away by deleting it.
//
// A LINE IS NAMED BY WHERE IT IS, the way ADD_CURTAIN_GRID_LINES places one:
// a vertical line by its distance ALONG the wall from its start, a horizontal
// one by its height above the base, both measured off the grid's own outline
// (its curtain cells), within SameLineMm. A SEGMENT is named by a point ON its
// line: a height on a vertical line, a distance from the start on a
// horizontal one - the segment whose span holds it.
//
// A PINNED LINE IS REFUSED, BY NAME. The lines a curtain wall TYPE lays out
// (Fixed Distance, Fixed Number, Maximum or Minimum Spacing) arrive pinned to
// it. Taking one away changes what the type drew on this wall, so it is a
// deliberate act: SET_PIN_STATE unpins it first, or SET_CURTAIN_WALL_GRID
// changes the type's layout for every wall. ASSUMPTION for the proof: that a
// type's own lines read Pinned through the API as they show the pin in Revit.
//
// EVERY CHANGE IS READ BACK after one regeneration: a deleted line is no
// longer on the grid, a removed segment is now among the skipped ones, a
// restored one among the existing. One that is not stops the call, and the
// executor keeps nothing. A segment that takes a mullion with it is counted
// off the wall before and after, never off what Revit said.

const double MillimetresPerFoot = 304.8;

// Closer than this to a line that way IS that line.
const double SameLineMm = 5.0;

// A point closer than this to the end of a segment sits on the joint between
// two segments - which one was meant cannot be told.
const double JointMm = 1.0;

// The most lines, or segment points, one call takes - more is a typing slip.
const int MostAsked = 50;

var changed = 0;
var notFound = new List<string>();
var notCurtain = new List<string>();
var readBack = "";
var refused = "";
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

// Lower case, one space between words - for THIS file's own words only.
Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

// A type the way the Properties palette writes it - "Curtain Wall: Storefront".
Func<ElementType, string> fullName = type =>
{
    if (type == null) return "(no type)";
    string family = "";
    try { family = type.FamilyName; } catch (Exception) { family = ""; }
    return string.IsNullOrEmpty(family) ? type.Name : family + ": " + type.Name;
};

Func<double, string> sayMm = value => value.ToString("0.#", invariant);

// WHICH WAY A CURVE RUNS: 0 vertical, 1 horizontal, 2 at an angle, -1 not
// readable - against Revit's own angle tolerance.
var steep = Math.Cos(doc.Application.AngleTolerance);
var level = Math.Sin(doc.Application.AngleTolerance);
Func<Curve, int> runs = curve =>
{
    if (curve == null) return -1;
    try
    {
        var stretch = curve.GetEndPoint(1) - curve.GetEndPoint(0);
        var length = stretch.GetLength();
        if (length <= 0) return -1;
        var rise = Math.Abs(stretch.Z) / length;
        if (rise >= steep) return 0;
        if (rise <= level) return 1;
        return 2;
    }
    catch (Exception) { return -1; }
};
var wayWords = new[] { "vertical", "horizontal", "at an angle", "unmeasured" };

// Millimetre numbers, digits only, commas between. Null on a word that is
// not one, with the word in `bad`.
Func<string, List<double>> numbers = null;
var bad = "";
numbers = text =>
{
    var found = new List<double>();
    foreach (var piece in (text ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var one = piece.Trim();
        if (one.Length == 0) continue;
        double value;
        if (!double.TryParse(one, System.Globalization.NumberStyles.Float, invariant, out value)
            || double.IsNaN(value) || double.IsInfinity(value))
        {
            bad = one;
            return null;
        }
        found.Add(value);
    }
    return found;
};

// ---------------------------------------------------------------------------
// THE WALL - exactly one curtain wall among what was handed in.
// ---------------------------------------------------------------------------
var curtainWalls = new List<Wall>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    var asWall = element as Wall;
    CurtainGrid itsGrid = null;
    if (asWall != null)
    {
        try { itsGrid = asWall.CurtainGrid; } catch (Exception) { itsGrid = null; }
    }
    if (asWall != null && itsGrid != null) { curtainWalls.Add(asWall); continue; }

    string named = "";
    try { named = element.Name; } catch (Exception) { named = ""; }
    if (asWall == null)
    {
        string category = "";
        try { category = element.Category == null ? "" : element.Category.Name; }
        catch (Exception) { category = ""; }
        notCurtain.Add(string.Format("'{0}' (id {1}{2}) is not a wall", named, element.Id,
            category.Length == 0 ? "" : ", " + category));
    }
    else
    {
        notCurtain.Add(string.Format("'{0}' (id {1}) is a wall with no curtain grid", named, element.Id));
    }
}

Wall wall = null;
CurtainGrid grid = null;
Line run = null;
var label = "";

if (curtainWalls.Count == 0)
{
    refused = notCurtain.Count == 0
        ? "No wall was given. Select the one curtain wall whose grid line or segment is to go, and ask "
            + "again. Nothing was changed"
        : string.Format("Nothing given is a curtain wall - {0}{1}. Select the one curtain wall whose grid "
            + "line or segment is to go. Nothing was changed", string.Join("; ", notCurtain.Take(3)),
            notCurtain.Count > 3 ? string.Format(", and {0} more", notCurtain.Count - 3) : "");
}
else if (curtainWalls.Count > 1)
{
    refused = string.Format(
        "{0} curtain walls were given - ids {1}. This edits ONE wall's grid at a time, so every position "
        + "is measured on the wall it was meant for. Select just the one. Nothing was changed",
        curtainWalls.Count, string.Join(", ", curtainWalls.Take(5).Select(w => w.Id.ToString())));
}
else
{
    wall = curtainWalls[0];
    grid = wall.CurtainGrid;
    WallType itsType = null;
    try { itsType = wall.WallType; } catch (Exception) { itsType = null; }
    label = string.Format("curtain wall {0} ('{1}')", wall.Id, fullName(itsType));
    var located = wall.Location as LocationCurve;
    Curve path = null;
    if (located != null)
    {
        try { path = located.Curve; } catch (Exception) { path = null; }
    }
    run = path as Line;
    if (run == null)
        refused = string.Format("{0} is {1}. This edits a STRAIGHT curtain wall only - along a curve, a "
            + "position from the start is not one straight distance. Nothing was changed", label,
            path == null ? "a wall whose path Revit does not give" : "curved");
}

if (refused.Length == 0 && doc.IsWorkshared)
{
    var holder = "";
    try
    {
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, wall.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
            holder = string.IsNullOrEmpty(owner) ? "another user" : owner;
    }
    catch (Exception) { holder = ""; }
    if (holder.Length > 0)
        refused = string.Format("In this shared model {0} is held by {1} - they relinquish it, then ask "
            + "again. Nothing was changed", label, holder);
}

// ---------------------------------------------------------------------------
// THE REQUEST, READ WHOLE BEFORE ANYTHING IS TOUCHED.
// ---------------------------------------------------------------------------
var way = -1;
if (refused.Length == 0)
{
    var said = plain(direction);
    if (said == "vertical") way = 0;
    else if (said == "horizontal") way = 1;
    else
        refused = string.Format("'{0}' is not a direction. Say vertical - a line running up the wall, "
            + "named by its distance along the wall from its start - or horizontal - a line running "
            + "along it, named by its height above its base. Nothing was changed", (direction ?? "").Trim());
}

// 0 remove, 1 restore.
var mode = -1;
if (refused.Length == 0)
{
    var said = plain(action);
    if (said.Length == 0 || said == "remove") mode = 0;
    else if (said == "restore") mode = 1;
    else
        refused = string.Format("'{0}' is not something this does. Say remove - the line, or the segments "
            + "named, are taken out - or restore - segments taken out before are put back. Nothing was "
            + "changed", (action ?? "").Trim());
}

var linesMm = new List<double>();
if (refused.Length == 0)
{
    var read = numbers(lineAt);
    if (read == null)
        refused = string.Format("'{0}' is not a position. Name each line in millimetres, digits only - "
            + "3000, not 3000mm or 3 m - with commas between. Nothing was changed", bad);
    else if (read.Count == 0)
        refused = "No line was named. Give where it is in millimetres - along the wall from its start for "
            + "a vertical line, above its base for a horizontal one. Nothing was changed";
    else if (read.Count > MostAsked)
        refused = string.Format("{0} lines is more than this edits in one go - {1} at most. Nothing was "
            + "changed", read.Count, MostAsked);
    else
        linesMm = read.Distinct().ToList();
}

// Empty means THE WHOLE LINE.
var pointsMm = new List<double>();
var wholeLine = true;
if (refused.Length == 0)
{
    var said = plain(segmentsAt);
    if (said.Length > 0 && said != "all")
    {
        var read = numbers(segmentsAt);
        if (read == null)
            refused = string.Format("'{0}' is not a point on the line. Name each segment by a point on it "
                + "in millimetres, digits only - a height on a vertical line, a distance from the start on "
                + "a horizontal one - or say all for the whole line. Nothing was changed", bad);
        else if (read.Count > MostAsked)
            refused = string.Format("{0} segment points is more than this takes in one go - {1} at most. "
                + "Nothing was changed", read.Count, MostAsked);
        else if (read.Count > 0)
        {
            pointsMm = read.Distinct().ToList();
            wholeLine = false;
        }
    }
}

// ---------------------------------------------------------------------------
// THE GRID, MEASURED - its outline off its curtain cells.
// ---------------------------------------------------------------------------
XYZ start = null, flat = null;
double alongMin = double.MaxValue, alongMax = double.MinValue;
double zMin = double.MaxValue, zMax = double.MinValue;
if (refused.Length == 0)
{
    start = run.GetEndPoint(0);
    var heading = run.Direction;
    flat = new XYZ(heading.X, heading.Y, 0);
    if (flat.GetLength() <= 1e-9)
        refused = string.Format("{0} has a location line that does not run along the ground, so along it "
            + "cannot be measured. Nothing was changed", label);
    else
    {
        flat = flat.Normalize();
        ICollection<CurtainCell> cells = null;
        try { cells = grid.GetCurtainCells(); } catch (Exception) { cells = null; }
        var corners = 0;
        if (cells != null)
        {
            foreach (var cell in cells)
            {
                if (cell == null) continue;
                CurveArrArray loops = null;
                try { loops = cell.CurveLoops; } catch (Exception) { loops = null; }
                if (loops == null) continue;
                foreach (CurveArray loop in loops)
                {
                    if (loop == null) continue;
                    foreach (Curve edge in loop)
                    {
                        if (edge == null) continue;
                        for (var end = 0; end < 2; end++)
                        {
                            XYZ corner;
                            try { corner = edge.GetEndPoint(end); } catch (Exception) { continue; }
                            var a = (corner - start).DotProduct(flat);
                            alongMin = Math.Min(alongMin, a);
                            alongMax = Math.Max(alongMax, a);
                            zMin = Math.Min(zMin, corner.Z);
                            zMax = Math.Max(zMax, corner.Z);
                            corners++;
                        }
                    }
                }
            }
        }
        if (corners == 0 || alongMax - alongMin <= 0 || zMax - zMin <= 0)
            refused = string.Format("Revit gives no outline for the grid of {0}, so where its lines are "
                + "cannot be measured. Nothing was changed", label);
    }
}

// Where a line sits ACROSS its own run, in feet: a vertical line's distance
// along the wall from the grid's start edge, a horizontal line's height.
Func<Curve, int, double> positionOf = (curve, which) =>
{
    var middle = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2.0;
    return which == 0 ? (middle - start).DotProduct(flat) - alongMin : middle.Z - zMin;
};
// Where a point sits ALONG a line of that way, in feet: a height on a
// vertical line, a distance from the start on a horizontal one.
Func<XYZ, int, double> alongLine = (point, which) =>
    which == 0 ? point.Z - zMin : (point - start).DotProduct(flat) - alongMin;

// The segment of `curves` whose span along its line holds `at` (feet):
// 1 found (in `hit`), 0 none, -1 on a joint.
Curve hit = null;
Func<CurveArray, int, double, int> segmentHolding = (curves, which, at) =>
{
    hit = null;
    if (curves == null) return 0;
    var joint = JointMm / MillimetresPerFoot;
    foreach (Curve piece in curves)
    {
        if (piece == null) continue;
        var a = alongLine(piece.GetEndPoint(0), which);
        var b = alongLine(piece.GetEndPoint(1), which);
        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);
        if (at < lo - joint || at > hi + joint) continue;
        if (Math.Abs(at - lo) <= joint || Math.Abs(at - hi) <= joint) return -1;
        hit = piece;
        return 1;
    }
    return 0;
};

Func<CurveArray, int> sizeOf = curves =>
{
    if (curves == null) return 0;
    try { return curves.Size; } catch (Exception) { return 0; }
};

// The wall's lines that way, with where each sits.
Func<List<KeyValuePair<CurtainGridLine, double>>> linesThatWay = () =>
{
    var found = new List<KeyValuePair<CurtainGridLine, double>>();
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return found;
    var ids = new List<ElementId>();
    try { ids.AddRange(now.GetUGridLineIds()); } catch (Exception) { }
    try { ids.AddRange(now.GetVGridLineIds()); } catch (Exception) { }
    foreach (var id in ids)
    {
        var line = doc.GetElement(id) as CurtainGridLine;
        Curve curve = null;
        try { curve = line == null ? null : line.FullCurve; } catch (Exception) { curve = null; }
        if (runs(curve) != way) continue;
        found.Add(new KeyValuePair<CurtainGridLine, double>(line, positionOf(curve, way)));
    }
    return found;
};

Func<string> countOnWall = () =>
{
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return "not readable";
    int panels = -1, mullions = -1;
    try { panels = now.GetPanelIds().Count; } catch (Exception) { panels = -1; }
    try { mullions = now.GetMullionIds().Count; } catch (Exception) { mullions = -1; }
    return string.Format("{0} panels, {1} mullions", panels < 0 ? "?" : panels.ToString(invariant),
        mullions < 0 ? "?" : mullions.ToString(invariant));
};

// ---------------------------------------------------------------------------
// WHAT EACH NAMED LINE IS, AND WHAT IS TO HAPPEN TO IT - settled before any
// write. 0 delete the line, 1 remove segments, 2 restore all, 3 restore these.
// ---------------------------------------------------------------------------
var chosen = new List<CurtainGridLine>();
var chosenAt = new List<double>();
var plans = new List<int>();
var plannedCurves = new List<List<Curve>>();
var plannedPoints = new List<List<double>>();
var nothingToDo = new List<string>();
var before = "";
var linesBefore = 0;
if (refused.Length == 0)
{
    var known = linesThatWay();
    linesBefore = known.Count;
    before = countOnWall();
    var knownWords = known.Count == 0 ? "it has none"
        : "they are at " + string.Join(", ", known.Select(k => k.Value).OrderBy(v => v)
            .Select(v => sayMm(v * MillimetresPerFoot))) + " mm";
    foreach (var mm in linesMm)
    {
        var at = mm / MillimetresPerFoot;
        var near = known.Where(k => Math.Abs(k.Value - at) * MillimetresPerFoot < SameLineMm)
            .OrderBy(k => Math.Abs(k.Value - at)).ToList();
        if (near.Count == 0)
        {
            notFound.Add(string.Format("no {0} line within {1} mm of {2} mm - {3}", wayWords[way],
                sayMm(SameLineMm), sayMm(mm), knownWords));
            continue;
        }
        var line = near[0].Key;
        if (chosen.Any(c => c.Id.Equals(line.Id))) continue;
        chosen.Add(line);
        chosenAt.Add(near[0].Value);
    }
    if (notFound.Count > 0)
        refused = string.Format("{0} on {1}: {2}. Nothing was changed", notFound.Count == 1
            ? "A line named is not there" : notFound.Count + " lines named are not there", label,
            string.Join("; ", notFound));
}

if (refused.Length == 0)
{
    for (var i = 0; i < chosen.Count && refused.Length == 0; i++)
    {
        var line = chosen[i];
        var where = string.Format("the {0} line at {1} mm", wayWords[way], sayMm(chosenAt[i] * MillimetresPerFoot));
        var pinned = false;
        try { pinned = line.Pinned; } catch (Exception) { pinned = false; }
        if (pinned)
        {
            refused = string.Format("{0} of {1} is pinned - the lines a curtain wall type lays out arrive "
                + "pinned to it. Taking it {2} changes what the type drew on this wall: unpin that line "
                + "first (SET_PIN_STATE), or change the type's own layout for every wall of it "
                + "(SET_CURTAIN_WALL_GRID). Nothing was changed", where, label,
                wholeLine && mode == 0 ? "away" : "apart");
            break;
        }

        CurveArray existing = null, skippedNow = null;
        try { existing = line.ExistingSegmentCurves; } catch (Exception) { existing = null; }
        try { skippedNow = line.SkippedSegmentCurves; } catch (Exception) { skippedNow = null; }
        var curves = new List<Curve>();
        var points = new List<double>();

        if (mode == 0 && wholeLine)
        {
            plans.Add(0);
        }
        else if (mode == 1 && wholeLine)
        {
            if (sizeOf(skippedNow) == 0)
            {
                nothingToDo.Add(where + " has no segment taken out - nothing to put back");
                plans.Add(-1);
            }
            else plans.Add(2);
        }
        else
        {
            foreach (var mm in pointsMm)
            {
                var at = mm / MillimetresPerFoot;
                var inExisting = segmentHolding(existing, way, at);
                var existingHit = hit;
                var inSkipped = segmentHolding(skippedNow, way, at);
                var skippedHit = hit;
                if (inExisting < 0 || inSkipped < 0)
                {
                    refused = string.Format("{0} mm sits on the joint between two segments of {1} - which one "
                        + "is meant cannot be told. Give a point inside the panel edge it bounds. Nothing was "
                        + "changed", sayMm(mm), where);
                    break;
                }
                if (mode == 0)
                {
                    if (inExisting == 1) { curves.Add(existingHit); points.Add(mm); }
                    else if (inSkipped == 1) nothingToDo.Add(string.Format("the segment of {0} at {1} mm is "
                        + "already taken out", where, sayMm(mm)));
                    else
                    {
                        refused = string.Format("{0} has no segment at {1} mm - it runs {2} to {3} mm {4}. "
                            + "Nothing was changed", where, sayMm(mm),
                            sayMm(alongLine(line.FullCurve.GetEndPoint(0), way) * MillimetresPerFoot),
                            sayMm(alongLine(line.FullCurve.GetEndPoint(1), way) * MillimetresPerFoot),
                            way == 0 ? "above the base" : "from the start");
                        break;
                    }
                }
                else
                {
                    if (inSkipped == 1) { curves.Add(skippedHit); points.Add(mm); }
                    else if (inExisting == 1) nothingToDo.Add(string.Format("the segment of {0} at {1} mm is "
                        + "already there", where, sayMm(mm)));
                    else
                    {
                        refused = string.Format("{0} has no segment at {1} mm to put back. Nothing was changed",
                            where, sayMm(mm));
                        break;
                    }
                }
            }
            if (refused.Length > 0) break;

            // Taking out EVERY segment a line has is taking the line away -
            // asked for by "all", never by listing each panel edge.
            if (mode == 0 && curves.Count > 0 && curves.Count >= sizeOf(existing))
            {
                refused = string.Format("That is every segment {0} has left ({1}). To take the whole line "
                    + "away, say all for the segments. Nothing was changed", where, sizeOf(existing));
                break;
            }
            plans.Add(curves.Count == 0 ? -1 : (mode == 0 ? 1 : 3));
        }
        plannedCurves.Add(curves);
        plannedPoints.Add(points);
    }
}

if (refused.Length == 0 && plans.All(p => p < 0))
    refused = string.Format("Nothing to change on {0}: {1}. Nothing was changed", label,
        string.Join("; ", nothingToDo));

// ---------------------------------------------------------------------------
// THE WRITE, then every change read back after one regeneration.
// ---------------------------------------------------------------------------
if (refused.Length == 0)
{
    var linesGone = 0;
    var segmentsOut = 0;
    var segmentsBack = 0;
    var alsoWent = 0;
    var done = new List<string>();
    for (var i = 0; i < chosen.Count; i++)
    {
        var line = chosen[i];
        var where = string.Format("the {0} line at {1} mm", wayWords[way], sayMm(chosenAt[i] * MillimetresPerFoot));
        try
        {
            if (plans[i] == 0)
            {
                var lineId = line.Id;
                var went = doc.Delete(lineId);
                if (went != null) alsoWent += Math.Max(0, went.Count - 1);
            }
            else if (plans[i] == 1) foreach (var curve in plannedCurves[i]) line.RemoveSegment(curve);
            else if (plans[i] == 2) line.AddAllSegments();
            else if (plans[i] == 3) foreach (var curve in plannedCurves[i]) line.AddSegment(curve);
        }
        catch (Exception notTaken)
        {
            throw new InvalidOperationException(string.Format("Revit would not {0} {1} of {2} - {3}. Nothing "
                + "this call did is kept", plans[i] == 0 ? "delete" : plans[i] == 1 ? "take segments out of"
                : "put segments back on", where, label, notTaken.Message));
        }
    }
    try { doc.Regenerate(); }
    catch (Exception notRebuilt)
    {
        throw new InvalidOperationException(string.Format("Revit could not rebuild {0} after the change - "
            + "{1}. Nothing this call did is kept", label, notRebuilt.Message));
    }

    var after = linesThatWay();
    for (var i = 0; i < chosen.Count; i++)
    {
        if (plans[i] < 0) continue;
        var where = string.Format("the {0} line at {1} mm", wayWords[way], sayMm(chosenAt[i] * MillimetresPerFoot));
        if (plans[i] == 0)
        {
            var still = after.Any(k => Math.Abs(k.Value - chosenAt[i]) * MillimetresPerFoot < SameLineMm);
            if (still)
                throw new InvalidOperationException(string.Format("{0} of {1} was deleted and a {2} line is "
                    + "still there after Revit rebuilt the wall - the type may have drawn it again. Nothing this "
                    + "call did is kept", where, label, wayWords[way]));
            linesGone++;
            done.Add(where + " taken away");
            continue;
        }
        var line = chosen[i];
        CurveArray existing = null, skippedNow = null;
        try { existing = line.ExistingSegmentCurves; } catch (Exception) { existing = null; }
        try { skippedNow = line.SkippedSegmentCurves; } catch (Exception) { skippedNow = null; }
        if (plans[i] == 2)
        {
            if (sizeOf(skippedNow) != 0)
                throw new InvalidOperationException(string.Format("{0} of {1} still has {2} segment(s) taken "
                    + "out after every one was put back. Nothing this call did is kept", where, label,
                    sizeOf(skippedNow)));
            segmentsBack++;
            done.Add(where + ": every segment put back");
            continue;
        }
        foreach (var mm in plannedPoints[i])
        {
            var at = mm / MillimetresPerFoot;
            var landed = segmentHolding(plans[i] == 1 ? skippedNow : existing, way, at);
            if (landed != 1)
                throw new InvalidOperationException(string.Format("The segment of {0} at {1} mm is not {2} "
                    + "after Revit rebuilt {3}. Nothing this call did is kept", where, sayMm(mm),
                    plans[i] == 1 ? "among those taken out" : "back", label));
            if (plans[i] == 1) segmentsOut++; else segmentsBack++;
        }
        done.Add(string.Format("{0}: the segment{1} at {2} mm {3}", where,
            plannedPoints[i].Count == 1 ? "" : "s", string.Join(", ", plannedPoints[i].Select(sayMm)),
            plans[i] == 1 ? "taken out" : "put back"));
    }
    changed = linesGone + segmentsOut + segmentsBack;

    readBack = string.Format("{0}: {1} lines {2} before, {3} now. Before: {4}. Now: {5}. {6}",
        label, wayWords[way], linesBefore, after.Count, before,
        countOnWall(), string.Join("; ", done));
    findings.Add(string.Format("{0} on {1}, on this wall only - its type and every other wall of it are "
        + "untouched", string.Join("; ", done), label));
    if (alsoWent > 0)
        findings.Add(string.Format("Deleting the line took {0} other element(s) with it - the mullions on it",
            alsoWent));
    if (nothingToDo.Count > 0)
        findings.Add("Left as they were: " + string.Join("; ", nothingToDo));
    if (notCurtain.Count > 0)
        findings.Add(string.Format("{0} other element{1} given and left alone - see notCurtain",
            notCurtain.Count, notCurtain.Count == 1 ? "" : "s"));
    findings.Add("Read back from Revit - see readBack");
}
else
{
    findings.Add(refused);
}
