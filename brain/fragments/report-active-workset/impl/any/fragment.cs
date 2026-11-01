// NOT STANDALONE. Assumes `doc` is in scope, and leaves `activeWorkset`,
// `workshared`, `activeEditable`, `userWorksets` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE ACTIVE WORKSET - the Active Workset box on Revit's status bar, and
// Collaborate > Active Workset: the workset new elements are placed on.
// WorksetTable.GetActiveWorksetId, present and unchanged 2020 to 2027
// (tools/api-surface --members, 2026-11-01).
//
// NOT WORKSHARED IS A THIRD ANSWER. A single-user file has no worksets and no
// active workset; `workshared` is false, `activeWorkset` is empty, and the
// finding SAYS so - "none active" would read as a workshared model with
// nothing chosen.
//
// WHETHER IT IS THE RIGHT ONE IS THE MODELLER'S CALL. The user worksets are
// listed beside it, each open or closed, editable or not, with its owner, and
// the active one marked - so the answer can be read against the choices.
//
// A WorksetId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

string activeWorkset = "";
var workshared = doc.IsWorkshared;
var activeEditable = false;
var userWorksets = new List<string>();
var findings = new List<string>();

if (!workshared)
{
    findings.Add("This model is not workshared, so it has no worksets and no active workset - "
        + "worksharing has never been turned on for it (Collaborate > Worksets). That is not the "
        + "same as a workshared model with nothing active.");
}
else
{
    var table = doc.GetWorksetTable();
    var activeId = table.GetActiveWorksetId();

    Workset active = null;
    if (!ReferenceEquals(activeId, null) && activeId != WorksetId.InvalidWorksetId)
    {
        try { active = table.GetWorkset(activeId); }
        catch (Exception) { active = null; }
    }

    // A workset's state in the words of Collaborate > Worksets: open or
    // closed, editable or not, and who owns it when somebody does.
    Func<Workset, string> state = workset =>
    {
        var owner = workset.Owner;
        return (workset.IsOpen ? "open" : "CLOSED")
            + ", " + (workset.IsEditable ? "editable" : "not editable")
            + (string.IsNullOrEmpty(owner) ? "" : ", owned by " + owner);
    };

    var users = new List<Workset>();
    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets())
    {
        if (workset != null) users.Add(workset);
    }
    users.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));

    foreach (var workset in users)
    {
        var isActive = active != null && workset.Id == active.Id;
        userWorksets.Add("\"" + workset.Name + "\" - " + state(workset) + (isActive ? " - ACTIVE" : ""));
    }

    if (active == null)
    {
        findings.Add("This model is workshared, but Revit names no active workset for it in this session. "
            + "Its " + users.Count + " user workset(s) are listed.");
    }
    else
    {
        activeWorkset = active.Name ?? "";
        activeEditable = active.IsEditable;

        // THE WHOLE ANSWER AS ONE LINE - a reply shows a list's first entries
        // and its count, and this line carries the answer in words.
        findings.Add("The active workset is \"" + activeWorkset + "\" (" + state(active) + ") - new elements "
            + "you place go on it. "
            + (active.Kind == WorksetKind.UserWorkset ? "" : "It is not a user workset. ")
            + "This model has " + users.Count + " user workset(s); whether \"" + activeWorkset + "\" is the "
            + "right one is yours to say, and the Active Workset box on the status bar changes it.");
    }

    findings.AddRange(userWorksets);
}
