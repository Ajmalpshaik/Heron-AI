// NOT STANDALONE. Assumes `doc`, `folder`, `fileName`, `schema`, `scopeViewId`
// and `withQuantities` are in scope; leaves `created`, `refused` and
// `overwrote` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). It does not need one to write a
// file, and it does not open one.
//
// WRITES OUTSIDE THE MODEL, so it cannot be undone in Revit - same class as
// EXPORT_VIEWS_TO_DWG. A file of the same name is OVERWRITTEN silently, so the
// collision is detected and REPORTED. It is not refused: re-exporting over
// yesterday's IFC is the normal way this job is done, and refusing would make
// the fragment useless for its actual use.
//
// THE SCHEMA IS AN IFCVersion VALUE, NOT A STRING. The enumeration has gained
// members across releases - the IFC4 variants are not all on 2020 - so mapping
// a name like "IFC4" onto a member here would compile on every release and
// throw at run time on the old ones. Taking the value makes the caller resolve
// it against the Revit that is actually running.
//
// A WRONG SCHEMA IS THE SILENT FAILURE THIS FRAGMENT EXISTS TO PREVENT. Hand a
// recipient expecting 2x3 an IFC 4 file and it OPENS, shows geometry, and has
// properties missing or renamed. It reads as a bad model rather than a wrong
// format, and the argument that follows is about the modelling.
//
// THE VIEW IS THE SCOPE, AND OMITTING IT IS A REAL CHOICE. With no view the
// whole model goes - every workset, every design option. Most submissions want
// one discipline or one level, which is a 3D view, and that is why the id is
// asked for rather than assumed absent.

string created = null;
string refused = null;
var overwrote = false;

var target = (folder ?? "").Trim();
var stem = (fileName ?? "").Trim();

if (target.Length == 0)
{
    refused = "no folder was given to export into";
}
else if (!System.IO.Directory.Exists(target))
{
    // Not created here, for the same reason EXPORT_VIEWS_TO_DWG does not: a
    // mistyped path is likelier than a missing one, and inventing it puts the
    // submission somewhere nobody looks.
    refused = string.Format("there is no folder at \"{0}\"", target);
}
else if (stem.Length == 0)
{
    refused = "no file name was given";
}
else
{
    // Revit appends the extension itself, so a name carrying one would produce
    // "model.ifc.ifc".
    if (stem.ToLowerInvariant().EndsWith(".ifc")) stem = stem.Substring(0, stem.Length - 4);

    var full = System.IO.Path.Combine(target, stem + ".ifc");
    overwrote = System.IO.File.Exists(full);

    var options = new IFCExportOptions();
    options.FileVersion = schema;

    // Off by default in Revit, and an IFC without them looks complete and
    // carries no volumes or areas - so a quantity take-off downstream has
    // nothing to work from and nobody finds out until it is opened.
    options.ExportBaseQuantities = withQuantities;

    // InvalidElementId is how the API is told "the whole model". Passing a
    // view id limits the export to what that view shows, which is how a
    // discipline-only or level-only IFC is produced.
    if (scopeViewId != null && scopeViewId != ElementId.InvalidElementId)
    {
        var scope = doc.GetElement(scopeViewId) as View3D;

        // A plan view cannot scope an IFC - the export is of a 3D model, and
        // handing it a plan silently exports everything, which is the wrong
        // deliverable with no sign anything went wrong.
        if (scope == null)
        {
            refused = "the scope view must be a 3D view. A plan cannot limit an IFC, and "
                    + "passing one exports the WHOLE model with nothing to show it did";
        }
        else
        {
            options.FilterViewId = scope.Id;
        }
    }

    if (refused == null)
    {
        if (doc.Export(target, stem, options)) created = full;
        else refused = "Revit declined the export - the commonest cause is the IFC "
                     + "exporter add-in not being installed on this machine";
    }
}
