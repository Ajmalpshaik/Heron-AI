// NOT STANDALONE. Assumes `doc`, `filterName` and `elements` are in scope;
// leaves `matched`, `outOfScope` and `findings` behind.
//
// THE OUT-OF-SCOPE VERDICT IS THE WHOLE POINT. A rule-based view filter only
// applies to the categories it was scoped to, so an element outside that list
// can satisfy the rule perfectly and never be caught. A plain pass/fail test
// calls those a failure and sends somebody off rewriting a rule that was
// already right.
//
// A SELECTION FILTER HAS NO RULE. It is a hand-picked list, so membership is
// the only question and there is nothing to diagnose.
//
// NOTHING IS APPLIED AND NO VIEW IS TOUCHED.

var matched = new List<Element>();
var outOfScope = 0;
var findings = new List<string>();

var wanted = string.IsNullOrEmpty(filterName) ? "" : filterName.Trim();

if (wanted.Length == 0)
{
    findings.Add("No filter was named - name the filter to test against");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given to test");
}
else
{
    FilterElement target = null;
    var available = new List<string>();

    foreach (var candidate in new FilteredElementCollector(doc)
        .OfClass(typeof(FilterElement)).Cast<FilterElement>())
    {
        available.Add(candidate.Name);
        if (target == null && string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            target = candidate;
    }

    if (target == null)
    {
        findings.Add(string.Format("No filter is called '{0}'. This model has: {1}",
            wanted, available.Count == 0 ? "none" : string.Join(", ", available.ToArray())));
    }
    else
    {
        var saved = target as SelectionFilterElement;
        var rule = target as ParameterFilterElement;

        if (saved != null)
        {
            var members = saved.GetElementIds();

            foreach (var element in elements)
            {
                if (element == null) continue;
                var inSet = false;
                if (members != null)
                {
                    foreach (var id in members)
                    {
                        if (id == element.Id) { inSet = true; break; }
                    }
                }
                if (inSet) matched.Add(element);
            }

            findings.Add(string.Format("'{0}' is a SELECTION filter - a hand-picked list of {1}, with "
                + "no rule. {2} of {3} tested element(s) are in it, and there is nothing to diagnose: "
                + "an element is in the list or it is not",
                target.Name, members == null ? 0 : members.Count, matched.Count, elements.Count));
        }
        else if (rule != null)
        {
            var categories = rule.GetCategories();
            var inner = rule.GetElementFilter();
            var failedRule = 0;

            foreach (var element in elements)
            {
                if (element == null) continue;

                var categoryId = element.Category == null ? ElementId.InvalidElementId : element.Category.Id;

                var inScope = false;
                if (categories != null)
                {
                    foreach (var allowed in categories)
                    {
                        if (allowed == categoryId) { inScope = true; break; }
                    }
                }

                if (!inScope)
                {
                    outOfScope++;
                    continue;
                }

                var passes = false;
                try { passes = inner == null || inner.PassesFilter(doc, element.Id); }
                catch { passes = false; }

                if (passes) matched.Add(element);
                else failedRule++;
            }

            findings.Add(string.Format("'{0}' is a RULE filter scoped to {1} category/categories. Of {2} "
                + "tested: {3} MATCH, {4} fail the rule, and {5} are OUT OF SCOPE",
                target.Name,
                categories == null ? 0 : categories.Count,
                elements.Count, matched.Count, failedRule, outOfScope));

            if (outOfScope > 0)
                findings.Add(string.Format("Those {0} can NEVER match, however well they satisfy the "
                    + "rule - the filter was never scoped to their category. Add the category to the "
                    + "filter; do not rewrite the rule", outOfScope));
        }
        else
        {
            findings.Add(string.Format("'{0}' is a filter of a kind this fragment cannot test ({1}). "
                + "Nothing was reported rather than something guessed",
                target.Name, target.GetType().Name));
        }
    }
}
