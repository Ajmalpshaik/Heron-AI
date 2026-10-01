// NOT STANDALONE. Assumes `doc`, `uidoc`, `form`, `parameterName` and `diameter`
// are in scope; leaves `labelled`, `valueMm`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A ROUND FORM'S SIZE TIED TO A PARAMETER - a radial or a diameter dimension on
// one of its round edges, labelled with a length parameter of the family.
//
// IN A FAMILY THE CALLS EXIST ON EVERY RELEASE. NewRadialDimension and
// NewDiameterDimension belong to the family editor's creation object and read
// the same 2020 to 2027 in the reference assemblies. The "a radius cannot be
// dimensioned before 2025" that create-radial-dimension rests on is about a
// PROJECT, which cannot reach that object at all.
//
// WHAT IS NOT KNOWN IS WHETHER THE LABEL DRIVES THE SHAPE. By hand, a circle's
// size is labelled inside its sketch; through the API the sketch cannot be
// edited in a family on any release - SketchEditScope refuses a document that
// is not a project, and refuses to start inside a transaction. So the label
// goes on the finished form's EDGE, where Revit's remarks say a label may
// stand, and whether moving the parameter then moves the circle is what
// FLEX_FAMILY measures. Revit refusing the label is reported in its own words.
//
// THE PARAMETER MUST ALREADY HOLD THE SIZE. A label moves the geometry to the
// parameter's value - a new length reads zero - so the parameter is compared
// with the edge first, and a difference refuses the call, as LABEL_FAMILY_
// DIMENSION does for planes. The edge labelled is the one whose radius matches
// the parameter, in the view that sees it as a circle, nearest the viewer.
//
// ONE LABEL PER PARAMETER. A second dimension driving it fights the first.
//
// ALL OR NOTHING, READ BACK after Revit regenerates: the label, and the value
// the dimension reads, must be the parameter's. Anything else fails the call.

var findings = new List<string>();
var labelled = "";
var valueMm = 0.0;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY - from 2021 a
// kind is a ForgeTypeId compared by name; before that an enum.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

// Is this parameter a length? Read the way its own release allows: the data
// type from 2022, the parameter type before.
Func<FamilyParameter, bool> isLength = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Length", flags);
            var length = property == null ? null : property.GetValue(null);
            return sameKind(spec, length);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Length";
    }
    catch (Exception)
    {
        return false;
    }
};

GenericForm target = null;
FamilyParameter parameter = null;
double wantedRadius = 0;
Edge edge = null;
Arc arc = null;
View view = null;
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family dimension is "
        + "labelled inside the family - open it first.";
}
else
{
    var said = (form ?? "").Trim();
    Element chosen = null;
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the form by its id.");
        else
        {
            var ids = uidoc.Selection.GetElementIds().ToList();
            if (ids.Count != 1) problems.Add(ids.Count + " elements are selected. Select exactly one form.");
            else chosen = doc.GetElement(ids[0]);
        }
    }
    else if (said.Length == 0) problems.Add("No form was named.");
    else
    {
        chosen = doc.GetElement(said);
        if (chosen == null) problems.Add("No element in this family has the id \"" + said + "\".");
    }
    if (chosen != null)
    {
        target = chosen as GenericForm;
        if (target == null)
            problems.Add("\"" + (chosen.Name ?? said) + "\" is not a form - an extrusion, revolve, blend, sweep or "
                + "swept blend.");
    }

    var fm = doc.FamilyManager;
    var wantedParameter = (parameterName ?? "").Trim();
    parameter = wantedParameter.Length == 0 ? null : fm.get_Parameter(wantedParameter);
    if (wantedParameter.Length == 0) problems.Add("No parameter was named to label the size with.");
    else if (parameter == null)
        problems.Add("\"" + wantedParameter + "\" is not a parameter of this family - ADD_FAMILY_PARAMETERS makes one.");
    else if (!isLength(parameter)) problems.Add("\"" + wantedParameter + "\" is not a length, and only a length can "
        + "label a radius or a diameter.");
    else if (parameter.IsReporting) problems.Add("\"" + wantedParameter + "\" is a reporting parameter - it would read "
        + "the size, not drive it.");
    else if (fm.CurrentType == null)
        problems.Add("This family has no type yet, so \"" + wantedParameter + "\" holds no value and a label would "
            + "drive the circle to nothing. SET_FAMILY_TYPE_VALUES makes the first type.");
    else
    {
        var holds = fm.CurrentType.AsDouble(parameter);
        if (!holds.HasValue || holds.Value <= 0)
            problems.Add("\"" + wantedParameter + "\" holds " + (holds.HasValue ? mm(holds.Value) + " mm" : "no value")
                + " in type \"" + fm.CurrentType.Name + "\". Set it to the form's " + (diameter ? "diameter" : "radius")
                + " first with SET_FAMILY_TYPE_VALUES - a label moves the circle to the parameter's value.");
        else wantedRadius = diameter ? holds.Value / 2 : holds.Value;

        foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
        {
            FamilyParameter label = null;
            try { label = d.FamilyLabel; } catch (Exception) { }
            if (label != null && label.Id == parameter.Id)
            {
                problems.Add("\"" + wantedParameter + "\" already labels a dimension in this family. A second label "
                    + "on it would fight the first as soon as anything moves.");
                break;
            }
        }
    }

    // THE EDGE: round, of the radius the parameter holds, seen as a circle in a
    // plan or an elevation, nearest the viewer.
    if (problems.Count == 0)
    {
        var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
            .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.Elevation))
            .ToList();
        var radii = new List<double>();
        var candidates = new List<Tuple<Edge, Arc, View>>();
        var geometry = target.get_Geometry(new Options { ComputeReferences = true });
        if (geometry != null)
            foreach (GeometryObject piece in geometry)
            {
                var body = piece as Solid;
                if (body == null) continue;
                foreach (Edge e in body.Edges)
                {
                    var round = e.AsCurve() as Arc;
                    if (round == null) continue;
                    radii.Add(round.Radius);
                    if (Math.Abs(round.Radius - wantedRadius) > halfMillimetre || e.Reference == null) continue;
                    var facing = axisOf(round.Normal);
                    if (facing < 0) continue;
                    var seen = views.FirstOrDefault(v => axisOf(v.ViewDirection) == facing
                        && (facing == 2 ? v.ViewType == ViewType.FloorPlan : v.ViewType == ViewType.Elevation));
                    if (seen != null) candidates.Add(Tuple.Create(e, round, seen));
                }
            }

        if (candidates.Count == 0)
        {
            var found = radii.Select(r => mm(r)).Distinct().OrderBy(r => r).ToList();
            problems.Add("No round edge of the form has a radius of " + mm(wantedRadius) + " mm"
                + (diameter ? " (a diameter of " + mm(wantedRadius * 2) + " mm)" : "") + " seen as a circle in a plan "
                + "or an elevation of this family. "
                + (found.Count == 0 ? "The form has no round edges at all."
                    : "Its round edges have radii of " + string.Join(", ", found) + " mm - set the parameter to one "
                        + "of those first."));
        }
        else
        {
            var best = candidates.OrderByDescending(c => c.Item2.Center.DotProduct(c.Item3.ViewDirection)).First();
            edge = best.Item1;
            arc = best.Item2;
            view = best.Item3;
        }
    }

    if (problems.Count > 0) refused = "Nothing was labelled. " + string.Join(" ", problems);
}

if (refused == null)
{
    Dimension dimension;
    var where = arc.Evaluate(0.5, true);
    try
    {
        dimension = diameter
            ? doc.FamilyCreate.NewDiameterDimension(view, edge.Reference, where)
            : doc.FamilyCreate.NewRadialDimension(view, edge.Reference, where);
        dimension.FamilyLabel = parameter;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to label the " + (diameter ? "diameter" : "radius")
            + " of the form's round edge (radius " + mm(arc.Radius) + " mm) with \"" + parameter.Definition.Name
            + "\": " + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    var readLabel = dimension.FamilyLabel;
    var reads = dimension.Value;
    var holds = doc.FamilyManager.CurrentType.AsDouble(parameter);
    if (readLabel == null || readLabel.Id != parameter.Id)
        throw new InvalidOperationException("The dimension was made and does not carry \"" + parameter.Definition.Name
            + "\" afterwards. NOTHING from this call was kept.");
    if (!reads.HasValue || !holds.HasValue || Math.Abs(reads.Value - holds.Value) > halfMillimetre)
        throw new InvalidOperationException("The labelled dimension reads "
            + (reads.HasValue ? mm(reads.Value) + " mm" : "nothing") + " and \"" + parameter.Definition.Name
            + "\" holds " + (holds.HasValue ? mm(holds.Value) + " mm" : "nothing") + ". NOTHING from this call was kept.");

    valueMm = Math.Round(reads.Value * 304.8, 2);
    labelled = "The " + (diameter ? "diameter" : "radius") + " of the form's round edge centred at X "
        + mm(arc.Center.X) + ", Y " + mm(arc.Center.Y) + ", Z " + mm(arc.Center.Z) + " mm, labelled \""
        + parameter.Definition.Name + "\" in \"" + view.Name + "\", reading " + mm(reads.Value) + " mm.";
    findings.Add(labelled);
    findings.Add("Labelled is not yet PROVED to drive the circle. FLEX_FAMILY changes \"" + parameter.Definition.Name
        + "\", reads the form back, and puts it back - and that is the first time anyone will know whether a label on "
        + "a form's round EDGE drives its sketch on this release.");
}

if (refused != null) findings.Add(refused);
