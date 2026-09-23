// NOT STANDALONE. Assumes `elements` is in scope, and leaves `connectorFacts`,
// `connections`, `openConnectors`, `flagDisagrees`, `noConnectors` and
// `connectorSummary` behind.
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
// WHICH AIR, WHICH WATER.
//
// The first thing a modeller asks of a connector is which system it serves -
// "put the SUPPLY connector 2000 from the wall" - and until 2026-09-23 this
// said `DomainHvac` and stopped there. So a duct connector now carries its
// `DuctSystemType` and a pipe connector its `PipeSystemType`, straight after
// the domain: "DomainHvac SupplyAir". Each property THROWS on a connector of
// any other domain - InvalidOperationException, documented the same on every
// release 2020 to 2027 - so each is asked only inside its own domain, and one
// that still cannot be read says "system unavailable" rather than vanishing: a
// missing word would read as a connector with no system at all.
//
// It is Revit's "instantaneous system type at this connector, calculated
// according to system", and the API says an unconnected connector's is
// undefined - yet an OPEN connector read SupplyAir in Project1 on Revit
// 2024 (2026-09-23). What the open end of a duct says has not been read
// on a real model yet; `UndefinedSystemType` is Revit's word for none, and it
// is printed as it comes. An electrical connector gets nothing added - its
// system type is a different property, and nothing here asks for it.
//
// THE WHOLE ANSWER ALSO COMES BACK AS ONE STRING.
//
// A reply prints a dictionary as its size and nothing else. Through a chat
// this answered `connectorFacts 1 entry(ies)` - where each connector sits,
// which way it faces and what it is, the one thing this fragment is for, could
// not be seen at all (Project1, Revit 2024, 2026-09-23). `connectorSummary` is
// every line of `connectorFacts`, element by element, joined into one string,
// which a reply prints whole. The three dictionaries are left exactly as they
// were, for anything that reads them after this.
//
// It is EMPTY when nothing had a connector, never a sentence saying so. The
// proving tool reads an empty string as nothing found and any sentence as a
// shape it cannot count, so a note there would turn a correct empty answer
// into a failed negative case. `noConnectors` already says what was looked at.
//
// It STOPS AFTER 50 CONNECTORS and says how many it left out. A tool answers a
// question and never returns the model (D-26): every connector on three
// thousand ducts is the best part of a megabyte of text nobody reads. It stops at
// a whole element, the first element is always listed in full, and the last
// part says how many elements and connectors were not listed. `connectorFacts`
// still holds every one of them.
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

        // WHICH AIR, WHICH WATER - the note at the top says why each system
        // type is asked only inside its own domain.
        string system = "";
        bool hvac = false;
        bool piping = false;
        try
        {
            hvac = connector.Domain == Domain.DomainHvac;
            piping = connector.Domain == Domain.DomainPiping;
        }
        catch { }
        if (hvac)
        {
            system = "system unavailable";
            try { system = connector.DuctSystemType.ToString(); } catch { }
        }
        else if (piping)
        {
            system = "system unavailable";
            try { system = connector.PipeSystemType.ToString(); } catch { }
        }
        if (system.Length > 0) domain += " " + system;

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

// THE WHOLE ANSWER AS ONE STRING - the note at the top says why. Walked in the
// order the elements were handed in, rather than in the dictionary's, and
// built inside this block so its working values stay out of the answer: the
// host reports every top-level variable a fragment leaves in scope, and a
// `shown` or a `listed` there would read as something this fragment found.
var connectorSummary = "";
if (connectorFacts.Count > 0)
{
    int limit = 50;
    int shown = 0;
    int leftElements = 0;
    int leftConnectors = 0;
    var parts = new List<string>();
    var listed = new HashSet<ElementId>();

    foreach (var element in elements)
    {
        if (element == null || !listed.Add(element.Id)) continue;
        IList<string> lines;
        if (!connectorFacts.TryGetValue(element.Id, out lines)) continue;

        // Whole elements only, and never the first one left out: one
        // element's connectors are the answer to "what does this have", and
        // half of them would answer a different question.
        if (leftElements > 0 || (parts.Count > 0 && shown + lines.Count > limit))
        {
            leftElements++;
            leftConnectors += lines.Count;
            continue;
        }

        parts.Add("element " + element.Id.ToString() + ": " +
                  string.Join(" ; ", lines.ToArray()));
        shown += lines.Count;
    }

    if (leftElements > 0)
        parts.Add(leftElements + " more element(s) with " + leftConnectors +
                  " connector(s) not listed here - select fewer to see them");

    connectorSummary = string.Join("  ||  ", parts.ToArray());
}
