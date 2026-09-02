// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `created`, `refused` and `notMepCurve` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// THE FITTING IS CHOSEN FROM THE GEOMETRY, which is what Revit itself does when
// one duct is dragged onto another:
//
//   2 ends, in line, same size        union
//   2 ends, in line, different size   transition
//   2 ends, at an angle               elbow
//   3 ends                            tee    - the odd one out is the branch
//   4 ends                            cross
//
// Asking the modeller which fitting to use is asking them to do this decision
// by hand, and it is a decision with one right answer.
//
// NOTHING IS MOVED TO CLOSE A GAP. Revit needs the ends at the same point, and
// ends that are apart are reported WITH THE DISTANCE rather than dragged
// together. Moving a run changes where services are, which is a decision
// somebody makes, not a detail this fragment tidies away.
//
// >> COINCIDENCE IS REQUIRED HERE AND IT MAY BE STRICTER THAN REVIT IS. The
// >> fitting calls may well accept ends that are apart but whose axes meet, and
// >> extend them. That is not assumed: requiring coincidence refuses a case
// >> that might have worked, where the looser rule would silently move
// >> ductwork. The first run against a real model is what settles it, and the
// >> refusal names the distance so that run has something to read.
//
// ONLY OPEN END CONNECTORS ARE CANDIDATES. A connector already carrying
// something is not an end to join, and a non-End connector is a tap or a
// reference rather than the end of a run.
//
// IsConnected IS BELIEVED HERE, WITH ITS LIMITS KNOWN. This library has already
// been caught out by it once: it describes what Revit RECORDS, not always what
// is physically joined, which is why TRACE_CONNECTIVITY walks the geometry
// instead. It is used here anyway and the risk runs the safe way - a connector
// wrongly marked connected is skipped, so the worst case is a refusal to build
// a fitting, never a fitting built on top of an existing joint.

var tolerance = toleranceMm / 304.8;

ElementId created = null;
string refused = null;
var notMepCurve = new List<ElementId>();

var runs = new List<MEPCurve>();

foreach (var element in elements)
{
    if (element == null) continue;

    var run = element as MEPCurve;
    if (run == null) { notMepCurve.Add(element.Id); continue; }

    runs.Add(run);
}

// The open ends of each run, in the same order as the runs.
var openEnds = new List<List<Connector>>();

foreach (var run in runs)
{
    var ends = new List<Connector>();

    var manager = run.ConnectorManager;
    if (manager != null)
    {
        foreach (Connector connector in manager.Connectors)
        {
            if (connector == null) continue;
            if (connector.ConnectorType != ConnectorType.End) continue;
            if (connector.IsConnected) continue;

            ends.Add(connector);
        }
    }

    openEnds.Add(ends);
}

if (runs.Count < 2 || runs.Count > 4)
{
    refused = string.Format(
        "a fitting joins two, three or four runs - {0} were given. Two makes an elbow, "
        + "a union or a transition, three a tee, four a cross", runs.Count);
}
else
{
    // One open end per run, chosen so that all of them sit at the same point.
    // Every combination is tried because a run has at most two ends, so this is
    // sixteen comparisons at the very worst.
    List<Connector> best = null;
    var bestSpread = double.MaxValue;

    var counts = new List<int>();
    var total = 1;
    foreach (var ends in openEnds) { counts.Add(ends.Count); total *= Math.Max(ends.Count, 1); }

    var anyEmpty = false;
    foreach (var count in counts) if (count == 0) anyEmpty = true;

    if (anyEmpty)
    {
        refused = "at least one of these runs has no open end - every connector on it is "
                + "already carrying something, so there is nothing here to join";
    }
    else
    {
        for (var pick = 0; pick < total; pick++)
        {
            var chosen = new List<Connector>();
            var remainder = pick;

            for (var i = 0; i < openEnds.Count; i++)
            {
                chosen.Add(openEnds[i][remainder % counts[i]]);
                remainder /= counts[i];
            }

            // How far apart the chosen ends are, worst pair. A fitting is built
            // where the ends MEET, so the worst pair is what has to be close.
            double spread = 0;
            for (var a = 0; a < chosen.Count; a++)
                for (var b = a + 1; b < chosen.Count; b++)
                    spread = Math.Max(spread, chosen[a].Origin.DistanceTo(chosen[b].Origin));

            if (spread < bestSpread) { bestSpread = spread; best = chosen; }
        }

        if (best == null)
        {
            refused = "no open ends could be paired up";
        }
        else if (bestSpread > tolerance)
        {
            refused = string.Format(
                "the nearest open ends are {0:0.#} mm apart and a fitting is built where they "
                + "MEET. Nothing is moved to close that - where the services run is a "
                + "decision, not a detail", bestSpread * 304.8);
        }
        else if (best.Count == 2)
        {
            var first = best[0];
            var second = best[1];

            // Facing each other means the run carries straight on through; any
            // other angle turns and needs an elbow. Measured on the connectors'
            // own outward directions rather than on the curves, because a
            // curve's direction depends on which end it was drawn from.
            var facing = first.CoordinateSystem.BasisZ.DotProduct(second.CoordinateSystem.BasisZ);
            var inLine = facing < -0.999;

            FamilyInstance fitting;

            if (!inLine)
            {
                fitting = doc.Create.NewElbowFitting(first, second);
            }
            else
            {
                // Same size or not. Shape is checked before size because
                // reading Radius off a rectangular connector throws, and a
                // round end meeting a rectangular one is a transition whatever
                // the numbers say.
                var sameSize = false;

                if (first.Shape == second.Shape)
                {
                    if (first.Shape == ConnectorProfileType.Round)
                    {
                        sameSize = Math.Abs(first.Radius - second.Radius) < 1e-6;
                    }
                    else if (first.Shape == ConnectorProfileType.Rectangular
                          || first.Shape == ConnectorProfileType.Oval)
                    {
                        sameSize = Math.Abs(first.Width - second.Width) < 1e-6
                                && Math.Abs(first.Height - second.Height) < 1e-6;
                    }
                }

                fitting = sameSize
                    ? doc.Create.NewUnionFitting(first, second)
                    : doc.Create.NewTransitionFitting(first, second);
            }

            if (fitting == null) refused = "Revit declined to build the fitting";
            else created = fitting.Id;
        }
        else
        {
            // Three or four ends. The MAIN run is the pair whose ends face each
            // other most nearly head-on; whatever is left is the branch. Taking
            // the first two in the order they were given would make the branch
            // the main run whenever the caller listed them in a different
            // order, and a tee built the wrong way round is a tee that has to
            // be deleted rather than adjusted.
            var mainA = 0;
            var mainB = 1;
            var mostOpposed = double.MaxValue;

            for (var a = 0; a < best.Count; a++)
                for (var b = a + 1; b < best.Count; b++)
                {
                    var facing = best[a].CoordinateSystem.BasisZ
                                     .DotProduct(best[b].CoordinateSystem.BasisZ);
                    if (facing < mostOpposed) { mostOpposed = facing; mainA = a; mainB = b; }
                }

            var branches = new List<Connector>();
            for (var i = 0; i < best.Count; i++)
                if (i != mainA && i != mainB) branches.Add(best[i]);

            FamilyInstance fitting = best.Count == 3
                ? doc.Create.NewTeeFitting(best[mainA], best[mainB], branches[0])
                : doc.Create.NewCrossFitting(best[mainA], best[mainB], branches[0], branches[1]);

            if (fitting == null) refused = "Revit declined to build the fitting";
            else created = fitting.Id;
        }
    }
}
