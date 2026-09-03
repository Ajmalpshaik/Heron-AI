// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `parameterName` and
// `namePrefix` are in scope; leaves `created`, `valuesFound` and `refused`
// behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THIS IS NOT COLOR_BY_PARAMETER. That writes per-element overrides - right for
// looking at something now, and an element drawn tomorrow gets nothing. This
// writes real filters, which re-evaluate themselves forever.
//
// THE FILTER'S CATEGORY SET COMES FROM THE ELEMENTS. A filter is only valid for
// categories that actually carry the parameter; hand it one that does not and
// Revit rejects the WHOLE filter. So the categories are collected from the
// elements that really resolved the parameter, and nothing else.
//
// THE RULE FACTORY IS REACHED BY REFLECTION, for a reason read off the
// assemblies: the TEXT overload of CreateEqualsRule is
// (ElementId, string, bool) on Revit 2020 and (ElementId, string) by 2027 - the
// case-sensitivity argument was dropped. EACH SPELLING COMPILES ON EXACTLY ONE
// END AND BREAKS THE OTHER, so the overload is picked at run time by its
// argument count.

var created = new List<Element>();
var valuesFound = new List<string>();
var refused = new List<string>();

// Group the elements by their value of the parameter, and remember which
// categories genuinely resolved it.
var byValue = new Dictionary<string, List<Element>>();
var categories = new List<ElementId>();
ElementId parameterId = ElementId.InvalidElementId;
var withoutParameter = 0;

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    var parameter = element.LookupParameter(parameterName);
    if (parameter == null) { withoutParameter++; continue; }

    if (parameterId == ElementId.InvalidElementId) parameterId = parameter.Id;

    var text = "";
    try
    {
        if (parameter.StorageType == StorageType.String) text = parameter.AsString() ?? "";
        else text = parameter.AsValueString() ?? "";
    }
    catch { }

    var key = string.IsNullOrWhiteSpace(text) ? "(no value)" : text;
    if (!byValue.ContainsKey(key)) byValue[key] = new List<Element>();
    byValue[key].Add(element);

    if (element.Category != null && !categories.Contains(element.Category.Id))
    {
        categories.Add(element.Category.Id);
    }
}

if (withoutParameter > 0)
{
    refused.Add(string.Format("{0} element(s) do not carry '{1}' and were left out of the category set. "
        + "A filter naming a category that lacks the parameter is rejected by Revit ENTIRELY, so they "
        + "cannot simply be included", withoutParameter, parameterName));
}

if (parameterId == ElementId.InvalidElementId)
{
    refused.Add(string.Format("no element carries a parameter called '{0}' - nothing to build filters "
        + "from", parameterName));
}
else if (categories.Count == 0)
{
    refused.Add("no category resolved the parameter - a filter needs at least one");
}
else
{
    // The reflection, resolved once. See the header for why.
    var factory = typeof(ParameterFilterRuleFactory);
    System.Reflection.MethodInfo equalsRule = null;
    var wantsCaseFlag = false;

    foreach (var candidate in factory.GetMethods())
    {
        if (candidate.Name != "CreateEqualsRule") continue;
        var arguments = candidate.GetParameters();
        if (arguments.Length < 2 || arguments[1].ParameterType != typeof(string)) continue;
        equalsRule = candidate;
        wantsCaseFlag = arguments.Length == 3;
        break;
    }

    if (equalsRule == null)
    {
        refused.Add("this Revit version exposes no text rule this recognises - no filter was created");
    }
    else
    {
        var ordered = new List<string>(byValue.Keys);
        ordered.Sort(StringComparer.OrdinalIgnoreCase);
        valuesFound.AddRange(ordered);

        // Even hue steps, so every group is distinct however many there are. A
        // fixed palette of six repeats on the seventh and two runs look like one.
        var count = ordered.Count;

        for (var index = 0; index < count; index++)
        {
            var value = ordered[index];
            var filterName = string.Format("{0}{1}", namePrefix, value);

            // A name Revit already holds is refused by throwing, so it is checked.
            var clash = false;
            foreach (var existing in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)))
            {
                if (string.Equals(existing.Name, filterName, StringComparison.OrdinalIgnoreCase))
                {
                    clash = true;
                    break;
                }
            }
            if (clash)
            {
                refused.Add(string.Format("'{0}' already exists as a filter - skipped rather than "
                    + "overwritten", filterName));
                continue;
            }

            try
            {
                var matchText = value == "(no value)" ? "" : value;
                var arguments = wantsCaseFlag
                    ? new object[] { parameterId, matchText, false }
                    : new object[] { parameterId, matchText };
                var rule = equalsRule.Invoke(null, arguments) as FilterRule;

                var elementFilter = new ElementParameterFilter(rule);
                var filter = ParameterFilterElement.Create(doc, filterName, categories);
                filter.SetElementFilter(elementFilter);

                view.AddFilter(filter.Id);

                // Hue stepped evenly round the wheel, converted to RGB.
                var hue = (360.0 * index) / Math.Max(count, 1);
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

                var settings = new OverrideGraphicSettings();
                settings.SetProjectionLineColor(new Color(red, green, blue));
                settings.SetCutLineColor(new Color(red, green, blue));
                view.SetFilterOverrides(filter.Id, settings);

                created.Add(filter);
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' could not be created - {1}", filterName, ex.Message));
            }
        }

        refused.Add(string.Format("{0} distinct value(s) of '{1}' across {2} category(ies). These are "
            + "FILTERS, not overrides - anything modelled tomorrow that matches a rule is caught "
            + "automatically", ordered.Count, parameterName, categories.Count));
    }
}
