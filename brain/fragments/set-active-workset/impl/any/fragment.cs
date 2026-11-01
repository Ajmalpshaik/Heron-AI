// NOT STANDALONE. Assumes `doc` and `worksetName` are in scope, and leaves
// `changed`, `alreadyActive`, `activeBefore`, `activeAfter` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and opens none. Revit's API
// documentation for WorksetTable.SetActiveWorksetId, all eight releases 2020 to
// 2027, lists no ModificationOutsideTransactionException - the active workset
// is not a transactional change, so the call works with or without one. It
// runs inside Heron's, as COPY_ELEMENTS' own call to it does. Whether a
// rolled-back run or Revit's Undo puts the active workset back is NOT KNOWN
// (NEEDS-CHECKING Group KN, KN4) - which is why `activeBefore` names it.
//
// THE ACTIVE WORKSET - Collaborate > Active Workset, and the box on the status
// bar: the workset new elements are placed on. Nothing else changes - no
// element moves and no workset is made, renamed or opened.
//
// NOT WORKSHARED IS REFUSED. Such a model has no worksets and no active one,
// and turning worksharing on is never done as a side effect.
//
// THE NAME IS NEVER GUESSED. It must equal a USER workset's name exactly, case
// and spaces included. Anything else changes nothing and lists every user
// workset; one differing only in case or spacing is named as "did you mean",
// and still refused. A CLOSED workset is refused - nothing placed on it would
// be loaded or drawn in this session.
//
// A WORKSET SOMEBODY ELSE OWNS IS STILL MADE ACTIVE, as Revit's own box allows,
// and the owner is named (Constitution Article 12). Nothing is taken.
//
// READ BACK: the active workset is read from the workset table after the call.
// One that does not read back as asked is set back to what it was and named.
//
// A WorksetId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var changed = false;
var alreadyActive = false;
string activeBefore = "";
string activeAfter = "";
string refused = "";

var asked = worksetName ?? "";

if (!doc.IsWorkshared)
{
    refused = "This model is not workshared, so it has no worksets and no active workset - "
        + "worksharing has never been turned on for it (Collaborate > Worksets). Turning it on "
        + "makes a central file and is a decision of its own, never made here. Nothing was changed.";
}
else
{
    var table = doc.GetWorksetTable();

    // A workset's name as the answer shows it, with its owner when somebody
    // has it - an owner is a normal outcome, not a fault (Article 12).
    Func<WorksetId, string> describe = id =>
    {
        if (ReferenceEquals(id, null) || id == WorksetId.InvalidWorksetId) return "no workset";
        Workset found = null;
        try { found = table.GetWorkset(id); }
        catch (Exception) { found = null; }
        if (found == null) return "a workset this model no longer lists";
        var owner = found.Owner;
        return string.IsNullOrEmpty(owner)
            ? "\"" + found.Name + "\""
            : "\"" + found.Name + "\" (owned by " + owner + ")";
    };

    var before = table.GetActiveWorksetId();
    activeBefore = describe(before);
    activeAfter = activeBefore;

    var users = new List<Workset>();
    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets())
    {
        if (workset != null) users.Add(workset);
    }
    users.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));

    var listed = new List<string>();
    foreach (var workset in users)
    {
        listed.Add("\"" + workset.Name + "\"" + (workset.IsOpen ? "" : " (closed)"));
    }
    var listText = listed.Count == 0 ? "none" : string.Join(", ", listed);

    // EXACTLY - Ordinal, no trimming. A near name is only ever offered back.
    Workset target = null;
    Workset near = null;
    Func<string, string> squeezed = text =>
        string.Join(" ", (text ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    foreach (var workset in users)
    {
        if (string.Equals(workset.Name, asked, StringComparison.Ordinal)) { target = workset; break; }
        if (near == null && string.Equals(squeezed(workset.Name), squeezed(asked), StringComparison.OrdinalIgnoreCase))
            near = workset;
    }

    if (asked.Trim().Length == 0)
    {
        refused = "No workset was named, and the active workset is never guessed. This model's user "
            + "worksets: " + listText + ". Nothing was changed.";
    }
    else if (target == null)
    {
        refused = "No user workset is called \"" + asked + "\" exactly"
            + (near != null ? " - did you mean \"" + near.Name + "\"? Name it exactly as Revit shows it" : "")
            + ". This model's user worksets: " + listText + ". The active workset is still "
            + activeBefore + ". Nothing was changed.";
    }
    else if (before == target.Id)
    {
        alreadyActive = true;
    }
    else if (!target.IsOpen)
    {
        refused = "\"" + target.Name + "\" is closed in this session, so nothing placed on it would be "
            + "loaded or drawn. Open it first (Collaborate > Worksets, select it, Open), then ask again. "
            + "The active workset is still " + activeBefore + ". Nothing was changed.";
    }
    else
    {
        try
        {
            table.SetActiveWorksetId(target.Id);
        }
        catch (Exception ex)
        {
            refused = "Revit refused to make \"" + target.Name + "\" the active workset - " + ex.Message
                + ". Nothing was changed.";
        }

        var after = table.GetActiveWorksetId();
        activeAfter = describe(after);

        if (refused.Length == 0)
        {
            if (after == target.Id)
            {
                changed = true;
            }
            else
            {
                // It did not take. Put back what was active, and say so.
                try { table.SetActiveWorksetId(before); }
                catch (Exception) { }
                activeAfter = describe(table.GetActiveWorksetId());
                refused = "\"" + target.Name + "\" was asked for, but the active workset read back as "
                    + describe(after) + "; it was set back, and is now " + activeAfter + ".";
            }
        }
    }
}
