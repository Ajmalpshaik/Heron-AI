// NOT STANDALONE. Assumes `elements` and `newTypeId` are in scope; leaves
// `changed`, `alreadyThatType` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// READ THE TYPE FIRST, CHANGE IT, THEN READ IT BACK. Both reads earn their
// keep and they catch different things.
//
//   before   an element already on the target type would otherwise be counted
//            as changed. "Swapped 60" when 58 were already that type is the
//            asked-for/happened failure again
//   after    ChangeTypeId does not always take. It throws for some mismatches
//            and for others returns quietly having done nothing - the same
//            silent no-op shape as the move that moved nothing. The absence of
//            an exception is not evidence
//
// SUBMITTED ONE AT A TIME so a refusal is recorded and stepped over. A throw
// part-way through a bulk change leaves some elements swapped, some not, and
// reports neither.

var changed = 0;
var alreadyThatType = 0;
var refused = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    ElementId was;
    try
    {
        was = element.GetTypeId();
    }
    catch (Exception)
    {
        refused.Add(element.Id);
        continue;
    }

    if (was == newTypeId)
    {
        alreadyThatType++;
        continue;
    }

    try
    {
        element.ChangeTypeId(newTypeId);
    }
    catch (Exception)
    {
        refused.Add(element.Id);
        continue;
    }

    if (element.GetTypeId() == newTypeId) changed++;
    else refused.Add(element.Id);
}
