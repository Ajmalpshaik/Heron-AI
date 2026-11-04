// NOT STANDALONE. Assumes `doc` and `settings` are in scope; leaves `before`,
// `after`, `changed`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// STRUCTURE > REINFORCEMENT SETTINGS, FOR THE WHOLE MODEL. `settings` is a table,
// one setting per entry, entries split by ";" or a new line, each "name=value":
//
//   include hooks in rebar shape definition=yes
//   include end treatments in rebar shape definition=no
//   host structural rebar=yes          (within area and path reinforcement)
//   rebar presentation in view=first and last; rebar presentation in section=all
//   rebar total length rounding=5; rebar segment length rounding=5
//   rebar rounding=5                   (both rebar roundings at once)
//   rebar rounding method=up           (nearest, up or down; both rebar ones)
//   fabric total length rounding / fabric segment length rounding / fabric rounding
//   ... and their "method" forms, the same way
//   varying length number suffix=-
//
// THE NAMES ARE THE DIALOG'S, forgiven for capitals, spaces and the small words.
// "rebar shape defines hooks" is "include hooks in rebar shape definition". A
// name not in the list is refused with the list. Roundings are MILLIMETRES
// ("5" or "5 mm"; feet and inches refused, never converted - D-33). Yes/no takes
// yes, no, on, off, true, false.
//
// ROUNDING UNITS - NOT KNOWN FROM THE ASSEMBLIES, AND SO CONVERTED, NOT ASSUMED.
// Revit's rounding manager names the length unit its rounding values are held
// in (GetLengthUnitTypeId, 2021 and later; LengthDisplayUnit on 2020). The
// millimetres asked are converted to that unit, and read back from it, so the
// answer is right whichever unit Revit holds them in (NEEDS-CHECKING Group MF
// checks what it reports).
//
// SETTINGS REVIT ONLY LETS CHANGE BEFORE THERE IS REBAR. Revit refuses
// "include hooks in rebar shape definition" and "include end treatments ..."
// once the model holds rebar or rebar shapes of the other kind - its own words
// come back as the reason (below), never a sentence of Heron's.
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

Func<bool, string> yesNo = b => b ? "yes" : "no";
var dropped = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "the", "and", "for", "of", "set", "in", "to", "within", "a" };
Func<string, string> key = text =>
{
    var words = System.Text.RegularExpressions.Regex.Split((text ?? "").ToLowerInvariant(), "[^a-z0-9]+")
        .Where(w => w.Length > 0 && !dropped.Contains(w));
    return string.Join("", words);
};
Func<RebarPresentationMode, string> modeText = m =>
    m == RebarPresentationMode.All ? "show all" : m == RebarPresentationMode.FirstLast ? "show first and last"
    : m == RebarPresentationMode.Middle ? "show middle" : "show selected";
Func<RoundingMethod, string> methodText = m => m == RoundingMethod.Up ? "up" : m == RoundingMethod.Down ? "down" : "nearest";

// THE ROUNDING MANAGER'S OWN UNIT: millimetres into it, and back out of it.
Func<ReinforcementRoundingManager, double, double> fromMm = (manager, n) =>
{
#if REVIT2020
    return UnitUtils.Convert(n, DisplayUnitType.DUT_MILLIMETERS, manager.LengthDisplayUnit);
#else
    return UnitUtils.Convert(n, UnitTypeId.Millimeters, manager.GetLengthUnitTypeId());
#endif
};
Func<ReinforcementRoundingManager, double, string> roundingMm = (manager, value) =>
{
#if REVIT2020
    var n = UnitUtils.Convert(value, manager.LengthDisplayUnit, DisplayUnitType.DUT_MILLIMETERS);
#else
    var n = UnitUtils.Convert(value, manager.GetLengthUnitTypeId(), UnitTypeId.Millimeters);
#endif
    return Math.Round(n, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mm";
};

Func<ReinforcementSettings, string> describe = s =>
{
    var parts = new List<string>
    {
        "include hooks in rebar shape definition " + yesNo(s.RebarShapeDefinesHooks),
        "include end treatments in rebar shape definition " + yesNo(s.RebarShapeDefinesEndTreatments),
        "host structural rebar " + yesNo(s.HostStructuralRebar),
        "rebar presentation in view " + modeText(s.RebarPresentationInView),
        "rebar presentation in section " + modeText(s.RebarPresentationInSection),
        "varying length number suffix '" + s.RebarVaryingLengthNumberSuffix + "'",
    };
    var r = s.GetRebarRoundingManager();
    if (r != null)
        parts.Add("rebar total length rounding " + roundingMm(r, r.TotalLengthRounding) + " " + methodText(r.TotalLengthRoundingMethod)
            + ", segment length rounding " + roundingMm(r, r.SegmentLengthRounding) + " " + methodText(r.SegmentLengthRoundingMethod));
    var f = s.GetFabricRoundingManager();
    if (f != null)
        parts.Add("fabric total length rounding " + roundingMm(f, f.TotalLengthRounding) + " " + methodText(f.TotalLengthRoundingMethod)
            + ", segment length rounding " + roundingMm(f, f.SegmentLengthRounding) + " " + methodText(f.SegmentLengthRoundingMethod));
    return string.Join("; ", parts);
};

ReinforcementSettings rs = null;
try { rs = ReinforcementSettings.GetReinforcementSettings(doc); }
catch (Exception ex) { refused = "Revit gave no Reinforcement Settings for " + doc.Title + " (" + ex.Message + "), so NOTHING WAS CHANGED."; }
if (refused == null && rs == null)
    refused = doc.Title + " has no Reinforcement Settings, so NOTHING WAS CHANGED.";

// kind: "yes" a yes/no, "mode" a presentation, "round" a rounding in mm,
// "method" a rounding method, "text" the suffix.
var names = new List<Tuple<string, string, string[]>>
{
    Tuple.Create("include hooks in rebar shape definition", "yes", new[] { "include hooks in rebar shape definition", "rebar shape defines hooks", "shape defines hooks", "hooks in rebar shape" }),
    Tuple.Create("include end treatments in rebar shape definition", "yes", new[] { "include end treatments in rebar shape definition", "rebar shape defines end treatments", "shape defines end treatments", "end treatments in rebar shape" }),
    Tuple.Create("host structural rebar", "yes", new[] { "host structural rebar", "host structural rebar within area and path reinforcement", "host rebar in area and path reinforcement" }),
    Tuple.Create("rebar presentation in view", "mode", new[] { "rebar presentation in view", "rebar presentation in views", "presentation in view", "default rebar presentation in view" }),
    Tuple.Create("rebar presentation in section", "mode", new[] { "rebar presentation in section", "rebar presentation in sections", "presentation in section", "default rebar presentation in section" }),
    Tuple.Create("varying length number suffix", "text", new[] { "varying length number suffix", "rebar varying length number suffix", "varying length suffix" }),
    Tuple.Create("rebar total length rounding", "round", new[] { "rebar total length rounding", "rebar total bar length rounding", "total bar length rounding" }),
    Tuple.Create("rebar segment length rounding", "round", new[] { "rebar segment length rounding", "segment length rounding" }),
    Tuple.Create("rebar rounding", "round", new[] { "rebar rounding", "rebar length rounding" }),
    Tuple.Create("rebar total length rounding method", "method", new[] { "rebar total length rounding method", "total bar length rounding method" }),
    Tuple.Create("rebar segment length rounding method", "method", new[] { "rebar segment length rounding method", "segment length rounding method" }),
    Tuple.Create("rebar rounding method", "method", new[] { "rebar rounding method" }),
    Tuple.Create("fabric total length rounding", "round", new[] { "fabric total length rounding" }),
    Tuple.Create("fabric segment length rounding", "round", new[] { "fabric segment length rounding" }),
    Tuple.Create("fabric rounding", "round", new[] { "fabric rounding", "fabric length rounding" }),
    Tuple.Create("fabric total length rounding method", "method", new[] { "fabric total length rounding method" }),
    Tuple.Create("fabric segment length rounding method", "method", new[] { "fabric segment length rounding method" }),
    Tuple.Create("fabric rounding method", "method", new[] { "fabric rounding method" }),
};
var nameList = string.Join(", ", names.Select(n => n.Item1));

var plans = new List<Tuple<string, string, Func<ReinforcementSettings, string>, Action<ReinforcementSettings>, Action<ReinforcementSettings>>>();
var problems = new List<string>();
Action<string, string, Func<ReinforcementSettings, string>, Action<ReinforcementSettings>, Action<ReinforcementSettings>> plan =
    (label, want, read, write, putBack) =>
        plans.Add(Tuple.Create(label, want, read, write, putBack));

Func<string, bool?> parseYes = v =>
{
    var t = (v ?? "").Trim().ToLowerInvariant();
    if (t == "yes" || t == "on" || t == "true" || t == "1" || t == "ticked" || t == "include") return true;
    if (t == "no" || t == "off" || t == "false" || t == "0" || t == "unticked" || t == "exclude") return false;
    return null;
};
Func<string, double?> parseMm = v =>
{
    var t = (v ?? "").Trim().ToLowerInvariant();
    if (t.EndsWith("millimetres")) t = t.Substring(0, t.Length - 11);
    else if (t.EndsWith("millimeters")) t = t.Substring(0, t.Length - 11);
    else if (t.EndsWith("mm")) t = t.Substring(0, t.Length - 2);
    double n;
    if (!double.TryParse(t.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out n)) return null;
    return n;
};

if (refused == null)
{
    var entries = (settings ?? "").Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
    if (entries.Count == 0)
        refused = "No setting was named, so NOTHING WAS CHANGED. Name each as \"name=value\", \";\" between: " + nameList + ".";
    foreach (var entry in entries)
    {
        var eq = entry.IndexOf('=');
        if (eq <= 0) { problems.Add("\"" + entry + "\" is not name=value"); continue; }
        var askedName = entry.Substring(0, eq).Trim();
        var value = entry.Substring(eq + 1).Trim();
        var k = key(askedName);
        var found = names.FirstOrDefault(n => n.Item3.Any(alias => key(alias) == k));
        if (found == null) { problems.Add("\"" + askedName + "\" is not a setting this card reaches; it takes " + nameList); continue; }
        var label = found.Item1;
        var kind = found.Item2;
        if (kind == "yes")
        {
            var b = parseYes(value);
            if (b == null) { problems.Add(label + " \"" + value + "\" is not yes or no"); continue; }
            var want = b.Value;
            if (label.StartsWith("include hooks"))
            { var old = rs.RebarShapeDefinesHooks; plan(label, yesNo(want), s => yesNo(s.RebarShapeDefinesHooks), s => s.RebarShapeDefinesHooks = want, s => s.RebarShapeDefinesHooks = old); }
            else if (label.StartsWith("include end"))
            { var old = rs.RebarShapeDefinesEndTreatments; plan(label, yesNo(want), s => yesNo(s.RebarShapeDefinesEndTreatments), s => s.RebarShapeDefinesEndTreatments = want, s => s.RebarShapeDefinesEndTreatments = old); }
            else
            { var old = rs.HostStructuralRebar; plan(label, yesNo(want), s => yesNo(s.HostStructuralRebar), s => s.HostStructuralRebar = want, s => s.HostStructuralRebar = old); }
        }
        else if (kind == "mode")
        {
            var v = key(value).Replace("show", "");
            RebarPresentationMode? m = null;
            if (v == "all") m = RebarPresentationMode.All;
            else if (v == "firstlast") m = RebarPresentationMode.FirstLast;
            else if (v == "middle") m = RebarPresentationMode.Middle;
            if (m == null) { problems.Add(label + " \"" + value + "\" is not one of: show all, show first and last, show middle"); continue; }
            var want = m.Value;
            if (label.EndsWith("view"))
            { var old = rs.RebarPresentationInView; plan(label, modeText(want), s => modeText(s.RebarPresentationInView), s => s.RebarPresentationInView = want, s => s.RebarPresentationInView = old); }
            else
            { var old = rs.RebarPresentationInSection; plan(label, modeText(want), s => modeText(s.RebarPresentationInSection), s => s.RebarPresentationInSection = want, s => s.RebarPresentationInSection = old); }
        }
        else if (kind == "text")
        {
            var want = value;
            var old = rs.RebarVaryingLengthNumberSuffix;
            plan(label, "'" + want + "'", s => "'" + s.RebarVaryingLengthNumberSuffix + "'", s => s.RebarVaryingLengthNumberSuffix = want, s => s.RebarVaryingLengthNumberSuffix = old);
        }
        else
        {
            var fabric = label.StartsWith("fabric");
            var both = !label.Contains("total") && !label.Contains("segment");
            var which = new List<bool>();             // true = total length, false = segment length
            if (both || label.Contains("total")) which.Add(true);
            if (both || label.Contains("segment")) which.Add(false);
            var group = fabric ? "fabric" : "rebar";
            if (kind == "round")
            {
                var n = parseMm(value);
                if (n == null || n.Value < 0) { problems.Add(label + " \"" + value + "\" is not a rounding in millimetres"); continue; }
                var asked = n.Value;
                foreach (var total in which)
                {
                    var isTotal = total;
                    var name = group + (isTotal ? " total length rounding" : " segment length rounding");
                    Func<ReinforcementSettings, ReinforcementRoundingManager> mgr = s => fabric ? (ReinforcementRoundingManager)s.GetFabricRoundingManager() : s.GetRebarRoundingManager();
                    Func<ReinforcementSettings, double> get = s => fabric
                        ? (isTotal ? s.GetFabricRoundingManager().TotalLengthRounding : s.GetFabricRoundingManager().SegmentLengthRounding)
                        : (isTotal ? s.GetRebarRoundingManager().TotalLengthRounding : s.GetRebarRoundingManager().SegmentLengthRounding);
                    Action<ReinforcementSettings, double> set = (s, x) =>
                    {
                        if (fabric) { var fm = s.GetFabricRoundingManager(); if (isTotal) fm.TotalLengthRounding = x; else fm.SegmentLengthRounding = x; }
                        else { var rm = s.GetRebarRoundingManager(); if (isTotal) rm.TotalLengthRounding = x; else rm.SegmentLengthRounding = x; }
                    };
                    var manager = mgr(rs);
                    if (manager == null) { problems.Add("Revit gave no " + group + " rounding for this model"); continue; }
                    var want = fromMm(manager, asked);
                    var old = get(rs);
                    plan(name, roundingMm(manager, want), s => roundingMm(mgr(s), get(s)), s => set(s, want), s => set(s, old));
                }
            }
            else
            {
                var v = key(value);
                RoundingMethod? m = v == "nearest" ? RoundingMethod.Nearest : v == "up" ? RoundingMethod.Up : v == "down" ? RoundingMethod.Down : (RoundingMethod?)null;
                if (m == null) { problems.Add(label + " \"" + value + "\" is not nearest, up or down"); continue; }
                var want = m.Value;
                foreach (var total in which)
                {
                    var isTotal = total;
                    var name = group + (isTotal ? " total length rounding method" : " segment length rounding method");
                    Func<ReinforcementSettings, RoundingMethod> get = s => fabric
                        ? (isTotal ? s.GetFabricRoundingManager().TotalLengthRoundingMethod : s.GetFabricRoundingManager().SegmentLengthRoundingMethod)
                        : (isTotal ? s.GetRebarRoundingManager().TotalLengthRoundingMethod : s.GetRebarRoundingManager().SegmentLengthRoundingMethod);
                    Action<ReinforcementSettings, RoundingMethod> set = (s, x) =>
                    {
                        if (fabric) { var fm = s.GetFabricRoundingManager(); if (isTotal) fm.TotalLengthRoundingMethod = x; else fm.SegmentLengthRoundingMethod = x; }
                        else { var rm = s.GetRebarRoundingManager(); if (isTotal) rm.TotalLengthRoundingMethod = x; else rm.SegmentLengthRoundingMethod = x; }
                    };
                    RoundingMethod old;
                    try { old = get(rs); }
                    catch (Exception ex) { problems.Add("Revit gave no " + group + " rounding for this model (" + ex.Message + ")"); continue; }
                    plan(name, methodText(want), s => methodText(get(s)), s => set(s, want), s => set(s, old));
                }
            }
        }
    }
    if (refused == null && problems.Count > 0)
        refused = string.Join("; ", problems) + ". NOTHING WAS CHANGED.";
    if (refused == null)
    {
        var labels = plans.Select(p => p.Item1).ToList();
        var twice = labels.Where(l => labels.Count(x => x == l) > 1).Distinct().ToList();
        if (twice.Count > 0)
            refused = string.Join(", ", twice) + " is named more than once (\"rebar rounding\" is both rebar roundings), so "
                + "NOTHING WAS CHANGED - name each setting once.";
    }
}

// ---- write, and on a refusal by Revit put back what this call wrote ------------
if (refused == null)
{
    before = describe(rs);
    var written = new List<Tuple<string, string, Func<ReinforcementSettings, string>, Action<ReinforcementSettings>, Action<ReinforcementSettings>>>();
    var already = new List<string>();
    foreach (var p in plans)
    {
        var now = p.Item3(rs);
        if (now == p.Item2) { already.Add(p.Item1 + " " + now); continue; }
        try { p.Item4(rs); written.Add(p); }
        catch (Exception ex)
        {
            foreach (var w in Enumerable.Reverse(written))
            {
                try { w.Item5(rs); }
                catch (Exception back)
                {
                    throw new InvalidOperationException("Revit refused " + p.Item1 + " = " + p.Item2 + " (" + ex.Message
                        + ") and then would not put " + w.Item1 + " back (" + back.Message + "). The whole change is "
                        + "rolled back, so NOTHING WAS CHANGED.", ex);
                }
            }
            refused = "Revit refused " + p.Item1 + " = " + p.Item2 + ": " + ex.Message + " NOTHING WAS CHANGED - "
                + (written.Count == 0 ? "nothing had been written yet." : "the " + written.Count + " setting(s) already written were put back.");
            written.Clear();
            break;
        }
    }
    if (refused == null)
    {
        var fresh = ReinforcementSettings.GetReinforcementSettings(doc);
        var wrong = written.Where(p => p.Item3(fresh) != p.Item2).Select(p => p.Item1 + " reads back " + p.Item3(fresh) + ", not " + p.Item2).ToList();
        if (wrong.Count > 0)
            throw new InvalidOperationException("Revit did not keep what was asked: " + string.Join("; ", wrong)
                + ". The whole change is rolled back, so NOTHING WAS CHANGED.");
        changed = written.Count;
        after = describe(fresh);
        if (already.Count > 0)
            findings.Add("Already so, not counted: " + string.Join("; ", already) + ".");
        if (changed > 0)
            findings.Add("Reinforcement Settings changed for the whole model (" + changed + " setting(s)): "
                + string.Join("; ", written.Select(p => p.Item1 + " " + p.Item2)) + ". Read back: " + after + ".");
        else
            findings.Add("Every setting asked for is already so, so nothing was changed: " + before + ".");
    }
}
if (refused != null) findings.Add(refused);
