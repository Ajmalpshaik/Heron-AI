// NOT STANDALONE. Assumes `doc`, `view`, `points`, `lineKind`, `areaPlanName`,
// `traceWalls` and `loopsMm` are in scope; leaves `created`, `tooShort`,
// `refused` and `findings` behind.
//
// VERSION 2 (2026-11-01) - CREATE_AREA_BOUNDARY_LINES MERGED IN. A THIRD
// KIND, `area`: area boundary lines, drawn with NewAreaBoundaryLine in an
// AREA PLAN CHOSEN BY NAME (`areaPlanName`; an area plan only), and two ways
// to give them that card had beside the run of points:
//
//   traceWalls   "inside", "outside" or "centre": a line along every exterior
//                wall on the plan's level (wall type Function = Exterior), at
//                that face - the measuring rule of a gross or net area. An
//                arc wall is named and left: its face is not its centreline
//                moved sideways.
//   loopsMm      closed loops of points in millimetres -
//                "x,y; x,y; x,y | x,y; ..." - each closed automatically; a
//                loop of fewer than three points is named and left out.
//
// THE KIND MUST BE SAID - `lineKind` room, space or area - and is never
// inferred. Version 1 drew room separation lines when nothing was said; that
// rule is the one given up, for the safer one: a room line where a space line
// was meant splits the architect's rooms and leaves the heating load as it
// was, and the two look alike on a plan. Tracing walls and loops are the area
// kind's ways, and refused for the other two rather than ignored.
//
// Each area line is drawn by its own call, one Revit refuses named and the
// rest drawn - both cards' rule - and read back by id with its category, as
// every line here is. The lines lie on the plane of the sketch plane Revit
// makes on the plan's level, this card's rule (that card used the level's
// Elevation, which a survey-datum model puts elsewhere).
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
// space separation line bounds MEP spaces, and neither bounds the other. Since
// version 2 it must be said - lineKind - and nothing is drawn until it is.
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

// VERSION 2: the kind, said - never inferred.
var said = (lineKind ?? "").Trim().ToLowerInvariant();
var asSpace = said == "space" || said == "spaces" || said == "space separation";
var asArea = said == "area" || said == "areas" || said == "area boundary";
var asRoom = said == "room" || said == "rooms" || said == "room separation";
Func<string, string> given = text =>
{
    var t = (text ?? "").Trim();
    return string.Equals(t, "none", StringComparison.OrdinalIgnoreCase) ? "" : t;
};
var planText = given(areaPlanName);
var trace = given(traceWalls).ToLowerInvariant();
if (trace == "center" || trace == "centreline" || trace == "centerline") trace = "centre";
var loopsText = given(loopsMm);

// VERSION 2: an AREA PLAN by name, for area lines - never a floor plan.
ViewPlan areaPlan = null;
if (asArea && planText.Length > 0)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
    {
        var candidate = element as ViewPlan;
        if (candidate != null && !candidate.IsTemplate && candidate.ViewType == ViewType.AreaPlan
            && string.Equals(candidate.Name, planText, StringComparison.OrdinalIgnoreCase)) { areaPlan = candidate; break; }
    }
}
var drawIn = asArea ? (View)areaPlan : view;

var kindWords = asArea ? "area boundary" : asSpace ? "space separation" : "room separation";
var wantedCategory = asArea ? BuiltInCategory.OST_AreaSchemeLines
                   : asSpace ? BuiltInCategory.OST_MEPSpaceSeparationLines
                             : BuiltInCategory.OST_RoomSeparationLines;
var onLevel = drawIn == null ? null : drawIn.GenLevel;
var pointCount = points == null ? 0 : points.Count;

if (!asRoom && !asSpace && !asArea)
{
    refused.Add("say which line - room, space or area. A room separation line bounds rooms, a space "
        + "separation line MEP spaces and an area boundary line areas, and none bounds the others; they "
        + "look alike on a plan, so the kind is never guessed. Nothing was drawn");
}
else if (doc.IsFamilyDocument)
{
    refused.Add("this is a family, and " + kindWords + " lines belong to a project - "
        + "open the project and ask again");
}
else if (!asArea && (planText.Length > 0 || trace.Length > 0 || loopsText.Length > 0))
{
    // VERSION 2. An area plan, traced walls and loops are the AREA kind's;
    // refused for room and space lines rather than silently ignored.
    refused.Add("an area plan, tracing the exterior walls and loops in millimetres are for area boundary "
        + "lines - a " + kindWords + " line is drawn along `points` in a floor plan. Nothing was drawn");
}
else if (asArea && planText.Length == 0)
{
    refused.Add("area boundary lines are drawn in an AREA PLAN - name it (areaPlanName). Nothing was drawn");
}
else if (asArea && areaPlan == null)
{
    refused.Add("No area plan is named \"" + planText + "\". Nothing was drawn");
}
else if (asArea && view != null && view.Id != areaPlan.Id)
{
    refused.Add(string.Format("two views were named - '{0}' and the area plan '{1}'. Area boundary lines go "
        + "in the area plan; leave the view out. Nothing was drawn", view.Name, areaPlan.Name));
}
else if (asArea && trace.Length > 0 && trace != "inside" && trace != "outside" && trace != "centre")
{
    refused.Add("\"" + traceWalls + "\" is not inside, outside or centre. Nothing was drawn");
}
else if (asArea && trace.Length == 0 && loopsText.Length == 0 && pointCount < 2)
{
    refused.Add("neither walls to trace, loops nor a run of points were given. Nothing was drawn");
}
else if (drawIn == null)
{
    refused.Add("a " + kindWords + " line is drawn in a plan view, and no view was given");
}
else if (drawIn.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view template, not a drawing - name the plan the "
        + "lines are to be drawn in", drawIn.Name));
}
else if (onLevel == null)
{
    refused.Add(string.Format("'{0}' is a {1} view and has no level of its own, so there is no "
        + "floor for a {2} line to lie on - name a floor plan", drawIn.Name, drawIn.ViewType, kindWords));
}
else if (!asArea && pointCount < 2)
{
    refused.Add(string.Format("a line needs two points and {0} were given", pointCount));
}
else
{
    var shortest = doc.Application.ShortCurveTolerance;
    var pairsRefused = 0;

    // VERSION 2, AREA LINES' LOOPS, read BEFORE anything is drawn: a point
    // that is not one refuses the call with nothing made. Millimetres, as
    // CREATE_AREA_BOUNDARY_LINES took them, to feet here.
    var loops = new List<List<XYZ>>();
    string loopProblem = null;
    if (asArea && loopsText.Length > 0)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var numberStyle = System.Globalization.NumberStyles.Float;
        foreach (var loop in loopsText.Split('|'))
        {
            var pts = new List<XYZ>();
            foreach (var chunk in loop.Split(';'))
            {
                if (chunk.Trim() == "") continue;
                var parts = chunk.Split(',');
                double x, y;
                if (parts.Length < 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
                    || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y))
                { loopProblem = "\"" + chunk.Trim() + "\" is not a point - two numbers in millimetres. Nothing was drawn"; break; }
                pts.Add(new XYZ(x / MillimetresPerFoot, y / MillimetresPerFoot, 0));
            }
            if (loopProblem != null) break;
            if (pts.Count < 3) { refused.Add("a loop of " + pts.Count + " point(s) encloses nothing - left out"); continue; }
            loops.Add(pts);
        }
    }

    SketchPlane onPlane = null;
    if (loopProblem != null)
    {
        refused.Add(loopProblem);
    }
    else
    {
        try
        {
            onPlane = SketchPlane.Create(doc, onLevel.Id);
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("Revit would not make a sketch plane on level '{0}' - {1}",
                onLevel.Name, ex.Message));
        }
    }

    if (onPlane != null)
    {
        // NOT onLevel.Elevation - see the header.
        var z = onPlane.GetPlane().Origin.Z;

        // EVERY SEGMENT TO DRAW, from wherever it was given: the run of points
        // (consecutive pairs), and for area lines the traced walls and the
        // loops (version 2).
        var segments = new List<Tuple<XYZ, XYZ>>();
        for (var i = 0; i + 1 < pointCount; i++)
        {
            var a = points[i];
            var b = points[i + 1];
            if (a == null || b == null)
            {
                refused.Add(string.Format("point {0} or point {1} was missing", i + 1, i + 2));
                pairsRefused++;
                continue;
            }
            segments.Add(Tuple.Create(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
        }

        if (asArea && trace.Length > 0)
        {
            // CREATE_AREA_BOUNDARY_LINES' TRACE: every exterior wall on the
            // plan's level, at the face asked for - Wall.Orientation points to
            // the exterior side. An arc wall is named and left.
            var traced = 0;
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
            {
                var wall = element as Wall;
                if (wall == null || wall.LevelId != onLevel.Id) continue;
                if (wall.WallType == null || wall.WallType.Function != WallFunction.Exterior) continue;
                var located = wall.Location as LocationCurve;
                var line = located == null ? null : located.Curve as Line;
                if (line == null) { refused.Add("wall " + wall.Id + ": not straight - left for a hand-drawn line"); continue; }
                var shift = trace == "outside" ? wall.Width / 2.0 : trace == "inside" ? -wall.Width / 2.0 : 0.0;
                var move = wall.Orientation.Normalize() * shift;
                var p = line.GetEndPoint(0) + move;
                var q = line.GetEndPoint(1) + move;
                segments.Add(Tuple.Create(new XYZ(p.X, p.Y, z), new XYZ(q.X, q.Y, z)));
                traced++;
            }
            if (traced == 0)
                refused.Add("no straight exterior wall (wall type Function Exterior) is on level '" + onLevel.Name
                    + "' - no wall was traced");
        }

        foreach (var loop in loops)
            for (var i = 0; i < loop.Count; i++)
            {
                var p = loop[i];
                var q = loop[(i + 1) % loop.Count];
                segments.Add(Tuple.Create(new XYZ(p.X, p.Y, z), new XYZ(q.X, q.Y, z)));
            }

        foreach (var segment in segments)
        {
            var start = segment.Item1;
            var end = segment.Item2;

            // A question about the POINTS, not about the model - the caller can
            // fix it, so it is counted apart from a refusal.
            if (start.DistanceTo(end) < shortest) { tooShort++; continue; }

            try
            {
                var made = new List<ModelCurve>();
                if (asArea)
                {
                    // One area boundary line per call, as CREATE_AREA_BOUNDARY_LINES drew them.
                    var one = doc.Create.NewAreaBoundaryLine(onPlane, Line.CreateBound(start, end), areaPlan);
                    if (one != null) made.Add(one);
                }
                else
                {
                    var curves = new CurveArray();
                    curves.Append(Line.CreateBound(start, end));

                    var many = asSpace
                        ? doc.Create.NewSpaceBoundaryLines(onPlane, curves, drawIn)
                        : doc.Create.NewRoomBoundaryLines(onPlane, curves, drawIn);
                    if (many != null) foreach (ModelCurve drawn in many) made.Add(drawn);
                }

                if (made.Count == 0)
                {
                    refused.Add(string.Format("Revit made nothing from ({0:0}, {1:0}) to ({2:0}, {3:0}) mm "
                        + "and gave no reason", start.X * MillimetresPerFoot, start.Y * MillimetresPerFoot,
                        end.X * MillimetresPerFoot, end.Y * MillimetresPerFoot));
                    pairsRefused++;
                    continue;
                }

                foreach (var drawn in made)
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
        created.Count, drawIn.Name, onLevel.Name, totalFeet * MillimetresPerFoot, rightKind, kindWords,
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
