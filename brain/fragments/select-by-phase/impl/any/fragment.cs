// NOT STANDALONE. Assumes `doc`, `createdPhaseName`, `demolishedPhaseName`,
// `categories` and `includeLinks` are in scope; leaves `elements`,
// `withoutPhasing`, `findings`, `linksSearched` and `linkedMatches` behind.
//
// MOST OF A MODEL HAS NO PHASING, AND IT IS COUNTED RATHER THAN DROPPED.
// Levels, grids, views, sheets and view-specific annotation carry no phase at
// all. Swallowing them in a try/catch makes a broken lookup and a correct small
// answer read identically - the same failure FILTER_ELEMENTS_BY_CATEGORY
// reports as `unresolvedLevel`, reported here as `withoutPhasing`.
//
// CreatedPhaseId AND DemolishedPhaseId ARE ON 2020. That was compiled at both
// ends of the range, not remembered; the plausible guess is that they are newer
// than that and the two phase BuiltInParameters are the compatible route.
//
// A PHASE NAME THAT DOES NOT EXIST IS REFUSED WITH THE REAL NAMES. Almost every
// office renames Revit's "Existing" and "New Construction", and a typo must not
// come back as an empty phase.
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
// A LINK'S PHASES ARE ITS OWN, SO THEY ARE MATCHED BY NAME THERE, and a link
// without a phase of that name says so on its own line rather than
// reporting zero.

var elements = new List<Element>();
var withoutPhasing = 0;
var findings = new List<string>();

var wantCreated = string.IsNullOrEmpty(createdPhaseName) ? "" : createdPhaseName.Trim();
var wantDemolished = string.IsNullOrEmpty(demolishedPhaseName) ? "" : demolishedPhaseName.Trim();

if (wantCreated.Length == 0 && wantDemolished.Length == 0)
{
    findings.Add("No phase was named - give a Phase Created name, a Phase Demolished name, or both");
}
else
{
    var phases = new List<Phase>();
    foreach (var phase in new FilteredElementCollector(doc).OfClass(typeof(Phase)).Cast<Phase>())
        phases.Add(phase);

    var names = new List<string>();
    foreach (var phase in phases) names.Add(phase.Name);
    var known = names.Count == 0 ? "none" : string.Join(", ", names.ToArray());

    Phase created = null;
    Phase demolished = null;
    foreach (var phase in phases)
    {
        if (created == null && wantCreated.Length > 0
            && string.Equals(phase.Name, wantCreated, StringComparison.OrdinalIgnoreCase)) created = phase;
        if (demolished == null && wantDemolished.Length > 0
            && string.Equals(phase.Name, wantDemolished, StringComparison.OrdinalIgnoreCase)) demolished = phase;
    }

    if (wantCreated.Length > 0 && created == null)
        findings.Add(string.Format("No phase is called '{0}'. This model has: {1}", wantCreated, known));
    else if (wantDemolished.Length > 0 && demolished == null)
        findings.Add(string.Format("No phase is called '{0}'. This model has: {1}", wantDemolished, known));
    else
    {
        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        foreach (var element in collector)
        {
            var createdId = ElementId.InvalidElementId;
            var demolishedId = ElementId.InvalidElementId;
            var phased = false;

            try
            {
                createdId = element.CreatedPhaseId;
                demolishedId = element.DemolishedPhaseId;
                phased = createdId != ElementId.InvalidElementId
                    || demolishedId != ElementId.InvalidElementId;
            }
            catch
            {
                phased = false;
            }

            if (!phased)
            {
                withoutPhasing++;
                continue;
            }

            var createdOk = created == null || createdId == created.Id;
            var demolishedOk = demolished == null || demolishedId == demolished.Id;
            if (createdOk && demolishedOk) elements.Add(element);
        }

        var asked = new List<string>();
        if (created != null) asked.Add(string.Format("created in '{0}'", created.Name));
        if (demolished != null) asked.Add(string.Format("demolished in '{0}'", demolished.Name));

        findings.Add(string.Format("{0} element(s) {1}. {2} more carry no phasing at all and were "
            + "neither matched nor counted against the answer",
            elements.Count,
            string.Join(" and ", asked.ToArray()),
            withoutPhasing));

        if (demolished != null)
            findings.Add("This is 'demolished in that phase', not 'demolished' - an element that is "
                + "never demolished has no demolition phase to match");
    }
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

if (wantCreated.Length == 0 && wantDemolished.Length == 0 && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no phase was named";

for (var i = 0; i < linkDocs.Count && linkBlocked.Length == 0; i++)
{
    var linked = linkDocs[i];
    Phase linkCreated = null;
    Phase linkDemolished = null;
    var linkNames = new List<string>();

    try
    {
        foreach (var phase in new FilteredElementCollector(linked).OfClass(typeof(Phase)).Cast<Phase>())
        {
            linkNames.Add(phase.Name);
            if (linkCreated == null && wantCreated.Length > 0
                && string.Equals(phase.Name, wantCreated, StringComparison.OrdinalIgnoreCase)) linkCreated = phase;
            if (linkDemolished == null && wantDemolished.Length > 0
                && string.Equals(phase.Name, wantDemolished, StringComparison.OrdinalIgnoreCase)) linkDemolished = phase;
        }
    }
    catch (Exception) { }

    linksSearched++;

    if ((wantCreated.Length > 0 && linkCreated == null)
        || (wantDemolished.Length > 0 && linkDemolished == null))
    {
        linkedMatches.Add(string.Format("{0}: no phase of that name - it has {1}", linked.Title,
            linkNames.Count == 0 ? "none" : string.Join(", ", linkNames.ToArray())));
        continue;
    }

    var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories));

    var linkMatched = 0;
    var linkUnphased = 0;
    foreach (var element in linkedCollector)
    {
        var createdId = ElementId.InvalidElementId;
        var demolishedId = ElementId.InvalidElementId;
        try
        {
            createdId = element.CreatedPhaseId;
            demolishedId = element.DemolishedPhaseId;
        }
        catch (Exception) { }

        if (createdId == ElementId.InvalidElementId && demolishedId == ElementId.InvalidElementId)
        {
            linkUnphased++;
            continue;
        }

        if ((linkCreated == null || createdId == linkCreated.Id)
            && (linkDemolished == null || demolishedId == linkDemolished.Id))
            linkMatched++;
    }

    linkedTotal += linkMatched;
    linkedMatches.Add(string.Format("{0}: {1}; {2} carry no phasing", linked.Title, linkMatched,
        linkUnphased));
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
