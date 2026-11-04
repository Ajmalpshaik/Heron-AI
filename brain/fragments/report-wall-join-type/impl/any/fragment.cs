// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `joins`,
// `freeEnds` and `findings` behind. Changes nothing.
//
// THE END JOIN OF A WALL - BUTT, MITER OR SQUARE OFF, as Modify > Wall Joins
// shows it. Revit keeps the type on the wall's location line, one per end;
// Revit's "Butt" is JoinType.Abut. This is not Join Geometry, which
// REPORT_JOINED_ELEMENTS reads: walls meeting at a corner clean up by the end
// join and do not show there.
//
// ONE ROW PER CORNER. A corner reached from two walls handed in is reported
// once, keyed by the walls at it. End 0 and end 1 are named as start and end
// of the wall as it was drawn - nothing on screen shows which is which, so
// the other walls at the join are what identify the corner.
//
// A FREE END IS ORDINARY. It is counted in `freeEnds`, and an end where
// joining is disallowed says so, so neither reads as a failed lookup.

var joins = new List<string>();
var freeEnds = 0;
var findings = new List<string>();

Func<Element, string> describe = e => string.Format("{0} '{1}' (id {2})",
    e is Wall ? "wall" : (e.Category != null ? e.Category.Name : "element"), e.Name, e.Id);

Func<JoinType, string> say = t =>
    t == JoinType.Abut ? "Butt"
    : t == JoinType.Miter ? "Miter"
    : t == JoinType.SquareOff ? "Square Off"
    : t == JoinType.None ? "no join type"
    : t.ToString();

if (elements == null || elements.Count == 0)
{
    findings.Add("No walls were handed in - select the walls at the corner in question");
}
else
{
    var seen = new HashSet<string>();
    var notWalls = 0;
    var walls = 0;
    var disallowed = 0;
    var cornersRead = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;
        var wall = element as Wall;
        var line = wall != null ? wall.Location as LocationCurve : null;
        if (line == null) { notWalls++; continue; }
        walls++;

        for (int end = 0; end <= 1; end++)
        {
            var side = end == 0 ? "start" : "end";
            var allowed = true;
            try { allowed = WallUtils.IsWallJoinAllowedAtEnd(wall, end); } catch { }

            var atJoin = new List<Element>();
            try
            {
                var array = line.get_ElementsAtJoin(end);
                if (array != null)
                    foreach (Element other in array)
                        if (other != null && other.Id != wall.Id) atJoin.Add(other);
            }
            catch
            {
                joins.Add(describe(wall) + ", " + side + ": Revit would not say what meets it here");
                continue;
            }

            if (atJoin.Count == 0)
            {
                freeEnds++;
                if (!allowed)
                {
                    disallowed++;
                    joins.Add(describe(wall) + ", " + side + ": meets nothing - joining is DISALLOWED at this end");
                }
                continue;
            }

            var key = string.Join("|", atJoin.Select(o => o.Id.ToString())
                .Concat(new[] { wall.Id.ToString() }).OrderBy(s => s, StringComparer.Ordinal));
            if (!seen.Add(key)) continue;

            string type;
            try { type = say(line.get_JoinType(end)); }
            catch (Exception ex) { type = "not readable (" + ex.Message + ")"; }

            cornersRead++;
            joins.Add(string.Format("{0}, {1}, with {2}: {3}", describe(wall), side,
                string.Join(", ", atJoin.Select(describe)), type));
        }
    }

    findings.Add(string.Format("{0} corner(s) read across {1} wall(s); {2} wall end(s) meet nothing, "
        + "which is ordinary", cornersRead, walls, freeEnds));
    if (disallowed > 0)
        findings.Add(string.Format("{0} end(s) have joining disallowed - the Disallow Join tool allows it again",
            disallowed));
    if (notWalls > 0)
        findings.Add(string.Format("{0} element(s) handed in are not walls drawn along a line and were not read",
            notWalls));
}
