// NOT STANDALONE. Assumes `doc`, `elements` and `detail` are in scope; leaves
// `geometry`, `solidCount`, `noGeometry` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// AN ELEMENT'S SHAPE IN NUMBERS, in millimetres:
//
//   summary   per element: solids, faces with their total area, edges with
//             their total length, volume, and the box it sits in
//   edges     the summary, then each edge - a line with its two ends, or an
//             arc with its centre and radius, or the kind of curve it is
//   faces     the summary, then each face - flat with its normal and area,
//             or the kind of surface it is with its area
//
// The detailed lists stop at 200 lines and say so; a cylinder has edges a
// person does not want one by one. A family instance is read as it stands in
// the model, placed and turned.

const double MmPerFoot = 304.8;
const double SquareMetresPerSquareFoot = 0.09290304;
const double CubicMetresPerCubicFoot = 0.028316846592;
const int MaxLines = 200;
var lines = new List<string>();
var findings = new List<string>();
var solidCount = 0;
var noGeometry = new List<ElementId>();
var how = (detail ?? "").Trim().ToLowerInvariant();
if (how != "edges" && how != "faces") how = "summary";
var detailLines = 0;
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(culture);
Func<XYZ, string> point = p => mm(p.X) + "," + mm(p.Y) + "," + mm(p.Z);

Func<GeometryElement, List<Solid>> solidsOf = null;
solidsOf = g =>
{
    var list = new List<Solid>();
    if (g == null) return list;
    foreach (var item in g)
    {
        var solid = item as Solid;
        if (solid != null && solid.Faces.Size > 0) list.Add(solid);
        var instance = item as GeometryInstance;
        if (instance != null) list.AddRange(solidsOf(instance.GetInstanceGeometry()));
    }
    return list;
};

var options = new Options { DetailLevel = ViewDetailLevel.Fine };
foreach (var element in elements)
{
    if (element == null) continue;
    List<Solid> solids;
    try { solids = solidsOf(element.get_Geometry(options)); } catch { solids = new List<Solid>(); }
    if (solids.Count == 0) { noGeometry.Add(element.Id); continue; }
    solidCount += solids.Count;
    int faceCount = 0, edgeCount = 0;
    double area = 0, length = 0, volume = 0;
    foreach (var s in solids)
    {
        faceCount += s.Faces.Size;
        edgeCount += s.Edges.Size;
        volume += s.Volume;
        foreach (Face f in s.Faces) area += f.Area;
        foreach (Edge e in s.Edges) length += e.ApproximateLength;
    }
    var box = element.get_BoundingBox(null);
    lines.Add((element.Category == null ? "element" : element.Category.Name) + " " + element.Id + " | " + solids.Count
        + " solid(s), " + faceCount + " face(s) " + Math.Round(area * SquareMetresPerSquareFoot, 3).ToString(culture) + " m2, "
        + edgeCount + " edge(s) " + mm(length) + " mm, volume " + Math.Round(volume * CubicMetresPerCubicFoot, 4).ToString(culture) + " m3"
        + (box == null ? "" : " | box " + point(box.Min) + " to " + point(box.Max) + " mm"));

    foreach (var s in solids)
    {
        if (how == "edges")
            foreach (Edge e in s.Edges)
            {
                if (detailLines >= MaxLines) break;
                var c = e.AsCurve();
                var arc = c as Arc;
                if (c is Line) lines.Add("  line " + point(c.GetEndPoint(0)) + " to " + point(c.GetEndPoint(1)));
                else if (arc != null) lines.Add("  arc centre " + point(arc.Center) + " radius " + mm(arc.Radius) + " from " + point(c.GetEndPoint(0)) + " to " + point(c.GetEndPoint(1)));
                else lines.Add("  " + c.GetType().Name + " from " + point(c.GetEndPoint(0)) + " to " + point(c.GetEndPoint(1)));
                detailLines++;
            }
        if (how == "faces")
            foreach (Face f in s.Faces)
            {
                if (detailLines >= MaxLines) break;
                var planar = f as PlanarFace;
                var a = Math.Round(f.Area * SquareMetresPerSquareFoot, 4).ToString(culture) + " m2";
                if (planar != null) lines.Add("  flat face, normal " + Math.Round(planar.FaceNormal.X, 3).ToString(culture) + "," + Math.Round(planar.FaceNormal.Y, 3).ToString(culture) + "," + Math.Round(planar.FaceNormal.Z, 3).ToString(culture) + ", " + a);
                else lines.Add("  " + f.GetType().Name + ", " + a);
                detailLines++;
            }
    }
}
if (detailLines >= MaxLines) lines.Add("(the " + how + " list stopped at " + MaxLines + " lines)");

var geometry = string.Join("\n", lines);
findings.Add(solidCount + " solid(s) read from " + (elements.Count - noGeometry.Count) + " element(s); " + noGeometry.Count + " with no solid geometry.");
