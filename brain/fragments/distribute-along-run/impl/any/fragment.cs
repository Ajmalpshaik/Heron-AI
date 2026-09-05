// NOT STANDALONE. Assumes `doc`, `run`, `symbol`, `spacingMm` and
// `startOffsetMm` are in scope; leaves `placed`, `leftoverMm` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a run full of hangers is one
// undo.
//
// THE SPACING IS NOT STRETCHED TO FIT. A 7.3 m run at 2 m centres takes three
// and leaves 1.3 m over. Quietly using 1.825 m to come out even is a DIFFERENT
// instruction, and it is how a support ends up somewhere the drawing says it is
// not. The leftover is reported and the decision stays with whoever asked.
//
// WHAT IS PLACED IS NOT HOSTED ON THE RUN. These are point-placed instances
// standing where the run is today; move the duct and they stay behind. Revit's
// own hanger families behave the same way and it surprises people every time,
// so it is in the read-back rather than left to be discovered.

const double MillimetresPerFoot = 304.8;

var placed = new List<ElementId>();
var leftoverMm = 0.0;
var findings = new List<string>();

var location = run == null ? null : run.Location as LocationCurve;
var curve = location == null ? null : location.Curve;

if (run == null || symbol == null)
{
    findings.Add("A run and a family type are both needed - name the element to follow and the "
        + "family to place along it");
}
else if (curve == null)
{
    findings.Add(string.Format("{0} is not placed along a line or a curve, so there is nothing to "
        + "distribute along", run.Name));
}
else if (spacingMm <= 0)
{
    findings.Add(string.Format("A spacing above zero is needed - {0} mm was given. Nothing placed",
        spacingMm));
}
else
{
    var lengthFeet = curve.Length;
    var lengthMm = lengthFeet * MillimetresPerFoot;
    var offsetMm = startOffsetMm < 0 ? 0 : startOffsetMm;

    if (offsetMm * 2 >= lengthMm)
    {
        findings.Add(string.Format("A start offset of {0:0} mm at each end leaves nothing of a "
            + "{1:0} mm run to place along. Nothing placed", offsetMm, lengthMm));
    }
    else
    {
        var usableMm = lengthMm - offsetMm * 2;
        var intervals = (int)Math.Floor(usableMm / spacingMm);
        var count = intervals + 1;
        leftoverMm = usableMm - intervals * spacingMm;

        try
        {
            if (!symbol.IsActive) symbol.Activate();

            for (var i = 0; i < count; i++)
            {
                var alongMm = offsetMm + i * spacingMm;
                var alongFeet = alongMm / MillimetresPerFoot;

                // Normalised so it works for any curve, not only a straight line.
                var parameter = alongFeet / lengthFeet;
                if (parameter < 0) parameter = 0;
                if (parameter > 1) parameter = 1;

                var point = curve.Evaluate(parameter, true);

                var instance = doc.Create.NewFamilyInstance(point, symbol,
                    StructuralType.NonStructural);

                if (instance != null) placed.Add(instance.Id);
            }

            findings.Add(string.Format("{0} of {1} placed at {2:0} mm centres along a {3:0} mm run, "
                + "starting {4:0} mm in. {5:0} mm is left over at the far end and the spacing was NOT "
                + "stretched to absorb it",
                placed.Count, count, spacingMm, lengthMm, offsetMm, leftoverMm));

            if (placed.Count < count)
                findings.Add(string.Format("{0} of them did not come back as elements. Revit refused "
                    + "those placements and the rest continued", count - placed.Count));

            if (placed.Count > 0)
                findings.Add("These are NOT hosted on the run. They stand where it is today, and "
                    + "moving the run leaves them behind");
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Placing along the run failed after {0}: {1}. The caller's "
                + "transaction is what undoes the ones already made", placed.Count, ex.Message));
        }
    }
}
