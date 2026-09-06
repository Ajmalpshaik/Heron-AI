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
// A TEMPLATE CAN BE IN USE WITH NO VIEW POINTING AT IT.
//
// A view family type names the template that NEW views of that kind start
// with. Nothing follows that template today, so a check that walks views alone
// calls it unused - and deleting it silently changes what every new plan is
// created with, with nothing on screen having said so. Corrected 2026-09-06,
// found while building SELECT_VIEW_TEMPLATES; the two now ask the same
// question the same way.
//
// MATERIALS ARE OUT OF SCOPE HERE.
//
// Proving a material unused means walking every element, every compound
// structure and every painted face. A partial answer would be a deletion list
// with real materials in it. FIND_UNUSED_MATERIALS is that walk, kept separate
// because its cost is different by orders of magnitude.

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

// And which templates a view family type hands to new views. This is the use
// that leaves no view pointing at the template, so walking views alone misses
// it entirely.
var templatesUsedAsViewTypeDefault = new HashSet<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
{
    var viewFamilyType = element as ViewFamilyType;
    if (viewFamilyType == null) continue;
    try
    {
        var defaultId = viewFamilyType.DefaultTemplateId;
        if (defaultId != null && defaultId != ElementId.InvalidElementId)
            templatesUsedAsViewTypeDefault.Add(defaultId);
    }
    catch { }
}

var templatesKeptForViewTypeDefault = 0;

foreach (var view in views)
{
    if (!view.IsTemplate) continue;
    if (templatesInUse.Contains(view.Id)) continue;
    if (templatesUsedAsViewTypeDefault.Contains(view.Id))
    {
        // In use, invisibly. Reported as kept rather than added to a list
        // somebody clears without thinking.
        templatesKeptForViewTypeDefault++;
        continue;
    }
    string name = "";
    try { name = view.Name ?? ""; } catch { }
    unusedTemplates[view.Id] = name;
}

if (templatesKeptForViewTypeDefault > 0)
{
    findings.Add(templatesKeptForViewTypeDefault + " template(s) have no view following them and "
        + "are still IN USE - a view family type names each as the template new views of that "
        + "kind start with. They are kept out of the unused list on purpose: deleting one "
        + "changes what every new view of that kind is created with, and nothing on screen "
        + "says so.");
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
