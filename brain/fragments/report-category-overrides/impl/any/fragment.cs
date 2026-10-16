// NOT STANDALONE. Assumes `doc`, `view` and `allCategories` are in scope;
// leaves `findings`, `overriddenCategories`, `overrideList`,
// `modelCategoryList`, `subCategoryList`, `annotationCategoryList` and
// `elementOverrideList` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// VERSION 2 (2026-10-15) ADDS THE WHOLE VISIBILITY/GRAPHICS OF THE VIEW, AND
// ONLY WHEN ASKED - see the block at the end. `allCategories` left out is
// false, the four new strings stay empty, and every line above that block is
// version 1's, so `findings`, `overriddenCategories` and `overrideList` come
// back exactly as they did. `overrideList` is also what the Companion's Load
// button parses, which is one more reason it does not move.
//
// READ_GRAPHIC_OVERRIDES CANNOT ANSWER THIS - it reads the PER-ELEMENT override
// only, and says so. An element that is grey with no per-element override is
// grey because its CATEGORY is, and until this existed nothing here could say
// that.
//
// THE VISIBILITY FLAG IS NOT THE SIGNAL, AND THIS IS THE TRAP. A pattern's own
// "is visible" property reads TRUE on a category with nothing overridden at
// all, so a check built on it reports every category in the model as
// overridden. The real signal is a VALID COLOUR or a REAL PATTERN ID -
// something that was actually set.
//
// ONLY THE CATEGORIES PRESENT IN THE VIEW are looked at. A model has hundreds; a
// drawing shows a few dozen, and a category absent from the view cannot be why
// it looks wrong.
//
// THE WHOLE ANSWER TRAVELS AS ONE STRING, `overrideList`. `findings` holds the
// same lines as a list, and a reply shows a list as its first three entries,
// each cut to sixty characters. Measured 2026-09-22 on Project1, straight after
// SET_CATEGORY_SOLID_FILL: the Walls line arrived as "Walls: line RGB(0,255,0),
// cut line RGB(0,255,0), surface RGB..." - the fill, the half that was asked
// about, never arrived. A string is not cut.

var findings = new List<string>();
var overriddenCategories = new List<ElementId>();

// Every overridden category, in full, joined. Empty when nothing is
// overridden, so a clean view reads as nothing rather than as a sentence.
var overrideList = "";

if (!view.AreGraphicsOverridesAllowed())
{
    findings.Add(string.Format("view '{0}' ({1}) does not accept graphic overrides at all",
        view.Name, view.ViewType));
}
else
{
    // Which categories actually have something in this view.
    var present = new Dictionary<ElementId, string>();
    foreach (var element in new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType())
    {
        var category = element.Category;
        if (category == null) continue;
        if (!present.ContainsKey(category.Id)) present[category.Id] = category.Name;
    }

    var checkedCount = 0;
    var lines = new List<string>();

    foreach (var pair in present)
    {
        OverrideGraphicSettings settings = null;
        try { settings = view.GetCategoryOverrides(pair.Key); }
        catch { continue; }
        if (settings == null) continue;

        checkedCount++;
        var parts = new List<string>();

        var projection = settings.ProjectionLineColor;
        if (projection != null && projection.IsValid)
        {
            parts.Add(string.Format("line RGB({0},{1},{2})",
                projection.Red, projection.Green, projection.Blue));
        }

        var cut = settings.CutLineColor;
        if (cut != null && cut.IsValid)
        {
            parts.Add(string.Format("cut line RGB({0},{1},{2})", cut.Red, cut.Green, cut.Blue));
        }

        var surface = settings.SurfaceForegroundPatternColor;
        if (surface != null && surface.IsValid)
        {
            parts.Add(string.Format("surface RGB({0},{1},{2})",
                surface.Red, surface.Green, surface.Blue));
        }

        // A real pattern id is a set pattern. The visibility flag beside it is
        // NOT - see the header.
        if (settings.SurfaceForegroundPatternId != ElementId.InvalidElementId)
        {
            var pattern = doc.GetElement(settings.SurfaceForegroundPatternId);
            parts.Add("surface pattern " + (pattern == null ? "set" : pattern.Name));
        }

        // THE CUT HALF OF THE FILL, read the same way as the surface half. It
        // was never read: SET_CATEGORY_SOLID_FILL stamps a cut pattern on every
        // cuttable category it touches and `cut-colour` tints it, and a
        // category overridden only here read as not overridden at all.
        var cutFill = settings.CutForegroundPatternColor;
        if (cutFill != null && cutFill.IsValid)
        {
            parts.Add(string.Format("cut RGB({0},{1},{2})", cutFill.Red, cutFill.Green, cutFill.Blue));
        }

        if (settings.CutForegroundPatternId != ElementId.InvalidElementId)
        {
            var cutPattern = doc.GetElement(settings.CutForegroundPatternId);
            parts.Add("cut pattern " + (cutPattern == null ? "set" : cutPattern.Name));
        }

        if (settings.Transparency > 0) parts.Add(string.Format("{0}% transparent", settings.Transparency));
        if (settings.Halftone) parts.Add("halftone");
        if (settings.ProjectionLineWeight > 0) parts.Add("line weight " + settings.ProjectionLineWeight);

        if (parts.Count > 0)
        {
            var line = string.Format("{0}: {1}", pair.Value, string.Join(", ", parts));
            overriddenCategories.Add(pair.Key);
            findings.Add(line);
            lines.Add(line);
        }
    }

    overrideList = string.Join("  ||  ", lines);

    findings.Insert(0, string.Format("{0} of {1} category(ies) present in '{2}' carry an override. Only "
        + "categories WITH elements in this view were looked at - one absent from the drawing cannot be "
        + "why it looks wrong", overriddenCategories.Count, checkedCount, view.Name));

    if (overriddenCategories.Count == 0)
    {
        findings.Add("nothing is overridden at CATEGORY level here. If something still looks wrong, it "
            + "is a per-element override (READ_GRAPHIC_OVERRIDES), a view filter, or the view template");
    }
}

// ---- VERSION 2: THE WHOLE VISIBILITY/GRAPHICS OF THE VIEW, WHEN ASKED -------
//
// WHY VERSION 1's SCOPE WAS NOT ENOUGH. Measured 2026-10-15 on a real HVAC
// project on Revit 2020: version 1 said "7 of 16 categories present" carry an
// override, while the view overrode 77 of 85 model categories - a grey,
// halftone background for walls, doors, floors and the rest. Those overrides
// act on the LINKED architecture, so the host holds none of those elements, and
// a scan of what the host draws can never see them. Sub-category overrides -
// 518 of them on that view - were not read at all.
//
// So `allCategories` asks EVERY category the document has, and lists the ones
// that are hidden, overridden, or have host elements in the view. Each section
// is ONE STRING, one row per line: a reply cuts a list to three items of sixty
// characters and a dictionary to a count, and here the rows are the answer.
//
//   modelCategoryList       every model category hidden, overridden or with
//                           host elements in the view - hidden state, every
//                           override cell, and its Object Styles beside it,
//                           because "black weight 3 in Object Styles, grey
//                           weight 1 here" is the comparison being asked for
//   subCategoryList         every sub-category hidden or overridden, written
//                           "category > sub-category", Object Styles beside it
//   annotationCategoryList  every annotation category hidden or overridden
//   elementOverrideList     Override Graphics in View > By Element, grouped by
//                           category and identical override, with a count and
//                           the first ids of each group
//
// THE VIEW FILTERS ARE NOT HERE. AUDIT_VIEW_FILTERS' `everyFilter` lists them
// in priority order with their rules and what each catches - one card per tab.
//
// A VISIBLE TICK IS NAMED ONLY WHEN UNTICKED. It reads TRUE with nothing set -
// version 1's trap, above - so FALSE is the only value that was set.
//
// THE WORDS MATCH AUDIT_VIEW_FILTERS' `filterList` cell for cell, so a
// category's row and a filter's row can be compared without translating one.

var modelCategoryList = "";
var subCategoryList = "";
var annotationCategoryList = "";
var elementOverrideList = "";

if (allCategories && view.AreGraphicsOverridesAllowed())
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

    // The category's own Object Styles row - the same four values, in the same
    // order, as LIST_CATEGORIES version 2 prints them.
    Func<Category, string> objectStyleOf = styled =>
    {
        string colourText, projectionWeight, cutWeight, patternText;
        try { colourText = rgbOf(styled.LineColor); if (colourText.Length == 0) colourText = "none"; }
        catch (Exception) { colourText = "unreadable"; }
        try
        {
            var weight = styled.GetLineWeight(GraphicsStyleType.Projection);
            projectionWeight = weight == null ? "none" : weight.Value.ToString();
        }
        catch (Exception) { projectionWeight = "unreadable"; }
        try
        {
            if (!styled.IsCuttable) cutWeight = "-";
            else
            {
                var weight = styled.GetLineWeight(GraphicsStyleType.Cut);
                cutWeight = weight == null ? "none" : weight.Value.ToString();
            }
        }
        catch (Exception) { cutWeight = "unreadable"; }
        try
        {
            patternText = patternOf(styled.GetLinePatternId(GraphicsStyleType.Projection));
            if (patternText.Length == 0) patternText = "none";
        }
        catch (Exception) { patternText = "unreadable"; }
        return "Object Styles: colour " + colourText + ", projection weight " + projectionWeight
            + ", cut weight " + cutWeight + ", pattern " + patternText;
    };

    // WHO SETS THE TAB. A template that controls it makes every row below the
    // template's, and a change made on the view does not stick.
    Func<BuiltInParameter, string> templateNote = slot =>
    {
        try
        {
            if (view.IsTemplate || view.ViewTemplateId == ElementId.InvalidElementId) return "";
            var ownSlots = new HashSet<ElementId>(view.GetNonControlledTemplateParameterIds());
            if (ownSlots.Contains(new ElementId(slot))) return "";
            var template = doc.GetElement(view.ViewTemplateId);
            return string.Format(" The view template '{0}' sets this tab, so these rows are the template's - "
                + "change them on the template", template == null ? "?" : template.Name);
        }
        catch (Exception) { return ""; }
    };

    // What the host draws, by category, and every per-element override, in
    // one pass over the view.
    var drawnByCategory = new Dictionary<ElementId, int>();
    var ownOverrideGroups = new Dictionary<string, List<ElementId>>();
    foreach (var drawn in new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType())
    {
        var drawnCategory = drawn.Category;
        if (drawnCategory != null)
        {
            int already;
            drawnByCategory.TryGetValue(drawnCategory.Id, out already);
            drawnByCategory[drawnCategory.Id] = already + 1;
        }

        var own = "";
        try { own = overrideOf(view.GetElementOverrides(drawn.Id)); }
        catch (Exception) { own = ""; }
        if (own.Length == 0) continue;

        var groupKey = (drawnCategory == null ? "(no category)" : drawnCategory.Name) + ": " + own;
        List<ElementId> members;
        if (!ownOverrideGroups.TryGetValue(groupKey, out members))
        {
            members = new List<ElementId>();
            ownOverrideGroups[groupKey] = members;
        }
        members.Add(drawn.Id);
    }

    var modelRows = new List<string>();
    var subRows = new List<string>();
    var annotationRows = new List<string>();
    int dialogRows = 0, modelHidden = 0, modelOverridden = 0, annotationHidden = 0, annotationOverridden = 0;

    foreach (Category asked in doc.Settings.Categories)
    {
        if (asked == null) continue;
        CategoryType kind;
        try { kind = asked.CategoryType; }
        catch (Exception) { continue; }
        if (kind != CategoryType.Model && kind != CategoryType.Annotation) continue;

        // A row of THIS view's dialog. Revit 2020 makes it an indexed
        // property, and so does every release since - get_ is the call.
        var isDialogRow = false;
        try { isDialogRow = asked.get_AllowsVisibilityControl(view); }
        catch (Exception) { isDialogRow = false; }

        var isHidden = false;
        try { isHidden = view.GetCategoryHidden(asked.Id); }
        catch (Exception) { isHidden = false; }

        var setHere = "";
        try { setHere = overrideOf(view.GetCategoryOverrides(asked.Id)); }
        catch (Exception) { setHere = ""; }

        int drawnCount;
        drawnByCategory.TryGetValue(asked.Id, out drawnCount);

        // Listed whether or not it is a dialog row here, and said so - a
        // gate on that flag would make a hidden or overridden category vanish
        // from the report if Revit ever answered it false.
        var state = (isHidden ? "HIDDEN" : "shown")
            + (drawnCount > 0 ? ", " + drawnCount + " in view" : "")
            + (isDialogRow ? "" : ", not a row of this view's Visibility/Graphics");

        // THE COUNTS ARE OF THE DIALOG'S ROWS, so they read as the dialog does;
        // a category listed below that is not a row says so on its own line.
        if (kind == CategoryType.Model)
        {
            if (isDialogRow) dialogRows++;
            if (isDialogRow && isHidden) modelHidden++;
            if (isDialogRow && setHere.Length > 0) modelOverridden++;
            if (isHidden || setHere.Length > 0 || drawnCount > 0)
            {
                modelRows.Add(string.Format("{0} [{1}] V/G: {2} | {3}", asked.Name, state,
                    setHere.Length > 0 ? setHere : "no override", objectStyleOf(asked)));
            }
        }
        else if (isHidden || setHere.Length > 0)
        {
            if (isDialogRow && isHidden) annotationHidden++;
            if (isDialogRow && setHere.Length > 0) annotationOverridden++;
            annotationRows.Add(string.Format("{0} [{1}] V/G: {2} | {3}", asked.Name, state,
                setHere.Length > 0 ? setHere : "no override", objectStyleOf(asked)));
        }

        CategoryNameMap subs = null;
        try { subs = asked.SubCategories; }
        catch (Exception) { subs = null; }
        if (subs == null) continue;

        foreach (Category sub in subs)
        {
            if (sub == null) continue;
            var subHidden = false;
            try { subHidden = view.GetCategoryHidden(sub.Id); }
            catch (Exception) { subHidden = false; }
            var subSet = "";
            try { subSet = overrideOf(view.GetCategoryOverrides(sub.Id)); }
            catch (Exception) { subSet = ""; }
            if (!subHidden && subSet.Length == 0) continue;

            subRows.Add(string.Format("{0} > {1} [{2}] V/G: {3} | {4}", asked.Name, sub.Name,
                subHidden ? "HIDDEN" : "shown", subSet.Length > 0 ? subSet : "no override", objectStyleOf(sub)));
        }
    }

    modelRows.Sort(StringComparer.OrdinalIgnoreCase);
    subRows.Sort(StringComparer.OrdinalIgnoreCase);
    annotationRows.Sort(StringComparer.OrdinalIgnoreCase);

    modelCategoryList = string.Format("{0} model categories are rows of Visibility/Graphics in '{1}': {2} of "
        + "them hidden, {3} overridden. Listed: every model category hidden, overridden or with host elements "
        + "in this view ({4}) - one that is not a row here says so. 'in view' counts HOST elements only - an "
        + "override on a category the host has none of still acts on linked models drawn By Host View.{5}",
        dialogRows, view.Name, modelHidden, modelOverridden, modelRows.Count, templateNote(BuiltInParameter.VIS_GRAPHICS_MODEL))
        + (modelRows.Count == 0 ? "" : "\n  " + string.Join("\n  ", modelRows));

    subCategoryList = string.Format("{0} sub-categories hidden or overridden in '{1}', written category > "
        + "sub-category.", subRows.Count, view.Name)
        + (subRows.Count == 0 ? "" : "\n  " + string.Join("\n  ", subRows));

    annotationCategoryList = string.Format("{0} annotation categories hidden or overridden in '{1}' - of the "
        + "rows of Visibility/Graphics, {2} hidden and {3} overridden; one that is not a row here says so.{4}", annotationRows.Count, view.Name, annotationHidden, annotationOverridden,
        templateNote(BuiltInParameter.VIS_GRAPHICS_ANNOTATION))
        + (annotationRows.Count == 0 ? "" : "\n  " + string.Join("\n  ", annotationRows));

    var groupRows = new List<string>();
    var withOwn = 0;
    foreach (var group in ownOverrideGroups.OrderByDescending(pair => pair.Value.Count)
        .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
    {
        withOwn += group.Value.Count;
        var firstIds = string.Join(", ", group.Value.Take(5).Select(memberId => memberId.ToString()));
        groupRows.Add(string.Format("{0} x {1} (ids {2}{3})", group.Value.Count, group.Key, firstIds,
            group.Value.Count > 5 ? " and " + (group.Value.Count - 5) + " more" : ""));
    }

    elementOverrideList = withOwn == 0
        ? string.Format("No element in '{0}' carries its own override (Override Graphics in View > By "
            + "Element).", view.Name)
        : string.Format("{0} element(s) in '{1}' carry their own override (Override Graphics in View > By "
            + "Element), in {2} group(s) of one category and one identical override:", withOwn, view.Name,
            groupRows.Count) + "\n  " + string.Join("\n  ", groupRows);
}
