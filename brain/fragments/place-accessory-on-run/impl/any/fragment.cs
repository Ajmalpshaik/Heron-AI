// NOT STANDALONE. Assumes `doc`, `elements`, `accessorySymbol` and
// `positionAlong` are in scope, and leaves `placed`, `leftSevered`,
// `notARun`, `refused` and `notTurned` behind.
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
// ENDS ARE PAIRED BY SIDE, NOT BY DISTANCE - VERSION 2.
//
// Version 1 paired each half's nearest free end with the accessory's nearest
// free end. Both halves end at the cut, and the accessory's two ends sit the
// same distance either side of it, so "nearest" was a tie, settled by the
// order Revit lists connectors - and on a LONE duct each half also has its far
// end free. A half joined to the accessory end on the OTHER half's side must
// run back through the accessory, which is Revit's "modified to be in the
// opposite direction causing the connections to be invalid" (row 5b-1119,
// Revit 2020, every lone duct and pipe). Read from the code; the Revit run is
// owed. Now each half's end AT THE CUT is joined to the accessory end lying on
// that half's side of the cut.
//
// THE ACCESSORY IS TURNED TO THE RUN FIRST. A family is placed along its own
// X axis whichever way the run goes; version 1 never turned it, so on a run
// not drawn along that axis its ends did not lie on the run at all. It is
// turned in plan about the cut until the line through its two ends is
// parallel to the run. A vertical run is not turned - said in notTurned.

var placed = new List<ElementId>();
var leftSevered = new List<ElementId>();
var notARun = new List<ElementId>();
var refused = new List<ElementId>();
var notTurned = new List<string>();

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

    // Turn the accessory in plan until the line through its two ends is
    // parallel to the run. Either way along the line will do: the ends are
    // then paired by side, below.
    XYZ runDirection = null;
    try { runDirection = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize(); } catch { }
    if (runDirection != null && accessoryEnds.Count != 2)
        notTurned.Add(string.Format("run {0}: the accessory has {1} free end(s), not 2 - not turned",
            runId, accessoryEnds.Count));
    if (runDirection != null && accessoryEnds.Count == 2)
    {
        var runFlat = new XYZ(runDirection.X, runDirection.Y, 0);
        XYZ axis = null;
        try { axis = accessoryEnds[1].Origin - accessoryEnds[0].Origin; } catch { }
        var axisFlat = axis == null ? null : new XYZ(axis.X, axis.Y, 0);
        if (runFlat.GetLength() < 1e-6 || axisFlat == null || axisFlat.GetLength() < 1e-6)
        {
            notTurned.Add(string.Format("run {0}: vertical, or the accessory's ends are one above the "
                + "other - not turned in plan", runId));
        }
        else
        {
            var angle = Math.Atan2(runFlat.Y, runFlat.X) - Math.Atan2(axisFlat.Y, axisFlat.X);
            if (Math.Abs(Math.Sin(angle)) > 1e-6)
            {
                try
                {
                    ElementTransformUtils.RotateElement(doc, accessory.Id,
                        Line.CreateBound(point, point + XYZ.BasisZ), angle);
                    doc.Regenerate();
                    accessoryEnds = freeConnectorsOf(accessory);
                }
                catch (Exception ex)
                {
                    notTurned.Add(string.Format("run {0}: Revit would not turn the accessory - {1}",
                        runId, ex.Message));
                }
            }
        }
    }

    int joins = 0;

    foreach (var half in new Element[] { halfOne, halfTwo })
    {
        // The half's end AT THE CUT - on a lone run its far end is free too,
        // and is never the one to join.
        Connector cutEnd = null;
        double cutDistance = double.MaxValue;
        foreach (var halfEnd in freeConnectorsOf(half))
        {
            double distance;
            try { distance = halfEnd.Origin.DistanceTo(point); }
            catch { continue; }
            if (distance < cutDistance) { cutDistance = distance; cutEnd = halfEnd; }
        }
        if (cutEnd == null) continue;

        // Which side of the cut this half lies on: from the cut towards the
        // middle of the half.
        XYZ side = null;
        try
        {
            var halfCurve = ((LocationCurve)half.Location).Curve;
            side = halfCurve.Evaluate(0.5, true) - point;
        }
        catch { }
        if (side == null || side.GetLength() < 1e-9) continue;

        // The accessory end lying furthest along that side.
        Connector sameSide = null;
        double best = 0;
        foreach (var accessoryEnd in accessoryEnds)
        {
            double along2;
            try { along2 = (accessoryEnd.Origin - point).DotProduct(side.Normalize()); }
            catch { continue; }
            if (along2 > best) { best = along2; sameSide = accessoryEnd; }
        }
        if (sameSide == null) continue;

        try { cutEnd.ConnectTo(sameSide); joins++; }
        catch { continue; }

        accessoryEnds.Remove(sameSide);
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
