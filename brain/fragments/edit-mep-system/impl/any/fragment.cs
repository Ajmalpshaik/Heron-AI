// NOT STANDALONE. Assumes `doc`, `elements`, `edit` and `systemName` are in
// scope; leaves `systemIds`, `changed`, `report`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry,
// and the caller owns it.
//
// EDITS A DUCT OR PIPING SYSTEM THAT ALREADY EXISTS - Revit's Add to System
// and Remove from System. CREATE_MEP_SYSTEM makes a new system; this one never
// makes one, never deletes one, and never touches an electrical circuit
// (EDIT_ELECTRICAL_CIRCUIT does).
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
// TAKING EVERY TERMINAL OUT OF A SYSTEM IS REFUSED for that system. Revit's
// own documentation forbids it on every release 2020 to 2027, and a system left
// with nothing on it is one to delete, which is not an edit (Article 7). The
// base equipment is not taken out here either - Revit says the element joined
// to it cannot be removed, and changing it is a different button.
//
// EVERY CHANGE IS READ BACK off the system afterwards - its own Elements and
// BaseEquipment - and `changed` counts what reads back changed, never what was
// asked. Revit refuses a whole set for one wrong connector, so a refused set
// is tried again one element at a time to name the one it would not take.

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

// The two buttons, and the words a modeller types for each.
var mode = squash(edit);
string action = null;
if (mode == "add" || mode == "addto" || mode == "addtosystem" || mode == "puton") action = "add";
else if (mode == "remove" || mode == "removefrom" || mode == "removefromsystem"
    || mode == "takeout" || mode == "takeoff") action = "remove";

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
// A system's members as Revit lists them: its terminals and equipment, never
// its base equipment, never its ducts or pipes.
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

// Ducts, pipes, flex, fittings and accessories: on a system through what they
// connect to, so never added to one or taken out of one here.
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

// Every duct and piping system in the model, once, and which systems each
// member is on.
var all = new List<MEPSystem>();
foreach (MechanicalSystem s in new FilteredElementCollector(doc)
    .OfClass(typeof(MechanicalSystem)).WhereElementIsNotElementType().Cast<MechanicalSystem>())
{
    if (s != null && s.IsValidObject) all.Add(s);
}
foreach (PipingSystem s in new FilteredElementCollector(doc)
    .OfClass(typeof(PipingSystem)).WhereElementIsNotElementType().Cast<PipingSystem>())
{
    if (s != null && s.IsValidObject) all.Add(s);
}
var onSystems = new Dictionary<ElementId, List<MEPSystem>>();
foreach (var s in all)
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

// The selection, split: duct and piping systems picked in Revit, and the rest.
var picked = new List<MEPSystem>();
var devices = new List<Element>();
if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;
        if (element is MechanicalSystem || element is PipingSystem)
        {
            var asSystem = (MEPSystem)element;
            if (!picked.Any(s => s.Id.Equals(asSystem.Id))) picked.Add(asSystem);
        }
        else if (element is MEPSystem)
        {
            findings.Add(nameOf(element) + " is an electrical circuit, not a duct or piping system, so "
                + "it was not used. Circuits are edited with Add to Circuit and Remove from Circuit.");
        }
        else if (!devices.Any(d => d.Id.Equals(element.Id)))
        {
            devices.Add(element);
        }
    }
}

if (action == null)
{
    refused = "\"" + edit + "\" is not an edit this understands, so NOTHING WAS CHANGED. Use add or "
        + "remove - Revit's Add to System and Remove from System.";
}

// A system named - exactly as the System Browser shows it, or else the one
// system a whole word of whose name it is, or whose system type it names.
MEPSystem named = null;
if (refused == null && !isNone(systemName))
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
            // Revit refuses the whole set for one connector it will not take:
            // try each again alone, to add the rest and name the one.
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
                    : " is on no duct or piping system, so it was left."));
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

if (refused == null && changed == 0)
{
    refused = "Nothing was changed. " + (problems.Count == 0 ? "" : string.Join(" ", problems));
    problems.Clear();
}
findings.AddRange(problems);
report = lines.Count == 0 ? "Nothing was changed." : string.Join("\n", lines);
if (refused != null) findings.Add(refused);
