// NOT STANDALONE. Assumes `elements` is in scope, and leaves `connectorFacts`,
// `connections`, `openConnectors`, `flagDisagrees` and `noConnectors` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE FLAG IS REPORTED, NEVER TRUSTED.
//
// `Connector.IsConnected` has been true in a real model with nothing joined to
// it. A run traced from the flag comes back complete while the air stops at a
// gap - the worst kind of wrong answer, because it is confident. The partner
// list is therefore built from `AllRefs` and the flag is only compared against
// it afterwards: where they disagree the element is named, so the disagreement
// is a finding rather than a silent choice made here.
//
// WHAT COUNTS AS A PARTNER.
//
// `AllRefs` carries three kinds of reference and only one of them is a
// physical joint:
//
//   the connector's own element   itself, and joining a thing to itself is not
//                                 a connection
//   an MEPSystem                  the LOGICAL system this connector belongs
//                                 to. Counting it would make every connector
//                                 on a named system read as connected, which
//                                 is the same false positive the flag gives
//   another element               the joint. This is the only one kept
//
// SIZE THROWS RATHER THAN RETURNING ZERO.
//
// `Radius` exists only on a round connector, `Width` and `Height` only on a
// rectangular one, and asking the wrong one throws on every release. An
// electrical connector has no profile at all. Each is guarded separately, and
// a connector whose size cannot be read is still reported with its domain and
// direction - which is what the caller came for - rather than being dropped.
//
// FEET, NOT MILLIMETRES. D-20 converts at the edge. Doing it here would make
// this the second place that knows about units, and the numbers below are also
// the input to geometry, where feet is what everything else speaks.

var connectorFacts = new Dictionary<ElementId, IList<string>>();
var connections = new Dictionary<ElementId, IList<ElementId>>();
var openConnectors = new Dictionary<ElementId, int>();
var flagDisagrees = new List<ElementId>();
var noConnectors = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    // Two different homes for the same thing: a duct or pipe carries its own
    // manager, a piece of equipment carries one inside its MEP model. An
    // element with neither is not a failure - a wall has no connectors and
    // saying so is the honest answer.
    ConnectorManager manager = null;
    var curve = element as MEPCurve;
    if (curve != null) { try { manager = curve.ConnectorManager; } catch { } }
    if (manager == null)
    {
        var instance = element as FamilyInstance;
        if (instance != null && instance.MEPModel != null)
        {
            try { manager = instance.MEPModel.ConnectorManager; } catch { } 
        }
    }

    if (manager == null) { noConnectors.Add(element.Id); continue; }

    var facts = new List<string>();
    var partners = new List<ElementId>();
    int open = 0;
    bool flagSaysConnected = false;
    bool anyRealPartner = false;

    ConnectorSet set = null;
    try { set = manager.Connectors; } catch { }
    if (set == null) { noConnectors.Add(element.Id); continue; }

    foreach (Connector connector in set)
    {
        if (connector == null) continue;

        var mine = new List<ElementId>();
        try
        {
            foreach (Connector reference in connector.AllRefs)
            {
                if (reference == null) continue;
                Element owner = null;
                try { owner = reference.Owner; } catch { }
                if (owner == null) continue;
                if (owner.Id == element.Id) continue;
                if (owner is MEPSystem) continue;
                mine.Add(owner.Id);
            }
        }
        catch { }

        bool flag = false;
        try { flag = connector.IsConnected; } catch { }
        if (flag) flagSaysConnected = true;
        if (mine.Count > 0) anyRealPartner = true; else open++;

        // Every read below is independent, because one that throws must not
        // cost the reader the ones that would have worked.
        string domain = "unknown domain";
        try { domain = connector.Domain.ToString(); } catch { }

        string shape = "no profile";
        string size = "size unavailable";
        try
        {
            shape = connector.Shape.ToString();
            if (connector.Shape == ConnectorProfileType.Round)
                size = "radius " + connector.Radius.ToString("0.###") + " ft";
            else if (connector.Shape == ConnectorProfileType.Rectangular ||
                     connector.Shape == ConnectorProfileType.Oval)
                size = connector.Width.ToString("0.###") + " x " +
                       connector.Height.ToString("0.###") + " ft";
        }
        catch { }

        string origin = "no origin";
        string facing = "no direction";
        try
        {
            var point = connector.Origin;
            origin = "at (" + point.X.ToString("0.###") + ", " +
                     point.Y.ToString("0.###") + ", " + point.Z.ToString("0.###") + ") ft";
        }
        catch { }
        try
        {
            // BasisZ is the way the connector FACES - the direction a duct
            // leaves it. It is the number that decides whether a spigot is
            // usable, and it is not in any schedule.
            var direction = connector.CoordinateSystem.BasisZ;
            facing = "facing (" + direction.X.ToString("0.##") + ", " +
                     direction.Y.ToString("0.##") + ", " + direction.Z.ToString("0.##") + ")";
        }
        catch { }

        string joined = mine.Count > 0
            ? "joined to " + string.Join(", ", mine.Select(id => id.ToString()).ToArray())
            : "OPEN";

        facts.Add(domain + ", " + shape + ", " + size + ", " + origin + ", " + facing +
                  ", " + joined + " (IsConnected flag: " + flag.ToString() + ")");

        foreach (var id in mine) if (!partners.Contains(id)) partners.Add(id);
    }

    if (facts.Count == 0) { noConnectors.Add(element.Id); continue; }

    connectorFacts[element.Id] = facts;
    connections[element.Id] = partners;
    openConnectors[element.Id] = open;

    // The disagreement, in the only direction that misleads anybody: the flag
    // claiming a joint that is not there. The opposite - a real partner with
    // the flag down - has not been seen, and would be reported here too.
    if (flagSaysConnected != anyRealPartner) flagDisagrees.Add(element.Id);
}
