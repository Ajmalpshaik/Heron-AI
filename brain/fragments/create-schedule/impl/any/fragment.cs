// NOT STANDALONE. Assumes `doc`, `categoryId`, `fieldNames` and `scheduleName`
// are in scope; leaves `created`, `refused` and `missingFields` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// FIELDS ARE MATCHED BY THE NAME REVIT SHOWS, which is what GetName(doc)
// returns and what appears in the Fields tab. So "Size" finds the field a
// modeller would pick off that list. It does NOT match internal parameter
// names, and that limit is deliberate: the request comes from somebody
// describing a drawing, not from somebody reading the API.
//
// A FIELD THAT DOES NOT EXIST IS REPORTED. Ask for Size on a category with no
// Size and a silently short schedule looks finished, prints, and is missing a
// column - and the person reading it cannot tell a column was ever wanted.
// Every name that found nothing comes back in `missingFields`.
//
// THE SCHEDULE IS STILL MADE when some fields are missing, rather than the
// whole thing being refused. A schedule with three of four columns is useful
// and can have the fourth added; no schedule at all is a re-run. The refusal
// is reserved for the cases where there is nothing to make.
//
// THE SCHEDULABLE FIELD LIST IS ASKED FOR ONCE. It is derived from the category
// and every parameter bound to it, so it is not cheap, and asking per requested
// field would multiply that by the number of columns.

ElementId created = null;
string refused = null;
var missingFields = new List<string>();

var wanted = fieldNames;
var name = (scheduleName ?? "").Trim();

if (categoryId == null || categoryId == ElementId.InvalidElementId)
{
    refused = "a schedule is a schedule OF something - no category was given";
}
else
{
    var schedule = ViewSchedule.CreateSchedule(doc, categoryId);

    if (schedule == null)
    {
        refused = "Revit declined to create a schedule for that category - not every "
                + "category can be scheduled";
    }
    else
    {
        var definition = schedule.Definition;

        // Name first, and only if one was asked for. Revit's own default
        // ("Duct Schedule") is a real name, and writing blank over it leaves a
        // nameless row in the Project Browser.
        if (name.Length > 0) schedule.Name = name;

        // One pass, because building this list walks the category and every
        // parameter bound to it.
        var available = new Dictionary<string, SchedulableField>();

        foreach (var field in definition.GetSchedulableFields())
        {
            if (field == null) continue;

            var fieldName = field.GetName(doc);
            if (string.IsNullOrEmpty(fieldName)) continue;

            // First one wins. Two schedulable fields can share a display name -
            // an instance parameter and a type parameter of the same name - and
            // adding both gives two identical column headings over different
            // numbers, which is worse than picking one.
            if (!available.ContainsKey(fieldName)) available[fieldName] = field;
        }

        if (wanted != null)
        {
            foreach (var requested in wanted)
            {
                if (string.IsNullOrEmpty(requested)) continue;

                var key = requested.Trim();

                if (!available.ContainsKey(key)) { missingFields.Add(key); continue; }

                definition.AddField(available[key]);
            }
        }

        created = schedule.Id;
    }
}
