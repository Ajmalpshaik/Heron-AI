// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `islandOf`, `islandSizes`, `sourceless` and `islandCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A SYSTEM THAT LOOKS LIKE ONE NETWORK IS VERY OFTEN THREE.
//
// The main run, a branch copied and never rejoined, and a spur that lost its
// fitting. On the drawing all three read as one system. The air arrives in one.
//
// NOT TRACE_CONNECTIVITY. That walks outward from ONE element and answers "what
// does this reach". This partitions the WHOLE SET, which no single walk can do:
// a walk starting inside the main run never learns the orphan exists.
//
// THE FINDING IS A SOURCELESS ISLAND, NOT THE COUNT.
//
// Two networks is normal - supply and return are two. A network containing no
// equipment at all is a run connected to nothing that can feed it, and that is
// a defect every time. Size is reported per island because it sorts the
// problem: a sourceless island of two is a leftover to delete, one of ninety is
// a main run that lost the plant.
//
// CONNECTOR JOINS ARE TRUSTED HERE, AND THAT IS A LIMIT WORTH SAYING.
//
// Revit's own connection data describes INTENT and has been proven wrong on a
// real model - two ducts can touch perfectly and be joined to nothing. This
// reports the network as Revit believes it, which is right for "why is the flow
// not calculating" and wrong for "is it physically joined".

var islandOf = new Dictionary<ElementId, int>();
var islandSizes = new Dictionary<int, int>();
var sourceless = new List<int>();

var inSet = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element != null) inSet.Add(element.Id);
}

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

// Equipment is what makes an island fed. Recognised by CATEGORY because "a
// source" is THREE different categories and no single class covers them - not
// because the classes are out of reach. Mechanical, Plumbing, Electrical and
// Structure are all imported here; only Architecture is not.
var sourceCategories = new List<BuiltInCategory>
{
    BuiltInCategory.OST_MechanicalEquipment,
    BuiltInCategory.OST_ElectricalEquipment,
    BuiltInCategory.OST_PlumbingFixtures,
};

// PLUMBING EQUIPMENT - a water heater, a pump, a tank - is a category of its
// own only in the newer releases: the API names it from the 2023.1 reference
// assemblies on, and not in 2020, 2021 or 2022. It feeds a network, and until
// 2026-09-23 this list did not know the category existed, so a domestic water
// network fed by one read as SOURCELESS. It is looked up BY NAME AT RUN TIME
// rather than spelled BuiltInCategory.OST_PlumbingEquipment, because that
// spelling does not compile on the older releases - and a version #if cannot
// stand in: the add-in compiles fragments with no release symbols defined
// (FRAGMENT-ISSUES 5b-181), so the #if branch would never be the one that ran.
BuiltInCategory plumbingEquipment;
if (Enum.TryParse("OST_PlumbingEquipment", out plumbingEquipment))
    sourceCategories.Add(plumbingEquipment);

Func<Element, bool> isSource = element =>
{
    try
    {
        if (element.Category == null) return false;
        foreach (var category in sourceCategories)
        {
            if (element.Category.Id == new ElementId(category)) return true;
        }
    }
    catch { }
    return false;
};

int nextIsland = 0;

foreach (var element in elements)
{
    if (element == null || islandOf.ContainsKey(element.Id)) continue;

    // Flood fill from this element through joined connectors, staying inside
    // the passed set - an island is a partition OF THE SET, and wandering out
    // of it would make the count depend on the rest of the model.
    int island = nextIsland++;
    int size = 0;
    bool fed = false;

    var pending = new Stack<Element>();
    pending.Push(element);
    islandOf[element.Id] = island;

    while (pending.Count > 0)
    {
        var here = pending.Pop();
        size++;
        if (isSource(here)) fed = true;

        var connectors = connectorsOf(here);
        if (connectors == null) continue;

        foreach (Connector connector in connectors)
        {
            if (connector == null) continue;
            ConnectorSet joined = null;
            try { joined = connector.AllRefs; } catch { }
            if (joined == null) continue;

            foreach (Connector other in joined)
            {
                if (other == null || other.Owner == null) continue;
                var neighbour = other.Owner;
                if (neighbour.Id == here.Id) continue;
                if (!inSet.Contains(neighbour.Id)) continue;
                if (islandOf.ContainsKey(neighbour.Id)) continue;

                islandOf[neighbour.Id] = island;
                pending.Push(neighbour);
            }
        }
    }

    islandSizes[island] = size;
    if (!fed) sourceless.Add(island);
}

int islandCount = nextIsland;
