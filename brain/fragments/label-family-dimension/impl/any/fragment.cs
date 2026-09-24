// NOT STANDALONE. Assumes `doc`, `fromPlane`, `toPlane`, `parameterName` and
// `centrePlane` are in scope; leaves `labelled`, `equalised`, `distanceMm`,
// `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// LABELLING MOVES THE PLANES TO THE PARAMETER'S VALUE. A new length reads zero,
// so a label put on first collapses the planes onto each other - reported from
// an earlier family build. The distance is therefore measured and compared with
// the parameter BEFORE anything is made, and a difference refuses the call.
//
// ONE LABEL PER PARAMETER. A second dimension driving the same parameter fights
// the first as soon as anything moves, so an existing label refuses the call.
//
// THE EQ IS MADE FIRST, THEN THE LABEL - both reported working in that order in
// an earlier family build - and it needs the centre plane already midway,
// because an EQ moves planes until the sides match.
//
// ANY REFUSAL FROM REVIT AFTER THE CHECKS THROWS, so the host rolls the whole
// call back: an EQ left without its label, or a label on the wrong planes, is
// worse than neither.

var findings = new List<string>();
var labelled = "";
var equalised = false;
var distanceMm = 0.0;
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
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisWords = new[] { "left-right", "front-back", "height" };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// EVERY PLANE THIS CAN DIMENSION TO: (element, reference, axis, position in
// feet, name). Reference planes by their own name; levels count as height
// planes, because a height is dimensioned from Ref. Level.
var datums = new List<Tuple<Element, Reference, int, double, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var at = axisOf(rp.Normal);
        if (at < 0) continue;
        datums.Add(Tuple.Create((Element)rp, rp.GetReference(), at, along(rp.GetPlane().Origin, at), ownName(rp)));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        // The level's own PLANE reference - what a dimension or an alignment to
        // a level is made against - rather than a reference to the element.
        datums.Add(Tuple.Create((Element)level, level.GetPlaneReference(), 2, level.Elevation, level.Name));
}

Func<string, List<Tuple<Element, Reference, int, double, string>>> named = wanted =>
    datums.Where(d => d.Item5.Length > 0
                   && string.Equals(d.Item5, (wanted ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
          .ToList();

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY. A ForgeTypeId
// names its version - "...:length-2.0.0" - and plain equality may compare it,
// so a length a template made under an older version would read as some other
// kind. NameEquals compares the name alone, from 2021; before that a kind is
// an enum and plain equality is exact.
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

// Is this parameter a length? Read the way its own release allows.
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

Tuple<Element, Reference, int, double, string> a = null, b = null, centre = null;
FamilyParameter parameter = null;
View view = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family "
        + "dimension is labelled inside the family - open it first.";
}
else
{
    var fm = doc.FamilyManager;
    var problems = new List<string>();

    Func<string, string, Tuple<Element, Reference, int, double, string>> one = (wanted, role) =>
    {
        var found = named(wanted);
        if ((wanted ?? "").Trim().Length == 0) { problems.Add("No " + role + " was named."); return null; }
        if (found.Count == 0)
        {
            var offered = datums.Where(d => d.Item5.Length > 0).Select(d => d.Item5).Distinct()
                .OrderBy(n => n).Take(15).ToList();
            problems.Add("No reference plane or level is called \"" + wanted.Trim() + "\". This family has "
                + (offered.Count == 0 ? "no named ones." : string.Join(", ", offered) + "."));
            return null;
        }
        if (found.Count > 1) { problems.Add("\"" + wanted.Trim() + "\" names " + found.Count + " planes - rename one first."); return null; }
        return found[0];
    };

    a = one(fromPlane, "first plane");
    b = one(toPlane, "second plane");
    var wantsCentre = !string.IsNullOrEmpty(centrePlane) && centrePlane.Trim().Length > 0;
    if (wantsCentre) centre = one(centrePlane, "centre plane");

    var wantedParameter = parameterName == null ? "" : parameterName.Trim();
    parameter = wantedParameter.Length == 0 ? null : fm.get_Parameter(wantedParameter);

    if (wantedParameter.Length == 0) problems.Add("No parameter was named to label the dimension with.");
    else if (parameter == null) problems.Add("\"" + wantedParameter + "\" is not a parameter of this family - ADD_FAMILY_PARAMETERS makes one.");
    else if (!isLength(parameter)) problems.Add("\"" + wantedParameter + "\" is not a length, and only a length can label a straight dimension.");
    else if (parameter.IsReporting) problems.Add("\"" + wantedParameter + "\" is a reporting parameter - it would read the distance, not drive it.");

    if (problems.Count == 0)
    {
        if (a.Item1.Id == b.Item1.Id) problems.Add("Both ends are \"" + a.Item5 + "\" - a dimension needs two planes.");
        else if (a.Item3 != b.Item3) problems.Add("\"" + a.Item5 + "\" controls " + axisWords[a.Item3] + " and \"" + b.Item5 + "\" controls " + axisWords[b.Item3] + " - they are not parallel.");
        else if (Math.Abs(a.Item4 - b.Item4) < halfMillimetre) problems.Add("\"" + a.Item5 + "\" and \"" + b.Item5 + "\" are in the same place - there is no distance to label.");
    }

    if (problems.Count == 0 && fm.CurrentType == null)
        problems.Add("This family has no type yet, so \"" + parameter.Definition.Name + "\" holds no value "
            + "and a label would drive the planes to nothing. SET_FAMILY_TYPE_VALUES makes the first type.");

    if (problems.Count == 0)
    {
        // THE GUARD THIS FRAGMENT EXISTS AROUND: the parameter must already say
        // what the planes say.
        var apart = Math.Abs(a.Item4 - b.Item4);
        var holds = fm.CurrentType.AsDouble(parameter);
        if (!holds.HasValue || Math.Abs(holds.Value - apart) > halfMillimetre)
            problems.Add("\"" + a.Item5 + "\" and \"" + b.Item5 + "\" are " + mm(apart) + " mm apart and \""
                + parameter.Definition.Name + "\" holds " + (holds.HasValue ? mm(holds.Value) + " mm" : "no value")
                + " in type \"" + fm.CurrentType.Name + "\". Labelling now would MOVE the planes to that "
                + "value. Set it to " + mm(apart) + " first with SET_FAMILY_TYPE_VALUES, or name the planes "
                + "that are that far apart.");

        foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
        {
            FamilyParameter label = null;
            try { label = d.FamilyLabel; } catch (Exception) { }
            if (label == null) continue;

            if (label.Id == parameter.Id)
            {
                problems.Add("\"" + parameter.Definition.Name + "\" already labels a dimension in this family. "
                    + "A second label on it would fight the first as soon as anything moves.");
                break;
            }

            // ONE LABEL PER PAIR OF PLANES TOO. Two parameters each driving the
            // same two planes agree only while their values do; the first flex
            // that changes one of them is a constraint Revit cannot satisfy.
            var measured = new List<ElementId>();
            try { foreach (Reference r in d.References) measured.Add(r.ElementId); } catch (Exception) { }
            if (measured.Contains(a.Item1.Id) && measured.Contains(b.Item1.Id))
            {
                problems.Add("A dimension labelled \"" + label.Definition.Name + "\" already measures \"" + a.Item5
                    + "\" to \"" + b.Item5 + "\". A second label on the same two planes would fight it as soon as "
                    + "either parameter changes.");
                break;
            }
        }
    }

    if (problems.Count == 0 && centre != null)
    {
        var lo = Math.Min(a.Item4, b.Item4);
        var hi = Math.Max(a.Item4, b.Item4);
        if (centre.Item3 != a.Item3)
            problems.Add("\"" + centre.Item5 + "\" controls " + axisWords[centre.Item3] + ", not " + axisWords[a.Item3] + " - it cannot centre these planes.");
        else if (Math.Abs(centre.Item4 - (lo + hi) / 2) > halfMillimetre)
            problems.Add("\"" + centre.Item5 + "\" is not midway between \"" + a.Item5 + "\" and \"" + b.Item5
                + "\" (it is at " + mm(centre.Item4) + " mm, midway is " + mm((lo + hi) / 2) + " mm). An EQ "
                + "would move the planes until it is, so nothing was made.");
    }

    if (problems.Count == 0)
    {
        var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
            .Where(v => !v.IsTemplate).ToList();
        view = a.Item3 == 2
            ? views.Where(v => v.ViewType == ViewType.Elevation)
                   .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1).FirstOrDefault()
            : views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
        if (view == null)
            problems.Add(a.Item3 == 2
                ? "This family has no elevation view, and a height is dimensioned in one."
                : "This family has no floor plan, and a left-right or front-back size is dimensioned in one.");
    }

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems);
}

if (refused == null)
{
    var fm = doc.FamilyManager;
    var axis = a.Item3;
    var lo = Math.Min(a.Item4, b.Item4) - 0.5;
    var hi = Math.Max(a.Item4, b.Item4) + 0.5;
    var level = view.GenLevel;
    var z0 = level == null ? 0.0 : level.Elevation;

    // PLACED OUTSIDE: past the furthest plane of the axis the line is offset
    // along, so the dimension does not sit on top of the geometry.
    var right = view.RightDirection;
    var across = axis == 0 ? 1 : axis == 1 ? 0 : (Math.Abs(right.X) > 0.9 ? 0 : 1);
    var furthest = datums.Where(d => d.Item3 == across).Select(d => Math.Abs(d.Item4)).DefaultIfEmpty(0).Max();

    Func<double, Line> lineAt = margin =>
    {
        var offset = -(furthest + margin / 304.8);
        if (axis == 0) return Line.CreateBound(new XYZ(lo, offset, z0), new XYZ(hi, offset, z0));
        if (axis == 1) return Line.CreateBound(new XYZ(offset, lo, z0), new XYZ(offset, hi, z0));
        return Line.CreateBound(new XYZ(right.X * offset, right.Y * offset, lo),
                                new XYZ(right.X * offset, right.Y * offset, hi));
    };

    var first = a.Item4 <= b.Item4 ? a : b;
    var last = a.Item4 <= b.Item4 ? b : a;
    Dimension dimension;
    Dimension eq = null;

    try
    {
        if (centre != null)
        {
            var three = new ReferenceArray();
            three.Append(first.Item2);
            three.Append(centre.Item2);
            three.Append(last.Item2);
            eq = doc.FamilyCreate.NewDimension(view, lineAt(300), three);
            eq.AreSegmentsEqual = true;
        }

        var two = new ReferenceArray();
        two.Append(first.Item2);
        two.Append(last.Item2);
        dimension = doc.FamilyCreate.NewDimension(view, lineAt(centre != null ? 600 : 300), two);
        dimension.FamilyLabel = parameter;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the dimension between \"" + a.Item5 + "\" and \""
            + b.Item5 + "\": " + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK, AFTER THE REGENERATION - the label, the EQ, the distance the
    // dimension reads, and where the planes are now. The guard above made the
    // parameter agree with the planes, so NOTHING should have moved; a plane
    // that did, or a reading that differs, is the collapse this exists to stop,
    // and it is thrown rather than reported.
    var readLabel = dimension.FamilyLabel;
    var reads = dimension.Value;
    distanceMm = reads.HasValue ? Math.Round(reads.Value * 304.8, 2) : 0.0;
    equalised = eq != null && eq.AreSegmentsEqual;

    Func<Element, int, double> positionNow = (element, index) =>
    {
        var plane = element as ReferencePlane;
        if (plane != null) return along(plane.GetPlane().Origin, index);
        var level = element as Level;
        return level != null ? level.Elevation : double.NaN;
    };

    if (readLabel == null || readLabel.Id != parameter.Id)
        throw new InvalidOperationException("The dimension between \"" + a.Item5 + "\" and \"" + b.Item5
            + "\" was made and does not carry \"" + parameter.Definition.Name + "\" afterwards. NOTHING from "
            + "this call was kept.");

    if (centre != null && !equalised)
        throw new InvalidOperationException("The EQ across \"" + first.Item5 + "\", \"" + centre.Item5
            + "\" and \"" + last.Item5 + "\" did not hold. NOTHING from this call was kept.");

    var apartBefore = Math.Abs(a.Item4 - b.Item4);
    if (!reads.HasValue || Math.Abs(reads.Value - apartBefore) > halfMillimetre)
        throw new InvalidOperationException("The labelled dimension reads "
            + (reads.HasValue ? mm(reads.Value) + " mm" : "nothing") + " where the planes were " + mm(apartBefore)
            + " mm apart. NOTHING from this call was kept.");

    foreach (var plane in centre != null ? new[] { a, b, centre } : new[] { a, b })
    {
        var now = positionNow(plane.Item1, axis);
        if (double.IsNaN(now) || Math.Abs(now - plane.Item4) > halfMillimetre)
            throw new InvalidOperationException("\"" + plane.Item5 + "\" moved from " + mm(plane.Item4) + " to "
                + mm(now) + " mm when the dimension was labelled. NOTHING from this call was kept.");
    }

    labelled = parameter.Definition.Name + " drives \"" + first.Item5 + "\" to \"" + last.Item5 + "\", "
        + "reading " + distanceMm.ToString(invariant) + " mm";

    findings.Add(labelled + ", in \"" + view.Name + "\"."
        + (centre != null
            ? " An EQ across \"" + first.Item5 + "\", \"" + centre.Item5 + "\" and \"" + last.Item5
              + "\" keeps them centred as it changes."
            : " With no centre plane, \"" + first.Item5 + "\" and \"" + last.Item5 + "\" can move "
              + "unequally - right for a height from Ref. Level, wrong for a box meant to stay centred."));

    findings.Add("The planes now follow \"" + parameter.Definition.Name + "\". Geometry follows only if it "
        + "is LOCKED to them - EXTRUDE_BETWEEN_PLANES builds a box that is. FLEX_FAMILY proves it by "
        + "changing the value and reading the geometry back.");
}

if (refused != null) findings.Add(refused);
