// NOT STANDALONE. Assumes `doc`, `elements`, `fieldName`, `newHeading`,
// `columnWidthMm`, `alignment`, `headingSideways` and `hideOrShow` are in
// scope; leaves `changed`, `notPresent`, `refused` and `appliedAppearance`
// behind.
//
// HIDING IS NOT REMOVING (version 2). A field added to sort or filter by is
// often not meant to print. Hidden, it keeps doing that job off the drawing;
// removed, the sort goes with it and somebody rebuilds it later wondering what
// changed.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// THE HEADING AND THE FIELD NAME ARE DIFFERENT THINGS AND ONLY ONE PRINTS.
// Renaming a heading to "Duct Size" leaves the field called Size - so sorting
// or filtering *by Size* still works and *by Duct Size* has to find it by
// heading. Both are reported after the change for exactly that reason.
//
// WIDTH ARRIVES IN mm AND IS DIVIDED BY 304.8. GridColumnWidth is a Revit
// internal length like any other. Plain arithmetic at the edge, never a units
// API - that is the call that breaks at Revit 2021.
//
// NOTHING HERE MAKES TEXT BOLD. Bold, font and size live on the schedule's TEXT
// TYPE, which other schedules share; changing it to satisfy one sentence would
// restyle drawings nobody asked about. Revit's own route is in the fragment's
// routing table.
//
// AN EMPTY INPUT MEANS "LEAVE IT ALONE", NOT "SET IT TO EMPTY". A blank heading
// would wipe a column title off a sheet, and a zero width would collapse the
// column - both look like the fragment working.
//
// READ FIRST, WRITE, READ BACK. appliedAppearance is a second read.
//
// AND A SCHEDULE IT CANNOT SEE IS REFUSED, NOT COUNTED AS `changed 0`. Handed
// a schedule placed on a sheet this used to change nothing and say nothing,
// which reads as "the column was already like that". Section 3h.1 of
// docs/FRAGMENT-ISSUES.md, 2026-09-09.

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

var changed = 0;
var notPresent = new List<string>();
var refused = new List<string>();
var appliedAppearance = new List<string>();

var handed = 0;
var schedulesSeen = 0;

var wantedAlignment = (alignment ?? "").Trim().ToLowerInvariant();

var requestUnusable = false;

if (string.IsNullOrEmpty((fieldName ?? "").Trim()))
{
    requestUnusable = true;
    refused.Add("no column was named, so there is nothing to restyle. NOTHING WAS CHANGED - "
        + "name the field or the heading as it prints");
}

foreach (var element in elements)
{
    if (requestUnusable) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    ScheduleField found = null;
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var field = definition.GetField(fieldId);
        if (field == null) continue;
        if (string.Equals(field.GetName(), fieldName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.ColumnHeading, fieldName, StringComparison.OrdinalIgnoreCase))
        {
            found = field;
            break;
        }
    }

    if (found == null)
    {
        notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule",
            fieldName, schedule.Name));
        continue;
    }

    var didSomething = false;

    // Heading text. Empty means leave it alone - a blank heading would wipe a
    // column title off an issued sheet.
    if (!string.IsNullOrEmpty(newHeading))
    {
        try { found.ColumnHeading = newHeading; didSomething = true; }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on '{1}' - heading refused: {2}",
                fieldName, schedule.Name, ex.Message));
        }
    }

    // Width. mm to internal feet, plain arithmetic. Zero or less means leave it.
    if (columnWidthMm > 0)
    {
        try { found.GridColumnWidth = columnWidthMm / 304.8; didSomething = true; }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on '{1}' - width refused: {2}",
                fieldName, schedule.Name, ex.Message));
        }
    }

    if (wantedAlignment == "left" || wantedAlignment == "center" || wantedAlignment == "centre"
        || wantedAlignment == "right")
    {
        try
        {
            found.HorizontalAlignment = wantedAlignment == "left"
                ? ScheduleHorizontalAlignment.Left
                : wantedAlignment == "right"
                    ? ScheduleHorizontalAlignment.Right
                    : ScheduleHorizontalAlignment.Center;
            didSomething = true;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on '{1}' - alignment refused: {2}",
                fieldName, schedule.Name, ex.Message));
        }
    }
    else if (!string.IsNullOrEmpty(wantedAlignment))
    {
        refused.Add(string.Format("'{0}' is not an alignment this fragment writes. Use left, "
            + "centre or right", alignment));
    }

    // VERSION 2. Empty leaves it alone, exactly like every other input here -
    // a fragment that wrote a default would un-hide a column somebody hid
    // deliberately, every time it was called to change a width.
    string visibility = (hideOrShow ?? "").Trim().ToLowerInvariant();
    if (visibility == "hide" || visibility == "show")
    {
        try
        {
            found.IsHidden = visibility == "hide";
            didSomething = true;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on '{1}' - hiding refused: {2}",
                fieldName, schedule.Name, ex.Message));
        }
    }

    if (headingSideways)
    {
        try
        {
            found.HeadingOrientation = ScheduleHeadingOrientation.Vertical;
            didSomething = true;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on '{1}' - heading orientation refused: {2}",
                fieldName, schedule.Name, ex.Message));
        }
    }

    if (!didSomething) continue;

    // Read back. Width is reported in mm, which is what it arrived as.
    try
    {
        var after = definition.GetField(found.FieldId);
        if (after == null)
        {
            refused.Add(string.Format("'{0}' on '{1}' - changed, and a read back cannot find the "
                + "field", fieldName, schedule.Name));
            continue;
        }

        appliedAppearance.Add(string.Format(
            "'{0}' / field '{1}': prints as '{2}', {3:0.#} mm wide, {4}, heading {5}",
            schedule.Name, after.GetName(), after.ColumnHeading,
            after.GridColumnWidth * 304.8, after.HorizontalAlignment,
            after.HeadingOrientation));
        changed++;
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("'{0}' on '{1}' - could not be read back: {2}",
            fieldName, schedule.Name, ex.Message));
    }
}

// Decided after the loop, and safe there: every element that is not a schedule
// `continue`s before a definition is reached, so nothing has been written.
if (handed > 0 && schedulesSeen == 0)
{
    refused.Add(string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS CHANGED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed));
}
