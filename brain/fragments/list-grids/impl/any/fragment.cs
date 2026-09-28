// NOT STANDALONE. Assumes `doc` and `includeLinks` are in scope; leaves
// `elements`, `findings`, `notLinear`, `linksSearched` and `linkedMatches`
// behind.
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
