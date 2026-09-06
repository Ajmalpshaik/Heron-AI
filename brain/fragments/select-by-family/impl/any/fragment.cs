// NOT STANDALONE. Assumes `doc`, `familyName`, `exactMatch` and `categories`
// are in scope, and leaves `elements`, `matchedFamilies`, `scannedWholeModel`
// and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE CATEGORY IS OPTIONAL BECAUSE IT IS OFTEN THE UNKNOWN.
//
// The same family is loaded as a duct accessory on one job and as mechanical
// equipment on the next. Naming the category first is exactly what makes the
// search come back empty and read as "that family is not in this model". An
// empty category list therefore means the whole document - and says so,
// because an unbounded walk is fine once and expensive in a loop.
//
// TWO PLACES A FAMILY NAME LIVES.
//
//   a loadable family   the instance's symbol knows its family
//   a system family     Basic Wall, Rectangular Duct - the element's TYPE
//                       carries the family name and there is no family element
//
// Reading only the first misses every wall, duct and pipe, which are exactly
// the things somebody asks for by name.
//
// WHAT MATCHED IS REPORTED, NOT JUST HOW MANY.
//
// Family names on a real job carry prefixes and revision suffixes, so the
// default match is by contained text - and a contains match can catch more
// than was meant. The families it actually hit, with a count each, is what
// makes that visible instead of leaving a number nobody can check.

var elements = new List<Element>();
var matchedFamilies = new Dictionary<string, int>();
bool scannedWholeModel = categories == null || categories.Count == 0;

string wanted = (familyName ?? "").Trim();

Func<string, bool> matches = name =>
{
    if (string.IsNullOrEmpty(name) || wanted.Length == 0) return false;
    return exactMatch
        ? string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)
        : name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
};

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

if (!scannedWholeModel)
{
    var wantedCategories = new List<ElementId>();
    foreach (var category in categories) wantedCategories.Add(new ElementId(category));
    if (wantedCategories.Count == 1)
        collector = collector.OfCategoryId(wantedCategories[0]);
    else
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories.ToList()));
}

foreach (var element in collector)
{
    if (element == null) continue;

    string family = null;

    // A loadable family: the instance's own symbol names it.
    var instance = element as FamilyInstance;
    if (instance != null)
    {
        try
        {
            if (instance.Symbol != null && instance.Symbol.Family != null)
                family = instance.Symbol.Family.Name;
        }
        catch { }
    }

    // A system family: no family element exists, and the type carries the
    // name. Walls, ducts, pipes and every other run live here.
    if (family == null)
    {
        try
        {
            var type = doc.GetElement(element.GetTypeId()) as ElementType;
            if (type != null) family = type.FamilyName;
        }
        catch { }
    }

    if (family == null || !matches(family)) continue;

    elements.Add(element);
    if (matchedFamilies.ContainsKey(family)) matchedFamilies[family] = matchedFamilies[family] + 1;
    else matchedFamilies[family] = 1;
}

int found = elements.Count;
