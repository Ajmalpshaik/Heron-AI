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
var notTagged = new List<ElementId>();

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

    IndependentTag made = null;
    try
    {
        made = IndependentTag.Create(
            doc, tagTypeId, view.Id, new Reference(element),
            false, TagOrientation.Horizontal, XYZ.Zero);
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
