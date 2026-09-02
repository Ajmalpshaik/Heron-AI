// NOT STANDALONE. Assumes `doc`, `exportFolder`, `fileName` and `scopeViewId`
// are in scope, and leaves `exportedPath`, `exporterMissing` and `problem`
// behind.
//
// NO TRANSACTION, and none is needed - unlike the IFC exporter this one writes
// nothing back into the document.
//
// WHY "THE EXPORTER IS NOT INSTALLED" IS ITS OWN OUTCOME.
//
// NWC export comes from a separate Autodesk add-in, so a completely healthy
// Revit can simply not have it. That is not an error in the request, in the
// model, or in this code - and it needs a different response from every other
// failure here:
//
//   the export failed          worth another try, or a different folder
//   the exporter is missing    needs an installer and probably an administrator
//
// Letting the second surface as an exception message buries the one fact that
// decides what somebody does next. So Revit is asked by name, first, and the
// answer gets its own output.

string exportedPath = "";
string problem = "";
bool exporterMissing = false;

var folder = (exportFolder ?? "").Trim();
var name = (fileName ?? "").Trim();

if (!OptionalFunctionalityUtils.IsNavisworksExporterAvailable())
{
    // Asked and answered before anything else is attempted. Nothing is created,
    // and the caller learns the one thing that matters.
    exporterMissing = true;
    problem = "this Revit has no Navisworks exporter installed, so NWC export "
            + "is unavailable here - it is a separate Autodesk add-in";
}
else if (folder.Length == 0) problem = "no export folder was given";
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
    var options = new NavisworksExportOptions();

    if (scopeViewId != null && scopeViewId != ElementId.InvalidElementId)
    {
        var scopeView = doc.GetElement(scopeViewId) as View3D;
        if (scopeView == null)
        {
            // Refused rather than silently exporting the whole model. A
            // coordination file holding far more than was asked for reaches
            // other trades before anybody notices.
            problem = "scopeViewId is not a 3D view, and exporting the whole "
                    + "model instead was not what was asked for";
        }
        else
        {
            options.ExportScope = NavisworksExportScope.View;
            options.ViewId = scopeView.Id;
        }
    }

    if (problem.Length == 0)
    {
        var expected = System.IO.Path.Combine(folder, name + ".nwc");
        bool threw = false;
        try
        {
            doc.Export(folder, name, options);
        }
        catch (Exception ex)
        {
            threw = true;
            problem = "the NWC export failed: " + ex.Message;
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
