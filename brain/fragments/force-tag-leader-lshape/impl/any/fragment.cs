// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `legMm` are in scope,
// and leaves `bent`, `noLeader`, `notTags` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE ELBOW IS IGNORED WHILE THE LEADER END IS ATTACHED.
//
// With an attached end Revit owns the leader's shape: setting an elbow returns
// normally and nothing bends. So the end condition is set to free first - and
// that is a real change, not a formality. A free end stops following its
// element when the element moves, which is the price of the drafting standard
// and the reason this is an explicit act.
//
// TAG CLASSES SHARE NO BASE THAT CARRIES A LEADER.
//
// An independent tag and a spatial one - room, space, area - each declare
// their own elbow, end and head with nothing in common. A cast to the
// independent kind alone skips every room tag silently and reports the rest as
// done. Both branches are written out, so the compiler checks them.
//
// THE LEG IS A PAPER LENGTH, SCALED.
//
// The shoulder is judged on the printed sheet. A model-length leg is invisible
// at 1:200 and absurd at 1:20, which is the same mistake every clearance in
// tag work invites.
//
// THE SHOULDER POINTS AT THE ELEMENT.
//
// A leg leaving the tag on the far side runs back across its own text - worse
// than the diagonal it replaced.

const double MmToFeet = 1.0 / 304.8;

int bent = 0;
var noLeader = new List<ElementId>();
var notTags = new List<ElementId>();
var alreadyStraight = new List<ElementId>();
var hooked = new List<ElementId>();
var usedLeaderEnd = new List<ElementId>();
int endsMoved = 0;
int endsAttached = 0;
int legStillTrue = 0;
int cornerMovedToEnd = 0;
var endsLeftFree = new List<ElementId>();
var legWentDiagonal = new List<ElementId>();
var upTheSheetLeftStraight = new List<ElementId>();
var refused = new List<ElementId>();

int scale = 1;
try { scale = Math.Max(1, view.Scale); } catch { }

// THE STUB IS A PAPER LENGTH, like every other clearance here. Both guards
// below are thresholds on the sheet, so both are scaled. The reference
// implementation this was studied from leaves its two guard thresholds as
// unscaled model feet, which makes its vertical hook 1.5 mm on paper at
// 1:100 and 7.6 mm at 1:20 - the same class of bug as an unscaled clearance
// anywhere else, and deliberately NOT reproduced here.
double stub = legMm * MmToFeet * scale;

// Guard 2's threshold. A tenth of the stub: below this the head and the
// element are on the same row already and there is no bend to make.
double flatEnough = stub * 0.1;

var right = view.RightDirection;
var up = view.UpDirection;

foreach (var element in elements)
{
    if (element == null) continue;

    var independent = element as IndependentTag;
    var spatial = element as SpatialElementTag;

    if (independent == null && spatial == null) { notTags.Add(element.Id); continue; }

    bool hasLeader = false;
    try { hasLeader = independent != null ? independent.HasLeader : spatial.HasLeader; }
    catch { }

    if (!hasLeader) { noLeader.Add(element.Id); continue; }

    XYZ head = null, end = null;

    // Declared out here, not inside the branch that sets it: the restore
    // below runs after both branches and the compile gate caught it.
    LeaderEndCondition wasCondition = LeaderEndCondition.Attached;

    if (independent != null)
    {
        try { head = independent.TagHeadPosition; } catch { }

        // WHAT IT WAS, so it can be put back. Freeing is a means, not an
        // outcome: an ATTACHED end follows its duct when the duct moves and a
        // free one does not, so a view left full of freed ends quietly rots
        // the first time somebody shifts a run. It is freed just long enough
        // to place the corner and the start.
        try { wasCondition = independent.LeaderEndCondition; } catch { }

        try
        {
            // Free first, or the elbow below is quietly ignored.
            independent.LeaderEndCondition = LeaderEndCondition.Free;
        }
        catch { refused.Add(element.Id); continue; }

#if REVIT2020 || REVIT2021
        try { end = independent.LeaderEnd; } catch { }
#else
        try
        {
            var references = independent.GetTaggedReferences();
            if (references != null && references.Count > 0) end = independent.GetLeaderEnd(references[0]);
        }
        catch { }
#endif
    }
    else
    {
        try { head = spatial.TagHeadPosition; } catch { }
        // A SPATIAL TAG HAS NO END CONDITION AT ALL - the property simply is
        // not on the class, on any release, which the compile gate caught the
        // first time this branch was written to mirror the other one. Its
        // elbow is set directly, and the read-back below is what says whether
        // that worked rather than an assumption that it must have.
        try { end = spatial.LeaderEnd; } catch { }
    }

    if (head == null || end == null) { noLeader.Add(element.Id); continue; }

    // THE ELBOW IS BUILT FROM BOTH ENDS AT ONCE, and that is the whole of
    // what makes an L rather than a stub with a diagonal hanging off it.
    //
    // It takes its ACROSS position from the ELEMENT and its ALONG position
    // from the HEAD. In the view's own axes the corner is (end.U, head.V):
    // straight up or down from where the leader meets the element, then
    // straight across into the tag. Both legs are axis-true and no diagonal
    // survives anywhere in the path.
    //
    // NEITHER LEG'S LENGTH IS CHOSEN. They fall out as |dx| and |dy|, the
    // separation the caller already created by putting the head where it is.
    // An earlier version of this fragment fixed the shoulder at a leg length
    // measured from the HEAD and let the run back to the element be whatever
    // was left - which is a short horizontal stub plus a long diagonal, the
    // exact thing somebody means when they say the leaders are not L-shaped.
    //
    // Everything is projected onto the view's axes, so a rotated plan or a
    // section behaves like a north-up plan and "across" means across on the
    // printed sheet rather than in world space.
    // WHERE THE ELEMENT ACTUALLY IS, rather than where the leader claims to
    // meet it. MEASURED: all twenty tags in one plan came back with the
    // leader end level with the tag head, which collapses both legs to
    // nothing and drops every tag into a degenerate guard. Freeing an
    // attached end lets Revit choose where the free end lands, and it does
    // not reliably choose a point on the element. The element's own location
    // cannot drift like that, so the corner is taken from there.
    //
    // THE SAME 2022 SPLIT AS EVERY OTHER TAG READ HERE: one tag became able
    // to carry several hosts, so the singular property became a plural method.
    XYZ anchor = null;
    XYZ runP0 = null, runP1 = null;
    if (independent != null)
    {
        try
        {
            Element host = null;
#if REVIT2020 || REVIT2021
            var hostId = independent.TaggedLocalElementId;
            if (hostId != null) host = doc.GetElement(hostId);
#else
            foreach (var hostId in independent.GetTaggedLocalElementIds())
            {
                host = doc.GetElement(hostId);
                break;
            }
#endif
            if (host != null)
            {
                var hostCurve = host.Location as LocationCurve;
                if (hostCurve != null && hostCurve.Curve != null)
                {
                    anchor = hostCurve.Curve.Evaluate(0.5, true);
                    // BOTH ENDS OF THE RUN, kept: which way it lies on the
                    // paper decides whether it is bent at all, and the drop
                    // has to land ON it rather than off its end.
                    runP0 = hostCurve.Curve.GetEndPoint(0);
                    runP1 = hostCurve.Curve.GetEndPoint(1);
                }
                else
                {
                    var hostPoint = host.Location as LocationPoint;
                    if (hostPoint != null) anchor = hostPoint.Point;
                }
            }
        }
        catch { }
    }

    // A tag whose host gives no location falls back to the leader end. That
    // is the weaker route and it is COUNTED, never silent - it is the one
    // that produced the degenerate run.
    if (anchor == null) { anchor = end; usedLeaderEnd.Add(element.Id); }

    // WHICH WAY THE RUN LIES ON THE PAPER decides whether it is bent at all.
    // Ajmal's rule: only the runs going ACROSS the plan get an L. A run going
    // UP the plan already meets its tag on a clean vertical, and an L there
    // would be a kink in something already correct.
    //
    // This is the run's direction ON THE SHEET, judged in the view's own
    // axes. It is a different question from the riser rule, which is about a
    // run standing end-on to the paper and is handled where tags are placed -
    // such a run never gets a tag at all.
    bool runsAcross = true;
    if (runP0 != null && runP1 != null)
    {
        var runSpan = runP1 - runP0;
        runsAcross = Math.Abs(runSpan.DotProduct(right)) >= Math.Abs(runSpan.DotProduct(up));
    }

    if (!runsAcross)
    {
        // LEFT STRAIGHT, AND LEFT ATTACHED. Nothing is written here, so
        // nothing had to be freed, and the end goes on following its duct -
        // which is the state a drawing wants to be left in.
        try { independent.LeaderEndCondition = wasCondition; } catch { }
        upTheSheetLeftStraight.Add(element.Id);
        continue;
    }

    // THE SHOULDER IS THE WHOLE OF WHAT MAKES THE L VISIBLE. With the head
    // sitting directly over the run, the leader drops straight down and there
    // is no corner to see - a plan of clean verticals, and not what anybody
    // means by an L-shaped tag. So the corner is put a DELIBERATE distance to
    // the side of the tag and the drop is taken from there.
    //
    // It leans toward the LONGER half of the run, so the drop lands on the
    // duct rather than past its end.
    double side = 1.0;
    if (runP0 != null && runP1 != null)
    {
        double toP0 = (runP0 - head).DotProduct(right);
        double toP1 = (runP1 - head).DotProduct(right);
        double pick = Math.Abs(toP0) >= Math.Abs(toP1) ? toP0 : toP1;
        if (pick < 0) side = -1.0;
    }

    // Level with the head, one shoulder to the side: the horizontal leg.
    var elbow = head + right * (stub * side);

    // The drop: the point on the run directly under the corner. Solved along
    // the run and clamped to it, so it stays ON the duct instead of floating
    // beside it. Both share an across-coordinate with the corner, so the leg
    // is a true vertical and the pair reads as an L.
    XYZ leaderStart = anchor;
    if (runP0 != null && runP1 != null)
    {
        var runSpan = runP1 - runP0;
        double spanAcross = runSpan.DotProduct(right);
        if (Math.Abs(spanAcross) > 1e-9)
        {
            double t = (elbow - runP0).DotProduct(right) / spanAcross;
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;
            leaderStart = runP0 + runSpan * t;
        }
    }

    try
    {
        if (independent != null)
        {
#if REVIT2020 || REVIT2021
            independent.LeaderEnd = leaderStart;
            independent.LeaderElbow = elbow;
#else
            var references = independent.GetTaggedReferences();
            if (references == null || references.Count == 0) { refused.Add(element.Id); continue; }
            // END FIRST, THEN THE CORNER. Moving the start can disturb a
            // corner already set, so the corner is written last and is what
            // the read-back below checks.
            independent.SetLeaderEnd(references[0], leaderStart);
            independent.SetLeaderElbow(references[0], elbow);
#endif
        }
        else
        {
            spatial.LeaderEnd = leaderStart;
            spatial.LeaderElbow = elbow;
        }
        endsMoved++;
    }
    catch { refused.Add(element.Id); continue; }

    // PUT THE CONDITION BACK, and do not assume the shape survived it.
    // Re-attaching hands the end back to Revit, which may move it off the
    // corner's line and reopen the diagonal this fragment exists to close.
    // That is a real question with a real answer, so it is READ rather than
    // reasoned about: the leg counts as true only if the end and the corner
    // still share an across-coordinate once the tag is attached again.
    if (independent != null)
    {
        // THE END STAYS FREE ON A BENT LEADER, and that is a measured
        // decision rather than an oversight. Putting it back to attached
        // hands the end to Revit, which moves it off the corner and flattens
        // the L - measured here three ways, twelve of twelve each time, with
        // and without a regenerate. Attached and L-shaped cannot both be had
        // through the API.
        //
        // So the trade is made where it costs least: a run lying UP the sheet
        // is never bent and keeps its attached end, and only a run bent
        // ACROSS the sheet pays. Named, never counted quietly - each one is a
        // leader that has stopped following its duct.
        endsLeftFree.Add(element.Id);

        XYZ endNow = null;
        try
        {
#if REVIT2020 || REVIT2021
            endNow = independent.LeaderEnd;
#else
            var refsNow = independent.GetTaggedReferences();
            if (refsNow != null && refsNow.Count > 0) endNow = independent.GetLeaderEnd(refsNow[0]);
#endif
        }
        catch { }

        if (endNow != null && Math.Abs((endNow - elbow).DotProduct(right)) < flatEnough)
        {
            legStillTrue++;
        }
        else if (endNow != null)
        {
            // THE CORNER GOES TO THE END, NOT THE END TO THE CORNER.
            //
            // Dragging the end onto the corner works only while the leader is
            // free, and attaching it again undoes exactly that - measured, all
            // twelve of twelve. Revit owns an attached end and will move it
            // back whatever anybody writes.
            //
            // So let it. It has now chosen where the end sits, and that
            // choice is stable. Build the corner directly over THAT point -
            // its across-coordinate from the end Revit picked, its along-
            // coordinate from the head - and the leg is true without anything
            // needing to be dragged anywhere.
            var elbowAtEnd = endNow + up * ((head - endNow).DotProduct(up));

            try
            {
#if REVIT2020 || REVIT2021
                independent.LeaderElbow = elbowAtEnd;
#else
                var refsFix = independent.GetTaggedReferences();
                if (refsFix != null && refsFix.Count > 0)
                    independent.SetLeaderElbow(refsFix[0], elbowAtEnd);
#endif
            }
            catch { }

            // REGENERATE BEFORE READING. A tag's geometry does not update
            // the instant a property is written, so a read-back taken too
            // early reports the OLD value and looks exactly like a set that
            // was ignored. The same trap the Brain records for measuring a
            // tag box, met from the other side.
            try { doc.Regenerate(); } catch { }

            // READ IT BACK. An attached end is said to make an elbow set a
            // no-op; whether that holds here is a question with an answer,
            // and the answer is in Revit rather than in anybody's reasoning.
            XYZ heldAt = null;
            try
            {
#if REVIT2020 || REVIT2021
                heldAt = independent.LeaderElbow;
#else
                var refsRead = independent.GetTaggedReferences();
                if (refsRead != null && refsRead.Count > 0)
                    heldAt = independent.GetLeaderElbow(refsRead[0]);
#endif
            }
            catch { }

            if (heldAt != null && heldAt.IsAlmostEqualTo(elbowAtEnd))
            {
                legStillTrue++;
                cornerMovedToEnd++;
            }
            else legWentDiagonal.Add(element.Id);
        }
        else legWentDiagonal.Add(element.Id);
    }

    // Read the elbow back. The set call returns whether or not the leader
    // actually bent, which is the failure this fragment exists around.
    XYZ now = null;
    try
    {
        if (independent != null)
        {
#if REVIT2020 || REVIT2021
            now = independent.LeaderElbow;
#else
            var references = independent.GetTaggedReferences();
            if (references != null && references.Count > 0) now = independent.GetLeaderElbow(references[0]);
#endif
        }
        else now = spatial.LeaderElbow;
    }
    catch { }

    if (now != null && now.IsAlmostEqualTo(elbow)) bent++;
    else refused.Add(element.Id);
}
