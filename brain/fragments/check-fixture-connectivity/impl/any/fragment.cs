// NOT STANDALONE. Assumes `doc`, `fixtures` and `requiredServices` are in
// scope; leaves `findings`, `missingService` and `noConnectors` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// IT REPORTS BY SERVICE, WHICH IS THE ONLY USEFUL FORM. "Twelve fixtures have
// an unconnected connector" is not actionable; "nine WCs have no vent and three
// basins have no hot water" is.
//
// A FIXTURE WITH NO CONNECTORS AT ALL is a different defect - usually the
// family was never built to be plumbed - so it is counted apart. The fix is
// different, and it is the row that keeps coming back until somebody opens the
// family.
//
// THIS IS NOT FIND_DEAD_ENDS FROM THE OTHER END. That starts at open pipe ends;
// a fixture nobody piped produces no open end at all, so it is structurally
// invisible there. This starts at the fixture.
//
// THE SERVICE NAME COMES FROM REVIT'S OWN CLASSIFICATION, not from the system's
// name - a renamed system still reports as sanitary. It is turned into text
// rather than compared against named enumeration members, so a release adding a
// new member cannot break the build.

var findings = new List<string>();
var missingService = new List<ElementId>();
var noConnectors = new List<ElementId>();

var wanted = new List<string>();
foreach (var service in requiredServices)
{
    if (!string.IsNullOrEmpty(service)) wanted.Add(service);
}

foreach (var fixture in fixtures)
{
    var instance = fixture as FamilyInstance;
    if (instance == null)
    {
        findings.Add(string.Format("{0} (id {1}): not a placed family instance", fixture.Name, fixture.Id));
        continue;
    }

    ConnectorManager manager = null;
    try { manager = instance.MEPModel == null ? null : instance.MEPModel.ConnectorManager; }
    catch { }

    var present = new List<string>();
    var unconnected = new List<string>();

    if (manager != null)
    {
        foreach (Connector connector in manager.Connectors)
        {
            string service = null;
            try { service = connector.PipeSystemType.ToString(); }
            catch { }
            if (string.IsNullOrEmpty(service)) continue;

            var joined = false;
            try { joined = connector.IsConnected; }
            catch { }

            if (joined) present.Add(service);
            else unconnected.Add(service);
        }
    }

    if (present.Count == 0 && unconnected.Count == 0)
    {
        noConnectors.Add(fixture.Id);
        findings.Add(string.Format("{0} (id {1}): NO PLUMBING CONNECTORS AT ALL. The family was never "
            + "built to be plumbed - a family problem, not a modelling one, and it will keep coming "
            + "back until somebody opens it", fixture.Name, fixture.Id));
        continue;
    }

    var faults = new List<string>();

    foreach (var service in unconnected)
    {
        faults.Add(string.Format("{0}: NOT CONNECTED", service));
    }

    foreach (var service in wanted)
    {
        var found = false;
        foreach (var have in present)
        {
            if (string.Equals(have, service, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
        }
        if (found) continue;

        foreach (var open in unconnected)
        {
            // already reported above as NOT CONNECTED, which is the more precise
            // statement of the same gap
            if (string.Equals(open, service, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
        }
        if (!found) faults.Add(string.Format("{0}: NO CONNECTOR for this service", service));
    }

    if (faults.Count > 0)
    {
        missingService.Add(fixture.Id);
        findings.Add(string.Format("{0} (id {1}) - {2}", fixture.Name, fixture.Id,
            string.Join("; ", faults)));
    }
}

findings.Insert(0, string.Format("{0} fixture(s): {1} with a service missing or unconnected, {2} with "
    + "no plumbing connectors at all. Reported PER SERVICE - a count of unconnected connectors is not "
    + "something anybody can act on", fixtures.Count, missingService.Count, noConnectors.Count));
