// NOT STANDALONE. Assumes `elements` and `parameterName` are in scope.
//
// THREE OUTCOMES, NOT TWO, and this is the whole point of the fragment.
//
//   values   the parameter is there and has something in it
//   blank    the parameter is there and is empty
//   absent   the element does not have that parameter at all
//
// Collapsing blank and absent into "no value" is the common shortcut and it
// hides the difference that matters: a blank parameter is a modelling job, an
// absent one is a family or a category problem, and they go to different people.
// A report that says "12 have no system name" when 9 of them cannot have one is
// a report that sends somebody to fix the wrong thing.

var values = new Dictionary<ElementId, string>();
var blank = new List<ElementId>();
var absent = new List<ElementId>();

foreach (var e in elements)
{
    var p = e.LookupParameter(parameterName);
    if (p == null)
    {
        absent.Add(e.Id);
        continue;
    }

    // AsValueString() first: it gives the number as the user sees it, with the
    // project's own units, which is what a modeller checks against. AsString()
    // is the fallback for text parameters, where AsValueString returns null.
    var text = p.AsValueString() ?? p.AsString();
    if (string.IsNullOrWhiteSpace(text)) blank.Add(e.Id);
    else values[e.Id] = text;
}
