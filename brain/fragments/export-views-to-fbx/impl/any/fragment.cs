// NOT STANDALONE. Assumes `doc`, `views`, `folder` and `fileName` are in
// scope, and leaves `exported`, `notThreeD` and `refused` behind.
//
// OPENS NO TRANSACTION, AND MUST NOT. An export reads the model; it changes
// nothing. It is still a publish: what leaves is no longer under the model's
// control.
//
// FBX TAKES 3D VIEWS AND NOTHING ELSE.
//
// A plan or a sheet in the set is rejected with an error about the export
// rather than about the view, which reads as the export being broken. They are
// separated here and named instead.
//
// THE FILE CONTAINS WHAT THE VIEW SHOWS.
//
// Hidden categories, a section box, a view filter, a workset switched off -
// all of it travels into the FBX, and nothing in the file says anything is
// missing. Exporting the wrong 3D view is how somebody receives a building
// with no services in it. The view is the thing to check, before rather than
// after.

var exported = new List<string>();
var notThreeD = new List<ElementId>();
string refused = "";

string target = (folder ?? "").Trim();
string name = (fileName ?? "").Trim();

var threeD = new ViewSet();
int count = 0;

foreach (var view in views)
{
    if (view == null) continue;
    if (view is View3D && !view.IsTemplate)
    {
        threeD.Insert(view);
        count++;
    }
    else notThreeD.Add(view.Id);
}

if (target.Length == 0)
    refused = "No folder was given, so there is nowhere to export to.";
else if (name.Length == 0)
    refused = "No file name was given.";
else if (count == 0)
    refused = "None of the views given is a 3D view, and FBX takes nothing else.";
else
{
    bool ok = false;
    try { ok = doc.Export(target, name, threeD, new FBXExportOptions()); }
    catch { ok = false; }

    if (ok)
    {
        // Named per view, because Revit writes one file per view when several
        // are asked for and the caller has to know what to look for.
        foreach (View view in threeD)
        {
            string viewName = "";
            try { viewName = view.Name ?? ""; } catch { }
            exported.Add(viewName);
        }
    }
    else
        refused = "Revit refused the export to '" + target + "'. The usual causes are a folder that " +
                  "does not exist or cannot be written to.";
}
