// NOT STANDALONE. Assumes `doc`, `elements`, `kind` and `typeName` are in
// scope; leaves `made`, `sweepIds`, `edgeCount`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A SWEEP ALONG THE EDGES OF A ROOF OR FLOOR, the edges taken from the host's
// own geometry so nobody picks them:
//
//   fascia, gutter   a roof's eaves: the level edges of its upward faces at
//                    the lowest height of each face
//   slab edge        a floor's top outer edge: the outer loop of its top face
//
// One sweep per host, along all its edges at once, which is how Revit joins
// them at the corners. The type is found by name among the project's fascia,
// gutter or slab edge types; a name that is not there refuses, naming those
// that are.
//
// READ BACK: each new sweep is looked up again after a regeneration.

var made = 0;
var sweepIds = new List<ElementId>();
var edgeCount = 0;
var lines = new List<string>();
var findings = new List<string>();

var what = (kind ?? "").Trim().ToLowerInvariant().Replace(" ", "");
Type typeClass = what == "fascia" ? typeof(FasciaType) : what == "gutter" ? typeof(GutterType) : what == "slabedge" ? typeof(SlabEdgeType) : null;
string refusal = null;
Element type = null;
if (typeClass == null) refusal = "\"" + kind + "\" is not fascia, gutter or slab edge. NOTHING WAS MADE.";
else
{
    var types = new FilteredElementCollector(doc).OfClass(typeClass).ToElements();
    foreach (var candidate in types)
        if (string.Equals(candidate.Name, (typeName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { type = candidate; break; }
    if (type == null)
        refusal = "No " + kind + " type is named \"" + typeName + "\". The project has: "
            + (types.Count == 0 ? "none" : string.Join(", ", types.Select(t => t.Name))) + ". NOTHING WAS MADE.";
}

var options = new Options { ComputeReferences = true };
if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var isRoof = element is RoofBase;
        var isFloor = element is Floor;
        if ((what == "slabedge" && !isFloor) || (what != "slabedge" && !isRoof))
        {
            lines.Add(element.Id + ": a " + kind + " goes on a " + (what == "slabedge" ? "floor" : "roof") + ", and this is not one.");
            continue;
        }

        var references = new ReferenceArray();
        GeometryElement geometry = null;
        try { geometry = element.get_Geometry(options); } catch { }
        if (geometry == null) { lines.Add(element.Id + ": no geometry."); continue; }
        foreach (var item in geometry)
        {
            var solid = item as Solid;
            if (solid == null) continue;
            foreach (Face face in solid.Faces)
            {
                var planar = face as PlanarFace;
                XYZ n;
                try { n = planar != null ? planar.FaceNormal : face.ComputeNormal(new UV(0.5, 0.5)); } catch { continue; }
                if (n.Z <= 0.01) continue;
                if (what == "slabedge")
                {
                    if (n.Z < 0.99 || face.EdgeLoops.Size == 0) continue;
                    // The outer loop is the longest one.
                    EdgeArray outer = null;
                    var longest = 0.0;
                    foreach (EdgeArray loop in face.EdgeLoops)
                    {
                        var length = 0.0;
                        foreach (Edge e in loop) length += e.ApproximateLength;
                        if (length > longest) { longest = length; outer = loop; }
                    }
                    foreach (Edge e in outer) if (e.Reference != null) references.Append(e.Reference);
                }
                else
                {
                    var lowest = double.MaxValue;
                    foreach (EdgeArray loop in face.EdgeLoops)
                        foreach (Edge e in loop)
                        {
                            var c = e.AsCurve();
                            lowest = Math.Min(lowest, Math.Min(c.GetEndPoint(0).Z, c.GetEndPoint(1).Z));
                        }
                    foreach (EdgeArray loop in face.EdgeLoops)
                        foreach (Edge e in loop)
                        {
                            var c = e.AsCurve();
                            if (Math.Abs(c.GetEndPoint(0).Z - lowest) < 0.001 && Math.Abs(c.GetEndPoint(1).Z - lowest) < 0.001
                                && e.Reference != null)
                                references.Append(e.Reference);
                        }
                }
            }
        }

        if (references.Size == 0) { lines.Add(element.Id + ": no " + (what == "slabedge" ? "top outer edge" : "eave edge") + " found."); continue; }
        try
        {
            Element sweep = null;
            if (what == "fascia") sweep = doc.Create.NewFascia((FasciaType)type, references);
            else if (what == "gutter") sweep = doc.Create.NewGutter((GutterType)type, references);
            else sweep = doc.Create.NewSlabEdge((SlabEdgeType)type, references);
            doc.Regenerate();
            if (sweep != null && doc.GetElement(sweep.Id) != null)
            {
                made++;
                sweepIds.Add(sweep.Id);
                edgeCount += references.Size;
                lines.Add(element.Id + ": " + kind + " along " + references.Size + " edge(s).");
            }
            else lines.Add(element.Id + ": Revit returned no " + kind + ".");
        }
        catch (Exception ex) { lines.Add(element.Id + ": Revit refused - " + ex.Message); }
    }
    findings.Add(made + " " + kind + "(s) of \"" + type.Name + "\" made along " + edgeCount + " edge(s), read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
