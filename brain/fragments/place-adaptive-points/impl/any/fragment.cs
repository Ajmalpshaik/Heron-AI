// NOT STANDALONE. Assumes `doc`, `points` and `pointKind` are in scope; leaves
// `pointIds`, `placedPoints`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE POINTS AN ADAPTIVE COMPONENT IS PLACED BY - the Family Editor's Point
// tool, then Make Adaptive. A modeller places an adaptive family by clicking
// its PLACEMENT points in their number order, and its shape follows wherever
// they land: a four-point panel, a two-point beam, a three-point bracket. A
// SHAPE HANDLE point moves the shape after placement and is not clicked; a
// plain REFERENCE point is a point to build on that nobody clicks.
//
// THREE WAYS TO SAY WHERE, semicolons between, made in the order given:
//   "X,Y,Z"                    FREE, in millimetres from the family origin;
//   "on <line id> at 0.25"     HOSTED ON A LINE, that far along it from its
//                              start (0 to 1, Revit's normalised curve
//                              parameter) - it slides with the line, which is
//                              what makes an inset frame or a mid-point;
//   "on point 1 yz 0,150"      HOSTED ON A POINT'S OWN PLANE - xy, yz or xz -
//                              at millimetres along that plane's two axes, so
//                              a profile drawn through such points turns with
//                              the point: a two-point beam's section.
// A hosted point is a REFERENCE point and follows its host; a placement or
// shape handle point is a free one, so hosted points and those kinds are not
// mixed in one call.
//
// VERSION 2 (2026-11-22) - PUSHED OFF A POINT'S PLANE, Revit's Offset:
//   "on point 7 normal 300"            off the plane of point 7 that lies in
//                                      the panel the placement points span,
//                                      300 mm along the panel's normal - the
//                                      folded panel's TIP over a centroid
//                                      point, so lines from the corners to it
//                                      make a pyramid facet;
//   "on point 1 xy 0,0 offset 300"     a plane named, then pushed off it.
// The plane that faces the panel is FOUND, never assumed - within half a
// degree, or refused naming each plane's angle. Offset is made to read the
// number typed, so a family parameter linked to it (LINK_FAMILY_PARAMETER)
// holds the same number; the answer says which side of the panel that is.
//
// ONLY IN A CONCEPTUAL FAMILY - an adaptive component, a pattern-based panel
// or a mass. Revit makes reference points nowhere else, and says so; that is
// read before anything is made, and refused by name.
//
// ALL OR NOTHING: every point's position, kind and number are read again -
// placement numbers must rise in the order given; one that does not read as
// asked fails the call, and the host rolls it back.

var findings = new List<string>();
var pointIds = "";
var placedPoints = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<string, double?> number = text =>
{
    double value;
    if (!double.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, invariant, out value))
        return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

var problems = new List<string>();
// Each point asked for: the words as given, where it should land, and how it is
// hosted - null for a free point.
var wanted = new List<Tuple<string, XYZ, Func<PointElementReference>>>();
// VERSION 2: a point pushed off its host's plane, by its place in `wanted` -
// the distance in feet, the point on the plane it is pushed from, the
// direction it was asked along, the plane's word, the host as named, and the
// points the panel's normal came from ("" for a plane named).
var offsetOf = new Dictionary<int, Tuple<double, XYZ, XYZ, string, string, string>>();
var offsetRows = new List<string>();
AdaptivePointType? kind = null;
var kindWords = "";
var placementBefore = -1;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "Adaptive points are placed inside an adaptive or pattern-based family - open it first.";
}
else
{
    var family = doc.OwnerFamily;
    var conceptual = false;
    var adaptive = false;
    var panel = false;
    try { conceptual = family != null && family.IsConceptualMassFamily; } catch (Exception) { }
    try { adaptive = family != null && AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(family); } catch (Exception) { }
    // A curtain-panel family counts only WITH reference points - the pattern-based
    // kind; a classic curtain wall panel holds none (the rule REPORT_FAMILY_TEMPLATE
    // reads, since the flag alone is not trusted to tell the two apart).
    try
    {
        panel = family != null && family.IsCurtainPanelFamily
            && new FilteredElementCollector(doc).OfClass(typeof(ReferencePoint)).GetElementCount() > 0;
    }
    catch (Exception) { }
    if (!conceptual && !adaptive && !panel)
        problems.Add("This family is not an adaptive component, a pattern-based panel or a mass - Revit places "
            + "reference points only in those. Start one from the Generic Model Adaptive or a pattern-based "
            + "template (REPORT_FAMILY_TEMPLATE says which this is).");
    if (adaptive)
        try { placementBefore = AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(family); } catch (Exception) { }

    var said = squash(pointKind);
    if (said == "placement" || said == "placementpoint" || said == "adaptive" || said == "adaptivepoint")
    { kind = AdaptivePointType.PlacementPoint; kindWords = "placement point"; }
    else if (said == "shapehandle" || said == "shapehandlepoint" || said == "handle")
    { kind = AdaptivePointType.ShapeHandlePoint; kindWords = "shape handle point"; }
    else if (said == "reference" || said == "referencepoint" || said == "plain")
    { kind = AdaptivePointType.ReferencePoint; kindWords = "reference point"; }
    else problems.Add("\"" + (pointKind ?? "") + "\" is not a kind of point - placement, shape handle or reference.");
    // A placement or shape handle point is an ADAPTIVE point, and Revit's
    // remark on MakeAdaptivePoint refuses one outside an adaptive family - so
    // a mass, or a panel Revit does not read as adaptive, takes reference
    // points only, refused here rather than half-made and rolled back.
    if (kind.HasValue && kind.Value != AdaptivePointType.ReferencePoint && (conceptual || panel) && !adaptive)
        problems.Add("A " + kindWords + " is an adaptive point, and Revit makes those only in an adaptive family - "
            + "this " + (conceptual ? "mass" : "panel") + " takes reference points only (pointKind \"reference\"). "
            + "Adaptive points are placed in a family started from Generic Model Adaptive.");

    // The family's points as they stand: by placement number and by id.
    var existing = new FilteredElementCollector(doc).OfClass(typeof(ReferencePoint)).Cast<ReferencePoint>().ToList();
    Func<string, ReferencePoint> pointNamed = token =>
    {
        int n;
        if (int.TryParse(token, out n))
            foreach (var p in existing)
            {
                var placementNumber = -1;
                try
                {
                    if (AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, p.Id))
                        placementNumber = AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, p.Id);
                }
                catch (Exception) { }
                if (placementNumber == n) return p;
            }
        return doc.GetElement(token) as ReferencePoint;
    };

    var items = (points ?? "").Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    if (items.Count == 0)
        problems.Add("No points were given - \"0,0,0; 1000,0,0\" in millimetres, \"on <line id> at 0.5\", or \"on point 1 "
            + "yz 0,150\", semicolons between.");
    foreach (var item in items)
    {
        var words = System.Text.RegularExpressions.Regex.Replace(item, @"\s*,\s*", ",")
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (words.Count > 0 && words[0].ToLowerInvariant() == "on")
        {
            // VERSION 2 - PUSHED OFF A POINT'S PLANE (Revit's Offset):
            //   on point <number or id> normal <mm>
            //   on point <number or id> <xy|yz|xz> <u>,<v> offset <mm>
            if ((words.Count == 5 && words[1].ToLowerInvariant() == "point" && words[3].ToLowerInvariant() == "normal")
                || (words.Count == 7 && words[1].ToLowerInvariant() == "point" && words[5].ToLowerInvariant() == "offset"))
            {
                var byNormal = words.Count == 5;
                var host = pointNamed(words[2]);
                var depth = number(words[byNormal ? 4 : 6]);
                var plane = byNormal ? "" : words[3].ToLowerInvariant();
                double? u = 0, v = 0;
                if (!byNormal)
                {
                    var uv = words[4].Split(',');
                    u = uv.Length == 2 ? number(uv[0]) : null;
                    v = uv.Length == 2 ? number(uv[1]) : null;
                }
                if (host == null)
                    problems.Add("\"" + item + "\": no point " + words[2] + " - a placement number or a point's id.");
                else if (!depth.HasValue || depth.Value == 0)
                    problems.Add("\"" + item + "\" does not end with how far off the plane, in millimetres and not 0 - "
                        + "\"300\", or \"-300\" for the other side.");
                else if (!byNormal && plane != "xy" && plane != "yz" && plane != "xz")
                    problems.Add("\"" + item + "\": \"" + words[3] + "\" is not one of a point's planes - xy, yz or xz.");
                else if (!u.HasValue || !v.HasValue)
                    problems.Add("\"" + item + "\" does not say where on the plane, \"U,V\" in millimetres, before offset.");
                else
                {
                    var frame = host.GetCoordinateSystem();
                    var toward = "";
                    if (byNormal)
                    {
                        // THE PANEL'S NORMAL, from the placement points in their
                        // number order - or, with fewer than three, from the
                        // family's points that are not hosted on anything (a
                        // pattern-based template's grid points, NEEDS-CHECKING
                        // BV4). They must lie in one plane.
                        var loop = existing
                            .Select(p =>
                            {
                                var n = -1;
                                try
                                {
                                    if (AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, p.Id))
                                        n = AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, p.Id);
                                }
                                catch (Exception) { }
                                return Tuple.Create(p, n);
                            })
                            .Where(t => t.Item2 > 0).OrderBy(t => t.Item2).ToList();
                        var loopWords = "placement points " + string.Join(", ", loop.Select(t => t.Item2));
                        if (loop.Count < 3)
                        {
                            loop = existing.Where(p =>
                                {
                                    try { return p.GetPointElementReference() == null; }
                                    catch (Exception) { return false; }
                                })
                                .Select(p => Tuple.Create(p, -1)).ToList();
                            loopWords = "the family's " + loop.Count + " point(s) that are not hosted on anything";
                        }
                        var corners = loop.Where(t => t.Item1.Position != null).Select(t => t.Item1.Position).ToList();
                        XYZ normal = null;
                        if (corners.Count >= 3)
                        {
                            // Three corners as far apart as they go, so a near
                            // straight run of points is not mistaken for a plane.
                            var a = corners[0];
                            var b = corners.OrderByDescending(c => c.DistanceTo(a)).First();
                            var c3 = corners.OrderByDescending(c => (b - a).CrossProduct(c - a).GetLength()).First();
                            var cross = (b - a).CrossProduct(c3 - a);
                            if (cross.GetLength() > 1e-9)
                            {
                                normal = cross.Normalize();
                                // Signed by the points' own order (Newell), so
                                // "anticlockwise" means what it says.
                                var newell = XYZ.Zero;
                                for (var i = 0; i < corners.Count; i++)
                                    newell = newell + corners[i].CrossProduct(corners[(i + 1) % corners.Count]);
                                if (newell.DotProduct(normal) < 0) normal = normal.Negate();
                            }
                        }
                        if (normal == null)
                            problems.Add("\"" + item + "\": " + loopWords + " do not span a panel - at least three points not "
                                + "in one straight line are needed to say which way its normal goes.");
                        else if (corners.Any(c => Math.Abs((c - corners[0]).DotProduct(normal)) > 2 * halfMillimetre))
                            problems.Add("\"" + item + "\": " + loopWords + " are not in one plane, so the panel has no one "
                                + "normal - push the point off a plane named instead, \"on point " + words[2]
                                + " xy 0,0 offset " + words[4] + "\".");
                        else
                        {
                            // THE HOST'S PLANE THAT FACES THE PANEL - never
                            // assumed: a point on a line takes its planes from
                            // the line, and which of them lies in the panel is
                            // Revit's to say. Within half a degree, or refused.
                            var axes = new[]
                            {
                                Tuple.Create("xy", frame.BasisZ),
                                Tuple.Create("yz", frame.BasisX),
                                Tuple.Create("xz", frame.BasisY)
                            };
                            var best = axes.OrderByDescending(t => Math.Abs(t.Item2.DotProduct(normal))).First();
                            if (Math.Abs(best.Item2.DotProduct(normal)) < Math.Cos(0.5 * Math.PI / 180))
                                problems.Add("\"" + item + "\": none of point " + words[2] + "'s planes lies in the panel " + loopWords
                                    + " span - their normals are " + string.Join(", ", axes.Select(t => t.Item1 + " "
                                        + Math.Round(Math.Acos(Math.Min(1, Math.Abs(t.Item2.DotProduct(normal)))) * 180 / Math.PI, 1)
                                            .ToString(invariant) + " degrees")) + " off the panel's normal. Nothing was placed.");
                            else
                            {
                                plane = best.Item1;
                                // The side is named only from placement points
                                // in their number order; the fallback points
                                // come in no order, so no side is claimed.
                                toward = loop.Count > 0 && loop[0].Item2 > 0 ? loopWords : "";
                                // Pushed along the PLANE's own normal - the
                                // way Revit measures Offset, so a flip below
                                // lands exactly where the read-back expects -
                                // signed toward the panel's normal.
                                var along = best.Item2.DotProduct(normal) >= 0 ? best.Item2 : best.Item2.Negate();
                                var at = frame.Origin + along.Multiply(depth.Value / 304.8);
                                var hostPoint = host;
                                var planeWord = plane;
                                var d = depth.Value / 304.8;
                                offsetOf[wanted.Count] = Tuple.Create(d, frame.Origin, along, planeWord, words[2], toward);
                                wanted.Add(Tuple.Create(item, at, (Func<PointElementReference>)(() =>
                                {
                                    var reference = planeWord == "xy" ? hostPoint.GetCoordinatePlaneReferenceXY()
                                        : planeWord == "yz" ? hostPoint.GetCoordinatePlaneReferenceYZ()
                                        : hostPoint.GetCoordinatePlaneReferenceXZ();
                                    return PointOnPlane.NewPointOnPlane(doc, reference, at, XYZ.BasisX);
                                })));
                            }
                        }
                    }
                    else
                    {
                        var first = plane == "yz" ? frame.BasisY : frame.BasisX;
                        var second = plane == "xy" ? frame.BasisY : frame.BasisZ;
                        var across = first.CrossProduct(second).Normalize();
                        var onPlane = frame.Origin + first.Multiply(u.Value / 304.8) + second.Multiply(v.Value / 304.8);
                        var at = onPlane + across.Multiply(depth.Value / 304.8);
                        var hostPoint = host;
                        var planeWord = plane;
                        offsetOf[wanted.Count] = Tuple.Create(depth.Value / 304.8, onPlane, across, planeWord, words[2], "");
                        wanted.Add(Tuple.Create(item, at, (Func<PointElementReference>)(() =>
                        {
                            var reference = planeWord == "xy" ? hostPoint.GetCoordinatePlaneReferenceXY()
                                : planeWord == "yz" ? hostPoint.GetCoordinatePlaneReferenceYZ()
                                : hostPoint.GetCoordinatePlaneReferenceXZ();
                            return PointOnPlane.NewPointOnPlane(doc, reference, at, XYZ.BasisX);
                        })));
                    }
                }
                continue;
            }
            // HOSTED ON A POINT'S PLANE: on point <number or id> <xy|yz|xz> <u>,<v>
            if (words.Count == 5 && words[1].ToLowerInvariant() == "point")
            {
                var host = pointNamed(words[2]);
                var plane = words[3].ToLowerInvariant();
                var uv = words[4].Split(',');
                var u = uv.Length == 2 ? number(uv[0]) : null;
                var v = uv.Length == 2 ? number(uv[1]) : null;
                if (host == null)
                    problems.Add("\"" + item + "\": no point " + words[2] + " - a placement number or a point's id.");
                else if (plane != "xy" && plane != "yz" && plane != "xz")
                    problems.Add("\"" + item + "\": \"" + words[3] + "\" is not one of a point's planes - xy, yz or xz.");
                else if (!u.HasValue || !v.HasValue)
                    problems.Add("\"" + item + "\" does not end with where on the plane, \"U,V\" in millimetres.");
                else
                {
                    var frame = host.GetCoordinateSystem();
                    var first = plane == "yz" ? frame.BasisY : frame.BasisX;
                    var second = plane == "xy" ? frame.BasisY : frame.BasisZ;
                    var at = frame.Origin + first.Multiply(u.Value / 304.8) + second.Multiply(v.Value / 304.8);
                    var hostPoint = host;
                    var planeWord = plane;
                    wanted.Add(Tuple.Create(item, at, (Func<PointElementReference>)(() =>
                    {
                        var reference = planeWord == "xy" ? hostPoint.GetCoordinatePlaneReferenceXY()
                            : planeWord == "yz" ? hostPoint.GetCoordinatePlaneReferenceYZ()
                            : hostPoint.GetCoordinatePlaneReferenceXZ();
                        return PointOnPlane.NewPointOnPlane(doc, reference, at, XYZ.BasisX);
                    })));
                }
                continue;
            }
            // HOSTED ON A LINE: on <line id> at <0 to 1>
            if (words.Count == 4 && words[2].ToLowerInvariant() == "at")
            {
                var line = doc.GetElement(words[1]) as CurveElement;
                var ratio = number(words[3]);
                var curve = line == null ? null : line.GeometryCurve;
                if (curve == null || curve.Reference == null)
                    problems.Add("\"" + item + "\": \"" + words[1] + "\" is not the id of a line in this family a point can "
                        + "be hosted on.");
                else if (!curve.IsBound)
                    problems.Add("\"" + item + "\": that line is a closed circle or ellipse, which has no start to measure "
                        + "from - host the point on a line with two ends.");
                else if (!ratio.HasValue || ratio < 0 || ratio > 1)
                    problems.Add("\"" + item + "\": how far along is a number from 0 (its start) to 1 (its end).");
                else
                {
                    var at = curve.Evaluate(ratio.Value, true);
                    var edge = curve.Reference;
                    var along = ratio.Value;
                    wanted.Add(Tuple.Create(item, at, (Func<PointElementReference>)(() =>
                        doc.Application.Create.NewPointOnEdge(edge, new PointLocationOnCurve(
                            PointOnCurveMeasurementType.NormalizedCurveParameter, along, PointOnCurveMeasureFrom.Beginning)))));
                }
                continue;
            }
            problems.Add("\"" + item + "\" is not a hosted point - \"on <line id> at 0.5\", \"on point 1 yz 0,150\", \"on point "
                + "7 normal 300\" or \"on point 1 xy 0,0 offset 300\".");
            continue;
        }

        var parts = item.Split(',');
        var x = parts.Length == 3 ? number(parts[0]) : null;
        var y = parts.Length == 3 ? number(parts[1]) : null;
        var z = parts.Length == 3 ? number(parts[2]) : null;
        if (!x.HasValue || !y.HasValue || !z.HasValue)
        {
            problems.Add("\"" + item + "\" is not a point \"X,Y,Z\" in millimetres, nor \"on ...\" a line or a point.");
            continue;
        }
        wanted.Add(Tuple.Create(item, new XYZ(x.Value / 304.8, y.Value / 304.8, z.Value / 304.8),
            (Func<PointElementReference>)null));
    }

    if (kind.HasValue && kind.Value != AdaptivePointType.ReferencePoint && wanted.Any(w => w.Item3 != null))
        problems.Add("A point hosted on a line or a plane is placed as a REFERENCE point and follows its host; a "
            + kindWords + " is a free one. Give pointKind reference for hosted points, and place the others in a "
            + "call of their own.");

    // Two points in one place cannot be told apart when clicked - against each
    // other AND against the points already in the family.
    for (var i = 0; i < wanted.Count; i++)
    {
        var at = wanted[i].Item2;
        if (wanted.Take(i).Any(w => w.Item2.DistanceTo(at) < halfMillimetre)
            || existing.Any(p => p.Position != null && p.Position.DistanceTo(at) < halfMillimetre))
            problems.Add("\"" + wanted[i].Item1 + "\" is where another point already is - two points in one place cannot "
                + "be told apart when clicked.");
    }

    if (problems.Count > 0) refused = "Nothing was placed. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// PLACE IN ORDER, THEN READ EVERY POINT BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var made = new List<Tuple<ReferencePoint, XYZ, string>>();
    foreach (var w in wanted)
    {
        ReferencePoint point;
        try
        {
            point = w.Item3 == null ? doc.FamilyCreate.NewReferencePoint(w.Item2)
                : doc.FamilyCreate.NewReferencePoint(w.Item3());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not place the point \"" + w.Item1 + "\": " + ex.Message
                + " The call failed, and Heron rolls the whole call back.");
        }
        if (point == null)
            throw new InvalidOperationException("Revit made no point for \"" + w.Item1 + "\". The call failed, and Heron "
                + "rolls the whole call back.");
        if (kind.Value != AdaptivePointType.ReferencePoint)
        {
            try { AdaptiveComponentFamilyUtils.MakeAdaptivePoint(doc, point.Id, kind.Value); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not make the point \"" + w.Item1 + "\" a " + kindWords
                    + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
            }
        }
        made.Add(Tuple.Create(point, w.Item2, w.Item1));
    }

    doc.Regenerate();

    // VERSION 2 - OFFSET READS WHAT WAS TYPED. Revit measures a point's Offset
    // along its host PLANE's normal, which may face either way along the
    // direction asked; where it reads the opposite of what was typed it is set
    // to what was typed, so a family parameter linked to Offset later holds the
    // same number. The point then sits on the other side, and the answer says
    // which side. Anything else it reads fails the call.
    if (offsetOf.Count > 0)
    {
        var flipped = new HashSet<int>();
        foreach (var entry in offsetOf)
        {
            var point = made[entry.Key].Item1;
            var asked = entry.Value.Item1;
            var field = point.get_Parameter(BuiltInParameter.POINT_ELEMENT_OFFSET);
            if (field == null || field.StorageType != StorageType.Double)
                throw new InvalidOperationException("The point \"" + made[entry.Key].Item3 + "\" has no Offset Revit lets "
                    + "be read. The call failed, and Heron rolls the whole call back.");
            var reads = field.AsDouble();
            if (Math.Abs(reads - asked) <= halfMillimetre) continue;
            if (Math.Abs(reads + asked) > halfMillimetre || field.IsReadOnly)
                throw new InvalidOperationException("The point \"" + made[entry.Key].Item3 + "\" reads Offset " + mm(reads)
                    + " mm, not the " + mm(asked) + " mm asked. The call failed, and Heron rolls the whole call back.");
            try { field.Set(asked); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not set the Offset of the point \"" + made[entry.Key].Item3
                    + "\" to " + mm(asked) + " mm: " + ex.Message + " The call failed, and Heron rolls the whole call back.");
            }
            flipped.Add(entry.Key);
        }
        if (flipped.Count > 0) doc.Regenerate();
        foreach (var entry in offsetOf)
        {
            var m = made[entry.Key];
            var spec = entry.Value;
            var side = flipped.Contains(entry.Key) ? -1.0 : 1.0;
            var expected = spec.Item2 + spec.Item3.Multiply(side * spec.Item1);
            made[entry.Key] = Tuple.Create(m.Item1, expected, m.Item3);
            var field = m.Item1.get_Parameter(BuiltInParameter.POINT_ELEMENT_OFFSET);
            var reads = field == null ? double.NaN : field.AsDouble();
            if (double.IsNaN(reads) || Math.Abs(reads - spec.Item1) > halfMillimetre)
                throw new InvalidOperationException("The point \"" + m.Item3 + "\" reads Offset "
                    + (double.IsNaN(reads) ? "nothing" : mm(reads) + " mm") + " after it was set to " + mm(spec.Item1)
                    + " mm. The call failed, and Heron rolls the whole call back.");
            // Which side, in the modeller's words: for the panel's normal, the
            // side from which the points run anticlockwise.
            var away = side * spec.Item1 > 0;
            offsetRows.Add(m.Item1.UniqueId + " is " + mm(Math.Abs(spec.Item1)) + " mm off point " + spec.Item5 + "'s "
                + spec.Item4 + " plane, Offset " + mm(spec.Item1) + " mm"
                + (spec.Item6.Length > 0
                    ? ", on the side from which " + spec.Item6 + " run " + (away ? "anticlockwise" : "clockwise")
                        + (flipped.Contains(entry.Key) ? " (Revit's Offset on this plane runs that way for a positive "
                            + "number - a negative one puts the point on the other side)" : "")
                    : (flipped.Contains(entry.Key) ? " - Revit's Offset on this plane runs the other way from the one "
                        + "asked, so the point sits on that side" : "")));
        }
    }

    var ids = new List<string>();
    var rows = new List<string>();
    var numbers = new List<int>();
    foreach (var m in made)
    {
        var at = m.Item1.Position;
        if (at == null || at.DistanceTo(m.Item2) > halfMillimetre)
            throw new InvalidOperationException("The point \"" + m.Item3 + "\" reads at " + (at == null ? "nowhere"
                : mm(at.X) + ", " + mm(at.Y) + ", " + mm(at.Z) + " mm") + ", not at " + mm(m.Item2.X) + ", " + mm(m.Item2.Y)
                + ", " + mm(m.Item2.Z) + " mm where it was asked. The call failed, and Heron rolls the whole call back.");
        var isPlacement = AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, m.Item1.Id);
        var isHandle = AdaptiveComponentFamilyUtils.IsAdaptiveShapeHandlePoint(doc, m.Item1.Id);
        var asKind = isPlacement ? AdaptivePointType.PlacementPoint : isHandle ? AdaptivePointType.ShapeHandlePoint
            : AdaptivePointType.ReferencePoint;
        if (asKind != kind.Value)
            throw new InvalidOperationException("The point \"" + m.Item3 + "\" reads as a different kind from the "
                + kindWords + " asked. The call failed, and Heron rolls the whole call back.");
        var label = kindWords;
        if (isPlacement)
        {
            var n = AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, m.Item1.Id);
            // THE NUMBERS RISE IN THE ORDER GIVEN - the order a modeller clicks.
            if (numbers.Count > 0 && n <= numbers[numbers.Count - 1])
                throw new InvalidOperationException("The point \"" + m.Item3 + "\" reads placement number " + n + ", after "
                    + numbers[numbers.Count - 1] + " for the point before it - not the order given. The call failed, and "
                    + "Heron rolls the whole call back.");
            numbers.Add(n);
            label += " " + n;
        }
        ids.Add(m.Item1.UniqueId);
        rows.Add(label + " " + m.Item1.UniqueId + " at " + mm(at.X) + ", " + mm(at.Y) + ", " + mm(at.Z) + " mm"
            + (m.Item3.StartsWith("on ", StringComparison.OrdinalIgnoreCase) ? " (" + m.Item3 + ")" : ""));
    }

    pointIds = string.Join(",", ids);
    placedPoints = string.Join("  ||  ", rows);
    var total = -1;
    try { total = AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(doc.OwnerFamily); } catch (Exception) { }
    findings.Add(made.Count + " " + kindWords + "(s) placed and read back."
        + (total >= 0 ? " The family now has " + total + " placement point(s) - a modeller clicks them in their number "
            + "order to place it." : ""));
    if (numbers.Count > 0 && placementBefore >= 0
        && !numbers.Select((n, i) => n == placementBefore + 1 + i).All(ok => ok))
        findings.Add("The new placement points read " + string.Join(", ", numbers) + " - not the next numbers after the "
            + placementBefore + " the family had. Revit numbered them; check the order before placing the family.");
    if (made.Any(m => m.Item3.StartsWith("on ", StringComparison.OrdinalIgnoreCase)))
        findings.Add("A hosted point follows its host: drag the line or the point it is on in the Family Editor and it "
            + "moves with it (NEEDS-CHECKING BV19).");
    // VERSION 2: each point pushed off a plane, its Offset and its side.
    foreach (var row in offsetRows) findings.Add("Pushed off: " + row + ".");
    if (offsetRows.Count > 0)
        findings.Add("Lines from the panel's corners to a point pushed off it are DRAW_FAMILY_POINT_CURVES, and a "
            + "folded facet on those lines CREATE_CONCEPTUAL_FORM. To drive the distance from a family length "
            + "parameter such as Tip Depth, link this point's Offset to it with LINK_FAMILY_PARAMETER. Whether the "
            + "point keeps its side on every cell of a divided surface is NEEDS-CHECKING Group PQ.");
    if (kind.Value == AdaptivePointType.PlacementPoint)
        findings.Add("Lines through these points are DRAW_FAMILY_POINT_CURVES, and a surface or solid on those lines "
            + "CREATE_CONCEPTUAL_FORM - geometry built on the points follows them when the family is placed.");
}

if (refused != null) findings.Add(refused);
