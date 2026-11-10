// NOT STANDALONE. Assumes `doc`, `uidoc`, `form` and `planes` are in scope;
// leaves `locked`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ALIGN AND LOCK a form's SKETCH to named reference planes and levels - what a
// modeller does by hand inside Edit Sweep > Edit Path, or Edit Extrusion: Align,
// pick the plane, pick the sketch line or the circle's centre, close the
// padlock. LOCK_FORM_TO_PLANES locks the finished form's FACES; this locks the
// lines the form is drawn from.
//
// WHY THE SKETCH AND NOT THE FACES. Measured 2026-11-10 on Revit 2020 (session
// 9912, a square diffuser family): a sweep whose ten faces, and again six, were
// locked to planes with LOCK_FORM_TO_PLANES failed at every later size change -
// "Constraints are not satisfied" and "Can't create sweep". A sweep is rebuilt
// from its path at every change, so the path is what must follow the planes.
// The same day a plenum box's round spigot, an extrusion whose circle's SIZE was
// labelled with LABEL_FAMILY_RADIUS, kept its size and drifted 75 mm sideways
// when the box's width and depth changed: nothing held the circle's centre.
//
// WHAT IS LOCKED, for each plane named:
//   every STRAIGHT sketch line lying in the plane - both ends on it;
//   every circle or arc whose CENTRE lies on the plane - once per centre, on
//   one of its arcs, because Revit stores every sketch circle as two half-arcs
//   (row 5b-1490) and one half's centre carries the circle.
// WHICH SKETCH: an extrusion's or a revolve's own sketch, a sweep's or a swept
// blend's PATH, a blend's base and top. A sweep's profile is not touched - it
// rides on the path.
//
// A SKETCH IS LOCKED ONLY WHERE IT ALREADY LIES, as a face is: Revit's remarks
// on the alignment call say the two references must already be aligned and the
// call will not force them. A plane with nothing on it refuses the whole call,
// naming the nearest line or centre and how far off it is - a lock never moves
// what it locks. The tolerance is the one LOCK_FORM_TO_PLANES measured for faces
// (row 5b-372); it has not been measured for sketch curves.
//
// HOW THE REFERENCES ARE REACHED - the way LABEL_FAMILY_RADIUS reaches them,
// with no sketch-editing scope (SketchEditScope refuses a family document): a
// sketch curve's own reference for a line; for a centre, the drawn arc's centre
// point reference, read back from its written form - the form Revit took for a
// torus's centre and a swept blend's path centre. An arc whose centre mark is
// hidden may give no centre reference; its "Center Mark Visible" is then
// switched on, as it is by hand, and said.
//
// IN THE VIEW THAT LOOKS STRAIGHT AT THE SKETCH: a floor plan for a sketch on a
// level, the elevation facing it for one on a vertical plane. The plane named
// must stand square to the sketch, so it is seen edge on there.
//
// ALL OR NOTHING, READ BACK. Every lock is read back as locked; after them the
// form must still have a solid and its extent must not have moved. Any refusal
// from Revit fails the call with nothing kept. Locked is not yet proved to
// FOLLOW: FLEX_FAMILY moves the planes and reads the form back.

var findings = new List<string>();
var locked = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> fineMm = feet => Math.Round(feet * 304.8, 4).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
// The largest gap Revit was measured to accept for a FACE (row 5b-372).
var exactly = 3e-7 / 304.8;
var square = 1e-6;

Func<XYZ, string> at = p => "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// (element, reference, plane, name) for every named reference plane and every level.
var datums = new List<Tuple<Element, Reference, Plane, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var name = ownName(rp);
        if (name.Length == 0) continue;
        datums.Add(Tuple.Create((Element)rp, rp.GetReference(), rp.GetPlane(), name));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, level.GetPlaneReference(),
            Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, level.Elevation)), level.Name));
}

Func<Plane, XYZ, double> off = (plane, point) => plane.Normal.DotProduct(point - plane.Origin);

GenericForm target = null;
// (what to call it, the sketch) for every sketch of the form that is locked.
var sketches = new List<Tuple<string, Sketch>>();
var wantedPlanes = new List<Tuple<Element, Reference, Plane, string>>();
var problems = new List<string>();

// One thing to lock: the plane, the sketch's word, the view, what it is, a
// LINE's reference in its written form (null for a centre, read at lock time),
// and the drawn sketch curve.
var jobs = new List<Tuple<Tuple<Element, Reference, Plane, string>, string, View, string, string, CurveElement>>();
var noView = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A form's sketch is "
        + "locked to its planes inside the family - open it first.";
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
        else
        {
            // A sweep whose path was picked rather than sketched may give no
            // path sketch back, or refuse to - either is said below.
            Func<Func<Sketch>, Sketch> read = get => { try { return get(); } catch (Exception) { return null; } };
            if (target is Extrusion) sketches.Add(Tuple.Create("sketch", read(() => ((Extrusion)target).Sketch)));
            else if (target is Revolution) sketches.Add(Tuple.Create("sketch", read(() => ((Revolution)target).Sketch)));
            else if (target is Sweep) sketches.Add(Tuple.Create("path", read(() => ((Sweep)target).PathSketch)));
            else if (target is SweptBlend) sketches.Add(Tuple.Create("path", read(() => ((SweptBlend)target).PathSketch)));
            else if (target is Blend)
            {
                sketches.Add(Tuple.Create("base", read(() => ((Blend)target).BottomSketch)));
                sketches.Add(Tuple.Create("top", read(() => ((Blend)target).TopSketch)));
            }
        }
        if (target != null && sketches.Count == 0)
            problems.Add("\"" + (chosen.Name ?? said) + "\" is a " + target.GetType().Name.ToLowerInvariant()
                + ", which has no sketch this locks.");
        if (target != null && sketches.Any(s => s.Item2 == null))
            problems.Add("The " + string.Join(" and ", sketches.Where(s => s.Item2 == null).Select(s => s.Item1))
                + " of this " + target.GetType().Name.ToLowerInvariant() + " is not a sketch Revit gives back - a "
                + "path picked from existing lines, or a 3D path. Lock it by hand.");
    }

    var names = (planes ?? new List<string>()).Select(n => (n ?? "").Trim()).Where(n => n.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    if (names.Count == 0) problems.Add("No plane was named to lock the sketch to.");
    foreach (var name in names)
    {
        var found = datums.Where(d => string.Equals(d.Item4, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (found.Count == 0)
            problems.Add("No reference plane or level is called \"" + name + "\". This family has "
                + (datums.Count == 0 ? "no named ones."
                    : string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + "."));
        else if (found.Count > 1) problems.Add("\"" + name + "\" names " + found.Count + " planes - rename one first.");
        else wantedPlanes.Add(found[0]);
    }

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.CeilingPlan
            || v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section))
        .ToList();
    // The view looking straight at a sketch: a floor plan first, then an
    // elevation, then the rest.
    Func<Sketch, View> viewOn = sketch =>
    {
        var normal = sketch.SketchPlane.GetPlane().Normal;
        return views.Where(v => Math.Abs(Math.Abs(v.ViewDirection.DotProduct(normal)) - 1) < square)
            .OrderBy(v => v.ViewType == ViewType.FloorPlan ? 0 : v.ViewType == ViewType.Elevation ? 1 : 2)
            .FirstOrDefault();
    };

    if (problems.Count == 0)
    {
        foreach (var p in wantedPlanes)
        {
            var near = new List<Tuple<double, string>>();
            var any = false;
            // Something lies on the plane, locked or not - so the refusal
            // never says "nothing lies on it" when something does.
            var onIt = false;
            var squareToOne = false;
            foreach (var s in sketches)
            {
                var sketchPlane = s.Item2.SketchPlane.GetPlane();
                // A plane lying flat in the sketch's plane, or leaning across it,
                // is not seen edge on from the sketch - nothing in it is aligned.
                if (Math.Abs(p.Item3.Normal.DotProduct(sketchPlane.Normal)) > square) continue;
                squareToOne = true;
                var view = viewOn(s.Item2);
                var centres = new List<XYZ>();
                foreach (CurveArray loop in s.Item2.Profile)
                    foreach (Curve curve in loop)
                    {
                        var line = curve as Line;
                        var arc = curve as Arc;
                        if (line != null)
                        {
                            // Parallel to the plane: its direction square to the
                            // plane's normal. Only then can it lie in it.
                            if (Math.Abs(line.Direction.DotProduct(p.Item3.Normal)) > square) continue;
                            var gap = Math.Max(Math.Abs(off(p.Item3, line.GetEndPoint(0))),
                                               Math.Abs(off(p.Item3, line.GetEndPoint(1))));
                            if (gap < exactly)
                            {
                                onIt = true;
                                string written = null;
                                try { written = curve.Reference == null ? null : curve.Reference.ConvertToStableRepresentation(doc); }
                                catch (Exception) { written = null; }
                                if (written == null)
                                    problems.Add("Revit gives no reference for the " + s.Item1 + " line from "
                                        + at(line.GetEndPoint(0)) + ", so it cannot be locked to \"" + p.Item4 + "\".");
                                else if (view == null)
                                {
                                    if (!noView.Contains(s.Item1)) noView.Add(s.Item1);
                                }
                                else
                                {
                                    jobs.Add(Tuple.Create(p, s.Item1, view, "the " + s.Item1 + " line from "
                                        + at(line.GetEndPoint(0)) + " to " + at(line.GetEndPoint(1)), written,
                                        doc.GetElement(curve.Reference) as CurveElement));
                                    any = true;
                                }
                            }
                            else near.Add(Tuple.Create(gap, "a " + s.Item1 + " line " + fineMm(gap) + " mm off"
                                + (gap < halfMillimetre ? " (closer than half a millimetre - a formula's remainder?)" : "")));
                        }
                        else if (arc != null)
                        {
                            // One lock per centre: the two halves of a circle share
                            // it, to within Revit's rounding (LABEL_FAMILY_RADIUS
                            // groups them at half a millimetre too).
                            if (centres.Any(c => c.IsAlmostEqualTo(arc.Center, halfMillimetre))) continue;
                            var gap = Math.Abs(off(p.Item3, arc.Center));
                            if (gap < exactly)
                            {
                                onIt = true;
                                centres.Add(arc.Center);
                                var drawn = arc.Reference == null ? null : doc.GetElement(arc.Reference) as CurveElement;
                                if (drawn == null)
                                    problems.Add("Revit gives no sketch line for the " + s.Item1 + " circle centred at "
                                        + at(arc.Center) + ", so its centre cannot be locked to \"" + p.Item4 + "\".");
                                else if (view == null)
                                {
                                    if (!noView.Contains(s.Item1)) noView.Add(s.Item1);
                                }
                                else
                                {
                                    jobs.Add(Tuple.Create(p, s.Item1, view, "the centre of the " + s.Item1
                                        + " circle (radius " + mm(arc.Radius) + " mm) at " + at(arc.Center), (string)null,
                                        drawn));
                                    any = true;
                                }
                            }
                            else near.Add(Tuple.Create(gap, "the centre of a " + s.Item1 + " circle (radius " + mm(arc.Radius) + " mm) "
                                + fineMm(gap) + " mm off" + (gap < halfMillimetre
                                    ? " (closer than half a millimetre - a formula's remainder?)" : "")));
                        }
                    }
            }
            if (!squareToOne)
                problems.Add("\"" + p.Item4 + "\" does not stand square to the form's " + string.Join(" or ",
                    sketches.Select(s => s.Item1)) + ", so nothing drawn in it can be aligned to it. "
                    + (target is Sweep || target is SweptBlend
                        // A sweep's faces locked were measured to stop every
                        // size change (2026-11-10) - never send one there.
                        ? "A sweep's ends follow its path; lock the path's lines to the planes they lie on instead."
                        : "A form's end or top is locked to a plane like that with LOCK_FORM_TO_PLANES."));
            else if (!any && !onIt)
                problems.Add("Nothing in the form's " + string.Join(" or ", sketches.Select(s => s.Item1)) + " lies on \""
                    + p.Item4 + "\" - no straight line in it, no circle's centre on it. "
                    + (near.Count == 0 ? "Nothing in the sketch runs along it." : "Nearest: "
                        + string.Join("; ", near.OrderBy(n => n.Item1).Select(n => n.Item2).Distinct().Take(6)) + ".")
                    + " A lock never moves what it locks - move the plane, or the sketch, onto the other first.");
        }
    }

    foreach (var word in noView)
        problems.Add("No plan, elevation or section looks straight at the form's " + word + ", where it is locked. Make "
            + "one facing it first.");
    if (problems.Count == 0 && jobs.Count == 0) problems.Add("Nothing in the form's sketch lies on the planes named.");
    if (problems.Count > 0) refused = "Nothing was locked. " + string.Join(" ", problems);
}

if (refused == null)
{
    // Locks already on the form's FACES - the ones that broke a sweep on
    // 2026-11-10 - are said, not removed.
    var faceLocks = new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>()
        .Count(d =>
        {
            try { return d.IsLocked && d.References.Cast<Reference>().Any(r => r.ElementId == target.Id); }
            catch (Exception) { return false; }
        });
    if (faceLocks > 0 && (target is Sweep || target is SweptBlend))
        findings.Add(faceLocks + " locked alignment(s) or dimension(s) already hold this sweep's own faces. On a sweep "
            + "they were measured to stop every size change (\"Can't create sweep\", 2026-11-10) - release them by "
            + "hand if the flex fails.");
    else if (faceLocks > 0)
        findings.Add(faceLocks + " locked alignment(s) or dimension(s) already hold this form's own faces; they are left "
            + "as they are.");

    // A LOCK ALREADY THERE is said and skipped: Revit refuses a second
    // alignment of the same two (LABEL_FAMILY_RADIUS met it on a re-run), and
    // one refusal would roll back the new locks with it.
    Func<ElementId, ElementId, bool> alreadyLocked = (planeId, curveId) =>
        new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>().Any(d =>
        {
            try
            {
                if (!d.IsLocked) return false;
                var ids = d.References.Cast<Reference>().Select(r => r.ElementId).ToList();
                return ids.Contains(planeId) && ids.Contains(curveId);
            }
            catch (Exception) { return false; }
        });
    var skipped = new List<string>();

    var start = target.get_BoundingBox(null);
    var centreMarks = 0;

    foreach (var job in jobs)
    {
        var p = job.Item1;
        if (job.Item6 != null && alreadyLocked(p.Item1.Id, job.Item6.Id))
        {
            skipped.Add(job.Item4 + " to \"" + p.Item4 + "\"");
            continue;
        }
        // A line's reference read now from its written form, not as it was
        // read before the earlier locks regenerated the family.
        Reference reference = null;
        if (job.Item5 != null)
        {
            try { reference = Reference.ParseFromStableRepresentation(doc, job.Item5); } catch (Exception) { reference = null; }
            if (reference == null)
                throw new InvalidOperationException("Revit no longer reads back " + job.Item4 + "'s reference after the "
                    + "locks before it. NOTHING from this call was kept.");
        }
        else
        {
            // A CENTRE. The arc's centre mark is switched on first if Revit
            // gives no centre reference with it hidden.
            var drawn = job.Item6;
            try { reference = drawn.CenterPointReference; } catch (Exception) { reference = null; }
            if (reference == null)
            {
                var mark = drawn.get_Parameter(BuiltInParameter.ARC_CENTER_MARK);
                if (mark != null && !mark.IsReadOnly && mark.AsInteger() == 0)
                {
                    mark.Set(1);
                    centreMarks++;
                    doc.Regenerate();
                    try { reference = drawn.CenterPointReference; } catch (Exception) { reference = null; }
                }
            }
            if (reference == null)
                throw new InvalidOperationException("Revit gives no reference for " + job.Item4 + ", even with its "
                    + "centre mark visible, so it cannot be locked to \"" + p.Item4 + "\". NOTHING from this call was kept.");
            reference = Reference.ParseFromStableRepresentation(doc, reference.ConvertToStableRepresentation(doc));
        }

        Dimension alignment = null;
        try { alignment = doc.FamilyCreate.NewAlignment(job.Item3, p.Item2, reference); }
        catch (Exception first)
        {
            // A LINE is asked again with the reference read straight off the
            // drawn sketch line, the other of the two ways to hold it (as
            // LABEL_FAMILY_RADIUS asks twice for a dimension). A centre has
            // only the one way, and is not asked again.
            Reference again = null;
            try { again = job.Item6 == null ? null : job.Item6.GeometryCurve.Reference; }
            catch (Exception) { again = null; }
            if (again == null || job.Item5 == null)
                throw new InvalidOperationException("Revit would not lock " + job.Item4 + " to \"" + p.Item4 + "\", in "
                    + job.Item3.Name + ": " + first.Message + " NOTHING from this call was kept.");
            try { alignment = doc.FamilyCreate.NewAlignment(job.Item3, p.Item2, again); }
            catch (Exception second)
            {
                throw new InvalidOperationException("Revit would not lock " + job.Item4 + " to \"" + p.Item4 + "\", in "
                    + job.Item3.Name + ": " + first.Message + " (and again with the reference read off the drawn line: "
                    + second.Message + ") NOTHING from this call was kept.");
            }
            findings.Add("Revit took " + job.Item4 + " only with the reference read off the drawn line, not from its "
                + "written form.");
        }
        if (alignment == null)
            throw new InvalidOperationException("Revit made no alignment for " + job.Item4 + " to \"" + p.Item4
                + "\". NOTHING from this call was kept.");
        if (!alignment.IsLocked) alignment.IsLocked = true;
        doc.Regenerate();
        if (!alignment.IsValidObject)
            throw new InvalidOperationException("Revit removed the alignment of " + job.Item4 + " to \"" + p.Item4
                + "\" when the family regenerated - it could not hold it. NOTHING from this call was kept.");
        if (!alignment.IsLocked)
            throw new InvalidOperationException("The alignment of " + job.Item4 + " to \"" + p.Item4 + "\" reads back "
                + "unlocked. NOTHING from this call was kept.");
        locked.Add(job.Item4 + " to \"" + p.Item4 + "\" (in " + job.Item3.Name + ")");
    }

    if (locked.Count == 0 && skipped.Count > 0)
        findings.Add("Every lock asked for was already there - nothing new was made.");

    // READ BACK: still a solid, and nowhere else than it was. A void is not
    // asked for a solid: whether a cutting void gives one back is not known.
    var solid = !target.IsSolid;
    var geometry = target.get_Geometry(new Options());
    if (geometry != null)
        foreach (GeometryObject piece in geometry)
        {
            var body = piece as Solid;
            if (body != null && body.Volume > 0) { solid = true; break; }
        }
    if (!solid)
        throw new InvalidOperationException("After the locks the form has no solid - Revit could not build it from its "
            + "locked sketch. NOTHING from this call was kept.");
    var end = target.get_BoundingBox(null);
    if (start == null || end == null)
        throw new InvalidOperationException("The form has no extent to read back. NOTHING from this call was kept.");
    var letters = new[] { "X", "Y", "Z" };
    for (var i = 0; i < 3; i++)
    {
        Func<XYZ, double> along = q => i == 0 ? q.X : i == 1 ? q.Y : q.Z;
        if (Math.Abs(along(start.Min) - along(end.Min)) > halfMillimetre
            || Math.Abs(along(start.Max) - along(end.Max)) > halfMillimetre)
            throw new InvalidOperationException("Locking moved the form: " + letters[i] + " " + mm(along(start.Min))
                + " to " + mm(along(start.Max)) + " mm before, " + mm(along(end.Min)) + " to " + mm(along(end.Max))
                + " mm after. A lock never moves what it locks. NOTHING from this call was kept.");
    }

    findings.Add("Locked " + locked.Count + " sketch line(s) and centre(s)" + (locked.Count == 0 ? "." : ": "
        + string.Join("; ", locked) + "."));
    if (skipped.Count > 0)
        findings.Add("Already locked, left as it was: " + string.Join("; ", skipped) + ".");
    if (centreMarks > 0)
        findings.Add("\"Center Mark Visible\" was switched on for " + centreMarks + " arc(s), as it is by hand, so "
            + "their centres could be picked.");
    findings.Add("Locked is not yet PROVED to follow the planes. FLEX_FAMILY changes the sizes, reads the form back, "
        + "and puts them back.");
}

if (refused != null) findings.Add(refused);
