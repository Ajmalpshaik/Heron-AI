// NOT STANDALONE. Assumes `doc`, `elements` and `levelNames` are in scope;
// leaves `removed`, `notThere`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// TAKES MASS FLOORS OFF conceptual masses - unticking levels in the Mass
// Floors dialog - at the levels NAMED, and only those. There is no "every
// level" here: an empty levelNames refuses, so a call can never clear a
// massing study's floors by leaving a value out.
//
// "A..B" is every level from A to B by height, both included.
//
// ALL OR NOTHING BEFORE ANYTHING IS REMOVED: a level the model has not got,
// or an element that is not a mass, refuses the call by name.
//
// The mass floor's Usage goes with it - it lives on the mass floor - and the
// report names each Usage removed, so it can be put back by hand. Revit's
// Undo puts it all back in one step.
//
// READ BACK: each mass's floor levels are read again, and only a level now
// gone from the list counts.

var removed = 0;
var notThere = 0;
var lines = new List<string>();
var findings = new List<string>();
var problems = new List<string>();

var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
    .OrderBy(l => l.ProjectElevation).ToList();
Func<string, Level> levelNamed = name =>
    allLevels.FirstOrDefault(l => string.Equals(l.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

var targets = new List<Level>();
var unknown = new List<string>();
foreach (var raw in (levelNames ?? "").Split(','))
{
    var item = raw.Trim();
    if (item == "") continue;
    var exact = levelNamed(item);
    if (exact != null) { targets.Add(exact); continue; }
    var dots = item.IndexOf("..", StringComparison.Ordinal);
    if (dots < 0) { unknown.Add(item); continue; }
    var from = levelNamed(item.Substring(0, dots));
    var to = levelNamed(item.Substring(dots + 2));
    if (from == null) unknown.Add(item.Substring(0, dots).Trim());
    if (to == null) unknown.Add(item.Substring(dots + 2).Trim());
    if (from == null || to == null) continue;
    var low = Math.Min(from.ProjectElevation, to.ProjectElevation) - 1e-6;
    var high = Math.Max(from.ProjectElevation, to.ProjectElevation) + 1e-6;
    targets.AddRange(allLevels.Where(l => l.ProjectElevation >= low && l.ProjectElevation <= high));
}
targets = targets.GroupBy(l => l.Id).Select(g => g.First()).ToList();
if (unknown.Count > 0)
    problems.Add("This model has no level named " + string.Join(", ", unknown.Select(n => "\"" + n + "\""))
        + ". Its levels, lowest first: " + string.Join(", ", allLevels.Take(40).Select(l => l.Name)) + ".");
else if (targets.Count == 0)
    problems.Add("No level was named - name the levels whose mass floors go, \"Roof\" or \"Level 5..Level 8\". Nothing is removed by leaving it empty.");

var massCategory = new ElementId(BuiltInCategory.OST_Mass);
var massList = new List<Element>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    if (element.Category == null || element.Category.Id != massCategory || !(element is FamilyInstance))
        problems.Add(element.Id + " is not a mass.");
    else massList.Add(element);
}
if (massList.Count == 0 && problems.Count == 0) problems.Add("No mass was given - select the masses, or hand on REPORT_MASSES' elements.");

if (problems.Count == 0)
{
    foreach (var mass in massList)
    {
        var usageAt = new Dictionary<ElementId, string>();
        foreach (var id in MassInstanceUtils.GetMassLevelDataIds(doc, mass.Id))
        {
            var floor = doc.GetElement(id) as MassLevelData;
            if (floor != null) usageAt[floor.LevelId] = floor.StrUsage ?? "";
        }
        var before = new HashSet<ElementId>(MassInstanceUtils.GetMassLevelIds(doc, mass.Id));
        var asked = new List<Level>();
        foreach (var level in targets)
        {
            if (!before.Contains(level.Id)) { notThere++; continue; }
            MassInstanceUtils.RemoveMassLevelDataFromMassInstance(doc, mass.Id, level.Id);
            asked.Add(level);
        }
        doc.Regenerate();
        var after = new HashSet<ElementId>(MassInstanceUtils.GetMassLevelIds(doc, mass.Id));
        var gone = asked.Where(l => !after.Contains(l.Id)).ToList();
        removed += gone.Count;
        if (gone.Count > 0)
            lines.Add("mass " + mass.Id + ": mass floors removed at " + string.Join(", ", gone.Select(l =>
                l.Name + (usageAt.ContainsKey(l.Id) && usageAt[l.Id] != "" ? " (Usage \"" + usageAt[l.Id] + "\")" : ""))) + ".");
        foreach (var level in asked.Where(l => after.Contains(l.Id)))
            lines.Add("mass " + mass.Id + " at " + level.Name + ": asked, and the mass floor still reads back.");
    }
}

var report = string.Join(" | ", lines);
if (problems.Count > 0) findings.Add("NOTHING WAS REMOVED: " + string.Join(" ", problems));
else
{
    findings.Add(removed + " mass floor(s) removed and read back; " + notThere + " level(s) had none on that mass.");
    findings.AddRange(lines);
}
