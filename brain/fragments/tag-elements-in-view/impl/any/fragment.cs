// NOT STANDALONE. Assumes `doc`, `elements`, `view`, `tagTypeHintId`,
// `tagTypeId`, `leader`, `minLengthMm` and `leaderOffsetMm` are in scope, and
// leaves `tagged`, `alreadyTagged`, `skipped`, `typeCorrections`,
// `notTagged`, `notTaggedWhy`, `tagTypesUsed`, `newTags`, `mismatched`,
// `unplaced`, `notEnclosed`, `otherLevel` and `findings` behind.
//
// VERSION 7 (2026-11-01) - TAG_ROOMS MERGED IN, on the owner's instruction.
// ROOMS, MEP SPACES AND HVAC ZONES ARE TAGGED HERE NOW, each with its OWN kind
// of tag through Revit's own call - NewRoomTag, NewSpaceTag, and for a zone an
// IndependentTag on a reference to the zone with a Zone Tags type - and never
// with an ordinary element tag (the 2026-09-23 failure). The tag type for one
// is the type named, which must be of that kind or the element is refused;
// else the hint of that kind; else the project's default for the kind -
// never the first one loaded. Placed only in a plan of the element's own
// level: `unplaced`, `notEnclosed` and `otherLevel` name the rest; a room
// already tagged in this plan - or a view sharing its tags - is skipped; each
// tag is read back for its type and its target, and one that is wrong is
// removed again (`mismatched`). The block is in the loop below, marked
// VERSION 7; everything else is version 6 exactly. `newTags` is every tag
// this placed, of either kind. An AREA is still refused.
//
// VERSION 6 (2026-11-01) - TAG_ELEMENTS MERGED IN, on the owner's
// instruction. What it had that this lacked:
//
//   AN EXACT TAG TYPE - `tagTypeId`. Given, that one type is used on EVERY
//   element whatever its category - a Multi-Category tag on a mixed set, or
//   one family chosen by hand - with no category map, no project default and
//   no hint. Revit says nothing when a tag of the wrong category is placed:
//   it places and prints blank, so the caller who names an exact type owns
//   that choice, as they did on TAG_ELEMENTS. Left out (none), version 5's
//   three routes choose per category exactly as before.
//
//   ALREADY TAGGED, TYPE-AWARE, with an exact type: an element is skipped
//   only when it already carries a tag OF THAT TYPE in this view. So "tag the
//   ducts again using this tag family" puts the new family on ducts that carry
//   another, and a re-run of the same type stacks nothing. Without an exact
//   type, ANY tag in the view still counts, as in version 5 - the safer rule,
//   because the type is this fragment's choice there, not the caller's.
//   TAG_ELEMENTS took the list of tagged ids as an input; this reads the
//   view's tags itself, so the list cannot be stale.
//
//   `skipped`, the ids of the elements left alone as already tagged, beside
//   the `alreadyTagged` count.
//
// KEPT FROM VERSION 5, THE SAFER RULE IN EACH CASE: a room, area or space is
// never given an independent tag in either mode (version 7 tags rooms, spaces
// and zones with their own tags, and refuses an area); a riser is skipped in both; a HINT of
// the wrong category is set aside for the category's default, and
// `tagTypesUsed` says so.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a whole view is one undo.
//
// THE THREE THINGS THIS CARRIES THAT THE API WILL NOT TELL YOU.
//
// 1. THE MEP CATEGORY -> TAG CATEGORY MAP IS A FIXED FACT. Nothing derives "a
//    duct is tagged by a duct tag" at run time. `ElementTypeGroup` carries no
//    per-MEP-category tag entries, so the map lives here. EVERY OTHER CATEGORY
//    is worked out from Revit's own category names (version 4, below) - the
//    map is checked first. Version 5 changed one thing on the map's path:
//    where a tag with no leader goes (5b-452, below).
//
// 2. THE TAG FAMILY COMES FROM THE PROJECT'S DEFAULT, NOT THE FIRST LOADED.
//    `GetDefaultFamilyTypeId` on the tag category returns what Revit's own
//    "Tag by Category" would use - the family this project standardised on.
//    Taking the first loaded symbol silently produces the wrong tag family on
//    every element, and the drawing still looks tagged. Order: the caller's
//    hint, then the project default, then first loaded - and the answer says
//    which, so a guess is never invisible.
//
// 3. `IndependentTag.Create` DOES NOT RELIABLY HONOUR THE TYPE PASSED TO IT,
//    AND IT THROWS NOTHING. Measured where this was re-authored from: 38 tags
//    created with an explicit type id all came out as the document's default
//    type. So the type is READ BACK after every creation and corrected, and the
//    corrections are COUNTED in the answer. Fixing it silently would hide an
//    API that is not doing what it was told, which is the thing worth knowing.

int tagged = 0;
int alreadyTagged = 0;
var skipped = new List<ElementId>();
int typeCorrections = 0;
int leadersDrawn = 0;
var verticalSkipped = new List<ElementId>();
var tooShort = new List<ElementId>();
int leadersNotDrawn = 0;
var notTagged = new List<ElementId>();

// WHY EACH ONE IN `notTagged` IS THERE, one line per reason with a count.
// Version 3 handed back bare ids: six doors came back "notTagged 6" with
// nothing to say that the fragment did not know doors at all, and the working
// route (FIND_UNTAGGED_ELEMENTS then TAG_ELEMENTS) had to be found by hand.
var whyCount = new Dictionary<string, int>();
Action<string> because = reason =>
{
    int seenBefore;
    whyCount.TryGetValue(reason, out seenBefore);
    whyCount[reason] = seenBefore + 1;
};

// LEADER SHAPE, and `none` is the default ON PURPOSE - addLeader false, the
// head on the element itself. Only a caller who asks for a leader gets a head
// moved off the element.
//
//   none      no leader at all
//   straight  one segment, element to tag head
//   L         a vertical off the element and a horizontal shoulder into the
//             head - what a modeller means by an L-shaped tag
//
// A LEADER NEEDS THE HEAD AWAY FROM THE ELEMENT. A leader from a point to
// itself is not a leader, so with one the head is offset from the element
// rather than placed on it as the no-leader path does.
//
// BENDING THE LEADER INTO AN L IS NOT THIS FRAGMENT'S JOB, and must not become
// it again. FORCE_TAG_LEADER_LSHAPE (FRG-VIEW-088) already does it and does it
// properly: it sets `LeaderEndCondition.Free` FIRST - without which Revit owns
// the leader's shape and setting an elbow is QUIETLY IGNORED, the call
// returning normally while nothing bends - and it reads the elbow back out
// before counting it. An earlier version of this file set an elbow here with
// the end still attached and counted the call returning as a bend: it reported
// 15 elbows drawn and put 15 straight diagonals on the drawing. Tag here, bend
// there. `l` is accepted as a spelling of `straight` so a caller asking for an
// L still gets a leader for that fragment to bend, rather than a refusal.
const double MmToFeet = 1.0 / 304.8;
var leaderMode = (leader ?? "none").Trim().ToLowerInvariant();
bool wantLeader = leaderMode == "l" || leaderMode == "straight";

// PAPER MILLIMETRES, so it is multiplied by the view scale. This is the house
// idiom - force-tag-leader-lshape line 42, stack-tags line 35,
// arrange-tags-to-view-edges line 41 all do it - and leaving it out is what
// put the first run's leaders right across the drawing: an unscaled 3000 mm is
// 60 mm on the sheet at 1:50. stack-tags calls the unscaled version "the
// commonest mistake in this whole subject".
//
// An unset double arrives as 0, and a zero offset puts the head on top of the
// element it is meant to point at. 8 paper mm is a working default, not a
// standard - a caller that cares passes leaderOffsetMm.
int scale = 1;
try { scale = Math.Max(1, view.Scale); } catch { }
double offsetFeet = (leaderOffsetMm > 0 ? leaderOffsetMm : 8.0) * MmToFeet * scale;

// Fixed map. Extended by adding a pair, never by guessing at run time.
var tagCategoryFor = new Dictionary<BuiltInCategory, BuiltInCategory>
{
    { BuiltInCategory.OST_DuctCurves,        BuiltInCategory.OST_DuctTags },
    { BuiltInCategory.OST_DuctFitting,       BuiltInCategory.OST_DuctFittingTags },
    { BuiltInCategory.OST_DuctAccessory,     BuiltInCategory.OST_DuctAccessoryTags },
    { BuiltInCategory.OST_FlexDuctCurves,    BuiltInCategory.OST_FlexDuctTags },
    { BuiltInCategory.OST_PipeCurves,        BuiltInCategory.OST_PipeTags },
    { BuiltInCategory.OST_PipeFitting,       BuiltInCategory.OST_PipeFittingTags },
    { BuiltInCategory.OST_PipeAccessory,     BuiltInCategory.OST_PipeAccessoryTags },
    { BuiltInCategory.OST_FlexPipeCurves,    BuiltInCategory.OST_FlexPipeTags },
    { BuiltInCategory.OST_DuctTerminal,      BuiltInCategory.OST_DuctTerminalTags },
    { BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_MechanicalEquipmentTags },
    { BuiltInCategory.OST_PlumbingFixtures,  BuiltInCategory.OST_PlumbingFixtureTags },
    { BuiltInCategory.OST_LightingFixtures,  BuiltInCategory.OST_LightingFixtureTags },
    { BuiltInCategory.OST_ElectricalEquipment, BuiltInCategory.OST_ElectricalEquipmentTags },
    { BuiltInCategory.OST_ElectricalFixtures, BuiltInCategory.OST_ElectricalFixtureTags },
    { BuiltInCategory.OST_CableTray,         BuiltInCategory.OST_CableTrayTags },
    { BuiltInCategory.OST_Conduit,           BuiltInCategory.OST_ConduitTags },
    { BuiltInCategory.OST_Sprinklers,        BuiltInCategory.OST_SprinklerTags },
};

// EVERY OTHER CATEGORY - doors, windows, walls, furniture, generic models,
// structure, casework and the rest - FROM REVIT'S OWN CATEGORY NAMES, not from
// a second hand list. A taggable category's tag category is named after it in
// the API: OST_Doors -> OST_DoorTags, OST_GenericModel -> OST_GenericModelTags,
// OST_Stairs -> OST_StairsTags, OST_Assemblies -> OST_AssemblyTags. So the
// category's own enum name is tried with "Tags" added, then singular with
// "Tags" added, and a name only counts when THIS document has that category
// and it is an ANNOTATION category. Enum names, not shown names, so a German
// or French Revit derives the same answer. The MEP map above stays first and
// unchanged, because OST_DuctCurves -> OST_DuctTags is exactly the kind of
// pair this rule cannot see.
//
// ROOMS, AREAS AND SPACES ARE NOT TAGGED BY THIS ROUTE. Their tags are a
// different kind of element (a room tag, not an independent tag) - version 7
// places them by the room route in the loop below, and an area is refused.
var derivedTagCategory = new Dictionary<ElementId, BuiltInCategory>();
Func<Category, BuiltInCategory> tagCategoryByName = elementCategory =>
{
    BuiltInCategory answer;
    if (derivedTagCategory.TryGetValue(elementCategory.Id, out answer)) return answer;
    answer = BuiltInCategory.INVALID;

    foreach (var enumName in Enum.GetNames(typeof(BuiltInCategory)))
    {
        BuiltInCategory ownCategory;
        if (!Enum.TryParse(enumName, false, out ownCategory)) continue;
        if (elementCategory.Id != new ElementId(ownCategory)) continue;

        var singular = enumName.EndsWith("ies", StringComparison.Ordinal)
            ? enumName.Substring(0, enumName.Length - 3) + "y"
            : enumName.EndsWith("s", StringComparison.Ordinal)
              && !enumName.EndsWith("ss", StringComparison.Ordinal)
                ? enumName.Substring(0, enumName.Length - 1)
                : enumName;

        foreach (var tagName in new[] { enumName + "Tags", singular + "Tags" })
        {
            BuiltInCategory tagCandidate;
            if (!Enum.TryParse(tagName, false, out tagCandidate)) continue;
            Category tagCategoryHere = null;
            try { tagCategoryHere = Category.GetCategory(doc, tagCandidate); } catch { }
            if (tagCategoryHere != null
                && tagCategoryHere.CategoryType == CategoryType.Annotation)
            {
                answer = tagCandidate;
                break;
            }
        }
        if (answer != BuiltInCategory.INVALID) break;
    }

    derivedTagCategory[elementCategory.Id] = answer;
    return answer;
};

// WHERE A TAG GOES, ON THE MEP MAP OR OFF IT: the element's own point -
// the middle of its location line, its insertion point, or, for an element
// Revit gives neither (a door that IS a curtain-wall panel is one), the centre
// of its extent in this view. TAG_ELEMENTS places at the location the same way,
// and refused a curtain-panel door for having no point; the extent is what
// lets one be tagged here.
Func<Element, XYZ> ownPoint = element =>
{
    var onCurve = element.Location as LocationCurve;
    if (onCurve != null && onCurve.Curve != null) return onCurve.Curve.Evaluate(0.5, true);
    var onPoint = element.Location as LocationPoint;
    if (onPoint != null) return onPoint.Point;
    BoundingBoxXYZ extent = null;
    try { extent = element.get_BoundingBox(view) ?? element.get_BoundingBox(null); } catch { }
    return extent == null ? null : (extent.Min + extent.Max) * 0.5;
};

// EVERY ELEMENT ALREADY TAGGED IN THIS VIEW, read once before anything is
// created. Running twice must be safe: a second tag stacked on the first makes
// a drawing that looks right on screen and prints wrong.
//
// THIS IS A REAL VERSION SPLIT WITH NO SINGLE EXPRESSION, and it was found by
// the compile gate rather than by reading. One tag became able to carry several
// hosts at 2022: `GetTaggedLocalElementIds` was added then and DOES NOT EXIST
// before it, while the singular `TaggedLocalElementId` it replaced is
// deprecated after. Either one alone is wrong across half this range, so the
// split is written out - the only place in this fragment where the release
// matters.
//
// AND WHICH TYPE EACH TAG IS (version 6), so an exact type can be judged
// against tags of that type alone.
var alreadyHasTag = new HashSet<ElementId>();
var tagTypesOn = new Dictionary<ElementId, HashSet<ElementId>>();
Action<ElementId, ElementId> heldTag = (hostId, typeId) =>
{
    if (hostId == null) return;
    alreadyHasTag.Add(hostId);
    HashSet<ElementId> types;
    if (!tagTypesOn.TryGetValue(hostId, out types))
    {
        types = new HashSet<ElementId>();
        tagTypesOn[hostId] = types;
    }
    if (typeId != null) types.Add(typeId);
};
foreach (var existing in new FilteredElementCollector(doc, view.Id)
                             .OfClass(typeof(IndependentTag))
                             .Cast<IndependentTag>())
{
    var existingType = existing.GetTypeId();
#if REVIT2020 || REVIT2021
    heldTag(existing.TaggedLocalElementId, existingType);
#else
    foreach (var hostId in existing.GetTaggedLocalElementIds())
        heldTag(hostId, existingType);
#endif
}

// THE EXACT TYPE (version 6, from TAG_ELEMENTS). `none` binds
// InvalidElementId, which is version 5's behaviour. A type given that is not
// an annotation family type - a door type typed by mistake - is refused for
// every element, never placed as if it were a tag.
FamilySymbol exactType = null;
string exactProblem = null;
if (tagTypeId != null && tagTypeId != ElementId.InvalidElementId)
{
    exactType = doc.GetElement(tagTypeId) as FamilySymbol;
    if (exactType == null || exactType.Category == null
        || exactType.Category.CategoryType != CategoryType.Annotation)
    {
        exactType = null;
        exactProblem = "the tag type given is not a tag family type - nothing was tagged with it";
    }
}

// Resolved once per tag category, not once per element - and WHICH of the
// three routes found it is kept, so `tagTypesUsed` can say so.
var typeForCategory = new Dictionary<BuiltInCategory, ElementId>();
var routeFor = new Dictionary<BuiltInCategory, string>();

// ---- VERSION 7: ROOMS, SPACES AND HVAC ZONES, from TAG_ROOMS -----------------
//
// A ROOM IS NEVER GIVEN AN ORDINARY ELEMENT TAG. A room tag is its own kind
// of element, made by the document's own call for it - NewRoomTag for a room,
// NewSpaceTag for a space. An IndependentTag on a room is the other kind: on
// 2026-09-23 TAG_ELEMENTS "tagged 5, refused 0" rooms in Project1's "1 - Mech"
// that way (revit_annotation counted 5 IndependentTags), and CENTER_ROOM_TAGS,
// STACK_TAGS and ARRANGE_TAGS work on the RoomTag kind. A ZONE has no tag call
// of its own; what tags one is IndependentTag.Create on a reference to the
// zone with a Zone Tags type - measured 2026-10-03 on Project2 (Revit 2024),
// rolled back, and proved by TAG_ROOMS the same day.
//
// THE TAG TYPE FOR A ROOM IS NAMED OR IS THE PROJECT'S DEFAULT, NEVER GUESSED
// (D-33). "Room Tag", "Room Tag With Area" and "Room Tag With Volume" print
// different drawings. So: `tagTypeId` if named - and it must be a Room Tags
// type for a room, Space Tags for a space, Zone Tags for a zone, or that
// element is REFUSED, never tagged with the type it was given; else the hint,
// when it is of that kind; else the project's own default for the kind. With
// none of them there is no tag - never the first one loaded.
//
// THE PLAN SAYS WHICH ROOMS. A room, space or zone is tagged only in a plan of
// its own level: one on another level is `otherLevel`, one never placed
// `unplaced`, one placed with no area `notEnclosed`. A view template, or a
// view that is not a plan of one level, tags none of them.
//
// ONE TAG PER ROOM PER VIEW. The room, space and zone tags already in this
// plan are read from the WHOLE MODEL by the view that owns them, never from
// what the view shows - a tag the view hides still counts - and a dependent
// view shares its tags with its primary and the primary's other dependents.
//
// EVERY TAG IS READ BACK: the type asked for, and the room, space or zone it
// points at. One that is wrong is removed again inside the same undo.
//
// LINKED ROOMS AND SPACES ARE NOT TAGGED HERE. One from another model is
// refused, never tagged by an id that means something else in this one.
var roomTagsCategory = new ElementId(BuiltInCategory.OST_RoomTags);
var spaceTagsCategory = new ElementId(BuiltInCategory.OST_MEPSpaceTags);
var zoneTagsCategory = new ElementId(BuiltInCategory.OST_ZoneTags);
var newTags = new List<ElementId>();
var mismatched = new List<ElementId>();
var unplaced = new List<ElementId>();
var notEnclosed = new List<ElementId>();
var otherLevel = new List<ElementId>();
var findings = new List<string>();
int spatialHanded = 0;
int spatialTagged = 0;
string firstSpatialRefusal = null;
var spatialCategoriesUsed = new HashSet<ElementId>();

// WHAT A ZONE TAG POINTS AT, on every release: the 2022-and-later method where
// Revit has it, the 2020/2021 property where it does not. Found by name on the
// tag, because the add-in compiles this with no release symbols, so an #if
// always takes its #else (row 5b-181). TAG_ROOMS did the same.
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

// How a type reads in Revit's own Properties palette - "M_Room Tag: Room Tag
// With Area" - so a refusal names the thing the modeller can see.
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

Level planLevel = null;
try { planLevel = view.GenLevel; } catch { }

// The views that share this plan's annotation: itself, its dependents, its
// primary and the primary's other dependents. A room tagged in any of them is
// already tagged in this one. Believed from how Revit treats dependent views,
// NOT yet seen through this card on a model - NEEDS-CHECKING says so.
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

// The rooms, spaces and zones ALREADY tagged in this plan - read once, from
// the whole model by CATEGORY (Revit's class filter refuses the room tag
// class), and only when one was handed in.
HashSet<ElementId> spatialTaggedHere = null;
Func<HashSet<ElementId>> readSpatialTags = () =>
{
    var held = new HashSet<ElementId>();
    foreach (var tagCategory in new[] { roomTagsCategory, spaceTagsCategory, zoneTagsCategory })
    {
        foreach (var existing in new FilteredElementCollector(doc)
                                     .OfCategoryId(tagCategory)
                                     .WhereElementIsNotElementType())
        {
            if (!viewsSharingTags.Contains(existing.OwnerViewId)) continue;
            var targets = new List<ElementId>();
            try
            {
                var existingRoomTag = existing as RoomTag;
                if (existingRoomTag != null) targets.Add(existingRoomTag.TaggedLocalRoomId);
                var existingSpaceTag = existing as SpaceTag;
                if (existingSpaceTag != null && existingSpaceTag.Space != null)
                    targets.Add(existingSpaceTag.Space.Id);
                var existingZoneTag = existing as IndependentTag;
                if (existingZoneTag != null) targets.AddRange(zoneTagTargets(existingZoneTag));
            }
            catch { }
            foreach (var targetId in targets)
                if (targetId != null && targetId != ElementId.InvalidElementId) held.Add(targetId);
        }
    }
    return held;
};

foreach (var element in elements)
{
    if (element == null || element.Category == null)
    {
        notTagged.Add(element == null ? ElementId.InvalidElementId : element.Id);
        because("no category - nothing to choose a tag by");
        continue;
    }

    if (exactProblem != null)
    {
        notTagged.Add(element.Id);
        because(exactProblem);
        continue;
    }

    // ---- VERSION 7: A ROOM, A SPACE OR AN HVAC ZONE takes its OWN tag -------
    var asRoom = element as Room;
    var asSpace = element as Space;
    var asZone = element as Zone;
    if (asRoom != null || asSpace != null || asZone != null)
    {
        spatialHanded++;
        var kindWord = asZone != null ? "zone" : asSpace != null ? "space" : "room";
        var kindTagCategory = asZone != null ? zoneTagsCategory
                            : asSpace != null ? spaceTagsCategory : roomTagsCategory;
        var kindTagBuiltIn = asZone != null ? BuiltInCategory.OST_ZoneTags
                           : asSpace != null ? BuiltInCategory.OST_MEPSpaceTags
                           : BuiltInCategory.OST_RoomTags;
        var kindTagWord = asZone != null ? "Zone Tags" : asSpace != null ? "Space Tags" : "Room Tags";
        var where = element.Category.Name;

        // ONE FROM ANOTHER MODEL. Its id means an element of THAT model.
        bool inThisModel = false;
        try { inThisModel = element.Document != null && element.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notTagged.Add(element.Id);
            because(where + ": from another model - a linked " + kindWord + " is not tagged here");
            continue;
        }

        if (view.IsTemplate || planLevel == null)
        {
            notTagged.Add(element.Id);
            because(where + ": \"" + view.Name + "\" is " + (view.IsTemplate ? "a view template"
                  : "not a plan of one level") + " - a " + kindWord + " tag goes in the floor or "
                  + "ceiling plan of its own level");
            continue;
        }

        // THE TAG TYPE: named, or the hint of this kind, or the project's own
        // default for this kind. A named type of ANOTHER kind is refused for
        // this element - never an ordinary element tag on a room.
        ElementId spatialTypeId = ElementId.InvalidElementId;
        string spatialRoute;
        if (exactType != null)
        {
            if (exactType.Category == null || exactType.Category.Id != kindTagCategory)
            {
                notTagged.Add(element.Id);
                because(where + ": \"" + typeLabel(exactType) + "\" is a "
                      + (exactType.Category == null ? "tag" : exactType.Category.Name)
                      + " type, not a " + kindTagWord + " type - a " + kindWord + " takes its own "
                      + "kind of tag and is never given another. Name a " + kindTagWord + " type");
                continue;
            }
            spatialTypeId = exactType.Id;
            spatialRoute = "the caller's exact type";
        }
        else
        {
            spatialRoute = "the caller's hint";
            if (tagTypeHintId != null && tagTypeHintId != ElementId.InvalidElementId)
            {
                var spatialHint = doc.GetElement(tagTypeHintId) as FamilySymbol;
                if (spatialHint != null && spatialHint.Category != null && spatialHint.Category.Id == kindTagCategory)
                    spatialTypeId = spatialHint.Id;
            }
            if (spatialTypeId == ElementId.InvalidElementId)
            {
                spatialRoute = "the project's default";
                try { spatialTypeId = doc.GetDefaultFamilyTypeId(kindTagCategory); }
                catch { spatialTypeId = ElementId.InvalidElementId; }
            }
            if (spatialTypeId == null || spatialTypeId == ElementId.InvalidElementId)
            {
                notTagged.Add(element.Id);
                because(where + ": no " + kindTagWord + " type was named and the project sets no "
                      + "default one - name the " + kindWord + " tag type to use. It is never "
                      + "guessed: \"Room Tag\" and \"Room Tag With Area\" print different drawings");
                continue;
            }
        }
        if (!typeForCategory.ContainsKey(kindTagBuiltIn))
        {
            typeForCategory[kindTagBuiltIn] = spatialTypeId;
            routeFor[kindTagBuiltIn] = spatialRoute;
        }

        if (spatialTaggedHere == null) spatialTaggedHere = readSpatialTags();
        if (spatialTaggedHere.Contains(element.Id))
        {
            alreadyTagged++;
            skipped.Add(element.Id);
            continue;
        }

        // WHERE THE TAG GOES, AND ON WHICH LEVEL. A room or space: its own
        // reference point and level. A zone has no point of its own - it is a
        // group of spaces - so its tag goes at the reference point of its
        // LARGEST space on this plan's level, which lies inside the zone by
        // construction. The Default zone, and a zone holding no spaces, has
        // nowhere to go.
        XYZ point = null;
        ElementId levelId = ElementId.InvalidElementId;
        double area = 0;
        if (asZone != null)
        {
            // THE DEFAULT ZONE holds every space no zone was given. Its flag is
            // read BY NAME: Revit 2027 removed IsDefaultZone (row 5b-181).
            bool isDefaultZone = false;
            try
            {
                var defaultFlag = asZone.GetType().GetProperty("IsDefaultZone");
                if (defaultFlag != null) isDefaultZone = (bool)defaultFlag.GetValue(asZone, null);
            }
            catch { }

            var zoneSpaces = new List<Space>();
            try
            {
                if (!isDefaultZone && asZone.Spaces != null)
                    foreach (Space member in asZone.Spaces)
                        if (member != null) zoneSpaces.Add(member);
            }
            catch { }

            if (zoneSpaces.Count == 0)
            {
                unplaced.Add(element.Id);
                continue;
            }

            levelId = asZone.LevelId;
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
            var placedAt = element.Location as LocationPoint;
            if (placedAt == null || placedAt.Point == null)
            {
                unplaced.Add(element.Id);
                continue;
            }
            point = placedAt.Point;
            levelId = element.LevelId;
            try { area = ((SpatialElement)element).Area; } catch { }
        }

        // ANOTHER LEVEL - left for that level's plan rather than tagged where
        // it is not drawn.
        if (levelId != planLevel.Id)
        {
            otherLevel.Add(element.Id);
            continue;
        }

        // NO AREA - not enclosed by walls or separation lines, or redundant.
        if (area <= 0 || point == null)
        {
            notEnclosed.Add(element.Id);
            continue;
        }

        var spatialSymbol = doc.GetElement(spatialTypeId) as FamilySymbol;
        if (spatialSymbol != null && !spatialSymbol.IsActive) spatialSymbol.Activate();

        // AT THAT POINT, by the document's own call for the kind.
        var at = new UV(point.X, point.Y);
        Element spatialMade = null;
        try
        {
            if (asZone != null)
                spatialMade = IndependentTag.Create(doc, spatialTypeId, view.Id, new Reference(asZone), false,
                                             TagOrientation.Horizontal, new XYZ(point.X, point.Y, 0));
            else if (asSpace != null)
                spatialMade = doc.Create.NewSpaceTag(asSpace, at, view);
            else
                spatialMade = doc.Create.NewRoomTag(new LinkElementId(asRoom.Id), at, view.Id);
        }
        catch (Exception failure)
        {
            if (firstSpatialRefusal == null) firstSpatialRefusal = failure.Message;
        }

        if (spatialMade == null)
        {
            notTagged.Add(element.Id);
            because(where + ": Revit would not place a " + kindWord + " tag here"
                  + (firstSpatialRefusal == null ? "" : " - see findings for its words"));
            continue;
        }

        // THE TYPE ASKED FOR, SET AND THEN READ BACK. The room and space calls
        // make the project's default tag of their kind.
        try
        {
            if (spatialMade.GetTypeId() != spatialTypeId) spatialMade.ChangeTypeId(spatialTypeId);
        }
        catch (Exception failure)
        {
            if (firstSpatialRefusal == null) firstSpatialRefusal = failure.Message;
        }
        if (spatialMade.GetTypeId() != spatialTypeId)
        {
            try { doc.Delete(spatialMade.Id); } catch { }
            notTagged.Add(element.Id);
            because(where + ": the " + kindWord + " tag would not take the type asked for, so it "
                  + "was taken out again");
            continue;
        }

        // AND WHAT IT POINTS AT, READ BACK. A tag on the wrong room still prints
        // a confident name.
        bool pointsRight = false;
        try
        {
            var madeRoomTag = spatialMade as RoomTag;
            if (madeRoomTag != null) pointsRight = madeRoomTag.TaggedLocalRoomId == element.Id;
            var madeSpaceTag = spatialMade as SpaceTag;
            if (madeSpaceTag != null && madeSpaceTag.Space != null)
                pointsRight = madeSpaceTag.Space.Id == element.Id;
            var madeZoneTag = spatialMade as IndependentTag;
            if (madeZoneTag != null) pointsRight = zoneTagTargets(madeZoneTag).Contains(element.Id);
        }
        catch { }

        if (!pointsRight)
        {
            try { doc.Delete(spatialMade.Id); } catch { }
            mismatched.Add(element.Id);
            continue;
        }

        newTags.Add(spatialMade.Id);
        spatialTaggedHere.Add(element.Id);
        spatialCategoriesUsed.Add(kindTagCategory);
        spatialTagged++;
        tagged++;
        continue;
    }

    // ALREADY TAGGED. With an exact type, only a tag OF THAT TYPE counts
    // (version 6); without one, any tag in this view does, as in version 5.
    HashSet<ElementId> typesHere;
    var hasOne = exactType != null
        ? tagTypesOn.TryGetValue(element.Id, out typesHere) && typesHere.Contains(exactType.Id)
        : alreadyHasTag.Contains(element.Id);
    if (hasOne)
    {
        alreadyTagged++;
        skipped.Add(element.Id);
        continue;
    }

    // ON THE MEP MAP OR NOT. Read here for the riser rule and the leader's
    // anchor; the tag category is looked up again below exactly as version 3
    // did. Where the head goes no longer depends on it (version 5).
    bool onMepMap = false;
    foreach (var pair in tagCategoryFor)
    {
        if (element.Category.Id == new ElementId(pair.Key)) { onMepMap = true; break; }
    }

    // A RISER IS A DOT ON A PLAN. Tagging one puts text and a leader against
    // a run that has no length on the paper at all, and the leader then
    // points at nothing a reader can follow. Ajmal's rule, 2026-09-21: a duct
    // running vertically is not tagged.
    //
    // JUDGED AGAINST THE VIEW, NOT WORLD Z, so a section or an elevation
    // decides it correctly too - what matters is whether the run still has
    // length once flattened onto the paper, not which way it points in the
    // model. A run keeping less than a fifth of its length in the view plane
    // is standing too close to end-on to tag.
    //
    // AN MEP RUN'S RULE, AND ONLY APPLIED TO ONE. A beam seen end-on in a
    // section is tagged there on purpose, so the categories version 4 added
    // skip this test; the minimum length below still applies to them, because
    // that one is the caller's own choice.
    var runCurve = element.Location as LocationCurve;
    if (runCurve != null && runCurve.Curve != null)
    {
        try
        {
            var span = runCurve.Curve.GetEndPoint(1) - runCurve.Curve.GetEndPoint(0);
            double full = span.GetLength();
            double across = span.DotProduct(view.RightDirection);
            double along = span.DotProduct(view.UpDirection);
            double inPlane = Math.Sqrt(across * across + along * along);
            if (onMepMap && full > 1e-9 && inPlane < full * 0.2)
            {
                verticalSkipped.Add(element.Id);
                continue;
            }

            // TOO SHORT TO BE WORTH A TAG. A 200 mm stub between two fittings
            // carries a tag wider than the run itself, and a drawing full of
            // those reads as clutter rather than as information. Where the
            // line falls is a drafting judgement, so it is the CALLER'S: 0,
            // the default, tags everything and keeps the old behaviour.
            //
            // MODEL MILLIMETRES, and deliberately not paper ones. "Do not tag
            // anything under half a metre" is a fact about the duct, and it
            // must not start meaning something different when somebody
            // changes the view scale. Every other millimetre value in this
            // fragment is a paper one, so this is the exception and is named
            // and scaled differently on purpose.
            if (minLengthMm > 0 && runCurve.Curve.Length < minLengthMm * MmToFeet)
            {
                tooShort.Add(element.Id);
                continue;
            }
        }
        catch { }
    }

    // ElementId.IntegerValue was removed by 2026 - the 2024 change finishing.
    // So a category is matched by comparing ELEMENT IDS rather than by
    // unwrapping one to an integer, which works unchanged on all eight.
    BuiltInCategory tagCategory = BuiltInCategory.INVALID;
    bool known = false;
    foreach (var pair in tagCategoryFor)
    {
        if (element.Category.Id == new ElementId(pair.Key))
        {
            tagCategory = pair.Value;
            known = true;
            break;
        }
    }

    // AN AREA - the one spatial element version 7's room route does not take
    // (rooms, spaces and zones went there above). It takes an area tag in an
    // area plan, which nothing in Heron places yet; an independent tag is not
    // that, whatever family is named, so it is refused in both modes.
    if (exactType != null && element is SpatialElement)
    {
        notTagged.Add(element.Id);
        because(element.Category.Name + ": an area takes an area tag in an area plan, which "
              + "nothing in Heron places yet - never an ordinary element tag");
        continue;
    }

    if (exactType != null)
    {
        // THE CALLER'S EXACT TYPE, on whatever category this is.
        known = true;
        tagCategory = BuiltInCategory.INVALID;
    }
    else if (!known)
    {
        // NOT ON THE MEP MAP: an area (the rooms, spaces and zones were
        // tagged above, version 7) is refused, and anything else takes the
        // tag category Revit's names give it.
        if (element is SpatialElement)
        {
            notTagged.Add(element.Id);
            because(element.Category.Name + ": an area takes an area tag in an area plan, which "
                  + "nothing in Heron places yet - never an ordinary element tag");
            continue;
        }

        tagCategory = tagCategoryByName(element.Category);
        if (tagCategory == BuiltInCategory.INVALID)
        {
            // No tag category exists for this kind of element. Reported rather
            // than tagged with something arbitrary.
            notTagged.Add(element.Id);
            because(element.Category.Name + ": Revit has no tag category for it");
            continue;
        }
    }

    ElementId chosenTypeId;
    if (exactType != null)
    {
        chosenTypeId = exactType.Id;
        if (!typeForCategory.ContainsKey(BuiltInCategory.INVALID))
        {
            typeForCategory[BuiltInCategory.INVALID] = exactType.Id;
            routeFor[BuiltInCategory.INVALID] = "the caller's exact type, on every category";
        }
    }
    else if (!typeForCategory.TryGetValue(tagCategory, out chosenTypeId))
    {
        chosenTypeId = ElementId.InvalidElementId;

        // 1. the caller's hint, if it really is a tag type for this category
        var hintSetAside = false;
        if (tagTypeHintId != null && tagTypeHintId != ElementId.InvalidElementId)
        {
            var hinted = doc.GetElement(tagTypeHintId) as FamilySymbol;
            if (hinted != null && hinted.Category != null
                && hinted.Category.Id == new ElementId(tagCategory))
                chosenTypeId = hinted.Id;
            else hintSetAside = true;
        }
        string route = "the caller's hint";

        // 2. the PROJECT'S OWN DEFAULT - what "Tag by Category" would use.
        // A hint of another category is SET ASIDE AND SAID (version 6): a
        // duct tag named for doors would print blank on every door.
        if (chosenTypeId == ElementId.InvalidElementId)
        {
            route = hintSetAside
                ? "the project's default - the hint given is not a tag for this category"
                : "the project's default";
            var category = Category.GetCategory(doc, tagCategory);
            if (category != null)
                chosenTypeId = doc.GetDefaultFamilyTypeId(category.Id);
        }

        // 3. last resort, the first one loaded - a guess, and the weakest route
        if (chosenTypeId == ElementId.InvalidElementId)
        {
            route = "the first one loaded - a guess";
            foreach (var symbol in new FilteredElementCollector(doc)
                                       .OfClass(typeof(FamilySymbol))
                                       .OfCategory(tagCategory)
                                       .Cast<FamilySymbol>())
            {
                chosenTypeId = symbol.Id;
                break;
            }
        }

        typeForCategory[tagCategory] = chosenTypeId;
        routeFor[tagCategory] = chosenTypeId == ElementId.InvalidElementId ? "none loaded" : route;
    }

    if (chosenTypeId == ElementId.InvalidElementId)
    {
        // No tag family of this category is loaded at all. Nothing this
        // fragment can do, and saying so is the useful answer.
        notTagged.Add(element.Id);
        string tagCategoryName = tagCategory.ToString();
        try { tagCategoryName = Category.GetCategory(doc, tagCategory).Name; } catch { }
        because(element.Category.Name + ": no " + tagCategoryName + " family is loaded - load one");
        continue;
    }

    // A tag type must be activated before it can be placed, and an inactive
    // symbol fails in a way that reads like the element's fault.
    var tagSymbol = doc.GetElement(chosenTypeId) as FamilySymbol;
    if (tagSymbol != null && !tagSymbol.IsActive) tagSymbol.Activate();

    // WHERE THE HEAD GOES: AT THE ELEMENT, leader or not, on the MEP map or
    // off it - its own point, or the centre of its extent when Revit gives no
    // location (a curtain-panel door). With no leader Revit puts the head
    // exactly at the point passed. Versions 3 and 4 passed XYZ.Zero on the MEP
    // map, and measured 2026-10-13 on Revit 2020.2 that stacked every duct tag
    // at the project origin, 87 to 93 m from its duct (FRAGMENT-ISSUES
    // 5b-452). Version 5 moved the head, and an MEP element with no point and
    // no extent is now refused with this reason rather than tagged at the
    // origin, as one off the map already was. The leader's anchor on the MEP
    // map below is version 3's.
    XYZ anchor = null;
    XYZ own = ownPoint(element);
    if (own == null)
    {
        notTagged.Add(element.Id);
        because(element.Category.Name + ": Revit gives no point or extent to put a tag at");
        continue;
    }
    if (wantLeader)
    {
        var locCurve = element.Location as LocationCurve;
        if (!onMepMap)
        {
            anchor = own;
        }
        else if (locCurve != null && locCurve.Curve != null)
        {
            anchor = locCurve.Curve.Evaluate(0.5, true);
        }
        else
        {
            var locPoint = element.Location as LocationPoint;
            if (locPoint != null) anchor = locPoint.Point;
        }
    }

    // SOME ELEMENTS HAVE NO LOCATION REVIT WILL GIVE - hosted and nested ones
    // among them. Those get the tag and no leader, and that is COUNTED rather
    // than passed off as a leader that was drawn.
    bool leaderHere = wantLeader && anchor != null;
    if (leaderHere) leadersDrawn++;
    if (wantLeader && !leaderHere) leadersNotDrawn++;

    XYZ headPoint = leaderHere
        ? new XYZ(anchor.X + offsetFeet, anchor.Y + offsetFeet, anchor.Z)
        : own;

    IndependentTag made = null;
    try
    {
        made = IndependentTag.Create(
            doc, chosenTypeId, view.Id, new Reference(element),
            leaderHere, TagOrientation.Horizontal, headPoint);
    }
    catch (Exception refusal)
    {
        // Revit's own words FIRST, cut short: a reason that opens with
        // context arrives with its last sentence missing.
        notTagged.Add(element.Id);
        var said = (refusal.Message ?? "").Trim();
        if (said.Length > 90) said = said.Substring(0, 90) + "...";
        because(element.Category.Name + ": Revit refused the tag - " + said);
        continue;
    }

    if (made == null)
    {
        notTagged.Add(element.Id);
        because(element.Category.Name + ": Revit made no tag and gave no reason");
        continue;
    }

    // THE READ-BACK THAT THIS FRAGMENT EXISTS FOR. Create was given a type id
    // and does not reliably use it. Check, correct, and COUNT the correction -
    // a silent fix would hide an API not doing what it was told.
    if (made.GetTypeId() != chosenTypeId)
    {
        try
        {
            made.ChangeTypeId(chosenTypeId);
            typeCorrections++;
        }
        catch (Exception)
        {
            // The tag exists but carries the wrong family. Not counted as
            // tagged: a tag of the wrong family is a drawing defect, and
            // reporting it as a success is how it reaches a printed sheet.
            notTagged.Add(element.Id);
            because(element.Category.Name + ": the tag came out in the wrong family and "
                  + "could not be changed back");
            continue;
        }
    }

    tagged++;
    newTags.Add(made.Id);
    heldTag(element.Id, chosenTypeId);
}

// THE TWO ANSWERS A PERSON CAN ACT ON. Why each untagged element is untagged,
// with a count, and which tag type each tag category used and by which of the
// three routes - so a guess at the first family loaded is never invisible.
var notTaggedWhy = new List<string>();
foreach (var reason in whyCount.OrderByDescending(r => r.Value))
    notTaggedWhy.Add(reason.Value + " x " + reason.Key);

var tagTypesUsed = new List<string>();
foreach (var entry in typeForCategory)
{
    // BuiltInCategory.INVALID is the exact type's own line (version 6): one
    // type on every category, so no tag category names it.
    string tagCategoryShown = entry.Key == BuiltInCategory.INVALID
        ? "every category" : entry.Key.ToString();
    if (entry.Key != BuiltInCategory.INVALID)
    {
        try { tagCategoryShown = Category.GetCategory(doc, entry.Key).Name; } catch { }
    }
    var symbolUsed = entry.Value == ElementId.InvalidElementId
        ? null : doc.GetElement(entry.Value) as FamilySymbol;
    string routeTaken;
    routeFor.TryGetValue(entry.Key, out routeTaken);
    tagTypesUsed.Add(tagCategoryShown + ": "
        + (symbolUsed == null ? "none" : symbolUsed.FamilyName + ": " + symbolUsed.Name)
        + " (" + (routeTaken ?? "unknown") + ")");
}

// VERSION 7. THE ROOM, SPACE AND ZONE ANSWER AS ONE LINE, because a reply
// shows a list's first three entries and its count - this line carries every
// count in words - and what would otherwise only show on the drawing: tags
// placed in a plan that hides their category, and Revit's own first refusal.
if (spatialHanded > 0)
{
    findings.Add(spatialTagged + " room, space or zone tag(s) placed in \"" + view.Name
        + "\". Of the " + spatialHanded + " handed in, not tagged: " + unplaced.Count
        + " not placed (or a zone holding no spaces), " + notEnclosed.Count + " not enclosed, "
        + otherLevel.Count + " on another level, " + mismatched.Count + " read back on the wrong "
        + "one and removed; the rest are in notTagged with why, or skipped as already tagged.");

    var hiddenWords = new List<string>();
    foreach (var used in spatialCategoriesUsed)
    {
        bool hidden = false;
        try { hidden = view.GetCategoryHidden(used); } catch { }
        if (hidden)
            hiddenWords.Add(used == zoneTagsCategory ? "Zone Tags"
                          : used == spaceTagsCategory ? "Space Tags" : "Room Tags");
    }
    if (hiddenWords.Count > 0)
        findings.Add(string.Join(" and ", hiddenWords.ToArray()) + " are turned off in \""
            + view.Name + "\" - in its Visibility/Graphics or its view template - so the tags "
            + "placed are in the model and NOT SHOWN in that view.");
    if (firstSpatialRefusal != null)
        findings.Add("Revit's first refusal said: " + firstSpatialRefusal);
}
