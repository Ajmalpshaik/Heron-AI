// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `notLoaded` behind.
//
// READ ONLY. Opens no transaction, needs none, and never touches a linked
// file - which is this agent's defining constraint, not a limitation of the
// implementation.
//
// LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT. GetLinkDocument() returns
// null exactly when there is no geometry to read, whatever the link's recorded
// status says. There IS a status accessor and it is not used here on purpose:
// a link can be recorded as loaded and hand back no document - after a
// workset-level unload, or when the file has moved - and every question that
// follows ("does it clash", "how many rooms") depends on the geometry being
// readable, not on what Revit last wrote down. Same discipline as
// TRACE_CONNECTIVITY, where IsConnected described intent and the geometry
// described reality.
//
// ONE ROW PER FILE, NOT PER PLACEMENT. Instances are what the collector
// returns; the TYPE is the file. A wing linked in twice is one model placed
// twice, and counting instances reports a project with four links as having
// nine.
//
// `elements` CARRIES THE INSTANCES, not the types, because an instance is what
// has a position in this model - the thing SET_SELECTION can highlight and
// ISOLATE_ELEMENTS can leave on screen. A type has no location and cannot be
// pointed at.

var elements = new List<Element>();
var findings = new List<string>();
var notLoaded = new List<ElementId>();

var placements = new Dictionary<ElementId, int>();
var loaded = new Dictionary<ElementId, bool>();
var order = new List<ElementId>();

foreach (var instance in new FilteredElementCollector(doc)
                             .OfClass(typeof(RevitLinkInstance))
                             .Cast<RevitLinkInstance>())
{
    if (instance == null) continue;

    elements.Add(instance);

    var typeId = instance.GetTypeId();
    if (typeId == null || typeId == ElementId.InvalidElementId) continue;

    if (!placements.ContainsKey(typeId))
    {
        placements[typeId] = 0;
        loaded[typeId] = false;
        order.Add(typeId);
    }
    placements[typeId] = placements[typeId] + 1;

    // ANY placement handing back a document means the file is loaded. Not all
    // of them: an instance can be hidden or on a closed workset while the file
    // itself is perfectly available, and reporting the file unloaded because
    // one placement was quiet would send somebody looking for a missing file
    // that is not missing.
    if (instance.GetLinkDocument() != null) loaded[typeId] = true;
}

foreach (var typeId in order)
{
    var linkType = doc.GetElement(typeId);

    // The type name is the file name as Revit records it. A type whose element
    // has gone is still worth a row - the instances are real and on screen -
    // so the name falls back rather than the row disappearing.
    var name = linkType == null ? "(the link type is no longer in this model)" : linkType.Name;

    var isLoaded = loaded[typeId];
    if (!isLoaded) notLoaded.Add(typeId);

    var count = placements[typeId];

    findings.Add(string.Format("{0}  - {1}, placed {2} time{3}",
                               name,
                               isLoaded ? "loaded" : "NOT LOADED, nothing in it can be read",
                               count,
                               count == 1 ? "" : "s"));
}
