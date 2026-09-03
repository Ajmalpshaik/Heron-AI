// NOT STANDALONE. Assumes `doc`, `services` and `requiredGap` are in scope;
// leaves `findings`, `tooClose`, `clashing` and `slopedOrVertical` behind.
//
// READ ONLY. Opens no transaction and needs none. Distances are internal FEET.
//
// THIS IS NOT A STRAIGHT-LINE GAP WITH A SMALLER NUMBER. Two services 200 mm
// apart diagonally have 200 mm of straight-line clearance and may have 40 mm of
// VERTICAL room, which is what a flange, a hanger and an insulation jacket
// need. Only the Z gap is measured.
//
// AND ONLY FOR PAIRS ACTUALLY ABOVE ONE ANOTHER. Services passing at different
// plan positions are not a stacking problem; listing them buries the real ones.
//
// A NEGATIVE GAP IS AN OVERLAP, flagged CLASH, so it can never read as tight.
//
// BOUNDING BOXES ARE RIGHT FOR A LEVEL RUN AND PESSIMISTIC FOR A SLOPED ONE -
// the box is the whole rise, so the gap comes out smaller than the truth. It
// over-reports and never misses, and those runs are COUNTED so that is visible.

var findings = new List<string>();
var tooClose = 0;
var clashing = 0;
var slopedOrVertical = 0;

var boxes = new List<KeyValuePair<Element, BoundingBoxXYZ>>();
var noGeometry = 0;

foreach (var service in services)
{
    if (service == null || !service.IsValidObject) continue;
    var box = service.get_BoundingBox(null);
    if (box == null) { noGeometry++; continue; }
    boxes.Add(new KeyValuePair<Element, BoundingBoxXYZ>(service, box));

    // A run whose height is a large share of its footprint is sloped or
    // vertical, and its box is the whole rise rather than the pipe.
    var height = box.Max.Z - box.Min.Z;
    var run = Math.Max(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y);
    if (height > run * 0.25 && height > requiredGap) slopedOrVertical++;
}

Func<Element, string> describe = element => string.Format("{0} {1}",
    element.Category == null ? "element" : element.Category.Name, element.Id);

for (var i = 0; i < boxes.Count; i++)
{
    for (var j = i + 1; j < boxes.Count; j++)
    {
        var a = boxes[i];
        var b = boxes[j];

        // Plan footprints must overlap, or this is not a stacking question.
        // Tested on boxes too: two runs crossing at 45 degrees count as
        // overlapping over their box corner, which is deliberate - at a
        // crossing, the box corner is where the hanger goes.
        if (a.Value.Max.X < b.Value.Min.X || a.Value.Min.X > b.Value.Max.X) continue;
        if (a.Value.Max.Y < b.Value.Min.Y || a.Value.Min.Y > b.Value.Max.Y) continue;

        Element upper;
        Element lower;
        double gap;

        if (a.Value.Min.Z >= b.Value.Min.Z)
        {
            upper = a.Key; lower = b.Key;
            gap = a.Value.Min.Z - b.Value.Max.Z;
        }
        else
        {
            upper = b.Key; lower = a.Key;
            gap = b.Value.Min.Z - a.Value.Max.Z;
        }

        if (gap < 0)
        {
            clashing++;
            findings.Add(string.Format("CLASH: {0} and {1} interpenetrate in Z by {2:0.#} mm",
                describe(upper), describe(lower), -gap * 304.8));
        }
        else if (gap < requiredGap)
        {
            tooClose++;
            findings.Add(string.Format("TIGHT: {0} is above {1} by {2:0.#} mm - {3:0.#} mm short of the "
                + "{4:0.#} mm asked for", describe(upper), describe(lower), gap * 304.8,
                (requiredGap - gap) * 304.8, requiredGap * 304.8));
        }
    }
}

findings.Insert(0, string.Format("{0} service(s) checked against a {1:0.#} mm vertical rule: {2} pair(s) "
    + "tight, {3} clashing. Reported BY EXCEPTION - a pair not listed has room",
    boxes.Count, requiredGap * 304.8, tooClose, clashing));

if (slopedOrVertical > 0)
{
    findings.Add(string.Format("{0} run(s) are sloped or vertical. Their bounding box is the whole "
        + "rise, so any gap reported for them is SMALLER than the truth - take those to a section",
        slopedOrVertical));
}
if (noGeometry > 0)
{
    findings.Add(string.Format("{0} element(s) had no readable geometry and were not checked", noGeometry));
}
