// NOT STANDALONE. Assumes `doc`, `scheduleName`, `fieldNames`, `sortByField`
// and `listKind` are in scope; leaves `sheetList`, `fieldsAdded` and `refused`
// behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SHEET LIST IS ITS OWN API CALL, AND SO IS A VIEW LIST. An ordinary schedule
// schedules a CATEGORY of model elements; sheets and views are not model
// elements, so the category route cannot produce either at all.
//
// `listKind` PICKS WHICH OF THE TWO. Blank or "sheets" is the sheet list this
// fragment has always made - `CreateSheetList`. "views" is Revit's View List -
// `CreateViewList`, a schedule of the project's views. Anything else is REFUSED
// and nothing is created: guessing would make the wrong index and report it
// made.
//
// THE FIELDS ARE THE LIST'S OWN PARAMETERS - Sheet Number, Sheet Name, Current
// Revision for a sheet list; View Name, Sheet Number, Title on Sheet for a view
// list. A name the list cannot carry is REPORTED with the real names listed. A
// silently short index looks finished on the printed sheet and nobody reading
// it can tell a column was asked for.
//
// `sheetList` KEEPS ITS NAME FOR A VIEW LIST TOO. It is the name the contract
// has always provided, and renaming an output breaks whatever reads it.

Element sheetList = null;
var fieldsAdded = new List<string>();
var refused = new List<string>();

var wantedKind = (listKind ?? "").Trim().ToLowerInvariant();
var makeViews = false;
var kindIsKnown = true;

if (wantedKind == "" || wantedKind == "sheets" || wantedKind == "sheet"
    || wantedKind == "sheet list" || wantedKind == "sheetlist")
    makeViews = false;
else if (wantedKind == "views" || wantedKind == "view"
    || wantedKind == "view list" || wantedKind == "viewlist")
    makeViews = true;
else
    kindIsKnown = false;

var kindWord = makeViews ? "view list" : "sheet list";

ViewSchedule schedule = null;
if (!kindIsKnown)
{
    refused.Add(string.Format("'{0}' is not a kind of list this makes, so NOTHING WAS CREATED. "
        + "Use 'sheets' (or leave it blank) for a sheet list, or 'views' for a view list",
        listKind));
}
else
{
    try { schedule = makeViews ? ViewSchedule.CreateViewList(doc) : ViewSchedule.CreateSheetList(doc); }
    catch (Exception ex) { refused.Add(string.Format("could not create a {0} - {1}", kindWord, ex.Message)); }
}

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
            refused.Add(string.Format("'{0}' is already the name of another view - the {1} was "
                + "created as '{2}' instead", scheduleName, kindWord, schedule.Name));
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
            refused.Add(string.Format("'{0}' is not a field a {1} can carry. It can carry: {2}",
                wanted, kindWord, string.Join(", ", available)));
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
            refused.Add(string.Format("cannot sort by '{0}' - it is not one of the columns. An "
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
        refused.Add(string.Format("the {0} was created with NO columns - it will print as an empty box",
            kindWord));
    }
}
