// NOT STANDALONE. Assumes `doc`, `categories`, `inViewOnly` and `includeLinks`
// are in scope, and leaves `elements`, `perCategory`, `found`, `linksSearched`
// and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ONE PASS, NOT ONE PER CATEGORY.
//
// A collector per category walks the document once each and then hands back
// lists that have to be joined and de-duplicated by hand. A multi-category
// filter asks the same question once - faster, and structurally unable to
// return the same element twice.
//
// THE BREAKDOWN IS THE PART THAT CATCHES THE MISTAKE.
//
// A total of 480 does not say whether the flex duct came in. Per category it
// is obvious: no flex duct means either the model has none or the category was
// left out of the list, and those need different actions. A category asked for
// and empty is reported as zero rather than left out, because the absence is
// the finding.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link's matches are
// reported as TEXT in `linkedMatches`, one line per link with its own
// breakdown. They never enter `elements` or `perCategory`: `elements` feeds
// the next fragment in a chain, and the chain revives ids against the HOST
// document, so a linked id that happens to be in use in the host binds an
// unrelated element silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// A VIEW IS READ THROUGH EACH PLACEMENT, AND ONLY FROM 2023. Revit 2023 added
// a collector that reads a link's elements as a host view draws them, per link
// placement. It is reached by reflection because the add-in compiles fragments
// without release symbols (row 5b-181); on 2020-2022 it is absent and the
// answer says so rather than reading the whole link and calling it the view.
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.

var elements = new List<Element>();
var perCategory = new Dictionary<string, int>();

var wanted = new List<BuiltInCategory>();
if (categories != null)
{
    foreach (var category in categories) wanted.Add(category);
}

if (wanted.Count > 0)
{
    // Every category asked for starts at zero, so one with nothing in it is
    // still in the report.
    foreach (var category in wanted)
    {
        var id = new ElementId(category);
        var definition = Category.GetCategory(doc, category);
        string name = definition != null ? definition.Name : category.ToString();
        if (!perCategory.ContainsKey(name)) perCategory[name] = 0;
    }

    var collector = inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc);

    foreach (var element in collector
                 .WhereElementIsNotElementType()
                 .WherePasses(new ElementMulticategoryFilter(wanted)))
    {
        if (element == null) continue;
        elements.Add(element);

        string name = "(no category)";
        try { if (element.Category != null) name = element.Category.Name ?? name; } catch { }
        if (perCategory.ContainsKey(name)) perCategory[name] = perCategory[name] + 1;
        else perCategory[name] = 1;
    }
}

int found = elements.Count;

// ---- D-59: the links, only when asked for ----------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
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

if (linkDocs.Count > 0 && inViewOnly != null && linkThroughView == null)
{
    linkBlocked = "Links NOT read: this Revit cannot read a link through a view "
        + "(2023 and later can) - ask without the view";
}
else if (wanted.Count > 0)
{
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkedElements = new List<Element>();
        var unreadPlacements = 0;

        if (inViewOnly == null)
        {
            try
            {
                foreach (var element in new FilteredElementCollector(linked)
                             .WhereElementIsNotElementType()
                             .WherePasses(new ElementMulticategoryFilter(wanted)))
                    if (element != null) linkedElements.Add(element);
            }
            catch (Exception) { }
        }
        else
        {
            // Per placement, and each element once however many placements
            // show it.
            var seen = new HashSet<ElementId>();
            foreach (var placement in linkPlacements[i])
            {
                try
                {
                    var through = linkThroughView.Invoke(
                        new object[] { doc, inViewOnly.Id, placement.Id })
                        as FilteredElementCollector;
                    if (through == null) { unreadPlacements++; continue; }
                    foreach (var element in through.WhereElementIsNotElementType()
                                 .WherePasses(new ElementMulticategoryFilter(wanted)))
                    {
                        if (element != null && seen.Add(element.Id)) linkedElements.Add(element);
                    }
                }
                catch (Exception) { unreadPlacements++; }
            }
        }

        // The link's own breakdown, in the same words as the host's.
        var byName = new Dictionary<string, int>();
        foreach (var element in linkedElements)
        {
            string name = "(no category)";
            try { if (element.Category != null) name = element.Category.Name ?? name; }
            catch (Exception) { }
            byName[name] = byName.ContainsKey(name) ? byName[name] + 1 : 1;
        }

        var parts = new List<string>();
        foreach (var entry in byName) parts.Add(entry.Key + " " + entry.Value);

        linksSearched++;
        linkedTotal += linkedElements.Count;
        linkedMatches.Add(string.Format("{0}: {1}{2}{3}", linked.Title, linkedElements.Count,
            parts.Count > 0 ? " (" + string.Join(", ", parts.ToArray()) + ")" : "",
            unreadPlacements > 0
                ? string.Format(" ({0} placement(s) could not be read through this view)",
                    unreadPlacements)
                : ""));
    }
}
else if (linkDocs.Count > 0)
{
    linkBlocked = "Links NOT read: no category was asked for";
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
