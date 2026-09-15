// NOT STANDALONE. Assumes `doc`, `tableName` and `exportTo` are in scope; leaves
// `tableNames`, `columnHeaders`, `rows`, `rowCount`, `columnCount`,
// `notAFamily` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A SIZE MISSING FROM THE LOOKUP TABLE IS SILENT. A parametric fitting looks
// its dimensions up by the size it is asked for; ask for a size with no row
// and it cannot resolve, and Revit says nothing. A new segment with new sizes
// is exactly how a table falls out of date.
//
// IT NEEDS THE FAMILY OPEN FOR EDITING. A size table belongs to the family
// document. Reporting an empty table from a project would blame the family
// for the reader looking in the wrong place, so that case is named instead.
//
// EVERY ROW COMES BACK AS ONE STRING. A list is abbreviated to three entries
// before a reader sees it, and the whole table is the answer here.
//
// VALUES ARE REVIT'S OWN STRINGS. AsValueString is what the family itself
// reads; re-deriving a number would invent a second opinion about a value the
// family has already settled.

var findings = new List<string>();
var tableNames = "";
var columnHeaders = "";
var rows = "";
var rowCount = 0;
var columnCount = 0;
var notAFamily = false;
// WHERE IT WAS WRITTEN, empty when nothing was asked for. Exporting writes a
// FILE and changes nothing in the model, which is why a read may do it: the
// CSV is the only place Revit states the header format a table must be
// imported in, and guessing that format is how an import silently produces a
// table nobody can look anything up in.
var exportedTo = "";

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front is a project, not a family being edited. A size table "
        + "belongs to the family - open the family for editing and run this again.");
}
else
{
    FamilySizeTableManager manager = null;
    try
    {
        manager = FamilySizeTableManager.GetFamilySizeTableManager(doc, doc.OwnerFamily.Id);
    }
    catch (Exception) { manager = null; }

    if (manager == null)
    {
        findings.Add(string.Format("'{0}' has NO lookup table at all. A family with no table "
            + "takes its dimensions from its own parameters instead - so a size that will not "
            + "appear is not a table problem here.", doc.Title));
    }
    else
    {
        var names = new List<string>();
        try
        {
            foreach (var name in manager.GetAllSizeTableNames()) names.Add(name);
        }
        catch (Exception) { }

        tableNames = string.Join(", ", names.ToArray());

        if (names.Count == 0)
        {
            findings.Add(string.Format("'{0}' has a size table manager but no tables in it.",
                doc.Title));
        }
        else
        {
            // EMPTY MEANS THE FIRST, because a family usually holds one and
            // asking a person to name it before they can read it is a loop.
            var wanted = (tableName ?? "").Trim();
            if (wanted.Length == 0) wanted = names[0];

            var matched = "";
            foreach (var name in names)
            {
                if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    matched = name;
                    break;
                }
            }

            if (matched.Length == 0)
            {
                findings.Add(string.Format("No table called '{0}' in this family. It holds: {1}",
                    wanted, tableNames));
            }
            else
            {
                FamilySizeTable table = null;
                try { table = manager.GetSizeTable(matched); }
                catch (Exception) { table = null; }

                if (table == null)
                {
                    findings.Add(string.Format("The table '{0}' could not be read.", matched));
                }
                else
                {
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

                    findings.Add(string.Format("Table '{0}': {1} row(s), {2} column(s)", matched,
                        rowCount, columnCount));
                    findings.Add(string.Format("Columns: {0}", columnHeaders));

                    // See the note beside `exportedTo`.
                    var wantedPath = (exportTo ?? "").Trim();
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
                            findings.Add(string.Format("Could not export '{0}' to {1}: {2}",
                                matched, wantedPath, error.Message));
                        }
                    }
                }
            }
        }
    }
}

findings.Insert(0, string.Format("{0}; {1} row(s) read",
    notAFamily ? "NOT a family document" : string.Format("family '{0}'", doc.Title), rowCount));
