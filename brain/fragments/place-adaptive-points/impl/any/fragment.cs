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
// ONLY IN A CONCEPTUAL FAMILY - an adaptive component, a pattern-based panel
// or a mass. Revit makes reference points nowhere else, and says so; that is
// read before anything is made, and refused by name.
//
// IN MILLIMETRES FROM THE FAMILY ORIGIN, "X,Y,Z" each, semicolons between, made
// in the order given - so placement points take the next numbers in that order,
// and the numbers Revit gave are READ BACK rather than assumed.
//
// ALL OR NOTHING: every point's position, kind and number are read again; one
// that does not read as asked fails the call, and the host rolls it back.

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
var wanted = new List<XYZ>();
AdaptivePointType? kind = null;
var kindWords = "";

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
    try { panel = family != null && family.IsCurtainPanelFamily; } catch (Exception) { }
    if (!conceptual && !adaptive && !panel)
        problems.Add("This family is not an adaptive component, a pattern-based panel or a mass - Revit places "
            + "reference points only in those. Start one from the Generic Model Adaptive or a pattern-based "
            + "template (REPORT_FAMILY_TEMPLATE says which this is).");

    var said = squash(pointKind);
    if (said == "placement" || said == "placementpoint" || said == "adaptive" || said == "adaptivepoint")
    { kind = AdaptivePointType.PlacementPoint; kindWords = "placement point"; }
    else if (said == "shapehandle" || said == "shapehandlepoint" || said == "handle")
    { kind = AdaptivePointType.ShapeHandlePoint; kindWords = "shape handle point"; }
    else if (said == "reference" || said == "referencepoint" || said == "plain")
    { kind = AdaptivePointType.ReferencePoint; kindWords = "reference point"; }
    else problems.Add("\"" + (pointKind ?? "") + "\" is not a kind of point - placement, shape handle or reference.");

    var items = (points ?? "").Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    if (items.Count == 0) problems.Add("No points were given - \"0,0,0; 1000,0,0\", millimetres, semicolons between.");
    foreach (var item in items)
    {
        var parts = item.Split(',');
        var x = parts.Length == 3 ? number(parts[0]) : null;
        var y = parts.Length == 3 ? number(parts[1]) : null;
        var z = parts.Length == 3 ? number(parts[2]) : null;
        if (!x.HasValue || !y.HasValue || !z.HasValue)
        {
            problems.Add("\"" + item + "\" is not a point \"X,Y,Z\" in millimetres.");
            continue;
        }
        var at = new XYZ(x.Value / 304.8, y.Value / 304.8, z.Value / 304.8);
        if (wanted.Any(w => w.DistanceTo(at) < halfMillimetre))
            problems.Add("\"" + item + "\" is where another point already is - two points in one place cannot be "
                + "told apart when clicked.");
        else wanted.Add(at);
    }

    if (problems.Count > 0) refused = "Nothing was placed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// PLACE IN ORDER, THEN READ EVERY POINT BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var made = new List<Tuple<ReferencePoint, XYZ>>();
    foreach (var at in wanted)
    {
        ReferencePoint point;
        try { point = doc.FamilyCreate.NewReferencePoint(at); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not place a point at " + mm(at.X) + ", " + mm(at.Y) + ", "
                + mm(at.Z) + " mm: " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        if (kind.Value != AdaptivePointType.ReferencePoint)
        {
            try { AdaptiveComponentFamilyUtils.MakeAdaptivePoint(doc, point.Id, kind.Value); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not make the point at " + mm(at.X) + ", " + mm(at.Y)
                    + ", " + mm(at.Z) + " mm a " + kindWords + ": " + ex.Message + " The call failed, and Heron rolls "
                    + "the whole call back.");
            }
        }
        made.Add(Tuple.Create(point, at));
    }

    doc.Regenerate();

    var ids = new List<string>();
    var rows = new List<string>();
    foreach (var m in made)
    {
        var at = m.Item1.Position;
        if (at == null || at.DistanceTo(m.Item2) > halfMillimetre)
            throw new InvalidOperationException("A point reads at " + (at == null ? "nowhere" : mm(at.X) + ", " + mm(at.Y)
                + ", " + mm(at.Z) + " mm") + ", not where it was placed. The call failed, and Heron rolls the whole "
                + "call back.");
        var isPlacement = AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, m.Item1.Id);
        var isHandle = AdaptiveComponentFamilyUtils.IsAdaptiveShapeHandlePoint(doc, m.Item1.Id);
        var asKind = isPlacement ? AdaptivePointType.PlacementPoint : isHandle ? AdaptivePointType.ShapeHandlePoint
            : AdaptivePointType.ReferencePoint;
        if (asKind != kind.Value)
            throw new InvalidOperationException("The point at " + mm(at.X) + ", " + mm(at.Y) + ", " + mm(at.Z)
                + " mm reads as a different kind from the " + kindWords + " asked. The call failed, and Heron rolls the "
                + "whole call back.");
        var label = kindWords;
        if (isPlacement) label += " " + AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, m.Item1.Id);
        ids.Add(m.Item1.UniqueId);
        rows.Add(label + " " + m.Item1.UniqueId + " at " + mm(at.X) + ", " + mm(at.Y) + ", " + mm(at.Z) + " mm");
    }

    pointIds = string.Join(",", ids);
    placedPoints = string.Join("  ||  ", rows);
    var total = 0;
    try { total = AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(doc.OwnerFamily); } catch (Exception) { }
    findings.Add(made.Count + " " + kindWords + "(s) placed and read back. The family now has " + total
        + " placement point(s) - a modeller clicks them in their number order to place it.");
    if (kind.Value == AdaptivePointType.PlacementPoint)
        findings.Add("Lines through these points are DRAW_FAMILY_POINT_CURVES, and a surface or solid on those lines "
            + "CREATE_CONCEPTUAL_FORM - geometry built on the points follows them when the family is placed.");
}

if (refused != null) findings.Add(refused);
