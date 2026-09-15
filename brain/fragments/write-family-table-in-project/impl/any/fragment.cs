// NOT STANDALONE. Assumes `doc`, `familyName`, `csvPath` and `tableName` are
// in scope; leaves `imported`, `reloaded`, `rowsBefore`, `rowsAfter`,
// `leftOpen`, `problem` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION ON THE PROJECT and does not open one there
// (Golden Rule 16), so the reload is one undo entry.
//
// IT DOES OPEN A TRANSACTION ON THE FAMILY DOCUMENT, and that is not a breach
// of that rule. The rule keeps a fragment out of the transaction the EXECUTOR
// manages, on the document the executor handed it. The family document opened
// here is a second document the executor knows nothing about - nobody else
// can commit it, and leaving it uncommitted would throw the import away.
//
// A FAMILY OPEN IN THE UI CANNOT BE CLOSED, and that is a feature: a session
// somebody is working in is never discarded by this. The refusal is caught
// and REPORTED, because "left open" is a fact the next fragment depends on -
// it will find that family in front.
//
// LOADING BACK IS THE STEP THAT MAKES IT REAL. An edited family document
// changes nothing in the project until it is loaded. Reporting `imported`
// without `reloaded` would be reporting success over a project that had not
// changed.

// RELOADING NEEDS AN OVERWRITE ANSWER, AND A FRAGMENT CANNOT GIVE ONE.
// LoadFamily refuses a family that is already loaded unless it is handed an
// IFamilyLoadOptions - and a fragment CANNOT declare a type, because the
// executor compiles it as a method body. LOAD_FAMILY's own header settles
// where that belongs: "it belongs in the executor (D-28) as a supplied
// helper, not in a fragment ... do not solve it by making fragments able to
// declare types; that changes what a fragment IS."
//
// So this imports, and reports honestly that the project is unchanged until
// the family is loaded back. With the family OPEN IN REVIT the import lands
// in that session and a person loads it in; that is the working route today.
// The automatic one needs the executor to supply the helper, which is add-in
// C# and a Revit restart.

var findings = new List<string>();
var imported = false;
var reloaded = false;
var rowsBefore = 0;
var rowsAfter = 0;
var leftOpen = false;
var problem = "";

var wantedFamily = (familyName ?? "").Trim();
var wantedCsv = (csvPath ?? "").Trim();

if (wantedFamily.Length == 0 || wantedCsv.Length == 0)
{
    findings.Add("This needs BOTH a family name and a CSV path - there is no default for either.");
}
else
{
    Family family = null;
    try
    {
        foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(Family)).ToElements())
        {
            var candidate = found as Family;
            if (candidate == null || candidate.Name == null) continue;
            if (string.Equals(candidate.Name, wantedFamily, StringComparison.OrdinalIgnoreCase))
            {
                family = candidate;
                break;
            }
        }
    }
    catch (Exception) { }

    if (family == null)
    {
        findings.Add(string.Format("No loaded family is called '{0}'.", wantedFamily));
    }
    else
    {
        Document familyDocument = null;
        try { familyDocument = doc.EditFamily(family); }
        catch (Exception error)
        {
            problem = error.Message;
            findings.Add(string.Format("'{0}' could not be opened for editing: {1}",
                wantedFamily, error.Message));
        }

        if (familyDocument != null)
        {
            try
            {
                var wantedTable = (tableName ?? "").Trim();
                if (wantedTable.Length == 0)
                    wantedTable = System.IO.Path.GetFileNameWithoutExtension(wantedCsv);

                FamilySizeTableManager manager = null;
                try
                {
                    manager = FamilySizeTableManager.GetFamilySizeTableManager(
                        familyDocument, familyDocument.OwnerFamily.Id);
                }
                catch (Exception) { manager = null; }

                // THE IMPORTED TABLE IS NAMED AFTER THE FILE, NOT AFTER THE
                // ARGUMENT. Revit takes the table's name from the CSV's
                // filename, so a file called anything else quietly ADDS a
                // second table and leaves the real one untouched - which
                // reads as an import that worked and changed nothing. Found
                // by hitting it: 9 rows before, 9 after, imported true.
                var fileStem = System.IO.Path.GetFileNameWithoutExtension(wantedCsv);

                if (!string.Equals(fileStem, wantedTable, StringComparison.OrdinalIgnoreCase))
                {
                    problem = string.Format("The CSV is called '{0}' but the table is '{1}'. "
                        + "Revit names an imported table after the FILE, so this would add a "
                        + "second table and leave '{1}' untouched. Rename the file to '{1}.csv'.",
                        fileStem, wantedTable);
                    findings.Add(problem);
                }
                else if (manager == null)
                {
                    findings.Add(string.Format("'{0}' has no lookup table manager, so there is "
                        + "no table to write over.", wantedFamily));
                }
                else
                {
                    try
                    {
                        if (manager.HasSizeTable(wantedTable))
                        {
                            var existing = manager.GetSizeTable(wantedTable);
                            if (existing != null) rowsBefore = existing.NumberOfRows;
                        }
                    }
                    catch (Exception) { rowsBefore = 0; }

                    // See the header - this transaction is on the FAMILY
                    // document, which the executor does not manage.
                    var transaction = new Transaction(familyDocument, "Heron: import size table");
                    try
                    {
                        transaction.Start();
                        var errorInfo = new FamilySizeTableErrorInfo();
                        var ok = manager.ImportSizeTable(familyDocument, wantedCsv, errorInfo);

                        if (!ok)
                        {
                            try
                            {
                                problem = string.Format("{0}; row {1}, column {2}, header '{3}'",
                                    errorInfo.FamilySizeTableErrorType, errorInfo.InvalidRowIndex,
                                    errorInfo.InvalidColumnIndex, errorInfo.InvalidHeaderText);
                            }
                            catch (Exception) { }
                            transaction.RollBack();
                            findings.Add(string.Format("The import into '{0}' was REFUSED and "
                                + "nothing changed: {1}", wantedFamily, problem));
                        }
                        else
                        {
                            transaction.Commit();
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
                        }
                    }
                    catch (Exception error)
                    {
                        problem = error.Message;
                        try { if (transaction.HasStarted()) transaction.RollBack(); }
                        catch (Exception) { }
                        findings.Add(string.Format("The import into '{0}' threw: {1}",
                            wantedFamily, error.Message));
                    }

                    // LOADING BACK IS THE STEP THAT MAKES IT REAL. See header.
                    if (imported)
                    {
                        try
                        {
                            var back = familyDocument.LoadFamily(doc);
                            reloaded = back != null;
                        }
                        catch (Exception error)
                        {
                            problem = (problem.Length > 0 ? problem + " | " : "") + error.Message;
                            findings.Add(string.Format("'{0}' was imported but could NOT be "
                                + "loaded back into the project, so the project is unchanged: {1}",
                                wantedFamily, error.Message));
                        }
                    }
                }
            }
            finally
            {
                // A family open in the UI refuses to close. See the header.
                try { familyDocument.Close(false); }
                catch (Exception)
                {
                    leftOpen = true;
                }
            }
        }
    }
}

if (imported && reloaded)
{
    findings.Add(string.Format("'{0}': {1} row(s) before, {2} after, and the family was loaded "
        + "back into the project.", wantedFamily, rowsBefore, rowsAfter));
    if (rowsBefore > rowsAfter)
    {
        findings.Add(string.Format("The table SHRANK from {0} row(s) to {1}. An import replaces "
            + "rather than merges, so any row not in the CSV is gone.", rowsBefore, rowsAfter));
    }
}

if (leftOpen)
{
    findings.Add(string.Format("'{0}' is open in Revit's own window, so it was left open rather "
        + "than closed - your editing session was not discarded. It will be the document in "
        + "front for whatever runs next.", wantedFamily));
}

findings.Insert(0, string.Format("{0}; {1}; {2} row(s) before, {3} after",
    imported ? "imported" : "NOTHING imported",
    reloaded ? "reloaded into the project" : "NOT reloaded",
    rowsBefore, rowsAfter));
