// NOT STANDALONE. Assumes `doc`, `editScopeFailures` and `elements` are in
// scope; leaves `shaped`, `shapedIds`, `noRoof`, `refused`, `report` and
// `findings` behind.
//
// RUNS WITH NO TRANSACTION OPEN, INSIDE HERON'S GROUP (D-112). A wall's
// profile is edited only inside a SketchEditScope, and Revit's reference says
// one "can only be started when there is no transaction active" (RevitAPI.xml
// 2024, SketchEditScope.Start). So this declares the ambient
// `editScopeFailures` need, opens its own transactions and its own edit scope,
// and hands each of them that need - Revit's words still reach the answer, and
// the group still makes the whole job one undo that rolls back on a preview.
// Lengths are internal FEET.
//
// WHAT IT DOES, WALL BY WALL (row 5b-1672, Q-91, issue #852):
//
//   1. REFUSED BEFORE ANYTHING IS WRITTEN: not a wall; inside a group; a wall
//      that already has an edited profile (Heron does not redraw one somebody
//      drew); and a wall Revit says cannot have a profile sketch - a curved,
//      tapered or stacked wall, an old curtain wall, an infill - with REVIT'S
//      OWN WORDS, asked of Revit in a transaction that is rolled back.
//
//   2. THE UNDERSIDE OF THE ROOF ABOVE IT, MEASURED. Every 50 mm along the
//      wall, on both of its faces (read off the wall, since its location
//      line may be a face rather than its centre), a vertical line from the wall's base up is
//      cut with the roof's solids; where it first enters a roof is the
//      underside there. The lower of the two faces is kept, so the new top is
//      level across the wall's thickness and never pokes through the roof. A
//      station with no roof above it refuses the wall - a top that follows a
//      roof for half its length is not a gable closed.
//
//   3. THE TOP AS A FEW POINTS. Stations on one straight piece of roof are one
//      line; where the roof's slope changes - a ridge, a valley, an eave - the
//      two lines either side are met, so a ridge between two stations is put
//      where it is, not 50 mm short of it.
//
//   4. THE PROFILE. Wall.CreateProfileSketch makes Revit's own rectangle; its
//      bottom and ends are read back after a rebuild (the reference: "the loop
//      of the sketch cannot be obtained until regeneration") and kept. Inside a
//      SketchEditScope the rectangle's lines are deleted and the new outline
//      drawn: the same bottom, the same two ends, and the top through the
//      points of step 3. Every point is put exactly on the sketch's own plane.
//
//   5. READ BACK. The committed sketch is read again: every point of the new
//      top must be a corner of it, to 1 mm. If not - or Revit refused any
//      step - the profile sketch is removed again and the wall is as it was.
//
// THE TOP IS A SKETCH. IT DOES NOT FOLLOW THE ROOF. Revit's Attach Top/Base
// keeps a wall on the roof when the roof moves; a profile does not, and the
// answer says so every time. On Revit 2020 and 2021 there is no profile sketch
// in the API at all, so this card is declared 2022 to 2027 (RevitAPI.xml
// 2022.1.80 to 2024.0.0: Wall.CanHaveProfileSketch, CreateProfileSketch,
// RemoveProfileSketch, SketchId and SketchEditScope; none in 2020.2.90 or
// 2021.1.100). From 2025 ATTACH_WALLS attaches the wall instead, and the
// answer names it.

const double MillimetresPerFoot = 304.8;

var shaped = 0;
var shapedIds = new List<ElementId>();
var noRoof = new List<ElementId>();
var refused = "";
var report = "";
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot).ToString(invariant);
var oneMillimetre = 1.0 / MillimetresPerFoot;
var stationStep = 50.0 / MillimetresPerFoot;
// The new top must stand at least this far above the wall's base.
var lowestTop = 100.0 / MillimetresPerFoot;

Action<string> refuse = text => refused += (refused.Length == 0 ? "" : " | ") + text;
Action<string> tell = text => report += (report.Length == 0 ? "" : " | ") + text;

Action<Transaction> discipline = t =>
{
    var options = t.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(editScopeFailures);
    options.SetClearAfterRollback(true);
    t.SetFailureHandlingOptions(options);
};

// WHAT REVIT SAID, when the add-in's failure discipline recorded it.
Func<string> revitSaid = () =>
{
    var text = editScopeFailures == null ? null : editScopeFailures.ToString();
    return text != null && text.StartsWith("Revit would not let it through", StringComparison.Ordinal)
        ? " - " + text : "";
};

Func<Element, string> labelOf = element =>
{
    string name = "";
    try { name = element.Name; } catch { }
    return string.Format("{0} {1}{2}",
        element is Wall ? "Wall" : element is RoofBase ? "Roof"
            : element.Category != null ? element.Category.Name : "Element",
        element.Id, string.IsNullOrEmpty(name) ? "" : " '" + name + "'");
};

// ---- WHAT WAS HANDED IN: walls to shape, and the roofs to follow ---------
var walls = new List<Wall>();
var chosenRoofs = new List<RoofBase>();
if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (element is Wall) walls.Add((Wall)element);
        else if (element is RoofBase) chosenRoofs.Add((RoofBase)element);
        else refuse(labelOf(element) + " is not a wall or a roof. Nothing on it was changed");
    }
}

// A ROOF IN THE SELECTION IS THE ONE TO FOLLOW; with none, every roof in the
// model is looked at, and each wall finds the one above it.
var roofs = chosenRoofs.Count > 0
    ? chosenRoofs
    : new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs)
        .WhereElementIsNotElementType().OfType<RoofBase>().ToList();

// Each roof's solids, with its plan extent so a station far from it is not cut.
var roofSolids = new List<Tuple<RoofBase, List<Solid>, BoundingBoxXYZ>>();
var roofTop = double.MinValue;
foreach (var roof in roofs)
{
    BoundingBoxXYZ box = null;
    try { box = roof.get_BoundingBox(null); } catch { box = null; }
    if (box == null) continue;
    var solids = new List<Solid>();
    GeometryElement geometry = null;
    try { geometry = roof.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine }); } catch { geometry = null; }
    if (geometry == null) continue;
    foreach (var item in geometry)
    {
        var solid = item as Solid;
        if (solid != null && solid.Volume > 0) { solids.Add(solid); continue; }
        var instance = item as GeometryInstance;
        if (instance == null) continue;
        foreach (var inner in instance.GetInstanceGeometry())
        {
            var innerSolid = inner as Solid;
            if (innerSolid != null && innerSolid.Volume > 0) solids.Add(innerSolid);
        }
    }
    if (solids.Count == 0) continue;
    roofSolids.Add(Tuple.Create(roof, solids, box));
    roofTop = Math.Max(roofTop, box.Max.Z);
}

// THE UNDERSIDE AT ONE POINT ON PLAN: where a vertical line from `fromZ` up
// first enters a roof. NaN when no roof is above. `hitRoof` says which.
ElementId hitRoof = ElementId.InvalidElementId;
Func<double, double, double, double> undersideAt = (x, y, fromZ) =>
{
    hitRoof = ElementId.InvalidElementId;
    var lowest = double.NaN;
    if (roofTop <= fromZ) return lowest;
    Line upward;
    try { upward = Line.CreateBound(new XYZ(x, y, fromZ), new XYZ(x, y, roofTop + 1.0)); }
    catch { return lowest; }
    foreach (var entry in roofSolids)
    {
        var box = entry.Item3;
        if (x < box.Min.X || x > box.Max.X || y < box.Min.Y || y > box.Max.Y) continue;
        foreach (var solid in entry.Item2)
        {
            SolidCurveIntersection cut = null;
            try { cut = solid.IntersectWithCurve(upward, new SolidCurveIntersectionOptions()); } catch { cut = null; }
            if (cut == null) continue;
            for (var i = 0; i < cut.SegmentCount; i++)
            {
                var piece = cut.GetCurveSegment(i);
                var z = Math.Min(piece.GetEndPoint(0).Z, piece.GetEndPoint(1).Z);
                if (double.IsNaN(lowest) || z < lowest)
                {
                    lowest = z;
                    hitRoof = entry.Item1.Id;
                }
            }
        }
    }
    return lowest;
};

if (walls.Count == 0)
{
    refuse("No wall was handed in to shape - select the walls under the roof in Revit (and the "
        + "roof, to name which one) and ask again. Nothing was changed");
}
else if (roofSolids.Count == 0)
{
    refuse(chosenRoofs.Count > 0
        ? "The roof selected has no solid to follow. Nothing was changed"
        : "There is no roof in this model for a wall's top to follow. Nothing was changed");
}
else
{
    foreach (var wall in walls)
    {
        var label = labelOf(wall);

        // ---- 1. REFUSED BEFORE ANYTHING IS WRITTEN ----------------------
        if (wall.GroupId != null && wall.GroupId != ElementId.InvalidElementId)
        {
            refuse(label + " is inside a group. Heron does not change a group member from outside "
                + "the group - edit the group, or ungroup it, and ask again. Nothing on it was changed");
            continue;
        }

        if (wall.SketchId != null && wall.SketchId != ElementId.InvalidElementId)
        {
            refuse(label + " already has an edited profile, and Heron does not redraw a profile "
                + "somebody drew. Reset Profile on it in Revit first if it should follow the roof. "
                + "Nothing on it was changed");
            continue;
        }

        if (!wall.CanHaveProfileSketch())
        {
            // REVIT'S OWN WORDS: asked of Revit, in a transaction rolled back.
            string why = null;
            using (var probe = new Transaction(doc, "Heron: ask Revit about the wall's profile"))
            {
                probe.Start();
                discipline(probe);
                try { wall.CreateProfileSketch(); }
                catch (Exception notAllowed) { why = notAllowed.Message; }
                probe.RollBack();
            }
            refuse(label + " cannot have its profile edited - Revit says: \""
                + (why ?? "this wall does not support a profile sketch") + "\". Nothing on it was changed");
            continue;
        }

        var located = wall.Location as LocationCurve;
        var line = located == null ? null : located.Curve as Line;
        if (line == null)
        {
            refuse(label + " has no straight location line to measure along. Nothing on it was changed");
            continue;
        }

        BoundingBoxXYZ wallBox = null;
        try { wallBox = wall.get_BoundingBox(null); } catch { wallBox = null; }
        if (wallBox == null)
        {
            refuse(label + ": Revit returned no extent for it, so its base is not known. Nothing on it "
                + "was changed");
            continue;
        }

        var start = line.GetEndPoint(0);
        var end = line.GetEndPoint(1);
        var along = new XYZ(end.X - start.X, end.Y - start.Y, 0);
        var planLength = along.GetLength();
        if (planLength <= oneMillimetre)
        {
            refuse(label + " has no length on plan. Nothing on it was changed");
            continue;
        }
        along = along.Normalize();
        var across = new XYZ(-along.Y, along.X, 0);

        // WHERE THE WALL'S TWO FACES ARE, across its location line. The
        // location line may be a finish face rather than the centre, so the
        // faces are read off the wall; only when Revit gives no side face is
        // half the width either side of the line used.
        var faceAt = new List<double>();
        foreach (var side in new[] { ShellLayerType.Exterior, ShellLayerType.Interior })
        {
            try
            {
                foreach (var reference in HostObjectUtils.GetSideFaces(wall, side))
                {
                    var face = wall.GetGeometryObjectFromReference(reference) as PlanarFace;
                    if (face == null) continue;
                    faceAt.Add((face.Origin - start).DotProduct(across));
                    break;
                }
            }
            catch { }
        }
        double acrossOne, acrossTwo;
        if (faceAt.Count == 2 && Math.Abs(faceAt[0] - faceAt[1]) > oneMillimetre)
        {
            // A hair inside each face, so a station is cut with the roof above
            // the wall and not with the roof's edge beside it.
            var middle = (faceAt[0] + faceAt[1]) / 2.0;
            acrossOne = middle + (faceAt[0] - middle) * 0.999;
            acrossTwo = middle + (faceAt[1] - middle) * 0.999;
        }
        else
        {
            acrossOne = wall.Width / 2.0 * 0.999;
            acrossTwo = -acrossOne;
        }

        // THE TOP AT EVERY STATION between two distances along the wall,
        // lower face kept. `missing` is the first station with no roof.
        var roofsUsed = new List<ElementId>();
        Func<double, double, double, List<double[]>> measure = (fromU, toU, baseZ) =>
        {
            var stations = new List<double[]>();
            var count = Math.Max(2, (int)Math.Ceiling((toU - fromU) / stationStep) + 1);
            for (var k = 0; k < count; k++)
            {
                var u = fromU + (toU - fromU) * k / (count - 1);
                // Half a millimetre inside each end, so the end station is
                // cut with the roof and not with its edge.
                var inside = Math.Min(Math.Max(u, fromU + oneMillimetre / 2), toU - oneMillimetre / 2);
                var x = start.X + along.X * inside;
                var y = start.Y + along.Y * inside;
                var zOne = undersideAt(x + across.X * acrossOne, y + across.Y * acrossOne, baseZ + oneMillimetre);
                var roofOne = hitRoof;
                var zTwo = undersideAt(x + across.X * acrossTwo, y + across.Y * acrossTwo, baseZ + oneMillimetre);
                var roofTwo = hitRoof;
                if (double.IsNaN(zOne) || double.IsNaN(zTwo))
                {
                    stations.Add(new[] { u, double.NaN });
                    continue;
                }
                var lower = Math.Min(zOne, zTwo);
                var holder = zOne <= zTwo ? roofOne : roofTwo;
                if (!roofsUsed.Contains(holder)) roofsUsed.Add(holder);
                stations.Add(new[] { u, lower });
            }
            return stations;
        };

        Func<List<double[]>, string> gapIn = stations =>
        {
            foreach (var station in stations)
                if (double.IsNaN(station[1]))
                    return string.Format("there is no roof above it {0} mm from its start, so its top "
                        + "cannot follow a roof along its whole length", mm(station[0]));
            return null;
        };

        // THE TOP AS A FEW POINTS (step 3): stations on one straight piece of
        // roof become one line, and a ridge or valley between two stations is
        // put where the lines either side of it meet.
        Func<List<double[]>, List<double[]>> simplify = stations =>
        {
            var keep = new bool[stations.Count];
            keep[0] = true;
            keep[stations.Count - 1] = true;
            var spans = new Stack<int[]>();
            spans.Push(new[] { 0, stations.Count - 1 });
            while (spans.Count > 0)
            {
                var span = spans.Pop();
                var a = stations[span[0]];
                var b = stations[span[1]];
                var worst = -1;
                var worstOff = oneMillimetre;
                for (var k = span[0] + 1; k < span[1]; k++)
                {
                    var t = (stations[k][0] - a[0]) / (b[0] - a[0]);
                    var off = Math.Abs(stations[k][1] - (a[1] + (b[1] - a[1]) * t));
                    if (off > worstOff) { worstOff = off; worst = k; }
                }
                if (worst < 0) continue;
                keep[worst] = true;
                spans.Push(new[] { span[0], worst });
                spans.Push(new[] { worst, span[1] });
            }

            var kept = new List<int>();
            for (var k = 0; k < stations.Count; k++) if (keep[k]) kept.Add(k);

            var points = new List<double[]>();
            for (var j = 0; j < kept.Count; j++)
            {
                var k = kept[j];
                if (j == 0 || j == kept.Count - 1 || k - 1 < 0 || k + 1 >= stations.Count)
                {
                    points.Add(stations[k]);
                    continue;
                }
                // The line to the left through two stations wholly on it, and
                // the line to the right the same; where they meet is the kink.
                var leftFar = kept[j - 1] + 1 < k - 1 ? kept[j - 1] + 1 : kept[j - 1];
                var leftNear = k - 1;
                var rightNear = k + 1;
                var rightFar = kept[j + 1] - 1 > k + 1 ? kept[j + 1] - 1 : kept[j + 1];
                var l1 = stations[leftFar]; var l2 = stations[leftNear];
                var r1 = stations[rightNear]; var r2 = stations[rightFar];
                double[] meet = null;
                if (Math.Abs(l2[0] - l1[0]) > 1e-9 && Math.Abs(r2[0] - r1[0]) > 1e-9)
                {
                    var leftSlope = (l2[1] - l1[1]) / (l2[0] - l1[0]);
                    var rightSlope = (r2[1] - r1[1]) / (r2[0] - r1[0]);
                    if (Math.Abs(leftSlope - rightSlope) > 1e-9)
                    {
                        var u = (r1[1] - l1[1] + leftSlope * l1[0] - rightSlope * r1[0]) / (leftSlope - rightSlope);
                        if (u >= stations[k - 1][0] && u <= stations[k + 1][0])
                            meet = new[] { u, l1[1] + leftSlope * (u - l1[0]) };
                    }
                }
                points.Add(meet ?? stations[k]);
            }
            return points;
        };

        // A first look along the location line, read only, so a wall the roof
        // does not cover is refused before any sketch is made.
        var firstLook = measure(0.0, planLength, wallBox.Min.Z);
        var gap = gapIn(firstLook);
        if (gap != null)
        {
            noRoof.Add(wall.Id);
            refuse(label + ": " + gap + ". Nothing on it was changed");
            continue;
        }

        // ---- 4. THE PROFILE ---------------------------------------------
        string failed = null;
        var sketchMade = false;
        ElementId sketchId = ElementId.InvalidElementId;

        using (var make = new Transaction(doc, "Heron: give the wall a profile sketch"))
        {
            make.Start();
            discipline(make);
            try
            {
                wall.CreateProfileSketch();
                doc.Regenerate();
            }
            catch (Exception notMade)
            {
                failed = "Revit would not give it a profile sketch - " + notMade.Message;
            }
            if (failed != null) make.RollBack();
            else if (make.Commit() != TransactionStatus.Committed)
                failed = "Revit would not keep a profile sketch on it" + revitSaid();
            else
            {
                sketchMade = true;
                sketchId = wall.SketchId;
            }
        }

        Sketch sketch = null;
        Plane plane = null;
        if (failed == null)
        {
            sketch = doc.GetElement(sketchId) as Sketch;
            if (sketch == null || sketch.SketchPlane == null)
                failed = "Revit made no profile sketch that can be read";
            else
                plane = sketch.SketchPlane.GetPlane();
        }

        // On the sketch's own plane, so every point drawn lies in it.
        Func<XYZ, XYZ> onPlane = point =>
        {
            var off = (point - plane.Origin).DotProduct(plane.Normal);
            return point - plane.Normal.Multiply(off);
        };
        var origin = plane == null ? start : onPlane(new XYZ(start.X, start.Y, 0));
        Func<double, double, XYZ> pointAt = (u, z) =>
            onPlane(new XYZ(origin.X + along.X * u, origin.Y + along.Y * u, z));
        Func<XYZ, double> uOf = point =>
            (point.X - origin.X) * along.X + (point.Y - origin.Y) * along.Y;

        // REVIT'S RECTANGLE: its bottom and its two ends are kept.
        var uLow = double.MaxValue; var uHigh = double.MinValue; var zBottom = double.MaxValue;
        var zWasTop = double.MinValue;
        if (failed == null)
        {
            foreach (CurveArray loop in sketch.Profile)
                foreach (Curve curve in loop)
                    for (var e = 0; e < 2; e++)
                    {
                        var point = curve.GetEndPoint(e);
                        uLow = Math.Min(uLow, uOf(point));
                        uHigh = Math.Max(uHigh, uOf(point));
                        zBottom = Math.Min(zBottom, point.Z);
                        zWasTop = Math.Max(zWasTop, point.Z);
                    }
            if (uHigh - uLow <= oneMillimetre)
                failed = "Revit's profile sketch for it has no length";
        }

        List<double[]> top = null;
        if (failed == null)
        {
            roofsUsed.Clear();
            var stations = measure(uLow, uHigh, zBottom);
            var gapNow = gapIn(stations);
            if (gapNow != null) failed = gapNow;
            else
            {
                top = simplify(stations);
                foreach (var point in top)
                    if (point[1] < zBottom + lowestTop)
                    {
                        failed = string.Format("the roof comes down to {0} mm above the wall's base, {1} mm "
                            + "from its start - lower than a wall top this will draw", mm(point[1] - zBottom),
                            mm(point[0] - uLow));
                        break;
                    }
            }
        }

        if (failed == null)
        {
            using (var scope = new SketchEditScope(doc, "Heron: shape the wall top to the roof"))
            {
                if (!scope.IsPermitted)
                {
                    failed = "Revit will not open the wall's profile for editing at this moment - another "
                        + "edit mode is active, or a transaction is open around this fragment (D-112)";
                }
                else
                {
                    try
                    {
                        scope.Start(sketchId);
                        using (var draw = new Transaction(doc, "Heron: draw the wall profile"))
                        {
                            draw.Start();
                            discipline(draw);
                            try
                            {
                                var editing = (Sketch)doc.GetElement(sketchId);
                                var lines = editing.GetAllElements()
                                    .Where(id => doc.GetElement(id) is CurveElement).ToList();
                                doc.Delete(lines);

                                // The bottom, the far end up, the top back
                                // through the roof's points, the near end down.
                                var outline = new List<XYZ>();
                                outline.Add(pointAt(uLow, zBottom));
                                outline.Add(pointAt(uHigh, zBottom));
                                for (var k = top.Count - 1; k >= 0; k--)
                                {
                                    var u = k == top.Count - 1 ? uHigh : k == 0 ? uLow : top[k][0];
                                    outline.Add(pointAt(u, top[k][1]));
                                }
                                for (var k = 0; k < outline.Count; k++)
                                {
                                    var from = outline[k];
                                    var to = outline[(k + 1) % outline.Count];
                                    if (from.DistanceTo(to) <= oneMillimetre) continue;
                                    doc.Create.NewModelCurve(Line.CreateBound(from, to), editing.SketchPlane);
                                }
                            }
                            catch (Exception notDrawn)
                            {
                                failed = "Revit refused the new profile - " + notDrawn.Message;
                            }
                            if (failed != null) draw.RollBack();
                            else if (draw.Commit() != TransactionStatus.Committed)
                                failed = "Revit would not keep the new profile" + revitSaid();
                        }

                        if (failed == null) scope.Commit(editScopeFailures);
                    }
                    catch (Exception notEdited)
                    {
                        if (failed == null) failed = "Revit would not finish the profile - " + notEdited.Message
                            + revitSaid();
                    }
                    if (scope.IsActive) scope.Cancel();
                }
            }
        }

        // ---- 5. READ BACK -----------------------------------------------
        var corners = new List<double[]>();
        if (failed == null)
        {
            var after = doc.GetElement(wall.SketchId) as Sketch;
            if (after == null) failed = "the profile could not be read back after Revit closed it";
            else
            {
                foreach (CurveArray loop in after.Profile)
                    foreach (Curve curve in loop)
                        for (var e = 0; e < 2; e++)
                        {
                            var point = curve.GetEndPoint(e);
                            corners.Add(new[] { uOf(point), point.Z });
                        }
                for (var k = 0; k < top.Count && failed == null; k++)
                {
                    var u = k == top.Count - 1 ? uHigh : k == 0 ? uLow : top[k][0];
                    var found = false;
                    foreach (var corner in corners)
                        if (Math.Abs(corner[0] - u) <= oneMillimetre && Math.Abs(corner[1] - top[k][1]) <= oneMillimetre)
                        { found = true; break; }
                    if (!found)
                        failed = string.Format("read back, the profile has no corner at {0} mm along, {1} mm up, "
                            + "where the roof's underside is", mm(u - uLow), mm(top[k][1]));
                }
            }
        }

        if (failed != null)
        {
            // THE WALL AS IT WAS: a profile sketch this call made is removed.
            if (sketchMade)
            {
                using (var undo = new Transaction(doc, "Heron: remove the wall profile again"))
                {
                    undo.Start();
                    discipline(undo);
                    try
                    {
                        if (wall.IsValidObject && wall.SketchId != ElementId.InvalidElementId)
                            wall.RemoveProfileSketch();
                    }
                    catch (Exception notRemoved)
                    {
                        failed += ". ITS PROFILE SKETCH COULD NOT BE REMOVED AGAIN - " + notRemoved.Message
                            + "; Discard the change";
                    }
                    if (undo.Commit() != TransactionStatus.Committed)
                        failed += ". Revit would not remove its profile sketch again" + revitSaid()
                            + "; Discard the change";
                }
            }
            refuse(label + ": " + failed + ". Nothing on it was kept");
            continue;
        }

        shaped++;
        shapedIds.Add(wall.Id);
        var highest = double.MinValue;
        var described = new List<string>();
        for (var k = 0; k < top.Count; k++)
        {
            var u = k == top.Count - 1 ? uHigh : k == 0 ? uLow : top[k][0];
            highest = Math.Max(highest, top[k][1]);
            described.Add(string.Format("{0} mm along at {1} mm", mm(u - uLow), mm(top[k][1])));
        }
        tell(string.Format("{0}: its top now follows the underside of {1} through {2} point(s) - {3}; "
            + "highest {4} mm, where its top was {5} mm. Read back from the profile Revit holds",
            label,
            string.Join(", ", roofsUsed.Select(id => "roof " + id).ToArray()),
            top.Count, string.Join("; ", described), mm(highest), mm(zWasTop)));
    }
}

if (shaped > 0)
{
    int release;
    int.TryParse(doc.Application.VersionNumber, out release);
    findings.Add(string.Format("{0} wall(s) have a profile whose top follows the roof above them, read "
        + "back from Revit - see report", shaped));
    findings.Add("THE TOP IS A SKETCH: it will NOT follow the roof if the roof moves or changes. Run this "
        + "again after Reset Profile, or Edit Profile by hand"
        + (release >= 2025 ? ". On this Revit, ATTACH_WALLS attaches the walls instead, so they follow it" : ""));
}
if (refused.Length > 0)
    findings.Add(shaped == 0
        ? "Nothing was changed - see refused for why"
        : "Part of the selection was left alone - see refused for why");
