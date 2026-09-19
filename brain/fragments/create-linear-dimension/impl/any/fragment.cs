// NOT STANDALONE. Assumes `doc`, `view`, `line` and `references` are in scope;
// leaves `created`, `refused` and `findings`.
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
// THE REFERENCES ARE THE CALLER'S. A face, an edge, a centreline and a grid are
// four different references on one wall and which a drawing wants is a drafting
// decision, not an inference (D-33).

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
else if (line == null)
{
    refused = "No line was given. The line is where the dimension string sits, "
        + "and it has to be drawn somewhere";
}
else if (references == null || references.Count < 2)
{
    refused = string.Format(
        "A dimension measures BETWEEN things and {0} reference(s) were given - "
        + "two is the minimum, and three or more make a chained string",
        references == null ? 0 : references.Count);
}
else
{
    var array = new ReferenceArray();
    var usable = 0;
    foreach (var reference in references)
    {
        if (reference == null) continue;
        array.Append(reference);
        usable++;
    }

    if (usable < 2)
    {
        refused = string.Format(
            "Only {0} of the {1} reference(s) given were usable - two is the "
            + "minimum", usable, references.Count);
    }
    else
    {
        try
        {
            var made = doc.Create.NewDimension(view, line, array);
            if (made == null)
            {
                refused = "Revit returned no dimension. The usual cause is a "
                    + "reference that is not visible in this view - a dimension "
                    + "can only measure what the view shows";
            }
            else
            {
                created = made.Id;
                findings.Add(string.Format(
                    "Dimension drawn on '{0}' across {1} reference(s)",
                    view.Name, usable));
            }
        }
        catch (Exception ex)
        {
            refused = string.Format("Revit refused the dimension: {0}", ex.Message);
        }
    }
}

if (refused != null) findings.Add(refused);
