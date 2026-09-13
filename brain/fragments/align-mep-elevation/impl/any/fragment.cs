// NOT STANDALONE. Assumes `doc`, `elements`, `edge` and `targetZ` are in scope;
// leaves `aligned`, `noSize` and `notCurveBased` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
//
// THIS HEADER SAID "Heights are internal FEET" UNTIL 2026-09-13, and the code
// matched it: `targetZ` went straight into Revit's coordinates unconverted. A
// modeller asking for 2700 got 2700 FEET - 823 metres - which is the 914-metre
// wall of D-71's opening paragraph wearing a different fragment's name. The
// fragment was PROVEN when this was found, on a proof that never measured the
// resulting height.
//
// WHY THIS IS NOT "MOVE THEM ALL TO ONE Z".
//
// An MEP run's position IS its centreline. Set every centreline to the same
// height and a 600 mm duct and a 100 mm pipe end up with tops 250 mm apart and
// bottoms 250 mm apart - which is the opposite of what "level with each other"
// means to whoever asked. The coordination question is almost always about the
// SOFFIT, because that is what the ceiling has to clear.
//
// So each run's own half-height is read and subtracted.
//
// THE HALF-HEIGHT LIVES IN A DIFFERENT PARAMETER PER KIND, and getting it wrong
// makes the alignment QUIETLY wrong rather than failing:
//
//   rectangular duct   RBS_CURVE_HEIGHT_PARAM
//   round duct         RBS_CURVE_DIAMETER_PARAM
//   pipe               RBS_PIPE_DIAMETER_PARAM
//   cable tray         RBS_CABLETRAY_HEIGHT_PARAM
//
// Anything carrying none of them is treated as having no height - its
// centreline IS its top and bottom - and is REPORTED SEPARATELY rather than
// silently assumed flat. A wrong assumption has to be visible; that list is how.
//
// INSULATION IS NOT INCLUDED. These give the bare service size. The caller adds
// clearance if the job needs it - see the fragment's purpose for why that is
// not read here.

Func<Element, double> halfHeightOf = e =>
{
    var order = new[]
    {
        BuiltInParameter.RBS_CURVE_HEIGHT_PARAM,       // rectangular duct, tray
        BuiltInParameter.RBS_CURVE_DIAMETER_PARAM,     // round duct
        BuiltInParameter.RBS_PIPE_DIAMETER_PARAM,      // pipe
        BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM,   // cable tray
    };

    foreach (var which in order)
    {
        var parameter = e.get_Parameter(which);
        if (parameter == null || !parameter.HasValue) continue;
        if (parameter.StorageType != StorageType.Double) continue;
        var size = parameter.AsDouble();
        if (size > 0) return size / 2.0;
    }
    return -1.0;      // negative means "no size found", not "zero height"
};

var wanted = (edge ?? "").Trim().ToLowerInvariant();

var aligned = 0;
var noSize = new List<ElementId>();
var notCurveBased = new List<ElementId>();

const double MillimetresPerFoot = 304.8;
targetZ = targetZ / MillimetresPerFoot;

foreach (var element in elements)
{
    var line = element.Location as LocationCurve;
    if (line == null || line.Curve == null)
    {
        notCurveBased.Add(element.Id);
        continue;
    }

    var half = halfHeightOf(element);
    if (half < 0)
    {
        // Reported rather than treated as flat. A run whose size could not be
        // read would align its centreline while every other run aligned its
        // soffit, and the result looks almost right - which is worse than
        // looking wrong.
        noSize.Add(element.Id);
        continue;
    }

    // The centreline height this run needs so that the requested edge lands on
    // targetZ.
    var wantCentre = targetZ;
    if (wanted == "top") wantCentre = targetZ - half;
    else if (wanted == "bottom") wantCentre = targetZ + half;

    // A sloped run has no single height. The midpoint is used, which levels the
    // run's MIDDLE onto the target and keeps its slope - the honest reading of
    // "line them up" for a run that was never horizontal.
    var at = line.Curve.Evaluate(0.5, true);
    var shift = wantCentre - at.Z;

    ElementTransformUtils.MoveElement(doc, element.Id, new XYZ(0, 0, shift));
    aligned++;
}
