// NOT STANDALONE. Assumes `doc`, `elements`, `fieldName`, `newHeading`,
// `columnWidthMm`, `alignment` and `headingSideways` are in scope; leaves
// `changed`, `notPresent`, `refused` and `appliedAppearance` behind.
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

var changed = 0;
var notPresent = new List<string>();
var refused = new List<string>();
var appliedAppearance = new List<string>();

var wantedAlignment = (alignment ?? "").Trim().ToLowerInvariant();

foreach (var element in elements)
{
    var schedule = element as ViewSchedule;
    if (schedule == null) continue;

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
