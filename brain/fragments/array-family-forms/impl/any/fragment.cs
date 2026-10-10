// NOT STANDALONE. Assumes `doc`, `uidoc`, `form`, `count` and `step` are in
// scope; leaves `arrayId`, `members`, `labelled`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A ROW OF THE SAME FORM - a grille's blades, a radiator's fins, the slots of a
// louvre, the shelves of a rack - as one linear array in the family open in
// the Family Editor: the Family Editor's Array, with its count LABELLED to an
// Integer family parameter when the types must choose how many.
//
// ONE FORM PER ARRAY. Arraying several elements at once makes each member a
// group, and a member that is a group is named by the group and not by the form
// in it; a second form takes a second call, and both may follow one count.
//
// OR ONE NESTED FAMILY (version 2, 2026-10-07) - a coil spring's turns, a
// rack's brackets: a family placed in this one, arrayed the same way. Measured
// on SpringMainProbe (Revit 2024, rolled back): a level-based nested family
// moved up, its Bottom locked to a plane, arrayed up to its LAST member, that
// copy's Top locked to a second plane - the row re-spaced when the planes
// moved and grew from 3 to 5 with its count. Every member, the original
// included, becomes a model group; `members` gives the copies' groups IN ORDER
// ALONG THE ROW, the last one last, which is the one LOCK_NESTED_FAMILY_TO_PLANES
// locks.
//
// THE COUNT is a whole number from 2 to 200, or the name of an INTEGER family
// parameter - then its value in the current type is the count, and the label
// makes every type's own value the count. Revit's own remark: an array is
// labelled only in a family, and only to an Integer parameter; a Yes/No is
// stored as a whole number too and is refused by its kind. The kind moved at
// 2022, ParameterType before and the spec from, so both are read by
// reflection.
//
// THE STEP is an axis and millimetres - "X 150" puts each member 150 mm further
// along +X than the one before; "X 900 last" puts the LAST member 900 mm along
// X from the first and spreads the rest evenly between, which is the one to
// tie to a plane so the spacing follows a length.
//
// A COPY CANNOT LEAVE A PLANE ITS FORM HANGS ON. Each member carries its own
// copy of the form's sketch plane, and one made from a reference plane or a
// level stays on it - so a row along the axis that plane faces leaves every
// copy on the original (5b-357: a vane sketched on Center (Left/Right), arrayed
// along X). A free plane moves with its copy, and which kind a form hangs on
// the API cannot say. So it is MEASURED before anything is kept: a trial row,
// rolled back in a sub-transaction, and a copy that did not move the spacing
// asked refuses the call in plain words, with the way round it.
//
// THE TRIAL ROW IS THREE, so a MIDDLE copy is measured (version 3, 5b-374). A
// row of two has one copy, and it is the anchor, which moves. A form with a
// face locked to a plane in the row's direction, or its top locked to a plane,
// leaves the middle copies on the original while the anchor moves - measured
// on a damper's blade in Family21, where the trial of two passed and the real
// row threw "one reads 0 mm". The trial uses the real row's anchor, and is two
// only for a fixed count of two, where there is no middle copy. A refusal lists
// the form's own locks and alignments - the planes and the faces they hold -
// and the way round, measured there: sketch on a level that does not move,
// link Extrusion Start and End to parameters instead of locking the top, lock
// before the array only the faces across the row, and after it the first and
// last copy's faces along it.
//
// READ BACK, ALL OR NOTHING. The array's count, its label and every copy's
// place are read again - each copy one step further along than the last - and
// anything that does not read as asked fails the call; the host rolls the
// whole call back.

var findings = new List<string>();
var arrayId = "";
var members = "";
var labelled = false;
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var axisLetters = new[] { "X", "Y", "Z" };

Func<Element, string> shapeOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep"
    : f is FamilyInstance ? "nested family" : "form";
Func<Element, string> describe = f =>
{
    var nested = f as FamilyInstance;
    if (nested != null)
    {
        var type = "";
        try { type = nested.Symbol.FamilyName + " : " + nested.Symbol.Name; } catch (Exception) { type = nested.Name; }
        return "nested family \"" + type + "\" " + f.UniqueId;
    }
    var solid = true;
    try { solid = ((GenericForm)f).IsSolid; } catch (Exception) { solid = true; }
    return (solid ? "solid " : "void ") + shapeOf(f) + " " + f.UniqueId;
};

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

// Is this family parameter an INTEGER one? From 2022 its data type is compared
// with the integer spec; before that its parameter type is read - that enum's
// Integer member is gone from 2023.
Func<FamilyParameter, bool> isInteger = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId+Int");
            var property = owner == null ? null : owner.GetProperty("Integer", flags);
            var wanted = property == null ? null : property.GetValue(null);
            return sameKind(spec, wanted);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Integer";
    }
    catch (Exception)
    {
        return false;
    }
};

// The lowest point of a form's box along one axis, in feet - where a copy sits.
Func<Element, int, double?> lowest = (e, index) =>
{
    var box = e.get_BoundingBox(null);
    if (box == null) return null;
    return index == 0 ? box.Min.X : index == 1 ? box.Min.Y : box.Min.Z;
};

Func<int, XYZ> unitAlong = index => new XYZ(index == 0 ? 1 : 0, index == 1 ? 1 : 0, index == 2 ? 1 : 0);

// The view an array is made in: a 3D view of the family when it has one - the
// array's view must see the form, and only a view-specific element needs the
// step in that view's plane - or else the view open.
Func<View> arrayView = () => (View)new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>()
    .FirstOrDefault(v => !v.IsTemplate) ?? doc.ActiveView;

// The plane a form is sketched on, where it has one to name: an extrusion's or
// a revolve's sketch, a blend's base. A sweep's path may be picked rather than
// sketched, so it is not guessed at.
Func<GenericForm, SketchPlane> sketchPlaneOf = f =>
{
    var sketch = f is Extrusion ? ((Extrusion)f).Sketch : f is Revolution ? ((Revolution)f).Sketch
        : f is Blend ? ((Blend)f).BottomSketch : null;
    return sketch == null ? null : sketch.SketchPlane;
};

// Which face of an element a reference names, in words - "top face", "+Y face".
Func<Element, Reference, string> faceWord = (e, r) =>
{
    try
    {
        var nested = e as FamilyInstance;
        if (nested != null)
        {
            var name = nested.GetReferenceName(r);
            return string.IsNullOrEmpty(name) ? "a reference" : "\"" + name + "\"";
        }
        var face = e.GetGeometryObjectFromReference(r) as PlanarFace;
        if (face == null) return "a face";
        for (var i = 0; i < 3; i++)
        {
            var dot = face.FaceNormal.DotProduct(unitAlong(i));
            if (Math.Abs(Math.Abs(dot) - 1) < 1e-6)
                return i == 2 ? (dot > 0 ? "top face" : "bottom face") : (dot > 0 ? "+" : "-") + axisLetters[i] + " face";
        }
        return "a sloping face";
    }
    catch (Exception)
    {
        return "a face";
    }
};

// WHAT HOLDS AN ELEMENT: every locked alignment or dimension, and every
// labelled one, that names one of its faces - the face, how it is held, and to
// what. A plain dimension that is neither locked nor labelled holds nothing.
Func<Element, List<string>> heldBy = e =>
{
    var said = new List<string>();
    foreach (var dimension in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
    {
        ReferenceArray refs = null;
        try { refs = dimension.References; } catch (Exception) { refs = null; }
        if (refs == null) continue;
        var faces = new List<string>();
        var others = new List<string>();
        foreach (Reference r in refs)
        {
            if (r == null) continue;
            if (r.ElementId == e.Id) { faces.Add(faceWord(e, r)); continue; }
            var other = doc.GetElement(r.ElementId);
            if (other != null) others.Add("\"" + (string.IsNullOrEmpty(other.Name) ? other.UniqueId : other.Name) + "\"");
        }
        if (faces.Count == 0) continue;
        FamilyParameter label = null;
        try { label = dimension.FamilyLabel; } catch (Exception) { label = null; }
        var locked = false;
        try { locked = dimension.IsLocked; } catch (Exception) { locked = false; }
        if (label == null && !locked) continue;
        said.Add("its " + string.Join(" and ", faces.Distinct()) + (label != null ? " dimensioned, labelled \""
                + label.Definition.Name + "\", to " : " locked to ")
            + (others.Count == 0 ? "another of its own faces" : string.Join(" and ", others.Distinct())));
    }
    return said;
};

// The form, or the nested family, the row is made of.
Element original = null;
GenericForm originalForm = null;
FamilyParameter driver = null;
var n = 0;
var axis = -1;
var stepMm = 0.0;
var toLast = false;
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's array is made "
        + "inside the family - open it first.";
}
else
{
    // ---- the form ---------------------------------------------------------
    var named = new List<Element>();
    var which = (form ?? "").Trim();
    if (string.Equals(which, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(which, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the form by its id.");
        else
            foreach (var id in uidoc.Selection.GetElementIds())
            {
                var element = doc.GetElement(id);
                if (element != null) named.Add(element);
            }
    }
    else if (which.Length > 0)
    {
        var element = doc.GetElement(which);
        if (element == null) problems.Add("No element in this family has the id \"" + which + "\".");
        else named.Add(element);
    }
    if (problems.Count == 0)
    {
        if (named.Count != 1)
            problems.Add((named.Count == 0 ? "No form was named" : named.Count + " elements were named") + " - an array "
                + "here is of ONE form, by the id the form tools gave back or selected alone. A second form takes a "
                + "second call, and both may follow one count.");
        else
        {
            originalForm = named[0] as GenericForm;
            original = originalForm != null ? named[0] : named[0] as FamilyInstance;
            if (original == null)
                problems.Add("\"" + (named[0].Name ?? named[0].UniqueId) + "\" (" + named[0].UniqueId + ") is neither "
                    + "a form - an extrusion, revolve, blend, sweep or swept blend - nor a nested family.");
        }
    }

    // ---- the count --------------------------------------------------------
    var saidCount = (count ?? "").Trim();
    int whole;
    if (saidCount.Length == 0)
        problems.Add("No count was given - a whole number from 2 to 200, or an Integer family parameter's name.");
    else if (int.TryParse(saidCount, System.Globalization.NumberStyles.Integer, invariant, out whole))
    {
        if (whole < 2 || whole > 200)
            problems.Add("A count of " + whole + " is not an array - give 2 to 200, the most Revit makes.");
        else n = whole;
    }
    else
    {
        var fm = doc.FamilyManager;
        FamilyParameter exact = null;
        try { exact = fm.get_Parameter(saidCount); } catch (Exception) { exact = null; }
        if (exact == null)
            foreach (FamilyParameter p in fm.Parameters)
                if (string.Equals(p.Definition.Name, saidCount, StringComparison.OrdinalIgnoreCase)) { exact = p; break; }
        if (exact == null)
        {
            var integers = new List<string>();
            foreach (FamilyParameter p in fm.Parameters) if (isInteger(p)) integers.Add(p.Definition.Name);
            problems.Add("\"" + saidCount + "\" is neither a whole number nor a family parameter. This family's "
                + "Integer parameters: " + (integers.Count == 0 ? "none - ADD_FAMILY_PARAMETERS makes one"
                    : string.Join(", ", integers.OrderBy(x => x))) + ".");
        }
        else if (!isInteger(exact))
            problems.Add("\"" + exact.Definition.Name + "\" is not an INTEGER parameter - Revit labels an array's "
                + "count only with one, and a Yes/No is not one even though Revit stores it as a whole number.");
        else if (exact.IsReporting)
            problems.Add("\"" + exact.Definition.Name + "\" is a reporting parameter, and cannot drive a count.");
        else
        {
            int? value = null;
            try { value = fm.CurrentType == null ? null : fm.CurrentType.AsInteger(exact); } catch (Exception) { value = null; }
            if (!value.HasValue)
                problems.Add("\"" + exact.Definition.Name + "\" has no value in the current type - give it one with "
                    + "SET_FAMILY_TYPE_VALUES first; it is the count the array is made with.");
            else if (value.Value < 2 || value.Value > 200)
                problems.Add("\"" + exact.Definition.Name + "\" holds " + value.Value + " in the current type - the "
                    + "array is made with that count, so give it 2 to 200 first.");
            else
            {
                driver = exact;
                n = value.Value;
            }
        }
    }

    // ---- the step ---------------------------------------------------------
    var words = (step ?? "").Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    double distance;
    if (words.Length < 2 || words.Length > 3)
        problems.Add("The step \"" + (step ?? "") + "\" is not an axis and millimetres - \"X 150\" for each member "
            + "150 mm further along X, or \"X 900 last\" for the last one 900 mm along.");
    else
    {
        axis = Array.IndexOf(axisLetters, words[0].ToUpperInvariant());
        if (axis < 0) problems.Add("\"" + words[0] + "\" is not an axis - X, Y or Z.");
        if (!double.TryParse(words[1], System.Globalization.NumberStyles.Float, invariant, out distance)
            || double.IsNaN(distance) || double.IsInfinity(distance))
            problems.Add("\"" + words[1] + "\" is not a distance in millimetres.");
        else if (Math.Abs(distance) < doc.Application.ShortCurveTolerance * 304.8)
            problems.Add("A step of " + plain(distance) + " mm puts the members on top of each other.");
        else stepMm = distance;
        if (words.Length == 3)
        {
            if (string.Equals(words[2], "last", StringComparison.OrdinalIgnoreCase)) toLast = true;
            else problems.Add("\"" + words[2] + "\" after the distance is not \"last\".");
        }
    }

    // ---- will every copy leave the original? -----------------------------
    // MEASURED, not predicted - see the header, 5b-357 and 5b-374. A trial row
    // of three, with the real row's anchor, made and read inside a
    // sub-transaction that is always rolled back. Where Revit cannot make even
    // the trial, the array below fails in its own words; here only copies that
    // did not land a spacing apart are refused.
    if (problems.Count == 0)
    {
        var spacing = toLast ? stepMm / (n - 1) : stepMm;
        var trialCount = n >= 3 || driver != null ? 3 : 2;
        var before = lowest(original, axis);
        var trialView = arrayView();
        // Each trial copy's distance from the original along the axis, in mm,
        // nearest first. Null when the trial could not be made or read.
        List<double> moved = null;
        var trial = new SubTransaction(doc);
        try
        {
            trial.Start();
            if (trialView != null && before.HasValue)
            {
                var row = toLast
                    ? LinearArray.Create(doc, trialView, original.Id, trialCount,
                        unitAlong(axis) * ((trialCount - 1) * spacing / 304.8), ArrayAnchorMember.Last)
                    : LinearArray.Create(doc, trialView, original.Id, trialCount,
                        unitAlong(axis) * (spacing / 304.8), ArrayAnchorMember.Second);
                doc.Regenerate();
                var at = row == null ? new List<double?>()
                    : row.GetCopiedMemberIds().Select(id => doc.GetElement(id)).Where(e => e != null)
                        .Select(e => lowest(e, axis)).ToList();
                if (at.Count == trialCount - 1 && at.All(a => a.HasValue))
                    moved = at.Select(a => (a.Value - before.Value) * 304.8)
                        .OrderBy(d => d * Math.Sign(spacing)).ToList();
            }
        }
        catch (Exception) { moved = null; }
        finally
        {
            if (trial.HasStarted() && !trial.HasEnded()) trial.RollBack();
        }

        if (moved != null && moved.Where((d, i) => Math.Abs(d - (i + 1) * spacing) > 0.5).Any())
        {
            var shown = moved.Select(d => Math.Abs(d) < 0.005 ? 0.0 : d).ToList();
            var asked = Enumerable.Range(1, shown.Count).Select(i => i * spacing).ToList();
            var measured = "a trial row of " + trialCount + ", rolled back, moved its " + (shown.Count == 1 ? "copy " : "copies ")
                + string.Join(" and ", shown.Select(plain)) + " mm of the " + string.Join(" and ", asked.Select(plain))
                + " mm asked, so " + (shown.All(d => Math.Abs(d) <= 0.5) ? "every copy would sit on the original."
                    : shown.Any(d => Math.Abs(d) <= 0.5) ? "the middle copies would sit on the original."
                    : "the copies would not sit " + plain(spacing) + " mm apart.");
            var holds = heldBy(original);
            var plane = originalForm == null ? null : sketchPlaneOf(originalForm);
            var planeName = plane == null ? "" : (plane.Name ?? "").Trim();
            // A nested family hangs on the plane or level it was placed on.
            var nestedHost = originalForm == null ? ((FamilyInstance)original).Host : null;
            if (nestedHost != null && planeName.Length == 0) planeName = (nestedHost.Name ?? "").Trim();
            var staysIn = new List<string>();
            if (plane != null)
            {
                var normal = plane.GetPlane().Normal;
                for (var i = 0; i < 3; i++)
                    if (i != axis && Math.Abs(normal.DotProduct(unitAlong(i))) < 1e-6) staysIn.Add(axisLetters[i]);
            }
            var runsAlong = axis == 0 ? "Ref. Level or Center (Front/Back)"
                : axis == 1 ? "Ref. Level or Center (Left/Right)" : "Center (Front/Back) or Center (Left/Right)";
            // Does the row leave the plane the form hangs on (5b-357)? A form's
            // sketch plane is read; a nested family's host is not measured, so
            // its plane is named as before.
            var leavesPlane = originalForm == null
                || (plane != null && Math.Abs(Math.Abs(plane.GetPlane().Normal.DotProduct(unitAlong(axis))) - 1) < 1e-6);
            var across = axis == 0 ? "Y" : axis == 1 ? "X" : "X and Y";
            if (leavesPlane)
                problems.Add("A copy of this " + shapeOf(original) + " cannot leave the plane it "
                    + (originalForm == null ? "is placed on" : "is sketched on")
                    + (planeName.Length > 0 ? ", " + planeName + "," : "") + " and a row along " + axisLetters[axis]
                    + " leaves it: " + measured
                    + (staysIn.Count > 0 ? " A row along " + string.Join(" or ", staysIn) + " stays in that plane." : "")
                    + (holds.Count > 0 ? " It is also held - " + string.Join("; ", holds) + "." : "")
                    + (originalForm == null
                        ? " For a row along " + axisLetters[axis] + ", place the nested family on a plane the row runs "
                            + "along - " + runsAlong + " in Revit's own templates - or nest a family that is not Work "
                            + "Plane-Based: a level-based one, measured, made a row upward."
                        : " For a row along " + axisLetters[axis] + ", sketch the form on a plane the row runs along - "
                            + runsAlong + " in Revit's own templates - or, with a fixed count, make each one its own form "
                            + "at its own place."));
            else
                problems.Add("The copies of this " + shapeOf(original) + " do not follow a row along " + axisLetters[axis]
                    + ": " + measured
                    + (holds.Count > 0
                        ? " It is held - " + string.Join("; ", holds) + ". Measured on a damper's blade (5b-374): a form "
                            + "with a face locked to a plane along the row, or its top locked to a plane, keeps the middle "
                            + "copies on the original while the last one moves."
                        : " No lock or labelled dimension holds its faces, and a row along " + axisLetters[axis] + " stays "
                            + "in the plane it is sketched on" + (planeName.Length > 0 ? ", " + planeName : "")
                            + ", so what held the copies was not found.")
                    + " The way round, measured on that damper: sketch the form on a level that does not move - Ref. "
                    + "Level - and link its Extrusion Start and End to parameters (LINK_FAMILY_FORM_PARAMETER) instead "
                    + "of locking its top; before the array, lock only its faces looking along " + across + "; after it, "
                    + "lock the first form's and the last copy's faces along " + axisLetters[axis] + " (LOCK_FORM_TO_PLANES "
                    + "- the last copy is the last id in `members`).");
        }
    }

    if (problems.Count > 0) refused = "Nothing was arrayed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// ARRAY, LABEL, THEN READ EVERY COPY BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    View view = arrayView();
    if (view == null)
        throw new InvalidOperationException("This family has no 3D view and no view open, and an array is made in a "
            + "view. The call failed, and Heron rolls the whole call back.");

    var direction = unitAlong(axis);
    var translation = direction * (stepMm / 304.8);
    var spacingMm = toLast ? stepMm / (n - 1) : stepMm;
    var startAt = lowest(original, axis);

    LinearArray array;
    try
    {
        array = LinearArray.Create(doc, view, original.Id, n, translation,
            toLast ? ArrayAnchorMember.Last : ArrayAnchorMember.Second);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to array the " + describe(original) + " " + n + " times, "
            + plain(stepMm) + " mm along " + axisLetters[axis] + (toLast ? " to the last" : "") + ": " + ex.Message
            + " The call failed, and Heron rolls the whole call back.");
    }
    if (array == null)
        throw new InvalidOperationException("Revit made no array. The call failed, and Heron rolls the whole call back.");

    if (driver != null)
    {
        try { array.Label = driver; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not label the array's count with \"" + driver.Definition.Name
                + "\": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }

    doc.Regenerate();

    // READ BACK: the count, the label, every copy one spacing further along.
    if (array.NumMembers != n)
        throw new InvalidOperationException("The array reads " + array.NumMembers + " members, not " + n + ". The call "
            + "failed, and Heron rolls the whole call back.");
    if (driver != null && (array.Label == null || array.Label.Id != driver.Id))
        throw new InvalidOperationException("The array's count is not labelled \"" + driver.Definition.Name + "\" after "
            + "the call. The call failed, and Heron rolls the whole call back.");

    var copies = array.GetCopiedMemberIds().Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
    if (copies.Count != n - 1)
        throw new InvalidOperationException("The array holds " + copies.Count + " copies of the form, not " + (n - 1)
            + ". The call failed, and Heron rolls the whole call back.");
    if (startAt.HasValue)
    {
        var offsets = copies.Select(c => lowest(c, axis)).ToList();
        if (offsets.Any(o => !o.HasValue))
            throw new InvalidOperationException("A copy in the array has no extent to read. The call failed, and Heron "
                + "rolls the whole call back.");
        var found = offsets.Select(o => (o.Value - startAt.Value) * 304.8 / spacingMm).OrderBy(k => k).ToList();
        for (var i = 0; i < found.Count; i++)
            if (Math.Abs(found[i] - (i + 1)) * Math.Abs(spacingMm) > 0.5)
                throw new InvalidOperationException("The copies of the form do not sit " + plain(spacingMm) + " mm apart "
                    + "along " + axisLetters[axis] + " - one reads " + plain(found[i] * spacingMm) + " mm from the "
                    + "original where " + plain((i + 1) * spacingMm) + " mm was asked. The call failed, and Heron "
                    + "rolls the whole call back.");
    }

    arrayId = array.UniqueId;
    // IN ORDER ALONG THE ROW, nearest the original first - Revit hands the
    // copies back in no order, and the last one is the one a plane holds.
    members = string.Join(",", copies
        .OrderBy(c => ((lowest(c, axis) ?? 0) - (startAt ?? 0)) * Math.Sign(spacingMm))
        .Select(c => c.UniqueId));
    labelled = driver != null;

    findings.Add("Arrayed the " + describe(original) + ": " + n + " in all, " + plain(spacingMm) + " mm apart along "
        + (spacingMm > 0 ? "+" : "-") + axisLetters[axis] + (toLast ? ", the last " + plain(stepMm) + " mm from the first"
            : "") + (labelled ? ", the count following \"" + driver.Definition.Name + "\" in each type" : "")
        + " - every copy read back. The array's id is " + arrayId + ".");
    if (toLast && originalForm != null)
        findings.Add("The last member anchors the spacing. LOCK_FORM_TO_PLANES can lock it to a plane, and FLEX_FAMILY "
            + "shows whether the members then re-space when that plane moves (NEEDS-CHECKING BQ5).");
    if (toLast && originalForm == null)
        findings.Add("The last member anchors the spacing. LOCK_NESTED_FAMILY_TO_PLANES locks the last copy - the last "
            + "id in `members` - to a plane; measured, the row then re-spaced when that plane moved. FLEX_FAMILY "
            + "shows it in this family.");
    if (originalForm != null && !originalForm.IsSolid)
        findings.Add("The copies are voids, and cut nothing until COMBINE_FAMILY_FORMS combines each with its solid - "
            + "their ids are in `members`.");
    if (labelled)
        findings.Add("When a type's count changes, Revit remakes the copies, and their ids change with it.");
}

if (refused != null) findings.Add(refused);
