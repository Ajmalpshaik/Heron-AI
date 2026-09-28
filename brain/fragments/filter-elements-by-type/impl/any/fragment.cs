// NOT STANDALONE. Assumes `doc`, `exemplar`, `inViewOnly` and `includeLinks`
// are in scope; leaves `elements`, `found`, `linksSearched` and
// `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// `inViewOnly` IS A VIEW OR NULL, AND THAT IS THE INSTRUCTION.
//
//   a view   only what that view contains - "the ones I can see"
//   null     the whole model - "all of them"
//
// It is an input rather than a default because the two answers differ by an
// order of magnitude on a real project, and choosing silently is how a change
// meant for one floor reaches nine. A bool would have been the same decision
// with less information; passing the view means the scope is the thing itself.
//
// SAME TYPE, NOT SAME FAMILY AND NOT SAME CATEGORY. Two 600x600 diffusers of
// different types are indistinguishable on a plan and are different products.
// Category sweeps up everything; family mixes the sizes.
//
// THE EXEMPLAR IS INCLUDED in the result, because "everything like this one"
// includes this one - and a caller passing the result to a change expects it
// to change too.
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
// THE SAME TYPE IN A LINK IS FOUND BY ITS NAMES. A type is an element of its
// own document, so the exemplar's type id means nothing in a link. There the
// match is the same category, family name and type name - the three things a
// modeller reads off the Type Selector - and the answer says so.
//
// A VIEW IS READ THROUGH EACH PLACEMENT, AND ONLY FROM 2023 - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule. On 2020-2022 the
// answer says the links were not read, never the whole link as the view.

var elements = new List<Element>();

var typeId = exemplar == null ? ElementId.InvalidElementId : exemplar.GetTypeId();

if (typeId != null && typeId != ElementId.InvalidElementId)
{
    var collector = inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc);

    foreach (var candidate in collector.WhereElementIsNotElementType())
    {
        if (candidate == null) continue;
        if (candidate.GetTypeId() != typeId) continue;
        elements.Add(candidate);
    }
}

var found = elements.Count;

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

        var linkTypeId = instance.GetTypeId();
        if (linkTypeId == null || linkTypeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(linkTypeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(linkTypeId);
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

// The exemplar's names, the only part of its type a link can be asked about.
string exFamily = null;
string exType = null;
string exCategory = null;
if (typeId != null && typeId != ElementId.InvalidElementId)
{
    var hostType = doc.GetElement(typeId) as ElementType;
    if (hostType != null) { exFamily = hostType.FamilyName; exType = hostType.Name; }
    try { if (exemplar.Category != null) exCategory = exemplar.Category.Name; } catch (Exception) { }
}

if (linkBlocked.Length == 0 && exType == null && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no exemplar type to look for";

for (var i = 0; i < linkDocs.Count && linkBlocked.Length == 0; i++)
{
    var linked = linkDocs[i];
    unreadPlacements = 0;
    var inLink = linkedElementsOf(i);
    var count = 0;

    foreach (var candidate in inLink ?? new List<Element>())
    {
        string category = null;
        try { if (candidate.Category != null) category = candidate.Category.Name; }
        catch (Exception) { }
        if (!string.Equals(category, exCategory, StringComparison.OrdinalIgnoreCase)) continue;

        var linkedType = linked.GetElement(candidate.GetTypeId()) as ElementType;
        if (linkedType == null) continue;
        if (!string.Equals(linkedType.Name, exType, StringComparison.OrdinalIgnoreCase)) continue;
        if (!string.Equals(linkedType.FamilyName, exFamily, StringComparison.OrdinalIgnoreCase)) continue;
        count++;
    }

    linksSearched++;
    linkedTotal += count;
    linkedMatches.Add(string.Format("{0}: {1} of '{2} : {3}'{4}", linked.Title, count, exFamily,
        exType, unreadPlacements > 0
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
