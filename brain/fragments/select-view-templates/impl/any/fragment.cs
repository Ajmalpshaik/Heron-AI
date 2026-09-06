// NOT STANDALONE. Assumes `doc`, `nameContains` and `usage` are in scope;
// leaves `elements`, `matchedNames`, `findings`, `scanned` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A VIEW TEMPLATE IS A VIEW WITH IsTemplate SET, and that is why it never
// appears in an ordinary view query: every fragment here that walks views
// filters templates OUT. This one walks the same collection and keeps only
// them.
//
// USED MEANS TWO THINGS AND THE SECOND ONE IS INVISIBLE:
//
//   a view follows it              View.ViewTemplateId points at it
//   a view family type starts      ViewFamilyType.DefaultTemplateId names it
//   new views from it
//
// The second leaves no view pointing at the template, so a check that only
// walks views reports it unused. Deleting it then changes what every new plan
// of that kind is created with, and nothing on screen said so. Both reasons
// are collected, and both are named per template.
//
// AN UNRECOGNISED MODE RETURNS NOTHING RATHER THAN EVERYTHING. Falling back to
// "no filter" on a typo hands the whole list to whatever runs next, and what
// runs next is often a delete.

var elements = new List<Element>();
var matchedNames = new List<string>();
var findings = new List<string>();
var scanned = 0;

var mode = string.IsNullOrEmpty(usage) ? "all" : usage.Trim().ToLowerInvariant();

if (mode != "all" && mode != "used" && mode != "unused")
{
    findings.Add("Usage mode \"" + usage + "\" is not one of all, used, unused. "
        + "Nothing is returned - a mode that is not understood must not fall back to "
        + "every template, because the next step is often a delete.");
}
else
{
    // Which templates a real view follows. Templates themselves are skipped:
    // a template does not follow a template.
    var followedByViews = new Dictionary<ElementId, int>();

    // Which templates a view family type hands to new views. This is the use
    // nothing on screen shows.
    var defaultForViewTypes = new HashSet<ElementId>();

    var templates = new List<View>();

    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var view = candidate as View;
        if (view == null) continue;

        if (view.IsTemplate)
        {
            templates.Add(view);
            continue;
        }

        var followedId = view.ViewTemplateId;
        if (followedId == null || followedId == ElementId.InvalidElementId) continue;

        var already = 0;
        followedByViews.TryGetValue(followedId, out already);
        followedByViews[followedId] = already + 1;
    }

    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
    {
        var viewFamilyType = candidate as ViewFamilyType;
        if (viewFamilyType == null) continue;

        var defaultId = viewFamilyType.DefaultTemplateId;
        if (defaultId == null || defaultId == ElementId.InvalidElementId) continue;

        defaultForViewTypes.Add(defaultId);
    }

    var needle = string.IsNullOrEmpty(nameContains) ? null : nameContains;

    foreach (var template in templates)
    {
        scanned++;

        var name = template.Name;

        if (needle != null)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
        }

        var viewsFollowing = 0;
        followedByViews.TryGetValue(template.Id, out viewsFollowing);

        var isDefaultForAViewType = defaultForViewTypes.Contains(template.Id);
        var isUsed = viewsFollowing > 0 || isDefaultForAViewType;

        if (mode == "used" && !isUsed) continue;
        if (mode == "unused" && isUsed) continue;

        elements.Add(template);

        // The reason is carried with the name, because "unused" is the answer
        // somebody deletes on and the two reasons are not equally visible.
        var reason = isUsed
            ? (viewsFollowing > 0
                ? viewsFollowing + " view(s) follow it"
                : "no view follows it")
            : "nothing uses it";

        if (isDefaultForAViewType)
        {
            reason = reason + "; it is the template new views of a view type start with";
        }

        matchedNames.Add((string.IsNullOrEmpty(name) ? "(no name)" : name) + " - " + reason);
    }

    if (mode == "unused" && elements.Count > 0)
    {
        findings.Add(elements.Count + " template(s) have nothing using them. That is what is "
            + "unreferenced today, not what should go - a template kept for work not yet "
            + "drawn looks exactly the same from here.");
    }
}

var found = elements.Count;
