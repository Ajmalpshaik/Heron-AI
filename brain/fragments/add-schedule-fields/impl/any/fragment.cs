// NOT STANDALONE. Assumes `doc`, `elements` and `fieldNames` are in scope, and
// leaves `added`, `alreadyPresent`, `unknownField`, `availableFields` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a batch of columns across
// several schedules is one undo entry.
//
// WHY THREE OUTCOMES INSTEAD OF ONE COUNTER.
//
// The version this was re-authored from caught every failure into a single
// `notFound` tally, and its own comment admitted what was being run together:
// "already present, or not addable to this schedule type". A genuine spelling
// mistake, a column that was already there, and a field that cannot go on this
// schedule at all are three different situations, and the reply a person needs
// is different for each:
//
//   added            it is there now
//   alreadyPresent   it was there before - NOT a failure, and nothing to do
//   unknownField     no field of that name for this category - check spelling
//
// WHY THE AVAILABLE NAMES COME BACK.
//
// Schedulable fields are CATEGORY-SPECIFIC and are not free text. A name that
// works on a door schedule may not exist on a duct one. Nearly every miss is a
// spelling or a wrong-category assumption, so the real names are the answer to
// the question that comes next.
//
// WHY A COLUMN IS NEVER ADDED TWICE.
//
// Two identical columns look plausible on screen and are wrong on the sheet -
// the same failure as two tags on one element.
//
// WHY IT REFUSES RATHER THAN REPORTING `added 0`.
//
// Handed a schedule placed on a sheet this used to answer `added 0`,
// `availableFields 0` - which reads as "there was nothing to add" and meant "I
// could not see what you gave me". Both legs of a proof then come back
// identical, and on a real project a modeller gets a confident number instead
// of a question. Section 3h.1 of docs/FRAGMENT-ISSUES.md, 2026-09-09.

// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE. The same fix and the
// same reason as REPORT_SCHEDULE_DEFINITION, where it was found on 2026-09-08:
// a schedule is a VIEW, a view cannot be selected as an element, opening one
// selects its ROWS, and nothing in this library provides a ViewSchedule to
// chain from. So a bare cast refuses the only input that could ever arrive.
//
// The placement carries the id of the schedule it draws, so it is resolved
// here rather than passed over.
Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;

    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;

    // Guarded like everything else: a placement whose schedule was deleted
    // under it should cost one element, not the whole run.
    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

int added = 0;
int alreadyPresent = 0;
var unknownField = new List<string>();
var availableFields = new List<string>();
string refused = null;

var handed = 0;
var schedulesSeen = 0;

var wanted = new List<string>();
if (fieldNames != null)
{
    foreach (var name in fieldNames)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length > 0) wanted.Add(trimmed);
    }
}

if (wanted.Count == 0)
{
    refused = "no column names were given, so there is nothing to add. NOTHING WAS ADDED - "
        + "name the fields you want as columns";
}

foreach (var element in elements)
{
    if (refused != null) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    var definition = schedule.Definition;
    var schedulable = definition.GetSchedulableFields();

    // Every field name this schedule COULD carry, gathered once. Reported when
    // something asked for is not among them.
    foreach (var candidate in schedulable)
    {
        var candidateName = candidate.GetName(doc);
        if (!availableFields.Contains(candidateName)) availableFields.Add(candidateName);
    }

    // Every field ALREADY on this schedule, so nothing is added twice.
    var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var existing = definition.GetField(fieldId);
        if (existing != null) present.Add(existing.GetName());
    }

    foreach (var name in wanted)
    {
        if (present.Contains(name))
        {
            // Not a failure. Reporting it as one sends somebody looking for a
            // problem that is a column already doing its job.
            alreadyPresent++;
            continue;
        }

        SchedulableField match = null;
        foreach (var candidate in schedulable)
        {
            if (string.Equals(candidate.GetName(doc), name, StringComparison.OrdinalIgnoreCase))
            {
                match = candidate;
                break;
            }
        }

        if (match == null)
        {
            // No field of that name for this schedule's category. The real
            // names are in availableFields - that is the useful half.
            if (!unknownField.Contains(name)) unknownField.Add(name);
            continue;
        }

        try
        {
            definition.AddField(match);
        }
        catch (Exception)
        {
            // Named, and Revit still refused it - the third case, kept apart
            // from a spelling mistake because the fix is different: choose a
            // different field rather than correct the name.
            if (!unknownField.Contains(name)) unknownField.Add(name);
            continue;
        }

        // THE READ-BACK. AddField returning without throwing is not the column
        // being there - it is asked for again from the definition.
        bool nowPresent = false;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var check = definition.GetField(fieldId);
            if (check != null
                && string.Equals(check.GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                nowPresent = true;
                break;
            }
        }

        if (nowPresent) { added++; present.Add(name); }
        else if (!unknownField.Contains(name)) unknownField.Add(name);
    }
}

availableFields.Sort(StringComparer.OrdinalIgnoreCase);

// Nothing was written on this path - the loop above `continue`s before it
// reaches a definition - so the refusal is decided here safely.
if (refused == null && handed > 0 && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS ADDED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed);
}
