// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `fieldNames`, `hiddenFieldNames`, `combinedFields`, `filterCount`,
// `sortFieldCount` and `skippedNotSchedules` behind.
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
// EVERY SCHEDULE IS GUARDED SEPARATELY. One schedule that refuses to describe
// itself must not take the report for the other twelve down with it.
//
// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE.
//
// Found 2026-09-08 by the owner doing exactly that: they placed 'Heat Recovery
// Unit Summary' on a sheet, selected it, and this fragment answered
// skippedNotSchedules 1. A view cannot be selected as an element - opening the
// schedule selects its ROWS - and nothing in the library provides a
// ViewSchedule to chain from. So the cast below refused the only input that
// could ever reach it, and the fragment was correct in isolation and unusable
// in practice.
//
// The placement carries the id of the schedule it draws, so it is resolved
// here rather than refused. `skippedNotSchedules` now means what it says: the
// thing was neither a schedule nor a placement of one.
//
// VERSION 2 - FRAGMENT-ISSUES 5b-1292. A COMBINED COLUMN IS READ OPEN. Its
// parts, each part's prefix and suffix, and the separator Revit draws AFTER
// each part - the last one's too, because a separator after the last value is
// exactly the fault that column showed on 2026-10-21 ("20001300x"). Each
// part's parameter is named the way the schedule offers it, so a name read
// here can be typed straight back into ADD_SCHEDULE_COMBINED_FIELD.

var findings = new List<string>();
var fieldNames = new List<string>();
var hiddenFieldNames = new List<string>();
var combinedFields = new List<string>();

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
var sortFieldCount = 0;
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

            // VERSION 2. A combined column, read open. Guarded on its own: one
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
                    combinedFields.Add(string.Format("'{0}' / '{1}', column {2}: draws '{3}' - {4}",
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

        // Filters. Each value read through the getter matching its kind.
        var filters = definition.GetFilters();
        filterCount += filters.Count;
        if (filters.Count == 0)
        {
            findings.Add("    filters: none - every element of the category is in this schedule");
        }
        else
        {
            var rules = new List<string>();
            foreach (var filter in filters)
            {
                var field = definition.GetField(filter.FieldId);
                var fieldName = field == null
                    ? "(a field no longer in the schedule)"
                    : field.GetName();

                string value;
                if (filter.IsNullValue) value = "(no value - the rule is about presence, not content)";
                else if (filter.IsStringValue) value = "'" + filter.GetStringValue() + "'";
                else if (filter.IsIntegerValue) value = filter.GetIntegerValue().ToString();
                else if (filter.IsElementIdValue)
                {
                    var target = doc.GetElement(filter.GetElementIdValue());
                    value = target == null ? "(an element no longer here)" : "'" + target.Name + "'";
                }
                else if (filter.IsDoubleValue)
                {
                    value = string.Format("{0} - REVIT INTERNAL UNITS, not converted; "
                        + "what unit that is depends on the parameter",
                        filter.GetDoubleValue());
                }
                else value = "(a kind of value this report does not read)";

                rules.Add(string.Format("{0} {1} {2}", fieldName, filter.FilterType, value));
            }
            findings.Add(string.Format("    filters ({0}): {1}", filters.Count,
                string.Join("  AND  ", rules)));
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

if (skippedNotSchedules > 0)
{
    findings.Add(string.Format("{0} of what was handed in was not a schedule and was skipped",
        skippedNotSchedules));
}

findings.Insert(0, string.Format("{0} schedule(s) described. {1} column(s) in total, {2} of them "
    + "hidden, {5} combined; {3} filter rule(s); {4} sort/group field(s)",
    elements.Count - skippedNotSchedules, fieldNames.Count, hiddenFieldNames.Count,
    filterCount, sortFieldCount, combinedFields.Count));

// VERSION 2. Each combined column's parts, after the schedule lines.
foreach (var line in combinedFields) findings.Add("    combined column " + line);
