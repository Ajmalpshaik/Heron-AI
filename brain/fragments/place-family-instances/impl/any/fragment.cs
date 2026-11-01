// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `level`,
// `structuralType`, `topLevelName` and `topOffset` are in scope, and leaves
// `placed`, `failed`, `readBack`, `tops`, `refused` and `findings` behind.
//
// VERSION 4 (2026-11-01) - PLACE_STRUCTURAL_FAMILY MERGED IN. Every point-
// based placement is this one tool now; what that card had, and its rules
// made safer:
//
//   THE STRUCTURAL TYPE IS AN INPUT, NEVER INFERRED (D-33). `structuralType`
//   "column", "footing" or "non-structural" - left out ("none") is version 3
//   exactly, NonStructural. Anything else is refused, naming what Revit knows.
//   Asked as a column or a footing, the family goes in through Revit's call
//   for a point on a level with that structural type.
//
//   A STRUCTURAL FAMILY GOES IN ONLY AS ITS OWN KIND. A Structural Columns
//   family is placed only as a column, a Structural Foundations one only as a
//   footing - or non-structural when that is SAID. Left out, either is refused
//   and asked which: placing a column non-structurally because nobody said is
//   the defect PLACE_STRUCTURAL_FAMILY was written for.
//
//   A BEAM OR A BRACE IS REFUSED. It is driven by the line between its two
//   ends and a point gives it none: asked as one, or a Structural Framing
//   family asked as anything, is refused before anything is placed, naming
//   PLACE_LINE_BASED_FAMILY (5b-1201).
//
//   A COLUMN'S TOP - `topLevelName` (the whole name, exactly) and `topOffset`
//   (millimetres). Left out ("none"), the top is as Revit placed it, and the
//   answer says so. No such level, two of that name, an offset with no level,
//   a top at or below the base, or a top asked of anything but a column: all
//   refused before anything is placed. Set on the built-in Top Level and Top
//   Offset, never a parameter found by name, and READ BACK after a
//   regenerate, in `tops`, with the top of its geometry as a second reading.
//
//   ONE INSTANCE LANDING WRONG UNDOES THE WHOLE LAYOUT (Article 8) - the
//   safer rule, and this card's own since version 2. PLACE_STRUCTURAL_FAMILY
//   removed a bad one and kept the rest: a column whose family has no
//   settable top, a top that reads back more than 1 mm out or on another
//   level, and - whenever a structural type was asked - a placement Revit
//   leaves with no solid geometry. Each of those now THROWS, naming every one,
//   and nothing is kept.
//
//   A POINT'S Z IS ITS HEIGHT IN THE MODEL for a structural member too - this
//   card's rule. A column or footing asked at its level's own height stands on
//   the level; one asked higher has its Base Offset or Elevation from Level
//   written, and is read back like every other kind. Where Revit's call puts
//   a column's or a footing's height from the point is NEEDS REAL REVIT.
//
// ASSUMES AN OPEN TRANSACTION. Golden Rule 16 - one layout is one undo entry,
// so the TransactionGroup belongs to the operation and not to this fragment.
//
// ACTIVATE THE SYMBOL FIRST. THIS IS THE WHOLE REASON THIS IS A FRAGMENT.
//
// A FamilySymbol that has never been placed in this document is INACTIVE. Ask
// NewFamilyInstance for one anyway and it throws, or on some releases returns
// an instance that is not really there. Either way the caller sees a failure
// with no obvious cause, in code that looks correct, and the fix is one line
// that is only obvious once somebody has lost an afternoon to it.
//
// Activate() must also be followed by Regenerate() before the symbol is used -
// the activation is not visible to the rest of the API until the document has
// regenerated. Doing it once, outside the loop, rather than per point.
//
// POINTS ARE IN INTERNAL FEET. No conversion here (D-20): whoever worked out
// the spacing in millimetres converted once, where the user was.
//
// A POINT'S Z IS ITS HEIGHT IN THE MODEL, like every other point Heron is
// handed - the space Level.ProjectElevation is in. "x, y, 6700" on a level at
// 4000 mm stands 2700 mm above it. REVIT'S OWN CALL FOR A POINT ON A LEVEL
// READS A LEVEL-BASED FAMILY'S Z THE OTHER WAY, as its height above the level:
// measured 2026-10-06 in a scratch project, a fan coil unit asked at 6700 on
// that level came in with Elevation from Level 6700 - at 10700 mm, the level
// counted twice. Version 2 reads every placement back and writes the height
// where Revit put it somewhere else, so every kind ends where it was asked.
//
// HOW A FAMILY IS PUT DOWN DEPENDS ON THE FAMILY, NEVER ON A GUESS (version 2,
// FRAGMENT-ISSUES 5b-334). Measured on six of Revit's own families, 2026-10-06:
//
//   WORK PLANE-BASED - and face-based, which Revit's API names the same way -
//   goes ON THE LEVEL'S OWN PLANE, the way Revit puts one when it is placed by
//   hand in that level's plan: its Host is the level, its Schedule Level the
//   level, and its height is its "Offset from Host". Version 1 sent these
//   through the call for a point on a level as well, which gives them no host
//   and an absolute height: three ceiling diffusers asked at 3000 mm came in at
//   0 mm - on the floor - and the answer said `placed 3`.
//
//   ONE THAT CARRIES NO "Offset from Host" ON A LEVEL'S PLANE CANNOT BE LIFTED
//   OFF IT, AND IS REFUSED ABOVE IT. M_Supply Diffuser - Circular - Round Neck -
//   Ceiling Mounted is one: there it has only Elevation from Level, read-only;
//   moved, it stays. Families like it are made to sit on a ceiling's face. Asked
//   for any height but 0 above the level, the layout is refused and nothing is
//   kept - never placed on the floor and counted.
//
//   LEVEL-BASED, AND EVERY OTHER KIND - the call for a point on a level, as
//   version 1 made, with "Elevation from Level" written wherever Revit has not
//   put it at the height asked - on any level but one at 0 mm, every time.
//
// A PARAMETER ASKED FOR BY ITS ID IS NOT PROOF THE ELEMENT CARRIES IT. On that
// ceiling diffuser get_Parameter(INSTANCE_FREE_HOST_OFFSET_PARAM) answers with
// a parameter that takes a value, says true, and keeps nothing - and the same
// call for FAMILY_LEVEL_PARAM answers "no level" where the element has none
// (FRAGMENT-ISSUES 5b-335). Only the element's own parameter set says what it
// carries, so `carried` looks there.
//
// WHERE A FAMILY STANDS IS WHERE ITS OWN LEVEL STANDS, NOT ITS INSERTION POINT
// (version 3, FRAGMENT-ISSUES 5b-590). Elevation from Level and Offset from
// Host both measure to the family's own Ref. Level, and that is where the
// instance's transform puts the family's origin. The INSERTION POINT -
// LocationPoint - followed the family's "Defines Origin" planes on the family
// measured, and a family may tick that on a plane that is not its level.
// Measured 2026-10-17, Revit 2020, a scratch project:
// TRG_GEN_CutOut_Rectangular_HVAC ticks it on the plane at the BOTTOM of its
// opening, which moves with Object Height. With Elevation from Level 2152 its
// centre stood at 2152 and its insertion point at 2002 (Object Height 250),
// 1827 (600) and 1627 (1000). Version 2 measured the insertion point, so on
// 2026-10-15 a real job asked for 2152 was refused
// "2014.5 mm above" and nothing was kept, with the cut-out where it was wanted.
// Revit's own call reads the height the same way: given no level, it put that
// family's own level at the point's Z, not its insertion point. So the height
// here is the transform's, everywhere; plan is still read off the insertion
// point, as version 2 read it; and where the two heights differ `readBack` says
// so. On a family whose origin is its level - every family version 2 was proven
// on - the two are one point and nothing reads differently.
//
// EVERY ONE IS READ BACK, after the model is rebuilt - where it stands, and the
// level it is on by the rule FILTER_ELEMENTS_BY_CATEGORY finds levels with, so
// the next step that looks for it on that level finds it. One that does not
// read back at its point, on the level asked, THROWS: the layout is rolled back
// whole rather than counted. `readBack` says how each kind ended up, in the
// names Revit's Properties palette shows - a count of what was placed is not a
// check of where it went.
//
// WHAT HAPPENS WHEN REVIT REFUSES ONE POINT, AND WHY THERE IS NO try/catch.
//
// A null return is counted - that costs nothing and loses nothing. A refusal
// that THROWS is deliberately left to propagate, and this is a real decision
// rather than an omission:
//
//   Golden Rule 16 and Phase 1's own definition of done say a failed operation
//   LEAVES THE MODEL UNTOUCHED. The operation above owns the TransactionGroup,
//   so an exception here rolls the whole layout back and the user gets a clean
//   model and a reason. Swallowing it here would quietly convert that into a
//   partial layout - 37 sprinklers placed, 3 missing, nothing rolled back and
//   no transaction aware anything went wrong.
//
//   WHETHER A LAYOUT SHOULD BE PARTIAL OR ALL-OR-NOTHING IS THE OWNER'S CALL,
//   not this fragment's. A modeller may well prefer 37 and a list of 3. That is
//   a question to ask, not to answer here by choosing which exceptions to hide.
//
// It also keeps this file to UNQUALIFIED type names, as every other fragment
// here does. Catching Revit's own exception type needs its full namespace, and
// that namespace outside revit/ trips the adapter boundary check - which fired
// on the first draft of this fragment, and then fired again on the COMMENT that
// explained why, exactly as it once did on the checker itself.

var placed = new List<ElementId>();
var failed = 0;
var readBack = "";
// VERSION 4, from PLACE_STRUCTURAL_FAMILY.
var tops = new List<string>();
string refused = null;
var findings = new List<string>();

// ---- VERSION 4: WHAT IS ASKED, JUDGED BEFORE ANYTHING IS PLACED.
Func<string, bool> notAsked = text =>
{
    var word = (text ?? "").Trim().ToLowerInvariant();
    return word.Length == 0 || word == "none";
};

var askedKind = notAsked(structuralType) ? "" : structuralType.Trim().ToLowerInvariant();
var wanted = StructuralType.NonStructural;
var understood = true;
if (askedKind == "") wanted = StructuralType.NonStructural;
else if (askedKind == "column") wanted = StructuralType.Column;
else if (askedKind == "footing") wanted = StructuralType.Footing;
else if (askedKind == "beam") wanted = StructuralType.Beam;
else if (askedKind == "brace") wanted = StructuralType.Brace;
else if (askedKind == "nonstructural" || askedKind == "non-structural") wanted = StructuralType.NonStructural;
else understood = false;
var structural = wanted == StructuralType.Column || wanted == StructuralType.Footing;

var isColumn = false;
var isFraming = false;
var isFoundation = false;
if (symbol != null && symbol.Category != null)
{
    var category = symbol.Category.Id;
    isColumn = category.Equals(new ElementId(BuiltInCategory.OST_StructuralColumns));
    isFraming = category.Equals(new ElementId(BuiltInCategory.OST_StructuralFraming));
    isFoundation = category.Equals(new ElementId(BuiltInCategory.OST_StructuralFoundation));
}
var familyWords = symbol == null ? "" : string.Format("'{0}: {1}'", symbol.FamilyName, symbol.Name);
var lineTool = "Ask PLACE_LINE_BASED_FAMILY instead, with `curves` as start;end pairs in millimetres "
    + "and a pipe between lines - \"0,0,0; 5000,0,0 | 5000,0,0; 5000,3000,4000\". Nothing was placed";

// THE TOP ASKED FOR - columns only.
var topName = notAsked(topLevelName) ? "" : topLevelName.Trim();
var offsetText = notAsked(topOffset) ? "" : topOffset.Trim();
Level topLevel = null;
var offsetFeet = 0.0;

if (symbol == null)
    refused = "No family type was given. Nothing was placed";
else if (!understood)
    refused = string.Format("'{0}' is not a structural type Revit knows. Say column, footing or "
        + "non-structural - or leave it out for a non-structural placement - and a beam or a brace goes "
        + "along a line, which is PLACE_LINE_BASED_FAMILY. Nothing was placed", structuralType);
else if (isFraming)
    refused = string.Format("{0} is a Structural Framing family, which is placed along a line between its "
        + "two ends, and a point gives it no line - Revit would place it with no length and then delete its "
        + "geometry. {1}", familyWords, lineTool);
else if ((wanted == StructuralType.Beam || wanted == StructuralType.Brace) && !isColumn && !isFoundation)
    refused = string.Format("A {0} is placed along a line between its two ends, and a point gives it no "
        + "line - Revit would place it with no length and then delete its geometry. {1}", askedKind, lineTool);
else if ((isColumn || isFoundation) && askedKind == "")
    refused = string.Format("{0} is a {1} family. Say whether it goes in as {2} or non-structural - which "
        + "one changes what every structural schedule and analytical model counts, and it is not guessed. "
        + "Nothing was placed", familyWords, symbol.Category.Name, isColumn ? "a column" : "a footing");
else if ((isColumn || isFoundation) && wanted != StructuralType.NonStructural
         && !((isColumn && wanted == StructuralType.Column) || (isFoundation && wanted == StructuralType.Footing)))
    refused = string.Format("{0} is a {1} family and was asked to go in as '{2}', which Revit would accept "
        + "and place anyway. Ask for it as {3}. Nothing was placed", familyWords, symbol.Category.Name,
        structuralType, isColumn ? "column" : "footing");
else if (topName.Length == 0 && offsetText.Length > 0)
    refused = "A top offset of " + offsetText + " mm was given with no top level, and which level it is "
        + "measured from is not guessed. Nothing was placed";
else if (topName.Length > 0)
{
    double offsetMm = 0;
    if (offsetText.Length > 0
        && !double.TryParse(offsetText, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out offsetMm))
        refused = "'" + offsetText + "' is not a top offset. Give it in millimetres - 300 or -150 - or leave "
            + "it out for none. Nothing was placed";
    else if (wanted != StructuralType.Column)
        refused = "A top level is set only on columns, and this was asked as '"
            + (askedKind == "" ? "non-structural" : structuralType) + "'. Nothing was placed";
    else
    {
        offsetFeet = offsetMm / 304.8;
        var matches = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
            .Where(candidate => candidate != null && string.Equals(candidate.Name, topName, StringComparison.Ordinal))
            .ToList();
        if (matches.Count == 0)
            refused = "No level is named '" + topName + "' exactly, so the top is not guessed. Nothing was placed";
        else if (matches.Count > 1)
            refused = matches.Count + " levels are named '" + topName + "', so which one is the top is not "
                + "guessed. Nothing was placed";
        else if (matches[0].Id == level.Id || matches[0].ProjectElevation <= level.ProjectElevation)
            refused = string.Format("The top level '{0}' at {1:0} mm is not above the base level '{2}' at "
                + "{3:0} mm. Nothing was placed", matches[0].Name, matches[0].ProjectElevation * 304.8,
                level.Name, level.ProjectElevation * 304.8);
        else if (matches[0].ProjectElevation + offsetFeet <= level.ProjectElevation)
            refused = string.Format("'{0}' with a {1:0} mm offset puts the top at or below the base level "
                + "'{2}'. Nothing was placed", matches[0].Name, offsetMm, level.Name);
        else topLevel = matches[0];
    }
}
if (refused != null) findings.Add(refused);

if (refused == null)
{

const double MillimetresPerFoot = 304.8;

// How far a placement may stand from its point and still be at it: half a
// millimetre - nothing anybody draws, and far above Revit's rounding.
const double CloseEnough = 0.5 / MillimetresPerFoot;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => (feet * MillimetresPerFoot).ToString("0.#", invariant) + " mm";

if (!symbol.IsActive)
{
    symbol.Activate();
    doc.Regenerate();
}

var named = symbol.Family == null ? symbol.Name : symbol.Family.Name + ": " + symbol.Name;
var placement = symbol.Family == null ? FamilyPlacementType.Invalid : symbol.Family.FamilyPlacementType;
var onLevelPlane = placement == FamilyPlacementType.WorkPlaneBased;

// The level's height in the model - ProjectElevation, not Elevation, which
// follows the level type's Elevation Base (CREATE_ROOF carries what the other
// one cost).
var levelZ = level.ProjectElevation;
var levelPlane = onLevelPlane ? level.GetPlaneReference() : null;

// What the element really carries, read off its own parameter set.
Func<Element, BuiltInParameter, Parameter> carried = (element, id) =>
{
    foreach (Parameter parameter in element.Parameters)
    {
        var definition = parameter.Definition as InternalDefinition;
        if (definition != null && definition.BuiltInParameter == id) return parameter;
    }
    return null;
};

// Where each kind keeps its height above the level.
var heightId = onLevelPlane && !structural
    ? BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM
    : wanted == StructuralType.Column
        ? BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM
        : BuiltInParameter.INSTANCE_ELEVATION_PARAM;
var heightWord = wanted == StructuralType.Column ? "Base Offset" : "Elevation from Level";
// A structural member goes through Revit's call for a point on a level with
// its structural type (version 4), never onto the level's plane.
if (structural) onLevelPlane = false;

// The height its own level stands at - see "WHERE A FAMILY STANDS" above.
Func<FamilyInstance, double> standsAt = instance => instance.GetTransform().Origin.Z;

var made = new List<FamilyInstance>();
var askedAt = new List<XYZ>();

foreach (var point in points)
{
    if (point == null) { failed++; continue; }

    // On the level's plane at the point's place on plan, the height to follow
    // as the offset; or Revit's call for a point on a level, which reads the
    // point's Z as the height above it - put right below.
    var instance = onLevelPlane
        ? doc.Create.NewFamilyInstance(levelPlane, new XYZ(point.X, point.Y, levelZ), XYZ.BasisX, symbol)
        : doc.Create.NewFamilyInstance(point, symbol, level, wanted);

    // A null return is not an exception and is not a placement. Counted as a
    // failure rather than added as an id nothing can resolve.
    if (instance == null) { failed++; continue; }

    placed.Add(instance.Id);
    made.Add(instance);
    askedAt.Add(point);
}

if (made.Count > 0) doc.Regenerate();

// THE HEIGHT, where Revit did not put it already - written to the parameter
// the element carries, or the layout refused when it carries none.
for (var i = 0; i < made.Count; i++)
{
    var stands = made[i].Location as LocationPoint;
    if (stands != null && Math.Abs(standsAt(made[i]) - askedAt[i].Z) <= CloseEnough) continue;

    var height = carried(made[i], heightId);
    if (height == null || height.IsReadOnly || height.StorageType != StorageType.Double)
        throw new InvalidOperationException(onLevelPlane
            ? string.Format(
                "'{0}' is work plane-based and carries no Offset from Host on a level's plane, so it "
                + "cannot stand {1} above '{2}' - it sits on the plane it is placed on. Place it on the "
                + "ceiling's face in Revit (Place on Face), use a hosted version of the family, which "
                + "carries the offset, or ask for it at the level's own height to put it on '{2}'. "
                + "Nothing this layout did is kept", named, mm(askedAt[i].Z - levelZ), level.Name)
            : string.Format(
                "Revit put '{0}' {1} above '{2}' where {3} above it was asked, and it carries no "
                + "{4} to correct that with. Nothing this layout did is kept", named,
                stands == null ? "somewhere it gives back no point" : mm(standsAt(made[i]) - levelZ),
                level.Name, mm(askedAt[i].Z - levelZ), heightWord));

    // The height above the level, which is what both of these parameters hold.
    height.Set(askedAt[i].Z - levelZ);
}

// VERSION 4: A COLUMN'S TOP - the built-ins only, never a parameter found by
// name. A family with no settable top, or a top Revit will not take, undoes
// the whole layout (Article 8): PLACE_STRUCTURAL_FAMILY removed that one and
// kept the rest.
if (topLevel != null)
{
    var noTop = new List<string>();
    for (var i = 0; i < made.Count; i++)
    {
        var topParameter = made[i].get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
        var offsetParameter = made[i].get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
        if (topParameter == null || topParameter.IsReadOnly || topParameter.StorageType != StorageType.ElementId
            || offsetParameter == null || offsetParameter.IsReadOnly || offsetParameter.StorageType != StorageType.Double)
        {
            noTop.Add(string.Format("point {0} - its family has no settable Top Level and Top Offset", i + 1));
            continue;
        }
        if (!topParameter.Set(topLevel.Id) || !offsetParameter.Set(offsetFeet))
            noTop.Add(string.Format("point {0} - Revit returned false for the top level or the top offset", i + 1));
    }
    if (noTop.Count > 0)
        throw new InvalidOperationException(string.Format("A top was asked for '{0}' + {1:0} mm and {2} "
            + "column(s) could not take it: {3}. Nothing this layout did is kept", topLevel.Name,
            offsetFeet * 304.8, noTop.Count, string.Join("; ", noTop)));
}

if (made.Count > 0) doc.Regenerate();

// THE LEVEL, FOUND THE WAY FILTER_ELEMENTS_BY_CATEGORY FINDS IT - LevelId,
// then the first of these that names a level.
var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    // Version 4: a column's base level.
    BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
    BuiltInParameter.LEVEL_PARAM,
    BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
    BuiltInParameter.RBS_START_LEVEL_PARAM,
};

Func<Element, ElementId> levelOf = e =>
{
    if (e.LevelId != ElementId.InvalidElementId) return e.LevelId;
    foreach (var candidate in levelParameterOrder)
    {
        var p = e.get_Parameter(candidate);
        if (p == null || !p.HasValue || p.StorageType != StorageType.ElementId) continue;
        var held = p.AsElementId();
        if (held == null || held == ElementId.InvalidElementId) continue;
        if (e.Document.GetElement(held) is Level) return held;
    }
    return ElementId.InvalidElementId;
};

// HOW IT ENDED UP, in the names the Properties palette shows.
var kindWords = onLevelPlane
    ? string.Format("work plane-based, on '{0}''s own plane", level.Name)
    : placement == FamilyPlacementType.OneLevelBased
        ? string.Format("level-based, on '{0}'", level.Name)
        : string.Format("{0}, on '{1}'", placement, level.Name);

Func<FamilyInstance, string> described = instance =>
{
    var parts = new List<string>();
    if (onLevelPlane)
        parts.Add(instance.Host == null ? "Host none" : string.Format("Host '{0}'", instance.Host.Name));
    foreach (var id in new[] { BuiltInParameter.FAMILY_LEVEL_PARAM, BuiltInParameter.SCHEDULE_LEVEL_PARAM,
                               BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM })
    {
        var parameter = carried(instance, id);
        if (parameter == null || parameter.StorageType != StorageType.ElementId) continue;
        var held = doc.GetElement(parameter.AsElementId());
        var line = string.Format("{0} '{1}'", parameter.Definition.Name, held == null ? "none" : held.Name);
        if (!parts.Contains(line)) parts.Add(line);
    }
    var height = carried(instance, heightId);
    if (height != null && height.StorageType == StorageType.Double)
        parts.Add(string.Format("{0} {1}", height.Definition.Name, mm(height.AsDouble())));
    return string.Join(", ", parts);
};

var tally = new SortedDictionary<string, int>(StringComparer.Ordinal);
for (var i = 0; i < made.Count; i++)
{
    var instance = made[i];
    var point = askedAt[i];
    var stands = instance.Location as LocationPoint;
    var foundOn = levelOf(instance);

    var off = stands == null ? double.NaN
        : Math.Sqrt(Math.Pow(stands.Point.X - point.X, 2) + Math.Pow(stands.Point.Y - point.Y, 2));
    var rise = stands == null ? double.NaN : standsAt(instance) - levelZ;

    if (stands == null || off > CloseEnough || Math.Abs(standsAt(instance) - point.Z) > CloseEnough
        || foundOn != level.Id)
        throw new InvalidOperationException(string.Format(
            "Revit put '{0}' {1}{2}, where it was asked {3} above '{4}', on it. Nothing this layout "
            + "did is kept", named,
            stands == null ? "where it gives back no point"
                : string.Format("{0} above '{1}' and {2} off on plan", mm(rise), level.Name, mm(off)),
            foundOn == level.Id ? ""
                : foundOn == ElementId.InvalidElementId ? ", on NO level"
                : ", on '" + doc.GetElement(foundOn).Name + "'",
            mm(point.Z - levelZ), level.Name));

    var said = string.Format("{0}, standing {1} above it - {2}", kindWords, mm(rise), described(instance));

    // Its insertion point, said only where it is not where the family stands.
    var insertionGap = stands.Point.Z - standsAt(instance);
    if (Math.Abs(insertionGap) > CloseEnough)
        said += string.Format(", its insertion point {0} {1} that", mm(Math.Abs(insertionGap)),
            insertionGap < 0 ? "below" : "above");
    int had;
    tally[said] = tally.TryGetValue(said, out had) ? had + 1 : 1;
}

// VERSION 4: WHAT REVIT LEFT WITH NO SOLID GEOMETRY UNDOES THE LAYOUT, when a
// structural type was asked (5b-1201). After "Line is too short" or "Can't
// create sweep" Revit leaves an instance with no solid; PLACE_STRUCTURAL_FAMILY
// removed it and counted it failed. Now every one is named and nothing kept.
if (askedKind != "" && made.Count > 0)
{
    var options = new Options();
    options.DetailLevel = ViewDetailLevel.Fine;
    var hollow = new List<string>();
    for (var i = 0; i < made.Count; i++)
    {
        var instance = made[i];
        var hasSolid = false;
        if (instance != null && instance.IsValidObject)
        {
            try
            {
                var geometry = instance.get_Geometry(options);
                if (geometry != null)
                {
                    foreach (var item in geometry)
                    {
                        var solid = item as Solid;
                        if (solid != null && solid.Volume > 0) { hasSolid = true; break; }
                        var nested = item as GeometryInstance;
                        if (nested == null) continue;
                        foreach (var inner in nested.GetInstanceGeometry())
                        {
                            var innerSolid = inner as Solid;
                            if (innerSolid != null && innerSolid.Volume > 0) { hasSolid = true; break; }
                        }
                        if (hasSolid) break;
                    }
                }
            }
            catch { hasSolid = false; }
        }
        if (!hasSolid)
            hollow.Add(string.Format("point {0} ({1:0}, {2:0})", i + 1, askedAt[i].X * 304.8, askedAt[i].Y * 304.8));
    }
    if (hollow.Count > 0)
        throw new InvalidOperationException(string.Format("Revit left {0} placement(s) with no solid geometry "
            + "- {1}. A structural member with no solid is not one, so nothing this layout did is kept",
            hollow.Count, string.Join("; ", hollow)));
}

// VERSION 4: THE TOP, READ BACK - the level and offset each column now
// carries, as an elevation, beside what was asked, and its geometry's top.
// One more than 1 mm out, or on another level, undoes the layout.
if (topLevel != null)
{
    var askedMm = (topLevel.ProjectElevation + offsetFeet) * 304.8;
    var wrong = new List<string>();
    for (var i = 0; i < made.Count; i++)
    {
        var label = string.Format("point {0} ({1:0}, {2:0})", i + 1, askedAt[i].X * 304.8, askedAt[i].Y * 304.8);
        Level readLevel = null;
        var readMm = double.NaN;
        var readTop = made[i].get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
        var readOffset = made[i].get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
        if (readTop != null) readLevel = doc.GetElement(readTop.AsElementId()) as Level;
        if (readLevel != null && readOffset != null)
            readMm = (readLevel.ProjectElevation + readOffset.AsDouble()) * 304.8;
        var box = made[i].get_BoundingBox(null);
        var geometryTop = box == null ? "" : string.Format(", its geometry reaches {0:0} mm", box.Max.Z * 304.8);
        var reads = double.IsNaN(readMm) ? "nothing" : string.Format("'{0}' + offset = {1:0} mm", readLevel.Name, readMm);
        tops.Add(string.Format("{0} - top asked '{1}' + {2:0} mm = {3:0} mm, reads {4}{5}",
            label, topLevel.Name, offsetFeet * 304.8, askedMm, reads, geometryTop));
        if (readLevel == null || readLevel.Id != topLevel.Id || double.IsNaN(readMm) || Math.Abs(readMm - askedMm) > 1.0)
            wrong.Add(string.Format("{0} reads {1}", label, reads));
    }
    if (wrong.Count > 0)
        throw new InvalidOperationException(string.Format("A top was asked at {0:0} mm on '{1}' and {2} "
            + "column(s) read back otherwise: {3}. Nothing this layout did is kept", askedMm, topLevel.Name,
            wrong.Count, string.Join("; ", wrong)));
}

findings.Add(string.Format("{0} placed{1} on '{2}', {3} point(s) could not take one", placed.Count,
    askedKind == "" ? "" : " as " + wanted, level.Name, failed));
if (wanted == StructuralType.Column)
    findings.Add(topLevel != null
        ? string.Format("Top asked for: '{0}' + {1:0} mm. Read back as asked on all {2}", topLevel.Name,
            offsetFeet * 304.8, made.Count)
        : "No top level was asked for, so every top was left as Revit placed it");

readBack = made.Count == 0
    ? "Nothing was placed, so nothing was read back"
    : string.Join(" | ", tally.Select(p => p.Value + " x " + p.Key));
}
