// NOT STANDALONE. Assumes `doc` is in scope; leaves `findings`,
// `worksetCount`, `closedCount` and `workshared` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A CLOSED WORKSET IS INVISIBLE TO EVERY OTHER FRAGMENT HERE, and that is the
// reason this one exists. Elements on a closed workset are not loaded into the
// session at all: FilteredElementCollector does not return them, a view does
// not draw them, a schedule does not count them. So every count this library
// produces is a count of what is OPEN, answered confidently and low, with
// nothing anywhere saying so. This fragment is where it gets said.
//
// USER WORKSETS ONLY. A workshared model also carries families, project
// standards and view worksets, which are Revit's own bookkeeping - listing them
// buries the four or five names a modeller recognises under dozens nobody
// assigns anything to.
//
// NOT WORKSHARED IS A THIRD STATE, not an empty list. A single-user file has no
// worksets to have; reporting "0 worksets" reads as a workshared model somebody
// has emptied, which is a different and much worse situation.
//
// AND NO `elements`. A Workset is not an Element - no element id, cannot be
// selected or isolated. Providing an empty list to look composable would be a
// claim the dependency graph would act on.

var findings = new List<string>();
var worksetCount = 0;
var closedCount = 0;
var workshared = doc.IsWorkshared;

if (!workshared)
{
    findings.Add("this model is not workshared, so it has no worksets - which is "
               + "not the same as having none open");
}
else
{
    var worksets = new List<Workset>();

    foreach (var workset in new FilteredWorksetCollector(doc)
                                .OfKind(WorksetKind.UserWorkset))
    {
        if (workset != null) worksets.Add(workset);
    }

    // By name, because that is how they appear in Revit's own dialog and how
    // somebody reads down the list looking for the one that is shut.
    worksets.Sort(delegate (Workset a, Workset b)
    {
        return string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase);
    });

    foreach (var workset in worksets)
    {
        worksetCount++;

        var isOpen = workset.IsOpen;
        if (!isOpen) closedCount++;

        // An owner means somebody has taken the whole workset for editing. Not
        // a problem in itself, and the reason a change to something on it will
        // be refused.
        var owner = workset.Owner;
        var ownedBy = string.IsNullOrEmpty(owner) ? "" : string.Format(", owned by {0}", owner);

        findings.Add(string.Format("{0}  - {1}{2}",
                                   workset.Name,
                                   isOpen ? "open" : "CLOSED, nothing on it is loaded or counted",
                                   ownedBy));
    }

    if (closedCount > 0)
    {
        findings.Add(string.Format(
            "{0} of {1} worksets are closed. Every count, schedule and clash check in "
            + "this session excludes what is on them, and nothing else will say so",
            closedCount, worksetCount));
    }
}
