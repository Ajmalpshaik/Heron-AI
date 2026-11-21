// NOT STANDALONE. Assumes `doc`, `numbers`, `names`, `titleBlockName`,
// `elements`, `numberSuffix`, `namePrefix`, `sheets` and `previewOnly` are in
// scope, and leaves `created`, `numberedAs`, `namedAs`, `valuesSet`,
// `preview`, `numberTaken`, `refused`, `titleBlockNotFound`, `noTitleblock`,
// `titleBlockProblem`, `notCreated`, `notDuplicated`, `columnsMatched`,
// `columnsUnmatched`, `columnsNotWritten`, `valuesNotSet` and `findings`
// behind.
//
// VERSION 5 (2026-11-21, work-note item WN-6, issue #1005) - PLACEHOLDER
// SHEETS. With typed numbers, `titleBlockName` placeholder - the word, said
// outright as none is - makes each sheet with ViewSheet.CreatePlaceholder: a
// number and a name for the sheet list, no title block and no views. Every
// other rule of the typed job holds - the number checked against the register
// first (placeholders already count), each sheet in its own SubTransaction,
// number and name read back, previewOnly taken back. A table and the copy job
// are unchanged. Not run in Revit: NEEDS-CHECKING Group PE.
//
// VERSION 4 (2026-11-21) - CREATE_SHEETS_FROM_TABLE MERGED IN, on the owner's
// "merge: yes" (issue #1080), with the three rules D-193 records. Three ways
// to say which sheets to make, and the first one given decides:
//
//   A TABLE GIVEN (`sheets`) - CREATE_SHEETS_FROM_TABLE version 1's code, moved
//   here whole: rows ";", cells "|", every %XX decoded, the first row the
//   headers; Sheet Number required, Sheet Name and Title Block optional, every
//   other column filled into the new sheet's own parameter of that name or its
//   title block instance's - never a title block TYPE's. `numbers`, `names`
//   and `elements` are not read.
//
//   NUMBERS GIVEN - versions 1 to 3, with version 4's three rules below.
//   `elements` is not read, so a selection never adds a copy.
//
//   NO TABLE, NO NUMBERS, SHEETS HANDED IN WITH A `numberSuffix` - version 3's
//   copy job, unchanged: the source's own title block, its number + the
//   suffix, `namePrefix` + its name. No suffix is refused: the copy would
//   collide with its source, and "make a new sheet" said with sheets selected
//   must not copy them.
//
// A TABLE AND NUMBERS BOTH GIVEN IS REFUSED. Which list is meant is the
// caller's to say, never this card's guess (D-33); nothing is made.
//
// VERSION 4'S THREE RULES (D-193), FOR A TABLE AND TYPED NUMBERS ALIKE:
//   1. A NUMBER OR A NAME REVIT REFUSES TAKES THAT SHEET BACK. Each sheet is
//      made in a SubTransaction of its own (the table card's way); the number
//      and the name must both take, read back, or the SubTransaction is rolled
//      back and the sheet is listed in notCreated with Revit's own words. So no
//      sheet is left under a number or a name Revit chose by itself. Version 3
//      kept such a sheet, named it in `refused` when its number did not take,
//      and said nothing at all when only its name did not (FRAGMENT-ISSUES row
//      5b-1893).
//   2. THE SAME NUMBER TWICE IN ONE REQUEST - capitals ignored - MAKES NEITHER,
//      and each is listed. Version 3 made the first and refused the second as
//      already used; which was meant is the modeller's to say.
//   3. AT MOST 1,000 SHEETS IN ONE CALL - a table's rows, and typed numbers
//      too. More is refused whole; nothing is made.
// "Taken back alone" covers what Revit THROWS. A failure Revit only POSTS, as
// the call's transaction is committed, is handled by the executor's failure
// discipline and rolls the WHOLE call back - one undo, nothing kept (Article
// 8); a SubTransaction cannot keep the rest past that.
//
// THE COPY JOB KEEPS VERSION 3'S RULES. It makes its copies in the call's own
// transaction, so a copy whose number did not take is still in `created` and
// `refused` because it exists; whether D-193's first rule should reach it too
// is the owner's (FRAGMENT-ISSUES row 5b-1935).
//
// previewOnly (version 4) - a table, or typed numbers: the whole job is done
// and every sheet's SubTransaction is rolled back after its read-back, so the
// preview is the run itself. Nothing is kept. Asked with the copy job, it is
// refused and nothing is made.
//
// D-183's four, kept for the table: an empty cell writes nothing; an Excel date
// arrives as an ISO date, written as the reader wrote it (brain/
// heron_sheet_list.py); a VALUE Revit refuses is listed in valuesNotSet and its
// sheet is kept - rule 1 above is about the sheet's own number and name; at
// most 1,000 rows.
//
// NO JOB COPIES THE VIEWS ON A SHEET. Every route makes sheets with nothing
// placed on them; DUPLICATE_VIEWS and PLACE_VIEW_ON_SHEET do that.
//
// `numberTaken` holds every number already in the register, in any job, and
// in the copy job one claimed earlier in the same batch (DUPLICATE_SHEETS'
// collidedNumbers); `notDuplicated` the sources no copy came back for.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16. It
// opens a SubTransaction per sheet for a table and for typed numbers.
//
// THE NUMBER IS CHECKED BEFORE ANYTHING IS CREATED.
//
// ViewSheet.Create makes the sheet with a number REVIT picks; setting the
// wanted one is a second step that can be refused for a clash. So a clash
// leaves a REAL SHEET in the register under the wrong number, and a count
// reports the job as done. That is the same shape DUPLICATE_SHEETS was
// re-authored to remove, where the earlier version reported "Duplicated 0 of 1"
// while the sheet count went 2 to 3 and an orphan stayed behind.
//
// Checking first removes the failure instead of cleaning up after it, and it
// makes a RE-RUN create nothing rather than a second set of misnumbered sheets.
//
// THE TITLE BLOCK HAS NO DEFAULT. Revit will create a sheet with none at all,
// and a register full of blank-bordered sheets is discovered at issue. Which
// title block a project uses is a project decision.
//
// NUMBER AND NAME ARE READ BACK SEPARATELY because they fail independently: the
// number can be refused while the name is accepted, and the result looks
// correct in any list showing only one of them.
//
// VERSION 2 (2026-11-01) - CREATE_SHEET MERGED IN, AND ITS RULES WITH IT.
//
// THE CLASH CHECK IGNORES CAPITALS. "M-101" and "m-101" are the same drawing to
// everybody except a string comparison, and Revit refuses the pair too. The
// line in `notCreated` names the sheet that already holds the number.
//
// AN EMPTY NUMBER IS REFUSED. Revit has no unnumbered sheets, and creating one
// to number it afterwards is the orphan above.
//
// A FAILED ViewSheet.Create IS A REFUSAL, NOT A TAKEN NUMBER. Version 1 put it
// in `numberTaken`, which sent the reader looking for a clash that was not
// there. It is a line in `notCreated` now, carrying Revit's own words - "Revit
// declined" does not say WHICH rule was broken and Revit's sentence does.
//
// THE TITLE BLOCK IS MATCHED SO TWO FAMILIES ARE TOLD APART. Two title block
// families can each have a type called "A1". The name may be written
// "Family: Type"; a plain type name that two families share is refused,
// naming both, never settled by taking the first.
//
// NO TITLE BLOCK ONLY WHEN THE CALLER SAYS `none` OUTRIGHT. CREATE_SHEET made a
// bare sheet whenever no title block came; this card refused. The safer rule
// is kept: a name that is not found refuses and creates nothing, and only the
// word `none` makes sheets with no title block - flagged in `noTitleblock`,
// because such a sheet looks finished and prints bare.
//
// ---- THE TABLE ROUTE, as CREATE_SHEETS_FROM_TABLE version 1 described it
// (2026-11-09, docs/48 section 5 item 7). The file is read in Python
// (brain/heron_sheet_list.py, the chat tool heron_sheet_list) and arrives here
// as ONE value: rows separated by ";", cells by "|", a ";", "|" or "%" inside a
// cell percent-encoded (any %XX is decoded as UTF-8, so the reader's %22, %27,
// %E2%80%99, %24, %60 and %5C arrive as typed) - SET_PARAMETER_VALUES_BY_ID's
// `rows` shape and the Companion's table Apply. The first row is the header.
//
// WHAT IS CHECKED BEFORE ANY SHEET IS MADE (nothing is made when one fails):
//   the table's shape - a Sheet Number column, one column per header, no row
//     with more cells than the header (a stray "|"), at most 1,000 rows;
//   every title block named - by titleBlockName or in a Title Block cell -
//     found, by type name or "Family: Type", a name two families share
//     refused; a row to be made with no title block and no titleBlockName
//     refuses the call - the title block has no default; the word none, said
//     outright, makes a bare sheet.
// WHAT IS CHECKED PER ROW, before its sheet is made:
//   no number - not made; a number twice in the table - NEITHER made, because
//   which row is meant is the modeller's to say (D-33); a number already in the
//   register, placeholders included, capitals ignored - not made, the sheet
//   holding it named.
//
// A PARAMETER COLUMN IS MATCHED BY NAME, ON EACH SHEET MADE:
//   1. the SHEET's own parameter of that name - Drawn By, Checked By, Sheet
//      Issue Date, a project parameter bound to Sheets. That is where a sheet's
//      values live (SET_SHEET_TITLE_BLOCK: "Sheet Number, Sheet Name, issue
//      date and every other sheet parameter live on the SHEET");
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
//                   SET_PARAMETER_VALUES_BY_ID recognises one, by its spec on
//                   2022+ and its ParameterType on 2020-2021, both by
//                   reflection so one source compiles 2020 to 2027;
//   a whole number  as one;
//   any other number  SetValueString - Revit's own reading in the project's
//                   units, no unit API called, so neither the 2021 ForgeTypeId
//                   change nor DisplayUnitType's removal reaches this code.
// An EMPTY CELL WRITES NOTHING. It does not clear a value - a new sheet has
// nothing to clear, and a blank in a register usually means "not yet".
// A kept run reads every value back after ONE doc.Regenerate() at the end
// and a fresh lookup; text exactly, a tick box and a whole number by the
// integer Revit holds. A number Revit took is counted - its reading is in the
// project's units. A preview reads each row at once, before its rollback.
//
// No ElementId is built from or read as a number.

var created = new List<ElementId>();
var numberedAs = new Dictionary<ElementId, string>();
var namedAs = new Dictionary<ElementId, string>();
var numberTaken = new List<string>();
var refused = new List<ElementId>();
bool titleBlockNotFound = false;
bool noTitleblock = false;
string titleBlockProblem = "";
var notCreated = new List<string>();
var notDuplicated = new List<ElementId>();
// VERSION 4, from CREATE_SHEETS_FROM_TABLE. Empty, or 0, on every route but
// the table's - except `preview` and `findings`, which typed numbers fill on a
// preview too.
int valuesSet = 0;
string preview = "";
var columnsMatched = new List<string>();
var columnsUnmatched = new List<string>();
var columnsNotWritten = new List<string>();
var valuesNotSet = new List<string>();
var findings = new List<string>();

// VERSION 4: one call makes at most this many sheets - a table's rows and
// typed numbers alike (D-183 item 4, D-193 rule 3) - and a preview shows at
// most this many lines.
const int mostRows = 1000;
const int mostPreviewLines = 500;
Func<string, string> oneLine = text => string.Join(" ", (text ?? "").Split());

// VERSION 3: WHICH JOB. The card's `absent:` words - "none" for a list, a
// table or a prefix not given, "from the source sheets" for a title block not
// named - are read here as nothing given. A sheet numbered "none" is not a
// sheet anybody means.
Func<string, string, bool> sameText = (a, b) =>
    string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);
Func<IList<string>, bool> leftOut = list =>
    list == null || list.Count == 0
    || (list.Count == 1 && (sameText(list[0], "none") || (list[0] ?? "").Trim().Length == 0));

bool byNumbers = !leftOut(numbers);
// VERSION 4: a table is ONE value, its absent word "none".
bool byTable = (sheets ?? "").Trim().Length > 0 && !sameText(sheets, "none");
if (leftOut(names)) names = null;
var suffix = (numberSuffix ?? "").Trim();
if (sameText(suffix, "none")) suffix = "";
var prefix = namePrefix ?? "";
if (sameText(prefix, "none")) prefix = "";
// A NAME PREFIX KEEPS ITS SPACE - "DRAFT " is how a modeller types one, and
// trimming it would run the prefix into the name.

var askedTitleBlock = (titleBlockName ?? "").Trim();
bool titleBlockLeftOut = sameText(askedTitleBlock, "from the source sheets");
if (titleBlockLeftOut) askedTitleBlock = "";

bool copying = !byTable && !byNumbers && elements != null && elements.Any(e => e != null);

// VERSION 4. Typed names, a number suffix or a name prefix beside a table
// would be IGNORED by the table route, and an input ignored without a word
// is a guess about what was meant (D-33) - so they refuse the call like
// typed numbers do (found by the review of 2026-11-21).
var typedBesideTable = new List<string>();
if (byTable)
{
    if (byNumbers) typedBesideTable.Add("typed sheet numbers (numbers)");
    if (names != null) typedBesideTable.Add("typed names (names)");
    if (suffix.Length > 0) typedBesideTable.Add("a number suffix (numberSuffix)");
    if (prefix.Length > 0) typedBesideTable.Add("a name prefix (namePrefix)");
}

if (byTable && typedBesideTable.Count > 0)
{
    // VERSION 4. A table and typed numbers are two lists of sheets, and which
    // one is meant is not this card's guess (D-33). Nothing is made.
    notCreated.Add("Both a table of sheets (sheets) and " + string.Join(", ", typedBesideTable)
                 + " were given, so which is meant is not known - give the table alone. "
                 + "Nothing was created.");
}
else if (byTable)
{
// VERSION 4: the table card's `refused` - why NOTHING was made - by another
// name here, because this card's `refused` lists sheet ids.
string tableRefused = "";

// One cell: percent-decoded, a line break made a space (a sheet name or a
// parameter value is one line), trimmed.
Func<string, string> cellText = raw =>
{
    var text = raw ?? "";
    try { text = Uri.UnescapeDataString(text); }
    catch (Exception) { }
    return text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
};

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
    tableRefused = "The table has no rows - the first row is the headers, and each row after it is one sheet. "
            + "Nothing was made.";
else if (numberAt < 0)
    tableRefused = "No column is headed Sheet Number - the headers given are: "
            + string.Join(", ", header.Where(h => h.Length > 0).Select(h => "\"" + h + "\"").ToArray())
            + ". Nothing was made.";
else if (shapeProblems.Count > 0)
    tableRefused = "The table cannot be read safely: " + string.Join("; ", shapeProblems.ToArray())
            + ". Nothing was made.";
else if (table.Count - 1 > mostRows)
    tableRefused = "The table has " + (table.Count - 1) + " rows; one call makes at most " + mostRows
            + " sheets. Split the list. Nothing was made.";

// ===========================================================================
// 2. WHICH ROWS - numbers, before any sheet exists
// ===========================================================================
// Every number in the register, WITHOUT CASE, with the sheet holding it.
// Placeholders count: a placeholder holds its number so nothing else takes it.
var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var toMake = new List<int>();     // table row indexes to make

if (tableRefused.Length == 0)
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
// By NAME among the title block types, by the same rules as the typed numbers
// below: "Family: Type" names one exactly; a plain type name must belong to one
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

// VERSION 4: the title block named, read once at the top of the card - its
// absent word ("from the source sheets") already read as nothing named.
var defaultTitleBlock = askedTitleBlock;
if (sameText(defaultTitleBlock, "from the list")) defaultTitleBlock = "";

var titleBlockOf = new Dictionary<int, Tuple<ElementId, string>>();   // row -> (type, label)
if (tableRefused.Length == 0 && toMake.Count > 0)
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
        tableRefused = titleBlockProblem;
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
// column -> outcome -> how many sheets. A row's outcomes are kept apart and
// counted ONLY when the row is kept (or, in a preview, would be): a row taken
// back is not a sheet any column was written on.
var outcomes = new Dictionary<int, Dictionary<string, int>>();
Action<List<Tuple<int, string>>> count = rowMet =>
{
    foreach (var m in rowMet)
    {
        Dictionary<string, int> counts;
        if (!outcomes.TryGetValue(m.Item1, out counts)) outcomes[m.Item1] = counts = new Dictionary<string, int>();
        int n;
        counts.TryGetValue(m.Item2, out n);
        counts[m.Item2] = n + 1;
    }
};
const string onSheet = "the sheet's own parameter";
const string onInstance = "the title block instance's parameter";
const string onType = "NOT WRITTEN - it is a parameter of the title block TYPE, and writing it would change every sheet on that type";
const string onNothing = "no parameter of this name on the sheet or its title block";

// The title block instance on a sheet just made. The collector is SCOPED TO
// THE SHEET - not every title block in the model once per row - and asked
// only when a column is not the sheet's own. Whether a view-scoped collector
// sees an element placed in this same transaction before a regeneration is
// NOT KNOWN from the assemblies (NEEDS-CHECKING OZ10, from MY5), so: the scoped
// collector; if it finds nothing or throws, one doc.Regenerate() and the
// scoped collector again; then the model's title blocks by OwnerViewId, a
// property set when the instance is placed. `regenerations` counts how often
// the second step was needed, for OZ10 and OZ13 to read.
int regenerations = 0;
Func<ViewSheet, FamilyInstance> borderOf = newSheet =>
{
    Func<FamilyInstance> scoped = () =>
    {
        try
        {
            return new FilteredElementCollector(doc, newSheet.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsNotElementType()
                .FirstElement() as FamilyInstance;
        }
        catch (Exception) { return null; }
    };
    var found = scoped();
    if (found != null) return found;
    doc.Regenerate();
    regenerations++;
    found = scoped();
    if (found != null) return found;
    foreach (var one in new FilteredElementCollector(doc)
                 .OfCategory(BuiltInCategory.OST_TitleBlocks)
                 .WhereElementIsNotElementType()
                 .ToElements())
    {
        if (one.OwnerViewId == newSheet.Id) return one as FamilyInstance;
    }
    return null;
};

// What landed: (values as asked, lines not set, words for the preview line)
// for the values written - each looked up again by name and compared: text
// exactly, a tick box and a whole number by the integer Revit holds, any
// other number as Revit's own reading in the project's units.
Func<List<Tuple<Element, string, string, string>>, Tuple<int, List<string>, List<string>>> readBack = written =>
{
    int asAsked = 0;
    var notSet = new List<string>();
    var said = new List<string>();
    foreach (var w in written)
    {
        Parameter again = null;
        try { again = oneNamed(w.Item1, w.Item4).Item1; } catch (Exception) { }
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
            differs = false;
        }
        if (differs)
        {
            notSet.Add(w.Item2 + ", " + w.Item4 + " = \"" + w.Item3 + "\": reads back \"" + (now ?? "") + "\"");
            said.Add(w.Item4 + " NOT SET (reads back \"" + (now ?? "") + "\")");
        }
        else
        {
            asAsked++;
            said.Add(w.Item4 + " = " + (now ?? w.Item3));
        }
    }
    return Tuple.Create(asAsked, notSet, said);
};

// ===========================================================================
// 5. MAKE THE SHEETS - each in its own SubTransaction
// ===========================================================================
// "TAKEN BACK ALONE" COVERS WHAT REVIT THROWS, AND ONLY THAT. A row whose
// sheet, number or name Revit refuses by THROWING is rolled back in its own
// SubTransaction and listed, and the others are kept. A failure Revit only
// POSTS - a warning or error it raises when the call's transaction is
// committed - is handled by the executor's failure discipline then, and rolls
// the WHOLE call back, one undo, nothing kept (Article 8): a SubTransaction
// cannot keep the rest past that (DRAW_MEP_FROM_CAD's measured lesson).
// Making a sheet posts nothing that is known; OZ12 (from MY13) records what a
// real run shows.
//
// ONE REGENERATION FOR THE WHOLE CALL, not one per row. A kept run reads every
// value back after the last row, after one doc.Regenerate() and a fresh
// lookup. A PREVIEW must read each row before rolling it back, so it reads it
// at once, with no regeneration - text and integers read back as set; a
// number's reading may be Revit's before it regenerates (said in findings).
var previewLines = new List<string>();
var keptWritten = new List<Tuple<Element, string, string, string>>();   // element, sheet number, text, column
int made = 0, wouldSet = 0;

if (tableRefused.Length == 0)
{
    foreach (var r in toMake)
    {
        var row = table[r];
        var number = row[numberAt];
        var name = nameAt >= 0 ? row[nameAt] : "";
        var titleBlock = titleBlockOf[r];
        var line = new List<string>();
        var rowMet = new List<Tuple<int, string>>();
        var rowNotSet = new List<string>();
        var written = new List<Tuple<Element, string, string, string>>();
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

                FamilyInstance border = null;
                bool borderAsked = false;
                foreach (var c in parameterAt)
                {
                    var column = header[c];
                    var text = row[c];
                    Element target = null;
                    Parameter p = null;
                    var got = oneNamed(sheet, column);
                    string where = null;
                    if (got.Item2 != null) { rowMet.Add(Tuple.Create(c, "NOT WRITTEN - " + got.Item2 + " on the sheet")); continue; }
                    if (got.Item1 != null) { p = got.Item1; target = sheet; where = onSheet; }
                    else if (titleBlock.Item1 != ElementId.InvalidElementId)
                    {
                        if (!borderAsked) { border = borderOf(sheet); borderAsked = true; }
                        if (border != null)
                        {
                            var onBorder = oneNamed(border, column);
                            if (onBorder.Item2 != null) { rowMet.Add(Tuple.Create(c, "NOT WRITTEN - " + onBorder.Item2 + " on the title block")); continue; }
                            if (onBorder.Item1 != null) { p = onBorder.Item1; target = border; where = onInstance; }
                            else
                            {
                                // On the TYPE: one value for every sheet on it -
                                // never written from one row.
                                var borderType = doc.GetElement(border.GetTypeId());
                                var typeHas = borderType == null ? null : oneNamed(borderType, column);
                                if (typeHas != null && (typeHas.Item1 != null || typeHas.Item2 != null)) { rowMet.Add(Tuple.Create(c, onType)); continue; }
                            }
                        }
                    }
                    if (p == null) { rowMet.Add(Tuple.Create(c, onNothing)); continue; }
                    if (p.IsReadOnly) { rowMet.Add(Tuple.Create(c, "NOT WRITTEN - read-only, Revit fills it itself")); continue; }
                    if (p.StorageType == StorageType.ElementId || p.StorageType == StorageType.None)
                    {
                        rowMet.Add(Tuple.Create(c, "NOT WRITTEN - it holds " + kindOf(p) + ", which this card does not write"));
                        continue;
                    }
                    rowMet.Add(Tuple.Create(c, where + " (" + kindOf(p) + ")"));
                    if (text.Length == 0) continue;   // an empty cell writes nothing
                    var why = writeValue(p, text);
                    if (why != null)
                    {
                        rowNotSet.Add(number + ", " + column + " = \"" + text + "\": " + why);
                        line.Add(column + " NOT SET (" + why + ")");
                        continue;
                    }
                    written.Add(Tuple.Create(target, number, text, column));
                }

                if (previewOnly)
                {
                    // Read before it is rolled back - at once, no regeneration.
                    var landed = readBack(written);
                    step.RollBack();
                    wouldSet += landed.Item1;
                    rowNotSet.AddRange(landed.Item2);
                    line.AddRange(landed.Item3);
                    previewLines.Add(number + " \"" + actualName + "\" on " + titleBlock.Item2 + ": would be made"
                                     + (line.Count > 0 ? " - " + string.Join("; ", line.ToArray()) : ""));
                }
                else
                {
                    step.Commit();
                    created.Add(sheet.Id);
                    numberedAs[sheet.Id] = actualNumber;
                    namedAs[sheet.Id] = actualName;
                    keptWritten.AddRange(written);
                }
                // Kept (or would be): only now do its outcomes count.
                count(rowMet);
                valuesNotSet.AddRange(rowNotSet);
                made++;
                taken[number] = actualName ?? "";
            }
            catch (Exception failure)
            {
                if (step.HasStarted() && !step.HasEnded()) step.RollBack();
                // Nothing of this row is kept, and nothing it met is counted.
                notCreated.Add(number + ": Revit refused it, so this row was taken back - " + oneLine(failure.Message));
                if (previewOnly) previewLines.Add(number + ": would NOT be made - " + oneLine(failure.Message));
            }
        }
    }

    // A KEPT RUN'S READ-BACK: one regeneration, then every value looked up
    // again. A value that reads back different is listed; its sheet stays.
    if (!previewOnly && keptWritten.Count > 0)
    {
        doc.Regenerate();
        var landed = readBack(keptWritten);
        valuesSet = landed.Item1;
        valuesNotSet.AddRange(landed.Item2);
    }
    if (previewOnly && wouldSet > 0)
        findings.Add("A preview reads each value back before the row is taken back, with no regeneration; "
                     + "a kept run reads them after one.");
    if (regenerations > 0)
        findings.Add("The title block on " + regenerations + " new sheet(s) was found only after a regeneration.");

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
if (tableRefused.Length > 0)
{
    findings.Insert(0, tableRefused);
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
if (noTitleblock && tableRefused.Length == 0)
    findings.Add("Some sheets were " + (previewOnly ? "to be " : "") + "made with NO title block, because none was said outright - they print bare.");

// VERSION 4: A REFUSAL OF THE WHOLE TABLE IS ALSO A LINE IN notCreated, where
// this card has always put a refusal of the whole call - the table card's own
// `refused` was a string, and this card's `refused` lists sheet ids. A title
// block refusal is in titleBlockProblem, as for typed numbers.
if (tableRefused.Length > 0 && !titleBlockNotFound)
    notCreated.Add(tableRefused);
}
else if (byNumbers)
{
// ---- NUMBERS GIVEN: versions 1 to 3, with version 4's three rules (D-193).
// `elements` is NOT read, so a sheet that happens to be selected is never
// copied by a request that named its numbers.

if (numbers.Count > mostRows)
{
    // VERSION 4, D-193 rule 3: the table's ceiling binds typed numbers too -
    // one rule for both ways of asking. Refused whole, before anything.
    notCreated.Add(numbers.Count + " sheet numbers were given; one call makes at most " + mostRows
                 + " sheets. Split the list. Nothing was created.");
}
else
{
// By NAME among the title block types - a project standard is written in names,
// and an id is not portable between models. "Family: Type" or "Family : Type"
// names one exactly; a plain type name must belong to one family only.
ElementId titleBlockId = ElementId.InvalidElementId;
bool titleBlockChosen = false;
string titleBlockLabel = "no title block";   // VERSION 4: a preview line says it
// VERSION 5 (work-note item WN-6, issue #1005): the word placeholder, said
// outright, makes PLACEHOLDER sheets - ViewSheet.CreatePlaceholder, in every
// release 2020 to 2027: Revit's own kind of sheet with a number and a name for
// the sheet list and no title block or views, which is how a drawing register
// is set up before the drawings are. The typed-numbers job only: a table's
// Title Block cell and the copy job are as version 4 left them.
bool placeholderAsked = false;

if (string.Equals(askedTitleBlock, "none", StringComparison.OrdinalIgnoreCase))
{
    // Said outright. The sheets are made bare and the answer says so.
    noTitleblock = true;
    titleBlockChosen = true;
}
else if (string.Equals(askedTitleBlock, "placeholder", StringComparison.OrdinalIgnoreCase))
{
    // VERSION 5: said outright, as none is. Not noTitleblock: a placeholder is
    // not a sheet that prints bare, it is a line on the sheet list.
    placeholderAsked = true;
    titleBlockChosen = true;
    titleBlockLabel = "a placeholder (no title block, no views)";
}
else
{
    string askedFamily = null, askedType = null;
    int colon = askedTitleBlock.IndexOf(':');
    if (colon > 0)
    {
        askedFamily = askedTitleBlock.Substring(0, colon).Trim();
        askedType = askedTitleBlock.Substring(colon + 1).Trim();
    }

    var matches = new List<ElementId>();
    var matchNames = new List<string>();
    foreach (var type in new FilteredElementCollector(doc)
                 .OfCategory(BuiltInCategory.OST_TitleBlocks)
                 .WhereElementIsElementType()
                 .ToElements())
    {
        string typeName = null, familyName = null;
        try { typeName = type.Name; } catch { }
        var elementType = type as ElementType;
        if (elementType != null)
        {
            try { familyName = elementType.FamilyName; } catch { }
        }
        if (typeName == null) continue;

        bool byType = askedTitleBlock.Length > 0
                      && string.Equals(typeName, askedTitleBlock, StringComparison.OrdinalIgnoreCase);
        bool byBoth = askedFamily != null && familyName != null
                      && string.Equals(familyName, askedFamily, StringComparison.OrdinalIgnoreCase)
                      && string.Equals(typeName, askedType, StringComparison.OrdinalIgnoreCase);
        if (!byType && !byBoth) continue;

        matches.Add(type.Id);
        matchNames.Add((familyName ?? "(no family)") + ": " + typeName);
    }

    if (matches.Count == 1)
    {
        titleBlockId = matches[0];
        titleBlockChosen = true;
        titleBlockLabel = matchNames[0];
    }
    else if (matches.Count > 1)
    {
        titleBlockNotFound = true;
        titleBlockProblem = "\"" + askedTitleBlock + "\" is a title block type in "
            + matches.Count + " families, so it does not say which: "
            + string.Join(", or ", matchNames.ToArray())
            + ". Name it as Family: Type. Nothing was created.";
    }
    else
    {
        titleBlockNotFound = true;
        titleBlockProblem = (askedTitleBlock.Length == 0
                ? "No title block was named"
                : "No title block type called \"" + askedTitleBlock + "\" is loaded in " + doc.Title)
            + ". Name one as it shows in the Project Browser - Family: Type - or say none "
            + "for sheets with no title block, or placeholder for placeholder sheets. Nothing was created.";
    }
}

if (titleBlockChosen)
{
    // Every number in the register, WITHOUT CASE, with the sheet holding it so
    // a clash can be named. Placeholders count: a placeholder holds its number
    // precisely so nothing else takes it.
    var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var sheet in new FilteredElementCollector(doc)
                 .OfClass(typeof(ViewSheet))
                 .Cast<ViewSheet>())
    {
        if (sheet == null) continue;
        string number = null, holder = null;
        try { number = sheet.SheetNumber; } catch { }
        try { holder = sheet.Name; } catch { }
        if (string.IsNullOrEmpty(number) || taken.ContainsKey(number)) continue;
        taken[number] = holder ?? "";
    }

    // VERSION 4, D-193 rule 2: every entry of each number in THIS request,
    // capitals ignored as the register's check ignores them. A number given
    // twice makes NEITHER sheet - which one was meant, with which name, is the
    // modeller's to say (D-33). Version 3 made the first and refused the
    // second as already used.
    var inRequest = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < numbers.Count; i++)
    {
        string given = (numbers[i] ?? "").Trim();
        if (given.Length == 0) continue;
        List<int> entries;
        if (!inRequest.TryGetValue(given, out entries)) inRequest[given] = entries = new List<int>();
        entries.Add(i + 1);
    }

    // VERSION 4: what a preview says, a line per number, in the request's order.
    var previewLines = new List<string>();
    Action<string> notMade = said =>
    {
        notCreated.Add(said);
        if (previewOnly) previewLines.Add(said);
    };
    int made = 0;

    for (int i = 0; i < numbers.Count; i++)
    {
        string wantedNumber = (numbers[i] ?? "").Trim();

        if (wantedNumber.Length == 0)
        {
            notMade("(blank, entry " + (i + 1) + "): a sheet needs a number - Revit has "
                  + "no unnumbered sheets");
            continue;
        }

        var twice = inRequest[wantedNumber];
        if (twice.Count > 1)
        {
            notMade(wantedNumber + ": entries " + string.Join(" and ", twice.Select(x => x.ToString()).ToArray())
                  + " of the numbers given both give this number - neither was made; say which is meant");
            continue;
        }

        // Checked BEFORE creating. Nothing is left behind to tidy up.
        string holderName;
        if (taken.TryGetValue(wantedNumber, out holderName))
        {
            numberTaken.Add(wantedNumber);
            notMade(wantedNumber + ": already used by \"" + holderName + "\" - Revit "
                  + "refuses a duplicate number, capitals or not");
            continue;
        }

        string wantedName = names != null && i < names.Count && !string.IsNullOrWhiteSpace(names[i])
            ? names[i].Trim() : null;

        // VERSION 4, D-193 rule 1: THE SHEET IN A SUBTRANSACTION OF ITS OWN.
        // Its number and its name must both take, read back, or the sheet is
        // TAKEN BACK and listed with Revit's own words - no sheet stays under a
        // number or a name Revit chose by itself. Fewer names than numbers is
        // still allowed: a sheet with no name given takes the one Revit gives
        // it, reported in namedAs.
        using (var step = new SubTransaction(doc))
        {
            ViewSheet sheet = null;
            string why = null, actualNumber = null, actualName = null;
            try
            {
                step.Start();
                string revitSaid = null;
                try
                {
                    sheet = placeholderAsked
                        ? ViewSheet.CreatePlaceholder(doc)
                        : ViewSheet.Create(doc, titleBlockId);
                }
                catch (Exception failure) { revitSaid = failure.Message; }
                if (sheet == null)
                {
                    why = "Revit declined to create the sheet"
                        + (revitSaid == null ? "" : ". Revit said: " + oneLine(revitSaid));
                }
                else
                {
                    try { sheet.SheetNumber = wantedNumber; }
                    catch (Exception failure)
                    {
                        why = "Revit refused the number, so the sheet was taken back. Revit said: "
                            + oneLine(failure.Message);
                    }
                    if (why == null && wantedName != null)
                    {
                        try { sheet.Name = wantedName; }
                        catch (Exception failure)
                        {
                            why = "Revit refused the name \"" + wantedName + "\", so the sheet was taken "
                                + "back. Revit said: " + oneLine(failure.Message);
                        }
                    }
                    // READ BACK, number and name apart - they fail independently.
                    try { actualNumber = sheet.SheetNumber; } catch (Exception) { }
                    try { actualName = sheet.Name; } catch (Exception) { }
                    if (why == null && actualNumber != wantedNumber)
                        why = "Revit kept the number as \"" + (actualNumber ?? "") + "\", so the sheet was taken back";
                    if (why == null && wantedName != null && actualName != wantedName)
                        why = "Revit kept the name as \"" + (actualName ?? "") + "\" where \"" + wantedName
                            + "\" was asked, so the sheet was taken back";
                }
                if (why == null)
                {
                    // Kept - or, a preview, read back above and taken back now.
                    // Inside the try, so a commit Revit refuses is this sheet's
                    // refusal, not the whole call's.
                    if (previewOnly) step.RollBack();
                    else step.Commit();
                }
            }
            catch (Exception failure)
            {
                why = "Revit refused it, so the sheet was taken back. Revit said: " + oneLine(failure.Message);
            }

            if (why != null)
            {
                if (step.HasStarted() && !step.HasEnded()) step.RollBack();
                notMade(wantedNumber + ": " + why);
                continue;
            }

            if (previewOnly)
            {
                previewLines.Add(wantedNumber + " \"" + (actualName ?? "") + "\" on " + titleBlockLabel
                                 + ": would be made");
            }
            else
            {
                created.Add(sheet.Id);
                if (actualNumber != null) numberedAs[sheet.Id] = actualNumber;
                if (actualName != null) namedAs[sheet.Id] = actualName;
            }
            made++;
            taken[wantedNumber] = actualName ?? "";
        }
    }

    // VERSION 5: what kind of sheet was made, said when it was a placeholder,
    // so the answer is never read as sheets ready for views.
    if (placeholderAsked && made > 0)
        findings.Add(made + " sheet(s) " + (previewOnly ? "would be" : "were") + " made as PLACEHOLDER "
            + "sheets - Revit's own kind: a number and a name for the sheet list, with no title block "
            + "and no views.");

    if (previewOnly)
    {
        var shown = previewLines.Take(mostPreviewLines).ToList();
        if (previewLines.Count > mostPreviewLines) shown.Add("... and " + (previewLines.Count - mostPreviewLines) + " more");
        preview = string.Join("\n", shown.ToArray());
        findings.Insert(0, "PREVIEW - nothing was kept. " + made + " of " + numbers.Count
                           + " number(s) would be made as sheets; " + notCreated.Count + " would not be made.");
    }
}
}
}
else if (copying)
{
    if (previewOnly)
    {
        // VERSION 4. A preview is for a table or typed numbers, whose rows are
        // each made in a SubTransaction and taken back. The copy job makes its
        // copies in the call's own transaction, as version 3 did, so it cannot
        // take them back alone - and copies kept by a call that said "preview"
        // are the one wrong answer. Refused, nothing made.
        foreach (var element in elements) if (element != null) notDuplicated.Add(element.Id);
        notCreated.Add("previewOnly was asked with sheets handed in to copy - a copy run is not "
                     + "previewed, so nothing was created. Ask for the copies without previewOnly, "
                     + "and keep or undo them in Revit.");
    }
    else
    {
        // ---- VERSION 3: COPIES OF THE SHEETS HANDED IN, from DUPLICATE_SHEETS.
        //
        // Each source sheet gives a new one: the SOURCE'S OWN title block, the
        // number made by adding `numberSuffix` to the source's, and the name
        // `namePrefix` + the source's name. THE VIEWS ON THE SOURCE ARE NOT
        // COPIED - neither job here places anything on a sheet; DUPLICATE_VIEWS
        // and PLACE_VIEW_ON_SHEET do, and welding them in would hide two
        // read-backs inside a third.
        //
        // THE NUMBER IS CHECKED BEFORE ANY SHEET EXISTS, as for a typed number,
        // capitals ignored - and each number made is added as it goes, so two
        // sources in one batch cannot both claim one new number. Measured where
        // DUPLICATE_SHEETS was re-authored from: "Duplicated 0 of 1 sheet(s), 1
        // failed" while the sheet count went 2 to 3 and an orphan remained. So
        // nothing is created that would then have to be deleted.
        if (suffix.Length == 0)
        {
            // A copy numbered like its source collides with it, every time - and
            // a request that gave no numbers and no suffix may have meant "make a
            // new sheet" with sheets selected. Refused, nothing made.
            foreach (var element in elements) if (element != null) notDuplicated.Add(element.Id);
            notCreated.Add("No sheet numbers were given, and no numberSuffix to number copies of the "
                         + elements.Count(e => e != null) + " thing(s) handed in - a copy numbered like "
                         + "its source would clash with it. Give the numbers, or the suffix for the "
                         + "copies (\"-COPY\"). Nothing was created.");
        }
        else
        {
            if (!titleBlockLeftOut && askedTitleBlock.Length > 0)
                titleBlockProblem = "The copies take each source sheet's own title block, so \""
                    + askedTitleBlock + "\" was not used.";

            var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in new FilteredElementCollector(doc)
                         .OfClass(typeof(ViewSheet))
                         .Cast<ViewSheet>())
            {
                if (existing == null) continue;
                string number = null, holder = null;
                try { number = existing.SheetNumber; } catch { }
                try { holder = existing.Name; } catch { }
                if (string.IsNullOrEmpty(number) || taken.ContainsKey(number)) continue;
                taken[number] = holder ?? "";
            }

            foreach (var element in elements)
            {
                if (element == null) continue;
                var source = element as ViewSheet;
                if (source == null || source.IsTemplate)
                {
                    notDuplicated.Add(element.Id);
                    notCreated.Add("'" + element.Name + "': not a sheet, so there is nothing to copy");
                    continue;
                }

                var wantedNumber = (source.SheetNumber ?? "") + suffix;

                // THE CHECK THAT MAKES THE ORPHAN IMPOSSIBLE. Before anything exists.
                string holderName;
                if (taken.TryGetValue(wantedNumber, out holderName))
                {
                    numberTaken.Add(wantedNumber);
                    notDuplicated.Add(source.Id);
                    notCreated.Add(wantedNumber + ": already used by \"" + holderName + "\" - Revit "
                                 + "refuses a duplicate number, capitals or not");
                    continue;
                }

                // THE SOURCE'S TITLE BLOCK, so the copy matches its set. A
                // placeholder has none, and is a reservation in the register, not
                // a drawing; a sheet stripped of its title block has none either.
                // Neither is guessed at: an arbitrary title block would make a
                // sheet that does not match the set it belongs to.
                var sourceTitleBlockType = ElementId.InvalidElementId;
                if (!source.IsPlaceholder)
                {
                    foreach (var tb in new FilteredElementCollector(doc, source.Id)
                                           .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                           .WhereElementIsNotElementType())
                    {
                        sourceTitleBlockType = tb.GetTypeId();
                        break;
                    }
                }
                if (sourceTitleBlockType == ElementId.InvalidElementId)
                {
                    notDuplicated.Add(source.Id);
                    notCreated.Add(source.SheetNumber + ": "
                        + (source.IsPlaceholder ? "a placeholder sheet" : "a sheet with no title block")
                        + " - there is no title block to copy, so no copy was made");
                    continue;
                }

                ViewSheet made = null;
                string revitSaid = null;
                try { made = ViewSheet.Create(doc, sourceTitleBlockType); }
                catch (Exception failure) { revitSaid = failure.Message; }
                if (made == null)
                {
                    notDuplicated.Add(source.Id);
                    notCreated.Add(wantedNumber + ": Revit declined to create the sheet"
                        + (revitSaid == null ? "" : ". Revit said: " + string.Join(" ", revitSaid.Split())));
                    continue;
                }

                created.Add(made.Id);
                var wantedName = prefix + (source.Name ?? "");
                taken[wantedNumber] = wantedName;

                try { made.SheetNumber = wantedNumber; } catch { }
                try { made.Name = wantedName; } catch { }

                // READ BACK, number and name apart - they fail independently.
                string actualNumber = null, actualName = null;
                try { actualNumber = made.SheetNumber; } catch { }
                try { actualName = made.Name; } catch { }
                if (actualNumber != null) numberedAs[made.Id] = actualNumber;
                if (actualName != null) namedAs[made.Id] = actualName;

                // A COPY WHOSE NUMBER DID NOT TAKE IS NOT THE COPY ASKED FOR - but it
                // EXISTS, so it stays in `created` with its real number in
                // numberedAs, is named in `refused`, and its source is in
                // notDuplicated. The register must show the truth.
                if (!string.Equals(actualNumber, wantedNumber, StringComparison.OrdinalIgnoreCase))
                {
                    refused.Add(made.Id);
                    notDuplicated.Add(source.Id);
                }
            }
        }
    }
}
else
{
    notCreated.Add("No table of sheets, no sheet numbers and no sheets to copy were given, so "
                 + "there is nothing to create. Give a table (sheets), or the numbers and names, "
                 + "or select the sheets to copy with a numberSuffix.");
}
