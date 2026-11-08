// NOT STANDALONE. Assumes `doc`, `systemName`, `start`, `newViewName`,
// `viewScale`, `spacingMm`, `textType` and `maxElements` are in scope; leaves
// `viewId`, `viewName`, `nodes`, `segments`, `terminals`, `openEnds`,
// `loopsCut`, `skipped`, `skippedWhy`, `labelsWithoutValue`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. The
// executor makes the whole call one TransactionGroup, so the new view and
// everything drawn in it are ONE undo.
//
// THE 3D MODEL IS NEVER CHANGED. Everything this makes is a new drafting view
// and the detail lines and text notes OWNED by that view. Nothing in the model
// is moved, sized, joined or written to - the system is only read.
//
// WHY ONE CARD DOES THE WHOLE JOB. A traced network handed out through a read
// is no use to a second step: the add-in hands a list back as its count and its
// first three items, and a chat's values lose their line breaks. So the trace,
// the layout and the drawing all happen here, inside Revit, in one call.
//
// ---------------------------------------------------------------------------
// RE-AUTHORED FROM HERON'S OWN CARDS - the lessons, in Heron's words
// ---------------------------------------------------------------------------
//
//   from TRACE_CONNECTIVITY (DRAFT) - the walk follows Connector.AllRefs from
//     each element's ConnectorManager (MEPCurve.ConnectorManager, or
//     FamilyInstance.MEPModel.ConnectorManager); Plumbing Equipment is a
//     category only from the 2023.1 API on, so it is looked up BY NAME at run
//     time; categories are compared as ElementIds, never read as numbers.
//   from REPORT_MEP_PRESSURE_DROP (PROVEN) - Revit computes hydraulics only for
//     a complete, sized, connected system, and an uncalculated one reports
//     ZERO, which reads as "no pressure loss" when it means "not calculable".
//     So SectionsCount is asked first, and a system with none has every
//     pressure drop label say "not calculated", never 0. MEPSection is in the
//     Mechanical namespace for pipe systems too.
//   from CREATE_DRAFTING_VIEW (PROVEN) - the view type is found by
//     ViewFamily.Drafting, never by its name; a name clash is checked BEFORE
//     the view is made, because Revit creates the view and then refuses the
//     name. Here a clash is answered with a suffix, never by touching the
//     existing view.
//   from CREATE_TEXT_NOTE (PROVEN) - a text type is never invented: named, or
//     the project's default text type, validated as a TextNoteType; TextNote
//     .Create makes a new line at '\r'.
//   from DRAW_FAMILY_DETAIL_LINES (PROVEN) - a project's detail line is
//     doc.Create.NewDetailCurve(view, curve), and a curve shorter than the
//     application's ShortCurveTolerance is refused by Revit, so it is left out
//     and counted rather than thrown.
//   from AUTO_SIZE_MEP and MEASURE_MEP_VELOCITY (PROVEN) - flow is
//     RBS_DUCT_FLOW_PARAM on a duct, RBS_PIPE_FLOW_PARAM on a pipe, read with
//     AsValueString so it is shown in the project's own units.
//   from REPORT_MODEL_DASHBOARD (PROVEN) - a run's size is RBS_CALCULATED_SIZE,
//     the text Revit shows ("600x400", "200ø").
//
// ---------------------------------------------------------------------------
// WHAT IT DOES
// ---------------------------------------------------------------------------
//
// 1. WHICH SYSTEM. By name (exactly, ignoring case; or the ONE system whose
//    name contains it, said in findings), or the system of the element handed
//    in as `start`. A start on several systems needs the name. Ducts and pipes
//    only: an electrical system is not drawn.
//
// 2. THE TRACE. The system's elements are Revit's own sets - MEPSystem
//    .Elements, MechanicalSystem.DuctNetwork or PipingSystem.PipingNetwork,
//    and the base equipment. Above `maxElements` it refuses, in words, before
//    anything is made. Each element's HVAC or piping connectors are followed
//    through AllRefs; a logical connector and the system's own connector are
//    not joints. A joint to an element OUTSIDE the set (another system - the
//    return side of an air handler) is not followed, and is counted. An END
//    connector of a run, fitting or accessory joined to nothing is an OPEN END:
//    listed, and marked on the drawing. The walk is breadth-first from the
//    root - `start`, else the base equipment, else (said) an element with one
//    joint - so the drawing is a TREE. A joint that would close a LOOP is CUT:
//    both its ends are marked L1, L2 ... and loopsCut names the two elements.
//    Elements of the system the walk never reaches are skipped and counted.
//
// 3. COLLAPSING. A schematic node is the root, equipment, a terminal, a
//    junction (a tee, a cross, a run with a tap - three or more joints), an end,
//    an element with an open end, or a loop end. Everything between two nodes -
//    runs, elbows, transitions, accessories - is ONE line. Along that line a
//    change of size starts a new labelled segment, marked with a short tick.
//
// 4. THE LABELS - Revit's own values, never Heron's maths. Size is
//    RBS_CALCULATED_SIZE of the segment's first run. Flow is the first run's
//    Flow parameter, as Revit shows it; 0 or none is named in
//    labelsWithoutValue. Pressure drop is the Pressure Drop parameter Revit
//    holds (RBS_PRESSURE_DROP on ducts, RBS_PIPE_PRESSUREDROP_PARAM on pipes) on
//    each run, elbow, transition and accessory of the segment that carries
//    one: one value is shown as Revit shows it; several are ADDED and the sum
//    is formatted in the project's units by Revit's own UnitFormatUtils - the
//    one piece of arithmetic here, and the legend says so. Tees and crosses
//    are nodes, so their loss is in no segment. A system with no sections has
//    every Δp label say "not calculated".
//
// 5. THE LAYOUT - NOT TO SCALE, and the view says so. A tree laid out from the
//    root: the main run goes right; at each node the child carrying the most
//    terminals continues straight on and every other child is a branch drawn
//    square to it, the sides alternating. A branch is itself laid out the same
//    way, square to its parent. Every label and every branch on one side of a
//    run is placed after the last thing on that side, `spacingMm` (paper mm)
//    clear, so nothing overlaps - judged from each text's size estimated from
//    the text type's height and width factor. After drawing, every label's real
//    box is read back from Revit and any two that touch are counted in
//    findings - the estimate is checked, not trusted. Paper millimetres become
//    model feet by the view scale / 304.8 (D-20).
//
// 6. THE DRAWING. Detail lines for the runs; a small rectangle for equipment,
//    a circle (two half arcs) for a terminal, an X for an open end, a tick for
//    another end, two ticks for a loop end. Text notes for the labels, the
//    Mark (or the type name) of equipment and terminals, and a legend above the
//    drawing. No family is loaded.
//
// NOT DONE IN VERSION 1, AND SAID: linked models (a system in a link is not
// found); electrical systems; flex runs are drawn like any run; the drawing
// does not update when the model changes - it is a drafting view.
//
// UNITS. Every length typed is paper millimetres (spacingMm). Flow, size and
// a single pressure drop are Revit's own text (AsValueString). A sum of
// pressure drops is formatted by UnitFormatUtils.Format: on Revit 2020 with
// UnitType (removed at 2022), from 2021 with SpecTypeId - the ONE version
// branch, in `formatPressure` below (revit-version-support: units at 2021).

const double MillimetresPerFoot = 304.8;
var began = DateTime.UtcNow;

ElementId viewId = null;
var viewName = "";
var nodes = 0;
var segments = 0;
var terminals = 0;
// Elements with an END connector joined to nothing. What the walk found open,
// never something drawn (D-52).
var openEnds = new List<ElementId>();
// One line per loop cut: its tag and the two elements of the cut joint (D-52).
var loopsCut = new List<string>();
// Elements of the system left out of the drawing, with their reasons counted
// in skippedWhy (D-52).
var skipped = new List<ElementId>();
var skippedWhy = new List<string>();
// One line per segment whose flow or pressure drop Revit has no value for
// (D-52).
var labelsWithoutValue = new List<string>();
// Why nothing was drawn at all, in plain words. Empty when it ran (D-52).
var refused = "";
var findings = new List<string>();

Func<string, bool> leftOut = text =>
    text == null || text.Trim().Length == 0
    || string.Equals(text.Trim(), "none", StringComparison.OrdinalIgnoreCase);

// ---- 0. the values, read before anything is looked for --------------------

var nameWanted = leftOut(systemName) ? "" : systemName.Trim();
var viewWanted = leftOut(newViewName) ? "" : newViewName.Trim();
var textWanted = leftOut(textType) ? "" : textType.Trim();

if (viewScale < 1 || viewScale > 24000)
    refused = string.Format("a view scale of {0} is not one Revit takes - give a whole number from 1 "
        + "to 24000 (100 for 1:100). Nothing was drawn", viewScale);
else if (spacingMm <= 0 || spacingMm > 200 || double.IsNaN(spacingMm))
    refused = string.Format("a spacing of {0} paper mm cannot be drawn - give a number above 0 and "
        + "up to 200 (10 is usual). Nothing was drawn", spacingMm);
else if (maxElements < 1)
    refused = string.Format("maxElements {0} would draw nothing - give the most elements a system "
        + "may hold to be drawn in one call (2000 when left out). Nothing was drawn", maxElements);
else if (nameWanted.Length == 0 && start == null)
    refused = "say which system to draw - its name, as Revit's System Browser shows it (\"SA-01\"), "
        + "or select one element on it and say \"selected\". Nothing was drawn";

// ---- 1. which system -------------------------------------------------------

var allSystems = new List<MEPSystem>();
if (refused.Length == 0)
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfClass(typeof(MechanicalSystem)).WhereElementIsNotElementType())
    {
        var one = element as MEPSystem;
        if (one != null) allSystems.Add(one);
    }
    foreach (var element in new FilteredElementCollector(doc)
        .OfClass(typeof(PipingSystem)).WhereElementIsNotElementType())
    {
        var one = element as MEPSystem;
        if (one != null) allSystems.Add(one);
    }
}

Func<MEPSystem, string> nameOf = s => (s == null ? "" : (s.Name ?? "")).Trim();

// The duct or pipe systems an element is on, read from its own connectors.
Func<Element, List<MEPSystem>> systemsOf = element =>
{
    var found = new List<MEPSystem>();
    if (element == null) return found;
    try
    {
        var curve = element as MEPCurve;
        if (curve != null)
        {
            var own = curve.MEPSystem;
            if (own is MechanicalSystem || own is PipingSystem) found.Add(own);
            return found;
        }
        var instance = element as FamilyInstance;
        if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null)
            return found;
        foreach (Connector connector in instance.MEPModel.ConnectorManager.Connectors)
        {
            MEPSystem on = null;
            try { on = connector.MEPSystem; } catch (Exception) { on = null; }
            if (!(on is MechanicalSystem || on is PipingSystem)) continue;
            if (!found.Any(s => s.Id == on.Id)) found.Add(on);
        }
    }
    catch (Exception) { }
    return found;
};

MEPSystem system = null;
if (refused.Length == 0)
{
    List<MEPSystem> startSystems = start != null ? systemsOf(start) : new List<MEPSystem>();

    if (nameWanted.Length > 0)
    {
        var exact = allSystems.Where(s => string.Equals(nameOf(s), nameWanted,
            StringComparison.OrdinalIgnoreCase)).ToList();
        var containing = allSystems.Where(s => nameOf(s).IndexOf(nameWanted,
            StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        if (exact.Count == 1) system = exact[0];
        else if (exact.Count > 1)
            refused = string.Format("{0} duct or pipe systems are called \"{1}\", so which one to draw "
                + "would be a guess. Select one element on the one meant and say \"selected\", with this "
                + "name. Nothing was drawn", exact.Count, nameWanted);
        else if (containing.Count == 1)
        {
            system = containing[0];
            findings.Add(string.Format("no system is called exactly \"{0}\" - the one system whose name "
                + "contains it, \"{1}\", was drawn", nameWanted, nameOf(system)));
        }
        else if (containing.Count > 1)
            refused = string.Format("no system is called exactly \"{0}\", and {1} have it in their name: "
                + "{2}. Say which one in full. Nothing was drawn", nameWanted, containing.Count,
                string.Join(", ", containing.Take(12).Select(s => "\"" + nameOf(s) + "\""))
                + (containing.Count > 12 ? " ..." : ""));
        else
        {
            var names = allSystems.Select(nameOf).Where(n => n.Length > 0).Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            refused = string.Format("no duct or pipe system in this model is called \"{0}\", and none has "
                + "it in its name. {1}Nothing was drawn", nameWanted,
                names.Count == 0 ? "This model has no duct or pipe systems at all. "
                : string.Format("The systems are: {0}{1}. ", string.Join(", ", names.Take(15)),
                    names.Count > 15 ? string.Format(" and {0} more", names.Count - 15) : ""));
        }

        if (system != null && start != null && !startSystems.Any(s => s.Id == system.Id))
        {
            refused = string.Format("the selected element is not on system \"{0}\"{1} - draw that system "
                + "without a selection, or select an element on it. Nothing was drawn", nameOf(system),
                startSystems.Count == 0 ? " (it is on no duct or pipe system)"
                : " (it is on " + string.Join(", ", startSystems.Select(s => "\"" + nameOf(s) + "\"")) + ")");
            system = null;
        }
    }
    else
    {
        if (startSystems.Count == 1) system = startSystems[0];
        else if (startSystems.Count == 0)
            refused = "the selected element is on no duct or pipe system, so there is nothing to trace. "
                + "Select a duct, pipe, fitting, terminal or piece of equipment that is on one. Nothing "
                + "was drawn";
        else
            refused = string.Format("the selected element is on {0} systems - {1} - so which to draw would "
                + "be a guess. Say the system's name too. Nothing was drawn", startSystems.Count,
                string.Join(", ", startSystems.Select(s => "\"" + nameOf(s) + "\"")));
    }
}

var mechanical = system as MechanicalSystem;
var piping = system as PipingSystem;
var isDuct = mechanical != null;

// ---- 2. the system's elements, and the size guard --------------------------

var members = new Dictionary<ElementId, Element>();
Action<ElementSet> addAll = set =>
{
    if (set == null) return;
    foreach (Element element in set)
    {
        if (element != null && !members.ContainsKey(element.Id)) members[element.Id] = element;
    }
};
FamilyInstance baseEquipment = null;
if (refused.Length == 0 && system != null)
{
    try { addAll(system.Elements); } catch (Exception) { }
    try { if (mechanical != null) addAll(mechanical.DuctNetwork); } catch (Exception) { }
    try { if (piping != null) addAll(piping.PipingNetwork); } catch (Exception) { }
    try { baseEquipment = system.BaseEquipment; } catch (Exception) { baseEquipment = null; }
    if (baseEquipment != null && !members.ContainsKey(baseEquipment.Id)) members[baseEquipment.Id] = baseEquipment;
    if (start != null && !members.ContainsKey(start.Id)) members[start.Id] = start;

    if (members.Count > maxElements)
        refused = string.Format("system \"{0}\" holds {1} elements, more than the {2} this draws in one call "
            + "(maxElements). Nothing was drawn. Give maxElements {1} or more to draw it - a large system "
            + "takes longer, and its schematic is large", nameOf(system), members.Count, maxElements);
    else if (members.Count == 0)
        refused = string.Format("system \"{0}\" holds no elements Revit will name - nothing to draw. "
            + "Nothing was drawn", nameOf(system));
}

// ---- 3. the text type and the view type, before anything is made -----------

TextNoteType noteType = null;
ViewFamilyType draftingType = null;
if (refused.Length == 0)
{
    var noteTypes = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType))
        .Cast<TextNoteType>().ToList();
    if (textWanted.Length > 0)
    {
        noteType = noteTypes.FirstOrDefault(t => string.Equals((t.Name ?? "").Trim(), textWanted,
            StringComparison.OrdinalIgnoreCase));
        if (noteType == null)
            refused = string.Format("no text type is called \"{0}\". The text types are: {1}. Nothing was "
                + "drawn", textWanted, string.Join(", ", noteTypes.Select(t => "\"" + t.Name + "\"")
                .OrderBy(n => n).Take(20)));
    }
    else
    {
        // THE PROJECT'S DEFAULT TEXT TYPE - never the first one the collector
        // happens to return (CREATE_TEXT_NOTE's lesson).
        ElementId defaultId = null;
        try { defaultId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType); } catch (Exception) { }
        noteType = defaultId == null ? null : doc.GetElement(defaultId) as TextNoteType;
        if (noteType == null)
            refused = "this project names no default text type, and choosing one would be a guess - give "
                + "textType, a text type by name. Nothing was drawn";
    }
}
if (refused.Length == 0)
{
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
        .Cast<ViewFamilyType>())
    {
        if (candidate.ViewFamily == ViewFamily.Drafting) { draftingType = candidate; break; }
    }
    if (draftingType == null)
        refused = "this project's template carries no Drafting view type, so there is nothing to make "
            + "the schematic's view from. Nothing was drawn";
}

// ---- 4. the walk -----------------------------------------------------------

var categoryIs = new Func<Element, BuiltInCategory, bool>((element, category) =>
{
    try { return element != null && element.Category != null && element.Category.Id == new ElementId(category); }
    catch (Exception) { return false; }
});
// PLUMBING EQUIPMENT IS A CATEGORY ONLY FROM THE 2023.1 API ON - looked up by
// name at run time (TRACE_CONNECTIVITY's lesson, row 5b-181).
BuiltInCategory plumbingEquipment;
var hasPlumbingEquipment = Enum.TryParse("OST_PlumbingEquipment", out plumbingEquipment);

Func<Element, bool> isTerminal = element =>
    categoryIs(element, BuiltInCategory.OST_DuctTerminal)
    || categoryIs(element, BuiltInCategory.OST_PlumbingFixtures)
    || categoryIs(element, BuiltInCategory.OST_Sprinklers);
Func<Element, bool> isEquipment = element =>
    (baseEquipment != null && element != null && element.Id == baseEquipment.Id)
    || categoryIs(element, BuiltInCategory.OST_MechanicalEquipment)
    || (hasPlumbingEquipment && categoryIs(element, plumbingEquipment));

Func<Element, ConnectorSet> connectorsOf = element =>
{
    try
    {
        var curve = element as MEPCurve;
        if (curve != null && curve.ConnectorManager != null) return curve.ConnectorManager.Connectors;
        var instance = element as FamilyInstance;
        if (instance != null && instance.MEPModel != null && instance.MEPModel.ConnectorManager != null)
            return instance.MEPModel.ConnectorManager.Connectors;
    }
    catch (Exception) { }
    return null;
};
Func<Element, string> describe = element =>
{
    if (element == null) return "(nothing)";
    var category = "";
    try { category = element.Category != null ? element.Category.Name : element.GetType().Name; }
    catch (Exception) { category = "element"; }
    return category + " " + element.Id;
};

var adjacent = new Dictionary<ElementId, List<Element>>();
var openAt = new HashSet<ElementId>();
var openConnectors = 0;
var outsideJoints = new HashSet<ElementId>();
var ordered = members.Values.OrderBy(e => e.UniqueId ?? "", StringComparer.Ordinal).ToList();

if (refused.Length == 0)
{
    foreach (var element in ordered)
    {
        var near = new List<Element>();
        var connectors = connectorsOf(element);
        if (connectors != null)
        {
            foreach (Connector connector in connectors)
            {
                if (connector == null) continue;
                Domain domain;
                ConnectorType kind;
                try { domain = connector.Domain; kind = connector.ConnectorType; }
                catch (Exception) { continue; }
                if (domain != Domain.DomainHvac && domain != Domain.DomainPiping) continue;
                if (kind == ConnectorType.Logical || kind == ConnectorType.Reference) continue;

                var joined = false;
                ConnectorSet refs = null;
                try { refs = connector.AllRefs; } catch (Exception) { refs = null; }
                if (refs != null)
                {
                    foreach (Connector other in refs)
                    {
                        if (other == null) continue;
                        Element owner = null;
                        ConnectorType otherKind;
                        try { owner = other.Owner; otherKind = other.ConnectorType; }
                        catch (Exception) { continue; }
                        // The system's own connector, and a logical link, are
                        // not a joint in the pipework.
                        if (owner == null || owner is MEPSystem) continue;
                        if (otherKind == ConnectorType.Logical || otherKind == ConnectorType.Reference) continue;
                        if (owner.Id == element.Id) continue;
                        joined = true;
                        // ANOTHER SYSTEM'S ELEMENT - the walk stops here.
                        if (!members.ContainsKey(owner.Id)) { outsideJoints.Add(owner.Id); continue; }
                        if (!near.Any(n => n.Id == owner.Id)) near.Add(members[owner.Id]);
                    }
                }
                // AN OPEN END: an END connector of a run, fitting or accessory
                // with nothing joined. Equipment and terminals carry connectors
                // of other systems, which are not this one's open ends.
                if (!joined && kind == ConnectorType.End && !isTerminal(element) && !isEquipment(element))
                {
                    openConnectors++;
                    if (openAt.Add(element.Id)) openEnds.Add(element.Id);
                }
            }
        }
        adjacent[element.Id] = near.OrderBy(n => n.UniqueId ?? "", StringComparer.Ordinal).ToList();
    }
}

// The root: the start, else the base equipment, else - said - an element with
// one joint (equipment first), else the first element.
Element root = null;
if (refused.Length == 0)
{
    if (start != null) root = start;
    else if (baseEquipment != null) root = baseEquipment;
    else
    {
        root = ordered.FirstOrDefault(e => isEquipment(e) && adjacent[e.Id].Count >= 1)
            ?? ordered.FirstOrDefault(e => !isTerminal(e) && adjacent[e.Id].Count == 1)
            ?? ordered.FirstOrDefault(e => adjacent[e.Id].Count >= 1)
            ?? ordered.First();
        findings.Add(string.Format("system \"{0}\" names no base equipment in Revit, so it is drawn from {1}",
            nameOf(system), describe(root)));
    }
}

// Breadth-first: a tree, and each joint that would close a loop cut.
var parentOf = new Dictionary<ElementId, Element>();
var childrenOf = new Dictionary<ElementId, List<Element>>();
var reached = new HashSet<ElementId>();
var loopTags = new Dictionary<ElementId, List<string>>();
if (refused.Length == 0)
{
    var cutPairs = new HashSet<string>();
    var queue = new Queue<Element>();
    queue.Enqueue(root);
    reached.Add(root.Id);
    childrenOf[root.Id] = new List<Element>();
    while (queue.Count > 0)
    {
        var here = queue.Dequeue();
        foreach (var next in adjacent[here.Id])
        {
            if (!reached.Contains(next.Id))
            {
                reached.Add(next.Id);
                parentOf[next.Id] = here;
                childrenOf[here.Id].Add(next);
                childrenOf[next.Id] = new List<Element>();
                queue.Enqueue(next);
                continue;
            }
            Element hereParent;
            if (parentOf.TryGetValue(here.Id, out hereParent) && hereParent.Id == next.Id) continue;
            Element nextParent;
            if (parentOf.TryGetValue(next.Id, out nextParent) && nextParent.Id == here.Id) continue;
            var a = string.CompareOrdinal(here.UniqueId ?? "", next.UniqueId ?? "") < 0 ? here : next;
            var b = a.Id == here.Id ? next : here;
            if (!cutPairs.Add((a.UniqueId ?? "") + "|" + (b.UniqueId ?? ""))) continue;
            var tag = "L" + (loopsCut.Count + 1);
            loopsCut.Add(string.Format("{0}: the joint between {1} and {2} closes a loop - it is real in the "
                + "model; the schematic is a tree, so it is drawn as two ends marked {0}", tag, describe(a),
                describe(b)));
            foreach (var end in new[] { a, b })
            {
                if (!loopTags.ContainsKey(end.Id)) loopTags[end.Id] = new List<string>();
                loopTags[end.Id].Add(tag);
            }
        }
    }

    var notReached = ordered.Where(e => !reached.Contains(e.Id)).ToList();
    foreach (var element in notReached) skipped.Add(element.Id);
    if (notReached.Count > 0)
        skippedWhy.Add(string.Format("{0} x on system \"{1}\" in Revit but not joined to the drawn part by its "
            + "connectors - a piece the walk from {2} cannot reach", notReached.Count, nameOf(system),
            describe(root)));
}

// ---- 5. collapsing into schematic nodes and segments -----------------------

Func<Element, int> treeDegree = element =>
    (childrenOf.ContainsKey(element.Id) ? childrenOf[element.Id].Count : 0)
    + (root != null && element.Id == root.Id ? 0 : 1);
Func<Element, bool> isNode = element =>
    (root != null && element.Id == root.Id) || treeDegree(element) != 2 || isTerminal(element)
    || isEquipment(element) || openAt.Contains(element.Id) || loopTags.ContainsKey(element.Id);

var nodeChildren = new Dictionary<ElementId, List<Element>>();
var chainTo = new Dictionary<ElementId, List<Element>>();
var nodeOrder = new List<Element>();
if (refused.Length == 0)
{
    var pending = new Stack<Element>();
    pending.Push(root);
    while (pending.Count > 0)
    {
        var node = pending.Pop();
        nodeOrder.Add(node);
        nodeChildren[node.Id] = new List<Element>();
        foreach (var child in childrenOf[node.Id])
        {
            var chain = new List<Element>();
            var here = child;
            // Not a node: exactly one joint in and one out, so one child.
            while (!isNode(here)) { chain.Add(here); here = childrenOf[here.Id][0]; }
            nodeChildren[node.Id].Add(here);
            chainTo[here.Id] = chain;
            pending.Push(here);
        }
    }
}

// How many leaves under each node - the heaviest child goes straight on.
var leavesUnder = new Dictionary<ElementId, int>();
for (var i = nodeOrder.Count - 1; i >= 0; i--)
{
    var node = nodeOrder[i];
    var kids = nodeChildren[node.Id];
    leavesUnder[node.Id] = kids.Count == 0 ? 1 : kids.Sum(k => leavesUnder[k.Id]);
}
Func<Element, Element> straightOn = node =>
{
    Element best = null;
    foreach (var kid in nodeChildren[node.Id])
        if (best == null || leavesUnder[kid.Id] > leavesUnder[best.Id]) best = kid;
    return best;
};

// Revit's own values.
var sectioned = 0;
if (refused.Length == 0)
{
    try { sectioned = mechanical != null ? mechanical.SectionsCount : (piping != null ? piping.SectionsCount : 0); }
    catch (Exception) { sectioned = 0; }
    if (sectioned == 0)
        findings.Add(string.Format("Revit has NOT calculated system \"{0}\" (no sections) - an open end, an "
            + "unsized run or calculations switched off. Every pressure drop label says \"not calculated\", "
            + "never 0 (REPORT_MEP_PRESSURE_DROP's lesson)", nameOf(system)));
}

Func<Element, BuiltInParameter, Parameter> parameterOf = (element, which) =>
{
    try { return element.get_Parameter(which); } catch (Exception) { return null; }
};
Func<Element, string> sizeOf = element =>
{
    var p = parameterOf(element, BuiltInParameter.RBS_CALCULATED_SIZE);
    if (p == null) return "";
    string text = null;
    try { text = p.AsString(); } catch (Exception) { text = null; }
    if (string.IsNullOrEmpty(text)) { try { text = p.AsValueString(); } catch (Exception) { text = null; } }
    return (text ?? "").Trim();
};
var flowParameter = isDuct ? BuiltInParameter.RBS_DUCT_FLOW_PARAM : BuiltInParameter.RBS_PIPE_FLOW_PARAM;
var dropParameter = isDuct ? BuiltInParameter.RBS_PRESSURE_DROP : BuiltInParameter.RBS_PIPE_PRESSUREDROP_PARAM;

// THE ONE VERSION BRANCH: a sum of Revit's own pressure drops, formatted in the
// project's units by Revit. UnitType is 2020-2021 and gone at 2022; SpecTypeId
// is 2021 on (tools/api-surface, 2026-11-08).
Func<double, string> formatPressure = value =>
{
    try
    {
#if REVIT2020
        return UnitFormatUtils.Format(doc.GetUnits(),
            isDuct ? UnitType.UT_HVAC_Pressure : UnitType.UT_Piping_Pressure, value, false, false);
#else
        return UnitFormatUtils.Format(doc.GetUnits(),
            isDuct ? SpecTypeId.HvacPressure : SpecTypeId.PipingPressure, value, false);
#endif
    }
    catch (Exception) { return value.ToString("0.###") + " (Revit internal units)"; }
};

// One labelled segment's three lines, and what was missing.
Func<List<Element>, string> labelOf = group =>
{
    var curves = group.Where(e => e is MEPCurve).ToList();
    var first = curves[0];
    var size = sizeOf(first);
    var flowText = "";
    var flowP = parameterOf(first, flowParameter);
    try
    {
        if (flowP != null && flowP.HasValue && flowP.StorageType == StorageType.Double && flowP.AsDouble() > 0)
            flowText = (flowP.AsValueString() ?? "").Trim();
    }
    catch (Exception) { flowText = ""; }

    var dropText = "";
    var missing = new List<string>();
    if (sectioned == 0) { dropText = "not calculated"; missing.Add("no pressure drop - Revit has not calculated the system"); }
    else
    {
        var sum = 0.0;
        var count = 0;
        string single = null;
        foreach (var member in group)
        {
            var p = parameterOf(member, dropParameter);
            try
            {
                if (p == null || !p.HasValue || p.StorageType != StorageType.Double) continue;
                sum += p.AsDouble();
                count++;
                single = p.AsValueString();
            }
            catch (Exception) { }
        }
        if (count == 0) { dropText = "-"; missing.Add("no pressure drop - Revit holds none on its runs"); }
        else dropText = count == 1 ? (single ?? formatPressure(sum)).Trim() : formatPressure(sum);
    }
    if (flowText.Length == 0) { flowText = "-"; missing.Add("no flow - Revit holds 0 or none"); }
    if (missing.Count > 0)
        labelsWithoutValue.Add(string.Format("the run from {0} ({1}): {2}", describe(first),
            size.Length == 0 ? "no size" : size, string.Join("; ", missing)));
    return (size.Length == 0 ? "-" : size) + "\rQ " + flowText + "\rΔp " + dropText;
};

// The segments of the line from a node's parent node to it: the chain, plus
// the node itself when it is a run (a run with a tap). Split where the size
// changes; fittings go with the segment they follow.
var rootCurveTaken = false;
Func<Element, Element, List<List<Element>>> segmentsTo = (parent, node) =>
{
    var line = new List<Element>();
    if (parent.Id == root.Id && root is MEPCurve && !rootCurveTaken) { line.Add(root); rootCurveTaken = true; }
    line.AddRange(chainTo[node.Id]);
    if (node is MEPCurve) line.Add(node);
    var groups = new List<List<Element>>();
    List<Element> current = null;
    string currentSize = null;
    foreach (var member in line)
    {
        if (member is MEPCurve)
        {
            var size = sizeOf(member);
            if (current == null) { current = new List<Element>(); groups.Add(current); currentSize = size; }
            else if (currentSize == null) currentSize = size;
            else if (!string.Equals(size, currentSize, StringComparison.Ordinal))
            {
                current = new List<Element>(); groups.Add(current); currentSize = size;
            }
        }
        else if (current == null) { current = new List<Element>(); groups.Add(current); currentSize = null; }
        current.Add(member);
    }
    return groups;
};

// What a node says beside it.
Func<Element, string> nodeLabelOf = node =>
{
    var parts = new List<string>();
    if (isEquipment(node) || isTerminal(node))
    {
        var mark = "";
        try
        {
            var p = parameterOf(node, BuiltInParameter.ALL_MODEL_MARK);
            if (p != null) mark = (p.AsString() ?? "").Trim();
        }
        catch (Exception) { mark = ""; }
        if (mark.Length == 0)
        {
            try
            {
                var type = doc.GetElement(node.GetTypeId());
                mark = type != null ? (type.Name ?? "").Trim() : "";
            }
            catch (Exception) { mark = ""; }
        }
        parts.Add(mark.Length == 0 ? describe(node) : mark);
    }
    else if (node.Id == root.Id)
    {
        var typeName = "";
        try { var type = doc.GetElement(node.GetTypeId()); typeName = type != null ? type.Name : ""; }
        catch (Exception) { typeName = ""; }
        parts.Add("start - " + (string.IsNullOrEmpty(typeName) ? describe(node) : typeName));
    }
    if (openAt.Contains(node.Id)) parts.Add("open end");
    if (loopTags.ContainsKey(node.Id)) parts.Add("loop " + string.Join(", ", loopTags[node.Id]) + " cut");
    return string.Join("\r", parts);
};

// ---- 6. the layout, in PAPER millimetres -----------------------------------

var textMm = 2.5;
var widthFactor = 1.0;
if (noteType != null)
{
    try
    {
        var size = noteType.get_Parameter(BuiltInParameter.TEXT_SIZE);
        if (size != null && size.HasValue && size.AsDouble() > 0) textMm = size.AsDouble() * MillimetresPerFoot;
        var width = noteType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE);
        if (width != null && width.HasValue && width.AsDouble() > 0) widthFactor = width.AsDouble();
    }
    catch (Exception) { }
}
// AN ESTIMATE, read back after drawing: a character about 0.65 of the text
// height wide (times the width factor), a line about 1.7 heights tall.
var charMm = textMm * 0.65 * widthFactor;
var lineMm = textMm * 1.7;
var gap = spacingMm;
var symbolHalf = 4.0;
var labelOffset = 2.5;

Func<string, double[]> measure = text =>
{
    var lines = (text ?? "").Split('\r');
    var longest = lines.Max(l => l.Length);
    return new[] { longest * charMm + 1.0, lines.Length * lineMm + 0.5 };
};

int[] DX = { 1, 0, -1, 0 };
int[] DY = { 0, 1, 0, -1 };
var texts = new List<string>();
// An item: { kind, a, b, c, d, e } - kind 0 a line (x1, y1, x2, y2), 1 a circle
// (cx, cy, r), 2 a text (left, top, width, height, index into `texts`).
Func<double, double, double, double, double[]> lineItem = (x1, y1, x2, y2) => new[] { 0.0, x1, y1, x2, y2, 0.0 };
Func<double, double, double, double[]> circleItem = (cx, cy, r) => new[] { 1.0, cx, cy, r, 0.0, 0.0 };
Func<double, double, double, double, string, double[]> textItem = (left, top, w, h, s) =>
{
    texts.Add(s);
    return new[] { 2.0, left, top, w, h, texts.Count - 1.0 };
};
Func<List<double[]>, double, double, IEnumerable<double[]>> shifted = (items, ox, oy) =>
    items.Select(t => t[0] == 0.0 ? new[] { 0.0, t[1] + ox, t[2] + oy, t[3] + ox, t[4] + oy, 0.0 }
        : t[0] == 1.0 ? new[] { 1.0, t[1] + ox, t[2] + oy, t[3], 0.0, 0.0 }
        : new[] { 2.0, t[1] + ox, t[2] + oy, t[3], t[4], t[5] });
// { minX, maxX, minY, maxY } of a list of items.
Func<List<double[]>, double[]> extentOf = items =>
{
    var box = new[] { double.MaxValue, double.MinValue, double.MaxValue, double.MinValue };
    foreach (var t in items)
    {
        double x0, x1, y0, y1;
        if (t[0] == 0.0) { x0 = Math.Min(t[1], t[3]); x1 = Math.Max(t[1], t[3]); y0 = Math.Min(t[2], t[4]); y1 = Math.Max(t[2], t[4]); }
        else if (t[0] == 1.0) { x0 = t[1] - t[3]; x1 = t[1] + t[3]; y0 = t[2] - t[3]; y1 = t[2] + t[3]; }
        else { x0 = t[1]; x1 = t[1] + t[3]; y0 = t[2] - t[4]; y1 = t[2]; }
        box[0] = Math.Min(box[0], x0); box[1] = Math.Max(box[1], x1);
        box[2] = Math.Min(box[2], y0); box[3] = Math.Max(box[3], y1);
    }
    if (items.Count == 0) box = new[] { 0.0, 0.0, 0.0, 0.0 };
    return box;
};

// A node's symbol at (x, y), the run arriving in direction `dir`.
Action<List<double[]>, Element, double, double, int, bool> symbolAt = (items, node, x, y, dir, leaf) =>
{
    var across = (dir + 1) % 4;
    if (isEquipment(node))
    {
        double hx = 4.0, hy = 2.5;
        items.Add(lineItem(x - hx, y - hy, x + hx, y - hy));
        items.Add(lineItem(x + hx, y - hy, x + hx, y + hy));
        items.Add(lineItem(x + hx, y + hy, x - hx, y + hy));
        items.Add(lineItem(x - hx, y + hy, x - hx, y - hy));
    }
    else if (isTerminal(node)) items.Add(circleItem(x, y, 2.0));
    else if (leaf && !openAt.Contains(node.Id))
        items.Add(lineItem(x - 2.0 * DX[across], y - 2.0 * DY[across], x + 2.0 * DX[across], y + 2.0 * DY[across]));
    if (openAt.Contains(node.Id))
    {
        items.Add(lineItem(x - 1.5, y - 1.5, x + 1.5, y + 1.5));
        items.Add(lineItem(x - 1.5, y + 1.5, x + 1.5, y - 1.5));
    }
    if (loopTags.ContainsKey(node.Id))
    {
        foreach (var along in new[] { -0.8, 0.8 })
        {
            var cx = x + along * DX[dir];
            var cy = y + along * DY[dir];
            items.Add(lineItem(cx - 2.5 * DX[across], cy - 2.5 * DY[across], cx + 2.5 * DX[across], cy + 2.5 * DY[across]));
        }
    }
};

// ONE RUN: from `from` (the origin, 0,0) along `dir`, through `first` and the
// child that goes straight on at each node after it. Its branches are runs of
// their own, square to it, placed so nothing on one side overlaps.
Func<Element, Element, int, bool, List<double[]>> layoutRun = null;
layoutRun = (from, first, dir, isRoot) =>
{
    var items = new List<double[]>();
    var left = (dir + 1) % 4;
    var right = (dir + 3) % 4;
    var horizontal = dir == 0 || dir == 2;
    var frontier = new double[4];
    frontier[left] = symbolHalf;
    frontier[right] = symbolHalf;
    var nextLeft = true;

    Func<double, double, int, double[]> at = (along, acrossBy, side) =>
        new[] { along * DX[dir] + acrossBy * DX[side], along * DY[dir] + acrossBy * DY[side] };
    // A text box beside the run: along a0..a1, across c0..c1 on `side`.
    Func<double, double, double, double, int, double[]> boxAt = (a0, a1, c0, c1, side) =>
    {
        var p = at(a0, c0, side);
        var q = at(a1, c1, side);
        return new[] { Math.Min(p[0], q[0]), Math.Max(p[1], q[1]) };
    };
    // A text's size along the run, and across it.
    Func<double[], double[]> alongAcross = wh => horizontal ? new[] { wh[0], wh[1] } : new[] { wh[1], wh[0] };
    Func<double[], double[]> alongRange = ext =>
    {
        var xs = new[] { ext[0], ext[1] };
        var ys = new[] { ext[2], ext[3] };
        var low = double.MaxValue;
        var high = double.MinValue;
        foreach (var x in xs) foreach (var y in ys)
        {
            var a = x * DX[dir] + y * DY[dir];
            low = Math.Min(low, a);
            high = Math.Max(high, a);
        }
        return new[] { low, high };
    };

    // A node's slots - its label when the run goes on past it, then each
    // branch - two to a station, one each side; more than two make the run go
    // on, as a stub of the same line, to the next station. Returns { where the
    // node sits, where the run goes on from }.
    Func<Element, double, List<Element>, bool, double[]> placeSlots = (node, minimum, branches, withLabel) =>
    {
        var slots = new List<object[]>();   // { side, label size or null, branch items or null, along range }
        if (withLabel)
        {
            var label = nodeLabelOf(node);
            if (label.Length > 0)
            {
                var wh = measure(label);
                var aa = alongAcross(wh);
                slots.Add(new object[] { 0, label, wh, null, new[] { -aa[0] / 2, aa[0] / 2 } });
            }
        }
        foreach (var branch in branches)
            slots.Add(new object[] { 0, null, null, branch, null });
        for (var s = 0; s < slots.Count; s++)
        {
            slots[s][0] = nextLeft ? left : right;
            nextLeft = !nextLeft;
            if (slots[s][3] != null)
            {
                var side = (int)slots[s][0];
                var block = layoutRun(node, (Element)slots[s][3], side, false);
                slots[s][3] = block;
                slots[s][4] = alongRange(extentOf(block));
            }
        }
        var position = minimum;
        var stationAt = minimum;
        for (var station = 0; station * 2 < slots.Count || station == 0; station++)
        {
            var need = station == 0 ? minimum : position + gap;
            for (var s = station * 2; s < Math.Min(slots.Count, station * 2 + 2); s++)
            {
                var side = (int)slots[s][0];
                var range = (double[])slots[s][4];
                need = Math.Max(need, frontier[side] + gap - range[0]);
            }
            position = need;
            if (station == 0) stationAt = need;
            for (var s = station * 2; s < Math.Min(slots.Count, station * 2 + 2); s++)
            {
                var side = (int)slots[s][0];
                var range = (double[])slots[s][4];
                if (slots[s][1] != null)
                {
                    var wh = (double[])slots[s][2];
                    var aa = alongAcross(wh);
                    var corner = boxAt(position - aa[0] / 2, position + aa[0] / 2, labelOffset + symbolHalf * 0.5,
                        labelOffset + symbolHalf * 0.5 + aa[1], side);
                    items.Add(textItem(corner[0], corner[1], wh[0], wh[1], (string)slots[s][1]));
                }
                else
                {
                    var origin = at(position, 0, left);
                    items.AddRange(shifted((List<double[]>)slots[s][3], origin[0], origin[1]));
                }
                frontier[side] = position + range[1];
            }
            if (slots.Count == 0) break;
        }
        return new[] { stationAt, position };
    };

    var pos = 0.0;
    if (isRoot)
    {
        symbolAt(items, from, 0, 0, dir, false);
        var label = nodeLabelOf(from);
        if (label.Length > 0)
        {
            var wh = measure(label);
            var aa = alongAcross(wh);
            var corner = boxAt(-(symbolHalf + 1.5 + aa[0]), -(symbolHalf + 1.5), -aa[1] / 2, aa[1] / 2, left);
            items.Add(textItem(corner[0], corner[1], wh[0], wh[1], label));
        }
        var rootBranches = nodeChildren[from.Id].Where(k => first == null || k.Id != first.Id).ToList();
        pos = placeSlots(from, 0, rootBranches, false)[1];
        items.Add(new[] { 3.0, pos, 0, 0, 0, 0 });
        nodes++;
        if (isTerminal(from)) terminals++;
    }

    var previous = from;
    var node = first;
    while (node != null)
    {
        // The segments of the line to this node, each with its label beside
        // it on the side with less on it so far.
        var groups = segmentsTo(previous, node);
        var marks = new List<double>();
        foreach (var group in groups)
        {
            var hasRun = group.Any(e => e is MEPCurve);
            var segmentStart = pos;
            if (!hasRun) { pos = pos + gap; continue; }
            var label = labelOf(group);
            segments++;
            var wh = measure(label);
            var aa = alongAcross(wh);
            var side = frontier[left] <= frontier[right] ? left : right;
            var a0 = Math.Max(pos + symbolHalf + 0.5, frontier[side] + gap);
            var a1 = a0 + aa[0];
            var corner = boxAt(a0, a1, labelOffset, labelOffset + aa[1], side);
            items.Add(textItem(corner[0], corner[1], wh[0], wh[1], label));
            frontier[side] = a1;
            pos = Math.Max(a1 + symbolHalf + 0.5, segmentStart + gap);
            marks.Add(pos);
        }
        if (groups.Count == 0) pos = pos + gap;
        // Size-change ticks between this edge's segments.
        if (marks.Count > 1)
        {
            for (var m = 0; m < marks.Count - 1; m++)
            {
                var a = at(marks[m], 1.5, left);
                var b = at(marks[m], -1.5, left);
                items.Add(lineItem(a[0], a[1], b[0], b[1]));
            }
        }

        var next = straightOn(node);
        var branches = nodeChildren[node.Id].Where(k => next == null || k.Id != next.Id).ToList();
        var leaf = nodeChildren[node.Id].Count == 0;
        var placed = placeSlots(node, pos, branches, !leaf);
        var nodeAt = placed[0];
        nodes++;
        if (isTerminal(node)) terminals++;
        var nodePoint = at(nodeAt, 0, left);
        symbolAt(items, node, nodePoint[0], nodePoint[1], dir, leaf);
        if (leaf)
        {
            var label = nodeLabelOf(node);
            if (label.Length > 0)
            {
                var wh = measure(label);
                var aa = alongAcross(wh);
                var corner = boxAt(nodeAt + symbolHalf + 1.5, nodeAt + symbolHalf + 1.5 + aa[0], -aa[1] / 2,
                    aa[1] / 2, left);
                items.Add(textItem(corner[0], corner[1], wh[0], wh[1], label));
            }
        }
        // How far this run's line reaches - to the last station of this node.
        pos = placed[1];
        items.Add(new[] { 3.0, pos, 0, 0, 0, 0 });
        previous = node;
        node = next;
    }
    // ONE straight line along the run from the origin to the last node it
    // reached: every node and segment of this run lies on it.
    var lastAlong = 0.0;
    foreach (var t in items) if (t[0] == 3.0) lastAlong = Math.Max(lastAlong, t[1]);
    items.RemoveAll(t => t[0] == 3.0);
    if (lastAlong > 0)
    {
        var end = at(lastAlong, 0, left);
        items.Add(lineItem(0, 0, end[0], end[1]));
    }
    return items;
};

var drawing = new List<double[]>();
if (refused.Length == 0)
    drawing = layoutRun(root, straightOn(root), 0, true);

// ---- 7. the view, then the drawing -----------------------------------------

ViewDrafting view = null;
var linesMade = 0;
var notesMade = 0;
var tooShort = 0;
var failedCurves = 0;
var failedNotes = 0;
var createdNotes = new List<TextNote>();
if (refused.Length == 0)
{
    // The name: the one asked for, else "Schematic - <system>"; characters
    // Revit refuses in a view name become "-"; an existing name gets " (2)",
    // " (3)" ... - an existing view is never touched.
    var wanted = viewWanted.Length > 0 ? viewWanted : "Schematic - " + nameOf(system);
    foreach (var bad in new[] { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '`', '~' })
        wanted = wanted.Replace(bad, '-');
    wanted = wanted.Trim();
    if (wanted.Length == 0) wanted = "Schematic";
    var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var existing in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        try { if (existing != null) taken.Add((existing.Name ?? "").Trim()); } catch (Exception) { }
    }
    var chosen = wanted;
    for (var n = 2; taken.Contains(chosen); n++) chosen = string.Format("{0} ({1})", wanted, n);
    if (!string.Equals(chosen, wanted, StringComparison.Ordinal))
        findings.Add(string.Format("a view called \"{0}\" already exists and was left alone - the schematic "
            + "is \"{1}\"", wanted, chosen));

    view = ViewDrafting.Create(doc, draftingType.Id);
    if (view == null) refused = "Revit declined to make the drafting view. Nothing was drawn";
    else
    {
        string nameProblem = null;
        try { view.Scale = viewScale; view.Name = chosen; }
        catch (Exception ex) { nameProblem = ex.Message; }
        if (nameProblem != null)
        {
            try { doc.Delete(view.Id); } catch (Exception) { }
            view = null;
            refused = string.Format("Revit would not give the new view the name \"{0}\" or the scale 1:{1} "
                + "({2}), so it was taken out again. Nothing was drawn", chosen, viewScale, nameProblem);
        }
        else { viewId = view.Id; viewName = chosen; }
    }
}

if (view != null)
{
    var toFeet = viewScale / MillimetresPerFoot;
    var shortest = doc.Application.ShortCurveTolerance;
    // The curve is BUILT inside the try as well: Revit refusing to make a curve
    // is counted, never let out to roll the whole call back.
    Action<Func<Curve>> make = build =>
    {
        try
        {
            var curve = build();
            if (curve == null || curve.Length < shortest) { tooShort++; return; }
            var made = doc.Create.NewDetailCurve(view, curve);
            if (made != null) linesMade++; else failedCurves++;
        }
        catch (Exception) { failedCurves++; }
    };

    // The legend, above the drawing.
    var extent = extentOf(drawing);
    var systemType = "";
    try { var type = doc.GetElement(system.GetTypeId()); systemType = type != null ? type.Name : ""; }
    catch (Exception) { systemType = ""; }
    var legend = new List<string>
    {
        "SCHEMATIC - " + nameOf(system) + (systemType.Length > 0 ? " (" + systemType + ")" : ""),
        "NOT TO SCALE - positions and lengths are not the model's. Drawn by Heron from the model on "
            + DateTime.Now.ToString("yyyy-MM-dd") + "; it does not change when the model does.",
        "Each run: size / Q flow / Δp pressure drop - Revit's own values, read from the model, not calculated by Heron.",
        "Δp of a run: the Pressure Drop Revit holds on its " + (isDuct ? "ducts" : "pipes")
            + ", elbows, transitions and accessories, added. Tees and crosses are not included.",
        "O terminal   [ ] equipment   X open end   L1 loop cut (the joint is real; drawn as two ends)",
    };
    if (sectioned == 0)
        legend.Add("Δp not calculated: Revit has no calculation for this system.");
    var legendText = string.Join("\r", legend);
    var legendSize = measure(legendText);
    drawing.Add(textItem(extent[0], extent[3] + gap + legendSize[1], legendSize[0], legendSize[1], legendText));

    foreach (var t in drawing)
    {
        if (t[0] == 0.0)
        {
            var a = new XYZ(t[1] * toFeet, t[2] * toFeet, 0);
            var b = new XYZ(t[3] * toFeet, t[4] * toFeet, 0);
            if (a.DistanceTo(b) < shortest) { tooShort++; continue; }
            make(() => Line.CreateBound(a, b));
        }
        else if (t[0] == 1.0)
        {
            var centre = new XYZ(t[1] * toFeet, t[2] * toFeet, 0);
            var radius = t[3] * toFeet;
            // Two half arcs - a closed circle is not one bound curve.
            if (radius * Math.PI < shortest) { tooShort += 2; continue; }
            make(() => Arc.Create(centre, radius, 0, Math.PI, XYZ.BasisX, XYZ.BasisY));
            make(() => Arc.Create(centre, radius, Math.PI, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY));
        }
        else if (t[0] == 2.0)
        {
            var text = texts[(int)t[5]];
            try
            {
                using (var options = new TextNoteOptions(noteType.Id))
                {
                    options.HorizontalAlignment = HorizontalTextAlignment.Left;
                    options.VerticalAlignment = VerticalTextAlignment.Top;
                    var note = TextNote.Create(doc, view.Id, new XYZ(t[1] * toFeet, t[2] * toFeet, 0), text, options);
                    if (note != null) { notesMade++; createdNotes.Add(note); } else failedNotes++;
                }
            }
            catch (Exception) { failedNotes++; }
        }
    }

    // READ BACK: every label's real box, and any two that touch.
    var touching = 0;
    try
    {
        doc.Regenerate();
        var boxes = new List<double[]>();
        foreach (var note in createdNotes)
        {
            var box = note.get_BoundingBox(view);
            if (box != null) boxes.Add(new[] { box.Min.X, box.Min.Y, box.Max.X, box.Max.Y });
        }
        boxes = boxes.OrderBy(b => b[0]).ToList();
        var counted = new HashSet<int>();
        for (var i = 0; i < boxes.Count; i++)
        {
            for (var j = i + 1; j < boxes.Count && boxes[j][0] < boxes[i][2]; j++)
            {
                if (boxes[j][1] < boxes[i][3] && boxes[i][1] < boxes[j][3])
                {
                    counted.Add(i);
                    counted.Add(j);
                }
            }
        }
        touching = counted.Count;
        findings.Add(touching == 0
            ? string.Format("read back: none of the {0} text notes touches another", boxes.Count)
            : string.Format("read back: {0} of the {1} text notes touch another - the size each text was "
                + "estimated at was too small for this text type. Try a larger spacingMm", touching, boxes.Count));
    }
    catch (Exception ex)
    {
        findings.Add("the text notes' boxes could not be read back (" + ex.Message + "), so whether any "
            + "two touch is NOT KNOWN");
    }
}

// ---- 8. what it says -------------------------------------------------------

if (refused.Length == 0 && view != null)
{
    findings.Insert(0, string.Format("system \"{0}\" drawn on the new drafting view \"{1}\" at 1:{2}, NOT TO "
        + "SCALE: {3} node(s), {4} labelled segment(s), {5} terminal(s), from {6}. {7} detail line(s) and {8} "
        + "text note(s); nothing in the model was changed", nameOf(system), viewName, viewScale, nodes, segments,
        terminals, describe(root), linesMade, notesMade));
    if (outsideJoints.Count > 0)
        findings.Add(string.Format("{0} element(s) of another system joined to this one were not followed - "
            + "the drawing stops at the system's edge", outsideJoints.Count));
    if (openEnds.Count > 0)
        findings.Add(string.Format("{0} open end(s) on {1} element(s), each marked X on the drawing", openConnectors,
            openEnds.Count));
    if (loopsCut.Count > 0)
        findings.Add(string.Format("{0} loop(s) cut to draw the system as a tree - each joint is real in the "
            + "model and marked at both ends (loopsCut)", loopsCut.Count));
    if (tooShort > 0)
        findings.Add(string.Format("{0} line(s) shorter than Revit draws were left out - at 1:{1} a symbol of a "
            + "few paper mm is too small; a larger scale number draws them", tooShort, viewScale));
    if (failedCurves > 0)
        findings.Add(string.Format("{0} line(s) Revit would not draw", failedCurves));
    if (failedNotes > 0)
        findings.Add(string.Format("{0} label(s) Revit would not place - the drawing is missing them", failedNotes));
    findings.Add("text type: \"" + noteType.Name + "\"" + (textWanted.Length > 0 ? "" : " (the project's default)"));
    findings.Add("linked models are not read - a system in a link is not found");
    findings.Add(string.Format("took {0:0.0} s", (DateTime.UtcNow - began).TotalSeconds));
}
