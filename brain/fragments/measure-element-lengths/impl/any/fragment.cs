// NOT STANDALONE. Assumes `elements` is in scope; leaves `lengthsMm`,
// `totalMm` and `noLength` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// FEET TO MILLIMETRES IS ARITHMETIC, NOT UnitUtils (D-20). Revit's internal
// length unit is decimal feet on every release from 2020 to 2027, and one foot
// is 304.8 mm exactly, by definition of the international inch. `UnitUtils` was
// rewritten in 2021 - DisplayUnitType became ForgeTypeId - so calling it here
// would put a version break into a fragment that needs none, in exchange for
// arithmetic that cannot drift.
//
// TWO SOURCES, AND THE GEOMETRY IS ASKED FIRST. The LocationCurve is the shape
// actually in the model. CURVE_ELEM_LENGTH is a parameter reporting it, and
// this repository's standing lesson is that Revit's own reported values
// describe intent and are not always the physical fact. Where an element has a
// curve, that curve is the answer; the parameter is the fallback for elements
// that carry a length without a location curve.
//
// NO LENGTH IS NOT ZERO LENGTH. A fitting, a piece of equipment, an air
// terminal - none of them has a length, and scoring them 0 mm would leave a
// total that looks complete. They are named in `noLength` instead, so the
// answer can say how many were set aside and why.

var lengthsMm = new Dictionary<ElementId, double>();
var noLength = new List<ElementId>();
var totalMm = 0.0;

const double MillimetresPerFoot = 304.8;

foreach (var element in elements)
{
    if (element == null) continue;

    double feet = 0.0;
    var found = false;

    var placement = element.Location as LocationCurve;
    if (placement != null && placement.Curve != null)
    {
        feet = placement.Curve.Length;
        found = true;
    }
    else
    {
        var declared = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        if (declared != null && declared.HasValue && declared.StorageType == StorageType.Double)
        {
            feet = declared.AsDouble();
            found = true;
        }
    }

    // A curve of no length is a degenerate element, not a measurement. Treated
    // as "no length" rather than added as 0.0, so it shows up as something to
    // look at rather than disappearing into a total.
    if (!found || feet <= 0.0)
    {
        noLength.Add(element.Id);
        continue;
    }

    var millimetres = feet * MillimetresPerFoot;

    // A duplicate id in the input would otherwise be counted twice into the
    // total while the dictionary kept one entry - the two outputs would then
    // disagree, and the total is the one nobody re-checks.
    if (lengthsMm.ContainsKey(element.Id)) continue;

    lengthsMm[element.Id] = millimetres;
    totalMm += millimetres;
}
