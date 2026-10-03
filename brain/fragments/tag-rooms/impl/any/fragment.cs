// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `tagTypeId` are in
// scope, and leaves `tagged`, `newTags`, `mismatched`, `alreadyTaggedHere`,
// `notTaggable`, `unplaced`, `notEnclosed`, `otherLevel`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One plan of tags, one undo.
//
// ROOMS, MEP SPACES AND HVAC ZONES, AND THE TAG TYPE DECIDES WHICH. A Room
// Tags type tags the rooms handed in, a Space Tags type the spaces, a Zone
// Tags type the zones. Rooms and spaces get Revit's own tag elements, each
// made by the document's own call for it - NewRoomTag, NewSpaceTag - and NOT
// the tag every other category takes. TAG_ELEMENTS makes that other kind: it
// DID tag rooms on 2026-09-23 ("tagged 5, refused 0", counted afterwards as
// IndependentTags), and whether those act as room tags has not been checked.
// Its category map has no spaces and no zones.
//
// A ZONE HAS NO TAG CALL OF ITS OWN. Its tag IS the other kind - an
// IndependentTag made on a reference to the zone, with the Zone Tags type.
// Measured 2026-10-03 on Project2 (Revit 2024), rolled back: that call made a
// Zone Tags tag on "L1 - West Wing" that read back pointing at that zone and
// showing its name. With no Zone Tags type loaded, the same call refuses:
// "There is no loaded tag type that can be used".
//
// THE ROOMS, SPACES AND ZONES ARE HANDED IN, NEVER LOOKED FOR IN THE VIEW. A
// collector scoped to a view returns only what Visibility/Graphics shows, and
// mechanical templates hide Rooms - on 2026-09-19 one found 0 of a model's 7
// rooms in every view. The fragment before collects them from the whole model.
//
// THE SAME RULE FOR THE TAGS ALREADY HERE. They are read from the whole model
// and kept when this plan owns them, so a tag the plan HIDES still counts, and
// running this twice cannot stack a second tag on anything. The duplicate
// would sit exactly on the first and nobody would see it until one was
// dragged.
//
// THE TAG TYPE IS THE CALLER'S AND IS NEVER GUESSED (D-33). It is checked for
// being a Room Tags, Space Tags or Zone Tags type before anything is placed,
// so a wrong type costs one sentence and no cleanup.
//
// EVERY TAG IS READ BACK - its type and what it points at - and one that is
// wrong is removed again inside the same undo. A count of calls that returned
// is not evidence that the drawing is right.
//
// LINKED ROOMS, SPACES AND ZONES ARE NOT TAGGED HERE. One in the architect's
// link needs the link instance as well, and nothing hands that pair in yet;
// an element from another document is refused rather than tagged by an id
// that means something else in this one.

var tagged = 0;
var newTags = new List<ElementId>();
var mismatched = new List<ElementId>();
var alreadyTaggedHere = new List<ElementId>();
var notTaggable = new List<ElementId>();
var unplaced = new List<ElementId>();
var notEnclosed = new List<ElementId>();
var otherLevel = new List<ElementId>();
var refused = new List<ElementId>();
var findings = new List<string>();

var roomTagsCategory = new ElementId(BuiltInCategory.OST_RoomTags);
var spaceTagsCategory = new ElementId(BuiltInCategory.OST_MEPSpaceTags);
var zoneTagsCategory = new ElementId(BuiltInCategory.OST_ZoneTags);

// How a type reads in Revit's own Properties palette - "M_Room Tag: Room Tag
// With Area" - so a refusal names the thing the modeller can see.
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// WHAT A ZONE TAG POINTS AT, on every release: the 2022-and-later method where
// Revit has it, the 2020/2021 property where it does not. Found by name on the
// tag, because the add-in compiles this with no release symbols, so an #if
// always takes its #else (row 5b-181). OFFSET_TAGS_FROM_HOST does the same.
Func<IndependentTag, List<ElementId>> zoneTagTargets = t =>
{
    var found = new List<ElementId>();
    var many = t.GetType().GetMethod("GetTaggedLocalElementIds", Type.EmptyTypes);
    if (many != null)
    {
        var all = many.Invoke(t, null) as IEnumerable<ElementId>;
        if (all != null) found.AddRange(all);
        return found;
    }
    var single = t.GetType().GetProperty("TaggedLocalElementId");
    var one = single == null ? null : single.GetValue(t, null) as ElementId;
    if (one != null) found.Add(one);
    return found;
};

// ---- refused before anything is placed -----------------------------------
//
// Each of these is a request that cannot be right for ANY room, space or zone,
// so it stops the whole call with one sentence rather than failing one by
// one. A throw rolls the call back, so nothing is kept.

if (view.IsTemplate)
    throw new InvalidOperationException("\"" + view.Name + "\" is a view template, and a "
        + "template holds no tags. Name the plan itself. Nothing was placed.");

var planLevel = view.GenLevel;
if (planLevel == null)
    throw new InvalidOperationException("\"" + view.Name + "\" is not a plan of one level "
        + "(Revit calls it a " + view.ViewType + " view). Room, space and zone tags from this "
        + "go in the floor or ceiling plan of their own level. Nothing was placed.");

if (tagTypeId == null || tagTypeId == ElementId.InvalidElementId)
    throw new InvalidOperationException("No room, space or zone tag type was named, and one "
        + "is never guessed - \"Room Tag\", \"Room Tag With Area\" and an office's own space "
        + "or zone tag print different drawings. Name the one to use. Nothing was placed.");

var tagType = doc.GetElement(tagTypeId) as ElementType;
var typeName = typeLabel(tagType);
var typeCategoryId = tagType != null && tagType.Category != null
    ? tagType.Category.Id : ElementId.InvalidElementId;

// THE TYPE'S CATEGORY SAYS WHAT IS TAGGED. One call tags one kind: a Room
// Tags type tags rooms, a Space Tags type spaces, a Zone Tags type zones, and
// whatever else is handed in lands in `notTaggable`.
bool forSpaces = typeCategoryId == spaceTagsCategory;
bool forZones = typeCategoryId == zoneTagsCategory;
if (!forSpaces && !forZones && typeCategoryId != roomTagsCategory)
    throw new InvalidOperationException("\"" + typeName + "\" is "
        + (tagType == null || tagType.Category == null
              ? "not a tag type"
              : "a " + tagType.Category.Name + " type")
        + ", not a Room Tags, Space Tags or Zone Tags type, so it cannot tag a room, a space "
        + "or a zone. Name a room tag type, like \"Room Tag With Area\", or a space or zone "
        + "tag type. Nothing was placed.");

var kindWord = forZones ? "zone" : forSpaces ? "space" : "room";
var tagsCategory = forZones ? zoneTagsCategory : forSpaces ? spaceTagsCategory : roomTagsCategory;
var tagsCategoryWord = forZones ? "Zone Tags" : forSpaces ? "Space Tags" : "Room Tags";

// A family type must be active before Revit places it. Tag types normally
// are; activating one that is not changes nothing else. TAG_ELEMENTS_IN_VIEW
// does the same.
var tagSymbol = tagType as FamilySymbol;
if (tagSymbol != null && !tagSymbol.IsActive) tagSymbol.Activate();

// ---- the tags this plan already shows -----------------------------------------
//
// A DEPENDENT VIEW SHARES ITS ANNOTATION with its primary view and with the
// primary's other dependents - a tag placed in one shows in all of them - so
// a room tagged in any of them is already tagged in this one. Believed from
// how Revit treats dependent views, and NOT yet seen through this fragment on
// a model; NEEDS-CHECKING says so. On an ordinary plan the set is the plan
// alone, and nothing below depends on this being right for one.
var viewsSharingTags = new HashSet<ElementId>();
viewsSharingTags.Add(view.Id);
try
{
    foreach (var dependentId in view.GetDependentViewIds()) viewsSharingTags.Add(dependentId);

    var primaryId = view.GetPrimaryViewId();
    if (primaryId != null && primaryId != ElementId.InvalidElementId)
    {
        viewsSharingTags.Add(primaryId);
        var primary = doc.GetElement(primaryId) as View;
        if (primary != null)
            foreach (var siblingId in primary.GetDependentViewIds()) viewsSharingTags.Add(siblingId);
    }
}
catch { }

// BY CATEGORY OVER THE WHOLE MODEL, and both halves of that matter. Revit's
// class filter refuses the room tag class outright - it exists in the API and
// not in Revit's own object model - and a collector scoped to the view would
// miss exactly the tags this plan hides. The owning view is then read off
// each tag. Space and zone tags are collected the same way, each by its own
// category.
var taggedHere = new HashSet<ElementId>();
foreach (var existing in new FilteredElementCollector(doc)
                             .OfCategoryId(tagsCategory)
                             .WhereElementIsNotElementType())
{
    if (!viewsSharingTags.Contains(existing.OwnerViewId)) continue;

    // The LOCAL room, space or zone only. A tag on one in a link carries no
    // local element, and this fragment never tags linked ones, so it cannot
    // collide.
    var existingTargets = new List<ElementId>();
    try
    {
        var existingRoomTag = existing as RoomTag;
        if (existingRoomTag != null) existingTargets.Add(existingRoomTag.TaggedLocalRoomId);
        var existingSpaceTag = existing as SpaceTag;
        if (existingSpaceTag != null && existingSpaceTag.Space != null)
            existingTargets.Add(existingSpaceTag.Space.Id);
        var existingZoneTag = existing as IndependentTag;
        if (existingZoneTag != null) existingTargets.AddRange(zoneTagTargets(existingZoneTag));
    }
    catch { }
    foreach (var existingTargetId in existingTargets)
        if (existingTargetId != null && existingTargetId != ElementId.InvalidElementId)
            taggedHere.Add(existingTargetId);
}

// Placed and not SHOWN is the quiet failure here: a plan whose Visibility/
// Graphics (or template) turns the tag category off takes every tag and draws
// none, and the modeller sees an unchanged drawing. Said in `findings`, not
// refused - the tags are real and a template change shows them.
bool tagsHiddenHere = false;
try { tagsHiddenHere = view.GetCategoryHidden(tagsCategory); } catch { }

// Revit's own words for the first one it would not tag, so a refusal can be
// acted on rather than guessed at.
string firstRefusal = null;

foreach (var element in elements)
{
    if (element == null) continue;

    // THE KIND THE TYPE TAGS, AND NOTHING ELSE. A space handed in with a room
    // tag type, a room with a zone tag type, a duct with any - all here.
    var room = forSpaces || forZones ? null : element as Room;
    var space = forSpaces ? element as Space : null;
    var zone = forZones ? element as Zone : null;
    Element target = forZones ? (Element)zone : forSpaces ? (Element)space : (Element)room;
    if (target == null)
    {
        notTaggable.Add(element.Id);
        continue;
    }

    // ONE FROM ANOTHER MODEL. Its id means an element of THAT model, and
    // tagging by it here would tag whatever this model holds under the same
    // number, if anything.
    bool inThisModel = false;
    try { inThisModel = target.Document != null && target.Document.Equals(doc); } catch { }
    if (!inThisModel)
    {
        refused.Add(target.Id);
        continue;
    }

    // Also what stops one handed in twice from getting two tags: it joins
    // this set the moment its first tag is made.
    if (taggedHere.Contains(target.Id))
    {
        alreadyTaggedHere.Add(target.Id);
        continue;
    }

    // WHERE THE TAG GOES, AND ON WHICH LEVEL. A room or space: its own
    // reference point and level. A zone has no point of its own - it is a
    // group of spaces - so its tag goes at the reference point of its LARGEST
    // space on this plan's level, which lies inside the zone by construction.
    // The Default zone, and any zone holding no spaces, has nowhere to go.
    XYZ point = null;
    ElementId levelId = ElementId.InvalidElementId;
    double area = 0;
    if (forZones)
    {
        // THE DEFAULT ZONE holds every space no zone was given, so a tag on it
        // would name "Default" across unzoned rooms. Its flag is read BY NAME:
        // Revit 2027 removed IsDefaultZone, which the compiler found, and the
        // add-in compiles with no release symbols for an #if (row 5b-181).
        bool isDefaultZone = false;
        try
        {
            var defaultFlag = zone.GetType().GetProperty("IsDefaultZone");
            if (defaultFlag != null) isDefaultZone = (bool)defaultFlag.GetValue(zone, null);
        }
        catch { }

        var zoneSpaces = new List<Space>();
        try
        {
            if (!isDefaultZone && zone.Spaces != null)
                foreach (Space member in zone.Spaces)
                    if (member != null) zoneSpaces.Add(member);
        }
        catch { }

        if (zoneSpaces.Count == 0)
        {
            unplaced.Add(zone.Id);
            continue;
        }

        // A zone's spaces share one level in Revit; read off the zone itself,
        // and off its spaces when the zone carries none.
        levelId = zone.LevelId;
        if (levelId == null || levelId == ElementId.InvalidElementId) levelId = zoneSpaces[0].LevelId;

        Space largest = null;
        foreach (var member in zoneSpaces)
        {
            if (member.LevelId != planLevel.Id) continue;
            if (!(member.Location is LocationPoint)) continue;
            double memberArea = 0;
            try { memberArea = member.Area; } catch { }
            if (memberArea <= 0) continue;
            area += memberArea;
            if (largest == null || memberArea > largest.Area) largest = member;
        }
        if (largest != null) point = ((LocationPoint)largest.Location).Point;
    }
    else
    {
        // NOT PLACED - in the schedule, in no plan. Nowhere to put a tag.
        var placedAt = target.Location as LocationPoint;
        if (placedAt == null || placedAt.Point == null)
        {
            unplaced.Add(target.Id);
            continue;
        }
        point = placedAt.Point;
        levelId = target.LevelId;
        try { area = ((SpatialElement)target).Area; } catch { }
    }

    // ANOTHER LEVEL. A plan of Level 1 is the drawing of Level 1's rooms,
    // spaces and zones; one on Level 2 is left for Level 2's plan rather than
    // tagged where it is not drawn.
    if (levelId != planLevel.Id)
    {
        otherLevel.Add(target.Id);
        continue;
    }

    // NO AREA - not enclosed by walls or separation lines, or redundant with
    // another in the same place. Revit gives both an area of zero. A zone
    // whose spaces on this level are all like that has no area here either.
    if (area <= 0 || point == null)
    {
        notEnclosed.Add(target.Id);
        continue;
    }

    // AT THAT POINT. Laying tags out is another job; CENTER_ROOM_TAGS puts
    // room tags in the middle.
    var at = new UV(point.X, point.Y);
    Element made = null;
    try
    {
        if (forZones)
            made = IndependentTag.Create(doc, tagTypeId, view.Id, new Reference(zone), false,
                                         TagOrientation.Horizontal, new XYZ(point.X, point.Y, 0));
        else if (forSpaces)
            made = doc.Create.NewSpaceTag(space, at, view);
        else
            made = doc.Create.NewRoomTag(new LinkElementId(room.Id), at, view.Id);
    }
    catch (Exception failure)
    {
        if (firstRefusal == null) firstRefusal = failure.Message;
    }

    if (made == null)
    {
        refused.Add(target.Id);
        continue;
    }

    // THE TYPE ASKED FOR, SET AND THEN READ BACK. The room and space calls
    // make the project's default tag of their kind; which type a tag carries
    // is only known by asking it afterwards.
    try
    {
        if (made.GetTypeId() != tagTypeId) made.ChangeTypeId(tagTypeId);
    }
    catch (Exception failure)
    {
        if (firstRefusal == null) firstRefusal = failure.Message;
    }

    if (made.GetTypeId() != tagTypeId)
    {
        // A tag of the wrong type is a drawing defect, and counting it is how
        // one reaches a printed sheet. Ours, made a moment ago - removed.
        try { doc.Delete(made.Id); } catch { }
        refused.Add(target.Id);
        continue;
    }

    // AND WHAT IT POINTS AT, READ BACK. A tag on the wrong room still prints
    // a confident name.
    bool pointsRight = false;
    try
    {
        var madeRoomTag = made as RoomTag;
        if (madeRoomTag != null) pointsRight = madeRoomTag.TaggedLocalRoomId == target.Id;
        var madeSpaceTag = made as SpaceTag;
        if (madeSpaceTag != null && madeSpaceTag.Space != null)
            pointsRight = madeSpaceTag.Space.Id == target.Id;
        var madeZoneTag = made as IndependentTag;
        if (madeZoneTag != null) pointsRight = zoneTagTargets(madeZoneTag).Contains(target.Id);
    }
    catch { }

    if (!pointsRight)
    {
        try { doc.Delete(made.Id); } catch { }
        mismatched.Add(target.Id);
        continue;
    }

    newTags.Add(made.Id);
    taggedHere.Add(target.Id);
    tagged++;
}

// THE WHOLE ANSWER AS ONE LINE, because a reply shows a list's first three
// entries and its count - this line carries every count in words.
findings.Add(tagged + " " + kindWord + " tag(s) placed in \"" + view.Name + "\" as \""
    + typeName + "\". Not tagged: " + alreadyTaggedHere.Count + " already tagged here, "
    + notTaggable.Count + " not " + kindWord + "s, " + unplaced.Count + " not placed"
    + (forZones ? " or holding no spaces" : "") + ", "
    + notEnclosed.Count + " not enclosed, " + otherLevel.Count + " on another level, "
    + refused.Count + " refused, " + mismatched.Count + " read back on the wrong "
    + kindWord + " and removed.");

var cautions = new List<string>();
if (tagsHiddenHere && tagged > 0)
    cautions.Add(tagsCategoryWord + " are turned off in \"" + view.Name + "\" - in its "
        + "Visibility/Graphics or its view template - so the " + tagged + " tag(s) placed are "
        + "in the model and not shown in that view.");
if (firstRefusal != null)
    cautions.Add("Revit's first refusal said: " + firstRefusal);
if (cautions.Count > 0)
    findings.Add(string.Join(" ", cautions));
