// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `tagTypeId` are in
// scope, and leaves `tagged`, `newTags`, `mismatched`, `alreadyTaggedHere`,
// `notRooms`, `unplaced`, `notEnclosed`, `otherLevel`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One plan of room tags, one
// undo.
//
// A ROOM TAG IS NOT THE TAG EVERY OTHER CATEGORY TAKES. Revit's room tag is an
// element of its own, made by the document's own room-tag call, and that call
// is the one used here. TAG_ELEMENTS makes the other kind: it DID tag rooms on
// 2026-09-23 ("tagged 5, refused 0", counted afterwards as IndependentTags),
// and whether those act as room tags has not been checked.
//
// THE ROOMS ARE HANDED IN, NEVER LOOKED FOR IN THE VIEW. A collector scoped to
// a view returns only what Visibility/Graphics shows, and mechanical
// templates hide Rooms - on 2026-09-19 one found 0 of a model's 7 rooms in
// every view. The fragment before collects them from the whole model.
//
// THE SAME RULE FOR THE ROOM TAGS ALREADY HERE. They are read from the whole
// model and kept when this plan owns them, so a tag the plan HIDES still
// counts, and running this twice cannot stack a second tag on a room. The
// duplicate would sit exactly on the first and nobody would see it until one
// was dragged.
//
// THE TAG TYPE IS THE CALLER'S AND IS NEVER GUESSED (D-33). It is checked for
// being a ROOM tag type before anything is placed, so a wrong type costs one
// sentence and no cleanup.
//
// EVERY TAG IS READ BACK - its type and the room it points at - and one that
// is wrong is removed again inside the same undo. A count of calls that
// returned is not evidence that the drawing is right.
//
// LINKED ROOMS ARE NOT TAGGED HERE. A room in the architect's link needs the
// link instance as well as the room, and nothing hands that pair in yet; a
// room element from another document is refused rather than tagged by an id
// that means something else in this one.

var tagged = 0;
var newTags = new List<ElementId>();
var mismatched = new List<ElementId>();
var alreadyTaggedHere = new List<ElementId>();
var notRooms = new List<ElementId>();
var unplaced = new List<ElementId>();
var notEnclosed = new List<ElementId>();
var otherLevel = new List<ElementId>();
var refused = new List<ElementId>();
var findings = new List<string>();

var roomTagsCategory = new ElementId(BuiltInCategory.OST_RoomTags);

// How a type reads in Revit's own Properties palette - "M_Room Tag: Room Tag
// With Area" - so a refusal names the thing the modeller can see.
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// ---- refused before anything is placed -----------------------------------
//
// Each of these is a request that cannot be right for ANY room, so it stops
// the whole call with one sentence rather than failing room by room. A throw
// rolls the call back, so nothing is kept.

if (view.IsTemplate)
    throw new InvalidOperationException("\"" + view.Name + "\" is a view template, and a "
        + "template holds no tags. Name the plan itself. Nothing was placed.");

var planLevel = view.GenLevel;
if (planLevel == null)
    throw new InvalidOperationException("\"" + view.Name + "\" is not a plan of one level "
        + "(Revit calls it a " + view.ViewType + " view). Room tags from this go in the floor "
        + "or ceiling plan of the rooms' own level. Nothing was placed.");

if (tagTypeId == null || tagTypeId == ElementId.InvalidElementId)
    throw new InvalidOperationException("No room tag type was named, and one is never "
        + "guessed - \"Room Tag\", \"Room Tag With Area\" and \"Room Tag With Volume\" print "
        + "different drawings. Name the one to use. Nothing was placed.");

var tagType = doc.GetElement(tagTypeId) as ElementType;
var typeName = typeLabel(tagType);
if (tagType == null || tagType.Category == null || tagType.Category.Id != roomTagsCategory)
    throw new InvalidOperationException("\"" + typeName + "\" is "
        + (tagType == null || tagType.Category == null
              ? "not a tag type"
              : "a " + tagType.Category.Name + " type")
        + ", not a Room Tags type, so it cannot tag a room. Name a room tag type, like "
        + "\"Room Tag With Area\". Nothing was placed.");

// A family type must be active before Revit places it. Tag types normally
// are; activating one that is not changes nothing else. TAG_ELEMENTS_IN_VIEW
// does the same.
var tagSymbol = tagType as FamilySymbol;
if (tagSymbol != null && !tagSymbol.IsActive) tagSymbol.Activate();

// ---- the room tags this plan already shows ----------------------------------
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
// each tag.
var roomsTaggedHere = new HashSet<ElementId>();
foreach (var existing in new FilteredElementCollector(doc)
                             .OfCategory(BuiltInCategory.OST_RoomTags)
                             .WhereElementIsNotElementType())
{
    var existingTag = existing as RoomTag;
    if (existingTag == null || !viewsSharingTags.Contains(existingTag.OwnerViewId)) continue;

    // The LOCAL room only. A tag on a room in a link carries no local room,
    // and this fragment never tags linked rooms, so it cannot collide with one.
    ElementId existingRoomId = null;
    try { existingRoomId = existingTag.TaggedLocalRoomId; } catch { }
    if (existingRoomId != null && existingRoomId != ElementId.InvalidElementId)
        roomsTaggedHere.Add(existingRoomId);
}

// Placed and not SHOWN is the quiet failure here: a plan whose Visibility/
// Graphics (or template) turns Room Tags off takes every tag and draws none,
// and the modeller sees an unchanged drawing. Said in `findings`, not refused
// - the tags are real and a template change shows them.
bool roomTagsHiddenHere = false;
try { roomTagsHiddenHere = view.GetCategoryHidden(roomTagsCategory); } catch { }

// Revit's own words for the first room it would not tag, so a refusal can be
// acted on rather than guessed at.
string firstRefusal = null;

foreach (var element in elements)
{
    if (element == null) continue;

    var room = element as Room;
    if (room == null)
    {
        notRooms.Add(element.Id);
        continue;
    }

    // A ROOM FROM ANOTHER MODEL. Its id means an element of THAT model, and
    // tagging by it here would tag whatever this model holds under the same
    // number, if anything.
    bool inThisModel = false;
    try { inThisModel = room.Document != null && room.Document.Equals(doc); } catch { }
    if (!inThisModel)
    {
        refused.Add(room.Id);
        continue;
    }

    // Also what stops a room handed in twice from getting two tags: it joins
    // this set the moment its first tag is made.
    if (roomsTaggedHere.Contains(room.Id))
    {
        alreadyTaggedHere.Add(room.Id);
        continue;
    }

    // NOT PLACED - in the schedule, in no plan. Nowhere to put a tag.
    var placedAt = room.Location as LocationPoint;
    if (placedAt == null || placedAt.Point == null)
    {
        unplaced.Add(room.Id);
        continue;
    }

    // NO AREA - not enclosed by walls or separation lines, or redundant with
    // another room in the same space. Revit gives both an area of zero.
    double area = 0;
    try { area = room.Area; } catch { }
    if (area <= 0)
    {
        notEnclosed.Add(room.Id);
        continue;
    }

    // ANOTHER LEVEL. A plan of Level 1 is the drawing of Level 1's rooms; a
    // room on Level 2 is left for Level 2's plan rather than tagged where it
    // is not drawn.
    if (room.LevelId != planLevel.Id)
    {
        otherLevel.Add(room.Id);
        continue;
    }

    // AT THE ROOM'S OWN REFERENCE POINT - where the room was placed. Laying
    // tags out is another job; CENTER_ROOM_TAGS puts them in the middle.
    RoomTag made = null;
    try
    {
        made = doc.Create.NewRoomTag(new LinkElementId(room.Id),
                                     new UV(placedAt.Point.X, placedAt.Point.Y),
                                     view.Id);
    }
    catch (Exception failure)
    {
        if (firstRefusal == null) firstRefusal = failure.Message;
    }

    if (made == null)
    {
        refused.Add(room.Id);
        continue;
    }

    // THE TYPE ASKED FOR, SET AND THEN READ BACK. The call makes the
    // project's default room tag; which type it carries is only known by
    // asking it afterwards.
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
        refused.Add(room.Id);
        continue;
    }

    // AND THE ROOM IT POINTS AT, READ BACK. A tag on the wrong room still
    // prints a confident name.
    ElementId pointsAt = ElementId.InvalidElementId;
    try { pointsAt = made.TaggedLocalRoomId; } catch { }

    if (pointsAt != room.Id)
    {
        try { doc.Delete(made.Id); } catch { }
        mismatched.Add(room.Id);
        continue;
    }

    newTags.Add(made.Id);
    roomsTaggedHere.Add(room.Id);
    tagged++;
}

// THE WHOLE ANSWER AS ONE LINE, because a reply shows a list's first three
// entries and its count - this line carries every count in words.
findings.Add(tagged + " room tag(s) placed in \"" + view.Name + "\" as \"" + typeName + "\". "
    + "Not tagged: " + alreadyTaggedHere.Count + " already tagged here, "
    + notRooms.Count + " not rooms, " + unplaced.Count + " not placed, "
    + notEnclosed.Count + " not enclosed, " + otherLevel.Count + " on another level, "
    + refused.Count + " refused, " + mismatched.Count + " read back on the wrong room "
    + "and removed.");

var cautions = new List<string>();
if (roomTagsHiddenHere && tagged > 0)
    cautions.Add("Room Tags are turned off in \"" + view.Name + "\" - in its Visibility/Graphics "
        + "or its view template - so the " + tagged + " tag(s) placed are in the model and not "
        + "shown in that view.");
if (firstRefusal != null)
    cautions.Add("Revit's first refusal said: " + firstRefusal);
if (cautions.Count > 0)
    findings.Add(string.Join(" ", cautions));
