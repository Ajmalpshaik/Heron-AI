// NOT STANDALONE. Assumes `doc`, `settings` and `system` are in scope; leaves
// `before`, `after`, `changed`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// MANAGE > MEP SETTINGS > MECHANICAL SETTINGS, DUCT SETTINGS AND PIPE SETTINGS,
// FOR THE WHOLE MODEL. `settings` is a table, one setting per entry, entries
// split by ";" or a new line, each "name=value":
//
//   duct connector separator=-; pipe connector separator=-
//   duct fitting annotation size=3; duct rise/drop annotation size=3
//   air density=1.2 kg/m3; air dynamic viscosity=1.81e-5 Pa-s
//   rectangular duct size separator=x; round duct size prefix=" dia "
//   duct flat on top=FOT; pipe set down from bottom=SDB; pipe size suffix=Ø
//   duct use annotation scale for single line fittings=yes
//
// THE NAMES ARE THE DIALOG'S, with "duct" or "pipe" in front where both pages
// have the setting. A name both pages have, given without it ("connector
// separator", "flat on top", "fitting annotation size"), is taken for the
// system `system` names - duct, pipe or both - and refused, naming both, when
// it names none. Never guessed (D-33).
//
// VALUES. Text is taken as written; put it in double quotes to keep a space
// at either end, and write "" for an empty one. A ";" cannot be set - it splits
// the table. Annotation sizes are MILLIMETRES ("3" or "3 mm"; any other unit is
// refused, never converted - D-33) and become feet by / 304.8 (D-20). Air
// density is kg/m3 and air dynamic viscosity Pa-s, converted by Revit's own
// UnitUtils - nothing assumed. Yes/no takes yes, no, on, off, true, false.
//
// NOT REACHED, REFUSED BY NAME: the rise/drop SYMBOLS - DuctSettings and
// PipeSettings have no member for them on any release 2020-2027
// (tools/api-surface --members, 2026-11-02); Revit holds them on each duct and
// pipe system type. Also the fitting angles, pipe slopes, conversion, sizing
// and calculation pages, which this card does not change.
//
// EVERYTHING IS PARSED BEFORE ANYTHING IS WRITTEN, so a bad entry changes
// nothing. A SETTING REVIT REFUSES is caught with Revit's own reason, every
// setting already written in this call is put back to the value read before,
// and the answer is `refused` with that reason - NOTHING WAS CHANGED. If a
// put-back itself fails, the whole call throws, so the caller's transaction
// rolls back (Golden Rule 16).
//
// READ BACK. `before` and `after` are every setting this card reaches, read off
// the model before and after; a value asked for that reads back different
// throws, so nothing is kept.

var findings = new List<string>();
string refused = null;
var before = "";
var after = "";
var changed = 0;
var culture = System.Globalization.CultureInfo.InvariantCulture;

// An optional need left out arrives as the text "none" (row 5b-1432).
var which = (system ?? "").Trim();
if (string.Equals(which, "none", StringComparison.OrdinalIgnoreCase)) which = "";

DuctSettings ds = null;
PipeSettings ps = null;
try { ds = DuctSettings.GetDuctSettings(doc); } catch (Exception) { ds = null; }
try { ps = PipeSettings.GetPipeSettings(doc); } catch (Exception) { ps = null; }
if (ds == null && ps == null)
    refused = doc.Title + " has no Duct Settings or Pipe Settings (Manage > MEP Settings > Mechanical Settings is a "
        + "project's), so NOTHING WAS CHANGED.";

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", culture) + " mm";
#if REVIT2020
Func<double, double> densityOut = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER);
Func<double, double> densityIn = v => UnitUtils.ConvertToInternalUnits(v, DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER);
Func<double, double> viscosityOut = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_PASCAL_SECONDS);
Func<double, double> viscosityIn = v => UnitUtils.ConvertToInternalUnits(v, DisplayUnitType.DUT_PASCAL_SECONDS);
#else
Func<double, double> densityOut = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.KilogramsPerCubicMeter);
Func<double, double> densityIn = v => UnitUtils.ConvertToInternalUnits(v, UnitTypeId.KilogramsPerCubicMeter);
Func<double, double> viscosityOut = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.PascalSeconds);
Func<double, double> viscosityIn = v => UnitUtils.ConvertToInternalUnits(v, UnitTypeId.PascalSeconds);
#endif

// How a value of each kind reads - the one form `before`, `after` and the
// "already so" comparison all use.
Func<string, object, string> show = (kind, v) =>
{
    if (kind == "mm") return mm((double)v);
    if (kind == "yes") return (bool)v ? "yes" : "no";
    if (kind == "density") return Math.Round(densityOut((double)v), 4).ToString("0.####", culture) + " kg/m3";
    if (kind == "viscosity") return viscosityOut((double)v).ToString("G6", culture) + " Pa-s";
    return "\"" + ((v as string) ?? "") + "\"";
};

// A name as letters and digits only, "centre" read as "center".
Func<string, string> key = text =>
{
    var t = (text ?? "").ToLowerInvariant().Replace("centre", "center");
    var words = System.Text.RegularExpressions.Regex.Split(t, "[^a-z0-9]+")
        .Where(w => w.Length > 0 && w != "the" && w != "settings" && w != "setting");
    return string.Join("", words);
};

// ---- the settings this card reaches -------------------------------------------
// label, system, the name shared with the other page (null when only one page
// has it), kind, other names, how to read it, how to write it.
var entries = new List<Tuple<string, string, string, string, string[], Func<object>, Action<object>>>();
Action<string, string, string, string, string[], Func<object>, Action<object>> add =
    (label, sys, shared, kind, aliases, get, set) => entries.Add(Tuple.Create(label, sys, shared, kind, aliases, get, set));
var none = new string[0];

if (ds != null)
{
    add("duct use annotation scale for single line fittings", "duct", "use annotation scale for single line fittings", "yes", none,
        () => ds.UseAnnotationScaleForSingleLineFittings, v => ds.UseAnnotationScaleForSingleLineFittings = (bool)v);
    add("duct fitting annotation size", "duct", "fitting annotation size", "mm", new[] { "duct fitting annotation" },
        () => ds.FittingAnnotationSize, v => ds.FittingAnnotationSize = (double)v);
    add("air density", "duct", null, "density", new[] { "duct air density" },
        () => ds.AirDensity, v => ds.AirDensity = (double)v);
#if REVIT2025_OR_GREATER
    add("air dynamic viscosity", "duct", null, "viscosity", new[] { "air viscosity", "duct air viscosity", "duct air dynamic viscosity" },
        () => ds.AirDynamicViscosity, v => ds.AirDynamicViscosity = (double)v);
#else
    add("air dynamic viscosity", "duct", null, "viscosity", new[] { "air viscosity", "duct air viscosity", "duct air dynamic viscosity" },
        () => ds.AirViscosity, v => ds.AirViscosity = (double)v);
#endif
    add("rectangular duct size separator", "duct", null, "text", new[] { "rectangular size separator" },
        () => ds.RectangularDuctSizeSeparator, v => ds.RectangularDuctSizeSeparator = (string)v);
    add("rectangular duct size suffix", "duct", null, "text", new[] { "rectangular size suffix" },
        () => ds.RectangularDuctSizeSuffix, v => ds.RectangularDuctSizeSuffix = (string)v);
    add("round duct size prefix", "duct", null, "text", new[] { "round size prefix" },
        () => ds.RoundDuctSizePrefix, v => ds.RoundDuctSizePrefix = (string)v);
    add("round duct size suffix", "duct", null, "text", new[] { "round size suffix" },
        () => ds.RoundDuctSizeSuffix, v => ds.RoundDuctSizeSuffix = (string)v);
    add("oval duct size separator", "duct", null, "text", new[] { "oval size separator" },
        () => ds.OvalDuctSizeSeparator, v => ds.OvalDuctSizeSeparator = (string)v);
    add("oval duct size suffix", "duct", null, "text", new[] { "oval size suffix" },
        () => ds.OvalDuctSizeSuffix, v => ds.OvalDuctSizeSuffix = (string)v);
    add("duct connector separator", "duct", "connector separator", "text", none,
        () => ds.ConnectorSeparator, v => ds.ConnectorSeparator = (string)v);
    add("duct flat on top", "duct", "flat on top", "text", none,
        () => ds.FlatOnTop, v => ds.FlatOnTop = (string)v);
    add("duct flat on bottom", "duct", "flat on bottom", "text", none,
        () => ds.FlatOnBottom, v => ds.FlatOnBottom = (string)v);
    add("duct set up from center", "duct", "set up from center", "text", new[] { "duct set up" },
        () => ds.SetUp, v => ds.SetUp = (string)v);
    add("duct set down from center", "duct", "set down from center", "text", new[] { "duct set down" },
        () => ds.SetDown, v => ds.SetDown = (string)v);
    add("duct set up from bottom", "duct", "set up from bottom", "text", none,
        () => ds.SetUpFromBottom, v => ds.SetUpFromBottom = (string)v);
    add("duct set down from bottom", "duct", "set down from bottom", "text", none,
        () => ds.SetDownFromBottom, v => ds.SetDownFromBottom = (string)v);
    add("duct centerline", "duct", "centerline", "text", none,
        () => ds.Centerline, v => ds.Centerline = (string)v);
    add("duct rise/drop annotation size", "duct", null, "mm", new[] { "rise/drop annotation size", "rise drop annotation size" },
        () => ds.RiseDropAnnotationSize, v => ds.RiseDropAnnotationSize = (double)v);
}
if (ps != null)
{
    add("pipe use annotation scale for single line fittings", "pipe", "use annotation scale for single line fittings", "yes", none,
        () => ps.UseAnnotationScaleForSingleLineFittings, v => ps.UseAnnotationScaleForSingleLineFittings = (bool)v);
    add("pipe fitting annotation size", "pipe", "fitting annotation size", "mm", new[] { "pipe fitting annotation" },
        () => ps.FittingAnnotationSize, v => ps.FittingAnnotationSize = (double)v);
    add("pipe size prefix", "pipe", null, "text", none,
        () => ps.SizePrefix, v => ps.SizePrefix = (string)v);
    add("pipe size suffix", "pipe", null, "text", none,
        () => ps.SizeSuffix, v => ps.SizeSuffix = (string)v);
    add("pipe connector separator", "pipe", "connector separator", "text", none,
        () => ps.ConnectorSeparator, v => ps.ConnectorSeparator = (string)v);
    add("pipe flat on top", "pipe", "flat on top", "text", none,
        () => ps.FlatOnTop, v => ps.FlatOnTop = (string)v);
    add("pipe flat on bottom", "pipe", "flat on bottom", "text", none,
        () => ps.FlatOnBottom, v => ps.FlatOnBottom = (string)v);
    add("pipe set up from center", "pipe", "set up from center", "text", new[] { "pipe set up" },
        () => ps.SetUp, v => ps.SetUp = (string)v);
    add("pipe set down from center", "pipe", "set down from center", "text", new[] { "pipe set down" },
        () => ps.SetDown, v => ps.SetDown = (string)v);
    add("pipe set up from bottom", "pipe", "set up from bottom", "text", none,
        () => ps.SetUpFromBottom, v => ps.SetUpFromBottom = (string)v);
    add("pipe set down from bottom", "pipe", "set down from bottom", "text", none,
        () => ps.SetDownFromBottom, v => ps.SetDownFromBottom = (string)v);
    add("pipe centerline", "pipe", "centerline", "text", none,
        () => ps.Centerline, v => ps.Centerline = (string)v);
}
var nameList = string.Join(", ", entries.Select(e => e.Item1));

Func<string> describe = () =>
{
    var pages = new List<string>();
    foreach (var page in new[] { "duct", "pipe" })
    {
        var these = entries.Where(e => e.Item2 == page).ToList();
        if (these.Count == 0) continue;
        pages.Add((page == "duct" ? "Duct Settings - " : "Pipe Settings - ")
            + string.Join("; ", these.Select(e => e.Item1 + " " + show(e.Item4, e.Item6()))));
    }
    return string.Join(" | ", pages);
};

// ---- the system a shared name is for ------------------------------------------
var problems = new List<string>();
var systems = new HashSet<string>();
var wk = key(which);
if (wk == "") { }
else if (wk == "duct" || wk == "ducts" || wk == "ductwork") systems.Add("duct");
else if (wk == "pipe" || wk == "pipes" || wk == "piping") systems.Add("pipe");
else if (wk == "both" || wk == "all" || wk == "ductandpipe" || wk == "pipeandduct" || wk == "ductsandpipes"
    || wk == "pipesandducts" || wk == "ductpipe" || wk == "pipeduct")
{ systems.Add("duct"); systems.Add("pipe"); }
else problems.Add("system \"" + which + "\" is not duct, pipe or both");

Func<string, bool?> parseYes = v =>
{
    var t = (v ?? "").Trim().ToLowerInvariant();
    if (t == "yes" || t == "on" || t == "true" || t == "1" || t == "ticked") return true;
    if (t == "no" || t == "off" || t == "false" || t == "0" || t == "unticked") return false;
    return null;
};
// A number with only the unit named after it (any of `units`), or none.
Func<string, string[], double?> parseNumber = (v, units) =>
{
    var t = (v ?? "").Trim().ToLowerInvariant().Replace(" ", "");
    foreach (var u in units.OrderByDescending(u => u.Length))
        if (t.EndsWith(u)) { t = t.Substring(0, t.Length - u.Length); break; }
    double n;
    if (!double.TryParse(t, System.Globalization.NumberStyles.Float, culture, out n)) return null;
    return n;
};

// One plan per setting: label, kind, the value wanted (internal), how to read
// it, how to write it, the value read before.
var plans = new List<Tuple<string, string, object, Func<object>, Action<object>, object>>();

if (refused == null)
{
    var entryTexts = (settings ?? "").Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
    if (entryTexts.Count == 0)
        refused = "No setting was named, so NOTHING WAS CHANGED. Name each as \"name=value\", \";\" between: " + nameList + ".";
    foreach (var entry in entryTexts)
    {
        var eq = entry.IndexOf('=');
        if (eq <= 0) { problems.Add("\"" + entry + "\" is not name=value"); continue; }
        var askedName = entry.Substring(0, eq).Trim();
        var value = entry.Substring(eq + 1).Trim();
        var k = key(askedName);
        if (k.Contains("risedrop") && (k.Contains("symbol") || k.Contains("style")))
        {
            problems.Add("\"" + askedName + "\": the rise/drop symbols cannot be set here - Duct Settings and Pipe Settings "
                + "have no member for them in the Revit API on any release 2020-2027; Revit holds them on each duct and "
                + "pipe system type (Type Properties > Rise / Drop). The size they are drawn at is \"duct rise/drop "
                + "annotation size\"");
            continue;
        }
        if (k.Contains("slope") || k.Contains("angle") || k.Contains("conversion") || k.Contains("sizing")
            || k.Contains("calculation") || k.Contains("tolerance") || k.Contains("segment"))
        {
            problems.Add("\"" + askedName + "\" is on a Mechanical Settings page this card does not change - change it in "
                + "Manage > MEP Settings > Mechanical Settings");
            continue;
        }
        var hits = entries.Where(e => key(e.Item1) == k || e.Item5.Any(a => key(a) == k)).ToList();
        if (hits.Count == 0) hits = entries.Where(e => e.Item3 != null && key(e.Item3) == k).ToList();
        if (hits.Count > 1)
        {
            if (systems.Count == 0)
            {
                problems.Add("\"" + askedName + "\" is on both Duct Settings and Pipe Settings - say \"duct " + hits[0].Item3
                    + "\" or \"pipe " + hits[0].Item3 + "\", or name the system as duct, pipe or both");
                continue;
            }
            hits = hits.Where(e => systems.Contains(e.Item2)).ToList();
        }
        if (hits.Count == 0)
        {
            problems.Add("\"" + askedName + "\" is not a setting this card reaches; it takes " + nameList);
            continue;
        }
        foreach (var hit in hits)
        {
            var label = hit.Item1;
            var kind = hit.Item4;
            object want = null;
            if (kind == "text")
            {
                if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\"")) want = value.Substring(1, value.Length - 2);
                else if (value.Length == 0) { problems.Add(label + " was given no value - write \"\" for an empty one"); continue; }
                else want = value;
            }
            else if (kind == "yes")
            {
                var b = parseYes(value);
                if (b == null) { problems.Add(label + " \"" + value + "\" is not yes or no"); continue; }
                want = b.Value;
            }
            else if (kind == "mm")
            {
                var n = parseNumber(value, new[] { "mm", "millimetres", "millimeters" });
                if (n == null) { problems.Add(label + " \"" + value + "\" is not a size in millimetres"); continue; }
                want = n.Value / 304.8;
            }
            else if (kind == "density")
            {
                var n = parseNumber(value, new[] { "kg/m3", "kg/m³", "kg/m^3", "kg/cu.m" });
                if (n == null || n.Value <= 0) { problems.Add(label + " \"" + value + "\" is not a density in kg/m3 above 0"); continue; }
                want = densityIn(n.Value);
            }
            else
            {
                var n = parseNumber(value, new[] { "pa-s", "pa·s", "pa.s", "pas", "pascalseconds", "pascal-seconds" });
                if (n == null || n.Value <= 0) { problems.Add(label + " \"" + value + "\" is not a viscosity in Pa-s above 0"); continue; }
                want = viscosityIn(n.Value);
            }
            plans.Add(Tuple.Create(label, kind, want, hit.Item6, hit.Item7, hit.Item6()));
        }
    }
    if (refused == null && problems.Count > 0)
        refused = string.Join("; ", problems) + ". NOTHING WAS CHANGED.";
    if (refused == null)
    {
        var labels = plans.Select(p => p.Item1).ToList();
        var twice = labels.Where(l => labels.Count(x => x == l) > 1).Distinct().ToList();
        if (twice.Count > 0)
            refused = string.Join(", ", twice) + " is named more than once, so NOTHING WAS CHANGED - name each setting once.";
    }
}

// ---- write, and on a refusal by Revit put back what this call wrote ------------
if (refused == null)
{
    before = describe();
    var written = new List<Tuple<string, string, object, Func<object>, Action<object>, object>>();
    var already = new List<string>();
    foreach (var p in plans)
    {
        var now = show(p.Item2, p.Item4());
        if (now == show(p.Item2, p.Item3)) { already.Add(p.Item1 + " " + now); continue; }
        try { p.Item5(p.Item3); written.Add(p); }
        catch (Exception ex)
        {
            foreach (var w in Enumerable.Reverse(written))
            {
                try { w.Item5(w.Item6); }
                catch (Exception back)
                {
                    throw new InvalidOperationException("Revit refused " + p.Item1 + " = " + show(p.Item2, p.Item3) + " ("
                        + ex.Message + ") and then would not put " + w.Item1 + " back (" + back.Message + "). The whole "
                        + "change is rolled back, so NOTHING WAS CHANGED.", ex);
                }
            }
            refused = "Revit refused " + p.Item1 + " = " + show(p.Item2, p.Item3) + ": " + ex.Message + " NOTHING WAS CHANGED - "
                + (written.Count == 0 ? "nothing had been written yet." : "the " + written.Count + " setting(s) already written were put back.");
            written.Clear();
            break;
        }
    }
    if (refused == null)
    {
        var wrong = written.Where(p => show(p.Item2, p.Item4()) != show(p.Item2, p.Item3))
            .Select(p => p.Item1 + " reads back " + show(p.Item2, p.Item4()) + ", not " + show(p.Item2, p.Item3)).ToList();
        if (wrong.Count > 0)
            throw new InvalidOperationException("Revit did not keep what was asked: " + string.Join("; ", wrong)
                + ". The whole change is rolled back, so NOTHING WAS CHANGED.");
        changed = written.Count;
        after = describe();
        if (already.Count > 0)
            findings.Add("Already so, not counted: " + string.Join("; ", already) + ".");
        if (changed > 0)
            findings.Add("Mechanical Settings changed for the whole model (" + changed + " setting(s)): "
                + string.Join("; ", written.Select(p => p.Item1 + " " + show(p.Item2, p.Item3))) + ". Every duct and pipe "
                + "tag, fitting annotation and pressure loss figure that uses these follows them. Read back: " + after + ".");
        else
            findings.Add("Every setting asked for is already so, so nothing was changed: " + before + ".");
    }
}
if (refused != null) findings.Add(refused);
