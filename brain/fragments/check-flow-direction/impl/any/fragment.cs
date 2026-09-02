// NOT STANDALONE. Assumes `elements` is in scope, and leaves `bothOut`,
// `bothIn`, `jointsChecked` and `bidirectionalSkipped` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY JOINED PAIR SHOULD BE ONE OUT MEETING ONE IN.
//
// Two connectors both pushing OUT at the same joint, or both pulling IN, is a
// system that cannot work as drawn - and it is INVISIBLE. The ducts touch, the
// fittings look right, the run reads as finished. Revit does not refuse it; it
// stops calculating sensible flow past that point, and the equipment gets
// blamed.
//
// THE OTHER HALF OF FIND_SYSTEM_ISLANDS. That asks whether the pieces are
// joined. This asks whether the joins make sense. A system can be perfectly
// whole with two pumps facing each other, and a connectivity check calls that
// one healthy network.
//
// BIDIRECTIONAL IS NOT A FAULT. Plenty of fittings carry it legitimately, and
// flagging them fills the report with pairs nobody can act on - after which
// nobody reads it. Counted, so their absence from the findings is visible.
//
// READING Direction THROWS on a connector whose domain has no flow - an
// electrical one - so it is guarded per CONNECTOR. A guard per element would
// lose a whole mixed set over one cable tray.

var bothOut = new List<string>();
var bothIn = new List<string>();
int jointsChecked = 0;
int bidirectionalSkipped = 0;

// Each joint is two connectors and would otherwise be seen twice, once from
// each side. Keyed on the pair so the count is joints and not connector visits.
var seen = new HashSet<string>();

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

foreach (var element in elements)
{
    if (element == null) continue;
    var connectors = connectorsOf(element);
    if (connectors == null) continue;

    foreach (Connector connector in connectors)
    {
        if (connector == null) continue;

        FlowDirectionType mine;
        try { mine = connector.Direction; } catch { continue; }

        ConnectorSet joined = null;
        try { joined = connector.AllRefs; } catch { }
        if (joined == null) continue;

        foreach (Connector other in joined)
        {
            if (other == null || other.Owner == null) continue;
            if (other.Owner.Id == element.Id) continue;

            FlowDirectionType theirs;
            try { theirs = other.Direction; } catch { continue; }

            // One joint, seen from both sides. Ordered by id text so the two
            // visits produce the same key and the joint counts once.
            string a = element.Id.ToString();
            string b = other.Owner.Id.ToString();
            string key = string.CompareOrdinal(a, b) <= 0 ? a + " | " + b : b + " | " + a;
            if (!seen.Add(key)) continue;

            if (mine == FlowDirectionType.Bidirectional || theirs == FlowDirectionType.Bidirectional)
            {
                bidirectionalSkipped++;
                continue;
            }

            jointsChecked++;

            if (mine == FlowDirectionType.Out && theirs == FlowDirectionType.Out) bothOut.Add(key);
            else if (mine == FlowDirectionType.In && theirs == FlowDirectionType.In) bothIn.Add(key);
        }
    }
}
