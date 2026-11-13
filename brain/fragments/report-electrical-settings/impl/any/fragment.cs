// NOT STANDALONE. Assumes `doc`, `show` and `nameContains` are in scope, and
// leaves `voltageTypes`, `distributionSystems`, `wireTypes`,
// `loadClassifications`, `demandFactors`, `panelScheduleTemplates`,
// `circuitNamingSchemes`, `report`, `names` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Manage > MEP Settings > Electrical Settings - Voltage
// Definitions, Distribution Systems and Wiring Types - and Manage > MEP
// Settings > Load Classifications and Demand Factors, Manage > Panel Schedule
// Templates and Electrical Settings > Circuit Naming (both added 2026-11-02,
// beside SET_PANEL_SCHEDULE_TEMPLATE and SET_CIRCUIT_NAMING_SCHEME). Every
// list, or only the ones `show` names (voltages; distribution; wires; loads;
// demand; templates; naming - several separated by ; or ,), narrowed to names
// containing `nameContains`.
//
// UNITS - A VOLTAGE TYPE'S VALUES ARE VOLTS (version 2, FRAGMENT-ISSUES
// 5b-1636). Autodesk's reference says it of VoltageType.ActualValue, MinValue
// and MaxValue in the same words on every release 2020 to 2027 - "the unit is
// volt" (RevitAPI.xml of the Nice3point reference packages, read 2026-11-13) -
// and Revit 2020 measured it: the type named "120" gave ActualValue 120.
// Version 1 divided them by Revit's internal volt factor, so "120" read
// "11.15 V". They are printed as they come, and so are a distribution
// system's two voltages, which are voltage types.
//
// A DEMAND FACTOR'S LOADS ARE NOT SETTLED, SO BOTH READINGS ARE PRINTED. The
// reference gives no unit for ElectricalDemandFactorValue.MinRange / MaxRange
// or ElectricalDemandFactorDefinition.AdditionalLoad, and no model has
// measured one. Version 1 read them in Revit's internal units, where one
// volt-ampere is 1 / (0.3048 * 0.3048) of them - checked against Revit's own
// UnitUtils on the release that runs (DisplayUnitType in 2020, UnitTypeId from
// 2021), Revit's own used and named if the two disagree. A load figure is
// printed that way AND as volt-amperes, beside each other, with a finding that
// says which is unsettled - never one of them guessed. A demand factor's
// percentage is its Factor x 100.
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
// PanelScheduleTemplate.GetPanelScheduleType and IsDefault, and
// PanelScheduleView.GetTemplate and IsPanelScheduleTemplate, are present 2020
// to 2027 (tools/api-surface --members, 2026-11-02). CircuitNamingScheme
// (GetCombinedParameters) and CircuitNamingSchemeSettings
// (GetCircuitNamingSchemeSettings, CircuitNamingSchemeId) are 2021 on and not
// in 2020 (#if !REVIT2020); TableCellCombinedParameterData ParamId, Prefix,
// Suffix and Separator are 2020 to 2027.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var voltageTypes = 0;
var distributionSystems = 0;
var wireTypes = 0;
var loadClassifications = 0;
var demandFactors = 0;
var panelScheduleTemplates = 0;
var circuitNamingSchemes = 0;
var report = "";
var names = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var needle = (nameContains ?? "").Trim();
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
if (string.Equals(needle, "none", StringComparison.OrdinalIgnoreCase)) needle = "";
Func<string, bool> wanted = n => needle.Length == 0 || (n ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
Func<double, string> num = v => Math.Round(v, 2).ToString("0.##", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";

// ---- Which lists -----------------------------------------------------------
var asked = (show ?? "").Trim().ToLowerInvariant();
var every = asked.Length == 0 || asked == "all";
bool wantVoltages = every, wantDistribution = every, wantWires = every, wantLoads = every, wantDemand = every;
var wantTemplates = every;
var wantNaming = every;
var unknownWords = new List<string>();
if (!every)
{
    foreach (var raw in asked.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var w = raw.Trim().Replace(" ", "").Replace("-", "");
        if (w.Length == 0) continue;
        if (w == "all") { wantVoltages = wantDistribution = wantWires = wantLoads = wantDemand = wantTemplates = wantNaming = true; }
        else if (w.StartsWith("volt")) wantVoltages = true;
        else if (w.StartsWith("distribution") || w == "systems") wantDistribution = true;
        else if (w.StartsWith("wire") || w.StartsWith("wiring")) wantWires = true;
        else if (w.StartsWith("load")) wantLoads = true;
        else if (w.StartsWith("demand")) wantDemand = true;
        // "panels" alone is not a list - a panel's circuits are another read.
        else if (w.StartsWith("template") || w.StartsWith("panelschedule")) wantTemplates = true;
        else if (w.StartsWith("naming") || w.StartsWith("circuitnaming") || w.StartsWith("scheme")) wantNaming = true;
        else unknownWords.Add(raw.Trim());
    }
}
if (unknownWords.Count > 0)
    findings.Add("\"" + string.Join("\", \"", unknownWords) + "\" is not one of the lists - voltages, distribution, wires, "
        + "loads, demand, templates or naming, or all. Nothing was guessed for it.");

// ---- The load unit, checked against Revit's own -----------------------------
// For a demand factor's load figures only - a voltage type's values are volts.
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
        + perVolt.ToString(invariant) + " - the demand factors' internal-unit load figures below use Revit's own.");
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
        // Volts as they come (version 2) - see UNITS in the header.
        rows.Add(quoted(v.Name) + " " + num(v.ActualValue) + " V (min " + num(v.MinValue)
            + ", max " + num(v.MaxValue) + ")" + (v.IsInUse ? ", in use" : ""));
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
            + (ll == null ? "none" : quoted(ll.Name) + " " + num(ll.ActualValue) + " V")
            + ", line to ground " + (lg == null ? "none" : quoted(lg.Name) + " " + num(lg.ActualValue) + " V")
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
// A load figure in both readings (version 2) - see UNITS in the header.
Func<double, string> bothReadings = x => num(x / perVolt / 1000.0) + " kVA if internal units, "
    + num(x / 1000.0) + " kVA if volt-amperes";
var loadFiguresPrinted = 0;
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
        Func<double, string> amount = x => byLoad ? "(" + bothReadings(x) + ")" : num(x);
        if (byLoad) loadFiguresPrinted++;
        // An unlimited upper end is stored as a very large number.
        var open = v.MaxRange >= 1e12 || double.IsInfinity(v.MaxRange);
        steps.Add(amount(v.MinRange) + (open ? " and over" : " to " + amount(v.MaxRange)) + ": " + pct);
    }
    factorRows.Add(quoted(f.Name) + " " + ruleWords + " - " + (steps.Count == 0 ? "no values" : string.Join(", ", steps))
        + (f.IncludeAdditionalLoad ? ", plus additional load (" + bothReadings(f.AdditionalLoad) + ")" : ""));
    if (f.IncludeAdditionalLoad) loadFiguresPrinted++;
}
if (loadFiguresPrinted > 0)
    findings.Add("A demand factor's load figures are printed TWO WAYS - as Revit's internal units and as volt-amperes - "
        + "because which of the two Revit's API gives is not established (FRAGMENT-ISSUES 5b-1636). Read the one "
        + "that matches Manage > MEP Settings > Demand Factors.");

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

// ---- Panel Schedule Templates ----------------------------------------------
if (wantTemplates)
{
    // Template -> the panel schedules that use it, by name.
    var used = new Dictionary<ElementId, List<string>>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(PanelScheduleView)))
    {
        var v = e as PanelScheduleView;
        if (v == null || v.IsPanelScheduleTemplate()) continue;
        ElementId t = null;
        try { t = v.GetTemplate(); } catch (Exception) { t = null; }
        if (t == null || t == ElementId.InvalidElementId) continue;
        List<string> on;
        if (!used.TryGetValue(t, out on)) { on = new List<string>(); used[t] = on; }
        on.Add(quoted(v.Name));
    }
    var rows = new List<string>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(PanelScheduleTemplate)))
    {
        var t = e as PanelScheduleTemplate;
        if (t == null || !wanted(t.Name)) continue;
        panelScheduleTemplates++;
        names.Add(quoted(t.Name) + " (panel schedule template)");
        var kind = "unknown kind";
        try
        {
            var k = t.GetPanelScheduleType();
            kind = k == PanelScheduleType.Branch ? "branch panel" : k == PanelScheduleType.Switchboard ? "switchboard"
                : k == PanelScheduleType.Data ? "data panel" : "unknown kind";
        }
        catch (Exception) { }
        List<string> users;
        if (!used.TryGetValue(t.Id, out users)) users = new List<string>();
        users.Sort(StringComparer.OrdinalIgnoreCase);
        rows.Add(quoted(t.Name) + " " + kind + (t.IsDefault ? ", default" : "") + ", used by " + users.Count
            + " panel schedule(s)" + (users.Count == 0 ? "" : ": " + string.Join(", ", users)));
    }
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Panel schedule templates (" + rows.Count + "): " + (rows.Count == 0 ? "none" : string.Join("; ", rows)));
}

// ---- Circuit Naming Schemes (Revit 2021 on) ---------------------------------
if (wantNaming)
{
#if REVIT2020
    lines.Add("Circuit naming schemes: none in Revit 2020 - they arrived in Revit 2021; in 2020 each panel names its "
        + "circuits by its own Circuit Naming parameter.");
#else
    var inUse = ElementId.InvalidElementId;
    try
    {
        var namingSettings = CircuitNamingSchemeSettings.GetCircuitNamingSchemeSettings(doc);
        if (namingSettings != null) inUse = namingSettings.CircuitNamingSchemeId;
    }
    catch (Exception ex) { findings.Add("The project's circuit naming setting could not be read: " + ex.Message); }

    // A part's parameter by name: a project parameter's element, else the
    // parameter of that id on any circuit in the model, else Revit's label of
    // the circuit parameters a scheme is usually made of.
    var circuitSample = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ElectricalCircuit)
        .WhereElementIsNotElementType().FirstElement();
    var usual = new[] { BuiltInParameter.RBS_ELEC_CIRCUIT_PANEL_PARAM, BuiltInParameter.RBS_ELEC_CIRCUIT_NUMBER,
        BuiltInParameter.RBS_ELEC_CIRCUIT_NAME, BuiltInParameter.RBS_ELEC_CIRCUIT_START_SLOT };
    Func<ElementId, string> partName = id =>
    {
        if (id == null || id == ElementId.InvalidElementId) return "nothing";
        var pe = doc.GetElement(id) as ParameterElement;
        if (pe != null) return pe.Name;
        if (circuitSample != null)
            foreach (Parameter p in circuitSample.Parameters)
                if (p != null && p.Id == id && p.Definition != null) return p.Definition.Name;
        foreach (var b in usual)
            if (new ElementId(b) == id)
            {
                try { return LabelUtils.GetLabelFor(b); } catch (Exception) { return b.ToString(); }
            }
        return "a parameter not named here (id " + id + ")";
    };

    var rows = new List<string>();
    var projects = "";
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(CircuitNamingScheme)))
    {
        var scheme = e as CircuitNamingScheme;
        if (scheme == null) continue;
        if (scheme.Id == inUse) projects = scheme.Name;
        if (!wanted(scheme.Name)) continue;
        circuitNamingSchemes++;
        names.Add(quoted(scheme.Name) + " (circuit naming scheme)");
        var parts = new List<string>();
        try
        {
            var data = scheme.GetCombinedParameters();
            for (var i = 0; data != null && i < data.Count; i++)
            {
                var d = data[i];
                parts.Add((string.IsNullOrEmpty(d.Prefix) ? "" : quoted(d.Prefix) + " ") + partName(d.ParamId)
                    + (string.IsNullOrEmpty(d.Suffix) ? "" : " " + quoted(d.Suffix))
                    + (i < data.Count - 1 || !string.IsNullOrEmpty(d.Separator) ? " " + quoted(d.Separator ?? "") : ""));
            }
        }
        catch (Exception ex) { parts.Add("parts not read: " + ex.Message); }
        rows.Add(quoted(scheme.Name) + ": " + (parts.Count == 0 ? "no parts" : string.Join(" + ", parts))
            + (scheme.Id == inUse ? " - the project's" : ""));
    }
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    lines.Add("Circuit naming schemes (" + rows.Count + "): " + (rows.Count == 0 ? "none" : string.Join("; ", rows))
        + ". The project uses " + (projects.Length == 0 ? "none Heron could read" : quoted(projects)) + ".");
#endif
}

report = string.Join(" | ", lines);
var total = voltageTypes + distributionSystems + wireTypes + loadClassifications + demandFactors + panelScheduleTemplates
    + circuitNamingSchemes;
findings.Insert(0, (needle.Length > 0 ? "Matching \"" + needle + "\": " : "")
    + voltageTypes + " voltage(s), " + distributionSystems + " distribution system(s), " + wireTypes + " wire type(s), "
    + loadClassifications + " load classification(s), " + demandFactors + " demand factor(s), "
    + panelScheduleTemplates + " panel schedule template(s), " + circuitNamingSchemes + " circuit naming scheme(s)"
    + (every ? "" : " - only the lists asked for were read") + ".");
if (needle.Length > 0 && total == 0)
    findings.Add("Nothing in the lists read has a name containing \"" + needle + "\" - leave it out to see every name.");
findings.AddRange(lines);
