// NOT STANDALONE. Assumes `doc`, `elements` and `unload` are in scope; leaves
// `findings`, `done`, `notLinks`, `unloaded`, `alreadyUnloaded` and `refused`
// behind.
//
// VERSION 2 (2026-11-01) - UNLOAD_LINKS MERGED IN, AND WHAT IT HAD THAT THE
// UNLOAD HALF HERE LACKED:
//
//   A LINK ALREADY UNLOADED IS SKIPPED AND SAID. Asked through
//   RevitLinkType.IsLoaded - STATIC, the compile gate caught it written as an
//   instance call - before anything is done; its type id goes to
//   `alreadyUnloaded` and it is not counted in `done`. Nothing happened, and
//   the report has to say so.
//
//   READ BACK AFTER UNLOADING. The call returning is not the link unloaded:
//   IsLoaded is asked again, and only a link that now reads unloaded goes to
//   `unloaded` and counts in `done`. One still loaded goes to `refused`, and so
//   does an unload Revit refused outright - in the user's words, never only a
//   line among the findings.
//
//   A RELOAD REVIT THREW ON is in `refused` too, beside its FAILED line, so a
//   caller reading `refused` sees every link this did not do.
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
var unloaded = new List<ElementId>();
var alreadyUnloaded = new List<ElementId>();
var refused = new List<string>();

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
    findings.Add("Nothing handed in is a linked model, so there is nothing to "
        + (unload ? "unload" : "reload"));
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
                // UNLOAD_LINKS' rules (version 2): already unloaded is skipped
                // and said; the unload is read back, never assumed.
                var loaded = false;
                try { loaded = RevitLinkType.IsLoaded(doc, linkType.Id); } catch (Exception) { }
                if (!loaded)
                {
                    alreadyUnloaded.Add(linkType.Id);
                    findings.Add(string.Format("'{0}' was ALREADY unloaded - nothing was done to "
                        + "it", name));
                    continue;
                }

                try { linkType.Unload(null); }
                catch (Exception ex)
                {
                    refused.Add(name + ": Revit refused the unload - " + ex.Message);
                    findings.Add(string.Format("'{0}' FAILED: Revit refused the unload - {1}",
                        name, ex.Message));
                    continue;
                }

                var stillLoaded = true;
                try { stillLoaded = RevitLinkType.IsLoaded(doc, linkType.Id); } catch (Exception) { }
                if (stillLoaded)
                {
                    refused.Add(name + ": it is still loaded after the unload call.");
                    findings.Add(string.Format("'{0}' is STILL LOADED after the unload call - "
                        + "read back, so it is not counted", name));
                    continue;
                }

                unloaded.Add(linkType.Id);
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
            refused.Add(name + ": " + ex.Message);
            findings.Add(string.Format("'{0}' FAILED: {1}", name, ex.Message));
        }
    }

    findings.Add(string.Format("{0} of {1} link(s) {2}{3}", done, typeIds.Count,
        unload ? "unloaded" : "reloaded",
        alreadyUnloaded.Count > 0
            ? string.Format("; {0} already unloaded, left as they were", alreadyUnloaded.Count)
            : ""));
}
