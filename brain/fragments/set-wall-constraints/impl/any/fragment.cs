// NOT STANDALONE. Assumes `doc`, `elements`, `baseLevelId`, `topLevelId` and
// `allowUnconnectedToBecomeBound` are in scope, and leaves `rehosted`,
// `elevationChanged`, `unconnectedTop`, `notAWall` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A block of walls, one undo.
//
// THE OFFSET IS WHY THIS IS NOT A ONE-LINER.
//
// A wall sits at (level elevation + offset). Change the level and the offset
// stays, so the wall jumps by the difference between the two levels - every
// wall at once, quietly, and it looks correct in plan. The real elevation is
// therefore measured BEFORE, and the offset re-solved after, so the wall ends
// up exactly where it was.
//
// UNCONNECTED IS NOT A TOP LEVEL.
//
// A wall with an unconnected top has a plain height and no top constraint.
// Giving it one converts it to level-bound - a design change, not a re-host -
// so it is named and skipped unless the caller has said that is what they
// want.
//
// MEASURE AGAIN AFTERWARDS.
//
// The whole promise here is "nothing moves". A count of walls re-hosted does
// not test that promise; reading each wall's elevation back and comparing it
// with what it was, does.

int rehosted = 0;
var elevationChanged = new List<ElementId>();
var unconnectedTop = new List<ElementId>();
var notAWall = new List<ElementId>();
var refused = new List<ElementId>();

bool wantBase = baseLevelId != null && baseLevelId != ElementId.InvalidElementId;
bool wantTop = topLevelId != null && topLevelId != ElementId.InvalidElementId;

var newBase = wantBase ? doc.GetElement(baseLevelId) as Level : null;
var newTop = wantTop ? doc.GetElement(topLevelId) as Level : null;

if ((wantBase && newBase == null) || (wantTop && newTop == null))
{
    // A level that is not a level fails every wall the same way. Refused once,
    // by name, rather than forty identical rows in a report.
    foreach (var element in elements) if (element != null) refused.Add(element.Id);
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

    // Where the wall really is, in the world, both ends. An unconnected top is
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

    foreach (var element in elements)
    {
        var wall = element as Wall;
        if (wall == null) { if (element != null) notAWall.Add(element.Id); continue; }

        var was = realElevations(wall);

        bool topIsUnconnected =
            idOf(wall, BuiltInParameter.WALL_HEIGHT_TYPE) == ElementId.InvalidElementId;

        if (wantTop && topIsUnconnected && !allowUnconnectedToBecomeBound)
        {
            unconnectedTop.Add(wall.Id);
            if (!wantBase) continue;
        }

        bool touched = false;

        if (wantBase)
        {
            try
            {
                var constraint = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                var offset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                if (constraint == null || constraint.IsReadOnly || offset == null || offset.IsReadOnly)
                {
                    refused.Add(wall.Id);
                    continue;
                }
                constraint.Set(baseLevelId);
                // Re-solve so the wall stays where it was.
                offset.Set(was[0] - newBase.Elevation);
                touched = true;
            }
            catch { refused.Add(wall.Id); continue; }
        }

        if (wantTop && (!topIsUnconnected || allowUnconnectedToBecomeBound))
        {
            try
            {
                var constraint = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                if (constraint == null || constraint.IsReadOnly)
                {
                    refused.Add(wall.Id);
                    continue;
                }

                // THE CONSTRAINT FIRST, THE OFFSET AFTER, AND THAT ORDER IS THE
                // WHOLE FIX. On a wall with an UNCONNECTED top there is no top
                // level to measure an offset from, so Revit makes
                // WALL_TOP_OFFSET READ-ONLY and greys it out. Reading it before
                // the constraint is set therefore refused every unconnected
                // wall - which is precisely the case
                // `allowUnconnectedToBecomeBound` exists to permit, so the flag
                // could never do the one thing it is for.
                //
                // MEASURED 2026-09-13 on `test projject`: four plain walls with
                // base Level 1 and an unconnected 3000 mm top, asked for top
                // Level 2 with the flag TRUE, came back `rehosted 0, refused 4`.
                // The same four through the base path alone - the flag FALSE, so
                // this block is skipped entirely - came back `rehosted 4,
                // refused 0, elevationChanged 0`. The base path was never the
                // problem.
                //
                // If the offset is STILL read-only once the wall is bound, the
                // CONSTRAINT IS PUT BACK BEFORE REFUSING. An earlier version
                // said "the transaction decides what happens to it" and that was
                // wrong: on a normal return with `apply=true` the transaction
                // group is assimilated, so the wall kept its new top level with
                // no offset re-solved - exactly the half-set wall this
                // fragment's own cases.yaml forbids in as many words, "the id in
                // `refused` and no half-set wall - the level must not be
                // changed". Found by review on PR #138.
                //
                // THE OLD VALUE IS READ BEFORE THE WRITE, not reconstructed
                // after it. A restore that guesses what the value used to be is
                // a second write dressed as an undo.
                var wasTopConstraint = constraint.AsElementId();
                constraint.Set(topLevelId);

                var offset = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                if (offset == null || offset.IsReadOnly)
                {
                    try { constraint.Set(wasTopConstraint); } catch { }
                    refused.Add(wall.Id);
                    continue;
                }
                offset.Set(was[1] - newTop.Elevation);
                touched = true;
            }
            catch { refused.Add(wall.Id); continue; }
        }

        if (!touched) continue;

        // The promise was that nothing moves. This is the test of it.
        var now = realElevations(wall);
        if (Math.Abs(now[0] - was[0]) > 1e-6 || Math.Abs(now[1] - was[1]) > 1e-6)
            elevationChanged.Add(wall.Id);

        rehosted++;
    }
}
