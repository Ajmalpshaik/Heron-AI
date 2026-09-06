// NOT STANDALONE. Assumes `doc`, `elements`, `setName`, `mode` and `newName`
// are in scope, and leaves `memberCount`, `done`, `notASheet` and `refused`
// behind.
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
// is application state, not document state. Worth knowing before assuming an
// undo puts everything back.

int memberCount = 0;
bool done = false;
var notASheet = new List<ElementId>();
string refused = "";

string wanted = (setName ?? "").Trim();
string operation = (mode ?? "").Trim().ToLowerInvariant();

if (wanted.Length == 0)
    refused = "No set name was given.";
else if (operation != "create" && operation != "rename" && operation != "delete")
    refused = "Mode '" + mode + "' is not one of create, rename, delete.";

if (refused.Length == 0)
{
    PrintManager printManager = null;
    ViewSheetSetting setting = null;

    try
    {
        printManager = doc.PrintManager;
        // First, or the setting itself throws with a message about the print
        // dialog that names nothing to do with sheet sets.
        printManager.PrintRange = PrintRange.Select;
        setting = printManager.ViewSheetSetting;
    }
    catch { refused = "The print manager would not open its sheet-set setting."; }

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
            // Rename and delete both work on the set made current first.
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
                else if (refused.Length == 0)
                {
                    string renameTo = (newName ?? "").Trim();
                    if (renameTo.Length == 0) refused = "No new name was given for the rename.";
                    else
                    {
                        try { setting.Rename(renameTo); done = true; }
                        catch { refused = "Revit refused to rename '" + wanted + "'."; }
                    }
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
