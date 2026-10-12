// NOT STANDALONE. Assumes `doc` and `elementIds` are already in scope, and
// leaves `elements`, `missing` and - since version 2 - `named` in scope for
// whatever is composed after it.
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

// VERSION 2 (FRAGMENT-ISSUES 5b-441): WHAT EACH ELEMENT IS, IN ONE STRING.
//
// Ids may now be TYPED - the binder matches them as text in this document and
// hands them here like any others - and a digit typed wrong names a different
// element with nothing about it looking wrong. So the answer says what every
// element is: its id, its category, and "Family: Type", the way the
// Properties palette writes it. A STRING, because a reply cuts a list to three
// items and prints an element by its name alone - "[Tees, Tees, Tees, ...]" -
// which is exactly what cannot show a wrong id. Read it BEFORE a change uses
// these elements through expect_from.
//
// "Family: Type" from the element's own type, which is the FamilySymbol for a
// family instance and the ElementType for a wall or a duct. An element with no
// type - a view, a level, a type itself - is named by its own name.

Func<Element, string> describeOne = element =>
{
    string category = null;
    try { category = element.Category == null ? null : element.Category.Name; } catch { }

    string what = null;
    try
    {
        var kind = doc.GetElement(element.GetTypeId()) as ElementType;
        if (kind != null)
            what = string.IsNullOrEmpty(kind.FamilyName) ? kind.Name : kind.FamilyName + ": " + kind.Name;
    }
    catch { }
    if (string.IsNullOrEmpty(what))
    {
        try { what = element.Name; } catch { }
    }

    return element.Id.ToString() + " "
        + (string.IsNullOrEmpty(category) ? "(no category)" : category)
        + (string.IsNullOrEmpty(what) ? "" : " - \"" + what + "\"");
};

var described = new List<string>();
foreach (var element in elements) described.Add(describeOne(element));
var named = (elements.Count == 0
        ? "no element"
        : elements.Count + " element(s): " + string.Join("; ", described))
    + (missing.Count == 0 ? "" : ". " + missing.Count + " id(s) named nothing - see `missing`");
