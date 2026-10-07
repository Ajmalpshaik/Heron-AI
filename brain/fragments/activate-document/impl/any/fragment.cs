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
//
// VERSION 2, 2026-10-07 - "ALREADY IN FRONT" IS JUDGED BY THE WINDOW IN FRONT,
// BY ITS FILE. Version 1 compared the title of `doc` - the document Heron was
// pointed at, which need not be in front - with the file name asked for. Aimed
// at a car family behind a tyre family and asked for the car's file, it said
// "already in front" and switched nothing: the very failure this exists to
// prevent (row 5b-359). Two files of one name in two folders matched as well.

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
    // THE WINDOW IN FRONT, asked of Revit - not `doc`, which is only the
    // document this run was aimed at.
    Document inFront = null;
    try
    {
        var active = uidoc == null ? null : uidoc.Application.ActiveUIDocument;
        inFront = active == null ? null : active.Document;
    }
    catch (Exception) { inFront = null; }

    var before = "";
    var frontPath = "";
    try
    {
        before = inFront == null ? "" : inFront.Title;
        frontPath = inFront == null ? "" : inFront.PathName;
    }
    catch (Exception) { before = ""; frontPath = ""; }

    Func<string, string> canonical = delegate (string p)
    {
        if (string.IsNullOrEmpty(p)) return "";
        try { return System.IO.Path.GetFullPath(p).TrimEnd('\\', '/'); }
        catch (Exception) { return p.Trim(); }
    };

    // ALREADY IN FRONT IS NOT A FAILURE, and not a reason to reopen either -
    // reopening would be a real action with real risk, taken for nothing.
    if (frontPath.Length > 0
        && string.Equals(canonical(frontPath), canonical(path), StringComparison.OrdinalIgnoreCase))
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
