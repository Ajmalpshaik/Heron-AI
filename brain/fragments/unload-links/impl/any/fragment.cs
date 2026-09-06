// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `unloaded`, `alreadyUnloaded`, `notALink` and `refused` behind.
//
// OPENS NO TRANSACTION (Golden Rule 16).
//
// This is document management rather than a model edit, and whether Revit
// permits it inside an open transaction is UNPROVEN here - the same position
// the reload fragment takes, and for the same reason: guessing either way
// would harden into a fact nobody checked.
//
// UNLOADING IS NOT REMOVING.
//
// An unloaded link is still in the project, still holds its position, and
// comes back with one reload. A removed one is gone with its position and
// everything hosted on it. Removing is deleting an element and this library
// does that deliberately elsewhere; it is not offered here, where somebody
// typing "get rid of the link" would find it by accident.
//
// UNLOADING IS A PROPERTY OF THE TYPE.
//
// The same building placed twice shares one link type. Three instances is one
// unload, and reporting three would be a count nobody could reconcile with the
// Manage Links dialog.

var unloaded = new List<ElementId>();
var alreadyUnloaded = new List<ElementId>();
var notALink = new List<ElementId>();
var refused = new List<string>();

var seenTypes = new HashSet<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    RevitLinkType linkType = element as RevitLinkType;

    if (linkType == null)
    {
        var instance = element as RevitLinkInstance;
        if (instance != null)
        {
            try { linkType = doc.GetElement(instance.GetTypeId()) as RevitLinkType; } catch { }
        }
    }

    if (linkType == null) { notALink.Add(element.Id); continue; }

    // One unload per TYPE, however many instances arrived.
    if (!seenTypes.Add(linkType.Id)) continue;

    string name = "";
    try { name = linkType.Name ?? ""; } catch { }

    bool loaded = false;
    // STATIC, not an instance call - the compile gate caught it written
    // the obvious way, which is how a fragment that reads correctly fails to
    // build.
    try { loaded = RevitLinkType.IsLoaded(doc, linkType.Id); } catch { }

    if (!loaded) { alreadyUnloaded.Add(linkType.Id); continue; }

    try { linkType.Unload(null); }
    catch { refused.Add(name + ": Revit refused the unload."); continue; }

    // Read back: loaded or not is the fact, and the call returning is not it.
    bool stillLoaded = true;
    try { stillLoaded = RevitLinkType.IsLoaded(doc, linkType.Id); } catch { }

    if (!stillLoaded) unloaded.Add(linkType.Id);
    else refused.Add(name + ": it is still loaded after the unload call.");
}
