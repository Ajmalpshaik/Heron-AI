// NOT STANDALONE. Assumes `doc`, `elements`, `slopeRatio`, `anchorEnd`,
// `fallAwayFromAnchor` and `maxEndMoveMm` are in scope; leaves `sloped`,
// `findings`, `risers`, `bothEndsConnected`, `inGroup` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// THIS MOVES REAL GEOMETRY. IT IS NOT A COSMETIC CHANGE.
// ===========================================================================
//
// Setting a run's fall changes where its ends are in space, which changes what
// its fittings sit on, which can drag or break a joint at either end. Every
// row reported says whether the end being moved had something connected to it.
//
// WHAT IS NEVER TOUCHED, AND WHY EACH TEST IS GEOMETRIC RATHER THAN A NUMBER:
//
//   A GROUP MEMBER. A move on one is SILENTLY IGNORED by Revit - it returns
//   normally and changes nothing. Refused before anything is attempted, because
//   counting that call as success is how a report claims work nobody can see.
//   Note the trap inside the trap: the MEMBER reports Pinned = true while the
//   GROUP reports false, so asking the group gives the wrong answer.
//
//   A RISER. A run that rises more than it runs is steeper than 45 degrees and
//   is a stack or a drop, not a graded line. A millimetre threshold does not
//   catch this: a 2 m drop with 10 mm of horizontal run passes any sane minimum
//   run and would be RE-DRAWN 10 MM LONG, destroying the pipe.
//
//   A RUN WITH NOTHING TO SLOPE AGAINST. Horizontal run at or below the
//   document's own short-curve tolerance means both ends sit over each other -
//   there is no length to fall over, and the new curve would be zero length.
//
//   AN END THAT WOULD MOVE FURTHER THAN ASKED. The guard against a typed ratio.
//
// EVERY WRITE IS READ BACK. Revit has more than one way to accept a write and
// do nothing with it - a constraint, a pinned neighbour, a group - and every
// one of them returns normally. The end is re-measured against where it was
// asked to go, and a run that did not move is reported as REFUSED rather than
// counted as done. That read-back is the fragment, not a nicety around it.
//
// THE SLOPE PARAMETER IS READ, NEVER WRITTEN. On a plain pipe Revit's Slope
// reports what the geometry is doing. Writing it to say something the geometry
// does not do puts a lie into every schedule that reads it.

var sloped = 0;
var findings = new List<string>();
var risers = new List<ElementId>();
var bothEndsConnected = new List<ElementId>();
var inGroup = new List<ElementId>();
var refused = new List<string>();

var mmPerFoot = 304.8;
var maxEndMove = maxEndMoveMm / mmPerFoot;
var shortest = doc.Application.ShortCurveTolerance;
var anchor = (anchorEnd ?? "low").Trim().ToLower();

// A run's connectors live in a different place depending on what it is.
Func<Element, ConnectorManager> managerOf = e =>
{
    var curve = e as MEPCurve;
    if (curve != null) return curve.ConnectorManager;
    var instance = e as FamilyInstance;
    if (instance != null && instance.MEPModel != null) return instance.MEPModel.ConnectorManager;
    return null;
};

// Is THIS end joined to something? Asked at the end's own position, because a
// run's two connectors are told apart by where they are, not by their order.
Func<Element, XYZ, bool> endIsConnected = (e, point) =>
{
    var manager = managerOf(e);
    if (manager == null) return false;
    try
    {
        foreach (Connector connector in manager.Connectors)
        {
            if (connector.ConnectorType != ConnectorType.End) continue;
            if (connector.Origin.DistanceTo(point) > shortest * 10.0) continue;
            if (connector.IsConnected) return true;
        }
    }
    catch { }
    return false;
};

foreach (var element in elements)
{
    if (element == null) continue;

    if (element.GroupId != null && element.GroupId != ElementId.InvalidElementId)
    {
        inGroup.Add(element.Id);
        continue;
    }

    var location = element.Location as LocationCurve;
    if (location == null || location.Curve == null)
    {
        // A fitting has no curve to slope. Not a failure - it is the wrong kind
        // of element for this question, and saying so is the answer.
        refused.Add(string.Format("{0}: not a linear run - nothing to slope", element.Id));
        continue;
    }

    var a = location.Curve.GetEndPoint(0);
    var b = location.Curve.GetEndPoint(1);

    var run = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
    var rise = Math.Abs(b.Z - a.Z);

    if (run <= shortest)
    {
        risers.Add(element.Id);
        continue;
    }

    if (rise >= run)
    {
        // Steeper than 45 degrees. A stack, a drop, or a swept bend leg.
        risers.Add(element.Id);
        continue;
    }

    XYZ fixedEnd, movingEnd;
    if (anchor == "start") { fixedEnd = a; movingEnd = b; }
    else if (anchor == "end") { fixedEnd = b; movingEnd = a; }
    else if (anchor == "high") { if (a.Z >= b.Z) { fixedEnd = a; movingEnd = b; } else { fixedEnd = b; movingEnd = a; } }
    else { if (a.Z <= b.Z) { fixedEnd = a; movingEnd = b; } else { fixedEnd = b; movingEnd = a; } }

    var movingConnected = endIsConnected(element, movingEnd);
    var fixedConnected = endIsConnected(element, fixedEnd);

    if (movingConnected && fixedConnected)
    {
        bothEndsConnected.Add(element.Id);
        continue;
    }

    // The fall this run's own horizontal length asks for.
    var drop = run / slopeRatio;
    var targetZ = fallAwayFromAnchor ? fixedEnd.Z - drop : fixedEnd.Z + drop;
    var moved = Math.Abs(targetZ - movingEnd.Z);

    if (moved > maxEndMove)
    {
        refused.Add(string.Format(
            "{0}: would move an end {1:0} mm, past the {2:0} mm limit asked for - check the ratio",
            element.Id, moved * mmPerFoot, maxEndMoveMm));
        continue;
    }

    var target = new XYZ(movingEnd.X, movingEnd.Y, targetZ);

    try
    {
        location.Curve = Line.CreateBound(fixedEnd, target);

        // READ IT BACK. This is the whole fragment.
        var after = (element.Location as LocationCurve);
        if (after == null || after.Curve == null)
        {
            refused.Add(string.Format("{0}: lost its curve after the write", element.Id));
            continue;
        }

        var off = Math.Min(after.Curve.GetEndPoint(0).DistanceTo(target),
                           after.Curve.GetEndPoint(1).DistanceTo(target));
        if (off * mmPerFoot > 1.0)
        {
            refused.Add(string.Format(
                "{0}: the write was accepted and the run did NOT move - still {1:0} mm from where it "
                + "was sent. A constraint, a group or a pinned neighbour is holding it",
                element.Id, off * mmPerFoot));
            continue;
        }

        sloped++;

        // Revit's own report of the result, rendered its way. Read, never
        // written - see the header.
        var slopeParameter = element.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE);
        var reads = slopeParameter == null ? "no slope parameter" : (slopeParameter.AsValueString() ?? "unreadable");

        findings.Add(string.Format(
            "{0}  - run {1:0} mm, end moved {2:0} mm, Revit now reads {3}{4}",
            element.Id, run * mmPerFoot, moved * mmPerFoot, reads,
            movingConnected ? "  [SOMETHING WAS CONNECTED to the end that moved - check that joint]" : ""));
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("{0}: {1}", element.Id, ex.Message));
    }
}
