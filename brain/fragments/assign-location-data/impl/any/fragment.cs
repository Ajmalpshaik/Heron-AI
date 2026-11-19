// NOT STANDALONE. Assumes `doc`, `elements`, `roomParameter`, `numberParameter`,
// `levelParameter`, `useSpaces` and `includeLinks` are in scope; leaves
// `written`, `noRoomFound`, `refused`, `linksSearched` and `linkedMatches`
// behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// IT IS NOT WRITE_ELEMENT_PARAMETERS. That writes a value somebody supplies.
// This READS each element's position and derives what to write, per element -
// nobody can hand in four hundred different room names.
//
// VERSION 3 (2026-11-19, FRAGMENT-ISSUES 5b-1856), from a real job stopped
// before anything was written:
//   1. THE NAME ALONE GOES INTO roomParameter. Version 2 wrote Element.Name,
//      and for a room or a space Revit answers that with the name AND the
//      number together - so a name column got the number glued on. The name is
//      now the container's own Name parameter (ROOM_NAME, which a space carries
//      too).
//   2. numberParameter, new and optional, takes the Number on its own.
//   3. A SPACE IS TESTED BY ITS BOUNDARY, NOT ITS BOUNDING BOX. Version 2 cast
//      every container to Room; a Space is not one, so every space fell
//      through to a box test - and an L-shaped space's box claims the corridor
//      beside it. Now Space.IsPointInSpace, as Room.IsPointInRoom for a room.
//   4. THE STOREY IS CHECKED - see below. Version 2 took the first container
//      that had the element in plan, whatever floor it was on.
//   5. includeLinks: rooms and spaces in loaded links, when asked - see below.
// Every target is optional: left out, "none" (the card's word for left out) or
// blank skips it.
//
// A TARGET NAME TWO PARAMETERS SHARE IS NOT FILLED. A shared or project
// parameter can be bound beside a built-in one of the same name, and a lookup
// by name then returns one of them - "determined at random", in Autodesk's own
// reference - so the value would land in a field no schedule reads. Each target
// is checked on its own and counted apart (D-54 s3, FRAGMENT-ISSUES 5b-203).
// A target that is not a TEXT parameter is counted with the missing ones, and
// every value written is read back: what Revit did not keep is counted.
//
// ROOM AND SPACE ARE NOT THE SAME THING and this is the commonest way a location
// register comes out wrong. Architecture puts people in ROOMS; MEP calculates in
// SPACES. A model can hold both, in the same place, with different names.
//
// IN PLAN, THEN IN HEIGHT. A container has the element IN PLAN when Revit's own
// test says yes at the container's mid-height or at its location point's height
// (COUNT_BY_SPATIAL_CONTAINER asks at the second) - the element's own height
// would miss a damper in the ceiling void above a room that stops at the
// ceiling. But in plan alone a space on Level 1 also says yes to a damper on
// Level 3 directly above it. So a container that has the element in plan is
// taken only when the element is:
//   - INSIDE its volume - Revit's own test at the element's own point, the test
//     FILTER_ELEMENTS_IN_ROOM uses; or
//   - ABOVE IT BUT STILL ON ITS STOREY - not below its floor, and below the
//     container's own level plus the height to the next level up. That height
//     is a DIFFERENCE of two levels' elevations, measured up from the
//     container's location point, so it means the same in a link. The ceiling
//     void case (row 5b-175's container-height side, as version 2 took it).
// Anything else is NOT WRITTEN and is named with the container it was in plan
// of and why: below its floor, above its storey, or a storey whose top is not
// known (no level above, or the container has no level). A level that is not
// a storey - a "ceiling" reference level - makes the storey short, so ceiling
// void elements above it go unwritten and named; never written to the wrong
// floor. This can leave elements unwritten that COUNT_BY_SPATIAL_CONTAINER puts
// in a space, because that count asks in plan alone.
//
// TWO CONTAINERS EQUALLY GOOD ARE NOT WRITTEN. One the element is inside beats
// one it is above; between two it is above, the nearer top wins; two it is
// inside (overlapping rooms or spaces), or two equally near, are named and
// left alone rather than written with a guess.
//
// ONE POINT PER ELEMENT: its location point, or the middle of its bounding box.
//
// ROOMS AND SPACES IN A LINK ARE ASKED ONLY WHEN `includeLinks` IS SET - D-59:
// the modeller decides, job by job, and left out it is host only, as version 2
// was. On a federated MEP job the ROOMS ARE IN THE ARCHITECTURAL LINK, and host
// only answers "in no room" for every element. Set, each LOADED link's placed
// and enclosed rooms (or spaces) are asked too - COUNT_BY_SPATIAL_CONTAINER's
// mechanism: one entry per link FILE, every placement of it asked, the
// element's point moved into the link through the placement's inverse
// transform and tested there, with the link's own levels for the storey.
// NESTED LINKS ARE NOT READ, and the answer counts them.
//
// THIS MODEL'S OWN CONTAINER WINS - but only one that passed the storey check.
// When one of this model's rooms or spaces and a linked one both hold the
// element, this model's is written, and the answer counts how often that
// happened. A linked one is given only where no host container passed. The
// ELEMENTS written are always this model's own; a linked room is only read.

var written = 0;
var noRoomFound = new List<ElementId>();
var refused = new List<string>();

var wantedCategory = useSpaces ? BuiltInCategory.OST_MEPSpaces : BuiltInCategory.OST_Rooms;
var label = useSpaces ? "SPACE" : "ROOM";
var tolerance = 1.0 / 304.8;    // 1 mm, in Revit's feet

// The targets. "none" - what the card says a value left out is read as - and
// blank both mean skip it.
var nameTarget = (roomParameter ?? "").Trim();
if (string.Equals(nameTarget, "none", StringComparison.OrdinalIgnoreCase)) nameTarget = "";
var numberTarget = (numberParameter ?? "").Trim();
if (string.Equals(numberTarget, "none", StringComparison.OrdinalIgnoreCase)) numberTarget = "";
var levelTarget = (levelParameter ?? "").Trim();
if (string.Equals(levelTarget, "none", StringComparison.OrdinalIgnoreCase)) levelTarget = "";

// The height from a container's level to the next level up in its own
// document, or -1 when there is none or the container has no level. A
// difference of two elevations, so its origin never matters.
Func<SpatialElement, List<double>, double> storeyOf = (container, elevations) =>
{
    Level own = null;
    try { own = container.Level; } catch (Exception) { own = null; }
    if (own == null) return -1.0;
    var at = own.ProjectElevation;
    var next = double.MaxValue;
    foreach (var e in elevations)
        if (e > at + tolerance && e < next) next = e;
    return next == double.MaxValue ? -1.0 : next - at;
};

// Every level's elevation in one document.
Func<Document, List<double>> elevationsIn = (inDoc) =>
{
    var found = new List<double>();
    try
    {
        foreach (var level in new FilteredElementCollector(inDoc).OfClass(typeof(Level)).Cast<Level>())
            found.Add(level.ProjectElevation);
    }
    catch (Exception) { }
    return found;
};

// The placed and enclosed containers, with the box each one occupies in its own
// document. Zero area means unplaced or unenclosed, and such a container's test
// means nothing - COUNT_BY_SPATIAL_CONTAINER's rule. The host's come first; a
// linked one carries the transform INTO its link and the link's name.
var spatial = new List<SpatialElement>();
var spatialBox = new List<BoundingBoxXYZ>();
var spatialStorey = new List<double>();     // height to the next level up, or -1
var spatialTo = new List<Transform>();      // null: this model's own
var spatialFrom = new List<string>();       // "": this model's own
var skippedContainers = 0;
var hostElevations = elevationsIn(doc);
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(wantedCategory).WhereElementIsNotElementType())
{
    var container = element as SpatialElement;
    if (container == null) continue;
    BoundingBoxXYZ box = null;
    try { box = container.get_BoundingBox(null); } catch (Exception) { box = null; }
    if (box == null || container.Area <= 0.0) { skippedContainers++; continue; }
    spatial.Add(container);
    spatialBox.Add(box);
    spatialStorey.Add(storeyOf(container, hostElevations));
    spatialTo.Add(null);
    spatialFrom.Add("");
}

// ---- D-59: the links, only when asked for -----------------------------------
var linksSearched = 0;
var linkedMatches = new List<string>();
var nestedLinks = 0;
var linkedSkipped = 0;
if (includeLinks)
{
    // One entry per link FILE, keyed by link type - a file placed twice is one
    // model placed twice. LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT.
    var linkTypes = new List<ElementId>();
    var linkDocs = new List<Document>();
    var linkPlacements = new List<List<RevitLinkInstance>>();
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;
        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

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

    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkElevations = elevationsIn(linked);
        var inLink = new List<SpatialElement>();
        var inLinkBox = new List<BoundingBoxXYZ>();
        var inLinkStorey = new List<double>();
        try
        {
            foreach (var element in new FilteredElementCollector(linked)
                .OfCategory(wantedCategory).WhereElementIsNotElementType())
            {
                var container = element as SpatialElement;
                if (container == null) continue;
                BoundingBoxXYZ box = null;
                try { box = container.get_BoundingBox(null); } catch (Exception) { box = null; }
                if (box == null || container.Area <= 0.0) { linkedSkipped++; continue; }
                inLink.Add(container);
                inLinkBox.Add(box);
                inLinkStorey.Add(storeyOf(container, linkElevations));
            }
        }
        catch (Exception) { }

        foreach (var placement in linkPlacements[i])
        {
            Transform toLink = null;
            try { toLink = placement.GetTotalTransform().Inverse; }
            catch (Exception) { toLink = null; }
            if (toLink == null) continue;

            for (var c = 0; c < inLink.Count; c++)
            {
                spatial.Add(inLink[c]);
                spatialBox.Add(inLinkBox[c]);
                spatialStorey.Add(inLinkStorey[c]);
                spatialTo.Add(toLink);
                spatialFrom.Add(linked.Title);
            }
        }

        linksSearched++;
        linkedMatches.Add(string.Format("{0}: {1} placed and enclosed {2}(s) asked", linked.Title,
            inLink.Count, label));
    }
}

// A container's name and number as the answer shows them, and where it is.
Func<int, string> named = (i) =>
{
    string name = null;
    try
    {
        var parameter = spatial[i].get_Parameter(BuiltInParameter.ROOM_NAME);
        if (parameter != null) name = parameter.AsString();
    }
    catch (Exception) { name = null; }
    return string.Format("'{0} {1}'{2}", name ?? "", spatial[i].Number ?? "",
        spatialTo[i] == null ? "" : " in the link " + spatialFrom[i]);
};

// Fills one text parameter and reads it back. 0 written, 1 the name is carried
// by two or more parameters, 2 no writable text parameter by that name, 3 Revit
// did not keep the value, 4 the parameter is fine but there is no value to write.
Func<Element, string, string, int> fill = (element, target, value) =>
{
    IList<Parameter> carriers = null;
    try { carriers = element.GetParameters(target); } catch (Exception) { carriers = null; }
    if (carriers != null && carriers.Count > 1) return 1;
    var parameter = carriers != null && carriers.Count == 1 ? carriers[0] : null;
    if (parameter == null || parameter.IsReadOnly || parameter.StorageType != StorageType.String) return 2;
    if (string.IsNullOrEmpty(value)) return 4;
    try { parameter.Set(value); } catch (Exception) { return 3; }
    return string.Equals(parameter.AsString() ?? "", value, StringComparison.Ordinal) ? 0 : 3;
};

// Picks among containers that passed the storey check: one the element is
// INSIDE (kind 0) beats one it is above (kind 1); between two above, the nearer
// top (smaller gap). Two equally good: -2, written by nobody. None: -1.
Func<List<int>, List<int>, List<double>, int> pick = (which, kind, gap) =>
{
    if (which.Count == 0) return -1;
    var best = -1;
    var tie = false;
    for (var k = 0; k < which.Count; k++)
    {
        if (best < 0) { best = k; tie = false; continue; }
        if (kind[k] < kind[best] || (kind[k] == kind[best] && gap[k] < gap[best] - tolerance))
        {
            best = k;
            tie = false;
        }
        else if (kind[k] == kind[best] && Math.Abs(gap[k] - gap[best]) <= tolerance)
        {
            tie = true;
        }
    }
    return tie ? -2 : which[best];
};

var nameWritten = 0;
var numberWritten = 0;
var levelWritten = 0;
var missingRoomParameter = 0;
var missingNumberParameter = 0;
var missingLevelParameter = 0;
var ambiguousRoomParameter = 0;
var ambiguousNumberParameter = 0;
var ambiguousLevelParameter = 0;
var notKept = 0;
var containerWithoutValue = 0;
var settledByStorey = 0;
var overlapping = new List<ElementId>();
var notOnItsStorey = new List<string>();
var notOnItsStoreyCount = 0;
var testFailed = 0;
var noLevelFound = 0;
var samples = new List<string>();
var landedInHost = 0;
var landedInLink = 0;
var hostOverLink = 0;
var landedPerLink = new Dictionary<string, int>();

if (nameTarget.Length == 0 && numberTarget.Length == 0 && levelTarget.Length == 0)
{
    refused.Add("Nothing was named to fill - roomParameter, numberParameter and levelParameter were all "
        + "left out, so NOTHING was written. Name at least one parameter");
}
else
{
    if (spatial.Count == 0 && (nameTarget.Length > 0 || numberTarget.Length > 0))
    {
        refused.Add(includeLinks
            ? string.Format("neither this model nor any loaded link has a placed and enclosed {0}, so "
                + "every element below comes back blank", label)
            : string.Format("this model has no placed and enclosed {0}S of its own. If the {1}s are in a "
                + "LINK, ask again with includeLinks - host only, nothing in a link was asked, and every "
                + "element below comes back blank", label, useSpaces ? "space" : "room"));
    }

    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        XYZ point = null;
        var location = element.Location as LocationPoint;
        if (location != null) point = location.Point;
        if (point == null)
        {
            BoundingBoxXYZ own = null;
            try { own = element.get_BoundingBox(null); } catch (Exception) { own = null; }
            if (own != null) point = (own.Min + own.Max) / 2.0;
        }
        if (point == null) continue;

        // Every container is asked - in plan, then in height - in its own
        // document. None is taken just for being first. The host's and the
        // links' are kept apart; the reasons a container in plan was turned
        // down are kept for the answer.
        var hostWhich = new List<int>(); var hostKind = new List<int>(); var hostGap = new List<double>();
        var linkWhich = new List<int>(); var linkKind = new List<int>(); var linkGap = new List<double>();
        var inPlanCount = 0;
        var turnedDown = "";
        if (nameTarget.Length > 0 || numberTarget.Length > 0)
        {
            for (var i = 0; i < spatial.Count; i++)
            {
                var local = spatialTo[i] == null ? point : spatialTo[i].OfPoint(point);
                var box = spatialBox[i];
                if (local.X < box.Min.X || local.X > box.Max.X) continue;
                if (local.Y < box.Min.Y || local.Y > box.Max.Y) continue;

                var asRoom = spatial[i] as Room;
                var asSpace = spatial[i] as Space;
                if (asRoom == null && asSpace == null) continue;

                var inside = false;
                var inPlan = false;
                var baseZ = box.Min.Z;
                try
                {
                    inside = asRoom != null ? asRoom.IsPointInRoom(local) : asSpace.IsPointInSpace(local);
                    inPlan = inside;
                    if (!inPlan)
                    {
                        var mid = new XYZ(local.X, local.Y, (box.Min.Z + box.Max.Z) / 2.0);
                        inPlan = asRoom != null ? asRoom.IsPointInRoom(mid) : asSpace.IsPointInSpace(mid);
                    }
                    var at = spatial[i].Location as LocationPoint;
                    if (at != null && at.Point != null)
                    {
                        baseZ = at.Point.Z;
                        if (!inPlan)
                        {
                            var level = new XYZ(local.X, local.Y, at.Point.Z);
                            inPlan = asRoom != null ? asRoom.IsPointInRoom(level) : asSpace.IsPointInSpace(level);
                        }
                    }
                }
                catch (Exception) { testFailed++; inside = false; inPlan = false; }

                if (!inPlan) continue;
                inPlanCount++;

                // In height. Inside: taken. Otherwise only above its floor and
                // below the top of its storey - see the header.
                var kind = -1;
                var gap = 0.0;
                var why = "";
                if (inside) { kind = 0; }
                else if (local.Z < box.Min.Z - tolerance) why = "it sits BELOW that " + label.ToLower() + "'s floor";
                else if (spatialStorey[i] < 0) why = "that " + label.ToLower() + " has no level above its own, so "
                    + "the top of its storey is not known";
                else if (local.Z >= Math.Max(baseZ + spatialStorey[i], box.Max.Z) - tolerance)
                    why = "it sits ABOVE that " + label.ToLower() + "'s storey - a higher floor";
                else { kind = 1; gap = Math.Max(0.0, local.Z - box.Max.Z); }

                if (kind < 0)
                {
                    if (turnedDown.Length == 0) turnedDown = "in plan of the " + label + " " + named(i) + ", but " + why;
                    continue;
                }
                if (spatialTo[i] == null) { hostWhich.Add(i); hostKind.Add(kind); hostGap.Add(gap); }
                else { linkWhich.Add(i); linkKind.Add(kind); linkGap.Add(gap); }
            }
        }

        // This model's own first; a linked one only where no host container
        // passed. A tie among the host's is not handed to the link.
        var chosen = pick(hostWhich, hostKind, hostGap);
        var fromHost = chosen != -1;
        if (!fromHost) chosen = pick(linkWhich, linkKind, linkGap);
        var tied = chosen == -2;

        string nameValue = null;
        string numberValue = null;
        if (tied)
        {
            overlapping.Add(element.Id);
            chosen = -1;
        }
        else if (chosen >= 0)
        {
            var container = spatial[chosen];
            var nameParameter = container.get_Parameter(BuiltInParameter.ROOM_NAME);
            if (nameParameter != null) nameValue = nameParameter.AsString();
            numberValue = container.Number;
            if (inPlanCount > 1) settledByStorey++;

            if (spatialTo[chosen] == null) landedInHost++;
            else
            {
                landedInLink++;
                var from = spatialFrom[chosen];
                int already;
                landedPerLink[from] = landedPerLink.TryGetValue(from, out already) ? already + 1 : 1;
            }
        }
        else if (nameTarget.Length > 0 || numberTarget.Length > 0)
        {
            noRoomFound.Add(element.Id);
            if (turnedDown.Length > 0)
            {
                notOnItsStoreyCount++;
                if (notOnItsStorey.Count < 20) notOnItsStorey.Add(element.Id + ": " + turnedDown);
            }
        }

        var touched = false;
        var wroteName = false;
        var wroteNumber = false;

        if (nameTarget.Length > 0)
        {
            var outcome = fill(element, nameTarget, chosen >= 0 ? nameValue : null);
            if (outcome == 0) { nameWritten++; touched = true; wroteName = true; }
            else if (outcome == 1) ambiguousRoomParameter++;
            else if (outcome == 2) missingRoomParameter++;
            else if (outcome == 3) notKept++;
            else if (chosen >= 0) containerWithoutValue++;
        }

        if (numberTarget.Length > 0)
        {
            var outcome = fill(element, numberTarget, chosen >= 0 ? numberValue : null);
            if (outcome == 0) { numberWritten++; touched = true; wroteNumber = true; }
            else if (outcome == 1) ambiguousNumberParameter++;
            else if (outcome == 2) missingNumberParameter++;
            else if (outcome == 3) notKept++;
            else if (chosen >= 0) containerWithoutValue++;
        }

        if (levelTarget.Length > 0)
        {
            var levelId = element.LevelId;
            var level = levelId == null || levelId == ElementId.InvalidElementId ? null : doc.GetElement(levelId);
            var outcome = fill(element, levelTarget, level != null ? level.Name : null);
            if (outcome == 0) { levelWritten++; touched = true; }
            else if (outcome == 1) ambiguousLevelParameter++;
            else if (outcome == 2) missingLevelParameter++;
            else if (outcome == 3) notKept++;
            else noLevelFound++;
        }

        if (touched) written++;
        // Counted only where this model's own was really written over a
        // linked one that also passed - never on a tie or a failed write.
        if (fromHost && chosen >= 0 && linkWhich.Count > 0 && (wroteName || wroteNumber)) hostOverLink++;
        if ((wroteName || wroteNumber) && samples.Count < 10)
        {
            samples.Add(string.Format("{0}: name \"{1}\" | number \"{2}\"{3}", element.Id,
                wroteName ? nameValue : "(not written)", wroteNumber ? numberValue : "(not written)",
                spatialTo[chosen] == null ? "" : " - from the link " + spatialFrom[chosen]));
        }
    }
}

if (missingRoomParameter > 0)
{
    refused.Add(string.Format("{0} element(s) have no writable text parameter '{1}'. This fills parameters; "
        + "it does not create them - bind a project or shared TEXT parameter first",
        missingRoomParameter, nameTarget));
}
if (missingNumberParameter > 0)
{
    refused.Add(string.Format("{0} element(s) have no writable text parameter '{1}'",
        missingNumberParameter, numberTarget));
}
if (missingLevelParameter > 0)
{
    refused.Add(string.Format("{0} element(s) have no writable text parameter '{1}'",
        missingLevelParameter, levelTarget));
}
if (ambiguousRoomParameter > 0)
{
    refused.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}' - usually a "
        + "shared or project parameter bound beside a built-in one of the same name. Revit would pick "
        + "one of them at random, so NEITHER was filled", ambiguousRoomParameter, nameTarget));
}
if (ambiguousNumberParameter > 0)
{
    refused.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}', so NEITHER "
        + "was filled", ambiguousNumberParameter, numberTarget));
}
if (ambiguousLevelParameter > 0)
{
    refused.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}', so NEITHER "
        + "was filled", ambiguousLevelParameter, levelTarget));
}
if (notKept > 0)
{
    refused.Add(string.Format("{0} value(s) were written and did NOT read back as written - Revit did "
        + "not keep them", notKept));
}
if (containerWithoutValue > 0)
{
    refused.Add(string.Format("{0} value(s) were not written because the {1} the element sits in has "
        + "no name or no number - the field was left as it was", containerWithoutValue, label));
}
if (noRoomFound.Count > 0)
{
    refused.Add(string.Format("{0} element(s) were given no {1} and were not written - {2} of them sit "
        + "in plan of one on another storey or below its floor (named below), the rest in plan of none",
        noRoomFound.Count, label, notOnItsStoreyCount));
}
if (notOnItsStoreyCount > 0)
{
    refused.Add(string.Format("NOT WRITTEN, rather than written with another floor's {0}: {1}{2}", label,
        string.Join("; ", notOnItsStorey),
        notOnItsStoreyCount > notOnItsStorey.Count
            ? string.Format("; and {0} more", notOnItsStoreyCount - notOnItsStorey.Count) : ""));
}
if (overlapping.Count > 0)
{
    refused.Add(string.Format("{0} element(s) sit where two {1}S hold them equally - overlapping, or "
        + "equally near - NOT written, rather than written with a guess: {2}", overlapping.Count, label,
        string.Join(", ", overlapping.Take(20).Select(id => id.ToString()))));
}
if (settledByStorey > 0)
{
    refused.Add(string.Format("{0} element(s) were in plan of more than one {1} - another storey's - and "
        + "were given the one on their own storey", settledByStorey, label));
}
if (noLevelFound > 0)
{
    refused.Add(string.Format("{0} element(s) carry no level Revit gives as theirs (Element.LevelId), so "
        + "'{1}' was left as it was on them", noLevelFound, levelTarget));
}
if (testFailed > 0)
{
    refused.Add(string.Format("Revit could not answer the containment test {0} time(s); those {1}S were "
        + "taken as NOT containing the element", testFailed, label));
}
if (skippedContainers > 0)
{
    refused.Add(string.Format("{0} {1}(S) in this model are UNPLACED or UNENCLOSED and were not asked",
        skippedContainers, label));
}
if (hostOverLink > 0)
{
    refused.Add(string.Format("{0} element(s) sit in one of this model's own {1}S AND in a linked one - "
        + "this model's own was written, never the link's", hostOverLink, label));
}

// THE ANSWER SAYS WHAT IT READ - D-59's three cases read differently on purpose.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} element(s) given a linked {2}, {3} one "
        + "of this model's own", linksSearched, landedInLink, label, landedInHost));
foreach (var pair in landedPerLink)
    linkedMatches.Add(string.Format("{0}: {1} element(s) given a {2} from it", pair.Key, pair.Value, label));
if (linkedSkipped > 0)
    linkedMatches.Add(string.Format("{0} linked {1}(S) are UNPLACED or UNENCLOSED and were not asked",
        linkedSkipped, label));
if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT read",
        nestedLinks));
if (linksSearched > 0)
    linkedMatches.Add("Linked rooms and spaces are only READ - every element written is this model's own");

refused.Add(string.Format("{0} element(s) written, using {1}S - name {2}, number {3}, level {4}; "
    + "{5} given one of this model's own {1}S, {6} a linked one. Architecture puts people in ROOMS and "
    + "MEP calculates in SPACES; a model can hold both in the same place with different names, and "
    + "picking the wrong one is the commonest way a location register comes out wrong", written, label,
    nameWritten, numberWritten, levelWritten, landedInHost, landedInLink));
