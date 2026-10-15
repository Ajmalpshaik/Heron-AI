// NOT STANDALONE. Assumes `doc`, `includeLinks` and `withIntersections` are in
// scope; leaves `elements`, `findings`, `notLinear`, `linksSearched`,
// `linkedMatches`, `intersections` and `spacings` behind.
//
// THE SPACING BETWEEN NEIGHBOURING PARALLEL GRIDS (version 4, 2026-10-14):
// `spacings` gives, for each set of straight grids that run the same way,
// each grid to the next across them and the distance between the two lines,
// square to them, in millimetres - "1 > 2 6000 mm". A bounding-box gap is not
// that distance for a grid on a skew, and a modeller asking "what is the grid
// spacing" means this one (FRAGMENT-ISSUES row 5b-477). Grids count as
// running the same way within half a degree; two on one line - a grid split
// in two - are 0 mm apart and said to be on one line.
//
// WHERE THE GRIDS CROSS, WHEN ASKED (version 3, 2026-10-13, the gap fill):
// `withIntersections` adds every crossing of two of this model's grids, within
// both grids' drawn extents, as "A/1 (x, y)" in millimetres - the points a
// column at every grid intersection is placed on, and a beam along a grid runs
// between. Curved grids cross too, at each point Revit finds.
//
// READ ONLY. Opens no transaction and needs none.
//
// GRID NAMES SORT AS TEXT AND MUST NOT. A building numbered 1 to 12 sorts to
// 1, 10, 11, 12, 2, 3 - and the person reading it is looking for grid 3 in a
// list where it sits sixth. So a name is split into its leading letters and its
// trailing digits, and the digits are compared as a NUMBER. That covers 1..12,
// A..N, and the mixed forms real projects use - "A1", "B10", "CG-2".
//
// THE DIRECTION IS ON EVERY ROW, because the name never says. "Is this on grid
// 5" cannot be answered without knowing whether 5 runs north-south or
// east-west, and both conventions exist in the same building.
//
// A CURVED GRID IS REAL AND HAS NO ONE DIRECTION. Radial grids exist in
// stadiums and curved facades. They are listed, and reported as curved rather
// than given a direction that is only true at one point on them.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link's grids are
// listed the same way and reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements` or
// `notLinear`: those feed the next fragment in a chain, and the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// A LINKED GRID'S DIRECTION IS TURNED INTO THIS MODEL'S TERMS. A link can be
// placed rotated, so "north-south" in the link is not north-south here; each
// direction is put through the first placement's transform before it is
// named, and a file placed at different angles is said to be.

var elements = new List<Element>();
var findings = new List<string>();
var notLinear = new List<ElementId>();

var grids = new List<Grid>();

foreach (var grid in new FilteredElementCollector(doc)
                         .OfClass(typeof(Grid))
                         .Cast<Grid>())
{
    if (grid != null) grids.Add(grid);
}

// Split once, so the comparison below is not re-parsing every name on every
// comparison - a sort does that O(n log n) times.
var letters = new Dictionary<ElementId, string>();
var numbers = new Dictionary<ElementId, long>();

foreach (var grid in grids)
{
    var name = grid.Name ?? "";

    var cut = name.Length;
    while (cut > 0 && char.IsDigit(name[cut - 1])) cut--;

    var head = name.Substring(0, cut);
    var tail = name.Substring(cut);

    letters[grid.Id] = head.ToUpperInvariant();

    long value;
    // No trailing digits at all - a purely alphabetic name like "A". -1 sorts
    // it before "A1", which is the order a drawing lists them in.
    numbers[grid.Id] = long.TryParse(tail, out value) ? value : -1;
}

grids.Sort(delegate (Grid a, Grid b)
{
    var byLetters = string.Compare(letters[a.Id], letters[b.Id], StringComparison.Ordinal);
    if (byLetters != 0) return byLetters;

    return numbers[a.Id].CompareTo(numbers[b.Id]);
});

foreach (var grid in grids)
{
    elements.Add(grid);

    var curve = grid.Curve;
    var line = curve as Line;

    if (line == null)
    {
        // Curved. Listed, because it is a real grid, but given no direction -
        // a curve's direction is only true at one point on it.
        notLinear.Add(grid.Id);
        findings.Add(string.Format("{0}  - curved", grid.Name));
        continue;
    }

    var along = line.Direction;

    // Which way it runs, in the words somebody would use. The threshold is
    // generous on purpose: a grid two degrees off north is a north-south grid
    // to everybody who works on the drawing.
    string direction;
    if (Math.Abs(along.Y) > 0.966) direction = "north-south";
    else if (Math.Abs(along.X) > 0.966) direction = "east-west";
    else direction = "on a skew";

    findings.Add(string.Format("{0}  - runs {1}", grid.Name, direction));
}

// ---- The spacing between neighbouring parallel grids (version 4) -------------

var spacings = "";
{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var sameWay = Math.Cos(0.5 * Math.PI / 180.0);
    var sets = new List<List<Grid>>();
    var setDirection = new List<XYZ>();
    foreach (var grid in grids)
    {
        var line = grid.Curve as Line;
        if (line == null) continue;
        var flat = new XYZ(line.Direction.X, line.Direction.Y, 0);
        if (flat.GetLength() < 1e-9) continue;
        flat = flat.Normalize();
        var placed = false;
        for (var k = 0; k < sets.Count && !placed; k++)
        {
            if (Math.Abs(flat.DotProduct(setDirection[k])) >= sameWay)
            {
                sets[k].Add(grid);
                placed = true;
            }
        }
        if (!placed)
        {
            sets.Add(new List<Grid> { grid });
            setDirection.Add(flat);
        }
    }

    var rowsOut = new List<string>();
    var pairs = 0;
    for (var k = 0; k < sets.Count; k++)
    {
        if (sets[k].Count < 2) continue;
        // Across the set: the plan direction square to its grids. Each grid's
        // place across is its line's start point measured along it.
        var across = new XYZ(-setDirection[k].Y, setDirection[k].X, 0);
        var placedAcross = sets[k]
            .Select(g => new { Grid = g, At = across.DotProduct(((Line)g.Curve).GetEndPoint(0)) })
            .OrderBy(x => x.At)
            .ToList();
        var steps = new List<string>();
        for (var i = 1; i < placedAcross.Count; i++)
        {
            var mm = Math.Round((placedAcross[i].At - placedAcross[i - 1].At) * 304.8);
            steps.Add(placedAcross[i - 1].Grid.Name + " > " + placedAcross[i].Grid.Name + " "
                + (mm < 0.5 ? "0 mm - on one line" : mm.ToString(invariant) + " mm"));
            pairs++;
        }
        string way;
        if (Math.Abs(setDirection[k].Y) > 0.966) way = "north-south";
        else if (Math.Abs(setDirection[k].X) > 0.966) way = "east-west";
        else way = "on a skew";
        rowsOut.Add("grids running " + way + ": " + string.Join(", ", steps.ToArray()));
    }
    spacings = string.Join("  ||  ", rowsOut.ToArray());
    findings.Add(pairs + " spacing(s) between neighbouring parallel grids, square to them, in "
        + "millimetres; a grid with no other running the same way, and a curved one, has none.");
}

// ---- Where the grids cross, when asked ----------------------------------------

var intersections = "";
if (withIntersections)
{
    var crossings = new List<string>();
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    for (var i = 0; i < grids.Count; i++)
        for (var j = i + 1; j < grids.Count; j++)
        {
            // REVIT 2026 changed how two curves are intersected; each release
            // takes its own call, and both give the crossing points.
            var points = new List<XYZ>();
            try
            {
                // Compared flat, at the first grid's height - grids are drawn
                // at their own levels and a height difference is no miss.
                var a = grids[i].Curve;
                var b = grids[j].Curve;
                var lift = Transform.CreateTranslation(new XYZ(0, 0, a.GetEndPoint(0).Z - b.GetEndPoint(0).Z));
#if REVIT2026_OR_GREATER
                var met = a.Intersect(b.CreateTransformed(lift), CurveIntersectResultOption.Detailed);
                if (met.Result != SetComparisonResult.Overlap) continue;
                foreach (var overlap in met.GetOverlaps()) points.Add(overlap.Point);
#else
                IntersectionResultArray hits;
                var met = a.Intersect(b.CreateTransformed(lift), out hits);
                if (met != SetComparisonResult.Overlap || hits == null) continue;
                for (var k = 0; k < hits.Size; k++) points.Add(hits.get_Item(k).XYZPoint);
#endif
            }
            catch (Exception) { continue; }
            foreach (var p in points)
                crossings.Add(grids[i].Name + "/" + grids[j].Name + " (" + Math.Round(p.X * 304.8).ToString(invariant)
                    + ", " + Math.Round(p.Y * 304.8).ToString(invariant) + ")");
        }
    intersections = string.Join(" | ", crossings.Take(500));
    findings.Add(crossings.Count + " grid intersection(s) within the grids' drawn extents"
        + (crossings.Count > 500 ? ", the first 500 listed" : "") + ", in millimetres.");
}

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];

    Transform turn = null;
    var anglesDiffer = false;
    try
    {
        turn = linkPlacements[i][0].GetTotalTransform();
        foreach (var placement in linkPlacements[i])
        {
            if (!placement.GetTotalTransform().BasisX.IsAlmostEqualTo(turn.BasisX)) anglesDiffer = true;
        }
    }
    catch (Exception) { turn = null; }

    var linkGrids = new List<Grid>();
    try
    {
        foreach (var grid in new FilteredElementCollector(linked).OfClass(typeof(Grid)).Cast<Grid>())
            if (grid != null) linkGrids.Add(grid);
    }
    catch (Exception) { }

    // The same letters-then-number order as the host list, computed from the
    // name each time - the host's lookup is keyed by id, and ids are not
    // unique across documents.
    Func<string, string> headOf = name =>
    {
        var cut = (name ?? "").Length;
        while (cut > 0 && char.IsDigit(name[cut - 1])) cut--;
        return (name ?? "").Substring(0, cut).ToUpperInvariant();
    };
    Func<string, long> numberOf = name =>
    {
        var text = name ?? "";
        var cut = text.Length;
        while (cut > 0 && char.IsDigit(text[cut - 1])) cut--;
        long value;
        return long.TryParse(text.Substring(cut), out value) ? value : -1;
    };
    linkGrids.Sort(delegate (Grid a, Grid b)
    {
        var byLetters = string.Compare(headOf(a.Name), headOf(b.Name), StringComparison.Ordinal);
        return byLetters != 0 ? byLetters : numberOf(a.Name).CompareTo(numberOf(b.Name));
    });

    linksSearched++;
    linkedTotal += linkGrids.Count;
    linkedMatches.Add(string.Format("{0}: {1} grid(s){2}", linked.Title, linkGrids.Count,
        anglesDiffer ? " - its placements are turned DIFFERENTLY; directions use the first" : ""));

    foreach (var grid in linkGrids)
    {
        var line = grid.Curve as Line;
        if (line == null)
        {
            linkedMatches.Add(string.Format("  {0}: {1}  - curved", linked.Title, grid.Name));
            continue;
        }

        var along = turn == null ? line.Direction : turn.OfVector(line.Direction);
        string direction;
        if (Math.Abs(along.Y) > 0.966) direction = "north-south";
        else if (Math.Abs(along.X) > 0.966) direction = "east-west";
        else direction = "on a skew";

        linkedMatches.Add(string.Format("  {0}: {1}  - runs {2}", linked.Title, grid.Name, direction));
    }
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} grid(s), NOT in the list above",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
