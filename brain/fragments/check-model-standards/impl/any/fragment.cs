// NOT STANDALONE. Assumes `doc`, `namePatterns`, `requiredOn`,
// `requiredParameters`, `suspectNames`, `skipViewTemplates` and `maxRows` are in
// scope; leaves `findings`, `failures`, `sectionsChecked`, `sectionsNotChecked`
// and `offenders` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE RULES ARE SUPPLIED, NOT KNOWN. A naming convention is project-specific, so
// a hard-coded one would be wrong on every job but one.
//
// WILDCARDS ONLY - `*` and literal text, case-insensitive. A regular expression
// would be more powerful and would move authorship of the standard away from the
// person who owns it.
//
// A SECTION WITH NO PATTERN IS "NOT CHECKED", NEVER A ZERO. A rule set that names
// no pattern for sheets does not mean the sheets are fine; it means nobody
// looked, and a reassuring zero is the defect this project exists around.
//
// BLANK AND ABSENT ARE SEPARATED. Empty is data entry; not on the category at
// all is project setup. One combined number sends the work to the wrong person.
//
// MEP SYSTEMS ARE COLLECTED BY CATEGORY. MEPSystem is abstract and Revit rejects
// an abstract type to OfClass at runtime, which would take the whole sweep down
// on exactly the workshared MEP model this is for.

var findings = new List<string>();
var failures = 0;
var sectionsChecked = 0;
var sectionsNotChecked = 0;
var offenders = new List<ElementId>();

// `*` and literal text only, case-insensitive. Written out rather than reached
// for as a regular expression - see the header.
Func<string, string, bool> matches = (text, pattern) =>
{
    text = text ?? "";
    if (string.IsNullOrEmpty(pattern)) return true;

    // A pattern with no '*' is a whole-name rule, and it has to be checked as
    // one. Without this line the loop below runs its first-part branch, which
    // tests only StartsWith, and then falls through to `return true` - so the
    // rule "Supply Diffuser" quietly ACCEPTED "Supply Diffuser OLD". A
    // standards check that passes the thing it exists to catch is worse than
    // no check, because somebody trusts it. Wildcards keep their old
    // behaviour: "Supply Diffuser*" still allows the suffix.
    if (pattern.IndexOf('*') < 0)
        return string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase);

    var parts = pattern.Split('*');
    var position = 0;

    for (var i = 0; i < parts.Length; i++)
    {
        var part = parts[i];
        if (part.Length == 0) continue;

        if (i == 0)
        {
            if (!text.StartsWith(part, StringComparison.OrdinalIgnoreCase)) return false;
            position = part.Length;
            continue;
        }

        if (i == parts.Length - 1 && !pattern.EndsWith("*"))
        {
            if (!text.EndsWith(part, StringComparison.OrdinalIgnoreCase)) return false;
            return text.Length - part.Length >= position;
        }

        var found = text.IndexOf(part, position, StringComparison.OrdinalIgnoreCase);
        if (found < 0) return false;
        position = found + part.Length;
    }

    return true;
};

Func<string, string> patternFor = key =>
{
    if (namePatterns == null) return "";
    string pattern = null;
    if (!namePatterns.TryGetValue(key, out pattern)) return "";
    return pattern ?? "";
};

Func<string, bool> looksSuspect = name =>
{
    if (suspectNames == null || string.IsNullOrEmpty(name)) return false;
    foreach (var suspect in suspectNames)
    {
        if (string.IsNullOrEmpty(suspect)) continue;
        if (name.IndexOf(suspect, StringComparison.OrdinalIgnoreCase) >= 0) return true;
    }
    return false;
};

// One section: a title, the pattern that governs it, the names examined, and the
// ids beside them. Reporting lives in one place so no section can quietly print
// a zero where it should print NOT CHECKED.
Action<string, string, IList<string>, IList<ElementId>> section =
    (title, pattern, names, ids) =>
{
    findings.Add("");
    findings.Add(title.ToUpper());

    if (names == null)
    {
        sectionsNotChecked++;
        findings.Add("  NOT CHECKED - nothing of this kind could be collected from the model");
        return;
    }

    var patternGiven = !string.IsNullOrEmpty(pattern);
    if (!patternGiven && (suspectNames == null || suspectNames.Count == 0))
    {
        sectionsNotChecked++;
        findings.Add("  NOT CHECKED - no pattern was given for " + title.ToLower()
            + ", and no suspect names either. THIS IS NOT A PASS: nobody looked. "
            + names.Count + " were present");
        return;
    }

    sectionsChecked++;
    var bad = 0;
    var shown = 0;

    for (var i = 0; i < names.Count; i++)
    {
        var name = names[i] ?? "";
        var breaksPattern = patternGiven && !matches(name, pattern);
        var suspect = looksSuspect(name);
        if (!breaksPattern && !suspect) continue;

        bad++;
        failures++;
        if (ids != null && i < ids.Count) offenders.Add(ids[i]);

        if (shown < maxRows)
        {
            shown++;
            findings.Add("  " + (breaksPattern ? "does not match '" + pattern + "'" : "suspect name")
                + ": '" + name + "'"
                + (ids != null && i < ids.Count ? " (id " + ids[i] + ")" : ""));
        }
    }

    if (bad > shown)
        findings.Add("  ... " + (bad - shown) + " more, not listed. The count is complete");

    findings.Add("  " + bad + " of " + names.Count + " fail"
        + (patternGiven ? ", against '" + pattern + "'" : ", on suspect names only - NO pattern was "
            + "given, so a correctly-shaped wrong name passes here"));
};

// ---- views ----
var viewNames = new List<string>();
var viewIds = new List<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var view = element as View;
    if (view == null) continue;
    if (view is ViewSheet) continue;                        // sheets have their own section
    if (skipViewTemplates && view.IsTemplate) continue;
    // Revit's own internal views are named by nobody, and flagging them buries
    // the findings a person can act on.
    if (view.ViewType == ViewType.ProjectBrowser
        || view.ViewType == ViewType.SystemBrowser
        || view.ViewType == ViewType.Internal
        || view.ViewType == ViewType.Undefined) continue;

    viewNames.Add(view.Name);
    viewIds.Add(view.Id);
}
section("Views", patternFor("view"), viewNames, viewIds);

// ---- sheets ----
var sheetNumbers = new List<string>();
var sheetIds = new List<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
{
    var sheet = element as ViewSheet;
    if (sheet == null) continue;
    sheetNumbers.Add(sheet.SheetNumber);
    sheetIds.Add(sheet.Id);
}
section("Sheet numbers", patternFor("sheet"), sheetNumbers, sheetIds);

// ---- levels ----
var levelNames = new List<string>();
var levelIds = new List<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
{
    if (element == null) continue;
    levelNames.Add(element.Name);
    levelIds.Add(element.Id);
}
section("Levels", patternFor("level"), levelNames, levelIds);

// ---- worksets ----
if (!doc.IsWorkshared)
{
    findings.Add("");
    findings.Add("WORKSETS");
    findings.Add("  NOT CHECKED - this model is not workshared, so it has no user worksets. That is a "
        + "fact about the model, not a pass");
    sectionsNotChecked++;
}
else
{
    var worksetNames = new List<string>();
    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
    {
        if (workset == null) continue;
        worksetNames.Add(workset.Name);
    }
    // NO IDS FOR WORKSETS, ON PURPOSE. A WorksetId is not an ElementId, and
    // padding the list with InvalidElementId would put ids into `offenders` that
    // resolve to nothing - a caller selecting them would select nothing and read
    // that as the finding being wrong.
    section("Worksets", patternFor("workset"), worksetNames, null);
}

// ---- MEP system names ----
var systemCategories = new List<ElementId>();
systemCategories.Add(new ElementId(BuiltInCategory.OST_DuctSystem));
systemCategories.Add(new ElementId(BuiltInCategory.OST_PipingSystem));

var systemNames = new List<string>();
var systemIds = new List<ElementId>();
foreach (var element in new FilteredElementCollector(doc)
    .WherePasses(new ElementMulticategoryFilter(systemCategories))
    .WhereElementIsNotElementType())
{
    if (element == null) continue;
    systemNames.Add(element.Name);
    systemIds.Add(element.Id);
}
section("MEP system names", patternFor("system"), systemNames, systemIds);

// ---- the parameters the project requires ----
findings.Add("");
findings.Add("REQUIRED PARAMETERS");

if (requiredOn == null || requiredOn.Count == 0
    || requiredParameters == null || requiredParameters.Count == 0)
{
    sectionsNotChecked++;
    findings.Add("  NOT CHECKED - no categories or no parameter names were given. Nothing here says "
        + "the data is complete");
}
else
{
    sectionsChecked++;

    var categoryIds = new List<ElementId>();
    var categoryNames = new List<string>();
    foreach (var category in requiredOn)
    {
        if (category == null) continue;
        categoryIds.Add(category.Id);
        categoryNames.Add(category.Name);
    }

    if (categoryIds.Count == 0)
    {
        findings.Add("  NOT CHECKED - every category handed in was null");
    }
    else
    {
        var subjects = new FilteredElementCollector(doc)
            .WherePasses(new ElementMulticategoryFilter(categoryIds))
            .WhereElementIsNotElementType()
            .ToElements();

        foreach (var parameterName in requiredParameters)
        {
            if (string.IsNullOrEmpty(parameterName)) continue;

            var blank = 0;
            var absent = 0;
            var filled = 0;
            var shown = 0;

            foreach (var element in subjects)
            {
                if (element == null) continue;

                var parameter = element.LookupParameter(parameterName);
                if (parameter == null)
                {
                    var type = doc.GetElement(element.GetTypeId()) as ElementType;
                    if (type != null) parameter = type.LookupParameter(parameterName);
                }

                if (parameter == null)
                {
                    absent++;
                    failures++;
                    offenders.Add(element.Id);
                    if (shown < maxRows)
                    {
                        shown++;
                        findings.Add("  '" + parameterName + "' is NOT ON " + element.Name
                            + " (id " + element.Id + ") at all - that is project setup, not data entry");
                    }
                    continue;
                }

                var value = parameter.AsString();
                if (string.IsNullOrEmpty(value)) value = parameter.AsValueString();
                if (!parameter.HasValue || string.IsNullOrEmpty(value))
                {
                    blank++;
                    failures++;
                    offenders.Add(element.Id);
                    if (shown < maxRows)
                    {
                        shown++;
                        findings.Add("  '" + parameterName + "' is BLANK on " + element.Name
                            + " (id " + element.Id + ") - that is data entry");
                    }
                    continue;
                }

                filled++;
            }

            findings.Add("  " + parameterName + ": " + filled + " filled, " + blank + " BLANK, "
                + absent + " NOT PRESENT on the element at all. Those last two go to different "
                + "people and are counted apart on purpose");
        }

        findings.Add("  categories covered: " + string.Join(", ", categoryNames.ToArray())
            + " (" + subjects.Count + " element(s))");
    }
}

findings.Insert(0, string.Format("{0} failure(s) across {1} section(s) checked, {2} section(s) NOT "
    + "CHECKED. Model: {3}. A SECTION NOBODY GAVE A RULE FOR IS NOT A SECTION THAT PASSED - read the "
    + "NOT CHECKED lines before the counts. This checks CONVENTIONS, not correctness: a view named "
    + "perfectly can still show the wrong thing",
    failures, sectionsChecked, sectionsNotChecked,
    string.IsNullOrEmpty(doc.Title) ? "(unsaved)" : doc.Title));
