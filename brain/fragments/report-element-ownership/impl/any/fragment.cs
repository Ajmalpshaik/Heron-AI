// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `owners`, `creators`, `lastChangedBy`, `ownedByOthers`, `editable` and
// `notWorkshared` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY IT IS ASKED BEFORE THE EDIT AND NOT DURING IT.
//
// Golden Rule 16: a batch runs inside ONE transaction, so it is one undo entry.
// Reaching an element owned by somebody else partway through does not skip that
// element - it takes the whole batch down with it. Asking first turns that into
// a decision about scope instead of a failure halfway through.
//
// "NOT OWNED" IS "PROBABLY FREE", NOT "GUARANTEED FREE".
//
// Checkout status reflects the LAST SYNC. Somebody may have borrowed an element
// since, and nothing local can see it. The fresh-reads rule applies to people
// as well as to geometry. The names here are deliberately `editable` and
// `ownedByOthers` rather than anything that reads as a guarantee, because a
// guarantee is what a caller would act on.
//
// A MODEL THAT IS NOT WORKSHARED IS ANSWERED, NOT REFUSED.
//
// "Everything is editable" is the correct answer to the question on the more
// common kind of model. Returning empty lists would read as "nothing is
// editable" - the exact opposite - so the flag is set and every element is
// reported as editable, which is true.
//
// THREE DIFFERENT PEOPLE.
//
// The OWNER is who to ask for a relinquish. The LAST CHANGER is who to ask
// about an odd piece of modelling. The CREATOR is who to ask about intent.
// Collapsing them sends the question to the wrong desk.

var owners = new Dictionary<ElementId, string>();
var creators = new Dictionary<ElementId, string>();
var lastChangedBy = new Dictionary<ElementId, string>();
var ownedByOthers = new List<ElementId>();
var editable = new List<ElementId>();

bool notWorkshared = !doc.IsWorkshared;

foreach (var element in elements)
{
    if (element == null) continue;
    var id = element.Id;

    if (notWorkshared)
    {
        // True, and said rather than left to be inferred from an empty list.
        editable.Add(id);
        continue;
    }

    var status = CheckoutStatus.NotOwned;
    bool statusKnown = false;
    try { status = WorksharingUtils.GetCheckoutStatus(doc, id); statusKnown = true; } catch { }

    // Guarded on its own: an element another user deleted since the last sync
    // throws on this call alone, and wrapping the loop instead would lose the
    // whole report over one stale id.
    try
    {
        var tooltip = WorksharingUtils.GetWorksharingTooltipInfo(doc, id);
        if (tooltip != null)
        {
            if (!string.IsNullOrEmpty(tooltip.Owner)) owners[id] = tooltip.Owner;
            if (!string.IsNullOrEmpty(tooltip.Creator)) creators[id] = tooltip.Creator;
            if (!string.IsNullOrEmpty(tooltip.LastChangedBy)) lastChangedBy[id] = tooltip.LastChangedBy;
        }
    }
    catch { }

    if (!statusKnown) continue;

    if (status == CheckoutStatus.OwnedByOtherUser) ownedByOthers.Add(id);
    else editable.Add(id);
}
