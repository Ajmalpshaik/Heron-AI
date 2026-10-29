// NOT STANDALONE. Assumes `doc`, `elements` and `usage` are in scope; leaves
// `changed`, `alreadySo`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE USAGE OF MASS FLOORS - the Usage field of each mass floor's
// Properties, what a Mass Floor schedule groups gross floor area by: Retail
// on the ground floor, Office above, Residential at the top.
//
//   usage   "Office" - every mass floor of the masses given; or
//           "Level 1=Retail; Level 2..Level 6=Office; Level 7..Roof=Residential"
//           - each entry levels = usage, entries between semicolons, the
//           levels by name with commas between, "A..B" every level from A to
//           B by height, both included. Nothing after = clears the Usage.
//
// ALL OR NOTHING BEFORE ANYTHING IS WRITTEN: a level name the model has not
// got, or an element that is not a mass, refuses the whole call by name.
// A level the masses have no mass floor at is said, never made -
// CREATE_MASS_FLOORS makes it.
//
// READ BACK: each mass floor's Usage is read again, and only one that now
// reads as asked counts. What it said before is in the report.

var changed = 0;
var alreadySo = 0;
var lines = new List<string>();
var findings = new List<string>();
var problems = new List<string>();

var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
    .OrderBy(l => l.ProjectElevation).ToList();
Func<string, Level> levelNamed = name =>
    allLevels.FirstOrDefault(l => string.Equals(l.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

// What each level is to say. A null key is every level.
var plan = new Dictionary<ElementId, string>();
string everyFloor = null;
var text = (usage ?? "").Trim();
if (text == "") problems.Add("No usage was given - \"Office\" for every mass floor, or \"Level 1=Retail; Level 2..Level 6=Office\".");
else if (text.IndexOf('=') < 0) everyFloor = text;
else
{
    var unknown = new List<string>();
    foreach (var raw in text.Split(';'))
    {
        var entry = raw.Trim();
        if (entry == "") continue;
        var equals = entry.IndexOf('=');
        if (equals < 0) { problems.Add("\"" + entry + "\" has no = - each entry is levels = usage."); continue; }
        var value = entry.Substring(equals + 1).Trim();
        foreach (var rawName in entry.Substring(0, equals).Split(','))
        {
            var item = rawName.Trim();
            if (item == "") continue;
            var targets = new List<Level>();
            var exact = levelNamed(item);
            var dots = item.IndexOf("..", StringComparison.Ordinal);
            if (exact != null) targets.Add(exact);
            else if (dots < 0) { unknown.Add(item); continue; }
            else
            {
                var from = levelNamed(item.Substring(0, dots));
                var to = levelNamed(item.Substring(dots + 2));
                if (from == null) unknown.Add(item.Substring(0, dots).Trim());
                if (to == null) unknown.Add(item.Substring(dots + 2).Trim());
                if (from == null || to == null) continue;
                var low = Math.Min(from.ProjectElevation, to.ProjectElevation) - 1e-6;
                var high = Math.Max(from.ProjectElevation, to.ProjectElevation) + 1e-6;
                targets.AddRange(allLevels.Where(l => l.ProjectElevation >= low && l.ProjectElevation <= high));
            }
            foreach (var level in targets)
            {
                if (plan.ContainsKey(level.Id) && plan[level.Id] != value)
                    problems.Add(level.Name + " is given two usages, \"" + plan[level.Id] + "\" and \"" + value + "\".");
                plan[level.Id] = value;
            }
        }
    }
    if (unknown.Count > 0)
        problems.Add("This model has no level named " + string.Join(", ", unknown.Select(n => "\"" + n + "\""))
            + ". Its levels, lowest first: " + string.Join(", ", allLevels.Take(40).Select(l => l.Name)) + ".");
}

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
    var levelName = allLevels.ToDictionary(l => l.Id, l => l.Name);
    var reached = new HashSet<ElementId>();
    foreach (var mass in massList)
    {
        var floors = MassInstanceUtils.GetMassLevelDataIds(doc, mass.Id)
            .Select(id => doc.GetElement(id) as MassLevelData).Where(f => f != null).ToList();
        if (floors.Count == 0) { lines.Add("mass " + mass.Id + ": no mass floors - CREATE_MASS_FLOORS adds them first."); continue; }
        var wrote = new List<Tuple<MassLevelData, string, string, string>>();
        foreach (var floor in floors)
        {
            string wanted = everyFloor;
            if (wanted == null)
            {
                if (!plan.ContainsKey(floor.LevelId)) continue;
                wanted = plan[floor.LevelId];
            }
            reached.Add(floor.LevelId);
            var name = levelName.ContainsKey(floor.LevelId) ? levelName[floor.LevelId] : "(level " + floor.LevelId + ")";
            var before = floor.StrUsage ?? "";
            if (before == wanted) { alreadySo++; continue; }
            try { floor.StrUsage = wanted; }
            catch (Exception ex) { lines.Add("mass " + mass.Id + " at " + name + ": Revit refused - " + ex.Message); continue; }
            wrote.Add(Tuple.Create(floor, name, before, wanted));
        }
        doc.Regenerate();
        var took = new List<string>();
        foreach (var w in wrote)
        {
            if ((w.Item1.StrUsage ?? "") == w.Item4) took.Add(w.Item2 + " \"" + w.Item3 + "\" -> \"" + w.Item4 + "\"");
            else lines.Add("mass " + mass.Id + " at " + w.Item2 + ": reads \"" + w.Item1.StrUsage + "\" after - not as asked.");
        }
        changed += took.Count;
        if (took.Count > 0) lines.Add("mass " + mass.Id + ": " + string.Join(", ", took) + ".");
    }
    foreach (var levelId in plan.Keys.Where(id => !reached.Contains(id)))
        lines.Add(levelName[levelId] + ": none of these masses has a mass floor there - nothing set; CREATE_MASS_FLOORS makes one.");
}

var report = string.Join(" | ", lines);
if (problems.Count > 0)
{
    findings.Add("NOTHING WAS CHANGED: " + string.Join(" ", problems));
}
else
{
    findings.Add(changed + " mass floor Usage value(s) set and read back; " + alreadySo + " already said so.");
    findings.AddRange(lines);
}
