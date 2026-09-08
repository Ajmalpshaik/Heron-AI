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
//
// CanCategoryBeHidden IS NOT A GATE ON THE QUESTION, and using it as one was a
// real defect - found 2026-09-08 against Snowdon Towers Sample HVAC. It answers
// "may this view CHANGE it", not "is it hidden". A view driven by a view
// template answers false to every category, because the template owns the
// setting - so the old loop skipped all 68 and reported "nothing is switched
// off" for 'L2', while GetCategoryHidden said 18 WERE, Levels and HVAC Zones
// among them. Most views in a real project carry a template, so the fragment
// was answering the opposite of the truth in the ordinary case.
//
// GetCategoryHidden reports the EFFECTIVE value and needed no help: asked of
// the view it returned exactly what the template 'Mechanical Plan' returned
// when asked directly. So the question is always asked, and what the template
// owns is REPORTED rather than used to stay silent.

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
var templateOwned = 0;

foreach (var pair in populated)
{
    var hidden = false;
    try { hidden = view.GetCategoryHidden(pair.Key); }
    catch { continue; }

    asked++;

    // Recorded, never used to skip. It is the difference between "this is off"
    // and "this is off and you cannot turn it back on here", which is the next
    // thing the person asks.
    try { if (!view.CanCategoryBeHidden(pair.Key)) templateOwned++; }
    catch { }

    if (hidden)
    {
        hiddenCategories.Add(pair.Key);
        findings.Add(pair.Value + " - SWITCHED OFF");
    }
}

findings.Insert(0, string.Format("{0} of {1} category(ies) present in this model are switched off in "
    + "'{2}'. Categories with nothing in the model are not listed - they are off everywhere by default",
    hiddenCategories.Count, asked, view.Name));

// WHERE THE SETTING LIVES, because "switch it back on" is the next sentence and
// in a templated view it cannot be done here at all.
var byTemplate = templateOwned > 0 && templateOwned == asked;
if (byTemplate)
{
    var templateName = "a view template";
    try
    {
        var owner = doc.GetElement(view.ViewTemplateId) as View;
        if (owner != null) templateName = "the view template '" + owner.Name + "'";
    }
    catch { }

    findings.Add("these are set by " + templateName + ", not in this view. Changing them here is "
        + "not possible - the template decides, and every other view using it follows");
}
else if (templateOwned > 0)
{
    findings.Add(string.Format("{0} of the {1} are set by a view template rather than in this view",
        templateOwned, asked));
}

if (hiddenCategories.Count == 0)
{
    findings.Add("nothing is switched off at CATEGORY level here. If something is still missing from "
        + "the drawing, it is a per-element hide, a view filter set to invisible, a workset, the view "
        + "range, or the phase - DIAGNOSE_VISIBILITY asks all of those for one element");
}
