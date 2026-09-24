// NOT STANDALONE. Assumes `doc`, `elements`, `roofShape` and `pitchDegrees`
// are in scope; leaves `sloped`, `upright`, `differs`, `readBack`,
// `measuredMm`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// ===========================================================================
// SlopeAngle IS A RISE OVER A RUN. IT IS NOT AN ANGLE, WHATEVER ITS NAME SAYS.
// ===========================================================================
//
// FootPrintRoof's per-edge SlopeAngle holds a slope as rise divided by run:
// 0.5 is one unit up for every two across, which is 26.57 degrees. The
// modeller's pitch arrives in DEGREES, so it is turned into that ratio by its
// tangent - tan(30 degrees) is 0.5774 - in exactly one place, below, and the
// ratio is what is written. Handing 30 straight through would ask Revit for a
// rise of 30 on every unit of run: a roof 88 degrees steep, accepted with no
// error and no warning. The API reference describes the value the same way on
// every release from 2020 to 2027, and the name has never changed to match.
//
// So the read-back runs the conversion BACKWARDS - the arctangent of what Revit
// holds - and prints the raw ratio beside it. If the conversion is ever lost,
// the report says 88.09 degrees and a rise of 30, and the roof's measured
// height says the same thing a second way.
//
// WHICH EDGES SLOPE:
//
//   GABLE   the two long sides slope and the two ends stay upright. Only on an
//           outline of four straight sides in two parallel pairs, one pair
//           longer than the other. A side drawn in several collinear pieces is
//           still one side. Anything else - an L (six sides), a square (no LONG
//           pair), a curved edge - is REFUSED: choosing the eaves for the
//           modeller is a design decision, not a default.
//   HIP     every edge slopes, straight or curved.
//
// NOTHING IS WRITTEN TO A ROOF UNTIL ITS WHOLE PLAN IS SETTLED. Every refusal
// Heron makes about a roof is decided before the first of its edges changes,
// so a refused roof is untouched. If REVIT refuses a write part way through,
// this throws: a half-sloped roof is worse than none, and the executor rolls
// the whole call back.
//
// THE REBUILD IS NOT SWALLOWED. Revit's reference for Document.Regenerate says
// that when it fails the model must not be read again and the transaction has
// to be abandoned. So a failure there is thrown straight back with the roof
// named, before anything else is read, and nothing this call did is kept.
//
// EVERY EDGE IS READ BACK after the rebuild, from a fresh read of the
// footprint, and matched to the plan by the sketch line's id - never by its
// place in the list, which nothing here relies on staying the same.

const double MillimetresPerFoot = 304.8;

// THE STEEPEST PITCH THIS WILL SET. At 80 degrees the rise is already 5.7
// times the run, past any ordinary roof. The bound is for a number typed in
// the wrong unit: a percentage (100) or a height (3000) read as degrees is no
// pitch at all, and its tangent is a slope nobody asked for.
const double SteepestPitchDegrees = 80.0;

var sloped = 0;
var upright = 0;
var differs = new List<string>();
var readBack = "";
var measuredMm = "";
var refused = "";
var findings = new List<string>();

var shape = (roofShape ?? "").Trim().ToLowerInvariant();
var risePerRun = 0.0;

if (shape != "gable" && shape != "hip")
{
    refused = string.Format(
        "'{0}' is not a roof shape this makes. Say gable - the two long sides slope "
        + "and the two ends stay upright - or hip - every side slopes. Nothing was changed",
        roofShape);
}
else if (double.IsNaN(pitchDegrees) || double.IsInfinity(pitchDegrees) || pitchDegrees <= 0)
{
    refused = string.Format(
        "A pitch of {0} degrees is not a slope. Give the pitch in degrees, more than 0 - "
        + "a roof with no slope on any edge is the flat roof it already is. Nothing was changed",
        pitchDegrees);
}
else if (pitchDegrees > SteepestPitchDegrees)
{
    refused = string.Format(
        "{0:F2} degrees is steeper than this will set - the limit is {1:F0} degrees, a rise "
        + "of 5.7 times the run. If the number was a percentage or a ratio, convert it first: "
        + "100 percent is 45 degrees and 1 in 2 is 26.57. Nothing was changed",
        pitchDegrees, SteepestPitchDegrees);
}
else if (elements == null || elements.Count == 0)
{
    refused = "Nothing was handed in to slope - select the roof in Revit and ask again. "
        + "Nothing was changed";
}
else
{
    // =======================================================================
    // DEGREES INTO REVIT'S RATIO - THE ONE CONVERSION IN THIS FILE, AND THE
    // REASON THE FRAGMENT EXISTS. SlopeAngle takes RISE OVER RUN (see the
    // header): tan(30 degrees) = 0.5774. Writing 30 here instead would be a
    // rise of 30 per unit of run, a roof 88 degrees steep, and Revit would
    // take it without a word.
    // =======================================================================
    risePerRun = Math.Tan(pitchDegrees * Math.PI / 180.0);

    var degreesPerRadian = 180.0 / Math.PI;

    // How far a read-back may sit from the pitch asked for and still be the
    // pitch asked for. A ratio written and read back should return the same
    // number; anything past a hundredth of a degree is Revit keeping something
    // else, and that goes in `differs`.
    var asAskedDegrees = 0.01;

    // Revit's own tolerances, so "the same point" and "parallel" mean here
    // what they mean to Revit.
    var shortest = doc.Application.ShortCurveTolerance;
    var parallel = Math.Sin(doc.Application.AngleTolerance);

    Action<string> refuse = text =>
    {
        refused += (refused.Length == 0 ? "" : " | ") + text;
    };

    foreach (var element in elements)
    {
        if (element == null) continue;

        var roof = element as FootPrintRoof;

        string name = "";
        try { name = element.Name; } catch { }
        var label = string.Format("{0} {1}{2}",
            roof != null ? "Roof" : (element.Category != null ? element.Category.Name : "Element"),
            element.Id,
            string.IsNullOrEmpty(name) ? "" : " '" + name + "'");

        if (roof == null)
        {
            if (element is ExtrusionRoof)
                refuse(label + " is an extrusion roof - its slope comes from the profile it "
                    + "was drawn with, and it has no footprint edges to slope. Nothing on it was changed");
            else if (element is RoofBase)
                refuse(label + " is a roof made some other way than by footprint, so it has "
                    + "no footprint edges to slope. Nothing on it was changed");
            else
                refuse(label + " is not a roof. Nothing on it was changed");
            continue;
        }

        // Refused BEFORE anything is attempted. Heron does not change one
        // copy of a group from outside the group.
        if (roof.GroupId != null && roof.GroupId != ElementId.InvalidElementId)
        {
            refuse(label + " is inside a group. Heron does not change a group member from "
                + "outside the group - edit the group, or ungroup it, and ask again. Nothing on "
                + "it was changed");
            continue;
        }

        ModelCurveArrArray loops = null;
        try
        {
            loops = roof.GetProfiles();
        }
        catch (Exception unread)
        {
            refuse(label + ": its footprint could not be read - " + unread.Message
                + ". Nothing on it was changed");
            continue;
        }

        if (loops == null || loops.Size == 0)
        {
            refuse(label + " has no footprint lines to slope. Nothing on it was changed");
            continue;
        }

        // ONE OUTLINE ONLY. A second loop is an opening or a second roof area
        // drawn in the same sketch, and which loop is the eaves is not a
        // thing to guess - for a hip as much as for a gable.
        if (loops.Size > 1)
        {
            refuse(string.Format(
                "{0}: its footprint has {1} separate outlines - an opening, or a second roof "
                + "area drawn in the same sketch - and which of them is the eaves is not "
                + "something to guess. Slope this one in Edit Footprint. Nothing on it was changed",
                label, loops.Size));
            continue;
        }

        // get_Item AND NOT [0]. The reference lists an indexer on this array,
        // but C# cannot apply one to it - the compile check refused `loops[0]`
        // on all eight releases on 2026-09-24.
        var edges = new List<ModelCurve>();
        foreach (ModelCurve edge in loops.get_Item(0))
            if (edge != null) edges.Add(edge);

        var count = edges.Count;
        var curves = new Curve[count];
        var lengthMm = new double[count];
        var ids = new ElementId[count];
        var unreadable = count == 0;
        for (var i = 0; i < count; i++)
        {
            ids[i] = edges[i].Id;
            curves[i] = edges[i].GeometryCurve;
            if (curves[i] == null) { unreadable = true; break; }
            lengthMm[i] = curves[i].Length * MillimetresPerFoot;
        }

        if (unreadable)
        {
            refuse(label + ": its footprint lines could not all be read. Nothing on it was changed");
            continue;
        }

        // THE PLAN FOR THIS ROOF: which edges slope, and what each one is.
        var slopes = new bool[count];
        var roles = new string[count];
        string cannot = null;

        if (shape == "hip")
        {
            for (var i = 0; i < count; i++)
            {
                slopes[i] = true;
                roles[i] = "eave";
            }
        }
        else
        {
            // GABLE. Work on the plan: every edge straight, then the SIDES of
            // the outline, then the two parallel pairs, then the longer pair.
            var fromX = new double[count];
            var fromY = new double[count];
            var toX = new double[count];
            var toY = new double[count];
            var alongX = new double[count];
            var alongY = new double[count];
            var planRun = new double[count];

            for (var i = 0; i < count && cannot == null; i++)
            {
                if (!(curves[i] is Line))
                {
                    cannot = "it has a curved edge, and a gable needs four straight sides. "
                        + "Hip slopes a curved edge too";
                    continue;
                }

                var start = curves[i].GetEndPoint(0);
                var end = curves[i].GetEndPoint(1);
                fromX[i] = start.X;
                fromY[i] = start.Y;
                toX[i] = end.X;
                toY[i] = end.Y;
                planRun[i] = Math.Sqrt((end.X - start.X) * (end.X - start.X)
                                     + (end.Y - start.Y) * (end.Y - start.Y));
                if (planRun[i] <= shortest)
                {
                    cannot = "one of its edges has no length on plan";
                    continue;
                }
                alongX[i] = (end.X - start.X) / planRun[i];
                alongY[i] = (end.Y - start.Y) / planRun[i];
            }

            if (cannot == null)
            {
                // SIDES, NOT SKETCH LINES. A side drawn in two pieces is two
                // sketch lines and still one side of the building: pieces that
                // touch end to end and run the same way are joined into one.
                Func<double, double, double, double, bool> near = (x1, y1, x2, y2) =>
                    Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) <= shortest;
                Func<int, int, bool> meet = (one, other) =>
                    near(fromX[one], fromY[one], fromX[other], fromY[other])
                    || near(fromX[one], fromY[one], toX[other], toY[other])
                    || near(toX[one], toY[one], fromX[other], fromY[other])
                    || near(toX[one], toY[one], toX[other], toY[other]);

                var side = new int[count];
                for (var i = 0; i < count; i++) side[i] = i;

                var joined = true;
                while (joined)
                {
                    joined = false;
                    for (var p = 0; p < count; p++)
                    {
                        for (var q = p + 1; q < count; q++)
                        {
                            if (side[p] == side[q]) continue;
                            if (Math.Abs(alongX[p] * alongY[q] - alongY[p] * alongX[q]) > parallel) continue;
                            if (!meet(p, q)) continue;
                            var absorbed = side[q];
                            for (var k = 0; k < count; k++)
                                if (side[k] == absorbed) side[k] = side[p];
                            joined = true;
                        }
                    }
                }

                var sideIds = new List<int>();
                for (var i = 0; i < count; i++)
                    if (!sideIds.Contains(side[i])) sideIds.Add(side[i]);

                if (sideIds.Count != 4)
                {
                    cannot = string.Format(
                        "its outline has {0} sides, so there is no single pair of long eaves - an "
                        + "L-shaped outline has six. Heron does not choose two edges for you: use hip, "
                        + "or tick Defines Slope on the two eave lines in Edit Footprint",
                        sideIds.Count);
                }
                else
                {
                    var sideRun = new double[4];
                    var sideX = new double[4];
                    var sideY = new double[4];
                    for (var s = 0; s < 4; s++)
                    {
                        for (var i = 0; i < count; i++)
                        {
                            if (side[i] != sideIds[s]) continue;
                            sideRun[s] += planRun[i];
                            sideX[s] = alongX[i];
                            sideY[s] = alongY[i];
                        }
                    }

                    // Each side's partner is the ONE other side parallel to it.
                    var partner = new int[4];
                    var twoPairs = true;
                    for (var s = 0; s < 4; s++)
                    {
                        partner[s] = -1;
                        var matching = 0;
                        for (var t = 0; t < 4; t++)
                        {
                            if (t == s) continue;
                            if (Math.Abs(sideX[s] * sideY[t] - sideY[s] * sideX[t]) > parallel) continue;
                            partner[s] = t;
                            matching++;
                        }
                        if (matching != 1) twoPairs = false;
                    }

                    if (!twoPairs)
                    {
                        cannot = "its four sides are not two pairs of parallel sides, so which two "
                            + "are the eaves is not clear. Use hip, or tick Defines Slope on the eave "
                            + "lines in Edit Footprint";
                    }
                    else
                    {
                        var otherSide = partner[0] == 1 ? 2 : 1;
                        var firstPair = (sideRun[0] + sideRun[partner[0]]) / 2.0;
                        var secondPair = (sideRun[otherSide] + sideRun[partner[otherSide]]) / 2.0;

                        if (Math.Abs(firstPair - secondPair) * MillimetresPerFoot <= 1.0)
                        {
                            cannot = string.Format(
                                "all four sides are {0:F0} mm, so neither pair is the long one and "
                                + "which way the ridge runs is a design choice. Tick Defines Slope on "
                                + "the two eave lines in Edit Footprint, or use hip",
                                firstPair * MillimetresPerFoot);
                        }
                        else
                        {
                            var eaveOne = firstPair > secondPair ? 0 : otherSide;
                            var eaveTwo = partner[eaveOne];
                            for (var i = 0; i < count; i++)
                            {
                                var isEave = side[i] == sideIds[eaveOne] || side[i] == sideIds[eaveTwo];
                                slopes[i] = isEave;
                                roles[i] = isEave ? "eave" : "gable end";
                            }
                        }
                    }
                }
            }
        }

        if (cannot != null)
        {
            refuse(label + ": " + cannot + ". Nothing on it was changed");
            continue;
        }

        // WHAT REVIT HOLDS BEFORE, so the report can say what changed.
        var before = new string[count];
        for (var i = 0; i < count; i++)
        {
            before[i] = roof.get_DefinesSlope(edges[i])
                ? string.Format("on at {0:F2} degrees",
                    Math.Atan(roof.get_SlopeAngle(edges[i])) * degreesPerRadian)
                : "off";
        }
        var boxBefore = roof.get_BoundingBox(null);

        // THE WRITE. Defines Slope first, then the ratio - a slope on an edge
        // that does not define one would mean nothing. A gable end only has
        // Defines Slope switched off; its slope value is left as it was.
        for (var i = 0; i < count; i++)
        {
            try
            {
                roof.set_DefinesSlope(edges[i], slopes[i]);
                if (slopes[i]) roof.set_SlopeAngle(edges[i], risePerRun);
            }
            catch (Exception notTaken)
            {
                // ALL OR NOTHING. Some edges of this roof may already have
                // changed, and a half-sloped roof is worse than none, so the
                // call is abandoned and the executor rolls it back.
                throw new InvalidOperationException(string.Format(
                    "{0}: Revit would not take the slope on edge {1} ({2}) - {3}. Nothing this "
                    + "call did is kept", label, i + 1, roles[i], notTaken.Message));
            }
        }

        // THE REBUILD. See the header: a failure here is thrown straight
        // back and nothing is read after it.
        try
        {
            doc.Regenerate();
        }
        catch (Exception notRebuilt)
        {
            throw new InvalidOperationException(string.Format(
                "{0}: Revit could not rebuild the roof with these slopes - {1}. Nothing this "
                + "call did is kept", label, notRebuilt.Message));
        }

        // READ BACK, from a fresh read of the footprint, matched by id.
        var readNow = new bool[count];
        var lines = new string[count];
        var roofSloped = 0;
        var roofUpright = 0;
        var differsBefore = differs.Count;

        ModelCurveArrArray loopsAfter = null;
        try
        {
            loopsAfter = roof.GetProfiles();
        }
        catch (Exception unreadAfter)
        {
            differs.Add(label + ": its footprint could not be read back after the rebuild - "
                + unreadAfter.Message + ". Check it in Revit");
        }

        if (loopsAfter != null)
        {
            foreach (ModelCurveArray loopAfter in loopsAfter)
            {
                foreach (ModelCurve now in loopAfter)
                {
                    if (now == null) continue;

                    var at = -1;
                    for (var i = 0; i < count; i++)
                    {
                        if (!ids[i].Equals(now.Id)) continue;
                        at = i;
                        break;
                    }

                    string holds;
                    bool definesNow;
                    double degreesNow;
                    try
                    {
                        definesNow = roof.get_DefinesSlope(now);
                        var ratioNow = definesNow ? roof.get_SlopeAngle(now) : 0.0;
                        degreesNow = Math.Atan(ratioNow) * degreesPerRadian;
                        holds = definesNow
                            ? string.Format("on at {0:F2} degrees (Revit holds a rise of {1:F4} per 1 of run)",
                                degreesNow, ratioNow)
                            : "off";
                    }
                    catch (Exception unreadEdge)
                    {
                        differs.Add(string.Format("{0} edge {1}: could not be read back - {2}",
                            label, at < 0 ? "?" : (at + 1).ToString(), unreadEdge.Message));
                        continue;
                    }

                    if (at < 0)
                    {
                        differs.Add(string.Format(
                            "{0}: a footprint line that was not there before the rebuild, Defines Slope {1}",
                            label, holds));
                        continue;
                    }

                    readNow[at] = true;
                    lines[at] = string.Format("edge {0}, {1:F0} mm, {2}: Defines Slope {3}, was {4}",
                        at + 1, lengthMm[at], roles[at], holds, before[at]);

                    if (slopes[at])
                    {
                        if (definesNow && Math.Abs(degreesNow - pitchDegrees) <= asAskedDegrees)
                            roofSloped++;
                        else
                            differs.Add(string.Format(
                                "{0} edge {1}: asked for {2:F2} degrees and Revit holds Defines Slope {3}",
                                label, at + 1, pitchDegrees, holds));
                    }
                    else
                    {
                        if (!definesNow)
                            roofUpright++;
                        else
                            differs.Add(string.Format(
                                "{0} edge {1}: asked to stay upright and Revit holds Defines Slope {2}",
                                label, at + 1, holds));
                    }
                }
            }
        }

        var described = new List<string>();
        for (var i = 0; i < count; i++)
        {
            if (readNow[i])
                described.Add(lines[i]);
            else
                differs.Add(string.Format(
                    "{0} edge {1}: not found in the footprint after the rebuild, so it was NOT read back",
                    label, i + 1));
        }

        sloped += roofSloped;
        upright += roofUpright;
        readBack += (readBack.Length == 0 ? "" : " | ") + label + ": " + string.Join("; ", described);

        // WHAT IT MADE, MEASURED. An edge count cannot tell a 30 degree roof
        // from an 88 degree one; the height between its lowest and highest
        // points can. Read off Revit, never worked out from the pitch.
        var boxAfter = roof.get_BoundingBox(null);
        string height;
        if (boxBefore != null && boxAfter != null)
        {
            var level = doc.GetElement(roof.LevelId) as Level;
            height = string.Format(
                "{0}: its highest point is now {1:F0} mm above its lowest, where it was {2:F0} mm "
                + "before - top at {3:F0} mm, bottom at {4:F0} mm{5}",
                label,
                (boxAfter.Max.Z - boxAfter.Min.Z) * MillimetresPerFoot,
                (boxBefore.Max.Z - boxBefore.Min.Z) * MillimetresPerFoot,
                boxAfter.Max.Z * MillimetresPerFoot,
                boxAfter.Min.Z * MillimetresPerFoot,
                level == null ? "" : string.Format(", level '{0}' sits at {1:F0} mm",
                    level.Name, level.ProjectElevation * MillimetresPerFoot));
        }
        else
        {
            height = label + ": Revit returned no bounding box, so its height was NOT measured";
        }
        measuredMm += (measuredMm.Length == 0 ? "" : " | ") + height;

        var roofDiffers = differs.Count - differsBefore;
        findings.Add(roofDiffers == 0
            ? string.Format("{0} is a {1} at {2:F2} degrees, read back from Revit: {3} edge(s) slope{4}. "
                + "See readBack and measuredMm", label, shape, pitchDegrees, roofSloped,
                shape == "gable" ? string.Format(", {0} end edge(s) upright", roofUpright) : "")
            : string.Format("{0}: {1} edge(s) are NOT what was asked - see differs", label, roofDiffers));
    }
}

if (refused.Length > 0)
{
    findings.Add(readBack.Length == 0
        ? "Nothing was changed - see refused for why"
        : "Part of the selection was left alone - see refused for why");
}
