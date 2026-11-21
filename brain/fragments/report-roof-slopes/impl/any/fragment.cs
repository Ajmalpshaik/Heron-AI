// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `roofsRead`,
// `report` and `findings` behind. Reads only - no transaction, nothing changed.
//
// SLOPEANGLE IS A RISE OVER A RUN, NOT AN ANGLE, WHATEVER ITS NAME SAYS.
// FootPrintRoof.get_SlopeAngle(edge) gives 0.5 for one up in two across, which
// is 26.57 degrees. It is turned into degrees here by its arctangent, in one
// place, and both are said - the degrees a modeller asks in and the ratio
// Revit's Properties shows when the project's slope unit is a rise.
//
// A ROOF BY FOOTPRINT KEEPS ITS SLOPE ON EACH FOOTPRINT LINE, not on the roof:
// a gable's two long sides define slope and its ends do not. So each line is
// read, with its length, and the lines that define no slope are said as such -
// that is half of what "which edges slope" asks.
//
// A ROOF BY EXTRUSION HAS NO SLOPE PARAMETER. Its pitch is the angle of each
// straight line of its profile from level, measured in the profile's own
// vertical plane: the rise is the line's height, the run its horizontal length.
// A curved line has no single pitch and is said to be curved.

const double MillimetresPerFoot = 304.8;
var degreesPerRadian = 180.0 / Math.PI;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

var roofsRead = 0;
var findings = new List<string>();
var lines = new List<string>();

var asked = elements ?? new List<Element>();
var fromSelection = asked.Count > 0;
IList<Element> candidates = fromSelection
    ? asked
    : new FilteredElementCollector(doc).OfClass(typeof(RoofBase)).WhereElementIsNotElementType().ToElements();

Func<double, string> pitchOf = ratio =>
    string.Format(invariant, "{0:0.##} degrees (rise {1:0.###} per 1 of run)",
        Math.Atan(ratio) * degreesPerRadian, ratio);

foreach (var element in candidates)
{
    if (element == null) continue;
    var typeName = doc.GetElement(element.GetTypeId()) is ElementType type ? type.Name : "no type";

    var footprint = element as FootPrintRoof;
    var extrusion = element as ExtrusionRoof;

    if (footprint != null)
    {
        var edges = new List<string>();
        var sloped = 0;
        var total = 0;
        foreach (ModelCurveArray loop in footprint.GetProfiles())
        {
            foreach (ModelCurve edge in loop)
            {
                total++;
                var lengthMm = edge.GeometryCurve.Length * MillimetresPerFoot;
                if (footprint.get_DefinesSlope(edge))
                {
                    sloped++;
                    edges.Add(string.Format(invariant, "line {0} ({1:0} mm) slopes at {2}",
                        edge.Id, lengthMm, pitchOf(footprint.get_SlopeAngle(edge))));
                }
                else
                {
                    edges.Add(string.Format(invariant, "line {0} ({1:0} mm) defines no slope",
                        edge.Id, lengthMm));
                }
            }
        }
        roofsRead++;
        lines.Add(string.Format(invariant, "Roof by footprint {0} \"{1}\": {2} of its {3} footprint lines "
            + "define slope{4}. {5}",
            element.Id, typeName, sloped, total,
            sloped == 0 ? " - it is flat" : "",
            string.Join("; ", edges)));
    }
    else if (extrusion != null)
    {
        var edges = new List<string>();
        foreach (ModelCurve edge in extrusion.GetProfile())
        {
            var curve = edge.GeometryCurve;
            var lengthMm = curve.Length * MillimetresPerFoot;
            if (curve is Line)
            {
                var start = curve.GetEndPoint(0);
                var end = curve.GetEndPoint(1);
                var rise = Math.Abs(end.Z - start.Z);
                var run = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
                edges.Add(run < 1e-9
                    ? string.Format(invariant, "line {0} ({1:0} mm) is upright", edge.Id, lengthMm)
                    : string.Format(invariant, "line {0} ({1:0} mm) at {2}", edge.Id, lengthMm, pitchOf(rise / run)));
            }
            else
            {
                edges.Add(string.Format(invariant, "line {0} ({1:0} mm) is curved, so it has no single pitch",
                    edge.Id, lengthMm));
            }
        }
        roofsRead++;
        lines.Add(string.Format(invariant, "Roof by extrusion {0} \"{1}\": its profile's lines, each from "
            + "level - {2}", element.Id, typeName, string.Join("; ", edges)));
    }
    else if (element is RoofBase)
    {
        findings.Add(string.Format(invariant, "{0} \"{1}\" is a roof this does not read - not a roof by "
            + "footprint or by extrusion (a roof by face, or one modelled in place)", element.Id, typeName));
    }
    else if (fromSelection)
    {
        findings.Add(string.Format(invariant, "{0} {1} is not a roof, so it was left out",
            element.Category == null ? "element" : element.Category.Name, element.Id));
    }
}

var report = string.Join("\n", lines);
if (roofsRead == 0)
{
    findings.Insert(0, fromSelection
        ? "No roof by footprint or by extrusion is among the elements selected, so no slope was read. "
          + "Select the roofs and ask again"
        : "This model has no roof by footprint or by extrusion, so no slope was read");
}
else
{
    findings.Insert(0, string.Format(invariant, "{0} roof(s) read{1}, nothing changed. {2}",
        roofsRead, fromSelection ? " from the selection" : " - every roof in the model, as none was selected",
        report.Replace("\n", " | ")));
}
