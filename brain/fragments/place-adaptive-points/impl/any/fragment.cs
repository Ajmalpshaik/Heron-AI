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
            problems.Add("\"" + item + "\" is not a hosted point - \"on <line id> at 0.5\" or \"on point 1 yz 0,150\".");
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
    if (kind.Value == AdaptivePointType.PlacementPoint)
        findings.Add("Lines through these points are DRAW_FAMILY_POINT_CURVES, and a surface or solid on those lines "
            + "CREATE_CONCEPTUAL_FORM - geometry built on the points follows them when the family is placed.");
}

if (refused != null) findings.Add(refused);
