// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `deleted`,
// `askedFor`, `alsoWent`, `refused`, `attachedBefore`, `detachedWalls` and
// `wallsNote` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE RETURNED SET IS THE ANSWER, NOT THE INPUT COUNT. This is the whole
// fragment.
//
// Revit deletes dependents without asking: a wall takes its doors and windows,
// a duct takes its fittings and insulation, a level can take everything on it.
// `Document.Delete` hands back the ids that ACTUALLY went, and that set is
// routinely larger than the one passed in. Reporting "deleted 4" from the input
// list would be the defining failure of this project - the number ASKED FOR
// reported as the number that HAPPENED - except here it under-reports
// destruction, which is the worst direction for it to be wrong in.
//
// `alsoWent` IS THE NUMBER SOMEBODY NEEDS TO SEE. Four ducts asked for and
// nineteen elements gone is not a detail; it is the difference between a tidy-up
// and an accident, and it is invisible unless it is counted here.
//
// A DELETION THAT REVIT REFUSES DOES NOT ABANDON THE BATCH. Some elements
// cannot be deleted - a pinned element, a member of an unmodifiable group, an
// element owned by another user. Each id is submitted so a refusal is recorded
// and stepped over. Slower than one bulk call and worth it: a batch that throws
// at the eleventh of two hundred has already deleted ten and reports nothing.
//
// VERSION 2 (2026-11-16, row 5b-1674) - A WALL ATTACHED TO WHAT GOES IS NAMED.
//
//   Deleting a roof, a floor or a ceiling DETACHES every wall attached to it.
//   The wall is not deleted, so it is in nothing Document.Delete returns, and
//   its top drops back to its own height without a word - Revit's only sign
//   is "Walls 19 modified" (issue #852). On Revit 2020 to 2024 Heron cannot
//   attach a wall again, so the modeller has to know before keeping it.
//
//   BEFORE THE DELETE: every wall whose top or base is attached - the wall's
//   own read-only "top is attached" / "base is attached", on every release -
//   and whose plan extent overlaps a roof, floor or ceiling being deleted is
//   named in `attachedBefore`. That is a likely list, not a certain one: a
//   wall can be attached to something else above the same plan.
//
//   AFTER IT: the model is rebuilt and each of those walls read again. One
//   attached before and not after is in `detachedWalls` - measured, not
//   inferred - and `wallsNote` says plainly what that means and, on 2020 to
//   2024, that Heron cannot attach them again. A wall that went with the
//   delete is in `deleted`, not here. When nothing attached is near what is
//   deleted, nothing is rebuilt and nothing here changes how a delete runs.
//
//   A REBUILD REVIT CANNOT FINISH IS THROWN, never swallowed: Revit's
//   reference says the model must not be read after it, so the whole call is
//   rolled back and nothing is deleted.

var askedFor = elements.Count;
var deleted = new List<ElementId>();
var refused = new List<ElementId>();
var attachedBefore = "";
var detachedWalls = new List<ElementId>();
var wallsNote = "";

// ---- VERSION 2: THE WALLS ATTACHED TO A ROOF, FLOOR OR CEILING BEING DELETED
var askedIds = new List<ElementId>();
var hostBoxes = new List<BoundingBoxXYZ>();
foreach (var element in elements)
{
    if (element == null) continue;
    askedIds.Add(element.Id);
    if (!(element is RoofBase || element is Floor || element is Ceiling)) continue;
    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch { box = null; }
    if (box != null) hostBoxes.Add(box);
}

Func<Wall, BuiltInParameter, bool> isAttached = (wall, which) =>
{
    try
    {
        var flag = wall.get_Parameter(which);
        return flag != null && flag.HasValue && flag.StorageType == StorageType.Integer && flag.AsInteger() == 1;
    }
    catch { return false; }
};

// Each candidate: the wall, and which ends were attached before.
var candidates = new List<Tuple<Wall, bool, bool>>();
if (hostBoxes.Count > 0)
{
    foreach (Wall wall in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        if (askedIds.Contains(wall.Id)) continue;
        var topWas = isAttached(wall, BuiltInParameter.WALL_TOP_IS_ATTACHED);
        var baseWas = isAttached(wall, BuiltInParameter.WALL_BOTTOM_IS_ATTACHED);
        if (!topWas && !baseWas) continue;

        BoundingBoxXYZ wallBox = null;
        try { wallBox = wall.get_BoundingBox(null); } catch { wallBox = null; }
        if (wallBox == null) continue;

        var under = false;
        foreach (var hostBox in hostBoxes)
        {
            if (wallBox.Max.X < hostBox.Min.X || wallBox.Min.X > hostBox.Max.X) continue;
            if (wallBox.Max.Y < hostBox.Min.Y || wallBox.Min.Y > hostBox.Max.Y) continue;
            under = true;
            break;
        }
        if (!under) continue;

        candidates.Add(Tuple.Create(wall, topWas, baseWas));
        attachedBefore += (attachedBefore.Length == 0 ? "" : ", ")
            + string.Format("wall {0} ({1})", wall.Id,
                topWas && baseWas ? "top and base attached" : topWas ? "top attached" : "base attached");
    }
    if (attachedBefore.Length > 0)
        attachedBefore = "Attached, and in plan under a roof, floor or ceiling being deleted: " + attachedBefore;
}

foreach (var element in elements)
{
    if (element == null) continue;

    var id = element.Id;
    try
    {
        var went = doc.Delete(id);

        // An EMPTY return is not a success. Revit accepted the call and removed
        // nothing, which is the silent-no-op shape this repository has already
        // been bitten by on the move path. Recorded as refused, not counted.
        if (went == null || went.Count == 0)
        {
            refused.Add(id);
            continue;
        }

        foreach (var gone in went)
        {
            if (!deleted.Contains(gone)) deleted.Add(gone);
        }
    }
    catch (Exception)
    {
        refused.Add(id);
    }
}

// What went beyond what was named. Never below zero: an element can be taken
// as a dependent of an earlier deletion and then be missing when its own turn
// comes, so the two counts are not guaranteed to be ordered.
var alsoWent = deleted.Count - askedFor;
if (alsoWent < 0) alsoWent = 0;

// ---- VERSION 2: WHICH OF THOSE WALLS LOST THEIR ATTACHMENT, MEASURED ------
if (candidates.Count > 0 && deleted.Count > 0)
{
    try
    {
        doc.Regenerate();
    }
    catch (Exception notRebuilt)
    {
        throw new InvalidOperationException(
            "Revit could not rebuild the model after the delete, so the walls attached to what was "
            + "deleted could not be read again - " + notRebuilt.Message + ". Nothing this call did is kept");
    }

    var stillAttached = 0;
    foreach (var candidate in candidates)
    {
        var wall = candidate.Item1;
        if (!wall.IsValidObject || deleted.Contains(wall.Id)) continue;
        var topNow = isAttached(wall, BuiltInParameter.WALL_TOP_IS_ATTACHED);
        var baseNow = isAttached(wall, BuiltInParameter.WALL_BOTTOM_IS_ATTACHED);
        if ((candidate.Item2 && !topNow) || (candidate.Item3 && !baseNow))
            detachedWalls.Add(wall.Id);
        else
            stillAttached++;
    }

    if (detachedWalls.Count > 0)
    {
        int release;
        int.TryParse(doc.Application.VersionNumber, out release);
        wallsNote = string.Format(
            "{0} wall(s) were attached to what was deleted and are DETACHED now - each wall's top or "
            + "base goes back to its own height and level: {1}. {2}{3}",
            detachedWalls.Count,
            string.Join(", ", detachedWalls.Select(id => id.ToString()).ToArray()),
            release > 0 && release < 2025
                ? "On Revit " + release + " Heron cannot attach a wall again - Attach Top/Base by hand "
                  + "(Modify | Walls) once the new roof, floor or ceiling is in."
                : "ATTACH_WALLS can attach them to what replaces it.",
            stillAttached > 0
                ? string.Format(" {0} other wall(s) there are still attached to something else.", stillAttached)
                : "");
    }
}
