// NOT STANDALONE. Assumes `elements` and `detail` are in scope; leaves
// `volumeM3`, `areaM2`, `totalVolumeM3`, `totalAreaM2`, `noSolid`,
// `geometry`, `solidCount` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// VERSION 2 (2026-11-01) - REPORT_ELEMENT_GEOMETRY MERGED IN. As well as the
// volume and surface area with their totals, each element's SHAPE in
// numbers, millimetres, in `geometry`:
//
//   summary   (the default) per element: solids, faces with their total area,
//             edges with their total length, volume, and the box it sits in
//   edges     the summary, then each edge - a line with its two ends, an arc
//             with its centre and radius, or the kind of curve it is
//   faces     the summary, then each face - flat with its normal and area, or
//             the kind of surface it is with its area
//
// The detailed lists stop at 200 lines and say so; a cylinder has edges a
// person does not want one by one.
//
// ONE DETAIL LEVEL FOR BOTH, AND IT IS FINE. The two cards read geometry at
// different detail levels: REPORT_ELEMENT_GEOMETRY at Fine, this one at an
// Options left unset - which Revit reads at its own default. A family can
// draw different solids at Coarse, Medium and Fine (its own visibility
// settings), and only Fine is the whole shape, so a volume measured below it
// can be short and the faces listed can be another shape's. Fine is the
// level that gives the element's real solids. Whether version 1's numbers
// move on a real model - for which families, and by how much - is NEEDS REAL
// REVIT.
//
// MEASURED FROM THE SOLIDS, NOT FROM A PARAMETER. Revit publishes a volume
// parameter on some categories and not on others, and where it exists it can
// be excluded by a project setting or left stale.
//
// UNITS ARE ARITHMETIC (D-20). One foot is 0.3048 m exactly.
//
// GEOMETRY IS ASKED FOR WITHOUT A VIEW, so it is the element's real solid
// rather than what some view happens to show, cut or cropped.
//
// NESTED GEOMETRY IS WALKED. A family instance carries its solids inside
// GeometryInstances, read as placed and turned.
//
// THE SAME ELEMENT TWICE IS MEASURED ONCE - totals and the dictionaries agree.
//
// INSULATION IS NOT INCLUDED. It is a separate element wrapped around the duct,
// so this is the bare duct's area.

var volumeM3 = new Dictionary<ElementId, double>();
var areaM2 = new Dictionary<ElementId, double>();
var noSolid = new List<ElementId>();
var totalVolumeM3 = 0.0;
var totalAreaM2 = 0.0;
// VERSION 2, from REPORT_ELEMENT_GEOMETRY.
var geometry = "";
var solidCount = 0;
var findings = new List<string>();

const double MetresPerFoot = 0.3048;
const double MmPerFoot = 304.8;
const int MaxLines = 200;
var cubic = MetresPerFoot * MetresPerFoot * MetresPerFoot;
var square = MetresPerFoot * MetresPerFoot;

var how = (detail ?? "").Trim().ToLowerInvariant();
if (how != "edges" && how != "faces") how = "summary";
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(culture);
Func<XYZ, string> point = p => mm(p.X) + "," + mm(p.Y) + "," + mm(p.Z);
var lines = new List<string>();
var detailLines = 0;

var options = new Options();
options.ComputeReferences = false;
options.IncludeNonVisibleObjects = false;
// Version 2: Fine, for both the measures and the shape - see above.
options.DetailLevel = ViewDetailLevel.Fine;

// Every solid with volume, nested ones included, as placed.
Func<GeometryElement, List<Solid>> solidsOf = null;
solidsOf = g =>
{
    var list = new List<Solid>();
    if (g == null) return list;
    foreach (var item in g)
    {
        var solid = item as Solid;
        if (solid != null && solid.Volume > 0.0) { list.Add(solid); continue; }
        // A family instance keeps its solids one level down. Reading only the
        // top level finds nothing for most equipment.
        var nested = item as GeometryInstance;
        if (nested != null) list.AddRange(solidsOf(nested.GetInstanceGeometry()));
    }
    return list;
};

var seen = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    if (!seen.Add(element.Id)) continue;   // measured once

    List<Solid> solids;
    try { solids = solidsOf(element.get_Geometry(options)); }
    catch (Exception) { noSolid.Add(element.Id); continue; }

    var volume = 0.0;
    var area = 0.0;
    foreach (var s in solids)
    {
        volume += s.Volume;
        area += s.SurfaceArea;
    }

    if (volume <= 0.0)
    {
        // No solid at all - a line, an annotation, a view-only element. Named
        // rather than recorded as zero, which would sit in a takeoff total
        // looking like a measured nothing.
        noSolid.Add(element.Id);
        continue;
    }

    var metres3 = volume * cubic;
    var metres2 = area * square;
    volumeM3[element.Id] = metres3;
    areaM2[element.Id] = metres2;
    totalVolumeM3 += metres3;
    totalAreaM2 += metres2;

    // VERSION 2: THE SHAPE, from the same solids.
    solidCount += solids.Count;
    int faceCount = 0, edgeCount = 0;
    double length = 0;
    foreach (var s in solids)
    {
        faceCount += s.Faces.Size;
        edgeCount += s.Edges.Size;
        foreach (Edge e in s.Edges) length += e.ApproximateLength;
    }
    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch { box = null; }
    lines.Add((element.Category == null ? "element" : element.Category.Name) + " " + element.Id + " | "
        + solids.Count + " solid(s), " + faceCount + " face(s) " + Math.Round(metres2, 3).ToString(culture)
        + " m2, " + edgeCount + " edge(s) " + mm(length) + " mm, volume "
        + Math.Round(metres3, 4).ToString(culture) + " m3"
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
                else if (arc != null) lines.Add("  arc centre " + point(arc.Center) + " radius " + mm(arc.Radius)
                    + " from " + point(c.GetEndPoint(0)) + " to " + point(c.GetEndPoint(1)));
                else lines.Add("  " + c.GetType().Name + " from " + point(c.GetEndPoint(0)) + " to "
                    + point(c.GetEndPoint(1)));
                detailLines++;
            }
        if (how == "faces")
            foreach (Face f in s.Faces)
            {
                if (detailLines >= MaxLines) break;
                var planar = f as PlanarFace;
                var a = Math.Round(f.Area * square, 4).ToString(culture) + " m2";
                if (planar != null) lines.Add("  flat face, normal " + Math.Round(planar.FaceNormal.X, 3).ToString(culture)
                    + "," + Math.Round(planar.FaceNormal.Y, 3).ToString(culture) + ","
                    + Math.Round(planar.FaceNormal.Z, 3).ToString(culture) + ", " + a);
                else lines.Add("  " + f.GetType().Name + ", " + a);
                detailLines++;
            }
    }
}
if (detailLines >= MaxLines) lines.Add("(the " + how + " list stopped at " + MaxLines + " lines)");

geometry = string.Join("\n", lines);
findings.Add(string.Format(culture, "{0} element(s) measured - {1:0.###} m3 and {2:0.###} m2 in all, from {3} "
    + "solid(s) read at Fine detail; {4} with no solid geometry", volumeM3.Count, totalVolumeM3, totalAreaM2,
    solidCount, noSolid.Count));
