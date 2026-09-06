// NOT STANDALONE. Assumes `doc` and `sequenceNumber` are in scope, and leaves
// `deleted`, `renumbered` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts back both the
// revision and the numbering it disturbed.
//
// DELETING ONE RENUMBERS EVERY LATER REVISION.
//
// Delete 3 of 5 and the old 4 and 5 become 3 and 4. Every email and every
// printed drawing that said "revision 4" now names a different issue, and
// nothing in the model records the shift. So the numbering AFTER the delete is
// read back and handed over: it is the only way anybody can see what moved.
//
// A CLOUD STILL USING IT IS A REFUSAL, NOT A CRASH.
//
// And usually the right answer - the cloud is on a sheet somebody has issued.
// Some releases refuse without throwing, so the outcome is established by
// looking for the revision afterwards rather than by trusting the call.

bool deleted = false;
var renumbered = new Dictionary<int, string>();
string refused = "";

Revision target = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Revision)))
{
    var candidate = element as Revision;
    if (candidate == null) continue;
    if (candidate.SequenceNumber == sequenceNumber) { target = candidate; break; }
}

if (target == null)
    refused = "No revision with sequence number " + sequenceNumber + ".";
else
{
    var targetId = target.Id;

    try { doc.Delete(targetId); }
    catch { }

    // Established by looking, not by the call returning: some releases refuse
    // a revision a cloud still uses without throwing at all.
    deleted = doc.GetElement(targetId) == null;

    if (!deleted)
        refused = "Revit would not delete revision " + sequenceNumber +
                  " - a revision cloud still uses it, which is usually the right answer.";
    else
    {
        // What every revision is numbered NOW. The numbers moved and nothing
        // else records it.
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Revision)))
        {
            var revision = element as Revision;
            if (revision == null) continue;
            string description = "";
            try { description = revision.Description ?? ""; } catch { }
            renumbered[revision.SequenceNumber] = description;
        }
    }
}
