// NOT STANDALONE. Assumes `elements` is in scope, and leaves `fittingAreas`,
// `grossAreas`, `partTypes`, `couplings`, `suspect` and `noGeometry` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THE FACES ALONE ARE THE WRONG ANSWER.
//
// Revit closes a fitting's solid with a cap at every connector. Those caps are
// faces, so adding up `Face.Area` counts them - and there is no metal there at
// all, the duct carries on through. The area of each connector's own opening
// is subtracted, which is what turns a closed solid back into a piece of
// sheet.
//
// THE ERROR THIS AVOIDS IS NOT EVEN. On a short elbow the caps are a big
// share of the surface; on a long transition they are nearly none. Leaving
// them in therefore skews the MIX between fitting kinds rather than inflating
// the total by a constant - and a skewed mix survives the sense-check that a
// uniformly wrong total would fail.
//
// GEOMETRY IS ASKED FOR AT FINE DETAIL ON PURPOSE.
//
// At coarse detail a fitting is drawn as a stick, and a stick has almost no
// surface. The detail level here decides the answer, so it is stated rather
// than inherited from whatever view happens to be open.
//
// NESTED GEOMETRY IS WALKED, NOT ASSUMED AWAY.
//
// A family instance usually hands back a GeometryInstance rather than solids
// directly, and a nested family hands back another one inside that. The walk
// below follows them to any depth; stopping at the first level finds no solids
// at all on most real fittings and reports them as having no geometry.

var fittingAreas = new Dictionary<ElementId, double>();
var grossAreas = new Dictionary<ElementId, double>();
var partTypes = new Dictionary<ElementId, string>();
var couplings = new List<ElementId>();
var suspect = new List<ElementId>();
var noGeometry = new List<ElementId>();

var geometryOptions = new Options();
geometryOptions.ComputeReferences = false;
geometryOptions.IncludeNonVisibleObjects = false;
geometryOptions.DetailLevel = ViewDetailLevel.Fine;

foreach (var element in elements)
{
    if (element == null) continue;

    string partType = "";
    var instance = element as FamilyInstance;
    if (instance != null)
    {
        try
        {
            var fitting = instance.MEPModel as MechanicalFitting;
            if (fitting != null) partType = fitting.PartType.ToString();
        }
        catch { }
    }
    partTypes[element.Id] = partType;

    // A coupling carries no sheet of its own. It is NAMED rather than dropped:
    // whether it belongs in a sheet-metal total is the estimator's decision,
    // and a fragment that silently removed elements would make the count
    // disagree with the set it was given.
    if (partType == "Union") couplings.Add(element.Id);

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
                    try { real = !solid.Faces.IsEmpty && solid.Volume > 0; } catch { }
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

    if (solids.Count == 0) { noGeometry.Add(element.Id); continue; }

    double gross = 0;
    foreach (var solid in solids)
    {
        try { foreach (Face face in solid.Faces) gross += face.Area; }
        catch { }
    }

    double openings = 0;
    ConnectorManager manager = null;
    if (instance != null && instance.MEPModel != null)
    {
        try { manager = instance.MEPModel.ConnectorManager; } catch { }
    }
    if (manager == null)
    {
        var curve = element as MEPCurve;
        if (curve != null) { try { manager = curve.ConnectorManager; } catch { } }
    }

    if (manager != null)
    {
        ConnectorSet set = null;
        try { set = manager.Connectors; } catch { }
        if (set != null)
        {
            foreach (Connector connector in set)
            {
                if (connector == null) continue;
                try
                {
                    // An oval connector has no simple opening area, so nothing
                    // is subtracted for it rather than a guess being made. The
                    // fitting then reads high, and the suspect test below is
                    // what stops that being invisible.
                    if (connector.Shape == ConnectorProfileType.Round)
                        openings += Math.PI * connector.Radius * connector.Radius;
                    else if (connector.Shape == ConnectorProfileType.Rectangular)
                        openings += connector.Width * connector.Height;
                }
                catch { }
            }
        }
    }

    double net = gross - openings;
    grossAreas[element.Id] = gross;

    if (net <= 0 || openings > gross)
    {
        // The gross figure is kept rather than a negative or a zero: it is
        // wrong in a known direction and by a knowable amount, which is
        // something an estimator can work with. A zero is not.
        suspect.Add(element.Id);
        fittingAreas[element.Id] = gross;
    }
    else
        fittingAreas[element.Id] = net;
}
