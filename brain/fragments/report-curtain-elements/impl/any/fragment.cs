// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `findings`, `curtainWallsFound`, `panelCount`, `mullionCount`,
// `emptyCells`, `notCurtain`, `differentPanelSizes`, `panelSizes` and
// `sizesNotRead` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A PANEL IS NOT ALWAYS A `Panel`. A curtain grid cell can hold a BASIC WALL
// instead of a curtain panel family - that is how a solid infill or a spandrel
// is normally modelled - and it comes back from GetPanelIds like any other cell.
// Counting only Panel elements under-reports the wall and makes a glazing
// takeoff look short, so both kinds are counted and the wall-as-panel ones are
// named separately: they are the ones a glass order must NOT include.
//
// AN EMPTY CELL IS A REAL STATE. A cell whose panel was deleted returns an id
// that resolves to nothing. It is a hole in the elevation, reported rather than
// skipped - a skipped null and a solid infill look identical in a count.
//
// MULLIONS ARE GROUPED BY TYPE, WHICH IS WHAT GETS ORDERED. Forty mullions is
// not a purchase order; twelve of one profile and twenty-eight of another is.
//
// VERSION 2 (2026-10-29) - PANEL SIZES. Everything version 1 wrote is still
// written, in the same words and in the same order; the size lines are ADDED -
// one at the end of each wall's block, and one for every wall together as the
// LAST line of all. Nothing else moved.
//
//   MEASURED, NEVER WORKED OUT. A cell's size is the Width and Height Revit
//   gives the element IN the cell - CURTAIN_WALL_PANELS_WIDTH and _HEIGHT, read
//   by Revit's own ids, the two read-only values the Properties palette shows
//   for a curtain panel. It is never taken from the grid's spacing (a wall's
//   end panels and any line added by hand are not the spacing), and never from
//   the panel's TYPE, which is not this cell.
//
//   A SIZE REVIT DOES NOT GIVE IS NOT GUESSED (D-52). A cell whose element has
//   no such value, or one that is not a length above zero, is put in NO size:
//   it is named in `sizesNotRead` with the reason, and the wall's size line
//   says how many. ASSUMPTION for the proof (NEEDS-CHECKING Group JR): whether
//   a BASIC WALL in a cell, and a curtain wall DOOR, carry those two values at
//   all. Either way the answer is honest - sized if Revit gives them, named
//   as not sized if it does not.
//
//   ASSUMPTION for the proof: that the two values are the CELL's size, grid
//   line to grid line, and not the glass inside the mullions. The rig in
//   Group JR has no mullions, so its numbers hold either way; row JR6 measures
//   it on a type that has them.
//
//   AN EMPTY CELL IS IN NO SIZE. There is no element there to measure. It is
//   still counted in `emptyCells` as version 1 counts it, and the wall's size
//   line says it is left out.
//
//   A DOOR OR A BASIC WALL IS SIZED LIKE ANY CELL, AND SAID. A size line reads
//   "4 at 1500 x 3000 mm (1 of them a door)", so the cell is neither hidden
//   nor sold as glass. Anything that is not a Curtain Panels element is named
//   the same way, by its category.
//
//   WITHIN THE TOLERANCE. Two cells are one size when their widths AND their
//   heights each differ by no more than `toleranceMm` - 5 mm when the modeller
//   gives none (the card's `absent:`), 0 for exact. Sizes are sorted, smallest
//   width first, and each cell is compared with its group's FIRST member, not
//   the last one to join, so no group spans more than the tolerance: 1500,
//   1504 and 1508 at 5 mm are two sizes, not one chain. A group whose members
//   differ is printed as the range it covers - "1500-1503 x 3000 mm". Width
//   and height are kept as Revit gives them: 1500 x 3000 and 3000 x 1500 are
//   two sizes, because a unit turned on its side is not the same unit.
//
//   PER WALL AND ACROSS THE WALLS. Each wall's block ends with its own sizes;
//   the last line groups every sized cell of every wall handed in together, so
//   a size shared by two walls is ONE size there. `differentPanelSizes` and
//   `panelSizes` are that across-the-walls answer.

var findings = new List<string>();
var curtainWallsFound = 0;
var panelCount = 0;
var mullionCount = 0;
var emptyCells = 0;
var notCurtain = new List<string>();

// VERSION 2 - what the sizes leave behind.
var differentPanelSizes = 0;
var panelSizes = new List<string>();
var sizesNotRead = new List<string>();

const double MillimetresPerFoot = 304.8;

// Floating-point slack on top of the tolerance, so 0 means "the same to a
// thousandth of a millimetre" rather than "the same bit pattern".
const double SameMm = 0.001;

var invariant = System.Globalization.CultureInfo.InvariantCulture;

// Millimetres the way the answer says them: one decimal at most, a point.
Func<double, string> sayMm = value => Math.Round(value, 1).ToString("0.#", invariant);

// A tolerance that is not a length cannot group anything. Said once, and no
// cell is sized - never a silent fall back to another number.
var tolerance = toleranceMm;
var toleranceUsable = !double.IsNaN(tolerance) && !double.IsInfinity(tolerance) && tolerance >= 0;

var panelsCategory = new ElementId(BuiltInCategory.OST_CurtainWallPanels);
var doorsCategory = new ElementId(BuiltInCategory.OST_Doors);

// One length off the element in the cell, in millimetres - or null. Read off
// the element itself, never its type.
Func<Element, BuiltInParameter, double?> lengthOf = (cell, id) =>
{
    Parameter value = null;
    try { value = cell.get_Parameter(id); } catch (Exception) { value = null; }
    if (value == null || !value.HasValue || value.StorageType != StorageType.Double) return null;
    double feet;
    try { feet = value.AsDouble(); } catch (Exception) { return null; }
    if (double.IsNaN(feet) || double.IsInfinity(feet) || feet <= 0) return null;
    return feet * MillimetresPerFoot;
};

// What a cell is, when it is not a plain curtain panel - "" when it is one.
Func<Element, string> kindOf = cell =>
{
    if (cell is Wall) return "a basic wall";
    Category category = null;
    try { category = cell.Category; } catch (Exception) { category = null; }
    if (category == null || category.Id == panelsCategory) return "";
    if (category.Id == doorsCategory) return "a door";
    return "a " + category.Name + " element";
};

// A range the way it is printed: one number when every member rounds alike.
Func<double, double, string> span = (low, high) =>
    sayMm(low) == sayMm(high) ? sayMm(low) : sayMm(low) + "-" + sayMm(high);

// The sizes, grouped within the tolerance - one line per size, in order of
// width then height. Item1 width, Item2 height, Item3 what the cell is.
Func<List<Tuple<double, double, string>>, List<string>> groupSizes = measured =>
{
    var sorted = measured.OrderBy(m => m.Item1).ThenBy(m => m.Item2).ToList();
    var groups = new List<List<Tuple<double, double, string>>>();
    foreach (var cell in sorted)
    {
        List<Tuple<double, double, string>> home = null;
        foreach (var group in groups)
        {
            if (Math.Abs(cell.Item1 - group[0].Item1) <= tolerance + SameMm
                && Math.Abs(cell.Item2 - group[0].Item2) <= tolerance + SameMm)
            {
                home = group;
                break;
            }
        }
        if (home == null) groups.Add(new List<Tuple<double, double, string>> { cell });
        else home.Add(cell);
    }

    var lines = new List<string>();
    foreach (var group in groups)
    {
        var line = string.Format("{0} at {1} x {2} mm", group.Count,
            span(group.Min(m => m.Item1), group.Max(m => m.Item1)),
            span(group.Min(m => m.Item2), group.Max(m => m.Item2)));

        var kinds = new Dictionary<string, int>();
        foreach (var cell in group)
        {
            if (string.IsNullOrEmpty(cell.Item3)) continue;
            if (kinds.ContainsKey(cell.Item3)) kinds[cell.Item3]++;
            else kinds[cell.Item3] = 1;
        }
        if (kinds.Count > 0)
        {
            var parts = new List<string>();
            foreach (var pair in kinds) parts.Add(string.Format("{0} of them {1}", pair.Value, pair.Key));
            line += " (" + string.Join(", ", parts) + ")";
        }
        lines.Add(line);
    }
    return lines;
};

// Every sized cell of every wall, for the across-the-walls line.
var allSized = new List<Tuple<double, double, string>>();
var allUnsized = 0;

foreach (var element in elements)
{
    var wall = element as Wall;
    if (wall == null)
    {
        notCurtain.Add(string.Format("'{0}' is not a wall",
            element == null ? "(nothing)" : element.Name));
        continue;
    }

    CurtainGrid grid = null;
    try { grid = wall.CurtainGrid; }
    catch (Exception) { grid = null; }

    if (grid == null)
    {
        notCurtain.Add(string.Format("'{0}' is a wall but not a CURTAIN wall - its build-up is "
            + "layers, not panels", wall.Name));
        continue;
    }

    curtainWallsFound++;
    findings.Add(string.Format("'{0}' (id {1}):", wall.Name, wall.Id));

    // Panels. Both curtain panels and basic walls used as infill.
    var realPanels = 0;
    var wallPanels = new List<string>();
    var thisEmpty = 0;

    // VERSION 2 - this wall's cells, sized or not.
    var thisSized = new List<Tuple<double, double, string>>();
    var thisUnsized = 0;
    var panelsRead = true;
    try
    {
        foreach (var id in grid.GetPanelIds())
        {
            var cell = doc.GetElement(id);
            if (cell == null) { thisEmpty++; continue; }

            var asWall = cell as Wall;
            if (asWall != null) wallPanels.Add(asWall.Name);
            else realPanels++;
            panelCount++;

            if (!toleranceUsable) continue;
            var kind = kindOf(cell);
            var width = lengthOf(cell, BuiltInParameter.CURTAIN_WALL_PANELS_WIDTH);
            var height = lengthOf(cell, BuiltInParameter.CURTAIN_WALL_PANELS_HEIGHT);
            if (width.HasValue && height.HasValue)
            {
                thisSized.Add(Tuple.Create(width.Value, height.Value, kind));
                continue;
            }

            thisUnsized++;
            string cellName = "";
            try { cellName = cell.Name; } catch (Exception) { cellName = ""; }
            sizesNotRead.Add(string.Format("'{0}' (id {1}): cell {2}{3}{4} - Revit gives it no {5}, "
                + "so it is in no size",
                wall.Name, wall.Id, cell.Id,
                string.IsNullOrEmpty(kind) ? "" : ", " + kind,
                string.IsNullOrEmpty(cellName) ? "" : " '" + cellName + "'",
                !width.HasValue && !height.HasValue ? "panel Width or Height"
                    : (!width.HasValue ? "panel Width" : "panel Height")));
        }
    }
    catch (Exception ex)
    {
        panelsRead = false;
        findings.Add(string.Format("    panels could not be read: {0}", ex.Message));
    }

    emptyCells += thisEmpty;
    findings.Add(string.Format("    {0} curtain panel(s)", realPanels));

    if (wallPanels.Count > 0)
    {
        // Grouped so the order is readable, and named because they are not glass.
        var byName = new Dictionary<string, int>();
        foreach (var name in wallPanels)
        {
            if (byName.ContainsKey(name)) byName[name]++;
            else byName[name] = 1;
        }
        var parts = new List<string>();
        foreach (var pair in byName) parts.Add(string.Format("{0} x {1}", pair.Value, pair.Key));
        findings.Add(string.Format("    {0} cell(s) filled with a BASIC WALL, not a panel - {1}. "
            + "These are not glazing and must not go on a glass order",
            wallPanels.Count, string.Join(", ", parts)));
    }

    if (thisEmpty > 0)
    {
        findings.Add(string.Format("    {0} EMPTY cell(s) - a hole in the elevation where a panel "
            + "was deleted", thisEmpty));
    }

    // Mullions, grouped by type - that is what gets ordered.
    try
    {
        var byType = new Dictionary<string, int>();
        foreach (var id in grid.GetMullionIds())
        {
            var mullion = doc.GetElement(id) as Mullion;
            if (mullion == null) continue;
            mullionCount++;

            var type = doc.GetElement(mullion.GetTypeId()) as ElementType;
            var name = type == null ? "<unknown type>" : type.Name;
            if (byType.ContainsKey(name)) byType[name]++;
            else byType[name] = 1;
        }

        if (byType.Count == 0)
        {
            findings.Add("    no mullions - the grid lines carry none");
        }
        else
        {
            foreach (var pair in byType)
            {
                findings.Add(string.Format("    {0} x mullion '{1}'", pair.Value, pair.Key));
            }
        }
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("    mullions could not be read: {0}", ex.Message));
    }

    try
    {
        findings.Add(string.Format("    grid: {0} vertical line(s), {1} horizontal line(s)",
            grid.GetVGridLineIds().Count, grid.GetUGridLineIds().Count));
    }
    catch (Exception) { }

    // VERSION 2 - this wall's panel sizes, the last line of its block.
    if (!toleranceUsable) continue;
    allSized.AddRange(thisSized);
    allUnsized += thisUnsized;

    var leftOut = new List<string>();
    if (thisUnsized > 0)
        leftOut.Add(string.Format("{0} panel(s) not sized - Revit gave no size for them, each is "
            + "named in sizesNotRead", thisUnsized));
    if (thisEmpty > 0)
        leftOut.Add(string.Format("{0} empty cell(s) in no size - there is no panel there to measure",
            thisEmpty));
    if (!panelsRead)
        leftOut.Add("its panels could not be read, so none was sized");
    var leftOutText = leftOut.Count == 0 ? "" : ". " + string.Join("; ", leftOut);

    if (thisSized.Count == 0)
    {
        findings.Add(string.Format("    panel sizes: none measured{0}",
            leftOutText.Length == 0 ? " - the wall has no panel" : leftOutText));
    }
    else
    {
        var sizes = groupSizes(thisSized);
        findings.Add(string.Format("    panel sizes, width x height within {0} mm: {1} - {2}{3}",
            sayMm(tolerance), sizes.Count, string.Join(", ", sizes), leftOutText));
    }
}

findings.Insert(0, string.Format("{0} curtain wall(s): {1} panel cell(s), {2} mullion(s), {3} empty "
    + "cell(s). {4} of what was handed in was not a curtain wall",
    curtainWallsFound, panelCount, mullionCount, emptyCells, notCurtain.Count));

// VERSION 2 - every wall together, the last line. Said only when a curtain
// wall was read, so a run on no curtain wall answers exactly as version 1 did.
if (curtainWallsFound > 0)
{
    if (!toleranceUsable)
    {
        var why = string.Format("a tolerance of {0} mm is not a size - give 0 mm or more",
            toleranceMm.ToString(invariant));
        sizesNotRead.Add(why + "; no panel was sized");
        findings.Add("Panel sizes were not read: " + why + ".");
    }
    else
    {
        panelSizes = groupSizes(allSized);
        differentPanelSizes = panelSizes.Count;
        var notSized = allUnsized == 0 && emptyCells == 0 ? ""
            : string.Format(" Not in any size: {0} panel(s) Revit gave no size for, {1} empty cell(s).",
                allUnsized, emptyCells);
        findings.Add(allSized.Count == 0
            ? string.Format("Across the {0} curtain wall(s): no panel size measured.{1}",
                curtainWallsFound, notSized)
            : string.Format("Across the {0} curtain wall(s): {1} different panel size(s), width x height "
                + "within {2} mm - {3}.{4}",
                curtainWallsFound, differentPanelSizes, sayMm(tolerance),
                string.Join(", ", panelSizes), notSized));
    }
}
