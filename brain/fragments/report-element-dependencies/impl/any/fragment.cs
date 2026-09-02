// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `dependents`, `joined`, `undeletable` and `standalone` behind.
//
// READ ONLY. Opens no transaction and changes nothing - which is the point:
// the question is asked BEFORE the change, not discovered after it.
//
// THE LIST IS REVIT'S OWN.
//
// `GetDependentElements(null)` - null meaning "no filter, everything" - returns
// what Revit itself treats as attached, the same set it removes on delete. It
// reaches things nothing here could derive: a tag referencing the element, a
// dimension witnessed on it, a family hosted in it, the sketch a floor is drawn
// from, an opening cut through it.
//
// IT ALWAYS CONTAINS THE ELEMENT ITSELF.
//
// Not a defect, and not something to strip blindly - it is why the count reads
// one higher than people expect. It is removed here by identity and the
// remainder reported, so the number is never quietly wrong in either direction.
//
// "CAN I DELETE IT" IS A DIFFERENT QUESTION.
//
// An element with zero dependents can still be undeletable: pinned, the last
// level, owned by another user in a workshared model. `CanDeleteElement` is
// Revit's own answer and is asked separately. Reporting only the dependents
// would say "nothing attached, safe to remove" about something that cannot be
// removed at all.
//
// JOINED IS NOT DEPENDENT, AND MERGING THEM WOULD LIE TWICE.
//
// Revit does not delete a joined neighbour - it re-cleans the geometry. A wall
// joined to three others changes SHAPE when one goes, in a drawing nobody was
// looking at. Listing it as a dependent would say it gets deleted, which is
// false; leaving it out would say nothing happens to it, which is also false.
//
// `GetJoinedElements` THROWS for an element that cannot be joined at all - a
// tag, a view, an annotation - rather than returning an empty list. On a mixed
// selection that is the difference between a report and a crash, so it is
// guarded per element.

var dependents = new Dictionary<ElementId, IList<ElementId>>();
var joined = new Dictionary<ElementId, IList<ElementId>>();
var undeletable = new List<ElementId>();
var standalone = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;
    var id = element.Id;

    var attached = new List<ElementId>();
    try
    {
        var reported = element.GetDependentElements(null);
        if (reported != null)
        {
            foreach (var dependentId in reported)
            {
                // The element itself is always in there. Excluded by identity,
                // not by trimming the first entry - the order is not promised.
                if (dependentId == id) continue;
                attached.Add(dependentId);
            }
        }
    }
    catch { }

    dependents[id] = attached;

    var joinedTo = new List<ElementId>();
    try
    {
        var neighbours = JoinGeometryUtils.GetJoinedElements(doc, element);
        if (neighbours != null) joinedTo.AddRange(neighbours);
    }
    catch { }

    if (joinedTo.Count > 0) joined[id] = joinedTo;

    bool canDelete = false;
    try { canDelete = DocumentValidation.CanDeleteElement(doc, id); } catch { }
    if (!canDelete) undeletable.Add(id);

    // Nothing attached, nothing joined, and Revit will allow it. Said as its
    // own fact rather than left to be inferred from three empty collections,
    // because that inference is exactly where "I checked" and "I found nothing
    // to check" get confused.
    if (canDelete && attached.Count == 0 && joinedTo.Count == 0) standalone.Add(id);
}
