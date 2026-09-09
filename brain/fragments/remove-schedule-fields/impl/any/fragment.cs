// NOT STANDALONE. Assumes `doc`, `elements`, `fieldNames`, `hideOnly` and
// `removeDependentRules` are in scope; leaves `removed`, `hidden`,
// `notPresent`, `refusedInUse`, `rulesRemoved`, `presentFields` and `refused`
// behind.
//
// `refusedInUse` AND `refused` ARE DIFFERENT SENTENCES. The first is a finding
// about the schedule - the field is still wired to a rule. The second is this
// fragment saying it could not use what it was handed at all, which used to
// come back as `removed 0`, `presentFields 0` and read as "there was nothing
// to remove". Section 3h.1 of docs/FRAGMENT-ISSUES.md, 2026-09-09.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// HIDING AND REMOVING ARE DIFFERENT JOBS. `hideOnly` keeps the field in the
// definition so a sort or filter that uses it keeps working; the default takes
// the field out altogether. Somebody who sorts by Level and does not want a
// Level column wants the first, and only offering the second breaks their
// schedule while doing what they asked.
//
// A FIELD A RULE STILL POINTS AT IS REFUSED, NOT SILENTLY CASCADED. Revit will
// not release it, and removing the rule too is a SECOND change nobody asked
// for. A schedule quietly losing its filter is how a drawing goes out listing
// every level, so the rules are named and `removeDependentRules` is the caller
// saying yes.
//
// MATCHED ON THE FIELD NAME **OR** THE PRINTED HEADING. Somebody reading a
// sheet says the heading, which is not always the field's name once it has been
// renamed. Both are accepted and the report says which matched.
//
// READ FIRST, WRITE, READ BACK. An absence of exception is not evidence the
// field went - what is reported is what a second read of the field order found.

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

var removed = 0;
var hidden = 0;
var notPresent = new List<string>();
var refusedInUse = new List<string>();
var rulesRemoved = 0;
var presentFields = new List<string>();
string refused = null;

var handed = 0;
var schedulesSeen = 0;

var askedFor = 0;
if (fieldNames != null)
{
    foreach (var wantedName in fieldNames)
    {
        if (!string.IsNullOrEmpty(wantedName)) askedFor++;
    }
}

if (askedFor == 0)
{
    refused = "no column names were given, so there is nothing to remove or hide. NOTHING WAS "
        + "CHANGED - name the fields to take out";
}

foreach (var element in elements)
{
    if (refused != null) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    foreach (var wanted in fieldNames)
    {
        if (string.IsNullOrEmpty(wanted)) continue;

        // Find it by field name or by printed heading.
        ScheduleFieldId foundId = null;
        ScheduleField found = null;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var field = definition.GetField(fieldId);
            if (field == null) continue;
            if (string.Equals(field.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase))
            {
                foundId = fieldId;
                found = field;
                break;
            }
        }

        if (found == null)
        {
            notPresent.Add(string.Format("'{0}' on '{1}'", wanted, schedule.Name));
            continue;
        }

        if (hideOnly)
        {
            try
            {
                found.IsHidden = true;
                // Read back: assigning does not always take.
                var after = definition.GetField(foundId);
                if (after != null && after.IsHidden) hidden++;
                else refusedInUse.Add(string.Format("'{0}' on '{1}' - asked to hide, and a read "
                    + "back says it is still shown", wanted, schedule.Name));
            }
            catch (Exception ex)
            {
                refusedInUse.Add(string.Format("'{0}' on '{1}' - could not be hidden: {2}",
                    wanted, schedule.Name, ex.Message));
            }
            continue;
        }

        // Which rules point at this field. Both lists are walked BACKWARDS
        // because removing by index renumbers everything after it.
        var usedBy = new List<string>();
        var filters = definition.GetFilters();
        for (var i = filters.Count - 1; i >= 0; i--)
        {
            if (filters[i].FieldId != foundId) continue;
            usedBy.Add("a filter rule");
            if (removeDependentRules)
            {
                try { definition.RemoveFilter(i); rulesRemoved++; } catch { }
            }
        }

        var sorts = definition.GetSortGroupFields();
        for (var i = sorts.Count - 1; i >= 0; i--)
        {
            if (sorts[i].FieldId != foundId) continue;
            usedBy.Add("a sort/group rule");
            if (removeDependentRules)
            {
                try { definition.RemoveSortGroupField(i); rulesRemoved++; } catch { }
            }
        }

        if (usedBy.Count > 0 && !removeDependentRules)
        {
            refusedInUse.Add(string.Format("'{0}' on '{1}' - still used by {2}. Removing it would "
                + "change the schedule a second way nobody asked for; pass removeDependentRules to "
                + "say yes, or hide it instead and it keeps working",
                wanted, schedule.Name, string.Join(" and ", usedBy)));
            continue;
        }

        try
        {
            definition.RemoveField(foundId);
        }
        catch (Exception ex)
        {
            refusedInUse.Add(string.Format("'{0}' on '{1}' - Revit refused: {2}",
                wanted, schedule.Name, ex.Message));
            continue;
        }

        // Read back. The field is gone only if a second read cannot find it.
        var stillThere = false;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            if (fieldId == foundId) { stillThere = true; break; }
        }

        if (stillThere)
        {
            refusedInUse.Add(string.Format("'{0}' on '{1}' - asked to remove, and a read back "
                + "still finds it", wanted, schedule.Name));
        }
        else
        {
            removed++;
        }
    }

    // What the schedule carries now, so the caller can see the result rather
    // than infer it from a count.
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var field = definition.GetField(fieldId);
        if (field == null) continue;
        presentFields.Add(string.Format("{0}: {1}{2}", schedule.Name, field.GetName(),
            field.IsHidden ? " [hidden]" : ""));
    }
}

// Decided after the loop because it can only be known by looking, and safe
// there because the loop `continue`s before it reaches a definition.
if (refused == null && handed > 0 && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS CHANGED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed);
}
