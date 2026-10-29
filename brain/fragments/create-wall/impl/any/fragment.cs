// NOT STANDALONE. Assumes `doc`, `level`, `wallType`, `points`, `height`,
// `structural` and `baseOffset` are in scope; leaves `created`, `tooShort`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// A RUN OF POINTS, like CREATE_DUCT and CREATE_PIPE - two points is the single
// wall. A room needs four and a corridor needs two, so the run is the general
// form and the library keeps one shape.
//
// THE BASE HEIGHT IS THE LEVEL'S PROJECT ELEVATION, NOT ITS ELEVATION. On a
// model set out to a survey datum those differ by the survey offset, and the
// wrong one builds at the wrong height with no error of any kind. Same defect
// as CREATE_CEILING and CREATE_FLOOR guard against.
//
// THE HEIGHT IS UNCONNECTED. The wall stands at the height given and is not
// attached to the level above - "up to the slab" is a different instruction and
// a different parameter.
//
// NOTHING IS JOINED. Walls meeting at a corner are not joined to each other
// here, and whether Revit joins them itself is not claimed. JOIN_GEOMETRY is
// what makes two elements agree about which one cuts the other.
//
// VERSION 2: A WALL CAN FOLLOW AN ARC (5b-1216). `points` is the text as typed,
// read here and no longer by the add-in: corners "X,Y,Z" with semicolons
// between, as before, and an arc "arc MX,MY X,Y" - a point the wall passes
// through, then where it ends - the writing CREATE_FAMILY_EXTRUSION's loops
// already take. A full circle is two arcs. Text with no `arc` in it is read
// exactly as the add-in's OnePoint and ManyPoints read it for version 1 - the
// same three millimetre numbers, the same refusals in the same words, the same
// 100 km bound, the same division by 304.8 - and built by the same lines, so a
// version 1 call builds what it built before. A bad arc builds nothing at all:
// the next wall would start where it ends.
//
// VERSION 3: A WALL CAN START ABOVE ITS LEVEL (5b-1345). `baseOffset` is
// Revit's Base Offset in millimetres - below the level when negative - and
// left out it is 0. It goes into Wall.Create's own offset argument, which
// versions 1 and 2 always handed 0, and never into the curve: the curve stays
// at the level's project elevation, so the offset cannot be counted twice. The
// height is measured from that base, as Revit measures Unconnected Height - a
// wall 1500 up and 3000 high stands from 1500 to 4500 above its level. With 0,
// every word of the findings is version 2's. Each wall's Base Offset is READ
// BACK off the wall, never echoed from the input, into `offsetsReadMm`.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
height = height / MillimetresPerFoot;
baseOffset = baseOffset / MillimetresPerFoot;

var created = new List<Element>();
var tooShort = 0;
var refused = 0;
var findings = new List<string>();
var offsetsReadMm = new List<double>();

var shortest = doc.Application.ShortCurveTolerance;

// NOT level.Elevation - see the header.
var z = level.ProjectElevation;

// ---- the text --------------------------------------------------------------

string problem = null;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

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

// The run: corners[i] to corners[i + 1] is one wall, straight when through[i]
// is null and an arc through it when it is not.
var corners = new List<XYZ>();
var through = new List<XYZ>();
var arcs = 0;

foreach (var piece in (points ?? "").Split(';'))
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
            problem = "A run starts at a point - \"" + item + "\" is an arc with nothing before it to start from.";
            break;
        }
        if (words.Length != 3 || new[] { words[1], words[2] }.Any(w => numbers(w) != 2 && numbers(w) != 3))
        {
            problem = "\"" + item + "\" is not an arc written \"arc MX,MY X,Y\" - a point the wall passes "
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

// EVERY ARC IS CHECKED BEFORE ANY WALL IS BUILT. Three points in a line, or two
// in one place, make no arc - Revit's own refusal names neither point.
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot, 1).ToString(invariant);
for (var i = 0; problem == null && i < through.Count; i++)
{
    if (through[i] == null) continue;
    var a = corners[i];
    var m = through[i];
    var b = corners[i + 1];
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

// ---- the walls ---------------------------------------------------------------

if (problem != null)
{
    // The text, not the model, said no - so nothing at all is built.
    refused++;
    findings.Add(problem + " Nothing was built.");
}
else if (height <= 0)
{
    findings.Add("A wall needs a height greater than zero");
}
else if (double.IsNaN(baseOffset) || double.IsInfinity(baseOffset) || Math.Abs(baseOffset) > 30000.0)
{
    // REVIT'S OWN LIMIT, read off its API documentation for 2020 and 2027:
    // Wall.Create takes an offset of no more than 30000 feet either way, and
    // past it throws for every wall. Said once, in words, before any is tried.
    refused++;
    findings.Add("A base offset is millimetres above the level - 1500, or -300 below it - and Revit "
               + "takes none past 9144000 (30000 feet) either way. "
               + (baseOffset * MillimetresPerFoot).ToString(invariant) + " is not one Heron can start "
               + "a wall at. Nothing was built.");
}
else
{
    var curved = 0;
    for (var i = 0; i + 1 < corners.Count; i++)
    {
        var a = corners[i];
        var b = corners[i + 1];

        if (a == null || b == null) { refused++; continue; }

        var start = new XYZ(a.X, a.Y, z);
        var end = new XYZ(b.X, b.Y, z);

        if (start.DistanceTo(end) <= shortest)
        {
            // A question about the POINTS, not about the model - the caller can
            // fix it, so it is counted apart from a refusal.
            tooShort++;
            continue;
        }

        try
        {
            var m = through[i];
            var wall = Wall.Create(doc,
                                   m == null ? (Curve)Line.CreateBound(start, end)
                                             : Arc.Create(start, end, new XYZ(m.X, m.Y, z)),
                                   wallType.Id, level.Id, height, baseOffset, false, structural);
            if (wall != null)
            {
                created.Add(wall);
                if (m != null) curved++;
                var read = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                if (read != null) offsetsReadMm.Add(Math.Round(read.AsDouble() * MillimetresPerFoot, 1));
            }
            else refused++;
        }
        catch
        {
            refused++;
        }
    }

    findings.Add(string.Format(
        "{0} wall(s) built on '{1}' at {2:0} mm high, unconnected to the level above. {3} refused, {4} "
        + "skipped as too short. NOTHING has been joined - two walls meeting at a corner are not joined "
        + "to each other by this",
        created.Count, level.Name, height * 304.8, refused, tooShort));
    if (arcs > 0)
        findings.Add(string.Format("{0} of them follow an arc, through the point given for it. A curved "
            + "wall's Length in Properties is measured along its arc", curved));

    // Said only when it is not version 2's answer - an offset asked for, or a
    // wall whose Base Offset did not read back as the one asked.
    var askedMm = Math.Round(baseOffset * MillimetresPerFoot, 1);
    var unlike = offsetsReadMm.Where(v => Math.Abs(v - askedMm) > 0.5).ToList();
    var unread = created.Count - offsetsReadMm.Count;
    if (created.Count > 0 && (askedMm != 0 || unlike.Count > 0 || unread > 0))
        findings.Add(string.Format(invariant,
            "Base Offset {0:+0;-0;0} mm from '{1}', so each wall stands from {0:+0;-0;0} to {2:+0;-0;0} mm, "
            + "its {3:0} mm height measured from its base. Read back off the walls: {4} of {5} as asked",
            askedMm, level.Name, askedMm + height * MillimetresPerFoot, height * MillimetresPerFoot,
            offsetsReadMm.Count - unlike.Count, created.Count)
            + (unlike.Count > 0 ? " - NOT AS ASKED: " + string.Join(", ", unlike.Select(v => v.ToString(invariant) + " mm")) : "")
            + (unread > 0 ? " - " + unread + " with no Base Offset to read" : ""));
}
