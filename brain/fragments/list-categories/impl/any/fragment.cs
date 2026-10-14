// NOT STANDALONE. Assumes `doc` and `everySubcategory` are in scope; leaves
// `names`, `bindable`, `objectStyles`, `familySubcategories` and `findings`.
//
// A READ. It opens no transaction and needs none.
//
// NOTHING LISTED THESE. SELECT_BY_CATEGORY_NAME returns near misses once you
// have guessed wrong, which is not a list, so "what is even in here" had no
// answer at all.
//
// THE NAMES ARE LOCALISED. On a French or Arabic Revit they come back in that
// language, and that is the point rather than a defect: this is what tells
// somebody which names SELECT_BY_CATEGORY_NAME will accept on THIS machine.
//
// ITERATED, NOT INDEXED. Revit promises no ordering for Categories.
//
// VERSION 2 - OBJECT STYLES (FRAGMENT-ISSUES 5b-464). Every category's own
// graphics as Manage > Object Styles shows them: the projection line colour,
// the projection line weight, the cut line weight where the category is cut,
// and the projection line pattern. Read off the category itself, never off a
// view, so a view's override is not in it - that is REPORT_CATEGORY_OVERRIDES.
//
// ONE STRING, ONE LINE PER ROW. A reply shows a list as its first three items,
// each cut to sixty characters, and a string whole - so `objectStyles` is the
// answer, and `names` stays the list it was for anything that consumes it.
//
// IN A FAMILY, THE FAMILY'S OWN CATEGORY AND EVERY SUBCATEGORY OF IT COME
// FIRST - the rows its own lines and forms sit on, Object Styles in the Family
// Editor. Every other category follows as its own row, sorted by name; their
// subcategories only with everySubcategory, because a project has hundreds and
// a reply would carry every one. Left out, everySubcategory is false
// (HeronBindingNote.AbsentValue), and names, bindable and the first finding
// are exactly version 1's.
//
// AN INTERNAL CATEGORY HAS NO ROW. Object Styles shows model, annotation and
// analytical categories; an internal one is counted and left out.

var names = new List<string>();
var bindable = 0;
var findings = new List<string>();
var objectStyles = "";
var familySubcategories = 0;

var subTotal = 0;
var settings = doc.Settings;

if (settings == null || settings.Categories == null)
{
    findings.Add("This document reports no category settings at all, which is "
        + "not something a project should do");
}
else
{
    var all = new List<Category>();
    foreach (Category category in settings.Categories)
    {
        if (category == null) continue;

        var name = category.Name;
        if (string.IsNullOrEmpty(name)) continue;
        names.Add(name);
        all.Add(category);

        try
        {
            if (category.AllowsBoundParameters) bindable++;
        }
        catch
        {
        }

        try
        {
            if (category.SubCategories != null) subTotal += category.SubCategories.Size;
        }
        catch
        {
        }
    }

    names.Sort(StringComparer.OrdinalIgnoreCase);

    findings.Add(string.Format(
        "{0} category/categories in this document, {1} of which can carry a "
        + "project parameter, with {2} sub-category/sub-categories between them. "
        + "These names are LOCALISED - on a non-English Revit they are the names "
        + "that machine will accept, and the internal identifiers are not",
        names.Count, bindable, subTotal));

    // OBJECT STYLES - one row per category or subcategory.
    var solid = LinePatternElement.GetSolidPatternId();
    var unreadable = 0;

    Func<Category, string, string> row = (category, label) =>
    {
        string colourText, projection, cut, pattern;
        try
        {
            var c = category.LineColor;
            colourText = c == null || !c.IsValid ? "none" : c.Red + "," + c.Green + "," + c.Blue;
        }
        catch (Exception) { colourText = "unreadable"; unreadable++; }
        try
        {
            var w = category.GetLineWeight(GraphicsStyleType.Projection);
            projection = w == null ? "none" : w.Value.ToString();
        }
        catch (Exception) { projection = "unreadable"; unreadable++; }
        try
        {
            if (!category.IsCuttable) cut = "-";
            else
            {
                var w = category.GetLineWeight(GraphicsStyleType.Cut);
                cut = w == null ? "none" : w.Value.ToString();
            }
        }
        catch (Exception) { cut = "unreadable"; unreadable++; }
        try
        {
            var id = category.GetLinePatternId(GraphicsStyleType.Projection);
            if (id == null || id == ElementId.InvalidElementId) pattern = "none";
            else if (id == solid) pattern = "Solid";
            else
            {
                var element = doc.GetElement(id);
                pattern = element == null ? "none" : element.Name;
            }
        }
        catch (Exception) { pattern = "unreadable"; unreadable++; }
        return label + " | " + colourText + " | " + projection + " | " + cut + " | " + pattern;
    };

    Func<Category, List<Category>> subcategoriesOf = category =>
    {
        var found = new List<Category>();
        try
        {
            if (category.SubCategories != null)
            {
                foreach (Category sub in category.SubCategories)
                {
                    if (sub != null && !string.IsNullOrEmpty(sub.Name)) found.Add(sub);
                }
            }
        }
        catch (Exception) { unreadable++; }
        found.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
        return found;
    };

    // THE FAMILY'S OWN CATEGORY, in a family open in the Family Editor.
    Category own = null;
    if (doc.IsFamilyDocument)
    {
        try
        {
            own = doc.OwnerFamily == null ? null : doc.OwnerFamily.FamilyCategory;
        }
        catch (Exception) { unreadable++; }
    }

    var rows = new List<string>();
    if (own != null)
    {
        rows.Add(row(own, own.Name));
        foreach (var sub in subcategoriesOf(own))
        {
            rows.Add(row(sub, own.Name + " > " + sub.Name));
            familySubcategories++;
        }
    }

    all.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
    var internalLeftOut = 0;
    var subcategoriesLeftOut = 0;
    foreach (var category in all)
    {
        if (own != null && category.Id == own.Id) continue;

        CategoryType type;
        try { type = category.CategoryType; }
        catch (Exception) { type = CategoryType.Invalid; }
        if (type != CategoryType.Model && type != CategoryType.Annotation
            && type != CategoryType.AnalyticalModel)
        {
            internalLeftOut++;
            continue;
        }

        rows.Add(row(category, category.Name));
        var subs = subcategoriesOf(category);
        if (everySubcategory)
        {
            foreach (var sub in subs) rows.Add(row(sub, category.Name + " > " + sub.Name));
        }
        else
        {
            subcategoriesLeftOut += subs.Count;
        }
    }

    // THE FIRST LINE SAYS WHAT THE ROWS ARE, because the findings below reach
    // a reply cut to sixty characters and this string reaches it whole.
    string scope;
    if (doc.IsFamilyDocument && own == null)
        scope = "This family reports no category of its own, so no row is its own.";
    else if (own != null)
        scope = "This family's own category, " + own.Name + ", and its " + familySubcategories
            + " subcategory(ies) are the first " + (familySubcategories + 1) + " row(s)"
            + (familySubcategories == 0 ? " - it has no subcategories, so its own row stands alone." : ".");
    else if (everySubcategory)
        scope = "This is a project: every category, sorted by name, each followed by its subcategories.";
    else
        scope = "This is a project: one row per category, sorted by name.";

    var leftOut = "";
    if (subcategoriesLeftOut > 0)
        leftOut += " " + subcategoriesLeftOut + " subcategory(ies) of the other categories are not "
            + "listed - everySubcategory=true lists every one.";
    if (internalLeftOut > 0)
        leftOut += " " + internalLeftOut + " internal category(ies) have no Object Styles row and are left out.";
    if (unreadable > 0)
        leftOut += " " + unreadable + " value(s) could not be read and say unreadable.";

    objectStyles = "Object Styles, " + rows.Count + " row(s), read off each category - not a view's "
        + "override. " + scope + leftOut + "\n"
        + "category > subcategory | projection line colour R,G,B | projection line weight | "
        + "cut line weight (- not cut) | projection line pattern\n"
        + string.Join("\n", rows.ToArray());

    findings.Add(scope + leftOut);
}
