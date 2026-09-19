// NOT STANDALONE. Assumes `doc`, `view`, `arcReference` and `isDiameter` are in
// scope; leaves `created`, `refused` and `findings`.
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

var created = ElementId.InvalidElementId;
string refused = null;
var findings = new List<string>();

if (view == null)
{
    refused = "No view was given, and a dimension lives on one view";
}
else if (view.IsTemplate)
{
    refused = string.Format("'{0}' is a view TEMPLATE, not a view - a template "
        + "carries settings and holds no annotation", view.Name);
}
else if (arcReference == null)
{
    refused = "No arc reference was given. A radial dimension witnesses an ARC, "
        + "and the reference has to be to one";
}
else
{
    try
    {
        var made = RadialDimension.Create(doc, view, arcReference, isDiameter);
        if (made == null)
        {
            refused = string.Format(
                "Revit returned no {0} dimension. The usual causes are a "
                + "reference that is not an arc, an arc the view does not show, "
                + "or an arc whose origin does not lie within it",
                isDiameter ? "diameter" : "radial");
        }
        else
        {
            created = made.Id;
            findings.Add(string.Format(
                "{0} dimension drawn on '{1}'",
                isDiameter ? "Diameter" : "Radial", view.Name));
        }
    }
    catch (Exception ex)
    {
        refused = string.Format("Revit refused the dimension: {0}", ex.Message);
    }
}

if (refused != null) findings.Add(refused);
