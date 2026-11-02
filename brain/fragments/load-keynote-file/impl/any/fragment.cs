// NOT STANDALONE. Assumes `doc`, `filePath` and `pathType` are in scope;
// leaves `loadResult`, `fileBefore`, `fileAfter`, `entriesBefore`,
// `entriesAfter`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and opens none (Golden Rule 16). Revit REQUIRES
// one here: KeyBasedTreeEntryTable.LoadFrom and Reload both throw
// ModificationOutsideTransactionException when "the document has no open
// transaction" - RevitAPI.xml, every release 2020 to 2027. So, unlike Reload
// Latest (row 5b-1418), this runs inside Heron's transaction group as it is.
//
// WHAT IT DOES - Annotate > Keynoting Settings, the keynote table file:
//
//   filePath given    load THAT file - the full path the modeller typed, read
//                     on the Revit PC. Never guessed, never completed: a path
//                     with no file at it is refused before anything changes.
//   filePath empty    reload the file already named in Keynoting Settings
//                     (Revit's Reload button) - or, with a path type asked
//                     for, load the same file again under that path type.
//   pathType          absolute or relative as asked; empty keeps the one the
//                     model has now.
//
// REVIT'S OWN RESULT IS THE ANSWER. LoadFrom and Reload return an
// ExternalResourceLoadStatus, and a KeyBasedTreeEntriesLoadResults carries the
// files Revit could not read, the lines it could not parse, the entries it
// left out and its failure messages - every one is reported in words. On a
// failure Revit leaves the table unchanged (its own remark on Reload); the
// file and the entry count are read back after the call either way.

var findings = new List<string>();
var refused = new List<string>();
var loadResult = "";
var fileBefore = "";
var fileAfter = "";
var entriesBefore = 0;
var entriesAfter = 0;

KeynoteTable table = null;
try { table = KeynoteTable.GetKeynoteTable(doc); } catch (Exception ex) { refused.Add("The keynote table could not be read: " + ex.Message); }
if (table == null && refused.Count == 0) refused.Add("This model has no keynote table, so there is nothing to load a keynote file into.");

Func<ModelPath, string> readable = path =>
{
    if (path == null) return "";
    try { return ModelPathUtils.ConvertModelPathToUserVisiblePath(path) ?? ""; }
    catch { return ""; }
};
Func<int> countEntries = () =>
{
    var n = 0;
    try { foreach (var entry in table.GetKeyBasedTreeEntries()) if (entry != null) n++; } catch { }
    return n;
};
// The file as Keynoting Settings names it: saved path, path type, where it
// resolves on this PC, Revit's status - and the absolute path and path type
// on their own, for a reload under another path type.
var currentAbsolute = "";
var currentType = "";
Func<string> describe = () =>
{
    currentAbsolute = "";
    currentType = "";
    ExternalFileReference fileRef = null;
    try { fileRef = table.GetExternalFileReference(); } catch { }
    if (fileRef != null)
    {
        var saved = "";
        var status = "";
        try { saved = readable(fileRef.GetPath()); } catch { }
        try { currentAbsolute = readable(fileRef.GetAbsolutePath()); } catch { }
        try { currentType = fileRef.PathType.ToString(); } catch { }
        try { status = fileRef.GetLinkedFileStatus().ToString(); } catch { }
        if (saved != "" || currentAbsolute != "")
            return (saved == "" ? currentAbsolute : saved)
                + (currentType == "" ? "" : " (" + currentType + ")")
                + (currentAbsolute != "" && !string.Equals(currentAbsolute, saved, StringComparison.OrdinalIgnoreCase)
                    ? ", resolving to " + currentAbsolute : "")
                + (status == "" ? "" : ", Revit: " + status);
    }
    try
    {
        var resource = table.GetExternalResourceReference(ExternalResourceTypes.BuiltInExternalResourceTypes.KeynoteTable);
        if (resource != null && !string.IsNullOrEmpty(resource.InSessionPath)) return resource.InSessionPath;
    }
    catch { }
    return "";
};

if (refused.Count == 0)
{
    fileBefore = describe();
    entriesBefore = countEntries();
}

// THE PATH TYPE - absolute or relative as asked, empty keeps the current one.
// Anything else is refused before anything changes.
var wantedType = (pathType ?? "").Trim().ToLowerInvariant();
PathType? asked = null;
if (wantedType == "absolute") asked = PathType.Absolute;
else if (wantedType == "relative") asked = PathType.Relative;
else if (wantedType != "")
    refused.Add("Path type \"" + pathType + "\" is not one this sets - say absolute or relative, or leave it out to keep the one the model has ("
        + (currentType == "" ? "none yet" : currentType) + ").");

// THE FILE - the modeller's path, as typed, quotes taken off. Never guessed.
var wanted = (filePath ?? "").Trim().Trim('"').Trim();

if (refused.Count == 0)
{
    var results = new KeyBasedTreeEntriesLoadResults();
    ExternalResourceLoadStatus? status = null;
    var target = wanted;
    var reloadInPlace = wanted == "" && asked == null;

    if (wanted == "" && asked != null)
    {
        // A path type asked for and no file named: the same file again, under
        // that path type - Revit's Reload keeps the stored one.
        if (currentAbsolute == "")
            refused.Add("No keynote file is named in Keynoting Settings, so there is no file to make " + wantedType + " - name the file by its full path.");
        else target = currentAbsolute;
    }
    else if (wanted != "")
    {
        if (!System.IO.Path.IsPathRooted(wanted))
            refused.Add("\"" + wanted + "\" is not a full path. Write it whole, as Windows Explorer shows it - C:\\Standards\\Keynotes.txt, or \\\\server\\share\\Keynotes.txt.");
        else if (!System.IO.File.Exists(wanted))
            refused.Add("There is no file at \"" + wanted + "\" that this PC can see - the keynote file is unchanged. Check the path, the drive letter, or that the server share is reachable.");
    }

    if (refused.Count == 0 && reloadInPlace)
    {
        if (fileBefore == "")
            refused.Add("No keynote file is named in Keynoting Settings, so there is nothing to reload - name the file by its full path.");
        else
        {
            try { status = table.Reload(results); }
            catch (Exception ex) { refused.Add("Revit refused to reload the keynote file: " + ex.Message); }
        }
    }
    else if (refused.Count == 0)
    {
        var type = asked ?? (currentType == "Relative" ? PathType.Relative : PathType.Absolute);
        if (asked == null && currentType != "" && currentType != "Absolute" && currentType != "Relative")
            findings.Add("The keynote file was " + currentType + " (Revit's dialog: At Library Locations for Content); a full path cannot be kept that way, so it is loaded Absolute.");
        if (type == PathType.Relative && string.IsNullOrEmpty(doc.PathName))
            refused.Add("The model has never been saved, so a relative path has nothing to be relative to. Save it first, or ask for absolute.");
        else
        {
            ExternalResourceReference reference = null;
            try
            {
                reference = ExternalResourceReference.CreateLocalResource(doc,
                    ExternalResourceTypes.BuiltInExternalResourceTypes.KeynoteTable,
                    ModelPathUtils.ConvertUserVisiblePathToModelPath(target), type);
            }
            catch (Exception ex) { refused.Add("Revit would not take \"" + target + "\" as a " + type + " keynote file: " + ex.Message); }
            if (reference != null)
            {
                var supported = false;
                try { supported = table.ServerSupports(reference); } catch { }
                if (!supported) refused.Add("Revit says the place \"" + target + "\" is in cannot serve keynotes.");
                else
                {
                    try { status = table.LoadFrom(reference, results); }
                    catch (Exception ex) { refused.Add("Revit refused to load \"" + target + "\": " + ex.Message); }
                }
            }
        }
    }

    // REVIT'S RESULT, IN WORDS.
    if (status != null)
    {
        var s = status.Value;
        loadResult = s == ExternalResourceLoadStatus.Success ? (reloadInPlace ? "reloaded" : "loaded")
            : s == ExternalResourceLoadStatus.ResourceAlreadyCurrent ? "already current - Revit changed nothing"
            : s == ExternalResourceLoadStatus.CannotFindServer ? "FAILED - Revit cannot find the place the file is kept"
            : s == ExternalResourceLoadStatus.ServerThrewException ? "FAILED - the place the file is kept raised an error"
            : "FAILED (" + s + ") - Revit left the keynote table unchanged";
        if (s != ExternalResourceLoadStatus.Success && s != ExternalResourceLoadStatus.ResourceAlreadyCurrent)
            refused.Add("Revit did not load the keynote file: " + loadResult + ".");
    }
    try
    {
        foreach (var file in results.GetFileReadErrors())
            findings.Add("Revit could not read " + file + " - it is locked, or this PC may not open it.");
        var syntax = results.GetFileSyntaxErrors();
        if (syntax.Count > 0)
            findings.Add(syntax.Count + " line(s) of the file could not be read as keynotes and were left out"
                + " - key, text and parent, separated by tabs - for example: "
                + string.Join(" | ", syntax.Take(5).Select(line => "\"" + line + "\"")));
        var entryErrors = results.GetKeyBasedTreeEntryErrors();
        if (entryErrors.Count > 0)
        {
            var named = new List<string>();
            foreach (var error in entryErrors.Take(5))
            {
                var key = "";
                try { var entry = error.GetEntry(); key = entry == null ? "" : entry.Key; } catch { }
                named.Add((key == "" ? "an entry" : "\"" + key + "\"") + " (" + error.ErrorType + ")");
            }
            findings.Add(entryErrors.Count + " entr" + (entryErrors.Count == 1 ? "y was" : "ies were") + " left out by Revit: "
                + string.Join(", ", named) + (entryErrors.Count > 5 ? ", and more" : "")
                + ". DuplicateEntry is a key written twice; MissingParent a parent key the file does not hold.");
        }
        foreach (var message in results.GetFailureMessages().Take(5))
        {
            var text = "";
            try { text = message.GetDescriptionText(); } catch { }
            if (!string.IsNullOrEmpty(text)) findings.Add("Revit: " + text);
        }
    }
    catch { }
}

// READ BACK, whatever happened.
if (table != null)
{
    fileAfter = describe();
    entriesAfter = countEntries();
}
if (loadResult == "" && refused.Count > 0) loadResult = "nothing loaded - " + refused[0];

findings.Insert(0, "Keynote file before: " + (fileBefore == "" ? "none" : fileBefore) + ", " + entriesBefore + " entries. After: "
    + (fileAfter == "" ? "none" : fileAfter) + ", " + entriesAfter + " entries. " + loadResult + ".");
if (wanted != "" && fileAfter != "" && loadResult.StartsWith("loaded")
    && currentAbsolute != "" && !string.Equals(currentAbsolute, wanted, StringComparison.OrdinalIgnoreCase))
    findings.Add("Revit reports the file as " + currentAbsolute + ", not the path typed (" + wanted + ") - check it in Keynoting Settings.");
