// NOT STANDALONE. Assumes `doc`, `includeRooms`, `includeSpaces` and
// `includeLinks` are in scope, and leaves `elements`, `unplaced`, `unenclosed`,
// `roomsChecked`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// COLLECTED BY CATEGORY, READ THROUGH THE BASE TYPE.
//
// Rooms and Spaces are two categories and one job: number, name, area and
// location are all declared on the type they share. Collecting by category and
// reading through that base means one pass covers both, and the fragment never
// has to name the category-specific classes.
//
// ZERO AREA IS TWO DIFFERENT FAULTS.
//
//   no location    NEVER PLACED. It exists in the schedule and is not in the
//                  model. Deleted, or placed
//   a location     NOT ENCLOSED. Placed where the boundaries do not close.
//                  A wall, a gap, or a room-bounding setting
//
// Both read as "zero area" in a schedule, and telling somebody to go looking
// for the walls around a room that was never placed wastes the afternoon that
// this fragment exists to save.
//
// REDUNDANT ROOMS ARE NOT DETECTED HERE. Two rooms in one enclosed region is a
// third fault, and Revit reports it as a warning - which is a better answer
// than anything that could be re-derived from geometry, and it already has a
// fragment.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per room found. They never enter `elements`, `unplaced` or
// `unenclosed`: those feed the next fragment in a chain as element ids, and
// the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// In a federated job the ROOMS ARE IN THE ARCHITECTURAL LINK, so this is the
// fragment that answers "none" most confidently on an MEP model when they are
// not read.

var elements = new List<Element>();
var unplaced = new List<ElementId>();
var unenclosed = new List<ElementId>();

var candidates = new List<SpatialElement>();

if (includeRooms)
{
    candidates.AddRange(new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Rooms)
        .WhereElementIsNotElementType()
        .OfType<SpatialElement>());
}

if (includeSpaces)
{
    candidates.AddRange(new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_MEPSpaces)
        .WhereElementIsNotElementType()
        .OfType<SpatialElement>());
}

int roomsChecked = candidates.Count;

foreach (var spatial in candidates)
{
    if (spatial == null) continue;

    double area = 0;
    try { area = spatial.Area; } catch { }
    if (area > 0) continue;

    bool placed = false;
    try { placed = spatial.Location != null; } catch { }

    if (placed) unenclosed.Add(spatial.Id); else unplaced.Add(spatial.Id);
    elements.Add(spatial);
}

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

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
}

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var linkCandidates = new List<SpatialElement>();

    try
    {
        if (includeRooms)
            linkCandidates.AddRange(new FilteredElementCollector(linked)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<SpatialElement>());

        if (includeSpaces)
            linkCandidates.AddRange(new FilteredElementCollector(linked)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .OfType<SpatialElement>());
    }
    catch (Exception) { }

    var linkFaulty = 0;
    var lines = new List<string>();

    foreach (var spatial in linkCandidates)
    {
        if (spatial == null) continue;

        double area = 0;
        try { area = spatial.Area; } catch (Exception) { }
        if (area > 0) continue;

        bool placed = false;
        try { placed = spatial.Location != null; } catch (Exception) { }

        string number = "";
        try { number = spatial.Number ?? ""; } catch (Exception) { }

        linkFaulty++;
        lines.Add(string.Format("  {0}: '{1}' {2} - {3}", linked.Title, spatial.Name, number,
            placed ? "NOT ENCLOSED" : "NOT PLACED"));
    }

    linksSearched++;
    linkedTotal += linkFaulty;
    linkedMatches.Add(string.Format("{0}: {1} of {2} checked have no area", linked.Title,
        linkFaulty, linkCandidates.Count));
    linkedMatches.AddRange(lines);
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} match(es), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
