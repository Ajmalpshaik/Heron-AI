// NOT STANDALONE. Assumes `doc`, `view`, `points` and `asSpace` are in scope;
// leaves `created`, `tooShort`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET: the
// add-in converts the millimetres a caller types at the boundary (D-67), so
// there is no bare length in this contract and nothing here converts one.
//
// A RUN OF POINTS, like CREATE_WALL - consecutive points make the lines. Two
// points is the one line across an archway; a run whose last point repeats the
// first is a closed loop. Each line is drawn by its own call, so one pair Revit
// refuses does not lose the lines either side of it.
//
// ROOM OR SPACE IS THE CALLER'S WORD. A room separation line bounds rooms and a
// space separation line bounds MEP spaces, and neither bounds the other. Not
// said means room.
//
// THE LINES LIE ON THE VIEW'S LEVEL. Only the plan position of each point is
// used. The height is read from the sketch plane Revit makes on that level, not
// worked out from an elevation: Elevation and the project height differ on a
// model set out to a survey datum, and the wrong one draws lines no room sees.
//
// READ BACK, NOT ASSUMED. Every line is fetched again by its id and what is
// reported is what came back - its category and its ends - never what was asked.

const double MillimetresPerFoot = 304.8;

var created = new List<Element>();
var tooShort = 0;
var refused = new List<string>();
var findings = new List<string>();

var kindWords = asSpace ? "space separation" : "room separation";
var wantedCategory = asSpace ? BuiltInCategory.OST_MEPSpaceSeparationLines
                             : BuiltInCategory.OST_RoomSeparationLines;
var onLevel = view == null ? null : view.GenLevel;

if (doc.IsFamilyDocument)
{
    refused.Add("this is a family, and " + kindWords + " lines belong to a project - "
        + "open the project and ask again");
}
else if (view == null)
{
    refused.Add("a " + kindWords + " line is drawn in a plan view, and no view was given");
}
else if (view.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view template, not a drawing - name the plan the "
        + "lines are to be drawn in", view.Name));
}
else if (onLevel == null)
{
    refused.Add(string.Format("'{0}' is a {1} view and has no level of its own, so there is no "
        + "floor for a {2} line to lie on - name a floor plan", view.Name, view.ViewType, kindWords));
}
else if (points == null || points.Count < 2)
{
    refused.Add(string.Format("a line needs two points and {0} were given",
        points == null ? 0 : points.Count));
}
else
{
    var shortest = doc.Application.ShortCurveTolerance;
    var pairsRefused = 0;

    SketchPlane onPlane = null;
    try
    {
        onPlane = SketchPlane.Create(doc, onLevel.Id);
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("Revit would not make a sketch plane on level '{0}' - {1}",
            onLevel.Name, ex.Message));
    }

    if (onPlane != null)
    {
        // NOT onLevel.Elevation - see the header.
        var z = onPlane.GetPlane().Origin.Z;

        for (var i = 0; i + 1 < points.Count; i++)
        {
            var a = points[i];
            var b = points[i + 1];

            if (a == null || b == null)
            {
                refused.Add(string.Format("point {0} or point {1} was missing", i + 1, i + 2));
                pairsRefused++;
                continue;
            }

            var start = new XYZ(a.X, a.Y, z);
            var end = new XYZ(b.X, b.Y, z);

            // A question about the POINTS, not about the model - the caller can
            // fix it, so it is counted apart from a refusal.
            if (start.DistanceTo(end) < shortest) { tooShort++; continue; }

            try
            {
                var curves = new CurveArray();
                curves.Append(Line.CreateBound(start, end));

                var made = asSpace
                    ? doc.Create.NewSpaceBoundaryLines(onPlane, curves, view)
                    : doc.Create.NewRoomBoundaryLines(onPlane, curves, view);

                if (made == null || made.Size == 0)
                {
                    refused.Add(string.Format("Revit made nothing from ({0:0}, {1:0}) to ({2:0}, {3:0}) mm "
                        + "and gave no reason", start.X * MillimetresPerFoot, start.Y * MillimetresPerFoot,
                        end.X * MillimetresPerFoot, end.Y * MillimetresPerFoot));
                    pairsRefused++;
                    continue;
                }

                foreach (ModelCurve drawn in made)
                {
                    // Fetched again by id: what the document holds now, not the
                    // object the factory handed back.
                    var held = drawn == null ? null : doc.GetElement(drawn.Id);
                    if (held != null) created.Add(held);
                }
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("line from ({0:0}, {1:0}) to ({2:0}, {3:0}) mm - {4}",
                    start.X * MillimetresPerFoot, start.Y * MillimetresPerFoot,
                    end.X * MillimetresPerFoot, end.Y * MillimetresPerFoot, ex.Message));
                pairsRefused++;
            }
        }
    }

    if (tooShort > 0)
    {
        refused.Add(string.Format("{0} pair(s) of points were closer together than Revit's shortest "
            + "line and were not drawn - the same point typed twice is the usual cause", tooShort));
    }

    // THE READ-BACK. Category and ends of what the document now holds.
    var wantedId = new ElementId(wantedCategory);
    var rightKind = 0;
    var otherKinds = new List<string>();
    var totalFeet = 0.0;
    var ends = new List<string>();

    foreach (var element in created)
    {
        if (element.Category != null && element.Category.Id.Equals(wantedId)) rightKind++;
        else otherKinds.Add(element.Category == null ? "no category" : element.Category.Name);

        var asCurve = element as CurveElement;
        var geometry = asCurve == null ? null : asCurve.GeometryCurve;
        if (geometry == null || !geometry.IsBound) continue;

        totalFeet += geometry.Length;
        var p = geometry.GetEndPoint(0);
        var q = geometry.GetEndPoint(1);
        if (ends.Count < 20)
        {
            ends.Add(string.Format("({0:0}, {1:0}) to ({2:0}, {3:0})",
                p.X * MillimetresPerFoot, p.Y * MillimetresPerFoot,
                q.X * MillimetresPerFoot, q.Y * MillimetresPerFoot));
        }
    }

    findings.Add(string.Format(
        "{0} line(s) drawn in '{1}' on level '{2}', {3:0} mm in all; {4} read back as {5} lines. "
        + "{6} pair(s) of points refused, {7} skipped as too short. No room was placed and nothing "
        + "was joined",
        created.Count, view.Name, onLevel.Name, totalFeet * MillimetresPerFoot, rightKind, kindWords,
        pairsRefused, tooShort));

    if (otherKinds.Count > 0)
    {
        findings.Add(string.Format("{0} line(s) came back filed under another category, not as {1} "
            + "lines: {2}", otherKinds.Count, kindWords, string.Join(", ", otherKinds.Distinct())));
    }

    if (ends.Count > 0)
    {
        findings.Add("ends read back, mm: " + string.Join("; ", ends)
            + (created.Count > ends.Count ? string.Format("; and {0} more", created.Count - ends.Count) : ""));
    }
}
