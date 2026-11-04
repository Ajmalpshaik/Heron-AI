// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `arc`, `face`,
// `dimensionType` and `offsetMm` are in scope; leaves `created`, `measured`,
// `typeUsed`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// REVIT 2025 AND LATER ONLY, AND THE CONTRACT SAYS SO. ArcLengthDimension and
// its Create(Document, View, Arc, Reference arcRef, IList<Reference>) are absent
// from 2020-2024 and present 2025-2027 - tools/api-surface --members on
// 2026-11-02. There is no older route to fall back to, so it is named directly.
//
// THE THREE REFERENCES REVIT WANTS. The arc being measured (`arcRef`), and two
// references that cross it at the ends the length is measured between. The
// `arc` argument is NOT the measured arc: it is where the dimension line sits,
// concentric with the measured one and `offsetMm` outside it.
//
//   arc line   the curve's own reference, and its two end point references -
//              the length then equals the line's own length exactly
//   wall       its exterior or interior side face (HostObjectUtils, Revit's own
//              sides), and the flat end face nearest each end of its location
//              line
//   beam       its outer or inner curved side face, and the flat end face
//              nearest each end of its location line. Faces are read from the
//              family's own geometry (GetSymbolGeometry), never an instance
//              copy - the same rule CREATE_LINEAR_DIMENSION keeps
//
// READ BACK: the length Revit measured, and the type the dimension carries.

const double MmPerFoot = 304.8;
var created = ElementId.InvalidElementId;
var measured = "";
var typeUsed = "";
string refused = null;
var findings = new List<string>();
string problem = null;

Func<string, Type, Element> byId = (said, wantedClass) =>
{
    var text = (said ?? "").Trim();
    if (text.Length == 0) return null;
    Element found = null;
    try { found = doc.GetElement(text); } catch (Exception) { found = null; }
    if (found != null) return wantedClass.IsInstanceOfType(found) ? found : null;
    foreach (var e in new FilteredElementCollector(doc).OfClass(wantedClass).WhereElementIsNotElementType())
        if (e.Id.ToString() == text) return e;
    return null;
};
Func<double, string> mm = feet => (feet * MmPerFoot).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

// ---- WHICH ELEMENT ---------------------------------------------------------
// Typed ("wall 123456", "beam 123456", "line 123456") wins; otherwise exactly
// one element from the selection.
var arcText = (arc ?? "").Trim();
if (arcText.Equals("none", StringComparison.OrdinalIgnoreCase)) arcText = "";
var picked = elements == null ? new List<Element>() : elements.Where(e => e != null).ToList();
Element target = null;
string label = null;
if (arcText.Length > 0)
{
    var space = arcText.IndexOf(' ');
    var word = (space < 0 ? arcText : arcText.Substring(0, space)).ToLowerInvariant();
    var rest = space < 0 ? "" : arcText.Substring(space + 1).Trim();
    if (word == "wall") target = byId(rest, typeof(Wall));
    else if (word == "beam") target = byId(rest, typeof(FamilyInstance));
    else if (word == "line") target = byId(rest, typeof(CurveElement));
    else problem = "\"" + arcText + "\" is not something this can dimension. Say wall ID, beam ID or line ID, or "
        + "select the curved wall, beam or line.";
    if (problem == null && target == null)
        problem = "No " + word + " has the id \"" + rest + "\".";
    label = arcText;
}
else if (picked.Count == 0)
{
    problem = "No curved wall, beam or line was named or selected. Select one, or say wall ID, beam ID or line ID.";
}
else if (picked.Count > 1)
{
    problem = picked.Count + " elements are selected (" + string.Join(", ", picked.Take(5).Select(e => e.Id.ToString()))
        + (picked.Count > 5 ? ", ..." : "") + "). An arc length dimension measures ONE arc - select one.";
}
else
{
    target = picked[0];
    label = (target.Category != null ? target.Category.Name : target.GetType().Name) + " " + target.Id;
}

// ---- ITS ARC ---------------------------------------------------------------
Arc measuredArc = null;      // the location or geometry arc: centre, ends, plane
string what = null;          // "wall", "beam" or "line"
if (problem == null)
{
    var curveElement = target as CurveElement;
    var wall = target as Wall;
    var beam = target as FamilyInstance;
    Curve curve = null;
    if (curveElement != null) { what = "line"; curve = curveElement.GeometryCurve; }
    else if (wall != null) { what = "wall"; var at = wall.Location as LocationCurve; curve = at == null ? null : at.Curve; }
    else if (beam != null && beam.StructuralType == StructuralType.Beam)
    { what = "beam"; var at = beam.Location as LocationCurve; curve = at == null ? null : at.Curve; }
    else
        problem = label + " is not a wall, a beam or a model or detail line, so it has no arc to measure.";

    if (problem == null)
    {
        measuredArc = curve as Arc;
        if (measuredArc == null)
            problem = label + " is straight. An arc length is measured along a CURVED " + what
                + " - a straight one is CREATE_LINEAR_DIMENSION's.";
        else if (!measuredArc.IsBound)
            problem = label + " is a full circle, which has no two ends to measure between.";
    }
}

// ---- THE VIEW --------------------------------------------------------------
string viewProblem = null;
if (view != null && !view.IsTemplate)
{
    var kind = view.ViewType;
    if (kind == ViewType.ThreeD)
    {
        var view3d = view as View3D;
        if (view3d == null || view3d.IsPerspective)
            viewProblem = "\"" + view.Name + "\" is a perspective 3D view, and Revit puts no dimension in a perspective.";
        else if (!view3d.IsLocked)
            viewProblem = "\"" + view.Name + "\" is a 3D view that is not locked, and Revit dimensions a 3D view only "
                + "once its orientation is saved and locked. Name a plan, or lock the view.";
    }
    else if (kind != ViewType.FloorPlan && kind != ViewType.CeilingPlan && kind != ViewType.EngineeringPlan
        && kind != ViewType.AreaPlan && kind != ViewType.Section && kind != ViewType.Elevation
        && kind != ViewType.Detail && kind != ViewType.DraftingView)
    {
        viewProblem = "\"" + view.Name + "\" is a " + kind + " view, and Revit draws a dimension only in a plan, a "
            + "section, an elevation, a detail or drafting view, or a locked 3D view.";
    }
    if (viewProblem == null && measuredArc != null
        && Math.Abs(measuredArc.Normal.Normalize().DotProduct(view.ViewDirection.Normalize())) < 0.999)
        viewProblem = label + "'s arc does not lie flat in \"" + view.Name + "\" - the view sees it edge-on or at a "
            + "slant, so there is no curve on the drawing to measure along. A curved wall or beam is dimensioned in a plan.";
}

// ---- THE DIMENSION TYPE ----------------------------------------------------
var typeText = (dimensionType ?? "").Trim();
if (typeText.Equals("none", StringComparison.OrdinalIgnoreCase)) typeText = "";
var arcTypes = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>()
    .Where(t => { try { return t.StyleType == DimensionStyleType.ArcLength; } catch (Exception) { return false; } })
    .ToList();
DimensionType chosen = null;
string typeProblem = null;
string defaultNote = null;
if (typeText.Length > 0)
{
    chosen = arcTypes.FirstOrDefault(t => string.Equals(t.Name, typeText, StringComparison.OrdinalIgnoreCase));
    if (chosen == null)
    {
        var other = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>()
            .FirstOrDefault(t => string.Equals(t.Name, typeText, StringComparison.OrdinalIgnoreCase));
        typeProblem = (other != null
                ? "'" + typeText + "' is a " + other.StyleType + " dimension type, not an arc length one."
                : "No dimension type is called '" + typeText + "'.")
            + " The arc length dimension types here: "
            + (arcTypes.Count == 0 ? "none" : string.Join(", ", arcTypes.Select(t => "'" + t.Name + "'"))) + ".";
    }
}
else
{
    chosen = doc.GetElement(doc.GetDefaultElementTypeId(ElementTypeGroup.ArcLengthDimensionType)) as DimensionType;
    if (chosen != null)
        defaultNote = "No dimension type was named, so the project's default arc length type is used: '" + chosen.Name + "'.";
    else
        defaultNote = ("No dimension type was named and the project names no default arc length type; Revit chooses one, "
            + "and the type read back below is the one it chose.");
}

// ---- REFUSE BY NAME --------------------------------------------------------
if (view == null) refused = "No view was named, and a dimension lives on one view.";
else if (view.IsTemplate)
    refused = "'" + view.Name + "' is a view TEMPLATE, not a view - a template carries settings and holds no annotation.";
else if (doc.IsFamilyDocument)
    refused = "The document in front is a family open in the Family Editor; this draws an arc length dimension in a project view.";
else if (problem != null) refused = problem;
else if (viewProblem != null) refused = viewProblem;
else if (typeProblem != null) refused = typeProblem;

// ---- THE REFERENCES --------------------------------------------------------
Reference arcRef = null;
var ends = new List<Reference>();
double refRadius = 0;
if (refused == null)
{
    var centre = measuredArc.Center;
    var axis = measuredArc.Normal.Normalize();
    var p0 = measuredArc.GetEndPoint(0);
    var p1 = measuredArc.GetEndPoint(1);
    var pm = measuredArc.Evaluate(0.5, true);
    var side = (face ?? "").Trim().ToLowerInvariant();
    if (side.Length == 0 || side == "none") side = "exterior";
    if (side == "outer" || side == "outside") side = "exterior";
    if (side == "inner" || side == "inside") side = "interior";

    if (what == "line")
    {
        arcRef = measuredArc.Reference;
        var r0 = measuredArc.GetEndPointReference(0);
        var r1 = measuredArc.GetEndPointReference(1);
        if (arcRef == null || r0 == null || r1 == null)
            problem = label + " gives Revit no reference to dimension to.";
        else { ends.Add(r0); ends.Add(r1); refRadius = measuredArc.Radius; }
    }
    else if (side != "exterior" && side != "interior")
    {
        problem = "'" + face + "' is not a side this knows. Say exterior (the outer face) or interior.";
    }
    else
    {
        // Every face of the element, in model co-ordinates, with its reference.
        var planar = new List<Tuple<Reference, XYZ, XYZ>>();          // reference, normal, a point on it
        var curved = new List<Tuple<Reference, double, XYZ>>();       // reference, radius, axis
        var options = new Options();
        options.ComputeReferences = true;
        options.IncludeNonVisibleObjects = false;
        options.DetailLevel = ViewDetailLevel.Fine;
        Action<GeometryElement, Transform> walk = null;
        walk = (geometry, placed) =>
        {
            if (geometry == null) return;
            foreach (GeometryObject item in geometry)
            {
                var nested = item as GeometryInstance;
                if (nested != null) { walk(nested.GetSymbolGeometry(), placed.Multiply(nested.Transform)); continue; }
                var solid = item as Solid;
                if (solid == null || solid.Faces.Size == 0) continue;
                foreach (Face one in solid.Faces)
                {
                    if (one.Reference == null) continue;
                    var flat = one as PlanarFace;
                    var round = one as CylindricalFace;
                    if (flat != null)
                        planar.Add(Tuple.Create(one.Reference, placed.OfVector(flat.FaceNormal).Normalize(), placed.OfPoint(flat.Origin)));
                    else if (round != null)
                        curved.Add(Tuple.Create(one.Reference, placed.OfVector(round.get_Radius(0)).GetLength(),
                            placed.OfVector(round.Axis).Normalize()));
                }
            }
        };
        try { walk(target.get_Geometry(options), Transform.Identity); }
        catch (Exception ex) { problem = label + "'s geometry could not be read: " + ex.Message; }

        if (problem == null && what == "wall")
        {
            IList<Reference> sides = null;
            try { sides = HostObjectUtils.GetSideFaces((Wall)target, side == "exterior" ? ShellLayerType.Exterior : ShellLayerType.Interior); }
            catch (Exception) { sides = null; }
            foreach (var r in sides ?? new List<Reference>())
            {
                var round = target.GetGeometryObjectFromReference(r) as CylindricalFace;
                if (round == null || Math.Abs(round.Axis.Normalize().DotProduct(axis)) < 0.999) continue;
                arcRef = r; refRadius = round.get_Radius(0).GetLength();
                break;
            }
            if (arcRef == null)
                problem = "Wall " + target.Id + "'s " + side + " face is not one curved face Revit can dimension along.";
        }
        else if (problem == null)
        {
            var rounds = curved.Where(c => Math.Abs(c.Item3.DotProduct(axis)) > 0.999).ToList();
            if (rounds.Count == 0)
                problem = "Beam " + target.Id + " has no curved side face standing upright to dimension along.";
            else
            {
                var pick = side == "exterior" ? rounds.OrderByDescending(c => c.Item2).First() : rounds.OrderBy(c => c.Item2).First();
                arcRef = pick.Item1; refRadius = pick.Item2;
            }
        }

        // THE END FACES: flat, upright, facing out of that end, nearest its end point.
        if (problem == null)
        {
            Func<XYZ, Tuple<Reference, double>> endFace = point =>
            {
                var outward = axis.CrossProduct(point - centre).Normalize();
                if (outward.DotProduct(pm - point) > 0) outward = outward.Negate();
                Tuple<Reference, double> best = null;
                foreach (var f in planar)
                {
                    if (Math.Abs(f.Item2.DotProduct(axis)) > 0.05 || f.Item2.DotProduct(outward) < 0.5) continue;
                    var gap = Math.Abs(f.Item2.DotProduct(point - f.Item3));
                    if (best == null || gap < best.Item2) best = Tuple.Create(f.Item1, gap);
                }
                return best;
            };
            var e0 = endFace(p0);
            var e1 = endFace(p1);
            if (e0 == null || e1 == null)
                problem = label + " has no flat end face at " + (e0 == null && e1 == null ? "either end" : "one end")
                    + " for the dimension to stop at.";
            else
            {
                ends.Add(e0.Item1); ends.Add(e1.Item1);
                foreach (var gap in new[] { e0.Item2, e1.Item2 })
                    if (gap > 1.0 / MmPerFoot)
                        findings.Add("An end face of " + label + " sits " + mm(gap) + " mm from the end of its location "
                            + "line - a join at that end - so the dimension stops at the face, not the line.");
            }
        }
    }
    if (problem != null) refused = problem;

    // ---- DRAW ----------------------------------------------------------------
    if (refused == null)
    {
        var offset = (offsetMm == 0 ? 500.0 : offsetMm) / MmPerFoot;
        var radius = refRadius + offset;
        if (offsetMm == 0) findings.Add("No offset was given, so the dimension line sits 500 mm outside the arc.");
        if (radius <= 1.0 / MmPerFoot)
            refused = "An offset of " + offsetMm + " mm puts the dimension line through the arc's centre; give a smaller one.";
        else
        {
            Func<XYZ, XYZ> onLine = p => centre + (p - centre).Normalize().Multiply(radius);
            var placement = Arc.Create(onLine(p0), onLine(p1), onLine(pm));
            ArcLengthDimension made = null;
            try { made = ArcLengthDimension.Create(doc, view, placement, arcRef, ends); }
            catch (Exception ex)
            {
                refused = "Revit refused the arc length dimension: " + ex.Message + " The usual cause is an arc the view "
                    + "does not show.";
            }
            if (refused == null && made == null)
                refused = "Revit returned no arc length dimension. The usual cause is an arc the view does not show.";
            if (refused == null)
            {
                if (chosen != null && made.GetTypeId() != chosen.Id) made.ChangeTypeId(chosen.Id);
                doc.Regenerate();
                if (!made.Value.HasValue || made.Value.Value <= 0)
                    throw new InvalidOperationException("The arc length dimension reads no length back. The call failed, "
                        + "and Heron rolls the whole call back.");
                var readType = doc.GetElement(made.GetTypeId()) as DimensionType;
                if (chosen != null && (readType == null || readType.Id != chosen.Id))
                    throw new InvalidOperationException("The dimension reads back as type '" + (readType == null ? "none" : readType.Name)
                        + "', not '" + chosen.Name + "'. The call failed, and Heron rolls the whole call back.");
                var value = made.Value.Value;
                var expected = measuredArc.Length / measuredArc.Radius * refRadius;
                if (what == "line" && Math.Abs(value - measuredArc.Length) > 0.5 / MmPerFoot)
                    throw new InvalidOperationException("The dimension reads " + mm(value) + " mm where the line is "
                        + mm(measuredArc.Length) + " mm long. The call failed, and Heron rolls the whole call back.");
                if (what != "line" && Math.Abs(value - expected) > 1.0 / MmPerFoot)
                    findings.Add("It reads " + mm(value) + " mm where the " + what + "'s " + side + " face runs "
                        + mm(expected) + " mm between the ends of its location line - the end faces are not square "
                        + "to the arc at a join.");
                if (defaultNote != null) findings.Add(defaultNote);
                created = made.Id;
                typeUsed = readType == null ? "" : readType.Name;
                measured = "arc length " + mm(value) + " mm";
                findings.Add("Arc length dimension drawn on '" + view.Name + "' along " + label
                    + (what == "line" ? "" : " (" + side + " face)") + ", reading " + measured + ", type '" + typeUsed
                    + "', id " + created + " - read back.");
            }
        }
    }
}

if (refused != null)
{
    refused = refused + " Nothing was drawn.";
    findings.Add(refused);
}
