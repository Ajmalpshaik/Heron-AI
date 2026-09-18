// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `centroidOf`, `combined`, `noSolid` and `findings`.
//
// A READ. It opens no transaction and needs none.
//
// NOT THE BOUNDING BOX CENTRE. An axis-aligned box's centre is not the centre of
// what is inside it, and for anything L-shaped, sloped or rotated the box centre
// can sit outside the element entirely.
//
// THE COMBINED POINT IS WEIGHTED BY VOLUME. Averaging centroids treats a bolt
// and a beam as equals and centres nothing. It is a centre of MASS only where
// the material is the same, which the finding says rather than implies.
//
// COORDINATES STAY IN FEET. A point is handed to another fragment far more often
// than it is read by a person (D-20).

var centroidOf = new Dictionary<ElementId, XYZ>();
XYZ combined = null;
var noSolid = new List<ElementId>();
var findings = new List<string>();

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given, so there is nothing to measure");
}
else
{
    var options = new Options();
    options.ComputeReferences = false;
    options.IncludeNonVisibleObjects = false;

    var weightedX = 0.0;
    var weightedY = 0.0;
    var weightedZ = 0.0;
    var totalVolume = 0.0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var volumeHere = 0.0;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumZ = 0.0;

        try
        {
            var geometry = element.get_Geometry(options);
            if (geometry != null)
            {
                foreach (var item in geometry)
                {
                    var solid = item as Solid;
                    if (solid == null) continue;

                    double volume;
                    XYZ centre;
                    try
                    {
                        volume = solid.Volume;
                        if (volume <= 0) continue;
                        centre = solid.ComputeCentroid();
                    }
                    catch
                    {
                        continue;
                    }
                    if (centre == null) continue;

                    // EACH SOLID WEIGHTED BY ITS OWN VOLUME - an element can be
                    // several solids and the big one has to count for more.
                    sumX += centre.X * volume;
                    sumY += centre.Y * volume;
                    sumZ += centre.Z * volume;
                    volumeHere += volume;
                }
            }
        }
        catch
        {
        }

        if (volumeHere <= 0)
        {
            noSolid.Add(element.Id);
            continue;
        }

        var here = new XYZ(sumX / volumeHere, sumY / volumeHere, sumZ / volumeHere);
        centroidOf[element.Id] = here;

        weightedX += here.X * volumeHere;
        weightedY += here.Y * volumeHere;
        weightedZ += here.Z * volumeHere;
        totalVolume += volumeHere;
    }

    if (totalVolume > 0)
    {
        combined = new XYZ(weightedX / totalVolume,
                           weightedY / totalVolume,
                           weightedZ / totalVolume);
    }

    findings.Add(string.Format(
        "{0} centroid(s) computed, {1} element(s) have no solid geometry and so "
        + "have no centroid at all. The combined point is weighted BY VOLUME, "
        + "which makes it a centre of mass only where the material is the same "
        + "throughout", centroidOf.Count, noSolid.Count));
}
