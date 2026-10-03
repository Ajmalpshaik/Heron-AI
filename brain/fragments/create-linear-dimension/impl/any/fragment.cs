// NOT STANDALONE. Assumes `doc`, `view`, `between`, `lineFrom` and `lineTo` are
// in scope; leaves `created`, `measured`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// THE PLAIN DIMENSION. CREATE_DIMENSION is the duct-spacing one and says so in
// its own purpose; every other dimension fragment here is equally specific.
// Measured 2026-09-18, none of them answered "dimension from this to that".
//
// THE CALL IS NewDimension, NOT NewLinearDimension, AND THE DIFFERENCE COSTS A
// COMPILE. The obvious-sounding name belongs to the FAMILY EDITOR's creation
// object, which a project document cannot reach. What `doc.Create` returns
// inherits the base item factory instead, and NewDimension is the one that lives
// there. Checked 2026-09-18 against the reference assemblies: it held on every
// release 2020 to 2027.
//
// WHAT IT MEASURES BETWEEN IS TYPED, AND ONLY WHAT HAS A NAME OR AN ID. Version 1
// took Revit `Reference` objects, which nothing can type (FRAGMENT-ISSUES
// section 6, D-72) - so it could be run by nobody. A face picked with the mouse
// still cannot be said; a grid, a level, a named reference plane, a drawn line
// and a wall's exterior or interior face can, and those are what a drawing
// dimensions to most of the time:
//   grid A | level Level 2 | plane Box Left | line 123456 | wall 123456 exterior
// `|` between them; two is the minimum, three or more make a chained string.
// An id is the number Revit shows (Manage > IDs of Selection) or a unique id.
//
// THE LINE IS WHERE THE STRING SITS, two points in millimetres, and it is the
// caller's: where a dimension goes on a sheet is a drafting decision (D-33).
//
// READ BACK: the dimension's references are counted and every segment's value is
// read, so the reply says what Revit measured rather than what was asked for.

var created = ElementId.InvalidElementId;
var measured = "";
string refused = null;
var findings = new List<string>();

Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

// ONE ELEMENT BY THE NUMBER REVIT SHOWS OR ITS UNIQUE ID. ElementId(long) does not
// exist before 2024 and ElementId(int) is gone by 2026, so the number is matched
// against each candidate's own Id.ToString() rather than constructed.
Func<string, Type, Element> byId = (text, kind) =>
{
    var said = (text ?? "").Trim();
    if (said.Length == 0) return null;
    Element found = null;
    try { found = doc.GetElement(said); } catch (Exception) { found = null; }
    if (found != null) return kind.IsInstanceOfType(found) ? found : null;
    foreach (var e in new FilteredElementCollector(doc).OfClass(kind).WhereElementIsNotElementType())
        if (e.Id.ToString() == said) return e;
    return null;
};

// ONE TYPED REFERENCE, or null with the reason added to `problems`.
var problems = new List<string>();
Func<string, Reference> referenceOf = token =>
{
    var text = (token ?? "").Trim();
    var space = text.IndexOf(' ');
    var word = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
    var rest = space < 0 ? "" : text.Substring(space + 1).Trim();
    if (word == "grid")
    {
        var grid = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
            .FirstOrDefault(g => string.Equals(g.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (grid == null) { problems.Add("No grid is called \"" + rest + "\"."); return null; }
        return new Reference(grid);
    }
    if (word == "level")
    {
        var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
            .FirstOrDefault(l => string.Equals(l.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (level == null) { problems.Add("No level is called \"" + rest + "\"."); return null; }
        return level.GetPlaneReference();
    }
    if (word == "plane")
    {
        var plane = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
            .FirstOrDefault(p => string.Equals(p.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (plane == null) { problems.Add("No reference plane is called \"" + rest + "\"."); return null; }
        return plane.GetReference();
    }
    if (word == "line")
    {
        var line = byId(rest, typeof(CurveElement)) as CurveElement;
        if (line == null) { problems.Add("No model or detail line has the id \"" + rest + "\"."); return null; }
        var reference = line.GeometryCurve == null ? null : line.GeometryCurve.Reference;
        if (reference == null) problems.Add("Line " + rest + " gives Revit no reference to dimension to.");
        return reference;
    }
    if (word == "wall")
    {
        var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var wall = parts.Length == 0 ? null : byId(parts[0], typeof(Wall)) as Wall;
        if (wall == null) { problems.Add("No wall has the id \"" + (parts.Length == 0 ? "" : parts[0]) + "\"."); return null; }
        var side = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
        if (side != "exterior" && side != "interior")
        {
            problems.Add("Say which face of wall " + parts[0] + ": \"wall " + parts[0] + " exterior\" or \"wall "
                + parts[0] + " interior\".");
            return null;
        }
        var faces = HostObjectUtils.GetSideFaces(wall, side == "exterior" ? ShellLayerType.Exterior : ShellLayerType.Interior);
        if (faces == null || faces.Count == 0) { problems.Add("Wall " + parts[0] + " has no " + side + " face Revit can name."); return null; }
        return faces[0];
    }
    problems.Add("\"" + text + "\" is not something this can dimension to. Say grid NAME, level NAME, plane NAME, "
        + "line ID or wall ID exterior|interior.");
    return null;
};

if (view == null)
{
    refused = "No view was given, and a dimension lives on one view.";
}
else if (view.IsTemplate)
{
    refused = string.Format("'{0}' is a view TEMPLATE, not a view - a template "
        + "carries settings and holds no annotation.", view.Name);
}
else if (doc.IsFamilyDocument)
{
    refused = "The document in front is a family open in the Family Editor. A family's dimensions are "
        + "LABEL_FAMILY_DIMENSION's; this draws one in a project view.";
}
else if (lineFrom == null || lineTo == null || lineFrom.DistanceTo(lineTo) < 0.01)
{
    refused = "The line the dimension sits on needs two different points, in millimetres - \"0,-1000,0\" and "
        + "\"6000,-1000,0\".";
}
else
{
    var tokens = (between ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    var array = new ReferenceArray();
    foreach (var token in tokens)
    {
        var reference = referenceOf(token);
        if (reference != null) array.Append(reference);
    }
    if (tokens.Count < 2)
        problems.Add(string.Format("A dimension measures BETWEEN things and {0} was given - two is the minimum, "
            + "and three or more make a chained string.", tokens.Count));

    if (problems.Count > 0)
    {
        refused = "Nothing was drawn. " + string.Join(" ", problems);
    }
    else
    {
        Dimension made = null;
        try
        {
            made = doc.Create.NewDimension(view, Line.CreateBound(lineFrom, lineTo), array);
        }
        catch (Exception ex)
        {
            refused = "Revit refused the dimension: " + ex.Message + " The usual causes are a reference the view "
                + "does not show, or references that are not parallel to each other and across the line.";
        }
        if (refused == null && made == null)
            refused = "Revit returned no dimension. The usual cause is a reference that is not visible in this "
                + "view - a dimension can only measure what the view shows.";
        if (refused == null)
        {
            doc.Regenerate();
            // READ BACK: one value for a single dimension, one per segment for a string.
            var values = new List<string>();
            if (made.NumberOfSegments > 1)
            {
                foreach (DimensionSegment segment in made.Segments)
                    values.Add(segment.Value.HasValue ? mm(segment.Value.Value) + " mm" : "(no value)");
            }
            else if (made.Value.HasValue)
            {
                values.Add(mm(made.Value.Value) + " mm");
            }
            if (made.References.Size != array.Size || values.Count == 0)
                throw new InvalidOperationException("The dimension does not read back as asked: " + made.References.Size
                    + " reference(s) of " + array.Size + ", " + values.Count + " value(s). The call failed, and Heron "
                    + "rolls the whole call back.");
            created = made.Id;
            measured = string.Join(" + ", values);
            findings.Add(string.Format("Dimension drawn on '{0}' across {1} reference(s), reading {2} - read back.",
                view.Name, array.Size, measured));
        }
    }
}

if (refused != null) findings.Add(refused);
