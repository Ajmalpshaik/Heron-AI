// NOT STANDALONE. Assumes `doc` and `settings` are in scope; leaves `before`,
// `after`, `changed`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// MANAGE > STRUCTURAL SETTINGS, SYMBOLIC REPRESENTATION AND BOUNDARY CONDITIONS,
// FOR THE WHOLE MODEL. `settings` is a table, one setting per entry, entries
// split by ";" or a new line, each "name=value":
//
//   brace cutback=2 mm; beam/truss cutback=50; column cutback=3
//   show brace above=yes; show brace below=no; show brace=above and below
//   parallel line offset=2; boundary condition symbol spacing=1200
//   brace above symbol=<a type the model has>; kicker brace symbol=...
//   fixed / pinned / roller / user defined boundary condition symbol=<a type>
//   loads display scaling=yes                          (Revit 2024 and later)
//
// THE NAMES ARE THE DIALOG'S, forgiven for capitals, spaces, "/", "the",
// "symbolic" and "distance" - "beam symbolic cutback distance" is "beam/truss
// cutback". A name not in the list is refused with the list. Distances are
// MILLIMETRES ("50" or "50 mm"; feet and inches are refused, never converted -
// D-33) and become feet by / 304.8 (D-20). Yes/no takes yes, no, on, off, true,
// false, show, hide.
//
// THE CONNECTION SYMBOLS ARE NOT REACHABLE. The dialog's Connection Symbols
// (beams and braces, column connection) have no member in the Revit API on any
// release 2020-2027 (tools/api-surface --members StructuralSettings, read
// 2026-11-02), so a name with "connection" in it is refused by name and sent to
// the dialog - never guessed onto another setting.
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

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mm";
Func<bool, string> yesNo = b => b ? "yes" : "no";
Func<ElementId, string> typeName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var t = doc.GetElement(id) as ElementType;
    if (t == null) return "none";
    return string.IsNullOrEmpty(t.FamilyName) ? "'" + t.Name + "'" : "'" + t.FamilyName + ": " + t.Name + "'";
};
// A name or alias, as letters and digits only, without the words the dialog
// and the modeller add or leave out freely.
var dropped = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "the", "symbolic", "distance", "and", "for", "of", "set" };
Func<string, string> key = text =>
{
    var words = System.Text.RegularExpressions.Regex.Split((text ?? "").ToLowerInvariant(), "[^a-z0-9]+")
        .Where(w => w.Length > 0 && !dropped.Contains(w));
    return string.Join("", words);
};

Func<StructuralSettings, string> describe = s =>
{
    var parts = new List<string>
    {
        "brace cutback " + mm(s.SymbolicCutbackForBrace),
        "beam/truss cutback " + mm(s.SymbolicCutbackForBeamAndTruss),
        "column cutback " + mm(s.SymbolicCutbackForColumn),
        "parallel line offset " + mm(s.BraceParallelLineOffset),
        "show brace above " + yesNo(s.ShowBraceAbove) + " (symbol " + typeName(s.BraceAboveSymbol) + ")",
        "show brace below " + yesNo(s.ShowBraceBelow) + " (symbol " + typeName(s.BraceBelowSymbol) + ")",
        "kicker brace symbol " + typeName(s.KickerBraceSymbol),
        "boundary condition symbol spacing " + mm(s.BoundaryConditionAreaAndLineSymbolSpacing),
        "fixed symbol " + typeName(s.BoundaryConditionFamilySymbolFixed),
        "pinned symbol " + typeName(s.BoundaryConditionFamilySymbolPinned),
        "roller symbol " + typeName(s.BoundaryConditionFamilySymbolRoller),
        "user defined symbol " + typeName(s.BoundaryConditionFamilySymbolUserDefined),
    };
#if REVIT2024_OR_GREATER
    parts.Add("loads display scaling " + yesNo(s.UseLoadsDisplayScaling));
#endif
    return string.Join("; ", parts);
};

StructuralSettings ss = null;
try { ss = StructuralSettings.GetStructuralSettings(doc); }
catch (Exception ex) { refused = "Revit gave no Structural Settings for " + doc.Title + " (" + ex.Message + "), so NOTHING WAS CHANGED."; }
if (refused == null && ss == null)
    refused = doc.Title + " has no Structural Settings (Manage > Structural Settings is a project's), so NOTHING WAS CHANGED.";

// ---- the names this card takes, each with its aliases --------------------------
// kind: "mm" a distance, "yes" a yes/no, "type" a symbol type by name, "show" the
// brace pair at once.
var names = new List<Tuple<string, string, string[]>>
{
    Tuple.Create("brace cutback", "mm", new[] { "brace cutback", "brace symbolic cutback distance", "symbolic cutback brace" }),
    Tuple.Create("beam/truss cutback", "mm", new[] { "beam/truss cutback", "beam cutback", "truss cutback", "beam symbolic cutback distance", "beam and truss symbolic cutback distance", "symbolic cutback beam" }),
    Tuple.Create("column cutback", "mm", new[] { "column cutback", "column symbolic cutback distance", "symbolic cutback column" }),
    Tuple.Create("parallel line offset", "mm", new[] { "parallel line offset", "brace parallel line offset", "brace plan offset", "brace line offset" }),
    Tuple.Create("show brace above", "yes", new[] { "show brace above", "brace above", "show brace in plan above" }),
    Tuple.Create("show brace below", "yes", new[] { "show brace below", "brace below", "show brace in plan below" }),
    Tuple.Create("show brace", "show", new[] { "show brace", "brace symbol", "brace symbols", "show brace symbol" }),
    Tuple.Create("brace above symbol", "type", new[] { "brace above symbol", "brace above type" }),
    Tuple.Create("brace below symbol", "type", new[] { "brace below symbol", "brace below type" }),
    Tuple.Create("kicker brace symbol", "type", new[] { "kicker brace symbol", "kicker symbol" }),
    Tuple.Create("boundary condition symbol spacing", "mm", new[] { "boundary condition symbol spacing", "area and line symbol spacing", "boundary condition spacing" }),
    Tuple.Create("fixed boundary condition symbol", "type", new[] { "fixed boundary condition symbol", "fixed symbol" }),
    Tuple.Create("pinned boundary condition symbol", "type", new[] { "pinned boundary condition symbol", "pinned symbol" }),
    Tuple.Create("roller boundary condition symbol", "type", new[] { "roller boundary condition symbol", "roller symbol" }),
    Tuple.Create("user defined boundary condition symbol", "type", new[] { "user defined boundary condition symbol", "user defined symbol" }),
#if REVIT2024_OR_GREATER
    Tuple.Create("loads display scaling", "yes", new[] { "loads display scaling", "use loads display scaling" }),
#endif
};
var nameList = string.Join(", ", names.Select(n => n.Item1));

// One plan per setting: its label, the value wanted as it reads back, how to
// read it now, how to write it and how to put back the value read before.
var plans = new List<Tuple<string, string, Func<StructuralSettings, string>, Action<StructuralSettings>, Action<StructuralSettings>>>();
var problems = new List<string>();

Func<string, bool?> parseYes = v =>
{
    var t = (v ?? "").Trim().ToLowerInvariant();
    if (t == "yes" || t == "on" || t == "true" || t == "show" || t == "1" || t == "ticked") return true;
    if (t == "no" || t == "off" || t == "false" || t == "hide" || t == "0" || t == "unticked") return false;
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
// A symbol type, matched among the types of the category the current symbol is
// in - the only evidence of which kind of type Revit takes there.
Func<string, ElementId, string, ElementId> findType = (label, current, asked) =>
{
    var currentType = current == null || current == ElementId.InvalidElementId ? null : doc.GetElement(current) as ElementType;
    if (currentType == null || currentType.Category == null)
    {
        problems.Add(label + " has no symbol set now, so the kind of type Revit takes there cannot be told - choose it in "
            + "Manage > Structural Settings");
        return null;
    }
    var catId = currentType.Category.Id;
    var types = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
        .Where(t => t.Category != null && t.Category.Id == catId).ToList();
    var a = (asked ?? "").Trim();
    var hits = types.Where(t => string.Equals(t.FamilyName + ": " + t.Name, a, StringComparison.OrdinalIgnoreCase)).ToList();
    if (hits.Count == 0) hits = types.Where(t => string.Equals(t.Name, a, StringComparison.OrdinalIgnoreCase)).ToList();
    if (hits.Count == 1) return hits[0].Id;
    var offered = string.Join(", ", types.Select(t => "'" + t.FamilyName + ": " + t.Name + "'").Distinct().OrderBy(n => n).Take(12));
    problems.Add(hits.Count == 0
        ? label + " \"" + a + "\" is not a type this model has for it; it has " + (offered.Length == 0 ? "none" : offered)
        : label + " \"" + a + "\" names " + hits.Count + " types in different families - name it as 'Family: Type': " + offered);
    return null;
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
        if (k.Contains("connection"))
        {
            problems.Add("\"" + askedName + "\": Revit's Connection Symbols (beams and braces, column connection) cannot be "
                + "set through the Revit API on any release 2020-2027 - change them in Manage > Structural Settings > "
                + "Symbolic Representation Settings. A column's own symbolic cutback is \"column cutback\"");
            continue;
        }
        var found = names.FirstOrDefault(n => n.Item3.Any(alias => key(alias) == k));
        if (found == null)
        {
#if !REVIT2024_OR_GREATER
            if (k.Contains("loadsdisplayscaling")) { problems.Add("\"" + askedName + "\" is Revit 2024 and later"); continue; }
#endif
            problems.Add("\"" + askedName + "\" is not a setting this card reaches; it takes " + nameList);
            continue;
        }
        var label = found.Item1;
        if (found.Item2 == "mm")
        {
            var n = parseMm(value);
            if (n == null) { problems.Add(label + " \"" + value + "\" is not a distance in millimetres"); continue; }
            var feet = n.Value / 304.8;
            Func<StructuralSettings, double> get = null;
            Action<StructuralSettings, double> set = null;
            if (label == "brace cutback") { get = s => s.SymbolicCutbackForBrace; set = (s, f) => s.SymbolicCutbackForBrace = f; }
            else if (label == "beam/truss cutback") { get = s => s.SymbolicCutbackForBeamAndTruss; set = (s, f) => s.SymbolicCutbackForBeamAndTruss = f; }
            else if (label == "column cutback") { get = s => s.SymbolicCutbackForColumn; set = (s, f) => s.SymbolicCutbackForColumn = f; }
            else if (label == "parallel line offset") { get = s => s.BraceParallelLineOffset; set = (s, f) => s.BraceParallelLineOffset = f; }
            else { get = s => s.BoundaryConditionAreaAndLineSymbolSpacing; set = (s, f) => s.BoundaryConditionAreaAndLineSymbolSpacing = f; }
            var old = get(ss);
            plans.Add(Tuple.Create<string, string, Func<StructuralSettings, string>, Action<StructuralSettings>, Action<StructuralSettings>>(
                label, mm(feet), s => mm(get(s)), s => set(s, feet), s => set(s, old)));
        }
        else if (found.Item2 == "yes" || found.Item2 == "show")
        {
            var pairs = new List<Tuple<string, bool>>();
            if (found.Item2 == "yes")
            {
                var b = parseYes(value);
                if (b == null) { problems.Add(label + " \"" + value + "\" is not yes or no"); continue; }
                pairs.Add(Tuple.Create(label, b.Value));
            }
            else
            {
                var v = key(value);
                if (v == "abovebelow" || v == "both" || v == "yes" || v == "showaboveshowbelow") { pairs.Add(Tuple.Create("show brace above", true)); pairs.Add(Tuple.Create("show brace below", true)); }
                else if (v == "above" || v == "aboveonly") { pairs.Add(Tuple.Create("show brace above", true)); pairs.Add(Tuple.Create("show brace below", false)); }
                else if (v == "below" || v == "belowonly") { pairs.Add(Tuple.Create("show brace above", false)); pairs.Add(Tuple.Create("show brace below", true)); }
                else if (v == "none" || v == "neither" || v == "no" || v == "hide") { pairs.Add(Tuple.Create("show brace above", false)); pairs.Add(Tuple.Create("show brace below", false)); }
                else { problems.Add("show brace \"" + value + "\" is not one of: above and below, above, below, none"); continue; }
            }
            foreach (var p in pairs)
            {
                var want = p.Item2;
                Func<StructuralSettings, bool> get = null;
                Action<StructuralSettings, bool> set = null;
                if (p.Item1 == "show brace above") { get = s => s.ShowBraceAbove; set = (s, x) => s.ShowBraceAbove = x; }
                else if (p.Item1 == "show brace below") { get = s => s.ShowBraceBelow; set = (s, x) => s.ShowBraceBelow = x; }
#if REVIT2024_OR_GREATER
                else { get = s => s.UseLoadsDisplayScaling; set = (s, x) => s.UseLoadsDisplayScaling = x; }
#endif
                if (get == null) continue;
                var old = get(ss);
                plans.Add(Tuple.Create<string, string, Func<StructuralSettings, string>, Action<StructuralSettings>, Action<StructuralSettings>>(
                    p.Item1, yesNo(want), s => yesNo(get(s)), s => set(s, want), s => set(s, old)));
            }
        }
        else
        {
            Func<StructuralSettings, ElementId> get = null;
            Action<StructuralSettings, ElementId> set = null;
            if (label == "brace above symbol") { get = s => s.BraceAboveSymbol; set = (s, x) => s.BraceAboveSymbol = x; }
            else if (label == "brace below symbol") { get = s => s.BraceBelowSymbol; set = (s, x) => s.BraceBelowSymbol = x; }
            else if (label == "kicker brace symbol") { get = s => s.KickerBraceSymbol; set = (s, x) => s.KickerBraceSymbol = x; }
            else if (label == "fixed boundary condition symbol") { get = s => s.BoundaryConditionFamilySymbolFixed; set = (s, x) => s.BoundaryConditionFamilySymbolFixed = x; }
            else if (label == "pinned boundary condition symbol") { get = s => s.BoundaryConditionFamilySymbolPinned; set = (s, x) => s.BoundaryConditionFamilySymbolPinned = x; }
            else if (label == "roller boundary condition symbol") { get = s => s.BoundaryConditionFamilySymbolRoller; set = (s, x) => s.BoundaryConditionFamilySymbolRoller = x; }
            else { get = s => s.BoundaryConditionFamilySymbolUserDefined; set = (s, x) => s.BoundaryConditionFamilySymbolUserDefined = x; }
            var old = get(ss);
            var id = findType(label, old, value);
            if (id == null) continue;
            plans.Add(Tuple.Create<string, string, Func<StructuralSettings, string>, Action<StructuralSettings>, Action<StructuralSettings>>(
                label, typeName(id), s => typeName(get(s)), s => set(s, id), s => set(s, old)));
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
    before = describe(ss);
    var written = new List<Tuple<string, string, Func<StructuralSettings, string>, Action<StructuralSettings>, Action<StructuralSettings>>>();
    var already = new List<string>();
    foreach (var p in plans)
    {
        var now = p.Item3(ss);
        if (now == p.Item2) { already.Add(p.Item1 + " " + now); continue; }
        try { p.Item4(ss); written.Add(p); }
        catch (Exception ex)
        {
            foreach (var w in Enumerable.Reverse(written))
            {
                try { w.Item5(ss); }
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
        var fresh = StructuralSettings.GetStructuralSettings(doc);
        var wrong = written.Where(p => p.Item3(fresh) != p.Item2).Select(p => p.Item1 + " reads back " + p.Item3(fresh) + ", not " + p.Item2).ToList();
        if (wrong.Count > 0)
            throw new InvalidOperationException("Revit did not keep what was asked: " + string.Join("; ", wrong)
                + ". The whole change is rolled back, so NOTHING WAS CHANGED.");
        changed = written.Count;
        after = describe(fresh);
        if (already.Count > 0)
            findings.Add("Already so, not counted: " + string.Join("; ", already) + ".");
        if (changed > 0)
            findings.Add("Structural Settings changed for the whole model (" + changed + " setting(s)): "
                + string.Join("; ", written.Select(p => p.Item1 + " " + p.Item2)) + ". Every plan view draws its beams, "
                + "braces, columns and boundary conditions with these. Read back: " + after + ".");
        else
            findings.Add("Every setting asked for is already so, so nothing was changed: " + before + ".");
    }
}
if (refused != null) findings.Add(refused);
