// NOT STANDALONE. Assumes `doc`, `elements`, `fieldName`, `newHeading`,
// `columnWidthMm`, `alignment`, `headingSideways`, `hideOrShow`,
// `headingOrientation` and `calculateTotals` are in scope; leaves `changed`,
// `notPresent`, `refused`, `appliedAppearance`, `ambiguous`, `totalled`,
// `appliedTotals`, `alreadyTotalled`, `cannotTotal` and `replacedCalculation`
// behind.
//
// VERSION 4 (2026-11-01) - SET_SCHEDULE_FIELD_TOTALS MERGED IN.
//
//   CALCULATE TOTALS - the drop-down on a schedule's Formatting tab, per
//   column - when `calculateTotals` is on. Until a column has it, a group
//   footer and the grand total print only a COUNT of rows, which is why a
//   schedule grouped by room, with footers on, still shows no total flow.
//   Switching the footers on is SET_SCHEDULE_SORT_GROUP's job; this is the
//   other half. ON ONLY: nothing here turns totals off.
//
//   CAN IT TOTAL IS ASKED FIRST. CanTotal is Revit's own answer - text such as
//   a type name or a room name cannot be added up - asked before anything is
//   set, so a refusal changes nothing (`cannotTotal`).
//
//   A COLUMN ALREADY TOTALLING is reported in `alreadyTotalled` and left
//   alone, so a second run is the read-back: it finds it already totalled.
//
//   REVIT ALLOWS ONE CALCULATION PER COLUMN. A column showing its minimum or
//   maximum is switched to totals, because totals is what was asked for, and
//   what it showed before is reported in `replacedCalculation`, never lost.
//
//   WHERE IT PRINTS is said beside every column switched on - a footer, the
//   grand total, or NOWHERE YET - and a HIDDEN column's total prints nowhere
//   until the column is shown. ONE STRING, `appliedTotals`, because a list
//   reaches the chat as three entries cut at sixty characters.
//
//   A NAME THAT COULD MEAN TWO COLUMNS TOUCHES NEITHER - now for EVERY change
//   this card makes, not only totals. A column is found by its field name or
//   its printed heading, and a heading can be renamed to another column's
//   field name. Version 3 took the first match, which restyles whichever
//   column happens to come first; the safer rule is kept and the name is
//   named in `ambiguous`.
//
//   A SCHEDULE HANDED IN TWICE - itself and its placement on a sheet - is one
//   schedule, changed and counted once. NOTHING HANDED IN AT ALL is refused
//   in words, never `changed 0`.
//
//   EVERY STYLE INPUT MAY BE LEFT OUT, so "calculate totals for the Flow
//   column" needs only the column and the switch: newHeading, alignment and
//   hideOrShow left out are "leave", columnWidthMm 0, headingSideways false -
//   each the value that already meant "leave it".
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
var ambiguous = new List<string>();
var totalled = 0;
string appliedTotals = "";
var alreadyTotalled = new List<string>();
var cannotTotal = new List<string>();
var replacedCalculation = new List<string>();
var appliedParts = new List<string>();

var handed = 0;
var schedulesSeen = 0;
// VERSION 4: a schedule selected twice - itself and its placement on a sheet,
// or placed on two sheets - is one schedule.
var scheduleIdsSeen = new HashSet<ElementId>();

// VERSION 4: the card's `absent:` word "leave" is the empty that always meant
// "leave it alone".
if (string.Equals((newHeading ?? "").Trim(), "leave", StringComparison.OrdinalIgnoreCase))
    newHeading = "";

var wantedAlignment = (alignment ?? "").Trim().ToLowerInvariant();
if (wantedAlignment == "leave") wantedAlignment = "";

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
    if (!scheduleIdsSeen.Add(schedule.Id)) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    // VERSION 4. Where a total prints on this schedule: a group footer, the
    // grand total, or neither - said beside every column switched on, because
    // totals on a schedule with no footer and no grand total print nowhere.
    string printsWhere = "";
    if (calculateTotals)
    {
        var footerGroups = new List<string>();
        try
        {
            foreach (var rule in definition.GetSortGroupFields())
            {
                if (!rule.ShowFooter) continue;
                var groupField = definition.GetField(rule.FieldId);
                footerGroups.Add(groupField == null ? "(a field no longer in the schedule)" : groupField.GetName());
            }
        }
        catch (Exception) { }

        bool grandTotal = false;
        try { grandTotal = definition.ShowGrandTotal; } catch (Exception) { }

        if (footerGroups.Count > 0 && grandTotal)
            printsWhere = string.Format("prints in each {0} footer and in the grand total",
                string.Join(" / ", footerGroups));
        else if (footerGroups.Count > 0)
            printsWhere = string.Format("prints in each {0} footer; the grand total is off",
                string.Join(" / ", footerGroups));
        else if (grandTotal)
            printsWhere = "prints in the grand total; no group has a footer";
        else
            printsWhere = "BUT PRINTS NOWHERE YET - no group has a footer and the grand total is "
                + "off. Switch one on in the schedule's Sorting/Grouping";
    }

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
    // VERSION 4: EVERY column answering to it is found, and more than one
    // touches NONE - a heading renamed to another column's field name would
    // otherwise restyle whichever came first.
    var wholeMatches = fields.Where(f => answersTo(f, fieldName)).ToList();
    if (wholeMatches.Count > 1)
    {
        ambiguous.Add(string.Format("'{0}' on '{1}' - {2} columns answer to that name, by field "
            + "name or heading, so NONE was touched. Rename a heading, or name the column "
            + "another way", fieldName, schedule.Name, wholeMatches.Count));
        continue;
    }
    if (wholeMatches.Count == 1)
        targets.Add(new KeyValuePair<ScheduleField, string>(wholeMatches[0], fieldName));

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

            // VERSION 4: every column answering to the piece; more than one
            // touches none of them.
            var pieceMatches = fields.Where(f => answersTo(f, wanted)).ToList();
            if (pieceMatches.Count > 1)
            {
                ambiguous.Add(string.Format("'{0}' on '{1}' - {2} columns answer to that name, by "
                    + "field name or heading, so NONE was touched. Rename a heading, or name the "
                    + "column another way", wanted, schedule.Name, pieceMatches.Count));
                continue;
            }
            ScheduleField match = pieceMatches.Count == 1 ? pieceMatches[0] : null;

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

        // VERSION 4, from SET_SCHEDULE_FIELD_TOTALS. CALCULATE TOTALS, asked of
        // Revit first, never turned off, read back through a fresh lookup.
        if (calculateTotals)
        {
            var name = found.GetName();
            var canTotal = false;
            try { canTotal = found.CanTotal(); }
            catch (Exception) { canTotal = false; }

            if (!canTotal)
            {
                cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit does not total this column. "
                    + "Only a number can be added up; text such as a type name or a room name "
                    + "cannot", name, schedule.Name));
            }
            else if (found.DisplayType == ScheduleFieldDisplayType.Totals)
            {
                alreadyTotalled.Add(string.Format("'{0}' on '{1}' - already calculates totals, left "
                    + "as it was", name, schedule.Name));
            }
            else
            {
                var before = found.DisplayType;
                var totalsTook = false;
                try { found.DisplayType = ScheduleFieldDisplayType.Totals; totalsTook = true; }
                catch (Exception ex)
                {
                    cannotTotal.Add(string.Format("'{0}' on '{1}' - Revit refused to total it: {2}",
                        name, schedule.Name, ex.Message));
                }

                ScheduleField readBack = null;
                if (totalsTook)
                {
                    try { readBack = definition.GetField(found.FieldId); } catch (Exception) { }
                    if (readBack == null || readBack.DisplayType != ScheduleFieldDisplayType.Totals)
                    {
                        cannotTotal.Add(string.Format("'{0}' on '{1}' - asked for totals, a read back "
                            + "finds {2}", name, schedule.Name,
                            readBack == null ? "the column gone" : readBack.DisplayType.ToString()));
                        totalsTook = false;
                    }
                }

                if (totalsTook)
                {
                    didSomething = true;

                    var was = "";
                    if (before != ScheduleFieldDisplayType.Standard)
                    {
                        if (before == ScheduleFieldDisplayType.MinMax) was = "its minimum and maximum";
                        else if (before == ScheduleFieldDisplayType.Max) was = "its maximum";
                        else if (before == ScheduleFieldDisplayType.Min) was = "its minimum";
                        else was = before.ToString();

                        replacedCalculation.Add(string.Format("'{0}' on '{1}' was calculating {2}. "
                            + "Revit allows one calculation per column, so it now calculates totals "
                            + "instead", name, schedule.Name, was));
                    }

                    // A HIDDEN COLUMN'S TOTAL PRINTS NOWHERE. The setting is
                    // kept - it shows the moment the column does - but saying
                    // it prints in the footer would be untrue.
                    var isHidden = false;
                    try { isHidden = readBack.IsHidden; } catch (Exception) { isHidden = false; }

                    totalled++;
                    appliedParts.Add(string.Format("'{0}' on '{1}' now calculates totals{2} - {3}",
                        name, schedule.Name,
                        was.Length == 0 ? "" : string.Format(" (it was calculating {0})", was),
                        isHidden
                            ? "BUT THE COLUMN IS HIDDEN, so its total prints nowhere until the column is shown"
                            : printsWhere));
                }
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
else if (handed == 0 && !requestUnusable)
{
    // VERSION 4, from SET_SCHEDULE_FIELD_TOTALS: nothing handed in is said in
    // words, never left to read as "changed 0".
    refused.Add("no schedule was handed in - nothing was found by that name, and nothing is "
        + "selected - so NOTHING WAS CHANGED. Name the schedule, or click it on a sheet");
}

appliedTotals = string.Join("; ", appliedParts);
