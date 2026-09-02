// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `editable`,
// `ownedByOthers` and `workshared` behind.
//
// READ ONLY. Opens no transaction, and deliberately does NOT check anything
// out: looking must never be the act of claiming. That is the same lesson the
// health tool learned about the lease one commit after building it.
//
// NOT WORKSHARED IS ITS OWN ANSWER. Everything is editable and nothing is
// owned, and `workshared` is false so a caller can say "this model is not
// workshared" rather than "all 200 are free" - which is true, and is a
// different sentence from "I checked 200 owners".
//
// THE OWNER'S NAME COMES FROM THE TOOLTIP INFO, WHICH CAN BE ABSENT. Revit
// returns the checkout status without always having a name to go with it -
// a stale central, a user who has since relinquished. An unknown owner is
// reported as unknown rather than as free, because "somebody has this and I
// cannot say who" and "nobody has this" lead to opposite actions.

var editable = new List<ElementId>();
var ownedByOthers = new Dictionary<ElementId, string>();
var workshared = doc.IsWorkshared;

if (!workshared)
{
    foreach (var element in elements)
    {
        if (element != null) editable.Add(element.Id);
    }
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var id = element.Id;
        if (ownedByOthers.ContainsKey(id) || editable.Contains(id)) continue;

        CheckoutStatus status;
        try
        {
            status = WorksharingUtils.GetCheckoutStatus(doc, id);
        }
        catch (Exception)
        {
            // Cannot be read is not the same as free. Recorded as held by an
            // unknown owner - the safe direction, because the other way round
            // promises an edit that will fail at the moment it is attempted.
            ownedByOthers[id] = "unknown";
            continue;
        }

        // OwnedByOtherUser is the only status that blocks an edit.
        // NotOwned means free to take; OwnedByCurrentUser is already ours.
        if (status != CheckoutStatus.OwnedByOtherUser)
        {
            editable.Add(id);
            continue;
        }

        var who = "unknown";
        try
        {
            var info = WorksharingUtils.GetWorksharingTooltipInfo(doc, id);
            if (info != null && !string.IsNullOrEmpty(info.Owner)) who = info.Owner;
        }
        catch (Exception)
        {
            // Left as unknown. The status already said it is held, and that is
            // the half that decides whether the edit can proceed.
        }

        ownedByOthers[id] = who;
    }
}
