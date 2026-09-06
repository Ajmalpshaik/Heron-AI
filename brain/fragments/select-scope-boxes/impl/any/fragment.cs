// NOT STANDALONE. Assumes `doc` and `nameContains` are in scope; leaves
// `elements`, `matchedNames`, `scanned` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// FOUND BY CATEGORY, NOT BY CLASS. A scope box has no dedicated API class - it
// is an ordinary element in the Volume of Interest category. Looking for a
// ScopeBox type finds nothing at all, which is the most likely wrong turn here.
//
// IT REPORTS NO EXTENTS. READ_SCOPE_BOX_EXTENT already answers "how big"; this
// answers "which". Two fragments answering one question is how two answers
// drift apart.

var elements = new List<Element>();
var matchedNames = new List<string>();
var scanned = 0;

var needle = string.IsNullOrEmpty(nameContains) ? null : nameContains;

foreach (var candidate in new FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
             .WhereElementIsNotElementType())
{
    if (candidate == null) continue;

    scanned++;

    var name = candidate.Name;

    if (needle != null)
    {
        if (string.IsNullOrEmpty(name)) continue;
        if (name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
    }

    elements.Add(candidate);
    matchedNames.Add(string.IsNullOrEmpty(name) ? "(no name)" : name);
}

var found = elements.Count;
