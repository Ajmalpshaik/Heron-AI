// NOT STANDALONE. Assumes `doc`, `elements`, `view` and `tagTypeHintId` are in
// scope, and leaves `tagged`, `alreadyTagged`, `typeCorrections` and
// `notTagged` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a whole view is one undo.
//
// THE THREE THINGS THIS CARRIES THAT THE API WILL NOT TELL YOU.
//
// 1. THE CATEGORY -> TAG CATEGORY MAP IS A FIXED FACT. Nothing derives "a duct
//    is tagged by a duct tag" at run time. `ElementTypeGroup` carries no
//    per-MEP-category tag entries, so the map lives here.
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

// LEADER SHAPE, and `none` is the default ON PURPOSE. `none` is byte for byte
// what this fragment did before leaders existed in it - addLeader false, head
// at XYZ.Zero - so the run proved on 2026-09-14 is not disturbed by any of
// this. Only a caller who asks for a leader gets the new path.
//
//   none      no leader at all
//   straight  one segment, element to tag head
//   L         a vertical off the element and a horizontal shoulder into the
//             head - what a modeller means by an L-shaped tag
//
// A LEADER NEEDS THE HEAD AWAY FROM THE ELEMENT. A leader from a point to
// itself is not a leader, so a head position is computed per element rather
// than the XYZ.Zero the no-leader path passes.
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

// Resolved once per tag category, not once per element.
var typeForCategory = new Dictionary<BuiltInCategory, ElementId>();

foreach (var element in elements)
{
    if (element == null || element.Category == null)
    {
        notTagged.Add(element == null ? ElementId.InvalidElementId : element.Id);
        continue;
    }

    if (alreadyHasTag.Contains(element.Id))
    {
        alreadyTagged++;
        continue;
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
            if (full > 1e-9 && inPlane < full * 0.2)
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
        // No tag category known for this kind of element. Reported rather than
        // tagged with something arbitrary.
        notTagged.Add(element.Id);
        continue;
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

        // 2. the PROJECT'S OWN DEFAULT - what "Tag by Category" would use
        if (tagTypeId == ElementId.InvalidElementId)
        {
            var category = Category.GetCategory(doc, tagCategory);
            if (category != null)
                tagTypeId = doc.GetDefaultFamilyTypeId(category.Id);
        }

        // 3. last resort, the first one loaded - a guess, and the weakest route
        if (tagTypeId == ElementId.InvalidElementId)
        {
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
    }

    if (tagTypeId == ElementId.InvalidElementId)
    {
        // No tag family of this category is loaded at all. Nothing this
        // fragment can do, and saying so is the useful answer.
        notTagged.Add(element.Id);
        continue;
    }

    // A tag type must be activated before it can be placed, and an inactive
    // symbol fails in a way that reads like the element's fault.
    var tagSymbol = doc.GetElement(tagTypeId) as FamilySymbol;
    if (tagSymbol != null && !tagSymbol.IsActive) tagSymbol.Activate();

    // WHERE THE HEAD GOES. Only asked once a leader is wanted: with no leader
    // the head is placed at the point passed, and that stays XYZ.Zero here.
    XYZ anchor = null;
    if (wantLeader)
    {
        var locCurve = element.Location as LocationCurve;
        if (locCurve != null && locCurve.Curve != null)
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
        : XYZ.Zero;

    IndependentTag made = null;
    try
    {
        made = IndependentTag.Create(
            doc, tagTypeId, view.Id, new Reference(element),
            leaderHere, TagOrientation.Horizontal, headPoint);
    }
    catch (Exception)
    {
        notTagged.Add(element.Id);
        continue;
    }

    if (made == null)
    {
        notTagged.Add(element.Id);
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
            continue;
        }
    }

    tagged++;
    alreadyHasTag.Add(element.Id);
}
