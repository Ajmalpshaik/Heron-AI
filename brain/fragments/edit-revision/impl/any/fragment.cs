// NOT STANDALONE. Assumes `doc`, `sequenceNumber`, `description`,
// `revisionDate`, `issuedBy`, `issuedTo` and `issueState` are in scope, and
// leaves `changed`, `nowIssued` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE SEQUENCE NUMBER IS THE IDENTITY, NOT THE DESCRIPTION.
//
// Two issues called "For Construction" is normal on a job, and matching on the
// description would edit whichever came first - silently, and on a document
// that is about to go out.
//
// ISSUING IS THE FIELD THAT CHANGES WHAT CAN BE DONE NEXT.
//
// Once a revision is issued Revit refuses most edits to it and to its clouds.
// It is reported on its own line for that reason: everything else here is a
// correction, and this one is a decision.
//
// EMPTY MEANS LEAVE ALONE, AND EVERY FIELD IS READ BACK.
//
// A revision that refuses an edit returns from the call with its old value
// still in place, so what is reported is what the model now holds.

var changed = new List<string>();
bool nowIssued = false;
string refused = "";

Revision revision = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Revision)))
{
    var candidate = element as Revision;
    if (candidate == null) continue;
    if (candidate.SequenceNumber == sequenceNumber) { revision = candidate; break; }
}

if (revision == null)
    refused = "No revision with sequence number " + sequenceNumber +
              ". LIST_REVISIONS is where that number comes from.";
else
{
    try { nowIssued = revision.Issued; } catch { }

    Action<string, string, Func<string>, Action<string>> apply =
        (label, wanted, read, write) =>
        {
            if (string.IsNullOrEmpty(wanted)) return;
            try { write(wanted); } catch { return; }
            string now = "";
            try { now = read() ?? ""; } catch { }
            if (now == wanted) changed.Add(label);
        };

    apply("description", description, () => revision.Description, v => revision.Description = v);
    apply("date", revisionDate, () => revision.RevisionDate, v => revision.RevisionDate = v);
    apply("issued by", issuedBy, () => revision.IssuedBy, v => revision.IssuedBy = v);
    apply("issued to", issuedTo, () => revision.IssuedTo, v => revision.IssuedTo = v);

    string wantedState = (issueState ?? "").Trim().ToLowerInvariant();
    if (wantedState == "issue" || wantedState == "unissue")
    {
        bool wantIssued = wantedState == "issue";
        try { revision.Issued = wantIssued; } catch { }

        bool actually = false;
        try { actually = revision.Issued; } catch { }

        if (actually == wantIssued) changed.Add(wantIssued ? "issued" : "un-issued");
        nowIssued = actually;
    }

    if (changed.Count == 0 && refused.Length == 0)
        refused = "Nothing changed on revision " + sequenceNumber +
                  (nowIssued ? " - it is ISSUED, which is what usually refuses an edit." : ".");
}
