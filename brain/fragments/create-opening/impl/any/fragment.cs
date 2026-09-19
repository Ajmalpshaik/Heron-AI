// NOT STANDALONE. Assumes `doc`, `host`, `boundary` and `perpendicularFace`;
// leaves `created` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// THIS IS PROPOSE_MEP_OPENINGS' MISSING SECOND HALF. That fragment says in its
// own purpose that cutting is "a separate write that somebody approved", and
// measured on 2026-09-18 no such write existed anywhere in the library.
//
// THE BOUNDARY IS THE CALLER'S AND IS IN THE HOST'S PLANE. Revit projects the
// profile onto the host, so a boundary worked out against the wrong face gives
// an opening in the wrong place rather than an error.

var created = ElementId.InvalidElementId;
var findings = new List<string>();

if (host == null)
{
    findings.Add("No host was given. An opening is cut THROUGH something - a "
        + "wall, a floor, a roof or a ceiling");
}
else if (boundary == null || boundary.Count < 3)
{
    findings.Add(string.Format(
        "An opening needs at least three boundary points and {0} were given",
        boundary == null ? 0 : boundary.Count));
}
else
{
    var profile = new CurveArray();
    var badPoint = false;

    for (var i = 0; i < boundary.Count; i++)
    {
        var a = boundary[i];
        var b = boundary[(i + 1) % boundary.Count];
        if (a == null || b == null) { badPoint = true; break; }
        try
        {
            profile.Append(Line.CreateBound(a, b));
        }
        catch
        {
            badPoint = true;
            break;
        }
    }

    if (badPoint)
    {
        findings.Add("Two boundary points are in the same place, so one edge of "
            + "the opening has no length - nothing was cut");
    }
    else
    {
        try
        {
            var opening = doc.Create.NewOpening(host, profile, perpendicularFace);

            if (opening == null)
            {
                findings.Add("Revit returned no opening - the usual causes are a "
                    + "profile that crosses itself, or one that does not lie on "
                    + "the host");
            }
            else
            {
                created = opening.Id;
                var what = host.Category == null ? "the host" : host.Category.Name;
                findings.Add(string.Format(
                    "Opening cut through {0}, {1} to the face. The host is now "
                    + "penetrated - check it in a section before relying on it",
                    what, perpendicularFace ? "perpendicular" : "vertical"));
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Opening failed: {0}", ex.Message));
        }
    }
}
