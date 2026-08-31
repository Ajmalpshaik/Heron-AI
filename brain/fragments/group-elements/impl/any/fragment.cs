// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `groupId`,
// `grouped` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ALL OR NOTHING, AND REVIT DECIDES. `NewGroup` takes the whole set and either
// makes one group or throws. There is no partial group, so there is no partial
// success to report - `refused` is a bool rather than a list, which is the
// honest shape of what this call can tell us.
//
// THE MEMBER COUNT IS READ BACK FROM THE GROUP, not taken from the input.
// Revit can absorb a nested group or drop an element it will not accept, and
// the group's own member list is what actually exists afterwards. Reporting
// the input count would be the asked-for/happened failure in the one place it
// is easy to check.
//
// WHAT THIS DOES TO EVERYTHING AFTERWARDS. A group member does not move when
// asked - Revit's move returns normally and moves nothing, which is the defect
// MOVE_ELEMENTS now probes for. Creating a group creates that condition, so a
// caller that groups and then moves must expect the second to report blocked.

var groupId = ElementId.InvalidElementId;
var grouped = 0;
var refused = false;

var ids = new List<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    if (!ids.Contains(element.Id)) ids.Add(element.Id);
}

// Revit refuses a group of nothing, and a group of one is a group Revit will
// make and nobody wants. Both are refused here by the same flag rather than
// left to throw, so the caller gets a condition instead of an exception.
if (ids.Count < 2)
{
    refused = true;
}
else
{
    try
    {
        var group = doc.Create.NewGroup(ids);
        if (group == null)
        {
            refused = true;
        }
        else
        {
            groupId = group.Id;

            // From the group, not from `ids`.
            var members = group.GetMemberIds();
            grouped = members == null ? 0 : members.Count;
        }
    }
    catch (Exception)
    {
        refused = true;
    }
}
