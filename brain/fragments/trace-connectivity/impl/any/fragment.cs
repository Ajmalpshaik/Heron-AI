// NOT STANDALONE. Assumes `start`, `elements` and `tolerance` are in scope;
// leaves `reached`, `openEnds`, `joinedByGeometry`, `islandOf`, `islandSizes`,
// `sourceless` and `islandCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO QUESTIONS, ONE TOOL (version 2, 2026-11-01). FIND_SYSTEM_ISLANDS, which
// split a whole set into its separate networks, is DEPRECATED and merged into
// this card:
//
//   `start` given      "what does this reach" - the walk outward from it, as
//                      version 1 did, unchanged
//   `start` left out   "how many pieces is this in" - the WHOLE SET split into
//                      its networks, each one's size, and the networks no
//                      equipment feeds. A walk from one element never learns an
//                      orphaned branch exists; only a partition of the set does
//
// THE OPPOSITE RULE, AND WHICH WAS KEPT. FIND_SYSTEM_ISLANDS trusted Revit's own
// joins and said so; this card also follows connectors touching within the
// tolerance. In the partition they are KEPT APART, never added into one count:
// the islands are Revit's own joins - what "why is the flow not calculating"
// needs - and every end Revit leaves open that another element of the set
// touches within the tolerance is counted in `joinedByGeometry` beside them,
// never used to merge two islands. Two ducts that touch perfectly and are
// joined to nothing read as two islands AND one touching joint, both said.
//
// TOLERANCE ARRIVES IN MILLIMETRES and is divided by 304.8 below (D-71), and
// it is an input (D-33). No default. (This line said "TOLERANCE IS IN INTERNAL
// FEET" until 2026-09-24; the division went in on 2026-09-13.)
//
// WHY THIS DOES NOT TRUST Connector.IsConnected.
//
// Revit's own data describes INTENT, not always physical reality. On a real
// model, tag names implied one pairing while the pipework ran to another, and
// `IsConnected` reported false end to end on a run that was visibly joined.
// Both of the obvious answers were wrong at the same time, on the same model,
// and only walking the actual connector POSITIONS got the truth out.
//
// So this fragment does both and keeps them apart:
//
//   the declared route   Connector.AllRefs - what Revit says is joined
//   the geometric route  another connector at the same point, within tolerance
//
// `joinedByGeometry` counts the second finding something the first denied. It
// is not a diagnostic left in by accident: a run where that number is not zero
// is a run whose system data cannot be trusted for anything else either, and
// the person reading the report needs to know that about their model.
//
// KEYED BY UniqueId, NOT ElementId. A string that is stable on every release
// from 2020 to 2027, which sidesteps 2024's widening of ElementId rather than
// assuming it hashes the same way on both sides of that change.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
tolerance = tolerance / MillimetresPerFoot;

Func<Element, ConnectorManager> managerOf = e =>
{
    var curve = e as MEPCurve;
    if (curve != null) return curve.ConnectorManager;

    var instance = e as FamilyInstance;
    if (instance != null && instance.MEPModel != null)
        return instance.MEPModel.ConnectorManager;

    // Not an MEP element at all. Not an error - a trace crossing one simply
    // stops there, and stopping is the honest outcome.
    return null;
};

var reached = new List<Element>();
var seen = new HashSet<string>();
var openEnds = 0;
var joinedByGeometry = 0;

// VERSION 2 - the whole-set partition's answers. Empty when `start` is given.
var islandOf = new Dictionary<ElementId, int>();
var islandSizes = new Dictionary<int, int>();
var sourceless = new List<int>();
var islandCount = 0;

var queue = new Queue<Element>();
if (start != null)
{
    queue.Enqueue(start);
    seen.Add(start.UniqueId);
}
else
{
    // =====================================================================
    // THE WHOLE SET, SPLIT INTO ITS NETWORKS (FIND_SYSTEM_ISLANDS' job).
    // =====================================================================
    //
    // AN ISLAND WITH NO SOURCE IS THE FINDING, NOT THE ISLAND COUNT. Two
    // networks is normal - supply and return are two. A network containing no
    // equipment is a run connected to nothing that can feed it. Size is
    // reported per island because it sorts the problem: a sourceless island of
    // two is a leftover to delete, one of ninety a main run that lost the plant.
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

    // Equipment is what makes an island fed, recognised by CATEGORY because "a
    // source" is several categories and no single class covers them.
    // PLUMBING EQUIPMENT is a category only from the 2023.1 API on, so it is
    // looked up BY NAME AT RUN TIME - the spelled-out member does not compile
    // on 2020 to 2022, and a version #if would never be the branch that ran
    // (FRAGMENT-ISSUES 5b-181).
    var sourceCategories = new List<BuiltInCategory>
    {
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_ElectricalEquipment,
        BuiltInCategory.OST_PlumbingFixtures,
    };
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

    // REVIT'S OWN JOINS ONLY, inside the set - an island is a partition OF THE
    // SET, and wandering out of it would make the count depend on the rest of
    // the model.
    foreach (var element in elements)
    {
        if (element == null || islandOf.ContainsKey(element.Id)) continue;

        int island = islandCount++;
        int size = 0;
        bool fed = false;

        var pending = new Stack<Element>();
        pending.Push(element);
        islandOf[element.Id] = island;

        while (pending.Count > 0)
        {
            var here = pending.Pop();
            size++;
            reached.Add(here);
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

    // TOUCHING WITHIN THE TOLERANCE, COUNTED APART. Every physical end Revit
    // leaves open is looked at once: another element of the set with a
    // connector there is `joinedByGeometry`, and nothing there is `openEnds`.
    // Neither changes an island.
    var farEnds = new List<KeyValuePair<string, XYZ>>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        var manager = managerOf(element);
        if (manager == null) continue;
        foreach (Connector far in manager.Connectors)
        {
            if (far.ConnectorType != ConnectorType.End &&
                far.ConnectorType != ConnectorType.Curve) continue;
            farEnds.Add(new KeyValuePair<string, XYZ>(element.UniqueId, far.Origin));
        }
    }

    var looked = new HashSet<string>();
    foreach (var element in elements)
    {
        if (element == null || !looked.Add(element.UniqueId)) continue;
        var manager = managerOf(element);
        if (manager == null) continue;

        foreach (Connector connector in manager.Connectors)
        {
            if (connector.ConnectorType != ConnectorType.End &&
                connector.ConnectorType != ConnectorType.Curve) continue;

            var declared = false;
            foreach (Connector other in connector.AllRefs)
            {
                var owner = other.Owner;
                if (owner == null || owner.UniqueId == element.UniqueId) continue;
                if (other.ConnectorType != ConnectorType.End &&
                    other.ConnectorType != ConnectorType.Curve) continue;
                declared = true;
                break;
            }
            if (declared) continue;

            var origin = connector.Origin;
            var touching = false;
            foreach (var far in farEnds)
            {
                if (far.Key == element.UniqueId) continue;
                if (far.Value.DistanceTo(origin) > tolerance) continue;
                touching = true;
                break;
            }

            if (touching) joinedByGeometry++;
            else openEnds++;
        }
    }
}

while (queue.Count > 0)
{
    var current = queue.Dequeue();
    reached.Add(current);

    var manager = managerOf(current);
    if (manager == null) continue;

    foreach (Connector connector in manager.Connectors)
    {
        // A logical connector describes system membership rather than a
        // physical join. Following one would report a whole system as
        // "connected" to everything in it, which is the question nobody asked.
        if (connector.ConnectorType != ConnectorType.End &&
            connector.ConnectorType != ConnectorType.Curve) continue;

        var wentSomewhere = false;

        foreach (Connector other in connector.AllRefs)
        {
            var owner = other.Owner;
            if (owner == null || owner.UniqueId == current.UniqueId) continue;
            if (other.ConnectorType != ConnectorType.End &&
                other.ConnectorType != ConnectorType.Curve) continue;

            wentSomewhere = true;
            if (seen.Add(owner.UniqueId)) queue.Enqueue(owner);
        }

        if (wentSomewhere) continue;

        // THE DECLARED ROUTE FOUND NOTHING. Before calling this an open end,
        // look where the flag cannot: is another candidate's connector sitting
        // at this same point?
        var foundByGeometry = false;
        var origin = connector.Origin;

        foreach (var candidate in elements)
        {
            if (candidate == null || candidate.UniqueId == current.UniqueId) continue;

            var otherManager = managerOf(candidate);
            if (otherManager == null) continue;

            foreach (Connector far in otherManager.Connectors)
            {
                if (far.ConnectorType != ConnectorType.End &&
                    far.ConnectorType != ConnectorType.Curve) continue;
                if (far.Origin.DistanceTo(origin) > tolerance) continue;

                foundByGeometry = true;
                if (seen.Add(candidate.UniqueId)) queue.Enqueue(candidate);
                break;
            }

            if (foundByGeometry) break;
        }

        if (foundByGeometry) joinedByGeometry++;
        else openEnds++;
    }
}
