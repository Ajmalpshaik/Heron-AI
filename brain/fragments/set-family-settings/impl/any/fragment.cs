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
    Tuple.Create("parttype", "Part Type", BuiltInParameter.FAMILY_CONTENT_PART_TYPE),
    Tuple.Create("profileusage", "Profile Usage", BuiltInParameter.FAM_PROFILE_USAGE),
};

// THE TWO SETTINGS THAT TAKE A NAME, NOT YES OR NO: each value Revit's own list
// holds, read from the enum on the running Revit, with the words Revit shows
// that the enum spells differently.
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
// A stored value back in words: the enum's own name for a named setting.
Func<string, int?, string> wordsFor = (key, value) =>
{
    if (!value.HasValue) return "unread";
    if (key == "parttype" && Enum.IsDefined(typeof(PartType), value.Value)) return ((PartType)value.Value).ToString();
    if (key == "profileusage" && Enum.IsDefined(typeof(ProfileFamilyUsage), value.Value))
        return ((ProfileFamilyUsage)value.Value).ToString();
    if (named.ContainsKey(key)) return value.Value.ToString();
    return value == 1 ? "Yes" : value == 0 ? "No" : "unread";
};

Func<Parameter, int?> stored = p =>
{
    try { return p == null || p.StorageType != StorageType.Integer ? (int?)null : p.AsInteger(); }
    catch (Exception) { return null; }
};
Func<int?, string> yesNo = v => v == 1 ? "Yes" : v == 0 ? "No" : "unread";

// Each switch asked for: the switch, and Yes (1) or No (0).
var plans = new List<Tuple<Tuple<string, string, BuiltInParameter>, int>>();
var problems = new List<string>();
Family family = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A family's own settings are set inside it - open it for editing first (OPEN_FAMILY_FOR_EDITING). Nothing "
        + "was changed.";
}
else
{
    family = doc.OwnerFamily;
    if (family == null) problems.Add("This family document has no Family element to hold its settings.");

    var offered = string.Join(", ", switches.Select(s => s.Item2));
    if (settings == null || settings.Count == 0)
        problems.Add("No settings were given - \"Work Plane-Based=yes; Shared=yes\", semicolons between, from "
            + offered + ".");
    else if (family != null)
        foreach (var pair in settings)
        {
            var key = squash(pair.Key);
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
                        + string.Join(", ", Enum.GetNames(match.Item1 == "parttype" ? typeof(PartType)
                            : typeof(ProfileFamilyUsage))) + ".");
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
            var row = family.get_Parameter(match.Item3);
            if (row == null || row.StorageType != StorageType.Integer)
            {
                problems.Add("This family's category, " + (family.FamilyCategory == null ? "unknown"
                    : family.FamilyCategory.Name) + ", has no " + match.Item2 + " setting.");
                continue;
            }
            plans.Add(Tuple.Create(match, value));
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

    foreach (var was in before)
    {
        var now = stored(family.get_Parameter(was.Item1.Item3));
        if (now == was.Item2) continue;
        var asked = plans.Any(p => p.Item1.Item1 == was.Item1.Item1);
        rows.Add(was.Item1.Item2 + " " + wordsFor(was.Item1.Item1, was.Item2) + " -> " + wordsFor(was.Item1.Item1, now)
            + (asked ? "" : " (Revit's own change)"));
        if (asked) changed++;
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
