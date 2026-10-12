// NOT STANDALONE. Assumes `doc` is in scope; leaves `lookupsChecked`,
// `tablesChecked`, `problemCount`, `report`, `typesRead`, `notAFamily` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A LOOKUP THAT MISSES IS SILENT. size_lookup answers with its DEFAULT when
// the table it names is not in the family, or the column it asks for is not
// in that table - and the family then builds at the default size with no
// warning anywhere. So every size_lookup formula is read and each thing it
// depends on is looked for: the Text parameter holding the table name, that
// name's value in EVERY type, the table, and the column.
//
// PER TYPE, BECAUSE THE TABLE NAME IS A TYPE VALUE. One type naming a table
// that is not there is one type building wrong; the others are fine. A
// family-wide answer would hide which.
//
// WHAT IT DOES NOT CHECK: whether each type's KEY value has a row in the
// table. Comparing a size in the family's units against a cell in the
// table's needs a units decision this read has not been given - so it is
// named as not checked, never passed. REPORT_FAMILY_SIZE_TABLE shows the
// rows to compare by eye.

var findings = new List<string>();
var problems = new List<string>();
var notes = new List<string>();
var lookupsChecked = 0;
var tablesChecked = 0;
var problemCount = 0;
var typesRead = 0;
var report = "";
var notAFamily = false;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front is a project, not a family being edited. A lookup "
        + "table and the formulas that read it live in the family - open the family for "
        + "editing and run this again. For every loaded family at once, read their tables "
        + "with REPORT_FAMILY_TABLES_IN_PROJECT.");
}
else
{
    // THE TABLES THIS FAMILY HOLDS, with their column names. A family with
    // no manager simply holds none - that is not a fault on its own.
    var tables = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    FamilySizeTableManager manager = null;
    try
    {
        manager = FamilySizeTableManager.GetFamilySizeTableManager(doc, doc.OwnerFamily.Id);
    }
    catch (Exception) { manager = null; }

    if (manager != null)
    {
        var names = new List<string>();
        try
        {
            foreach (var name in manager.GetAllSizeTableNames()) names.Add(name);
        }
        catch (Exception) { }

        foreach (var name in names)
        {
            FamilySizeTable table = null;
            try { table = manager.GetSizeTable(name); }
            catch (Exception) { table = null; }

            var headers = new List<string>();
            if (table == null)
            {
                problems.Add(string.Format("UNREADABLE TABLE: '{0}' is listed but could not "
                    + "be read", name));
                tables[name] = headers;
                continue;
            }

            tablesChecked++;
            var columnCount = 0;
            var rowCount = 0;
            try { columnCount = table.NumberOfColumns; rowCount = table.NumberOfRows; }
            catch (Exception) { }

            for (var c = 0; c < columnCount; c++)
            {
                var header = "";
                try
                {
                    var column = table.GetColumnHeader(c);
                    if (column != null) header = column.Name ?? "";
                }
                catch (Exception) { header = ""; }
                headers.Add(header);

                // THE FIRST COLUMN may carry no name - it holds the row names.
                // Any other column with none cannot be asked for by name.
                if (c > 0 && header.Trim().Length == 0)
                {
                    problems.Add(string.Format("UNNAMED COLUMN: table '{0}' column {1} has no "
                        + "name, so no size_lookup can ask for it", name, c + 1));
                }
            }

            if (rowCount == 0)
            {
                problems.Add(string.Format("EMPTY TABLE: '{0}' has no rows - every lookup "
                    + "into it answers with its default", name));
            }

            tables[name] = headers;
        }
    }

    // EVERY TYPE, so a table name is read the way each type has it.
    var familyManager = doc.FamilyManager;
    var types = new List<FamilyType>();
    try
    {
        foreach (FamilyType type in familyManager.Types)
        {
            if (type != null) types.Add(type);
        }
    }
    catch (Exception) { }
    if (types.Count == 0)
    {
        try
        {
            if (familyManager.CurrentType != null) types.Add(familyManager.CurrentType);
        }
        catch (Exception) { }
    }
    typesRead = types.Count;

    var parameters = new List<FamilyParameter>();
    try
    {
        foreach (var parameter in familyManager.GetParameters())
        {
            if (parameter != null && parameter.Definition != null) parameters.Add(parameter);
        }
    }
    catch (Exception) { }

    var tablesNamed = new HashSet<string>(StringComparer.Ordinal);

    foreach (var owner in parameters)
    {
        var formula = "";
        try { formula = owner.Formula ?? ""; }
        catch (Exception) { formula = ""; }
        if (formula.IndexOf("size_lookup", StringComparison.OrdinalIgnoreCase) < 0) continue;

        var ownerName = owner.Definition.Name;
        var at = 0;
        while (true)
        {
            var start = formula.IndexOf("size_lookup", at, StringComparison.OrdinalIgnoreCase);
            if (start < 0) break;
            at = start + "size_lookup".Length;

            var open = at;
            while (open < formula.Length && char.IsWhiteSpace(formula[open])) open++;
            if (open >= formula.Length || formula[open] != '(') continue;

            // THE ARGUMENTS, split at commas that are not inside brackets or
            // quotes - a default is often itself a sum, and a column name may
            // hold a comma.
            var args = new List<string>();
            var depth = 0;
            var quoted = false;
            var piece = new System.Text.StringBuilder();
            var close = -1;
            for (var i = open + 1; i < formula.Length; i++)
            {
                var ch = formula[i];
                if (ch == '"') { quoted = !quoted; piece.Append(ch); continue; }
                if (!quoted && ch == '(') depth++;
                if (!quoted && ch == ')')
                {
                    if (depth == 0) { close = i; break; }
                    depth--;
                }
                if (!quoted && depth == 0 && ch == ',')
                {
                    args.Add(piece.ToString().Trim());
                    piece.Length = 0;
                    continue;
                }
                piece.Append(ch);
            }
            if (close < 0)
            {
                problems.Add(string.Format("UNREADABLE FORMULA: '{0}' - its size_lookup has no "
                    + "closing bracket", ownerName));
                break;
            }
            args.Add(piece.ToString().Trim());
            at = close + 1;
            lookupsChecked++;

            if (args.Count < 4)
            {
                problems.Add(string.Format("TOO FEW ARGUMENTS: '{0}' - size_lookup needs the "
                    + "table, the column, a default and at least one key; it has {1}",
                    ownerName, args.Count));
                continue;
            }

            var tableArg = args[0];
            var columnArg = args[1];

            // THE COLUMN, as written. Only a quoted name can be checked here.
            var column = "";
            var columnQuoted = columnArg.Length >= 2 && columnArg.StartsWith("\"")
                && columnArg.EndsWith("\"");
            if (columnQuoted) column = columnArg.Substring(1, columnArg.Length - 2);
            else
            {
                notes.Add(string.Format("NOT CHECKED: '{0}' asks for column {1}, which is not a "
                    + "quoted name", ownerName, columnArg));
            }

            // THE TABLE NAME COMES FROM A TEXT PARAMETER, read per type.
            FamilyParameter holder = null;
            foreach (var candidate in parameters)
            {
                if (string.Equals(candidate.Definition.Name, tableArg, StringComparison.Ordinal))
                {
                    holder = candidate;
                    break;
                }
            }

            if (holder == null)
            {
                problems.Add(string.Format("NOT A PARAMETER: '{0}' names its table as {1}, and "
                    + "this family has no parameter by that name - the first argument must be "
                    + "a Text parameter holding the table's name", ownerName, tableArg));
                continue;
            }

            var isText = false;
            try { isText = holder.StorageType == StorageType.String; }
            catch (Exception) { isText = false; }
            if (!isText)
            {
                problems.Add(string.Format("NOT A TEXT PARAMETER: '{0}' reads its table name "
                    + "from '{1}', which is not a Text parameter", ownerName, tableArg));
                continue;
            }

            foreach (var type in types)
            {
                var typeName = "";
                try { typeName = type.Name ?? ""; }
                catch (Exception) { typeName = ""; }
                if (typeName.Trim().Length == 0) typeName = "<the default type>";

                var tableName = "";
                try { tableName = type.AsString(holder) ?? ""; }
                catch (Exception) { tableName = ""; }

                if (tableName.Trim().Length == 0)
                {
                    problems.Add(string.Format("BLANK TABLE NAME: type '{0}' - '{1}' is empty, "
                        + "so '{2}' takes its default", typeName, tableArg, ownerName));
                    continue;
                }

                tablesNamed.Add(tableName);

                if (!tables.ContainsKey(tableName))
                {
                    // WHETHER REVIT MATCHES A TABLE NAME REGARDLESS OF CASE is
                    // not established, so a case-only difference is said as
                    // that - never as a table that is not there.
                    var tableCaseOnly = "";
                    foreach (var held in tables.Keys)
                    {
                        if (string.Equals(held, tableName, StringComparison.OrdinalIgnoreCase))
                            tableCaseOnly = held;
                    }

                    if (tableCaseOnly.Length > 0)
                    {
                        tablesNamed.Add(tableCaseOnly);
                        problems.Add(string.Format("TABLE NAME CASE DIFFERS: type '{0}' - '{1}' "
                            + "names table '{2}'; the family's table is '{3}'", typeName, tableArg,
                            tableName, tableCaseOnly));
                    }
                    else
                    {
                        problems.Add(string.Format("MISSING TABLE: type '{0}' - '{1}' names table "
                            + "'{2}', which is not in this family, so '{3}' takes its default",
                            typeName, tableArg, tableName, ownerName));
                    }
                    continue;
                }

                if (!columnQuoted) continue;

                var headers = tables[tableName];
                var exact = false;
                var caseOnly = "";
                foreach (var header in headers)
                {
                    if (string.Equals(header, column, StringComparison.Ordinal)) { exact = true; break; }
                    if (string.Equals(header, column, StringComparison.OrdinalIgnoreCase)) caseOnly = header;
                }

                if (exact) continue;

                if (caseOnly.Length > 0)
                {
                    problems.Add(string.Format("COLUMN CASE DIFFERS: '{0}' asks table '{1}' for "
                        + "\"{2}\"; the table's column is \"{3}\"", ownerName, tableName, column,
                        caseOnly));
                }
                else
                {
                    problems.Add(string.Format("MISSING COLUMN: '{0}' asks table '{1}' for \"{2}\", "
                        + "which it has not got - its columns are {3}", ownerName, tableName,
                        column, string.Join(" | ", headers.ToArray())));
                }
            }
        }
    }

    // A TABLE NO TYPE NAMES is carried by the family and read by nothing. Not
    // a fault on its own - it may be kept for a type not built yet - so it is
    // a note, not a problem.
    foreach (var name in tables.Keys)
    {
        if (!tablesNamed.Contains(name))
        {
            notes.Add(string.Format("UNUSED TABLE: '{0}' is in the family but no type's "
                + "size_lookup names it", name));
        }
    }

    // A MESSAGE SAID ONCE. A missing table is the same finding for the same
    // formula however many types name it the same way, so lines repeat
    // only when they say something different.
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var unique = new List<string>();
    foreach (var line in problems)
    {
        if (seen.Add(line)) unique.Add(line);
    }
    problemCount = unique.Count;

    var lines = new List<string>(unique);
    lines.AddRange(notes);
    report = string.Join("  ||  ", lines.ToArray());

    if (lookupsChecked == 0 && tables.Count == 0)
    {
        findings.Add(string.Format("'{0}' has no lookup table and no size_lookup formula - "
            + "nothing here to check.", doc.Title));
    }
    else if (problemCount == 0)
    {
        findings.Add("No problem found in the tables or the formulas that read them.");
    }

    findings.Add("NOT CHECKED: whether each type's key value has a row in its table. "
        + "Read the rows with REPORT_FAMILY_SIZE_TABLE to compare.");
}

findings.Insert(0, string.Format("{0}; {1} size_lookup formula(s) and {2} table(s) checked "
    + "across {3} type(s); {4} problem(s)",
    notAFamily ? "NOT a family document" : string.Format("family '{0}'", doc.Title),
    lookupsChecked, tablesChecked, typesRead, problemCount));
