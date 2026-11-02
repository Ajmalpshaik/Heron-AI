// NOT STANDALONE. Assumes `doc`, `elements`, `ducts` and `maxDistanceMm` are in
// scope; leaves `connected`, `findings`, `alreadyConnected`, `noDuctNear`,
// `refused`, `moved`, `notMeasured`, `branched`, `runs`, `noMainNear` and
// `created` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// VERSION 3 (2026-11-02) - REVIT'S CONNECT INTO, FOR THE CASES THIS ALREADY
// DRAWS. Asked as "connect this diffuser into the main duct" and "connect this
// sink into the waste pipe". Two things version 2 lacked:
//
//   THE RIGHT CONNECTOR OF A FIXTURE WITH SEVERAL. Version 2 branched the
//   FIRST open pipe connector - on a sink, possibly the cold water one, to a
//   waste pipe. Now, when an element has more than one open pipe connector,
//   only a connector whose system classification (Connector.PipeSystemType)
//   is the main's (its piping system type's SystemClassification) is used,
//   and only to mains of that classification. None, or more than one
//   connector, matching: nothing is drawn and the connectors are named. One
//   open connector - a sprinkler, a floor drain - is version 2, unchanged.
//
//   THE NEW ELEMENTS BY ID. `created` holds every element this made - the
//   tap fitting Revit cut for a terminal, read off the terminal's connector
//   after the tap, and each riser and arm of a branch - and the findings say
//   which.
//
// THE MAIN IS NEVER SEARCHED FOR IN THE MODEL: it is chosen only from
// `ducts`, handed in, the nearest of them. Hand in one and that is the main.
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
// VERSION 3 - every element made, by id.
var created = new List<ElementId>();

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
// VERSION 3: a main's system classification, as its piping system type says
// it - compared by name with a connector's PipeSystemType.
Func<Pipe, string> classOf = p =>
{
    try
    {
        var sp = p.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
        var st = sp == null ? null : doc.GetElement(sp.AsElementId()) as MEPSystemType;
        return st == null ? null : st.SystemClassification.ToString();
    }
    catch { return null; }
};
var mainsUsed = new HashSet<ElementId>();
var headsSeen = new HashSet<ElementId>();

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

    // =====================================================================
    // VERSION 2: A SPRINKLER OR A FIXTURE - CREATE_FIXTURE_BRANCHES' path.
    // =====================================================================
    if (isPipeHead(terminal))
    {
        var head = terminal;
        if (!headsSeen.Add(head.Id)) continue;
        var instance = (FamilyInstance)head;

        // The head's open pipe ends. All piping ends connected already: skipped.
        var opens = new List<Connector>();
        try
        {
            foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
            {
                if (c.ConnectorType != ConnectorType.End || c.Domain != Domain.DomainPiping) continue;
                if (!c.IsConnected) opens.Add(c);
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("{0}: its connectors could not be read - {1}", head.Id, ex.Message));
            continue;
        }
        if (opens.Count == 0) { alreadyConnected.Add(head.Id); continue; }

        // VERSION 3: more than one open pipe connector - a sink's waste, cold
        // and hot - and only the one whose system is the main's is used.
        Connector open = opens[0];
        var headMains = mains;
        if (opens.Count > 1)
        {
            var mainClasses = new HashSet<string>(mains.Select(m => classOf(m)).Where(n => n != null));
            var matching = opens.Where(c => mainClasses.Contains(c.PipeSystemType.ToString())).ToList();
            if (matching.Count != 1)
            {
                refused.Add(string.Format("{0} ('{1}'): it has {2} open pipe connectors ({3}) and {4} of them "
                    + "matches the system of the pipes handed in ({5}), so nothing was drawn - hand in the one "
                    + "main its connector belongs on.", head.Id, head.Name, opens.Count,
                    string.Join(", ", opens.Select(c => c.PipeSystemType.ToString()).ToArray()),
                    matching.Count == 0 ? "none" : matching.Count.ToString(),
                    mainClasses.Count == 0 ? "no system read" : string.Join(", ", mainClasses.ToArray())));
                continue;
            }
            open = matching[0];
            var wantedClass = open.PipeSystemType.ToString();
            headMains = mains.Where(m => classOf(m) == wantedClass).ToList();
        }

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

        foreach (var main in headMains)
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
                // READ IT BACK. ConnectTo returning is not a connection.
                try { open.ConnectTo(riserEnd); joined = open.IsConnected; }
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

            foreach (var e in made) { runs.Add(e); created.Add(e.Id); }
            if (mainsUsed.Add(best.Id)) runs.Add(best);
            branched++;
            findings.Add(string.Format("{0}: riser {1} mm{2}, {3}, to main {4}; joined at the head; new pipe id(s) {5}",
                head.Id, Math.Round(Math.Abs(bestLanding.Z - start.Z) * mmPerFoot),
                arm == null ? " landing on the main's axis"
                    : string.Format(" and arm {0} mm", Math.Round(bendPoint.DistanceTo(bestLanding) * mmPerFoot)),
                string.IsNullOrEmpty(diameterText) ? "at the connector's size" : diameterText,
                best.Id, string.Join(", ", made.Select(e => e.Id.ToString()).ToArray())));
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
        // VERSION 2: `ducts` may hold the pipes a sprinkler branches to. An
        // air terminal taps a duct, never one of those.
        if (duct is Pipe) continue;
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
        // VERSION 3: the tap Revit cut, by id - whatever the terminal's
        // connector is now joined to, other than the duct itself.
        var tapIds = new List<ElementId>();
        try
        {
            foreach (Connector other in connectorOf(terminal).AllRefs)
                if (other.Owner != null && other.Owner.Id != terminal.Id && other.Owner.Id != nearest.Id
                    && !(other.Owner is MEPSystem) && !tapIds.Contains(other.Owner.Id))
                    tapIds.Add(other.Owner.Id);
        }
        catch { }
        created.AddRange(tapIds);
        findings.Add(string.Format("{0} tapped into {1}, {2:0} mm away{3}",
            terminal.Id, nearest.Id, nearestDistance * 304.8,
            tapIds.Count == 0 ? "" : "; new fitting id(s) " + string.Join(", ", tapIds.Select(i => i.ToString()).ToArray())));
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
