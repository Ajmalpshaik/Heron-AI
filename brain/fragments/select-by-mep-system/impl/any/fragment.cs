// NOT STANDALONE. Assumes `doc`, `matchOn` and `systemText` are in scope;
// leaves `elements`, `withoutSystem` and `findings` behind.
//
// NAME AND TYPE ARE DIFFERENT QUESTIONS AND THE CALLER SAYS WHICH. The type is
// the classification - Supply Air, Domestic Cold Water, or the project's short
// code. The name is the particular run - SA 1, DXS 2. Several named systems
// share one type, which is the exact difference somebody is checking.
//
// THEY LIVE ON DIFFERENT PARAMETERS, AND GETTING IT WRONG IS SILENT. Reading
// RBS_SYSTEM_NAME_PARAM first and "falling back" to the type parameter never
// reaches the fallback: the name parameter is present on nearly every duct and
// pipe, so such a fragment matches NAMES while reporting TYPES.
//
// ONE MULTI-CATEGORY PASS, NOT FOUR COLLECTORS UNITED. FilteredElementCollector
// .UnionWith does NOT carry each side's own quick filters into the merged
// result - an instances-only filter applied per side is silently dropped and
// TYPE elements arrive beside real ones, measured at 52 where 4 were right.
// A single ElementMulticategoryFilter cannot have that problem.

var elements = new List<Element>();
var withoutSystem = 0;
var findings = new List<string>();

var how = string.IsNullOrEmpty(matchOn) ? "" : matchOn.Trim().ToLowerInvariant();
var needle = string.IsNullOrEmpty(systemText) ? "" : systemText.Trim();

if (how != "name" && how != "type")
{
    findings.Add("Say whether to match the system NAME - one run, like 'DXS 1' - or the system TYPE, "
        + "the classification every run of that kind shares. Several named systems share one type, so "
        + "the two answer different questions and neither is a safe guess");
}
else if (needle.Length == 0)
{
    findings.Add("No system text was given - say which system name or system type to look for");
}
else
{
    var categories = new List<BuiltInCategory>
    {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PipeFitting,
    };

    var collector = new FilteredElementCollector(doc)
        .WherePasses(new ElementMulticategoryFilter(categories))
        .WhereElementIsNotElementType();

    Func<Element, string> systemTextOf = element =>
    {
        if (how == "name")
        {
            var p = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM);
            if (p == null) return null;
            return p.AsString() ?? p.AsValueString();
        }

        // The type parameter holds an ElementId pointing at the system type
        // element. Its DISPLAY NAME is where the project's short code lives, so
        // it is read as a value string and never as a number.
        var typeParam = element.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)
                     ?? element.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
        if (typeParam == null) return null;
        return typeParam.AsValueString();
    };

    var scanned = 0;
    var leaked = 0;

    foreach (var element in collector)
    {
        scanned++;

        // Belt and braces on the trap above: if a type element ever reaches
        // here, it is counted and named rather than quietly answered with.
        if (element is ElementType) { leaked++; continue; }

        string value = null;
        try { value = systemTextOf(element); }
        catch { value = null; }

        if (string.IsNullOrEmpty(value)) { withoutSystem++; continue; }

        if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) elements.Add(element);
    }

    findings.Add(string.Format("{0} of {1} scanned duct/pipe element(s) have a system {2} containing "
        + "'{3}'. {4} are on no system at all",
        elements.Count, scanned, how, needle, withoutSystem));

    if (withoutSystem > 0)
        findings.Add("A duct or pipe on no system is usually one that is not actually connected. "
            + "FIND_SYSTEM_ISLANDS and TRACE_CONNECTIVITY are what diagnose that; it is counted here, "
            + "not explained here");

    if (leaked > 0)
        findings.Add(string.Format("{0} TYPE element(s) reached the instance loop and were dropped. "
            + "That should be impossible through a single multi-category filter - if it happens, the "
            + "collector is not behaving as this fragment assumes", leaked));
}
