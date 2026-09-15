// NOT STANDALONE. Assumes `doc` and `nameContains` are in scope; leaves
// `elements`, `scheduleCount`, `scheduleList`, `unused`, `scanned` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// NOTHING ELSE CAN REACH A PIPE SCHEDULE. It is an ElementType with NO
// CATEGORY, so the category-based selectors refuse it - correctly - and it
// never appears in a view, so it cannot be clicked. `elements` is provided so
// the answer can be handed on to a selection and then acted on.
//
// WHICH SEGMENTS USE IT IS THE WHOLE POINT. Removing a schedule a segment
// still uses takes the segment with it and Revit does not warn first, so the
// users are named rather than counted.
//
// THE LIST COMES BACK AS ONE STRING. A list is abbreviated to its first three
// entries before a reader sees it, and "there are three" is then a wrong
// answer nobody can tell from a right one.
//
// AN EXACT NAME BEATS A PREFIX, so naming one schedule cannot also return a
// longer one that merely starts the same way.

var findings = new List<string>();
var elements = new List<Element>();
var unused = new List<string>();
var scheduleCount = 0;
var scanned = 0;
var scheduleList = "";

var needle = (nameContains ?? "").Trim();
var rows = new List<string>();

var schedules = new List<PipeScheduleType>();
try
{
    foreach (var found in new FilteredElementCollector(doc)
        .OfClass(typeof(PipeScheduleType)).ToElements())
    {
        var schedule = found as PipeScheduleType;
        if (schedule != null) schedules.Add(schedule);
    }
}
catch (Exception) { }

scanned = schedules.Count;

// WHO USES WHAT, built once. Walking the segments per schedule would read the
// project N times to answer one question.
var usersBySchedule = new Dictionary<string, List<string>>();
try
{
    foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(PipeSegment)).ToElements())
    {
        var segment = found as PipeSegment;
        if (segment == null) continue;

        var key = segment.ScheduleTypeId == null ? "" : segment.ScheduleTypeId.ToString();
        if (key.Length == 0) continue;

        if (!usersBySchedule.ContainsKey(key)) usersBySchedule[key] = new List<string>();
        usersBySchedule[key].Add(segment.Name ?? "<unnamed>");
    }
}
catch (Exception) { }

// See the header - an exact name is not an ambiguous request.
var exactName = false;
if (needle.Length > 0)
{
    foreach (var schedule in schedules)
    {
        if (schedule.Name != null
            && string.Equals(schedule.Name, needle, StringComparison.OrdinalIgnoreCase))
        {
            exactName = true;
            break;
        }
    }
}

foreach (var schedule in schedules)
{
    var name = schedule.Name ?? "<unnamed>";

    if (exactName)
    {
        if (!string.Equals(name, needle, StringComparison.OrdinalIgnoreCase)) continue;
    }
    else if (needle.Length > 0
        && name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
        continue;

    scheduleCount++;
    elements.Add(schedule);

    var key = schedule.Id == null ? "" : schedule.Id.ToString();
    var users = usersBySchedule.ContainsKey(key) ? usersBySchedule[key] : new List<string>();

    if (users.Count == 0)
    {
        unused.Add(name);
        rows.Add(string.Format("{0} [UNUSED]", name));
        findings.Add(string.Format("'{0}' is used by NO segment - it can be removed", name));
    }
    else
    {
        rows.Add(string.Format("{0} [used by {1}]", name, string.Join(", ", users.ToArray())));
        findings.Add(string.Format("'{0}' is used by {1} segment(s): {2}. Removing it would take "
            + "them with it, and Revit does not warn first",
            name, users.Count, string.Join(", ", users.ToArray())));
    }
}

scheduleList = string.Join("  ||  ", rows.ToArray());

findings.Insert(0, string.Format("{0} pipe schedule(s) reported of {1} scanned; {2} used by "
    + "nothing", scheduleCount, scanned, unused.Count));

if (scheduleCount == 0 && scanned > 0)
{
    findings.Add(string.Format("Nothing matched '{0}'. The {1} schedule(s) in this project are "
        + "named something else - run it again with an empty needle to see them all",
        needle, scanned));
}
