// NOT STANDALONE. Assumes `doc`, `elements`, `accessorySymbol` and
// `positionAlong` are in scope, and leaves `placed`, `leftSevered`,
// `notARun` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). It matters more here than
// usual: a half-done insert leaves a severed system, and one undo has to put
// the run back.
//
// HOSTING THE ACCESSORY ON THE RUN DOES NOT BREAK THE RUN.
//
// The obvious call takes the run as a host and looks exactly like the Revit
// command that breaks it. Measured: the duct count does not change, the
// accessory has no host, and it sits loose on the duct with neither connector
// joined - and nothing throws. The sequence that works is break, place,
// connect.
//
// THE BREAK SWAPS WHICH HALF KEEPS THE ID.
//
// The original id stays with the SECOND half; the first half is the new one.
// Anything holding an element across the break is then holding the wrong one,
// so both halves are re-fetched by id.
//
// THE BREAK DOES NOT JOIN THE HALVES TO EACH OTHER.
//
// That gap is where the accessory goes. If the placement then fails, the run
// is left in two pieces - which is reported with both ids rather than
// swallowed, because a severed system that nobody knows about is the worst
// outcome available here.
//
// ENDS ARE PAIRED BY DISTANCE.
//
// Which connector of the accessory faces which half depends on how the family
// was built and which way the duct was drawn. Nearest free end is the only
// rule that holds for both.

var placed = new List<ElementId>();
var leftSevered = new List<ElementId>();
var notARun = new List<ElementId>();
var refused = new List<ElementId>();

double along = positionAlong;
if (along <= 0 || along >= 1) along = 0.5;

if (accessorySymbol != null && !accessorySymbol.IsActive)
{
    try { accessorySymbol.Activate(); doc.Regenerate(); } catch { }
}

Func<Element, List<Connector>> freeConnectorsOf = element =>
{
    var free = new List<Connector>();
    ConnectorManager manager = null;
    var curve = element as MEPCurve;
    if (curve != null) { try { manager = curve.ConnectorManager; } catch { } }
    var instance = element as FamilyInstance;
    if (manager == null && instance != null && instance.MEPModel != null)
    {
        try { manager = instance.MEPModel.ConnectorManager; } catch { }
    }
    if (manager == null) return free;

    ConnectorSet set = null;
    try { set = manager.Connectors; } catch { }
    if (set == null) return free;

    foreach (Connector connector in set)
    {
        if (connector == null) continue;
        bool joined = false;
        try
        {
            foreach (Connector reference in connector.AllRefs)
            {
                if (reference == null || reference.Owner == null) continue;
                if (reference.Owner.Id == element.Id) continue;
                if (reference.Owner is MEPSystem) continue;
                joined = true;
                break;
            }
        }
        catch { }
        if (!joined) free.Add(connector);
    }
    return free;
};

foreach (var element in elements)
{
    var run = element as MEPCurve;
    if (run == null || accessorySymbol == null)
    {
        if (element != null) notARun.Add(element.Id);
        continue;
    }

    var location = run.Location as LocationCurve;
    Curve line = location != null ? location.Curve : null;
    if (line == null) { notARun.Add(run.Id); continue; }

    XYZ point;
    try { point = line.Evaluate(along, true); }
    catch { refused.Add(run.Id); continue; }

    var runId = run.Id;
    bool isDuct = run is Duct;
    bool isPipe = run is Pipe;
    if (!isDuct && !isPipe) { notARun.Add(run.Id); continue; }

    // The break. Ducts and pipes have separate utilities and no shared call.
    ElementId otherHalfId = ElementId.InvalidElementId;
    try
    {
        // Ducts and pipes break through their own utility - there is no
        // shared call, and each namespace is already in scope for a fragment.
        otherHalfId = isDuct
            ? MechanicalUtils.BreakCurve(doc, runId, point)
            : PlumbingUtils.BreakCurve(doc, runId, point);
    }
    catch { refused.Add(run.Id); continue; }

    if (otherHalfId == ElementId.InvalidElementId) { refused.Add(run.Id); continue; }

    // Re-fetched by id: the original id now belongs to the SECOND half.
    var halfOne = doc.GetElement(runId);
    var halfTwo = doc.GetElement(otherHalfId);

    if (halfOne == null || halfTwo == null)
    {
        leftSevered.Add(runId);
        continue;
    }

    FamilyInstance accessory = null;
    try
    {
        accessory = doc.Create.NewFamilyInstance(
            point, accessorySymbol, StructuralType.NonStructural);
    }
    catch { }

    if (accessory == null)
    {
        // The run is in two pieces and nothing bridges them. Reported with the
        // ids, never swallowed.
        leftSevered.Add(runId);
        leftSevered.Add(otherHalfId);
        continue;
    }

    doc.Regenerate();

    var accessoryEnds = freeConnectorsOf(accessory);
    int joins = 0;

    foreach (var half in new Element[] { halfOne, halfTwo })
    {
        var halfEnds = freeConnectorsOf(half);
        Connector bestHalf = null, bestAccessory = null;
        double bestDistance = double.MaxValue;

        foreach (var halfEnd in halfEnds)
        {
            foreach (var accessoryEnd in accessoryEnds)
            {
                double distance;
                try { distance = halfEnd.Origin.DistanceTo(accessoryEnd.Origin); }
                catch { continue; }
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestHalf = halfEnd;
                    bestAccessory = accessoryEnd;
                }
            }
        }

        if (bestHalf == null || bestAccessory == null) continue;

        try { bestHalf.ConnectTo(bestAccessory); joins++; }
        catch { continue; }

        accessoryEnds.Remove(bestAccessory);
    }

    if (joins == 2) placed.Add(accessory.Id);
    else
    {
        // Placed but not fully joined: the accessory exists and the system
        // does not run through it. That is a severed run with something in
        // the gap, and it has to be visible.
        placed.Add(accessory.Id);
        leftSevered.Add(runId);
    }
}
