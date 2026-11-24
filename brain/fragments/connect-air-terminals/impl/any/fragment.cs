// NOT STANDALONE. Assumes `doc`, `elements`, `ducts` and `maxDistanceMm` are in
// scope; leaves `connected`, `findings`, `alreadyConnected`, `noDuctNear`,
// `refused`, `moved`, `notMeasured`, `branched`, `runs` and `noMainNear` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// VERSION 2 (2026-11-01) - CREATE_FIXTURE_BRANCHES MERGED IN. AIR TERMINALS AND
// SPRINKLERS OR FIXTURES, ONE TOOL. An element handed in with a DUCT connector
// is an air terminal and is tapped into the nearest DUCT exactly as version 1
// did. An element with a PIPE connector and no duct one - a sprinkler, a floor
// drain - is BRANCHED to the nearest PIPE among `ducts` (they may be named
// "Ducts, Pipes" together): Revit has no pipe twin of the tap call, so a
// straight RISER is drawn from the head's connector to the main's centreline
// height and a level ARM across to the main's centreline, at the connector's
// own size, on the main's pipe type and system, and joined at the head.
// FIT_MEP_JOINTS then makes the elbow and the tap or tee, from `runs`.
//
// THE BRANCH'S RULES, CARRIED WHOLE: the connector must face UP or DOWN and
// the main must be that way; the arm lands at least 100 mm or two main
// diameters in from a main's end; riser and arm together within maxDistanceMm,
// or the head goes to `noMainNear`; a head already connected is skipped; no
// head, fixture or main is moved by this file - and a head's point is read
// before and after with the terminals', so if Revit moves one it is in `moved`.
//
// THE CONFLICT, AND THE SAFER RULE KEPT. CREATE_FIXTURE_BRANCHES kept the pipes
// it had drawn when Revit refused the rest of a branch, and kept a branch that
// would not join the head. A terminal Revit declines is left UNCHANGED here, so
// a head is too: each head's branch is drawn inside its own SubTransaction and
// rolled back whole when Revit refuses any part of it or the head still reads
// open - nothing is left behind for that head, and it is named in `refused`.
//
// VERSION 3 (2026-11-23, WN-79 - work note FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07
// C05, issue #1005) - ONE KIND OF CONNECTION EVIDENCE, AND THE DUCT CHOSEN FROM
// THE ONES THAT FIT. Version 2 took a terminal's FIRST End connector whatever
// its domain, trusted `IsConnected` - an intent flag, wrong in a real model
// (REPORT_CONNECTORS) - both to skip a terminal and to call it joined, and
// picked the nearest duct before asking whether it could take the terminal.
// So a nearer return duct beside a supply diffuser hid the supply duct behind
// it, and Revit 2024 was seen joining a supply diffuser to a return duct (row
// 5b-187). Now:
//   - only an AIR TERMINAL (its category) is tapped; anything else handed in
//     with a duct end - a fan coil, an AHU - is refused by name, never counted;
//   - the terminal's connector is its HVAC (duct) End connector, never a pipe
//     or electrical one;
//   - "joined" is read off the PHYSICAL PARTNERS in AllRefs, the rule
//     REPORT_CONNECTORS uses: refs owned by the terminal itself or by an
//     MEPSystem are not partners. That decides the skip AND the result, and a
//     head's join too;
//   - the ducts are FILTERED FIRST - a straight Duct only, and one whose system
//     classification is not the opposite air system of the terminal's (supply,
//     return, exhaust) - then the nearest of those within maxDistanceMm is
//     taken. A nearer duct skipped as the wrong system is said by id;
//   - after the tap the terminal must be joined to THAT duct, directly or
//     through the one fitting between them; joined to anything else goes to
//     `refused`, never `connected`.
// Version 2 was PROVEN (its proof block kept as the record); its C# moved, so
// the card is DRAFT until NEEDS-CHECKING Group PZ proves version 3 (D-30).
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
// VERSION 2 - CREATE_FIXTURE_BRANCHES' answers.
var branched = 0;
var runs = new List<Element>();
var noMainNear = new List<ElementId>();

var maxDistance = maxDistanceMm / 304.8;

// VERSION 2: which kind of element this is. A duct connector makes it an air
// terminal - version 1's path, untouched. Only an element with a pipe end and
// no duct end is branched.
Func<Element, bool> isPipeHead = e =>
{
    var instance = e as FamilyInstance;
    if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null) return false;
    var piping = false;
    try
    {
        foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
        {
            if (c.ConnectorType != ConnectorType.End) continue;
            if (c.Domain == Domain.DomainHvac) return false;
            if (c.Domain == Domain.DomainPiping) piping = true;
        }
    }
    catch { return false; }
    return piping;
};

// THE MAINS for a branch: the straight pipes among `ducts`.
var mmPerFoot = 304.8;
var minRiser = 50.0 / mmPerFoot;       // shorter than this leaves no room for an elbow
var minArm = 1.0 / mmPerFoot;          // shorter than this, the riser lands on the main itself
var endClearMin = 100.0 / mmPerFoot;   // the arm lands at least this far in from a main's end
var mains = new List<Pipe>();
if (ducts != null)
{
    foreach (var candidate in ducts)
    {
        var pipe = candidate as Pipe;
        if (pipe == null) continue;
        var place = pipe.Location as LocationCurve;
        if (place == null || !(place.Curve is Line)) continue;
        mains.Add(pipe);
    }
}
var mainsUsed = new HashSet<ElementId>();
var headsSeen = new HashSet<ElementId>();

// VERSION 3: WHAT IS PHYSICALLY JOINED TO A CONNECTOR - the elements that own
// its partners in AllRefs, leaving out the connector's own element and the
// logical MEPSystem, which AllRefs carries too (REPORT_CONNECTORS' rule).
// IsConnected is never asked. Null when AllRefs cannot be read: could not look
// is not open.
Func<Connector, List<ElementId>> partnersOf = c =>
{
    var owners = new List<ElementId>();
    if (c == null) return null;
    try
    {
        var self = c.Owner == null ? ElementId.InvalidElementId : c.Owner.Id;
        foreach (Connector other in c.AllRefs)
        {
            Element owner = null;
            try { owner = other.Owner; } catch { }
            if (owner == null || owner is MEPSystem || owner.Id == self) continue;
            if (!owners.Contains(owner.Id)) owners.Add(owner.Id);
        }
    }
    catch { return null; }
    return owners;
};

// VERSION 3: AN AIR TERMINAL IS ITS CATEGORY. ConnectAirTerminalOnDuct is
// Revit's call for an air terminal; a fan coil or an AHU with a duct spigot is
// not one, and is refused rather than tried.
var airTerminalCategory = new ElementId(BuiltInCategory.OST_DuctTerminal);
Func<Element, bool> isAirTerminal = e =>
{
    try { return e != null && e.Category != null && e.Category.Id == airTerminalCategory; }
    catch { return false; }
};

// The terminal's own DUCT connectors - not its insertion point (see the
// header), and VERSION 3: only HVAC End connectors, never a pipe or an
// electrical one a family may also carry. Null when they cannot be read.
Func<Element, List<Connector>> ductEndsOf = e =>
{
    var ends = new List<Connector>();
    var instance = e as FamilyInstance;
    if (instance == null || instance.MEPModel == null) return ends;
    var manager = instance.MEPModel.ConnectorManager;
    if (manager == null) return ends;
    try
    {
        foreach (Connector connector in manager.Connectors)
            if (connector.ConnectorType == ConnectorType.End && connector.Domain == Domain.DomainHvac)
                ends.Add(connector);
    }
    catch { return null; }
    return ends;
};

// VERSION 3: WHICH AIR SYSTEM A CONNECTOR IS ON - supply, return or exhaust -
// or null when it is none of the three (other air, undefined, unreadable),
// and then nothing is refused on it.
Func<Connector, string> airSystemOf = c =>
{
    try
    {
        var kind = c.DuctSystemType;
        if (kind == DuctSystemType.SupplyAir) return "supply air";
        if (kind == DuctSystemType.ReturnAir) return "return air";
        if (kind == DuctSystemType.ExhaustAir) return "exhaust air";
    }
    catch { }
    return null;
};

// A duct's air system, read off its own connectors - the first that says one.
Func<Element, string> ductSystemOf = d =>
{
    var curve = d as MEPCurve;
    if (curve == null || curve.ConnectorManager == null) return null;
    try
    {
        foreach (Connector c in curve.ConnectorManager.Connectors)
        {
            var said = airSystemOf(c);
            if (said != null) return said;
        }
    }
    catch { }
    return null;
};

// VERSION 3: IS THE TERMINAL JOINED TO THAT DUCT - directly, or through the one
// fitting the tap puts between them. Answers null when it is, otherwise why
// not, in the modeller's words.
Func<Element, ElementId, string> notJoinedTo = (e, ductId) =>
{
    var fresh = doc.GetElement(e.Id);
    var ends = ductEndsOf(fresh);
    if (ends == null) return "its duct connector could not be read back";
    var others = new List<ElementId>();
    foreach (var end in ends)
    {
        var partners = partnersOf(end);
        if (partners == null) return "what its duct connector is joined to could not be read back";
        foreach (var id in partners)
        {
            if (id == ductId) return null;
            var between = doc.GetElement(id) as FamilyInstance;
            if (between != null && between.MEPModel != null && between.MEPModel.ConnectorManager != null)
            {
                try
                {
                    foreach (Connector c in between.MEPModel.ConnectorManager.Connectors)
                    {
                        var next = partnersOf(c);
                        if (next != null && next.Contains(ductId)) return null;
                    }
                }
                catch { }
            }
            if (!others.Contains(id)) others.Add(id);
        }
    }
    if (others.Count == 0) return "nothing is physically joined to its duct connector";
    return "it is joined to " + string.Join(", ", others.Select(x => x.ToString()).ToArray())
        + ", which does not reach duct " + ductId;
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

    // =====================================================================
    // VERSION 2: A SPRINKLER OR A FIXTURE - CREATE_FIXTURE_BRANCHES' path.
    // =====================================================================
    if (isPipeHead(terminal))
    {
        var head = terminal;
        if (!headsSeen.Add(head.Id)) continue;
        var instance = (FamilyInstance)head;

        // The head's open pipe end. All piping ends connected already: skipped.
        // VERSION 3: open means NO PHYSICAL PARTNER, read off AllRefs - never
        // the IsConnected flag (the header).
        Connector open = null;
        var headUnreadable = false;
        try
        {
            foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
            {
                if (c.ConnectorType != ConnectorType.End || c.Domain != Domain.DomainPiping) continue;
                var partners = partnersOf(c);
                if (partners == null) { headUnreadable = true; break; }
                if (partners.Count == 0 && open == null) open = c;
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("{0}: its connectors could not be read - {1}", head.Id, ex.Message));
            continue;
        }
        if (headUnreadable)
        {
            refused.Add(string.Format("{0}: what its pipe connector is joined to could not be read, so no "
                + "branch was drawn", head.Id));
            continue;
        }
        if (open == null) { alreadyConnected.Add(head.Id); continue; }

        if (mains.Count == 0)
        {
            refused.Add(string.Format("{0} ('{1}'): no straight pipe was handed in as a main, so no branch was "
                + "drawn. Name the mains by category - \"Pipes\", or \"Ducts, Pipes\" with terminals too.",
                head.Id, head.Name));
            continue;
        }
        if (maxDistanceMm <= 0)
        {
            refused.Add(string.Format("{0}: a maximum branch length greater than zero is required, so no "
                + "branch was drawn.", head.Id));
            continue;
        }

        var facing = open.CoordinateSystem.BasisZ;
        if (Math.Abs(facing.Z) < 0.99)
        {
            refused.Add(string.Format("{0}: its pipe connector faces sideways, not up or down. This draws a "
                + "riser and an arm only - a sideways connector needs a branch shape this does not guess.",
                head.Id));
            continue;
        }

        var start = open.Origin;
        Pipe best = null;
        XYZ bestLanding = null;
        var bestLength = double.MaxValue;

        foreach (var main in mains)
        {
            var line = (Line)((LocationCurve)main.Location).Curve;
            var a = line.GetEndPoint(0);
            var b = line.GetEndPoint(1);
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var planLengthSq = dx * dx + dy * dy;
            if (planLengthSq < 1e-9) continue; // a vertical pipe is not a main to branch from here

            // Nearest point on the main, in plan, to the connector.
            var t = ((start.X - a.X) * dx + (start.Y - a.Y) * dy) / planLengthSq;
            var length = line.Length;

            var mainDiameter = 0.0;
            var dp = main.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (dp != null && dp.HasValue) mainDiameter = dp.AsDouble();
            var clear = Math.Max(endClearMin, 2.0 * mainDiameter);
            var tClear = length > 0 ? clear / length : 1.0;
            if (t < tClear || t > 1.0 - tClear) continue; // lands at or past an end

            var landing = a + (b - a) * t;
            var rise = landing.Z - start.Z;
            if (Math.Abs(rise) < minRiser) continue;            // no room for an elbow
            if (Math.Sign(rise) != Math.Sign(facing.Z)) continue; // the main is behind the head

            var bend = new XYZ(start.X, start.Y, landing.Z);
            var total = Math.Abs(rise) + bend.DistanceTo(landing);
            if (total < bestLength) { bestLength = total; best = main; bestLanding = landing; }
        }

        if (best == null || bestLength > maxDistance)
        {
            noMainNear.Add(head.Id);
            continue;
        }

        var bendPoint = new XYZ(start.X, start.Y, bestLanding.Z);

        var systemTypeId = ElementId.InvalidElementId;
        var sp = best.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
        if (sp != null && sp.HasValue) systemTypeId = sp.AsElementId();
        var level = best.ReferenceLevel;
        if (systemTypeId == ElementId.InvalidElementId || level == null)
        {
            refused.Add(string.Format("{0}: main {1} has no piping system type or no reference level "
                + "to give the branch, so nothing was drawn for this head.", head.Id, best.Id));
            continue;
        }

        // ONE HEAD, ONE SUBTRANSACTION - kept whole or rolled back whole.
        SubTransaction branch = null;
        var made = new List<Element>();
        try
        {
            branch = new SubTransaction(doc);
            branch.Start();
            called++;

            var riser = Pipe.Create(doc, systemTypeId, best.GetTypeId(), level.Id, start, bendPoint);
            made.Add(riser);

            if (open.Shape == ConnectorProfileType.Round)
            {
                var rp = riser.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (rp != null && !rp.IsReadOnly) rp.Set(open.Radius * 2.0);
            }

            Pipe arm = null;
            if (bendPoint.DistanceTo(bestLanding) >= minArm)
            {
                arm = Pipe.Create(doc, systemTypeId, best.GetTypeId(), level.Id, bendPoint, bestLanding);
                made.Add(arm);
                var rp = riser.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                var ap = arm.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (rp != null && ap != null && !ap.IsReadOnly) ap.Set(rp.AsDouble());
            }

            var diameterText = "";
            var rd = riser.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (rd != null) diameterText = rd.AsValueString();

            // Join the riser to the head: the riser's end nearest the connector.
            Connector riserEnd = null;
            var nearestGap = double.MaxValue;
            foreach (Connector c in riser.ConnectorManager.Connectors)
            {
                if (c.ConnectorType != ConnectorType.End) continue;
                var gap = c.Origin.DistanceTo(start);
                if (gap < nearestGap) { nearestGap = gap; riserEnd = c; }
            }

            var joined = false;
            var joinProblem = "";
            if (riserEnd != null)
            {
                // READ IT BACK. ConnectTo returning is not a connection, and
                // VERSION 3: joined means the RISER is the head's partner.
                try
                {
                    open.ConnectTo(riserEnd);
                    var partners = partnersOf(open);
                    joined = partners != null && partners.Contains(riser.Id);
                }
                catch (Exception ex) { joinProblem = ex.Message; }
            }

            if (!joined)
            {
                branch.RollBack();
                refused.Add(string.Format("{0}: the branch to main {1} would NOT join the head - {2} - so it "
                    + "was taken back and NOTHING is left for this head. The head's size against the main's "
                    + "pipe type is the usual reason.", head.Id, best.Id,
                    riserEnd == null ? "its end at the head could not be found"
                        : joinProblem.Length > 0 ? "Revit said: " + joinProblem
                        : "Revit accepted the join and the head still reads open"));
                continue;
            }

            branch.Commit();

            foreach (var e in made) runs.Add(e);
            if (mainsUsed.Add(best.Id)) runs.Add(best);
            branched++;
            findings.Add(string.Format("{0}: riser {1} mm{2}, {3}, to main {4}; joined at the head",
                head.Id, Math.Round(Math.Abs(bestLanding.Z - start.Z) * mmPerFoot),
                arm == null ? " landing on the main's axis"
                    : string.Format(" and arm {0} mm", Math.Round(bendPoint.DistanceTo(bestLanding) * mmPerFoot)),
                string.IsNullOrEmpty(diameterText) ? "at the connector's size" : diameterText,
                best.Id));
        }
        catch (Exception ex)
        {
            var undoFailed = "";
            try
            {
                if (branch != null && branch.HasStarted() && !branch.HasEnded()) branch.RollBack();
            }
            catch (Exception undo) { undoFailed = undo.Message ?? "no message"; }

            refused.Add(string.Format("{0}: Revit refused the branch to main {1} - {2}. {3}", head.Id, best.Id,
                ex.Message, undoFailed.Length > 0
                    ? "Taking the " + made.Count + " pipe(s) back failed too: \"" + undoFailed
                      + "\" - check the model and use Undo if they are there."
                    : "The " + made.Count + " pipe(s) made before the refusal were taken back; nothing is "
                      + "left for this head."));
        }
        continue;
    }

    // VERSION 3: AN AIR TERMINAL ONLY - anything else with a duct end is refused
    // by name, never tapped and never counted.
    if (!isAirTerminal(terminal))
    {
        var categoryName = "no category";
        try { if (terminal.Category != null) categoryName = terminal.Category.Name; } catch { }
        refused.Add(string.Format("{0} ('{1}', {2}): not an air terminal, so it was not tapped. Only Air "
            + "Terminals are tapped into a duct here; equipment with a duct spigot is joined with "
            + "CONNECT_INTO_MAIN", terminal.Id, terminal.Name, categoryName));
        continue;
    }

    var ends = ductEndsOf(terminal);
    if (ends == null)
    {
        refused.Add(string.Format("{0}: its connectors could not be read", terminal.Id));
        continue;
    }
    if (ends.Count == 0)
    {
        refused.Add(string.Format("{0} ('{1}'): it has no duct connector, so there is nothing to tap",
            terminal.Id, terminal.Name));
        continue;
    }

    // VERSION 3: ALREADY JOINED is read off the partners, never IsConnected. A
    // terminal with a duct end physically joined is skipped - reconnecting it
    // leaves the old tap behind (the header).
    Connector connector = null;
    var joinedAlready = false;
    var unreadable = false;
    foreach (var end in ends)
    {
        var partners = partnersOf(end);
        if (partners == null) { unreadable = true; break; }
        if (partners.Count > 0) joinedAlready = true;
        else if (connector == null) connector = end;
    }
    if (unreadable)
    {
        refused.Add(string.Format("{0}: what its duct connector is joined to could not be read, so it was "
            + "not tapped", terminal.Id));
        continue;
    }
    if (joinedAlready || connector == null) { alreadyConnected.Add(terminal.Id); continue; }

    // VERSION 3: THE DUCTS THAT FIT, THEN THE NEAREST OF THOSE. A straight Duct
    // only - a pipe is a head's main, and flex or a cable tray is not tapped -
    // and not one on the opposite air system. A nearer duct passed over for
    // its system is said, so the choice is never silent.
    var terminalSystem = airSystemOf(connector);
    Element nearest = null;
    var nearestDistance = double.MaxValue;
    Element nearerWrong = null;
    var nearerWrongDistance = double.MaxValue;
    string nearerWrongSystem = null;

    foreach (var duct in ducts)
    {
        if (!(duct is Duct)) continue;
        var location = duct.Location as LocationCurve;
        if (location == null || location.Curve == null) continue;

        double distance;
        try { distance = location.Curve.Distance(connector.Origin); }
        catch { continue; }

        var ductSystem = ductSystemOf(duct);
        if (terminalSystem != null && ductSystem != null && ductSystem != terminalSystem)
        {
            if (distance < nearerWrongDistance)
            {
                nearerWrongDistance = distance;
                nearerWrong = duct;
                nearerWrongSystem = ductSystem;
            }
            continue;
        }

        if (distance < nearestDistance) { nearestDistance = distance; nearest = duct; }
    }

    var passedOver = "";
    if (nearerWrong != null && nearerWrongDistance <= maxDistance
        && (nearest == null || nearerWrongDistance < nearestDistance))
    {
        passedOver = string.Format("; duct {0}, {1:0} mm away, was passed over - it is {2} and the terminal "
            + "is {3}", nearerWrong.Id, nearerWrongDistance * 304.8, nearerWrongSystem, terminalSystem);
    }

    if (nearest == null || nearestDistance > maxDistance)
    {
        noDuctNear.Add(terminal.Id);
        if (passedOver.Length > 0)
            findings.Add(string.Format("{0}: no duct of its own system within {1:0} mm{2}", terminal.Id,
                maxDistanceMm, passedOver));
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

        // READ IT BACK. A returned true is not a connection, and VERSION 3: a
        // connection is to THIS duct, read off the partners.
        var why = notJoinedTo(terminal, nearest.Id);
        if (why != null)
        {
            refused.Add(string.Format("{0}: the tap into {1} was accepted, and {2} - not counted as "
                + "connected. Look at it before keeping this", terminal.Id, nearest.Id, why));
            continue;
        }

        connected++;
        findings.Add(string.Format("{0} tapped into {1}, {2:0} mm away{3}",
            terminal.Id, nearest.Id, nearestDistance * 304.8, passedOver));
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

if (headsSeen.Count > 0)
{
    // VERSION 2 - CREATE_FIXTURE_BRANCHES' sentence, said only when a head was handed in.
    findings.Add(string.Format("{0} sprinkler or fixture head(s) branched and joined at the head; {1} had no "
        + "main within {2:0} mm in the direction they face. NO FITTING IS MADE HERE: run FIT_MEP_JOINTS on "
        + "`runs` for the elbows and the taps, then TRACE_CONNECTIVITY to prove it",
        branched, noMainNear.Count, maxDistanceMm));
}

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
