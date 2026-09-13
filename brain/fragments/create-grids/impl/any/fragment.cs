// NOT STANDALONE. Assumes `doc`, `origin`, `spacingsAcross`, `spacingsUp`,
// `firstLetter` and `firstNumber` are in scope; leaves `created`, `namedAs` and
// `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Distances are internal FEET.
//
// SPACINGS ARE GAPS, NOT POSITIONS, and both readings look sensible. Four
// spacings make FIVE grids: the first sits at the origin and each spacing steps
// to the next. Read as positions they give one grid too few and every line in
// the wrong place.
//
// THE NAME INCREMENTS THE WAY A DRAWING DOES. Letters run A to Z then AA, AB -
// not A to Z and stop, and not A1.
//
// A NAME REVIT ALREADY HOLDS IS REFUSED BY REVIT, by throwing. So the existing
// names are read FIRST and a clash is reported per grid: the grid is still
// created, carrying Revit's own name, rather than the whole run being lost to
// one collision in the middle of somebody's setting-out.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
spacingsAcross = spacingsAcross.Select(v => v / MillimetresPerFoot).ToList();
spacingsUp = spacingsUp.Select(v => v / MillimetresPerFoot).ToList();

var created = new List<Element>();
var namedAs = new List<string>();
var refused = new List<string>();

var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Grid)))
{
    if (element != null) taken.Add(element.Name);
}

// A to Z, then AA, AB - a drawing's own sequence.
Func<string, string> nextLetters = current =>
{
    var characters = current.ToUpperInvariant().ToCharArray();
    var position = characters.Length - 1;
    while (position >= 0)
    {
        if (characters[position] != 'Z') { characters[position]++; return new string(characters); }
        characters[position] = 'A';
        position--;
    }
    return "A" + new string(characters);
};

// Positions along one axis, from gaps. N spacings give N+1 positions.
Func<IList<double>, List<double>> positionsFrom = spacings =>
{
    var result = new List<double>();
    result.Add(0.0);
    var running = 0.0;
    foreach (var gap in spacings)
    {
        running += gap;
        result.Add(running);
    }
    return result;
};

var acrossPositions = positionsFrom(spacingsAcross);
var upPositions = positionsFrom(spacingsUp);

var width = acrossPositions[acrossPositions.Count - 1];
var depth = upPositions[upPositions.Count - 1];

if (width <= 0 && depth <= 0)
{
    refused.Add("no spacings given in either direction - nothing to set out");
}
else
{
    // An overhang so the lines run past the outer grids, the way a drawing shows
    // them. A tenth of the span, and never less than one bay would look wrong.
    var overhangAcross = depth > 0 ? depth * 0.1 : width * 0.1;
    var overhangUp = width > 0 ? width * 0.1 : depth * 0.1;

    Action<XYZ, XYZ, string> makeGrid = (start, end, wantedName) =>
    {
        Grid grid = null;
        try { grid = Grid.Create(doc, Line.CreateBound(start, end)); }
        catch (Exception ex)
        {
            refused.Add(string.Format("grid '{0}' could not be created - {1}", wantedName, ex.Message));
            return;
        }

        created.Add(grid);

        if (taken.Contains(wantedName))
        {
            namedAs.Add(grid.Name);
            refused.Add(string.Format("'{0}' is already a grid in this project - the new one was left as "
                + "'{1}'. Rename it, or start the sequence somewhere else", wantedName, grid.Name));
            return;
        }

        try
        {
            grid.Name = wantedName;
            taken.Add(wantedName);
            namedAs.Add(wantedName);
        }
        catch
        {
            namedAs.Add(grid.Name);
            refused.Add(string.Format("'{0}' was refused by Revit - the grid stands as '{1}'",
                wantedName, grid.Name));
        }
    };

    // Lettered grids run ACROSS - each one a line in Y at a fixed X.
    var letter = string.IsNullOrEmpty(firstLetter) ? "A" : firstLetter.ToUpperInvariant();
    foreach (var x in acrossPositions)
    {
        var start = new XYZ(origin.X + x, origin.Y - overhangAcross, origin.Z);
        var end = new XYZ(origin.X + x, origin.Y + depth + overhangAcross, origin.Z);
        makeGrid(start, end, letter);
        letter = nextLetters(letter);
    }

    // Numbered grids run UP - each one a line in X at a fixed Y.
    var number = firstNumber;
    foreach (var y in upPositions)
    {
        var start = new XYZ(origin.X - overhangUp, origin.Y + y, origin.Z);
        var end = new XYZ(origin.X + width + overhangUp, origin.Y + y, origin.Z);
        makeGrid(start, end, number.ToString());
        number++;
    }

    refused.Add(string.Format("{0} spacing(s) across gave {1} lettered grid(s), and {2} spacing(s) up "
        + "gave {3} numbered grid(s). Spacings are GAPS - n of them make n+1 grids",
        spacingsAcross.Count, acrossPositions.Count, spacingsUp.Count, upPositions.Count));
}
