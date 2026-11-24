// NOT STANDALONE. Assumes `doc`, `view3d` and `elements` are in scope; leaves
// `meets`, `gapMm`, `report` and `findings` behind. Reads only - no
// transaction, nothing changed.
//
// ===========================================================================
// DOES ONE ROOF REACH THE OTHER - MEASURED WITH RAYS, THE WAY THE EXTEND IS.
// ===========================================================================
//
// FRAGMENT-ISSUES row 5b-1953, issue #1187: "does this roof reach the other
// roof" reached EXTEND_ROOF_TO_ROOF, a write, on every backend, and no read
// answered it. MEASURE_DISTANCE measures between BOUNDING BOXES, and a main
// roof's box takes in the whole space under its slopes - so a wing roof that
// stops two metres short of the main roof's face still sits inside that box,
// and the answer would be 0 mm, touching. That is the plausible zero (D-52),
// so this card measures what the question means instead.
//
// THE MEASURE IS EXTEND_ROOF_TO_ROOF's, with nothing changed. Rays are cast
// from the corners and edge points of each END of each roof toward the other
// roof alone (ReferenceIntersector given that roof's id), in the 3D view
// given; a hit counts only on a face that looks up and back at the ray (its
// normal's Z above 0.05). An end is:
//
//   ROOF BY EXTRUSION  either end along its profile plane's normal.
//   ROOF BY FOOTPRINT  a gable end - a straight footprint line that defines no
//                      slope and is the outermost of its loop the way it faces.
//                      Read with GetProfiles and get_DefinesSlope, present
//                      2020 to 2027, so measured on every release - the EXTEND
//                      can MOVE such an end only from 2022, and this reads it.
//
// The farthest hit of an end is how far it is from the other roof: the end is
// flat, so its nearest point can touch while its farthest is still short.
// Both are said; `gapMm` is the farthest, of the end nearest the other roof.
//
// TWO ROOFS THAT ALREADY OVERLAP ARE FOUND FIRST, by Revit's own Boolean of
// their solids. An end already through the other roof's face has its points
// under that face, and a ray from there meets no upper face: by rays alone it
// would read as "no end faces the other roof". A Boolean Revit refuses (its
// InvalidOperationException) is counted and said, never read as "no overlap".
//
// NO HIT IS NOT "THEY DO NOT MEET". It means no end this measures faces the
// other roof's upper face in that view. Two roofs can still meet at an eave,
// a hip or a valley, which no end ray sees - so that answer says so, and
// never says the roofs are apart.
//
// THE RAYS SEE ONLY WHAT THE 3D VIEW SHOWS (MOVE_TO_RAY_HIT learned it): a
// roof hidden, or cut away by a section box, is met by no ray.

const double MillimetresPerFoot = 304.8;
const double CubicMetresPerCubicFoot = 0.028316846592;
var oneMillimetre = 1.0 / MillimetresPerFoot;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot).ToString("0", invariant);

var meets = 0;
var gapMm = double.NaN;
var report = "";
var findings = new List<string>();

var lines = new List<string>();
Func<Element, string> labelOf = e => string.Format("{0} {1} '{2}'",
    e is ExtrusionRoof ? "roof by extrusion" : (e is FootPrintRoof ? "roof by footprint" : "roof"),
    e.Id, e.Name);

// EVERY SOLID OF A ROOF, as Revit holds it now.
Func<Element, List<Solid>> solidsOf = e =>
{
    var found = new List<Solid>();
    Action<GeometryElement> collect = null;
    collect = geometry =>
    {
        if (geometry == null) return;
        foreach (GeometryObject piece in geometry)
        {
            var solid = piece as Solid;
            if (solid != null && solid.Volume > 0) { found.Add(solid); continue; }
            var instance = piece as GeometryInstance;
            if (instance != null) collect(instance.GetInstanceGeometry());
        }
    };
    collect(e.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine }));
    return found;
};

// EVERY CORNER AND EDGE POINT OF A SET OF SOLIDS.
Func<List<Solid>, List<XYZ>> pointsOf = solids =>
{
    var found = new List<XYZ>();
    foreach (var solid in solids)
        foreach (Edge edge in solid.Edges)
            foreach (var point in edge.Tessellate())
                found.Add(point);
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

var roofs = new List<RoofBase>();
foreach (var element in elements ?? new List<Element>())
{
    var roof = element as RoofBase;
    if (roof != null) roofs.Add(roof);
    else if (element != null)
        findings.Add(string.Format("{0} {1} is not a roof by extrusion or by footprint - an in-place "
            + "roof, or not a roof - and was left out",
            element.Category == null ? "element" : element.Category.Name, element.Id));
}

if (roofs.Count != 2)
{
    findings.Insert(0, string.Format("Nothing was measured: this asks whether one roof reaches ANOTHER, "
        + "so it takes exactly TWO roofs, and {0} were selected{1}", roofs.Count,
        roofs.Count == 0 ? "" : " - " + string.Join(", ", roofs.Select(r => labelOf(r)))));
}
else if (view3d == null || view3d.IsTemplate)
{
    findings.Insert(0, "Nothing was measured: a 3D view to measure in is needed - one that shows both "
        + "roofs, with no section box cutting them - and " + (view3d == null ? "none was given"
        : "the view given is a template"));
}
else
{
    var solids = roofs.ToDictionary(r => r.Id, r => solidsOf(r));

    // ---- 1. DO THEY ALREADY OVERLAP? Revit's own Boolean, piece by piece. ----
    double shared = 0.0;
    int refusedBooleans = 0;
    foreach (var mine in solids[roofs[0].Id])
        foreach (var theirs in solids[roofs[1].Id])
        {
            try
            {
                var part = BooleanOperationsUtils.ExecuteBooleanOperation(mine, theirs,
                    BooleanOperationsType.Intersect);
                if (part != null && part.Volume > 0) shared += part.Volume;
            }
            catch (Exception refusedByRevit) when (refusedByRevit.GetType().Name == "InvalidOperationException")
            {
                refusedBooleans++;
            }
        }
    var sharedM3 = shared * CubicMetresPerCubicFoot;
    var overlapping = sharedM3 > 1e-6;
    if (refusedBooleans > 0)
        findings.Add(string.Format("Revit could not intersect {0} pair(s) of the two roofs' solids, so "
            + "whether they overlap there was not checked - not the same as no overlap", refusedBooleans));
    if (overlapping)
        lines.Add(string.Format(invariant, "{0} and {1} OVERLAP - {2:0.###} m3 of one is inside the other",
            labelOf(roofs[0]), labelOf(roofs[1]), sharedM3));

    // ---- 2. EACH END, ITS RAYS AIMED AT THE OTHER ROOF ALONE ----------------
    var tries = new List<Tuple<RoofBase, RoofBase, double, double, int, int>>();
    Action<RoofBase, RoofBase, XYZ, ModelCurve, List<XYZ>> measure = (from, other, toward, edge, points) =>
    {
        var intersector = new ReferenceIntersector(other.Id, FindReferenceTarget.Face, view3d);
        var end = endOf(points, toward);
        int hit = 0, miss = 0;
        double nearest = double.MaxValue, farthest = 0.0;
        foreach (var origin in end)
        {
            var hits = intersector.Find(origin, toward);
            var best = double.MaxValue;
            if (hits != null)
                foreach (var h in hits)
                    if (h.Proximity < best && onUpperFace(other, h, origin, toward))
                        best = h.Proximity;
            if (best == double.MaxValue) { miss++; continue; }
            hit++;
            nearest = Math.Min(nearest, best);
            farthest = Math.Max(farthest, best);
        }
        if (hit == 0) return;
        lines.Add(string.Format(invariant, "{0}, its end toward ({1:0.###}, {2:0.###}, {3:0.###}){4}: "
            + "{5} of {6} point(s) of its end face the upper face of {7}, nearest {8} mm, farthest {9} mm "
            + "away{10}", labelOf(from), toward.X, toward.Y, toward.Z,
            edge == null ? "" : " (footprint line " + edge.Id + ")", hit, end.Count, labelOf(other),
            mm(nearest), mm(farthest), miss == 0 ? "" : " - " + miss + " point(s) pass beside or under it"));
        tries.Add(Tuple.Create(from, other, nearest, farthest, hit, miss));
    };
    foreach (var roof in roofs)
    {
        var other = roofs.First(r => r.Id != roof.Id);
        var points = pointsOf(solids[roof.Id]);
        var asExtrusion = roof as ExtrusionRoof;
        var asFootprint = roof as FootPrintRoof;
        if (asExtrusion != null)
        {
            ModelCurve firstEdge = null;
            foreach (ModelCurve curve in asExtrusion.GetProfile()) { firstEdge = curve; break; }
            if (firstEdge == null || firstEdge.SketchPlane == null)
            {
                findings.Add(string.Format("{0}: Revit gave no profile to read its direction from, so its "
                    + "ends were not measured", labelOf(roof)));
                continue;
            }
            var normal = firstEdge.SketchPlane.GetPlane().Normal.Normalize();
            foreach (var sign in new[] { 1.0, -1.0 })
                measure(roof, other, normal.Multiply(sign), null, points);
        }
        else if (asFootprint != null)
        {
            // A GABLE END: a straight footprint line that defines no slope, and
            // is the outermost line of its loop in the direction it faces.
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

    // ---- 3. THE ANSWER ---------------------------------------------------------
    var closest = tries.OrderBy(t => t.Item4).FirstOrDefault();
    if (overlapping)
    {
        meets = 1;
        gapMm = 0.0;
        findings.Insert(0, string.Format("YES - {0} and {1} already overlap, so one reaches the other "
            + "and goes into it. Nothing was changed", labelOf(roofs[0]), labelOf(roofs[1])));
    }
    else if (closest == null)
    {
        findings.Insert(0, string.Format("NOT MEASURED - no end of either roof faces the upper face of the "
            + "other in the 3D view '{0}' (a roof by extrusion is measured from its two ends, a roof by "
            + "footprint from its gable ends - lines that define no slope). That is not the same as the "
            + "roofs being apart: they can still meet at an eave, a hip or a valley, which no end ray "
            + "sees. Is the other roof showing in that view - not hidden, not cut away by a section box? "
            + "Nothing was changed", view3d.Name));
    }
    else
    {
        gapMm = closest.Item4 * MillimetresPerFoot;
        if (closest.Item4 <= oneMillimetre)
        {
            meets = 1;
            findings.Insert(0, string.Format("YES - the end of {0} meets the upper face of {1}: its "
                + "farthest point is {2} mm from it. Nothing was changed", labelOf(closest.Item1),
                labelOf(closest.Item2), mm(closest.Item4)));
        }
        else
        {
            findings.Insert(0, string.Format("NO - the end of {0} stops short of {1}: its nearest point is "
                + "{2} mm and its farthest {3} mm from the other roof's upper face, measured with rays. "
                + "EXTEND_ROOF_TO_ROOF lengthens it to meet, offered and asked first. Nothing was changed",
                labelOf(closest.Item1), labelOf(closest.Item2), mm(closest.Item3), mm(closest.Item4)));
        }
        if (closest.Item6 > 0)
            findings.Add(string.Format("{0} of the {1} point(s) of that end met no upper face of {2}: those "
                + "parts pass beside or under it, and would not reach it however far the end went",
                closest.Item6, closest.Item5 + closest.Item6, labelOf(closest.Item2)));
        if (tries.Count > 1)
            findings.Add(string.Format("{0} ends face the other roof; the answer is the nearest - every "
                + "one is in report", tries.Count));
    }
}

report = string.Join(" | ", lines);
