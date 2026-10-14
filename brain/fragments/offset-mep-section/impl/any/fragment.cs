// NOT STANDALONE. Assumes `doc`, `run`, `startMm`, `endMm`, `offsetMm` and
// `angleDegrees` are in scope; leaves `runs`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// BREAK, MOVE, DRAW - NOT FIT. Two breaks with Revit's own break (the one
// SPLIT_MEP_RUN uses), so the run's far ends keep their connections. The three
// pieces are told apart by geometry, not by which id came back, because which
// half keeps the original id is Revit's business. The joints at the two
// points are opened, the middle piece is moved, and the two legs are drawn.
// FIT_MEP_JOINTS on `runs` makes the four elbows, all or nothing.

var runs = new List<Element>();
var refused = new List<string>();
var findings = new List<string>();

var mm = 304.8;
var isDuct = run is Duct;
var isPipe = run is Pipe;
var curve = run == null ? null : run as MEPCurve;
Line line = null;
if (curve != null)
{
    var place = curve.Location as LocationCurve;
    if (place != null) line = place.Curve as Line;
}

var go = true;
if (run == null) { refused.Add("No duct or pipe was given."); go = false; }
else if (!isDuct && !isPipe)
{
    refused.Add(string.Format("{0} is not a duct or a pipe. Revit gives a break for those two only - cable "
        + "tray and conduit cannot be rerouted this way.", run.Id));
    go = false;
}
else if (line == null)
{
    refused.Add(string.Format("{0} is not one straight run, so it was not touched.", run.Id));
    go = false;
}
else if (Math.Abs(line.Direction.Z) > 0.5)
{
    refused.Add(string.Format("{0} rises more than it runs. Up and down are the offset here; a riser needs "
        + "a sideways offset this version does not make.", run.Id));
    go = false;
}
else if (Math.Abs(angleDegrees - 90.0) > 0.01 && Math.Abs(angleDegrees - 45.0) > 0.01)
{
    refused.Add(string.Format("{0:0.#} degrees was asked for; the legs are drawn at 90 or at 45.", angleDegrees));
    go = false;
}
else if (Math.Abs(offsetMm) < 1.0)
{
    refused.Add("An offset of nothing was asked for, so the run was not touched.");
    go = false;
}

double d1 = 0, d2 = 0, length = 0, lean = 0;
if (go)
{
    length = line.Length;
    d1 = Math.Min(startMm, endMm) / mm;
    d2 = Math.Max(startMm, endMm) / mm;
    lean = Math.Abs(angleDegrees - 45.0) < 0.01 ? Math.Abs(offsetMm) / mm : 0.0;
    if (d1 <= 1.0 / mm || d2 >= length - 1.0 / mm)
    {
        refused.Add(string.Format("The stretch {0:0} to {1:0} mm does not fall inside the run, which is "
            + "{2:0} mm long measured from its start.", d1 * mm, d2 * mm, length * mm));
        go = false;
    }
    else if (d2 - d1 <= 2.0 * lean + 1.0 / mm)
    {
        refused.Add(string.Format("At 45 degrees each leg runs {0:0} mm along, so the stretch must be longer "
            + "than {1:0} mm - it is {2:0}.", lean * mm, 2.0 * lean * mm, (d2 - d1) * mm));
        go = false;
    }
    else
    {
        // A BRANCH TAPPED INTO THE STRETCH WOULD BE TORN OFF when it moves, so
        // it is looked for BEFORE anything is broken: a connected connector
        // whose point lies between the two distances.
        var tapped = new List<string>();
        foreach (Connector c in curve.ConnectorManager.Connectors)
        {
            if (!c.IsConnected) continue;
            var at = (c.Origin - line.GetEndPoint(0)).DotProduct(line.Direction);
            if (at > d1 - 1.0 / mm && at < d2 + 1.0 / mm)
            {
                foreach (Connector r in c.AllRefs)
                    if (r != null && r.Owner != null && r.Owner.Id != run.Id)
                        tapped.Add(string.Format("{0} at {1:0} mm", r.Owner.Id, at * mm));
            }
        }
        if (tapped.Count > 0)
        {
            refused.Add(string.Format("Something is connected to {0} inside the stretch - {1} - and would be "
                + "torn off when it moves. Nothing was changed; move the stretch clear of it, or reroute the "
                + "branch first.", run.Id, string.Join(", ", tapped)));
            go = false;
        }
    }
}

if (go)
{
    var start = line.GetEndPoint(0);
    var end = line.GetEndPoint(1);
    var along = line.Direction;
    var a = start + along * d1;
    var b = start + along * d2;
    var up = new XYZ(0, 0, offsetMm / mm);

    // Everything the legs copy, read before the run is touched.
    var typeId = run.GetTypeId();
    var level = curve.ReferenceLevel;
    var systemParam = run.get_Parameter(isDuct ? BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM
                                               : BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
    var systemTypeId = systemParam != null && systemParam.HasValue ? systemParam.AsElementId()
                                                                   : ElementId.InvalidElementId;
    var sizeParams = isDuct
        ? new[] { BuiltInParameter.RBS_CURVE_WIDTH_PARAM, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM,
                  BuiltInParameter.RBS_CURVE_DIAMETER_PARAM }
        : new[] { BuiltInParameter.RBS_PIPE_DIAMETER_PARAM };
    var sizes = new Dictionary<BuiltInParameter, double>();
    foreach (var which in sizeParams)
    {
        var p = run.get_Parameter(which);
        if (p != null && p.HasValue && !p.IsReadOnly) sizes[which] = p.AsDouble();
    }

    if (level == null || systemTypeId == ElementId.InvalidElementId)
    {
        refused.Add(string.Format("{0} has no reference level or no system type for the legs to take, so it "
            + "was not touched.", run.Id));
    }
    else
    {
        Func<ElementId, XYZ, ElementId> breakAt = (id, point) =>
            isDuct ? MechanicalUtils.BreakCurve(doc, id, point) : PlumbingUtils.BreakCurve(doc, id, point);

        Func<Element, Line> lineOf = e =>
        {
            var lc = e == null ? null : e.Location as LocationCurve;
            return lc == null ? null : lc.Curve as Line;
        };

        // Break at the far point first, then at the near point on whichever
        // piece still holds it.
        var firstNew = breakAt(run.Id, b);
        if (firstNew == null || firstNew == ElementId.InvalidElementId)
        {
            refused.Add(string.Format("Revit would not break {0} at {1:0} mm - a run in a group, or owned by "
                + "somebody else. Nothing was changed.", run.Id, d2 * mm));
        }
        else
        {
            var pieces = new List<Element> { doc.GetElement(run.Id), doc.GetElement(firstNew) };
            Element holdsA = null;
            foreach (var piece in pieces)
            {
                var l = lineOf(piece);
                if (l != null && l.Distance(a) < 1.0 / mm) { holdsA = piece; break; }
            }
            var secondNew = holdsA == null ? ElementId.InvalidElementId : breakAt(holdsA.Id, a);
            if (secondNew == null || secondNew == ElementId.InvalidElementId)
            {
                refused.Add(string.Format("{0} was broken at {1:0} mm and Revit would not break it again at "
                    + "{2:0} mm. The first break stays - undo it, or SPLIT_MEP_RUN's halves are joined by "
                    + "CONNECT_OPEN_ENDS.", run.Id, d2 * mm, d1 * mm));
                foreach (var piece in pieces) runs.Add(piece);
            }
            else
            {
                pieces.Add(doc.GetElement(secondNew));

                // Told apart by where each one's middle is.
                Func<XYZ, Element> nearestTo = target =>
                {
                    Element best = null; var bestGap = double.MaxValue;
                    foreach (var piece in pieces)
                    {
                        var l = lineOf(piece);
                        if (l == null) continue;
                        var gap = l.Evaluate(0.5, true).DistanceTo(target);
                        if (gap < bestGap) { bestGap = gap; best = piece; }
                    }
                    return best;
                };
                var startPiece = nearestTo((start + a) / 2.0);
                var middle = nearestTo((a + b) / 2.0);
                var endPiece = nearestTo((b + end) / 2.0);

                // Open the joints at the two points: a leg goes in there.
                Action<Element, Element> open = (one, other) =>
                {
                    var m1 = one as MEPCurve; var m2 = other as MEPCurve;
                    if (m1 == null || m2 == null) return;
                    foreach (Connector c in m1.ConnectorManager.Connectors)
                    {
                        if (!c.IsConnected) continue;
                        var linked = new List<Connector>();
                        foreach (Connector r in c.AllRefs)
                            if (r != null && r.Owner != null && r.Owner.Id == m2.Id) linked.Add(r);
                        foreach (var r in linked) c.DisconnectFrom(r);
                    }
                };
                open(startPiece, middle);
                open(middle, endPiece);

                // Move the stretch: up by the offset, pulled in by the lean at 45.
                var aTop = a + along * lean + up;
                var bTop = b - along * lean + up;
                ((LocationCurve)middle.Location).Curve = Line.CreateBound(aTop, bTop);

                Func<XYZ, XYZ, Element> leg = (from, to) =>
                {
                    Element made = isDuct
                        ? (Element)Duct.Create(doc, systemTypeId, typeId, level.Id, from, to)
                        : Pipe.Create(doc, systemTypeId, typeId, level.Id, from, to);
                    foreach (var size in sizes)
                    {
                        var p = made.get_Parameter(size.Key);
                        if (p != null && !p.IsReadOnly) p.Set(size.Value);
                    }
                    return made;
                };

                var legA = leg(a, aTop);
                var legB = leg(bTop, b);

                runs.Add(startPiece); runs.Add(legA); runs.Add(middle); runs.Add(legB); runs.Add(endPiece);

                findings.Add(string.Format("{0} {1}: the stretch from {2:0} to {3:0} mm along it moved {4} by "
                    + "{5:0} mm, with a leg at each end at {6:0} degrees. Five pieces in `runs`.",
                    isDuct ? "Duct" : "Pipe", run.Id, d1 * mm, d2 * mm, offsetMm > 0 ? "UP" : "DOWN",
                    Math.Abs(offsetMm), angleDegrees));
                findings.Add("NO ELBOW IS MADE HERE. Run FIT_MEP_JOINTS on `runs` - it fits all four or rolls "
                    + "back and names the one that would not fit; a leg shorter than two elbows will not.");
                if (isDuct && sizes.ContainsKey(BuiltInParameter.RBS_CURVE_WIDTH_PARAM))
                    findings.Add("A RECTANGULAR LEG MAY BE TURNED: check the legs' width runs the same way as the "
                        + "duct's in a section before keeping this.");
                findings.Add("Insulation and lining are not copied onto the legs - SET_MEP_INSULATION adds them.");
            }
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
