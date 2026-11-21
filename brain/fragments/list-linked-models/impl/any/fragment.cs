// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings`,
// `notLoaded`, `summary` and `skippedPlacements` behind.
//
// VERSION 3 (2026-11-21, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - A PLACEMENT WITH NO LINK TYPE IS COUNTED, NOT DROPPED. Version 2
// passed over a placement whose type Revit would not give: it stayed in
// `elements` but no file row named it, so `summary` could read empty - the
// answer for "no link" - with placements on screen. `skippedPlacements`
// carries the count every time; the sentence naming it rides only on the empty
// answer (no file listed), so an answer that lists files reads as version 2's.
//
// VERSION 2 (2026-11-20) - WHAT A MODELLER ASKS ABOUT A LINK, BESIDE LOADED.
// Row 5b-474, the owner's yes of 2026-11-20 (Ajmalpshaik/Heron-AI#921):
// "is the link room bounding" and "are the links relative or absolute" had no
// read and reached a write. Each FILE now says its path type, Room Bounding,
// reference type (Overlay or Attachment), whether it is nested in another
// link, whether it is unloaded for this user only, and the path it was saved
// with; each PLACEMENT says whether it is pinned, its workset on a workshared
// model, and its name as Revit shows it - which ends with the shared position,
// "<Not Shared>" when it has none. Every member read here is on 2020 to 2027
// (tools/api-surface, read 2026-11-20). A setting that cannot be read is said,
// never guessed. `summary` carries every line whole: a reply prints a list as
// its first three names (row 5b-195), and six links did not fit.
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
var summary = "";
var skippedPlacements = 0;

var placements = new Dictionary<ElementId, int>();
var loaded = new Dictionary<ElementId, bool>();
var order = new List<ElementId>();
// VERSION 2: what each placement is, under its file.
var pinnedCount = new Dictionary<ElementId, int>();
var placementLines = new Dictionary<ElementId, List<string>>();

bool workshared = false;
try { workshared = doc.IsWorkshared; } catch { }

foreach (var instance in new FilteredElementCollector(doc)
                             .OfClass(typeof(RevitLinkInstance))
                             .Cast<RevitLinkInstance>())
{
    if (instance == null) continue;

    elements.Add(instance);

    var typeId = instance.GetTypeId();
    // VERSION 3: counted, never silently dropped (D-64).
    if (typeId == null || typeId == ElementId.InvalidElementId) { skippedPlacements++; continue; }

    if (!placements.ContainsKey(typeId))
    {
        placements[typeId] = 0;
        loaded[typeId] = false;
        order.Add(typeId);
        pinnedCount[typeId] = 0;
        placementLines[typeId] = new List<string>();
    }
    placements[typeId] = placements[typeId] + 1;

    // VERSION 2: pinned, workset, and the name Revit gives the placement -
    // "<file> : <placement> : location <shared position>".
    string pinnedText = "pinned: NOT READ";
    try
    {
        var pinned = instance.Pinned;
        if (pinned) pinnedCount[typeId] = pinnedCount[typeId] + 1;
        pinnedText = pinned ? "pinned" : "NOT pinned";
    }
    catch { }
    string placementName = "";
    try { placementName = instance.Name ?? ""; } catch { }
    string worksetText = "";
    if (workshared)
    {
        worksetText = ", workset NOT READ";
        try
        {
            var workset = doc.GetWorksetTable().GetWorkset(instance.WorksetId);
            if (workset != null) worksetText = ", workset '" + workset.Name + "'";
        }
        catch { }
    }
    placementLines[typeId].Add("  - " + (placementName.Length > 0 ? placementName : "placement " + instance.Id)
        + ": " + pinnedText + worksetText);

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

    // ---- VERSION 2: the file's own settings, each read or said unread ----
    var fileType = linkType as RevitLinkType;
    string pathType = "NOT READ", roomBounding = "NOT READ", reference = "NOT READ";
    string nested = "NOT READ", localUnload = "NOT READ", savedPath = null;
    if (fileType != null)
    {
        // Relative, Absolute, Server (Revit Server) or Content - the enum's
        // own word, the one Manage Links shows.
        try { pathType = fileType.PathType.ToString(); } catch { }
        // The link type's Room Bounding box - the parameter
        // SET_LINK_ROOM_BOUNDING writes, read the same way.
        try
        {
            var bounding = fileType.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
            if (bounding != null && bounding.HasValue && bounding.StorageType == StorageType.Integer)
                roomBounding = bounding.AsInteger() == 1 ? "yes" : "no";
        }
        catch { }
        try { reference = fileType.AttachmentType.ToString(); } catch { }
        try { nested = fileType.IsNestedLink ? "yes - inside another link" : "no"; } catch { }
        try { localUnload = fileType.LocallyUnloaded ? "yes - unloaded for this user only" : "no"; } catch { }
        try
        {
            var fileReference = fileType.GetExternalFileReference();
            if (fileReference != null)
                savedPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(fileReference.GetPath());
        }
        catch { savedPath = null; }
    }

    var pinnedHere = pinnedCount[typeId];
    findings.Add(string.Format("{0}  - {1}, placed {2} time{3}; path {4}; Room Bounding {5}; {6}; pinned {7} of {2}",
                               name,
                               isLoaded ? "loaded" : "NOT LOADED, nothing in it can be read",
                               count,
                               count == 1 ? "" : "s",
                               pathType,
                               roomBounding,
                               reference,
                               pinnedHere));

    var block = new List<string>();
    block.Add(name + " - " + (isLoaded ? "loaded" : "NOT LOADED, nothing in it can be read")
        + ", placed " + count + " time" + (count == 1 ? "" : "s") + ".");
    block.Add("  Path type: " + pathType + (savedPath == null ? " (the saved path could not be read)" : " - saved as " + savedPath));
    block.Add("  Room Bounding: " + roomBounding + ". Reference type: " + reference
        + ". Nested: " + nested + ". Unloaded for me: " + localUnload + ".");
    block.AddRange(placementLines[typeId]);
    summary += (summary.Length == 0 ? "" : "\n") + string.Join("\n", block.ToArray());
}

// VERSION 3 (D-64): ON THE EMPTY ANSWER ONLY, what was passed over - so an
// empty `summary` is never the "no link" answer for placements that are there.
if (order.Count == 0 && skippedPlacements > 0)
    findings.Add(skippedPlacements + " link placement(s) are in this model but Revit gives no link type for "
        + "them, so no file is listed for them. They are still among the placements handed back, so they can be "
        + "selected and looked at.");
