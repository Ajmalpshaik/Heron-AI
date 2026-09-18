// NOT STANDALONE. Assumes `doc`, `view`, `arc` and `references` are in scope;
// leaves `created`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE CALL IS AngularDimension.Create, A STATIC ON THE DB TYPE. The
// obvious-sounding NewAngularDimension is on Creation.FamilyItemFactory - the
// FAMILY EDITOR's creation object - and cannot be reached from a project
// document. Checked 2026-09-18 against the reference assemblies at both ends.
//
// THE ARC IS THE WITNESS PATH, NOT THE THING MEASURED, and its centre must be
// the apex of the angle. An arc struck somewhere convenient reads a different
// angle with no error at all.

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
else if (arc == null)
{
    refused = "No arc was given. The arc is where the dimension sits, and its "
        + "centre has to be the apex of the angle being measured";
}
else if (references == null || references.Count < 2)
{
    refused = string.Format(
        "An angle is measured BETWEEN two things and {0} reference(s) were given",
        references == null ? 0 : references.Count);
}
else
{
    var usable = new List<Reference>();
    foreach (var reference in references)
    {
        if (reference != null) usable.Add(reference);
    }

    if (usable.Count < 2)
    {
        refused = string.Format(
            "Only {0} of the {1} reference(s) given were usable - two is the "
            + "minimum", usable.Count, references.Count);
    }
    else
    {
        try
        {
            var made = AngularDimension.Create(doc, view, arc, usable, null);
            if (made == null)
            {
                refused = "Revit returned no dimension. The usual causes are a "
                    + "reference the view does not show, or an arc whose centre "
                    + "is not the apex of the angle";
            }
            else
            {
                created = made.Id;
                findings.Add(string.Format(
                    "Angular dimension drawn on '{0}' between {1} reference(s)",
                    view.Name, usable.Count));
            }
        }
        catch (Exception ex)
        {
            refused = string.Format("Revit refused the angular dimension: {0}",
                ex.Message);
        }
    }
}

if (refused != null) findings.Add(refused);
