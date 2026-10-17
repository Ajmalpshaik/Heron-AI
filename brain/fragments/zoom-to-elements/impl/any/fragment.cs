// NOT STANDALONE. Assumes `uidoc`, `elements` and `alsoSelect` are in scope;
// leaves `shown`, `missing`, `viewAfter` and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing in the model changes - this acts
// on the session.
//
// VERSION 2 - IT RUNS WITH NO TRANSACTION OPEN. revit_change is the only door
// a chat has for an EXECUTE fragment, and it ran version 1 inside Heron's
// transaction - where Revit changes no view: "The active view can only be
// changed when there is no open transaction" (RevitAPIUI.xml 2024,
// UIDocument.ActiveView), measured for RequestViewChange on Revit 2020 (row
// 5b-456). Show moves to another view whenever the one in front cannot see the
// elements. The card declares the ambient `editScopeFailures` need, and the
// add-in runs a fragment declaring it inside the job's group with no
// transaction open (D-115). This opens none and never touches that need.
// Whether Show itself is refused inside a transaction is NOT measured; with the
// switch the answer is the same either way.
//
// IN THE FAMILY EDITOR it is the same call: nothing here asks what kind of
// document it is. Revit shows the forms in the view in front when that view
// can see them, so the family method opens the 3D view first (OPEN_VIEW
// "View 1") and then zooms it to the family's forms.
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

    var showRefused = "";
    try { uidoc.ShowElements(ids); }
    catch (Exception ex) { showRefused = ex.Message; }

    if (showRefused.Length > 0)
    {
        // REVIT'S WORDS FIRST. A finding reaches the chat cut to about sixty
        // characters, and a reason at the end of one is never seen (5b-456).
        findings.Add(showRefused + " - so Revit did not move the screen to the "
            + ids.Count + " element(s) and the view is UNCHANGED. Nothing in the model was altered."
            + (alsoSelect ? " They ARE selected: the selection is set before the show." : "")
            + (doc.IsModifiable
                ? " A transaction was open around this run, and Revit changes no view inside "
                  + "one: the add-in did not run this with none open."
                : ""));
    }
    else
    {
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
}

if (missing.Count > 0)
{
    findings.Add(missing.Count + " element(s) no longer exist and were left out.");
}
