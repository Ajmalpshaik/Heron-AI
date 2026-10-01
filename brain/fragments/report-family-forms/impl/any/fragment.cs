// NOT STANDALONE. Assumes `doc` and `which` are in scope; leaves `formCount`,
// `formList`, `combinationList`, `scanned`, `notAFamily` and `findings` behind.
//
// READS ONLY. Nothing is changed.
//
// EVERY FORM IN THE FAMILY OPEN IN THE FAMILY EDITOR, with the id a later step
// names it by: what kind it is, solid or void, the plane its sketch is on, its
// extent and a solid's volume, its subcategory, where it shows, and the join or
// cut it belongs to. The question asked before locking, labelling, cutting or
// hiding anything - and the only way to name a form made by hand, which no form
// tool gave an id for.
//
// ONE LINE, NOT A LIST. A list is cut to three entries before anyone reads it,
// and the whole of this is the answer - the reason REPORT_FAMILY_PARAMETERS
// answers on one line too.
//
// `which` NARROWS IT: "all" or nothing for every form, or kinds and sides with
// commas between - "void", "extrusion", "solid, revolve". A form is kept when it
// matches every word given, so "void, extrusion" is the void extrusions.

var findings = new List<string>();
var formCount = 0;
var formList = "";
var combinationList = "";
var scanned = 0;
var notAFamily = false;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep" : "form";

// The sketch a form stands on, by its plane's name - the work plane it was
// drawn on. Each kind keeps it under a different name.
Func<GenericForm, string> planeOf = f =>
{
    Sketch sketch = null;
    try
    {
        if (f is Extrusion) sketch = ((Extrusion)f).Sketch;
        else if (f is Revolution) sketch = ((Revolution)f).Sketch;
        else if (f is Blend) sketch = ((Blend)f).BottomSketch;
        else if (f is SweptBlend) sketch = ((SweptBlend)f).PathSketch;
        else if (f is Sweep) sketch = ((Sweep)f).PathSketch;
    }
    catch (Exception) { }
    var plane = sketch == null ? null : sketch.SketchPlane;
    return plane == null ? "(no sketch plane)" : "on " + plane.Name;
};

var words = (which ?? "").Split(',').Select(squash).Where(w => w.Length > 0 && w != "all").ToList();
var known = new[] { "solid", "void", "extrusion", "revolve", "revolution", "blend", "sweep", "sweptblend" };
var unknown = words.Where(w => !known.Contains(w)).ToList();

var rows = new List<string>();
var joins = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front is a project, not a family open in the Family Editor. A family's forms are "
        + "read inside the family - open it first.");
}
else if (unknown.Count > 0)
{
    findings.Add("Nothing was read. \"" + string.Join("\", \"", unknown) + "\" is not a kind of form or a side - "
        + "name solid or void, and extrusion, revolve, blend, sweep or swept blend, or all.");
}
else
{
    var forms = new FilteredElementCollector(doc).OfClass(typeof(GenericForm)).Cast<GenericForm>().ToList();
    scanned = forms.Count;
    foreach (var f in forms.OrderBy(x => kindOf(x)).ThenBy(x => x.UniqueId))
    {
        var kind = kindOf(f);
        var side = f.IsSolid ? "solid" : "void";
        var matches = words.All(w => w == side || w == squash(kind) || (w == "revolution" && kind == "revolve"));
        if (!matches) continue;

        var extent = "";
        var box = f.get_BoundingBox(null);
        if (box != null)
            extent = "X " + mm(box.Min.X) + " to " + mm(box.Max.X) + ", Y " + mm(box.Min.Y) + " to " + mm(box.Max.Y)
                + ", Z " + mm(box.Min.Z) + " to " + mm(box.Max.Z) + " mm";

        var volume = 0.0;
        if (f.IsSolid)
        {
            var geometry = f.get_Geometry(new Options());
            if (geometry != null)
                foreach (GeometryObject piece in geometry)
                {
                    var body = piece as Solid;
                    if (body != null && body.Volume > 0) volume += body.Volume;
                }
        }

        var shows = "";
        var seen = f.GetVisibility();
        if (seen != null)
        {
            var levels = new[] { seen.IsShownInCoarse ? "coarse" : null, seen.IsShownInMedium ? "medium" : null,
                                 seen.IsShownInFine ? "fine" : null }.Where(s => s != null).ToList();
            var kinds = new[] { seen.IsShownInTopBottom ? "plan" : null, seen.IsShownInFrontBack ? "front/back" : null,
                                seen.IsShownInLeftRight ? "left/right" : null }.Where(s => s != null).ToList();
            shows = "shows in " + (levels.Count == 0 ? "no detail level" : string.Join(", ", levels)) + " | "
                + (kinds.Count == 0 ? "no plan or elevation" : string.Join(", ", kinds));
        }

        var inJoin = new List<string>();
        foreach (GeomCombination one in f.Combinations) inJoin.Add(one.UniqueId);

        rows.Add(side + " " + kind + " " + f.UniqueId + " (" + planeOf(f) + ")"
            + (extent.Length > 0 ? ": " + extent : "")
            + (f.IsSolid ? ", " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L" : "")
            + ", subcategory " + (f.Subcategory == null ? "(the family's own)" : f.Subcategory.Name)
            + (shows.Length > 0 ? ", " + shows : "")
            + (inJoin.Count > 0 ? ", in " + string.Join(" and ", inJoin) : ""));
    }

    foreach (var c in new FilteredElementCollector(doc).OfClass(typeof(GeomCombination)).Cast<GeomCombination>())
    {
        var members = new List<string>();
        foreach (CombinableElement m in c.AllMembers)
        {
            var mf = m as GenericForm;
            members.Add(mf == null ? m.UniqueId : (mf.IsSolid ? "solid " : "void ") + kindOf(mf) + " " + m.UniqueId);
        }
        joins.Add(c.UniqueId + ": " + string.Join(", ", members));
    }

    formCount = rows.Count;
    formList = string.Join("  ||  ", rows);
    combinationList = string.Join("  ||  ", joins);
    findings.Add(formCount + " form(s) of " + scanned + " in the family" + (words.Count > 0 ? " match \""
        + string.Join(", ", words) + "\"" : "") + "; " + joins.Count + " join(s) or cut(s).");
    if (scanned == 0) findings.Add("This family has no forms yet - nothing has been modelled in it.");
}
