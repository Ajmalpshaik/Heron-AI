// NOT STANDALONE. Assumes `doc`, `categoryName`, `inViewOnly` and
// `includeLinks` are in scope, and leaves `elements`, `resolvedTo`,
// `nearMisses`, `found`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE WORDS ARE WHAT ARRIVES.
//
// A request comes in somebody's own language, and turning "duct accessories"
// into the right internal category is a guess whose wrong answer looks right -
// the identifier is not the obvious one, and a near miss silently selects a
// different category.
//
// A NAME THAT MATCHES NOTHING RETURNS THE NEAR MISSES.
//
// "Duct Accessory" singular is what a person types. Handing back the category
// names that nearly matched turns a dead end into one more sentence; guessing
// which was meant is what this refuses to do.
//
// AND THE NAMES ARE LOCALISED.
//
// On a French or Arabic Revit the names are in that language and the internal
// identifiers are not. This fragment is the one that works there; the
// identifier route is the one that works everywhere. Both are worth having.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read and
// its matches are reported as TEXT in `linkedMatches`, one line per link.
// They never enter `elements`: that list feeds the next fragment in a chain,
// and the chain revives ids against the HOST document, so a linked id that
// happens to be in use in the host binds an unrelated element silently
// (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// A VIEW IS READ THROUGH EACH PLACEMENT, AND ONLY FROM 2023. Revit 2023 added
// a collector that reads a link's elements as a host view draws them, per link
// placement. It is reached by reflection because the add-in compiles fragments
// without release symbols (row 5b-181); on 2020-2022 it is absent and the
// answer says so rather than reading the whole link and calling it the view.
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. A link inside a link
// is a real case D-59 left open; reading it needs the parent placement's
// transform and a decision about what `linksSearched` counts. Until that is
// decided against a real federated model, they are named, not guessed at.

var elements = new List<Element>();
string resolvedTo = "";
var nearMisses = new List<string>();

string wanted = (categoryName ?? "").Trim();

Category match = null;
var everyName = new List<string>();

try
{
    foreach (Category category in doc.Settings.Categories)
    {
        if (category == null) continue;
        string name = "";
        try { name = category.Name ?? ""; } catch { }
        if (name.Length == 0) continue;
        everyName.Add(name);

        if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)) match = category;
    }
}
catch { }

if (match == null && wanted.Length > 0)
{
    // Near misses: a category whose name contains the words, or whose words
    // are contained in it. Enough to answer with a question rather than a
    // dead end.
    foreach (var name in everyName)
    {
        if (name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0 ||
            wanted.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            nearMisses.Add(name);
    }
}

if (match != null)
{
    resolvedTo = match.Name;

    var collector = inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc);

    try
    {
        foreach (var element in collector
                     .WhereElementIsNotElementType()
                     .OfCategoryId(match.Id))
            if (element != null) elements.Add(element);
    }
    catch { }
}

int found = elements.Count;

// ---- D-59: the links, only when asked for ----------------------------------

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

if (linkDocs.Count > 0 && inViewOnly != null && linkThroughView == null)
{
    linkBlocked = "Links NOT read: this Revit cannot read a link through a view "
        + "(2023 and later can) - ask without the view";
}
else if (match != null)
{
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];

        // BY NAME IN THE LINK'S OWN LIST. The link has its own categories;
        // the name is what was asked for and what both documents share.
        Category linkedCategory = null;
        try
        {
            foreach (Category category in linked.Settings.Categories)
            {
                if (category == null) continue;
                string name = "";
                try { name = category.Name ?? ""; } catch (Exception) { }
                if (string.Equals(name, match.Name, StringComparison.OrdinalIgnoreCase))
                {
                    linkedCategory = category;
                    break;
                }
            }
        }
        catch (Exception) { }

        var count = 0;
        var unreadPlacements = 0;

        if (linkedCategory != null)
        {
            if (inViewOnly == null)
            {
                try
                {
                    count = new FilteredElementCollector(linked)
                        .WhereElementIsNotElementType()
                        .OfCategoryId(linkedCategory.Id)
                        .GetElementCount();
                }
                catch (Exception) { }
            }
            else
            {
                // Per placement, and each element once however many
                // placements show it.
                var seen = new HashSet<ElementId>();
                foreach (var placement in linkPlacements[i])
                {
                    try
                    {
                        var through = linkThroughView.Invoke(
                            new object[] { doc, inViewOnly.Id, placement.Id })
                            as FilteredElementCollector;
                        if (through == null) { unreadPlacements++; continue; }
                        foreach (var id in through.WhereElementIsNotElementType()
                                     .OfCategoryId(linkedCategory.Id).ToElementIds())
                            seen.Add(id);
                    }
                    catch (Exception) { unreadPlacements++; }
                }
                count = seen.Count;
            }
        }

        linksSearched++;
        linkedTotal += count;
        linkedMatches.Add(string.Format("{0}: {1}{2}{3}", linked.Title, count,
            linkedCategory == null ? " (no category of that name in this link)" : "",
            unreadPlacements > 0
                ? string.Format(" ({0} placement(s) could not be read through this view)",
                    unreadPlacements)
                : ""));
    }
}
else if (linkDocs.Count > 0)
{
    linkBlocked = "Links NOT read: the name matched no category here, so there was "
        + "nothing to look for";
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
