// NOT STANDALONE. Assumes `doc`, `elements`, `direction`, `maxDistance` and
// `evenness` are in scope; leaves `findings`, `unsafeToMove` and `clean` behind.
//
// READ ONLY. Opens no transaction and needs none. Distances are internal FEET.
//
// A SINGLE CENTRE RAY IS RIGHT IN THE MIDDLE OF A LARGE FLAT SURFACE AND LIES
// AT THE EDGES - and edges are where the mistakes are. Same arithmetic, five
// sample points, and four named ways one ray gets it wrong:
//   STRADDLING  - corners hit DIFFERENT elements: it spans an edge
//   OVERHANGING - some corners hit nothing: it hangs off
//   UNEVEN      - same element, different distances
//   SLOPED      - the surface is not square to the ray, so nothing sits flush
//
// IT IS THE CHECK BEFORE A BATCH MOVE. A move that includes a straddling unit
// puts it in the wrong place confidently.
//
// REPORTS BY EXCEPTION. Ray count is cheap; output is not. A clean run over
// thousands should print a summary line and nothing else.
//
// LINKED MODELS ARE NOT HIT. If the ceilings and slabs are a link, everything
// reports as finding nothing, which looks exactly like a real finding.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
maxDistance = maxDistance / MillimetresPerFoot;

var findings = new List<string>();
var unsafeToMove = new List<ElementId>();
var clean = 0;

View3D rayView = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
{
    var candidate = element as View3D;
    if (candidate != null && !candidate.IsTemplate) { rayView = candidate; break; }
}

if (rayView == null)
{
    findings.Add("no 3D view in this model, so no ray could be cast and NOTHING was checked. That is "
        + "not the same as every element passing");
}
else
{
    // THE TWO-ARGUMENT CONSTRUCTOR DOES NOT EXIST ON ANY RELEASE HERE. This
    // read `new ReferenceIntersector(FindReferenceTarget.Element, rayView)`
    // until 2026-09-05, and failed to compile on all EIGHT - it is not a
    // version split, it is a signature Revit has never shipped. The
    // filter-first overload is the one that exists, so a filter has to be
    // given; an inverted ElementIsElementTypeFilter passes every placed
    // instance and no types, which is what "anything behind it" means.
    var intersector = new ReferenceIntersector(
        new ElementIsElementTypeFilter(true), FindReferenceTarget.Element, rayView);
    var unit = direction.Normalize();

    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        var box = element.get_BoundingBox(null);
        if (box == null)
        {
            findings.Add(string.Format("{0} (id {1}): no readable extent - not checked",
                element.Name, element.Id));
            continue;
        }

        // Five sample points across the footprint: centre and four corners,
        // pulled in slightly so a corner exactly on an edge does not decide the
        // verdict by a rounding error.
        var centre = (box.Min + box.Max) / 2.0;
        var insetX = (box.Max.X - box.Min.X) * 0.45;
        var insetY = (box.Max.Y - box.Min.Y) * 0.45;

        var samples = new List<XYZ>
        {
            centre,
            new XYZ(centre.X - insetX, centre.Y - insetY, centre.Z),
            new XYZ(centre.X + insetX, centre.Y - insetY, centre.Z),
            new XYZ(centre.X + insetX, centre.Y + insetY, centre.Z),
            new XYZ(centre.X - insetX, centre.Y + insetY, centre.Z)
        };

        var hitIds = new List<ElementId>();
        var distances = new List<double>();
        var missed = 0;

        foreach (var point in samples)
        {
            ReferenceWithContext found = null;
            try { found = intersector.FindNearest(point, unit); }
            catch { }

            if (found == null || found.Proximity > maxDistance) { missed++; continue; }

            var reference = found.GetReference();
            if (reference != null) hitIds.Add(reference.ElementId);
            distances.Add(found.Proximity);
        }

        var verdicts = new List<string>();

        if (missed == samples.Count)
        {
            findings.Add(string.Format("{0} (id {1}): NOTHING within reach in that direction",
                element.Name, element.Id));
            unsafeToMove.Add(element.Id);
            continue;
        }

        if (missed > 0) verdicts.Add(string.Format("OVERHANGING - {0} of {1} sample points hit nothing",
            missed, samples.Count));

        // Different elements under different corners: it spans an edge.
        var distinct = new List<ElementId>();
        foreach (var id in hitIds)
        {
            var seen = false;
            foreach (var known in distinct) { if (known == id) { seen = true; break; } }
            if (!seen) distinct.Add(id);
        }
        if (distinct.Count > 1)
        {
            verdicts.Add(string.Format("STRADDLING - the corners land on {0} different elements",
                distinct.Count));
        }

        if (distances.Count > 1)
        {
            var lowest = distances[0];
            var highest = distances[0];
            foreach (var value in distances)
            {
                if (value < lowest) lowest = value;
                if (value > highest) highest = value;
            }
            var spread = highest - lowest;
            if (spread > evenness)
            {
                // One surface at different distances across the footprint is
                // either a slope or a step; both mean it cannot sit flush.
                verdicts.Add(string.Format("{0} - the surface is {1:0.#} mm further away at one corner "
                    + "than another", distinct.Count == 1 ? "SLOPED or UNEVEN" : "UNEVEN",
                    spread * 304.8));
            }
        }

        if (verdicts.Count == 0)
        {
            clean++;
        }
        else
        {
            unsafeToMove.Add(element.Id);
            findings.Add(string.Format("{0} (id {1}): {2}", element.Name, element.Id,
                string.Join("; ", verdicts)));
        }
    }

    findings.Insert(0, string.Format("{0} element(s): {1} sit flat, {2} do NOT and should not be moved "
        + "automatically. Reported BY EXCEPTION - anything not listed is clean. LINKED models were not "
        + "hit, so if the ceilings and slabs are a link this has checked nothing",
        elements.Count, clean, unsafeToMove.Count));
}
