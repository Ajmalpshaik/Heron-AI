// NOT STANDALONE. Assumes `doc`, `paths`, `viewName`, `placement` and
// `thisViewOnly` are in scope; leaves `imported`, `importIds`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// CAD DRAWINGS IMPORTED INTO A VIEW - a project or a family. In a family
// that is the tracing underlay a family is drawn over; drawings that share an
// origin line up across the plan and elevation they are imported into.
//
// IMPORTED, NOT LINKED: the drawing becomes part of the file, which a project
// rarely wants and a family underlay always does. LINK_FILES links instead.
//
//   placement     "origin" (the default), "centre" or "shared"
//   thisViewOnly  true keeps the drawing in that one view - what an underlay
//                 wants; false puts it in the model
//
// READ BACK: each import is looked up again by its id.

var imported = 0;
var importIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

View view = null;
var wantedView = (viewName ?? "").Trim();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var v = element as View;
    if (v != null && !v.IsTemplate && string.Equals(v.Name, wantedView, StringComparison.OrdinalIgnoreCase)) { view = v; break; }
}
var how = (placement ?? "").Trim().ToLowerInvariant();
var place = how == "shared" ? ImportPlacement.Shared : (how == "centre" || how == "center") ? ImportPlacement.Centered : ImportPlacement.Origin;

string refusal = null;
if (view == null) refusal = "No view is named \"" + viewName + "\" in this document. NOTHING WAS IMPORTED.";
else if (how != "" && how != "origin" && how != "centre" && how != "center" && how != "shared")
    refusal = "\"" + placement + "\" is not origin, centre or shared. NOTHING WAS IMPORTED.";

if (refusal == null)
{
    foreach (var raw in (paths ?? "").Split(';'))
    {
        var file = raw.Trim().Trim('"');
        if (file == "") continue;
        var name = System.IO.Path.GetFileName(file);
        if (!System.IO.File.Exists(file)) { lines.Add(name + ": no file at " + file + "."); continue; }
        var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
        try
        {
            ElementId id;
            bool ok;
            if (ext == ".dgn")
            {
                var options = new DGNImportOptions();
                options.Placement = place;
                options.ThisViewOnly = thisViewOnly;
                ok = doc.Import(file, options, view, out id);
            }
            else if (ext == ".dwg" || ext == ".dxf")
            {
                var options = new DWGImportOptions();
                options.Placement = place;
                options.ThisViewOnly = thisViewOnly;
                ok = doc.Import(file, options, view, out id);
            }
            else { lines.Add(name + ": not a DWG, DXF or DGN."); continue; }
            if (!ok || doc.GetElement(id) == null) { lines.Add(name + ": Revit returned no import."); continue; }
            imported++;
            importIds.Add(id);
            lines.Add(name + ": imported into \"" + view.Name + "\".");
        }
        catch (Exception ex) { lines.Add(name + ": Revit refused - " + ex.Message); }
    }
    if (imported == 0 && lines.Count == 0) lines.Add("No file was given. NOTHING WAS IMPORTED.");
    findings.Add(imported + " drawing(s) imported into \"" + view.Name + "\"" + (thisViewOnly ? " (this view only)" : "") + ", read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
