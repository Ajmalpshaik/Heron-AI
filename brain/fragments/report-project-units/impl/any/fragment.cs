// NOT STANDALONE. Assumes `doc` is in scope; leaves `unitsTable`,
// `unitTypes` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// MANAGE > PROJECT UNITS, READ. Every unit type Revit lets a project format -
// its discipline, its name, the units its values are shown in, the symbol
// after the number and the rounding - in Revit's own words, which come from
// LabelUtils and are never typed here; and the decimal symbol / digit grouping
// at the top of the dialog, written as the dialog's own example.
//
// TWO SHAPES OF THE UNITS API, ONE IMPLEMENTATION, CHOSEN AT RUN TIME.
// Revit 2020 names a unit type with the UnitType enum, a unit with
// DisplayUnitType and a symbol with UnitSymbolType. 2021 added ForgeTypeId
// and kept the enums, marked obsolete; 2022 removed them. Neither shape
// compiles on all eight releases, and the add-in compiles a fragment with no
// release symbols (FRAGMENT-ISSUES 5b-181), so every call that names either
// goes by reflection - the way REPORT_FAMILY_TYPE_CATALOG reads units. The
// ForgeTypeId route is taken wherever Units.GetModifiableSpecs exists (2021
// on), the enum route where it does not (2020); the discipline comes from
// UnitUtils.GetDiscipline from 2022 and from the UnitGroup enum before.
// Measured with tools/api-surface on all eight reference assemblies. The
// types are found by name in the namespace `Units` itself lives in.
//
// ONE STRING, BECAUSE A LIST REACHES THE CHAT CUT TO THREE ITEMS OF SIXTY
// CHARACTERS (the reply's own rule), and the whole table is the answer.

var unitsTable = "";
var unitTypes = 0;
var findings = "";

{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var statics = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
    var api = typeof(Units).Assembly;
    var space = typeof(Units).Namespace + ".";
    var units = doc.GetUnits();
    var notes = new List<string>();

    // Rounding in the dialog's words: "2 decimal places", "to the nearest 1/16",
    // or the increment itself.
    Func<double, string> roundingText = accuracy =>
    {
        for (var places = 0; places <= 12; places++)
        {
            var step = Math.Pow(10.0, -places);
            if (Math.Abs(accuracy - step) <= step * 1e-9)
                return places + (places == 1 ? " decimal place" : " decimal places");
        }
        if (accuracy > 0 && accuracy < 1)
        {
            var inverse = 1.0 / accuracy;
            var whole = Math.Round(inverse);
            if (Math.Abs(inverse - whole) < 1e-9 && whole >= 2 && ((long)whole & ((long)whole - 1)) == 0)
                return "to the nearest " + accuracy.ToString("0.############", invariant) + " (1/"
                    + ((long)whole).ToString(invariant) + ")";
        }
        return "to the nearest " + accuracy.ToString("0.############", invariant);
    };

    // ---- the two routes, as the same five questions -----------------------
    IEnumerable<object> specs = null;
    Func<object, string> specName = null;
    Func<object, string> disciplineOf = null;
    Func<object, FormatOptions> formatOf = null;
    Func<FormatOptions, string> unitName = null;
    Func<FormatOptions, string> symbolName = null;
    var route = "";

    var forgeType = api.GetType(space + "ForgeTypeId");
    var modifiableSpecs = typeof(Units).GetMethod("GetModifiableSpecs", statics, null, Type.EmptyTypes, null);
    if (forgeType != null && modifiableSpecs != null)
    {
        route = "Revit's ForgeTypeId units (2021 and later)";
        var one = new[] { forgeType };
        var labelSpec = typeof(LabelUtils).GetMethod("GetLabelForSpec", statics, null, one, null);
        var labelUnit = typeof(LabelUtils).GetMethod("GetLabelForUnit", statics, null, one, null);
        var labelSymbol = typeof(LabelUtils).GetMethod("GetLabelForSymbol", statics, null, one, null);
        var labelDiscipline = typeof(LabelUtils).GetMethod("GetLabelForDiscipline", statics, null, one, null);
        var discipline = typeof(UnitUtils).GetMethod("GetDiscipline", statics, null, one, null);
        var group = typeof(UnitUtils).GetMethod("GetUnitGroup", statics, null, one, null);
        var getFormat = typeof(Units).GetMethod("GetFormatOptions", one);
        var getUnit = typeof(FormatOptions).GetMethod("GetUnitTypeId", Type.EmptyTypes);
        var getSymbol = typeof(FormatOptions).GetMethod("GetSymbolTypeId", Type.EmptyTypes);
        var isEmpty = forgeType.GetMethod("Empty", Type.EmptyTypes);

        specs = ((System.Collections.IEnumerable)modifiableSpecs.Invoke(null, null)).Cast<object>().ToList();
        specName = spec => labelSpec.Invoke(null, new[] { spec }) as string;
        disciplineOf = spec =>
        {
            if (discipline != null && labelDiscipline != null)
                return labelDiscipline.Invoke(null, new[] { discipline.Invoke(null, new[] { spec }) }) as string;
            if (group != null) return group.Invoke(null, new[] { spec }).ToString();
            return "";
        };
        formatOf = spec => getFormat.Invoke(units, new[] { spec }) as FormatOptions;
        unitName = format => labelUnit.Invoke(null, new[] { getUnit.Invoke(format, null) }) as string;
        symbolName = format =>
        {
            var symbol = getSymbol.Invoke(format, null);
            if (symbol == null || (bool)isEmpty.Invoke(symbol, null)) return "";
            return labelSymbol.Invoke(null, new[] { symbol }) as string;
        };
    }
    else
    {
        route = "Revit 2020's UnitType names";
        var unitType = api.GetType(space + "UnitType");
        var displayType = api.GetType(space + "DisplayUnitType");
        var symbolType = api.GetType(space + "UnitSymbolType");
        var modifiable = typeof(Units).GetMethod("GetModifiableUnitTypes", statics, null, Type.EmptyTypes, null);
        Func<Type, System.Reflection.MethodInfo> labelFor = t =>
            typeof(LabelUtils).GetMethod("GetLabelFor", statics, null, new[] { t }, null);
        var labelType = labelFor(unitType);
        var labelDisplay = labelFor(displayType);
        var labelSymbol = labelFor(symbolType);
        var group = typeof(UnitUtils).GetMethod("GetUnitGroup", statics, null, new[] { unitType }, null);
        var getFormat = typeof(Units).GetMethod("GetFormatOptions", new[] { unitType });
        var displayUnits = typeof(FormatOptions).GetProperty("DisplayUnits");
        var unitSymbol = typeof(FormatOptions).GetProperty("UnitSymbol");

        specs = ((System.Collections.IEnumerable)modifiable.Invoke(null, null)).Cast<object>().ToList();
        specName = spec => labelType.Invoke(null, new[] { spec }) as string;
        disciplineOf = spec => group == null ? "" : group.Invoke(null, new[] { spec }).ToString();
        formatOf = spec => getFormat.Invoke(units, new[] { spec }) as FormatOptions;
        unitName = format => labelDisplay.Invoke(null, new[] { displayUnits.GetValue(format) }) as string;
        symbolName = format =>
        {
            var symbol = unitSymbol.GetValue(format);
            if (symbol == null || symbol.ToString() == "UST_NONE") return "";
            return labelSymbol.Invoke(null, new[] { symbol }) as string;
        };
    }

    // ---- every unit type, by discipline in the dialog's order ---------------
    var order = new[] { "Common", "Structural", "HVAC", "Electrical", "Piping", "Energy", "Infrastructure" };
    var rows = new List<Tuple<int, string, string, string>>();   // rank, discipline, name, the line
    foreach (var spec in specs)
    {
        string name = null;
        try
        {
            name = specName(spec);
            var inDiscipline = disciplineOf(spec) ?? "";
            var format = formatOf(spec);
            var symbol = symbolName(format) ?? "";
            var line = inDiscipline + ": " + name + " = " + unitName(format)
                + (symbol.Length > 0 ? " (" + symbol + ")" : " (no symbol)")
                + ", " + roundingText(format.Accuracy);
            var rank = Array.IndexOf(order, inDiscipline);
            rows.Add(Tuple.Create(rank < 0 ? order.Length : rank, inDiscipline, name ?? "", line));
        }
        catch (Exception failure)
        {
            notes.Add("one unit type" + (name == null ? "" : " (" + name + ")") + " could not be read: "
                + (failure.InnerException ?? failure).Message.TrimEnd('.'));
        }
    }
    rows = rows.OrderBy(r => r.Item1).ThenBy(r => r.Item2).ThenBy(r => r.Item3, StringComparer.OrdinalIgnoreCase).ToList();
    unitTypes = rows.Count;

    // The decimal symbol and digit grouping, as the dialog's own example.
    var grouping = "";
    try
    {
        var groupChar = new[] { ".", ",", " ", "'" }[Math.Min(3, (int)units.DigitGroupingSymbol)];
        var decimalChar = (int)units.DecimalSymbol == 0 ? "." : ",";
        grouping = ((int)units.DigitGroupingAmount == 0
                       ? "12" + groupChar + "34" + groupChar + "56" + groupChar + "789"
                       : "123" + groupChar + "456" + groupChar + "789") + decimalChar + "00";
    }
    catch (Exception failure)
    {
        notes.Add("the decimal symbol and digit grouping could not be read: "
            + (failure.InnerException ?? failure).Message.TrimEnd('.'));
    }

    var lines = new List<string>();
    if (grouping.Length > 0) lines.Add("Decimal symbol/digit grouping = " + grouping);
    lines.AddRange(rows.Select(r => r.Item4));
    unitsTable = string.Join("  ||  ", lines.ToArray());

    var disciplines = rows.Select(r => r.Item2).Distinct().ToList();
    notes.Insert(0, unitTypes + " unit type(s) in " + disciplines.Count + " discipline(s) ("
        + string.Join(", ", disciplines.ToArray()) + ") of '" + doc.Title + "', read through " + route
        + ". Each is 'discipline: unit type = units (symbol), rounding', in Revit's own words");
    findings = string.Join("; ", notes.ToArray()) + ".";
}
