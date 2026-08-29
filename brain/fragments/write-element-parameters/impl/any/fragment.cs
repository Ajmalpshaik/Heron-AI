// NOT STANDALONE. Assumes `elements`, `parameterName` and `value` are in scope,
// and leaves `written`, `readOnly`, `absent` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one. Golden Rule 16 wants one
// user action to be one undo entry, and that is owned by the operation's
// TransactionGroup - a fragment opening its own transaction is how a single
// "set the airflow" turns into forty undo steps.
//
// WHY THE VALUE IS TEXT, AND WHY THIS IS NOT A UNIT CONVERSION.
//
// The obvious version of this fragment takes a double and writes it. It is
// wrong, and it fails quietly, which is worse.
//
// Revit stores lengths in decimal FEET. Ajmal speaks millimetres. So a
// length-shaped fragment would convert with mm / 304.8 (D-20 - plain
// arithmetic, exact on every release). But this fragment is also asked to set
// an AIRFLOW, an AREA, a temperature and a comment, and those are stored in
// their own internal units or in none at all. A blanket /304.8 writes 200 mm
// into an airflow as 0.656 of whatever that parameter measures, reports
// success, and is found weeks later by somebody reading a schedule.
//
// `SetValueString` is the call that already knows. It parses the text in the
// DOCUMENT's own display units - the units the user is looking at while they
// type - and it is the only route here that cannot be wrong about which
// quantity it is writing. It also returns FALSE rather than throwing when the
// text does not parse, which is reported rather than swallowed.
//
// A parameter that stores an ElementId is REFUSED, not guessed at. "Level 2" as
// text cannot be resolved to an id without deciding which Level 2, and D-33
// says Heron asks rather than assumes.

var written = 0;
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var refused = new List<ElementId>();

foreach (var element in elements)
{
    var parameter = element.LookupParameter(parameterName);

    if (parameter == null)
    {
        // ABSENT IS A FAMILY QUESTION, NOT A TYPING ONE. The parameter does not
        // exist on this element at all, so no value would have helped. Kept
        // apart from read-only for that reason - they need different answers.
        absent.Add(element.Id);
        continue;
    }

    if (parameter.IsReadOnly)
    {
        readOnly.Add(element.Id);
        continue;
    }

    var ok = false;
    switch (parameter.StorageType)
    {
        case StorageType.String:
            ok = parameter.Set(value);
            break;

        case StorageType.Integer:
        case StorageType.Double:
            // Units belong to the document, not to this code. See the note above.
            ok = parameter.SetValueString(value);
            break;

        default:
            // ElementId, and None. Refused by name rather than attempted.
            refused.Add(element.Id);
            continue;
    }

    if (ok) written++;
    else refused.Add(element.Id);
}
