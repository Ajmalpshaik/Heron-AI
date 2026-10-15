// NOT STANDALONE. Assumes `doc`, `action` and `worksetNames` are in scope;
// leaves `changed`, `report`, `refused` and `findings` behind.
//
// OPENS NO TRANSACTION AND MUST NOT BE INSIDE ONE. Revit refuses both calls in
// a transaction - the shape SYNC_WITH_CENTRAL has, not the usual one.
//
// PUBLISH: it reaches the central model and other people, and Undo does not
// take it back. Each workset asked for is READ BACK afterwards.

var changed = 0;
var report = "";
var refused = new List<string>();
var findings = new List<string>();

var mode = (action ?? "").Trim().ToLowerInvariant();

if (doc == null) refused.Add("No document was given.");
else if (!doc.IsWorkshared)
    refused.Add("'" + doc.Title + "' is not workshared, so nobody else owns anything in it. NOTHING WAS CHANGED.");
else if (doc.IsReadOnly)
    refused.Add("'" + doc.Title + "' is open read-only. NOTHING WAS CHANGED.");
else if (mode != "take" && mode != "give")
    refused.Add("\"" + action + "\" is not take or give. NOTHING WAS CHANGED.");
else if (mode == "take")
{
    var wanted = new List<string>();
    foreach (var piece in (worksetNames ?? "").Split(','))
        if (piece.Trim().Length > 0) wanted.Add(piece.Trim());

    var byName = new Dictionary<string, Workset>(StringComparer.OrdinalIgnoreCase);
    foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
        byName[workset.Name] = workset;

    var ids = new HashSet<WorksetId>();
    foreach (var name in wanted)
    {
        Workset found;
        if (byName.TryGetValue(name, out found)) ids.Add(found.Id);
        else refused.Add("No user workset is called '" + name + "' - left out.");
    }

    if (wanted.Count == 0)
        refused.Add("No workset was named, so NOTHING WAS CHANGED. Name them, comma separated.");
    else if (ids.Count > 0)
    {
        try
        {
            WorksharingUtils.CheckoutWorksets(doc, ids, new TransactWithCentralOptions());
            var lines = new List<string>();
            var me = doc.Application.Username;
            var table = doc.GetWorksetTable();
            foreach (var id in ids)
            {
                var now = table.GetWorkset(id);
                var owner = now == null ? "" : now.Owner;
                var mine = string.Equals(owner, me, StringComparison.OrdinalIgnoreCase);
                if (mine) changed++;
                lines.Add((now == null ? id.ToString() : now.Name) + ": "
                    + (mine ? "editable by me" : (string.IsNullOrEmpty(owner) ? "NOT taken - owned by nobody"
                        : "NOT taken - owned by " + owner)));
            }
            report = string.Join("; ", lines);
            findings.Add(string.Format("{0} of {1} workset(s) asked for are now editable by {2}. {3}",
                changed, ids.Count, me, report));
        }
        catch (Exception ex)
        {
            refused.Add("Revit refused to make them editable - " + ex.Message + " The central model may be out "
                + "of reach, or a transaction is open.");
        }
    }
}
else
{
    try
    {
        var given = WorksharingUtils.RelinquishOwnership(doc, new RelinquishOptions(true), new TransactWithCentralOptions());
        var worksets = given == null ? 0 : given.GetRelinquishedWorksets().Count;
        var elements = given == null ? 0 : given.GetRelinquishedElements().Count;
        changed = worksets + elements;
        report = string.Format("{0} workset(s) and {1} element(s) given back", worksets, elements);
        findings.Add(report + ". Anything changed since the last sync stays owned until SYNC_WITH_CENTRAL.");
    }
    catch (Exception ex)
    {
        refused.Add("Revit refused to relinquish - " + ex.Message + " The central model may be out of reach, or a "
            + "transaction is open.");
    }
}

foreach (var reason in refused) findings.Add(reason);
