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
// Work Plane-Based family that stays upright on a sloped plane) and Room
// Calculation Point (the point that says which room a placed unit is in).
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
            if (said == "yes" || said == "true" || said == "on" || said == "1") value = 1;
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
                + yesNo(plan.Item2) + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        // A plane switch decides what the next switch may be; Revit's view of
        // it is brought up to date before the next one is written.
        doc.Regenerate();
    }

    foreach (var plan in plans)
    {
        var now = stored(family.get_Parameter(plan.Item1.Item3));
        if (now != plan.Item2)
            throw new InvalidOperationException(plan.Item1.Item2 + " reads " + yesNo(now) + " after the call, not "
                + yesNo(plan.Item2) + ". The call failed, and Heron rolls the whole call back.");
    }

    foreach (var was in before)
    {
        var now = stored(family.get_Parameter(was.Item1.Item3));
        if (now == was.Item2) continue;
        var asked = plans.Any(p => p.Item1.Item1 == was.Item1.Item1);
        rows.Add(was.Item1.Item2 + " " + yesNo(was.Item2) + " -> " + yesNo(now) + (asked ? "" : " (Revit's own change)"));
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
}

if (refused != null) findings.Add(refused);
