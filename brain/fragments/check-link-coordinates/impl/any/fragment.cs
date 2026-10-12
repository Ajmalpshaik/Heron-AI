// NOT STANDALONE. Assumes `doc` and `toleranceMm` are in scope; leaves
// `coordinates`, `mismatched`, `agreeing` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// DO THE LINKS AND THIS MODEL AGREE ON SHARED COORDINATES? For every loaded
// Revit link, two points of the link - its internal origin and a point 30 m
// east of it - are given their shared coordinates twice:
//
//   in the link's own coordinate system, as Report Shared Coordinates would
//   in this model's, after the link instance's placement carries them here
//
// When the two agree within toleranceMm for both points, the link sits where
// its own shared coordinates say it should. When they do not, the difference
// in East/West, North/South and Elevation is printed in millimetres, for both
// points - a difference that changes between the points is a rotation.
//
// A link placed origin to origin on purpose will disagree, and that is true
// information: it is not where the survey says it is.

const double MmPerFoot = 304.8;
var lines = new List<string>();
var mismatched = new List<string>();
var agreeing = 0;
var findings = new List<string>();
var tolerance = (toleranceMm <= 0 ? 1.0 : toleranceMm) / MmPerFoot;

ProjectLocation here = null;
try { here = doc.ActiveProjectLocation; } catch { }

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
{
    var link = element as RevitLinkInstance;
    if (link == null) continue;
    var linked = link.GetLinkDocument();
    if (linked == null) { lines.Add(link.Name + ": not loaded - not checked."); continue; }

    ProjectLocation there = null;
    try { there = linked.ActiveProjectLocation; } catch { }
    if (here == null || there == null) { lines.Add(link.Name + ": a project location could not be read."); continue; }

    var placed = link.GetTotalTransform();
    var probes = new[] { XYZ.Zero, new XYZ(30000.0 / MmPerFoot, 0, 0) };
    var worst = 0.0;
    var parts = new List<string>();
    foreach (var probe in probes)
    {
        var own = there.GetProjectPosition(probe);
        var ours = here.GetProjectPosition(placed.OfPoint(probe));
        var dEast = ours.EastWest - own.EastWest;
        var dNorth = ours.NorthSouth - own.NorthSouth;
        var dUp = ours.Elevation - own.Elevation;
        worst = Math.Max(worst, Math.Max(Math.Abs(dEast), Math.Max(Math.Abs(dNorth), Math.Abs(dUp))));
        parts.Add("E/W " + Math.Round(dEast * MmPerFoot, 1) + ", N/S " + Math.Round(dNorth * MmPerFoot, 1)
            + ", elevation " + Math.Round(dUp * MmPerFoot, 1) + " mm");
    }
    if (worst <= tolerance)
    {
        agreeing++;
        lines.Add(link.Name + ": agrees within " + Math.Round(tolerance * MmPerFoot, 1) + " mm.");
    }
    else
    {
        mismatched.Add(link.Name);
        lines.Add(link.Name + ": DISAGREES - at its origin " + parts[0] + "; 30 m east " + parts[1]
            + (parts[0] == parts[1] ? "" : " (the difference changes, so it is also turned)") + ".");
    }
}

var coordinates = string.Join("\n", lines);
findings.Add(lines.Count == 0 ? "The model has no Revit links."
    : agreeing + " link(s) agree with this model's shared coordinates; " + mismatched.Count + " do not"
      + (mismatched.Count == 0 ? "." : ": " + string.Join(", ", mismatched) + "."));
