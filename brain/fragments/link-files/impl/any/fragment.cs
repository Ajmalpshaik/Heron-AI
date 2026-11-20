// NOT STANDALONE. Assumes `doc`, `paths`, `placement` and `viewName` are in
// scope; leaves `linked`, `linkedIds`, `alreadyLinked`, `notAttached`,
// `report` and `findings` behind.
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
// regeneration; a point cloud's found status is read off its type.
//
// VERSION 3 (FRAGMENT-ISSUES rows 5b-1124 and 5b-1121) - A REVIT LINK IS READ
// BACK BY ITS INSTANCE, NOT ITS TYPE. Version 2 read only the type's
// GetLinkedFileStatus and said "Loaded" - measured 2026-10-18 on Revit 2020,
// an instance made here whose GetLinkDocument() stayed null and drew nothing,
// while the same file linked by hand drew at once. Now the instance's own
// GetLinkDocument() is read after the regeneration, and the model elements in
// it are counted. A Revit link is counted in `linked` and `linkedIds` ONLY
// when its model attached; one made whose document reads null is named in
// `notAttached` with its id, and never called Loaded. It is NOT removed: the
// change is the modeller's to keep or discard. WHETHER GetLinkDocument()
// ANSWERS INSIDE THE OPEN TRANSACTION IS NOT KNOWN - no run has read it there;
// the NEEDS-CHECKING run of this version says, against the same file linked
// by hand (Insert > Link Revit). RevitLinkType.Load and LoadFrom cannot be the
// cure here: RevitAPI.xml says every transaction phase must be finished
// before they are called, and this card runs inside Heron's.
//
// Two more facts are read and said, changing nothing: the LinkLoadResult
// RevitLinkType.Create hands back (its LoadResult, by name - "UsedExisting"
// would say Revit took a document it already had open), and how many
// documents of that file are already open in this Revit before it is linked
// - row 5b-1121 found thirteen open after rolled-back link runs, and the link
// then kept answered null.
//
// RELEASES (tools/api-surface --members, 2026-11-02): PointCloudType.Create(
// Document, string, string), PointCloudType.GetPath and FoundStatus,
// PointCloudInstance.Create(Document, ElementId, Transform) and SubTransaction
// are the same 2020 to 2027. PointCloudFoundStatus gains Invalid in 2025, so
// it is written out by name, never matched value by value.

var linked = 0;
var linkedIds = new List<ElementId>();
var alreadyLinked = new List<string>();
var notAttached = new List<string>();
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
        var loadResult = "";
        var openAlready = "";
        if (ext == ".rvt")
        {
            // Row 5b-1121: copies of this file already open in this Revit,
            // read before linking and only reported.
            try
            {
                var wanted = System.IO.Path.GetFullPath(file);
                var openCount = 0;
                var openLinked = 0;
                foreach (Document open in doc.Application.Documents)
                {
                    string openPath = null;
                    try { openPath = open.PathName; } catch { }
                    if (string.IsNullOrEmpty(openPath)) continue;
                    string full = openPath;
                    try { full = System.IO.Path.GetFullPath(openPath); } catch { }
                    if (!string.Equals(full, wanted, StringComparison.OrdinalIgnoreCase)) continue;
                    openCount++;
                    try { if (open.IsLinked) openLinked++; } catch { }
                }
                if (openCount > 0)
                    openAlready = "; this file was ALREADY OPEN in this Revit as " + openCount
                        + " document(s)" + (openLinked > 0 ? ", " + openLinked + " of them as a link" : "")
                        + " before it was linked";
            }
            catch { }
            var result = RevitLinkType.Create(doc, ModelPathUtils.ConvertUserVisiblePathToModelPath(file), new RevitLinkOptions(false));
            try { loadResult = result.LoadResult.ToString(); } catch { }
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
        var status = "";
        var revitInstance = back as RevitLinkInstance;
        if (revitInstance != null)
        {
            // VERSION 3: the instance's own document, never the type's status.
            Document linkDocument = null;
            try { linkDocument = revitInstance.GetLinkDocument(); } catch { }
            var typeStatus = "";
            try
            {
                var type = doc.GetElement(revitInstance.GetTypeId()) as RevitLinkType;
                if (type != null) typeStatus = type.GetLinkedFileStatus().ToString();
            }
            catch { }
            var told = (loadResult == "" ? "" : "; Revit's load result " + loadResult)
                + (typeStatus == "" ? "" : "; its type reads " + typeStatus) + openAlready;
            present.Add(name);
            if (linkDocument == null)
            {
                notAttached.Add(name + " (link " + made + ")");
                lines.Add(name + ": Revit made the link (id " + made + ") but its model did NOT attach - "
                    + "nothing of it shows, and it is NOT counted as linked" + told + ".");
                continue;
            }
            var modelElements = 0;
            try
            {
                foreach (var inLink in new FilteredElementCollector(linkDocument)
                             .WhereElementIsNotElementType().WhereElementIsViewIndependent())
                {
                    Category category = null;
                    try { category = inLink.Category; } catch { }
                    if (category != null && category.CategoryType == CategoryType.Model) modelElements++;
                }
            }
            catch { }
            var title = "";
            try { title = linkDocument.Title; } catch { }
            status = ", its model attached - \"" + title + "\", " + modelElements + " model element(s)" + told;
        }
        else present.Add(name);
        linked++;
        linkedIds.Add(made);
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
findings.Add(linked + " file(s) linked and read back; " + alreadyLinked.Count + " already linked."
    + (notAttached.Count == 0 ? "" : " " + notAttached.Count + " Revit link(s) made whose model did NOT attach, "
       + "not counted as linked: " + string.Join(", ", notAttached) + "."));
findings.AddRange(lines);
