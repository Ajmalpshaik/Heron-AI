// NOT STANDALONE. Assumes `doc` and `which` are in scope; leaves `formCount`,
// `formList`, `combinationList`, `scanned`, `notAFamily`, `elements`,
// `refused` and `findings` behind.
//
// READS ONLY. Nothing is changed, and nothing is highlighted - SET_SELECTION
// does that with the `elements` this leaves, and DELETE_ELEMENTS deletes them.
//
// VERSION 4 (2026-11-01) took in SELECT_FAMILY_FORMS, which picked the same
// forms by the same ids or kinds and handed them on, and is DEPRECATED, merged
// into this card. What it did that this did not is here now: the forms read
// are handed on as `elements` - in the order their ids were named, each once
// - for SET_SELECTION, DELETE_ELEMENTS or any tool that takes elements; a call
// that reads nothing says why in `refused`; and "freeform" is a kind, as a
// FreeFormElement is a form. ALL OR NOTHING as that card was: one id that is
// not a form refuses the call and `elements` stays EMPTY, so a delete after it
// can never reach part of a list.
//
// NOTHING NAMED, NOTHING HANDED ON - the conflict, and the safer rule kept.
// SELECT_FAMILY_FORMS refused a call that named no form; this card reads
// every form when `which` is left out (row 5b-488). Both stand: left out,
// `which` binds the card's own "not named" - every form is READ, as before,
// and NONE is handed on, so a delete chained after it has nothing to delete.
// Typing "all" names every form and hands them all on.
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
//
// OR THE IDS THE FORM TOOLS GAVE BACK (version 3, row 5b-1312) - the long
// UniqueId each answers with and this lists, commas between - so one form's
// extents are read after a flex without the rest. Read as SELECT_FAMILY_FORMS
// reads them: a part that is not a kind or side is an id; ids and words are not
// mixed in one call; and one id that is not a form of this family refuses the
// whole call, each bad one named, before anything is read. The short number
// Select by ID shows is refused with that said - an ElementId built from typed
// text is the constructor that changed at 2024.

var findings = new List<string>();
var formCount = 0;
var formList = "";
var combinationList = "";
var scanned = 0;
var notAFamily = false;
// VERSION 4, from SELECT_FAMILY_FORMS.
var elements = new List<Element>();
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep"
    : f is FreeFormElement ? "freeform" : "form";

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

var known = new[] { "solid", "void", "extrusion", "revolve", "revolution", "blend", "sweep", "sweptblend",
                    "freeform" };
// LEFT OUT, `which` is the card's "not named": every form read, none handed
// on (version 4). Anything typed - "all" included - names what is handed on.
var notNamed = squash(which) == "notnamed";
var parts = (which ?? "").Split(',').Select(p => p.Trim())
    .Where(p => squash(p).Length > 0 && squash(p) != "all" && squash(p) != "notnamed").ToList();
var words = parts.Select(squash).Where(w => known.Contains(w)).ToList();
var ids = parts.Where(p => !known.Contains(squash(p))).ToList();

var rows = new List<string>();
var joins = new List<string>();
var named = new HashSet<string>();
// The forms named by id, in the order named, each once - the order `elements`
// hands them on in (SELECT_FAMILY_FORMS' rule).
var namedInOrder = new List<GenericForm>();
var problems = new List<string>();

if (doc.IsFamilyDocument && words.Count == 0)
{
    foreach (var id in ids)
    {
        Element element = null;
        try { element = doc.GetElement(id); }
        catch (Exception) { element = null; }

        var form = element as GenericForm;
        if (element == null)
            problems.Add("\"" + id + "\" is not an id in this family - "
                + (id.All(char.IsDigit)
                    ? "that is an element number; give the long id the form tools gave back"
                    : "nor a kind of form or a side (solid, void, extrusion, revolve, blend, sweep, swept blend, "
                      + "freeform)"));
        else if (form == null)
            problems.Add("\"" + id + "\" is "
                + (string.IsNullOrEmpty(element.Name) ? "an element" : "\"" + element.Name + "\"")
                + (element.Category != null ? " (" + element.Category.Name + ")" : "") + ", not a form");
        else if (named.Add(form.UniqueId))
            namedInOrder.Add(form);
    }
}

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's forms are "
        + "read inside the family - open it first.";
    findings.Add(refused);
}
else if (words.Count > 0 && ids.Count > 0)
{
    refused = "Nothing was read. Name forms by their ids OR by kind and side, not both in one call - \""
        + string.Join(", ", ids) + "\" read as ids beside \"" + string.Join(", ", words) + "\". A word that is "
        + "neither is read as an id: name solid or void, and extrusion, revolve, blend, sweep, swept blend or "
        + "freeform, or all.";
    findings.Add(refused);
}
else if (problems.Count > 0)
{
    refused = "Nothing was read. " + string.Join("; ", problems) + ".";
    findings.Add(refused);
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
        if (!matches || (ids.Count > 0 && !named.Contains(f.UniqueId))) continue;
        if (ids.Count == 0 && !notNamed) elements.Add(f);

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

    // BY ID, IN THE ORDER NAMED (version 4) - the order a caller listed them
    // in is the order they are handed on.
    if (ids.Count > 0) foreach (var f in namedInOrder) elements.Add(f);

    formCount = rows.Count;
    formList = string.Join("  ||  ", rows);
    combinationList = string.Join("  ||  ", joins);
    findings.Add(formCount + " form(s) of " + scanned + " in the family" + (words.Count > 0 ? " match \""
        + string.Join(", ", words) + "\"" : ids.Count > 0 ? ", by id" : "") + "; " + joins.Count + " join(s) or cut(s).");
    if (scanned == 0) findings.Add("This family has no forms yet - nothing has been modelled in it.");
    findings.Add(notNamed
        ? "No form was named, so none is handed on - every form was read. Name them by id, by kind and side, or "
          + "say all, to hand them to SET_SELECTION or DELETE_ELEMENTS."
        : elements.Count + " form(s) handed on as elements - SET_SELECTION highlights them; DELETE_ELEMENTS "
          + "deletes them once the modeller has said yes.");
}
