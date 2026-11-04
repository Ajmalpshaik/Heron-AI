// NOT STANDALONE. Assumes `doc`, `elements`, `newLevel`, `topLevelId`,
// `keepWallBase` and `allowUnconnectedToBecomeBound` are in scope; leaves
// `moved`, `alreadyThere`, `heightChanged`, `refused`, `unconnectedTop`,
// `notAWall`, `notApplicable` and `problem` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE OFFSET MUST BE RECOMPUTED OR THE ELEMENT JUMPS A STOREY.
//
// Height is stored as an OFFSET FROM THE LEVEL. Set the level and leave the
// offset alone, and a duct 2800 above Level 1 becomes a duct 2800 above
// Level 2 - three metres higher, with nothing reported and nothing on screen
// to draw the eye until somebody sections through it.
//
// So: read the absolute height first, change the level, then write the offset
// that restores it. The arithmetic is a difference of two internal lengths,
// so no unit conversion arises anywhere in this fragment.
//
// AND VERIFIED AFTER. A recomputed offset that was written is not one that
// took - Revit refuses some offsets outright and clamps others. Any element
// whose absolute height moved is reported in `heightChanged`, which is the
// failure this fragment exists to prevent and the one number worth reading.
//
// VERSION 2 (2026-11-01) - SET_WALL_CONSTRAINTS MERGED IN: WALLS.
//
//   A WALL HAS TWO ENDS, AND EACH HAS A LEVEL. Version 1 found no level
//   parameter it knew on a wall and refused every one. Now a wall's BASE goes
//   to `newLevel`, its base offset re-solved so it does not move - unless
//   `keepWallBase` is set, which leaves every wall's base alone - and its TOP
//   goes to `topLevelId` when one is given, its top offset re-solved the same
//   way. `none`, or leaving it out, leaves the top alone. One call can re-host
//   the base, the top, or both.
//
//   UNCONNECTED IS NOT A TOP LEVEL. A wall with an unconnected top has a plain
//   height and no top constraint. Giving it one converts it to level-bound - a
//   design change, not a re-host - so it is named in `unconnectedTop` and its
//   top skipped, unless `allowUnconnectedToBecomeBound` says that is wanted.
//
//   A TOP LEVEL ID THAT IS NOT A LEVEL fails every element the same way, so it
//   is refused once, by name, and NOTHING is changed.
//
//   NOT A WALL: when a top level is asked for, or the walls' base is kept,
//   whatever is not a wall is named in `notAWall` and `notApplicable` says
//   what that meant for it - a top level does not apply to a duct.
//
//   A WRITE THAT FAILS PART-WAY IS PUT BACK. Version 1 set the level and then
//   the offset, and an offset Revit threw on left the element on its new level
//   with its old offset - moved by the difference between the levels, and only
//   in `refused`. SET_WALL_CONSTRAINTS had the same hole on a wall's base and a
//   thrown top offset. Now every value written to an element is read first,
//   and a failure writes them all back before the element is refused; if even
//   that leaves it somewhere else, it is in `heightChanged` too.

var moved = 0;
var alreadyThere = 0;
var heightChanged = new List<ElementId>();
var refused = new List<ElementId>();
var unconnectedTop = new List<ElementId>();
var notAWall = new List<ElementId>();
string notApplicable = null;
string problem = null;

var levelParameters = new BuiltInParameter[]
{
    BuiltInParameter.RBS_START_LEVEL_PARAM,
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
};

var offsetParameters = new BuiltInParameter[]
{
    BuiltInParameter.RBS_OFFSET_PARAM,
    BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM,
};

// Within a tenth of a millimetre, in internal feet: far below anything a
// modeller can draw and far above rounding noise.
const double SameHeight = 0.0003;

bool wantTop = topLevelId != null && topLevelId != ElementId.InvalidElementId;
var newTop = wantTop ? doc.GetElement(topLevelId) as Level : null;
bool wallOnly = wantTop || keepWallBase;

if (newLevel == null || (wantTop && newTop == null))
{
    // A level that is not a level fails every element identically. Refused
    // once, by name, rather than forty identical rows in a report.
    foreach (var element in elements) if (element != null) refused.Add(element.Id);
    problem = newLevel == null
        ? "No level was given, so NOTHING WAS CHANGED."
        : "The top level given is not a level, so NOTHING WAS CHANGED - it would fail every wall "
          + "the same way. Name a level for the walls' tops, or none to leave them alone.";
}
else
{
    Func<Element, BuiltInParameter, double> valueOf = (element, which) =>
    {
        try
        {
            var parameter = element.get_Parameter(which);
            return parameter != null && parameter.HasValue ? parameter.AsDouble() : 0;
        }
        catch { return 0; }
    };

    Func<Element, BuiltInParameter, ElementId> idOf = (element, which) =>
    {
        try
        {
            var parameter = element.get_Parameter(which);
            return parameter != null && parameter.HasValue
                ? parameter.AsElementId()
                : ElementId.InvalidElementId;
        }
        catch { return ElementId.InvalidElementId; }
    };

    Func<ElementId, double> elevationOf = id =>
    {
        if (id == null || id == ElementId.InvalidElementId) return 0;
        var level = doc.GetElement(id) as Level;
        return level == null ? 0 : level.Elevation;
    };

    // Where a wall really is, in the world, both ends. An unconnected top is
    // base plus its own height and has no level in it at all.
    Func<Wall, double[]> realElevations = wall =>
    {
        var baseId = idOf(wall, BuiltInParameter.WALL_BASE_CONSTRAINT);
        double baseElevation = elevationOf(baseId) + valueOf(wall, BuiltInParameter.WALL_BASE_OFFSET);

        var topId = idOf(wall, BuiltInParameter.WALL_HEIGHT_TYPE);
        double topElevation = topId != ElementId.InvalidElementId
            ? elevationOf(topId) + valueOf(wall, BuiltInParameter.WALL_TOP_OFFSET)
            : baseElevation + valueOf(wall, BuiltInParameter.WALL_USER_HEIGHT_PARAM);

        return new double[] { baseElevation, topElevation };
    };

    Func<Wall, double[], bool> wallMoved = (wall, was) =>
    {
        var now = realElevations(wall);
        return Math.Abs(now[0] - was[0]) > SameHeight || Math.Abs(now[1] - was[1]) > SameHeight;
    };

    var wallsWithNothingAsked = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var wall = element as Wall;

        // ---- A WALL (version 2, from SET_WALL_CONSTRAINTS) -------------------
        if (wall != null)
        {
            var was = realElevations(wall);
            var currentTop = idOf(wall, BuiltInParameter.WALL_HEIGHT_TYPE);
            bool topIsUnconnected = currentTop == ElementId.InvalidElementId;

            bool doBase = !keepWallBase
                && idOf(wall, BuiltInParameter.WALL_BASE_CONSTRAINT) != newLevel.Id;
            bool doTop = wantTop && currentTop != topLevelId;

            if (doTop && topIsUnconnected && !allowUnconnectedToBecomeBound)
            {
                unconnectedTop.Add(wall.Id);
                doTop = false;
            }

            if (!doBase && !doTop)
            {
                if (keepWallBase && !wantTop) wallsWithNothingAsked++;
                else if (!unconnectedTop.Contains(wall.Id)) alreadyThere++;
                continue;
            }

            var baseConstraint = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            var baseOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
            var topConstraint = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);

            if ((doBase && (baseConstraint == null || baseConstraint.IsReadOnly
                            || baseOffset == null || baseOffset.IsReadOnly))
                || (doTop && (topConstraint == null || topConstraint.IsReadOnly)))
            {
                refused.Add(wall.Id);
                continue;
            }

            // EVERY VALUE THIS MAY WRITE, READ BEFORE THE FIRST WRITE. A restore
            // that guesses what a value used to be is a second write dressed as
            // an undo.
            var wasBaseId = baseConstraint != null ? baseConstraint.AsElementId() : ElementId.InvalidElementId;
            var wasBaseOffset = baseOffset != null ? baseOffset.AsDouble() : 0;
            var wasTopId = currentTop;
            var wasTopOffsetParameter = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
            bool topOffsetWasWritable = wasTopOffsetParameter != null && !wasTopOffsetParameter.IsReadOnly;
            var wasTopOffset = topOffsetWasWritable ? wasTopOffsetParameter.AsDouble() : 0;

            bool baseWritten = false, topWritten = false, failed = false;

            if (doBase)
            {
                try
                {
                    baseWritten = true;
                    baseConstraint.Set(newLevel.Id);
                    // Re-solve so the wall stays where it was.
                    baseOffset.Set(was[0] - newLevel.Elevation);
                }
                catch { failed = true; }
            }

            if (doTop && !failed)
            {
                try
                {
                    // THE CONSTRAINT FIRST, THE OFFSET AFTER, AND THAT ORDER IS
                    // THE WHOLE FIX. On a wall with an UNCONNECTED top there is
                    // no top level to measure an offset from, so Revit makes
                    // WALL_TOP_OFFSET READ-ONLY. Reading it writable before the
                    // constraint is set refused every unconnected wall - which
                    // is precisely the case `allowUnconnectedToBecomeBound`
                    // exists to permit. MEASURED 2026-09-13 on `test projject`
                    // for SET_WALL_CONSTRAINTS: four walls with an unconnected
                    // 3000 mm top, asked for Level 2 with the flag true, came back
                    // `rehosted 0, refused 4` until the order was this one.
                    topWritten = true;
                    topConstraint.Set(topLevelId);

                    var topOffset = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                    if (topOffset == null || topOffset.IsReadOnly) failed = true;
                    else topOffset.Set(was[1] - newTop.Elevation);
                }
                catch { failed = true; }
            }

            if (failed)
            {
                // PUT BACK, IN REVERSE, AND ONLY WHAT WAS WRITTEN. A wall in
                // `refused` is a wall left as it was - never one on its new
                // level with its old offset (version 2's fix, both ends).
                if (topWritten)
                {
                    try { topConstraint.Set(wasTopId); } catch { }
                    if (topOffsetWasWritable)
                    {
                        try
                        {
                            var topOffset = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                            if (topOffset != null && !topOffset.IsReadOnly) topOffset.Set(wasTopOffset);
                        }
                        catch { }
                    }
                }
                if (baseWritten)
                {
                    try { baseConstraint.Set(wasBaseId); } catch { }
                    try { baseOffset.Set(wasBaseOffset); } catch { }
                }
                refused.Add(wall.Id);
                if (wallMoved(wall, was)) heightChanged.Add(wall.Id);
                continue;
            }

            moved++;

            // The promise was that nothing moves. This is the test of it.
            if (wallMoved(wall, was)) heightChanged.Add(wall.Id);
            continue;
        }

        // ---- NOT A WALL -------------------------------------------------------
        if (wallOnly)
        {
            notAWall.Add(element.Id);
            // The walls' base kept means the request was about walls' tops
            // alone: there is nothing here for anything else.
            if (keepWallBase) continue;
        }

        // Which parameter carries this element's level. MEP and family instances
        // differ, and an element with none cannot be reassigned at all.
        Parameter levelParameter = null;
        foreach (var which in levelParameters)
        {
            var candidate = element.get_Parameter(which);
            if (candidate == null || candidate.IsReadOnly) continue;
            if (candidate.StorageType != StorageType.ElementId) continue;
            levelParameter = candidate;
            break;
        }

        if (levelParameter == null)
        {
            refused.Add(element.Id);
            continue;
        }

        if (levelParameter.AsElementId() == newLevel.Id)
        {
            alreadyThere++;
            continue;
        }

        var oldLevelId = levelParameter.AsElementId();
        var oldLevel = element.Document.GetElement(oldLevelId) as Level;
        if (oldLevel == null)
        {
            refused.Add(element.Id);
            continue;
        }

        Parameter offsetParameter = null;
        foreach (var which in offsetParameters)
        {
            var candidate = element.get_Parameter(which);
            if (candidate == null || candidate.IsReadOnly) continue;
            if (candidate.StorageType != StorageType.Double) continue;
            offsetParameter = candidate;
            break;
        }

        if (offsetParameter == null)
        {
            // No offset to correct means no way to hold the height. Refused rather
            // than changing the level and letting it jump.
            refused.Add(element.Id);
            continue;
        }

        var oldOffset = offsetParameter.AsDouble();
        var wasAbsolute = oldLevel.Elevation + oldOffset;

        bool took = true;
        try
        {
            levelParameter.Set(newLevel.Id);
            offsetParameter.Set(wasAbsolute - newLevel.Elevation);
        }
        catch (Exception) { took = false; }

        if (took && levelParameter.AsElementId() != newLevel.Id) took = false;

        if (!took)
        {
            // VERSION 2: PUT BACK. Version 1 refused here and left whatever had
            // been written - a level changed with the offset not, or an offset
            // written while the level would not take - and either one moves the
            // element by the difference between the two levels.
            try { if (levelParameter.AsElementId() != oldLevelId) levelParameter.Set(oldLevelId); } catch { }
            try { offsetParameter.Set(oldOffset); } catch { }
            refused.Add(element.Id);

            var nowLevel = element.Document.GetElement(levelParameter.AsElementId()) as Level;
            if (nowLevel == null
                || Math.Abs(nowLevel.Elevation + offsetParameter.AsDouble() - wasAbsolute) > SameHeight)
                heightChanged.Add(element.Id);
            continue;
        }

        moved++;

        // The check this fragment is for.
        var nowAbsolute = newLevel.Elevation + offsetParameter.AsDouble();
        if (Math.Abs(nowAbsolute - wasAbsolute) > SameHeight) heightChanged.Add(element.Id);
    }

    if (notAWall.Count > 0)
    {
        notApplicable = keepWallBase
            ? string.Format("{0} element(s) are not walls, so they were LEFT ALONE: the walls' base "
                + "was kept and a top level applies to walls only.", notAWall.Count)
            : string.Format("{0} element(s) are not walls, so the top level does not apply to them; "
                + "each was moved to {1} like any other element, its height held.",
                notAWall.Count, newLevel.Name);
    }

    if (wallsWithNothingAsked > 0)
    {
        problem = string.Format("{0} wall(s) were left alone: the walls' base was kept and no top "
            + "level was given, so nothing was asked of them.", wallsWithNothingAsked);
    }
}
