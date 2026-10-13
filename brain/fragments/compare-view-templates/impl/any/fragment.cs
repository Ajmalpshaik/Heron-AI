// NOT STANDALONE. Assumes `doc`, `templateA` and `templateB` are in scope;
// leaves `differences`, `differenceCount`, `refused` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO VIEW TEMPLATES SIDE BY SIDE, AND ONLY WHERE THEY DIFFER. Compared:
//
//   the basics        view type, scale, detail level, visual style,
//                     discipline, phase filter - each as Revit prints it
//   what each controls the template parameters ticked in one and not the other
//   categories        hidden in one and shown in the other; and, for categories
//                     shown in both, an override of line colour, line weight,
//                     halftone or transparency that differs
//   filters           applied in one only; applied in both but visible in one
//                     only; or with different halftone or line colour
//
// A difference is printed "setting: A value | B value", so two near-identical
// templates are read in one glance. Nothing is changed in either.

var lines = new List<string>();
var findings = new List<string>();
string refused = null;

Func<string, View> template = name =>
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var v = element as View;
        if (v != null && v.IsTemplate && string.Equals(v.Name, (name ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            return v;
    }
    return null;
};

var a = template(templateA);
var b = template(templateB);
if (a == null) refused = "No view template is named \"" + templateA + "\".";
else if (b == null) refused = "No view template is named \"" + templateB + "\".";
else if (a.Id == b.Id) refused = "Both names are the same template.";

Func<View, BuiltInParameter, string> shown = (v, which) =>
{
    try
    {
        var p = v.get_Parameter(which);
        if (p == null || !p.HasValue) return "(none)";
        if (p.StorageType == StorageType.ElementId)
        {
            var target = doc.GetElement(p.AsElementId());
            return target == null ? "(none)" : target.Name;
        }
        return p.AsValueString() ?? "(none)";
    }
    catch { return "(not read)"; }
};

Action<string, string, string> differ = (what, va, vb) =>
{
    if (va != vb) lines.Add(what + ": " + va + " | " + vb);
};

Func<OverrideGraphicSettings, string> look = o =>
{
    if (o == null) return "";
    var c = o.ProjectionLineColor;
    return "colour " + (c == null || !c.IsValid ? "-" : c.Red + "," + c.Green + "," + c.Blue)
        + ", weight " + (o.ProjectionLineWeight < 0 ? "-" : o.ProjectionLineWeight.ToString())
        + ", halftone " + (o.Halftone ? "on" : "off")
        + ", transparency " + o.Transparency;
};

if (refused == null)
{
    differ("View type", a.ViewType.ToString(), b.ViewType.ToString());
    differ("Scale", "1:" + a.Scale, "1:" + b.Scale);
    differ("Detail level", a.DetailLevel.ToString(), b.DetailLevel.ToString());
    differ("Visual style", a.DisplayStyle.ToString(), b.DisplayStyle.ToString());
    differ("Discipline", shown(a, BuiltInParameter.VIEW_DISCIPLINE), shown(b, BuiltInParameter.VIEW_DISCIPLINE));
    differ("Phase filter", shown(a, BuiltInParameter.VIEW_PHASE_FILTER), shown(b, BuiltInParameter.VIEW_PHASE_FILTER));

    // WHAT EACH CONTROLS - names of the template parameters ticked in one only.
    Func<View, HashSet<string>> controlled = v =>
    {
        var names = new HashSet<string>();
        try
        {
            var off = new HashSet<ElementId>(v.GetNonControlledTemplateParameterIds());
            foreach (var id in v.GetTemplateParameterIds())
            {
                if (off.Contains(id)) continue;
                foreach (Parameter p in v.Parameters)
                    if (p.Id == id && p.Definition != null) { names.Add(p.Definition.Name); break; }
            }
        }
        catch { }
        return names;
    };
    var ca = controlled(a);
    var cb = controlled(b);
    foreach (var n in ca.Except(cb).OrderBy(n => n)) lines.Add("Controls \"" + n + "\": yes | no");
    foreach (var n in cb.Except(ca).OrderBy(n => n)) lines.Add("Controls \"" + n + "\": no | yes");

    // CATEGORIES.
    foreach (Category category in doc.Settings.Categories)
    {
        if (category == null) continue;
        bool ha = false, hb = false;
        try { ha = a.GetCategoryHidden(category.Id); hb = b.GetCategoryHidden(category.Id); }
        catch { continue; }
        if (ha != hb) { lines.Add("Category " + category.Name + ": " + (ha ? "hidden" : "shown") + " | " + (hb ? "hidden" : "shown")); continue; }
        if (ha) continue;
        try
        {
            if (!a.IsCategoryOverridable(category.Id) || !b.IsCategoryOverridable(category.Id)) continue;
            differ("Category " + category.Name + " override", look(a.GetCategoryOverrides(category.Id)), look(b.GetCategoryOverrides(category.Id)));
        }
        catch { }
    }

    // FILTERS.
    var fa = new HashSet<ElementId>(a.GetFilters());
    var fb = new HashSet<ElementId>(b.GetFilters());
    Func<ElementId, string> filterName = id => doc.GetElement(id) == null ? id.ToString() : doc.GetElement(id).Name;
    foreach (var id in fa.Except(fb)) lines.Add("Filter " + filterName(id) + ": applied | not applied");
    foreach (var id in fb.Except(fa)) lines.Add("Filter " + filterName(id) + ": not applied | applied");
    foreach (var id in fa.Intersect(fb))
    {
        try
        {
            differ("Filter " + filterName(id) + " visible", a.GetFilterVisibility(id) ? "yes" : "no", b.GetFilterVisibility(id) ? "yes" : "no");
            differ("Filter " + filterName(id) + " override", look(a.GetFilterOverrides(id)), look(b.GetFilterOverrides(id)));
        }
        catch { }
    }

    findings.Add(lines.Count == 0
        ? "\"" + a.Name + "\" and \"" + b.Name + "\" agree on everything compared."
        : lines.Count + " difference(s) between \"" + a.Name + "\" (left) and \"" + b.Name + "\" (right).");
}

var differences = string.Join("\n", lines);
var differenceCount = lines.Count;
if (refused != null) findings.Add(refused);
