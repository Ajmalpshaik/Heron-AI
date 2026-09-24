// NOT STANDALONE. Assumes `start`, `elements` and `tolerance` are in scope;
// leaves `reached`, `openEnds` and `joinedByGeometry` behind.
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

var queue = new Queue<Element>();
queue.Enqueue(start);
seen.Add(start.UniqueId);

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
