// NOT STANDALONE. Assumes `doc`, `elements`, `viewName`, `direction`,
// `lookDownDeg`, `distanceMm`, `marginPercent`, `shot`, `side`, `eyeHeightMm`,
// `lensMm`, `aspect` and `composition` are in scope; leaves
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
double azimuth = 135;
var d = squash(direction);
var directionGiven = d.Length > 0 && d != "auto";
if (!directionGiven) d = "southeast";
if (compass.ContainsKey(d)) azimuth = compass[d];
else
{
    var a = number(direction);
    if (a == null || a < 0 || a > 360)
        refused = "'" + direction + "' is not a direction - say north, south-east and so on, or degrees clockwise from "
            + "project north. NOTHING WAS CHANGED.";
    else azimuth = a.Value;
}
var lookDownGiven = squash(lookDownDeg).Length > 0 && squash(lookDownDeg) != "auto";
var lookDown = lookDownGiven ? number(lookDownDeg) : 30;
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
var marginGiven = squash(marginPercent).Length > 0 && squash(marginPercent) != "auto";
var margin = marginGiven ? number(marginPercent) : 10;
if (refused == null && (margin == null || margin < 0 || margin > 200))
    refused = "'" + marginPercent + "' is not a margin - a percentage, 0 to 200. NOTHING WAS CHANGED.";

// ---- VERSION 3: a named shot, a side, a camera height, a lens, a picture shape
// and a composition - a visualisation artist's camera in one call. Every value
// below is Heron's STARTING value, never a standard (D-33): the picture is the
// judge. Each row: { turn from the side in degrees clockwise, look-down degrees
// (NaN: the camera height decides), camera height in mm above the elements'
// base (NaN: the look-down decides; -1: half the elements' height), lens in mm
// as on a 35 mm camera, picture width over height, composition (0 centre,
// 1 rule of thirds, 2 low horizon), 1 a LEVEL camera - verticals stay vertical,
// a two-point perspective - or 0 aimed at the elements' centre, margin % }.
var shots = new Dictionary<string, double[]>
{
    { "eyelevel",     new[] { 0.0,  double.NaN, 1600, 35, 1.5,       0, 1, 10 } },
    { "threequarter", new[] { 45.0, double.NaN, 1600, 35, 1.5,       0, 1, 10 } },
    { "front",        new[] { 0.0,  double.NaN, -1,   35, 1.5,       0, 1, 10 } },
    { "hero",         new[] { 30.0, double.NaN, 600,  24, 16.0 / 9,  1, 0, 10 } },
    { "wormseye",     new[] { 20.0, double.NaN, 150,  20, 1.5,       0, 0, 10 } },
    { "highangle",    new[] { 30.0, 35,         double.NaN, 35, 1.5, 0, 0, 10 } },
    { "birdseye",     new[] { 45.0, 55,         double.NaN, 35, 16.0 / 9, 0, 0, 10 } },
    { "topdown",      new[] { 0.0,  89,         double.NaN, 50, 4.0 / 3,  0, 0, 10 } },
    { "cinematic",    new[] { 30.0, double.NaN, 1200, 28, 2.39,      1, 1, 10 } },
    { "closeup",      new[] { 35.0, 15,         double.NaN, 85, 4.0 / 3,  0, 0, 5 } },
};
var shotAlias = new Dictionary<string, string>
{
    { "lowangle", "hero" }, { "aerial", "birdseye" }, { "plan", "topdown" }, { "onepoint", "front" },
    { "detail", "closeup" }, { "twopoint", "eyelevel" }, { "34", "threequarter" }, { "corner", "threequarter" },
    { "cinema", "cinematic" }, { "film", "cinematic" }, { "elevation", "front" },
};
double[] shotRow = null;
var shotKey = squash(shot);
if (shotKey.Length > 0 && shotKey != "none" && shotKey != "auto")
{
    if (shotAlias.ContainsKey(shotKey)) shotKey = shotAlias[shotKey];
    if (shots.ContainsKey(shotKey)) shotRow = shots[shotKey];
    else if (refused == null)
        refused = "'" + shot + "' is not a shot. The shots: eye-level, three-quarter, front, hero (low angle), worm's-eye, "
            + "high-angle, bird's-eye (aerial), top-down, cinematic, close-up. NOTHING WAS CHANGED.";
}
// THE SIDE: which side of the elements the camera stands on. Front is project
// south looking north, back north, left WEST and right EAST - as the plan reads
// with project north up; a compass word or degrees clockwise from project
// north is taken as it is.
double? sideAzimuth = null;
var sideKey = squash(side);
if (sideKey.Length > 0 && sideKey != "none" && sideKey != "auto")
{
    var sides = new Dictionary<string, double> { { "front", 180 }, { "back", 0 }, { "rear", 0 }, { "left", 270 }, { "right", 90 } };
    if (sides.ContainsKey(sideKey)) sideAzimuth = sides[sideKey];
    else if (compass.ContainsKey(sideKey)) sideAzimuth = compass[sideKey];
    else
    {
        var sa = number(side);
        if (sa != null && sa >= 0 && sa <= 360) sideAzimuth = sa;
        else if (refused == null)
            refused = "'" + side + "' is not a side - front, back, left, right, a compass word or degrees. NOTHING WAS CHANGED.";
    }
}
double? eyeHeight = null;
var eyeKey = squash(eyeHeightMm);
if (eyeKey.Length > 0 && eyeKey != "auto" && eyeKey != "none")
{
    eyeHeight = number(eyeHeightMm);
    if (refused == null && (eyeHeight == null || eyeHeight < 0 || eyeHeight > 500000))
        refused = "'" + eyeHeightMm + "' is not a camera height - millimetres above the elements' base. NOTHING WAS CHANGED.";
    if (refused == null && lookDownGiven)
        refused = "A camera height and a look-down angle were both given - say one: the height keeps the camera level, "
            + "the angle looks down at the elements. NOTHING WAS CHANGED.";
}
double? lens = null;
var lensKey = squash(lensMm);
if (lensKey.Length > 0 && lensKey != "auto" && lensKey != "none")
{
    lens = number((lensMm ?? "").ToLowerInvariant().Replace("mm", ""));
    if (refused == null && (lens == null || lens < 10 || lens > 300))
        refused = "'" + lensMm + "' is not a lens - millimetres on a 35 mm camera, 10 to 300: 24 wide, 35, 50 normal, 85 "
            + "close. NOTHING WAS CHANGED.";
}
double? pictureShape = null;
var aspectText = (aspect ?? "").Trim().ToLowerInvariant().Replace(" ", "");
if (aspectText.Length > 0 && aspectText != "auto" && aspectText != "none")
{
    var named = new Dictionary<string, double> { { "cinema", 2.39 }, { "cinemascope", 2.39 }, { "widescreen", 16.0 / 9 },
        { "square", 1 }, { "portrait", 2.0 / 3 }, { "landscape", 1.5 } };
    if (named.ContainsKey(aspectText)) pictureShape = named[aspectText];
    else
    {
        var parts = aspectText.Split(':', 'x', '/');
        double aw, ah;
        if (parts.Length == 2 && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out aw)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out ah) && aw > 0 && ah > 0)
            pictureShape = aw / ah;
        else if (parts.Length == 1 && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out aw) && aw > 0)
            pictureShape = aw;
    }
    if (refused == null && (pictureShape == null || pictureShape < 0.2 || pictureShape > 5))
        refused = "'" + aspect + "' is not a picture shape - 16:9, 3:2, 4:3, 2.39:1 (cinema), 1:1, 9:16 or auto. NOTHING WAS CHANGED.";
}
int? compositionWanted = null;
var compKey = squash(composition);
if (compKey.Length > 0 && compKey != "auto")
{
    if (compKey == "centre" || compKey == "center" || compKey == "centred" || compKey == "centered") compositionWanted = 0;
    else if (compKey == "thirds" || compKey == "ruleofthirds" || compKey == "third") compositionWanted = 1;
    else if (compKey == "lowhorizon" || compKey == "horizonlow") compositionWanted = 2;
    else if (refused == null)
        refused = "'" + composition + "' is not a composition - centre, thirds or low horizon. NOTHING WAS CHANGED.";
}
// Version 3 runs only when one of its inputs is given; left out, every value is
// version 2's and the camera is version 2's.
var cinematic = shotRow != null || sideAzimuth != null || eyeHeight != null || lens != null || pictureShape != null
    || (compositionWanted != null && compositionWanted.Value != 0);
if (cinematic)
{
    var turn = shotRow != null ? shotRow[0] : 0;
    if (sideAzimuth != null) azimuth = (sideAzimuth.Value + turn) % 360;
    else if (!directionGiven) azimuth = (180 + turn) % 360;
    if (!lookDownGiven && eyeHeight == null && shotRow != null && !double.IsNaN(shotRow[1])) lookDown = shotRow[1];
    if (!lookDownGiven && eyeHeight == null && shotRow != null && double.IsNaN(shotRow[1])) eyeHeight = shotRow[2];
    if (lens == null && shotRow != null) lens = shotRow[3];
    if (pictureShape == null && shotRow != null) pictureShape = shotRow[4];
    if (compositionWanted == null) compositionWanted = shotRow != null ? (int)shotRow[5] : 0;
    if (!marginGiven && shotRow != null) margin = shotRow[7];
    if (lens != null && pictureShape == null) pictureShape = 1.5;
}
var levelCamera = cinematic && eyeHeight != null && (shotRow == null || shotRow[6] == 1);

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

var shotSaid = "";
if (refused == null && camera != null)
{
    var centre = (low + high) * 0.5;
    var halfDiagonal = Math.Max((high - low).GetLength() * 0.5, 100 / MmPerFoot);
    var standOff = distance ?? halfDiagonal * 2.5;
    var az = azimuth * Math.PI / 180.0;
    var el = lookDown.Value * Math.PI / 180.0;
    var plan = new XYZ(Math.Sin(az), Math.Cos(az), 0);
    var heightAbove = eyeHeight == null ? 0 : (eyeHeight.Value < 0 ? (high.Z - low.Z) * 0.5 : eyeHeight.Value / MmPerFoot);
    // Where the eye stands for a stand-off s, and where it looks.
    Func<double, XYZ[]> pose = s =>
    {
        XYZ e, f;
        if (cinematic && eyeHeight != null)
        {
            e = new XYZ(centre.X + plan.X * s, centre.Y + plan.Y * s, low.Z + heightAbove);
            f = levelCamera ? plan.Negate() : (centre - e).Normalize();
        }
        else
        {
            var toEyeHere = new XYZ(Math.Sin(az) * Math.Cos(el), Math.Cos(az) * Math.Cos(el), Math.Sin(el));
            e = centre + toEyeHere * s;
            f = toEyeHere.Negate();
        }
        XYZ u;
        if (new XYZ(f.X, f.Y, 0).GetLength() < 1e-9) u = XYZ.BasisY;
        else u = f.CrossProduct(XYZ.BasisZ).Normalize().CrossProduct(f).Normalize();
        return new[] { e, f, u };
    };
    // The elements seen from a pose, on a window one unit in front of the eye:
    // { left, right, bottom, top, deepest } before the margin.
    Func<XYZ[], double[]> seen = p =>
    {
        var right = p[1].CrossProduct(p[2]);
        double sx0 = double.MaxValue, sx1 = double.MinValue, sy0 = double.MaxValue, sy1 = double.MinValue, far = 0;
        foreach (var x in new[] { low.X, high.X })
            foreach (var y in new[] { low.Y, high.Y })
                foreach (var z in new[] { low.Z, high.Z })
                {
                    var v = new XYZ(x, y, z) - p[0];
                    var depth = Math.Max(v.DotProduct(p[1]), 0.05);
                    var px = v.DotProduct(right) / depth;
                    var py = v.DotProduct(p[2]) / depth;
                    sx0 = Math.Min(sx0, px); sx1 = Math.Max(sx1, px); sy0 = Math.Min(sy0, py); sy1 = Math.Max(sy1, py);
                    far = Math.Max(far, depth);
                }
        return new[] { sx0, sx1, sy0, sy1, far };
    };
    // The window for what is seen: { centre x, centre y, half width, half height }.
    Func<double[], double[]> window = e =>
    {
        var gx = (e[1] - e[0]) * (margin.Value / 200.0);
        var gy = (e[3] - e[2]) * (margin.Value / 200.0);
        double x0 = e[0] - gx, x1 = e[1] + gx, y0 = e[2] - gy, y1 = e[3] + gy;
        var comp = compositionWanted ?? 0;
        double w, h, cx, cy;
        if (lens != null)
        {
            w = 18.0 / lens.Value;
            h = w / pictureShape.Value;
        }
        else
        {
            w = (x1 - x0) * 0.5 * (comp == 1 ? 1.5 : 1.0);
            h = (y1 - y0) * 0.5;
            if (comp == 2) h = Math.Max(h, Math.Max(0.75 * y1, -1.5 * y0));
            if (pictureShape != null)
            {
                if (w / h < pictureShape.Value) w = h * pictureShape.Value;
                else h = w / pictureShape.Value;
            }
        }
        cx = (x0 + x1) * 0.5 + (comp == 1 ? w / 3.0 : 0);
        cy = comp == 2 ? h / 3.0 : (y0 + y1) * 0.5;
        return new[] { cx, cy, w, h, x0, x1, y0, y1 };
    };
    // A LENS FIXES THE ANGLE, SO THE EYE MOVES: the stand-off is scaled until
    // the elements fill the window - a few rounds, since a perspective is not
    // linear in distance. A distance given is kept as it is.
    var minimum = cinematic && eyeHeight != null
        ? Math.Max(new XYZ((high - low).X, (high - low).Y, 0).GetLength() * 0.55, 300 / MmPerFoot)
        : halfDiagonal * 1.05;
    if (cinematic && lens != null && distance == null)
    {
        for (var round = 0; round < 12; round++)
        {
            var w0 = window(seen(pose(standOff)));
            var need = Math.Max(Math.Max((w0[5] - w0[0]) / w0[2], (w0[0] - w0[4]) / w0[2]),
                Math.Max((w0[7] - w0[1]) / w0[3], (w0[1] - w0[6]) / w0[3]));
            if (double.IsNaN(need) || need <= 0) break;
            var next = Math.Max(standOff * need, minimum);
            if (Math.Abs(next - standOff) < 1.0 / MmPerFoot) { standOff = next; break; }
            standOff = next;
        }
    }
    else if (cinematic) standOff = Math.Max(standOff, minimum);
    var posed = pose(standOff);
    var eye = posed[0];
    var forward = posed[1];
    var up = posed[2];
    if (cinematic)
        shotSaid = (shotRow != null ? shotKey + " shot" : "camera") + " from the " + (sideAzimuth != null ? side.Trim() + " side" : (directionGiven ? direction.Trim() : "front"))
            + (eyeHeight != null ? ", eye " + Math.Round(heightAbove * MmPerFoot).ToString(culture) + " mm above the elements' base"
                + (levelCamera ? ", a level camera - verticals stay vertical" : ", aimed at their centre")
                : ", looking down " + lookDown.Value.ToString(culture) + " degrees")
            + (lens != null ? ", lens " + lens.Value.ToString(culture) + " mm (35 mm camera)" : "")
            + (pictureShape != null ? ", picture " + Math.Round(pictureShape.Value, 2).ToString(culture) + ":1" : "")
            + ((compositionWanted ?? 0) == 1 ? ", rule of thirds" : (compositionWanted ?? 0) == 2 ? ", low horizon" : "");

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
        if (cinematic)
        {
            // Version 3: the same window worked out on the near plane - the
            // lens's angle, the picture shape and the composition - from the
            // corners Revit's own frame gives.
            var w3 = window(new[] { pxLo.Value / nearPlane, pxHi.Value / nearPlane, pyLo.Value / nearPlane, pyHi.Value / nearPlane });
            wanted.Min = new XYZ((w3[0] - w3[2]) * nearPlane, (w3[1] - w3[3]) * nearPlane, wanted.Min.Z);
            wanted.Max = new XYZ((w3[0] + w3[2]) * nearPlane, (w3[1] + w3[3]) * nearPlane, -nearPlane);
        }
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
        + "; " + framed + " of " + boxed.Count + " element(s) inside that crop box"
        + (camera.IsPerspective && crop.Max.X - crop.Min.X > 1e-9
            ? "; the window is a " + Math.Round(36.0 * (-crop.Max.Z) / (crop.Max.X - crop.Min.X)).ToString(culture)
                + " mm lens on a 35 mm camera, picture " + Math.Round((crop.Max.X - crop.Min.X) / (crop.Max.Y - crop.Min.Y), 2).ToString(culture) + ":1"
            : "")
        + (shotSaid.Length > 0 ? "; " + shotSaid : "") + ".";
    if (refused == null)
        findings.Add((madeNew ? "Made perspective view '" : "Re-aimed '") + camera.Name + "' at " + boxed.Count
            + " element(s) from " + (directionGiven && compass.ContainsKey(d) ? "the " + direction.Trim()
                : Math.Round(azimuth).ToString(culture) + " degrees clockwise from project north")
            + (cinematic && eyeHeight != null ? "" : ", looking down " + lookDown + "°") + ": " + readBack);
}
if (noGeometry.Count > 0) findings.Add(noGeometry.Count + " element(s) have no geometry and were left out of the frame");
if (refused != null) findings.Add(refused);
