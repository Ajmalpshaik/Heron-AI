// NOT STANDALONE. Assumes `doc`, `uidoc`, `documentPath`, `saveAsPath`,
// `replaceFile`, `closeWindow`, `saveWhenKept` and `saveOtherWhenKept` are in
// scope; leaves `saveAsked`, `saveAskedTo`, `closeAsked`, `refused` and
// `findings`.
//
// VERSION 4, 2026-11-21 - A CENTRAL MODEL OPENED DIRECTLY IS REFUSED AS ANOTHER
// OPEN DOCUMENT TOO (row 5b-1833). Version 3 asked only the model Heron works
// in whether it is the central model itself; another open document that was one
// was saved, writing the central file, while the reply said "local copy only".
// It is refused now in the run, and again by the add-in before it saves.
//
// VERSION 3, 2026-11-16 - AND THE DOCUMENT HERON WORKS IN, SAVED BY THE ADD-IN
// ONCE ITS UNDO ENTRY HAS CLOSED. OPEN-QUESTIONS Q-69, answered by Ajmal PS that
// day: save after the group closes, from the add-in, only when the modeller
// asks; Heron never picks a name. Revit saves nothing while Heron's group is
// open in a document, so this fragment never saves `doc` itself: it checks the
// request and hands it to `saveWhenKept` - null for the model's own file, or
// the full path the modeller typed for a Save As of a model NEVER saved - and
// the add-in saves after the group: after a Keep, AND AFTER A PREVIEW - the
// change rolled back, the save made and not rolled back (D-171, the owner's
// "okkey" the same day) - but NEVER AFTER A DISCARD, since a save follows only
// a Keep. The reply's `save` block says what the add-in did. A model that has
// a file is saved to it, never under a new name; a file already at a Save As
// path is replaced only with replaceFile=true. Two whole builds were lost
// unsaved (row 5b-513).
//
// VERSION 2, 2026-10-07 - IT SAVES ANOTHER OPEN DOCUMENT, NAMED BY ITS FILE.
// Since 2026-11-16 (row 5b-1824) that save is the ADD-IN'S TOO, after the
// group, through `saveOtherWhenKept` - the document's file, and whether to
// close its window once it is saved. Until then it was done here, inside the
// run, before Revit's Keep or Discard box, so a Discard still wrote that file
// and closed its window, against D-171 point 5. Now this fragment writes and
// closes NOTHING itself: a preview still saves that document, as version 2's
// proof records (D-171 point 6), and a Discard saves nothing.
// Version 1 saved `doc`, the document Heron runs in, and that can never work:
// Revit's Document.Save and SaveAs "may not be called unless all transactions,
// sub-transactions, and transaction groups that were opened by the API code
// were closed" (RevitAPI.xml, 2020 to 2027), and a PUBLISH run is the write
// path - one TransactionGroup, open in `doc`, for the whole run. The job it
// exists for: a nested family built in its own window - a tyre for a car -
// saved so its host can load it.
//
// PUBLISH RISK, AND NOT UNDOABLE. A save commits whatever is in that document
// right now, including a change somebody was about to undo.
//
// A LOCAL SAVE IS NOT A SYNC. On a workshared model this writes the local file
// and reaches nobody. That is the confusion worth preventing, so it is said in
// the findings every time. The CENTRAL model opened directly is refused.
//
// IT NEVER CHOOSES A FILENAME. Another document is found by the file it was
// opened from and saved to it; the document Heron works in is saved to its own
// file, or - never saved - to the path the modeller typed, word for word.
//
// CLOSING IS ASKED FOR, AND ONLY A WINDOW THAT IS NOT IN FRONT CAN BE CLOSED.
// Document.Close "cannot close the currently active document. It can only be
// closed via Revit's UI." And no posted command closes one document:
// PostableCommand.Close is "Close Revit" - the whole application - and is
// never used here. So with closeWindow=true, a document behind the one in
// front is closed by the add-in after its save; the one in front is saved and
// left open, and the reply says the window is the modeller's to close. Nothing
// unsaved is ever closed: Close(false) would discard it. The document Heron
// works in is never closed by Heron.

var saveAsked = false;
string saveAskedTo = null;
var closeAsked = false;
string refused = null;
var findings = new List<string>();

var wanted = (documentPath ?? "").Trim().Trim('"');
var saveAs = (saveAsPath ?? "").Trim().Trim('"').Trim();

// One spelling of a path, so "D:\a\b.rfa" and "d:/a/b.rfa" are the same file.
Func<string, string> canonical = delegate (string p)
{
    if (string.IsNullOrEmpty(p)) return "";
    try { return System.IO.Path.GetFullPath(p).TrimEnd('\\', '/'); }
    catch (Exception) { return p.Trim(); }
};

Document found = null;
var thisOne = false;
var openTitles = new List<string>();

if (doc == null)
{
    refused = "No document was given";
}
else if (wanted.Length == 0 && saveAs.Length == 0)
{
    refused = "No file was named. Name the open document to save by its full file path - "
        + "the .rfa or .rvt it was opened from - or, for the model Heron works in when it "
        + "has never been saved, the full path to save it as (saveAsPath). Heron never "
        + "chooses a name";
}
else if (wanted.Length == 0)
{
    // A Save As names no open document: it is of the one Heron works in.
    found = doc;
    thisOne = true;
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
            + "and cannot be named here - for the model Heron works in, give the path to "
            + "save it as (saveAsPath)", wanted,
            openTitles.Count == 0 ? "(none)" : string.Join(", ", openTitles));
    }
    else if (found.Equals(doc))
    {
        thisOne = true;
    }
    else if (saveAs.Length > 0)
    {
        refused = string.Format("A path to save as is only for the model Heron works in, "
            + "when it has never been saved. '{0}' is another open document and is saved to "
            + "its own file - leave saveAsPath out", found.Title);
    }
    else if (found.IsReadOnly)
    {
        refused = string.Format("'{0}' is open read-only and cannot be saved", found.Title);
    }
    else if (found.IsWorkshared)
    {
        // VERSION 4 (row 5b-1833): A CENTRAL MODEL OPENED DIRECTLY IS REFUSED
        // HERE TOO. Until then only the model Heron works in was asked, so
        // another open document that was the central model itself was handed
        // on to be saved - writing the file everybody on the job works from,
        // under a reply that said "local copy only". The add-in asks again
        // before it saves (CentralItself).
        string otherCentral = null;
        try
        {
            var centralPath = found.GetWorksharingCentralModelPath();
            otherCentral = centralPath == null
                ? "" : ModelPathUtils.ConvertModelPathToUserVisiblePath(centralPath) ?? "";
        }
        catch (Exception) { otherCentral = null; }

        var otherFile = "";
        try { otherFile = found.PathName ?? ""; }
        catch (Exception) { otherFile = ""; }

        if (otherCentral == null)
        {
            refused = string.Format("'{0}' is workshared and Heron could not tell whether it "
                + "is the central model itself, so it will not save it", found.Title);
        }
        else if (otherCentral.Length > 0
                 && string.Equals(canonical(otherCentral), canonical(otherFile),
                                  StringComparison.OrdinalIgnoreCase))
        {
            refused = string.Format("'{0}' is the CENTRAL model itself, opened directly: "
                + "saving it writes the file everybody on the job works from. Open a local "
                + "copy, or Synchronize with Central yourself - Heron never syncs", found.Title);
        }
    }
}

if (refused == null && thisOne)
{
    // VERSION 3: THE DOCUMENT HERON WORKS IN. Checked here, so a refusal is the
    // run's own answer - on a preview too - and asked of the add-in only when
    // nothing stands in the way. The add-in checks again before it saves.
    var title = doc.Title;
    var file = "";
    try { file = doc.PathName ?? ""; }
    catch (Exception) { file = ""; }

    if (doc.IsReadOnly)
    {
        refused = string.Format("'{0}' is open read-only and cannot be saved", title);
    }
    else if (saveAs.Length == 0)
    {
        // A SAVE TO ITS OWN FILE - it was found by that file, so it has one.
        string central = null;
        if (doc.IsWorkshared)
        {
            try
            {
                var centralPath = doc.GetWorksharingCentralModelPath();
                central = centralPath == null
                    ? "" : ModelPathUtils.ConvertModelPathToUserVisiblePath(centralPath) ?? "";
            }
            catch (Exception) { central = null; }
        }

        if (doc.IsWorkshared && central == null)
        {
            refused = string.Format("'{0}' is workshared and Heron could not tell whether it "
                + "is the central model itself, so it will not save it", title);
        }
        else if (doc.IsWorkshared && central.Length > 0
                 && string.Equals(canonical(central), canonical(file),
                                  StringComparison.OrdinalIgnoreCase))
        {
            refused = string.Format("'{0}' is the CENTRAL model itself, opened directly: "
                + "saving it writes the file everybody on the job works from. Open a local "
                + "copy, or Synchronize with Central yourself - Heron never syncs", title);
        }
        else if (!doc.IsModified)
        {
            // Nothing to write. Not a refusal: the file already holds what is open.
            findings.Add(string.Format("'{0}' had no unsaved changes - the file at {1} already "
                + "holds what is open, so nothing will be written", title, file));
        }
        else
        {
            saveWhenKept(null, false);
            saveAsked = true;
            saveAskedTo = file;
            findings.Add(string.Format("'{0}' is the model Heron works in, so Heron saves it to "
                + "{1} once this change's Undo entry has closed - after a Keep, and after a "
                + "preview too, which rolls the change back and still saves; never after a "
                + "Discard. A save cannot be undone{2}", title, file,
                doc.IsWorkshared
                    ? ". IT IS WORKSHARED AND NOTHING GOES TO CENTRAL - the save writes the "
                      + "local copy only. SYNC_WITH_CENTRAL is the other job"
                    : ""));
        }
    }
    else
    {
        // A SAVE AS - only of a model that has NEVER been saved, only to the
        // full path the modeller typed, and never over a file unless they said.
        var family = doc.IsFamilyDocument;
        var extension = family ? ".rfa" : ".rvt";
        var rooted = saveAs.StartsWith(@"\\", StringComparison.Ordinal)
            || (saveAs.Length >= 3 && char.IsLetter(saveAs[0]) && saveAs[1] == ':'
                && (saveAs[2] == '\\' || saveAs[2] == '/'));
        var full = rooted ? canonical(saveAs) : "";
        var folder = "";
        try { folder = full.Length == 0 ? "" : System.IO.Path.GetDirectoryName(full) ?? ""; }
        catch (Exception) { folder = ""; }
        string openThere = null;
        if (full.Length > 0)
        {
            foreach (Document open in doc.Application.Documents)
            {
                if (open == null || open.IsLinked) continue;
                var here = "";
                try { here = open.PathName ?? ""; }
                catch (Exception) { here = ""; }
                if (here.Length > 0 && string.Equals(canonical(here), full,
                                                      StringComparison.OrdinalIgnoreCase))
                    openThere = open.Title;
            }
        }

        if (file.Length > 0)
            refused = string.Format("'{0}' already has a file at {1}, and Heron saves it there "
                + "- never under a new name. Leave saveAsPath out and name that file", title, file);
        else if (doc.IsWorkshared)
            refused = string.Format("'{0}' is workshared, and a Save As makes or moves a "
                + "central model - that is the modeller's to do, with File > Save As", title);
        else if (!rooted)
            refused = string.Format("'{0}' is not a full path - it needs the drive or the "
                + "network share as well as the folder and the name. Heron never chooses a "
                + "folder or a name", saveAs);
        else if (!string.Equals(System.IO.Path.GetExtension(full), extension,
                                StringComparison.OrdinalIgnoreCase))
            refused = string.Format("A {0} is saved as a {1} file, and '{2}' is not one",
                family ? "family" : "project", extension, saveAs);
        else if (folder.Length == 0 || !System.IO.Directory.Exists(folder))
            refused = string.Format("The folder {0} does not exist, and Heron makes no folder",
                folder.Length == 0 ? "of '" + saveAs + "'" : folder);
        else if (openThere != null)
            refused = string.Format("{0} is the file of '{1}', open in this Revit", full, openThere);
        else if (System.IO.File.Exists(full) && !replaceFile)
            refused = string.Format("A file is already at {0}, and Heron replaces a file only "
                + "when the modeller says to replace it (replaceFile=true)", full);
        else
        {
            saveWhenKept(saveAs, replaceFile);
            saveAsked = true;
            saveAskedTo = full;
            findings.Add(string.Format("'{0}' has never been saved, so Heron saves it as {1} - "
                + "the path given, not one Heron chose - once this change's Undo entry has "
                + "closed: after a Keep, and after a preview too, which rolls the change back "
                + "and still saves; never after a Discard. Revit then names it by that file. A "
                + "save cannot be undone{2}", title, full,
                System.IO.File.Exists(full) ? ". It REPLACES the file there, as asked" : ""));
        }
    }

    if (refused == null && closeWindow)
    {
        findings.Add(string.Format("'{0}' is the model Heron works in, and Heron never closes "
            + "that window - close it yourself once the save is done", title));
    }
}
else if (refused == null)
{
    // ANOTHER OPEN DOCUMENT - version 2's job, a nested family's own window.
    // Checked here, so a refusal is the run's own answer; saved, and its
    // window closed, by the add-in once this change's Undo entry has closed
    // (row 5b-1824) - after a Keep or a preview, never after a Discard. The
    // add-in checks every rule again before it saves or closes anything.
    var file = found.PathName;
    var title = found.Title;
    var unsaved = found.IsModified;

    Document inFront = null;
    try
    {
        var active = uidoc == null ? null : uidoc.Application.ActiveUIDocument;
        inFront = active == null ? null : active.Document;
    }
    catch (Exception) { inFront = null; }
    var front = inFront != null && inFront.Equals(found);
    var close = closeWindow && !front;

    if (unsaved || close)
    {
        saveOtherWhenKept(file, close);
        saveAsked = unsaved;
        saveAskedTo = unsaved ? file : null;
        closeAsked = close;
    }

    if (unsaved)
    {
        findings.Add(string.Format("'{0}' is another open document, so Heron saves it to {1}{2} "
            + "once this change's Undo entry has closed - after a Keep, and after a preview "
            + "too, which rolls the change back and still saves; never after a Discard. A save "
            + "cannot be undone{3}", title, file, close ? ", then closes its window," : "",
            found.IsWorkshared
                ? ". IT IS WORKSHARED AND NOTHING GOES TO CENTRAL - the save writes the "
                  + "local file only, and nobody else on the job can see it. "
                  + "SYNC_WITH_CENTRAL is the other job"
                : ""));
    }
    else
    {
        // Nothing to write. Not a refusal: the file already holds what is open,
        // which is the state a save exists to reach.
        findings.Add(string.Format("'{0}' had no unsaved changes - the file at {1} already "
            + "holds what is open, so nothing will be written{2}", title, file,
            close ? ". Heron closes its window once this change's Undo entry has closed - "
                    + "after a Keep or a preview, never after a Discard" : ""));
    }

    if (closeWindow && front)
    {
        findings.Add(string.Format("'{0}' is the window in front, and Revit lets no tool "
            + "close that one - once it is saved, close its window by hand and nothing is "
            + "lost. To have it closed for you, bring another document to the front first "
            + "(ACTIVATE_DOCUMENT) and run this again", title));
    }
}

if (refused != null) findings.Add(refused);
