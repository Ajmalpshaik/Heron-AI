// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `fieldNames`, `hiddenFieldNames`, `combinedFields`, `filterCount`,
// `filterRules`, `sortFieldCount` and `skippedNotSchedules` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A HIDDEN COLUMN IS STILL A COLUMN. Revit keeps a hidden field in the
// definition - it is in the schedule, it is simply not drawn. Somebody asking
// "where did the Level column go" is looking at a schedule that still HAS it,
// so hidden fields are listed and MARKED rather than left out.
//
// EACH FILTER IS ASKED WHAT KIND OF VALUE IT HOLDS BEFORE BEING READ. Asking a
// string filter for its double value throws, and a filter holding no value at
// all is a real and common case - "has a value" and "is blank" are filter types
// somebody chose on purpose.
//
// A NUMERIC FILTER VALUE IS REPORTED IN REVIT'S INTERNAL UNITS AND SAID TO BE.
// Telling a length from an airflow needs the units API, which breaks at 2021
// and is forbidden here. A number converted by a guessed rule looks finished
// and is wrong; a raw number carrying its caveat does not.
//
// EVERY FILTER RULE IS LISTED, NUMBERED, ON ONE LINE (version 2, row 5b-1290).
// `filterRules` names each rule's column by its printed heading, its match
// type in words and its value as Revit shows it - an element value, such as a
// Level rule's level, by NAME. Found on 2026-10-21 on a real project: two
// duplicated schedules came back "filterCount 4" and nothing else, because the
// rules were only a line inside `findings`, and a list is cut to three entries
// before anyone reads it - the filter line is the fourth. The numbers are the
// ones SET_SCHEDULE_FILTERS' `ruleNumber` counts with.
//
// EVERY SCHEDULE IS GUARDED SEPARATELY. One schedule that refuses to describe
// itself must not take the report for the other twelve down with it.
//
// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE.
//
// Found 2026-09-08 by the owner doing exactly that: they placed 'Heat Recovery
// Unit Summary' on a sheet, selected it, and this fragment answered
// skippedNotSchedules 1. A view cannot be selected as an element - opening the
// schedule selects its ROWS - and nothing in the 349 fragments the library
// held on 2026-09-08 provided a ViewSchedule to chain from. So the cast below refused the only input that
// could ever reach it, and the fragment was correct in isolation and unusable
// in practice.
//
// The placement carries the id of the schedule it draws, so it is resolved
// here rather than refused. `skippedNotSchedules` now means what it says: the
// thing was neither a schedule nor a placement of one.
//
// VERSION 3 - FRAGMENT-ISSUES 5b-1292. A COMBINED COLUMN IS READ OPEN. Its
// parts, each part's prefix and suffix, and the separator Revit draws AFTER
// each part - the last one's too, because a separator after the last value is
// exactly the fault that column showed on 2026-10-21 ("20001300x"). Each
// part's parameter is named the way the schedule offers it, so a name read
// here can be typed straight back into ADD_SCHEDULE_COMBINED_FIELD.

var findings = new List<string>();
var fieldNames = new List<string>();
var hiddenFieldNames = new List<string>();
var combinedLines = new List<string>();

// A part's parameter, by the name this schedule offers it under - matched on
// the parameter's id, never read as a number.
Func<IList<SchedulableField>, ElementId, string> partName = (offered, paramId) =>
{
    if (offered != null)
        foreach (var candidate in offered)
        {
            try
            {
                if (candidate.ParameterId == paramId) return candidate.GetName(doc) ?? "";
            }
            catch { }
        }
    try
    {
        var named = doc.GetElement(paramId);
        if (named != null) return named.Name;
    }
    catch { }
    return "(a parameter this schedule no longer offers)";
};

// Text as it is typed, quoted, and an empty one said to be empty - a blank
// separator and a missing one must not read alike.
Func<string, string> quoted = text => string.IsNullOrEmpty(text) ? "none" : "'" + text + "'";
var filterCount = 0;
var filterRules = "";
var filterRuleLines = new List<string>();
var sortFieldCount = 0;

// A match type in the words a person reads. Every member, because any of them
// can be on a schedule somebody else set up.
Func<ScheduleFilterType, string> matchWords = type =>
{
    switch (type)
    {
        case ScheduleFilterType.Equal: return "equals";
        case ScheduleFilterType.NotEqual: return "does not equal";
        case ScheduleFilterType.GreaterThan: return "is greater than";
        case ScheduleFilterType.GreaterThanOrEqual: return "is greater than or equal to";
        case ScheduleFilterType.LessThan: return "is less than";
        case ScheduleFilterType.LessThanOrEqual: return "is less than or equal to";
        case ScheduleFilterType.Contains: return "contains";
        case ScheduleFilterType.NotContains: return "does not contain";
        case ScheduleFilterType.BeginsWith: return "begins with";
        case ScheduleFilterType.NotBeginsWith: return "does not begin with";
        case ScheduleFilterType.EndsWith: return "ends with";
        case ScheduleFilterType.NotEndsWith: return "does not end with";
        case ScheduleFilterType.HasValue: return "has a value";
        case ScheduleFilterType.HasNoValue: return "has no value";
        case ScheduleFilterType.HasParameter: return "has the parameter";
        case ScheduleFilterType.IsAssociatedWithGlobalParameter: return "is associated with a global parameter";
        case ScheduleFilterType.IsNotAssociatedWithGlobalParameter: return "is not associated with a global parameter";
        default: return type.ToString();
    }
};
var skippedNotSchedules = 0;

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;

    if (schedule == null)
    {
        var placed = element as ScheduleSheetInstance;
        if (placed != null)
        {
            // Guarded like everything else here: a placement whose schedule has
            // been deleted under it should cost one row, not the report.
            try { schedule = doc.GetElement(placed.ScheduleId) as ViewSchedule; }
            catch { schedule = null; }
        }
    }

    if (schedule == null)
    {
        skippedNotSchedules++;
        continue;
    }

    try
    {
        var definition = schedule.Definition;
        var order = definition.GetFieldOrder();

        var columns = new List<string>();
        IList<SchedulableField> offered = null;
        int columnNumber = 0;
        foreach (var fieldId in order)
        {
            var field = definition.GetField(fieldId);
            columnNumber++;
            if (field == null) continue;

            var name = field.GetName();
            var heading = field.ColumnHeading;
            fieldNames.Add(name);

            // The heading is what is PRINTED; the name is what the field IS.
            // They are the same until somebody renames the heading, and after
            // that only one of them appears on the drawing.
            var shown = string.IsNullOrEmpty(heading) || heading == name
                ? name
                : string.Format("{0} (printed as '{1}')", name, heading);

            // VERSION 3. A combined column, read open. Guarded on its own: one
            // column that will not open must not lose the schedule's report.
            bool combined = false;
            try { combined = field.IsCombinedParameterField; } catch { }
            if (combined)
            {
                try
                {
                    if (offered == null) offered = definition.GetSchedulableFields();
                    var parts = field.GetCombinedParameters();
                    var drawn = new System.Text.StringBuilder();
                    var each = new List<string>();
                    int partNumber = 0;
                    foreach (var part in parts)
                    {
                        if (part == null) continue;
                        partNumber++;
                        var parameter = partName(offered, part.ParamId);
                        drawn.Append(part.Prefix ?? "").Append("[").Append(parameter).Append("]")
                             .Append(part.Suffix ?? "").Append(part.Separator ?? "");
                        each.Add(string.Format("{0}. {1} - prefix {2}, suffix {3}, separator after it {4}",
                            partNumber, parameter, quoted(part.Prefix), quoted(part.Suffix),
                            quoted(part.Separator)));
                    }
                    shown += string.Format("  [COMBINED - draws '{0}']", drawn);
                    combinedLines.Add(string.Format("'{0}' / '{1}', column {2}: draws '{3}' - {4}",
                        schedule.Name, string.IsNullOrEmpty(heading) ? name : heading, columnNumber,
                        drawn, each.Count == 0 ? "no parts" : string.Join("; ", each)));
                }
                catch (Exception ex)
                {
                    shown += "  [COMBINED - its parts could not be read: " + ex.Message + "]";
                }
            }

            if (field.IsHidden)
            {
                hiddenFieldNames.Add(name);
                columns.Add(shown + "  [HIDDEN - in the schedule, not drawn]");
            }
            else
            {
                columns.Add(shown);
            }
        }

        findings.Add(string.Format("'{0}'  - {1} column(s), in this order: {2}",
            schedule.Name, columns.Count,
            columns.Count == 0 ? "none" : string.Join(" | ", columns)));

        // Sorting and grouping.
        var sortFields = definition.GetSortGroupFields();
        sortFieldCount += sortFields.Count;
        if (sortFields.Count == 0)
        {
            findings.Add("    sorted by: nothing - the rows come back in Revit's own order");
        }
        else
        {
            var sorts = new List<string>();
            foreach (var sortField in sortFields)
            {
                var field = definition.GetField(sortField.FieldId);
                var extras = new List<string>();
                if (sortField.ShowHeader) extras.Add("header");
                if (sortField.ShowFooter) extras.Add("footer");
                if (sortField.ShowBlankLine) extras.Add("blank line");
                sorts.Add(string.Format("{0} {1}{2}",
                    field == null ? "(a field no longer in the schedule)" : field.GetName(),
                    sortField.SortOrder == ScheduleSortOrder.Descending ? "descending" : "ascending",
                    extras.Count == 0 ? "" : " with " + string.Join(" + ", extras)));
            }
            findings.Add("    sorted by: " + string.Join(", then ", sorts));
        }

        // Filters. Each value read through the getter matching its kind, each
        // column by the heading it prints under, numbered in the order the
        // schedule holds them.
        var filters = definition.GetFilters();
        filterCount += filters.Count;
        if (filters.Count == 0)
        {
            findings.Add("    filters: none - every element of the category is in this schedule");
            filterRuleLines.Add(string.Format("'{0}': no filter rules", schedule.Name));
        }
        else
        {
            var rules = new List<string>();
            var number = 0;
            foreach (var filter in filters)
            {
                number++;
                var field = definition.GetField(filter.FieldId);
                var fieldName = "(a field no longer in the schedule)";
                if (field != null)
                {
                    var name = field.GetName();
                    var heading = field.ColumnHeading;
                    fieldName = string.IsNullOrEmpty(heading) || heading == name
                        ? name
                        : string.Format("{0} (field {1})", heading, name);
                }

                string value;
                if (filter.IsNullValue) value = "";
                else if (filter.IsStringValue) value = " '" + filter.GetStringValue() + "'";
                else if (filter.IsIntegerValue) value = " " + filter.GetIntegerValue().ToString();
                else if (filter.IsElementIdValue)
                {
                    var target = doc.GetElement(filter.GetElementIdValue());
                    value = target == null ? " (an element no longer here)" : " '" + target.Name + "'";
                }
                else if (filter.IsDoubleValue)
                {
                    value = string.Format(" {0} - REVIT INTERNAL UNITS, not converted; "
                        + "what unit that is depends on the parameter",
                        filter.GetDoubleValue());
                }
                else value = " (a kind of value this report does not read)";

                rules.Add(string.Format("{0}. {1} {2}{3}", number, fieldName,
                    matchWords(filter.FilterType), value));
            }
            findings.Add(string.Format("    filters ({0}): {1}", filters.Count,
                string.Join("  AND  ", rules)));
            filterRuleLines.Add(string.Format("'{0}': {1}", schedule.Name, string.Join("; ", rules)));
        }

        findings.Add(string.Format("    {0}; grand total {1}",
            definition.IsItemized
                ? "every element on its own row"
                : "rows collapsed - identical elements share a row",
            definition.ShowGrandTotal ? "ON" : "off"));
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("'{0}'  - could not be described: {1}",
            element.Name, ex.Message));
    }
}

filterRules = string.Join("  ||  ", filterRuleLines);

if (skippedNotSchedules > 0)
{
    findings.Add(string.Format("{0} of what was handed in was not a schedule and was skipped",
        skippedNotSchedules));
}

findings.Insert(0, string.Format("{0} schedule(s) described. {1} column(s) in total, {2} of them "
    + "hidden, {5} combined; {3} filter rule(s); {4} sort/group field(s)",
    elements.Count - skippedNotSchedules, fieldNames.Count, hiddenFieldNames.Count,
    filterCount, sortFieldCount, combinedLines.Count));

// VERSION 3. Each combined column's parts, after the schedule lines - and as
// ONE string, a column per line joined by "  ||  ", for the reason filterRules
// is one: a list is shown as three entries cut to sixty characters.
foreach (var line in combinedLines) findings.Add("    combined column " + line);
var combinedFields = string.Join("  ||  ", combinedLines);
