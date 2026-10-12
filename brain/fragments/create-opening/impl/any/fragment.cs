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
            // A BEAM, BRACE OR COLUMN takes Revit's own framing opening, the
            // overload whose third argument is a reference face (version 2,
            // 2026-10-12, the gap fill): perpendicularFace true cuts through
            // the web, false through the flanges. That face type lives in a
            // namespace the fragment contract does not supply, so the
            // overload is found by its parameter's type name and the value
            // parsed from its own name - nothing is written out in full.
            // Every other host keeps the wall-and-slab call it always had.
            var framingHost = host is FamilyInstance && host.Category != null
                && (host.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming)
                    || host.Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns));
            Opening opening = null;
            if (framingHost)
            {
                var creator = doc.Create;
                var method = creator.GetType().GetMethods().FirstOrDefault(m => m.Name == "NewOpening"
                    && m.GetParameters().Length == 3
                    && m.GetParameters()[2].ParameterType.Name == "eRefFace");
                if (method == null)
                    throw new InvalidOperationException("this Revit has no framing opening call");
                var face = Enum.Parse(method.GetParameters()[2].ParameterType, perpendicularFace ? "CenterY" : "CenterZ");
                try { opening = method.Invoke(creator, new object[] { host, profile, face }) as Opening; }
                catch (System.Reflection.TargetInvocationException wrapped)
                {
                    throw wrapped.InnerException ?? wrapped;
                }
            }
            else opening = doc.Create.NewOpening(host, profile, perpendicularFace);

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
