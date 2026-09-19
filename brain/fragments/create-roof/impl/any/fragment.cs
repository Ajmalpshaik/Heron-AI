// NOT STANDALONE. Assumes `doc`, `level`, `roofType` and `boundary` are in
// scope; leaves `created`, `edges` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// ONE CALL FOR EVERY RELEASE. Unlike the floor, NewFootPrintRoof did not change
// at 2022 - checked against the reference assemblies at both ends on
// 2026-09-18 - so there is no run-time lookup here and there should not be one.
//
// THE BASE HEIGHT IS THE LEVEL'S PROJECT ELEVATION, not its Elevation. On a
// survey-datum model those differ and the wrong one puts the roof metres out
// with no error at all.
//
// IT MAKES A FLAT ROOF. The out parameter is the boundary's model curves, which
// is what a slope would be set on, per edge, afterwards. Inventing a pitch here
// would become a pitch quietly relied on.

const double MillimetresPerFoot = 304.8;

var created = ElementId.InvalidElementId;
var edges = 0;
var measuredMm = "";
var findings = new List<string>();

if (boundary == null || boundary.Count < 3)
{
    findings.Add(string.Format(
        "A roof needs at least three boundary points and {0} were given",
        boundary == null ? 0 : boundary.Count));
}
else if (roofType == null)
{
    findings.Add("No roof type was given");
}
else if (level == null)
{
    findings.Add("No level was given");
}
else
{
    // NOT level.Elevation - see the header.
    var z = level.ProjectElevation;

    var footprint = new CurveArray();
    var badPoint = false;

    for (var i = 0; i < boundary.Count; i++)
    {
        var a = boundary[i];
        var b = boundary[(i + 1) % boundary.Count];
        if (a == null || b == null) { badPoint = true; break; }
        try
        {
            footprint.Append(Line.CreateBound(
                new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
        }
        catch
        {
            badPoint = true;
            break;
        }
    }

    if (badPoint)
    {
        findings.Add("Two boundary points are in the same place, so one edge has "
            + "no length - nothing was created");
    }
    else
    {
        try
        {
            // INSTANTIATED EVEN THOUGH IT IS AN `out`, AND THAT IS THE WHOLE
            // DEFECT. Declared `= null` this call threw
            // "Value cannot be null" on a model that had a real RoofType, a
            // real Level and a well-formed four-point boundary - measured
            // 2026-09-19 on `test projject`, Revit 2024, where the binder had
            // already PROVED the type resolves by refusing a made-up name in
            // the same arrangement. Autodesk's own sample for this call
            // instantiates the array first; the parameter crosses the interop
            // boundary and a null going in is not the same as an unassigned
            // local. `out` reassigns it immediately afterwards, so this line
            // costs one allocation and buys the call working at all.
            ModelCurveArray madeEdges = new ModelCurveArray();
            var roof = doc.Create.NewFootPrintRoof(footprint, level, roofType, out madeEdges);

            if (roof == null)
            {
                findings.Add("Revit returned no roof - the usual cause is a "
                    + "boundary that crosses itself");
            }
            else
            {
                created = roof.Id;
                edges = madeEdges == null ? 0 : madeEdges.Size;
                findings.Add(string.Format(
                    "Roof created on '{0}' with {1} edge(s). It is FLAT - no "
                    + "slope has been set on any edge, which is a separate job",
                    level.Name, edges));

                // WHAT IT ACTUALLY MADE, MEASURED BACK OFF THE ELEMENT AND IN
                // MILLIMETRES - the units the caller typed.
                //
                // AN ID AND AN EDGE COUNT ARE NOT A PROOF, WHICH IS THE WHOLE
                // REASON THIS IS HERE. A boundary read as FEET instead of
                // millimetres returns the same id and the same four edges and
                // makes a roof a mile and a half across; a base taken from
                // `Elevation` instead of `ProjectElevation` puts it metres out
                // on a survey-datum model, and this file's own header warns
                // about that one. Neither shows in any number the fragment
                // used to report, so no proof could ever have caught them.
                // Named by review on PR #198, which asked for exactly this
                // before the promotion - and the fragment's own cases.yaml had
                // been asking for it all along: "checked IN A SECTION against
                // the level rather than read off the report".
                //
                // READ BACK, never recomputed from the input: the point is to
                // ask REVIT what is there, so a conversion that went wrong on
                // the way in comes back wrong and visible.
                try
                {
                    var box = roof.get_BoundingBox(null);
                    if (box != null)
                    {
                        measuredMm = string.Format(
                            "{0:F0} x {1:F0} mm on plan, base at {2:F0} mm, "
                            + "level '{3}' sits at {4:F0} mm",
                            (box.Max.X - box.Min.X) * MillimetresPerFoot,
                            (box.Max.Y - box.Min.Y) * MillimetresPerFoot,
                            box.Min.Z * MillimetresPerFoot,
                            level.Name,
                            level.ProjectElevation * MillimetresPerFoot);
                        findings.Add("Measured back from Revit - see measuredMm");
                    }
                    else
                    {
                        measuredMm = "";
                        findings.Add("Revit returned no bounding box for the new roof, "
                            + "so its size and height were NOT read back");
                    }
                }
                catch (Exception measured)
                {
                    findings.Add(string.Format(
                        "The roof was created but could not be measured back: {0}",
                        measured.Message));
                }
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Roof creation failed: {0}", ex.Message));
        }
    }
}
