// NOT STANDALONE. Assumes `doc`, `filterNames`, `categories`, `newName`,
// `parameterName` and `matchValue` are in scope; leaves `changed`, `readBack`,
// `alreadySet`, `leftOut`, `notFound` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// EDITS FILTERS THAT ALREADY EXIST - their categories, their name and their
// rule, any of the three, in one call - and never makes one. No view is
// touched: a filter that is on a view stays on it with its overrides, under
// its new name, and one that is on none stays on none.
//
// A BLANK KEEPS WHAT THE FILTER HAS (version 2, 2026-10-15). `categories`
// blank keeps its categories, `newName` blank keeps its name, `parameterName`
// and `matchValue` blank keep its rule. Version 1 took categories only and
// every older call still passes them, so it means exactly what it meant.
//
// THE CATEGORY LIST GIVEN IS THE WHOLE NEW LIST. A category the filter held
// and the list does not name is taken off; that is how "not Duct Systems" is
// said, and "add Grids" is said by giving the ones it has AND Grids.
//
// THE CATEGORIES ARE READ HERE, NOT BY THE ADD-IN, since version 2. The
// add-in's binder refuses a blank category list before a fragment runs, so a
// list that may be blank is typed as text and turned into categories here, in
// the binder's own order: the API name as typed (OST_DuctCurves), then the
// name Revit shows (Ducts, Railings - 5b-444), then the API name without its
// prefix (DuctCurves). One that is none of those stops the call, named, as
// the binder stopped it.
//
// A CATEGORY THE RULE CANNOT APPLY TO IS LEFT OUT, NAMED, AND THE REST ARE
// STILL SET - when categories are given. Unlike CREATE_VIEW_FILTERS_BY_RULE,
// which refuses the whole call: there, a category the modeller named is part
// of the filter being asked for; here, the modeller is correcting a list, and
// the owner's instruction was that one unusable category must not stop the
// others. Each category is asked twice - whether Revit lists every parameter
// the rule tests among what it may be filtered by, and then Revit's own
// acceptability test for the rule - and the survivors are tested together once
// more before anything is set.
//
// A NEW RULE WITH THE CATEGORIES KEPT MUST FIT EVERY ONE OF THEM. Leaving one
// out would take off a category nobody mentioned, so the filter is refused
// instead, naming the ones that cannot take it.
//
// A NEW NAME OR A NEW RULE IS ALL OR NOTHING. If any filter named cannot take
// what was asked, NOTHING is changed - not its categories either. A name goes
// on one filter only, and one already used by another filter is refused by
// name; a name that differs from the filter's own only in capitals is a
// rename.
//
// THE RULE IS WRITTEN THE WAY CREATE_VIEW_FILTERS_BY_RULE VERSION 2 TAKES IT -
// the operator in front of the value, in Revit's Filters dialog words: "does
// not contain TRG_MEC", "begins with M_", "equals Level 1"; none means equals.
// `parameterName` blank keeps the parameter the filter's rule already tests,
// which is how "change the rule to does not contain TRG_MEC" is said. A filter
// whose rule tests more than one parameter, or none, needs the parameter named.
//
// KEPT IN STEP WITH CREATE_VIEW_FILTERS_BY_RULE, BY HAND. A fragment is a
// snippet compiled on its own and cannot call another one's code, so these
// parts are copies and must change together: the operator words and their
// factory methods, the factory reached by reflection (CreateNotContainsRule
// and the rest are (ElementId, string, bool) on 2020 to 2025 and (ElementId,
// string) on 2023 to 2027 - the two-argument one is preferred where it
// exists, the three-argument one passed `false`), the parameter matched by its
// label or its built-in name, and the read-back's rule words.
//
// SETTING WHAT THE FILTER ALREADY HOLDS IS NOT A CHANGE. Categories are
// compared as a set and the rule by the words the read-back gives it; what
// already matches is reported in `alreadySet` and not written - so a second
// run changes nothing and leaves nothing in the undo list. The words do not
// carry 2020-2022's case-sensitive tick, so a rule differing only in that is
// taken as already so.
//
// A SELECTION FILTER HAS NO CATEGORIES - it holds chosen elements - and is
// named in `notFound` rather than being edited.
//
// THE READ-BACK IS WHAT REVIT HOLDS AFTERWARDS, for every filter named - the
// changed ones and the ones already so: name, categories and rule read out of
// the filter, and what that rule catches in the model today, per category.
//
// Everything that could be checked is checked before the first change; the
// changing calls are not caught, so an unexpected failure ends the run and
// Heron rolls the whole call back rather than leaving some filters changed.

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

var renameTo = (newName ?? "").Trim();
var wanted = (parameterName ?? "").Trim();
var ruleValue = (matchValue ?? "").Trim();
var changingRule = ruleValue.Length > 0;

// The categories, once each, with the name Revit shows for them - in the
// add-in binder's order (see the header).
var categoryIds = new List<ElementId>();
var categoryName = new Dictionary<ElementId, string>();
var unknownCategories = new List<string>();
if (categories != null)
{
    foreach (var given in categories)
    {
        var text = (given ?? "").Trim();
        if (text.Length == 0) continue;

        ElementId id = null;
        string label = null;

        BuiltInCategory builtIn;
        if (Enum.TryParse(text, true, out builtIn) && builtIn != BuiltInCategory.INVALID
            && Enum.IsDefined(typeof(BuiltInCategory), builtIn))
        {
            id = new ElementId(builtIn);
        }

        if (id == null)
        {
            foreach (Category category in doc.Settings.Categories)
            {
                if (category == null) continue;
                string readable;
                try { readable = category.Name; } catch { continue; }
                if (!string.Equals(readable, text, StringComparison.OrdinalIgnoreCase)) continue;
                id = category.Id;
                label = readable;
                break;
            }
        }

        if (id == null && Enum.TryParse("OST_" + text, true, out builtIn)
            && Enum.IsDefined(typeof(BuiltInCategory), builtIn))
        {
            id = new ElementId(builtIn);
        }

        if (id == null)
        {
            unknownCategories.Add(text);
            continue;
        }
        if (categoryIds.Contains(id)) continue;
        categoryIds.Add(id);

        if (label == null)
        {
            label = text;
            try
            {
                var category = Category.GetCategory(doc, id);
                if (category != null) label = category.Name;
            }
            catch { }
        }
        categoryName[id] = label;
    }
}

// The new rule's value split into the operator in front of it and the text it
// matches - CREATE_VIEW_FILTERS_BY_RULE's grammar, kept in step by hand.
// Longest first, so "does not contain" is never read as something shorter.
var operatorWords = new[]
{
    "does not begin with", "does not end with", "does not contain", "does not equal",
    "begins with", "ends with", "contains", "equals",
};
var operatorMethods = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "does not begin with", "CreateNotBeginsWithRule" },
    { "does not end with", "CreateNotEndsWithRule" },
    { "does not contain", "CreateNotContainsRule" },
    { "does not equal", "CreateNotEqualsRule" },
    { "begins with", "CreateBeginsWithRule" },
    { "ends with", "CreateEndsWithRule" },
    { "contains", "CreateContainsRule" },
    { "equals", "CreateEqualsRule" },
};
var ruleOperator = "equals";
var ruleText = ruleValue;
var bareOperator = false;
foreach (var word in operatorWords)
{
    if (string.Equals(ruleValue, word, StringComparison.OrdinalIgnoreCase))
    {
        bareOperator = true;
        break;
    }
    if (ruleValue.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase))
    {
        ruleOperator = word;
        ruleText = ruleValue.Substring(word.Length).Trim();
        break;
    }
}

// ---- what was asked for, checked before Revit is asked anything -----------

if (names.Count == 0)
{
    refused = "no filter name was given - name the filters to change";
}
else if (unknownCategories.Count > 0)
{
    refused = string.Format("NOTHING WAS CHANGED. {0} {1} not a category Revit knows - type what "
        + "Visibility/Graphics shows, Ducts or Mechanical Equipment, and separate several with commas",
        string.Join(", ", unknownCategories), unknownCategories.Count == 1 ? "is" : "are");
}
else if (categoryIds.Count == 0 && renameTo.Length == 0 && !changingRule && wanted.Length == 0)
{
    refused = "nothing to change was given - give the categories, a new name, a new rule, or any of "
        + "them; a blank keeps what the filter has";
}
else if (renameTo.Length > 0 && names.Count != 1)
{
    refused = string.Format("NOTHING WAS CHANGED. A new name goes on one filter, and {0} were named - "
        + "rename them one at a time", names.Count);
}
else if (renameTo.Length > 0 && !NamingUtils.IsValidName(renameTo))
{
    refused = string.Format("NOTHING WAS CHANGED. '{0}' cannot be a filter name - Revit does not allow "
        + "characters such as : ; [ ] {{ }} | < > ? ` ~ in a name", renameTo);
}
else if (wanted.Length > 0 && !changingRule)
{
    refused = string.Format("NOTHING WAS CHANGED. '{0}' was named for the rule but no value was given - "
        + "write what it should match, \"does not contain TRG\", or leave both blank to keep the rule",
        wanted);
}
else if (bareOperator)
{
    refused = string.Format("NOTHING WAS CHANGED. '{0}' is an operator with nothing after it - write "
        + "what it should match, \"{0} TRG\", or \"equals {0}\" to match the word itself", ruleValue);
}
else if (changingRule && ruleText.Length == 0)
{
    refused = "NOTHING WAS CHANGED. The rule's value was an operator with nothing after it - write the "
        + "text it should match";
}

// ---- the filters, by name --------------------------------------------------

var targets = new List<ParameterFilterElement>();
var everyFilter = new List<Element>();

if (refused.Length == 0)
{
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
    else if (renameTo.Length > 0)
    {
        // A name another filter already has - rule-based or selection, they
        // share one list - is refused by name. The filter's own name in other
        // capitals is a rename, not a clash.
        foreach (var element in everyFilter)
        {
            if (element.Id == targets[0].Id) continue;
            if (!string.Equals(element.Name ?? "", renameTo, StringComparison.OrdinalIgnoreCase)) continue;
            refused = string.Format("NOTHING WAS CHANGED. '{0}' is already the name of another filter "
                + "in this project - a filter name can be used once", element.Name);
            break;
        }
        if (refused.Length == 0
            && !string.Equals(targets[0].Name ?? "", renameTo, StringComparison.OrdinalIgnoreCase)
            && !FilterElement.IsNameUnique(doc, renameTo))
        {
            refused = string.Format("NOTHING WAS CHANGED. Revit says '{0}' is not a unique filter name in "
                + "this project", renameTo);
        }
    }
}

// ---- the categories to ask about, and what each may be filtered by ---------

// Categories given, or - when they are kept and the rule changes - the ones
// each filter already holds, since the new rule has to fit them.
var asked = new List<ElementId>(categoryIds);
if (refused.Length == 0 && categoryIds.Count == 0 && changingRule)
{
    foreach (var filter in targets)
    {
        foreach (var categoryId in filter.GetCategories())
        {
            if (asked.Contains(categoryId)) continue;
            asked.Add(categoryId);
            var label = categoryId.ToString();
            try
            {
                var category = Category.GetCategory(doc, categoryId);
                if (category != null) label = category.Name;
            }
            catch { }
            categoryName[categoryId] = label;
        }
    }
}

var allowedBy = new Dictionary<ElementId, ICollection<ElementId>>();
var allowedProblem = new Dictionary<ElementId, string>();

if (refused.Length == 0 && asked.Count > 0)
{
    ICollection<ElementId> filterable = null;
    try { filterable = ParameterFilterUtilities.GetAllFilterableCategories(); } catch { }

    foreach (var categoryId in asked)
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
}

// ---- names for the parameters ----------------------------------------------

var parameterLabel = new Dictionary<ElementId, string>();
var builtInName = new Dictionary<ElementId, string>();

if (refused.Length == 0 && targets.Count > 0)
{
    // Every parameter the rules test today, and - when a parameter is named for
    // a new rule - every one the categories asked about may be filtered by.
    var everyCandidate = new HashSet<ElementId>();
    foreach (var filter in targets)
    {
        var tested = filter.GetElementFilterParameters();
        if (tested == null) continue;
        foreach (var parameterId in tested) if (parameterId != null) everyCandidate.Add(parameterId);
    }
    if (wanted.Length > 0)
    {
        foreach (var allowed in allowedBy.Values)
            foreach (var parameterId in allowed) if (parameterId != null) everyCandidate.Add(parameterId);
    }

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
            builtInName[candidate] = builtIn.ToString();
            unnamed.Remove(candidate);
        }
        foreach (var parameterId in unnamed) parameterLabel[parameterId] = parameterId.ToString();
    }
}

// A parameter answers to the name typed - by the label Revit shows, or by its
// built-in name.
Func<ElementId, bool> answersToName = parameterId =>
    parameterId != null
    && ((parameterLabel.ContainsKey(parameterId)
            && string.Equals(parameterLabel[parameterId], wanted, StringComparison.OrdinalIgnoreCase))
        || (builtInName.ContainsKey(parameterId)
            && string.Equals(builtInName[parameterId], wanted, StringComparison.OrdinalIgnoreCase)));

// A filter's rules in the read-back's words, walked without recursion - a
// logical filter holds others. The same words CREATE_VIEW_FILTERS_BY_RULE's
// read-back gives, and what "already so" is judged by.
Func<ElementFilter, string> describeRule = held =>
{
    var heldRules = new List<string>();
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

        foreach (var outerRule in byParameter.GetRules())
        {
            // A "does not" rule is held as the positive one, inverted.
            var heldRule = outerRule;
            var inverted = false;
            var inverse = outerRule as FilterInverseRule;
            if (inverse != null && inverse.GetInnerRule() != null)
            {
                heldRule = inverse.GetInnerRule();
                inverted = true;
            }

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
                // Revit's Filters dialog words, not the evaluator's class name.
                var evaluator = textRule.GetEvaluator();
                var verb = evaluator == null ? "?" : evaluator.GetType().Name;
                if (verb == "FilterStringEquals") verb = inverted ? "does not equal" : "equals";
                else if (verb == "FilterStringContains") verb = inverted ? "does not contain" : "contains";
                else if (verb == "FilterStringBeginsWith") verb = inverted ? "does not begin with" : "begins with";
                else if (verb == "FilterStringEndsWith") verb = inverted ? "does not end with" : "ends with";
                else if (inverted) verb = "NOT " + verb;
                heldRules.Add(string.Format("{0} {1} '{2}'", parameterText, verb, textRule.RuleString));
            }
            else
            {
                heldRules.Add(string.Format("{0} ({1}{2})", parameterText, inverted ? "NOT " : "",
                    heldRule.GetType().Name));
            }
        }
    }
    return heldRules.Count == 0 ? "NONE" : string.Join(" AND ", heldRules);
};

// ---- the rule factory, in whichever spelling this release has --------------

System.Reflection.MethodInfo withoutFlag = null;
System.Reflection.MethodInfo withFlag = null;
if (refused.Length == 0 && changingRule)
{
    foreach (var candidate in typeof(ParameterFilterRuleFactory).GetMethods())
    {
        if (candidate.Name != operatorMethods[ruleOperator]) continue;
        var arguments = candidate.GetParameters();
        if (arguments.Length < 2 || arguments[0].ParameterType != typeof(ElementId)
            || arguments[1].ParameterType != typeof(string)) continue;
        if (arguments.Length == 2) withoutFlag = candidate;
        else if (arguments.Length == 3 && arguments[2].ParameterType == typeof(bool)) withFlag = candidate;
    }
    if (withoutFlag == null && withFlag == null)
    {
        refused = string.Format("NOTHING WAS CHANGED. This Revit version has no text rule '{0}' this "
            + "recognises", ruleOperator);
    }
}

// ---- what each filter will hold, worked out before anything is set --------

var plan = new Dictionary<ParameterFilterElement, List<ElementId>>();
var newRule = new Dictionary<ParameterFilterElement, ElementFilter>();
var refusals = new List<string>();
// "category - reason" -> the filters it was left out of, so one reason shared by
// every filter is said once.
var leftOutOf = new Dictionary<string, List<string>>();
var leftOutOrder = new List<string>();
// One rule per parameter, so filters testing the same one share it.
var builtRule = new Dictionary<ElementId, ElementFilter>();

if (refused.Length == 0 && targets.Count > 0)
{
    foreach (var filter in targets)
    {
        var current = new List<ElementId>(filter.GetCategories());

        // A rename alone asks nothing of the categories or the rule.
        if (categoryIds.Count == 0 && !changingRule)
        {
            plan[filter] = current;
            continue;
        }

        var keepingCategories = categoryIds.Count == 0;
        var candidates = keepingCategories ? current : categoryIds;
        ElementFilter rule = filter.GetElementFilter();
        ICollection<ElementId> tested = filter.GetElementFilterParameters();
        var reasons = new List<KeyValuePair<ElementId, string>>();
        var pool = new List<ElementId>(candidates);

        if (changingRule)
        {
            ElementId ruleParameter = null;
            if (wanted.Length == 0)
            {
                // The parameter the filter's rule tests today, kept.
                var count = tested == null ? 0 : tested.Count;
                if (count != 1)
                {
                    refusals.Add(string.Format("'{0}' - its rule tests {1} parameters, so the new rule's "
                        + "parameter has to be named", filter.Name, count == 0 ? "no" : count.ToString()));
                    continue;
                }
                foreach (var parameterId in tested) ruleParameter = parameterId;
            }
            else
            {
                // Each category asked on its own whether it may be filtered by a
                // parameter of that name, then the one they share.
                pool.Clear();
                foreach (var categoryId in candidates)
                {
                    if (allowedProblem.ContainsKey(categoryId))
                    {
                        reasons.Add(new KeyValuePair<ElementId, string>(categoryId, allowedProblem[categoryId]));
                        continue;
                    }
                    var answers = false;
                    foreach (var parameterId in allowedBy[categoryId])
                    {
                        if (answersToName(parameterId)) { answers = true; break; }
                    }
                    if (answers) pool.Add(categoryId);
                    else reasons.Add(new KeyValuePair<ElementId, string>(categoryId,
                        string.Format("it cannot be filtered by '{0}'", wanted)));
                }

                if (pool.Count > 0)
                {
                    ICollection<ElementId> common = null;
                    var why = "";
                    try { common = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, pool); }
                    catch (Exception ex) { why = ex.Message; }

                    var matching = new List<ElementId>();
                    if (common != null)
                        foreach (var parameterId in common) if (answersToName(parameterId)) matching.Add(parameterId);

                    if (common == null)
                    {
                        refusals.Add(string.Format("'{0}' - Revit would not list the parameters its categories "
                            + "share: {1}", filter.Name, why));
                        continue;
                    }
                    if (matching.Count == 0)
                    {
                        refusals.Add(string.Format("'{0}' - every category can be filtered by a parameter called "
                            + "'{1}', but not by the SAME one, and a rule is built on one", filter.Name, wanted));
                        continue;
                    }
                    if (matching.Count > 1)
                    {
                        var described = new List<string>();
                        foreach (var parameterId in matching)
                        {
                            described.Add(builtInName.ContainsKey(parameterId)
                                ? string.Format("'{0}' (built in, {1})", parameterLabel[parameterId],
                                    builtInName[parameterId])
                                : string.Format("'{0}' (a project or shared parameter)", parameterLabel[parameterId]));
                        }
                        refusals.Add(string.Format("'{0}' - '{1}' names {2} different parameters on its "
                            + "categories: {3}. Type the built-in one by its Revit name to choose it",
                            filter.Name, wanted, matching.Count, string.Join(", ", described)));
                        continue;
                    }
                    ruleParameter = matching[0];
                }
            }

            if (ruleParameter != null)
            {
                if (!builtRule.ContainsKey(ruleParameter))
                {
                    FilterRule made = null;
                    var cause = "";
                    try
                    {
                        made = withoutFlag != null
                            ? withoutFlag.Invoke(null, new object[] { ruleParameter, ruleText }) as FilterRule
                            : withFlag.Invoke(null, new object[] { ruleParameter, ruleText, false }) as FilterRule;
                    }
                    catch (Exception ex)
                    {
                        cause = ": " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                    }
                    builtRule[ruleParameter] = made == null ? null : new ElementParameterFilter(made);
                    if (made == null)
                    {
                        refusals.Add(string.Format("'{0}' - Revit would not build the rule '{1}' {2} '{3}'{4}",
                            filter.Name, parameterLabel.ContainsKey(ruleParameter)
                                ? parameterLabel[ruleParameter] : ruleParameter.ToString(),
                            ruleOperator, ruleText, cause));
                        continue;
                    }
                }
                if (builtRule[ruleParameter] == null) continue;
                rule = builtRule[ruleParameter];
                tested = new List<ElementId>();
                tested.Add(ruleParameter);
            }
        }

        // Each category left in the running, asked whether it may be filtered by
        // every parameter the rule tests, then put to Revit's own test.
        var keep = new List<ElementId>();
        foreach (var categoryId in pool)
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
                    reasons.Add(new KeyValuePair<ElementId, string>(categoryId, (changingRule
                        ? "Revit rejects the new rule for it" : "Revit rejects this filter's rule for it") + why));
                    continue;
                }
            }

            keep.Add(categoryId);
        }

        if (keepingCategories && reasons.Count > 0)
        {
            // A new rule that does not fit a category the filter holds: leaving
            // that category out would take off one nobody mentioned.
            var said = new List<string>();
            foreach (var reason in reasons)
                said.Add(string.Format("{0} - {1}", categoryName[reason.Key], reason.Value));
            refusals.Add(string.Format("'{0}' - the new rule cannot apply to every category it holds: {1}. "
                + "Give the categories it should hold instead", filter.Name, string.Join("; ", said)));
            continue;
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
                + "can take its {1}rule, and a filter is never emptied", filter.Name,
                changingRule ? "new " : ""));
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
                refusals.Add(string.Format("'{0}' left exactly as it was - Revit accepts its {1}rule for "
                    + "each category alone but not for all of them together{2}", filter.Name,
                    changingRule ? "new " : "", why));
                continue;
            }
        }

        plan[filter] = keep;
        if (changingRule) newRule[filter] = rule;
    }

    foreach (var key in leftOutOrder)
    {
        leftOut.Add(leftOutOf[key].Count == targets.Count
            ? key
            : string.Format("{0} (for {1})", key, string.Join(", ", leftOutOf[key])));
    }
}

// A new name or a new rule is all or nothing: one filter that cannot take it
// leaves every filter as it was.
var allOrNothing = (renameTo.Length > 0 || changingRule) && refusals.Count > 0;
if (allOrNothing)
{
    plan.Clear();
    newRule.Clear();
}

// ---- change, then read every named filter back out of Revit ----------------

if (refused.Length == 0 && targets.Count > 0)
{
    var lines = new List<string>();

    foreach (var filter in targets)
    {
        var state = "left as it was";
        if (plan.ContainsKey(filter))
        {
            var oldName = filter.Name;
            var currentSet = new HashSet<ElementId>(filter.GetCategories());
            var categoriesChange = !currentSet.SetEquals(plan[filter]);
            var ruleChange = newRule.ContainsKey(filter)
                && describeRule(filter.GetElementFilter()) != describeRule(newRule[filter]);
            var nameChange = renameTo.Length > 0 && oldName != renameTo;

            if (!categoriesChange && !ruleChange && !nameChange)
            {
                alreadySet.Add(oldName);
                state = "already so, not changed";
            }
            else
            {
                // Not caught, on purpose - see the header. Both changing: the old
                // rule may not fit the new categories nor the new rule the old
                // ones, so the rule is cleared, the categories set, then the rule.
                if (ruleChange && categoriesChange)
                {
                    filter.ClearRules();
                    filter.SetCategories(plan[filter]);
                    if (!filter.SetElementFilter(newRule[filter]))
                        throw new InvalidOperationException(string.Format("Revit would not set the new rule on "
                            + "'{0}', though it passed every check first - nothing is kept", oldName));
                }
                else if (ruleChange)
                {
                    if (!filter.SetElementFilter(newRule[filter]))
                        throw new InvalidOperationException(string.Format("Revit would not set the new rule on "
                            + "'{0}', though it passed every check first - nothing is kept", oldName));
                }
                else if (categoriesChange)
                {
                    filter.SetCategories(plan[filter]);
                }
                if (nameChange) filter.Name = renameTo;

                changed.Add(filter);
                var what = new List<string>();
                if (nameChange) what.Add(string.Format("renamed from '{0}'", oldName));
                if (categoriesChange) what.Add("categories set");
                if (ruleChange) what.Add("rule set");
                state = "changed - " + string.Join(", ", what);
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

        var held = filter.GetElementFilter();

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
            describeRule(held), total,
            perCategory.Count == 0 ? "" : " (" + string.Join(", ", perCategory) + ")"));
    }

    readBack = string.Join(" | ", lines);

    var said = new List<string>();
    if (allOrNothing)
    {
        said.Add(string.Format("NOTHING WAS CHANGED - a new name or rule changes every filter named or "
            + "none: {0}", string.Join("; ", refusals)));
    }
    else
    {
        said.AddRange(refusals);
    }
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
