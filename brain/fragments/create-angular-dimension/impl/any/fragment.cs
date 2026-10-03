// NOT STANDALONE. Assumes `doc`, `view`, `between` and `radiusMm` are in scope;
// leaves `created`, `measured`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE CALL IS AngularDimension.Create, A STATIC ON THE DB TYPE. The
// obvious-sounding NewAngularDimension is on Creation.FamilyItemFactory - the
// FAMILY EDITOR's creation object - and cannot be reached from a project
// document. Checked 2026-09-18 against the reference assemblies at both ends.
//
// THE TWO ARMS ARE TYPED, AND THE ARC IS WORKED OUT FROM THEM. Version 1 took a
// Revit `Arc` and `Reference` objects, which nothing can type (FRAGMENT-ISSUES
// section 6, D-72). Now the arms are named - `grid A`, `plane Skew`,
// `line 123456`, `wall 123456 exterior` - and the arc is struck HERE, centred on
// the point where the two arms meet in plan, because an arc whose centre is not
// that apex reads a different angle with no error at all. Its radius is the
// caller's; it spans from each arm's far end towards the other, so it marks the
// angle between them that is under 180 degrees.
//
// READ BACK: the angle Revit measured is read off the dimension, in degrees.

var created = ElementId.InvalidElementId;
var measured = "";
string refused = null;
var findings = new List<string>();
var problems = new List<string>();

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

// ONE ARM: the reference Revit dimensions to, and the straight line it lies along.
Func<string, Tuple<Reference, Line>> armOf = token =>
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
        var line = grid.Curve as Line;
        if (line == null) { problems.Add("Grid " + rest + " is an arc; an angle is measured between straight arms."); return null; }
        return Tuple.Create(new Reference(grid), line);
    }
    if (word == "plane")
    {
        var plane = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
            .FirstOrDefault(p => string.Equals(p.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (plane == null) { problems.Add("No reference plane is called \"" + rest + "\"."); return null; }
        return Tuple.Create(plane.GetReference(), Line.CreateBound(plane.BubbleEnd, plane.FreeEnd));
    }
    if (word == "line")
    {
        var element = byId(rest, typeof(CurveElement)) as CurveElement;
        var line = element == null ? null : element.GeometryCurve as Line;
        if (element == null) { problems.Add("No model or detail line has the id \"" + rest + "\"."); return null; }
        if (line == null || line.Reference == null) { problems.Add("Line " + rest + " is not a straight line Revit can dimension to."); return null; }
        return Tuple.Create(line.Reference, line);
    }
    if (word == "wall")
    {
        var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var wall = parts.Length == 0 ? null : byId(parts[0], typeof(Wall)) as Wall;
        if (wall == null) { problems.Add("No wall has the id \"" + (parts.Length == 0 ? "" : parts[0]) + "\"."); return null; }
        var side = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
        var location = wall.Location as LocationCurve;
        var line = location == null ? null : location.Curve as Line;
        if (line == null) { problems.Add("Wall " + parts[0] + " is not straight."); return null; }
        if (side != "exterior" && side != "interior")
        {
            problems.Add("Say which face of wall " + parts[0] + ": \"wall " + parts[0] + " exterior\" or \"interior\".");
            return null;
        }
        var faces = HostObjectUtils.GetSideFaces(wall, side == "exterior" ? ShellLayerType.Exterior : ShellLayerType.Interior);
        if (faces == null || faces.Count == 0) { problems.Add("Wall " + parts[0] + " has no " + side + " face Revit can name."); return null; }
        return Tuple.Create(faces[0], line);
    }
    problems.Add("\"" + text + "\" is not an arm this can measure from. Say grid NAME, plane NAME, line ID or "
        + "wall ID exterior|interior - a level is horizontal and makes no angle in plan.");
    return null;
};

var plan = view as ViewPlan;
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
    refused = "The document in front is a family open in the Family Editor; this draws an angle in a project view.";
}
else if (plan == null)
{
    refused = "'" + view.Name + "' is not a plan. The angle is struck in plan, where the arms meet - give a floor, "
        + "ceiling or area plan.";
}
else if (radiusMm <= 0)
{
    refused = "The radius of the arc the dimension sits on has to be more than 0 mm - 1500, say.";
}
else
{
    var tokens = (between ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    if (tokens.Count != 2)
        problems.Add("An angle is measured between TWO arms and " + tokens.Count + " were given.");
    var arms = tokens.Count == 2 ? tokens.Select(armOf).ToList() : new List<Tuple<Reference, Line>>();

    if (problems.Count == 0)
    {
        var z = plan.GenLevel != null ? plan.GenLevel.Elevation : 0.0;
        Func<XYZ, XYZ> flat = p => new XYZ(p.X, p.Y, z);
        var a0 = flat(arms[0].Item2.GetEndPoint(0)); var a1 = flat(arms[0].Item2.GetEndPoint(1));
        var b0 = flat(arms[1].Item2.GetEndPoint(0)); var b1 = flat(arms[1].Item2.GetEndPoint(1));
        var da = a1 - a0; var db = b1 - b0;
        var cross = da.X * db.Y - da.Y * db.X;
        if (Math.Abs(cross) < 1e-9 * da.GetLength() * db.GetLength())
        {
            problems.Add("\"" + tokens[0] + "\" and \"" + tokens[1] + "\" are parallel in plan, so they never meet and "
                + "make no angle - a distance between them is CREATE_LINEAR_DIMENSION's.");
        }
        else
        {
            // THE APEX: where the two arms, run on as infinite lines, cross in plan.
            var t = ((b0.X - a0.X) * db.Y - (b0.Y - a0.Y) * db.X) / cross;
            var apex = new XYZ(a0.X + t * da.X, a0.Y + t * da.Y, z);
            // Each arm points from the apex to whichever of its ends is farther away.
            Func<XYZ, XYZ, double> heading = (p, q) =>
            {
                var far = p.DistanceTo(apex) >= q.DistanceTo(apex) ? p : q;
                return Math.Atan2(far.Y - apex.Y, far.X - apex.X);
            };
            var h1 = heading(a0, a1); var h2 = heading(b0, b1);
            var sweep = h2 - h1;
            while (sweep <= -Math.PI) sweep += 2 * Math.PI;
            while (sweep > Math.PI) sweep -= 2 * Math.PI;
            var start = sweep >= 0 ? h1 : h2;
            var radius = radiusMm / 304.8;
            var arc = Arc.Create(apex, radius, start, start + Math.Abs(sweep), XYZ.BasisX, XYZ.BasisY);

            AngularDimension made = null;
            try
            {
                made = AngularDimension.Create(doc, view, arc, arms.Select(a => a.Item1).ToList(), null);
            }
            catch (Exception ex)
            {
                refused = "Revit refused the angular dimension: " + ex.Message + " The usual cause is an arm the view "
                    + "does not show.";
            }
            if (refused == null && made == null)
                refused = "Revit returned no dimension. The usual cause is an arm the view does not show.";
            if (refused == null)
            {
                doc.Regenerate();
                if (!made.Value.HasValue)
                    throw new InvalidOperationException("The angular dimension reads no angle back. The call failed, "
                        + "and Heron rolls the whole call back.");
                var degrees = made.Value.Value * 180.0 / Math.PI;
                created = made.Id;
                measured = Math.Round(degrees, 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + " degrees";
                findings.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Angular dimension drawn on '{0}' between {1} and {2}, apex at X {3:0.#}, Y {4:0.#} mm, arc radius "
                    + "{5:0.#} mm, reading {6} - read back.",
                    view.Name, tokens[0], tokens[1], apex.X * 304.8, apex.Y * 304.8, radiusMm, measured));
            }
        }
    }
    if (refused == null && problems.Count > 0) refused = "Nothing was drawn. " + string.Join(" ", problems);
}

if (refused != null) findings.Add(refused);
