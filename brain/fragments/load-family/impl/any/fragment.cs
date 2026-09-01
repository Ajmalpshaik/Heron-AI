// NOT STANDALONE. Assumes `doc`, `uidoc`, `paths` and `askOnConflict` are in
// scope, and leaves `loaded`, `types`, `alreadyPresent` and `unreadable`
// behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// AN EXISTING FAMILY IS REPORTED, NEVER SILENTLY OVERWRITTEN.
//
// The plain load simply refuses, and that refusal is the DEFAULT here on
// purpose: an office family is somebody's work, and replacing it without asking
// is how a model's content gets changed underneath the people using it.
//
// REVIT SHIPS THE OVERWRITE DECISION AS A DIALOG.
//
// GetRevitUIFamilyLoadOptions() returns Revit's OWN implementation - the one
// File > Load Family uses. Handing it to LoadFamily makes Revit ASK, in its own
// dialog, exactly as if the family had been loaded by hand. That is a real
// reload path with nobody's work overwritten behind their back.
//
// It is named DIRECTLY here. The version this was re-authored from reached it
// through reflection, which reads as "this might not exist" - the compile gate
// says it exists on all eight releases, and a measured call is worth more than
// a defensive one.
//
// THE WIDER LESSON: a technique needing an interface is not automatically out
// of reach for a fragment, which cannot declare a class. Check whether Revit
// already ships an implementation.
//
// A FRESHLY LOADED TYPE IS NOT ACTIVE until first placed, and an inactive type
// makes placement fail with an error naming neither the family nor the reason.
// The types are returned so the caller can see what arrived.

var loaded = new List<string>();
var types = new List<ElementId>();
var alreadyPresent = new List<string>();
var unreadable = new Dictionary<string, string>();

foreach (var path in paths)
{
    if (string.IsNullOrWhiteSpace(path)) continue;

    Family family = null;
    bool ok = false;

    try
    {
        if (askOnConflict)
        {
            // Revit's own dialog. The user answers, exactly as they would by
            // hand - nothing is decided here on their behalf.
            var options = UIDocument.GetRevitUIFamilyLoadOptions();
            ok = doc.LoadFamily(path, options, out family);
        }
        else
        {
            ok = doc.LoadFamily(path, out family);
        }
    }
    catch (Exception ex)
    {
        unreadable[path] = ex.Message;
        continue;
    }

    if (!ok || family == null)
    {
        // The commonest reason by far, and it is not an error: the family is
        // already here and the plain load declines to replace it.
        alreadyPresent.Add(path);
        continue;
    }

    loaded.Add(path);

    try
    {
        foreach (var symbolId in family.GetFamilySymbolIds())
        {
            types.Add(symbolId);
        }
    }
    catch { }
}
