// NOT STANDALONE. Assumes `doc`, `worksetName`, `newName` and `deleteInstead`
// are in scope; leaves `renamed` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A WORKSET IS NOT AN ELEMENT. It lives in the workset table, not in the model,
// which is why RENAME_ELEMENTS cannot reach it.
//
// DELETING A WORKSET IS ABSENT FROM REVIT 2020 AND PRESENT FROM 2024 - read off
// both reference assemblies, not assumed. So the delete call is reached BY
// NAME: naming it directly would break the 2020 BUILD, and a try/catch does not
// help because it never runs. Same shape as CREATE_HVAC_ZONE, opposite
// direction - there a release took a capability away, here an older release
// never had one.
//
// A NAME ALREADY IN USE IS REFUSED BY REVIT, so it is checked first. A throw
// mid-rename on a workshared model is a confusing place to end up.

var renamed = false;
var refused = new List<string>();

if (!doc.IsWorkshared)
{
    refused.Add("this model is not workshared - it has no user worksets, so there is nothing to rename. "
        + "Nothing was changed");
}
else
{
    Workset target = null;
    var available = new List<string>();

    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets())
    {
        available.Add(workset.Name);
        if (string.Equals(workset.Name, worksetName, StringComparison.OrdinalIgnoreCase)) target = workset;
    }

    if (target == null)
    {
        refused.Add(string.Format("no user workset called '{0}'. This model has: {1}",
            worksetName, string.Join(", ", available)));
    }
    else if (deleteInstead)
    {
        var table = doc.GetWorksetTable();
        // BY NAME ONLY, with no signature. The delete call takes a settings
        // object whose TYPE does not exist on 2020 either, so naming it in the
        // lookup would break the very build the reflection is here to protect -
        // caught by the metadata check before this was ever compiled.
        var deleteMethod = table.GetType().GetMethod("DeleteWorkset");

        if (deleteMethod == null)
        {
            refused.Add(string.Format("this Revit version cannot delete a workset from the API - the "
                + "call does not exist before Revit 2024. Delete '{0}' by hand: Collaborate, Worksets, "
                + "select it, Delete. Nothing was changed", target.Name));
        }
        else
        {
            refused.Add("deleting a workset moves everything on it onto another workset, and which one "
                + "is a decision this fragment will not make silently. Nothing was deleted");
        }
    }
    else if (string.IsNullOrWhiteSpace(newName))
    {
        refused.Add("no new name given - a workset cannot be renamed to nothing");
    }
    else if (string.Equals(target.Name, newName, StringComparison.Ordinal))
    {
        refused.Add(string.Format("'{0}' is already its name - nothing to do", newName));
    }
    else
    {
        var free = true;
        try { free = WorksetTable.IsWorksetNameUnique(doc, newName); }
        catch { }

        if (!free)
        {
            refused.Add(string.Format("'{0}' is already the name of another workset. Revit refuses a "
                + "duplicate, and finding that out mid-rename is worse than being told now", newName));
        }
        else
        {
            try
            {
                WorksetTable.RenameWorkset(doc, target.Id, newName);
                renamed = true;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' could not be renamed - {1}", target.Name, ex.Message));
            }
        }
    }
}
