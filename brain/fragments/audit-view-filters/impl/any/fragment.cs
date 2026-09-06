// NOT STANDALONE. Assumes `doc` and `view` are in scope, and leaves
// `findings`, `disabled`, `hidden`, `blankOverride`, `nothingToActOn`,
// `templateOwned` and `filterCount` behind.
//
// READ ONLY. Opens no transaction and needs none - including for the disabled
// filters it finds. Switching one back on is a write, and a write hidden
// inside an audit is how a diagnosis becomes a change nobody asked for.
//
// FOUR STATES, ONE SYMPTOM.
//
// "The filter is on the view and nothing is coloured" has four unrelated
// causes and Revit flags none of them:
//
//   switched off       still listed, greyed, doing nothing
//   visibility off     it HIDES what it matches instead of colouring it -
//                      a different switch from the overrides, same list
//   categories off     the filter only ever acts on what the view is already
//                      drawing. All categories hidden here means a correct
//                      filter with nothing to act on
//   empty override     adding a filter without setting one leaves a blank
//                      override: present, enabled, matching, and identical to
//                      having no filter
//
// THE TEMPLATE IS REPORTED FIRST BECAUSE IT EXPLAINS THE REST AT ONCE.
//
// Where a template controls the filters slot, everything set on the view is
// discarded when the template reasserts. Saying it once at the top is the
// difference between one answer and a list of rows nobody can act on.
//
// A VIEW THAT CANNOT CARRY OVERRIDES AT ALL is said to be one. "0 filters" on
// a schedule is true and useless.

// WHAT COUNTS AS A BLANK OVERRIDE.
//
// There is no "is this override empty" call, so every slot an override can
// carry is asked and the answer is "blank" only when NONE of them is set. Read
// the other way round - looking for one particular thing being set - a filter
// carrying only a halftone or only a transparency would be reported as blank,
// which is a wrong finding on a filter that is working.
Func<OverrideGraphicSettings, bool> isBlank = settings =>
{
    try
    {
        if (settings.Halftone) return false;
        if (settings.Transparency > 0) return false;
        if (settings.DetailLevel != ViewDetailLevel.Undefined) return false;

        if (settings.ProjectionLineColor != null && settings.ProjectionLineColor.IsValid) return false;
        if (settings.CutLineColor != null && settings.CutLineColor.IsValid) return false;
        if (settings.SurfaceForegroundPatternColor != null && settings.SurfaceForegroundPatternColor.IsValid) return false;
        if (settings.SurfaceBackgroundPatternColor != null && settings.SurfaceBackgroundPatternColor.IsValid) return false;
        if (settings.CutForegroundPatternColor != null && settings.CutForegroundPatternColor.IsValid) return false;
        if (settings.CutBackgroundPatternColor != null && settings.CutBackgroundPatternColor.IsValid) return false;

        if (settings.ProjectionLineWeight > 0) return false;
        if (settings.CutLineWeight > 0) return false;

        if (settings.ProjectionLinePatternId != ElementId.InvalidElementId) return false;
        if (settings.CutLinePatternId != ElementId.InvalidElementId) return false;
        if (settings.SurfaceForegroundPatternId != ElementId.InvalidElementId) return false;
        if (settings.SurfaceBackgroundPatternId != ElementId.InvalidElementId) return false;
        if (settings.CutForegroundPatternId != ElementId.InvalidElementId) return false;
        if (settings.CutBackgroundPatternId != ElementId.InvalidElementId) return false;
    }
    catch
    {
        // A slot that cannot be read is not evidence of blankness. Saying "not
        // blank" here means the audit stays quiet rather than reporting a
        // finding it cannot stand up.
        return false;
    }
    return true;
};

var findings = new List<string>();
var disabled = new List<ElementId>();
var hidden = new List<ElementId>();
var blankOverride = new List<ElementId>();
var nothingToActOn = new List<ElementId>();
bool templateOwned = false;
int filterCount = 0;

bool overridesAllowed = true;
try { overridesAllowed = view.AreGraphicsOverridesAllowed(); } catch { }

if (!overridesAllowed)
{
    findings.Add("'" + view.Name + "' (" + view.ViewType.ToString() +
                 ") does not accept graphic overrides at all, so no filter can " +
                 "apply here. There is nothing to audit - this is not the same " +
                 "as a view with no filters.");
}
else
{
    List<ElementId> filterIds = new List<ElementId>();
    try { filterIds = view.GetFilters().ToList(); } catch { }
    filterCount = filterIds.Count;

    // Which template parameters the view still controls itself. Anything NOT
    // in that list belongs to the template, and the filters slot is one entry
    // in it.
    try
    {
        if (!view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
        {
            var selfControlled = new HashSet<ElementId>(view.GetNonControlledTemplateParameterIds());
            templateOwned = !selfControlled.Contains(new ElementId(BuiltInParameter.VIS_GRAPHICS_FILTERS));

            var template = doc.GetElement(view.ViewTemplateId) as View;
            if (templateOwned)
                findings.Add("The view template '" + (template == null ? "?" : template.Name) +
                             "' owns the filters on this view. Anything changed here is " +
                             "discarded when the template reasserts - change it ON THE TEMPLATE.");
        }
    }
    catch { }

#if REVIT2020
    if (filterCount > 0)
        findings.Add("This release has no enabled/disabled switch for view filters, " +
                     "so that check was SKIPPED, not passed.");
#endif

    foreach (var filterId in filterIds)
    {
        var filter = doc.GetElement(filterId);
        string name = filter == null ? filterId.ToString() : filter.Name;

#if !REVIT2020
        // The one that wastes the afternoon: listed, greyed, doing nothing.
        bool enabled = true;
        try { enabled = view.GetIsFilterEnabled(filterId); } catch { }
        if (!enabled)
        {
            disabled.Add(filterId);
            findings.Add("'" + name + "' is APPLIED BUT SWITCHED OFF on this view. " +
                         "It is in the list and it does nothing.");
        }
#endif

        // Visibility is a separate switch from the overrides, and off means
        // the matches are HIDDEN rather than coloured.
        bool visible = true;
        try { visible = view.GetFilterVisibility(filterId); } catch { }
        if (!visible)
        {
            hidden.Add(filterId);
            findings.Add("'" + name + "' has its VISIBILITY off, so everything it " +
                         "matches is hidden in this view rather than coloured.");
        }

        // A blank override is invisible in every sense: present, enabled,
        // matching, and identical to having no filter at all.
        try
        {
            var settings = view.GetFilterOverrides(filterId);
            if (settings != null && isBlank(settings))
            {
                blankOverride.Add(filterId);
                findings.Add("'" + name + "' has an EMPTY override - nothing is set on it. " +
                             "Adding a filter without setting one leaves exactly this, and it " +
                             "looks identical to having no filter.");
            }
        }
        catch { }

        // A filter only ever acts on what the view already draws. Every
        // category hidden here means a correct filter with nothing to act on -
        // and the fix is the category, not the rule.
        try
        {
            var rule = filter as ParameterFilterElement;
            if (rule != null)
            {
                var categories = rule.GetCategories();
                if (categories != null && categories.Count > 0)
                {
                    bool anyDrawn = false;
                    foreach (var categoryId in categories)
                    {
                        bool categoryHidden = false;
                        try { categoryHidden = view.GetCategoryHidden(categoryId); } catch { }
                        if (!categoryHidden) { anyDrawn = true; break; }
                    }
                    if (!anyDrawn)
                    {
                        nothingToActOn.Add(filterId);
                        findings.Add("'" + name + "' has every one of its categories switched off " +
                                     "in this view, so it has nothing to act on. The rule is fine; " +
                                     "the category visibility is what is hiding the result.");
                    }
                }
            }
        }
        catch { }
    }

    if (filterCount == 0)
        findings.Add("No filters are applied to '" + view.Name + "' at all.");
}
