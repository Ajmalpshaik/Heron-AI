// NOT STANDALONE. Assumes `doc` is in scope; leaves `unusedGroupTypes`,
// `unusedNames`, `usedOnlyAsAttached`, `scanned` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none, and deletes nothing.
//
// THE TEST IS A REVERSE-LOOKUP, NOT A GUESS. A group type whose set of placed
// groups is empty has nothing placed anywhere. Revit keeps that relationship
// both ways, which is what makes this worth reporting.
//
// AN ATTACHED DETAIL GROUP HAS NO INSTANCES OF ITS OWN AND IS STILL IN USE.
// It is offered through its PARENT: the parent model group is placed, the
// detail group is available on every one of those placements, and its own
// instance set can still read empty. Reported as unused and deleted, the
// detail that annotates a placed group is gone. So every placed group is asked
// which attached detail group types it offers, and those are used whatever
// their own count says.
//
// DELETING A GROUP TYPE IS NOT UNGROUPING. There is nothing placed to ungroup -
// the definition goes and the model looks identical. UNGROUP_ELEMENTS is the
// opposite operation on the opposite thing.

var unusedGroupTypes = new List<ElementId>();
var unusedNames = new List<string>();
var usedOnlyAsAttached = new List<ElementId>();
var findings = new List<string>();
var scanned = 0;

// Which detail group types are offered by something that IS placed. Gathered
// from the placed groups themselves, so it is the same kind of reverse-lookup
// as the instance test rather than a second guess layered on top.
var offeredByAPlacedGroup = new HashSet<ElementId>();

foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(Group)))
{
    var group = candidate as Group;
    if (group == null) continue;

    try
    {
        var attached = group.GetAvailableAttachedDetailGroupTypeIds();
        if (attached == null) continue;

        foreach (var id in attached) offeredByAPlacedGroup.Add(id);
    }
    catch
    {
        // A group that will not answer is left alone rather than assumed to
        // offer nothing. Assuming nothing is the direction that deletes a
        // detail group still in use.
    }
}

foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(GroupType)))
{
    var groupType = candidate as GroupType;
    if (groupType == null) continue;

    scanned++;

    var placed = 0;
    try
    {
        var groups = groupType.Groups;
        if (groups != null) placed = groups.Size;
    }
    catch
    {
        // Unreadable is not zero. Skipped rather than reported as unused.
        continue;
    }

    if (placed > 0) continue;

    if (offeredByAPlacedGroup.Contains(groupType.Id))
    {
        // Nothing of its own is placed and it is still in use, through the
        // model group it hangs off. This is the trap, and it is reported
        // rather than folded into either list.
        usedOnlyAsAttached.Add(groupType.Id);
        continue;
    }

    unusedGroupTypes.Add(groupType.Id);
    unusedNames.Add(string.IsNullOrEmpty(groupType.Name) ? "(no name)" : groupType.Name);
}

findings.Add(unusedGroupTypes.Count + " group definition(s) have nothing placed, out of "
    + scanned + " walked.");

if (usedOnlyAsAttached.Count > 0)
{
    findings.Add(usedOnlyAsAttached.Count + " detail group definition(s) have no instances of "
        + "their own and ARE in use - they are attached to model groups that are placed. They "
        + "are kept out of the unused list on purpose: deleting one removes the detail that "
        + "annotates a placed group.");
}

findings.Add("Nothing was deleted, deliberately. Deleting a group definition is not ungrouping - "
    + "nothing placed comes apart and the model looks identical - but it is still hard to reverse "
    + "on a shared model, and a group kept for a repeating unit not yet placed reports here and "
    + "should stay. Revit's own Purge Unused does the removing.");
