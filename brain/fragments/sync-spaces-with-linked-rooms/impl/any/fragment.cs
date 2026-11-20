// NOT STANDALONE. Assumes `doc`, `linkName`, `phaseName` and `updateNames` are in
// scope; leaves `created`, `renamed`, `alreadyMatching`, `orphans`,
// `notPlaced`, `noArea`, `report` and `findings` behind.
//
// VERSION 2 (2026-11-20) - A SPACE WITH NO AREA IS NOT COUNTED AS MADE. Row
// 5b-1125 (Ajmalpshaik/Heron-AI#921): on Project1, Revit 2020, both spaces
// made read back with NO AREA and still counted in `created`, so the run
// judged PASS. The read-back below said it would "drop any that did not
// hold" and only wrote a line. Now a space made that reads back with no area
// leaves `created` and goes to `noArea`: it is still in the model, where the
// linked model has a room, and takes the room's shape once something bounds
// it - its line says whether the link's type is Room Bounding, read, never
// guessed. A space is still made only where the linked model has a placed
// room; nowhere else (the owner's word, 2026-11-20).
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE MEP MODEL'S SPACES, KEPT IN STEP WITH THE ARCHITECT'S ROOMS. For every
// placed room in the linked model:
//
//   no space at its point in this model   a space is made there, on the
//                                         level at the same height, and
//                                         given the room's number and name
//   a space there, number or name differs renamed to match - only when
//                                         updateNames is true, otherwise
//                                         reported and left
//   a space there that already matches    counted, nothing written
//
// And every space in this model with no linked room under it is listed as an
// ORPHAN - the room was deleted or moved in the architect's model. It is
// never deleted here: deleting a space loses its loads and its tags, and that
// is the modeller's call.
//
// THE POINT IS LIFTED 300 MM. A room's location point sits exactly on its
// level, which is the bottom face of the space above it - a borderline that
// can read as outside. Lifted, it is unambiguously in the air of the room.
//
// THE LEVEL IS MATCHED BY HEIGHT, never by name: the architect's "Level 1" and
// the MEP model's "01 - Ground" are the same floor when they are the same
// height. A room with no level at its height in this model is named in
// notPlaced, not put on the nearest one.

const double MmPerFoot = 304.8;
var lift = 300.0 / MmPerFoot;
var levelTolerance = 10.0 / MmPerFoot;

var created = new List<ElementId>();
var renamed = new List<ElementId>();
var alreadyMatching = 0;
var orphans = new List<ElementId>();
var notPlaced = new List<string>();
// VERSION 2: spaces made that read back with no area - counted apart.
var noArea = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

// THE LINK BY NAME, OR THE ONLY LOADED ONE WHEN THE NAME IS EMPTY. A string
// and not a RevitLinkInstance need, because a typed need cannot be left out -
// the binder refuses an absent one - and "the only link" is the common case.
// A link instance's name reads "file.rvt : 1 : location", so the given name
// is matched against the start of it as well as the whole.
RevitLinkInstance source = null;
var wantedLink = (linkName ?? "").Trim();
var loaded = new List<RevitLinkInstance>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
{
    var instance = element as RevitLinkInstance;
    if (instance != null && instance.GetLinkDocument() != null) loaded.Add(instance);
}
if (wantedLink != "")
{
    var matching = loaded.Where(l => string.Equals(l.Name, wantedLink, StringComparison.OrdinalIgnoreCase)
        || l.Name.StartsWith(wantedLink, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matching.Count == 1) source = matching[0];
    else refusal = matching.Count == 0
        ? "No loaded link is named \"" + wantedLink + "\", so NOTHING WAS MADE. Loaded: "
          + (loaded.Count == 0 ? "none" : string.Join(", ", loaded.Select(l => l.Name))) + "."
        : matching.Count + " loaded links answer to \"" + wantedLink + "\" - "
          + string.Join(", ", matching.Select(l => l.Name)) + " - so NOTHING WAS MADE. Give the whole name.";
}
else if (loaded.Count == 1) source = loaded[0];
else refusal = loaded.Count == 0
    ? "No linked model is loaded, so NOTHING WAS MADE. Load the architect's link first."
    : loaded.Count + " linked models are loaded - " + string.Join(", ", loaded.Select(l => l.Name))
      + " - and nothing says which holds the rooms, so NOTHING WAS MADE. Name the link.";

Document linked = null;
if (refusal == null)
{
    linked = source.GetLinkDocument();
    if (linked == null) refusal = "\"" + source.Name + "\" is not loaded, so NOTHING WAS MADE.";
}

// THE PHASE BY NAME, OR THE LAST PHASE WHEN THE NAME IS EMPTY.
Phase onPhase = null;
var wantedPhase = (phaseName ?? "").Trim();
if (refusal == null)
{
    if (doc.Phases.Size == 0) refusal = "This model has no phase, so NOTHING WAS MADE.";
    else if (wantedPhase == "") onPhase = doc.Phases.get_Item(doc.Phases.Size - 1);
    else
    {
        foreach (Phase candidate in doc.Phases)
            if (string.Equals(candidate.Name, wantedPhase, StringComparison.OrdinalIgnoreCase)) onPhase = candidate;
        if (onPhase == null)
            refusal = "No phase is named \"" + wantedPhase + "\", so NOTHING WAS MADE.";
    }
}

Func<SpatialElement, string> nameOf = s =>
{
    try
    {
        var p = s.get_Parameter(BuiltInParameter.ROOM_NAME);
        return p == null ? "" : (p.AsString() ?? "");
    }
    catch { return ""; }
};

if (refusal == null)
{
    var toHost = source.GetTotalTransform();
    var toLink = toHost.Inverse;

    var levels = new List<Level>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
    {
        var level = element as Level;
        if (level != null) levels.Add(level);
    }

    foreach (var element in new FilteredElementCollector(linked).OfCategory(BuiltInCategory.OST_Rooms))
    {
        var room = element as Room;
        if (room == null || room.Area <= 0) continue;
        var at = room.Location as LocationPoint;
        if (at == null) continue;

        var number = room.Number ?? "";
        var name = nameOf(room);
        var label = "room " + number + " \"" + name + "\"";
        var hostPoint = toHost.OfPoint(at.Point);

        Space space = null;
        try { space = doc.GetSpaceAtPoint(hostPoint + new XYZ(0, 0, lift)); } catch { }

        if (space == null)
        {
            Level onLevel = null;
            foreach (var level in levels)
            {
                if (Math.Abs(level.ProjectElevation - hostPoint.Z) <= levelTolerance) { onLevel = level; break; }
            }
            if (onLevel == null)
            {
                notPlaced.Add(label + " - no level in this model at "
                    + Math.Round(hostPoint.Z * MmPerFoot) + " mm");
                continue;
            }
            try
            {
                var made = doc.Create.NewSpace(onLevel, onPhase, new UV(hostPoint.X, hostPoint.Y));
                if (made == null) { notPlaced.Add(label + " - Revit returned no space"); continue; }
                made.Number = number;
                var p = made.get_Parameter(BuiltInParameter.ROOM_NAME);
                if (p != null && !p.IsReadOnly) p.Set(name);
                created.Add(made.Id);
                lines.Add(label + ": space made on " + onLevel.Name);
            }
            catch (Exception ex)
            {
                notPlaced.Add(label + " - Revit refused: " + ex.Message);
            }
            continue;
        }

        var spaceName = nameOf(space);
        if (space.Number == number && spaceName == name) { alreadyMatching++; continue; }

        if (!updateNames)
        {
            lines.Add(label + ": space " + space.Id + " reads " + space.Number + " \"" + spaceName
                + "\" - DIFFERS, left as it is (updateNames is false)");
            continue;
        }
        try
        {
            var wasNumber = space.Number;
            if (space.Number != number) space.Number = number;
            var p = space.get_Parameter(BuiltInParameter.ROOM_NAME);
            if (spaceName != name && p != null && !p.IsReadOnly) p.Set(name);
            renamed.Add(space.Id);
            lines.Add(label + ": space " + space.Id + " was " + wasNumber + " \"" + spaceName
                + "\", renamed to match");
        }
        catch (Exception ex)
        {
            lines.Add(label + ": space " + space.Id + " could not be renamed - " + ex.Message);
        }
    }

    doc.Regenerate();

    // VERSION 2: whether the link's type is Room Bounding, read once for the
    // lines below - the parameter SET_LINK_ROOM_BOUNDING writes.
    string bounding = "its link's Room Bounding could not be read";
    try
    {
        var linkType = doc.GetElement(source.GetTypeId());
        var flag = linkType == null ? null : linkType.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
        if (flag != null && flag.HasValue && flag.StorageType == StorageType.Integer)
            bounding = flag.AsInteger() == 1
                ? "its link IS Room Bounding in this model, so what keeps it open is not established"
                : "its link is NOT Room Bounding in this model - SET_LINK_ROOM_BOUNDING turns it on";
    }
    catch { }

    // READ BACK what was made and drop any that did not hold (version 2: it
    // only said so before). A space made with no area leaves `created`.
    foreach (var id in created.ToList())
    {
        var space = doc.GetElement(id) as Space;
        if (space != null && space.Area > 0) continue;
        created.Remove(id);
        if (space != null) noArea.Add(id);
        lines.Add("space " + id + (space == null
            ? " was made and is gone on the read-back - NOT counted as made"
            : " reads back with NO AREA - placed where nothing bounds it, so NOT counted as made; "
              + bounding));
    }
    // A renamed space is read back by its number and name, which is what was
    // written to it.
    foreach (var id in renamed.ToList())
    {
        var space = doc.GetElement(id) as Space;
        if (space == null) { renamed.Remove(id); lines.Add("space " + id + " is gone on the read-back - NOT counted as renamed"); }
    }

    // ORPHANS: spaces here with no linked room under them.
    foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_MEPSpaces))
    {
        var space = element as Space;
        if (space == null || space.Area <= 0) continue;
        if (created.Contains(space.Id) || noArea.Contains(space.Id)) continue;
        var at = space.Location as LocationPoint;
        if (at == null) continue;
        Room under = null;
        try { under = linked.GetRoomAtPoint(toLink.OfPoint(at.Point + new XYZ(0, 0, lift))); } catch { }
        if (under == null) orphans.Add(space.Id);
    }

    findings.Add("From \"" + source.Name + "\": " + created.Count + " space(s) made, "
        + (noArea.Count > 0 ? noArea.Count + " made with NO AREA and not counted as made, " : "")
        + renamed.Count
        + " renamed, " + alreadyMatching + " already matching, " + notPlaced.Count + " room(s) not "
        + "placed, " + orphans.Count + " space(s) with no linked room under them (listed, never deleted).");
}

if (refusal != null) lines.Add(refusal);
lines.AddRange(notPlaced);
if (orphans.Count > 0)
    lines.Add("ORPHAN spaces, no linked room under them: " + string.Join(", ", orphans.Select(i => i.ToString())));
var report = string.Join(" | ", lines);
findings.AddRange(lines);
