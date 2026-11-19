// NOT STANDALONE. Assumes `doc`, `roomNumbers` and `marginMm` are in scope;
// leaves `refitted`, `refittedViews`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// INTERIOR ELEVATIONS MADE TO FIT THEIR ROOM AGAIN after the room changed size.
// An elevation marker standing inside a room carries that room's interior
// elevations; each is refitted to the room as it is NOW:
//
//   crop width and height   the room's extent across and up the view, plus
//                           marginMm each side
//   far clip                to the room's furthest extent in the direction the
//                           view looks, plus marginMm
//
// The room's extent is its bounding box - from its level to its upper limit -
// seen from each view. A room that is not rectangular is fitted to its box,
// which is what a person dragging the crop would do too.
//
// A VIEW A TEMPLATE CONTROLS keeps whatever the template owns; the crop is set
// where Revit allows it and each refusal is named.
//
// READ BACK: each view's crop is read again after a regeneration and compared
// with what was asked.
//
// THE MARKER IS FOUND THROUGH A FLOOR PLAN (rows 5b-1843 and 5b-1859). An
// elevation marker has no box of its own: get_BoundingBox(null) was null for
// every marker measured, five of them on Revit 2024, so version 1 skipped them
// all. Its box in a floor plan that shows it is not null, and that box's middle
// stands in the room. So the marker is read in the floor plans - one plan per
// level is enough - and the room is asked for at the box's middle, a foot above
// the box's foot. A marker that shows in the plans of more than one level can
// stand over a different room on each; it is then refitted only when
// roomNumbers names one of them, and otherwise named and left as it is.

const double MmPerFoot = 304.8;
var refitted = 0;
var refittedViews = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var part in (roomNumbers ?? "").Split(','))
    if (part.Trim() != "") wanted.Add(part.Trim());
var margin = Math.Max(0, marginMm) / MmPerFoot;

var floorPlans = new List<View>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
{
    var plan = element as View;
    if (plan != null && !plan.IsTemplate && plan.ViewType == ViewType.FloorPlan) floorPlans.Add(plan);
}

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ElevationMarker)))
{
    var marker = element as ElevationMarker;
    if (marker == null || marker.CurrentViewCount == 0) continue;

    var boxes = new List<BoundingBoxXYZ>();
    var ownBox = marker.get_BoundingBox(null);
    if (ownBox != null) boxes.Add(ownBox);
    else
    {
        var levelsRead = new HashSet<ElementId>();
        foreach (var plan in floorPlans)
        {
            var level = plan.GenLevel;
            if (level != null && levelsRead.Contains(level.Id)) continue;
            BoundingBoxXYZ inPlan = null;
            try { inPlan = marker.get_BoundingBox(plan); } catch { }
            if (inPlan == null) continue;
            boxes.Add(inPlan);
            if (level != null) levelsRead.Add(level.Id);
        }
    }

    var standsIn = new List<Room>();
    foreach (var box in boxes)
    {
        var middle = (box.Min + box.Max) / 2.0;
        Room found = null;
        try { found = doc.GetRoomAtPoint(new XYZ(middle.X, middle.Y, box.Min.Z + 1.0)); } catch { }
        if (found == null || found.Area <= 0) continue;
        if (!standsIn.Any(r => r.Id == found.Id)) standsIn.Add(found);
    }
    if (wanted.Count > 0) standsIn = standsIn.Where(r => wanted.Contains(r.Number)).ToList();
    if (standsIn.Count == 0) continue;
    if (standsIn.Count > 1)
    {
        var markerName = "the marker " + marker.Id;
        for (var i = 0; i < marker.MaximumViewCount; i++)
        {
            var carriedId = marker.GetViewId(i);
            if (carriedId == null || carriedId == ElementId.InvalidElementId) continue;
            var carried = doc.GetElement(carriedId) as View;
            if (carried != null) { markerName = "the marker of \"" + carried.Name + "\""; break; }
        }
        lines.Add("rooms " + string.Join(", ", standsIn.Select(r => r.Number)) + ": " + markerName
            + " stands over each of them, in the plans of different levels - not refitted; name the one room to refit it");
        continue;
    }
    var room = standsIn[0];

    var extent = room.get_BoundingBox(null);
    if (extent == null) continue;
    var corners = new List<XYZ>();
    foreach (var x in new[] { extent.Min.X, extent.Max.X })
        foreach (var y in new[] { extent.Min.Y, extent.Max.Y })
            foreach (var z in new[] { extent.Min.Z, extent.Max.Z })
                corners.Add(new XYZ(x, y, z));

    for (var i = 0; i < marker.MaximumViewCount; i++)
    {
        var viewId = marker.GetViewId(i);
        if (viewId == null || viewId == ElementId.InvalidElementId) continue;
        var view = doc.GetElement(viewId) as View;
        if (view == null) continue;
        var label = "room " + room.Number + ", \"" + view.Name + "\"";

        try
        {
            var crop = view.CropBox;
            var toView = crop.Transform.Inverse;
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            double deepest = double.MinValue;
            foreach (var corner in corners)
            {
                var local = toView.OfPoint(corner);
                minX = Math.Min(minX, local.X); maxX = Math.Max(maxX, local.X);
                minY = Math.Min(minY, local.Y); maxY = Math.Max(maxY, local.Y);
                // A view looks down its own -Z; depth is how far a corner lies
                // in front of the view's plane.
                deepest = Math.Max(deepest, -local.Z);
            }
            var fitted = new BoundingBoxXYZ();
            fitted.Transform = crop.Transform;
            fitted.Min = new XYZ(minX - margin, minY - margin, crop.Min.Z);
            fitted.Max = new XYZ(maxX + margin, maxY + margin, crop.Max.Z);

            if (!view.CropBoxActive) view.CropBoxActive = true;
            view.CropBox = fitted;

            var far = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
            if (far != null && !far.IsReadOnly && deepest > 0) far.Set(deepest + margin);

            doc.Regenerate();
            var now = view.CropBox;
            var width = (now.Max.X - now.Min.X) * MmPerFoot;
            var height = (now.Max.Y - now.Min.Y) * MmPerFoot;
            var askedWidth = (fitted.Max.X - fitted.Min.X) * MmPerFoot;
            var askedHeight = (fitted.Max.Y - fitted.Min.Y) * MmPerFoot;
            if (Math.Abs(width - askedWidth) < 1 && Math.Abs(height - askedHeight) < 1)
            {
                refitted++;
                refittedViews.Add(view.Id);
                lines.Add(label + ": crop " + Math.Round(width) + " x " + Math.Round(height) + " mm, far clip "
                    + Math.Round((deepest + margin) * MmPerFoot) + " mm");
            }
            else lines.Add(label + ": asked " + Math.Round(askedWidth) + " x " + Math.Round(askedHeight)
                + " mm and the crop reads back " + Math.Round(width) + " x " + Math.Round(height) + " mm");
        }
        catch (Exception ex)
        {
            lines.Add(label + ": Revit refused - " + ex.Message);
        }
    }
}

var report = string.Join(" | ", lines);
findings.Add(refitted + " interior elevation(s) refitted to their room and read back"
    + (wanted.Count > 0 ? " in room(s) " + string.Join(", ", wanted) : "") + ".");
if (refitted == 0 && lines.Count == 0)
    findings.Add("No elevation marker stands inside " + (wanted.Count > 0 ? "those rooms" : "any room") + ".");
findings.AddRange(lines);
