// NOT STANDALONE. Assumes `doc`, `elements`, `newLevel`, `topLevelId`,
// `keepWallBase` and `allowUnconnectedToBecomeBound` are in scope; leaves
// `moved`, `alreadyThere`, `heightChanged`, `refused`, `unconnectedTop`,
// `notAWall`, `notApplicable`, `problem` and `columnReport` behind.
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
//
// VERSION 3 (2026-11-16, row 5b-1673) - COLUMNS, BOTH ENDS, THE WALL'S RULE.
//
//   A COLUMN HAS A BASE LEVEL AND A TOP LEVEL, like a wall, and version 2 had
//   no parameter it knew for either: a column was refused, or named in
//   `notAWall` while its top was left as it was. Now an architectural or a
//   structural column - known by the built-in column Base Level and Top Level
//   parameters, never by a name (an architectural column carries two
//   parameters both called "Top Level", #853 item B2) - goes the way a wall
//   does: its base to `newLevel` unless `keepWallBase`, its top to
//   `topLevelId` when one is given, each offset re-solved so the column does
//   not move, and a failure part-way put back.
//
//   A COLUMN WHOSE TOP OR BASE IS ATTACHED to a roof, floor or beam has that
//   end set by what it is attached to, not by its level and offset, so the
//   end is not re-hosted: the column is refused and `columnReport` says
//   which end and to what. A column with no top level has no height to keep,
//   so its top is named in `unconnectedTop` and left alone.
//
//   `columnReport` says, column by column, what each end was and is now - or
//   why it was left - in plain words.
//
// VERSION 4 (2026-11-20, FRAGMENT-ISSUES row 5b-342) - A PARAMETER ASKED FOR BY
// ITS ID IS NOT PROOF THE ELEMENT CARRIES IT.
//
//   Measured 2026-10-06 (Project2, Revit 2024) on a ceiling diffuser placed on
//   a level's plane: get_Parameter(INSTANCE_FREE_HOST_OFFSET_PARAM) answered
//   with a writable "Offset from Host" whose Set said true and kept nothing,
//   and get_Parameter(FAMILY_LEVEL_PARAM) with an empty "Level" - neither in
//   the element's own parameter set. Version 3 took the first level and the
//   first offset parameter get_Parameter returned, so a write could go into
//   one of those and be counted as moved. So for everything that is not a
//   wall, the level, the offset and a column's two ends are taken from the
//   ELEMENT'S OWN parameter set (`carried`, PLACE_FAMILY_INSTANCES version 2's
//   rule), and the level parameter must hold a LEVEL - one that holds none is
//   passed over and the next asked (FILTER_ELEMENTS_BY_CATEGORY version 3's
//   rule, row 5b-335). An offset that does not read back as written is not
//   taken: what was written is put back and the element refused. A wall is
//   read as version 3 read it.

var moved = 0;
var alreadyThere = 0;
var heightChanged = new List<ElementId>();
var refused = new List<ElementId>();
var unconnectedTop = new List<ElementId>();
var notAWall = new List<ElementId>();
string notApplicable = null;
string problem = null;
var columnReport = "";

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

    // VERSION 4 (row 5b-342): what the element really carries, read off its
    // own parameter set - never get_Parameter(id), which can answer with a
    // parameter the element has not got.
    Func<Element, Dictionary<BuiltInParameter, Parameter>> carried = element =>
    {
        var held = new Dictionary<BuiltInParameter, Parameter>();
        try
        {
            foreach (Parameter parameter in element.Parameters)
            {
                var definition = parameter == null ? null : parameter.Definition as InternalDefinition;
                if (definition == null || definition.BuiltInParameter == BuiltInParameter.INVALID) continue;
                if (!held.ContainsKey(definition.BuiltInParameter)) held[definition.BuiltInParameter] = parameter;
            }
        }
        catch { }
        return held;
    };
    Func<Dictionary<BuiltInParameter, Parameter>, BuiltInParameter, Parameter> ownOne = (set, which) =>
    {
        Parameter found;
        return set != null && set.TryGetValue(which, out found) ? found : null;
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

        // ---- A COLUMN (version 3, row 5b-1673) --------------------------------
        // Known by the built-in column Base Level and Top Level, the test
        // COPY_ELEMENTS uses, never by a parameter found by name - and since
        // version 4 (row 5b-342) only when the element's own parameter set
        // holds both, as it does each offset this writes.
        var own = carried(element);
        var columnInstance = element as FamilyInstance;
        var columnBase = columnInstance != null
            ? ownOne(own, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM) : null;
        var columnTop = columnInstance != null
            ? ownOne(own, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM) : null;
        if (columnBase != null && columnTop != null
            && columnBase.StorageType == StorageType.ElementId
            && columnTop.StorageType == StorageType.ElementId)
        {
            var label = string.Format("column {0}", element.Id);
            Action<string> say = text =>
                columnReport += (columnReport.Length == 0 ? "" : " | ") + label + ": " + text;

            Func<ElementId, string> levelName = id =>
            {
                var level = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id) as Level;
                return level == null ? "no level" : "'" + level.Name + "'";
            };

            var wasBaseLevel = columnBase.AsElementId();
            var wasTopLevel = columnTop.AsElementId();
            var baseOffsetParameter = ownOne(own, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
            var topOffsetParameter = ownOne(own, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
            var wasColumnBaseOffset = baseOffsetParameter != null ? baseOffsetParameter.AsDouble() : 0;
            var wasColumnTopOffset = topOffsetParameter != null ? topOffsetParameter.AsDouble() : 0;

            bool columnTopUnconnected = wasTopLevel == ElementId.InvalidElementId
                || !(doc.GetElement(wasTopLevel) is Level);
            var wasColumnBase = elevationOf(wasBaseLevel) + wasColumnBaseOffset;
            var wasColumnTop = elevationOf(wasTopLevel) + wasColumnTopOffset;

            bool columnDoBase = !keepWallBase && wasBaseLevel != newLevel.Id;
            bool columnDoTop = wantTop && wasTopLevel != topLevelId;

            if (columnDoTop && columnTopUnconnected)
            {
                // No top level means no top height to hold - the flag that
                // lets a wall's typed height become level-bound does not
                // reach a column.
                unconnectedTop.Add(element.Id);
                say("it has no top level, so there is no top height to keep - its top was left alone");
                columnDoTop = false;
            }

            if (!columnDoBase && !columnDoTop)
            {
                if (keepWallBase && !wantTop) wallsWithNothingAsked++;
                else if (!unconnectedTop.Contains(element.Id))
                {
                    alreadyThere++;
                    say("already on the levels asked for");
                }
                continue;
            }

            // AN ATTACHED END IS SET BY WHAT IT IS ATTACHED TO. Re-hosting it
            // would write an offset Revit then overrides, so it is refused.
            string attachedTo = null;
            for (var end = 0; end < 2 && attachedTo == null; end++)
            {
                if (end == 0 && !columnDoBase) continue;
                if (end == 1 && !columnDoTop) continue;
                ColumnAttachment attachment = null;
                try { attachment = ColumnAttachment.GetColumnAttachment(columnInstance, end); } catch { attachment = null; }
                if (attachment == null) continue;
                var target = doc.GetElement(attachment.TargetId);
                attachedTo = string.Format("its {0} is attached to {1} {2}, which sets that end - "
                    + "detach it (ATTACH_COLUMNS, detach) and ask again",
                    end == 0 ? "base" : "top",
                    target != null && target.Category != null ? target.Category.Name : "element",
                    attachment.TargetId);
            }
            if (attachedTo != null)
            {
                refused.Add(element.Id);
                say(attachedTo + ". Nothing on it was changed");
                continue;
            }

            if ((columnDoBase && (columnBase.IsReadOnly || baseOffsetParameter == null || baseOffsetParameter.IsReadOnly))
                || (columnDoTop && (columnTop.IsReadOnly || topOffsetParameter == null || topOffsetParameter.IsReadOnly)))
            {
                refused.Add(element.Id);
                say("Revit holds the level or offset it would need as read-only. Nothing on it was changed");
                continue;
            }

            bool columnBaseWritten = false, columnTopWritten = false, columnFailed = false;
            string columnError = null;

            // THE TOP FIRST WHEN IT RISES, THE BASE FIRST OTHERWISE, so the
            // column's two levels never cross on the way: a base set above an
            // old top, or a top set below an old base, is a column Revit may
            // refuse. Each offset is re-solved at once, so the column does not
            // move between the two.
            var topFirst = columnDoTop && newTop != null && newTop.Elevation > elevationOf(wasTopLevel);
            for (var step = 0; step < 2 && !columnFailed; step++)
            {
                var doingTop = topFirst ? step == 0 : step == 1;
                try
                {
                    if (doingTop && columnDoTop)
                    {
                        columnTopWritten = true;
                        columnTop.Set(topLevelId);
                        topOffsetParameter.Set(wasColumnTop - newTop.Elevation);
                    }
                    else if (!doingTop && columnDoBase)
                    {
                        columnBaseWritten = true;
                        columnBase.Set(newLevel.Id);
                        baseOffsetParameter.Set(wasColumnBase - newLevel.Elevation);
                    }
                }
                catch (Exception notTaken)
                {
                    columnFailed = true;
                    columnError = notTaken.Message;
                }
            }

            Func<bool> columnMoved = () =>
                Math.Abs(elevationOf(columnBase.AsElementId())
                         + (baseOffsetParameter != null ? baseOffsetParameter.AsDouble() : 0) - wasColumnBase) > SameHeight
                || (!columnTopUnconnected
                    && Math.Abs(elevationOf(columnTop.AsElementId())
                                + (topOffsetParameter != null ? topOffsetParameter.AsDouble() : 0) - wasColumnTop) > SameHeight);

            if (columnFailed)
            {
                // PUT BACK, IN REVERSE, ONLY WHAT WAS WRITTEN - the wall's rule.
                if (columnTopWritten)
                {
                    try { columnTop.Set(wasTopLevel); } catch { }
                    try { topOffsetParameter.Set(wasColumnTopOffset); } catch { }
                }
                if (columnBaseWritten)
                {
                    try { columnBase.Set(wasBaseLevel); } catch { }
                    try { baseOffsetParameter.Set(wasColumnBaseOffset); } catch { }
                }
                refused.Add(element.Id);
                if (columnMoved()) heightChanged.Add(element.Id);
                say("Revit would not take it - " + columnError + ". What was written was put back");
                continue;
            }

            moved++;
            if (columnMoved()) heightChanged.Add(element.Id);
            say(string.Format("base {0} to {1}, top {2} to {3}; base {4:F0} mm and top {5:F0} mm {6}",
                levelName(wasBaseLevel), levelName(columnBase.AsElementId()),
                levelName(wasTopLevel), levelName(columnTop.AsElementId()),
                (elevationOf(columnBase.AsElementId()) + baseOffsetParameter.AsDouble()) * 304.8,
                (elevationOf(columnTop.AsElementId()) + topOffsetParameter.AsDouble()) * 304.8,
                heightChanged.Contains(element.Id) ? "- AND IT MOVED, see heightChanged" : "where they were"));
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
        // VERSION 4 (row 5b-342): one of its OWN parameters, and one that holds
        // a level - a writable one that holds none is passed over and the next
        // asked; if none holds a level, the first writable one is kept, so the
        // element is refused below as on no level, as version 3 refused it.
        Parameter levelParameter = null;
        Parameter levelParameterEmpty = null;
        foreach (var which in levelParameters)
        {
            var candidate = ownOne(own, which);
            if (candidate == null || candidate.IsReadOnly) continue;
            if (candidate.StorageType != StorageType.ElementId) continue;
            ElementId holds = ElementId.InvalidElementId;
            try { holds = candidate.AsElementId(); } catch { }
            if (holds != null && holds != ElementId.InvalidElementId
                && element.Document.GetElement(holds) is Level)
            {
                levelParameter = candidate;
                break;
            }
            if (levelParameterEmpty == null) levelParameterEmpty = candidate;
        }
        if (levelParameter == null) levelParameter = levelParameterEmpty;

        // VERSION 4, THE REVIEW OF 2026-11-20: A SCHEDULE LEVEL HOLDS NO HEIGHT.
        // Only a Reference Level (RBS_START_LEVEL_PARAM) or a family's Level
        // (FAMILY_LEVEL_PARAM) is what an element's offset is measured from. An
        // element whose only level is its Schedule Level - a work plane-based
        // family, whose offset is from its host plane - would be given a new
        // Schedule Level and an offset lowered by the levels' difference, and
        // really drop by it, while the arithmetic read-back below said moved.
        // Version 3 refused such an element by accident, off an empty phantom
        // Level; version 4 refuses it on purpose, and writes nothing.
        if (levelParameter != null)
        {
            var levelDefinition = levelParameter.Definition as InternalDefinition;
            if (levelDefinition == null
                || (levelDefinition.BuiltInParameter != BuiltInParameter.RBS_START_LEVEL_PARAM
                    && levelDefinition.BuiltInParameter != BuiltInParameter.FAMILY_LEVEL_PARAM))
            {
                refused.Add(element.Id);
                continue;
            }
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
            // VERSION 4 (row 5b-342): its OWN offset only. get_Parameter
            // answered a diffuser on a level's plane with an "Offset from
            // Host" it has not got, whose Set says true and keeps nothing.
            var candidate = ownOne(own, which);
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
        // VERSION 4 (row 5b-342): a Set that says true and keeps nothing is not
        // a write - the offset must read back as written.
        if (took && Math.Abs(offsetParameter.AsDouble() - (wasAbsolute - newLevel.Elevation)) > SameHeight)
            took = false;

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
            ? string.Format("{0} element(s) are not walls or columns, so they were LEFT ALONE: the "
                + "base was kept and a top level applies to walls and columns only.", notAWall.Count)
            : string.Format("{0} element(s) are not walls or columns, so the top level does not apply "
                + "to them; each was moved to {1} like any other element, its height held.",
                notAWall.Count, newLevel.Name);
    }

    if (wallsWithNothingAsked > 0)
    {
        problem = string.Format("{0} wall(s) or column(s) were left alone: the base was kept and no "
            + "top level was given, so nothing was asked of them.", wallsWithNothingAsked);
    }
}
