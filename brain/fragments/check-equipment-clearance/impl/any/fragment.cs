// NOT STANDALONE. Assumes `doc`, `equipment`, `frontClearance`,
// `sideClearance`, `backClearance`, `topClearance` and `includeLinks` are in
// scope; leaves `findings`, `blocked`, `unreadable`, `linksSearched` and
// `linkedMatches` behind.
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
// LINKED MODELS ARE SCANNED ONLY WHEN ASKED FOR - D-59. Structure and
// architecture usually live in links and are exactly what blocks access, so a
// clean result with `includeLinks` absent has checked nothing, and the
// host-model candidate count is reported for that reason. With it set, each
// unit's zone is carried into every placement of every loaded link and tested
// against that link's elements with the same solid test; rooms, spaces and
// areas are not obstructions and are left out. Every unit something in a link
// blocks gets a line in `linkedMatches` naming the linked elements, their type,
// id IN THE LINK and Fire Rating. `blocked` stays this model's answer, exactly
// as its proof measured - the linked verdict is TEXT, so nothing from a link
// reaches the next step (FRAGMENT-ISSUES row 75). NESTED LINKS ARE NOT READ,
// AND THE ANSWER COUNTS THEM. Only model elements are read from a link, never
// its views or sheets.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
frontClearance = frontClearance / MillimetresPerFoot;
sideClearance = sideClearance / MillimetresPerFoot;
backClearance = backClearance / MillimetresPerFoot;
topClearance = topClearance / MillimetresPerFoot;

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
    linksSearched = linkDocs.Count;
}


// Fire Rating by the name Revit's own walls, floors and doors carry it under,
// on the element first and then its type. Read as the palette shows it. A
// family that calls it something else reads "not set" - said, never guessed.
Func<Element, string> fireRatingOf = element =>
{
    Element type = null;
    try { type = element.Document.GetElement(element.GetTypeId()); } catch (Exception) { }
    foreach (var source in new[] { element, type })
    {
        if (source == null) continue;
        Parameter parameter = null;
        try { parameter = source.LookupParameter("Fire Rating"); } catch (Exception) { }
        if (parameter == null || !parameter.HasValue) continue;
        string text = null;
        try
        {
            text = parameter.StorageType == StorageType.String
                ? parameter.AsString() : parameter.AsValueString();
        }
        catch (Exception) { }
        if (!string.IsNullOrEmpty(text)) return text;
    }
    return "not set";
};

// A linked element as text: the category, 'Family: Type', its id IN THE LINK
// (never this model's), and its Fire Rating.
Func<Document, Element, string> describeLinked = (linked, element) =>
{
    var typeName = "";
    try
    {
        var type = linked.GetElement(element.GetTypeId()) as ElementType;
        if (type != null)
            typeName = string.IsNullOrEmpty(type.FamilyName)
                ? type.Name : type.FamilyName + ": " + type.Name;
    }
    catch (Exception) { }
    return string.Format("{0} '{1}' (id {2} in the link), Fire Rating {3}",
        element.Category == null ? "element" : element.Category.Name,
        typeName, element.Id, fireRatingOf(element));
};

var linkBlocked = "";
var notObstructions = new List<ElementId> { new ElementId(BuiltInCategory.OST_Rooms),
    new ElementId(BuiltInCategory.OST_MEPSpaces), new ElementId(BuiltInCategory.OST_Areas) };

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

    // The same zone, in each link. Text only - see the header.
    var linkIntruders = new List<string>();
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var seen = new HashSet<ElementId>();
        foreach (var placement in linkPlacements[i])
        {
            try
            {
                var moved = SolidUtils.CreateTransformed(zone, placement.GetTotalTransform().Inverse);
                foreach (var found in new FilteredElementCollector(linkDocs[i])
                    .WhereElementIsNotElementType()
                    .WherePasses(new ElementIntersectsSolidFilter(moved)))
                {
                    if (found.Category == null || notObstructions.Contains(found.Category.Id)) continue;
                    if (!seen.Add(found.Id)) continue;
                    if (linkIntruders.Count < 12)
                        linkIntruders.Add(linkDocs[i].Title + " - " + describeLinked(linkDocs[i], found));
                }
            }
            catch (Exception) { }
        }
    }
    if (linkIntruders.Count > 0)
    {
        linkedTotal++;
        linkedMatches.Add(string.Format("  {0} (id {1}): {2} linked element(s) inside the access zone - {3}",
            unit.Name, unit.Id, linkIntruders.Count, string.Join("; ", linkIntruders)));
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
    + "{3} unreadable. {4}",
    equipment.Count, hostCandidates, blocked.Count, unreadable,
    linksSearched > 0
        ? "LINKED models were read too - see linkedMatches"
        : "LINKED models were NOT scanned - if the structure and architecture are links, a clear result "
            + "here has not seen what usually blocks access"));
findings.Insert(1, "CHECK THE REPORTED FACING DIRECTION on one unit before trusting the rest. If the "
    + "front zone points into the wall, the family was authored facing the wrong way and every result "
    + "below is confidently wrong");

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} unit(s) with something in a link inside the access zone, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
