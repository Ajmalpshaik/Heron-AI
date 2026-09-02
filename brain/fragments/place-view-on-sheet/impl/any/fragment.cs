// NOT STANDALONE. Assumes `doc`, `sheetId`, `views` and `at` are in scope;
// leaves `placed` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ASKED BEFORE ATTEMPTED. `CanAddViewToSheet` is Revit's own answer and it
// covers what would otherwise be exceptions - a view already on another sheet,
// a template, a schedule that needs a different call entirely. Asking turns
// each into a reported outcome and lets the other twenty-nine views place.
//
// A VIEW CAN BE ON ONLY ONE SHEET. That is the commonest refusal here and the
// fix is not in this fragment: duplicate the view and place the copy. Worth
// knowing before reading a batch that placed eleven of thirty.
//
// EVERY VIEWPORT AT THE SAME POINT, deliberately. Laying out a sheet - what
// goes where, at what scale, aligned with what - is drafting judgement and its
// own job. Stacking them is honest and predictable: a viewport that exists can
// be dragged, and one that was never placed cannot.

var placed = new List<ElementId>();
var refused = new List<ElementId>();

foreach (var view in views)
{
    if (view == null) continue;

    if (!Viewport.CanAddViewToSheet(doc, sheetId, view.Id))
    {
        refused.Add(view.Id);
        continue;
    }

    try
    {
        var viewport = Viewport.Create(doc, sheetId, view.Id, at);

        // A null back without an exception is the silent no-op shape this
        // repository has met on the move and parameter paths. Never counted.
        if (viewport == null) refused.Add(view.Id);
        else placed.Add(viewport.Id);
    }
    catch (Exception)
    {
        refused.Add(view.Id);
    }
}
