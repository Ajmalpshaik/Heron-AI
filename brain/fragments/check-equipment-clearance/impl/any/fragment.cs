// NOT STANDALONE. Assumes `doc`, `equipment`, `frontClearance`,
// `sideClearance`, `backClearance` and `topClearance` are in scope; leaves
// `findings`, `blocked` and `unreadable` behind.
//
// READ ONLY. Opens no transaction and needs none. Clearances are internal FEET.
//
// THE ZONE IS DIRECTIONAL AND THAT IS THE WHOLE POINT. An air handling unit
// needs a metre and a half in FRONT for coil withdrawal and 200 mm behind. A
// sphere either passes real obstructions or fails on the wall the unit is meant
// to stand against.
//
// "FRONT" IS THE FAMILY'S OWN FACING DIRECTION. The zone is built on the
// instance transform so it rotates with the unit - and if the family was
// authored facing the wrong way, the front zone points into the wall and this
// check is confidently wrong. Every unit's facing direction is REPORTED so that
// is visible on the first run rather than never.
//
// THE UNIT'S OWN SERVICES ARE NOT OBSTRUCTIONS. Anything connected to it, and
// its own sub-components, are excluded - otherwise every unit reports its own
// pipework as blocking it and the check gets ignored.
//
// LINKED MODELS ARE NOT SCANNED. Structure and architecture usually live in
// links and are exactly what blocks access, so a clean result there has checked
// nothing. The host-model candidate count is reported for that reason.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
frontClearance = frontClearance / MillimetresPerFoot;
sideClearance = sideClearance / MillimetresPerFoot;
backClearance = backClearance / MillimetresPerFoot;
topClearance = topClearance / MillimetresPerFoot;

var findings = new List<string>();
var blocked = new List<ElementId>();
var unreadable = 0;

var hostCandidates = new FilteredElementCollector(doc)
    .WhereElementIsNotElementType().GetElementCount();

foreach (var unit in equipment)
{
    var instance = unit as FamilyInstance;
    if (instance == null)
    {
        unreadable++;
        findings.Add(string.Format("{0} (id {1}): not a placed family instance - it has no facing "
            + "direction, so no directional zone can be built", unit.Name, unit.Id));
        continue;
    }

    var box = unit.get_BoundingBox(null);
    if (box == null)
    {
        unreadable++;
        findings.Add(string.Format("{0} (id {1}): no readable geometry - not checked", unit.Name, unit.Id));
        continue;
    }

    var transform = instance.GetTransform();
    var facing = transform.BasisY;      // +Y is the family's facing direction
    var hand = transform.BasisX;
    var up = XYZ.BasisZ;

    // The unit's own half-extents, measured along its OWN axes rather than the
    // world box - a unit rotated off the project axes has a world box much
    // larger than itself, and a zone built on that starts outside the equipment
    // and misses exactly the obstructions that matter.
    var centre = (box.Min + box.Max) / 2.0;
    var corners = new List<XYZ>
    {
        new XYZ(box.Min.X, box.Min.Y, box.Min.Z), new XYZ(box.Max.X, box.Min.Y, box.Min.Z),
        new XYZ(box.Min.X, box.Max.Y, box.Min.Z), new XYZ(box.Max.X, box.Max.Y, box.Min.Z),
        new XYZ(box.Min.X, box.Min.Y, box.Max.Z), new XYZ(box.Max.X, box.Min.Y, box.Max.Z),
        new XYZ(box.Min.X, box.Max.Y, box.Max.Z), new XYZ(box.Max.X, box.Max.Y, box.Max.Z)
    };

    var halfFacing = 0.0;
    var halfHand = 0.0;
    var halfUp = 0.0;
    foreach (var corner in corners)
    {
        var offset = corner - centre;
        halfFacing = Math.Max(halfFacing, Math.Abs(offset.DotProduct(facing)));
        halfHand = Math.Max(halfHand, Math.Abs(offset.DotProduct(hand)));
        halfUp = Math.Max(halfUp, Math.Abs(offset.DotProduct(up)));
    }

    // The zone, in the unit's own frame: front and back differ, sides match,
    // and the top is grown. The base rectangle is built in plan and extruded.
    var front = halfFacing + frontClearance;
    var back = halfFacing + backClearance;
    var side = halfHand + sideClearance;
    var top = halfUp + topClearance;

    var baseZ = centre - (up * halfUp);
    var p1 = baseZ + (facing * front) + (hand * side);
    var p2 = baseZ + (facing * front) - (hand * side);
    var p3 = baseZ - (facing * back) - (hand * side);
    var p4 = baseZ - (facing * back) + (hand * side);

    Solid zone = null;
    try
    {
        var loop = CurveLoop.Create(new List<Curve>
        {
            Line.CreateBound(p1, p2), Line.CreateBound(p2, p3),
            Line.CreateBound(p3, p4), Line.CreateBound(p4, p1)
        });
        zone = GeometryCreationUtilities.CreateExtrusionGeometry(
            new List<CurveLoop> { loop }, up, halfUp + top);
    }
    catch (Exception ex)
    {
        unreadable++;
        findings.Add(string.Format("{0} (id {1}): could not build the clearance zone - {2}",
            unit.Name, unit.Id, ex.Message));
        continue;
    }

    // Everything to ignore: the unit itself, its own sub-components, and
    // whatever is plugged into it.
    var ignore = new HashSet<ElementId>();
    ignore.Add(unit.Id);
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

    var intruders = new List<string>();
    foreach (var found in new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .WherePasses(new ElementIntersectsSolidFilter(zone)))
    {
        if (ignore.Contains(found.Id)) continue;
        if (found.Category == null) continue;
        if (intruders.Count < 12)
        {
            intruders.Add(string.Format("{0} (id {1})", found.Category.Name, found.Id));
        }
    }

    var facingText = string.Format("faces ({0:0.##}, {1:0.##}, {2:0.##})", facing.X, facing.Y, facing.Z);

    if (intruders.Count == 0)
    {
        findings.Add(string.Format("{0} (id {1}, {2}): access zone CLEAR", unit.Name, unit.Id, facingText));
    }
    else
    {
        blocked.Add(unit.Id);
        findings.Add(string.Format("{0} (id {1}, {2}): {3} thing(s) inside the access zone - {4}",
            unit.Name, unit.Id, facingText, intruders.Count, string.Join(", ", intruders)));
    }
}

findings.Insert(0, string.Format("{0} unit(s) checked against {1} host-model element(s). {2} blocked, "
    + "{3} unreadable. LINKED models were NOT scanned - if the structure and architecture are links, a "
    + "clear result here has not seen what usually blocks access",
    equipment.Count, hostCandidates, blocked.Count, unreadable));
findings.Insert(1, "CHECK THE REPORTED FACING DIRECTION on one unit before trusting the rest. If the "
    + "front zone points into the wall, the family was authored facing the wrong way and every result "
    + "below is confidently wrong");
