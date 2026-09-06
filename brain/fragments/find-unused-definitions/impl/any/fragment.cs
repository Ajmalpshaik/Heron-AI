// NOT STANDALONE. Assumes `doc` is in scope, and leaves `unusedTemplates`,
// `unusedFilters`, `filtersUsedOnlyByUnusedTemplates` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// NOBODY SEES A DEFINITION ACCUMULATE.
//
// An unused family type is visible in a browser. An unused view filter sits in
// a dialog nobody opens until they need it, and by then there are fifty and
// the one that matters is lost among them.
//
// A FILTER USED ONLY BY AN UNUSED TEMPLATE IS A DIFFERENT FINDING.
//
// It is referenced, so a plain "is anything using it" says no. But its only
// user is itself unused, which makes it second-order rubbish - and separating
// the two is what lets somebody clear the obvious half without thinking about
// it.
//
// MATERIALS ARE OUT OF SCOPE HERE.
//
// Proving a material unused means walking every element, every compound
// structure and every painted face. A partial answer would be a deletion list
// with real materials in it.

var unusedTemplates = new Dictionary<ElementId, string>();
var unusedFilters = new Dictionary<ElementId, string>();
var filtersUsedOnlyByUnusedTemplates = new List<ElementId>();
var findings = new List<string>();

var views = new List<View>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var view = element as View;
    if (view != null) views.Add(view);
}

// Which templates are applied to a real view.
var templatesInUse = new HashSet<ElementId>();
foreach (var view in views)
{
    if (view.IsTemplate) continue;
    try
    {
        if (view.ViewTemplateId != ElementId.InvalidElementId)
            templatesInUse.Add(view.ViewTemplateId);
    }
    catch { }
}

foreach (var view in views)
{
    if (!view.IsTemplate) continue;
    if (templatesInUse.Contains(view.Id)) continue;
    string name = "";
    try { name = view.Name ?? ""; } catch { }
    unusedTemplates[view.Id] = name;
}

// Which filters are used, and by what kind of view. A template counts as a
// user - it is just a user that may itself be unused.
var usedByRealView = new HashSet<ElementId>();
var usedByTemplate = new HashSet<ElementId>();

foreach (var view in views)
{
    ICollection<ElementId> filters = null;
    try { filters = view.GetFilters(); } catch { }
    if (filters == null) continue;

    foreach (var filterId in filters)
    {
        if (view.IsTemplate) usedByTemplate.Add(filterId);
        else usedByRealView.Add(filterId);
    }
}

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
{
    if (element == null) continue;
    string name = "";
    try { name = element.Name ?? ""; } catch { }

    bool usedSomewhere = usedByRealView.Contains(element.Id) || usedByTemplate.Contains(element.Id);
    if (!usedSomewhere) { unusedFilters[element.Id] = name; continue; }

    if (!usedByRealView.Contains(element.Id))
    {
        // Its only users are templates. Second-order rubbish if all of those
        // templates are themselves unused.
        bool everyUserUnused = true;
        foreach (var view in views)
        {
            if (!view.IsTemplate) continue;
            ICollection<ElementId> filters = null;
            try { filters = view.GetFilters(); } catch { }
            if (filters == null || !filters.Contains(element.Id)) continue;
            if (!unusedTemplates.ContainsKey(view.Id)) { everyUserUnused = false; break; }
        }
        if (everyUserUnused) filtersUsedOnlyByUnusedTemplates.Add(element.Id);
    }
}

findings.Add(unusedTemplates.Count + " view template(s) applied to no view.");
findings.Add(unusedFilters.Count + " view filter(s) used by nothing at all.");
findings.Add(filtersUsedOnlyByUnusedTemplates.Count +
             " further filter(s) used ONLY by templates that are themselves unused.");
findings.Add("Materials are not covered: proving one unused means walking every element, every " +
             "compound structure and every painted face, and a partial answer would list real ones.");
