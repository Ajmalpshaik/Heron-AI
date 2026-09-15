// NOT STANDALONE. Assumes `doc`, `csvPath` and `tableName` are in scope;
// leaves `imported`, `rowsBefore`, `rowsAfter`, `tableCreated`, `problem`,
// `notAFamily` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// IT REPLACES, IT DOES NOT MERGE. Revit's import overwrites the whole table
// of that name, so a CSV holding five rows leaves a table of five rows. That
// is why `rowsBefore` is read and reported beside `rowsAfter` - a table that
// shrank is the failure worth seeing, and nothing else would show it.
//
// THE HEADER FORMAT IS REVIT'S. Each column reads `Name##type##unit`, the
// first column is the unnamed key. A header Revit cannot parse is refused,
// and the row, column and text that failed are reported rather than
// swallowed: an import that quietly does nothing looks exactly like one that
// worked.
//
// A FAMILY WITH NO MANAGER GETS ONE. A first import into a family that never
// had a lookup table is legitimate, so the manager is created rather than
// refused - and that is said out loud, because gaining a table is a bigger
// change than gaining a row.

var findings = new List<string>();
var imported = false;
var rowsBefore = 0;
var rowsAfter = 0;
var tableCreated = false;
var problem = "";
var notAFamily = false;

var wantedCsv = (csvPath ?? "").Trim();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front is a project, not a family being edited. A lookup table "
        + "belongs to the family - open the family for editing and run this again.");
}
else if (wantedCsv.Length == 0)
{
    findings.Add("No CSV was named. This imports a file over the table, so there is nothing to "
        + "default to.");
}
else
{
    // The name Revit will use is the CSV's filename unless one is given.
    var wantedTable = (tableName ?? "").Trim();
    if (wantedTable.Length == 0)
    {
        var slash = wantedCsv.LastIndexOfAny(new char[] { '\\', '/' });
        var leaf = slash >= 0 ? wantedCsv.Substring(slash + 1) : wantedCsv;
        var dot = leaf.LastIndexOf('.');
        wantedTable = dot > 0 ? leaf.Substring(0, dot) : leaf;
    }

    FamilySizeTableManager manager = null;
    try { manager = FamilySizeTableManager.GetFamilySizeTableManager(doc, doc.OwnerFamily.Id); }
    catch (Exception) { manager = null; }

    if (manager == null)
    {
        // See the header - a first import is legitimate.
        try
        {
            FamilySizeTableManager.CreateFamilySizeTableManager(doc, doc.OwnerFamily.Id);
            manager = FamilySizeTableManager.GetFamilySizeTableManager(doc, doc.OwnerFamily.Id);
            tableCreated = manager != null;
        }
        catch (Exception error)
        {
            problem = error.Message;
            findings.Add(string.Format("This family has no lookup table and one could not be "
                + "created: {0}", error.Message));
        }
    }

    if (manager != null)
    {
        // BEFORE, so a replacement that shrank the table is visible.
        try
        {
            if (manager.HasSizeTable(wantedTable))
            {
                var existing = manager.GetSizeTable(wantedTable);
                if (existing != null) rowsBefore = existing.NumberOfRows;
            }
        }
        catch (Exception) { rowsBefore = 0; }

        var errorInfo = new FamilySizeTableErrorInfo();
        var ok = false;
        try
        {
            ok = manager.ImportSizeTable(doc, wantedCsv, errorInfo);
        }
        catch (Exception error)
        {
            problem = error.Message;
            ok = false;
        }

        if (!ok)
        {
            // REVIT'S OWN WORDS, WITH WHERE IT FAILED. See the header.
            var detail = "";
            try
            {
                detail = string.Format("{0}; row {1}, column {2}, header text '{3}', file '{4}'",
                    errorInfo.FamilySizeTableErrorType, errorInfo.InvalidRowIndex,
                    errorInfo.InvalidColumnIndex, errorInfo.InvalidHeaderText, errorInfo.FilePath);
            }
            catch (Exception) { }

            if (problem.Length == 0) problem = detail;
            else if (detail.Length > 0) problem = problem + " | " + detail;

            findings.Add(string.Format("The import was REFUSED and the table is unchanged: {0}",
                problem));
        }
        else
        {
            imported = true;
            try
            {
                if (manager.HasSizeTable(wantedTable))
                {
                    var after = manager.GetSizeTable(wantedTable);
                    if (after != null) rowsAfter = after.NumberOfRows;
                }
            }
            catch (Exception) { rowsAfter = 0; }

            findings.Add(string.Format("'{0}' imported from {1}: {2} row(s) before, {3} after.",
                wantedTable, wantedCsv, rowsBefore, rowsAfter));

            // THE IMPORT REPLACES. A table that shrank is not automatically
            // wrong - it is only wrong if nobody meant it - so it is named
            // rather than refused.
            if (rowsBefore > rowsAfter)
            {
                findings.Add(string.Format("The table SHRANK from {0} row(s) to {1}. An import "
                    + "replaces rather than merges, so any row not in the CSV is gone - check "
                    + "that was meant.", rowsBefore, rowsAfter));
            }

            if (tableCreated)
            {
                findings.Add("This family had NO lookup table before and now has one. That is a "
                    + "bigger change than a new row - the family now looks its dimensions up "
                    + "rather than taking them from its own parameters.");
            }
        }
    }
}

findings.Insert(0, string.Format("{0}; {1} row(s) before, {2} after",
    imported ? "imported" : "NOTHING was imported", rowsBefore, rowsAfter));
