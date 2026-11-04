// NOT STANDALONE. Assumes `doc`, `elements` and `joinType` are in scope;
// leaves `changed`, `corners`, `alreadyRight`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list.
//
// MODIFY > WALL JOINS - BUTT, MITER OR SQUARE OFF AT A WALL-END JOIN. Revit
// keeps the type on the wall's location line, one per end; Revit's "Butt" is
// JoinType.Abut.
//
// WHICH END IS NEVER GUESSED. End 0 and end 1 are the order the wall was drawn
// in, and nothing on screen shows which is which. So an end is changed only
// where the walls at that end's join include ANOTHER wall handed in: the
// modeller selects the walls on both sides of the corner, and the corner is
// where they meet. A wall meeting none of the others is refused by name; one
// wall alone is refused before anything changes.
//
// JOINING ON OR OFF IS NOT THIS FRAGMENT'S. An end where joining is disallowed
// is refused in words and left alone - DISALLOW_JOIN turns joining back on.
//
// ONE CORNER, ONE CHANGE. A corner reached from both of its walls is changed
// once: it is keyed by the walls at it, never by a number read off an id.
//
// READ BACK. Every corner set is read again after one regeneration; a call
// Revit accepted whose read-back did not change is refused, never counted.

var changed = 0;
var corners = new List<string>();
var alreadyRight = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

Func<Element, string> describe = e => string.Format("{0} '{1}' (id {2})",
    e is Wall ? "wall" : (e.Category != null ? e.Category.Name : "element"), e.Name, e.Id);

Func<JoinType, string> say = t =>
    t == JoinType.Abut ? "Butt"
    : t == JoinType.Miter ? "Miter"
    : t == JoinType.SquareOff ? "Square Off"
    : t == JoinType.None ? "no join"
    : t.ToString();

var word = new string((joinType ?? "").ToLowerInvariant().Where(char.IsLetter).ToArray());
JoinType wanted = JoinType.None;
var known = true;
if (word == "butt" || word == "abut") wanted = JoinType.Abut;
else if (word == "miter" || word == "mitre" || word == "mitered" || word == "mitred") wanted = JoinType.Miter;
else if (word == "squareoff" || word == "squared" || word == "square") wanted = JoinType.SquareOff;
else known = false;

var walls = new List<Wall>();
var handedIn = new HashSet<ElementId>();
if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var wall = element as Wall;
        if (wall == null || !(wall.Location is LocationCurve))
        {
            refused.Add(describe(element) + ": not a wall drawn along a line - only wall-end joins have a "
                + "join type");
            continue;
        }
        if (handedIn.Add(wall.Id)) walls.Add(wall);
    }
}

if (!known)
{
    refused.Insert(0, string.Format("joinType was '{0}' - say butt, miter or square off. NOTHING WAS CHANGED",
        joinType));
}
else if (walls.Count < 2)
{
    refused.Insert(0, string.Format("{0} wall(s) handed in. Select the walls on BOTH sides of the corner - "
        + "which end of a wall is the corner is read from where it meets the other wall, never guessed. "
        + "NOTHING WAS CHANGED", walls.Count));
}
else
{
    var done = new HashSet<string>();
    var pending = new List<Tuple<Wall, int, JoinType, string, string>>();

    foreach (var wall in walls)
    {
        var line = (LocationCurve)wall.Location;
        var meetsAny = false;

        for (int end = 0; end <= 1; end++)
        {
            var atJoin = new List<Element>();
            try
            {
                var array = line.get_ElementsAtJoin(end);
                if (array != null)
                    foreach (Element other in array)
                        if (other != null && other.Id != wall.Id) atJoin.Add(other);
            }
            catch { continue; }

            if (!atJoin.Any(other => handedIn.Contains(other.Id))) continue;
            meetsAny = true;

            var key = string.Join("|", atJoin.Select(o => o.Id.ToString())
                .Concat(new[] { wall.Id.ToString() }).OrderBy(s => s, StringComparer.Ordinal));
            if (!done.Add(key)) continue;

            var names = describe(wall) + " with " + string.Join(", ", atJoin.Select(describe));

            var allowed = true;
            try { allowed = WallUtils.IsWallJoinAllowedAtEnd(wall, end); } catch { }
            if (!allowed)
            {
                refused.Add(names + ": joining is disallowed at that end, so there is no join to shape. "
                    + "Allowing it again is the Disallow Join tool's - nothing was changed here");
                continue;
            }

            JoinType before;
            try { before = line.get_JoinType(end); }
            catch (Exception ex)
            {
                refused.Add(names + ": Revit would not say what join is there - " + ex.Message);
                continue;
            }

            if (before == wanted)
            {
                alreadyRight.Add(names + ": already " + say(wanted) + " - left alone");
                continue;
            }

            try { line.set_JoinType(end, wanted); }
            catch (Exception ex)
            {
                refused.Add(string.Format("{0}: Revit would not make this join a {1} - it stays {2}. "
                    + "Revit said: {3}", names, say(wanted), say(before), ex.Message));
                continue;
            }

            pending.Add(Tuple.Create(wall, end, before, names, say(before)));
        }

        if (!meetsAny)
            refused.Add(describe(wall) + ": meets none of the other walls handed in at either end - "
                + "nothing was changed on it. Select the walls on both sides of the corner");
    }

    if (pending.Count > 0)
    {
        doc.Regenerate();
        foreach (var p in pending)
        {
            JoinType after = JoinType.None;
            var read = true;
            try { after = ((LocationCurve)p.Item1.Location).get_JoinType(p.Item2); }
            catch { read = false; }

            if (read && after == wanted)
            {
                changed++;
                corners.Add(string.Format("{0}: {1} -> {2}, read back", p.Item4, p.Item5, say(after)));
            }
            else
            {
                var pinned = false;
                try { pinned = p.Item1.Pinned; } catch { }
                refused.Add(string.Format("{0}: Revit took the change but the join reads {1}, not {2}{3}",
                    p.Item4, read ? say(after) : "nothing readable", say(wanted),
                    pinned ? " - the wall is PINNED, which may be why; unpin it and run again" : ""));
            }
        }
    }
}

findings.Add(string.Format("{0} corner(s) made {1} and read back; {2} already {1}; {3} refused",
    changed, known ? say(wanted) : "(no type)", alreadyRight.Count, refused.Count));
findings.AddRange(refused);
