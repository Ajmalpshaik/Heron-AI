// NOT STANDALONE. Assumes `elements` and `maximumVelocity` are in scope; leaves
// `velocity`, `revitSays`, `overLimit`, `disagrees` and `noFlow` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// COMPUTED FROM FLOW AND AREA, AND REVIT'S OWN VALUE READ ALONGSIDE IT.
//
// This repository's standing rule is that Revit's reported values describe
// intent. `RBS_VELOCITY` is derived from the size and the flow the system last
// calculated, and a duct resized afterwards keeps reporting the old figure -
// which looks entirely correct on a schedule. Computing it from the geometry
// and the flow gives a second number from the same model, and a disagreement
// is the finding.
//
// UNITS ARE ARITHMETIC (D-20), NOT UnitUtils, which was rewritten in 2021:
//
//   flow      internal is CUBIC FEET PER SECOND
//   size      internal is DECIMAL FEET
//   so        flow / area is FEET PER SECOND
//   and       1 ft = 0.3048 m exactly, by definition of the international inch
//
// ROUND AND RECTANGULAR NEED DIFFERENT ARITHMETIC and a duct is one or the
// other. Diameter is tried first because a round duct carries no width or
// height; anything with neither is named rather than assigned an area.
//
// NO FLOW IS NOT ZERO VELOCITY. A duct not yet in a calculated system has no
// flow, and dividing gives 0.0 - which reads as "well within limits" on the
// one duct nobody has connected yet.

var velocity = new Dictionary<ElementId, double>();
var revitSays = new Dictionary<ElementId, double>();
var overLimit = new List<ElementId>();
var disagrees = new List<ElementId>();
var noFlow = new List<ElementId>();

const double MetresPerFoot = 0.3048;

// Two velocities more than a tenth of a metre per second apart are reporting
// different states of the model, not rounding.
const double DisagreementMs = 0.1;

foreach (var element in elements)
{
    if (element == null) continue;
    if (velocity.ContainsKey(element.Id) || noFlow.Contains(element.Id)) continue;

    var flowParameter = element.get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM);
    if (flowParameter == null || !flowParameter.HasValue
        || flowParameter.StorageType != StorageType.Double)
    {
        noFlow.Add(element.Id);
        continue;
    }

    var flow = flowParameter.AsDouble();          // cubic feet per second
    if (flow <= 0.0)
    {
        noFlow.Add(element.Id);
        continue;
    }

    // Area, in square feet. Round first - a round duct has no width or height.
    var area = 0.0;

    var diameter = element.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
    if (diameter != null && diameter.HasValue
        && diameter.StorageType == StorageType.Double && diameter.AsDouble() > 0.0)
    {
        var radius = diameter.AsDouble() / 2.0;
        area = Math.PI * radius * radius;
    }
    else
    {
        var width = element.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
        var height = element.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
        if (width != null && width.HasValue && height != null && height.HasValue
            && width.StorageType == StorageType.Double
            && height.StorageType == StorageType.Double)
        {
            area = width.AsDouble() * height.AsDouble();
        }
    }

    if (area <= 0.0)
    {
        // Neither round nor rectangular, or a size not filled in. Named rather
        // than given an invented area, which would produce a confident number.
        noFlow.Add(element.Id);
        continue;
    }

    var computed = (flow / area) * MetresPerFoot;
    velocity[element.Id] = computed;

    if (computed > maximumVelocity) overLimit.Add(element.Id);

    // Revit's own, read for comparison only. Its absence is not a fault - it
    // is simply nothing to compare against.
    var reported = element.get_Parameter(BuiltInParameter.RBS_VELOCITY);
    if (reported == null || !reported.HasValue
        || reported.StorageType != StorageType.Double)
    {
        continue;
    }

    var theirs = reported.AsDouble() * MetresPerFoot;
    revitSays[element.Id] = theirs;

    if (Math.Abs(theirs - computed) > DisagreementMs) disagrees.Add(element.Id);
}
