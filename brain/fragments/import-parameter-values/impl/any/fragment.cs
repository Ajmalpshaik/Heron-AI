// NOT STANDALONE. Assumes `doc`, `elements`, `csvPath`,
// `includeTypeParameters` and `blankCells` are in scope; leaves `findings`,
// `valuesWritten`, `rowsMatched`, `rowsUnmatched`, `valuesRejected`,
// `blankCellsSkipped` and `cellsCleared` behind.
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
//
// ===========================================================================
// VERSION 3: A YES/NO COLUMN COMES BACK, AND A BLANK CELL IS ASKED ABOUT.
// ===========================================================================
//
// A YES/NO PARAMETER IS A TICK BOX, NOT A NUMBER THAT PARSES (FRAGMENT-ISSUES
// 5b-315). Revit stores it as the Integer 1 or 0, and the parameter export
// writes it as the text Revit shows - "Yes" or "No" - so version 2's
// int.TryParse refused every cell of a file this library had just written. A
// Yes/No parameter is now found first, the way WRITE_ELEMENT_PARAMETERS
// version 4 finds one: the kind is read by reflection, `Definition.GetDataType`
// against `SpecTypeId.Boolean.YesNo` where the running Revit has them (2022
// on) and `Definition.ParameterType` where it has that instead (2020 to 2022).
// The add-in compiles fragments with no release symbols, so an `#if` would
// take the same branch everywhere. Yes/No, True/False, On/Off and 1/0 in any
// case are taken; anything else is refused, counted apart and named. The
// read-back for a tick box is `AsInteger` against the 1 or 0 asked for - the
// displayed word is the UI language's, so comparing words could call a good
// write different.
//
// A BLANK CELL USED TO CLEAR TEXT SILENTLY (5b-316). Version 2 called Set("")
// on a text parameter for an empty cell - wiping a value somebody typed,
// without a word - and refused the same blank for a number. Which one a blank
// means is the modeller's to say, so `blankCells` is ASKED, with no default:
// "skip" leaves the element exactly as it is and counts the cell in
// `blankCellsSkipped`; "clear" empties a TEXT parameter, counts it in
// `cellsCleared`, and refuses a blank for a number, a Yes/No or an element
// reference with the reason, because those have no empty value a file can set.
// Any other word, or none, refuses the run and NOTHING is written.

var findings = new List<string>();
var valuesWritten = 0;
var rowsMatched = 0;
var rowsUnmatched = 0;
var valuesRejected = 0;
var blankCellsSkipped = 0;
var cellsCleared = 0;

// Asked, never assumed. Null for a word that is neither.
var blankWord = (blankCells ?? "").Trim().ToLowerInvariant();
var blankMode = blankWord == "skip" || blankWord == "clear" ? blankWord : null;

// SpecTypeId.Boolean.YesNo on 2022 and later; null on 2020 and 2021, which
// answer through ParameterType instead. The same lookup WRITE_ELEMENT_PARAMETERS
// version 4 makes.
var yesNoSpecs = typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Boolean");
var yesNoProperty = yesNoSpecs == null ? null : yesNoSpecs.GetProperty("YesNo",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var yesNoSpec = yesNoProperty == null ? null : yesNoProperty.GetValue(null, null);

Func<Parameter, bool> isYesNo = p =>
{
    if (p.StorageType != StorageType.Integer) return false;
    try
    {
        var definition = p.Definition;
        if (definition == null) return false;

        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null && yesNoSpec != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return false;
            // NameEquals, because a spec id carries its version and a parameter
            // made under another version must still read as a tick box.
            var nameEquals = spec.GetType().GetMethod("NameEquals", new[] { yesNoSpec.GetType() });
            return nameEquals != null
                ? (bool)nameEquals.Invoke(spec, new[] { yesNoSpec })
                : spec.Equals(yesNoSpec);
        }

        var parameterType = definition.GetType().GetProperty("ParameterType");
        var kind = parameterType == null ? null : parameterType.GetValue(definition, null);
        return kind != null && kind.ToString() == "YesNo";
    }
    catch (Exception)
    {
        // Not known to be a tick box, so it takes the plain Integer road, where
        // a word is refused rather than written wrongly.
        return false;
    }
};

// What a cell means for a tick box: 1, 0, or null for "not a Yes/No word".
var tickedWords = new[] { "yes", "true", "on", "1" };
var untickedWords = new[] { "no", "false", "off", "0" };
Func<string, int?> tickOf = text =>
{
    var said = (text ?? "").Trim().ToLowerInvariant();
    return Array.IndexOf(tickedWords, said) >= 0 ? 1
        : Array.IndexOf(untickedWords, said) >= 0 ? 0
        : (int?)null;
};

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

if (blankMode == null)
{
    findings.Add("What a blank cell means was not said" + (blankWord.Length > 0 ? " ('" + blankCells
        + "' is neither word)" : "") + ", so NOTHING was written. Say blankCells 'skip' to leave an "
        + "element's value as it is wherever its cell is empty, or 'clear' to empty a text parameter "
        + "wherever its cell is empty - Heron does not choose for you, because one of the two deletes "
        + "what somebody typed");
}
else if (string.IsNullOrEmpty(csvPath) || !System.IO.File.Exists(csvPath))
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
            var notAYesNoWord = 0;
            var blankNotClearable = 0;
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
                    var blank = wanted.Trim().Length == 0;

                    // "skip": an empty cell asks for nothing, so nothing on this
                    // element is looked up or touched.
                    if (blank && blankMode == "skip")
                    {
                        blankCellsSkipped++;
                        continue;
                    }

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

                    // "clear" empties TEXT only. A number, a tick box or an element
                    // reference has no empty value a file can set, so the blank is
                    // refused with that reason rather than read as a bad number.
                    if (blank && parameter.StorageType != StorageType.String)
                    {
                        blankNotClearable++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": '" + parameterName + "' on id " + element.Id
                                + " is not text, so a blank cell cannot clear it - it was left as it was");
                        }
                        continue;
                    }

                    var yesNo = isYesNo(parameter);
                    int? tick = yesNo ? tickOf(wanted) : null;
                    if (yesNo && tick == null)
                    {
                        notAYesNoWord++;
                        if (problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": '" + wanted + "' is not a Yes/No value, and '"
                                + parameterName + "' on id " + element.Id + " is a Yes/No parameter - "
                                + "write Yes or No (True/False, On/Off and 1/0 are taken too). Left as it was");
                        }
                        continue;
                    }

                    var accepted = false;
                    try
                    {
                        if (yesNo)
                        {
                            accepted = parameter.Set(tick.Value);
                        }
                        else if (parameter.StorageType == StorageType.String)
                        {
                            accepted = parameter.Set(blank ? "" : wanted);
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

                    // READ BACK. Accepted is not landed. A tick box is read as its 1 or
                    // 0, not as the word Revit displays in this UI language.
                    string landed;
                    bool same;
                    if (yesNo)
                    {
                        var got = parameter.AsInteger();
                        landed = got.ToString();
                        same = got == tick.Value;
                    }
                    else
                    {
                        landed = parameter.AsString();
                        if (string.IsNullOrEmpty(landed)) landed = parameter.AsValueString();
                        if (landed == null) landed = "";
                        same = landed.Trim() == wanted.Trim();
                    }

                    if (!same)
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
                    if (blank) cellsCleared++;
                }
            }

            foreach (var problem in problems) findings.Add("  " + problem);
            if (problems.Count >= 25)
                findings.Add("  ... further detail not listed. The counts above are complete");

            findings.Add("Skipped: " + unknownParameter + " because the parameter is not there, "
                + ambiguousName + " because the column's name belongs to two or more parameters, "
                + readOnlySkips + " because it is read-only, " + refusedValue
                + " because Revit would not take the value, " + notAYesNoWord
                + " because a Yes/No parameter was given a word that is not Yes or No, " + blankNotClearable
                + " because the cell was blank and the parameter is not text. Those are six different "
                + "fixes and are counted apart on purpose");
            findings.Add(blankMode == "skip"
                ? blankCellsSkipped + " blank cell(s) skipped - those elements keep the value they had"
                : cellsCleared + " text value(s) CLEARED because their cell was blank, as asked");
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
