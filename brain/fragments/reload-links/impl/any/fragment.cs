// NOT STANDALONE. Assumes `doc`, `elements` and `unload` are in scope; leaves
// `findings`, `done` and `notLinks` behind.
//
// OPENS NO TRANSACTION (Golden Rule 16), AND THIS ONE NEEDS SAYING TWICE.
// Reloading is document management rather than a model edit - it reads a file
// from disk. Whether Revit permits it INSIDE an open transaction is not proven
// here, and it is the first thing to find out against a real model. If it
// refuses, the answer is where the host's write path puts its transaction, not
// a change to this fragment.
//
// NOTHING IS REMOVED. Unload drops a link from memory and from every view and
// it returns with one click. Removing deletes it and everything hosted on it,
// and that belongs behind a deliberate delete.
//
// ONE FILE SHARED BY THREE INSTANCES IS READ ONCE. The work is on the link
// TYPE, so instances are reduced to their distinct types first - otherwise the
// same file comes off disk three times and the report claims three.
//
// REVIT'S OWN RESULT CODE IS PRINTED RAW FOR EVERY LINK. Which code comes back
// for a link that was already up to date is NOT established, and printing what
// Revit actually returns is how the first real run settles it - rather than a
// guess hardening into a fact by being repeated.

var findings = new List<string>();
var done = 0;
var notLinks = new List<ElementId>();

// Distinct link TYPES behind the instances handed in.
var typeIds = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    var instance = element as RevitLinkInstance;
    var typeId = instance != null ? instance.GetTypeId() : ElementId.InvalidElementId;

    // A link TYPE handed in directly is just as good as an instance of it.
    if (instance == null && element is RevitLinkType) typeId = element.Id;

    if (typeId == ElementId.InvalidElementId) { notLinks.Add(element.Id); continue; }

    // ElementId to ElementId. Never as a number.
    var seen = false;
    foreach (var known in typeIds) if (known == typeId) { seen = true; break; }
    if (!seen) typeIds.Add(typeId);
}

if (typeIds.Count == 0)
{
    findings.Add("Nothing handed in is a linked model, so there is nothing to reload");
}
else
{
    foreach (var typeId in typeIds)
    {
        var linkType = doc.GetElement(typeId) as RevitLinkType;
        if (linkType == null) { notLinks.Add(typeId); continue; }

        var name = linkType.Name ?? typeId.ToString();

        try
        {
            if (unload)
            {
                linkType.Unload(null);
                findings.Add(string.Format("'{0}' unloaded - it is out of every view until it is "
                    + "reloaded, and nothing has been deleted", name));
                done++;
            }
            else
            {
                var result = linkType.Reload();
                // Printed raw. See the header.
                findings.Add(string.Format("'{0}': Revit reports {1}", name, result.LoadResult));
                if (result.LoadResult == LinkLoadResultType.LinkLoaded
                    || result.LoadResult == LinkLoadResultType.UsedExisting) done++;
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("'{0}' FAILED: {1}", name, ex.Message));
        }
    }

    findings.Add(string.Format("{0} of {1} link(s) {2}", done, typeIds.Count,
        unload ? "unloaded" : "reloaded"));
}
