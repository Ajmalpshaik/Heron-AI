// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `perimeterOf`, `totalMm`, `unmeasurable` and `findings`.
//
// A READ. It opens no transaction and needs none.
//
// A ROOM'S PERIMETER IS REVIT'S OWN NUMBER. ROOM_PERIMETER is computed against
// the same boundary rules the area uses; summing boundary segments instead
// disagrees with the room schedule on the same sheet wherever the boundary
// location is not at the wall centre.
//
// EVERYTHING ELSE IS THE LARGEST HORIZONTAL FACE. Holes are NOT added - somebody
// asking for a slab's perimeter wants the outside - and the number skipped is
// reported rather than silently dropped.
//
// 304.8 APPEARS ONCE, at the report. Revit's internal unit is the foot and the
// answer speaks millimetres (D-20).

const double MillimetresPerFoot = 304.8;

var perimeterOf = new Dictionary<ElementId, double>();
var totalMm = 0.0;
var unmeasurable = new List<ElementId>();
var findings = new List<string>();

var holesSkipped = 0;
var fromParameter = 0;
var fromGeometry = 0;

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given, so there is nothing to measure");
}
else
{
    var options = new Options();
    options.ComputeReferences = false;
    options.IncludeNonVisibleObjects = false;

    foreach (var element in elements)
    {
        if (element == null) continue;

        // A ROOM ANSWERS FOR ITSELF - see the header.
        var parameter = element.get_Parameter(BuiltInParameter.ROOM_PERIMETER);
        if (parameter != null && parameter.HasValue)
        {
            var feet = parameter.AsDouble();
            if (feet > 0)
            {
                var mm = feet * MillimetresPerFoot;
                perimeterOf[element.Id] = mm;
                totalMm += mm;
                fromParameter++;
                continue;
            }
        }

        // EVERYTHING ELSE: round the largest horizontal face.
        double best = 0;
        var holesHere = 0;

        try
        {
            var geometry = element.get_Geometry(options);
            if (geometry != null)
            {
                foreach (var item in geometry)
                {
                    var solid = item as Solid;
                    if (solid == null || solid.Faces == null) continue;

                    foreach (Face face in solid.Faces)
                    {
                        var planar = face as PlanarFace;
                        if (planar == null) continue;
                        // Horizontal only - a wall's side faces are not a
                        // footprint and would each answer a different question.
                        if (Math.Abs(Math.Abs(planar.FaceNormal.Z) - 1.0) > 1e-6) continue;

                        var loops = face.GetEdgesAsCurveLoops();
                        if (loops == null || loops.Count == 0) continue;

                        // THE OUTER LOOP IS THE LONGEST. The rest are holes and
                        // are counted rather than added.
                        double longest = 0;
                        foreach (var loop in loops)
                        {
                            double round = 0;
                            foreach (var curve in loop) round += curve.Length;
                            if (round > longest) longest = round;
                        }
                        if (loops.Count > 1) holesHere += loops.Count - 1;

                        if (longest > best) best = longest;
                    }
                }
            }
        }
        catch
        {
        }

        if (best > 0)
        {
            var mm = best * MillimetresPerFoot;
            perimeterOf[element.Id] = mm;
            totalMm += mm;
            fromGeometry++;
            holesSkipped += holesHere;
        }
        else
        {
            unmeasurable.Add(element.Id);
        }
    }

    findings.Add(string.Format(
        "{0} measured - {1} from Revit's own room perimeter, {2} round their "
        + "largest horizontal face - totalling {3:F0} mm. {4} had no perimeter "
        + "parameter and no horizontal face",
        perimeterOf.Count, fromParameter, fromGeometry, totalMm, unmeasurable.Count));

    if (holesSkipped > 0)
    {
        findings.Add(string.Format(
            "{0} hole(s) in those faces were NOT added - the perimeter reported "
            + "is the outside edge, which is what skirting and kerb are measured "
            + "in", holesSkipped));
    }
}
