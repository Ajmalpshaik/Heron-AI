// NOT STANDALONE. Assumes `doc`, `elements` and `keyNames` are in scope;
// leaves `added`, `addedKeys`, `alreadyThere` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16). One undo
// takes every key added here back out.
//
// A KEY SCHEDULE'S ROWS ARE ITS KEYS. CREATE_KEY_SCHEDULE makes the table and
// no rows, so an element's key drop-down is empty until somebody presses
// Insert Data Row once per key and types each Key Name. This is that, for a
// list of names, in one call.
//
// WHETHER INSERTING A ROW IN A KEY SCHEDULE'S BODY ADDS A KEY IS NOT SETTLED BY
// THE REVIT REFERENCE. `TableSectionData.InsertRow` and `CanInsertRow` exist,
// unchanged, on every release 2020 to 2027 (tools/api-surface --members,
// 2026-11-01), and the reference says what they do to a TABLE, not that a key
// element is made behind the row. So nothing here takes that on trust:
//
//   - the keys are counted BEFORE and AFTER each row, as the elements the
//     schedule view owns (FilteredElementCollector on the schedule's id) - an
//     ASSUMPTION about where a key schedule's keys are found, which the proof
//     settles;
//   - exactly ONE new element per row is required. None, or more than one,
//     throws, so the add-in rolls the WHOLE call back and nothing is kept -
//     "Revit added no key" is said, and no half-filled table is left;
//   - the new key's Key Name is written (the REF_TABLE_ELEM_NAME parameter,
//     else Element.Name) and READ BACK; a name that does not read back throws
//     the same way.
//
// WHERE THE ROW GOES. CanInsertRow is asked first, at the end of the body and
// then at the places a body with only its heading row could take one; the
// first place Revit says yes is used and named in the answer. A schedule with
// no place Revit will take a row is refused before anything is inserted.
//
// A KEY NAME ALREADY IN THE SCHEDULE IS LEFT ALONE and named in
// `alreadyThere` - two keys of one name would be two entries nobody can tell
// apart in an element's drop-down (an ASSUMPTION that Revit would refuse it is
// not relied on: the names are compared before inserting). The same name
// twice in one request is one key.
//
// ONLY A KEY SCHEDULE. A schedule of elements has no rows to insert - its rows
// are the model - so it is refused by name and not touched.

Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;

    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;

    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

var added = 0;
var addedKeys = "";
var alreadyThere = new List<string>();
string refused = null;

{
    var handed = 0;
    var schedulesSeen = new HashSet<ElementId>();
    var notKey = new List<string>();
    var noRow = new List<string>();
    var addedParts = new List<string>();

    // The names asked for, once each, in the order given.
    var names = new List<string>();
    var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (keyNames != null)
    {
        foreach (var raw in keyNames)
        {
            var one = (raw ?? "").Trim();
            if (one.Length == 0) continue;
            if (seenNames.Add(one)) names.Add(one);
        }
    }

    Func<ViewSchedule, Dictionary<ElementId, string>> keysOf = schedule =>
    {
        var found = new Dictionary<ElementId, string>();
        foreach (var key in new FilteredElementCollector(doc, schedule.Id).WhereElementIsNotElementType())
        {
            if (key == null) continue;
            string called;
            try { called = key.Name ?? ""; } catch (Exception) { called = ""; }
            found[key.Id] = called;
        }
        return found;
    };

    Func<Element, string> keyNameOf = key =>
    {
        try
        {
            var p = key.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME);
            if (p != null && p.HasValue) return p.AsString() ?? "";
        }
        catch (Exception) { }
        try { return key.Name ?? ""; } catch (Exception) { return ""; }
    };

    if (names.Count == 0)
    {
        refused = "no key name was given, so NO KEY WAS ADDED. Name the keys - 'Finish A, Finish B'";
    }

    foreach (var element in elements ?? new List<Element>())
    {
        if (refused != null) break;
        if (element == null) continue;
        handed++;

        var schedule = scheduleBehind(element);
        if (schedule == null) continue;
        if (!schedulesSeen.Add(schedule.Id)) continue;

        var isKey = false;
        try { isKey = schedule.Definition.IsKeySchedule; } catch (Exception) { isKey = false; }
        if (!isKey)
        {
            notKey.Add("'" + schedule.Name + "' is a schedule of elements, not a key schedule - its rows are "
                + "the model and none can be inserted; it was not touched");
            continue;
        }

        var before = keysOf(schedule);
        var namesThere = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in before.Keys)
        {
            var e = doc.GetElement(key);
            if (e != null) namesThere.Add(keyNameOf(e).Trim());
        }

        var toAdd = new List<string>();
        foreach (var one in names)
        {
            if (namesThere.Contains(one))
                alreadyThere.Add("'" + one + "' on '" + schedule.Name + "' - a key of that name is already there, left as it is");
            else toAdd.Add(one);
        }
        if (toAdd.Count == 0) continue;

        // Where Revit will take a row - asked before anything is inserted.
        Func<int?> placeForRow = () =>
        {
            TableSectionData body;
            try { body = schedule.GetTableData().GetSectionData(SectionType.Body); }
            catch (Exception) { return null; }
            if (body == null) return null;
            var candidates = new List<int> { body.LastRowNumber + 1, body.LastRowNumber, body.FirstRowNumber + 1, body.FirstRowNumber };
            foreach (var at in candidates.Distinct())
            {
                if (at < 0) continue;
                try { if (body.CanInsertRow(at)) return at; } catch (Exception) { }
            }
            return null;
        };

        if (placeForRow() == null)
        {
            noRow.Add("'" + schedule.Name + "' - Revit says no row can be inserted in its body (CanInsertRow), "
                + "so no key was added to it");
            continue;
        }

        var known = new HashSet<ElementId>(before.Keys);
        foreach (var one in toAdd)
        {
            var at = placeForRow();
            if (at == null)
                throw new InvalidOperationException("Revit stopped taking rows in '" + schedule.Name + "' after "
                    + addedParts.Count + " key(s). Nothing is kept - the whole change is rolled back.");

            var body = schedule.GetTableData().GetSectionData(SectionType.Body);
            try { body.InsertRow(at.Value); }
            catch (Exception failure)
            {
                throw new InvalidOperationException("Revit would not insert a row in '" + schedule.Name + "' at row "
                    + at.Value + ": " + (failure.InnerException ?? failure).Message.TrimEnd('.')
                    + ". Nothing is kept - the whole change is rolled back.");
            }
            doc.Regenerate();

            var after = keysOf(schedule);
            var fresh = after.Keys.Where(id => !known.Contains(id)).ToList();
            if (fresh.Count != 1)
                throw new InvalidOperationException("Revit added " + (fresh.Count == 0 ? "no key" : fresh.Count + " elements")
                    + " for one row inserted in '" + schedule.Name + "' - so inserting a row is not how a key is added "
                    + "here, or the keys are not where this looks. Nothing is kept - the whole change is rolled back.");

            var newKey = doc.GetElement(fresh[0]);
            known.Add(fresh[0]);
            var written = false;
            try
            {
                var p = newKey.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME);
                if (p != null && !p.IsReadOnly) written = p.Set(one);
            }
            catch (Exception) { written = false; }
            if (!written)
            {
                try { newKey.Name = one; written = true; }
                catch (Exception failure)
                {
                    throw new InvalidOperationException("the new key in '" + schedule.Name + "' could not be named '" + one
                        + "': " + (failure.InnerException ?? failure).Message.TrimEnd('.')
                        + ". Nothing is kept - the whole change is rolled back.");
                }
            }

            var back = keyNameOf(doc.GetElement(fresh[0])).Trim();
            if (!string.Equals(back, one, StringComparison.Ordinal))
                throw new InvalidOperationException("the new key in '" + schedule.Name + "' reads back as '" + back
                    + "', not '" + one + "'. Nothing is kept - the whole change is rolled back.");

            added++;
            addedParts.Add("'" + back + "' on '" + schedule.Name + "' (row " + at.Value + ")");
        }
    }

    addedKeys = string.Join("; ", addedParts.ToArray());

    if (refused == null && handed == 0)
        refused = "no schedule was handed in - nothing was found by that name, and nothing is selected - "
            + "so NO KEY WAS ADDED. Name the key schedule, or click it on a sheet";
    else if (refused == null && schedulesSeen.Count == 0)
        refused = "not one of the " + handed + " element(s) handed in is a schedule, so NO KEY WAS ADDED. "
            + "A schedule is a VIEW and cannot be selected in the model; what CAN be selected is a schedule "
            + "PLACED ON A SHEET, which this reads through. Click it on the sheet, or find it by name";
    else if (refused == null && (notKey.Count > 0 || noRow.Count > 0))
    {
        var why = notKey.Concat(noRow).ToArray();
        refused = (added == 0 ? "NO KEY WAS ADDED: " : "added to the others, but: ") + string.Join("; ", why);
    }
}
