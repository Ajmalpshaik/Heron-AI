// NOT STANDALONE. Assumes `doc`, `elements`, `fromColumn`, `toColumn`,
// `caption` and `mode` are in scope; leaves `grouped`, `applied`, `notPresent`
// and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// GROUP AND UNGROUP COLUMN HEADERS - the schedule view's Modify Schedule/
// Quantities, Group and Ungroup. Two or more neighbouring column headings get
// one caption printed above them ("Size" over Width and Height), or a caption
// is taken away. Nothing about the columns themselves changes.
//
// THE COLUMNS ARE NAMED, NOT NUMBERED. `fromColumn` and `toColumn` are a
// field name or its printed heading, matched as every schedule fragment here
// matches them; a name that answers to two columns touches neither, and a
// hidden column is refused - it has no heading on the sheet. Everything
// between the two is in the group, in the schedule's own column order.
//
// THE ROW AND COLUMN NUMBERS ARE AN ASSUMPTION THE PROOF SETTLES
// (NEEDS-CHECKING Group KF). GroupHeaders(top, left, bottom, right, caption)
// takes table cell numbers, and Revit's reference does not say which
// section's numbering they are. Taken here: the BODY section of the
// schedule's table, where the column headings print. A column's number is the
// body's first column plus its place among the VISIBLE fields. The heading
// ROW is not guessed: the body's first rows are read, and the row is the one
// where both end columns read their own headings. When more than one row
// does (a heading merged down under another group), the top one is tried and
// then the bottom one; CanGroupHeaders - Revit's own answer - decides, and a
// no is refused naming every row tried. Nothing is grouped on a guess Revit
// has not accepted.
//
// UNGROUP FINDS THE CAPTION, IT DOES NOT ASSUME ONE. The rows above the
// headings are read for a cell over the named columns - the one reading
// `caption` when it is given - and that cell's merged range is handed to
// CanUngroupHeaders, then UngroupHeaders.
//
// READ FIRST, WRITE, READ BACK. `applied` comes from reading the table again
// after the change: the caption found above the columns after grouping, or
// gone after ungrouping.

var grouped = 0;
var applied = "";
var notPresent = new List<string>();
var refused = new List<string>();

Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;
    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;
    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

{
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    var appliedParts = new List<string>();

    var how = (mode ?? "").Trim().ToLowerInvariant();
    if (how.Length == 0) how = "group";
    var ungroup = how == "ungroup" || how == "un-group" || how == "remove";
    var known = ungroup || how == "group";
    var wantCaption = (caption ?? "").Trim();
    if (string.Equals(wantCaption, "none", StringComparison.OrdinalIgnoreCase)) wantCaption = "";
    var fromName = (fromColumn ?? "").Trim();
    var toName = (toColumn ?? "").Trim();

    var requestUnusable = !known || fromName.Length == 0 || toName.Length == 0;
    if (!known)
        refused.Add("mode takes group or ungroup, not '" + (mode ?? "").Trim() + "'. NOTHING WAS CHANGED");
    else if (fromName.Length == 0 || toName.Length == 0)
        refused.Add("name the first and the last column of the group - fromColumn and toColumn, by field name or "
            + "heading as it prints. NOTHING WAS CHANGED");

    var handed = 0;
    var schedulesSeen = 0;
    var seenIds = new HashSet<ElementId>();

    foreach (var element in elements ?? new List<Element>())
    {
        if (requestUnusable) break;
        if (element == null) continue;
        handed++;
        var schedule = scheduleBehind(element);
        if (schedule == null) continue;
        if (!seenIds.Add(schedule.Id)) continue;
        schedulesSeen++;
        var label = "'" + schedule.Name + "'";

        ScheduleDefinition definition;
        try { definition = schedule.Definition; }
        catch (Exception failure) { refused.Add(label + " - its definition could not be read: " + revitSaid(failure)); continue; }

        if (!definition.ShowHeaders)
        {
            refused.Add(label + " - its headers are switched off (Appearance, Show Headers), so there is no heading "
                + "to group. Nothing changed on it");
            continue;
        }

        // The visible columns, in the schedule's order, and the one each name means.
        var visible = new List<ScheduleField>();
        var all = new List<ScheduleField>();
        foreach (var id in definition.GetFieldOrder())
        {
            var field = definition.GetField(id);
            if (field == null) continue;
            all.Add(field);
            if (!field.IsHidden) visible.Add(field);
        }
        Func<string, ScheduleField> find = wanted =>
        {
            var matches = all.Where(f => string.Equals(f.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0)
            {
                notPresent.Add("'" + wanted + "' on " + label + " - not a column in this schedule");
                return null;
            }
            if (matches.Count > 1)
            {
                refused.Add(label + " - '" + wanted + "': " + matches.Count + " columns answer to that name, by field "
                    + "name or heading, so NONE was touched. Rename a heading, or name the column another way");
                return null;
            }
            if (matches[0].IsHidden)
            {
                refused.Add(label + " - '" + wanted + "' is a hidden column, so it has no heading to group. Show it first");
                return null;
            }
            return matches[0];
        };
        var first = find(fromName);
        var last = find(toName);
        if (first == null || last == null) continue;

        // The same objects as in `all`, so their place is found by reference.
        var a = visible.IndexOf(first);
        var b = visible.IndexOf(last);
        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);
        var span = string.Join(", ", visible.Skip(lo).Take(hi - lo + 1).Select(f => "'" + f.GetName() + "'").ToArray());

        TableSectionData body;
        try { body = schedule.GetTableData().GetSectionData(SectionType.Body); }
        catch (Exception failure) { refused.Add(label + " - its table could not be read: " + revitSaid(failure)); continue; }

        var left = body.FirstColumnNumber + lo;
        var right = body.FirstColumnNumber + hi;
        Func<TableSectionData, int, int, string> cell = (section, row, column) =>
        {
            try { return (section.GetCellText(row, column) ?? "").Trim(); }
            catch { return ""; }
        };
        Func<ScheduleField, string> printed = f =>
            string.IsNullOrEmpty(f.ColumnHeading) ? f.GetName() : f.ColumnHeading;
        var lowField = visible[lo];
        var highField = visible[hi];
        var lastRowRead = Math.Min(body.LastRowNumber, body.FirstRowNumber + 9);

        // The heading rows: where both end columns read their own headings.
        var headingRows = new List<int>();
        for (var row = body.FirstRowNumber; row <= lastRowRead; row++)
        {
            if (string.Equals(cell(body, row, left), printed(lowField).Trim(), StringComparison.Ordinal)
                && string.Equals(cell(body, row, right), printed(highField).Trim(), StringComparison.Ordinal))
                headingRows.Add(row);
        }
        if (headingRows.Count == 0)
        {
            refused.Add(label + " - the headings '" + printed(lowField) + "' and '" + printed(highField) + "' were not "
                + "found in the first rows of the schedule's table (body rows " + body.FirstRowNumber + " to "
                + lastRowRead + ", columns " + left + " and " + right + "), so NOTHING WAS CHANGED on it");
            continue;
        }

        if (!ungroup)
        {
            if (lo == hi)
            {
                refused.Add(label + " - a group needs two or more columns; '" + lowField.GetName() + "' is one. "
                    + "Nothing changed on it");
                continue;
            }
            var tried = new List<string>();
            var chosen = -1;
            foreach (var row in new[] { headingRows.First(), headingRows.Last() }.Distinct())
            {
                tried.Add("rows " + row + " to " + row + ", columns " + left + " to " + right);
                var can = false;
                try { can = schedule.CanGroupHeaders(row, left, row, right); } catch { can = false; }
                if (can) { chosen = row; break; }
            }
            if (chosen < 0)
            {
                refused.Add(label + " - Revit says the headers of " + span + " cannot be grouped (asked at "
                    + string.Join("; ", tried.ToArray()) + " of the body section - the cell numbers are an assumption "
                    + "the proof settles). Nothing changed on it");
                continue;
            }
            try { schedule.GroupHeaders(chosen, left, chosen, right, wantCaption); }
            catch (Exception failure)
            {
                refused.Add(label + " - Revit refused to group " + span + ": " + revitSaid(failure));
                continue;
            }

            // Read back: the caption above the columns, in a fresh read of the table.
            var after = schedule.GetTableData().GetSectionData(SectionType.Body);
            var found = new List<string>();
            var maxRow = Math.Min(after.LastRowNumber, after.FirstRowNumber + 10);
            for (var row = after.FirstRowNumber; row <= maxRow; row++)
            {
                var text = cell(after, row, left);
                // With a caption, the row that reads it; with none, every row
                // down to the headings, so the blank caption can be seen.
                if (wantCaption.Length > 0 ? text == wantCaption : row <= headingRows.First() + 1)
                    found.Add("row " + row + " reads '" + text + "'");
            }
            if (wantCaption.Length > 0 && found.Count == 0)
            {
                throw new InvalidOperationException("Revit grouped " + span + " on " + label + " but the caption '"
                    + wantCaption + "' is not read back above them. Nothing is kept - the change is rolled back");
            }
            grouped++;
            appliedParts.Add(label + ": " + span + " grouped under '" + wantCaption + "' (asked at body row " + chosen
                + ", columns " + left + " to " + right + "); read back: "
                + (found.Count == 0 ? "a blank caption above them" : string.Join(", ", found.ToArray())));
        }
        else
        {
            // The rows above the headings, for a caption cell over these columns.
            TableMergedCell target = null;
            var targetText = "";
            for (var row = body.FirstRowNumber; row < headingRows.Last() && target == null; row++)
            {
                for (var column = left; column <= right && target == null; column++)
                {
                    var text = cell(body, row, column);
                    if (wantCaption.Length > 0 ? text != wantCaption : text.Length == 0) continue;
                    var heading = visible[column - body.FirstColumnNumber];
                    if (wantCaption.Length == 0 && text == printed(heading).Trim()) continue;
                    TableMergedCell merged;
                    try { merged = body.GetMergedCell(row, column); } catch { merged = null; }
                    if (merged == null) continue;
                    var can = false;
                    try { can = schedule.CanUngroupHeaders(merged.Top, merged.Left, merged.Bottom, merged.Right); } catch { can = false; }
                    if (!can) continue;
                    target = merged;
                    targetText = text;
                }
            }
            if (target == null)
            {
                refused.Add(label + " - no group heading" + (wantCaption.Length > 0 ? " reading '" + wantCaption + "'" : "")
                    + " that Revit can ungroup was found above " + span + " (body rows " + body.FirstRowNumber + " to "
                    + (headingRows.Last() - 1) + "). Nothing changed on it");
                continue;
            }
            try { schedule.UngroupHeaders(target.Top, target.Left, target.Bottom, target.Right); }
            catch (Exception failure)
            {
                refused.Add(label + " - Revit refused to ungroup '" + targetText + "': " + revitSaid(failure));
                continue;
            }
            var after = schedule.GetTableData().GetSectionData(SectionType.Body);
            var still = false;
            var maxRow = Math.Min(after.LastRowNumber, after.FirstRowNumber + 10);
            for (var row = after.FirstRowNumber; row <= maxRow && !still; row++)
                for (var column = left; column <= right && !still; column++)
                    if (cell(after, row, column) == targetText && targetText != printed(visible[column - after.FirstColumnNumber]).Trim())
                        still = true;
            if (still)
                throw new InvalidOperationException("Revit was asked to ungroup '" + targetText + "' on " + label
                    + " and the caption still reads back above the columns. Nothing is kept - the change is rolled back");
            grouped++;
            appliedParts.Add(label + ": the group heading '" + targetText + "' over " + span + " removed (cells rows "
                + target.Top + " to " + target.Bottom + ", columns " + target.Left + " to " + target.Right
                + "); read back: no longer above the columns");
        }
    }

    applied = string.Join("; ", appliedParts.ToArray());

    if (!requestUnusable && handed == 0)
        refused.Add("no schedule was handed in - nothing was found by that name, and nothing is selected - so "
            + "NOTHING WAS CHANGED. Check the schedule's name, or click it on a sheet");
    else if (!requestUnusable && handed > 0 && schedulesSeen == 0)
        refused.Add("not one of the " + handed + " element(s) handed in is a schedule, so NOTHING WAS CHANGED. A "
            + "schedule is a VIEW and cannot be selected in the model; what CAN be selected is a schedule PLACED "
            + "ON A SHEET, which this reads through to the schedule behind it");
}
