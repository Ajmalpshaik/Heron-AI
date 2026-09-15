// NOT STANDALONE. Assumes `doc`, `nameContains` and `exportFolder` are in
// scope; leaves `familiesRead`, `tablesFound`, `summary`, `noTable`,
// `refused`, `scanned`, `leftOpen` and `findings` behind.
//
// READ ONLY of the project. Opens no transaction and needs none. It DOES open
// each family document to look inside it, and closes every one again without
// saving.
//
// A FAMILY LEFT OPEN IS NOT A NEUTRAL SIDE EFFECT. The next fragment would
// find it in front and answer about the wrong document, which reads as a
// fragment giving a wrong answer rather than as this one leaving a mess. So
// every family document is closed in a finally, whatever happened.
//
// A FAMILY THAT REFUSES TO OPEN IS NAMED. In-place families and some system
// families cannot be edited, and a silent skip is indistinguishable from a
// family with no table - two real answers that are not the same one.
//
// THE CSV IS THE POINT. A reducing tee is two keys and three dimensions; no
// line of text holds that. The summary says which family has which table and
// how big; the files hold what is in them.

var findings = new List<string>();
var noTable = new List<string>();
var refused = new List<string>();
var summaryRows = new List<string>();
var familiesRead = 0;
var tablesFound = 0;
var scanned = 0;
var summary = "";

var needle = (nameContains ?? "").Trim();
var folder = (exportFolder ?? "").Trim();
if (folder.Length > 0 && !folder.EndsWith("\\") && !folder.EndsWith("/")) folder = folder + "/";

// WHAT WAS ALREADY OPEN BEFORE THIS TOUCHED ANYTHING. A document this
// fragment did NOT open is somebody's unsaved work, and closing it throws
// that work away without a word. Measured 2026-09-16: six families were open
// with corrected size tables not yet loaded, this read closed all six, and
// every correction was lost. Revit does not always refuse to close a window a
// person has open - so the refusal cannot be relied on, and the fragment has
// to remember what it found.
var wasOpenBefore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
try
{
    foreach (Document open in app.Documents)
    {
        if (open != null && open.IsFamilyDocument && open.Title != null)
            wasOpenBefore.Add(open.Title);
    }
}
catch (Exception) { }

var leftOpen = new List<string>();
var families = new List<Family>();
try
{
    foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(Family)).ToElements())
    {
        var family = found as Family;
        if (family != null) families.Add(family);
    }
}
catch (Exception) { }

scanned = families.Count;

foreach (var family in families)
{
    var name = family.Name ?? "<unnamed>";
    if (needle.Length > 0 && name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
        continue;

    // NOT EVERY FAMILY CAN BE EDITED, and asking anyway throws rather than
    // returning null - so it is checked first where Revit offers the check.
    var editable = true;
    try { editable = family.IsEditable; }
    catch (Exception) { editable = true; }

    if (!editable)
    {
        refused.Add(string.Format("{0}: not editable - in-place or a system family", name));
        continue;
    }

    Document familyDocument = null;
    try { familyDocument = doc.EditFamily(family); }
    catch (Exception error)
    {
        refused.Add(string.Format("{0}: Revit refused to open it - {1}", name, error.Message));
        continue;
    }

    if (familyDocument == null)
    {
        refused.Add(string.Format("{0}: no family document came back", name));
        continue;
    }

    try
    {
        if (!familyDocument.IsFamilyDocument)
        {
            refused.Add(string.Format("{0}: what came back was not a family document", name));
            continue;
        }

        familiesRead++;

        FamilySizeTableManager manager = null;
        try
        {
            manager = FamilySizeTableManager.GetFamilySizeTableManager(
                familyDocument, familyDocument.OwnerFamily.Id);
        }
        catch (Exception) { manager = null; }

        if (manager == null)
        {
            noTable.Add(name);
            summaryRows.Add(string.Format("{0}: NO TABLE", name));
            continue;
        }

        var names = new List<string>();
        try
        {
            foreach (var tableName in manager.GetAllSizeTableNames()) names.Add(tableName);
        }
        catch (Exception) { }

        if (names.Count == 0)
        {
            noTable.Add(name);
            summaryRows.Add(string.Format("{0}: NO TABLE", name));
            continue;
        }

        var parts = new List<string>();
        foreach (var tableName in names)
        {
            var rowCount = -1;
            try
            {
                var table = manager.GetSizeTable(tableName);
                if (table != null) rowCount = table.NumberOfRows;
            }
            catch (Exception) { rowCount = -1; }

            tablesFound++;
            parts.Add(string.Format("{0}({1} rows)", tableName, rowCount));

            if (folder.Length > 0)
            {
                // One file per table, named so a folder of them is readable
                // without opening any.
                var safe = name;
                foreach (var bad in new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' })
                    safe = safe.Replace(bad, '_');
                var path = string.Format("{0}{1}__{2}.csv", folder, safe, tableName);
                try { manager.ExportSizeTable(tableName, path); }
                catch (Exception error)
                {
                    findings.Add(string.Format("Could not export {0} / {1}: {2}", name, tableName,
                        error.Message));
                }
            }
        }

        summaryRows.Add(string.Format("{0}: {1}", name, string.Join(", ", parts.ToArray())));
    }
    finally
    {
        // NEVER CLOSE A DOCUMENT THIS DID NOT OPEN. See the note beside
        // `wasOpenBefore` - closing somebody's open family discards whatever
        // they had not yet loaded.
        var title = "";
        try { title = familyDocument.Title ?? ""; }
        catch (Exception) { title = ""; }

        if (title.Length > 0 && wasOpenBefore.Contains(title))
        {
            leftOpen.Add(title);
        }
        else
        {
            try { familyDocument.Close(false); }
            catch (Exception) { leftOpen.Add(title.Length > 0 ? title : name); }
        }
    }
}

summary = string.Join("  ||  ", summaryRows.ToArray());

if (leftOpen.Count > 0)
{
    findings.Add(string.Format("{0} family(ies) were ALREADY OPEN and were left open: {1}. "
        + "Anything not yet loaded into the project is still safe in those windows.",
        leftOpen.Count, string.Join(", ", leftOpen.ToArray())));
}

findings.Insert(0, string.Format("{0} family(ies) read of {1} scanned; {2} table(s) found, {3} "
    + "family(ies) carry none, {4} refused to open, {5} left open as found",
    familiesRead, scanned, tablesFound, noTable.Count, refused.Count, leftOpen.Count));
