// NOT STANDALONE. Assumes `elements` and `measure` are in scope, and leaves
// `quantities`, `lengths`, `areas`, `areaUnknown`, `lengthUnknown` and
// `refused` behind.
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
// It is recognised GENEROUSLY rather than matched exactly, so "area", "areas"
// and "surface area" all mean the same thing - AND SO DO "length", "lengths"
// and "total length". Both sides are matched the same way, which they were not
// until 2026-09-09: area was tested by substring and length against a closed
// list of single words, so "surface area" was understood and "total length"
// was REFUSED. A fragment that accepts a phrase for one of its two modes and
// demands a bare word for the other teaches nobody anything; it just fails on
// the half of the requests that were phrased naturally.
//
// AND IT IS MATCHED BY WORD, NOT BY SUBSTRING. "diameter" contains "meter" and
// "volume" contains "m", so a raw substring test hands back a length for two of
// the commonest wrong asks in this library - silently, which is the exact
// failure this fragment was fixed for in the first place. The words are split
// out and compared whole.
//
// AREA IS TESTED FIRST, AND THE ORDER IS THE WHOLE REASON IT IS SAFE. "square
// metres" is an area request carrying a length word inside it, and asking the
// length question first would answer it in millimetres. Anything naming an area
// wins outright, so a phrase carrying both reads as area.
//
// AND A WORD IT DOES NOT RECOGNISE IS REFUSED, NOT QUIETLY READ AS LENGTH.
//
// Until 2026-09-09 an unrecognised `measure` fell through to length. Proving
// it in front of a model showed what that costs: `measure=ZZZNOTHINGHERE`
// produced an answer IDENTICAL to a valid mode, which means the input had no
// effect at all and nothing about the fragment could be tested by varying it.
//
// On a real project it is worse than untestable. Somebody who asks for the
// wrong measure - "weight", "volume", "insulation" - gets a confident column
// of numbers in the wrong unit rather than a question, and every one of them
// is a length pretending to be something else. Refusing costs one round trip.
// Answering the wrong question costs whatever was billed against it.
//
// AN ABSENT `measure` IS STILL LENGTH, deliberately and narrowly: length is
// the only measure every linear element has, and an input nobody supplied is a
// different thing from an input somebody got wrong. A WORD that means nothing
// here is the one that gets refused.
var wantedMeasure = (measure ?? "").Trim().ToLowerInvariant();

string refused = null;
bool wantArea = false;

// Split on the punctuation a person actually types between words. Anything
// left is compared WHOLE, so "diameter" is never read as "meter".
var measureWords = new List<string>(wantedMeasure.Split(
    new[] { ' ', '\t', ',', '-', '/', '.', '_', '(', ')', ';', ':' },
    StringSplitOptions.RemoveEmptyEntries));

Func<string[], bool> namesOneOf = accepted =>
{
    foreach (var word in measureWords)
    {
        foreach (var candidate in accepted)
        {
            if (word == candidate) return true;
        }
    }
    return false;
};

var areaWords = new[] { "area", "areas", "sqm", "m2", "sq" };
var lengthWords = new[] { "length", "lengths", "long", "linear", "run", "runs",
                          "distance", "metre", "metres", "meter", "meters", "m", "mm" };

// "sheet metal" and "square metres" are PHRASES - neither word means area on
// its own, and "metal" and "metres" must not be read as length because of it.
var saysSheetMetal = wantedMeasure.IndexOf("sheet metal", StringComparison.Ordinal) >= 0;
var saysSquareMetres = namesOneOf(new[] { "square", "sq" })
    && namesOneOf(new[] { "metre", "metres", "meter", "meters", "m" });

// AREA FIRST. See the header: a phrase naming both reads as area.
if (namesOneOf(areaWords) || saysSheetMetal || saysSquareMetres)
{
    wantArea = true;
}
else if (wantedMeasure.Length == 0 || namesOneOf(lengthWords))
{
    wantArea = false;
}
else
{
    refused = string.Format(
        "'{0}' is not a measure this fragment takes, so NOTHING WAS MEASURED. It knows two: "
        + "LENGTH (length, linear, run, distance, metres - or leave it out) and AREA (area, "
        + "surface area, sheet metal, square metres). Either may be part of a longer phrase, "
        + "so 'total length' and 'surface area' both read. Reading an unknown word as length "
        + "would hand back a column of confident numbers in the wrong unit. If what you want "
        + "is weight, volume or insulation, that is a different fragment", measure);
}

var quantities = new Dictionary<ElementId, double>();
var lengths = new Dictionary<ElementId, double>();
var areas = new Dictionary<ElementId, double>();
var areaUnknown = new List<ElementId>();
var lengthUnknown = new List<ElementId>();

foreach (var element in elements)
{
    // Nothing is measured at all when the measure was not understood. A
    // partial answer here would be indistinguishable from a whole one.
    if (refused != null) break;
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
