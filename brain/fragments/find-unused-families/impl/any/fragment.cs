// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `wholeFamilyUnused` behind.
//
// READ ONLY. Opens no transaction, needs none, and DELETES NOTHING. That is the
// design, not a missing half: purging is hard to reverse, Revit already has one
// command that does it, and a fragment deleting a hundred types on a shared
// model would be the most damaging thing in this library.
//
// COUNTED IN ONE PASS OVER THE INSTANCES, not by asking each type whether
// anything uses it. A project with 2,000 types would otherwise mean 2,000
// collector passes, and the cost is what decides whether anybody runs this
// before an issue.
//
// GetTypeId() ON EVERY PLACED ELEMENT, not just on FamilyInstances. A wall, a
// duct and a pipe are placed elements whose type is a system family type, and
// counting only FamilyInstances would report every wall type in the project as
// unused.
//
// NESTED FAMILIES ARE NOT COUNTED AS USED BY THEIR PARENT, and this is the one
// place this report can mislead. A family placed only INSIDE another family has
// no instance in the project, so it comes back unused - and deleting it would
// break the parent. Revit's own Purge Unused knows the difference; this does
// not, and says so rather than being quietly wrong.

var elements = new List<Element>();
var findings = new List<string>();
var wholeFamilyUnused = new List<ElementId>();

// Every type that something placed is using, counted once over the instances.
var used = new Dictionary<ElementId, int>();

foreach (var element in new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType())
{
    if (element == null) continue;

    var typeId = element.GetTypeId();
    if (typeId == null || typeId == ElementId.InvalidElementId) continue;

    used[typeId] = (used.ContainsKey(typeId) ? used[typeId] : 0) + 1;
}

var families = new List<Family>();
foreach (var family in new FilteredElementCollector(doc)
                           .OfClass(typeof(Family))
                           .Cast<Family>())
{
    if (family != null) families.Add(family);
}

families.Sort(delegate (Family a, Family b)
{
    return string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase);
});

var idleTypes = 0;

foreach (var family in families)
{
    var symbolIds = family.GetFamilySymbolIds();
    if (symbolIds == null || symbolIds.Count == 0) continue;

    var idle = new List<string>();
    var inUse = 0;

    foreach (var symbolId in symbolIds)
    {
        if (used.ContainsKey(symbolId) && used[symbolId] > 0) { inUse++; continue; }

        var symbol = doc.GetElement(symbolId);
        if (symbol == null) continue;

        elements.Add(symbol);
        idle.Add(symbol.Name);
    }

    if (idle.Count == 0) continue;

    idleTypes += idle.Count;
    idle.Sort(delegate (string a, string b)
    {
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    });

    // A family where NOTHING is placed is the interesting row - the whole file
    // could go, rather than a few of its types.
    if (inUse == 0)
    {
        wholeFamilyUnused.Add(family.Id);
        findings.Add(string.Format("{0}  - NOTHING placed, all {1} type(s): {2}",
                                   family.Name, idle.Count,
                                   string.Join(", ", idle.ToArray())));
    }
    else
    {
        findings.Add(string.Format("{0}  - {1} of {2} type(s) unplaced: {3}",
                                   family.Name, idle.Count, inUse + idle.Count,
                                   string.Join(", ", idle.ToArray())));
    }
}

if (idleTypes > 0)
{
    findings.Add(string.Format(
        "{0} unplaced type(s) in {1} family/families. NOTHING WAS DELETED - this is "
        + "what Revit's Purge Unused would look at, and a family placed only INSIDE "
        + "another family appears here wrongly, because it has no instance of its own",
        idleTypes, wholeFamilyUnused.Count));
}
