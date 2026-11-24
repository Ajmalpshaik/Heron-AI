// NOT STANDALONE. Assumes `doc`, `elements`, `move`, `direction` and `lineAt`
// are in scope; leaves `moved`, `notMovable`, `readBack`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit keeps lengths in feet;
// the modeller's distances are MILLIMETRES (D-71), turned into feet once, here,
// and every length read back is turned into millimetres once, to be said.
//
// ===========================================================================
// A MULLION IS MOVED BY MOVING THE GRID LINE IT SITS ON.
// ===========================================================================
//
// In Revit a mullion follows its curtain grid line: drag the line and its
// mullions and the panels either side go with it. So "move this mullion" is
// this: the grid line under each mullion handed in is found - the line of the
// same wall running the same way within SameLineMm of it - and THAT is moved,
// along the wall for a vertical line, up or down for a horizontal one. A
// mullion on the wall's border sits on no grid line and is named, never moved.
//
// A LINE CAN ALSO BE NAMED BY WHERE IT IS - a curtain wall handed in with
// `direction` and `lineAt`, the way ADD_CURTAIN_GRID_LINES places a line.
//
// `move` IS A DISTANCE OR A PLACE. "+300" or "300" moves each line 300 mm
// toward the wall's END (the end it was drawn to) or UP; "-300" toward its
// start or down; "to 1800" puts ONE line at 1800 mm from the start, or above
// the base. Left and right depend on the view, so they are never guessed here.
//
// NO LINE IS MOVED ONTO, OR PAST, ANOTHER, and none closer than InsideEdgeMm
// to the grid's edge: either would merge or reorder panels, and a number
// typed in metres (0.3 for 300) would otherwise move a line 0.3 mm. A PINNED
// line - a type's own, laid out by its spacing - is refused by name: unpin it
// first (SET_PIN_STATE) or change the type (SET_CURTAIN_WALL_GRID).
//
// EVERY LINE IS MEASURED AFTER ONE REGENERATION: it must sit within
// PlacedWithinMm of where it was sent, and the mullions that were on it must
// be on it there. Either failing stops the call, and the executor keeps
// nothing. ASSUMPTION for the proof: that ElementTransformUtils.MoveElement
// moves an unpinned curtain grid line, and its mullions, as Revit's Move
// tool does - the read-back is what settles it.

const double MillimetresPerFoot = 304.8;
const double InsideEdgeMm = 100.0;
const double SameLineMm = 5.0;
const double PlacedWithinMm = 1.0;
const double MostMoveMm = 100000.0;

var moved = 0;
var notMovable = new List<string>();
var readBack = "";
var refused = "";
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

Func<ElementType, string> fullName = type =>
{
    if (type == null) return "(no type)";
    string family = "";
    try { family = type.FamilyName; } catch (Exception) { family = ""; }
    return string.IsNullOrEmpty(family) ? type.Name : family + ": " + type.Name;
};

Func<double, string> sayMm = value => value.ToString("0.#", invariant);

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

Func<Element, string> nameOf = element =>
{
    string named = "";
    try { named = element.Name; } catch (Exception) { named = ""; }
    string category = "";
    try { category = element.Category == null ? "" : element.Category.Name; } catch (Exception) { category = ""; }
    return string.Format("'{0}' (id {1}{2})", named, element.Id, category.Length == 0 ? "" : ", " + category);
};

// The grid line ids of one wall, both of Revit's sets.
Func<Wall, List<ElementId>> lineIdsOf = owner =>
{
    var ids = new List<ElementId>();
    CurtainGrid itsGrid = null;
    try { itsGrid = owner.CurtainGrid; } catch (Exception) { itsGrid = null; }
    if (itsGrid == null) return ids;
    try { ids.AddRange(itsGrid.GetUGridLineIds()); } catch (Exception) { }
    try { ids.AddRange(itsGrid.GetVGridLineIds()); } catch (Exception) { }
    return ids;
};

// ---------------------------------------------------------------------------
// WHAT WAS HANDED IN: a curtain wall, grid lines, or mullions - of ONE wall.
// ---------------------------------------------------------------------------
var walls = new List<Wall>();
var handedLines = new List<CurtainGridLine>();
var handedMullions = new List<Mullion>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    var asWall = element as Wall;
    CurtainGrid itsGrid = null;
    if (asWall != null)
    {
        try { itsGrid = asWall.CurtainGrid; } catch (Exception) { itsGrid = null; }
        if (itsGrid != null) { walls.Add(asWall); continue; }
    }
    var asLine = element as CurtainGridLine;
    if (asLine != null) { handedLines.Add(asLine); continue; }
    var asMullion = element as Mullion;
    if (asMullion != null) { handedMullions.Add(asMullion); continue; }
    notMovable.Add(nameOf(element) + " is not a curtain wall, a curtain grid line or a mullion - left alone");
}

// Lines and mullions are found on their wall by asking every curtain wall
// which ids it holds - never by a host property whose meaning differs
// between a wall and a panel.
if (handedLines.Count + handedMullions.Count > 0)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        var candidate = element as Wall;
        if (candidate == null || walls.Any(w => w.Id.Equals(candidate.Id))) continue;
        CurtainGrid itsGrid = null;
        try { itsGrid = candidate.CurtainGrid; } catch (Exception) { itsGrid = null; }
        if (itsGrid == null) continue;
        var lineIds = lineIdsOf(candidate);
        ICollection<ElementId> mullionIds = null;
        try { mullionIds = itsGrid.GetMullionIds(); } catch (Exception) { mullionIds = null; }
        if (handedLines.Any(l => lineIds.Contains(l.Id))
            || (mullionIds != null && handedMullions.Any(m => mullionIds.Contains(m.Id))))
            walls.Add(candidate);
    }
}

Wall wall = null;
Line run = null;
var label = "";
if (walls.Count == 0)
{
    refused = notMovable.Count == 0 && handedLines.Count + handedMullions.Count == 0
        ? "Nothing was given. Select the mullion or the curtain grid line to move - or the curtain wall, "
            + "with which line. Nothing was changed"
        : "No curtain wall holds what was given" + (notMovable.Count > 0 ? " - " + string.Join("; ",
            notMovable.Take(3)) : "") + ". Select the mullion or the curtain grid line to move. Nothing was "
            + "changed";
}
else if (walls.Count > 1)
{
    refused = string.Format("What was given is on {0} curtain walls - ids {1}. This moves lines on ONE wall "
        + "at a time, so every distance is measured on the wall it was meant for. Nothing was changed",
        walls.Count, string.Join(", ", walls.Take(5).Select(w => w.Id.ToString())));
}
else
{
    wall = walls[0];
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
        refused = string.Format("{0} is {1}. This moves lines on a STRAIGHT curtain wall only - along a "
            + "curve, a distance along it is not one straight move. Nothing was changed", label,
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
// THE MOVE ASKED FOR: a signed distance, or "to" a position.
// ---------------------------------------------------------------------------
var toPlace = false;
var amountMm = 0.0;
if (refused.Length == 0)
{
    var said = plain(move);
    var number = said;
    if (said.StartsWith("to ", StringComparison.Ordinal)) { toPlace = true; number = said.Substring(3).Trim(); }
    if (number.StartsWith("+", StringComparison.Ordinal)) number = number.Substring(1);
    double value;
    if (said.Length == 0)
        refused = "No move was given. Say how far in millimetres - 300 toward the wall's end or up, -300 "
            + "toward its start or down - or where to: to 1800. Nothing was changed";
    else if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariant, out value)
        || double.IsNaN(value) || double.IsInfinity(value))
        refused = string.Format("'{0}' is not a move. Say how far in millimetres, digits only - 300 toward "
            + "the wall's end or up, -300 toward its start or down - or where to: to 1800. Left and right "
            + "depend on the view, so they are not taken. Nothing was changed", (move ?? "").Trim());
    else if (!toPlace && Math.Abs(value) < SameLineMm)
        refused = string.Format("A move of {0} mm is less than {1} mm - nothing a modeller would see. "
            + "Nothing was changed", sayMm(value), sayMm(SameLineMm));
    else if (Math.Abs(value) > MostMoveMm)
        refused = string.Format("{0} mm is further than any curtain wall is long. Nothing was changed",
            sayMm(value));
    else
        amountMm = value;
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
        try { cells = wall.CurtainGrid.GetCurtainCells(); } catch (Exception) { cells = null; }
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

Func<Curve, int, double> positionOf = (curve, which) =>
{
    var middle = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2.0;
    return which == 0 ? (middle - start).DotProduct(flat) - alongMin : middle.Z - zMin;
};

// Every line of the wall that runs vertical or horizontal: [line, way, at].
Func<List<Tuple<CurtainGridLine, int, double>>> allLines = () =>
{
    var found = new List<Tuple<CurtainGridLine, int, double>>();
    foreach (var id in lineIdsOf(wall))
    {
        var line = doc.GetElement(id) as CurtainGridLine;
        Curve curve = null;
        try { curve = line == null ? null : line.FullCurve; } catch (Exception) { curve = null; }
        var ran = runs(curve);
        if (ran == 0 || ran == 1) found.Add(Tuple.Create(line, ran, positionOf(curve, ran)));
    }
    return found;
};

// The mullions lying on a line: running its way, at its position.
Func<int, double, int> mullionsAt = (which, at) =>
{
    var count = 0;
    ICollection<ElementId> ids = null;
    try { ids = wall.CurtainGrid.GetMullionIds(); } catch (Exception) { ids = null; }
    if (ids == null) return 0;
    foreach (var id in ids)
    {
        var mullion = doc.GetElement(id) as Mullion;
        if (mullion == null) continue;
        Curve along = null;
        try { along = mullion.LocationCurve; } catch (Exception) { along = null; }
        if (runs(along) != which) continue;
        if (Math.Abs(positionOf(along, which) - at) * MillimetresPerFoot < SameLineMm) count++;
    }
    return count;
};

// ---------------------------------------------------------------------------
// WHICH LINES, settled before any write.
// ---------------------------------------------------------------------------
var lines = new List<Tuple<CurtainGridLine, int, double>>();
var known = new List<Tuple<CurtainGridLine, int, double>>();
if (refused.Length == 0)
{
    known = allLines();
    Action<CurtainGridLine> take = line =>
    {
        var entry = known.FirstOrDefault(k => k.Item1.Id.Equals(line.Id));
        if (entry == null)
            notMovable.Add(string.Format("grid line {0} runs at an angle - only vertical and horizontal lines "
                + "are moved", line.Id));
        else if (!lines.Any(l => l.Item1.Id.Equals(line.Id))) lines.Add(entry);
    };
    foreach (var line in handedLines) take(line);
    foreach (var mullion in handedMullions)
    {
        Curve along = null;
        try { along = mullion.LocationCurve; } catch (Exception) { along = null; }
        var ran = runs(along);
        if (ran != 0 && ran != 1)
        {
            notMovable.Add(nameOf(mullion) + " runs at an angle, or its line cannot be read - left alone");
            continue;
        }
        var at = positionOf(along, ran);
        var under = known.Where(k => k.Item2 == ran && Math.Abs(k.Item3 - at) * MillimetresPerFoot < SameLineMm)
            .ToList();
        if (under.Count == 0)
        {
            notMovable.Add(string.Format("{0} sits on the wall's {1} edge, not on a grid line - a border "
                + "mullion moves only with the wall", nameOf(mullion), ran == 0 ? "end" : "top or bottom"));
            continue;
        }
        take(under[0].Item1);
    }
    if (walls.Count == 1 && handedLines.Count + handedMullions.Count == 0)
    {
        // A wall alone: the line is named by `direction` and `lineAt`.
        var said = plain(direction);
        var way = said == "vertical" ? 0 : said == "horizontal" ? 1 : -1;
        double at;
        if (way < 0)
            refused = string.Format("Only the curtain wall was given, so say which line: direction vertical "
                + "or horizontal, and lineAt its position in millimetres - or select the mullion itself. "
                + "'{0}' is not a direction. Nothing was changed", (direction ?? "").Trim());
        else if (!double.TryParse(plain(lineAt), System.Globalization.NumberStyles.Float, invariant, out at))
            refused = string.Format("Only the curtain wall was given, so say where the line is - lineAt in "
                + "millimetres, digits only. '{0}' is not one. Nothing was changed", (lineAt ?? "").Trim());
        else
        {
            var feet = at / MillimetresPerFoot;
            var near = known.Where(k => k.Item2 == way && Math.Abs(k.Item3 - feet) * MillimetresPerFoot < SameLineMm)
                .ToList();
            if (near.Count == 0)
                refused = string.Format("{0} has no {1} line within {2} mm of {3} mm - its {1} lines are at {4}. "
                    + "Nothing was changed", label, wayWords[way], sayMm(SameLineMm), sayMm(at),
                    known.Count(k => k.Item2 == way) == 0 ? "none"
                        : string.Join(", ", known.Where(k => k.Item2 == way).Select(k => k.Item3).OrderBy(v => v)
                            .Select(v => sayMm(v * MillimetresPerFoot))) + " mm");
            else lines.Add(near[0]);
        }
    }
    if (refused.Length == 0 && lines.Count == 0)
        refused = string.Format("No grid line to move on {0}: {1}. Nothing was changed", label,
            string.Join("; ", notMovable));
    if (refused.Length == 0 && toPlace && lines.Count > 1)
        refused = string.Format("'to' puts ONE line at a place, and {0} lines were found. Move them by a "
            + "distance instead, or select one. Nothing was changed", lines.Count);
}

// THE TARGETS, each checked against the edges and every line not moving.
var targets = new List<double>();
var mullionsBefore = new List<int>();
if (refused.Length == 0)
{
    foreach (var entry in lines)
    {
        var way = entry.Item2;
        var span = way == 0 ? alongMax - alongMin : zMax - zMin;
        var target = toPlace ? amountMm / MillimetresPerFoot : entry.Item3 + amountMm / MillimetresPerFoot;
        var where = string.Format("the {0} line at {1} mm", wayWords[way], sayMm(entry.Item3 * MillimetresPerFoot));
        var edge = InsideEdgeMm / MillimetresPerFoot;
        if (target < edge || target > span - edge)
        {
            refused = string.Format("Moving {0} of {1} would put it at {2} mm, within {3} mm of the grid's edge "
                + "(it runs 0 to {4} mm {5}) - a number typed in metres? 0.3 m is 300. Nothing was changed",
                where, label, sayMm(target * MillimetresPerFoot), sayMm(InsideEdgeMm),
                sayMm(span * MillimetresPerFoot), way == 0 ? "along the wall" : "up the wall");
            break;
        }
        if (Math.Abs(target - entry.Item3) * MillimetresPerFoot < SameLineMm)
        {
            refused = string.Format("{0} of {1} is already at {2} mm. Nothing was changed", where, label,
                sayMm(entry.Item3 * MillimetresPerFoot));
            break;
        }
        var lo = Math.Min(entry.Item3, target);
        var hi = Math.Max(entry.Item3, target);
        var inTheWay = known.Where(k => k.Item2 == way && !lines.Any(l => l.Item1.Id.Equals(k.Item1.Id))
            && k.Item3 > lo - SameLineMm / MillimetresPerFoot && k.Item3 < hi + SameLineMm / MillimetresPerFoot)
            .Select(k => sayMm(k.Item3 * MillimetresPerFoot)).ToList();
        if (inTheWay.Count > 0)
        {
            refused = string.Format("Moving {0} of {1} to {2} mm would put it onto or past the {3} line at {4} mm - "
                + "panels would merge or change order. Move that one first, or ask for less. Nothing was changed",
                where, label, sayMm(target * MillimetresPerFoot), wayWords[way], string.Join(", ", inTheWay));
            break;
        }
        var pinned = false;
        try { pinned = entry.Item1.Pinned; } catch (Exception) { pinned = false; }
        if (pinned)
        {
            refused = string.Format("{0} of {1} is pinned - the lines a curtain wall type lays out arrive pinned "
                + "to it, at the type's spacing. Unpin that line first (SET_PIN_STATE), or change the type's own "
                + "layout for every wall of it (SET_CURTAIN_WALL_GRID). Nothing was changed", where, label);
            break;
        }
        targets.Add(target);
        mullionsBefore.Add(mullionsAt(way, entry.Item3));
    }
}

// ---------------------------------------------------------------------------
// THE MOVE, then every line measured where it landed.
// ---------------------------------------------------------------------------
if (refused.Length == 0)
{
    var panelsBefore = -1;
    try { panelsBefore = wall.CurtainGrid.GetPanelIds().Count; } catch (Exception) { panelsBefore = -1; }
    for (var i = 0; i < lines.Count; i++)
    {
        var entry = lines[i];
        var shift = targets[i] - entry.Item3;
        var translation = entry.Item2 == 0 ? flat.Multiply(shift) : XYZ.BasisZ.Multiply(shift);
        try { ElementTransformUtils.MoveElement(doc, entry.Item1.Id, translation); }
        catch (Exception notTaken)
        {
            throw new InvalidOperationException(string.Format("Revit would not move the {0} line at {1} mm of {2} "
                + "- {3}. Nothing this call did is kept", wayWords[entry.Item2],
                sayMm(entry.Item3 * MillimetresPerFoot), label, notTaken.Message));
        }
    }
    try { doc.Regenerate(); }
    catch (Exception notRebuilt)
    {
        throw new InvalidOperationException(string.Format("Revit could not rebuild {0} after the move - {1}. "
            + "Nothing this call did is kept", label, notRebuilt.Message));
    }

    var done = new List<string>();
    for (var i = 0; i < lines.Count; i++)
    {
        var entry = lines[i];
        Curve curve = null;
        try { curve = entry.Item1.IsValidObject ? entry.Item1.FullCurve : null; } catch (Exception) { curve = null; }
        if (curve == null || runs(curve) != entry.Item2)
            throw new InvalidOperationException(string.Format("The {0} line that was at {1} mm on {2} cannot be "
                + "read after the move. Nothing this call did is kept", wayWords[entry.Item2],
                sayMm(entry.Item3 * MillimetresPerFoot), label));
        var now = positionOf(curve, entry.Item2);
        if (Math.Abs(now - targets[i]) * MillimetresPerFoot > PlacedWithinMm)
            throw new InvalidOperationException(string.Format("The {0} line sent from {1} to {2} mm on {3} is at "
                + "{4} mm after Revit rebuilt the wall. Nothing this call did is kept", wayWords[entry.Item2],
                sayMm(entry.Item3 * MillimetresPerFoot), sayMm(targets[i] * MillimetresPerFoot), label,
                sayMm(now * MillimetresPerFoot)));
        var mullionsNow = mullionsAt(entry.Item2, now);
        if (mullionsNow != mullionsBefore[i])
            throw new InvalidOperationException(string.Format("The {0} line moved to {1} mm on {2} carried {3} "
                + "mullion(s) before and has {4} on it now. Nothing this call did is kept", wayWords[entry.Item2],
                sayMm(now * MillimetresPerFoot), label, mullionsBefore[i], mullionsNow));
        moved++;
        done.Add(string.Format("the {0} line from {1} to {2} mm{3}", wayWords[entry.Item2],
            sayMm(entry.Item3 * MillimetresPerFoot), sayMm(now * MillimetresPerFoot),
            mullionsNow == 0 ? "" : string.Format(", with its {0} mullion{1}", mullionsNow, mullionsNow == 1 ? "" : "s")));
    }
    var panelsAfter = -1;
    try { panelsAfter = wall.CurtainGrid.GetPanelIds().Count; } catch (Exception) { panelsAfter = -1; }
    readBack = string.Format("{0}: moved {1}, each measured {2} from the grid's {3} after Revit rebuilt the "
        + "wall. Panels {4} before, {5} now", label, string.Join("; ", done),
        lines.All(l => l.Item2 == 0) ? "along the wall" : lines.All(l => l.Item2 == 1) ? "up the wall" : "along or up",
        lines.All(l => l.Item2 == 0) ? "start" : lines.All(l => l.Item2 == 1) ? "base" : "start or base",
        panelsBefore < 0 ? "?" : panelsBefore.ToString(invariant), panelsAfter < 0 ? "?" : panelsAfter.ToString(invariant));
    findings.Add(string.Format("Moved {0} on {1} - the panels either side follow; this wall only, its type "
        + "untouched", string.Join("; ", done), label));
    if (notMovable.Count > 0)
        findings.Add(string.Format("{0} given and left alone - see notMovable", notMovable.Count));
    findings.Add("Read back from Revit - see readBack");
}
else
{
    findings.Add(refused);
}
