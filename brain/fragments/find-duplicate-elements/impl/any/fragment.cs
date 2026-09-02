// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `elements` narrowed to the EXTRAS, plus `findings` and `noPoint`.
//
// READ ONLY. Opens no transaction, needs none, and deletes nothing. The split
// between finding and deleting is deliberate - see the routing table.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// ONE OF EACH CLUSTER IS KEPT, and the rest are handed on. That is what makes
// the result safe to pass to DELETE_ELEMENTS: acting on every member of a
// cluster removes the real element along with its copy, and the drawing loses a
// diffuser that was supposed to be there. So a cluster of three yields two.
//
// SAME TYPE AND SAME PLACE, BOTH REQUIRED. Two different diffuser types at one
// point is a design clash - a real coordination problem - and putting it in
// this list files it under "things to bulk delete".
//
// GROUPED ON A ROUNDED CELL, NOT COMPARED PAIRWISE. Comparing every element
// with every other is fine for fifty and unusable for five thousand, which is
// the size a whole-floor check actually is. Rounding each point onto a grid of
// the tolerance gives the same answer in one pass.
//
// AND THE NEIGHBOURING CELLS ARE CHECKED TOO, because rounding alone misses the
// pair this fragment most needs to catch: two elements 1 mm apart that fall
// either side of a cell boundary land in different cells and are reported as
// unique. Twenty-seven cells per element is still one pass.
//
// A CURVE-BASED ELEMENT IS PLACED BY ITS MIDPOINT. Two identical ducts on top
// of each other share a midpoint; two ducts merely meeting end to end do not,
// which is right - they are a run, not a duplicate.

var tolerance = toleranceMm / 304.8;

var findings = new List<string>();
var noPoint = new List<ElementId>();

// Guarded rather than trusted: a zero or negative tolerance makes every
// coordinate collapse to one cell and reports the entire input as duplicates
// of each other.
var cell = tolerance > 1e-9 ? tolerance : 1e-9;

var buckets = new Dictionary<string, List<Element>>();
var placed = new Dictionary<ElementId, XYZ>();

foreach (var element in elements)
{
    if (element == null) continue;

    XYZ point = null;

    var asPoint = element.Location as LocationPoint;
    if (asPoint != null) point = asPoint.Point;
    else
    {
        var asCurve = element.Location as LocationCurve;
        if (asCurve != null && asCurve.Curve != null)
        {
            var curve = asCurve.Curve;
            point = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2.0;
        }
    }

    // No location at all - a group, a datum, a view-owned element. It cannot be
    // in the same PLACE as anything, so it is recorded rather than silently
    // dropped from a count somebody reconciles.
    if (point == null) { noPoint.Add(element.Id); continue; }

    placed[element.Id] = point;

    var typeId = element.GetTypeId();
    var typeKey = typeId == null ? "none" : typeId.ToString();

    var key = string.Format("{0}|{1}|{2}|{3}",
                            typeKey,
                            (long)Math.Floor(point.X / cell),
                            (long)Math.Floor(point.Y / cell),
                            (long)Math.Floor(point.Z / cell));

    if (!buckets.ContainsKey(key)) buckets[key] = new List<Element>();
    buckets[key].Add(element);
}

// The extras, and the ones already accounted for. `claimed` stops an element
// being counted twice when it is reachable from more than one neighbouring
// cell.
var extras = new List<Element>();
var claimed = new HashSet<ElementId>();
var clusters = 0;

foreach (var entry in buckets)
{
    var seed = entry.Value[0];
    if (claimed.Contains(seed.Id)) continue;

    var typeId = seed.GetTypeId();
    var typeKey = typeId == null ? "none" : typeId.ToString();
    var seedPoint = placed[seed.Id];

    var baseX = (long)Math.Floor(seedPoint.X / cell);
    var baseY = (long)Math.Floor(seedPoint.Y / cell);
    var baseZ = (long)Math.Floor(seedPoint.Z / cell);

    // The cluster: this cell and the twenty-six around it. Two elements a
    // millimetre apart can fall either side of a cell boundary, and those are
    // exactly the pairs this exists to catch.
    var cluster = new List<Element>();

    for (long dx = -1; dx <= 1; dx++)
    for (long dy = -1; dy <= 1; dy++)
    for (long dz = -1; dz <= 1; dz++)
    {
        var key = string.Format("{0}|{1}|{2}|{3}", typeKey, baseX + dx, baseY + dy, baseZ + dz);
        if (!buckets.ContainsKey(key)) continue;

        foreach (var candidate in buckets[key])
        {
            if (claimed.Contains(candidate.Id)) continue;
            if (candidate.Id == seed.Id) continue;

            // The cell was only ever a way of not comparing everything with
            // everything. The real test is the distance.
            if (placed[candidate.Id].DistanceTo(seedPoint) > tolerance) continue;

            cluster.Add(candidate);
        }
    }

    if (cluster.Count == 0) continue;

    clusters++;
    claimed.Add(seed.Id);

    foreach (var duplicate in cluster)
    {
        claimed.Add(duplicate.Id);
        extras.Add(duplicate);
    }

    var type = doc.GetElement(seed.GetTypeId());

    findings.Add(string.Format(
        "{0} x{1} at the same point - {2} extra",
        type == null ? seed.Name : type.Name,
        cluster.Count + 1,
        cluster.Count));
}

if (clusters > 0)
{
    findings.Add(string.Format(
        "{0} cluster(s), {1} extra element(s). ONE of each cluster is kept and is NOT "
        + "in this list - deleting everything found here would remove the real element "
        + "along with its copy", clusters, extras.Count));
}

elements = extras;
