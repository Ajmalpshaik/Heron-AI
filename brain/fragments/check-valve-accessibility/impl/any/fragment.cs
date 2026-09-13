// NOT STANDALONE. Assumes `doc`, `accessories`, `envelope` and
// `maxReachHeight` are in scope; leaves `findings`, `obstructed`,
// `needAccessPanel` and `outOfReach` behind.
//
// READ ONLY. Opens no transaction and needs none. Distances are internal FEET.
//
// THREE SEPARATE QUESTIONS, EACH ANSWERED SEPARATELY, because each has a
// different fix:
//   ROOM    - something inside the operating envelope. Move the obstruction.
//   CEILING - it sits above a ceiling, so it needs an ACCESS PANEL. A drawing
//             item, not a modelling one, and the thing most often forgotten.
//   HEIGHT  - out of reach from the floor. Platform, chain wheel, or relocate.
// A valve can pass one and fail another, so all three are reported per valve.
//
// ABOVE A CEILING IS NOT A FAULT. Most valves live in the ceiling void. The
// finding is that each NEEDS A PANEL, and that list goes to the architect. A
// check that cries wolf about every valve gets switched off by lunchtime.
//
// THE CEILING IS FOUND BY CASTING A RAY UPWARD, which needs a real View3D. The
// active view is used when it is already 3D, otherwise any non-template 3D view
// is borrowed. No 3D view at all is REPORTED, not returned as zero findings.
//
// HEIGHT IS ABOVE THE ACCESSORY'S OWN LEVEL, using ProjectElevation. A level has
// two heights and only one is in the same space as a point in the model. A
// raised floor makes the real reach shorter than this number.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
maxReachHeight = maxReachHeight / MillimetresPerFoot;

// AND `envelope` IS THE OTHER LENGTH, which the first pass of this block
// missed. It is added to and subtracted from `centre`, an XYZ in Revit's own
// units, to build the operating zone - so an unconverted 500 meant 500 FEET
// and put a 152-metre box around every valve. Everything within reach of a
// building would have read as an obstruction, and the check that exists to
// find crowded valves would have condemned all of them.
envelope = envelope / MillimetresPerFoot;

var findings = new List<string>();
var obstructed = new List<ElementId>();
var needAccessPanel = new List<ElementId>();
var outOfReach = new List<ElementId>();

// A 3D view for the ray cast.
View3D rayView = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
{
    var candidate = element as View3D;
    if (candidate != null && !candidate.IsTemplate) { rayView = candidate; break; }
}

var ceilingIds = new List<ElementId>();
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType())
{
    ceilingIds.Add(element.Id);
}

if (rayView == null)
{
    findings.Add("no 3D view in this model, so the CEILING question could not be asked at all - that "
        + "half of the check is missing rather than clear");
}
if (ceilingIds.Count == 0)
{
    findings.Add("no ceilings in the HOST model. If the architecture is a LINK, the ceiling question "
        + "has checked nothing and every valve below will read as not needing a panel");
}

foreach (var accessory in accessories)
{
    if (accessory == null || !accessory.IsValidObject) continue;

    var box = accessory.get_BoundingBox(null);
    if (box == null)
    {
        findings.Add(string.Format("{0} (id {1}): no readable geometry - not checked",
            accessory.Name, accessory.Id));
        continue;
    }

    var centre = (box.Min + box.Max) / 2.0;
    var verdicts = new List<string>();

    // ---- ROOM: a plain box round the accessory. An indication, not a swept
    // volume of a hand and a lever - see the header.
    var ignore = new HashSet<ElementId>();
    ignore.Add(accessory.Id);
    var instance = accessory as FamilyInstance;
    if (instance != null)
    {
        try { foreach (var id in instance.GetSubComponentIds()) ignore.Add(id); }
        catch { }
        try
        {
            var manager = instance.MEPModel == null ? null : instance.MEPModel.ConnectorManager;
            if (manager != null)
            {
                foreach (Connector connector in manager.Connectors)
                {
                    foreach (Connector other in connector.AllRefs)
                    {
                        if (other.Owner != null) ignore.Add(other.Owner.Id);
                    }
                }
            }
        }
        catch { }
    }

    Solid zone = null;
    try
    {
        var low = centre - new XYZ(envelope, envelope, envelope);
        var high = centre + new XYZ(envelope, envelope, envelope);
        var loop = CurveLoop.Create(new List<Curve>
        {
            Line.CreateBound(new XYZ(low.X, low.Y, low.Z), new XYZ(high.X, low.Y, low.Z)),
            Line.CreateBound(new XYZ(high.X, low.Y, low.Z), new XYZ(high.X, high.Y, low.Z)),
            Line.CreateBound(new XYZ(high.X, high.Y, low.Z), new XYZ(low.X, high.Y, low.Z)),
            Line.CreateBound(new XYZ(low.X, high.Y, low.Z), new XYZ(low.X, low.Y, low.Z))
        });
        zone = GeometryCreationUtilities.CreateExtrusionGeometry(
            new List<CurveLoop> { loop }, XYZ.BasisZ, high.Z - low.Z);
    }
    catch { }

    if (zone == null)
    {
        verdicts.Add("ROOM: could not build the envelope");
    }
    else
    {
        var intruders = new List<string>();
        foreach (var found in new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WherePasses(new ElementIntersectsSolidFilter(zone)))
        {
            if (ignore.Contains(found.Id) || found.Category == null) continue;
            if (intruders.Count < 6) intruders.Add(found.Category.Name + " " + found.Id);
        }

        if (intruders.Count == 0)
        {
            verdicts.Add("ROOM: clear");
        }
        else
        {
            obstructed.Add(accessory.Id);
            verdicts.Add("ROOM: BLOCKED by " + string.Join(", ", intruders));
        }
    }

    // ---- CEILING: cast a ray straight up. Not a fault - a panel needed.
    if (rayView == null)
    {
        verdicts.Add("CEILING: not asked, no 3D view");
    }
    else
    {
        try
        {
            var intersector = new ReferenceIntersector(
                new ElementCategoryFilter(BuiltInCategory.OST_Ceilings), FindReferenceTarget.Element, rayView);
            var hit = intersector.FindNearest(centre, XYZ.BasisZ);
            if (hit == null)
            {
                verdicts.Add("CEILING: none over it");
            }
            else
            {
                needAccessPanel.Add(accessory.Id);
                verdicts.Add(string.Format("CEILING: {0:0} mm above it - NEEDS AN ACCESS PANEL. Normal "
                    + "for a valve; it is the drawing item that gets forgotten",
                    hit.Proximity * 304.8));
            }
        }
        catch (Exception ex)
        {
            verdicts.Add("CEILING: could not be asked - " + ex.Message);
        }
    }

    // ---- HEIGHT above its own level.
    var levelId = accessory.LevelId;
    var level = levelId == ElementId.InvalidElementId ? null : doc.GetElement(levelId) as Level;
    if (level == null)
    {
        verdicts.Add("HEIGHT: no level, not measured");
    }
    else
    {
        var above = centre.Z - level.ProjectElevation;
        if (above > maxReachHeight)
        {
            outOfReach.Add(accessory.Id);
            verdicts.Add(string.Format("HEIGHT: {0:0} mm above {1} - OUT OF REACH. Needs a platform, a "
                + "chain wheel, or relocating", above * 304.8, level.Name));
        }
        else
        {
            verdicts.Add(string.Format("HEIGHT: {0:0} mm above {1} - reachable", above * 304.8, level.Name));
        }
    }

    findings.Add(string.Format("{0} (id {1}) - {2}", accessory.Name, accessory.Id,
        string.Join("  |  ", verdicts)));
}

findings.Insert(0, string.Format("{0} accessory(ies): {1} obstructed, {2} needing an access panel, {3} "
    + "out of reach. LINKED models were NOT scanned - if the architecture is a link, the ceiling and "
    + "room questions have not seen it",
    accessories.Count, obstructed.Count, needAccessPanel.Count, outOfReach.Count));
