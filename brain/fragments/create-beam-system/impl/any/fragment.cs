// NOT STANDALONE. Assumes `doc`, `boundaryMm`, `level`, `beamType`,
// `spacingMm` and `directionEdge` are in scope; leaves `beamSystem`,
// `beamCount`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A BEAM SYSTEM FILLING ONE BAY - the bay's corners in millimetres, the beams
// at a fixed spacing, running parallel to one edge of the bay. Revit lays the
// beams; this reads back how many it laid.
//
//   boundaryMm      "x,y; x,y; x,y; x,y" - the corners in order, closed
//                   automatically
//   directionEdge   which edge the beams run along: 0 is first corner to
//                   second, 1 is second to third, and so on
//   spacingMm       the fixed distance between beams, centred in the bay
//
// The level gives the height; the beam type is any structural framing type.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
BeamSystem beamSystem = null;
var beamCount = 0;

var corners = new List<XYZ>();
var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
foreach (var chunk in (boundaryMm ?? "").Split(';'))
{
    if (chunk.Trim() == "") continue;
    var parts = chunk.Split(',');
    double x, y;
    if (parts.Length < 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y))
    {
        refused = "\"" + chunk.Trim() + "\" is not a corner - two numbers in millimetres. NOTHING WAS MADE.";
        break;
    }
    corners.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, level == null ? 0 : level.Elevation));
}

if (refused == null && corners.Count < 3) refused = "A bay needs at least three corners. NOTHING WAS MADE.";
else if (refused == null && level == null) refused = "No level was given. NOTHING WAS MADE.";
else if (refused == null && beamType == null) refused = "No beam type was given. NOTHING WAS MADE.";
else if (refused == null && spacingMm <= 0) refused = "The spacing must be more than 0 mm. NOTHING WAS MADE.";
else if (refused == null && (directionEdge < 0 || directionEdge >= corners.Count))
    refused = "The bay has " + corners.Count + " edges, numbered 0 to " + (corners.Count - 1) + "; " + directionEdge + " is not one. NOTHING WAS MADE.";

if (refused == null)
{
    var profile = new List<Curve>();
    for (var i = 0; i < corners.Count; i++)
        profile.Add(Line.CreateBound(corners[i], corners[(i + 1) % corners.Count]));
    try
    {
        if (!beamType.IsActive) beamType.Activate();
        beamSystem = BeamSystem.Create(doc, profile, level, directionEdge, false);
        beamSystem.BeamType = beamType;
        beamSystem.LayoutRule = new LayoutRuleFixedDistance(spacingMm / MmPerFoot, BeamSystemJustifyType.Center);
        doc.Regenerate();
        beamCount = beamSystem.GetBeamIds().Count;
        findings.Add("Made a beam system of " + beamCount + " \"" + beamType.Name + "\" beam(s) at " + spacingMm
            + " mm on " + level.Name + ", running along edge " + directionEdge + " (read back).");
    }
    catch (Exception ex)
    {
        refused = "Revit refused the beam system: " + ex.Message + ". NOTHING WAS MADE.";
        beamSystem = null;
    }
}
if (refused != null) findings.Add(refused);
