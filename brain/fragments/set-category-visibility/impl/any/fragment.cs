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
        view.SetCategoryHidden(category.Id, !visible);
        changed++;
    }
    catch
    {
        // Almost always a view template holding the setting. Named, so the
        // answer can be "that view has a template" rather than a failure count.
        refused.Add(category.Name);
    }
}
