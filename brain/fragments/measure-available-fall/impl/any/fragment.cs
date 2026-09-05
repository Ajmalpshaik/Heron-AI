// NOT STANDALONE. Assumes `doc`, `upstream` and `downstream` are in scope;
// leaves `fallMm`, `runMm`, `gradientOneIn` and `findings` behind.
//
// THE RUN LENGTH IS THE HORIZONTAL DISTANCE, NOT THE STRAIGHT LINE. A drain is
// laid to a gradient measured along the plan; using the sloping length
// overstates the fall available - by little on a shallow run and by enough to
// matter on a steep one.
//
// A RISE IS REPORTED AS A RISE. Water does not run uphill, so a downstream
// point above the upstream one is a finding, not a negative number somebody
// might carry into a calculation.
//
// IT SAYS WHAT IS AVAILABLE, NEVER WHAT IS REQUIRED. Whether 1 in 80 will do
// depends on pipe size, flow, and the standard being worked to. That is a
// judgement and it is not this fragment's.

const double MillimetresPerFoot = 304.8;

var fallMm = 0.0;
var runMm = 0.0;
var gradientOneIn = 0.0;
var findings = new List<string>();

if (upstream == null || downstream == null)
{
    findings.Add("Two elements are needed - the upstream point and the downstream point");
}
else if (upstream.Id == downstream.Id)
{
    findings.Add("Both points are the same element, so there is no run and no fall to measure");
}
else
{
    var boxUp = upstream.get_BoundingBox(null);
    var boxDown = downstream.get_BoundingBox(null);

    if (boxUp == null || boxDown == null)
    {
        findings.Add("One of the two has no geometry to measure from, so it has no level and no "
            + "position");
    }
    else
    {
        // The invert is what a drain is set out from, so the LOWEST point of
        // each element is the one that matters - not its centre.
        var upZ = boxUp.Min.Z;
        var downZ = boxDown.Min.Z;

        var upCentre = (boxUp.Min + boxUp.Max) / 2.0;
        var downCentre = (boxDown.Min + boxDown.Max) / 2.0;

        var dx = downCentre.X - upCentre.X;
        var dy = downCentre.Y - upCentre.Y;
        var horizontalFeet = Math.Sqrt(dx * dx + dy * dy);

        runMm = horizontalFeet * MillimetresPerFoot;
        fallMm = (upZ - downZ) * MillimetresPerFoot;

        if (runMm < 1e-6)
        {
            findings.Add(string.Format("The two points are vertically above one another, so there is "
                + "no horizontal run to fall along. The height difference is {0:0.#} mm", fallMm));
        }
        else if (fallMm > 0)
        {
            gradientOneIn = runMm / fallMm;

            findings.Add(string.Format("{0:0.#} mm of fall over {1:0} mm of horizontal run, which is "
                + "1 in {2:0.#}", fallMm, runMm, gradientOneIn));

            findings.Add("That is what is AVAILABLE. Whether it is enough depends on the pipe size, "
                + "the flow and the standard being worked to, and this fragment does not know any of "
                + "those");
        }
        else if (fallMm < 0)
        {
            findings.Add(string.Format("The downstream point is {0:0.#} mm HIGHER than the upstream "
                + "one over {1:0} mm of run. That is a rise, not a fall - water will not run this way "
                + "and no gradient is reported", -fallMm, runMm));
        }
        else
        {
            findings.Add(string.Format("The two points are at exactly the same level over {0:0} mm of "
                + "run, so there is no fall available at all", runMm));
        }

        findings.Add("Measured from the LOWEST point of each element, which is what a drain is set "
            + "out from, and along the horizontal distance between their centres - a gradient is "
            + "measured on plan, not along the slope");
    }
}
