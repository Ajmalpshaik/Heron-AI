// NOT STANDALONE. Assumes `doc`, `scheduleName`, `fieldNames` and
// `sortByField` are in scope; leaves `sheetList`, `fieldsAdded` and `refused`
// behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SHEET LIST IS ITS OWN API CALL. An ordinary schedule schedules a CATEGORY
// of model elements; sheets are not model elements, so the category route
// cannot produce this at all.
//
// THE FIELDS ARE SHEET PARAMETERS - Sheet Number, Sheet Name, Current Revision,
// Sheet Issue Date. A name the list cannot carry is REPORTED with the real
// names listed. A silently short index looks finished on the printed sheet and
// nobody reading it can tell a column was asked for.

Element sheetList = null;
var fieldsAdded = new List<string>();
var refused = new List<string>();

ViewSchedule schedule = null;
try { schedule = ViewSchedule.CreateSheetList(doc); }
catch (Exception ex) { refused.Add(string.Format("could not create a sheet list - {0}", ex.Message)); }

if (schedule != null)
{
    sheetList = schedule;

    // A name already in use leaves Revit's generated name in place rather than
    // failing the whole job - the index still exists and can be renamed.
    if (!string.IsNullOrEmpty(scheduleName))
    {
        try { schedule.Name = scheduleName; }
        catch
        {
            refused.Add(string.Format("'{0}' is already the name of another view - the sheet list was "
                + "created as '{1}' instead", scheduleName, schedule.Name));
        }
    }

    var definition = schedule.Definition;
    var schedulable = definition.GetSchedulableFields();

    var available = new List<string>();
    foreach (var field in schedulable)
    {
        try { available.Add(field.GetName(doc)); }
        catch { }
    }

    ScheduleField sortField = null;

    foreach (var wanted in fieldNames)
    {
        SchedulableField match = null;
        foreach (var field in schedulable)
        {
            string name = null;
            try { name = field.GetName(doc); }
            catch { continue; }
            if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)) { match = field; break; }
        }

        if (match == null)
        {
            refused.Add(string.Format("'{0}' is not a field a sheet list can carry. It can carry: {1}",
                wanted, string.Join(", ", available)));
            continue;
        }

        try
        {
            var added = definition.AddField(match);
            fieldsAdded.Add(wanted);
            if (!string.IsNullOrEmpty(sortByField)
                && string.Equals(wanted, sortByField, StringComparison.OrdinalIgnoreCase))
            {
                sortField = added;
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' could not be added - {1}", wanted, ex.Message));
        }
    }

    if (!string.IsNullOrEmpty(sortByField))
    {
        if (sortField == null)
        {
            refused.Add(string.Format("cannot sort by '{0}' - it is not one of the columns. A drawing "
                + "index in creation order is the usual reason one looks wrong", sortByField));
        }
        else
        {
            try
            {
                var sort = new ScheduleSortGroupField(sortField.FieldId);
                sort.SortOrder = ScheduleSortOrder.Ascending;
                definition.AddSortGroupField(sort);
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("could not sort by '{0}' - {1}", sortByField, ex.Message));
            }
        }
    }

    if (fieldsAdded.Count == 0)
    {
        refused.Add("the sheet list was created with NO columns - it will print as an empty box");
    }
}
