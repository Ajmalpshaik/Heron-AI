// NOT STANDALONE. Assumes `doc`, `category`, `levelId` and `includeLinks` are
// already in scope, and leaves `elements`, `unresolvedLevel`, `linksSearched`
// and `linkedMatches` in scope for whatever is composed after it.
//
// WHAT REVIT ACTUALLY DOES WITH LEVELS, AND WHY THIS IS NOT A PROPERTY READ.
//
// There is no single "which level is this on" call. Where an element records
// its level depends on what kind of element it is, and these are genuinely
// different storage locations rather than aliases of one:
//
//   Wall                     -> Wall.LevelId
//   many elements            -> Element.LevelId
//   FamilyInstance           -> FAMILY_LEVEL_PARAM / SCHEDULE_LEVEL_PARAM
//   MEP CURVE (duct, pipe,   -> RBS_START_LEVEL_PARAM, and the others above are
//   cable tray, conduit)        NOT MERELY EMPTY ON IT - THEY ARE ABSENT
//
// The last row is the whole reason this code exists. A lookup that stops before
// RBS_START_LEVEL_PARAM does not throw on a duct and does not warn: every duct
// resolves to nothing, fails the comparison, and the filter returns a confident,
// successful ZERO.
//
// HERON'S DECISION, WHICH IS NOT THE OBVIOUS ONE: an element whose level cannot
// be resolved at all is COUNTED AND REPORTED, not silently dropped. Silently
// dropping it is what makes the failure above invisible - the caller sees a
// smaller number and no reason for it. Reporting it means a broken lookup shows
// up as "12 elements, 12 with no level found" instead of as a plausible zero.
// That is D-30's rule applied to code rather than to a proof: a thing that does
// nothing must never be able to look like a thing that worked.
//
// The order below is data rather than a chain of ?? operators, because the order
// IS the knowledge here and it should be readable as a list, changeable in one
// place, and quotable in the fragment's own documentation.
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
// A LINK'S LEVELS ARE ITS OWN, SO THE LEVEL IS MATCHED BY NAME THERE. `levelId`
// names a level in THIS model; in a link the same id means nothing, or another
// element. The host level's NAME is what the modeller chose, and it is what
// the link is asked for - and the answer says so, because two models can
// disagree about what "Level 2" is.

var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
    BuiltInParameter.LEVEL_PARAM,
    BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
    BuiltInParameter.RBS_START_LEVEL_PARAM,   // MEP curves. Never drop this one.
};

Func<Element, ElementId> levelOf = e =>
{
    var wall = e as Wall;
    if (wall != null) return wall.LevelId;

    if (e.LevelId != ElementId.InvalidElementId) return e.LevelId;

    foreach (var candidate in levelParameterOrder)
    {
        var p = e.get_Parameter(candidate);
        if (p != null) return p.AsElementId();
    }
    return ElementId.InvalidElementId;
};

var matching = new FilteredElementCollector(doc)
    .OfCategory(category)
    .WhereElementIsNotElementType()
    .ToElements();

var unresolvedLevel = 0;
var elements = new List<Element>();

foreach (var e in matching)
{
    if (levelId == ElementId.InvalidElementId)
    {
        elements.Add(e);
        continue;
    }

    var found = levelOf(e);
    if (found == ElementId.InvalidElementId) unresolvedLevel++;
    else if (found == levelId) elements.Add(e);
}

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

// The host level's name, the only thing about it a link can be asked.
string hostLevelName = null;
if (levelId != ElementId.InvalidElementId)
{
    var hostLevel = doc.GetElement(levelId) as Level;
    if (hostLevel != null) hostLevelName = hostLevel.Name;
}

if (levelId != ElementId.InvalidElementId && hostLevelName == null)
{
    if (linkDocs.Count > 0)
        linkBlocked = "Links NOT read: the level asked for has no name here to look for";
}
else
{
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkMatched = 0;
        var linkUnresolved = 0;

        try
        {
            foreach (var e in new FilteredElementCollector(linked)
                         .OfCategory(category)
                         .WhereElementIsNotElementType())
            {
                if (hostLevelName == null) { linkMatched++; continue; }

                var at = levelOf(e);
                if (at == ElementId.InvalidElementId) { linkUnresolved++; continue; }

                var linkedLevel = linked.GetElement(at) as Level;
                if (linkedLevel != null
                    && string.Equals(linkedLevel.Name, hostLevelName, StringComparison.OrdinalIgnoreCase))
                    linkMatched++;
            }
        }
        catch (Exception) { }

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1}{2}{3}", linked.Title, linkMatched,
            hostLevelName == null ? "" : string.Format(" on a level named '{0}'", hostLevelName),
            linkUnresolved > 0 ? string.Format(", {0} with no level found", linkUnresolved) : ""));
    }
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
