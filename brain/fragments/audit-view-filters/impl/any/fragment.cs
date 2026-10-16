// NOT STANDALONE. Assumes `doc`, `view` and `everyFilter` are in scope, and
// leaves `findings`, `disabled`, `hidden`, `blankOverride`, `nothingToActOn`,
// `templateOwned`, `filterCount` and `filterList` behind.
//
// VERSION 2 (2026-10-15) ADDS EVERY FILTER IN FULL, AND ONLY WHEN ASKED - see
// the block at the end. `everyFilter` left out is false, `filterList` stays
// empty, and every line above that block is version 1's, so the audit answers
// exactly as it did.
//
// READ ONLY. Opens no transaction and needs none - including for the disabled
// filters it finds. Switching one back on is a write, and a write hidden
// inside an audit is how a diagnosis becomes a change nobody asked for.
//
// FOUR STATES, ONE SYMPTOM.
//
// "The filter is on the view and nothing is coloured" has four unrelated
// causes and Revit flags none of them:
//
//   switched off       still listed, greyed, doing nothing
//   visibility off     it HIDES what it matches instead of colouring it -
//                      a different switch from the overrides, same list
//   categories off     the filter only ever acts on what the view is already
//                      drawing. All categories hidden here means a correct
//                      filter with nothing to act on
//   empty override     adding a filter without setting one leaves a blank
//                      override: present, enabled, matching, and identical to
//                      having no filter
//
// THE TEMPLATE IS REPORTED FIRST BECAUSE IT EXPLAINS THE REST AT ONCE.
//
// Where a template controls the filters slot, everything set on the view is
// discarded when the template reasserts. Saying it once at the top is the
// difference between one answer and a list of rows nobody can act on.
//
// A VIEW THAT CANNOT CARRY OVERRIDES AT ALL is said to be one. "0 filters" on
// a schedule is true and useless.

// WHAT COUNTS AS A BLANK OVERRIDE.
//
// There is no "is this override empty" call, so every slot an override can
// carry is asked and the answer is "blank" only when NONE of them is set. Read
// the other way round - looking for one particular thing being set - a filter
// carrying only a halftone or only a transparency would be reported as blank,
// which is a wrong finding on a filter that is working.
Func<OverrideGraphicSettings, bool> isBlank = settings =>
{
    try
    {
        if (settings.Halftone) return false;
        if (settings.Transparency > 0) return false;
        if (settings.DetailLevel != ViewDetailLevel.Undefined) return false;

        if (settings.ProjectionLineColor != null && settings.ProjectionLineColor.IsValid) return false;
        if (settings.CutLineColor != null && settings.CutLineColor.IsValid) return false;
        if (settings.SurfaceForegroundPatternColor != null && settings.SurfaceForegroundPatternColor.IsValid) return false;
        if (settings.SurfaceBackgroundPatternColor != null && settings.SurfaceBackgroundPatternColor.IsValid) return false;
        if (settings.CutForegroundPatternColor != null && settings.CutForegroundPatternColor.IsValid) return false;
        if (settings.CutBackgroundPatternColor != null && settings.CutBackgroundPatternColor.IsValid) return false;

        if (settings.ProjectionLineWeight > 0) return false;
        if (settings.CutLineWeight > 0) return false;

        if (settings.ProjectionLinePatternId != ElementId.InvalidElementId) return false;
        if (settings.CutLinePatternId != ElementId.InvalidElementId) return false;
        if (settings.SurfaceForegroundPatternId != ElementId.InvalidElementId) return false;
        if (settings.SurfaceBackgroundPatternId != ElementId.InvalidElementId) return false;
        if (settings.CutForegroundPatternId != ElementId.InvalidElementId) return false;
        if (settings.CutBackgroundPatternId != ElementId.InvalidElementId) return false;
    }
    catch
    {
        // A slot that cannot be read is not evidence of blankness. Saying "not
        // blank" here means the audit stays quiet rather than reporting a
        // finding it cannot stand up.
        return false;
    }
    return true;
};

var findings = new List<string>();
var disabled = new List<ElementId>();
var hidden = new List<ElementId>();
var blankOverride = new List<ElementId>();
var nothingToActOn = new List<ElementId>();
bool templateOwned = false;
int filterCount = 0;

bool overridesAllowed = true;
try { overridesAllowed = view.AreGraphicsOverridesAllowed(); } catch { }

if (!overridesAllowed)
{
    findings.Add("'" + view.Name + "' (" + view.ViewType.ToString() +
                 ") does not accept graphic overrides at all, so no filter can " +
                 "apply here. There is nothing to audit - this is not the same " +
                 "as a view with no filters.");
}
else
{
    List<ElementId> filterIds = new List<ElementId>();
    try { filterIds = view.GetFilters().ToList(); } catch { }
    filterCount = filterIds.Count;

    // Which template parameters the view still controls itself. Anything NOT
    // in that list belongs to the template, and the filters slot is one entry
    // in it.
    try
    {
        if (!view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
        {
            var selfControlled = new HashSet<ElementId>(view.GetNonControlledTemplateParameterIds());
            templateOwned = !selfControlled.Contains(new ElementId(BuiltInParameter.VIS_GRAPHICS_FILTERS));

            var template = doc.GetElement(view.ViewTemplateId) as View;
            if (templateOwned)
                findings.Add("The view template '" + (template == null ? "?" : template.Name) +
                             "' owns the filters on this view. Anything changed here is " +
                             "discarded when the template reasserts - change it ON THE TEMPLATE.");
        }
    }
    catch { }

#if REVIT2020
    if (filterCount > 0)
        findings.Add("This release has no enabled/disabled switch for view filters, " +
                     "so that check was SKIPPED, not passed.");
#endif

    foreach (var filterId in filterIds)
    {
        var filter = doc.GetElement(filterId);
        string name = filter == null ? filterId.ToString() : filter.Name;

#if !REVIT2020
        // The one that wastes the afternoon: listed, greyed, doing nothing.
        bool enabled = true;
        try { enabled = view.GetIsFilterEnabled(filterId); } catch { }
        if (!enabled)
        {
            disabled.Add(filterId);
            findings.Add("'" + name + "' is APPLIED BUT SWITCHED OFF on this view. " +
                         "It is in the list and it does nothing.");
        }
#endif

        // Visibility is a separate switch from the overrides, and off means
        // the matches are HIDDEN rather than coloured.
        bool visible = true;
        try { visible = view.GetFilterVisibility(filterId); } catch { }
        if (!visible)
        {
            hidden.Add(filterId);
            findings.Add("'" + name + "' has its VISIBILITY off, so everything it " +
                         "matches is hidden in this view rather than coloured.");
        }

        // A blank override is invisible in every sense: present, enabled,
        // matching, and identical to having no filter at all.
        try
        {
            var settings = view.GetFilterOverrides(filterId);
            if (settings != null && isBlank(settings))
            {
                blankOverride.Add(filterId);
                findings.Add("'" + name + "' has an EMPTY override - nothing is set on it. " +
                             "Adding a filter without setting one leaves exactly this, and it " +
                             "looks identical to having no filter.");
            }
        }
        catch { }

        // A filter only ever acts on what the view already draws. Every
        // category hidden here means a correct filter with nothing to act on -
        // and the fix is the category, not the rule.
        try
        {
            var rule = filter as ParameterFilterElement;
            if (rule != null)
            {
                var categories = rule.GetCategories();
                if (categories != null && categories.Count > 0)
                {
                    bool anyDrawn = false;
                    foreach (var categoryId in categories)
                    {
                        bool categoryHidden = false;
                        try { categoryHidden = view.GetCategoryHidden(categoryId); } catch { }
                        if (!categoryHidden) { anyDrawn = true; break; }
                    }
                    if (!anyDrawn)
                    {
                        nothingToActOn.Add(filterId);
                        findings.Add("'" + name + "' has every one of its categories switched off " +
                                     "in this view, so it has nothing to act on. The rule is fine; " +
                                     "the category visibility is what is hiding the result.");
                    }
                }
            }
        }
        catch { }
    }

    if (filterCount == 0)
        findings.Add("No filters are applied to '" + view.Name + "' at all.");
}

// ---- VERSION 2: EVERY FILTER IN FULL, WHEN ASKED ----------------------------
//
// WHY. Measured 2026-10-15 on a real HVAC project on Revit 2020, 21 filters on
// one plan: this audit answered in counts and in lists a reply cuts to three
// items of sixty characters, and nothing in the library printed a filter's
// RULE, its categories, what it catches here and its override together. Two
// more ways a filter can be present and do nothing need exactly those:
//
//   catches nothing    the rule is right for some other drawing, and nothing
//                      in THIS view passes it
//   outranked          a filter HIGHER in the list sets the same thing on the
//                      same elements, and the higher one wins
//
// So `everyFilter` leaves `filterList`: ONE STRING, a heading line and then
// one row per filter in priority order - Enable Filter, Visibility, the
// categories, the rule (logical filters walked, an inverse rule written NOT),
// how many host elements this view draws that it catches, and every override
// cell that is set.
//
// THE ORDER IS READ BY NAME. `GetOrderedFilters` arrived at Revit 2021; on
// 2020 the list is in the order `GetFilters` returns, and the heading says that
// release cannot give the dialog's order. `GetIsFilterEnabled` is reached the
// same way, as REPORT_VIEW_FILTERS does - by name, never under a version #if.
//
// THE OVERRIDE WORDS MATCH REPORT_CATEGORY_OVERRIDES' `allCategories` rows
// cell for cell, so a filter's row and a category's row compare directly.
//
// A COUNT IS OF HOST ELEMENTS THIS VIEW DRAWS. A filter also acts on linked
// models drawn By Host View, and those are not counted - said in the heading,
// so "catches 0" on a grey-out filter meant for the links reads as what it is.

var filterList = "";

if (everyFilter && overridesAllowed)
{
    var solidLinePattern = LinePatternElement.GetSolidPatternId();

    Func<Color, string> rgbOf = colour => colour != null && colour.IsValid
        ? string.Format("{0},{1},{2}", colour.Red, colour.Green, colour.Blue) : "";

    Func<ElementId, string> patternOf = patternId =>
    {
        if (patternId == null || patternId == ElementId.InvalidElementId) return "";
        if (patternId == solidLinePattern) return "Solid";
        var patternElement = doc.GetElement(patternId);
        return patternElement == null ? "pattern " + patternId : patternElement.Name;
    };

    // One cell of the dialog - what is set in it, or nothing at all.
    Func<string, string, string, int, bool, string> cellOf = (column, pattern, colour, weight, shown) =>
    {
        var bits = new List<string>();
        if (pattern.Length > 0) bits.Add("pattern " + pattern);
        if (colour.Length > 0) bits.Add("colour " + colour);
        if (weight > 0) bits.Add("weight " + weight);
        if (!shown) bits.Add("Visible UNTICKED");
        return bits.Count == 0 ? "" : column + " " + string.Join(", ", bits);
    };

    // Every override cell that is set, "; " between cells. Empty when nothing is.
    Func<OverrideGraphicSettings, string> overrideOf = read =>
    {
        if (read == null) return "";
        var cells = new List<string>
        {
            cellOf("projection lines", patternOf(read.ProjectionLinePatternId), rgbOf(read.ProjectionLineColor),
                read.ProjectionLineWeight, true),
            cellOf("surface foreground", patternOf(read.SurfaceForegroundPatternId),
                rgbOf(read.SurfaceForegroundPatternColor), 0, read.IsSurfaceForegroundPatternVisible),
            cellOf("surface background", patternOf(read.SurfaceBackgroundPatternId),
                rgbOf(read.SurfaceBackgroundPatternColor), 0, read.IsSurfaceBackgroundPatternVisible),
            cellOf("cut lines", patternOf(read.CutLinePatternId), rgbOf(read.CutLineColor), read.CutLineWeight, true),
            cellOf("cut foreground", patternOf(read.CutForegroundPatternId), rgbOf(read.CutForegroundPatternColor),
                0, read.IsCutForegroundPatternVisible),
            cellOf("cut background", patternOf(read.CutBackgroundPatternId), rgbOf(read.CutBackgroundPatternColor),
                0, read.IsCutBackgroundPatternVisible)
        };
        if (read.Halftone) cells.Add("halftone");
        if (read.Transparency > 0) cells.Add("transparency " + read.Transparency + "%");
        if (read.DetailLevel != ViewDetailLevel.Undefined) cells.Add("detail level " + read.DetailLevel);
        return string.Join("; ", cells.Where(text => text.Length > 0));
    };

    // A rule's parameter by the name the Filters dialog shows. A project or
    // shared parameter is an element; a built-in one is matched against
    // `new ElementId(BuiltInParameter)`, built once and only if needed - the
    // number inside an ElementId changes type at 2024, so it is never read.
    var parameterNames = new Dictionary<ElementId, string>();
    Dictionary<ElementId, BuiltInParameter> builtIns = null;
    Func<ElementId, string> parameterOf = parameterId =>
    {
        if (parameterId == null || parameterId == ElementId.InvalidElementId) return "?";
        string known;
        if (parameterNames.TryGetValue(parameterId, out known)) return known;

        known = null;
        try
        {
            var definition = doc.GetElement(parameterId) as ParameterElement;
            if (definition != null && definition.GetDefinition() != null) known = definition.GetDefinition().Name;
        }
        catch (Exception) { known = null; }

        if (known == null)
        {
            if (builtIns == null)
            {
                builtIns = new Dictionary<ElementId, BuiltInParameter>();
                foreach (BuiltInParameter candidate in Enum.GetValues(typeof(BuiltInParameter)))
                {
                    ElementId candidateId;
                    try { candidateId = new ElementId(candidate); }
                    catch (Exception) { continue; }
                    if (!builtIns.ContainsKey(candidateId)) builtIns[candidateId] = candidate;
                }
            }
            BuiltInParameter matched;
            if (builtIns.TryGetValue(parameterId, out matched))
            {
                try { known = LabelUtils.GetLabelFor(matched); }
                catch (Exception) { known = matched.ToString(); }
            }
        }

        if (known == null) known = "parameter " + parameterId;
        parameterNames[parameterId] = known;
        return known;
    };

    // "FilterStringBeginsWith" -> "begins with", as the Filters dialog says it.
    Func<object, string> verbOf = evaluator =>
    {
        if (evaluator == null) return "?";
        var raw = evaluator.GetType().Name.Replace("FilterString", "").Replace("FilterNumeric", "");
        switch (raw)
        {
            case "Equals": return "equals";
            case "BeginsWith": return "begins with";
            case "EndsWith": return "ends with";
            case "Contains": return "contains";
            case "Greater": return "is greater than";
            case "GreaterOrEqual": return "is greater than or equal to";
            case "Less": return "is less than";
            case "LessOrEqual": return "is less than or equal to";
            default: return raw;
        }
    };

    Func<ICollection<ElementId>, string> categoryNamesOf = categoryIds =>
    {
        var categoryNames = new List<string>();
        foreach (var categoryId in categoryIds)
        {
            var label = categoryId.ToString();
            try
            {
                var category = Category.GetCategory(doc, categoryId);
                if (category != null) label = category.Name;
            }
            catch (Exception) { }
            categoryNames.Add(label);
        }
        categoryNames.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", categoryNames);
    };

    // ONE RULE, as the dialog reads it. An inverse rule is "does not ..." in
    // the dialog and NOT here.
    Func<FilterRule, string> ruleOf = null;
    ruleOf = rule =>
    {
        if (rule == null) return "?";
        var inverse = rule as FilterInverseRule;
        if (inverse != null) return "NOT " + ruleOf(inverse.GetInnerRule());

        var byCategory = rule as FilterCategoryRule;
        if (byCategory != null) return "category is one of " + categoryNamesOf(byCategory.GetCategories());

        var parameterText = "?";
        try { parameterText = parameterOf(rule.GetRuleParameter()); }
        catch (Exception) { parameterText = "?"; }

        if (rule is HasValueFilterRule) return parameterText + " has a value";
        if (rule is HasNoValueFilterRule) return parameterText + " has no value";

        var byText = rule as FilterStringRule;
        if (byText != null)
            return string.Format("{0} {1} '{2}'", parameterText, verbOf(byText.GetEvaluator()), byText.RuleString);

        var byNumber = rule as FilterNumericValueRule;
        if (byNumber != null)
        {
            var value = "?";
            var asInteger = rule as FilterIntegerRule;
            var asDouble = rule as FilterDoubleRule;
            var asId = rule as FilterElementIdRule;
            if (asInteger != null) value = asInteger.RuleValue.ToString();
            else if (asDouble != null)
                value = asDouble.RuleValue.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)
                    + " (Revit's internal units - feet for a length)";
            else if (asId != null)
            {
                var target = asId.RuleValue == ElementId.InvalidElementId ? null : doc.GetElement(asId.RuleValue);
                value = target == null ? "element " + asId.RuleValue : "'" + target.Name + "'";
            }
            return string.Format("{0} {1} {2}", parameterText, verbOf(byNumber.GetEvaluator()), value);
        }

        return parameterText + " (" + rule.GetType().Name + ")";
    };

    // THE WHOLE RULE SET, logical filters walked. Revit keeps a filter's rules
    // as AND and OR groups of parameter filters, so each group is bracketed
    // and an inverted one is written NOT.
    Func<ElementFilter, string> ruleSetOf = null;
    ruleSetOf = held =>
    {
        if (held == null) return "?";
        string body;
        var logical = held as ElementLogicalFilter;
        var byParameter = held as ElementParameterFilter;
        if (logical != null)
        {
            var inner = logical.GetFilters().Where(piece => piece != null).Select(piece => ruleSetOf(piece)).ToList();
            body = inner.Count == 1 ? inner[0]
                : "(" + string.Join(held is LogicalOrFilter ? " OR " : " AND ", inner) + ")";
        }
        else if (byParameter != null)
        {
            var rules = byParameter.GetRules().Select(each => ruleOf(each)).ToList();
            body = rules.Count == 1 ? rules[0] : "(" + string.Join(" AND ", rules) + ")";
        }
        else body = held.GetType().Name;

        var inverted = false;
        try { inverted = held.Inverted; }
        catch (Exception) { inverted = false; }
        return inverted ? "NOT " + body : body;
    };

    // The priority order where the release can give it.
    var ordered = new List<ElementId>();
    var orderKnown = false;
    try
    {
        var getOrdered = typeof(View).GetMethod("GetOrderedFilters", Type.EmptyTypes);
        var inOrder = getOrdered == null ? null : getOrdered.Invoke(view, null) as IEnumerable<ElementId>;
        if (inOrder != null)
        {
            ordered.AddRange(inOrder);
            orderKnown = true;
        }
    }
    catch (Exception)
    {
        ordered.Clear();
        orderKnown = false;
    }
    if (!orderKnown)
    {
        try { ordered.AddRange(view.GetFilters()); }
        catch (Exception) { }
    }

    var getEnabledByName = typeof(View).GetMethod("GetIsFilterEnabled", new[] { typeof(ElementId) });
    HashSet<ElementId> drawnHere = null;
    var filterRows = new List<string>();
    var position = 0;

    foreach (var listedId in ordered)
    {
        position++;
        var listed = doc.GetElement(listedId) as FilterElement;
        var parts = new List<string>();

        if (getEnabledByName != null)
        {
            try { parts.Add("Enable Filter " + ((bool)getEnabledByName.Invoke(view, new object[] { listedId }) ? "ON" : "OFF")); }
            catch (Exception) { parts.Add("Enable Filter could not be read"); }
        }

        try { parts.Add(view.GetFilterVisibility(listedId) ? "Visibility ON" : "Visibility OFF - what it catches is HIDDEN"); }
        catch (Exception) { parts.Add("Visibility could not be read"); }

        var asRule = listed as ParameterFilterElement;
        var asSet = listed as SelectionFilterElement;
        if (asRule != null)
        {
            ICollection<ElementId> ruleCategories = new List<ElementId>();
            try { ruleCategories = asRule.GetCategories(); }
            catch (Exception) { }
            parts.Add(string.Format("categories ({0}): {1}", ruleCategories.Count,
                ruleCategories.Count == 0 ? "none" : categoryNamesOf(ruleCategories)));

            ElementFilter held = null;
            try { held = asRule.GetElementFilter(); }
            catch (Exception) { held = null; }
            parts.Add("rule: " + (held == null ? "none - every element of its categories" : ruleSetOf(held)));

            try
            {
                if (ruleCategories.Count == 0) parts.Add("catches 0 - it has no categories");
                else
                {
                    var caught = new FilteredElementCollector(doc, view.Id)
                        .WherePasses(new ElementMulticategoryFilter(ruleCategories))
                        .WhereElementIsNotElementType();
                    if (held != null) caught = caught.WherePasses(held);
                    parts.Add("catches " + caught.GetElementCount() + " host element(s) drawn here");
                }
            }
            catch (Exception ex) { parts.Add("catches could not be counted: " + ex.Message); }
        }
        else if (asSet != null)
        {
            var members = asSet.GetElementIds();
            if (drawnHere == null)
            {
                drawnHere = new HashSet<ElementId>();
                try { foreach (var drawnId in new FilteredElementCollector(doc, view.Id).ToElementIds()) drawnHere.Add(drawnId); }
                catch (Exception) { }
            }
            parts.Add(string.Format("a saved set of {0} element(s); catches {1} host element(s) drawn here",
                members.Count, members.Count(member => drawnHere.Contains(member))));
        }

        var setHere = "";
        try { setHere = overrideOf(view.GetFilterOverrides(listedId)); }
        catch (Exception ex) { setHere = "could not be read: " + ex.Message; }
        parts.Add("override: " + (setHere.Length > 0 ? setHere : "none"));

        filterRows.Add(string.Format("{0}. '{1}' ({2}) - {3}", position,
            listed == null ? "filter " + listedId : listed.Name,
            asRule != null ? "rule" : asSet != null ? "saved set" : "unfamiliar kind",
            string.Join(" | ", parts)));
    }

    if (filterRows.Count > 0)
    {
        var templateName = "";
        if (templateOwned)
        {
            var owner = doc.GetElement(view.ViewTemplateId);
            templateName = owner == null ? "?" : owner.Name;
        }
        filterList = string.Format("{0} filter(s) on '{1}', {2}. Where two filters set the same thing on the "
            + "same element, the one HIGHER in this list wins. 'catches' counts host elements this view draws - a "
            + "filter also acts on linked models drawn By Host View, which are not counted.{3}{4}",
            filterRows.Count, view.Name,
            orderKnown ? "in the order Visibility/Graphics lists them"
                : "in the order Revit returns them - this release cannot give the dialog's order",
            getEnabledByName == null ? " This release has no Enable Filter tick; every filter here is in effect." : "",
            templateOwned ? " The view template '" + templateName + "' owns these filters - change them on it." : "")
            + "\n  " + string.Join("\n  ", filterRows);
    }
}
