// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `elements`,
// `ungrouped` and `notGroups` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE RELEASED MEMBERS REPLACE `elements`, which is what makes "ungroup then
// move" one composable chain. `elements` is the name every action in this
// library consumes, and the members are what the next step acts on - the
// groups themselves are gone.
//
// SOMETHING THAT IS NOT A GROUP IS NAMED, NOT SKIPPED. Handing a mixed
// selection here is normal - a user selects a region and some of it is grouped.
// Dropping the rest silently would make the answer describe fewer elements than
// were asked about, and only somebody counting rows would notice.
//
// UNGROUPING ONE INSTANCE LEAVES THE GROUP TYPE ALONE, and every other placed
// instance with it. That is what a user expects and it is worth knowing here,
// because "get rid of this group" sometimes means the type, which is a much
// larger action and is not this one.

var released = new List<Element>();
var ungrouped = 0;
var notGroups = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    var group = element as Group;
    if (group == null)
    {
        notGroups.Add(element.Id);
        continue;
    }

    ICollection<ElementId> members;
    try
    {
        members = group.UngroupMembers();
    }
    catch (Exception)
    {
        // Recorded with the non-groups rather than silently lost: from the
        // caller's side "this one did not come apart" is the same fact.
        notGroups.Add(element.Id);
        continue;
    }

    if (members == null || members.Count == 0)
    {
        notGroups.Add(element.Id);
        continue;
    }

    ungrouped++;

    foreach (var id in members)
    {
        var member = doc.GetElement(id);
        if (member != null) released.Add(member);
    }
}

elements = released;
