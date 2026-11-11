// NOT STANDALONE. Assumes `doc`, `uidoc`, `lines` and `planes` are in scope;
// leaves `locked`, `ends`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ALIGN AND LOCK a family's 2D LINES to named reference planes, so they stretch
// when a labelled dimension moves the planes: detail lines and symbolic lines
// already drawn, and the boundary lines of a filled region's sketch. Two kinds
// of lock, both what a modeller does by hand with Align and the padlock:
//
//   ALONG - a straight line lying ON a plane named is locked along it, as
//   DRAW_FAMILY_DETAIL_LINES does for the lines it draws, here for lines
//   already there;
//   AT AN END - a line's END lying on a plane that CROSSES the line is locked
//   to it, so the end goes where the plane goes. That is what makes a slanted
//   line - an arrow head's edge - follow two planes it touches only at its ends.
//
// WRITTEN 2026-11-11 for a detail item's flow arrow (row 5b-1493): tail at the
// origin pointing +X, shaft to the head base, a solid triangular head - the
// head base and tip planes labelled from Length. Nothing before this card locked
// a line's END, and nothing reached a filled region's sketch.
//
// A FILLED REGION'S SKETCH: its lines are joined at their corners, so each
// corner is locked ONCE per plane - and not at all to a plane another line of
// the same sketch through that corner is already locked along. A corner locked
// twice to one plane is a constraint said twice, which Revit may refuse.
// Detail and symbolic lines are separate elements: every end is locked on its
// own. Whether Revit keeps a region's corners joined when one line moves is
// NEEDS-CHECKING NC, not a promise.
//
// HOW THE REFERENCES ARE REACHED - with no sketch-editing scope (SketchEditScope
// refuses a family document), as LOCK_FORM_SKETCH_TO_PLANES reaches a sweep's
// path line, PROVED on Revit 2020 on 2026-11-10: a curve's own reference for an
// ALONG lock; for an END, Curve.GetEndPointReference, written down at once and
// read back at lock time, after the locks before it regenerated the family. An
// END reference aligned to a plane has never been run anywhere in Heron - only a
// circle's centre point has (LOCK_FORM_SKETCH_TO_PLANES NA2). NEEDS-CHECKING NC1
// is that question.
//
// LOCKED ONLY WHERE IT ALREADY LIES. A plane named that nothing lies along and
// nothing ends on refuses the whole call, naming the nearest ends and how far
// off - a lock never moves what it locks. The tolerance is the one measured for
// faces (row 5b-372), not yet for lines.
//
// IN THE LINE'S OWN VIEW - a detail line's or a region's owner view; a symbolic
// line has none, so the plan or elevation looking straight at it. The plane
// named must be seen edge on there.
//
// ALL OR NOTHING, READ BACK. Every lock reads back locked; every line's ends
// read back where they were. Any refusal from Revit fails the call with nothing
// kept. Locked is not yet proved to FOLLOW: change the length parameter and run
// this again - every lock said as already there, and the ends read back where
// the planes now are.

var findings = new List<string>();
var locked = new List<string>();
var ends = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> fineMm = feet => Math.Round(feet * 304.8, 4).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
// The largest gap Revit was measured to accept for a FACE (row 5b-372).
var exactly = 3e-7 / 304.8;
var square = 1e-6;

Func<XYZ, string> at = p => "(" + mm(p.X) + ", " + mm(p.Y) + (Math.Abs(p.Z) < halfMillimetre ? "" : ", " + mm(p.Z)) + ") mm";

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

Func<Plane, XYZ, double> off = (plane, point) => plane.Normal.DotProduct(point - plane.Origin);

// (element, reference, plane, name) for every named reference plane and every level.
var datums = new List<Tuple<Element, Reference, Plane, string>>();
// One line to lock: (what to call it, its view, the curve as read, the drawn
// line, the sketch it belongs to or null).
var pieces = new List<Tuple<string, View, Curve, CurveElement, Sketch>>();
// One lock: (plane, view, what it is, the reference's written form, the drawn
// line, the end - -1 for ALONG - and the drawn lines through that corner).
var jobs = new List<Tuple<Tuple<Element, Reference, Plane, string>, View, string, string, CurveElement, int, List<ElementId>>>();
var problems = new List<string>();
var wantedPlanes = new List<Tuple<Element, Reference, Plane, string>>();
var targets = new List<Element>();
var freeEnds = 0;
var freeCorners = new List<Tuple<ElementId, XYZ>>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's lines are locked "
        + "to its planes inside the family - open it first.";
}
else
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

    // Every line that belongs to a sketch - a form's, a region's - so "all"
    // never takes a sketch's line for a loose detail line.
    var inSketch = new HashSet<ElementId>();
    foreach (var sketch in new FilteredElementCollector(doc).OfClass(typeof(Sketch)).Cast<Sketch>())
    {
        try
        {
            foreach (CurveArray loop in sketch.Profile)
                foreach (Curve curve in loop)
                    if (curve.Reference != null) inSketch.Add(curve.Reference.ElementId);
        }
        catch (Exception) { }
    }

    Func<Element, bool> isLoose = e => (e is DetailCurve || e is SymbolicCurve) && !inSketch.Contains(e.Id);

    var said = (lines ?? "").Trim();
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the lines by their ids.");
        else
        {
            var ids = uidoc.Selection.GetElementIds().ToList();
            if (ids.Count == 0) problems.Add("Nothing is selected. Select the lines and filled regions to lock.");
            targets.AddRange(ids.Select(id => doc.GetElement(id)).Where(e => e != null));
        }
    }
    else if (string.Equals(said, "all", StringComparison.OrdinalIgnoreCase))
    {
        targets.AddRange(new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).Cast<Element>().Where(isLoose));
        targets.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FilledRegion)).Cast<Element>());
        if (targets.Count == 0) problems.Add("This family has no detail line, symbolic line or filled region to lock.");
    }
    else if (said.Length == 0) problems.Add("No lines were named - give their ids, \"selected\" or \"all\".");
    else
    {
        foreach (var id in said.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).Distinct())
        {
            var e = doc.GetElement(id);
            if (e == null) problems.Add("No element in this family has the id \"" + id + "\".");
            else targets.Add(e);
        }
    }

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.CeilingPlan
            || v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section || v.ViewType == ViewType.Detail
            || v.ViewType == ViewType.DraftingView))
        .ToList();
    // The view looking straight at a plane: a floor plan first, then an elevation.
    Func<XYZ, View> viewFacing = normal => views
        .Where(v => Math.Abs(Math.Abs(v.ViewDirection.DotProduct(normal)) - 1) < square)
        .OrderBy(v => v.ViewType == ViewType.FloorPlan ? 0 : v.ViewType == ViewType.Elevation ? 1 : 2)
        .FirstOrDefault();
    Func<Element, View> ownView = e =>
        e.OwnerViewId == null || e.OwnerViewId == ElementId.InvalidElementId ? null : doc.GetElement(e.OwnerViewId) as View;

    var regions = 0;
    foreach (var e in targets.Distinct())
    {
        var region = e as FilledRegion;
        var curveElement = e as CurveElement;
        if (region != null)
        {
            regions++;
            Sketch sketch = null;
            try
            {
                var sketchId = region.GetDependentElements(new ElementClassFilter(typeof(Sketch))).FirstOrDefault();
                sketch = sketchId == null ? null : doc.GetElement(sketchId) as Sketch;
            }
            catch (Exception) { sketch = null; }
            if (sketch == null)
            {
                problems.Add("Revit gives no sketch back for the filled region " + e.UniqueId + ", so its edges cannot be "
                    + "locked. Lock them by hand in Edit Boundary.");
                continue;
            }
            var view = ownView(region) ?? viewFacing(sketch.SketchPlane.GetPlane().Normal);
            var edge = 0;
            foreach (CurveArray loop in sketch.Profile)
                foreach (Curve curve in loop)
                {
                    edge++;
                    var drawn = curve.Reference == null ? null : doc.GetElement(curve.Reference) as CurveElement;
                    if (drawn == null)
                        problems.Add("Revit gives no line for edge " + edge + " of the filled region " + e.UniqueId
                            + ", so it cannot be locked.");
                    else pieces.Add(Tuple.Create("the region's edge from " + at(curve.GetEndPoint(0)) + " to "
                        + at(curve.GetEndPoint(1)), view, curve, drawn, sketch));
                }
        }
        else if (curveElement != null && isLoose(curveElement))
        {
            var curve = curveElement.GeometryCurve;
            var view = ownView(curveElement);
            if (view == null && curveElement.SketchPlane != null) view = viewFacing(curveElement.SketchPlane.GetPlane().Normal);
            var kind = curveElement is SymbolicCurve ? "symbolic line" : "detail line";
            if (curve == null || !curve.IsBound)
                problems.Add("The " + kind + " " + e.UniqueId + " is a closed curve with no ends - a circle or an "
                    + "ellipse - and only lines and arcs with ends are locked here.");
            else pieces.Add(Tuple.Create("the " + kind + " from " + at(curve.GetEndPoint(0)) + " to "
                + at(curve.GetEndPoint(1)), view, curve, curveElement, (Sketch)null));
        }
        else if (curveElement != null)
        {
            // A sketch line: a filled region's edge, or a form's.
            var owner = new FilteredElementCollector(doc).OfClass(typeof(FilledRegion)).Cast<FilledRegion>()
                .FirstOrDefault(r =>
                {
                    try
                    {
                        var sid = r.GetDependentElements(new ElementClassFilter(typeof(Sketch))).FirstOrDefault();
                        var sk = sid == null ? null : doc.GetElement(sid) as Sketch;
                        if (sk == null) return false;
                        foreach (CurveArray loop in sk.Profile)
                            foreach (Curve c in loop)
                                if (c.Reference != null && c.Reference.ElementId == curveElement.Id) return true;
                        return false;
                    }
                    catch (Exception) { return false; }
                });
            problems.Add(owner != null
                ? "The line " + e.UniqueId + " is an edge of the filled region " + owner.UniqueId + ". Name the filled "
                    + "region instead - its edges are locked together, a corner once."
                : "The line " + e.UniqueId + " belongs to a form's sketch. A form's sketch is locked with "
                    + "LOCK_FORM_SKETCH_TO_PLANES.");
        }
        else
            problems.Add("\"" + (e.Name ?? e.UniqueId) + "\" is not a detail line, a symbolic line or a filled region. "
                + (e is GenericForm ? "A form is locked with LOCK_FORM_TO_PLANES or LOCK_FORM_SKETCH_TO_PLANES."
                    : e is FamilyInstance ? "A nested family is locked with LOCK_NESTED_FAMILY_TO_PLANES." : ""));
    }
    foreach (var piece in pieces.Where(p => p.Item2 == null).Select(p => p.Item1).Distinct().Take(3))
        problems.Add("No view looks straight at " + piece + ", where it would be locked. Make a plan or elevation "
            + "facing it first.");

    var names = (planes ?? new List<string>()).Select(n => (n ?? "").Trim()).Where(n => n.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    if (names.Count == 0) problems.Add("No plane was named to lock the lines to.");
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

    Func<Curve, int, string> writtenEnd = (curve, i) =>
    {
        try
        {
            var r = curve.GetEndPointReference(i);
            return r == null ? null : r.ConvertToStableRepresentation(doc);
        }
        catch (Exception) { return null; }
    };
    Func<Curve, string> writtenLine = curve =>
    {
        try { return curve.Reference == null ? null : curve.Reference.ConvertToStableRepresentation(doc); }
        catch (Exception) { return null; }
    };

    if (problems.Count == 0)
    {
        // ALONG first, so a region's corner on a line already locked along a
        // plane is not locked to that plane a second time.
        // (sketch id, corner, plane id) already held.
        var held = new List<Tuple<ElementId, XYZ, ElementId>>();
        Func<Sketch, XYZ, Element, bool> isHeld = (sk, point, plane) =>
            held.Any(h => h.Item1 == sk.Id && h.Item3 == plane.Id && h.Item2.IsAlmostEqualTo(point, halfMillimetre));

        var planeHits = wantedPlanes.ToDictionary(p => p.Item4, p => false);
        var planeSquare = wantedPlanes.ToDictionary(p => p.Item4, p => false);
        var near = wantedPlanes.ToDictionary(p => p.Item4, p => new List<Tuple<double, string>>());

        foreach (var p in wantedPlanes)
            foreach (var piece in pieces)
            {
                if (Math.Abs(p.Item3.Normal.DotProduct(piece.Item2.ViewDirection)) > square) continue;
                planeSquare[p.Item4] = true;
                var line = piece.Item3 as Line;
                if (line == null || Math.Abs(line.Direction.DotProduct(p.Item3.Normal)) > square) continue;
                var gap = Math.Max(Math.Abs(off(p.Item3, line.GetEndPoint(0))), Math.Abs(off(p.Item3, line.GetEndPoint(1))));
                if (gap >= exactly) continue;
                planeHits[p.Item4] = true;
                var written = writtenLine(piece.Item3);
                if (written == null)
                {
                    problems.Add("Revit gives no reference for " + piece.Item1 + ", so it cannot be locked along \""
                        + p.Item4 + "\".");
                    continue;
                }
                jobs.Add(Tuple.Create(p, piece.Item2, piece.Item1 + " along", written, piece.Item4, -1,
                    new List<ElementId> { piece.Item4.Id }));
                if (piece.Item5 != null)
                {
                    held.Add(Tuple.Create(piece.Item5.Id, line.GetEndPoint(0), p.Item1.Id));
                    held.Add(Tuple.Create(piece.Item5.Id, line.GetEndPoint(1), p.Item1.Id));
                }
            }

        foreach (var piece in pieces)
            for (var i = 0; i < 2; i++)
            {
                var point = piece.Item3.GetEndPoint(i);
                var onAny = false;
                foreach (var p in wantedPlanes)
                {
                    if (Math.Abs(p.Item3.Normal.DotProduct(piece.Item2.ViewDirection)) > square) continue;
                    var gap = Math.Abs(off(p.Item3, point));
                    var line = piece.Item3 as Line;
                    var along = line != null && Math.Abs(line.Direction.DotProduct(p.Item3.Normal)) <= square;
                    if (gap >= exactly)
                    {
                        if (!along) near[p.Item4].Add(Tuple.Create(gap, "an end of " + piece.Item1 + " " + fineMm(gap)
                            + " mm off" + (gap < halfMillimetre ? " (closer than half a millimetre - a formula's remainder?)" : "")));
                        continue;
                    }
                    onAny = true;
                    // A line lying along the plane is locked ALONG it above,
                    // never at its ends.
                    if (along) continue;
                    planeHits[p.Item4] = true;
                    if (piece.Item5 != null && isHeld(piece.Item5, point, p.Item1)) continue;
                    var written = writtenEnd(piece.Item3, i);
                    if (written == null)
                    {
                        problems.Add("Revit gives no reference for the end " + at(point) + " of " + piece.Item1
                            + ", so it cannot be locked to \"" + p.Item4 + "\".");
                        continue;
                    }
                    // Every drawn line of the same sketch through this corner -
                    // a lock already on any of them holds the corner.
                    var through = piece.Item5 == null ? new List<ElementId> { piece.Item4.Id }
                        : pieces.Where(q => q.Item5 != null && q.Item5.Id == piece.Item5.Id
                                && (q.Item3.GetEndPoint(0).IsAlmostEqualTo(point, halfMillimetre)
                                    || q.Item3.GetEndPoint(1).IsAlmostEqualTo(point, halfMillimetre)))
                            .Select(q => q.Item4.Id).Distinct().ToList();
                    jobs.Add(Tuple.Create(p, piece.Item2, (piece.Item5 == null ? "the end " + at(point) + " of " + piece.Item1
                        : "the region's corner " + at(point)), written, piece.Item4, i, through));
                    if (piece.Item5 != null) held.Add(Tuple.Create(piece.Item5.Id, point, p.Item1.Id));
                }
                // A region's corner is two edges' ends: counted once.
                if (!onAny && (piece.Item5 == null || !freeCorners.Any(c => c.Item1 == piece.Item5.Id
                        && c.Item2.IsAlmostEqualTo(point, halfMillimetre))))
                {
                    freeEnds++;
                    if (piece.Item5 != null) freeCorners.Add(Tuple.Create(piece.Item5.Id, point));
                }
            }

        foreach (var p in wantedPlanes)
        {
            if (!planeSquare[p.Item4])
                problems.Add("\"" + p.Item4 + "\" is not seen edge on in the view the lines are in, so nothing drawn "
                    + "there can be aligned to it.");
            else if (!planeHits[p.Item4])
                problems.Add("Nothing lies along \"" + p.Item4 + "\" and nothing ends on it. " + (near[p.Item4].Count == 0
                    ? "No line end comes near it." : "Nearest: " + string.Join("; ", near[p.Item4].OrderBy(n => n.Item1)
                        .Select(n => n.Item2).Distinct().Take(6)) + ".")
                    + " A lock never moves what it locks - move the plane, or the line, onto the other first.");
        }
        if (problems.Count == 0 && jobs.Count == 0)
            problems.Add("Everything on the planes named is already held by another lock in its sketch - nothing to lock.");
    }
    if (problems.Count > 0) refused = "Nothing was locked. " + string.Join(" ", problems);
    else if (regions > 0)
        findings.Add("A filled region's corners are locked once per plane: its lines are joined there, so the corner "
            + "carries both. Whether Revit keeps them joined as one line moves is not yet proved.");
}

if (refused == null)
{
    // A LOCK ALREADY THERE is said and skipped: Revit refuses a second
    // alignment of the same two (LABEL_FAMILY_RADIUS met it on a re-run), and one
    // refusal would roll back the new locks with it. An END lock and an ALONG
    // lock of one line to one plane cannot both be asked - a line along a plane
    // is never locked at its ends to it - so the plane and the line say which.
    // Only locks made BEFORE this call count: an ARC with both ends on one
    // crossing plane asks two locks of the same arc to the same plane, and the
    // first, made here, must not stand in for the second. (An arc that had one
    // end locked by hand before and not the other is still taken as locked -
    // the plane and the arc do not say which end; said in row 5b-1493.)
    var earlier = new HashSet<ElementId>(new FilteredElementCollector(doc).OfClass(typeof(Dimension)).ToElementIds());
    Func<ElementId, List<ElementId>, bool> alreadyLocked = (planeId, curveIds) =>
        new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>().Any(d =>
        {
            try
            {
                if (!earlier.Contains(d.Id) || !d.IsLocked) return false;
                var ids = d.References.Cast<Reference>().Select(r => r.ElementId).ToList();
                return ids.Contains(planeId) && curveIds.Any(ids.Contains);
            }
            catch (Exception) { return false; }
        });
    var skipped = new List<string>();

    // Every line's ends before, to read back after.
    var before = pieces.Select(p => Tuple.Create(p.Item1, p.Item4, p.Item3.GetEndPoint(0), p.Item3.GetEndPoint(1))).ToList();

    foreach (var job in jobs)
    {
        var p = job.Item1;
        if (alreadyLocked(p.Item1.Id, job.Item7))
        {
            skipped.Add(job.Item3 + " to \"" + p.Item4 + "\"");
            continue;
        }
        Reference reference = null;
        try { reference = Reference.ParseFromStableRepresentation(doc, job.Item4); } catch (Exception) { reference = null; }
        if (reference == null)
            throw new InvalidOperationException("Revit no longer reads back " + job.Item3 + "'s reference after the locks "
                + "before it. NOTHING from this call was kept.");

        Dimension alignment = null;
        try { alignment = doc.FamilyCreate.NewAlignment(job.Item2, p.Item2, reference); }
        catch (Exception first)
        {
            // Asked again with the reference read straight off the drawn line,
            // the other of the two ways to hold it.
            Reference again = null;
            try
            {
                var curve = job.Item5.GeometryCurve;
                again = curve == null ? null : job.Item6 < 0 ? curve.Reference : curve.GetEndPointReference(job.Item6);
            }
            catch (Exception) { again = null; }
            if (again == null)
                throw new InvalidOperationException("Revit would not lock " + job.Item3 + " to \"" + p.Item4 + "\", in "
                    + job.Item2.Name + ": " + first.Message + " NOTHING from this call was kept.");
            try { alignment = doc.FamilyCreate.NewAlignment(job.Item2, p.Item2, again); }
            catch (Exception second)
            {
                throw new InvalidOperationException("Revit would not lock " + job.Item3 + " to \"" + p.Item4 + "\", in "
                    + job.Item2.Name + ": " + first.Message + " (and again with the reference read off the drawn line: "
                    + second.Message + ") NOTHING from this call was kept.");
            }
            findings.Add("Revit took " + job.Item3 + " only with the reference read off the drawn line, not from its "
                + "written form.");
        }
        if (alignment == null)
            throw new InvalidOperationException("Revit made no alignment for " + job.Item3 + " to \"" + p.Item4
                + "\". NOTHING from this call was kept.");
        if (!alignment.IsLocked) alignment.IsLocked = true;
        doc.Regenerate();
        if (!alignment.IsValidObject)
            throw new InvalidOperationException("Revit removed the alignment of " + job.Item3 + " to \"" + p.Item4
                + "\" when the family regenerated - it could not hold it. NOTHING from this call was kept.");
        if (!alignment.IsLocked)
            throw new InvalidOperationException("The alignment of " + job.Item3 + " to \"" + p.Item4 + "\" reads back "
                + "unlocked. NOTHING from this call was kept.");
        locked.Add(job.Item3 + " to \"" + p.Item4 + "\" (in " + job.Item2.Name + ")");
    }

    // READ BACK: every line still there, its ends where they were.
    var report = new List<string>();
    foreach (var b in before)
    {
        if (!b.Item2.IsValidObject)
            throw new InvalidOperationException("Locking removed " + b.Item1 + ". NOTHING from this call was kept.");
        var curve = b.Item2.GeometryCurve;
        if (curve == null)
            throw new InvalidOperationException(b.Item1 + " has no curve to read back after the locks. NOTHING from this "
                + "call was kept.");
        var a = curve.GetEndPoint(0);
        var z = curve.GetEndPoint(1);
        var same = (a.IsAlmostEqualTo(b.Item3, halfMillimetre) && z.IsAlmostEqualTo(b.Item4, halfMillimetre))
            || (a.IsAlmostEqualTo(b.Item4, halfMillimetre) && z.IsAlmostEqualTo(b.Item3, halfMillimetre));
        if (!same)
            throw new InvalidOperationException("Locking moved " + b.Item1 + ": it now runs " + at(a) + " to " + at(z)
                + ". A lock never moves what it locks. NOTHING from this call was kept.");
        report.Add(at(a) + " to " + at(z));
    }
    ends = string.Join("; ", report.Distinct());

    if (locked.Count == 0 && skipped.Count > 0)
        findings.Add("Every lock asked for was already there - nothing new was made.");
    findings.Add("Locked " + locked.Count + " line(s) and end(s)" + (locked.Count == 0 ? "." : ": "
        + string.Join("; ", locked) + "."));
    if (skipped.Count > 0)
        findings.Add("Already locked, left as it was: " + string.Join("; ", skipped) + ".");
    if (freeEnds > 0)
        findings.Add(freeEnds + " line end(s) lie on none of the planes named. Each stays where it is when the planes "
            + "move, unless the line through it is locked along a plane, or a region's corner carries it.");
    findings.Add("Read back - every line runs: " + ends + ".");
    findings.Add("Locked is not yet PROVED to follow the planes. Change the length in Family Types and run this again: "
        + "every lock is said as already there, and the ends read back where the planes now are.");
}

if (refused != null) findings.Add(refused);
