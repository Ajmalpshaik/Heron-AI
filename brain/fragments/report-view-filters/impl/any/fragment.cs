// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves
// `findings`, `unusedFilters`, `filterCount`, `filterSettings`, `disabled`,
// `hidden`, `blankOverride`, `nothingToActOn` and `templateOwned` behind.
//
// VERSION 3 (2026-11-01) - AUDIT_VIEW_FILTERS MERGED IN, on the owner's
// instruction ("make sure there is no duplicate tools"). For each view named,
// what that card had that this lacked:
//
//   THE PRIORITY ORDER. Each filter is numbered in the order Visibility/
//   Graphics lists them - `GetOrderedFilters`, reached BY NAME because it
//   arrived at Revit 2021 - and where two filters set the same thing on the
//   same element, the one HIGHER wins. On 2020 the order is Revit's and the
//   heading says the dialog's order cannot be given.
//
//   EACH FILTER'S RULE, CATEGORIES AND CATCH. Its categories by name; its rule
//   as the Filters dialog reads it, logical filters walked and an inverse rule
//   written NOT; and how many HOST elements this view draws that it catches -
//   a filter also acts on linked models drawn By Host View, which are not
//   counted, and the heading says so.
//
//   THE FOUR WAYS A FILTER CAN BE PRESENT AND DO NOTHING, none of which Revit
//   reports as an error, each by id and in a finding naming the view:
//     `disabled`        applied but its Enable Filter tick OFF (2021 on; on
//                       2020 there is no such tick and that is SAID, never
//                       counted as passed)
//     `hidden`          its Visibility off - it HIDES what it matches
//     `blankOverride`   an EMPTY override - every slot asked, blank only when
//                       none is set, so a filter carrying only halftone is not
//                       called blank
//     `nothingToActOn`  every one of its categories switched off in the view
//
//   THE TEMPLATE-OWNED VERDICT - `templateOwned`, and a finding naming the
//   template: where the view's template controls the filters slot, anything
//   set on the view is discarded when the template reasserts. The answer to
//   "my filter change did not save".
//
//   A VIEW THAT TAKES NO GRAPHIC OVERRIDE AT ALL - a schedule, a sheet - is
//   said to be one, which is not the same as a view with no filters.
//
//   IT DOES NOT REPAIR ANYTHING. Switching a disabled filter back on is a
//   write - APPLY_VIEW_FILTER's - and a write hidden inside a read is how a
//   diagnosis becomes a change nobody asked for.
//
// READ ONLY. Opens no transaction and needs none.
//
// NOT EVERY "VIEW" REVIT RETURNS CAN ANSWER THE QUESTION. The project browser
// and the system browser come back from the same collector as real views and
// THROW when asked whether a filter is applied - "the view type does not
// support Visibility/Graphics Overrides". So every probe sits in its own guard:
// one pseudo-view must not take the whole report down. Found on a real model,
// not reasoned about.
//
// A FILTER APPLIED TO NO VIEW IS THE ROW WORTH FINDING. It is either a leftover
// from a standard nobody follows any more or one somebody made and never
// applied, and both look identical to a project that is simply tidy.
//
// BOTH KINDS ARE LISTED. A rule-based filter and a saved set are different
// things doing the same job in a view, and which one a filter is decides
// whether it keeps working as the model grows.
//
// NAME A VIEW AND ITS WHOLE FILTERS TAB COMES BACK (version 2, 2026-09-28,
// FRAGMENT-ISSUES 5b-248). Each filter's row - Enable Filter, Visibility, the
// projection and cut lines and patterns with their colours and Visible ticks,
// transparency, halftone - read off the view, which is the read half of
// APPLY_VIEW_FILTER. ONE STRING, because a list reaches the reply cut to three
// items of sixty characters and this list IS the answer. Blank `viewName`
// leaves it empty and the report is what it always was.
//
// ENABLE FILTER IS READ BY NAME, the same way APPLY_VIEW_FILTER sets it: it
// arrived at Revit 2021, and the add-in compiles this with no release symbols
// (5b-181), so a `#if` would name a member Revit 2020 has not got.

var findings = new List<string>();
var unusedFilters = new List<ElementId>();
var filterSettings = "";
var disabled = new List<ElementId>();
var hidden = new List<ElementId>();
var blankOverride = new List<ElementId>();
var nothingToActOn = new List<ElementId>();
var templateOwned = false;

var filters = new FilteredElementCollector(doc).OfClass(typeof(FilterElement))
    .Cast<FilterElement>().OrderBy(f => f.Name).ToList();

var filterCount = filters.Count;

// Real views only - templates hold filters but are not where a drawing is, and
// sheets and schedules have none.
var views = new List<View>();
foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
{
    if (view == null || view.IsTemplate) continue;
    if (view.ViewType == ViewType.Schedule || view.ViewType == ViewType.DrawingSheet) continue;
    views.Add(view);
}

foreach (var filter in filters)
{
    var kind = filter is ParameterFilterElement
        ? "a RULE, which keeps working as the model grows"
        : filter is SelectionFilterElement
            ? "a SAVED LIST, which does not follow new elements"
            : "an unfamiliar kind of filter";

    var usedIn = new List<string>();
    foreach (var view in views)
    {
        // One guard per view. See the header.
        try { if (view.IsFilterApplied(filter.Id)) usedIn.Add(view.Name); }
        catch { }
    }

    if (usedIn.Count == 0)
    {
        unusedFilters.Add(filter.Id);
        findings.Add(string.Format("'{0}'  - {1}  - ON NO VIEW. Either a leftover, or made and never "
            + "applied", filter.Name, kind));
    }
    else
    {
        findings.Add(string.Format("'{0}'  - {1}  - used in {2} view(s): {3}",
            filter.Name, kind, usedIn.Count,
            usedIn.Count > 12
                ? string.Join(", ", usedIn.Take(12)) + string.Format(" and {0} more", usedIn.Count - 12)
                : string.Join(", ", usedIn)));
    }
}

findings.Insert(0, string.Format("{0} filter(s) in this project, checked against {1} view(s). {2} are "
    + "on no view at all", filterCount, views.Count, unusedFilters.Count));

// ---- the Filters tab of the views named ------------------------------------

var asked = (viewName ?? "").Split(';').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();

if (asked.Count > 0)
{
    var blank = new OverrideGraphicSettings();
    var solidLine = LinePatternElement.GetSolidPatternId();
    var getEnabled = typeof(View).GetMethod("GetIsFilterEnabled", new[] { typeof(ElementId) });

    Func<Color, string> rgb = c => c != null && c.IsValid
        ? string.Format("{0},{1},{2}", c.Red, c.Green, c.Blue) : "";

    Func<ElementId, string> patternName = id =>
    {
        if (id == null || id == ElementId.InvalidElementId) return "";
        if (id == solidLine) return "Solid";
        var e = doc.GetElement(id);
        return e == null ? "pattern " + id : e.Name;
    };

    // One cell of the dialog: the parts that are set, or "no override".
    Func<string, string, int, string> lines = (pattern, colour, weight) =>
    {
        var bits = new List<string>();
        if (pattern.Length > 0) bits.Add("pattern " + pattern);
        if (colour.Length > 0) bits.Add("colour " + colour);
        if (weight != blank.ProjectionLineWeight) bits.Add("weight " + weight);
        return bits.Count == 0 ? "no override" : string.Join(", ", bits);
    };
    Func<string, string, bool, string> fill = (pattern, colour, shown) =>
    {
        var bits = new List<string>();
        if (pattern.Length > 0) bits.Add(pattern);
        if (colour.Length > 0) bits.Add("colour " + colour);
        if (!shown) bits.Add("Visible UNTICKED");
        return bits.Count == 0 ? "no override" : string.Join(", ", bits);
    };

    // ---- VERSION 3, FROM AUDIT_VIEW_FILTERS ----

    // WHAT COUNTS AS A BLANK OVERRIDE. There is no "is this override empty"
    // call, so every slot is asked and the answer is blank only when NONE is
    // set. A slot that cannot be read is not evidence of blankness.
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
            return false;
        }
        return true;
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

    Func<ICollection<ElementId>, string> categoryNamesOf = ids =>
    {
        var categoryNames = new List<string>();
        foreach (var categoryId in ids)
        {
            var shown = categoryId.ToString();
            try
            {
                var category = Category.GetCategory(doc, categoryId);
                if (category != null) shown = category.Name;
            }
            catch (Exception) { }
            categoryNames.Add(shown);
        }
        categoryNames.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", categoryNames);
    };

    // ONE RULE, as the dialog reads it. An inverse rule is NOT here.
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

    // THE WHOLE RULE SET, logical filters walked and bracketed.
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

    var getOrdered = typeof(View).GetMethod("GetOrderedFilters", Type.EmptyTypes);

    // A name as Heron prints it - "FloorPlan: Level 1" - or as the browser
    // does - "Level 1". Every view that answers is read; reading is harmless,
    // and choosing one of two lookalikes silently is not.
    var blocks = new List<string>();
    // Said FIRST in findings, because a reply shows only three of them and
    // an empty filterSettings with its reason fourth reads as no answer.
    var readNotes = new List<string>();
    foreach (var name in asked)
    {
        var everyView = name == "*";
        var matched = new List<View>();
        foreach (var view in views)
        {
            if (everyView)
            {
                try { if (view.GetFilters().Count > 0) matched.Add(view); } catch { }
                continue;
            }
            var typed = view.ViewType + ": " + view.Name;
            if (string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(typed, name, StringComparison.OrdinalIgnoreCase))
                matched.Add(view);
        }

        // NOTHING FOUND LEAVES filterSettings EMPTY and says why in findings,
        // so an answer with nothing in it reads as empty rather than as a
        // sentence - the same rule for a missing view, a view with no filter
        // and a view that cannot carry one.
        if (matched.Count == 0)
        {
            readNotes.Add(everyView
                ? "No view in this model has a filter on it."
                : string.Format("No view called '{0}' - type it as the Project Browser shows it, or as "
                    + "'FloorPlan: {0}'. Nothing was read for it.", name));
            continue;
        }

        foreach (var view in matched)
        {
            // A VIEW THAT TAKES NO OVERRIDE AT ALL (version 3) is said to be
            // one - "0 filters" on a schedule is true and useless.
            var overridesAllowed = true;
            try { overridesAllowed = view.AreGraphicsOverridesAllowed(); } catch { }
            if (!overridesAllowed)
            {
                readNotes.Add(string.Format("View '{0}' ({1}) does not accept graphic overrides at all, so no filter "
                    + "can apply there - not the same as a view with no filters.", view.Name, view.ViewType));
                continue;
            }

            ICollection<ElementId> onView = null;
            try { onView = view.GetFilters(); }
            catch (Exception ex)
            {
                readNotes.Add(string.Format("View '{0}' ({1}) cannot carry filters: {2}",
                    view.Name, view.ViewType, ex.Message));
                continue;
            }

            if (onView.Count == 0)
            {
                readNotes.Add(string.Format("View '{0}' ({1}) carries no filters.", view.Name, view.ViewType));
                continue;
            }

            // THE PRIORITY ORDER (version 3), where the release can give it.
            var ordered = new List<ElementId>();
            var orderKnown = false;
            try
            {
                var inOrder = getOrdered == null ? null : getOrdered.Invoke(view, null) as IEnumerable<ElementId>;
                if (inOrder != null) { ordered.AddRange(inOrder); orderKnown = true; }
            }
            catch (Exception) { ordered.Clear(); orderKnown = false; }
            if (!orderKnown) ordered.AddRange(onView);

            var head = string.Format("View '{0}' ({1}): {2} filter(s), {3}. Where two filters set the same thing "
                + "on the same element, the one HIGHER in this list wins. 'catches' counts host elements this view "
                + "draws - a filter also acts on linked models drawn By Host View, which are not counted.",
                view.Name, view.ViewType, onView.Count,
                orderKnown ? "in the order Visibility/Graphics lists them"
                    : "in the order Revit returns them - this release cannot give the dialog's order");

            // A TEMPLATE THAT OWNS THE FILTERS is what makes a filter change
            // "not stick", so it is said once, at the top of the view.
            try
            {
                if (view.ViewTemplateId != ElementId.InvalidElementId)
                {
                    var own = new HashSet<ElementId>(view.GetNonControlledTemplateParameterIds());
                    if (!own.Contains(new ElementId(BuiltInParameter.VIS_GRAPHICS_FILTERS)))
                    {
                        var template = doc.GetElement(view.ViewTemplateId);
                        head += string.Format(" Its view template '{0}' controls these - a change made "
                            + "on the view itself does not stick.", template == null ? "?" : template.Name);
                        // THE TEMPLATE-OWNED VERDICT (version 3).
                        templateOwned = true;
                        findings.Add(string.Format("The view template '{0}' owns the filters on '{1}'. Anything "
                            + "changed there is discarded when the template reasserts - change it ON THE TEMPLATE.",
                            template == null ? "?" : template.Name, view.Name));
                    }
                }
            }
            catch { }

            if (getEnabled == null) head += " Revit 2020 has no Enable Filter tick; every filter here is in effect.";
            blocks.Add(head);

            HashSet<ElementId> drawnHere = null;
            var position = 0;
            foreach (var id in ordered)
            {
                position++;
                var filter = doc.GetElement(id) as FilterElement;
                var label = filter == null ? "filter " + id : filter.Name;
                try
                {
                    var s = view.GetFilterOverrides(id);
                    var parts = new List<string>();

                    if (getEnabled != null)
                    {
                        try
                        {
                            var enabled = (bool)getEnabled.Invoke(view, new object[] { id });
                            parts.Add("Enable Filter " + (enabled ? "ON" : "OFF"));
                            // DO-NOTHING 1 (version 3): listed, greyed, doing nothing.
                            if (!enabled)
                            {
                                if (!disabled.Contains(id)) disabled.Add(id);
                                findings.Add(string.Format("'{0}' is APPLIED BUT SWITCHED OFF on '{1}'. It is in the "
                                    + "list and it does nothing.", label, view.Name));
                            }
                        }
                        catch { parts.Add("Enable Filter could not be read"); }
                    }
                    var visible = view.GetFilterVisibility(id);
                    parts.Add("Visibility " + (visible ? "ON" : "OFF (what it matches is HIDDEN)"));
                    // DO-NOTHING 2 (version 3): it HIDES rather than colours.
                    if (!visible)
                    {
                        if (!hidden.Contains(id)) hidden.Add(id);
                        findings.Add(string.Format("'{0}' has its VISIBILITY off on '{1}', so everything it matches "
                            + "is hidden there rather than coloured.", label, view.Name));
                    }
                    // DO-NOTHING 3 (version 3): an EMPTY override.
                    if (s != null && isBlank(s))
                    {
                        if (!blankOverride.Contains(id)) blankOverride.Add(id);
                        findings.Add(string.Format("'{0}' has an EMPTY override on '{1}' - nothing is set on it. Adding "
                            + "a filter without setting one leaves exactly this, and it looks identical to having no "
                            + "filter.", label, view.Name));
                    }
                    parts.Add("Projection/Surface Lines: " + lines(patternName(s.ProjectionLinePatternId),
                        rgb(s.ProjectionLineColor), s.ProjectionLineWeight));
                    parts.Add("Surface Patterns: foreground " + fill(patternName(s.SurfaceForegroundPatternId),
                        rgb(s.SurfaceForegroundPatternColor), s.IsSurfaceForegroundPatternVisible)
                        + " / background " + fill(patternName(s.SurfaceBackgroundPatternId),
                        rgb(s.SurfaceBackgroundPatternColor), s.IsSurfaceBackgroundPatternVisible));
                    parts.Add("Transparency " + s.Transparency);
                    parts.Add("Cut Lines: " + lines(patternName(s.CutLinePatternId), rgb(s.CutLineColor),
                        s.CutLineWeight));
                    parts.Add("Cut Patterns: foreground " + fill(patternName(s.CutForegroundPatternId),
                        rgb(s.CutForegroundPatternColor), s.IsCutForegroundPatternVisible)
                        + " / background " + fill(patternName(s.CutBackgroundPatternId),
                        rgb(s.CutBackgroundPatternColor), s.IsCutBackgroundPatternVisible));
                    parts.Add("Halftone " + (s.Halftone ? "ON" : "off"));
                    if (s.DetailLevel != blank.DetailLevel) parts.Add("Detail level " + s.DetailLevel);

                    // WHETHER THE CUT COLUMNS MEAN ANYTHING HERE - Revit greys them
                    // out when none of the filter's categories can be cut.
                    var categoryIds = new List<ElementId>();
                    var asRule = filter as ParameterFilterElement;
                    var asSet = filter as SelectionFilterElement;
                    if (asRule != null) categoryIds.AddRange(asRule.GetCategories());

                    // ITS CATEGORIES, ITS RULE AND WHAT IT CATCHES HERE (version 3).
                    if (asRule != null)
                    {
                        parts.Insert(0, string.Format("categories ({0}): {1}", categoryIds.Count,
                            categoryIds.Count == 0 ? "none" : categoryNamesOf(categoryIds)));
                        ElementFilter held = null;
                        try { held = asRule.GetElementFilter(); }
                        catch (Exception) { held = null; }
                        parts.Insert(1, "rule: " + (held == null ? "none - every element of its categories" : ruleSetOf(held)));
                        try
                        {
                            if (categoryIds.Count == 0) parts.Insert(2, "catches 0 - it has no categories");
                            else
                            {
                                var caught = new FilteredElementCollector(doc, view.Id)
                                    .WherePasses(new ElementMulticategoryFilter(categoryIds))
                                    .WhereElementIsNotElementType();
                                if (held != null) caught = caught.WherePasses(held);
                                parts.Insert(2, "catches " + caught.GetElementCount() + " host element(s) drawn here");
                            }
                        }
                        catch (Exception ex) { parts.Insert(2, "catches could not be counted: " + ex.Message); }

                        // DO-NOTHING 4 (version 3): every category switched off here.
                        if (categoryIds.Count > 0)
                        {
                            var anyDrawn = false;
                            foreach (var categoryId in categoryIds)
                            {
                                var categoryHidden = false;
                                try { categoryHidden = view.GetCategoryHidden(categoryId); } catch { }
                                if (!categoryHidden) { anyDrawn = true; break; }
                            }
                            if (!anyDrawn)
                            {
                                if (!nothingToActOn.Contains(id)) nothingToActOn.Add(id);
                                findings.Add(string.Format("'{0}' has every one of its categories switched off in '{1}', "
                                    + "so it has nothing to act on. The rule is fine; the category visibility is what is "
                                    + "hiding the result.", label, view.Name));
                            }
                        }
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
                        parts.Insert(0, string.Format("a saved set of {0} element(s); catches {1} host element(s) drawn here",
                            members.Count, members.Count(member => drawnHere.Contains(member))));
                    }
                    else if (asSet != null)
                        foreach (var memberId in asSet.GetElementIds())
                        {
                            var member = doc.GetElement(memberId);
                            if (member != null && member.Category != null && !categoryIds.Contains(member.Category.Id))
                                categoryIds.Add(member.Category.Id);
                        }
                    var cuttable = new List<string>();
                    var notCuttable = new List<string>();
                    foreach (var categoryId in categoryIds)
                    {
                        Category category = null;
                        try { category = Category.GetCategory(doc, categoryId); } catch { category = null; }
                        if (category == null) continue;
                        if (category.IsCuttable) cuttable.Add(category.Name); else notCuttable.Add(category.Name);
                    }
                    parts.Add(cuttable.Count == 0
                        ? "cut not applicable - none of " + (notCuttable.Count == 0 ? "its categories"
                            : string.Join(", ", notCuttable)) + " can be cut, so the Cut columns are greyed out"
                        : "cut applies to " + string.Join(", ", cuttable)
                            + (notCuttable.Count == 0 ? "" : "; not to " + string.Join(", ", notCuttable)));

                    blocks.Add("  " + position + ". '" + label + "' - " + string.Join("; ", parts) + ".");
                }
                catch (Exception ex)
                {
                    blocks.Add(string.Format("  {0}. '{1}' - could not be read: {2}", position, label, ex.Message));
                }
            }
        }
    }

    findings.InsertRange(0, readNotes);
    filterSettings = string.Join("\n", blocks);
}
