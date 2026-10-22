// NOT STANDALONE. Assumes `doc` and `settings` are in scope; leaves
// `settingReport`, `changed`, `alreadySet`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE FAMILY'S OWN SETTINGS - the switches in Family Category and Parameters:
// Work Plane-Based (placed on any plane or face, and moving with it - the way a
// family nested in another is best made), Shared (a nested copy that shows in a
// project's schedules and tags as an element of its own), Always Vertical (a
// Work Plane-Based family that stays upright on a sloped plane), Room
// Calculation Point (the point that says which room a placed unit is in), Cut
// with Voids When Loaded (its unattached voids cut walls, floors and the rest
// in a project), Maintain Annotation Orientation (a nested symbol stays
// readable in plan), Rotate with Component and Keep Text Readable (an
// annotation's), and Enable Cutting in Views.
//
// AND TWO THAT ARE NOT YES OR NO: PART TYPE - Elbow, Tee, Transition, Breaks
// Into, Damper... - which decides how an MEP fitting or accessory behaves and
// which routing preference group offers it; and PROFILE USAGE - Wall Sweep,
// Mullion, Railing... - which decides where a profile family is offered. Each
// is matched to Revit's own list, read from the running Revit, and the words
// Revit shows are accepted with their spaces and dashes.
//
// THEY BELONG TO THE FAMILY, NOT TO A TYPE: one value for the whole family, read
// from the family document's own Family element. A switch this family's
// category does not have, or that Revit holds read-only here, is refused by
// name - never quietly skipped.
//
// THE ORDER IS WORK PLANE-BASED FIRST. Always Vertical belongs to a Work
// Plane-Based family, so the plane switch is written before it, and a switch
// Revit still holds read-only after that is refused in Revit's own state.
//
// READ BACK, ALL OR NOTHING. Every switch is read again after the last write;
// one that does not read as asked fails the call, and the host rolls the whole
// call back.
//
// VERSION 3 (2026-10-21): LIGHT SOURCE - the tick a Lighting Fixtures family
// needs before it has a light source to define, done by hand until then. Revit's
// switch for it is the built-in LIGHTING_FIXTURE_LIGHT_EMITTER ("Light Emitter"
// in the reference, the only Family-level light switch there, 2020 to 2027),
// and whether it is the box Family Category and Parameters calls Light Source is
// not in the reference - so the tick is PROVED, not trusted: after the write,
// Revit's lighting API must find a light source in the family, or the call
// fails and nothing is kept. Only YES is taken. Unticking removes the light
// source and every photometric value with it - by hand, never by Heron.
//
// VERSION 2 (2026-10-11): ONE SETTING THAT TAKES TEXT - THE OMNICLASS NUMBER
// ("Classification Number" from Revit 2026), the code a schedule, an export or
// a library sorts the family by. Only a code the classification file files
// under THIS family's category is taken; anything else is refused with nothing
// changed and the reason by name, and every refusal lists the codes the file
// does give this category, so they can be put to the modeller. Nothing near a
// code is ever picked, and free text is never taken (D-33, D-34). Blank,
// "none" or "clear" clears it, and clearing needs no file.

var findings = new List<string>();
var settingReport = "";
var changed = 0;
var alreadySet = 0;
var notAFamily = false;
string refused = null;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// THE SWITCHES: the key they are named by, the words Family Category and
// Parameters shows, and Revit's id for each. In the order they are written.
var switches = new List<Tuple<string, string, BuiltInParameter>>
{
    Tuple.Create("workplanebased", "Work Plane-Based", BuiltInParameter.FAMILY_WORK_PLANE_BASED),
    Tuple.Create("alwaysvertical", "Always Vertical", BuiltInParameter.FAMILY_ALWAYS_VERTICAL),
    Tuple.Create("shared", "Shared", BuiltInParameter.FAMILY_SHARED),
    Tuple.Create("roomcalculationpoint", "Room Calculation Point", BuiltInParameter.ROOM_CALCULATION_POINT),
    Tuple.Create("cutwithvoidswhenloaded", "Cut with Voids When Loaded", BuiltInParameter.FAMILY_ALLOW_CUT_WITH_VOIDS),
    Tuple.Create("maintainannotationorientation", "Maintain Annotation Orientation",
        BuiltInParameter.FAMILY_ELECTRICAL_MAINTAIN_ANNOTATION_ORIENTATION),
    Tuple.Create("rotatewithcomponent", "Rotate with Component", BuiltInParameter.FAMILY_ROTATE_WITH_COMPONENT),
    Tuple.Create("keeptextreadable", "Keep Text Readable", BuiltInParameter.FAMILY_KEEP_TEXT_READABLE),
    Tuple.Create("enablecuttinginviews", "Enable Cutting in Views", BuiltInParameter.FAMILY_ENABLE_CUTTING_IN_VIEWS),
    // VERSION 3: the Light Source tick of a Lighting Fixtures family - proved
    // after the write by Revit's lighting API finding a light source.
    Tuple.Create("lightsource", "Light Source", BuiltInParameter.LIGHTING_FIXTURE_LIGHT_EMITTER),
    Tuple.Create("parttype", "Part Type", BuiltInParameter.FAMILY_CONTENT_PART_TYPE),
    Tuple.Create("profileusage", "Profile Usage", BuiltInParameter.FAM_PROFILE_USAGE),
    Tuple.Create("materialformodelbehavior", "Material for Model Behavior", BuiltInParameter.FAMILY_STRUCT_MATERIAL_TYPE),
    Tuple.Create("sectionshape", "Section Shape", BuiltInParameter.STRUCTURAL_SECTION_SHAPE),
};

// THE SETTINGS THAT TAKE A NAME, NOT YES OR NO: each value Revit's own list
// holds, read from the enum on the running Revit, with the words Revit shows
// that the enum spells differently. A structural family's Section Shape enum
// lives in a namespace the executor does not import, so it is read from the
// Family property that carries it.
var namedTypes = new Dictionary<string, Type>
{
    { "parttype", typeof(PartType) },
    { "profileusage", typeof(ProfileFamilyUsage) },
    { "materialformodelbehavior", typeof(StructuralMaterialType) },
};
var sectionShapeProperty = typeof(Family).GetProperty("StructuralSectionShape");
if (sectionShapeProperty != null && sectionShapeProperty.PropertyType.IsEnum)
    namedTypes["sectionshape"] = sectionShapeProperty.PropertyType;
var named = new Dictionary<string, Dictionary<string, int>>();
Func<Type, Dictionary<string, string>, Dictionary<string, int>> listOf = (enumType, aliases) =>
{
    var values = new Dictionary<string, int>();
    foreach (var name in Enum.GetNames(enumType))
        values[squash(name)] = Convert.ToInt32(Enum.Parse(enumType, name));
    foreach (var alias in aliases)
        if (values.ContainsKey(alias.Value)) values[alias.Key] = values[alias.Value];
    return values;
};
named["parttype"] = listOf(typeof(PartType), new Dictionary<string, string>
{
    { "flange", "pipeflange" }, { "mechanicalcoupling", "pipemechanicalcoupling" },
});
named["profileusage"] = listOf(typeof(ProfileFamilyUsage), new Dictionary<string, string>
{
    { "generic", "any" },
});
named["materialformodelbehavior"] = listOf(typeof(StructuralMaterialType), new Dictionary<string, string>
{
    { "precast", "precastconcrete" },
});
if (namedTypes.ContainsKey("sectionshape"))
    named["sectionshape"] = listOf(namedTypes["sectionshape"], new Dictionary<string, string>());
// A stored value back in words: the enum's own name for a named setting.
Func<string, int?, string> wordsFor = (key, value) =>
{
    if (!value.HasValue) return "unread";
    if (namedTypes.ContainsKey(key) && Enum.IsDefined(namedTypes[key], value.Value))
        return Enum.GetName(namedTypes[key], value.Value);
    if (named.ContainsKey(key)) return value.Value.ToString();
    return value == 1 ? "Yes" : value == 0 ? "No" : "unread";
};

Func<Parameter, int?> stored = p =>
{
    try { return p == null || p.StorageType != StorageType.Integer ? (int?)null : p.AsInteger(); }
    catch (Exception) { return null; }
};
Func<int?, string> yesNo = v => v == 1 ? "Yes" : v == 0 ? "No" : "unread";

// VERSION 3: does Revit's lighting API find a light source in this family? Its
// classes live in a namespace the executor does not import, so they are found
// by name; null when it does, Revit's reason when it does not.
Func<string> noLightSource = () =>
{
    var lightFamilyType = typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".Lighting.LightFamily");
    if (lightFamilyType == null) return "Revit's lighting classes could not be found in this release";
    try
    {
        lightFamilyType.GetMethod("GetLightFamily", new[] { typeof(Document) }).Invoke(null, new object[] { doc });
        return null;
    }
    catch (System.Reflection.TargetInvocationException ex)
    {
        return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
    }
    catch (Exception ex) { return ex.Message; }
};

// ---------------------------------------------------------------------------
// VERSION 2: THE OMNICLASS NUMBER
// ---------------------------------------------------------------------------
//
// ITS BUILT-IN PARAMETER WAS RENAMED AT REVIT 2026 AND KEPT ITS NUMBER:
// OMNICLASS_CODE and OMNICLASS_DESCRIPTION up to 2025, CLASSIFICATION_CODE and
// CLASSIFICATION_DESCRIPTION from 2026, and neither pair exists on the other
// side. Naming either in code breaks the build on the other releases, so the
// one this Revit has is found by NAME at run time, and shown in Revit's own
// words - "OmniClass Number" on 2024, "Classification Number" on 2026.
var clsNewNames = Enum.IsDefined(typeof(BuiltInParameter), "CLASSIFICATION_CODE");
var clsCode = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter), clsNewNames ? "CLASSIFICATION_CODE" : "OMNICLASS_CODE");
var clsTitle = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter),
    Enum.IsDefined(typeof(BuiltInParameter), "CLASSIFICATION_DESCRIPTION") ? "CLASSIFICATION_DESCRIPTION" : "OMNICLASS_DESCRIPTION");
Func<BuiltInParameter, string, string> clsLabelFor = (clsBip, clsOtherwise) =>
{
    try
    {
        var clsSaid = LabelUtils.GetLabelFor(clsBip);
        return string.IsNullOrEmpty(clsSaid) ? clsOtherwise : clsSaid;
    }
    catch (Exception) { return clsOtherwise; }
};
var clsCodeLabel = clsLabelFor(clsCode, clsNewNames ? "Classification Number" : "OmniClass Number");
var clsTitleLabel = clsLabelFor(clsTitle, clsNewNames ? "Classification Title" : "OmniClass Title");
// The words a modeller names it by, squashed: either release's.
var clsKeys = new[] { "omniclassnumber", "omniclass", "classificationnumber", "classificationcode", "classification" };

// Trimmed of every character char.IsWhiteSpace calls space - the 2020 to 2025
// file ends some titles in a non-breaking space.
Func<string, string> clsTidy = clsText =>
{
    var clsS = clsText ?? "";
    int clsFrom = 0, clsTo = clsS.Length;
    while (clsFrom < clsTo && char.IsWhiteSpace(clsS[clsFrom])) clsFrom++;
    while (clsTo > clsFrom && char.IsWhiteSpace(clsS[clsTo - 1])) clsTo--;
    return clsS.Substring(clsFrom, clsTo - clsFrom);
};
Func<string, string> clsShown = clsValue => string.IsNullOrEmpty(clsValue) ? "(blank)" : clsValue;

// THE CLASSIFICATION FILE - the list Revit's own OmniClass Number dialog
// offers. No API reads it; it is a plain file, one row per line, TAB between
// code, title, level and the BuiltInCategory it is filed under (that last one
// may be empty), and '#' starts a comment: "OmniClassTaxonomy.txt" up to 2025,
// "Classification_Taxonomy.txt" from 2026, the name that matches the parameter
// found above tried first, and the path actually read is said. ONLY THE
// PER-USER COPY IS READ - the one Revit itself reads, in
// Application.CurrentUsersDataFolderPath, which Revit fills from its own cache
// at start-up. That cache sits under a Windows special folder, and only
// HeronPaths may resolve one (tools/check-structure.py, docs/06 section 2).
var clsTried = new List<string>();
string clsPath = null;
string clsProblem = null;
var clsSample = false;
// code, title, the category column as written, the BuiltInCategory (boxed) or null
var clsRows = new List<Tuple<string, string, string, object>>();
Action clsLoad = () =>
{
    var clsFolders = new List<string>();
    try
    {
        var clsUser = doc.Application.CurrentUsersDataFolderPath;
        if (!string.IsNullOrEmpty(clsUser)) clsFolders.Add(clsUser);
    }
    catch (Exception) { }
    var clsNames = clsNewNames
        ? new[] { "Classification_Taxonomy.txt", "OmniClassTaxonomy.txt" }
        : new[] { "OmniClassTaxonomy.txt", "Classification_Taxonomy.txt" };
    foreach (var clsName in clsNames)
        foreach (var clsFolder in clsFolders)
        {
            if (clsPath != null) break;
            var clsCandidate = System.IO.Path.Combine(clsFolder, clsName);
            clsTried.Add(clsCandidate);
            if (System.IO.File.Exists(clsCandidate)) clsPath = clsCandidate;
        }
    if (clsPath == null) return;
    string clsAll;
    try
    {
        // THE 2020 TO 2025 FILE IS NOT UTF-8: it holds Windows-1252 bytes (an
        // en dash, an e-cedilla). Strict UTF-8 first; then code page 1252,
        // which .NET 8 and 10 refuse unless a code-page provider is
        // registered; then Latin-1, which every runtime has.
        var clsBytes = System.IO.File.ReadAllBytes(clsPath);
        try { clsAll = new System.Text.UTF8Encoding(false, true).GetString(clsBytes); }
        catch (Exception)
        {
            try { clsAll = System.Text.Encoding.GetEncoding(1252).GetString(clsBytes); }
            catch (Exception) { clsAll = System.Text.Encoding.GetEncoding(28591).GetString(clsBytes); }
        }
    }
    catch (Exception ex)
    {
        clsProblem = "the classification file " + clsPath + " could not be read: " + ex.Message;
        return;
    }
    clsSample = clsAll.IndexOf("sample Classification File", StringComparison.OrdinalIgnoreCase) >= 0;
    foreach (var clsLine in clsAll.Replace(((char)0xFEFF).ToString(), "").Split((char)10))
    {
        var clsTrimmed = clsTidy(clsLine);
        if (clsTrimmed.Length == 0 || clsTrimmed.StartsWith("#")) continue;
        var clsColumns = clsLine.Split((char)9);
        if (clsColumns.Length < 2) continue;
        var clsRowCode = clsTidy(clsColumns[0]);
        if (clsRowCode.Length == 0) continue;
        var clsRaw = clsColumns.Length > 3 ? clsTidy(clsColumns[3]) : "";
        object clsFiled = null;
        long clsNumber;
        if (clsRaw.Length > 0 && long.TryParse(clsRaw, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out clsNumber))
        {
            var clsEnum = Enum.ToObject(typeof(BuiltInCategory), clsNumber);
            if (Enum.IsDefined(typeof(BuiltInCategory), clsEnum)) clsFiled = clsEnum;
        }
        clsRows.Add(Tuple.Create(clsRowCode, clsTidy(clsColumns[1]), clsRaw, clsFiled));
    }
};
// Filed under this category: compared as ElementIds, never as numbers.
Func<Tuple<string, string, string, object>, ElementId, bool> clsFiledUnder = (clsRow, clsCategoryId) =>
    clsRow.Item4 != null && clsCategoryId != null && new ElementId((BuiltInCategory)clsRow.Item4).Equals(clsCategoryId);
Func<Tuple<string, string, string, object>, string> clsFiledWords = clsRow =>
{
    if (clsRow.Item3.Length == 0) return "no category";
    if (clsRow.Item4 == null) return "a category this Revit does not have (" + clsRow.Item3 + ")";
    var clsBic = (BuiltInCategory)clsRow.Item4;
    try
    {
        var clsCategory = Category.GetCategory(doc, clsBic);
        if (clsCategory != null) return clsCategory.Name;
    }
    catch (Exception) { }
    try { return LabelUtils.GetLabelFor(clsBic); }
    catch (Exception) { return clsBic.ToString(); }
};
Func<List<Tuple<string, string, string, object>>, string> clsList = clsSome =>
    string.Join(" || ", clsSome.Select(r => r.Item1 + " " + r.Item2));

var clsAsked = false;
var clsClear = false;
var clsSaidCode = "";
Tuple<string, string, string, object> clsChosen = null;
Parameter clsCodeRow = null;

// Each switch asked for: the switch, and Yes (1) or No (0).
var plans = new List<Tuple<Tuple<string, string, BuiltInParameter>, int>>();
var problems = new List<string>();
Family family = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    var namesClassification = settings != null && settings.Keys.Any(k => clsKeys.Contains(squash(k)));
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A family's own settings are set inside it - open it for editing first (OPEN_FAMILY_FOR_EDITING). "
        + (namesClassification
            ? "The " + clsCodeLabel + " belongs to the family too: set it there, then load the family back into this "
              + "project with LOAD_FAMILY - a family reload, which needs the modeller's word for that operation "
              + "(HERON_CONSTITUTION Article 7). "
            : "")
        + "Nothing was changed.";
}
else
{
    family = doc.OwnerFamily;
    if (family == null) problems.Add("This family document has no Family element to hold its settings.");

    var offered = string.Join(", ", switches.Select(s => s.Item2)) + ", " + clsCodeLabel;
    if (settings == null || settings.Count == 0)
        problems.Add("No settings were given - \"Work Plane-Based=yes; Shared=yes\", semicolons between, from "
            + offered + ".");
    else if (family != null)
        foreach (var pair in settings)
        {
            var key = squash(pair.Key);
            // THE ONE TEXT SETTING, taken aside before the yes-or-no switches -
            // none of whose rules (an Integer, yes or no, a name from an enum)
            // applies to it.
            if (clsKeys.Contains(key))
            {
                if (clsAsked)
                {
                    problems.Add("\"" + clsCodeLabel + "\" is named twice - say it once.");
                    continue;
                }
                clsAsked = true;
                clsSaidCode = clsTidy(pair.Value);
                continue;
            }
            var match = switches.FirstOrDefault(s => s.Item1 == key);
            if (match == null)
            {
                problems.Add("\"" + pair.Key + "\" is not one of the settings this sets - " + offered + ".");
                continue;
            }
            if (plans.Any(p => p.Item1.Item1 == match.Item1))
            {
                problems.Add("\"" + match.Item2 + "\" is named twice - say it once.");
                continue;
            }
            var said = squash(pair.Value);
            int value;
            if (named.ContainsKey(match.Item1))
            {
                if (!named[match.Item1].TryGetValue(said, out value))
                {
                    problems.Add("\"" + (pair.Value ?? "") + "\" is not a " + match.Item2 + " this Revit has - it has "
                        + string.Join(", ", Enum.GetNames(namedTypes[match.Item1])) + ".");
                    continue;
                }
            }
            else if (said == "yes" || said == "true" || said == "on" || said == "1") value = 1;
            else if (said == "no" || said == "false" || said == "off" || said == "0") value = 0;
            else
            {
                problems.Add("\"" + (pair.Value ?? "") + "\" for " + match.Item2 + " is neither yes nor no.");
                continue;
            }
            if (match.Item1 == "lightsource" && value == 0)
            {
                problems.Add("Light Source is not unticked by Heron: unticking removes the light source and every "
                    + "photometric value of every type with it. Untick it by hand in Family Category and Parameters if "
                    + "that is meant.");
                continue;
            }
            var row = family.get_Parameter(match.Item3);
            if (row == null || row.StorageType != StorageType.Integer)
            {
                problems.Add("This family's category, " + (family.FamilyCategory == null ? "unknown"
                    : family.FamilyCategory.Name) + ", has no " + match.Item2 + " setting"
                    + (match.Item1 == "lightsource"
                        ? " that Revit hands the API (LIGHTING_FIXTURE_LIGHT_EMITTER) - a light source belongs to a "
                          + "Lighting Fixtures family (SET_FAMILY_CATEGORY first); if it already is one, tick Light Source "
                          + "by hand in Family Category and Parameters"
                        : "") + ".");
                continue;
            }
            plans.Add(Tuple.Create(match, value));
        }

    // THE OMNICLASS NUMBER, checked against the classification file before
    // anything is written - so a refusal changes nothing.
    if (clsAsked && family != null)
    {
        var clsSquashed = squash(clsSaidCode);
        clsClear = clsSaidCode.Length == 0 || clsSquashed == "none" || clsSquashed == "clear";
        var clsCategoryName = family.FamilyCategory == null ? "no category" : family.FamilyCategory.Name;
        clsCodeRow = family.get_Parameter(clsCode);
        if (clsCodeRow == null || clsCodeRow.StorageType != StorageType.String)
            problems.Add("This family's category, " + clsCategoryName + ", has no " + clsCodeLabel + " for Revit to hold.");
        else if (!clsClear)
        {
            clsLoad();
            if (clsPath == null)
                problems.Add("No classification file was found to check \"" + clsSaidCode + "\" against - looked for "
                    + string.Join(", ", clsTried) + ". Revit's own Family Category and Parameters dialog sets the "
                    + clsCodeLabel + " by hand; or put the office's classification file at the first of those paths "
                    + "and ask again.");
            else if (clsProblem != null)
                problems.Add("The " + clsCodeLabel + " was not checked: " + clsProblem + ".");
            else
            {
                var clsMine = family.FamilyCategory == null ? new List<Tuple<string, string, string, object>>()
                    : clsRows.Where(r => clsFiledUnder(r, family.FamilyCategory.Id)).ToList();
                var clsOffer = clsMine.Count > 0
                    ? "The file gives " + clsCategoryName + " " + clsMine.Count + " code(s): " + clsList(clsMine) + "."
                    : clsSample
                        ? "And the file is the SAMPLE Revit 2026 and later ship - a few example codes filed under no "
                          + "category - so no code is valid for " + clsCategoryName + " or any other category until "
                          + "the office's own classification file is put at " + clsPath + "."
                        : "The file gives " + clsCategoryName + " no code at all.";
                var clsSame = clsRows.Where(r => string.Equals(r.Item1, clsSaidCode, StringComparison.OrdinalIgnoreCase)).ToList();
                clsChosen = family.FamilyCategory == null ? null
                    : clsSame.FirstOrDefault(r => clsFiledUnder(r, family.FamilyCategory.Id));
                if (clsSame.Count == 0)
                    problems.Add("\"" + clsSaidCode + "\" is not a code in the classification file " + clsPath
                        + ", and no nearby code is picked in its place. " + clsOffer);
                else if (clsChosen == null)
                    problems.Add(clsSame[0].Item1 + " " + clsSame[0].Item2 + " is filed under "
                        + clsFiledWords(clsSame[0]) + " in " + clsPath + ", not " + clsCategoryName + ". " + clsOffer);
            }
        }
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// WRITE IN ORDER, THEN READ EVERY SWITCH BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var before = switches.Select(s => Tuple.Create(s, stored(family.get_Parameter(s.Item3)))).ToList();
    var rows = new List<string>();

    foreach (var plan in plans.OrderBy(p => switches.IndexOf(p.Item1)))
    {
        var row = family.get_Parameter(plan.Item1.Item3);
        if (stored(row) == plan.Item2) { alreadySet++; continue; }
        if (row.IsReadOnly)
            throw new InvalidOperationException("Revit holds " + plan.Item1.Item2 + " read-only in this family"
                + (plan.Item1.Item1 == "alwaysvertical"
                    ? " - it belongs to a Work Plane-Based family, and Work Plane-Based reads "
                      + yesNo(stored(family.get_Parameter(BuiltInParameter.FAMILY_WORK_PLANE_BASED)))
                    : "")
                + ". The call failed, and Heron rolls the whole call back.");
        try { row.Set(plan.Item2); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not set " + plan.Item1.Item2 + " to "
                + wordsFor(plan.Item1.Item1, plan.Item2) + ": " + ex.Message + " The call failed, and Heron rolls the "
                + "whole call back.");
        }
        // A plane switch decides what the next switch may be; Revit's view of
        // it is brought up to date before the next one is written.
        doc.Regenerate();
    }

    foreach (var plan in plans)
    {
        var now = stored(family.get_Parameter(plan.Item1.Item3));
        if (now != plan.Item2)
            throw new InvalidOperationException(plan.Item1.Item2 + " reads " + wordsFor(plan.Item1.Item1, now)
                + " after the call, not " + wordsFor(plan.Item1.Item1, plan.Item2) + ". The call failed, and Heron "
                + "rolls the whole call back.");
    }

    // VERSION 3: LIGHT SOURCE READING YES IS NOT ENOUGH - Revit's lighting API
    // must now find a light source, or the switch written was not the tick.
    if (plans.Any(p => p.Item1.Item1 == "lightsource"))
    {
        var why = noLightSource();
        var wasOn = before.Any(b => b.Item1.Item1 == "lightsource" && b.Item2 == 1);
        if (why != null && !wasOn)
            throw new InvalidOperationException("Light Source reads Yes after the call, but Revit's lighting API still "
                + "finds no light source in this family (" + why + ") - the switch Heron wrote is not the Light Source "
                + "tick here. The call failed, and Heron rolls the whole call back; tick Light Source by hand in Family "
                + "Category and Parameters.");
        findings.Add(why == null
            ? "Light Source is ticked and Revit's lighting API finds the light source - SET_FAMILY_LIGHT_SOURCE sets its "
              + "shape, distribution, position and aim, then each type's intensity and colour."
            : "Light Source already read Yes, yet Revit's lighting API finds no light source in this family (" + why
              + ") - nothing was written. Look at Family Category and Parameters by hand.");
    }

    foreach (var was in before)
    {
        var now = stored(family.get_Parameter(was.Item1.Item3));
        if (now == was.Item2) continue;
        var asked = plans.Any(p => p.Item1.Item1 == was.Item1.Item1);
        rows.Add(was.Item1.Item2 + " " + wordsFor(was.Item1.Item1, was.Item2) + " -> " + wordsFor(was.Item1.Item1, now)
            + (asked ? "" : " (Revit's own change)"));
        if (asked) changed++;
    }

    // -----------------------------------------------------------------------
    // THE OMNICLASS NUMBER - written as the file spells it, then read back.
    //
    // WHETHER AN API WRITE FILLS THE TITLE IS NOT KNOWN (NEEDS-CHECKING), so
    // the title is READ after the write: Revit's own fill is reported as
    // Revit's; otherwise, where Revit lets it be written, the file's title is
    // written; where it holds it read-only, what it reads is said. The CODE
    // must read as asked or the call fails, all or nothing; the title is
    // reported, never a failure.
    // -----------------------------------------------------------------------
    if (clsAsked)
    {
        var clsWanted = clsClear ? "" : clsChosen.Item1;
        var clsWantedTitle = clsClear ? "" : clsChosen.Item2;
        var clsBefore = clsTidy(clsCodeRow.AsString());
        var clsTitleRow = family.get_Parameter(clsTitle);
        var clsTitleBefore = clsTitleRow == null ? "" : clsTidy(clsTitleRow.AsString());
        if (string.Equals(clsBefore, clsWanted, StringComparison.Ordinal))
        {
            alreadySet++;
            findings.Add(clsCodeLabel + " already read " + clsShown(clsWanted) + "; " + clsTitleLabel + " reads "
                + clsShown(clsTitleBefore) + ". Nothing was written to either.");
        }
        else
        {
            if (clsCodeRow.IsReadOnly)
                throw new InvalidOperationException("Revit holds the " + clsCodeLabel + " read-only in this family. The "
                    + "call failed, and Heron rolls the whole call back.");
            var clsTook = false;
            try { clsTook = clsCodeRow.Set(clsWanted); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not set the " + clsCodeLabel + " to "
                    + clsShown(clsWanted) + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
            }
            doc.Regenerate();
            var clsNow = clsTidy(family.get_Parameter(clsCode).AsString());
            if (!clsTook || !string.Equals(clsNow, clsWanted, StringComparison.Ordinal))
                throw new InvalidOperationException(clsCodeLabel + " reads " + clsShown(clsNow) + " after the call, not "
                    + clsShown(clsWanted) + ". The call failed, and Heron rolls the whole call back.");
            changed++;

            string clsTitleSaid;
            clsTitleRow = family.get_Parameter(clsTitle);
            var clsTitleNow = clsTitleRow == null ? "" : clsTidy(clsTitleRow.AsString());
            if (clsTitleRow == null)
                clsTitleSaid = "this family has no " + clsTitleLabel;
            else if (string.Equals(clsTitleNow, clsWantedTitle, StringComparison.Ordinal))
                clsTitleSaid = string.Equals(clsTitleBefore, clsWantedTitle, StringComparison.Ordinal)
                    ? clsTitleLabel + " already read " + clsShown(clsWantedTitle)
                    : (clsClear ? "Revit cleared " : "Revit filled ") + clsTitleLabel;
            else if (!clsTitleRow.IsReadOnly)
            {
                var clsTitleTook = false;
                try { clsTitleTook = clsTitleRow.Set(clsWantedTitle); }
                catch (Exception) { clsTitleTook = false; }
                var clsTitleAfter = clsTidy(family.get_Parameter(clsTitle).AsString());
                clsTitleSaid = clsTitleTook && string.Equals(clsTitleAfter, clsWantedTitle, StringComparison.Ordinal)
                    ? (clsClear ? clsTitleLabel + " was cleared" : clsTitleLabel + " was written from the file")
                    : "Revit would not take " + clsTitleLabel + " - it reads " + clsShown(clsTitleAfter);
            }
            else
                clsTitleSaid = "Revit holds " + clsTitleLabel + " read-only and it reads " + clsShown(clsTitleNow);

            rows.Add(clsCodeLabel + " " + clsShown(clsBefore) + " -> "
                + (clsClear ? "(blank)" : clsWanted + " " + clsWantedTitle) + " - " + clsTitleSaid);
        }
        if (clsPath != null)
            findings.Add("The " + clsCodeLabel + " was checked against the classification file " + clsPath + ". A copy "
                + "of this family already in a project keeps its old code until the family is loaded there again "
                + "(LOAD_FAMILY).");
    }

    settingReport = rows.Count == 0 ? "Every setting named already read as asked." : string.Join("; ", rows);
    findings.Add(changed + " setting(s) changed and " + alreadySet + " already as asked, on the family \""
        + family.Name + "\" - read back: " + settingReport + ".");
    if (plans.Any(p => p.Item1.Item1 == "workplanebased" && p.Item2 == 1) && changed > 0)
        findings.Add("A Work Plane-Based family is placed on a plane or face and moves with it. Copies already "
            + "placed in a project or a family take it when this family is loaded there again.");
    if (plans.Any(p => p.Item1.Item1 == "shared") && changed > 0)
        findings.Add("Shared decides whether a copy nested in another family shows in a project as an element of "
            + "its own. It takes effect where this family is nested, when it is loaded there again.");
    if (plans.Any(p => p.Item1.Item1 == "cutwithvoidswhenloaded" && p.Item2 == 1) && changed > 0)
        findings.Add("Cut with Voids When Loaded lets the family's UNATTACHED voids cut walls, floors, roofs, "
            + "ceilings, generic models and structural elements in a project - with Cut Geometry there, once the "
            + "family is loaded again. A void already cut into the family's own solid cuts nothing outside it.");
    if (plans.Any(p => p.Item1.Item1 == "parttype") && changed > 0)
        findings.Add("Part Type decides how an MEP fitting or accessory behaves and which routing preference "
            + "group offers it; a fitting's connectors must match it - an elbow two, a tee three.");
}

if (refused != null) findings.Add(refused);
