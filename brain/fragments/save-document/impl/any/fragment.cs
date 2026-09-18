// NOT STANDALONE. Assumes `doc` is in scope; leaves `saved`, `refused` and
// `findings`.
//
// OPENS NO TRANSACTION AND MUST NOT BE INSIDE ONE.
//
// PUBLISH RISK, AND NOT UNDOABLE. A save commits whatever is in the model right
// now, including the change somebody was about to undo. Phase 0 and Phase 1
// refuse PUBLISH unconditionally.
//
// A LOCAL SAVE IS NOT A SYNC. On a workshared model this writes the local file
// and reaches nobody. That is the confusion worth preventing, so it is said in
// the findings every time.
//
// IT NEVER CHOOSES A FILENAME. SaveAs can overwrite another model, and inventing
// a path is exactly how that happens.

var saved = false;
string refused = null;
var findings = new List<string>();

if (doc == null)
{
    refused = "No document was given";
}
else if (doc.IsReadOnly)
{
    refused = string.Format("'{0}' is open read-only and cannot be saved",
        doc.Title);
}
else if (string.IsNullOrEmpty(doc.PathName))
{
    refused = string.Format(
        "'{0}' has never been saved, so it has no file to save TO. Nothing here "
        + "invents a filename - SaveAs can overwrite another model and that is "
        + "not a decision a fragment makes", doc.Title);
}
else
{
    try
    {
        doc.Save(new SaveOptions());
        saved = true;

        findings.Add(string.Format(
            "'{0}' saved to {1}. This cannot be undone{2}",
            doc.Title, doc.PathName,
            doc.IsWorkshared
                ? ". IT IS WORKSHARED AND NOTHING WENT TO CENTRAL - this wrote "
                  + "the local file only, and nobody else on the job can see it. "
                  + "SYNC_WITH_CENTRAL is the other job"
                : ""));
    }
    catch (Exception ex)
    {
        refused = string.Format("Revit refused the save: {0}", ex.Message);
    }
}

if (refused != null) findings.Add(refused);
