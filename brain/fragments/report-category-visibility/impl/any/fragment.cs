// NOT STANDALONE. Assumes `doc` and `view` are in scope; leaves `findings` and
// `hiddenCategories` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// IT DELIBERATELY DOES NOT SCOPE TO THE VIEW, and that is the OPPOSITE decision
// from REPORT_CATEGORY_OVERRIDES. A hidden category's elements do not appear in
// a view-scoped collector AT ALL, so scoping this to what the view shows would
// hide exactly the categories it exists to find. Every category with something
// in the MODEL is asked instead.
//
// VISIBILITY IS NOT AN OVERRIDE. Switched off shows nothing; overridden shows
// differently. Missing from a drawing is this question; wrong colour is the
// other fragment's.
//
// A CATEGORY WITH NOTHING IN THE MODEL IS NOT REPORTED - it is off by default in
// every view, and listing hundreds buries the few that matter.

var findings = new List<string>();
var hiddenCategories = new List<ElementId>();

// What actually exists anywhere in the model.
var populated = new Dictionary<ElementId, string>();
foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
{
    var category = element.Category;
    if (category == null) continue;
    if (!populated.ContainsKey(category.Id)) populated[category.Id] = category.Name;
}

var asked = 0;

foreach (var pair in populated)
{
    var canHide = false;
    try { canHide = view.CanCategoryBeHidden(pair.Key); }
    catch { continue; }
    if (!canHide) continue;

    asked++;

    var hidden = false;
    try { hidden = view.GetCategoryHidden(pair.Key); }
    catch { continue; }

    if (hidden)
    {
        hiddenCategories.Add(pair.Key);
        findings.Add(pair.Value + " - SWITCHED OFF");
    }
}

findings.Insert(0, string.Format("{0} of {1} category(ies) present in this model are switched off in "
    + "'{2}'. Categories with nothing in the model are not listed - they are off everywhere by default",
    hiddenCategories.Count, asked, view.Name));

if (hiddenCategories.Count == 0)
{
    findings.Add("nothing is switched off at CATEGORY level here. If something is still missing from "
        + "the drawing, it is a per-element hide, a view filter set to invisible, a workset, the view "
        + "range, or the phase - DIAGNOSE_VISIBILITY asks all of those for one element");
}
