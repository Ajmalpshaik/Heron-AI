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
// that is half of what "which edges slope" asks. A slope arrow, or shape
// editing, can slope a roof with no line defining slope; neither is read, so a
// roof with none is never called flat.
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
var candidates = new List<Element>();
if (fromSelection)
{
    candidates.AddRange(asked);
}
else
{
    candidates.AddRange(new FilteredElementCollector(doc).OfClass(typeof(RoofBase))
        .WhereElementIsNotElementType().ToElements());
    // A ROOF MODELLED IN PLACE is a family instance in the Roofs category, not
    // a RoofBase, so the collector above never sees one. They are gathered so
    // that "every roof in the model" names them rather than skipping them.
    candidates.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
        .OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType().ToElements());
}

Func<double, string> pitchOf = ratio =>
    string.Format(invariant, "{0:0.##} degrees (rise {1:0.###} per 1 of run)",
        Math.Atan(ratio) * degreesPerRadian, ratio);

foreach (var element in candidates)
{
    if (element == null) continue;
    var typeName = doc.GetElement(element.GetTypeId()) is ElementType type ? type.Name : "no type";
    var isRoofCategory = element.Category != null
        && element.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Roofs));

    var footprint = element as FootPrintRoof;
    var extrusion = element as ExtrusionRoof;

    if (footprint != null)
    {
        // ONE ROOF THAT CANNOT BE READ DOES NOT COST THE OTHERS THEIR ANSWER:
        // it is named, and the rest are read.
        ModelCurveArrArray loops = null;
        try
        {
            loops = footprint.GetProfiles();
        }
        catch (Exception unread)
        {
            findings.Add(string.Format(invariant, "Roof by footprint {0} \"{1}\": its footprint could not "
                + "be read - {2}", element.Id, typeName, unread.Message));
            continue;
        }
        if (loops == null || loops.Size == 0)
        {
            findings.Add(string.Format(invariant, "Roof by footprint {0} \"{1}\" has no footprint lines "
                + "to read", element.Id, typeName));
            continue;
        }

        var edges = new List<string>();
        var sloped = 0;
        var total = 0;
        var loopNumber = 0;
        foreach (ModelCurveArray loop in loops)
        {
            loopNumber++;
            if (loop == null) continue;
            var loopLabel = loops.Size > 1 ? string.Format(invariant, "outline {0}, ", loopNumber) : "";
            foreach (ModelCurve edge in loop)
            {
                if (edge == null) continue;
                total++;
                var curve = edge.GeometryCurve;
                var lengthText = curve == null ? "length not read"
                    : string.Format(invariant, "{0:0} mm", curve.Length * MillimetresPerFoot);
                if (footprint.get_DefinesSlope(edge))
                {
                    sloped++;
                    edges.Add(string.Format(invariant, "{0}line {1} ({2}) slopes at {3}",
                        loopLabel, edge.Id, lengthText, pitchOf(footprint.get_SlopeAngle(edge))));
                }
                else
                {
                    edges.Add(string.Format(invariant, "{0}line {1} ({2}) defines no slope",
                        loopLabel, edge.Id, lengthText));
                }
            }
        }
        roofsRead++;
        // NOT "FLAT". A roof by footprint can be sloped by a slope arrow, or
        // shaped by Modify Sub Elements, with no footprint line defining slope,
        // and neither is read here - so no line defining slope is said as that,
        // and never as a flat roof.
        lines.Add(string.Format(invariant, "Roof by footprint {0} \"{1}\": {2} of its {3} footprint lines "
            + "define slope{4}. {5}",
            element.Id, typeName, sloped, total,
            sloped == 0 ? " - no line sets a slope; a slope arrow or shape editing, if the roof has "
                + "either, is not read here" : "",
            string.Join("; ", edges)));
    }
    else if (extrusion != null)
    {
        ModelCurveArray profile = null;
        try
        {
            profile = extrusion.GetProfile();
        }
        catch (Exception unread)
        {
            findings.Add(string.Format(invariant, "Roof by extrusion {0} \"{1}\": its profile could not "
                + "be read - {2}", element.Id, typeName, unread.Message));
            continue;
        }
        if (profile == null || profile.Size == 0)
        {
            findings.Add(string.Format(invariant, "Roof by extrusion {0} \"{1}\" has no profile lines "
                + "to read", element.Id, typeName));
            continue;
        }

        var edges = new List<string>();
        foreach (ModelCurve edge in profile)
        {
            if (edge == null) continue;
            var curve = edge.GeometryCurve;
            if (curve == null)
            {
                edges.Add(string.Format(invariant, "line {0} could not be read", edge.Id));
                continue;
            }
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
    else if (element is RoofBase || isRoofCategory)
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
