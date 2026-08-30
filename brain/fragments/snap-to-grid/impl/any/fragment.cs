// NOT STANDALONE. Assumes `doc`, `elements`, `anchor`, `spacingX` and
// `spacingY` are in scope; leaves `snapped`, `notPointBased` and `blocked`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Distances are internal FEET.
//
// PLAN ONLY. Z IS NEVER TOUCHED, and that is a decision rather than a
// simplification. Putting a diffuser in the right place on the ceiling and
// putting it at the right height are two jobs, and a fragment that did both
// could not do either on its own. MOVE_TO_RAY_HIT is the other half.
//
// CELL CENTRES, NOT GRID LINES. A ceiling tile's centre sits half a tile from
// the intersection, which is where a diffuser goes. Snapping to the
// intersection instead puts every diffuser on a tile corner - a mistake that
// looks tidy in plan and is wrong in the ceiling.
//
// THE MOVE IS VERIFIED, for the same reason the plain move fragment is: Revit
// returns normally and moves nothing for a member of a group. Here the check is
// cheap - these are point-based elements, so the position is one read.

var snapped = 0;
var notPointBased = new List<ElementId>();
var blocked = new List<ElementId>();

var tolerance = 1.0 / 304.8;   // one millimetre, in feet

// A grid with no spacing has no cells to snap to. Refused rather than divided
// by: it would be a division by zero on every element.
if (spacingX > tolerance && spacingY > tolerance)
{
    foreach (var element in elements)
    {
        var placed = element.Location as LocationPoint;
        if (placed == null)
        {
            notPointBased.Add(element.Id);
            continue;
        }

        var at = placed.Point;

        // Which cell it is in, then that cell's centre. The half-cell is what
        // makes this a centre rather than a corner.
        var cellX = Math.Floor((at.X - anchor.X) / spacingX);
        var cellY = Math.Floor((at.Y - anchor.Y) / spacingY);

        var centreX = anchor.X + (cellX + 0.5) * spacingX;
        var centreY = anchor.Y + (cellY + 0.5) * spacingY;

        var shift = new XYZ(centreX - at.X, centreY - at.Y, 0);

        // Already on a centre. Counted as snapped rather than moved, because it
        // IS where it should be - and a zero move would otherwise be read as
        // blocked by the check below.
        if (shift.GetLength() < tolerance)
        {
            snapped++;
            continue;
        }

        ElementTransformUtils.MoveElement(doc, element.Id, shift);

        var now = element.Location as LocationPoint;
        if (now == null || now.Point.DistanceTo(at) < tolerance) blocked.Add(element.Id);
        else snapped++;
    }
}
