// NOT STANDALONE. Assumes `doc` and `categories` are in scope; leaves
// `elements`, `purgeableNames`, `byCategory`, `findings`, `perFamily`,
// `wholeFamilyUnused` and `route` behind.
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
    if (scope.Count == 0) refusal = "No category here is named " + string.Join(", ", wanted) + ".";
}

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

var counts = new Dictionary<string, int>();

if (refusal == null && unusedList != null)
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
