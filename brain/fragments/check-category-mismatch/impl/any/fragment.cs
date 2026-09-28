// NOT STANDALONE. Assumes `doc`, `nameKeywords`, `expectedCategory` and
// `includeLinks` are in scope, and leaves `elements`, `foundIn`,
// `correctlyCategorised`, `scanned`, `linksSearched` and `linkedMatches`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE NAME CARRIES THE INTENT; THE CATEGORY WAS PICKED FROM A LIST.
//
// Somebody named the family for what the thing IS. The category is a choice
// made when the family was built, sometimes wrongly and sometimes because the
// right one was not available at the time. So the name is the evidence and the
// category is what is being tested against it - which is also why the keywords
// have to come from the caller: every job names its kit differently, and a
// built-in word list would be one office's vocabulary applied to everybody
// else's model.
//
// BOTH NAMES ARE READ.
//
// Family name and type name. A family named neutrally often has the meaning in
// its type name, and reading only one halves what this finds.
//
// WHERE THEY ARE IS PART OF THE ANSWER.
//
// Eleven in the wrong category is half a finding. Seven in generic models and
// four in casework says whether this was one family made wrongly or several
// unrelated mistakes - which decides whether the fix is one family or a
// morning.
//
// A WHOLE-MODEL WALK, ON PURPOSE. The elements being looked for are by
// definition not in the category anybody would filter by first.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. A door modelled as a generic model in the architect's link breaks a
// door schedule and a clearance check just as surely as one here. With
// `includeLinks` set, each loaded link is walked the same way and reported as
// TEXT in `linkedMatches`, one line per link: how many are wrong, where they
// actually are, and how many were right. `elements`, `foundIn` and the two
// counts stay this model's own: the chain revives ids against the HOST
// document, so a linked id carried there binds an unrelated element silently
// (FRAGMENT-ISSUES row 75). NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS
// THEM. Only model elements are read, never a link's views or sheets.

var elements = new List<Element>();
var foundIn = new Dictionary<string, int>();
int correctlyCategorised = 0;
int scanned = 0;

var expectedId = new ElementId(expectedCategory);

Func<string, bool> saysSo = name =>
{
    if (string.IsNullOrEmpty(name) || nameKeywords == null) return false;
    foreach (var keyword in nameKeywords)
    {
        if (string.IsNullOrEmpty(keyword)) continue;
        if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
    }
    return false;
};

foreach (var element in new FilteredElementCollector(doc)
             .OfClass(typeof(FamilyInstance))
             .WhereElementIsNotElementType())
{
    var instance = element as FamilyInstance;
    if (instance == null) continue;
    scanned++;

    string familyName = null, typeName = null;
    try
    {
        if (instance.Symbol != null)
        {
            typeName = instance.Symbol.Name;
            if (instance.Symbol.Family != null) familyName = instance.Symbol.Family.Name;
        }
    }
    catch { }

    if (!saysSo(familyName) && !saysSo(typeName)) continue;

    ElementId actual = ElementId.InvalidElementId;
    string actualName = "(no category)";
    try
    {
        if (instance.Category != null)
        {
            actual = instance.Category.Id;
            actualName = instance.Category.Name ?? actualName;
        }
    }
    catch { }

    if (actual == expectedId) { correctlyCategorised++; continue; }

    elements.Add(instance);
    if (foundIn.ContainsKey(actualName)) foundIn[actualName] = foundIn[actualName] + 1;
    else foundIn[actualName] = 1;
}

// ---- D-59: the same walk, in each link -------------------------------------

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;

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

var linkBlocked = "";

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var linkWrong = 0;
    var linkRight = 0;
    var linkScanned = 0;
    var linkFoundIn = new Dictionary<string, int>();
    try
    {
        foreach (var element in new FilteredElementCollector(linked)
                     .OfClass(typeof(FamilyInstance))
                     .WhereElementIsNotElementType())
        {
            var instance = element as FamilyInstance;
            if (instance == null) continue;
            linkScanned++;

            string familyName = null, typeName = null;
            try
            {
                if (instance.Symbol != null)
                {
                    typeName = instance.Symbol.Name;
                    if (instance.Symbol.Family != null) familyName = instance.Symbol.Family.Name;
                }
            }
            catch { }
            if (!saysSo(familyName) && !saysSo(typeName)) continue;

            // BuiltInCategory ids are the same in every document, so the
            // expected category is compared the same way as in the host.
            ElementId actual = ElementId.InvalidElementId;
            string actualName = "(no category)";
            try
            {
                if (instance.Category != null)
                {
                    actual = instance.Category.Id;
                    actualName = instance.Category.Name ?? actualName;
                }
            }
            catch { }

            if (actual == expectedId) { linkRight++; continue; }
            linkWrong++;
            linkFoundIn[actualName] = linkFoundIn.ContainsKey(actualName) ? linkFoundIn[actualName] + 1 : 1;
        }
    }
    catch (Exception) { }

    var parts = new List<string>();
    foreach (var entry in linkFoundIn) parts.Add(entry.Key + " " + entry.Value);

    linksSearched++;
    linkedTotal += linkWrong;
    linkedMatches.Add(string.Format("{0}: {1} named like it but in the wrong category{2}; {3} in the "
        + "right one; {4} family instance(s) scanned", linked.Title, linkWrong,
        parts.Count > 0 ? " (" + string.Join(", ", parts.ToArray()) + ")" : "",
        linkRight, linkScanned));
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
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} in the wrong category, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
