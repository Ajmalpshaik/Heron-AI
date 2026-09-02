// NOT STANDALONE. Assumes `doc`, `elevations` and `names` are in scope, and
// leaves `created`, `namedAs`, `elevationOf`, `nameRefused` and `alreadyThere`
// behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// THE NAME CLASH IS THE INTERESTING PART.
//
// A level is CREATED first and NAMED second, so a name Revit refuses leaves a
// real level in the model carrying whatever Revit called it - "Level 7" when
// "Level 3" was asked for. A count hides that completely: the number is right
// and the Project Browser is wrong. `namedAs` returns what each one is actually
// called.
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

var created = new List<ElementId>();
var namedAs = new Dictionary<ElementId, string>();
var elevationOf = new Dictionary<ElementId, double>();
var nameRefused = new List<ElementId>();
var alreadyThere = new List<double>();

// Half a millimetre in feet. Two levels closer together than this are the same
// level with rounding on it, not two storeys.
const double SameHeight = 0.0005 / 0.3048;

var existing = new List<double>();
foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
{
    existing.Add(level.ProjectElevation);
}

for (int i = 0; i < elevations.Count; i++)
{
    double wanted = elevations[i];

    bool taken = false;
    foreach (var height in existing)
    {
        if (Math.Abs(height - wanted) <= SameHeight) { taken = true; break; }
    }
    if (taken) { alreadyThere.Add(wanted); continue; }

    Level level = null;
    try { level = Level.Create(doc, wanted); } catch { }
    if (level == null) continue;

    created.Add(level.Id);
    existing.Add(wanted);

    // Read back the height rather than echoing what was asked for.
    try { elevationOf[level.Id] = level.ProjectElevation; } catch { }

    if (names == null || i >= names.Count || string.IsNullOrWhiteSpace(names[i]))
    {
        try { namedAs[level.Id] = level.Name; } catch { }
        continue;
    }

    try { level.Name = names[i]; } catch { }

    // Read the name back too. A refused name does not throw on every release,
    // and either way the level is already in the model under a different one.
    string actual = null;
    try { actual = level.Name; } catch { }
    if (actual != null) namedAs[level.Id] = actual;
    if (actual != names[i]) nameRefused.Add(level.Id);
}
