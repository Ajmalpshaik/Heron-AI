// NOT STANDALONE. Assumes `doc`, `paths`, `placement` and `viewName` are in
// scope; leaves `linked`, `linkedIds`, `alreadyLinked`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// LINK FILES INTO THE MODEL - a Revit model, CAD drawings or a point cloud -
// one file, a list, or every Revit and CAD file in a folder. The file's
// extension decides:
//
//   .rvt                    a Revit link: its type made, then one instance
//   .dwg .dxf               a CAD link, in the model (not this view only)
//   .dgn                    a CAD link, likewise
//   .rcp .rcs               a point cloud (VERSION 2, 2026-11-02): its type
//                           made, then one instance, origin to origin
//
// A POINT CLOUD IS LINKED WHEN ITS FILE IS NAMED. A folder still gathers only
// .rvt, .dwg, .dxf and .dgn files, exactly as version 1 did, so a folder
// linked before links the same files now. A point cloud by shared
// coordinates is not built - the transform from the project's shared
// coordinates is not on any page read for it - and is named, not linked,
// rather than placed origin to origin when shared was asked (row 5b-1441).
//
// AN IFC FILE IS NAMED, NOT LINKED. Revit links an IFC through its IFC
// add-in, which first writes a Revit file from it; RevitLinkType.CreateFromIFC
// only links that file once it exists, and a fragment cannot make it (row
// 5b-1440).
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
// regeneration; a Revit link's load status is read off its type, and a point
// cloud's found status off its type.
//
// RELEASES (tools/api-surface --members, 2026-11-02): PointCloudType.Create(
// Document, string, string), PointCloudType.GetPath and FoundStatus,
// PointCloudInstance.Create(Document, ElementId, Transform) and SubTransaction
// are the same 2020 to 2027. PointCloudFoundStatus gains Invalid in 2025, so
// it is written out by name, never matched value by value.

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
    var cloudType = element as PointCloudType;
    if (cloudType != null)
    {
        try
        {
            var cloudPath = cloudType.GetPath();
            if (cloudPath != null) present.Add(System.IO.Path.GetFileName(ModelPathUtils.ConvertModelPathToUserVisiblePath(cloudPath)));
        }
        catch { }
        continue;
    }
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
        else if (ext == ".rcp" || ext == ".rcs")
        {
            if (place == ImportPlacement.Shared) { lines.Add(name + ": a point cloud is linked origin to origin only - by shared coordinates is not built here, so it was not linked."); continue; }
            // The type and its one instance together: an instance Revit
            // refuses takes the type it had just made away with it.
            using (var sub = new SubTransaction(doc))
            {
                sub.Start();
                try
                {
                    var cloudType = PointCloudType.Create(doc, ext.Substring(1), file);
                    var cloud = PointCloudInstance.Create(doc, cloudType.Id, Transform.Identity);
                    made = cloud.Id;
                    sub.Commit();
                }
                catch
                {
                    if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack();
                    throw;
                }
            }
        }
        else if (ext == ".ifc") { lines.Add(name + ": an IFC file is not linked here - Revit links an IFC through its IFC add-in (Insert > Link IFC), which first writes a Revit file from it."); continue; }
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
        var cloudInstance = back as PointCloudInstance;
        if (cloudInstance != null)
        {
            var cloudType = doc.GetElement(cloudInstance.GetTypeId()) as PointCloudType;
            try
            {
                if (cloudType != null)
                {
                    var found = cloudType.FoundStatus;
                    status = found == PointCloudFoundStatus.FoundOK ? ", a point cloud, its file found"
                        : found == PointCloudFoundStatus.NotFound ? ", a point cloud, ITS FILE NOT FOUND"
                        : ", a point cloud, file status " + found;
                }
            }
            catch { }
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
