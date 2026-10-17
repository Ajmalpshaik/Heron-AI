// NOT STANDALONE. Assumes `uidoc`, `elements` and `alsoSelect` are in scope;
// leaves `shown`, `missing`, `viewAfter` and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing in the model changes - this acts
// on the session.
//
// SELECTING IS NOT SHOWING. SET_SELECTION highlights and does not move the
// view. Three fittings on Level 6, highlighted while a Level 2 plan is open,
// look exactly like a result of nothing.
//
// IT CAN CHANGE THE ACTIVE VIEW. Revit finds a view where the elements are
// visible, which may not be the one on screen. The view it landed on is
// reported so the jump is explainable rather than mysterious.
//
// IF NOTHING CAN SHOW THEM, REVIT PUTS UP A MODAL DIALOG THIS CANNOT SUPPRESS -
// "there is no open view that shows any of the highlighted elements" - and in
// the middle of a batch it stops everything until somebody clicks it. So ids
// that no longer resolve are dropped first: a deleted element is the cheap half
// of that problem and the half worth removing.

var missing = new List<ElementId>();
var findings = new List<string>();
var shown = 0;
var viewAfter = "";

var doc = uidoc.Document;

var ids = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    // Resolved against the model rather than trusted. An id from an earlier
    // step can name something that has since gone, and handing a dead id to
    // Show is one of the ways the dialog appears.
    if (doc.GetElement(element.Id) == null) { missing.Add(element.Id); continue; }

    ids.Add(element.Id);
}

if (ids.Count == 0)
{
    findings.Add("Nothing left to show - none of the elements given still exists in the model. "
        + "Nothing was attempted, deliberately: asking Revit to show a dead id is one of the ways "
        + "the modal dialog appears, and a dialog in the middle of a batch stops everything.");
}
else
{
    if (alsoSelect)
    {
        // Before the show, so the highlight is already on when the view
        // arrives. Selecting is a separate wish from navigating - doing it
        // always would replace a selection somebody was in the middle of
        // building.
        uidoc.Selection.SetElementIds(ids);
    }

    uidoc.ShowElements(ids);

    shown = ids.Count;

    try
    {
        var view = uidoc.ActiveView;
        if (view != null) viewAfter = view.Name;
    }
    catch { }

    findings.Add("Moved the screen to " + shown + " element(s)"
        + (alsoSelect ? " and selected them" : " without changing the selection")
        + (string.IsNullOrEmpty(viewAfter) ? "." : ", now looking at \"" + viewAfter + "\".")
        + " Revit chooses the view, so this may not be the view that was open before.");
}

if (missing.Count > 0)
{
    findings.Add(missing.Count + " element(s) no longer exist and were left out.");
}
