// NOT STANDALONE. Assumes `elements` (the sources), `targets` (what to search)
// and `metric` are in scope, and leaves `nearest`, `distancesMm`, `tied` and
// `unmeasurable` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THREE METRICS, AND THE CHOICE CHANGES THE ANSWER.
//
//   centre     straight line between box centres. Fine for point-like
//              equipment. MISLEADING for long elements - a 30 m wall's centre
//              can be far away while the wall runs right past you.
//   gap        closest approach between the two boxes, 0 when they overlap.
//              Real clearance, and the honest answer to "how close is that".
//   manhattan  |dx| + |dy| + |dz| between centres. Cable, conduit and duct do
//              not fly diagonally through a building; they run orthogonally,
//              so for "how much cable will this take" this beats a straight
//              line by a wide margin.
//
// It is a REQUEST input rather than a default because all three are defensible
// and picking one silently answers a different question from the one asked.
//
// PROXIMITY IS NOT CONNECTION OR DIRECTION. The nearest FCU to a terminal is
// not necessarily the one serving it, and the nearest exit may be through a
// wall. This narrows a search; TRACE_CONNECTIVITY establishes what is joined.
//
// THE GAP IS OPTIMISTIC ON A ROTATED ELEMENT. Bounding boxes are axis-aligned,
// so a rotated element's box is bigger than the element and two of them read
// closer than they are. Fine for RANKING, which is what this is for. Not a
// certified clearance.

// MILLIMETRES OUT, FEET INSIDE. Every comparison below is done in Revit's own
// feet - that is what a bounding box is in, and converting before comparing
// would buy nothing and risk the tie tolerance. The ONE number that leaves
// this fragment is converted, once, where it is stored.
//
// A DISTANCE IN THE WRONG UNIT IS A WRONG ANSWER THAT LOOKS RIGHT. `distances`
// used to be handed out in feet under a bare name, and its own contract cited
// D-20 for it. D-20 says no units API - plain arithmetic - and says nothing
// about what a RESULT is measured in; D-71 left that open in as many words.
// MEASURE_DISTANCE, which answers the same question about two named elements,
// has always multiplied by 304.8 and says so in its purpose. Two fragments
// answering "how far apart" in units 304.8x apart is the failure this library
// is built to refuse.
const double MillimetresPerFoot = 304.8;

var nearest = new Dictionary<ElementId, ElementId>();
var distancesMm = new Dictionary<ElementId, double>();
var tied = new List<ElementId>();
var unmeasurable = new List<ElementId>();

string how = (metric ?? "").Trim().ToLowerInvariant();
bool useGap = how == "gap";
bool useManhattan = how == "manhattan";

Func<Element, BoundingBoxXYZ> boxOf = element =>
{
    try { return element.get_BoundingBox(null); }
    catch { return null; }
};

Func<BoundingBoxXYZ, XYZ> centreOf = box => new XYZ(
    (box.Min.X + box.Max.X) / 2.0,
    (box.Min.Y + box.Max.Y) / 2.0,
    (box.Min.Z + box.Max.Z) / 2.0);

// Gap on one axis: how far apart the two spans are, 0 when they overlap.
Func<double, double, double, double, double> axisGap = (aMin, aMax, bMin, bMax) =>
{
    if (bMin > aMax) return bMin - aMax;
    if (aMin > bMax) return aMin - bMax;
    return 0.0;
};

// Boxes once per target, not once per source pair.
var targetBoxes = new List<KeyValuePair<ElementId, BoundingBoxXYZ>>();
foreach (var target in targets)
{
    if (target == null) continue;
    var box = boxOf(target);
    if (box != null) targetBoxes.Add(new KeyValuePair<ElementId, BoundingBoxXYZ>(target.Id, box));
}

// A tie is real. Two candidates equally close is a situation the model is
// actually in, and settling it on id order would make the answer depend on
// whatever order Revit handed them back in.
const double TieTolerance = 1.0 / 304.8;   // one millimetre, in feet

foreach (var element in elements)
{
    if (element == null) continue;

    var sourceBox = boxOf(element);
    if (sourceBox == null || targetBoxes.Count == 0)
    {
        unmeasurable.Add(element.Id);
        continue;
    }

    var sourceCentre = centreOf(sourceBox);
    ElementId best = ElementId.InvalidElementId;
    double bestDistance = double.MaxValue;
    int equallyClose = 0;

    foreach (var pair in targetBoxes)
    {
        if (pair.Key == element.Id) continue;   // never its own nearest
        var box = pair.Value;
        double distance;

        if (useGap)
        {
            double dx = axisGap(sourceBox.Min.X, sourceBox.Max.X, box.Min.X, box.Max.X);
            double dy = axisGap(sourceBox.Min.Y, sourceBox.Max.Y, box.Min.Y, box.Max.Y);
            double dz = axisGap(sourceBox.Min.Z, sourceBox.Max.Z, box.Min.Z, box.Max.Z);
            distance = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }
        else
        {
            var centre = centreOf(box);
            double dx = centre.X - sourceCentre.X;
            double dy = centre.Y - sourceCentre.Y;
            double dz = centre.Z - sourceCentre.Z;
            distance = useManhattan
                ? Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz)
                : Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        if (distance < bestDistance - TieTolerance)
        {
            bestDistance = distance;
            best = pair.Key;
            equallyClose = 1;
        }
        else if (Math.Abs(distance - bestDistance) <= TieTolerance)
        {
            equallyClose++;
        }
    }

    if (best == ElementId.InvalidElementId) { unmeasurable.Add(element.Id); continue; }

    nearest[element.Id] = best;
    distancesMm[element.Id] = bestDistance * MillimetresPerFoot;
    if (equallyClose > 1) tied.Add(element.Id);
}
