// NOT STANDALONE. Assumes `doc`, `elements`, `sortFieldNames`, `descending`,
// `showHeader`, `showFooter`, `showGrandTotal` and `itemized` are in scope;
// leaves `sorted`, `replacedRules`, `cannotSortBy`, `notPresent` and
// `appliedOrder` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// SORTING AND GROUPING ARE ONE CONTROL IN REVIT. There is no separate group-by:
// a sort field with ShowHeader on IS a group, and the header is the band
// carrying the level name across the sheet. "Group by system type" and "sort by
// system type" are this same call with one flag different.
//
// THE SORT IS REPLACED, NOT APPENDED TO. Asking for "sort by level" on a
// schedule already sorted by mark means sorted by LEVEL. Appending is how a
// schedule ends up with four sort levels nobody chose - so what was there is
// REPORTED before it goes, rather than vanishing quietly.
//
// EVERY FIELD IS ASKED ABOUT BEFORE IT IS TRIED. CanSortByField is Revit's own
// answer and it covers what is otherwise an exception half way through a batch
// that has already changed the first two fields.
//
// READ FIRST, WRITE, READ BACK. `appliedOrder` is a second read of what the
// schedule now sorts by, not an echo of what was asked for.

var sorted = 0;
var replacedRules = new List<string>();
var cannotSortBy = new List<string>();
var notPresent = new List<string>();
var appliedOrder = new List<string>();

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;
    if (schedule == null) continue;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    // Resolve every requested name to a field FIRST. Nothing is cleared until
    // the whole request is known to be workable - clearing and then failing
    // leaves a schedule with no sorting at all, which is worse than refusing.
    var wantedIds = new List<ScheduleFieldId>();
    var wantedNames = new List<string>();
    var blocked = false;

    foreach (var wanted in sortFieldNames)
    {
        if (string.IsNullOrEmpty(wanted)) continue;

        ScheduleFieldId foundId = null;
        string foundName = null;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var field = definition.GetField(fieldId);
            if (field == null) continue;
            if (string.Equals(field.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase))
            {
                foundId = fieldId;
                foundName = field.GetName();
                break;
            }
        }

        if (foundId == null)
        {
            notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule, so there "
                + "is nothing to sort by. Add it first", wanted, schedule.Name));
            blocked = true;
            continue;
        }

        var canSort = false;
        try { canSort = definition.CanSortByField(foundId); }
        catch (Exception) { canSort = false; }

        if (!canSort)
        {
            cannotSortBy.Add(string.Format("'{0}' on '{1}' - Revit does not allow sorting on this "
                + "field", foundName, schedule.Name));
            blocked = true;
            continue;
        }

        wantedIds.Add(foundId);
        wantedNames.Add(foundName);
    }

    if (blocked || wantedIds.Count == 0)
    {
        // Nothing is changed on this schedule. See the header.
        continue;
    }

    // Report what is being replaced before it goes.
    foreach (var existing in definition.GetSortGroupFields())
    {
        var field = definition.GetField(existing.FieldId);
        replacedRules.Add(string.Format("'{0}' was sorted by {1} {2}", schedule.Name,
            field == null ? "(a field no longer in the schedule)" : field.GetName(),
            existing.SortOrder == ScheduleSortOrder.Descending ? "descending" : "ascending"));
    }

    try
    {
        definition.ClearSortGroupFields();

        foreach (var fieldId in wantedIds)
        {
            var rule = new ScheduleSortGroupField(fieldId,
                descending ? ScheduleSortOrder.Descending : ScheduleSortOrder.Ascending);
            rule.ShowHeader = showHeader;
            rule.ShowFooter = showFooter;
            // ShowBlankLine is deliberately LEFT AS REVIT HAS IT. A blank line
            // between groups is a separate drafting choice, and tying it to the
            // footer flag would be this fragment inventing a preference and
            // then changing the sheet to match it.
            definition.AddSortGroupField(rule);
        }

        definition.IsItemized = itemized;
        definition.ShowGrandTotal = showGrandTotal;
        if (showGrandTotal)
        {
            definition.ShowGrandTotalTitle = true;
            definition.ShowGrandTotalCount = true;
        }
    }
    catch (Exception ex)
    {
        cannotSortBy.Add(string.Format("'{0}' - Revit refused the sort: {1}",
            schedule.Name, ex.Message));
        continue;
    }

    // Read back. What is reported is what a second read found.
    var after = definition.GetSortGroupFields();
    var readBack = new List<string>();
    foreach (var rule in after)
    {
        var field = definition.GetField(rule.FieldId);
        readBack.Add(string.Format("{0} {1}{2}",
            field == null ? "(unknown)" : field.GetName(),
            rule.SortOrder == ScheduleSortOrder.Descending ? "descending" : "ascending",
            rule.ShowHeader ? " with header (grouped)" : ""));
    }

    appliedOrder.Add(string.Format("'{0}': {1}; {2}; grand total {3}", schedule.Name,
        readBack.Count == 0 ? "nothing" : string.Join(", then ", readBack),
        definition.IsItemized
            ? "every element on its own row"
            : "rows collapsed - identical elements share a row",
        definition.ShowGrandTotal ? "ON" : "off"));

    if (after.Count == wantedIds.Count) sorted++;
    else cannotSortBy.Add(string.Format("'{0}' - asked for {1} sort field(s), a read back finds {2}",
        schedule.Name, wantedIds.Count, after.Count));
}
