// NOT STANDALONE. Assumes `elements` (Rooms and/or Spaces), `doc` and
// `includeLinks` are in scope, and leaves `mainCeiling`, `mainHeight`,
// `ceilingsOver`, `heights`, `coverage`, `noCeiling`, `linksSearched` and
// `linkedMatches` behind.
//
// READ ONLY, AND THAT COST SOMETHING. See the last section.
//
// A ROOM USUALLY HAS MORE THAN ONE CEILING.
//
// A bulkhead, a soffit over the joinery and the main grid are three ceilings
// over one room at three heights. Taking the first found is how a report quotes
// a 400 mm bulkhead as the room's ceiling height. The MAIN one here is the
// ceiling covering the most of the room in plan, and every ceiling found comes
// back with its own height so the drop and the bulkhead are both visible.
//
// THE HEIGHT IS THE CEILING'S OWN PARAMETER, NOT GEOMETRY.
//
// Height-above-level plus the level's elevation is exact, cheap, and is what
// Revit's own Properties palette shows. Deriving it from a solid gives a second
// figure disagreeing in the last decimal, and two numbers that both look right
// is worse than one that is plainly wrong.
//
// ProjectElevation, NOT Elevation. A level has two heights and only one is in
// the same space as the model's coordinates; on a survey-offset site model the
// other is wrong by exactly that offset, silently.
//
// WHAT THIS DELIBERATELY DOES NOT DO.
//
// The obvious method is to intersect the room's SOLID with the ceilings. A
// room's solid stops at its Upper Limit, and on a great many real models that
// is left just above the room's own level - so the solid stops BELOW the
// ceiling, intersects nothing, and the report says "no ceiling" for a room that
// plainly has one. The known workaround is to raise the upper limit, measure,
// and roll it back.
//
// A FRAGMENT HERE MAY NOT DO THAT. Golden Rule 16: a fragment runs inside a
// transaction it did not open and must not open one, so a change-and-rollback
// would land inside somebody else's batch and take its undo entry with it. A
// read that quietly writes is worse than a read that returns less. The
// plan-overlap test below needs no room solid, so it never meets the trap.
//
// THE COST, STATED RATHER THAN HIDDEN: bounding boxes are axis-aligned, so a
// rotated room over-reports how much of it a ceiling covers, and ranking by
// plan area can choose differently from a volume test where a large thin soffit
// competes with a smaller main grid. `coverage` comes back per ceiling so a
// close call is visible instead of settled in silence.
//
// THE CEILINGS ARE USUALLY THE ARCHITECT'S, IN A LINK, AND THEY ARE READ ONLY
// WHEN ASKED FOR - D-59. An MEP model's spaces sit under ceilings it does not
// contain. With `includeLinks` set, every loaded link's ceilings are measured
// the same way - their own height above their own level - carried through
// each placement into THIS model's space, and tested against each room or
// space by the same plan-overlap rule. The main linked ceiling per room is
// TEXT in `linkedMatches`, with its clear height, coverage and type. Every
// dictionary above stays this model's own: they are keyed and valued by ids
// the next step would look up here, and a linked ceiling's id is not one
// (FRAGMENT-ISSUES row 75). NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS
// THEM. Only model elements are read from a link, never its views or sheets.

var mainCeiling = new Dictionary<ElementId, ElementId>();
var mainHeight = new Dictionary<ElementId, double>();
var ceilingsOver = new Dictionary<ElementId, IList<ElementId>>();
var heights = new Dictionary<ElementId, double>();
var coverage = new Dictionary<ElementId, double>();
var noCeiling = new List<ElementId>();

// Collected by CATEGORY, not by the `Ceiling` class. The class IS reachable
// here - measured, not assumed - so this is a choice: a category collector
// catches a ceiling from any family or subclass without a cast, which is what a
// real model contains.
var allCeilings = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Ceilings)
    .WhereElementIsNotElementType()
    .ToElements();

// Height once per ceiling, not once per room. The underside above the project
// origin: the ceiling's own height-above-level plus that level's elevation.
var undersideOf = new Dictionary<ElementId, double>();
var boxOf = new Dictionary<ElementId, BoundingBoxXYZ>();

foreach (var ceiling in allCeilings)
{
    BoundingBoxXYZ box = null;
    try { box = ceiling.get_BoundingBox(null); } catch { }
    if (box == null) continue;
    boxOf[ceiling.Id] = box;

    double aboveLevel = 0.0;
    try
    {
        var p = ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
        if (p != null && p.HasValue) aboveLevel = p.AsDouble();
    }
    catch { }

    double levelElevation = 0.0;
    try
    {
        var level = doc.GetElement(ceiling.LevelId) as Level;
        if (level != null) levelElevation = level.ProjectElevation;
    }
    catch { }

    undersideOf[ceiling.Id] = levelElevation + aboveLevel;
}

foreach (var element in elements)
{
    var spatial = element as SpatialElement;
    if (spatial == null) continue;

    BoundingBoxXYZ roomBox = null;
    try { roomBox = spatial.get_BoundingBox(null); } catch { }
    if (roomBox == null) { noCeiling.Add(element.Id); continue; }

    double roomFloor = roomBox.Min.Z;
    var over = new List<ElementId>();
    ElementId best = ElementId.InvalidElementId;
    double bestOverlap = 0.0;

    foreach (var pair in boxOf)
    {
        var box = pair.Value;

        // Overlap in plan only. A ceiling that does not sit over this room in
        // plan is not this room's ceiling whatever its height.
        double wide = Math.Min(roomBox.Max.X, box.Max.X) - Math.Max(roomBox.Min.X, box.Min.X);
        double deep = Math.Min(roomBox.Max.Y, box.Max.Y) - Math.Max(roomBox.Min.Y, box.Min.Y);
        if (wide <= 0.0 || deep <= 0.0) continue;

        // And it has to be ABOVE the floor. Without this a ceiling on the
        // storey below is directly under the room in plan and would be picked.
        double underside;
        if (!undersideOf.TryGetValue(pair.Key, out underside)) continue;
        if (underside <= roomFloor) continue;

        // Nor may it be so far above that it belongs to the storey above. The
        // room's own box top is the honest limit: it is where the room stops.
        if (underside > roomBox.Max.Z) continue;

        over.Add(pair.Key);
        heights[pair.Key] = underside - roomFloor;

        double overlap = wide * deep;
        double roomArea = (roomBox.Max.X - roomBox.Min.X) * (roomBox.Max.Y - roomBox.Min.Y);
        coverage[pair.Key] = roomArea > 0.0 ? overlap / roomArea * 100.0 : 0.0;

        if (overlap > bestOverlap) { bestOverlap = overlap; best = pair.Key; }
    }

    if (over.Count == 0) { noCeiling.Add(element.Id); continue; }

    ceilingsOver[element.Id] = over;
    mainCeiling[element.Id] = best;
    mainHeight[element.Id] = heights[best];
}

// ---- D-59: the linked ceilings over the same rooms ------------------------

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

var linkBlocked = "";

// Every linked ceiling as a box in THIS model's space and an underside height
// here, with a name for the answer. One entry per placement.
var linkedBoxes = new List<Outline>();
var linkedUndersides = new List<double>();
var linkedNames = new List<string>();

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var count = 0;
    IList<Element> ceilings = new List<Element>();
    try
    {
        ceilings = new FilteredElementCollector(linked)
            .OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType().ToElements();
    }
    catch (Exception) { }

    foreach (var placement in linkPlacements[i])
    {
        Transform placed = null;
        try { placed = placement.GetTotalTransform(); } catch (Exception) { }
        if (placed == null) continue;

        foreach (var ceiling in ceilings)
        {
            BoundingBoxXYZ box = null;
            try { box = ceiling.get_BoundingBox(null); } catch { }
            if (box == null) continue;

            double aboveLevel = 0.0;
            try
            {
                var p = ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                if (p != null && p.HasValue) aboveLevel = p.AsDouble();
            }
            catch { }
            double levelElevation = 0.0;
            try
            {
                var level = linked.GetElement(ceiling.LevelId) as Level;
                if (level != null) levelElevation = level.ProjectElevation;
            }
            catch { }

            // The box's eight corners, carried here.
            var low = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            var high = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            foreach (var x in new[] { box.Min.X, box.Max.X })
                foreach (var y in new[] { box.Min.Y, box.Max.Y })
                    foreach (var z in new[] { box.Min.Z, box.Max.Z })
                    {
                        var corner = placed.OfPoint(new XYZ(x, y, z));
                        low = new XYZ(Math.Min(low.X, corner.X), Math.Min(low.Y, corner.Y), Math.Min(low.Z, corner.Z));
                        high = new XYZ(Math.Max(high.X, corner.X), Math.Max(high.Y, corner.Y), Math.Max(high.Z, corner.Z));
                    }

            var typeName = "";
            try
            {
                var type = linked.GetElement(ceiling.GetTypeId()) as ElementType;
                if (type != null) typeName = type.Name;
            }
            catch { }

            linkedBoxes.Add(new Outline(low, high));
            linkedUndersides.Add(placed.OfPoint(new XYZ(0, 0, levelElevation + aboveLevel)).Z);
            linkedNames.Add(string.Format("{0} - Ceilings '{1}' (id {2} in the link)",
                linked.Title, typeName, ceiling.Id));
            count++;
        }
    }
    linksSearched++;
    linkedMatches.Add(string.Format("{0}: {1} ceiling placement(s) read", linked.Title, count));
}

foreach (var element in linkDocs.Count > 0 ? elements : new List<Element>())
{
    var spatial = element as SpatialElement;
    if (spatial == null) continue;
    BoundingBoxXYZ roomBox = null;
    try { roomBox = spatial.get_BoundingBox(null); } catch { }
    if (roomBox == null) continue;

    var roomFloor = roomBox.Min.Z;
    var roomArea = (roomBox.Max.X - roomBox.Min.X) * (roomBox.Max.Y - roomBox.Min.Y);
    var best = -1;
    var bestOverlap = 0.0;
    var over = 0;
    for (var c = 0; c < linkedBoxes.Count; c++)
    {
        var box = linkedBoxes[c];
        var wide = Math.Min(roomBox.Max.X, box.MaximumPoint.X) - Math.Max(roomBox.Min.X, box.MinimumPoint.X);
        var deep = Math.Min(roomBox.Max.Y, box.MaximumPoint.Y) - Math.Max(roomBox.Min.Y, box.MinimumPoint.Y);
        if (wide <= 0.0 || deep <= 0.0) continue;
        var underside = linkedUndersides[c];
        if (underside <= roomFloor || underside > roomBox.Max.Z) continue;
        over++;
        if (wide * deep > bestOverlap) { bestOverlap = wide * deep; best = c; }
    }

    var roomName = "";
    try { roomName = spatial.Name ?? ""; } catch { }
    if (best < 0)
    {
        linkedMatches.Add(string.Format("  '{0}' (id {1}): no linked ceiling over it", roomName, element.Id));
        continue;
    }
    linkedTotal++;
    linkedMatches.Add(string.Format("  '{0}' (id {1}): {2:0} mm clear under {3}, covering {4:0}% in plan"
        + "{5}", roomName, element.Id, (linkedUndersides[best] - roomFloor) * 304.8, linkedNames[best],
        roomArea > 0.0 ? bestOverlap / roomArea * 100.0 : 0.0,
        over > 1 ? string.Format("; {0} linked ceilings over it in all", over) : ""));
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} room(s) or space(s) under a linked ceiling, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
