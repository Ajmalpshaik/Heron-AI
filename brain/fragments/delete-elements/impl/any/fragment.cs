// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `deleted`,
// `askedFor`, `alsoWent` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE RETURNED SET IS THE ANSWER, NOT THE INPUT COUNT. This is the whole
// fragment.
//
// Revit deletes dependents without asking: a wall takes its doors and windows,
// a duct takes its fittings and insulation, a level can take everything on it.
// `Document.Delete` hands back the ids that ACTUALLY went, and that set is
// routinely larger than the one passed in. Reporting "deleted 4" from the input
// list would be the defining failure of this project - the number ASKED FOR
// reported as the number that HAPPENED - except here it under-reports
// destruction, which is the worst direction for it to be wrong in.
//
// `alsoWent` IS THE NUMBER SOMEBODY NEEDS TO SEE. Four ducts asked for and
// nineteen elements gone is not a detail; it is the difference between a tidy-up
// and an accident, and it is invisible unless it is counted here.
//
// A DELETION THAT REVIT REFUSES DOES NOT ABANDON THE BATCH. Some elements
// cannot be deleted - a pinned element, a member of an unmodifiable group, an
// element owned by another user. Each id is submitted so a refusal is recorded
// and stepped over. Slower than one bulk call and worth it: a batch that throws
// at the eleventh of two hundred has already deleted ten and reports nothing.

var askedFor = elements.Count;
var deleted = new List<ElementId>();
var refused = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    var id = element.Id;
    try
    {
        var went = doc.Delete(id);

        // An EMPTY return is not a success. Revit accepted the call and removed
        // nothing, which is the silent-no-op shape this repository has already
        // been bitten by on the move path. Recorded as refused, not counted.
        if (went == null || went.Count == 0)
        {
            refused.Add(id);
            continue;
        }

        foreach (var gone in went)
        {
            if (!deleted.Contains(gone)) deleted.Add(gone);
        }
    }
    catch (Exception)
    {
        refused.Add(id);
    }
}

// What went beyond what was named. Never below zero: an element can be taken
// as a dependent of an earlier deletion and then be missing when its own turn
// comes, so the two counts are not guaranteed to be ordered.
var alsoWent = deleted.Count - askedFor;
if (alsoWent < 0) alsoWent = 0;
