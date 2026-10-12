// NOT STANDALONE. Assumes `doc`, `elements`, `material`, `faces` and `remove`
// are in scope; leaves `painted`, `elementsTouched`, `notPainted`, `report`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE PAINT TOOL, FOR MANY ELEMENTS AND A CHOSEN KIND OF FACE AT ONCE - a
// material laid on faces without changing the element's type. `faces` says
// which:
//
//   all        every face
//   vertical   faces standing upright
//   top        faces looking up;  bottom - faces looking down
//   exterior / interior   a wall's two main faces, as Revit names its sides
//   reveals    a wall's faces round its openings - jambs, head and sill:
//              upright faces square to the wall that are not its ends, and
//              level faces that are not its top or bottom
//
// remove true takes the paint off those faces instead.
//
// READ BACK: Revit is asked, face by face, whether it is painted, and only a
// face that now reads as asked counts. A face Revit will not paint - most
// family geometry - is counted apart, not as painted.

var painted = 0;
var elementsTouched = new List<ElementId>();
var notPainted = 0;
var lines = new List<string>();
var findings = new List<string>();

var which = (faces ?? "").Trim().ToLowerInvariant();
var known = new[] { "all", "vertical", "top", "bottom", "exterior", "interior", "reveals" };
string refusal = null;
if (!known.Contains(which)) refusal = "\"" + faces + "\" is not one of " + string.Join(", ", known) + ". NOTHING WAS CHANGED.";
else if (!remove && material == null) refusal = "No material was given. NOTHING WAS CHANGED.";

var options = new Options { ComputeReferences = true };
Func<Face, XYZ> normalOf = f =>
{
    try
    {
        var box = f.GetBoundingBox();
        var middle = (box.Min + box.Max) / 2.0;
        return f.ComputeNormal(middle).Normalize();
    }
    catch { return null; }
};
Func<Face, XYZ> centreOf = f =>
{
    try
    {
        var box = f.GetBoundingBox();
        return f.Evaluate((box.Min + box.Max) / 2.0);
    }
    catch { return null; }
};

if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;
        var wall = element as Wall;
        if ((which == "exterior" || which == "interior" || which == "reveals") && wall == null)
        {
            lines.Add(element.Id + ": \"" + which + "\" faces belong to walls, and this is not one.");
            continue;
        }

        var keep = new HashSet<string>();
        var drop = new HashSet<string>();
        Func<Reference, string> stable = r => { try { return r.ConvertToStableRepresentation(doc); } catch { return null; } };
        if (wall != null)
        {
            try
            {
                if (which == "exterior" || which == "reveals")
                    foreach (var r in HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior)) (which == "exterior" ? keep : drop).Add(stable(r));
                if (which == "interior" || which == "reveals")
                    foreach (var r in HostObjectUtils.GetSideFaces(wall, ShellLayerType.Interior)) (which == "interior" ? keep : drop).Add(stable(r));
                if (which == "reveals")
                {
                    foreach (var r in HostObjectUtils.GetTopFaces(wall)) drop.Add(stable(r));
                    foreach (var r in HostObjectUtils.GetBottomFaces(wall)) drop.Add(stable(r));
                }
            }
            catch { }
        }

        XYZ along = null, startPoint = null, endPoint = null;
        var located = wall == null ? null : wall.Location as LocationCurve;
        if (located != null && located.Curve is Line)
        {
            startPoint = located.Curve.GetEndPoint(0);
            endPoint = located.Curve.GetEndPoint(1);
            along = (endPoint - startPoint).Normalize();
        }

        var count = 0;
        GeometryElement geometry = null;
        try { geometry = element.get_Geometry(options); } catch { }
        if (geometry == null) { lines.Add(element.Id + ": no geometry to paint."); continue; }

        foreach (var item in geometry)
        {
            var solid = item as Solid;
            if (solid == null || solid.Faces.Size == 0) continue;
            foreach (Face face in solid.Faces)
            {
                var key = face.Reference == null ? null : stable(face.Reference);
                var n = normalOf(face);
                if (n == null) continue;
                bool wanted;
                if (which == "all") wanted = true;
                else if (which == "vertical") wanted = Math.Abs(n.Z) < 0.01;
                else if (which == "top") wanted = n.Z > 0.99;
                else if (which == "bottom") wanted = n.Z < -0.99;
                else if (which == "exterior" || which == "interior") wanted = key != null && keep.Contains(key);
                else
                {
                    // reveals
                    if (key != null && drop.Contains(key)) wanted = false;
                    else if (Math.Abs(n.Z) > 0.99) wanted = true;                       // head or sill
                    else if (along != null && Math.Abs(n.DotProduct(along)) > 0.99)      // square to the wall
                    {
                        var c = centreOf(face);
                        var half = wall.Width / 2.0 + 0.01;
                        wanted = c != null && c.DistanceTo(startPoint) > half && c.DistanceTo(endPoint) > half
                            && Math.Abs((c - startPoint).DotProduct(along)) > half
                            && Math.Abs((c - endPoint).DotProduct(along)) > half;      // not an end of the wall
                    }
                    else wanted = false;
                }
                if (!wanted) continue;

                try
                {
                    if (remove) doc.RemovePaint(element.Id, face);
                    else doc.Paint(element.Id, face, material.Id);
                }
                catch { notPainted++; continue; }

                bool now = false;
                try { now = doc.IsPainted(element.Id, face); } catch { }
                if (now == !remove) { painted++; count++; }
                else notPainted++;
            }
        }
        if (count > 0) elementsTouched.Add(element.Id);
        else lines.Add(element.Id + ": no " + which + " face " + (remove ? "had paint to take off" : "would take the paint") + ".");
    }

    findings.Add((remove ? "Paint taken off " : "Painted ") + painted + " " + which + " face(s)"
        + (remove ? "" : " with \"" + material.Name + "\"") + " on " + elementsTouched.Count + " element(s), read back; "
        + notPainted + " face(s) Revit would not " + (remove ? "clear" : "paint") + ".");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
