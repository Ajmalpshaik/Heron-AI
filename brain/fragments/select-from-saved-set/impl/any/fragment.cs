// NOT STANDALONE. Assumes `doc`, `filterName` and `view` are in scope; leaves
// `elements`, `missing` and `findings` behind.
//
// ONE WORD, TWO THINGS. A SelectionFilterElement holds an explicit LIST of ids -
// reading it hands back what was saved, and the answer is the same everywhere.
// A ParameterFilterElement holds a RULE and no list at all, so "its elements"
// can only mean what that rule matches IN ONE VIEW.
//
// SO A VIEW FILTER WITH NO VIEW IS REFUSED, NOT GUESSED. Falling back to the
// active view answers a question about whichever drawing happened to be open,
// and would be right often enough that nobody would notice when it was not.
//
// A SAVED LIST CAN NAME ELEMENTS THAT ARE GONE, because deleting one does not
// tidy the sets that mentioned it. Those are COUNTED, which is the difference
// between "the set has shrunk since you saved it" and "the set was always
// this size".

var elements = new List<Element>();
var missing = 0;
var findings = new List<string>();

var wanted = string.IsNullOrEmpty(filterName) ? "" : filterName.Trim();

if (wanted.Length == 0)
{
    findings.Add("No filter name was given - name the saved filter to read");
}
else
{
    FilterElement target = null;
    var available = new List<string>();

    foreach (var candidate in new FilteredElementCollector(doc)
        .OfClass(typeof(FilterElement)).Cast<FilterElement>())
    {
        available.Add(string.Format("'{0}' ({1})", candidate.Name,
            candidate is SelectionFilterElement ? "saved list" : "rule"));

        if (target == null && string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            target = candidate;
    }

    if (target == null)
    {
        findings.Add(string.Format("No filter is called '{0}'. This model has: {1}",
            wanted,
            available.Count == 0 ? "none" : string.Join(", ", available.ToArray())));
    }
    else
    {
        var saved = target as SelectionFilterElement;
        var rule = target as ParameterFilterElement;

        if (saved != null)
        {
            var ids = saved.GetElementIds();
            var held = ids == null ? 0 : ids.Count;

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    var element = doc.GetElement(id);
                    if (element == null) missing++;
                    else elements.Add(element);
                }
            }

            findings.Add(string.Format("'{0}' is a SAVED LIST holding {1} id(s); {2} still exist. "
                + "The answer is the same in every view",
                target.Name, held, elements.Count));

            if (missing > 0)
                findings.Add(string.Format("{0} of them have been deleted since the set was saved. "
                    + "Revit does not tidy a saved set when an element goes", missing));
        }
        else if (rule != null)
        {
            if (view == null)
            {
                findings.Add(string.Format("'{0}' is a RULE, not a saved list, so it has no elements "
                    + "of its own - only the ones it matches in a given view. Name the view. Falling "
                    + "back to whatever is on screen would answer a different question and look "
                    + "identical", target.Name));
            }
            else
            {
                var categories = rule.GetCategories();
                var collector = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType();

                if (categories != null && categories.Count > 0)
                    collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

                var inner = rule.GetElementFilter();
                if (inner != null) collector = collector.WherePasses(inner);

                foreach (var element in collector) elements.Add(element);

                findings.Add(string.Format("'{0}' is a RULE. It matches {1} element(s) in view '{2}' - "
                    + "re-evaluated now, not read from a list, so the same rule in another view is a "
                    + "different answer",
                    target.Name, elements.Count, view.Name));
            }
        }
        else
        {
            findings.Add(string.Format("'{0}' is a filter of a kind this fragment does not read ({1}). "
                + "Nothing was returned rather than something guessed",
                target.Name, target.GetType().Name));
        }
    }
}
