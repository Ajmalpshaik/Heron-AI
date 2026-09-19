// NOT STANDALONE. Assumes `doc` is in scope; leaves `names`, `bindable` and
// `findings`.
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

var names = new List<string>();
var bindable = 0;
var findings = new List<string>();

var subTotal = 0;
var settings = doc.Settings;

if (settings == null || settings.Categories == null)
{
    findings.Add("This document reports no category settings at all, which is "
        + "not something a project should do");
}
else
{
    foreach (Category category in settings.Categories)
    {
        if (category == null) continue;

        var name = category.Name;
        if (string.IsNullOrEmpty(name)) continue;
        names.Add(name);

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
}
