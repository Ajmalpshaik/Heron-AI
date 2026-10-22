// NOT STANDALONE. Assumes `doc`, `level`, `floorType` and `boundary` are in
// scope; leaves `created` and `findings` behind.
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

// ---- the floor ---------------------------------------------------------------

if (problem != null)
{
    // The text, not the model, said no - so nothing is made.
    findings.Add(problem + " Nothing was created.");
}
else if (newStyle == null && oldStyle == null)
{
    findings.Add("This Revit has neither Floor.Create nor Document.Create.NewFloor - nothing here can "
        + "make a floor on it");
}
else if (arcs == 0 && corners.Count < 3)
{
    findings.Add(string.Format("A floor needs at least three boundary points and {0} were given",
        corners.Count));
}
else
{
    // NOT level.Elevation - see the header.
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
    else
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
                    new object[] { arrayCurves, floorType, level, false });
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
}
