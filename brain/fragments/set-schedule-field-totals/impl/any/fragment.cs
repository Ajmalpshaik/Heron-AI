// NOT STANDALONE. Assumes `doc`, `elements` and `fieldNames` are in scope;
// leaves `totalled`, `appliedTotals`, `alreadyTotalled`, `cannotTotal`,
// `notPresent`, `replacedCalculation` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// THIS IS THE "CALCULATE TOTALS" DROP-DOWN ON A SCHEDULE'S FORMATTING TAB, ONE
// COLUMN AT A TIME. Until a column has it, a group footer and the grand total
// print only a COUNT of rows - which is why a schedule grouped by room, with
// footers on, still shows no total flow. Switching the footers on is
// SET_SCHEDULE_SORT_GROUP's job; this is the other half.
//
// ON ONLY. Nothing here turns totals off or asks for a minimum or maximum. A
// column already totalling is reported and left alone, so running this twice
// is how its effect is read back: the second run finds it already totalled.
//
// EVERY COLUMN IS ASKED ABOUT BEFORE IT IS TRIED. CanTotal is Revit's own
// answer - text such as a type name or a room name cannot be added up - and it
// is asked before anything is set, so a refusal changes nothing.
//
// REVIT ALLOWS ONE CALCULATION PER COLUMN. A column showing its minimum or
// maximum is switched to totals, because totals is what was asked for, and
// what it showed before is REPORTED in `replacedCalculation` rather than
// vanishing quietly - the same rule SET_SCHEDULE_SORT_GROUP keeps for a sort.
//
// READ FIRST, WRITE, READ BACK. `appliedTotals` comes from a second read of the
// column, not an echo of what was asked for.

// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE. The same read-through
// as SET_SCHEDULE_SORT_GROUP and REPORT_SCHEDULE_DEFINITION: a schedule is a
// VIEW, a view cannot be selected as an element, and the placement carries the
// id of the schedule it draws.
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

var totalled = 0;
var appliedTotals = new List<string>();
var alreadyTotalled = new List<string>();
var cannotTotal = new List<string>();
var notPresent = new List<string>();
var replacedCalculation = new List<string>();
string refused = null;

var handed = 0;
var schedulesSeen = 0;

// A schedule selected twice - itself and its placement on a sheet, or placed
// on two sheets - is one schedule. Counting it twice would report its own
// change back as "already totalled".
var scheduleIdsSeen = new HashSet<ElementId>();

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
    refused = "no column was named to total, so NOTHING WAS TOTALLED. Name the column as "
        + "it reads in the schedule - 'Flow', for instance";
}

foreach (var element in elements)
{
    if (refused != null) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    if (!scheduleIdsSeen.Add(schedule.Id)) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    // Where a total prints on this schedule: a group footer, the grand total,
    // or neither. Said beside every column switched on, because totals on a
    // schedule with no footer and no grand total print nowhere.
    var footerGroups = new List<string>();
    foreach (var rule in definition.GetSortGroupFields())
    {
        if (!rule.ShowFooter) continue;
        var groupField = definition.GetField(rule.FieldId);
        footerGroups.Add(groupField == null ? "(a field no longer in the schedule)" : groupField.GetName());
    }

    string printsWhere;
    if (footerGroups.Count > 0 && definition.ShowGrandTotal)
        printsWhere = string.Format("prints in each {0} footer and in the grand total",
            string.Join(" / ", footerGroups));
    else if (footerGroups.Count > 0)
        printsWhere = string.Format("prints in each {0} footer; the grand total is off",
            string.Join(" / ", footerGroups));
    else if (definition.ShowGrandTotal)
        printsWhere = "prints in the grand total; no group has a footer";
    else
        printsWhere = "BUT PRINTS NOWHERE YET - no group has a footer and the grand total is "
            + "off. Switch one on in the schedule's Sorting/Grouping";

    foreach (var wanted in fieldNames)
    {
        if (string.IsNullOrEmpty(wanted)) continue;

        // The field name or the printed heading - a heading renamed to
        // "Airflow" still finds the field called Flow, and the other way round.
        ScheduleFieldId foundId = null;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var candidate = definition.GetField(fieldId);
            if (candidate == null) continue;
            if (string.Equals(candidate.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase))
            {
                foundId = fieldId;
                break;
            }
        }

        if (foundId == null)
        {
            notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule, so there "
                + "is nothing to total. Add it first", wanted, schedule.Name));
            continue;
        }

        var field = definition.GetField(foundId);
        var name = field.GetName();

        var canTotal = false;
        try { canTotal = field.CanTotal(); }
        catch (Exception) { canTotal = false; }

        if (!canTotal)
        {
            cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit does not total this column. Only "
                + "a number can be added up; text such as a type name or a room name cannot",
                name, schedule.Name));
            continue;
        }

        var before = field.DisplayType;
        if (before == ScheduleFieldDisplayType.Totals)
        {
            alreadyTotalled.Add(string.Format("'{0}' on '{1}' - already calculates totals, left "
                + "as it was", name, schedule.Name));
            continue;
        }

        try { field.DisplayType = ScheduleFieldDisplayType.Totals; }
        catch (Exception ex)
        {
            cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit refused to total it: {2}",
                name, schedule.Name, ex.Message));
            continue;
        }

        // Read back through a fresh lookup of the same field. What is reported
        // is what a second read found.
        var readBack = definition.GetField(foundId);
        if (readBack == null || readBack.DisplayType != ScheduleFieldDisplayType.Totals)
        {
            cannotTotal.Add(string.Format("'{0}' on '{1}' - asked for totals, a read back finds {2}",
                name, schedule.Name,
                readBack == null ? "the column gone" : readBack.DisplayType.ToString()));
            continue;
        }

        if (before != ScheduleFieldDisplayType.Standard)
        {
            string was;
            if (before == ScheduleFieldDisplayType.MinMax) was = "its minimum and maximum";
            else if (before == ScheduleFieldDisplayType.Max) was = "its maximum";
            else if (before == ScheduleFieldDisplayType.Min) was = "its minimum";
            else was = before.ToString();

            replacedCalculation.Add(string.Format("'{0}' on '{1}' was calculating {2}. Revit allows "
                + "one calculation per column, so it now calculates totals instead", name,
                schedule.Name, was));
        }

        totalled++;
        appliedTotals.Add(string.Format("'{0}' on '{1}': calculates totals - {2}", name,
            schedule.Name, printsWhere));
    }
}

// Decided after the loop, and safe there: an element that is not a schedule
// `continue`s before any column is touched.
if (refused == null && handed > 0 && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS TOTALLED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or find it by name",
        handed);
}
