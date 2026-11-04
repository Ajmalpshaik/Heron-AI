// NOT STANDALONE. Assumes `doc`, `add`, `name`, `demandFactor`, `rule`,
// `percent` and `steps` are in scope, and leaves `added`, `before`, `after`,
// `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Manage > MEP Settings > Load Classifications, or Demand
// Factors: adds ONE load classification - optionally using a demand factor the
// model already has - or ONE demand factor, constant or by a table over
// quantity or kVA. Nothing existing is changed, renamed or removed: a name
// already present, in any capitals, is SKIPPED and named. The list it adds to
// is read before and after, and both are handed back.
//
// NEVER GUESSED (D-33). A demand factor given steps but no rule is refused:
// "80% over 10 kVA" is a table of the whole load (load) or of each portion
// (loadPortion), and which one is the modeller's. A demand factor named for a
// load classification must be one the model has, matched exactly; one that
// differs only in capitals is named, never taken.
//
// UNITS. Steps over load are in kVA. Revit keeps a load in internal units,
// one volt-ampere being 1 / (0.3048 * 0.3048) of them - the factor
// SET_FAMILY_CONNECTOR_ROLES writes connector loads with - checked against
// Revit's own UnitUtils on the release that runs (DisplayUnitType in 2020,
// UnitTypeId from 2021); if they disagree nothing is written. A percentage is
// written as its fraction - 80% as 0.8 - and read back the same way.
//
// RELEASES. ElectricalLoadClassification.Create(Document, string),
// DemandFactorId; ElectricalDemandFactorDefinition.Create(Document, string),
// RuleType, SetValues, GetValues; ElectricalDemandFactorValue(double, double,
// double) and SetMaxRangeToUnlimited; ElectricalDemandFactorRule Constant,
// QuantityTable, LoadTable, QuantityTablePerPortion, LoadTablePerPortion -
// present with the same signatures 2020 to 2027 (tools/api-surface --members,
// 2026-11-01).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var added = 0;
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> num = v => Math.Round(v, 2).ToString("0.##", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";

var what = (add ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "");
var isLoad = what == "loadclassification" || what == "loadclass" || what == "load" || what == "classification";
var isFactor = what == "demandfactor" || what == "demand" || what == "factor";
var wantName = (name ?? "").Trim();

// ---- The unit, checked against Revit's own ---------------------------------
var perVoltAmpere = 1.0 / (0.3048 * 0.3048);
double revitsPerVoltAmpere = perVoltAmpere;
try
{
#if REVIT2020
    revitsPerVoltAmpere = UnitUtils.ConvertToInternalUnits(1.0, DisplayUnitType.DUT_VOLT_AMPERES);
#else
    revitsPerVoltAmpere = UnitUtils.ConvertToInternalUnits(1.0, UnitTypeId.VoltAmperes);
#endif
}
catch (Exception) { }

var factors = new List<ElectricalDemandFactorDefinition>();
var classes = new List<ElectricalLoadClassification>();
Action readLists = () =>
{
    factors.Clear();
    classes.Clear();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ElectricalDemandFactorDefinition)))
        if (e is ElectricalDemandFactorDefinition f) factors.Add(f);
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ElectricalLoadClassification)))
        if (e is ElectricalLoadClassification c) classes.Add(c);
};

Func<ElectricalDemandFactorRule, string> ruleWords = r =>
    r == ElectricalDemandFactorRule.Constant ? "constant"
    : r == ElectricalDemandFactorRule.QuantityTable ? "by quantity"
    : r == ElectricalDemandFactorRule.QuantityTablePerPortion ? "by quantity, for each portion"
    : r == ElectricalDemandFactorRule.LoadTable ? "by load" : "by load, for each portion";
Func<ElectricalDemandFactorDefinition, string> factorLine = f =>
{
    var rule = f.RuleType;
    var byLoad = rule == ElectricalDemandFactorRule.LoadTable || rule == ElectricalDemandFactorRule.LoadTablePerPortion;
    var values = new List<ElectricalDemandFactorValue>(f.GetValues() ?? new List<ElectricalDemandFactorValue>());
    values.Sort((a, b) => a.MinRange.CompareTo(b.MinRange));
    var parts = new List<string>();
    foreach (var v in values)
    {
        var pct = num(v.Factor * 100) + "%";
        if (rule == ElectricalDemandFactorRule.Constant) { parts.Add(pct); continue; }
        Func<double, string> amount = x => byLoad ? num(x / perVoltAmpere / 1000.0) + " kVA" : num(x);
        var open = v.MaxRange >= 1e12 || double.IsInfinity(v.MaxRange);
        parts.Add(amount(v.MinRange) + (open ? " and over" : " to " + amount(v.MaxRange)) + ": " + pct);
    }
    return quoted(f.Name) + " " + ruleWords(rule) + " - " + (parts.Count == 0 ? "no values" : string.Join(", ", parts));
};
Func<ElectricalLoadClassification, string> classLine = c =>
{
    var f = factors.FirstOrDefault(x => c.DemandFactorId != null && x.Id == c.DemandFactorId);
    return quoted(c.Name) + ", demand factor " + (f == null ? "none" : quoted(f.Name));
};
Func<List<string>> listNow = () =>
{
    var rows = isLoad ? classes.Select(classLine).ToList() : factors.Select(factorLine).ToList();
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    return rows;
};
Func<IEnumerable<string>, string, string> nameTaken = (existing, wanted) =>
{
    foreach (var n in existing)
        if (string.Equals(n, wanted, StringComparison.OrdinalIgnoreCase)) return n;
    return null;
};

// ---- Refusals before anything is touched -----------------------------------
if (!isLoad && !isFactor)
    refused.Add("\"" + (add ?? "") + "\" is not something this adds - loadClassification or demandFactor. Nothing was guessed.");
if (wantName.Length == 0)
    refused.Add("No name was given for the new " + (isFactor ? "demand factor" : "load classification") + " - it is never made up.");
if (Math.Abs(revitsPerVoltAmpere - perVoltAmpere) > 1e-9 * perVoltAmpere)
    refused.Add("Revit makes one volt-ampere " + revitsPerVoltAmpere.ToString(invariant) + " internal units, and Heron would have written "
        + perVoltAmpere.ToString(invariant) + " - so nothing is written until that is looked at.");

if (refused.Count == 0) { readLists(); before.AddRange(listNow()); }

// ---- A load classification -------------------------------------------------
if (refused.Count == 0 && isLoad)
{
    ElectricalDemandFactorDefinition useFactor = null;
    var factorWanted = (demandFactor ?? "").Trim();
    if (factorWanted.Length > 0 && !string.Equals(factorWanted, "none", StringComparison.OrdinalIgnoreCase))
    {
        useFactor = factors.FirstOrDefault(f => string.Equals(f.Name, factorWanted, StringComparison.Ordinal));
        if (useFactor == null)
        {
            var near = factors.Where(f => string.Equals(f.Name, factorWanted, StringComparison.OrdinalIgnoreCase)).ToList();
            refused.Add("No demand factor is named " + quoted(factorWanted) + ". "
                + (near.Count > 0 ? quoted(near[0].Name) + " differs only in capitals - Heron does not take one for the other. " : "")
                + "The model's demand factors are: " + (factors.Count == 0 ? "none" : string.Join(", ", factors.Select(f => quoted(f.Name)))) + ".");
        }
    }
    var taken = nameTaken(classes.Select(c => c.Name), wantName);
    if (refused.Count == 0 && taken != null)
        skipped.Add("A load classification is already named " + quoted(taken) + " - left as it is, nothing added.");
    else if (refused.Count == 0)
    {
        try
        {
            var made = ElectricalLoadClassification.Create(doc, wantName);
            if (made == null) refused.Add("Revit made no load classification named " + quoted(wantName) + ".");
            else
            {
                if (useFactor != null) made.DemandFactorId = useFactor.Id;
                added++;
                readLists();
                findings.Add("Added load classification " + classLine(made) + "."
                    + (useFactor == null ? " Its demand factor is Revit's own default - change it in Load Classifications, or name one." : ""));
            }
        }
        catch (Exception ex) { refused.Add("Revit would not add load classification " + quoted(wantName) + ": " + ex.Message); }
    }
}

// ---- A demand factor -------------------------------------------------------
if (refused.Count == 0 && isFactor)
{
    var ruleWord = (rule ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    var stepText = (steps ?? "").Trim();
    var hasSteps = stepText.Length > 0 && !string.Equals(stepText, "none", StringComparison.OrdinalIgnoreCase);
    ElectricalDemandFactorRule chosen = ElectricalDemandFactorRule.Constant;
    if (ruleWord.Length == 0 || ruleWord == "none")
    {
        if (hasSteps)
            refused.Add("Steps were given but no rule - by load (the whole load takes the step it falls in), loadPortion (each "
                + "portion of the load takes its own step), quantity or quantityPortion. Which one is never guessed.");
    }
    else if (ruleWord == "constant") chosen = ElectricalDemandFactorRule.Constant;
    else if (ruleWord == "load" || ruleWord == "loadtable") chosen = ElectricalDemandFactorRule.LoadTable;
    else if (ruleWord == "loadportion" || ruleWord == "loadtableperportion" || ruleWord == "loadperportion") chosen = ElectricalDemandFactorRule.LoadTablePerPortion;
    else if (ruleWord == "quantity" || ruleWord == "quantitytable") chosen = ElectricalDemandFactorRule.QuantityTable;
    else if (ruleWord == "quantityportion" || ruleWord == "quantitytableperportion" || ruleWord == "quantityperportion") chosen = ElectricalDemandFactorRule.QuantityTablePerPortion;
    else refused.Add("\"" + rule + "\" is not a demand factor rule - constant, load, loadPortion, quantity or quantityPortion.");

    var byLoad = chosen == ElectricalDemandFactorRule.LoadTable || chosen == ElectricalDemandFactorRule.LoadTablePerPortion;
    var parsed = new List<Tuple<double, double, double>>();   // from, to (NaN = and over), fraction
    if (refused.Count == 0 && chosen == ElectricalDemandFactorRule.Constant)
    {
        if (hasSteps) refused.Add("A constant demand factor has one percentage, not steps - give percent, or a table rule.");
        var p = double.IsNaN(percent) || percent <= 0 ? 100.0 : percent;
        if (p > 1000) refused.Add(num(p) + "% is not a demand factor percentage.");
        parsed.Add(Tuple.Create(0.0, double.NaN, p / 100.0));
    }
    else if (refused.Count == 0)
    {
        if (!hasSteps) refused.Add("A table demand factor needs its steps - \"0-10:100; 10-:80\", from-to "
            + (byLoad ? "in kVA" : "as a count") + ", then the percentage.");
        foreach (var raw in stepText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!hasSteps) break;
            var piece = raw.Trim();
            var colon = piece.LastIndexOf(':');
            var dash = colon > 0 ? piece.IndexOf('-', 1) : -1;
            double from, to = double.NaN, pct;
            var ok = colon > 0 && dash > 0 && dash < colon
                && double.TryParse(piece.Substring(0, dash).Trim(), System.Globalization.NumberStyles.Float, invariant, out from)
                && double.TryParse(piece.Substring(colon + 1).Trim().TrimEnd('%'), System.Globalization.NumberStyles.Float, invariant, out pct);
            if (!ok) { refused.Add("\"" + piece + "\" is not a step - write from-to:percent, as \"10-:80\" for 10 and over at 80%."); continue; }
            from = double.Parse(piece.Substring(0, dash).Trim(), invariant);
            pct = double.Parse(piece.Substring(colon + 1).Trim().TrimEnd('%'), invariant);
            var toText = piece.Substring(dash + 1, colon - dash - 1).Trim();
            if (toText.Length > 0 && !double.TryParse(toText, System.Globalization.NumberStyles.Float, invariant, out to))
            { refused.Add("\"" + piece + "\": \"" + toText + "\" is not a number."); continue; }
            if (from < 0 || (!double.IsNaN(to) && to <= from) || pct < 0 || pct > 1000)
            { refused.Add("\"" + piece + "\" does not make sense - from below to, and a percentage from 0."); continue; }
            parsed.Add(Tuple.Create(from, to, pct / 100.0));
        }
        parsed.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        for (var i = 1; i < parsed.Count && refused.Count == 0; i++)
            if (double.IsNaN(parsed[i - 1].Item2) || parsed[i - 1].Item2 > parsed[i].Item1)
                refused.Add("The steps overlap at " + num(parsed[i].Item1) + (byLoad ? " kVA" : "") + ".");
    }

    var taken = nameTaken(factors.Select(f => f.Name), wantName);
    if (refused.Count == 0 && taken != null)
        skipped.Add("A demand factor is already named " + quoted(taken) + " - left as it is, nothing added.");
    else if (refused.Count == 0)
    {
        try
        {
            var made = ElectricalDemandFactorDefinition.Create(doc, wantName);
            if (made == null) refused.Add("Revit made no demand factor named " + quoted(wantName) + ".");
            else
            {
                made.RuleType = chosen;
                var scale = byLoad ? 1000.0 * perVoltAmpere : 1.0;
                var values = new List<ElectricalDemandFactorValue>();
                foreach (var s in parsed)
                {
                    var v = new ElectricalDemandFactorValue(s.Item1 * scale, double.IsNaN(s.Item2) ? s.Item1 * scale : s.Item2 * scale, s.Item3);
                    if (double.IsNaN(s.Item2)) v.SetMaxRangeToUnlimited();
                    values.Add(v);
                }
                made.SetValues(values);
                added++;
                findings.Add("Added demand factor " + factorLine(made) + ".");
            }
        }
        catch (Exception ex) { refused.Add("Revit would not add demand factor " + quoted(wantName) + ": " + ex.Message); }
    }
}

// ---- Read back -------------------------------------------------------------
if (isLoad || isFactor) { readLists(); after.AddRange(listNow()); }
findings.Insert(0, (added > 0 ? "Added 1 " : "Added nothing - ") + (isFactor ? "demand factor" : "load classification")
    + (added > 0 ? "" : (skipped.Count > 0 ? "; the name is already here, skipped" : refused.Count > 0 ? "; refused" : ""))
    + ". " + (isFactor ? "Demand factors" : "Load classifications") + ": " + before.Count + " before, " + after.Count + " after.");
findings.AddRange(skipped);
findings.AddRange(refused);
