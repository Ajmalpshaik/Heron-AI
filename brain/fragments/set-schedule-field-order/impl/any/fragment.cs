// NOT STANDALONE. Assumes `doc`, `elements` and `order` are in scope; leaves
// `reordered`, `alreadyInOrder`, `appliedOrder`, `notPresent`, `notKept` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16). One undo
// puts every schedule's columns back.
//
// THE FIELDS TAB'S "MOVE UP / MOVE DOWN", IN ONE CALL. `order` is either
//
//   "Mark, Size, Length"     the columns named go FIRST, in that order, and
//                            every other column keeps its own order after
//                            them - so naming the whole list sets the whole
//                            order, and naming two moves just those two to
//                            the front;
//   "move Length to 2"       that column becomes the 2nd field in the Fields
//   "move Length to the front" / "... to the end"
//   "move Length before Size" / "move Length after Size"
//
// A PLACE IS A PLACE IN THE FIELDS TAB'S LIST, 1 THE FIRST, AND HIDDEN FIELDS
// COUNT - that list shows them, and GetFieldOrder, which SetFieldOrder takes
// back, holds them. On the printed schedule a column can therefore sit
// further left than its number when a hidden field comes before it; the
// read-back marks hidden fields so the two can be told apart.
//
// A COLUMN IS FOUND BY ITS FIELD NAME OR ITS PRINTED HEADING, and a name that
// answers to more than one column - a heading renamed to another column's
// field name - touches nothing on that schedule. The cluster's rule, kept
// with SET_SCHEDULE_FIELD_TOTALS and SET_SCHEDULE_APPEARANCE.
//
// EVERY NAME IS RESOLVED BEFORE ANYTHING IS WRITTEN. A schedule with one
// unknown, ambiguous or repeated name is left exactly as it was and named in
// `notPresent`; the others still go on.
//
// SetFieldOrder IS GIVEN EVERY FIELD, ONCE. The reference says the list sets
// the order of the schedule's fields; whether Revit would accept a list that
// left some out is NOT SAID, so this never asks - it always hands over the
// whole order, hidden fields included, each exactly once. An ASSUMPTION it
// does not need to rely on.
//
// READ FIRST, WRITE, READ BACK. `appliedOrder` is GetFieldOrder read after the
// write, never the list that was sent; a schedule whose read back differs
// from what was asked is named in `notKept`. A schedule already in that order
// is counted in `alreadyInOrder` and not written.

// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule - the
// read-through every schedule fragment in this library keeps: a schedule is a
// VIEW, a view cannot be selected, and the placement carries the id of the
// schedule it draws.
Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;

    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;

    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

var reordered = 0;
var alreadyInOrder = 0;
var appliedOrder = "";
var notPresent = new List<string>();
var notKept = new List<string>();
string refused = null;

var handed = 0;
var schedulesSeen = 0;
var scheduleIdsSeen = new HashSet<ElementId>();
var orderParts = new List<string>();

// ---- what is asked, read once -------------------------------------------
var asked = string.Join(" ", (order ?? "").Trim().Split(new[] { ' ', (char)9 },
    StringSplitOptions.RemoveEmptyEntries));
var askedLower = asked.ToLowerInvariant();

string moveName = null;            // "move X ..." - the column moved
string moveAnchor = null;          // before/after this column
var moveAfter = false;
int moveTo = 0;                    // 1-based place; -1 the end
var listed = new List<string>();   // "A, B, C"

if (asked.Length == 0)
{
    refused = "no order was given, so NOTHING WAS MOVED. Name the columns in the order wanted - "
        + "'Mark, Size, Length' - or say 'move Length to 2', 'move Length before Size'";
}
else if (askedLower.StartsWith("move "))
{
    var rest = asked.Substring(5).Trim();
    var restLower = rest.ToLowerInvariant();
    int at;
    if ((at = restLower.LastIndexOf(" before ")) > 0)
    {
        moveName = rest.Substring(0, at).Trim();
        moveAnchor = rest.Substring(at + 8).Trim();
    }
    else if ((at = restLower.LastIndexOf(" after ")) > 0)
    {
        moveName = rest.Substring(0, at).Trim();
        moveAnchor = rest.Substring(at + 7).Trim();
        moveAfter = true;
    }
    else if ((at = restLower.LastIndexOf(" to ")) > 0)
    {
        moveName = rest.Substring(0, at).Trim();
        var where = restLower.Substring(at + 4).Trim();
        if (where.StartsWith("the ")) where = where.Substring(4).Trim();
        if (where.StartsWith("place ")) where = where.Substring(6).Trim();
        if (where.StartsWith("position ")) where = where.Substring(9).Trim();
        int number;
        if (where == "front" || where == "start" || where == "first" || where == "beginning") moveTo = 1;
        else if (where == "end" || where == "last" || where == "back") moveTo = -1;
        else if (int.TryParse(where, System.Globalization.NumberStyles.None,
                     System.Globalization.CultureInfo.InvariantCulture, out number) && number >= 1)
            moveTo = number;
    }

    // "the Length column" is said as often as "Length".
    Func<string, string> bare = text =>
    {
        var t = (text ?? "").Trim();
        if (t.StartsWith("the ", StringComparison.OrdinalIgnoreCase)) t = t.Substring(4).Trim();
        if (t.EndsWith(" column", StringComparison.OrdinalIgnoreCase)) t = t.Substring(0, t.Length - 7).Trim();
        return t;
    };
    moveName = moveName == null ? null : bare(moveName);
    moveAnchor = moveAnchor == null ? null : bare(moveAnchor);

    if (string.IsNullOrEmpty(moveName) || (moveAnchor == null && moveTo == 0)
        || (moveAnchor != null && moveAnchor.Length == 0))
    {
        refused = "'" + asked + "' is not a move this understands, so NOTHING WAS MOVED. Say 'move "
            + "Length to 2' (1 is the first field in the Fields tab), 'move Length to the front', "
            + "'move Length to the end', 'move Length before Size' or 'move Length after Size'";
    }
}
else
{
    foreach (var part in asked.Split(','))
    {
        var name = part.Trim();
        if (name.Length > 0) listed.Add(name);
    }
    if (listed.Count == 0)
        refused = "'" + asked + "' names no column, so NOTHING WAS MOVED. Name the columns with "
            + "commas between - 'Mark, Size, Length'";
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
    IList<ScheduleFieldId> before;
    try
    {
        definition = schedule.Definition;
        before = definition.GetFieldOrder();
    }
    catch (Exception) { continue; }

    // Every column answering to a name, by field name or printed heading.
    Func<string, List<ScheduleFieldId>> answering = wanted =>
    {
        var found = new List<ScheduleFieldId>();
        foreach (var id in before)
        {
            var field = definition.GetField(id);
            if (field == null) continue;
            if (string.Equals(field.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase))
                found.Add(id);
        }
        return found;
    };
    var problems = new List<string>();
    Func<string, ScheduleFieldId> one = wanted =>
    {
        var found = answering(wanted);
        if (found.Count == 1) return found[0];
        problems.Add(found.Count == 0
            ? string.Format("'{0}' is not a column in it", wanted)
            : string.Format("{0} columns answer to '{1}', by field name or heading", found.Count, wanted));
        return null;
    };

    // ---- the whole new order, worked out before anything is written -----
    var next = new List<ScheduleFieldId>();
    if (moveName != null)
    {
        var moved = one(moveName);
        ScheduleFieldId anchor = moveAnchor == null ? null : one(moveAnchor);
        if ((object)moved != null && (object)anchor != null && moved == anchor)
            problems.Add(string.Format("'{0}' cannot be moved before or after itself", moveName));
        if (problems.Count == 0)
        {
            next.AddRange(before.Where(id => id != moved));
            int index;
            if ((object)anchor != null) index = next.IndexOf(anchor) + (moveAfter ? 1 : 0);
            else if (moveTo == -1) index = next.Count;
            else if (moveTo > before.Count)
            {
                problems.Add(string.Format("it has {0} field(s) in its Fields tab, so place {1} does not "
                    + "exist; give 1 to {0}", before.Count, moveTo));
                index = -1;
            }
            else index = moveTo - 1;
            if (index >= 0) next.Insert(index, moved);
        }
    }
    else
    {
        var first = new List<ScheduleFieldId>();
        foreach (var name in listed)
        {
            var id = one(name);
            if ((object)id == null) continue;
            if (first.Contains(id)) { problems.Add(string.Format("'{0}' is named twice", name)); continue; }
            first.Add(id);
        }
        if (problems.Count == 0)
        {
            next.AddRange(first);
            next.AddRange(before.Where(id => !first.Contains(id)));
        }
    }

    Func<IList<ScheduleFieldId>, string> describe = ids =>
    {
        var words = new List<string>();
        for (var i = 0; i < ids.Count; i++)
        {
            var field = definition.GetField(ids[i]);
            words.Add((i + 1) + " " + (field == null ? "(unknown)" : field.GetName())
                + (field != null && field.IsHidden ? " (hidden)" : ""));
        }
        return string.Join(", ", words);
    };

    if (problems.Count > 0)
    {
        notPresent.Add(string.Format("'{0}' - {1}. NOTHING WAS MOVED on it", schedule.Name,
            string.Join("; ", problems)));
        continue;
    }
    if (next.Count != before.Count)
    {
        notKept.Add(string.Format("'{0}' - the new order held {1} field(s) where the schedule has {2}; "
            + "nothing was written", schedule.Name, next.Count, before.Count));
        continue;
    }
    if (next.SequenceEqual(before))
    {
        alreadyInOrder++;
        orderParts.Add(string.Format("'{0}' already in that order: {1}", schedule.Name, describe(before)));
        continue;
    }

    try { definition.SetFieldOrder(next); }
    catch (Exception failure)
    {
        notKept.Add(string.Format("'{0}' - Revit refused the new order: {1}. Nothing changed on it",
            schedule.Name, (failure.InnerException ?? failure).Message.TrimEnd('.')));
        continue;
    }

    // Read back from the definition - what the Fields tab will show.
    var after = definition.GetFieldOrder();
    if (!after.SequenceEqual(next))
    {
        notKept.Add(string.Format("'{0}' - asked for {1}; a read back finds {2}", schedule.Name,
            describe(next), describe(after)));
        continue;
    }
    reordered++;
    orderParts.Add(string.Format("'{0}': {1}", schedule.Name, describe(after)));
}

appliedOrder = string.Join("; ", orderParts);

// Decided after the loop, and safe there: an element that is not a schedule
// `continue`s before SetFieldOrder is reached.
if (refused == null && handed == 0)
{
    refused = "no schedule was handed in - nothing was found by that name, and nothing is selected - "
        + "so NOTHING WAS MOVED. Check the schedule's name, or click it on a sheet";
}
else if (refused == null && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS MOVED. A schedule is a "
        + "VIEW and cannot be selected in the model; what CAN be selected is a schedule PLACED ON A "
        + "SHEET, which this reads through to the schedule behind it. Click the schedule on the sheet, "
        + "or find it by name", handed);
}
