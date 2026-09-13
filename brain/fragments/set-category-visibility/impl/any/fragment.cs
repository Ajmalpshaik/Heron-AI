// NOT STANDALONE. Assumes `view`, `categories` and `visible` are in scope;
// leaves `changed`, `notControllable` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THIS IS NOT HIDING ELEMENTS, AND THE DIFFERENCE MATTERS.
//
// Hiding elements affects the ones it was given. This affects the CATEGORY in
// this view - every instance of it, including every one modelled after today.
// It is saved into the view, it survives closing the model, and it prints.
//
// Somebody who asked to "hide those three ducts" and got the duct category
// switched off will not notice until a duct they draw next week fails to
// appear. That failure has a long delay on it, which is what makes it worth a
// paragraph here.
//
// ASK WHETHER THE CATEGORY CAN BE CONTROLLED AT ALL. Some cannot, in some
// views, and `get_AllowsVisibilityControl` is Revit answering for itself rather
// than this fragment keeping a list that would go stale. Reported by NAME,
// because "three categories could not be changed" is not something a person can
// act on and "Rebar, Structural Connections" is.
//
// A VIEW TEMPLATE IS THE COMMON REFUSAL. When one controls visibility for this
// view, the setting is not the view's to change and Revit says so. It is caught
// and reported rather than thrown, because "that view has a template" is an
// answer, and a stack trace is not.

var changed = 0;
var alreadyThatWay = 0;
var notControllable = new List<string>();
var refused = new List<string>();

foreach (var category in categories)
{
    if (category == null) continue;

    if (!category.get_AllowsVisibilityControl(view))
    {
        notControllable.Add(category.Name);
        continue;
    }

    try
    {
        // Revit's own call is phrased as HIDDEN, and this fragment is phrased
        // as VISIBLE, because that is how the question is asked out loud.
        //
        // READ FIRST, WRITE, READ BACK - and `changed` used to be `changed++`
        // straight after the Set, which counted the CALL rather than the
        // change. Asked to hide a category already hidden it answered
        // `changed 1`, and a caller checking whether their instruction did
        // anything was told yes when nothing moved. Measured 2026-09-13 on
        // `test projject`: Walls hidden in '{3D}', then hidden again, changed 1.
        // Same shape as FRAGMENT-ISSUES row 20, where `hidden = ids.Count`.
        //
        // A category that was already the way it was asked for is NOT a
        // failure, so it is named rather than dropped - the sibling fragments
        // all do this: set-element-phase has alreadySet, apply-view-template
        // has alreadyOnIt, change-element-type has alreadyThatType.
        var was = view.GetCategoryHidden(category.Id);
        view.SetCategoryHidden(category.Id, !visible);

        if (view.GetCategoryHidden(category.Id) != was) changed++;
        else alreadyThatWay++;
    }
    catch
    {
        // Almost always a view template holding the setting. Named, so the
        // answer can be "that view has a template" rather than a failure count.
        refused.Add(category.Name);
    }
}
