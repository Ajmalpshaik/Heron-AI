// NOT STANDALONE. Assumes `doc` and `ceiling` are in scope; leaves `spacingX`,
// `spacingY`, `anchor`, `anchorIsGuessed` and `found` behind.
//
// Spacings are in internal FEET.
//
// THE TILE SIZE IS IN THE MODEL. WHERE THE GRID STARTS IS NOT.
//
// That asymmetry is the whole reason this fragment exists on its own. The size
// can be read exactly, from the ceiling's own surface pattern. The origin
// cannot: Revit does not store it on any release this supports, which is why
// the interactive tools ask a person to click a grid intersection.
//
// So this returns a corner of the ceiling as a starting point and says
// `anchorIsGuessed`. Centres computed from it are evenly spaced and parallel to
// the real grid, and may sit half a tile out from what is drawn. Nothing throws.
// It lands on a neat, wrong grid - which is why the flag is an output and not a
// comment.
//
// THE PATTERN MUST BE A MODEL PATTERN. A drafting pattern's spacing is a PAPER
// distance: it means millimetres on a printed sheet, so it changes with view
// scale and is not a tile size at all. Reading one would give a number that
// moves when somebody changes the scale of a view.
//
// AN ABSURD NUMBER IS REJECTED RATHER THAN USED. A misread pattern does not
// give a plausible-but-wrong size, it gives a nonsense one - so anything under
// 15 mm or over 10 m is treated as "no grid found" instead of moving two
// hundred diffusers onto it.

var spacingX = 0.0;
var spacingY = 0.0;
var anchor = XYZ.Zero;
var anchorIsGuessed = true;
var found = false;

// Sanity bounds, in feet. A real ceiling tile is between these; a misread
// pattern is not.
var smallest = 15.0 / 304.8;
var largest = 10000.0 / 304.8;

FillPattern surfacePattern = null;

foreach (var materialId in ceiling.GetMaterialIds(false))
{
    var material = doc.GetElement(materialId) as Material;
    if (material == null) continue;

    var patternElement = doc.GetElement(material.SurfaceForegroundPatternId)
                         as FillPatternElement;
    if (patternElement == null) continue;

    var pattern = patternElement.GetFillPattern();
    if (pattern == null) continue;

    // Drafting patterns measure paper, not the building. See the note above.
    if (pattern.Target != FillPatternTarget.Model) continue;

    surfacePattern = pattern;
    break;
}

if (surfacePattern != null)
{
    var grids = surfacePattern.GetFillGrids();
    if (grids != null && grids.Count > 0)
    {
        var first = grids[0].Offset;
        // A one-directional pattern is stripes, not tiles. Treated as square
        // rather than refused: the spacing that IS known is still the spacing.
        var second = grids.Count > 1 ? grids[1].Offset : first;

        if (first >= smallest && first <= largest &&
            second >= smallest && second <= largest)
        {
            spacingX = first;
            spacingY = second;
            found = true;
        }
    }
}

if (found)
{
    // A corner of the ceiling, as somewhere to start counting from. Its bounding
    // box rather than its trimmed face: on an L-shaped ceiling the box reaches
    // into the missing corner, which would matter for asking WHICH elements sit
    // over this ceiling - but not here, because any corner is equally a guess
    // and is reported as one.
    var box = ceiling.get_BoundingBox(null);
    if (box != null) anchor = new XYZ(box.Min.X, box.Min.Y, box.Min.Z);
    anchorIsGuessed = true;
}
