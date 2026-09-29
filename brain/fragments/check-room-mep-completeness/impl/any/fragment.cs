// NOT STANDALONE. Assumes `doc`, `elements`, `devices`, `ruleRoomNames`,
// `ruleCategories`, `ruleMinimums` and `includeLinks` are in scope; leaves
// `findings`, `shortRooms`, `unbounded`, `noRule`, `linksSearched` and
// `linkedMatches` behind.
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

// ---- D-59: the same rules, over each link's rooms --------------------------

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

var linkBlocked = "";

if (ruleCount == 0 && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no rules were given - see findings";

for (var i = 0; i < linkDocs.Count && ruleCount > 0; i++)
{
    var linked = linkDocs[i];
    var summaryAt = linkedMatches.Count;
    var linkShort = 0;
    var linkUnbounded = 0;
    var linkNoRule = 0;
    var linkChecked = 0;

    // Each device's point in each placement's own coordinates, worked out once
    // per link rather than once per room.
    var localPoints = new List<KeyValuePair<string, List<XYZ>>>();
    foreach (var device in devices)
    {
        if (device == null || device.Category == null) continue;
        var point = device.Location as LocationPoint;
        if (point == null) continue;
        var here = new List<XYZ>();
        foreach (var placement in linkPlacements[i])
        {
            try { here.Add(placement.GetTotalTransform().Inverse.OfPoint(point.Point)); }
            catch (Exception) { }
        }
        localPoints.Add(new KeyValuePair<string, List<XYZ>>(device.Category.Name, here));
    }

    IList<Element> linkRooms = new List<Element>();
    try
    {
        linkRooms = new FilteredElementCollector(linked)
            .OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();
    }
    catch (Exception) { }

    foreach (var element in linkRooms)
    {
        var room = element as Room;
        if (room == null) continue;
        if (room.Area <= 0) { linkUnbounded++; continue; }

        var roomName = room.Name ?? "";
        var matched = 0;
        var missing = new List<string>();
        for (var r = 0; r < ruleCount; r++)
        {
            var wants = ruleRoomNames[r] ?? "";
            if (wants.Length > 0 && roomName.IndexOf(wants, StringComparison.OrdinalIgnoreCase) < 0) continue;
            matched++;

            var count = 0;
            foreach (var entry in localPoints)
            {
                if (entry.Key != ruleCategories[r]) continue;
                foreach (var local in entry.Value)
                {
                    var inside = false;
                    try { inside = room.IsPointInRoom(local); } catch { }
                    if (inside) { count++; break; }
                }
            }
            if (count < ruleMinimums[r])
                missing.Add(string.Format("{0} ({1} of {2})", ruleCategories[r], count, ruleMinimums[r]));
        }

        if (matched == 0) { linkNoRule++; continue; }
        linkChecked++;
        if (missing.Count == 0) continue;
        linkShort++;
        linkedMatches.Add(string.Format("  {0} - room '{1}' (id {2} in the link) - short of: {3}",
            linked.Title, roomName, room.Id, string.Join(", ", missing)));
    }

    linksSearched++;
    linkedTotal += linkShort;
    linkedMatches.Insert(summaryAt, string.Format("{0}: {1} of {2} room(s) checked are short; {3} had no "
        + "area, {4} matched no rule - a NAMING mismatch, not a pass", linked.Title, linkShort,
        linkChecked, linkUnbounded, linkNoRule));
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} linked room(s) short of what the rules ask for, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
