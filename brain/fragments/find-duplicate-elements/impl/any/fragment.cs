// NOT STANDALONE. Assumes `elements` and `tolerance` are in scope, and leaves
// `duplicateOf`, `duplicateGroups` and `unplaceable` behind.
//
// READ ONLY. Opens no transaction and deletes nothing - see the last note.
//
// A DUPLICATE IS INVISIBLE AND IT DOUBLES A SCHEDULE.
//
// Two air terminals in the same spot draw as ONE and count as TWO. Nobody sees
// it on the drawing; everybody sees it in the quantity, and by then it is in a
// tender.
//
// SAME PLACE IS A TOLERANCE, NOT AN EQUALITY. Two elements pasted at the same
// point can differ in the tenth decimal, and an exact comparison finds nothing.
//
// SAME PLACE IS NOT ENOUGH. A diffuser and a sprinkler at the same point are
// coordinated, not duplicated. Only elements of the SAME TYPE are paired.
//
// THE LOCATION IS TAKEN THREE WAYS. A point for anything placed, the midpoint
// for a linear run, the box centre for everything else. Without the last two a
// duplicated DUCT reports nothing - and that is the most common duplicate there
// is, because a duct has no insertion point.
//
// IT DELETES NOTHING. Which of two identical elements to keep is not something
// geometry can settle: one of them may be the one that is tagged, grouped or
// connected.

var duplicateOf = new Dictionary<ElementId, ElementId>();
var unplaceable = new List<ElementId>();
int duplicateGroups = 0;

Func<Element, XYZ> placeOf = element =>
{
    try
    {
        var point = element.Location as LocationPoint;
        if (point != null) return point.Point;

        var curve = element.Location as LocationCurve;
        if (curve != null && curve.Curve != null) return curve.Curve.Evaluate(0.5, true);

        var box = element.get_BoundingBox(null);
        if (box != null) return (box.Min + box.Max) * 0.5;
    }
    catch { }
    return null;
};

// Type and place, gathered once each.
var placed = new List<KeyValuePair<Element, XYZ>>();
foreach (var element in elements)
{
    if (element == null) continue;
    var where = placeOf(element);
    if (where == null) { unplaceable.Add(element.Id); continue; }
    placed.Add(new KeyValuePair<Element, XYZ>(element, where));
}

var claimed = new HashSet<ElementId>();

for (int i = 0; i < placed.Count; i++)
{
    var first = placed[i].Key;
    if (claimed.Contains(first.Id)) continue;

    ElementId firstType = ElementId.InvalidElementId;
    try { firstType = first.GetTypeId(); } catch { }

    bool startedGroup = false;

    for (int j = i + 1; j < placed.Count; j++)
    {
        var second = placed[j].Key;
        if (claimed.Contains(second.Id)) continue;

        ElementId secondType = ElementId.InvalidElementId;
        try { secondType = second.GetTypeId(); } catch { }
        if (secondType != firstType) continue;

        if (placed[i].Value.DistanceTo(placed[j].Value) > tolerance) continue;

        // The FIRST of a group is the one the others point at, so a group of
        // three reports two duplicates rather than three - which is the number
        // that would actually be removed.
        duplicateOf[second.Id] = first.Id;
        claimed.Add(second.Id);
        if (!startedGroup) { duplicateGroups++; startedGroup = true; }
    }
}
