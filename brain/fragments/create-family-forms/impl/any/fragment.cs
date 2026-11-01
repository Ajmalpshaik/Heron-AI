// NOT STANDALONE. Assumes `doc` and `forms` are in scope; leaves `formIds`,
// `formId`, `formCount`, `built`, `notAFamily`, `refused` and `findings`
// behind.
//
// VERSION 2 (2026-11-01) took in CREATE_FAMILY_EXTRUSION and
// CREATE_FAMILY_REVOLUTION, which built one form a call and are DEPRECATED,
// merged into this card - a list of one row is the single-form call. What
// their read-back said that this one did not is said here now: an
// extrusion's loops, how many are outlines and how many holes inside them,
// by where they lie and never by their order; a revolve's angles and its
// axis; the plane's facing and where it lies; and the part-turn warning. And
// `formId`, the FIRST form's id, beside `formIds`, so every method step and
// chain written for the single-form tools' `formId` binds unchanged.
//
// VERSION 3 (2026-11-01) took in CREATE_FAMILY_BLEND, CREATE_FAMILY_SWEEP and
// CREATE_FAMILY_SWEPT_BLEND, merged into this card and DEPRECATED. A row may
// now be `kind=blend` (workPlane, baseProfile, topProfile, topMm, solid),
// `kind=sweep` (workPlane, path, profile, solid) or `kind=swept blend`
// (workPlane, path, bottomProfile, topProfile, solid) - each card's own
// fields, read as that card read them, with its own checks and read-back:
//   A BLEND'S profiles are one loop each, the top `topMm` along the axis the
//   plane faces; a circle is split in two before Revit sees it; which way it
//   runs is Revit's call from the way the loops go round (5b-369), so up to
//   four ways are built - base first as written, reversed, then top first -
//   and each READ BACK (plane to top along the axis, the base's outline on
//   the plane and the top's at the top), a wrong one deleted before the next.
//   A SWEEP'S path is open corners and arcs, never closed back to its start
//   by anything but its own last corner; its profile is drawn about its own
//   origin, loops | between, a loop inside another a hole by where it lies
//   (5b-276); the profile's area and the path's length are said beside the
//   volume, so a sweep far from area x length is seen at once.
//   A SWEPT BLEND'S path is ONE straight piece or ONE arc; its two profiles
//   are one loop each, bottom at the path's start and top at its end.
// Every row's id is in `formIds`, so SET_FAMILY_SWEEP_PROFILE takes a
// sweep's id from here as it took CREATE_FAMILY_SWEEP's `formId`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one - so the
// whole list is ONE entry in the Family Editor's undo list, and one Undo takes
// every form back (Article 8).
//
// MANY FORMS IN ONE CALL, IN THE ORDER LISTED: extrusions and revolves, solid
// or void, each on its own plane with its own profile and extent - the values
// CREATE_FAMILY_EXTRUSION and CREATE_FAMILY_REVOLUTION take one form at a
// time, under the same names - and, if asked, its material and the detail
// levels and views it shows in. Q-62: a 107-form pump set took 11 batches of
// single-form calls, every profile typed again as tool input, 11 more turns
// carrying the ids forward, and about 130 undo entries where Article 8 wants
// one per request.
//
// THE LIST. One row per form, each starting `kind=extrusion` or
// `kind=revolve`; a row runs until the next `kind=`. Fields are `name=value`,
// a field running until the next field's name:
//   kind=extrusion workPlane=Ref. Level profile=circle 0,0 150 startMm=0
//     endMm=500 solid=true material=Steel
//   kind=revolve workPlane=Center (Front/Back) profile=0,0; 40,0; 40,80; 0,80
//     axisLine=0,0 0,100 startDegrees=0 endDegrees=360 solid=false
// The same list may be a FILE, its full path given instead: one row per line,
// lines starting # ignored, inside the temporary folder (%TEMP%) - the folder
// both a chat and Revit see on a PC whose Claude app keeps a private copy of
// AppData. Where Revit's own temporary folder is a GUID-named sub-folder of
// the user's, the user's %TEMP%\heron-work is read too (5b-430).
//
// ALL OR NOTHING. Every row is read and checked before anything is built, and
// every problem is named with its row. Each form is then built, and read back
// as the single-form tools read theirs: an extrusion's extent along and across
// its plane, a full revolve's extent about its axis, a solid's volume, and a
// material or visibility asked for. One row Revit refuses or that reads back
// wrong throws, and the host rolls the whole call back - none of the forms is
// kept.
//
// A VOID CUTS NOTHING BY BEING MADE. Through the API a void in an ordinary
// family cuts a solid only once the two are combined - COMBINE_FAMILY_FORMS,
// with the ids handed back here.

var findings = new List<string>();
var formIds = "";
// VERSION 2: the first form's id alone - the single-form tools' `formId`.
var formId = "";
var formCount = 0;
var built = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var ignoreCase = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
// A solid's volume in words: litres to a thousandth from one litre up, cubic
// millimetres below - a small part never reads 0 L (5b-398).
Func<double, string> volumeWords = cubicFeet =>
{
    var cubicMm = cubicFeet * 304.8 * 304.8 * 304.8;
    return cubicMm >= 1e6 ? Math.Round(cubicMm / 1e6, 3).ToString(invariant) + " L"
        : Math.Round(cubicMm, 1).ToString(invariant) + " mm³";
};

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisLetters = new[] { "X", "Y", "Z" };
var facing = new[] { "facing left or right", "facing front or back", "horizontal" };
// The two model coordinates a profile is written in, per axis the plane faces.
var inPlane = new[] { new[] { 1, 2 }, new[] { 0, 2 }, new[] { 0, 1 } };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

Func<string, double?> number = text =>
{
    double value;
    if (!double.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, invariant, out value))
        return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

Func<string, double[]> pair = text =>
{
    var parts = (text ?? "").Split(',');
    if (parts.Length != 2) return null;
    var first = number(parts[0]);
    var second = number(parts[1]);
    return first.HasValue && second.HasValue ? new[] { first.Value, second.Value } : null;
};

var problems = new List<string>();
// Revit's own shortest curve, in millimetres - a segment shorter is refused by
// Revit with a message nobody can place, so it is refused here by name.
var shortest = doc.Application.ShortCurveTolerance * 304.8;

// ONE LOOP, as segments {kind, u0, v0, um, vm, u1, v1} in millimetres - kind 0
// a straight line, 1 an arc through (um, vm). Null, with the reason added to
// `problems`, when the text is not a closed loop.
Func<string, string, List<double[]>> loopOf = (text, where) =>
{
    var said = System.Text.RegularExpressions.Regex.Replace((text ?? "").Trim(), @"\s*,\s*", ",");
    var words = said.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    var head = words.Length == 0 ? "" : words[0].ToLowerInvariant();
    var segments = new List<double[]>();

    if (head == "circle")
    {
        var centre = words.Length == 3 ? pair(words[1]) : null;
        var radius = words.Length == 3 ? number(words[2]) : null;
        if (centre == null || !radius.HasValue || radius.Value <= 0)
        {
            problems.Add(where + " is not a circle written \"circle CX,CY R\" - its centre, then its radius in "
                + "millimetres, as in \"circle 0,0 150\". It reads \"" + said + "\".");
            return null;
        }
        var r = radius.Value;
        // TWO HALVES, NEVER ONE CLOSED CURVE. A blend and a swept blend refuse a
        // loop of one curve outright, and an extrusion splits it in two anyway.
        segments.Add(new[] { 1.0, centre[0] + r, centre[1], centre[0], centre[1] + r, centre[0] - r, centre[1] });
        segments.Add(new[] { 1.0, centre[0] - r, centre[1], centre[0], centre[1] - r, centre[0] + r, centre[1] });
    }
    else if (head == "rect")
    {
        var a = words.Length == 3 ? pair(words[1]) : null;
        var b = words.Length == 3 ? pair(words[2]) : null;
        if (a == null || b == null)
        {
            problems.Add(where + " is not a rectangle written \"rect X1,Y1 X2,Y2\" - two opposite corners, as in "
                + "\"rect -300,-200 300,200\". It reads \"" + said + "\".");
            return null;
        }
        segments.Add(new[] { 0.0, a[0], a[1], 0, 0, b[0], a[1] });
        segments.Add(new[] { 0.0, b[0], a[1], 0, 0, b[0], b[1] });
        segments.Add(new[] { 0.0, b[0], b[1], 0, 0, a[0], b[1] });
        segments.Add(new[] { 0.0, a[0], b[1], 0, 0, a[0], a[1] });
    }
    else if (head == "polygon")
    {
        var centre = words.Length == 4 ? pair(words[1]) : null;
        var radius = words.Length == 4 ? number(words[2]) : null;
        var sides = words.Length == 4 ? number(words[3]) : null;
        if (centre == null || !radius.HasValue || radius.Value <= 0 || !sides.HasValue
            || sides.Value != Math.Floor(sides.Value) || sides.Value < 3 || sides.Value > 64)
        {
            problems.Add(where + " is not a regular polygon written \"polygon CX,CY R N\" - its centre, the radius "
                + "to a corner and 3 to 64 sides, as in \"polygon 0,0 100 6\". It reads \"" + said + "\".");
            return null;
        }
        var n = (int)sides.Value;
        for (var k = 0; k < n; k++)
        {
            var a0 = 2 * Math.PI * k / n;
            var a1 = 2 * Math.PI * (k + 1) / n;
            segments.Add(new[] { 0.0,
                centre[0] + radius.Value * Math.Cos(a0), centre[1] + radius.Value * Math.Sin(a0), 0, 0,
                centre[0] + radius.Value * Math.Cos(a1), centre[1] + radius.Value * Math.Sin(a1) });
        }
    }
    else
    {
        var items = said.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var first = items.Count == 0 ? null : pair(items[0]);
        if (first == null)
        {
            problems.Add(where + " does not start with a corner \"X,Y\". A loop is \"circle ...\", \"rect ...\", "
                + "\"polygon ...\" or corners with semicolons between, as in \"0,0; 600,0; 600,300; 0,300\". It "
                + "reads \"" + said + "\".");
            return null;
        }
        var at = first;
        foreach (var item in items.Skip(1))
        {
            var parts = item.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0].ToLowerInvariant() == "arc")
            {
                var through = parts.Length == 3 ? pair(parts[1]) : null;
                var end = parts.Length == 3 ? pair(parts[2]) : null;
                if (through == null || end == null)
                {
                    problems.Add(where + ": \"" + item + "\" is not an arc written \"arc MX,MY X,Y\" - a point "
                        + "the arc passes through, then where it ends.");
                    return null;
                }
                segments.Add(new[] { 1.0, at[0], at[1], through[0], through[1], end[0], end[1] });
                at = end;
            }
            else
            {
                var next = parts.Length == 1 ? pair(parts[0]) : null;
                if (next == null)
                {
                    problems.Add(where + ": \"" + item + "\" is neither a corner \"X,Y\" nor an arc \"arc MX,MY X,Y\".");
                    return null;
                }
                segments.Add(new[] { 0.0, at[0], at[1], 0, 0, next[0], next[1] });
                at = next;
            }
        }
        // CLOSED BACK TO THE FIRST CORNER - by a straight line when the last
        // corner is not already there, by snapping it when it is all but there.
        var gap = Math.Sqrt(Math.Pow(at[0] - first[0], 2) + Math.Pow(at[1] - first[1], 2));
        if (gap >= shortest) segments.Add(new[] { 0.0, at[0], at[1], 0, 0, first[0], first[1] });
        else if (segments.Count > 0)
        {
            var last = segments[segments.Count - 1];
            last[5] = first[0];
            last[6] = first[1];
        }
    }

    if (segments.Count < 2 || (segments.Count < 3 && segments.All(s => s[0] == 0.0)))
    {
        problems.Add(where + " has " + segments.Count + " side(s) - a closed loop needs at least three straight "
            + "sides, or an arc and one more side.");
        return null;
    }

    foreach (var s in segments)
    {
        var chord = Math.Sqrt(Math.Pow(s[5] - s[1], 2) + Math.Pow(s[6] - s[2], 2));
        if (s[0] == 0.0 && chord < shortest)
        {
            problems.Add(where + " has a side from " + plain(s[1]) + "," + plain(s[2]) + " to " + plain(s[5]) + ","
                + plain(s[6]) + " shorter than Revit's shortest line, " + plain(shortest) + " mm.");
            return null;
        }
        if (s[0] == 1.0)
        {
            var ux = s[3] - s[1]; var uy = s[4] - s[2];
            var vx = s[5] - s[1]; var vy = s[6] - s[2];
            var lu = Math.Sqrt(ux * ux + uy * uy);
            var lv = Math.Sqrt(vx * vx + vy * vy);
            var lw = Math.Sqrt(Math.Pow(s[5] - s[3], 2) + Math.Pow(s[6] - s[4], 2));
            if (lu < shortest || lv < shortest || lw < shortest
                || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
            {
                problems.Add(where + " has an arc from " + plain(s[1]) + "," + plain(s[2]) + " through "
                    + plain(s[3]) + "," + plain(s[4]) + " to " + plain(s[5]) + "," + plain(s[6]) + " that is not "
                    + "an arc - its three points are in a line, or two are in the same place.");
                return null;
            }
        }
    }

    // A LOOP WITH NO AREA - every corner on one line - is refused here rather
    // than by Revit. The corners and each arc's middle point are enough to tell.
    var ring = new List<double[]>();
    foreach (var s in segments)
    {
        ring.Add(new[] { s[1], s[2] });
        if (s[0] == 1.0) ring.Add(new[] { s[3], s[4] });
    }
    var area = 0.0;
    for (var i = 0; i < ring.Count; i++)
    {
        var p = ring[i];
        var q = ring[(i + 1) % ring.Count];
        area += p[0] * q[1] - q[0] * p[1];
    }
    if (Math.Abs(area) / 2 < shortest * shortest)
    {
        problems.Add(where + " encloses no area - its corners lie on one line.");
        return null;
    }
    return segments;
};

// VERSION 3, CREATE_FAMILY_SWEEP's reader: A PATH, as segments in the same
// shape as a loop's - but open, and never closed back to its start by anything
// but its own last corner.
Func<string, string, List<double[]>> pathOf = (text, where) =>
{
    var said = System.Text.RegularExpressions.Regex.Replace((text ?? "").Trim(), @"\s*,\s*", ",");
    var items = said.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    var first = items.Count == 0 ? null : pair(items[0]);
    if (first == null)
    {
        problems.Add(where + ": the path does not start with a point \"X,Y\". A path is corners with semicolons "
            + "between, and arcs \"arc MX,MY X,Y\" - \"0,0; 1000,0; arc 1300,300 1000,600\". It reads \"" + said + "\".");
        return null;
    }
    var segments = new List<double[]>();
    var at = first;
    foreach (var item in items.Skip(1))
    {
        var parts = item.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && parts[0].ToLowerInvariant() == "arc")
        {
            var through = parts.Length == 3 ? pair(parts[1]) : null;
            var end = parts.Length == 3 ? pair(parts[2]) : null;
            if (through == null || end == null)
            {
                problems.Add(where + ": the path's \"" + item + "\" is not an arc written \"arc MX,MY X,Y\".");
                return null;
            }
            segments.Add(new[] { 1.0, at[0], at[1], through[0], through[1], end[0], end[1] });
            at = end;
        }
        else
        {
            var next = parts.Length == 1 ? pair(parts[0]) : null;
            if (next == null)
            {
                problems.Add(where + ": the path's \"" + item + "\" is neither a point \"X,Y\" nor an arc \"arc MX,MY X,Y\".");
                return null;
            }
            segments.Add(new[] { 0.0, at[0], at[1], 0, 0, next[0], next[1] });
            at = next;
        }
    }
    if (segments.Count == 0)
    {
        problems.Add(where + ": the path has one point and no length - it needs at least a second point.");
        return null;
    }
    foreach (var s in segments)
    {
        var chord = Math.Sqrt(Math.Pow(s[5] - s[1], 2) + Math.Pow(s[6] - s[2], 2));
        if (s[0] == 0.0 && chord < shortest)
        {
            problems.Add(where + ": the path has a piece from " + plain(s[1]) + "," + plain(s[2]) + " to " + plain(s[5])
                + "," + plain(s[6]) + " shorter than Revit's shortest line, " + plain(shortest) + " mm.");
            return null;
        }
        if (s[0] == 1.0)
        {
            var ux = s[3] - s[1]; var uy = s[4] - s[2];
            var vx = s[5] - s[1]; var vy = s[6] - s[2];
            var lu = Math.Sqrt(ux * ux + uy * uy);
            var lv = Math.Sqrt(vx * vx + vy * vy);
            var lw = Math.Sqrt(Math.Pow(s[5] - s[3], 2) + Math.Pow(s[6] - s[4], 2));
            if (lu < shortest || lv < shortest || lw < shortest || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
            {
                problems.Add(where + ": the path's arc from " + plain(s[1]) + "," + plain(s[2]) + " through "
                    + plain(s[3]) + "," + plain(s[4]) + " to " + plain(s[5]) + "," + plain(s[6]) + " is not an arc - "
                    + "its three points are in a line, or two are in the same place.");
                return null;
            }
        }
    }
    // A LAST CORNER ALL BUT ON THE FIRST CLOSES THE PATH EXACTLY.
    var lastPiece = segments[segments.Count - 1];
    var gap = Math.Sqrt(Math.Pow(lastPiece[5] - first[0], 2) + Math.Pow(lastPiece[6] - first[1], 2));
    if (gap > 1e-9 && gap < shortest)
    {
        lastPiece[5] = first[0];
        lastPiece[6] = first[1];
    }
    return segments;
};

// ---- the planes -------------------------------------------------------------

// (element, axis it faces, position along that axis in feet, name) for every
// named reference plane and every level.
var datums = new List<Tuple<Element, int, double, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var name = ownName(rp);
        if (name.Length == 0) continue;
        var at = axisOf(rp.Normal);
        datums.Add(Tuple.Create((Element)rp, at, at < 0 ? 0.0 : along(rp.GetPlane().Origin, at), name));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, 2, level.Elevation, level.Name));
}

// ---- a material, read as LINK_FAMILY_PARAMETER reads one --------------------

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

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

// Is this family parameter a MATERIAL parameter? From 2022 its data type is
// compared with the material spec; before that its parameter type is read -
// that enum's Material member is gone from 2023.
Func<FamilyParameter, bool> isMaterial = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId+Reference");
            var property = owner == null ? null : owner.GetProperty("Material", flags);
            var wanted = property == null ? null : property.GetValue(null);
            return sameKind(spec, wanted);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Material";
    }
    catch (Exception)
    {
        return false;
    }
};

// ---- the rows ---------------------------------------------------------------

// Every field a row may carry - the single-form tools' own names for them.
var fieldNames = new[] { "kind", "workPlane", "profile", "startMm", "endMm", "axisLine", "startDegrees",
    "endDegrees", "solid", "material", "detailLevels", "views",
    // VERSION 3: CREATE_FAMILY_BLEND's, CREATE_FAMILY_SWEEP's and
    // CREATE_FAMILY_SWEPT_BLEND's own names.
    "baseProfile", "topProfile", "topMm", "path", "bottomProfile" };
// Each kind's own fields, and every field that belongs to a kind - so a field
// of another kind in a row is refused rather than ignored.
var kindFields = new Dictionary<string, string[]>
{
    { "extrusion", new[] { "workPlane", "profile", "startMm", "endMm", "solid" } },
    { "revolve", new[] { "workPlane", "profile", "axisLine", "startDegrees", "endDegrees", "solid" } },
    { "blend", new[] { "workPlane", "baseProfile", "topProfile", "topMm", "solid" } },
    { "sweep", new[] { "workPlane", "path", "profile", "solid" } },
    { "swept blend", new[] { "workPlane", "path", "bottomProfile", "topProfile", "solid" } },
};
var kindOwned = new[] { "profile", "startMm", "endMm", "axisLine", "startDegrees", "endDegrees", "baseProfile",
    "topProfile", "topMm", "path", "bottomProfile" };
Func<string, string> kindWords = k => k == "extrusion" ? "an extrusion" : k == "revolve" ? "a revolve"
    : k == "blend" ? "a blend" : k == "sweep" ? "a sweep" : "a swept blend";
var mostRows = 200;
Func<string, string> shortened = text =>
{
    var flat = System.Text.RegularExpressions.Regex.Replace(text ?? "", @"\s+", " ").Trim();
    return flat.Length > 80 ? flat.Substring(0, 77) + "..." : flat;
};

// ONE ENTRY PER ROW in each list, in the order listed - what the row asks for,
// already checked. A row with a problem adds nothing, and nothing is built.
var rowKind = new List<string>();
var rowPlane = new List<Tuple<Element, int, double, string>>();
var rowLoops = new List<List<List<double[]>>>();
var rowFrom = new List<double>();
var rowTo = new List<double>();
var rowAxis = new List<double[]>();
var rowSolid = new List<bool>();
var rowParameter = new List<FamilyParameter>();
var rowMaterial = new List<Material>();
var rowByCategory = new List<bool>();
var rowShows = new List<bool[]>();
var rowSnapped = new List<int>();
// VERSION 3: a blend's and a swept blend's second profile, one loop - and a
// sweep's or a swept blend's path. Null for the kinds that have none.
var rowTopLoop = new List<List<double[]>>();
var rowPath = new List<List<double[]>>();
var rowCount = 0;
var everyKind = "kind=extrusion, kind=revolve, kind=blend, kind=sweep or kind=swept blend";

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's forms are "
        + "built inside the family - open it first.";
}
else
{
    // THE LIST, OR THE FILE THAT HOLDS IT. A value with no `kind=` in it is
    // read as a file's full path, and only a file in the temporary folder is
    // read - where the chat writes it and Revit can see it.
    var said = (forms ?? "").Trim();
    string rowsText = null;
    if (said.Length == 0)
        problems.Add("No forms were listed. Each row starts " + everyKind + " - or give the full "
            + "path of a file that holds the rows.");
    else if (System.Text.RegularExpressions.Regex.IsMatch(said, @"\bkind\s*=", ignoreCase))
        rowsText = said;
    else
    {
        var path = said.Trim('"', '\'');
        var temporary = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
        // 5b-430: a Revit may run with its temporary folder moved into a
        // sub-folder of the user's own, named by a GUID, while the chat writes
        // to the user's own %TEMP%\heron-work. That one folder of the parent is
        // read as well - never the rest of the parent.
        string shared = null;
        var temporaryName = System.IO.Path.GetFileName(temporary.TrimEnd(System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar));
        Guid guidName;
        if (Guid.TryParse(temporaryName ?? "", out guidName))
        {
            var parent = System.IO.Path.GetDirectoryName(temporary.TrimEnd(System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(parent))
                shared = System.IO.Path.Combine(parent, "heron-work") + System.IO.Path.DirectorySeparatorChar;
        }
        string full = null;
        try { if (System.IO.Path.IsPathRooted(path)) full = System.IO.Path.GetFullPath(path); }
        catch (Exception) { full = null; }
        if (full == null)
            problems.Add("\"" + shortened(said) + "\" is neither a list of forms - each row starts " + everyKind
                + " - nor the full path of a file that holds one.");
        else if (!full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase)
            && (shared == null || !full.StartsWith(shared, StringComparison.OrdinalIgnoreCase)))
            problems.Add("The file " + full + " is not in the temporary folder, " + temporary
                + (shared == null ? "" : ", nor in " + shared) + ". Write the list under "
                + (shared ?? temporary + "heron-work") + " - the folder both the chat and Revit can see.");
        else if (!System.IO.File.Exists(full))
            problems.Add("There is no file at " + full + ".");
        else
        {
            try { rowsText = System.IO.File.ReadAllText(full); }
            catch (Exception ex) { problems.Add("The file " + full + " could not be read: " + ex.Message); }
        }
    }

    var pieces = new List<string>();
    if (rowsText != null)
    {
        var kept = rowsText.Replace("\r", "\n").Split('\n').Where(line => !line.TrimStart().StartsWith("#"));
        var whole = string.Join(" ", kept).Trim();
        pieces = System.Text.RegularExpressions.Regex.Split(whole, @"(?=\bkind\s*=)", ignoreCase)
            .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (pieces.Count > 0 && !System.Text.RegularExpressions.Regex.IsMatch(pieces[0], @"^kind\s*=", ignoreCase))
        {
            problems.Add("The list has \"" + shortened(pieces[0]) + "\" before its first row - every row starts "
                + everyKind + ".");
            pieces.RemoveAt(0);
        }
        if (pieces.Count == 0 && problems.Count == 0)
            problems.Add("The list holds no rows - each row starts " + everyKind + ".");
        if (pieces.Count > mostRows)
            problems.Add("The list has " + pieces.Count + " rows, and one call builds at most " + mostRows
                + ". Split it into calls of up to " + mostRows + ".");
    }
    rowCount = pieces.Count;

    for (var r = 0; r < pieces.Count && pieces.Count <= mostRows; r++)
    {
        var where = "Row " + (r + 1);
        var problemsBefore = problems.Count;

        // FIELDS, each running until the next field's name. A name that is not
        // a field is refused rather than read as part of the value before it.
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var marks = new List<Tuple<int, int, string>>();
        foreach (System.Text.RegularExpressions.Match m in
            System.Text.RegularExpressions.Regex.Matches(pieces[r], @"\b([A-Za-z]+)\s*="))
        {
            var known = fieldNames.FirstOrDefault(n => string.Equals(n, m.Groups[1].Value,
                StringComparison.OrdinalIgnoreCase));
            if (known == null)
                problems.Add(where + ": \"" + m.Groups[1].Value + "\" is not a field. A row takes kind, workPlane, "
                    + "profile and solid; startMm and endMm for an extrusion, or axisLine, startDegrees and "
                    + "endDegrees for a revolve; and if wanted material, detailLevels and views.");
            else marks.Add(Tuple.Create(m.Index, m.Index + m.Length, known));
        }
        for (var k = 0; k < marks.Count; k++)
        {
            var end = k + 1 < marks.Count ? marks[k + 1].Item1 : pieces[r].Length;
            var value = pieces[r].Substring(marks[k].Item2, end - marks[k].Item2).Trim();
            if (fields.ContainsKey(marks[k].Item3))
                problems.Add(where + " gives " + marks[k].Item3 + " twice.");
            else fields[marks[k].Item3] = value;
        }
        Func<string, string> field = name => fields.ContainsKey(name) ? fields[name] : null;

        // VERSION 3: blend, sweep and swept blend as well - CREATE_FAMILY_BLEND's,
        // CREATE_FAMILY_SWEEP's and CREATE_FAMILY_SWEPT_BLEND's own kinds.
        var kindSaid = squash(field("kind"));
        var kindIs = kindSaid == "extrusion" ? "extrusion"
            : kindSaid == "revolve" || kindSaid == "revolution" ? "revolve"
            : kindSaid == "blend" ? "blend"
            : kindSaid == "sweep" ? "sweep"
            : kindSaid == "sweptblend" ? "swept blend" : null;
        if (kindIs == null)
        {
            problems.Add(where + ": kind is \"" + shortened(field("kind") ?? "") + "\" - a row here is an extrusion, "
                + "a revolve, a blend, a sweep or a swept blend.");
            continue;
        }

        var needed = kindFields[kindIs];
        var missing = needed.Where(n => string.IsNullOrEmpty(field(n))).ToList();
        if (missing.Count > 0)
            problems.Add(where + " (" + kindWords(kindIs) + ") has no " + string.Join(", ", missing) + ".");
        var stray = kindOwned.Where(n => !needed.Contains(n) && field(n) != null).ToList();
        if (stray.Count > 0)
            problems.Add(where + " is " + kindWords(kindIs) + " and gives " + string.Join(", ", stray)
                + " - another kind's fields. This kind takes " + string.Join(", ", needed) + ".");
        if (problems.Count > problemsBefore) continue;

        // THE PLANE, by its name, as the single-form tools find it.
        Tuple<Element, int, double, string> plane = null;
        var wanted = field("workPlane").Trim();
        var found = datums.Where(d => string.Equals(d.Item4, wanted, StringComparison.OrdinalIgnoreCase)).ToList();
        if (found.Count == 0)
            problems.Add(where + ": no reference plane or level is called \"" + shortened(wanted) + "\". This family has "
                + (datums.Count == 0 ? "no named ones."
                    : string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + "."));
        else if (found.Count > 1) problems.Add(where + ": \"" + wanted + "\" names " + found.Count
            + " planes - rename one first.");
        else if (found[0].Item2 < 0)
            problems.Add(where + ": \"" + wanted + "\" is at an angle. A " + (kindIs == "sweep"
                || kindIs == "swept blend" ? "path" : "profile") + " here is written in the model's own "
                + "coordinates, so the plane must face left-right, front-back or up.");
        else plane = found[0];

        // THE PROFILE, loops with `|` between - the single-form tools' own
        // reader. A blend's and a swept blend's profiles are ONE loop each, as
        // CREATE_FAMILY_BLEND and CREATE_FAMILY_SWEPT_BLEND took them (version 3).
        var loops = new List<List<double[]>>();
        var loopTexts = new List<string>();
        List<double[]> topLoop = null;
        List<double[]> route = null;
        Func<string, string, List<double[]>> oneLoop = (fieldName, words) =>
        {
            var text = field(fieldName) ?? "";
            if (text.Contains("|"))
            {
                problems.Add(where + ": the " + words + " has more than one loop. " + kindWords(kindIs)
                    + "'s profiles are one loop each" + (kindIs == "blend" ? ", with no holes." : "."));
                return null;
            }
            return loopOf(text, where + "'s " + words);
        };
        if (kindIs == "blend" || kindIs == "swept blend")
        {
            var firstName = kindIs == "blend" ? "baseProfile" : "bottomProfile";
            var firstLoop = oneLoop(firstName, kindIs == "blend" ? "base profile" : "bottom profile");
            topLoop = oneLoop("topProfile", "top profile");
            loopTexts.Add(field(firstName));
            if (firstLoop != null) loops.Add(firstLoop);
        }
        else
        {
            loopTexts = field("profile").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            if (loopTexts.Count == 0) problems.Add(where + " has no profile.");
            for (var i = 0; i < loopTexts.Count; i++)
            {
                var loop = loopOf(loopTexts[i], loopTexts.Count == 1 ? where + "'s profile"
                    : where + ", loop " + (i + 1) + " of the profile,");
                if (loop != null) loops.Add(loop);
            }
        }

        // THE PATH - CREATE_FAMILY_SWEEP's reader for a sweep; for a swept
        // blend ONE straight piece or ONE arc and nothing after it, Revit's own
        // rule, as CREATE_FAMILY_SWEPT_BLEND refused anything longer.
        if (kindIs == "sweep") route = pathOf(field("path"), where);
        else if (kindIs == "swept blend")
        {
            var pathSaid = System.Text.RegularExpressions.Regex.Replace(field("path").Trim(), @"\s*,\s*", ",");
            var steps = pathSaid.Split(';').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
            var start = steps.Count == 2 ? pair(steps[0]) : null;
            var bits = steps.Count == 2 ? steps[1].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[0];
            if (start != null && bits.Length == 1 && pair(bits[0]) != null)
            {
                var end = pair(bits[0]);
                if (Math.Sqrt(Math.Pow(end[0] - start[0], 2) + Math.Pow(end[1] - start[1], 2)) < shortest)
                    problems.Add(where + ": the path is shorter than Revit's shortest line, " + plain(shortest) + " mm.");
                else route = new List<double[]> { new[] { 0.0, start[0], start[1], 0, 0, end[0], end[1] } };
            }
            else if (start != null && bits.Length == 3 && bits[0].ToLowerInvariant() == "arc"
                && pair(bits[1]) != null && pair(bits[2]) != null)
            {
                var through = pair(bits[1]);
                var end = pair(bits[2]);
                var ux = through[0] - start[0]; var uy = through[1] - start[1];
                var vx = end[0] - start[0]; var vy = end[1] - start[1];
                var lu = Math.Sqrt(ux * ux + uy * uy);
                var lv = Math.Sqrt(vx * vx + vy * vy);
                var lw = Math.Sqrt(Math.Pow(end[0] - through[0], 2) + Math.Pow(end[1] - through[1], 2));
                if (lu < shortest || lv < shortest || lw < shortest || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
                    problems.Add(where + ": the path's arc is not an arc - its three points are in a line, or two are "
                        + "in the same place.");
                else route = new List<double[]> { new[] { 1.0, start[0], start[1], through[0], through[1], end[0], end[1] } };
            }
            else
                problems.Add(where + ": a swept blend's path is ONE piece - a straight \"X,Y; X,Y\" or an arc \"X,Y; arc "
                    + "MX,MY X,Y\". It reads \"" + shortened(pathSaid) + "\". A longer path is kind=sweep, which "
                    + "carries one profile.");
        }

        var solidSaid = squash(field("solid"));
        bool? isSolid = solidSaid == "true" || solidSaid == "yes" || solidSaid == "solid" ? true
            : solidSaid == "false" || solidSaid == "no" || solidSaid == "void" ? (bool?)false : null;
        if (!isSolid.HasValue)
            problems.Add(where + ": solid is \"" + shortened(field("solid")) + "\" - true for a solid, false for a void.");

        double first = 0, last = 0;
        double[] axisPoints = null;
        var snapped = 0;
        if (kindIs == "extrusion")
        {
            var a = number(field("startMm"));
            var b = number(field("endMm"));
            if (!a.HasValue) problems.Add(where + ": startMm \"" + shortened(field("startMm")) + "\" is not a number "
                + "of millimetres.");
            if (!b.HasValue) problems.Add(where + ": endMm \"" + shortened(field("endMm")) + "\" is not a number "
                + "of millimetres.");
            if (a.HasValue && b.HasValue)
            {
                first = a.Value;
                last = b.Value;
                // THE LEAST DEPTH IS REVIT'S SHORTEST LINE, said as
                // CREATE_FAMILY_EXTRUSION says it (5b-400).
                var depth = Math.Abs(last - first);
                if (depth < shortest)
                    problems.Add(where + ": the extrusion starts at " + plain(first) + " mm and ends at " + plain(last)
                        + " mm, " + plain(depth) + " mm deep - less than Revit's shortest line, so it cannot be "
                        + "built. Make it at least " + (Math.Ceiling(shortest * 100.0) / 100.0).ToString(invariant)
                        + " mm deep.");
            }
        }
        else if (kindIs == "blend")
        {
            // VERSION 3, CREATE_FAMILY_BLEND's rule: the top `topMm` from the
            // plane along the axis it faces, and a blend needs a height.
            var top = number(field("topMm"));
            if (!top.HasValue) problems.Add(where + ": topMm \"" + shortened(field("topMm")) + "\" is not a number "
                + "of millimetres.");
            else if (Math.Abs(top.Value) < shortest)
                problems.Add(where + ": the top profile is " + plain(top.Value) + " mm from the base - a blend needs a "
                    + "height.");
            else last = top.Value;
        }
        else if (kindIs == "revolve")
        {
            var a = number(field("startDegrees"));
            var b = number(field("endDegrees"));
            if (!a.HasValue) problems.Add(where + ": startDegrees \"" + shortened(field("startDegrees"))
                + "\" is not a number of degrees.");
            if (!b.HasValue) problems.Add(where + ": endDegrees \"" + shortened(field("endDegrees"))
                + "\" is not a number of degrees.");
            if (a.HasValue && b.HasValue)
            {
                first = a.Value;
                last = b.Value;
                if (last <= first)
                    problems.Add(where + ": the end angle, " + plain(last) + " degrees, is not past the start angle, "
                        + plain(first) + ". A full turn is 0 to 360.");
                else if (last - first > 360.0 + 1e-9)
                    problems.Add(where + ": from " + plain(first) + " to " + plain(last) + " degrees is more than "
                        + "one full turn.");
            }

            var ends = System.Text.RegularExpressions.Regex.Replace(field("axisLine").Trim(), @"\s*,\s*", ",")
                .Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var axisFrom = ends.Length == 2 ? pair(ends[0]) : null;
            var axisTo = ends.Length == 2 ? pair(ends[1]) : null;
            if (axisFrom == null || axisTo == null)
                problems.Add(where + ": the axis is not two points written \"X1,Y1 X2,Y2\" - \"0,0 0,1000\" is a "
                    + "vertical axis through the origin on a plane facing front or back. It reads \""
                    + shortened(field("axisLine")) + "\".");
            else if (Math.Sqrt(Math.Pow(axisTo[0] - axisFrom[0], 2) + Math.Pow(axisTo[1] - axisFrom[1], 2)) < shortest)
                problems.Add(where + ": the axis's two points are in the same place, so it has no direction.");
            else axisPoints = new[] { axisFrom[0], axisFrom[1], axisTo[0], axisTo[1] };

            // EVERY CORNER AND EVERY ARC'S MIDDLE POINT ON ONE SIDE OF THE AXIS,
            // a corner within the shortest curve of it put on it exactly first
            // (5b-318) - CREATE_FAMILY_REVOLUTION's own rule.
            if (axisPoints != null && loops.Count == loopTexts.Count && loops.Count > 0)
            {
                var dx = axisPoints[2] - axisPoints[0];
                var dy = axisPoints[3] - axisPoints[1];
                var length = Math.Sqrt(dx * dx + dy * dy);
                foreach (var loop in loops)
                    foreach (var s in loop)
                        foreach (var k in new[] { 1, 5 })
                        {
                            var side = (dx * (s[k + 1] - axisPoints[1]) - dy * (s[k] - axisPoints[0])) / length;
                            if (side == 0.0 || Math.Abs(side) > shortest) continue;
                            var t = (dx * (s[k] - axisPoints[0]) + dy * (s[k + 1] - axisPoints[1])) / (length * length);
                            s[k] = axisPoints[0] + dx * t;
                            s[k + 1] = axisPoints[1] + dy * t;
                            snapped++;
                        }
                if (snapped > 0)
                    foreach (var loop in loops)
                        foreach (var s in loop)
                            if (s[0] == 0.0 && Math.Sqrt(Math.Pow(s[5] - s[1], 2) + Math.Pow(s[6] - s[2], 2)) < shortest)
                            {
                                problems.Add(where + ": putting the corners that touch the axis exactly on it leaves "
                                    + "a side from " + plain(s[1]) + "," + plain(s[2]) + " to " + plain(s[5]) + ","
                                    + plain(s[6]) + " shorter than Revit's shortest line. Redraw the profile with "
                                    + "that corner off the axis.");
                                break;
                            }

                var left = false;
                var right = false;
                foreach (var loop in loops)
                    foreach (var s in loop)
                        foreach (var p in s[0] == 1.0
                            ? new[] { new[] { s[1], s[2] }, new[] { s[3], s[4] } } : new[] { new[] { s[1], s[2] } })
                        {
                            var side = (dx * (p[1] - axisPoints[1]) - dy * (p[0] - axisPoints[0])) / length;
                            if (side > shortest) left = true;
                            if (side < -shortest) right = true;
                        }
                if (left && right)
                    problems.Add(where + ": the profile crosses its axis. A revolve's profile lies wholly on one "
                        + "side of the axis - draw half the section, from the axis outwards.");
                if (!left && !right)
                    problems.Add(where + ": the profile lies on its axis, so turning it encloses nothing.");
            }
        }

        // A MATERIAL, IF ASKED: a family material parameter, a material in the
        // family, or "by category" - never guessed between them.
        FamilyParameter parameter = null;
        Material chosen = null;
        var byCategory = false;
        var materialSaid = (field("material") ?? "").Trim();
        if (field("material") != null)
        {
            if (isSolid.HasValue && !isSolid.Value)
                problems.Add(where + " is a void, and a void has no material to show.");
            else if (materialSaid.Length == 0)
                problems.Add(where + ": material is empty - name a family material parameter, a material in the "
                    + "family, or \"by category\".");
            else if (squash(materialSaid) == "bycategory" || squash(materialSaid) == "none")
                byCategory = true;
            else
            {
                FamilyParameter asParameter = null;
                foreach (FamilyParameter p in doc.FamilyManager.Parameters)
                    if (string.Equals(p.Definition.Name, materialSaid, StringComparison.OrdinalIgnoreCase))
                    {
                        asParameter = p;
                        break;
                    }
                var asMaterial = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>()
                    .FirstOrDefault(m => string.Equals(m.Name, materialSaid, StringComparison.OrdinalIgnoreCase));
                if (asParameter != null && asMaterial != null)
                    problems.Add(where + ": \"" + materialSaid + "\" is both a family parameter and a material in this "
                        + "family, so it does not say which is meant. Rename one of them.");
                else if (asParameter != null && !isMaterial(asParameter))
                    problems.Add(where + ": \"" + asParameter.Definition.Name + "\" is a family parameter, and not a "
                        + "MATERIAL one - Revit links a form's Material only to a material parameter.");
                else if (asParameter != null && asParameter.IsReporting)
                    problems.Add(where + ": \"" + asParameter.Definition.Name + "\" is a reporting parameter, and "
                        + "cannot drive a material.");
                else if (asParameter != null) parameter = asParameter;
                else if (asMaterial != null) chosen = asMaterial;
                else
                    problems.Add(where + ": \"" + shortened(materialSaid) + "\" is neither a family parameter nor a "
                        + "material in this family. Its materials include "
                        + string.Join(", ", new FilteredElementCollector(doc).OfClass(typeof(Material))
                            .Cast<Material>().Select(m => m.Name).OrderBy(n => n).Take(15)) + ".");
            }
        }

        // WHERE IT SHOWS, IF ASKED: detail levels and kinds of view, as
        // SET_FAMILY_FORM_VISIBILITY reads them. One given alone leaves the
        // other at all - what a new form shows in already.
        bool[] shows = null;
        if (field("detailLevels") != null || field("views") != null)
        {
            shows = new bool[6];
            var levelsSaid = field("detailLevels") ?? "all";
            var viewsSaid = field("views") ?? "all";
            foreach (var word in levelsSaid.Split(',').Select(squash).Where(w => w.Length > 0))
            {
                if (word == "all") { shows[0] = true; shows[1] = true; shows[2] = true; }
                else if (word == "coarse") shows[0] = true;
                else if (word == "medium") shows[1] = true;
                else if (word == "fine") shows[2] = true;
                else problems.Add(where + ": \"" + word + "\" is not a detail level. Name coarse, medium and fine, "
                    + "or all.");
            }
            if (!shows[0] && !shows[1] && !shows[2])
                problems.Add(where + ": no detail level was named. A form shown in none would vanish from every "
                    + "project view - name coarse, medium or fine, or all.");
            foreach (var word in viewsSaid.Split(',').Select(squash).Where(w => w.Length > 0))
            {
                if (word == "all") { shows[3] = true; shows[4] = true; shows[5] = true; }
                else if (word == "plan" || word == "planrcp" || word == "rcp" || word == "top" || word == "topbottom")
                    shows[3] = true;
                else if (word == "front" || word == "back" || word == "frontback") shows[4] = true;
                else if (word == "left" || word == "right" || word == "leftright" || word == "side") shows[5] = true;
                else problems.Add(where + ": \"" + word + "\" is not a kind of view. Name plan, front and left, or "
                    + "all.");
            }
            if (!shows[3] && !shows[4] && !shows[5])
                problems.Add(where + ": no kind of view was named. A form shown in none would vanish from plans and "
                    + "elevations - name plan, front or left, or all.");
        }

        if (problems.Count > problemsBefore) continue;

        rowKind.Add(kindIs);
        rowPlane.Add(plane);
        rowLoops.Add(loops);
        rowFrom.Add(first);
        rowTo.Add(last);
        rowAxis.Add(axisPoints);
        rowSolid.Add(isSolid.Value);
        rowParameter.Add(parameter);
        rowMaterial.Add(chosen);
        rowByCategory.Add(byCategory);
        rowShows.Add(shows);
        rowSnapped.Add(snapped);
        rowTopLoop.Add(topLoop);
        rowPath.Add(route);
    }

    if (problems.Count > 0)
        refused = (rowCount > 0 ? "Nothing was built - none of the " + rowCount + " row(s). " : "Nothing was built. ")
            + string.Join(" ", problems.Take(12))
            + (problems.Count > 12 ? " And " + (problems.Count - 12) + " more problem(s)." : "");
}

// ---- building, then reading back -------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;
    var made = new List<GenericForm>();
    var rowCurves = new List<List<Curve>>();
    // Each row's loops as points in the plane's two coordinates, to say which
    // lies inside which - CREATE_FAMILY_EXTRUSION's read-back (version 2).
    var rowRings = new List<List<List<double[]>>>();
    var rowTurn = new List<Line>();
    // VERSION 3: which way Revit took a blend's profiles and the ways it turned
    // down; a sweep's path length and profile area, a swept blend's length.
    var rowOrder = new List<string>();
    var rowTurnedDown = new List<List<string>>();
    var rowLength = new List<double>();
    var rowArea = new List<double>();
    var count = rowKind.Count;
    Func<int, string> whose = r => "Row " + (r + 1) + " (" + (rowSolid[r] ? "solid " : "void ") + rowKind[r] + " on \""
        + rowPlane[r].Item4 + "\")";
    var keptNothing = " NOTHING from this call was kept - none of the " + count + " form(s).";

    for (var r = 0; r < count; r++)
    {
        var plane = rowPlane[r];
        var axis = plane.Item2;
        var u = inPlane[axis][0];
        var v = inPlane[axis][1];
        var at = plane.Item3;
        Func<double, double, XYZ> onPlane = (one, two) =>
        {
            var xyz = new double[3];
            xyz[axis] = at;
            xyz[u] = one / 304.8;
            xyz[v] = two / 304.8;
            return new XYZ(xyz[0], xyz[1], xyz[2]);
        };

        var curves = new List<Curve>();
        var rings = new List<List<double[]>>();
        Line turnAbout = null;
        GenericForm form = null;
        string blendFailure = null;
        var blendOrder = "";
        var blendTurnedDown = new List<string>();
        var pathLength = 0.0;
        var profileArea = 0.0;
        try
        {
            // A FRESH SKETCH PLANE FOR EVERY FORM, made on the plane itself so
            // the form moves with it - one shared would be deleted with any form.
            var sketch = SketchPlane.Create(doc, plane.Item1.Id);
            var surface = sketch.GetPlane();
            if (axisOf(surface.Normal) != axis || Math.Abs(along(surface.Origin, axis) - at) > halfMillimetre)
                throw new InvalidOperationException("the sketch plane made on \"" + plane.Item4 + "\" does not lie on it");

            // VERSION 3: a profile drawn about its own origin, in its own XY -
            // a sweep's and a swept blend's, which Revit lays onto the path.
            Func<double, double, XYZ> flat = (one, two) => new XYZ(one / 304.8, two / 304.8, 0);
            Func<List<double[]>, CurveArray> flatArray = loop =>
            {
                var array = new CurveArray();
                foreach (var s in loop)
                {
                    var a = flat(s[1], s[2]);
                    var b = flat(s[5], s[6]);
                    array.Append(s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, flat(s[3], s[4])));
                }
                return array;
            };
            Func<List<double[]>, CurveArray> trailOf = steps =>
            {
                var trail = new CurveArray();
                foreach (var s in steps)
                {
                    var a = onPlane(s[1], s[2]);
                    var b = onPlane(s[5], s[6]);
                    Curve piece = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
                    trail.Append(piece);
                    pathLength += piece.Length;
                }
                return trail;
            };

            if (rowKind[r] == "sweep")
            {
                // CREATE_FAMILY_SWEEP's call, its profile at the path's start.
                var trail = trailOf(rowPath[r]);
                var shape = new CurveArrArray();
                var flatRings = new List<List<XYZ>>();
                var areas = new List<double>();
                foreach (var loop in rowLoops[r])
                {
                    var array = flatArray(loop);
                    shape.Append(array);
                    var ring = new List<XYZ>();
                    foreach (Curve piece in array)
                        for (var k = 0; k < 64; k++) ring.Add(piece.Evaluate(k / 64.0, true));
                    var twice = 0.0;
                    for (var k = 0; k < ring.Count; k++)
                        twice += ring[k].X * ring[(k + 1) % ring.Count].Y - ring[(k + 1) % ring.Count].X * ring[k].Y;
                    flatRings.Add(ring);
                    areas.Add(Math.Abs(twice) / 2);
                }
                // A LOOP IS A HOLE ONLY WHEN IT LIES INSIDE ANOTHER - inside an
                // odd number of the others, every point of it; never by its place
                // in the list (5b-276).
                Func<XYZ, List<XYZ>, bool> insideFlat = (point, ring) =>
                {
                    var odd = false;
                    for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                        if ((ring[i].Y > point.Y) != (ring[j].Y > point.Y)
                            && point.X < (ring[j].X - ring[i].X) * (point.Y - ring[i].Y) / (ring[j].Y - ring[i].Y) + ring[i].X)
                            odd = !odd;
                    return odd;
                };
                for (var i = 0; i < flatRings.Count; i++)
                {
                    var within = 0;
                    for (var j = 0; j < flatRings.Count; j++)
                        if (j != i && flatRings[i].All(point => insideFlat(point, flatRings[j]))) within++;
                    profileArea += (within % 2 == 1 ? -1 : 1) * areas[i];
                }
                var carried = doc.Application.Create.NewCurveLoopsProfile(shape);
                form = doc.FamilyCreate.NewSweep(rowSolid[r], trail, sketch, carried, 0, ProfilePlaneLocation.Start);
            }
            else if (rowKind[r] == "swept blend")
            {
                // CREATE_FAMILY_SWEPT_BLEND's call: one piece of path, the bottom
                // profile at its start and the top at its end.
                var trail = trailOf(rowPath[r]);
                Curve route = null;
                foreach (Curve piece in trail) route = piece;
                var bottomShape = new CurveArrArray();
                bottomShape.Append(flatArray(rowLoops[r][0]));
                var topShape = new CurveArrArray();
                topShape.Append(flatArray(rowTopLoop[r]));
                form = doc.FamilyCreate.NewSweptBlend(rowSolid[r], route, sketch,
                    doc.Application.Create.NewCurveLoopsProfile(bottomShape),
                    doc.Application.Create.NewCurveLoopsProfile(topShape));
            }
            else if (rowKind[r] == "blend")
            {
                // CREATE_FAMILY_BLEND's way. REVIT TAKES A BLEND'S DIRECTION FROM
                // THE WAY ITS LOOPS RUN, not from where the top lies (5b-369):
                // each way is BUILT AND READ BACK, a wrong one deleted before the
                // next, and its sketch plane goes with it, so each try makes its
                // own.
                var topFeet = rowTo[r] / 304.8;
                Func<double, double, double, XYZ> lifted = (one, two, offset) =>
                {
                    var xyz = new double[3];
                    xyz[axis] = at + offset;
                    xyz[u] = one / 304.8;
                    xyz[v] = two / 304.8;
                    return new XYZ(xyz[0], xyz[1], xyz[2]);
                };
                // REVERSED, each loop runs the other way round - the same corners,
                // every side's ends swapped and the sides in the opposite order.
                Func<List<double[]>, double, bool, CurveArray> arrayOf = (loop, offset, reversed) =>
                {
                    var array = new CurveArray();
                    foreach (var s in reversed ? Enumerable.Reverse(loop) : loop)
                    {
                        var a = lifted(s[1], s[2], offset);
                        var b = lifted(s[5], s[6], offset);
                        if (reversed) { var swap = a; a = b; b = swap; }
                        Curve piece = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, lifted(s[3], s[4], offset));
                        array.Append(piece);
                        curves.Add(piece);
                    }
                    return array;
                };
                // How far some points reach across the axis, {u low, v low, u high, v high}.
                Func<IEnumerable<XYZ>, double[]> reachOf = points =>
                {
                    var reach = new[] { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue };
                    foreach (var point in points)
                    {
                        reach[0] = Math.Min(reach[0], along(point, u));
                        reach[1] = Math.Min(reach[1], along(point, v));
                        reach[2] = Math.Max(reach[2], along(point, u));
                        reach[3] = Math.Max(reach[3], along(point, v));
                    }
                    return reach;
                };
                Func<CurveArray, double[]> reachOfLoop = array => reachOf(array.Cast<Curve>()
                    .SelectMany(c => Enumerable.Range(0, 257).Select(k => c.Evaluate(k / 256.0, true))));
                var low = at + Math.Min(0.0, topFeet);
                var high = at + Math.Max(0.0, topFeet);
                // WHERE A BLEND LANDED. Null when it runs from the plane to the top
                // with the base's own outline on the plane and the top's at the
                // top; otherwise what it reads. The ends are told apart by outline.
                Func<Blend, CurveArray, CurveArray, string> landing = (candidate, baseArray, topArray) =>
                {
                    var box = candidate.get_BoundingBox(null);
                    if (box == null) return "it has no extent to read back";
                    var says = "it reads " + axisLetters[axis] + " " + mm(along(box.Min, axis)) + " to "
                        + mm(along(box.Max, axis)) + " mm";
                    if (Math.Abs(along(box.Min, axis) - low) > halfMillimetre
                        || Math.Abs(along(box.Max, axis) - high) > halfMillimetre)
                        return says;
                    var ends = new List<PlanarFace>();
                    var shape = candidate.get_Geometry(new Options());
                    if (shape != null)
                        foreach (GeometryObject bit in shape)
                        {
                            var lump = bit as Solid;
                            if (lump == null) continue;
                            foreach (Face side in lump.Faces)
                            {
                                var flatFace = side as PlanarFace;
                                if (flatFace != null && axisOf(flatFace.FaceNormal) == axis) ends.Add(flatFace);
                            }
                        }
                    foreach (var end in new[] { Tuple.Create(at, baseArray, "base"), Tuple.Create(at + topFeet, topArray, "top") })
                    {
                        var face = ends.FirstOrDefault(f => Math.Abs(along(f.Origin, axis) - end.Item1) <= halfMillimetre);
                        if (face == null) return says + ", with no flat end at " + mm(end.Item1) + " mm";
                        var wantReach = reachOfLoop(end.Item2);
                        var gotReach = reachOf(face.EdgeLoops.Cast<EdgeArray>().SelectMany(l => l.Cast<Edge>())
                            .SelectMany(e => e.Tessellate()));
                        for (var i = 0; i < 2; i++)
                        {
                            var allowance = 1.0 / 304.8 + 0.005 * (wantReach[i + 2] - wantReach[i]);
                            if (Math.Abs(gotReach[i] - wantReach[i]) > allowance
                                || Math.Abs(gotReach[i + 2] - wantReach[i + 2]) > allowance)
                                return says + ", but its end at " + mm(end.Item1) + " mm is not the " + end.Item3
                                    + " profile's outline";
                        }
                    }
                    return null;
                };
                var tries = new[] { "base first", "base first, both loops reversed", "top first",
                    "top first, both loops reversed" };
                Blend kept = null;
                for (var t = 0; t < tries.Length && kept == null; t++)
                {
                    if (sketch == null || !sketch.IsValidObject)
                    {
                        sketch = SketchPlane.Create(doc, plane.Item1.Id);
                        var again = sketch.GetPlane();
                        if (axisOf(again.Normal) != axis || Math.Abs(along(again.Origin, axis) - at) > halfMillimetre)
                            throw new InvalidOperationException("the sketch plane made on \"" + plane.Item4
                                + "\" does not lie on it");
                    }
                    curves.Clear();
                    var bottom = arrayOf(rowLoops[r][0], 0.0, t % 2 == 1);
                    var topArray = arrayOf(rowTopLoop[r], topFeet, t % 2 == 1);
                    Blend tried;
                    try
                    {
                        tried = t < 2 ? doc.FamilyCreate.NewBlend(rowSolid[r], bottom, topArray, sketch)
                                      : doc.FamilyCreate.NewBlend(rowSolid[r], topArray, bottom, sketch);
                    }
                    catch (Exception refusal)
                    {
                        blendTurnedDown.Add(tries[t] + ": Revit refused it (" + refusal.Message + ")");
                        continue;
                    }
                    doc.Regenerate();
                    var wrong = landing(tried, bottom, topArray);
                    if (wrong == null)
                    {
                        kept = tried;
                        blendOrder = tries[t];
                    }
                    else
                    {
                        blendTurnedDown.Add(tries[t] + ": " + wrong);
                        doc.Delete(tried.Id);
                    }
                }
                if (kept == null)
                    blendFailure = whose(r) + " was asked to run from the plane at " + mm(at) + " mm to the top at "
                        + mm(at + topFeet) + " mm along " + axisLetters[axis] + ", and none of the four ways Revit was "
                        + "offered the profiles built that - " + string.Join("; ", blendTurnedDown) + "." + keptNothing;
                form = kept;
            }
            else
            {
            var outline = new CurveArrArray();
            foreach (var loop in rowLoops[r])
            {
                var array = new CurveArray();
                var ring = new List<double[]>();
                foreach (var s in loop)
                {
                    var a = onPlane(s[1], s[2]);
                    var b = onPlane(s[5], s[6]);
                    Curve piece = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
                    array.Append(piece);
                    curves.Add(piece);
                    for (var k = 0; k < 16; k++)
                    {
                        var point = piece.Evaluate(k / 16.0, true);
                        ring.Add(new[] { along(point, u), along(point, v) });
                    }
                }
                outline.Append(array);
                rings.Add(ring);
            }

            if (rowKind[r] == "extrusion")
            {
                // Along the sketch plane's own normal, turned by which way it
                // points, made to whichever end is not ON the plane and the other
                // set afterwards - CREATE_FAMILY_EXTRUSION's way.
                var sign = along(surface.Normal, axis) > 0 ? 1.0 : -1.0;
                var farMm = Math.Abs(rowTo[r]) >= Math.Abs(rowFrom[r]) ? rowTo[r] : rowFrom[r];
                var nearMm = Math.Abs(rowTo[r]) >= Math.Abs(rowFrom[r]) ? rowFrom[r] : rowTo[r];
                var extrusion = doc.FamilyCreate.NewExtrusion(rowSolid[r], outline, sketch, farMm / 304.8 * sign);
                if (Math.Abs(nearMm) > 1e-9) extrusion.StartOffset = nearMm / 304.8 * sign;
                form = extrusion;
            }
            else
            {
                var ax = rowAxis[r];
                turnAbout = Line.CreateBound(onPlane(ax[0], ax[1]), onPlane(ax[2], ax[3]));
                form = doc.FamilyCreate.NewRevolution(rowSolid[r], outline, sketch, turnAbout,
                    rowFrom[r] * Math.PI / 180.0, rowTo[r] * Math.PI / 180.0);
            }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(whose(r) + ": Revit refused it: " + ex.Message + keptNothing);
        }
        if (blendFailure != null) throw new InvalidOperationException(blendFailure);
        if (form == null) throw new InvalidOperationException(whose(r) + ": Revit made no form." + keptNothing);
        made.Add(form);
        rowCurves.Add(curves);
        rowRings.Add(rings);
        rowTurn.Add(turnAbout);
        rowOrder.Add(blendOrder);
        rowTurnedDown.Add(blendTurnedDown);
        rowLength.Add(pathLength);
        rowArea.Add(profileArea);
    }

    // ONE REGENERATION FOR THE WHOLE LIST, then every form read back.
    doc.Regenerate();

    var lines = new List<string>();
    for (var r = 0; r < count; r++)
    {
        var form = made[r];
        var axis = rowPlane[r].Item2;
        var u = inPlane[axis][0];
        var v = inPlane[axis][1];
        var at = rowPlane[r].Item3;
        var volume = 0.0;
        string reads;

        if (rowKind[r] == "extrusion")
        {
            var bounds = form.get_BoundingBox(null);
            if (bounds == null)
                throw new InvalidOperationException(whose(r) + " was built and has no extent to read back." + keptNothing);
            var low = Math.Min(rowFrom[r], rowTo[r]) / 304.8 + at;
            var high = Math.Max(rowFrom[r], rowTo[r]) / 304.8 + at;
            if (Math.Abs(along(bounds.Min, axis) - low) > halfMillimetre
                || Math.Abs(along(bounds.Max, axis) - high) > halfMillimetre)
                throw new InvalidOperationException(whose(r) + " reads " + axisLetters[axis] + " "
                    + mm(along(bounds.Min, axis)) + " to " + mm(along(bounds.Max, axis)) + " mm and was asked for "
                    + mm(low) + " to " + mm(high) + " mm." + keptNothing);

            // ACROSS THE PLANE it must cover what the profile covers, with the
            // allowance a curved face's tessellated extent needs.
            var spanMin = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
            var spanMax = new[] { double.MinValue, double.MinValue, double.MinValue };
            foreach (var c in rowCurves[r])
                for (var k = 0; k <= 256; k++)
                {
                    var p = c.Evaluate(k / 256.0, true);
                    foreach (var index in new[] { u, v })
                    {
                        spanMin[index] = Math.Min(spanMin[index], along(p, index));
                        spanMax[index] = Math.Max(spanMax[index], along(p, index));
                    }
                }
            foreach (var index in new[] { u, v })
            {
                var slack = 1.0 / 304.8 + 0.005 * (spanMax[index] - spanMin[index]);
                if (Math.Abs(along(bounds.Min, index) - spanMin[index]) > slack
                    || Math.Abs(along(bounds.Max, index) - spanMax[index]) > slack)
                    throw new InvalidOperationException(whose(r) + " reads " + axisLetters[index] + " "
                        + mm(along(bounds.Min, index)) + " to " + mm(along(bounds.Max, index)) + " mm and its "
                        + "profile covers " + mm(spanMin[index]) + " to " + mm(spanMax[index]) + " mm." + keptNothing);
            }

            var geometry = form.get_Geometry(new Options());
            if (geometry != null)
                foreach (GeometryObject piece in geometry)
                {
                    var body = piece as Solid;
                    if (body != null && body.Volume > 0) volume += body.Volume;
                }
            reads = string.Join(", ", new[] { 0, 1, 2 }.Select(i => axisLetters[i] + " " + mm(along(bounds.Min, i))
                + " to " + mm(along(bounds.Max, i)))) + " mm";
        }
        else if (rowKind[r] != "revolve")
        {
            // VERSION 3 - A BLEND, A SWEEP, A SWEPT BLEND, read back as their own
            // tools read them: an extent, and a solid's volume. A blend was read
            // along its axis as it was built; across the plane it must cover both
            // its profiles, with the allowance a curved face's tessellated extent
            // needs - CREATE_FAMILY_BLEND's check.
            var bounds = form.get_BoundingBox(null);
            if (bounds == null)
                throw new InvalidOperationException(whose(r) + " was built and has no extent to read back." + keptNothing);
            if (rowKind[r] == "blend")
            {
                var spanMin = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
                var spanMax = new[] { double.MinValue, double.MinValue, double.MinValue };
                foreach (var c in rowCurves[r])
                    for (var k = 0; k <= 256; k++)
                    {
                        var p = c.Evaluate(k / 256.0, true);
                        foreach (var index in new[] { u, v })
                        {
                            spanMin[index] = Math.Min(spanMin[index], along(p, index));
                            spanMax[index] = Math.Max(spanMax[index], along(p, index));
                        }
                    }
                foreach (var index in new[] { u, v })
                {
                    var slack = 1.0 / 304.8 + 0.005 * (spanMax[index] - spanMin[index]);
                    if (Math.Abs(along(bounds.Min, index) - spanMin[index]) > slack
                        || Math.Abs(along(bounds.Max, index) - spanMax[index]) > slack)
                        throw new InvalidOperationException(whose(r) + " reads " + axisLetters[index] + " "
                            + mm(along(bounds.Min, index)) + " to " + mm(along(bounds.Max, index)) + " mm and its two "
                            + "profiles cover " + mm(spanMin[index]) + " to " + mm(spanMax[index]) + " mm." + keptNothing);
                }
            }
            var geometry = form.get_Geometry(new Options());
            if (geometry != null)
                foreach (GeometryObject piece in geometry)
                {
                    var body = piece as Solid;
                    if (body != null && body.Volume > 0) volume += body.Volume;
                }
            reads = string.Join(", ", new[] { 0, 1, 2 }.Select(i => axisLetters[i] + " " + mm(along(bounds.Min, i))
                + " to " + mm(along(bounds.Max, i)))) + " mm";
        }
        else
        {
            // MEASURED ON THE SOLID ITSELF, never on the bounding box, which a
            // curved surface's tessellation reads short (5b-277) - every edge
            // walked and every face sampled inside its trim.
            var low = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
            var high = new[] { double.MinValue, double.MinValue, double.MinValue };
            var measured = false;
            Action<XYZ> take = p =>
            {
                for (var i = 0; i < 3; i++)
                {
                    low[i] = Math.Min(low[i], along(p, i));
                    high[i] = Math.Max(high[i], along(p, i));
                }
                measured = true;
            };
            var geometry = form.get_Geometry(new Options());
            if (geometry != null)
                foreach (GeometryObject piece in geometry)
                {
                    var body = piece as Solid;
                    if (body == null || body.Faces.Size == 0) continue;
                    if (body.Volume > 0) volume += body.Volume;
                    foreach (Edge edge in body.Edges)
                        for (var k = 0; k <= 256; k++)
                            take(edge.Evaluate(k / 256.0));
                    foreach (Face face in body.Faces)
                    {
                        var box = face.GetBoundingBox();
                        for (var i = 0; i <= 128; i++)
                            for (var j = 0; j <= 64; j++)
                            {
                                var uv = new UV(box.Min.U + (box.Max.U - box.Min.U) * i / 128.0,
                                    box.Min.V + (box.Max.V - box.Min.V) * j / 64.0);
                                if (face.IsInside(uv)) take(face.Evaluate(uv));
                            }
                    }
                }
            var bounds = form.get_BoundingBox(null);
            if (!measured && bounds == null)
                throw new InvalidOperationException(whose(r) + " was made with no solid in it, so it has no extent to "
                    + "read back - most often a profile that crosses its axis by a sliver." + keptNothing);
            Func<int, double> readLow = i => measured ? low[i] : along(bounds.Min, i);
            Func<int, double> readHigh = i => measured ? high[i] : along(bounds.Max, i);

            // A FULL TURN ABOUT AN AXIS LYING ALONG A MODEL AXIS has an extent
            // that can be worked out, and it must read it.
            var turnAbout = rowTurn[r];
            var direction = turnAbout.Direction;
            var spin = axisOf(direction);
            if (rowTo[r] - rowFrom[r] >= 360.0 - 1e-9 && spin >= 0)
            {
                var origin = turnAbout.GetEndPoint(0);
                double lo = double.MaxValue, hi = double.MinValue, reach = 0.0;
                foreach (var c in rowCurves[r])
                    for (var k = 0; k <= 256; k++)
                    {
                        var p = c.Evaluate(k / 256.0, true);
                        lo = Math.Min(lo, along(p, spin));
                        hi = Math.Max(hi, along(p, spin));
                        var off = p - origin;
                        reach = Math.Max(reach, (off - direction.Multiply(off.DotProduct(direction))).GetLength());
                    }
                var size = Math.Max(hi - lo, 2 * reach);
                var slack = measured ? halfMillimetre + 0.001 * size : 1.0 / 304.8 + 0.005 * size;
                var expected = new List<Tuple<int, double, double>> { Tuple.Create(spin, lo, hi) };
                foreach (var index in new[] { 0, 1, 2 }.Where(i => i != spin))
                    expected.Add(Tuple.Create(index, along(origin, index) - reach, along(origin, index) + reach));
                foreach (var e in expected)
                    if (Math.Abs(readLow(e.Item1) - e.Item2) > slack || Math.Abs(readHigh(e.Item1) - e.Item3) > slack)
                        throw new InvalidOperationException(whose(r) + " reads " + axisLetters[e.Item1] + " "
                            + mm(readLow(e.Item1)) + " to " + mm(readHigh(e.Item1)) + " mm, and a full turn of this "
                            + "profile about this axis covers " + mm(e.Item2) + " to " + mm(e.Item3) + " mm."
                            + keptNothing);
            }
            reads = string.Join(", ", new[] { 0, 1, 2 }.Select(i => axisLetters[i] + " " + mm(readLow(i)) + " to "
                + mm(readHigh(i)))) + " mm";
        }

        if (rowSolid[r] && volume <= 0)
            throw new InvalidOperationException(whose(r) + " was built and holds no solid to measure." + keptNothing);

        // ITS MATERIAL, set and read back.
        var materialWords = "";
        if (rowParameter[r] != null || rowMaterial[r] != null || rowByCategory[r])
        {
            var materialField = form.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            if (materialField == null)
                throw new InvalidOperationException(whose(r) + " has no Material field." + keptNothing);
            var expected = rowParameter[r] != null ? "linked to \"" + rowParameter[r].Definition.Name + "\""
                : rowByCategory[r] ? "<By Category>" : rowMaterial[r].Name;
            try
            {
                if (rowParameter[r] != null)
                {
                    if (!fm.CanElementParameterBeAssociated(materialField))
                        throw new InvalidOperationException("Revit says its Material cannot be linked");
                    fm.AssociateElementParameterToFamilyParameter(materialField, rowParameter[r]);
                }
                else materialField.Set(rowByCategory[r] ? ElementId.InvalidElementId : rowMaterial[r].Id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(whose(r) + ": Revit would not give it the material " + expected
                    + ": " + ex.Message + keptNothing);
            }
            var link = fm.GetAssociatedFamilyParameter(materialField);
            var nowId = materialField.AsElementId();
            var nowMaterial = nowId == null || nowId == ElementId.InvalidElementId ? null : doc.GetElement(nowId) as Material;
            var now = link != null ? "linked to \"" + link.Definition.Name + "\""
                : nowMaterial == null ? "<By Category>" : nowMaterial.Name;
            if (now != expected)
                throw new InvalidOperationException(whose(r) + " reads material " + now + " after being given "
                    + expected + "." + keptNothing);
            materialWords = ", material " + now;
        }

        // WHERE IT SHOWS, set and read back.
        var showWords = "";
        if (rowShows[r] != null)
        {
            var s = rowShows[r];
            try
            {
                var setting = new FamilyElementVisibility(FamilyElementVisibilityType.Model);
                setting.IsShownInCoarse = s[0];
                setting.IsShownInMedium = s[1];
                setting.IsShownInFine = s[2];
                setting.IsShownInTopBottom = s[3];
                setting.IsShownInFrontBack = s[4];
                setting.IsShownInLeftRight = s[5];
                form.SetVisibility(setting);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(whose(r) + ": Revit refused its visibility: " + ex.Message
                    + keptNothing);
            }
            var now = form.GetVisibility();
            if (now == null || now.IsShownInCoarse != s[0] || now.IsShownInMedium != s[1] || now.IsShownInFine != s[2]
                || now.IsShownInTopBottom != s[3] || now.IsShownInFrontBack != s[4] || now.IsShownInLeftRight != s[5])
                throw new InvalidOperationException(whose(r) + " does not read back the visibility asked for."
                    + keptNothing);
            showWords = ", shown in " + string.Join(", ", new[] { s[0] ? "coarse" : null, s[1] ? "medium" : null,
                s[2] ? "fine" : null }.Where(w => w != null)) + " | " + string.Join(", ", new[] { s[3] ? "plan" : null,
                s[4] ? "front/back" : null, s[5] ? "left/right" : null }.Where(w => w != null));
        }

        // WHAT THE SINGLE-FORM TOOLS SAID OF THE SHAPE (version 2). An
        // extrusion's loops by where they lie: inside an odd number of the
        // others a hole, inside none or an even number an outline of its own,
        // half in and half out named as crossing - never by its place in the
        // list (5b-275). A revolve's angles and its axis.
        var shapeWords = "";
        if (rowKind[r] == "extrusion")
        {
            var rings = rowRings[r];
            Func<double[], List<double[]>, bool> inside = (point, ring) =>
            {
                var odd = false;
                for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                    if ((ring[i][1] > point[1]) != (ring[j][1] > point[1])
                        && point[0] < (ring[j][0] - ring[i][0]) * (point[1] - ring[i][1])
                            / (ring[j][1] - ring[i][1]) + ring[i][0])
                        odd = !odd;
                return odd;
            };
            var outlines = 0;
            var holes = 0;
            var crossing = 0;
            for (var i = 0; i < rings.Count; i++)
            {
                var within = 0;
                var crosses = false;
                for (var j = 0; j < rings.Count; j++)
                {
                    if (j == i) continue;
                    var inCount = rings[i].Count(point => inside(point, rings[j]));
                    if (inCount == rings[i].Count) within++;
                    else if (inCount > 0) crosses = true;
                }
                if (crosses) crossing++;
                else if (within % 2 == 1) holes++;
                else outlines++;
            }
            var loopParts = new List<string>();
            if (outlines > 0) loopParts.Add(outlines + (outlines == 1 ? " outline" : holes == 0 ? " separate outlines" : " outlines"));
            if (holes > 0) loopParts.Add(holes + (holes == 1 ? " hole" : " holes") + " inside " + (outlines == 1 ? "it" : "them"));
            if (crossing > 0) loopParts.Add(crossing + " crossing another loop");
            shapeWords = ", " + rings.Count + " loop(s)" + (rings.Count > 1 ? " (" + string.Join(", ", loopParts) + ")" : "")
                + " from " + plain(rowFrom[r]) + " to " + plain(rowTo[r]) + " mm";
        }
        else if (rowKind[r] == "revolve")
        {
            var ax = rowAxis[r];
            shapeWords = ", " + rowLoops[r].Count + " loop(s) turned from " + plain(rowFrom[r]) + " to "
                + plain(rowTo[r]) + " degrees about the axis " + plain(ax[0]) + "," + plain(ax[1]) + " to "
                + plain(ax[2]) + "," + plain(ax[3]);
        }
        // VERSION 3: what CREATE_FAMILY_BLEND, CREATE_FAMILY_SWEEP and
        // CREATE_FAMILY_SWEPT_BLEND said of theirs.
        else if (rowKind[r] == "blend")
            shapeWords = ", the top profile " + plain(rowTo[r]) + " mm along " + axisLetters[axis] + ", the profiles "
                + "taken " + rowOrder[r];
        else if (rowKind[r] == "sweep")
            shapeWords = ", a path of " + rowPath[r].Count + " piece(s), " + plain(rowLength[r] * 304.8)
                + " mm long, carrying a profile of " + rowLoops[r].Count + " loop(s)";
        else
            shapeWords = ", " + (rowPath[r][0][0] == 0.0 ? "a straight path" : "an arc path") + " "
                + plain(rowLength[r] * 304.8) + " mm long";

        lines.Add((r + 1) + " " + (rowSolid[r] ? "solid " : "void ") + rowKind[r] + " on \"" + rowPlane[r].Item4
            + "\" (" + facing[axis] + ", " + axisLetters[axis] + " " + mm(at) + " mm)" + shapeWords + ": " + reads
            + (rowSolid[r] ? ", " + volumeWords(volume) : "") + materialWords + showWords);
    }

    formIds = string.Join(",", made.Select(f => f.UniqueId));
    formId = made.Count > 0 ? made[0].UniqueId : "";
    formCount = made.Count;
    built = string.Join(" || ", lines);

    var extrusions = rowKind.Count(k => k == "extrusion");
    var voids = rowSolid.Count(s => !s);
    // VERSION 3: every kind counted, never "the rest are revolves".
    var kindCounts = new[] { "extrusion", "revolve", "blend", "sweep", "swept blend" }
        .Select(k => Tuple.Create(k, rowKind.Count(x => x == k))).Where(t => t.Item2 > 0)
        .Select(t => t.Item2 + " " + t.Item1 + "(s)").ToList();
    findings.Add("Built " + formCount + " form(s) in this one call - " + string.Join(", ", kindCounts) + ", "
        + (formCount - voids) + " solid and " + voids + " void - every "
        + "one read back. ONE entry in the undo list takes them all back. Their ids, in the order listed, are in "
        + "formIds, commas between - name them so to the tools that lock, link, cut, colour or hide them.");
    for (var r = 0; r < count; r++)
    {
        if (rowKind[r] == "blend")
        {
            findings.Add("Row " + (r + 1) + ": Revit took the blend's profiles " + rowOrder[r] + ", and paired their "
                + "corners itself - a twist it chose is undone by hand with Edit Vertices.");
            if (rowTurnedDown[r].Count > 0)
                findings.Add("Row " + (r + 1) + ", before that, and deleted at once: " + string.Join("; ", rowTurnedDown[r])
                    + ". Revit decides which way a blend runs from the way its loops go round, not from where the top "
                    + "lies.");
        }
        else if (rowKind[r] == "sweep")
            findings.Add("Row " + (r + 1) + ", for comparison: the profile encloses "
                + Math.Round(rowArea[r] * 92903.04, 1).ToString(invariant) + " square millimetres, and a profile "
                + "centred on a path of this length sweeps about " + volumeWords(rowArea[r] * rowLength[r]) + ". An arc "
                + "in the path with the profile off-centre changes that. The profile was laid square to the path where "
                + "it starts, turned by Revit's own rule - LOOK at an asymmetric one. SET_FAMILY_SWEEP_PROFILE gives it "
                + "a loaded profile family by this form's id.");
        else if (rowKind[r] == "swept blend")
            findings.Add("Row " + (r + 1) + ": the bottom profile was laid square to the path's start and the top square "
                + "to its end, turned by Revit's own rule, with their corners paired by Revit - LOOK at an asymmetric "
                + "profile.");
    }
    var withMaterial = Enumerable.Range(0, count).Count(r => rowParameter[r] != null || rowMaterial[r] != null
        || rowByCategory[r]);
    var withShows = rowShows.Count(s => s != null);
    for (var r = 0; r < count; r++)
        if (rowKind[r] == "revolve" && rowTo[r] - rowFrom[r] < 360.0 - 1e-9)
            findings.Add("Row " + (r + 1) + " is a part-turn: which way it swept is Revit's rule for the plane's "
                + "normal - LOOK at it, and if it went the wrong way, build it again with the angles moved by 180 "
                + "degrees.");
    if (withMaterial > 0 || withShows > 0)
        findings.Add(withMaterial + " were given a material and " + withShows + " their visibility, each read back.");
    for (var r = 0; r < count; r++)
        if (rowSnapped[r] > 0)
            findings.Add("Row " + (r + 1) + ": " + rowSnapped[r] / 2 + " corner(s) lay within " + plain(shortest)
                + " mm of the axis and were put on it exactly - Revit builds no solid from a corner a hair across "
                + "its axis.");
    if (voids > 0)
        findings.Add("A void CUTS NOTHING by being made. In a family built through the API it cuts a solid only once "
            + "the two are combined - COMBINE_FAMILY_FORMS, with the void's id and the solid's.");
    findings.Add("Each sketch is hosted on the plane named and moves with it. Nothing else ties a form to the "
        + "family's planes or parameters yet - LOCK_FORM_TO_PLANES, LINK_FAMILY_PARAMETER and "
        + "LABEL_FAMILY_RADIUS do, by these ids.");
}

if (refused != null) findings.Add(refused);
