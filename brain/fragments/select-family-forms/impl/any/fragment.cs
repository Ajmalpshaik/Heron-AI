// NOT STANDALONE. Assumes `doc` and `forms` are in scope; leaves `elements`,
// `formList`, `scanned`, `notAFamily`, `refused` and `findings` behind.
//
// READS ONLY. Nothing is changed and nothing is highlighted - SET_SELECTION
// does that with what this leaves, and DELETE_ELEMENTS deletes it.
//
// FORMS OF THE FAMILY OPEN IN THE FAMILY EDITOR, named one of two ways:
//
//   by the ids the form tools gave back - the long UniqueId each answers with
//   as `formId`, and REPORT_FAMILY_FORMS lists - commas between; or
//
//   by kind and side, as REPORT_FAMILY_FORMS narrows: "all", "void", "solid,
//   revolve". A form is kept when it matches every word given.
//
// Not both in one call: "void, <id>" could mean the void among those ids or
// that id and every void, and guessing between them hands a delete the wrong
// set.
//
// ALL OR NOTHING. An id that resolves to nothing in this family, or to
// something that is not a form, refuses the whole call with every bad one
// named, and `elements` is left EMPTY - a delete chained after a partial list
// would remove some of what was meant and report success.
//
// THE SHORT NUMBER IS NOT TAKEN. Revit's Select by ID shows an element number;
// that is the file's bookkeeping, and an ElementId built from typed text is the
// constructor that changed at 2024. The UniqueId needs no constructing.

var elements = new List<Element>();
var formList = "";
var scanned = 0;
var notAFamily = false;
string refused = null;
var findings = new List<string>();

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep"
    : f is FreeFormElement ? "freeform" : "form";

var known = new[] { "all", "solid", "void", "extrusion", "revolve", "revolution", "blend", "sweep",
                    "sweptblend", "freeform" };

var parts = (forms ?? "").Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
var words = parts.Where(p => known.Contains(squash(p))).Select(squash).ToList();
var ids = parts.Where(p => !known.Contains(squash(p))).ToList();

var picked = new List<GenericForm>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document is a project, not a family open in the Family Editor. Forms live inside a family - "
        + "open it, or name it with --in.";
}
else
{
    var all = new FilteredElementCollector(doc).OfClass(typeof(GenericForm)).Cast<GenericForm>().ToList();
    scanned = all.Count;

    if (parts.Count == 0)
        refused = "No form was named. Give the ids the form tools gave back, commas between, or a kind or side - "
            + "all, solid, void, extrusion, revolve, blend, sweep, swept blend, freeform.";
    else if (words.Count > 0 && ids.Count > 0)
        refused = "Name forms by their ids OR by kind and side, not both in one call - \""
            + string.Join(", ", ids) + "\" read as ids beside \"" + string.Join(", ", words) + "\".";
    else if (ids.Count > 0)
    {
        var problems = new List<string>();
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
                        : "nor a kind or side (solid, void, extrusion, revolve, blend, sweep, swept blend, freeform)"));
            else if (form == null)
                problems.Add("\"" + id + "\" is "
                    + (string.IsNullOrEmpty(element.Name) ? "an element" : "\"" + element.Name + "\"")
                    + (element.Category != null ? " (" + element.Category.Name + ")" : "") + ", not a form");
            else if (!picked.Any(p => p.Id == form.Id))
                picked.Add(form);
        }
        if (problems.Count > 0)
        {
            refused = "Nothing was picked. " + string.Join("; ", problems) + ".";
            picked.Clear();
        }
    }
    else
    {
        var narrow = words.Where(w => w != "all").ToList();
        foreach (var f in all.OrderBy(x => kindOf(x)).ThenBy(x => x.UniqueId))
        {
            var kind = squash(kindOf(f));
            var side = f.IsSolid ? "solid" : "void";
            if (narrow.All(w => w == side || w == kind || (w == "revolution" && kind == "revolve")))
                picked.Add(f);
        }
    }
}

foreach (var f in picked) elements.Add(f);
formList = string.Join("  ||  ", picked.Select(f => (f.IsSolid ? "solid " : "void ") + kindOf(f) + " " + f.UniqueId));

if (refused != null)
    findings.Add(refused);
else
{
    findings.Add(elements.Count + " form(s) picked of " + scanned + " in the family"
        + (words.Count > 0 ? ", matching \"" + string.Join(", ", words) + "\"" : ", by id")
        + ". SET_SELECTION highlights them; DELETE_ELEMENTS deletes them.");
    if (scanned == 0) findings.Add("This family has no forms yet - nothing has been modelled in it.");
}
