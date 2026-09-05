// NOT STANDALONE. Assumes `doc`, `optionName`, `optionSetName` and `categories`
// are in scope; leaves `elements`, `optionId` and `findings` behind.
//
// TWO DESIGN OPTIONS WITH ONE NAME IS THE NORMAL CASE, NOT AN EDGE CASE. Revit
// calls the primary option of EVERY option set "Option 1 (primary)", so a model
// with three sets has three options with that exact name. Taking the first
// match picks whichever the collector happened to return and reports success:
// the wrong answer, delivered with confidence. THIS REFUSES TO CHOOSE, and
// names the sets so the caller can say which one they meant.
//
// NO OPTION NAMED MEANS THE MAIN MODEL - and that is very nearly the whole
// file. The category bound matters more here than anywhere else in this batch.
//
// A MODEL WITH NO OPTIONS AT ALL IS STILL ANSWERABLE. "There are no design
// options" and "the Main Model is everything" are the same true statement, so
// only a NAMED option that cannot be found is refused.

var elements = new List<Element>();
var optionId = ElementId.InvalidElementId;
var findings = new List<string>();

var wantOption = string.IsNullOrEmpty(optionName) ? "" : optionName.Trim();
var wantSet = string.IsNullOrEmpty(optionSetName) ? "" : optionSetName.Trim();

var allOptions = new List<DesignOption>();
foreach (var option in new FilteredElementCollector(doc).OfClass(typeof(DesignOption)).Cast<DesignOption>())
    allOptions.Add(option);

Func<DesignOption, string> setNameOf = option =>
{
    var p = option.get_Parameter(BuiltInParameter.OPTION_SET_ID);
    if (p == null) return "";
    var owner = doc.GetElement(p.AsElementId());
    return owner == null ? "" : owner.Name;
};

DesignOption target = null;
var resolved = wantOption.Length == 0;   // no name asked for -> Main Model

if (!resolved)
{
    var matches = new List<DesignOption>();
    foreach (var option in allOptions)
    {
        if (!string.Equals(option.Name, wantOption, StringComparison.OrdinalIgnoreCase)) continue;
        if (wantSet.Length > 0 && !string.Equals(setNameOf(option), wantSet, StringComparison.OrdinalIgnoreCase))
            continue;
        matches.Add(option);
    }

    if (matches.Count == 0)
    {
        var listed = new List<string>();
        foreach (var option in allOptions)
            listed.Add(string.Format("'{0}' (set '{1}')", option.Name, setNameOf(option)));

        findings.Add(string.Format("No design option called '{0}'{1}. This model has: {2}",
            wantOption,
            wantSet.Length == 0 ? "" : string.Format(" in set '{0}'", wantSet),
            listed.Count == 0 ? "no design options at all" : string.Join(", ", listed.ToArray())));
    }
    else if (matches.Count > 1)
    {
        // DO NOT PICK ONE. Choosing here is how the wrong option gets filtered
        // and nobody finds out.
        var sets = new List<string>();
        foreach (var option in matches) sets.Add(string.Format("'{0}'", setNameOf(option)));

        findings.Add(string.Format("AMBIGUOUS: {0} design options are called '{1}', one in each of "
            + "these sets: {2}. Revit names the primary option of every set the same way, so this is "
            + "normal rather than a broken model. Name the option set to choose",
            matches.Count, wantOption, string.Join(", ", sets.ToArray())));
    }
    else
    {
        target = matches[0];
        optionId = target.Id;
        resolved = true;
    }
}

if (resolved)
{
    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    foreach (var element in collector)
    {
        DesignOption on = null;
        try { on = element.DesignOption; }
        catch { on = null; }

        if (target == null)
        {
            if (on == null) elements.Add(element);
        }
        else if (on != null && on.Id == target.Id)
        {
            elements.Add(element);
        }
    }

    var where = target == null
        ? "the Main Model (no design option)"
        : string.Format("design option '{0}' in set '{1}'", target.Name, setNameOf(target));

    findings.Add(string.Format("{0} element(s) in {1}{2}",
        elements.Count,
        where,
        categories == null || categories.Count == 0
            ? target == null
                ? " - across every category, which for the Main Model is very nearly the whole file"
                : ", across every category"
            : string.Format(", within {0} category/categories", categories.Count)));

    if (target != null && allOptions.Count > 1)
        findings.Add("Anything not listed here is in a sibling option or in the Main Model. Design "
            + "options are alternatives, so a smaller count than expected usually means the elements "
            + "are in the other option, not that they were dropped");
}
