// NOT STANDALONE. Assumes `doc`, `category`, `categories`, `categoryName`,
// `levelId`, `inViewOnly`, `notShowingIn`, `withoutLevel` and `includeLinks`
// are already in scope, and leaves `elements`, `unresolvedLevel`,
// `linksSearched`, `linkedMatches`, `perCategory`, `found`, `resolvedTo`,
// `nearMisses`, `examined`, `inModel`, `showing` and `findings` in scope for
// whatever is composed after it.
//
// VERSION 6 (2026-11-01, the review of the merges) - "WHAT HAS NO LEVEL" WITH
// NO CATEGORY IS ANSWERED AGAIN, OVER THE MODEL CATEGORIES ONLY. Version 5
// refused `withoutLevel` with nothing else named; SELECT_WITHOUT_LEVEL answered
// it, with every view, sheet and material in the answer and a warning. Here it
// is answered, and bounded: only elements of a MODEL category (CategoryType
// .Model) that are not views, materials, types, project information or
// view-specific are looked at - in a link as here - and `findings` and
// `resolvedTo` say so. A category named is still the bound, unchanged.
//
// VERSION 5 (2026-11-01) - SELECT_BY_LEVEL, SELECT_VISIBLE_IN_VIEW,
// SELECT_WITHOUT_LEVEL AND SELECT_HIDDEN_IN_VIEW MERGED IN, AND WHAT THEY HAD
// THAT VERSION 4 LACKED:
//
//   NO CATEGORY AT ALL (SELECT_BY_LEVEL, SELECT_VISIBLE_IN_VIEW). A level alone
//   is everything on that level; a view alone is everything that view draws.
//   The collector is then every category, and `findings` says it walked the
//   whole model. THE ONE REFUSAL LEFT for "nothing named" is no category, no
//   categoryName, no level and no view at all - nothing to bound the read.
//
//   A VIEW TEMPLATE IS REFUSED (SELECT_VISIBLE_IN_VIEW, SELECT_HIDDEN_IN_VIEW).
//   A template has no elements of its own; an empty answer from one reads as
//   "this view shows nothing", and as "everything is hidden" the other way.
//
//   A LEVEL ID THAT IS NOT A LEVEL IS REFUSED (SELECT_BY_LEVEL) - never the
//   plausible zero every comparison against it would give.
//
//   THE ELEMENTS WITH NO LEVEL, HANDED ON (SELECT_WITHOUT_LEVEL). `withoutLevel`
//   returns the ones whose level cannot be found - the ones `unresolvedLevel`
//   only counted. THE LOOKUP IS THIS CARD'S, row 5b-335's fixed one: a
//   parameter that holds no level is passed over and the next asked.
//   SELECT_WITHOUT_LEVEL's own four-step copy took the first parameter an
//   element HAD and never checked it named a level, which is the drift row
//   5b-335 fixed here. `examined` is how many were looked at.
//
//   THE MODEL MINUS WHAT A VIEW SHOWS (SELECT_HIDDEN_IN_VIEW). `notShowingIn`
//   returns what is in the model, in the categories named, and NOT drawn in
//   that view. `inModel` and `showing` are the two halves of the subtraction,
//   so the arithmetic can be checked. Links are not read for it - that card
//   never read them, and a link's view-scoped read is per placement.
//
//   `findings` - the sentences the four cards gave: how many of how many,
//   the whole-model warning when no category bounds the read, and that why
//   something is missing is DIAGNOSE_VISIBILITY's question, one element at a
//   time.
//
// VERSION 4, REVIEW FIX OF 2026-11-01 - THE TWO THINGS OF
// SELECT_BY_CATEGORY_NAME THE FIRST DRAFT LOST, put back in the card and not
// in the binder:
//
//   A CATEGORY BY THE NAME THE MODEL HOLDS - `categoryName`, matched against
//   doc.Settings.Categories, capitals ignored, exactly as that card matched
//   it. That list holds what no BuiltInCategory names: an IMPORTED CAD
//   file's category ("Site Plan.dwg") is matched by its name too, and read
//   by its id with OfCategoryId. In a link the category is found by the same
//   name in the link's own list.
//
//   THE NEAR MISSES. A name that matches nothing reads NOTHING and returns
//   the category names that nearly matched in `nearMisses` - one containing
//   the words, or contained in them - with `resolvedTo` saying no category
//   has that name. An empty set beside "no category called" is not the
//   zero that reads as "there are none", which is why the first draft
//   refused instead; this says which it is.
//
//   EXACTLY ONE WAY OF NAMING: `categoryName`, or `category` and/or
//   `categories` as before. `categoryName` beside either of the others is
//   refused, nothing read, and so is none of the three.
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
// per link placement. It is reached by reflection, chosen when the add-in
// compiled fragments without release symbols (row 5b-181) - since
// revit/Heron.Revit.Addin/HeronFragmentSymbols.cs it hands a fragment the
// symbols it was built with, and reflection is kept as the code that was
// written and needs no list of releases. On 2020-2022 it is absent and the
// answer says so rather than reading the whole link and calling it the view.
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

// THE NAME THE MODEL HOLDS (review fix). Left out it binds "none", the card's
// `absent:`, which names no category.
var byName = (categoryName ?? "").Trim();
if (string.Equals(byName, "none", StringComparison.OrdinalIgnoreCase)) byName = "";

if (byName.Length > 0 && wanted.Count > 0)
    throw new InvalidOperationException("A category was named twice over - categoryName \""
        + byName + "\" and category or categories as well - so NOTHING was read. Name it ONE way: "
        + "categoryName for the name the model holds, an imported CAD file's included, or "
        + "category / categories for Revit's own categories.");

// VERSION 5: a level alone, or a view alone, bounds the read too - everything
// on that level, everything that view draws. VERSION 6: so does `withoutLevel`
// alone - SELECT_WITHOUT_LEVEL's "what has no level", answered over the MODEL
// categories only (see `modelOnly` below). Refused only when NOTHING bounds it:
// an unbounded read of every category hands back every view, sheet and
// material in the model, and nobody asked for that.
var levelGiven = levelId != ElementId.InvalidElementId;
if (byName.Length == 0 && wanted.Count == 0 && !levelGiven && inViewOnly == null
    && notShowingIn == null && !withoutLevel)
    throw new InvalidOperationException("Nothing was named to look for, so NOTHING was read. "
        + "Name a category - Ducts, Mechanical Equipment, OST_DuctCurves - or several with "
        + "categories, separated by commas, or one by the name the model holds with "
        + "categoryName; or a level with levelId for everything on it; or a view with "
        + "inViewOnly for everything it draws, or notShowingIn for what it does not; or "
        + "withoutLevel for the model elements on no level.");

if (inViewOnly != null && notShowingIn != null)
    throw new InvalidOperationException("Two views were named - inViewOnly '" + inViewOnly.Name
        + "' and notShowingIn '" + notShowingIn.Name + "' - so NOTHING was read. Ask one: "
        + "what a view draws, or what the model holds that it does not.");

if (withoutLevel && levelGiven)
    throw new InvalidOperationException("withoutLevel asks for the elements that are on NO "
        + "level, and a level was named as well, so NOTHING was read. Ask one or the other.");

// A VIEW TEMPLATE IS NOT A VIEW (SELECT_VISIBLE_IN_VIEW, SELECT_HIDDEN_IN_VIEW).
foreach (var asked in new[] { inViewOnly, notShowingIn })
{
    if (asked != null && asked.IsTemplate)
        throw new InvalidOperationException("'" + asked.Name + "' is a view TEMPLATE, not a "
            + "view, so NOTHING was read. A template has no elements of its own - it is "
            + "settings applied to views - so an answer from it would read as \"this view "
            + "shows nothing\", or as everything in the model hidden. Name the view it is "
            + "applied to.");
}

// A LEVEL ID THAT IS NOT A LEVEL (SELECT_BY_LEVEL): refused with the reason,
// never the plausible zero every comparison against it would give.
Level askedLevel = null;
if (levelGiven)
{
    askedLevel = doc.GetElement(levelId) as Level;
    if (askedLevel == null)
        throw new InvalidOperationException("levelId " + levelId + " is not a level in "
            + doc.Title + ", so NOTHING was read. LIST_LEVELS gives the levels by name.");
}

// categoryName: matched against EVERY category the document holds, imported
// ones included, capitals ignored - SELECT_BY_CATEGORY_NAME's rule. Nothing
// matching reads nothing and gathers the near misses.
var nearMisses = new List<string>();
Category namedCategory = null;
if (byName.Length > 0)
{
    var everyName = new List<string>();
    try
    {
        foreach (Category held in doc.Settings.Categories)
        {
            if (held == null) continue;
            string heldName = "";
            try { heldName = held.Name ?? ""; } catch (Exception) { }
            if (heldName.Length == 0) continue;
            everyName.Add(heldName);
            if (namedCategory == null
                && string.Equals(heldName, byName, StringComparison.OrdinalIgnoreCase))
                namedCategory = held;
        }
    }
    catch (Exception) { }

    if (namedCategory == null)
    {
        foreach (var heldName in everyName)
        {
            if (heldName.IndexOf(byName, StringComparison.OrdinalIgnoreCase) >= 0
                || byName.IndexOf(heldName, StringComparison.OrdinalIgnoreCase) >= 0)
                nearMisses.Add(heldName);
        }
        nearMisses.Sort(StringComparer.OrdinalIgnoreCase);
    }
}
bool nameMissed = byName.Length > 0 && namedCategory == null;

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
if (namedCategory != null)
{
    perCategory[namedCategory.Name] = 0;
    resolvedNames.Add(namedCategory.Name);
}
// No category named at all (version 5): every category, and said so.
var everyCategory = namedCategory == null && wanted.Count == 0 && !nameMissed;

// VERSION 6 - "WHAT HAS NO LEVEL", WITH NO CATEGORY, OVER THE MODEL ONLY.
// SELECT_WITHOUT_LEVEL answered `withoutLevel` with no category list; version
// 5 refused it. Restored here, and bounded: with no category named, only
// elements of a MODEL category are looked at - Revit's own CategoryType.Model,
// the Model Categories tab of Visibility/Graphics. Not a view or a sheet, not
// a material, not a type (never collected), not project information, and not
// a view-specific element (a detail item, a filled region): every one of
// those correctly has no level, and an answer made of them is SELECT_WITHOUT_
// LEVEL's own "correct and useless". With a category named nothing changes -
// the category is the bound, as before.
var modelOnly = withoutLevel && everyCategory;
Func<Element, bool> isModelElement = e =>
{
    if (e == null || e is View || e is Material || e is ElementType || e is ProjectInfo)
        return false;
    try
    {
        if (e.ViewSpecific) return false;
        var c = e.Category;
        return c != null && c.CategoryType == CategoryType.Model;
    }
    catch (Exception) { return false; }
};
var resolvedTo = nameMissed
    ? "NO CATEGORY in " + doc.Title + " is called \"" + byName + "\", so nothing was read"
      + (nearMisses.Count > 0 ? " - the nearest names are in nearMisses" : " - and no name is near it")
    : everyCategory
        ? (withoutLevel ? "every MODEL category - none was named (withoutLevel, version 6)"
                        : "every category - none was named")
        : string.Join(", ", resolvedNames.ToArray());

// One category is OfCategory, as versions 1 to 3 read it; several are one
// multi-category filter, which cannot return an element twice. A category by
// the name the model holds is read by its id - an imported one has no
// BuiltInCategory. None named is no category filter at all (version 5).
Func<FilteredElementCollector, FilteredElementCollector> ofWanted = c =>
    namedCategory != null
        ? c.OfCategoryId(namedCategory.Id)
        : wanted.Count == 0
            ? c
            : wanted.Count == 1
                ? c.OfCategory(wanted[0])
                : c.WherePasses(new ElementMulticategoryFilter(wanted));

Func<Element, string> categoryOf = e =>
{
    string name = "(no category)";
    try { if (e.Category != null) name = e.Category.Name ?? name; }
    catch (Exception) { }
    return name;
};

// THE MODEL MINUS WHAT A VIEW SHOWS (version 5, SELECT_HIDDEN_IN_VIEW's
// subtraction). Both halves are counted - `inModel` and `showing` - so a
// reader can check the arithmetic rather than trust it.
var inModel = 0;
var showing = 0;
IList<Element> matching;
if (nameMissed)
{
    matching = new List<Element>();
}
else if (notShowingIn != null)
{
    var visible = new HashSet<ElementId>();
    foreach (var e in ofWanted(new FilteredElementCollector(doc, notShowingIn.Id))
                 .WhereElementIsNotElementType())
        if (e != null) visible.Add(e.Id);
    showing = visible.Count;

    var notShown = new List<Element>();
    foreach (var e in ofWanted(new FilteredElementCollector(doc)).WhereElementIsNotElementType())
    {
        if (e == null) continue;
        inModel++;
        if (!visible.Contains(e.Id)) notShown.Add(e);
    }
    matching = notShown;
}
else
{
    matching = ofWanted(inViewOnly != null
            ? new FilteredElementCollector(doc, inViewOnly.Id)
            : new FilteredElementCollector(doc))
        .WhereElementIsNotElementType()
        .ToElements();
}

// VERSION 6: with no category and withoutLevel, the model elements only.
if (modelOnly)
{
    var model = new List<Element>();
    foreach (var e in matching)
        if (isModelElement(e)) model.Add(e);
    matching = model;
}

// How many were looked at, so a small answer can be told from a small scan.
var examined = matching.Count;

// The level lookup can meet an element that will not say - read as no level.
Func<Element, ElementId> safeLevelOf = e =>
{
    try { return levelOf(e); }
    catch (Exception) { return ElementId.InvalidElementId; }
};

var unresolvedLevel = 0;
var elements = new List<Element>();

foreach (var e in matching)
{
    if (e == null) continue;
    if (withoutLevel)
    {
        // SELECT_WITHOUT_LEVEL's answer: the ones whose level cannot be found,
        // handed on rather than only counted.
        if (safeLevelOf(e) != ElementId.InvalidElementId) continue;
        unresolvedLevel++;
    }
    else if (levelGiven)
    {
        var at = safeLevelOf(e);
        if (at == ElementId.InvalidElementId) { unresolvedLevel++; continue; }
        if (at != levelId) continue;
    }

    elements.Add(e);
    var name = categoryOf(e);
    perCategory[name] = perCategory.ContainsKey(name) ? perCategory[name] + 1 : 1;
}

int found = elements.Count;

// ---- findings: the four merged cards' sentences (version 5) ----------------

var findings = new List<string>();
var scope = everyCategory ? ", across every category" : ", within " + resolvedTo;

if (nameMissed)
{
    findings.Add(resolvedTo);
}
else
{
    if (withoutLevel)
        findings.Add(string.Format("{0} of {1} element(s) examined have no level{2}{3}",
            found, examined,
            modelOnly ? ", across the MODEL categories only" : scope,
            modelOnly
                ? ". No category was named, so only elements of a model category were looked "
                  + "at - views, sheets, materials, types, project information and view-specific "
                  + "elements such as detail items were left out, because each of them correctly "
                  + "has no level. Give a category list to look at one kind"
                : ""));
    else if (levelGiven)
        findings.Add(string.Format("{0} of {1} scanned element(s) are on level '{2}'{3}. {4} carry "
            + "no level at all", found, examined, askedLevel.Name, scope, unresolvedLevel));
    else
        findings.Add(string.Format("{0} element(s){1}", found, scope));

    if (levelGiven && everyCategory)
        findings.Add("This walked the whole model. A category list bounds it, and is cheaper by "
            + "the size of the model");

    if (inViewOnly != null)
    {
        findings.Add(string.Format("Read through view '{0}'. This is what the view SHOWS, which "
            + "is not what the model contains - the crop, the view range, the phase filter, the "
            + "category switches, the view filters and the template all remove things that are "
            + "certainly there", inViewOnly.Name));
        findings.Add("Something expected and missing is a question for DIAGNOSE_VISIBILITY, which "
            + "takes one element and one view and says which mechanism hid it. This reports the "
            + "set and no reasons");
    }

    if (notShowingIn != null)
    {
        findings.Add(string.Format("{0} in the model, {1} showing on '{2}', so {3} are not{4}{5}",
            inModel, showing, notShowingIn.Name, inModel - showing, scope,
            everyCategory ? ". NO CATEGORY BOUNDED THIS, so every view, sheet, material and "
                + "template in the document counts as not showing - all true, none useful. Give "
                + "a category list" : ""));
        if (found > 0)
            findings.Add("WHY each one is missing is a question for DIAGNOSE_VISIBILITY, one "
                + "element at a time - this says which, never why");
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

if (linkDocs.Count > 0 && nameMissed)
{
    linkBlocked = "Links NOT read: the name matched no category here, so there was nothing to "
        + "look for";
}
else if (linkDocs.Count > 0 && notShowingIn != null)
{
    // SELECT_HIDDEN_IN_VIEW never read links, and what a host view does not
    // draw of a link is a per-placement question this does not answer.
    linkBlocked = "Links NOT read: what a view does NOT show is answered for this model only - "
        + "ask without notShowingIn to count the links";
}
else if (linkDocs.Count > 0 && levelId != ElementId.InvalidElementId && hostLevelName == null)
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

        // BY NAME IN THE LINK'S OWN LIST, for categoryName (review fix): the
        // link has its own categories, and an imported one's id here means
        // nothing there. The name is what both documents share.
        Func<FilteredElementCollector, FilteredElementCollector> ofWantedHere = ofWanted;
        var linkLacksName = false;
        if (namedCategory != null)
        {
            Category linkedCategory = null;
            try
            {
                foreach (Category held in linked.Settings.Categories)
                {
                    if (held == null) continue;
                    string heldName = "";
                    try { heldName = held.Name ?? ""; } catch (Exception) { }
                    if (string.Equals(heldName, namedCategory.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        linkedCategory = held;
                        break;
                    }
                }
            }
            catch (Exception) { }
            if (linkedCategory == null) linkLacksName = true;
            else
            {
                var linkedCategoryId = linkedCategory.Id;
                ofWantedHere = c => c.OfCategoryId(linkedCategoryId);
            }
        }

        if (linkLacksName)
        {
            // Nothing of that name in this link: counted 0 and said below.
        }
        else if (inViewOnly == null)
        {
            try
            {
                foreach (var e in ofWantedHere(new FilteredElementCollector(linked))
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
                    foreach (var e in ofWantedHere(through).WhereElementIsNotElementType())
                        if (e != null && seen.Add(e.Id)) linkedElements.Add(e);
                }
                catch (Exception) { unreadPlacements++; }
            }
        }

        var linkMatched = 0;
        var linkUnresolved = 0;
        var linkByCategory = new Dictionary<string, int>();

        // VERSION 6: the same model-only bound in a link as here.
        if (modelOnly)
            linkedElements = linkedElements.Where(isModelElement).ToList();

        foreach (var e in linkedElements)
        {
            if (withoutLevel)
            {
                // SELECT_WITHOUT_LEVEL's count, per link: the same lookup.
                if (safeLevelOf(e) != ElementId.InvalidElementId) continue;
            }
            else if (hostLevelName != null)
            {
                var at = safeLevelOf(e);
                if (at == ElementId.InvalidElementId) { linkUnresolved++; continue; }

                var linkedLevel = linked.GetElement(at) as Level;
                if (linkedLevel == null
                    || !string.Equals(linkedLevel.Name, hostLevelName, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            linkMatched++;
            var name = categoryOf(e);
            linkByCategory[name] = linkByCategory.ContainsKey(name) ? linkByCategory[name] + 1 : 1;
        }

        // The link's own breakdown, in the same words as the host's - only
        // when more than one category was asked, where it says something.
        var parts = new List<string>();
        if (wanted.Count > 1)
            foreach (var entry in linkByCategory) parts.Add(entry.Key + " " + entry.Value);

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1}{2}{3}{4}{5}", linked.Title, linkMatched,
            linkLacksName ? " (no category of that name in this link)"
            : withoutLevel ? string.Format(" of {0} have no level", linkedElements.Count)
            : hostLevelName == null ? "" : string.Format(" on a level named '{0}'", hostLevelName),
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
