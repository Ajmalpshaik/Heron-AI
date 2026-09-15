// NOT STANDALONE. Assumes `doc`, `uidoc` and `documentPath` are in scope;
// leaves `activated`, `activeAfter`, `wasAlready`, `problem` and `findings`
// behind.
//
// IT CANNOT RUN INSIDE A TRANSACTION, which is why it is EXECUTE. Activating
// a document while the current one is modifiable is refused.
//
// IT EXISTS BECAUSE THERE WAS NO WAY BACK OUT OF A FAMILY.
// SWITCH_ACTIVE_PROJECT moves between open PROJECTS and refuses from inside a
// family: "Changing the active view is not applicable to inactive documents."
// So opening a family stranded every fragment after it - and one looking for
// loaded families found none, because a family document holds none. Measured
// 2026-09-16: five "opens" failed and five size tables were written into the
// one family that happened to be in front.
//
// activeAfter IS READ BACK FROM REVIT, NEVER ASSUMED FROM THE PATH. Believing
// a switch happened when it did not is exactly how those five tables landed
// in the wrong family, and an argument echoed back would have hidden it.

var findings = new List<string>();
var activated = false;
var activeAfter = "";
var wasAlready = false;
var problem = "";

var path = (documentPath ?? "").Trim();

if (path.Length == 0)
{
    findings.Add("No document path was given.");
}
else if (!System.IO.File.Exists(path))
{
    // Checked separately: Revit's failure for a missing file and its failure
    // for a file it will not open read the same from here, and they need
    // opposite things done about them.
    findings.Add(string.Format("There is no file at \"{0}\".", path));
}
else
{
    var before = "";
    try { before = doc == null ? "" : doc.Title; }
    catch (Exception) { before = ""; }

    // ALREADY IN FRONT IS NOT A FAILURE, and not a reason to reopen either -
    // reopening would be a real action with real risk, taken for nothing.
    var leaf = System.IO.Path.GetFileNameWithoutExtension(path);
    if (before.Length > 0 && string.Equals(before, leaf, StringComparison.OrdinalIgnoreCase))
    {
        wasAlready = true;
        activated = true;
        activeAfter = before;
        findings.Add(string.Format("'{0}' was already the document in front - nothing was "
            + "changed.", before));
    }
    else
    {
        try
        {
            var uiapp = uidoc.Application;
            var opened = uiapp.OpenAndActivateDocument(path);
            if (opened != null && opened.Document != null) activeAfter = opened.Document.Title;
            activated = activeAfter.Length > 0;
        }
        catch (Exception error)
        {
            problem = error.Message;
            findings.Add(string.Format("Revit would not activate \"{0}\": {1}", path,
                error.Message));
        }

        if (activated)
        {
            findings.Add(string.Format("'{0}' is now the document in front (was '{1}').",
                activeAfter, before.Length == 0 ? "<unknown>" : before));
        }
    }
}

findings.Insert(0, string.Format("{0}; in front now: {1}",
    activated ? (wasAlready ? "already in front" : "activated") : "NOT activated",
    activeAfter.Length == 0 ? "<unchanged>" : activeAfter));
