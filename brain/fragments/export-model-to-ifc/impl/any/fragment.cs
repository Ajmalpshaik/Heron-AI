// NOT STANDALONE. Assumes `doc`, `exportFolder`, `fileName` and `scopeViewId`
// are in scope, and leaves `exportedPath` and `problem` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) AND DOES NOT OPEN ONE, and here
// that is the answer to a question the original could not settle.
//
// The IFC exporter can write export-setup data back into the document, and
// Revit refuses that outside a transaction - so the version this was
// re-authored from opened its own, and carried a warning that the behaviour
// varies by Revit build with instructions to delete the wrapper if it threw the
// other way. That is an environment-dependent branch nobody could test from one
// machine.
//
// Under Heron's rule the caller already holds a transaction. The exporter's
// requirement is therefore already met, this fragment opens nothing, and the
// branch does not arise. If setup data is written it lands inside the caller's
// single undo entry, which is where a document change belongs.
//
// The read-back is the filesystem, as in every export here: the call returns a
// bool and a bool is a claim about the call, not about the drive.

string exportedPath = "";
string problem = "";

var folder = (exportFolder ?? "").Trim();
var name = (fileName ?? "").Trim();

if (folder.Length == 0) problem = "no export folder was given";
else if (name.Length == 0) problem = "no file name was given";

if (problem.Length == 0)
{
    try
    {
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);
    }
    catch (Exception ex)
    {
        problem = "cannot use folder '" + folder + "': " + ex.Message;
    }
}

if (problem.Length == 0)
{
    var options = new IFCExportOptions();

    // Scoping to a 3D view is the standard coordination export - it carries
    // exactly what that view shows. An invalid id means the whole model, and
    // that is expressed as InvalidElementId rather than a zero so no integer
    // ever stands in for an ElementId (the 2024 width change).
    if (scopeViewId != null && scopeViewId != ElementId.InvalidElementId)
    {
        var scopeView = doc.GetElement(scopeViewId) as View3D;
        if (scopeView == null)
        {
            // Asked to scope to something that is not a 3D view. Refused rather
            // than silently exporting the WHOLE MODEL - a coordination file
            // containing far more than was asked for is the kind of mistake
            // that reaches other trades before anybody notices.
            problem = "scopeViewId is not a 3D view, and exporting the whole "
                    + "model instead was not what was asked for";
        }
        else
        {
            options.FilterViewId = scopeView.Id;
        }
    }

    if (problem.Length == 0)
    {
        var expected = System.IO.Path.Combine(folder, name + ".ifc");
        bool threw = false;
        try
        {
            doc.Export(folder, name, options);
        }
        catch (Exception ex)
        {
            threw = true;
            problem = "the IFC export failed: " + ex.Message;
        }

        // THE READ-BACK.
        if (!threw)
        {
            if (System.IO.File.Exists(expected)) exportedPath = expected;
            else problem = "the export reported no error and produced no file at "
                         + expected;
        }
    }
}
