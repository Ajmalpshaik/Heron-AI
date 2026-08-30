// NOT STANDALONE. Assumes `view`, `categories` and `overrides` are in scope;
// leaves `overridden`, `notControllable` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHY A GRAYOUT SHOULD DO ITS BACKGROUND THIS WAY.
//
// Overriding every wall, floor and ceiling one at a time gives the right
// picture TODAY and a wrong one tomorrow: a wall drawn after the grayout ran
// keeps its normal graphics, because nothing ever touched it. The drawing
// quietly stops matching itself, and the person who notices is whoever prints
// it.
//
// A category override covers everything in the category, including what does
// not exist yet. It is also one setting to clear instead of four hundred.
//
// AN EMPTY SETTINGS OBJECT CLEARS IT. Same as the per-element reset - there is
// no separate clear call, and passing a fresh OverrideGraphicSettings removes
// whatever the category had. So this fragment applies AND undoes, depending
// entirely on what it is handed.
//
// IT REPLACES RATHER THAN MERGES, exactly like the per-element version. To
// change one aspect and keep the rest, read the category's current overrides
// with `view.GetCategoryOverrides(id)`, modify that, and pass it back.
//
// CATEGORIES THAT CANNOT BE CONTROLLED ARE NAMED, not counted. "Two categories
// were skipped" is not something a person can act on.

var overridden = 0;
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
        view.SetCategoryOverrides(category.Id, overrides);
        overridden++;
    }
    catch
    {
        // A view template holding the graphics, almost always. Named, so the
        // answer is "that view has a template" rather than a failure count.
        refused.Add(category.Name);
    }
}
