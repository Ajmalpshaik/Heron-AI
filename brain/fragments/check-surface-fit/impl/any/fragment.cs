// NOT STANDALONE. Assumes `doc`, `elements`, `direction`, `maxDistance`,
// `evenness` and `includeLinks` are in scope; leaves `findings`,
// `unsafeToMove`, `clean`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none. Distances are internal FEET.
//
// A SINGLE CENTRE RAY IS RIGHT IN THE MIDDLE OF A LARGE FLAT SURFACE AND LIES
// AT THE EDGES - and edges are where the mistakes are. Same arithmetic, five
// sample points, and four named ways one ray gets it wrong:
//   STRADDLING  - corners hit DIFFERENT elements: it spans an edge
//   OVERHANGING - some corners hit nothing: it hangs off
//   UNEVEN      - same element, different distances
//   SLOPED      - the surface is not square to the ray, so nothing sits flush
//
// IT IS THE CHECK BEFORE A BATCH MOVE. A move that includes a straddling unit
// puts it in the wrong place confidently.
//
// REPORTS BY EXCEPTION. Ray count is cheap; output is not. A clean run over
// thousands should print a summary line and nothing else.
//
// LINKED MODELS ARE HIT ONLY WHEN ASKED FOR - D-59. Without `includeLinks`,
// if the ceilings and slabs are a link, everything reports as finding nothing,
// which looks exactly like a real finding. With it set, the same five rays are
// cast again with Revit's own FindReferencesInRevitLinks, and every element
// whose rays land on a LINKED surface gets its own verdict in `linkedMatches` -
// the same four names, and the linked element's type, id IN THE LINK and Fire
// Rating. `unsafeToMove` and `clean` stay this model's answer, exactly as the
// proof measured: `unsafeToMove` feeds the move, and the linked verdict is TEXT
// so nothing from a link reaches the next step (FRAGMENT-ISSUES row 75). THE
// RAY SEES A LINK ONLY WHERE THE 3D VIEW SHOWS IT, and the answer names the
// view. NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model
// elements are read from a link, never its views or sheets.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
maxDistance = maxDistance / MillimetresPerFoot;

// AND `evenness` IS A LENGTH TOO, which the first pass of this block missed.
// It is compared against `spread`, and `spread` is the difference between two
// ReferenceIntersector distances - Revit's own units, feet. So an unconverted
// `evenness` of 5 meant FIVE FEET, a flatness tolerance 304.8x looser than the
// 5 mm the caller typed, and a surface a metre and a half out of true reported
// as sitting flush. The giveaway was in this file the whole time: the message
// it prints does `spread * 304.8` to turn the same number into millimetres.
evenness = evenness / MillimetresPerFoot;

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
var linkFlat = 0;
var linkNot = 0;

var findings = new List<string>();
var unsafeToMove = new List<ElementId>();
var clean = 0;

View3D rayView = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
{
    var candidate = element as View3D;
    if (candidate != null && !candidate.IsTemplate) { rayView = candidate; break; }
}

if (rayView == null)
{
    findings.Add("no 3D view in this model, so no ray could be cast and NOTHING was checked. That is "
        + "not the same as every element passing");
}
else
{
    // THE TWO-ARGUMENT CONSTRUCTOR DOES NOT EXIST ON ANY RELEASE HERE. This
    // read `new ReferenceIntersector(FindReferenceTarget.Element, rayView)`
    // until 2026-09-05, and failed to compile on all EIGHT - it is not a
    // version split, it is a signature Revit has never shipped. The
    // filter-first overload is the one that exists, so a filter has to be
    // given; an inverted ElementIsElementTypeFilter passes every placed
    // instance and no types, which is what "anything behind it" means.
    var intersector = new ReferenceIntersector(
        new ElementIsElementTypeFilter(true), FindReferenceTarget.Element, rayView);
    var unit = direction.Normalize();

    // The same rays through the links as they are placed. See the header.
    ReferenceIntersector linkIntersector = null;
    if (linkDocs.Count > 0)
    {
        linkIntersector = new ReferenceIntersector(
            new ElementIsElementTypeFilter(true), FindReferenceTarget.Element, rayView);
        linkIntersector.FindReferencesInRevitLinks = true;
    }

    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        var box = element.get_BoundingBox(null);
        if (box == null)
        {
            findings.Add(string.Format("{0} (id {1}): no readable extent - not checked",
                element.Name, element.Id));
            continue;
        }

        // Five sample points across the footprint: centre and four corners,
        // pulled in slightly so a corner exactly on an edge does not decide the
        // verdict by a rounding error.
        var centre = (box.Min + box.Max) / 2.0;
        var insetX = (box.Max.X - box.Min.X) * 0.45;
        var insetY = (box.Max.Y - box.Min.Y) * 0.45;

        var samples = new List<XYZ>
        {
            centre,
            new XYZ(centre.X - insetX, centre.Y - insetY, centre.Z),
            new XYZ(centre.X + insetX, centre.Y - insetY, centre.Z),
            new XYZ(centre.X + insetX, centre.Y + insetY, centre.Z),
            new XYZ(centre.X - insetX, centre.Y + insetY, centre.Z)
        };

        // THE LINKED VERDICT, from the same five points. Counted only when at
        // least one ray lands on a linked element - text, never a result.
        if (linkIntersector != null)
        {
            var linkHits = new List<string>();
            var linkDistances = new List<double>();
            var linkMissed = 0;
            var linkedAny = false;
            string firstLinked = null;
            foreach (var point in samples)
            {
                ReferenceWithContext found = null;
                try { found = linkIntersector.FindNearest(point, unit); } catch { }
                if (found == null || found.Proximity > maxDistance) { linkMissed++; continue; }
                Reference reference = null;
                try { reference = found.GetReference(); } catch { }
                if (reference == null) { linkMissed++; continue; }
                linkHits.Add(reference.ElementId + ":" + reference.LinkedElementId);
                linkDistances.Add(found.Proximity);
                if (reference.LinkedElementId != ElementId.InvalidElementId)
                {
                    linkedAny = true;
                    if (firstLinked == null)
                    {
                        var placement = doc.GetElement(reference.ElementId) as RevitLinkInstance;
                        Document linked = null;
                        try { linked = placement == null ? null : placement.GetLinkDocument(); } catch { }
                        var surface = linked == null ? null : linked.GetElement(reference.LinkedElementId);
                        firstLinked = surface == null ? "a linked element"
                            : linked.Title + " - " + describeLinked(linked, surface);
                    }
                }
            }
            if (linkedAny)
            {
                var linkVerdicts = new List<string>();
                if (linkMissed > 0) linkVerdicts.Add(string.Format("OVERHANGING - {0} of {1} sample points "
                    + "hit nothing", linkMissed, samples.Count));
                var linkDistinct = new HashSet<string>(linkHits);
                if (linkDistinct.Count > 1) linkVerdicts.Add(string.Format("STRADDLING - the corners land on "
                    + "{0} different elements", linkDistinct.Count));
                if (linkDistances.Count > 1)
                {
                    var spread = linkDistances.Max() - linkDistances.Min();
                    if (spread > evenness) linkVerdicts.Add(string.Format("UNEVEN - {0:0.#} mm further away "
                        + "at one corner than another", spread * 304.8));
                }
                if (linkVerdicts.Count == 0) linkFlat++; else linkNot++;
                linkedMatches.Add(string.Format("  {0} (id {1}): against {2} - {3}", element.Name,
                    element.Id, firstLinked,
                    linkVerdicts.Count == 0 ? "sits flat" : string.Join("; ", linkVerdicts)));
            }
        }

        var hitIds = new List<ElementId>();
        var distances = new List<double>();
        var missed = 0;

        foreach (var point in samples)
        {
            ReferenceWithContext found = null;
            try { found = intersector.FindNearest(point, unit); }
            catch { }

            if (found == null || found.Proximity > maxDistance) { missed++; continue; }

            var reference = found.GetReference();
            if (reference != null) hitIds.Add(reference.ElementId);
            distances.Add(found.Proximity);
        }

        var verdicts = new List<string>();

        if (missed == samples.Count)
        {
            findings.Add(string.Format("{0} (id {1}): NOTHING within reach in that direction",
                element.Name, element.Id));
            unsafeToMove.Add(element.Id);
            continue;
        }

        if (missed > 0) verdicts.Add(string.Format("OVERHANGING - {0} of {1} sample points hit nothing",
            missed, samples.Count));

        // Different elements under different corners: it spans an edge.
        var distinct = new List<ElementId>();
        foreach (var id in hitIds)
        {
            var seen = false;
            foreach (var known in distinct) { if (known == id) { seen = true; break; } }
            if (!seen) distinct.Add(id);
        }
        if (distinct.Count > 1)
        {
            verdicts.Add(string.Format("STRADDLING - the corners land on {0} different elements",
                distinct.Count));
        }

        if (distances.Count > 1)
        {
            var lowest = distances[0];
            var highest = distances[0];
            foreach (var value in distances)
            {
                if (value < lowest) lowest = value;
                if (value > highest) highest = value;
            }
            var spread = highest - lowest;
            if (spread > evenness)
            {
                // One surface at different distances across the footprint is
                // either a slope or a step; both mean it cannot sit flush.
                verdicts.Add(string.Format("{0} - the surface is {1:0.#} mm further away at one corner "
                    + "than another", distinct.Count == 1 ? "SLOPED or UNEVEN" : "UNEVEN",
                    spread * 304.8));
            }
        }

        if (verdicts.Count == 0)
        {
            clean++;
        }
        else
        {
            unsafeToMove.Add(element.Id);
            findings.Add(string.Format("{0} (id {1}): {2}", element.Name, element.Id,
                string.Join("; ", verdicts)));
        }
    }

    findings.Insert(0, string.Format("{0} element(s): {1} sit flat, {2} do NOT and should not be moved "
        + "automatically. Reported BY EXCEPTION - anything not listed is clean. {3}",
        elements.Count, clean, unsafeToMove.Count,
        linkDocs.Count > 0
            ? "LINKED models were hit too - see linkedMatches"
            : "LINKED models were not hit, so if the ceilings and slabs are a link this has checked nothing"));
}

if (linkDocs.Count > 0 && rayView == null)
    linkBlocked = "Links NOT read: there is no 3D view to cast the rays in - see findings";
else if (linkDocs.Count > 0)
    linkedMatches.Insert(0, string.Format("Rays cast in the 3D view '{0}': {1} element(s) sit flat "
        + "against a linked surface, {2} do not. An element whose rays reach nothing linked has no line. "
        + "A link hidden in that view is not seen", rayView.Name, linkFlat, linkNot));
linkedTotal = linkFlat + linkNot;

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} element(s) against a linked surface, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
