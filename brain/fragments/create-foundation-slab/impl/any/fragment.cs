// NOT STANDALONE. Assumes `doc`, `level`, `foundationType` and `boundary` are in
// scope; leaves `created`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, and
// the caller owns it. The slab is made inside a SubTransaction of that
// transaction, so a slab that reads back wrong is put back and nothing is left.
// Lengths are internal FEET; millimetres become feet by / 304.8 (D-20).
//
// REVIT'S STRUCTURE > FOUNDATION > SLAB. A foundation slab is a Floor whose type
// is a Foundation Slab type (FloorType.IsFoundationSlab).
//
// ===========================================================================
// THE CREATION CALL CHANGES AT 2022 - read off the shipped assemblies.
// ===========================================================================
//
//   2020-2022  doc.Create.NewFoundationSlab(CurveArray, FloorType, Level, bool, XYZ)
//              OBSOLETE at 2022, gone from 2023
//   2022-2027  Floor.Create(Document, IList<CurveLoop>, ElementId, ElementId,
//                           bool isStructural, Line slopeArrow, double slope)
//
// So NewFoundationSlab under `#if REVIT2020 || REVIT2021`, Floor.Create on every
// other release; the obsolete call is never compiled on 2022. RevitAPI.xml says
// a null slope arrow makes a flat floor, and does NOT say whether Floor.Create
// with a foundation slab type makes a Structural Foundation. So the category is
// READ BACK: a slab that is not in Structural Foundations is rolled back and
// named, never kept as an ordinary floor.
//
// THE TYPE IS NEVER GUESSED (D-33). `foundationType` names a Foundation Slab
// type by its name or as "Family: Type". Left out it arrives as "list": nothing
// is made and the model's foundation slab types are named with their
// thicknesses - even when there is only one. A name that belongs only to an
// ordinary floor type is refused, by name.
//
// THE BOUNDARY IS CREATE_FLOOR'S WRITING, its reader copied: corners "X,Y,Z"
// with semicolons between, the Z taken from the level, and "arc MX,MY X,Y" for
// an arc edge. THE BASE HEIGHT IS THE LEVEL'S PROJECT ELEVATION, not its
// Elevation - on a model set out to a survey datum the two differ, and the
// wrong one puts the slab metres out with no error.
//
// WHAT IS REPORTED IS READ BACK: after a regenerate the slab is looked at again
// - still in the model, in Structural Foundations, of the type asked - and its
// Area and Thickness are read off its own parameters.

var created = ElementId.InvalidElementId;
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was made.
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
const double SquareMetresPerSquareFoot = 0.09290304;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot, 1).ToString(invariant);

// ---- the type ----------------------------------------------------------------

Func<Element, string> typeLabel = one =>
{
    string name = "", family = "";
    try { name = one.Name ?? ""; } catch (Exception) { }
    try { family = ((ElementType)one).FamilyName ?? ""; } catch (Exception) { }
    return family.Length > 0 ? family + ": " + name : name;
};

// A type's thickness is the width of its layers - what Revit's Edit Type shows.
Func<FloorType, string> typeThickness = one =>
{
    CompoundStructure layers = null;
    try { layers = one.GetCompoundStructure(); } catch (Exception) { layers = null; }
    if (layers == null) return "thickness not readable";
    try { return mm(layers.GetWidth()) + " mm"; } catch (Exception) { return "thickness not readable"; }
};

var said = (foundationType ?? "").Trim();
var listOnly = said.Length == 0 || string.Equals(said, "list", StringComparison.OrdinalIgnoreCase);

var foundationTypes = new List<FloorType>();
var floorTypes = new List<FloorType>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FloorType)))
{
    var one = element as FloorType;
    if (one == null) continue;
    var isFoundation = false;
    try { isFoundation = one.IsFoundationSlab; } catch (Exception) { isFoundation = false; }
    if (isFoundation) foundationTypes.Add(one); else floorTypes.Add(one);
}

var typeNames = new List<string>();
foreach (var one in foundationTypes)
    typeNames.Add("\"" + typeLabel(one) + "\" (" + typeThickness(one) + ")");
var typeChoices = typeNames.Count == 0
    ? "this model has no foundation slab type - load or duplicate one in Revit first, under "
      + "Structure > Foundation > Slab, Edit Type"
    : "the model's foundation slab types are " + string.Join(", ", typeNames.ToArray());

Func<FloorType, bool> namedBy = one =>
{
    string name = "", family = "";
    try { name = one.Name ?? ""; } catch (Exception) { }
    try { family = one.FamilyName ?? ""; } catch (Exception) { }
    return string.Equals(name, said, StringComparison.OrdinalIgnoreCase)
        || string.Equals(family + ": " + name, said, StringComparison.OrdinalIgnoreCase)
        || string.Equals(family + " : " + name, said, StringComparison.OrdinalIgnoreCase);
};

FloorType useType = null;
string problem = null;

if (level == null)
{
    problem = "No level was given, so no foundation slab was made - name the level it sits on";
}
else if (listOnly)
{
    problem = "No foundation slab type was named, so nothing was made - " + typeChoices
        + ". Say which one to use";
}
else
{
    var matches = foundationTypes.Where(one => namedBy(one)).ToList();
    var floorMatches = floorTypes.Where(one => namedBy(one)).ToList();
    if (matches.Count == 1) useType = matches[0];
    else if (matches.Count > 1)
        problem = string.Format("{0} foundation slab types are called \"{1}\" - name it as "
            + "\"Family: Type\"; {2}", matches.Count, said, typeChoices);
    else if (floorMatches.Count > 0)
        problem = string.Format("\"{0}\" is an ordinary floor type (\"{1}\"), not a foundation slab "
            + "type, so nothing was made - a floor of it would schedule as a floor, not a "
            + "foundation; {2}", said, typeLabel(floorMatches[0]), typeChoices);
    else
        problem = string.Format("No foundation slab type called \"{0}\" - {1}", said, typeChoices);
}

// ---- the boundary: CREATE_FLOOR's reader, copied -------------------------------

var shortest = doc.Application.ShortCurveTolerance;

// One point. `flat` lets an arc's point be written X,Y as well - the level
// gives the height either way.
Func<string, bool, XYZ> pointOf = (text, flat) =>
{
    var parts = text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (parts.Count != 3 && !(flat && parts.Count == 2))
    {
        problem = "A point is three numbers in MILLIMETRES, separated by commas - "
                + "\"0, 0, 0\" or \"5000, 3000, 2800\". \"" + text + "\" has "
                + parts.Count + " number" + (parts.Count == 1 ? "" : "s") + " in it.";
        return null;
    }
    var ordinates = new double[3];
    for (var index = 0; index < parts.Count; index++)
    {
        double millimetres;
        if (!double.TryParse(parts[index], System.Globalization.NumberStyles.Float, invariant, out millimetres))
        {
            problem = "\"" + parts[index] + "\" is not a number, and a point is three of them in millimetres. "
                    + "Type digits only - 250 or 250.5, not 250mm.";
            return null;
        }
        if (double.IsNaN(millimetres) || double.IsInfinity(millimetres))
        {
            problem = "\"" + parts[index] + "\" is not a real number, so Heron cannot place a point at it. "
                    + "Type digits only - 250 or 250.5.";
            return null;
        }
        if (Math.Abs(millimetres) > 100.0 * 1000.0 * 1000.0)
        {
            problem = "\"" + parts[index] + "\" is further than 100 km from the origin, which is not a coordinate "
                    + "in any building. Heron reads a point in MILLIMETRES - a number this size usually means it "
                    + "was typed in another unit, or has a digit too many.";
            return null;
        }
        ordinates[index] = millimetres / MillimetresPerFoot;
    }
    return new XYZ(ordinates[0], ordinates[1], ordinates[2]);
};
Func<string, int> numbers = text => text.Split(',').Count(p => p.Trim().Length > 0);

// corners[i] to corners[(i + 1) % n] is one edge, straight when through[i] is
// null and an arc through it when it is not.
var corners = new List<XYZ>();
var through = new List<XYZ>();
var arcs = 0;

if (problem == null)
{
    foreach (var piece in (boundary ?? "").Split(';'))
    {
        var item = piece.Trim();
        if (item.Length == 0) continue;

        var words = System.Text.RegularExpressions.Regex.Replace(item, @"\s*,\s*", ",")
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0 && words[0].ToLowerInvariant() == "arc")
        {
            arcs++;
            if (corners.Count == 0)
            {
                problem = "A boundary starts at a corner - \"" + item + "\" is an arc with nothing before it to "
                        + "start from.";
                break;
            }
            if (words.Length != 3 || new[] { words[1], words[2] }.Any(w => numbers(w) != 2 && numbers(w) != 3))
            {
                problem = "\"" + item + "\" is not an arc written \"arc MX,MY X,Y\" - a point the edge passes "
                        + "through, then where it ends, in millimetres.";
                break;
            }
            var mid = pointOf(words[1], true);
            var end = mid == null ? null : pointOf(words[2], true);
            if (end == null) break;
            through.Add(mid);
            corners.Add(end);
        }
        else
        {
            var corner = pointOf(item, false);
            if (corner == null) break;
            if (corners.Count > 0) through.Add(null);
            corners.Add(corner);
        }
    }

    if (problem == null && corners.Count == 0)
        problem = "No corners were given. Separate them with semicolons and their three millimetre ordinates "
                + "with commas - \"0,0,0; 20000,0,0; 20000,12000,0; 0,12000,0\".";
}

// The edge back to the first corner is straight - or, with an arc in the loop
// and the last corner already on the first, it is the last edge typed.
through.Add(null);
if (problem == null && arcs > 0 && corners.Count > 1)
{
    var last = corners[corners.Count - 1];
    var first = corners[0];
    if (Math.Sqrt(Math.Pow(last.X - first.X, 2) + Math.Pow(last.Y - first.Y, 2)) <= shortest)
    {
        corners.RemoveAt(corners.Count - 1);
        through.RemoveAt(through.Count - 1);
    }
}

if (problem == null && arcs == 0 && corners.Count < 3)
    problem = string.Format("A foundation slab needs at least three corners and {0} were given",
        corners.Count);

// Every arc is checked before anything is made. Three points in a line, or two
// in one place, make no arc - Revit's own refusal names neither point.
for (var i = 0; problem == null && arcs > 0 && i < corners.Count; i++)
{
    if (through[i] == null) continue;
    var a = corners[i];
    var m = through[i];
    var b = corners[(i + 1) % corners.Count];
    var ux = m.X - a.X; var uy = m.Y - a.Y;
    var vx = b.X - a.X; var vy = b.Y - a.Y;
    var lu = Math.Sqrt(ux * ux + uy * uy);
    var lv = Math.Sqrt(vx * vx + vy * vy);
    var lw = Math.Sqrt(Math.Pow(b.X - m.X, 2) + Math.Pow(b.Y - m.Y, 2));
    if (lu <= shortest || lv <= shortest || lw <= shortest || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
        problem = "The arc from " + mm(a.X) + "," + mm(a.Y) + " through " + mm(m.X) + "," + mm(m.Y) + " to "
                + mm(b.X) + "," + mm(b.Y) + " is not an arc - its three points are in a line, or two are in the "
                + "same place.";
}

// ---- the edges ----------------------------------------------------------------

var loopCurves = new List<Curve>();
var arrayCurves = new CurveArray();

if (problem == null)
{
    // NOT level.Elevation - see the header.
    var z = level.ProjectElevation;
    for (var i = 0; i < corners.Count; i++)
    {
        var a = corners[i];
        var b = corners[(i + 1) % corners.Count];
        try
        {
            var m = through[i];
            var edge = m == null
                ? (Curve)Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z))
                : Arc.Create(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z), new XYZ(m.X, m.Y, z));
            loopCurves.Add(edge);
            arrayCurves.Append(edge);
        }
        catch (Exception)
        {
            problem = string.Format("Two corners are in the same place - {0},{1} - so one edge has no "
                + "length", mm(a.X), mm(a.Y));
            break;
        }
    }
}

// ---- the slab -------------------------------------------------------------------

var foundationCategory = new ElementId(BuiltInCategory.OST_StructuralFoundation);
Floor slab = null;
#if REVIT2020 || REVIT2021
var callName = "pre-2022";
#else
var callName = "Revit 2022 and later";
#endif

if (problem == null)
{
    using (var attempt = new SubTransaction(doc))
    {
        try
        {
            attempt.Start();
#if REVIT2020 || REVIT2021
            // The old call wants the TYPE object and a normal saying which side
            // is up; the bool says the slab is structural.
            slab = doc.Create.NewFoundationSlab(arrayCurves, useType, level, true, XYZ.BasisZ);
#else
            var loops = new List<CurveLoop> { CurveLoop.Create(loopCurves) };
            slab = Floor.Create(doc, loops, useType.Id, level.Id, true, (Line)null, 0.0);
#endif
            if (slab == null)
            {
                attempt.RollBack();
                problem = "Revit returned no slab - the usual cause is a boundary that crosses itself";
            }
            else
            {
                // Read back BEFORE it is kept: a slab in the wrong category or of
                // another type is taken back here, never left in the model.
                doc.Regenerate();
                var category = slab.Category;
                if (category == null || category.Id != foundationCategory)
                {
                    var categoryName = category == null ? "no category" : "\"" + category.Name + "\"";
                    attempt.RollBack();
                    slab = null;
                    problem = string.Format("Revit made the slab in {0}, not in Structural Foundations, so "
                        + "it was taken back and nothing was kept - type \"{1}\" through the {2} call",
                        categoryName, typeLabel(useType), callName);
                }
                else if (slab.GetTypeId() != useType.Id)
                {
                    attempt.RollBack();
                    slab = null;
                    problem = string.Format("Revit made the slab of another type than \"{0}\", so it was "
                        + "taken back and nothing was kept", typeLabel(useType));
                }
                else
                {
                    attempt.Commit();
                }
            }
        }
        catch (Exception ex)
        {
            try { attempt.RollBack(); } catch (Exception) { }
            slab = null;
            problem = "Revit refused the foundation slab: " + ex.Message;
        }
    }
}

// ---- read back ------------------------------------------------------------------

if (slab != null)
{
    var now = doc.GetElement(slab.Id) as Floor;
    if (now == null || !now.IsValidObject)
    {
        problem = "Revit took the call but the foundation slab is NOT in the model afterwards";
    }
    else
    {
        created = now.Id;

        var area = "not readable";
        var areaParameter = now.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
        if (areaParameter != null && areaParameter.HasValue)
            area = Math.Round(areaParameter.AsDouble() * SquareMetresPerSquareFoot, 2).ToString(invariant)
                + " m2";

        // The slab's own Thickness; the type's layers if the slab carries none.
        var thickness = "not readable";
        foreach (var which in new[] { BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM,
                                      BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS })
        {
            var parameter = now.get_Parameter(which);
            if (parameter != null && parameter.HasValue && parameter.AsDouble() > 0)
            {
                thickness = mm(parameter.AsDouble()) + " mm";
                break;
            }
        }
        if (thickness == "not readable")
            thickness = typeThickness(useType) + ", from its type";

        var top = "";
        BoundingBoxXYZ box = null;
        try { box = now.get_BoundingBox(null); } catch (Exception) { box = null; }
        if (box != null)
            top = string.Format("; its top is {0} mm and its bottom {1} mm from the level",
                mm(box.Max.Z - level.ProjectElevation), mm(box.Min.Z - level.ProjectElevation));

        report = string.Format("Foundation slab made ({0}), type \"{1}\", on \"{2}\", in Structural "
            + "Foundations: Area {3}, Thickness {4}{5}", now.Id, typeLabel(useType), level.Name, area,
            thickness, top);
        findings.Add(string.Format("Made through the {0} call. {1} edge(s), {2} of them arcs", callName,
            corners.Count, through.Count(t => t != null)));
        findings.Add("It is one slab: nothing has been cut through it and nothing reinforces it here");
    }
}

if (created == ElementId.InvalidElementId)
{
    refused = (problem ?? "No foundation slab was made").TrimEnd('.') + ". Nothing was created.";
    findings.Add(refused);
}
