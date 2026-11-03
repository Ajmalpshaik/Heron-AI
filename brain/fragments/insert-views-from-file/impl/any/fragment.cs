// NOT STANDALONE. Assumes `doc`, `filePath`, `viewNames` and `into` are in
// scope; leaves `copied`, `newViews`, `elementsCopied`, `notFound`, `clashed`,
// `refused`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) on `doc`, and opens none - on
// `doc` or on the file it reads.
//
// REVIT'S INSERT > INSERT FROM FILE, FOR A FILE THAT IS NOT OPEN. The .rvt
// named is opened DETACHED (its central, if it has one, is never touched),
// with its worksets open so that nothing drawn is left behind, as the wrong
// user allowed (the path is often somebody else's local). It is read and
// CLOSED WITHOUT SAVING in a finally - whatever happens in between. A file
// already open in this Revit is used as it is and left open.
//
// INSERT VIEWS FROM FILE. Each drafting view or legend named is copied with
// the document-to-document copy, which brings the view EMPTY; then the
// elements the source view owns are copied view to view into the new one.
// TRANSFER_VIEWS_BETWEEN_DOCUMENTS found both halves are needed; it is PROVEN
// and not called (its source must be an open document found by title).
//
// INSERT 2D ELEMENTS FROM FILE. With `into`, the elements the ONE view named
// owns are copied view to view into that view here, at the same coordinates -
// Revit's own command places them at the cursor; MOVE_ELEMENTS moves them on.
//
// A TYPE NAME ALREADY HERE. Revit resolves it through a handler that must be
// a class, and a fragment cannot declare one, so none is set: each view is
// copied on its own, a refusal names that view in Revit's words, and the rest
// go on.
//
// READ BACK. Each view here is counted again after the copy - the elements it
// owns - and set beside what the file's view held. A shortfall is named.

var copied = new List<string>();
var newViews = new List<ElementId>();
var elementsCopied = 0;
var notFound = new List<string>();
var clashed = new List<string>();
var refused = "";
var report = "";
var findings = new List<string>();

// An optional need left out arrives as the text "none" (row 5b-1432).
var target = (into ?? "").Trim();
if (string.Equals(target, "none", StringComparison.OrdinalIgnoreCase)) target = "";
var path = (filePath ?? "").Trim().Trim('"');
var wanted = new List<string>();
foreach (var part in (viewNames ?? "").Split(';'))
{
    var name = part.Trim();
    if (name.Length > 0 && !wanted.Contains(name)) wanted.Add(name);
}

Func<Document, View, List<ElementId>> ownedBy = (d, v) =>
{
    var ids = new List<ElementId>();
    foreach (var element in new FilteredElementCollector(d, v.Id).WhereElementIsNotElementType())
    {
        if (element == null || element.Id == v.Id) continue;
        ElementId owner = null;
        try { owner = element.OwnerViewId; } catch (Exception) { owner = null; }
        if (owner != null && owner == v.Id) ids.Add(element.Id);
    }
    return ids;
};

if (doc.IsFamilyDocument)
    refused = "The document in front is a family. Views are inserted from a file into a project. NOTHING WAS COPIED.";
else if (path.Length == 0)
    refused = "No file was named. Name the .rvt to take the views from. NOTHING WAS COPIED.";
else if (!path.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
    refused = "\"" + path + "\" is not a Revit project file (.rvt). NOTHING WAS COPIED.";
else if (!System.IO.File.Exists(path))
    refused = "No file is at \"" + path + "\". NOTHING WAS COPIED.";
else if (wanted.Count == 0)
    refused = "No view was named. Name the drafting views or legends to insert, \";\" between them. NOTHING WAS COPIED.";
else if (target.Length > 0 && wanted.Count != 1)
    refused = "Insert 2D Elements takes the elements of ONE view - " + wanted.Count + " were named. NOTHING WAS COPIED.";

var fullPath = "";
if (refused.Length == 0)
{
    try { fullPath = System.IO.Path.GetFullPath(path); } catch (Exception) { fullPath = path; }
    var herePath = "";
    try { herePath = doc.PathName ?? ""; } catch (Exception) { herePath = ""; }
    if (herePath.Length > 0 && string.Equals(System.IO.Path.GetFullPath(herePath), fullPath, StringComparison.OrdinalIgnoreCase))
        refused = "\"" + path + "\" is the model in front. NOTHING WAS COPIED.";
}

// The target view here, for Insert 2D Elements.
View targetView = null;
if (refused.Length == 0 && target.Length > 0)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var view = element as View;
        if (view == null || view.IsTemplate) continue;
        var name = "";
        try { name = view.Name ?? ""; } catch (Exception) { name = ""; }
        if (string.Equals(name, target, StringComparison.Ordinal)) { targetView = view; break; }
    }
    if (targetView == null)
        refused = "No view in this model is named exactly \"" + target + "\" to insert the 2D elements into. NOTHING WAS COPIED.";
}

Document source = null;
var openedHere = false;
if (refused.Length == 0)
{
    foreach (Document open in doc.Application.Documents)
    {
        if (open == null || open.Equals(doc)) continue;
        var openPath = "";
        try { openPath = open.PathName ?? ""; } catch (Exception) { openPath = ""; }
        if (openPath.Length == 0) continue;
        var same = false;
        try { same = string.Equals(System.IO.Path.GetFullPath(openPath), fullPath, StringComparison.OrdinalIgnoreCase); }
        catch (Exception) { same = false; }
        if (same && !open.IsLinked) { source = open; break; }
    }
    if (source != null)
        findings.Add("\"" + source.Title + "\" is already open in this Revit - used as it is and left open.");
}

try
{
    if (refused.Length == 0 && source == null)
    {
        var openOptions = new OpenOptions();
        openOptions.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
        openOptions.AllowOpeningLocalByWrongUser = true;
        openOptions.Audit = false;
        try
        {
            openOptions.SetOpenWorksetsConfiguration(new WorksetConfiguration(WorksetConfigurationOption.OpenAllWorksets));
        }
        catch (Exception) { }
        try
        {
            source = doc.Application.OpenDocumentFile(ModelPathUtils.ConvertUserVisiblePathToModelPath(path), openOptions);
            openedHere = source != null;
        }
        catch (Exception ex)
        {
            // Matched by type NAME, as COMPARE_MODELS does - the Revit
            // exceptions namespace is not in a fragment's scope.
            var kind = ex.GetType().Name;
            refused = "Revit would not open \"" + path + "\" - " + ex.Message
                + (kind == "CorruptModelException" ? " (Revit says corrupt for a file saved in a newer release, too)" : "")
                + ". NOTHING WAS COPIED.";
        }
        if (refused.Length == 0 && source == null)
            refused = "Revit opened nothing at \"" + path + "\". NOTHING WAS COPIED.";
        if (refused.Length == 0 && source.IsFamilyDocument)
            refused = "\"" + path + "\" is a family, not a project. NOTHING WAS COPIED.";
    }

    // ---- the views asked for, in the file ----
    var found = new List<View>();
    if (refused.Length == 0)
    {
        var byName = new Dictionary<string, View>(StringComparer.Ordinal);
        var otherKind = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var element in new FilteredElementCollector(source).OfClass(typeof(View)))
        {
            var view = element as View;
            if (view == null || view.IsTemplate) continue;
            var name = "";
            try { name = view.Name ?? ""; } catch (Exception) { continue; }
            if (view.ViewType == ViewType.DraftingView || view.ViewType == ViewType.Legend) byName[name] = view;
            else otherKind[name] = view.ViewType.ToString();
        }
        foreach (var name in wanted)
        {
            View view;
            string kind;
            if (byName.TryGetValue(name, out view)) found.Add(view);
            else if (otherKind.TryGetValue(name, out kind))
                notFound.Add(name + " (a " + kind + " view, not a drafting view or legend)");
            else notFound.Add(name + " (no view of that name in the file)");
        }
        if (found.Count == 0)
            refused = "None of the " + wanted.Count + " view(s) named is a drafting view or legend in \"" + source.Title + "\". "
                + "NOTHING WAS COPIED.";
    }

    var options = new CopyPasteOptions();
    if (refused.Length == 0 && targetView != null)
    {
        // ---- Insert 2D Elements from File ----
        var view = found[0];
        var contents = ownedBy(source, view);
        if (contents.Count == 0)
            refused = "\"" + view.Name + "\" in \"" + source.Title + "\" has nothing drawn in it. NOTHING WAS COPIED.";
        else
        {
            var before = ownedBy(doc, targetView).Count;
            try
            {
                ElementTransformUtils.CopyElements(view, contents, targetView, Transform.Identity, options);
                doc.Regenerate();
                var arrived = ownedBy(doc, targetView).Count - before;
                elementsCopied += arrived;
                copied.Add(targetView.Name + " (" + arrived + " of " + contents.Count + " element(s) from \"" + view.Name + "\")");
                if (arrived < contents.Count)
                    findings.Add("\"" + targetView.Name + "\" gained " + arrived + " element(s) for the " + contents.Count
                        + " \"" + view.Name + "\" holds - look before keeping.");
            }
            catch (Exception ex)
            {
                clashed.Add(view.Name + " into " + targetView.Name + " - Revit refused: " + ex.Message);
            }
        }
    }
    else if (refused.Length == 0)
    {
        // ---- Insert Views from File: the view, then what it owns ----
        foreach (var view in found)
        {
            var name = view.Name;
            View landed = null;
            try
            {
                var arrived = ElementTransformUtils.CopyElements(source, new List<ElementId> { view.Id }, doc,
                    Transform.Identity, options);
                foreach (var id in arrived)
                {
                    landed = doc.GetElement(id) as View;
                    if (landed != null) break;
                }
            }
            catch (Exception ex)
            {
                clashed.Add(name + " - Revit refused the view: " + ex.Message);
                continue;
            }
            if (landed == null) { clashed.Add(name + " - Revit's copy came back without a view"); continue; }
            newViews.Add(landed.Id);

            var contents = ownedBy(source, view);
            var got = 0;
            if (contents.Count > 0)
            {
                try
                {
                    ElementTransformUtils.CopyElements(view, contents, landed, Transform.Identity, options);
                    doc.Regenerate();
                    got = ownedBy(doc, landed).Count;
                }
                catch (Exception ex)
                {
                    clashed.Add(name + " - the view arrived EMPTY: Revit refused its contents: " + ex.Message);
                }
            }
            elementsCopied += got;
            copied.Add(landed.Name + " (" + got + " of " + contents.Count + " element(s))");
            if (landed.Name != name)
                findings.Add("\"" + name + "\" arrived as \"" + landed.Name + "\" - a view of that name is already here.");
            if (got < contents.Count)
                findings.Add("\"" + landed.Name + "\" holds " + got + " element(s) for the " + contents.Count
                    + " the file's view holds - look before keeping.");
        }
    }
}
finally
{
    if (openedHere && source != null)
    {
        try { source.Close(false); }
        catch (Exception ex) { findings.Add("Revit would not close \"" + path + "\" - " + ex.Message + ". Close it without saving."); }
    }
}

if (refused.Length == 0 && copied.Count == 0)
    refused = "Revit refused every view named - each is in clashed with its words. NOTHING WAS COPIED.";

report = refused.Length > 0 ? refused
    : (targetView != null
        ? "Inserted " + elementsCopied + " 2D element(s) from \"" + wanted[0] + "\" into \"" + targetView.Name + "\""
        : "Inserted " + newViews.Count + " view(s) with " + elementsCopied + " element(s)")
      + " from \"" + System.IO.Path.GetFileName(path) + "\""
      + (notFound.Count > 0 ? "; " + notFound.Count + " name(s) not found" : "")
      + (clashed.Count > 0 ? "; " + clashed.Count + " refused by Revit, each named" : "")
      + (openedHere ? "; the file was closed without saving" : "") + ".";
foreach (var name in notFound) findings.Add("Not found: " + name + ".");
foreach (var reason in clashed) findings.Add(reason);
if (refused.Length > 0) findings.Add(refused);