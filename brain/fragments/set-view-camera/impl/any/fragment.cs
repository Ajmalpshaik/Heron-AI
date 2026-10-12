// NOT STANDALONE. Assumes `doc`, `viewName`, `eyeMm`, `targetMm`,
// `perspective` and `createIfMissing` are in scope; leaves `camera`,
// `madeNew`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A 3D VIEW'S CAMERA - where the eye is and what it looks at - set from two
// points in millimetres. One job, whether the view exists or is new:
//
//   a 3D view of that name exists   its camera is moved
//   none, and createIfMissing       a new PERSPECTIVE view is made with that
//                                   name, then its camera set - what the
//                                   ribbon's Camera tool makes
//   none, and not createIfMissing   refused, naming it
//
// PERSPECTIVE: true turns an isometric view perspective where Revit allows
// it; false leaves a view as it is. Revit has no call that turns a
// perspective view back to isometric, and none is claimed.
//
// THE UP DIRECTION IS DERIVED, never asked for: square to the line of sight,
// in the vertical plane through it, so the camera is level - as a person
// standing there would hold it. Looking straight up or down, north is up.
//
// A VIEW A TEMPLATE LOCKS is refused rather than half-changed: the template
// owns its orientation.
//
// READ BACK: eye, target direction and perspective are read off the view
// afterwards and printed in millimetres.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
View3D camera = null;
var madeNew = false;
var readBack = "";

Func<string, XYZ> point = text =>
{
    var parts = (text ?? "").Split(',');
    if (parts.Length != 3) return null;
    double x, y, z;
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    var numberStyle = System.Globalization.NumberStyles.Float;
    if (!double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
        || !double.TryParse(parts[2].Trim(), numberStyle, culture, out z)) return null;
    return new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot);
};

var name = (viewName ?? "").Trim();
var eye = point(eyeMm);
var target = point(targetMm);

if (name == "") refused = "No view name was given, so NOTHING WAS CHANGED.";
else if (eye == null) refused = "\"" + eyeMm + "\" is not a point - three numbers in millimetres, "
    + "separated by commas. NOTHING WAS CHANGED.";
else if (target == null) refused = "\"" + targetMm + "\" is not a point - three numbers in millimetres, "
    + "separated by commas. NOTHING WAS CHANGED.";
else if (eye.DistanceTo(target) < 1.0 / MmPerFoot)
    refused = "The eye and the target are the same point, so there is nothing to look along. NOTHING WAS CHANGED.";

if (refused == null)
{
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
    {
        var v = candidate as View3D;
        if (v != null && !v.IsTemplate && string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            camera = v;
            break;
        }
    }

    if (camera == null && !createIfMissing)
        refused = "No 3D view is named \"" + name + "\", so NOTHING WAS CHANGED. Say createIfMissing to make a "
            + "new perspective view of that name.";
    else if (camera == null)
    {
        ViewFamilyType threeD = null;
        foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
        {
            var t = candidate as ViewFamilyType;
            if (t != null && t.ViewFamily == ViewFamily.ThreeDimensional) { threeD = t; break; }
        }
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

if (refused == null && camera != null && camera.ViewTemplateId != ElementId.InvalidElementId)
{
    refused = "\"" + camera.Name + "\" is controlled by a view template, which owns its orientation, so NOTHING "
        + "WAS CHANGED. Take the template off first.";
    camera = null;
}

if (refused == null && camera != null)
{
    var forward = (target - eye).Normalize();
    XYZ up;
    var flat = new XYZ(forward.X, forward.Y, 0);
    if (flat.GetLength() < 1e-9) up = XYZ.BasisY;
    else
    {
        var side = forward.CrossProduct(XYZ.BasisZ).Normalize();
        up = side.CrossProduct(forward).Normalize();
    }

    try
    {
        if (camera.IsLocked) camera.Unlock();
        camera.SetOrientation(new ViewOrientation3D(eye, up, forward));
        if (perspective && !camera.IsPerspective)
        {
            try { camera.ToggleToPerspective(); }
            catch (Exception ex) { findings.Add("Revit would not turn it perspective: " + ex.Message); }
        }
    }
    catch (Exception ex)
    {
        refused = "Revit refused the camera: " + ex.Message + ". " + (madeNew ? "The new view was made; its camera was not set." : "NOTHING WAS CHANGED.");
    }

    doc.Regenerate();
    var o = camera.GetOrientation();
    var e = o.EyePosition;
    var f = o.ForwardDirection;
    Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(System.Globalization.CultureInfo.InvariantCulture);
    Func<double, string> unit = v => Math.Round(v, 4).ToString(System.Globalization.CultureInfo.InvariantCulture);
    readBack = "eye " + mm(e.X) + "," + mm(e.Y) + "," + mm(e.Z) + " mm, looking "
        + unit(f.X) + "," + unit(f.Y) + "," + unit(f.Z) + ", " + (camera.IsPerspective ? "perspective" : "isometric");
    var aimedOff = f.AngleTo(forward) * 180.0 / Math.PI;
    if (e.DistanceTo(eye) > 1.0 / MmPerFoot || aimedOff > 0.01)
        findings.Add("The camera did not read back where it was put - Revit holds " + readBack + ".");
    findings.Add((madeNew ? "Made perspective view \"" : "Set the camera of \"") + camera.Name + "\": " + readBack + " (read back).");
}
if (refused != null) findings.Add(refused);
