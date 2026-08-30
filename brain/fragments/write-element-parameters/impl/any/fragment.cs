// NOT STANDALONE. Assumes `doc`, `elements`, `parameterName` and `value` are in
// scope, and leaves `written`, `snapped`, `unverified`, `readOnly`, `absent` and
// `refused` behind.
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
//
// ===========================================================================
// A TRUE RETURN VALUE IS NOT EVIDENCE THAT THE VALUE WAS WRITTEN.
// ===========================================================================
//
// This is the whole reason this fragment is at version 2, and it is the same
// defect that was found in Heron's own move path: reporting the number that was
// ASKED FOR as the number that HAPPENED.
//
// Revit accepts a size and then SNAPS it to the nearest size its type allows.
// The set returns TRUE. Nothing throws. Ask a pipe for 77 and it becomes 80,
// and a fragment that trusts the return value reports a clean success while the
// model holds a different number. Version 1 of this file did exactly that - and
// its own proof case asked a human to check that "the number shown in Revit
// MATCHES WHAT WAS TYPED", which the code had no way of knowing.
//
// THE CHECK USES THE SNAP'S OWN TIMING, and that is why it needs no string
// parsing, no culture and no display-rounding tolerance:
//
//   * immediately after the set, the parameter still reports WHAT WAS ASKED FOR
//   * the snap happens at REGENERATION, not at the set
//
// So read the internal double straight after the set, regenerate once, read it
// again, and compare. Two numbers from the same API in the same units. The
// alternative - comparing `AsValueString()` against the user's text - has to
// cope with "300" against "300.0 mm", with thousands separators, and with a
// display rounded to the project's decimal places, and would raise false alarms
// on all three.
//
// ONE regeneration for the whole batch, not one per element. The snap is
// applied to everything pending, so a single regenerate settles all of them,
// and regenerating inside the loop turns a 400-element write into 400 model
// updates.
//
// `unverified` is separated from `snapped` and is NOT counted as written. It
// means the check could not be run - the parameter vanished between the write
// and the read-back, which should not happen and is reported rather than
// assumed away. Silence about a check that did not run is how an unproven
// claim becomes a believed one.

var written = 0;
var snapped = new List<ElementId>();
var unverified = new List<ElementId>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var refused = new List<ElementId>();

// Integer parameters report through AsInteger and read 0 from AsDouble, so the
// read has to branch. It is written once and used on both sides of the
// regeneration, so the two comparisons cannot drift apart.
Func<Parameter, double> numericValue = p =>
    p.StorageType == StorageType.Integer ? (double)p.AsInteger() : p.AsDouble();

// Accepted by the set, and therefore NOT YET BELIEVED. Parallel lists rather
// than a carried Parameter, because the parameter is looked up again after the
// regeneration rather than held across it.
var pending = new List<Element>();
var asked = new List<double>();

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
    var numeric = false;

    switch (parameter.StorageType)
    {
        case StorageType.String:
            ok = parameter.Set(value);
            break;

        case StorageType.Integer:
        case StorageType.Double:
            // Units belong to the document, not to this code. See the note above.
            ok = parameter.SetValueString(value);
            numeric = true;
            break;

        default:
            // ElementId, and None. Refused by name rather than attempted.
            refused.Add(element.Id);
            continue;
    }

    if (!ok)
    {
        refused.Add(element.Id);
        continue;
    }

    if (!numeric)
    {
        // Text does not go through the size table, so there is nothing for a
        // regeneration to change. Checked here rather than deferred.
        if (parameter.AsString() == value) written++;
        else snapped.Add(element.Id);
        continue;
    }

    // Read BEFORE the regeneration: this is what Revit parsed the text as, and
    // it is the only moment it is available.
    pending.Add(element);
    asked.Add(numericValue(parameter));
}

if (pending.Count > 0)
{
    doc.Regenerate();

    for (var i = 0; i < pending.Count; i++)
    {
        var parameter = pending[i].LookupParameter(parameterName);

        if (parameter == null)
        {
            unverified.Add(pending[i].Id);
            continue;
        }

        var stored = numericValue(parameter);
        var want = asked[i];

        // Both sides came from the same API in the same internal units, so the
        // only tolerance needed is for floating point itself - not for anything
        // Revit might reasonably have done to the number.
        if (Math.Abs(stored - want) <= Math.Abs(want) * 1e-9 + 1e-12) written++;
        else snapped.Add(pending[i].Id);
    }
}
