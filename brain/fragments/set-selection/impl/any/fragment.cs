// NOT STANDALONE. Assumes `uidoc`, `elements`, `show` and `keepSelection` are
// already in scope, and leaves `selectedCount`, `shown`, `missing`,
// `viewAfter` and `findings` for whatever reports the result.
//
// Reads nothing back from Revit's own selection on purpose: the caller asked
// for these elements to be selected, and re-reading to confirm would be
// testing Revit rather than this fragment.
//
// VERSION 2 (2026-11-01) - ZOOM_TO_ELEMENTS MERGED IN, AND WHAT IT HAD THAT
// THIS LACKED:
//
//   `show` - Revit's own Show, which MOVES THE SCREEN to the elements.
//   SELECTING IS NOT SHOWING: a highlight on three fittings on Level 6 while a
//   Level 2 plan is open looks exactly like a result of nothing. Show CAN
//   CHANGE THE ACTIVE VIEW - Revit finds a view where the elements are
//   visible - so the view it landed on is reported in `viewAfter`, and the
//   jump is explainable rather than mysterious.
//
//   `keepSelection` - with `show`, the camera only: the selection is left as
//   it was. "Where is it" wants the camera moved and nothing touched; doing
//   both always would replace a selection somebody was part-way through
//   building. Without `show` there is nothing to do but select, so it is
//   refused in a finding and nothing is touched.
//
//   `missing` - ids that no longer resolve to an element are LEFT OUT, on
//   every call: an id from an earlier step can name something since deleted,
//   and handing a dead id to Show is one of the ways Revit's modal dialog
//   appears ("there is no open view that shows any of the highlighted
//   elements"), which stops a batch until somebody clicks it. That dialog
//   cannot be suppressed when the elements exist but no view can show them,
//   so Show belongs at the END of a job, never inside a loop. With nothing
//   left, Show is not attempted at all.
//
//   THE SAFER RULE IN EACH CONFLICT IS KEPT. Version 1 handed every id to the
//   selection, dead ones too; a dead id is now left out of the selection as
//   well as the Show. An empty set still CLEARS the selection, as version 1 -
//   never left holding the previous one, which reads on screen as a success -
//   unless `keepSelection` asked for it to be left alone.

var missing = new List<ElementId>();
var findings = new List<string>();
var shown = 0;
var viewAfter = "";

var doc = uidoc.Document;

var ids = new List<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;

    // Resolved against the model rather than trusted.
    if (doc.GetElement(element.Id) == null) { missing.Add(element.Id); continue; }
    ids.Add(element.Id);
}

var selectedCount = 0;

if (keepSelection && !show)
{
    findings.Add("keepSelection leaves the selection alone, and without show there is nothing "
        + "else to do - NOTHING WAS CHANGED. Ask for show too, to move the screen to them.");
}
else
{
    if (!keepSelection)
    {
        // Before the show, so the highlight is already on when the view
        // arrives. An empty list CLEARS the selection (version 1).
        uidoc.Selection.SetElementIds(ids);
        selectedCount = ids.Count;
    }

    if (show)
    {
        if (ids.Count == 0)
        {
            findings.Add("Nothing left to show - none of the elements given still exists in the "
                + "model. Show was not attempted, deliberately: asking Revit to show nothing, or a "
                + "dead id, is one of the ways its modal dialog appears.");
        }
        else
        {
            uidoc.ShowElements(ids);
            shown = ids.Count;

            try
            {
                var view = uidoc.ActiveView;
                if (view != null) viewAfter = view.Name;
            }
            catch { }

            findings.Add("Moved the screen to " + shown + " element(s)"
                + (keepSelection ? " without changing the selection" : " and selected them")
                + (string.IsNullOrEmpty(viewAfter) ? "." : ", now looking at \"" + viewAfter + "\".")
                + " Revit chooses the view, so this may not be the view that was open before.");
        }
    }
}

if (missing.Count > 0)
{
    findings.Add(missing.Count + " element(s) no longer exist and were left out.");
}
