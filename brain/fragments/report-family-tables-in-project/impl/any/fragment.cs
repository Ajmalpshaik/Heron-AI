// NOT STANDALONE. Assumes `doc`, `app`, `nameContains`, `exportFolder`,
// `tableName` and `exportTo` are in scope; leaves `familiesRead`,
// `tablesFound`, `summary`, `noTable`, `refused`, `scanned`, `leftOpen`,
// `tableNames`, `columnHeaders`, `rows`, `rowCount`, `columnCount`,
// `exportedTo`, `notAFamily` and `findings` behind.
//
// READ ONLY of the model. Opens no transaction and needs none. From a project
// it DOES open each family document to look inside it, and closes every one
// again without saving.
//
// VERSION 2 (2026-11-01) took in REPORT_FAMILY_SIZE_TABLE, which read the
// lookup table of the family open in front and is DEPRECATED, merged into
// this card. So this reads EITHER:
//
//   the FAMILY IN FRONT, when the document in front is a family - every table
//   it holds named, and one table read whole: every row, every column header,
//   Revit's own value strings; `tableName` picks it, empty reads the first,
//   and a name the family has not got is refused with the ones it has, never
//   a silent fall back to the first; `exportTo` writes that table as CSV.
//
//   every LOADED FAMILY, when the document in front is a project - each
//   opened, its tables read and written out as CSV, and closed again, as
//   version 1 did. When `nameContains` narrows to ONE family, that family's
//   table is read whole as well, as if it were in front.
//
// "none" in any of the four text inputs means empty: it is what each binds
// when left out.
//
// A SIZE MISSING FROM THE LOOKUP TABLE IS SILENT. A parametric fitting looks
// its dimensions up by the size it is asked for; ask for a size with no row
// and it cannot resolve, and Revit says nothing. EVERY ROW COMES BACK AS ONE
// STRING: a list is abbreviated to three entries before a reader sees it, and
// the whole table is the answer. VALUES ARE REVIT'S OWN STRINGS - AsValueString
// is what the family itself reads.
//
// A FAMILY LEFT OPEN IS NOT A NEUTRAL SIDE EFFECT. The next fragment would
// find it in front and answer about the wrong document, which reads as a
// fragment giving a wrong answer rather than as this one leaving a mess. So
// every family document this opened is closed in a finally, whatever happened.
//
// A FAMILY THAT REFUSES TO OPEN IS NAMED. In-place families and some system
// families cannot be edited, and a silent skip is indistinguishable from a
// family with no table - two real answers that are not the same one.
//
// THE CSV IS THE POINT FOR MANY FAMILIES. A reducing tee is two keys and three
// dimensions; no line of text holds that. The summary says which family has
// which table and how big; the files hold what is in them.

var findings = new List<string>();
var noTable = new List<string>();
var refused = new List<string>();
var summaryRows = new List<string>();
var familiesRead = 0;
var tablesFound = 0;
var scanned = 0;
var summary = "";
var leftOpen = new List<string>();

// VERSION 2, from REPORT_FAMILY_SIZE_TABLE - one table, whole.
var tableNames = "";
var columnHeaders = "";
var rows = "";
var rowCount = 0;
var columnCount = 0;
// WHERE THAT TABLE WAS WRITTEN, empty when nothing was asked for. Exporting
// writes a FILE and changes nothing in the model, which is why a read may do
// it: the CSV is the only place Revit states the header format a table must be
// imported in.
var exportedTo = "";
// That the document in front is a project, so the loaded families were read.
var notAFamily = !doc.IsFamilyDocument;

// What each text input binds to when left out is "none", read as empty.
Func<string, string> given = text =>
{
    var said = (text ?? "").Trim();
    return string.Equals(said, "none", StringComparison.OrdinalIgnoreCase) ? "" : said;
};

var needle = given(nameContains);
var folder = given(exportFolder);
if (folder.Length > 0 && !folder.EndsWith("\\") && !folder.EndsWith("/")) folder = folder + "/";
var wantedTable = given(tableName);
var wantedPath = given(exportTo);

// EVERY TABLE OF ONE FAMILY'S MANAGER, by name.
Func<FamilySizeTableManager, List<string>> namesIn = manager =>
{
    var found = new List<string>();
    try
    {
        foreach (var one in manager.GetAllSizeTableNames()) found.Add(one);
    }
    catch (Exception) { }
    return found;
};

// ONE TABLE READ WHOLE - REPORT_FAMILY_SIZE_TABLE's own read. EMPTY MEANS THE
// FIRST, because a family usually holds one and asking a person to name it
// before they can read it is a loop; a name it has not got is refused with the
// ones it has. `exportTo` writes it as CSV.
Action<FamilySizeTableManager, List<string>, string> readWhole = (manager, names, familyName) =>
{
    tableNames = string.Join(", ", names.ToArray());
    var wanted = wantedTable.Length == 0 ? names[0] : wantedTable;

    var matched = "";
    foreach (var one in names)
    {
        if (string.Equals(one, wanted, StringComparison.OrdinalIgnoreCase))
        {
            matched = one;
            break;
        }
    }

    if (matched.Length == 0)
    {
        findings.Add(string.Format("No table called '{0}' in '{1}'. It holds: {2}", wanted, familyName,
            tableNames));
        return;
    }

    FamilySizeTable table = null;
    try { table = manager.GetSizeTable(matched); }
    catch (Exception) { table = null; }

    if (table == null)
    {
        findings.Add(string.Format("The table '{0}' of '{1}' could not be read.", matched, familyName));
        return;
    }

    rowCount = table.NumberOfRows;
    columnCount = table.NumberOfColumns;

    var headers = new List<string>();
    for (var c = 0; c < columnCount; c++)
    {
        var header = "";
        try
        {
            var column = table.GetColumnHeader(c);
            if (column != null) header = column.Name;
        }
        catch (Exception) { }
        headers.Add(header.Length == 0 ? string.Format("col{0}", c) : header);
    }
    columnHeaders = string.Join(" | ", headers.ToArray());

    var lines = new List<string>();
    for (var r = 0; r < rowCount; r++)
    {
        var cells = new List<string>();
        for (var c = 0; c < columnCount; c++)
        {
            var value = "";
            try { value = table.AsValueString(r, c); }
            catch (Exception) { value = "?"; }
            cells.Add(value ?? "");
        }
        lines.Add(string.Join(" | ", cells.ToArray()));
    }
    rows = string.Join("  //  ", lines.ToArray());

    findings.Add(string.Format("Table '{0}' of '{1}': {2} row(s), {3} column(s)", matched, familyName,
        rowCount, columnCount));
    findings.Add(string.Format("Columns: {0}", columnHeaders));

    if (wantedPath.Length > 0)
    {
        try
        {
            manager.ExportSizeTable(matched, wantedPath);
            exportedTo = wantedPath;
            findings.Add(string.Format("Exported '{0}' to {1}", matched, wantedPath));
        }
        catch (Exception error)
        {
            findings.Add(string.Format("Could not export '{0}' to {1}: {2}", matched, wantedPath, error.Message));
        }
    }
};

// EVERY TABLE OF ONE FAMILY counted, and written into the folder if one was
// given - one file per table, named so a folder of them is readable without
// opening any. Gives back the summary row's tail.
Func<FamilySizeTableManager, List<string>, string, string> countAll = (manager, names, familyName) =>
{
    var parts = new List<string>();
    foreach (var oneTable in names)
    {
        var tableRows = -1;
        try
        {
            var counted = manager.GetSizeTable(oneTable);
            if (counted != null) tableRows = counted.NumberOfRows;
        }
        catch (Exception) { tableRows = -1; }

        tablesFound++;
        parts.Add(string.Format("{0}({1} rows)", oneTable, tableRows));

        if (folder.Length > 0)
        {
            var safe = familyName;
            foreach (var bad in new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' })
                safe = safe.Replace(bad, '_');
            var path = string.Format("{0}{1}__{2}.csv", folder, safe, oneTable);
            try { manager.ExportSizeTable(oneTable, path); }
            catch (Exception error)
            {
                findings.Add(string.Format("Could not export {0} / {1}: {2}", familyName, oneTable,
                    error.Message));
            }
        }
    }
    return string.Join(", ", parts.ToArray());
};

if (doc.IsFamilyDocument)
{
    // ---- THE FAMILY IN FRONT (version 2, REPORT_FAMILY_SIZE_TABLE's job) ----
    var title = doc.Title ?? "<this family>";
    scanned = 1;
    familiesRead = 1;

    FamilySizeTableManager manager = null;
    try { manager = FamilySizeTableManager.GetFamilySizeTableManager(doc, doc.OwnerFamily.Id); }
    catch (Exception) { manager = null; }

    var names = manager == null ? new List<string>() : namesIn(manager);
    if (names.Count == 0)
    {
        // A family with no table takes its dimensions from its own
        // parameters - so a size that will not appear is not a table problem
        // here, and "0 rows" would send somebody to fix the wrong thing.
        noTable.Add(title);
        summaryRows.Add(string.Format("{0}: NO TABLE", title));
        findings.Add(string.Format("'{0}' has NO lookup table at all. A family with no table takes its "
            + "dimensions from its own parameters instead - so a size that will not appear is not a table "
            + "problem here.", title));
    }
    else
    {
        summaryRows.Add(string.Format("{0}: {1}", title, countAll(manager, names, title)));
        readWhole(manager, names, title);
    }
}
else
{
    // ---- EVERY LOADED FAMILY, FROM THE PROJECT (version 1) -----------------

    // WHAT WAS ALREADY OPEN BEFORE THIS TOUCHED ANYTHING. A document this
    // fragment did NOT open is somebody's unsaved work, and closing it throws
    // that work away without a word. Measured 2026-09-16: six families were
    // open with corrected size tables not yet loaded, this read closed all six,
    // and every correction was lost. Revit does not always refuse to close a
    // window a person has open - so the refusal cannot be relied on, and the
    // fragment has to remember what it found.
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

    // ONE FAMILY NARROWED TO is read whole as well (version 2) - the answer
    // to "what is in TRG_Cap_Copper's lookup table" without opening it.
    var matching = families.Count(f => needle.Length == 0
        || (f.Name ?? "<unnamed>").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);

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

            var names = manager == null ? new List<string>() : namesIn(manager);
            if (names.Count == 0)
            {
                noTable.Add(name);
                summaryRows.Add(string.Format("{0}: NO TABLE", name));
                continue;
            }

            summaryRows.Add(string.Format("{0}: {1}", name, countAll(manager, names, name)));
            if (matching == 1) readWhole(manager, names, name);
        }
        finally
        {
            // NEVER CLOSE A DOCUMENT THIS DID NOT OPEN. See the note beside
            // `wasOpenBefore` - closing somebody's open family discards
            // whatever they had not yet loaded.
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
}

summary = string.Join("  ||  ", summaryRows.ToArray());

if (leftOpen.Count > 0)
{
    findings.Add(string.Format("{0} family(ies) were ALREADY OPEN and were left open: {1}. "
        + "Anything not yet loaded into the project is still safe in those windows.",
        leftOpen.Count, string.Join(", ", leftOpen.ToArray())));
}

findings.Insert(0, doc.IsFamilyDocument
    ? string.Format("family '{0}' in front; {1} table(s); {2} row(s) read", doc.Title, tablesFound, rowCount)
    : string.Format("{0} family(ies) read of {1} scanned; {2} table(s) found, {3} "
        + "family(ies) carry none, {4} refused to open, {5} left open as found",
        familiesRead, scanned, tablesFound, noTable.Count, refused.Count, leftOpen.Count));
