// NOT STANDALONE. Assumes `doc`, `filterNames` and `categories` are in scope;
// leaves `changed`, `readBack`, `alreadySet`, `leftOut`, `notFound` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// SETS THE CATEGORY LIST OF FILTERS THAT ALREADY EXIST - the ticks in the
// Categories column of Manage, Filters - and nothing else. The rule is left as
// it is, the name is left as it is, and no view is touched: a filter that is on
// a view stays on it with its overrides, and one that is on none stays on none.
//
// THE LIST GIVEN IS THE WHOLE NEW LIST. A category the filter held and the list
// does not name is taken off; that is how "not Duct Systems" is said.
//
// A CATEGORY THE FILTER'S RULE CANNOT APPLY TO IS LEFT OUT, NAMED, AND THE REST
// ARE STILL SET. Unlike CREATE_VIEW_FILTERS_BY_RULE, which refuses the whole
// call: there, a category the modeller named is part of the filter being asked
// for; here, the modeller is correcting a list, and the owner's instruction was
// that one unusable category must not stop the others. Each category is asked
// twice - whether Revit lists every parameter the rule tests among what it may
// be filtered by, and then Revit's own acceptability test for the rule - and
// the survivors are tested together once more before anything is set.
//
// A FILTER IS NEVER EMPTIED. If no category survives, it is left exactly as it
// was and named in `refused`.
//
// SETTING A LIST THE FILTER ALREADY HOLDS IS NOT A CHANGE. It is compared as a
// set first, reported in `alreadySet`, and SetCategories is not called - so a
// second run changes nothing and leaves nothing in the undo list.
//
// A SELECTION FILTER HAS NO CATEGORIES - it holds chosen elements - and is
// named in `notFound` rather than being edited.
//
// THE READ-BACK IS WHAT REVIT HOLDS AFTERWARDS, for every filter named - the
// changed ones and the ones already so: categories and rule read out of the
// filter, and what that rule catches in the model today, per category.
//
// Everything that could be checked is checked before the first SetCategories;
// that call is not caught, so an unexpected failure ends the run and Heron
// rolls the whole call back rather than leaving some filters changed.

var changed = new List<Element>();
var readBack = "";
var alreadySet = new List<string>();
var leftOut = new List<string>();
var notFound = new List<string>();
var refused = "";

var names = new List<string>();
if (filterNames != null)
{
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var given in filterNames)
    {
        var name = (given ?? "").Trim();
        if (name.Length > 0 && seen.Add(name)) names.Add(name);
    }
}

// The categories, once each, with the name Revit shows for them.
var categoryIds = new List<ElementId>();
var categoryName = new Dictionary<ElementId, string>();
if (categories != null)
{
    foreach (var builtIn in categories)
    {
        var id = new ElementId(builtIn);
        if (categoryIds.Contains(id)) continue;
        categoryIds.Add(id);

        var label = builtIn.ToString();
        try
        {
            var category = Category.GetCategory(doc, builtIn);
            if (category != null) label = category.Name;
        }
        catch { }
        categoryName[id] = label;
    }
}

if (names.Count == 0)
{
    refused = "no filter name was given - name the filters whose categories should be set";
}
else if (categoryIds.Count == 0)
{
    refused = "no category was given - the list given replaces the filter's categories, and a "
        + "filter is never emptied";
}

// ---- the filters, by name --------------------------------------------------

var targets = new List<ParameterFilterElement>();

if (refused.Length == 0)
{
    var everyFilter = new List<Element>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
        everyFilter.Add(element);

    foreach (var name in names)
    {
        Element exact = null;
        var loose = new List<Element>();
        foreach (var element in everyFilter)
        {
            var held = element.Name ?? "";
            if (held == name) exact = element;
            else if (string.Equals(held, name, StringComparison.OrdinalIgnoreCase)) loose.Add(element);
        }

        var found = exact ?? (loose.Count == 1 ? loose[0] : null);
        if (found == null)
        {
            notFound.Add(loose.Count > 1
                ? string.Format("{0} - {1} filters differ from it only in capitals; type one exactly",
                    name, loose.Count)
                : string.Format("{0} - no filter of that name in this project", name));
            continue;
        }

        var ruleBased = found as ParameterFilterElement;
        if (ruleBased == null)
        {
            notFound.Add(string.Format("{0} - a selection filter: it holds chosen elements, not "
                + "categories", found.Name));
            continue;
        }

        if (!targets.Contains(ruleBased)) targets.Add(ruleBased);
    }

    if (targets.Count == 0)
    {
        refused = "none of the names is a rule-based filter in this project - nothing was changed";
    }
}

// ---- names for the parameters the rules test --------------------------------

var parameterLabel = new Dictionary<ElementId, string>();

if (targets.Count > 0)
{
    var unnamed = new HashSet<ElementId>();
    foreach (var filter in targets)
    {
        var tested = filter.GetElementFilterParameters();
        if (tested == null) continue;
        foreach (var parameterId in tested)
        {
            if (parameterId == null || parameterLabel.ContainsKey(parameterId)) continue;
            string label = null;
            try
            {
                var definition = doc.GetElement(parameterId) as ParameterElement;
                if (definition != null && definition.GetDefinition() != null)
                    label = definition.GetDefinition().Name;
            }
            catch { }
            if (label != null) parameterLabel[parameterId] = label;
            else unnamed.Add(parameterId);
        }
    }

    // Built-in ones: the enumeration walked once, each member turned INTO an
    // id - never an id read as a number.
    if (unnamed.Count > 0)
    {
        foreach (BuiltInParameter builtIn in Enum.GetValues(typeof(BuiltInParameter)))
        {
            if (unnamed.Count == 0) break;
            ElementId candidate = null;
            try { candidate = new ElementId(builtIn); } catch { continue; }
            if (candidate == null || !unnamed.Contains(candidate)) continue;

            var label = builtIn.ToString();
            try { label = LabelUtils.GetLabelFor(builtIn); } catch { }
            parameterLabel[candidate] = label;
            unnamed.Remove(candidate);
        }
        foreach (var parameterId in unnamed) parameterLabel[parameterId] = parameterId.ToString();
    }
}

// ---- what each filter will hold, worked out before anything is set --------

var plan = new Dictionary<ParameterFilterElement, List<ElementId>>();
var refusals = new List<string>();
// "category - reason" -> the filters it was left out of, so one reason shared by
// every filter is said once.
var leftOutOf = new Dictionary<string, List<string>>();
var leftOutOrder = new List<string>();

if (targets.Count > 0)
{
    ICollection<ElementId> filterable = null;
    try { filterable = ParameterFilterUtilities.GetAllFilterableCategories(); } catch { }

    var allowedBy = new Dictionary<ElementId, ICollection<ElementId>>();
    var allowedProblem = new Dictionary<ElementId, string>();
    foreach (var categoryId in categoryIds)
    {
        if (filterable != null && !filterable.Contains(categoryId))
        {
            allowedProblem[categoryId] = "Revit cannot filter this category at all";
            continue;
        }
        try
        {
            var single = new List<ElementId>();
            single.Add(categoryId);
            var allowed = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, single);
            allowedBy[categoryId] = allowed ?? new List<ElementId>();
        }
        catch (Exception ex)
        {
            allowedProblem[categoryId] = "Revit would not list what it can be filtered by: " + ex.Message;
        }
    }

    foreach (var filter in targets)
    {
        var tested = filter.GetElementFilterParameters();
        var rule = filter.GetElementFilter();
        var keep = new List<ElementId>();
        var reasons = new List<KeyValuePair<ElementId, string>>();

        foreach (var categoryId in categoryIds)
        {
            if (allowedProblem.ContainsKey(categoryId))
            {
                reasons.Add(new KeyValuePair<ElementId, string>(categoryId, allowedProblem[categoryId]));
                continue;
            }

            var missing = new List<string>();
            if (tested != null)
            {
                foreach (var parameterId in tested)
                {
                    if (parameterId == null || allowedBy[categoryId].Contains(parameterId)) continue;
                    missing.Add("'" + (parameterLabel.ContainsKey(parameterId)
                        ? parameterLabel[parameterId] : parameterId.ToString()) + "'");
                }
            }

            if (missing.Count > 0)
            {
                reasons.Add(new KeyValuePair<ElementId, string>(categoryId,
                    "it cannot be filtered by " + string.Join(" or ", missing)));
                continue;
            }

            if (rule != null)
            {
                var alone = new HashSet<ElementId>();
                alone.Add(categoryId);
                var acceptable = false;
                var why = "";
                try
                {
                    acceptable = ParameterFilterElement.ElementFilterIsAcceptableForParameterFilterElement(
                        doc, alone, rule);
                }
                catch (Exception ex) { why = ": " + ex.Message; }

                if (!acceptable)
                {
                    reasons.Add(new KeyValuePair<ElementId, string>(categoryId,
                        "Revit rejects this filter's rule for it" + why));
                    continue;
                }
            }

            keep.Add(categoryId);
        }

        foreach (var reason in reasons)
        {
            var key = string.Format("{0} - {1}", categoryName[reason.Key], reason.Value);
            if (!leftOutOf.ContainsKey(key))
            {
                leftOutOf[key] = new List<string>();
                leftOutOrder.Add(key);
            }
            leftOutOf[key].Add(filter.Name);
        }

        if (keep.Count == 0)
        {
            refusals.Add(string.Format("'{0}' left exactly as it was - none of the categories given "
                + "can take its rule, and a filter is never emptied", filter.Name));
            continue;
        }

        if (rule != null)
        {
            var together = false;
            var why = "";
            try
            {
                together = ParameterFilterElement.ElementFilterIsAcceptableForParameterFilterElement(
                    doc, new HashSet<ElementId>(keep), rule);
            }
            catch (Exception ex) { why = ": " + ex.Message; }

            if (!together)
            {
                refusals.Add(string.Format("'{0}' left exactly as it was - Revit accepts its rule for "
                    + "each category alone but not for all of them together{1}", filter.Name, why));
                continue;
            }
        }

        plan[filter] = keep;
    }

    foreach (var key in leftOutOrder)
    {
        leftOut.Add(leftOutOf[key].Count == targets.Count
            ? key
            : string.Format("{0} (for {1})", key, string.Join(", ", leftOutOf[key])));
    }
}

// ---- set, then read every named filter back out of Revit ------------------

if (targets.Count > 0)
{
    var lines = new List<string>();

    foreach (var filter in targets)
    {
        var state = "left as it was";
        if (plan.ContainsKey(filter))
        {
            var current = new HashSet<ElementId>(filter.GetCategories());
            if (current.SetEquals(plan[filter]))
            {
                alreadySet.Add(filter.Name);
                state = "already so, not changed";
            }
            else
            {
                // Not caught, on purpose - see the header.
                filter.SetCategories(plan[filter]);
                changed.Add(filter);
                state = "changed";
            }
        }

        var heldCategories = new List<string>();
        var heldIds = new List<ElementId>(filter.GetCategories());
        foreach (var categoryId in heldIds)
        {
            var label = categoryId.ToString();
            try
            {
                var category = Category.GetCategory(doc, categoryId);
                if (category != null) label = category.Name;
            }
            catch { }
            heldCategories.Add(label);
        }
        heldCategories.Sort(StringComparer.OrdinalIgnoreCase);

        // The rules, walked without recursion - a logical filter holds others.
        var heldRules = new List<string>();
        var held = filter.GetElementFilter();
        var pending = new List<ElementFilter>();
        if (held != null) pending.Add(held);
        while (pending.Count > 0)
        {
            var next = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);

            var logical = next as ElementLogicalFilter;
            if (logical != null)
            {
                foreach (var inner in logical.GetFilters()) if (inner != null) pending.Add(inner);
                continue;
            }

            var byParameter = next as ElementParameterFilter;
            if (byParameter == null)
            {
                heldRules.Add(next.GetType().Name);
                continue;
            }

            foreach (var heldRule in byParameter.GetRules())
            {
                var parameterText = "?";
                try
                {
                    var parameterId = heldRule.GetRuleParameter();
                    parameterText = parameterLabel.ContainsKey(parameterId)
                        ? parameterLabel[parameterId]
                        : parameterId.ToString();
                }
                catch { }

                var textRule = heldRule as FilterStringRule;
                if (textRule != null)
                {
                    var evaluator = textRule.GetEvaluator();
                    var verb = evaluator == null ? "?" : evaluator.GetType().Name;
                    if (verb == "FilterStringEquals") verb = "equals";
                    heldRules.Add(string.Format("{0} {1} '{2}'", parameterText, verb, textRule.RuleString));
                }
                else
                {
                    heldRules.Add(string.Format("{0} ({1})", parameterText, heldRule.GetType().Name));
                }
            }
        }

        // What the rule, as Revit holds it, catches in the model today.
        var perCategory = new List<string>();
        var total = 0;
        if (held != null)
        {
            foreach (var categoryId in heldIds)
            {
                var single = new List<ElementId>();
                single.Add(categoryId);
                var count = new FilteredElementCollector(doc)
                    .WherePasses(new ElementMulticategoryFilter(single))
                    .WhereElementIsNotElementType()
                    .WherePasses(held)
                    .GetElementCount();
                total += count;
                if (count > 0)
                {
                    var label = categoryId.ToString();
                    try
                    {
                        var category = Category.GetCategory(doc, categoryId);
                        if (category != null) label = category.Name;
                    }
                    catch { }
                    perCategory.Add(string.Format("{0} {1}", label, count));
                }
            }
        }

        lines.Add(string.Format("'{0}' ({1}): categories {2}; rule {3}; catches {4} element(s) in "
            + "the model today{5}", filter.Name, state, string.Join(", ", heldCategories),
            heldRules.Count == 0 ? "NONE" : string.Join(" AND ", heldRules), total,
            perCategory.Count == 0 ? "" : " (" + string.Join(", ", perCategory) + ")"));
    }

    readBack = string.Join(" | ", lines);

    var said = new List<string>(refusals);
    if (leftOut.Count > 0)
    {
        said.Add(string.Format("left out and NOT set: {0}", string.Join("; ", leftOut)));
    }
    if (notFound.Count > 0)
    {
        said.Add(string.Format("not a rule-based filter here: {0}", string.Join("; ", notFound)));
    }
    refused = string.Join(". ", said);
}
