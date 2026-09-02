// NOT STANDALONE. Assumes `doc` and `worksetNames` are in scope; leaves
// `createdNames`, `alreadyThere` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A WORKSET IS NOT AN ELEMENT. It has no ElementId, so nothing downstream can
// be handed "the worksets that were made" - the names are the answer, and that
// is why this provides strings rather than ids.
//
// AN EXISTING NAME IS REPORTED, NEVER DUPLICATED. Revit refuses a duplicate
// workset name, and a batch that threw on the third of five would leave two
// made, three not, and nothing saying which.
//
// A MODEL THAT IS NOT WORKSHARED IS ANSWERED, NOT FAILED. Enabling worksharing
// means a central file, local copies and a different way of working - a
// decision, never a side effect of asking for a workset.

var createdNames = new List<string>();
var alreadyThere = new List<string>();
var findings = new List<string>();

if (!doc.IsWorkshared)
{
    findings.Add("This model is not workshared, so it has no worksets to add to. Turning worksharing "
        + "on means a central file and local copies for everybody on the job - a decision, and not "
        + "something to do on the way to making a workset");
}
else
{
    var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
        existing.Add(workset.Name);

    foreach (var wanted in worksetNames)
    {
        if (string.IsNullOrEmpty(wanted) || wanted.Trim().Length == 0)
        {
            findings.Add("A blank workset name was asked for and skipped");
            continue;
        }

        var name = wanted.Trim();

        if (existing.Contains(name))
        {
            alreadyThere.Add(name);
            continue;
        }

        try
        {
            var made = Workset.Create(doc, name);
            // READ THE NAME BACK. Revit is the one that decides what a workset
            // ends up called.
            createdNames.Add(made.Name);
            existing.Add(made.Name);
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("'{0}' was refused: {1}", name, ex.Message));
        }
    }

    findings.Add(string.Format("{0} workset(s) created{1}{2}",
        createdNames.Count,
        createdNames.Count == 0 ? "" : ": " + string.Join(", ", createdNames),
        alreadyThere.Count == 0 ? "" : string.Format(". {0} already existed and were left alone: {1}",
            alreadyThere.Count, string.Join(", ", alreadyThere))));
}
