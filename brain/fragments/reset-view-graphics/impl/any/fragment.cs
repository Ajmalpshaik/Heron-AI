// NOT STANDALONE. Assumes `doc`, `view` and `includeAnnotation` are in scope;
// leaves `cleared`, `notOverridable`, `refused` and `stillOverriding` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16). There is
// no dry run for that reason - a fragment may not change and roll back to
// preview, because the batch it sits in is one undo entry.
//
// AN EMPTY OverrideGraphicSettings IS THE CLEARED STATE. Same mechanism
// RESET_GRAPHIC_OVERRIDES uses per element; there is no separate clear call.
//
// IsCategoryOverridable IS THE REAL TEST. Whether a category accepts an
// override depends on the VIEW, not just on the category - reading CategoryType
// instead lets through categories that then throw. Ones that refuse are counted
// and skipped, never failed.
//
// THIS DOES NOT MAKE THE VIEW NORMAL, AND stillOverriding IS WHERE IT SAYS SO.
// Overrides on individual ELEMENTS and overrides carried by the view's FILTERS
// both survive a full category reset and both can leave the view looking
// exactly as wrong as before. A bare count of ninety would be true and
// misleading.

var cleared = 0;
var notOverridable = 0;
var refused = new List<string>();
var stillOverriding = new List<string>();

if (view == null || !view.IsValidObject)
{
    refused.Add("no view was given, so nothing was reset");
}
else if (view.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view template. Category overrides are cleared on a real "
        + "view - clear them there, then capture the view as a template", view.Name));
}
else if (!view.AreGraphicsOverridesAllowed())
{
    refused.Add(string.Format("'{0}' ({1}) does not accept graphic overrides at all, so there are "
        + "none to clear", view.Name, view.ViewType));
}
else
{
    var blank = new OverrideGraphicSettings();

    foreach (Category category in doc.Settings.Categories)
    {
        if (category == null) continue;
        if (category.Id == ElementId.InvalidElementId) continue;
        if (!includeAnnotation && category.CategoryType != CategoryType.Model) continue;

        // The real test. CategoryType alone is not enough and a refusing
        // category throws.
        var overridable = false;
        try { overridable = view.IsCategoryOverridable(category.Id); }
        catch (Exception) { overridable = false; }

        if (!overridable) { notOverridable++; continue; }

        try
        {
            view.SetCategoryOverrides(category.Id, blank);
            cleared++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' - would not clear: {1}", category.Name, ex.Message));
        }
    }

    // What survives this, read off the view. The person asked for the view back
    // to normal; a count of cleared categories does not answer that on its own.
    try
    {
        var filterIds = view.GetFilters();
        var colouring = 0;
        foreach (var filterId in filterIds)
        {
            try
            {
                var filterOverride = view.GetFilterOverrides(filterId);
                if (filterOverride == null) continue;
                if (filterOverride.Halftone
                    || filterOverride.Transparency > 0
                    || filterOverride.ProjectionLineColor.IsValid
                    || filterOverride.CutLineColor.IsValid
                    || filterOverride.SurfaceForegroundPatternId != ElementId.InvalidElementId
                    || filterOverride.CutForegroundPatternId != ElementId.InvalidElementId)
                {
                    colouring++;
                }
            }
            catch (Exception) { }
        }

        if (colouring > 0)
        {
            stillOverriding.Add(string.Format("{0} view filter(s) on '{1}' still carry graphics of "
                + "their own and are UNTOUCHED by this. If the view still looks wrong, that is the "
                + "first place to look - REMOVE_VIEW_FILTER takes one off", colouring, view.Name));
        }
    }
    catch (Exception ex)
    {
        stillOverriding.Add(string.Format("the view's filters could not be read, so whether any is "
            + "still colouring '{0}' is unknown: {1}", view.Name, ex.Message));
    }

    stillOverriding.Add("overrides set on individual ELEMENTS are untouched by this and still win "
        + "over the category. RESET_GRAPHIC_OVERRIDES clears those, and a view that was greyed "
        + "element by element needs it as well as this");
}
