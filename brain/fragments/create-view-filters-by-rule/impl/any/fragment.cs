// NOT STANDALONE. Assumes `doc`, `filterNames`, `categories`, `parameterName`
// and `matchValues` are in scope; leaves `created`, `readBack`, `alreadyExists`,
// `cannotFilter` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE FILTERS ARE CREATED AND PUT ON NO VIEW. They land in the project's
// Filters dialog, which is where the modeller asked for them. Putting one on a
// view is APPLY_VIEW_FILTER's job, and a colour chosen here would be a colour
// nobody asked for.
//
// THE PARAMETER IS NAMED, NOT PICKED OFF AN ELEMENT. "System Classification"
// is looked for among the parameters Revit says each category may be FILTERED
// by - ParameterFilterUtilities, which needs no element to exist. So a filter
// can be made before a single duct is drawn, and a category nothing has been
// modelled in yet is still checked.
//
// ONE CATEGORY THAT CANNOT TAKE THE RULE STOPS THE WHOLE CALL. Revit rejects a
// filter whose category list holds one category the parameter does not apply
// to, and rejects it as a whole. So every category is asked first, each one
// that cannot be filtered by the parameter is named, and NOTHING is created.
// Taking the category out quietly would make a filter the modeller did not ask
// for.
//
// THE NAME HAS TO MEAN ONE PARAMETER. A shared or project parameter can carry
// the same name as a built-in one, and a rule is built on ONE parameter's id.
// Two candidates are both named and nothing is created; the built-in one can
// then be typed by its Revit name, RBS_SYSTEM_CLASSIFICATION_PARAM.
//
// THE RULE FACTORY IS REACHED BY REFLECTION, for the reason
// CREATE_VIEW_FILTERS_BY_VALUE gives. CreateEqualsRule for TEXT is
// (ElementId, string, bool) on 2020 to 2025 and (ElementId, string) on 2023 to
// 2027 - read off the reference assemblies for each release. Each spelling
// compiles on one end of the range and breaks the other, so the overload is
// picked at run time. The two-argument one is preferred where it exists; the
// three-argument one is passed `false`, not case sensitive, the same as
// CREATE_VIEW_FILTERS_BY_VALUE.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS CREATED - names, the categories,
// the parameter, and each rule against Revit's own acceptability test. The
// creating loop then catches nothing: an exception there ends the run, and
// Heron rolls the whole call back, so a half-made set of filters is never
// left behind. A name that is ALREADY a filter is the one planned skip - named,
// never overwritten.
//
// THE READ-BACK IS WHAT REVIT HOLDS, NOT AN ECHO OF THE REQUEST. Each filter's
// categories and rules are read out of the filter after it exists, and the
// elements that rule catches in the model today are counted, per category.

var created = new List<Element>();
var readBack = "";
var alreadyExists = new List<string>();
var cannotFilter = new List<string>();
var refused = "";

var names = new List<string>();
if (filterNames != null)
    foreach (var given in filterNames) names.Add((given ?? "").Trim());

var values = new List<string>();
if (matchValues != null)
    foreach (var given in matchValues) values.Add((given ?? "").Trim());

var wanted = (parameterName ?? "").Trim();

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

// ---- what was asked for, checked before Revit is asked anything -----------

var repeated = "";
var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var name in names)
{
    if (name.Length > 0 && !seen.Add(name)) { repeated = name; break; }
}

var badName = "";
foreach (var name in names)
{
    if (name.Length > 0 && !NamingUtils.IsValidName(name)) { badName = name; break; }
}

if (names.Count == 0)
{
    refused = "no filter name was given - each filter needs one, in the order of its value";
}
else if (names.Count != values.Count)
{
    refused = string.Format("{0} filter name(s) and {1} value(s) were given - each filter needs "
        + "exactly one value, in the same order as the names", names.Count, values.Count);
}
else if (names.Contains("") || values.Contains(""))
{
    refused = "a filter name or a value was blank - every filter needs both";
}
else if (repeated.Length > 0)
{
    refused = string.Format("'{0}' was asked for twice - a filter name can be used once in a "
        + "project", repeated);
}
else if (badName.Length > 0)
{
    refused = string.Format("'{0}' cannot be a filter name - Revit does not allow characters "
        + "such as : ; [ ] {{ }} | < > ? ` ~ in a name", badName);
}
else if (categoryIds.Count == 0)
{
    refused = "no category was given - a filter needs at least one to look at";
}
else if (wanted.Length == 0)
{
    refused = "no parameter was named - a filter with no rule catches the whole category, which "
        + "is SET_CATEGORY_GRAPHICS' job";
}

// ---- each category, asked on its own --------------------------------------

var allowedBy = new Dictionary<ElementId, ICollection<ElementId>>();
var parameterLabel = new Dictionary<ElementId, string>();
var builtInName = new Dictionary<ElementId, string>();

if (refused.Length == 0)
{
    ICollection<ElementId> filterable = null;
    try { filterable = ParameterFilterUtilities.GetAllFilterableCategories(); } catch { }

    var everyCandidate = new HashSet<ElementId>();

    foreach (var categoryId in categoryIds)
    {
        if (filterable != null && !filterable.Contains(categoryId))
        {
            cannotFilter.Add(string.Format("{0} - Revit cannot filter this category at all",
                categoryName[categoryId]));
            continue;
        }

        ICollection<ElementId> allowed = null;
        try
        {
            var single = new List<ElementId>();
            single.Add(categoryId);
            allowed = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, single);
        }
        catch (Exception ex)
        {
            cannotFilter.Add(string.Format("{0} - Revit would not list what it can be filtered by: "
                + "{1}", categoryName[categoryId], ex.Message));
            continue;
        }

        if (allowed == null) allowed = new List<ElementId>();
        allowedBy[categoryId] = allowed;
        foreach (var parameterId in allowed)
            if (parameterId != null) everyCandidate.Add(parameterId);
    }

    // Names for every candidate. A project or shared parameter is an element in
    // this document and names itself. A built-in one is not, so the enumeration
    // is walked once and each member turned INTO an id - the direction that
    // never reads an id as a number, which changed width in 2024.
    var unnamed = new HashSet<ElementId>();
    foreach (var parameterId in everyCandidate)
    {
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
            builtInName[candidate] = builtIn.ToString();
            unnamed.Remove(candidate);
        }
    }

    // A category is fine when at least one parameter it may be filtered by
    // answers to the name - by the label Revit shows, or by its built-in name.
    foreach (var categoryId in categoryIds)
    {
        if (!allowedBy.ContainsKey(categoryId)) continue;

        var answers = false;
        foreach (var parameterId in allowedBy[categoryId])
        {
            if (parameterId == null) continue;
            if ((parameterLabel.ContainsKey(parameterId)
                    && string.Equals(parameterLabel[parameterId], wanted, StringComparison.OrdinalIgnoreCase))
                || (builtInName.ContainsKey(parameterId)
                    && string.Equals(builtInName[parameterId], wanted, StringComparison.OrdinalIgnoreCase)))
            {
                answers = true;
                break;
            }
        }

        if (!answers)
        {
            cannotFilter.Add(string.Format("{0} - it cannot be filtered by '{1}'",
                categoryName[categoryId], wanted));
        }
    }

    if (cannotFilter.Count > 0)
    {
        refused = string.Format("NOTHING WAS CREATED. {0} of the {1} categories cannot take a rule on "
            + "'{2}': {3}. Revit rejects a filter whose categories include one the parameter does not "
            + "apply to, so take {4} out of the list and ask again",
            cannotFilter.Count, categoryIds.Count, wanted, string.Join("; ", cannotFilter),
            cannotFilter.Count == 1 ? "it" : "them");
    }
}

// ---- one parameter, common to every category ------------------------------

ElementId ruleParameter = ElementId.InvalidElementId;

if (refused.Length == 0)
{
    ICollection<ElementId> common = null;
    try { common = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, categoryIds); }
    catch (Exception ex)
    {
        refused = "Revit would not list the parameters these categories share: " + ex.Message;
    }

    if (common != null)
    {
        var matching = new List<ElementId>();
        foreach (var parameterId in common)
        {
            if (parameterId == null) continue;
            if ((parameterLabel.ContainsKey(parameterId)
                    && string.Equals(parameterLabel[parameterId], wanted, StringComparison.OrdinalIgnoreCase))
                || (builtInName.ContainsKey(parameterId)
                    && string.Equals(builtInName[parameterId], wanted, StringComparison.OrdinalIgnoreCase)))
            {
                matching.Add(parameterId);
            }
        }

        if (matching.Count == 1)
        {
            ruleParameter = matching[0];
        }
        else if (matching.Count == 0)
        {
            refused = string.Format("NOTHING WAS CREATED. Every category can be filtered by a "
                + "parameter called '{0}', but not by the SAME one - a filter's rule is built on one "
                + "parameter, so these categories cannot share a filter on it", wanted);
        }
        else
        {
            var described = new List<string>();
            foreach (var parameterId in matching)
            {
                described.Add(builtInName.ContainsKey(parameterId)
                    ? string.Format("'{0}' (built in, {1})", parameterLabel[parameterId],
                        builtInName[parameterId])
                    : string.Format("'{0}' (a project or shared parameter)", parameterLabel[parameterId]));
            }
            refused = string.Format("NOTHING WAS CREATED. '{0}' names {1} different parameters on these "
                + "categories: {2}. A rule is built on one of them - type the built-in one by its Revit "
                + "name to choose it", wanted, matching.Count, string.Join(", ", described));
        }
    }
}

// ---- the rules, built and tested before any filter exists -----------------

var toCreate = new List<int>();
var tests = new Dictionary<int, ElementFilter>();

if (refused.Length == 0)
{
    System.Reflection.MethodInfo withoutFlag = null;
    System.Reflection.MethodInfo withFlag = null;
    foreach (var candidate in typeof(ParameterFilterRuleFactory).GetMethods())
    {
        if (candidate.Name != "CreateEqualsRule") continue;
        var arguments = candidate.GetParameters();
        if (arguments.Length < 2 || arguments[0].ParameterType != typeof(ElementId)
            || arguments[1].ParameterType != typeof(string)) continue;
        if (arguments.Length == 2) withoutFlag = candidate;
        else if (arguments.Length == 3 && arguments[2].ParameterType == typeof(bool)) withFlag = candidate;
    }

    // Filter names already in the project - FilterElement, so a SELECTION
    // filter of the same name is seen too; both live in one list.
    var existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
    {
        var held = element.Name ?? "";
        if (!existing.ContainsKey(held)) existing[held] = held;
    }

    var categorySet = new HashSet<ElementId>(categoryIds);

    if (withoutFlag == null && withFlag == null)
    {
        refused = "this Revit version exposes no text rule this recognises - no filter was created";
    }
    else
    {
        for (var index = 0; index < names.Count && refused.Length == 0; index++)
        {
            var name = names[index];
            if (existing.ContainsKey(name))
            {
                alreadyExists.Add(existing[name] == name
                    ? name
                    : string.Format("{0} (held as '{1}')", name, existing[name]));
                continue;
            }

            FilterRule rule = null;
            try
            {
                rule = withoutFlag != null
                    ? withoutFlag.Invoke(null, new object[] { ruleParameter, values[index] }) as FilterRule
                    : withFlag.Invoke(null, new object[] { ruleParameter, values[index], false }) as FilterRule;
            }
            catch (Exception ex)
            {
                var cause = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                refused = string.Format("NOTHING WAS CREATED. Revit would not build the rule '{0}' "
                    + "equals '{1}': {2}", wanted, values[index], cause);
                break;
            }

            if (rule == null)
            {
                refused = string.Format("NOTHING WAS CREATED. Revit built no rule for '{0}' equals "
                    + "'{1}'", wanted, values[index]);
                break;
            }

            var test = new ElementParameterFilter(rule);

            var acceptable = false;
            var why = "";
            try
            {
                acceptable = ParameterFilterElement.ElementFilterIsAcceptableForParameterFilterElement(
                    doc, categorySet, test);
            }
            catch (Exception ex) { why = " - " + ex.Message; }

            if (!acceptable)
            {
                refused = string.Format("NOTHING WAS CREATED. Revit says the rule '{0}' equals '{1}' "
                    + "cannot be used on these categories{2}. A text rule on a parameter that holds a "
                    + "number is the usual reason", wanted, values[index], why);
                break;
            }

            toCreate.Add(index);
            tests[index] = test;
        }
    }
}

// ---- create, then read each one back out of Revit --------------------------

if (refused.Length == 0)
{
    var lines = new List<string>();

    foreach (var index in toCreate)
    {
        // Not caught, on purpose - see the header. Everything that could be
        // checked has been; a failure here rolls the whole call back.
        var filter = ParameterFilterElement.Create(doc, names[index], categoryIds, tests[index]);
        created.Add(filter);

        var heldCategories = new List<string>();
        foreach (var categoryId in filter.GetCategories())
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
            foreach (var categoryId in categoryIds)
            {
                var single = new List<ElementId>();
                single.Add(categoryId);
                var count = new FilteredElementCollector(doc)
                    .WherePasses(new ElementMulticategoryFilter(single))
                    .WhereElementIsNotElementType()
                    .WherePasses(held)
                    .GetElementCount();
                total += count;
                if (count > 0) perCategory.Add(string.Format("{0} {1}", categoryName[categoryId], count));
            }
        }

        lines.Add(string.Format("'{0}': categories {1}; rule {2}; catches {3} element(s) in the model "
            + "today{4}", filter.Name, string.Join(", ", heldCategories),
            heldRules.Count == 0 ? "NONE" : string.Join(" AND ", heldRules), total,
            perCategory.Count == 0 ? "" : " (" + string.Join(", ", perCategory) + ")"));
    }

    if (lines.Count > 0)
    {
        readBack = string.Join(" | ", lines)
            + " | none of them is on a view yet - APPLY_VIEW_FILTER puts one on a view";
    }

    if (alreadyExists.Count > 0)
    {
        refused = string.Format("{0} name(s) already a filter in this project and left exactly as "
            + "they were, not overwritten: {1}", alreadyExists.Count, string.Join(", ", alreadyExists));
    }
}
