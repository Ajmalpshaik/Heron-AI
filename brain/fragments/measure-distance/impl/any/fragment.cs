// NOT STANDALONE. Assumes `doc`, `first` and `second` are in scope; leaves
// `straightMm`, `horizontalMm`, `verticalMm` and `findings` behind.
//
// AN ELEMENT IS NOT A POINT. A thirty-metre duct has no single position, so
// centre to centre would call two touching runs thirty metres apart. The gap is
// measured between the nearest faces of the two bounding boxes, and the report
// says which definition was used - where both are point-placed the two agree.
//
// THREE NUMBERS, BECAUSE ALL THREE GET ASKED. Straight line is the tape
// measure; horizontal is what a plan shows; vertical is the one that decides
// whether a duct clears a beam. Reporting only the first makes the reader do
// the trigonometry the question was about.
//
// MILLIMETRES OUT, by multiplying by 304.8 - internal feet are not what anybody
// asked, and naming the unit is what stops the number being read as another one.

const double MillimetresPerFoot = 304.8;

var straightMm = 0.0;
var horizontalMm = 0.0;
var verticalMm = 0.0;
var findings = new List<string>();

if (first == null || second == null)
{
    findings.Add("Two elements are needed - name both of the things being measured between");
}
else if (first.Id == second.Id)
{
    findings.Add("Both sides are the same element, so the distance is zero by definition rather "
        + "than by measurement");
}
else
{
    var boxA = first.get_BoundingBox(null);
    var boxB = second.get_BoundingBox(null);

    if (boxA == null || boxB == null)
    {
        findings.Add(string.Format("{0} has no geometry to measure from. An element with no bounding "
            + "box - much annotation, and the analytical elements - has no position in space",
            boxA == null ? first.Name : second.Name));
    }
    else
    {
        // The gap along one axis is zero where the two ranges overlap, and the
        // clear distance between them where they do not.
        Func<double, double, double, double, double> gap = (lowA, highA, lowB, highB) =>
        {
            if (highA < lowB) return lowB - highA;
            if (highB < lowA) return lowA - highB;
            return 0.0;
        };

        var dx = gap(boxA.Min.X, boxA.Max.X, boxB.Min.X, boxB.Max.X);
        var dy = gap(boxA.Min.Y, boxA.Max.Y, boxB.Min.Y, boxB.Max.Y);
        var dz = gap(boxA.Min.Z, boxA.Max.Z, boxB.Min.Z, boxB.Max.Z);

        var straight = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var horizontal = Math.Sqrt(dx * dx + dy * dy);

        straightMm = straight * MillimetresPerFoot;
        horizontalMm = horizontal * MillimetresPerFoot;
        verticalMm = dz * MillimetresPerFoot;

        findings.Add(string.Format("{0:0.#} mm apart in a straight line: {1:0.#} mm horizontally and "
            + "{2:0.#} mm vertically, measured between the nearest faces of the two bounding boxes",
            straightMm, horizontalMm, verticalMm));

        if (straight < 1e-9)
            findings.Add("They touch or overlap, so every distance is zero. That is a real answer, "
                + "and this fragment cannot tell touching from overlapping - SELECT_TOUCHING can");

        findings.Add("A bounding box is axis-aligned and is larger than a sloped or rotated element, "
            + "so for those this gap is a LOWER bound: the real clearance is at least this and may "
            + "be more");
    }
}
