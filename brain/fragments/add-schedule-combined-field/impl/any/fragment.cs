// NOT STANDALONE. Assumes `doc`, `elements`, `columnName`, `partFields`,
// `partSeparators`, `editExisting` and `position` are in scope, and leaves
// `added`, `edited`, `columnNow`, `partMissing`, `notASchedule`, `refused` and
// `refusedWhy` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE PARTS ARE RESOLVED PER SCHEDULE, NOT ONCE.
//
// What is schedulable depends on the schedule's own category: "Mark" exists
// for one and not for another, and the same name can resolve to different
// parameters. So the lookup runs against each schedule's own list, which is
// also why one schedule can succeed while the next reports a missing part.
//
// ALL OR NOTHING, PER SCHEDULE.
//
// A column built from the parts that happened to resolve is worse than no
// column: it looks right and is missing a value on every row, and nobody
// checks a column that is already there. Where a part is missing, that
// schedule gets nothing and is named.
//
// A SCHEDULE ON A SHEET IS READ THROUGH TO THE SCHEDULE BEHIND IT, so
// `notASchedule` now means what it says. See the resolver below.
//
// A COMBINED FIELD JOINS TEXT. IT DOES NOT CALCULATE.
//
// Arithmetic on schedule values is a calculated value - a different mechanism,
// with no public API across the whole release range this library supports. Not
// wired up here on purpose: a fragment that worked on the newer half would be
// a version-shaped hole that reads as green, which is the failure this
// repository is most exposed to.
//
// VERSION 2 - FRAGMENT-ISSUES 5b-1292.
//
// REVIT DRAWS A PART'S Separator AFTER THAT PART, NOT BEFORE IT. Version 1
// read `partSeparators[i]` as what goes before part i and wrote it to part i's
// own Separator, with part 0's forced empty. Measured 2026-10-21, Revit 2020,
// 'DUCT HEATER SCHEDULE (ROOF FLOOR)': Duct_Accessory_Width,
// Duct_Accessory_Height with separators "-", "x" drew "20001300x" - the x
// AFTER the height, and nothing between. So version 1 could never put a
// separator between two parts.
//
// THE LIST NOW MEANS "BETWEEN": entry i goes between part i and part i + 1,
// and is written to part i's Separator, where Revit draws it. N parts take
// N - 1 at most; fewer leave nothing in the rest. The last part's Separator is
// always written empty, so nothing is ever drawn after the last value. MORE
// than N - 1 is refused: N entries is version 1's shape, where the first was
// ignored, and reading it either way would guess. Left out ("keep"), a new
// column has none, and an edited one keeps each gap's separator where it was.
//
// `editExisting` true: `columnName` names a column ALREADY on the schedule, by
// printed heading (by field name only when no heading matches), and its parts
// and separators are replaced in place - same field, same place, same
// heading, so its sort, filter, width and total stay. Two columns answering
// to the name, or a plain column, is refused in words and nothing is changed.
//
// `position`: "keep" (absent) puts a new column last, as version 1 did, and
// leaves an edited one where it is. "first", "last", "after <column>",
// "before <column>" or a number (the Nth in Revit's Fields tab list, hidden
// columns counted) put it there. With `editExisting` and no parts and no
// separators - both left out - it moves the column and changes nothing else,
// whatever kind of column it is.

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

int added = 0;
int edited = 0;
var columnNow = new List<string>();
var partMissing = new List<ElementId>();
var notASchedule = new List<ElementId>();
var refused = new List<ElementId>();
var refusedWhy = new List<string>();

string wantedName = (columnName ?? "").Trim();

// "keep" is what a left-out `partFields` or `partSeparators` arrives as (the
// card's `absent: keep`). An empty list is read the same way for the parts:
// a column cannot be made of nothing.
Func<IList<string>, bool> saysKeep = list =>
    list == null || list.Count == 0 ||
    (list.Count == 1 && string.Equals((list[0] ?? "").Trim(), "keep", StringComparison.OrdinalIgnoreCase));

bool keepParts = saysKeep(partFields);
bool keepSeparators = partSeparators != null && partSeparators.Count == 1 &&
    string.Equals((partSeparators[0] ?? "").Trim(), "keep", StringComparison.OrdinalIgnoreCase);
var separatorList = new List<string>();
if (!keepSeparators && partSeparators != null)
    foreach (var s in partSeparators) separatorList.Add(s ?? "");

// THE POSITION, read once. kind: keep | first | last | after | before | at.
string positionText = (position ?? "").Trim();
string positionKind = "keep";
string positionAnchor = "";
int positionNumber = 0;
string positionProblem = null;
{
    string lower = positionText.ToLowerInvariant();
    int number;
    if (lower.Length == 0 || lower == "keep") positionKind = "keep";
    else if (lower == "first" || lower == "start" || lower == "front") positionKind = "first";
    else if (lower == "last" || lower == "end") positionKind = "last";
    else if (lower.StartsWith("after "))
    {
        positionKind = "after";
        positionAnchor = positionText.Substring(6).Trim();
    }
    else if (lower.StartsWith("before "))
    {
        positionKind = "before";
        positionAnchor = positionText.Substring(7).Trim();
    }
    else if (int.TryParse(positionText, System.Globalization.NumberStyles.Integer,
                          System.Globalization.CultureInfo.InvariantCulture, out number))
    {
        positionKind = "at";
        positionNumber = number;
    }
    else positionProblem = "position '" + positionText + "' is not one this tool reads - say first, "
        + "last, after <column>, before <column>, or a column number counted from 1.";

    if (positionProblem == null && (positionKind == "after" || positionKind == "before") && positionAnchor.Length == 0)
        positionProblem = "position '" + positionText + "' names no column to go " + positionKind + ".";
    if (positionProblem == null && positionKind == "at" && positionNumber < 1)
        positionProblem = "position " + positionNumber + " - columns are counted from 1.";
}

// The columns of one schedule that answer to a name: printed heading first,
// field name only when no heading matches - the heading is what the modeller
// reads on the sheet, and SET_SCHEDULE_APPEARANCE's rename is why the two can
// differ.
Func<ScheduleDefinition, string, List<ScheduleField>> columnsCalled = (definition, said) =>
{
    var byHeading = new List<ScheduleField>();
    var byName = new List<ScheduleField>();
    foreach (var id in definition.GetFieldOrder())
    {
        ScheduleField field = null;
        try { field = definition.GetField(id); } catch { }
        if (field == null) continue;
        string heading = "";
        string name = "";
        try { heading = (field.ColumnHeading ?? "").Trim(); } catch { }
        try { name = (field.GetName() ?? "").Trim(); } catch { }
        if (string.Equals(heading, said, StringComparison.OrdinalIgnoreCase)) byHeading.Add(field);
        else if (string.Equals(name, said, StringComparison.OrdinalIgnoreCase)) byName.Add(field);
    }
    return byHeading.Count > 0 ? byHeading : byName;
};

Func<ScheduleField, string> headingOf = field =>
{
    string heading = "";
    try { heading = field.ColumnHeading ?? ""; } catch { }
    if (heading.Length == 0) { try { heading = field.GetName() ?? ""; } catch { } }
    return heading;
};

// A part's parameter, named the way the schedule offers it - the same name
// `partFields` is matched on - so the read-back can be typed straight back in.
Func<ScheduleDefinition, IList<SchedulableField>, ElementId, string> parameterName = (definition, offered, paramId) =>
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
        var element = doc.GetElement(paramId);
        if (element != null) return element.Name;
    }
    catch { }
    return "(a parameter this schedule no longer offers)";
};

// How Revit will draw a combined column: each part's prefix, [its parameter],
// its suffix, then its separator - the last one's too, so a trailing separator
// is seen rather than hidden. A plain column is said to be one.
Func<ScheduleDefinition, IList<SchedulableField>, IList<TableCellCombinedParameterData>, string> drawn =
    (definition, offered, data) =>
{
    var text = new System.Text.StringBuilder();
    if (data == null || data.Count == 0) return "a plain column, not a combined one";
    foreach (var part in data)
    {
        if (part == null) continue;
        text.Append(part.Prefix ?? "");
        text.Append("[").Append(parameterName(definition, offered, part.ParamId)).Append("]");
        text.Append(part.Suffix ?? "");
        text.Append(part.Separator ?? "");
    }
    return "'" + text.ToString() + "'";
};

foreach (var element in elements)
{
    var schedule = scheduleBehind(element);
    if (schedule == null)
    {
        if (element != null) notASchedule.Add(element.Id);
        continue;
    }

    Action<string> refuse = why =>
    {
        refused.Add(schedule.Id);
        refusedWhy.Add("'" + schedule.Name + "' - " + why + " Nothing was changed on this schedule.");
    };

    if (positionProblem != null) { refuse(positionProblem); continue; }

    ScheduleDefinition definition = null;
    try { definition = schedule.Definition; } catch { }
    if (definition == null) { refuse("its definition could not be read."); continue; }

    if (!editExisting && keepParts)
    {
        refuse("a new combined column needs its parts named - partFields was empty.");
        continue;
    }

    IList<SchedulableField> schedulable = null;
    try { schedulable = definition.GetSchedulableFields(); } catch { }
    if (schedulable == null) { refuse("Revit would not list what this schedule can show."); continue; }

    // ---- the column to edit, when editing --------------------------------
    ScheduleField target = null;
    IList<TableCellCombinedParameterData> before = null;
    if (editExisting)
    {
        var found = columnsCalled(definition, wantedName);
        if (found.Count == 0)
        {
            var headings = new List<string>();
            foreach (var id in definition.GetFieldOrder())
            {
                try { headings.Add(headingOf(definition.GetField(id))); } catch { }
            }
            refuse("no column is headed '" + wantedName + "'. Its columns are: "
                + (headings.Count == 0 ? "none" : string.Join(" | ", headings)) + ".");
            continue;
        }
        if (found.Count > 1)
        {
            var places = new List<string>();
            foreach (var f in found) places.Add((definition.GetFieldIndex(f.FieldId) + 1).ToString());
            refuse(found.Count + " columns answer to '" + wantedName + "' (columns "
                + string.Join(" and ", places) + "), so which one is meant is not clear. "
                + "Give one of them another heading first, then ask again.");
            continue;
        }
        target = found[0];

        bool combined = false;
        try { combined = target.IsCombinedParameterField; } catch { }
        if (combined)
        {
            try { before = target.GetCombinedParameters(); } catch { }
        }

        bool replacingParts = !keepParts || !keepSeparators;
        if (replacingParts && !combined)
        {
            refuse("'" + headingOf(target) + "' is a plain column of one parameter, not a combined "
                + "column, so it has no parts to replace. Add a combined column instead (without "
                + "editExisting), then hide or remove the plain one.");
            continue;
        }
        if (replacingParts && (before == null || before.Count == 0))
        {
            refuse("'" + headingOf(target) + "' is a combined column whose parts Revit would not read.");
            continue;
        }
        if (!replacingParts && positionKind == "keep")
        {
            refuse("nothing to change was asked for '" + headingOf(target) + "' - no parts, no "
                + "separators and no position.");
            continue;
        }
    }

    // ---- the parts --------------------------------------------------------
    // Null when the column keeps the parts it has (a move only).
    List<TableCellCombinedParameterData> parts = null;
    string missingName = null;

    if (!keepParts)
    {
        parts = new List<TableCellCombinedParameterData>();
        for (int i = 0; i < partFields.Count; i++)
        {
            string wanted = (partFields[i] ?? "").Trim();
            SchedulableField match = null;

            foreach (var candidate in schedulable)
            {
                string name = "";
                try { name = candidate.GetName(doc) ?? ""; } catch { }
                if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    match = candidate;
                    break;
                }
            }

            if (match == null) { missingName = wanted; break; }

            TableCellCombinedParameterData data = null;
            try
            {
                data = TableCellCombinedParameterData.Create();
                data.ParamId = match.ParameterId;
                data.CategoryId = definition.CategoryId;
                data.Prefix = "";
                data.Suffix = "";
                data.Separator = "";

                // EDITING: a parameter that was already in the column keeps the
                // prefix, suffix and category it had there - what somebody set
                // by hand in Revit's dialog is not this request's to wipe.
                if (before != null)
                    foreach (var old in before)
                    {
                        if (old == null || old.ParamId != match.ParameterId) continue;
                        data.Prefix = old.Prefix ?? "";
                        data.Suffix = old.Suffix ?? "";
                        data.CategoryId = old.CategoryId;
                        break;
                    }
            }
            catch { missingName = wanted; break; }

            parts.Add(data);
        }

        if (missingName != null)
        {
            partMissing.Add(schedule.Id);
            refusedWhy.Add("'" + schedule.Name + "' - '" + missingName + "' is not a parameter this "
                + "schedule can show, so no part of the column was made. Nothing was changed on "
                + "this schedule.");
            continue;
        }
    }
    else if (!keepSeparators && before != null)
    {
        // New separators on the parts the column already has.
        parts = new List<TableCellCombinedParameterData>();
        try
        {
            foreach (var old in before)
            {
                var data = TableCellCombinedParameterData.Create();
                data.ParamId = old.ParamId;
                data.CategoryId = old.CategoryId;
                data.Prefix = old.Prefix ?? "";
                data.Suffix = old.Suffix ?? "";
                data.Separator = "";
                parts.Add(data);
            }
        }
        catch { refuse("its parts could not be copied to take the new separators."); continue; }
    }

    // ---- the separators: BETWEEN parts, never after the last -------------
    if (parts != null)
    {
        if (!keepSeparators)
        {
            if (separatorList.Count > parts.Count - 1)
            {
                refuse(parts.Count + " part(s) take " + (parts.Count - 1) + " separator(s) - one between "
                    + "each pair, none after the last - and " + separatorList.Count + " were given, so "
                    + "which goes where is not clear.");
                continue;
            }
            for (int i = 0; i < parts.Count - 1 && i < separatorList.Count; i++)
                parts[i].Separator = separatorList[i];
        }
        else if (before != null)
        {
            // New parts, separators left out, on a column that had some: each
            // gap keeps the separator it had - a gap, never the old last part's
            // trailing one - and the last part gets none.
            for (int i = 0; i < parts.Count - 1 && i < before.Count - 1; i++)
                parts[i].Separator = before[i] == null ? "" : (before[i].Separator ?? "");
        }
        parts[parts.Count - 1].Separator = "";

        bool valid = false;
        try { valid = definition.IsValidCombinedParameters(parts); } catch { }
        if (!valid)
        {
            refuse("Revit will not combine these parameters on this schedule.");
            continue;
        }
    }

    // ---- where it goes ----------------------------------------------------
    // An index into Revit's own field list (hidden columns counted), worked out
    // with the edited column taken OUT, so "after X" means after X either way.
    var order = new List<ScheduleFieldId>(definition.GetFieldOrder());
    if (target != null) order.Remove(target.FieldId);

    int wantIndex = -1;          // -1: keep (new column last, edited one where it is)
    if (positionKind == "first") wantIndex = 0;
    else if (positionKind == "last") wantIndex = order.Count;
    else if (positionKind == "at")
    {
        if (positionNumber > order.Count + 1)
        {
            refuse("position " + positionNumber + " is past the end - the schedule would have "
                + (order.Count + 1) + " column(s).");
            continue;
        }
        wantIndex = positionNumber - 1;
    }
    else if (positionKind == "after" || positionKind == "before")
    {
        var anchors = columnsCalled(definition, positionAnchor);
        if (target != null) anchors.RemoveAll(f => f.FieldId == target.FieldId);
        if (anchors.Count != 1)
        {
            refuse(anchors.Count == 0
                ? "no other column is headed '" + positionAnchor + "' to go " + positionKind + "."
                : anchors.Count + " columns answer to '" + positionAnchor + "', so where '"
                  + positionKind + "' is, is not clear.");
            continue;
        }
        int at = order.IndexOf(anchors[0].FieldId);
        if (at < 0) { refuse("the column '" + positionAnchor + "' could not be placed in the order."); continue; }
        wantIndex = positionKind == "after" ? at + 1 : at;
    }

    // ---- the change -------------------------------------------------------
    ScheduleField result = null;
    string was = null;
    string headingBefore = null;

    if (target == null)
    {
        try
        {
            result = definition.InsertCombinedParameterField(parts, wantedName,
                wantIndex < 0 ? definition.GetFieldCount() : wantIndex);
        }
        catch { refuse("Revit refused to add the column."); continue; }
    }
    else
    {
        result = target;
        headingBefore = target.ColumnHeading;
        was = drawn(definition, schedulable, before);
        bool partsWritten = false;
        try
        {
            if (parts != null) { target.SetCombinedParameters(parts); partsWritten = true; }

            // The heading is the column's, not the parts': Revit is not known to
            // touch it here, and it is put back if it moved.
            if (target.ColumnHeading != headingBefore) target.ColumnHeading = headingBefore;

            if (wantIndex >= 0)
            {
                order.Insert(wantIndex, target.FieldId);
                definition.SetFieldOrder(order);
            }
        }
        catch
        {
            // Once the parts are written, "nothing was changed" would be false.
            if (!partsWritten) { refuse("Revit refused the change to '" + headingOf(target) + "'."); continue; }
            refused.Add(schedule.Id);
            refusedWhy.Add("'" + schedule.Name + "' - the parts of '" + headingOf(target) + "' were "
                + "replaced, then Revit refused the rest (its heading or its place). Look at this "
                + "column before the change is kept.");
            continue;
        }
    }

    // ---- read back: the column has to be ON the schedule, as asked --------
    bool present = false;
    string now = "";
    int index = -1;
    try
    {
        if (result != null)
        {
            foreach (var id in definition.GetFieldOrder())
                if (id == result.FieldId) { present = true; break; }
            index = definition.GetFieldIndex(result.FieldId);

            // A plain column, moved, has no parts to read - it is said to be plain.
            IList<TableCellCombinedParameterData> kept = null;
            if (result.IsCombinedParameterField) kept = result.GetCombinedParameters();
            now = drawn(definition, schedulable, kept);

            if (present && parts != null)
            {
                if (kept == null || kept.Count != parts.Count) present = false;
                else
                    for (int i = 0; i < parts.Count; i++)
                        if (kept[i].ParamId != parts[i].ParamId ||
                            (kept[i].Separator ?? "") != (parts[i].Separator ?? ""))
                        { present = false; break; }
            }
            if (present && wantIndex >= 0 && index != wantIndex) present = false;
            if (present && headingBefore != null && result.ColumnHeading != headingBefore) present = false;
        }
    }
    catch { present = false; }

    if (!present)
    {
        refused.Add(schedule.Id);
        refusedWhy.Add("'" + schedule.Name + "' - the change did not read back as asked: the column "
            + "reads " + (now.Length == 0 ? "(nothing)" : now) + ". Look at this column before "
            + "the change is kept.");
        continue;
    }

    string line = "'" + schedule.Name + "' / '" + headingOf(result) + "', column " + (index + 1)
        + " of " + definition.GetFieldCount() + ": " + now;
    if (target == null)
    {
        added++;
        columnNow.Add(line + " - added");
    }
    else
    {
        edited++;
        columnNow.Add(line + " - was " + was + (wantIndex >= 0 ? ", moved" : ", same place"));
    }
}
