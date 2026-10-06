// NOT STANDALONE. Assumes `doc`, `symbol`, `points` and `level` are in scope,
// and leaves `placed`, `failed` and `readBack` behind.
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
var heightId = onLevelPlane
    ? BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM
    : BuiltInParameter.INSTANCE_ELEVATION_PARAM;

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
        : doc.Create.NewFamilyInstance(point, symbol, level, StructuralType.NonStructural);

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
    if (stands != null && Math.Abs(stands.Point.Z - askedAt[i].Z) <= CloseEnough) continue;

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
                + "Elevation from Level to correct that with. Nothing this layout did is kept", named,
                stands == null ? "somewhere it gives back no point" : mm(stands.Point.Z - levelZ),
                level.Name, mm(askedAt[i].Z - levelZ)));

    // The height above the level, which is what both of these parameters hold.
    height.Set(askedAt[i].Z - levelZ);
}

if (made.Count > 0) doc.Regenerate();

// THE LEVEL, FOUND THE WAY FILTER_ELEMENTS_BY_CATEGORY FINDS IT - LevelId,
// then the first of these that names a level.
var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
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
    var rise = stands == null ? double.NaN : stands.Point.Z - levelZ;

    if (stands == null || off > CloseEnough || Math.Abs(stands.Point.Z - point.Z) > CloseEnough
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
    int had;
    tally[said] = tally.TryGetValue(said, out had) ? had + 1 : 1;
}

readBack = made.Count == 0
    ? "Nothing was placed, so nothing was read back"
    : string.Join(" | ", tally.Select(p => p.Value + " x " + p.Key));
