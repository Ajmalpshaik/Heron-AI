// NOT STANDALONE. Assumes `doc`, `familyName`, `exactMatch`, `categories` and
// `includeLinks` are in scope, and leaves `elements`, `matchedFamilies`,
// `scannedWholeModel`, `found`, `linksSearched` and `linkedMatches` behind.
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
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements`: that list feeds the next
// fragment in a chain, and the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// A TYPE IS LOOKED UP IN THE ELEMENT'S OWN DOCUMENT, not in `doc` - a linked
// element's type id belongs to the link. For a host element the two are the
// same document.

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
            var type = element.Document.GetElement(element.GetTypeId()) as ElementType;
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

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

if (wanted.Length == 0 && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no family was named";

for (var i = 0; i < linkDocs.Count && linkBlocked.Length == 0; i++)
{
    var linked = linkDocs[i];
    var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
    if (!scannedWholeModel)
        linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories.ToList()));

    var byFamily = new Dictionary<string, int>();
    var count = 0;

    foreach (var element in linkedCollector)
    {
        if (element == null) continue;

        string family = null;
        var instance = element as FamilyInstance;
        if (instance != null)
        {
            try
            {
                if (instance.Symbol != null && instance.Symbol.Family != null)
                    family = instance.Symbol.Family.Name;
            }
            catch (Exception) { }
        }
        if (family == null)
        {
            try
            {
                var type = linked.GetElement(element.GetTypeId()) as ElementType;
                if (type != null) family = type.FamilyName;
            }
            catch (Exception) { }
        }

        if (family == null || !matches(family)) continue;
        count++;
        byFamily[family] = byFamily.ContainsKey(family) ? byFamily[family] + 1 : 1;
    }

    var parts = new List<string>();
    foreach (var entry in byFamily) parts.Add(entry.Key + " " + entry.Value);

    linksSearched++;
    linkedTotal += count;
    linkedMatches.Add(string.Format("{0}: {1}{2}", linked.Title, count,
        parts.Count > 0 ? " (" + string.Join(", ", parts.ToArray()) + ")" : ""));
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} match(es), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
