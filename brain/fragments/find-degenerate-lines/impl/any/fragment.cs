// NOT STANDALONE. Assumes `doc`, `categories` and `shorterThanMm` are in scope;
// leaves `elements`, `shortestMm`, `examined` and `findings`.
//
// A READ. It opens no transaction and needs none, and it deletes nothing.
//
// NOT FIND_OVERLAPPING_LINES. That one finds lines drawn on top of each other,
// both of which are real and visible. A degenerate line has almost no length: it
// prints as nothing and cannot be picked without a crossing window.
//
// THE THRESHOLD IS THE CALLER'S. Revit's own short-curve tolerance is about
// 0.8 mm, a 2 mm detail line is certainly a mistake, and a 50 mm one may be
// deliberate. Inventing the number would make it one somebody relies on (D-33).
//
// 304.8 appears once, converting the threshold in.

const double MillimetresPerFoot = 304.8;

var elements = new List<Element>();
var shortestMm = 0.0;
var examined = 0;
var findings = new List<string>();

if (shorterThanMm <= 0)
{
    findings.Add("No threshold was given. How short is too short is a judgement "
        + "about the job - Revit's own short-curve tolerance is about 0.8 mm, "
        + "and a 50 mm line may be perfectly deliberate");
}
else
{
    var limitFeet = shorterThanMm / MillimetresPerFoot;
    var found = false;

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

    var bounded = categories != null && categories.Count > 0;
    if (bounded)
    {
        var wanted = new List<BuiltInCategory>();
        foreach (var category in categories) wanted.Add(category);
        collector = collector.WherePasses(new ElementMulticategoryFilter(wanted));
    }

    foreach (var element in collector)
    {
        if (element == null) continue;

        var located = element.Location as LocationCurve;
        if (located == null) continue;

        Curve curve = null;
        try { curve = located.Curve; } catch { continue; }
        if (curve == null) continue;

        examined++;

        double length;
        try { length = curve.Length; } catch { continue; }

        var mm = length * MillimetresPerFoot;
        if (!found || mm < shortestMm)
        {
            shortestMm = mm;
            found = true;
        }

        if (length < limitFeet) elements.Add(element);
    }

    findings.Add(string.Format(
        "{0} of {1} curve-driven element(s) are shorter than {2:F1} mm. The "
        + "shortest is {3:F2} mm{4}",
        elements.Count, examined, shorterThanMm,
        found ? shortestMm : 0,
        bounded ? "" : ". THIS WAS UNBOUNDED - every duct, pipe and wall in the "
            + "model was measured. Give a category list if you meant lines"));

    if (elements.Count > 0)
    {
        findings.Add("Nothing has been deleted. DELETE_ELEMENTS is the other "
            + "half, and it reports what ELSE goes first - a line can take a "
            + "dimension with it");
    }
}
