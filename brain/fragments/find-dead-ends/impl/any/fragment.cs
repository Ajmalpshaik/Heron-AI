// NOT STANDALONE. Assumes `elements` and `stubLength` are in scope, and leaves
// `deadEnds`, `stubs`, `served`, `capped` and `openEndsFound` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// MOST OPEN ENDS ARE MEANT TO BE THERE.
//
// A run ending at an air terminal, a fixture, equipment or a cap is FINISHED,
// not broken. Report those as faults and the sweep gets switched off within a
// week - which is the same outcome as never writing it. So every open end is
// classified and only the unexplained ones are the finding.
//
// "SERVES NOTHING" IS DECIDED BY WALKING, NOT BY THE LAST ELEMENT.
//
// A branch can end at a fitting that ends at another fitting that ends at
// nothing. Judging the last element alone calls that chain SERVED, because a
// fitting is something. The walk follows the chain from each open end until it
// finds something served or runs out of elements.
//
// A STUB IS REPORTED SEPARATELY RATHER THAN SUPPRESSED. A short open spur is
// usually deliberate and occasionally the whole problem. Folded into deadEnds
// it is a false alarm; dropped it is a missed one.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
stubLength = stubLength / MillimetresPerFoot;

var deadEnds = new List<ElementId>();
var stubs = new List<ElementId>();
var served = new List<ElementId>();
var capped = new List<ElementId>();
int openEndsFound = 0;

var terminalCategories = new List<BuiltInCategory>
{
    BuiltInCategory.OST_DuctTerminal,
    BuiltInCategory.OST_PlumbingFixtures,
    BuiltInCategory.OST_MechanicalEquipment,
    BuiltInCategory.OST_Sprinklers,
};

// PLUMBING EQUIPMENT - a water heater, a pump, a tank - is a category of its
// own only in the newer releases: the API names it from the 2023.1 reference
// assemblies on, and not in 2020, 2021 or 2022. A run ending at one is served,
// and until 2026-09-23 this list did not know the category existed, so every
// such run came back a dead end. It is looked up BY NAME AT RUN TIME rather
// than spelled BuiltInCategory.OST_PlumbingEquipment, because that spelling
// does not compile on the older releases - and a version #if cannot stand in:
// the add-in compiles fragments with no release symbols defined
// (FRAGMENT-ISSUES 5b-169), so the #if branch would never be the one that ran.
BuiltInCategory plumbingEquipment;
if (Enum.TryParse("OST_PlumbingEquipment", out plumbingEquipment))
    terminalCategories.Add(plumbingEquipment);

Func<Element, bool> servesSomething = element =>
{
    try
    {
        if (element.Category == null) return false;
        foreach (var category in terminalCategories)
        {
            if (element.Category.Id == new ElementId(category)) return true;
        }
    }
    catch { }
    return false;
};

Func<Element, bool> isCap = element =>
{
    try
    {
        var instance = element as FamilyInstance;
        if (instance == null || instance.MEPModel == null) return false;
        // A cap is the one fitting with a single connector. Recognised by shape
        // rather than by name, which is localised and set by whoever built the
        // family.
        var connectors = instance.MEPModel.ConnectorManager.Connectors;
        return connectors != null && connectors.Size == 1;
    }
    catch { }
    return false;
};

Func<Element, ConnectorSet> connectorsOf = element =>
{
    try
    {
        var curve = element as MEPCurve;
        if (curve != null) return curve.ConnectorManager.Connectors;
        var instance = element as FamilyInstance;
        if (instance != null && instance.MEPModel != null)
            return instance.MEPModel.ConnectorManager.Connectors;
    }
    catch { }
    return null;
};

Func<Element, double> lengthOf = element =>
{
    try
    {
        var p = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        if (p != null && p.HasValue) return p.AsDouble();
    }
    catch { }
    return 0.0;
};

foreach (var element in elements)
{
    if (element == null) continue;
    var connectors = connectorsOf(element);
    if (connectors == null) continue;

    bool hasOpenEnd = false;
    foreach (Connector connector in connectors)
    {
        if (connector == null) continue;
        bool connected = true;
        try { connected = connector.IsConnected; } catch { }
        if (!connected) { hasOpenEnd = true; break; }
    }

    if (!hasOpenEnd) continue;
    openEndsFound++;

    if (servesSomething(element)) { served.Add(element.Id); continue; }
    if (isCap(element)) { capped.Add(element.Id); continue; }

    // Walk the chain from here. A fitting is not an answer - what matters is
    // whether anything ANYWHERE along the chain serves something.
    bool reachesSomething = false;
    var visited = new HashSet<ElementId>();
    var pending = new Stack<Element>();
    pending.Push(element);
    visited.Add(element.Id);

    while (pending.Count > 0 && !reachesSomething)
    {
        var here = pending.Pop();
        var theirs = connectorsOf(here);
        if (theirs == null) continue;

        foreach (Connector connector in theirs)
        {
            if (connector == null) continue;
            ConnectorSet joined = null;
            try { joined = connector.AllRefs; } catch { }
            if (joined == null) continue;

            foreach (Connector other in joined)
            {
                if (other == null || other.Owner == null) continue;
                var neighbour = other.Owner;
                if (!visited.Add(neighbour.Id)) continue;

                if (servesSomething(neighbour) || isCap(neighbour))
                {
                    reachesSomething = true;
                    break;
                }
                pending.Push(neighbour);
            }
            if (reachesSomething) break;
        }
    }

    if (reachesSomething) { served.Add(element.Id); continue; }

    if (lengthOf(element) > 0.0 && lengthOf(element) <= stubLength) stubs.Add(element.Id);
    else deadEnds.Add(element.Id);
}
