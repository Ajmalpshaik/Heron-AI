// NOT STANDALONE. Assumes `doc`, `uidoc` and `forms` are in scope; leaves
// `combinationId`, `combined`, `cut`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// JOIN SOLIDS, OR CUT A SOLID WITH A VOID, in the family open in the Family
// Editor - Revit's Join Geometry and Cut Geometry there - by combining the
// forms named into ONE geometry combination. Solids in it are joined; a void in
// it cuts them.
//
// THIS IS THE ONLY ROUTE THE API HAS IN AN ORDINARY FAMILY, and its own remarks
// say why, 2020 to 2027: the solid-cut utility refuses any document that is not
// a project, a conceptual mass, a curtain panel or an adaptive component, and
// the join utility says it "is not available for family documents". A geometry
// combination is "created by Join and Cut operations ... in a family
// document", and Document.CombineElements makes one.
//
// A VOID MADE THROUGH THE API CUTS NOTHING UNTIL THIS. A void drawn by hand
// cuts what it touches when its sketch is finished; one made by the API does
// not - which is what an earlier family build measured five ways and called a
// void that never cut.
//
// THE CUT IS MEASURED, NOT ASSUMED. The solids' volume is read before, and the
// combination's after: a void that took nothing away fails the call, and so
// does a join that came out bigger than its parts. Nothing is kept from a call
// that fails.
//
// AND EVERY SOLID NAMED MUST BE IN WHAT REVIT MADE (5b-370). Revit leaves out
// of a combination any form its join or cut fails on, and still lists it as a
// member: the form's own geometry is gone and the combination's holds only the
// rest. It says "Can't keep elements joined" only when the transaction commits,
// after this has measured. Measured 2026-10-07 on a scratch family, Revit 2024:
// 24 stacked 15-degree revolves of a coil and two end voids - every other
// revolve was left out, and the call reported half the coil "taken away" by
// two voids that grind its ends. With EVERY solid left out the combination
// holds no solid at all - "no solid in it to measure", as the owner's Family1
// coil and Family11 seat threw. The seat was reproduced with a void whose
// sides lie on its faces and whose corner arcs were typed to whole millimetres;
// with those sides a millimetre past the faces it cut. So points inside each solid
// are taken BEFORE combining, and each one that no void covers must be inside
// the result afterwards. Inside is judged by counting where rays from the point
// cross the solid's faces - a void's own geometry is an inside-out solid, and
// Revit's inside/outside answer for one reads backwards.
//
// A FORM ALREADY IN A COMBINATION IS NAMED BY ITS COMBINATION. Revit's remarks:
// a solid belongs to at most one, so naming a member of one would quietly
// merge two; it is refused with the combination's id instead. A VOID may cut
// several combinations - run this once per body to cut two bodies with one void
// and keep them apart.

var findings = new List<string>();
var combinationId = "";
var combined = "";
var cut = false;
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> litres = cubicFeet => Math.Round(cubicFeet * 28.316846592, 3).ToString(invariant);
// One cubic centimetre, in cubic feet - less than any cut worth making.
var crumb = 1.0 / (304.8 * 304.8 * 304.8) * 1000.0;

Func<Element, double> volumeOf = element =>
{
    var total = 0.0;
    var geometry = element.get_Geometry(new Options());
    if (geometry == null) return 0.0;
    foreach (GeometryObject piece in geometry)
    {
        var body = piece as Solid;
        if (body != null && body.Volume > 0) total += body.Volume;
    }
    return total;
};

Func<Element, string> describe = element =>
{
    var form = element as GenericForm;
    if (form == null) return "the combination " + element.UniqueId;
    var kind = element is Extrusion ? "extrusion" : element is Revolution ? "revolve" : element is Blend ? "blend"
        : element is SweptBlend ? "swept blend" : element is Sweep ? "sweep" : "form";
    return (form.IsSolid ? "solid " : "void ") + kind + " " + element.UniqueId;
};

var members = new List<Element>();
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. Forms are joined and "
        + "cut inside the family - open it first.";
}
else
{
    var said = (forms ?? "").Trim();
    var named = new List<Element>();
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the forms by their ids.");
        else
            foreach (var id in uidoc.Selection.GetElementIds())
            {
                var element = doc.GetElement(id);
                if (element != null) named.Add(element);
            }
    }
    else
    {
        foreach (var part in said.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var element = doc.GetElement(part);
            if (element == null) problems.Add("No element in this family has the id \"" + part + "\".");
            else named.Add(element);
        }
    }

    foreach (var element in named)
    {
        if (members.Any(m => m.Id == element.Id)) continue;
        var combinable = element as CombinableElement;
        if (combinable == null || !(element is GenericForm || element is GeomCombination))
        {
            problems.Add("\"" + (element.Name ?? element.UniqueId) + "\" (" + element.UniqueId + ") is not a form - "
                + "an extrusion, revolve, blend, sweep or swept blend - nor a join of them.");
            continue;
        }
        var form = element as GenericForm;
        if (form != null && form.IsSolid)
        {
            GeomCombination already = null;
            foreach (GeomCombination one in combinable.Combinations) { already = one; break; }
            if (already != null)
            {
                problems.Add("The " + describe(element) + " is already joined or cut in the combination "
                    + already.UniqueId + " - name that combination instead, so the two are not merged without "
                    + "being asked.");
                continue;
            }
        }
        members.Add(element);
    }

    if (problems.Count == 0 && members.Count < 2)
        problems.Add((members.Count == 0 ? "Nothing was named" : "Only one form was named") + " - joining or "
            + "cutting needs at least two forms. Name them by the ids the form tools gave back, commas between, or "
            + "select them and say \"selected\".");

    if (problems.Count == 0
        && members.All(m => m is GenericForm && !((GenericForm)m).IsSolid))
        problems.Add("Every form named is a void. A void cuts a solid - name the solid it should cut as well.");

    if (problems.Count > 0) refused = "Nothing was joined or cut. " + string.Join(" ", problems);
}

if (refused == null)
{
    var solids = members.Where(m => !(m is GenericForm) || ((GenericForm)m).IsSolid).ToList();
    var voids = members.Where(m => m is GenericForm && !((GenericForm)m).IsSolid).ToList();
    cut = voids.Count > 0;

    var before = solids.Sum(s => volumeOf(s));
    if (before <= 0)
        throw new InvalidOperationException("The solids named hold no volume to measure, so nothing could prove a "
            + "join or a cut. NOTHING from this call was kept.");

    // POINTS INSIDE EACH SOLID, TAKEN BEFORE COMBINING - afterwards a member's
    // own geometry is empty. One per face, from the chord through the solid
    // along the face's normal, so every separate lump of a form is sampled.
    var touching = 0.01 / 304.8;
    var rays = new[] { new XYZ(0.5773, 0.5774, 0.5775).Normalize(), new XYZ(-0.6, 0.48, 0.64).Normalize(),
        new XYZ(0.31, -0.71, 0.63).Normalize() };
    Func<Element, List<Solid>> bodiesOf = element =>
    {
        var bodies = new List<Solid>();
        var geometry = element.get_Geometry(new Options());
        if (geometry == null) return bodies;
        foreach (GeometryObject piece in geometry)
        {
            var body = piece as Solid;
            if (body != null && Math.Abs(body.Volume) > 0) bodies.Add(SolidUtils.Clone(body));
        }
        return bodies;
    };
    // INSIDE, BY COUNTING CROSSINGS: an odd number of face crossings between the
    // point and a far point means inside, whichever way the faces are turned. Two
    // of three skewed rays must agree, so one that grazes an edge is outvoted.
    Func<Solid, XYZ, bool> holds = (body, point) =>
    {
        var box = body.GetBoundingBox();
        var local = box.Transform.Inverse.OfPoint(point);
        if (local.X < box.Min.X - touching || local.Y < box.Min.Y - touching || local.Z < box.Min.Z - touching
            || local.X > box.Max.X + touching || local.Y > box.Max.Y + touching || local.Z > box.Max.Z + touching)
            return false;
        var reach = box.Max.DistanceTo(box.Min) + 1.0;
        var votes = 0;
        foreach (var ray in rays)
        {
            var far = point + ray * reach;
            var hit = body.IntersectWithCurve(Line.CreateBound(point, far), new SolidCurveIntersectionOptions());
            var crossings = 0;
            for (var i = 0; i < hit.SegmentCount; i++)
            {
                var piece = hit.GetCurveSegment(i);
                for (var end = 0; end < 2; end++)
                {
                    var at = piece.GetEndPoint(end);
                    if (at.DistanceTo(point) > touching && at.DistanceTo(far) > touching) crossings++;
                }
            }
            if (crossings % 2 == 1) votes++;
        }
        return votes >= 2;
    };
    Func<Solid, List<XYZ>> pointsIn = body =>
    {
        var points = new List<XYZ>();
        var box = body.GetBoundingBox();
        var across = box.Max.DistanceTo(box.Min) + 1.0;
        foreach (Face face in body.Faces)
        {
            if (points.Count >= 24) break;
            var mesh = face.Triangulate();
            if (mesh == null || mesh.NumTriangles == 0) continue;
            var corner = mesh.get_Triangle(0);
            var near = face.Project((corner.get_Vertex(0) + corner.get_Vertex(1) + corner.get_Vertex(2)) / 3.0);
            if (near == null) continue;
            var normal = face.ComputeNormal(near.UVPoint);
            if (normal.IsZeroLength()) continue;
            normal = normal.Normalize();
            var hit = body.IntersectWithCurve(Line.CreateBound(near.XYZPoint - normal * across,
                near.XYZPoint + normal * across), new SolidCurveIntersectionOptions());
            Curve chord = null;
            var gap = double.MaxValue;
            for (var i = 0; i < hit.SegmentCount; i++)
            {
                var piece = hit.GetCurveSegment(i);
                var off = piece.Distance(near.XYZPoint);
                if (off < gap) { gap = off; chord = piece; }
            }
            if (chord != null && gap < 1.0 / 304.8) points.Add(chord.Evaluate(0.5, true));
        }
        return points;
    };
    var voidBodies = voids.SelectMany(bodiesOf).ToList();
    var samples = solids.Select(s => new KeyValuePair<Element, List<XYZ>>(s, bodiesOf(s).SelectMany(pointsIn).ToList()))
        .ToList();
    var unsampled = samples.Where(kv => kv.Value.Count == 0).Select(kv => describe(kv.Key)).ToList();
    if (unsampled.Count > 0)
        throw new InvalidOperationException("No point inside " + string.Join(", ", unsampled) + " could be found to "
            + "check it by, so nothing could prove it ends up in the combination. NOTHING from this call was kept.");
    // A point a void covers may rightly be gone; every other one must stay.
    var watched = samples.Select(kv => new KeyValuePair<Element, List<XYZ>>(kv.Key,
        kv.Value.Where(p => !voidBodies.Any(v => holds(v, p))).ToList())).ToList();

    // ONE COMBINATION OF THE FORMS NAMED, regenerated so what it holds can be read.
    Func<GeomCombination> combine = () =>
    {
        GeomCombination made;
        try
        {
            var array = new CombinableElementArray();
            foreach (var m in members) array.Append((CombinableElement)m);
            made = doc.CombineElements(array);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused to combine "
                + string.Join(", ", members.Select(describe)) + ": " + ex.Message
                + " NOTHING from this call was kept.");
        }
        doc.Regenerate();
        if (made == null)
            throw new InvalidOperationException("Revit combined nothing. NOTHING from this call was kept.");
        return made;
    };
    Func<List<Solid>, List<Element>> leftOutOf = result => watched
        .Where(kv => kv.Value.Any(p => !result.Any(r => holds(r, p)))).Select(kv => kv.Key).ToList();
    Func<List<Element>, string> leftOutWords = left => "Revit could not take " + left.Count + " of the "
        + solids.Count + " solid(s) named into the combination: " + string.Join(", ", left.Take(5).Select(describe))
        + (left.Count > 5 ? " and " + (left.Count - 5) + " more" : "") + ". Their material went missing from what it "
        + "made - it would vanish from the family - while Revit still listed them in it; its own words are \"Can't "
        + "keep elements joined\". Its join or cut fails like this where forms meet on faces that only partly line "
        + "up: a void whose sides lie exactly on a solid's faces, which cuts once its sides are moved a millimetre "
        + "past them, or the stepped segments of a coil, where overlapping them by a degree does not help.";

    // A TRIAL FIRST, ALWAYS TAKEN BACK (5b-370): Revit reports a form it left
    // out only at commit, so the combination is made once in a sub-transaction,
    // read form by form and rolled back. A solid left out refuses the call with
    // nothing kept; only then is the real one made - and read again.
    List<Element> leftOut;
    using (var trial = new SubTransaction(doc))
    {
        trial.Start();
        leftOut = leftOutOf(bodiesOf(combine()).Where(b => b.Volume > 0).ToList());
        trial.RollBack();
    }

    if (leftOut.Count > 0)
    {
        cut = false;
        refused = "Nothing was joined or cut. " + leftOutWords(leftOut);
    }
    else
    {
        var combination = combine();
        var result = bodiesOf(combination).Where(b => b.Volume > 0).ToList();
        var after = result.Sum(b => b.Volume);

        var left = leftOutOf(result);
        if (left.Count > 0)
            throw new InvalidOperationException(leftOutWords(left) + " NOTHING from this call was kept.");

        if (after <= 0)
            throw new InvalidOperationException("Nothing is left of the solids once they are cut - the voids named "
                + "take all of them away. NOTHING from this call was kept.");

        if (cut && after > before - crumb)
            throw new InvalidOperationException("The solids measured " + litres(before) + " L before and the cut "
                + "combination " + litres(after) + " L - the void took nothing away. It does not pass through them, "
                + "or Revit did not cut with it. NOTHING from this call was kept.");
        if (!cut && after > before + crumb)
            throw new InvalidOperationException("The solids measured " + litres(before) + " L apart and "
                + litres(after) + " L joined - a join cannot add material. NOTHING from this call was kept.");

        combinationId = combination.UniqueId;
        combined = (cut ? "Cut " + solids.Count + " solid(s) with " + voids.Count + " void(s)"
                        : "Joined " + solids.Count + " solid(s)")
            + ": " + litres(before) + " L before, " + litres(after) + " L after"
            + (cut ? " - " + litres(before - after) + " L taken away." : (before - after > crumb
                ? " - " + litres(before - after) + " L where they overlapped, now counted once."
                : " - they only touched."))
            + " Read back form by form: all " + solids.Count + " solid(s) are in the result - "
            + watched.Sum(kv => kv.Value.Count) + " point(s) inside them that no void covers, every one found in it.";

        findings.Add(combined + " The combination's id is " + combinationId + ".");
        findings.Add("Combined: " + string.Join(", ", members.Select(describe)) + ".");
        findings.Add("Undo, or Revit's Unjoin Geometry and Uncut Geometry, take it apart again; the forms themselves "
            + "are unchanged.");
    }
}

if (refused != null) findings.Add(refused);
