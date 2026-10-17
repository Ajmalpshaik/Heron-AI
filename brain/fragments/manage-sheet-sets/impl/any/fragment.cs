// NOT STANDALONE. Assumes `doc`, `elements`, `setName`, `mode` and `newName`
// are in scope, and leaves `memberCount`, `done`, `notASheet`, `refused`,
// `changed`, `unchanged` and `printRange` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) - for the document side of it.
//
// EVERY ROUTE IS THE PRINT MANAGER, AND THERE IS NO OTHER.
//
// A sheet set is an element that cannot be constructed, renamed through its
// name, or removed with a delete. Creating, renaming and deleting all go
// through the print manager's sheet-set setting. It is an odd shape for a
// model change and it is simply how Revit exposes it.
//
// THE SETTING THROWS UNLESS THE RANGE IS SET FIRST.
//
// Asking for it while the print range is anything but "selected views" raises
// an error whose wording is about the print dialog and mentions no sheet set
// at all. Every mode below sets the range first.
//
// AND THE PRINT MANAGER IS NOT TRANSACTIONAL.
//
// The range stays changed even if the surrounding transaction rolls back: it
// is application state, not document state. Version 1 set it and never put it
// back, so an Undo left Revit's Print dialog on "Selected views/sheets" (found
// 2026-10-14). SINCE VERSION 2 THE RANGE IS READ FIRST AND PUT BACK at the end,
// in a finally, whatever happened in between - and `printRange` says which.
// It is put back on the same print manager it was changed on and Apply() is
// NOT called: Apply() pushes every local print setting to the global ones for
// all documents (RevitAPI.xml), a wider change than version 1 ever made.
// Whether the dialog reads it back is a question for the Revit run
// (NEEDS-CHECKING Group FA), not a thing this file can know.
//
// ADD AND REMOVE (version 2, FRAGMENT-ISSUES 5b-466).
//
// They change what an EXISTING set holds, under its own name, where version 1
// could only delete it and make it again. The set is made current, its members
// read back, the elements given merged in or taken out, and Save() called -
// the route RevitAPI.xml names for a change to a saved set. Save() throws when
// nothing changed, so a call that would change nothing is refused before it is
// asked, and so is one that would leave the set holding nothing.

int memberCount = 0;
bool done = false;
var notASheet = new List<ElementId>();
string refused = "";
// Version 2. How many went in (add) or came out (remove) - 0 for every other
// mode. What was handed in and left as it was: already in the set for add,
// never in it for remove. And what happened to the print range.
int changed = 0;
var unchanged = new List<ElementId>();
string printRange = "";

string wanted = (setName ?? "").Trim();
string operation = (mode ?? "").Trim().ToLowerInvariant();

if (wanted.Length == 0)
    refused = "No set name was given.";
else if (operation != "create" && operation != "rename" && operation != "delete"
         && operation != "add" && operation != "remove")
    refused = "Mode '" + mode + "' is not one of create, rename, delete, add, remove.";

if (refused.Length == 0)
{
    PrintManager printManager = null;
    ViewSheetSetting setting = null;
    // The range as it was, so it can be put back - and whether it was changed
    // at all, so a range that was never touched is not "put back".
    PrintRange rangeBefore = PrintRange.Select;
    bool rangeRead = false;
    bool rangeChanged = false;

    try
    {
        printManager = doc.PrintManager;
        rangeBefore = printManager.PrintRange;
        rangeRead = true;
        // First, or the setting itself throws with a message about the print
        // dialog that names nothing to do with sheet sets.
        if (rangeBefore != PrintRange.Select)
        {
            printManager.PrintRange = PrintRange.Select;
            rangeChanged = true;
        }
        setting = printManager.ViewSheetSetting;
    }
    catch { refused = "The print manager would not open its sheet-set setting."; }

    try
    {
        if (setting != null)
        {
            if (operation == "create")
            {
                var views = new ViewSet();
                foreach (var element in elements)
                {
                    var view = element as View;
                    if (view == null || view.IsTemplate)
                    {
                        if (element != null) notASheet.Add(element.Id);
                        continue;
                    }
                    views.Insert(view);
                    memberCount++;
                }

                if (memberCount == 0)
                    refused = "No views or sheets to put in the set.";
                else
                {
                    try
                    {
                        setting.CurrentViewSheetSet.Views = views;
                        setting.SaveAs(wanted);
                        done = true;
                    }
                    catch { refused = "Revit refused to save a set called '" + wanted + "'."; }
                }
            }
            else
            {
                // Rename, delete, add and remove all work on the set made current
                // first.
                ViewSheetSet existing = null;
                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
                {
                    var candidate = element as ViewSheetSet;
                    if (candidate == null) continue;
                    string name = "";
                    try { name = candidate.Name ?? ""; } catch { }
                    if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = candidate;
                        break;
                    }
                }

                if (existing == null)
                    refused = "No sheet set called '" + wanted + "'.";
                else
                {
                    try { setting.CurrentViewSheetSet = existing; }
                    catch { refused = "Could not make '" + wanted + "' the current set."; }

                    if (refused.Length == 0 && operation == "delete")
                    {
                        try { setting.Delete(); done = true; }
                        catch { refused = "Revit refused to delete the set '" + wanted + "'."; }
                    }
                    else if (refused.Length == 0 && operation == "rename")
                    {
                        string renameTo = (newName ?? "").Trim();
                        if (renameTo.Length == 0) refused = "No new name was given for the rename.";
                        else
                        {
                            try { setting.Rename(renameTo); done = true; }
                            catch { refused = "Revit refused to rename '" + wanted + "'."; }
                        }
                    }
                    else if (refused.Length == 0)
                    {
                        // What the set holds now, read from the set rather than
                        // assumed - and compared by id, never by name.
                        var members = new List<View>();
                        var memberIds = new HashSet<ElementId>();
                        bool membersRead = true;
                        try
                        {
                            foreach (View member in setting.CurrentViewSheetSet.Views)
                                if (member != null && memberIds.Add(member.Id)) members.Add(member);
                        }
                        catch { membersRead = false; }

                        // What was handed in, once each. A template cannot be in a
                        // set, so it goes to notASheet exactly as it does for create.
                        var given = new List<View>();
                        var givenIds = new HashSet<ElementId>();
                        foreach (var element in elements)
                        {
                            var view = element as View;
                            if (view == null || view.IsTemplate)
                            {
                                if (element != null) notASheet.Add(element.Id);
                                continue;
                            }
                            if (givenIds.Add(view.Id)) given.Add(view);
                        }

                        var views = new ViewSet();
                        if (operation == "add")
                        {
                            foreach (var member in members) views.Insert(member);
                            foreach (var view in given)
                            {
                                if (memberIds.Contains(view.Id)) { unchanged.Add(view.Id); continue; }
                                views.Insert(view);
                                changed++;
                            }
                        }
                        else
                        {
                            foreach (var view in given)
                                if (!memberIds.Contains(view.Id)) unchanged.Add(view.Id);
                            foreach (var member in members)
                            {
                                if (givenIds.Contains(member.Id)) { changed++; continue; }
                                views.Insert(member);
                            }
                        }

                        if (!membersRead)
                            refused = "Revit would not hand back what '" + wanted + "' holds, so nothing was changed.";
                        else if (given.Count == 0)
                            refused = "No views or sheets were given to " + operation
                                + (operation == "add" ? " to '" : " from '") + wanted + "'.";
                        else if (changed == 0)
                            refused = operation == "add"
                                ? "Every sheet and view given is already in '" + wanted + "' - nothing was added."
                                : "None of the sheets and views given is in '" + wanted + "' - nothing was taken out.";
                        else if (views.IsEmpty)
                            refused = "Taking these out would leave '" + wanted + "' holding nothing, so nothing was "
                                + "taken out. To remove the whole set, use mode delete.";
                        else
                        {
                            try
                            {
                                setting.CurrentViewSheetSet.Views = views;
                                if (setting.Save()) done = true;
                                else refused = "Revit did not save the change to '" + wanted + "'; nothing was changed.";
                            }
                            catch { refused = "Revit refused to save the change to '" + wanted + "'; nothing was changed."; }

                            // A change Revit did not save is not left waiting on
                            // the current set for the next print to pick up.
                            if (!done)
                            {
                                try { setting.Revert(); } catch { }
                            }
                        }

                        if (!done) changed = 0;
                    }
                }
            }

            // What the set holds NOW, read back rather than assumed from what was
            // sent - a view deleted since leaves a hole in it.
            if (done && operation != "delete")
            {
                try
                {
                    int actual = 0;
                    foreach (View member in setting.CurrentViewSheetSet.Views) if (member != null) actual++;
                    memberCount = actual;
                }
                catch { }
            }
        }
    }
    finally
    {
        // PUT THE PRINT RANGE BACK, whatever happened above - a refusal, a
        // throw, a rollback the caller does afterwards. The words are the
        // Print dialog's own.
        Func<PrintRange, string> rangeWords = range =>
            range == PrintRange.Current ? "Current window"
            : range == PrintRange.Visible ? "Visible portion of current window"
            : range == PrintRange.Select ? "Selected views/sheets"
            : range.ToString();

        if (rangeChanged)
        {
            try
            {
                printManager.PrintRange = rangeBefore;
                printRange = "Print range put back to " + rangeWords(rangeBefore) + ", as it was before.";
            }
            catch
            {
                printRange = "Print range NOT put back: Revit's Print dialog is left on Selected views/sheets "
                    + "and was on " + rangeWords(rangeBefore) + ". Set it back there in File - Print.";
            }
        }
        else if (rangeRead && rangeBefore == PrintRange.Select)
            printRange = "Print range was already Selected views/sheets and is left there.";
    }
}
