// NOT STANDALONE. Assumes `doc` is in scope; leaves `findings`,
// `unusedFilters` and `filterCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// NOT EVERY "VIEW" REVIT RETURNS CAN ANSWER THE QUESTION. The project browser
// and the system browser come back from the same collector as real views and
// THROW when asked whether a filter is applied - "the view type does not
// support Visibility/Graphics Overrides". So every probe sits in its own guard:
// one pseudo-view must not take the whole report down. Found on a real model,
// not reasoned about.
//
// A FILTER APPLIED TO NO VIEW IS THE ROW WORTH FINDING. It is either a leftover
// from a standard nobody follows any more or one somebody made and never
// applied, and both look identical to a project that is simply tidy.
//
// BOTH KINDS ARE LISTED. A rule-based filter and a saved set are different
// things doing the same job in a view, and which one a filter is decides
// whether it keeps working as the model grows.

var findings = new List<string>();
var unusedFilters = new List<ElementId>();

var filters = new FilteredElementCollector(doc).OfClass(typeof(FilterElement))
    .Cast<FilterElement>().OrderBy(f => f.Name).ToList();

var filterCount = filters.Count;

// Real views only - templates hold filters but are not where a drawing is, and
// sheets and schedules have none.
var views = new List<View>();
foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
{
    if (view == null || view.IsTemplate) continue;
    if (view.ViewType == ViewType.Schedule || view.ViewType == ViewType.DrawingSheet) continue;
    views.Add(view);
}

foreach (var filter in filters)
{
    var kind = filter is ParameterFilterElement
        ? "a RULE, which keeps working as the model grows"
        : filter is SelectionFilterElement
            ? "a SAVED LIST, which does not follow new elements"
            : "an unfamiliar kind of filter";

    var usedIn = new List<string>();
    foreach (var view in views)
    {
        // One guard per view. See the header.
        try { if (view.IsFilterApplied(filter.Id)) usedIn.Add(view.Name); }
        catch { }
    }

    if (usedIn.Count == 0)
    {
        unusedFilters.Add(filter.Id);
        findings.Add(string.Format("'{0}'  - {1}  - ON NO VIEW. Either a leftover, or made and never "
            + "applied", filter.Name, kind));
    }
    else
    {
        findings.Add(string.Format("'{0}'  - {1}  - used in {2} view(s): {3}",
            filter.Name, kind, usedIn.Count,
            usedIn.Count > 12
                ? string.Join(", ", usedIn.Take(12)) + string.Format(" and {0} more", usedIn.Count - 12)
                : string.Join(", ", usedIn)));
    }
}

findings.Insert(0, string.Format("{0} filter(s) in this project, checked against {1} view(s). {2} are "
    + "on no view at all", filterCount, views.Count, unusedFilters.Count));
