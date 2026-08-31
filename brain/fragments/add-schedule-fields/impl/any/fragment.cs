// NOT STANDALONE. Assumes `doc`, `elements` and `fieldNames` are in scope, and
// leaves `added`, `alreadyPresent`, `unknownField` and `availableFields` behind.
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

int added = 0;
int alreadyPresent = 0;
var unknownField = new List<string>();
var availableFields = new List<string>();

var wanted = new List<string>();
if (fieldNames != null)
{
    foreach (var name in fieldNames)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length > 0) wanted.Add(trimmed);
    }
}

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;
    if (schedule == null) continue;

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
