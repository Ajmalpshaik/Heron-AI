// NOT STANDALONE. Assumes `doc`, `elements` and `maxRows` are in scope; leaves
// `findings`, `rows`, `totalBodyRows`, `truncated` and `skippedNotSchedules`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// IT READS THE PRINTED TEXT, DELIBERATELY. `GetCellText` hands back exactly
// what the cell shows, so every number arrives ALREADY FORMATTED IN THE
// PROJECT'S UNITS - "450x250", not 1.4763779527 feet. Reading the underlying
// parameters instead would need a units API to format them, and that is the
// call that breaks at Revit 2021. This route sidesteps it and is the more
// honest answer besides: what the schedule says is what gets issued.
//
// THE HEADER ROW IS READ FROM THE BODY SECTION, NOT FROM `SectionType.Header`.
// Revit's Header section is the schedule's TITLE area, and the column headings
// live in the first body row. Reading Header expecting column names returns the
// schedule's name and a lot of blanks, which looks like a working read.
//
// EVERY SCHEDULE IS GUARDED SEPARATELY, and so is every cell. A schedule with
// one unreadable cell must still report its other four hundred.
//
// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE. Same fix and same
// reason as REPORT_SCHEDULE_DEFINITION, where it was found on 2026-09-08: a
// view cannot be selected as an element, and nothing in the library provides a
// ViewSchedule to chain from, so refusing the placement refused the only input
// that could ever arrive.

var findings = new List<string>();
var rows = new List<string>();
var totalBodyRows = 0;
var truncated = false;
var skippedNotSchedules = 0;

var cap = maxRows > 0 ? maxRows : 50;

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;

    if (schedule == null)
    {
        var placed = element as ScheduleSheetInstance;
        if (placed != null)
        {
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
        var body = schedule.GetTableData().GetSectionData(SectionType.Body);
        var rowCount = body.NumberOfRows;
        var columnCount = body.NumberOfColumns;
        totalBodyRows += rowCount;

        findings.Add(string.Format("'{0}'  - {1} row(s) x {2} column(s) as printed",
            schedule.Name, rowCount, columnCount));

        if (rowCount == 0)
        {
            findings.Add("    EMPTY - the schedule prints no rows at all. Either nothing in the "
                + "model matches its category, or its filters exclude everything");
            continue;
        }

        var taken = 0;
        for (var r = 0; r < rowCount; r++)
        {
            if (taken >= cap)
            {
                truncated = true;
                findings.Add(string.Format("    ... {0} more row(s) not shown - {1} of {2} read",
                    rowCount - taken, taken, rowCount));
                break;
            }

            var cells = new List<string>();
            for (var c = 0; c < columnCount; c++)
            {
                // One guard per cell. A single unreadable cell must not cost
                // the other four hundred rows.
                try { cells.Add(schedule.GetCellText(SectionType.Body, r, c)); }
                catch { cells.Add("(unreadable)"); }
            }

            rows.Add(string.Join(" | ", cells));
            taken++;
        }
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("'{0}'  - could not be read: {1}", element.Name, ex.Message));
    }
}

if (skippedNotSchedules > 0)
{
    findings.Add(string.Format("{0} of what was handed in was not a schedule and was skipped",
        skippedNotSchedules));
}

findings.Insert(0, string.Format("{0} schedule(s) read, {1} row(s) in total{2}",
    elements.Count - skippedNotSchedules, totalBodyRows,
    truncated ? string.Format(" - CUT OFF at {0} row(s) per schedule", cap) : ""));
