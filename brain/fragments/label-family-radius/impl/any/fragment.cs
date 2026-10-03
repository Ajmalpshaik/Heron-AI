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
//   A distance from the axis is a radius: a diameter parameter is refused with
//   the formula that gives a radius from it.
//   A REVOLVE WHOSE PROFILE IS A CIRCLE - a torus, a handwheel's rim - has two
//   more: the circle's own radius or diameter, labelled on its arc as an
//   extrusion's is, and its CENTRE's distance from the axis, measured from the
//   plane through the axis to the circle's centre point, axis locked as above.
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
// An extrusion's circle, or a revolve's profile circle labelled by its own size.
Arc arc = null;
// A revolve's distance from its axis: the profile line, or the profile circle
// and its centre point, the plane the axis lies on, and the line the dimension
// is drawn along.
Curve sideLine = null;
Arc centreArc = null;
Reference centreReference = null;
// The plane through the profile circle's centre, square to the sketch; made
// when the family has none.
ReferencePlane centrePlane = null;
// A dimension the parameter already labels.
Dimension already = null;
// Does this dimension join exactly these two reference planes?
Func<Dimension, ReferencePlane, ReferencePlane, bool> joins = (d, first, second) =>
{
    try
    {
        var ids = d.References.Cast<Reference>().Select(r => r.ElementId).ToList();
        return ids.Count == 2 && ids.Contains(first.Id) && ids.Contains(second.Id);
    }
    catch (Exception) { return false; }
};
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
                already = d;
                break;
            }
        }
    }

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

    // A REVOLVE: three round sizes, each found by the size the parameter holds.
    //   The profile CIRCLE's own radius or diameter - a torus's tube - takes a
    //   radial or diameter dimension on its arc, as an extrusion's circle does.
    //   The distance from the axis to the profile circle's CENTRE - a torus's
    //   ring - takes a straight dimension from a reference plane the axis lies
    //   on to the circle's centre point, with the axis locked to that plane.
    //   The distance from the axis to a profile LINE alongside it - a bell, a
    //   drum - takes a straight dimension from that plane to the line.
    // A distance from the axis is a radius; only the circle's own size can be
    // a diameter. On 2026-10-03 a torus rim in Family4 was refused both ways by
    // version 2, which knew only the third (row 5b-306).
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
            var lineDistances = new List<double>();
            var circleRadii = new List<double>();
            var centreDistances = new List<double>();
            var lineMatches = new List<Curve>();
            var circleMatches = new List<Arc>();
            var centreMatches = new List<Arc>();
            var centreReferences = new List<Reference>();
            foreach (CurveArray loop in revolve.Sketch.Profile)
                foreach (Curve curve in loop)
                {
                    // The two halves of one circle share a centre and a radius:
                    // one circle, counted once.
                    var round = curve as Arc;
                    if (round != null)
                    {
                        var fromAxis = distanceFromAxis(round.Center);
                        circleRadii.Add(round.Radius);
                        centreDistances.Add(fromAxis);
                        if (round.Reference == null) continue;
                        if (Math.Abs(round.Radius - wanted) <= halfMillimetre
                            && !circleMatches.Any(c => c.Center.IsAlmostEqualTo(round.Center, halfMillimetre)))
                            circleMatches.Add(round);
                        if (!diameter && Math.Abs(fromAxis - wanted) <= halfMillimetre
                            && !centreMatches.Any(c => c.Center.IsAlmostEqualTo(round.Center, halfMillimetre)))
                        {
                            var drawn = doc.GetElement(round.Reference) as CurveElement;
                            Reference centrePoint = null;
                            try { centrePoint = drawn == null ? null : drawn.CenterPointReference; } catch (Exception) { }
                            if (centrePoint == null) continue;
                            centreMatches.Add(round);
                            centreReferences.Add(centrePoint);
                        }
                        continue;
                    }
                    var straight = curve as Line;
                    if (diameter || straight == null
                        || Math.Abs(Math.Abs(straight.Direction.DotProduct(axisDirection)) - 1) > 1e-6) continue;
                    var distance = distanceFromAxis(straight.GetEndPoint(0));
                    lineDistances.Add(distance);
                    if (Math.Abs(distance - wanted) <= halfMillimetre && straight.Reference != null) lineMatches.Add(straight);
                }
            Func<List<double>, string> listed = sizes =>
                string.Join(", ", sizes.Select(r => mm(r)).Distinct().OrderBy(r => double.Parse(r, invariant)));

            var kinds = (circleMatches.Count > 0 ? 1 : 0) + (centreMatches.Count > 0 ? 1 : 0) + (lineMatches.Count > 0 ? 1 : 0);
            if (kinds == 0)
            {
                var sizes = new List<string>();
                if (circleRadii.Count > 0)
                    sizes.Add("its circles have radii of " + listed(circleRadii) + " mm"
                        + (diameter ? "" : " and centres " + listed(centreDistances) + " mm from the axis"));
                if (lineDistances.Count > 0) sizes.Add("its lines alongside the axis lie " + listed(lineDistances) + " mm from it");
                problems.Add((diameter
                        ? "No circle of the revolve's profile is " + mm(wanted * 2) + " mm across. A distance from the axis "
                            + "is a RADIUS - for that, label with a length parameter given the formula \"" + parameter.Definition.Name
                            + " / 2\". "
                        : "Nothing in the revolve's profile is " + mm(wanted) + " mm in size: no circle of that radius, no "
                            + "circle centred that far from the axis, no line alongside the axis that far from it. ")
                    + (sizes.Count == 0 ? "The profile has no circle and no line alongside the axis."
                        : "In the profile, " + string.Join("; ", sizes) + " - set the parameter to one of those first."));
            }
            else if (kinds > 1)
                problems.Add(mm(wanted) + " mm is more than one size of this revolve - "
                    + string.Join(" and ", new[] {
                        circleMatches.Count > 0 ? "a circle's radius" : null,
                        centreMatches.Count > 0 ? "a circle's centre from the axis" : null,
                        lineMatches.Count > 0 ? "a line's distance from the axis" : null }.Where(w => w != null))
                    + ". Only one can be labelled and which one is meant cannot be told - label it by hand in Edit Revolve.");
            else if (circleMatches.Count > 1 || centreMatches.Count > 1 || lineMatches.Count > 1)
                problems.Add(Math.Max(circleMatches.Count, Math.Max(centreMatches.Count, lineMatches.Count)) + " "
                    + (lineMatches.Count > 1 ? "lines of the revolve's profile lie " + mm(wanted) + " mm from its axis"
                        : circleMatches.Count > 1 ? "circles of the revolve's profile have a radius of " + mm(wanted) + " mm"
                        : "circles of the revolve's profile are centred " + mm(wanted) + " mm from its axis")
                    + ". Only one can be labelled, and the others would not follow - "
                    + (lineMatches.Count > 1 ? "join them into one line by hand first." : "label it by hand in Edit Revolve."));
            else if (circleMatches.Count == 1)
            {
                // The circle's own size needs no plane: its centre stays where it is.
                arc = circleMatches[0];
                view = viewFacing(arc.Normal);
                if (view == null)
                    problems.Add("No plan or elevation of this family looks straight at the revolve's sketch, so its "
                        + "circle cannot be dimensioned.");
            }
            else if (centreMatches.Count == 1)
            {
                centreArc = centreMatches[0];
                centreReference = centreReferences[0];
            }
            else sideLine = lineMatches[0];

            // A DISTANCE FROM THE AXIS: the plane the axis lies on, square to the sketch.
            if (problems.Count == 0 && arc == null)
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
                // The circle's own plane, the way a modeller draws a torus by
                // hand: a plane through the circle's centre, parallel to the
                // axis's plane, and the dimension between the two planes.
                if (axisPlane != null && centreArc != null)
                    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane))
                        .Cast<ReferencePlane>())
                    {
                        var plane = candidate.GetPlane();
                        if (candidate.Id == axisPlane.Id) continue;
                        if (Math.Abs(plane.Normal.DotProduct(axisDirection)) > 1e-6) continue;
                        if (Math.Abs(plane.Normal.DotProduct(sketchNormal)) > 1e-6) continue;
                        if (Math.Abs(plane.Normal.DotProduct(centreArc.Center - plane.Origin)) > halfMillimetre / 10) continue;
                        centrePlane = candidate;
                        break;
                    }
                if (axisPlane == null)
                    problems.Add("No reference plane runs through the revolve's axis. Measured from the axis line "
                        + "itself, Revit moves the axis when the size changes and the revolve breaks - make a reference "
                        + "plane through the axis first (CREATE_REFERENCE_PLANES), then label.");
            }
            if (problems.Count == 0 && arc == null)
            {
                view = viewFacing(sketchNormal);
                if (view == null)
                    problems.Add("No plan or elevation of this family looks straight at the revolve's sketch, so its "
                        + "radius cannot be dimensioned.");
                else
                {
                    var middle = centreArc != null ? centreArc.Center : sideLine.Evaluate(0.5, true);
                    var offset = middle - axisPoint;
                    var foot = axisPoint + axisDirection.Multiply(offset.DotProduct(axisDirection));
                    along = Line.CreateBound(foot, middle);
                }
            }
        }
    }

    // ONE LABEL PER PARAMETER. The one exception is the dimension this call
    // would make anyway: a torus whose centre plane is already labelled from
    // the axis's plane, its circle not yet locked to it - Family4's rim on
    // 2026-10-03, built by hand that way before this tool could do it.
    if (problems.Count == 0 && already != null
        && !(centreArc != null && centrePlane != null && joins(already, axisPlane, centrePlane)))
        problems.Add("\"" + parameter.Definition.Name + "\" already labels a dimension in this family. A second label "
            + "on it would fight the first as soon as anything moves.");

    if (problems.Count > 0) refused = "Nothing was labelled. " + string.Join(" ", problems);
}

if (refused == null)
{
    Dimension dimension;
    var what = arc != null ? (diameter ? "diameter" : "radius") + " of the " + (target is Extrusion
            ? "extrusion's sketched circle" : "revolve's profile circle") + " (radius " + mm(arc.Radius) + " mm)"
        : centreArc != null ? "distance of the revolve's profile circle from its axis, from \"" + axisPlane.Name
            + "\" to " + (centrePlane == null ? "a new reference plane" : "\"" + centrePlane.Name + "\"") + " through the "
            + "circle's centre, " + mm(along.Length) + " mm out"
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
            // the profile line, or the circle, moves.
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
            // A TORUS: the circle's centre locked to a plane through it, and
            // that plane dimensioned from the axis's plane. A dimension straight
            // to the centre point is refused by Revit ("Invalid number of
            // references", measured 2026-10-03 on a scratch family); the centre
            // point does take an alignment once its reference is read back
            // from its stable form.
            if (centreArc != null)
            {
                var sketchNormal = ((Revolution)target).Sketch.SketchPlane.GetPlane().Normal;
                if (centrePlane == null)
                {
                    centrePlane = doc.FamilyCreate.NewReferencePlane(centreArc.Center + along.Direction.CrossProduct(sketchNormal),
                        centreArc.Center, sketchNormal, view);
                    var name = parameter.Definition.Name;
                    if (!new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
                        .Any(p => p.Name == name))
                        centrePlane.Name = name;
                    doc.Regenerate();
                    lockNote += " A reference plane" + (centrePlane.Name == name ? " \"" + name + "\"" : "") + " was made "
                        + "through the circle's centre.";
                }
                var centrePoint = Reference.ParseFromStableRepresentation(doc,
                    centreReference.ConvertToStableRepresentation(doc));
                try
                {
                    var held = doc.FamilyCreate.NewAlignment(view, centrePlane.GetReference(), centrePoint);
                    held.IsLocked = true;
                    lockNote += " The circle's centre is locked to \"" + centrePlane.Name + "\".";
                }
                catch (Exception centreFailure)
                {
                    // Run a second time, the lock is already there and Revit
                    // refuses another; the read-back below still checks the
                    // circle sits on the plane.
                    if (already == null) throw;
                    lockNote += " The circle's centre was not locked again (" + centreFailure.Message + ").";
                }
            }
            if (already != null) dimension = already;
            else
            {
                var references = new ReferenceArray();
                references.Append(axisPlane.GetReference());
                references.Append(centrePlane != null ? centrePlane.GetReference() : sideLine.Reference);
                dimension = doc.FamilyCreate.NewDimension(view, along, references);
            }
        }
        if (dimension != already) dimension.FamilyLabel = parameter;
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

    // A TORUS'S CIRCLE MUST SIT ON THE PLANE THE LABEL MOVES.
    if (centreArc != null)
    {
        var onPlane = false;
        var plane = centrePlane.GetPlane();
        foreach (CurveArray loop in ((Revolution)target).Sketch.Profile)
            foreach (Curve curve in loop)
            {
                var round = curve as Arc;
                if (round != null && Math.Abs(round.Radius - centreArc.Radius) <= halfMillimetre
                    && Math.Abs(plane.Normal.DotProduct(round.Center - plane.Origin)) <= halfMillimetre)
                    onPlane = true;
            }
        if (!onPlane)
            throw new InvalidOperationException("After the label, the revolve's circle does not sit on \"" + centrePlane.Name
                + "\", so the ring would not follow \"" + parameter.Definition.Name + "\". NOTHING from this call was kept.");
    }

    valueMm = Math.Round(reads.Value * 304.8, 2);
    labelled = "The " + what + ", " + (dimension == already ? "already labelled" : "labelled") + " \""
        + parameter.Definition.Name + "\" inside the form's sketch, reading "
        + mm(reads.Value) + " mm." + lockNote;
    findings.Add(labelled);
    findings.Add("The dimension is in the sketch, where Edit " + (target is Extrusion ? "Extrusion" : "Revolve") + " shows it. "
        + "FLEX_FAMILY with \"" + parameter.Definition.Name + "\" at two different sizes is what shows the form following.");
}

if (refused != null) findings.Add(refused);
