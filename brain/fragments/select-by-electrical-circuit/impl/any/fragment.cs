// NOT STANDALONE. Assumes `doc`, `circuitKind` and `circuitName` are in scope;
// leaves `elements`, `panels` and `findings` behind.
//
// A CIRCUIT'S Elements ARE ITS LOADS. THE PANEL IS NOT AMONG THEM. Measured: a
// circuit plainly involving a receptacle and a panelboard reported ONE element.
// Revit is right - everything downstream of the panel is on the circuit, the
// panel FEEDS it - and it is very easy to read as a broken filter. So the
// panels are reported separately rather than left as an apparent omission.
//
// SEPARATE FROM SELECT_BY_MEP_SYSTEM ON PURPOSE. A duct or pipe system type is
// a document element with a name a project renames; a circuit's type is a fixed
// enumeration in the API. Same words from a modeller, genuinely different reads.
//
// THE KIND IS MATCHED AS TEXT against the enumeration's own name, so "power"
// finds PowerCircuit. The alternative is asking a modeller to know the API's
// spelling.

var elements = new List<Element>();
var panels = new List<string>();
var findings = new List<string>();

var wantKind = string.IsNullOrEmpty(circuitKind) ? "" : circuitKind.Trim();
var wantName = string.IsNullOrEmpty(circuitName) ? "" : circuitName.Trim();

if (wantKind.Length == 0 && wantName.Length == 0)
{
    findings.Add("Name a circuit kind - power, lighting, data - or a circuit name, or both. Every "
        + "circuit in the model is a legitimate answer to neither, and almost never the question");
}
else
{
    var matched = 0;
    var seen = new List<ElementId>();

    foreach (var circuit in new FilteredElementCollector(doc)
        .OfClass(typeof(ElectricalSystem)).Cast<ElectricalSystem>())
    {
        var kindText = "";
        try { kindText = circuit.SystemType.ToString(); }
        catch { kindText = ""; }

        if (wantKind.Length > 0 && kindText.IndexOf(wantKind, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

        var name = circuit.Name ?? "";
        if (wantName.Length > 0 && name.IndexOf(wantName, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

        matched++;

        try
        {
            var panelName = circuit.PanelName;
            if (!string.IsNullOrEmpty(panelName) && !panels.Contains(panelName)) panels.Add(panelName);
        }
        catch
        {
            // An unassigned circuit has no panel. That is a real state, not a failure.
        }

        foreach (Element load in circuit.Elements)
        {
            if (load == null) continue;

            var already = false;
            foreach (var known in seen)
            {
                if (known == load.Id) { already = true; break; }
            }
            if (already) continue;

            seen.Add(load.Id);
            elements.Add(load);
        }
    }

    var asked = new List<string>();
    if (wantKind.Length > 0) asked.Add(string.Format("kind containing '{0}'", wantKind));
    if (wantName.Length > 0) asked.Add(string.Format("name containing '{0}'", wantName));

    findings.Add(string.Format("{0} element(s) across {1} circuit(s) with {2}",
        elements.Count, matched, string.Join(" and ", asked.ToArray())));

    if (matched > 0)
        findings.Add(string.Format("These are the LOADS. The panel feeding a circuit is NOT one of "
            + "its elements, so a count that looks one short usually is not: the panel(s) here are {0}",
            panels.Count == 0 ? "none - these circuits have no panel assigned" : string.Join(", ", panels.ToArray())));

    if (matched == 0)
        findings.Add("No circuit matched. A model with no electrical work at all gives the same "
            + "answer, so check that circuits exist before reading this as a filter problem");
}
