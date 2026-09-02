// NOT STANDALONE. Assumes `doc`, `elements` and `against` are in scope; leaves
// `clashes`, `clashing` and `clear` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// REVIT'S OWN SOLID INTERSECTION, NOT BOUNDING BOXES. A bounding box around a
// diagonal duct overlaps everything in the rectangle it spans, and a clash
// report built that way runs to hundreds of rows that are not clashes - which
// is how a coordination report stops being read. `ElementIntersectsElementFilter`
// tests the real geometry.
//
// ONE COLLECTOR PASS PER ELEMENT, over the OTHER SET ONLY. The collector is
// constructed from `against` rather than from the whole document, so the work
// is |elements| x |against| and not |elements| x |model|. On a real model that
// is the difference between a check somebody runs and one they do not.
//
// AN ELEMENT WITH NO GEOMETRY CANNOT CLASH AND IS COUNTED AS CLEAR. Revit's
// filter throws for some elements rather than returning nothing - a view, a
// group, an element whose geometry is not generated in the active context.
// Those are stepped over so a batch of two hundred does not end at the
// eleventh, and they land in `clear`, which is true: nothing was found to
// intersect them.
//
// TOUCHING COUNTS AS INTERSECTING and that is left alone deliberately. A duct
// resting exactly on a beam soffit is a hit geometrically and is usually fine
// on site. No tolerance is invented here, because a number invented in a
// fragment is a number quietly relied on everywhere afterwards.

var clashes = new Dictionary<ElementId, ICollection<ElementId>>();
var clashing = 0;
var clear = 0;

foreach (var element in elements)
{
    if (element == null) continue;
    if (clashes.ContainsKey(element.Id)) continue;

    if (against == null || against.Count == 0)
    {
        clear++;
        continue;
    }

    List<ElementId> hit;
    try
    {
        var filter = new ElementIntersectsElementFilter(element);
        hit = new FilteredElementCollector(doc, against)
            .WherePasses(filter)
            .ToElementIds()
            .ToList();
    }
    catch (Exception)
    {
        // No usable geometry. Recorded as clear rather than skipped: the
        // element was checked, nothing was found, and dropping it would make
        // the two counts disagree with the list that was handed in.
        clear++;
        continue;
    }

    // An element is in both sets on an overlapping request. Intersecting
    // itself is not a clash, and reporting it would put a row in every
    // coordination report that nobody can act on.
    hit.Remove(element.Id);

    if (hit.Count == 0)
    {
        clear++;
        continue;
    }

    clashes[element.Id] = hit;
    clashing++;
}
