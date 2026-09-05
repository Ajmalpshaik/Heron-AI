// NOT STANDALONE. Assumes `doc`, `elements`, `fieldNames`, `hideOnly` and
// `removeDependentRules` are in scope; leaves `removed`, `hidden`,
// `notPresent`, `refusedInUse`, `rulesRemoved` and `presentFields` behind.
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

var removed = 0;
var hidden = 0;
var notPresent = new List<string>();
var refusedInUse = new List<string>();
var rulesRemoved = 0;
var presentFields = new List<string>();

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;
    if (schedule == null) continue;

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
