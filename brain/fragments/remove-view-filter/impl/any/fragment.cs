// NOT STANDALONE. Assumes `doc`, `view`, `filterNames` and `deleteFromProject`
// are in scope; leaves `removed`, `deleted` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TAKING A FILTER OFF ONE VIEW AND DELETING IT ARE COMPLETELY DIFFERENT IN
// REACH. Off one view, it stays in the project and on every other view using
// it. Deleted, it goes from EVERY view at once. So deleting is a separate flag,
// off by default.
//
// BEFORE DELETING, THE OTHER-VIEW COUNT IS REPORTED. A filter on one view is a
// leftover; a filter on twenty is somebody's standard.
//
// A FILTER NOT ON THE VIEW IS NOT AN ERROR - it is the state somebody wanted.
// Re-running a tidy-up has to be safe.
//
// A VIEW GOVERNED BY A TEMPLATE OWNS NO FILTERS OF ITS OWN. Removing one there
// does nothing whatever, so those views are NAMED rather than counted as done.

var removed = 0;
var deleted = 0;
var refused = new List<string>();

if (!view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
{
    var template = doc.GetElement(view.ViewTemplateId);
    refused.Add(string.Format("'{0}' is governed by template '{1}', which owns its filters. Removing "
        + "one here does NOTHING - change the template instead", view.Name,
        template == null ? "(unnamed)" : template.Name));
}
else
{
    ICollection<ElementId> onView = null;
    try { onView = view.GetFilters(); }
    catch
    {
        refused.Add(string.Format("'{0}' ({1}) cannot carry filters at all", view.Name, view.ViewType));
    }

    if (onView != null)
    {
        // Every real view, for the "how many others use it" count.
        var allViews = new List<View>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
        {
            var candidate = element as View;
            if (candidate != null && !candidate.IsTemplate) allViews.Add(candidate);
        }

        foreach (var wanted in filterNames)
        {
            if (string.IsNullOrEmpty(wanted)) continue;

            Element filter = null;
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
            {
                if (string.Equals(element.Name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    filter = element;
                    break;
                }
            }

            if (filter == null)
            {
                refused.Add(string.Format("no filter called '{0}' in this project", wanted));
                continue;
            }

            // Count the other views BEFORE removing - afterwards the number has
            // already changed.
            var elsewhere = 0;
            foreach (var candidate in allViews)
            {
                if (candidate.Id == view.Id) continue;
                try { if (candidate.IsFilterApplied(filter.Id)) elsewhere++; }
                catch { }
            }

            if (onView.Contains(filter.Id))
            {
                try
                {
                    view.RemoveFilter(filter.Id);
                    removed++;
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("'{0}' could not be removed from the view - {1}",
                        wanted, ex.Message));
                    continue;
                }
            }
            else
            {
                refused.Add(string.Format("'{0}' was not on this view - nothing to remove, which is "
                    + "the state you asked for", wanted));
            }

            if (deleteFromProject)
            {
                if (elsewhere > 0)
                {
                    refused.Add(string.Format("'{0}' is used on {1} OTHER view(s) and was NOT deleted. "
                        + "Deleting it would change all of them at once; take it off this view only, or "
                        + "say so again knowing the count", wanted, elsewhere));
                }
                else
                {
                    try
                    {
                        doc.Delete(filter.Id);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        refused.Add(string.Format("'{0}' could not be deleted - {1}", wanted, ex.Message));
                    }
                }
            }
            else if (elsewhere > 0)
            {
                refused.Add(string.Format("'{0}' is still on {1} other view(s) and still in the project",
                    wanted, elsewhere));
            }
        }
    }
}
