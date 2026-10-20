// NOT STANDALONE. Assumes `doc`, `elements`, `viewName`, `direction`,
// `lookDownDeg`, `distanceMm` and `marginPercent` are in scope; leaves
// `camera`, `madeNew`, `framed`, `readBack`, `noGeometry`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A PERSPECTIVE CAMERA THAT FRAMES THE ELEMENTS HANDED IN - "a close-up of this
// duct heater". The elements' box is taken; the eye stands off it in the
// compass direction asked (from project north, plan up), looking down at the
// angle asked, at the distance asked or two and a half times the box's
// half-diagonal; and the view's crop box is set to the elements' box seen from
// that eye, which Revit's API reference says moves "the crop region and far
// clip plane to the smallest values which will completely contain the 3D box".
//
// ONE JOB WHETHER THE VIEW EXISTS OR IS NEW: a perspective 3D view of that name
// is re-aimed; with none, one is made - what the ribbon's Camera tool makes.
// An isometric view of that name is refused, not turned: SET_VIEW_CAMERA turns
// a view perspective when that is what is wanted.
//
// THE CROP BOX IS GIVEN IN THE VIEW'S OWN FRAME - the transform Revit hands back
// with the crop box - so no assumption is made about where that frame sits.
// A PERSPECTIVE crop window is on Revit's near plane, millimetres in front of
// the eye, so the elements' corners are projected onto it (version 2, row
// 5b-1281 - version 1 set the box itself and drew the elements as a dot).
// `framed` counts the elements whose projected corners lie inside the window
// Revit reads back afterwards; the proof row still has the picture looked at.
//
// A VIEW A TEMPLATE CONTROLS is refused rather than half-changed.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
View3D camera = null;
var madeNew = false;
var framed = 0;
var readBack = "";
var noGeometry = new List<ElementId>();
var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
Func<string, double?> number = text =>
{
    double value;
    var t = (text ?? "").Trim().TrimEnd('°', '%').Trim();
    return double.TryParse(t, System.Globalization.NumberStyles.Float, culture, out value) ? (double?)value : null;
};
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(culture);

// The compass, clockwise from project north (plan up).
var compass = new Dictionary<string, double>
{
    { "north", 0 }, { "n", 0 }, { "northeast", 45 }, { "ne", 45 }, { "east", 90 }, { "e", 90 },
    { "southeast", 135 }, { "se", 135 }, { "south", 180 }, { "s", 180 }, { "southwest", 225 }, { "sw", 225 },
    { "west", 270 }, { "w", 270 }, { "northwest", 315 }, { "nw", 315 },
};
double azimuth = 0;
var d = squash(direction);
if (compass.ContainsKey(d)) azimuth = compass[d];
else
{
    var a = number(direction);
    if (a == null || a < 0 || a > 360)
        refused = "'" + direction + "' is not a direction - say north, south-east and so on, or degrees clockwise from "
            + "project north. NOTHING WAS CHANGED.";
    else azimuth = a.Value;
}
var lookDown = number(lookDownDeg);
if (refused == null && (lookDown == null || lookDown < -60 || lookDown > 89))
    refused = "'" + lookDownDeg + "' is not a look-down angle - degrees below level, -60 to 89. NOTHING WAS CHANGED.";
double? distance = null;
if (refused == null && squash(distanceMm) != "auto")
{
    distance = number(distanceMm);
    if (distance == null || distance <= 0)
        refused = "'" + distanceMm + "' is not a distance - millimetres from the elements' centre, or auto. "
            + "NOTHING WAS CHANGED.";
    else distance = distance / MmPerFoot;
}
var margin = number(marginPercent);
if (refused == null && (margin == null || margin < 0 || margin > 200))
    refused = "'" + marginPercent + "' is not a margin - a percentage, 0 to 200. NOTHING WAS CHANGED.";

// The elements' box, in model coordinates.
XYZ low = null, high = null;
var boxed = new List<Element>();
if (refused == null)
{
    if (elements == null || elements.Count == 0)
        refused = "No elements were handed in - select what the camera should frame. NOTHING WAS CHANGED.";
    else
    {
        foreach (var element in elements)
        {
            if (element == null) continue;
            var box = element.get_BoundingBox(null);
            if (box == null) { noGeometry.Add(element.Id); continue; }
            var corners = new[] { box.Transform.OfPoint(box.Min), box.Transform.OfPoint(box.Max) };
            foreach (var p in corners)
            {
                low = low == null ? p : new XYZ(Math.Min(low.X, p.X), Math.Min(low.Y, p.Y), Math.Min(low.Z, p.Z));
                high = high == null ? p : new XYZ(Math.Max(high.X, p.X), Math.Max(high.Y, p.Y), Math.Max(high.Z, p.Z));
            }
            boxed.Add(element);
        }
        if (boxed.Count == 0)
            refused = "None of the " + elements.Count + " element(s) has geometry to frame. NOTHING WAS CHANGED.";
    }
}

var name = (viewName ?? "").Trim();
if (refused == null && (name.Length == 0 || squash(name) == "auto"))
{
    var first = boxed[0];
    name = "Heron Frame - " + (first.Category != null ? first.Category.Name + " " : "") + first.Name
        + (boxed.Count > 1 ? " and " + (boxed.Count - 1) + " more" : "");
}

if (refused == null)
{
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
    {
        var v = candidate as View3D;
        if (v != null && !v.IsTemplate && string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)) { camera = v; break; }
    }
    if (camera != null && !camera.IsPerspective)
    {
        refused = "'" + camera.Name + "' is an isometric 3D view. This frames elements in a perspective view - name a new "
            + "one, or turn this one perspective with SET_VIEW_CAMERA first. NOTHING WAS CHANGED.";
        camera = null;
    }
    else if (camera != null && camera.ViewTemplateId != ElementId.InvalidElementId)
    {
        refused = "'" + camera.Name + "' is controlled by a view template, which owns its orientation. NOTHING WAS CHANGED.";
        camera = null;
    }
    else if (camera == null)
    {
        var threeD = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .FirstOrDefault(t => t.ViewFamily == ViewFamily.ThreeDimensional);
        if (threeD == null) refused = "The project has no 3D view type, so NOTHING WAS MADE.";
        else
        {
            try
            {
                camera = View3D.CreatePerspective(doc, threeD.Id);
                camera.Name = name;
                madeNew = true;
            }
            catch (Exception ex)
            {
                refused = "Revit refused the new perspective view: " + ex.Message + ". NOTHING WAS MADE.";
                camera = null;
            }
        }
    }
}

if (refused == null && camera != null)
{
    var centre = (low + high) * 0.5;
    var halfDiagonal = Math.Max((high - low).GetLength() * 0.5, 100 / MmPerFoot);
    var standOff = distance ?? halfDiagonal * 2.5;
    var az = azimuth * Math.PI / 180.0;
    var el = lookDown.Value * Math.PI / 180.0;
    var toEye = new XYZ(Math.Sin(az) * Math.Cos(el), Math.Cos(az) * Math.Cos(el), Math.Sin(el));
    var eye = centre + toEye * standOff;
    var forward = toEye.Negate();
    XYZ up;
    if (new XYZ(forward.X, forward.Y, 0).GetLength() < 1e-9) up = XYZ.BasisY;
    else up = forward.CrossProduct(XYZ.BasisZ).Normalize().CrossProduct(forward).Normalize();

    try
    {
        if (camera.IsLocked) camera.Unlock();
        camera.SetOrientation(new ViewOrientation3D(eye, up, forward));
        doc.Regenerate();

        // THE CROP WINDOW OF A PERSPECTIVE VIEW SITS ON REVIT'S OWN NEAR PLANE
        // (version 2, row 5b-1281). Measured 2026-10-20 on Revit 2020: a new
        // perspective view's crop box reads X -14.3 to 14.3, Y -10.7 to 10.7
        // mm at Z -40.2 mm - a window 40 mm in front of the eye. Version 1 set
        // the elements' box itself, metres wide and metres away; the view then
        // drew the elements as a dot in the middle of the picture
        // (EXPORT_VIEW_IMAGE, three views). So each corner is now projected
        // onto that near plane - x times near over its depth - and the window
        // is the projected corners plus the margin; the near plane is left
        // where Revit has it. A near plane a version-1 run moved metres away is
        // put back at 40.2 mm, a new view's.
        var frame = camera.CropBox.Transform;
        var toView = frame.Inverse;
        var nearPlane = -camera.CropBox.Max.Z;
        if (nearPlane <= 0 || nearPlane > 1.0) nearPlane = 40.2 / MmPerFoot;
        double? pxLo = null, pxHi = null, pyLo = null, pyHi = null, deepest = null;
        foreach (var x in new[] { low.X, high.X })
            foreach (var y in new[] { low.Y, high.Y })
                foreach (var z in new[] { low.Z, high.Z })
                {
                    var p = toView.OfPoint(new XYZ(x, y, z));
                    var depth = Math.Max(-p.Z, nearPlane);
                    var px = p.X * nearPlane / depth;
                    var py = p.Y * nearPlane / depth;
                    pxLo = pxLo == null ? px : Math.Min(pxLo.Value, px);
                    pxHi = pxHi == null ? px : Math.Max(pxHi.Value, px);
                    pyLo = pyLo == null ? py : Math.Min(pyLo.Value, py);
                    pyHi = pyHi == null ? py : Math.Max(pyHi.Value, py);
                    deepest = deepest == null ? depth : Math.Max(deepest.Value, depth);
                }
        var growX = (pxHi.Value - pxLo.Value) * (margin.Value / 200.0);
        var growY = (pyHi.Value - pyLo.Value) * (margin.Value / 200.0);
        var wanted = new BoundingBoxXYZ();
        wanted.Transform = frame;
        wanted.Min = new XYZ(pxLo.Value - growX, pyLo.Value - growY, Math.Min(camera.CropBox.Min.Z, -deepest.Value * 1.1));
        wanted.Max = new XYZ(pxHi.Value + growX, pyHi.Value + growY, -nearPlane);
        camera.CropBoxActive = true;
        camera.CropBox = wanted;
        doc.Regenerate();
    }
    catch (Exception ex)
    {
        refused = "Revit refused the camera: " + ex.Message + ". " + (madeNew ? "The new view was made; it is not framed."
            : "NOTHING WAS CHANGED.");
    }

    // ---- read back ----
    var o = camera.GetOrientation();
    var crop = camera.CropBox;
    var back = crop.Transform.Inverse;
    foreach (var element in boxed)
    {
        var box = element.get_BoundingBox(null);
        var inside = true;
        foreach (var x in new[] { box.Min.X, box.Max.X })
            foreach (var y in new[] { box.Min.Y, box.Max.Y })
                foreach (var z in new[] { box.Min.Z, box.Max.Z })
                {
                    // Seen through the window: the corner projected onto the
                    // crop box's near plane, as the view draws it (version 2).
                    var p = back.OfPoint(box.Transform.OfPoint(new XYZ(x, y, z)));
                    var near = -crop.Max.Z;
                    var depthHere = -p.Z;
                    var tol = 0.01 / MmPerFoot;
                    if (depthHere <= 0) { inside = false; continue; }
                    var sx = camera.IsPerspective ? p.X * near / depthHere : p.X;
                    var sy = camera.IsPerspective ? p.Y * near / depthHere : p.Y;
                    if (sx < crop.Min.X - tol || sx > crop.Max.X + tol || sy < crop.Min.Y - tol || sy > crop.Max.Y + tol
                        || p.Z < crop.Min.Z - tol) inside = false;
                }
        if (inside) framed++;
    }
    var farActive = camera.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR);
    var farOffset = camera.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
    readBack = "'" + camera.Name + "': eye " + mm(o.EyePosition.X) + "," + mm(o.EyePosition.Y) + "," + mm(o.EyePosition.Z)
        + " mm looking " + Math.Round(o.ForwardDirection.X, 4).ToString(culture) + ","
        + Math.Round(o.ForwardDirection.Y, 4).ToString(culture) + "," + Math.Round(o.ForwardDirection.Z, 4).ToString(culture)
        + ", " + (camera.IsPerspective ? "perspective" : "isometric") + "; crop box in the view's frame X "
        + mm(crop.Min.X) + " to " + mm(crop.Max.X) + ", Y " + mm(crop.Min.Y) + " to " + mm(crop.Max.Y) + ", Z "
        + mm(crop.Min.Z) + " to " + mm(crop.Max.Z) + " mm; far clip "
        + (farActive != null && farActive.AsInteger() == 1
            ? "on at " + (farOffset != null ? mm(farOffset.AsDouble()) + " mm" : "an offset Revit did not give") : "off")
        + "; " + framed + " of " + boxed.Count + " element(s) inside that crop box.";
    if (refused == null)
        findings.Add((madeNew ? "Made perspective view '" : "Re-aimed '") + camera.Name + "' at " + boxed.Count
            + " element(s) from the " + (compass.ContainsKey(d) ? direction.Trim() : azimuth + "°") + ", looking down "
            + lookDown + "°: " + readBack);
}
if (noGeometry.Count > 0) findings.Add(noGeometry.Count + " element(s) have no geometry and were left out of the frame");
if (refused != null) findings.Add(refused);
