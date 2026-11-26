// NOT STANDALONE. Assumes `doc`, `pointCloudName`, `boxMm`, `bandMm` and
// `maxPoints` are in scope, and leaves `candidates`, `candidateElevationsMm`,
// `pointsRead`, `zMinMm`, `zMaxMm`, `pointCloudUsed` and `findings` behind.
//
// READ ONLY. Opens no transaction, creates nothing. Every line it gives back
// is a CANDIDATE for the modeller to say yes to - never a level (D-33).
//
// WHAT IT DOES. Reads a sample of the linked point cloud's points inside a
// box, sorts their heights into bands `bandMm` tall, and reports each band
// that holds far more points than the bands around it. A floor slab, a roof,
// a ceiling or the ground is a dense HORIZONTAL layer of points; a facade is
// spread evenly up its whole height. So a band that stands out from its
// neighbours is a horizontal surface, and that is all this claims.
//
// AN OUTSIDE-ONLY SCAN HAS NO FLOORS IN IT. A street scan sees the ground,
// the facades, and perhaps an eave, a canopy or a parapet - never a floor
// slab inside the building. Every answer says so, because "two candidates"
// from a facade scan is right, and must not be read as "a two-storey
// building".
//
// THE FILTER IS IN MODEL COORDINATES - RevitAPI.xml, on GetPoints: "The filter
// should be passed in the coordinates of the Revit model". The POINTS are
// carried through the instance's total transform into model space; RevitAPI.xml
// does not say which space they come back in. On a cloud linked origin to
// origin with no rotation the transform is the identity and the two readings
// agree; on a moved cloud the proof decides, and the answer reports points
// that fall outside the cloud's own extent rather than hiding them.
//
// HEIGHTS ARE FROM THE INTERNAL ORIGIN - the same space as Level.ProjectElevation
// and as the millimetres CREATE_LEVELS takes. The project's own units show the
// same number beside it. Each candidate also names the nearest existing level
// and how far it is from it, so "already have that one" is visible.
//
// The filter factory is the one type here that must be named; its namespace
// is in the fragment imports since 2026-11-25. The points are reached by var.

const double MillimetresPerFoot = 304.8;

string pointCloudUsed = "";
int pointsRead = 0;
double zMinMm = 0.0;
double zMaxMm = 0.0;
var candidates = new List<string>();
var candidateElevationsMm = new List<double>();
var findings = new List<string>();

// ---- inputs --------------------------------------------------------------

if (double.IsNaN(bandMm) || double.IsInfinity(bandMm) || bandMm < 5.0 || bandMm > 1000.0)
    throw new InvalidOperationException(string.Format(
        "A band of {0} mm is not one this can use - give a band between 5 mm and 1000 mm (25 mm is usual).", bandMm));

if (maxPoints < 1000 || maxPoints > 1000000)
    throw new InvalidOperationException(string.Format(
        "{0} points is not a number this can read - give between 1,000 and 1,000,000 (Revit's own limit).", maxPoints));

// ---- which point cloud ---------------------------------------------------

var clouds = new FilteredElementCollector(doc)
    .OfClass(typeof(PointCloudInstance))
    .Cast<PointCloudInstance>()
    .ToList();

Func<PointCloudInstance, string> cloudLabel = pc =>
{
    string typeName = "";
    try
    {
        var t = doc.GetElement(pc.GetTypeId());
        if (t != null) typeName = t.Name ?? "";
    }
    catch (Exception) { }
    string own = pc.Name ?? "";
    return string.IsNullOrEmpty(typeName) || typeName == own ? own : own + " (" + typeName + ")";
};

if (clouds.Count == 0)
    throw new InvalidOperationException(
        "No point cloud is linked in this model, so there are no points to read levels from. "
        + "Link one with Insert > Point Cloud and ask again.");

PointCloudInstance cloud = null;
string asked = (pointCloudName ?? "").Trim();
if (asked.Length == 0 || string.Equals(asked, "only", StringComparison.OrdinalIgnoreCase))
{
    if (clouds.Count > 1)
        throw new InvalidOperationException(string.Format(
            "This model has {0} point clouds - {1} - so say which one to read.",
            clouds.Count, string.Join(", ", clouds.Select(cloudLabel))));
    cloud = clouds[0];
}
else
{
    var exact = clouds.Where(pc => string.Equals(pc.Name, asked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(cloudLabel(pc), asked, StringComparison.OrdinalIgnoreCase)).ToList();
    var near = exact.Count > 0 ? exact
        : clouds.Where(pc => cloudLabel(pc).IndexOf(asked, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    if (near.Count == 0)
        throw new InvalidOperationException(string.Format(
            "No point cloud matches '{0}'. This model has: {1}.", asked, string.Join(", ", clouds.Select(cloudLabel))));
    if (near.Count > 1)
        throw new InvalidOperationException(string.Format(
            "AMBIGUOUS: {0} point clouds match '{1}' - {2}. Name one exactly.",
            near.Count, asked, string.Join(", ", near.Select(cloudLabel))));
    cloud = near[0];
}
pointCloudUsed = cloudLabel(cloud);

// ---- the box to read, in model coordinates ---------------------------------

var bounds = cloud.get_BoundingBox(null);
if (bounds == null)
    throw new InvalidOperationException(string.Format(
        "Revit gives no extent for the point cloud '{0}' - it may be unloaded or its file missing. "
        + "Check it in Manage Links > Point Clouds and ask again.", pointCloudUsed));

double minX = bounds.Min.X, minY = bounds.Min.Y, maxX = bounds.Max.X, maxY = bounds.Max.Y;
string boxSaid = "the whole cloud";
string boxText = (boxMm ?? "").Trim();
if (boxText.Length > 0 && !string.Equals(boxText, "all", StringComparison.OrdinalIgnoreCase))
{
    var parts = boxText.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
    var numbers = new List<double>();
    foreach (var p in parts)
    {
        double v;
        if (!double.TryParse(p, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out v) || double.IsNaN(v) || double.IsInfinity(v))
            throw new InvalidOperationException(string.Format(
                "'{0}' in the box is not a number. Give the box as minX,minY,maxX,maxY in millimetres.", p));
        numbers.Add(v);
    }
    if (numbers.Count != 4)
        throw new InvalidOperationException(string.Format(
            "The box needs four numbers - minX,minY,maxX,maxY in millimetres - and '{0}' has {1}.", boxText, numbers.Count));
    minX = Math.Min(numbers[0], numbers[2]) / MillimetresPerFoot;
    maxX = Math.Max(numbers[0], numbers[2]) / MillimetresPerFoot;
    minY = Math.Min(numbers[1], numbers[3]) / MillimetresPerFoot;
    maxY = Math.Max(numbers[1], numbers[3]) / MillimetresPerFoot;
    if (maxX - minX < 1.0 / MillimetresPerFoot || maxY - minY < 1.0 / MillimetresPerFoot)
        throw new InvalidOperationException("The box is flat - its two corners must differ in both X and Y.");
    boxSaid = string.Format("the box X {0:0} to {1:0} mm, Y {2:0} to {3:0} mm",
        minX * MillimetresPerFoot, maxX * MillimetresPerFoot, minY * MillimetresPerFoot, maxY * MillimetresPerFoot);
}

// A foot above and below the cloud's own extent, so the ground and the top
// are not cut on the plane.
double slack = 1.0;
double minZ = bounds.Min.Z - slack, maxZ = bounds.Max.Z + slack;

// Normals point INTO the box: a point passes when it is on the positive side
// of every plane.
var planes = new List<Plane>
{
    Plane.CreateByNormalAndOrigin(XYZ.BasisX, new XYZ(minX, 0, 0)),
    Plane.CreateByNormalAndOrigin(-XYZ.BasisX, new XYZ(maxX, 0, 0)),
    Plane.CreateByNormalAndOrigin(XYZ.BasisY, new XYZ(0, minY, 0)),
    Plane.CreateByNormalAndOrigin(-XYZ.BasisY, new XYZ(0, maxY, 0)),
    Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, minZ)),
    Plane.CreateByNormalAndOrigin(-XYZ.BasisZ, new XYZ(0, 0, maxZ)),
};
var filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);

// ---- read the points --------------------------------------------------------

// Spacing asked of Revit: a fifth of a band, so a 25 mm band is read at 5 mm,
// and never finer than 2 mm. Revit returns up to maxPoints at about this
// spacing, whatever the scanner's own density.
double averageDistance = Math.Max(bandMm / 5.0, 2.0) / MillimetresPerFoot;

var points = cloud.GetPoints(filter, averageDistance, maxPoints);
Transform toModel = cloud.GetTotalTransform();

var zs = new List<double>(points == null ? 0 : points.Count);
if (points != null)
{
    foreach (var cp in points)
    {
        XYZ raw = cp;
        XYZ here = toModel == null ? raw : toModel.OfPoint(raw);
        zs.Add(here.Z * MillimetresPerFoot);
    }
}
pointsRead = zs.Count;

findings.Add(string.Format("Point cloud '{0}', read inside {1}: {2} point(s) (asked for up to {3}).",
    pointCloudUsed, boxSaid, pointsRead, maxPoints));

if (pointsRead == 0)
{
    findings.Add("NO POINTS were read inside that box, so there are no candidates. The box may miss the cloud, "
        + "or the cloud may be hidden or unloaded. Nothing is guessed.");
}
else
{
    zMinMm = zs.Min();
    zMaxMm = zs.Max();
    findings.Add(string.Format("Heights read: {0:0} mm to {1:0} mm from the internal origin ({2:0} mm span).",
        zMinMm, zMaxMm, zMaxMm - zMinMm));

    // The cloud's own extent, to catch points read in the wrong space.
    double boxLowMm = bounds.Min.Z * MillimetresPerFoot, boxHighMm = bounds.Max.Z * MillimetresPerFoot;
    if (zMinMm < boxLowMm - 1000.0 || zMaxMm > boxHighMm + 1000.0)
        findings.Add(string.Format(
            "CHECK: the points read lie outside the cloud's own extent in Revit ({0:0} to {1:0} mm) by more "
            + "than a metre. The heights below may be in the scan's own coordinates rather than the model's - "
            + "do not use them until that is looked at.", boxLowMm, boxHighMm));

    if (pointsRead < 1000)
        findings.Add("FEW POINTS: under 1,000 were read, so a band's count says little. Widen the box or raise maxPoints.");

    // ---- the histogram ------------------------------------------------------

    int bandCount = (int)Math.Floor((zMaxMm - zMinMm) / bandMm) + 1;
    var counts = new int[bandCount];
    var sums = new double[bandCount];
    foreach (var z in zs)
    {
        int i = (int)Math.Floor((z - zMinMm) / bandMm);
        if (i < 0) i = 0;
        if (i >= bandCount) i = bandCount - 1;
        counts[i]++;
        sums[i] += z;
    }

    // A band's background is the median of the bands within a metre either
    // side, leaving out the two next to it each way so a thick slab does not
    // count itself as its own background. Bands beyond the cloud count as 0.
    int reach = Math.Max(4, (int)Math.Round(1000.0 / bandMm));
    int gap = 2;
    var found = new List<Tuple<double, double, double, string>>(); // elevation mm, share, ratio, confidence

    for (int i = 0; i < bandCount; i++)
    {
        int c = counts[i];
        if (c == 0) continue;

        // A peak is the highest band among its immediate neighbours; a tie
        // goes to the lower band, so a flat top is reported once.
        bool top = true;
        for (int j = Math.Max(0, i - gap); j <= Math.Min(bandCount - 1, i + gap); j++)
            if (j != i && (counts[j] > c || (counts[j] == c && j < i))) { top = false; break; }
        if (!top) continue;

        var around = new List<int>();
        for (int j = i - reach; j <= i + reach; j++)
        {
            if (Math.Abs(j - i) <= gap) continue;
            around.Add(j >= 0 && j < bandCount ? counts[j] : 0);
        }
        around.Sort();
        double background = around.Count == 0 ? 0.0 : around[around.Count / 2];
        double ratio = c / Math.Max(background, 1.0);

        // The layer is the peak band and the one either side of it.
        int lo = Math.Max(0, i - 1), hi = Math.Min(bandCount - 1, i + 1);
        int layerCount = 0; double layerSum = 0.0;
        for (int j = lo; j <= hi; j++) { layerCount += counts[j]; layerSum += sums[j]; }
        double share = (double)layerCount / pointsRead;

        if (ratio < 3.0 || share < 0.005) continue;

        double elevationMm = layerSum / layerCount;
        string confidence = ratio >= 8.0 ? "HIGH" : ratio >= 5.0 ? "MEDIUM" : "LOW";
        found.Add(Tuple.Create(elevationMm, share, ratio, confidence));
    }

    // ---- the existing levels, to compare against ------------------------------

    var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
        .OrderBy(l => l.ProjectElevation).ToList();

    foreach (var f in found.OrderBy(t => t.Item1))
    {
        double feet = f.Item1 / MillimetresPerFoot;
        string shown;
        try
        {
#if REVIT2020
            shown = UnitFormatUtils.Format(doc.GetUnits(), UnitType.UT_Length, feet, false, false);
#else
            shown = UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, feet, false);
#endif
        }
        catch (Exception) { shown = string.Format("{0:0} mm", f.Item1); }

        string nearest = "no level in this model";
        if (levels.Count > 0)
        {
            var l = levels.OrderBy(x => Math.Abs(x.ProjectElevation - feet)).First();
            double apartMm = (feet - l.ProjectElevation) * MillimetresPerFoot;
            nearest = Math.Abs(apartMm) < 0.5
                ? string.Format("on '{0}'", l.Name)
                : string.Format("{0:0} mm {1} '{2}'", Math.Abs(apartMm), apartMm > 0 ? "above" : "below", l.Name);
        }

        candidateElevationsMm.Add(Math.Round(f.Item1, 0));
        candidates.Add(string.Format(
            "CANDIDATE {0:0} mm ({1}) | {2:0.0}% of points | {3}x its neighbours, {4} | nearest level: {5}",
            f.Item1, shown, f.Item2 * 100.0, Math.Round(f.Item3, 1), f.Item4, nearest));
    }

    if (candidates.Count == 0)
        findings.Add("NO dense horizontal layer stands out in this box - nothing here is proposed as a level. "
            + "A box over a facade alone, or a band too fine for a sparse scan, gives this; it is an answer, not a failure.");
    else
        findings.Add(string.Format("{0} candidate level(s), bottom to top - the lowest is usually the ground or the lowest floor.",
            candidates.Count));
}

findings.AddRange(candidates);
findings.Add("THESE ARE CANDIDATES, NOT LEVELS. Nothing was created. Each one is a horizontal surface the scan "
    + "caught - a floor, a ceiling, a roof, a canopy or the ground - for you to say yes or no to before any level is made.");
findings.Add("A SCAN TAKEN FROM OUTSIDE ONLY catches the ground and the roof, eaves or parapets - never the floors "
    + "inside. Floor levels need an interior scan, or a box drawn over where the scan sees a floor edge.");
