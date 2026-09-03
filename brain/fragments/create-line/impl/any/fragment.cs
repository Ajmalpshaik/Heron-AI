// NOT STANDALONE. Assumes `doc`, `pointPairs`, `asDetail` and `view` are in
// scope; leaves `created`, `tooShort` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// DETAIL OR MODEL IS THE DECISION, NOT A DETAIL. A detail line lives in ONE
// view and prints with it. A model line is a real object that shows in every
// view that sees it. There is no safe default - the caller says which - because
// getting it wrong is invisible until somebody opens another sheet.
//
// A MODEL LINE NEEDS A PLANE, A DETAIL LINE NEEDS A VIEW. Model lines are drawn
// on a horizontal plane through their own height, which is a setting-out line;
// it is not meant for a line leaning through space.
//
// A LINE SHORTER THAN THE APPLICATION'S OWN TOLERANCE CANNOT EXIST. The limit
// is read from the application rather than typed, because it belongs to the
// installation. Those pairs are counted, not thrown - a batch of twenty must
// not die on the one pair somebody typed twice.

var created = new List<Element>();
var tooShort = 0;
var refused = new List<string>();

var shortest = doc.Application.ShortCurveTolerance;

if (asDetail && view == null)
{
    refused.Add("a detail line has to be drawn in a view, and no view was given");
}
else if (asDetail && view.ViewType == ViewType.ThreeD)
{
    refused.Add(string.Format("'{0}' is a 3D view and cannot hold detail lines - they are drawing, "
        + "not model. Draw model lines instead, or pick a plan or a section", view.Name));
}
else
{
    foreach (var pair in pointPairs)
    {
        if (pair == null || pair.Count < 2)
        {
            refused.Add("a point pair had fewer than two points");
            continue;
        }

        var start = pair[0];
        var end = pair[1];

        if (start.DistanceTo(end) < shortest) { tooShort++; continue; }

        try
        {
            var line = Line.CreateBound(start, end);

            if (asDetail)
            {
                var detail = doc.Create.NewDetailCurve(view, line);
                if (detail != null) created.Add(detail);
            }
            else
            {
                // A horizontal plane through the line's own height. Enough for a
                // setting-out or reference line; see the header for what it is not.
                var origin = new XYZ(start.X, start.Y, start.Z);
                var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, origin);
                var sketchPlane = SketchPlane.Create(doc, plane);
                var model = doc.Create.NewModelCurve(line, sketchPlane);
                if (model != null) created.Add(model);
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("line from ({0:0.##}, {1:0.##}, {2:0.##}) - {3}",
                start.X, start.Y, start.Z, ex.Message));
        }
    }

    if (tooShort > 0)
    {
        refused.Add(string.Format("{0} pair(s) were closer together than Revit's shortest line and "
            + "were not drawn - two points typed the same is the usual cause", tooShort));
    }
}
