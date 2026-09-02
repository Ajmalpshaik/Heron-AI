// NOT STANDALONE. Assumes `doc`, `view` and `elements` are in scope; leaves
// `visible` and `reasons` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// GROUND TRUTH FIRST, EXPLANATION SECOND.
//
// A collector scoped to the view returns what the view ACTUALLY contains. That
// is asked once, up front, and it settles the question. Everything after it is
// explaining a fact rather than predicting one - which is what separates this
// from a checklist that can be confidently wrong about a view it never looked
// at.
//
// EVERY CAUSE, NOT THE FIRST. Two at once is routine: a category switched off
// by a template AND the element hidden by somebody. Stopping at the first
// found sends the user to fix one thing, after which it is still invisible -
// and that is where the second hour goes.
//
// WHAT THIS CANNOT SEE, AND SAYS SO RATHER THAN GUESSING. Design options,
// worksets not loaded, and elements outside the view range are all real causes
// that need reads this fragment does not make. When the collector says absent
// and none of the checked causes fired, it reports exactly that instead of
// inventing the likeliest - an invented cause reads identically to a found one.

var visible = new List<ElementId>();
var reasons = new Dictionary<ElementId, string>();

// What the view really contains. One pass, reused for every element.
var shown = new HashSet<ElementId>(
    new FilteredElementCollector(doc, view.Id).ToElementIds());

// A template controls the category switches when one is attached, so the
// answer about "the category is off" has to name where to go and change it.
var template = view.ViewTemplateId != null
    && view.ViewTemplateId != ElementId.InvalidElementId
    ? doc.GetElement(view.ViewTemplateId) as View
    : null;

var filterCount = 0;
try
{
    var filters = view.GetFilters();
    filterCount = filters == null ? 0 : filters.Count;
}
catch (Exception)
{
    filterCount = 0;
}

foreach (var element in elements)
{
    if (element == null) continue;
    if (visible.Contains(element.Id) || reasons.ContainsKey(element.Id)) continue;

    if (shown.Contains(element.Id))
    {
        visible.Add(element.Id);
        continue;
    }

    var found = new List<string>();

    // 1. Hidden element by element. SHOW_ELEMENTS is the fix.
    try
    {
        if (element.IsHidden(view)) found.Add("hidden in this view");
    }
    catch (Exception)
    {
        // Cannot be asked - not a cause, and not a reason to stop.
    }

    // 2. Its whole category switched off. SET_CATEGORY_VISIBILITY is the fix,
    //    and it has to be applied where the switch actually lives.
    if (element.Category != null)
    {
        try
        {
            if (view.GetCategoryHidden(element.Category.Id))
            {
                found.Add(template != null
                    ? "its category is switched off - by the view template '"
                      + template.Name + "', so change it there"
                    : "its category is switched off in this view");
            }
        }
        catch (Exception)
        {
        }
    }

    // 3. Cropped out. Reported as possible rather than certain: the crop is a
    //    region and this does not compute whether the element falls outside it.
    if (view.CropBoxActive) found.Add("the crop is on and may exclude it");

    // 4. A filter may hide it. Same honesty - the filter's rules are not
    //    evaluated here, only their presence reported.
    if (filterCount > 0)
    {
        found.Add(filterCount + " view filter(s) are applied and one may hide it");
    }

    reasons[element.Id] = found.Count > 0
        ? string.Join("; ", found.ToArray())
        : "not in this view, and none of the checked causes applies - look at "
          + "the view range, the phase, design options and worksets, which this "
          + "does not read";
}
