// NOT STANDALONE. Assumes `doc`, `show` and `nameContains` are in scope, and
// leaves `voltageTypes`, `distributionSystems`, `wireTypes`,
// `loadClassifications`, `demandFactors`, `report`, `names` and `findings`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Manage > MEP Settings > Electrical Settings - Voltage
// Definitions, Distribution Systems and Wiring Types - and Manage > MEP
// Settings > Load Classifications and Demand Factors. Every list, or only the
// ones `show` names (voltages; distribution; wires; loads; demand - several
// separated by ; or ,), narrowed to names containing `nameContains`.
//
// UNITS. Revit keeps a voltage and a load in its own internal units: one volt,
// and one volt-ampere, is 1 / (0.3048 * 0.3048) of them - the factor
// SET_FAMILY_CONNECTOR_ROLES writes connector voltages with. It is checked
// against Revit's own UnitUtils on the release that runs (DisplayUnitType in
// 2020, UnitTypeId from 2021); if the two disagree, Revit's own is used and the
// disagreement is named. A demand factor's percentage is its Factor x 100.
//
// RELEASES. ElectricalSetting.GetElectricalSettings, VoltageTypes,
// DistributionSysTypes, WireTypes; VoltageType.ActualValue, MinValue, MaxValue,
// IsInUse; DistributionSysType.ElectricalPhase, ElectricalPhaseConfiguration,
// NumWires, VoltageLineToLine, VoltageLineToGround; ElectricalLoadClassification
// .DemandFactorId, Motor, SpaceLoadClass; ElectricalDemandFactorDefinition
// .RuleType, GetValues, AdditionalLoad, IncludeAdditionalLoad and
// ElectricalDemandFactorValue.MinRange, MaxRange, Factor are present 2020 to
// 2027 (tools/api-surface --members, 2026-11-01). ElectricalLoadClassification
// .Abbreviation is 2021 on. WireType's material, insulation and temperature
// rating are objects to 2025 and ElementIds from 2026, and MaxSize a WireSize
// to 2025 and a string from 2026 - each release takes its own branch.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var voltageTypes = 0;
var distributionSystems = 0;
var wireTypes = 0;
var loadClassifications = 0;
var demandFactors = 0;
var report = "";
var names = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var needle = (nameContains ?? "").Trim();
Func<string, bool> wanted = n => needle.Length == 0 || (n ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
Func<double, string> num = v => Math.Round(v, 2).ToString("0.##", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";

// ---- Which lists -----------------------------------------------------------
var asked = (show ?? "").Trim().ToLowerInvariant();
var every = asked.Length == 0 || asked == "all";
bool wantVoltages = every, wantDistribution = every, wantWires = every, wantLoads = every, wantDemand = every;
var unknownWords = new List<string>();
if (!every)
{
    foreach (var raw in asked.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var w = raw.Trim().Replace(" ", "").Replace("-", "");
        if (w.Length == 0) continue;
        if (w == "all") { wantVoltages = wantDistribution = wantWires = wantLoads = wantDemand = true; }
        else if (w.StartsWith("volt")) wantVoltages = true;
        else if (w.StartsWith("distribution") || w == "systems") wantDistribution = true;
        else if (w.StartsWith("wire") || w.StartsWith("wiring")) wantWires = true;
        else if (w.StartsWith("load")) wantLoads = true;
        else if (w.StartsWith("demand")) wantDemand = true;
        else unknownWords.Add(raw.Trim());
    }
}
if (unknownWords.Count > 0)
    findings.Add("\"" + string.Join("\", \"", unknownWords) + "\" is not one of the lists - voltages, distribution, wires, "
        + "loads or demand, or all. Nothing was guessed for it.");

// ---- The unit, checked against Revit's own ---------------------------------
var perVolt = 1.0 / (0.3048 * 0.3048);
double revitsPerVolt;
try
{
#if REVIT2020
    revitsPerVolt = UnitUtils.ConvertToInternalUnits(1.0, DisplayUnitType.DUT_VOLTS);
#else
    revitsPerVolt = UnitUtils.ConvertToInternalUnits(1.0, UnitTypeId.Volts);
#endif
}
catch (Exception) { revitsPerVolt = perVolt; }
if (Math.Abs(revitsPerVolt - perVolt) > 1e-9 * perVolt)
{
    findings.Add("Revit makes one volt " + revitsPerVolt.ToString(invariant) + " internal units where Heron expected "
        + perVolt.ToString(invariant) + " - the voltages and loads below use Revit's own.");
    perVolt = revitsPerVolt;
}

var lines = new List<string>();

ElectricalSetting settings = null;
try { settings = ElectricalSetting.GetElectricalSettings(doc); }
catch (Exception ex) { findings.Add("Electrical Settings could not be read: " + ex.Message); }
if (settings == null && (wantVoltages || wantDistribution || wantWires))
    findings.Add("This model has no Electrical Settings to read" + (doc.IsFamilyDocument ? " - it is a family; open a project." : "."));

// ---- Voltage Definitions ---------------------------------------------------
if (settings != null && wantVoltages)
{
    var rows = new List<string>();
    foreach (VoltageType v in settings.VoltageTypes)
    {
        if (v == null || !wanted(v.Name)) continue;
        voltageTypes++;
        names.Add(quoted(v.Name) + " (voltage)");
        rows.Add(quoted(v.Name) + " " + num(v.ActualValue / perVolt) + " V (min " + num(v.MinValue / perVolt)
            + ", max " + num(v.MaxValue / perVolt) + ")" + (v.IsInUse ? ", in use" : ""));
    }
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Voltages (" + rows.Count + "): " + (rows.Count == 0 ? "none" : string.Join("; ", rows)));
}

// ---- Distribution Systems --------------------------------------------------
if (settings != null && wantDistribution)
{
    var rows = new List<string>();
    foreach (DistributionSysType d in settings.DistributionSysTypes)
    {
        if (d == null || !wanted(d.Name)) continue;
        distributionSystems++;
        names.Add(quoted(d.Name) + " (distribution system)");
        var phase = d.ElectricalPhase == ElectricalPhase.ThreePhase ? "3-phase" : "1-phase";
        var config = d.ElectricalPhase == ElectricalPhase.ThreePhase
            ? (d.ElectricalPhaseConfiguration == ElectricalPhaseConfiguration.Wye ? " wye"
                : d.ElectricalPhaseConfiguration == ElectricalPhaseConfiguration.Delta ? " delta" : "")
            : "";
        var ll = d.VoltageLineToLine;
        var lg = d.VoltageLineToGround;
        rows.Add(quoted(d.Name) + " " + phase + config + " " + d.NumWires + "-wire, line to line "
            + (ll == null ? "none" : quoted(ll.Name) + " " + num(ll.ActualValue / perVolt) + " V")
            + ", line to ground " + (lg == null ? "none" : quoted(lg.Name) + " " + num(lg.ActualValue / perVolt) + " V")
            + (d.IsInUse ? ", in use" : ""));
    }
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Distribution systems (" + rows.Count + "): " + (rows.Count == 0 ? "none" : string.Join("; ", rows)));
}

// ---- Wiring Types ----------------------------------------------------------
if (settings != null && wantWires)
{
    Func<ElementId, string> nameOf = id =>
    {
        if (id == null || id == ElementId.InvalidElementId) return "none";
        var e = doc.GetElement(id);
        return e == null ? "none" : e.Name;
    };
    var rows = new List<string>();
    foreach (WireType w in settings.WireTypes)
    {
        if (w == null || !wanted(w.Name)) continue;
        wireTypes++;
        names.Add(quoted(w.Name) + " (wire type)");
        string material = "", insulation = "", rating = "", maxSize = "";
        try
        {
#if REVIT2026_OR_GREATER
            material = nameOf(w.WireMaterial);
            insulation = nameOf(w.Insulation);
            rating = nameOf(w.TemperatureRating);
            maxSize = w.MaxSize ?? "";
#else
            material = w.WireMaterial == null ? "none" : w.WireMaterial.Name;
            insulation = w.Insulation == null ? "none" : w.Insulation.Name;
            rating = w.TemperatureRating == null ? "none" : w.TemperatureRating.Name;
            maxSize = w.MaxSize == null ? "" : w.MaxSize.Size;
#endif
        }
        catch (Exception) { }
        var conduit = "";
        try { conduit = w.Conduit == null ? "" : w.Conduit.Name; } catch (Exception) { }
        rows.Add(quoted(w.Name) + " " + material + ", " + rating + ", " + insulation
            + (maxSize.Length > 0 ? ", max size " + maxSize : "")
            + ", neutral multiplier " + num(w.NeutralMultiplier) + (w.NeutralRequired ? ", neutral required" : "")
            + (conduit.Length > 0 ? ", conduit " + conduit : "") + (w.IsInUse ? ", in use" : ""));
    }
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Wire types (" + rows.Count + "): " + (rows.Count == 0 ? "none" : string.Join("; ", rows)));
}

// ---- Demand Factors (read first: a load classification names one) ----------
var factorNames = new Dictionary<ElementId, string>();
var factorRows = new List<string>();
foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ElectricalDemandFactorDefinition)))
{
    var f = e as ElectricalDemandFactorDefinition;
    if (f == null) continue;
    factorNames[f.Id] = f.Name;
    if (!wantDemand || !wanted(f.Name)) continue;
    demandFactors++;
    names.Add(quoted(f.Name) + " (demand factor)");
    var rule = f.RuleType;
    var byLoad = rule == ElectricalDemandFactorRule.LoadTable || rule == ElectricalDemandFactorRule.LoadTablePerPortion;
    var ruleWords = rule == ElectricalDemandFactorRule.Constant ? "constant"
        : rule == ElectricalDemandFactorRule.QuantityTable ? "by quantity"
        : rule == ElectricalDemandFactorRule.QuantityTablePerPortion ? "by quantity, for each portion"
        : rule == ElectricalDemandFactorRule.LoadTable ? "by load"
        : "by load, for each portion";
    var steps = new List<string>();
    var values = new List<ElectricalDemandFactorValue>(f.GetValues() ?? new List<ElectricalDemandFactorValue>());
    values.Sort((a, b) => a.MinRange.CompareTo(b.MinRange));
    foreach (var v in values)
    {
        var pct = num(v.Factor * 100) + "%";
        if (rule == ElectricalDemandFactorRule.Constant) { steps.Add(pct); continue; }
        Func<double, string> amount = x => byLoad ? num(x / perVolt / 1000.0) + " kVA" : num(x);
        // An unlimited upper end is stored as a very large number.
        var open = v.MaxRange >= 1e12 || double.IsInfinity(v.MaxRange);
        steps.Add(amount(v.MinRange) + (open ? " and over" : " to " + amount(v.MaxRange)) + ": " + pct);
    }
    factorRows.Add(quoted(f.Name) + " " + ruleWords + " - " + (steps.Count == 0 ? "no values" : string.Join(", ", steps))
        + (f.IncludeAdditionalLoad ? ", plus additional load " + num(f.AdditionalLoad / perVolt / 1000.0) + " kVA" : ""));
}

// ---- Load Classifications --------------------------------------------------
if (wantLoads)
{
    var rows = new List<string>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ElectricalLoadClassification)))
    {
        var c = e as ElectricalLoadClassification;
        if (c == null || !wanted(c.Name)) continue;
        loadClassifications++;
        names.Add(quoted(c.Name) + " (load classification)");
        var factor = c.DemandFactorId != null && factorNames.ContainsKey(c.DemandFactorId)
            ? quoted(factorNames[c.DemandFactorId]) : "none";
        var abbreviation = "";
#if !REVIT2020
        abbreviation = c.Abbreviation ?? "";
#endif
        var space = c.SpaceLoadClass == ElectricalLoadClassificationSpace.Lighting ? ", space load: lighting"
            : c.SpaceLoadClass == ElectricalLoadClassificationSpace.Power ? ", space load: power" : "";
        rows.Add(quoted(c.Name) + (abbreviation.Length > 0 ? " (" + abbreviation + ")" : "") + ", demand factor " + factor
            + (c.Motor ? ", motor" : "") + space);
    }
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Load classifications (" + rows.Count + "): " + (rows.Count == 0 ? "none" : string.Join("; ", rows)));
}
if (wantDemand)
{
    factorRows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Demand factors (" + factorRows.Count + "): " + (factorRows.Count == 0 ? "none" : string.Join("; ", factorRows)));
}

report = string.Join(" | ", lines);
var total = voltageTypes + distributionSystems + wireTypes + loadClassifications + demandFactors;
findings.Insert(0, (needle.Length > 0 ? "Matching \"" + needle + "\": " : "")
    + voltageTypes + " voltage(s), " + distributionSystems + " distribution system(s), " + wireTypes + " wire type(s), "
    + loadClassifications + " load classification(s), " + demandFactors + " demand factor(s)"
    + (every ? "" : " - only the lists asked for were read") + ".");
if (needle.Length > 0 && total == 0)
    findings.Add("Nothing in the lists read has a name containing \"" + needle + "\" - leave it out to see every name.");
findings.AddRange(lines);
