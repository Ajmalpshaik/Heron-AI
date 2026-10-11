// NOT STANDALONE. Assumes `doc`, `devices`, `tolerance` and `includeLinks` are
// in scope; leaves `findings`, `outOfPlane`, `noCeilingAbove`, `linksSearched`
// and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none. Distances are internal FEET.
//
// THE FAULT THIS CATCHES IS INVISIBLE IN PLAN. A diffuser 40 mm above the
// ceiling and one exactly in it look identical from above. It shows up as a site
// query, or on a section nobody cut.
//
// THE CEILING IS FOUND BY CASTING A RAY UPWARD. A device placed unhosted is not
// "hosted by" the ceiling in any readable way, and a room cannot tell you its
// own ceiling. The ray needs a real View3D; no 3D view at all is REPORTED, not
// returned as zero findings.
//
// "NO CEILING ABOVE" IS A FINDING, NOT AN ERROR - an open soffit is fine.
// Counted separately, because a missing ceiling and a misplaced device look the
// same in every other check.
//
// THE RAY GOES UP FROM THE INSERTION POINT. For a family whose insertion point
// is off its own centre the ray can miss the ceiling beside it, so a large NO
// CEILING count in an area known to have one is usually that. Said in the
// report rather than left to read as a hundred real faults.
//
// THE CEILINGS ARE USUALLY THE ARCHITECT'S, IN A LINK, AND THEY ARE READ ONLY
// WHEN ASKED FOR - D-59. With `includeLinks` set, a second ray is cast with
// Revit's own FindReferencesInRevitLinks, which looks through each link as it
// is placed, and every device whose nearest ceiling above is a LINKED one gets a
// line in `linkedMatches`: the gap, the verdict against the same tolerance, and
// the ceiling's type and id IN THE LINK. `outOfPlane` and `noCeilingAbove` stay
// this model's answer, exactly as its proof measured - the linked verdict is
// TEXT, so nothing from a link reaches the next step (FRAGMENT-ISSUES row 75).
// THE RAY SEES A LINK ONLY WHERE THE 3D VIEW SHOWS IT: a link hidden in that
// view is read as no ceiling, and the answer names the view so that can be
// checked. NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model
// elements are read from a link, never its views or sheets.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
tolerance = tolerance / MillimetresPerFoot;

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
// TWO PARAMETERS BY THAT NAME where the lookup lands - a shared or project
// "Fire Rating" bound beside the built-in one - read "NOT READ" with the
// count, never either value: asked by name, Revit returns "the first one
// encountered", which its own reference says "is determined at random"
// (D-54 s3, FRAGMENT-ISSUES 5b-203). The guard check-sleeve-size carries.
Func<Element, string> fireRatingOf = element =>
{
    Element type = null;
    try { type = element.Document.GetElement(element.GetTypeId()); } catch (Exception) { }
    foreach (var source in new[] { element, type })
    {
        if (source == null) continue;
        // Two by this name where the lookup lands: said, never read (5b-203).
        var sharing = 0;
        try { sharing = source.GetParameters("Fire Rating").Count; } catch (Exception) { }
        if (sharing > 1) return "NOT READ - " + sharing + " parameters share that name";
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
var linkAt = 0;
var linkOff = 0;
var linkNone = 0;

var findings = new List<string>();
var outOfPlane = new List<ElementId>();
var noCeilingAbove = new List<ElementId>();

View3D rayView = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
{
    var candidate = element as View3D;
    if (candidate != null && !candidate.IsTemplate) { rayView = candidate; break; }
}

var hostCeilings = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType().GetElementCount();

if (rayView == null)
{
    findings.Add("no 3D view in this model, so no ray could be cast and NOTHING was checked. That is "
        + "different from every device passing");
}
else
{
    if (hostCeilings == 0)
    {
        findings.Add("no ceilings in the HOST model. If the architecture is a LINK, every device below "
            + "will read as having no ceiling above it and none of that is a real finding");
    }

    var intersector = new ReferenceIntersector(
        new ElementCategoryFilter(BuiltInCategory.OST_Ceilings), FindReferenceTarget.Element, rayView);

    // The same ray, looking through the links as they are placed. Only built
    // when asked for - see the header.
    ReferenceIntersector linkIntersector = null;
    if (linkDocs.Count > 0)
    {
        linkIntersector = new ReferenceIntersector(
            new ElementCategoryFilter(BuiltInCategory.OST_Ceilings), FindReferenceTarget.Element, rayView);
        linkIntersector.FindReferencesInRevitLinks = true;
    }

    foreach (var device in devices)
    {
        if (device == null || !device.IsValidObject) continue;

        XYZ point = null;
        var location = device.Location as LocationPoint;
        if (location != null) point = location.Point;
        if (point == null)
        {
            var box = device.get_BoundingBox(null);
            if (box != null) point = (box.Min + box.Max) / 2.0;
        }
        if (point == null)
        {
            findings.Add(string.Format("{0} (id {1}): no readable position - not checked",
                device.Name, device.Id));
            continue;
        }

        ReferenceWithContext hit = null;
        try { hit = intersector.FindNearest(point, XYZ.BasisZ); }
        catch { }

        // The linked ceiling above, if the nearest one is in a link. Text only.
        if (linkIntersector != null)
        {
            ReferenceWithContext linkHit = null;
            try { linkHit = linkIntersector.FindNearest(point, XYZ.BasisZ); } catch { }
            Reference reference = null;
            try { reference = linkHit == null ? null : linkHit.GetReference(); } catch { }
            if (reference == null || reference.LinkedElementId == ElementId.InvalidElementId)
            {
                if (linkHit == null) linkNone++;
            }
            else
            {
                var placement = doc.GetElement(reference.ElementId) as RevitLinkInstance;
                Document linked = null;
                try { linked = placement == null ? null : placement.GetLinkDocument(); } catch { }
                var ceiling = linked == null ? null : linked.GetElement(reference.LinkedElementId);
                var linkGap = linkHit.Proximity;
                var atCeiling = linkGap <= tolerance;
                if (atCeiling) linkAt++; else linkOff++;
                linkedMatches.Add(string.Format("  {0} (id {1}): {2:0.#} mm below {3} - {4}",
                    device.Name, device.Id, linkGap * 304.8,
                    ceiling == null ? "a linked ceiling" : linked.Title + " - " + describeLinked(linked, ceiling),
                    atCeiling ? "AT the ceiling" : string.Format("past the {0:0.#} mm allowed", tolerance * 304.8)));
            }
        }

        if (hit == null)
        {
            noCeilingAbove.Add(device.Id);
            findings.Add(string.Format("{0} (id {1}): NO CEILING above it. Fine in an open soffit; "
                + "otherwise the device is in the wrong place or the ceiling is missing",
                device.Name, device.Id));
            continue;
        }

        var gap = hit.Proximity;
        if (gap <= tolerance)
        {
            findings.Add(string.Format("{0} (id {1}): AT the ceiling, {2:0.#} mm below it",
                device.Name, device.Id, gap * 304.8));
        }
        else
        {
            outOfPlane.Add(device.Id);
            findings.Add(string.Format("{0} (id {1}): {2:0.#} mm BELOW the ceiling - past the {3:0.#} mm "
                + "allowed. Invisible in plan; it shows up on site",
                device.Name, device.Id, gap * 304.8, tolerance * 304.8));
        }
    }

    findings.Insert(0, string.Format("{0} device(s) checked against {1} host-model ceiling(s): {2} out "
        + "of plane, {3} with no ceiling above. A large NO CEILING count where you know there is one "
        + "usually means the family's insertion point sits off its own centre and the ray missed - "
        + "check one in a section before believing it",
        devices.Count, hostCeilings, outOfPlane.Count, noCeilingAbove.Count));
}

if (linkDocs.Count > 0 && rayView == null)
    linkBlocked = "Links NOT read: there is no 3D view to cast the ray in - see findings";
else if (linkDocs.Count > 0)
    linkedMatches.Insert(0, string.Format("Ray cast in the 3D view '{0}': {1} device(s) AT a linked "
        + "ceiling, {2} past the allowance below one, {3} with no ceiling above in this model or any "
        + "link it shows. A link hidden in that view is not seen", rayView.Name, linkAt, linkOff, linkNone));
linkedTotal = linkAt + linkOff;

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} device(s) under a linked ceiling, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
