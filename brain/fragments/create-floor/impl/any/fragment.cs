// NOT STANDALONE. Assumes `doc`, `level`, `floorType`, `boundary` and
// `heightAboveLevel` are in scope; leaves `created`, `edges`, `measuredMm` and
// `findings` behind.
//
// VERSION 3 (2026-11-01) - CREATE_CEILING AND CREATE_ROOF MERGED IN. One closed
// boundary on a level, at the level's PROJECT elevation, made into whatever
// the type is: a floor type a floor (versions 1 and 2, word for word), a
// ceiling type a ceiling - Ceiling.Create, found by name because it arrived in
// Revit 2022, so 2020 and 2021 are refused with the sentence to draw one by
// hand; its height above the level set from `heightAboveLevel` in millimetres
// and read back - and a roof type a FOOTPRINT ROOF, left flat, its edge count
// and its size and base height measured back off Revit. `floorType` is the
// TYPE, a floor, ceiling or roof type, resolved by the add-in among them
// (HostObjAttributes); a wall type is refused by name. The arc writing of
// version 2 serves all three. SLOPE_ROOF_EDGES still slopes a roof; nothing
// here sets a pitch.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// ===========================================================================
// THE CREATION CALL IS DIFFERENT AT EACH END OF THE RANGE.
// ===========================================================================
//
//   up to 2021   doc.Create.NewFloor(CurveArray, FloorType, Level, bool)
//   from 2022    Floor.Create(Document, IList<CurveLoop>, ElementId, ElementId)
//
// Checked against the reference assemblies: Floor.Create is absent on 2020 and
// NewFloor is gone by 2024. Naming either in source breaks the other build, so
// both are looked up at run time and whichever exists is used. The boundary is
// built into both shapes because the two want different ones - that is the only
// real difference between the paths.
//
// THE BASE HEIGHT IS THE LEVEL'S PROJECT ELEVATION, NOT ITS ELEVATION. On a
// model set out to a survey datum those differ by the survey offset, and the
// wrong one puts the slab metres out with no error of any kind.
//
// THE LOOP CLOSES ITSELF and must not cross itself.
//
// IT MAKES ONE FLOOR, NOT A STOREY. Nothing here cuts an opening in anything.
//
// VERSION 2: AN EDGE CAN BE AN ARC (5b-1216). `boundary` is the text as typed,
// read here and no longer by the add-in: corners "X,Y,Z" with semicolons
// between, as before, and an arc "arc MX,MY X,Y" - a point the edge passes
// through, then where it ends - the writing CREATE_FAMILY_EXTRUSION's loops
// already take. A round slab is two arcs, the second ending where the first
// began. Text with no `arc` in it is read exactly as the add-in's OnePoint and
// ManyPoints read it for version 1 - the same three millimetre numbers, the
// same refusals in the same words, the same 100 km bound, the same division by
// 304.8 - and closed by the same lines, so a version 1 call makes the floor it
// made before; a last corner repeating the first is still refused there. With
// an arc, a boundary may end on its first corner - a round slab has to.

var created = ElementId.InvalidElementId;
var findings = new List<string>();
// Version 3, from CREATE_ROOF: a roof's model curves, and the roof measured.
var edges = 0;
var measuredMm = "";

var newStyle = typeof(Floor).GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

var creation = doc.Create;
System.Reflection.MethodInfo oldStyle = null;
foreach (var candidate in creation.GetType().GetMethods())
{
    if (candidate.Name != "NewFloor") continue;
    var parameters = candidate.GetParameters();
    if (parameters.Length == 4 && parameters[0].ParameterType == typeof(CurveArray))
    {
        oldStyle = candidate;
        break;
    }
}

// ---- the text --------------------------------------------------------------

const double MillimetresPerFoot = 304.8;
string problem = null;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var shortest = doc.Application.ShortCurveTolerance;

// One point, as the add-in's OnePoint reads it. `flat` lets an arc's point be
// written X,Y as well - the level gives the height either way.
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
            problem = "\"" + parts[index] + "\" is not a real number, so Heron cannot place a point at it. A "
                    + "value like that usually arrives from a calculation that did not work out. Type digits "
                    + "only - 250 or 250.5.";
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

// The loop: corners[i] to corners[(i + 1) % n] is one edge, straight when
// through[i] is null and an arc through it when it is not.
var corners = new List<XYZ>();
var through = new List<XYZ>();
var arcs = 0;

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
    problem = "No points were given. Separate them with semicolons and their three millimetre ordinates "
            + "with commas - \"0,0,0; 5000,0,0; 5000,3000,0\".";

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

// EVERY ARC IS CHECKED BEFORE THE FLOOR IS MADE. Three points in a line, or
// two in one place, make no arc - Revit's own refusal names neither point.
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot, 1).ToString(invariant);
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

// ---- what the type makes ------------------------------------------------------
//
// VERSION 3: THE TYPE DECIDES. A floor type makes a floor, a ceiling type a
// ceiling, a roof type a footprint roof - by the type's class, and for a
// ceiling its category as well, CREATE_CEILING's check. Anything else - a wall
// type above all - is refused by name, never made into something strange.

var asFloor = floorType as FloorType;
var asRoof = floorType as RoofType;
var ceilingCategory = Category.GetCategory(doc, BuiltInCategory.OST_Ceilings);
var asCeiling = floorType is CeilingType && floorType.Category != null && ceilingCategory != null
    && floorType.Category.Id == ceilingCategory.Id;
var kindWord = asFloor != null ? "floor" : asCeiling ? "ceiling" : asRoof != null ? "roof" : "";
var typeShown = floorType == null ? "(no type)"
    : (string.IsNullOrEmpty(floorType.FamilyName) ? floorType.Name : floorType.FamilyName + ": " + floorType.Name);

// CEILING.CREATE ARRIVED IN REVIT 2022, so it is looked up by name and its
// absence is REPORTED AS AN ANSWER - CREATE_CEILING's rule. Naming the class
// would break the 2020 build.
var ceilingClass = typeof(Element).Assembly.GetType(typeof(Document).Namespace + ".Ceiling");
var ceilingCreate = ceilingClass == null ? null : ceilingClass.GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

// A CEILING'S HEIGHT ABOVE ITS LEVEL, MILLIMETRES IN (D-71), feet inside. NaN
// is "not given" - the card's `absent:` - and only a ceiling takes one.
var heightAsked = !double.IsNaN(heightAboveLevel);
var heightFeet = heightAsked ? heightAboveLevel / MillimetresPerFoot : 0.0;

// ---- the floor, the ceiling or the roof ----------------------------------------

if (problem != null)
{
    // The text, not the model, said no - so nothing is made.
    findings.Add(problem + " Nothing was created.");
}
else if (floorType == null)
{
    findings.Add("No type was given - name a floor, ceiling or roof type. Nothing was created");
}
else if (kindWord.Length == 0)
{
    findings.Add(string.Format("'{0}' is a {1} type, and this makes a floor, a ceiling or a footprint roof from "
        + "the type it is given - a floor type, a ceiling type or a roof type. Nothing was created",
        typeShown, floorType.Category == null ? "non-model" : floorType.Category.Name));
}
else if (kindWord == "ceiling" && !heightAsked)
{
    findings.Add("A ceiling needs its height above the level, in millimetres - heightAboveLevel 2700, for "
        + "one. Nothing was created");
}
else if (kindWord == "ceiling" && (double.IsInfinity(heightAboveLevel) || Math.Abs(heightAboveLevel) > 100000.0))
{
    findings.Add(string.Format("A ceiling {0} mm above its level is not a height in any building - the height is "
        + "MILLIMETRES, so 2.7 m is 2700. Nothing was created", heightAboveLevel));
}
else if (kindWord != "ceiling" && heightAsked)
{
    findings.Add(string.Format("heightAboveLevel is a ceiling's height above its level, and '{0}' is a {1} "
        + "type - a {1} is made on its level and this sets no offset for it. Leave the height out. Nothing "
        + "was created", typeShown, kindWord));
}
else if (kindWord == "floor" && newStyle == null && oldStyle == null)
{
    findings.Add("This Revit has neither Floor.Create nor Document.Create.NewFloor - nothing here can "
        + "make a floor on it");
}
else if (kindWord == "ceiling" && ceilingCreate == null)
{
    findings.Add("This Revit cannot create a ceiling - the method arrived in Revit 2022 and this "
        + "release is older. Draw ONE by hand (Architecture, then Ceiling) and everything else here "
        + "works on it: the height, the grid and the clearance checks all read a ceiling that exists");
}
else if (arcs == 0 && corners.Count < 3)
{
    findings.Add(string.Format("A {0} needs at least three boundary points and {1} were given",
        kindWord, corners.Count));
}
else
{
    // NOT level.Elevation - see the header. The same for all three.
    var z = level.ProjectElevation;

    var loopCurves = new List<Curve>();
    var arrayCurves = new CurveArray();
    var badPoint = false;

    for (var i = 0; i < corners.Count; i++)
    {
        var a = corners[i];
        var b = corners[(i + 1) % corners.Count];
        if (a == null || b == null) { badPoint = true; break; }
        try
        {
            var m = through[i];
            var edge = m == null
                ? (Curve)Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z))
                : Arc.Create(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z), new XYZ(m.X, m.Y, z));
            loopCurves.Add(edge);
            arrayCurves.Append(edge);
        }
        catch
        {
            badPoint = true;
            break;
        }
    }

    if (badPoint)
    {
        findings.Add("Two boundary points are in the same place, so one edge has no length - nothing "
            + "was created");
    }
    else if (kindWord == "floor")
    {
        try
        {
            Element made = null;

            if (newStyle != null)
            {
                var loops = new List<CurveLoop> { CurveLoop.Create(loopCurves) };
                made = (Element)newStyle.Invoke(null,
                    new object[] { doc, (IList<CurveLoop>)loops, floorType.Id, level.Id });
            }
            else
            {
                // The old one wants the TYPE object, not its id, and a bool for
                // whether the floor is structural.
                made = (Element)oldStyle.Invoke(creation,
                    new object[] { arrayCurves, asFloor, level, false });
            }

            if (made == null)
            {
                findings.Add("Revit returned no floor - the usual cause is a boundary that crosses "
                    + "itself");
            }
            else
            {
                created = made.Id;
                findings.Add(string.Format("Floor created on '{0}' using the {1} call. It is a slab and "
                    + "nothing has been cut through it", level.Name,
                    newStyle != null ? "Revit 2022 and later" : "pre-2022"));
                if (arcs > 0)
                    findings.Add(string.Format("{0} of its {1} edges are arcs. Its Area in Properties is "
                        + "measured inside them", through.Count(t => t != null), corners.Count));
            }
        }
        catch (Exception ex)
        {
            var why = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            findings.Add(string.Format("Floor creation failed: {0}", why));
        }
    }
    else if (kindWord == "ceiling")
    {
        // VERSION 3, from CREATE_CEILING: Ceiling.Create by reflection, then its
        // height above the level set and READ BACK in millimetres.
        try
        {
            var loops = new List<CurveLoop> { CurveLoop.Create(loopCurves) };
            var made = (Element)ceilingCreate.Invoke(null,
                new object[] { doc, (IList<CurveLoop>)loops, floorType.Id, level.Id });

            if (made == null)
            {
                findings.Add("Revit returned no ceiling - the usual cause is a boundary that crosses "
                    + "itself");
            }
            else
            {
                created = made.Id;

                var heightParameter = made.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                if (heightParameter != null && !heightParameter.IsReadOnly)
                {
                    heightParameter.Set(heightFeet);
                    doc.Regenerate();
                    // READ IT BACK, in millimetres, because that is the number
                    // somebody will check on screen.
                    var got = heightParameter.AsDouble();
                    var heightSet = Math.Abs(got - heightFeet) * MillimetresPerFoot < 1.0;
                    findings.Add(string.Format(
                        "Ceiling created on '{0}' at {1:0} mm above it{2}",
                        level.Name, got * MillimetresPerFoot,
                        heightSet ? "" : string.Format(" - ASKED for {0:0} mm and Revit settled on "
                            + "{1:0} mm", heightAboveLevel, got * MillimetresPerFoot)));
                }
                else
                {
                    findings.Add(string.Format("Ceiling created on '{0}', but its height above the "
                        + "level could not be set and is whatever the type gives", level.Name));
                }
                if (arcs > 0)
                    findings.Add(string.Format("{0} of its {1} edges are arcs", through.Count(t => t != null),
                        corners.Count));
            }
        }
        catch (Exception ex)
        {
            // The reflection call wraps whatever Revit threw, and the inner one
            // is the message worth reading.
            var why = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            findings.Add(string.Format("Ceiling creation failed: {0}", why));
        }
    }
    else
    {
        // VERSION 3, from CREATE_ROOF: one call on every release - NewFootPrintRoof
        // did not change at 2022 - and the roof is left FLAT.
        try
        {
            // INSTANTIATED EVEN THOUGH IT IS AN `out` - declared `= null` the
            // call threw "Value cannot be null" on a model with a real roof type
            // (CREATE_ROOF, measured 2026-09-19 on `test projject`, Revit 2024).
            ModelCurveArray madeEdges = new ModelCurveArray();
            var roof = doc.Create.NewFootPrintRoof(arrayCurves, level, asRoof, out madeEdges);

            if (roof == null)
            {
                findings.Add("Revit returned no roof - the usual cause is a "
                    + "boundary that crosses itself");
            }
            else
            {
                created = roof.Id;
                edges = madeEdges == null ? 0 : madeEdges.Size;
                findings.Add(string.Format(
                    "Roof created on '{0}' with {1} edge(s). It is FLAT - no "
                    + "slope has been set on any edge, which is a separate job",
                    level.Name, edges));

                // WHAT IT ACTUALLY MADE, MEASURED BACK OFF THE ELEMENT AND IN
                // MILLIMETRES - never recomputed from the input, so a
                // conversion that went wrong on the way in comes back wrong and
                // visible (CREATE_ROOF, PR #198's review).
                try
                {
                    var box = roof.get_BoundingBox(null);
                    if (box != null)
                    {
                        measuredMm = string.Format(
                            "{0:F0} x {1:F0} mm on plan, base at {2:F0} mm, "
                            + "level '{3}' sits at {4:F0} mm",
                            (box.Max.X - box.Min.X) * MillimetresPerFoot,
                            (box.Max.Y - box.Min.Y) * MillimetresPerFoot,
                            box.Min.Z * MillimetresPerFoot,
                            level.Name,
                            level.ProjectElevation * MillimetresPerFoot);
                        findings.Add("Measured back from Revit - see measuredMm");
                    }
                    else
                    {
                        measuredMm = "";
                        findings.Add("Revit returned no bounding box for the new roof, "
                            + "so its size and height were NOT read back");
                    }
                }
                catch (Exception notMeasured)
                {
                    findings.Add(string.Format(
                        "The roof was created but could not be measured back: {0}",
                        notMeasured.Message));
                }
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Roof creation failed: {0}", ex.Message));
        }
    }
}
