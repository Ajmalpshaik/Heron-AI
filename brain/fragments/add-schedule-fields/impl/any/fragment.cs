// NOT STANDALONE. Assumes `doc`, `elements`, `fieldNames` and `position` are in
// scope, and leaves `added`, `alreadyPresent`, `unknownField`,
// `availableFields`, `notPlaced`, `appliedOrder` and `refused` behind.
//
// VERSION 2 (2026-11-01, FRAGMENT-ISSUES row 5b-1363): `position` says WHERE
// the new columns go. "end" - what a caller of version 1 is bound to, because
// the card declares it as the absent word - is version 1 to the letter:
// ScheduleDefinition.AddField, at the end. Otherwise:
//
//   "3"              the new column becomes the THIRD FIELD IN THE FIELDS
//                    TAB's list - 1 is the first. That list shows hidden
//                    fields too, and so does GetFieldOrder, which is the list
//                    InsertField's index counts in. So a hidden field before
//                    it counts, exactly as it does on screen in the Fields
//                    tab; on the PRINTED schedule the column can sit further
//                    left than 3 when a hidden field comes before it. One more
//                    than the field count is the end.
//   "before Size"    immediately before that column, by field name or printed
//   "after Size"     heading - the cluster's rule: anything but exactly one
//                    column answering to the name is refused (ambiguous).
//
// Several fields asked for go in IN THE ORDER GIVEN from that place - "Mark,
// Size" at 2 makes Mark the 2nd field and Size the 3rd. A field already on the
// schedule is NOT MOVED (alreadyPresent, as in version 1): moving a column is
// SET_SCHEDULE_FIELD_ORDER's job.
//
// A POSITION THAT CANNOT BE FOUND ON A SCHEDULE ADDS NOTHING TO IT. Every
// position is worked out before the first InsertField on that schedule, and a
// schedule whose position fails is named in `notPlaced` and left exactly as
// it was - a column added at the end instead of where it was asked is a
// schedule somebody then has to fix by hand. The other schedules still go on.
//
// READ BACK. `appliedOrder` is each schedule's field order read after the
// change, with the new columns marked; a column that did not land where it
// was asked is named in `notPlaced` with where it did land.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a batch of columns across
// several schedules is one undo entry.
//
// WHY THREE OUTCOMES INSTEAD OF ONE COUNTER.
//
// The version this was re-authored from caught every failure into a single
// `notFound` tally, and its own comment admitted what was being run together:
// "already present, or not addable to this schedule type". A genuine spelling
// mistake, a column that was already there, and a field that cannot go on this
// schedule at all are three different situations, and the reply a person needs
// is different for each:
//
//   added            it is there now
//   alreadyPresent   it was there before - NOT a failure, and nothing to do
//   unknownField     no field of that name for this category - check spelling
//
// WHY THE AVAILABLE NAMES COME BACK.
//
// Schedulable fields are CATEGORY-SPECIFIC and are not free text. A name that
// works on a door schedule may not exist on a duct one. Nearly every miss is a
// spelling or a wrong-category assumption, so the real names are the answer to
// the question that comes next.
//
// WHY A COLUMN IS NEVER ADDED TWICE.
//
// Two identical columns look plausible on screen and are wrong on the sheet -
// the same failure as two tags on one element.
//
// WHY IT REFUSES RATHER THAN REPORTING `added 0`.
//
// Handed a schedule placed on a sheet this used to answer `added 0`,
// `availableFields 0` - which reads as "there was nothing to add" and meant "I
// could not see what you gave me". Both legs of a proof then come back
// identical, and on a real project a modeller gets a confident number instead
// of a question. Section 3h.1 of docs/FRAGMENT-ISSUES.md, 2026-09-09.

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
int alreadyPresent = 0;
var unknownField = new List<string>();
var availableFields = new List<string>();
var notPlaced = new List<string>();
var appliedOrder = "";
string refused = null;
var orderParts = new List<string>();

var handed = 0;
var schedulesSeen = 0;

var wanted = new List<string>();
if (fieldNames != null)
{
    foreach (var name in fieldNames)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length > 0) wanted.Add(trimmed);
    }
}

if (wanted.Count == 0)
{
    refused = "no column names were given, so there is nothing to add. NOTHING WAS ADDED - "
        + "name the fields you want as columns";
}

// VERSION 2: the position, read once. atEnd is version 1. A number is the
// Fields tab place, 1-based; "before"/"after" name a column, matched per
// schedule below.
var placeText = string.Join(" ", (position ?? "").Trim().Split(new[] { ' ', (char)9 },
    StringSplitOptions.RemoveEmptyEntries));
var placeLower = placeText.ToLowerInvariant();
var atEnd = placeLower.Length == 0 || placeLower == "end" || placeLower == "at the end"
    || placeLower == "last";
int placeNumber = 0;
string placeAnchor = null;
var placeAfter = false;
if (refused == null && !atEnd)
{
    int parsed;
    if (placeLower == "first" || placeLower == "start" || placeLower == "at the start") placeNumber = 1;
    else if (int.TryParse(placeLower, System.Globalization.NumberStyles.None,
                 System.Globalization.CultureInfo.InvariantCulture, out parsed) && parsed >= 1)
        placeNumber = parsed;
    else if (placeLower.StartsWith("before ")) placeAnchor = placeText.Substring(7).Trim();
    else if (placeLower.StartsWith("after ")) { placeAnchor = placeText.Substring(6).Trim(); placeAfter = true; }

    if (placeNumber == 0 && string.IsNullOrEmpty(placeAnchor))
    {
        refused = "the position '" + placeText + "' is not one this understands, so NOTHING WAS "
            + "ADDED. Give a place in the Fields tab's list - '3' for the third field, 1 for the "
            + "first - or 'before <column>', 'after <column>', or 'end'";
    }
}

foreach (var element in elements)
{
    if (refused != null) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    var definition = schedule.Definition;
    var schedulable = definition.GetSchedulableFields();

    // Every field name this schedule COULD carry, gathered once. Reported when
    // something asked for is not among them.
    foreach (var candidate in schedulable)
    {
        var candidateName = candidate.GetName(doc);
        if (!availableFields.Contains(candidateName)) availableFields.Add(candidateName);
    }

    // Every field ALREADY on this schedule, so nothing is added twice.
    var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var existing = definition.GetField(fieldId);
        if (existing != null) present.Add(existing.GetName());
    }

    // VERSION 2: where the first new column goes on THIS schedule, worked out
    // before anything is inserted. -1 is the end (version 1's AddField).
    var order = definition.GetFieldOrder();
    var insertAt = -1;
    if (!atEnd)
    {
        if (placeNumber > 0)
        {
            if (placeNumber > order.Count + 1)
            {
                notPlaced.Add(string.Format("'{0}' - it has {1} field(s) in its Fields tab, so place {2} "
                    + "does not exist; give 1 to {3}. NOTHING WAS ADDED to it", schedule.Name,
                    order.Count, placeNumber, order.Count + 1));
                continue;
            }
            insertAt = placeNumber - 1;
        }
        else
        {
            var anchors = new List<int>();
            for (var i = 0; i < order.Count; i++)
            {
                var f = definition.GetField(order[i]);
                if (f == null) continue;
                if (string.Equals(f.GetName(), placeAnchor, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(f.ColumnHeading, placeAnchor, StringComparison.OrdinalIgnoreCase))
                    anchors.Add(i);
            }
            if (anchors.Count != 1)
            {
                notPlaced.Add(string.Format(anchors.Count == 0
                    ? "'{0}' - '{1}' is not a column in it, so there is nowhere to put the new column. "
                      + "NOTHING WAS ADDED to it"
                    : "'{0}' - {2} columns answer to '{1}', by field name or heading, so the place is not "
                      + "clear. NOTHING WAS ADDED to it", schedule.Name, placeAnchor, anchors.Count));
                continue;
            }
            insertAt = anchors[0] + (placeAfter ? 1 : 0);
        }
    }
    var placedHere = new List<KeyValuePair<string, int>>();

    foreach (var name in wanted)
    {
        if (present.Contains(name))
        {
            // Not a failure. Reporting it as one sends somebody looking for a
            // problem that is a column already doing its job.
            alreadyPresent++;
            continue;
        }

        SchedulableField match = null;
        foreach (var candidate in schedulable)
        {
            if (string.Equals(candidate.GetName(doc), name, StringComparison.OrdinalIgnoreCase))
            {
                match = candidate;
                break;
            }
        }

        if (match == null)
        {
            // No field of that name for this schedule's category. The real
            // names are in availableFields - that is the useful half.
            if (!unknownField.Contains(name)) unknownField.Add(name);
            continue;
        }

        try
        {
            if (insertAt < 0) definition.AddField(match);
            else definition.InsertField(match, insertAt);
        }
        catch (Exception)
        {
            // Named, and Revit still refused it - the third case, kept apart
            // from a spelling mistake because the fix is different: choose a
            // different field rather than correct the name.
            if (!unknownField.Contains(name)) unknownField.Add(name);
            continue;
        }

        // THE READ-BACK. AddField returning without throwing is not the column
        // being there - it is asked for again from the definition.
        bool nowPresent = false;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var check = definition.GetField(fieldId);
            if (check != null
                && string.Equals(check.GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                nowPresent = true;
                break;
            }
        }

        if (nowPresent)
        {
            added++;
            present.Add(name);
            if (insertAt >= 0) { placedHere.Add(new KeyValuePair<string, int>(name, insertAt)); insertAt++; }
        }
        else if (!unknownField.Contains(name)) unknownField.Add(name);
    }

    // VERSION 2: read the order back, the new columns marked, and name any
    // that did not land where it was asked.
    var readOrder = new List<string>();
    var fresh = definition.GetFieldOrder();
    for (var i = 0; i < fresh.Count; i++)
    {
        var f = definition.GetField(fresh[i]);
        if (f == null) continue;
        var fieldName = f.GetName();
        var isNew = placedHere.Any(p => string.Equals(p.Key, fieldName, StringComparison.OrdinalIgnoreCase));
        readOrder.Add((i + 1) + " " + fieldName + (isNew ? " (new)" : "") + (f.IsHidden ? " (hidden)" : ""));
    }
    foreach (var p in placedHere)
    {
        var landed = -1;
        for (var i = 0; i < fresh.Count; i++)
        {
            var f = definition.GetField(fresh[i]);
            if (f != null && string.Equals(f.GetName(), p.Key, StringComparison.OrdinalIgnoreCase)) { landed = i; break; }
        }
        if (landed != p.Value)
            notPlaced.Add(string.Format("'{0}' - '{1}' was asked for at place {2} and a read back finds it at "
                + "{3}", schedule.Name, p.Key, p.Value + 1, landed < 0 ? "no place" : (landed + 1).ToString()));
    }
    orderParts.Add(string.Format("'{0}': {1}", schedule.Name, string.Join(", ", readOrder)));
}

appliedOrder = string.Join("; ", orderParts);

availableFields.Sort(StringComparer.OrdinalIgnoreCase);

// Nothing was written on this path - the loop above `continue`s before it
// reaches a definition - so the refusal is decided here safely.
if (refused == null && handed > 0 && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS ADDED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed);
}
