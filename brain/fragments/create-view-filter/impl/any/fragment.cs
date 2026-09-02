// NOT STANDALONE. Assumes `doc`, `view`, `filterName`, `categoryIds`,
// `parameterId`, `matchValue` and `colour` are in scope; leaves `created` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FILTER IS NOT AN OVERRIDE, and the difference decides whether the drawing
// still reads correctly next month. An override is stuck to the element ids it
// was given; a filter matches. A duct drawn next week on the right system comes
// out the right colour by itself, and on a drawing that gets reissued that is
// the entire reason to use one.
//
// THE RULE FACTORY CHANGED, AND THE FIGURES BELOW WERE READ OFF THE REFERENCE
// ASSEMBLY FOR EACH RELEASE rather than remembered:
//
//   CreateEqualsRule(ElementId, string, bool)   2020 to 2025. REMOVED IN 2026
//                                               - the flag is case sensitivity
//   CreateEqualsRule(ElementId, string)         ADDED IN 2023
//
// The overlap is 2023 to 2025 and neither form covers the whole span, so this
// splits on a compile symbol - the same pattern FIND_UNTAGGED_ELEMENTS uses for
// the tag accessor Revit 2027 removed, and the second fragment here to need one.
//
// Splitting at 2023 rather than at 2026 is a choice: both compile everywhere,
// and taking the newer form from the first release that has it leaves the
// shorter-lived branch to maintain.
//
// >> THE COMMENT THAT USED TO BE HERE SAID "up to 2022" AND WAS WRONG - the old
// >> overload lived to 2025. The code was right, so all eight releases compiled
// >> green and nothing complained. A compile gate checks the call; it does not
// >> read the sentence beside it.
//
// >> FOR WHOEVER BUILDS THE EXECUTOR (D-28, unbuilt): this REQUIRES the
// >> in-process Roslyn compilation to define the same REVIT20xx symbol MSBuild
// >> does. A host defining none takes the #else branch and breaks on 2020 to
// >> 2022 - which includes the release the owner actually runs.
//
// THE NAME IS CHECKED FIRST. A filter lives in the PROJECT, not in the view, so
// a duplicate name collides with something another view may be relying on -
// and Revit throws, after which the view has been modified and the filter has
// not been made.

ElementId created = null;
string refused = null;

var name = (filterName ?? "").Trim();

if (view == null || view.IsTemplate)
{
    refused = "a filter is applied to a real view - a template is not one";
}
else if (name.Length == 0)
{
    refused = "a filter needs a name - it goes in the project's filter list, where "
            + "other views will see it";
}
else if (categoryIds == null || categoryIds.Count == 0)
{
    refused = "a filter needs at least one category to look at";
}
else if (parameterId == null || parameterId == ElementId.InvalidElementId)
{
    refused = "a filter needs a parameter to test - without one it matches everything "
            + "in the category, which is what SET_CATEGORY_GRAPHICS is for";
}
else
{
    ParameterFilterElement clash = null;

    foreach (var existing in new FilteredElementCollector(doc)
                                 .OfClass(typeof(ParameterFilterElement))
                                 .Cast<ParameterFilterElement>())
    {
        if (existing == null) continue;
        if (string.Equals(existing.Name ?? "", name, StringComparison.OrdinalIgnoreCase))
        {
            clash = existing;
            break;
        }
    }

    if (clash != null)
    {
        refused = string.Format(
            "a filter called \"{0}\" already exists in this project. Filters are "
            + "project-wide, so another view may be relying on it - and Revit throws on "
            + "the duplicate after the view has already been touched", name);
    }
    else
    {
#if REVIT2020 || REVIT2021 || REVIT2022
        // The third argument is case sensitivity. TRUE, because a system name
        // is a name: "SUPPLY" and "Supply" are two different system types in
        // any project that has both, and matching loosely colours the wrong one.
        var rule = ParameterFilterRuleFactory.CreateEqualsRule(
            parameterId, matchValue ?? "", true);
#else
        // 2023 onwards, where the flag is no longer offered.
        //
        // WHETHER THIS MATCHES CASE-SENSITIVELY IS NOT KNOWN HERE. The
        // overload simply has nowhere to say, and reflection over the assembly
        // cannot show behaviour - only the signature. So the two branches may
        // not agree on "supply air" against "Supply Air", and that difference
        // would appear only between Revit versions, which is the worst place
        // for one. It is the first thing to check when this fragment meets a
        // real model, and it is written in the test cases as such rather than
        // asserted here as if it were settled.
        var rule = ParameterFilterRuleFactory.CreateEqualsRule(
            parameterId, matchValue ?? "");
#endif

        var test = new ElementParameterFilter(rule);

        var filter = ParameterFilterElement.Create(doc, name, categoryIds, test);

        if (filter == null)
        {
            refused = "Revit declined to create the filter";
        }
        else
        {
            created = filter.Id;

            view.AddFilter(filter.Id);

            // Projection AND cut, because a duct in a plan is cut by the view
            // range and a duct in a 3D view is not. Setting only one leaves
            // half the drawing uncoloured, and which half depends on the view.
            var look = new OverrideGraphicSettings();
            look.SetProjectionLineColor(colour);
            look.SetCutLineColor(colour);
            look.SetSurfaceForegroundPatternColor(colour);
            look.SetCutForegroundPatternColor(colour);

            view.SetFilterOverrides(filter.Id, look);
        }
    }
}
