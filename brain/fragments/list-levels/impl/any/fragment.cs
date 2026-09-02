// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `elevationsMm` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// FEET TO MILLIMETRES BY ARITHMETIC (D-20). 1 ft = 304.8 mm exactly.
//
// TWO ELEVATIONS, AND WHICH ONE THE DRAWING SHOWS IS NOT THE OBVIOUS ONE.
//
//   Elevation         from the project's INTERNAL ORIGIN. The property the API
//                     offers first, and the one most code reaches for
//   ProjectElevation  from the project's ELEVATION BASE - the number Revit
//                     puts on the level head
//
// They are equal in a project whose base point has never been moved, which is
// most of them, which is exactly why the difference goes unnoticed until a
// project where it matters. Reporting only the first gives a list of heights
// that disagrees with the drawing and carries no sign that it does. So both are
// read, and the row SAYS SO whenever they differ.
//
// SORTED BY HEIGHT, NOT BY NAME. "Level 10" sorts before "Level 2" as text and
// sits above it in the building. A level list is read as a section through the
// building, and one in name order is read the same way and is wrong.

var elements = new List<Element>();
var findings = new List<string>();
var elevationsMm = new Dictionary<ElementId, double>();

var levels = new List<Level>();

foreach (var level in new FilteredElementCollector(doc)
                          .OfClass(typeof(Level))
                          .Cast<Level>())
{
    if (level != null) levels.Add(level);
}

levels.Sort(delegate (Level a, Level b) { return a.Elevation.CompareTo(b.Elevation); });

foreach (var level in levels)
{
    elements.Add(level);

    var internalMm = level.Elevation * 304.8;
    var projectMm = level.ProjectElevation * 304.8;

    // The number that matches the drawing is the one reported as THE
    // elevation; the internal one is added only when it differs, where it
    // explains why other tools report something else.
    elevationsMm[level.Id] = projectMm;

    // A tenth of a millimetre. Below that the two agree for every practical
    // purpose and printing both would put a meaningless caveat on every row.
    var differs = Math.Abs(internalMm - projectMm) > 0.1;

    findings.Add(string.Format(
        "{0}  {1:0.#} mm{2}",
        level.Name,
        projectMm,
        differs
            ? string.Format(" (the level head reads this; from the internal origin it is {0:0.#} mm - "
                            + "the project base point has been moved)", internalMm)
            : ""));
}
