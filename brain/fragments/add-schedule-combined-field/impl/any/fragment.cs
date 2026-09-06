// NOT STANDALONE. Assumes `doc`, `elements`, `columnName`, `partFields` and
// `partSeparators` are in scope, and leaves `added`, `partMissing`,
// `notASchedule` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE PARTS ARE RESOLVED PER SCHEDULE, NOT ONCE.
//
// What is schedulable depends on the schedule's own category: "Mark" exists
// for one and not for another, and the same name can resolve to different
// parameters. So the lookup runs against each schedule's own list, which is
// also why one schedule can succeed while the next reports a missing part.
//
// ALL OR NOTHING, PER SCHEDULE.
//
// A column built from the parts that happened to resolve is worse than no
// column: it looks right and is missing a value on every row, and nobody
// checks a column that is already there. Where a part is missing, that
// schedule gets nothing and is named.
//
// A COMBINED FIELD JOINS TEXT. IT DOES NOT CALCULATE.
//
// Arithmetic on schedule values is a calculated value - a different mechanism,
// with no public API across the whole release range this library supports. Not
// wired up here on purpose: a fragment that worked on the newer half would be
// a version-shaped hole that reads as green, which is the failure this
// repository is most exposed to.

int added = 0;
var partMissing = new List<ElementId>();
var notASchedule = new List<ElementId>();
var refused = new List<ElementId>();

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;
    if (schedule == null)
    {
        if (element != null) notASchedule.Add(element.Id);
        continue;
    }

    ScheduleDefinition definition = null;
    try { definition = schedule.Definition; } catch { }
    if (definition == null || partFields == null || partFields.Count == 0)
    {
        refused.Add(schedule.Id);
        continue;
    }

    IList<SchedulableField> schedulable = null;
    try { schedulable = definition.GetSchedulableFields(); } catch { }
    if (schedulable == null) { refused.Add(schedule.Id); continue; }

    var parts = new List<TableCellCombinedParameterData>();
    bool everyPartResolved = true;

    for (int i = 0; i < partFields.Count; i++)
    {
        string wanted = partFields[i] ?? "";
        SchedulableField match = null;

        foreach (var candidate in schedulable)
        {
            string name = "";
            try { name = candidate.GetName(doc) ?? ""; } catch { }
            if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                match = candidate;
                break;
            }
        }

        if (match == null) { everyPartResolved = false; break; }

        TableCellCombinedParameterData data = null;
        try
        {
            data = TableCellCombinedParameterData.Create();
            data.ParamId = match.ParameterId;
            data.CategoryId = definition.CategoryId;
            data.Prefix = "";
            data.Suffix = "";
            // Nothing precedes the first value, whatever was passed for it.
            data.Separator = i > 0 && partSeparators != null && i < partSeparators.Count
                ? (partSeparators[i] ?? "")
                : "";
        }
        catch { everyPartResolved = false; break; }

        parts.Add(data);
    }

    if (!everyPartResolved || parts.Count != partFields.Count)
    {
        partMissing.Add(schedule.Id);
        continue;
    }

    try
    {
        definition.InsertCombinedParameterField(parts, columnName ?? "", definition.GetFieldCount());
    }
    catch { refused.Add(schedule.Id); continue; }

    // Read back: the column has to be ON the schedule, not merely accepted.
    bool present = false;
    try
    {
        foreach (var id in definition.GetFieldOrder())
        {
            var field = definition.GetField(id);
            if (field == null) continue;
            if (string.Equals(field.GetName(), columnName ?? "", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(field.ColumnHeading, columnName ?? "", StringComparison.OrdinalIgnoreCase))
            {
                present = true;
                break;
            }
        }
    }
    catch { }

    if (present) added++;
    else refused.Add(schedule.Id);
}
