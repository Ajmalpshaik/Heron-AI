// NOT STANDALONE. Assumes `elements` and `measure` are in scope, and leaves
// `quantities`, `lengths`, `areas`, `areaUnknown` and `lengthUnknown` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO NUMBERS, IN REVIT'S INTERNAL UNITS.
//
//   CURVE_ELEM_LENGTH       feet. What Revit schedules, and what a quantity
//                           surveyor is billed against.
//   RBS_CURVE_SURFACE_AREA  square feet. Revit's OWN perimeter x length,
//                           already computed and sitting on the element. For
//                           ductwork that is the sheet-metal area of the
//                           straight runs.
//
// Handed on in feet and square feet, not millimetres - D-20 keeps the
// conversion at the edge where the user is, and doing it here would make this
// the second place that knows about units.
//
// LENGTH COMES FROM THE PARAMETER, NOT THE LOCATION CURVE.
//
// They are not reliably the same number - a duct's curve is its centreline.
// Measuring the curve gives a defensible figure that disagrees with the model's
// own schedule, and that is worse than a plainly wrong one: both sides look
// right and nobody can say which to bill.
//
// A MISSING AREA IS NOT ZERO.
//
// Cable tray and conduit carry no surface area parameter. Zero would read as
// "no metal in it" - a plausible number and a false one. They are named in
// `areaUnknown` instead, so a total built on them declares what it is missing
// rather than quietly absorbing it.

// WHICH MEASURE IS A REQUEST INPUT, NOT A DEFAULT.
//
// "How much ductwork is there" means metres to one person and square metres to
// another. Choosing one silently answers a question nobody asked. `measure`
// says which, and the chosen one is copied into `quantities` so SUM_BY_GROUP
// can total it WITHOUT KNOWING WHAT IT IS - which is what lets one totalling
// serve length, area, insulation, tray weight and concrete rather than being
// rewritten per material. Both are still returned separately.
//
// An unrecognised `measure` falls to length rather than throwing, because
// length is the only one every linear element has - but it is recognised
// generously rather than matched exactly, so "area", "areas" and "surface
// area" all mean the same thing.
bool wantArea = (measure ?? "").IndexOf("area", StringComparison.OrdinalIgnoreCase) >= 0;

var quantities = new Dictionary<ElementId, double>();
var lengths = new Dictionary<ElementId, double>();
var areas = new Dictionary<ElementId, double>();
var areaUnknown = new List<ElementId>();
var lengthUnknown = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    Parameter lengthParameter = null;
    try { lengthParameter = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH); } catch { }

    if (lengthParameter != null && lengthParameter.HasValue)
    {
        lengths[element.Id] = lengthParameter.AsDouble();
        if (!wantArea) quantities[element.Id] = lengthParameter.AsDouble();
    }
    else
        lengthUnknown.Add(element.Id);

    Parameter areaParameter = null;
    try { areaParameter = element.get_Parameter(BuiltInParameter.RBS_CURVE_SURFACE_AREA); } catch { }

    if (areaParameter != null && areaParameter.HasValue)
    {
        areas[element.Id] = areaParameter.AsDouble();
        if (wantArea) quantities[element.Id] = areaParameter.AsDouble();
    }
    else
        areaUnknown.Add(element.Id);
}
