// NOT STANDALONE. Assumes `doc`, `elements`, `exportFolder` and
// `fileNamePrefix` are in scope, and leaves `exported`, `notExported` and
// `folderProblem` behind.
//
// NO TRANSACTION, and none is needed - export is document I/O, not a model
// edit. Opening one here would never change anything, and would teach the next
// reader that exports are edits.
//
// WHY THE READ-BACK IS THE FILESYSTEM.
//
// `Document.Export` returns a bool, and a bool is a claim about the CALL, not
// about the drive. A full disk, a path longer than Windows accepts, a folder
// removed while the loop ran, a name that collided with a locked file - none of
// those reliably come back as false. The only honest evidence that a drawing
// was exported is that the file is on disk, so every expected path is checked
// with File.Exists afterwards and only the ones really there are reported.
//
// This is the strongest read-back anywhere in this library, and it is available
// here for free. Where MOVE_ELEMENTS has to compare positions and
// APPLY_VIEW_TEMPLATE has to re-read a property, an export can simply be
// asked of the operating system.
//
// System.IO is deliberately written out in full: it is not among the usings the
// fragment harness provides, and spelling it out keeps the one place this
// fragment reaches outside Revit visible rather than hidden behind a using.

var exported = new List<string>();
var notExported = new List<ElementId>();
string folderProblem = "";

var folder = (exportFolder ?? "").Trim();
var prefix = (fileNamePrefix ?? "").Trim();

if (folder.Length == 0)
{
    folderProblem = "no export folder was given";
    foreach (var element in elements) notExported.Add(element.Id);
}
else
{
    // Creating the folder is the one filesystem change made before any export,
    // and it is reported if it fails rather than left to surface as every view
    // failing for a reason that looks like Revit's fault.
    try
    {
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);
    }
    catch (Exception ex)
    {
        folderProblem = "cannot use folder '" + folder + "': " + ex.Message;
    }

    if (folderProblem.Length > 0)
    {
        foreach (var element in elements) notExported.Add(element.Id);
    }
    else
    {
        var options = new DWGExportOptions();
        var forbidden = new HashSet<char>(
            new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' });

        foreach (var element in elements)
        {
            var view = element as View;

            // A template is not a drawing, and a view Revit will not print is a
            // view it will not export either. Both are reported by id.
            if (view == null || view.IsTemplate || !view.CanBePrinted)
            {
                notExported.Add(element.Id);
                continue;
            }

            // A sheet is named by its number and title, because that is what
            // the receiving office files it under. Everything Windows forbids
            // is replaced rather than dropped, so two views cannot silently
            // collapse onto one filename.
            var sheet = view as ViewSheet;
            var baseName = sheet != null
                ? sheet.SheetNumber + " - " + sheet.Name
                : view.Name;

            var safe = new System.Text.StringBuilder();
            foreach (var c in prefix + baseName)
                safe.Append(forbidden.Contains(c) ? '_' : c);

            var fileName = safe.ToString().Trim();
            if (fileName.Length == 0)
            {
                notExported.Add(view.Id);
                continue;
            }

            var expected = System.IO.Path.Combine(folder, fileName + ".dwg");

            try
            {
                doc.Export(folder, fileName, new List<ElementId> { view.Id }, options);
            }
            catch (Exception)
            {
                // One view's problem. The rest of the set must still export -
                // a batch that abandons twenty drawings because the
                // twenty-first has an awkward name is not useful.
                notExported.Add(view.Id);
                continue;
            }

            // THE READ-BACK. Everything above is the request; this is the
            // only evidence a drawing exists.
            if (System.IO.File.Exists(expected)) exported.Add(expected);
            else notExported.Add(view.Id);
        }
    }
}
