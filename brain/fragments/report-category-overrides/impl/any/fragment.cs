// NOT STANDALONE. Assumes `doc` and `view` are in scope; leaves `findings` and
// `overriddenCategories` behind.
//
// READ ONLY. Opens no transaction and needs none.
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

var findings = new List<string>();
var overriddenCategories = new List<ElementId>();

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

        if (settings.Transparency > 0) parts.Add(string.Format("{0}% transparent", settings.Transparency));
        if (settings.Halftone) parts.Add("halftone");
        if (settings.ProjectionLineWeight > 0) parts.Add("line weight " + settings.ProjectionLineWeight);

        if (parts.Count > 0)
        {
            overriddenCategories.Add(pair.Key);
            findings.Add(string.Format("{0}: {1}", pair.Value, string.Join(", ", parts)));
        }
    }

    findings.Insert(0, string.Format("{0} of {1} category(ies) present in '{2}' carry an override. Only "
        + "categories WITH elements in this view were looked at - one absent from the drawing cannot be "
        + "why it looks wrong", overriddenCategories.Count, checkedCount, view.Name));

    if (overriddenCategories.Count == 0)
    {
        findings.Add("nothing is overridden at CATEGORY level here. If something still looks wrong, it "
            + "is a per-element override (READ_GRAPHIC_OVERRIDES), a view filter, or the view template");
    }
}
