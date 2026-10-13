// NOT STANDALONE. Assumes `doc` is in scope; leaves `lookupsChecked`,
// `tablesChecked`, `rowSearches`, `problemCount`, `report`, `typesRead`,
// `notAFamily` and `findings` behind.
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
// VERSION 2: WHETHER EACH TYPE'S KEY HAS A ROW. A table that is there and a
// column that is there still answer with the default when no row holds the
// type's size. So each key that is a TYPE parameter holding a number is read
// in every type, converted INTO THE COLUMN'S OWN UNIT - the unit the table's
// header declares, which is what Revit reads the cell in - and looked for in
// the rows, to 0.0001 of that unit.
//
// KEYS ARE MATCHED FROM THE SECOND COLUMN ON: the first key against the
// second column, the next against the third. The first column holds row
// names - the reading seen in Revit 2024 (docs/43 section 5); the negative
// half of that is still owed, FRAGMENT-ISSUES row 5b-281.
//
// WHAT IS STILL NOT CHECKED, and said so: a key that is an INSTANCE parameter
// - its value is set where the family is placed, not in the type; a key that
// is an expression rather than one parameter; a key that is text; and a
// measured key against a column that declares no unit, where comparing would
// mean guessing the unit.

var findings = new List<string>();
var problems = new List<string>();
var notes = new List<string>();
var lookupsChecked = 0;
var tablesChecked = 0;
var rowSearches = 0;
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
    // Per table: each column's conversion from Revit's internal units into
    // the unit its header declares (null for the first column, and for one
    // with no unit), and every cell read as a number in that unit.
    var tableUnits = new Dictionary<string, List<Func<double, double>>>(StringComparer.Ordinal);
    var tableCells = new Dictionary<string, List<double?[]>>(StringComparer.Ordinal);

    // THE UNIT A COLUMN DECLARES. Revit 2021 moved units from an enum to
    // ForgeTypeId; 2020 has only the enum. A column with no unit - "other"
    // in its header - answers null, and a measured key is not compared
    // against it.
    Func<FamilySizeTableColumn, Func<double, double>> columnUnit = column =>
    {
        if (column == null) return null;
        try
        {
#if REVIT2020
            var unit = column.DisplayUnitType;
            if (unit == DisplayUnitType.DUT_GENERAL || unit == DisplayUnitType.DUT_UNDEFINED
                || unit == DisplayUnitType.DUT_CUSTOM) return null;
            return value => UnitUtils.ConvertFromInternalUnits(value, unit);
#else
            var unit = column.GetUnitTypeId();
            if (unit == null || unit.Empty()) return null;
            if ((unit.TypeId ?? "").StartsWith("autodesk.unit.unit:general",
                StringComparison.OrdinalIgnoreCase)) return null;
            return value => UnitUtils.ConvertFromInternalUnits(value, unit);
#endif
        }
        catch (Exception) { return null; }
    };

    // A CELL AS A NUMBER. AsValueString writes the cell in its column's own
    // unit; invariant first, then this machine's own decimal separator.
    Func<string, double?> asNumber = text =>
    {
        var trimmed = (text ?? "").Trim();
        double value;
        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value)) return value;
        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.CurrentCulture, out value)) return value;
        return null;
    };

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
            var units = new List<Func<double, double>>();
            var cells = new List<double?[]>();
            tableUnits[name] = units;
            tableCells[name] = cells;
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
                Func<double, double> unit = null;
                try
                {
                    var column = table.GetColumnHeader(c);
                    if (column != null) header = column.Name ?? "";
                    if (c > 0) unit = columnUnit(column);
                }
                catch (Exception) { header = ""; }
                headers.Add(header);
                units.Add(unit);

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

            // EVERY CELL BUT THE FIRST COLUMN'S - the row names, never a key.
            for (var r = 0; r < rowCount; r++)
            {
                var row = new double?[columnCount];
                for (var c = 1; c < columnCount; c++)
                {
                    var text = "";
                    try { text = table.AsValueString(r, c) ?? ""; }
                    catch (Exception) { text = ""; }
                    row[c] = asNumber(text);
                }
                cells.Add(row);
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
    // A missed row, said once with every formula it leaves at its default -
    // a fitting's height and its outside diameter usually miss together.
    var missedRows = new Dictionary<string, List<string>>(StringComparer.Ordinal);

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

            // THE KEYS. Each must be one TYPE parameter holding a number for
            // its value in a type to be looked for in the rows; anything else
            // is said as not checked, once, for this formula.
            var keyParameters = new List<FamilyParameter>();
            var keysNotChecked = "";
            for (var k = 3; k < args.Count; k++)
            {
                FamilyParameter key = null;
                foreach (var candidate in parameters)
                {
                    if (string.Equals(candidate.Definition.Name, args[k], StringComparison.Ordinal))
                    {
                        key = candidate;
                        break;
                    }
                }

                if (key == null)
                {
                    keysNotChecked = string.Format("its key {0} is not one parameter's name", args[k]);
                    break;
                }

                var instance = false;
                try { instance = key.IsInstance; }
                catch (Exception) { instance = false; }
                if (instance)
                {
                    keysNotChecked = string.Format("its key '{0}' is an instance parameter, whose "
                        + "value is set where the family is placed", args[k]);
                    break;
                }

                var storage = StorageType.None;
                try { storage = key.StorageType; }
                catch (Exception) { storage = StorageType.None; }
                if (storage != StorageType.Double && storage != StorageType.Integer)
                {
                    keysNotChecked = string.Format("its key '{0}' does not hold a number", args[k]);
                    break;
                }

                keyParameters.Add(key);
            }

            if (keysNotChecked.Length > 0)
            {
                notes.Add(string.Format("ROWS NOT CHECKED: '{0}' - {1}", ownerName, keysNotChecked));
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

                if (columnQuoted)
                {
                    var headers = tables[tableName];
                    var exact = false;
                    var caseOnly = "";
                    foreach (var header in headers)
                    {
                        if (string.Equals(header, column, StringComparison.Ordinal)) { exact = true; break; }
                        if (string.Equals(header, column, StringComparison.OrdinalIgnoreCase)) caseOnly = header;
                    }

                    if (!exact)
                    {
                        if (caseOnly.Length > 0)
                        {
                            problems.Add(string.Format("COLUMN CASE DIFFERS: '{0}' asks table '{1}' "
                                + "for \"{2}\"; the table's column is \"{3}\"", ownerName, tableName,
                                column, caseOnly));
                        }
                        else
                        {
                            problems.Add(string.Format("MISSING COLUMN: '{0}' asks table '{1}' for "
                                + "\"{2}\", which it has not got - its columns are {3}", ownerName,
                                tableName, column, string.Join(" | ", headers.ToArray())));
                        }
                        continue;
                    }
                }

                // THE ROW. Each key's value in this type, in the unit its
                // column declares, looked for across the rows.
                if (keysNotChecked.Length > 0 || keyParameters.Count == 0) continue;

                var keyUnits = tableUnits[tableName];
                var keyRows = tableCells[tableName];
                // An unreadable or empty table is already said above; a
                // missing row in it would only say the same thing again.
                if (keyUnits.Count == 0 || keyRows.Count == 0) continue;
                if (keyParameters.Count + 1 > keyUnits.Count)
                {
                    problems.Add(string.Format("TOO MANY KEYS: '{0}' looks table '{1}' up by {2} "
                        + "key(s), and the table has {3} column(s) after its first", ownerName,
                        tableName, keyParameters.Count, Math.Max(0, keyUnits.Count - 1)));
                    continue;
                }

                var wanted = new double[keyParameters.Count];
                var said = new List<string>();
                var whyNot = "";
                for (var k = 0; k < keyParameters.Count; k++)
                {
                    var key = keyParameters[k];
                    var toUnit = keyUnits[k + 1];
                    double? keyValue = null;

                    if (key.StorageType == StorageType.Double)
                    {
                        if (toUnit == null)
                        {
                            whyNot = string.Format("column {0} of '{1}' declares no unit, so its "
                                + "measured key '{2}' is not compared", k + 2, tableName,
                                key.Definition.Name);
                            break;
                        }
                        double? stored = null;
                        try { stored = type.AsDouble(key); }
                        catch (Exception) { stored = null; }
                        if (stored.HasValue)
                        {
                            try { keyValue = toUnit(stored.Value); }
                            catch (Exception) { keyValue = null; }
                        }
                    }
                    else
                    {
                        if (toUnit != null)
                        {
                            whyNot = string.Format("column {0} of '{1}' declares a unit, and its "
                                + "key '{2}' is a whole number", k + 2, tableName, key.Definition.Name);
                            break;
                        }
                        int? stored = null;
                        try { stored = type.AsInteger(key); }
                        catch (Exception) { stored = null; }
                        if (stored.HasValue) keyValue = stored.Value;
                    }

                    if (!keyValue.HasValue)
                    {
                        whyNot = string.Format("type '{0}' has no value for '{1}'", typeName,
                            key.Definition.Name);
                        break;
                    }

                    wanted[k] = keyValue.Value;
                    said.Add(string.Format("{0} = {1}", key.Definition.Name,
                        keyValue.Value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)));
                }

                if (whyNot.Length > 0)
                {
                    notes.Add(string.Format("ROWS NOT CHECKED: '{0}' - {1}", ownerName, whyNot));
                    continue;
                }

                rowSearches++;
                var rowFound = false;
                foreach (var candidateRow in keyRows)
                {
                    var allMatch = true;
                    for (var k = 0; k < wanted.Length; k++)
                    {
                        var cell = candidateRow[k + 1];
                        if (!cell.HasValue || Math.Abs(cell.Value - wanted[k]) > 0.0001)
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch) { rowFound = true; break; }
                }

                if (!rowFound)
                {
                    var missedAt = string.Format("type '{0}' - table '{1}' has no row for {2} "
                        + "(in the units its columns declare)", typeName, tableName,
                        string.Join(", ", said.ToArray()));
                    if (!missedRows.ContainsKey(missedAt)) missedRows[missedAt] = new List<string>();
                    if (!missedRows[missedAt].Contains(ownerName)) missedRows[missedAt].Add(ownerName);
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

    foreach (var missed in missedRows)
    {
        problems.Add(string.Format("NO ROW: {0}, so {1} take(s) the default", missed.Key,
            string.Join(", ", missed.Value.Select(owned => "'" + owned + "'").ToArray())));
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
    foreach (var line in notes)
    {
        if (seen.Add(line)) lines.Add(line);
    }
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

    if (rowSearches > 0)
    {
        findings.Add(string.Format("{0} row search(es): keys matched from each table's second "
            + "column on, to 0.0001 in the unit each column declares - the reading seen in Revit "
            + "2024 (docs/43 section 5); FRAGMENT-ISSUES row 5b-281 is still open.", rowSearches));
    }
}

findings.Insert(0, string.Format("{0}; {1} size_lookup formula(s) and {2} table(s) checked "
    + "across {3} type(s), {4} row search(es); {5} problem(s)",
    notAFamily ? "NOT a family document" : string.Format("family '{0}'", doc.Title),
    lookupsChecked, tablesChecked, typesRead, rowSearches, problemCount));
