// NOT STANDALONE. Assumes `doc`, `uidoc`, `form`, `parameterName` and `diameter`
// are in scope; leaves `labelled`, `valueMm`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A ROUND FORM'S SIZE TIED TO A PARAMETER - a dimension INSIDE THE FORM'S
// SKETCH, labelled with a length parameter of the family, which is where a
// modeller puts it by hand with Edit Extrusion or Edit Revolve.
//
// THE LABEL GOES ON THE SKETCH, NEVER ON THE FINISHED EDGE. Version 1 labelled
// the round EDGE of the finished form. Revit accepted the label and the circle
// did not follow it: on 2026-10-03 a diameter label on a stem's edge in Family1
// (Revit 2024) read 8 mm while its parameter held 14, and FLEX_FAMILY refused
// (NEEDS-CHECKING BM3, row 5b-305). The same evening a dimension made on the
// SKETCH's own curves, outside sketch mode, went into the sketch and drove the
// form at 300 and at 40 mm - on a cylinder made in that run and on one made
// days before. A sketch curve's reference is what the dimension takes; no
// sketch-editing scope is needed, which matters because SketchEditScope
// refuses a family document on every release.
//
// TWO SHAPES OF ROUND SIZE.
//   An EXTRUSION's circle: a radial or diameter dimension on one of the
//   sketch's arcs of that radius. One half of a split circle drives both.
//   A REVOLVE's radius: a straight distance, in the sketch's plane, from a
//   REFERENCE PLANE the axis lies on to the profile line parallel to the axis
//   at that radius - and the axis is locked to that plane. Measured from the
//   revolve's own axis line instead, Revit moved the AXIS at the first flex
//   and the solid broke at the second; from a plane it held at 300, 40 and 50.
//   So a revolve whose axis lies on no reference plane is refused, saying so.
//   A revolve's dimension is a radius: a diameter parameter is refused with
//   the formula that gives a radius from it.
//
// THE PARAMETER MUST ALREADY HOLD THE SIZE. A label moves the geometry to the
// parameter's value - a new length reads zero - so the sketch is searched for
// the curve whose size the parameter already holds, and a form with none is
// refused, listing the sizes it has.
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
double wanted = 0;
View view = null;
// An extrusion's circle.
Arc arc = null;
// A revolve's radius: the profile line, the plane the axis lies on, and the
// line the dimension is drawn along.
Curve sideLine = null;
ReferencePlane axisPlane = null;
Line along = null;
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
        else if (!(target is Extrusion) && !(target is Revolution))
            problems.Add("\"" + (chosen.Name ?? said) + "\" is a " + target.GetType().Name.ToLowerInvariant() + ". A round "
                + "size is labelled on an extrusion's circle or a revolve's radius; for any other form, label it by hand "
                + "in the form's sketch.");
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
        else wanted = diameter ? holds.Value / 2 : holds.Value;

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

    if (problems.Count == 0 && target is Revolution && diameter)
        problems.Add("A revolve's round size is the distance from its axis - a RADIUS. Label it with a length "
            + "parameter that holds the radius, given the formula \"" + wantedParameter + " / 2\", and keep \""
            + wantedParameter + "\" as the size a type sets.");

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.Elevation))
        .ToList();
    Func<XYZ, View> viewFacing = normal =>
    {
        var facing = axisOf(normal);
        if (facing < 0) return null;
        return views.FirstOrDefault(v => axisOf(v.ViewDirection) == facing
            && (facing == 2 ? v.ViewType == ViewType.FloorPlan : v.ViewType == ViewType.Elevation));
    };

    // AN EXTRUSION: a sketch arc of the radius the parameter holds.
    if (problems.Count == 0 && target is Extrusion)
    {
        var radii = new List<double>();
        foreach (CurveArray loop in ((Extrusion)target).Sketch.Profile)
            foreach (Curve curve in loop)
            {
                var round = curve as Arc;
                if (round == null) continue;
                radii.Add(round.Radius);
                if (arc != null || Math.Abs(round.Radius - wanted) > halfMillimetre || round.Reference == null) continue;
                var seen = viewFacing(round.Normal);
                if (seen == null) continue;
                arc = round;
                view = seen;
            }
        if (arc == null)
        {
            var found = radii.Select(r => mm(r)).Distinct().OrderBy(r => r).ToList();
            problems.Add("No circle in the extrusion's sketch has a radius of " + mm(wanted) + " mm"
                + (diameter ? " (a diameter of " + mm(wanted * 2) + " mm)" : "") + " on a plane a plan or an "
                + "elevation of this family looks at. "
                + (found.Count == 0 ? "Its sketch has no arcs at all."
                    : "Its sketch's arcs have radii of " + string.Join(", ", found) + " mm - set the parameter to one "
                        + "of those first."));
        }
    }

    // A REVOLVE: the profile line parallel to the axis at the radius held, and
    // a reference plane the axis lies on to measure it from.
    if (problems.Count == 0 && target is Revolution)
    {
        var revolve = (Revolution)target;
        var sketchNormal = revolve.Sketch.SketchPlane.GetPlane().Normal;
        var axisLine = revolve.Axis == null ? null : revolve.Axis.GeometryCurve as Line;
        if (axisLine == null) problems.Add("The revolve's axis could not be read as a straight line.");
        else
        {
            var axisDirection = axisLine.Direction.Normalize();
            var axisPoint = axisLine.GetEndPoint(0);
            Func<XYZ, double> distanceFromAxis = point =>
            {
                var offset = point - axisPoint;
                return (offset - axisDirection.Multiply(offset.DotProduct(axisDirection))).GetLength();
            };
            var distances = new List<double>();
            var matches = new List<Curve>();
            foreach (CurveArray loop in revolve.Sketch.Profile)
                foreach (Curve curve in loop)
                {
                    var straight = curve as Line;
                    if (straight == null || Math.Abs(Math.Abs(straight.Direction.DotProduct(axisDirection)) - 1) > 1e-6) continue;
                    var distance = distanceFromAxis(straight.GetEndPoint(0));
                    distances.Add(distance);
                    if (Math.Abs(distance - wanted) <= halfMillimetre && straight.Reference != null) matches.Add(straight);
                }
            if (matches.Count != 1)
            {
                var found = distances.Select(r => mm(r)).Distinct().OrderBy(r => r).ToList();
                problems.Add(matches.Count > 1
                    ? matches.Count + " lines of the revolve's profile lie " + mm(wanted) + " mm from its axis. Only one "
                        + "can be labelled, and the others would not follow - join them into one line by hand first."
                    : "No line of the revolve's profile runs alongside its axis " + mm(wanted) + " mm from it. "
                        + (found.Count == 0 ? "The profile has no straight line parallel to the axis."
                            : "Its lines alongside the axis lie " + string.Join(", ", found) + " mm from it - set the "
                                + "parameter to one of those first."));
            }
            else sideLine = matches[0];

            // THE PLANE THE AXIS LIES ON, square to the sketch.
            if (problems.Count == 0)
            {
                foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane))
                    .Cast<ReferencePlane>())
                {
                    var plane = candidate.GetPlane();
                    if (Math.Abs(plane.Normal.DotProduct(axisDirection)) > 1e-6) continue;
                    if (Math.Abs(plane.Normal.DotProduct(sketchNormal)) > 1e-6) continue;
                    if (Math.Abs(plane.Normal.DotProduct(axisPoint - plane.Origin)) > halfMillimetre / 10) continue;
                    axisPlane = candidate;
                    break;
                }
                if (axisPlane == null)
                    problems.Add("No reference plane runs through the revolve's axis. Measured from the axis line "
                        + "itself, Revit moves the axis when the size changes and the revolve breaks - make a reference "
                        + "plane through the axis first (CREATE_REFERENCE_PLANES), then label.");
            }
            if (problems.Count == 0)
            {
                view = viewFacing(sketchNormal);
                if (view == null)
                    problems.Add("No plan or elevation of this family looks straight at the revolve's sketch, so its "
                        + "radius cannot be dimensioned.");
                else
                {
                    var middle = sideLine.Evaluate(0.5, true);
                    var offset = middle - axisPoint;
                    var foot = axisPoint + axisDirection.Multiply(offset.DotProduct(axisDirection));
                    along = Line.CreateBound(foot, middle);
                }
            }
        }
    }

    if (problems.Count > 0) refused = "Nothing was labelled. " + string.Join(" ", problems);
}

if (refused == null)
{
    Dimension dimension;
    var what = arc != null ? (diameter ? "diameter" : "radius") + " of the extrusion's sketched circle (radius "
            + mm(arc.Radius) + " mm)"
        : "radius of the revolve, from \"" + axisPlane.Name + "\" to its profile line " + mm(along.Length) + " mm out";
    var lockNote = "";
    try
    {
        if (arc != null)
            dimension = diameter
                ? doc.FamilyCreate.NewDiameterDimension(view, arc.Reference, arc.Evaluate(0.5, true))
                : doc.FamilyCreate.NewRadialDimension(view, arc.Reference, arc.Evaluate(0.5, true));
        else
        {
            // The axis is locked to the plane first, so the plane holds it while
            // the profile line moves.
            try
            {
                var alignment = doc.FamilyCreate.NewAlignment(view, axisPlane.GetReference(),
                    ((Revolution)target).Axis.GeometryCurve.Reference);
                alignment.IsLocked = true;
                lockNote = " Its axis is locked to \"" + axisPlane.Name + "\".";
            }
            catch (Exception lockFailure)
            {
                lockNote = " Its axis was NOT locked to \"" + axisPlane.Name + "\" (" + lockFailure.Message + ") - the "
                    + "plane still holds the dimension, which is what kept the axis in place when this was measured.";
            }
            var references = new ReferenceArray();
            references.Append(axisPlane.GetReference());
            references.Append(sideLine.Reference);
            dimension = doc.FamilyCreate.NewDimension(view, along, references);
        }
        dimension.FamilyLabel = parameter;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to label the " + what + " with \"" + parameter.Definition.Name
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
    labelled = "The " + what + ", labelled \"" + parameter.Definition.Name + "\" inside the form's sketch, reading "
        + mm(reads.Value) + " mm." + lockNote;
    findings.Add(labelled);
    findings.Add("The dimension is in the sketch, where Edit " + (arc != null ? "Extrusion" : "Revolve") + " shows it. "
        + "FLEX_FAMILY with \"" + parameter.Definition.Name + "\" at two different sizes is what shows the form following.");
}

if (refused != null) findings.Add(refused);
