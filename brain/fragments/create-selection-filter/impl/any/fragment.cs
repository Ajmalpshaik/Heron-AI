// NOT STANDALONE. Assumes `doc`, `elements` and `filterName` are in scope;
// leaves `filterId`, `saved` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SAVED SET IS NOT THE CURRENT SELECTION. This survives closing the model and
// can be used in a view template a year from now; SET_SELECTION's highlight is
// gone at the next click.
//
// AN EXISTING FILTER OF THAT NAME IS UPDATED, NEVER DUPLICATED. Revit allows no
// second filter of one name, and inventing "Plant Room 2" would leave two sets
// and one wrong drawing.
//
// THE CONTENTS ARE READ BACK. SetElementIds returning is not the set being what
// was asked for - a deleted element handed in is dropped by Revit, and the
// count that matters is what the filter really holds.

var filterId = ElementId.InvalidElementId;
var saved = 0;
var findings = new List<string>();

if (string.IsNullOrEmpty(filterName) || filterName.Trim().Length == 0)
{
    findings.Add("A saved set needs a name - that is the whole point of saving it");
}
else
{
    var name = filterName.Trim();

    var ids = new List<ElementId>();
    foreach (var element in elements)
        if (element != null) ids.Add(element.Id);

    try
    {
        SelectionFilterElement existing = null;
        foreach (var candidate in new FilteredElementCollector(doc)
            .OfClass(typeof(SelectionFilterElement)).Cast<SelectionFilterElement>())
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                existing = candidate;
                break;
            }
        }

        var replaced = existing != null;
        var target = existing ?? SelectionFilterElement.Create(doc, name);
        target.SetElementIds(ids);
        filterId = target.Id;

        // READ IT BACK - Revit drops what it will not keep.
        var held = target.GetElementIds();
        saved = held == null ? 0 : held.Count;

        findings.Add(string.Format("'{0}' {1} and holds {2} element(s){3}. It is applied to NO view - "
            + "deciding where it shows is a separate job",
            name,
            replaced ? "already existed, so its contents were REPLACED" : "was created",
            saved,
            saved == ids.Count ? "" : string.Format(" - {0} were handed in, so Revit kept {1} of them",
                ids.Count, saved)));
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("Saving '{0}' failed: {1}", name, ex.Message));
    }
}
