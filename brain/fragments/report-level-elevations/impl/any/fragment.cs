// NOT STANDALONE. Assumes `doc`, `includeLinks`, `linkName`, `datums`,
// `toleranceMm` and `angleToleranceDeg` are in scope, and leaves `levels`,
// `elevations`, `projectElevations`, `worstDifference`, `affected`, `atRisk`,
// `typesDisagree`, `linksSearched`, `linkedMatches`, `storeys`, `unreadable`,
// `elements`, `elevationsMm`, `projectElevationsMm`, `findings`, `report`,
// `failures`, `aligned`, `compared`, `notChecked`, `hostOnly`, `linkOnly`,
// `failingIds`, `failingElements`, `linkUsed`, `refused` and `monitored`
// behind.
//
// VERSION 7 (2026-11-26, row 5b-1878 (C), D-207) - THE ONE DATUM TOOL.
// CHECK_DATUMS_AGAINST_LINK merged in: this model's grids and levels checked
// against a link's, the copy-monitored ones listed and measured against the
// placement they watch, and a link placed more than once never measured until
// its copy is named. The host part above the version 7 section is version 6's,
// line for line; the D-59 link section is replaced by it (see there).
//
// VERSION 4 (2026-11-01) - LIST_LEVELS MERGED IN. Its answers are here beside
// this card's: the levels handed on as `elements` too (the name the next step
// reads), bottom to top; `elevationsMm`, each level's height in millimetres as
// its level head shows it; `projectElevationsMm`, from the project origin;
// and `findings`, one line per level, the two said apart wherever they
// differ. Each linked level's line also gives the number its own level head
// reads.
//
// THE READING OF THE TWO HEIGHTS IS THIS CARD'S, AND IT IS RevitAPI.xml's:
// ProjectElevation is relative to the project origin whatever the Elevation
// Base says, and Elevation is relative to the base the level type names. The
// retired LIST_LEVELS read them the other way round (row 5b-180) - its
// `elevationsMm` held ProjectElevation and called it the level head. Here the
// level head is Elevation, as the header below has always said.
//
// VERSION 5 (2026-11-20, FRAGMENT-ISSUES rows 5b-1365 and 5b-342) - the level an
// element stands on, for the Building Story count, is FILTER_ELEMENTS_BY_CATEGORY's
// lookup: five level parameters, and one that holds no level is passed over.
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

// ---- Version 4: LIST_LEVELS' answers ----------------------------------------
// Feet to millimetres by arithmetic (D-20): 1 ft = 304.8 mm exactly.
var elements = new List<Element>(levels);
var elevationsMm = new Dictionary<ElementId, double>();
var projectElevationsMm = new Dictionary<ElementId, double>();
var findings = new List<string>();
foreach (var element in levels)
{
    var level = element as Level;
    if (level == null) continue;
    var headMm = level.Elevation * 304.8;
    var fromOriginMm = level.ProjectElevation * 304.8;
    elevationsMm[level.Id] = headMm;
    projectElevationsMm[level.Id] = fromOriginMm;
    // A tenth of a millimetre: below that the two are the same number with
    // rounding on it, and a caveat on every row would mean nothing.
    findings.Add(string.Format("{0}  {1:0.#} mm{2}", level.Name, headMm,
        Math.Abs(headMm - fromOriginMm) > 0.1
            ? string.Format(" (the level head reads this; from the project origin it is {0:0.#} mm - "
                + "the one to use against a coordinate)", fromOriginMm)
            : ""));
}
findings.Insert(0, string.Format("{0} level(s), bottom to top", levels.Count));
bool atRisk = !affected && surveyOffsetKnown && Math.Abs(surveyOffset) > Tolerance;
bool typesDisagree = basesSeen.Count > 1;

// ---- Version 3: which levels are Building Stories ---------------------------
//
// EACH LEVEL'S "BUILDING STORY" TICK, AND WHAT STANDS ON THE ONES WITHOUT IT.
// An IFC export makes a storey only from a level ticked Building Story, so an
// element whose level is NOT ticked is put in some other storey on the way out.
// This reports the tick and a COUNT - never a verdict: an unticked level with
// nothing on it is common and harmless, and whether one with elements on it
// matters is the modeller's call.
//
// THE LEVEL OF AN ELEMENT IS LOOKED UP THE WAY FILTER_ELEMENTS_BY_CATEGORY
// DOES (version 5, FRAGMENT-ISSUES rows 5b-335 and 5b-1365) - wall, LevelId,
// then five level parameters in turn, an MEP curve's START level last, which
// is the only place a duct or pipe keeps one. A PARAMETER THAT HOLDS NO LEVEL
// IS PASSED OVER, NEVER ANSWERED WITH: version 4 asked the family's level
// parameter and then the start level, SELECT_WITHOUT_LEVEL's four steps (that
// card is retired into FILTER_ELEMENTS_BY_CATEGORY), so a work plane-based
// diffuser whose "Level" is empty and whose Schedule Level is Level 1 counted
// on no level. Only MODEL elements are counted: views, sheets and annotation
// carry a level too, and none of them goes into a storey.
//
// THIS MODEL'S LEVELS ONLY. A link's levels are not read for this, with or
// without `includeLinks`.

var storeys = new List<string>();
var unticked = new List<Level>();
var ticked = new List<string>();
var unreadable = new List<string>();

foreach (var element in levels)
{
    var level = element as Level;
    if (level == null) continue;
    Parameter flag = null;
    try { flag = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY); } catch (Exception) { flag = null; }
    if (flag == null || !flag.HasValue) { unreadable.Add(level.Name); continue; }
    if (flag.AsInteger() == 1) ticked.Add(level.Name);
    else unticked.Add(level);
}

// Version 5: FILTER_ELEMENTS_BY_CATEGORY's five, in its order - the first that
// holds a LEVEL answers (row 5b-335).
var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
    BuiltInParameter.LEVEL_PARAM,
    BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
    BuiltInParameter.RBS_START_LEVEL_PARAM,
};

var onLevel = new Dictionary<ElementId, int>();
foreach (var level in unticked) onLevel[level.Id] = 0;
var modelElementsRead = 0;

if (unticked.Count > 0)
{
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        if (element == null || element is Level || element.ViewSpecific) continue;
        Category category = null;
        try { category = element.Category; } catch (Exception) { category = null; }
        if (category == null || category.CategoryType != CategoryType.Model) continue;
        modelElementsRead++;

        // FILTER_ELEMENTS_BY_CATEGORY's lookup, in its order (version 5).
        var at = ElementId.InvalidElementId;
        var wall = element as Wall;
        if (wall != null) at = wall.LevelId;
        if (at == ElementId.InvalidElementId) at = element.LevelId;
        if (at == ElementId.InvalidElementId)
        {
            foreach (var candidate in levelParameterOrder)
            {
                Parameter parameter = null;
                try { parameter = element.get_Parameter(candidate); } catch (Exception) { parameter = null; }
                if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.ElementId) continue;
                ElementId held = null;
                try { held = parameter.AsElementId(); } catch (Exception) { held = null; }
                if (held == null || held == ElementId.InvalidElementId) continue;
                if (doc.GetElement(held) is Level) { at = held; break; }
            }
        }

        int count;
        if (at != ElementId.InvalidElementId && onLevel.TryGetValue(at, out count)) onLevel[at] = count + 1;
    }
}

// VERSION 8 (2026-11-26, row 5b-2070, issue #1480) - A DOCUMENT WITH NO LEVELS
// IS NOT TOLD EVERY LEVEL IS MARKED. Version 7 wrote "Every level is marked
// Building Story - 0 level(s) read: (this model has no levels)" on Revit 2020
// for an area tag family: a claim about levels that are not there, and the one
// line that kept that run from reading empty. With no level read, `storeys` is
// left EMPTY - `findings` already opens "0 level(s), bottom to top", which says
// it - and nothing else here changes.
if (ticked.Count == 0 && unticked.Count == 0 && unreadable.Count == 0)
{
    // No level read at all: nothing to say about storeys.
}
else if (unticked.Count == 0 && unreadable.Count == 0)
{
    storeys.Add(string.Format("Every level is marked Building Story - {0} level(s) read: {1}",
        ticked.Count, string.Join(", ", ticked)));
}
else
{
    var onUnticked = 0;
    foreach (var pair in onLevel) onUnticked += pair.Value;
    // NO UNMARKED LEVEL MEANS NO ELEMENT WAS READ, and "0 of 0" would read as a
    // measured zero (review of PR #410), so that case says the scan did not run.
    storeys.Add(string.Format("{0} level(s) read: {1} marked Building Story, {2} NOT marked{3}. {4}",
        levels.Count, ticked.Count, unticked.Count,
        unreadable.Count > 0 ? string.Format(", {0} whose Building Story setting could not be read", unreadable.Count) : "",
        unticked.Count == 0
            ? "No level was found unmarked, so elements were NOT counted"
            : string.Format("{0} model element(s) have one of the unmarked levels as their level, "
                + "of {1} model element(s) read", onUnticked, modelElementsRead)));
    foreach (var level in unticked)
        storeys.Add(string.Format("  '{0}' at {1:0.0} mm (project) - NOT a Building Story; {2} model "
            + "element(s) on it", level.Name, level.ProjectElevation * 304.8, onLevel[level.Id]));
    if (ticked.Count > 0)
        storeys.Add("  Marked Building Story: " + string.Join(", ", ticked));
    if (unreadable.Count > 0)
        storeys.Add("  Building Story could not be read: " + string.Join(", ", unreadable));
    storeys.Add("An IFC export makes storeys only from levels marked Building Story, so elements "
        + "on an unmarked level are placed in another storey in the export. These are counts, not "
        + "a judgement - views, sheets and annotation are not counted, and linked models are not read for this");
}

// ---- Version 7: grids and levels against the links, and the copy-monitored ones ----
//
// CHECK_DATUMS_AGAINST_LINK WAS MERGED INTO THIS CARD on 2026-11-26 (row
// 5b-1878 (C), the owner's word: one datum tool for levels and grids in this
// model, in a linked model, and copy-monitored). Its check is here, rule for
// rule: each link datum carried into this model's space through the
// placement's TOTAL transform and matched to this model's datum of the same
// kind - by NAME first, then the first unmatched one that lines up within both
// tolerances ("name differs"), each matched once; a level by ProjectElevation,
// + when the link's is higher; a straight grid by its distance square to ours
// from the link grid's start point AND its angle, "drawn the opposite way" when
// it runs the other way along the same line; an arc grid by its centre and
// radius; a multi-segment grid, an arc against a straight grid, and anything
// on a TILTED placement "not checked" with the reason, never a guess. One row
// per datum in `report`, `failures` the datums OFF, `failingIds` and
// `failingElements` this model's datums that are off, `compared` how many pairs
// were measured (0 is "nothing read", D-165), `refused` why nothing was.
//
// WHICH LINKS (D-207, the owner's "Ask which copy"). `linkName` names one
// placement, as CHECK_DATUMS_AGAINST_LINK took it: its full name, a part of it,
// or "only" for the one placement this model has - two matching, or none, is
// refused with every placement named. Left out, with `includeLinks`, every
// loaded link FILE placed once is checked. A FILE PLACED MORE THAN ONCE IS
// NEVER MEASURED THROUGH ONE OF ITS PLACEMENTS UNTIL ONE IS NAMED - every
// placement is named and the modeller is asked; the first is never taken,
// which is what version 6 did. Neither `includeLinks` nor `linkName`: no link
// is opened (D-59), and the check says it did not run.
//
// COPY-MONITORED DATUMS. A level or grid here that copy-monitors a link
// element knows WHICH PLACEMENT it monitors (Element.GetMonitoredLinkElementIds
// gives the link instance) - so that placement is named by the monitoring, and
// its monitored datums are measured against it even when its file is placed
// twice. Revit does not hand over WHICH element in the link is monitored, so
// within that placement it is matched the same way, by name then by position.
// Every copy-monitored datum is listed in `monitored` on every run - that is
// read from this model alone - and is measured only when links are asked for.
// A monitored datum with no partner in its link is reported, never dropped.
//
// `linkedMatches` KEEPS VERSION 6'S LEVEL LINES - by NAME only, never paired
// with the nearest height - now through the placement chosen above rather than
// a file's first placement, and with `toleranceMm` as "same height".

const double MmPerFoot = 304.8;
const double Vertical = 1e-9;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

var report = "";
var failures = 0;
var aligned = 0;
var compared = 0;
var notChecked = 0;
var hostOnly = 0;
var linkOnly = 0;
var failingIds = new List<ElementId>();
var failingElements = new List<Element>();
var linkUsed = "";
string refused = null;
var monitored = new List<string>();

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";
var linkRefusals = new List<string>();
var usedNames = new List<string>();

// ---- What was asked -----------------------------------------------------------

var wantText = (datums ?? "").Trim().ToLowerInvariant();
var doGrids = false;
var doLevels = false;
if (wantText == "" || wantText == "both" || wantText == "all"
    || wantText == "grids and levels" || wantText == "levels and grids")
{
    doGrids = true;
    doLevels = true;
}
else if (wantText == "grids" || wantText == "grid") doGrids = true;
else if (wantText == "levels" || wantText == "level") doLevels = true;
else refused = string.Format("'{0}' is not grids, levels or both - say which datums to check.", datums);

if (refused == null && (double.IsNaN(toleranceMm) || double.IsInfinity(toleranceMm) || toleranceMm <= 0))
    refused = string.Format("A distance tolerance of {0} mm cannot be met by anything - give one above 0 mm "
        + "(left out, it is 0.5 mm).", toleranceMm.ToString(invariant));

if (refused == null && (double.IsNaN(angleToleranceDeg) || double.IsInfinity(angleToleranceDeg)
    || angleToleranceDeg <= 0 || angleToleranceDeg >= 90))
    refused = string.Format("An angle tolerance of {0} degrees is not one this can use - give one above 0 and "
        + "below 90 (left out, it is 0.01).", angleToleranceDeg.ToString(invariant));

var given = (linkName ?? "").Trim();
var readLinks = includeLinks || given.Length > 0;

// ---- Every placement this model has, nested ones left out ----------------------

var placements = new List<RevitLinkInstance>();
foreach (var instance in new FilteredElementCollector(doc)
    .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
{
    if (instance == null) continue;
    RevitLinkType linkType = null;
    try { linkType = doc.GetElement(instance.GetTypeId()) as RevitLinkType; }
    catch (Exception) { linkType = null; }
    if (linkType != null && linkType.IsNestedLink) continue;
    placements.Add(instance);
}
Func<IEnumerable<RevitLinkInstance>, string> placementNames = some =>
    string.Join(", ", some.Select(each => "'" + (each.Name ?? "(unnamed)") + "'").ToArray());
var everyName = placements.Count == 0 ? "no links at all" : placementNames(placements);

// ---- Copy-monitored datums here - read from this model alone --------------------

var monitorsOf = new Dictionary<ElementId, List<ElementId>>();
var hostDatums = new List<Element>();
foreach (var datum in new FilteredElementCollector(doc).OfClass(typeof(Level))) hostDatums.Add(datum);
var hostGridCount = 0;
foreach (var datum in new FilteredElementCollector(doc).OfClass(typeof(Grid)))
{
    var ownerId = ElementId.InvalidElementId;
    try { ownerId = MultiSegmentGrid.GetMultiSegementGridId(datum as Grid); } catch (Exception) { }
    if (ownerId == null || ownerId == ElementId.InvalidElementId) hostGridCount++;
    hostDatums.Add(datum);
}
foreach (var datum in new FilteredElementCollector(doc).OfClass(typeof(MultiSegmentGrid)))
{
    hostGridCount++;
    hostDatums.Add(datum);
}
foreach (var datum in hostDatums)
{
    if (datum == null) continue;
    var monitoring = false;
    try { monitoring = datum.IsMonitoringLinkElement(); } catch (Exception) { monitoring = false; }
    if (!monitoring) continue;
    IList<ElementId> sources = null;
    try { sources = datum.GetMonitoredLinkElementIds(); } catch (Exception) { sources = null; }
    var ids = sources == null ? new List<ElementId>() : sources.ToList();
    monitorsOf[datum.Id] = ids;
    var sourcesSaid = ids.Select(id =>
    {
        var placement = doc.GetElement(id) as RevitLinkInstance;
        return placement == null ? "a link placement no longer in this model" : "'" + (placement.Name ?? "(unnamed)") + "'";
    }).ToList();
    monitored.Add(string.Format("{0} '{1}' copy-monitors {2}", datum is Level ? "level" : "grid", datum.Name ?? "",
        sourcesSaid.Count == 0 ? "a link Revit did not name" : string.Join(" and ", sourcesSaid.ToArray())));
}
monitored.Insert(0, string.Format("This model has {0} level(s) and {1} grid(s); {2} of them copy-monitored from a "
    + "link{3}", levels.Count, hostGridCount, monitorsOf.Count,
    monitorsOf.Count == 0 ? " - none" : readLinks ? "" : " - listed here, and measured only when links are asked for"));
Func<Element, ElementId, bool> monitorsThis = (datum, placementId) =>
{
    List<ElementId> ids;
    return datum != null && monitorsOf.TryGetValue(datum.Id, out ids) && ids.Contains(placementId);
};

// ---- Which placements to check against (D-207) ---------------------------------

var targets = new List<Tuple<RevitLinkInstance, bool>>();
if (refused == null && readLinks && given.Length > 0)
{
    RevitLinkInstance chosen = null;
    if (placements.Count == 0)
        refused = "This model has no Revit links, so there is nothing to check its grids and levels against.";
    else if (string.Equals(given, "only", StringComparison.OrdinalIgnoreCase))
    {
        if (placements.Count == 1) chosen = placements[0];
        else refused = string.Format("This model has {0} link placements - {1} - so the one to check against "
            + "has to be named.", placements.Count, everyName);
    }
    else
    {
        var exact = placements.Where(each => string.Equals((each.Name ?? "").Trim(), given,
            StringComparison.OrdinalIgnoreCase)).ToList();
        var matching = exact.Count > 0 ? exact
            : placements.Where(each => (each.Name ?? "").IndexOf(given, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        if (matching.Count == 1) chosen = matching[0];
        else if (matching.Count == 0)
            refused = string.Format("No link matches '{0}'. This model has: {1}.", given, everyName);
        else
            refused = string.Format("AMBIGUOUS: {0} link placements match '{1}' - {2}. Name one exactly rather "
                + "than have this pick.", matching.Count, given, placementNames(matching));
    }
    if (chosen != null) targets.Add(Tuple.Create(chosen, false));
}
else if (refused == null && readLinks)
{
    var typeIds = new List<ElementId>();
    var groups = new List<List<RevitLinkInstance>>();
    foreach (var placement in placements)
    {
        var typeId = placement.GetTypeId();
        var known = typeIds.IndexOf(typeId);
        if (known >= 0) { groups[known].Add(placement); continue; }
        typeIds.Add(typeId);
        groups.Add(new List<RevitLinkInstance> { placement });
    }
    foreach (var group in groups)
    {
        if (group.Count == 1) { targets.Add(Tuple.Create(group[0], false)); continue; }
        string fileName = null;
        try { var linkType = doc.GetElement(group[0].GetTypeId()); fileName = linkType == null ? null : linkType.Name; }
        catch (Exception) { fileName = null; }
        linkRefusals.Add(string.Format("'{0}' is placed {1} times - {2} - so which copy to check against has to be "
            + "named (linkName). None of its placements was measured; the first is never taken.",
            fileName ?? "a linked file", group.Count, placementNames(group)));
        // A placement a datum here copy-monitors is named by that monitoring.
        foreach (var placement in group)
            if (hostDatums.Any(datum => monitorsThis(datum, placement.Id)))
                targets.Add(Tuple.Create(placement, true));
    }
}

// ---- The datums, each side in this model's space ------------------------------
//
// CHECK_DATUMS_AGAINST_LINK's reader, unchanged. One entry per datum: Item1
// name, Item2 kind ("grid" or "level"), Item3 the element, Item4 its shape
// ("line", "arc", "multi", "other" or "level"), Item5 a line's start or an
// arc's centre, Item6 a line's direction, Item7 an arc's radius or a level's
// height - all in this model's internal feet.

Func<Document, Transform, List<Tuple<string, string, Element, string, XYZ, XYZ, double>>> read = (d, t) =>
{
    var found = new List<Tuple<string, string, Element, string, XYZ, XYZ, double>>();
    if (doGrids)
    {
        var segments = new HashSet<ElementId>();
        foreach (var multi in new FilteredElementCollector(d).OfClass(typeof(MultiSegmentGrid))
            .Cast<MultiSegmentGrid>())
        {
            if (multi == null) continue;
            try { foreach (var id in multi.GetGridIds()) segments.Add(id); } catch (Exception) { }
            found.Add(Tuple.Create(multi.Name ?? "", "grid", (Element)multi, "multi", (XYZ)null, (XYZ)null, 0.0));
        }
        foreach (var grid in new FilteredElementCollector(d).OfClass(typeof(Grid)).Cast<Grid>())
        {
            if (grid == null || segments.Contains(grid.Id)) continue;
            var owner = ElementId.InvalidElementId;
            try { owner = MultiSegmentGrid.GetMultiSegementGridId(grid); } catch (Exception) { }
            if (owner != null && owner != ElementId.InvalidElementId) continue;

            Curve curve = null;
            try { curve = grid.Curve; } catch (Exception) { curve = null; }
            var line = curve as Line;
            var arc = curve as Arc;
            if (line != null)
                found.Add(Tuple.Create(grid.Name ?? "", "grid", (Element)grid, "line",
                    t.OfPoint(line.GetEndPoint(0)), t.OfVector(line.Direction), 0.0));
            else if (arc != null)
                found.Add(Tuple.Create(grid.Name ?? "", "grid", (Element)grid, "arc",
                    t.OfPoint(arc.Center), (XYZ)null, arc.Radius));
            else
                found.Add(Tuple.Create(grid.Name ?? "", "grid", (Element)grid, "other", (XYZ)null, (XYZ)null, 0.0));
        }
    }
    if (doLevels)
    {
        foreach (var lv in new FilteredElementCollector(d).OfClass(typeof(Level)).Cast<Level>())
        {
            if (lv == null) continue;
            found.Add(Tuple.Create(lv.Name ?? "", "level", (Element)lv, "level", (XYZ)null, (XYZ)null,
                t.OfPoint(new XYZ(0, 0, lv.ProjectElevation)).Z));
        }
    }

    // Grids in the order a drawing reads them - letters as letters, trailing
    // digits as a number, so 2 comes before 10 - then levels bottom to top.
    Func<string, string> head = n =>
    {
        var cut = n.Length;
        while (cut > 0 && char.IsDigit(n[cut - 1])) cut--;
        return n.Substring(0, cut).ToUpperInvariant();
    };
    Func<string, long> tail = n =>
    {
        var cut = n.Length;
        while (cut > 0 && char.IsDigit(n[cut - 1])) cut--;
        long value;
        return n.Length - cut > 0 && n.Length - cut < 18 && long.TryParse(n.Substring(cut), out value) ? value : -1;
    };
    found.Sort((a, b) =>
    {
        if (a.Item2 != b.Item2) return a.Item2 == "grid" ? -1 : 1;
        if (a.Item2 == "level") return a.Item7.CompareTo(b.Item7);
        var byHead = string.Compare(head(a.Item1), head(b.Item1), StringComparison.Ordinal);
        if (byHead != 0) return byHead;
        var byTail = tail(a.Item1).CompareTo(tail(b.Item1));
        return byTail != 0 ? byTail : string.Compare(a.Item1, b.Item1, StringComparison.Ordinal);
    });
    return found;
};

// ---- One comparison: status, offset in mm, angle in degrees, reason ---------
// CHECK_DATUMS_AGAINST_LINK's, unchanged but for the tilt, which is per link now.

var tiltReason = "the link is placed TILTED - its Z axis is not straight up - so this cannot be compared";
Func<Tuple<string, string, Element, string, XYZ, XYZ, double>, Tuple<string, string, Element, string, XYZ, XYZ, double>,
    bool, Tuple<string, double, double, string>> compare = (h, l, tilted) =>
{
    if (tilted) return Tuple.Create("not checked", double.NaN, double.NaN, tiltReason);

    if (h.Item2 == "level")
    {
        var rise = (l.Item7 - h.Item7) * MmPerFoot;
        return Tuple.Create(Math.Abs(rise) <= toleranceMm ? "aligned" : "off", rise, double.NaN,
            Math.Abs(rise) < 0.005 ? "" : (rise > 0 ? "the link's is higher" : "the link's is lower"));
    }

    if (h.Item4 == "multi" || l.Item4 == "multi")
        return Tuple.Create("not checked", double.NaN, double.NaN,
            "a multi-segment grid - its segments are not compared in this version");
    if (h.Item4 == "other" || l.Item4 == "other")
        return Tuple.Create("not checked", double.NaN, double.NaN, "a grid that is neither straight nor an arc");
    if (h.Item4 != l.Item4)
        return Tuple.Create("not checked", double.NaN, double.NaN, "one is an arc and the other straight");

    if (h.Item4 == "arc")
    {
        var cx = l.Item5.X - h.Item5.X;
        var cy = l.Item5.Y - h.Item5.Y;
        var centres = Math.Sqrt(cx * cx + cy * cy) * MmPerFoot;
        var radii = (l.Item7 - h.Item7) * MmPerFoot;
        var arcOk = centres <= toleranceMm && Math.Abs(radii) <= toleranceMm;
        return Tuple.Create(arcOk ? "aligned" : "off", centres, double.NaN,
            "centres " + centres.ToString("0.00", invariant) + " mm apart, radius "
            + (radii >= 0 ? "+" : "") + radii.ToString("0.00", invariant) + " mm in the link");
    }

    var ux = h.Item6.X; var uy = h.Item6.Y;
    var vx = l.Item6.X; var vy = l.Item6.Y;
    var uLength = Math.Sqrt(ux * ux + uy * uy);
    var vLength = Math.Sqrt(vx * vx + vy * vy);
    if (uLength < 1e-9 || vLength < 1e-9)
        return Tuple.Create("not checked", double.NaN, double.NaN, "a grid line with no direction in plan");
    ux /= uLength; uy /= uLength; vx /= vLength; vy /= vLength;

    var qx = l.Item5.X - h.Item5.X;
    var qy = l.Item5.Y - h.Item5.Y;
    var square = Math.Abs(ux * qy - uy * qx) * MmPerFoot;
    var angle = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy) * 180.0 / Math.PI;
    var reversed = false;
    // A PERPENDICULAR GRID HAS NO "OPPOSITE WAY": +90 and -90 are the same
    // line, so either is 90 and never reversed - and a hair off 90 from
    // rounding is 90 too, so it never prints as -90.
    if (Math.Abs(Math.Abs(angle) - 90.0) < 1e-9) angle = 90.0;
    else if (angle > 90.0) { angle -= 180.0; reversed = true; }
    else if (angle < -90.0) { angle += 180.0; reversed = true; }
    var lineOk = square <= toleranceMm && Math.Abs(angle) <= angleToleranceDeg;
    return Tuple.Create(lineOk ? "aligned" : "off", square, angle, reversed ? "drawn the opposite way" : "");
};

// ROUNDED FIRST, AND NEVER "-0.00": on .NET 8 and 10 (Revit 2025 on) a tiny
// negative value formats as "-0.00", where .NET Framework printed "0.00".
Func<double, int, double> tidy = (v, places) =>
{
    var r = Math.Round(v, places, MidpointRounding.AwayFromZero);
    return r == 0.0 ? 0.0 : r;
};
Func<double, string> mm = v => double.IsNaN(v) ? "-" : tidy(v, 2).ToString("0.00", invariant) + " mm";
Func<double, string> deg = v => double.IsNaN(v) ? "-" : tidy(v, 4).ToString("0.0000", invariant) + " deg";

// Host levels by name, for version 6's level lines. Revit keeps level names
// unique in one model.
var hostByName = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase);
foreach (var element in levels)
{
    var level = element as Level;
    if (level != null && !hostByName.ContainsKey(level.Name)) hostByName[level.Name] = level;
}

// ---- Each placement chosen, measured ----------------------------------------------

var rows = new List<string>();
rows.Add("kind | name | status | offset | angle | reason | against");
var host = targets.Count > 0 ? read(doc, Transform.Identity)
    : new List<Tuple<string, string, Element, string, XYZ, XYZ, double>>();
var tiltedLinks = new List<string>();

foreach (var target in targets)
{
    if (refused != null) break;
    var placement = target.Item1;
    var monitorOnly = target.Item2;
    var against = (placement.Name ?? "(unnamed)") + (monitorOnly ? " (copy-monitored only)" : "");

    Document linked = null;
    try { linked = placement.GetLinkDocument(); } catch (Exception) { linked = null; }
    Transform placed = null;
    if (linked != null) { try { placed = placement.GetTotalTransform(); } catch (Exception) { placed = null; } }
    if (linked == null || placed == null)
    {
        var why = linked == null
            ? string.Format("'{0}' is placed here but NOT LOADED, so it has no grids or levels to read. Reload it "
                + "and ask again.", placement.Name)
            : string.Format("Revit gave no placement for '{0}'.", placement.Name);
        if (given.Length > 0) { refused = why; break; }
        linkRefusals.Add(why);
        continue;
    }

    if (!monitorOnly) linksSearched++;
    usedNames.Add(placement.Name ?? "(unnamed)");
    try
    {
        nestedLinks += new FilteredElementCollector(linked).OfClass(typeof(RevitLinkInstance)).GetElementCount();
    }
    catch (Exception) { }

    var link = read(linked, placed);
    var up = placed.BasisZ;
    var tilted = Math.Abs(up.X) > Vertical || Math.Abs(up.Y) > Vertical || Math.Abs(up.Z - 1.0) > Vertical;
    if (tilted) tiltedLinks.Add(placement.Name ?? "(unnamed)");

    // Which of this model's datums take part: all of them, or - on a placement
    // named only by copy-monitoring - only the datums that monitor it.
    var pairOf = new int[link.Count];
    var nameDiffers = new bool[link.Count];
    var hostTaken = new bool[host.Count];
    for (var i = 0; i < link.Count; i++) pairOf[i] = -1;
    for (var j = 0; j < host.Count; j++) hostTaken[j] = monitorOnly && !monitorsThis(host[j].Item3, placement.Id);
    var notInThis = (bool[])hostTaken.Clone();

    for (var i = 0; i < link.Count; i++)
    {
        for (var j = 0; j < host.Count; j++)
        {
            if (hostTaken[j] || host[j].Item2 != link[i].Item2) continue;
            if (!string.Equals(host[j].Item1, link[i].Item1, StringComparison.Ordinal)) continue;
            pairOf[i] = j;
            hostTaken[j] = true;
            break;
        }
    }
    for (var i = 0; i < link.Count; i++)
    {
        if (pairOf[i] >= 0 || tilted) continue;
        for (var j = 0; j < host.Count; j++)
        {
            if (hostTaken[j] || host[j].Item2 != link[i].Item2) continue;
            if (compare(host[j], link[i], false).Item1 != "aligned") continue;
            pairOf[i] = j;
            nameDiffers[i] = true;
            hostTaken[j] = true;
            break;
        }
    }

    Func<Element, string> monitorWords = datum =>
    {
        List<ElementId> ids;
        if (datum == null || !monitorsOf.TryGetValue(datum.Id, out ids)) return "";
        if (ids.Contains(placement.Id)) return "copy-monitored from this link";
        return "copy-monitored from another link";
    };

    for (var i = 0; i < link.Count; i++)
    {
        var l = link[i];
        if (pairOf[i] < 0)
        {
            if (monitorOnly) continue;
            if (tilted)
            {
                notChecked++;
                rows.Add(string.Format("{0} | {1} | not checked | - | - | no {0} of that name here, and {2} | {3}",
                    l.Item2, l.Item1, tiltReason, against));
            }
            else
            {
                linkOnly++;
                rows.Add(string.Format("{0} | {1} | link only | - | - | no {0} here has that name or lines up with it | {2}",
                    l.Item2, l.Item1, against));
            }
            continue;
        }

        var h = host[pairOf[i]];
        var result = compare(h, l, tilted);
        var why = result.Item4;
        if (nameDiffers[i])
            why = "name differs - the link calls it '" + l.Item1 + "'" + (why.Length > 0 ? "; " + why : "");
        var watched = monitorWords(h.Item3);
        if (watched.Length > 0) why = watched + (why.Length > 0 ? "; " + why : "");

        if (result.Item1 == "aligned") { aligned++; compared++; }
        else if (result.Item1 == "off")
        {
            failures++;
            compared++;
            if (!failingIds.Contains(h.Item3.Id))
            {
                failingIds.Add(h.Item3.Id);
                failingElements.Add(h.Item3);
            }
        }
        else notChecked++;

        rows.Add(string.Format("{0} | {1} | {2} | {3} | {4} | {5} | {6}",
            h.Item2, h.Item1, result.Item1, mm(result.Item2), deg(result.Item3), why, against));
    }

    for (var j = 0; j < host.Count; j++)
    {
        if (hostTaken[j] || notInThis[j]) continue;
        var h = host[j];
        var watched = monitorWords(h.Item3);
        if (tilted)
        {
            notChecked++;
            rows.Add(string.Format("{0} | {1} | not checked | - | - | no {0} of that name in the link, and {2} | {3}",
                h.Item2, h.Item1, tiltReason, against));
        }
        else
        {
            hostOnly++;
            rows.Add(string.Format("{0} | {1} | host only | - | - | {2}no {0} in the link has that name or lines up with it | {3}",
                h.Item2, h.Item1, watched.Length > 0 ? watched + "; " : "", against));
        }
    }

    // Version 6's level lines, by NAME only, through this placement.
    if (monitorOnly) continue;
    var linkLevels = new List<Level>();
    try
    {
        foreach (var lv in new FilteredElementCollector(linked).OfClass(typeof(Level)).Cast<Level>())
            if (lv != null) linkLevels.Add(lv);
    }
    catch (Exception) { }
    linkLevels = linkLevels.OrderBy(lv => lv.ProjectElevation).ToList();

    var named = 0;
    var disagree = 0;
    var unmatched = 0;
    var levelLines = new List<string>();
    foreach (var lv in linkLevels)
    {
        var here = placed.OfPoint(new XYZ(0, 0, lv.ProjectElevation)).Z;
        Level mine;
        if (!hostByName.TryGetValue(lv.Name, out mine))
        {
            unmatched++;
            levelLines.Add(string.Format("  {0} - '{1}' at {2:0.0} mm here (its own level head reads {3:0.0} mm): "
                + "NO level of that name in this model",
                linked.Title, lv.Name, here * 304.8, lv.Elevation * 304.8));
            continue;
        }
        named++;
        var difference = here - mine.ProjectElevation;
        var off = Math.Abs(difference) * MmPerFoot > toleranceMm;
        if (off) disagree++;
        levelLines.Add(string.Format("  {0} - '{1}' at {2:0.0} mm here (its own level head reads {5:0.0} mm); "
            + "this model's at {3:0.0} mm - {4}",
            linked.Title, lv.Name, here * 304.8, mine.ProjectElevation * 304.8,
            off ? string.Format("DIFFERS by {0:0.0} mm", difference * 304.8) : "same height",
            lv.Elevation * 304.8));
    }
    linkedTotal += disagree;
    linkedMatches.Add(string.Format("{0} ('{5}'): {1} level(s), {2} named like one here and {3} of those at "
        + "a different height, {4} with no level of that name here",
        linked.Title, linkLevels.Count, named, disagree, unmatched, placement.Name));
    linkedMatches.AddRange(levelLines);
}

linkUsed = string.Join("; ", usedNames.ToArray());
if (refused == null && readLinks && compared == 0 && notChecked == 0 && linksSearched == 0 && linkRefusals.Count > 0)
    refused = string.Join(" ", linkRefusals.ToArray());
if (refused != null) linkBlocked = refused;

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!readLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} linked level(s) at a different height from this model's level of the same name, NOT selected",
        linksSearched, linkedTotal));
if (linkBlocked.Length == 0)
    foreach (var why in linkRefusals) linkedMatches.Add(why);

if (readLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");

// ---- The check's answer, after the levels' own lines in `findings` ---------------

if (!readLinks)
{
    findings.Add("Grids and levels were NOT checked against a link - links were not asked for (includeLinks, "
        + "or name one with linkName).");
}
else if (refused != null)
{
    findings.Add(refused + " NOTHING WAS COMPARED.");
}
else
{
    report = string.Join("\n", rows.ToArray());
    var what = doGrids && doLevels ? "grids and levels" : doGrids ? "grids" : "levels";
    findings.Add(string.Format("{0} checked against {1}: {2} pair(s) compared - {3} aligned within {4} mm "
        + "and {5} degrees, {6} off; {7} not checked, {8} only in this model, {9} only in the link.",
        what, usedNames.Count == 0 ? "no link" : "'" + linkUsed + "'", compared, aligned,
        toleranceMm.ToString(invariant), angleToleranceDeg.ToString(invariant), failures, notChecked,
        hostOnly, linkOnly));
    if (failures > 0)
        findings.Add(string.Format("Off: {0}. Their ids are in failingIds, to select.", string.Join(", ",
            failingElements.Select(e => "'" + (e.Name ?? "") + "'").ToArray())));
    foreach (var why in linkRefusals) findings.Add(why);
    if (tiltedLinks.Count > 0)
        findings.Add(string.Format("Placed TILTED, so no grid or level was compared against it: {0}.",
            string.Join(", ", tiltedLinks.Select(n => "'" + n + "'").ToArray())));
    findings.Add("Grids are measured in plan from the link grid's start point and by angle; levels by "
        + "their height in this model's space, the link's placement included.");
    if (nestedLinks > 0)
        findings.Add(string.Format("{0} link placement(s) nested inside those links were NOT read.", nestedLinks));
    if (compared == 0)
        findings.Add("Nothing was compared, so no figure here says the datums line up.");
}
