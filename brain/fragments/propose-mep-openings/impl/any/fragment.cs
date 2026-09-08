// NOT STANDALONE. Assumes `doc`, `elements`, `hosts` and `allowanceMm` are in
// scope; leaves `proposals`, `hostIds`, `runIds`, `unmeasured` and `findings`
// behind.
//
// NO TRANSACTION, and it needs none. IT CREATES NOTHING - cutting holes in
// somebody's structure is not a side effect of looking for them.
//
// IT MEASURES THE REAL INTERSECTION, NOT A BOUNDING BOX. A duct crossing a wall
// at an angle needs a bigger hole than its own width, and a bounding-box answer
// is right only for a perpendicular crossing.

var proposals = new List<string>();
var hostIds = new List<ElementId>();
var runIds = new List<ElementId>();
var unmeasured = new List<string>();
var findings = new List<string>();

var mmPerFoot = 304.8;

if (allowanceMm < 0)
{
    findings.Add("A negative allowance makes no sense, so NOTHING WAS PROPOSED.");
}
else if (elements == null || elements.Count == 0 || hosts == null || hosts.Count == 0)
{
    findings.Add("Both services and hosts are needed, and at least one list was empty. "
        + "NOTHING WAS PROPOSED.");
}
else
{
    var options = new Options();
    options.ComputeReferences = false;
    options.DetailLevel = ViewDetailLevel.Fine;

    var intersectOptions = new SolidCurveIntersectionOptions();

    foreach (var run in elements)
    {
        if (run == null || !run.IsValidObject) continue;

        var curve = run as MEPCurve;
        if (curve == null)
        {
            unmeasured.Add(run.Id + " is not a duct, pipe, tray or conduit - it has no centre "
                + "line to intersect");
            continue;
        }

        var location = curve.Location as LocationCurve;
        if (location == null || location.Curve == null)
        {
            unmeasured.Add(curve.Id + " has no centre line");
            continue;
        }

        // Width, Height and Diameter THROW for the wrong profile rather than
        // returning zero, so each is asked for inside its own guard.
        var widthFt = 0.0;
        var heightFt = 0.0;
        var round = false;

        try { var d = curve.Diameter; if (d > 0) { widthFt = d; heightFt = d; round = true; } }
        catch { }

        if (!round)
        {
            try { widthFt = curve.Width; } catch { }
            try { heightFt = curve.Height; } catch { }
        }

        if (widthFt <= 0 && heightFt <= 0)
        {
            unmeasured.Add(curve.Id + " has no size this could read, so no hole size can be "
                + "proposed for it");
            continue;
        }

        foreach (var host in hosts)
        {
            if (host == null || !host.IsValidObject) continue;

            GeometryElement geometry = null;
            try { geometry = host.get_Geometry(options); } catch { }

            if (geometry == null)
            {
                unmeasured.Add(host.Id + " has no geometry this could read");
                continue;
            }

            foreach (GeometryObject shape in geometry)
            {
                var solid = shape as Solid;
                if (solid == null || solid.Volume <= 0) continue;

                SolidCurveIntersection crossing = null;
                try { crossing = solid.IntersectWithCurve(location.Curve, intersectOptions); }
                catch { continue; }

                if (crossing == null || crossing.SegmentCount == 0) continue;

                // A RUN THROUGH TWO WALLS IS TWO OPENINGS, and a run that
                // re-enters the same solid is two as well. Each segment is its
                // own hole because that is how many have to be cut.
                for (var i = 0; i < crossing.SegmentCount; i++)
                {
                    Curve segment = null;
                    try { segment = crossing.GetCurveSegment(i); } catch { continue; }
                    if (segment == null) continue;

                    var throughMm = segment.Length * mmPerFoot;
                    var midpoint = segment.Evaluate(0.5, true);

                    var holeWidthMm = widthFt * mmPerFoot + allowanceMm * 2.0;
                    var holeHeightMm = heightFt * mmPerFoot + allowanceMm * 2.0;

                    proposals.Add("host " + host.Id + " - service " + curve.Id
                        + " - opening " + Math.Round(holeWidthMm) + " x " + Math.Round(holeHeightMm)
                        + " mm" + (round ? " (round service, squared off)" : "")
                        + ", passing through " + Math.Round(throughMm) + " mm of host"
                        + ", centred at " + Math.Round(midpoint.X * mmPerFoot) + ", "
                        + Math.Round(midpoint.Y * mmPerFoot) + ", "
                        + Math.Round(midpoint.Z * mmPerFoot) + " mm");

                    hostIds.Add(host.Id);
                    runIds.Add(curve.Id);
                }
            }
        }
    }

    findings.Add(proposals.Count + " opening(s) proposed across " + elements.Count + " service(s) "
        + "and " + hosts.Count + " host(s), each sized from the service plus " + allowanceMm
        + " mm all round.");

    findings.Add("NOTHING WAS CREATED. This is a proposal to read and argue with. Cutting the "
        + "holes is a separate operation somebody has to approve.");
}

if (unmeasured.Count > 0)
{
    findings.Add(unmeasured.Count + " item(s) could not be measured and are NOT in the proposals: "
        + string.Join("; ", unmeasured.ToArray())
        + ". They are reported apart, because a hole schedule that silently omits what it could not "
        + "read is one that gets built from.");
}
