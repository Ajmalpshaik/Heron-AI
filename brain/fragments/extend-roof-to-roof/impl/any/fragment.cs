// NOT STANDALONE. Assumes `doc`, `view3d`, `elements`, `maxExtendMm` and
// `extend` are in scope; leaves `extended`, `extendedRoof`, `needMm`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET
// inside; the one length a caller types, maxExtendMm, arrives in millimetres
// and is divided by 304.8 here, once (D-71).
//
// ===========================================================================
// REVIT'S API HAS NO JOIN/UNJOIN ROOF, SO THIS MEASURES AND EXTENDS INSTEAD.
// ===========================================================================
//
// Row 5b-1877, the owner's way (issue #1087): cast a ray from the end of one
// roof to the other roof's face, work out how far the first has to extend,
// and extend it there. Read in RevitAPI.xml 2020.2.90 to 2027.4.0 on
// 2026-11-20:
//
//   ROOF BY EXTRUSION  its extent is its "Extrusion Start" and "Extrusion End"
//                      (EXTRUSION_START_PARAM, EXTRUSION_END_PARAM) on every
//                      release - so its end CAN be moved, and this moves it.
//   ROOF BY FOOTPRINT  its edges are its sketch, and FootPrintRoof exposes no
//                      sketch to edit on any release (no SketchId; only
//                      GetProfiles and each edge's Offset, Overhang,
//                      SlopeAngle, DefinesSlope and ExtendIntoWall), and
//                      Overhang is documented "only applicable when the
//                      FootPrintRoof is created with Pick Walls". So a
//                      footprint roof is NEVER the one extended here: the
//                      answer says so, naming the release, and the join is
//                      done by hand. It can be the roof that is MET.
//
// WHICH WAY REVIT MEASURES EXTRUSION START AND END is not written anywhere
// (CREATE_EXTRUSION_ROOF's note says the same). So it is not assumed: the
// roof's own geometry says where its two ends are along the profile plane's
// normal, the two parameters are matched to them - either sign - and after
// the write the geometry is READ BACK. An end that did not move by the
// distance measured, or an other end that moved at all, puts the parameter
// back and is said; nothing is called extended off the write alone.
//
// THE RAYS SEE ONLY WHAT THE 3D VIEW SHOWS (MOVE_TO_RAY_HIT learned it). They
// are aimed at the OTHER ROOF ALONE - ReferenceIntersector given that roof's
// id - so nothing else in the view can be hit by mistake; but if that roof is
// hidden or cut away in the view, every ray misses and nothing changes.
//
// THE FARTHEST HIT IS THE DISTANCE. The end's outline points are each cast
// toward the other roof, and the end is moved by the FARTHEST of them, so no
// part of the end stops short of the other roof's face; where the face is
// nearer, the end then runs into the other roof by the difference, which is
// said. JOIN_GEOMETRY is the next step that cleans that overlap - offered,
// never run from here.
//
// NOTHING CHANGES WITHOUT A LIMIT. maxExtendMm left out, the distance is
// measured and said and nothing is changed - the owner's "offered first, and
// run only on your yes". Given, the end is moved only if the distance needed
// is within it.

const double MillimetresPerFoot = 304.8;
var oneMillimetre = 1.0 / MillimetresPerFoot;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot).ToString("0", invariant);

var extended = 0;
var extendedRoof = ElementId.InvalidElementId;
var needMm = double.NaN;
var report = "";
string refused = null;
var findings = new List<string>();

var release = doc.Application.VersionNumber;
var lines = new List<string>();
Func<Element, string> labelOf = e => string.Format("{0} {1} '{2}'",
    e is ExtrusionRoof ? "roof by extrusion" : (e is FootPrintRoof ? "roof by footprint" : "roof"),
    e.Id, e.Name);

// EVERY POINT OF A ROOF'S EDGES, from its geometry as Revit holds it now.
Func<Element, List<XYZ>> pointsOf = e =>
{
    var found = new List<XYZ>();
    Action<GeometryElement> collect = null;
    collect = geometry =>
    {
        if (geometry == null) return;
        foreach (GeometryObject piece in geometry)
        {
            var solid = piece as Solid;
            if (solid != null && solid.Volume > 0)
            {
                foreach (Edge edge in solid.Edges)
                    foreach (var point in edge.Tessellate())
                        found.Add(point);
                continue;
            }
            var instance = piece as GeometryInstance;
            if (instance != null) collect(instance.GetInstanceGeometry());
        }
    };
    collect(e.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine }));
    return found;
};

// THE POINTS OF ONE END: those furthest along `toward`, to 1 mm, each once.
Func<List<XYZ>, XYZ, List<XYZ>> endOf = (points, toward) =>
{
    var end = new List<XYZ>();
    if (points.Count == 0) return end;
    var furthest = points.Max(p => p.DotProduct(toward));
    foreach (var p in points)
    {
        if (p.DotProduct(toward) < furthest - oneMillimetre) continue;
        if (end.Any(q => q.DistanceTo(p) < oneMillimetre)) continue;
        end.Add(p);
    }
    return end;
};

var roofs = new List<RoofBase>();
foreach (var element in elements ?? new List<Element>())
{
    var roof = element as RoofBase;
    if (roof != null) roofs.Add(roof);
    else if (element != null)
        findings.Add(string.Format("{0} {1} is not a roof and was left alone",
            element.Category == null ? "element" : element.Category.Name, element.Id));
}

if (view3d == null || view3d.IsTemplate)
{
    refused = "A 3D view to measure in is needed - one that shows both roofs, with no section box "
        + "cutting them - and " + (view3d == null ? "none was given" : "the view given is a template")
        + ". Nothing was changed";
}
else if (roofs.Count != 2)
{
    refused = string.Format("This extends one roof to meet ANOTHER, so it takes exactly TWO roofs - "
        + "{0} roof(s) were handed in{1}. Nothing was changed", roofs.Count,
        roofs.Count == 0 ? "" : ": " + string.Join(", ", roofs.Select(r => labelOf(r))));
}
else if (roofs.Any(r => r.GroupId != ElementId.InvalidElementId))
{
    var grouped = roofs.First(r => r.GroupId != ElementId.InvalidElementId);
    refused = string.Format("{0} is in a group, and a roof in a group is changed by editing the "
        + "group. Nothing was changed", labelOf(grouped));
}
else if (!roofs.Any(r => r is ExtrusionRoof))
{
    refused = string.Format("Both roofs are roofs by footprint, and Revit {0}'s API cannot extend "
        + "one: a footprint roof's edges are its sketch, and the API gives no way to edit that "
        + "sketch from an add-in (Overhang moves an edge only on a roof made with Pick Walls). Join "
        + "them by hand: Modify > Join/Unjoin Roof, then pick the edge of the roof to extend and the "
        + "face it is to meet. Nothing was changed: {1}", release,
        string.Join(", ", roofs.Select(r => labelOf(r))));
}
else
{
    // EVERY WAY ONE ROOF'S END COULD MEET THE OTHER: each roof by extrusion,
    // each of its two ends, its rays aimed at the other roof alone.
    var tries = new List<Tuple<ExtrusionRoof, RoofBase, XYZ, double, double, int, int>>();
    foreach (var roof in roofs)
    {
        var moving = roof as ExtrusionRoof;
        if (moving == null) continue;
        var other = roofs.First(r => r.Id != roof.Id);
        ModelCurve firstEdge = null;
        foreach (ModelCurve curve in moving.GetProfile()) { firstEdge = curve; break; }
        if (firstEdge == null || firstEdge.SketchPlane == null)
        {
            findings.Add(string.Format("{0}: Revit gave no profile to read its direction from, so "
                + "it was not measured", labelOf(moving)));
            continue;
        }
        var normal = firstEdge.SketchPlane.GetPlane().Normal.Normalize();
        var points = pointsOf(moving);
        var intersector = new ReferenceIntersector(other.Id, FindReferenceTarget.Face, view3d);
        foreach (var sign in new[] { 1.0, -1.0 })
        {
            var toward = normal.Multiply(sign);
            var end = endOf(points, toward);
            int hit = 0, miss = 0;
            double nearest = double.MaxValue, farthest = 0.0;
            foreach (var from in end)
            {
                var hits = intersector.Find(from, toward);
                var best = double.MaxValue;
                if (hits != null)
                    foreach (var h in hits)
                        if (h.GetReference() != null && h.GetReference().ElementId == other.Id
                            && h.Proximity < best)
                            best = h.Proximity;
                if (best == double.MaxValue) { miss++; continue; }
                hit++;
                nearest = Math.Min(nearest, best);
                farthest = Math.Max(farthest, best);
            }
            lines.Add(string.Format(invariant, "{0}, its end toward ({1:0.###}, {2:0.###}, {3:0.###}): {4} of {5} "
                + "point(s) of its end meet {6}{7}", labelOf(moving), toward.X, toward.Y, toward.Z,
                hit, end.Count, labelOf(other),
                hit == 0 ? "" : ", nearest " + mm(nearest) + " mm, farthest " + mm(farthest) + " mm away"));
            if (hit > 0)
                tries.Add(Tuple.Create(moving, other, toward, nearest, farthest, hit, miss));
        }
    }

    var wanted = (extend ?? "").Trim();
    if (wanted.Length > 0)
    {
        if (!roofs.Any(r => r.Id.ToString() == wanted))
            refused = string.Format("extend names roof {0}, and the two roofs handed in are {1}. "
                + "Nothing was changed", wanted, string.Join(", ", roofs.Select(r => labelOf(r))));
        tries = tries.Where(t => t.Item1.Id.ToString() == wanted).ToList();
    }

    if (refused != null)
    {
        // said above
    }
    else if (tries.Count == 0)
    {
        refused = string.Format("NOTHING MET: no ray from either end of a roof by extrusion met the "
            + "other roof in the 3D view '{0}'. Is the other roof showing in that view - not hidden, "
            + "not cut away by a section box - and does the end face it? Nothing was changed",
            view3d.Name);
    }
    else if (tries.Count > 1)
    {
        refused = "MORE THAN ONE END MEETS THE OTHER ROOF, and which one to extend is the modeller's "
            + "to say: " + string.Join("; ", tries.Select(t => string.Format(invariant, "{0}, toward "
            + "({1:0.###}, {2:0.###}, {3:0.###}), {4} mm", labelOf(t.Item1), t.Item3.X, t.Item3.Y,
            t.Item3.Z, mm(t.Item5)))) + ". Name the roof to extend with extend, or select a pair "
            + "where only one end faces the other roof. Nothing was changed";
    }
    else
    {
        var chosen = tries[0];
        var roof = chosen.Item1;
        var other = chosen.Item2;
        var toward = chosen.Item3;
        var distance = chosen.Item5;
        needMm = distance * MillimetresPerFoot;
        if (chosen.Item7 > 0)
            findings.Add(string.Format("{0} of the {1} point(s) of the end met no face of {2}: those "
                + "parts of the end pass beside or under it, and do not reach it however far the end "
                + "goes", chosen.Item7, chosen.Item6 + chosen.Item7, labelOf(other)));
        if (distance <= oneMillimetre)
        {
            refused = string.Format("{0} already meets {1} - its farthest point is {2} mm from it. "
                + "Nothing was changed", labelOf(roof), labelOf(other), mm(distance));
        }
        else if (double.IsNaN(maxExtendMm))
        {
            refused = string.Format("MEASURED, NOTHING CHANGED: {0} has to extend {1} mm for its end "
                + "to meet {2} (its nearest point is {3} mm away). To extend it, ask again with "
                + "maxExtendMm at least {1}", labelOf(roof), mm(distance), labelOf(other),
                mm(chosen.Item4));
        }
        else if (distance > maxExtendMm / MillimetresPerFoot + oneMillimetre)
        {
            refused = string.Format("{0} has to extend {1} mm to meet {2}, more than the {3} mm "
                + "allowed. Nothing was changed", labelOf(roof), mm(distance), labelOf(other),
                maxExtendMm.ToString("0", invariant));
        }
        else
        {
            var start = roof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM);
            var finish = roof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
            if (start == null || finish == null || start.IsReadOnly || finish.IsReadOnly)
            {
                refused = string.Format("Revit gives {0} no Extrusion Start and End that can be set. "
                    + "Nothing was changed", labelOf(roof));
            }
            else
            {
                ModelCurve firstEdge = null;
                foreach (ModelCurve curve in roof.GetProfile()) { firstEdge = curve; break; }
                var plane = firstEdge.SketchPlane.GetPlane();
                var normal = plane.Normal.Normalize();
                var origin = plane.Origin.DotProduct(normal);
                var sign = toward.DotProduct(normal) > 0 ? 1.0 : -1.0;
                var before = pointsOf(roof);
                var hereBefore = before.Max(p => p.DotProduct(toward));
                var backBefore = before.Max(p => p.DotProduct(toward.Negate()));
                var s0 = start.AsDouble();
                var e0 = finish.AsDouble();
                var low = Math.Min(s0, e0);
                var high = Math.Max(s0, e0);
                var alongNormal = before.Max(p => p.DotProduct(normal));
                var againstNormal = before.Min(p => p.DotProduct(normal));
                Func<double, double, bool> near = (a, b) => Math.Abs(a - b) <= oneMillimetre;

                // Which sign Revit measures the two along - read off the roof, both tried
                // where both agree (a roof whose two extents are equal and opposite).
                var ways = new List<double>();
                if (near(alongNormal, origin + high) && near(againstNormal, origin + low)) ways.Add(1.0);
                if (near(alongNormal, origin - low) && near(againstNormal, origin - high)) ways.Add(-1.0);
                if (ways.Count == 0)
                {
                    refused = string.Format("{0}'s ends could not be matched to its Extrusion Start "
                        + "({1} mm) and End ({2} mm) - its ends are {3} and {4} mm along its plane's "
                        + "normal from the plane - so which to change was not guessed. Nothing was "
                        + "changed", labelOf(roof), mm(s0), mm(e0), mm(againstNormal - origin),
                        mm(alongNormal - origin));
                }
                foreach (var k in ways)
                {
                    // The parameter whose end faces the other roof, and its new value.
                    var startAt = origin + k * s0;
                    var finishAt = origin + k * e0;
                    var useStart = sign * startAt >= sign * finishAt;
                    var which = useStart ? start : finish;
                    var was = useStart ? s0 : e0;
                    var wasSet = which.Set(was + sign * k * distance);
                    doc.Regenerate();
                    var after = pointsOf(roof);
                    var hereAfter = after.Count == 0 ? double.NaN : after.Max(p => p.DotProduct(toward));
                    var backAfter = after.Count == 0 ? double.NaN : after.Max(p => p.DotProduct(toward.Negate()));
                    if (wasSet && near(hereAfter, hereBefore + distance) && near(backAfter, backBefore))
                    {
                        extended = 1;
                        extendedRoof = roof.Id;
                        refused = null;
                        lines.Add(string.Format("EXTENDED: {0}'s {1} is {2} mm (was {3} mm), and its "
                            + "end moved {4} mm toward {5} - read back off the roof's geometry, its "
                            + "other end where it was", labelOf(roof),
                            useStart ? "Extrusion Start" : "Extrusion End",
                            mm(which.AsDouble()), mm(was), mm(hereAfter - hereBefore), labelOf(other)));
                        if (chosen.Item5 - chosen.Item4 > oneMillimetre)
                            findings.Add(string.Format("The end now runs up to {0} mm into {1} where "
                                + "that roof was nearest. JOIN_GEOMETRY on the two roofs cuts the "
                                + "overlap - offered, not run", mm(chosen.Item5 - chosen.Item4),
                                labelOf(other)));
                        break;
                    }
                    which.Set(was);
                    doc.Regenerate();
                    refused = string.Format("Revit did not move {0}'s end by changing its {1}: read "
                        + "back, the end moved {2} mm and its other end {3} mm, where {4} mm and 0 "
                        + "were asked. It was put back. Nothing was changed", labelOf(roof),
                        useStart ? "Extrusion Start" : "Extrusion End",
                        double.IsNaN(hereAfter) ? "?" : mm(hereAfter - hereBefore),
                        double.IsNaN(backAfter) ? "?" : mm(backAfter - backBefore), mm(distance));
                }
            }
        }
    }
}

report = string.Join(" | ", lines);
if (extended == 1)
{
    findings.Insert(0, "1 roof extended to meet the other - " + lines.Last());
    findings.Add(string.Format("Revit {0}'s API has no Join/Unjoin Roof, so this moved the end of "
        + "the roof by extrusion instead, by the distance its rays measured. It is not a join: if "
        + "the other roof moves, this end does not follow it", release));
}
else
{
    findings.Insert(0, "Nothing was changed - see refused for why");
    findings.Add(string.Format("Revit {0}'s API has no Join/Unjoin Roof; this tool extends a roof "
        + "by extrusion's end to meet another roof, measured by rays, and nothing else", release));
}
