// NOT STANDALONE. Assumes `doc`, `uidoc`, `forms` and `subcategory` are in
// scope; leaves `changed`, `createdSubcategory`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// PUTS FORMS ON A SUBCATEGORY of the family's own category - the Subcategory
// field in Properties - so a project can colour, hide or line-weight that part
// of the family on its own: the insulation, the hidden parts, the casing.
//
// A SUBCATEGORY THE FAMILY DOES NOT HAVE YET IS MADE, under the family's
// category, and said to have been made - Object Styles in the Family Editor,
// the same place a modeller makes one by hand. One whose name differs only by
// case from one already there is that one, never a second. "none" puts the
// forms back on the family's category itself, which Revit's remarks name as
// the other value the field takes.
//
// A CATEGORY THAT TAKES NO SUBCATEGORIES IS REFUSED BY NAME, from Revit's own
// flag for it, before anything is made.
//
// READ BACK, ALL OR NOTHING. Each form's subcategory is read again after it is
// set; one that does not read as asked fails the call and nothing is kept.

var findings = new List<string>();
var changed = new List<string>();
var createdSubcategory = false;
var notAFamily = false;
string refused = null;

var targets = new List<GenericForm>();
var problems = new List<string>();
Category parent = null;
Category wanted = null;
var wantedName = (subcategory ?? "").Trim();
var backToCategory = string.Equals(wantedName, "none", StringComparison.OrdinalIgnoreCase);

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep" : "form";
Func<Category, string> nameOf = c => c == null ? "(none)" : c.Name;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A form's subcategory is "
        + "set inside the family - open it first.";
}
else
{
    var said = (forms ?? "").Trim();
    var named = new List<Element>();
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the forms by their ids.");
        else
            foreach (var id in uidoc.Selection.GetElementIds())
            {
                var element = doc.GetElement(id);
                if (element != null) named.Add(element);
            }
    }
    else
        foreach (var part in said.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var element = doc.GetElement(part);
            if (element == null) problems.Add("No element in this family has the id \"" + part + "\".");
            else named.Add(element);
        }

    foreach (var element in named)
    {
        var f = element as GenericForm;
        if (f == null)
            problems.Add("\"" + (element.Name ?? element.UniqueId) + "\" (" + element.UniqueId + ") is not a form - "
                + "an extrusion, revolve, blend, sweep or swept blend.");
        else if (!targets.Any(t => t.Id == f.Id)) targets.Add(f);
    }
    if (problems.Count == 0 && targets.Count == 0)
        problems.Add("No form was named. Name them by the ids the form tools gave back, commas between, or select "
            + "them and say \"selected\".");

    parent = doc.OwnerFamily == null ? null : doc.OwnerFamily.FamilyCategory;
    if (parent == null) problems.Add("This family has no category to hold a subcategory.");
    else if (wantedName.Length == 0) problems.Add("No subcategory was named. Name one, or \"none\" for the family's "
        + "own category, " + parent.Name + ".");
    else if (backToCategory) wanted = parent;
    else
    {
        foreach (Category sub in parent.SubCategories)
            if (string.Equals(sub.Name, wantedName, StringComparison.OrdinalIgnoreCase)) { wanted = sub; break; }
        if (wanted == null && !parent.CanAddSubcategory)
            problems.Add("The family's category, " + parent.Name + ", takes no subcategories, so \"" + wantedName
                + "\" cannot be made in it.");
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

if (refused == null)
{
    if (wanted == null)
    {
        try
        {
            wanted = doc.Settings.Categories.NewSubcategory(parent, wantedName);
            createdSubcategory = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not make the subcategory \"" + wantedName + "\" under "
                + parent.Name + ": " + ex.Message + " NOTHING from this call was kept.");
        }
    }

    foreach (var f in targets)
    {
        var before = nameOf(f.Subcategory);
        try
        {
            f.Subcategory = wanted;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not put the " + kindOf(f) + " " + f.UniqueId + " on \""
                + wanted.Name + "\": " + ex.Message + " NOTHING from this call was kept.");
        }
        // Put back on the family's category, a form may read that category or
        // nothing at all - both mean the same field in Properties.
        var now = f.Subcategory;
        var reads = now == null ? (backToCategory ? wanted.Id : ElementId.InvalidElementId) : now.Id;
        if (reads != wanted.Id)
            throw new InvalidOperationException("The " + kindOf(f) + " " + f.UniqueId + " reads \"" + nameOf(now)
                + "\" after being put on \"" + wanted.Name + "\". NOTHING from this call was kept.");
        changed.Add(kindOf(f) + " " + f.UniqueId + ": " + before + " -> " + wanted.Name);
    }

    findings.Add((createdSubcategory ? "Made the subcategory \"" + wanted.Name + "\" under " + parent.Name + ", and put "
        : "Put ") + changed.Count + " form(s) on \"" + wanted.Name + "\": " + string.Join("; ", changed) + ".");
    if (createdSubcategory)
        findings.Add("Its line weights, colour and material are Revit's defaults until set in Object Styles.");
}

if (refused != null) findings.Add(refused);
