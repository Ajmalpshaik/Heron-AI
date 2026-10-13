// NOT STANDALONE. Assumes `doc`, `paths`, `placement` and `viewName` are in
// scope; leaves `linked`, `linkedIds`, `alreadyLinked`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// LINK FILES INTO THE MODEL - a Revit model, or CAD drawings - one file, a
// list, or every linkable file in a folder. The file's extension decides:
//
//   .rvt                    a Revit link: its type made, then one instance
//   .dwg .dxf               a CAD link, in the model (not this view only)
//   .dgn                    a CAD link, likewise
//
// PLACEMENT: "origin" (the default) puts internal origin on internal origin;
// "shared" places by shared coordinates, which the files must already agree
// on - CHECK_LINK_COORDINATES says whether they do.
//
// A CAD link is placed relative to a plan view's level; `viewName` names it,
// and empty takes the first floor plan.
//
// A FILE ALREADY LINKED IS LEFT OUT AND NAMED - linking it twice puts the same
// building in the model twice. A path that does not exist is named. Nothing
// is imported: a link stays a link, which is what keeps the model small.
//
// READ BACK: every new link is looked up again by its id after a
// regeneration; a Revit link's load status is read off its type.

var linked = 0;
var linkedIds = new List<ElementId>();
var alreadyLinked = new List<string>();
var lines = new List<string>();
var findings = new List<string>();

var files = new List<string>();
foreach (var raw in (paths ?? "").Split(';'))
{
    var path = raw.Trim().Trim('"');
    if (path == "") continue;
    if (System.IO.Directory.Exists(path))
    {
        foreach (var file in System.IO.Directory.GetFiles(path))
        {
            var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".rvt" || ext == ".dwg" || ext == ".dxf" || ext == ".dgn") files.Add(file);
        }
    }
    else files.Add(path);
}

var how = (placement ?? "").Trim().ToLowerInvariant();
var place = how == "shared" ? ImportPlacement.Shared : ImportPlacement.Origin;

// What is linked already, by file name - the name is what a person would
// recognise as "the same drawing" across moved folders.
var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType())
{
    if (!(element is RevitLinkType) && !(element is CADLinkType)) continue;
    try
    {
        var reference = element.GetExternalFileReference();
        if (reference == null) continue;
        var full = ModelPathUtils.ConvertModelPathToUserVisiblePath(reference.GetAbsolutePath());
        present.Add(System.IO.Path.GetFileName(full));
    }
    catch { }
}

View cadView = null;
var wantedView = (viewName ?? "").Trim();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
{
    var v = element as ViewPlan;
    if (v == null || v.IsTemplate) continue;
    if (wantedView == "" ? v.ViewType == ViewType.FloorPlan : string.Equals(v.Name, wantedView, StringComparison.OrdinalIgnoreCase))
    {
        cadView = v;
        break;
    }
}

if (files.Count == 0) lines.Add("No file or folder was given, or the folder holds nothing linkable. NOTHING WAS LINKED.");
if (how != "" && how != "origin" && how != "shared")
{
    lines.Add("\"" + placement + "\" is not origin or shared. NOTHING WAS LINKED.");
    files.Clear();
}

foreach (var file in files)
{
    var name = System.IO.Path.GetFileName(file);
    if (!System.IO.File.Exists(file)) { lines.Add(name + ": no file at " + file + "."); continue; }
    if (present.Contains(name)) { alreadyLinked.Add(name); continue; }
    var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
    try
    {
        ElementId made = ElementId.InvalidElementId;
        if (ext == ".rvt")
        {
            var result = RevitLinkType.Create(doc, ModelPathUtils.ConvertUserVisiblePathToModelPath(file), new RevitLinkOptions(false));
            var instance = RevitLinkInstance.Create(doc, result.ElementId, place);
            made = instance.Id;
        }
        else if (ext == ".dwg" || ext == ".dxf" || ext == ".dgn")
        {
            if (cadView == null) { lines.Add(name + ": no floor plan " + (wantedView == "" ? "" : "named \"" + wantedView + "\" ") + "to place a CAD link from."); continue; }
            ElementId linkId;
            bool ok;
            if (ext == ".dgn")
            {
                var options = new DGNImportOptions();
                options.Placement = place;
                options.ThisViewOnly = false;
                ok = doc.Link(file, options, cadView, out linkId);
            }
            else
            {
                var options = new DWGImportOptions();
                options.Placement = place;
                options.ThisViewOnly = false;
                ok = doc.Link(file, options, cadView, out linkId);
            }
            if (!ok) { lines.Add(name + ": Revit returned no link."); continue; }
            made = linkId;
        }
        else { lines.Add(name + ": not a Revit or CAD file this links."); continue; }

        doc.Regenerate();
        var back = doc.GetElement(made);
        if (back == null) { lines.Add(name + ": the link did not read back."); continue; }
        linked++;
        linkedIds.Add(made);
        present.Add(name);
        var status = "";
        var revitInstance = back as RevitLinkInstance;
        if (revitInstance != null)
        {
            var type = doc.GetElement(revitInstance.GetTypeId()) as RevitLinkType;
            try { status = type == null ? "" : ", " + type.GetLinkedFileStatus(); } catch { }
        }
        lines.Add(name + ": linked, " + (place == ImportPlacement.Shared ? "by shared coordinates" : "origin to origin") + status + ".");
    }
    catch (Exception ex)
    {
        lines.Add(name + ": Revit refused - " + ex.Message);
    }
}

if (alreadyLinked.Count > 0) lines.Add("Already linked, left out: " + string.Join(", ", alreadyLinked) + ".");
var report = string.Join(" | ", lines);
findings.Add(linked + " file(s) linked and read back; " + alreadyLinked.Count + " already linked.");
findings.AddRange(lines);
