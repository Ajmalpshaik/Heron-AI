// NOT STANDALONE. Assumes `doc`, `categoryName` and `inViewOnly` are in scope,
// and leaves `elements`, `resolvedTo`, `nearMisses` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE WORDS ARE WHAT ARRIVES.
//
// A request comes in somebody's own language, and turning "duct accessories"
// into the right internal category is a guess whose wrong answer looks right -
// the identifier is not the obvious one, and a near miss silently selects a
// different category.
//
// A NAME THAT MATCHES NOTHING RETURNS THE NEAR MISSES.
//
// "Duct Accessory" singular is what a person types. Handing back the category
// names that nearly matched turns a dead end into one more sentence; guessing
// which was meant is what this refuses to do.
//
// AND THE NAMES ARE LOCALISED.
//
// On a French or Arabic Revit the names are in that language and the internal
// identifiers are not. This fragment is the one that works there; the
// identifier route is the one that works everywhere. Both are worth having.

var elements = new List<Element>();
string resolvedTo = "";
var nearMisses = new List<string>();

string wanted = (categoryName ?? "").Trim();

Category match = null;
var everyName = new List<string>();

try
{
    foreach (Category category in doc.Settings.Categories)
    {
        if (category == null) continue;
        string name = "";
        try { name = category.Name ?? ""; } catch { }
        if (name.Length == 0) continue;
        everyName.Add(name);

        if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)) match = category;
    }
}
catch { }

if (match == null && wanted.Length > 0)
{
    // Near misses: a category whose name contains the words, or whose words
    // are contained in it. Enough to answer with a question rather than a
    // dead end.
    foreach (var name in everyName)
    {
        if (name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0 ||
            wanted.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            nearMisses.Add(name);
    }
}

if (match != null)
{
    resolvedTo = match.Name;

    var collector = inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc);

    try
    {
        foreach (var element in collector
                     .WhereElementIsNotElementType()
                     .OfCategoryId(match.Id))
            if (element != null) elements.Add(element);
    }
    catch { }
}

int found = elements.Count;
