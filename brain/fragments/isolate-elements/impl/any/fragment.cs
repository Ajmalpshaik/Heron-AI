// NOT STANDALONE. Assumes `view` and `elements` are in scope; leaves
// `isolated` and `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// RESET FIRST, THEN ISOLATE. Revit's temporary isolate is ADDITIVE to whatever
// temporary state the view is already in - isolate one set, then isolate
// another, and the view shows the union of both. To whoever asked, that reads
// as the filter having found far too much, and the view is the thing lying
// rather than the filter.
//
// So any isolation already running is turned off before this one is applied.
// That makes the fragment idempotent: running it twice leaves the view showing
// exactly the same thing, which is what somebody expects from "show me just
// these".
//
// ASK REVIT WHETHER THE VIEW CAN DO THIS, rather than keeping a list of which
// view types support isolation. A schedule, a sheet and a legend cannot, and a
// hand-maintained list of view types would be wrong the first time Autodesk
// adds one. `CanUseTemporaryVisibilityModes` is Revit answering for itself.
//
// TEMPORARY ON PURPOSE. Nothing here is saved into the view: Reset Temporary
// Hide/Isolate clears it, and so does closing the model. A permanent change to
// what a view shows is a different capability with a higher risk, and merging
// the two would make "let me look at this for a second" and "change this
// drawing" the same request.

var isolated = 0;
var viewRefused = false;

// A SHEET IS NAMED SEPARATELY, BECAUSE IT IS THE ONE THIS TEST LET THROUGH.
//
// The comment below used to say "a schedule, A SHEET, a legend, or a view
// template", and a sheet was the one case it did not catch:
// `CanUseTemporaryVisibilityModes()` answers TRUE for a ViewSheet - the
// fragment's own comment calls that check *"Revit answering for itself"*, and
// here Revit answers yes. Measured 2026-09-13 on sheet `A101` of
// `Project1 work_ajmal.al`, handed 9 ducts that live in `1 - Mech` and are not
// on that sheet: **`isolated 9`, `viewRefused false`**. Nothing was isolated.
// FRAGMENT-ISSUES rows 20 and 24.
if (view == null || view is ViewSheet || !view.CanUseTemporaryVisibilityModes())
{
    // A schedule, a sheet, a legend, or a view template. Reported rather than
    // thrown, because asking to isolate in a schedule is an ordinary mistake.
    viewRefused = true;
}
else
{
    if (view.IsTemporaryHideIsolateActive())
    {
        view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
    }

    var ids = new List<ElementId>();
    foreach (var element in elements)
    {
        if (element != null) ids.Add(element.Id);
    }

    // An empty list would isolate NOTHING - a blank view, which looks exactly
    // like a broken tool. Refused, and the view is left as it was found.
    if (ids.Count > 0)
    {
        view.IsolateElementsTemporary(ids);
        isolated = ids.Count;
    }
}
