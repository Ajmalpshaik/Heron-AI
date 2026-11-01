// NOT STANDALONE. Assumes `doc`, `elements`, `fieldNames` and `calculation` are
// in scope; leaves `totalled`, `appliedTotals`, `alreadyTotalled`,
// `cannotTotal`, `notPresent`, `ambiguous`, `replacedCalculation` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// THIS IS THE CALCULATION DROP-DOWN ON A SCHEDULE'S FORMATTING TAB, ONE
// COLUMN AT A TIME - "Calculate totals" above all, and since VERSION 2 also
// "Calculate minimum", "Calculate maximum", "Calculate minimum and maximum"
// and "No calculation". Until a column has it, a group footer and the grand total
// print only a COUNT of rows - which is why a schedule grouped by room, with
// footers on, still shows no total flow. Switching the footers on is
// SET_SCHEDULE_SORT_GROUP's job; this is the other half.
//
// VERSION 2: ONE CALCULATION, NAMED BY `calculation`. "totals" (also left
// out, `absent: totals`, so a version 1 caller is bound exactly as before),
// "minimum", "maximum", "minimum and maximum" ("min", "max", "min and max",
// "min/max"), or "none" - No calculation, ScheduleFieldDisplayType.Standard,
// which is how a calculation is turned OFF. Any other word refuses the whole
// call, naming the words accepted, before any column is touched. A column
// already showing the asked calculation is reported and left alone, so running
// this twice is how its effect is read back: the second run finds it already
// that way. `totalled` counts the columns SET to the asked calculation - the
// name is version 1's, kept so no caller's reading of it changes.
//
// EVERY COLUMN IS ASKED ABOUT BEFORE IT IS TRIED. CanTotal is Revit's own
// answer for totals - text such as a type name or a room name cannot be added
// up - and CanDisplayMinMax its answer for a minimum or maximum. The one that
// matches the asked calculation is asked before anything is set, so a refusal
// changes nothing. "none" needs neither: any column may show no calculation.
//
// A NAME THAT COULD MEAN TWO COLUMNS TOUCHES NEITHER. A column is found by its
// field name or by its printed heading, and a heading can be renamed to
// anything - including another column's field name. Taking the first match
// would total whichever column happens to come first, which may not be the
// one the modeller is looking at. So every column answering to the name is
// collected, and anything but exactly one is refused in `ambiguous`.
//
// REVIT ALLOWS ONE CALCULATION PER COLUMN. A column showing another
// calculation is switched to the asked one, because that is what was asked
// for, and what it showed before is REPORTED in `replacedCalculation` rather
// than vanishing quietly - the same rule SET_SCHEDULE_SORT_GROUP keeps for a
// sort.
//
// READ FIRST, WRITE, READ BACK. `appliedTotals` comes from a second read of the
// column, not an echo of what was asked for. It is ONE STRING, not a list,
// because a list reaches the chat as its first three entries cut at sixty
// characters, and the part that was cut was the part saying where the total
// prints.

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
string appliedTotals = "";
var alreadyTotalled = new List<string>();
var cannotTotal = new List<string>();
var notPresent = new List<string>();
var ambiguous = new List<string>();
var replacedCalculation = new List<string>();
string refused = null;

var handed = 0;
var schedulesSeen = 0;
var appliedParts = new List<string>();

// A schedule selected twice - itself and its placement on a sheet, or placed
// on two sheets - is one schedule. Counting it twice would report its own
// change back as "already totalled".
var scheduleIdsSeen = new HashSet<ElementId>();

// What each calculation is called, as the Formatting tab's drop-down says it.
Func<ScheduleFieldDisplayType, string> calculationText = kind =>
{
    if (kind == ScheduleFieldDisplayType.Totals) return "totals";
    if (kind == ScheduleFieldDisplayType.MinMax) return "its minimum and maximum";
    if (kind == ScheduleFieldDisplayType.Max) return "its maximum";
    if (kind == ScheduleFieldDisplayType.Min) return "its minimum";
    if (kind == ScheduleFieldDisplayType.Standard) return "no calculation";
    return kind.ToString();
};

// VERSION 2: the calculation asked for. Left out it is "totals" (the card's
// `absent: totals`), so version 1 is unchanged. Spaces, case, "and", "&" and
// "/" between minimum and maximum do not matter.
var calculationWord = string.Join(" ", (calculation ?? "").Trim().ToLowerInvariant()
    .Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
ScheduleFieldDisplayType wantedKind = ScheduleFieldDisplayType.Totals;
var kindKnown = true;
switch (calculationWord.Replace(" & ", " and ").Replace("/", " and ").Replace("  ", " "))
{
    case "":
    case "total":
    case "totals":
    case "calculate totals":
        wantedKind = ScheduleFieldDisplayType.Totals; break;
    case "min":
    case "minimum":
    case "calculate minimum":
        wantedKind = ScheduleFieldDisplayType.Min; break;
    case "max":
    case "maximum":
    case "calculate maximum":
        wantedKind = ScheduleFieldDisplayType.Max; break;
    case "min and max":
    case "minimum and maximum":
    case "min max":
    case "minmax":
    case "calculate minimum and maximum":
        wantedKind = ScheduleFieldDisplayType.MinMax; break;
    case "none":
    case "no calculation":
    case "off":
    case "standard":
        wantedKind = ScheduleFieldDisplayType.Standard; break;
    default:
        kindKnown = false; break;
}
var wantedText = calculationText(wantedKind);

var askedFor = 0;
if (fieldNames != null)
{
    foreach (var wantedName in fieldNames)
    {
        if (!string.IsNullOrEmpty(wantedName)) askedFor++;
    }
}

if (!kindKnown)
{
    refused = string.Format("'{0}' is not a calculation a schedule column has, so NOTHING WAS "
        + "CHANGED. The Formatting tab offers: totals, minimum, maximum, minimum and maximum, "
        + "or none (No calculation)", (calculation ?? "").Trim());
}
else if (askedFor == 0)
{
    refused = "no column was named to " + (wantedKind == ScheduleFieldDisplayType.Standard
        ? "clear the calculation of" : "calculate " + wantedText + " for")
        + ", so NOTHING WAS CHANGED. Name the column as it reads in the schedule - 'Flow', "
        + "for instance";
}

foreach (var element in elements ?? new List<Element>())
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

    // Where a calculation prints on this schedule: a group footer, the grand
    // total, or neither. Said beside every column set, because a total, a
    // minimum or a maximum on a schedule with no footer and no grand total
    // prints nowhere.
    var footerGroups = new List<string>();
    foreach (var rule in definition.GetSortGroupFields())
    {
        if (!rule.ShowFooter) continue;
        var groupField = definition.GetField(rule.FieldId);
        footerGroups.Add(groupField == null ? "(a field no longer in the schedule)" : groupField.GetName());
    }

    string printsWhere;
    if (wantedKind == ScheduleFieldDisplayType.Standard)
        printsWhere = "the footers and the grand total no longer calculate it";
    else if (footerGroups.Count > 0 && definition.ShowGrandTotal)
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

    // The same column named twice - "Flow, Flow", or its field name and its
    // heading - is one request. Without this the second pass would find the
    // first pass's change and report it as "already" that way.
    var fieldsDone = new HashSet<ScheduleFieldId>();

    foreach (var wanted in fieldNames)
    {
        if (string.IsNullOrEmpty(wanted)) continue;

        // Every column answering to the name, by field name or by printed
        // heading. One column matching both ways is still one column.
        var matches = new List<ScheduleFieldId>();
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var candidate = definition.GetField(fieldId);
            if (candidate == null) continue;
            if (string.Equals(candidate.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(fieldId);
            }
        }

        if (matches.Count == 0)
        {
            notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule, so there "
                + "is nothing to calculate. Add it first", wanted, schedule.Name));
            continue;
        }

        if (matches.Count > 1)
        {
            ambiguous.Add(string.Format("'{0}' on '{1}' - {2} columns answer to that name, by field "
                + "name or heading, so NONE was touched. Rename a heading, or name the column "
                + "another way", wanted, schedule.Name, matches.Count));
            continue;
        }

        var foundId = matches[0];
        if (!fieldsDone.Add(foundId)) continue;

        var field = definition.GetField(foundId);
        var name = field.GetName();

        // Revit's own answer for the calculation asked, before anything is set.
        if (wantedKind == ScheduleFieldDisplayType.Totals)
        {
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
        }
        else if (wantedKind != ScheduleFieldDisplayType.Standard)
        {
            var canMinMax = false;
            try { canMinMax = field.CanDisplayMinMax(); }
            catch (Exception) { canMinMax = false; }

            if (!canMinMax)
            {
                cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit does not show a minimum or "
                    + "maximum for this column. Only a number has one; text such as a type name or "
                    + "a room name has not", name, schedule.Name));
                continue;
            }
        }

        var before = field.DisplayType;
        if (before == wantedKind)
        {
            alreadyTotalled.Add(string.Format("'{0}' on '{1}' - already {2}, left as it was", name,
                schedule.Name, wantedKind == ScheduleFieldDisplayType.Standard
                    ? "has no calculation" : "calculates " + wantedText));
            continue;
        }

        try { field.DisplayType = wantedKind; }
        catch (Exception ex)
        {
            cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit refused to set {2}: {3}",
                name, schedule.Name, wantedText, ex.Message));
            continue;
        }

        // Read back through a fresh lookup of the same field. What is reported
        // is what a second read found.
        var readBack = definition.GetField(foundId);
        if (readBack == null || readBack.DisplayType != wantedKind)
        {
            cannotTotal.Add(string.Format("'{0}' on '{1}' - asked for {2}, a read back finds {3}",
                name, schedule.Name, wantedText,
                readBack == null ? "the column gone" : calculationText(readBack.DisplayType)));
            continue;
        }

        var was = "";
        if (before != ScheduleFieldDisplayType.Standard)
        {
            was = calculationText(before);
            replacedCalculation.Add(string.Format("'{0}' on '{1}' was calculating {2}. Revit allows "
                + "one calculation per column, so it now {3}", name, schedule.Name, was,
                wantedKind == ScheduleFieldDisplayType.Standard
                    ? "has no calculation" : "calculates " + wantedText + " instead"));
        }

        // A HIDDEN COLUMN'S CALCULATION PRINTS NOWHERE. The setting is kept -
        // it is what was asked for, and it shows the moment the column does -
        // but saying it prints in the footer would be untrue.
        var hidden = false;
        try { hidden = readBack.IsHidden; }
        catch (Exception) { hidden = false; }

        totalled++;
        appliedParts.Add(string.Format("'{0}' on '{1}' now {2}{3} - {4}", name,
            schedule.Name,
            wantedKind == ScheduleFieldDisplayType.Standard ? "has no calculation" : "calculates " + wantedText,
            was.Length == 0 ? "" : string.Format(" (it was calculating {0})", was),
            hidden && wantedKind != ScheduleFieldDisplayType.Standard
                ? "BUT THE COLUMN IS HIDDEN, so it prints nowhere until the column is shown"
                : printsWhere));
    }
}

appliedTotals = string.Join("; ", appliedParts);

// Decided after the loop, and safe there: an element that is not a schedule
// `continue`s before any column is touched.
if (refused == null && handed == 0)
{
    refused = "no schedule was handed in - nothing was found by that name, and nothing is "
        + "selected - so NOTHING WAS CHANGED. Check the schedule's name, or click it on a sheet";
}
else if (refused == null && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS CHANGED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or find it by name",
        handed);
}
