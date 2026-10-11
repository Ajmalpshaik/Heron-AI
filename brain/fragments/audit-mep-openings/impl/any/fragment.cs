// NOT STANDALONE. Assumes `doc`, `openings` and `includeLinks` are in scope;
// leaves `findings`, `stale`, `combined`, `unhosted`, `linksSearched` and
// `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and changes nothing, here or in any link.
//
// THE TWO KINDS OF "OPENING" ARE NOT THE SAME KIND OF ELEMENT, and this is the
// trap the fragment is built around:
//   * a cut void is a Revit `Opening`. It has NO SOLID - only a boundary and a
//     host. Asking it for geometry returns nothing usable.
//   * a placed sleeve is a FamilyInstance, which does have solids.
// The obvious way to gather openings returns mostly the FIRST kind, so an audit
// written for the second reports "no geometry" for every one: no crash, no
// error, and an audit that audited nothing. Both are handled, and each row says
// which route was used.
//
// THE EXTENT IS CROSS-CHECKED. An Opening's boundary and its bounding box must
// agree; where they do not, the row reads SUSPECT rather than being audited on
// geometry that may be in the wrong place. The boundary's coordinate space is
// not stated in the documentation, so it is verified rather than assumed.
//
// STRUCTURE IN A LINK IS SCANNED ONLY WHEN ASKED FOR - D-59. On a normal
// coordination job the walls and slabs are a link, so a clean UNHOSTED count
// with `includeLinks` absent has checked nothing. With it set, each opening's
// centre is looked for inside the linked walls, floors, roofs, ceilings and
// structural members, and what it sits in is reported as TEXT in
// `linkedMatches` - the element's type and its Fire Rating, which is the
// question a penetration through a linked wall actually asks. `stale`,
// `combined` and `unhosted` stay this model's own: they feed the next step, and
// the chain revives ids against the HOST document, so a linked id carried there
// binds an unrelated element silently (FRAGMENT-ISSUES row 75).
//
// "SITS INSIDE" IS A BOX TEST, IN THE LINK'S OWN SPACE. The opening's centre is
// taken into each placement's coordinates and tested against the linked
// element's bounding box. A wall running on the diagonal has a loose box, so a
// hole near a corner can name the neighbouring wall too - every candidate is
// listed rather than one picked. Each PLACEMENT of a link is tested, because a
// file placed twice is two sets of walls in this model.
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule. Only model elements
// are read from a link, never its views or sheets.

var findings = new List<string>();
var stale = new List<ElementId>();
var combined = new List<ElementId>();
var unhosted = new List<ElementId>();

var SERVICE_CATEGORIES = new[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_DuctCurves,
                                 BuiltInCategory.OST_CableTray, BuiltInCategory.OST_Conduit };

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var unhostedInLink = 0;

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

// What an opening can be cut through. Model elements only - never a view.
var LINKED_HOST_CATEGORIES = new List<BuiltInCategory> { BuiltInCategory.OST_Walls,
    BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Ceilings,
    BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralColumns };

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

// Every linked host element whose box holds this point, as text: the link, the
// category, 'Family: Type', its id IN THE LINK, and its Fire Rating.
Func<XYZ, List<string>> linkedHostsAt = point =>
{
    var hits = new List<string>();
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var seen = new HashSet<ElementId>();
        foreach (var placement in linkPlacements[i])
        {
            try
            {
                var local = placement.GetTotalTransform().Inverse.OfPoint(point);
                foreach (var element in new FilteredElementCollector(linkDocs[i])
                             .WhereElementIsNotElementType()
                             .WherePasses(new ElementMulticategoryFilter(LINKED_HOST_CATEGORIES))
                             .WherePasses(new BoundingBoxContainsPointFilter(local)))
                {
                    if (element == null || !seen.Add(element.Id)) continue;
                    var typeName = "";
                    try
                    {
                        var type = linkDocs[i].GetElement(element.GetTypeId()) as ElementType;
                        if (type != null)
                            typeName = string.IsNullOrEmpty(type.FamilyName)
                                ? type.Name : type.FamilyName + ": " + type.Name;
                    }
                    catch (Exception) { }
                    hits.Add(string.Format("{0} - {1} '{2}' (id {3} in the link), Fire Rating {4}",
                        linkDocs[i].Title,
                        element.Category == null ? "element" : element.Category.Name,
                        typeName, element.Id, fireRatingOf(element)));
                }
            }
            catch (Exception) { }
        }
    }
    return hits;
};

var services = new List<KeyValuePair<Element, BoundingBoxXYZ>>();
foreach (var category in SERVICE_CATEGORIES)
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(category).WhereElementIsNotElementType())
    {
        var box = element.get_BoundingBox(null);
        if (box != null) services.Add(new KeyValuePair<Element, BoundingBoxXYZ>(element, box));
    }
}

foreach (var opening in openings)
{
    if (opening == null || !opening.IsValidObject) continue;

    var asOpening = opening as Opening;
    var route = asOpening != null ? "cut void" : "placed family";

    var box = opening.get_BoundingBox(null);
    if (box == null)
    {
        findings.Add(string.Format("opening {0} ({1}): no readable extent - not audited",
            opening.Id, route));
        continue;
    }

    // A cut void's boundary is cross-checked against its box. See the header.
    var suspect = false;
    if (asOpening != null)
    {
        try
        {
            var rect = asOpening.BoundaryRect;
            if (rect != null && rect.Count >= 2)
            {
                var low = rect[0];
                var high = rect[1];
                var boundaryWidth = Math.Abs(high.X - low.X);
                var boxWidth = box.Max.X - box.Min.X;
                // An order-of-magnitude disagreement means the boundary is not
                // in the space this assumed.
                if (boundaryWidth > 0 && boxWidth > 0
                    && (boundaryWidth > boxWidth * 10 || boxWidth > boundaryWidth * 10))
                {
                    suspect = true;
                }
            }
        }
        catch { }
    }

    // Hosting. Answered against the HOST model only.
    var hosted = true;
    if (asOpening != null)
    {
        try { hosted = asOpening.Host != null; }
        catch { hosted = false; }
    }
    else
    {
        var instance = opening as FamilyInstance;
        if (instance != null)
        {
            try { hosted = instance.Host != null; }
            catch { hosted = false; }
        }
    }

    if (!hosted)
    {
        unhosted.Add(opening.Id);
    }

    // What it sits in, in the links. Text only - see the header.
    if (linkDocs.Count > 0)
    {
        var centre = (box.Min + box.Max) / 2.0;
        var inLinks = linkedHostsAt(centre);
        if (inLinks.Count > 0)
        {
            linkedTotal++;
            if (!hosted) unhostedInLink++;
            linkedMatches.Add(string.Format("opening {0}: inside {1}", opening.Id,
                string.Join("; ", inLinks)));
        }
        else
        {
            linkedMatches.Add(string.Format("opening {0}: inside nothing in any link{1}", opening.Id,
                hosted ? "" : " - and hosted by nothing here either"));
        }
    }

    // What passes through it.
    var through = new List<string>();
    foreach (var pair in services)
    {
        var serviceBox = pair.Value;
        if (serviceBox.Max.X < box.Min.X || serviceBox.Min.X > box.Max.X) continue;
        if (serviceBox.Max.Y < box.Min.Y || serviceBox.Min.Y > box.Max.Y) continue;
        if (serviceBox.Max.Z < box.Min.Z || serviceBox.Min.Z > box.Max.Z) continue;
        if (through.Count < 6)
        {
            through.Add(string.Format("{0} {1}",
                pair.Key.Category == null ? "service" : pair.Key.Category.Name, pair.Key.Id));
        }
        else
        {
            through.Add("...");
            break;
        }
    }

    var status = new List<string>();
    if (suspect) status.Add("SUSPECT GEOMETRY - its boundary and its extent disagree, so the rest of "
        + "this row is not trustworthy");
    if (!hosted) status.Add("UNHOSTED - it is not inside anything in the HOST model. If the structure "
        + "is a link, that is expected and this question has not really been asked");

    if (through.Count == 0)
    {
        stale.Add(opening.Id);
        status.Add("STALE - nothing runs through it any more. Either the run moved and the hole did "
            + "not, or it was never needed");
    }
    else if (through.Count > 1)
    {
        combined.Add(opening.Id);
        status.Add(string.Format("COMBINED - {0} services share it: {1}. Sometimes intended, and worth "
            + "confirming with the structural engineer", through.Count, string.Join(", ", through)));
    }
    else
    {
        status.Add("OK - " + through[0] + " through it");
    }

    findings.Add(string.Format("opening {0} ({1}): {2}", opening.Id, route, string.Join("; ", status)));
}

findings.Insert(0, string.Format("{0} opening(s) audited against {1} service(s) in the host model: {2} "
    + "stale, {3} shared by more than one service, {4} unhosted. {5}",
    openings.Count, services.Count, stale.Count, combined.Count, unhosted.Count,
    linksSearched > 0
        ? "STRUCTURE IN LINKS WAS READ - see linkedMatches"
        : "STRUCTURE IN LINKS WAS NOT SCANNED"));

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} of {2} opening(s) sit inside a "
        + "linked wall, floor, roof, ceiling or structural member ({3} of the {4} unhosted here "
        + "among them), NOT selected", linksSearched, linkedTotal, openings.Count,
        unhostedInLink, unhosted.Count));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are named here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
