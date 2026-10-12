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
//
// ===========================================================================
// VERSION 4: READ BACK AFTER ONE REGENERATION, AND SAY THE REASON IN FULL.
// ===========================================================================
//
// A CORRECT TICK BOX WAS CALLED REJECTED (FRAGMENT-ISSUES 5b-439). Revit 2020,
// three Mechanical Equipment instances, Show_Clearance written No: valuesWritten
// 3, valuesRejected 3, and a separate read straight afterwards found all three
// at 0, as asked. Version 3 read each value back the moment it was set, inside
// the open transaction and before Revit had regenerated anything; the tool it
// says it copies, WRITE_ELEMENT_PARAMETERS version 4, believes a tick box and a
// number only after `doc.Regenerate()` and a fresh lookup of the parameter.
// Every read-back now waits for ONE regeneration after the last write, and
// `changedOnRegeneration` counts the values whose read changed across it - the
// number that says whether the early read was what went wrong.
//
// The three nulls in that reply - yesNoSpecs, yesNoProperty, yesNoSpec - were
// RIGHT for 2020, not a failed lookup: SpecTypeId does not exist before 2022, and
// 2020 answers through Definition.ParameterType, checked in the 2020 to 2022
// assemblies. `yesNoRoute` now names the route that answers, so nobody has to
// infer it from three nulls again.
//
// THE REASON WAS CUT AT 60 CHARACTERS. A list is shown as its first three items,
// sixty characters each, and "was set to 'No' and READS BACK as" filled exactly
// sixty - so the value that disagreed never reached the chat. `rowFindings`
// carries every row's line as ONE string, which is shown whole.

var findings = new List<string>();
var valuesWritten = 0;
var rowsMatched = 0;
var rowsUnmatched = 0;
var valuesRejected = 0;
var blankCellsSkipped = 0;
var cellsCleared = 0;
var changedOnRegeneration = 0;
var rowFindings = "";

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

// Which of the two answers on this Revit: "GetDataType" from 2022, "ParameterType"
// on 2020 and 2021, "none" if a release ever has neither - and then no column is
// recognised as a tick box, which the findings say.
var yesNoRoute = yesNoSpec != null && typeof(Definition).GetMethod("GetDataType", System.Type.EmptyTypes) != null
    ? "GetDataType"
    : typeof(Definition).GetProperty("ParameterType") != null ? "ParameterType" : "none";

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

// What a parameter holds now, as the read-back compares it. A tick box is its
// 1 or 0 - the displayed word is the UI language's - and is shown with the word
// beside it so a person reading the finding need not decode it.
// Never throws: a value Revit will not print is said, not allowed to end the run.
Func<Parameter, bool, string> readOf = (p, tickBox) =>
{
    try
    {
        if (tickBox)
        {
            var shown = p.AsValueString();
            return p.AsInteger() + (string.IsNullOrEmpty(shown) ? "" : " (" + shown + ")");
        }
        var text = p.AsString();
        if (string.IsNullOrEmpty(text)) text = p.AsValueString();
        return text ?? "";
    }
    catch (Exception ex)
    {
        return "(could not be read: " + ex.Message + ")";
    }
};

// EVERY WRITE WAITS HERE FOR ITS READ-BACK, which happens once, after one
// regeneration (version 4). Item1 the element or type written, Item2 the
// parameter name, Item3 the row, Item4 the cell as written, Item5 the tick asked
// for (null unless a tick box), Item6 what it read straight after the write.
// Keyed by element and name, so a later row writing the same parameter on the
// same element replaces the earlier one - the model holds the later value.
var pending = new List<Tuple<Element, string, int, string, int?, string>>();
var pendingAt = new Dictionary<string, int>();

// THE READ-BACK'S OWN LINES, kept apart from the write-phase lines and listed
// first: they are what this version exists to show, and a file of many
// write-phase notes must not crowd them out of the 25 a reply can carry.
var readBack = new List<string>();

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

                    // READ BACK LATER. Accepted is not landed, and what landed is
                    // read after Revit has regenerated - below, once, for every
                    // write. What it reads now is kept only to say whether the
                    // regeneration changed it.
                    var key = target.Id + "|" + parameterName;
                    var entry = Tuple.Create(target, parameterName, r + 1, wanted, tick, readOf(parameter, yesNo));
                    int earlier;
                    if (pendingAt.TryGetValue(key, out earlier))
                    {
                        // ONLY THE LATER VALUE STANDS, so only it is counted and read
                        // back. A schedule repeating a type value on every instance row
                        // writes the same thing again - not a problem, and not said.
                        var before = pending[earlier];
                        valuesWritten--;
                        if (before.Item4.Trim().Length == 0) cellsCleared--;
                        var differs = tick.HasValue ? before.Item5 != tick : before.Item4.Trim() != wanted.Trim();
                        if (differs && problems.Count < 25)
                        {
                            problems.Add("row " + (r + 1) + ": '" + parameterName + "' on id " + target.Id
                                + (before.Item3 == r + 1
                                    ? " is named by two columns of this row"
                                    : " was also written by row " + before.Item3)
                                + " with a different value ('" + before.Item4 + "') - '" + wanted
                                + "' is the one the model keeps");
                        }
                        pending[earlier] = entry;
                    }
                    else
                    {
                        pendingAt[key] = pending.Count;
                        pending.Add(entry);
                    }

                    valuesWritten++;
                    if (blank) cellsCleared++;
                }
            }

            // ONE REGENERATION, THEN EVERY READ-BACK (version 4, 5b-439). The
            // parameter is looked up again rather than trusted from before the
            // regeneration, the way WRITE_ELEMENT_PARAMETERS version 4 does.
            var regenerated = false;
            if (pending.Count > 0)
            {
                try { doc.Regenerate(); regenerated = true; }
                catch (Exception ex)
                {
                    findings.Add("Revit would not regenerate before the read-back (" + ex.Message
                        + "), so every value below was read without it");
                }
            }

            foreach (var written in pending)
            {
                var tickBox = written.Item5.HasValue;
                var stillThere = written.Item1.IsValidObject;
                // Counted again, as at the write: a name two parameters share is
                // never read from whichever Revit returns first (5b-203).
                var named = 0;
                Parameter again = null;
                try
                {
                    named = stillThere ? written.Item1.GetParameters(written.Item2).Count : 0;
                    again = named == 1 ? written.Item1.LookupParameter(written.Item2) : null;
                }
                catch (Exception)
                {
                    again = null;   // said below as NOT KNOWN, never as a value
                }
                var onId = stillThere ? "id " + written.Item1.Id : "an element no longer in the model";
                if (again == null)
                {
                    valuesRejected++;
                    if (readBack.Count < 25)
                    {
                        readBack.Add("row " + written.Item3 + ": '" + written.Item2 + "' on " + onId
                            + (named > 1 ? " now names " + named + " parameters" : " could not be found again")
                            + " after the write, so what landed is NOT KNOWN");
                    }
                    continue;
                }

                var landed = readOf(again, tickBox);
                if (landed != written.Item6) changedOnRegeneration++;

                int? tickNow = null;
                if (tickBox)
                {
                    try { tickNow = again.AsInteger(); } catch (Exception) { tickNow = null; }
                }
                var same = tickBox
                    ? tickNow == written.Item5
                    : landed.Trim() == written.Item4.Trim();
                if (same) continue;

                valuesRejected++;
                if (readBack.Count < 25)
                {
                    readBack.Add("row " + written.Item3 + ": '" + written.Item2 + "' on " + onId
                        + " was set to '" + written.Item4 + "' and READS BACK as '" + landed + "'"
                        + (regenerated ? " after Revit regenerated" : "")
                        + (landed != written.Item6 ? " (straight after the write it read '" + written.Item6 + "')" : "")
                        + " - Revit reformatted, rounded or overrode it");
                }
            }

            if (pending.Count > 0 && changedOnRegeneration > 0)
            {
                findings.Add(changedOnRegeneration + " value(s) read differently after Revit regenerated than "
                    + "straight after the write - every read-back below is the later one");
            }
            if (yesNoRoute == "none")
            {
                findings.Add("This Revit answers neither Definition.GetDataType nor Definition.ParameterType, "
                    + "so no column was recognised as a Yes/No and a Yes or No cell was refused as a number");
            }

            var allLines = new List<string>(readBack);
            allLines.AddRange(problems);
            rowFindings = string.Join(" | ", allLines.ToArray());
            foreach (var line in allLines) findings.Add("  " + line);
            if (readBack.Count >= 25 || problems.Count >= 25)
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
