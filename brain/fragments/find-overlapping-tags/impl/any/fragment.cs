// NOT STANDALONE. Assumes `doc`, `view`, `minOverlapMm` and `annotationOnly`
// are in scope; leaves `findings`, `overlapping`, `pairs`, `overModel` and
// `noBox` behind.
//
// VERSION 2 (2026-11-16) - TAGS OVER THE DRAWING TOO (row 5b-1731, issue #854
// item b, villa 2 on Revit 2024). Version 1 compared annotation with
// annotation only and its answer did not say so: "0 clash(es)" on a plan where
// nearly every door and window tag sat on the cut wall. Widened here rather
// than in a card of its own - it is the same question, "what is printing on
// top of what". Each TAG's paper box is now also compared with:
//
//   WALLS, IN A PLAN. The poche is a band the wall's width wide about its
//   centre line, worked out from the location line Revit keeps (a straight
//   wall's location-line offset is taken off; a curved one is given a band
//   wide enough for any location line, and the answer says so). Only in a
//   plan, where the cut is the plan's own - in a section a wall's location
//   line is not where its cut is drawn, so walls are not compared there and
//   the answer says that too. A tag's OWN element is never its clash: a wall
//   tag sits on its wall.
//   LINES - model and detail lines (category Lines) the view shows: a tag
//   box a line crosses.
//   DIMENSIONS - the point each dimension's figure is placed at, inside a tag
//   box. Never the dimension's own box: that is the whole string, witness
//   lines and all, which is why version 1 left dimensions out.
//
// `minOverlapMm` still judges annotation against annotation. Against the
// drawing a tag counts when it touches at all - a box on the poche is the
// complaint, however little. `annotationOnly` true is version 1 exactly.
// The first finding names what was compared, every time.
//
// READ ONLY. Opens no transaction and needs none.
//
// ===========================================================================
// THE UNIT IS PAPER MILLIMETRES, AND IT IS THE ONLY UNIT THIS QUESTION HAS.
// ===========================================================================
//
// Two tags are clear at 1:50 and merged at 1:200. So every figure is model feet
// divided by the view's own scale - what comes out of the plotter, not what the
// model happens to look like.
//
// THE BOXES ARE PROJECTED ONTO THE VIEW'S OWN PAPER AXES. Comparing model X
// against model Y is right in a plan and wrong in every section: a view looking
// along X has all its annotation at one X, so every box overlaps every other
// and the report is worthless exactly where a section is busiest. Each box's
// eight corners are projected onto the view's right and up directions, which is
// the same test in a plan and the correct one everywhere else.
//
// A BOUNDING BOX IS BIGGER THAN THE INK, so this over-reports slightly. Two
// boxes can overlap while the characters do not touch. The opposite error costs
// a reissued drawing; this one costs thirty seconds of looking.
//
// DIMENSIONS ARE NOT COLLECTED. A dimension's box is the whole string, witness
// lines and all, so it appears to overlap nearly everything inside its own
// extent. Included, it produces hundreds of rows that are all the same
// non-finding.
//
// ROOM, SPACE AND AREA TAGS ARE COLLECTED BY CATEGORY, not by class. Which of
// them is an IndependentTag subclass has moved between releases; a category
// sweep is the same on all of them.

var findings = new List<string>();
var overlapping = new List<ElementId>();
var pairs = 0;
var noBox = 0;
// VERSION 2 (row 5b-1731): tag-over-drawing clashes - walls, lines, dimensions.
var overModel = 0;

var mmPerFoot = 304.8;

if (view.IsTemplate)
{
    findings.Add("That is a view template - it holds no annotation of its own");
}
else if (view.Scale <= 0)
{
    // A perspective or an unscaled view has no paper size, so "how many mm
    // apart on the sheet" has no answer. Saying so is the answer.
    findings.Add(string.Format("View '{0}' has no print scale, so nothing here can be measured in paper "
        + "millimetres", view.Name));
}
else
{
    var scale = (double)view.Scale;
    var right = view.RightDirection;
    var up = view.UpDirection;

    var annotation = new List<Element>();
    var kinds = new List<string>();

    Action<IEnumerable<Element>, string> take = (found, kind) =>
    {
        foreach (var e in found)
        {
            if (e == null) continue;
            if (annotation.Any(a => a.Id == e.Id)) continue;
            annotation.Add(e);
            kinds.Add(kind);
        }
    };

    take(new FilteredElementCollector(doc, view.Id).OfClass(typeof(IndependentTag)).ToList(), "tag");
    take(new FilteredElementCollector(doc, view.Id).OfClass(typeof(TextNote)).ToList(), "text");

    var byCategory = new[]
    {
        BuiltInCategory.OST_GenericAnnotation,
        BuiltInCategory.OST_KeynoteTags,
        BuiltInCategory.OST_RoomTags,
        BuiltInCategory.OST_MEPSpaceTags,
        BuiltInCategory.OST_AreaTags,
    };
    foreach (var category in byCategory)
    {
        try
        {
            take(new FilteredElementCollector(doc, view.Id)
                .OfCategory(category).WhereElementIsNotElementType().ToList(), "tag");
        }
        catch { }
    }

    // Each box reduced to a paper rectangle: min and max along the view's own
    // right and up directions.
    var uMin = new List<double>();
    var uMax = new List<double>();
    var vMin = new List<double>();
    var vMax = new List<double>();
    var kept = new List<Element>();
    var keptKind = new List<string>();

    for (var i = 0; i < annotation.Count; i++)
    {
        BoundingBoxXYZ box = null;
        try { box = annotation[i].get_BoundingBox(view); }
        catch { box = null; }
        if (box == null) { noBox++; continue; }

        var lo = box.Min;
        var hi = box.Max;
        double u0 = 0, u1 = 0, v0 = 0, v1 = 0;
        var first = true;

        for (var c = 0; c < 8; c++)
        {
            var corner = new XYZ(
                (c & 1) == 0 ? lo.X : hi.X,
                (c & 2) == 0 ? lo.Y : hi.Y,
                (c & 4) == 0 ? lo.Z : hi.Z);
            var u = corner.DotProduct(right);
            var v = corner.DotProduct(up);
            if (first) { u0 = u1 = u; v0 = v1 = v; first = false; }
            else
            {
                if (u < u0) u0 = u;
                if (u > u1) u1 = u;
                if (v < v0) v0 = v;
                if (v > v1) v1 = v;
            }
        }

        uMin.Add(u0); uMax.Add(u1); vMin.Add(v0); vMax.Add(v1);
        kept.Add(annotation[i]);
        keptKind.Add(kinds[i]);
    }

    Func<Element, string> label = e =>
    {
        var note = e as TextNote;
        if (note != null)
        {
            var text = (note.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length > 40 ? text.Substring(0, 40) + "..." : text;
        }
        var type = doc.GetElement(e.GetTypeId());
        return type != null ? type.Name : (e.Name ?? "");
    };

    var flagged = new HashSet<ElementId>();

    for (var i = 0; i < kept.Count; i++)
    {
        for (var j = i + 1; j < kept.Count; j++)
        {
            var sharedU = Math.Min(uMax[i], uMax[j]) - Math.Max(uMin[i], uMin[j]);
            if (sharedU <= 0) continue;
            var sharedV = Math.Min(vMax[i], vMax[j]) - Math.Max(vMin[i], vMin[j]);
            if (sharedV <= 0) continue;

            // The short side of the shared rectangle, on paper. Two boxes
            // sharing an edge share nothing; two sharing half their area are
            // unreadable, and this number is what tells them apart.
            var shortSide = Math.Min(sharedU, sharedV) * mmPerFoot / scale;
            if (shortSide < minOverlapMm) continue;

            pairs++;
            flagged.Add(kept[i].Id);
            flagged.Add(kept[j].Id);
            findings.Add(string.Format("{0} '{1}' over {2} '{3}'  - {4:0.0} mm on paper",
                keptKind[i], label(kept[i]), keptKind[j], label(kept[j]), shortSide));
        }
    }

    // ------------------------------------------------------------------
    // VERSION 2 (row 5b-1731): each TAG against the drawing under it.
    // ------------------------------------------------------------------
    var compared = "annotation against annotation only - walls, lines and dimensions were not compared "
        + "(annotationOnly)";
    if (!annotationOnly)
    {
        // Paper-plane geometry, in model feet along the view's right and up.
        Func<double, double, double, double, double, double, double> pointToSegment = (px, py, ax, ay, bx, by) =>
        {
            var dx = bx - ax; var dy = by - ay;
            var lengthSquared = dx * dx + dy * dy;
            var t = lengthSquared < 1e-18 ? 0.0 : ((px - ax) * dx + (py - ay) * dy) / lengthSquared;
            if (t < 0) t = 0; else if (t > 1) t = 1;
            var cx = ax + t * dx - px; var cy = ay + t * dy - py;
            return Math.Sqrt(cx * cx + cy * cy);
        };
        Func<double, double, double, double, double, double, double> side = (ax, ay, bx, by, cx, cy) =>
            (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        Func<double, double, double, double, double, double, double, double, bool> segmentsCross =
            (ax, ay, bx, by, cx, cy, ex, ey) =>
        {
            var d1 = side(cx, cy, ex, ey, ax, ay); var d2 = side(cx, cy, ex, ey, bx, by);
            var d3 = side(ax, ay, bx, by, cx, cy); var d4 = side(ax, ay, bx, by, ex, ey);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        };
        // The distance on paper from a segment to tag box k; 0 when they meet.
        Func<int, double, double, double, double, double> segmentToBox = (k, ax, ay, bx, by) =>
        {
            Func<double, double, bool> inside = (x, y) => x >= uMin[k] && x <= uMax[k] && y >= vMin[k] && y <= vMax[k];
            if (inside(ax, ay) || inside(bx, by)) return 0.0;
            var cu = new[] { uMin[k], uMax[k], uMax[k], uMin[k] };
            var cv = new[] { vMin[k], vMin[k], vMax[k], vMax[k] };
            for (var e = 0; e < 4; e++)
                if (segmentsCross(ax, ay, bx, by, cu[e], cv[e], cu[(e + 1) % 4], cv[(e + 1) % 4])) return 0.0;
            var best = double.MaxValue;
            for (var e = 0; e < 4; e++) best = Math.Min(best, pointToSegment(cu[e], cv[e], ax, ay, bx, by));
            Func<double, double, double> toBox = (x, y) =>
            {
                var ox = Math.Max(Math.Max(uMin[k] - x, 0.0), x - uMax[k]);
                var oy = Math.Max(Math.Max(vMin[k] - y, 0.0), y - vMax[k]);
                return Math.Sqrt(ox * ox + oy * oy);
            };
            best = Math.Min(best, Math.Min(toBox(ax, ay), toBox(bx, by)));
            return best;
        };
        Func<Curve, List<double[]>> onPaper = curve =>
        {
            var points = new List<double[]>();
            if (curve == null) return points;
            IList<XYZ> along = null;
            try { along = curve.Tessellate(); } catch { along = null; }
            if (along == null) return points;
            foreach (var p in along) points.Add(new[] { p.DotProduct(right), p.DotProduct(up) });
            return points;
        };
        // What a tag points at - its own element is never its clash.
        Func<Element, HashSet<ElementId>> taggedBy = e =>
        {
            var ids = new HashSet<ElementId>();
            var tag = e as IndependentTag;
            if (tag == null) return ids;
            try
            {
#if REVIT2020 || REVIT2021
                var one = tag.TaggedLocalElementId;
                if (one != null && one != ElementId.InvalidElementId) ids.Add(one);
#else
                foreach (var id in tag.GetTaggedLocalElementIds()) ids.Add(id);
#endif
            }
            catch { }
            return ids;
        };

        var tagIndex = new List<int>();
        for (var k = 0; k < kept.Count; k++) if (keptKind[k] == "tag") tagIndex.Add(k);
        var tagged = new Dictionary<int, HashSet<ElementId>>();
        foreach (var k in tagIndex) tagged[k] = taggedBy(kept[k]);

        var isPlan = view is ViewPlan;
        var walls = 0; var lines = 0; var dimensions = 0;
        var curvedWalls = 0;

        // WALLS - in a plan only.
        if (isPlan)
        {
            foreach (var wallElement in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Wall)).ToList())
            {
                var wall = wallElement as Wall;
                if (wall == null) continue;
                var located = wall.Location as LocationCurve;
                if (located == null || located.Curve == null) continue;
                double width = 0;
                try { width = wall.Width; } catch { width = 0; }
                if (width <= 0) continue;
                walls++;

                var centre = located.Curve;
                var half = width / 2.0;
                var line = centre as Line;
                if (line != null)
                {
                    // The poche is centred on the wall's middle, and the
                    // location curve is NOT always on the Location Line: one
                    // changed in Properties after the wall was drawn leaves
                    // the curve where it was (Group OC, OC5, Revit 2024,
                    // 2026-11-16 - Finish Face: Interior, curve still on the
                    // centre, so shifting by the parameter put the band 100 mm
                    // outside the wall). So the middle is read from the wall's
                    // own EXTERIOR side face; the parameter is the fallback
                    // only when no planar side face can be read.
                    try
                    {
                        double? toExterior = null;
                        foreach (var reference in HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior))
                        {
                            var face = wall.GetGeometryObjectFromReference(reference) as PlanarFace;
                            if (face == null) continue;
                            toExterior = (face.Origin - line.GetEndPoint(0)).DotProduct(wall.Orientation);
                            break;
                        }
                        if (toExterior.HasValue)
                        {
                            var toMiddle = toExterior.Value - half;
                            if (Math.Abs(toMiddle) > 1e-9)
                                centre = centre.CreateTransformed(Transform.CreateTranslation(wall.Orientation * toMiddle));
                        }
                        else
                        {
                            var structure = wall.WallType.GetCompoundStructure();
                            var keyParameter = wall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM);
                            if (structure != null && keyParameter != null)
                            {
                                var shift = structure.GetOffsetForLocationLine((WallLocationLine)keyParameter.AsInteger());
                                if (Math.Abs(shift) > 1e-9)
                                    centre = centre.CreateTransformed(Transform.CreateTranslation(wall.Orientation * -shift));
                            }
                        }
                    }
                    catch { centre = located.Curve; }
                }
                else
                {
                    // A curved wall: a band wide enough for any location line.
                    curvedWalls++;
                    half = width;
                }

                var path = onPaper(centre);
                var wallType = doc.GetElement(wall.GetTypeId());
                var wallName = wallType == null ? "wall" : wallType.Name;
                foreach (var k in tagIndex)
                {
                    if (tagged[k].Contains(wall.Id)) continue;
                    var hit = false;
                    for (var p = 0; p + 1 < path.Count && !hit; p++)
                        hit = segmentToBox(k, path[p][0], path[p][1], path[p + 1][0], path[p + 1][1]) < half;
                    if (!hit) continue;
                    overModel++;
                    flagged.Add(kept[k].Id);
                    findings.Add(string.Format("tag '{0}' over wall '{1}' (id {2}) - across the wall's thickness "
                        + "in plan, a door or window opening in it included", label(kept[k]), wallName, wall.Id));
                }
            }
        }

        // LINES - model and detail lines the view shows.
        try
        {
            foreach (var lineElement in new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_Lines).WhereElementIsNotElementType().ToList())
            {
                var curveElement = lineElement as CurveElement;
                if (curveElement == null) continue;
                lines++;
                var path = onPaper(curveElement.GeometryCurve);
                var style = "";
                try { style = curveElement.LineStyle == null ? "" : curveElement.LineStyle.Name; } catch { }
                foreach (var k in tagIndex)
                {
                    if (tagged[k].Contains(curveElement.Id)) continue;
                    var hit = false;
                    for (var p = 0; p + 1 < path.Count && !hit; p++)
                        hit = segmentToBox(k, path[p][0], path[p][1], path[p + 1][0], path[p + 1][1]) <= 0.0;
                    if (!hit) continue;
                    overModel++;
                    flagged.Add(kept[k].Id);
                    findings.Add(string.Format("tag '{0}' crossed by a line '{1}' (id {2})", label(kept[k]), style,
                        curveElement.Id));
                }
            }
        }
        catch { }

        // DIMENSIONS - the point each figure sits at.
        foreach (var dimensionElement in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Dimension)).ToList())
        {
            var dimension = dimensionElement as Dimension;
            if (dimension == null) continue;
            dimensions++;
            var figures = new List<XYZ>();
            try
            {
                if (dimension.NumberOfSegments > 1)
                {
                    foreach (DimensionSegment segment in dimension.Segments)
                    {
                        try { if (segment.TextPosition != null) figures.Add(segment.TextPosition); } catch { }
                    }
                }
                else if (dimension.TextPosition != null) figures.Add(dimension.TextPosition);
            }
            catch { }
            var dimensionType = doc.GetElement(dimension.GetTypeId());
            var dimensionName = dimensionType == null ? "dimension" : dimensionType.Name;
            foreach (var k in tagIndex)
            {
                var hit = false;
                foreach (var figure in figures)
                {
                    var fu = figure.DotProduct(right); var fv = figure.DotProduct(up);
                    if (fu >= uMin[k] && fu <= uMax[k] && fv >= vMin[k] && fv <= vMax[k]) { hit = true; break; }
                }
                if (!hit) continue;
                overModel++;
                flagged.Add(kept[k].Id);
                findings.Add(string.Format("tag '{0}' over the figure of dimension '{1}' (id {2})", label(kept[k]),
                    dimensionName, dimension.Id));
            }
        }

        compared = string.Format("{0} tag(s) also against {1}{2} line(s) and {3} dimension figure(s){4}",
            tagIndex.Count,
            isPlan ? walls + " wall(s), " : "",
            lines, dimensions,
            isPlan
                ? (curvedWalls > 0 ? string.Format(" ({0} curved wall(s) given a band as wide as the wall each side, "
                    + "so a tag beside one may be counted)", curvedWalls) : "")
                : " - walls are compared in a plan only, because in this view a wall's location line is not "
                    + "where its cut is drawn");
    }

    foreach (var id in flagged) overlapping.Add(id);

    findings.Insert(0, string.Format(
        "View '{0}' at 1:{1:0} - {2} annotation element(s) compared with each other, {3} clash(es); {4}, "
        + "{5} clash(es). A box is bigger than the ink, so check the top few on the sheet",
        view.Name, scale, kept.Count, pairs, compared, overModel));

    if (noBox > 0)
        findings.Add(string.Format("{0} annotation element(s) reported no bounding box in this view and "
            + "were not checked", noBox));
}
