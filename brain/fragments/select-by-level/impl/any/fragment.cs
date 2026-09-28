// NOT STANDALONE. Assumes `doc`, `levelId`, `categories` and `includeLinks`
// are in scope; leaves `elements`, `unresolvedLevel`, `findings`,
// `linksSearched` and `linkedMatches` behind.
//
// THIS IS FILTER_ELEMENTS_BY_CATEGORY'S LEVEL LOOKUP, DELIBERATELY IDENTICAL.
// There is no single "which level is this on" call, and where an element keeps
// its level depends on what it is:
//
//   Wall                     -> Wall.LevelId
//   many elements            -> Element.LevelId
//   FamilyInstance           -> FAMILY_LEVEL_PARAM / SCHEDULE_LEVEL_PARAM
//   MEP CURVE (duct, pipe,   -> RBS_START_LEVEL_PARAM, and the others are
//   cable tray, conduit)        ABSENT on it rather than merely empty
//
// Stop before that last entry and every duct in the model resolves to nothing,
// fails the comparison, and the filter returns a confident, successful ZERO.
//
// THE DIFFERENCE FROM THAT FRAGMENT is only that no category is required here.
// When one IS known it should be used, because it bounds the scan to that
// category instead of walking the model.
//
// AN UNRESOLVED LEVEL IS COUNTED, NOT DROPPED. Unbounded, that count is large
// and correct - views, sheets, materials and most annotation have no level at
// all. What it protects against is the other case: a lookup that broke,
// reporting a plausible small number with no reason attached.
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
// A LINK'S LEVELS ARE ITS OWN, SO THE LEVEL IS MATCHED BY NAME THERE -
// FILTER_ELEMENTS_BY_CATEGORY's rule. `levelId` names a level in THIS model;
// the link is asked for a level of the same NAME, and the answer says which
// name it looked for.

var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
    BuiltInParameter.LEVEL_PARAM,
    BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
    BuiltInParameter.RBS_START_LEVEL_PARAM,   // MEP curves. Never drop this one.
};

var elements = new List<Element>();
var unresolvedLevel = 0;
var findings = new List<string>();

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

var level = levelId == ElementId.InvalidElementId ? null : doc.GetElement(levelId) as Level;

if (level == null)
{
    findings.Add("No level was given, or that id is not a Level. Name the level to search - unlike "
        + "FILTER_ELEMENTS_BY_CATEGORY, this fragment has no whole-model meaning without one");
}
else
{
    Func<Element, ElementId> levelOf = element =>
    {
        var wall = element as Wall;
        if (wall != null) return wall.LevelId;

        if (element.LevelId != ElementId.InvalidElementId) return element.LevelId;

        foreach (var candidate in levelParameterOrder)
        {
            var p = element.get_Parameter(candidate);
            if (p != null) return p.AsElementId();
        }
        return ElementId.InvalidElementId;
    };

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    var scanned = 0;
    foreach (var element in collector)
    {
        scanned++;

        var found = ElementId.InvalidElementId;
        try { found = levelOf(element); }
        catch { found = ElementId.InvalidElementId; }

        if (found == ElementId.InvalidElementId) unresolvedLevel++;
        else if (found == level.Id) elements.Add(element);
    }

    var bounded = categories != null && categories.Count > 0;

    findings.Add(string.Format("{0} of {1} scanned element(s) are on level '{2}'{3}. {4} carry no "
        + "level at all",
        elements.Count, scanned, level.Name,
        bounded ? string.Format(", within {0} category/categories", categories.Count)
                : ", across every category",
        unresolvedLevel));

    if (!bounded)
        findings.Add("This walked the whole model. A category list bounds it, and where a single "
            + "category is known FILTER_ELEMENTS_BY_CATEGORY takes a level and is cheaper");

    // THE SAME LEVEL, BY NAME, IN EACH LINK. Counted, never selected.
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories));

        var linkMatched = 0;
        var linkUnresolved = 0;
        foreach (var element in linkedCollector)
        {
            var at = ElementId.InvalidElementId;
            try { at = levelOf(element); }
            catch (Exception) { at = ElementId.InvalidElementId; }

            if (at == ElementId.InvalidElementId) { linkUnresolved++; continue; }

            var linkedLevel = linked.GetElement(at) as Level;
            if (linkedLevel != null
                && string.Equals(linkedLevel.Name, level.Name, StringComparison.OrdinalIgnoreCase))
                linkMatched++;
        }

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1} on a level named '{2}'; {3} carry no level",
            linked.Title, linkMatched, level.Name, linkUnresolved));
    }
}

if (level == null && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no level was given to look for - see findings";

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
