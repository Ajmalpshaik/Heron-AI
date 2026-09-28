// NOT STANDALONE. Assumes `doc`, `categories`, `wantInsulated`,
// `countLiningAsInsulated` and `includeLinks` are in scope; leaves `elements`,
// `scanned`, `findings`, `linksSearched` and `linkedMatches` behind.
//
// IT ASKS THE INSULATION, NOT THE DUCT. A duct carries no reliable "am I
// insulated" parameter - the truth is a separate element recording which host
// it wraps. Reading the wrappers is the real relationship; a parameter is
// whatever somebody last typed.
//
// THE LOOKUP IS BUILT ONCE. Asking each duct "is anything wrapping you" is a
// whole-model scan per duct. One pass over the wrappers gives the same answer,
// and on a real model that is seconds against minutes.
//
// LINING IS NOT INSULATION. External thermal insulation and internal acoustic
// lining are different elements doing different jobs, and "insulated" means
// either depending on who is asking - so it is asked, and the report says
// which was counted.
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
// A LINK'S WRAPPERS ARE ASKED IN THE LINK. Insulation names its host by id,
// and that id is the link's own, so each link builds its own lookup.

var elements = new List<Element>();
var scanned = 0;
var findings = new List<string>();

var covered = new List<ElementId>();

Action<FilteredElementCollector> collectHosts = source =>
{
    foreach (var wrapper in source)
    {
        var covering = wrapper as InsulationLiningBase;
        if (covering == null) continue;
        try
        {
            var hostId = covering.HostElementId;
            if (hostId == ElementId.InvalidElementId) continue;

            var already = false;
            foreach (var known in covered)
            {
                if (known == hostId) { already = true; break; }
            }
            if (!already) covered.Add(hostId);
        }
        catch
        {
            // A wrapper that will not name its host tells us nothing about any
            // duct, and must not be allowed to stop the pass.
        }
    }
};

collectHosts(new FilteredElementCollector(doc).OfClass(typeof(DuctInsulation)));
collectHosts(new FilteredElementCollector(doc).OfClass(typeof(PipeInsulation)));
if (countLiningAsInsulated)
    collectHosts(new FilteredElementCollector(doc).OfClass(typeof(DuctLining)));

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
if (categories != null && categories.Count > 0)
    collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

foreach (var element in collector)
{
    scanned++;

    var isCovered = false;
    foreach (var hostId in covered)
    {
        if (hostId == element.Id) { isCovered = true; break; }
    }

    if (isCovered == wantInsulated) elements.Add(element);
}

findings.Add(string.Format("{0} of {1} scanned element(s) are {2}. {3} host(s) in this model carry "
    + "insulation or lining in total{4}",
    elements.Count,
    scanned,
    wantInsulated ? "INSULATED" : "BARE - nothing wrapping them",
    covered.Count,
    countLiningAsInsulated
        ? ", and internal LINING was counted as insulated"
        : ", and internal lining was NOT counted - only external insulation"));

if (categories == null || categories.Count == 0)
    findings.Add("No category was given, so this swept every element in the model. Almost all of "
        + "them are bare because almost none of them could be insulated - name the duct or pipe "
        + "categories to make the count mean something");

if (!wantInsulated && elements.Count > 0)
    findings.Add("This is what has nothing on it, not a work list. Fittings, flex and equipment sit "
        + "in the same categories, and whether each one SHOULD be insulated is a judgement");

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
    var linkCovered = new HashSet<ElementId>();

    Action<FilteredElementCollector> linkHosts = source =>
    {
        foreach (var wrapper in source)
        {
            var covering = wrapper as InsulationLiningBase;
            if (covering == null) continue;
            try
            {
                var hostId = covering.HostElementId;
                if (hostId != ElementId.InvalidElementId) linkCovered.Add(hostId);
            }
            catch (Exception) { }
        }
    };

    try
    {
        linkHosts(new FilteredElementCollector(linked).OfClass(typeof(DuctInsulation)));
        linkHosts(new FilteredElementCollector(linked).OfClass(typeof(PipeInsulation)));
        if (countLiningAsInsulated)
            linkHosts(new FilteredElementCollector(linked).OfClass(typeof(DuctLining)));
    }
    catch (Exception) { }

    var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories));

    var linkScanned = 0;
    var linkMatched = 0;
    foreach (var element in linkedCollector)
    {
        linkScanned++;
        if (linkCovered.Contains(element.Id) == wantInsulated) linkMatched++;
    }

    linksSearched++;
    linkedTotal += linkMatched;
    linkedMatches.Add(string.Format("{0}: {1} of {2} are {3}", linked.Title, linkMatched,
        linkScanned, wantInsulated ? "insulated" : "bare"));
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
