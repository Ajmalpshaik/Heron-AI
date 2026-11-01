// NOT STANDALONE. Assumes `doc`, `containers` and `recursive` are in scope;
// leaves `elements`, `containerKind`, `nestedGroups`, `withoutSubComponents`
// and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// VERSION 3 (2026-11-01) - SELECT_SUBCOMPONENTS MERGED IN. THREE KINDS OF
// CONTAINER, each opened by the INSTANCE handed in - the selection, or the set
// a chain hands on - never by a name several copies share:
//
//   a model GROUP      its members. NESTED GROUPS COME BACK AS GROUPS, one
//                      element each, NOT FLATTENED - flattening silently would
//                      make this disagree with what Revit shows and hide a
//                      structure somebody built on purpose. Counted in
//                      `nestedGroups`; hand one back in to open it
//   an ASSEMBLY        its members, the same way, with its type name and its
//                      naming category (version 2)
//   a FAMILY INSTANCE  its NESTED shared families (version 3, from
//                      SELECT_SUBCOMPONENTS). These GO DEEP BY DEFAULT -
//                      `recursive` left out is true - because a part four
//                      levels down is still a part, and the depth reached is
//                      reported. Every id is visited once, so a family that
//                      refers back to itself cannot loop. `recursive` false is
//                      one level only. A parent with none is counted in
//                      `withoutSubComponents` - the ordinary case, never a
//                      failed read
//
// THE DEPTH RULES ARE OPPOSITE, AND EACH IS KEPT FOR ITS OWN KIND: a group is
// opened one level because a nested group is a thing the modeller placed and
// edits as one; a nested family is followed down because nothing in a
// category sweep ever returns it, and a count that stops one level down is
// short with nothing to say so.
//
// Anything else handed in - a wall, a duct, a system family - is named and
// counted, never skipped in silence. Each member comes back once, however
// many containers handed in hold it.
//
// A GROUP IS ONE ELEMENT TO EVERY OTHER FILTER IN THIS LIBRARY, and a nested
// family is not in any sweep at all - a collector returns the group, or the
// parent. This is the fragment that opens them.
//
// READING IS SAFE; WRITING TO WHAT COMES OUT USUALLY IS NOT. Revit refuses an
// in-place edit of a group member, or applies it to every instance of the
// group type. The warning belongs here, where the set is produced.

var elements = new List<Element>();
var containerKind = "";
var nestedGroups = 0;
var withoutSubComponents = 0;
var findings = new List<string>();

if (containers == null || containers.Count == 0)
{
    findings.Add("No group, assembly or family instance was given - select the one to open, or hand "
        + "them on, and run this again");
}
else
{
    var kinds = new List<string>();
    // Each member once, whichever container held it.
    var taken = new List<ElementId>();
    Func<Element, bool> take = member =>
    {
        if (member == null) return false;
        foreach (var id in taken) if (id == member.Id) return false;
        taken.Add(member.Id);
        elements.Add(member);
        if (member is Group) nestedGroups++;
        return true;
    };

    var opened = new List<ElementId>();
    var notContainers = new List<Element>();
    var familyParents = new List<FamilyInstance>();
    var groupLines = new List<string>();

    foreach (var container in containers)
    {
        if (container == null) continue;
        var again = false;
        foreach (var id in opened) if (id == container.Id) { again = true; break; }
        if (again) continue;
        opened.Add(container.Id);

        var instance = container as Group;
        var assembly = container as AssemblyInstance;
        var parent = container as FamilyInstance;

        if (assembly != null)
        {
            if (!kinds.Contains("assembly")) kinds.Add("assembly");
            var ids = assembly.GetMemberIds();
            var listed = ids == null ? 0 : ids.Count;
            var found = 0;
            var groupsInside = 0;

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    var member = doc.GetElement(id);
                    if (member == null) continue;
                    found++;
                    if (member is Group) groupsInside++;
                    take(member);
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
                found,
                listed == found ? "" : string.Format(" - {0} member id(s) listed, {1} no longer exist",
                    listed, listed - found)));

            if (groupsInside > 0)
                findings.Add(string.Format("{0} of those are model GROUPS, returned whole rather than "
                    + "flattened. Hand one back in to open it", groupsInside));

            if (found > 0)
                groupLines.Add("These are safe to read. CHANGING one changes the assembly: Revit may then find "
                    + "it no longer matches the other instances of its type and asks for a new type, or the "
                    + "edit has to be made in Edit Assembly. Its shop drawing views follow the members");
        }
        else if (instance != null)
        {
            if (!kinds.Contains("group")) kinds.Add("group");
            var ids = instance.GetMemberIds();
            var listed = ids == null ? 0 : ids.Count;
            var found = 0;
            var groupsInside = 0;

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    var member = doc.GetElement(id);
                    if (member == null) continue;
                    found++;
                    if (member is Group) groupsInside++;
                    take(member);
                }
            }

            var typeName = "";
            var groupType = doc.GetElement(instance.GetTypeId()) as GroupType;
            if (groupType != null) typeName = groupType.Name;

            findings.Add(string.Format("Group instance id {0}{1} holds {2} member(s){3}",
                instance.Id,
                string.IsNullOrEmpty(typeName) ? "" : string.Format(", of group type '{0}'", typeName),
                found,
                listed == found ? "" : string.Format(" - {0} member id(s) listed, {1} no longer exist",
                    listed, listed - found)));

            if (groupsInside > 0)
                findings.Add(string.Format("{0} of those are themselves GROUPS, returned whole rather than "
                    + "flattened. Hand one back in to open it", groupsInside));

            if (found > 0)
                groupLines.Add("These are safe to read. CHANGING one is not: Revit either refuses an in-place "
                    + "edit of a group member or applies the change to every instance of this group type. "
                    + "UNGROUP_ELEMENTS is what is really wanted when the next step is to edit them");
        }
        else if (parent != null)
        {
            familyParents.Add(parent);
        }
        else
        {
            notContainers.Add(container);
        }
    }

    // NESTED FAMILIES (version 3, from SELECT_SUBCOMPONENTS) - followed as deep
    // as somebody built them unless `recursive` is false, every id visited once.
    if (familyParents.Count > 0)
    {
        if (!kinds.Contains("family")) kinds.Add("family");
        var visited = new List<ElementId>();
        foreach (var parent in familyParents) visited.Add(parent.Id);
        var queue = new Queue<KeyValuePair<FamilyInstance, int>>();
        foreach (var parent in familyParents) queue.Enqueue(new KeyValuePair<FamilyInstance, int>(parent, 0));
        var deepest = 0;
        var nestedFound = 0;

        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            var current = next.Key;
            var currentDepth = next.Value;

            ICollection<ElementId> childIds = null;
            try { childIds = current.GetSubComponentIds(); }
            catch { childIds = null; }

            if (childIds == null || childIds.Count == 0)
            {
                if (currentDepth == 0) withoutSubComponents++;
                continue;
            }

            foreach (var childId in childIds)
            {
                var already = false;
                foreach (var seen in visited)
                {
                    if (seen == childId) { already = true; break; }
                }
                if (already) continue;
                visited.Add(childId);

                var child = doc.GetElement(childId);
                if (child == null) continue;

                nestedFound++;
                take(child);
                if (currentDepth + 1 > deepest) deepest = currentDepth + 1;

                var childInstance = child as FamilyInstance;
                if (recursive && childInstance != null)
                    queue.Enqueue(new KeyValuePair<FamilyInstance, int>(childInstance, currentDepth + 1));
            }
        }

        findings.Add(string.Format("{0} nested element(s) found under {1} family instance(s){2}. {3} of "
            + "them have no sub-components at all, which is the ordinary case",
            nestedFound,
            familyParents.Count,
            recursive ? string.Format(", followed {0} level(s) deep", deepest) : ", one level only",
            withoutSubComponents));

        if (nestedFound > 0)
            findings.Add("Nested parts are the elements a category sweep of the model does NOT return, because "
                + "a collector gives the parent instead. Any count taken without them is short");
    }

    if (notContainers.Count > 0)
    {
        var first = notContainers[0];
        findings.Add(notContainers.Count == 1
            ? string.Format("'{0}' (id {1}) is neither a model group, an assembly nor a family instance, so it "
                + "has no members to open - a wall, a duct or a system family has none. It was counted, not "
                + "skipped in silence", string.IsNullOrEmpty(first.Name) ? "That element" : first.Name, first.Id)
            : string.Format("{0} of what was handed in are neither model groups, assemblies nor family "
                + "instances - a wall, a duct or a system family has no members - so they were counted rather "
                + "than skipped in silence. The first is '{1}' (id {2})", notContainers.Count,
                string.IsNullOrEmpty(first.Name) ? "an element" : first.Name, first.Id));
    }

    foreach (var line in groupLines.Distinct()) findings.Add(line);
    containerKind = string.Join(", ", kinds);
}
