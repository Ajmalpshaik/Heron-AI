// NOT STANDALONE. Assumes `doc`, `elevationMm` and `levelName` are in scope;
// leaves `created`, `refused` and `noViewsMade` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20). 1 ft = 304.8 mm exactly.
//
// THE HEIGHT IS ASKED FOR THE WAY THE LEVEL HEAD READS IT, and Level.Create
// takes the other one. Create() sets Elevation, measured from the INTERNAL
// ORIGIN; the number on the drawing is ProjectElevation, measured from the
// project's elevation base. They are equal until somebody moves the base point,
// so passing the requested height straight in is right in most projects and
// silently wrong in the ones that matter - the level lands where nobody asked
// and every drawing agrees with itself about the wrong number.
//
// So the offset between the two is measured off an EXISTING level and applied.
// With no level in the project there is nothing to measure against and no
// offset to apply, which is correct: a project with no levels has not had its
// base point moved relative to anything.
//
// A DUPLICATE NAME THROWS, so it is checked first - same reasoning as
// CREATE_SHEET, and the same failure if it is not: the level is created, the
// rename throws, and a level called "Level 12" is left in the model at the
// right height with the wrong name.
//
// AND IT MAKES NO VIEWS. Revit's Level TOOL creates a floor plan and a ceiling
// plan; the level itself does not, and the API is the level. The modeller looks
// in the Project Browser, finds nothing, and reports that it did not work. It
// did. `noViewsMade` is always true and exists to be said out loud.

var wantedMm = elevationMm;
var name = (levelName ?? "").Trim();

ElementId created = null;
string refused = null;
var noViewsMade = true;

// The offset between the two elevation systems, measured rather than assumed.
// Any existing level answers it: both properties are read off the same object,
// so their difference is the project's own base-point shift.
double baseShiftFt = 0;

foreach (var existing in new FilteredElementCollector(doc)
                             .OfClass(typeof(Level))
                             .Cast<Level>())
{
    if (existing == null) continue;
    baseShiftFt = existing.ProjectElevation - existing.Elevation;
    break;
}

string clash = null;

if (name.Length > 0)
{
    foreach (var existing in new FilteredElementCollector(doc)
                                 .OfClass(typeof(Level))
                                 .Cast<Level>())
    {
        if (existing == null) continue;
        if (string.Equals(existing.Name ?? "", name, StringComparison.OrdinalIgnoreCase))
        {
            clash = existing.Name;
            break;
        }
    }
}

if (clash != null)
{
    refused = string.Format(
        "a level called \"{0}\" already exists - Revit refuses a duplicate name, and "
        + "creating it first and renaming after leaves a wrongly-named level at the "
        + "right height", clash);
}
else
{
    // Requested height is in the drawing's terms; Create() wants the internal
    // one, so the measured shift comes back off it.
    var internalFt = (wantedMm / 304.8) - baseShiftFt;

    var level = Level.Create(doc, internalFt);

    if (level == null)
    {
        refused = "Revit declined to create the level";
    }
    else
    {
        // An empty name leaves Revit's own default, which is a real name and
        // beats writing blank over it.
        if (name.Length > 0) level.Name = name;

        created = level.Id;
    }
}
