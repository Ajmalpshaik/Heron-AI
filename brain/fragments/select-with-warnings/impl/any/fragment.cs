// NOT STANDALONE. Assumes `doc`, `errorsOnly` and `descriptionContains` are in
// scope; leaves `elements`, `unresolved` and `findings` behind.
//
// ONE ELEMENT IS USUALLY NAMED BY SEVERAL WARNINGS, AND IT BELONGS IN THE
// ANSWER ONCE. A wall in three overlap warnings is one wall to select. Adding
// up the failing elements of every warning gives a list with duplicates in it,
// and every count taken from that list is then wrong.
//
// A WARNING CAN NAME AN ELEMENT THAT IS NO LONGER THERE. Revit's list is not
// always ahead of the model, so an id that does not resolve is COUNTED as
// unresolved rather than dropped - a large figure there means the warning list
// is stale, which is worth knowing before acting on it.
//
// THIS IS REVIT'S WARNING LIST, NOT A QUALITY AUDIT. Clashes, wrong sizes,
// missing tags and naming breaches are not warnings and none of them appear
// here.

var elements = new List<Element>();
var unresolved = 0;
var findings = new List<string>();

var phrase = string.IsNullOrEmpty(descriptionContains) ? "" : descriptionContains.Trim();

var considered = 0;
var seen = new List<ElementId>();

foreach (var warning in doc.GetWarnings())
{
    if (errorsOnly && warning.GetSeverity() != FailureSeverity.Error) continue;

    if (phrase.Length > 0)
    {
        var text = warning.GetDescriptionText() ?? "";
        if (text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) < 0) continue;
    }

    considered++;

    foreach (var id in warning.GetFailingElements())
    {
        var already = false;
        foreach (var known in seen)
        {
            if (known == id) { already = true; break; }
        }
        if (already) continue;
        seen.Add(id);

        var element = doc.GetElement(id);
        if (element == null) unresolved++;
        else elements.Add(element);
    }
}

findings.Add(string.Format("{0} element(s) named by {1} of the model's warnings{2}{3}. Each element "
    + "appears once however many warnings name it",
    elements.Count,
    considered,
    errorsOnly ? ", error severity only" : "",
    phrase.Length == 0 ? "" : string.Format(", description containing '{0}'", phrase)));

if (unresolved > 0)
    findings.Add(string.Format("{0} id(s) in those warnings no longer resolve to an element. Revit's "
        + "warning list is behind the model - it is worth reopening the warning dialog before acting "
        + "on this set", unresolved));

if (considered == 0)
    findings.Add(phrase.Length == 0 && !errorsOnly
        ? "This model has no warnings at all"
        : "No warning matched that filter - the model may still have others");

findings.Add("These are Revit's own warnings. A clash, a wrong size, a missing tag or a naming "
    + "breach is not one of them and will never appear here");
