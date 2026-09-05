// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `preferTopFace` and
// `leaderOffsetMm` are in scope; leaves `placed` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHICH FACE IS PICKED IS THE ANSWER, AND REVIT OFFERS THE WRONG ONE FIRST. A
// floor hands back its BOTTOM face before its top, so first-planar-face-wins
// annotates the SOFFIT and reports success. Found by running it: a 300 mm slab
// with its top at 0 was annotated -300.0 and reported as one placed.
//
// THE TEST MUST BE POSITIVE. Guarding on the ABSOLUTE value of the normal's Z
// does not catch it - the soffit's normal is -1 and its absolute value passes.
// The normal has to point UP.
//
// SO EVERY CANDIDATE FACE IS COLLECTED FIRST, never first-one-wins, and the
// height actually annotated is printed per element so a wrong face cannot hide
// behind a success count.

const double MillimetresPerFoot = 304.8;

var placed = 0;
var findings = new List<string>();

if (view == null || view.IsTemplate)
{
    findings.Add("A real view is needed to draw in - a template is not one, and it has no drawing "
        + "to put an annotation on");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given to annotate");
}
else
{
    var offset = (leaderOffsetMm <= 0 ? 300.0 : leaderOffsetMm) / MillimetresPerFoot;

    var options = new Options();
    options.ComputeReferences = true;
    options.DetailLevel = ViewDetailLevel.Fine;

    var skipped = 0;
    var fellBack = 0;
    var notes = new List<string>();

    foreach (var element in elements)
    {
        if (element == null) continue;

        Reference wantedFace = null;
        XYZ wantedPoint = null;
        var fallback = false;

        try
        {
            // EVERY candidate first. Never the first one that answers.
            var faceRefs = new List<Reference>();
            var facePoints = new List<XYZ>();
            var faceNormals = new List<double>();

            var geometry = element.get_Geometry(options);
            if (geometry != null)
            {
                foreach (GeometryObject top in geometry)
                {
                    var solid = top as Solid;
                    if (solid == null)
                    {
                        var nested = top as GeometryInstance;
                        if (nested != null)
                        {
                            foreach (GeometryObject inner in nested.GetInstanceGeometry())
                            {
                                var innerSolid = inner as Solid;
                                if (innerSolid != null && innerSolid.Faces.Size > 0) { solid = innerSolid; break; }
                            }
                        }
                    }
                    if (solid == null || solid.Faces.Size == 0) continue;

                    foreach (Face face in solid.Faces)
                    {
                        if (face.Reference == null) continue;
                        var planar = face as PlanarFace;
                        if (planar == null) continue;

                        var box = face.GetBoundingBox();
                        var middle = (box.Min + box.Max) / 2.0;

                        faceRefs.Add(face.Reference);
                        facePoints.Add(face.Evaluate(middle));
                        faceNormals.Add(planar.FaceNormal.Z);
                    }
                }
            }

            var best = -1;
            for (var i = 0; i < faceRefs.Count; i++)
            {
                // POSITIVE test. Math.Abs would let the soffit through.
                var wanted = preferTopFace ? faceNormals[i] > 0.9 : faceNormals[i] < -0.9;
                if (!wanted) continue;

                if (best < 0) { best = i; continue; }
                var better = preferTopFace
                    ? facePoints[i].Z > facePoints[best].Z
                    : facePoints[i].Z < facePoints[best].Z;
                if (better) best = i;
            }

            if (best < 0 && faceRefs.Count > 0)
            {
                best = 0;
                fallback = true;
            }

            if (best >= 0)
            {
                wantedFace = faceRefs[best];
                wantedPoint = facePoints[best];
            }
        }
        catch
        {
            wantedFace = null;
        }

        if (wantedFace == null || wantedPoint == null)
        {
            skipped++;
            notes.Add(string.Format("id {0}: no usable face reference", element.Id));
            continue;
        }

        try
        {
            var bend = wantedPoint + new XYZ(offset, offset, 0);
            var end = bend + new XYZ(offset, 0, 0);

            doc.Create.NewSpotElevation(view, wantedFace, wantedPoint, bend, end, wantedPoint, true);
            placed++;

            if (fallback) fellBack++;

            notes.Add(string.Format("id {0}: annotated {1:0.#} mm{2}",
                element.Id,
                wantedPoint.Z * MillimetresPerFoot,
                fallback ? " - NO face pointing the way asked for, so the first planar face was used" : ""));
        }
        catch (Exception ex)
        {
            skipped++;
            notes.Add(string.Format("id {0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} spot elevation(s) placed in view '{1}', {2} skipped, using the {3} "
        + "face of each element",
        placed, view.Name, skipped, preferTopFace ? "TOP" : "BOTTOM"));

    foreach (var note in notes) findings.Add(note);

    if (fellBack > 0)
        findings.Add(string.Format("{0} element(s) had NO face pointing the way asked for and were "
            + "annotated on whatever face came first. Read those heights before the drawing goes out",
            fellBack));

    if (placed > 0)
        findings.Add("The height annotated on each element is printed above ON PURPOSE. Revit hands "
            + "back a floor's soffit before its top, so a wrong face gives a plausible number three "
            + "hundred millimetres out and a clean success count");
}
