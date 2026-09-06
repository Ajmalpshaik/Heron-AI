// NOT STANDALONE. Assumes `doc`, `categories` and `inViewOnly` are in scope,
// and leaves `elements`, `perCategory` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ONE PASS, NOT ONE PER CATEGORY.
//
// A collector per category walks the document once each and then hands back
// lists that have to be joined and de-duplicated by hand. A multi-category
// filter asks the same question once - faster, and structurally unable to
// return the same element twice.
//
// THE BREAKDOWN IS THE PART THAT CATCHES THE MISTAKE.
//
// A total of 480 does not say whether the flex duct came in. Per category it
// is obvious: no flex duct means either the model has none or the category was
// left out of the list, and those need different actions. A category asked for
// and empty is reported as zero rather than left out, because the absence is
// the finding.

var elements = new List<Element>();
var perCategory = new Dictionary<string, int>();

var wanted = new List<BuiltInCategory>();
if (categories != null)
{
    foreach (var category in categories) wanted.Add(category);
}

if (wanted.Count > 0)
{
    // Every category asked for starts at zero, so one with nothing in it is
    // still in the report.
    foreach (var category in wanted)
    {
        var id = new ElementId(category);
        var definition = Category.GetCategory(doc, category);
        string name = definition != null ? definition.Name : category.ToString();
        if (!perCategory.ContainsKey(name)) perCategory[name] = 0;
    }

    var collector = inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc);

    foreach (var element in collector
                 .WhereElementIsNotElementType()
                 .WherePasses(new ElementMulticategoryFilter(wanted)))
    {
        if (element == null) continue;
        elements.Add(element);

        string name = "(no category)";
        try { if (element.Category != null) name = element.Category.Name ?? name; } catch { }
        if (perCategory.ContainsKey(name)) perCategory[name] = perCategory[name] + 1;
        else perCategory[name] = 1;
    }
}

int found = elements.Count;
