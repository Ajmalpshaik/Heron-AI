// NOT STANDALONE. Assumes `doc`, `baseLevel`, `topLevel` and `corners`;
// leaves `created`, `openingId`, `built`, `slabsCut`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// A SHAFT OPENING - Revit's Architecture > Opening > Shaft: one plan outline,
// drawn from a base level up to a top level, that cuts every floor, roof and
// ceiling between. `Document.Create.NewOpening(Level, Level, CurveArray)`.
// No host is named: that is what makes it a different tool from
// CREATE_OPENING, which cuts one host it is given.
//
// THE LEVELS ARE NEVER GUESSED (D-33). Each is a name matched exactly; a name
// no level has, or one two levels share, makes nothing and lists every level
// with its elevation. A top level that is not ABOVE the base is refused,
// naming both.
//
// THE OUTLINE IS THE CALLER'S: corners in plan, in millimetres (the add-in
// reads "0,0,0; 1200,0,0; 1200,800,0; 0,800,0" and hands them here in feet),
// at least three, closed back to the first. Their heights are not used - the
// outline is drawn on the base level. Two corners in one place, a side
// shorter than Revit's shortest line, an outline with no area and one that
// crosses itself are each refused by name before anything is made.
//
// READ BACK, ALL OR NOTHING. The shaft's outline extent is read back against
// the corners to a millimetre, and its Base Constraint and Top Constraint
// against the two levels; a miss THROWS and the host rolls the whole call
// back. Every floor, roof and ceiling whose box lies in the shaft's reach has
// its volume read before and after: the ones that lost volume are `slabsCut`.
// A shaft that took nothing out is SAID, not refused - a shaft drawn ahead of
// the slabs is a real thing to want - and CREATE_STAIRS measured one from
// Level 1 to Level 2 that removed nothing from either slab (2026-10-03,
// rolled back), so the findings tell the modeller to look in a section.

var created = ElementId.InvalidElementId;
var openingId = "";
var built = "";
var slabsCut = new List<ElementId>();
string refused = null;
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString(invariant);
var oneMillimetre = 1.0 / 304.8;
var problems = new List<string>();

if (doc.IsFamilyDocument)
{
    refused = "A shaft opening is cut in a project, between two of its levels - the family editor has no levels to "
        + "cut between. An opening in a family's own wall or ceiling is CREATE_OPENING's - nothing was cut";
    findings.Add(refused);
}
else
{
    // ---- THE TWO LEVELS, BY NAME ---------------------------------------------
    var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
        .OrderBy(l => l.ProjectElevation).ToList();
    var levelList = levels.Count == 0
        ? "This model has no levels."
        : "The levels here are " + string.Join(", ", levels.Select(l => "\"" + l.Name + "\" ("
            + mm(l.ProjectElevation) + " mm)")) + ".";
    Func<string, string, Level> levelNamed = (said, role) =>
    {
        var name = (said ?? "").Trim();
        if (name.Length == 0)
        {
            problems.Add("No " + role + " level was given.");
            return null;
        }
        var found = levels.Where(l => string.Equals(l.Name, name, StringComparison.Ordinal)).ToList();
        if (found.Count == 1) return found[0];
        problems.Add(found.Count == 0
            ? "No level is called \"" + name + "\" for the " + role + " - the name has to match the Project Browser "
              + "exactly."
            : found.Count + " levels are called \"" + name + "\", so which is the " + role + " is not clear.");
        return null;
    };
    var bottom = levelNamed(baseLevel, "base");
    var top = levelNamed(topLevel, "top");
    if (bottom != null && top != null && top.ProjectElevation <= bottom.ProjectElevation + oneMillimetre)
        problems.Add("The top level \"" + top.Name + "\" (" + mm(top.ProjectElevation) + " mm) is not above the base "
            + "level \"" + bottom.Name + "\" (" + mm(bottom.ProjectElevation) + " mm) - a shaft runs UP from its base.");
    var levelsMissing = problems.Count > 0;

    // ---- THE OUTLINE ---------------------------------------------------------
    var shortest = doc.Application.ShortCurveTolerance;
    var ring = new List<XYZ>();
    if (corners == null || corners.Count == 0)
        problems.Add("No corners were given. A shaft's outline is its corners in plan, in millimetres - "
            + "\"0,0,0; 1200,0,0; 1200,800,0; 0,800,0\".");
    else if (corners.Any(c => c == null))
        problems.Add("A corner is missing from the list.");
    else
    {
        foreach (var c in corners) ring.Add(new XYZ(c.X, c.Y, 0));
        // Closed back to the first: a last corner repeating the first is dropped.
        if (ring.Count > 1 && ring[0].DistanceTo(ring[ring.Count - 1]) < shortest) ring.RemoveAt(ring.Count - 1);
        if (ring.Count < 3)
            problems.Add("A shaft's outline needs at least three corners and " + ring.Count + " were given.");
        else
        {
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                if (a.DistanceTo(b) < shortest)
                {
                    problems.Add("Corners " + (i + 1) + " and " + ((i + 1) % ring.Count + 1) + " are "
                        + (a.DistanceTo(b) < 1e-9 ? "in the same place" : "closer than Revit's shortest line")
                        + " - " + mm(a.X) + "," + mm(a.Y) + " mm.");
                    break;
                }
            }
            var twiceArea = 0.0;
            for (var i = 0; i < ring.Count; i++)
            {
                var p = ring[i];
                var q = ring[(i + 1) % ring.Count];
                twiceArea += p.X * q.Y - q.X * p.Y;
            }
            if (problems.Count == 0 && Math.Abs(twiceArea) / 2 < shortest * shortest)
                problems.Add("The corners enclose no area - they lie on one line.");

            // An outline that crosses itself: two sides that are not neighbours meet.
            Func<XYZ, XYZ, XYZ, double> turn = (o, p, q) => (p.X - o.X) * (q.Y - o.Y) - (p.Y - o.Y) * (q.X - o.X);
            var n = ring.Count;
            for (var i = 0; i < n && problems.Count == 0; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    if (j == i + 1 || (i == 0 && j == n - 1)) continue;
                    XYZ a = ring[i], b = ring[(i + 1) % n], c = ring[j], d = ring[(j + 1) % n];
                    var d1 = turn(c, d, a); var d2 = turn(c, d, b);
                    var d3 = turn(a, b, c); var d4 = turn(a, b, d);
                    if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                    {
                        problems.Add("The outline crosses itself - the side from corner " + (i + 1) + " meets the "
                            + "side from corner " + (j + 1) + ". Give the corners in order round the shaft.");
                        break;
                    }
                }
            }
        }
    }

    if (problems.Count > 0)
    {
        refused = "Nothing was cut. " + string.Join(" ", problems) + (levelsMissing ? " " + levelList : "");
        findings.Add(refused);
    }
    else
    {
        // ---- WHAT IT REACHES, MEASURED BEFORE ------------------------------------
        var z = bottom.ProjectElevation;
        var xMin = ring.Min(p => p.X); var xMax = ring.Max(p => p.X);
        var yMin = ring.Min(p => p.Y); var yMax = ring.Max(p => p.Y);
        var hosts = new List<Element>();
        foreach (var category in new[] { BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs,
                                         BuiltInCategory.OST_Ceilings })
            hosts.AddRange(new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType());
        Func<Element, double?> volumeOf = element =>
        {
            var parameter = element.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
            return parameter == null || !parameter.HasValue ? (double?)null : parameter.AsDouble();
        };
        var reached = new List<Element>();
        var before = new Dictionary<ElementId, double>();
        foreach (var element in hosts)
        {
            var box = element.get_BoundingBox(null);
            if (box == null || box.Max.X < xMin || box.Min.X > xMax || box.Max.Y < yMin || box.Min.Y > yMax
                || box.Max.Z < z || box.Min.Z > top.ProjectElevation)
                continue;
            var volume = volumeOf(element);
            if (!volume.HasValue) continue;
            reached.Add(element);
            before[element.Id] = volume.Value;
        }

        var outline = new CurveArray();
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            outline.Append(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
        }

        Opening shaft = null;
        try { shaft = doc.Create.NewOpening(bottom, top, outline); }
        catch (Exception ex)
        {
            refused = "Revit refused the shaft: " + ex.Message + " - nothing was cut";
            findings.Add(refused);
        }
        if (refused == null && shaft == null)
        {
            refused = "Revit made no shaft - nothing was cut";
            findings.Add(refused);
        }

        if (refused == null)
        {
            doc.Regenerate();

            // ---- READ BACK, ALL OR NOTHING ---------------------------------------
            var read = new List<XYZ>();
            foreach (Curve curve in shaft.BoundaryCurves) read.AddRange(curve.Tessellate());
            if (read.Count == 0)
                throw new InvalidOperationException("The shaft has no outline to read back. The call failed, and Heron "
                    + "rolls the whole call back.");
            var rx0 = read.Min(p => p.X); var rx1 = read.Max(p => p.X);
            var ry0 = read.Min(p => p.Y); var ry1 = read.Max(p => p.Y);
            if (Math.Abs(rx0 - xMin) > oneMillimetre || Math.Abs(rx1 - xMax) > oneMillimetre
                || Math.Abs(ry0 - yMin) > oneMillimetre || Math.Abs(ry1 - yMax) > oneMillimetre)
                throw new InvalidOperationException("The shaft reads X " + mm(rx0) + " to " + mm(rx1) + ", Y " + mm(ry0)
                    + " to " + mm(ry1) + " mm, not where its corners are - X " + mm(xMin) + " to " + mm(xMax) + ", Y "
                    + mm(yMin) + " to " + mm(yMax) + " mm. The call failed, and Heron rolls the whole call back.");

            var baseRead = shaft.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            var topRead = shaft.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            if (baseRead != null && baseRead.AsElementId() != bottom.Id)
                throw new InvalidOperationException("The shaft's Base Constraint reads another level than \""
                    + bottom.Name + "\". The call failed, and Heron rolls the whole call back.");
            if (topRead != null && topRead.AsElementId() != top.Id)
                throw new InvalidOperationException("The shaft's Top Constraint reads another level than \""
                    + top.Name + "\". The call failed, and Heron rolls the whole call back.");

            created = shaft.Id;
            openingId = shaft.UniqueId;
            var twice = 0.0;
            for (var i = 0; i < ring.Count; i++)
                twice += ring[i].X * ring[(i + 1) % ring.Count].Y - ring[(i + 1) % ring.Count].X * ring[i].Y;
            var squareMetres = Math.Round(Math.Abs(twice) / 2 * 0.09290304, 2).ToString(invariant);
            built = "A shaft from \"" + bottom.Name + "\" (" + mm(bottom.ProjectElevation) + " mm) to \"" + top.Name
                + "\" (" + mm(top.ProjectElevation) + " mm), " + mm(top.ProjectElevation - bottom.ProjectElevation)
                + " mm tall; its outline " + ring.Count + " corners, X " + mm(rx0) + " to " + mm(rx1) + ", Y " + mm(ry0)
                + " to " + mm(ry1) + " mm - " + mm(rx1 - rx0) + " x " + mm(ry1 - ry0) + " mm, " + squareMetres
                + " m2 - read back.";
            findings.Add(built);
            if (baseRead == null || topRead == null)
                findings.Add("The shaft's Base Constraint and Top Constraint could not be read from its parameters, so "
                    + "its levels are the ones it was made with, not read back.");

            // ---- WHAT IT CUT, MEASURED AFTER --------------------------------------
            var untouched = 0;
            foreach (var element in reached)
            {
                var after = volumeOf(element);
                var removed = after.HasValue ? (before[element.Id] - after.Value) * 0.0283168466 : 0;
                if (removed > 0.001)
                {
                    slabsCut.Add(element.Id);
                    var level = doc.GetElement(element.LevelId) as Level;
                    findings.Add("Cut through " + (element.Category == null ? "an element" : element.Category.Name)
                        + " \"" + element.Name + "\"" + (level == null ? "" : " on \"" + level.Name + "\"") + " - "
                        + Math.Round(removed, 2).ToString(invariant) + " m3 taken out.");
                }
                else untouched++;
            }
            if (reached.Count == 0)
                findings.Add("No floor, roof or ceiling lies in the shaft's reach yet - it will cut the ones drawn there "
                    + "later.");
            else if (slabsCut.Count == 0)
                findings.Add("The shaft took nothing out of the " + reached.Count + " floor(s), roof(s) or ceiling(s) in "
                    + "its reach. Look at it in a section before relying on it - a slab whose top sits on the base "
                    + "level is under the shaft, not in it.");
            else if (untouched > 0)
                findings.Add(untouched + " more floor(s), roof(s) or ceiling(s) whose box lies in the shaft's reach "
                    + "lost no volume - their boxes overlap the shaft, their slabs may not.");
        }
    }
}
