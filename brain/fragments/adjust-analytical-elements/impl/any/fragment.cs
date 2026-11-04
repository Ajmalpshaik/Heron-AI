// NOT STANDALONE. Assumes `doc`, `elements`, `offset` and `points` are in
// scope, and leaves `adjusted`, `adjustedIds`, `report`, `noAnalytical`,
// `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Moves the ANALYTICAL line of each beam, column or brace, or the
// analytical outline of each floor or wall, handed in - never the physical
// element - either
//
//   by `offset`   every one by the same distance          SetCurve / SetOuterContour
//   by `points`   ONE redrawn through new points:         of a translated copy,
//                 two for a member, three or more         or of a new line or
//                 corners for a panel                     outline
//
// and reads each one back before and after. One that does not read back
// within a millimetre of where it was asked to go is reported in `skipped`,
// not counted.
//
// POINTS AND THE OFFSET ARRIVE IN FEET. Typed in millimetres, converted by the
// add-in (RevitFragment.OnePoint and ManyPoints); reported back in
// millimetres (D-20).
//
// A PHYSICAL ELEMENT reaches its analytical element through Revit's own
// association (AnalyticalToPhysicalAssociationManager); one with none is named
// in noAnalytical and left alone. An analytical element handed in is used as
// it is. A link is skipped - it follows its two nodes.
//
// NEVER DELETES an element, physical or analytical, and never moves the
// physical one.
//
// RELEASES: 2023-2027, the card's claim (tools/api-surface --members,
// 2026-11-02). None of the analytical classes read here exist on 2020-2022.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var adjusted = 0;
var adjustedIds = new List<ElementId>();
var report = "";
var noAnalytical = new List<ElementId>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<XYZ, string> pt = p => p == null ? "?" : "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> describe = e => e == null ? "?" : (e.Category == null ? "" : e.Category.Name + " ") + quoted(e.Name) + " (" + e.Id + ")";
var tolerance = 1.0 / 304.8;
var lines = new List<string>();
var ok = true;

var move = offset ?? XYZ.Zero;
var byOffset = !move.IsZeroLength();
var newPoints = (points ?? new List<XYZ>()).Where(p => p != null).ToList();
var byPoints = newPoints.Count > 0;
var handed = (elements ?? new List<Element>()).Where(e => e != null).ToList();

if (byOffset && byPoints)
{
    refused.Add("Both an offset and new points were given - one or the other: an offset moves every analytical line "
        + "or outline by the same distance, points redraw one. Nothing was changed.");
    ok = false;
}
else if (!byOffset && !byPoints)
{
    refused.Add("Neither how far to move it nor where to redraw it was given - an offset in mm (\"200 mm down\" is "
        + "0,0,-200) or the new points. Nothing was changed.");
    ok = false;
}
if (ok && handed.Count == 0)
{
    refused.Add("No element was handed in - select the beams, columns, braces, floors or walls, or their analytical "
        + "elements. Nothing was changed.");
    ok = false;
}

// ---- The analytical element of each one handed in ---------------------------------------
AnalyticalToPhysicalAssociationManager manager = null;
try { manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc); } catch { }
Func<ElementId, string> physicalOf = id =>
{
    try
    {
        var other = manager == null ? ElementId.InvalidElementId : manager.GetAssociatedElementId(id);
        return other == null || other == ElementId.InvalidElementId ? "no physical element" : describe(doc.GetElement(other));
    }
    catch { return "no physical element"; }
};
var targets = new List<AnalyticalElement>();
if (ok)
{
    foreach (var e in handed)
    {
        AnalyticalElement a = e as AnalyticalElement;
        if (a == null && !(e is AnalyticalLink))
        {
            try
            {
                var aid = manager == null ? ElementId.InvalidElementId : manager.GetAssociatedElementId(e.Id);
                if (aid != null && aid != ElementId.InvalidElementId) a = doc.GetElement(aid) as AnalyticalElement;
            }
            catch { }
        }
        if (e is AnalyticalLink) { skipped.Add(describe(e) + " - an analytical link follows its two nodes; move them instead."); continue; }
        if (a == null) { noAnalytical.Add(e.Id); continue; }
        if (targets.Any(t => t.Id == a.Id)) continue;
        if (!(a is AnalyticalMember) && !(a is AnalyticalSurfaceBase))
        {
            skipped.Add(describe(e) + " - its analytical element " + a.Id + " is neither a member nor a panel.");
            continue;
        }
        targets.Add(a);
    }
    if (byPoints && targets.Count != 1)
    {
        refused.Add("New points redraw ONE analytical element; " + targets.Count + " were found in what was handed in. "
            + "Select one, or move them all by an offset. Nothing was changed.");
        ok = false;
    }
}

// ---- Change each one, and read it back -----------------------------------------------------
Func<CurveLoop, List<XYZ>> cornersOf = loop =>
{
    var list = new List<XYZ>();
    if (loop != null) foreach (Curve c in loop) list.Add(c.GetEndPoint(0));
    return list;
};
if (ok)
{
    var shift = Transform.CreateTranslation(move);
    foreach (var a in targets)
    {
        var owner = physicalOf(a.Id);
        try
        {
            if (a is AnalyticalMember)
            {
                var member = (AnalyticalMember)a;
                var was = member.GetCurve();
                Curve want;
                if (byOffset) want = was.CreateTransformed(shift);
                else if (newPoints.Count != 2)
                {
                    skipped.Add("analytical member " + a.Id + " (of " + owner + ") - a member's line is redrawn through exactly two "
                        + "points; " + newPoints.Count + " were given. Not changed.");
                    continue;
                }
                else want = Line.CreateBound(newPoints[0], newPoints[1]);
                if (!AnalyticalMember.IsValidCurve(want))
                {
                    skipped.Add("analytical member " + a.Id + " (of " + owner + ") - Revit says that line cannot be an analytical member. Not changed.");
                    continue;
                }
                member.SetCurve(want);
                doc.Regenerate();
                var now = member.GetCurve();
                var there = now != null && now.GetEndPoint(0).IsAlmostEqualTo(want.GetEndPoint(0), tolerance)
                    && now.GetEndPoint(1).IsAlmostEqualTo(want.GetEndPoint(1), tolerance);
                var line = "analytical member " + a.Id + " of " + owner + ": was " + pt(was.GetEndPoint(0)) + " to "
                    + pt(was.GetEndPoint(1)) + ", now " + (now == null ? "?" : pt(now.GetEndPoint(0)) + " to " + pt(now.GetEndPoint(1)));
                if (there) { adjusted++; adjustedIds.Add(a.Id); lines.Add(line); }
                else skipped.Add(line + " - not where it was asked to go (" + pt(want.GetEndPoint(0)) + " to " + pt(want.GetEndPoint(1)) + "); Revit held it.");
            }
            else
            {
                var panel = (AnalyticalSurfaceBase)a;
                var was = panel.GetOuterContour();
                CurveLoop want;
                if (byOffset) want = CurveLoop.CreateViaTransform(was, shift);
                else if (newPoints.Count < 3)
                {
                    skipped.Add("analytical panel " + a.Id + " (of " + owner + ") - an outline needs three or more corners; "
                        + newPoints.Count + " were given. Not changed.");
                    continue;
                }
                else
                {
                    want = new CurveLoop();
                    for (var i = 0; i < newPoints.Count; i++) want.Append(Line.CreateBound(newPoints[i], newPoints[(i + 1) % newPoints.Count]));
                }
                var before = cornersOf(was);
                var asked = cornersOf(want);
                panel.SetOuterContour(want);
                doc.Regenerate();
                var after = cornersOf(panel.GetOuterContour());
                var there = after.Count == asked.Count && asked.All(p => after.Any(q => q.IsAlmostEqualTo(p, tolerance)));
                var line = "analytical panel " + a.Id + " of " + owner + ": was " + string.Join("; ", before.Select(pt))
                    + ", now " + string.Join("; ", after.Select(pt));
                if (there) { adjusted++; adjustedIds.Add(a.Id); lines.Add(line); }
                else skipped.Add(line + " - not where it was asked to go; Revit held it.");
            }
        }
        catch (Exception ex)
        {
            skipped.Add("analytical element " + a.Id + " (of " + owner + ") - Revit would not change it: " + ex.Message);
        }
    }
    if (noAnalytical.Count > 0)
        findings.Add(noAnalytical.Count + " element(s) have no analytical element, so nothing was moved for them: "
            + string.Join(", ", noAnalytical.Select(id => describe(doc.GetElement(id)))) + ".");
}

report = string.Join("\n", lines);
if (adjusted > 0)
    findings.Add(adjusted + " analytical line(s) or outline(s) changed" + (byOffset ? " by " + pt(move) : " to the points given")
        + "; the physical elements were not moved.");
else findings.Add("No analytical line or outline was changed - the model is as it was.");
