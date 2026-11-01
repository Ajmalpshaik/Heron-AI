// NOT STANDALONE. Assumes `doc`, `category`, `categories`, `levelId`,
// `inViewOnly` and `includeLinks` are already in scope, and leaves `elements`,
// `unresolvedLevel`, `linksSearched`, `linkedMatches`, `perCategory`, `found`
// and `resolvedTo` in scope for whatever is composed after it.
//
// VERSION 4 (2026-11-01) - SELECT_BY_CATEGORY_NAME AND SELECT_BY_CATEGORIES
// MERGED IN, AND WHAT THEY HAD THAT THIS LACKED:
//
//   SEVERAL CATEGORIES IN ONE PASS (SELECT_BY_CATEGORIES). `categories` names
//   them, comma separated - "the ductwork" is five categories and one answer.
//   One ElementMulticategoryFilter asks once, so no element can come back
//   twice. `category` still names one, exactly as before, and the two may be
//   given together. NEITHER GIVEN IS REFUSED, here, before anything is read:
//   an empty list falling through to "every category" would hand back the
//   whole model, and an answer of 0 would read as "there are none".
//
//   THE BREAKDOWN (SELECT_BY_CATEGORIES). `perCategory` has one entry per
//   category ASKED FOR, by Revit's own name, and one asked for and empty is
//   there at 0 - the absence is the finding. `found` is the total.
//
//   ONE VIEW (both). `inViewOnly` narrows the read to what that view draws;
//   left out it is null, the whole model, as version 3 always read.
//
//   THE LEVEL MAY BE LEFT OUT. `levelId` left out reads as none - the whole
//   model - which is what every caller of version 3 typed `levelId=none` for.
//
//   WHICH CATEGORY THE WORDS FOUND (SELECT_BY_CATEGORY_NAME). `resolvedTo` is
//   Revit's own name for each category collected, so a word that bound to a
//   different category than the one meant shows in the answer. The WORD is
//   turned into a category by the add-in's binder before this runs - the
//   name Visibility/Graphics shows first, localised as the model is, then the
//   API name - and a word that is no category is refused THERE, with nothing
//   read (row 5b-444's route, tests/test_category_by_name.py).
//
//   LINKS THROUGH A VIEW, ONLY FROM 2023 (both). See below.
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
// A PARAMETER THAT HOLDS NO LEVEL IS PASSED OVER, NEVER ANSWERED WITH. An
// element can carry several of these and fill only some. Until 2026-10-06 the
// first one an element HAD answered, empty or not - and three work plane-based
// diffusers with FAMILY_LEVEL_PARAM empty and SCHEDULE_LEVEL_PARAM on Level 1
// were on no level at all: "matching 3", "elements 0", and the loads' Finalize
// wrote a flow to none of them (FRAGMENT-ISSUES 5b-335). So each is asked in
// turn, and the first that names a LEVEL answers.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements` or `perCategory`: that list feeds
// the next fragment in a chain, and the chain revives ids against the HOST
// document, so a linked id that happens to be in use in the host binds an
// unrelated element silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// A VIEW IS READ THROUGH EACH LINK PLACEMENT, AND ONLY FROM 2023. Revit 2023
// added a collector that reads a link's elements as a host view draws them,
// per link placement. It is reached by reflection because the add-in compiles
// fragments without release symbols (row 5b-181); on 2020-2022 it is absent
// and the answer says so rather than reading the whole link and calling it
// the view.
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. A link inside a link
// is a real case D-59 left open; the owner decided on 2026-09-28 to keep them
// counted and unread for now (FRAGMENT-ISSUES row 5b-253).
//
// A LINK'S LEVELS ARE ITS OWN, SO THE LEVEL IS MATCHED BY NAME THERE. `levelId`
// names a level in THIS model; in a link the same id means nothing, or another
// element. The host level's NAME is what the modeller chose, and it is what
// the link is asked for - through a view as well as without one - and the
// answer says so, because two models can disagree about what "Level 2" is.

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
        if (p == null || !p.HasValue || p.StorageType != StorageType.ElementId) continue;

        var held = p.AsElementId();
        if (held == null || held == ElementId.InvalidElementId) continue;

        // A level of the element's OWN document - a linked element's levels
        // are its link's, and that is where the caller below looks it up.
        if (e.Document.GetElement(held) is Level) return held;
    }
    return ElementId.InvalidElementId;
};

// ---- which categories: `category`, `categories`, or both -------------------
//
// Left out, each binds BuiltInCategory.INVALID (the card's `absent: INVALID`),
// which names no category and is dropped here. Named twice, a category is
// asked for once.

var wanted = new List<BuiltInCategory>();
if (category != BuiltInCategory.INVALID) wanted.Add(category);
if (categories != null)
{
    foreach (var one in categories)
        if (one != BuiltInCategory.INVALID && !wanted.Contains(one)) wanted.Add(one);
}

if (wanted.Count == 0)
    throw new InvalidOperationException("No category was named, so NOTHING was read. Name one "
        + "with category - Ducts, Mechanical Equipment, OST_DuctCurves - or several with "
        + "categories, separated by commas - Ducts, Duct Fittings, Duct Accessories.");

// Every category asked for starts at zero, so one with nothing in it is still
// in the report - by the name Revit shows, which is what the reader uses.
var perCategory = new Dictionary<string, int>();
var resolvedNames = new List<string>();
foreach (var one in wanted)
{
    string name = null;
    try
    {
        var definition = Category.GetCategory(doc, one);
        if (definition != null) name = definition.Name;
    }
    catch (Exception) { name = null; }
    if (string.IsNullOrEmpty(name)) name = one.ToString();
    if (!perCategory.ContainsKey(name))
    {
        perCategory[name] = 0;
        resolvedNames.Add(name);
    }
}
var resolvedTo = string.Join(", ", resolvedNames.ToArray());

// One category is OfCategory, as versions 1 to 3 read it; several are one
// multi-category filter, which cannot return an element twice.
Func<FilteredElementCollector, FilteredElementCollector> ofWanted = c =>
    wanted.Count == 1
        ? c.OfCategory(wanted[0])
        : c.WherePasses(new ElementMulticategoryFilter(wanted));

Func<Element, string> categoryOf = e =>
{
    string name = "(no category)";
    try { if (e.Category != null) name = e.Category.Name ?? name; }
    catch (Exception) { }
    return name;
};

var matching = ofWanted(inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc))
    .WhereElementIsNotElementType()
    .ToElements();

var unresolvedLevel = 0;
var elements = new List<Element>();

foreach (var e in matching)
{
    if (levelId != ElementId.InvalidElementId)
    {
        var at = levelOf(e);
        if (at == ElementId.InvalidElementId) { unresolvedLevel++; continue; }
        if (at != levelId) continue;
    }

    elements.Add(e);
    var name = categoryOf(e);
    perCategory[name] = perCategory.ContainsKey(name) ? perCategory[name] + 1 : 1;
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

// The host level's name, the only thing about it a link can be asked.
string hostLevelName = null;
if (levelId != ElementId.InvalidElementId)
{
    var hostLevel = doc.GetElement(levelId) as Level;
    if (hostLevel != null) hostLevelName = hostLevel.Name;
}

// Revit 2023 and later only - see the header. Null on 2020-2022, and never
// asked for without a view.
var linkThroughView = inViewOnly == null ? null
    : typeof(FilteredElementCollector).GetConstructor(
        new[] { typeof(Document), typeof(ElementId), typeof(ElementId) });

if (linkDocs.Count > 0 && levelId != ElementId.InvalidElementId && hostLevelName == null)
{
    linkBlocked = "Links NOT read: the level asked for has no name here to look for";
}
else if (linkDocs.Count > 0 && inViewOnly != null && linkThroughView == null)
{
    linkBlocked = "Links NOT read: this Revit cannot read a link through a view "
        + "(2023 and later can) - ask without the view";
}
else
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
                foreach (var e in ofWanted(new FilteredElementCollector(linked))
                             .WhereElementIsNotElementType())
                    if (e != null) linkedElements.Add(e);
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
                    foreach (var e in ofWanted(through).WhereElementIsNotElementType())
                        if (e != null && seen.Add(e.Id)) linkedElements.Add(e);
                }
                catch (Exception) { unreadPlacements++; }
            }
        }

        var linkMatched = 0;
        var linkUnresolved = 0;
        var byName = new Dictionary<string, int>();

        foreach (var e in linkedElements)
        {
            if (hostLevelName != null)
            {
                var at = levelOf(e);
                if (at == ElementId.InvalidElementId) { linkUnresolved++; continue; }

                var linkedLevel = linked.GetElement(at) as Level;
                if (linkedLevel == null
                    || !string.Equals(linkedLevel.Name, hostLevelName, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            linkMatched++;
            var name = categoryOf(e);
            byName[name] = byName.ContainsKey(name) ? byName[name] + 1 : 1;
        }

        // The link's own breakdown, in the same words as the host's - only
        // when more than one category was asked, where it says something.
        var parts = new List<string>();
        if (wanted.Count > 1)
            foreach (var entry in byName) parts.Add(entry.Key + " " + entry.Value);

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1}{2}{3}{4}{5}", linked.Title, linkMatched,
            hostLevelName == null ? "" : string.Format(" on a level named '{0}'", hostLevelName),
            parts.Count > 0 ? " (" + string.Join(", ", parts.ToArray()) + ")" : "",
            linkUnresolved > 0 ? string.Format(", {0} with no level found", linkUnresolved) : "",
            unreadPlacements > 0
                ? string.Format(" ({0} placement(s) could not be read through this view)",
                    unreadPlacements)
                : ""));
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
