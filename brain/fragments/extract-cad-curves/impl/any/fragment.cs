// NOT STANDALONE. Assumes `doc`, `imports`, `layerNames`, `asDetailLines`,
// `view` and `maxCurves` are in scope; leaves `created`, `layerCounts` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Golden Rule 17's preview - run,
// look, roll back - is how to see the result before keeping it.
//
// NAMING NO LAYER SURVEYS; NAMING LAYERS EXTRACTS. The empty list creates
// NOTHING and reports what is in the drawing. That is the opposite of the
// obvious default on purpose: layer names cannot be guessed, they are
// case-sensitive, and "all layers" on a survey drawing is tens of thousands of
// lines nobody asked for.
//
// FLAT CURVES ONLY, AND THE REST ARE COUNTED. A model line needs a sketch plane
// and a detail line lives in one view; a curve whose ends differ in height
// belongs to neither. A survey drawing with levels baked in produces almost
// nothing, and that count is the only clue why.
//
// POLYLINES BECOME SEGMENTS; SPLINES AND ELLIPSES ARE REFUSED AND COUNTED.
// A polyline already IS a chain of straight pieces. A spline is not, and
// approximating one quietly would put linework in the model that does not match
// the drawing it came from.

var created = new List<ElementId>();
var layerCounts = new Dictionary<string, int>();
var findings = new List<string>();

var wantedLayers = new List<string>();
if (layerNames != null)
    foreach (var name in layerNames)
        if (!string.IsNullOrEmpty(name)) wantedLayers.Add(name.Trim());

var cap = maxCurves > 0 ? maxCurves : 500;

var byLayer = new Dictionary<string, List<Curve>>();
var importsRead = 0;
var curvedSkipped = 0;

if (imports != null)
{
    foreach (var element in imports)
    {
        var import = element as ImportInstance;
        if (import == null) continue;
        importsRead++;

        var geometry = import.get_Geometry(new Options());
        if (geometry == null) continue;

        foreach (GeometryObject top in geometry)
        {
            var nested = top as GeometryInstance;
            if (nested == null) continue;

            // GetInstanceGeometry, not GetSymbolGeometry: the import's own
            // placement transform is already applied, so the coordinates are
            // where the linework actually sits in this model.
            foreach (GeometryObject item in nested.GetInstanceGeometry())
            {
                var style = doc.GetElement(item.GraphicsStyleId) as GraphicsStyle;
                var layer = "(no layer)";
                if (style != null && style.GraphicsStyleCategory != null)
                    layer = style.GraphicsStyleCategory.Name;

                var pieces = new List<Curve>();

                if (item is Line || item is Arc)
                {
                    pieces.Add((Curve)item);
                }
                else if (item is PolyLine)
                {
                    var points = ((PolyLine)item).GetCoordinates();
                    for (var i = 0; i < points.Count - 1; i++)
                        if (points[i].DistanceTo(points[i + 1]) > 1e-6)
                            pieces.Add(Line.CreateBound(points[i], points[i + 1]));
                }
                else if (item is Curve)
                {
                    curvedSkipped++;   // spline or ellipse - a deliberate refusal
                    continue;
                }

                if (pieces.Count == 0) continue;

                if (!byLayer.ContainsKey(layer)) byLayer[layer] = new List<Curve>();
                byLayer[layer].AddRange(pieces);
            }
        }
    }
}

foreach (var layer in byLayer) layerCounts[layer.Key] = layer.Value.Count;

if (importsRead == 0)
{
    findings.Add("None of what was handed in is an imported or linked CAD file, so there was nothing "
        + "to read. LIST_LINKED_MODELS is what finds them");
}
else
{
    var summary = new List<string>();
    foreach (var layer in byLayer)
        summary.Add(string.Format("'{0}': {1}", layer.Key, layer.Value.Count));

    findings.Add(string.Format("{0} CAD import(s) read. Layers and curve segments: {1}",
        importsRead, summary.Count == 0 ? "none found" : string.Join(", ", summary.ToArray())));

    if (curvedSkipped > 0)
        findings.Add(string.Format("{0} spline or ellipse object(s) were refused rather than "
            + "approximated - linework that does not match the drawing it came from is worse than "
            + "linework that is missing", curvedSkipped));

    if (wantedLayers.Count == 0)
    {
        findings.Add("NO LAYER WAS NAMED, so this was a survey and NOTHING was created. Name the "
            + "layers wanted - exactly as listed above, including case - and run it again");
    }
    else if (asDetailLines && view == null)
    {
        findings.Add("Detail lines live in one view, so the view has to be named. Nothing created");
    }
    else
    {
        var wanted = new List<Curve>();
        var unmatched = new List<string>();

        foreach (var name in wantedLayers)
        {
            if (byLayer.ContainsKey(name)) wanted.AddRange(byLayer[name]);
            else unmatched.Add(name);
        }

        foreach (var name in unmatched)
            findings.Add(string.Format("No layer called '{0}' is in these imports. Layer names are "
                + "case-sensitive and are whatever the other office typed - take one from the list "
                + "above", name));

        var found = wanted.Count;
        var notFlat = 0;
        var madeDetail = asDetailLines;

        foreach (var curve in wanted)
        {
            if (created.Count >= cap) break;

            var first = curve.GetEndPoint(0);
            var second = curve.GetEndPoint(1);
            if (Math.Abs(first.Z - second.Z) > 1e-6) { notFlat++; continue; }

            try
            {
                if (madeDetail)
                {
                    created.Add(doc.Create.NewDetailCurve(view, curve).Id);
                }
                else
                {
                    var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, first);
                    var sketch = SketchPlane.Create(doc, plane);
                    created.Add(doc.Create.NewModelCurve(curve, sketch).Id);
                }
            }
            catch (Exception ex)
            {
                findings.Add(string.Format("One curve could not be redrawn: {0}. The rest continued",
                    ex.Message));
            }
        }

        findings.Add(string.Format("{0} {1} line(s) created from {2} matched segment(s){3}{4}",
            created.Count,
            madeDetail ? "detail" : "model",
            found,
            notFlat == 0 ? "" : string.Format(", {0} skipped as not flat - both ends of a curve must "
                + "be at one height", notFlat),
            found > cap ? string.Format(", CAPPED at {0}", cap) : ""));

        if (created.Count > 0)
            findings.Add("These landed on a default line style, not on anything matching the CAD "
                + "layer they came from. REMAP_LINE_STYLES is what puts them right");
    }
}
