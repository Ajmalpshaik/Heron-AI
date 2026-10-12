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

const double MmPerFoot = 304.8;
var refitted = 0;
var refittedViews = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var part in (roomNumbers ?? "").Split(','))
    if (part.Trim() != "") wanted.Add(part.Trim());
var margin = Math.Max(0, marginMm) / MmPerFoot;

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ElevationMarker)))
{
    var marker = element as ElevationMarker;
    if (marker == null || marker.CurrentViewCount == 0) continue;
    var box = marker.get_BoundingBox(null);
    if (box == null) continue;
    var middle = (box.Min + box.Max) / 2.0;

    Room room = null;
    try { room = doc.GetRoomAtPoint(new XYZ(middle.X, middle.Y, box.Min.Z + 1.0)); } catch { }
    if (room == null || room.Area <= 0) continue;
    if (wanted.Count > 0 && !wanted.Contains(room.Number)) continue;

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
