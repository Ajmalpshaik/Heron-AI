// NOT STANDALONE. Assumes `doc`, `categories`, `familyNameContains` and
// `typeNameContains` are in scope; leaves `elements`, `matchedNames`,
// `scanned` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THIS RETURNS TYPES, NOT INSTANCES, AND THAT IS THE WHOLE POINT. Every other
// route to a type here starts from a placed element and asks what type it is,
// which cannot see a type that has been loaded and never used - the one
// somebody wants to rename before it spreads, or check against a standard.
//
// `categories` NULL OR EMPTY MEANS EVERY CATEGORY. That is the honest answer
// to "what types are in this model" rather than a missing input, so it is
// allowed rather than refused. The count is reported so a request that meant
// one category and passed none is visible as an absurd number.
//
// FAMILY NAME IS TWO DIFFERENT QUESTIONS:
//
//   FamilySymbol       a loadable family - the family is an OBJECT
//   every other type   a system family - duct, pipe, wall - which carries
//                      only a NAME and has no family object at all
//
// Asking a system family type for a Family gets nothing, and a filter that
// only matched the loadable case would report zero for a duct type whose
// family is sitting in the browser. Both are matched.

var elements = new List<Element>();
var matchedNames = new List<string>();

// Resolved categories in, ids out. Never a name in a string, and never an id
// read as a number - ElementId is compared to ElementId throughout.
var wantedCategoryIds = new HashSet<ElementId>();
if (categories != null)
{
    foreach (var category in categories)
    {
        if (category == null) continue;
        wantedCategoryIds.Add(category.Id);
    }
}

var familyNeedle = string.IsNullOrEmpty(familyNameContains) ? null : familyNameContains;
var typeNeedle = string.IsNullOrEmpty(typeNameContains) ? null : typeNameContains;

var scanned = 0;

foreach (var candidate in new FilteredElementCollector(doc).WhereElementIsElementType())
{
    var elementType = candidate as ElementType;
    if (elementType == null) continue;

    scanned++;

    if (wantedCategoryIds.Count > 0)
    {
        var category = elementType.Category;
        // A type with no category cannot match a category that was asked for.
        // It is skipped rather than treated as a wildcard, which is how an
        // internal type ends up in a rename batch.
        if (category == null) continue;
        if (!wantedCategoryIds.Contains(category.Id)) continue;
    }

    var typeName = elementType.Name;
    if (typeNeedle != null)
    {
        if (string.IsNullOrEmpty(typeName)) continue;
        if (typeName.IndexOf(typeNeedle, StringComparison.OrdinalIgnoreCase) < 0) continue;
    }

    // The two ways a type carries a family, both answered. A FamilySymbol
    // whose Family cannot be read falls back to the name property rather than
    // dropping out - a type that exists is reported one way or another.
    string familyName = null;
    var familySymbol = elementType as FamilySymbol;
    if (familySymbol != null && familySymbol.Family != null)
    {
        familyName = familySymbol.Family.Name;
    }
    if (string.IsNullOrEmpty(familyName))
    {
        familyName = elementType.FamilyName;
    }

    if (familyNeedle != null)
    {
        if (string.IsNullOrEmpty(familyName)) continue;
        if (familyName.IndexOf(familyNeedle, StringComparison.OrdinalIgnoreCase) < 0) continue;
    }

    elements.Add(elementType);
    matchedNames.Add(
        (string.IsNullOrEmpty(familyName) ? "(no family)" : familyName)
        + " : "
        + (string.IsNullOrEmpty(typeName) ? "(no name)" : typeName));
}

var found = elements.Count;
