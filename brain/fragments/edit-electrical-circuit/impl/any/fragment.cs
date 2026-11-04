// NOT STANDALONE. Assumes `doc`, `elements`, `edit`, `circuitNumber`,
// `circuitPanel` and `panel` are in scope; leaves `circuitIds`, `changed`,
// `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry,
// and the caller owns it.
//
// EDITS A CIRCUIT THAT ALREADY EXISTS - the four buttons of Revit's Electrical
// Circuits tab: Add to Circuit, Remove from Circuit, Select Panel and
// Disconnect Panel. CREATE_ELECTRICAL_CIRCUIT makes a new circuit; this one
// never makes one.
//
// WHICH CIRCUIT. Named by its circuit number, and by the panel it is on now
// where numbers repeat - circuit 3 is on every panel. Or the one circuit
// selected in Revit. Or, to take a device off or move a device's circuit, the
// one circuit that device is on. A circuit NAMED for a move or a disconnect is
// the only one changed: the selection is set aside and said so, because one
// left over in Revit would otherwise move circuits nobody named. A number matching on more than one panel
// refuses and names the panels; a device on two circuits (power and data)
// is skipped and both are named. Nothing is chosen for the modeller.
//
// `panel` IS THE PANEL THE SENTENCE NAMES. For "select panel" it is where the
// circuit goes. For the other three it can only be the panel the circuit is
// on NOW - "disconnect circuit 3 from DB-1" - so it narrows which circuit is
// meant and changes no panel. Said in a finding either way.
//
// TAKING EVERY DEVICE OFF A CIRCUIT IS REFUSED for that circuit. A circuit
// with nothing on it is one Revit may delete, and deleting a circuit is not
// an edit (Article 7). What Revit does to an emptied circuit is NOT known from
// the assemblies; it is a NEEDS-CHECKING item, and refused here whatever the
// answer turns out to be.
//
// EVERY CHANGE IS READ BACK from the circuit afterwards - its loads, or its
// panel - and `changed` counts what reads back changed, never what was asked.
//
// MEMBERSHIP IS READ FROM EACH CIRCUIT'S OWN Elements, never from
// MEPModel.ElectricalSystems (2020-2021, obsolete in 2021) or
// GetElectricalSystems() (2021 on): neither compiles on all eight releases.
// A circuit's Elements are its loads; the panel feeding it is not among them.

var circuitIds = new List<ElementId>();
var changed = 0;
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was changed; each branch sets it at most once.
string refused = null;
var findings = new List<string>();
var lines = new List<string>();
var problems = new List<string>();

Func<string, string> squash = text => new string((text ?? "").ToLowerInvariant()
    .Where(c => c != ' ' && c != '-' && c != '_').ToArray());
Func<string, bool> isNone = text =>
{
    var said = (text ?? "").Trim();
    return said.Length == 0 || string.Equals(said, "none", StringComparison.OrdinalIgnoreCase);
};
Func<string, string, bool> same = (a, b) =>
    string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

// The four buttons, and the words a modeller types for each.
var mode = squash(edit);
string action = null;
if (mode == "add" || mode == "addto" || mode == "addtocircuit") action = "add";
else if (mode == "remove" || mode == "removefrom" || mode == "removefromcircuit"
    || mode == "takeoff") action = "remove";
else if (mode == "selectpanel" || mode == "panel" || mode == "move" || mode == "movetopanel"
    || mode == "changepanel" || mode == "setpanel") action = "panel";
else if (mode == "disconnect" || mode == "disconnectpanel" || mode == "unassign") action = "disconnect";

Func<ElectricalSystem, string> numberOf = c => { try { return c.CircuitNumber ?? ""; } catch { return ""; } };
Func<ElectricalSystem, string> panelOf = c => { try { return c.PanelName ?? ""; } catch { return ""; } };
Func<ElectricalSystem, string> label = c =>
{
    var number = numberOf(c);
    var onPanel = panelOf(c);
    var load = "";
    try { load = c.LoadName ?? ""; } catch { }
    return "circuit " + (number.Length == 0 ? "(no number)" : number)
        + (onPanel.Length == 0 ? " on no panel" : " on " + onPanel)
        + (load.Length == 0 ? "" : " (" + load + ")");
};
Func<ElectricalSystem, List<ElementId>> loadsOf = c =>
{
    var ids = new List<ElementId>();
    try
    {
        if (c == null || !c.IsValidObject) return ids;
        // ElementSet is not generic, so the loop variable carries its type.
        foreach (Element load in c.Elements)
        {
            if (load != null && load.IsValidObject) ids.Add(load.Id);
        }
    }
    catch { }
    return ids;
};
Func<Element, string> nameOf = e =>
{
    var category = "";
    try { category = e.Category != null ? e.Category.Name : ""; } catch { }
    return (category.Length == 0 ? "element" : category) + " " + e.Id.ToString();
};

// The panel the request named, by its own Panel Name - what the panel
// schedule shows, and what a circuit's PanelName reads back.
string panelWord = null;
if (panel != null && panel.IsValidObject)
{
    try
    {
        var named = panel.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
        if (named != null) panelWord = named.AsString();
    }
    catch { }
    if (string.IsNullOrEmpty(panelWord)) panelWord = panel.Name;
}

// Every circuit in the model, once, and which circuits each load is on.
var all = new List<ElectricalSystem>();
foreach (ElectricalSystem circuit in new FilteredElementCollector(doc)
    .OfClass(typeof(ElectricalSystem)).Cast<ElectricalSystem>())
{
    if (circuit != null && circuit.IsValidObject) all.Add(circuit);
}
var onCircuits = new Dictionary<ElementId, List<ElectricalSystem>>();
foreach (var circuit in all)
{
    foreach (var id in loadsOf(circuit))
    {
        List<ElectricalSystem> carried;
        if (!onCircuits.TryGetValue(id, out carried))
        {
            carried = new List<ElectricalSystem>();
            onCircuits[id] = carried;
        }
        carried.Add(circuit);
    }
}

// The selection, split: circuits picked in Revit, and everything else.
var picked = new List<ElectricalSystem>();
var devices = new List<Element>();
if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;
        var asCircuit = element as ElectricalSystem;
        if (asCircuit != null)
        {
            if (!picked.Any(c => c.Id.Equals(asCircuit.Id))) picked.Add(asCircuit);
        }
        else if (!devices.Any(d => d.Id.Equals(element.Id)))
        {
            devices.Add(element);
        }
    }
}

// The panel the circuit is on NOW, when the request says one.
string onPanel = null;
if (!isNone(circuitPanel)) onPanel = circuitPanel.Trim();

if (action == null)
{
    refused = "\"" + edit + "\" is not an edit this understands, so NOTHING WAS CHANGED. Use add, "
        + "remove, select panel or disconnect - the four buttons of Revit's Electrical Circuits tab.";
}
else if (action == "panel" && panelWord == null)
{
    refused = "No panel was named to move the circuit to, so NOTHING WAS CHANGED. Name it by its "
        + "Panel Name - the one on the panel schedule.";
}
else if (action != "panel" && panelWord != null)
{
    if (onPanel != null && !same(onPanel, panelWord))
    {
        refused = "Two different panels were named - \"" + onPanel + "\" and \"" + panelWord + "\" - "
            + "for a " + action + ", which changes no panel, so NOTHING WAS CHANGED. Name the one "
            + "panel the circuit is on now.";
    }
    else
    {
        onPanel = panelWord;
        findings.Add("Panel \"" + panelWord + "\" was read as the panel the circuit is on now, to "
            + "tell which circuit is meant. No circuit was moved to or from it.");
    }
}

// A circuit named by number - and by its panel, where the number repeats.
ElectricalSystem namedCircuit = null;
if (refused == null && !isNone(circuitNumber))
{
    var number = circuitNumber.Trim();
    var hits = all.Where(c => same(numberOf(c), number)
        && (onPanel == null || same(panelOf(c), onPanel))).ToList();
    if (hits.Count == 1)
    {
        namedCircuit = hits[0];
    }
    else if (hits.Count == 0)
    {
        var elsewhere = all.Where(c => same(numberOf(c), number))
            .Select(c => panelOf(c).Length == 0 ? "no panel" : panelOf(c)).Distinct().ToList();
        refused = "No circuit numbered \"" + number + "\""
            + (onPanel == null ? "" : " on panel \"" + onPanel + "\"") + " in this model, so "
            + "NOTHING WAS CHANGED."
            + (elsewhere.Count == 0 ? "" : " A circuit with that number is on: "
                + string.Join(", ", elsewhere.Take(12)) + ".");
    }
    else
    {
        refused = hits.Count + " circuits are numbered \"" + number + "\" - on "
            + string.Join(", ", hits.Take(12).Select(c => panelOf(c).Length == 0 ? "no panel" : panelOf(c)))
            + " - so it does not say which one is meant, and NOTHING WAS CHANGED. Name the panel "
            + "it is on as well.";
    }
}
else if (refused == null && onPanel != null && namedCircuit == null && picked.Count == 0
    && devices.Count == 0)
{
    refused = "Only a panel was named. A panel alone names every circuit on it, and this edits "
        + "one circuit at a time, so NOTHING WAS CHANGED. Give the circuit number as well, or "
        + "select the circuit.";
}

// The circuit(s) a selected device is on, kept to the named panel if there is one.
Func<Element, List<ElectricalSystem>> circuitsOf = d =>
{
    List<ElectricalSystem> carried;
    if (!onCircuits.TryGetValue(d.Id, out carried)) return new List<ElectricalSystem>();
    return carried.Where(c => onPanel == null || same(panelOf(c), onPanel)).ToList();
};

if (refused == null && (action == "add" || action == "remove"))
{
    // ONE circuit to add to or take off: the named one, or the one selected.
    ElectricalSystem target = namedCircuit;
    if (picked.Count > 1)
    {
        refused = picked.Count + " circuits are selected - " + string.Join("; ", picked.Take(6).Select(label))
            + " - and " + (action == "add" ? "devices go onto one" : "devices come off one")
            + " at a time, so NOTHING WAS CHANGED. Select one circuit, or name it.";
    }
    else if (picked.Count == 1)
    {
        if (target != null && !target.Id.Equals(picked[0].Id))
        {
            refused = "The circuit named (" + label(target) + ") is not the circuit selected ("
                + label(picked[0]) + "), so NOTHING WAS CHANGED. Name one or select one.";
        }
        else if (onPanel != null && !same(panelOf(picked[0]), onPanel))
        {
            refused = "The circuit selected is " + label(picked[0]) + ", not one on \"" + onPanel
                + "\", so NOTHING WAS CHANGED.";
        }
        else
        {
            target = picked[0];
        }
    }

    if (refused == null && devices.Count == 0)
    {
        refused = "No devices were selected to " + (action == "add" ? "add to" : "take off")
            + " a circuit, so NOTHING WAS CHANGED. Select the lights, sockets or equipment first.";
    }
    else if (refused == null && action == "add" && target == null)
    {
        refused = "No circuit was named to add to, so NOTHING WAS CHANGED. Give its circuit number "
            + "(and its panel, where numbers repeat), or select the circuit with the devices.";
    }

    if (refused == null && action == "add")
    {
        var before = loadsOf(target);
        var asked = new List<Element>();
        var toAdd = new ElementSet();
        foreach (var device in devices)
        {
            if (before.Contains(device.Id))
            {
                findings.Add(nameOf(device) + " is already on " + label(target) + ", so it was left.");
                continue;
            }
            List<ElectricalSystem> carried;
            if (onCircuits.TryGetValue(device.Id, out carried) && carried.Count > 0)
            {
                findings.Add(nameOf(device) + " was on " + string.Join("; ", carried.Select(label))
                    + " before this.");
            }
            toAdd.Insert(device);
            asked.Add(device);
        }

        if (asked.Count == 0)
        {
            refused = "Every device selected is already on " + label(target) + ", so NOTHING WAS CHANGED.";
        }
        else
        {
            var targetLabel = label(target);
            string revitSaid = null;
            var answered = false;
            try { answered = target.AddToCircuit(toAdd); }
            catch (Exception ex) { revitSaid = ex.Message; }

            // READ BACK, whatever Revit answered - an exception does not prove
            // nothing changed, and a true does not prove everything did.
            var after = loadsOf(target);
            var joined = asked.Where(d => after.Contains(d.Id)).ToList();
            changed = joined.Count;

            if (changed == 0)
            {
                refused = "Revit added none of the " + asked.Count + " device(s) to " + targetLabel
                    + (revitSaid != null ? ": " + revitSaid : " and gave no reason")
                    + ". NOTHING WAS CHANGED. Devices whose connectors do not suit the circuit's "
                    + "system type, or whose voltage or poles differ from it, are the usual reasons.";
            }
            else
            {
                circuitIds.Add(target.Id);
                lines.Add(targetLabel + ": " + before.Count + " device(s) before, " + after.Count
                    + " after - " + changed + " added.");
                if (changed < asked.Count)
                {
                    findings.Add("NOT EVERY DEVICE JOINED: " + asked.Count + " were asked and " + changed
                        + " are on it. Still off it: " + string.Join(", ", asked
                            .Where(d => !after.Contains(d.Id)).Take(12).Select(nameOf))
                        + (revitSaid != null ? ". Revit said: " + revitSaid : "") + ".");
                }
                else if (!answered && revitSaid == null)
                {
                    findings.Add("Revit answered that the add did not succeed, and yet every device "
                        + "reads back on the circuit - the read-back is what is reported.");
                }
                // A device that was on another circuit: say where it is now,
                // read back rather than assumed.
                foreach (var device in joined)
                {
                    List<ElectricalSystem> carried;
                    if (!onCircuits.TryGetValue(device.Id, out carried)) continue;
                    foreach (var earlier in carried)
                    {
                        if (earlier.Id.Equals(target.Id)) continue;
                        findings.Add(nameOf(device) + (loadsOf(earlier).Contains(device.Id)
                            ? " is STILL ON " + label(earlier) + " as well."
                            : " is no longer on " + label(earlier) + "."));
                    }
                }
            }
        }
    }
    else if (refused == null && action == "remove")
    {
        var groups = new Dictionary<ElementId, List<Element>>();
        var byId = new Dictionary<ElementId, ElectricalSystem>();
        foreach (var device in devices)
        {
            ElectricalSystem from = null;
            if (target != null)
            {
                if (!loadsOf(target).Contains(device.Id))
                {
                    problems.Add(nameOf(device) + " is not on " + label(target) + ", so it was left.");
                    continue;
                }
                from = target;
            }
            else
            {
                var carried = circuitsOf(device);
                if (carried.Count == 0)
                {
                    problems.Add(nameOf(device) + " is on no circuit"
                        + (onPanel == null ? "" : " fed from \"" + onPanel + "\"") + ", so it was left.");
                    continue;
                }
                if (carried.Count > 1)
                {
                    problems.Add(nameOf(device) + " is on " + carried.Count + " circuits - "
                        + string.Join("; ", carried.Select(label)) + " - so it was left. Name the "
                        + "circuit to take it off.");
                    continue;
                }
                from = carried[0];
            }
            List<Element> group;
            if (!groups.TryGetValue(from.Id, out group))
            {
                group = new List<Element>();
                groups[from.Id] = group;
                byId[from.Id] = from;
            }
            group.Add(device);
        }

        foreach (var pair in groups)
        {
            var circuit = byId[pair.Key];
            var circuitLabel = label(circuit);
            var before = loadsOf(circuit);
            if (pair.Value.Count >= before.Count)
            {
                problems.Add("Taking all " + before.Count + " device(s) off " + circuitLabel + " would "
                    + "leave it with nothing on it, and Revit may delete an empty circuit - deleting a "
                    + "circuit is not an edit. It was left as it is. Delete the circuit by hand if "
                    + "that is what is meant.");
                continue;
            }
            var toRemove = new ElementSet();
            foreach (var device in pair.Value) toRemove.Insert(device);
            string revitSaid = null;
            try { circuit.RemoveFromCircuit(toRemove); }
            catch (Exception ex) { revitSaid = ex.Message; }

            var after = loadsOf(circuit);
            var left = pair.Value.Where(d => !after.Contains(d.Id)).ToList();
            if (left.Count == 0)
            {
                problems.Add("Revit took none of " + pair.Value.Count + " device(s) off " + circuitLabel
                    + (revitSaid != null ? ": " + revitSaid : " and gave no reason") + ".");
                continue;
            }
            changed += left.Count;
            circuitIds.Add(circuit.Id);
            lines.Add(circuitLabel + ": " + before.Count + " device(s) before, " + after.Count
                + " after - " + left.Count + " taken off: " + string.Join(", ", left.Take(12).Select(nameOf)) + ".");
            if (left.Count < pair.Value.Count)
            {
                problems.Add("NOT EVERY DEVICE CAME OFF " + circuitLabel + ": " + (pair.Value.Count - left.Count)
                    + " still on it" + (revitSaid != null ? ". Revit said: " + revitSaid : "") + ".");
            }
        }
    }
}
else if (refused == null && (action == "panel" || action == "disconnect"))
{
    // The circuits to move or disconnect: the named one ALONE when one is
    // named - a selection left over in Revit must not add circuits nobody
    // named - or else the selected ones, and the one circuit each selected
    // device is on.
    var targets = new List<ElectricalSystem>();
    if (namedCircuit != null)
    {
        targets.Add(namedCircuit);
        if (picked.Count + devices.Count > 0)
        {
            findings.Add("A circuit was named, so only " + label(namedCircuit) + " was "
                + (action == "panel" ? "moved" : "disconnected") + ". The " + (picked.Count + devices.Count)
                + " element(s) selected in Revit were not used.");
        }
        picked.Clear();
        devices.Clear();
    }
    foreach (var circuit in picked)
    {
        if (onPanel != null && !same(panelOf(circuit), onPanel))
        {
            problems.Add(label(circuit) + " is selected but is not on \"" + onPanel + "\", so it was left.");
            continue;
        }
        if (!targets.Any(t => t.Id.Equals(circuit.Id))) targets.Add(circuit);
    }
    foreach (var device in devices)
    {
        var carried = circuitsOf(device);
        if (carried.Count == 1)
        {
            if (!targets.Any(t => t.Id.Equals(carried[0].Id))) targets.Add(carried[0]);
        }
        else if (carried.Count == 0)
        {
            problems.Add(nameOf(device) + " is on no circuit"
                + (onPanel == null ? "" : " fed from \"" + onPanel + "\"") + ", so it gave none to change.");
        }
        else
        {
            problems.Add(nameOf(device) + " is on " + carried.Count + " circuits - "
                + string.Join("; ", carried.Select(label)) + " - so neither was changed. Select the "
                + "circuit, or name it.");
        }
    }

    if (targets.Count == 0)
    {
        refused = "No circuit was named or selected to " + (action == "panel" ? "move" : "disconnect")
            + ", so NOTHING WAS CHANGED. Give its circuit number (and the panel it is on, where numbers "
            + "repeat), or select the circuit."
            + (problems.Count == 0 ? "" : " " + string.Join(" ", problems));
        problems.Clear();
    }

    foreach (var circuit in targets)
    {
        var circuitLabel = label(circuit);
        var was = panelOf(circuit);
        string revitSaid = null;
        if (action == "panel")
        {
            if (same(was, panelWord))
            {
                problems.Add(circuitLabel + " is already on " + panelWord + ", so it was left.");
                continue;
            }
            try { circuit.SelectPanel(panel); }
            catch (Exception ex) { revitSaid = ex.Message; }
            var now = panelOf(circuit);
            if (same(now, panelWord))
            {
                changed++;
                circuitIds.Add(circuit.Id);
                lines.Add(circuitLabel + ": moved from " + (was.Length == 0 ? "no panel" : was) + " to "
                    + now + ", where it is circuit " + (numberOf(circuit).Length == 0 ? "(no number)"
                        : numberOf(circuit)) + ".");
            }
            else
            {
                problems.Add("Revit did not move " + circuitLabel + " to " + panelWord
                    + (revitSaid != null ? ": " + revitSaid : "") + ". It reads back on "
                    + (now.Length == 0 ? "no panel" : now) + ". A panel with no free slot, or whose "
                    + "voltage or distribution system does not suit the circuit, is the usual reason.");
            }
        }
        else
        {
            if (was.Length == 0)
            {
                problems.Add(circuitLabel + " is already on no panel, so it was left.");
                continue;
            }
            try { circuit.DisconnectPanel(); }
            catch (Exception ex) { revitSaid = ex.Message; }
            var now = panelOf(circuit);
            if (now.Length == 0)
            {
                changed++;
                circuitIds.Add(circuit.Id);
                lines.Add(circuitLabel + ": taken off " + was + ". IT IS ON NO PANEL NOW, and appears on "
                    + "no panel schedule.");
            }
            else
            {
                problems.Add("Revit did not disconnect " + circuitLabel
                    + (revitSaid != null ? ": " + revitSaid : "") + ". It reads back on " + now + ".");
            }
        }
    }
}

if (refused == null && changed == 0)
{
    refused = "Nothing was changed. " + (problems.Count == 0 ? "" : string.Join(" ", problems));
    problems.Clear();
}
findings.AddRange(problems);
report = lines.Count == 0 ? "Nothing was changed." : string.Join("\n", lines);
if (refused != null) findings.Add(refused);
