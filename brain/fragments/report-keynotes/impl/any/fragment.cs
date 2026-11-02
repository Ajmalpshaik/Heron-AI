// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves `keynotes`,
// `tagCount`, `missingFromTable`, `tableLoaded`, `keynoteFile`, `tableEntries`
// and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO QUESTIONS ABOUT KEYNOTES, ANSWERED TOGETHER:
//
//   the keynote tags   in one view or every view: the view, the text the tag
//                      shows, and whether that text is a key or a keynote text
//                      in the loaded keynote table
//   the values         every element type and material carrying a Keynote
//                      value, and which of those values the table does not hold
//
// A VALUE THE TABLE DOES NOT HOLD IS THE FINDING. The keynote file was swapped
// or edited and an element still carries a key that is no longer in it; the
// tag prints the bare key, or nothing, and the legend has no line for it.
//
// THE TABLE IS REVIT'S OWN LOADED COPY - KeynoteTable, read once - not the
// file on disk, so the answer is what the model would print today.
//
// Retagging or changing a keynote value is a parameter write on the element
// type: WRITE_ELEMENT_PARAMETERS with the Keynote parameter. This only reads.
//
// WHICH KEYNOTE FILE (2026-11-02): the file Annotate > Keynoting Settings
// names - the path as the model SAVED it, its path type (Absolute, Relative,
// or Content, which Revit's dialog calls At Library Locations), where it
// RESOLVES on this PC, and whether Revit can find it - with the count of
// entries in the loaded table. Loading or reloading it is LOAD_KEYNOTE_FILE.
// The numbering method (By keynote / By sheet) is not read: no Revit API
// member reaches it on any release 2020-2027 (row 5b-1420).

var findings = new List<string>();
var lines = new List<string>();
var missingFromTable = new List<string>();
var tagCount = 0;
var tableLoaded = false;
var keynoteFile = "";
var tableEntries = 0;

// THE TABLE: every key, and every keynote text, so a tag showing either is known.
var keys = new HashSet<string>(StringComparer.Ordinal);
var texts = new HashSet<string>(StringComparer.Ordinal);
try
{
    var table = KeynoteTable.GetKeynoteTable(doc);
    if (table != null)
    {
        foreach (var entry in table.GetKeyBasedTreeEntries())
        {
            if (entry == null) continue;
            tableEntries++;
            if (!string.IsNullOrEmpty(entry.Key)) keys.Add(entry.Key);
            var keynote = entry as KeynoteEntry;
            if (keynote != null && !string.IsNullOrEmpty(keynote.KeynoteText)) texts.Add(keynote.KeynoteText);
        }
        tableLoaded = keys.Count > 0;

        // THE FILE. A ModelPath is not a string: each conversion is guarded,
        // because a cloud-hosted path has no user-visible form.
        Func<ModelPath, string> readable = path =>
        {
            if (path == null) return "";
            try { return ModelPathUtils.ConvertModelPathToUserVisiblePath(path) ?? ""; }
            catch { return ""; }
        };
        ExternalFileReference fileRef = null;
        try { fileRef = table.GetExternalFileReference(); } catch { }
        if (fileRef != null)
        {
            var saved = "";
            var resolved = "";
            var pathType = "";
            var status = "";
            try { saved = readable(fileRef.GetPath()); } catch { }
            try { resolved = readable(fileRef.GetAbsolutePath()); } catch { }
            try { pathType = fileRef.PathType.ToString(); } catch { }
            try { status = fileRef.GetLinkedFileStatus().ToString(); } catch { }
            keynoteFile = (saved == "" ? "(no path saved)" : saved)
                + (pathType == "" ? "" : " | path type " + pathType)
                + (resolved != "" && !string.Equals(resolved, saved, StringComparison.OrdinalIgnoreCase)
                    ? " | resolves to " + resolved : "")
                + (status == "" ? "" : " | Revit: " + status);
        }
        if (keynoteFile == "")
        {
            // Not a file Revit tracks as one - a keynote server, or none. The
            // resource reference still names it by its display path.
            try
            {
                var resource = table.GetExternalResourceReference(
                    ExternalResourceTypes.BuiltInExternalResourceTypes.KeynoteTable);
                if (resource != null && !string.IsNullOrEmpty(resource.InSessionPath))
                    keynoteFile = resource.InSessionPath;
            }
            catch { }
        }
    }
}
catch (Exception ex)
{
    findings.Add("The keynote table could not be read: " + ex.Message);
}
lines.Add("KEYNOTE FILE: " + (keynoteFile == "" ? "none named in Keynoting Settings" : keynoteFile)
    + " | " + tableEntries + " entr" + (tableEntries == 1 ? "y" : "ies") + " loaded");

// THE VIEW BY NAME, AND EMPTY MEANS EVERY VIEW - a string, because a View
// need cannot be left out.
// "Level 1" can name a floor plan AND a ceiling plan, so the kind may be
// written in front - "FloorPlan: Level 1", as the rest of Heron writes views -
// and a name that still answers to two views is refused, never guessed.
View view = null;
string viewProblem = null;
var wantedView = (viewName ?? "").Trim();
if (wantedView != "")
{
    var wantedKind = "";
    var bare = wantedView;
    var colon = wantedView.IndexOf(':');
    if (colon > 0)
    {
        wantedKind = wantedView.Substring(0, colon).Replace(" ", "").Trim();
        bare = wantedView.Substring(colon + 1).Trim();
    }
    var answering = new List<View>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var v = candidate as View;
        if (v == null || v.IsTemplate) continue;
        if (!string.Equals(v.Name, bare, StringComparison.OrdinalIgnoreCase)) continue;
        if (wantedKind != "" && !string.Equals(v.ViewType.ToString(), wantedKind, StringComparison.OrdinalIgnoreCase)) continue;
        answering.Add(v);
    }
    if (answering.Count == 1) view = answering[0];
    else if (answering.Count > 1)
        viewProblem = answering.Count + " views are named \"" + bare + "\" ("
            + string.Join(", ", answering.Select(a => a.ViewType.ToString())) + "). Write it as \""
            + answering[0].ViewType + ": " + bare + "\".";
    if (view == null)
        findings.Add((viewProblem ?? "No view is named \"" + wantedView + "\".")
            + " So the tags of every view were read.");
}

// THE TAGS.
var collector = view == null
    ? new FilteredElementCollector(doc)
    : new FilteredElementCollector(doc, view.Id);
foreach (var element in collector.OfCategory(BuiltInCategory.OST_KeynoteTags).WhereElementIsNotElementType())
{
    var tag = element as IndependentTag;
    if (tag == null) continue;
    tagCount++;
    var shown = "";
    try { shown = tag.TagText ?? ""; } catch { }
    var ownerName = "";
    try
    {
        var owner = doc.GetElement(tag.OwnerViewId) as View;
        ownerName = owner == null ? "" : owner.Name;
    }
    catch { }
    var where = shown == "" ? "SHOWS NOTHING"
        : keys.Contains(shown) ? "a key in the table"
        : texts.Contains(shown) ? "a keynote text in the table"
        : tableLoaded ? "NOT IN THE TABLE" : "table not loaded, not checked";
    lines.Add("tag " + tag.Id + " | " + ownerName + " | \"" + shown + "\" | " + where);
}

// THE VALUES on element types and materials.
var seen = new Dictionary<string, List<string>>(StringComparer.Ordinal);
Action<Element> readValue = owner =>
{
    try
    {
        var p = owner.get_Parameter(BuiltInParameter.KEYNOTE_PARAM);
        if (p == null || !p.HasValue) return;
        var value = p.AsString();
        if (string.IsNullOrEmpty(value)) return;
        List<string> list;
        if (!seen.TryGetValue(value, out list)) { list = new List<string>(); seen[value] = list; }
        if (list.Count < 5) list.Add(owner.Name);
        else if (list.Count == 5) list.Add("and more");
    }
    catch { }
};
foreach (var owner in new FilteredElementCollector(doc).WhereElementIsElementType()) readValue(owner);
foreach (var owner in new FilteredElementCollector(doc).OfClass(typeof(Material))) readValue(owner);

foreach (var pair in seen)
{
    if (tableLoaded && !keys.Contains(pair.Key))
        missingFromTable.Add(pair.Key + " (on " + string.Join(", ", pair.Value) + ")");
}

if (missingFromTable.Count > 0)
    lines.Add("KEYNOTE VALUES THE TABLE DOES NOT HOLD: " + string.Join("; ", missingFromTable));

var keynotes = string.Join("\n", lines);
findings.Add("Keynote file: " + (keynoteFile == "" ? "none named in Keynoting Settings" : keynoteFile)
    + "; " + tableEntries + " entr" + (tableEntries == 1 ? "y" : "ies") + " in the loaded table.");
findings.Add(tagCount + " keynote tag(s)" + (view == null ? "" : " in \"" + view.Name + "\"") + "; "
    + seen.Count + " keynote value(s) in use on types and materials; "
    + (tableLoaded
        ? missingFromTable.Count + " of them not in the loaded keynote table (" + keys.Count + " keys)."
        : "NO KEYNOTE TABLE IS LOADED, so no value could be checked against one."));
