// NOT STANDALONE. Assumes `doc`, `elements`, `faces` and `offsetMm` are in
// scope; leaves `offsetCount`, `partsTouched`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE FACES OF PARTS PUSHED OUT OR PULLED IN - a finish part thickened at its
// top, a structure part trimmed back from an edge. `faces` picks which: top,
// bottom, vertical or all; a positive offset is outward. Only faces Revit lets
// move are moved.
//
// READ BACK: each face's offset is read again, and only one reading as asked
// counts.

const double MmPerFoot = 304.8;
var offsetCount = 0;
var partsTouched = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
var which = (faces ?? "").Trim().ToLowerInvariant();
var distance = offsetMm / MmPerFoot;

string refusal = null;
if (which != "top" && which != "bottom" && which != "vertical" && which != "all")
    refusal = "\"" + faces + "\" is not top, bottom, vertical or all. NOTHING WAS CHANGED.";

if (refusal == null)
{
    var options = new Options { ComputeReferences = true };
    foreach (var element in elements)
    {
        var part = element as Part;
        if (part == null) { if (element != null) lines.Add(element.Id + ": not a part."); continue; }
        var count = 0;
        var geometry = part.get_Geometry(options);
        if (geometry == null) continue;
        foreach (var item in geometry)
        {
            var solid = item as Solid;
            if (solid == null) continue;
            foreach (Face face in solid.Faces)
            {
                XYZ n;
                try
                {
                    var box = face.GetBoundingBox();
                    n = face.ComputeNormal((box.Min + box.Max) / 2.0);
                }
                catch { continue; }
                var wanted = which == "all" || (which == "top" && n.Z > 0.99) || (which == "bottom" && n.Z < -0.99)
                    || (which == "vertical" && Math.Abs(n.Z) < 0.01);
                if (!wanted) continue;
                try
                {
                    if (!part.CanOffsetFace(face)) continue;
                    part.SetFaceOffset(face, distance);
                    if (Math.Abs(part.GetFaceOffset(face) - distance) < 1e-6) count++;
                }
                catch { }
            }
        }
        if (count > 0) { offsetCount += count; partsTouched.Add(part.Id); }
        else lines.Add("part " + part.Id + ": no " + which + " face Revit lets move.");
    }
    findings.Add(offsetCount + " " + which + " face(s) offset " + offsetMm + " mm on " + partsTouched.Count + " part(s), read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
