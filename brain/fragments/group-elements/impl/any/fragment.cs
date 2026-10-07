// NOT STANDALONE. Assumes `doc`, `elements` and `name` are in scope; leaves
// `groupId`, `grouped`, `groupName`, `refused`, `refusalReason` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A PROJECT OR A FAMILY. Version 1 called `doc.Create.NewGroup`, and
// `Document.Create` THROWS in a family document - "Thrown when the current
// document is family document", RevitAPI.xml - so in the Family Editor every
// call was refused and the reply never said why. `NewGroup` is declared on the
// creation factory that `doc.Create` and `doc.FamilyCreate` both are, so the
// family route is the same call on the family's own factory, 2020 to 2027.
// Neither factory is touched until the document says which kind it is: each
// throws in the other kind of document.
//
// ALL OR NOTHING, AND REVIT DECIDES. `NewGroup` takes the whole set and either
// makes one group or throws. There is no partial group, so there is no partial
// success to report - `refused` is a bool rather than a list, which is the
// honest shape of what this call can tell us. `refusalReason` says WHY in plain
// words, with Revit's own message quoted when it was Revit that said no.
//
// THE GROUP AND ITS NAME ARE ONE STEP. A name another group type already has is
// refused before anything is made. Anything else that stops the name taking -
// Revit is the judge of what counts as the same name - rolls the group back
// with it, inside a sub-transaction, so a refused name never leaves behind a
// "Group 1" nobody asked for.
//
// THE MEMBER COUNT IS READ BACK FROM THE GROUP, not taken from the input.
// Revit adds elements closely tied to the ones given - "such as sketch and
// sketch plane", its own remark on NewGroup - and can drop one it will not
// accept. The group's own member list is what actually exists afterwards, and
// any difference is said, by kind, rather than left for the modeller to find.
//
// REGENERATED FIRST. Revit's other remark on NewGroup: an element made earlier
// in the same change may need a regenerate, or the group shows a warning that
// it changed outside group edit mode. A chain that makes forms and then groups
// them is exactly that case, so the model is regenerated before grouping.
//
// WHAT THIS DOES TO EVERYTHING AFTERWARDS. A group member does not move when
// asked - Revit's move returns normally and moves nothing, which is the defect
// MOVE_ELEMENTS now probes for. Creating a group creates that condition, so a
// caller that groups and then moves must expect the second to report blocked.

var groupId = ElementId.InvalidElementId;
var grouped = 0;
var groupName = "";
var refused = false;
var refusalReason = "";
var findings = new List<string>();

var inFamily = doc.IsFamilyDocument;
var inDoc = inFamily ? "the family being edited" : "the project";
var wanted = (name ?? "").Trim();

var ids = new List<ElementId>();
if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (!ids.Contains(element.Id)) ids.Add(element.Id);
    }
}

// Revit refuses a group of nothing, and a group of one is a group Revit will
// make and nobody wants. Both are refused here rather than left to throw, so
// the caller gets a condition instead of an exception.
if (ids.Count == 0)
{
    refused = true;
    refusalReason = "Nothing was given to group. Select two or more elements in " + inDoc +
                    " and ask again - nothing has been changed.";
}
else if (ids.Count == 1)
{
    refused = true;
    refusalReason = "Only one element was given, and a group of one is a group nobody wants. " +
                    "Select two or more and ask again - nothing has been changed.";
}
else if (wanted.Length > 0 && !NamingUtils.IsValidName(wanted))
{
    refused = true;
    refusalReason = "'" + wanted + "' cannot be a group name - Revit allows none of " +
                    "\\ : { } [ ] | ; < > ? ` ~ in a name. Choose another, or give it empty " +
                    "for Revit's own - nothing has been grouped.";
}
else
{
    // EVERY group type, model and detail alike. Revit itself is the second
    // check, inside the step below.
    var taken = false;
    if (wanted.Length > 0)
    {
        foreach (var existing in new FilteredElementCollector(doc).OfClass(typeof(GroupType)))
        {
            if (string.Equals(existing.Name, wanted, StringComparison.Ordinal)) { taken = true; break; }
        }
    }

    if (taken)
    {
        refused = true;
        refusalReason = "The name '" + wanted + "' is already used by another group in " + inDoc +
                        ". Choose another name, or give it empty for Revit's own - nothing has " +
                        "been grouped.";
    }
    else
    {
        SubTransaction grouping = null;
        var naming = false;
        try
        {
            grouping = new SubTransaction(doc);
            grouping.Start();

            doc.Regenerate();

            var group = inFamily ? doc.FamilyCreate.NewGroup(ids) : doc.Create.NewGroup(ids);
            if (group == null)
            {
                grouping.RollBack();
                refused = true;
                refusalReason = "Revit made no group from these " + ids.Count + " elements and gave " +
                                "no reason - nothing has been grouped.";
            }
            else
            {
                if (wanted.Length > 0)
                {
                    naming = true;
                    group.GroupType.Name = wanted;
                    naming = false;
                }

                // From the group, not from `ids`.
                var members = group.GetMemberIds() ?? new List<ElementId>();
                var count = members.Count;
                var named = group.GroupType.Name;
                var madeId = group.Id;

                var extra = 0;
                var addedKinds = new List<string>();
                foreach (var memberId in members)
                {
                    if (ids.Contains(memberId)) continue;
                    extra++;
                    var member = doc.GetElement(memberId);
                    var kind = member == null ? "an element"
                             : member.Category != null ? member.Category.Name
                             : member.GetType().Name;
                    if (!addedKinds.Contains(kind)) addedKinds.Add(kind);
                }
                var leftOut = 0;
                foreach (var asked in ids)
                {
                    if (!members.Contains(asked)) leftOut++;
                }

                // Every sentence is written BEFORE the commit, so nothing that
                // can fail runs after the group is kept.
                var notes = new List<string>();
                notes.Add("Grouped " + count + " elements in " + inDoc + " as '" + named + "' - " +
                          (wanted.Length > 0 ? "the name asked for." : "Revit's own name, as none was given."));

                if (extra > 0 || leftOut > 0)
                {
                    var said = new List<string>();
                    if (extra > 0)
                        said.Add("Revit added " + extra + " element(s) closely tied to the ones given (" +
                                 string.Join(", ", addedKinds) + "), as it does");
                    if (leftOut > 0)
                        said.Add("Revit left " + leftOut + " of the " + ids.Count + " given out of the group");
                    notes.Add(string.Join("; ", said) + " - so the group holds " + count +
                              ", not the " + ids.Count + " given.");
                }

                if (inFamily)
                    notes.Add("A group inside a family is allowed but adds weight. For many identical " +
                              "small parts, one nested family placed many times is usually lighter, " +
                              "or one form arrayed for a row.");

                grouping.Commit();

                groupId = madeId;
                grouped = count;
                groupName = named;
                findings.AddRange(notes);
            }
        }
        catch (Exception ex)
        {
            // TAKE BACK WHATEVER THE STEP MADE - and if that fails too, say so
            // rather than claim nothing was grouped.
            var undoFailed = "";
            try
            {
                if (grouping != null && grouping.HasStarted() && !grouping.HasEnded()) grouping.RollBack();
            }
            catch (Exception undo)
            {
                undoFailed = undo.Message ?? "no message";
            }

            refused = true;
            groupId = ElementId.InvalidElementId;
            grouped = 0;
            groupName = "";

            var what = naming
                ? "Revit would not name the group '" + wanted + "': \"" + ex.Message + "\""
                : "Revit would not group these " + ids.Count + " elements in " + inDoc + ": \"" +
                  ex.Message + "\"";
            refusalReason = undoFailed.Length > 0
                ? what + " Taking the step back failed too: \"" + undoFailed + "\" Check the model " +
                  "for a new group, and use Undo if one is there."
                : what + (naming ? " The group was taken back with it, so nothing has been grouped."
                                 : " Nothing has been grouped.");
        }
        finally
        {
            if (grouping != null) grouping.Dispose();
        }
    }
}
