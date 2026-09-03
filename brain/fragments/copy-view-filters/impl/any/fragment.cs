// NOT STANDALONE. Assumes `doc`, `sourceView`, `targetViews` and
// `onlyFilterNamed` are in scope; leaves `viewsChanged`, `filtersApplied`,
// `blockedByTemplate` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ADDING THE FILTER IS NOT COPYING IT. AddFilter puts the filter on the view
// with EMPTY overrides. The look lives in a second object fetched with
// GetFilterOverrides, and the on/off state in a third. Copy only the first and
// twelve views carry the right filters and show no change at all - which reads
// as the job not having run. All three move here.
//
// A VIEW GOVERNED BY A TEMPLATE WILL NOT TAKE ITS OWN FILTERS. The template
// owns Visibility/Graphics. Those views are NAMED with their template rather
// than counted as failures, because the fix - copy onto the template instead -
// does every view using it at once.
//
// SOME VIEW TYPES CANNOT CARRY FILTERS AT ALL - schedules, legends, sheets.
// GetFilters THROWS on them, so every view is asked inside its own guard: one
// unsuitable view must not take the batch down.

var viewsChanged = 0;
var filtersApplied = 0;
var blockedByTemplate = new List<string>();
var refused = new List<string>();

ICollection<ElementId> sourceFilterIds = null;
try { sourceFilterIds = sourceView.GetFilters(); }
catch { refused.Add(string.Format("source view '{0}' cannot carry filters at all", sourceView.Name)); }

var toCopy = new List<ElementId>();
if (sourceFilterIds != null)
{
    foreach (var id in sourceFilterIds)
    {
        if (!string.IsNullOrEmpty(onlyFilterNamed))
        {
            var f = doc.GetElement(id);
            if (f == null || f.Name != onlyFilterNamed) continue;
        }
        toCopy.Add(id);
    }
}

if (sourceFilterIds != null && sourceFilterIds.Count == 0)
{
    refused.Add(string.Format("source view '{0}' has no filters on it - nothing to copy", sourceView.Name));
}
else if (sourceFilterIds != null && toCopy.Count == 0)
{
    var have = new List<string>();
    foreach (var id in sourceFilterIds)
    {
        var f = doc.GetElement(id);
        if (f != null) have.Add(f.Name);
    }
    refused.Add(string.Format("filter '{0}' is not on '{1}'. It carries: {2}",
        onlyFilterNamed, sourceView.Name, string.Join(", ", have)));
}

foreach (var view in targetViews)
{
    if (view == null || !view.IsValidObject) continue;
    if (view.Id == sourceView.Id) continue;
    if (toCopy.Count == 0) break;

    // A view driven by a template cannot take its own filters - the template
    // owns them. Named, not counted as a failure.
    if (!view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
    {
        var template = doc.GetElement(view.ViewTemplateId);
        blockedByTemplate.Add(string.Format("'{0}' is governed by template '{1}' - copy onto that "
            + "template instead and every view using it changes at once",
            view.Name, template == null ? "(unnamed)" : template.Name));
        continue;
    }

    ICollection<ElementId> existing = null;
    try { existing = view.GetFilters(); }
    catch
    {
        refused.Add(string.Format("'{0}' ({1}) cannot carry filters", view.Name, view.ViewType));
        continue;
    }

    var appliedHere = 0;
    foreach (var filterId in toCopy)
    {
        var filter = doc.GetElement(filterId);
        var filterName = filter == null ? filterId.ToString() : filter.Name;
        try
        {
            if (!existing.Contains(filterId)) view.AddFilter(filterId);

            // The two halves AddFilter does NOT carry. See the header.
            view.SetFilterOverrides(filterId, sourceView.GetFilterOverrides(filterId));
            view.SetFilterVisibility(filterId, sourceView.GetFilterVisibility(filterId));

            appliedHere++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}': filter '{1}' - {2}", view.Name, filterName, ex.Message));
        }
    }

    if (appliedHere > 0)
    {
        viewsChanged++;
        filtersApplied += appliedHere;
    }
}
