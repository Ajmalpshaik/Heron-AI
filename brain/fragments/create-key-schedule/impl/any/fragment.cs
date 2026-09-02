// NOT STANDALONE. Assumes `doc`, `category`, `scheduleName` and
// `keyParameterName` are in scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A KEY SCHEDULE IS NOT A SCHEDULE OF THINGS. CREATE_SCHEDULE lists what is in
// the model; this is a table of DEFINITIONS that elements point at, so one
// choice fills several fields. Nearly the same sentence, completely different
// jobs.
//
// REVIT IS ASKED WHETHER THE CATEGORY CAN HAVE ONE. The create call throws
// rather than explaining, and "an exception occurred" is not an answer somebody
// can act on.
//
// THE KEY PARAMETER IS NAMED HERE. Every key schedule defaults to "Key Name",
// and three of them called that is how nobody can tell which is which in a
// dropdown six months later.
//
// IT CREATES THE TABLE AND NO ROWS. The rows are the definitions somebody types
// or imports, and an empty key schedule offers an empty dropdown - which looks
// broken and is simply new.

var created = ElementId.InvalidElementId;
var findings = new List<string>();

if (category == null)
{
    findings.Add("No category was given, and a key schedule belongs to one");
}
else
{
    var allowed = false;
    try { allowed = ViewSchedule.IsValidCategoryForKeySchedule(category.Id); }
    catch { allowed = false; }

    if (!allowed)
    {
        findings.Add(string.Format("Revit does not allow a key schedule on '{0}'. Asked before trying, "
            + "because the create call throws rather than explaining", category.Name));
    }
    else
    {
        try
        {
            var schedule = ViewSchedule.CreateKeySchedule(doc, category.Id);

            if (schedule == null)
            {
                findings.Add(string.Format("Revit returned no key schedule for '{0}'", category.Name));
            }
            else
            {
                created = schedule.Id;

                var namedOk = true;
                var asked = (scheduleName ?? "").Trim();
                if (asked.Length > 0)
                {
                    try { schedule.Name = asked; }
                    catch { namedOk = false; }
                }

                var keyOk = true;
                var key = (keyParameterName ?? "").Trim();
                if (key.Length > 0)
                {
                    try { schedule.KeyScheduleParameterName = key; }
                    catch { keyOk = false; }
                }

                // READ IT BACK. Revit decides what things end up called.
                var back = doc.GetElement(created) as ViewSchedule;
                var nowCalled = back == null ? asked : back.Name;

                findings.Add(string.Format(
                    "Key schedule '{0}' created for {1}{2}{3}. It has NO ROWS yet - the definitions are "
                    + "what somebody types, and until then the dropdown on an element is empty",
                    nowCalled, category.Name,
                    namedOk ? "" : string.Format(", though the name '{0}' was refused", asked),
                    keyOk ? "" : string.Format(", and the key parameter could not be named '{0}' - it "
                        + "stays whatever Revit called it, which is 'Key Name' on every one of them", key)));
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("The key schedule for '{0}' could not be created: {1}",
                category.Name, ex.Message));
        }
    }
}
