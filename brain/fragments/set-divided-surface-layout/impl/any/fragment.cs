// NOT STANDALONE. Assumes `doc`, `dividedSurface` and `surfaceLayout` are in
// scope; leaves `applied`, `alreadyAsAsked`, `readBack`, `skipped`,
// `surfaceId`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// CHANGES A DIVIDED SURFACE THAT IS ALREADY THERE - the settings its
// Properties list under U Grid and V Grid, and the surface's own: All Grid
// Rotation, Border Tile, Component Rotation, Component Flip and Mirror, the
// two pattern indents, and the levels and planes that cut it.
// DIVIDE_FAMILY_SURFACE makes one - both grids centred, unrotated, with no
// offset - and refuses a face already divided; this changes it afterwards.
//
// ONE SURFACE. Named by its UniqueId (DIVIDE_FAMILY_SURFACE's surfaceId) or
// by the id REPORT_DIVIDED_SURFACES prints - compared as TEXT, never read as a
// number - or, with none named ("only"), the only one in the family. Several
// and none named is refused with their ids; the form's own id is refused with
// the surfaces on it.
//
// ONLY WHAT IS NAMED CHANGES. Revit sets a grid's five parts together -
// layout, number or distance, justification, rotation, offset - through
// SpacingRule.SetLayout*, so the parts not named are read first and handed
// back exactly as they were. A grid nothing names is not written at all, and
// neither is a surface setting nothing names.
//
// NOTHING IS WRITTEN UNTIL THE WHOLE REQUEST IS SETTLED: every refusal is
// made before the first write. Then the writes, one rebuild, and a READ BACK
// off Revit afresh: every part written must read as written, or the call
// THROWS and the host rolls the whole call back. A part NOT written that Revit
// moved anyway is reported in `readBack` and `findings`, never hidden and
// never counted as asked.
//
// UNITS (D-71): the modeller's lengths are MILLIMETRES, turned into Revit's
// feet once, at the write (/ 304.8), and back once, at the read. Angles are
// DEGREES, turned into radians once. The bounds are for a number typed in the
// wrong unit, not a design rule: 1.5 meant as metres would put a gridline every
// 1.5 mm across the surface.

const double MillimetresPerFoot = 304.8;
const double SmallestDistanceMm = 100.0;
const double LargestDistanceMm = 100000.0;
const double SmallestOffsetMm = 10.0;
const double LargestOffsetMm = 100000.0;
const double LargestAngleDegrees = 360.0;
const int MostDivisions = 1000;
const int MostIndent = 1000;
// How far a value read back may sit from the one written and still be it.
const double SameMm = 0.5;
const double SameDegrees = 0.01;

var applied = 0;
var alreadyAsAsked = 0;
var readBack = "";
var skipped = new List<string>();
var surfaceId = "";
var notAFamily = false;
var refused = "";
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

// Lower case, one space between words. These are THIS file's words - setting
// names, layouts, yes and no - so their spelling is loosened here; no name the
// model owns is changed by it.
Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

// A NUMBER, with at most the one unit it is already in written after it -
// "1500" or "1500 mm", "45" or "45 deg". NaN for anything else, "1.5 m" too.
Func<string, string[], double> numberOf = (text, units) =>
{
    var t = plain(text);
    foreach (var unit in units)
        if (t.EndsWith(unit, StringComparison.Ordinal)) { t = t.Substring(0, t.Length - unit.Length).Trim(); break; }
    double value;
    return double.TryParse(t, System.Globalization.NumberStyles.Float, invariant, out value)
        && !double.IsNaN(value) && !double.IsInfinity(value) ? value : double.NaN;
};
var mmUnits = new[] { "mm" };
var degreeUnits = new[] { "degrees", "degree", "deg", "°" };
var noUnits = new string[0];

// Rounded, then + 0.0: a -0.0 becomes 0.0, so a value Revit holds as a hair
// below zero never reads as "-0" - which net8 and net10 print, and which would
// look like a setting that moved.
Func<double, string> mmText = feet =>
    (Math.Round(feet * MillimetresPerFoot, 1) + 0.0).ToString("F1", invariant) + " mm";
Func<double, string> degreesText = radians =>
    (Math.Round(radians * 180.0 / Math.PI, 2) + 0.0).ToString(invariant) + " degrees";
Func<double, string> number0 = value => value.ToString("F0", invariant);

// Two angles the same, a whole turn apart or not - Revit may keep -45 as 315.
Func<double, double, bool> sameAngle = (a, b) =>
{
    var turn = 2 * Math.PI;
    var gap = Math.Abs((a - b) % turn);
    var near = SameDegrees * Math.PI / 180.0;
    return gap < near || turn - gap < near;
};
Func<double, double, bool> sameLength = (a, b) => Math.Abs(a - b) < SameMm / MillimetresPerFoot;

// THE WORDS, beside Revit's own enum members - cast from the enum, never typed
// as its number. Read off all eight releases' assemblies on 2026-10-28.
var layoutWords = new Dictionary<string, SpacingRuleLayout>
{
    { "none", SpacingRuleLayout.None },
    { "number", SpacingRuleLayout.FixedNumber }, { "fixed number", SpacingRuleLayout.FixedNumber },
    { "distance", SpacingRuleLayout.FixedDistance }, { "fixed distance", SpacingRuleLayout.FixedDistance },
    { "maximum spacing", SpacingRuleLayout.MaximumSpacing }, { "maximum", SpacingRuleLayout.MaximumSpacing },
    { "minimum spacing", SpacingRuleLayout.MinimumSpacing }, { "minimum", SpacingRuleLayout.MinimumSpacing },
};
var layoutShown = new Dictionary<SpacingRuleLayout, string>
{
    { SpacingRuleLayout.None, "None" }, { SpacingRuleLayout.FixedNumber, "Fixed Number" },
    { SpacingRuleLayout.FixedDistance, "Fixed Distance" },
    { SpacingRuleLayout.MaximumSpacing, "Maximum Spacing" },
    { SpacingRuleLayout.MinimumSpacing, "Minimum Spacing" },
};
Func<SpacingRuleLayout, string> sayLayout = layout =>
    layoutShown.ContainsKey(layout) ? layoutShown[layout] : layout.ToString();
Func<SpacingRuleLayout, bool> usesDistance = layout =>
    layout == SpacingRuleLayout.FixedDistance || layout == SpacingRuleLayout.MaximumSpacing
    || layout == SpacingRuleLayout.MinimumSpacing;
var justificationWords = new Dictionary<string, SpacingRuleJustification>
{
    { "center", SpacingRuleJustification.Center }, { "centre", SpacingRuleJustification.Center },
    { "beginning", SpacingRuleJustification.Beginning }, { "start", SpacingRuleJustification.Beginning },
    { "end", SpacingRuleJustification.End },
};
var borderWords = new Dictionary<string, BorderTile>
{
    { "empty", BorderTile.Empty }, { "partial", BorderTile.Partial }, { "overhanging", BorderTile.Overhanging },
};
// The modeller's degrees, beside Revit's member for each.
var componentRotations = new Dictionary<int, ComponentRotation>
{
    { 0, ComponentRotation.Angle0 }, { 90, ComponentRotation.Angle90 },
    { 180, ComponentRotation.Angle180 }, { 270, ComponentRotation.Angle270 },
};
Func<ComponentRotation, string> sayComponentRotation = rotation =>
    componentRotations.Where(p => p.Value == rotation).Select(p => p.Key + " degrees")
        .DefaultIfEmpty(rotation.ToString()).First();
var yesWords = new[] { "yes", "true", "on" };
var noWords = new[] { "no", "false", "off" };
Func<bool, string> yesNo = flag => flag ? "yes" : "no";

// THE SETTINGS THIS CHANGES, by every name it answers to. "u grid layout"
// reads as "u layout" (settingOf below).
var settingNames = new Dictionary<string, string>
{
    { "u layout", "u layout" }, { "v layout", "v layout" },
    { "u number", "u number" }, { "v number", "v number" },
    { "u distance", "u distance" }, { "u spacing", "u distance" },
    { "v distance", "v distance" }, { "v spacing", "v distance" },
    { "u justification", "u justification" }, { "v justification", "v justification" },
    { "u rotation", "u rotation" }, { "v rotation", "v rotation" },
    { "u offset", "u offset" }, { "v offset", "v offset" },
    { "u indent", "u indent" }, { "v indent", "v indent" },
    { "all rotation", "all rotation" },
    { "border", "border" }, { "border tile", "border" }, { "border tiles", "border" },
    { "component rotation", "component rotation" },
    { "flip", "flip" }, { "component flip", "flip" },
    { "mirror", "mirror" }, { "component mirror", "mirror" },
    { "intersect", "intersect" }, { "intersects", "intersect" },
};
var settingList = "u layout, u number, u distance, u justification, u rotation, u offset, u indent, the same "
    + "seven for v, all rotation, border, component rotation, flip, mirror, intersect";
Func<string, string> settingOf = key =>
{
    var k = plain((key ?? "").Replace('_', ' ').Replace('-', ' '));
    foreach (var lead in new[] { "u", "v", "all" })
        if (k.StartsWith(lead + " grid ")) k = lead + " " + k.Substring(lead.Length + 6);
    return settingNames.ContainsKey(k) ? settingNames[k] : null;
};

// HOW THINGS ARE NAMED BACK.
Func<Element, string> hostWords = host => host == null ? "no host"
    : (host is Form ? "form " : host.Category != null ? host.Category.Name + " " : "element ") + host.Id;
Func<DividedSurface, string> surfaceWords = s =>
    "divided surface " + s.Id + " on " + hostWords(s.Host) + " (" + s.UniqueId + ")";
Func<Element, string> elementWords = e =>
    e is Level ? "level " + e.Name
    : e is ReferencePlane ? (string.IsNullOrEmpty(e.Name) ? "unnamed reference plane " + e.Id : "reference plane " + e.Name)
    : (e.Category != null ? e.Category.Name + " " : "element ") + e.Id;
Func<ElementId, string> cutWords = id =>
{
    var e = doc.GetElement(id);
    return e == null ? "element " + id : elementWords(e);
};

// ---------------------------------------------------------------------------
// WHAT WAS ASKED, READ WHOLE BEFORE ANYTHING IS TOUCHED
// ---------------------------------------------------------------------------

var letters = new[] { "U", "V" };
var askLayout = new SpacingRuleLayout?[2];
var askNumber = new int?[2];
var askDistanceMm = new double?[2];
var askJustification = new SpacingRuleJustification?[2];
var askRotationDegrees = new double?[2];
var askOffsetMm = new double?[2];
var askIndent = new int?[2];
double? askAllRotationDegrees = null;
BorderTile? askBorder = null;
ComponentRotation? askComponentRotation = null;
bool? askFlip = null;
bool? askMirror = null;
string askIntersect = null;

DividedSurface surface = null;
var surfaces = new List<DividedSurface>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A divided surface is changed inside the mass or pattern-based family that holds it - open that family "
        + "first. Nothing was changed";
}
else
{
    foreach (DividedSurface each in new FilteredElementCollector(doc).OfClass(typeof(DividedSurface)))
        surfaces.Add(each);
    var said = (dividedSurface ?? "").Trim();
    var wantsOnly = said.Length == 0 || plain(said) == "only" || plain(said) == "the only one";
    if (surfaces.Count == 0)
        refused = "This family has no divided surface. DIVIDE_FAMILY_SURFACE divides a face of a form first. "
            + "Nothing was changed";
    else if (wantsOnly)
    {
        if (surfaces.Count == 1) surface = surfaces[0];
        else
            refused = "This family has " + surfaces.Count + " divided surfaces and none was named - name one by its "
                + "id: " + string.Join("; ", surfaces.Take(20).Select(surfaceWords)) + ". Nothing was changed";
    }
    else
    {
        surface = surfaces.FirstOrDefault(s => s.UniqueId == said || s.Id.ToString() == said);
        if (surface == null)
        {
            var onHost = surfaces.Where(s => s.Host != null
                && (s.Host.UniqueId == said || s.Host.Id.ToString() == said)).ToList();
            refused = onHost.Count > 0
                ? "\"" + said + "\" is " + hostWords(onHost[0].Host) + ", which the surface is on - not the divided "
                    + "surface itself. The divided surface(s) on it: " + string.Join("; ", onHost.Select(surfaceWords))
                    + ". Nothing was changed"
                : "\"" + said + "\" is not the id of a divided surface in this family. Its divided surface(s): "
                    + string.Join("; ", surfaces.Take(20).Select(surfaceWords)) + ". Nothing was changed";
        }
    }
}

if (refused.Length == 0 && (surfaceLayout == null || surfaceLayout.Count == 0))
    refused = "Nothing was named to change. Name each setting and what it becomes - \"all rotation=45\" for a "
        + "diamond pattern, \"u layout=number; u number=20\". Nothing was changed";

if (refused.Length == 0)
{
    var seen = new List<string>();
    foreach (var pair in surfaceLayout)
    {
        var setting = settingOf(pair.Key);
        var value = (pair.Value ?? "").Trim();
        var word = plain(value);
        if (setting == null)
        {
            refused = "\"" + pair.Key + "\" is not a setting this changes. Name one or more of: " + settingList
                + ". Nothing was changed";
            break;
        }
        if (seen.Contains(setting))
        {
            refused = "\"" + setting + "\" is named twice, and only one of the two could be kept. Say it once. "
                + "Nothing was changed";
            break;
        }
        seen.Add(setting);
        if (value.Length == 0)
        {
            refused = "\"" + pair.Key + "\" is given no value. Nothing was changed";
            break;
        }

        var g = setting.StartsWith("u ") ? 0 : setting.StartsWith("v ") ? 1 : -1;
        var part = g >= 0 ? setting.Substring(2) : setting;
        var problem = "";

        if (part == "layout")
        {
            if (layoutWords.ContainsKey(word)) askLayout[g] = layoutWords[word];
            else problem = "\"" + value + "\" is not a layout. The " + letters[g] + " layout is one of: none, number, "
                + "distance, maximum spacing, minimum spacing";
        }
        else if (part == "number")
        {
            var n = numberOf(value, noUnits);
            if (double.IsNaN(n) || n != Math.Floor(n) || n < 1 || n > MostDivisions)
                problem = "\"" + value + "\" is not a whole number of " + letters[g] + " divisions from 1 to "
                    + MostDivisions;
            else askNumber[g] = (int)n;
        }
        else if (part == "distance")
        {
            var mm = numberOf(value, mmUnits);
            if (double.IsNaN(mm))
                problem = "\"" + value + "\" is not a distance. Give the " + letters[g] + " distance in millimetres - "
                    + "1500, not 1.5 m";
            else if (mm < SmallestDistanceMm || mm > LargestDistanceMm)
                problem = "A " + letters[g] + " distance of " + value + " is outside what this sets - "
                    + number0(SmallestDistanceMm) + " to " + number0(LargestDistanceMm) + " mm. The distance is in "
                    + "MILLIMETRES, so 1.5 m is 1500";
            else askDistanceMm[g] = mm;
        }
        else if (part == "justification")
        {
            if (justificationWords.ContainsKey(word)) askJustification[g] = justificationWords[word];
            else problem = "\"" + value + "\" is not a justification. The " + letters[g] + " justification is one of: "
                + "center, beginning, end";
        }
        else if (part == "rotation")
        {
            var a = numberOf(value, degreeUnits);
            if (double.IsNaN(a) || Math.Abs(a) > LargestAngleDegrees)
                problem = "\"" + value + "\" is not a " + letters[g] + " rotation - degrees, from -360 to 360";
            else askRotationDegrees[g] = a;
        }
        else if (part == "offset")
        {
            var mm = numberOf(value, mmUnits);
            if (double.IsNaN(mm))
                problem = "\"" + value + "\" is not an offset. Give the " + letters[g] + " offset in millimetres - "
                    + "750, not 0.75 m";
            else if (mm != 0 && (Math.Abs(mm) < SmallestOffsetMm || Math.Abs(mm) > LargestOffsetMm))
                problem = "A " + letters[g] + " offset of " + value + " is outside what this sets - 0, or "
                    + number0(SmallestOffsetMm) + " to " + number0(LargestOffsetMm) + " mm either way. The offset is "
                    + "in MILLIMETRES, so 0.75 m is 750";
            else askOffsetMm[g] = mm;
        }
        else if (part == "indent")
        {
            var n = numberOf(value, noUnits);
            if (double.IsNaN(n) || n != Math.Floor(n) || n < 0 || n > MostIndent)
                problem = "\"" + value + "\" is not a " + letters[g] + " indent - a whole number from 0 to " + MostIndent;
            else askIndent[g] = (int)n;
        }
        else if (setting == "all rotation")
        {
            var a = numberOf(value, degreeUnits);
            if (double.IsNaN(a) || Math.Abs(a) > LargestAngleDegrees)
                problem = "\"" + value + "\" is not an all grid rotation - degrees, from -360 to 360; 45 makes the "
                    + "diamond pattern";
            else askAllRotationDegrees = a;
        }
        else if (setting == "border")
        {
            if (borderWords.ContainsKey(word)) askBorder = borderWords[word];
            else problem = "\"" + value + "\" is not a border tile setting - empty, partial or overhanging";
        }
        else if (setting == "component rotation")
        {
            var a = numberOf(value, degreeUnits);
            if (double.IsNaN(a) || a != Math.Floor(a) || !componentRotations.ContainsKey((int)a))
                problem = "\"" + value + "\" is not a component rotation - 0, 90, 180 or 270 degrees";
            else askComponentRotation = componentRotations[(int)a];
        }
        else if (setting == "flip" || setting == "mirror")
        {
            bool? flag = yesWords.Contains(word) ? true : noWords.Contains(word) ? false : (bool?)null;
            if (!flag.HasValue) problem = "\"" + value + "\" is not yes or no, for " + setting;
            else if (setting == "flip") askFlip = flag;
            else askMirror = flag;
        }
        else if (setting == "intersect")
        {
            askIntersect = value;
        }

        if (problem.Length > 0)
        {
            refused = problem + ". Nothing was changed";
            break;
        }
    }
}

// ---------------------------------------------------------------------------
// WHAT REVIT HOLDS NOW, AND WHAT EACH PART WILL BE
// ---------------------------------------------------------------------------

var rules = new SpacingRule[2];
var layoutBefore = new SpacingRuleLayout[2];
var numberBefore = new int[2];
var distanceBefore = new double[2];      // feet
var justificationBefore = new SpacingRuleJustification[2];
var rotationBefore = new double[2];      // radians
var offsetBefore = new double[2];        // feet
var linesBefore = new int[2];
var indentBefore = new int[2];
var allRotationBefore = 0.0;
var borderBefore = BorderTile.Partial;
var componentRotationBefore = ComponentRotation.Angle0;
var flipBefore = false;
var mirrorBefore = false;
var cutsBefore = new List<ElementId>();

if (refused.Length == 0)
{
    rules[0] = surface.USpacingRule;
    rules[1] = surface.VSpacingRule;
    for (var d = 0; d < 2; d++)
    {
        layoutBefore[d] = rules[d].Layout;
        // A part the layout does not use may not read at all; the default
        // below is what DIVIDE_FAMILY_SURFACE writes, and is handed back only
        // when this request gives that grid a layout that uses the part.
        try { numberBefore[d] = rules[d].Number; } catch (Exception) { numberBefore[d] = 0; }
        try { distanceBefore[d] = rules[d].Distance; } catch (Exception) { distanceBefore[d] = double.NaN; }
        try { justificationBefore[d] = rules[d].Justification; }
        catch (Exception) { justificationBefore[d] = SpacingRuleJustification.Center; }
        try { rotationBefore[d] = rules[d].GridlinesRotation; } catch (Exception) { rotationBefore[d] = 0.0; }
        try { offsetBefore[d] = rules[d].Offset; } catch (Exception) { offsetBefore[d] = 0.0; }
    }
    linesBefore[0] = surface.NumberOfUGridlines;
    linesBefore[1] = surface.NumberOfVGridlines;
    indentBefore[0] = surface.UPatternIndent;
    indentBefore[1] = surface.VPatternIndent;
    allRotationBefore = surface.AllGridRotation;
    borderBefore = surface.BorderTile;
    componentRotationBefore = surface.ComponentRotation;
    flipBefore = surface.IsComponentFlipped;
    mirrorBefore = surface.IsComponentMirrored;
    cutsBefore = surface.GetAllIntersectionElements().ToList();
}

var layoutTarget = new SpacingRuleLayout[2];
var numberTarget = new int[2];
var distanceTarget = new double[2];
var justificationTarget = new SpacingRuleJustification[2];
var rotationTarget = new double[2];
var offsetTarget = new double[2];
var writeGrid = new bool[2];

// A PART ONLY MEANS SOMETHING UNDER A LAYOUT THAT USES IT. A number under a
// distance layout, or a rotation under None, would be a write that changes
// nothing reported as one that did - so it is refused, and the layout asked
// for by name. A layout switched to one that needs a value it does not hold
// yet must be given that value in the same request.
for (var d = 0; d < 2 && refused.Length == 0; d++)
{
    var letter = letters[d];
    var lower = letter.ToLowerInvariant();
    layoutTarget[d] = askLayout[d] ?? layoutBefore[d];
    var keepsDistance = usesDistance(layoutBefore[d]) && !double.IsNaN(distanceBefore[d]) && distanceBefore[d] > 0;

    if (askNumber[d].HasValue && layoutTarget[d] != SpacingRuleLayout.FixedNumber)
        refused = "A " + letter + " number does nothing while the " + letter + " layout is " + sayLayout(layoutTarget[d])
            + ". Say \"" + lower + " layout=number\" as well";
    else if (askDistanceMm[d].HasValue && !usesDistance(layoutTarget[d]))
        refused = "A " + letter + " distance does nothing while the " + letter + " layout is "
            + sayLayout(layoutTarget[d]) + ". Say the layout as well - \"" + lower + " layout=distance\", maximum "
            + "spacing or minimum spacing";
    else if (layoutTarget[d] == SpacingRuleLayout.None
        && (askJustification[d].HasValue || askRotationDegrees[d].HasValue || askOffsetMm[d].HasValue))
        refused = "The " + letter + " layout " + (askLayout[d].HasValue ? "asked for" : "is") + " None, so the " + letter
            + " grid has no gridlines to justify, rotate or offset. Name a layout that has them, or leave those out";
    else if (layoutTarget[d] == SpacingRuleLayout.FixedNumber && !askNumber[d].HasValue
        && layoutBefore[d] != SpacingRuleLayout.FixedNumber)
        refused = "A " + letter + " layout of Number needs the number - \"" + lower + " number=20\"";
    else if (usesDistance(layoutTarget[d]) && !askDistanceMm[d].HasValue && !keepsDistance)
        refused = "A " + letter + " layout of " + sayLayout(layoutTarget[d]) + " needs a distance - \"" + lower
            + " distance=1500\"";
    if (refused.Length > 0)
    {
        refused += ". Nothing was changed";
        break;
    }

    numberTarget[d] = askNumber[d] ?? numberBefore[d];
    distanceTarget[d] = askDistanceMm[d].HasValue ? askDistanceMm[d].Value / MillimetresPerFoot : distanceBefore[d];
    justificationTarget[d] = askJustification[d] ?? justificationBefore[d];
    rotationTarget[d] = askRotationDegrees[d].HasValue ? askRotationDegrees[d].Value * Math.PI / 180.0 : rotationBefore[d];
    offsetTarget[d] = askOffsetMm[d].HasValue ? askOffsetMm[d].Value / MillimetresPerFoot : offsetBefore[d];

    writeGrid[d] = layoutTarget[d] != layoutBefore[d]
        || (layoutTarget[d] == SpacingRuleLayout.FixedNumber && numberTarget[d] != numberBefore[d])
        || (usesDistance(layoutTarget[d]) && !sameLength(distanceTarget[d], distanceBefore[d]))
        || (layoutTarget[d] != SpacingRuleLayout.None
            && (justificationTarget[d] != justificationBefore[d] || !sameAngle(rotationTarget[d], rotationBefore[d])
                || !sameLength(offsetTarget[d], offsetBefore[d])));
}

var allRotationTarget = askAllRotationDegrees.HasValue ? askAllRotationDegrees.Value * Math.PI / 180.0 : allRotationBefore;
var borderTarget = askBorder ?? borderBefore;
var componentRotationTarget = askComponentRotation ?? componentRotationBefore;
var flipTarget = askFlip ?? flipBefore;
var mirrorTarget = askMirror ?? mirrorBefore;
var indentTarget = new[] { askIndent[0] ?? indentBefore[0], askIndent[1] ?? indentBefore[1] };

// THE CUTS. "levels" is every level of THIS family - a conceptual mass has its
// own - and "reference planes" every reference plane in it; any other entry is
// a level or reference plane by name, or an element by its id. Each one Revit
// says cannot cut the surface (CanBeIntersectionElement) is skipped and named;
// one already cutting it is left as it is. "none" alone removes every cut.
var toAdd = new List<ElementId>();
var alreadyCutting = new List<string>();
var clearCuts = false;
if (refused.Length == 0 && askIntersect != null)
{
    var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Element>().ToList();
    var planes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<Element>().ToList();
    var items = askIntersect.Split(',').Select(i => i.Trim()).Where(i => i.Length > 0).ToList();
    var wanted = new List<Element>();
    Func<string> theirNames = () => "Its levels: " + (levels.Count == 0 ? "none"
            : string.Join(", ", levels.Take(20).Select(e => e.Name))) + "; its named reference planes: "
        + (planes.Count(e => !string.IsNullOrEmpty(e.Name)) == 0 ? "none"
            : string.Join(", ", planes.Where(e => !string.IsNullOrEmpty(e.Name)).Take(20).Select(e => e.Name)));

    if (items.Count == 1 && plain(items[0]) == "none") clearCuts = true;
    else
    {
        foreach (var item in items)
        {
            var w = plain(item);
            if (w == "none")
            {
                refused = "\"none\" removes every cut and is said alone, not beside other elements";
                break;
            }
            if (w == "levels" || w == "every level" || w == "all levels")
            {
                if (levels.Count == 0) { refused = "This family has no level to cut the surface with"; break; }
                wanted.AddRange(levels);
                continue;
            }
            if (w == "reference planes" || w == "every reference plane" || w == "all reference planes")
            {
                if (planes.Count == 0) { refused = "This family has no reference plane to cut the surface with"; break; }
                wanted.AddRange(planes);
                continue;
            }
            var byName = levels.Concat(planes)
                .Where(e => string.Equals((e.Name ?? "").Trim(), item, StringComparison.OrdinalIgnoreCase)).ToList();
            if (byName.Count > 1)
            {
                refused = "\"" + item + "\" names " + byName.Count + " levels or reference planes - name it by its id";
                break;
            }
            Element found = byName.Count == 1 ? byName[0] : null;
            if (found == null)
            {
                // A UniqueId - or any other text, which finds nothing.
                try { found = doc.GetElement(item); } catch (Exception) { found = null; }
            }
            if (found == null)
                found = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                    .FirstOrDefault(e => e.Id.ToString() == item);
            if (found == null)
            {
                refused = "\"" + item + "\" is not a level, a reference plane or an element id in this family. "
                    + theirNames();
                break;
            }
            wanted.Add(found);
        }
        if (refused.Length == 0)
        {
            foreach (var e in wanted.GroupBy(one => one.Id).Select(group => group.First()))
            {
                if (cutsBefore.Contains(e.Id)) alreadyCutting.Add(elementWords(e));
                else if (e.Id == surface.Id || (surface.Host != null && e.Id == surface.Host.Id))
                    skipped.Add(elementWords(e) + " - the surface itself, or the form it is on");
                else if (!surface.CanBeIntersectionElement(e.Id))
                    skipped.Add(elementWords(e) + " - Revit says it cannot cut this surface");
                else toAdd.Add(e.Id);
            }
            if (toAdd.Count == 0 && alreadyCutting.Count == 0)
                refused = "Nothing named can cut this surface: " + string.Join("; ", skipped);
        }
    }
    if (refused.Length > 0) refused += ". Nothing was changed";
}

// ---------------------------------------------------------------------------
// THE WRITES - only what differs from what Revit already holds
// ---------------------------------------------------------------------------

var wrote = false;
if (refused.Length == 0)
{
    Action<string, Action> write = (what, act) =>
    {
        try { act(); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not take " + what + " on " + surfaceWords(surface) + ": "
                + ex.Message + " Nothing this call did is kept.");
        }
        wrote = true;
    };

    for (var d = 0; d < 2; d++)
    {
        if (!writeGrid[d]) continue;
        var rule = rules[d];
        var at = d;
        var what = "the " + letters[d] + " grid as " + sayLayout(layoutTarget[d]);
        if (layoutTarget[d] == SpacingRuleLayout.None)
            write(what, () => rule.SetLayoutNone());
        else if (layoutTarget[d] == SpacingRuleLayout.FixedNumber)
            write(what + " " + numberTarget[d], () => rule.SetLayoutFixedNumber(
                numberTarget[at], justificationTarget[at], rotationTarget[at], offsetTarget[at]));
        else if (layoutTarget[d] == SpacingRuleLayout.FixedDistance)
            write(what + " " + mmText(distanceTarget[d]), () => rule.SetLayoutFixedDistance(
                distanceTarget[at], justificationTarget[at], rotationTarget[at], offsetTarget[at]));
        else if (layoutTarget[d] == SpacingRuleLayout.MaximumSpacing)
            write(what + " " + mmText(distanceTarget[d]), () => rule.SetLayoutMaximumSpacing(
                distanceTarget[at], justificationTarget[at], rotationTarget[at], offsetTarget[at]));
        else if (layoutTarget[d] == SpacingRuleLayout.MinimumSpacing)
            write(what + " " + mmText(distanceTarget[d]), () => rule.SetLayoutMinimumSpacing(
                distanceTarget[at], justificationTarget[at], rotationTarget[at], offsetTarget[at]));
    }

    if (!sameAngle(allRotationTarget, allRotationBefore))
        write("an all grid rotation of " + degreesText(allRotationTarget), () => surface.AllGridRotation = allRotationTarget);
    if (borderTarget != borderBefore)
        write("border tiles " + borderTarget, () => surface.BorderTile = borderTarget);
    if (componentRotationTarget != componentRotationBefore)
        write("a component rotation of " + sayComponentRotation(componentRotationTarget),
            () => surface.ComponentRotation = componentRotationTarget);
    if (flipTarget != flipBefore)
        write("component flip " + yesNo(flipTarget), () => surface.IsComponentFlipped = flipTarget);
    if (mirrorTarget != mirrorBefore)
        write("component mirror " + yesNo(mirrorTarget), () => surface.IsComponentMirrored = mirrorTarget);
    if (indentTarget[0] != indentBefore[0])
        write("a U indent of " + indentTarget[0], () => surface.UPatternIndent = indentTarget[0]);
    if (indentTarget[1] != indentBefore[1])
        write("a V indent of " + indentTarget[1], () => surface.VPatternIndent = indentTarget[1]);
    if (clearCuts && cutsBefore.Count > 0)
        write("removing every cut", () => surface.RemoveAllIntersectionElements());
    foreach (var id in toAdd)
    {
        var one = id;
        write("a cut by " + cutWords(one), () => surface.AddIntersectionElement(one));
    }

    // THE REBUILD IS NOT SWALLOWED. A failure there leaves a model that must
    // not be read again, so it is thrown straight back.
    if (wrote)
    {
        try { doc.Regenerate(); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit could not rebuild " + surfaceWords(surface) + " with these "
                + "settings - " + ex.Message + " Nothing this call did is kept.");
        }
    }
}

// ---------------------------------------------------------------------------
// READ BACK, off Revit afresh - what it holds, never what was written
// ---------------------------------------------------------------------------

if (refused.Length == 0)
{
    var wrong = new List<string>();
    var moved = new List<string>();
    var rows = new List<string>();

    // One part's words: what Revit holds now, and how it came to - asked and
    // changed, asked and already so, or left as it was. A part not asked that
    // reads differently now is MOVED, and said so.
    Func<string, string, string, bool, bool, string> part = (label, nowText, beforeText, asked, changed) =>
    {
        if (!asked && nowText != beforeText) moved.Add(label + " was " + beforeText + " and now reads " + nowText);
        return label + " " + nowText + (asked
            ? (changed ? " (asked, was " + beforeText + ")" : " (asked, already so)")
            : (nowText == beforeText ? "" : " (NOT asked - was " + beforeText + ")"));
    };
    Action<bool, bool> count = (asked, changed) =>
    {
        if (!asked) return;
        if (changed) applied++; else alreadyAsAsked++;
    };

    var nowRules = new[] { surface.USpacingRule, surface.VSpacingRule };
    var linesNow = new[] { surface.NumberOfUGridlines, surface.NumberOfVGridlines };
    for (var d = 0; d < 2; d++)
    {
        var letter = letters[d];
        var rule = nowRules[d];
        var layoutNow = rule.Layout;
        var numberNow = 0;
        var distanceNow = double.NaN;
        var justificationNow = SpacingRuleJustification.Center;
        var rotationNow = 0.0;
        var offsetNow = 0.0;
        try { numberNow = rule.Number; } catch (Exception) { numberNow = 0; }
        try { distanceNow = rule.Distance; } catch (Exception) { distanceNow = double.NaN; }
        try { justificationNow = rule.Justification; } catch (Exception) { justificationNow = SpacingRuleJustification.Center; }
        try { rotationNow = rule.GridlinesRotation; } catch (Exception) { rotationNow = 0.0; }
        try { offsetNow = rule.Offset; } catch (Exception) { offsetNow = 0.0; }

        // EVERY PART WRITTEN MUST READ AS WRITTEN - the asked ones and the
        // ones handed back as they were alike.
        if (writeGrid[d])
        {
            if (layoutNow != layoutTarget[d])
                wrong.Add(letter + " layout reads " + sayLayout(layoutNow) + ", written " + sayLayout(layoutTarget[d]));
            if (layoutTarget[d] == SpacingRuleLayout.FixedNumber && numberNow != numberTarget[d])
                wrong.Add(letter + " number reads " + numberNow + ", written " + numberTarget[d]);
            if (usesDistance(layoutTarget[d]) && !sameLength(distanceNow, distanceTarget[d]))
                wrong.Add(letter + " distance reads " + mmText(distanceNow) + ", written " + mmText(distanceTarget[d]));
            if (layoutTarget[d] != SpacingRuleLayout.None)
            {
                if (justificationNow != justificationTarget[d])
                    wrong.Add(letter + " justification reads " + justificationNow + ", written " + justificationTarget[d]);
                if (!sameAngle(rotationNow, rotationTarget[d]))
                    wrong.Add(letter + " rotation reads " + degreesText(rotationNow) + ", written "
                        + degreesText(rotationTarget[d]));
                if (!sameLength(offsetNow, offsetTarget[d]))
                    wrong.Add(letter + " offset reads " + mmText(offsetNow) + ", written " + mmText(offsetTarget[d]));
            }
        }

        var parts = new List<string>();
        var layoutChanged = askLayout[d].HasValue && askLayout[d].Value != layoutBefore[d];
        parts.Add(part("layout", sayLayout(layoutNow), sayLayout(layoutBefore[d]), askLayout[d].HasValue, layoutChanged));
        count(askLayout[d].HasValue, layoutChanged);
        if (layoutNow == SpacingRuleLayout.FixedNumber || askNumber[d].HasValue)
        {
            var changed = askNumber[d].HasValue
                && (layoutBefore[d] != SpacingRuleLayout.FixedNumber || askNumber[d].Value != numberBefore[d]);
            parts.Add(part("number", numberNow.ToString(invariant),
                layoutBefore[d] == SpacingRuleLayout.FixedNumber ? numberBefore[d].ToString(invariant) : "none",
                askNumber[d].HasValue, changed));
            count(askNumber[d].HasValue, changed);
        }
        if (usesDistance(layoutNow) || askDistanceMm[d].HasValue)
        {
            var changed = askDistanceMm[d].HasValue && (!usesDistance(layoutBefore[d])
                || !sameLength(askDistanceMm[d].Value / MillimetresPerFoot, distanceBefore[d]));
            parts.Add(part("distance", mmText(distanceNow),
                usesDistance(layoutBefore[d]) ? mmText(distanceBefore[d]) : "none", askDistanceMm[d].HasValue, changed));
            count(askDistanceMm[d].HasValue, changed);
        }
        if (layoutNow != SpacingRuleLayout.None)
        {
            var wasNone = layoutBefore[d] == SpacingRuleLayout.None;
            var justChanged = askJustification[d].HasValue
                && (wasNone || askJustification[d].Value != justificationBefore[d]);
            var rotationChanged = askRotationDegrees[d].HasValue
                && (wasNone || !sameAngle(rotationTarget[d], rotationBefore[d]));
            var offsetChanged = askOffsetMm[d].HasValue && (wasNone || !sameLength(offsetTarget[d], offsetBefore[d]));
            parts.Add(part("justification", justificationNow.ToString(), justificationBefore[d].ToString(),
                askJustification[d].HasValue, justChanged));
            parts.Add(part("rotation", degreesText(rotationNow), degreesText(rotationBefore[d]),
                askRotationDegrees[d].HasValue, rotationChanged));
            parts.Add(part("offset", mmText(offsetNow), mmText(offsetBefore[d]), askOffsetMm[d].HasValue, offsetChanged));
            count(askJustification[d].HasValue, justChanged);
            count(askRotationDegrees[d].HasValue, rotationChanged);
            count(askOffsetMm[d].HasValue, offsetChanged);
        }
        rows.Add(letter + " grid: " + string.Join(", ", parts) + "; " + linesBefore[d] + " gridline(s) before, "
            + linesNow[d] + " now");
    }

    // THE SURFACE'S OWN SETTINGS.
    var allRotationNow = surface.AllGridRotation;
    var borderNow = surface.BorderTile;
    var componentRotationNow = surface.ComponentRotation;
    var flipNow = surface.IsComponentFlipped;
    var mirrorNow = surface.IsComponentMirrored;
    var indentNow = new[] { surface.UPatternIndent, surface.VPatternIndent };

    if (!sameAngle(allRotationNow, allRotationTarget) && !sameAngle(allRotationTarget, allRotationBefore))
        wrong.Add("all grid rotation reads " + degreesText(allRotationNow) + ", written " + degreesText(allRotationTarget));
    if (borderNow != borderTarget && borderTarget != borderBefore)
        wrong.Add("border tiles read " + borderNow + ", written " + borderTarget);
    if (componentRotationNow != componentRotationTarget && componentRotationTarget != componentRotationBefore)
        wrong.Add("component rotation reads " + sayComponentRotation(componentRotationNow) + ", written "
            + sayComponentRotation(componentRotationTarget));
    if (flipNow != flipTarget && flipTarget != flipBefore)
        wrong.Add("component flip reads " + yesNo(flipNow) + ", written " + yesNo(flipTarget));
    if (mirrorNow != mirrorTarget && mirrorTarget != mirrorBefore)
        wrong.Add("component mirror reads " + yesNo(mirrorNow) + ", written " + yesNo(mirrorTarget));
    for (var d = 0; d < 2; d++)
        if (indentNow[d] != indentTarget[d] && indentTarget[d] != indentBefore[d])
            wrong.Add(letters[d] + " indent reads " + indentNow[d] + ", written " + indentTarget[d]);

    var allChanged = askAllRotationDegrees.HasValue && !sameAngle(allRotationTarget, allRotationBefore);
    var borderChanged = askBorder.HasValue && borderTarget != borderBefore;
    var componentChanged = askComponentRotation.HasValue && componentRotationTarget != componentRotationBefore;
    var flipChanged = askFlip.HasValue && flipTarget != flipBefore;
    var mirrorChanged = askMirror.HasValue && mirrorTarget != mirrorBefore;
    var surfaceParts = new List<string>
    {
        part("all grid rotation", degreesText(allRotationNow), degreesText(allRotationBefore),
            askAllRotationDegrees.HasValue, allChanged),
        part("border tiles", borderNow.ToString(), borderBefore.ToString(), askBorder.HasValue, borderChanged),
        part("component rotation", sayComponentRotation(componentRotationNow),
            sayComponentRotation(componentRotationBefore), askComponentRotation.HasValue, componentChanged),
        part("component flip", yesNo(flipNow), yesNo(flipBefore), askFlip.HasValue, flipChanged),
        part("component mirror", yesNo(mirrorNow), yesNo(mirrorBefore), askMirror.HasValue, mirrorChanged),
    };
    count(askAllRotationDegrees.HasValue, allChanged);
    count(askBorder.HasValue, borderChanged);
    count(askComponentRotation.HasValue, componentChanged);
    count(askFlip.HasValue, flipChanged);
    count(askMirror.HasValue, mirrorChanged);
    for (var d = 0; d < 2; d++)
    {
        var changed = askIndent[d].HasValue && indentTarget[d] != indentBefore[d];
        surfaceParts.Add(part(letters[d] + " indent", indentNow[d].ToString(invariant),
            indentBefore[d].ToString(invariant), askIndent[d].HasValue, changed));
        count(askIndent[d].HasValue, changed);
    }

    // THE CUTS, read again: each added one must be there, and with "none" no
    // cut may be left. One that was there and was not asked about must still be.
    var cutsNow = surface.GetAllIntersectionElements().ToList();
    if (clearCuts && cutsNow.Count > 0)
        wrong.Add("the surface is still cut by " + string.Join(", ", cutsNow.Select(cutWords)) + " after every cut "
            + "was removed");
    foreach (var id in toAdd)
        if (!cutsNow.Contains(id)) wrong.Add(cutWords(id) + " does not read as cutting the surface");
    if (!clearCuts)
        foreach (var id in cutsBefore)
            if (!cutsNow.Contains(id)) moved.Add(cutWords(id) + " cut the surface and no longer does");
    var cutsChanged = askIntersect != null && (toAdd.Count > 0 || (clearCuts && cutsBefore.Count > 0));
    count(askIntersect != null, cutsChanged);
    var cutText = cutsNow.Count == 0 ? "none" : string.Join(", ", cutsNow.Select(cutWords));
    surfaceParts.Add("cut by " + cutText + (askIntersect == null ? ""
        : clearCuts ? (cutsBefore.Count > 0 ? " (asked: every cut removed, " + cutsBefore.Count + " were there)"
            : " (asked: none, already so)")
        : " (asked: " + toAdd.Count + " added" + (alreadyCutting.Count > 0 ? ", " + alreadyCutting.Count
            + " already cutting it" : "") + (skipped.Count > 0 ? ", " + skipped.Count + " skipped" : "") + ")"));
    rows.Add("surface: " + string.Join(", ", surfaceParts));

    // ONE PART NOT READING AS WRITTEN FAILS THE CALL - and nothing is kept.
    if (wrong.Count > 0)
        throw new InvalidOperationException("Revit does not hold what was written on " + surfaceWords(surface) + ": "
            + string.Join("; ", wrong) + ". Nothing this call did is kept.");

    surfaceId = surface.UniqueId;
    readBack = string.Join(" | ", rows);
    findings.Add(applied == 0
        ? "Nothing on " + surfaceWords(surface) + " needed changing - every setting asked for was already so ("
            + alreadyAsAsked + ")."
        : "Changed " + surfaceWords(surface) + ": " + applied + " setting(s) changed and read back from Revit"
            + (alreadyAsAsked > 0 ? ", " + alreadyAsAsked + " already so" : "") + ". See readBack.");
    if (skipped.Count > 0)
        findings.Add("Skipped, and the surface is not cut by them: " + string.Join("; ", skipped) + ".");
    if (moved.Count > 0)
        findings.Add("Revit moved what was NOT asked: " + string.Join("; ", moved) + ". It was not written by this "
            + "call - look at the surface before keeping the change.");
    findings.Add("Gridline counts are what Revit made: whether a Number counts the divisions or the lines between "
        + "them is NEEDS-CHECKING BV21, and whether a level's cut is counted among them is JL's.");
}

if (refused.Length > 0) findings.Add(refused);
