// NOT STANDALONE. Assumes `doc`, `elements` and `distanceMm` are in scope;
// leaves `created`, `refused` and `notMepCurve` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// TWO UTILITIES, ONE JOB, AND NO SHARED BASE THAT DOES IT. Duct and Pipe both
// derive from MEPCurve and MEPCurve has no break of its own; the break lives on
// MechanicalUtils for ducts and PlumbingUtils for pipes, with identical
// signatures and no common ancestor. So the type test is not defensiveness -
// it is the only way to reach either call. Cable tray and conduit derive from
// MEPCurve too and have NO break at all, which is why they are named as
// unsupported rather than attempted.
//
// THE POINT IS EVALUATED ON THE CURVE, NOT INTERPOLATED BETWEEN ITS ENDS.
// Evaluate(t, true) walks the curve's own parameterisation, so it is right for
// a curved run as well as a straight one. Splitting the straight line between
// the endpoints of a bend would put the break off the duct entirely, and Revit
// would take the nearest point on the run instead - a break that silently
// lands somewhere other than where it was asked for.
//
// A BREAK AT AN END IS NOT A BREAK. Asking for zero, or for the full length,
// produces one zero-length segment; those are refused rather than made,
// because a zero-length duct is an element that passes every check and cannot
// be selected on screen.

var distance = distanceMm / 304.8;

var created = new List<ElementId>();
var halvesJoined = new List<ElementId>();
var halvesLeftOpen = new List<ElementId>();

// The connectors of one element nearest a point - the ends either side of the
// cut. Written once because both halves are asked the same question.
Func<Element, XYZ, List<Connector>> connectorsNear = (element2, where) =>
{
    var found = new List<Connector>();
    var curve2 = element2 as MEPCurve;
    if (curve2 == null) return found;
    ConnectorManager manager = null;
    try { manager = curve2.ConnectorManager; } catch { }
    if (manager == null) return found;
    ConnectorSet set = null;
    try { set = manager.Connectors; } catch { }
    if (set == null) return found;
    foreach (Connector connector in set) if (connector != null) found.Add(connector);
    return found;
};
var refused = new List<ElementId>();
var notMepCurve = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    var isDuct = element is Duct;
    var isPipe = element is Pipe;

    // Cable tray and conduit land here, and so does everything else. They are
    // MEPCurves that Revit gives no break for, so this is the honest place for
    // them - not `refused`, which would suggest the split was attempted.
    if (!isDuct && !isPipe) { notMepCurve.Add(element.Id); continue; }

    var location = element.Location as LocationCurve;
    if (location == null || location.Curve == null) { notMepCurve.Add(element.Id); continue; }

    var curve = location.Curve;
    var length = curve.Length;

    // Outside the run, or exactly at either end. The tolerance is a
    // millimetre's worth of feet: closer than that to an end and the shorter
    // half is not a duct anybody can work with.
    if (distance <= 1.0 / 304.8 || distance >= length - 1.0 / 304.8)
    {
        refused.Add(element.Id);
        continue;
    }

    // Normalised, so Evaluate walks the curve's own parameterisation rather
    // than a straight line between its ends.
    var at = curve.Evaluate(distance / length, true);

    var newId = isDuct
        ? MechanicalUtils.BreakCurve(doc, element.Id, at)
        : PlumbingUtils.BreakCurve(doc, element.Id, at);

    // InvalidElementId comes back when Revit declined the break - a run inside
    // a group, one owned by somebody else, one whose connectors will not
    // divide. Recorded rather than reported as a success with a bad id.
    if (newId == null || newId == ElementId.InvalidElementId)
    {
        refused.Add(element.Id);
        continue;
    }

    created.Add(newId);

    // VERSION 2. THE JOINT IN THE MIDDLE IS ITS OWN QUESTION.
    //
    // The far ends keep whatever they were connected to. Whether the two new
    // halves are joined TO EACH OTHER is not the same fact, and the earlier
    // library measured the break leaving it open - a run that traces as two
    // systems while looking perfect on screen. Unverified here, so this asks
    // rather than asserts: the connectors nearest the cut are found, joined if
    // they are not already, and which happened is reported.
    var halves = new List<Element>();
    halves.Add(doc.GetElement(element.Id));
    halves.Add(doc.GetElement(newId));

    Connector first = null, second = null;
    double closest = double.MaxValue;
    bool alreadyJoined = false;

    foreach (var oneEnd in connectorsNear(halves[0], at))
    {
        foreach (var otherEnd in connectorsNear(halves[1], at))
        {
            if (oneEnd == null || otherEnd == null) continue;

            bool joined = false;
            try
            {
                foreach (Connector reference in oneEnd.AllRefs)
                {
                    if (reference == null || reference.Owner == null) continue;
                    if (reference.Owner.Id == newId) { joined = true; break; }
                }
            }
            catch { }
            if (joined) { alreadyJoined = true; break; }

            double apart;
            try { apart = oneEnd.Origin.DistanceTo(otherEnd.Origin); } catch { continue; }
            if (apart < closest) { closest = apart; first = oneEnd; second = otherEnd; }
        }
        if (alreadyJoined) break;
    }

    if (alreadyJoined) { halvesJoined.Add(element.Id); continue; }

    bool nowJoined = false;
    if (first != null && second != null)
    {
        try { first.ConnectTo(second); nowJoined = first.IsConnected; }
        catch { nowJoined = false; }
    }

    if (nowJoined) halvesJoined.Add(element.Id);
    else halvesLeftOpen.Add(element.Id);
}
