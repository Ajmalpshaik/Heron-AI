// NOT STANDALONE. Assumes `doc`, `dimensionTypeName`, `changes` and
// `duplicateAs` are in scope; leaves `changedTypeId`, `typeAfter`,
// `typeBefore`, `usedBy`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// CHANGES A DIMENSION TYPE as its Type Properties does:
//
//   dimensionTypeName  the type, by name - "Linear - 3mm Arial" - or
//                      "Family: Type" where two styles share a name. Never
//                      guessed: a name matching none, or two, is refused with
//                      the types listed
//   changes            "Text Size=2.5; Tick Mark=Diagonal 3mm; Units
//                      Format=Millimeters, 0 decimal places" - each setting
//                      as Type Properties names it, a value after "=", ";"
//                      or a new line between. Sizes are PAPER millimetres
//   duplicateAs        a new name: the type is copied first and the COPY
//                      changed - what "make a new dimension type called X
//                      from Y" means. A name already used is refused, named,
//                      and nothing is made
//
// The settings: Text Size, Text Font, Width Factor, Bold, Italic, Underline,
// Text Background (opaque / transparent), Text Offset, Tick Mark (an
// arrowhead type by its exact name), Line Weight and Tick Mark Line Weight
// (1-16), Color (R,G,B), Dimension Line Extension, Witness Line Gap to
// Element, Witness Line Extension and Units Format - where the type has them.
// A setting is matched by the name typed here or by Revit's own name for the
// parameter on that type, so "Tick Mark" is what Type Properties says too.
//
// UNITS FORMAT takes "project" (Use project settings) or a unit, a rounding
// and a symbol in any order, comma between: "Millimeters, 0 decimal places",
// "Millimeters, rounding 5, no symbol". The units Revit offers for the type's
// own kind (length for a linear type, angle for an angular one) are listed
// when a name matches none. The format is copied from the type's own, so the
// tick boxes it does not name stay as they were. ForgeTypeId from 2021,
// DisplayUnitType on 2020, by reflection - REPORT_DIMENSION_TYPES' comment.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS WRITTEN: the type found, every
// setting known to it and writable, every value in range, the tick mark
// found, the units offered, the new name free. Then one regeneration and
// every value READ BACK; a value other than asked, or a refusal from Revit,
// throws - so the copy and the changes go back together. Nothing half-done
// is ever kept.
//
// ONLY THE TYPE NAMED, OR ITS COPY, IS TOUCHED. A type change reaches every
// dimension of that type; `usedBy` counts them.
//
// LENGTHS ARE PAPER MILLIMETRES, into Revit's internal feet by 304.8 (D-20).

const double MmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var findings = new List<string>();
var changedTypeId = ElementId.InvalidElementId;
var typeBefore = "";
var typeAfter = "";
var usedBy = "";
string refused = null;
var statics = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var api = typeof(Units).Assembly;
var space = typeof(Units).Namespace + ".";

// Name typed here, the parameters that hold it (the first the type carries),
// the kind of value, and the other words a modeller says for it.
var settings = new List<Tuple<string, BuiltInParameter[], string, string[]>>
{
    Tuple.Create("Line Weight", new[] { BuiltInParameter.LINE_PEN }, "integer", new[] { "lineweight", "pen" }),
    Tuple.Create("Tick Mark Line Weight", new[] { BuiltInParameter.TICK_MARK_PEN }, "integer", new[] { "tick line weight", "tick weight" }),
    Tuple.Create("Color", new[] { BuiltInParameter.LINE_COLOR }, "color", new[] { "colour" }),
    Tuple.Create("Dimension Line Extension", new[] { BuiltInParameter.DIM_LINE_EXTENSION }, "mm", new[] { "dimension line extension" }),
    Tuple.Create("Witness Line Gap to Element", new[] { BuiltInParameter.WITNS_LINE_GAP_TO_ELT }, "mm", new[] { "witness line gap", "gap to element" }),
    Tuple.Create("Witness Line Extension", new[] { BuiltInParameter.WITNS_LINE_EXTENSION }, "mm", new string[0]),
    Tuple.Create("Tick Mark", new[] { BuiltInParameter.DIM_LEADER_ARROWHEAD }, "element", new[] { "tick", "arrowhead", "arrow" }),
    Tuple.Create("Text Size", new[] { BuiltInParameter.TEXT_SIZE }, "mm", new[] { "size", "text height", "height" }),
    Tuple.Create("Text Offset", new[] { BuiltInParameter.TEXT_DIST_TO_LINE }, "mm", new[] { "offset" }),
    Tuple.Create("Text Font", new[] { BuiltInParameter.TEXT_FONT }, "text", new[] { "font" }),
    Tuple.Create("Text Background", new[] { BuiltInParameter.DIM_TEXT_BACKGROUND, BuiltInParameter.TEXT_BACKGROUND }, "background", new[] { "background" }),
    Tuple.Create("Width Factor", new[] { BuiltInParameter.TEXT_WIDTH_SCALE }, "factor", new[] { "width scale", "width" }),
    Tuple.Create("Bold", new[] { BuiltInParameter.TEXT_STYLE_BOLD }, "yesno", new string[0]),
    Tuple.Create("Italic", new[] { BuiltInParameter.TEXT_STYLE_ITALIC }, "yesno", new string[0]),
    Tuple.Create("Underline", new[] { BuiltInParameter.TEXT_STYLE_UNDERLINE }, "yesno", new string[0]),
    Tuple.Create("Units Format", new BuiltInParameter[0], "units", new[] { "units", "unit format", "unit" }),
};

Func<string, string> squash = s => new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
Func<string, string> loose = text =>
{
    var t = (text ?? "").ToLowerInvariant().Replace("metre", "meter");
    var parts = t.Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries)
                 .Select(w => w.Length > 3 && w.EndsWith("s") ? w.Substring(0, w.Length - 1) : w);
    return string.Join(" ", parts.ToArray());
};

Func<Element, BuiltInParameter[], Parameter> parameterOf = (e, which) =>
{
    foreach (var bip in which)
    {
        Parameter p = null;
        try { p = e.get_Parameter(bip); } catch { p = null; }
        if (p != null) return p;
    }
    return null;
};

Func<Parameter, string, string> labelOf = (p, typed) =>
{
    try
    {
        var name = p.Definition == null ? "" : p.Definition.Name;
        return string.IsNullOrWhiteSpace(name) ? typed : name;
    }
    catch { return typed; }
};

Func<ElementType, string> fullName = t =>
    string.IsNullOrWhiteSpace(t.FamilyName) ? t.Name : t.FamilyName + ": " + t.Name;

Func<string, StorageType> storageFor = kind =>
    kind == "mm" || kind == "factor" ? StorageType.Double
    : kind == "text" ? StorageType.String
    : kind == "element" ? StorageType.ElementId
    : StorageType.Integer;

Func<Parameter, string, string> shown = (p, kind) =>
{
    try
    {
        if (kind == "mm" && p.StorageType == StorageType.Double)
            return Math.Round(p.AsDouble() * MmPerFoot, 3).ToString(invariant) + " mm";
        if (kind == "factor" && p.StorageType == StorageType.Double)
            return Math.Round(p.AsDouble(), 4).ToString(invariant);
        if (kind == "yesno" && p.StorageType == StorageType.Integer)
            return p.AsInteger() != 0 ? "yes" : "no";
        if (kind == "integer" && p.StorageType == StorageType.Integer)
            return p.AsInteger().ToString(invariant);
        if (kind == "background" && p.StorageType == StorageType.Integer)
        {
            var revitSays = p.AsValueString();
            return !string.IsNullOrWhiteSpace(revitSays) ? revitSays
                : (p.AsInteger() == 1 ? "Transparent" : "Opaque");
        }
        if (kind == "color" && p.StorageType == StorageType.Integer)
        {
            var c = p.AsInteger();
            return string.Format(invariant, "{0},{1},{2}", c & 0xFF, (c >> 8) & 0xFF, (c >> 16) & 0xFF);
        }
        if (kind == "text" && p.StorageType == StorageType.String)
            return p.AsString() ?? "";
        if (kind == "element" && p.StorageType == StorageType.ElementId)
        {
            var target = doc.GetElement(p.AsElementId());
            return target == null ? "None" : target.Name;
        }
        return p.AsValueString() ?? "";
    }
    catch { return ""; }
};

Func<double, string> roundingText = accuracy =>
{
    for (var places = 0; places <= 12; places++)
    {
        var step = Math.Pow(10.0, -places);
        if (Math.Abs(accuracy - step) <= step * 1e-9)
            return places + (places == 1 ? " decimal place" : " decimal places");
    }
    return "to the nearest " + accuracy.ToString("0.############", invariant);
};

// A rounding in the dialog's words, as the increment: null when the text is
// not a rounding at all, NaN when it is one that cannot be read.
Func<string, double?> roundingOf = text =>
{
    var t = string.Join(" ", (text ?? "").Trim().ToLowerInvariant().Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
    if (t == "no decimals" || t == "no decimal places" || t == "no decimal" || t == "whole numbers") return 1.0;
    foreach (var tail in new[] { " decimal places", " decimal place", " decimals", " decimal" })
    {
        if (!t.EndsWith(tail)) continue;
        int places;
        var count = t.Substring(0, t.Length - tail.Length).Trim();
        if (!int.TryParse(count, System.Globalization.NumberStyles.None, invariant, out places) || places > 12) return double.NaN;
        return Math.Pow(10.0, -places);
    }
    string number;
    if (t.StartsWith("rounding ")) number = t.Substring(9).Trim();
    else if (t.StartsWith("to the nearest ")) number = t.Substring(15).Trim();
    else return null;
    double value;
    if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariant, out value)) return double.NaN;
    return value;
};

// ---- Units Format, the two routes as the same questions --------------------

Func<DimensionType, object> specOf = null;
Func<FormatOptions, object> unitOf = null;
Func<FormatOptions, object> symbolOf = null;
Func<object, string> unitName = null;
Func<object, string> symbolName = null;            // "" for no symbol
Func<object, List<object>> validUnits = null;      // of the type's kind
Func<object, List<object>> validSymbols = null;    // of a unit
Action<FormatOptions, object> putUnit = null;
Action<FormatOptions, object> putSymbol = null;
object noSymbol = null;

var forgeType = api.GetType(space + "ForgeTypeId");
var specOfType = typeof(DimensionType).GetMethod("GetSpecTypeId", Type.EmptyTypes);
if (forgeType != null && specOfType != null)
{
    var one = new[] { forgeType };
    var getUnit = typeof(FormatOptions).GetMethod("GetUnitTypeId", Type.EmptyTypes);
    var getSymbol = typeof(FormatOptions).GetMethod("GetSymbolTypeId", Type.EmptyTypes);
    var setUnit = typeof(FormatOptions).GetMethod("SetUnitTypeId", one);
    var setSymbol = typeof(FormatOptions).GetMethod("SetSymbolTypeId", one);
    var labelUnit = typeof(LabelUtils).GetMethod("GetLabelForUnit", statics, null, one, null);
    var labelSymbol = typeof(LabelUtils).GetMethod("GetLabelForSymbol", statics, null, one, null);
    var unitsOfSpec = typeof(UnitUtils).GetMethod("GetValidUnits", statics, null, one, null);
    var symbolsOfUnit = typeof(FormatOptions).GetMethod("GetValidSymbols", statics, null, one, null);
    var isEmpty = forgeType.GetMethod("Empty", Type.EmptyTypes);
    Func<object, bool> empty = id => id == null || (bool)isEmpty.Invoke(id, null);
    noSymbol = Activator.CreateInstance(forgeType);
    specOf = t => specOfType.Invoke(t, null);
    unitOf = format => getUnit.Invoke(format, null);
    symbolOf = format => getSymbol.Invoke(format, null);
    unitName = unit => empty(unit) ? "" : (labelUnit.Invoke(null, new[] { unit }) as string ?? "");
    symbolName = symbol => empty(symbol) ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
    validUnits = spec => ((System.Collections.IEnumerable)unitsOfSpec.Invoke(null, new[] { spec })).Cast<object>().ToList();
    validSymbols = unit => ((System.Collections.IEnumerable)symbolsOfUnit.Invoke(null, new[] { unit })).Cast<object>().ToList();
    putUnit = (format, unit) => setUnit.Invoke(format, new[] { unit });
    putSymbol = (format, symbol) => setSymbol.Invoke(format, new[] { symbol });
}
else
{
    var unitType = api.GetType(space + "UnitType");
    var displayType = api.GetType(space + "DisplayUnitType");
    var symbolType = api.GetType(space + "UnitSymbolType");
    var unitTypeOf = typeof(DimensionType).GetProperty("UnitType");
    var displayUnits = typeof(FormatOptions).GetProperty("DisplayUnits");
    var unitSymbol = typeof(FormatOptions).GetProperty("UnitSymbol");
    var labelDisplay = typeof(LabelUtils).GetMethod("GetLabelFor", statics, null, new[] { displayType }, null);
    var labelSymbol = typeof(LabelUtils).GetMethod("GetLabelFor", statics, null, new[] { symbolType }, null);
    var unitsOfType = typeof(UnitUtils).GetMethod("GetValidDisplayUnits", statics, null, new[] { unitType }, null);
    var symbolsOfUnit = typeof(FormatOptions).GetMethod("GetValidUnitSymbols", statics, null, new[] { displayType }, null);
    noSymbol = Enum.Parse(symbolType, "UST_NONE");
    specOf = t => unitTypeOf.GetValue(t);
    unitOf = format => displayUnits.GetValue(format);
    symbolOf = format => unitSymbol.GetValue(format);
    unitName = unit => labelDisplay.Invoke(null, new[] { unit }) as string ?? "";
    symbolName = symbol => symbol == null || symbol.ToString() == "UST_NONE"
        ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
    validUnits = spec => ((System.Collections.IEnumerable)unitsOfType.Invoke(null, new[] { spec })).Cast<object>().ToList();
    validSymbols = unit => ((System.Collections.IEnumerable)symbolsOfUnit.Invoke(null, new[] { unit })).Cast<object>().ToList();
    putUnit = (format, unit) => displayUnits.SetValue(format, unit);
    putSymbol = (format, symbol) => unitSymbol.SetValue(format, symbol);
}

Func<FormatOptions, string> formatText = format =>
{
    if (format == null) return "";
    if (format.UseDefault) return "Use project settings";
    var symbol = symbolName(symbolOf(format));
    return unitName(unitOf(format)) + ", " + roundingText(format.Accuracy)
        + (symbol.Length > 0 ? ", symbol " + symbol : ", no symbol");
};

Func<DimensionType, string> describe = t =>
{
    var parts = new List<string> { fullName(t), "style " + t.StyleType };
    foreach (var s in settings)
    {
        if (s.Item3 == "units")
        {
            try { parts.Add("Units Format " + formatText(t.GetUnitsFormatOptions())); } catch { }
            continue;
        }
        var p = parameterOf(t, s.Item2);
        if (p != null) parts.Add(labelOf(p, s.Item1) + " " + shown(p, s.Item3));
    }
    return string.Join(" | ", parts);
};

// ---- What there is ---------------------------------------------------------

var dimTypes = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>()
    .Where(t => !string.IsNullOrWhiteSpace(t.Name))
    .OrderBy(t => t.StyleType.ToString()).ThenBy(t => t.FamilyName ?? "").ThenBy(t => t.Name ?? "").ToList();
var dimensions = new FilteredElementCollector(doc).OfClass(typeof(Dimension)).ToElements();
var typeList = dimTypes.Count == 0 ? "none" : string.Join(", ", dimTypes.Select(fullName));

// ---- The type: named, never guessed ----------------------------------------

DimensionType start = null;
var asked = (dimensionTypeName ?? "").Trim();
if (asked.Length == 0)
    refused = "No dimension type was named, and Heron never picks one. The dimension types here are: " + typeList + ".";
else
{
    var matches = dimTypes.Where(t =>
        string.Equals(t.Name, asked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fullName(t), asked, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 1) start = matches[0];
    else if (matches.Count > 1)
        refused = "\"" + asked + "\" names " + matches.Count + " dimension types: "
            + string.Join(", ", matches.Select(t => "\"" + fullName(t) + "\""))
            + ". Name one as \"Family: Type\".";
    else
    {
        var why = "";
        foreach (var other in new FilteredElementCollector(doc).WhereElementIsElementType().ToElements())
        {
            var type = other as ElementType;
            if (type == null || type is DimensionType) continue;
            if (!string.Equals(type.Name, asked, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullName(type), asked, StringComparison.OrdinalIgnoreCase)) continue;
            why = " \"" + fullName(type) + "\" is a " + (string.IsNullOrWhiteSpace(type.FamilyName) ? "type" : type.FamilyName)
                + ", not a dimension type" + (type is TextElementType ? " - SET_TEXT_TYPE changes text types" : "") + ".";
            break;
        }
        refused = "No dimension type is named \"" + asked + "\"." + why + " The dimension types here are: " + typeList + ".";
    }
}

// ---- What to change --------------------------------------------------------

// setting, value to write, the words given
var wanted = new List<Tuple<Tuple<string, BuiltInParameter[], string, string[]>, object, string>>();
var said = (changes ?? "").Trim();
if (refused == null && said.Length > 0 && !string.Equals(said, "none", StringComparison.OrdinalIgnoreCase))
{
    foreach (var raw in said.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var piece = raw.Trim();
        if (piece.Length == 0) continue;
        var eq = piece.IndexOf('=');
        if (eq <= 0) { refused = "\"" + piece + "\" is not a setting=value pair - e.g. Text Size=2.5; Tick Mark=Diagonal 3mm."; break; }
        var name = piece.Substring(0, eq).Trim();
        var value = piece.Substring(eq + 1).Trim();
        var setting = settings.FirstOrDefault(s => squash(s.Item1) == squash(name)
            || s.Item4.Any(a => squash(a) == squash(name)));
        if (setting == null)
            setting = settings.FirstOrDefault(s =>
            {
                var p = parameterOf(start, s.Item2);
                return p != null && squash(labelOf(p, s.Item1)) == squash(name);
            });
        if (setting == null)
        {
            refused = "\"" + name + "\" is not a dimension type setting Heron changes. They are: "
                + string.Join(", ", settings.Select(s => s.Item1)) + ".";
            break;
        }
        if (wanted.Any(w => w.Item1 == setting)) { refused = setting.Item1 + " is asked twice."; break; }

        object parsed = null;
        string problem = null;
        var kind = setting.Item3;
        var lower = value.ToLowerInvariant();
        if (kind == "mm" || kind == "factor")
        {
            var number = lower.EndsWith("mm") && kind == "mm" ? lower.Substring(0, lower.Length - 2).Trim() : lower;
            double d;
            if (value.Contains("\"") || value.Contains("'") || lower.EndsWith("in") || lower.EndsWith("ft"))
                problem = "give it in millimetres - 2.5 for 2.5 mm";
            else if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariant, out d))
                problem = "is not a number";
            else if (setting.Item1 == "Text Size" || kind == "factor" ? d <= 0 : d < 0)
                problem = setting.Item1 == "Text Size" || kind == "factor" ? "must be above zero" : "cannot be below zero";
            else parsed = kind == "mm" ? d / MmPerFoot : d;
        }
        else if (kind == "yesno")
        {
            if (new[] { "yes", "true", "on", "1" }.Contains(lower)) parsed = 1;
            else if (new[] { "no", "false", "off", "0" }.Contains(lower)) parsed = 0;
            else problem = "is not yes or no";
        }
        else if (kind == "integer")
        {
            int n;
            if (!int.TryParse(lower, System.Globalization.NumberStyles.Integer, invariant, out n) || n < 1 || n > 16)
                problem = "is not a line weight from 1 to 16";
            else parsed = n;
        }
        else if (kind == "background")
        {
            if (lower == "opaque") parsed = 0;
            else if (lower == "transparent") parsed = 1;
            else problem = "is not opaque or transparent";
        }
        else if (kind == "color")
        {
            int[] rgb = null;
            if (lower == "black") rgb = new[] { 0, 0, 0 };
            else if (lower == "white") rgb = new[] { 255, 255, 255 };
            else if (lower.StartsWith("#") && lower.Length == 7)
            {
                try { rgb = new[] { Convert.ToInt32(lower.Substring(1, 2), 16), Convert.ToInt32(lower.Substring(3, 2), 16), Convert.ToInt32(lower.Substring(5, 2), 16) }; }
                catch { rgb = null; }
            }
            else
            {
                var bits = lower.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                int r, g, b;
                if (bits.Length == 3 && int.TryParse(bits[0], out r) && int.TryParse(bits[1], out g) && int.TryParse(bits[2], out b))
                    rgb = new[] { r, g, b };
            }
            if (rgb == null || rgb.Any(c => c < 0 || c > 255)) problem = "is not a colour - give R,G,B from 0 to 255, #RRGGBB, black or white";
            else parsed = rgb[0] + (rgb[1] << 8) + (rgb[2] << 16);
        }
        else if (kind == "text")
        {
            if (value.Length == 0) problem = "is empty";
            else parsed = value;
        }
        else if (kind == "element")
        {
            // A tick mark by its exact name, never the nearest one.
            var arrowheads = new FilteredElementCollector(doc).WhereElementIsElementType().ToElements()
                .Where(e => parameterOf(e, new[] { BuiltInParameter.ARROW_TYPE }) != null)
                .OrderBy(e => e.Name).ToList();
            var arrow = arrowheads.Where(e => string.Equals(e.Name, value, StringComparison.OrdinalIgnoreCase)).ToList();
            if (arrow.Count != 1)
                problem = (arrow.Count == 0 ? "is not" : "names " + arrow.Count + " of") + " the arrowhead types here - they are: "
                    + (arrowheads.Count == 0 ? "none" : string.Join(", ", arrowheads.Select(e => e.Name)));
            else parsed = arrow[0].Id;
        }
        else if (kind == "units")
        {
            // Read here, against the type's own kind of units; written below.
            try
            {
                var current = start.GetUnitsFormatOptions();
                if (lower == "project" || lower == "use project settings" || lower == "project settings" || lower == "default")
                    parsed = Tuple.Create(true, (object)null, (object)null, (double?)null);
                else
                {
                    var offered = validUnits(specOf(start));
                    object unit = null;
                    object symbol = null;
                    double? rounding = null;
                    foreach (var part in value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                    {
                        var r = roundingOf(part);
                        if (r.HasValue)
                        {
                            if (double.IsNaN(r.Value) || r.Value <= 0) { problem = "has a rounding Revit does not take - \"0 decimal places\", \"rounding 5\""; break; }
                            rounding = r;
                            continue;
                        }
                        var s = squash(part);
                        if (s == "nosymbol" || s == "none" || s == "withoutsymbol") { symbol = noSymbol; continue; }
                        var named = offered.Where(u => loose(unitName(u)) == loose(part)).ToList();
                        if (named.Count == 1 && unit == null) { unit = named[0]; continue; }
                        var bySymbol = new List<Tuple<object, object>>();
                        foreach (var u in (unit == null ? offered : new List<object> { unit }))
                            foreach (var candidate in validSymbols(u))
                                if (symbolName(candidate).Length > 0 && symbolName(candidate) == part) bySymbol.Add(Tuple.Create(u, candidate));
                        if (bySymbol.Count == 1 && symbol == null)
                        {
                            if (unit == null) unit = bySymbol[0].Item1;
                            symbol = bySymbol[0].Item2;
                            continue;
                        }
                        problem = "- \"" + part + "\" is not a unit or symbol Revit offers for this type. It offers: "
                            + string.Join(", ", offered.Select(u => unitName(u)).Where(n => n.Length > 0));
                        break;
                    }
                    if (problem == null && unit == null && rounding == null && symbol == null)
                        problem = "names no unit, rounding or symbol - e.g. Millimeters, 0 decimal places; or project";
                    // A type on project settings has no unit of its own to keep.
                    if (problem == null && unit == null && current.UseDefault)
                        problem = "names no unit, and this type uses the project's units - name the unit too, "
                            + "e.g. Millimeters, 0 decimal places";
                    if (problem == null)
                    {
                        // Check the rounding against the unit it will have.
                        var check = new FormatOptions(current);
                        check.UseDefault = false;
                        if (unit != null) putUnit(check, unit);
                        var accuracy = rounding ?? check.Accuracy;
                        if (!check.IsValidAccuracy(accuracy))
                            problem = "has a rounding " + accuracy.ToString("0.############", invariant) + " that "
                                + unitName(unitOf(check)) + " does not take";
                        else parsed = Tuple.Create(false, unit, symbol, rounding);
                    }
                }
            }
            catch (Exception ex) { problem = "could not be read against this type: " + (ex.InnerException ?? ex).Message; }
        }
        if (problem != null) { refused = setting.Item1 + " \"" + value + "\" " + problem + "."; break; }
        wanted.Add(Tuple.Create(setting, parsed, value));
    }
}

// ---- The copy --------------------------------------------------------------

var newName = (duplicateAs ?? "").Trim();
if (string.Equals(newName, "none", StringComparison.OrdinalIgnoreCase)) newName = "";
if (refused == null && newName != "")
{
    var taken = dimTypes.FirstOrDefault(t => string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(t.FamilyName ?? "", start.FamilyName ?? "", StringComparison.OrdinalIgnoreCase));
    if (taken != null)
        refused = "\"" + fullName(taken) + "\" is already a dimension type, so no copy was made - give another name, "
            + "or change that type by its name.";
}
if (refused == null && wanted.Count == 0 && newName == "")
    refused = "Nothing was asked - give a setting to change (Text Size=2.5; Tick Mark=Diagonal 3mm) or a duplicateAs name.";

// Every setting asked must be one this type has, writable, held as expected.
if (refused == null)
{
    var missing = new List<string>();
    foreach (var w in wanted)
    {
        if (w.Item1.Item3 == "units") continue;
        var p = parameterOf(start, w.Item1.Item2);
        if (p == null) missing.Add(w.Item1.Item1 + " - this type has none");
        else if (p.IsReadOnly) missing.Add(w.Item1.Item1 + " - read-only on this type");
        else if (p.StorageType != storageFor(w.Item1.Item3)) missing.Add(w.Item1.Item1 + " - held another way here");
    }
    if (missing.Count > 0)
        refused = "\"" + fullName(start) + "\" cannot take: " + string.Join("; ", missing) + ". Its settings are: "
            + string.Join(", ", settings.Where(s => s.Item3 == "units" || parameterOf(start, s.Item2) != null)
                .Select(s => s.Item3 == "units" ? s.Item1 : labelOf(parameterOf(start, s.Item2), s.Item1))) + ".";
}

// ---- The change ------------------------------------------------------------

if (refused == null)
{
    typeBefore = describe(start);

    DimensionType target = start;
    if (newName != "")
    {
        try { target = start.Duplicate(newName) as DimensionType; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nothing was kept. Revit would not copy \"" + fullName(start) + "\" as \""
                + newName + "\": " + ex.Message);
        }
        if (target == null)
            throw new InvalidOperationException("Nothing was kept. Copying \"" + fullName(start) + "\" gave no dimension type.");
    }

    var notTaken = new List<string>();
    FormatOptions wantFormat = null;
    foreach (var w in wanted)
    {
        if (w.Item1.Item3 == "units")
        {
            var plan = (Tuple<bool, object, object, double?>)w.Item2;
            try
            {
                var format = new FormatOptions(target.GetUnitsFormatOptions());
                if (plan.Item1) format.UseDefault = true;
                else
                {
                    var oldSymbol = symbolName(symbolOf(format));
                    format.UseDefault = false;
                    if (plan.Item2 != null) putUnit(format, plan.Item2);
                    if (plan.Item3 != null) putSymbol(format, plan.Item3);
                    else if (plan.Item2 != null)
                    {
                        // A new unit keeps the old symbol where it offers it, else none.
                        var keep = validSymbols(plan.Item2).FirstOrDefault(one => symbolName(one) == oldSymbol && oldSymbol.Length > 0);
                        putSymbol(format, keep ?? noSymbol);
                    }
                    if (plan.Item4.HasValue) format.Accuracy = plan.Item4.Value;
                }
                target.SetUnitsFormatOptions(format);
                wantFormat = format;
            }
            catch (Exception ex) { notTaken.Add("Units Format \"" + w.Item3 + "\": " + (ex.InnerException ?? ex).Message); }
            continue;
        }
        var p = parameterOf(target, w.Item1.Item2);
        bool ok;
        try
        {
            if (w.Item2 is double) ok = p.Set((double)w.Item2);
            else if (w.Item2 is int) ok = p.Set((int)w.Item2);
            else if (w.Item2 is ElementId) ok = p.Set((ElementId)w.Item2);
            else ok = p.Set((string)w.Item2);
        }
        catch (Exception ex) { ok = false; notTaken.Add(w.Item1.Item1 + " \"" + w.Item3 + "\": " + ex.Message); continue; }
        if (!ok) notTaken.Add(w.Item1.Item1 + " \"" + w.Item3 + "\": Revit did not take it");
    }
    if (notTaken.Count > 0)
        throw new InvalidOperationException("Nothing was kept. Revit refused " + string.Join("; ", notTaken) + ".");

    doc.Regenerate();

    // READ BACK - the type as Revit now holds it, against what was asked.
    var wrong = new List<string>();
    foreach (var w in wanted)
    {
        var kind = w.Item1.Item3;
        if (kind == "units")
        {
            var now = target.GetUnitsFormatOptions();
            var same = now != null && now.UseDefault == wantFormat.UseDefault
                && (wantFormat.UseDefault || (unitName(unitOf(now)) == unitName(unitOf(wantFormat))
                    && symbolName(symbolOf(now)) == symbolName(symbolOf(wantFormat))
                    && Math.Abs(now.Accuracy - wantFormat.Accuracy) <= wantFormat.Accuracy * 1e-9));
            if (!same) wrong.Add("Units Format reads " + formatText(now) + ", asked " + w.Item3);
            continue;
        }
        var p = parameterOf(target, w.Item1.Item2);
        bool ok;
        if (w.Item2 is double) ok = Math.Abs(p.AsDouble() - (double)w.Item2) < (kind == "mm" ? 0.001 / MmPerFoot : 1e-6);
        else if (w.Item2 is int) ok = p.AsInteger() == (int)w.Item2;
        else if (w.Item2 is ElementId) ok = p.AsElementId() == (ElementId)w.Item2;
        else ok = string.Equals(p.AsString() ?? "", (string)w.Item2, StringComparison.Ordinal);
        // Background is also asked of Revit's own word for it, so a wrong
        // reading of which number is which cannot pass as kept.
        if (ok && kind == "background")
        {
            var word = (p.AsValueString() ?? "").ToLowerInvariant();
            var other = (int)w.Item2 == 1 ? "opaque" : "transparent";
            if (word.Contains(other)) ok = false;
        }
        if (!ok) wrong.Add(w.Item1.Item1 + " reads " + shown(p, kind) + ", asked " + w.Item3);
    }
    if (wrong.Count > 0)
        throw new InvalidOperationException("Nothing was kept. NOT AS ASKED after the change: " + string.Join("; ", wrong) + ".");

    changedTypeId = target.Id;
    typeAfter = describe(target);
    var on = dimensions.Count(e => e.GetTypeId() == target.Id);
    usedBy = on + " dimension(s) are on \"" + fullName(target) + "\"";

    findings.Add((target.Id != start.Id ? "Copied \"" + fullName(start) + "\" as \"" + fullName(target) + "\"" : "Changed \"" + fullName(target) + "\"")
        + (wanted.Count > 0 ? ": " + string.Join(", ", wanted.Select(w => w.Item1.Item1 + " " + w.Item3)) : " with nothing changed on it")
        + " - read back as asked.");
    findings.Add(usedBy + (target.Id == start.Id ? " and changed with it." : " - the copy is new, so none yet; "
        + "put dimensions on it with their type selector."));
    if (wanted.Any(w => w.Item1.Item1 == "Text Font"))
        findings.Add("The font must be installed on every PC that opens this file - Heron does not check that.");
}

if (refused != null)
{
    refused = refused.TrimEnd() + " NOTHING WAS CHANGED.";
    findings.Add(refused);
}
