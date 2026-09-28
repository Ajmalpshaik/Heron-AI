// NOT STANDALONE. Assumes `doc`, `inViewOnly` and `includeLinks` are in scope,
// and leaves `elements`, `byKind`, `found`, `linksSearched` and
// `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// OPENINGS ARE NOT ONE CATEGORY.
//
// A shaft cutting several levels, a hole in a slab, a rectangular opening in a
// wall and one in a roof are four different categories with four different
// names. Asking for the obvious one returns a fraction of what is there and
// looks like a complete answer, which is the failure this exists to avoid.
//
// AN OPENING CUT BY A FAMILY IS NOT ONE OF THESE.
//
// The hole a door or a window makes belongs to that family and is in none of
// these categories. That is correct - it is the door's hole - but "show me
// every hole" means both to a person, so it is said rather than left to be
// discovered.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// (it has never been proved). When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements` or
// `byKind`: those feed the next fragment in a chain, and the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// FOR MEP COORDINATION THE OPENINGS ARE USUALLY IN A LINK - the shafts and
// slab holes are the architect's and the structural engineer's - so this is
// one of the fragments host-only answers most confidently wrong.
//
// A VIEW IS READ THROUGH EACH PLACEMENT, AND ONLY FROM 2023 - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule. On 2020-2022 the
// answer says the links were not read, never the whole link as the view.

var elements = new List<Element>();
var byKind = new Dictionary<string, int>();

var openingCategories = new List<BuiltInCategory>();
openingCategories.Add(BuiltInCategory.OST_ShaftOpening);
openingCategories.Add(BuiltInCategory.OST_FloorOpening);
openingCategories.Add(BuiltInCategory.OST_SWallRectOpening);
openingCategories.Add(BuiltInCategory.OST_RoofOpening);
openingCategories.Add(BuiltInCategory.OST_ColumnOpening);

// Every category starts at zero, so a kind with none is still in the report -
// no shafts at all is a finding on a building with risers in it.
foreach (var category in openingCategories)
{
    var definition = Category.GetCategory(doc, category);
    string name = definition != null ? definition.Name : category.ToString();
    if (!byKind.ContainsKey(name)) byKind[name] = 0;
}

var collector = inViewOnly != null
    ? new FilteredElementCollector(doc, inViewOnly.Id)
    : new FilteredElementCollector(doc);

foreach (var element in collector
             .WhereElementIsNotElementType()
             .WherePasses(new ElementMulticategoryFilter(openingCategories)))
{
    if (element == null) continue;
    elements.Add(element);

    string name = "(no category)";
    try { if (element.Category != null) name = element.Category.Name ?? name; } catch { }
    if (byKind.ContainsKey(name)) byKind[name] = byKind[name] + 1;
    else byKind[name] = 1;
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

// Revit 2023 and later only - see the header. Null on 2020-2022.
var linkThroughView = inViewOnly == null ? null
    : typeof(FilteredElementCollector).GetConstructor(
        new[] { typeof(Document), typeof(ElementId), typeof(ElementId) });

// Every element one link shows, once however many placements show it: the
// whole document without a view, each placement through the view with one.
// Null when a view was asked for and this Revit cannot read a link through
// one. `unreadPlacements` counts placements the view could not read.
var unreadPlacements = 0;
Func<int, List<Element>> linkedElementsOf = i =>
{
    var gathered = new List<Element>();
    if (inViewOnly == null)
    {
        try
        {
            foreach (var element in new FilteredElementCollector(linkDocs[i])
                         .WhereElementIsNotElementType())
                if (element != null) gathered.Add(element);
        }
        catch (Exception) { }
        return gathered;
    }
    if (linkThroughView == null) return null;

    var seen = new HashSet<ElementId>();
    foreach (var placement in linkPlacements[i])
    {
        try
        {
            var through = linkThroughView.Invoke(
                new object[] { doc, inViewOnly.Id, placement.Id }) as FilteredElementCollector;
            if (through == null) { unreadPlacements++; continue; }
            foreach (var element in through.WhereElementIsNotElementType())
                if (element != null && seen.Add(element.Id)) gathered.Add(element);
        }
        catch (Exception) { unreadPlacements++; }
    }
    return gathered;
};

if (linkDocs.Count > 0 && inViewOnly != null && linkThroughView == null)
    linkBlocked = "Links NOT read: this Revit cannot read a link through a view "
        + "(2023 and later can) - ask without the view";

for (var i = 0; i < linkDocs.Count && linkBlocked.Length == 0; i++)
{
    var linked = linkDocs[i];
    unreadPlacements = 0;
    var inLink = linkedElementsOf(i) ?? new List<Element>();

    var linkByKind = new Dictionary<string, int>();
    var count = 0;
    foreach (var element in inLink)
    {
        Category category = null;
        try { category = element.Category; } catch (Exception) { }
        if (category == null) continue;

        var isOpening = false;
        foreach (var wanted in openingCategories)
        {
            if (category.Id == new ElementId(wanted)) { isOpening = true; break; }
        }
        if (!isOpening) continue;

        count++;
        var name = category.Name ?? "(no category)";
        linkByKind[name] = linkByKind.ContainsKey(name) ? linkByKind[name] + 1 : 1;
    }

    var parts = new List<string>();
    foreach (var entry in linkByKind) parts.Add(entry.Key + " " + entry.Value);

    linksSearched++;
    linkedTotal += count;
    linkedMatches.Add(string.Format("{0}: {1}{2}{3}", linked.Title, count,
        parts.Count > 0 ? " (" + string.Join(", ", parts.ToArray()) + ")" : "",
        unreadPlacements > 0
            ? string.Format(" ({0} placement(s) could not be read through this view)", unreadPlacements)
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
