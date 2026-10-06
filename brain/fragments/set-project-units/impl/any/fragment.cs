// NOT STANDALONE. Assumes `doc` and `settings` are in scope, and leaves
// `changed`, `alreadyThat`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts every unit back.
//
// MANAGE > PROJECT UNITS, SET. One table, a unit type per entry:
//
//   Cooling Load=Btu/h; Heating Load=Btu/h; Air Flow=CFM
//   HVAC: Temperature=Celsius, 1 decimal place; Length=Millimeters, 0 decimal places
//   decimal symbol/digit grouping=123 456 789,00
//
// THE KEY is a unit type as the dialog names it - "Cooling Load", "Air Flow"
// (spaces and case do not matter) - with its discipline in front, "HVAC:
// Temperature", or behind, "Temperature (HVAC)", when several disciplines
// have one of that name; a bare shared name is refused with every discipline
// that has it. "decimal symbol/digit grouping" takes the dialog's own example
// of a number, "123,456,789.00".
//
// THE VALUE is the units, the symbol and the rounding - any of them, commas
// between, each in Revit's own words: the unit's name ("British thermal
// units per hour"; metre/meter, litre/liter and a plural s do not matter), or
// its symbol alone when only one unit has it ("Btu/h", "CFM"), "no symbol",
// and "N decimal places", "rounding 0.5" or "to the nearest 1/16". A name
// Revit does not offer for that unit type is refused with every one it does.
//
// WHAT IS NOT SAID IS KEPT. Each unit type starts from a COPY of its own
// format, so the dialog's tick boxes (suppress trailing zeros, digit
// grouping, plus sign, spaces) stay as they were; one the new units cannot
// have is cleared, and said. A new unit with no symbol named keeps the old
// symbol when the new unit offers it, else takes the first symbol the unit
// offers; with no rounding named it keeps the old rounding when the new unit
// accepts it, else takes Revit's own default for that unit. Each choice is
// said.
//
// ONE WRITE. Every format is built and checked on the Units object in memory
// - Revit validates each one there - and the model is written once, by
// Document.SetUnits. So a refusal of that one write, with no transaction
// open, is Revit's own words and the plan: nothing was stored. Everything is
// then read back from a fresh Document.GetUnits, and a value that reads back
// different throws, so the add-in rolls it back.
//
// TWO SHAPES OF THE UNITS API, ONE IMPLEMENTATION: the reasoning and the
// measurement are REPORT_PROJECT_UNITS', which reads what this writes. The
// ForgeTypeId route wherever Units.GetModifiableSpecs exists (2021 on), the
// enum route (UnitType, DisplayUnitType, UnitSymbolType) on 2020, every call
// that names either by reflection.

var changed = "";
var alreadyThat = "";
var findings = "";
var refused = "";

{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var statics = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
    var api = typeof(Units).Assembly;
    var space = typeof(Units).Namespace + ".";
    var units = doc.GetUnits();
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
    // A unit's name compared loosely: case, spacing, metre/litre and a plural s.
    Func<string, string> loose = text =>
    {
        var t = (text ?? "").ToLowerInvariant().Replace("metre", "meter").Replace("litre", "liter");
        var parts = t.Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(w => w.Length > 3 && w.EndsWith("s") ? w.Substring(0, w.Length - 1) : w);
        return string.Join(" ", parts.ToArray());
    };
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
    // A rounding in the dialog's words, as the increment: null when the text
    // is not a rounding at all, NaN when it is one that cannot be read.
    Func<string, double?> roundingOf = text =>
    {
        var t = string.Join(" ", (text ?? "").Trim().ToLowerInvariant().Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
        if (t.EndsWith(" decimal places") || t.EndsWith(" decimal place"))
        {
            int places;
            var count = t.Substring(0, t.IndexOf(" decimal place"));
            if (!int.TryParse(count, System.Globalization.NumberStyles.None, invariant, out places) || places > 12) return double.NaN;
            return Math.Pow(10.0, -places);
        }
        string number;
        if (t.StartsWith("rounding ")) number = t.Substring(9).Trim();
        else if (t.StartsWith("to the nearest ")) number = t.Substring(15).Trim();
        else return null;
        var slash = number.IndexOf('/');
        double top, bottom;
        if (slash > 0)
        {
            if (!double.TryParse(number.Substring(0, slash), System.Globalization.NumberStyles.Float, invariant, out top)
                || !double.TryParse(number.Substring(slash + 1), System.Globalization.NumberStyles.Float, invariant, out bottom)
                || bottom == 0) return double.NaN;
            return top / bottom;
        }
        if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariant, out top)) return double.NaN;
        return top;
    };

    // ---- the two routes, as the same questions ----------------------------
    List<object> specs = null;
    Func<object, string> specName = null;
    Func<object, string> disciplineOf = null;
    Func<Units, object, FormatOptions> formatOf = null;
    Func<FormatOptions, object> unitOf = null;
    Func<FormatOptions, object> symbolOf = null;
    Func<object, string> unitName = null;
    Func<object, string> symbolName = null;            // "" for no symbol
    Func<object, List<object>> validUnits = null;      // of a unit type
    Func<object, List<object>> validSymbols = null;    // of a unit
    Func<object, FormatOptions> revitDefault = null;   // a new format for a unit
    Action<FormatOptions, object> putUnit = null;
    Action<FormatOptions, object> putSymbol = null;
    Action<Units, object, FormatOptions> putFormat = null;
    object noSymbol = null;
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
        var setFormat = typeof(Units).GetMethod("SetFormatOptions", new[] { forgeType, typeof(FormatOptions) });
        var getUnit = typeof(FormatOptions).GetMethod("GetUnitTypeId", Type.EmptyTypes);
        var getSymbol = typeof(FormatOptions).GetMethod("GetSymbolTypeId", Type.EmptyTypes);
        var setUnit = typeof(FormatOptions).GetMethod("SetUnitTypeId", one);
        var setSymbol = typeof(FormatOptions).GetMethod("SetSymbolTypeId", one);
        var unitsOfSpec = typeof(UnitUtils).GetMethod("GetValidUnits", statics, null, one, null);
        var symbolsOfUnit = typeof(FormatOptions).GetMethod("GetValidSymbols", statics, null, one, null);
        var withUnit = typeof(FormatOptions).GetConstructor(one);
        var isEmpty = forgeType.GetMethod("Empty", Type.EmptyTypes);
        Func<object, bool> empty = id => id == null || (bool)isEmpty.Invoke(id, null);
        noSymbol = Activator.CreateInstance(forgeType);

        specs = ((System.Collections.IEnumerable)modifiableSpecs.Invoke(null, null)).Cast<object>().ToList();
        specName = spec => labelSpec.Invoke(null, new[] { spec }) as string;
        disciplineOf = spec =>
        {
            if (discipline != null && labelDiscipline != null)
                return labelDiscipline.Invoke(null, new[] { discipline.Invoke(null, new[] { spec }) }) as string;
            if (group != null) return group.Invoke(null, new[] { spec }).ToString();
            return "";
        };
        formatOf = (from, spec) => getFormat.Invoke(from, new[] { spec }) as FormatOptions;
        unitOf = format => getUnit.Invoke(format, null);
        symbolOf = format => getSymbol.Invoke(format, null);
        unitName = unit => labelUnit.Invoke(null, new[] { unit }) as string;
        symbolName = symbol => empty(symbol) ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
        validUnits = spec => ((System.Collections.IEnumerable)unitsOfSpec.Invoke(null, new[] { spec })).Cast<object>().ToList();
        validSymbols = unit => ((System.Collections.IEnumerable)symbolsOfUnit.Invoke(null, new[] { unit })).Cast<object>().ToList();
        revitDefault = unit => withUnit.Invoke(new[] { unit }) as FormatOptions;
        putUnit = (format, unit) => setUnit.Invoke(format, new[] { unit });
        putSymbol = (format, symbol) => setSymbol.Invoke(format, new[] { symbol });
        putFormat = (into, spec, format) => setFormat.Invoke(into, new object[] { spec, format });
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
        var setFormat = typeof(Units).GetMethod("SetFormatOptions", new[] { unitType, typeof(FormatOptions) });
        var displayUnits = typeof(FormatOptions).GetProperty("DisplayUnits");
        var unitSymbol = typeof(FormatOptions).GetProperty("UnitSymbol");
        var unitsOfType = typeof(UnitUtils).GetMethod("GetValidDisplayUnits", statics, null, new[] { unitType }, null);
        var symbolsOfUnit = typeof(FormatOptions).GetMethod("GetValidUnitSymbols", statics, null, new[] { displayType }, null);
        var withUnit = typeof(FormatOptions).GetConstructor(new[] { displayType });
        noSymbol = Enum.Parse(symbolType, "UST_NONE");

        specs = ((System.Collections.IEnumerable)modifiable.Invoke(null, null)).Cast<object>().ToList();
        specName = spec => labelType.Invoke(null, new[] { spec }) as string;
        disciplineOf = spec => group == null ? "" : group.Invoke(null, new[] { spec }).ToString();
        formatOf = (from, spec) => getFormat.Invoke(from, new[] { spec }) as FormatOptions;
        unitOf = format => displayUnits.GetValue(format);
        symbolOf = format => unitSymbol.GetValue(format);
        unitName = unit => labelDisplay.Invoke(null, new[] { unit }) as string;
        symbolName = symbol => symbol == null || symbol.ToString() == "UST_NONE"
            ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
        validUnits = spec => ((System.Collections.IEnumerable)unitsOfType.Invoke(null, new[] { spec })).Cast<object>().ToList();
        validSymbols = unit => ((System.Collections.IEnumerable)symbolsOfUnit.Invoke(null, new[] { unit })).Cast<object>().ToList();
        revitDefault = unit => withUnit.Invoke(new[] { unit }) as FormatOptions;
        putUnit = (format, unit) => displayUnits.SetValue(format, unit);
        putSymbol = (format, symbol) => unitSymbol.SetValue(format, symbol);
        putFormat = (into, spec, format) => setFormat.Invoke(into, new object[] { spec, format });
    }

    Func<FormatOptions, string> describe = format =>
    {
        var symbol = symbolName(symbolOf(format));
        return unitName(unitOf(format)) + (symbol.Length > 0 ? " (" + symbol + ")" : " (no symbol)")
            + ", " + roundingText(format.Accuracy);
    };
    // Every unit Revit offers for a unit type, with its symbols, for a refusal.
    Func<List<object>, string> offers = offered => string.Join(", ", offered.Select(u =>
    {
        var symbols = validSymbols(u).Select(s => symbolName(s)).Where(s => s.Length > 0).ToArray();
        return unitName(u) + (symbols.Length > 0 ? " (" + string.Join("/", symbols) + ")" : "");
    }).ToArray());

    // ---- the unit types this project can format ---------------------------
    var catalogue = new List<Tuple<object, string, string>>();   // spec, discipline, name
    foreach (var spec in specs)
    {
        try { catalogue.Add(Tuple.Create(spec, disciplineOf(spec) ?? "", specName(spec) ?? "")); }
        catch (Exception) { }
    }
    Func<Tuple<object, string, string>, string> fullName = row => row.Item2 + ": " + row.Item3;

    // ---- what is asked ------------------------------------------------------
    var problems = new List<string>();
    var notes = new List<string>();
    var plans = new List<Tuple<Tuple<object, string, string>, FormatOptions, string, string>>();   // row, new format, before, after
    var already = new List<string>();
    var seen = new HashSet<string>();
    DecimalSymbol? wantDecimal = null;
    DigitGroupingSymbol? wantGroup = null;
    DigitGroupingAmount? wantAmount = null;
    Func<DecimalSymbol, DigitGroupingSymbol, DigitGroupingAmount, string> example = (d, g, a) =>
    {
        var groupChar = new[] { ".", ",", " ", "'" }[Math.Min(3, (int)g)];
        return (a == DigitGroupingAmount.Two
                   ? "12" + groupChar + "34" + groupChar + "56" + groupChar + "789"
                   : "123" + groupChar + "456" + groupChar + "789") + (d == DecimalSymbol.Dot ? "." : ",") + "00";
    };
    var groupingKeys = new[] { "decimalsymboldigitgrouping", "decimalsymbolanddigitgrouping", "decimalsymbol", "digitgrouping" };

    var asked = 0;
    if (settings != null)
    {
        foreach (var pair in settings)
        {
            var key = (pair.Key ?? "").Trim();
            var value = (pair.Value ?? "").Trim();
            if (value.Length == 0) continue;
            asked++;

            // -- the decimal symbol and digit grouping, as the dialog's example
            if (groupingKeys.Contains(squash(key)))
            {
                var examples = new List<string>();
                foreach (var d in new[] { DecimalSymbol.Dot, DecimalSymbol.Comma })
                    foreach (var g in new[] { DigitGroupingSymbol.Comma, DigitGroupingSymbol.Dot, DigitGroupingSymbol.Space, DigitGroupingSymbol.Apostrophe })
                        foreach (var a in new[] { DigitGroupingAmount.Three, DigitGroupingAmount.Two })
                        {
                            if ((int)d == (int)g) continue;
                            var text = example(d, g, a);
                            examples.Add(text);
                            if (text == value) { wantDecimal = d; wantGroup = g; wantAmount = a; }
                        }
                if (wantDecimal == null)
                    problems.Add("decimal symbol/digit grouping takes the dialog's example of a number - one of "
                        + string.Join(", ", examples.Select(e => "'" + e + "'").ToArray()) + " - not '" + value + "'");
                continue;
            }

            // -- the unit type
            var k = squash(key);
            var matches = catalogue.Where(entry => squash(entry.Item3) == k || squash(entry.Item2 + entry.Item3) == k
                                                || squash(entry.Item3 + entry.Item2) == k).ToList();
            if (matches.Count == 0)
            {
                var keyWords = key.ToLowerInvariant().Split(new[] { ' ', ':', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Where(w => w.Length >= 4).ToList();
                var near = catalogue.Where(entry => keyWords.Any(w => entry.Item3.ToLowerInvariant().Contains(w)))
                                    .Select(fullName).Take(12).ToList();
                problems.Add("'" + key + "' is not a unit type in Project Units"
                    + (near.Count > 0 ? " - near: " + string.Join("; ", near.ToArray()) : ""));
                continue;
            }
            if (matches.Count > 1)
            {
                problems.Add("'" + key + "' is a unit type in " + matches.Count + " disciplines - "
                    + string.Join("; ", matches.Select(fullName).ToArray()) + ". Name the discipline, as '"
                    + fullName(matches[0]) + "'");
                continue;
            }
            var row = matches[0];
            if (!seen.Add(fullName(row))) { problems.Add(fullName(row) + " is named twice - say it once"); continue; }

            // -- the value: units, symbol, rounding, in any order
            double? rounding = null;
            var symbolNone = false;
            var given = new List<string>();
            foreach (var part in value.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                var r = roundingOf(part);
                if (r.HasValue)
                {
                    if (double.IsNaN(r.Value) || r.Value <= 0)
                        problems.Add("'" + part + "' for " + fullName(row) + " is not a rounding Revit takes - "
                            + "'2 decimal places', 'rounding 0.5' or 'to the nearest 1/16'");
                    else rounding = r;
                    continue;
                }
                var s = squash(part);
                if (s == "nosymbol" || s == "none" || s == "withoutsymbol") { symbolNone = true; continue; }
                given.Add(part);
            }

            FormatOptions current;
            List<object> offered;
            try
            {
                current = formatOf(units, row.Item1);
                offered = validUnits(row.Item1);
            }
            catch (Exception failure) { problems.Add(fullName(row) + " could not be read: " + revitSaid(failure)); continue; }

            object unit = null;
            object symbol = null;
            var bad = false;
            var rowNotes = new List<string>();
            foreach (var word in given)
            {
                var named = offered.Where(u => loose(unitName(u)) == loose(word)).ToList();
                if (named.Count == 1)
                {
                    if (unit == null) { unit = named[0]; continue; }
                    if (unitName(unit) == unitName(named[0])) continue;
                    problems.Add(fullName(row) + " was given two units, " + unitName(unit) + " and "
                        + unitName(named[0]) + " - say one");
                    bad = true;
                    break;
                }
                var bySymbol = new List<Tuple<object, object>>();
                foreach (var u in offered)
                    foreach (var one in validSymbols(u))
                        if (symbolName(one).Length > 0 && symbolName(one) == word) bySymbol.Add(Tuple.Create(u, one));
                if (bySymbol.Count == 0)
                    foreach (var u in offered)
                        foreach (var one in validSymbols(u))
                            if (symbolName(one).Length > 0 && string.Equals(symbolName(one), word, StringComparison.OrdinalIgnoreCase))
                                bySymbol.Add(Tuple.Create(u, one));
                if (unit != null) bySymbol = bySymbol.Where(p => unitName(p.Item1) == unitName(unit)).ToList();
                if (bySymbol.Count == 1 && symbol == null)
                {
                    if (unit == null) unit = bySymbol[0].Item1;
                    symbol = bySymbol[0].Item2;
                    continue;
                }
                problems.Add("'" + word + "' is not " + (bySymbol.Count > 1 ? "one" : "a") + " unit or symbol Revit offers for "
                    + fullName(row) + (bySymbol.Count > 1 ? " - more than one unit has that symbol; name the unit" : "")
                    + ". It offers " + offers(offered));
                bad = true;
                break;
            }
            if (bad) continue;
            if (symbolNone && symbol != null)
            {
                problems.Add(fullName(row) + " was given a symbol and 'no symbol' - say one");
                continue;
            }
            if (unit == null && rounding == null && !symbolNone)
            {
                problems.Add("nothing to set for " + fullName(row) + " - give units, a symbol or a rounding");
                continue;
            }

            // -- the new format, from a copy of the old so the tick boxes stay
            var currentUnit = unitOf(current);
            var sameUnit = unit == null || unitName(unit) == unitName(currentUnit);
            if (unit == null) unit = currentUnit;
            if (symbol == null)
            {
                if (symbolNone) symbol = noSymbol;
                else
                {
                    var oldSymbol = symbolName(symbolOf(current));
                    var symbols = validSymbols(unit);
                    var keep = symbols.FirstOrDefault(one => symbolName(one) == oldSymbol);
                    var first = symbols.FirstOrDefault(one => symbolName(one).Length > 0);
                    if (oldSymbol.Length == 0 && sameUnit) symbol = noSymbol;
                    else if (keep != null)
                    {
                        symbol = keep;
                        if (oldSymbol.Length == 0)
                            rowNotes.Add(fullName(row) + " keeps NO symbol, as before - a number with no symbol "
                                + "does not say its units; name one to add it");
                    }
                    else if (first != null)
                    {
                        symbol = first;
                        rowNotes.Add(fullName(row) + " takes the symbol " + symbolName(first) + ", the first "
                            + unitName(unit) + " offers - name another to change it");
                    }
                    else symbol = noSymbol;
                }
            }
            var next = new FormatOptions(current);
            try
            {
                putUnit(next, unit);
                putSymbol(next, symbol);
            }
            catch (Exception failure)
            {
                problems.Add(fullName(row) + " cannot be " + unitName(unit)
                    + (symbolName(symbol).Length > 0 ? " (" + symbolName(symbol) + ")" : " with no symbol")
                    + ": Revit says " + revitSaid(failure));
                continue;
            }
            var wantRounding = rounding ?? current.Accuracy;
            if (next.IsValidAccuracy(wantRounding)) next.Accuracy = wantRounding;
            else if (rounding.HasValue)
            {
                problems.Add("Revit does not take a rounding of " + roundingText(rounding.Value) + " for "
                    + fullName(row) + " in " + unitName(unit));
                continue;
            }
            else
            {
                next.Accuracy = revitDefault(unit).Accuracy;
                rowNotes.Add(fullName(row) + " takes Revit's own rounding for " + unitName(unit) + ", "
                    + roundingText(next.Accuracy) + " - the old one does not fit it");
            }
            var cleared = new List<string>();
            if (next.SuppressTrailingZeros && !next.CanSuppressTrailingZeros()) { next.SuppressTrailingZeros = false; cleared.Add("suppress trailing 0's"); }
            if (next.SuppressLeadingZeros && !next.CanSuppressLeadingZeros()) { next.SuppressLeadingZeros = false; cleared.Add("suppress leading 0's"); }
            if (next.SuppressSpaces && !next.CanSuppressSpaces()) { next.SuppressSpaces = false; cleared.Add("suppress spaces"); }
            if (next.UsePlusPrefix && !next.CanUsePlusPrefix()) { next.UsePlusPrefix = false; cleared.Add("show + for positive values"); }
            if (cleared.Count > 0)
                rowNotes.Add(fullName(row) + ": cleared " + string.Join(", ", cleared.ToArray()) + " - " + unitName(unit) + " cannot have "
                    + (cleared.Count == 1 ? "it" : "them"));

            var before = describe(current);
            var after = describe(next);
            if (before == after) already.Add(fullName(row) + " " + before);
            else { plans.Add(Tuple.Create(row, next, before, after)); notes.AddRange(rowNotes); }
        }
    }
    if (asked == 0 && problems.Count == 0)
        problems.Add("nothing was asked for - name a unit type and its units, as 'Cooling Load=Btu/h'");
    if (doc.IsFamilyDocument && problems.Count == 0)
        notes.Add("'" + doc.Title + "' is a family - these are the family's own units, not a project's");

    var nowGrouping = example(units.DecimalSymbol, units.DigitGroupingSymbol, units.DigitGroupingAmount);
    var setGrouping = false;
    if (wantDecimal.HasValue)
    {
        var wanted = example(wantDecimal.Value, wantGroup.Value, wantAmount.Value);
        if (wanted == nowGrouping) already.Add("decimal symbol/digit grouping " + nowGrouping);
        else setGrouping = true;
    }

    // ---- the change: built in memory, written once --------------------------
    var plan = plans.Select(p => fullName(p.Item1) + ": " + p.Item3 + " -> " + p.Item4).ToList();
    if (setGrouping) plan.Insert(0, "decimal symbol/digit grouping " + nowGrouping + " -> "
        + example(wantDecimal.Value, wantGroup.Value, wantAmount.Value));

    if (problems.Count > 0)
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was changed.";
    else if (plan.Count > 0)
    {
        try
        {
            foreach (var p in plans) putFormat(units, p.Item1.Item1, p.Item2);
            if (setGrouping)
            {
                units.DecimalSymbol = wantDecimal.Value;
                units.DigitGroupingSymbol = wantGroup.Value;
                units.DigitGroupingAmount = wantAmount.Value;
            }
        }
        catch (Exception failure)
        {
            refused = "Revit would not take the new units: " + revitSaid(failure) + ". Nothing was changed.";
        }
        if (refused.Length == 0)
        {
            try { doc.SetUnits(units); }
            catch (Exception failure)
            {
                refused = "Revit refused the change: " + revitSaid(failure) + ". It would have set "
                    + string.Join("; ", plan.ToArray()) + ". Nothing was changed.";
            }
        }

        if (refused.Length == 0)
        {
            // ---- read back from the model, and a wrong one undoes the lot ----
            var model = doc.GetUnits();
            var wrong = new List<string>();
            var done = new List<string>();
            var readBack = new List<string>();
            if (setGrouping)
            {
                var backGrouping = example(model.DecimalSymbol, model.DigitGroupingSymbol, model.DigitGroupingAmount);
                var wanted = example(wantDecimal.Value, wantGroup.Value, wantAmount.Value);
                if (backGrouping != wanted) wrong.Add("decimal symbol/digit grouping reads back " + backGrouping);
                else done.Add("decimal symbol/digit grouping " + nowGrouping + " -> " + backGrouping);
                readBack.Add("Decimal symbol/digit grouping = " + backGrouping);
            }
            foreach (var p in plans)
            {
                var back = describe(formatOf(model, p.Item1.Item1));
                if (back != p.Item4) wrong.Add(fullName(p.Item1) + " reads back " + back);
                else done.Add(fullName(p.Item1) + ": " + p.Item3 + " -> " + back);
                readBack.Add(fullName(p.Item1) + " = " + back);
            }
            if (wrong.Count > 0)
                throw new InvalidOperationException("Revit did not keep what was asked: " + string.Join("; ", wrong.ToArray())
                    + ". Nothing is kept - the whole change is rolled back.");
            changed = string.Join("; ", done.ToArray());
            notes.Add("READ BACK  ||  " + string.Join("  ||  ", readBack.ToArray()));
        }
    }

    if (already.Count > 0) alreadyThat = string.Join("; ", already.ToArray());
    notes.Add("read and written through " + route);
    findings = string.Join("; ", notes.ToArray()) + ".";
}
