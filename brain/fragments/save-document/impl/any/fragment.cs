// NOT STANDALONE. Assumes `doc`, `uidoc`, `documentPath` and `closeWindow` are
// in scope; leaves `saved`, `savedTo`, `closed`, `refused` and `findings`.
//
// VERSION 2, 2026-10-07 - IT SAVES ANOTHER OPEN DOCUMENT, NAMED BY ITS FILE.
// Version 1 saved `doc`, the document Heron runs in, and that can never work:
// Revit's Document.Save and SaveAs "may not be called unless all transactions,
// sub-transactions, and transaction groups that were opened by the API code
// were closed" (RevitAPI.xml, 2020 to 2027), and a PUBLISH run is the write
// path - one TransactionGroup, open in `doc`, for the whole run. So the
// document saved here is any OTHER open one, found by its path, and `doc` is
// refused by name with the reason. The job it exists for: a nested family
// built in its own window - a tyre for a car - saved so its host can load it.
//
// PUBLISH RISK, AND NOT UNDOABLE. A save commits whatever is in that document
// right now, including a change somebody was about to undo.
//
// A LOCAL SAVE IS NOT A SYNC. On a workshared model this writes the local file
// and reaches nobody. That is the confusion worth preventing, so it is said in
// the findings every time.
//
// IT NEVER CHOOSES A FILENAME. Only a document that already has a file is
// found, and it is saved to that file - SaveAs can overwrite another model, and
// inventing a path is exactly how that happens.
//
// CLOSING IS ASKED FOR, AND ONLY A WINDOW THAT IS NOT IN FRONT CAN BE CLOSED.
// Document.Close "cannot close the currently active document. It can only be
// closed via Revit's UI." And no posted command closes one document:
// PostableCommand.Close is "Close Revit" - the whole application - and is
// never used here. So with closeWindow=true, a document behind the one in
// front is closed after its save; the one in front is saved and left open,
// and the reply says the window is the modeller's to close. Nothing unsaved is
// ever closed: Close(false) would discard it.

var saved = false;
string savedTo = null;
var closed = false;
string refused = null;
var findings = new List<string>();

var wanted = (documentPath ?? "").Trim().Trim('"');

// One spelling of a path, so "D:\a\b.rfa" and "d:/a/b.rfa" are the same file.
Func<string, string> canonical = delegate (string p)
{
    if (string.IsNullOrEmpty(p)) return "";
    try { return System.IO.Path.GetFullPath(p).TrimEnd('\\', '/'); }
    catch (Exception) { return p.Trim(); }
};

Document found = null;
var openTitles = new List<string>();

if (doc == null)
{
    refused = "No document was given";
}
else if (wanted.Length == 0)
{
    refused = "No file was named. Name the open document to save by its full file path - "
        + "the .rfa or .rvt it was opened from";
}
else
{
    var key = canonical(wanted);
    foreach (Document open in doc.Application.Documents)
    {
        if (open == null || open.IsLinked) continue;
        openTitles.Add(open.Title);
        var here = "";
        try { here = open.PathName; }
        catch (Exception) { here = ""; }
        if (here.Length > 0
            && string.Equals(canonical(here), key, StringComparison.OrdinalIgnoreCase))
        {
            found = open;
        }
    }

    if (found == null)
    {
        refused = string.Format("No document open in this Revit is saved at '{0}', so there "
            + "is nothing to save. Open: {1}. A document that was never saved has no file "
            + "and cannot be named here", wanted,
            openTitles.Count == 0 ? "(none)" : string.Join(", ", openTitles));
    }
    else if (found.Equals(doc))
    {
        refused = string.Format("'{0}' is the document Heron is working in, and Revit will "
            + "not save a document while Heron's Undo entry is open in it. Switch Heron to "
            + "another open document and name this file again - or save it with Ctrl+S",
            found.Title);
    }
    else if (found.IsReadOnly)
    {
        refused = string.Format("'{0}' is open read-only and cannot be saved", found.Title);
    }
}

if (refused == null)
{
    savedTo = found.PathName;

    if (!found.IsModified)
    {
        // Nothing to write. Not a refusal: the file already holds what is open,
        // which is the state a save exists to reach.
        findings.Add(string.Format("'{0}' had no unsaved changes - the file at {1} already "
            + "holds what is open, so nothing was written", found.Title, savedTo));
    }
    else
    {
        try
        {
            found.Save(new SaveOptions());
            saved = true;
            findings.Add(string.Format(
                "'{0}' saved to {1}. This cannot be undone{2}",
                found.Title, savedTo,
                found.IsWorkshared
                    ? ". IT IS WORKSHARED AND NOTHING WENT TO CENTRAL - this wrote "
                      + "the local file only, and nobody else on the job can see it. "
                      + "SYNC_WITH_CENTRAL is the other job"
                    : ""));
        }
        catch (Exception ex)
        {
            refused = string.Format("Revit refused to save '{0}': {1}", found.Title,
                ex.Message);
        }
    }
}

if (refused == null && closeWindow)
{
    Document inFront = null;
    try
    {
        var active = uidoc == null ? null : uidoc.Application.ActiveUIDocument;
        inFront = active == null ? null : active.Document;
    }
    catch (Exception) { inFront = null; }

    var title = found.Title;
    if (inFront != null && inFront.Equals(found))
    {
        findings.Add(string.Format("'{0}' is the window in front, and Revit lets no tool "
            + "close that one - it is SAVED, so close its window by hand and nothing is lost. "
            + "To have it closed for you, bring another document to the front first "
            + "(ACTIVATE_DOCUMENT) and run this again", title));
    }
    else if (found.IsModified)
    {
        // Only reachable if the save above was skipped or did not take; closing
        // now would throw away work.
        findings.Add(string.Format("'{0}' still has unsaved changes, so it was NOT closed - "
            + "closing would lose them", title));
    }
    else
    {
        try
        {
            closed = found.Close(false);
            if (!closed)
                findings.Add(string.Format("Revit did not close '{0}' - another add-in may "
                    + "have cancelled it. Its file is saved; close it by hand", title));
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Revit would not close '{0}': {1}. Its file is "
                + "saved; close it by hand", title, ex.Message));
        }

        if (closed)
        {
            // READ BACK, not assumed: the document must be gone from what is open.
            var key = canonical(savedTo);
            foreach (Document open in doc.Application.Documents)
            {
                if (open == null || open.IsLinked) continue;
                var here = "";
                try { here = open.PathName; }
                catch (Exception) { here = ""; }
                if (here.Length > 0
                    && string.Equals(canonical(here), key, StringComparison.OrdinalIgnoreCase))
                {
                    closed = false;
                }
            }
            findings.Add(closed
                ? string.Format("'{0}' is closed.", title)
                : string.Format("Revit said '{0}' closed, but it is still open.", title));
        }
    }
}

if (refused != null) findings.Add(refused);
