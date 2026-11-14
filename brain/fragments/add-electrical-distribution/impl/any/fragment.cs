// NOT STANDALONE. Assumes `doc`, `add`, `name`, `volts`, `minVolts`,
// `maxVolts`, `lineToGroundVolts`, `phases`, `configuration` and `wires` are in
// scope, and leaves `added`, `before`, `after`, `skipped`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Manage > MEP Settings > Electrical Settings: adds ONE voltage
// type (Voltage Definitions) or ONE distribution system (Distribution
// Systems). Nothing existing is changed, renamed or removed:
//   - a name already present - in any capitals - is SKIPPED and named;
//   - with no name given, an item with the same values already present is
//     SKIPPED and named, rather than a second copy made under a made-up name.
// The list it adds to is read before and after, and both are handed back.
//
// NEVER GUESSED (D-33). A distribution system needs its phases, its wires and,
// for 3-phase, wye or delta: each missing one is refused and named. Its
// voltages are found among the model's voltage types by VALUE; a value no
// voltage type has is refused with the voltages the model has, so it is added
// first, as its own change.
//
// UNITS - VOLTS IN, VOLTS TO THE API, AND READ BACK (version 2, FRAGMENT-ISSUES
// 5b-1636). Autodesk's reference says of VoltageType.ActualValue, MinValue and
// MaxValue, in the same words on every release 2020 to 2027, "the unit is
// volt", and of VoltageType.SetVoltageValue "all of the unit are volt"; Revit
// 2020 measured it - the type named "120" gave ActualValue 120. Version 1
// multiplied by Revit's internal volt factor, 1 / (0.3048 * 0.3048) - right
// for a connector's voltage PARAMETER, wrong here - so a distribution system
// on the model's own 208 V and 120 V was refused ("No voltage type of 208 V")
// and a 230 V type would have been made at about 2476. Now a voltage is
// matched, and written, in volts. AddVoltageType's own entry names no unit,
// so what it made is READ BACK: a new type that does not read the volts
// asked is removed again in the same undo entry and the answer says what
// Revit read - nothing is kept on a unit nobody confirmed.
//
// RELEASES. ElectricalSetting.GetElectricalSettings, VoltageTypes,
// DistributionSysTypes, AddVoltageType(string, double, double, double) and
// AddDistributionSysType(string, ElectricalPhase, ElectricalPhaseConfiguration,
// int, VoltageType, VoltageType); VoltageType.ActualValue, MinValue, MaxValue;
// DistributionSysType.ElectricalPhase, ElectricalPhaseConfiguration, NumWires,
// VoltageLineToLine, VoltageLineToGround - present with the same signatures
// 2020 to 2027 (tools/api-surface --members, 2026-11-01).

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
var isVoltage = what == "voltage" || what == "voltagetype" || what == "voltages";
var isDistribution = what == "distribution" || what == "distributionsystem" || what == "distributionsystems";
var wantName = (name ?? "").Trim();
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
if (string.Equals(wantName, "none", StringComparison.OrdinalIgnoreCase)) wantName = "";

ElectricalSetting settings = null;
try { settings = ElectricalSetting.GetElectricalSettings(doc); }
catch (Exception ex) { refused.Add("Electrical Settings could not be read: " + ex.Message); }

// Volts as they come (version 2) - see UNITS in the header.
Func<VoltageType, string> voltageLine = v => quoted(v.Name) + " " + num(v.ActualValue) + " V (min "
    + num(v.MinValue) + ", max " + num(v.MaxValue) + ")";
Func<DistributionSysType, string> systemLine = d =>
    quoted(d.Name) + " " + (d.ElectricalPhase == ElectricalPhase.ThreePhase ? "3-phase" : "1-phase")
    + (d.ElectricalPhase != ElectricalPhase.ThreePhase ? ""
        : d.ElectricalPhaseConfiguration == ElectricalPhaseConfiguration.Wye ? " wye"
        : d.ElectricalPhaseConfiguration == ElectricalPhaseConfiguration.Delta ? " delta" : "")
    + " " + d.NumWires + "-wire, line to line "
    + (d.VoltageLineToLine == null ? "none" : num(d.VoltageLineToLine.ActualValue) + " V")
    + ", line to ground " + (d.VoltageLineToGround == null ? "none" : num(d.VoltageLineToGround.ActualValue) + " V");

var voltages = new List<VoltageType>();
var systems = new List<DistributionSysType>();
Action readLists = () =>
{
    voltages.Clear();
    systems.Clear();
    if (settings == null) return;
    foreach (VoltageType v in settings.VoltageTypes) if (v != null) voltages.Add(v);
    foreach (DistributionSysType d in settings.DistributionSysTypes) if (d != null) systems.Add(d);
};
Func<List<string>> listNow = () =>
{
    var rows = new List<string>();
    if (isVoltage) foreach (var v in voltages) rows.Add(voltageLine(v));
    else foreach (var d in systems) rows.Add(systemLine(d));
    rows.Sort(StringComparer.OrdinalIgnoreCase);
    return rows;
};

// ---- Refusals before anything is touched -----------------------------------
if (!isVoltage && !isDistribution)
    refused.Add("\"" + (add ?? "") + "\" is not something this adds - voltage or distribution. Nothing was guessed.");
if (settings == null && refused.Count == 0)
    refused.Add("This model has no Electrical Settings" + (doc.IsFamilyDocument ? " - it is a family; open a project." : "."));
if (double.IsNaN(volts) || volts <= 0)
    refused.Add("volts must be a voltage above 0 - " + (isDistribution ? "the line-to-line voltage" : "the voltage's actual value") + ".");

if (refused.Count == 0) readLists();
if (refused.Count == 0) before.AddRange(listNow());

// A name already present, in any capitals, is skipped and named.
Func<IEnumerable<string>, string, string> nameTaken = (existing, wanted) =>
{
    foreach (var n in existing)
        if (string.Equals(n, wanted, StringComparison.OrdinalIgnoreCase)) return n;
    return null;
};
// Two voltages in volts are the same to a two-hundredth of a volt.
Func<double, double, bool> sameVolts = (a, b) => Math.Abs(a - b) < 0.005;

if (refused.Count == 0 && isVoltage)
{
    var min = double.IsNaN(minVolts) || minVolts <= 0 ? volts : minVolts;
    var max = double.IsNaN(maxVolts) || maxVolts <= 0 ? volts : maxVolts;
    if (min > volts || max < volts)
        refused.Add("The voltage's minimum " + num(min) + " V and maximum " + num(max) + " V must lie either side of its actual "
            + num(volts) + " V.");
    var newName = wantName.Length > 0 ? wantName : num(volts) + " V";
    var taken = nameTaken(voltages.Select(v => v.Name), newName);
    var same = voltages.Where(v => sameVolts(v.ActualValue, volts)).ToList();
    if (refused.Count == 0 && taken != null)
        skipped.Add("A voltage type is already named " + quoted(taken) + " - left as it is, nothing added. Give another name to add one.");
    else if (refused.Count == 0 && wantName.Length == 0 && same.Count > 0)
        skipped.Add("A voltage type of " + num(volts) + " V is already here: " + string.Join(", ", same.Select(voltageLine))
            + " - nothing added. Give a name to add a second one.");
    else if (refused.Count == 0)
    {
        try
        {
            var made = settings.AddVoltageType(newName, volts, min, max);
            if (made != null && !(sameVolts(made.ActualValue, volts) && sameVolts(made.MinValue, min) && sameVolts(made.MaxValue, max)))
            {
                // READ BACK OTHERWISE - taken out again, never kept (version 2).
                var readBack = voltageLine(made);
                string removal = null;
                try { settings.RemoveVoltageType(made); }
                catch (Exception ex) { removal = ex.Message; }
                refused.Add("Revit read the new voltage type back as " + readBack + ", not " + num(volts) + " V (min " + num(min)
                    + ", max " + num(max) + ") - "
                    + (removal == null ? "it was removed again and nothing was kept"
                        : "and Revit would not remove it again (" + removal + "), so press Discard or Undo")
                    + ". Which unit AddVoltageType takes is FRAGMENT-ISSUES 5b-1636.");
            }
            else if (made != null)
            {
                added++;
                findings.Add("Added voltage type " + voltageLine(made) + ", read back as asked.");
                if (same.Count > 0)
                    findings.Add("Another voltage type has the same " + num(volts) + " V: " + string.Join(", ", same.Select(v => quoted(v.Name))) + ".");
            }
            else refused.Add("Revit added no voltage type named " + quoted(newName) + ".");
        }
        catch (Exception ex) { refused.Add("Revit would not add voltage type " + quoted(newName) + ": " + ex.Message); }
    }
}

if (refused.Count == 0 && isDistribution)
{
    var lg = double.IsNaN(lineToGroundVolts) || lineToGroundVolts <= 0 ? 0.0 : lineToGroundVolts;
    var conf = (configuration ?? "").Trim().ToLowerInvariant();
    if (conf == "none") conf = ""; // row 5b-1432
    ElectricalPhase phase = ElectricalPhase.SinglePhase;
    ElectricalPhaseConfiguration phaseConfig = ElectricalPhaseConfiguration.Undefined;
    if (phases == 1) phase = ElectricalPhase.SinglePhase;
    else if (phases == 3) phase = ElectricalPhase.ThreePhase;
    else refused.Add("phases must be 1 or 3 - how many phases the distribution system has is never guessed.");
    if (phases == 3)
    {
        if (conf == "wye" || conf == "star" || conf == "y") phaseConfig = ElectricalPhaseConfiguration.Wye;
        else if (conf == "delta") phaseConfig = ElectricalPhaseConfiguration.Delta;
        else refused.Add("A 3-phase distribution system is wye or delta - configuration was "
            + (conf.Length == 0 ? "not given" : "\"" + configuration + "\"") + ", and it is never guessed.");
    }
    else if (phases == 1 && conf.Length > 0 && conf != "none")
        findings.Add("A 1-phase distribution system has no wye or delta - \"" + configuration + "\" is not used.");
    if (phases == 1 && wires != 2 && wires != 3)
        refused.Add("A 1-phase distribution system has 2 or 3 wires - wires was " + (wires <= 0 ? "not given" : wires.ToString()) + ".");
    if (phases == 3 && wires != 3 && wires != 4)
        refused.Add("A 3-phase distribution system has 3 or 4 wires - wires was " + (wires <= 0 ? "not given" : wires.ToString()) + ".");

    // The voltages, found by value among the model's voltage types.
    Func<double, string, VoltageType> voltageOf = (v, role) =>
    {
        var hits = voltages.Where(t => sameVolts(t.ActualValue, v)).ToList();
        if (hits.Count == 1) return hits[0];
        if (hits.Count == 0)
            refused.Add("No voltage type of " + num(v) + " V for the " + role + " voltage. The model's voltage types are: "
                + (voltages.Count == 0 ? "none" : string.Join(", ", voltages.Select(voltageLine)))
                + ". Add a " + num(v) + " V voltage type first.");
        else
            refused.Add(hits.Count + " voltage types are " + num(v) + " V - " + string.Join(", ", hits.Select(t => quoted(t.Name)))
                + " - so which is the " + role + " voltage is not guessed.");
        return null;
    };
    var lineToLine = voltageOf(volts, "line-to-line");
    VoltageType lineToGround = lg > 0 ? voltageOf(lg, "line-to-ground") : null;

    var newName = wantName.Length > 0 ? wantName
        : (lg > 0 ? num(volts) + "/" + num(lg) : num(volts)) + " V " + phases + "-phase " + wires + "-wire";
    if (refused.Count == 0)
    {
        var taken = nameTaken(systems.Select(d => d.Name), newName);
        var same = systems.Where(d => d.ElectricalPhase == phase
            && (phase != ElectricalPhase.ThreePhase || d.ElectricalPhaseConfiguration == phaseConfig)
            && d.NumWires == wires
            && d.VoltageLineToLine != null && d.VoltageLineToLine.Id == lineToLine.Id
            && (lineToGround == null ? d.VoltageLineToGround == null
                : d.VoltageLineToGround != null && d.VoltageLineToGround.Id == lineToGround.Id)).ToList();
        if (taken != null)
            skipped.Add("A distribution system is already named " + quoted(taken) + " - left as it is, nothing added. Give another name to add one.");
        else if (wantName.Length == 0 && same.Count > 0)
            skipped.Add("A distribution system with the same phases, wires and voltages is already here: "
                + string.Join(", ", same.Select(systemLine)) + " - nothing added. Give a name to add a second one.");
        else
        {
            try
            {
                var made = settings.AddDistributionSysType(newName, phase, phaseConfig, wires, lineToLine, lineToGround);
                if (made != null)
                {
                    added++;
                    findings.Add("Added distribution system " + systemLine(made) + ".");
                    if (same.Count > 0)
                        findings.Add("Another distribution system has the same phases, wires and voltages: "
                            + string.Join(", ", same.Select(d => quoted(d.Name))) + ".");
                }
                else refused.Add("Revit added no distribution system named " + quoted(newName) + ".");
            }
            catch (Exception ex) { refused.Add("Revit would not add distribution system " + quoted(newName) + ": " + ex.Message); }
        }
    }
}

// ---- Read back -------------------------------------------------------------
if (before.Count > 0 || settings != null)
{
    readLists();
    if (isVoltage || isDistribution) after.AddRange(listNow());
}
findings.Insert(0, (added > 0 ? "Added 1 " : "Added nothing - ") + (isDistribution ? "distribution system" : "voltage type")
    + (added > 0 ? "" : (skipped.Count > 0 ? "; one already here, skipped" : refused.Count > 0 ? "; refused" : ""))
    + ". " + (isDistribution ? "Distribution systems" : "Voltage types") + ": " + before.Count + " before, " + after.Count + " after.");
findings.AddRange(skipped);
findings.AddRange(refused);
