// NOT STANDALONE. Assumes `doc`, `view`, `filterName`, `categoryIds`,
// `parameterName`, `matchValue`, `moreRules`, `combine` and `colour` are in
// scope; leaves `created` and `refused` behind.
//
// VERSION 3 (2026-10-15): THE PARAMETER IS TYPED BY ITS NAME. Version 2 took
// `parameterId`, an ElementId the add-in binds by no name at all
// (FRAGMENT-ISSUES 5b-191), so every call was refused before it reached Revit.
// `parameterName` is the name Revit shows - "Type Name", "System
// Classification" - looked for among the parameters the filter's categories
// may ALL be filtered by (ParameterFilterUtilities), so no element has to
// exist. A name that means none of them, or two, is refused and nothing is
// made; a built-in parameter can also be typed by its Revit name,
// RBS_SYSTEM_CLASSIFICATION_PARAM, which is how one of two is chosen.
//
// AND `matchValue` MAY LEAD WITH AN OPERATOR, in the words of Revit's own
// Filters dialog, exactly as CREATE_VIEW_FILTERS_BY_RULE version 2 reads one:
// equals, does not equal, contains, does not contain, begins with, does not
// begin with, ends with, does not end with - "does not contain Level 1". No
// operator is equals, as versions 1 and 2 read it. "equals contains" matches
// the word itself; an operator with nothing after it is refused.
//
// MORE THAN ONE RULE (version 2, 2026-10-13, the gap fill). `moreRules` adds
// rules beside the first, "Parameter Name = value; Other Name contains text",
// each operator one of =, != or the eight words above; the parameter is found
// by name the same way as the first one's. `combine` "or" makes the filter
// match any rule; EMPTY or "and" makes it match all. EMPTY `moreRules` is the
// one rule alone.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FILTER IS NOT AN OVERRIDE, and the difference decides whether the drawing
// still reads correctly next month. An override is stuck to the element ids it
// was given; a filter matches. A duct drawn next week on the right system comes
// out the right colour by itself, and on a drawing that gets reissued that is
// the entire reason to use one.
//
// THE RULE FACTORIES ARE REACHED BY REFLECTION, by method name. Each text rule
// has two spellings, read off the reference assembly for each release:
//
//   Create<Op>Rule(ElementId, string, bool)   2020 to 2025. REMOVED IN 2026
//                                             - the flag is case sensitivity
//   Create<Op>Rule(ElementId, string)         ADDED IN 2023
//
// Neither covers the whole span, and until version 3 this split on a REVIT20xx
// compile symbol. The add-in compiled fragments with NO release symbols until
// 2026-10-09 (FRAGMENT-ISSUES 5b-181), so on 2020 to 2022 it took the newer
// branch and failed; an add-in installed before that change still does. So the
// overload is picked at run time, as CREATE_VIEW_FILTERS_BY_RULE picks it: the
// two-argument one where it exists, otherwise the three-argument one passed
// `false`, NOT case sensitive.
//
// >> VERSION 2 PASSED `true` THERE, AND IT CHANGED ON PURPOSE. 2023 and later
// >> have nowhere to say, so a case-sensitive 2020 to 2022 could match
// >> different elements from the same filter on a newer Revit. `false` is what
// >> CREATE_VIEW_FILTERS_BY_RULE and CREATE_VIEW_FILTERS_BY_VALUE pass. Whether
// >> the two-argument form ignores capitals is still not measured - the test
// >> cases say so rather than this comment settling it.
//
// THE NAME IS CHECKED FIRST. A filter lives in the PROJECT, not in the view, so
// a duplicate name collides with something another view may be relying on -
// and Revit throws, after which the view has been modified and the filter has
// not been made. Every rule is built, and the whole filter tested against
// Revit's own acceptability check, before anything is created.

ElementId created = null;
string refused = null;

var name = (filterName ?? "").Trim();
var wanted = (parameterName ?? "").Trim();

// The eight text rules the Filters dialog offers, longest first so "does not
// contain" is never read as something shorter, and the factory each one calls.
// `=` and `!=` are moreRules' short forms of equals and does not equal.
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
    { "!=", "CreateNotEqualsRule" },
    { "=", "CreateEqualsRule" },
};

// The first rule's operator, read off the front of matchValue.
var firstOperator = "equals";
var firstText = (matchValue ?? "").Trim();
var bareOperator = "";
foreach (var word in operatorWords)
{
    if (string.Equals(firstText, word, StringComparison.OrdinalIgnoreCase))
    {
        bareOperator = firstText;
        break;
    }
    if (firstText.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase))
    {
        firstOperator = word;
        firstText = firstText.Substring(word.Length).Trim();
        break;
    }
}

if (view == null || view.IsTemplate)
{
    refused = "a filter is applied to a real view - a template is not one";
}
else if (name.Length == 0)
{
    refused = "a filter needs a name - it goes in the project's filter list, where "
            + "other views will see it";
}
else if (categoryIds == null || categoryIds.Count == 0)
{
    refused = "a filter needs at least one category to look at";
}
else if (wanted.Length == 0)
{
    refused = "a filter needs a parameter to test, by the name Revit shows - without one it "
            + "matches everything in the category, which is what SET_CATEGORY_GRAPHICS is for";
}
else if (bareOperator.Length > 0)
{
    refused = string.Format("'{0}' is an operator with nothing after it - write what it should "
            + "match, \"{0} Level 1\", or \"equals {0}\" to match the word itself - nothing was made",
            bareOperator);
}
else
{
    ParameterFilterElement clash = null;

    foreach (var existing in new FilteredElementCollector(doc)
                                 .OfClass(typeof(ParameterFilterElement))
                                 .Cast<ParameterFilterElement>())
    {
        if (existing == null) continue;
        if (string.Equals(existing.Name ?? "", name, StringComparison.OrdinalIgnoreCase))
        {
            clash = existing;
            break;
        }
    }

    string ruleProblem = null;

    // What every category may be filtered by, and a name for each. A project
    // or shared parameter is an element and names itself; a built-in one's id
    // is its enum value, whose label is the same on every release, where the
    // property that holds the number changed.
    ICollection<ElementId> shared = null;
    try { shared = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, categoryIds); }
    catch (Exception ex) { ruleProblem = "Revit would not list the parameters these categories share: " + ex.Message; }
    if (shared == null) shared = new List<ElementId>();

    Func<ElementId, string> labelOf = id =>
    {
        var element = doc.GetElement(id);
        if (element != null) return element.Name;
        try { return LabelUtils.GetLabelFor((BuiltInParameter)(int)long.Parse(id.ToString())); } catch { return ""; }
    };
    Func<ElementId, string> builtInNameOf = id =>
    {
        if (doc.GetElement(id) != null) return "";
        try { return ((BuiltInParameter)(int)long.Parse(id.ToString())).ToString(); } catch { return ""; }
    };

    // The ONE parameter a name means, or the reason it means none or two.
    string parameterProblem = null;
    Func<string, ElementId> parameterCalled = paramName =>
    {
        parameterProblem = null;
        var found = shared.Where(id => id != null
            && (string.Equals(labelOf(id), paramName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(builtInNameOf(id), paramName, StringComparison.OrdinalIgnoreCase))).ToList();
        if (found.Count == 1) return found[0];
        if (found.Count == 0)
        {
            parameterProblem = "no parameter the filter's categories share is called \"" + paramName + "\"";
        }
        else
        {
            var described = found.Select(id => builtInNameOf(id).Length > 0
                ? "'" + labelOf(id) + "' (built in, " + builtInNameOf(id) + ")"
                : "'" + labelOf(id) + "' (a project or shared parameter)").ToList();
            parameterProblem = found.Count + " parameters the filter's categories share are called \""
                + paramName + "\": " + string.Join(", ", described)
                + " - type the built-in one by its Revit name to choose it";
        }
        return null;
    };

    // A text rule, from the factory for its operator in whichever spelling
    // this release has.
    string factoryProblem = null;
    Func<ElementId, string, string, FilterRule> textRule = (pid, op, value) =>
    {
        factoryProblem = null;
        var method = operatorMethods[op];
        System.Reflection.MethodInfo withoutFlag = null;
        System.Reflection.MethodInfo withFlag = null;
        foreach (var candidate in typeof(ParameterFilterRuleFactory).GetMethods())
        {
            if (candidate.Name != method) continue;
            var arguments = candidate.GetParameters();
            if (arguments.Length < 2 || arguments[0].ParameterType != typeof(ElementId)
                || arguments[1].ParameterType != typeof(string)) continue;
            if (arguments.Length == 2) withoutFlag = candidate;
            else if (arguments.Length == 3 && arguments[2].ParameterType == typeof(bool)) withFlag = candidate;
        }
        if (withoutFlag == null && withFlag == null)
        {
            factoryProblem = "this Revit version has no text rule '" + op + "' this recognises";
            return null;
        }
        try
        {
            var made = withoutFlag != null
                ? withoutFlag.Invoke(null, new object[] { pid, value }) as FilterRule
                : withFlag.Invoke(null, new object[] { pid, value, false }) as FilterRule;
            if (made == null) factoryProblem = "Revit built no rule '" + op + " " + value + "'";
            return made;
        }
        catch (Exception ex)
        {
            factoryProblem = "Revit would not build the rule '" + op + " " + value + "': "
                + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            return null;
        }
    };

    // The first rule.
    FilterRule rule = null;
    if (ruleProblem == null)
    {
        var firstParameter = parameterCalled(wanted);
        if (firstParameter == null) ruleProblem = parameterProblem;
        else
        {
            rule = textRule(firstParameter, firstOperator, firstText);
            if (rule == null) ruleProblem = factoryProblem;
        }
    }

    // The extra rules, read before anything is made. The operator is the one
    // that starts EARLIEST in the text - the parameter's name comes first -
    // and the longest of those that start there.
    var extra = new List<FilterRule>();
    var how = (combine ?? "").Trim().ToLowerInvariant();
    if (ruleProblem == null && how != "" && how != "and" && how != "or")
        ruleProblem = "\"" + combine + "\" is not and or or";
    if (ruleProblem == null && !string.IsNullOrWhiteSpace(moreRules))
    {
        var moreOperators = operatorWords.Concat(new[] { "!=", "=" }).ToArray();
        foreach (var text in moreRules.Split(';'))
        {
            if (text.Trim() == "") continue;
            string op = null;
            var at = -1;
            foreach (var candidate in moreOperators)
            {
                var position = text.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
                if (position <= 0) continue;
                if (op == null || position < at || (position == at && candidate.Length > op.Length))
                {
                    op = candidate;
                    at = position;
                }
            }
            if (op == null) { ruleProblem = "\"" + text.Trim() + "\" has no operator (=, !=, " + string.Join(", ", operatorWords) + ")"; break; }
            var paramName = text.Substring(0, at).Trim();
            var value = text.Substring(at + op.Length).Trim();
            var pid = parameterCalled(paramName);
            if (pid == null) { ruleProblem = parameterProblem; break; }
            var made = textRule(pid, op, value);
            if (made == null) { ruleProblem = factoryProblem; break; }
            extra.Add(made);
        }
    }

    ElementFilter test = null;
    if (ruleProblem == null)
    {
        if (extra.Count == 0) test = new ElementParameterFilter(rule);
        else
        {
            var all = new List<FilterRule> { rule };
            all.AddRange(extra);
            test = how == "or"
                ? (ElementFilter)new LogicalOrFilter(all.Select(r => (ElementFilter)new ElementParameterFilter(r)).ToList())
                : new ElementParameterFilter(all);
        }

        // Revit's own test, before anything exists: a text rule on a parameter
        // that holds a number is the usual reason it says no.
        var acceptable = false;
        var why = "";
        try
        {
            acceptable = ParameterFilterElement.ElementFilterIsAcceptableForParameterFilterElement(
                doc, new HashSet<ElementId>(categoryIds), test);
        }
        catch (Exception ex) { why = " - " + ex.Message; }
        if (!acceptable)
            ruleProblem = "Revit says this rule cannot be used on these categories" + why
                + ". A text rule on a parameter that holds a number is the usual reason";
    }

    if (ruleProblem != null)
    {
        refused = ruleProblem + " - nothing was made";
    }
    else if (clash != null)
    {
        refused = string.Format(
            "a filter called \"{0}\" already exists in this project. Filters are "
            + "project-wide, so another view may be relying on it - and Revit throws on "
            + "the duplicate after the view has already been touched", name);
    }
    else
    {
        var filter = ParameterFilterElement.Create(doc, name, categoryIds, test);

        if (filter == null)
        {
            refused = "Revit declined to create the filter";
        }
        else
        {
            created = filter.Id;

            view.AddFilter(filter.Id);

            // Projection AND cut, because a duct in a plan is cut by the view
            // range and a duct in a 3D view is not. Setting only one leaves
            // half the drawing uncoloured, and which half depends on the view.
            var look = new OverrideGraphicSettings();
            look.SetProjectionLineColor(colour);
            look.SetCutLineColor(colour);
            look.SetSurfaceForegroundPatternColor(colour);
            look.SetCutForegroundPatternColor(colour);

            view.SetFilterOverrides(filter.Id, look);
        }
    }
}
