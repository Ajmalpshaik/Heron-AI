// NOT STANDALONE. Assumes `doc`, `matchOn`, `systemText` and `includeLinks`
// are in scope; leaves `elements`, `withoutSystem`, `findings`,
// `linksSearched` and `linkedMatches` behind.
//
// NAME AND TYPE ARE DIFFERENT QUESTIONS AND THE CALLER SAYS WHICH. The type is
// the classification - Supply Air, Domestic Cold Water, or the project's short
// code. The name is the particular run - SA 1, DXS 2. Several named systems
// share one type, which is the exact difference somebody is checking.
//
// THEY LIVE ON DIFFERENT PARAMETERS, AND GETTING IT WRONG IS SILENT. Reading
// RBS_SYSTEM_NAME_PARAM first and "falling back" to the type parameter never
// reaches the fallback: the name parameter is present on nearly every duct and
// pipe, so such a fragment matches NAMES while reporting TYPES.
//
// ONE MULTI-CATEGORY PASS, NOT FOUR COLLECTORS UNITED. FilteredElementCollector
// .UnionWith does NOT carry each side's own quick filters into the merged
// result - an instances-only filter applied per side is silently dropped and
// TYPE elements arrive beside real ones, measured at 52 where 4 were right.
// A single ElementMulticategoryFilter cannot have that problem.
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
// A LINKED RUN IS ON THE LINK'S OWN SYSTEM. The system name and type are read
// off each linked element's own parameters, which name the link's systems -
// a host system called SA 1 and a linked one called SA 1 are two systems.

var elements = new List<Element>();
var withoutSystem = 0;
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

var how = string.IsNullOrEmpty(matchOn) ? "" : matchOn.Trim().ToLowerInvariant();
var needle = string.IsNullOrEmpty(systemText) ? "" : systemText.Trim();

if (how != "name" && how != "type")
{
    findings.Add("Say whether to match the system NAME - one run, like 'DXS 1' - or the system TYPE, "
        + "the classification every run of that kind shares. Several named systems share one type, so "
        + "the two answer different questions and neither is a safe guess");
}
else if (needle.Length == 0)
{
    findings.Add("No system text was given - say which system name or system type to look for");
}
else
{
    var categories = new List<BuiltInCategory>
    {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PipeFitting,
    };

    var collector = new FilteredElementCollector(doc)
        .WherePasses(new ElementMulticategoryFilter(categories))
        .WhereElementIsNotElementType();

    Func<Element, string> systemTextOf = element =>
    {
        if (how == "name")
        {
            var p = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM);
            if (p == null) return null;
            return p.AsString() ?? p.AsValueString();
        }

        // The type parameter holds an ElementId pointing at the system type
        // element. Its DISPLAY NAME is where the project's short code lives, so
        // it is read as a value string and never as a number.
        var typeParam = element.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)
                     ?? element.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
        if (typeParam == null) return null;
        return typeParam.AsValueString();
    };

    var scanned = 0;
    var leaked = 0;

    foreach (var element in collector)
    {
        scanned++;

        // Belt and braces on the trap above: if a type element ever reaches
        // here, it is counted and named rather than quietly answered with.
        if (element is ElementType) { leaked++; continue; }

        string value = null;
        try { value = systemTextOf(element); }
        catch { value = null; }

        if (string.IsNullOrEmpty(value)) { withoutSystem++; continue; }

        if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) elements.Add(element);
    }

    findings.Add(string.Format("{0} of {1} scanned duct/pipe element(s) have a system {2} containing "
        + "'{3}'. {4} are on no system at all",
        elements.Count, scanned, how, needle, withoutSystem));

    if (withoutSystem > 0)
        findings.Add("A duct or pipe on no system is usually one that is not actually connected. "
            + "FIND_SYSTEM_ISLANDS and TRACE_CONNECTIVITY are what diagnose that; it is counted here, "
            + "not explained here");

    if (leaked > 0)
        findings.Add(string.Format("{0} TYPE element(s) reached the instance loop and were dropped. "
            + "That should be impossible through a single multi-category filter - if it happens, the "
            + "collector is not behaving as this fragment assumes", leaked));

    // THE SAME MATCH IN EACH LINK. Counted, never selected.
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkScanned = 0;
        var linkMatched = 0;
        var linkWithout = 0;

        foreach (var element in new FilteredElementCollector(linked)
                     .WherePasses(new ElementMulticategoryFilter(categories))
                     .WhereElementIsNotElementType())
        {
            if (element is ElementType) continue;
            linkScanned++;

            string value = null;
            try { value = systemTextOf(element); }
            catch (Exception) { value = null; }

            if (string.IsNullOrEmpty(value)) { linkWithout++; continue; }
            if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) linkMatched++;
        }

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1} of {2} match; {3} on no system", linked.Title,
            linkMatched, linkScanned, linkWithout));
    }
}

if (includeLinks && linkDocs.Count > 0 && linksSearched == 0)
    linkBlocked = "Links NOT read: the question was incomplete - see findings";

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
