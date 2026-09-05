// NOT STANDALONE. Assumes `doc`, `view` and `categories` are in scope; leaves
// `elements` and `findings` behind.
//
// THIS IS REVIT'S OWN ANSWER, NOT A RE-IMPLEMENTATION OF ONE. The view-scoped
// collector applies the view's crop, view range, phase filter, category
// switches, view filters, worksets and template. Rebuilding that logic by hand
// would be eight mechanisms to get right and to keep right across releases.
//
// "VISIBLE IN THIS VIEW" IS NOT "EXISTS IN THE MODEL", and the difference is
// the reason this fragment exists. Counting the model and reporting it as the
// drawing is how a schedule and a plan come to disagree.
//
// A TEMPLATE IS REFUSED. It has no elements of its own - it is a set of
// settings applied to views - so an empty answer from one would read as "this
// view shows nothing".

var elements = new List<Element>();
var findings = new List<string>();

if (view == null)
{
    findings.Add("No view was given - name the view to look at");
}
else if (view.IsTemplate)
{
    findings.Add(string.Format("'{0}' is a view TEMPLATE, not a view. A template has no elements of "
        + "its own - it is settings applied to views - so this is refused rather than answered with "
        + "an empty set", view.Name));
}
else
{
    var collector = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    foreach (var element in collector) elements.Add(element);

    findings.Add(string.Format("{0} element(s) are showing in view '{1}'{2}. This is what the view "
        + "SHOWS, which is not what the model contains - the crop, the view range, the phase filter, "
        + "the category switches, the view filters and the template all remove things that are "
        + "certainly there",
        elements.Count,
        view.Name,
        categories == null || categories.Count == 0
            ? ", across every category"
            : string.Format(", within {0} category/categories", categories.Count)));

    findings.Add("Something expected and missing is a question for DIAGNOSE_VISIBILITY, which takes "
        + "one element and one view and says which mechanism hid it. This fragment reports the set "
        + "and no reasons");
}
