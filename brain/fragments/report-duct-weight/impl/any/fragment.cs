// NOT STANDALONE. Assumes `doc`, `elements`, `sizeBandsMm`, `thicknessMm` and
// `densityKgPerM3` are in scope, and leaves `sheetAreas`, `weights`,
// `bandUsed`, `shapeInferred`, `unsized`, `outsideBands`, `areaDifference` and
// `areaChecked` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE ORDER OF THE SHAPE TEST IS THE WHOLE TRICK.
//
// An OVAL duct carries a width, a height AND a diameter. Testing "does it have
// a diameter" therefore calls every oval round, and a round perimeter on an
// oval duct is wrong by a wide margin - which lands in the weight at full
// strength, because the kilos are proportional to the area. So the duct TYPE
// is asked first: it states Round, Rectangular or Oval outright. The parameter
// pattern is only the fallback for a duct with no readable type, and every one
// that needed it is named, because that is where this goes wrong.
//
// AN OVAL PERIMETER HAS NO CLOSED FORM.
//
// The series below is the standard approximation and is good to a fraction of
// a percent. pi times the mean of the two sides - the obvious thing to write -
// is several percent out on a flat oval, and a flat oval is exactly the shape
// somebody chose because the ceiling void was tight.
//
// MILLIMETRES ONLY WHERE THE BANDS ARE.
//
// The gauge table arrives in millimetres, because that is how a specification
// writes it, so the governing size is converted to millimetres to be matched
// against it. Everything else stays in Revit's feet, and the areas handed on
// are square feet - D-20 keeps the user-facing conversion at the edge. The
// weight is kilograms, which Revit has no internal unit for at all.

const double FeetToMm = 304.8;
const double SquareFeetToSquareMetres = 0.09290304;

var sheetAreas = new Dictionary<ElementId, double>();
var weights = new Dictionary<ElementId, double>();
var bandUsed = new Dictionary<ElementId, string>();
var shapeInferred = new List<ElementId>();
var unsized = new List<ElementId>();
var outsideBands = new List<ElementId>();
double areaDifference = 0;
int areaChecked = 0;

// A table whose two halves are different lengths is a caller mistake that
// would otherwise show up as ducts silently falling outside every band. Only
// the pairs that exist are used, and the rest of the run still reports.
int bandCount = Math.Min(
    sizeBandsMm == null ? 0 : sizeBandsMm.Count,
    thicknessMm == null ? 0 : thicknessMm.Count);

// Only ducts that carry BOTH figures go into the comparison. Adding every
// computed area to one side and only some to the other would report a
// disagreement made of the ducts Revit has no area for, which is not a
// disagreement at all.
double comparableComputed = 0;
double revitArea = 0;

foreach (var element in elements)
{
    if (element == null) continue;

    double width = 0, height = 0, diameter = 0, length = 0;
    try
    {
        var p = element.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
        if (p != null && p.HasValue) width = p.AsDouble() * FeetToMm;
    }
    catch { }
    try
    {
        var p = element.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
        if (p != null && p.HasValue) height = p.AsDouble() * FeetToMm;
    }
    catch { }
    try
    {
        var p = element.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
        if (p != null && p.HasValue) diameter = p.AsDouble() * FeetToMm;
    }
    catch { }
    try
    {
        var p = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        if (p != null && p.HasValue) length = p.AsDouble();
    }
    catch { }

    if (length <= 0) { unsized.Add(element.Id); continue; }

    // Revit's own answer first.
    string shape = null;
    try
    {
        var type = doc.GetElement(element.GetTypeId()) as MEPCurveType;
        if (type != null)
        {
            if (type.Shape == ConnectorProfileType.Round) shape = "round";
            else if (type.Shape == ConnectorProfileType.Rectangular) shape = "rectangular";
            else if (type.Shape == ConnectorProfileType.Oval) shape = "oval";
        }
    }
    catch { }

    if (shape == null)
    {
        // The fallback, and the oval case is why it is a fallback. Oval is
        // tested FIRST because it is the only shape carrying all three.
        shapeInferred.Add(element.Id);
        if (width > 0 && height > 0 && diameter > 0) shape = "oval";
        else if (diameter > 0) shape = "round";
        else if (width > 0 && height > 0) shape = "rectangular";
        else { unsized.Add(element.Id); continue; }
    }

    // A duct whose stated shape does not have the dimensions that shape needs
    // cannot be measured at all, and guessing the other shape here would put
    // the fallback's mistake back in through the side door.
    if (shape == "round" && diameter <= 0) { unsized.Add(element.Id); continue; }
    if (shape != "round" && (width <= 0 || height <= 0)) { unsized.Add(element.Id); continue; }

    double governingMm = shape == "round" ? diameter : Math.Max(width, height);

    int band = -1;
    for (int i = 0; i < bandCount; i++)
    {
        if (governingMm <= sizeBandsMm[i]) { band = i; break; }
    }
    if (band < 0) { outsideBands.Add(element.Id); continue; }

    // Developed area: the perimeter of the profile times the length, in feet.
    double perimeterFeet;
    if (shape == "rectangular")
        perimeterFeet = 2.0 * (width + height) / FeetToMm;
    else if (shape == "round")
        perimeterFeet = Math.PI * diameter / FeetToMm;
    else
    {
        double a = (width / FeetToMm) / 2.0;
        double b = (height / FeetToMm) / 2.0;
        double inner = (3.0 * a + b) * (a + 3.0 * b);
        perimeterFeet = inner > 0 ? Math.PI * (3.0 * (a + b) - Math.Sqrt(inner)) : 0;
    }

    double areaSquareFeet = perimeterFeet * length;
    if (areaSquareFeet <= 0) { unsized.Add(element.Id); continue; }

    double areaSquareMetres = areaSquareFeet * SquareFeetToSquareMetres;
    double kilos = areaSquareMetres * (thicknessMm[band] / 1000.0) * densityKgPerM3;

    sheetAreas[element.Id] = areaSquareFeet;
    weights[element.Id] = kilos;
    bandUsed[element.Id] = shape + " up to " + sizeBandsMm[band].ToString("0.##") +
                           " mm -> " + thicknessMm[band].ToString("0.###") + " mm metal";

    // Revit's own figure for the same duct, where it has one. Counted
    // separately so the comparison can say how much of the run it covers
    // instead of quietly assuming all of it.
    try
    {
        var own = element.get_Parameter(BuiltInParameter.RBS_CURVE_SURFACE_AREA);
        if (own != null && own.HasValue)
        {
            revitArea += own.AsDouble();
            comparableComputed += areaSquareFeet;
            areaChecked++;
        }
    }
    catch { }
}

// The gap between two independent calculations of the same quantity, as a
// fraction. Zero when there was nothing to check against, which `areaChecked`
// is what distinguishes - a zero difference and no check are not the same
// thing and must not read as agreement.
if (revitArea > 0) areaDifference = comparableComputed / revitArea - 1.0;
