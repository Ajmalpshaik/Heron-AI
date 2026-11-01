// NOT STANDALONE. Assumes `doc`, `origin`, `spacingsAcross`, `spacingsUp`,
// `firstLetter`, `firstNumber`, `gridLines` and `gridNames` are in scope;
// leaves `created`, `namedAs` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Spacings and `gridLines` are
// MILLIMETRES, converted below; `origin` is internal FEET, converted by the
// add-in.
//
// VERSION 2 (2026-11-01) - CREATE_GRID MERGED IN, AND WHAT IT HAD THAT
// VERSION 1 LACKED:
//
//   GRIDS BETWEEN POINTS GIVEN, as well as from bay gaps. `gridLines` is one
//   line or many - "x,y,z; x,y,z | x,y,z; x,y,z" in MILLIMETRES, the shape a
//   point pair is typed in everywhere else - named in order by `gridNames`.
//   A line whose two ends are more than a millimetre apart in HEIGHT is
//   refused, naming the difference: a grid is a vertical plane and its line
//   must be level. Two ends at one point are refused. Within a millimetre,
//   both ends are FORCED ONTO ONE HEIGHT so Revit is handed an exactly level
//   line. One bad line and nothing is made.
//
//   A NAME ALREADY TAKEN IS REFUSED BEFORE ANYTHING IS MADE - the safer of the
//   two cards' rules, as CREATE_LEVELS version 2 did for levels. Every name
//   asked for, from the lettered and numbered sequence or from `gridNames`, is
//   checked against the project's grids and against the other names in the
//   call, IGNORING CAPITALS, and one clash creates NOTHING and says which.
//   Version 1 created each grid first and left a clashing one under Revit's
//   own name in the middle of the setting-out, reported afterwards.
//
//   A GRID REVIT WILL NOT MAKE, OR A NAME IT WILL NOT TAKE, UNDOES THE WHOLE
//   CALL. Version 1 caught both and carried on. Now each is recorded, and then
//   the call THROWS: nothing is kept - whole or not at all (Article 8).
//
//   A DIRECTION WITH NO GAPS GETS NO GRIDS. Every input but a mode's own may
//   now be left out, so a spacing list left out reads as "0" - none that way.
//   Version 1 put one grid at the origin for an empty list, and two on one
//   line for "0". A gap of 0 or less among real gaps would stack two grids on
//   one line, and is refused before anything is made.
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
// names are read FIRST. Since version 2 one clash refuses the whole call
// before anything is created (above); version 1 created the grid anyway under
// Revit's own name and reported it.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
// `gridLines` is text, so its points are converted here too.
const double MillimetresPerFoot = 304.8;
var across = (spacingsAcross ?? new List<double>()).ToList();
var up = (spacingsUp ?? new List<double>()).ToList();

var created = new List<Element>();
var namedAs = new List<string>();
var refused = new List<string>();

var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Grid)))
{
    if (element != null && element.Name != null && !taken.ContainsKey(element.Name))
        taken[element.Name] = element.Name;
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

// A DIRECTION WITH NO GAPS - an empty list, or the lone "0" a left-out list
// reads as - gets no grids (version 2).
Func<IList<double>, bool> noGaps = spacings =>
    spacings.Count == 0 || (spacings.Count == 1 && spacings[0] == 0);

// Positions along one axis, from gaps, in feet. N spacings give N+1 positions.
Func<IList<double>, List<double>> positionsFrom = spacings =>
{
    var result = new List<double>();
    if (noGaps(spacings)) return result;
    result.Add(0.0);
    var running = 0.0;
    foreach (var gap in spacings)
    {
        running += gap / MillimetresPerFoot;
        result.Add(running);
    }
    return result;
};

// "none", blank - what a caller types, or the card reads, for "not asked".
Func<string, bool> notAsked = text =>
{
    var word = (text ?? "").Trim().ToLowerInvariant();
    return word.Length == 0 || word == "none";
};

// ONE POINT IN MILLIMETRES, to feet - three real numbers inside 100 km, the
// bound the add-in's own point reader uses.
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<string, XYZ> pointMm = text =>
{
    var parts = (text ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 3) return null;
    var values = new double[3];
    for (var i = 0; i < 3; i++)
    {
        double value;
        if (!double.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, culture, out value)
            || double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 100000000.0) return null;
        values[i] = value / MillimetresPerFoot;
    }
    return new XYZ(values[0], values[1], values[2]);
};

// THE PLAN: every grid to be made - its two ends and the name asked, or null
// for Revit's own - worked out and checked BEFORE ANYTHING IS MADE.
var plan = new List<Tuple<XYZ, XYZ, string>>();
var byLines = !notAsked(gridLines);
var namesGiven = !notAsked(gridNames);
var acrossPositions = positionsFrom(across);
var upPositions = positionsFrom(up);
var badGap = (!noGaps(across) && across.Any(gap => gap <= 0)) || (!noGaps(up) && up.Any(gap => gap <= 0));

if (byLines && (acrossPositions.Count > 0 || upPositions.Count > 0))
{
    refused.Add("grid lines between points AND bay spacings were both given - ask for one or the other. "
        + "Nothing was created");
}
else if (byLines)
{
    // VERSION 2, CREATE_GRID'S JOB - one grid per pair of points.
    var lineTexts = gridLines.Split('|').Select(each => each.Trim()).Where(each => each.Length > 0).ToList();
    var names = namesGiven
        ? gridNames.Split(',').Select(each => each.Trim()).ToList()
        : new List<string>();
    if (namesGiven && names.Count != lineTexts.Count)
        refused.Add(string.Format("{0} grid line(s) and {1} name(s) were given - one name per line, in "
            + "order, commas between, or leave the names out for Revit's own. Nothing was created",
            lineTexts.Count, names.Count));

    for (var i = 0; i < lineTexts.Count && refused.Count == 0; i++)
    {
        var ends = lineTexts[i].Split(';').Select(each => each.Trim()).Where(each => each.Length > 0).ToList();
        var from = ends.Count == 2 ? pointMm(ends[0]) : null;
        var to = ends.Count == 2 ? pointMm(ends[1]) : null;
        if (from == null || to == null)
            refused.Add(string.Format("line {0}, \"{1}\", is not two points - each grid is two points in "
                + "millimetres, \"0,0,0; 30000,0,0\", with a pipe between grids. Nothing was created",
                i + 1, lineTexts[i]));
        // A GRID IS A VERTICAL PLANE, SO ITS DEFINING LINE IS LEVEL - to a
        // millimetre, because points from a snap or a calculation are rarely
        // exactly level, and refusing over a millionth of a foot is useless.
        else if (Math.Abs(from.Z - to.Z) > 1.0 / MillimetresPerFoot)
            refused.Add(string.Format("line {0}: the two ends are {1:0.#} mm apart in height. A grid is a "
                + "VERTICAL PLANE, so the line defining it has to be level - there is no sloping grid in "
                + "Revit. Nothing was created", i + 1, Math.Abs(from.Z - to.Z) * MillimetresPerFoot));
        else if (from.DistanceTo(to) < 1.0 / MillimetresPerFoot)
            refused.Add(string.Format("line {0}: the two points are the same - there is no direction for "
                + "the grid to run in. Nothing was created", i + 1));
        else
            // BOTH ENDS FORCED ONTO ONE HEIGHT - already within a millimetre;
            // this hands Revit an exactly level line.
            plan.Add(Tuple.Create(from, new XYZ(to.X, to.Y, from.Z),
                namesGiven && names[i].Length > 0 ? names[i] : null));
    }
    if (lineTexts.Count == 0 && refused.Count == 0)
        refused.Add("no grid lines were given - nothing to draw. Nothing was created");
}
else if (namesGiven)
{
    refused.Add("gridNames names the grids drawn between points (gridLines). From bay spacings the names "
        + "run from firstLetter and firstNumber. Nothing was created");
}
else if (acrossPositions.Count == 0 && upPositions.Count == 0)
{
    refused.Add("no spacings given in either direction - nothing to set out");
}
else if (badGap)
{
    refused.Add("every bay spacing must be more than 0 mm - a gap of 0 or less puts two grids on one "
        + "line. Nothing was created");
}
else
{
    var width = acrossPositions.Count > 0 ? acrossPositions[acrossPositions.Count - 1] : 0.0;
    var depth = upPositions.Count > 0 ? upPositions[upPositions.Count - 1] : 0.0;

    // An overhang so the lines run past the outer grids, the way a drawing shows
    // them. A tenth of the span, and never less than one bay would look wrong.
    var overhangAcross = depth > 0 ? depth * 0.1 : width * 0.1;
    var overhangUp = width > 0 ? width * 0.1 : depth * 0.1;
    var at = origin ?? XYZ.Zero;

    // Lettered grids run ACROSS - each one a line in Y at a fixed X.
    var letter = string.IsNullOrEmpty(firstLetter) ? "A" : firstLetter.Trim().ToUpperInvariant();
    foreach (var x in acrossPositions)
    {
        plan.Add(Tuple.Create(new XYZ(at.X + x, at.Y - overhangAcross, at.Z),
            new XYZ(at.X + x, at.Y + depth + overhangAcross, at.Z), letter));
        letter = nextLetters(letter);
    }

    // Numbered grids run UP - each one a line in X at a fixed Y.
    var number = firstNumber;
    foreach (var y in upPositions)
    {
        plan.Add(Tuple.Create(new XYZ(at.X - overhangUp, at.Y + y, at.Z),
            new XYZ(at.X + width + overhangUp, at.Y + y, at.Z), number.ToString(culture)));
        number++;
    }
}

// A NAME ALREADY TAKEN IS REFUSED BEFORE ANYTHING IS MADE (version 2) - by a
// grid in the project or by an earlier name in this call, ignoring capitals.
if (refused.Count == 0)
{
    var clashes = new List<string>();
    var inThisCall = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in plan)
    {
        if (item.Item3 == null) continue;
        string holder;
        if (taken.TryGetValue(item.Item3, out holder))
            clashes.Add(string.Format("'{0}' is already the name of grid '{1}'", item.Item3, holder));
        else if (!inThisCall.Add(item.Item3))
            clashes.Add(string.Format("'{0}' is asked for twice in this call", item.Item3));
    }
    if (clashes.Count > 0)
        refused.Add(string.Format("{0} name(s) already taken, so NO GRID WAS CREATED: {1}. Revit refuses a "
            + "duplicate grid name, ignoring capitals, and creating the grid first and naming it after "
            + "leaves a wrongly-named grid in the setting-out. Choose free names - start the sequence "
            + "somewhere else - and ask again", clashes.Count, string.Join("; ", clashes)));
}

if (refused.Count == 0)
{
    // EVERYTHING IS MADE, OR NOTHING IS KEPT (version 2). A grid Revit will not
    // create, or a name it will not take, is recorded, the rest are still
    // tried so every one is named, and then the call throws.
    var declined = new List<string>();
    foreach (var item in plan)
    {
        var label = item.Item3 ?? string.Format("the line from {0:0}, {1:0} mm",
            item.Item1.X * MillimetresPerFoot, item.Item1.Y * MillimetresPerFoot);
        Grid grid = null;
        try { grid = Grid.Create(doc, Line.CreateBound(item.Item1, item.Item2)); }
        catch (Exception ex)
        {
            declined.Add(string.Format("grid '{0}' could not be created - {1}", label, ex.Message));
            continue;
        }
        if (grid == null)
        {
            declined.Add(string.Format("Revit declined to create grid '{0}'", label));
            continue;
        }
        created.Add(grid);

        if (item.Item3 != null)
        {
            try { grid.Name = item.Item3; }
            catch (Exception ex)
            {
                declined.Add(string.Format("'{0}' was refused by Revit as a name - {1}", item.Item3, ex.Message));
            }
        }
        // READ BACK the name it carries, rather than echoing the one asked.
        namedAs.Add(grid.Name);
        if (item.Item3 != null && !string.Equals(grid.Name, item.Item3, StringComparison.Ordinal))
            declined.Add(string.Format("'{0}' did not take - the grid reads '{1}'", item.Item3, grid.Name));
    }

    if (declined.Count > 0)
        throw new InvalidOperationException(string.Format(
            "{0} grid(s) could not be made as asked: {1}. Nothing this call did is kept - one request, "
            + "one change, whole or not at all", declined.Count, string.Join("; ", declined.Distinct())));

    if (byLines)
        refused.Add(string.Format("{0} grid line(s) drawn between the points given, each one level",
            created.Count));
    else
        refused.Add(string.Format("{0} spacing(s) across gave {1} lettered grid(s), and {2} spacing(s) up "
            + "gave {3} numbered grid(s). Spacings are GAPS - n of them make n+1 grids",
            acrossPositions.Count == 0 ? 0 : across.Count, acrossPositions.Count,
            upPositions.Count == 0 ? 0 : up.Count, upPositions.Count));
}
