// NOT STANDALONE. Assumes `doc`, `elevations` and `names` are in scope, and
// leaves `created`, `namedAs`, `elevationOf`, `nameRefused`, `alreadyThere`,
// `refused` and `noViewsMade` behind.
//
// VERSION 2 (2026-11-01) - CREATE_LEVEL MERGED IN, AND THE THREE RULES IT HAD
// THAT VERSION 1 LACKED:
//
//   A NAME ALREADY TAKEN IS REFUSED BEFORE ANYTHING IS CREATED. Every name asked
//   for is checked against the project's levels, and against the other names in
//   the same call, IGNORING CAPITALS - Revit's own rule, so "level 2" clashes
//   with "Level 2". One clash and NOTHING is created, and `refused` says which
//   name, at which height, clashed with which level. Version 1 created the level
//   first and named it second, so a clash left a real level in the model under a
//   name nobody asked for, reported in `nameRefused` afterwards. A level that is
//   never made needs no reporting.
//
//   A LEVEL REVIT WILL NOT MAKE IS NEVER DROPPED. Version 1 wrapped Level.Create
//   in a catch that swallowed the refusal and went on, so four asked for could
//   come back three with nothing said. Now a throw and a null - "Revit declined"
//   - are each recorded against the height asked for, the rest are still tried
//   so every refusal is named, and then the whole job THROWS: nothing is kept,
//   one request is one change, whole or not at all (Article 8).
//
//   NO VIEWS ARE MADE, AND `noViewsMade` SAYS SO. Revit's Level TOOL makes a
//   floor plan and a ceiling plan beside the level; Level.Create makes the
//   level alone. The modeller looks in the Project Browser, finds nothing and
//   concludes it failed. It did not. CREATE_PLAN_VIEW makes the plans.
//
//   NOT CARRIED: CREATE_LEVEL measured a shift between Elevation and
//   ProjectElevation off an existing level and took it off the height before
//   Level.Create. Which of the two is measured from where is FRAGMENT-ISSUES
//   row 5b-180, OPEN and waiting for a real Revit, so this card keeps its own
//   reading below - the height in millimetres straight to Level.Create, read
//   back as ProjectElevation - until that row is settled.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// THE NAME CLASH IS THE INTERESTING PART.
//
// A level is CREATED first and NAMED second, so a name Revit refuses leaves a
// real level in the model carrying whatever Revit called it - "Level 7" when
// "Level 3" was asked for. A count hides that completely: the number is right
// and the Project Browser is wrong. Since version 2 a name already taken is
// refused before anything is made (above); a name refused for another reason -
// a character Revit does not allow - still can only be found afterwards, so
// `namedAs` returns what each one is actually called and `nameRefused` names
// the ones that did not take.
//
// READ BACK AS ProjectElevation.
//
// That is the one in the same space as everything else in the model.
// `Elevation` is measured from whatever the level type's base says, so reading
// it back would confirm a number that was never in doubt and miss a survey
// offset entirely.
//
// AN ELEVATION THAT ALREADY HAS A LEVEL IS SKIPPED.
//
// Two levels at the same height is legal and almost never wanted - it makes
// every view range and every "which level is this on" answer ambiguous. It is
// also exactly the shape a RE-RUN of this job takes if nothing guards it.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
var askedMm = elevations == null ? new List<double>() : elevations.ToList();
elevations = askedMm.Select(v => v / MillimetresPerFoot).ToList();

var created = new List<ElementId>();
var namedAs = new Dictionary<ElementId, string>();
var elevationOf = new Dictionary<ElementId, double>();
var nameRefused = new List<ElementId>();
var alreadyThere = new List<double>();
string refused = null;
// Always true, and there to be said out loud (version 2, from CREATE_LEVEL).
var noViewsMade = true;

// Half a millimetre in feet. Two levels closer together than this are the same
// level with rounding on it, not two storeys.
const double SameHeight = 0.0005 / 0.3048;

var existing = new List<double>();
var existingNames = new List<string>();
foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
{
    if (level == null) continue;
    existing.Add(level.ProjectElevation);
    existingNames.Add(level.Name ?? "");
}

// FIRST, EVERYTHING IS DECIDED AND NOTHING IS MADE (version 2). Which heights
// already have a level, and whether any name asked for is taken - by a level in
// the project or by an earlier name in this same call - ignoring capitals.
var planned = new List<int>();
var plannedNames = new List<string>();
var clashes = new List<string>();

for (int i = 0; i < elevations.Count; i++)
{
    double wanted = elevations[i];

    bool taken = false;
    foreach (var height in existing)
    {
        if (Math.Abs(height - wanted) <= SameHeight) { taken = true; break; }
    }
    // A height asked for twice in one call is a level made once.
    foreach (var j in planned)
    {
        if (Math.Abs(elevations[j] - wanted) <= SameHeight) { taken = true; break; }
    }
    if (taken) { alreadyThere.Add(wanted); continue; }

    planned.Add(i);

    if (names == null || i >= names.Count || string.IsNullOrWhiteSpace(names[i])) continue;
    var name = names[i].Trim();

    string holder = null;
    foreach (var other in existingNames)
    {
        if (string.Equals(other, name, StringComparison.OrdinalIgnoreCase)) { holder = "level \"" + other + "\""; break; }
    }
    if (holder == null)
    {
        foreach (var other in plannedNames)
        {
            if (string.Equals(other, name, StringComparison.OrdinalIgnoreCase)) { holder = "another level in this same request"; break; }
        }
    }
    if (holder != null)
    {
        clashes.Add(string.Format("\"{0}\" at {1} mm is already the name of {2}",
            name, Math.Round(askedMm[i], 1), holder));
    }
    plannedNames.Add(name);
}

if (clashes.Count > 0)
{
    // REFUSED BEFORE ANYTHING IS CREATED (version 2, from CREATE_LEVEL).
    refused = string.Format(
        "{0} name(s) already taken, so NO LEVEL WAS CREATED: {1}. Revit refuses a duplicate "
        + "level name, ignoring capitals, and creating the level first and naming it after "
        + "leaves a level in the model under a name nobody asked for. Choose free names and "
        + "ask again.",
        clashes.Count, string.Join("; ", clashes));
}
else
{
    var declined = new List<string>();

    foreach (var i in planned)
    {
        double wanted = elevations[i];

        // A LEVEL REVIT WILL NOT MAKE IS RECORDED, NEVER DROPPED (version 2).
        // Version 1 swallowed the throw here and went on to the next height.
        Level level = null;
        try { level = Level.Create(doc, wanted); }
        catch (Exception failure)
        {
            declined.Add(string.Format("at {0} mm Revit refused: {1}",
                Math.Round(askedMm[i], 1), failure.Message));
            continue;
        }
        if (level == null)
        {
            declined.Add(string.Format("at {0} mm Revit declined to create the level",
                Math.Round(askedMm[i], 1)));
            continue;
        }

        created.Add(level.Id);
        existing.Add(wanted);

        // Read back the height rather than echoing what was asked for.
        try { elevationOf[level.Id] = level.ProjectElevation; } catch { }

        if (names == null || i >= names.Count || string.IsNullOrWhiteSpace(names[i]))
        {
            try { namedAs[level.Id] = level.Name; } catch { }
            continue;
        }

        var name = names[i].Trim();
        try { level.Name = name; } catch { }

        // Read the name back too. A name that passed the clash check can still
        // be refused for a character Revit does not allow in a name, and a
        // refused name does not throw on every release; either way the level is
        // already in the model under a different one.
        string actual = null;
        try { actual = level.Name; } catch { }
        if (actual != null) namedAs[level.Id] = actual;
        if (actual != name) nameRefused.Add(level.Id);
    }

    if (declined.Count > 0)
    {
        // ALL OR NOTHING (Article 8). Every height was tried, so every refusal
        // is named; the levels that were made go back with the rest.
        throw new InvalidOperationException(string.Format(
            "Revit would not create {0} of the {1} level(s) asked for, so NOTHING WAS KEPT - one "
            + "request is one change, whole or not at all (Article 8): {2}",
            declined.Count, planned.Count, string.Join("; ", declined)));
    }
}
