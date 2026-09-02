// NOT STANDALONE. Assumes `doc`, `description`, `revisionDate` and `issuedBy`
// are in scope; leaves `created`, `refused` and `sequence` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// `Issued` IS NOT SET, AND IT IS A ONE-WAY GATE. Once a revision is issued
// Revit locks its description and date and refuses to put any new cloud on it.
// Creating one already issued makes a revision nobody can cloud, which is the
// opposite of why anybody makes one. Issuing is a decision taken at the end of
// the job, in Revit, by the person signing it off.
//
// THE DATE IS NOT PARSED OR REFORMATTED. RevisionDate is free text in Revit and
// goes straight onto the titleblock, so the project's own convention is the
// only one that matters - "02/09/26" and "2 Sep 2026" are both correct
// somewhere, and a fragment that tidied either into the other would change what
// a signed drawing says.
//
// THE SEQUENCE NUMBER IS READ BACK, NOT ASSUMED. Revit assigns it, and it is
// what the titleblock's revision schedule sorts on - so the caller gets told
// which position this revision actually took rather than being left to count.

ElementId created = null;
string refused = null;
var sequence = 0;

var text = (description ?? "").Trim();

if (text.Length == 0)
{
    // An unlabelled row on a signed drawing. Revit allows it; a drawing office
    // does not.
    refused = "a revision needs a description - it is the line that appears in the "
            + "titleblock's revision schedule, and an empty one is a row nobody can read";
}
else
{
    var revision = Revision.Create(doc);

    if (revision == null)
    {
        refused = "Revit declined to create the revision";
    }
    else
    {
        revision.Description = text;

        // Empty values leave Revit's own defaults rather than writing blank
        // over them - a blank date on a titleblock reads as an error, where a
        // default reads as not yet filled in.
        var when = (revisionDate ?? "").Trim();
        if (when.Length > 0) revision.RevisionDate = when;

        var who = (issuedBy ?? "").Trim();
        if (who.Length > 0) revision.IssuedBy = who;

        created = revision.Id;

        // Where it landed in the sequence, read from Revit rather than counted
        // here: the order is what the revision schedule sorts on.
        var all = Revision.GetAllRevisionIds(doc);
        for (var i = 0; i < all.Count; i++)
        {
            if (all[i] == revision.Id) { sequence = i + 1; break; }
        }
    }
}
