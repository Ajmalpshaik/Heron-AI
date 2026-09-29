// NOT STANDALONE. Assumes `doc`, `accessories`, `envelope`, `maxReachHeight`
// and `includeLinks` are in scope; leaves `findings`, `obstructed`,
// `needAccessPanel`, `outOfReach`, `linksSearched` and `linkedMatches` behind.
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
//
// THE ARCHITECTURE IS USUALLY A LINK, AND IT IS READ ONLY WHEN ASKED FOR - D-59.
// With `includeLinks` set, the ROOM and CEILING questions are asked of every
// loaded link too: the operating envelope is carried into each placement and
// tested against that link's elements (rooms, spaces and areas are space, not
// obstructions), and the upward ray is cast again with Revit's own
// FindReferencesInRevitLinks. Each accessory the links answer for gets a line
// in `linkedMatches` - the linked obstruction or ceiling, with its type, id IN
// THE LINK and Fire Rating. `obstructed`, `needAccessPanel` and `outOfReach`
// stay this model's answer, exactly as the proof measured - the linked verdict
// is TEXT, so nothing from a link reaches the next step (FRAGMENT-ISSUES row
// 75). THE RAY SEES A LINK ONLY WHERE THE 3D VIEW SHOWS IT. NESTED LINKS ARE
// NOT READ, AND THE ANSWER COUNTS THEM. Only model elements are read from a
// link, never its views or sheets.

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
var linkBlockedCount = 0;
var linkPanelCount = 0;
var notObstructions = new List<ElementId> { new ElementId(BuiltInCategory.OST_Rooms),
    new ElementId(BuiltInCategory.OST_MEPSpaces), new ElementId(BuiltInCategory.OST_Areas) };

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

    // ---- THE LINKS: the same two questions. Text only - see the header.
    if (linkDocs.Count > 0)
    {
        var linkSays = new List<string>();
        if (zone != null)
        {
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
                            if (linkIntruders.Count < 6)
                                linkIntruders.Add(linkDocs[i].Title + " - " + describeLinked(linkDocs[i], found));
                        }
                    }
                    catch (Exception) { }
                }
            }
            if (linkIntruders.Count > 0)
            {
                linkBlockedCount++;
                linkSays.Add("ROOM: BLOCKED by " + string.Join("; ", linkIntruders));
            }
        }
        if (rayView != null)
        {
            try
            {
                var linkIntersector = new ReferenceIntersector(
                    new ElementCategoryFilter(BuiltInCategory.OST_Ceilings), FindReferenceTarget.Element, rayView);
                linkIntersector.FindReferencesInRevitLinks = true;
                var linkHit = linkIntersector.FindNearest(centre, XYZ.BasisZ);
                var reference = linkHit == null ? null : linkHit.GetReference();
                if (reference != null && reference.LinkedElementId != ElementId.InvalidElementId)
                {
                    var placement = doc.GetElement(reference.ElementId) as RevitLinkInstance;
                    Document linked = null;
                    try { linked = placement == null ? null : placement.GetLinkDocument(); } catch { }
                    var ceiling = linked == null ? null : linked.GetElement(reference.LinkedElementId);
                    linkPanelCount++;
                    linkSays.Add(string.Format("CEILING: {0:0} mm above it, {1} - NEEDS AN ACCESS PANEL",
                        linkHit.Proximity * 304.8,
                        ceiling == null ? "a linked ceiling" : linked.Title + " - " + describeLinked(linked, ceiling)));
                }
            }
            catch (Exception) { }
        }
        if (linkSays.Count > 0)
            linkedMatches.Add(string.Format("  {0} (id {1}) - {2}", accessory.Name, accessory.Id,
                string.Join("  |  ", linkSays)));
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
    + "out of reach. {4}",
    accessories.Count, obstructed.Count, needAccessPanel.Count, outOfReach.Count,
    linkDocs.Count > 0
        ? "LINKED models were read too - see linkedMatches"
        : "LINKED models were NOT scanned - if the architecture is a link, the ceiling and room "
            + "questions have not seen it"));

if (linkDocs.Count > 0)
    linkedMatches.Insert(0, string.Format("{0} accessory(ies) blocked by something in a link, {1} under a "
        + "linked ceiling and needing an access panel{2}", linkBlockedCount, linkPanelCount,
        rayView == null ? ". The CEILING question was not asked - no 3D view"
            : string.Format(". Ceiling ray cast in the 3D view '{0}'; a link hidden there is not seen", rayView.Name)));
linkedTotal = linkBlockedCount + linkPanelCount;

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} linked finding(s), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
