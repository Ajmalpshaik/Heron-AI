// NOT STANDALONE. Assumes `elements` and `pinned` are in scope; leaves
// `changed`, `alreadySet` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHY `alreadySet` IS A SEPARATE NUMBER AND NOT PART OF THE TOTAL.
//
// Setting Pinned to the value it already holds is accepted by Revit and does
// nothing. Counting those would produce "pinned 40" on a batch where 38 were
// already pinned and 2 actually changed - true as English, useless as an
// answer, and the precise shape of the failure this project keeps meeting:
// the number ASKED FOR reported as the number that HAPPENED.
//
// So the state is read FIRST. After the write there is no way to tell an
// element this call changed from one that arrived that way.
//
// NOT EVERYTHING CAN BE PINNED. Revit throws on some elements rather than
// returning false, and a throw part-way abandons the rest of the batch. Each
// one is set individually and a refusal is recorded and stepped over, so a
// batch of two hundred does not end at the eleventh with nothing reported.
// The catch is deliberately narrow in what it DOES - it records the id, it
// never decides the operation succeeded.

var changed = 0;
var alreadySet = 0;
var refused = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    bool was;
    try
    {
        was = element.Pinned;
    }
    catch (Exception)
    {
        // Cannot even be READ, so it certainly cannot be set. Refused rather
        // than assumed unpinned - a guess here would be reported as a change.
        refused.Add(element.Id);
        continue;
    }

    if (was == pinned)
    {
        alreadySet++;
        continue;
    }

    try
    {
        element.Pinned = pinned;
    }
    catch (Exception)
    {
        refused.Add(element.Id);
        continue;
    }

    // READ BACK. The set threw nothing, which is not the same as it having
    // taken - the whole reason MOVE_ELEMENTS now probes positions is that
    // Revit's silence is not evidence. Two reads of the same property.
    if (element.Pinned == pinned) changed++;
    else refused.Add(element.Id);
}
