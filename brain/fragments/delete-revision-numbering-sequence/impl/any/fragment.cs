// NOT STANDALONE. Assumes `doc`, `name` and `confirm` are in scope, and
// leaves `deleted`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts it back.
//
// THE DELETE BUTTON IN CUSTOMIZE NUMBERING > NUMBERING.
//
// DELETING IS ARTICLE 7, SO IT NEEDS THE OWNER'S YES FOR THIS SEQUENCE.
// `confirm` must repeat the sequence's name. A bare "yes" is refused: a yes
// given for one sequence must never delete another.
//
// A SEQUENCE A REVISION USES IS REFUSED, NAMING THE REVISION. Deleting it
// would leave those revisions numbered by something else, or by nothing -
// which Revit decides, not this. Move each revision to another sequence first
// (EDIT_REVISION, numbering=<another>).
//
// REVIT'S OWN REASON is reported when Revit refuses - it may keep the last
// sequence of a type - and the outcome is established by looking for the
// sequence afterwards, never by trusting the call.
//
// 2022 ON ONLY - earlier releases have no named sequences; see the contract.

var deleted = false;
var findings = "";
var refused = "";

{
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');

    var existing = new List<RevisionNumberingSequence>();
    foreach (var id in RevisionNumberingSequence.GetAllRevisionNumberingSequences(doc))
    {
        var sequence = doc.GetElement(id) as RevisionNumberingSequence;
        if (sequence != null) existing.Add(sequence);
    }
    Func<string> remaining = () =>
    {
        var names = new List<string>();
        foreach (var id in RevisionNumberingSequence.GetAllRevisionNumberingSequences(doc))
        {
            var sequence = doc.GetElement(id) as RevisionNumberingSequence;
            if (sequence != null)
                names.Add("'" + sequence.SequenceName + "' (" + sequence.NumberType.ToString().ToLowerInvariant() + ")");
        }
        return string.Join(", ", names.ToArray());
    };

    var wantedName = (name ?? "").Trim();
    var target = existing.FirstOrDefault(s => string.Equals(s.SequenceName, wantedName, StringComparison.OrdinalIgnoreCase));

    if (target == null)
        refused = "No numbering sequence called '" + wantedName + "' in " + doc.Title + " - it has "
                + remaining() + ". Nothing was deleted.";
    else
    {
        var targetId = target.Id;
        var targetName = target.SequenceName;

        var users = new List<string>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision != null && revision.RevisionNumberingSequenceId == targetId)
                users.Add("Seq. " + revision.SequenceNumber + " " + quoted(revision.Description)
                          + (revision.Issued ? " (ISSUED)" : ""));
        }

        var said = (confirm ?? "").Trim();
        if (!string.Equals(said, targetName, StringComparison.OrdinalIgnoreCase))
            refused = "Deleting the numbering sequence '" + targetName + "' needs the owner's yes for "
                    + "THIS sequence: pass confirm=" + targetName + " only after he has agreed. "
                    + (said.Length == 0 ? "No confirm was given" : "confirm='" + said + "' does not name it")
                    + ". Nothing was deleted." + (users.Count > 0
                        ? " It is also used by " + string.Join(", ", users.ToArray()) + ", which would refuse it anyway." : "");
        else if (users.Count > 0)
            refused = "'" + targetName + "' is used by " + string.Join(", ", users.ToArray())
                    + ". Move each to another sequence first (EDIT_REVISION, numbering=<another>). "
                    + "Nothing was deleted.";
        else
        {
            var revitReason = "";
            try { doc.Delete(targetId); }
            catch (Exception failure) { revitReason = revitSaid(failure); }

            deleted = doc.GetElement(targetId) == null;
            if (!deleted)
                refused = "Revit did not delete '" + targetName + "'. "
                        + (revitReason.Length > 0 ? "Revit said: " + revitReason : "Revit kept it without giving a reason")
                        + ". Nothing was deleted.";
        }

        findings = "Numbering sequences now: " + remaining() + ".";
    }
}
