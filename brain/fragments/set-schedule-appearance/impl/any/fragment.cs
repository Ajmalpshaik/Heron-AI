// NOT STANDALONE. Assumes `doc`, `elements`, `fieldName`, `newHeading`,
// `columnWidthMm`, `alignment`, `headingSideways`, `hideOrShow` and
// `headingOrientation` are in scope; leaves `changed`, `notPresent`, `refused`
// and `appliedAppearance` behind.
//
// VERSION 3 (FRAGMENT-ISSUES row 5b-620). Two gaps found 2026-10-15 on a real
// project, Revit 2020:
//
//   ONE COLUMN PER CALL. "Make all the fields horizontal and centre" took one
//   call per field. `fieldName` may now be "all" (every column of the
//   schedule) or a list with commas between - "Mark, Size, Length". Still one
//   call, so still one transaction and one undo entry, and every column is
//   read back on its own line. A name that matches a column EXACTLY, commas
//   and all, is that one column as before - so nothing a version 2 caller
//   sent changes meaning. A NEW HEADING IS NEVER WRITTEN ON MORE THAN ONE
//   COLUMN: the same title printed over every column is a mistake nobody
//   asked for, so it is refused and the rest still applied.
//
//   A HEADING COULD NOT BE TURNED BACK. `headingSideways` false meant "leave
//   it", so nothing set Horizontal. `headingOrientation` "horizontal" or
//   "vertical" now sets it either way; "leave" - the card's `absent:` word,
//   so a caller who never sends it gets exactly version 2 - leaves it, and
//   `headingSideways` true still turns it sideways. The two disagreeing
//   (sideways true, orientation horizontal) is refused, not guessed.
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

// VERSION 3. "leave", "" or "unchanged" leaves the heading as it is; anything
// but horizontal or vertical is refused once, by name, below.
var wantedOrientation = (headingOrientation ?? "").Trim().ToLowerInvariant();
var orientationLeft = wantedOrientation == "" || wantedOrientation == "leave"
    || wantedOrientation == "unchanged";
var orientationKnown = orientationLeft || wantedOrientation == "horizontal"
    || wantedOrientation == "vertical";

var requestUnusable = false;

if (string.IsNullOrEmpty((fieldName ?? "").Trim()))
{
    requestUnusable = true;
    refused.Add("no column was named, so there is nothing to restyle. NOTHING WAS CHANGED - "
        + "name the field or the heading as it prints");
}

// VERSION 3. Refused before anything is written: sideways true says vertical,
// and horizontal says the opposite. Guessing either would undo what the other
// half of the request asked for.
if (!requestUnusable && headingSideways && wantedOrientation == "horizontal")
{
    requestUnusable = true;
    refused.Add("headingSideways is true and headingOrientation is horizontal - they ask for "
        + "opposite things. NOTHING WAS CHANGED - send headingOrientation alone");
}

// VERSION 3. "all", or the whole text matching no column and holding a comma,
// names more than one column. These are the words for every column.
var trimmedFieldName = (fieldName ?? "").Trim();
var everyWord = trimmedFieldName.ToLowerInvariant();
var meansEvery = everyWord == "all" || everyWord == "*" || everyWord == "every"
    || everyWord == "all fields" || everyWord == "all columns"
    || everyWord == "every field" || everyWord == "every column";

Func<ScheduleField, string, bool> answersTo = (field, wanted) =>
    string.Equals(field.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
    || string.Equals(field.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase);

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

    var fields = new List<ScheduleField>();
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var field = definition.GetField(fieldId);
        if (field != null) fields.Add(field);
    }

    // Each target is a column and the words it is reported under: the text as
    // given when it was named, its own name when "all" found it.
    var targets = new List<KeyValuePair<ScheduleField, string>>();

    // The whole text first, exactly as version 2 matched it - so a field
    // called "all", or one whose name holds a comma, is still that one field.
    foreach (var field in fields)
    {
        if (answersTo(field, fieldName))
        {
            targets.Add(new KeyValuePair<ScheduleField, string>(field, fieldName));
            break;
        }
    }

    if (targets.Count == 0 && meansEvery)
    {
        foreach (var field in fields)
            targets.Add(new KeyValuePair<ScheduleField, string>(field, field.GetName()));

        if (targets.Count == 0)
        {
            notPresent.Add(string.Format("'{0}' on '{1}' - this schedule has no columns",
                fieldName, schedule.Name));
            continue;
        }
    }
    else if (targets.Count == 0 && trimmedFieldName.IndexOf(',') >= 0)
    {
        var taken = new HashSet<int>();
        foreach (var piece in trimmedFieldName.Split(','))
        {
            var wanted = piece.Trim();
            if (wanted.Length == 0) continue;

            ScheduleField match = null;
            foreach (var field in fields)
            {
                if (answersTo(field, wanted)) { match = field; break; }
            }

            if (match == null)
            {
                notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule",
                    wanted, schedule.Name));
                continue;
            }

            // The same column named twice, once by name and once by heading,
            // is written and read back once. Told apart by its place in the
            // schedule, which needs no API that differs between releases.
            if (!taken.Add(fields.IndexOf(match))) continue;
            targets.Add(new KeyValuePair<ScheduleField, string>(match, wanted));
        }

        if (targets.Count == 0) continue;
    }

    if (targets.Count == 0)
    {
        notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule",
            fieldName, schedule.Name));
        continue;
    }

    // A heading names one column. Written over several it prints one title
    // above all of them - refused, and every other change still made.
    var headingWanted = !string.IsNullOrEmpty(newHeading);
    if (headingWanted && targets.Count > 1)
    {
        headingWanted = false;
        refused.Add(string.Format("'{0}' on '{1}' - heading NOT written: one heading over {2} "
            + "columns would print the same title above each. Name one column to rename it",
            newHeading, schedule.Name, targets.Count));
    }

    // A word that is not an alignment or an orientation is said once per
    // schedule, as version 2 said it - not once for every column.
    var alignmentRefusedHere = false;
    var orientationRefusedHere = false;

    foreach (var target in targets)
    {
        var found = target.Key;
        var named = target.Value;
        var didSomething = false;

        // Heading text. Empty means leave it alone - a blank heading would wipe a
        // column title off an issued sheet.
        if (headingWanted)
        {
            try { found.ColumnHeading = newHeading; didSomething = true; }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - heading refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }

        // Width. mm to internal feet, plain arithmetic. Zero or less means leave it.
        if (columnWidthMm > 0)
        {
            try { found.GridColumnWidth = columnWidthMm / 304.8; didSomething = true; }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - width refused: {2}",
                    named, schedule.Name, ex.Message));
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
                    named, schedule.Name, ex.Message));
            }
        }
        else if (!string.IsNullOrEmpty(wantedAlignment) && !alignmentRefusedHere)
        {
            alignmentRefusedHere = true;
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
                    named, schedule.Name, ex.Message));
            }
        }

        // VERSION 3. headingOrientation decides when it names a way; otherwise
        // headingSideways true turns the heading sideways, as in version 2, and
        // false leaves it.
        if (!orientationKnown)
        {
            if (!orientationRefusedHere)
                refused.Add(string.Format("'{0}' is not a heading orientation this fragment "
                    + "writes. Use horizontal or vertical", headingOrientation));
            orientationRefusedHere = true;
        }
        else if (!orientationLeft || headingSideways)
        {
            try
            {
                found.HeadingOrientation = wantedOrientation == "horizontal"
                    ? ScheduleHeadingOrientation.Horizontal
                    : ScheduleHeadingOrientation.Vertical;
                didSomething = true;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' on '{1}' - heading orientation refused: {2}",
                    named, schedule.Name, ex.Message));
            }
        }

        if (!didSomething) continue;

        // Read back, one line per column. Width is reported in mm, which is what
        // it arrived as.
        try
        {
            var after = definition.GetField(found.FieldId);
            if (after == null)
            {
                refused.Add(string.Format("'{0}' on '{1}' - changed, and a read back cannot find the "
                    + "field", named, schedule.Name));
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
                named, schedule.Name, ex.Message));
        }
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
