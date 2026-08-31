// NOT STANDALONE. Assumes `elements` (Rooms and/or Spaces) and `doc` are in
// scope, and leaves `mainCeiling`, `mainHeight`, `ceilingsOver`, `heights`,
// `coverage` and `noCeiling` behind.
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
