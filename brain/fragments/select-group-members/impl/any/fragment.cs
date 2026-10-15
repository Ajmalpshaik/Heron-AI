// NOT STANDALONE. Assumes `doc` and `group` are in scope; leaves `elements`,
// `containerKind`, `nestedGroups` and `findings` behind.
//
// VERSION 2 OPENS AN ASSEMBLY TOO. `group` may be an assembly instance: it is a
// container the collector also returns whole, and it is opened the same way, by
// the instance. `containerKind` says which of the two was opened.
//
// A GROUP IS ONE ELEMENT TO EVERY OTHER FILTER IN THIS LIBRARY. A collector
// returns the group, never the twelve things inside it, so every count and
// every sweep sees it as a single item. This is the fragment that opens it.
//
// READING IS SAFE; WRITING TO WHAT COMES OUT USUALLY IS NOT. Revit refuses an
// in-place edit of a member, or applies it to every instance of the group type.
// The warning belongs here, where the set is produced, rather than where
// somebody is surprised by it.
//
// AN INSTANCE IS TAKEN, NOT A NAME. A group's name is its TYPE's name and every
// placed copy shares it, which is the whole point of groups - so a name lookup
// would be ambiguous exactly when groups are being used properly.
//
// NESTED GROUPS COME BACK AS GROUPS, one element each. Flattening silently
// would make this disagree with what Revit shows and would hide a structure
// somebody built on purpose.

var elements = new List<Element>();
var containerKind = "";
var nestedGroups = 0;
var findings = new List<string>();

var instance = group as Group;
var assembly = group as AssemblyInstance;

if (group == null)
{
    findings.Add("No group or assembly was given - hand in the instance to open");
}
else if (assembly != null)
{
    containerKind = "assembly";
    var ids = assembly.GetMemberIds();
    var listed = ids == null ? 0 : ids.Count;
    var gone = 0;

    if (ids != null)
    {
        foreach (var id in ids)
        {
            var member = doc.GetElement(id);
            if (member == null) { gone++; continue; }
            if (member is Group) nestedGroups++;
            elements.Add(member);
        }
    }

    var namingCategory = "";
    var categoryId = assembly.NamingCategoryId;
    if (categoryId != null && categoryId != ElementId.InvalidElementId)
    {
        var category = Category.GetCategory(doc, categoryId);
        if (category != null) namingCategory = category.Name;
    }

    findings.Add(string.Format("Assembly id {0}{1}{2} holds {3} member(s){4}",
        assembly.Id,
        string.IsNullOrEmpty(assembly.AssemblyTypeName) ? ""
            : string.Format(", of assembly type '{0}'", assembly.AssemblyTypeName),
        string.IsNullOrEmpty(namingCategory) ? ""
            : string.Format(", named by the {0} category", namingCategory),
        elements.Count,
        listed == elements.Count ? "" : string.Format(" - {0} member id(s) listed, {1} no longer exist",
            listed, gone)));

    if (nestedGroups > 0)
        findings.Add(string.Format("{0} of those are model GROUPS, returned whole rather than "
            + "flattened. Hand one back in to open it", nestedGroups));

    if (elements.Count > 0)
        findings.Add("These are safe to read. CHANGING one changes the assembly: Revit may then find "
            + "it no longer matches the other instances of its type and asks for a new type, or the "
            + "edit has to be made in Edit Assembly. Its shop drawing views follow the members");
}
else if (instance == null)
{
    findings.Add(string.Format("'{0}' (id {1}) is neither a model group nor an assembly, so it has "
        + "no members to open. A nested family's parts are GROUP_BY_ASSEMBLY's job",
        string.IsNullOrEmpty(group.Name) ? "That element" : group.Name, group.Id));
}
else
{
    containerKind = "group";
    var ids = instance.GetMemberIds();
    var listed = ids == null ? 0 : ids.Count;
    var gone = 0;

    if (ids != null)
    {
        foreach (var id in ids)
        {
            var member = doc.GetElement(id);
            if (member == null) { gone++; continue; }
            if (member is Group) nestedGroups++;
            elements.Add(member);
        }
    }

    var typeName = "";
    var groupType = doc.GetElement(instance.GetTypeId()) as GroupType;
    if (groupType != null) typeName = groupType.Name;

    findings.Add(string.Format("Group instance id {0}{1} holds {2} member(s){3}",
        instance.Id,
        string.IsNullOrEmpty(typeName) ? "" : string.Format(", of group type '{0}'", typeName),
        elements.Count,
        listed == elements.Count ? "" : string.Format(" - {0} member id(s) listed, {1} no longer exist",
            listed, gone)));

    if (nestedGroups > 0)
        findings.Add(string.Format("{0} of those are themselves GROUPS, returned whole rather than "
            + "flattened. Hand one back in to open it", nestedGroups));

    if (elements.Count > 0)
        findings.Add("These are safe to read. CHANGING one is not: Revit either refuses an in-place "
            + "edit of a group member or applies the change to every instance of this group type. "
            + "UNGROUP_ELEMENTS is what is really wanted when the next step is to edit them");
}
