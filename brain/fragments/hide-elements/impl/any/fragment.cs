// NOT STANDALONE. Assumes `view`, `elements` and `permanent` are in scope;
// leaves `hidden`, `cannotHide` and `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE TWO MODES ARE NOT DEGREES OF THE SAME THING.
//
//   temporary   a look at something. Revit's Reset Temporary Hide/Isolate
//               clears it, closing the model clears it, and nothing is written
//               into the view
//   permanent   SAVED INTO THE VIEW. It survives closing the model, it appears
//               on the printed sheet, and the next person to open that view
//               finds something missing with no way of knowing why
//
// The second is a change to a drawing. It is not inferred from anything - not
// from how many elements there are, not from which view it is. The flag is an
// input, and the caller is expected to have been told which one was meant.
//
// NOT EVERY ELEMENT CAN BE HIDDEN. Revit refuses some outright, and asking
// `CanBeHidden` first turns that into a reported outcome rather than an
// exception that abandons a batch of two hundred at the eleventh.
//
// AN EMPTY LIST IS NOT AN ERROR HERE, unlike isolating. Hiding nothing leaves
// the view exactly as it was, which is harmless - where isolating nothing
// produces a blank view that looks like a broken tool.

var hidden = 0;
var cannotHide = new List<ElementId>();
var viewRefused = false;

// A temporary hide needs the view to support temporary modes. A permanent one
// does not - it is an ordinary view setting - so the check applies to the
// temporary path only.
//
if (view == null || (!permanent && !view.CanUseTemporaryVisibilityModes()))
{
    viewRefused = true;
}
else
{
    // ON A SHEET, ONLY WHAT BELONGS TO THE SHEET CAN BE HIDDEN - AND THE
    // ELEMENT IS REJECTED, NOT THE VIEW.
    //
    // Neither existing test catches a sheet: `CanUseTemporaryVisibilityModes()`
    // answers TRUE for one, and `CanBeHidden(sheet)` answers true for model
    // elements that are not on it - so nothing refused, and `hidden` below
    // counted the request. Measured 2026-09-13 on `Cover Sheet` of
    // `Snowdon-scratch_ajmal.al`: handed 191 ducts living in another view, this
    // returned `hidden 191`, `viewRefused false`, `cannotHide 0`. Nothing was
    // hidden and nothing could be - a duct is not on a sheet, the viewport is.
    // FRAGMENT-ISSUES rows 20, 24 and 123.
    //
    // REFUSING THE WHOLE SHEET WAS THE FIRST REPAIR AND IT WAS TOO BROAD.
    // Caught by Codex on PR #191: a text note, a revision cloud or a title
    // block placed ON the sheet is genuinely hideable there, and "hide it on
    // this drawing" is a real request. What the measurement establishes is only
    // that OFF-SHEET elements must not be counted, so that is what is rejected.
    //
    // `OwnerViewId` is the discriminator and it is exact rather than clever: an
    // element drawn on a view carries that view's id, and a MODEL element -
    // every duct, wall and pipe - carries `InvalidElementId`, which can never
    // equal the sheet's. It costs one comparison and needs no geometry.
    var onASheet = view is ViewSheet;

    var ids = new List<ElementId>();
    foreach (var element in elements)
    {
        if (element == null) continue;

        if (onASheet && element.OwnerViewId != view.Id)
        {
            cannotHide.Add(element.Id);
            continue;
        }

        if (!element.CanBeHidden(view))
        {
            cannotHide.Add(element.Id);
            continue;
        }
        ids.Add(element.Id);
    }

    if (ids.Count > 0)
    {
        if (permanent) view.HideElements(ids);
        else view.HideElementsTemporary(ids);
        hidden = ids.Count;
    }
}
