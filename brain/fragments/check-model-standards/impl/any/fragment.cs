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
//
// VERSION 2 - FOUR MORE KEYS IN `namePatterns`, EVERY ONE OPTIONAL. They ride in
// the table the request already supplies rather than as new needs, because the
// binder refuses an absent request need of any type but an optional bool
// (HeronBindingNote.AbsentValue) - a new list here would have stopped every
// existing caller. Each one is NOT CHECKED when absent, never a zero:
//
//   type=<pattern>          type names, as "Family: Type" - no space before the
//                           colon - over the TYPES of the categories in
//                           `requiredOn`
//   room=<pattern>          room names, read from the room's Name parameter (the
//                           element name carries the number as well)
//   value:<Parameter>=<p>   the pattern a FILLED value of that required
//                           parameter must match. A filled value that breaks it
//                           is counted apart from a blank one: wrong data is a
//                           different job from missing data
//   placeholder=<a>,<b>     values the project treats as not filled in - TBD,
//                           XXX, whatever the request names. Counted as BLANK.
//                           Nothing is assumed: with no such key, "TBD" is a
//                           filled value
//
// A NAME TWO PARAMETERS SHARE ON ONE ELEMENT IS REPORTED, NOT READ. Revit's
// LookupParameter returns one of them at random (FRAGMENT-ISSUES row 5b-203, D-54
// s3), so a value read that way is no evidence of anything. Such an element is
// counted as NOT READ, apart from blank, filled and absent.

var findings = new List<string>();
var failures = 0;
var sectionsChecked = 0;
var sectionsNotChecked = 0;
var offenders = new List<ElementId>();
var totalNotRead = 0;

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

// The table's keys are read case-insensitively and trimmed: "View" and "view "
// are the same rule to the person who typed them. Version 1 read them exactly,
// so a capital letter turned a rule into NOT CHECKED.
var rules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var valuePatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var placeholders = new List<string>();
var placeholdersGiven = false;
var unknownKeys = new List<string>();
var knownKeys = new[] { "view", "sheet", "level", "workset", "system", "type", "room" };

if (namePatterns != null)
{
    foreach (var pair in namePatterns)
    {
        var key = (pair.Key ?? "").Trim();
        var value = (pair.Value ?? "").Trim();
        if (key.Length == 0) continue;

        if (key.StartsWith("value:", StringComparison.OrdinalIgnoreCase))
        {
            var parameterName = key.Substring("value:".Length).Trim();
            if (parameterName.Length == 0 || value.Length == 0) { unknownKeys.Add(key); continue; }
            valuePatterns[parameterName] = value;
            continue;
        }

        if (string.Equals(key, "placeholder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "placeholders", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var piece in value.Split(','))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length > 0) placeholders.Add(trimmed);
            }
            placeholdersGiven = placeholders.Count > 0;
            continue;
        }

        if (Array.IndexOf(knownKeys, key.ToLowerInvariant()) >= 0) { rules[key] = value; continue; }
        unknownKeys.Add(key);
    }
}

Func<string, string> patternFor = key =>
{
    string pattern = null;
    if (!rules.TryGetValue(key, out pattern)) return "";
    return pattern ?? "";
};

// A value is a placeholder when it matches one the request named, by the same
// wildcard rule as everything else - "TBD" is exactly TBD, "TBC*" allows a tail.
Func<string, bool> isPlaceholder = value =>
{
    if (!placeholdersGiven) return false;
    var trimmed = (value ?? "").Trim();
    foreach (var placeholder in placeholders)
        if (matches(trimmed, placeholder)) return true;
    return false;
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

// A NEW SECTION RUNS ONLY WHEN ITS OWN KEY WAS GIVEN. Version 1's suspectNames
// apply to the five sections version 1 had; letting them reach type and room
// names would have grown a version 1 caller's failures with no key asking for
// it (review of PR #410). Absent key: NOT CHECKED, said in words.
Action<string, string> notAsked = (title, key) =>
{
    findings.Add("");
    findings.Add(title.ToUpper());
    sectionsNotChecked++;
    findings.Add("  NOT CHECKED - no '" + key + "=' pattern was given, so " + title.ToLower()
        + " were not looked at. THIS IS NOT A PASS");
};

// ---- type names, as "Family: Type" (version 2) ----
// Over the TYPES of the categories in `requiredOn` - the same categories the
// parameter rules cover, so one list says which part of the model is in scope.
// "Family: Type" with no space before the colon is the form Heron writes a type
// name in everywhere else, so a pattern written for one tool works in another.
var typeCategoryIds = new List<ElementId>();
if (requiredOn != null)
    foreach (var category in requiredOn)
        if (category != null) typeCategoryIds.Add(category.Id);

if (patternFor("type").Length == 0)
{
    notAsked("Type names", "type");
}
else if (typeCategoryIds.Count == 0)
{
    findings.Add("");
    findings.Add("TYPE NAMES");
    sectionsNotChecked++;
    findings.Add("  NOT CHECKED - type names are checked over the types of the categories in "
        + "requiredOn, and no category was given" + (patternFor("type").Length > 0
            ? ". A type pattern WAS given ('" + patternFor("type") + "') and could not be applied"
            : ""));
}
else
{
    var typeNames = new List<string>();
    var typeIds = new List<ElementId>();
    foreach (var element in new FilteredElementCollector(doc)
        .WherePasses(new ElementMulticategoryFilter(typeCategoryIds))
        .WhereElementIsElementType())
    {
        var type = element as ElementType;
        if (type == null) continue;
        var familyName = type.FamilyName ?? "";
        typeNames.Add(familyName.Length > 0 ? familyName + ": " + type.Name : type.Name);
        typeIds.Add(type.Id);
    }
    section("Type names", patternFor("type"), typeNames, typeIds);
}

// ---- room names (version 2) ----
// Read from the Name parameter, not Element.Name: a room's element name carries
// its number as well, so a pattern for the name would fail on every room.
var roomNames = new List<string>();
var roomIds = new List<ElementId>();
if (patternFor("room").Length > 0)
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Rooms)
    .WhereElementIsNotElementType())
{
    if (element == null) continue;
    var nameParameter = element.get_Parameter(BuiltInParameter.ROOM_NAME);
    roomNames.Add(nameParameter == null ? "" : (nameParameter.AsString() ?? ""));
    roomIds.Add(element.Id);
}
if (patternFor("room").Length == 0) notAsked("Room names", "room");
else section("Room names", patternFor("room"), roomNames, roomIds);

// ---- the parameters the project requires ----
findings.Add("");
findings.Add("REQUIRED PARAMETERS");

if (requiredOn == null || requiredOn.Count == 0
    || requiredParameters == null || requiredParameters.Count == 0)
{
    sectionsNotChecked++;
    findings.Add("  NOT CHECKED - no categories or no parameter names were given. Nothing here says "
        + "the data is complete"
        + (valuePatterns.Count > 0 ? ". " + valuePatterns.Count + " value pattern(s) were given and "
            + "could not be applied" : "")
        + (placeholdersGiven ? ". The placeholders named were not used" : ""));
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

            string valuePattern = null;
            valuePatterns.TryGetValue(parameterName, out valuePattern);
            var valuePatternGiven = !string.IsNullOrEmpty(valuePattern);

            var blank = 0;
            var asPlaceholder = 0;
            var absent = 0;
            var filled = 0;
            var wrongForm = 0;
            var notRead = 0;
            var shown = 0;

            foreach (var element in subjects)
            {
                if (element == null) continue;

                // THE SAME-NAME GUARD, row 5b-203. Two parameters with this name on
                // the element - or, when the element has none, on its type - and
                // LookupParameter would hand back one of them at random. Reported,
                // never read: a value picked that way is no evidence either way.
                var type = doc.GetElement(element.GetTypeId()) as ElementType;
                var onElement = element.GetParameters(parameterName).Count;
                var onType = type == null ? 0 : type.GetParameters(parameterName).Count;
                if (onElement > 1 || (onElement == 0 && onType > 1))
                {
                    notRead++;
                    totalNotRead++;
                    if (shown < maxRows)
                    {
                        shown++;
                        findings.Add("  '" + parameterName + "' is NOT READ on " + element.Name
                            + " (id " + element.Id + ") - " + (onElement > 1 ? onElement : onType)
                            + " parameters on the " + (onElement > 1 ? "element" : "type")
                            + " share that name, and reading one of them by name picks at random");
                    }
                    continue;
                }

                var parameter = element.LookupParameter(parameterName);
                if (parameter == null && type != null) parameter = type.LookupParameter(parameterName);

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
                var placeholder = parameter.HasValue && !string.IsNullOrEmpty(value)
                    && value.Trim().Length > 0 && isPlaceholder(value);
                if (!parameter.HasValue || string.IsNullOrEmpty(value) || value.Trim().Length == 0
                    || placeholder)
                {
                    blank++;
                    if (placeholder) asPlaceholder++;
                    failures++;
                    offenders.Add(element.Id);
                    if (shown < maxRows)
                    {
                        shown++;
                        findings.Add("  '" + parameterName + "' is BLANK on " + element.Name
                            + " (id " + element.Id + ")"
                            + (placeholder ? " - it holds '" + value.Trim() + "', a placeholder the "
                                + "request named" : "")
                            + " - that is data entry");
                    }
                    continue;
                }

                // FILLED, AND - WHEN A PATTERN WAS GIVEN FOR IT - IN THE WRONG FORM.
                // Kept apart from blank: somebody typed something, and the fix is
                // to correct it, not to fill it. The text compared is what Revit
                // shows, so a number carries its unit.
                if (valuePatternGiven && !matches(value.Trim(), valuePattern))
                {
                    wrongForm++;
                    failures++;
                    offenders.Add(element.Id);
                    if (shown < maxRows)
                    {
                        shown++;
                        findings.Add("  '" + parameterName + "' on " + element.Name + " (id "
                            + element.Id + ") is '" + value.Trim() + "', which does not match '"
                            + valuePattern + "'");
                    }
                    continue;
                }

                filled++;
            }

            if (blank + absent + wrongForm + notRead > shown)
                findings.Add("  ... " + (blank + absent + wrongForm + notRead - shown)
                    + " more for '" + parameterName + "', not listed. The counts are complete");

            findings.Add("  " + parameterName + ": " + filled + " filled"
                + (valuePatternGiven ? " and matching '" + valuePattern + "', " + wrongForm
                    + " filled but NOT matching it" : " (value form NOT CHECKED - no value:"
                    + parameterName + " pattern was given)")
                + ", " + blank + " BLANK"
                + (placeholdersGiven ? " (" + asPlaceholder + " of them holding a placeholder)"
                    : " (placeholders NOT CHECKED - none were named, so a 'TBD' counts as filled)")
                + ", " + absent + " NOT PRESENT on the element at all"
                + (notRead > 0 ? ", " + notRead + " NOT READ because two parameters share the name"
                    : "")
                + ". Blank and not present go to different people and are counted apart on purpose");
        }

        foreach (var pattern in valuePatterns)
        {
            var required = false;
            foreach (var parameterName in requiredParameters)
                if (string.Equals(parameterName, pattern.Key, StringComparison.OrdinalIgnoreCase))
                    required = true;
            if (!required)
                findings.Add("  value:" + pattern.Key + " NOT CHECKED - '" + pattern.Key + "' is not "
                    + "in requiredParameters, and a value pattern is only applied to a parameter "
                    + "the run was asked to require");
        }

        findings.Add("  categories covered: " + string.Join(", ", categoryNames.ToArray())
            + " (" + subjects.Count + " element(s))");
    }
}

// A KEY THIS FRAGMENT DOES NOT KNOW IS SAID, NOT DROPPED. "views=" for "view="
// would otherwise leave the views NOT CHECKED with nothing to say why.
if (unknownKeys.Count > 0)
{
    findings.Add("");
    findings.Add("RULES NOT UNDERSTOOD");
    findings.Add("  NOT USED: " + string.Join(", ", unknownKeys.ToArray()) + ". The keys this reads "
        + "are view, sheet, level, workset, system, type, room, value:<parameter> and placeholder");
}

if (totalNotRead > 0)
    findings.Insert(0, totalNotRead + " parameter read(s) were NOT MADE because two parameters share "
        + "the name on one element - those elements are neither passed nor failed");

findings.Insert(0, string.Format("{0} failure(s) across {1} section(s) checked, {2} section(s) NOT "
    + "CHECKED. Model: {3}. A SECTION NOBODY GAVE A RULE FOR IS NOT A SECTION THAT PASSED - read the "
    + "NOT CHECKED lines before the counts. This checks CONVENTIONS, not correctness: a view named "
    + "perfectly can still show the wrong thing",
    failures, sectionsChecked, sectionsNotChecked,
    string.IsNullOrEmpty(doc.Title) ? "(unsaved)" : doc.Title));
