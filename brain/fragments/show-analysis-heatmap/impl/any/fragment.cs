// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `values` and
// `measurementName` are in scope, and leaves `painted`, `noFaces`, `noValue`,
// `allOneValue` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// FIVE WAYS THIS SHOWS NOTHING, AND NONE OF THEM THROWS.
//
//   the view is not using the style   creating a display style is not enough;
//                                     the view's own parameter has to point at
//                                     it, or the data is stored, no legend
//                                     appears, and the model looks untouched
//   the schema id has gone stale      the id handed back by registering a
//                                     result is only valid while that schema
//                                     is registered on this view's manager.
//                                     Clear the view and it is gone, so it is
//                                     re-checked against what IS registered
//                                     rather than remembered
//   the element has no faces          the paint attaches to a FACE. Anything
//                                     the view draws as a line contributes
//                                     nothing, which is a fact about the view
//                                     and not a failure
//   it is a plan                      the faces painted are the ones you look
//                                     down at - for a pipe, the top of a
//                                     cylinder. A 3D view is what this is for
//   every value is the same           one colour over everything, which reads
//                                     as broken. Reported as its own state
//
// ONE PRIMITIVE HOLDS ABOUT A THOUSAND POINTS. This paints two per face, so
// the limit is nowhere near - but anything that ever paints point clouds
// through this has to split them across primitives.

int painted = 0;
var noFaces = new List<ElementId>();
var noValue = new List<ElementId>();
bool allOneValue = false;
string refused = "";

var manager = SpatialFieldManager.GetSpatialFieldManager(view);
if (manager == null)
{
    try { manager = SpatialFieldManager.CreateSpatialFieldManager(view, 1); }
    catch { refused = "This view will not take an analysis display - a 3D view is what this is for."; }
}

if (manager != null)
{
    // The schema, re-checked rather than remembered.
    int schemaIndex = -1;
    string schemaName = string.IsNullOrEmpty(measurementName) ? "Value" : measurementName;

    try
    {
        foreach (var registered in manager.GetRegisteredResults())
        {
            var existing = manager.GetResultSchema(registered);
            if (existing != null && existing.Name == schemaName) { schemaIndex = registered; break; }
        }
    }
    catch { }

    if (schemaIndex < 0)
    {
        try
        {
            var schema = new AnalysisResultSchema(schemaName, schemaName);
            schemaIndex = manager.RegisterResult(schema);
        }
        catch { refused = "The result schema could not be registered, so nothing can be painted."; }
    }

    if (refused.Length == 0)
    {
        // Whether the numbers vary at all. One colour over everything reads as
        // a broken tool rather than as a flat dataset.
        double smallest = double.MaxValue, largest = double.MinValue;
        foreach (var entry in values)
        {
            if (entry.Value < smallest) smallest = entry.Value;
            if (entry.Value > largest) largest = entry.Value;
        }
        allOneValue = values.Count > 0 && Math.Abs(largest - smallest) < 1e-9;

        var geometryOptions = new Options();
        geometryOptions.ComputeReferences = false;
        geometryOptions.IncludeNonVisibleObjects = false;
        geometryOptions.View = view;

        foreach (var element in elements)
        {
            if (element == null) continue;

            if (!values.ContainsKey(element.Id)) { noValue.Add(element.Id); continue; }
            double value = values[element.Id];

            var solids = new List<Solid>();
            GeometryElement geometry = null;
            try { geometry = element.get_Geometry(geometryOptions); } catch { }

            if (geometry != null)
            {
                var pending = new Stack<GeometryElement>();
                pending.Push(geometry);
                while (pending.Count > 0)
                {
                    var current = pending.Pop();
                    foreach (GeometryObject item in current)
                    {
                        var solid = item as Solid;
                        if (solid != null)
                        {
                            bool real = false;
                            try { real = solid.Faces.Size > 0 && solid.Volume > 0; } catch { }
                            if (real) solids.Add(solid);
                            continue;
                        }
                        var nested = item as GeometryInstance;
                        if (nested != null)
                        {
                            try { pending.Push(nested.GetInstanceGeometry()); } catch { }
                        }
                    }
                }
            }

            bool paintedAnyFace = false;

            foreach (var solid in solids)
            {
                foreach (Face face in solid.Faces)
                {
                    try
                    {
                        int primitive = manager.AddSpatialFieldPrimitive(face, Transform.Identity);

                        // Two corners of the face's own parameter space, both
                        // carrying the element's value: a flat colour per face,
                        // graded against every other element by the style.
                        var box = face.GetBoundingBox();
                        var points = new List<UV>();
                        points.Add(box.Min);
                        points.Add(box.Max);

                        var readings = new List<ValueAtPoint>();
                        var one = new List<double>();
                        one.Add(value);
                        readings.Add(new ValueAtPoint(one));
                        var two = new List<double>();
                        two.Add(value);
                        readings.Add(new ValueAtPoint(two));

                        manager.UpdateSpatialFieldPrimitive(
                            primitive,
                            new FieldDomainPointsByUV(points),
                            new FieldValues(readings),
                            schemaIndex);

                        paintedAnyFace = true;
                    }
                    catch { }
                }
            }

            if (paintedAnyFace) painted++; else noFaces.Add(element.Id);
        }

        // AND THE STEP THAT MAKES IT VISIBLE. Without this the data is all
        // stored and the view shows nothing at all.
        try
        {
            ElementId styleId = ElementId.InvalidElementId;
            try { styleId = AnalysisDisplayStyle.FindByName(doc, schemaName); } catch { }

            if (styleId == ElementId.InvalidElementId)
            {
                var surface = new AnalysisDisplayColoredSurfaceSettings();
                var colours = new AnalysisDisplayColorSettings();
                colours.ColorSettingsType = AnalysisDisplayStyleColorSettingsType.GradientColor;
                var legend = new AnalysisDisplayLegendSettings();

                var style = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                    doc, schemaName, surface, colours, legend);
                if (style != null) styleId = style.Id;
            }

            if (styleId != ElementId.InvalidElementId)
            {
                var parameter = view.get_Parameter(BuiltInParameter.VIEW_ANALYSIS_DISPLAY_STYLE);
                if (parameter != null && !parameter.IsReadOnly) parameter.Set(styleId);
            }
        }
        catch { }
    }
}
