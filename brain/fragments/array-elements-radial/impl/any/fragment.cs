// NOT STANDALONE. Assumes `doc`, `elements`, `centreXMm`, `centreYMm`,
// `totalSweepDegrees` and `count` are in scope; leaves `created`,
// `copiesEach`, `stepDegrees`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET AND DEGREES TO RADIANS BY ARITHMETIC (D-20).
//
// A FULL CIRCLE AND A PARTIAL SWEEP DIVIDE DIFFERENTLY, and this is the whole
// correctness of the fragment:
//
//   full circle    step = sweep / count        the last gap closes to the start
//   partial sweep  step = sweep / (count - 1)  both ends are occupied
//
// Always dividing by the count puts the last copy of a 360 degree array at 360
// degrees - exactly on top of the original. That is a stacked duplicate: it
// looks right on screen, schedules twice and counts twice. The earlier version
// of this idea did precisely that.
//
// THE COUNT IS THE TOTAL, INCLUDING THE ORIGINAL. Eight means eight things
// afterwards, not nine - the same promise ARRAY_ELEMENTS makes.
//
// THE AXIS IS VERTICAL through the point given, which is the array anybody
// draws on a plan.
//
// EVERY COPY IS COUNTED FROM THE MODEL. A rotation Revit refuses leaves fewer
// copies than were asked for, and a count taken from the request would report
// an array that is not there.

var mmPerFoot = 304.8;

var created = new List<ElementId>();
var refused = new List<string>();
var findings = new List<string>();
var copiesEach = 0;
var stepDegrees = 0.0;

var sourceIds = new List<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    sourceIds.Add(element.Id);
}

if (sourceIds.Count == 0)
{
    findings.Add("Nothing was given to array.");
}
else if (count < 2)
{
    // One item is the original. There is no array.
    findings.Add("A count of " + count + " asks for no copies at all - the count is the TOTAL "
        + "including the original, so two is the smallest array there is.");
}
else
{
    // Is the sweep a whole number of full turns? Then the last gap closes back
    // to the start and the step divides by the count rather than by count - 1.
    var turns = totalSweepDegrees / 360.0;
    var closesOnItself = Math.Abs(turns - Math.Round(turns)) < 1e-9
        && Math.Abs(totalSweepDegrees) > 1e-9;

    stepDegrees = closesOnItself
        ? totalSweepDegrees / count
        : totalSweepDegrees / (count - 1);

    findings.Add(closesOnItself
        ? "Full circle: the step is the sweep divided by the count, because the last gap closes "
          + "back to the start. Dividing by count minus one here would put the final copy on top "
          + "of the original."
        : "Partial sweep: the step is the sweep divided by the count minus one, because both ends "
          + "of the sweep are occupied.");

    if (Math.Abs(stepDegrees) < 1e-9)
    {
        findings.Add("The step works out at zero degrees, so every copy would land on the "
            + "original. Nothing was created.");
    }
    else
    {
        var centre = new XYZ(centreXMm / mmPerFoot, centreYMm / mmPerFoot, 0.0);
        var axis = Line.CreateBound(centre, centre + XYZ.BasisZ);

        var stepRadians = stepDegrees * Math.PI / 180.0;

        // count - 1 copies, because the original is one of the count.
        for (int i = 1; i < count; i++)
        {
            // ICollection, not IList - that is what CopyElements returns, and the
            // compile gate caught the assumption.
            ICollection<ElementId> copies = null;

            try
            {
                // Copied where they stand, then rotated into place. Copying at
                // an offset first would need the offset worked out per element,
                // and the rotation does that anyway.
                copies = ElementTransformUtils.CopyElements(doc, sourceIds, XYZ.Zero);
            }
            catch (Exception failure)
            {
                refused.Add("Copy " + i + " failed - " + failure.Message);
                continue;
            }

            if (copies == null || copies.Count == 0)
            {
                refused.Add("Copy " + i + " produced nothing.");
                continue;
            }

            try
            {
                ElementTransformUtils.RotateElements(doc, copies, axis, stepRadians * i);
            }
            catch (Exception failure)
            {
                // The copies exist but are sitting on the originals. Left in
                // place and reported rather than deleted - deleting something
                // this fragment created, inside somebody else's transaction, is
                // not its call to make.
                refused.Add("Copy " + i + " was made but would not rotate - " + failure.Message
                    + ". It is sitting on the original.");
            }

            // Counted from the model. A copy that did not survive is not a copy.
            foreach (var id in copies)
            {
                if (doc.GetElement(id) == null) continue;
                created.Add(id);
            }
        }

        copiesEach = sourceIds.Count == 0 ? 0 : created.Count / sourceIds.Count;

        findings.Add("Arrayed " + sourceIds.Count + " element(s) into " + count + " total position(s) "
            + "at " + stepDegrees.ToString("F2") + " degrees apart, around ("
            + centreXMm.ToString("F0") + ", " + centreYMm.ToString("F0") + ") mm. Created "
            + created.Count + " new element(s); the originals were not moved.");
    }
}
