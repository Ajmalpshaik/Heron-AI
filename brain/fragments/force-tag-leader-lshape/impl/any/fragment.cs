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
var refused = new List<ElementId>();

int scale = 1;
try { scale = Math.Max(1, view.Scale); } catch { }

double leg = legMm * MmToFeet * scale;
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

    if (independent != null)
    {
        try { head = independent.TagHeadPosition; } catch { }
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

    // The shoulder: level with the tag head, a leg's length toward the
    // element. Measured in the view's own axes so it is horizontal on the
    // sheet rather than in world space.
    double towards = (end - head).DotProduct(right) >= 0 ? 1.0 : -1.0;
    var elbow = head + right * (leg * towards);
    // Level with the head: the elbow keeps the head's height in the view.
    elbow = elbow + up * ((head - elbow).DotProduct(up));

    try
    {
        if (independent != null)
        {
#if REVIT2020 || REVIT2021
            independent.LeaderElbow = elbow;
#else
            var references = independent.GetTaggedReferences();
            if (references == null || references.Count == 0) { refused.Add(element.Id); continue; }
            independent.SetLeaderElbow(references[0], elbow);
#endif
        }
        else spatial.LeaderElbow = elbow;
    }
    catch { refused.Add(element.Id); continue; }

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
