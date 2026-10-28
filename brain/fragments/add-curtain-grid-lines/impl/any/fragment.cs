// NOT STANDALONE. Assumes `doc`, `elements`, `direction`, `positions` and
// `mullionType` are in scope; leaves `added`, `mullionsAdded`, `skipped`,
// `notCurtain`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit keeps lengths in feet;
// the modeller's positions are MILLIMETRES (D-71), turned into feet once, here,
// and every length read back is turned into millimetres once, to be said.
//
// ===========================================================================
// ONE WALL'S OWN GRID, NEVER ITS TYPE.
// ===========================================================================
//
// Every line this adds goes on the selected wall's CurtainGrid - the wall's
// own, the lines Revit's Curtain Grid tool adds by hand. Nothing here reads a
// type parameter to write it, so the wall's type and every other wall built
// from it keep exactly what they had. SET_CURTAIN_WALL_GRID is the one that
// changes a type's Layout and Spacing, for every wall of it.
//
// WHAT A LATER TYPE CHANGE DOES TO THESE LINES IS NOT KNOWN HERE. Whether
// Revit keeps, moves or drops a line added by hand when the type's own grid
// is changed afterwards has not been measured - an ASSUMPTION the proof
// settles, not a promise this file makes.
//
// ===========================================================================
// WHICH OF REVIT'S TWO SETS IS VERTICAL IS MEASURED, NEVER ASSUMED.
// ===========================================================================
//
// CurtainGrid.AddGridLine takes a flag - a U line or a V line - and Revit's
// reference does not tie U or V to vertical or horizontal on a wall. So:
//
//   1. the wall's own lines decide, when it has any: the set its vertical
//      lines are in is the set a new vertical line is asked for in;
//   2. a wall with no line to measure takes the one recorded measurement -
//      SET_CURTAIN_WALL_GRID's proof, Project2, Revit 2024, 2026-10-06, where
//      a wall's vertical lines came back in Revit's V set;
//   3. EVERY new line is then measured - the way its full curve runs and
//      where it sits. One that runs the other way, or sits more than
//      PlacedWithinMm from where it was asked, stops the call, and the
//      executor keeps nothing.
//
// A POSITION IS MEASURED OFF THE GRID ITSELF. The wall's grid outline is read
// from its curtain cells - the edges Revit draws - so "from the start" is the
// grid's edge at the end the wall was drawn from, "from the base" is the
// grid's bottom edge, and each new line's point is put on the grid's own
// plane rather than on the location line, which a curtain wall's Location
// Line setting can move off it.
//
// NOTHING IS WRITTEN UNTIL THE WHOLE REQUEST IS SETTLED. Every refusal is made
// before the first write. A line or a mullion Revit will not take throws, and
// the executor rolls the whole call back: a wall half-divided is worse than
// one left alone.

const double MillimetresPerFoot = 304.8;

// THE BOUNDS ARE FOR A NUMBER TYPED IN THE WRONG UNIT, not a design rule:
// 1.2 meant as metres would put a line 1.2 mm from the wall's edge.
const double InsideEdgeMm = 100.0;

// Closer than this to a line the wall already has that way IS that line.
const double SameLineMm = 5.0;

// A new line measured further than this from where it was asked is wrong.
const double PlacedWithinMm = 1.0;

// The most lines one call adds - a request for more is a typing slip.
const int MostLines = 100;

var added = 0;
var mullionsAdded = 0;
var skipped = new List<string>();
var notCurtain = new List<string>();
var readBack = "";
var refused = "";
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

// Lower case, one space between words. For THIS file's words - the direction
// and "equal" - only; a mullion type's name is the model's and is compared
// exactly.
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

// Millimetres, the way the answer says them: no trailing zeros, a point.
Func<double, string> sayMm = value => value.ToString("0.#", invariant);

// WHICH WAY A CURVE RUNS: 0 vertical, 1 horizontal, 2 at an angle, -1 not
// readable - against Revit's own angle tolerance, the test the two curtain
// neighbours use.
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
    if (asWall != null && itsGrid != null)
    {
        curtainWalls.Add(asWall);
        continue;
    }

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
        WallKind kind = WallKind.Unknown;
        try { kind = asWall.WallType.Kind; } catch (Exception) { kind = WallKind.Unknown; }
        notCurtain.Add(string.Format("'{0}' (id {1}) is {2}, not a curtain wall - it has no grid",
            named, element.Id,
            kind == WallKind.Basic ? "a basic wall" : kind == WallKind.Stacked ? "a stacked wall"
                : "a wall with no curtain grid"));
    }
}

Wall wall = null;
CurtainGrid grid = null;
Line run = null;
var label = "";

if (curtainWalls.Count == 0)
{
    refused = notCurtain.Count == 0
        ? "No wall was given. Select the one curtain wall to add grid lines to, and ask again. "
            + "Nothing was changed"
        : string.Format("Nothing given is a curtain wall - {0}{1}. Select the one curtain wall to add "
            + "grid lines to. Nothing was changed", string.Join("; ", notCurtain.Take(3)),
            notCurtain.Count > 3 ? string.Format(", and {0} more", notCurtain.Count - 3) : "");
}
else if (curtainWalls.Count > 1)
{
    refused = string.Format(
        "{0} curtain walls were given - ids {1}. This adds grid lines to ONE wall at a time, so "
        + "every position is measured on the wall it was meant for. Select just the one. Nothing "
        + "was changed", curtainWalls.Count,
        string.Join(", ", curtainWalls.Take(5).Select(w => w.Id.ToString())));
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
    {
        refused = string.Format(
            "{0} is {1}. This adds lines to a STRAIGHT curtain wall only - along a curve, a "
            + "position from the start is not one straight distance. Nothing was changed", label,
            path == null ? "a wall whose path Revit does not give" : "curved");
    }
}

// In a shared model a wall another user holds cannot take a line - said now,
// with who, and never guessed: Revit is asked.
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
        refused = string.Format("In this shared model {0} is held by {1} - they relinquish it, then "
            + "ask again. Nothing was changed", label, holder);
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
        refused = string.Format(
            "'{0}' is not a direction this adds. Say vertical - lines running up the wall, each "
            + "position measured along it from its start - or horizontal - lines running along it, "
            + "transoms, each position a height above its base. Nothing was changed",
            (direction ?? "").Trim());
}

var askedMm = new List<double>();
var equalPanels = 0;
if (refused.Length == 0)
{
    var text = (positions ?? "").Trim();
    var lowered = plain(text);
    if (lowered.Length == 0)
    {
        refused = "No positions were given. Give them in millimetres, commas between - '900, 2700' "
            + "- or 'equal 6' to divide the wall into six equal panels. Nothing was changed";
    }
    else if (lowered.StartsWith("equal", StringComparison.Ordinal))
    {
        var count = lowered.Substring(5).Trim();
        int panels;
        if (!int.TryParse(count, System.Globalization.NumberStyles.None, invariant, out panels))
            refused = string.Format(
                "'{0}' is not a number of panels. Write equal and a whole number - 'equal 6' divides "
                + "the wall into six equal panels with five lines. Nothing was changed", text);
        else if (panels < 2 || panels > MostLines + 1)
            refused = string.Format(
                "{0} equal panels is outside what this makes - 2 to {1}, one line fewer than the "
                + "panels. Nothing was changed", panels, MostLines + 1);
        else
            equalPanels = panels;
    }
    else
    {
        foreach (var piece in text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var one = piece.Trim();
            if (one.Length == 0) continue;
            double value;
            if (!double.TryParse(one, System.Globalization.NumberStyles.Float, invariant, out value)
                || double.IsNaN(value) || double.IsInfinity(value))
            {
                refused = string.Format(
                    "'{0}' is not a position. Give each in millimetres, digits only - 1200, not 1200mm "
                    + "or 1.2 m - with commas between, or 'equal 6'. Nothing was changed", one);
                break;
            }
            askedMm.Add(value);
        }
        if (refused.Length == 0 && askedMm.Count == 0)
            refused = "No positions were given. Give them in millimetres, commas between - '900, 2700'. "
                + "Nothing was changed";
        if (refused.Length == 0 && askedMm.Count > MostLines)
            refused = string.Format("{0} positions is more than this adds in one go - {1} at most. "
                + "Nothing was changed", askedMm.Count, MostLines);
    }
}

// A MULLION TYPE, BY NAME, AND THE NAME HAS TO MEAN ONE - the rule
// SET_CURTAIN_WALL_MULLIONS uses (D-54): "Family: Type", with or without a
// space before the colon, or the bare type name when no other mullion type
// shares it. Compared exactly. Left out, blank or "none": no mullions.
MullionType mullionToAdd = null;
var mullionAsked = (mullionType ?? "").Trim();
if (refused.Length == 0 && mullionAsked.Length > 0
    && !string.Equals(mullionAsked, "none", StringComparison.OrdinalIgnoreCase))
{
    var known = new List<MullionType>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType())
    {
        var candidate = element as MullionType;
        if (candidate != null) known.Add(candidate);
    }
    var hits = new List<MullionType>();
    foreach (var candidate in known)
    {
        string name = "", family = "";
        try { name = candidate.Name; family = candidate.FamilyName; } catch (Exception) { continue; }
        if (string.Equals(name, mullionAsked, StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(family)
                && (string.Equals(family + ": " + name, mullionAsked, StringComparison.Ordinal)
                    || string.Equals(family + " : " + name, mullionAsked, StringComparison.Ordinal))))
            hits.Add(candidate);
    }
    if (hits.Count == 1)
    {
        mullionToAdd = hits[0];
    }
    else if (hits.Count == 0)
    {
        var spellings = known.Select(t => fullName(t)).Distinct()
            .OrderBy(n => n, StringComparer.Ordinal).ToList();
        refused = known.Count == 0
            ? string.Format("This model has no mullion types at all, so '{0}' cannot go on the new "
                + "lines. Load a mullion family first - Insert > Load Family; Heron does not load "
                + "families. Nothing was changed", mullionAsked)
            : string.Format("No mullion type called '{0}' in this model - capitals count. The mullion "
                + "types it has are: {1}. A mullion family that is not in the model has to be loaded "
                + "first - Insert > Load Family; Heron does not load families. Nothing was changed",
                mullionAsked, string.Join(", ", spellings));
    }
    else
    {
        refused = string.Format("{0} mullion types are called '{1}', so the name does not say which is "
            + "meant. Say which: {2}. Nothing was changed", hits.Count, mullionAsked,
            string.Join(", or ", hits.Select(t => fullName(t)).Distinct()));
    }
}

// ---------------------------------------------------------------------------
// THE GRID, MEASURED - its outline off its curtain cells, its lines off Revit.
// ---------------------------------------------------------------------------
XYZ start = null, flat = null, across = null;
double alongMin = double.MaxValue, alongMax = double.MinValue;
double zMin = double.MaxValue, zMax = double.MinValue;
double plane = 0;
if (refused.Length == 0)
{
    start = run.GetEndPoint(0);
    var heading = run.Direction;
    flat = new XYZ(heading.X, heading.Y, 0);
    if (flat.GetLength() <= 1e-9)
    {
        refused = string.Format("{0} has a location line that does not run along the ground, so "
            + "along it cannot be measured. Nothing was changed", label);
    }
    else
    {
        flat = flat.Normalize();
        across = XYZ.BasisZ.CrossProduct(flat);

        ICollection<CurtainCell> cells = null;
        try { cells = grid.GetCurtainCells(); } catch (Exception) { cells = null; }
        var acrossSum = 0.0;
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
                            var offset = corner - start;
                            var a = offset.DotProduct(flat);
                            alongMin = Math.Min(alongMin, a);
                            alongMax = Math.Max(alongMax, a);
                            zMin = Math.Min(zMin, corner.Z);
                            zMax = Math.Max(zMax, corner.Z);
                            acrossSum += offset.DotProduct(across);
                            corners++;
                        }
                    }
                }
            }
        }
        if (corners == 0 || alongMax - alongMin <= 0 || zMax - zMin <= 0)
            refused = string.Format("Revit gives no outline for the grid of {0}, so where its edges "
                + "are cannot be measured and no position can be placed. Nothing was changed", label);
        else
            plane = acrossSum / corners;
    }
}

// A position along or up the grid, in feet from its start edge or its base -
// the middle of the curve, so a line's two ends cannot disagree about it.
Func<Curve, int, double> positionOf = (curve, which) =>
{
    var middle = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2.0;
    return which == 0 ? (middle - start).DotProduct(flat) - alongMin : middle.Z - zMin;
};

// THE WALL'S LINES: counted by the way each runs and the set Revit returned it
// in - [way 0-2, or 3 unreadable; set 0 = U, 1 = V] - with the positions of
// the vertical and the horizontal ones, in feet.
var countsBefore = new int[4, 2];
var placedBefore = new[] { new List<double>(), new List<double>() };
Action<int[,], List<double>[]> measureLines = (counts, placed) =>
{
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return;
    for (var set = 0; set < 2; set++)
    {
        ICollection<ElementId> ids = null;
        try { ids = set == 0 ? now.GetUGridLineIds() : now.GetVGridLineIds(); } catch (Exception) { ids = null; }
        if (ids == null) continue;
        foreach (var id in ids)
        {
            var line = doc.GetElement(id) as CurtainGridLine;
            Curve curve = null;
            try { curve = line == null ? null : line.FullCurve; } catch (Exception) { curve = null; }
            var ran = runs(curve);
            counts[ran < 0 ? 3 : ran, set]++;
            if (ran == 0 || ran == 1) placed[ran].Add(positionOf(curve, ran));
        }
    }
};

// THE WALL'S MULLIONS, by the way each runs and by type - what gets ordered -
// and how many of one type there are, for the count of what this added.
Func<string> mullionsOnWall = () =>
{
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return "not readable";
    ICollection<ElementId> ids = null;
    try { ids = now.GetMullionIds(); } catch (Exception) { return "not readable"; }
    var tally = new SortedDictionary<string, int>(StringComparer.Ordinal);
    foreach (var id in ids)
    {
        var mullion = doc.GetElement(id) as Mullion;
        if (mullion == null) continue;
        var type = doc.GetElement(mullion.GetTypeId()) as ElementType;
        Curve along = null;
        try { along = mullion.LocationCurve; } catch (Exception) { along = null; }
        var ran = runs(along);
        var key = wayWords[ran < 0 ? 3 : ran] + " '" + fullName(type) + "'";
        int had;
        tally[key] = tally.TryGetValue(key, out had) ? had + 1 : 1;
    }
    return tally.Count == 0 ? "none" : string.Join(", ", tally.Select(p => p.Value + " x " + p.Key));
};
Func<ElementId, int> mullionsOfType = typeId =>
{
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return 0;
    var found = 0;
    foreach (var id in now.GetMullionIds())
    {
        var mullion = doc.GetElement(id) as Mullion;
        if (mullion != null && mullion.GetTypeId().Equals(typeId)) found++;
    }
    return found;
};
// The mullions lying ON one line: running its way, at its position.
Func<int, double, List<Mullion>> mullionsOnLine = (which, at) =>
{
    var found = new List<Mullion>();
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return found;
    foreach (var id in now.GetMullionIds())
    {
        var mullion = doc.GetElement(id) as Mullion;
        if (mullion == null) continue;
        Curve along = null;
        try { along = mullion.LocationCurve; } catch (Exception) { along = null; }
        if (runs(along) != which) continue;
        if (Math.Abs(positionOf(along, which) - at) * MillimetresPerFoot < SameLineMm) found.Add(mullion);
    }
    return found;
};
Func<string> panelsOnWall = () =>
{
    CurtainGrid now = null;
    try { now = wall.CurtainGrid; } catch (Exception) { now = null; }
    if (now == null) return "not readable";
    try { return now.GetPanelIds().Count.ToString(invariant); } catch (Exception) { return "not readable"; }
};

var verticalIsU = false;
var flagFrom = "";
var targets = new List<double>();
var toAdd = new List<double>();
var span = new double[2];
if (refused.Length == 0)
{
    span[0] = alongMax - alongMin;
    span[1] = zMax - zMin;
    measureLines(countsBefore, placedBefore);

    // WHICH SET IS VERTICAL ON THIS WALL - its own lines first.
    var uVertical = countsBefore[0, 0] + countsBefore[1, 1];
    var vVertical = countsBefore[0, 1] + countsBefore[1, 0];
    if (uVertical > 0 && vVertical == 0)
    {
        verticalIsU = true;
        flagFrom = "measured on this wall - its vertical lines are in Revit's U set";
    }
    else if (vVertical > 0 && uVertical == 0)
    {
        verticalIsU = false;
        flagFrom = "measured on this wall - its vertical lines are in Revit's V set";
    }
    else if (uVertical == 0 && vVertical == 0)
    {
        verticalIsU = false;
        flagFrom = "this wall had no vertical or horizontal line to measure, so from the one recorded "
            + "measurement (SET_CURTAIN_WALL_GRID's proof, Project2, Revit 2024): vertical lines in "
            + "Revit's V set - and checked on every line made";
    }
    else
    {
        refused = string.Format(
            "Revit's two sets of grid lines on {0} do not each run one way - U set: {1} vertical, {2} "
            + "horizontal; V set: {3} vertical, {4} horizontal - so which set a new {5} line belongs "
            + "to cannot be told from this wall. Nothing was changed", label,
            countsBefore[0, 0], countsBefore[1, 0], countsBefore[0, 1], countsBefore[1, 1], wayWords[way]);
    }
}

var edgeWords = new[] { "start", "base" };
var sizeWords = new[] { "long", "high" };
if (refused.Length == 0)
{
    var inside = InsideEdgeMm / MillimetresPerFoot;
    if (equalPanels > 0)
    {
        var panel = span[way] / equalPanels;
        if (panel < inside)
            refused = string.Format(
                "{0} equal panels on a grid {1} mm {2} are {3} mm each - narrower than the {4} mm this "
                + "lets a panel be. Nothing was changed", equalPanels, sayMm(span[way] * MillimetresPerFoot),
                sizeWords[way], sayMm(panel * MillimetresPerFoot), sayMm(InsideEdgeMm));
        for (var k = 1; k < equalPanels && refused.Length == 0; k++) targets.Add(span[way] * k / equalPanels);

        // EQUAL PANELS MEANS EQUAL PANELS. A line already there that is not on
        // one of the divisions would cut a panel in two, so the wall would not
        // come out in that many panels - refused, never reported as done.
        if (refused.Length == 0)
        {
            var offDivision = placedBefore[way]
                .Where(e => !targets.Any(t => Math.Abs(t - e) * MillimetresPerFoot < SameLineMm))
                .OrderBy(e => e).ToList();
            if (offDivision.Count > 0)
                refused = string.Format(
                    "{0} already has {1} {2} grid line(s), and {3} of them - at {4} mm from its {5} - "
                    + "{6} not where {7} equal panels put a line, so adding lines would make {8} panels, "
                    + "not {7}. Nothing was changed. Lines its type places stay while the type's {2} "
                    + "layout places them - SET_CURTAIN_WALL_GRID sets that layout, for every wall of the "
                    + "type; or give the positions this wall needs, which are added beside the ones it has",
                    label, placedBefore[way].Count, wayWords[way], offDivision.Count,
                    string.Join(", ", offDivision.Take(6).Select(e => sayMm(e * MillimetresPerFoot))),
                    edgeWords[way], offDivision.Count == 1 ? "is" : "are", equalPanels,
                    equalPanels + offDivision.Count);
        }
    }
    else
    {
        foreach (var mm in askedMm)
        {
            var feet = mm / MillimetresPerFoot;
            if (feet < inside || feet > span[way] - inside)
            {
                refused = string.Format(
                    "{0} mm is {1} - the grid of {2} is {3} mm {4}, and a new line has to sit at least "
                    + "{5} mm inside its edges. Positions are MILLIMETRES from its {6}, so 1.2 m is 1200. "
                    + "Nothing was changed", sayMm(mm),
                    feet <= 0 ? "not inside the wall" : feet < inside ? "too close to its " + edgeWords[way]
                        : feet >= span[way] ? "beyond its " + (way == 0 ? "end" : "top")
                        : "too close to its " + (way == 0 ? "end" : "top"),
                    label, sayMm(span[way] * MillimetresPerFoot), sizeWords[way], sayMm(InsideEdgeMm),
                    edgeWords[way]);
                break;
            }
            targets.Add(feet);
        }
    }
}

// A LINE ALREADY THERE IS NOT DOUBLED, and one asked twice is added once.
if (refused.Length == 0)
{
    foreach (var t in targets)
    {
        var there = placedBefore[way].Where(e => Math.Abs(e - t) * MillimetresPerFoot < SameLineMm).ToList();
        if (there.Count > 0)
        {
            skipped.Add(string.Format("{0} mm - the wall already has a {1} line there, at {2} mm",
                sayMm(t * MillimetresPerFoot), wayWords[way], sayMm(there[0] * MillimetresPerFoot)));
            continue;
        }
        if (toAdd.Any(o => Math.Abs(o - t) * MillimetresPerFoot < SameLineMm))
        {
            skipped.Add(string.Format("{0} mm - asked twice, added once", sayMm(t * MillimetresPerFoot)));
            continue;
        }
        toAdd.Add(t);
    }
}

if (refused.Length == 0)
{
    var panelsBefore = panelsOnWall();
    var mullionsBefore = mullionsOnWall();
    var ofTypeBefore = mullionToAdd == null ? 0 : mullionsOfType(mullionToAdd.Id);
    var isU = way == 0 ? verticalIsU : !verticalIsU;

    // WHERE EACH NEW LINE IS PUT THROUGH: on the grid's own plane, at the asked
    // position, and the other way in the middle of the widest gap between the
    // lines already there - so the point never sits on one of them.
    var other = 1 - way;
    var gaps = new List<double> { 0.0, span[other] };
    gaps.AddRange(placedBefore[other].Where(x => x > 0 && x < span[other]));
    gaps.Sort();
    var crossAt = span[other] / 2.0;
    var widest = 0.0;
    for (var i = 0; i + 1 < gaps.Count; i++)
    {
        if (gaps[i + 1] - gaps[i] > widest)
        {
            widest = gaps[i + 1] - gaps[i];
            crossAt = (gaps[i] + gaps[i + 1]) / 2.0;
        }
    }

    // THE WRITE.
    var made = new List<CurtainGridLine>();
    foreach (var t in toAdd)
    {
        var a = alongMin + (way == 0 ? t : crossAt);
        var z = zMin + (way == 0 ? crossAt : t);
        var onPlane = start + flat * a + across * plane;
        var through = new XYZ(onPlane.X, onPlane.Y, z);
        CurtainGridLine line;
        try
        {
            line = grid.AddGridLine(isU, through, false);
        }
        catch (Exception notTaken)
        {
            throw new InvalidOperationException(string.Format(
                "Revit would not add a {0} grid line at {1} mm from the {2} of {3} - {4}. Nothing this "
                + "call did is kept", wayWords[way], sayMm(t * MillimetresPerFoot), edgeWords[way], label,
                notTaken.Message));
        }
        if (line == null)
            throw new InvalidOperationException(string.Format(
                "Revit added no {0} grid line at {1} mm from the {2} of {3}. Nothing this call did is kept",
                wayWords[way], sayMm(t * MillimetresPerFoot), edgeWords[way], label));
        made.Add(line);
    }

    // THE REBUILD IS NOT SWALLOWED - Revit's reference for Regenerate says a
    // failure there leaves a model that must not be read again.
    if (made.Count > 0)
    {
        try { doc.Regenerate(); }
        catch (Exception notRebuilt)
        {
            throw new InvalidOperationException(string.Format(
                "Revit could not rebuild {0} with the new grid lines - {1}. Nothing this call did is kept",
                label, notRebuilt.Message));
        }
    }

    // EVERY NEW LINE MEASURED: the way it runs and where it sits. Wrong either
    // way and the call stops - the flag, or the outline, was not what was
    // measured, and a line in the wrong place is not the one asked for.
    var ids = new HashSet<string>();
    try
    {
        foreach (var id in wall.CurtainGrid.GetUGridLineIds()) ids.Add(id.ToString());
        foreach (var id in wall.CurtainGrid.GetVGridLineIds()) ids.Add(id.ToString());
    }
    catch (Exception notRead)
    {
        throw new InvalidOperationException(string.Format(
            "Revit would not list the grid lines of {0} after adding them - {1}. Nothing this call did "
            + "is kept", label, notRead.Message));
    }
    for (var i = 0; i < made.Count; i++)
    {
        var line = made[i];
        var askedAt = sayMm(toAdd[i] * MillimetresPerFoot);
        if (!line.IsValidObject || !ids.Contains(line.Id.ToString()))
            throw new InvalidOperationException(string.Format(
                "The {0} line asked for at {1} mm is not among the grid lines of {2} after Revit rebuilt it. "
                + "Nothing this call did is kept", wayWords[way], askedAt, label));
        Curve full = null;
        try { full = line.FullCurve; } catch (Exception) { full = null; }
        var ran = runs(full);
        if (ran != way)
            throw new InvalidOperationException(string.Format(
                "Revit made the line asked for at {0} mm {1} in its {2} set - {3} - and it runs {4}. "
                + "Which of Revit's two sets runs which way on this wall is not what was measured, so "
                + "nothing this call did is kept", askedAt, wayWords[way], isU ? "U" : "V", flagFrom,
                wayWords[ran < 0 ? 3 : ran]));
        var at = positionOf(full, way);
        if (Math.Abs(at - toAdd[i]) * MillimetresPerFoot > PlacedWithinMm)
            throw new InvalidOperationException(string.Format(
                "Revit placed the {0} line asked for at {1} mm from the {2} of {3} at {4} mm. Nothing this "
                + "call did is kept", wayWords[way], askedAt, edgeWords[way], label,
                sayMm(at * MillimetresPerFoot)));
    }
    added = made.Count;

    // MULLIONS ON EACH NEW LINE, every segment of it - unless Revit has already
    // put some there (the wall type's own interior mullions, if Revit adds
    // them to a line drawn by hand - not yet measured), which are named and
    // left: changing a mullion's type is CHANGE_ELEMENT_TYPE's.
    var alreadyMullioned = new List<string>();
    var setsMade = 0;
    if (mullionToAdd != null && made.Count > 0)
    {
        for (var i = 0; i < made.Count; i++)
        {
            var there = mullionsOnLine(way, toAdd[i]);
            if (there.Count > 0)
            {
                var kinds = there.Select(m => fullName(doc.GetElement(m.GetTypeId()) as ElementType))
                    .Distinct().OrderBy(n => n, StringComparer.Ordinal);
                alreadyMullioned.Add(string.Format("{0} mm ({1} x '{2}')", sayMm(toAdd[i] * MillimetresPerFoot),
                    there.Count, string.Join("', '", kinds)));
                continue;
            }
            CurveArray segments = null;
            try { segments = made[i].ExistingSegmentCurves; } catch (Exception) { segments = null; }
            if (segments == null || segments.Size == 0)
                throw new InvalidOperationException(string.Format(
                    "The new {0} line at {1} mm on {2} has no segment Revit can put a mullion on. Nothing "
                    + "this call did is kept", wayWords[way], sayMm(toAdd[i] * MillimetresPerFoot), label));
            ElementSet put;
            try
            {
                put = made[i].AddMullions(segments.get_Item(0), mullionToAdd, false);
            }
            catch (Exception notTaken)
            {
                throw new InvalidOperationException(string.Format(
                    "Revit would not put '{0}' mullions on the new {1} line at {2} mm on {3} - {4}. Nothing "
                    + "this call did is kept", fullName(mullionToAdd), wayWords[way],
                    sayMm(toAdd[i] * MillimetresPerFoot), label, notTaken.Message));
            }
            if (put != null) setsMade += put.Size;
        }
        try { doc.Regenerate(); }
        catch (Exception notRebuilt)
        {
            throw new InvalidOperationException(string.Format(
                "Revit could not rebuild {0} with the new mullions - {1}. Nothing this call did is kept",
                label, notRebuilt.Message));
        }
        // COUNTED OFF THE WALL, before and after - never off what Revit said it made.
        mullionsAdded = mullionsOfType(mullionToAdd.Id) - ofTypeBefore;
    }

    // READ BACK - what Revit holds now, each way, beside what it held.
    var countsAfter = new int[4, 2];
    var placedAfter = new[] { new List<double>(), new List<double>() };
    measureLines(countsAfter, placedAfter);
    Func<int[,], int, string> oneWay = (counts, which) =>
        counts[which, 0] + counts[which, 1] == 0 ? "0"
            : string.Format("{0} (Revit's U set {1}, V set {2})", counts[which, 0] + counts[which, 1],
                counts[which, 0], counts[which, 1]);
    var nowAt = placedAfter[way].OrderBy(x => x).Select(x => sayMm(x * MillimetresPerFoot)).ToList();

    // EQUAL PANELS ARE COUNTED, not assumed: N panels is N - 1 lines that way.
    if (equalPanels > 0 && placedAfter[way].Count != equalPanels - 1)
        throw new InvalidOperationException(string.Format(
            "{0} was asked for {1} equal {2} panels and now has {3} {4} grid lines, which is {5} panels. "
            + "Nothing this call did is kept", label, equalPanels, way == 0 ? "side-by-side" : "stacked",
            placedAfter[way].Count, wayWords[way], placedAfter[way].Count + 1));

    readBack = string.Format(
        "{0}: its grid measured {1} mm long and {2} mm high, from its curtain cells. Vertical grid lines "
        + "{3} before, {4} now; horizontal {5} before, {6} now; at an angle {7} before, {8} now. {9} lines "
        + "now at {10} mm from its {11}. Panels {12} before, {13} now. Mullions before: {14}. Now: {15}. "
        + "New lines asked in Revit's {16} set - {17}",
        label, sayMm(span[0] * MillimetresPerFoot), sayMm(span[1] * MillimetresPerFoot),
        oneWay(countsBefore, 0), oneWay(countsAfter, 0), oneWay(countsBefore, 1), oneWay(countsAfter, 1),
        countsBefore[2, 0] + countsBefore[2, 1], countsAfter[2, 0] + countsAfter[2, 1],
        way == 0 ? "Vertical" : "Horizontal", nowAt.Count == 0 ? "(none)" : string.Join(", ", nowAt),
        edgeWords[way], panelsBefore, panelsOnWall(), mullionsBefore, mullionsOnWall(),
        isU ? "U" : "V", flagFrom);

    // WHAT HAPPENED, THEN WHAT DID NOT.
    var otherWalls = 0;
    ElementId typeId = null;
    try { typeId = wall.GetTypeId(); } catch (Exception) { typeId = null; }
    if (typeId != null)
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
        {
            if (element.Id.Equals(wall.Id)) continue;
            ElementId its = null;
            try { its = element.GetTypeId(); } catch (Exception) { its = null; }
            if (its != null && its.Equals(typeId)) otherWalls++;
        }
    }
    findings.Add(added == 0
        ? string.Format("No line was added to {0} - every position asked for already has a {1} line. "
            + "Nothing was changed", label, wayWords[way])
        : string.Format("Added {0} {1} grid line{2} to {3}, at {4} mm from its {5}{6} - on this wall only. "
            + "{7}", added, wayWords[way], added == 1 ? "" : "s", label,
            string.Join(", ", toAdd.Select(t => sayMm(t * MillimetresPerFoot))), edgeWords[way],
            equalPanels > 0 ? string.Format(", so it is now {0} equal panels {1}", equalPanels,
                way == 0 ? "across" : "high") : "",
            otherWalls == 0 ? "Its type is untouched, and no other wall uses it"
                : string.Format("Its type and the {0} other wall{1} of it are untouched", otherWalls,
                    otherWalls == 1 ? "" : "s")));
    if (skipped.Count > 0)
        findings.Add(string.Format("{0} position{1} skipped, not doubled - see skipped", skipped.Count,
            skipped.Count == 1 ? "" : "s"));
    if (mullionToAdd != null)
    {
        findings.Add(string.Format("'{0}' mullions on the new lines: {1} added, counted on the wall before "
            + "and after{2}", fullName(mullionToAdd), mullionsAdded,
            setsMade == mullionsAdded ? "" : string.Format(" (Revit said it made {0})", setsMade)));
        if (alreadyMullioned.Count > 0)
            findings.Add(string.Format("Revit had already put mullions on {0} of the new lines - {1} - so "
                + "none were added there. CHANGE_ELEMENT_TYPE changes a mullion's type",
                alreadyMullioned.Count, string.Join("; ", alreadyMullioned)));
    }
    if (Math.Abs(alongMin) * MillimetresPerFoot > PlacedWithinMm
        || Math.Abs(alongMax - run.Length) * MillimetresPerFoot > PlacedWithinMm)
        findings.Add(string.Format("The grid's ends are not the ends of the wall's location line - it runs "
            + "{0} to {1} mm along a line {2} mm long - so positions were measured from the grid's own edge "
            + "at the start", sayMm(alongMin * MillimetresPerFoot), sayMm(alongMax * MillimetresPerFoot),
            sayMm(run.Length * MillimetresPerFoot)));
    if (notCurtain.Count > 0)
        findings.Add(string.Format("{0} other element{1} given and left alone - see notCurtain",
            notCurtain.Count, notCurtain.Count == 1 ? "" : "s"));
    findings.Add("Read back from Revit - see readBack");
}
else
{
    findings.Add(refused);
}
