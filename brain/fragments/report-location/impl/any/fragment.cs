// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `locations`, `withoutLocation` and `findings` behind.
//
// THREE KINDS OF POSITION, AND THE REPORT SAYS WHICH. Equipment is at a POINT.
// A duct RUNS from one place to another and has no single position at all.
// Some elements have neither. Printing one set of coordinates for all three
// makes the second kind a lie: the midpoint of a thirty-metre run is a place
// the duct passes through, not where it is.
//
// THE BOUNDING-BOX CENTRE IS NAMED AS A FALLBACK EVERY TIME, never dressed up
// as a location - for anything L-shaped or sloped it is a point in fresh air.
//
// COORDINATES ARE MEASURED FROM THE PROJECT'S INTERNAL ORIGIN, which is not
// necessarily the survey point or the base point. Good for comparing elements
// with each other; not a coordinate to hand a surveyor.

const double MillimetresPerFoot = 304.8;

var locations = new List<string>();
var withoutLocation = 0;
var findings = new List<string>();

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given");
}
else
{
    Func<XYZ, string> asMm = p => string.Format("({0:0}, {1:0}, {2:0}) mm",
        p.X * MillimetresPerFoot, p.Y * MillimetresPerFoot, p.Z * MillimetresPerFoot);

    var points = 0;
    var runs = 0;
    var boxes = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var atPoint = element.Location as LocationPoint;
        if (atPoint != null)
        {
            points++;
            locations.Add(string.Format("id {0}: AT {1}", element.Id, asMm(atPoint.Point)));
            continue;
        }

        var alongCurve = element.Location as LocationCurve;
        if (alongCurve != null && alongCurve.Curve != null)
        {
            runs++;
            locations.Add(string.Format("id {0}: RUNS from {1} to {2}",
                element.Id,
                asMm(alongCurve.Curve.GetEndPoint(0)),
                asMm(alongCurve.Curve.GetEndPoint(1))));
            continue;
        }

        var box = element.get_BoundingBox(null);
        if (box != null)
        {
            boxes++;
            var centre = (box.Min + box.Max) / 2.0;
            locations.Add(string.Format("id {0}: no position of its own - the CENTRE OF ITS BOUNDING "
                + "BOX is {1}", element.Id, asMm(centre)));
            continue;
        }

        withoutLocation++;
        locations.Add(string.Format("id {0}: no position and no bounding box", element.Id));
    }

    findings.Add(string.Format("{0} element(s): {1} at a point, {2} running between two, {3} reported "
        + "by the centre of a bounding box, and {4} with no position at all",
        elements.Count, points, runs, boxes, withoutLocation));

    if (runs > 0)
        findings.Add("An element that RUNS has no single position. Both ends are given because the "
            + "midpoint of a long run is somewhere it passes through, not where it is");

    if (boxes > 0)
        findings.Add("A bounding-box centre is a fallback and is named as one. For an L-shaped or "
            + "sloped element it is a point in fresh air, outside the element entirely");

    findings.Add("These are measured from the project's internal origin, which is not necessarily the "
        + "survey point or the project base point. Good for comparing elements with each other; a "
        + "coordinate for a surveyor is a different question");
}
