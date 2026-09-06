// NOT STANDALONE. Assumes `doc`, `elements`, `hostType` and `heightAboveLevel`
// are in scope, and leaves `created`, `withHoles`, `unenclosed` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE HOLES ARE THE POINT.
//
// A room's boundary is several LOOPS: the first is the outside edge, and every
// one after it is a shaft, a core or a column the room wraps around. Build
// from the first alone and the slab covers the shaft - correct in plan,
// through every check, and found by whoever tries to run a riser.
//
// THE CREATION CALL MOVED, SO IT IS LOOKED UP RATHER THAN NAMED.
//
// A floor is made one way up to 2021 and another from 2022; a ceiling cannot
// be made from code at all before 2022. Naming either in source stops this
// compiling on the other half of the range, so both are found at run time -
// and a release with no ceiling call says so rather than failing to build.

var created = new Dictionary<ElementId, ElementId>();
var withHoles = new List<ElementId>();
var unenclosed = new List<ElementId>();
var refused = new List<string>();

bool wantCeiling = hostType is CeilingType;
bool wantFloor = hostType is FloorType;

var floorNewStyle = typeof(Floor).GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

System.Reflection.MethodInfo floorOldStyle = null;
foreach (var candidate in doc.Create.GetType().GetMethods())
{
    if (candidate.Name != "NewFloor") continue;
    var parameters = candidate.GetParameters();
    if (parameters.Length == 4 && parameters[0].ParameterType == typeof(CurveArray))
    {
        floorOldStyle = candidate;
        break;
    }
}

var ceilingCreate = typeof(Ceiling).GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

if (!wantCeiling && !wantFloor)
    refused.Add("The type given is neither a floor type nor a ceiling type, so there is nothing to " +
                "decide what to build.");
else if (wantCeiling && ceilingCreate == null)
    refused.Add("This Revit has no way to create a ceiling from code - it arrived at 2022. A floor " +
                "type works here, or the ceilings are drawn by hand.");
else if (wantFloor && floorNewStyle == null && floorOldStyle == null)
    refused.Add("This Revit has neither way to create a floor.");
else
{
    var boundaryOptions = new SpatialElementBoundaryOptions();

    foreach (var element in elements)
    {
        var room = element as SpatialElement;
        if (room == null) { refused.Add("Not a room or space: " + element.Id.ToString()); continue; }

        IList<IList<BoundarySegment>> loops = null;
        try { loops = room.GetBoundarySegments(boundaryOptions); } catch { }

        double area = 0;
        try { area = room.Area; } catch { }

        if (loops == null || loops.Count == 0 || area <= 0) { unenclosed.Add(room.Id); continue; }

        // Every loop: the first is the outline and the rest are holes.
        var curveLoops = new List<CurveLoop>();
        foreach (var loop in loops)
        {
            var curves = new List<Curve>();
            foreach (var segment in loop)
            {
                Curve curve = null;
                try { curve = segment.GetCurve(); } catch { }
                if (curve != null) curves.Add(curve);
            }
            if (curves.Count < 3) continue;
            try { curveLoops.Add(CurveLoop.Create(curves)); } catch { }
        }

        if (curveLoops.Count == 0) { unenclosed.Add(room.Id); continue; }
        if (curveLoops.Count > 1) withHoles.Add(room.Id);

        var levelId = ElementId.InvalidElementId;
        try { if (room.Level != null) levelId = room.Level.Id; } catch { }
        if (levelId == ElementId.InvalidElementId)
        {
            refused.Add("No level on room " + room.Id.ToString());
            continue;
        }

        Element built = null;

        if (wantCeiling)
        {
            try
            {
                built = ceilingCreate.Invoke(null,
                    new object[] { doc, (IList<CurveLoop>)curveLoops, hostType.Id, levelId }) as Element;
            }
            catch { }
        }
        else if (floorNewStyle != null)
        {
            try
            {
                built = floorNewStyle.Invoke(null,
                    new object[] { doc, (IList<CurveLoop>)curveLoops, hostType.Id, levelId }) as Element;
            }
            catch { }
        }
        else
        {
            // Pre-2022: one loop only, so a room with a hole gets a slab over
            // the shaft. It is built and NAMED rather than skipped - the caller
            // decides whether that is acceptable, but nobody finds out later.
            try
            {
                var array = new CurveArray();
                foreach (var curve in curveLoops[0]) array.Append(curve);
                var levelElement = doc.GetElement(levelId) as Level;
                built = floorOldStyle.Invoke(doc.Create,
                    new object[] { array, hostType, levelElement, false }) as Element;
                if (curveLoops.Count > 1)
                    refused.Add("Room " + room.Id.ToString() + ": this release takes one loop only, so " +
                                "its hole was NOT cut.");
            }
            catch { }
        }

        if (built == null) { refused.Add("Revit refused to build on room " + room.Id.ToString()); continue; }

        if (wantCeiling)
        {
            try
            {
                var offset = built.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                if (offset != null && !offset.IsReadOnly) offset.Set(heightAboveLevel);
            }
            catch { }
        }

        created[room.Id] = built.Id;
    }
}
