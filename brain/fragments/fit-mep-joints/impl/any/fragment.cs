// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `fitted`, `created`, `summary`, `freeEnds` and `notMepCurve` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// EVERY JOINT IN ONE CALL. PLACE_MEP_FITTING builds one fitting per call, so a
// run drawn as straight segments needed a selection and a call for every
// joint - 45 of each for five small rooms on 2026-09-23, most of an hour for
// work whose shape was fully known the moment the ducts were drawn. Here the
// runs are handed in once and every point where two, three or four open ends
// meet gets its fitting.
//
// THE FITTING IS CHOSEN FROM THE GEOMETRY, by the same rules as
// PLACE_MEP_FITTING, which are the rules Revit follows when one duct is dragged
// onto another:
//
//   2 ends, in line, same size        union
//   2 ends, in line, different size   transition
//   2 ends, at an angle               elbow
//   3 ends                            tee    - the most nearly head-on pair is
//                                              the main, the odd one the branch
//   4 ends                            cross
//   1 end on the SIDE of another run  tap    - the main is not cut; see
//                                              tapTarget below
//
// A LONE END IS NOT A JOINT - unless it lands on the side of another run. An end with nothing meeting it - the one at a
// unit's outlet, the one sitting on a diffuser's neck - is counted in
// `freeEnds` and left alone. Joining those to equipment and terminals is
// CONNECT_OPEN_ENDS, which needs no fitting.
//
// ALL OR NOTHING - ARTICLE 8. If any joint cannot be fitted, this THROWS, and
// the whole job is rolled back with every failed joint named in the message.
// Keeping forty-four fittings of forty-five leaves a run that looks finished
// and carries no air past the gap, which is the partial change Article 8
// forbids. The message is what the next attempt is built from.
//
// NOTHING IS MOVED TO CLOSE A GAP. Ends are grouped only when they already sit
// within the tolerance of one another. Ends that are apart stay apart and
// simply do not form a joint.
//
// EVERY FITTING IS READ BACK. A fitting call returning without an exception is
// not the same as the runs being joined to it, so each joint's ends are asked
// afterwards whether they are connected, and a joint whose ends say no is a
// failure like any other.

var mmPerFoot = 304.8;
var tolerance = toleranceMm / mmPerFoot;

var fitted = 0;
var created = new List<ElementId>();
string summary = null;
var freeEnds = 0;
var notMepCurve = new List<ElementId>();

// Every open end of every run handed in, with the run it belongs to.
var ends = new List<Connector>();
var owners = new List<ElementId>();
var runs = new List<MEPCurve>();
var runCount = 0;

foreach (var element in elements)
{
    if (element == null) continue;

    var run = element as MEPCurve;
    if (run == null) { notMepCurve.Add(element.Id); continue; }
    runCount++;
    runs.Add(run);

    var manager = run.ConnectorManager;
    if (manager == null) continue;

    foreach (Connector connector in manager.Connectors)
    {
        if (connector == null) continue;
        if (connector.ConnectorType != ConnectorType.End) continue;
        if (connector.IsConnected) continue;

        ends.Add(connector);
        owners.Add(run.Id);
    }
}

// The joints: every end within the tolerance of a group's first end, one end
// per run. Two ends of the same run never meet at a point, and letting them
// would build a fitting from a run to itself.
var taken = new bool[ends.Count];
var joints = new List<List<Connector>>();

for (var i = 0; i < ends.Count; i++)
{
    if (taken[i]) continue;
    taken[i] = true;

    var joint = new List<Connector> { ends[i] };
    var runsHere = new List<ElementId> { owners[i] };

    for (var j = i + 1; j < ends.Count; j++)
    {
        if (taken[j]) continue;
        if (runsHere.Contains(owners[j])) continue;
        if (ends[i].Origin.DistanceTo(ends[j].Origin) > tolerance) continue;

        taken[j] = true;
        joint.Add(ends[j]);
        runsHere.Add(owners[j]);
    }

    joints.Add(joint);
}

Func<XYZ, string> placeOf = p => string.Format("({0:0}, {1:0}, {2:0}) mm",
    p.X * mmPerFoot, p.Y * mmPerFoot, p.Z * mmPerFoot);

// A LONE END ON THE SIDE OF ANOTHER RUN IS A BRANCH TO TAP IN. Drawn to a
// point on another run's axis, clear of both its ends, the end gets a TAP -
// Revit's takeoff - and the run it lands on is NOT cut. That is how a duct
// type whose routing preference is "Taps" wants its branches: the owner's
// correction on 2026-09-23 was "tap, not tee". Which tap family is built is
// the duct type's own routing preference, never chosen here. Only a straight
// run can be tapped this way; its axis is where the end has to land.
Func<Connector, MEPCurve> tapTarget = end =>
{
    MEPCurve best = null;
    var bestDistance = double.MaxValue;

    foreach (var candidate in runs)
    {
        if (candidate.Id == end.Owner.Id) continue;

        var location = candidate.Location as LocationCurve;
        if (location == null) continue;
        var line = location.Curve as Line;
        if (line == null) continue;

        var projected = line.Project(end.Origin);
        if (projected == null || projected.Distance > tolerance) continue;

        var onAxis = projected.XYZPoint;
        if (onAxis.DistanceTo(line.GetEndPoint(0)) <= tolerance) continue;
        if (onAxis.DistanceTo(line.GetEndPoint(1)) <= tolerance) continue;

        if (projected.Distance < bestDistance) { bestDistance = projected.Distance; best = candidate; }
    }

    return best;
};

var failures = new List<string>();
var jointCount = 0;
int elbows = 0, unions = 0, transitions = 0, tees = 0, crosses = 0, taps = 0;

foreach (var joint in joints)
{
    if (joint.Count == 1)
    {
        var target = tapTarget(joint[0]);
        if (target == null) { freeEnds++; continue; }
        jointCount++;

        var tapAt = placeOf(joint[0].Origin);
        FamilyInstance tap = null;

        try { tap = doc.Create.NewTakeoffFitting(joint[0], target); }
        catch (Exception failure)
        {
            failures.Add(tapAt + ": Revit would not build the tap - " + failure.Message);
            continue;
        }

        if (tap == null) { failures.Add(tapAt + ": Revit returned no tap"); continue; }

        var tapJoined = false;
        try { tapJoined = joint[0].IsConnected; } catch { tapJoined = false; }
        if (!tapJoined)
        {
            failures.Add(tapAt + ": the tap was built but the branch end does not report being joined to it");
            continue;
        }

        created.Add(tap.Id);
        fitted++;
        taps++;
        continue;
    }
    jointCount++;

    var at = placeOf(joint[0].Origin);

    if (joint.Count > 4)
    {
        failures.Add(at + ": " + joint.Count + " ends meet there, and no fitting joins more than four");
        continue;
    }

    FamilyInstance fitting = null;
    var kind = "fitting";

    try
    {
        if (joint.Count == 2)
        {
            var first = joint[0];
            var second = joint[1];

            // Facing each other means the run carries straight on through; any
            // other angle turns. Measured on the connectors' own outward
            // directions, never on the curves, whose direction depends on
            // which end they were drawn from.
            var facing = first.CoordinateSystem.BasisZ.DotProduct(second.CoordinateSystem.BasisZ);

            if (facing > -0.999)
            {
                kind = "elbow";
                fitting = doc.Create.NewElbowFitting(first, second);
            }
            else
            {
                // Shape before size: reading Radius off a rectangular
                // connector throws, and round meeting rectangular is a
                // transition whatever the numbers say.
                var sameSize = false;
                if (first.Shape == second.Shape)
                {
                    if (first.Shape == ConnectorProfileType.Round)
                        sameSize = Math.Abs(first.Radius - second.Radius) < 1e-6;
                    else if (first.Shape == ConnectorProfileType.Rectangular
                          || first.Shape == ConnectorProfileType.Oval)
                        sameSize = Math.Abs(first.Width - second.Width) < 1e-6
                                && Math.Abs(first.Height - second.Height) < 1e-6;
                }

                // THE TRANSITION IS BUILT INTO THE LARGER RUN, ON PURPOSE.
                // Measured 2026-09-23 on Project1, Revit 2024: an 850x195 to
                // 300x300 transition came out 664 mm long and sat wholly on
                // the side of the connector passed FIRST. Left to the order
                // the ends were found in, it landed in whichever run happened
                // to come first, and a 600 mm run shorter than the fitting was
                // turned inside out - Revit's "modified to be in the opposite
                // direction". Passing the larger end first puts it where a
                // modeller expects it: in the run leaving the equipment.
                if (!sameSize && first.Shape != ConnectorProfileType.Round
                    && second.Shape != ConnectorProfileType.Round
                    && first.Width * first.Height < second.Width * second.Height)
                {
                    var larger = second;
                    second = first;
                    first = larger;
                }
                else if (!sameSize && first.Shape == ConnectorProfileType.Round
                         && second.Shape == ConnectorProfileType.Round
                         && first.Radius < second.Radius)
                {
                    var larger = second;
                    second = first;
                    first = larger;
                }

                kind = sameSize ? "union" : "transition";
                fitting = sameSize
                    ? doc.Create.NewUnionFitting(first, second)
                    : doc.Create.NewTransitionFitting(first, second);
            }
        }
        else
        {
            // The MAIN is the pair facing each other most nearly head-on, so a
            // tee is never built sideways because of the order ends were found.
            var mainA = 0;
            var mainB = 1;
            var mostOpposed = double.MaxValue;

            for (var a = 0; a < joint.Count; a++)
                for (var b = a + 1; b < joint.Count; b++)
                {
                    var facing = joint[a].CoordinateSystem.BasisZ
                                     .DotProduct(joint[b].CoordinateSystem.BasisZ);
                    if (facing < mostOpposed) { mostOpposed = facing; mainA = a; mainB = b; }
                }

            var branches = new List<Connector>();
            for (var k = 0; k < joint.Count; k++)
                if (k != mainA && k != mainB) branches.Add(joint[k]);

            kind = joint.Count == 3 ? "tee" : "cross";
            fitting = joint.Count == 3
                ? doc.Create.NewTeeFitting(joint[mainA], joint[mainB], branches[0])
                : doc.Create.NewCrossFitting(joint[mainA], joint[mainB], branches[0], branches[1]);
        }
    }
    catch (Exception failure)
    {
        failures.Add(at + ": Revit would not build the " + kind + " - " + failure.Message);
        continue;
    }

    if (fitting == null)
    {
        failures.Add(at + ": Revit returned no " + kind);
        continue;
    }

    var allJoined = true;
    foreach (var end in joint)
    {
        try { if (!end.IsConnected) allJoined = false; }
        catch { allJoined = false; }
    }

    if (!allJoined)
    {
        failures.Add(at + ": the " + kind + " was built but not every run end reports being joined to it");
        continue;
    }

    created.Add(fitting.Id);
    fitted++;

    if (kind == "elbow") elbows++;
    else if (kind == "union") unions++;
    else if (kind == "transition") transitions++;
    else if (kind == "tee") tees++;
    else crosses++;
}

if (failures.Count > 0)
{
    throw new InvalidOperationException(string.Format(
        "{0} of {1} joint(s) could not be fitted, so NOTHING was kept - one request is one "
        + "change, whole or not at all (Article 8). {2}",
        failures.Count, jointCount, string.Join(" | ", failures)));
}

summary = string.Format(
    "{0} joint(s) among {1} run(s), and all {2} fitted: {3} elbow(s), {4} tee(s), "
    + "{5} tap(s), {6} transition(s), {7} union(s), {8} cross(es). {9} free end(s) left "
    + "open - an end with nothing meeting it is not a joint; CONNECT_OPEN_ENDS joins those "
    + "to equipment and terminals",
    jointCount, runCount, fitted, elbows, tees, taps, transitions, unions, crosses, freeEnds);
