// NOT STANDALONE. Assumes `doc` and `group` are in scope; leaves `elements`,
// `nestedGroups` and `findings` behind.
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
var nestedGroups = 0;
var findings = new List<string>();

var instance = group as Group;

if (group == null)
{
    findings.Add("No group was given - hand in the group instance to open");
}
else if (instance == null)
{
    findings.Add(string.Format("'{0}' (id {1}) is not a model group, so it has no members. If it is "
        + "an assembly, GROUP_BY_ASSEMBLY is the one that reads it",
        string.IsNullOrEmpty(group.Name) ? "That element" : group.Name, group.Id));
}
else
{
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
