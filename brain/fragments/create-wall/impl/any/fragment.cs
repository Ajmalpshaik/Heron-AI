// NOT STANDALONE. Assumes `doc`, `level`, `wallType`, `points`, `height` and
// `structural` are in scope; leaves `created`, `tooShort`, `refused` and
// `findings` behind.
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

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
height = height / MillimetresPerFoot;

var created = new List<Element>();
var tooShort = 0;
var refused = 0;
var findings = new List<string>();

var shortest = doc.Application.ShortCurveTolerance;

// NOT level.Elevation - see the header.
var z = level.ProjectElevation;

if (height <= 0)
{
    findings.Add("A wall needs a height greater than zero");
}
else
{
    for (var i = 0; i + 1 < points.Count; i++)
    {
        var a = points[i];
        var b = points[i + 1];

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
            var wall = Wall.Create(doc, Line.CreateBound(start, end), wallType.Id, level.Id,
                                   height, 0.0, false, structural);
            if (wall != null) created.Add(wall);
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
}
