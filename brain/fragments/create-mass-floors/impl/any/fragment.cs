// NOT STANDALONE. Assumes `doc`, `elements` and `levelNames` are in scope;
// leaves `added`, `alreadyThere`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// MASS FLOORS on conceptual masses - Mass Floors in the ribbon - at the levels
// named, or at every level the mass reaches when none is named. A mass floor
// is what makes a mass's gross floor area and volume appear in schedules.
//
// A level the mass does not reach gets no floor from Revit; it is asked, and
// the read-back says it did not take.
//
// READ BACK: each mass's floor levels are read again afterwards, and only a
// level now on the list counts.

var added = 0;
var alreadyThere = 0;
var lines = new List<string>();
var findings = new List<string>();

var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var part in (levelNames ?? "").Split(','))
    if (part.Trim() != "") wanted.Add(part.Trim());

var levels = new List<Level>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
{
    var level = element as Level;
    if (level != null && (wanted.Count == 0 || wanted.Contains(level.Name))) levels.Add(level);
}
foreach (var name in wanted)
    if (!levels.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
        lines.Add("No level is named \"" + name + "\".");

var massCategory = new ElementId(BuiltInCategory.OST_Mass);
foreach (var element in elements)
{
    if (element == null || element.Category == null || element.Category.Id != massCategory)
    {
        if (element != null) lines.Add(element.Id + ": not a mass.");
        continue;
    }
    var box = element.get_BoundingBox(null);
    var before = new HashSet<ElementId>(MassInstanceUtils.GetMassLevelIds(doc, element.Id));
    var asked = new List<Level>();
    foreach (var level in levels)
    {
        if (before.Contains(level.Id)) { alreadyThere++; continue; }
        if (wanted.Count == 0 && box != null && (level.ProjectElevation < box.Min.Z - 0.01 || level.ProjectElevation > box.Max.Z + 0.01)) continue;
        try
        {
            MassInstanceUtils.AddMassLevelDataToMassInstance(doc, element.Id, level.Id);
            asked.Add(level);
        }
        catch (Exception ex) { lines.Add("mass " + element.Id + " at " + level.Name + ": Revit refused - " + ex.Message); }
    }
    doc.Regenerate();
    var after = new HashSet<ElementId>(MassInstanceUtils.GetMassLevelIds(doc, element.Id));
    var took = asked.Where(l => after.Contains(l.Id)).Select(l => l.Name).ToList();
    added += took.Count;
    if (took.Count > 0) lines.Add("mass " + element.Id + ": floors at " + string.Join(", ", took) + ".");
    foreach (var level in asked.Where(l => !after.Contains(l.Id)))
        lines.Add("mass " + element.Id + " at " + level.Name + ": asked, and no floor reads back - the mass may not reach it.");
}

var report = string.Join(" | ", lines);
findings.Add(added + " mass floor(s) added and read back; " + alreadyThere + " already there.");
findings.AddRange(lines);
