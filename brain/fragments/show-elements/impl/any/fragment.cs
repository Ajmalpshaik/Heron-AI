// NOT STANDALONE. Assumes `view`, `elements` and `permanent` are in scope;
// leaves `shown`, `notHidden`, `temporaryModeCleared` and `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// UNDOING A HIDE IS NOT THE MIRROR OF DOING ONE, and getting that wrong is the
// whole risk here.
//
//   permanent   element by element. `UnhideElements` takes exactly the ids
//               given, so the count reported is the count that changed
//   temporary   ALL OR NOTHING. Revit offers no way to un-hide one temporarily
//               hidden element - the only exit is leaving the mode, which also
//               brings back everything else anybody hid or isolated temporarily
//               in this view
//
// So the temporary path deliberately IGNORES its element list. Reporting
// `shown = 4` there would be a lie of exactly the shape this project keeps
// meeting: the number ASKED FOR reported as the number that HAPPENED. It
// reports `temporaryModeCleared` instead, which is the true statement.
//
// AN ELEMENT THAT WAS NEVER HIDDEN IS NOT AN ERROR, but it is not a success
// either. Revit accepts it in the batch and nothing observable changes, so
// counting it would inflate the answer. It goes to `notHidden`, which is what
// lets a caller say "3 of the 5 were not hidden in the first place" instead of
// reporting five successes and leaving the user wondering what moved.

var shown = 0;
var notHidden = new List<ElementId>();
var temporaryModeCleared = false;
var viewRefused = false;

if (view == null)
{
    viewRefused = true;
}
else if (!permanent)
{
    // Leaving the mode is the only granularity Revit has. Asked for on a view
    // that does not support temporary modes, that is a refusal rather than a
    // silent no-op - the user is watching for something to come back.
    if (!view.CanUseTemporaryVisibilityModes())
    {
        viewRefused = true;
    }
    else
    {
        view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
        temporaryModeCleared = true;
    }
}
else
{
    var ids = new List<ElementId>();
    foreach (var element in elements)
    {
        if (element == null) continue;

        // Asked BEFORE unhiding, because afterwards there is no way to tell an
        // element this call brought back from one that was already visible.
        if (!element.IsHidden(view))
        {
            notHidden.Add(element.Id);
            continue;
        }
        ids.Add(element.Id);
    }

    if (ids.Count > 0)
    {
        view.UnhideElements(ids);
        shown = ids.Count;
    }
}
