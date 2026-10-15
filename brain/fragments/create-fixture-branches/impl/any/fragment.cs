// NOT STANDALONE. Assumes `doc`, `elements`, `pipes` and `maxDistanceMm` are in
// scope; leaves `branched`, `runs`, `alreadyConnected`, `noMainNear`, `refused`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TWO PIPES PER HEAD, AND NO FITTING. A RISER from the head's connector
// straight up or down to the main's centreline height, then a level ARM to the
// main's centreline. FIT_MEP_JOINTS makes the elbow where they meet and the tap
// or tee where the arm lands - `runs` is what it takes next.
//
// DRAWN BY POINTS, JOINED BY ConnectTo. The connector overload of Pipe.Create
// throws AFTER creating the pipe when it cannot join, so the pipe it made has
// no name here. By points, every pipe made is held and listed.
//
// NOTHING IS MOVED. A head with no main in reach goes to `noMainNear`.

var branched = 0;
var runs = new List<Element>();
var alreadyConnected = new List<ElementId>();
var noMainNear = new List<ElementId>();
var refused = new List<string>();
var findings = new List<string>();

var mmPerFoot = 304.8;
var maxDistance = maxDistanceMm / mmPerFoot;
var minRiser = 50.0 / mmPerFoot;       // shorter than this leaves no room for an elbow
var minArm = 1.0 / mmPerFoot;          // shorter than this, the riser lands on the main itself
var endClearMin = 100.0 / mmPerFoot;   // the arm lands at least this far in from a main's end

// The mains: pipes only, each with its straight centreline.
var mains = new List<Pipe>();
if (pipes != null)
{
    foreach (var candidate in pipes)
    {
        var pipe = candidate as Pipe;
        if (pipe == null) continue;
        var place = pipe.Location as LocationCurve;
        if (place == null || !(place.Curve is Line)) continue;
        mains.Add(pipe);
    }
}

if (elements == null || elements.Count == 0)
    refused.Add("No sprinklers or fixtures were handed in, so NOTHING WAS DRAWN.");
else if (mains.Count == 0)
    refused.Add("No straight pipe was handed in as a main, so NOTHING WAS DRAWN. Name the mains "
        + "by category - \"Pipes\".");
else if (maxDistanceMm <= 0)
    refused.Add("A maximum branch length greater than zero is required, so NOTHING WAS DRAWN.");
else
{
    var mainsUsed = new HashSet<ElementId>();
    var seen = new HashSet<ElementId>();

    foreach (var head in elements)
    {
        if (head == null || !seen.Add(head.Id)) continue;

        var instance = head as FamilyInstance;
        if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null)
        {
            refused.Add(string.Format("{0} ('{1}'): no pipe connector could be read - is it a "
                + "sprinkler or a plumbing fixture?", head.Id, head.Name));
            continue;
        }

        // The head's open pipe end. All piping ends connected already: skipped.
        Connector open = null;
        var pipingEnds = 0;
        try
        {
            foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
            {
                if (c.ConnectorType != ConnectorType.End || c.Domain != Domain.DomainPiping) continue;
                pipingEnds++;
                if (!c.IsConnected && open == null) open = c;
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("{0}: its connectors could not be read - {1}", head.Id, ex.Message));
            continue;
        }

        if (pipingEnds == 0)
        {
            refused.Add(string.Format("{0} ('{1}') has no pipe connector.", head.Id, head.Name));
            continue;
        }
        if (open == null) { alreadyConnected.Add(head.Id); continue; }

        var facing = open.CoordinateSystem.BasisZ;
        if (Math.Abs(facing.Z) < 0.99)
        {
            refused.Add(string.Format("{0}: its pipe connector faces sideways, not up or down. Version 1 "
                + "draws a riser and an arm only - a sideways connector needs a branch shape this does "
                + "not guess.", head.Id));
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

        var made = new List<Element>();
        try
        {
            var riser = Pipe.Create(doc, systemTypeId, best.GetTypeId(), level.Id, start, bendPoint);
            made.Add(riser);

            var diameterText = "";
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

            foreach (var e in made) runs.Add(e);
            if (mainsUsed.Add(best.Id)) runs.Add(best);

            if (joined)
            {
                branched++;
                findings.Add(string.Format("{0}: riser {1} mm{2}, {3}, to main {4}; joined at the head",
                    head.Id, Math.Round(Math.Abs(bestLanding.Z - start.Z) * mmPerFoot),
                    arm == null ? " landing on the main's axis"
                        : string.Format(" and arm {0} mm", Math.Round(bendPoint.DistanceTo(bestLanding) * mmPerFoot)),
                    string.IsNullOrEmpty(diameterText) ? "at the connector's size" : diameterText,
                    best.Id));
            }
            else if (riserEnd == null)
            {
                refused.Add(string.Format("{0}: the branch was drawn and its end at the head could not "
                    + "be found, so it is NOT joined to the head", head.Id));
            }
            else
            {
                refused.Add(string.Format("{0}: the branch was drawn and it is NOT joined to the head - {1}. "
                    + "CONNECT_OPEN_ENDS can join it once the sizes agree.", head.Id,
                    joinProblem.Length > 0 ? "Revit said: " + joinProblem
                        : "Revit accepted the join and the head still reads open"));
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("{0}: Revit refused the branch to main {1} - {2}. {3} pipe(s) of "
                + "it were made before the refusal and are listed in runs.", head.Id, best.Id, ex.Message,
                made.Count));
            foreach (var e in made) runs.Add(e);
        }
    }

    findings.Insert(0, string.Format("{0} head(s) branched and joined at the head. {1} were already "
        + "connected, {2} had no main within {3:0} mm in the direction they face, {4} refused. "
        + "NO FITTING IS MADE HERE: run FIT_MEP_JOINTS on `runs` for the elbows and the taps, then "
        + "TRACE_CONNECTIVITY to prove it", branched, alreadyConnected.Count, noMainNear.Count,
        maxDistanceMm, refused.Count));
}

foreach (var reason in refused) findings.Add(reason);
