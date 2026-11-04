// NOT STANDALONE. Assumes `doc`, `dimensionTypeName` and `style` are in
// scope; leaves `dimensionTypes`, `typeCount`, `dimensionCount` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY DIMENSION TYPE - Annotate > Dimension > Types - with what its Type
// Properties shows: its style (Linear, Angular, Radial, Diameter, Arc Length,
// Spot Elevation, Spot Coordinate, Spot Slope ...), Text Size, Text Font,
// Width Factor, Bold, Italic, Underline, Text Background, Text Offset, Tick
// Mark, Line Weight, Tick Mark Line Weight, Color, Dimension Line Extension,
// Witness Line Control, Witness Line Gap to Element, Witness Line Extension
// and Units Format, and how many dimensions use it. A setting the type has
// not got is left out of its line, never printed as a guess.
//
// EACH SETTING IS PRINTED UNDER REVIT'S OWN NAME FOR IT - the parameter's
// Definition.Name - not under a name typed here. Which built-in parameter
// Type Properties calls "Tick Mark" on a dimension type is not something the
// assemblies say (NEEDS-CHECKING Group LR, LR3), so the read says what Revit
// calls what it read, and a wrong guess shows as a wrong label, never as a
// tick mark that is really something else.
//
// UNITS FORMAT BY REFLECTION, ONE IMPLEMENTATION FOR EVERY RELEASE. The
// format is DimensionType.GetUnitsFormatOptions() on all eight releases; its
// unit and symbol are ForgeTypeIds from 2021 (GetUnitTypeId, GetSymbolTypeId,
// LabelUtils.GetLabelForUnit / ForSymbol) and enums on 2020 (DisplayUnits,
// UnitSymbol, LabelUtils.GetLabelFor) - read off the shipped RevitAPI.dll for
// 2020 to 2027 with tools/api-surface on 2026-11-02. The newer route is used
// wherever DimensionType.GetSpecTypeId exists (2021 on), as SET_PROJECT_UNITS
// does, so there is no #if.
//
// LENGTHS ARE PAPER MILLIMETRES, from Revit's internal feet by 304.8 (D-20).

const double MmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var findings = new List<string>();
var statics = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var api = typeof(Units).Assembly;
var space = typeof(Units).Namespace + ".";

// Name typed here, the parameters that hold it (the first the type carries),
// and how its value is shown.
var settings = new List<Tuple<string, BuiltInParameter[], string>>
{
    Tuple.Create("Line Weight", new[] { BuiltInParameter.LINE_PEN }, "integer"),
    Tuple.Create("Tick Mark Line Weight", new[] { BuiltInParameter.TICK_MARK_PEN }, "integer"),
    Tuple.Create("Color", new[] { BuiltInParameter.LINE_COLOR }, "color"),
    Tuple.Create("Dimension Line Extension", new[] { BuiltInParameter.DIM_LINE_EXTENSION }, "mm"),
    Tuple.Create("Witness Line Control", new[] { BuiltInParameter.DIM_WITNS_LINE_CNTRL }, "revit"),
    Tuple.Create("Witness Line Gap to Element", new[] { BuiltInParameter.WITNS_LINE_GAP_TO_ELT }, "mm"),
    Tuple.Create("Witness Line Extension", new[] { BuiltInParameter.WITNS_LINE_EXTENSION }, "mm"),
    Tuple.Create("Tick Mark", new[] { BuiltInParameter.DIM_LEADER_ARROWHEAD }, "element"),
    Tuple.Create("Text Size", new[] { BuiltInParameter.TEXT_SIZE }, "mm"),
    Tuple.Create("Text Offset", new[] { BuiltInParameter.TEXT_DIST_TO_LINE }, "mm"),
    Tuple.Create("Text Font", new[] { BuiltInParameter.TEXT_FONT }, "text"),
    Tuple.Create("Text Background", new[] { BuiltInParameter.DIM_TEXT_BACKGROUND, BuiltInParameter.TEXT_BACKGROUND }, "background"),
    Tuple.Create("Width Factor", new[] { BuiltInParameter.TEXT_WIDTH_SCALE }, "factor"),
    Tuple.Create("Bold", new[] { BuiltInParameter.TEXT_STYLE_BOLD }, "yesno"),
    Tuple.Create("Italic", new[] { BuiltInParameter.TEXT_STYLE_ITALIC }, "yesno"),
    Tuple.Create("Underline", new[] { BuiltInParameter.TEXT_STYLE_UNDERLINE }, "yesno"),
};

Func<string, string> squash = s => new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

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

Func<ElementType, string> fullName = t =>
    string.IsNullOrWhiteSpace(t.FamilyName) ? t.Name : t.FamilyName + ": " + t.Name;

Func<Parameter, string, string> labelOf = (p, typed) =>
{
    try
    {
        var name = p.Definition == null ? "" : p.Definition.Name;
        return string.IsNullOrWhiteSpace(name) ? typed : name;
    }
    catch { return typed; }
};

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

// ---- Units Format, the two routes as the same questions --------------------

Func<DimensionType, object> specOf = null;
Func<object, FormatOptions> projectFormat = null;
Func<FormatOptions, object> unitOf = null;
Func<FormatOptions, object> symbolOf = null;
Func<object, string> unitName = null;
Func<object, string> symbolName = null;            // "" for no symbol

var forgeType = api.GetType(space + "ForgeTypeId");
var specOfType = typeof(DimensionType).GetMethod("GetSpecTypeId", Type.EmptyTypes);
if (forgeType != null && specOfType != null)
{
    var one = new[] { forgeType };
    var getFormat = typeof(Units).GetMethod("GetFormatOptions", one);
    var getUnit = typeof(FormatOptions).GetMethod("GetUnitTypeId", Type.EmptyTypes);
    var getSymbol = typeof(FormatOptions).GetMethod("GetSymbolTypeId", Type.EmptyTypes);
    var labelUnit = typeof(LabelUtils).GetMethod("GetLabelForUnit", statics, null, one, null);
    var labelSymbol = typeof(LabelUtils).GetMethod("GetLabelForSymbol", statics, null, one, null);
    var isEmpty = forgeType.GetMethod("Empty", Type.EmptyTypes);
    Func<object, bool> empty = id => id == null || (bool)isEmpty.Invoke(id, null);
    specOf = t => specOfType.Invoke(t, null);
    projectFormat = spec => getFormat.Invoke(doc.GetUnits(), new[] { spec }) as FormatOptions;
    unitOf = format => getUnit.Invoke(format, null);
    symbolOf = format => getSymbol.Invoke(format, null);
    unitName = unit => empty(unit) ? "" : (labelUnit.Invoke(null, new[] { unit }) as string ?? "");
    symbolName = symbol => empty(symbol) ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
}
else
{
    var unitType = api.GetType(space + "UnitType");
    var displayType = api.GetType(space + "DisplayUnitType");
    var symbolType = api.GetType(space + "UnitSymbolType");
    var unitTypeOf = typeof(DimensionType).GetProperty("UnitType");
    var getFormat = typeof(Units).GetMethod("GetFormatOptions", new[] { unitType });
    var displayUnits = typeof(FormatOptions).GetProperty("DisplayUnits");
    var unitSymbol = typeof(FormatOptions).GetProperty("UnitSymbol");
    var labelDisplay = typeof(LabelUtils).GetMethod("GetLabelFor", statics, null, new[] { displayType }, null);
    var labelSymbol = typeof(LabelUtils).GetMethod("GetLabelFor", statics, null, new[] { symbolType }, null);
    specOf = t => unitTypeOf.GetValue(t);
    projectFormat = spec => getFormat.Invoke(doc.GetUnits(), new[] { spec }) as FormatOptions;
    unitOf = format => displayUnits.GetValue(format);
    symbolOf = format => unitSymbol.GetValue(format);
    unitName = unit => labelDisplay.Invoke(null, new[] { unit }) as string ?? "";
    symbolName = symbol => symbol == null || symbol.ToString() == "UST_NONE"
        ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
}

Func<FormatOptions, string> formatText = format =>
{
    var symbol = symbolName(symbolOf(format));
    return unitName(unitOf(format)) + ", " + roundingText(format.Accuracy)
        + (symbol.Length > 0 ? ", symbol " + symbol : ", no symbol");
};

Func<DimensionType, string> unitsText = t =>
{
    try
    {
        var format = t.GetUnitsFormatOptions();
        if (format == null) return "";
        if (!format.UseDefault) return formatText(format);
        try { return "Use project settings (" + formatText(projectFormat(specOf(t))) + ")"; }
        catch { return "Use project settings"; }
    }
    catch { return ""; }
};

// ---- What there is ---------------------------------------------------------

// A dimension type with no name is one Revit keeps for itself and never shows
// under Annotate > Dimension > Types; it is counted, never listed.
var allTypes = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().ToList();
var dimTypes = allTypes.Where(t => !string.IsNullOrWhiteSpace(t.Name))
    .OrderBy(t => t.StyleType.ToString()).ThenBy(t => t.FamilyName ?? "").ThenBy(t => t.Name ?? "").ToList();
var hidden = allTypes.Count - dimTypes.Count;
var dimensions = new FilteredElementCollector(doc).OfClass(typeof(Dimension)).ToElements();
var dimensionCount = dimensions.Count;
var typeList = dimTypes.Count == 0 ? "none" : string.Join(", ", dimTypes.Select(fullName));

// ---- Which ------------------------------------------------------------------

var chosen = dimTypes;
var askedStyle = (style ?? "").Trim();
if (askedStyle.Length > 0 && !string.Equals(askedStyle, "all", StringComparison.OrdinalIgnoreCase))
{
    var styles = dimTypes.Select(t => t.StyleType).Distinct().ToList();
    var wantStyle = styles.Where(s => squash(s.ToString()) == squash(askedStyle)
        || squash(s.ToString()) == squash(askedStyle.Replace("dimension", "").Replace("Dimension", ""))).ToList();
    if (wantStyle.Count == 0)
    {
        findings.Add("No dimension type here has the style \"" + askedStyle + "\". The styles here are: "
            + (styles.Count == 0 ? "none" : string.Join(", ", styles.Select(s => s.ToString()))) + ".");
        chosen = new List<DimensionType>();
    }
    else chosen = chosen.Where(t => wantStyle.Contains(t.StyleType)).ToList();
}

var asked = (dimensionTypeName ?? "").Trim();
if (asked.Length > 0 && !string.Equals(asked, "all", StringComparison.OrdinalIgnoreCase))
{
    var named = chosen.Where(t => string.Equals(t.Name, asked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fullName(t), asked, StringComparison.OrdinalIgnoreCase)).ToList();
    if (named.Count == 0)
    {
        var why = "";
        foreach (var other in new FilteredElementCollector(doc).WhereElementIsElementType().ToElements())
        {
            var type = other as ElementType;
            if (type == null || type is DimensionType) continue;
            if (string.Equals(type.Name, asked, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fullName(type), asked, StringComparison.OrdinalIgnoreCase))
            {
                why = " \"" + fullName(type) + "\" is a " + (string.IsNullOrWhiteSpace(type.FamilyName) ? "type" : type.FamilyName)
                    + ", not a dimension type" + (type is TextElementType ? " - REPORT_TEXT_TYPES reads text types" : "") + ".";
                break;
            }
        }
        findings.Add("No dimension type" + (chosen.Count < dimTypes.Count ? " of that style" : "") + " is named \""
            + asked + "\"." + why + " The dimension types here are: " + typeList + ".");
    }
    chosen = named;
}

// ---- The lines -------------------------------------------------------------

var lines = new List<string>();
foreach (var type in chosen)
{
    var parts = new List<string> { fullName(type), "style " + type.StyleType };
    foreach (var setting in settings)
    {
        var p = parameterOf(type, setting.Item2);
        if (p == null) continue;
        parts.Add(labelOf(p, setting.Item1) + " " + shown(p, setting.Item3));
    }
    var units = unitsText(type);
    if (units.Length > 0) parts.Add("Units Format " + units);
    parts.Add("used by " + dimensions.Count(e => e.GetTypeId() == type.Id) + " dimension(s)");
    lines.Add(string.Join(" | ", parts));
}

var typeCount = chosen.Count;
var dimensionTypes = string.Join("  ||  ", lines);
findings.Add(typeCount + " dimension type(s) listed of " + dimTypes.Count + " in "
    + (doc.IsFamilyDocument ? "this family" : "this project") + "; " + dimensionCount
    + " dimension(s) in it. Lengths are paper millimetres, as printed."
    + (hidden > 0 ? " " + hidden + " unnamed type(s) Revit keeps for itself were left out." : ""));
