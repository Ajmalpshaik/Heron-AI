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

Func<GenericForm, string> shapeOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep" : "form";
Func<GenericForm, string> describe = f =>
{
    var solid = true;
    try { solid = f.IsSolid; } catch (Exception) { solid = true; }
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

GenericForm original = null;
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
            original = named[0] as GenericForm;
            if (original == null)
                problems.Add("\"" + (named[0].Name ?? named[0].UniqueId) + "\" (" + named[0].UniqueId + ") is not a "
                    + "form - an extrusion, revolve, blend, sweep or swept blend.");
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

    if (problems.Count > 0) refused = "Nothing was arrayed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// ARRAY, LABEL, THEN READ EVERY COPY BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    // A 3D view of the family when it has one: the array's view must see the
    // form, and only a view-specific element needs the step in that view's plane.
    View view = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>()
        .FirstOrDefault(v => !v.IsTemplate);
    if (view == null) view = doc.ActiveView;
    if (view == null)
        throw new InvalidOperationException("This family has no 3D view and no view open, and an array is made in a "
            + "view. The call failed, and Heron rolls the whole call back.");

    var direction = new XYZ(axis == 0 ? 1 : 0, axis == 1 ? 1 : 0, axis == 2 ? 1 : 0);
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
    members = string.Join(",", copies.Select(c => c.UniqueId));
    labelled = driver != null;

    findings.Add("Arrayed the " + describe(original) + ": " + n + " in all, " + plain(spacingMm) + " mm apart along "
        + (spacingMm > 0 ? "+" : "-") + axisLetters[axis] + (toLast ? ", the last " + plain(stepMm) + " mm from the first"
            : "") + (labelled ? ", the count following \"" + driver.Definition.Name + "\" in each type" : "")
        + " - every copy read back. The array's id is " + arrayId + ".");
    if (toLast)
        findings.Add("The last member anchors the spacing. LOCK_FORM_TO_PLANES can lock it to a plane, and FLEX_FAMILY "
            + "shows whether the members then re-space when that plane moves (NEEDS-CHECKING BQ5).");
    if (!original.IsSolid)
        findings.Add("The copies are voids, and cut nothing until COMBINE_FAMILY_FORMS combines each with its solid - "
            + "their ids are in `members`.");
    if (labelled)
        findings.Add("When a type's count changes, Revit remakes the copies, and their ids change with it.");
}

if (refused != null) findings.Add(refused);
