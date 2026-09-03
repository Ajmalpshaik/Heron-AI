// NOT STANDALONE. Assumes `doc`, `elements`, `ducts` and `maxDistanceMm` are in
// scope; leaves `connected`, `findings`, `alreadyConnected`, `noDuctNear` and
// `refused` behind.
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
// NOTHING IS MOVED. The terminal has to be in the right place already.

var connected = 0;
var findings = new List<string>();
var alreadyConnected = new List<ElementId>();
var noDuctNear = new List<ElementId>();
var refused = new List<string>();

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

findings.Add(string.Format("{0} terminal(s) tapped in. {1} were already connected, {2} had no duct "
    + "within {3:0} mm, {4} were refused. CHECK THE RESULT with TRACE_CONNECTIVITY rather than trusting "
    + "this count", connected, alreadyConnected.Count, noDuctNear.Count, maxDistanceMm, refused.Count));
