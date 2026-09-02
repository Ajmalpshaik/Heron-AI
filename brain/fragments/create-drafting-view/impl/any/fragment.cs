// NOT STANDALONE. Assumes `doc`, `viewName` and `viewScale` are in scope;
// leaves `created` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE VIEW TYPE IS FOUND BY WHAT IT IS. Matching ViewFamily.Drafting finds the
// right type in any template; matching the name "Drafting View" finds it in an
// English project set up the default way and nowhere else.
//
// THE SCALE IS ASKED FOR BECAUSE A DRAFTING VIEW HAS NOTHING TO INFER IT FROM.
// A plan takes its scale from what it is showing; a drafting view is empty, so
// whatever Revit's default happens to be is what the detail gets drawn at - and
// a detail drawn at 1:100 and issued at 1:5 is a redraw, not an adjustment.
//
// A NAME CLASH THROWS, so it is checked first - the same order as CREATE_SHEET,
// CREATE_LEVEL and CREATE_GRID, and for the same reason: Revit creates the view
// and then refuses the name, leaving an unnamed view in the browser.

ElementId created = null;
string refused = null;

var name = (viewName ?? "").Trim();

ViewFamilyType draftingType = null;
foreach (var candidate in new FilteredElementCollector(doc)
                              .OfClass(typeof(ViewFamilyType))
                              .Cast<ViewFamilyType>())
{
    if (candidate.ViewFamily == ViewFamily.Drafting) { draftingType = candidate; break; }
}

View clash = null;
if (name.Length > 0)
{
    foreach (var existing in new FilteredElementCollector(doc)
                                 .OfClass(typeof(View))
                                 .Cast<View>())
    {
        if (existing == null || existing.IsTemplate) continue;
        if (string.Equals(existing.Name ?? "", name, StringComparison.OrdinalIgnoreCase))
        {
            clash = existing;
            break;
        }
    }
}

if (draftingType == null)
{
    refused = "this project's template carries no Drafting view type, so there is "
            + "nothing to create one from";
}
else if (clash != null)
{
    refused = string.Format(
        "a view called \"{0}\" already exists - Revit refuses a duplicate name, and "
        + "creating it first and renaming after leaves an unnamed view in the browser",
        name);
}
else if (viewScale <= 0)
{
    // A drafting view has nothing to infer a scale from, so a missing one is a
    // real gap rather than something to default.
    refused = "give the scale. A drafting view is empty, so it has nothing to take a "
            + "scale from, and a detail drawn at the wrong one is a redraw";
}
else
{
    var view = ViewDrafting.Create(doc, draftingType.Id);

    if (view == null)
    {
        refused = "Revit declined to create the drafting view";
    }
    else
    {
        view.Scale = viewScale;
        if (name.Length > 0) view.Name = name;
        created = view.Id;
    }
}
