// NOT STANDALONE. Assumes `doc`, `categories` and `shorterThanMm` are in scope;
// leaves `elements`, `shortestMm`, `examined`, `skippedCurves` and `findings`.
//
// VERSION 2 (2026-11-24, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - A LINE WHOSE CURVE WILL NOT READ IS COUNTED. Version 1 passed over
// an element whose location curve or length Revit would not give without a
// word, so "0 of N are shorter" could leave out the very line that is.
// `skippedCurves` carries the count every time; the sentence naming it is
// added only when it is above 0, so every other answer reads as version 1's.
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
// VERSION 2 (D-64): curve-driven elements whose curve or length would not read.
var skippedCurves = 0;
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
        try { curve = located.Curve; } catch { skippedCurves++; continue; }
        if (curve == null) { skippedCurves++; continue; }

        examined++;

        double length;
        try { length = curve.Length; } catch { skippedCurves++; continue; }

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

    // VERSION 2 (D-64): said only when a curve was passed over.
    if (skippedCurves > 0)
        findings.Add(string.Format("{0} element(s) with a location line would not give their curve or "
            + "its length, so they were NOT measured - one of them could be shorter than the threshold",
            skippedCurves));

    if (elements.Count > 0)
    {
        findings.Add("Nothing has been deleted. DELETE_ELEMENTS is the other "
            + "half, and it reports what ELSE goes first - a line can take a "
            + "dimension with it");
    }
}
