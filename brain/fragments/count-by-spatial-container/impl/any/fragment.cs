// NOT STANDALONE. Assumes `doc`, `elements` and `containerKind` are in scope;
// leaves `findings`, `groupNames`, `groupCounts`, `placed` and `unplaced` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// GROUP_AND_COUNT CANNOT DO THIS. It groups on a parameter, and most MEP
// elements have NO Room or Space parameter at all - a duct does not know which
// room it passes through. Asking through a lookup returns one large group of
// blanks and looks like an answer.
//
// PHASE IS NOT OFFERED HERE ON PURPOSE. "Count by phase created" looks like the
// same question; phase IS an ordinary parameter, so GROUP_AND_COUNT already does
// it, faster and exactly.
//
// THE TEST POINT TAKES THE CONTAINER'S Z, NOT THE ELEMENT'S. A room answers its
// containment test at its own level, and a diffuser sits at ceiling height - so
// testing the element's own Z reports everything as in no room.
//
// A ZONE IS ONE HOP FURTHER: the containing space is found first, then that
// space's zone. "No space" and "the space has no zone" are two different
// failures for two different people, so they are two groups.
//
// AN UNPLACED OR UNENCLOSED CONTAINER IS SKIPPED, NOT ASKED. Its test cannot
// mean anything, and asking it is how an element gets filed under a room that is
// not on the drawing.

var findings = new List<string>();
var groupNames = new List<string>();
var groupCounts = new List<int>();
var placed = 0;
var unplaced = 0;

var kind = (containerKind ?? "").Trim().ToLower();

var rooms = new List<Room>();
var spaces = new List<Space>();
var skippedContainers = 0;

if (kind == "room")
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType())
    {
        var room = element as Room;
        if (room == null) continue;
        // Area is inherited from SpatialElement. Zero means unplaced or
        // unenclosed, and such a room's containment test means nothing.
        if (room.Area <= 0.0) { skippedContainers++; continue; }
        rooms.Add(room);
    }
}
else if (kind == "space" || kind == "zone")
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_MEPSpaces).WhereElementIsNotElementType())
    {
        var space = element as Space;
        if (space == null) continue;
        if (space.Area <= 0.0) { skippedContainers++; continue; }
        spaces.Add(space);
    }
}

Func<Element, XYZ> pointOf = element =>
{
    var location = element.Location as LocationPoint;
    if (location != null && location.Point != null) return location.Point;

    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch (Exception) { }
    if (box == null) return null;
    return (box.Min + box.Max) * 0.5;
};

Func<Element, string> groupFor = element =>
{
    var point = pointOf(element);
    if (point == null) return "(no location Revit can read)";

    if (kind == "room")
    {
        foreach (var room in rooms)
        {
            // THE CONTAINER'S OWN Z, not the element's. See the header.
            var at = room.Location as LocationPoint;
            var z = at != null && at.Point != null ? at.Point.Z : point.Z;
            if (room.IsPointInRoom(new XYZ(point.X, point.Y, z)))
                return room.Name + " " + room.Number;
        }
        return "(in no room)";
    }

    Space containing = null;
    foreach (var space in spaces)
    {
        var at = space.Location as LocationPoint;
        var z = at != null && at.Point != null ? at.Point.Z : point.Z;
        if (space.IsPointInSpace(new XYZ(point.X, point.Y, z))) { containing = space; break; }
    }

    if (kind == "space")
    {
        return containing != null
            ? containing.Name + " " + containing.Number
            : "(in no space)";
    }

    // "zone" - and the two ways it can miss are different jobs for different
    // people, so they are different groups.
    if (containing == null) return "(in no space, so no zone either)";
    var zone = containing.Zone;
    return zone != null ? zone.Name : "(the space it is in has no zone)";
};

if (kind != "room" && kind != "space" && kind != "zone")
{
    findings.Add("'" + (containerKind ?? "") + "' is not a container this fragment knows. It counts by "
        + "\"room\", \"space\" or \"zone\", and NOTHING was counted - a default here would answer a "
        + "question nobody asked");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were handed in, so there is nothing to count. That is not a model with "
        + "no elements in any " + kind);
}
else if ((kind == "room" && rooms.Count == 0) || (kind != "room" && spaces.Count == 0))
{
    findings.Add("This model has no placed and enclosed " + (kind == "room" ? "room" : "space")
        + " at all" + (skippedContainers > 0
            ? " - " + skippedContainers + " were found but are unplaced or unenclosed, so their "
                + "containment test means nothing"
            : "")
        + ". EVERY element would report as being in none, and that says nothing about the elements");
}
else
{
    var index = new Dictionary<string, int>();

    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        var group = groupFor(element);
        var known = 0;
        if (index.TryGetValue(group, out known))
        {
            groupCounts[known] = groupCounts[known] + 1;
        }
        else
        {
            index[group] = groupNames.Count;
            groupNames.Add(group);
            groupCounts.Add(1);
        }

        if (group.StartsWith("(")) unplaced++; else placed++;
    }

    // Largest group first, by a plain selection pass over the two lists together
    // - which keeps them honestly in step at these sizes.
    var taken = new List<bool>();
    for (var i = 0; i < groupNames.Count; i++) taken.Add(false);

    while (true)
    {
        var best = -1;
        for (var i = 0; i < groupNames.Count; i++)
        {
            if (taken[i]) continue;
            if (best < 0 || groupCounts[i] > groupCounts[best]) best = i;
        }
        if (best < 0) break;
        taken[best] = true;
        findings.Add(string.Format("  {0,-46} {1}", groupNames[best], groupCounts[best]));
    }
}

if (skippedContainers > 0)
{
    findings.Add(string.Format("{0} {1}(s) in this model are UNPLACED or UNENCLOSED and were not asked. "
        + "Anything actually sitting in one of those reports as being in none, and a model full of them "
        + "is the real finding here",
        skippedContainers, kind == "room" ? "room" : "space"));
}

findings.Insert(0, string.Format("{0} element(s) counted by {1}: {2} landed in one, {3} did not. "
    + "Containment is tested GEOMETRICALLY, at the container's own height - these elements carry no "
    + "{1} parameter to group by, which is why a parameter lookup returns one large group of blanks",
    placed + unplaced,
    kind == "room" || kind == "space" || kind == "zone" ? kind : "(nothing)",
    placed, unplaced));
