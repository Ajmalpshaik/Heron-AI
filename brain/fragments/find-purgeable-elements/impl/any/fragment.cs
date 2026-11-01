// NOT STANDALONE. Assumes `doc`, `categories` and `includeMaterials` are in
// scope; leaves `elements`, `purgeableNames`, `byCategory`, `findings`,
// `perFamily`, `wholeFamilyUnused`, `route`, `heldBack`,
// `unusedGroupTypeNames`, `attachedDetailGroupsInUse`, `unusedImageNames`,
// `usedImageCount`, `unusedTemplateNames`, `unusedFilterNames`,
// `filtersOnlyInUnusedTemplates`, `templatesKeptAsDefault`,
// `unusedMaterialNames`, `scannedInstances`, `scannedTypes` and `unreadable`
// behind. VERSION 3 - see the block above `var counts`.
//
// READ ONLY - opens no transaction. Nothing is deleted here.
//
// WHAT PURGE UNUSED WOULD OFFER - in a project, or in a family in front -
// found by Revit's own list of unused elements, so nothing is decided here
// about what counts as used:
//
//   categories   category names, comma separated, to look in; EMPTY looks
//                in every category, as Purge Unused does
//
// The elements are handed on: DELETE_ELEMENTS removes them, after the
// modeller has seen the list. Purge is a ladder - removing a type can leave
// its material unused - so a second run after the delete finds the next rung.
//
// VERSION 2 (2026-11-01) - FIND_UNUSED_FAMILIES MERGED IN, AND WHAT IT HAD:
//
//   REVIT 2020 TO 2023. Revit opened its unused-element list to code in 2024
//   (Document.GetUnusedElements). It is reached by REFLECTION here, so this
//   one file compiles on every release and asks for the list where Revit has
//   it. Where Revit has not - 2020 to 2023 - FIND_UNUSED_FAMILIES' own count
//   is the stated fallback: every placed element's type counted in one pass,
//   and each loaded family's types with no placed instance reported.
//
//   THE FALLBACK IS A REPORT, NEVER HANDED ON FOR DELETING. It is not Revit's
//   list: a family placed only INSIDE another family has no instance of its
//   own, so it comes back unplaced - and deleting it would break the parent.
//   Revit's own list knows the difference; a count of instances does not. So
//   on the fallback `elements` and `purgeableNames` stay EMPTY, `route` says
//   which list answered, and the families are in `perFamily` and
//   `wholeFamilyUnused` as TEXT - names, never ids a delete could take. The
//   two merged cards had opposite rules here (one handed its list on, one
//   deleted nothing and said so); the safer one holds wherever the list is
//   not Revit's.
//
//   PER FAMILY, on both routes. One line per family with types unused, and
//   the families where NOTHING is used named apart - those are whole files
//   that could go.
//
//   On the fallback only loadable families are counted: materials, view
//   types, system family types and the rest Purge Unused offers are not, and
//   the findings say so.

var elements = new List<Element>();
var purgeableNames = new List<string>();
var findings = new List<string>();
var perFamily = new List<string>();
var wholeFamilyUnused = new List<string>();
var route = "";
string refusal = null;

var wanted = new HashSet<string>((categories ?? "").Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => t != ""));
var scope = new HashSet<ElementId>();
if (wanted.Count > 0)
{
    foreach (Category category in doc.Settings.Categories)
        if (wanted.Contains(category.Name.ToLowerInvariant())) scope.Add(category.Id);
    // Version 3: a kind of report that is no category of its own.
    var reportWords = new[] { "groups", "images", "templates", "view templates", "filters", "view filters" };
    if (scope.Count == 0 && !wanted.Any(w => reportWords.Contains(w)))
        refusal = "No category here is named " + string.Join(", ", wanted) + ".";
}
// Version 3: only report words named, and no category - Revit's list is NOT
// asked, because an empty scope would ask it for every category.
var onlyReports = refusal == null && wanted.Count > 0 && scope.Count == 0;

// Revit 2024 and later only - see the header. Null on 2020-2023.
var unusedList = typeof(Document).GetMethod("GetUnusedElements", new[] { typeof(ISet<ElementId>) });

// One family's line, the same words on both routes.
Action<string, List<string>, int> familyLine = (familyName, idle, total) =>
{
    idle.Sort(StringComparer.OrdinalIgnoreCase);
    if (idle.Count == total)
    {
        wholeFamilyUnused.Add(familyName);
        perFamily.Add(string.Format("{0}  - NOTHING used, all {1} type(s): {2}",
            familyName, idle.Count, string.Join(", ", idle.ToArray())));
    }
    else
    {
        perFamily.Add(string.Format("{0}  - {1} of {2} type(s) unused: {3}",
            familyName, idle.Count, total, string.Join(", ", idle.ToArray())));
    }
};


// ===========================================================================
// VERSION 3 (2026-11-01) - FIND_UNUSED_MATERIALS, FIND_UNUSED_GROUP_TYPES,
// FIND_UNUSED_IMAGES AND FIND_UNUSED_DEFINITIONS MERGED IN, each with its own
// rule. EVERY LIST BUILT HERE IS A REPORT - NAMES, NEVER HANDED ON FOR
// DELETING. Only Revit's own list (2024 and later) reaches `elements`; that is
// version 2's rule and the safer of the cards' rules (two of the retired cards
// handed ids on, two deleted nothing and handed nothing on).
//
// AND WHERE A CHECK HERE FINDS SOMETHING IN USE, REVIT'S LIST DOES NOT HAND IT
// ON EITHER. Each retired card was built around a use a naive count misses -
// a detail group attached to a placed model group, a template a view family
// type names as the default for new views, an image named by an image
// parameter, a material used only as paint. Anything one of those checks
// finds in use is held back from `elements`, and `heldBack` names it.
//
//   groups      a group type with nothing placed is unused - UNLESS a placed
//               model group offers it as an attached detail group, which is
//               in use with no instance of its own. A placed group that will
//               not say what it offers makes every unplaced DETAIL group
//               type count as in use, never as unused
//   images      an image is used if placed, or named by ANY element-id
//               parameter of any element or type (Image, Type Image, a shared
//               or family image parameter). One used only as a schedule's
//               header picture is not seen, and the finding says so
//   templates   a template no view follows is unused - UNLESS a view family
//               type names it as the default for new views, which is in use
//               with no view pointing at it
//   filters     used by no view or template, and apart from those, the ones
//               used ONLY by templates that are themselves unused
//   materials   OPT-IN, `includeMaterials`, because it walks every element
//               and every type: BOTH material sets of each - geometry and
//               PAINT - unioned, so a material used only as paint is in use
//
// Each runs on every release. With `categories` named, each runs only when its
// kind is among them: Model Groups or Detail Groups; Raster Images; Views for
// templates; Filters; Materials.
var askedEverything = wanted.Count == 0;
Func<string[], bool> askedFor = names => askedEverything || names.Any(n => wanted.Contains(n));
var inUseHere = new HashSet<ElementId>();
var heldBack = new List<string>();
var unusedGroupTypeNames = new List<string>();
var attachedDetailGroupsInUse = new List<string>();
var unusedImageNames = new List<string>();
var usedImageCount = 0;
var unusedTemplateNames = new List<string>();
var unusedFilterNames = new List<string>();
var filtersOnlyInUnusedTemplates = new List<string>();
var templatesKeptAsDefault = 0;
var unusedMaterialNames = new List<string>();
var scannedInstances = 0;
var scannedTypes = 0;
var unreadable = 0;
var reportLines = new List<string>();

if (refusal == null && askedFor(new[] { "model groups", "detail groups", "groups" }))
{
    // FIND_UNUSED_GROUP_TYPES. Which detail group types a PLACED group offers.
    var offeredByPlacedGroup = new HashSet<ElementId>();
    var groupsSilent = 0;
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(Group)))
    {
        var placedGroup = candidate as Group;
        if (placedGroup == null) continue;
        try
        {
            var attachedIds = placedGroup.GetAvailableAttachedDetailGroupTypeIds();
            if (attachedIds != null) foreach (var attachedId in attachedIds) offeredByPlacedGroup.Add(attachedId);
        }
        catch { groupsSilent++; }
    }
    var groupTypesWalked = 0;
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(GroupType)))
    {
        var groupType = candidate as GroupType;
        if (groupType == null) continue;
        groupTypesWalked++;
        var placedCount = 0;
        try { var placedSet = groupType.Groups; if (placedSet != null) placedCount = placedSet.Size; }
        catch { inUseHere.Add(groupType.Id); continue; }   // unreadable is not zero
        if (placedCount > 0) continue;
        var groupTypeName = string.IsNullOrEmpty(groupType.Name) ? "(no name)" : groupType.Name;
        var isDetail = groupType.Category != null
            && groupType.Category.Id == new ElementId(BuiltInCategory.OST_IOSDetailGroups);
        if (offeredByPlacedGroup.Contains(groupType.Id) || (isDetail && groupsSilent > 0))
        {
            attachedDetailGroupsInUse.Add(groupTypeName);
            inUseHere.Add(groupType.Id);
            continue;
        }
        unusedGroupTypeNames.Add(groupTypeName);
    }
    reportLines.Add(unusedGroupTypeNames.Count + " group definition(s) have nothing placed, of "
        + groupTypesWalked + " walked - a REPORT, by name.");
    if (attachedDetailGroupsInUse.Count > 0)
        reportLines.Add(attachedDetailGroupsInUse.Count + " detail group definition(s) have no instance of "
            + "their own and ARE in use - attached to placed model groups"
            + (groupsSilent > 0 ? ", or a placed group would not say what it offers" : "")
            + ". Kept out of every list: deleting one removes the detail that annotates a placed group.");
}

if (refusal == null && askedFor(new[] { "raster images", "images" }))
{
    // FIND_UNUSED_IMAGES. Placed, or named by any image parameter.
    var imageTypes = new Dictionary<ElementId, Element>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ImageType)))
        imageTypes[candidate.Id] = candidate;
    var imagesUsed = new HashSet<ElementId>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ImageInstance)))
        imagesUsed.Add(candidate.GetTypeId());
    Action<Element> readImageParameters = holder =>
    {
        try
        {
            foreach (Parameter p in holder.Parameters)
            {
                if (p == null || p.StorageType != StorageType.ElementId || !p.HasValue) continue;
                var named = p.AsElementId();
                if (named != null && imageTypes.ContainsKey(named)) imagesUsed.Add(named);
            }
        }
        catch { }
    };
    if (imageTypes.Count > 0)
    {
        foreach (var holder in new FilteredElementCollector(doc).WhereElementIsNotElementType()) readImageParameters(holder);
        foreach (var holder in new FilteredElementCollector(doc).WhereElementIsElementType()) readImageParameters(holder);
    }
    foreach (var pair in imageTypes)
    {
        if (imagesUsed.Contains(pair.Key)) { inUseHere.Add(pair.Key); continue; }
        unusedImageNames.Add(pair.Value.Name);
    }
    usedImageCount = imageTypes.Count - unusedImageNames.Count;
    reportLines.Add(imageTypes.Count + " image(s) in Manage Images; " + unusedImageNames.Count + " used nowhere"
        + " - a REPORT, by name. An image used only as a schedule header is not seen by this search - "
        + "check those before deleting.");
}

if (refusal == null && askedFor(new[] { "views", "view templates", "templates", "filters", "view filters" }))
{
    // FIND_UNUSED_DEFINITIONS. Templates no view follows and no view family
    // type names as a default; filters nothing uses, and those only unused
    // templates use.
    var allViews = new List<View>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var aView = candidate as View;
        if (aView != null) allViews.Add(aView);
    }
    var followed = new HashSet<ElementId>();
    foreach (var aView in allViews)
    {
        if (aView.IsTemplate) continue;
        try { if (aView.ViewTemplateId != ElementId.InvalidElementId) followed.Add(aView.ViewTemplateId); } catch { }
    }
    var defaults = new HashSet<ElementId>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
    {
        var viewType = candidate as ViewFamilyType;
        if (viewType == null) continue;
        try
        {
            var defaultId = viewType.DefaultTemplateId;
            if (defaultId != null && defaultId != ElementId.InvalidElementId) defaults.Add(defaultId);
        }
        catch { }
    }
    var unusedTemplateIds = new HashSet<ElementId>();
    foreach (var aView in allViews)
    {
        if (!aView.IsTemplate || followed.Contains(aView.Id)) continue;
        if (defaults.Contains(aView.Id)) { templatesKeptAsDefault++; inUseHere.Add(aView.Id); continue; }
        unusedTemplateIds.Add(aView.Id);
        string templateName = "";
        try { templateName = aView.Name ?? ""; } catch { }
        unusedTemplateNames.Add(templateName);
    }
    var byRealView = new HashSet<ElementId>();
    var byTemplate = new HashSet<ElementId>();
    var templateFilters = new Dictionary<ElementId, ICollection<ElementId>>();
    foreach (var aView in allViews)
    {
        ICollection<ElementId> viewFilters = null;
        try { viewFilters = aView.GetFilters(); } catch { }
        if (viewFilters == null) continue;
        if (aView.IsTemplate) templateFilters[aView.Id] = viewFilters;
        foreach (var filterId in viewFilters)
        {
            if (aView.IsTemplate) byTemplate.Add(filterId);
            else byRealView.Add(filterId);
        }
    }
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
    {
        if (candidate == null) continue;
        string filterName = "";
        try { filterName = candidate.Name ?? ""; } catch { }
        if (!byRealView.Contains(candidate.Id) && !byTemplate.Contains(candidate.Id))
        {
            unusedFilterNames.Add(filterName);
            continue;
        }
        if (byRealView.Contains(candidate.Id)) continue;
        var everyUserUnused = true;
        foreach (var pair in templateFilters)
        {
            if (!pair.Value.Contains(candidate.Id)) continue;
            if (!unusedTemplateIds.Contains(pair.Key)) { everyUserUnused = false; break; }
        }
        if (everyUserUnused) filtersOnlyInUnusedTemplates.Add(filterName);
    }
    reportLines.Add(unusedTemplateNames.Count + " view template(s) applied to no view, "
        + unusedFilterNames.Count + " view filter(s) used by nothing, and "
        + filtersOnlyInUnusedTemplates.Count + " more used ONLY by templates that are themselves unused"
        + " - a REPORT, by name.");
    if (templatesKeptAsDefault > 0)
        reportLines.Add(templatesKeptAsDefault + " template(s) no view follows are still IN USE - a view "
            + "family type names each as the template new views of that kind start with. Kept out of "
            + "every list: deleting one changes what every new view of that kind is created with.");
}

if (refusal == null && includeMaterials && askedFor(new[] { "materials" }))
{
    // FIND_UNUSED_MATERIALS. Both sets, every element and every type.
    var materialsUsed = new HashSet<ElementId>();
    Action<Element> harvest = holder =>
    {
        var readAnything = false;
        try
        {
            var geometryIds = holder.GetMaterialIds(false);
            if (geometryIds != null) foreach (var materialId in geometryIds) materialsUsed.Add(materialId);
            readAnything = true;
        }
        catch { }
        try
        {
            var paintIds = holder.GetMaterialIds(true);
            if (paintIds != null) foreach (var materialId in paintIds) materialsUsed.Add(materialId);
            readAnything = true;
        }
        catch { }
        if (!readAnything) unreadable++;
    };
    foreach (var holder in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        if (holder == null) continue;
        scannedInstances++;
        harvest(holder);
    }
    foreach (var holder in new FilteredElementCollector(doc).WhereElementIsElementType())
    {
        if (holder == null) continue;
        scannedTypes++;
        harvest(holder);
    }
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(Material)))
    {
        if (candidate == null) continue;
        if (materialsUsed.Contains(candidate.Id)) { inUseHere.Add(candidate.Id); continue; }
        unusedMaterialNames.Add(string.IsNullOrEmpty(candidate.Name) ? "(no name)" : candidate.Name);
    }
    reportLines.Add(unusedMaterialNames.Count + " material(s) referenced by nothing, of " + scannedInstances
        + " placed element(s) and " + scannedTypes + " type(s) walked, geometry AND paint read on every "
        + "one - a REPORT, by name.");
    if (unreadable > 0)
        reportLines.Add(unreadable + " element(s) would not report their materials at all, so the "
            + "materials list is a starting point rather than a proof on this model.");
}
else if (refusal == null && askedFor(new[] { "materials" }))
{
    reportLines.Add("Materials were not walked for use - it reads every element and every type, so it "
        + "is asked for with includeMaterials=true.");
}

var counts = new Dictionary<string, int>();

if (onlyReports)
{
    route = "the reports named only - no category was named, so Revit's own list was not asked";
}
else if (refusal == null && unusedList != null)
{
    // ---- Revit's own list, 2024 and later ---------------------------------
    route = "Revit's own unused-element list (Revit 2024 and later)";

    var unused = unusedList.Invoke(doc, new object[] { scope }) as IEnumerable<ElementId>;
    var unusedIds = new HashSet<ElementId>();
    if (unused != null)
        foreach (var id in unused) if (id != null) unusedIds.Add(id);

    // Per family: the unused types of each, and a Family Revit lists itself.
    var byFamily = new Dictionary<ElementId, List<string>>();
    var familiesListed = new HashSet<ElementId>();
    foreach (var id in unusedIds)
    {
        var element = doc.GetElement(id);
        if (element == null) continue;
        // VERSION 3: a check above found it IN USE - never handed on.
        if (inUseHere.Contains(id))
        {
            heldBack.Add((element.Category != null ? element.Category.Name : element.GetType().Name)
                + ": " + element.Name);
            continue;
        }
        elements.Add(element);
        var group = element.Category != null ? element.Category.Name : element.GetType().Name;
        counts[group] = counts.ContainsKey(group) ? counts[group] + 1 : 1;
        if (purgeableNames.Count < 300) purgeableNames.Add(group + ": " + element.Name);

        if (element is Family)
        {
            familiesListed.Add(element.Id);
            if (!byFamily.ContainsKey(element.Id)) byFamily[element.Id] = new List<string>();
            continue;
        }
        var symbol = element as FamilySymbol;
        if (symbol == null || symbol.Family == null) continue;
        var familyId = symbol.Family.Id;
        if (!byFamily.ContainsKey(familyId)) byFamily[familyId] = new List<string>();
        byFamily[familyId].Add(symbol.Name);
    }

    var listedFamilies = new List<Family>();
    foreach (var familyId in byFamily.Keys)
    {
        var family = doc.GetElement(familyId) as Family;
        if (family != null) listedFamilies.Add(family);
    }
    listedFamilies.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));

    foreach (var family in listedFamilies)
    {
        var idle = byFamily[family.Id];
        var symbolIds = family.GetFamilySymbolIds();
        var total = symbolIds == null ? 0 : symbolIds.Count;

        // A Family Revit lists as unused is unused whole, whatever of its
        // types the list also names.
        if (familiesListed.Contains(family.Id) && symbolIds != null)
        {
            foreach (var symbolId in symbolIds)
            {
                var symbol = doc.GetElement(symbolId);
                if (symbol != null && !idle.Contains(symbol.Name)) idle.Add(symbol.Name);
            }
            total = idle.Count;
        }
        if (idle.Count == 0)
        {
            if (familiesListed.Contains(family.Id))
            {
                wholeFamilyUnused.Add(family.Name ?? "");
                perFamily.Add(string.Format("{0}  - NOTHING used, no types", family.Name ?? ""));
            }
            continue;
        }
        familyLine(family.Name ?? "", idle, Math.Max(total, idle.Count));
    }
}
else if (refusal == null)
{
    // ---- FIND_UNUSED_FAMILIES' count, Revit 2020 to 2023 -------------------
    route = "placed-instance count (Revit before 2024 has no unused list) - a REPORT, "
        + "nothing handed on";

    // Every type something placed is using, counted once over the instances -
    // on EVERY placed element, not only FamilyInstances, or every wall type
    // would read as unused.
    var used = new HashSet<ElementId>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        if (element == null) continue;
        var typeId = element.GetTypeId();
        if (typeId != null && typeId != ElementId.InvalidElementId) used.Add(typeId);
    }

    var families = new List<Family>();
    foreach (var family in new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>())
    {
        if (family == null) continue;
        if (scope.Count > 0)
        {
            Category familyCategory = null;
            try { familyCategory = family.FamilyCategory; } catch (Exception) { familyCategory = null; }
            if (familyCategory == null || !scope.Contains(familyCategory.Id)) continue;
        }
        families.Add(family);
    }
    families.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));

    var idleTypes = 0;
    foreach (var family in families)
    {
        var symbolIds = family.GetFamilySymbolIds();
        if (symbolIds == null || symbolIds.Count == 0) continue;

        var idle = new List<string>();
        foreach (var symbolId in symbolIds)
        {
            if (used.Contains(symbolId)) continue;
            var symbol = doc.GetElement(symbolId);
            if (symbol != null) idle.Add(symbol.Name);
        }
        if (idle.Count == 0) continue;

        idleTypes += idle.Count;
        var group = family.FamilyCategory != null ? family.FamilyCategory.Name : "Family";
        counts[group] = counts.ContainsKey(group) ? counts[group] + idle.Count : idle.Count;
        familyLine(family.Name, idle, symbolIds.Count);
    }
}

var byCategory = string.Join(", ", counts.OrderByDescending(p => p.Value).Select(p => p.Key + " " + p.Value));
if (refusal != null)
{
    findings.Add(refusal);
}
else if (onlyReports)
{
    findings.Add("Only the reports named were run, and NOTHING is handed on for deleting - every list "
        + "below is by name.");
}
else if (unusedList != null)
{
    findings.Add(elements.Count + " element(s) Purge Unused would offer in \"" + doc.Title + "\""
        + (elements.Count > 0 ? ": " + byCategory + ". Nothing deleted - DELETE_ELEMENTS removes them, then run this again for the next rung." : "."));
    if (elements.Count > purgeableNames.Count) findings.Add("The names list stops at " + purgeableNames.Count + "; every element is handed on.");
}
else
{
    var idleCount = counts.Values.Sum();
    findings.Add(idleCount + " loaded family type(s) with no placed instance in \"" + doc.Title + "\""
        + (idleCount > 0 ? ", in " + perFamily.Count + " family/families (" + wholeFamilyUnused.Count
            + " with nothing placed at all): " + byCategory : "") + ".");
    findings.Add("THIS IS A REPORT, AND NOTHING IS HANDED ON FOR DELETING. Revit before 2024 does "
        + "not give its unused-element list to code, so this counted placed instances instead - "
        + "and a family placed only INSIDE another family has no instance of its own, so it "
        + "appears here wrongly, and deleting it would break the parent. Revit's own Manage > "
        + "Purge Unused knows the difference.");
    findings.Add("Only loadable families were counted - materials, view types, system family "
        + "types and the rest Purge Unused offers were not read on this release.");
}

// VERSION 3 - the retired cards' reports, and what Revit's list was not let
// hand on.
if (refusal == null)
{
    findings.AddRange(reportLines);
    if (reportLines.Count > 0)
        findings.Add("Every list by name above is a REPORT - nothing in it is handed on for deleting. "
            + "Unused is not the same as safe to remove: a project keeps some on purpose.");
    if (heldBack.Count > 0)
        findings.Add(heldBack.Count + " element(s) on Revit's own list were HELD BACK, not handed on, "
            + "because a check here finds them in use: " + string.Join(", ", heldBack.Take(20).ToArray())
            + (heldBack.Count > 20 ? ", and more" : "") + ".");
}
