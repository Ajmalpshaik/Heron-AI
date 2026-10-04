// NOT STANDALONE. Assumes `doc`, `view`, `arc` and `isDiameter` are in scope;
// leaves `created`, `measured`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// REVIT 2025 AND LATER ONLY, AND THE CONTRACT SAYS SO. RadialDimension.Create is
// absent from 2020-2024 and present from 2025 - counted in all eight reference
// assemblies on 2026-09-18. Before 2025 the only radial call in the API is
// Creation.FamilyItemFactory.NewRadialDimension, which is the FAMILY EDITOR's
// and unreachable from a project. So this is named directly rather than looked
// up at run time: there is no older path to fall back to.
//
// RADIUS AND DIAMETER ARE THE SAME CALL. isDiameter is the only difference, and
// DiameterDimension.Create exists on no release at all.
//
// THE ARC IS TYPED. Version 1 took a Revit `Reference`, which nothing can type
// (FRAGMENT-ISSUES section 6, D-72). Now it is `grid NAME` for an arc grid or
// `line ID` for an arc model or detail line - the things whose arc Revit hands
// a reference for without a mouse. An arc edge of a solid still has to be picked.
//
// READ BACK: the radius or diameter Revit measured is read off the dimension.

var created = ElementId.InvalidElementId;
var measured = "";
string refused = null;
var findings = new List<string>();

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

var text = (arc ?? "").Trim();
var space = text.IndexOf(' ');
var word = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
var rest = space < 0 ? "" : text.Substring(space + 1).Trim();
Reference arcReference = null;
Arc curve = null;
string problem = null;

if (word == "grid")
{
    var grid = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
        .FirstOrDefault(g => string.Equals(g.Name, rest, StringComparison.OrdinalIgnoreCase));
    if (grid == null) problem = "No grid is called \"" + rest + "\".";
    else if ((curve = grid.Curve as Arc) == null) problem = "Grid " + rest + " is straight; a radius needs an arc.";
    else arcReference = new Reference(grid);
}
else if (word == "line")
{
    var element = byId(rest, typeof(CurveElement)) as CurveElement;
    if (element == null) problem = "No model or detail line has the id \"" + rest + "\".";
    else if ((curve = element.GeometryCurve as Arc) == null) problem = "Line " + rest + " is not an arc.";
    else if ((arcReference = curve.Reference) == null) problem = "Arc " + rest + " gives Revit no reference to dimension.";
}
else
{
    problem = "\"" + text + "\" is not an arc this can dimension. Say grid NAME for an arc grid, or line ID for an "
        + "arc model or detail line.";
}

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
    refused = "The document in front is a family open in the Family Editor; LABEL_FAMILY_RADIUS dimensions an arc "
        + "there. This draws one in a project view.";
}
else if (problem != null)
{
    refused = "Nothing was drawn. " + problem;
}
else
{
    RadialDimension made = null;
    try
    {
        made = RadialDimension.Create(doc, view, arcReference, isDiameter);
    }
    catch (Exception ex)
    {
        refused = "Revit refused the dimension: " + ex.Message + " The usual causes are an arc the view does not show, "
            + "or an arc whose origin does not lie within it.";
    }
    if (refused == null && made == null)
        refused = string.Format("Revit returned no {0} dimension. The usual causes are an arc the view does not "
            + "show, or an arc whose origin does not lie within it.", isDiameter ? "diameter" : "radial");
    if (refused == null)
    {
        doc.Regenerate();
        if (!made.Value.HasValue)
            throw new InvalidOperationException("The dimension reads no value back. The call failed, and Heron rolls "
                + "the whole call back.");
        var expected = isDiameter ? curve.Radius * 2 : curve.Radius;
        if (Math.Abs(made.Value.Value - expected) > 0.5 / 304.8)
            throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "The dimension reads {0:0.#} mm where the arc's {1} is {2:0.#} mm. The call failed, and Heron rolls "
                + "the whole call back.", made.Value.Value * 304.8, isDiameter ? "diameter" : "radius", expected * 304.8));
        created = made.Id;
        measured = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1:0.#} mm",
            isDiameter ? "diameter" : "radius", made.Value.Value * 304.8);
        findings.Add(string.Format("{0} dimension drawn on '{1}' on {2}, reading {3} - read back.",
            isDiameter ? "Diameter" : "Radial", view.Name, text, measured));
    }
}

if (refused != null) findings.Add(refused);
