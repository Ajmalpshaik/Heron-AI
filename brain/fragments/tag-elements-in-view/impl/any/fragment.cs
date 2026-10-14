// NOT STANDALONE. Assumes `doc`, `elements`, `view` and `tagTypeHintId` are in
// scope, and leaves `tagged`, `alreadyTagged`, `typeCorrections`,
// `notTagged`, `notTaggedWhy` and `tagTypesUsed` behind.
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
// ROOMS, AREAS AND SPACES ARE NOT TAGGED HERE. Their tags are a different kind
// of element (a room tag, not an independent tag), placed by TAG_ROOMS, and
// IndependentTag.Create refuses them - so they are named in the answer rather
// than attempted.
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
var alreadyHasTag = new HashSet<ElementId>();
foreach (var existing in new FilteredElementCollector(doc, view.Id)
                             .OfClass(typeof(IndependentTag))
                             .Cast<IndependentTag>())
{
#if REVIT2020 || REVIT2021
    alreadyHasTag.Add(existing.TaggedLocalElementId);
#else
    foreach (var hostId in existing.GetTaggedLocalElementIds())
        alreadyHasTag.Add(hostId);
#endif
}

// Resolved once per tag category, not once per element - and WHICH of the
// three routes found it is kept, so `tagTypesUsed` can say so.
var typeForCategory = new Dictionary<BuiltInCategory, ElementId>();
var routeFor = new Dictionary<BuiltInCategory, string>();

foreach (var element in elements)
{
    if (element == null || element.Category == null)
    {
        notTagged.Add(element == null ? ElementId.InvalidElementId : element.Id);
        because("no category - nothing to choose a tag by");
        continue;
    }

    if (alreadyHasTag.Contains(element.Id))
    {
        alreadyTagged++;
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
    if (!known)
    {
        // NOT ON THE MEP MAP: a room, area or space is TAG_ROOMS' job, and
        // anything else takes the tag category Revit's names give it.
        if (element is SpatialElement)
        {
            notTagged.Add(element.Id);
            because(element.Category.Name + ": a room, area or space takes its own kind of "
                  + "tag - TAG_ROOMS places room tags");
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

    ElementId tagTypeId;
    if (!typeForCategory.TryGetValue(tagCategory, out tagTypeId))
    {
        tagTypeId = ElementId.InvalidElementId;

        // 1. the caller's hint, if it really is a tag type for this category
        if (tagTypeHintId != null && tagTypeHintId != ElementId.InvalidElementId)
        {
            var hinted = doc.GetElement(tagTypeHintId) as FamilySymbol;
            if (hinted != null && hinted.Category != null
                && hinted.Category.Id == new ElementId(tagCategory))
                tagTypeId = hinted.Id;
        }
        string route = "the caller's hint";

        // 2. the PROJECT'S OWN DEFAULT - what "Tag by Category" would use
        if (tagTypeId == ElementId.InvalidElementId)
        {
            route = "the project's default";
            var category = Category.GetCategory(doc, tagCategory);
            if (category != null)
                tagTypeId = doc.GetDefaultFamilyTypeId(category.Id);
        }

        // 3. last resort, the first one loaded - a guess, and the weakest route
        if (tagTypeId == ElementId.InvalidElementId)
        {
            route = "the first one loaded - a guess";
            foreach (var symbol in new FilteredElementCollector(doc)
                                       .OfClass(typeof(FamilySymbol))
                                       .OfCategory(tagCategory)
                                       .Cast<FamilySymbol>())
            {
                tagTypeId = symbol.Id;
                break;
            }
        }

        typeForCategory[tagCategory] = tagTypeId;
        routeFor[tagCategory] = tagTypeId == ElementId.InvalidElementId ? "none loaded" : route;
    }

    if (tagTypeId == ElementId.InvalidElementId)
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
    var tagSymbol = doc.GetElement(tagTypeId) as FamilySymbol;
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
            doc, tagTypeId, view.Id, new Reference(element),
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
    if (made.GetTypeId() != tagTypeId)
    {
        try
        {
            made.ChangeTypeId(tagTypeId);
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
    alreadyHasTag.Add(element.Id);
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
    string tagCategoryShown = entry.Key.ToString();
    try { tagCategoryShown = Category.GetCategory(doc, entry.Key).Name; } catch { }
    var symbolUsed = entry.Value == ElementId.InvalidElementId
        ? null : doc.GetElement(entry.Value) as FamilySymbol;
    string routeTaken;
    routeFor.TryGetValue(entry.Key, out routeTaken);
    tagTypesUsed.Add(tagCategoryShown + ": "
        + (symbolUsed == null ? "none" : symbolUsed.FamilyName + ": " + symbolUsed.Name)
        + " (" + (routeTaken ?? "unknown") + ")");
}
