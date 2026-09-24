// NOT STANDALONE. Assumes `doc`, `elements`, `csvPath` and
// `includeTypeParameters` are in scope; leaves `findings`, `valuesWritten`,
// `rowsMatched`, `rowsUnmatched` and `valuesRejected` behind.
//
// IT WRITES, AND IT OPENS NO TRANSACTION. Golden Rule 16: a fragment assumes one
// is open. A partial import is undone by the caller's own rollback, with
// everything else in the same operation - which is what Ctrl+Z is expected to do.
//
// THE ID IS MATCHED AS TEXT. No ElementId is built from a number: ElementId(int)
// still exists at 2027 and ElementId(long) does not exist before 2024, so
// constructing one means picking a side. Every element prints its own id, so the
// lookup is element.Id.ToString() against the cell.
//
// SCOPED TO THE ELEMENTS HANDED IN, not to the whole document. A stale file
// cannot then write somewhere nobody was looking, and a row for a different
// model is reported rather than silently applied.
//
// IT READS BACK WHAT LANDED. Revit accepts a value and then formats, rounds or
// rejects it more often than people expect, so every write is re-read and
// anything that came back different is counted separately. A tally of Set calls
// returning true is not a tally of values now in the model.
//
// THREE KINDS OF SKIP, COUNTED APART: read-only target, unknown parameter name,
// and a value Revit would not take. Three different fixes.

var findings = new List<string>();
var valuesWritten = 0;
var rowsMatched = 0;
var rowsUnmatched = 0;
var valuesRejected = 0;

// Honours the quoting a spreadsheet export writes: "quoted, fields" and a
// doubled "" for a literal quote inside one.
Func<string, List<string>> splitRow = line =>
{
    var cells = new List<string>();
    var current = new System.Text.StringBuilder();
    var inQuotes = false;

    for (var i = 0; i < line.Length; i++)
    {
        var ch = line[i];
        if (inQuotes)
        {
            if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
            else if (ch == '"') inQuotes = false;
            else current.Append(ch);
        }
        else if (ch == '"') inQuotes = true;
        else if (ch == ',') { cells.Add(current.ToString()); current.Length = 0; }
        else current.Append(ch);
    }

    cells.Add(current.ToString());
    return cells;
};

// A CSV RECORD IS NOT A LINE. Alt+Enter inside an Excel cell is written out as
// a real newline INSIDE the quotes, so reading the file line by line tears one
// parameter value into two rows: the first row gets half the text and the
// second is rejected as having the wrong number of cells. A note typed on
// three lines came back as one line and a complaint.
//
// So records are split here, honouring the same quoting splitRow honours, and
// splitRow is left to do the cells - it already keeps a newline that arrives
// inside a quoted field. Nothing is unescaped at this level: a doubled "" is
// copied through as both characters so splitRow still sees what the file said.
var quotingIsBroken = false;
Func<string, List<string>> splitRecords = text =>
{
    var records = new List<string>();
    var current = new System.Text.StringBuilder();
    var inQuotes = false;

    for (var i = 0; i < text.Length; i++)
    {
        var ch = text[i];
        if (ch == '"')
        {
            if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
            {
                current.Append('"').Append('"');
                i++;
                continue;
            }
            inQuotes = !inQuotes;
            current.Append(ch);
        }
        else if (!inQuotes && (ch == '\n' || ch == '\r'))
        {
            if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            records.Add(current.ToString());
            current.Length = 0;
        }
        else current.Append(ch);
    }

    records.Add(current.ToString());
    quotingIsBroken = inQuotes;   // a quote opened and never closed
    return records;
};

var byId = new Dictionary<string, Element>();
foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;
    byId[element.Id.ToString()] = element;
}

if (string.IsNullOrEmpty(csvPath) || !System.IO.File.Exists(csvPath))
{
    findings.Add("There is no file at '" + (csvPath ?? "") + "', so NOTHING was written");
}
else if (byId.Count == 0)
{
    findings.Add("No elements were handed in, so no row could match anything and NOTHING was written. "
        + "This fragment writes only to the set in front of you - run the filter first");
}
else
{
    string fileText = null;
    try { fileText = System.IO.File.ReadAllText(csvPath); }
    catch (Exception ex)
    {
        findings.Add("That file could not be read: " + ex.Message + ". Nothing was written");
    }

    var rows = new List<string>();
    if (fileText != null)
    {
        foreach (var record in splitRecords(fileText))
        {
            if (!string.IsNullOrEmpty(record) && record.Trim().Length > 0) rows.Add(record);
        }
    }

    if (fileText != null && quotingIsBroken)
    {
        findings.Add("'" + csvPath + "' has a quotation mark that is opened and never closed, so the "
            + "rows after it cannot be read as the file meant them. NOTHING was written. Look for a "
            + "stray \" in one of the values");
    }
    else if (fileText != null && rows.Count < 2)
    {
        findings.Add("'" + csvPath + "' has a header and no data rows, so nothing was written");
    }
    else if (fileText != null)
    {
        var header = splitRow(rows[0]);
        if (header.Count < 2)
        {
            findings.Add("The header has no parameter columns - the first column identifies the "
                + "element and every column after it is a parameter name. Nothing was written");
        }
        else
        {
            var readOnlySkips = 0;
            var unknownParameter = 0;
            var ambiguousName = 0;
            var refusedValue = 0;
            var problems = new List<string>();

            for (var r = 1; r < rows.Count; r++)
            {
                var cells = splitRow(rows[r]);
                var idCell = cells.Count > 0 ? cells[0].Trim() : "";

                Element element = null;
                if (!byId.TryGetValue(idCell, out element) || element == null)
                {
                    rowsUnmatched++;
                    if (problems.Count < 25)
                    {
                        problems.Add("row " + (r + 1) + ": '" + idCell
                            + "' is not one of the elements handed in");
                    }
                    continue;
                }

                rowsMatched++;

                for (var c = 1; c < header.Count && c < cells.Count; c++)
                {
                    var parameterName = header[c].Trim();
                    if (parameterName.Length == 0) continue;
                    var wanted = cells[c];

                    // A COLUMN NAME TWO PARAMETERS SHARE IS NOT WRITTEN. A shared or project
                    // parameter can be bound beside a built-in one of the same name, and
                    // LookupParameter then returns one of them - "determined at random", in
                    // Autodesk's own reference - so a file would fill whichever it hit, and the
                    // read-back below would agree with itself. Checked on the instance, and on
                    // the type when the search reaches it (D-54 s3, FRAGMENT-ISSUES 5b-203).
                    var twice = element.GetParameters(parameterName).Count > 1;
                    var parameter = twice ? null : element.LookupParameter(parameterName);
                    var target = element;
                    if (!twice && parameter == null && includeTypeParameters)
                    {
                        var type = doc.GetElement(element.GetTypeId()) as ElementType;
                        if (type != null)
                        {
                            if (type.GetParameters(parameterName).Count > 1)
                            {
                                twice = true;
                            }
                            else
                            {
                                var typeParameter = type.LookupParameter(parameterName);
                                if (typeParameter != null) { parameter = typeParameter; target = type; }
                            }
                        }
                    }

                    if (twice)
                    {
                        ambiguousName++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": '" + parameterName + "' is the name of "
                                + "two or more parameters on id " + element.Id + " - Revit would pick "
                                + "one of them at random, so neither was written");
                        }
                        continue;
                    }

                    if (parameter == null)
                    {
                        unknownParameter++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": no parameter called '" + parameterName
                                + "' on id " + element.Id + " - fix the header, or the category has "
                                + "not got that parameter at all");
                        }
                        continue;
                    }

                    if (parameter.IsReadOnly)
                    {
                        readOnlySkips++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": '" + parameterName
                                + "' is read-only on id " + element.Id
                                + " - Revit computes it, so it is not a target");
                        }
                        continue;
                    }

                    var accepted = false;
                    try
                    {
                        if (parameter.StorageType == StorageType.String)
                        {
                            accepted = parameter.Set(wanted);
                        }
                        else if (parameter.StorageType == StorageType.Integer)
                        {
                            var whole = 0;
                            accepted = int.TryParse(wanted, out whole) && parameter.Set(whole);
                        }
                        else
                        {
                            // Doubles and ElementId-backed parameters both go through Revit's own
                            // parsing in the project's display units. No units API is named, and a
                            // cell that read "450" on the way out means the same on the way back.
                            accepted = parameter.SetValueString(wanted);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (problems.Count < 25)
                            problems.Add("row " + (r + 1) + ": '" + parameterName + "' - " + ex.Message);
                    }

                    if (!accepted)
                    {
                        refusedValue++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": Revit would not take '" + wanted
                                + "' for '" + parameterName + "' on id " + element.Id);
                        }
                        continue;
                    }

                    // READ BACK. Accepted is not landed.
                    var landed = parameter.AsString();
                    if (string.IsNullOrEmpty(landed)) landed = parameter.AsValueString();
                    if (landed == null) landed = "";

                    if (landed.Trim() != wanted.Trim())
                    {
                        valuesRejected++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": '" + parameterName + "' was set to '"
                                + wanted + "' and READS BACK as '" + landed
                                + "' - Revit reformatted or rounded it");
                        }
                    }

                    valuesWritten++;
                }
            }

            foreach (var problem in problems) findings.Add("  " + problem);
            if (problems.Count >= 25)
                findings.Add("  ... further detail not listed. The counts above are complete");

            findings.Add("Skipped: " + unknownParameter + " because the parameter is not there, "
                + ambiguousName + " because the column's name belongs to two or more parameters, "
                + readOnlySkips + " because it is read-only, " + refusedValue
                + " because Revit would not take the value. Those are four different fixes and are "
                + "counted apart on purpose");
        }
    }
}

findings.Insert(0, valuesWritten + " value(s) written across " + rowsMatched + " matched row(s)"
    + (rowsUnmatched > 0
        ? ", " + rowsUnmatched + " row(s) matched NO element in the set handed in - check the file is "
            + "for this model and that the filter above covered it"
        : "")
    + (valuesRejected > 0
        ? ". " + valuesRejected + " of the values written READ BACK DIFFERENT from what was asked for, "
            + "listed below - Revit reformatted or rounded them"
        : ". Every value written reads back as it was asked for")
    + ". No transaction was opened here: undo is the caller's, and covers this with everything else in "
    + "the same operation");
