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
//   A SWEPT BLEND - version 4, for the half-turns of a coil spring - has three,
//   measured 2026-10-07 on a scratch family (SpringProbe, Revit 2024):
//     its PATH's arc radius: a radial or diameter dimension on the path arc in
//     the plan or elevation looking at the path's plane, and the arc's centre
//     locked to every reference plane through it. At 40, 60 and 25 mm the arc
//     kept its centre and its ends, and both profiles went with its ends.
//     a PROFILE circle's own radius or diameter, on every circle of that size
//     in the bottom and top profiles - a half circle's arc included. One
//     dimension per circle; both profiles followed one parameter at 8 and 3.
//     a profile circle's CENTRE's distance from the path's plane - the rise of
//     a coil: a plane through the centre, parallel to the path's plane, the
//     centre locked to it and the plane dimensioned from the level or plane
//     the path is sketched on. The circle rose with it at 30 and fell at 6.
//   ONE PARAMETER MAY LABEL THE SAME KIND OF SIZE ON SEVERAL SWEPT BLENDS - the
//   half-turns of one coil share their radii - so for a swept blend a
//   parameter already on other radial or diameter dimensions is not refused;
//   a rise reuses the plane its parameter already labels from the path's plane.
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

// A SWEPT BLEND's sizes - see the header. Each list or field is filled only
// for the kind of size the parameter's value picked.
SweptBlend blend = null;
// The path's arc, the view looking at its plane, and a radial or diameter
// dimension this parameter already puts on it.
Arc pathArc = null;
View pathView = null;
Dimension pathAlready = null;
// Profile circles labelled by their own size: the arc, the view looking at
// its profile, and whether this parameter already labels it.
var circles = new List<Tuple<Arc, View, bool>>();
// Profile circles whose centre's distance from the path's plane is labelled:
// the arc, its sketch, and its centre point's reference.
var risers = new List<Tuple<Arc, ElementId, Reference>>();
// The level or reference plane the path lies on, the path plane's facing,
// the view the rise is dimensioned in, the plane through the centres (found,
// or made at the label) and a dimension that already labels it.
Element pathDatum = null;
Reference pathDatumReference = null;
XYZ pathNormal = null;
View riseView = null;
ReferencePlane risePlane = null;
Dimension riseAlready = null;
// Does a dimension join exactly these two elements?
Func<Dimension, ElementId, ElementId, bool> joinsIds = (d, first, second) =>
{
    try
    {
        var ids = d.References.Cast<Reference>().Select(r => r.ElementId).ToList();
        return ids.Count == 2 && ids.Contains(first) && ids.Contains(second);
    }
    catch (Exception) { return false; }
};
// Every dimension a parameter labels.
Func<FamilyParameter, List<Dimension>> labelledWith = p =>
{
    var found = new List<Dimension>();
    foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
    {
        FamilyParameter label = null;
        try { label = d.FamilyLabel; } catch (Exception) { }
        if (label != null && label.Id == p.Id) found.Add(d);
    }
    return found;
};
Func<Dimension, bool> isRound = d =>
{
    try { return d.DimensionShape == DimensionShape.Radial || d.DimensionShape == DimensionShape.Diameter; }
    catch (Exception) { return false; }
};
// Does a dimension measure this curve element?
Func<Dimension, ElementId, bool> measures = (d, curveId) =>
{
    try { return d.References.Cast<Reference>().Any(r => r.ElementId == curveId); }
    catch (Exception) { return false; }
};

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
        else if (!(target is Extrusion) && !(target is Revolution) && !(target is SweptBlend))
            problems.Add("\"" + (chosen.Name ?? said) + "\" is a " + target.GetType().Name.ToLowerInvariant() + ". A round "
                + "size is labelled on an extrusion's circle, a revolve's radius or a swept blend's path and profiles; "
                + "for any other form, label it by hand in the form's sketch.");
        blend = target as SweptBlend;
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

    // A SWEPT BLEND: three round sizes, each found by the size the parameter
    // holds - the path's arc radius, a profile circle's own size, and a
    // profile circle centre's distance from the path's plane. See the header.
    if (problems.Count == 0 && blend != null)
    {
        var pathPlane = blend.PathSketch.SketchPlane.GetPlane();
        pathNormal = pathPlane.Normal.Normalize();
        var pathCurves = new List<Curve>();
        foreach (CurveArray loop in blend.PathSketch.Profile)
            foreach (Curve curve in loop) pathCurves.Add(curve);
        var pathRound = pathCurves.Count == 1 ? pathCurves[0] as Arc : null;
        // How far a point stands off the path's plane, on its facing side.
        Func<XYZ, double> standOff = point => pathNormal.DotProduct(point - pathPlane.Origin);

        var radii = new List<double>();
        var heights = new List<double>();
        var ownSize = new List<Tuple<Arc, ElementId>>();
        foreach (var sketch in new[] { blend.BottomSketch, blend.TopSketch })
        {
            if (sketch == null) continue;
            foreach (CurveArray loop in sketch.Profile)
                foreach (Curve curve in loop)
                {
                    // The two halves of one circle share a centre and a radius:
                    // one circle, counted once per profile.
                    var round = curve as Arc;
                    if (round == null || round.Reference == null) continue;
                    var height = standOff(round.Center);
                    radii.Add(round.Radius);
                    if (Math.Abs(height) > halfMillimetre) heights.Add(Math.Abs(height));
                    if (Math.Abs(round.Radius - wanted) <= halfMillimetre
                        && !ownSize.Any(t => t.Item2 == sketch.Id && t.Item1.Center.IsAlmostEqualTo(round.Center, halfMillimetre)))
                        ownSize.Add(Tuple.Create(round, sketch.Id));
                    if (!diameter && Math.Abs(height) > halfMillimetre && Math.Abs(Math.Abs(height) - wanted) <= halfMillimetre
                        && !risers.Any(t => t.Item2 == sketch.Id && t.Item1.Center.IsAlmostEqualTo(round.Center, halfMillimetre)))
                    {
                        var drawn = doc.GetElement(round.Reference) as CurveElement;
                        Reference centrePoint = null;
                        try { centrePoint = drawn == null ? null : drawn.CenterPointReference; } catch (Exception) { }
                        if (centrePoint != null) risers.Add(Tuple.Create(round, sketch.Id, centrePoint));
                    }
                }
        }
        var onPath = pathRound != null && pathRound.Reference != null
            && Math.Abs(pathRound.Radius - wanted) <= halfMillimetre;
        Func<List<double>, string> listed = sizes =>
            string.Join(", ", sizes.Select(r => mm(r)).Distinct().OrderBy(r => double.Parse(r, invariant)));

        var kinds = (onPath ? 1 : 0) + (ownSize.Count > 0 ? 1 : 0) + (risers.Count > 0 ? 1 : 0);
        if (kinds == 0)
        {
            var sizes = new List<string>();
            if (pathRound != null) sizes.Add("its path is an arc of radius " + mm(pathRound.Radius) + " mm");
            if (radii.Count > 0) sizes.Add("its profiles' circles have radii of " + listed(radii) + " mm");
            if (heights.Count > 0 && !diameter)
                sizes.Add("their centres stand " + listed(heights) + " mm off the path's plane");
            problems.Add((diameter ? "Nothing in the swept blend is " + mm(wanted * 2) + " mm across. "
                    : "Nothing in the swept blend is " + mm(wanted) + " mm in size: not its path's radius, no profile "
                        + "circle's radius, no profile circle centred that far off the path's plane. ")
                + (sizes.Count == 0 ? "Its path is not an arc and its profiles have no circles."
                    : "In it, " + string.Join("; ", sizes) + " - set the parameter to one of those first."));
        }
        else if (kinds > 1)
            problems.Add(mm(wanted) + " mm is more than one size of this swept blend - "
                + string.Join(" and ", new[] {
                    onPath ? "its path's radius" : null,
                    ownSize.Count > 0 ? "a profile circle's radius" : null,
                    risers.Count > 0 ? "a profile circle's height off the path's plane" : null }.Where(w => w != null))
                + ". Only one can be labelled and which one is meant cannot be told - give the parameter a size only "
                + "one of them has, label, then set it back.");
        else if (onPath)
        {
            pathArc = pathRound;
            pathView = viewFacing(pathNormal);
            if (pathView == null)
                problems.Add("No plan or elevation of this family looks straight at the swept blend's path, so its "
                    + "radius cannot be dimensioned.");
        }
        else if (ownSize.Count > 0)
        {
            foreach (var found in ownSize)
            {
                var seen = viewFacing(found.Item1.Normal);
                if (seen == null)
                {
                    problems.Add("No plan or elevation of this family looks straight at the swept blend's profile, so its "
                        + "circle cannot be dimensioned.");
                    break;
                }
                circles.Add(Tuple.Create(found.Item1, seen, false));
            }
        }
        else
        {
            // ONE SIDE OF THE PATH'S PLANE: the centres share one plane only
            // when they stand off it the same way.
            var sides = risers.Select(r => Math.Sign(standOff(r.Item1.Center))).Distinct().Count();
            if (sides > 1)
                problems.Add("The profile circles " + mm(wanted) + " mm off the path's plane stand on both sides of it, "
                    + "and one plane cannot hold them both. Label one profile by hand in Edit Swept Blend.");
            riseView = viewFacing(risers[0].Item1.Normal);
            if (riseView == null)
                problems.Add("No plan or elevation of this family looks straight at the swept blend's profile, so its "
                    + "height cannot be dimensioned.");

            // THE PATH'S PLANE, as the level or named reference plane the
            // path was sketched on - the sketch plane carries its name.
            var sketchName = blend.PathSketch.SketchPlane.Name ?? "";
            var onDatum = new List<Element>();
            foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
                if (Math.Abs(Math.Abs(pathNormal.Z) - 1) < 1e-6
                    && Math.Abs(level.Elevation - pathPlane.Origin.Z) <= halfMillimetre / 10)
                    onDatum.Add(level);
            foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
            {
                var plane = candidate.GetPlane();
                if (Math.Abs(Math.Abs(plane.Normal.DotProduct(pathNormal)) - 1) > 1e-6) continue;
                if (Math.Abs(plane.Normal.DotProduct(pathPlane.Origin - plane.Origin)) > halfMillimetre / 10) continue;
                if ((candidate.Name ?? "").Length == 0) continue;
                onDatum.Add(candidate);
            }
            pathDatum = onDatum.FirstOrDefault(e => e.Name == sketchName) ?? onDatum.FirstOrDefault();
            if (pathDatum == null)
                problems.Add("The swept blend's path lies on no level or named reference plane, so the profile's height "
                    + "has nothing to be measured from. Sketch the path on one.");
            else pathDatumReference = pathDatum is Level ? ((Level)pathDatum).GetPlaneReference()
                : ((ReferencePlane)pathDatum).GetReference();

            // A PLANE ALREADY THROUGH THE CENTRES, parallel to the path's.
            var centre = risers[0].Item1.Center;
            foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
            {
                var plane = candidate.GetPlane();
                if (Math.Abs(Math.Abs(plane.Normal.DotProduct(pathNormal)) - 1) > 1e-6) continue;
                if (Math.Abs(plane.Normal.DotProduct(centre - plane.Origin)) > halfMillimetre / 10) continue;
                risePlane = candidate;
                break;
            }
        }

        // ONE LABEL PER PARAMETER - EXCEPT ACROSS SWEPT BLENDS. A coil's
        // half-turns share their radii, so a parameter already on other radial
        // or diameter dimensions may label this one's too; a curve it already
        // labels is left as it is. A rise reuses the plane its parameter
        // already labels from the path's plane; any other dimension the
        // parameter drives is refused, as for every other form.
        if (problems.Count == 0)
        {
            var labels = labelledWith(parameter);
            if (risers.Count > 0)
            {
                riseAlready = risePlane == null || pathDatum == null ? null
                    : labels.FirstOrDefault(d => joinsIds(d, pathDatum.Id, risePlane.Id));
                var other = labels.FirstOrDefault(d => d != riseAlready);
                if (other != null)
                    problems.Add("\"" + parameter.Definition.Name + "\" already labels another dimension in this family. "
                        + "A height labelled twice would fight the first as soon as anything moves.");
                if (risePlane != null && riseAlready == null)
                {
                    FamilyParameter heldBy = null;
                    foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
                    {
                        FamilyParameter label = null;
                        try { label = d.FamilyLabel; } catch (Exception) { }
                        if (label != null && joinsIds(d, pathDatum.Id, risePlane.Id)) { heldBy = label; break; }
                    }
                    if (heldBy != null)
                        problems.Add("\"" + risePlane.Name + "\" runs through the profile's centre and is already held "
                            + "from \"" + pathDatum.Name + "\" by \"" + heldBy.Definition.Name + "\". Use that "
                            + "parameter, or move the profile first.");
                }
            }
            else
            {
                if (labels.Any(d => !isRound(d)))
                    problems.Add("\"" + parameter.Definition.Name + "\" already labels a straight dimension in this "
                        + "family. A round size shares its parameter only with other round sizes.");
                if (pathArc != null)
                {
                    var drawn = pathArc.Reference.ElementId;
                    pathAlready = labels.FirstOrDefault(d => measures(d, drawn));
                }
                for (var i = 0; i < circles.Count; i++)
                {
                    // Either half of the circle may carry the label.
                    var circle = circles[i].Item1;
                    var halves = new List<ElementId>();
                    foreach (var sketch in new[] { blend.BottomSketch, blend.TopSketch })
                        foreach (CurveArray loop in sketch.Profile)
                            foreach (Curve curve in loop)
                            {
                                var round = curve as Arc;
                                if (round != null && round.Reference != null
                                    && round.Center.IsAlmostEqualTo(circle.Center, halfMillimetre)
                                    && Math.Abs(round.Radius - circle.Radius) <= halfMillimetre)
                                    halves.Add(round.Reference.ElementId);
                            }
                    if (labels.Any(d => halves.Any(h => measures(d, h))))
                        circles[i] = Tuple.Create(circles[i].Item1, circles[i].Item2, true);
                }
            }
        }
    }

    // ONE LABEL PER PARAMETER. The one exception is the dimension this call
    // would make anyway: a torus whose centre plane is already labelled from
    // the axis's plane, its circle not yet locked to it - Family4's rim on
    // 2026-10-03, built by hand that way before this tool could do it. A swept
    // blend's own rule is just above.
    if (problems.Count == 0 && already != null && blend == null
        && !(centreArc != null && centrePlane != null && joins(already, axisPlane, centrePlane)))
        problems.Add("\"" + parameter.Definition.Name + "\" already labels a dimension in this family. A second label "
            + "on it would fight the first as soon as anything moves.");

    if (problems.Count > 0) refused = "Nothing was labelled. " + string.Join(" ", problems);
}

if (refused == null && blend == null)
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

// ---------------------------------------------------------------------------
// A SWEPT BLEND: LABEL, LOCK, THEN READ EVERY SIZE BACK
// ---------------------------------------------------------------------------

if (refused == null && blend != null)
{
    var made = new List<Dimension>();
    var notes = new List<string>();
    var name = parameter.Definition.Name;
    var what = "";
    try
    {
        if (pathArc != null)
        {
            what = (diameter ? "diameter" : "radius") + " of the swept blend's path arc";
            if (pathAlready != null)
            {
                made.Add(pathAlready);
                notes.Add("The path arc already carried \"" + name + "\".");
            }
            else
            {
                var d = diameter
                    ? doc.FamilyCreate.NewDiameterDimension(pathView, pathArc.Reference, pathArc.Evaluate(0.5, true))
                    : doc.FamilyCreate.NewRadialDimension(pathView, pathArc.Reference, pathArc.Evaluate(0.5, true));
                d.FamilyLabel = parameter;
                made.Add(d);
            }
            // THE CENTRE HELD - locked to every named reference plane through
            // it, square to the path's plane, as a revolve's axis is.
            var drawn = doc.GetElement(pathArc.Reference) as CurveElement;
            Reference centrePoint = null;
            try { centrePoint = drawn == null ? null : drawn.CenterPointReference; } catch (Exception) { }
            var through = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
                .Where(rp => (rp.Name ?? "").Length > 0
                    && Math.Abs(rp.GetPlane().Normal.DotProduct(pathNormal)) < 1e-6
                    && Math.Abs(rp.GetPlane().Normal.DotProduct(pathArc.Center - rp.GetPlane().Origin)) <= halfMillimetre / 10)
                .ToList();
            foreach (var rp in through)
            {
                if (centrePoint == null) break;
                try
                {
                    var held = doc.FamilyCreate.NewAlignment(pathView, rp.GetReference(),
                        Reference.ParseFromStableRepresentation(doc, centrePoint.ConvertToStableRepresentation(doc)));
                    held.IsLocked = true;
                    notes.Add("Its centre is locked to \"" + rp.Name + "\".");
                }
                catch (Exception lockFailure)
                {
                    notes.Add("Its centre was not locked to \"" + rp.Name + "\" (" + lockFailure.Message + ").");
                }
            }
            if (through.Count == 0)
                notes.Add("No named reference plane runs through the path's centre, so the sketch alone holds it - "
                    + "measured, an arc whose radius alone was labelled kept its centre.");
        }
        else if (circles.Count > 0)
        {
            what = (diameter ? "diameter" : "radius") + " of " + circles.Count + " profile circle(s) of the swept blend";
            foreach (var circle in circles)
            {
                if (circle.Item3)
                {
                    notes.Add("A profile circle already carried \"" + name + "\".");
                    continue;
                }
                var d = diameter
                    ? doc.FamilyCreate.NewDiameterDimension(circle.Item2, circle.Item1.Reference, circle.Item1.Evaluate(0.5, true))
                    : doc.FamilyCreate.NewRadialDimension(circle.Item2, circle.Item1.Reference, circle.Item1.Evaluate(0.5, true));
                d.FamilyLabel = parameter;
                made.Add(d);
            }
        }
        else
        {
            what = "height of " + risers.Count + " profile circle centre(s) off \"" + pathDatum.Name + "\"";
            var centre = risers[0].Item1.Center;
            var profileNormal = risers[0].Item1.Normal.Normalize();
            if (risePlane == null)
            {
                // Drawn in the profile's view, along the path's plane: its
                // normal is the path's.
                var across = pathNormal.CrossProduct(profileNormal).Normalize();
                risePlane = doc.FamilyCreate.NewReferencePlane(centre + across, centre - across, profileNormal, riseView);
                if (!new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
                        .Any(p => p.Name == name)
                    && !new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Any(l => l.Name == name))
                    risePlane.Name = name;
                doc.Regenerate();
                notes.Add("A reference plane" + (risePlane.Name == name ? " \"" + name + "\"" : "") + " was made through "
                    + "the centre, parallel to \"" + pathDatum.Name + "\".");
            }
            foreach (var riser in risers)
            {
                try
                {
                    var held = doc.FamilyCreate.NewAlignment(riseView, risePlane.GetReference(),
                        Reference.ParseFromStableRepresentation(doc, riser.Item3.ConvertToStableRepresentation(doc)));
                    held.IsLocked = true;
                    notes.Add("A profile circle's centre is locked to \"" + risePlane.Name + "\".");
                }
                catch (Exception lockFailure)
                {
                    // Run a second time, the lock is already there and Revit
                    // refuses another; the read-back below still checks the
                    // centre sits on the plane.
                    if (riseAlready == null) throw;
                    notes.Add("A profile circle's centre was not locked again (" + lockFailure.Message + ").");
                }
            }
            if (riseAlready != null)
            {
                made.Add(riseAlready);
                notes.Add("\"" + risePlane.Name + "\" was already labelled \"" + name + "\" from \"" + pathDatum.Name
                    + "\" - used as it is.");
            }
            else
            {
                var origin = blend.PathSketch.SketchPlane.GetPlane().Origin;
                var foot = centre - pathNormal.Multiply(pathNormal.DotProduct(centre - origin));
                var references = new ReferenceArray();
                references.Append(pathDatumReference);
                references.Append(risePlane.GetReference());
                var d = doc.FamilyCreate.NewDimension(riseView, Line.CreateBound(foot, centre), references);
                d.FamilyLabel = parameter;
                made.Add(d);
            }
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to label the swept blend with \"" + name + "\": " + ex.Message
            + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK: every dimension carries the parameter and reads its value.
    var holds = doc.FamilyManager.CurrentType.AsDouble(parameter);
    if (!holds.HasValue)
        throw new InvalidOperationException("\"" + name + "\" holds no value after the label. NOTHING from this call was kept.");
    foreach (var d in made)
    {
        FamilyParameter readLabel = null;
        try { readLabel = d.FamilyLabel; } catch (Exception) { }
        var reads = d.Value;
        if (readLabel == null || readLabel.Id != parameter.Id)
            throw new InvalidOperationException("A dimension was made and does not carry \"" + name + "\" afterwards. "
                + "NOTHING from this call was kept.");
        if (!reads.HasValue || Math.Abs(reads.Value - holds.Value) > halfMillimetre)
            throw new InvalidOperationException("A labelled dimension reads " + (reads.HasValue ? mm(reads.Value) + " mm" : "nothing")
                + " and \"" + name + "\" holds " + mm(holds.Value) + " mm. NOTHING from this call was kept.");
    }

    // A RISE MUST LEAVE EVERY CENTRE ON THE PLANE THE LABEL MOVES.
    if (risers.Count > 0)
    {
        var plane = risePlane.GetPlane();
        foreach (var riser in risers)
        {
            var sketch = doc.GetElement(riser.Item2) as Sketch;
            var onPlane = false;
            if (sketch != null)
                foreach (CurveArray loop in sketch.Profile)
                    foreach (Curve curve in loop)
                    {
                        var round = curve as Arc;
                        if (round != null && Math.Abs(round.Radius - riser.Item1.Radius) <= halfMillimetre
                            && Math.Abs(plane.Normal.DotProduct(round.Center - plane.Origin)) <= halfMillimetre)
                            onPlane = true;
                    }
            if (!onPlane)
                throw new InvalidOperationException("After the label, a profile circle's centre does not sit on \""
                    + risePlane.Name + "\", so it would not follow \"" + name + "\". NOTHING from this call was kept.");
        }
    }

    valueMm = Math.Round(holds.Value * 304.8, 2);
    labelled = "The " + what + ", labelled \"" + name + "\" inside the swept blend's sketches"
        + (made.Count > 1 ? " (" + made.Count + " dimensions)" : "") + ", reading " + mm(holds.Value) + " mm."
        + (notes.Count > 0 ? " " + string.Join(" ", notes) : "");
    findings.Add(labelled);
    findings.Add("The dimensions are in the sketches, where Edit Swept Blend shows them. FLEX_FAMILY with \"" + name
        + "\" at two different sizes is what shows the form following.");
}

if (refused != null) findings.Add(refused);
