// NOT STANDALONE. Assumes `doc`, `sourceView`, `targetViews` and
// `onlyFilterNamed` are in scope; leaves `viewsChanged`, `filtersApplied`,
// `blockedByTemplate`, `refused` and `findings` behind.
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
//
// VERSION 2 (row 5b-1853): THE SOURCE'S ORDER, AND ITS ENABLE FILTER TICK.
// Version 1 copied in `GetFilters` order, which Revit's 2021-2027 reference
// says is sorted by id, and `AddFilter` appends at the bottom - so the copies
// stood in the order the filters were MADE, and where two set the same thing
// on one element the target drew differently from the source. It also left
// Enable Filter behind, so a filter unticked on the source started drawing on
// the target. From 2021 the source's own order is read (`GetOrderedFilters`)
// and each copy carries its tick (`Get/SetIsFilterEnabled`), read back. All
// three are reached BY NAME - Revit 2020 has none of them (tools/api-surface,
// row 5b-1852) - the pattern REORDER_VIEW_FILTERS and APPLY_VIEW_FILTER use.
// On 2020 the API cannot read the Filters tab's order, so the copies go in id
// order and `findings` says so; 2020 has no Enable Filter tick to carry. A
// filter already on a target keeps its place there - only its look is copied.

var viewsChanged = 0;
var filtersApplied = 0;
var blockedByTemplate = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var getOrdered = typeof(View).GetMethod("GetOrderedFilters", Type.EmptyTypes);
var getEnabled = typeof(View).GetMethod("GetIsFilterEnabled", new[] { typeof(ElementId) });
var setEnabled = typeof(View).GetMethod("SetIsFilterEnabled", new[] { typeof(ElementId), typeof(bool) });

ICollection<ElementId> sourceFilterIds = null;
try
{
    sourceFilterIds = getOrdered != null
        ? (ICollection<ElementId>)getOrdered.Invoke(sourceView, null)
        : sourceView.GetFilters();
}
catch { refused.Add(string.Format("source view '{0}' cannot carry filters at all", sourceView.Name)); }
if (getOrdered == null && sourceFilterIds != null && sourceFilterIds.Count > 1)
    findings.Add("Revit 2020 cannot tell Heron the order of the source's filters in Visibility/Graphics > "
        + "Filters, so the copies were added in the order the filters were made - check the order on "
        + "each target's Filters tab, and REORDER_VIEW_FILTERS moves one");
var keptPlace = 0;
var tickFailed = new List<string>();

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
            else keptPlace++;

            // The two halves AddFilter does NOT carry. See the header.
            view.SetFilterOverrides(filterId, sourceView.GetFilterOverrides(filterId));
            view.SetFilterVisibility(filterId, sourceView.GetFilterVisibility(filterId));

            // VERSION 2: the Enable Filter tick, 2021 on, read back.
            if (getEnabled != null && setEnabled != null)
            {
                var wanted = (bool)getEnabled.Invoke(sourceView, new object[] { filterId });
                setEnabled.Invoke(view, new object[] { filterId, wanted });
                if ((bool)getEnabled.Invoke(view, new object[] { filterId }) != wanted)
                    tickFailed.Add(string.Format("'{0}': filter '{1}'", view.Name, filterName));
            }

            appliedHere++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}': filter '{1}' - {2}", view.Name, filterName,
                (ex.InnerException ?? ex).Message));
        }
    }

    if (appliedHere > 0)
    {
        viewsChanged++;
        filtersApplied += appliedHere;
    }
}

if (keptPlace > 0)
    findings.Add(string.Format("{0} filter(s) were already on a target and kept their place in its list - only "
        + "their look and visibility were copied", keptPlace));
if (tickFailed.Count > 0)
    findings.Add("Enable Filter did not read back as on the source for: " + string.Join(", ", tickFailed));
if (getOrdered != null && filtersApplied > 0)
    findings.Add("Copied in the source view's own order, each with its Enable Filter tick; a target that "
        + "already had filters has the copies below its own");
