// NOT STANDALONE. Assumes `doc`, `view`, `filterName`, `categoryIds`,
// `parameterName`, `matchValue`, `moreRules`, `combine`, `colour`,
// `elements`, `perValue` and `namePrefix` are in scope; leaves `created`,
// `createdIds`, `readBack`, `alreadyExists`, `cannotFilter`, `refused`,
// `valuesFound`, `droppedCategories` and `leftOut` behind.
//
// VERSION 5 (2026-11-01) - CREATE_VIEW_FILTERS_BY_VALUE MERGED IN, on the
// owner's instruction ("make sure there is no duplicate tools"). ONE FILTER PER
// DISTINCT VALUE, THE VALUES READ OFF THE ELEMENTS - "a filter per system",
// "one for each fire zone" - asked for with `perValue` true and the elements
// handed in. What that card had that this lacked:
//
//   THE VALUES ARE READ OFF THE ELEMENTS, never typed: every distinct value of
//   the parameter across them, capitals ignored, because the rule ignores
//   them too. Each filter is named `namePrefix` and the value.
//
//   THE CATEGORIES COME FROM THE ELEMENTS when none are typed - the distinct
//   categories they belong to. THE OPPOSITE RULES, AND WHICH WAS KEPT WHERE:
//   a category TYPED that cannot take the rule STOPS THE CALL and is named, as
//   this card always did - the caller named it, so a filter without it is not
//   what was asked; a category DERIVED from the elements that cannot take the
//   rule is DROPPED AND NAMED in `droppedCategories`, as that card did -
//   nobody named it, and Revit rejects a filter whose categories hold one the
//   parameter does not apply to, so including it would lose every filter.
//
//   AN ELEMENT CARRYING THE NAME TWICE IS LEFT OUT - a shared or project
//   parameter bound beside a built-in one of the same name, where Revit's
//   lookup picks one "at random" in Autodesk's own words - and never gives a
//   value; so is one with no such parameter on it or on its type, and one
//   outside the categories typed. Each kind counted and named in `leftOut`
//   (D-54 s3, FRAGMENT-ISSUES 5b-203).
//
//   THE BLANK GROUP is a filter of its own, named "(no value)", so elements
//   with nothing in the parameter are not left plain - plain, in a coloured
//   drawing, reads as "not part of this". Its rule is THIS CARD's: "has no
//   value" (CreateHasNoValueParameterRule), where that card built an equals
//   rule on empty text.
//
//   THE COLOURS ARE STEPPED EVENLY ROUND THE HUE WHEEL, ONE PER FILTER, on
//   the view given - a fixed palette of six repeats on the seventh system and
//   two runs then look like one. So a `colour` beside perValue is refused:
//   one colour for every value is the opposite of the job. Left out with the
//   view too, the filters are made on no view, as typed filters are.
//
//   Everything else is this card's: a name already taken is skipped and
//   named, never overwritten; every filter's test is checked by Revit before
//   any filter exists; a name Revit cannot take stops the call before any is
//   made; and each new filter is read back. `moreRules` is refused beside
//   perValue, as it is beside several names.
//
// `filterName`, `matchValue` and `categoryIds` are OPTIONAL since version 5,
// because a perValue call names none of them - left out they read `none`,
// `none` and INVALID. A typed call that leaves one out is refused here, in
// words, as the binder refused it before; "equals none" matches the word.
//
// VERSION 4 (2026-11-01) - CREATE_VIEW_FILTERS_BY_RULE MERGED IN, on the
// owner's instruction. What it had that this lacked:
//
//   MANY FILTERS IN ONE CALL, ONE VALUE EACH. `filterName` is a list now,
//   comma separated, and `matchValue` holds one value per name in the same
//   order - "Supply Air, Return Air, Exhaust Air" with three values, divided
//   by this code (see the review fixes below). One name and one value mean
//   what they meant in version 3. moreRules belongs to ONE filter, and is
//   refused beside several names.
//
//   THE VIEW AND THE COLOUR ARE OPTIONAL, TOGETHER. Both left out, the filters
//   are made on NO view - the standards job, done the way a modeller does it
//   in Manage > Filters - and APPLY_VIEW_FILTER puts one on a view later. A
//   view with no colour, or a colour with no view, is refused before anything
//   is made: neither half alone is something this can finish.
//
//   CATEGORIES BY REVIT'S OWN NAME TOO - OST_DuctCurves beside "Ducts".
//   `categoryIds` is read as a list of BuiltInCategory, which the binder fills
//   from either spelling.
//
//   EVERY CATEGORY ASKED ON ITS OWN, and each that cannot take the rule NAMED
//   in `cannotFilter` - Revit cannot filter it at all
//   (GetAllFilterableCategories), or it has no parameter by that name. Revit
//   rejects a filter whose categories hold one the parameter does not apply
//   to, and rejects it whole, so one entry there means nothing was made.
//
//   PAIRING, BLANK, REPEAT AND NAME REFUSALS before Revit is asked anything:
//   as many values as names, none blank, no name twice, every name one
//   NamingUtils.IsValidName takes.
//
//   A NAME ALREADY TAKEN IS SKIPPED AND NAMED, NEVER OVERWRITTEN, and the
//   others are still made (`alreadyExists`). The clash scan reads every
//   FilterElement, so a SELECTION filter of the same name is seen too - both
//   live in one list and Revit throws on the duplicate. Version 3 scanned rule
//   filters only. Anything else that stops one filter stops all of them, before
//   any exists.
//
//   `readBack`: each new filter as Revit holds it - its categories, its rules
//   (a "does not" rule is held as a FilterInverseRule round the positive one,
//   and is unwrapped and said as "does not"), how many elements it catches in
//   the model today, and whether it is on the view with its colour.
//
// `created` is still ONE id - the filter made, or the first of several - so a
// caller of version 3 reads what it read; `createdIds` is every one.
//
// VERSION 4, REVIEW FIXES OF 2026-11-01 (the owner's rule: a merged card
// covers everything both old cards did):
//
//   A BLANK VALUE IS ASKED FOR IN WORDS - "has no value", or "is blank" -
//   the Filters dialog's own operator, built by Revit's
//   CreateHasNoValueParameterRule (in RevitAPI 2020 to 2027, read off the
//   reference assemblies). It catches elements whose parameter holds no
//   value. An EMPTY value is still refused, as both old cards refused it: a
//   value left out by accident must not make a filter that catches every
//   element with the parameter unset. "has no value Level 1" - the operator
//   with text after it - is refused, and "equals has no value" matches the
//   words. It works in moreRules too ("Comments has no value"). NOT COPIED
//   INTO SET_VIEW_FILTER_CATEGORIES, whose rule code is otherwise kept in
//   step with this by hand: that card is PROVEN, and its impl/ is changed
//   only with its own next version.
//
//   A COMMA INSIDE ONE VALUE NO LONGER SPLITS IT. `matchValue` is TEXT again,
//   as in version 3, and divided here rather than by the binder - the
//   binder's list rule (Parts() in RevitFragment.cs) splits on every comma
//   and has no quote or escape, so "Smith, John" arrived as two values and
//   one name, and was refused. Now: with ONE filter name the whole text is
//   the one value, commas and all, exactly as version 3 read it. With
//   several names it is divided at the commas, and a value holding a comma
//   is written in double quotes - Supply Air, "Return, Mixed" - which are
//   taken off. A value that is wholly in double quotes is taken as written
//   inside them in either case, so "does not contain" in quotes matches
//   those words rather than being read as an operator.
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
var createdIds = new List<ElementId>();
string refused = null;
var readBack = "";
var alreadyExists = new List<string>();
var cannotFilter = new List<string>();
var valuesFound = new List<string>();
var droppedCategories = new List<string>();
var leftOut = new List<string>();

// Version 5: what leaving a need out reads as, and which road this call is.
Func<string, bool> isNone = text => string.Equals((text ?? "").Trim(), "none", StringComparison.OrdinalIgnoreCase);
var prefix = isNone(namePrefix) ? "" : (namePrefix ?? "");
var noValueGiven = isNone(matchValue) || (matchValue ?? "").Trim().Length == 0;
var handed = new List<Element>();
if (elements != null) foreach (var element in elements) if (element != null && element.IsValidObject) handed.Add(element);
// `moreRules` left out reads "none" (version 5): no more rules, as an empty
// one always meant.
if (isNone(moreRules)) moreRules = "";

// One name per filter and one value per name, in the same order (version 4).
// A single name and a single value bind as a list of one, so every call
// version 3 took is still taken.
var names = new List<string>();
if (filterName != null)
    foreach (var givenName in filterName) names.Add((givenName ?? "").Trim());
// LEFT OUT reads as the one name "none" (version 5) - no name given.
if (names.Count == 1 && isNone(names[0])) names.Clear();
var namesGiven = names.Count > 0;

// THE VALUES, DIVIDED HERE AND NOT BY THE BINDER (review fix, 2026-11-01).
// One name: the whole text is its one value, commas and all - version 3's
// reading. Several: divided at each comma outside double quotes, an empty
// piece dropped as the binder drops one ("a,,b" is a typing slip).
var values = new List<string>();
var quoteProblem = "";
var valueText = noValueGiven ? "" : (matchValue ?? "").Trim();
if (perValue)
{
    // The values come from the elements, below; none is typed.
}
else if (names.Count <= 1)
{
    values.Add(valueText);
}
else
{
    var piece = new System.Text.StringBuilder();
    var inQuotes = false;
    foreach (var character in valueText)
    {
        if (character == '"') inQuotes = !inQuotes;
        if (character == ',' && !inQuotes)
        {
            if (piece.ToString().Trim().Length > 0) values.Add(piece.ToString().Trim());
            piece.Length = 0;
            continue;
        }
        piece.Append(character);
    }
    if (piece.ToString().Trim().Length > 0) values.Add(piece.ToString().Trim());
    if (inQuotes)
        quoteProblem = "matchValue opens a double quote and never closes it - put a value that holds "
            + "a comma in double quotes, \"Return, Mixed\", and close it; nothing was made";
}

// A value wholly in double quotes - one pair, nothing outside it - is the text
// inside them, taken as written.
Func<string, string> unquoted = text =>
    text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"'
        && text.IndexOf('"', 1) == text.Length - 1
        ? text.Substring(1, text.Length - 2)
        : text;

var wanted = (parameterName ?? "").Trim();

// The eight text rules the Filters dialog offers, longest first so "does not
// contain" is never read as something shorter, and the factory each one calls.
// `=` and `!=` are moreRules' short forms of equals and does not equal.
var operatorWords = new[]
{
    "does not begin with", "does not end with", "does not contain", "does not equal",
    "has no value", "begins with", "ends with", "is blank", "contains", "equals",
};
// THE OPERATORS THAT TAKE NO TEXT (review fix, 2026-11-01): the value IS the
// operator, and text after one is refused. "is blank" is the same rule in
// plainer words.
var noTextOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "has no value", "is blank" };
var operatorMethods = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "has no value", "CreateHasNoValueParameterRule" },
    { "is blank", "CreateHasNoValueParameterRule" },
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

// Each value's operator, read off its front. No operator is equals.
var operators = new List<string>();
var texts = new List<string>();
var bareOperator = "";
var noTextWithText = "";
foreach (var value in values)
{
    var valueOperator = "equals";
    var ruleText = unquoted(value);
    foreach (var word in operatorWords)
    {
        if (string.Equals(value, word, StringComparison.OrdinalIgnoreCase))
        {
            // "has no value" alone IS the rule; any other word alone has
            // nothing to match.
            if (noTextOperators.Contains(word)) { valueOperator = word; ruleText = ""; }
            else if (bareOperator.Length == 0) bareOperator = value;
            break;
        }
        if (value.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase))
        {
            if (noTextOperators.Contains(word))
            {
                if (noTextWithText.Length == 0) noTextWithText = value;
                break;
            }
            valueOperator = word;
            ruleText = unquoted(value.Substring(word.Length).Trim());
            break;
        }
    }
    operators.Add(valueOperator);
    texts.Add(ruleText);
}

// The categories, once each, with the name Revit shows for them (version 4:
// typed as Visibility/Graphics shows them OR by Revit's own name,
// OST_DuctCurves - the binder reads both into a BuiltInCategory).
var categoryList = new List<ElementId>();
var categoryName = new Dictionary<ElementId, string>();
if (categoryIds != null)
{
    foreach (var builtIn in categoryIds)
    {
        // LEFT OUT binds INVALID (version 5), which names no category.
        if (builtIn == BuiltInCategory.INVALID) continue;
        var categoryId = new ElementId(builtIn);
        if (categoryList.Contains(categoryId)) continue;
        categoryList.Add(categoryId);

        var shown = builtIn.ToString();
        try
        {
            var category = Category.GetCategory(doc, builtIn);
            if (category != null) shown = category.Name;
        }
        catch { }
        categoryName[categoryId] = shown;
    }
}

// THE CATEGORIES FROM THE ELEMENTS (version 5, from CREATE_VIEW_FILTERS_BY_
// VALUE), when perValue names none: the distinct categories they belong to.
var categoriesTyped = categoryList.Count > 0;
if (perValue && !categoriesTyped)
{
    foreach (var element in handed)
    {
        Category category = null;
        try { category = element.Category; } catch { category = null; }
        if (category == null || categoryList.Contains(category.Id)) continue;
        categoryList.Add(category.Id);
        categoryName[category.Id] = category.Name;
    }
}

// THE COLOUR, OR NONE (version 4). Three numbers 0 to 255, "255,0,0", or left
// out - which is "none", and goes with leaving the view out.
var colourWord = (colour ?? "").Trim();
var wantColour = colourWord.Length > 0
    && !string.Equals(colourWord, "none", StringComparison.OrdinalIgnoreCase);
Color paint = null;
var colourProblem = "";
if (wantColour)
{
    var channels = colourWord.Split(',').Select(c => c.Trim()).ToList();
    var bytes = new List<byte>();
    foreach (var channel in channels)
    {
        int number;
        if (int.TryParse(channel, System.Globalization.NumberStyles.Integer,
                         System.Globalization.CultureInfo.InvariantCulture, out number)
            && number >= 0 && number <= 255)
            bytes.Add((byte)number);
    }
    if (channels.Count == 3 && bytes.Count == 3) paint = new Color(bytes[0], bytes[1], bytes[2]);
    else colourProblem = "\"" + colourWord + "\" is not a colour - type three numbers from 0 to 255, "
        + "comma separated, \"255,0,0\" for red, or leave the colour out with the view";
}

// ---- what was asked for, checked before Revit is asked anything -----------

var repeated = "";
var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var oneName in names)
{
    if (oneName.Length > 0 && !seen.Add(oneName)) { repeated = oneName; break; }
}

var badName = "";
foreach (var oneName in names)
{
    if (oneName.Length > 0 && !NamingUtils.IsValidName(oneName)) { badName = oneName; break; }
}

if (view != null && view.IsTemplate)
{
    refused = "a filter is put on a real view - a template is not one. Leave the view out to make "
            + "the filter on no view";
}
else if (perValue)
{
    // ONE FILTER PER VALUE (version 5) - what it needs, checked first.
    if (handed.Count == 0)
        refused = "one filter per value reads the values off the elements, and none was handed in - select the "
                + "elements, or give the filters' names and values instead";
    else if (namesGiven || !noValueGiven)
        refused = "one filter per value names each filter after its value and reads the values off the elements - "
                + "leave filterName and matchValue out, and give namePrefix for the start of every name";
    else if (!string.IsNullOrWhiteSpace(moreRules))
        refused = "moreRules makes ONE filter that needs several rules to match, and one filter per value makes "
                + "several - make that filter in a call of its own; nothing was made";
    else if (wantColour || colourProblem.Length > 0)
        refused = "one filter per value gives EACH filter its own colour, stepped evenly round the hue wheel, so "
                + "one colour for all of them is refused - leave the colour out; nothing was made";
    else if (wanted.Length == 0)
        refused = "a filter needs a parameter to test, by the name Revit shows";
    else if (categoryList.Count == 0)
        refused = "none of the elements handed in belongs to a category, so there is nothing to filter";
}
else if (!namesGiven)
{
    refused = "a filter needs a name - it goes in the project's filter list, where other views will see it. "
            + "For one filter per value read off the elements, set perValue";
}
else if (noValueGiven)
{
    refused = "no value was given for the rule to match - type it, \"has no value\" to catch the elements whose "
            + "parameter is empty, or \"equals none\" to match the word none";
}
else if (view != null && !wantColour)
{
    refused = "a view was given and no colour - a filter put on a view here is coloured in the same "
            + "call, so give one (\"255,0,0\"), or leave the view out too and the filter is made on "
            + "no view. APPLY_VIEW_FILTER puts a filter that exists on a view with any settings";
}
else if (view == null && wantColour)
{
    refused = "a colour was given and no view to colour it in - give the view, or leave the colour "
            + "out too and the filter is made on no view";
}
else if (colourProblem.Length > 0)
{
    refused = colourProblem;
}
else if (names.Count == 0)
{
    refused = "a filter needs a name - it goes in the project's filter list, where "
            + "other views will see it";
}
else if (names.Count != values.Count)
{
    refused = string.Format("{0} filter name(s) and {1} value(s) were given - each filter needs "
        + "exactly one value, in the same order as the names", names.Count, values.Count);
}
else if (quoteProblem.Length > 0)
{
    refused = quoteProblem;
}
else if (names.Contains("") || values.Contains(""))
{
    refused = "a filter name or a value was blank - every filter needs both. To catch the elements "
            + "whose parameter is empty, say so: type the value as \"has no value\"";
}
else if (bareOperator.Length > 0)
{
    refused = string.Format("'{0}' is an operator with nothing after it - write what it should "
            + "match, \"{0} Level 1\", or \"equals {0}\" to match the word itself - nothing was made",
            bareOperator);
}
else if (noTextWithText.Length > 0)
{
    refused = string.Format("\"{0}\" - \"has no value\" and \"is blank\" take nothing after them; "
            + "they catch the elements whose parameter is empty. Type the operator alone, or \"equals "
            + "{0}\" to match those words - nothing was made", noTextWithText);
}
else if (Enumerable.Range(0, texts.Count).Any(i => texts[i] == "" && !noTextOperators.Contains(operators[i])))
{
    refused = "a value was an operator with nothing after it, or an empty pair of quotes - every "
            + "filter needs text to match, or \"has no value\" to catch an empty parameter";
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
else if (categoryList.Count == 0)
{
    refused = "a filter needs at least one category to look at";
}
else if (wanted.Length == 0)
{
    refused = "a filter needs a parameter to test, by the name Revit shows - without one it "
            + "matches everything in the category, which is what SET_CATEGORY_GRAPHICS is for";
}
else if (names.Count > 1 && !string.IsNullOrWhiteSpace(moreRules))
{
    refused = "moreRules makes ONE filter that needs several rules to match, and " + names.Count
            + " filter names were given - make that filter in a call of its own; nothing was made";
}

// A project or shared parameter is an element and names itself; a built-in
// one's id is its enum value, whose label is the same on every release, where
// the property that holds the number changed.
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
Func<ElementId, string, bool> answersTo = (id, paramName) => id != null
    && (string.Equals(labelOf(id), paramName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(builtInNameOf(id), paramName, StringComparison.OrdinalIgnoreCase));

// ---- each category, asked on its own (version 4) --------------------------
// Revit rejects a filter whose categories include ONE the parameter does not
// apply to, and rejects it whole - so every category is asked first, each that
// cannot be filtered by the parameter is NAMED, and nothing is made.

if (refused == null)
{
    ICollection<ElementId> filterable = null;
    try { filterable = ParameterFilterUtilities.GetAllFilterableCategories(); } catch { }

    var keptCategories = new List<ElementId>();
    foreach (var categoryId in categoryList)
    {
        var before = cannotFilter.Count;
        if (filterable != null && !filterable.Contains(categoryId))
        {
            cannotFilter.Add(string.Format("{0} - Revit cannot filter this category at all",
                categoryName[categoryId]));
            continue;
        }

        ICollection<ElementId> allowed = null;
        try
        {
            allowed = ParameterFilterUtilities.GetFilterableParametersInCommon(
                doc, new List<ElementId> { categoryId });
        }
        catch (Exception ex)
        {
            cannotFilter.Add(string.Format("{0} - Revit would not list what it can be filtered by: "
                + "{1}", categoryName[categoryId], ex.Message));
            continue;
        }

        if (allowed == null || !allowed.Any(id => answersTo(id, wanted)))
            cannotFilter.Add(string.Format("{0} - it cannot be filtered by '{1}'",
                categoryName[categoryId], wanted));
        if (cannotFilter.Count == before) keptCategories.Add(categoryId);
    }

    // A CATEGORY DERIVED FROM THE ELEMENTS IS DROPPED AND NAMED (version 5) -
    // nobody named it. A TYPED one stops the call, below, as it always did.
    if (perValue && !categoriesTyped && cannotFilter.Count > 0)
    {
        droppedCategories.AddRange(cannotFilter);
        cannotFilter.Clear();
        categoryList = keptCategories;
        if (categoryList.Count == 0)
            refused = string.Format("NOTHING WAS CREATED. None of the elements' categories can take a rule on '{0}': "
                + "{1}", wanted, string.Join("; ", droppedCategories));
    }

    if (cannotFilter.Count > 0)
    {
        refused = string.Format("NOTHING WAS CREATED. {0} of the {1} categories cannot take a rule on "
            + "'{2}': {3}. Revit rejects a filter whose categories include one the parameter does not "
            + "apply to, so take {4} out of the list and ask again",
            cannotFilter.Count, categoryList.Count, wanted, string.Join("; ", cannotFilter),
            cannotFilter.Count == 1 ? "it" : "them");
    }
}

// ---- one parameter the categories share, and the rules --------------------

ICollection<ElementId> shared = null;
if (refused == null)
{
    try { shared = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, categoryList); }
    catch (Exception ex) { refused = "Revit would not list the parameters these categories share: " + ex.Message; }
}
if (shared == null) shared = new List<ElementId>();

// The ONE parameter a name means, or the reason it means none or two.
string parameterProblem = null;
Func<string, ElementId> parameterCalled = paramName =>
{
    parameterProblem = null;
    var found = shared.Where(id => answersTo(id, paramName)).ToList();
    if (found.Count == 1) return found[0];
    if (found.Count == 0)
    {
        parameterProblem = "no parameter the filter's categories share is called \"" + paramName
            + "\" - every category can be filtered by one of that name, but not by the SAME one";
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

// A text rule, from the factory for its operator in whichever spelling this
// release has.
string factoryProblem = null;
Func<ElementId, string, string, FilterRule> textRule = (pid, op, ruleValue) =>
{
    factoryProblem = null;
    var method = operatorMethods[op];

    // "has no value" takes the parameter alone; the factory is in every
    // release this card claims, so it is called by name, not reflected.
    if (noTextOperators.Contains(op))
    {
        try
        {
            var empty = ParameterFilterRuleFactory.CreateHasNoValueParameterRule(pid);
            if (empty == null) factoryProblem = "Revit built no rule 'has no value'";
            return empty;
        }
        catch (Exception ex)
        {
            factoryProblem = "Revit would not build the rule 'has no value': " + ex.Message;
            return null;
        }
    }

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
            ? withoutFlag.Invoke(null, new object[] { pid, ruleValue }) as FilterRule
            : withFlag.Invoke(null, new object[] { pid, ruleValue, false }) as FilterRule;
        if (made == null) factoryProblem = "Revit built no rule '" + op + " " + ruleValue + "'";
        return made;
    }
    catch (Exception ex)
    {
        factoryProblem = "Revit would not build the rule '" + op + " " + ruleValue + "': "
            + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
        return null;
    }
};

ElementId firstParameter = null;
string ruleProblem = null;
if (refused == null)
{
    firstParameter = parameterCalled(wanted);
    if (firstParameter == null) ruleProblem = parameterProblem;
}

// ---- ONE FILTER PER VALUE: THE VALUES, READ OFF THE ELEMENTS (version 5) ----
// The parameter the categories share, read on each element - or on its type,
// for a type parameter - by its id, never by a name lookup that can pick one
// of two at random. An element carrying the name twice is left out.

if (perValue && refused == null && ruleProblem == null && firstParameter != null)
{
    var builtInName = builtInNameOf(firstParameter);
    var label = labelOf(firstParameter);
    ParameterElement asElement = doc.GetElement(firstParameter) as ParameterElement;
    Func<Element, Parameter> parameterOn = holder =>
    {
        if (holder == null) return null;
        try
        {
            if (builtInName.Length > 0)
                return holder.get_Parameter((BuiltInParameter)(int)long.Parse(firstParameter.ToString()));
            if (asElement != null && asElement.GetDefinition() != null) return holder.get_Parameter(asElement.GetDefinition());
        }
        catch { }
        return null;
    };

    var kept = new HashSet<ElementId>(categoryList);
    var byValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var hasBlank = false;
    int outside = 0, twice = 0, without = 0;
    foreach (var element in handed)
    {
        Category category = null;
        try { category = element.Category; } catch { category = null; }
        if (category == null || !kept.Contains(category.Id)) { outside++; continue; }
        if (label.Length > 0 && element.GetParameters(label).Count > 1) { twice++; continue; }

        var parameter = parameterOn(element);
        if (parameter == null)
        {
            Element type = null;
            try { type = doc.GetElement(element.GetTypeId()); } catch { type = null; }
            parameter = parameterOn(type);
        }
        if (parameter == null) { without++; continue; }

        var text = "";
        try { text = parameter.StorageType == StorageType.String ? parameter.AsString() ?? "" : parameter.AsValueString() ?? ""; }
        catch { text = ""; }
        text = text.Trim();
        if (text.Length == 0) { hasBlank = true; continue; }
        if (!byValue.ContainsKey(text)) byValue[text] = text;
    }

    if (outside > 0)
        leftOut.Add(string.Format("{0} element(s) are in no category the filters are made for - {1} - and gave no "
            + "value", outside, categoriesTyped ? "outside the categories named" : "a category that cannot take the rule"));
    if (twice > 0)
        leftOut.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}' - usually a shared or "
            + "project parameter bound beside a built-in one of the same name - and were left out. Revit would pick "
            + "one of them at random, and a filter is built on one parameter", twice, label));
    if (without > 0)
        leftOut.Add(string.Format("{0} element(s) have no '{1}' on themselves or their type and gave no value",
            without, label));

    var ordered = new List<string>(byValue.Values);
    ordered.Sort(StringComparer.OrdinalIgnoreCase);
    foreach (var value in ordered)
    {
        valuesFound.Add(value);
        names.Add(prefix + value);
        values.Add(value);
        operators.Add("equals");
        texts.Add(value);
    }
    if (hasBlank)
    {
        // THE BLANK GROUP, ITS OWN FILTER, on this card's "has no value" rule.
        valuesFound.Add("(no value)");
        names.Add(prefix + "(no value)");
        values.Add("has no value");
        operators.Add("has no value");
        texts.Add("");
    }

    if (names.Count == 0)
        ruleProblem = "none of the elements handed in gave a value of '" + wanted + "'";
    else
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var oneName in names)
        {
            if (!NamingUtils.IsValidName(oneName))
            {
                ruleProblem = string.Format("'{0}' cannot be a filter name - Revit does not allow characters such as "
                    + ": ; [ ] {{ }} | < > ? ` ~ in a name, and a value named it. No filter is made until every name "
                    + "can be", oneName);
                break;
            }
            if (!seenNames.Add(oneName))
            {
                ruleProblem = string.Format("'{0}' would be two filters' name - two values differ only in capitals "
                    + "or spaces", oneName);
                break;
            }
        }
    }
}

// The extra rules of ONE filter, read before anything is made. The operator
// is the one that starts EARLIEST in the text - the parameter's name comes
// first - and the longest of those that start there.
var extra = new List<FilterRule>();
var how = (combine ?? "").Trim().ToLowerInvariant();
if (refused == null && ruleProblem == null && how != "" && how != "and" && how != "or")
    ruleProblem = "\"" + combine + "\" is not and or or";
if (refused == null && ruleProblem == null && !string.IsNullOrWhiteSpace(moreRules))
{
    var moreOperators = operatorWords.Concat(new[] { "!=", "=" }).ToArray();
    foreach (var ruleText in moreRules.Split(';'))
    {
        if (ruleText.Trim() == "") continue;
        string op = null;
        var at = -1;
        foreach (var candidate in moreOperators)
        {
            var position = ruleText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
            if (position <= 0) continue;
            if (op == null || position < at || (position == at && candidate.Length > op.Length))
            {
                op = candidate;
                at = position;
            }
        }
        if (op == null) { ruleProblem = "\"" + ruleText.Trim() + "\" has no operator (=, !=, " + string.Join(", ", operatorWords) + ")"; break; }
        var extraName = ruleText.Substring(0, at).Trim();
        var extraValue = unquoted(ruleText.Substring(at + op.Length).Trim());
        if (noTextOperators.Contains(op) && extraValue.Length > 0)
        {
            ruleProblem = "\"" + ruleText.Trim() + "\" - \"" + op + "\" takes nothing after it";
            break;
        }
        var pid = parameterCalled(extraName);
        if (pid == null) { ruleProblem = parameterProblem; break; }
        var extraRule = textRule(pid, op, extraValue);
        if (extraRule == null) { ruleProblem = factoryProblem; break; }
        extra.Add(extraRule);
    }
}

// ---- every filter's test, built and checked before any filter exists ------
// Filter names already in the project - FilterElement, so a SELECTION filter
// of the same name is seen too; both live in one list, and Revit throws on the
// duplicate. A name taken is SKIPPED AND NAMED, never overwritten, and the
// others are still made (version 4).

var toCreate = new List<int>();
var tests = new Dictionary<int, ElementFilter>();

if (refused == null && ruleProblem == null)
{
    var existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
    {
        var held = element.Name ?? "";
        if (!existing.ContainsKey(held)) existing[held] = held;
    }

    var categorySet = new HashSet<ElementId>(categoryList);

    for (var index = 0; index < names.Count && ruleProblem == null; index++)
    {
        if (existing.ContainsKey(names[index]))
        {
            alreadyExists.Add(existing[names[index]] == names[index]
                ? names[index]
                : string.Format("{0} (held as '{1}')", names[index], existing[names[index]]));
            continue;
        }

        var rule = textRule(firstParameter, operators[index], texts[index]);
        if (rule == null) { ruleProblem = factoryProblem; break; }

        ElementFilter test;
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
                doc, categorySet, test);
        }
        catch (Exception ex) { why = " - " + ex.Message; }
        if (!acceptable)
        {
            ruleProblem = string.Format("Revit says the rule '{0}' {1} '{2}' cannot be used on these "
                + "categories{3}. A text rule on a parameter that holds a number is the usual reason",
                wanted, operators[index], texts[index], why);
            break;
        }

        toCreate.Add(index);
        tests[index] = test;
    }
}

if (refused == null && ruleProblem != null)
{
    refused = ruleProblem + " - nothing was made";
}

// ---- create, put on the view when one was given, then read each back -------

if (refused == null)
{
    var lines = new List<string>();

    foreach (var index in toCreate)
    {
        // Everything that could be checked has been. A failure here ends the
        // run and Heron rolls the whole call back, so a half-made set of
        // filters is never left behind.
        var filter = ParameterFilterElement.Create(doc, names[index], categoryList, tests[index]);
        if (filter == null)
            throw new InvalidOperationException("Revit declined to create the filter '" + names[index]
                + "' - the whole call is rolled back, so nothing was kept");

        createdIds.Add(filter.Id);
        if (created == null) created = filter.Id;

        if (view != null)
        {
            view.AddFilter(filter.Id);

            // ONE COLOUR PER FILTER, stepped evenly round the hue wheel
            // (version 5, from CREATE_VIEW_FILTERS_BY_VALUE) - a fixed palette
            // of six repeats on the seventh. The typed road keeps its colour.
            if (perValue)
            {
                var step = toCreate.IndexOf(index);
                var hue = (360.0 * step) / Math.Max(toCreate.Count, 1);
                var sector = (int)(hue / 60.0) % 6;
                var fraction = (hue / 60.0) - Math.Floor(hue / 60.0);
                byte high = 235;
                byte low = 60;
                byte rising = (byte)(low + (high - low) * fraction);
                byte falling = (byte)(high - (high - low) * fraction);
                byte red = high, green = high, blue = high;
                if (sector == 0) { red = high; green = rising; blue = low; }
                else if (sector == 1) { red = falling; green = high; blue = low; }
                else if (sector == 2) { red = low; green = high; blue = rising; }
                else if (sector == 3) { red = low; green = falling; blue = high; }
                else if (sector == 4) { red = rising; green = low; blue = high; }
                else { red = high; green = low; blue = falling; }
                paint = new Color(red, green, blue);
            }

            // Projection AND cut, because a duct in a plan is cut by the view
            // range and a duct in a 3D view is not. Setting only one leaves
            // half the drawing uncoloured, and which half depends on the view.
            var look = new OverrideGraphicSettings();
            look.SetProjectionLineColor(paint);
            look.SetCutLineColor(paint);
            look.SetSurfaceForegroundPatternColor(paint);
            look.SetCutForegroundPatternColor(paint);

            view.SetFilterOverrides(filter.Id, look);
        }

        // THE READ-BACK IS WHAT REVIT HOLDS, NOT AN ECHO OF THE REQUEST
        // (version 4, from CREATE_VIEW_FILTERS_BY_RULE): the categories and
        // rules read out of the filter after it exists.
        var heldCategories = new List<string>();
        foreach (var heldId in filter.GetCategories())
        {
            var label = heldId.ToString();
            try
            {
                var category = Category.GetCategory(doc, heldId);
                if (category != null) label = category.Name;
            }
            catch { }
            heldCategories.Add(label);
        }
        heldCategories.Sort(StringComparer.OrdinalIgnoreCase);

        // The rules, walked without recursion - a logical filter holds others.
        var heldRules = new List<string>();
        var heldFilter = filter.GetElementFilter();
        var pending = new List<ElementFilter>();
        if (heldFilter != null) pending.Add(heldFilter);
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
                try { parameterText = labelOf(heldRule.GetRuleParameter()); } catch { }

                var heldText = heldRule as FilterStringRule;
                if (heldText != null)
                {
                    var evaluator = heldText.GetEvaluator();
                    var verb = evaluator == null ? "?" : evaluator.GetType().Name;
                    if (verb == "FilterStringEquals") verb = inverted ? "does not equal" : "equals";
                    else if (verb == "FilterStringContains") verb = inverted ? "does not contain" : "contains";
                    else if (verb == "FilterStringBeginsWith") verb = inverted ? "does not begin with" : "begins with";
                    else if (verb == "FilterStringEndsWith") verb = inverted ? "does not end with" : "ends with";
                    else if (inverted) verb = "NOT " + verb;
                    heldRules.Add(string.Format("{0} {1} '{2}'", parameterText, verb, heldText.RuleString));
                }
                else if (heldRule is HasNoValueFilterRule)
                {
                    heldRules.Add(string.Format("{0} {1}", parameterText,
                        inverted ? "has a value" : "has no value"));
                }
                else if (heldRule is HasValueFilterRule)
                {
                    heldRules.Add(string.Format("{0} {1}", parameterText,
                        inverted ? "has no value" : "has a value"));
                }
                else
                {
                    heldRules.Add(string.Format("{0} ({1}{2})", parameterText, inverted ? "NOT " : "",
                        heldRule.GetType().Name));
                }
            }
        }

        // What the rule, as Revit holds it, catches in the model today.
        var perCategory = new List<string>();
        var total = 0;
        if (heldFilter != null)
        {
            foreach (var categoryId in categoryList)
            {
                var count = new FilteredElementCollector(doc)
                    .WherePasses(new ElementMulticategoryFilter(new List<ElementId> { categoryId }))
                    .WhereElementIsNotElementType()
                    .WherePasses(heldFilter)
                    .GetElementCount();
                total += count;
                if (count > 0) perCategory.Add(string.Format("{0} {1}", categoryName[categoryId], count));
            }
        }

        // Where it is, read back from the view rather than assumed.
        var placed = "on no view";
        if (view != null)
        {
            var onView = false;
            try { onView = view.GetFilters().Contains(filter.Id); } catch { }
            var lineColour = "none";
            try
            {
                var c = view.GetFilterOverrides(filter.Id).ProjectionLineColor;
                if (c != null && c.IsValid) lineColour = string.Format("{0},{1},{2}", c.Red, c.Green, c.Blue);
            }
            catch { }
            placed = onView
                ? string.Format("on '{0}', line colour read back {1}", view.Name, lineColour)
                : string.Format("NOT on '{0}' - Revit did not keep it there", view.Name);
        }

        lines.Add(string.Format("'{0}': categories {1}; rule {2}; catches {3} element(s) in the model "
            + "today{4}; {5}", filter.Name, string.Join(", ", heldCategories),
            heldRules.Count == 0 ? "NONE"
                : string.Join(heldFilter is LogicalOrFilter ? " OR " : " AND ", heldRules), total,
            perCategory.Count == 0 ? "" : " (" + string.Join(", ", perCategory) + ")", placed));
    }

    if (lines.Count > 0)
    {
        readBack = string.Join(" | ", lines)
            + (view == null ? " | none of them is on a view yet - APPLY_VIEW_FILTER puts one on a view" : "");
    }

    if (alreadyExists.Count > 0)
    {
        refused = string.Format("{0} name(s) already a filter in this project and left exactly as "
            + "they were, not overwritten - filters are project-wide, so another view may be relying "
            + "on one: {1}", alreadyExists.Count, string.Join(", ", alreadyExists));
    }
}

// VERSION 5: what one filter per value read, said.
if (perValue && (createdIds.Count > 0 || refused == null) && (valuesFound.Count > 0 || droppedCategories.Count > 0 || leftOut.Count > 0))
{
    var said = new List<string>();
    if (valuesFound.Count > 0)
        said.Add(string.Format("{0} distinct value(s) of '{1}' read off the elements: {2}. These are FILTERS, not "
            + "overrides - anything modelled tomorrow that matches a rule is caught", valuesFound.Count, wanted,
            string.Join(", ", valuesFound)));
    if (droppedCategories.Count > 0)
        said.Add("left out of the filters' categories, because Revit would reject every filter with them in: "
            + string.Join("; ", droppedCategories));
    said.AddRange(leftOut);
    readBack = readBack.Length == 0 ? string.Join(" | ", said) : string.Join(" | ", said) + " | " + readBack;
}
