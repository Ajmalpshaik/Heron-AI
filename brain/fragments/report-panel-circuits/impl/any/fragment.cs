// NOT STANDALONE. Assumes `doc` and `panelName` are in scope; leaves
// `schedule`, `panelCount`, `circuitCount`, `unassigned` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A PANEL SCHEDULE AS TEXT, READ LIVE FROM THE CIRCUITS. Every electrical
// circuit in the model is read once and grouped under the panel it is fed
// from. For each: its number, load name, rating, poles, apparent load,
// voltage and how many devices are on it - and for each panel, Revit's own
// connected-load and demand totals.
//
// EVERY VALUE IS REVIT'S OWN FORMATTED TEXT. Loads, ratings and voltages are
// read with AsValueString, so they come in the project's own units with the
// project's own symbol - VA or kVA, A, V - exactly as the panel schedule would
// print them. Electrical values are not in SI inside Revit, and a number
// converted here by hand is a number nobody can check against the drawing.
//
// A CIRCUIT ON NO PANEL IS THE FINDING, not noise. It appears on no panel
// schedule anywhere, so it is listed by id, apart from the panels.

var unassigned = new List<ElementId>();
var findings = new List<string>();
var wanted = (panelName ?? "").Trim();

Func<Element, BuiltInParameter, string> shown = (e, which) =>
{
    try
    {
        var p = e.get_Parameter(which);
        if (p == null || !p.HasValue) return "";
        var text = p.AsValueString();
        if (string.IsNullOrEmpty(text) && p.StorageType == StorageType.String) text = p.AsString();
        return text ?? "";
    }
    catch { return ""; }
};

// Panel id -> its circuits, kept in the order of their start slot.
var byPanel = new Dictionary<ElementId, List<ElectricalSystem>>();
var panelOrder = new List<ElementId>();
var circuitCount = 0;

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ElectricalSystem)))
{
    var circuit = element as ElectricalSystem;
    if (circuit == null) continue;

    FamilyInstance panel = null;
    try { panel = circuit.BaseEquipment; } catch { }

    if (panel == null)
    {
        if (wanted == "") unassigned.Add(circuit.Id);
        continue;
    }

    var name = "";
    try { name = circuit.PanelName ?? ""; } catch { }
    if (wanted != "" && !string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)) continue;

    List<ElectricalSystem> list;
    if (!byPanel.TryGetValue(panel.Id, out list))
    {
        list = new List<ElectricalSystem>();
        byPanel[panel.Id] = list;
        panelOrder.Add(panel.Id);
    }
    list.Add(circuit);
    circuitCount++;
}

var rows = new List<string>();
foreach (var panelId in panelOrder)
{
    var panel = doc.GetElement(panelId);
    var circuits = byPanel[panelId];
    circuits.Sort((a, b) =>
    {
        int sa = 0, sb = 0;
        try { sa = a.StartSlot; } catch { }
        try { sb = b.StartSlot; } catch { }
        return sa.CompareTo(sb);
    });

    var panelLabel = shown(panel, BuiltInParameter.RBS_ELEC_PANEL_NAME);
    if (panelLabel == "") panelLabel = "panel " + panelId;
    var connected = shown(panel, BuiltInParameter.RBS_ELEC_PANEL_TOTALLOAD_PARAM);
    var demand = shown(panel, BuiltInParameter.RBS_ELEC_PANEL_TOTALESTLOAD_PARAM);
    var current = shown(panel, BuiltInParameter.RBS_ELEC_PANEL_TOTAL_DEMAND_CURRENT_PARAM);

    rows.Add("PANEL " + panelLabel + " - " + circuits.Count + " circuit(s)"
        + (connected == "" ? "" : ", connected load " + connected)
        + (demand == "" ? "" : ", demand load " + demand)
        + (current == "" ? "" : ", demand current " + current));

    foreach (var circuit in circuits)
    {
        var number = "";
        var load = "";
        var poles = "";
        var kind = "";
        var devices = 0;
        try { number = circuit.CircuitNumber ?? ""; } catch { }
        try { load = circuit.LoadName ?? ""; } catch { }
        try { poles = circuit.PolesNumber.ToString(); } catch { }
        try { kind = circuit.SystemType.ToString(); } catch { }
        try { foreach (Element member in circuit.Elements) devices++; } catch { }

        rows.Add("  " + (number == "" ? "(no number)" : number)
            + " | " + (load == "" ? "(no load name)" : load)
            + " | " + shown(circuit, BuiltInParameter.RBS_ELEC_CIRCUIT_RATING_PARAM)
            + " | " + poles + " pole(s)"
            + " | " + shown(circuit, BuiltInParameter.RBS_ELEC_APPARENT_LOAD)
            + " | " + shown(circuit, BuiltInParameter.RBS_ELEC_VOLTAGE)
            + " | " + devices + " device(s)"
            + " | " + kind);
    }
}

if (unassigned.Count > 0)
    rows.Add("ON NO PANEL - " + unassigned.Count + " circuit(s), on no panel schedule anywhere: "
        + string.Join(", ", unassigned.Select(id => id.ToString())));

var schedule = string.Join("\n", rows);
var panelCount = panelOrder.Count;

if (wanted != "" && panelCount == 0)
    findings.Add("No circuit is fed from a panel named \"" + wanted + "\". The panel may have no "
        + "circuits yet, or its Panel Name differs - this reads the Panel Name parameter.");
else
    findings.Add(panelCount + " panel(s), " + circuitCount + " circuit(s) on them"
        + (wanted == "" ? ", " + unassigned.Count + " circuit(s) on no panel." : "."));
