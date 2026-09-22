// NOT STANDALONE. Assumes `doc`, `elements`, `ducts` and `maxDistanceMm` are in
// scope; leaves `connected`, `findings`, `alreadyConnected`, `noDuctNear`,
// `refused`, `moved` and `notMeasured` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ONE CALL DOES THE WHOLE JOB. ConnectAirTerminalOnDuct creates the tap fitting
// on the duct, positions it under the terminal's connector and connects both
// ends. By hand this is a fitting instance, the duct's parameter at that point
// and three connectors - which is why nothing did it before.
//
// IT RETURNS A BOOL, AND FALSE IS NOT AN EXCEPTION. Revit declining - a supply
// diffuser on a return duct - leaves the model unchanged for that terminal. A
// false is reported by id and never counted as done.
//
// NEAREST IS MEASURED TO THE TERMINAL'S CONNECTOR. A ceiling diffuser's
// connector is on TOP of it, and that is the end that has to reach. Measuring
// from the middle of the diffuser picks the wrong duct where two run side by
// side.
//
// AN ALREADY CONNECTED TERMINAL IS SKIPPED. Reconnecting leaves the old tap
// behind as an orphan fitting nobody sees.
//
// ===========================================================================
// THE TERMINAL CAN MOVE, AND THIS HEADER USED TO SAY IT COULD NOT.
// ===========================================================================
//
// Until 2026-09-23 the last line here read "NOTHING IS MOVED. The terminal has
// to be in the right place already." Nothing in this file moves a terminal -
// but the Revit call it makes was OBSERVED ELSEWHERE, mostly on Revit 2020,
// lifting a terminal 625 mm to meet the duct. NOT YET PROVEN HERE. A tap that
// drags a diffuser out of its ceiling still comes back `connected`, so the
// count alone can never show it.
//
// So every terminal handed in is READ BEFORE ANY CALL and READ AGAIN AFTER -
// its point and the way it faces - and every one that changed is listed in
// `moved`, with how far in millimetres and on which axis. All of them, not only
// the ones tapped: a terminal that moved when it was not the one being
// connected is the same surprise.
//
// NO AMOUNT OF MOVEMENT IS CALLED ACCEPTABLE HERE. The only comparison is
// Revit's own test of whether two points are the same point
// (XYZ.IsAlmostEqualTo), which tells a moved terminal from rounding in the last
// digit and says nothing about how far is too far. That is a number nobody has
// given, so it is the owner's question and not this file's answer - until it
// is answered, every movement is reported and none is refused.
//
// A terminal whose position cannot be read, before or after, goes to
// `notMeasured`. It is NOT counted as unmoved: could not look is not did not
// move.

var connected = 0;
var findings = new List<string>();
var alreadyConnected = new List<ElementId>();
var noDuctNear = new List<ElementId>();
var refused = new List<string>();
var moved = new List<string>();
var notMeasured = new List<ElementId>();

var maxDistance = maxDistanceMm / 304.8;

// The terminal's own connector - not its insertion point. See the header.
Func<Element, Connector> connectorOf = e =>
{
    var instance = e as FamilyInstance;
    if (instance == null || instance.MEPModel == null) return null;
    var manager = instance.MEPModel.ConnectorManager;
    if (manager == null) return null;
    try
    {
        foreach (Connector connector in manager.Connectors)
            if (connector.ConnectorType == ConnectorType.End) return connector;
    }
    catch { }
    return null;
};

// Where a terminal is and which way it faces. Null when either cannot be read,
// which becomes `notMeasured` rather than "it did not move".
Func<Element, XYZ> pointOf = e =>
{
    if (e == null) return null;
    try
    {
        var place = e.Location as LocationPoint;
        return place == null ? null : place.Point;
    }
    catch { return null; }
};

Func<Element, XYZ> facingOf = e =>
{
    if (e == null) return null;
    try
    {
        var place = e.Location as LocationPoint;
        if (place == null) return null;
        return new XYZ(Math.Cos(place.Rotation), Math.Sin(place.Rotation), 0.0);
    }
    catch { return null; }
};

// BEFORE ANYTHING IS CALLED. Every terminal handed in, not only the ones that
// will be tapped - see the header.
var pointBefore = new Dictionary<ElementId, XYZ>();
var facingBefore = new Dictionary<ElementId, XYZ>();
foreach (var terminal in elements)
{
    if (terminal == null) continue;
    var point = pointOf(terminal);
    if (point != null) pointBefore[terminal.Id] = point;
    var facing = facingOf(terminal);
    if (facing != null) facingBefore[terminal.Id] = facing;
}

// How many times Revit was actually asked. With none, nothing can have moved,
// and the comparison below is skipped rather than run over an untouched model.
var called = 0;

foreach (var terminal in elements)
{
    if (terminal == null) continue;

    var connector = connectorOf(terminal);
    if (connector == null)
    {
        refused.Add(string.Format("{0}: no connector could be read - is it an air terminal?", terminal.Id));
        continue;
    }

    var isConnected = false;
    try { isConnected = connector.IsConnected; } catch { }
    if (isConnected) { alreadyConnected.Add(terminal.Id); continue; }

    // Nearest duct whose centreline passes within reach of THAT connector.
    Element nearest = null;
    var nearestDistance = double.MaxValue;

    foreach (var duct in ducts)
    {
        if (duct == null) continue;
        var location = duct.Location as LocationCurve;
        if (location == null || location.Curve == null) continue;

        double distance;
        try { distance = location.Curve.Distance(connector.Origin); }
        catch { continue; }

        if (distance < nearestDistance) { nearestDistance = distance; nearest = duct; }
    }

    if (nearest == null || nearestDistance > maxDistance)
    {
        noDuctNear.Add(terminal.Id);
        continue;
    }

    try
    {
        called++;
        var ok = MechanicalUtils.ConnectAirTerminalOnDuct(doc, terminal.Id, nearest.Id);

        if (!ok)
        {
            // Revit declined. Usually the system types disagree, and that is a
            // modelling question rather than something to work around.
            refused.Add(string.Format("{0}: Revit declined the tap into {1} at {2:0} mm - usually the "
                + "system types do not agree", terminal.Id, nearest.Id, nearestDistance * 304.8));
            continue;
        }

        // READ IT BACK. A returned true is not a connection.
        var reallyJoined = false;
        try { reallyJoined = connectorOf(terminal).IsConnected; }
        catch { reallyJoined = false; }

        if (!reallyJoined)
        {
            refused.Add(string.Format("{0}: the tap was accepted and the terminal is STILL open",
                terminal.Id));
            continue;
        }

        connected++;
        findings.Add(string.Format("{0} tapped into {1}, {2:0} mm away",
            terminal.Id, nearest.Id, nearestDistance * 304.8));
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("{0}: {1}", terminal.Id, ex.Message));
    }
}

// AFTER. One regeneration, so a terminal Revit moves as part of a tap is read
// where it ended up rather than where the call left it pending.
var furthestMm = 0.0;
if (called > 0)
{
    doc.Regenerate();

    var looked = new HashSet<ElementId>();
    foreach (var terminal in elements)
    {
        if (terminal == null || !looked.Add(terminal.Id)) continue;

        XYZ was;
        if (!pointBefore.TryGetValue(terminal.Id, out was)) { notMeasured.Add(terminal.Id); continue; }

        var fresh = doc.GetElement(terminal.Id);
        var now = pointOf(fresh);
        if (now == null) { notMeasured.Add(terminal.Id); continue; }

        var shifted = !now.IsAlmostEqualTo(was);

        var turned = false;
        var turnedDegrees = 0.0;
        XYZ facedBefore;
        var facingNow = facingOf(fresh);
        if (facingNow != null && facingBefore.TryGetValue(terminal.Id, out facedBefore))
        {
            turned = !facingNow.IsAlmostEqualTo(facedBefore);
            if (turned) turnedDegrees = facedBefore.AngleTo(facingNow) * 180.0 / Math.PI;
        }

        if (!shifted && !turned) continue;

        var distanceMm = was.DistanceTo(now) * 304.8;
        if (distanceMm > furthestMm) furthestMm = distanceMm;

        moved.Add(string.Format("{0}: moved {1:0.0} mm (x {2:+0.0;-0.0;0.0}, y {3:+0.0;-0.0;0.0}, "
            + "z {4:+0.0;-0.0;0.0}){5}",
            terminal.Id, distanceMm,
            (now.X - was.X) * 304.8, (now.Y - was.Y) * 304.8, (now.Z - was.Z) * 304.8,
            turned ? string.Format(", turned {0:0.0} degrees", turnedDegrees) : ""));
    }
}

findings.Add(string.Format("{0} terminal(s) tapped in. {1} were already connected, {2} had no duct "
    + "within {3:0} mm, {4} were refused. CHECK THE RESULT with TRACE_CONNECTIVITY rather than trusting "
    + "this count", connected, alreadyConnected.Count, noDuctNear.Count, maxDistanceMm, refused.Count));

if (moved.Count > 0)
{
    findings.Add(string.Format("{0} terminal(s) MOVED while this ran - the furthest by {1:0} mm. Revit can "
        + "shift a terminal to meet the duct, and nothing here decides how far is acceptable. Look at each "
        + "one in a section before keeping this", moved.Count, furthestMm));
}
else if (called > 0 && notMeasured.Count == 0)
{
    findings.Add("No terminal moved: every one handed in is at the same point, facing the same way, as "
        + "before the taps were made");
}

if (notMeasured.Count > 0)
{
    findings.Add(string.Format("{0} terminal(s) could not be measured before and after, so whether they "
        + "moved is NOT known - they are not counted as unmoved", notMeasured.Count));
}
