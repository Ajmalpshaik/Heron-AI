// NOT STANDALONE. Assumes `elements` and `sizeTolerance` are in scope, and
// leaves `connectorFacts`, `connections`, `openConnectors`, `flagDisagrees`,
// `noConnectors`, `connectorSummary`, `mismatched`, `unconnectedSpigots` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// VERSION 3 (2026-11-01) - EVERY JOINT ON A PLACED FAMILY IS GRADED TOO.
// CHECK_EQUIPMENT_CONNECTORS (check-equipment-connectors) was merged into this
// card on the owner's instruction and is DEPRECATED. Its grading is carried
// here, below the description: on each FamilyInstance - equipment, terminal,
// fixture, fitting, accessory - every connector is compared with what is
// REALLY joined to it. A pipe on a duct spigot is a DOMAIN MISMATCH, its own
// class; a round-to-rectangular joint is a SHAPE CHANGE, said and not
// measured; same shape is compared per shape - diameter, or width and height -
// within `sizeTolerance` (MILLIMETRES, D-71; left out, 0); and an open spigot
// is NOTHING PLUGGED IN, counted in `unconnectedSpigots`. `mismatched` names
// each element with a domain or size mismatch, once. Its sentences are in
// MILLIMETRES, as that card's were - they are read by a person; the fact lines
// stay in feet.
//
// THE SAFER RULE OF THE TWO IS KEPT. That card took the first ref in AllRefs
// that was not the unit itself as the partner, and trusted IsConnected to say
// whether anything was plugged in - so the logical MEPSystem, which AllRefs
// also carries, could be graded as the "duct" on the other side, and a spigot
// whose flag lied read as joined. Here the partner is the same physical one
// the description uses: refs owned by the element itself or by an MEPSystem
// are dropped first, and a spigot with no physical partner is OPEN whatever
// the flag says.
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

// ---- VERSION 3: the grading, from CHECK_EQUIPMENT_CONNECTORS.
//
// MILLIMETRES IN, FEET INSIDE (D-71). The add-in converts an XYZ at the
// boundary and cannot convert a bare double, so the tolerance is converted
// here, once, before it is used. A negative one grades no size at all and says
// so, rather than calling every joint a mismatch.
const double MillimetresPerFoot = 304.8;
var gradeSizes = !(sizeTolerance < 0) && !double.IsNaN(sizeTolerance);
var toleranceFeet = gradeSizes ? sizeTolerance / MillimetresPerFoot : 0;
var mismatched = new List<ElementId>();
var unconnectedSpigots = 0;
var findings = new List<string>();
var gradeLines = new List<string>();
var gradeLeft = 0;
var unitsGraded = 0;
Action<string> grade = line =>
{
    // A tool answers a question and never returns the model (D-26): the first
    // 100 sentences are kept, and the rest are counted.
    if (gradeLines.Count < 100) gradeLines.Add(line); else gradeLeft++;
};
Func<Connector, string> describeMm = connector =>
{
    try
    {
        if (connector.Shape == ConnectorProfileType.Round)
        {
            return string.Format("round {0:0} mm dia", connector.Radius * 2 * MillimetresPerFoot);
        }
        if (connector.Shape == ConnectorProfileType.Rectangular
            || connector.Shape == ConnectorProfileType.Oval)
        {
            return string.Format("{0} {1:0} x {2:0} mm",
                connector.Shape == ConnectorProfileType.Oval ? "oval" : "rect",
                connector.Width * MillimetresPerFoot, connector.Height * MillimetresPerFoot);
        }
    }
    catch { }
    return "(size unreadable)";
};

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

    if (manager == null)
    {
        noConnectors.Add(element.Id);
        // VERSION 3: CHECK_EQUIPMENT_CONNECTORS's two sentences, kept apart -
        // nothing to connect, and a family that can never join a system.
        if (!(element is FamilyInstance))
            grade(string.Format("{0} (id {1}): not a placed family instance or an MEP run - it has no "
                + "connectors", element.Name, element.Id));
        else
            grade(string.Format("{0} (id {1}): no MEP model at all. A Generic Model used as equipment, "
                + "or a placeholder - it can never join a system", element.Name, element.Id));
        continue;
    }

    // VERSION 3: only a placed family is graded - its spigots against what is
    // bolted on. A duct's or pipe's own ends are described and not graded, so
    // a joint between two elements handed in together is judged once, from the
    // family's side.
    var unit = curve == null ? element as FamilyInstance : null;
    var index = 0;
    var unitMismatched = false;

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
        index++;

        var mine = new List<ElementId>();
        // VERSION 3: the first physical partner's own connector, which is what
        // a joint is graded against - through a transition, the FITTING.
        Connector partner = null;
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
                if (partner == null) partner = reference;
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

        // ---- VERSION 3: GRADE THIS JOINT, on a placed family only.
        if (unit == null) continue;
        var spigot = describeMm(connector);

        if (partner == null)
        {
            // OPEN, decided by what is physically joined and never by the
            // flag - the safer of the two cards' rules.
            unconnectedSpigots++;
            grade(string.Format("{0} (id {1}) connector {2} [{3}]: NOTHING PLUGGED IN{4}. No clash test "
                + "and no connectivity walk will flag this - the services that ARE connected trace perfectly",
                element.Name, element.Id, index, spigot,
                flag ? " - though its IsConnected flag says joined" : ""));
            continue;
        }

        var theirs = describeMm(partner);
        string ownerName = "element";
        try
        {
            if (partner.Owner != null && partner.Owner.Category != null) ownerName = partner.Owner.Category.Name;
        }
        catch { }

        // Domain first: a different kind of wrong, never a tolerance.
        var domainClash = false;
        try { domainClash = connector.Domain != partner.Domain; }
        catch { }
        if (domainClash)
        {
            unitMismatched = true;
            string mineDomain = "unknown domain";
            string theirDomain = "unknown domain";
            try { mineDomain = connector.Domain.ToString(); } catch { }
            try { theirDomain = partner.Domain.ToString(); } catch { }
            grade(string.Format("{0} (id {1}) connector {2}: DOMAIN MISMATCH - {3} joined to a {4} ({5}). "
                + "Not a tolerance problem; usually the wrong family was placed",
                element.Name, element.Id, index, mineDomain, theirDomain, ownerName));
            continue;
        }

        // Shape next, never compared as one number. A connector with no
        // profile - an electrical one - throws on Shape and has no size to
        // grade; the domain above is all there is to compare.
        ConnectorProfileType myShape;
        ConnectorProfileType theirShape;
        try { myShape = connector.Shape; theirShape = partner.Shape; }
        catch { continue; }
        if (myShape != theirShape)
        {
            grade(string.Format("{0} (id {1}) connector {2}: SHAPE CHANGE - {3} onto {4} ({5}). May be a "
                + "correct transition; it should be a decision rather than an accident",
                element.Name, element.Id, index, spigot, theirs, ownerName));
            continue;
        }
        if (!gradeSizes) continue;

        // Size, per shape. The 1e-9 ft is floating noise, not a tolerance.
        var differs = false;
        var detail = "";
        try
        {
            if (myShape == ConnectorProfileType.Round)
            {
                var delta = Math.Abs(connector.Radius - partner.Radius) * 2;
                differs = delta > toleranceFeet + 1e-9;
                detail = string.Format("{0:0.#} mm on diameter", delta * MillimetresPerFoot);
            }
            else if (myShape == ConnectorProfileType.Rectangular || myShape == ConnectorProfileType.Oval)
            {
                var dw = Math.Abs(connector.Width - partner.Width);
                var dh = Math.Abs(connector.Height - partner.Height);
                differs = dw > toleranceFeet + 1e-9 || dh > toleranceFeet + 1e-9;
                detail = string.Format("{0:0.#} mm wide, {1:0.#} mm high",
                    dw * MillimetresPerFoot, dh * MillimetresPerFoot);
            }
        }
        catch { differs = false; }

        if (differs)
        {
            unitMismatched = true;
            grade(string.Format("{0} (id {1}) connector {2}: SIZE MISMATCH - spigot {3}, {4} is {5}. Out by "
                + "{6}. Revit builds it with a transition and nobody sees it until the airflow does not "
                + "arrive", element.Name, element.Id, index, spigot, ownerName, theirs, detail));
        }
    }

    if (unitMismatched && !mismatched.Contains(element.Id)) mismatched.Add(element.Id);

    if (facts.Count == 0) { noConnectors.Add(element.Id); continue; }
    if (unit != null) unitsGraded++;

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

// ---- VERSION 3: THE GRADING'S OWN SENTENCES, the summary first - in
// CHECK_EQUIPMENT_CONNECTORS's words, with its warning that every size is what
// the FAMILY declares.
findings.Add(string.Format("{0} placed family instance(s) graded: {1} with a mismatched joint, {2} "
    + "spigot(s) with nothing physically plugged in{3}. Sizes are what the FAMILY declares - check one unit "
    + "of each type against its datasheet once, or every result agrees with itself and is wrong together",
    unitsGraded, mismatched.Count, unconnectedSpigots,
    gradeSizes
        ? (sizeTolerance > 0 ? string.Format(", sizes compared within {0:0.#} mm", sizeTolerance) : ", sizes compared exactly")
        : ". NO SIZE WAS GRADED: a sizeTolerance of " + sizeTolerance + " mm is not a tolerance - give 0 or more"));
findings.AddRange(gradeLines);
if (gradeLeft > 0)
    findings.Add(gradeLeft + " more sentence(s) not listed here - select fewer to see them. `mismatched` "
        + "still names every element with a mismatched joint");
