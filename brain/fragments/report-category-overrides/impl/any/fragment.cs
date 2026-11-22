// NOT STANDALONE. Assumes `doc`, `view` and `allCategories` are in scope;
// leaves `findings`, `overriddenCategories`, `overrideList`,
// `modelCategoryList`, `subCategoryList`, `annotationCategoryList`,
// `elementOverrideList`, `hiddenCategories`, `hiddenCategoryList`,
// `templateOwnedCategories`, `visibilityTemplate` and `skippedCategories`
// behind.
//
// VERSION 6 (2026-11-21, row 5b-1959, issue #1207) - IN THE WHOLE-DIALOG
// READ (`allCategories` true) A READ THAT FAILED SAYS "NOT READ" ON ITS ROW,
// never "shown", "no override" or "not a row" - a read that throws, or an
// override Revit gives back as nothing. A category whose kind or whose
// sub-categories will not read is named, and an element whose own override
// will not read is counted with its ids, never one "without" an override.
// D-52: a failed read is never a no. The totals count only what WAS read, and
// the sentence saying what was not rides only on an answer with one, so an
// answer with none reads as version 5's.
//
// VERSION 5 (2026-11-21, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - A CATEGORY DRAWN IN THE VIEW THAT WILL NOT GIVE ITS OVERRIDES IS
// COUNTED. Version 4 passed over one whose overrides Revit would not read,
// without a word, and then said "nothing is overridden at CATEGORY level here"
// - of categories it had not looked at. `skippedCategories` carries the count
// every time; the sentence naming it rides only on that empty answer, so an
// answer that lists overrides reads as version 4's. Only the version 1 block's
// scan is counted; the blocks below it are unchanged.
//
// READ ONLY. Opens no transaction and needs none.
//
// VERSION 3 (2026-11-01) - REPORT_CATEGORY_VISIBILITY MERGED IN, on the
// owner's instruction ("make sure there is no duplicate tools"). Which
// categories are SWITCHED OFF in the view, always - see the block at the end:
// `hiddenCategories` as ids, `hiddenCategoryList` one string, how many of the
// view's Visibility/Graphics rows its template owns and the template's name,
// and DIAGNOSE_VISIBILITY named when nothing is switched off. It does NOT
// depend on `allCategories`, and it is NOT scoped to the view - a hidden
// category's elements are in no view-scoped collector at all.
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
// VERSION 5 (D-64): categories drawn in this view whose overrides Revit would
// not give.
var skippedCategories = 0;

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
        // VERSION 5: counted, never silently dropped (D-64).
        try { settings = view.GetCategoryOverrides(pair.Key); }
        catch { skippedCategories++; continue; }
        if (settings == null) { skippedCategories++; continue; }

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
        // VERSION 5 (D-64): ON THE EMPTY ANSWER ONLY, what was not looked at.
        if (skippedCategories > 0)
            findings.Add(string.Format("{0} more category(ies) drawn in '{1}' would not give their overrides, so "
                + "they are not among the {2} looked at - \"nothing is overridden\" is not said of them",
                skippedCategories, view.Name, checkedCount));
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
// THE VIEW FILTERS ARE NOT HERE. REPORT_VIEW_FILTERS (AUDIT_VIEW_FILTERS' job since 2026-11-01) lists them
// in priority order with their rules and what each catches - one card per tab.
//
// A VISIBLE TICK IS NAMED ONLY WHEN UNTICKED. It reads TRUE with nothing set -
// version 1's trap, above - so FALSE is the only value that was set.
//
// THE WORDS MATCH what AUDIT_VIEW_FILTERS' `filterList` printed, cell for cell, so a
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
            // VERSION 4 (row 5b-1854): the include list is read FROM THE
            // TEMPLATE - Revit's reference: the parameters "not marked as
            // included when this view is used as a template". Version 3 read
            // it off the view, so "the template sets this tab" could be wrong
            // either way.
            var template = doc.GetElement(view.ViewTemplateId) as View;
            if (template == null) return " Its view template could not be found, so who sets this tab is not known";
            var ownSlots = new HashSet<ElementId>(template.GetNonControlledTemplateParameterIds());
            if (ownSlots.Contains(new ElementId(slot))) return "";
            return string.Format(" The view template '{0}' sets this tab, so these rows are the template's - "
                + "change them on the template", template.Name);
        }
        catch (Exception thrown)
        {
            return " Whether its view template sets this tab could not be read - Revit says: "
                + (thrown.InnerException ?? thrown).Message;
        }
    };

    // What the host draws, by category, and every per-element override, in
    // one pass over the view.
    var drawnByCategory = new Dictionary<ElementId, int>();
    var ownOverrideGroups = new Dictionary<string, List<ElementId>>();
    var ownNotRead = new List<ElementId>();
    foreach (var drawn in new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType())
    {
        var drawnCategory = drawn.Category;
        if (drawnCategory != null)
        {
            int already;
            drawnByCategory.TryGetValue(drawnCategory.Id, out already);
            drawnByCategory[drawnCategory.Id] = already + 1;
        }

        // VERSION 6 (row 5b-1959): an element whose own override Revit will
        // not give - it throws, or gives nothing back - is COUNTED, never read
        // as one without an override (D-52).
        var own = "";
        try
        {
            var read = view.GetElementOverrides(drawn.Id);
            if (read == null) { ownNotRead.Add(drawn.Id); continue; }
            own = overrideOf(read);
        }
        catch (Exception) { ownNotRead.Add(drawn.Id); continue; }
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

    // VERSION 6 (row 5b-1959, issue #1207) - A READ THAT FAILED SAYS SO ON ITS
    // ROW. Version 5 turned a GetCategoryHidden that threw into "shown", a
    // GetCategoryOverrides that threw into "no override" and an
    // AllowsVisibilityControl that threw into "not a row" - the same words as a
    // category that was read and is plain. D-52: a failed read is never a no.
    // Each now reads "NOT READ" with Revit's own words and the row is listed
    // whatever else it holds. The totals on each first line stay what they
    // were - rows that WERE read hidden, overridden or drawn - and a sentence
    // after them counts the rows with something unread, only when there is
    // one, so an answer with none reads as version 5's.
    Func<Exception, string> revitSays = thrown =>
        ((thrown.InnerException ?? thrown).Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
    const string gaveNothing = "it gave no settings back";
    int modelListed = 0, subListed = 0, annotationListed = 0;
    int modelNotRead = 0, subNotRead = 0, annotationNotRead = 0, rowFlagNotRead = 0;
    var kindNotRead = new List<string>();
    var subsNotRead = new List<string>();
    Func<Category, string> nameOf = named =>
    {
        try { return named.Name; }
        catch (Exception) { return "(a category whose name will not read)"; }
    };

    foreach (Category asked in doc.Settings.Categories)
    {
        if (asked == null) continue;
        CategoryType kind;
        try { kind = asked.CategoryType; }
        catch (Exception thrown)
        {
            // Neither list can hold it, and its sub-categories are not read -
            // so it is NAMED, never a category with nothing set.
            kindNotRead.Add(nameOf(asked) + " (Revit says: " + revitSays(thrown) + ")");
            continue;
        }
        if (kind != CategoryType.Model && kind != CategoryType.Annotation) continue;

        // A row of THIS view's dialog. Revit 2020 makes it an indexed
        // property, and so does every release since - get_ is the call.
        var isDialogRow = false;
        string rowNotRead = null;
        try { isDialogRow = asked.get_AllowsVisibilityControl(view); }
        catch (Exception thrown) { isDialogRow = false; rowNotRead = revitSays(thrown); }

        var isHidden = false;
        string hiddenNotRead = null;
        try { isHidden = view.GetCategoryHidden(asked.Id); }
        catch (Exception thrown) { isHidden = false; hiddenNotRead = revitSays(thrown); }

        var setHere = "";
        string overrideNotRead = null;
        try
        {
            var read = view.GetCategoryOverrides(asked.Id);
            if (read == null) overrideNotRead = gaveNothing;
            else setHere = overrideOf(read);
        }
        catch (Exception thrown) { setHere = ""; overrideNotRead = revitSays(thrown); }

        var anyNotRead = rowNotRead != null || hiddenNotRead != null || overrideNotRead != null;
        var overrideText = overrideNotRead != null
            ? "override NOT READ - Revit says: " + overrideNotRead
            : setHere.Length > 0 ? setHere : "no override";

        int drawnCount;
        drawnByCategory.TryGetValue(asked.Id, out drawnCount);

        // Listed whether or not it is a dialog row here, and said so - a
        // gate on that flag would make a hidden or overridden category vanish
        // from the report if Revit ever answered it false.
        var state = (hiddenNotRead != null ? "visibility NOT READ - Revit says: " + hiddenNotRead
                : isHidden ? "HIDDEN" : "shown")
            + (drawnCount > 0 ? ", " + drawnCount + " in view" : "")
            + (rowNotRead != null ? ", whether it is a row of this view's Visibility/Graphics NOT READ - Revit says: "
                + rowNotRead
                : isDialogRow ? "" : ", not a row of this view's Visibility/Graphics");

        // THE COUNTS ARE OF THE DIALOG'S ROWS, so they read as the dialog does;
        // a category listed below that is not a row says so on its own line.
        // What WAS read is counted; what Revit would not give is not.
        if (kind == CategoryType.Model)
        {
            if (isDialogRow) dialogRows++;
            if (rowNotRead != null) rowFlagNotRead++;
            if (isDialogRow && isHidden) modelHidden++;
            if (isDialogRow && setHere.Length > 0) modelOverridden++;
            if (anyNotRead) modelNotRead++;
            if (isHidden || setHere.Length > 0 || drawnCount > 0) modelListed++;
            if (isHidden || setHere.Length > 0 || drawnCount > 0 || anyNotRead)
            {
                modelRows.Add(string.Format("{0} [{1}] V/G: {2} | {3}", asked.Name, state,
                    overrideText, objectStyleOf(asked)));
            }
        }
        else if (isHidden || setHere.Length > 0 || anyNotRead)
        {
            if (isDialogRow && isHidden) annotationHidden++;
            if (isDialogRow && setHere.Length > 0) annotationOverridden++;
            if (anyNotRead) annotationNotRead++;
            if (isHidden || setHere.Length > 0) annotationListed++;
            annotationRows.Add(string.Format("{0} [{1}] V/G: {2} | {3}", asked.Name, state,
                overrideText, objectStyleOf(asked)));
        }

        CategoryNameMap subs = null;
        try { subs = asked.SubCategories; }
        catch (Exception thrown)
        {
            subsNotRead.Add(asked.Name + " (Revit says: " + revitSays(thrown) + ")");
            continue;
        }
        if (subs == null) continue;

        foreach (Category sub in subs)
        {
            if (sub == null) continue;
            var subHidden = false;
            string subHiddenNotRead = null;
            try { subHidden = view.GetCategoryHidden(sub.Id); }
            catch (Exception thrown) { subHidden = false; subHiddenNotRead = revitSays(thrown); }
            var subSet = "";
            string subSetNotRead = null;
            try
            {
                var read = view.GetCategoryOverrides(sub.Id);
                if (read == null) subSetNotRead = gaveNothing;
                else subSet = overrideOf(read);
            }
            catch (Exception thrown) { subSet = ""; subSetNotRead = revitSays(thrown); }
            var subAnyNotRead = subHiddenNotRead != null || subSetNotRead != null;
            if (!subHidden && subSet.Length == 0 && !subAnyNotRead) continue;
            if (subAnyNotRead) subNotRead++;
            if (subHidden || subSet.Length > 0) subListed++;

            subRows.Add(string.Format("{0} > {1} [{2}] V/G: {3} | {4}", asked.Name, sub.Name,
                subHiddenNotRead != null ? "visibility NOT READ - Revit says: " + subHiddenNotRead
                    : subHidden ? "HIDDEN" : "shown",
                subSetNotRead != null ? "override NOT READ - Revit says: " + subSetNotRead
                    : subSet.Length > 0 ? subSet : "no override",
                objectStyleOf(sub)));
        }
    }

    modelRows.Sort(StringComparer.OrdinalIgnoreCase);
    subRows.Sort(StringComparer.OrdinalIgnoreCase);
    annotationRows.Sort(StringComparer.OrdinalIgnoreCase);

    // VERSION 6: what would not read, said after the section's first line and
    // only when there is some. A template note ends with no full stop, so one
    // is put between the two.
    Func<string, string, string> thenSay = (head, note) => note.Length == 0 ? head
        : head + (head.EndsWith(".") ? "" : ".") + note;
    Func<int, string, string> notReadNote = (count, what) => count == 0 ? ""
        : string.Format(" {0} {1} listed below would not give all of their Visibility/Graphics - what Revit "
            + "would not give says NOT READ on its row, with Revit's words, and is not counted above.", count, what);

    var modelHead = string.Format("{0} model categories are rows of Visibility/Graphics in '{1}': {2} of "
        + "them hidden, {3} overridden. Listed: every model category hidden, overridden or with host elements "
        + "in this view ({4}) - one that is not a row here says so. 'in view' counts HOST elements only - an "
        + "override on a category the host has none of still acts on linked models drawn By Host View.{5}",
        dialogRows, view.Name, modelHidden, modelOverridden, modelListed, templateNote(BuiltInParameter.VIS_GRAPHICS_MODEL));
    var modelNote = notReadNote(modelNotRead, "model category(ies)")
        + (rowFlagNotRead == 0 ? "" : string.Format(" {0} of them would not say whether they are a row of "
            + "Visibility/Graphics here, so are not in the {1} rows.", rowFlagNotRead, dialogRows))
        + (kindNotRead.Count == 0 ? "" : string.Format(" {0} category(ies) would not say whether they are model "
            + "or annotation, so are in no list below and their sub-categories were not read - NOT READ, never "
            + "a category with nothing set: {1}.", kindNotRead.Count, string.Join("; ", kindNotRead)));
    modelCategoryList = thenSay(modelHead, modelNote)
        + (modelRows.Count == 0 ? "" : "\n  " + string.Join("\n  ", modelRows));

    var subNote = notReadNote(subNotRead, "sub-category(ies)")
        + (subsNotRead.Count == 0 ? "" : string.Format(" {0} category(ies) would not give their sub-categories, "
            + "so none of theirs was read - NOT READ, never none hidden or overridden: {1}.",
            subsNotRead.Count, string.Join("; ", subsNotRead)))
        + (kindNotRead.Count == 0 ? "" : string.Format(" The sub-categories of {0} category(ies) whose kind "
            + "would not read were not read either - named in the model categories.", kindNotRead.Count));
    subCategoryList = thenSay(string.Format("{0} sub-categories hidden or overridden in '{1}', written category > "
        + "sub-category.", subListed, view.Name), subNote)
        + (subRows.Count == 0 ? "" : "\n  " + string.Join("\n  ", subRows));

    annotationCategoryList = thenSay(string.Format("{0} annotation categories hidden or overridden in '{1}' - of the "
        + "rows of Visibility/Graphics, {2} hidden and {3} overridden; one that is not a row here says so.{4}", annotationListed, view.Name, annotationHidden, annotationOverridden,
        templateNote(BuiltInParameter.VIS_GRAPHICS_ANNOTATION)), notReadNote(annotationNotRead, "annotation category(ies)"))
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

    // VERSION 6: an element whose own override would not read is never one
    // "without" an override - version 5 said "No element ... carries its own
    // override" of elements it had not read. Said on the first line, with the
    // first five ids, as a group is.
    var ownNotReadNote = ownNotRead.Count == 0 ? ""
        : string.Format(" {0} element(s) in view would not give their own override, so are NOT READ - never "
            + "counted as without one (ids {1}{2}).", ownNotRead.Count,
            string.Join(", ", ownNotRead.Take(5).Select(memberId => memberId.ToString())),
            ownNotRead.Count > 5 ? " and " + (ownNotRead.Count - 5) + " more" : "");
    elementOverrideList = withOwn == 0
        ? (ownNotRead.Count == 0
            ? string.Format("No element in '{0}' carries its own override (Override Graphics in View > By "
                + "Element).", view.Name)
            : string.Format("No element read in '{0}' carries its own override (Override Graphics in View > By "
                + "Element).", view.Name) + ownNotReadNote)
        : (ownNotRead.Count == 0
            ? string.Format("{0} element(s) in '{1}' carry their own override (Override Graphics in View > By "
                + "Element), in {2} group(s) of one category and one identical override:", withOwn, view.Name,
                groupRows.Count)
            : string.Format("{0} element(s) in '{1}' carry their own override (Override Graphics in View > By "
                + "Element), in {2} group(s) of one category and one identical override.", withOwn, view.Name,
                groupRows.Count) + ownNotReadNote + " The groups:")
            + "\n  " + string.Join("\n  ", groupRows);
}

// ---- VERSION 3: WHICH CATEGORIES ARE SWITCHED OFF, ALWAYS -------------------
//
// FROM REPORT_CATEGORY_VISIBILITY, and the safer of its rules where the two
// cards disagreed:
//
// IT DELIBERATELY DOES NOT SCOPE TO THE VIEW - the OPPOSITE decision from the
// override scan above, and not a style choice. A hidden category's elements
// do not appear in a view-scoped collector AT ALL, so scoping this to what the
// view shows would hide exactly the categories it exists to find.
//
// EVERY HIDDEN CATEGORY IS LISTED, EMPTY ONES INCLUDED - the owner's rule for
// this merge, "in every conflict keep the safer rule". REPORT_CATEGORY_
// VISIBILITY left out a category with nothing in the model; listed here, it is
// marked "nothing in the model", so the few that matter still read first and a
// category switched off before its first element is drawn is not missed.
//
// IT DOES NOT DEPEND ON `allCategories`. Asking what is switched off is the
// commonest question about a view, and it is answered on every call.
//
// VISIBILITY IS NOT AN OVERRIDE. Switched off shows nothing; overridden shows
// differently. Something MISSING from a drawing is this block's question;
// something the WRONG COLOUR is the one above.
//
// CanCategoryBeHidden IS NOT A GATE ON THE QUESTION - a real defect found
// 2026-09-08 against Snowdon Towers Sample HVAC. It answers "may this view
// CHANGE it", not "is it hidden": a view driven by a template answers false
// to every category, so the old loop reported "nothing is switched off" for
// 'L2' while GetCategoryHidden said 18 were. GetCategoryHidden reports the
// EFFECTIVE value, so the question is always asked, and what the template owns
// is REPORTED - counted over the rows of this view's Visibility/Graphics, so a
// category no view can switch is not mistaken for one the template holds.

var hiddenCategories = new List<ElementId>();
var hiddenCategoryList = "";
var templateOwnedCategories = 0;
var visibilityTemplate = "";
{
    // What exists anywhere in the model, so an empty category can be said.
    var populated = new HashSet<ElementId>();
    var populatedName = new Dictionary<ElementId, string>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        var category = element.Category;
        if (category == null) continue;
        if (populated.Add(category.Id)) populatedName[category.Id] = category.Name;
    }

    // Every category the document has, and any category an element carries
    // that the settings list does not.
    var everyCategory = new List<KeyValuePair<ElementId, string>>();
    var listed = new HashSet<ElementId>();
    foreach (Category asked in doc.Settings.Categories)
    {
        if (asked == null || !listed.Add(asked.Id)) continue;
        everyCategory.Add(new KeyValuePair<ElementId, string>(asked.Id, asked.Name));
    }
    foreach (var pair in populatedName)
        if (listed.Add(pair.Key)) everyCategory.Add(pair);

    var hiddenRows = new List<string>();
    var dialogRowsAsked = 0;
    var askedCount = 0;
    foreach (var pair in everyCategory)
    {
        var hidden = false;
        try { hidden = view.GetCategoryHidden(pair.Key); }
        catch { continue; }
        askedCount++;

        var isDialogRow = false;
        try
        {
            var category = Category.GetCategory(doc, pair.Key);
            isDialogRow = category != null && category.get_AllowsVisibilityControl(view);
        }
        catch { isDialogRow = false; }

        // Recorded, never used to skip: "this is off and you cannot turn it
        // back on here" is the next thing the person asks.
        if (isDialogRow)
        {
            dialogRowsAsked++;
            try { if (!view.CanCategoryBeHidden(pair.Key)) templateOwnedCategories++; }
            catch { }
        }

        if (hidden)
        {
            hiddenCategories.Add(pair.Key);
            hiddenRows.Add(pair.Value + " - SWITCHED OFF" + (populated.Contains(pair.Key) ? "" : " (nothing in the model)"));
        }
    }

    hiddenRows.Sort(StringComparer.OrdinalIgnoreCase);
    hiddenCategoryList = string.Format("{0} of {1} category(ies) asked are switched off in '{2}', every one listed - "
        + "a category with nothing in the model is marked so.", hiddenCategories.Count, askedCount, view.Name)
        + (hiddenRows.Count == 0 ? "" : "\n  " + string.Join("\n  ", hiddenRows));
    findings.Add(string.Format("{0} category(ies) are SWITCHED OFF in '{1}' - every one, empty ones included, in "
        + "hiddenCategoryList. Switched off is not overridden: it shows nothing", hiddenCategories.Count, view.Name));

    // WHERE THE SETTING LIVES, because "switch it back on" is the next
    // sentence and in a templated view it cannot be done here at all.
    if (!view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
    {
        try
        {
            var owner = doc.GetElement(view.ViewTemplateId) as View;
            if (owner != null) visibilityTemplate = owner.Name;
        }
        catch { }
    }
    var templateWords = visibilityTemplate.Length > 0 ? "the view template '" + visibilityTemplate + "'" : "a view template";
    if (templateOwnedCategories > 0 && templateOwnedCategories == dialogRowsAsked)
        findings.Add("these are set by " + templateWords + ", not in this view. Changing them here is not possible - "
            + "the template decides, and every other view using it follows");
    else if (templateOwnedCategories > 0)
        findings.Add(string.Format("{0} of the {1} Visibility/Graphics rows of this view cannot be switched here - "
            + "set by {2} rather than in this view", templateOwnedCategories, dialogRowsAsked, templateWords));

    if (hiddenCategories.Count == 0)
        findings.Add("nothing is switched off at CATEGORY level here. If something is still missing from the drawing, "
            + "it is a per-element hide, a view filter set to invisible, a workset, the view range, or the phase - "
            + "DIAGNOSE_VISIBILITY asks all of those for one element");
}
