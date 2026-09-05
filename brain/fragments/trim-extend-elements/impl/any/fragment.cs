// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `corner`,
// `moved` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The earlier version of this
// opened its own, which turns a batch into several undo steps.
//
// WRITING THE CURVE IS NOT PROOF IT TOOK. Reassigning a location curve in place
// has been seen to do NOTHING for two drafting lines that already shared an
// endpoint - no exception, the transaction commits clean, and re-reading the
// element gives back the original geometry. So both elements are RE-READ
// afterwards and the report says where each end actually landed. A move that
// silently did not happen becomes a stated failure instead of a success line.
//
// THE CORNER IS SOLVED IN PLAN. Two runs at the same height square off; two
// skew in 3D have no single corner. Each element keeps its own Z.
//
// PARALLEL IS REFUSED. There is no corner, and inventing one is worse than
// saying so.

var corner = XYZ.Zero;
var moved = 0;
var findings = new List<string>();

if (elements == null || elements.Count != 2)
{
    findings.Add(string.Format("This squares off exactly TWO elements - {0} were given. Nothing "
        + "changed", elements == null ? 0 : elements.Count));
}
else
{
    var locationA = elements[0] == null ? null : elements[0].Location as LocationCurve;
    var locationB = elements[1] == null ? null : elements[1].Location as LocationCurve;
    var lineA = locationA == null ? null : locationA.Curve as Line;
    var lineB = locationB == null ? null : locationB.Curve as Line;

    if (lineA == null || lineB == null)
    {
        findings.Add("Both elements need a straight line location. An arc, or an element placed at a "
            + "point rather than along a line, has no direction to cross. Nothing changed");
    }
    else
    {
        var startA = lineA.GetEndPoint(0);
        var startB = lineB.GetEndPoint(0);
        var dirA = lineA.Direction;
        var dirB = lineB.Direction;

        var denominator = dirA.X * dirB.Y - dirA.Y * dirB.X;

        if (Math.Abs(denominator) < 1e-9)
        {
            findings.Add("The two are parallel in plan, so there is no corner to meet at. Nothing "
                + "changed - a corner invented here would be somewhere neither of them goes");
        }
        else
        {
            var along = ((startB.X - startA.X) * dirB.Y - (startB.Y - startA.Y) * dirB.X) / denominator;
            corner = startA + dirA * along;

            // The nearer end moves to the corner; the far end is left exactly
            // where it was, which is what makes this trim AND extend at once.
            Func<Line, Line> squaredOff = line =>
            {
                var first = line.GetEndPoint(0);
                var second = line.GetEndPoint(1);
                return first.DistanceTo(corner) <= second.DistanceTo(corner)
                    ? Line.CreateBound(new XYZ(corner.X, corner.Y, first.Z), second)
                    : Line.CreateBound(first, new XYZ(corner.X, corner.Y, second.Z));
            };

            var wantedA = squaredOff(lineA);
            var wantedB = squaredOff(lineB);

            var failures = new List<string>();

            try { locationA.Curve = wantedA; }
            catch (Exception ex) { failures.Add(string.Format("id {0}: {1}", elements[0].Id, ex.Message)); }

            try { locationB.Curve = wantedB; }
            catch (Exception ex) { failures.Add(string.Format("id {0}: {1}", elements[1].Id, ex.Message)); }

            // READ IT BACK. This is the whole point - see the header.
            Func<Element, Line, bool> landedWhereAsked = (element, wanted) =>
            {
                var fresh = doc.GetElement(element.Id);
                if (fresh == null) return false;
                var freshLine = fresh.Location as LocationCurve;
                var actual = freshLine == null ? null : freshLine.Curve as Line;
                if (actual == null) return false;

                return actual.GetEndPoint(0).DistanceTo(wanted.GetEndPoint(0)) < 1e-6
                    && actual.GetEndPoint(1).DistanceTo(wanted.GetEndPoint(1)) < 1e-6;
            };

            var tookA = landedWhereAsked(elements[0], wantedA);
            var tookB = landedWhereAsked(elements[1], wantedB);

            if (tookA) moved++;
            if (tookB) moved++;

            findings.Add(string.Format("Corner solved at ({0:0.###}, {1:0.###}) in feet. {2} of 2 "
                + "element(s) are really there when re-read",
                corner.X, corner.Y, moved));

            if (!tookA || !tookB)
                findings.Add(string.Format("{0} did NOT move, and the write raised nothing. Two "
                    + "drafting lines that already shared an endpoint are the known case: Revit "
                    + "accepts the new curve and keeps the old geometry. Delete and recreate those "
                    + "two rather than editing them in place",
                    !tookA && !tookB
                        ? string.Format("Neither id {0} nor id {1}", elements[0].Id, elements[1].Id)
                        : string.Format("Id {0}", (!tookA ? elements[0] : elements[1]).Id)));

            foreach (var failure in failures) findings.Add("Refused: " + failure);

            if (moved > 0)
                findings.Add("Any MEP connection at a moved end is now broken - Revit does not put it "
                    + "back. PLACE_MEP_FITTING joins the new corner with a real elbow");
        }
    }
}
