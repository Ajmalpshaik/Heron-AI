// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves
// `findings`, `unusedFilters`, `filterCount` and `filterSettings` behind.
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

            var head = string.Format("View '{0}' ({1}): {2} filter(s).", view.Name, view.ViewType,
                onView.Count);

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
                    }
                }
            }
            catch { }

            if (getEnabled == null) head += " Revit 2020 has no Enable Filter tick; every filter here is in effect.";
            blocks.Add(head);

            foreach (var id in onView)
            {
                var filter = doc.GetElement(id) as FilterElement;
                var label = filter == null ? "filter " + id : filter.Name;
                try
                {
                    var s = view.GetFilterOverrides(id);
                    var parts = new List<string>();

                    if (getEnabled != null)
                    {
                        try { parts.Add("Enable Filter " + ((bool)getEnabled.Invoke(view, new object[] { id }) ? "ON" : "OFF")); }
                        catch { parts.Add("Enable Filter could not be read"); }
                    }
                    parts.Add("Visibility " + (view.GetFilterVisibility(id) ? "ON" : "OFF (what it matches is HIDDEN)"));
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

                    blocks.Add("  '" + label + "' - " + string.Join("; ", parts) + ".");
                }
                catch (Exception ex)
                {
                    blocks.Add(string.Format("  '{0}' - could not be read: {1}", label, ex.Message));
                }
            }
        }
    }

    findings.InsertRange(0, readNotes);
    filterSettings = string.Join("\n", blocks);
}
