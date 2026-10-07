// NOT STANDALONE. Assumes `doc`, `uidoc`, `instance` and `locks` are in scope;
// leaves `locked`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ALIGN AND LOCK A NESTED FAMILY to the planes of the family it sits in - the
// padlock after Align - so the motor in the air handler follows the planes a
// labelled dimension moves, and does not stay where it was dropped.
//
// IT LOCKS THE NESTED FAMILY'S OWN REFERENCES, named by its own planes' Is
// Reference - Left, Center (Left/Right), Right, Front, Center (Front/Back),
// Back, Bottom, Center (Elevation), Top - which are the only planes a placed
// family shows to the family around it: Revit's remarks, a plane that is "Not
// a Reference" makes none. A side the nested family does not have is refused
// with the sides it does; SET_FAMILY_PLANE_REFERENCE, run in the nested family,
// gives it one.
//
// A SIDE IS LOCKED ONLY TO A PLANE IT ALREADY LIES ON. Revit's own remarks on
// the alignment call: the two must "already be geometrically aligned". So a
// turned nested family - its Left facing front, say - is refused before
// anything is locked, and Revit's own refusal names the rest.
//
// AN ARRAY'S COPY IS NAMED BY ITS GROUP (version 2, 2026-10-07). Every member
// of an array is a model group, and ARRAY_FAMILY_FORMS gives back the groups'
// ids; a group holding exactly one nested family is locked through it. The
// last copy of a row spread to its last member, its Top locked to a plane,
// re-spaced the whole row when that plane moved - the parametric array of a
// coil spring's turns (SpringMainProbe, Revit 2024, rolled back).
//
// A SIDE FACING LEFT-RIGHT OR FRONT-BACK IS LOCKED IN A FLOOR PLAN, one facing
// up or down in an elevation, as it is done by hand.
//
// ALL OR NOTHING, READ BACK. After the locks Revit regenerates, and the nested
// family's extent must not have moved - a lock never moves what it locks.

var findings = new List<string>();
var locked = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Replace("centre", "center").Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisWords = new[] { "left-right", "front-back", "up-down" };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// The nine sides a nested family can show: the key, Revit's words, the API's
// reference type, and which of the nested family's own axes it faces along.
var sides = new List<Tuple<string, string, FamilyInstanceReferenceType, int>>
{
    Tuple.Create("left", "Left", FamilyInstanceReferenceType.Left, 0),
    Tuple.Create("centerleftright", "Center (Left/Right)", FamilyInstanceReferenceType.CenterLeftRight, 0),
    Tuple.Create("right", "Right", FamilyInstanceReferenceType.Right, 0),
    Tuple.Create("front", "Front", FamilyInstanceReferenceType.Front, 1),
    Tuple.Create("centerfrontback", "Center (Front/Back)", FamilyInstanceReferenceType.CenterFrontBack, 1),
    Tuple.Create("back", "Back", FamilyInstanceReferenceType.Back, 1),
    Tuple.Create("bottom", "Bottom", FamilyInstanceReferenceType.Bottom, 2),
    Tuple.Create("centerelevation", "Center (Elevation)", FamilyInstanceReferenceType.CenterElevation, 2),
    Tuple.Create("top", "Top", FamilyInstanceReferenceType.Top, 2),
};

// (element, reference, axis it faces, name) for every named reference plane
// and every level.
var datums = new List<Tuple<Element, Reference, int, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var name = ownName(rp);
        if (name.Length > 0) datums.Add(Tuple.Create((Element)rp, rp.GetReference(), axisOf(rp.Normal), name));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, level.GetPlaneReference(), 2, level.Name));
}

FamilyInstance nested = null;
// Each lock: the side, the nested family's reference for it, and the plane.
var plans = new List<Tuple<Tuple<string, string, FamilyInstanceReferenceType, int>, Reference, Tuple<Element, Reference, int, string>>>();
var problems = new List<string>();
View plan = null, elevation = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A nested family is "
        + "locked to its planes inside the family - open it first.";
}
else
{
    var said = (instance ?? "").Trim();
    Element chosen = null;
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the nested family by its id.");
        else
        {
            var ids = uidoc.Selection.GetElementIds().ToList();
            if (ids.Count != 1) problems.Add(ids.Count + " elements are selected. Select exactly one nested family.");
            else chosen = doc.GetElement(ids[0]);
        }
    }
    else if (said.Length == 0) problems.Add("No nested family was named.");
    else
    {
        chosen = doc.GetElement(said);
        if (chosen == null) problems.Add("No element in this family has the id \"" + said + "\".");
    }
    if (chosen != null)
    {
        nested = chosen as FamilyInstance;
        // AN ARRAY'S COPY IS A GROUP holding the nested family - every member
        // of an array is one (5b-358) - and ARRAY_FAMILY_FORMS gives back the
        // groups' ids. The nested family inside is the one locked: on
        // 2026-10-07 (SpringMainProbe, Revit 2024) the LAST copy's Top locked
        // to a plane, and the copies re-spaced when that plane moved.
        var group = chosen as Group;
        if (nested == null && group != null)
        {
            var inside = group.GetMemberIds().Select(id => doc.GetElement(id)).OfType<FamilyInstance>().ToList();
            if (inside.Count == 1)
            {
                nested = inside[0];
                findings.Add("\"" + (chosen.Name ?? said) + "\" is a group - an array's copy - and the nested family "
                    + "inside it, " + nested.UniqueId + ", is the one locked.");
            }
            else
                problems.Add("\"" + (chosen.Name ?? said) + "\" is a group holding " + inside.Count + " nested families. "
                    + "An array's copy holding exactly one is locked through its group; name any other by its own id.");
        }
        else if (nested == null)
            problems.Add("\"" + (chosen.Name ?? said) + "\" is not a nested family - PLACE_NESTED_FAMILY gives back "
                + "the id of one it places, and ARRAY_FAMILY_FORMS the ids of its copies.");
    }

    var transform = nested == null ? null : nested.GetTransform();
    var bases = transform == null ? null : new[] { transform.BasisX, transform.BasisY, transform.BasisZ };

    if (locks == null || locks.Count == 0)
        problems.Add("No locks were given - the nested family's side = this family's plane, semicolons between: "
            + "\"Center (Left/Right)=Center (Left/Right); Bottom=Ref. Level\".");
    else if (nested != null)
        foreach (var pair in locks)
        {
            var side = sides.FirstOrDefault(s => s.Item1 == squash(pair.Key));
            if (side == null)
            {
                problems.Add("\"" + pair.Key + "\" is not a side a nested family shows - "
                    + string.Join(", ", sides.Select(s => s.Item2)) + ".");
                continue;
            }
            IList<Reference> references = null;
            try { references = nested.GetReferences(side.Item3); } catch (Exception) { references = null; }
            if (references == null || references.Count == 0)
            {
                var has = sides.Where(s =>
                {
                    try { var r = nested.GetReferences(s.Item3); return r != null && r.Count > 0; }
                    catch (Exception) { return false; }
                }).Select(s => s.Item2).ToList();
                problems.Add("The nested family has no plane that is " + side.Item2 + ". The sides it shows: "
                    + (has.Count == 0 ? "none" : string.Join(", ", has)) + ". SET_FAMILY_PLANE_REFERENCE, run in the "
                    + "nested family, gives a plane one; then load it again.");
                continue;
            }
            if (references.Count > 1)
            {
                problems.Add("The nested family shows " + references.Count + " planes that are " + side.Item2 + " - it "
                    + "should have one.");
                continue;
            }
            var name = (pair.Value ?? "").Trim();
            var found = datums.Where(d => string.Equals(d.Item4, name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 0)
            {
                problems.Add("No reference plane or level is called \"" + name + "\". This family has "
                    + string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + ".");
                continue;
            }
            if (found.Count > 1)
            {
                problems.Add("\"" + name + "\" names " + found.Count + " planes - rename one first.");
                continue;
            }
            var facesAlong = axisOf(bases[side.Item4]);
            if (found[0].Item3 < 0 || facesAlong != found[0].Item3)
            {
                problems.Add("The nested family's " + side.Item2 + " faces " + (facesAlong < 0 ? "at an angle"
                        : axisWords[facesAlong]) + " where it stands, and \"" + name + "\" faces "
                    + (found[0].Item3 < 0 ? "at an angle" : axisWords[found[0].Item3]) + " - they cannot lie on each "
                    + "other. A turned nested family is locked by its sides that face the plane's way.");
                continue;
            }
            plans.Add(Tuple.Create(side, references[0], found[0]));
        }

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
    plan = views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
    elevation = views.Where(v => v.ViewType == ViewType.Elevation)
        .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1).FirstOrDefault();
    if (plans.Any(p => p.Item3.Item3 != 2) && plan == null)
        problems.Add("This family has no floor plan, where a side facing left-right or front-back is locked.");
    if (plans.Any(p => p.Item3.Item3 == 2) && elevation == null)
        problems.Add("This family has no elevation view, where a side facing up or down is locked.");

    if (problems.Count > 0) refused = "Nothing was locked. " + string.Join(" ", problems);
}

if (refused == null)
{
    var start = nested.get_BoundingBox(null);
    foreach (var p in plans)
    {
        var view = p.Item3.Item3 == 2 ? elevation : plan;
        try
        {
            var alignment = doc.FamilyCreate.NewAlignment(view, p.Item3.Item2, p.Item2);
            if (!alignment.IsLocked) alignment.IsLocked = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not lock the nested family's " + p.Item1.Item2 + " to \""
                + p.Item3.Item4 + "\": " + ex.Message + " Revit locks only two that already lie on each other - move "
                + "the nested family onto the plane first. In an array, measured: the last copy of a row spread to "
                + "its last member took a lock, and the second copy of a row stepped member by member was refused. "
                + "The call failed, and Heron rolls the whole call back.");
        }
        locked.Add(p.Item1.Item2 + " to \"" + p.Item3.Item4 + "\" (in " + view.Name + ")");
        doc.Regenerate();
    }

    var end = nested.get_BoundingBox(null);
    if (start == null || end == null)
        throw new InvalidOperationException("The nested family has no extent to read back. The call failed, and Heron "
            + "rolls the whole call back.");
    for (var i = 0; i < 3; i++)
        if (Math.Abs(along(start.Min, i) - along(end.Min, i)) > halfMillimetre
            || Math.Abs(along(start.Max, i) - along(end.Max, i)) > halfMillimetre)
            throw new InvalidOperationException("Locking moved the nested family along " + axisWords[i] + ": "
                + mm(along(start.Min, i)) + " to " + mm(along(start.Max, i)) + " mm before, " + mm(along(end.Min, i))
                + " to " + mm(along(end.Max, i)) + " mm after. A lock never moves what it locks. The call failed, and "
                + "Heron rolls the whole call back.");

    findings.Add("Locked " + locked.Count + " side(s) of the nested family " + nested.UniqueId + ": "
        + string.Join("; ", locked) + ".");
    findings.Add("Locked is not yet PROVED to follow the planes. FLEX_FAMILY changes the sizes, reads the family "
        + "back, and puts them back.");
}

if (refused != null) findings.Add(refused);
