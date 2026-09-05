// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `curtainWallsFound`, `panelCount`, `mullionCount`, `emptyCells` and
// `notCurtain` behind.
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

var findings = new List<string>();
var curtainWallsFound = 0;
var panelCount = 0;
var mullionCount = 0;
var emptyCells = 0;
var notCurtain = new List<string>();

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
        }
    }
    catch (Exception ex)
    {
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
}

findings.Insert(0, string.Format("{0} curtain wall(s): {1} panel cell(s), {2} mullion(s), {3} empty "
    + "cell(s). {4} of what was handed in was not a curtain wall",
    curtainWallsFound, panelCount, mullionCount, emptyCells, notCurtain.Count));
