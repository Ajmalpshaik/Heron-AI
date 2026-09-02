// NOT STANDALONE. Assumes `doc` is in scope, and leaves `levels`,
// `elevations`, `projectElevations`, `worstDifference`, `affected`, `atRisk`
// and `typesDisagree` behind.
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
bool atRisk = !affected && surveyOffsetKnown && Math.Abs(surveyOffset) > Tolerance;
bool typesDisagree = basesSeen.Count > 1;
