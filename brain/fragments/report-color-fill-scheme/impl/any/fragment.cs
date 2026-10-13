// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves `schemes`,
// `legends`, `report`, `findings` and `refused` behind.
//
// READS ONLY. It opens no transaction and changes nothing; setting,
// re-colouring or clearing a scheme is APPLY_COLOR_FILL_SCHEME.
//
// THE READ HALF OF APPLY_COLOR_FILL_SCHEME, AND ITS CHECK. For each view: the
// colour scheme it shows for every category it can colour-fill, what that
// scheme colours by, every row's value and colour, and the Color Fill Legends
// the view owns. Then what a person checking the drawing would look for:
//   - a coloured plan with no legend, which nobody can read;
//   - a legend for a category with no scheme, which reads "No color scheme
//     assigned";
//   - two rows in one colour, or nearly one, which the plan cannot tell apart;
//   - values the view shows that have no row, and elements with no value,
//     which draw uncoloured;
//   - rows nothing in the model uses;
//   - a view whose template holds the colour scheme, so a change goes to the
//     template.
//
// `schemes` is the RESULT - one line per view and category with a scheme set -
// and `legends` counts what the views own. Everything else is prose
// (accounting, D-52), so a view with no scheme and no legend reads as empty.
//
// REVIT 2020 AND 2021 TAKE THE OTHER BRANCH: the 2020 API has no colour scheme
// type and no view call that reads one, and 2021's is a different shape with
// no legend. What those releases CAN see is reported - the legends each view
// owns, the schemes the model holds, and the view's own property when Revit
// exposes it as a parameter - and the rest is said, not guessed.

var schemes = new List<string>();
var legends = 0;
var findings = new List<string>();
var refused = "";
var report = "";
var lines = new List<string>();

var asked = new List<View>();
if (views != null)
{
    foreach (var one in views)
        if (one != null && !asked.Any(w => w.Id.Equals(one.Id))) asked.Add(one);
}
if (asked.Count == 0)
    refused = "no view was given - name a plan, several separated by commas, or a view template.";

Func<View, string> headOf = v =>
{
    var template = v.IsTemplate ? null : doc.GetElement(v.ViewTemplateId) as View;
    return (v.IsTemplate ? "view template '" : "'") + v.Name + "' (" + v.ViewType
        + (template == null ? "" : ", template '" + template.Name + "'") + ")";
};

var version = "";
try { version = doc.Application.VersionNumber; } catch (Exception) { version = "this release"; }

#if REVIT2020 || REVIT2021
if (refused.Length == 0)
{
    // The legend categories exist in every release, and a legend's owner view
    // is a property of any element - so these counts need no colour scheme API.
    var legendCategories = new[] { BuiltInCategory.OST_ColorFillLegends,
        BuiltInCategory.OST_DuctColorFillLegends, BuiltInCategory.OST_PipeColorFillLegends };
    var schemeCategory = new ElementId(BuiltInCategory.OST_ColorFillSchema);
    // Every scheme counted, each name once with how many carry it.
    var held = new List<string>();
    var heldCount = 0;
    try
    {
        var all = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ColorFillSchema).ToElements();
        heldCount = all.Count;
        foreach (var group in all.GroupBy(e => e.Name).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            held.Add(group.Key + (group.Count() > 1 ? " (" + group.Count() + " by that name)" : ""));
    }
    catch (Exception) { }

    foreach (var v in asked)
    {
        var owned = 0;
        foreach (var c in legendCategories)
        {
            try
            {
                owned += new FilteredElementCollector(doc).OfCategory(c).WhereElementIsNotElementType()
                    .Count(e => e.OwnerViewId.Equals(v.Id));
            }
            catch (Exception) { }
        }
        legends += owned;

        // THE VIEW'S OWN PROPERTY, WHEN REVIT EXPOSES IT: a parameter whose
        // value is a colour scheme element. Found by what it holds, not by its
        // name, so a Revit in another language finds it too.
        var named = new List<string>();
        foreach (Parameter p in v.Parameters)
        {
            if (p == null || p.Definition == null || p.StorageType != StorageType.ElementId) continue;
            Element held1 = null;
            try { held1 = doc.GetElement(p.AsElementId()); } catch (Exception) { held1 = null; }
            if (held1 != null && held1.Category != null && held1.Category.Id.Equals(schemeCategory))
                named.Add(p.Definition.Name + " '" + held1.Name + "'");
        }
        foreach (var n in named) schemes.Add(headOf(v) + " | " + n);
        lines.Add(headOf(v) + ": " + owned + " colour fill legend(s)"
            + (named.Count > 0 ? "; its properties name " + string.Join(", ", named)
                : "; which scheme it shows per category is not something Revit " + version + "'s API can read"));
    }
    lines.Add("this model holds " + heldCount + " colour scheme(s)"
        + (held.Count > 0 ? ": " + string.Join(", ", held.Take(30)) : ""));
    findings.Add("Revit " + version + "'s API has no colour scheme calls - which scheme a view shows per "
        + "category, its rows and its colours arrived in Revit 2022. Read them in Revit: Properties > "
        + "Color Scheme > Edit");
}
#else
// A ROW'S VALUE, as the modeller sees it - the text, the referenced element's
// name, the integer - else the caption the legend prints.
Func<ColorFillSchemeEntry, string> keyOf = row =>
{
    try
    {
        if (row.StorageType == StorageType.String) return row.GetStringValue() ?? "";
        if (row.StorageType == StorageType.ElementId)
        {
            var referenced = doc.GetElement(row.GetElementIdValue());
            return referenced == null ? (row.Caption ?? "") : referenced.Name;
        }
        if (row.StorageType == StorageType.Integer) return row.GetIntegerValue().ToString();
    }
    catch (Exception) { }
    return row.Caption ?? "";
};

// A PARAMETER'S NAME, WITHOUT READING ITS ID AS A NUMBER - off an element of
// the category, off the project's parameter elements, else off Revit's list.
Dictionary<ElementId, string> builtIns = null;
Func<ElementId, ElementId, string> labelOf = (id, categoryId) =>
{
    try
    {
        var sample = new FilteredElementCollector(doc).OfCategoryId(categoryId)
            .WhereElementIsNotElementType().FirstElement();
        if (sample != null)
            foreach (Parameter p in sample.Parameters)
                if (p.Id.Equals(id) && p.Definition != null) return p.Definition.Name;
    }
    catch (Exception) { }
    var asElement = doc.GetElement(id) as ParameterElement;
    if (asElement != null && asElement.GetDefinition() != null) return asElement.GetDefinition().Name;
    if (builtIns == null)
    {
        builtIns = new Dictionary<ElementId, string>();
        foreach (var typeId in ParameterUtils.GetAllBuiltInParameters())
        {
            try
            {
                var asId = new ElementId(ParameterUtils.GetBuiltInParameter(typeId));
                if (!builtIns.ContainsKey(asId)) builtIns[asId] = LabelUtils.GetLabelForBuiltInParameter(typeId);
            }
            catch (Exception) { }
        }
    }
    string found;
    return builtIns.TryGetValue(id, out found) ? found : "a parameter Revit does not name";
};

if (refused.Length == 0)
{
    foreach (var v in asked)
    {
        var head = headOf(v);
        var template = v.IsTemplate ? null : doc.GetElement(v.ViewTemplateId) as View;
        ICollection<ElementId> takes;
        try { takes = v.SupportedColorFillCategoryIds(); }
        catch (Exception) { takes = new List<ElementId>(); }
        var owned = new FilteredElementCollector(doc).OfClass(typeof(ColorFillLegend)).Cast<ColorFillLegend>()
            .Where(l => l.OwnerViewId.Equals(v.Id)).ToList();
        legends += owned.Count;

        if (takes.Count == 0)
        {
            lines.Add(head + ": takes no colour scheme at all - colour fill belongs to plans, sections and "
                + "their templates" + (owned.Count > 0 ? "; it owns " + owned.Count + " colour fill legend(s)" : ""));
            continue;
        }

        var shown = 0;
        var categoryNames = new List<string>();
        foreach (var categoryId in takes)
        {
            var category = Category.GetCategory(doc, categoryId);
            var categoryName = category == null ? "a category" : category.Name;
            categoryNames.Add(categoryName);
            var here = owned.Where(l => l.ColorFillCategoryId.Equals(categoryId)).ToList();
            var id = v.GetColorFillSchemeId(categoryId);
            var scheme = id.Equals(ElementId.InvalidElementId) ? null : doc.GetElement(id) as ColorFillScheme;
            if (scheme == null)
            {
                if (here.Count > 0)
                    findings.Add(head + ": a Color Fill Legend for " + categoryName + " but no colour scheme - "
                        + "it reads 'No color scheme assigned'");
                continue;
            }
            shown++;

            var by = labelOf(scheme.ParameterDefinition, categoryId);
            var area = "";
            try
            {
                if (!scheme.AreaSchemeId.Equals(ElementId.InvalidElementId) && doc.GetElement(scheme.AreaSchemeId) != null)
                    area = ", area scheme '" + doc.GetElement(scheme.AreaSchemeId).Name + "'";
            }
            catch (Exception) { area = ""; }
            var rows = scheme.GetEntries();
            var rowText = rows.Select(r => keyOf(r) + " " + r.Color.Red + "," + r.Color.Green + "," + r.Color.Blue
                + (r.IsInUse ? "" : " (not in use)") + (r.IsVisible ? "" : " (hidden)")).ToList();

            schemes.Add(head + " | " + categoryName + " | '" + scheme.Name + "' by " + by
                + (scheme.IsByRange ? " (by range)" : "") + area + " | " + rows.Count + " row(s) | "
                + here.Count + " legend(s)");
            lines.Add(head + ": " + categoryName + " shown in '" + scheme.Name + "' (title '" + scheme.Title
                + "'), coloured by " + by + (scheme.IsByRange ? " by range" : " by value") + area + "; "
                + rows.Count + " row(s): " + string.Join("; ", rowText.Take(40))
                + (rowText.Count > 40 ? "; and " + (rowText.Count - 40) + " more" : "")
                + "; " + here.Count + " legend(s)"
                + (here.Count > 0 ? " at " + string.Join(", ", here.Select(l => Math.Round(l.Origin.X * 304.8).ToString("0")
                    + ", " + Math.Round(l.Origin.Y * 304.8).ToString("0") + " mm")) : ""));

            // ---- the checks ----------------------------------------------------
            if (here.Count == 0 && !v.IsTemplate)
                findings.Add(head + ": " + categoryName + " is coloured but the view has no Color Fill Legend "
                    + "for it - nobody reading the plan can tell what the colours mean");
            if (here.Count > 1)
                findings.Add(head + ": " + here.Count + " Color Fill Legends for " + categoryName
                    + " - the same information twice");

            var unused = rows.Where(r => !r.IsInUse).Select(r => keyOf(r)).ToList();
            if (unused.Count > 0)
                findings.Add("'" + scheme.Name + "': " + unused.Count + " row(s) colour nothing in the model - "
                    + string.Join(", ", unused.Take(10)));
            var hidden = rows.Where(r => !r.IsVisible).Select(r => keyOf(r)).ToList();
            if (hidden.Count > 0)
                findings.Add("'" + scheme.Name + "': " + hidden.Count + " row(s) are switched to hidden - "
                    + string.Join(", ", hidden.Take(10)));

            // Two rows the eye cannot tell apart: the same RGB, or within 24 of
            // each other across the three channels together.
            var close = new List<string>();
            for (var i = 0; i < rows.Count && close.Count < 6; i++)
            {
                for (var j = i + 1; j < rows.Count && close.Count < 6; j++)
                {
                    var a = rows[i].Color;
                    var b = rows[j].Color;
                    var dr = a.Red - b.Red;
                    var dg = a.Green - b.Green;
                    var db = a.Blue - b.Blue;
                    var gap = Math.Sqrt(dr * dr + dg * dg + db * db);
                    if (gap < 24.0)
                        close.Add("'" + keyOf(rows[i]) + "' and '" + keyOf(rows[j]) + "'"
                            + (gap < 0.5 ? " share one colour" : " are " + Math.Round(gap) + " apart"));
                }
            }
            if (close.Count > 0)
                findings.Add("'" + scheme.Name + "': rows the plan can hardly tell apart - " + string.Join("; ", close));

            // VALUES THE VIEW SHOWS WITH NO ROW, and elements with no value at
            // all - both draw uncoloured. By value only; a range covers numbers.
            if (!v.IsTemplate && !scheme.IsByRange
                && (scheme.StorageType == StorageType.String || scheme.StorageType == StorageType.ElementId))
            {
                var inView = new List<Element>();
                try
                {
                    inView = new FilteredElementCollector(doc, v.Id).OfCategoryId(categoryId)
                        .WhereElementIsNotElementType().ToList();
                }
                catch (Exception) { inView = new List<Element>(); }
                var keys = new HashSet<string>(rows.Select(r => keyOf(r)), StringComparer.OrdinalIgnoreCase);
                var noRow = new List<string>();
                var blank = 0;
                foreach (var e in inView)
                {
                    foreach (Parameter p in e.Parameters)
                    {
                        if (!p.Id.Equals(scheme.ParameterDefinition)) continue;
                        var value = "";
                        try
                        {
                            if (p.StorageType == StorageType.String) value = p.AsString() ?? "";
                            else
                            {
                                var referenced = doc.GetElement(p.AsElementId());
                                value = referenced == null ? "" : referenced.Name;
                            }
                        }
                        catch (Exception) { value = ""; }
                        if (string.IsNullOrWhiteSpace(value)) blank++;
                        else if (!keys.Contains(value) && !noRow.Contains(value)) noRow.Add(value);
                        break;
                    }
                }
                if (noRow.Count > 0)
                    findings.Add(head + ": " + categoryName + " the view shows with no row in '" + scheme.Name
                        + "' - " + string.Join(", ", noRow.Take(15)) + (noRow.Count > 15 ? " and " + (noRow.Count - 15)
                        + " more" : "") + ". Revit usually adds a row when the view redraws");
                if (blank > 0)
                    findings.Add(head + ": " + blank + " " + categoryName + " in the view have no " + by
                        + " and draw uncoloured");
            }

            // A TEMPLATE THAT HOLDS THE SCHEME: the view cannot take even the
            // scheme it already shows, so a change goes to the template.
            if (template != null)
            {
                var free = true;
                try { free = v.CanApplyColorFillScheme(categoryId, id); } catch (Exception) { free = true; }
                if (!free)
                    findings.Add(head + ": its view template '" + template.Name + "' holds the colour scheme - "
                        + "change it on '" + template.Name + "', which changes every view that follows it");
            }
        }
        if (shown == 0)
            lines.Add(head + ": no colour scheme on any category it can colour-fill (" + string.Join(", ", categoryNames)
                + ")" + (owned.Count > 0 ? "; it owns " + owned.Count + " colour fill legend(s)" : ""));
    }
}
#endif

// ---- one string the reply cannot truncate ----------------------------------
report = refused.Length > 0
    ? "NOT READ: " + refused
    : "Revit " + version + " - " + string.Join(" || ", lines)
        + (findings.Count > 0 ? " || CHECKS: " + string.Join("; ", findings) : " || CHECKS: nothing to report");
