// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `centred`, `landedAt`, `interiorPointUsed`, `orphanTags`, `notRoomTags` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One plan of tags, one undo.
//
// FOUR WAYS TO FIND "THE MIDDLE", IN THIS ORDER, AND THE ORDER IS THE POINT.
//
//   1  the area-weighted centroid of the room's own boundary
//   2  a sampled point that is genuinely INSIDE, when 1 falls outside
//   3  the centre of the bounding box, when the boundary cannot be read
//   4  the room's own location point, when nothing else survives
//
// Step 2 is the one that earns its place. On an L-shaped or U-shaped room the
// true centroid lands in the corridor or in the next tenancy, and a tag there
// reads as belonging to a room it is not in - a worse fault than the untidy
// tag it replaced, and a quiet one, because the tag still shows the right
// name.
//
// THE BOX CENTRE IS NOT A SUBSTITUTE FOR THE CENTROID.
//
// They agree on a rectangle and disagree on everything else, and the
// disagreement points at doorways. The box is kept only for a room whose
// boundary cannot be read at all.
//
// A LINKED ROOM IS IN THE LINK'S COORDINATES.
//
// On an MEP job the rooms usually live in the architect's link. Its centre
// comes back in that model's coordinate system and has to be pushed through
// the link's transform before it means anything here. Skipping that does not
// look like a small error - it puts the tag at the far end of the site.
//
// THE HEAD IS WHAT MOVES. Setting the tag's Location does nothing useful on a
// room tag. The head position is the property Revit draws from, and it is read
// back afterwards so the report says where the tag IS, not where it was sent.

int centred = 0;
var landedAt = new Dictionary<ElementId, XYZ>();
var interiorPointUsed = new List<ElementId>();
var orphanTags = new List<ElementId>();
var notRoomTags = new List<ElementId>();
var refused = new List<ElementId>();

var boundaryOptions = new SpatialElementBoundaryOptions();

// The area-weighted centroid of one closed loop of points, by the standard
// polygon formula. A degenerate loop - a slither with no area - returns null
// rather than a division by zero, and the caller falls through to the box.
Func<IList<XYZ>, XYZ> centroidOf = points =>
{
    if (points == null || points.Count < 3) return null;
    double twiceArea = 0, cx = 0, cy = 0;
    for (int i = 0; i < points.Count; i++)
    {
        var a = points[i];
        var b = points[(i + 1) % points.Count];
        double cross = a.X * b.Y - b.X * a.Y;
        twiceArea += cross;
        cx += (a.X + b.X) * cross;
        cy += (a.Y + b.Y) * cross;
    }
    if (Math.Abs(twiceArea) < 1e-9) return null;
    return new XYZ(cx / (3.0 * twiceArea), cy / (3.0 * twiceArea), points[0].Z);
};

foreach (var element in elements)
{
    var tag = element as RoomTag;
    if (tag == null)
    {
        if (element != null) notRoomTags.Add(element.Id);
        continue;
    }

    // The room, and the transform that puts it in THIS model's coordinates.
    // A local room needs the identity transform; a linked one needs its
    // link's, and a link that cannot be resolved is refused rather than
    // guessed at.
    Room room = null;
    Transform toHost = Transform.Identity;

    try { room = tag.Room; } catch { }

    if (room == null)
    {
        LinkElementId taggedId = null;
        try { taggedId = tag.TaggedRoomId; } catch { }

        if (taggedId != null && taggedId.LinkInstanceId != ElementId.InvalidElementId)
        {
            RevitLinkInstance link = null;
            try { link = doc.GetElement(taggedId.LinkInstanceId) as RevitLinkInstance; } catch { }
            if (link == null) { refused.Add(tag.Id); continue; }

            Document linkedDocument = null;
            try { linkedDocument = link.GetLinkDocument(); } catch { }
            if (linkedDocument == null) { refused.Add(tag.Id); continue; }

            try { room = linkedDocument.GetElement(taggedId.LinkedElementId) as Room; } catch { }
            try { toHost = link.GetTotalTransform(); } catch { toHost = Transform.Identity; }
        }
    }

    if (room == null) { orphanTags.Add(tag.Id); continue; }

    // An unplaced room has no area and nowhere to be centred on. Not a
    // failure of this fragment, and reported as its own state.
    double area = 0;
    try { area = room.Area; } catch { }
    if (area <= 0) { orphanTags.Add(tag.Id); continue; }

    // ---- 1. the boundary centroid ----
    XYZ target = null;
    bool sampled = false;
    IList<XYZ> outline = null;

    try
    {
        var loops = room.GetBoundarySegments(boundaryOptions);
        if (loops != null && loops.Count > 0)
        {
            var points = new List<XYZ>();
            foreach (var segment in loops[0])
            {
                Curve curve = null;
                try { curve = segment.GetCurve(); } catch { }
                if (curve == null) continue;
                // Tessellated, so an arc wall contributes its shape rather
                // than a chord - a curved room's centroid is otherwise pulled
                // towards the straight line across it.
                var pieces = curve.Tessellate();
                if (pieces == null) continue;
                for (int i = 0; i < pieces.Count - 1; i++) points.Add(pieces[i]);
            }
            if (points.Count >= 3) { outline = points; target = centroidOf(points); }
        }
    }
    catch { }

    // ---- 2. inside, or find one that is ----
    if (target != null)
    {
        bool inside = false;
        try { inside = room.IsPointInRoom(target); } catch { }

        if (!inside && outline != null)
        {
            // Sample across the loop's own extent and keep the inside point
            // nearest the centroid: nearest keeps the tag where the eye
            // expects it on a room that is only slightly concave, and any
            // inside point beats a point in the corridor.
            double minX = outline.Min(p => p.X), maxX = outline.Max(p => p.X);
            double minY = outline.Min(p => p.Y), maxY = outline.Max(p => p.Y);
            XYZ best = null;
            double bestDistance = double.MaxValue;
            const int steps = 12;

            for (int ix = 1; ix < steps; ix++)
            {
                for (int iy = 1; iy < steps; iy++)
                {
                    var candidate = new XYZ(
                        minX + (maxX - minX) * ix / steps,
                        minY + (maxY - minY) * iy / steps,
                        target.Z);
                    bool ok = false;
                    try { ok = room.IsPointInRoom(candidate); } catch { }
                    if (!ok) continue;
                    double distance = candidate.DistanceTo(target);
                    if (distance < bestDistance) { bestDistance = distance; best = candidate; }
                }
            }

            if (best != null) { target = best; sampled = true; }
        }
    }

    // ---- 3. the bounding box, only if the boundary was unreadable ----
    if (target == null)
    {
        try
        {
            var box = room.get_BoundingBox(null);
            if (box != null) target = (box.Min + box.Max) / 2.0;
        }
        catch { }
    }

    // ---- 4. the room's own point ----
    if (target == null)
    {
        try
        {
            var point = room.Location as LocationPoint;
            if (point != null) target = point.Point;
        }
        catch { }
    }

    if (target == null) { refused.Add(tag.Id); continue; }

    XYZ inHostCoordinates;
    try { inHostCoordinates = toHost.OfPoint(target); } catch { refused.Add(tag.Id); continue; }

    // The tag keeps its own height. A room tag is annotation in a plan, and
    // its Z belongs to the view it is drawn in, not to the room.
    XYZ head = null;
    try { head = tag.TagHeadPosition; } catch { }
    double z = head != null ? head.Z : inHostCoordinates.Z;

    try { tag.TagHeadPosition = new XYZ(inHostCoordinates.X, inHostCoordinates.Y, z); }
    catch { refused.Add(tag.Id); continue; }

    // Where it actually IS, read back, never where it was sent.
    XYZ now = null;
    try { now = tag.TagHeadPosition; } catch { }

    if (now == null) { refused.Add(tag.Id); continue; }

    landedAt[tag.Id] = now;
    centred++;
    if (sampled) interiorPointUsed.Add(tag.Id);
}
