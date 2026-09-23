// NOT STANDALONE. Assumes `doc`, `elements` and `against` are in scope; leaves
// `clashes`, `clashing`, `clear`, `notChecked` and `findings` behind.
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
// ===========================================================================
// A TEST THAT COULD NOT BE RUN IS NOT A CLEAR RESULT.
// ===========================================================================
//
// Revit's filter THROWS for some elements rather than returning nothing - a
// view, a group, an element whose geometry is not generated in the active
// context. Until 2026-09-23 those were counted CLEAR, and this header said that
// was true because "nothing was found to intersect them". Nothing was LOOKED
// FOR. A clash report that calls an element clear when it never tested it is
// the one line a coordination meeting acts on without checking - and the same
// trap was observed elsewhere, in another tool's clash check.
//
// So they are still stepped over - a batch of two hundred must not end at the
// eleventh - but they are named in `notChecked`, with Revit's reason, and
// clashing + clear + notChecked is the number handed in.
//
// NOTHING TO CHECK AGAINST IS NOT CLEAR EITHER. An empty `against` used to count
// every element clear. It now counts none, and says why.
//
// TOUCHING COUNTS AS INTERSECTING and that is left alone deliberately. A duct
// resting exactly on a beam soffit is a hit geometrically and is usually fine
// on site. No tolerance is invented here, because a number invented in a
// fragment is a number quietly relied on everywhere afterwards.

var clashes = new Dictionary<ElementId, ICollection<ElementId>>();
var clashing = 0;
var clear = 0;
var notChecked = new List<ElementId>();
var findings = new List<string>();

// Why an element could not be tested, counted by reason, so two hundred
// elements of one kind Revit refuses are one sentence and not two hundred.
var whyNot = new Dictionary<string, int>();
var handed = 0;

if (against == null || against.Count == 0)
{
    foreach (var element in elements) if (element != null) handed++;
    findings.Add(string.Format("Nothing was given to check against, so NOTHING WAS CHECKED - none of "
        + "the {0} element(s) is clear and none clashes. Hand in the second set.", handed));
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (clashes.ContainsKey(element.Id)) continue;
        handed++;

        List<ElementId> hit;
        try
        {
            var filter = new ElementIntersectsElementFilter(element);
            hit = new FilteredElementCollector(doc, against)
                .WherePasses(filter)
                .ToElementIds()
                .ToList();
        }
        catch (Exception ex)
        {
            // Not tested, so neither clear nor clashing. See the header.
            notChecked.Add(element.Id);
            var reason = string.IsNullOrEmpty(ex.Message) ? ex.GetType().Name : ex.Message;
            var times = 0;
            whyNot.TryGetValue(reason, out times);
            whyNot[reason] = times + 1;
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

    findings.Add(string.Format("{0} element(s) checked against {1}: {2} clash, {3} clear, {4} could not "
        + "be tested", handed, against.Count, clashing, clear, notChecked.Count));

    foreach (var why in whyNot)
    {
        findings.Add(string.Format("{0} element(s) NOT CHECKED - Revit's intersection test refused them: "
            + "{1}. They are in `notChecked` and are NOT counted clear", why.Value, why.Key));
    }
}
