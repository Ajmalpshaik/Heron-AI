// NOT STANDALONE. Assumes `doc` and `includeLinks` are in scope, and leaves
// `levels`, `elevations`, `projectElevations`, `worstDifference`, `affected`,
// `atRisk`, `typesDisagree`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A LEVEL HAS TWO HEIGHTS.
//
//   Elevation         measured from whatever the level TYPE's "Elevation Base"
//                     parameter says - Project or Shared. What the level head
//                     and the Properties palette show, so it is the right one
//                     to put in front of a person.
//   ProjectElevation  always measured from the project origin. The only one in
//                     the same space as every XYZ the API returns.
//
// Use Elevation in a calculation against a coordinate and the answer is wrong
// by the survey offset - silently, with a plausible number, on exactly the real
// site models where nobody is checking.
//
// THE VERDICT IS SUBTRACTION, NOT THE SETTING.
//
// "Elevation Base" is read and reported because a person will want to see it,
// but no branch below depends on it. The API publishes no enum for its values,
// so mapping an integer to "Project" is an assumption, and AsValueString is
// localised. Subtracting the two elevations is true in any language and on any
// release.
//
// ZERO DIFFERENCE IS NOT PROOF OF SAFETY.
//
// The two agree both when the base is Project AND when the base is Shared with
// the shared origin sitting on the project origin. The second is one relocate
// away from diverging, so the survey point's own vertical offset is measured
// separately and reported as `atRisk`. Folding that into "not affected" is the
// same shortcut this fragment exists to catch.
//
// A LINK'S LEVELS ARE COMPARED WITH THIS MODEL'S, BY NAME, ONLY WHEN ASKED FOR -
// D-59. "Is the architect's Level 2 at the same height as mine" is the
// coordination question, and it is a subtraction too. With `includeLinks` set,
// each linked level's project elevation is carried through the link's FIRST
// placement into this model's space and set against the host level of the
// same NAME - a link's levels are its own elements, so a host id means nothing
// there. Every result is TEXT in `linkedMatches`; `levels` and the two
// dictionaries stay this model's own, because they are keyed by ids the next
// step would look up here (FRAGMENT-ISSUES row 75). A file placed more than
// once at different heights is said, not averaged. NESTED LINKS ARE NOT READ,
// AND THE ANSWER COUNTS THEM. Only model elements are read, never a link's
// views or sheets.

var levels = new FilteredElementCollector(doc)
    .OfClass(typeof(Level))
    .Cast<Level>()
    .OrderBy(l => l.ProjectElevation)
    .Cast<Element>()
    .ToList();

var elevations = new Dictionary<ElementId, double>();
var projectElevations = new Dictionary<ElementId, double>();
var basesSeen = new HashSet<string>();

double worstDifference = 0.0;

foreach (var element in levels)
{
    var level = element as Level;
    if (level == null) continue;

    double shown = level.Elevation;
    double real = level.ProjectElevation;
    elevations[level.Id] = shown;
    projectElevations[level.Id] = real;

    double difference = Math.Abs(shown - real);
    if (difference > worstDifference) worstDifference = difference;

    // "Elevation Base" is on the level TYPE, not the level. Two level types in
    // one model can disagree, and that is the case a spot check misses.
    try
    {
        var levelType = doc.GetElement(level.GetTypeId());
        if (levelType != null)
        {
            var p = levelType.get_Parameter(BuiltInParameter.LEVEL_RELATIVE_BASE_TYPE);
            if (p != null && p.HasValue)
            {
                string shownBase = null;
                try { shownBase = p.AsValueString(); } catch { }
                if (!string.IsNullOrEmpty(shownBase)) basesSeen.Add(shownBase);
            }
        }
    }
    catch { }
}

// The survey point's own vertical offset from the project origin. Measured off
// the element rather than inferred, because it is what a Shared-based level
// height WOULD carry the moment anything switches.
double surveyOffset = 0.0;
bool surveyOffsetKnown = false;
try
{
    foreach (var basePoint in new FilteredElementCollector(doc)
                 .OfClass(typeof(BasePoint))
                 .Cast<BasePoint>())
    {
        if (!basePoint.IsShared) continue;
        surveyOffset = basePoint.SharedPosition.Z - basePoint.Position.Z;
        surveyOffsetKnown = true;
    }
}
catch { }

// Half a millimetre in feet. Below this the two numbers are the same number
// with rounding on it, and calling that a difference would make every model
// report as affected.
const double Tolerance = 0.0005 / 0.3048;

bool affected = worstDifference > Tolerance;
bool atRisk = !affected && surveyOffsetKnown && Math.Abs(surveyOffset) > Tolerance;
bool typesDisagree = basesSeen.Count > 1;

// ---- D-59: the links' levels against these, by name -------------------------

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
}

var linkBlocked = "";

// Host levels by name, for the comparison. Names are unique among levels in
// one model, which Revit enforces.
var hostByName = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase);
foreach (var element in levels)
{
    var level = element as Level;
    if (level != null && !hostByName.ContainsKey(level.Name)) hostByName[level.Name] = level;
}

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    Transform placed = null;
    try { placed = linkPlacements[i][0].GetTotalTransform(); } catch (Exception) { }

    // A second placement at another height moves every level in it.
    var heightsDiffer = false;
    for (var j = 1; placed != null && j < linkPlacements[i].Count; j++)
    {
        try
        {
            if (Math.Abs(linkPlacements[i][j].GetTotalTransform().Origin.Z - placed.Origin.Z) > Tolerance)
                heightsDiffer = true;
        }
        catch (Exception) { }
    }

    var linkLevels = new List<Level>();
    try
    {
        foreach (var level in new FilteredElementCollector(linked).OfClass(typeof(Level)).Cast<Level>())
            if (level != null) linkLevels.Add(level);
    }
    catch (Exception) { }
    linkLevels = linkLevels.OrderBy(l => l.ProjectElevation).ToList();

    var named = 0;
    var disagree = 0;
    var unmatched = 0;
    var rows = new List<string>();
    foreach (var level in linkLevels)
    {
        if (placed == null) break;
        var here = placed.OfPoint(new XYZ(0, 0, level.ProjectElevation)).Z;
        Level mine;
        if (!hostByName.TryGetValue(level.Name, out mine))
        {
            unmatched++;
            rows.Add(string.Format("  {0} - '{1}' at {2:0.0} mm here: NO level of that name in this model",
                linked.Title, level.Name, here * 304.8));
            continue;
        }
        named++;
        var difference = here - mine.ProjectElevation;
        var off = Math.Abs(difference) > Tolerance;
        if (off) disagree++;
        rows.Add(string.Format("  {0} - '{1}' at {2:0.0} mm here; this model's at {3:0.0} mm - {4}",
            linked.Title, level.Name, here * 304.8, mine.ProjectElevation * 304.8,
            off ? string.Format("DIFFERS by {0:0.0} mm", difference * 304.8) : "same height"));
    }

    linksSearched++;
    linkedTotal += disagree;
    linkedMatches.Add(string.Format("{0}: {1} level(s), {2} named like one here and {3} of those at "
        + "a different height, {4} with no level of that name here{5}",
        linked.Title, linkLevels.Count, named, disagree, unmatched,
        heightsDiffer ? ". PLACED MORE THAN ONCE AT DIFFERENT HEIGHTS - measured through the first "
            + "placement only" : ""));
    linkedMatches.AddRange(rows);
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
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} linked level(s) at a different height from this model's level of the same name, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
