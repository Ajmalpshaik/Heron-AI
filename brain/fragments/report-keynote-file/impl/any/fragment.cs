// NOT STANDALONE. Assumes `doc` is in scope; leaves `keynoteFile`,
// `tableEntries` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHICH KEYNOTE FILE: the file Annotate > Keynoting Settings names - the path
// as the model SAVED it, its path type (Absolute, Relative, or Content, which
// Revit's dialog calls At Library Locations), where it RESOLVES on this PC,
// and whether Revit can find it - with the count of entries in the loaded
// table. Loading or reloading it is LOAD_KEYNOTE_FILE. The numbering method
// (By keynote / By sheet) is not read: no Revit API member reaches it on any
// release 2020-2027 (row 5b-1420).
//
// MOVED HERE 2026-11-02, LINE FOR LINE, from this branch's widening of
// REPORT_KEYNOTES - which #794 proved on Revit 2024 against code without it.
// The tags and the values the table does not hold stay there.
//
// THE PATH IS REPORTED AS SAVED, NEVER REPLACED. A file Revit cannot find is
// named with "Revit: NotFound", never swapped for one that exists.

var findings = new List<string>();
var keynoteFile = "";
var tableEntries = 0;

try
{
    var table = KeynoteTable.GetKeynoteTable(doc);
    if (table != null)
    {
        foreach (var entry in table.GetKeyBasedTreeEntries())
        {
            if (entry == null) continue;
            tableEntries++;
        }

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
    else
    {
        findings.Add("This model has no keynote table, so no keynote file is named.");
    }
}
catch (Exception ex)
{
    findings.Add("The keynote table could not be read: " + ex.Message);
}

findings.Add("Keynote file: " + (keynoteFile == "" ? "none named in Keynoting Settings" : keynoteFile)
    + "; " + tableEntries + " entr" + (tableEntries == 1 ? "y" : "ies") + " in the loaded table.");
