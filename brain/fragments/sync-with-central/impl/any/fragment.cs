// NOT STANDALONE. Assumes `doc`, `comment` and `relinquish` are in scope; leaves
// `synchronised`, `refused` and `findings`.
//
// OPENS NO TRANSACTION AND MUST NOT BE INSIDE ONE. Revit refuses a sync within a
// transaction - the opposite of the usual shape here, and worth stating because
// every other write fragment in this library assumes an open one.
//
// PUBLISH RISK. A sync reaches everybody on the job, cannot be undone, and can
// take minutes. Phase 0 and Phase 1 refuse PUBLISH unconditionally, so this is
// deliberately unreachable until the register says otherwise.
//
// RELINQUISH OR SAY SO. Syncing without relinquishing leaves elements checked
// out under somebody who has gone home, and the next person gets a refusal they
// cannot explain.

var synchronised = false;
string refused = null;
var findings = new List<string>();

if (doc == null)
{
    refused = "No document was given";
}
else if (!doc.IsWorkshared)
{
    refused = string.Format(
        "'{0}' is not workshared, so there is no central model to sync with. "
        + "SAVE_DOCUMENT is what an ordinary file wants", doc.Title);
}
else if (doc.IsReadOnly)
{
    refused = string.Format("'{0}' is open read-only and cannot be synced",
        doc.Title);
}
else
{
    try
    {
        var transact = new TransactWithCentralOptions();

        var options = new SynchronizeWithCentralOptions();
        options.Comment = string.IsNullOrEmpty(comment)
            ? "Synchronised by Heron" : comment;
        options.SaveLocalAfter = true;

        if (relinquish)
        {
            var giveUp = new RelinquishOptions(true);
            options.SetRelinquishOptions(giveUp);
        }

        doc.SynchronizeWithCentral(transact, options);
        synchronised = true;

        findings.Add(string.Format(
            "'{0}' synchronised with central. {1} The local file was saved "
            + "afterwards. THIS REACHED EVERYBODY on the job and cannot be undone",
            doc.Title,
            relinquish
                ? "Everything this user owned was relinquished."
                : "NOTHING WAS RELINQUISHED - elements stay checked out under "
                  + "this user, and the next person to touch them will be "
                  + "refused."));
    }
    catch (Exception ex)
    {
        refused = string.Format("Revit refused the sync: {0}", ex.Message);
    }
}

if (refused != null) findings.Add(refused);
