// NOT STANDALONE. Assumes `doc`, `planes` and `axis` are in scope; leaves
// `created`, `alreadyThere`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE FAMILY'S OWN AXES: left is -X and right +X, front is -Y and back +Y. A
// plane that controls left-right is drawn in the floor plan along Y; one that
// controls front-back along X; a height plane in an elevation, along that
// view's own right direction - a horizontal plane cannot be drawn in plan.
//
// NewReferencePlane's THIRD ARGUMENT IS A DIRECTION, not a point. The normal
// comes out as (freeEnd - bubbleEnd) crossed with it. A point passed there made
// a visibly tilted plane in an earlier family build (reported), and nothing
// downstream notices a tilt - so every plane is read back here, axis and
// position, and one that does not read back true THROWS: the host rolls the
// whole call back and nothing is kept.
//
// A PLANE'S NAME IS READ FROM ITS NAME PARAMETER. `ReferencePlane.Name` falls
// back to Revit's own label for a plane nobody named, which would make every
// unnamed plane look named - reported from an earlier family build.
//
// THE LINES ARE NOT TRIMMED OR STRETCHED. Each is drawn across the origin and
// past the distances in the call; how far a plane's line runs is judged by eye
// in Revit, and is left to the modeller.

var findings = new List<string>();
var created = new List<string>();
var alreadyThere = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;

Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;

var axisWords = new[] { "left-right (X)", "front-back (Y)", "height (Z)" };
var axisLetters = new[] { "X", "Y", "Z" };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

var wantedAxis = -1;
var said = squash(axis);
if (said == "leftright" || said == "x" || said == "width") wantedAxis = 0;
else if (said == "frontback" || said == "y" || said == "depth") wantedAxis = 1;
else if (said == "height" || said == "z" || said == "updown" || said == "topbottom" || said == "elevation") wantedAxis = 2;

View view = null;
var toMake = new List<KeyValuePair<string, double>>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. Reference "
        + "planes for a family are made inside the family - open it first.";
}
else if (wantedAxis < 0)
{
    refused = "\"" + (axis ?? "") + "\" is not a way a plane can face here. It takes left-right, "
        + "front-back or height.";
}
else if (planes == null || planes.Count == 0)
{
    refused = "No planes were given - name=distance pairs in millimetres from the family origin, "
        + "semicolons between: \"Box Left=-300; Box Right=300\".";
}
else
{
    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate).ToList();

    view = wantedAxis == 2
        ? views.Where(v => v.ViewType == ViewType.Elevation)
               .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1)
               .FirstOrDefault()
        : views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);

    var existing = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane))
        .Cast<ReferencePlane>().ToList();

    // A LEVEL'S NAME IS TAKEN TOO. Every family fragment after this finds a
    // plane or a level by name, so a plane called "Ref. Level" would make that
    // name mean two things and every one of them would refuse it.
    var levelNames = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
        .Select(l => l.Name).ToList();

    var problems = new List<string>();

    // Two names that differ only in case are one name to Revit.
    var repeated = planes.Keys.GroupBy(k => (k ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
    if (repeated.Count > 0)
        problems.Add("Named twice: " + string.Join(", ", repeated) + ". A family cannot hold two "
            + "reference planes of one name.");

    if (view == null)
    {
        problems.Add(wantedAxis == 2
            ? "This family has no elevation view, and a height plane can only be drawn in one."
            : "This family has no floor plan, and a left-right or front-back plane is drawn in one.");
    }

    foreach (var pair in planes)
    {
        var name = pair.Key == null ? "" : pair.Key.Trim();
        var wanted = pair.Value / 304.8;

        if (levelNames.Any(l => string.Equals(l, name, StringComparison.OrdinalIgnoreCase)))
        {
            problems.Add("\"" + name + "\" is already the name of a level in this family. A plane of the "
                + "same name would make every later step unable to tell the two apart - give it another.");
            continue;
        }

        var same = existing.Where(rp => string.Equals(ownName(rp), name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (same.Count == 0) { toMake.Add(new KeyValuePair<string, double>(name, wanted)); continue; }

        var plane = same[0];
        var at = axisOf(plane.Normal);
        var where = at < 0 ? double.NaN : along(plane.GetPlane().Origin, at);

        if (same.Count == 1 && at == wantedAxis && Math.Abs(where - wanted) < 0.01 / 304.8)
        {
            alreadyThere.Add(name + " (" + axisWords[at] + ", at " + axisLetters[at] + " = " + mm(where)
                + " mm)");
        }
        else
        {
            problems.Add("\"" + name + "\" is already a reference plane in this family"
                + (at < 0 ? ", not lying along any axis"
                          : ", controlling " + axisWords[at] + " at " + axisLetters[at] + " = " + mm(where)
                            + " mm")
                + ". Moving it would drag whatever is locked to it, so give the new one another name "
                + "or move that one by hand.");
        }
    }

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems);
}

if (refused == null)
{
    // Drawn across the origin and past every distance in the call.
    var furthest = toMake.Count == 0 ? 0 : toMake.Max(p => Math.Abs(p.Value));
    var half = Math.Max(1000.0 / 304.8, furthest * 1.5);

    var level = view.GenLevel;
    var z0 = level == null ? 0.0 : level.Elevation;

    foreach (var pair in toMake)
    {
        var at = pair.Value;
        XYZ bubble, free, cut;

        if (wantedAxis == 0)
        {
            bubble = new XYZ(at, -half, z0); free = new XYZ(at, half, z0); cut = XYZ.BasisZ;
        }
        else if (wantedAxis == 1)
        {
            bubble = new XYZ(-half, at, z0); free = new XYZ(half, at, z0); cut = XYZ.BasisZ;
        }
        else
        {
            // Along the elevation's own right direction, cut toward the viewer:
            // their cross product is vertical, so the plane is horizontal.
            var right = view.RightDirection;
            bubble = new XYZ(-half * right.X, -half * right.Y, at);
            free = new XYZ(half * right.X, half * right.Y, at);
            cut = view.ViewDirection;
        }

        ReferencePlane made;
        try
        {
            made = doc.FamilyCreate.NewReferencePlane(bubble, free, cut, view);
            made.Name = pair.Key;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the reference plane \"" + pair.Key + "\": "
                + ex.Message + " NOTHING from this call was kept.");
        }

        // READ BACK - the axis and the position, before anything relies on them.
        var normal = made.Normal;
        var lies = axisOf(normal);
        var sits = lies < 0 ? double.NaN : along(made.GetPlane().Origin, lies);

        if (lies != wantedAxis || Math.Abs(sits - at) > 0.01 / 304.8)
            throw new InvalidOperationException("The plane \"" + pair.Key + "\" came out facing ("
                + Math.Round(normal.X, 3).ToString(invariant) + ", " + Math.Round(normal.Y, 3).ToString(invariant)
                + ", " + Math.Round(normal.Z, 3).ToString(invariant) + ")"
                + (lies < 0 ? "" : " at " + axisLetters[lies] + " = " + mm(sits) + " mm")
                + " - not the " + axisWords[wantedAxis] + " plane at " + mm(at) + " mm that was asked "
                + "for. NOTHING from this call was kept.");

        created.Add(pair.Key + " (" + axisWords[lies] + ", at " + axisLetters[lies] + " = " + mm(sits) + " mm)");
    }

    if (created.Count > 0)
        findings.Add("Made " + created.Count + " reference plane(s) in \"" + view.Name + "\", each read "
            + "back: " + string.Join("; ", created) + ". Is Reference is Revit's default - set it in "
            + "Properties where a project must dimension to a plane.");

    if (alreadyThere.Count > 0)
        findings.Add(alreadyThere.Count + " were already in the family where they were asked for, and "
            + "were left alone: " + string.Join("; ", alreadyThere) + ".");

    findings.Add("Next: LABEL_FAMILY_DIMENSION ties the distance between two planes to a parameter, "
        + "once that parameter holds the distance they are apart now.");
}

if (refused != null) findings.Add(refused);
