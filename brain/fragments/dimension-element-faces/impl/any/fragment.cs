// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `direction`, `offsetMm`,
// `withOverall` and `side` are in scope; leaves `created`, `measured`,
// `noFaces`, `findings`, `segmentsDisagree`, `noOpeningFaces`, `notAWall` and
// `sideUsed` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ONE DIMENSION STRING PER ELEMENT, through every face of it that faces one
// way - the layers of a wall in a plan or a section, the build-up of a floor
// or a roof in a section, the base, sill, head and top of a wall in an
// elevation, the grid lines of a curtain wall, and - `along` a wall in a plan
// - its ends and the jambs of every opening cut through it.
//
//   direction   "across"     - a wall's thickness, a floor's or roof's depth
//               "along"      - down a wall's length: ends and jambs
//               "horizontal" - the view's own left to right
//               "vertical"   - the view's own bottom to top
//               EMPTY means across.
//
// THE FACES ARE THE ELEMENT'S OWN SOLID FACES. A compound wall or floor is
// read as Revit gives its geometry; each planar face whose normal runs the
// asked way is a reference, and faces at one position count once. An opening
// cuts the wall, so its sill and head are faces of the wall - the elevation
// string comes from the wall, not from the window - and so are its jambs: the
// plan string along a wall is taken off the WALL, never off the door, whose
// own left and right references are planes with no position to order them by
// (an unordered set gives a scrambled string that still looks like a
// dimension). A curtain wall offers its grid lines instead, each crossing the
// asked direction.
//
// The string sits through the middle of the element, moved offsetMm sideways
// in the view (0 runs through the middle). A family instance is refused here:
// CREATE_DIMENSION strings those by their centre references.
//
// READ BACK: each string's segments are read from the dimension Revit made.
//
// VERSION 2 (2026-11-01) - DIMENSION_WALL_OPENINGS MERGED IN, AND WHAT IT HAD
// THAT THIS LACKED:
//
//   ROOMS ARE ACCEPTED FOR THEIR WALLS. "Dimension room 4's walls" is how the
//   job is asked; a room, space or area handed in resolves to the walls on its
//   boundary, each once.
//
//   `withOverall` - a second, overall string twice the offset out, from the
//   first face to the last. It needs an offset: at 0 it would sit on top of
//   the running string, so it is not drawn and the line says so.
//
//   THE SEGMENTS MUST ADD UP, AND THAT CHECK IS BUILT IN. A join notch or a
//   second solid can leave an extra face, and an extra face is an extra
//   segment that still reads as a valid dimension; references out of order
//   overlap. Every string's segments are summed and compared with the overall
//   dimension when one was drawn, and with the distance between its first and
//   last face otherwise - more than 1 mm apart and the element is named in
//   `segmentsDisagree`. That card checked only with the overall drawn; the
//   check on every string is the safer of the two.
//
//   `side` - which side of a WALL the strings go, `along` it in a plan:
//   right or left of the wall as drawn (its location line, start to end);
//   outside, away from the rooms (a room or space found 300 mm clear of one
//   face and not the other; with rooms on both sides or neither, an Exterior
//   wall type's own exterior face, any other wall the side farther from the
//   middle of the walls joined to it, and only on a tie the exterior face);
//   inside, the opposite. Left out ("keep") is this card's own placement,
//   unchanged. Named, only walls are dimensioned - anything else goes to
//   `notAWall` - and each wall's line in `sideUsed` says which side, in plan
//   words, and why. A side this does not know, a side with a direction other
//   than along, or a side in a view that is not a plan is refused WHOLE,
//   nothing drawn: a misspelt side drawing every string on a default side
//   looks like it worked.
//
//   `noOpeningFaces` - walls dimensioned along their run that offered fewer
//   than two faces, or whose run is not a straight line: a CURVED WALL has no
//   single direction to project faces onto, and is refused rather than strung
//   along the view's horizontal, which version 1 did. They are in `noFaces`
//   too.
//
//   A REFUSAL BY REVIT is caught per element, as version 1 did; the overall's
//   refusal costs the overall, never the running string.

const double MmPerFoot = 304.8;
var created = new List<ElementId>();
var noFaces = new List<ElementId>();
var segmentsDisagree = new List<ElementId>();
var noOpeningFaces = new List<ElementId>();
var notAWall = new List<ElementId>();
var sideUsed = new List<string>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var way = (direction ?? "").Trim().ToLowerInvariant();
if (way == "") way = "across";

// "keep" is what a left-out side arrives as (the card's `absent: keep`); empty
// reads the same. Either is version 1's placement exactly.
var sideMode = (side ?? "").Trim().ToLowerInvariant();
if (sideMode == "keep") sideMode = "";
bool sideNamed = sideMode.Length > 0;

if (way != "across" && way != "along" && way != "horizontal" && way != "vertical")
    refusal = "\"" + direction + "\" is not across, along, horizontal or vertical. NOTHING WAS DRAWN.";
else if (view == null) refusal = "No view was given. NOTHING WAS DRAWN.";
else if (view is View3D || view.ViewType == ViewType.Schedule || view.ViewType == ViewType.DrawingSheet)
    refusal = "\"" + view.Name + "\" is not a plan, section, elevation or detail view - a dimension is drawn in one of those. NOTHING WAS DRAWN.";
else if (sideNamed && sideMode != "right" && sideMode != "left" && sideMode != "outside" && sideMode != "inside")
    refusal = "side \"" + side + "\" is not one this knows - leave it out, or say outside, inside, left or right. NOTHING WAS DRAWN.";
else if (sideNamed && way != "along")
    refusal = "side places the strings beside a wall dimensioned ALONG its run - with direction " + way + " there is no wall side to choose. Leave side out, or ask for along. NOTHING WAS DRAWN.";
else if (sideNamed && Math.Abs(view.ViewDirection.Normalize().DotProduct(XYZ.BasisZ)) < 0.99)
    refusal = "side places the strings beside a wall in a PLAN; \"" + view.Name + "\" is not one, so beside the wall points into the view. Leave side out here. NOTHING WAS DRAWN.";

var options = new Options();
options.ComputeReferences = true;
options.IncludeNonVisibleObjects = false;
options.DetailLevel = ViewDetailLevel.Fine;
var tolerance = 0.5 / MmPerFoot;

// ---- WHICH SIDE OF A WALL (version 2, from DIMENSION_WALL_OPENINGS) --------
// The phase rooms are looked for in: the view's own, so a demolished room in
// an earlier phase does not decide which side is outside.
Phase viewPhase = null;
if (refusal == null && sideNamed)
{
    try
    {
        var phaseParameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE);
        if (phaseParameter != null) viewPhase = doc.GetElement(phaseParameter.AsElementId()) as Phase;
    }
    catch { viewPhase = null; }
}

Func<XYZ, bool> inARoom = point =>
{
    try
    {
        if (viewPhase != null)
            return doc.GetRoomAtPoint(point, viewPhase) != null
                || doc.GetSpaceAtPoint(point, viewPhase) != null;
        return doc.GetRoomAtPoint(point) != null || doc.GetSpaceAtPoint(point) != null;
    }
    catch { return false; }
};

// THE MIDDLE OF THE BUILDING A WALL BELONGS TO: the centre, in plan, of the
// walls reached from it through the joins at their ends. Not every wall on the
// level - a level can carry two buildings, and the middle of both can sit
// outside each. Only a wall joined to fewer than two others falls back to
// every wall on its level.
var middleFor = new Dictionary<ElementId, XYZ>();
Func<IEnumerable<Wall>, XYZ> centreOf = group =>
{
    double minX = double.MaxValue, minY = double.MaxValue;
    double maxX = double.MinValue, maxY = double.MinValue;
    foreach (var member in group)
    {
        var bounds = member.get_BoundingBox(null);
        if (bounds == null) continue;
        minX = Math.Min(minX, bounds.Min.X); minY = Math.Min(minY, bounds.Min.Y);
        maxX = Math.Max(maxX, bounds.Max.X); maxY = Math.Max(maxY, bounds.Max.Y);
    }
    return minX <= maxX ? new XYZ((minX + maxX) / 2, (minY + maxY) / 2, 0) : null;
};
Func<Wall, XYZ> buildingMiddle = forWall =>
{
    XYZ found;
    if (middleFor.TryGetValue(forWall.Id, out found)) return found;

    var joined = new Dictionary<ElementId, Wall> { { forWall.Id, forWall } };
    var toVisit = new Queue<Wall>();
    toVisit.Enqueue(forWall);
    while (toVisit.Count > 0)
    {
        var at = toVisit.Dequeue();
        var atCurve = at.Location as LocationCurve;
        if (atCurve == null) continue;
        for (int end = 0; end < 2; end++)
        {
            ElementArray atJoin = null;
            try { atJoin = atCurve.get_ElementsAtJoin(end); } catch { atJoin = null; }
            if (atJoin == null) continue;
            foreach (Element neighbour in atJoin)
            {
                var neighbourWall = neighbour as Wall;
                if (neighbourWall == null || joined.ContainsKey(neighbourWall.Id)) continue;
                joined[neighbourWall.Id] = neighbourWall;
                toVisit.Enqueue(neighbourWall);
            }
        }
    }

    if (joined.Count >= 3)
    {
        found = centreOf(joined.Values);
        foreach (var member in joined.Keys) middleFor[member] = found;
    }
    else
    {
        var onLevel = new List<Wall>();
        foreach (Wall other in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
            if (other.LevelId == forWall.LevelId) onLevel.Add(other);
        found = centreOf(onLevel);
    }
    middleFor[forWall.Id] = found;
    return found;
};

// A direction in plan words, against the view's own up and right.
Func<XYZ, string> planWords = towards =>
{
    try
    {
        double up = towards.DotProduct(view.UpDirection);
        double right = towards.DotProduct(view.RightDirection);
        if (Math.Abs(up) >= Math.Abs(right)) return up >= 0 ? "above" : "below";
        return right >= 0 ? "right of" : "left of";
    }
    catch { return "to one side of"; }
};

// The side vector for a wall, and why - right of the wall as drawn is its
// location line's direction crossed with up.
Func<Wall, Line, KeyValuePair<XYZ, string>> wallSide = (wall, run) =>
{
    XYZ across = run.Direction.CrossProduct(XYZ.BasisZ).Normalize();
    string why = sideMode == "right" ? "right of the wall as drawn" : "left of the wall as drawn";
    if (sideMode == "left") across = across.Negate();
    if (sideMode == "outside" || sideMode == "inside")
    {
        // A point clear of each face, 300 mm above the wall's base, at the
        // middle of its run - a WHOLE width plus 300 mm from the location
        // line, because that line can be a face or a core edge, and half a
        // width from a finish face lands a probe inside a thick wall.
        XYZ middle = run.Evaluate(0.5, true);
        double clear = wall.Width + 300 / MmPerFoot;
        XYZ probeRight = middle + across * clear + XYZ.BasisZ * (300 / MmPerFoot);
        XYZ probeLeft = middle - across * clear + XYZ.BasisZ * (300 / MmPerFoot);
        bool roomRight = inARoom(probeRight);
        bool roomLeft = inARoom(probeLeft);

        int outwardIsRight = 0;
        string how;
        bool exteriorByType = false;
        try { exteriorByType = wall.WallType.Function == WallFunction.Exterior; } catch { }
        if (roomRight != roomLeft)
        {
            outwardIsRight = roomRight ? -1 : 1;
            how = "a room on one side only - outside is the side without it";
        }
        else if (exteriorByType)
        {
            // AN OUTSIDE WALL BY ITS TYPE KEEPS REVIT'S OWN EXTERIOR FACE: on
            // an L, U or C plan the box's centre can fall in the courtyard.
            outwardIsRight = wall.Orientation.DotProduct(across) >= 0 ? 1 : -1;
            how = (roomRight ? "rooms on both sides" : "no room on either side")
                + ", an Exterior wall type - outside is its exterior face";
        }
        else
        {
            how = null;
            XYZ centre = buildingMiddle(wall);
            if (centre != null)
            {
                var flatRight = new XYZ(probeRight.X - centre.X, probeRight.Y - centre.Y, 0);
                var flatLeft = new XYZ(probeLeft.X - centre.X, probeLeft.Y - centre.Y, 0);
                double gap = flatRight.GetLength() - flatLeft.GetLength();
                if (Math.Abs(gap) > 1e-6)
                {
                    outwardIsRight = gap > 0 ? 1 : -1;
                    how = (roomRight ? "rooms on both sides" : "no room on either side")
                        + " - outside is the side farther from the middle of the walls joined to it";
                }
            }
            if (outwardIsRight == 0)
            {
                outwardIsRight = wall.Orientation.DotProduct(across) >= 0 ? 1 : -1;
                how = "nothing else told the sides apart - outside is its exterior face";
            }
        }

        bool wantRight = sideMode == "outside" ? outwardIsRight > 0 : outwardIsRight < 0;
        if (!wantRight) across = across.Negate();
        why = sideMode + ": " + how;
    }
    return new KeyValuePair<XYZ, string>(across, why);
};

// ---- WHAT WAS HANDED IN: rooms resolve to their walls (version 2) ----------
var targets = new List<Element>();
var seen = new HashSet<ElementId>();
if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var room = element as SpatialElement;
        if (room == null)
        {
            if (seen.Add(element.Id)) targets.Add(element);
            continue;
        }
        try
        {
            var loops = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            int bounded = 0;
            if (loops != null)
                foreach (var loop in loops)
                    foreach (var segment in loop)
                    {
                        var bounding = doc.GetElement(segment.ElementId) as Wall;
                        if (bounding == null) continue;
                        bounded++;
                        if (seen.Add(bounding.Id)) targets.Add(bounding);
                    }
            if (bounded == 0)
            {
                // Unplaced, or bounded only by separation lines: no wall to
                // dimension, said rather than dropped.
                notAWall.Add(room.Id);
                noFaces.Add(room.Id);
                lines.Add(room.Id + ": a room with no wall on its boundary to dimension.");
            }
        }
        catch { notAWall.Add(room.Id); noFaces.Add(room.Id); lines.Add(room.Id + ": its boundary could not be read."); }
    }
}

if (refusal == null)
{
    var normal = view.ViewDirection.Normalize();
    var offset = offsetMm / MmPerFoot;
    foreach (var element in targets)
    {
        var wall = element as Wall;
        if (sideNamed && wall == null)
        {
            // A side is a wall's; nothing else is strung when one is named.
            notAWall.Add(element.Id);
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": not a wall - side places strings beside walls.");
            continue;
        }
        if (element is FamilyInstance)
        {
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": a family instance - CREATE_DIMENSION places those, by its centre references.");
            continue;
        }

        // The direction, in the model.
        XYZ dir = null;
        Line wallRun = null;
        if (way == "horizontal") dir = view.RightDirection;
        else if (way == "vertical") dir = view.UpDirection;
        else if (way == "across") dir = wall != null ? wall.Orientation : XYZ.BasisZ;
        else
        {
            var located = element.Location as LocationCurve;
            var run = located != null ? located.Curve as Line : null;
            if (wall != null && run == null)
            {
                // A CURVED WALL has no single direction along it (version 2,
                // from DIMENSION_WALL_OPENINGS): refused, never strung along
                // the view's horizontal.
                noFaces.Add(element.Id);
                noOpeningFaces.Add(element.Id);
                lines.Add(element.Id + ": a curved wall - there is no one direction along it to put its faces in order.");
                continue;
            }
            wallRun = wall != null ? run : null;
            dir = run != null ? run.Direction : view.RightDirection;
        }
        dir = dir.Normalize();
        if (Math.Abs(dir.DotProduct(normal)) > 0.01)
        {
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": the asked direction runs into the view, not across it - a wall's layers read in a plan or a section across the wall, a floor's in a section.");
            continue;
        }

        // Every reference, with where it sits along the direction.
        var stations = new List<KeyValuePair<double, Reference>>();
        var curtain = wall != null ? wall.CurtainGrid : null;
        try
        {
            if (curtain != null)
            {
                var ids = new List<ElementId>(curtain.GetUGridLineIds());
                ids.AddRange(curtain.GetVGridLineIds());
                foreach (var id in ids)
                {
                    var gridLine = doc.GetElement(id) as CurtainGridLine;
                    var curve = gridLine != null ? gridLine.FullCurve as Line : null;
                    if (curve == null || Math.Abs(curve.Direction.DotProduct(dir)) > 0.01) continue;
                    stations.Add(new KeyValuePair<double, Reference>(curve.Evaluate(0.5, true).DotProduct(dir), new Reference(gridLine)));
                }
            }
            else
            {
                foreach (GeometryObject item in element.get_Geometry(options))
                {
                    var solid = item as Solid;
                    if (solid == null || solid.Faces.Size == 0) continue;
                    foreach (Face face in solid.Faces)
                    {
                        var planar = face as PlanarFace;
                        if (planar == null || face.Reference == null) continue;
                        if (Math.Abs(planar.FaceNormal.DotProduct(dir)) > 0.99)
                            stations.Add(new KeyValuePair<double, Reference>(planar.Origin.DotProduct(dir), face.Reference));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            noFaces.Add(element.Id);
            if (wallRun != null) noOpeningFaces.Add(element.Id);
            lines.Add(element.Id + ": its geometry could not be read - " + ex.Message);
            continue;
        }

        var kept = new List<KeyValuePair<double, Reference>>();
        foreach (var station in stations.OrderBy(s => s.Key))
            if (kept.Count == 0 || station.Key - kept[kept.Count - 1].Key > tolerance) kept.Add(station);
        if (kept.Count < 2)
        {
            noFaces.Add(element.Id);
            if (wallRun != null) noOpeningFaces.Add(element.Id);
            lines.Add(element.Id + ": " + kept.Count + " face(s) facing that way - a string needs two.");
            continue;
        }

        // The line: through the middle of the element, in the view's plane,
        // moved sideways by the offset - to the named side of a wall when one
        // was named (version 2), else square to the direction in the view.
        var box = element.get_BoundingBox(null);
        if (box == null) { noFaces.Add(element.Id); lines.Add(element.Id + ": no extent to place a string by."); continue; }
        var middle = (box.Min + box.Max) / 2.0;
        middle = middle - normal * (middle - view.Origin).DotProduct(normal);
        var sideways = normal.CrossProduct(dir).Normalize();
        string sideLine = null;
        if (sideNamed && wall != null && wallRun != null)
        {
            var chosen = wallSide(wall, wallRun);
            sideways = chosen.Key;
            sideLine = wall.Name + " " + wall.Id + ": " + planWords(sideways)
                     + " the wall on the view (" + chosen.Value + ")";
        }
        var through = middle + sideways * offset;
        var from = through + dir * (kept[0].Key - through.DotProduct(dir));
        var to = through + dir * (kept[kept.Count - 1].Key - through.DotProduct(dir));

        Dimension dimension = null;
        try
        {
            var references = new ReferenceArray();
            foreach (var station in kept) references.Append(station.Value);
            dimension = doc.Create.NewDimension(view, Line.CreateBound(from, to), references);
        }
        catch (Exception ex) { lines.Add(element.Id + ": Revit refused the string - " + ex.Message); }
        if (dimension == null)
        {
            noFaces.Add(element.Id);
            if (wallRun != null) noOpeningFaces.Add(element.Id);
            continue;
        }
        created.Add(dimension.Id);
        if (sideLine != null) sideUsed.Add(sideLine);

        // THE OVERALL (version 2), twice the offset out, first face to last.
        Dimension overall = null;
        string overallNote = "";
        if (withOverall)
        {
            if (Math.Abs(offset) < 1e-9)
            {
                overallNote = "; no overall - at offset 0 it would sit on top of the running string";
            }
            else
            {
                try
                {
                    var ends = new ReferenceArray();
                    ends.Append(kept[0].Value);
                    ends.Append(kept[kept.Count - 1].Value);
                    overall = doc.Create.NewDimension(view,
                        Line.CreateBound(from + sideways * offset, to + sideways * offset), ends);
                }
                catch (Exception ex) { overallNote = "; Revit refused the overall - " + ex.Message; }
                if (overall != null) created.Add(overall.Id);
                else if (overallNote.Length == 0) overallNote = "; no overall was made, and Revit gave no reason";
            }
        }

        doc.Regenerate();
        var values = new List<string>();
        double summed = 0;
        try
        {
            if (dimension.Segments.Size > 0)
            {
                foreach (DimensionSegment segment in dimension.Segments)
                    if (segment.Value.HasValue)
                    {
                        summed += segment.Value.Value;
                        values.Add(Math.Round(segment.Value.Value * MmPerFoot).ToString());
                    }
            }
            else if (dimension.Value.HasValue)
            {
                // A MULTI-SEGMENT STRING HAS NO SINGLE VALUE; a two-reference
                // one does, and it is its own sum.
                summed = dimension.Value.Value;
                values.Add(Math.Round(dimension.Value.Value * MmPerFoot).ToString());
            }
        }
        catch { }

        // THE CHECK (version 2): the segments against the overall when one was
        // drawn, against the span from the first face to the last otherwise.
        double? overallValue = null;
        if (overall != null) { try { overallValue = overall.Value; } catch { overallValue = null; } }
        double? whole = overallValue.HasValue ? overallValue : kept[kept.Count - 1].Key - kept[0].Key;
        string checkNote = "";
        if (summed > 0 && Math.Abs(summed - whole.Value) > 1.0 / MmPerFoot)
        {
            segmentsDisagree.Add(element.Id);
            checkNote = "; THE SEGMENTS ADD UP TO " + Math.Round(summed * MmPerFoot) + " mm AGAINST "
                      + Math.Round(whole.Value * MmPerFoot) + " mm overall - check this string";
        }

        lines.Add(element.Id + " (" + (element.Category != null ? element.Category.Name : "element") + "): "
            + kept.Count + " references, " + string.Join(" + ", values) + " mm"
            + (overallValue.HasValue ? ", overall " + Math.Round(overallValue.Value * MmPerFoot) + " mm" : "")
            + overallNote + checkNote);
    }
    findings.Add(created.Count + " dimension string(s) drawn " + way + " in \"" + view.Name + "\" and read back; "
        + noFaces.Count + " element(s) offered no string"
        + (segmentsDisagree.Count > 0 ? "; " + segmentsDisagree.Count + " whose segments do not add up" : "")
        + ".");
}

if (refusal != null) lines.Add(refusal);
var measured = string.Join(" | ", lines);
findings.AddRange(lines);
