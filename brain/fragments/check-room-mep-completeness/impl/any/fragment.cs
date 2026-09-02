// NOT STANDALONE. Assumes `doc`, `elements`, `devices`, `ruleRoomNames`,
// `ruleCategories` and `ruleMinimums` are in scope; leaves `findings`,
// `shortRooms`, `unbounded` and `noRule` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A DEVICE IS IN A ROOM BY REVIT'S OWN BOUNDARY TEST. A bounding box would
// claim a device standing in the notch of an L-shaped room, and that is exactly
// the room somebody is checking.
//
// A DEVICE IS PLACED BY ITS INSERTION POINT, so one in the ceiling void above
// the room still counts. That is what is wanted for a diffuser, and it is said
// rather than left to be discovered.
//
// ROOMS WITH NO AREA ARE EXCLUDED AND COUNTED, NEVER FAILED. An unenclosed room
// has no boundary, every test against it returns false, and it would report as
// missing everything - dozens of false failures burying the real ones.
//
// A ROOM MATCHING NO RULE IS COUNTED AND LISTED. Rules match on the room's NAME,
// so a naming mismatch must never come back as a clean result.
//
// SEVERAL RULES CAN APPLY TO ONE ROOM AND ALL OF THEM DO. "Every room needs a
// detector" and "every toilet needs extract" are both true of a toilet.
//
// THE TOTAL FOUND PER CATEGORY IS PRINTED FIRST. A category with zero found
// anywhere means the devices are in a LINKED model, not that every room is
// missing them - and without this line that reads as a catastrophic result.

var findings = new List<string>();
var shortRooms = new List<ElementId>();
var unbounded = new List<ElementId>();
var noRule = new List<ElementId>();

var ruleCount = Math.Min(ruleRoomNames.Count, Math.Min(ruleCategories.Count, ruleMinimums.Count));

if (ruleCount == 0)
{
    findings.Add("No rules were given, and there is no default set worth applying - what a room needs "
        + "depends on the project, the discipline and the authority");
}
else
{
    // What was actually found, by category. See the header - this line is what
    // tells a link problem apart from a modelling one.
    var foundByCategory = new Dictionary<string, int>();
    foreach (var device in devices)
    {
        if (device == null || device.Category == null) continue;
        var name = device.Category.Name;
        if (!foundByCategory.ContainsKey(name)) foundByCategory[name] = 0;
        foundByCategory[name] = foundByCategory[name] + 1;
    }

    for (var r = 0; r < ruleCount; r++)
    {
        var category = ruleCategories[r];
        var found = foundByCategory.ContainsKey(category) ? foundByCategory[category] : 0;
        findings.Add(string.Format("'{0}': {1} found across everything handed in{2}",
            category, found,
            found == 0 ? "  <- ZERO ANYWHERE. They are probably in a LINKED model, in which case every "
                + "room below will read as missing them and none of it means anything" : ""));
    }

    foreach (var element in elements)
    {
        if (element == null) continue;

        var room = element as Room;
        if (room == null) { noRule.Add(element.Id); continue; }

        if (room.Area <= 0) { unbounded.Add(room.Id); continue; }

        var roomName = (room.Name ?? "");
        var matched = 0;
        var missing = new List<string>();

        for (var r = 0; r < ruleCount; r++)
        {
            var wants = ruleRoomNames[r] ?? "";
            if (wants.Length > 0
                && roomName.IndexOf(wants, StringComparison.OrdinalIgnoreCase) < 0) continue;

            matched++;

            var category = ruleCategories[r];
            var minimum = ruleMinimums[r];
            var count = 0;

            foreach (var device in devices)
            {
                if (device == null || device.Category == null) continue;
                if (device.Category.Name != category) continue;

                var point = device.Location as LocationPoint;
                if (point == null) continue;

                // Revit's own boundary test, not a box.
                try { if (room.IsPointInRoom(point.Point)) count++; }
                catch { }
            }

            if (count < minimum)
                missing.Add(string.Format("{0} ({1} of {2})", category, count, minimum));
        }

        if (matched == 0) { noRule.Add(room.Id); continue; }

        if (missing.Count > 0)
        {
            shortRooms.Add(room.Id);
            findings.Add(string.Format("{0}  - short of: {1}", roomName, string.Join(", ", missing)));
        }
    }

    findings.Add(string.Format(
        "{0} room(s) short of what the rules ask for. {1} had no area and could not be checked at all, "
        + "{2} matched no rule - and a room matching no rule is a NAMING mismatch, not a pass",
        shortRooms.Count, unbounded.Count, noRule.Count));
}
