// NOT STANDALONE. Assumes `doc` and `elementIds` are already in scope, and
// leaves `elements` and `missing` in scope for whatever is composed after it.
//
// WHY A MISSING ID IS REPORTED AND NOT SKIPPED.
//
// GetElement returns null for an id that no longer resolves - deleted, undone,
// or belonging to a different document. Dropping those quietly is the same
// failure `unresolvedLevel` exists to prevent one fragment over: the caller
// receives a shorter list and no reason for it, and a trace built on it reports
// a smaller system rather than a stale input.
//
// So the two outcomes are kept apart and both are handed on. "12 of 15 found"
// is an answer. "12 found" is a guess wearing an answer's clothes.
//
// AN ID FROM ANOTHER DOCUMENT IS THE CASE WORTH KNOWING ABOUT. It does not
// throw - it simply resolves to nothing, or worse, resolves to an unrelated
// element that happens to share the number. Ids are per-document and carry no
// document identity of their own, which is why Heron pins the document for a
// conversation (Golden Rule 20) rather than trusting an id to be about the
// model in front of the user.

var elements = new List<Element>();
var missing = new List<ElementId>();

foreach (var id in elementIds)
{
    if (id == null || id == ElementId.InvalidElementId)
    {
        // InvalidElementId is not a lookup that failed - it is a value that was
        // never an element. Recorded all the same, because a caller sending one
        // has a bug upstream and a silent skip hides it.
        missing.Add(ElementId.InvalidElementId);
        continue;
    }

    var element = doc.GetElement(id);
    if (element == null) missing.Add(id);
    else elements.Add(element);
}
