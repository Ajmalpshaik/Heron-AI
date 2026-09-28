// NOT STANDALONE. Assumes `doc` and `includeLinks` are in scope; leaves
// `elements`, `findings`, `elevationsMm`, `linksSearched` and `linkedMatches`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// FEET TO MILLIMETRES BY ARITHMETIC (D-20). 1 ft = 304.8 mm exactly.
//
// TWO ELEVATIONS, AND WHICH ONE THE DRAWING SHOWS IS NOT THE OBVIOUS ONE.
//
//   Elevation         from the project's INTERNAL ORIGIN. The property the API
//                     offers first, and the one most code reaches for
//   ProjectElevation  from the project's ELEVATION BASE - the number Revit
//                     puts on the level head
//
// They are equal in a project whose base point has never been moved, which is
// most of them, which is exactly why the difference goes unnoticed until a
// project where it matters. Reporting only the first gives a list of heights
// that disagrees with the drawing and carries no sign that it does. So both are
// read, and the row SAYS SO whenever they differ.
//
// SORTED BY HEIGHT, NOT BY NAME. "Level 10" sorts before "Level 2" as text and
// sits above it in the building. A level list is read as a section through the
// building, and one in name order is read the same way and is wrong.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link's levels are
// read the same way and reported as TEXT in `linkedMatches`, one line per
// level. They never enter `elements` or `elevationsMm`: those feed the next
// fragment in a chain by element id, and the chain revives ids against the
// HOST document, so a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// A LINK'S LEVEL HEAD READS IN THE LINK'S OWN TERMS. The number on a linked
// level is measured from the LINK's elevation base, and the link may be placed
// higher or lower than the host. So where the placement is moved vertically
// the line adds where the level sits from THIS model's internal origin - the
// one figure the two models can be compared on. Read from the first
// placement; a file placed at two different heights is said to be.
//
// THE LINK LINES USE THIS FILE'S OWN READING OF THE TWO ELEVATIONS, ON
// PURPOSE. FRAGMENT-ISSUES row 5b-180 records that this reading is the
// opposite of Autodesk's and of five other fragments, and is OPEN. Linked
// lines on a different reading from the host lines above them would put two
// contradictory numbers in one answer; on the same reading, whatever settles
// row 5b-180 corrects both together.

var elements = new List<Element>();
var findings = new List<string>();
var elevationsMm = new Dictionary<ElementId, double>();

var levels = new List<Level>();

foreach (var level in new FilteredElementCollector(doc)
                          .OfClass(typeof(Level))
                          .Cast<Level>())
{
    if (level != null) levels.Add(level);
}

levels.Sort(delegate (Level a, Level b) { return a.Elevation.CompareTo(b.Elevation); });

foreach (var level in levels)
{
    elements.Add(level);

    var internalMm = level.Elevation * 304.8;
    var projectMm = level.ProjectElevation * 304.8;

    // The number that matches the drawing is the one reported as THE
    // elevation; the internal one is added only when it differs, where it
    // explains why other tools report something else.
    elevationsMm[level.Id] = projectMm;

    // A tenth of a millimetre. Below that the two agree for every practical
    // purpose and printing both would put a meaningless caveat on every row.
    var differs = Math.Abs(internalMm - projectMm) > 0.1;

    findings.Add(string.Format(
        "{0}  {1:0.#} mm{2}",
        level.Name,
        projectMm,
        differs
            ? string.Format(" (the level head reads this; from the internal origin it is {0:0.#} mm - "
                            + "the project base point has been moved)", internalMm)
            : ""));
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

    // How far the link is lifted, from its first placement, and whether its
    // other placements agree.
    double liftFt = 0;
    var heightsDiffer = false;
    try
    {
        liftFt = linkPlacements[i][0].GetTotalTransform().Origin.Z;
        foreach (var placement in linkPlacements[i])
        {
            if (Math.Abs(placement.GetTotalTransform().Origin.Z - liftFt) * 304.8 > 0.1)
                heightsDiffer = true;
        }
    }
    catch (Exception) { }

    var linkLevels = new List<Level>();
    try
    {
        foreach (var level in new FilteredElementCollector(linked)
                                  .OfClass(typeof(Level))
                                  .Cast<Level>())
        {
            if (level != null) linkLevels.Add(level);
        }
    }
    catch (Exception) { }

    linkLevels.Sort(delegate (Level a, Level b) { return a.Elevation.CompareTo(b.Elevation); });

    linksSearched++;
    linkedTotal += linkLevels.Count;
    linkedMatches.Add(string.Format("{0}: {1} level(s){2}", linked.Title, linkLevels.Count,
        heightsDiffer ? " - its placements sit at DIFFERENT heights; figures use the first" : ""));

    foreach (var level in linkLevels)
    {
        var headMm = level.ProjectElevation * 304.8;
        var fromHostMm = (level.Elevation + liftFt) * 304.8;
        var lifted = Math.Abs(liftFt * 304.8) > 0.1;

        linkedMatches.Add(string.Format("  {0}: {1}  {2:0.#} mm{3}", linked.Title, level.Name,
            headMm,
            lifted
                ? string.Format(" (the link is placed {0:0.#} mm {1}; from this model's internal "
                    + "origin it is {2:0.#} mm)", Math.Abs(liftFt * 304.8),
                    liftFt > 0 ? "up" : "down", fromHostMm)
                : ""));
    }
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
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} level(s), NOT in the list above",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
