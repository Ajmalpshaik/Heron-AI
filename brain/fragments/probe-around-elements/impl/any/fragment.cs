// NOT STANDALONE. Assumes `doc`, `view3d`, `elements`, `directions`,
// `maxDistance` and `includeLinks` are in scope, and leaves `hits`,
// `nothingHit`, `noOrigin`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE BIGGEST TRAP: A RAY SEES ONLY WHAT THE 3D VIEW SHOWS.
//
// Ray casting runs inside a 3D view and obeys it completely - hidden
// categories, section boxes, view filters, closed worksets. A view with walls
// switched off reports a clear path with a wall standing in it, silently.
// Proven in a real model: same element, same direction, 0 neighbours in one
// view and 4 in another. That is why the view is a request input and the first
// thing to check when a result looks impossibly empty.
//
// THE DISTANCE IS FROM THE ELEMENT'S POINT, NOT ITS FACE.
//
// The ray starts at the insertion point, inside the element, so a 600 mm deep
// unit reports about 300 mm less clearance than a tape would. Reading it as
// face to face is how a clearance passes on paper and fails on site.
//
// ONE RAY PER DIRECTION MISSES WHAT IS BESIDE A LARGE ELEMENT.
//
// It sees only what is directly in line with the centre: a pipe passing the
// corner of an air handling unit is invisible to it.
//
// LINKS ARE PROBED ONLY WHEN ASKED FOR - D-59. The first ray keeps
// FindReferencesInRevitLinks off, so `hits` and `nothingHit` are exactly what
// the proof measured. With `includeLinks` set, each direction is cast a second
// time with it on, and where the nearest thing within reach is in a LINK - the
// architect's wall, the structural beam - it gets a line in `linkedMatches`
// with the linked element's type, id IN THE LINK, Fire Rating and distance.
// Where something in this model is nearer, the host answer already has it and
// no linked line is written. Nothing from a link enters `hits` (FRAGMENT-ISSUES
// row 75). THE SAME VIEW RULE HOLDS: a link hidden in the view is not seen.
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model elements
// are read from a link, never its views or sheets.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
maxDistance = maxDistance / MillimetresPerFoot;

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
ReferenceIntersector linkIntersector = null;
if (linkDocs.Count > 0 && view3d != null)
{
    linkIntersector = new ReferenceIntersector(view3d);
    linkIntersector.FindReferencesInRevitLinks = true;
    linkIntersector.TargetType = FindReferenceTarget.Element;
}

var hits = new Dictionary<ElementId, IList<string>>();
var nothingHit = new List<ElementId>();
var noOrigin = new List<ElementId>();

var intersector = new ReferenceIntersector(view3d);
intersector.FindReferencesInRevitLinks = false;
intersector.TargetType = FindReferenceTarget.Element;

foreach (var element in elements)
{
    if (element == null) continue;

    // Where the ray starts: the element's own point, or the middle of its box
    // when it has no point. Both are inside the element, which is why its own
    // faces have to be dropped below.
    XYZ origin = null;
    var point = element.Location as LocationPoint;
    if (point != null) { try { origin = point.Point; } catch { } }

    if (origin == null)
    {
        try
        {
            var box = element.get_BoundingBox(null);
            if (box != null) origin = (box.Min + box.Max) / 2.0;
        }
        catch { }
    }

    if (origin == null) { noOrigin.Add(element.Id); continue; }

    var found = new List<string>();

    foreach (var direction in directions)
    {
        if (direction == null || direction.GetLength() < 1e-9) continue;
        var unit = direction.Normalize();

        // THE LINKED RAY - see the header. Written only when a link is nearest.
        if (linkIntersector != null)
        {
            try
            {
                var linkNearest = linkIntersector.FindNearest(origin, unit);
                var linkReference = linkNearest == null ? null : linkNearest.GetReference();
                if (linkReference != null && linkNearest.Proximity <= maxDistance
                    && linkReference.LinkedElementId != ElementId.InvalidElementId)
                {
                    var placement = doc.GetElement(linkReference.ElementId) as RevitLinkInstance;
                    Document linked = null;
                    try { linked = placement == null ? null : placement.GetLinkDocument(); } catch { }
                    var linkedElement = linked == null ? null : linked.GetElement(linkReference.LinkedElementId);
                    linkedTotal++;
                    linkedMatches.Add(string.Format("  id {0}: ({1:0.##}, {2:0.##}, {3:0.##}) hits {4} at "
                        + "{5:0} mm from the element's own point", element.Id, unit.X, unit.Y, unit.Z,
                        linkedElement == null ? "a linked element"
                            : linked.Title + " - " + describeLinked(linked, linkedElement),
                        linkNearest.Proximity * MillimetresPerFoot));
                }
            }
            catch { }
        }

        ReferenceWithContext nearest = null;
        try { nearest = intersector.FindNearest(origin, unit); }
        catch { continue; }

        if (nearest == null) continue;

        double distance = nearest.Proximity;
        if (distance > maxDistance) continue;

        ElementId hitId = ElementId.InvalidElementId;
        try
        {
            var reference = nearest.GetReference();
            if (reference != null) hitId = reference.ElementId;
        }
        catch { }

        // Its own geometry is the first thing in the way of a ray that starts
        // inside it.
        if (hitId == ElementId.InvalidElementId || hitId == element.Id) continue;

        var hitElement = doc.GetElement(hitId);
        string what = "(unknown)";
        if (hitElement != null)
        {
            string name = "";
            string category = "";
            try { name = hitElement.Name ?? ""; } catch { }
            try { if (hitElement.Category != null) category = hitElement.Category.Name ?? ""; } catch { }
            what = (category.Length > 0 ? category + " " : "") + name + " (" + hitId.ToString() + ")";
        }

        found.Add("(" + unit.X.ToString("0.##") + ", " + unit.Y.ToString("0.##") + ", " +
                  unit.Z.ToString("0.##") + ") hits " + what +
                  " at " + distance.ToString("0.###") + " ft from the element's own point");
    }

    if (found.Count > 0) hits[element.Id] = found;
    else nothingHit.Add(element.Id);
}

if (linkDocs.Count > 0 && view3d == null)
    linkBlocked = "Links NOT read: no 3D view was given to cast the rays in";

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} direction(s) where a linked element is the nearest thing in reach, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
