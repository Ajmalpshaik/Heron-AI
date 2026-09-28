// NOT STANDALONE. Assumes `doc`, `categories`, `wantOpenEnds` and
// `includeLinks` are in scope; leaves `elements`, `withoutConnectors`,
// `findings`, `linksSearched` and `linkedMatches` behind.
//
// ASKING A CONNECTOR WHETHER IT IS CONNECTED CAN THROW. A panelboard's six
// connectors include surface ones, and reading connection status on those
// raises rather than returning false - measured. So only END connectors are
// asked, which is also the only kind the question means: a surface connector
// has no end to leave open. Every read is guarded even so.
//
// A LOCAL CHECK, NOT A TRACE. It asks each element about its own connectors and
// walks nothing. A run joined at every joint can still be a closed loop that
// reaches no plant - FIND_SYSTEM_ISLANDS is what sees that.
//
// AN ELEMENT WITH NO END CONNECTORS IS NEITHER OPEN NOR JOINED and is counted
// separately. Putting it in either list would be an invention, and a large
// count there usually means the category is wrong rather than the model.
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
// A LINK'S OPEN END IS OPEN IN THE LINK. A connector in a link is asked about
// its own document; a linked duct that meets a host duct end to end still
// reads as open, because Revit does not connect across documents.

var elements = new List<Element>();
var withoutConnectors = 0;
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

Func<Element, ConnectorManager> managerOf = element =>
{
    var curve = element as MEPCurve;
    if (curve != null) return curve.ConnectorManager;

    var instance = element as FamilyInstance;
    if (instance != null && instance.MEPModel != null) return instance.MEPModel.ConnectorManager;

    return null;
};

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
if (categories != null && categories.Count > 0)
    collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

var scanned = 0;
var unreadable = 0;

foreach (var element in collector)
{
    scanned++;

    ConnectorManager manager = null;
    try { manager = managerOf(element); }
    catch { manager = null; }

    if (manager == null) { withoutConnectors++; continue; }

    var ends = 0;
    var open = 0;
    var refused = false;

    try
    {
        foreach (Connector connector in manager.Connectors)
        {
            // END connectors only. A surface connector has no end to leave
            // open, and asking it about connection status is what throws.
            if (connector.ConnectorType != ConnectorType.End) continue;
            ends++;

            try
            {
                if (!connector.IsConnected) open++;
            }
            catch
            {
                refused = true;
            }
        }
    }
    catch
    {
        refused = true;
    }

    if (refused) unreadable++;

    if (ends == 0) { withoutConnectors++; continue; }

    var hasOpenEnd = open > 0;
    if (hasOpenEnd == wantOpenEnds) elements.Add(element);
}

findings.Add(string.Format("{0} of {1} scanned element(s) {2}. {3} have no end connectors at all and "
    + "are in neither list",
    elements.Count,
    scanned,
    wantOpenEnds ? "have at least one OPEN end" : "are joined at every end",
    withoutConnectors));

if (unreadable > 0)
    findings.Add(string.Format("{0} element(s) refused to report connection status on at least one "
        + "end and may be answered wrongly. Reading that status on the wrong kind of connector raises "
        + "rather than returning false, which is why only ends are asked", unreadable));

if (withoutConnectors > 0 && withoutConnectors >= scanned / 2)
    findings.Add("More than half of what was scanned has no end connectors. That usually means the "
        + "category list is wrong rather than the model being unmodelled");

findings.Add("This asked each element about itself and walked nothing. A run joined at every joint "
    + "can still reach no plant at all - FIND_SYSTEM_ISLANDS is what sees that, and this cannot");

// THE SAME QUESTION IN EACH LINK. Counted, never selected - see the header.
for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories));

    var linkScanned = 0;
    var linkMatched = 0;
    var linkWithout = 0;
    var linkUnreadable = 0;

    foreach (var element in linkedCollector)
    {
        linkScanned++;

        ConnectorManager manager = null;
        try { manager = managerOf(element); }
        catch (Exception) { manager = null; }

        if (manager == null) { linkWithout++; continue; }

        var ends = 0;
        var open = 0;
        var refused = false;

        try
        {
            foreach (Connector connector in manager.Connectors)
            {
                if (connector.ConnectorType != ConnectorType.End) continue;
                ends++;

                try
                {
                    if (!connector.IsConnected) open++;
                }
                catch (Exception)
                {
                    refused = true;
                }
            }
        }
        catch (Exception)
        {
            refused = true;
        }

        if (refused) linkUnreadable++;

        if (ends == 0) { linkWithout++; continue; }

        if ((open > 0) == wantOpenEnds) linkMatched++;
    }

    linksSearched++;
    linkedTotal += linkMatched;
    linkedMatches.Add(string.Format("{0}: {1} of {2} {3}; {4} have no end connectors{5}",
        linked.Title, linkMatched, linkScanned,
        wantOpenEnds ? "have an OPEN end" : "are joined at every end",
        linkWithout,
        linkUnreadable > 0
            ? string.Format(", {0} refused to answer", linkUnreadable)
            : ""));
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
