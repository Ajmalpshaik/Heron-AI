// NOT STANDALONE. Assumes `doc`, `worksetName` and `categories` are in scope;
// leaves `elements`, `scanned` and `findings` behind.
//
// A FILE THAT IS NOT WORKSHARED IS NOT AN EMPTY WORKSET. Both return zero
// elements and they mean opposite things - "nothing is on that workset" versus
// "this file has no worksets at all". Returning a bare zero for the second is
// the wrong-answer-with-confidence failure, so it is refused and named.
//
// A MISSPELLED WORKSET NAME IS REFUSED WITH THE REAL NAMES. "Linked models"
// for "Linked Models" must not read as an empty workset.
//
// USER WORKSETS ONLY. A workshared model also carries family worksets and view
// worksets that Revit maintains and nobody assigns work to; matching across all
// kinds lets an internal name win a question that was about a team's workset.
//
// CATEGORIES ARE HANDED TO ElementMulticategoryFilter AS BuiltInCategory
// VALUES, never as ElementIds built from them. That overload has been there
// since long before 2020 and it keeps 2024's 64-bit ElementId out of this code.

var elements = new List<Element>();
var scanned = 0;
var findings = new List<string>();

if (!doc.IsWorkshared)
{
    findings.Add("This model is not workshared, so it has no worksets at all - that is different "
        + "from the workset being empty, and it is why this is refused rather than answered with 0");
}
else if (string.IsNullOrEmpty(worksetName) || worksetName.Trim().Length == 0)
{
    findings.Add("No workset name was given - name the workset to search");
}
else
{
    var wanted = worksetName.Trim();

    Workset target = null;
    var available = new List<string>();
    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
    {
        available.Add(workset.Name);
        if (target == null && string.Equals(workset.Name, wanted, StringComparison.OrdinalIgnoreCase))
            target = workset;
    }

    if (target == null)
    {
        findings.Add(string.Format("No user workset is called '{0}'. This model has: {1}",
            wanted,
            available.Count == 0 ? "none" : string.Join(", ", available.ToArray())));
    }
    else
    {
        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        foreach (var element in collector)
        {
            scanned++;
            try
            {
                if (element.WorksetId == target.Id) elements.Add(element);
            }
            catch
            {
                // An element that refuses to report a workset is not a match and
                // is not a failure of the sweep. It is still counted as scanned.
            }
        }

        findings.Add(string.Format("{0} of {1} scanned element(s) are on workset '{2}'{3}. "
            + "This says nothing about who has them checked out",
            elements.Count,
            scanned,
            target.Name,
            categories == null || categories.Count == 0
                ? ", across every category"
                : string.Format(", within {0} category/categories", categories.Count)));
    }
}
