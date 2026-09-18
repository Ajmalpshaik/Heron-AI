// NOT STANDALONE. Assumes `doc`, `view` and `categories` are in scope; leaves
// `elements`, `inModel`, `showing` and `findings`.
//
// A READ. It opens no transaction and needs none.
//
// THIS IS THE SUBTRACTION NOTHING DID. SELECT_VISIBLE_IN_VIEW gives what shows;
// DIAGNOSE_VISIBILITY explains ONE element. Neither answers "which ones are
// missing", which is the question you need answered BEFORE you know what to
// diagnose.
//
// BOTH HALVES ARE REPORTED so the arithmetic can be checked: inModel minus
// showing should be what came back, and a reader who cannot check that has to
// trust it.
//
// IT NEVER SAYS WHY. That is DIAGNOSE_VISIBILITY, one element at a time.

var elements = new List<Element>();
var inModel = 0;
var showing = 0;
var findings = new List<string>();

if (view == null)
{
    findings.Add("No view was given");
}
else if (view.IsTemplate)
{
    findings.Add(string.Format(
        "'{0}' is a view TEMPLATE, not a view. A template has no elements of its "
        + "own - it is a set of settings applied to views - so everything in the "
        + "model would come back as hidden, which would read as a catastrophe "
        + "rather than as the wrong question", view.Name));
}
else
{
    var bounded = categories != null && categories.Count > 0;

    ElementMulticategoryFilter filter = null;
    if (bounded)
    {
        var wanted = new List<BuiltInCategory>();
        foreach (var category in categories) wanted.Add(category);
        filter = new ElementMulticategoryFilter(wanted);
    }

    // WHAT THE VIEW SHOWS. Revit's own answer, view-scoped.
    var visible = new HashSet<ElementId>();
    var inView = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType();
    if (filter != null) inView = inView.WherePasses(filter);
    foreach (var element in inView)
    {
        if (element == null) continue;
        visible.Add(element.Id);
    }
    showing = visible.Count;

    // WHAT THE MODEL HOLDS.
    var whole = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (filter != null) whole = whole.WherePasses(filter);
    foreach (var element in whole)
    {
        if (element == null) continue;
        inModel++;
        if (!visible.Contains(element.Id)) elements.Add(element);
    }

    findings.Add(string.Format(
        "{0} in the model, {1} showing on '{2}', so {3} are not{4}",
        inModel, showing, view.Name, elements.Count,
        bounded
            ? string.Format(", within {0} category/categories", categories.Count)
            : ". THIS WAS UNBOUNDED, so it counts every view, sheet, material and "
              + "template in the document as hidden - all true, none useful. "
              + "Give a category list"));

    if (elements.Count > 0)
    {
        findings.Add("WHY each one is missing is a question for "
            + "DIAGNOSE_VISIBILITY, one element at a time - this says which, "
            + "never why");
    }
}
