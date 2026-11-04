// NOT STANDALONE. Assumes `doc`, `level`, `trussType` and `curves` are in
// scope; leaves `created`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, and
// the caller owns it. Everything is made inside one SubTransaction of that
// transaction, and each truss inside a SubTransaction of its own within it, so
// a truss Revit refuses - or one that reads back wrong - is put back alone,
// and when NO truss is made the sketch plane made for them is put back too.
// Lengths are internal FEET; the lines arrive in feet, converted from
// millimetres at the edge by the add-in (D-20), and are said back in mm.
//
// REVIT'S STRUCTURE > TRUSS. Truss.Create(doc, trussTypeId, sketchPlaneId,
// curve) - RevitAPI.xml, the same words 2020 and 2027: the curve "must be a
// line, must not be a vertical line, and must be within the sketch plane".
// So each line is checked here, before Revit is asked, and refused BY NAME:
// not straight, vertical, of no length, or rising from one end to the other.
//
// THE LINE IS LAID ON THE LEVEL'S PROJECT ELEVATION, as CREATE_WALL lays a
// wall: the heights typed at its ends are not used, only that they agree. On
// a model set out to a survey datum `Elevation` and `ProjectElevation` differ,
// and the wrong one puts the line off the sketch plane with no clear error.
// The sketch plane is made from the level (SketchPlane.Create(doc, levelId))
// and its plane is READ: one that is not flat at that height stops the run
// before any truss is tried, with both heights named.
//
// THE TYPE IS NEVER GUESSED (D-33). `trussType` names a truss type by its name
// or as "Family: Type". Left out it arrives as "list": nothing is made and the
// model's truss types are named - even when there is only one. "Howe" is not
// a type name unless the model has a type called that; the list says what is.
//
// TrussType CANNOT BE FOUND WITH OfClass - RevitAPI.xml lists it among the
// classes ElementClassFilter does not support. The types are collected as
// family types in Structural Trusses (OST_Truss) and cast.
//
// ONE TRUSS PER LINE. A line on which a truss already stands - either way
// round - or a line given twice in one call is skipped and named, never
// doubled.
//
// WHAT IS REPORTED IS READ BACK, before each truss is kept: after a
// regenerate it is still in the model, of the type asked, and its length is
// read off its own location line.

var created = new List<ElementId>();
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was made.
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot, 1).ToString(invariant);
Func<XYZ, string> at = p => mm(p.X) + "," + mm(p.Y);

var shortest = doc.Application.ShortCurveTolerance;

var lines = new List<string>();
var skipped = new List<string>();
var problems = new List<string>();

// ---- the type ----------------------------------------------------------------

Func<Element, string> typeLabel = one =>
{
    string name = "", family = "";
    try { name = one.Name ?? ""; } catch (Exception) { }
    try { family = ((ElementType)one).FamilyName ?? ""; } catch (Exception) { }
    return family.Length > 0 ? family + ": " + name : name;
};

var said = (trussType ?? "").Trim();
var listOnly = said.Length == 0 || string.Equals(said, "list", StringComparison.OrdinalIgnoreCase);

var allTypes = new List<TrussType>();
foreach (var element in new FilteredElementCollector(doc)
             .OfClass(typeof(FamilySymbol))
             .OfCategory(BuiltInCategory.OST_Truss))
{
    var one = element as TrussType;
    if (one != null) allTypes.Add(one);
}

var typeNames = new List<string>();
foreach (var one in allTypes) typeNames.Add("\"" + typeLabel(one) + "\"");
var typeChoices = typeNames.Count == 0
    ? "this model has no truss type loaded - load a truss family first (Insert > Load Family, "
      + "Structural > Trusses)"
    : "the model's truss types are " + string.Join(", ", typeNames.ToArray());

TrussType useType = null;
string problem = null;

if (level == null)
{
    problem = "No level was given, so no truss was made - name the level the trusses sit on";
}
else if (listOnly)
{
    problem = "No truss type was named, so nothing was made - " + typeChoices
        + ". Say which one to place";
}
else
{
    var matches = new List<TrussType>();
    foreach (var one in allTypes)
    {
        string name = "", family = "";
        try { name = one.Name ?? ""; } catch (Exception) { }
        try { family = one.FamilyName ?? ""; } catch (Exception) { }
        if (string.Equals(name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + ": " + name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + " : " + name, said, StringComparison.OrdinalIgnoreCase))
            matches.Add(one);
    }
    if (matches.Count == 1) useType = matches[0];
    else if (matches.Count == 0)
        problem = string.Format("No truss type called \"{0}\" - {1}", said, typeChoices);
    else
        problem = string.Format("{0} truss types are called \"{1}\" - name it as \"Family: Type\"; {2}",
            matches.Count, said, typeChoices);
}

if (problem == null && (curves == null || curves.Count == 0))
    problem = "No lines were given, so there is nothing to place a truss along";

// ---- the trusses already in the model -------------------------------------------

// Each existing truss's location line, to skip a line that already has one.
var standing = new List<Line>();
var standingIds = new List<ElementId>();
if (problem == null)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Truss)))
    {
        var located = element.Location as LocationCurve;
        var existing = located == null ? null : located.Curve as Line;
        if (existing == null) continue;
        standing.Add(existing);
        standingIds.Add(element.Id);
    }
}

Func<XYZ, XYZ, Line, bool> sameLine = (a, b, other) =>
{
    var p = other.GetEndPoint(0);
    var q = other.GetEndPoint(1);
    return (a.DistanceTo(p) <= shortest && b.DistanceTo(q) <= shortest)
        || (a.DistanceTo(q) <= shortest && b.DistanceTo(p) <= shortest);
};

// ---- the lines, checked before Revit is asked -----------------------------------

var z = level == null ? 0.0 : level.ProjectElevation;   // NOT level.Elevation - see the header
var bases = new List<Line>();
var baseNumbers = new List<int>();

for (var i = 0; problem == null && i < curves.Count; i++)
{
    var number = i + 1;
    var curve = curves[i];
    if (curve == null)
    {
        skipped.Add(string.Format("Line {0}: missing - nothing placed along it", number));
        continue;
    }
    var line = curve as Line;
    if (line == null || !curve.IsBound)
    {
        skipped.Add(string.Format("Line {0}: not a straight line between two ends - a truss is placed "
            + "only along a straight line", number));
        continue;
    }

    var a = line.GetEndPoint(0);
    var b = line.GetEndPoint(1);
    var across = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
    var rise = Math.Abs(b.Z - a.Z);
    var where = string.Format("from {0} to {1}", at(a), at(b));

    if (across <= shortest && rise > shortest)
    {
        skipped.Add(string.Format("Line {0} ({1}): VERTICAL - its ends are at one point in plan and {2} mm "
            + "apart in height, and Revit places a truss only along a line that is not vertical", number,
            where, mm(rise)));
        continue;
    }
    if (across <= shortest)
    {
        skipped.Add(string.Format("Line {0} ({1}): has NO LENGTH - its two ends are in the same place", number,
            where));
        continue;
    }
    if (rise > shortest)
    {
        skipped.Add(string.Format("Line {0} ({1}): rises {2} mm from one end to the other - a truss is laid "
            + "flat on its level. Give both ends the same height", number, where, mm(rise)));
        continue;
    }

    var start = new XYZ(a.X, a.Y, z);
    var end = new XYZ(b.X, b.Y, z);

    var twice = -1;
    for (var k = 0; k < bases.Count; k++)
        if (sameLine(start, end, bases[k])) { twice = baseNumbers[k]; break; }
    if (twice > 0)
    {
        skipped.Add(string.Format("Line {0} ({1}): the same line as line {2} - one truss per line, not two",
            number, where, twice));
        continue;
    }
    var already = -1;
    for (var k = 0; k < standing.Count; k++)
        if (sameLine(start, end, standing[k])) { already = k; break; }
    if (already >= 0)
    {
        skipped.Add(string.Format("Line {0} ({1}): a truss already stands on this line (id {2}) - left as it "
            + "was, not doubled", number, where, standingIds[already]));
        continue;
    }

    bases.Add(Line.CreateBound(start, end));
    baseNumbers.Add(number);
}

// ---- the trusses ------------------------------------------------------------------

if (problem == null && bases.Count > 0)
{
    using (var whole = new SubTransaction(doc))
    {
        try
        {
            whole.Start();

            // AN UNACTIVATED TYPE PLACES NOTHING USEFUL - activated here, inside
            // this SubTransaction, so a run that makes nothing leaves it as it was.
            if (!useType.IsActive)
            {
                useType.Activate();
                doc.Regenerate();
            }

            var sketch = SketchPlane.Create(doc, level.Id);
            Plane plane = null;
            try { plane = sketch.GetPlane(); } catch (Exception) { plane = null; }
            if (plane == null || Math.Abs(plane.Normal.Z) < 0.999999 || Math.Abs(plane.Origin.Z - z) > shortest)
            {
                problem = string.Format("Revit's sketch plane for \"{0}\" is {1}, while the level's project "
                    + "elevation is {2} mm - the lines would not lie in it, so no truss was tried",
                    level.Name,
                    plane == null ? "not readable"
                        : Math.Abs(plane.Normal.Z) < 0.999999 ? "not flat"
                        : "at " + mm(plane.Origin.Z) + " mm",
                    mm(z));
            }

            for (var k = 0; problem == null && k < bases.Count; k++)
            {
                var baseLine = bases[k];
                var where = string.Format("Line {0} (from {1} to {2})", baseNumbers[k],
                    at(baseLine.GetEndPoint(0)), at(baseLine.GetEndPoint(1)));
                using (var attempt = new SubTransaction(doc))
                {
                    try
                    {
                        attempt.Start();
                        var truss = Truss.Create(doc, useType.Id, sketch.Id, baseLine);
                        if (truss == null)
                        {
                            attempt.RollBack();
                            problems.Add(where + ": NOT made - Revit returned no truss");
                            continue;
                        }

                        // Read back BEFORE it is kept.
                        doc.Regenerate();
                        var now = doc.GetElement(truss.Id) as Truss;
                        if (now == null || !now.IsValidObject)
                        {
                            attempt.RollBack();
                            problems.Add(where + ": Revit took the call but the truss is NOT in the model afterwards");
                            continue;
                        }
                        if (now.GetTypeId() != useType.Id)
                        {
                            attempt.RollBack();
                            problems.Add(string.Format("{0}: Revit made the truss of another type than \"{1}\", so "
                                + "it was taken back", where, typeLabel(useType)));
                            continue;
                        }

                        var located = now.Location as LocationCurve;
                        var length = located != null && located.Curve != null
                            ? mm(located.Curve.Length) + " mm long"
                            : mm(baseLine.Length) + " mm long (the line given - Revit gave no location line)";

                        var onLevel = "on \"" + level.Name + "\"";
                        ElementId levelNow = null;
                        try { levelNow = now.LevelId; } catch (Exception) { levelNow = null; }
                        if (levelNow != null && levelNow != ElementId.InvalidElementId && levelNow != level.Id)
                        {
                            var other = doc.GetElement(levelNow);
                            onLevel = string.Format("on the sketch plane of \"{0}\", though Revit gives it the level "
                                + "\"{1}\"", level.Name, other == null ? "?" : other.Name);
                        }

                        var members = 0;
                        try { members = now.Members == null ? 0 : now.Members.Count; } catch (Exception) { members = 0; }

                        attempt.Commit();
                        created.Add(now.Id);
                        lines.Add(string.Format("{0}: truss made ({1}), type \"{2}\", {3}, {4}, {5} member(s)", where,
                            now.Id, typeLabel(useType), length, onLevel, members));
                    }
                    catch (Exception ex)
                    {
                        try { attempt.RollBack(); } catch (Exception) { }
                        problems.Add(where + ": NOT made - Revit refused: " + ex.Message);
                    }
                }
            }

            // Nothing made: the sketch plane and the activation go back too.
            if (created.Count == 0) whole.RollBack();
            else whole.Commit();
        }
        catch (Exception ex)
        {
            try { whole.RollBack(); } catch (Exception) { }
            created.Clear();
            lines.Clear();
            problem = "Revit refused before any truss was made: " + ex.Message;
        }
    }
}

// ---- the answer -----------------------------------------------------------------

var all = new List<string>(lines);
all.AddRange(skipped);
all.AddRange(problems);
report = string.Join("\n", all.ToArray());

if (problem == null)
    findings.Add(string.Format("{0} truss(es) made, {1} line(s) skipped, {2} line(s) Revit would not take, of "
        + "{3} line(s) given", created.Count, skipped.Count, problems.Count, curves == null ? 0 : curves.Count));
foreach (var one in skipped) findings.Add(one);
foreach (var one in problems) findings.Add(one);

if (created.Count > 0)
    findings.Add(string.Format("Each truss is laid on \"{0}\" at its project elevation, {1} mm - the heights typed "
        + "at the line ends are not used. Nothing is attached to a roof or a floor here, and its chords are "
        + "the truss type's own", level.Name, mm(z)));

if (created.Count == 0)
{
    if (problem != null)
        refused = problem;
    else if (problems.Count == 0)
        refused = string.Format("None of the {0} line(s) could take a truss - the report names each and why",
            curves.Count);
    else
        refused = string.Format("No truss was made: Revit would not take {0} line(s) and {1} were skipped - the "
            + "report names each and why", problems.Count, skipped.Count);
    refused = refused.TrimEnd('.') + ". Nothing was created.";
    findings.Add(refused);
}
