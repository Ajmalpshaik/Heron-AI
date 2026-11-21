// NOT STANDALONE. Assumes `doc`, `editScopeFailures`, `view3d`, `elements`,
// `maxExtendMm` and `extend` are in scope; leaves `extended`, `extendedRoof`,
// `needMm`, `report`, `refused` and `findings` behind.
//
// RUNS WITH NO TRANSACTION OPEN, INSIDE HERON'S GROUP (D-112) - since
// version 2. A roof by footprint's edges are its sketch, edited only inside a
// SketchEditScope, which "can only be started when there is no transaction
// active" (RevitAPI.xml, SketchEditScope.Start). So the card declares the
// ambient `editScopeFailures` need and opens its own transactions - one for a
// roof by extrusion's Extrusion Start or End, an edit scope and one inside it
// for a footprint's end line - each handed that need, so Revit's words reach
// the answer, and the group still makes the whole job one undo that rolls
// back on a preview. Lengths are internal FEET inside; the one length a caller
// types, maxExtendMm, arrives in millimetres and is divided by 304.8 here,
// once (D-71).
//
// ===========================================================================
// REVIT'S API HAS NO JOIN/UNJOIN ROOF, SO THIS MEASURES AND EXTENDS INSTEAD.
// ===========================================================================
//
// Row 5b-1877, the owner's way (issue #1087): cast a ray from the end of one
// roof to the other roof's face, work out how far the first has to extend,
// and extend it there. Read in RevitAPI.xml 2020.2.90 to 2027.4.0:
//
//   ROOF BY EXTRUSION  its extent is its "Extrusion Start" and "Extrusion End"
//                      (EXTRUSION_START_PARAM, EXTRUSION_END_PARAM) on every
//                      release - so its end CAN be moved, and this moves it.
//   ROOF BY FOOTPRINT  its edges are its sketch. From 2022 the sketch is found
//                      among the roof's dependents and edited in a
//                      SketchEditScope: the GABLE END line facing the other
//                      roof - one that does not define slope - is moved out
//                      by the distance, and the two lines beside it are
//                      lengthened to the new corners, each by
//                      CurveElement.SetGeometryCurve, so every line keeps its
//                      own slope settings (version 2, row 5b-1930, issue
//                      #1143). On 2020 and 2021 there is no SketchEditScope,
//                      and the answer says so, naming the release.
//
// NOTHING ABOUT THE FOOTPRINT WAY HAS BEEN SEEN IN REVIT. Whether Revit lets
// an add-in edit a roof's sketch, whether SetGeometryCurve moves a footprint
// line in edit mode, and whether the roof is rebuilt from it are all read
// back, not assumed: the end has to have moved by the distance, the other end
// not at all, and every footprint line has to keep the Defines Slope and
// Slope it had - or this THROWS, and the whole call is rolled back. The first
// Revit run of this is the look row 5b-1930 asked for (NEEDS-CHECKING Group
// OY, OY6 to OY8).
//
// WHICH WAY START AND END GO. NewExtrusionRoof's reference says both are
// "measured from refPlane in the direction of the plane normal" - but the
// profile's own sketch plane is what this can read, and its normal may point
// the other way from the reference plane's. So the direction is read off the
// roof's own geometry, either sign, and after the write the geometry is READ
// BACK; a write that did not move the end as asked is rolled back and said.
//
// THE RAYS SEE ONLY WHAT THE 3D VIEW SHOWS (MOVE_TO_RAY_HIT learned it). They
// are aimed at the OTHER ROOF ALONE - ReferenceIntersector given that roof's
// id - so nothing else in the view can be hit by mistake; but if that roof is
// hidden or cut away in the view, every ray misses and nothing changes.
//
// ONLY THE OTHER ROOF'S UPPER FACE COUNTS. A ray from low on the end can pass
// UNDER the near slope and meet the underside of the far slope, or the far
// eave, a whole roof away; so a hit counts only on a face that looks up (its
// normal's Z above 0.05) and toward the ray. The misses are counted and said.
//
// THE FARTHEST HIT IS THE DISTANCE, AND IT OVERSHOOTS. The end is flat, and
// it is moved by the FARTHEST of its points' hits, so no part of it stops
// short of the other roof's face; every other part goes further - through the
// other roof, and below the ridge on into the space under it. JOIN_GEOMETRY
// cleans only where the two overlap; the part under the other roof stays,
// seen in a section. Revit's own Join/Unjoin Roof cuts the end to the face.
//
// THE POINTS ARE THE END'S CORNERS AND EDGE POINTS, as Revit tessellates the
// edges: a straight edge gives its two ends. Against a curved roof, or across
// a valley of the other roof, the farthest gap can lie between two points.
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
#if REVIT2020 || REVIT2021
const bool footprintsEditable = false;
#else
const bool footprintsEditable = true;
#endif
var noFootprint = string.Format("Revit {0}'s API has no sketch editing for an add-in (SketchEditScope "
    + "came in 2022), so this tool cannot extend a roof by footprint on this release. Join it by hand: "
    + "Modify > Join/Unjoin Roof, then pick the edge of the roof to extend and the face it is to meet",
    release);

// THE ADD-IN'S FAILURE DISCIPLINE ON EACH OF THIS CARD'S OWN TRANSACTIONS.
Action<Transaction> discipline = t =>
{
    var options = t.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(editScopeFailures);
    options.SetClearAfterRollback(true);
    t.SetFailureHandlingOptions(options);
};
Func<string> revitSaid = () =>
{
    var text = editScopeFailures == null ? null : editScopeFailures.ToString();
    return text != null && text.StartsWith("Revit would not let it through", StringComparison.Ordinal)
        ? " - " + text : "";
};

// EVERY CORNER AND EDGE POINT OF A ROOF, from its geometry as Revit holds it now.
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

// WHETHER A HIT IS ON THE OTHER ROOF'S UPPER FACE, looking back at the ray.
Func<RoofBase, ReferenceWithContext, XYZ, XYZ, bool> onUpperFace = (other, hit, from, toward) =>
{
    var reference = hit.GetReference();
    if (reference == null || reference.ElementId != other.Id) return false;
    var face = other.GetGeometryObjectFromReference(reference) as Face;
    if (face == null) return false;
    var at = face.Project(from.Add(toward.Multiply(hit.Proximity)));
    if (at == null) return false;
    var normal = face.ComputeNormal(at.UVPoint);
    return normal.Z > 0.05 && normal.DotProduct(toward) < 0;
};

// EACH FOOTPRINT LINE'S DEFINES SLOPE AND SLOPE, by its id - read before the
// sketch is edited and again after, and they must agree.
Func<FootPrintRoof, Dictionary<string, string>> slopesOf = roof =>
{
    var held = new Dictionary<string, string>();
    foreach (ModelCurveArray loop in roof.GetProfiles())
        foreach (ModelCurve curve in loop)
            held[curve.Id.ToString()] = roof.get_DefinesSlope(curve)
                ? "slope " + roof.get_SlopeAngle(curve).ToString("0.######", invariant) : "no slope";
    return held;
};

var roofs = new List<RoofBase>();
foreach (var element in elements ?? new List<Element>())
{
    var roof = element as RoofBase;
    if (roof != null) roofs.Add(roof);
    else if (element != null)
        findings.Add(string.Format("{0} {1} is not a roof by extrusion or by footprint - an in-place "
            + "roof, or not a roof - and was left alone",
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
else if (!footprintsEditable && !roofs.Any(r => r is ExtrusionRoof))
{
    refused = string.Format("Both roofs are roofs by footprint, and {0}. Nothing was changed: {1}",
        noFootprint, string.Join(", ", roofs.Select(r => labelOf(r))));
}
else
{
    var wanted = (extend ?? "").Trim();
    var named = wanted.Length == 0 ? null : roofs.FirstOrDefault(r => r.Id.ToString() == wanted);
    if (wanted.Length > 0 && named == null)
        refused = string.Format("extend names roof {0}, and the two roofs handed in are {1}. Nothing "
            + "was changed", wanted, string.Join(", ", roofs.Select(r => labelOf(r))));
    else if (named != null && named is FootPrintRoof && !footprintsEditable)
        refused = string.Format("extend names {0}, and {1}. Nothing was changed", labelOf(named),
            noFootprint);
    else if (named != null && !(named is ExtrusionRoof) && !(named is FootPrintRoof))
        refused = string.Format("extend names {0}, which is neither a roof by extrusion nor a roof by "
            + "footprint. Nothing was changed", labelOf(named));

    // EVERY WAY ONE ROOF'S END COULD MEET THE OTHER: each roof that can be
    // extended (only the one named, when one is), each end of it - a roof by
    // extrusion's two, a footprint's gable-end lines - its rays aimed at the
    // other roof alone. `edges` holds, beside each try, the footprint line
    // that is moved, or null for a roof by extrusion.
    var tries = new List<Tuple<RoofBase, RoofBase, XYZ, double, double, int, int>>();
    var edges = new List<ModelCurve>();
    Action<RoofBase, RoofBase, XYZ, ModelCurve, List<XYZ>> measure = (moving, other, toward, edge, points) =>
    {
        var intersector = new ReferenceIntersector(other.Id, FindReferenceTarget.Face, view3d);
        var end = endOf(points, toward);
        int hit = 0, miss = 0;
        double nearest = double.MaxValue, farthest = 0.0;
        foreach (var from in end)
        {
            var hits = intersector.Find(from, toward);
            var best = double.MaxValue;
            if (hits != null)
                foreach (var h in hits)
                    if (h.Proximity < best && onUpperFace(other, h, from, toward))
                        best = h.Proximity;
            if (best == double.MaxValue) { miss++; continue; }
            hit++;
            nearest = Math.Min(nearest, best);
            farthest = Math.Max(farthest, best);
        }
        lines.Add(string.Format(invariant, "{0}, its end toward ({1:0.###}, {2:0.###}, {3:0.###}){4}: {5} "
            + "of {6} point(s) of its end meet the upper face of {7}{8}", labelOf(moving), toward.X,
            toward.Y, toward.Z, edge == null ? "" : " (footprint line " + edge.Id + ")", hit, end.Count,
            labelOf(other),
            hit == 0 ? "" : ", nearest " + mm(nearest) + " mm, farthest " + mm(farthest) + " mm away"));
        if (hit > 0)
        {
            tries.Add(Tuple.Create(moving, other, toward, nearest, farthest, hit, miss));
            edges.Add(edge);
        }
    };
    foreach (var roof in roofs)
    {
        if (refused != null) break;
        if (named != null && named.Id != roof.Id) continue;
        var other = roofs.First(r => r.Id != roof.Id);
        var asExtrusion = roof as ExtrusionRoof;
        var asFootprint = roof as FootPrintRoof;
        if (asExtrusion != null)
        {
            ModelCurve firstEdge = null;
            foreach (ModelCurve curve in asExtrusion.GetProfile()) { firstEdge = curve; break; }
            if (firstEdge == null || firstEdge.SketchPlane == null)
            {
                findings.Add(string.Format("{0}: Revit gave no profile to read its direction from, so "
                    + "it was not measured", labelOf(roof)));
                continue;
            }
            var normal = firstEdge.SketchPlane.GetPlane().Normal.Normalize();
            var points = pointsOf(roof);
            foreach (var sign in new[] { 1.0, -1.0 })
                measure(roof, other, normal.Multiply(sign), null, points);
        }
        else if (asFootprint != null && footprintsEditable)
        {
            // A GABLE END: a straight footprint line that defines no slope, and
            // is the outermost line of its loop in the direction it faces.
            var points = pointsOf(roof);
            foreach (ModelCurveArray loop in asFootprint.GetProfiles())
            {
                var loopCurves = new List<ModelCurve>();
                foreach (ModelCurve curve in loop) loopCurves.Add(curve);
                var ends = loopCurves.SelectMany(c => new[] { c.GeometryCurve.GetEndPoint(0),
                                                              c.GeometryCurve.GetEndPoint(1) }).ToList();
                if (ends.Count == 0) continue;
                var middle = new XYZ(ends.Average(p => p.X), ends.Average(p => p.Y), ends.Average(p => p.Z));
                foreach (var curve in loopCurves)
                {
                    var line = curve.GeometryCurve as Line;
                    if (line == null || asFootprint.get_DefinesSlope(curve)) continue;
                    var along = line.Direction;
                    var facing = new XYZ(-along.Y, along.X, 0);
                    if (facing.GetLength() < 1e-9) continue;
                    facing = facing.Normalize();
                    var centre = line.Evaluate(0.5, true);
                    if (facing.DotProduct(centre - middle) < 0) facing = facing.Negate();
                    if (centre.DotProduct(facing) < ends.Max(p => p.DotProduct(facing)) - oneMillimetre)
                        continue;
                    measure(roof, other, facing, curve, points);
                }
            }
        }
    }

    if (refused != null)
    {
        // said above
    }
    else if (tries.Count == 0)
    {
        refused = string.Format("NOTHING MET: no ray from an end this tool can move met the upper face "
            + "of the other roof in the 3D view '{0}'{1}. Is the other roof showing in that view - not "
            + "hidden, not cut away by a section box - and does the end face it? Nothing was changed",
            view3d.Name, footprintsEditable ? " (a roof by footprint is measured from its gable ends - "
            + "lines that define no slope)" : "");
        if (!footprintsEditable && roofs.Any(r => r is FootPrintRoof))
            findings.Add(noFootprint);
    }
    else if (tries.Count > 1)
    {
        var oneRoof = tries.All(t => t.Item1.Id == tries[0].Item1.Id);
        refused = "MORE THAN ONE END MEETS THE OTHER ROOF, and which one to extend is the modeller's "
            + "to say: " + string.Join("; ", tries.Select(t => string.Format(invariant, "{0}, toward "
            + "({1:0.###}, {2:0.###}, {3:0.###}), {4} mm", labelOf(t.Item1), t.Item3.X, t.Item3.Y,
            t.Item3.Z, mm(t.Item5)))) + (oneRoof
                ? ". Two ends of the same roof meet it, which naming the roof cannot settle - extend "
                  + "it by hand, or select a pair where only one end faces the other roof"
                : ". Name the roof to extend with extend")
            + ". Nothing was changed";
    }
    else
    {
        var chosen = tries[0];
        var roof = chosen.Item1;
        var other = chosen.Item2;
        var toward = chosen.Item3;
        var distance = chosen.Item5;
        var edge = edges[0];
        needMm = distance * MillimetresPerFoot;
        if (chosen.Item7 > 0)
            findings.Add(string.Format("{0} of the {1} point(s) of the end met no upper face of {2}: "
                + "those parts of the end pass beside or under it, and do not reach it however far "
                + "the end goes", chosen.Item7, chosen.Item6 + chosen.Item7, labelOf(other)));
        if (roof.GroupId != ElementId.InvalidElementId)
        {
            refused = string.Format("{0} is in a group, and a roof in a group is changed by editing the "
                + "group. Its end has to move {1} mm. Nothing was changed", labelOf(roof), mm(distance));
        }
        else if (distance <= oneMillimetre)
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
            Func<double, double, bool> near = (a, b) => Math.Abs(a - b) <= oneMillimetre;
            var before = pointsOf(roof);
            var hereBefore = before.Max(p => p.DotProduct(toward));
            var backBefore = before.Max(p => p.DotProduct(toward.Negate()));
            string movedWhat = null;

            if (edge == null)
            {
                // ---- A ROOF BY EXTRUSION: its Extrusion Start or End --------------
                var start = roof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM);
                var finish = roof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
                ModelCurve firstEdge = null;
                foreach (ModelCurve curve in ((ExtrusionRoof)roof).GetProfile()) { firstEdge = curve; break; }
                if (start == null || finish == null || start.IsReadOnly || finish.IsReadOnly)
                {
                    refused = string.Format("Revit gives {0} no Extrusion Start and End that can be set. "
                        + "Nothing was changed", labelOf(roof));
                }
                else
                {
                    var plane = firstEdge.SketchPlane.GetPlane();
                    var normal = plane.Normal.Normalize();
                    var origin = plane.Origin.DotProduct(normal);
                    var sign = toward.DotProduct(normal) > 0 ? 1.0 : -1.0;
                    var s0 = start.AsDouble();
                    var e0 = finish.AsDouble();
                    var low = Math.Min(s0, e0);
                    var high = Math.Max(s0, e0);
                    var alongNormal = before.Max(p => p.DotProduct(normal));
                    var againstNormal = before.Min(p => p.DotProduct(normal));
                    var ways = new List<double>();
                    if (near(alongNormal, origin + high) && near(againstNormal, origin + low)) ways.Add(1.0);
                    if (near(alongNormal, origin - low) && near(againstNormal, origin - high)) ways.Add(-1.0);
                    if (ways.Count == 0)
                        refused = string.Format("{0}'s ends could not be matched to its Extrusion Start "
                            + "({1} mm) and End ({2} mm) - its ends are {3} and {4} mm along its plane's "
                            + "normal from the plane - so which to change was not guessed. Nothing was "
                            + "changed", labelOf(roof), mm(s0), mm(e0), mm(againstNormal - origin),
                            mm(alongNormal - origin));
                    foreach (var k in ways)
                    {
                        var useStart = sign * (origin + k * s0) >= sign * (origin + k * e0);
                        var name = useStart ? "Extrusion Start" : "Extrusion End";
                        var was = useStart ? s0 : e0;
                        double hereAfter = double.NaN, backAfter = double.NaN, now = was;
                        // ONE TRANSACTION A TRY: kept only if the read-back agrees,
                        // else rolled back - nothing to put back by hand.
                        using (var t = new Transaction(doc, "Heron: extend the roof by extrusion"))
                        {
                            t.Start();
                            discipline(t);
                            var which = useStart ? roof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM)
                                                 : roof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
                            var wasSet = which.Set(was + sign * k * distance);
                            doc.Regenerate();
                            var after = pointsOf(roof);
                            if (after.Count > 0)
                            {
                                hereAfter = after.Max(p => p.DotProduct(toward));
                                backAfter = after.Max(p => p.DotProduct(toward.Negate()));
                            }
                            now = which.AsDouble();
                            if (wasSet && near(hereAfter, hereBefore + distance) && near(backAfter, backBefore)
                                && t.Commit() == TransactionStatus.Committed)
                            {
                                movedWhat = string.Format("{0}'s {1} is {2} mm (was {3} mm), and its end "
                                    + "moved {4} mm toward {5}", labelOf(roof), name, mm(now), mm(was),
                                    mm(hereAfter - hereBefore), labelOf(other));
                            }
                            else if (t.GetStatus() == TransactionStatus.Started)
                            {
                                t.RollBack();
                            }
                        }
                        if (movedWhat != null) { refused = null; break; }
                        refused = string.Format("Revit did not move {0}'s end by changing its {1}: read "
                            + "back, the end moved {2} mm and its other end {3} mm, where {4} mm and 0 "
                            + "were asked. It was rolled back. Nothing was changed{5}", labelOf(roof), name,
                            double.IsNaN(hereAfter) ? "?" : mm(hereAfter - hereBefore),
                            double.IsNaN(backAfter) ? "?" : mm(backAfter - backBefore), mm(distance),
                            revitSaid());
                    }
                }
            }
#if !REVIT2020 && !REVIT2021
            else
            {
                // ---- A ROOF BY FOOTPRINT: its gable-end line, in sketch edit mode --
                var footprint = (FootPrintRoof)roof;
                var line = (Line)edge.GeometryCurve;
                var shift = toward.Multiply(distance);
                var p0 = line.GetEndPoint(0);
                var p1 = line.GetEndPoint(1);
                var beside = new List<Tuple<ModelCurve, int>>();
                foreach (ModelCurveArray loop in footprint.GetProfiles())
                    foreach (ModelCurve curve in loop)
                    {
                        if (curve.Id == edge.Id) continue;
                        var c = curve.GeometryCurve;
                        for (var i = 0; i < 2; i++)
                            if (c.GetEndPoint(i).DistanceTo(p0) <= oneMillimetre
                                || c.GetEndPoint(i).DistanceTo(p1) <= oneMillimetre)
                                beside.Add(Tuple.Create(curve, i));
                    }
                var square = beside.Count == 2 && beside.All(b => b.Item1.GeometryCurve is Line
                    && Math.Abs(((Line)b.Item1.GeometryCurve).Direction.DotProduct(toward)) >= 0.9999);
                Sketch sketch = null;
                foreach (var id in footprint.GetDependentElements(new ElementClassFilter(typeof(Sketch))))
                {
                    sketch = doc.GetElement(id) as Sketch;
                    if (sketch != null) break;
                }
                if (!square)
                {
                    refused = string.Format("{0}'s end line {1} does not have two straight lines beside it "
                        + "running the way it would move, so moving it would change the roof's shape, not "
                        + "only its length. Nothing was changed - join it by hand: Modify > Join/Unjoin "
                        + "Roof", labelOf(roof), edge.Id);
                }
                else if (sketch == null)
                {
                    refused = string.Format("Revit gave no sketch for {0}, so its footprint could not be "
                        + "edited. Nothing was changed - join it by hand: Modify > Join/Unjoin Roof",
                        labelOf(roof));
                }
                else
                {
                    var slopesBefore = slopesOf(footprint);
                    string failed = null;
                    using (var scope = new SketchEditScope(doc, "Heron: extend the roof by footprint"))
                    {
                        if (!scope.IsPermitted)
                            failed = "Revit will not open the roof's sketch for editing at this moment - "
                                + "another edit mode is active, or a transaction is open around this "
                                + "fragment (D-112)";
                        else if (!scope.IsSketchEditingSupported(sketch.Id))
                            failed = "Revit says this roof's sketch cannot be edited from an add-in";
                        else
                        {
                            try
                            {
                                scope.Start(sketch.Id);
                                using (var draw = new Transaction(doc, "Heron: move the roof's end line"))
                                {
                                    draw.Start();
                                    discipline(draw);
                                    try
                                    {
                                        ((CurveElement)doc.GetElement(edge.Id)).SetGeometryCurve(
                                            Line.CreateBound(p0 + shift, p1 + shift), true);
                                        foreach (var b in beside)
                                        {
                                            var c = (Line)b.Item1.GeometryCurve;
                                            var fixedEnd = c.GetEndPoint(1 - b.Item2);
                                            var movedEnd = c.GetEndPoint(b.Item2) + shift;
                                            ((CurveElement)doc.GetElement(b.Item1.Id)).SetGeometryCurve(
                                                b.Item2 == 1 ? Line.CreateBound(fixedEnd, movedEnd)
                                                             : Line.CreateBound(movedEnd, fixedEnd), true);
                                        }
                                    }
                                    catch (Exception notMoved)
                                    {
                                        failed = "Revit refused the moved line - " + notMoved.Message;
                                    }
                                    if (failed != null) draw.RollBack();
                                    else if (draw.Commit() != TransactionStatus.Committed)
                                        failed = "Revit would not keep the moved line" + revitSaid();
                                }
                                if (failed == null) scope.Commit(editScopeFailures);
                            }
                            catch (Exception notEdited)
                            {
                                if (failed == null)
                                    failed = "Revit would not finish the sketch - " + notEdited.Message
                                        + revitSaid();
                            }
                            if (scope.IsActive) scope.Cancel();
                        }
                    }
                    if (failed != null)
                    {
                        refused = string.Format("{0}: {1}. Nothing was changed - join it by hand: Modify > "
                            + "Join/Unjoin Roof", labelOf(roof), failed);
                    }
                    else
                    {
                        // READ BACK - the end moved by the distance, the other end not
                        // at all, and every line's slope as it was. Anything else is
                        // THROWN, so the group rolls the whole call back.
                        var again = doc.GetElement(roof.Id) as FootPrintRoof;
                        var after = again == null ? new List<XYZ>() : pointsOf(again);
                        var hereAfter = after.Count == 0 ? double.NaN : after.Max(p => p.DotProduct(toward));
                        var backAfter = after.Count == 0 ? double.NaN : after.Max(p => p.DotProduct(toward.Negate()));
                        var slopesAfter = again == null ? new Dictionary<string, string>() : slopesOf(again);
                        var slopesKept = slopesBefore.Count == slopesAfter.Count
                            && slopesBefore.All(kv => slopesAfter.ContainsKey(kv.Key) && slopesAfter[kv.Key] == kv.Value);
                        if (!near(hereAfter, hereBefore + distance) || !near(backAfter, backBefore) || !slopesKept)
                            throw new InvalidOperationException(string.Format("{0}'s sketch was edited, and "
                                + "read back the end moved {1} mm and its other end {2} mm, where {3} mm and 0 "
                                + "were asked{4} - so NOTHING from this call was kept", labelOf(roof),
                                double.IsNaN(hereAfter) ? "?" : mm(hereAfter - hereBefore),
                                double.IsNaN(backAfter) ? "?" : mm(backAfter - backBefore), mm(distance),
                                slopesKept ? "" : ", and a footprint line's Defines Slope or Slope changed"));
                        movedWhat = string.Format("{0}'s end line {1} moved {2} mm out, with the two lines "
                            + "beside it lengthened to meet it, and its end moved {3} mm toward {4}; every "
                            + "footprint line kept its slope", labelOf(roof), edge.Id, mm(distance),
                            mm(hereAfter - hereBefore), labelOf(other));
                    }
                }
            }
#endif

            if (movedWhat != null)
            {
                extended = 1;
                extendedRoof = roof.Id;
                refused = null;
                lines.Add("EXTENDED: " + movedWhat + " - read back off the roof's geometry, its other end "
                    + "where it was");
                if (chosen.Item5 - chosen.Item4 > oneMillimetre)
                    findings.Add(string.Format("The end is flat and went to its FARTHEST point, so "
                        + "elsewhere it goes up to {0} mm further than it needed: through {1}, and below "
                        + "the ridge on into the space under it. JOIN_GEOMETRY on the two roofs cleans "
                        + "where they overlap - offered, not run - and the part under the other roof "
                        + "stays: look at it in a section. Revit's own Join/Unjoin Roof cuts the end to "
                        + "the face instead", mm(chosen.Item5 - chosen.Item4), labelOf(other)));
            }
        }
    }
}

report = string.Join(" | ", lines);
if (extended == 1)
{
    findings.Insert(0, "1 roof extended to meet the other - " + lines.Last());
    findings.Add(string.Format("Revit {0}'s API has no Join/Unjoin Roof, so this moved the roof's end "
        + "instead, by the distance its rays measured. It is not a join: if the other roof moves, this "
        + "end does not follow it", release));
}
else
{
    findings.Insert(0, "Nothing was changed - see refused for why");
    findings.Add(string.Format("Revit {0}'s API has no Join/Unjoin Roof; this tool extends a roof by "
        + "extrusion's end{1} to meet another roof's upper face, measured by rays, and nothing else",
        release, footprintsEditable ? ", or a roof by footprint's gable end," : ""));
}
