// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `notLinear` behind.
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
