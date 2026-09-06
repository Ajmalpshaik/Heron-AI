// NOT STANDALONE. Assumes `elements` and `coverageRadius` are in scope, and
// leaves `areaEach`, `areaTotal`, `nearestNeighbour`, `gapSuspects` and
// `noPoint` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THERE IS NO COVERAGE PARAMETER TO READ.
//
// A diffuser carries flow, pressure drop and neck size. What it COVERS is a
// design decision - a throw at a velocity, a code spacing, a lighting level -
// so the radius arrives with the request. A fragment that invented one would
// put a number nobody chose into a layout somebody signs.
//
// THE TOTAL IS AN UPPER BOUND.
//
// Overlapping circles are counted twice by a sum. Below the room's floor area
// it proves there are gaps; above it, it proves nothing at all. Both halves of
// that are worth saying, because the number looks like an answer either way.
//
// THE NEIGHBOUR DISTANCE IS WHERE A GAP SHOWS.
//
// Two elements more than twice the radius apart cannot overlap, so there is
// floor between them that nothing covers. That is the comparison that finds a
// hole in a layout, and it is per element rather than a single verdict.

double areaEach = Math.PI * coverageRadius * coverageRadius;
double areaTotal = 0;
var nearestNeighbour = new Dictionary<ElementId, double>();
var gapSuspects = new List<ElementId>();
var noPoint = new List<ElementId>();

var placed = new List<KeyValuePair<ElementId, XYZ>>();

foreach (var element in elements)
{
    if (element == null) continue;

    XYZ where = null;
    var point = element.Location as LocationPoint;
    if (point != null) { try { where = point.Point; } catch { } }

    if (where == null)
    {
        try
        {
            var box = element.get_BoundingBox(null);
            if (box != null) where = (box.Min + box.Max) / 2.0;
        }
        catch { }
    }

    if (where == null) { noPoint.Add(element.Id); continue; }

    placed.Add(new KeyValuePair<ElementId, XYZ>(element.Id, where));
}

areaTotal = areaEach * placed.Count;

for (int i = 0; i < placed.Count; i++)
{
    double nearest = double.MaxValue;

    for (int j = 0; j < placed.Count; j++)
    {
        if (i == j) continue;
        // Measured in plan: two diffusers at different ceiling heights in the
        // same room are neighbours, and their height difference is not a gap.
        double dx = placed[i].Value.X - placed[j].Value.X;
        double dy = placed[i].Value.Y - placed[j].Value.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < nearest) nearest = distance;
    }

    if (nearest == double.MaxValue)
    {
        // The only one in the set. It covers what it covers and has no
        // neighbour to leave a gap with, which is a fact rather than a fault.
        nearestNeighbour[placed[i].Key] = 0;
        continue;
    }

    nearestNeighbour[placed[i].Key] = nearest;

    // Further apart than two radii means the circles cannot touch: floor
    // between them that nothing covers.
    if (nearest > 2.0 * coverageRadius) gapSuspects.Add(placed[i].Key);
}
