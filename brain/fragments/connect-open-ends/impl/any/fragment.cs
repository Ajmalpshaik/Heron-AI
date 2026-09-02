// NOT STANDALONE. Assumes `doc`, `elements`, `maxGapMm` and `sizeToleranceMm`
// are in scope; leaves `connected`, `findings`, `openEnds`, `notPaired` and
// `noConnectors` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// FOUR TESTS, AND ALL FOUR HAVE TO PASS BEFORE ANYTHING IS JOINED.
// ===========================================================================
//
//   1  BOTH OPEN        a connector Revit already believes is joined is left
//                       alone. Joining over the top of one throws.
//   2  SAME DOMAIN      a duct connector is never joined to a pipe connector,
//                       whatever the geometry says.
//   3  SIZES AGREE      within what the caller allows. A 100 and a 150 meeting
//                       at a point is a reducer's job, not a join.
//   4  FACING           the two connectors point INTO each other - the dot
//                       product of their directions is negative. THIS IS THE
//                       TEST THE OTHERS CANNOT STAND IN FOR: two pipes crossing
//                       at the same height pass 1, 2 and 3 and are not a joint.
//
// NOTHING IS MOVED. ConnectTo joins two connectors where they already are. So
// the gap allowed is a rounding gap, not a hole. Widening it to swallow a real
// hole gives a model that REPORTS connected while the geometry is still open,
// which is the more expensive of the two problems.
//
// EACH CONNECTOR IS USED ONCE. Pairs are taken smallest-gap first and both ends
// struck off as each is taken. Where three pieces meet at one point, one open
// end would otherwise be claimed twice and the second join would throw.
//
// EVERY JOIN IS READ BACK. ConnectTo returning without an exception is not the
// same as two connectors being joined, and this library counts what happened,
// never what was asked for.

var connected = 0;
var findings = new List<string>();
var notPaired = new List<string>();
var noConnectors = new List<ElementId>();

var mmPerFoot = 304.8;
var maxGap = maxGapMm / mmPerFoot;
var sizeTolerance = sizeToleranceMm / mmPerFoot;

Func<Element, ConnectorManager> managerOf = e =>
{
    var curve = e as MEPCurve;
    if (curve != null) return curve.ConnectorManager;
    var instance = e as FamilyInstance;
    if (instance != null && instance.MEPModel != null) return instance.MEPModel.ConnectorManager;
    return null;
};

// There is no ONE size property. A round connector has a radius; a rectangular
// one has a width and a height. "Size" here is a comparable nominal figure per
// shape, and the label says which so a rejected pair can be read.
Func<Connector, double> nominalSize = c =>
{
    try
    {
        if (c.Shape == ConnectorProfileType.Round) return c.Radius * 2.0;
        return Math.Max(c.Width, c.Height);
    }
    catch { return -1.0; }
};

Func<Connector, string> sizeLabel = c =>
{
    try
    {
        if (c.Shape == ConnectorProfileType.Round)
            return string.Format("{0:0} round", c.Radius * 2.0 * mmPerFoot);
        return string.Format("{0:0}x{1:0}", c.Width * mmPerFoot, c.Height * mmPerFoot);
    }
    catch { return "size unreadable"; }
};

var owners = new List<Element>();
var ends = new List<Connector>();

foreach (var element in elements)
{
    if (element == null) continue;
    var manager = managerOf(element);
    if (manager == null) { noConnectors.Add(element.Id); continue; }
    try
    {
        foreach (Connector connector in manager.Connectors)
        {
            if (connector.ConnectorType != ConnectorType.End) continue;
            if (connector.Domain == Domain.DomainUndefined) continue;
            if (connector.IsConnected) continue;
            owners.Add(element);
            ends.Add(connector);
        }
    }
    catch
    {
        noConnectors.Add(element.Id);
    }
}

var openEnds = ends.Count;

// Score every candidate pair, then take them best-first.
var pairA = new List<int>();
var pairB = new List<int>();
var pairGap = new List<double>();

for (var i = 0; i < ends.Count; i++)
{
    for (var j = i + 1; j < ends.Count; j++)
    {
        // An element's own two ends are never a pair.
        if (owners[i].Id == owners[j].Id) continue;

        double gap;
        try { gap = ends[i].Origin.DistanceTo(ends[j].Origin); }
        catch { continue; }
        if (gap > maxGap) continue;

        if (ends[i].Domain != ends[j].Domain)
        {
            notPaired.Add(string.Format("{0} and {1}: different domain ({2} against {3})",
                owners[i].Id, owners[j].Id, ends[i].Domain, ends[j].Domain));
            continue;
        }

        var sizeI = nominalSize(ends[i]);
        var sizeJ = nominalSize(ends[j]);
        if (sizeI < 0 || sizeJ < 0 || Math.Abs(sizeI - sizeJ) > sizeTolerance)
        {
            notPaired.Add(string.Format("{0} and {1}: {2} against {3}, {4:0} mm apart - a size change is a fitting, not a join",
                owners[i].Id, owners[j].Id, sizeLabel(ends[i]), sizeLabel(ends[j]), gap * mmPerFoot));
            continue;
        }

        double facing;
        try
        {
            facing = ends[i].CoordinateSystem.BasisZ.Normalize()
                .DotProduct(ends[j].CoordinateSystem.BasisZ.Normalize());
        }
        catch { continue; }

        if (facing > 0)
        {
            notPaired.Add(string.Format("{0} and {1}: not facing each other - {2:0.00}, so a crossing rather than a joint",
                owners[i].Id, owners[j].Id, facing));
            continue;
        }

        pairA.Add(i);
        pairB.Add(j);
        pairGap.Add(gap);
    }
}

var order = Enumerable.Range(0, pairGap.Count).OrderBy(k => pairGap[k]).ToList();
var used = new HashSet<int>();

foreach (var k in order)
{
    var i = pairA[k];
    var j = pairB[k];
    if (used.Contains(i) || used.Contains(j)) continue;

    try
    {
        ends[i].ConnectTo(ends[j]);

        // READ IT BACK. A call that returned is not a joint.
        var reallyJoined = false;
        try { reallyJoined = ends[i].IsConnected && ends[j].IsConnected; }
        catch { reallyJoined = false; }

        if (!reallyJoined)
        {
            notPaired.Add(string.Format(
                "{0} and {1}: the join was accepted and they are STILL open - Revit refused it quietly",
                owners[i].Id, owners[j].Id));
            continue;
        }

        used.Add(i);
        used.Add(j);
        connected++;
        findings.Add(string.Format("{0} joined to {1}  - {2}, gap was {3:0.0} mm",
            owners[i].Id, owners[j].Id, sizeLabel(ends[i]), pairGap[k] * mmPerFoot));
    }
    catch (Exception ex)
    {
        notPaired.Add(string.Format("{0} and {1}: {2}", owners[i].Id, owners[j].Id, ex.Message));
    }
}
