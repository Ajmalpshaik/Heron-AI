// NOT STANDALONE. Assumes `doc`, `elements`, `radiusMm` and `view` are in
// scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), and it matters more here than
// usual: the delete and the redraw must be ONE undo step, or a failure halfway
// leaves two lines gone and no arc.
//
// THE SOURCE LINES ARE DELETED AND REDRAWN, AND COME BACK WITH NEW IDS. That is
// not tidiness. Shortening a line in place has been seen to do NOTHING when the
// two lines already share an endpoint - which is exactly the corner this
// fragment is for: no exception, a clean commit, and the original geometry
// still there on re-read. Delete and redraw is reliable where the in-place edit
// is not; the cost is that anything holding the old ids is now holding ids that
// do not exist, so the new ones are reported.
//
// DRAFTING LINES ONLY. An MEP corner is rounded by a real elbow fitting whose
// shape comes from the type's routing preferences, not by an arc at a radius
// somebody typed - PLACE_MEP_FITTING does that, and this refuses MEP curves by
// name rather than doing something plausible to them.

const double MillimetresPerFoot = 304.8;

var created = new List<ElementId>();
var findings = new List<string>();

if (elements == null || elements.Count != 2)
{
    findings.Add(string.Format("A fillet rounds the corner between exactly TWO lines - {0} were "
        + "given. Nothing changed", elements == null ? 0 : elements.Count));
}
else if (elements[0] is MEPCurve || elements[1] is MEPCurve)
{
    findings.Add("These are MEP curves, and an MEP corner is rounded by a real elbow fitting whose "
        + "shape comes from the type's routing preferences - there is no radius to choose. "
        + "PLACE_MEP_FITTING does that. Nothing changed");
}
else if (radiusMm <= 0)
{
    findings.Add(string.Format("A fillet needs a radius above zero - {0} mm was given. Nothing "
        + "changed", radiusMm));
}
else
{
    var detail = elements[0] is DetailLine || elements[1] is DetailLine;
    var drafting = (elements[0] is ModelLine || elements[0] is DetailLine)
                && (elements[1] is ModelLine || elements[1] is DetailLine);

    var locationA = elements[0].Location as LocationCurve;
    var locationB = elements[1].Location as LocationCurve;
    var lineA = locationA == null ? null : locationA.Curve as Line;
    var lineB = locationB == null ? null : locationB.Curve as Line;

    if (!drafting)
    {
        findings.Add("Both elements have to be model lines or detail lines. A curved wall or a "
            + "rounded structural member is a different and much heavier operation. Nothing changed");
    }
    else if (lineA == null || lineB == null)
    {
        findings.Add("Both lines have to be straight - an arc has no direction to cross. Nothing "
            + "changed");
    }
    else if (detail && view == null)
    {
        findings.Add("Detail lines live in one view, so the view has to be named to redraw them. "
            + "Nothing changed");
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
            findings.Add("The two lines are parallel in plan, so there is no corner to round. "
                + "Nothing changed");
        }
        else
        {
            var along = ((startB.X - startA.X) * dirB.Y - (startB.Y - startA.Y) * dirB.X) / denominator;
            var cornerPoint = startA + dirA * along;

            // The FAR end of each line is the direction the corner opens towards.
            var farA = lineA.GetEndPoint(0).DistanceTo(cornerPoint) >= lineA.GetEndPoint(1).DistanceTo(cornerPoint)
                ? lineA.GetEndPoint(0) : lineA.GetEndPoint(1);
            var farB = lineB.GetEndPoint(0).DistanceTo(cornerPoint) >= lineB.GetEndPoint(1).DistanceTo(cornerPoint)
                ? lineB.GetEndPoint(0) : lineB.GetEndPoint(1);

            var towardsA = (farA - cornerPoint).Normalize();
            var towardsB = (farB - cornerPoint).Normalize();
            var angle = towardsA.AngleTo(towardsB);

            if (angle < 1e-6 || Math.PI - angle < 1e-6)
            {
                findings.Add("The corner is about 0 or about 180 degrees. There is nothing to round "
                    + "in a straight line or a fold-back, and the arithmetic there gives an enormous "
                    + "arc rather than an error. Nothing changed");
            }
            else
            {
                var radiusFeet = radiusMm / MillimetresPerFoot;
                var tangentLength = radiusFeet / Math.Tan(angle / 2.0);

                if (tangentLength > lineA.Length || tangentLength > lineB.Length)
                {
                    findings.Add(string.Format("A {0:0.#} mm radius at this corner needs {1:0} mm of "
                        + "straight line on each side, and the shorter line is only {2:0} mm. Reduce "
                        + "the radius. Nothing changed",
                        radiusMm,
                        tangentLength * MillimetresPerFoot,
                        Math.Min(lineA.Length, lineB.Length) * MillimetresPerFoot));
                }
                else
                {
                    var tangentA = cornerPoint + towardsA * tangentLength;
                    var tangentB = cornerPoint + towardsB * tangentLength;

                    var centreDistance = radiusFeet / Math.Sin(angle / 2.0);
                    var bisector = (towardsA + towardsB).Normalize();
                    var centre = cornerPoint + bisector * centreDistance;
                    var midDirection = ((tangentA - centre).Normalize() + (tangentB - centre).Normalize()).Normalize();
                    var midPoint = centre + midDirection * radiusFeet;

                    try
                    {
                        var arc = Arc.Create(tangentA, tangentB, midPoint);
                        var shortenedA = Line.CreateBound(tangentA, farA);
                        var shortenedB = Line.CreateBound(tangentB, farB);

                        var wasA = elements[0].Id;
                        var wasB = elements[1].Id;

                        // DELETE THEN REDRAW - see the header. Editing in place
                        // silently keeps the old geometry for this exact shape.
                        doc.Delete(wasA);
                        doc.Delete(wasB);

                        if (detail)
                        {
                            created.Add(doc.Create.NewDetailCurve(view, arc).Id);
                            created.Add(doc.Create.NewDetailCurve(view, shortenedA).Id);
                            created.Add(doc.Create.NewDetailCurve(view, shortenedB).Id);
                        }
                        else
                        {
                            var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, cornerPoint);
                            var sketch = SketchPlane.Create(doc, plane);
                            created.Add(doc.Create.NewModelCurve(arc, sketch).Id);
                            created.Add(doc.Create.NewModelCurve(shortenedA, sketch).Id);
                            created.Add(doc.Create.NewModelCurve(shortenedB, sketch).Id);
                        }

                        findings.Add(string.Format("Corner rounded with a {0:0.#} mm arc. Ids {1} and "
                            + "{2} were DELETED and redrawn - the arc and both shortened lines are "
                            + "new elements, ids {3}",
                            radiusMm, wasA, wasB,
                            string.Join(", ", created.ConvertAll(id => id.ToString()).ToArray())));

                        findings.Add("Anything that referred to the two old ids - a saved set, a tag, "
                            + "a dimension - is referring to elements that no longer exist. Editing "
                            + "them in place instead is not an option: for two lines that already "
                            + "share an endpoint it reports success and changes nothing");
                    }
                    catch (Exception ex)
                    {
                        findings.Add(string.Format("The fillet failed part way: {0}. The caller's "
                            + "transaction must be rolled back - the two lines may already be "
                            + "deleted, and that is not a state to leave a drawing in", ex.Message));
                    }
                }
            }
        }
    }
}
