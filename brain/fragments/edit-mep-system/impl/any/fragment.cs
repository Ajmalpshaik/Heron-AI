// NOT STANDALONE. Assumes `doc`, `elements`, `edit`, `systemName`,
// `circuitNumber`, `circuitPanel` and `panel` are in scope; leaves `systemIds`,
// `changed`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry,
// and the caller owns it.
//
// EDITS A SYSTEM THAT ALREADY EXISTS - a duct or piping system with Revit's
// Add to System and Remove from System, and (VERSION 2) an electrical circuit
// with the four buttons of the Electrical Circuits tab: Add to Circuit, Remove
// from Circuit, Select Panel and Disconnect Panel. CREATE_MEP_SYSTEM makes a
// new system or circuit; this one never makes one and never deletes one.
//
// VERSION 2 (2026-11-04, FRAGMENT-ISSUES row 5b-1382, on the owner's yes):
// EDIT_ELECTRICAL_CIRCUIT (FRG-MEP-070) was merged into this card and is
// DEPRECATED. Its circuit half is below, marked CIRCUITS, and carries its
// rules: a circuit named by number and by the panel it is on now where numbers
// repeat, or the one selected, or the one a device is on; a circuit NAMED for
// a move or a disconnect is the only one changed; `panel` is where Select
// Panel moves a circuit, and for the other three only narrows which circuit is
// meant; taking every device off a circuit is refused; every change read back.
// The duct and piping half is version 1 to the word, marked DUCT AND PIPING.
//
// WHICH HALF IS NEVER GUESSED. A circuit number, the panel a circuit is on, a
// panel, a selected circuit, or Select Panel / Disconnect Panel mean a circuit;
// a system name or a selected duct or piping system means a duct or piping
// system. Both at once refuses. Neither, to take elements out, means the one
// kind of system they are on; elements on circuits AND on duct or piping
// systems refuse, naming them.
//
// THE OPPOSITE RULE, AND THE SAFER ONE KEPT: AN ADD NEVER MOVES AN ELEMENT
// FROM ONE SYSTEM TO ANOTHER. Version 1 skipped an element already on another
// duct or piping system and named that system; EDIT_ELECTRICAL_CIRCUIT added
// a device already on another circuit and said afterwards where it had been.
// Here a device already on another circuit OF THE SAME KIND (power on power,
// data on data) is skipped and that circuit named - take it off first, then
// add it. A device on a circuit of another kind - a socket's data circuit
// while it is added to power - is not moved by joining a second, and it is
// added, said in findings. If Revit takes it off an earlier circuit anyway,
// the read-back says so in capitals; nothing hides it.
//
// WHICH SYSTEM. Named as the System Browser shows it - "Supply Air 2",
// "Domestic Cold Water 1" - or the one duct or piping system selected in Revit.
// To take an element out, it can also be the one system that element is on.
// A name matching no system exactly is looked for as a whole word of a system
// name ("FP" in "FP 1") or as a system type's name or abbreviation; only ONE
// system answering is taken, and how it was found is said. Two or more answer:
// refused, naming them. An element on two systems (a fan coil on supply and
// return, a basin on hot and cold water) is skipped and both are named. Nothing
// is chosen for the modeller.
//
// WHAT JOINS. A terminal, fixture, sprinkler or piece of equipment joins
// through one free connector of the system's domain and classification - the
// rule CREATE_MEP_SYSTEM proved. One already on ANOTHER system is skipped and
// that system named: Revit refuses a connector already in use, and moving an
// element from one system to another without being asked is not an add.
// Ducts, pipes, fittings and accessories are on a system through what they
// connect to; they are named and left, with what does change them.
//
// TAKING EVERY TERMINAL OUT OF A SYSTEM, OR EVERY DEVICE OFF A CIRCUIT, IS
// REFUSED for that system. Revit's own documentation forbids the first on
// every release 2020 to 2027; a circuit with nothing on it is one Revit may
// delete; and a system left empty is one to delete, which is not an edit
// (Article 7). The base equipment is not taken out here either - Revit says
// the element joined to it cannot be removed, and changing it is a different
// button.
//
// EVERY CHANGE IS READ BACK off the system afterwards - its own Elements and
// BaseEquipment, a circuit's loads or its panel - and `changed` counts what
// reads back changed, never what was asked. Revit refuses a whole set for one
// wrong connector, so a refused duct or piping set is tried again one element
// at a time to name the one it would not take.
//
// CIRCUIT MEMBERSHIP IS READ FROM EACH CIRCUIT'S OWN Elements, never from
// MEPModel.ElectricalSystems (2020-2021, obsolete in 2021) or
// GetElectricalSystems() (2021 on): neither compiles on all eight releases.
// A circuit's Elements are its loads; the panel feeding it is not among them.

var systemIds = new List<ElementId>();
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

// The buttons, and the words a modeller types for each. Add and Remove are on
// both tabs; Select Panel and Disconnect Panel are a circuit's only.
var mode = squash(edit);
string action = null;
if (mode == "add" || mode == "addto" || mode == "addtosystem" || mode == "addtocircuit"
    || mode == "puton") action = "add";
else if (mode == "remove" || mode == "removefrom" || mode == "removefromsystem"
    || mode == "removefromcircuit" || mode == "takeout" || mode == "takeoff") action = "remove";
else if (mode == "selectpanel" || mode == "panel" || mode == "move" || mode == "movetopanel"
    || mode == "changepanel" || mode == "setpanel") action = "panel";
else if (mode == "disconnect" || mode == "disconnectpanel" || mode == "unassign") action = "disconnect";

Func<Element, string> nameOf = e =>
{
    var category = "";
    try { category = e.Category != null ? e.Category.Name : ""; } catch { }
    var mark = "";
    try
    {
        var p = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        if (p != null) mark = p.AsString() ?? "";
    }
    catch { }
    return (category.Length == 0 ? "element" : category) + " " + e.Id.ToString()
        + (mark.Length == 0 ? "" : " (Mark " + mark + ")");
};
// The members of any system as Revit lists them: terminals and equipment, or
// a circuit's loads - never base equipment, a panel, ducts or pipes.
Func<MEPSystem, List<ElementId>> membersOf = s =>
{
    var ids = new List<ElementId>();
    try
    {
        if (s == null || !s.IsValidObject) return ids;
        // ElementSet is not generic, so the loop variable carries its type.
        foreach (Element member in s.Elements)
        {
            if (member != null && member.IsValidObject) ids.Add(member.Id);
        }
    }
    catch { }
    return ids;
};
Func<MEPSystem, ElementId> baseOf = s =>
{
    try { return s.BaseEquipment != null ? s.BaseEquipment.Id : ElementId.InvalidElementId; }
    catch { return ElementId.InvalidElementId; }
};

// Every duct and piping system, and every circuit, in the model, once - and
// which of each every member is on.
var ductAndPipeSystems = new List<MEPSystem>();
foreach (MechanicalSystem s in new FilteredElementCollector(doc)
    .OfClass(typeof(MechanicalSystem)).WhereElementIsNotElementType().Cast<MechanicalSystem>())
{
    if (s != null && s.IsValidObject) ductAndPipeSystems.Add(s);
}
foreach (PipingSystem s in new FilteredElementCollector(doc)
    .OfClass(typeof(PipingSystem)).WhereElementIsNotElementType().Cast<PipingSystem>())
{
    if (s != null && s.IsValidObject) ductAndPipeSystems.Add(s);
}
var onSystems = new Dictionary<ElementId, List<MEPSystem>>();
foreach (var s in ductAndPipeSystems)
{
    foreach (var id in membersOf(s))
    {
        List<MEPSystem> carried;
        if (!onSystems.TryGetValue(id, out carried))
        {
            carried = new List<MEPSystem>();
            onSystems[id] = carried;
        }
        carried.Add(s);
    }
}
var allCircuits = new List<ElectricalSystem>();
foreach (ElectricalSystem circuit in new FilteredElementCollector(doc)
    .OfClass(typeof(ElectricalSystem)).Cast<ElectricalSystem>())
{
    if (circuit != null && circuit.IsValidObject) allCircuits.Add(circuit);
}
var onCircuits = new Dictionary<ElementId, List<ElectricalSystem>>();
foreach (var circuit in allCircuits)
{
    foreach (var id in membersOf(circuit))
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

// The selection, split three ways: duct and piping systems picked in Revit,
// circuits picked in Revit, and the rest.
var pickedSystems = new List<MEPSystem>();
var pickedCircuits = new List<ElectricalSystem>();
var devices = new List<Element>();
if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;
        if (element is MechanicalSystem || element is PipingSystem)
        {
            var asSystem = (MEPSystem)element;
            if (!pickedSystems.Any(s => s.Id.Equals(asSystem.Id))) pickedSystems.Add(asSystem);
        }
        else if (element is ElectricalSystem)
        {
            var asCircuit = (ElectricalSystem)element;
            if (!pickedCircuits.Any(c => c.Id.Equals(asCircuit.Id))) pickedCircuits.Add(asCircuit);
        }
        else if (element is MEPSystem)
        {
            findings.Add(nameOf(element) + " is a system of a kind this does not edit - not a duct or "
                + "piping system and not a circuit - so it was not used.");
        }
        else if (!devices.Any(d => d.Id.Equals(element.Id)))
        {
            devices.Add(element);
        }
    }
}

// WHICH HALF - from what was said and what was selected, never guessed.
var circuitSaid = !isNone(circuitNumber) || !isNone(circuitPanel) || panel != null
    || pickedCircuits.Count > 0 || action == "panel" || action == "disconnect";
var systemSaid = !isNone(systemName) || pickedSystems.Count > 0;
string half = null;
if (action == null)
{
    refused = "\"" + edit + "\" is not an edit this understands, so NOTHING WAS CHANGED. Use add or "
        + "remove - Revit's Add to System and Remove from System, or Add to Circuit and Remove from "
        + "Circuit - or, for a circuit, select panel or disconnect.";
}
else if (circuitSaid && systemSaid)
{
    refused = "Both a circuit and a duct or piping system were named or selected, and this edits one "
        + "at a time, so NOTHING WAS CHANGED. Name or select one.";
}
else if (circuitSaid)
{
    half = "circuit";
}
else if (systemSaid)
{
    half = "system";
}
else if (devices.Count == 0)
{
    refused = "Nothing was selected to " + (action == "add" ? "add" : "take out") + ", and no system or "
        + "circuit was named, so NOTHING WAS CHANGED. Select the terminals, fixtures, lights or devices "
        + "first.";
}
else if (action == "add")
{
    refused = "No system or circuit was named to add to, so NOTHING WAS CHANGED. Name the system as the "
        + "System Browser shows it - Supply Air 2 - or give the circuit's number (and its panel, where "
        + "numbers repeat), or select the system or circuit with the elements.";
}
else
{
    // To take out, with nothing named: the one kind of system they are on.
    var onBoth = devices.Where(d => onCircuits.ContainsKey(d.Id) && onSystems.ContainsKey(d.Id)).ToList();
    var onCircuit = devices.Where(d => onCircuits.ContainsKey(d.Id)).ToList();
    var onSystem = devices.Where(d => onSystems.ContainsKey(d.Id)).ToList();
    if (onBoth.Count > 0 || (onCircuit.Count > 0 && onSystem.Count > 0))
    {
        refused = "Some of the elements selected are on circuits and some on duct or piping systems"
            + (onBoth.Count == 0 ? "" : " - " + string.Join(", ", onBoth.Take(12).Select(nameOf))
                + " on both")
            + " - so it does not say which to take them out of, and NOTHING WAS CHANGED. Name the circuit "
            + "or the system.";
    }
    else if (onCircuit.Count > 0)
    {
        half = "circuit";
    }
    else
    {
        // On no circuit: the duct and piping half names each one and why -
        // base equipment, or on no system at all.
        half = "system";
    }
}

if (refused == null && half == "system")
{
    // ===================== DUCT AND PIPING - version 1 =====================
    var all = ductAndPipeSystems;
    var picked = pickedSystems;

    Func<MEPSystem, string> nameOfSystem = s => { try { return s.Name ?? ""; } catch { return ""; } };
    Func<MEPSystem, MEPSystemType> typeOf = s =>
    {
        try { return doc.GetElement(s.GetTypeId()) as MEPSystemType; } catch { return null; }
    };
    Func<MEPSystem, string> typeName = s =>
    {
        var t = typeOf(s);
        try { return t == null ? "" : t.Name ?? ""; } catch { return ""; }
    };
    Func<MEPSystem, string> typeAbbreviation = s =>
    {
        var t = typeOf(s);
        try { return t == null ? "" : t.Abbreviation ?? ""; } catch { return ""; }
    };
    Func<MEPSystem, string> label = s =>
    {
        var name = nameOfSystem(s);
        var kind = typeName(s);
        return "system \"" + (name.Length == 0 ? s.Id.ToString() : name) + "\""
            + (kind.Length == 0 ? "" : " (" + kind + ")");
    };

    // Ducts, pipes, flex, fittings and accessories: on a system through what
    // they connect to, so never added to one or taken out of one here.
    var runCategories = new List<ElementId>
    {
        new ElementId(BuiltInCategory.OST_DuctCurves), new ElementId(BuiltInCategory.OST_PipeCurves),
        new ElementId(BuiltInCategory.OST_FlexDuctCurves), new ElementId(BuiltInCategory.OST_FlexPipeCurves),
        new ElementId(BuiltInCategory.OST_DuctFitting), new ElementId(BuiltInCategory.OST_PipeFitting),
        new ElementId(BuiltInCategory.OST_DuctAccessory), new ElementId(BuiltInCategory.OST_PipeAccessory)
    };
    Func<Element, bool> isRun = e =>
    {
        if (e is MEPCurve) return true;
        try { return e.Category != null && runCategories.Contains(e.Category.Id); } catch { return false; }
    };
    Func<Element, List<Connector>> connectorsOf = e =>
    {
        var found = new List<Connector>();
        var instance = e as FamilyInstance;
        if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null)
            return found;
        foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
        {
            if (c != null && c.ConnectorType == ConnectorType.End) found.Add(c);
        }
        return found;
    };

    // A system named - exactly as the System Browser shows it, or else the one
    // system a whole word of whose name it is, or whose system type it names.
    MEPSystem named = null;
    if (!isNone(systemName))
    {
        var word = systemName.Trim();
        var hits = all.Where(s => same(nameOfSystem(s), word)).ToList();
        var exact = hits.Count > 0;
        if (!exact)
        {
            var token = word.ToLowerInvariant();
            hits = all.Where(s => nameOfSystem(s).ToLowerInvariant()
                    .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries).Contains(token)
                || same(typeName(s), word) || same(typeAbbreviation(s), word)).ToList();
        }
        if (hits.Count == 1)
        {
            named = hits[0];
            if (!exact)
            {
                findings.Add("No system is named \"" + word + "\" exactly. It was read as " + label(named)
                    + " - the one duct or piping system whose name holds that word, or whose system type "
                    + "is named or abbreviated so.");
            }
        }
        else if (hits.Count == 0)
        {
            refused = "No duct or piping system answers to \"" + word + "\" in this model, so NOTHING WAS "
                + "CHANGED." + (all.Count == 0 ? " The model has no duct or piping systems."
                    : " Systems here include: " + string.Join(", ", all.Take(12).Select(nameOfSystem)) + ".");
        }
        else
        {
            refused = hits.Count + " systems answer to \"" + word + "\" - "
                + string.Join("; ", hits.Take(12).Select(label)) + " - so it does not say which one is "
                + "meant, and NOTHING WAS CHANGED. Name the system as the System Browser shows it.";
        }
    }

    // ONE system to add to, or to take out of: the named one, or the one selected.
    MEPSystem target = named;
    if (refused == null)
    {
        if (picked.Count > 1)
        {
            refused = picked.Count + " systems are selected - " + string.Join("; ", picked.Take(6).Select(label))
                + " - and elements go onto or come out of one at a time, so NOTHING WAS CHANGED. Select one "
                + "system, or name it.";
        }
        else if (picked.Count == 1)
        {
            if (target != null && !target.Id.Equals(picked[0].Id))
            {
                refused = "The system named (" + label(target) + ") is not the system selected ("
                    + label(picked[0]) + "), so NOTHING WAS CHANGED. Name one or select one.";
            }
            else
            {
                target = picked[0];
            }
        }
    }

    if (refused == null && devices.Count == 0)
    {
        refused = "No terminals, fixtures, sprinklers or equipment were selected to "
            + (action == "add" ? "add to" : "take out of") + " a system, so NOTHING WAS CHANGED. Select "
            + "them first.";
    }
    else if (refused == null && action == "add" && target == null)
    {
        refused = "No system was named to add to, so NOTHING WAS CHANGED. Name it as the System Browser "
            + "shows it - Supply Air 2 - or select the system with the elements.";
    }

    Func<Element, string> runReason = e => nameOf(e) + " was left: a duct, pipe, fitting or accessory is "
        + "on a system through what it is connected to, not added to it or taken out of it. Disconnect it "
        + "from the run, or give it another system type.";

    if (refused == null && action == "add")
    {
        var isDuctSystem = target is MechanicalSystem;
        var wantedDomain = isDuctSystem ? Domain.DomainHvac : Domain.DomainPiping;
        var targetType = typeOf(target);
        var wantedClass = targetType == null ? "" : targetType.SystemClassification.ToString();
        var targetLabel = label(target);
        var before = membersOf(target);
        var baseId = baseOf(target);
        var asked = new List<Element>();
        var chosen = new Dictionary<ElementId, Connector>();
        var toAdd = new ConnectorSet();

        foreach (var device in devices)
        {
            if (isRun(device)) { problems.Add(runReason(device)); continue; }
            if (before.Contains(device.Id) || device.Id.Equals(baseId))
            {
                findings.Add(nameOf(device) + " is already on " + targetLabel + ", so it was left.");
                continue;
            }
            var ofDomain = connectorsOf(device).Where(c => c.Domain == wantedDomain).ToList();
            if (ofDomain.Count == 0)
            {
                problems.Add(nameOf(device) + " was left: it has no " + (isDuctSystem ? "duct" : "pipe")
                    + " connector, so it cannot go on " + targetLabel + ".");
                continue;
            }
            Connector fit = null;
            var elsewhere = new List<MEPSystem>();
            var otherKinds = new List<string>();
            var onTarget = false;
            foreach (var c in ofDomain)
            {
                MEPSystem on = null;
                try { on = c.MEPSystem; } catch { }
                if (on != null)
                {
                    if (on.Id.Equals(target.Id)) onTarget = true;
                    else if (!elsewhere.Any(s => s.Id.Equals(on.Id))) elsewhere.Add(on);
                    continue;
                }
                var own = "";
                try { own = isDuctSystem ? c.DuctSystemType.ToString() : c.PipeSystemType.ToString(); } catch { }
                var open = own == "" || own == "Global" || own == "Fitting" || own == "UndefinedSystemType";
                if (open || own == wantedClass) { fit = c; break; }
                if (!otherKinds.Contains(own)) otherKinds.Add(own);
            }
            if (fit == null && onTarget)
            {
                findings.Add(nameOf(device) + " is already on " + targetLabel + ", so it was left.");
                continue;
            }
            if (fit == null)
            {
                problems.Add(nameOf(device) + " was left: "
                    + (elsewhere.Count > 0
                        ? "it is on " + string.Join("; ", elsewhere.Select(label)) + " already. Take it out of "
                            + "that system first, then add it - nothing is moved from one system to another "
                            + "without being asked."
                        : "its free connector is of the " + string.Join(", ", otherKinds) + " kind, and "
                            + targetLabel + " is " + wantedClass + "."));
                continue;
            }
            toAdd.Insert(fit);
            chosen[device.Id] = fit;
            asked.Add(device);
        }

        if (asked.Count == 0)
        {
            refused = "Not one element selected could join " + targetLabel + ", so NOTHING WAS CHANGED. "
                + string.Join(" ", problems);
            problems.Clear();
        }
        else
        {
            string revitSaid = null;
            try { target.Add(toAdd); }
            catch (Exception ex) { revitSaid = ex.Message; }

            if (revitSaid != null)
            {
                // Revit refuses the whole set for one connector it will not
                // take: try each again alone, to add the rest and name the one.
                problems.Add("Revit refused the elements together: " + revitSaid + " Each was tried alone.");
                foreach (var device in asked)
                {
                    if (membersOf(target).Contains(device.Id)) continue;
                    try
                    {
                        var one = new ConnectorSet();
                        one.Insert(chosen[device.Id]);
                        target.Add(one);
                    }
                    catch (Exception ex)
                    {
                        problems.Add(nameOf(device) + ": Revit would not add it - " + ex.Message);
                    }
                }
            }

            try { doc.Regenerate(); } catch { }

            // READ BACK, whatever Revit answered.
            var after = membersOf(target);
            var joined = asked.Where(d => after.Contains(d.Id)).ToList();
            changed = joined.Count;
            if (changed == 0)
            {
                refused = "Revit added none of the " + asked.Count + " element(s) to " + targetLabel
                    + ", so NOTHING WAS CHANGED. " + string.Join(" ", problems);
                problems.Clear();
            }
            else
            {
                systemIds.Add(target.Id);
                lines.Add(targetLabel + ": " + before.Count + " element(s) before, " + after.Count
                    + " after - " + changed + " added: " + string.Join(", ", joined.Take(12).Select(nameOf)) + ".");
                if (changed < asked.Count)
                {
                    problems.Add("NOT EVERY ELEMENT JOINED: " + asked.Count + " were asked and " + changed
                        + " are on it. Still off it: " + string.Join(", ", asked
                            .Where(d => !after.Contains(d.Id)).Take(12).Select(nameOf)) + ".");
                }
            }
        }
    }
    else if (refused == null && action == "remove")
    {
        var groups = new Dictionary<ElementId, List<Element>>();
        var byId = new Dictionary<ElementId, MEPSystem>();
        foreach (var device in devices)
        {
            if (isRun(device)) { problems.Add(runReason(device)); continue; }
            MEPSystem from = null;
            if (target != null)
            {
                if (device.Id.Equals(baseOf(target)))
                {
                    problems.Add(nameOf(device) + " is the base equipment of " + label(target) + ", so it was "
                        + "left. Remove from System does not take base equipment off; choose other base "
                        + "equipment for the system in Revit instead.");
                    continue;
                }
                if (!membersOf(target).Contains(device.Id))
                {
                    problems.Add(nameOf(device) + " is not on " + label(target) + ", so it was left.");
                    continue;
                }
                from = target;
            }
            else
            {
                List<MEPSystem> carried;
                if (!onSystems.TryGetValue(device.Id, out carried) || carried.Count == 0)
                {
                    var basedOn = all.Where(s => device.Id.Equals(baseOf(s))).ToList();
                    problems.Add(nameOf(device) + (basedOn.Count > 0
                        ? " is the base equipment of " + string.Join("; ", basedOn.Select(label)) + ", and "
                            + "Remove from System does not take base equipment off, so it was left."
                        : " is on no duct or piping system, and on no circuit, so it was left."));
                    continue;
                }
                if (carried.Count > 1)
                {
                    problems.Add(nameOf(device) + " is on " + carried.Count + " systems - "
                        + string.Join("; ", carried.Select(label)) + " - so it was left. Name the system to "
                        + "take it out of.");
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
            var system = byId[pair.Key];
            var systemLabel = label(system);
            var before = membersOf(system);
            if (pair.Value.Count >= before.Count)
            {
                problems.Add("Taking all " + before.Count + " element(s) out of " + systemLabel + " would leave "
                    + "it with nothing on it. Revit forbids that, and a system left empty is one to delete, "
                    + "which is not an edit. It was left as it is. Delete the system by hand if that is what "
                    + "is meant.");
                continue;
            }
            var ids = pair.Value.Select(d => d.Id).ToList();
            string revitSaid = null;
            try { system.Remove(ids); }
            catch (Exception ex) { revitSaid = ex.Message; }

            if (revitSaid != null)
            {
                problems.Add("Revit refused to take them out of " + systemLabel + " together: " + revitSaid
                    + " Each was tried alone.");
                foreach (var device in pair.Value)
                {
                    if (!membersOf(system).Contains(device.Id)) continue;
                    try { system.Remove(new List<ElementId> { device.Id }); }
                    catch (Exception ex)
                    {
                        problems.Add(nameOf(device) + ": Revit would not take it out - " + ex.Message);
                    }
                }
            }

            try { doc.Regenerate(); } catch { }

            var after = membersOf(system);
            var taken = pair.Value.Where(d => !after.Contains(d.Id)).ToList();
            if (taken.Count == 0)
            {
                problems.Add("Revit took none of " + pair.Value.Count + " element(s) out of " + systemLabel + ".");
                continue;
            }
            changed += taken.Count;
            systemIds.Add(system.Id);
            lines.Add(systemLabel + ": " + before.Count + " element(s) before, " + after.Count + " after - "
                + taken.Count + " taken out: " + string.Join(", ", taken.Take(12).Select(nameOf)) + ".");
            if (taken.Count < pair.Value.Count)
            {
                problems.Add("NOT EVERY ELEMENT CAME OUT OF " + systemLabel + ": " + (pair.Value.Count - taken.Count)
                    + " still on it.");
            }
        }
    }
}
else if (refused == null && half == "circuit")
{
    // ============ CIRCUITS - version 2, from EDIT_ELECTRICAL_CIRCUIT ============
    var all = allCircuits;
    var picked = pickedCircuits;

    Func<ElectricalSystem, string> numberOf = c => { try { return c.CircuitNumber ?? ""; } catch { return ""; } };
    Func<ElectricalSystem, string> panelOf = c => { try { return c.PanelName ?? ""; } catch { return ""; } };
    // What kind of circuit - power, data, fire alarm. "" when Revit will not
    // say, and an unknown kind is read as the SAME kind, the safer side.
    Func<ElectricalSystem, string> kindOf = c => { try { return c.SystemType.ToString(); } catch { return ""; } };
    Func<ElectricalSystem, string> label = c =>
    {
        var number = numberOf(c);
        var onPanelNow = panelOf(c);
        var load = "";
        try { load = c.LoadName ?? ""; } catch { }
        return "circuit " + (number.Length == 0 ? "(no number)" : number)
            + (onPanelNow.Length == 0 ? " on no panel" : " on " + onPanelNow)
            + (load.Length == 0 ? "" : " (" + load + ")");
    };

    // The panel the request named, by its own Panel Name - what the panel
    // schedule shows, and what a circuit's PanelName reads back.
    string panelWord = null;
    if (panel != null && panel.IsValidObject)
    {
        try
        {
            var panelName = panel.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
            if (panelName != null) panelWord = panelName.AsString();
        }
        catch { }
        if (string.IsNullOrEmpty(panelWord)) panelWord = panel.Name;
    }

    // The panel the circuit is on NOW, when the request says one.
    string onPanel = null;
    if (!isNone(circuitPanel)) onPanel = circuitPanel.Trim();

    if (action == "panel" && panelWord == null)
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
            var before = membersOf(target);
            var targetKind = kindOf(target);
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
                var earlier = onCircuits.TryGetValue(device.Id, out carried)
                    ? carried.Where(c => !c.Id.Equals(target.Id)).ToList() : new List<ElectricalSystem>();
                // AN ADD NEVER MOVES A DEVICE: one already on a circuit of the
                // same kind is left, and that circuit named.
                var sameKind = earlier.Where(c => kindOf(c).Length == 0 || targetKind.Length == 0
                    || kindOf(c) == targetKind).ToList();
                if (sameKind.Count > 0)
                {
                    problems.Add(nameOf(device) + " was left: it is on " + string.Join("; ", sameKind.Select(label))
                        + " already. Take it off that circuit first, then add it - an add never moves a "
                        + "device from one circuit to another.");
                    continue;
                }
                if (earlier.Count > 0)
                {
                    findings.Add(nameOf(device) + " is on " + string.Join("; ", earlier.Select(label))
                        + " as well - a circuit of another kind, which joining this one does not take it "
                        + "off.");
                }
                toAdd.Insert(device);
                asked.Add(device);
            }

            if (asked.Count == 0)
            {
                refused = problems.Count == 0
                    ? "Every device selected is already on " + label(target) + ", so NOTHING WAS CHANGED."
                    : "Not one device selected could go onto " + label(target) + ", so NOTHING WAS CHANGED. "
                        + string.Join(" ", problems);
                problems.Clear();
            }
            else
            {
                var targetLabel = label(target);
                string revitSaid = null;
                var answered = false;
                try { answered = target.AddToCircuit(toAdd); }
                catch (Exception ex) { revitSaid = ex.Message; }

                // READ BACK, whatever Revit answered - an exception does not
                // prove nothing changed, and a true does not prove everything did.
                var after = membersOf(target);
                var joined = asked.Where(d => after.Contains(d.Id)).ToList();
                changed = joined.Count;

                if (changed == 0)
                {
                    refused = "Revit added none of the " + asked.Count + " device(s) to " + targetLabel
                        + (revitSaid != null ? ": " + revitSaid : " and gave no reason")
                        + ". NOTHING WAS CHANGED. Devices whose connectors do not suit the circuit's "
                        + "system type, or whose voltage or poles differ from it, are the usual reasons."
                        + (problems.Count == 0 ? "" : " " + string.Join(" ", problems));
                    problems.Clear();
                }
                else
                {
                    systemIds.Add(target.Id);
                    lines.Add(targetLabel + ": " + before.Count + " device(s) before, " + after.Count
                        + " after - " + changed + " added.");
                    if (changed < asked.Count)
                    {
                        problems.Add("NOT EVERY DEVICE JOINED: " + asked.Count + " were asked and " + changed
                            + " are on it. Still off it: " + string.Join(", ", asked
                                .Where(d => !after.Contains(d.Id)).Take(12).Select(nameOf))
                            + (revitSaid != null ? ". Revit said: " + revitSaid : "") + ".");
                    }
                    else if (!answered && revitSaid == null)
                    {
                        findings.Add("Revit answered that the add did not succeed, and yet every device "
                            + "reads back on the circuit - the read-back is what is reported.");
                    }
                    // A device that was on another circuit: read back where it
                    // is now, never assumed. One Revit took off is said loudly.
                    foreach (var device in joined)
                    {
                        List<ElectricalSystem> carried;
                        if (!onCircuits.TryGetValue(device.Id, out carried)) continue;
                        foreach (var was in carried)
                        {
                            if (was.Id.Equals(target.Id)) continue;
                            if (!membersOf(was).Contains(device.Id))
                            {
                                problems.Add(nameOf(device) + " IS NO LONGER ON " + label(was) + " - Revit "
                                    + "took it off when it was added here. That was not asked; Undo takes the "
                                    + "whole edit back.");
                            }
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
                    if (!membersOf(target).Contains(device.Id))
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
                var before = membersOf(circuit);
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

                var after = membersOf(circuit);
                var left = pair.Value.Where(d => !after.Contains(d.Id)).ToList();
                if (left.Count == 0)
                {
                    problems.Add("Revit took none of " + pair.Value.Count + " device(s) off " + circuitLabel
                        + (revitSaid != null ? ": " + revitSaid : " and gave no reason") + ".");
                    continue;
                }
                changed += left.Count;
                systemIds.Add(circuit.Id);
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
            picked = new List<ElectricalSystem>();
            devices = new List<Element>();
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
                    systemIds.Add(circuit.Id);
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
                    systemIds.Add(circuit.Id);
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
}

if (refused == null && changed == 0)
{
    refused = "Nothing was changed. " + (problems.Count == 0 ? "" : string.Join(" ", problems));
    problems.Clear();
}
findings.AddRange(problems);
report = lines.Count == 0 ? "Nothing was changed." : string.Join("\n", lines);
if (refused != null) findings.Add(refused);
