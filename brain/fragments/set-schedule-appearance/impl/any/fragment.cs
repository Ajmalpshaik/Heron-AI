// NOT STANDALONE. Assumes `doc`, `elements`, `fieldName`, `newHeading`,
// `columnWidthMm`, `alignment`, `headingSideways`, `hideOrShow`,
// `headingOrientation`, `calculateTotals`, `calculation`, `units`,
// `multipleValues` and `cellStyle` are in scope; leaves `changed`,
// `notPresent`, `refused`, `appliedAppearance`, `ambiguous`, `totalled`,
// `appliedTotals`, `alreadyTotalled`, `cannotTotal` and `replacedCalculation`
// behind.
//
// VERSION 5 (2026-11-01, FRAGMENT-ISSUES row 5b-1367, NEEDS-CHECKING Group
// KK), on top of version 4 below: a column's own UNITS, its MULTIPLE VALUES
// text and its CELL STYLE - three more things on the Formatting tab of one
// column, in the block headed "VERSION 5" further down - and `calculation`,
// which widens version 4's totals to the drop-down's other choices:
// minimum, maximum, minimum and maximum, and none. Left out it is "leave",
// and `calculateTotals` decides exactly as in version 4.
//
// VERSION 4 (2026-11-01) - SET_SCHEDULE_FIELD_TOTALS MERGED IN.
//
//   CALCULATE TOTALS - the drop-down on a schedule's Formatting tab, per
//   column - when `calculateTotals` is on. Until a column has it, a group
//   footer and the grand total print only a COUNT of rows, which is why a
//   schedule grouped by room, with footers on, still shows no total flow.
//   Switching the footers on is SET_SCHEDULE_SORT_GROUP's job; this is the
//   other half. ON ONLY: nothing here turns totals off.
//
//   CAN IT TOTAL IS ASKED FIRST. CanTotal is Revit's own answer - text such as
//   a type name or a room name cannot be added up - asked before anything is
//   set, so a refusal changes nothing (`cannotTotal`).
//
//   A COLUMN ALREADY TOTALLING is reported in `alreadyTotalled` and left
//   alone, so a second run is the read-back: it finds it already totalled.
//
//   REVIT ALLOWS ONE CALCULATION PER COLUMN. A column showing its minimum or
//   maximum is switched to totals, because totals is what was asked for, and
//   what it showed before is reported in `replacedCalculation`, never lost.
//
//   WHERE IT PRINTS is said beside every column switched on - a footer, the
//   grand total, or NOWHERE YET - and a HIDDEN column's total prints nowhere
//   until the column is shown. ONE STRING, `appliedTotals`, because a list
//   reaches the chat as three entries cut at sixty characters.
//
//   A NAME THAT COULD MEAN TWO COLUMNS TOUCHES NEITHER - now for EVERY change
//   this card makes, not only totals. A column is found by its field name or
//   its printed heading, and a heading can be renamed to another column's
//   field name. Version 3 took the first match, which restyles whichever
//   column happens to come first; the safer rule is kept and the name is
//   named in `ambiguous`.
//
//   A SCHEDULE HANDED IN TWICE - itself and its placement on a sheet - is one
//   schedule, changed and counted once. NOTHING HANDED IN AT ALL is refused
//   in words, never `changed 0`.
//
//   EVERY STYLE INPUT MAY BE LEFT OUT, so "calculate totals for the Flow
//   column" needs only the column and the switch: newHeading, alignment and
//   hideOrShow left out are "leave", columnWidthMm 0, headingSideways false -
//   each the value that already meant "leave it".
//
// VERSION 3 (FRAGMENT-ISSUES row 5b-620). Two gaps found 2026-10-15 on a real
// project, Revit 2020:
//
//   ONE COLUMN PER CALL. "Make all the fields horizontal and centre" took one
//   call per field. `fieldName` may now be "all" (every column of the
//   schedule) or a list with commas between - "Mark, Size, Length". Still one
//   call, so still one transaction and one undo entry, and every column is
//   read back on its own line. A name that matches a column EXACTLY, commas
//   and all, is that one column as before - so nothing a version 2 caller
//   sent changes meaning. A NEW HEADING IS NEVER WRITTEN ON MORE THAN ONE
//   COLUMN: the same title printed over every column is a mistake nobody
//   asked for, so it is refused and the rest still applied.
//
//   A HEADING COULD NOT BE TURNED BACK. `headingSideways` false meant "leave
//   it", so nothing set Horizontal. `headingOrientation` "horizontal" or
//   "vertical" now sets it either way; "leave" - the card's `absent:` word,
//   so a caller who never sends it gets exactly version 2 - leaves it, and
//   `headingSideways` true still turns it sideways. The two disagreeing
//   (sideways true, orientation horizontal) is refused, not guessed.
//
// HIDING IS NOT REMOVING (version 2). A field added to sort or filter by is
// often not meant to print. Hidden, it keeps doing that job off the drawing;
// removed, the sort goes with it and somebody rebuilds it later wondering what
// changed.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// THE HEADING AND THE FIELD NAME ARE DIFFERENT THINGS AND ONLY ONE PRINTS.
// Renaming a heading to "Duct Size" leaves the field called Size - so sorting
// or filtering *by Size* still works and *by Duct Size* has to find it by
// heading. Both are reported after the change for exactly that reason.
//
// WIDTH ARRIVES IN mm AND IS DIVIDED BY 304.8. GridColumnWidth is a Revit
// internal length like any other. Plain arithmetic at the edge, never a units
// API - that is the call that breaks at Revit 2021.
//
// BOLD, FONT AND SIZE OF ONE COLUMN ARE THIS SCHEDULE'S OWN (version 4), and
// they are the only text styling here. They are a cell-style override on the
// column, as Revit's Modify Schedule ribbon makes one. The schedule's TEXT
// TYPES - title, header, body - are shared with other schedules and drawings;
// this never edits one. Which text type the schedule uses is chosen by
// SET_SCHEDULE_OPTIONS, and editing a text type itself is Revit's own job.
//
// AN EMPTY INPUT MEANS "LEAVE IT ALONE", NOT "SET IT TO EMPTY". A blank heading
// would wipe a column title off a sheet, and a zero width would collapse the
// column - both look like the fragment working.
//
// READ FIRST, WRITE, READ BACK. appliedAppearance is a second read.
//
// AND A SCHEDULE IT CANNOT SEE IS REFUSED, NOT COUNTED AS `changed 0`. Handed
// a schedule placed on a sheet this used to change nothing and say nothing,
// which reads as "the column was already like that". Section 3h.1 of
// docs/FRAGMENT-ISSUES.md, 2026-09-09.

// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE. The same fix and the
// same reason as REPORT_SCHEDULE_DEFINITION, where it was found on 2026-09-08:
// a schedule is a VIEW, a view cannot be selected as an element, opening one
// selects its ROWS, and nothing in this library provides a ViewSchedule to
// chain from. So a bare cast refuses the only input that could ever arrive.
//
// The placement carries the id of the schedule it draws, so it is resolved
// here rather than passed over.
Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;

    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;

    // Guarded like everything else: a placement whose schedule was deleted
    // under it should cost one element, not the whole run.
    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

var changed = 0;
var notPresent = new List<string>();
var refused = new List<string>();
var appliedAppearance = new List<string>();
var ambiguous = new List<string>();
var totalled = 0;
string appliedTotals = "";
var alreadyTotalled = new List<string>();
var cannotTotal = new List<string>();
var replacedCalculation = new List<string>();
var appliedParts = new List<string>();

var handed = 0;
var schedulesSeen = 0;
// VERSION 4: a schedule selected twice - itself and its placement on a sheet,
// or placed on two sheets - is one schedule.
var scheduleIdsSeen = new HashSet<ElementId>();

// VERSION 4: the card's `absent:` word "leave" is the empty that always meant
// "leave it alone".
if (string.Equals((newHeading ?? "").Trim(), "leave", StringComparison.OrdinalIgnoreCase))
    newHeading = "";

var wantedAlignment = (alignment ?? "").Trim().ToLowerInvariant();
if (wantedAlignment == "leave") wantedAlignment = "";

// VERSION 3. "leave", "" or "unchanged" leaves the heading as it is; anything
// but horizontal or vertical is refused once, by name, below.
var wantedOrientation = (headingOrientation ?? "").Trim().ToLowerInvariant();
var orientationLeft = wantedOrientation == "" || wantedOrientation == "leave"
    || wantedOrientation == "unchanged";
var orientationKnown = orientationLeft || wantedOrientation == "horizontal"
    || wantedOrientation == "vertical";

var requestUnusable = false;

// ---- VERSION 5: `calculation`, the Formatting tab's whole drop-down ----
//
// Version 4 could only switch Calculate totals ON (`calculateTotals`). The
// drop-down also offers Calculate minimum, Calculate maximum, Calculate
// minimum and maximum, and No calculation - ScheduleFieldDisplayType Min,
// Max, MinMax and Standard, on every release 2020-2027. `calculation` is
// `optional: true` with `absent: leave`, so a version 4 caller that never
// sends it is bound "leave" and `calculateTotals` decides exactly as before.
// A minimum or maximum is asked of Revit with CanDisplayMinMax first, as a
// total is with CanTotal. A word that is not one of these, or `calculateTotals`
// on with a calculation other than totals, refuses the calculation ONCE, by
// name, and every other setting in the call is still made - this card's rule
// for one bad input (version 3).
Func<ScheduleFieldDisplayType, string> calcWords = kind =>
{
    if (kind == ScheduleFieldDisplayType.Totals) return "totals";
    if (kind == ScheduleFieldDisplayType.MinMax) return "its minimum and maximum";
    if (kind == ScheduleFieldDisplayType.Max) return "its maximum";
    if (kind == ScheduleFieldDisplayType.Min) return "its minimum";
    if (kind == ScheduleFieldDisplayType.Standard) return "no calculation";
    return kind.ToString();
};
ScheduleFieldDisplayType? wantedDisplay = null;
{
    var said = string.Join(" ", (calculation ?? "").Trim().ToLowerInvariant()
        .Replace("calculate ", "").Replace("/", " and ").Replace("&", " and ")
        .Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
    if (said == "" || said == "leave" || said == "unchanged")
    {
        if (calculateTotals) wantedDisplay = ScheduleFieldDisplayType.Totals;
    }
    else
    {
        ScheduleFieldDisplayType? named = null;
        if (said == "totals" || said == "total" || said == "sum") named = ScheduleFieldDisplayType.Totals;
        else if (said == "minimum and maximum" || said == "min and max" || said == "minmax"
                 || said == "min max" || said == "minimum maximum") named = ScheduleFieldDisplayType.MinMax;
        else if (said == "maximum" || said == "max") named = ScheduleFieldDisplayType.Max;
        else if (said == "minimum" || said == "min") named = ScheduleFieldDisplayType.Min;
        else if (said == "none" || said == "no calculation" || said == "off") named = ScheduleFieldDisplayType.Standard;

        if (named == null)
            refused.Add(string.Format("'{0}' is not a calculation Revit offers - say totals, minimum, "
                + "maximum, minimum and maximum, or none. No column's calculation was changed",
                calculation));
        else if (calculateTotals && named.Value != ScheduleFieldDisplayType.Totals)
            refused.Add(string.Format("calculateTotals is on and calculation is '{0}' - a column has one "
                + "calculation, so say one. No column's calculation was changed", calculation));
        else wantedDisplay = named;
    }
}

// ---- VERSION 5: a column's own units, multiple-values text, cell style ----
//
// Three things on the Formatting tab of one column, each `optional: true`
// with `absent: leave` on the card, so a caller of version 3 that never sends
// them is bound "leave" and gets version 3 exactly. Asked for on 2026-11-01:
// Ajmal PS made a duct length schedule on his Revit 2020 job model and could
// not set its Length column to metres - no tool set a column's units
// (FRAGMENT-ISSUES row 5b-1367).
//
// A REFUSAL WRITES NOTHING FOR THAT SETTING, and the rest of the call is still
// made - this card's rule for a bad alignment word. A setting Revit TOOK but
// that reads back different is not a refusal: it throws, and the add-in rolls
// the whole call back, as SET_PROJECT_UNITS does - a column printing the wrong
// units while the answer says "metres" is the worse of the two.

var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
var statics = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitApi = typeof(Units).Assembly;
var revitSpace = typeof(Units).Namespace + ".";
Func<Exception, string> revitSaid = failure =>
    ((failure is System.Reflection.TargetInvocationException && failure.InnerException != null)
        ? failure.InnerException : failure).Message.TrimEnd('.');
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<string, bool> leaveWord = text =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    return t == "" || t == "leave" || t == "unchanged";
};

// -- 1. UNITS. The words SET_PROJECT_UNITS takes for one unit type, here for
// one column: the unit's name or its symbol, the rounding, "no symbol", in any
// order with commas between - "metres, 2 decimal places, m". "project" (or
// "use project settings") ticks Use project settings again.
var unitsText = (units ?? "").Trim();
var unitsLeft = leaveWord(unitsText);
var unitsProject = false;
var unitsWords = new List<string>();
double? unitsRounding = null;
var unitsNoSymbol = false;
var unitsBad = "";

// The column's unit names compared loosely: case, spacing, metre/litre and a
// plural s - SET_PROJECT_UNITS' rule.
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
            return "to the nearest " + accuracy.ToString("0.############", invariantCulture) + " (1/"
                + ((long)whole).ToString(invariantCulture) + ")";
    }
    return "to the nearest " + accuracy.ToString("0.############", invariantCulture);
};
// A rounding in the dialog's words, as the increment: null when the text is
// not a rounding at all, NaN when it is one that cannot be read.
Func<string, double?> roundingOf = text =>
{
    var t = string.Join(" ", (text ?? "").Trim().ToLowerInvariant().Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
    if (t.EndsWith(" decimal places") || t.EndsWith(" decimal place"))
    {
        int places;
        var count = t.Substring(0, t.IndexOf(" decimal place"));
        if (!int.TryParse(count, System.Globalization.NumberStyles.None, invariantCulture, out places) || places > 12) return double.NaN;
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
        if (!double.TryParse(number.Substring(0, slash), System.Globalization.NumberStyles.Float, invariantCulture, out top)
            || !double.TryParse(number.Substring(slash + 1), System.Globalization.NumberStyles.Float, invariantCulture, out bottom)
            || bottom == 0) return double.NaN;
        return top / bottom;
    }
    if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariantCulture, out top)) return double.NaN;
    return top;
};

if (!unitsLeft)
{
    var whole = squash(unitsText);
    if (whole == "project" || whole == "projectsettings" || whole == "useprojectsettings"
        || whole == "projectunits")
        unitsProject = true;
    else
    {
        foreach (var part in unitsText.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var r = roundingOf(part);
            if (r.HasValue)
            {
                if (double.IsNaN(r.Value) || r.Value <= 0)
                    unitsBad = "'" + part + "' is not a rounding Revit takes - '2 decimal places', "
                        + "'rounding 0.5' or 'to the nearest 1/16'";
                else unitsRounding = r;
                continue;
            }
            var s = squash(part);
            if (s == "nosymbol" || s == "none" || s == "withoutsymbol") { unitsNoSymbol = true; continue; }
            unitsWords.Add(part);
        }
        if (unitsBad.Length == 0 && unitsWords.Count == 0 && unitsRounding == null && !unitsNoSymbol)
            unitsBad = "nothing to set - give units ('metres'), a rounding ('2 decimal places'), "
                + "'no symbol', or 'project' to use the project's units";
        if (unitsBad.Length == 0 && unitsWords.Count > 2)
            unitsBad = "more than one unit and one symbol were named - '" + unitsText + "'";
    }
    if (unitsBad.Length > 0)
    {
        refused.Add("units '" + unitsText + "' NOT SET on any column: " + unitsBad);
        unitsLeft = true;
    }
}

// THE TWO SHAPES OF THE UNITS API, ONE IMPLEMENTATION, BY NAME AT RUN TIME.
// Neither shape is named in code: both are reached by name, one
// implementation for every release, as SET_PROJECT_UNITS does. (The add-in
// has handed fragments the release symbols since row 5b-181's fix - this is a
// choice, not a constraint.) A column's unit type is ScheduleField.UnitType
// on Revit 2020 - gone by 2022 - and GetSpecTypeId from 2021, which 2020 has
// never had. So: the ForgeTypeId route wherever GetSpecTypeId exists, the
// enum route (UnitType, DisplayUnitType, UnitSymbolType) where it does not -
// SET_PROJECT_UNITS' two routes, read off the 2020 and 2027 reference
// assemblies, narrowed to one column's unit type.
Func<ScheduleField, object> specOf = null;            // null: not a number with units
Func<object, string> specName = null;
Func<object, FormatOptions> projectFormatOf = null;
Func<FormatOptions, object> unitOf = null;
Func<FormatOptions, object> symbolOf = null;
Func<object, string> unitName = null;
Func<object, string> symbolName = null;               // "" for no symbol
Func<object, List<object>> validUnits = null;
Func<object, List<object>> validSymbols = null;
Func<object, FormatOptions> revitDefault = null;
Action<FormatOptions, object> putUnit = null;
Action<FormatOptions, object> putSymbol = null;
object noSymbol = null;
var unitsRoute = "";

if (!unitsLeft && !unitsProject)
{
    var forgeType = revitApi.GetType(revitSpace + "ForgeTypeId");
    var getSpec = typeof(ScheduleField).GetMethod("GetSpecTypeId", Type.EmptyTypes);
    if (forgeType != null && getSpec != null)
    {
        unitsRoute = "Revit's ForgeTypeId units (2021 and later)";
        var one = new[] { forgeType };
        var labelSpec = typeof(LabelUtils).GetMethod("GetLabelForSpec", statics, null, one, null);
        var labelUnit = typeof(LabelUtils).GetMethod("GetLabelForUnit", statics, null, one, null);
        var labelSymbol = typeof(LabelUtils).GetMethod("GetLabelForSymbol", statics, null, one, null);
        var getFormat = typeof(Units).GetMethod("GetFormatOptions", one);
        var getUnit = typeof(FormatOptions).GetMethod("GetUnitTypeId", Type.EmptyTypes);
        var getSymbol = typeof(FormatOptions).GetMethod("GetSymbolTypeId", Type.EmptyTypes);
        var setUnit = typeof(FormatOptions).GetMethod("SetUnitTypeId", one);
        var setSymbol = typeof(FormatOptions).GetMethod("SetSymbolTypeId", one);
        var unitsOfSpec = typeof(UnitUtils).GetMethod("GetValidUnits", statics, null, one, null);
        var symbolsOfUnit = typeof(FormatOptions).GetMethod("GetValidSymbols", statics, null, one, null);
        var withUnit = typeof(FormatOptions).GetConstructor(one);
        var isEmpty = forgeType.GetMethod("Empty", Type.EmptyTypes);
        // 2022 on. On 2021 the valid-units list alone decides.
        var measurable = typeof(UnitUtils).GetMethod("IsMeasurableSpec", statics, null, one, null);
        Func<object, bool> empty = id => id == null || (bool)isEmpty.Invoke(id, null);
        noSymbol = Activator.CreateInstance(forgeType);
        var projectUnits = doc.GetUnits();

        specOf = field =>
        {
            object spec;
            try { spec = getSpec.Invoke(field, null); } catch (Exception) { return null; }
            if (empty(spec)) return null;
            try { if (measurable != null && !(bool)measurable.Invoke(null, new[] { spec })) return null; }
            catch (Exception) { return null; }
            return spec;
        };
        specName = spec => { try { return labelSpec.Invoke(null, new[] { spec }) as string; } catch (Exception) { return "its unit type"; } };
        projectFormatOf = spec => getFormat.Invoke(projectUnits, new[] { spec }) as FormatOptions;
        unitOf = format => getUnit.Invoke(format, null);
        symbolOf = format => getSymbol.Invoke(format, null);
        unitName = unit => labelUnit.Invoke(null, new[] { unit }) as string;
        symbolName = symbol => empty(symbol) ? "" : (labelSymbol.Invoke(null, new[] { symbol }) as string ?? "");
        validUnits = spec => ((System.Collections.IEnumerable)unitsOfSpec.Invoke(null, new[] { spec })).Cast<object>().ToList();
        validSymbols = unit => ((System.Collections.IEnumerable)symbolsOfUnit.Invoke(null, new[] { unit })).Cast<object>().ToList();
        revitDefault = unit => withUnit.Invoke(new[] { unit }) as FormatOptions;
        putUnit = (format, unit) => setUnit.Invoke(format, new[] { unit });
        putSymbol = (format, symbol) => setSymbol.Invoke(format, new[] { symbol });
    }
    else
    {
        unitsRoute = "Revit 2020's UnitType names";
        var unitTypeOfField = typeof(ScheduleField).GetProperty("UnitType");
        var unitType = revitApi.GetType(revitSpace + "UnitType");
        var displayType = revitApi.GetType(revitSpace + "DisplayUnitType");
        var symbolType = revitApi.GetType(revitSpace + "UnitSymbolType");
        Func<Type, System.Reflection.MethodInfo> labelFor = t =>
            typeof(LabelUtils).GetMethod("GetLabelFor", statics, null, new[] { t }, null);
        var labelType = labelFor(unitType);
        var labelDisplay = labelFor(displayType);
        var labelSymbol = labelFor(symbolType);
        var getFormat = typeof(Units).GetMethod("GetFormatOptions", new[] { unitType });
        var displayUnits = typeof(FormatOptions).GetProperty("DisplayUnits");
        var unitSymbol = typeof(FormatOptions).GetProperty("UnitSymbol");
        var unitsOfType = typeof(UnitUtils).GetMethod("GetValidDisplayUnits", statics, null, new[] { unitType }, null);
        var symbolsOfUnit = typeof(FormatOptions).GetMethod("GetValidUnitSymbols", statics, null, new[] { displayType }, null);
        var withUnit = typeof(FormatOptions).GetConstructor(new[] { displayType });
        noSymbol = Enum.Parse(symbolType, "UST_NONE");
        var projectUnits = doc.GetUnits();

        specOf = field =>
        {
            object spec;
            try { spec = unitTypeOfField.GetValue(field); } catch (Exception) { return null; }
            if (spec == null || spec.ToString() == "UT_Undefined") return null;
            return spec;
        };
        specName = spec => { try { return labelType.Invoke(null, new[] { spec }) as string; } catch (Exception) { return spec.ToString(); } };
        projectFormatOf = spec => getFormat.Invoke(projectUnits, new[] { spec }) as FormatOptions;
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
    }
}

Func<FormatOptions, string> describeFormat = format =>
{
    var symbol = symbolName(symbolOf(format));
    return unitName(unitOf(format)) + (symbol.Length > 0 ? " (" + symbol + ")" : " (no symbol)")
        + ", " + roundingText(format.Accuracy);
};
// A column's format as the Field Format dialog shows it. A format with Use
// project settings ticked holds no units of its own - reading them is not
// safe - so the project's own format for that unit type is named instead.
Func<ScheduleField, string> columnFormat = field =>
{
    FormatOptions format;
    try { format = field.GetFormatOptions(); } catch (Exception failure) { return "unreadable (" + revitSaid(failure) + ")"; }
    if (format == null) return "unreadable";
    if (format.UseDefault)
    {
        if (specOf == null) return "project settings";
        var spec = specOf(field);
        if (spec == null) return "project settings";
        try { return "project settings (" + describeFormat(projectFormatOf(spec)) + ")"; }
        catch (Exception) { return "project settings"; }
    }
    if (specOf == null) return "its own units, not the project's";
    try { return describeFormat(format); } catch (Exception) { return "its own units, not the project's"; }
};

// -- 3. MULTIPLE VALUES TEXT - what a row shows when the elements it gathers
// disagree. ScheduleField.MultipleValuesDisplayType and MultipleValuesCustomText
// arrived at Revit 2022 (read off the reference assemblies: 2020 and 2021 have
// neither, nor the enum), so they are reached by name; on 2020 and 2021 this
// one setting is refused and the rest of the call still made.
var multipleText = (multipleValues ?? "").Trim();
var multipleLeft = leaveWord(multipleText);
string multipleMode = null;       // the enum's own name: Project, Varies, Custom
string multipleCustom = null;
var multipleProperty = typeof(ScheduleField).GetProperty("MultipleValuesDisplayType");
var multipleCustomProperty = typeof(ScheduleField).GetProperty("MultipleValuesCustomText");
if (!multipleLeft)
{
    var word = squash(multipleText);
    if (word == "project" || word == "projectsettings" || word == "useprojectsettings") multipleMode = "Project";
    else if (word == "varies") multipleMode = "Varies";
    else if (multipleText.ToLowerInvariant().StartsWith("custom:"))
    {
        multipleCustom = multipleText.Substring(7).Trim();
        if (multipleCustom.Length > 0) multipleMode = "Custom";
    }

    if (multipleMode == null)
    {
        refused.Add("multiple values '" + multipleText + "' NOT SET on any column: say 'project', "
            + "'varies' or 'custom: <the text to print>'");
        multipleLeft = true;
    }
    else if (multipleProperty == null || multipleCustomProperty == null)
    {
        refused.Add("multiple values NOT SET on any column: Revit " + doc.Application.VersionNumber
            + " has no setting for a column's multiple-values text - it arrived at Revit 2022. "
            + "The rest of this call was still made");
        multipleLeft = true;
    }
}

// -- 4. CELL STYLE OF ONE COLUMN - bold, italic, underline, colour, font,
// size - as Revit's Modify Schedule ribbon overrides it on the selected cells.
// THIS SCHEDULE'S COLUMN ONLY, through ScheduleField.GetStyle and SetStyle
// with only the asked overrides switched on and every override already there
// kept. It never touches a TEXT TYPE, which other schedules share - choosing
// which text type the whole schedule's title, headers or body use is
// SET_SCHEDULE_OPTIONS'. "reset" takes every override off the column.
var styleText = (cellStyle ?? "").Trim();
var styleLeft = leaveWord(styleText);
var styleReset = false;
bool? wantBold = null, wantItalic = null, wantUnderline = null;
Color wantColour = null;
string wantColourName = null;
string wantFont = null;
double wantSizeMm = 0;
var styleBad = new List<string>();
if (!styleLeft)
{
    foreach (var part in styleText.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
    {
        var lower = string.Join(" ", part.ToLowerInvariant().Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
        if (lower == "reset" || lower == "reset style" || lower == "clear") { styleReset = true; continue; }
        if (lower == "bold") { wantBold = true; continue; }
        if (lower == "not bold" || lower == "no bold" || lower == "regular") { wantBold = false; continue; }
        if (lower == "italic") { wantItalic = true; continue; }
        if (lower == "not italic" || lower == "no italic") { wantItalic = false; continue; }
        if (lower == "underline" || lower == "underlined") { wantUnderline = true; continue; }
        if (lower == "not underline" || lower == "not underlined" || lower == "no underline") { wantUnderline = false; continue; }
        if (lower.StartsWith("font "))
        {
            wantFont = part.Trim().Substring(5).Trim();
            if (wantFont.Length == 0) styleBad.Add("'" + part + "' names no font");
            continue;
        }
        if (lower.StartsWith("size "))
        {
            var number = lower.Substring(5).Trim();
            if (number.EndsWith("mm")) number = number.Substring(0, number.Length - 2).Trim();
            double size;
            if (double.TryParse(number, System.Globalization.NumberStyles.Float, invariantCulture, out size) && size > 0 && size < 100)
                wantSizeMm = size;
            else styleBad.Add("'" + part + "' is not a text size - say 'size 2.5 mm'");
            continue;
        }
        if (lower.StartsWith("colour ") || lower.StartsWith("color "))
        {
            var value = lower.Substring(lower.IndexOf(' ') + 1).Trim();
            var named = new Dictionary<string, int[]>
            {
                { "black", new[] { 0, 0, 0 } }, { "white", new[] { 255, 255, 255 } },
                { "red", new[] { 255, 0, 0 } }, { "green", new[] { 0, 128, 0 } },
                { "blue", new[] { 0, 0, 255 } }, { "yellow", new[] { 255, 255, 0 } },
                { "orange", new[] { 255, 128, 0 } }, { "cyan", new[] { 0, 255, 255 } },
                { "magenta", new[] { 255, 0, 255 } }, { "grey", new[] { 128, 128, 128 } },
                { "gray", new[] { 128, 128, 128 } },
            };
            int[] rgb = null;
            if (named.ContainsKey(value)) rgb = named[value];
            else if (value.StartsWith("#") && value.Length == 7)
            {
                try
                {
                    rgb = new[] { Convert.ToInt32(value.Substring(1, 2), 16), Convert.ToInt32(value.Substring(3, 2), 16),
                                  Convert.ToInt32(value.Substring(5, 2), 16) };
                }
                catch (Exception) { rgb = null; }
            }
            else
            {
                var pieces = value.Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries);
                int r, g, b;
                if (pieces.Length == 3 && int.TryParse(pieces[0], out r) && int.TryParse(pieces[1], out g)
                    && int.TryParse(pieces[2], out b) && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
                    rgb = new[] { r, g, b };
            }
            if (rgb == null)
                styleBad.Add("'" + part + "' is not a colour - say a name (" + string.Join(", ", named.Keys.ToArray())
                    + "), '#FF0000' or three numbers '255 0 0'");
            else
            {
                wantColour = new Color((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
                wantColourName = value;
            }
            continue;
        }
        styleBad.Add("'" + part + "' is not a style word");
    }
    if (styleReset && (wantBold != null || wantItalic != null || wantUnderline != null
        || wantColour != null || wantFont != null || wantSizeMm > 0))
        styleBad.Add("'reset' takes every override off and cannot be sent with other style words");
    if (styleBad.Count > 0)
    {
        refused.Add("cell style '" + styleText + "' NOT SET on any column: " + string.Join("; ", styleBad.ToArray())
            + ". Style words: bold, not bold, italic, not italic, underline, not underline, colour <name, #RRGGBB "
            + "or R G B>, font <name>, size <N> mm, reset");
        styleLeft = true;
    }
}

// TEXT SIZE - AN ASSUMPTION THE PROOF SETTLES. TableCellStyle.TextSize is a
// double, and the reference does not say in what. It is written here as a
// Revit internal length - mm / 304.8, like every other distance on paper - and
// read back both ways: in mm on that assumption, and as Revit's own number,
// so the Edit Font dialog can say which it is.
Func<ScheduleField, string> styleNow = field =>
{
    try
    {
        var style = field.GetStyle();
        var on = style.GetCellStyleOverrideOptions();
        var parts = new List<string>();
        if (on.Bold) parts.Add(style.IsFontBold ? "bold" : "not bold");
        if (on.Italics) parts.Add(style.IsFontItalic ? "italic" : "not italic");
        if (on.Underline) parts.Add(style.IsFontUnderline ? "underline" : "not underline");
        if (on.FontColor)
        {
            var c = style.TextColor;
            parts.Add(c != null && c.IsValid ? string.Format("colour {0} {1} {2}", c.Red, c.Green, c.Blue) : "colour (none)");
        }
        if (on.Font) parts.Add("font " + style.FontName);
        if (on.FontSize)
            parts.Add(string.Format(invariantCulture, "size {0:0.##} mm if feet (Revit's number {1:0.######})",
                style.TextSize * 304.8, style.TextSize));
        return parts.Count == 0 ? "no style override" : string.Join(", ", parts.ToArray());
    }
    catch (Exception failure) { return "style unreadable (" + revitSaid(failure) + ")"; }
};

if (string.IsNullOrEmpty((fieldName ?? "").Trim()))
{
    requestUnusable = true;
    refused.Add("no column was named, so there is nothing to restyle. NOTHING WAS CHANGED - "
        + "name the field or the heading as it prints");
}

// VERSION 3. Refused before anything is written: sideways true says vertical,
// and horizontal says the opposite. Guessing either would undo what the other
// half of the request asked for.
if (!requestUnusable && headingSideways && wantedOrientation == "horizontal")
{
    requestUnusable = true;
    refused.Add("headingSideways is true and headingOrientation is horizontal - they ask for "
        + "opposite things. NOTHING WAS CHANGED - send headingOrientation alone");
}

// VERSION 3. "all", or the whole text matching no column and holding a comma,
// names more than one column. These are the words for every column.
var trimmedFieldName = (fieldName ?? "").Trim();
var everyWord = trimmedFieldName.ToLowerInvariant();
var meansEvery = everyWord == "all" || everyWord == "*" || everyWord == "every"
    || everyWord == "all fields" || everyWord == "all columns"
    || everyWord == "every field" || everyWord == "every column";

Func<ScheduleField, string, bool> answersTo = (field, wanted) =>
    string.Equals(field.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
    || string.Equals(field.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase);

foreach (var element in elements)
{
    if (requestUnusable) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    if (!scheduleIdsSeen.Add(schedule.Id)) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    // VERSION 4. Where a total prints on this schedule: a group footer, the
    // grand total, or neither - said beside every column switched on, because
    // totals on a schedule with no footer and no grand total print nowhere.
    string printsWhere = "";
    if (wantedDisplay.HasValue)
    {
        var footerGroups = new List<string>();
        try
        {
            foreach (var rule in definition.GetSortGroupFields())
            {
                if (!rule.ShowFooter) continue;
                var groupField = definition.GetField(rule.FieldId);
                footerGroups.Add(groupField == null ? "(a field no longer in the schedule)" : groupField.GetName());
            }
        }
        catch (Exception) { }

        bool grandTotal = false;
        try { grandTotal = definition.ShowGrandTotal; } catch (Exception) { }

        if (footerGroups.Count > 0 && grandTotal)
            printsWhere = string.Format("prints in each {0} footer and in the grand total",
                string.Join(" / ", footerGroups));
        else if (footerGroups.Count > 0)
            printsWhere = string.Format("prints in each {0} footer; the grand total is off",
                string.Join(" / ", footerGroups));
        else if (grandTotal)
            printsWhere = "prints in the grand total; no group has a footer";
        else
            printsWhere = "BUT PRINTS NOWHERE YET - no group has a footer and the grand total is "
                + "off. Switch one on in the schedule's Sorting/Grouping";
    }

    var fields = new List<ScheduleField>();
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var field = definition.GetField(fieldId);
        if (field != null) fields.Add(field);
    }

    // Each target is a column and the words it is reported under: the text as
    // given when it was named, its own name when "all" found it.
    var targets = new List<KeyValuePair<ScheduleField, string>>();

    // The whole text first, exactly as version 2 matched it - so a field
    // called "all", or one whose name holds a comma, is still that one field.
    // VERSION 4: EVERY column answering to it is found, and more than one
    // touches NONE - a heading renamed to another column's field name would
    // otherwise restyle whichever came first.
    var wholeMatches = fields.Where(f => answersTo(f, fieldName)).ToList();
    if (wholeMatches.Count > 1)
    {
        ambiguous.Add(string.Format("'{0}' on '{1}' - {2} columns answer to that name, by field "
            + "name or heading, so NONE was touched. Rename a heading, or name the column "
            + "another way", fieldName, schedule.Name, wholeMatches.Count));
        continue;
    }
    if (wholeMatches.Count == 1)
        targets.Add(new KeyValuePair<ScheduleField, string>(wholeMatches[0], fieldName));

    if (targets.Count == 0 && meansEvery)
    {
        foreach (var field in fields)
            targets.Add(new KeyValuePair<ScheduleField, string>(field, field.GetName()));

        if (targets.Count == 0)
        {
            notPresent.Add(string.Format("'{0}' on '{1}' - this schedule has no columns",
                fieldName, schedule.Name));
            continue;
        }
    }
    else if (targets.Count == 0 && trimmedFieldName.IndexOf(',') >= 0)
    {
        var taken = new HashSet<int>();
        foreach (var piece in trimmedFieldName.Split(','))
        {
            var wanted = piece.Trim();
            if (wanted.Length == 0) continue;

            // VERSION 4: every column answering to the piece; more than one
            // touches none of them.
            var pieceMatches = fields.Where(f => answersTo(f, wanted)).ToList();
            if (pieceMatches.Count > 1)
            {
                ambiguous.Add(string.Format("'{0}' on '{1}' - {2} columns answer to that name, by "
                    + "field name or heading, so NONE was touched. Rename a heading, or name the "
                    + "column another way", wanted, schedule.Name, pieceMatches.Count));
                continue;
            }
            ScheduleField match = pieceMatches.Count == 1 ? pieceMatches[0] : null;

            if (match == null)
            {
                notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule",
                    wanted, schedule.Name));
                continue;
            }

            // The same column named twice, once by name and once by heading,
            // is written and read back once. Told apart by its place in the
            // schedule, which needs no API that differs between releases.
            if (!taken.Add(fields.IndexOf(match))) continue;
            targets.Add(new KeyValuePair<ScheduleField, string>(match, wanted));
        }

        if (targets.Count == 0) continue;
    }

    if (targets.Count == 0)
    {
        notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule",
            fieldName, schedule.Name));
        continue;
    }

    // A heading names one column. Written over several it prints one title
    // above all of them - refused, and every other change still made.
    var headingWanted = !string.IsNullOrEmpty(newHeading);
    if (headingWanted && targets.Count > 1)
    {
        headingWanted = false;
        refused.Add(string.Format("'{0}' on '{1}' - heading NOT written: one heading over {2} "
            + "columns would print the same title above each. Name one column to rename it",
            newHeading, schedule.Name, targets.Count));
    }

    // A word that is not an alignment or an orientation is said once per
    // schedule, as version 2 said it - not once for every column.
    var alignmentRefusedHere = false;
    var orientationRefusedHere = false;

    foreach (var target in targets)
    {
        var found = target.Key;
        var named = target.Value;
        var didSomething = false;

        // Heading text. Empty means leave it alone - a blank heading would wipe a
        // column title off an issued sheet.
        if (headingWanted)
        {
            try { found.ColumnHeading = newHeading; didSomething = true; }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - heading refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }

        // Width. mm to internal feet, plain arithmetic. Zero or less means leave it.
        if (columnWidthMm > 0)
        {
            try { found.GridColumnWidth = columnWidthMm / 304.8; didSomething = true; }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - width refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }

        if (wantedAlignment == "left" || wantedAlignment == "center" || wantedAlignment == "centre"
            || wantedAlignment == "right")
        {
            try
            {
                found.HorizontalAlignment = wantedAlignment == "left"
                    ? ScheduleHorizontalAlignment.Left
                    : wantedAlignment == "right"
                        ? ScheduleHorizontalAlignment.Right
                        : ScheduleHorizontalAlignment.Center;
                didSomething = true;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - alignment refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }
        else if (!string.IsNullOrEmpty(wantedAlignment) && !alignmentRefusedHere)
        {
            alignmentRefusedHere = true;
            refused.Add(string.Format("'{0}' is not an alignment this fragment writes. Use left, "
                + "centre or right", alignment));
        }

        // VERSION 2. Empty leaves it alone, exactly like every other input here -
        // a fragment that wrote a default would un-hide a column somebody hid
        // deliberately, every time it was called to change a width.
        string visibility = (hideOrShow ?? "").Trim().ToLowerInvariant();
        if (visibility == "hide" || visibility == "show")
        {
            try
            {
                found.IsHidden = visibility == "hide";
                didSomething = true;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - hiding refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }

        // VERSION 3. headingOrientation decides when it names a way; otherwise
        // headingSideways true turns the heading sideways, as in version 2, and
        // false leaves it.
        if (!orientationKnown)
        {
            if (!orientationRefusedHere)
                refused.Add(string.Format("'{0}' is not a heading orientation this fragment "
                    + "writes. Use horizontal or vertical", headingOrientation));
            orientationRefusedHere = true;
        }
        else if (!orientationLeft || headingSideways)
        {
            try
            {
                found.HeadingOrientation = wantedOrientation == "horizontal"
                    ? ScheduleHeadingOrientation.Horizontal
                    : ScheduleHeadingOrientation.Vertical;
                didSomething = true;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - heading orientation refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }

        // VERSION 4, from SET_SCHEDULE_FIELD_TOTALS - CALCULATE TOTALS, asked of
        // Revit first, read back through a fresh lookup. VERSION 5 widens it to
        // the drop-down's other choices through `calculation`: minimum, maximum,
        // minimum and maximum, or none. `wantedDisplay` was decided once, above
        // the loop - totals when `calculateTotals` is on and `calculation` is
        // left out, exactly version 4.
        if (wantedDisplay.HasValue)
        {
            var name = found.GetName();
            var calcTarget = wantedDisplay.Value;
            var minMaxKind = calcTarget == ScheduleFieldDisplayType.Min
                || calcTarget == ScheduleFieldDisplayType.Max
                || calcTarget == ScheduleFieldDisplayType.MinMax;
            var canCalc = true;
            if (calcTarget == ScheduleFieldDisplayType.Totals)
            {
                try { canCalc = found.CanTotal(); }
                catch (Exception) { canCalc = false; }
            }
            else if (minMaxKind)
            {
                try { canCalc = found.CanDisplayMinMax(); }
                catch (Exception) { canCalc = false; }
            }

            if (!canCalc)
            {
                cannotTotal.Add(calcTarget == ScheduleFieldDisplayType.Totals
                    ? string.Format("'{0}' on '{1}' - Revit does not total this column. Only a number "
                        + "can be added up; text such as a type name or a room name cannot",
                        name, schedule.Name)
                    : string.Format("'{0}' on '{1}' - Revit does not show a {2} for this column "
                        + "(CanDisplayMinMax says no). Only a number has one", name, schedule.Name,
                        calcWords(calcTarget)));
            }
            else if (found.DisplayType == calcTarget)
            {
                alreadyTotalled.Add(string.Format("'{0}' on '{1}' - already {2}, left as it was",
                    name, schedule.Name, calcTarget == ScheduleFieldDisplayType.Standard
                        ? "has no calculation" : "calculates " + calcWords(calcTarget)));
            }
            else
            {
                var before = found.DisplayType;
                var calcTook = false;
                try { found.DisplayType = calcTarget; calcTook = true; }
                catch (Exception ex)
                {
                    cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit refused {2}: {3}",
                        name, schedule.Name, calcTarget == ScheduleFieldDisplayType.Standard
                            ? "to clear its calculation" : "to calculate " + calcWords(calcTarget), ex.Message));
                }

                ScheduleField readBack = null;
                if (calcTook)
                {
                    try { readBack = definition.GetField(found.FieldId); } catch (Exception) { }
                    if (readBack == null || readBack.DisplayType != calcTarget)
                    {
                        cannotTotal.Add(string.Format("'{0}' on '{1}' - asked for {2}, a read back "
                            + "finds {3}", name, schedule.Name, calcWords(calcTarget),
                            readBack == null ? "the column gone" : calcWords(readBack.DisplayType)));
                        calcTook = false;
                    }
                }

                if (calcTook)
                {
                    didSomething = true;

                    var was = "";
                    if (before != ScheduleFieldDisplayType.Standard)
                    {
                        was = calcWords(before);
                        replacedCalculation.Add(string.Format("'{0}' on '{1}' was calculating {2}. "
                            + "Revit allows one calculation per column, so it now {3}", name,
                            schedule.Name, was, calcTarget == ScheduleFieldDisplayType.Standard
                                ? "has no calculation" : "calculates " + calcWords(calcTarget) + " instead"));
                    }

                    // A HIDDEN COLUMN'S CALCULATION PRINTS NOWHERE. The setting
                    // is kept - it shows the moment the column does - but saying
                    // it prints in the footer would be untrue.
                    var isHidden = false;
                    try { isHidden = readBack.IsHidden; } catch (Exception) { isHidden = false; }

                    totalled++;
                    appliedParts.Add(calcTarget == ScheduleFieldDisplayType.Standard
                        ? string.Format("'{0}' on '{1}' now has no calculation{2}", name, schedule.Name,
                            was.Length == 0 ? "" : string.Format(" (it was calculating {0})", was))
                        : string.Format("'{0}' on '{1}' now calculates {2}{3} - {4}",
                            name, schedule.Name, calcWords(calcTarget),
                            was.Length == 0 ? "" : string.Format(" (it was calculating {0})", was),
                            isHidden
                                ? "BUT THE COLUMN IS HIDDEN, so it prints nowhere until the column is shown"
                                : printsWhere));
                }
            }
        }

        // VERSION 5. Each of the three is asked for, written, and read back on
        // its own - a refusal of one leaves the other two to be made.
        var extraReadBack = new List<string>();

        // -- units
        if (!unitsLeft)
        {
            var was = columnFormat(found);
            FormatOptions next = null;
            string wantedText = null;
            if (unitsProject)
            {
                next = new FormatOptions();
                next.UseDefault = true;
                wantedText = "project settings";
            }
            else
            {
                var spec = specOf(found);
                List<object> offered = null;
                if (spec != null)
                {
                    try { offered = validUnits(spec); } catch (Exception) { offered = null; }
                }
                if (spec == null || offered == null || offered.Count == 0)
                {
                    refused.Add(string.Format("'{0}' on '{1}' - units NOT SET: this column is not a number "
                        + "with units (text, a count or a yes/no has none), so Revit's Field Format has "
                        + "nothing to set", named, schedule.Name));
                }
                else
                {
                    Func<string> offers = () => string.Join(", ", offered.Select(u =>
                    {
                        var symbols = validSymbols(u).Select(s => symbolName(s)).Where(s => s.Length > 0).ToArray();
                        return unitName(u) + (symbols.Length > 0 ? " (" + string.Join("/", symbols) + ")" : "");
                    }).ToArray());

                    FormatOptions current = null;
                    try
                    {
                        var own = found.GetFormatOptions();
                        // Use project settings ticked: the column has no units of
                        // its own, so it starts from the project's for its unit
                        // type - what Revit's dialog shows the moment the box is
                        // unticked.
                        current = own != null && !own.UseDefault ? own : projectFormatOf(spec);
                    }
                    catch (Exception failure)
                    {
                        refused.Add(string.Format("'{0}' on '{1}' - units NOT SET: its format could not be "
                            + "read: {2}", named, schedule.Name, revitSaid(failure)));
                    }

                    object unit = null;
                    object symbol = null;
                    string problem = null;
                    if (current != null)
                    {
                        foreach (var word in unitsWords)
                        {
                            var byName = offered.Where(u => loose(unitName(u)) == loose(word)).ToList();
                            if (byName.Count == 1)
                            {
                                if (unit == null || unitName(unit) == unitName(byName[0])) { unit = byName[0]; continue; }
                                problem = "two units were named, " + unitName(unit) + " and " + unitName(byName[0]);
                                break;
                            }
                            var bySymbol = new List<Tuple<object, object>>();
                            foreach (var u in offered)
                                foreach (var s in validSymbols(u))
                                    if (symbolName(s).Length > 0 && symbolName(s) == word) bySymbol.Add(Tuple.Create(u, s));
                            if (bySymbol.Count == 0)
                                foreach (var u in offered)
                                    foreach (var s in validSymbols(u))
                                        if (symbolName(s).Length > 0 && string.Equals(symbolName(s), word, StringComparison.OrdinalIgnoreCase))
                                            bySymbol.Add(Tuple.Create(u, s));
                            if (unit != null) bySymbol = bySymbol.Where(p => unitName(p.Item1) == unitName(unit)).ToList();
                            if (bySymbol.Count == 1 && symbol == null)
                            {
                                if (unit == null) unit = bySymbol[0].Item1;
                                symbol = bySymbol[0].Item2;
                                continue;
                            }
                            problem = "'" + word + "' is not " + (bySymbol.Count > 1 ? "one" : "a") + " unit or symbol Revit "
                                + "offers for this column's " + specName(spec)
                                + (bySymbol.Count > 1 ? " - more than one unit has that symbol; name the unit" : "")
                                + ". It offers " + offers();
                            break;
                        }
                        if (problem == null && unitsNoSymbol && symbol != null) problem = "a symbol and 'no symbol' were both given";

                        if (problem == null)
                        {
                            var currentUnit = unitOf(current);
                            var sameUnit = unit == null || unitName(unit) == unitName(currentUnit);
                            if (unit == null) unit = currentUnit;
                            if (symbol == null)
                            {
                                // SET_PROJECT_UNITS' rule: the old symbol when the new
                                // unit offers it, else the first the unit offers.
                                if (unitsNoSymbol) symbol = noSymbol;
                                else
                                {
                                    var oldSymbol = symbolName(symbolOf(current));
                                    var symbols = validSymbols(unit);
                                    var keep = symbols.FirstOrDefault(s => symbolName(s) == oldSymbol);
                                    var first = symbols.FirstOrDefault(s => symbolName(s).Length > 0);
                                    if (oldSymbol.Length == 0 && sameUnit) symbol = noSymbol;
                                    else if (keep != null) symbol = keep;
                                    else if (first != null) symbol = first;
                                    else symbol = noSymbol;
                                }
                            }
                            next = new FormatOptions(current);
                            next.UseDefault = false;
                            try
                            {
                                putUnit(next, unit);
                                putSymbol(next, symbol);
                            }
                            catch (Exception failure)
                            {
                                problem = "Revit would not take " + unitName(unit) + ": " + revitSaid(failure);
                                next = null;
                            }
                            if (next != null)
                            {
                                var wantRounding = unitsRounding ?? current.Accuracy;
                                if (next.IsValidAccuracy(wantRounding)) next.Accuracy = wantRounding;
                                else if (unitsRounding.HasValue)
                                {
                                    problem = "Revit does not take a rounding of " + roundingText(unitsRounding.Value)
                                        + " in " + unitName(unit);
                                    next = null;
                                }
                                else next.Accuracy = revitDefault(unit).Accuracy;
                            }
                            if (next != null)
                            {
                                if (next.SuppressTrailingZeros && !next.CanSuppressTrailingZeros()) next.SuppressTrailingZeros = false;
                                if (next.SuppressLeadingZeros && !next.CanSuppressLeadingZeros()) next.SuppressLeadingZeros = false;
                                if (next.SuppressSpaces && !next.CanSuppressSpaces()) next.SuppressSpaces = false;
                                if (next.UsePlusPrefix && !next.CanUsePlusPrefix()) next.UsePlusPrefix = false;
                                wantedText = describeFormat(next);
                            }
                        }
                        if (problem != null)
                            refused.Add(string.Format("'{0}' on '{1}' - units NOT SET: {2}", named, schedule.Name, problem));
                    }
                }
            }

            if (next != null)
            {
                var written = false;
                try { found.SetFormatOptions(next); written = true; }
                catch (Exception failure)
                {
                    refused.Add(string.Format("'{0}' on '{1}' - units refused by Revit: {2}",
                        named, schedule.Name, revitSaid(failure)));
                }
                if (written)
                {
                    // Read back through a fresh lookup of the same column. Revit
                    // TOOK it, so a different answer is not a refusal: the whole
                    // call is rolled back rather than a column left printing units
                    // nobody asked for.
                    var again = definition.GetField(found.FieldId);
                    var back = again == null ? "the column gone" : columnFormat(again);
                    var matches = unitsProject ? back.StartsWith("project settings") : back == wantedText;
                    if (!matches)
                        throw new InvalidOperationException(string.Format("'{0}' on '{1}': units were asked to be "
                            + "{2} and read back {3}. Nothing is kept - the whole change is rolled back",
                            named, schedule.Name, wantedText, back));
                    extraReadBack.Add("units " + was + " -> " + back);
                    didSomething = true;
                }
            }
        }

        // -- multiple values text
        if (!multipleLeft)
        {
            try
            {
                var modeValue = Enum.Parse(multipleProperty.PropertyType, multipleMode);
                multipleProperty.SetValue(found, modeValue, null);
                if (multipleCustom != null) multipleCustomProperty.SetValue(found, multipleCustom, null);
                var again = definition.GetField(found.FieldId);
                var backMode = multipleProperty.GetValue(again, null).ToString();
                var backText = multipleCustomProperty.GetValue(again, null) as string ?? "";
                if (backMode != multipleMode || (multipleCustom != null && backText != multipleCustom))
                    throw new InvalidOperationException(string.Format("'{0}' on '{1}': multiple values were asked "
                        + "to be {2} and read back {3}. Nothing is kept - the whole change is rolled back",
                        named, schedule.Name, multipleMode + (multipleCustom != null ? " '" + multipleCustom + "'" : ""),
                        backMode + (backMode == "Custom" ? " '" + backText + "'" : "")));
                extraReadBack.Add("multiple values " + (backMode == "Custom" ? "custom '" + backText + "'"
                    : backMode == "Varies" ? "<varies>" : "project settings"));
                didSomething = true;
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception failure)
            {
                refused.Add(string.Format("'{0}' on '{1}' - multiple values refused by Revit: {2}",
                    named, schedule.Name, revitSaid(failure)));
            }
        }

        // -- cell style, this column only
        if (!styleLeft)
        {
            try
            {
                if (styleReset)
                {
                    // ASSUMPTION THE PROOF SETTLES: the override options switched
                    // off are the column's style override cleared, as the ribbon's
                    // Reset does. Nothing else of the column is touched.
                    var style = new TableCellStyle(found.GetStyle());
                    var none = new TableCellStyleOverrideOptions();
                    none.SetAllOverrides(false);
                    style.SetCellStyleOverrideOptions(none);
                    found.SetStyle(style);
                }
                else
                {
                    var style = new TableCellStyle(found.GetStyle());
                    var on = new TableCellStyleOverrideOptions(style.GetCellStyleOverrideOptions());
                    if (wantBold.HasValue) { on.Bold = true; style.IsFontBold = wantBold.Value; }
                    if (wantItalic.HasValue) { on.Italics = true; style.IsFontItalic = wantItalic.Value; }
                    if (wantUnderline.HasValue) { on.Underline = true; style.IsFontUnderline = wantUnderline.Value; }
                    if (wantColour != null) { on.FontColor = true; style.TextColor = wantColour; }
                    if (wantFont != null) { on.Font = true; style.FontName = wantFont; }
                    if (wantSizeMm > 0) { on.FontSize = true; style.TextSize = wantSizeMm / 304.8; }
                    style.SetCellStyleOverrideOptions(on);
                    found.SetStyle(style);
                }

                var again = definition.GetField(found.FieldId);
                var back = styleNow(again);
                var wrong = new List<string>();
                var backStyle = again.GetStyle();
                var backOn = backStyle.GetCellStyleOverrideOptions();
                if (styleReset)
                {
                    if (backOn.Bold || backOn.Italics || backOn.Underline || backOn.FontColor || backOn.Font || backOn.FontSize)
                        wrong.Add("an override is still on: " + back);
                }
                else
                {
                    if (wantBold.HasValue && (!backOn.Bold || backStyle.IsFontBold != wantBold.Value)) wrong.Add("bold");
                    if (wantItalic.HasValue && (!backOn.Italics || backStyle.IsFontItalic != wantItalic.Value)) wrong.Add("italic");
                    if (wantUnderline.HasValue && (!backOn.Underline || backStyle.IsFontUnderline != wantUnderline.Value)) wrong.Add("underline");
                    if (wantColour != null && (!backOn.FontColor || backStyle.TextColor == null || !backStyle.TextColor.IsValid
                        || backStyle.TextColor.Red != wantColour.Red || backStyle.TextColor.Green != wantColour.Green
                        || backStyle.TextColor.Blue != wantColour.Blue)) wrong.Add("colour " + wantColourName);
                    if (wantFont != null && (!backOn.Font || !string.Equals(backStyle.FontName, wantFont, StringComparison.OrdinalIgnoreCase)))
                        wrong.Add("font " + wantFont);
                    if (wantSizeMm > 0 && (!backOn.FontSize || Math.Abs(backStyle.TextSize - wantSizeMm / 304.8) > 1e-9))
                        wrong.Add("size " + wantSizeMm.ToString(invariantCulture) + " mm");
                }
                if (wrong.Count > 0)
                    throw new InvalidOperationException(string.Format("'{0}' on '{1}': the cell style did not read "
                        + "back as asked ({2}) - it reads {3}. Nothing is kept - the whole change is rolled back",
                        named, schedule.Name, string.Join(", ", wrong.ToArray()), back));
                extraReadBack.Add("style " + back);
                didSomething = true;
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception failure)
            {
                refused.Add(string.Format("'{0}' on '{1}' - cell style refused by Revit: {2}",
                    named, schedule.Name, revitSaid(failure)));
            }
        }

        if (!didSomething) continue;

        // Read back, one line per column. Width is reported in mm, which is what
        // it arrived as.
        try
        {
            var after = definition.GetField(found.FieldId);
            if (after == null)
            {
                refused.Add(string.Format("'{0}' on '{1}' - changed, and a read back cannot find the "
                    + "field", named, schedule.Name));
                continue;
            }

            appliedAppearance.Add(string.Format(
                "'{0}' / field '{1}': prints as '{2}', {3:0.#} mm wide, {4}, heading {5}{6}",
                schedule.Name, after.GetName(), after.ColumnHeading,
                after.GridColumnWidth * 304.8, after.HorizontalAlignment,
                after.HeadingOrientation,
                extraReadBack.Count == 0 ? "" : "; " + string.Join("; ", extraReadBack.ToArray())));
            changed++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on '{1}' - could not be read back: {2}",
                named, schedule.Name, ex.Message));
        }
    }
}

// Decided after the loop, and safe there: every element that is not a schedule
// `continue`s before a definition is reached, so nothing has been written.
if (handed > 0 && schedulesSeen == 0)
{
    refused.Add(string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS CHANGED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed));
}
else if (handed == 0 && !requestUnusable)
{
    // VERSION 4, from SET_SCHEDULE_FIELD_TOTALS: nothing handed in is said in
    // words, never left to read as "changed 0".
    refused.Add("no schedule was handed in - nothing was found by that name, and nothing is "
        + "selected - so NOTHING WAS CHANGED. Name the schedule, or click it on a sheet");
}

appliedTotals = string.Join("; ", appliedParts);
