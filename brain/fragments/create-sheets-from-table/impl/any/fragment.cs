// NOT STANDALONE. Assumes `doc`, `sheets`, `titleBlockName` and `previewOnly`
// are in scope, and leaves `created`, `numberedAs`, `namedAs`, `valuesSet`,
// `preview`, `numberTaken`, `notCreated`, `columnsMatched`,
// `columnsUnmatched`, `columnsNotWritten`, `valuesNotSet`,
// `titleBlockNotFound`, `noTitleblock`, `titleBlockProblem`, `refused` and
// `findings` behind.
//
// SHEETS FROM A SPREADSHEET - docs/48 section 5 item 7, 2026-11-09. The file
// is read in Python (brain/heron_sheet_list.py, the chat tool heron_sheet_list)
// and arrives here as ONE value: rows separated by ";", cells by "|", a ";",
// "|" or "%" inside a cell percent-encoded - SET_PARAMETER_VALUES_BY_ID's
// `rows` shape and the Companion's table Apply. The first row is the header.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and opens none. Each sheet is
// made inside a SubTransaction of its own, so a row Revit refuses - its
// number, its name, the sheet itself - is TAKEN BACK ALONE and listed with
// Revit's words, and the other rows are kept. That is stricter than
// CREATE_SHEETS, which keeps a sheet whose number did not take and names it in
// `refused`: here no sheet is left in the register under a number Revit
// picked. A value Revit will not take is listed and the sheet is kept - a
// blank Drawn By is a smaller loss than a missing sheet.
//
// previewOnly DOES THE WHOLE JOB AND TAKES EVERY ROW BACK - each row's
// SubTransaction is rolled back instead of committed, after its read-back. So
// the preview is the run itself, not an estimate of it; only what Revit
// posts as a whole call is committed (nothing, for a preview) cannot show.
//
// WHAT IS CHECKED BEFORE ANY SHEET IS MADE (nothing is made when one fails):
//   the table's shape - a Sheet Number column, one column per header, no row
//     with more cells than the header (a stray "|");
//   every title block named - by titleBlockName or in a Title Block cell -
//     found, by type name or "Family: Type", a name two families share
//     refused (CREATE_SHEETS' rule, the same code); a row to be made with no
//     title block and no titleBlockName refuses the call - the title block has
//     no default; the word none, said outright, makes a bare sheet.
// WHAT IS CHECKED PER ROW, before its sheet is made:
//   no number - not made; a number twice in the table - NEITHER made, because
//   which row is meant is the modeller's to say (D-33); a number already in the
//   register, placeholders included, capitals ignored - not made, the sheet
//   holding it named (CREATE_SHEETS' rule).
//
// A PARAMETER COLUMN IS MATCHED BY NAME, ON EACH SHEET MADE:
//   1. the SHEET's own parameter of that name - Drawn By, Checked By, Sheet
//      Issue Date, a project parameter bound to Sheets. That is where a sheet's
//      values live (SET_SHEET_TITLE_BLOCK, PROVEN: "Sheet Number, Sheet Name,
//      issue date and every other sheet parameter live on the SHEET");
//   2. else the sheet's TITLE BLOCK INSTANCE's parameter of that name - a
//      per-sheet value of the border, such as a tick box that shows a stamp;
//   3. a TITLE BLOCK TYPE parameter is NOT written: it would change every sheet
//      on that type. Listed in columnsNotWritten;
//   4. nothing - listed in columnsUnmatched, never guessed (D-33, D-34).
//   Two parameters with one name on the same element are refused - Revit's
//   lookup by name returns either (FRAGMENT-ISSUES 5b-203). A read-only one,
//   and one holding another element (a type, a level), are listed, not written.
//
// A VALUE IS SET BY WHAT THE PARAMETER STORES (StorageType):
//   text            as typed - an issue date arrives as the reader wrote it,
//                   2026-11-09 for an Excel date;
//   a tick box      Yes/No, True/False, On/Off, 1/0, Y/N, X - recognised as
//                   SET_PARAMETER_VALUES_BY_ID (PROVEN) recognises one, by its
//                   spec on 2022+ and its ParameterType on 2020-2021, both by
//                   reflection so one source compiles 2020 to 2027;
//   a whole number  as one;
//   any other number  SetValueString - Revit's own reading in the project's
//                   units, no unit API called, so neither the 2021 ForgeTypeId
//                   change nor DisplayUnitType's removal reaches this code.
// An EMPTY CELL WRITES NOTHING. It does not clear a value - a new sheet has
// nothing to clear, and a blank in a register usually means "not yet".
// Every value is read back after doc.Regenerate() and a fresh lookup;
// text exactly, a tick box and a whole number by the integer Revit holds. A
// number Revit took is counted - its reading is in the project's units.
//
// No ElementId is built from or read as a number.

var created = new List<ElementId>();
var numberedAs = new Dictionary<ElementId, string>();
var namedAs = new Dictionary<ElementId, string>();
int valuesSet = 0;
string preview = "";
var numberTaken = new List<string>();
var notCreated = new List<string>();
var columnsMatched = new List<string>();
var columnsUnmatched = new List<string>();
var columnsNotWritten = new List<string>();
var valuesNotSet = new List<string>();
bool titleBlockNotFound = false;
bool noTitleblock = false;
string titleBlockProblem = "";
string refused = "";
var findings = new List<string>();

const int mostRows = 1000;
const int mostPreviewLines = 500;

// One cell: percent-decoded, a line break made a space (a sheet name or a
// parameter value is one line), trimmed.
Func<string, string> cellText = raw =>
{
    var text = raw ?? "";
    try { text = Uri.UnescapeDataString(text); }
    catch (Exception) { }
    return text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
};
Func<string, string, bool> sameText = (a, b) =>
    string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
Func<string, string> oneLine = text => string.Join(" ", (text ?? "").Split());

// ===========================================================================
// 1. THE TABLE
// ===========================================================================
var table = new List<string[]>();
foreach (var raw in (sheets ?? "").Split(';'))
{
    if (raw.Trim().Length == 0) continue;
    table.Add(raw.Split('|').Select(c => cellText(c)).ToArray());
}

var header = table.Count > 0 ? table[0] : new string[0];
int numberAt = -1, nameAt = -1, titleBlockAt = -1;
var parameterAt = new List<int>();
var shapeProblems = new List<string>();
var seenHeader = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

for (int c = 0; c < header.Length; c++)
{
    var said = header[c];
    if (said.Length == 0)
    {
        // A column with no header is ignored only when every cell in it is
        // empty - a trailing "|". With anything in it, which parameter it is
        // would be a guess.
        bool hasValues = false;
        for (int r = 1; r < table.Count; r++)
            if (c < table[r].Length && table[r][c].Length > 0) hasValues = true;
        if (hasValues)
            shapeProblems.Add("column " + (c + 1) + " has values but no header, so what it holds is not known");
        continue;
    }
    int earlier;
    if (seenHeader.TryGetValue(said, out earlier))
    {
        shapeProblems.Add("the header \"" + said + "\" is columns " + (earlier + 1) + " and " + (c + 1)
                        + " - one column per name");
        continue;
    }
    seenHeader[said] = c;
    var key = said.ToLowerInvariant();
    if (key == "sheet number" || key == "number")
    {
        if (numberAt >= 0) shapeProblems.Add("two columns give the sheet number: \"" + header[numberAt] + "\" and \"" + said + "\"");
        else numberAt = c;
    }
    else if (key == "sheet name" || key == "name")
    {
        if (nameAt >= 0) shapeProblems.Add("two columns give the sheet name: \"" + header[nameAt] + "\" and \"" + said + "\"");
        else nameAt = c;
    }
    else if (key == "title block" || key == "titleblock")
    {
        if (titleBlockAt >= 0) shapeProblems.Add("two columns give the title block: \"" + header[titleBlockAt] + "\" and \"" + said + "\"");
        else titleBlockAt = c;
    }
    else parameterAt.Add(c);
}

for (int r = 1; r < table.Count; r++)
{
    var row = table[r];
    if (row.Length > header.Length)
    {
        bool extra = false;
        for (int c = header.Length; c < row.Length; c++) if (row[c].Length > 0) extra = true;
        if (extra)
            shapeProblems.Add("entry " + r + " has " + row.Length + " cells and the header " + header.Length
                            + " - a | inside a value is written %7C, a ; as %3B");
    }
    if (row.Length < header.Length)
    {
        // Trailing empty cells left off: padded, never moved.
        var padded = new string[header.Length];
        for (int c = 0; c < header.Length; c++) padded[c] = c < row.Length ? row[c] : "";
        table[r] = padded;
    }
}

if (table.Count < 2)
    refused = "The table has no rows - the first row is the headers, and each row after it is one sheet. "
            + "Nothing was made.";
else if (numberAt < 0)
    refused = "No column is headed Sheet Number - the headers given are: "
            + string.Join(", ", header.Where(h => h.Length > 0).Select(h => "\"" + h + "\"").ToArray())
            + ". Nothing was made.";
else if (shapeProblems.Count > 0)
    refused = "The table cannot be read safely: " + string.Join("; ", shapeProblems.ToArray())
            + ". Nothing was made.";
else if (table.Count - 1 > mostRows)
    refused = "The table has " + (table.Count - 1) + " rows; one call makes at most " + mostRows
            + " sheets. Split the list. Nothing was made.";

// ===========================================================================
// 2. WHICH ROWS - numbers, before any sheet exists
// ===========================================================================
// Every number in the register, WITHOUT CASE, with the sheet holding it.
// Placeholders count: a placeholder holds its number so nothing else takes it.
var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var toMake = new List<int>();     // table row indexes to make

if (refused.Length == 0)
{
    foreach (var existing in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>())
    {
        if (existing == null) continue;
        string number = null, holder = null;
        try { number = existing.SheetNumber; } catch (Exception) { }
        try { holder = existing.Name; } catch (Exception) { }
        if (string.IsNullOrEmpty(number) || taken.ContainsKey(number)) continue;
        taken[number] = holder ?? "";
    }

    var inTable = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    for (int r = 1; r < table.Count; r++)
    {
        var number = table[r][numberAt];
        if (number.Length == 0) continue;
        List<int> where;
        if (!inTable.TryGetValue(number, out where)) inTable[number] = where = new List<int>();
        where.Add(r);
    }

    for (int r = 1; r < table.Count; r++)
    {
        var row = table[r];
        var number = row[numberAt];
        if (row.All(c => c.Length == 0)) continue;   // a blank row - nothing asked
        if (number.Length == 0)
        {
            notCreated.Add("entry " + r + (nameAt >= 0 && row[nameAt].Length > 0 ? " (\"" + row[nameAt] + "\")" : "")
                         + ": no sheet number - Revit has no unnumbered sheets; not made");
            continue;
        }
        var same = inTable[number];
        if (same.Count > 1)
        {
            notCreated.Add(number + ": entries " + string.Join(" and ", same.Select(x => x.ToString()).ToArray())
                         + " of the table both give this number - neither was made; say which is meant");
            continue;
        }
        string holderName;
        if (taken.TryGetValue(number, out holderName))
        {
            numberTaken.Add(number);
            notCreated.Add(number + ": already used by \"" + holderName + "\" - Revit refuses a duplicate "
                         + "number, capitals or not; not made");
            continue;
        }
        toMake.Add(r);
    }
}

// ===========================================================================
// 3. TITLE BLOCKS - every one named is found before any sheet exists
// ===========================================================================
// By NAME among the title block types, exactly as CREATE_SHEETS (PROVEN) finds
// one: "Family: Type" names one exactly; a plain type name must belong to one
// family only. Item1 the type (InvalidElementId for none), Item2 its label,
// Item3 the problem or null.
Func<string, Tuple<ElementId, string, string>> findTitleBlock = asked =>
{
    asked = (asked ?? "").Trim();
    if (string.Equals(asked, "none", StringComparison.OrdinalIgnoreCase))
        return Tuple.Create(ElementId.InvalidElementId, "no title block", (string)null);
    string askedFamily = null, askedType = null;
    int colon = asked.IndexOf(':');
    if (colon > 0)
    {
        askedFamily = asked.Substring(0, colon).Trim();
        askedType = asked.Substring(colon + 1).Trim();
    }
    var matches = new List<ElementId>();
    var labels = new List<string>();
    foreach (var type in new FilteredElementCollector(doc)
                 .OfCategory(BuiltInCategory.OST_TitleBlocks)
                 .WhereElementIsElementType()
                 .ToElements())
    {
        string typeName = null, familyName = null;
        try { typeName = type.Name; } catch (Exception) { }
        var elementType = type as ElementType;
        if (elementType != null)
        {
            try { familyName = elementType.FamilyName; } catch (Exception) { }
        }
        if (typeName == null) continue;
        bool byType = asked.Length > 0 && string.Equals(typeName, asked, StringComparison.OrdinalIgnoreCase);
        bool byBoth = askedFamily != null && familyName != null
                      && string.Equals(familyName, askedFamily, StringComparison.OrdinalIgnoreCase)
                      && string.Equals(typeName, askedType, StringComparison.OrdinalIgnoreCase);
        if (!byType && !byBoth) continue;
        matches.Add(type.Id);
        labels.Add((familyName ?? "(no family)") + ": " + typeName);
    }
    if (matches.Count == 1) return Tuple.Create(matches[0], labels[0], (string)null);
    if (matches.Count > 1)
        return Tuple.Create(ElementId.InvalidElementId, "",
            "\"" + asked + "\" is a title block type in " + matches.Count + " families - "
            + string.Join(", or ", labels.ToArray()) + " - so it does not say which; name it as Family: Type");
    return Tuple.Create(ElementId.InvalidElementId, "",
        "no title block type called \"" + asked + "\" is loaded in " + doc.Title);
};

var defaultTitleBlock = (titleBlockName ?? "").Trim();
if (sameText(defaultTitleBlock, "from the list")) defaultTitleBlock = "";

var titleBlockOf = new Dictionary<int, Tuple<ElementId, string>>();   // row -> (type, label)
if (refused.Length == 0 && toMake.Count > 0)
{
    var resolved = new Dictionary<string, Tuple<ElementId, string, string>>(StringComparer.OrdinalIgnoreCase);
    var askedBy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    var withoutOne = new List<string>();
    foreach (var r in toMake)
    {
        var asked = titleBlockAt >= 0 ? table[r][titleBlockAt] : "";
        if (asked.Length == 0) asked = defaultTitleBlock;
        if (asked.Length == 0) { withoutOne.Add(table[r][numberAt]); continue; }
        if (!resolved.ContainsKey(asked)) resolved[asked] = findTitleBlock(asked);
        List<string> who;
        if (!askedBy.TryGetValue(asked, out who)) askedBy[asked] = who = new List<string>();
        who.Add(table[r][numberAt]);
    }

    var problems = new List<string>();
    foreach (var pair in resolved)
    {
        if (pair.Value.Item3 == null) continue;
        var who = askedBy[pair.Key];
        problems.Add(pair.Value.Item3 + " (asked by " + string.Join(", ", who.Take(5).ToArray())
                     + (who.Count > 5 ? " and " + (who.Count - 5) + " more" : "") + ")");
    }
    if (withoutOne.Count > 0)
        problems.Add("no title block was named for " + string.Join(", ", withoutOne.Take(5).ToArray())
                     + (withoutOne.Count > 5 ? " and " + (withoutOne.Count - 5) + " more" : "")
                     + " - give titleBlockName, or a Title Block cell on each row; the word none makes "
                     + "sheets with no title block");
    if (problems.Count > 0)
    {
        titleBlockNotFound = true;
        titleBlockProblem = string.Join("; ", problems.ToArray())
            + ". Name each as it shows in the Project Browser - Family: Type. Nothing was made.";
        refused = titleBlockProblem;
    }
    else
    {
        foreach (var r in toMake)
        {
            var asked = titleBlockAt >= 0 ? table[r][titleBlockAt] : "";
            if (asked.Length == 0) asked = defaultTitleBlock;
            var found = resolved[asked];
            titleBlockOf[r] = Tuple.Create(found.Item1, found.Item2);
            if (found.Item1 == ElementId.InvalidElementId) noTitleblock = true;
        }
    }
}

// ===========================================================================
// 4. HOW A VALUE IS WRITTEN
// ===========================================================================
// A TICK BOX IS FOUND BY WHAT IT IS, NOT BY WHAT IT SAYS - SET_PARAMETER_
// VALUES_BY_ID's own version-safe check: SpecTypeId.Boolean.YesNo on 2022 and
// later; null on 2020 and 2021, which answer through ParameterType instead.
var yesNoSpecs = typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Boolean");
var yesNoProperty = yesNoSpecs == null ? null : yesNoSpecs.GetProperty("YesNo",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var yesNoSpec = yesNoProperty == null ? null : yesNoProperty.GetValue(null, null);

Func<Parameter, bool> isYesNo = p =>
{
    if (p == null || p.StorageType != StorageType.Integer) return false;
    try
    {
        var definition = p.Definition;
        if (definition == null) return false;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null && yesNoSpec != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return false;
            // NameEquals: a spec id carries its version - "...bool-1.0.0".
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
        // Not known to be a tick box: it is read as a whole number, where a
        // word is refused rather than written wrongly.
        return false;
    }
};

var ticked = new[] { "yes", "y", "true", "on", "1", "x" };
var unticked = new[] { "no", "n", "false", "off", "0" };

Func<Parameter, string> kindOf = p =>
{
    if (isYesNo(p)) return "Yes/No";
    if (p.StorageType == StorageType.String) return "text";
    if (p.StorageType == StorageType.Integer) return "whole number";
    if (p.StorageType == StorageType.Double) return "number, in the project's units";
    if (p.StorageType == StorageType.ElementId) return "another element";
    return "no value";
};

// The integer a tick box or a whole number should hold, or null when the text
// is not one.
Func<Parameter, string, int?> integerFor = (p, text) =>
{
    var word = (text ?? "").Trim().ToLowerInvariant();
    if (isYesNo(p))
    {
        if (ticked.Contains(word)) return 1;
        if (unticked.Contains(word)) return 0;
        return null;
    }
    int whole;
    if (int.TryParse(word, System.Globalization.NumberStyles.Integer,
                     System.Globalization.CultureInfo.InvariantCulture, out whole)) return whole;
    double real;
    if (double.TryParse(word, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out real)
        && Math.Abs(real - Math.Round(real)) < 1e-9 && Math.Abs(real) < int.MaxValue)
        return (int)Math.Round(real);
    return null;
};

// Null when written, else why not.
Func<Parameter, string, string> writeValue = (p, text) =>
{
    try
    {
        if (p.StorageType == StorageType.String)
            return p.Set(text) ? null : "Revit would not take it";
        if (p.StorageType == StorageType.Integer)
        {
            var wanted = integerFor(p, text);
            if (wanted == null)
                return isYesNo(p) ? "a tick box takes Yes or No" : "a whole number is needed";
            return p.Set(wanted.Value) ? null : "Revit would not take it";
        }
        if (p.StorageType == StorageType.Double)
            return p.SetValueString(text) ? null
                : "Revit could not read it as this parameter's kind of number in the project's units";
        return "it holds " + kindOf(p) + ", which this card does not write";
    }
    catch (Exception failure)
    {
        return "Revit said: " + oneLine(failure.Message);
    }
};

// One parameter of that name on the element: Item1 it, or null; Item2 why it
// cannot be used when two share the name.
Func<Element, string, Tuple<Parameter, string>> oneNamed = (element, name) =>
{
    IList<Parameter> found = null;
    try { found = element.GetParameters(name); }
    catch (Exception) { }
    if (found == null || found.Count == 0) return Tuple.Create((Parameter)null, (string)null);
    if (found.Count > 1)
        return Tuple.Create((Parameter)null, "two parameters are called \"" + name + "\" and Revit's lookup by name returns either");
    return Tuple.Create(found[0], (string)null);
};

// What each parameter column met, counted over the sheets made:
// column -> outcome -> how many sheets.
var outcomes = new Dictionary<int, Dictionary<string, int>>();
Action<int, string> met = (c, outcome) =>
{
    Dictionary<string, int> counts;
    if (!outcomes.TryGetValue(c, out counts)) outcomes[c] = counts = new Dictionary<string, int>();
    int n;
    counts.TryGetValue(outcome, out n);
    counts[outcome] = n + 1;
};
const string onSheet = "the sheet's own parameter";
const string onInstance = "the title block instance's parameter";
const string onType = "NOT WRITTEN - it is a parameter of the title block TYPE, and writing it would change every sheet on that type";
const string onNothing = "no parameter of this name on the sheet or its title block";

// ===========================================================================
// 5. MAKE THE SHEETS - each in its own SubTransaction
// ===========================================================================
var previewLines = new List<string>();
int made = 0, wouldSet = 0;

if (refused.Length == 0)
{
    foreach (var r in toMake)
    {
        var row = table[r];
        var number = row[numberAt];
        var name = nameAt >= 0 ? row[nameAt] : "";
        var titleBlock = titleBlockOf[r];
        var line = new List<string>();
        var written = new List<Tuple<Element, string, string, string>>();   // element, parameter, text, column
        ViewSheet sheet = null;
        string actualNumber = null, actualName = null;

        using (var step = new SubTransaction(doc))
        {
            step.Start();
            try
            {
                sheet = ViewSheet.Create(doc, titleBlock.Item1);
                if (sheet == null) throw new InvalidOperationException("Revit made no sheet");

                // The number and the name must both take, or the row is taken
                // back: no sheet stays under a number Revit picked.
                sheet.SheetNumber = number;
                if (name.Length > 0) sheet.Name = name;
                actualNumber = sheet.SheetNumber;
                actualName = sheet.Name;
                if (actualNumber != number)
                    throw new InvalidOperationException("Revit kept the number as \"" + actualNumber + "\"");
                if (name.Length > 0 && actualName != name)
                    throw new InvalidOperationException("Revit kept the name as \"" + actualName + "\"");

                // The title block instance on this sheet, by its owner view -
                // a property set when it is placed, so no regeneration is
                // needed to find it.
                FamilyInstance border = null;
                if (titleBlock.Item1 != ElementId.InvalidElementId)
                {
                    foreach (var one in new FilteredElementCollector(doc)
                                 .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                 .WhereElementIsNotElementType()
                                 .ToElements())
                    {
                        if (one.OwnerViewId == sheet.Id) { border = one as FamilyInstance; break; }
                    }
                }

                foreach (var c in parameterAt)
                {
                    var column = header[c];
                    var text = row[c];
                    Element target = null;
                    Parameter p = null;
                    var got = oneNamed(sheet, column);
                    string where = null;
                    if (got.Item2 != null) { met(c, "NOT WRITTEN - " + got.Item2 + " on the sheet"); continue; }
                    if (got.Item1 != null) { p = got.Item1; target = sheet; where = onSheet; }
                    else if (border != null)
                    {
                        var onBorder = oneNamed(border, column);
                        if (onBorder.Item2 != null) { met(c, "NOT WRITTEN - " + onBorder.Item2 + " on the title block"); continue; }
                        if (onBorder.Item1 != null) { p = onBorder.Item1; target = border; where = onInstance; }
                        else
                        {
                            // On the TYPE: one value for every sheet on it -
                            // never written from one row.
                            var borderType = doc.GetElement(border.GetTypeId());
                            var typeHas = borderType == null ? null : oneNamed(borderType, column);
                            if (typeHas != null && (typeHas.Item1 != null || typeHas.Item2 != null)) { met(c, onType); continue; }
                        }
                    }
                    if (p == null) { met(c, onNothing); continue; }
                    if (p.IsReadOnly) { met(c, "NOT WRITTEN - read-only, Revit fills it itself"); continue; }
                    if (p.StorageType == StorageType.ElementId || p.StorageType == StorageType.None)
                    {
                        met(c, "NOT WRITTEN - it holds " + kindOf(p) + ", which this card does not write");
                        continue;
                    }
                    met(c, where + " (" + kindOf(p) + ")");
                    if (text.Length == 0) continue;   // an empty cell writes nothing
                    var why = writeValue(p, text);
                    if (why != null)
                    {
                        valuesNotSet.Add(number + ", " + column + " = \"" + text + "\": " + why);
                        line.Add(column + " NOT SET (" + why + ")");
                        continue;
                    }
                    written.Add(Tuple.Create(target, p.Definition == null ? column : p.Definition.Name, text, column));
                }

                // READ BACK after one regeneration and a fresh lookup - what
                // landed, not what was asked for.
                doc.Regenerate();
                int setHere = 0;
                foreach (var w in written)
                {
                    var again = oneNamed(w.Item1, w.Item4).Item1;
                    bool differs;
                    string now = null;
                    if (again == null) differs = true;
                    else if (again.StorageType == StorageType.String)
                    {
                        now = again.AsString() ?? "";
                        differs = now != w.Item3;
                    }
                    else if (again.StorageType == StorageType.Integer)
                    {
                        var meant = integerFor(again, w.Item3);
                        try { now = again.AsValueString(); } catch (Exception) { }
                        differs = meant == null || again.AsInteger() != meant.Value;
                        if (now == null) now = again.AsInteger().ToString();
                    }
                    else
                    {
                        try { now = again.AsValueString(); } catch (Exception) { }
                        differs = false;   // Revit's own reading, in the project's units
                    }
                    if (differs)
                    {
                        valuesNotSet.Add(number + ", " + w.Item4 + " = \"" + w.Item3 + "\": reads back \"" + (now ?? "") + "\"");
                        line.Add(w.Item4 + " NOT SET (reads back \"" + (now ?? "") + "\")");
                    }
                    else
                    {
                        setHere++;
                        line.Add(w.Item4 + " = " + (now ?? w.Item3));
                    }
                }

                if (previewOnly)
                {
                    step.RollBack();
                    wouldSet += setHere;
                    previewLines.Add(number + " \"" + actualName + "\" on " + titleBlock.Item2 + ": would be made"
                                     + (line.Count > 0 ? " - " + string.Join("; ", line.ToArray()) : ""));
                }
                else
                {
                    step.Commit();
                    created.Add(sheet.Id);
                    numberedAs[sheet.Id] = actualNumber;
                    namedAs[sheet.Id] = actualName;
                    valuesSet += setHere;
                }
                made++;
                taken[number] = actualName ?? "";
            }
            catch (Exception failure)
            {
                if (step.HasStarted() && !step.HasEnded()) step.RollBack();
                // Nothing of this row is kept, and the values it counted go
                // with it.
                valuesNotSet.RemoveAll(v => v.StartsWith(number + ", ", StringComparison.Ordinal));
                notCreated.Add(number + ": Revit refused it, so this row was taken back - " + oneLine(failure.Message));
                if (previewOnly) previewLines.Add(number + ": would NOT be made - " + oneLine(failure.Message));
            }
        }
    }

    foreach (var line in notCreated)
        if (previewOnly && !line.Contains("Revit refused it")) previewLines.Add(line);

    // What each column met, one line each, in the table's order.
    foreach (var c in parameterAt)
    {
        var column = header[c];
        Dictionary<string, int> counts;
        if (!outcomes.TryGetValue(c, out counts))
        {
            if (made == 0) findings.Add(column + ": not checked - no sheet was made to look on");
            continue;
        }
        var into = counts.Where(k => k.Key.StartsWith(onSheet) || k.Key.StartsWith(onInstance)).ToList();
        var notWritten = counts.Where(k => k.Key.StartsWith("NOT WRITTEN")).ToList();
        int nothing;
        counts.TryGetValue(onNothing, out nothing);
        if (into.Count > 0)
            columnsMatched.Add(column + " -> " + string.Join("; ", into.Select(k => k.Key + " on " + k.Value + " sheet(s)").ToArray())
                               + (nothing > 0 ? "; " + onNothing + " on " + nothing + " sheet(s), not written there" : ""));
        else if (nothing > 0 && notWritten.Count == 0)
            columnsUnmatched.Add(column + ": " + onNothing + " - not written. A project parameter not bound to "
                                 + "Sheets, or a header worded unlike the parameter? Ask which parameter is meant");
        foreach (var k in notWritten)
            columnsNotWritten.Add(column + ": " + k.Key.Substring("NOT WRITTEN - ".Length) + " (" + k.Value + " sheet(s))");
    }
}

// ===========================================================================
// 6. THE ANSWER
// ===========================================================================
int rowsAsked = Math.Max(0, table.Count - 1);
if (refused.Length > 0)
{
    findings.Insert(0, refused);
}
else if (previewOnly)
{
    var shown = previewLines.Take(mostPreviewLines).ToList();
    if (previewLines.Count > mostPreviewLines) shown.Add("... and " + (previewLines.Count - mostPreviewLines) + " more");
    preview = string.Join("\n", shown.ToArray());
    findings.Insert(0, "PREVIEW - nothing was kept. " + made + " of " + rowsAsked + " row(s) would be made as sheets, "
                       + wouldSet + " value(s) set; " + notCreated.Count + " row(s) would not be made.");
}
else
{
    findings.Insert(0, "Made " + made + " of " + rowsAsked + " row(s) as sheets and set " + valuesSet + " value(s). "
                       + notCreated.Count + " row(s) not made, " + valuesNotSet.Count + " value(s) not set.");
}
if (noTitleblock && refused.Length == 0)
    findings.Add("Some sheets were " + (previewOnly ? "to be " : "") + "made with NO title block, because none was said outright - they print bare.");
