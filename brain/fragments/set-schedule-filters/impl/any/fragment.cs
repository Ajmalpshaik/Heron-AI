// NOT STANDALONE. Assumes `doc`, `elements`, `filterFieldName`, `filterValue`
// and `matchType` are in scope; leaves `filtered`, `replacedRules`,
// `cannotFilterBy`, `notPresent` and `appliedRules` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// NOTHING IN THE MODEL IS TOUCHED. This changes which rows a table shows. The
// sentence it answers used to resolve to SET_ELEMENT_LEVEL, which moves ducts
// to another level - so it is worth saying plainly in the file itself.
//
// THE MATCH TYPE IS A STATED INPUT AND IS NOT GUESSED FROM THE VALUE. "Equals",
// "contains" and "begins with" produce different drawings from the same word,
// and inferring one is how a schedule silently includes Level 20 when somebody
// asked for Level 2. An unrecognised match type is REFUSED rather than
// defaulted - a wrong default here is invisible on screen.
//
// THE FILTERS ARE REPLACED, NOT ADDED TO, and what was there is reported before
// it goes. A schedule that accumulates filters shows fewer and fewer rows and
// each addition looks correct on its own.
//
// READ FIRST, WRITE, READ BACK. `appliedRules` is a second read of what the
// schedule now filters on.

var filtered = 0;
var replacedRules = new List<string>();
var cannotFilterBy = new List<string>();
var notPresent = new List<string>();
var appliedRules = new List<string>();

// The match type, resolved once. Unrecognised is a refusal, not a default.
var wantedMatch = (matchType ?? "").Trim().ToLowerInvariant();
var matchIsKnown = true;
var filterType = ScheduleFilterType.Equal;

if (wantedMatch == "equals" || wantedMatch == "equal" || wantedMatch == "is")
    filterType = ScheduleFilterType.Equal;
else if (wantedMatch == "notequals" || wantedMatch == "not equals" || wantedMatch == "is not")
    filterType = ScheduleFilterType.NotEqual;
else if (wantedMatch == "contains")
    filterType = ScheduleFilterType.Contains;
else if (wantedMatch == "notcontains" || wantedMatch == "does not contain")
    filterType = ScheduleFilterType.NotContains;
else if (wantedMatch == "beginswith" || wantedMatch == "begins with" || wantedMatch == "starts with")
    filterType = ScheduleFilterType.BeginsWith;
else if (wantedMatch == "endswith" || wantedMatch == "ends with")
    filterType = ScheduleFilterType.EndsWith;
else if (wantedMatch == "hasvalue" || wantedMatch == "has value" || wantedMatch == "is filled")
    filterType = ScheduleFilterType.HasValue;
else if (wantedMatch == "hasnovalue" || wantedMatch == "has no value" || wantedMatch == "is blank")
    filterType = ScheduleFilterType.HasNoValue;
else
    matchIsKnown = false;

var wantsAValue = filterType != ScheduleFilterType.HasValue
    && filterType != ScheduleFilterType.HasNoValue;

if (!matchIsKnown)
{
    cannotFilterBy.Add(string.Format("'{0}' is not a match type this fragment writes. Use one of: "
        + "equals, not equals, contains, does not contain, begins with, ends with, has value, "
        + "has no value. Guessing one from the value is how a schedule quietly includes Level 20 "
        + "when somebody asked for Level 2", matchType));
}

foreach (var element in elements)
{
    if (!matchIsKnown) break;

    var schedule = element as ViewSchedule;
    if (schedule == null) continue;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    // Resolve the field FIRST. Nothing is cleared until the request is known to
    // be workable - clearing and then failing leaves a schedule showing every
    // row, which reads as a successful filter that matched everything.
    ScheduleFieldId foundId = null;
    string foundName = null;
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var field = definition.GetField(fieldId);
        if (field == null) continue;
        if (string.Equals(field.GetName(), filterFieldName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.ColumnHeading, filterFieldName, StringComparison.OrdinalIgnoreCase))
        {
            foundId = fieldId;
            foundName = field.GetName();
            break;
        }
    }

    if (foundId == null)
    {
        notPresent.Add(string.Format("'{0}' on '{1}' - a schedule cannot filter on a field it does "
            + "not carry. Add the column first", filterFieldName, schedule.Name));
        continue;
    }

    var canFilter = false;
    try { canFilter = definition.CanFilterByValue(foundId); }
    catch (Exception) { canFilter = false; }

    if (wantsAValue && !canFilter)
    {
        cannotFilterBy.Add(string.Format("'{0}' on '{1}' - Revit does not allow filtering this "
            + "field by a value", foundName, schedule.Name));
        continue;
    }

    foreach (var existing in definition.GetFilters())
    {
        var field = definition.GetField(existing.FieldId);
        replacedRules.Add(string.Format("'{0}' was filtered on {1} {2}", schedule.Name,
            field == null ? "(a field no longer in the schedule)" : field.GetName(),
            existing.FilterType));
    }

    try
    {
        definition.ClearFilters();
        definition.AddFilter(wantsAValue
            ? new ScheduleFilter(foundId, filterType, filterValue ?? "")
            : new ScheduleFilter(foundId, filterType));
    }
    catch (Exception ex)
    {
        cannotFilterBy.Add(string.Format("'{0}' - Revit refused the filter: {1}",
            schedule.Name, ex.Message));
        continue;
    }

    // Read back. What is reported is what a second read found.
    var after = definition.GetFilters();
    if (after.Count == 1)
    {
        filtered++;
        var field = definition.GetField(after[0].FieldId);
        appliedRules.Add(string.Format("'{0}': {1} {2} {3}", schedule.Name,
            field == null ? "(unknown)" : field.GetName(),
            after[0].FilterType,
            wantsAValue ? "'" + filterValue + "'" : ""));
    }
    else
    {
        cannotFilterBy.Add(string.Format("'{0}' - one filter was written and a read back finds {1}",
            schedule.Name, after.Count));
    }
}
