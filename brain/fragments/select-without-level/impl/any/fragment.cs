// NOT STANDALONE. Assumes `doc`, `categories` and `includeLinks` are in scope;
// leaves `elements`, `examined`, `findings`, `linksSearched` and
// `linkedMatches`.
//
// A READ. It opens no transaction and needs none.
//
// SELECT_BY_LEVEL COUNTS THESE AND DROPS THEM - its `unresolvedLevel` is an int.
// This returns the set instead, which is the whole difference.
//
// THE LOOKUP HAS FOUR STEPS AND THE LAST ONE MATTERS MOST. An MEP curve keeps
// its level ONLY on RBS_START_LEVEL_PARAM and has no LevelId at all, so a lookup
// that stops early reports every duct in the model as unlevelled.
//
// MOST OF WHAT HAS NO LEVEL SHOULD HAVE NONE. Views, sheets, materials and
// annotation are all correct answers and all useless ones, which is why the
// category list is what makes this mean anything.
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

var elements = new List<Element>();
var examined = 0;
var findings = new List<string>();

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

if (categories != null && categories.Count > 0)
{
    var wanted = new List<BuiltInCategory>();
    foreach (var category in categories) wanted.Add(category);
    collector = collector.WherePasses(new ElementMulticategoryFilter(wanted));
}

foreach (var element in collector)
{
    if (element == null) continue;
    examined++;

    var levelId = ElementId.InvalidElementId;

    // 1. A wall keeps it here.
    var wall = element as Wall;
    if (wall != null) levelId = wall.LevelId;

    // 2. Most elements keep it here.
    if (levelId == ElementId.InvalidElementId) levelId = element.LevelId;

    // 3. A family instance may keep it on its own level parameter.
    if (levelId == ElementId.InvalidElementId)
    {
        var parameter = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
        if (parameter != null && parameter.HasValue) levelId = parameter.AsElementId();
    }

    // 4. AN MEP CURVE KEEPS IT ONLY HERE, and a lookup stopping above this one
    //    returns every duct in the model.
    if (levelId == ElementId.InvalidElementId)
    {
        var parameter = element.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
        if (parameter != null && parameter.HasValue) levelId = parameter.AsElementId();
    }

    if (levelId == ElementId.InvalidElementId) elements.Add(element);
}

var bounded = categories != null && categories.Count > 0;
findings.Add(string.Format(
    "{0} of {1} element(s) examined have no level{2}",
    elements.Count, examined,
    bounded
        ? string.Format(", within {0} category/categories", categories.Count)
        : ". THIS WAS UNBOUNDED, so it includes views, sheets, materials and "
          + "annotation - all of which correctly have no level and none of "
          + "which is the answer to anything. Give a category list"));

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

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories.ToList()));

    var linkExamined = 0;
    var linkMatched = 0;
    foreach (var element in linkedCollector)
    {
        if (element == null) continue;
        linkExamined++;

        // The same four steps as above, in the same order.
        var at = ElementId.InvalidElementId;
        var linkedWall = element as Wall;
        if (linkedWall != null) at = linkedWall.LevelId;
        if (at == ElementId.InvalidElementId) at = element.LevelId;
        if (at == ElementId.InvalidElementId)
        {
            var parameter = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
            if (parameter != null && parameter.HasValue) at = parameter.AsElementId();
        }
        if (at == ElementId.InvalidElementId)
        {
            var parameter = element.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
            if (parameter != null && parameter.HasValue) at = parameter.AsElementId();
        }

        if (at == ElementId.InvalidElementId) linkMatched++;
    }

    linksSearched++;
    linkedTotal += linkMatched;
    linkedMatches.Add(string.Format("{0}: {1} of {2} have no level", linked.Title, linkMatched,
        linkExamined));
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
