// NOT STANDALONE. Assumes `doc`, `views`, `rules`, `minGapMm` and `skipTagged`
// are in scope, and leaves `tagged`, `newTags`, `alreadyTagged`, `crowded`,
// `leadersDrawn`, `typeCorrections`, `notTagged`, `notTaggedWhy`,
// `verticalSkipped`, `tooShort`, `tagTypesUsed`, `rulesRead`, `perView`,
// `refused` and `findings` behind.
//
// TAG_VIEWS_BY_RULES, version 1 (2026-11-08). One pass over one view or many:
// a RULE PER CATEGORY says which tag, whether a leader may be drawn, and the
// shortest run worth a tag; every element of those categories shown in each
// view gets one tag, and each tag is put where its head does not sit on another
// tag head - placed clear at the moment it is placed, not tidied afterwards.
//
// RE-AUTHORED FROM TWO PROVEN CARDS, WHICH THIS DOES NOT CHANGE.
// TAG_ELEMENTS_IN_VIEW (FRG-VIEW-030) taught: the MEP tag-category map is a
// fixed fact and every other category's tag category comes from Revit's own
// enum names; a tag's type is READ BACK after IndependentTag.Create and
// corrected, and the corrections COUNTED; "already tagged" is read from the
// view's own tags, and with a type named only a tag OF THAT TYPE counts;
// risers are skipped; a room, a space or a zone takes its own kind of tag,
// only in a plan of its own level, never from a view-scoped collect. ARRANGE_TAGS
// (FRG-VIEW-080) taught: A TAG'S BOUNDING BOX INCLUDES ITS LEADER, so a head
// is measured from a LEADERLESS tag only, and a leadered one is judged at the
// median size of the leaderless ones; and every position is worked in the
// view's own plane - along its right and up directions - so nothing moves in
// the direction the reader cannot see.
//
// WHAT IS SAFER HERE THAN THERE, ON PURPOSE. No tag type is ever the first
// one loaded: a rule names one, or the project's default for that tag
// category is used, or the rule is refused before anything is placed. A tag
// that will not take its type is removed again rather than left on the
// drawing in the wrong family. One rule that cannot be read stops the whole
// call - a template applied in part is a drawing set tagged two ways.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16). The
// executor makes the whole call one TransactionGroup, so every view is one
// undo. It calls doc.Regenerate() twice per view inside that transaction -
// once to measure the heads it has just made, once to read back where they
// went - which needs no transaction of its own.

const double MmToFeet = 1.0 / 304.8;

int tagged = 0;
int typeCorrections = 0;
int leadersDrawn = 0;
var newTags = new List<ElementId>();
var alreadyTagged = new List<ElementId>();
var crowded = new List<ElementId>();
var notTagged = new List<ElementId>();
var verticalSkipped = new List<ElementId>();
var tooShort = new List<ElementId>();
var tagTypesUsed = new List<string>();
var rulesRead = new List<string>();
var perView = new List<string>();
var findings = new List<string>();
string refused = "";

// WHY EACH ONE IN `notTagged` IS THERE, one line per reason with a count -
// TAG_ELEMENTS_IN_VIEW's version 3 handed back bare ids and six untagged doors
// said nothing about why.
var whyCount = new Dictionary<string, int>();
var whyOrder = new List<string>();
Action<ElementId, string> because = (id, reason) =>
{
    notTagged.Add(id);
    int seenBefore;
    if (!whyCount.TryGetValue(reason, out seenBefore)) { seenBefore = 0; whyOrder.Add(reason); }
    whyCount[reason] = seenBefore + 1;
};
string firstRefusal = null;

// How a type reads in Revit's own Properties palette - "M_Duct Size Tag: Size".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};
Func<string, string> folded = s =>
    string.Join(":", (s ?? "").Split(':').Select(p => p.Trim()).ToArray()).ToLowerInvariant();
Func<FamilySymbol, string, bool> isNamed = (t, name) =>
    folded(name) == folded(typeLabel(t)) || folded(name) == folded(t.Name);

// ---- THE MEP MAP, a fixed fact (TAG_ELEMENTS_IN_VIEW lesson 1) --------------
// Nothing in the API derives "a duct is tagged by a duct tag". Extended by
// adding a pair, never by guessing at run time.
var mepTagCategory = new Dictionary<BuiltInCategory, BuiltInCategory>
{
    { BuiltInCategory.OST_DuctCurves,          BuiltInCategory.OST_DuctTags },
    { BuiltInCategory.OST_DuctFitting,         BuiltInCategory.OST_DuctFittingTags },
    { BuiltInCategory.OST_DuctAccessory,       BuiltInCategory.OST_DuctAccessoryTags },
    { BuiltInCategory.OST_FlexDuctCurves,      BuiltInCategory.OST_FlexDuctTags },
    { BuiltInCategory.OST_PipeCurves,          BuiltInCategory.OST_PipeTags },
    { BuiltInCategory.OST_PipeFitting,         BuiltInCategory.OST_PipeFittingTags },
    { BuiltInCategory.OST_PipeAccessory,       BuiltInCategory.OST_PipeAccessoryTags },
    { BuiltInCategory.OST_FlexPipeCurves,      BuiltInCategory.OST_FlexPipeTags },
    { BuiltInCategory.OST_DuctTerminal,        BuiltInCategory.OST_DuctTerminalTags },
    { BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_MechanicalEquipmentTags },
    { BuiltInCategory.OST_PlumbingFixtures,    BuiltInCategory.OST_PlumbingFixtureTags },
    { BuiltInCategory.OST_LightingFixtures,    BuiltInCategory.OST_LightingFixtureTags },
    { BuiltInCategory.OST_ElectricalEquipment, BuiltInCategory.OST_ElectricalEquipmentTags },
    { BuiltInCategory.OST_ElectricalFixtures,  BuiltInCategory.OST_ElectricalFixtureTags },
    { BuiltInCategory.OST_CableTray,           BuiltInCategory.OST_CableTrayTags },
    { BuiltInCategory.OST_Conduit,             BuiltInCategory.OST_ConduitTags },
    { BuiltInCategory.OST_Sprinklers,          BuiltInCategory.OST_SprinklerTags },
};

// EVERY OTHER CATEGORY from Revit's own enum names: OST_Doors -> OST_DoorTags,
// OST_GenericModel -> OST_GenericModelTags. The name with "Tags", then the
// singular with "Tags", and only when THIS document has that category and it
// is an annotation one. Enum names, not shown names, so a German Revit
// derives the same answer.
Func<Category, ElementId> derivedTagCategory = elementCategory =>
{
    foreach (var enumName in Enum.GetNames(typeof(BuiltInCategory)))
    {
        BuiltInCategory own;
        if (!Enum.TryParse(enumName, false, out own)) continue;
        if (elementCategory.Id != new ElementId(own)) continue;

        var singular = enumName.EndsWith("ies", StringComparison.Ordinal)
            ? enumName.Substring(0, enumName.Length - 3) + "y"
            : enumName.EndsWith("s", StringComparison.Ordinal) && !enumName.EndsWith("ss", StringComparison.Ordinal)
                ? enumName.Substring(0, enumName.Length - 1)
                : enumName;
        foreach (var tagName in new[] { enumName + "Tags", singular + "Tags" })
        {
            BuiltInCategory candidate;
            if (!Enum.TryParse(tagName, false, out candidate)) continue;
            Category here = null;
            try { here = Category.GetCategory(doc, candidate); } catch { }
            if (here != null && here.CategoryType == CategoryType.Annotation) return here.Id;
        }
        break;
    }
    return ElementId.InvalidElementId;
};

// A category as the modeller types it - what Visibility/Graphics shows,
// capitals ignored, "Duct" for "Ducts" - or the API's own OST_ name.
Func<string, Category> categoryNamed = name =>
{
    var wanted = (name ?? "").Trim();
    if (wanted.Length == 0) return null;
    Category loose = null;
    foreach (Category c in doc.Settings.Categories)
    {
        if (c == null) continue;
        var shown = c.Name ?? "";
        if (string.Equals(shown, wanted, StringComparison.OrdinalIgnoreCase)) return c;
        if (loose == null
            && (string.Equals(shown, wanted + "s", StringComparison.OrdinalIgnoreCase)
                || string.Equals(shown + "s", wanted, StringComparison.OrdinalIgnoreCase)))
            loose = c;
    }
    if (loose != null) return loose;
    BuiltInCategory byApi;
    if (wanted.StartsWith("OST_", StringComparison.OrdinalIgnoreCase) && Enum.TryParse(wanted, true, out byApi))
    {
        try { return Category.GetCategory(doc, byApi); } catch { }
    }
    return null;
};

var roomsId = new ElementId(BuiltInCategory.OST_Rooms);
var spacesId = new ElementId(BuiltInCategory.OST_MEPSpaces);
var zonesId = new ElementId(BuiltInCategory.OST_HVAC_Zones);
var areasId = new ElementId(BuiltInCategory.OST_Areas);
var roomTagsId = new ElementId(BuiltInCategory.OST_RoomTags);
var spaceTagsId = new ElementId(BuiltInCategory.OST_MEPSpaceTags);
var zoneTagsId = new ElementId(BuiltInCategory.OST_ZoneTags);
var multiTagsId = new ElementId(BuiltInCategory.OST_MultiCategoryTags);

// ---- 1. THE RULES, read in full before anything is placed -------------------
//
// One per line - a new line or a semicolon ends one - each
//     category | tag family: type | leader | shortest run in mm
// with everything after the category optional. A blank tag type is the
// PROJECT'S DEFAULT for that tag category, never the first one loaded. The
// leader is `none` (left out) or `straight`: straight is drawn only when no
// clear spot was found beside the element. The length is MODEL millimetres,
// the run's own length, so it does not change meaning with the view scale.
var ruleCategory = new List<Category>();
var ruleKind = new List<string>();           // element | room | space | zone
var ruleTagCategory = new List<ElementId>();
var ruleType = new List<FamilySymbol>();
var ruleNamed = new List<bool>();
var ruleHow = new List<string>();
var ruleLeader = new List<bool>();
var ruleMinFeet = new List<double>();
var ruleOnMepMap = new List<bool>();
var ruleProblems = new List<string>();

var ruleText = (rules ?? "").Replace("\r", "\n");
int ruleNumber = 0;
foreach (var raw in ruleText.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
{
    var line = raw.Trim();
    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
    ruleNumber++;
    var said = "rule " + ruleNumber + " (\"" + line + "\")";
    var parts = line.Split('|');
    if (parts.Length > 4)
    {
        ruleProblems.Add(said + " has " + parts.Length + " parts - a rule is at most four: category | tag type | "
            + "leader | shortest run in mm");
        continue;
    }
    var categoryText = parts[0].Trim();
    var typeText = parts.Length > 1 ? parts[1].Trim() : "";
    var leaderText = parts.Length > 2 ? parts[2].Trim().ToLowerInvariant() : "";
    var lengthText = parts.Length > 3 ? parts[3].Trim() : "";

    var category = categoryNamed(categoryText);
    if (category == null)
    {
        ruleProblems.Add(said + ": no category is called \"" + categoryText + "\" in this model - type it as "
            + "Visibility/Graphics shows it, e.g. Ducts, Pipe Fittings, Doors");
        continue;
    }
    if (ruleCategory.Any(c => c.Id == category.Id))
    {
        ruleProblems.Add(said + ": " + category.Name + " has a rule already - one rule per category, so it is "
            + "never tagged twice by one call");
        continue;
    }

    string kind = "element";
    ElementId tagCategoryId = ElementId.InvalidElementId;
    bool onMepMap = false;
    if (category.Id == areasId)
    {
        ruleProblems.Add(said + ": an area takes an area tag in an area plan of its own scheme, which this does "
            + "not place - ask to tag the areas in that area plan, and take this line out");
        continue;
    }
    if (category.Id == roomsId) { kind = "room"; tagCategoryId = roomTagsId; }
    else if (category.Id == spacesId) { kind = "space"; tagCategoryId = spaceTagsId; }
    else if (category.Id == zonesId) { kind = "zone"; tagCategoryId = zoneTagsId; }
    else
    {
        if (category.CategoryType != CategoryType.Model)
        {
            ruleProblems.Add(said + ": " + category.Name + " is not a model category - tags go on model elements");
            continue;
        }
        foreach (var pair in mepTagCategory)
        {
            if (category.Id != new ElementId(pair.Key)) continue;
            onMepMap = true;
            tagCategoryId = new ElementId(pair.Value);
            break;
        }
        if (!onMepMap) tagCategoryId = derivedTagCategory(category);
        if (tagCategoryId == ElementId.InvalidElementId)
        {
            ruleProblems.Add(said + ": Revit has no tag category for " + category.Name);
            continue;
        }
    }
    string tagCategoryName = "tag";
    try { tagCategoryName = Category.GetCategory(doc, tagCategoryId).Name; } catch { }

    // THE TAG TYPE. Of this tag category - or a Multi-Category tag, for an
    // element - and never of another: Revit places a tag of the wrong
    // category without a word, and it prints blank.
    var allowed = new List<ElementId> { tagCategoryId };
    if (kind == "element") allowed.Add(multiTagsId);
    var loaded = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
        .Where(t => t.Category != null && allowed.Contains(t.Category.Id)).ToList();
    var loadedWords = loaded.Count == 0 ? "none is loaded"
        : "loaded: " + string.Join(", ", loaded.Take(4).Select(t => "'" + typeLabel(t) + "'").ToArray())
          + (loaded.Count > 4 ? " and " + (loaded.Count - 4) + " more" : "");

    FamilySymbol chosen = null;
    string how = null;
    if (typeText.Length == 0)
    {
        ElementId defaultId = null;
        try { defaultId = doc.GetDefaultFamilyTypeId(tagCategoryId); } catch { defaultId = null; }
        chosen = defaultId == null || defaultId == ElementId.InvalidElementId ? null : doc.GetElement(defaultId) as FamilySymbol;
        if (chosen == null)
        {
            ruleProblems.Add(said + ": no tag type is named and this project sets no default " + tagCategoryName
                + " type, so none is chosen for you - name one (" + loadedWords + ")");
            continue;
        }
        how = "the project's default " + tagCategoryName;
    }
    else
    {
        var hits = loaded.Where(t => isNamed(t, typeText)).ToList();
        if (hits.Count > 1)
        {
            ruleProblems.Add(said + ": " + hits.Count + " tag types are called \"" + typeText + "\" - "
                + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                + " - name it with its family, \"Family: Type\"");
            continue;
        }
        if (hits.Count == 0)
        {
            var elsewhere = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .Where(t => t.Category != null && isNamed(t, typeText))
                .Select(t => "'" + typeLabel(t) + "' (" + t.Category.Name + ")").Distinct().Take(3).ToList();
            ruleProblems.Add(said + ": " + (elsewhere.Count > 0
                ? "\"" + typeText + "\" is " + string.Join(", ", elsewhere.ToArray()) + ", not a " + tagCategoryName
                  + (kind == "element" ? " or Multi-Category" : "") + " type, and would print blank on "
                  + category.Name
                : "no " + tagCategoryName + " type is called \"" + typeText + "\" - the name has to match the "
                  + "Project Browser") + " (" + loadedWords + ")");
            continue;
        }
        chosen = hits[0];
        how = "named in the rule";
    }

    bool leaderAllowed;
    if (leaderText.Length == 0 || leaderText == "none" || leaderText == "no") leaderAllowed = false;
    else if (leaderText == "straight" || leaderText == "yes") leaderAllowed = true;
    else
    {
        ruleProblems.Add(said + ": \"" + leaderText + "\" is not a leader rule - say none or straight. A bent "
            + "leader is made afterwards, by fixing the leader shape on the tags");
        continue;
    }
    if (leaderAllowed && kind != "element")
    {
        ruleProblems.Add(said + ": a " + kind + " tag stays at its " + kind + "'s own point here and is never "
            + "drawn out on a leader - leave the leader blank or none");
        continue;
    }

    double minMm = 0;
    var lengthDigits = lengthText.EndsWith("mm", StringComparison.OrdinalIgnoreCase)
        ? lengthText.Substring(0, lengthText.Length - 2).Trim() : lengthText;
    if (lengthDigits.Length > 0
        && (!double.TryParse(lengthDigits, System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture, out minMm) || minMm < 0))
    {
        ruleProblems.Add(said + ": \"" + lengthText + "\" is not a length - the shortest run, in model "
            + "millimetres, e.g. 500");
        continue;
    }
    if (minMm > 0 && kind != "element")
    {
        ruleProblems.Add(said + ": a shortest run means nothing for a " + kind + " - leave it blank");
        continue;
    }

    ruleCategory.Add(category);
    ruleKind.Add(kind);
    ruleTagCategory.Add(tagCategoryId);
    ruleType.Add(chosen);
    ruleNamed.Add(typeText.Length > 0);
    ruleHow.Add(how);
    ruleLeader.Add(leaderAllowed);
    ruleMinFeet.Add(minMm * MmToFeet);
    ruleOnMepMap.Add(onMepMap);
    rulesRead.Add(ruleCategory.Count + ". " + category.Name + " -> '" + typeLabel(chosen) + "' (" + how + "); leader "
        + (leaderAllowed ? "straight, drawn only when no clear spot is found beside the element" : "none")
        + (minMm > 0 ? "; runs shorter than " + minMm + " mm left untagged" : ""));
    tagTypesUsed.Add(category.Name + ": " + typeLabel(chosen) + " (" + how + ")");
}

if (ruleNumber == 0)
    refused = "No rules were given. One per line, \"category | tag type | leader | shortest run in mm\" - e.g. "
            + "\"Ducts | M_Duct Size Tag: Size | straight | 500\". Everything after the category may be left "
            + "out: a blank tag type is the project's default tag for that category.";
else if (ruleProblems.Count > 0)
    refused = ruleProblems.Count + " of the " + ruleNumber + " rule(s) could not be read: "
            + string.Join("; ", ruleProblems.ToArray()) + ".";

// ---- 2. THE VIEWS ------------------------------------------------------------
//
// None named is the ACTIVE view. A template, a kind of view that takes no
// tags, or one that SHARES ITS ANNOTATION with a view already in the list (a
// dependent view and its primary) is left out by name - tagging both would
// put two tags on one element in one drawing.
var given = new List<View>();
bool fromActive = false;
if (views != null) foreach (var v in views) if (v != null && !given.Any(g => g.Id == v.Id)) given.Add(v);
if (given.Count == 0)
{
    View active = null;
    try { active = doc.ActiveView; } catch { }
    if (active != null) { given.Add(active); fromActive = true; }
}

var tagKinds = new[] { ViewType.FloorPlan, ViewType.CeilingPlan, ViewType.EngineeringPlan, ViewType.AreaPlan,
                       ViewType.Section, ViewType.Elevation, ViewType.Detail };
Func<View, HashSet<ElementId>> sharing = v =>
{
    var held = new HashSet<ElementId> { v.Id };
    try
    {
        foreach (var dependentId in v.GetDependentViewIds()) held.Add(dependentId);
        var primaryId = v.GetPrimaryViewId();
        if (primaryId != null && primaryId != ElementId.InvalidElementId)
        {
            held.Add(primaryId);
            var primary = doc.GetElement(primaryId) as View;
            if (primary != null) foreach (var siblingId in primary.GetDependentViewIds()) held.Add(siblingId);
        }
    }
    catch { }
    return held;
};

var working = new List<View>();
var coveredBy = new Dictionary<ElementId, string>();
var viewsLeft = new List<string>();
foreach (var v in given)
{
    string leftWhy = null;
    string earlier;
    if (v.IsTemplate) leftWhy = "is a view template, which holds no tags";
    else if (!tagKinds.Contains(v.ViewType))
        leftWhy = "is a " + v.ViewType + " view - tags go in a plan, a ceiling plan, a section, an elevation or a detail";
    else if (coveredBy.TryGetValue(v.Id, out earlier))
        leftWhy = "shares its tags with \"" + earlier + "\", already in the list - a tag placed there shows here, so "
                + "it is not tagged twice";
    if (leftWhy != null)
    {
        viewsLeft.Add("\"" + v.Name + "\" " + leftWhy);
        perView.Add("\"" + v.Name + "\": not tagged - it " + leftWhy + ".");
        continue;
    }
    working.Add(v);
    foreach (var id in sharing(v)) if (!coveredBy.ContainsKey(id)) coveredBy[id] = v.Name;
}

if (refused.Length == 0 && given.Count == 0)
    refused = "No view was named and there is no active view. Name the views as the Project Browser shows them, "
            + "separated by commas.";
else if (refused.Length == 0 && working.Count == 0)
    refused = "None of the views can take tags: " + string.Join("; ", viewsLeft.ToArray()) + ".";
if (refused.Length > 0) refused += " Nothing was placed.";

// ---- helpers shared by every view -------------------------------------------

// The element's own point: the middle of its location line, its insertion
// point, or the centre of its extent in this view (a curtain-panel door).
Func<Element, View, XYZ> ownPoint = (element, inView) =>
{
    var onCurve = element.Location as LocationCurve;
    if (onCurve != null && onCurve.Curve != null) return onCurve.Curve.Evaluate(0.5, true);
    var onPoint = element.Location as LocationPoint;
    if (onPoint != null) return onPoint.Point;
    BoundingBoxXYZ extent = null;
    try { extent = element.get_BoundingBox(inView) ?? element.get_BoundingBox(null); } catch { }
    return extent == null ? null : (extent.Min + extent.Max) * 0.5;
};

// A tag's box ON THE PAPER: its eight corners carried through the box's own
// transform and read along the view's right and up. {centre u, centre v, half
// width, half height}, or null when Revit gives none (a hidden category).
Func<Element, View, double[]> paperBox = (element, inView) =>
{
    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(inView); } catch { }
    if (box == null) return null;
    Transform frame = null;
    try { frame = box.Transform; } catch { }
    if (frame == null) frame = Transform.Identity;
    var right = inView.RightDirection;
    var up = inView.UpDirection;
    double uLow = double.MaxValue, uHigh = double.MinValue, vLow = double.MaxValue, vHigh = double.MinValue;
    for (int corner = 0; corner < 8; corner++)
    {
        var p = frame.OfPoint(new XYZ((corner & 1) == 0 ? box.Min.X : box.Max.X,
                                      (corner & 2) == 0 ? box.Min.Y : box.Max.Y,
                                      (corner & 4) == 0 ? box.Min.Z : box.Max.Z));
        double u = p.DotProduct(right), w = p.DotProduct(up);
        uLow = Math.Min(uLow, u); uHigh = Math.Max(uHigh, u);
        vLow = Math.Min(vLow, w); vHigh = Math.Max(vHigh, w);
    }
    if (uHigh - uLow <= 1e-9 || vHigh - vLow <= 1e-9) return null;
    return new[] { (uLow + uHigh) / 2, (vLow + vHigh) / 2, (uHigh - uLow) / 2, (vHigh - vLow) / 2 };
};

Func<Element, XYZ> headOf = element =>
{
    try
    {
        var asTag = element as IndependentTag;
        if (asTag != null) return asTag.TagHeadPosition;
        var asSpatial = element as SpatialElementTag;
        if (asSpatial != null) return asSpatial.TagHeadPosition;
    }
    catch { }
    return null;
};
Func<Element, bool> leaderOf = element =>
{
    try
    {
        var asTag = element as IndependentTag;
        if (asTag != null) return asTag.HasLeader;
        var asSpatial = element as SpatialElementTag;
        if (asSpatial != null) return asSpatial.HasLeader;
    }
    catch { }
    return false;
};

// What a ZONE tag points at, on every release - found by name, as
// TAG_ELEMENTS_IN_VIEW reads it (GetTaggedLocalElementIds from 2022, the
// TaggedLocalElementId property before).
Func<IndependentTag, List<ElementId>> zoneTargets = t =>
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

// Two boxes clash when they are closer than `gap` on BOTH axes.
Func<double[], double[], double, bool> clash = (a, b, gap) =>
    Math.Abs(a[0] - b[0]) < a[2] + b[2] + gap && Math.Abs(a[1] - b[1]) < a[3] + b[3] + gap;

int sizesGuessed = 0;
int leaderedJudged = 0;
int headsNotTaken = 0;
var hiddenWords = new List<string>();

// ---- 3. EVERY VIEW -----------------------------------------------------------
foreach (var view in (refused.Length == 0 ? working : new List<View>()))
{
    int placedHere = 0, leadersHere = 0, alreadyHere = 0, risersHere = 0, shortHere = 0, notHere = 0;
    int outsideCrop = 0;
    var crowdedHere = new HashSet<ElementId>();
    var spatialNotes = new List<string>();

    int scale = 1;
    try { scale = Math.Max(1, view.Scale); } catch { }
    double gapFeet = Math.Max(0, minGapMm) * MmToFeet * scale;
    var right = view.RightDirection;
    var up = view.UpDirection;

    Level planLevel = null;
    try { planLevel = view.GenLevel; } catch { }
    bool isPlan = view.ViewType == ViewType.FloorPlan || view.ViewType == ViewType.CeilingPlan
               || view.ViewType == ViewType.EngineeringPlan || view.ViewType == ViewType.AreaPlan;

    Func<XYZ, bool> insideCrop = p =>
    {
        try
        {
            if (!view.CropBoxActive) return true;
            var box = view.CropBox;
            if (box == null) return true;
            var local = box.Transform.Inverse.OfPoint(p);
            return local.X >= box.Min.X - 1e-6 && local.X <= box.Max.X + 1e-6
                && local.Y >= box.Min.Y - 1e-6 && local.Y <= box.Max.Y + 1e-6;
        }
        catch { return true; }
    };

    // THE TAGS ALREADY IN THIS VIEW, read once before anything is made: what
    // each points at and of which type (the "already tagged" rule), and which
    // of them are heads to keep clear of.
    var alreadyHasTag = new HashSet<ElementId>();
    var tagTypesOn = new Dictionary<ElementId, HashSet<ElementId>>();
    Action<ElementId, ElementId> heldTag = (hostId, typeId) =>
    {
        if (hostId == null) return;
        alreadyHasTag.Add(hostId);
        HashSet<ElementId> types;
        if (!tagTypesOn.TryGetValue(hostId, out types)) { types = new HashSet<ElementId>(); tagTypesOn[hostId] = types; }
        if (typeId != null) types.Add(typeId);
    };
    var existingHeads = new List<Element>();
    foreach (var existing in new FilteredElementCollector(doc, view.Id).OfClass(typeof(IndependentTag))
                                 .Cast<IndependentTag>())
    {
        existingHeads.Add(existing);
        var existingType = existing.GetTypeId();
#if REVIT2020 || REVIT2021
        heldTag(existing.TaggedLocalElementId, existingType);
#else
        foreach (var hostId in existing.GetTaggedLocalElementIds()) heldTag(hostId, existingType);
#endif
    }
    foreach (var spatialTagCategory in new[] { roomTagsId, spaceTagsId,
                                               new ElementId(BuiltInCategory.OST_AreaTags) })
        foreach (var existing in new FilteredElementCollector(doc, view.Id).OfCategoryId(spatialTagCategory)
                                     .WhereElementIsNotElementType())
            if (existing is SpatialElementTag) existingHeads.Add(existing);

    // Rooms, spaces and zones already tagged in this plan - read from the
    // WHOLE MODEL by the view that owns each tag, the views sharing its
    // annotation included, never from what the view shows.
    HashSet<ElementId> spatialHeld = null;
    Func<HashSet<ElementId>> readSpatialHeld = () =>
    {
        var held = new HashSet<ElementId>();
        var owners = sharing(view);
        foreach (var tagCategory in new[] { roomTagsId, spaceTagsId, zoneTagsId })
            foreach (var existing in new FilteredElementCollector(doc).OfCategoryId(tagCategory)
                                         .WhereElementIsNotElementType())
            {
                if (!owners.Contains(existing.OwnerViewId)) continue;
                try
                {
                    var roomTag = existing as RoomTag;
                    if (roomTag != null) held.Add(roomTag.TaggedLocalRoomId);
                    var spaceTag = existing as SpaceTag;
                    if (spaceTag != null && spaceTag.Space != null) held.Add(spaceTag.Space.Id);
                    var zoneTag = existing as IndependentTag;
                    if (zoneTag != null) foreach (var id in zoneTargets(zoneTag)) held.Add(id);
                }
                catch { }
            }
        return held;
    };

    var madeElementTags = new List<IndependentTag>();
    var madeLeaderAllowed = new Dictionary<ElementId, bool>();
    var madeSpatialTags = new List<Element>();

    for (int r = 0; r < ruleCategory.Count; r++)
    {
        var symbol = ruleType[r];
        if (symbol != null && !symbol.IsActive) { try { symbol.Activate(); } catch { } }
        var kind = ruleKind[r];

        // ---- A ROOM, A SPACE OR AN HVAC ZONE takes its own kind of tag -------
        if (kind != "element")
        {
            if (!isPlan || planLevel == null)
            {
                spatialNotes.Add(ruleCategory[r].Name + " not tagged here - a " + kind + " tag goes in a plan of its "
                    + "own level");
                continue;
            }
            if (spatialHeld == null) spatialHeld = readSpatialHeld();

            var candidates = new List<Element>();
            foreach (var e in new FilteredElementCollector(doc).OfCategoryId(ruleCategory[r].Id)
                                  .WhereElementIsNotElementType())
                candidates.Add(e);

            foreach (var element in candidates)
            {
                XYZ point = null;
                if (kind == "zone")
                {
                    var zone = element as Zone;
                    if (zone == null || zone.LevelId != planLevel.Id) continue;
                    bool isDefault = false;
                    try
                    {
                        var flag = zone.GetType().GetProperty("IsDefaultZone");
                        if (flag != null) isDefault = (bool)flag.GetValue(zone, null);
                    }
                    catch { }
                    if (isDefault) continue;
                    Space largest = null;
                    try
                    {
                        foreach (Space member in zone.Spaces)
                        {
                            if (member == null || member.LevelId != planLevel.Id || !(member.Location is LocationPoint)) continue;
                            if (member.Area <= 0) continue;
                            if (largest == null || member.Area > largest.Area) largest = member;
                        }
                    }
                    catch { }
                    if (largest == null)
                    {
                        because(element.Id, "HVAC Zones: holds no enclosed space on this level - nowhere to put its tag");
                        notHere++;
                        continue;
                    }
                    point = ((LocationPoint)largest.Location).Point;
                }
                else
                {
                    if (element.LevelId != planLevel.Id) continue;
                    var placedAt = element.Location as LocationPoint;
                    if (placedAt == null || placedAt.Point == null)
                    {
                        because(element.Id, ruleCategory[r].Name + ": not placed - in the schedule and in no plan");
                        notHere++;
                        continue;
                    }
                    double area = 0;
                    try { area = ((SpatialElement)element).Area; } catch { }
                    if (area <= 0)
                    {
                        because(element.Id, ruleCategory[r].Name + ": not enclosed - no area, so nowhere certain "
                            + "to put a tag");
                        notHere++;
                        continue;
                    }
                    point = placedAt.Point;
                }
                if (!insideCrop(point)) { outsideCrop++; continue; }
                if (spatialHeld.Contains(element.Id))
                {
                    alreadyTagged.Add(element.Id);
                    alreadyHere++;
                    continue;
                }

                Element made = null;
                try
                {
                    if (kind == "zone")
                        made = IndependentTag.Create(doc, symbol.Id, view.Id, new Reference(element), false,
                                                     TagOrientation.Horizontal, new XYZ(point.X, point.Y, 0));
                    else if (kind == "space")
                        made = doc.Create.NewSpaceTag((Space)element, new UV(point.X, point.Y), view);
                    else
                        made = doc.Create.NewRoomTag(new LinkElementId(element.Id), new UV(point.X, point.Y), view.Id);
                }
                catch (Exception failure) { if (firstRefusal == null) firstRefusal = failure.Message; }
                if (made == null)
                {
                    because(element.Id, ruleCategory[r].Name + ": Revit would not place the tag - see findings for "
                        + "its words");
                    notHere++;
                    continue;
                }
                try { if (made.GetTypeId() != symbol.Id) made.ChangeTypeId(symbol.Id); }
                catch (Exception failure) { if (firstRefusal == null) firstRefusal = failure.Message; }
                bool pointsRight = false;
                try
                {
                    var roomTag = made as RoomTag;
                    if (roomTag != null) pointsRight = roomTag.TaggedLocalRoomId == element.Id;
                    var spaceTag = made as SpaceTag;
                    if (spaceTag != null && spaceTag.Space != null) pointsRight = spaceTag.Space.Id == element.Id;
                    var zoneTag = made as IndependentTag;
                    if (zoneTag != null) pointsRight = zoneTargets(zoneTag).Contains(element.Id);
                }
                catch { }
                if (made.GetTypeId() != symbol.Id || !pointsRight)
                {
                    try { doc.Delete(made.Id); } catch { }
                    because(element.Id, ruleCategory[r].Name + ": the tag read back " + (pointsRight
                        ? "as another type" : "pointing at another " + kind) + ", so it was taken out again");
                    notHere++;
                    continue;
                }
                spatialHeld.Add(element.Id);
                madeSpatialTags.Add(made);
                newTags.Add(made.Id);
                tagged++;
                placedHere++;
            }
            continue;
        }

        // ---- ANY OTHER ELEMENT: an independent tag ----------------------------
        foreach (var element in new FilteredElementCollector(doc, view.Id).OfCategoryId(ruleCategory[r].Id)
                                    .WhereElementIsNotElementType())
        {
            if (element.Category == null) continue;

            // ALREADY TAGGED. Never a second tag of the SAME type in one view,
            // whatever the switch says. With skipTagged (the default) the rule
            // of TAG_ELEMENTS_IN_VIEW: with a type named, only a tag of THAT type
            // counts; with none named, any tag in this view does.
            HashSet<ElementId> typesHere;
            bool hasSame = tagTypesOn.TryGetValue(element.Id, out typesHere) && typesHere.Contains(symbol.Id);
            bool hasAny = alreadyHasTag.Contains(element.Id);
            if (hasSame || (skipTagged && (ruleNamed[r] ? hasSame : hasAny)))
            {
                alreadyTagged.Add(element.Id);
                alreadyHere++;
                continue;
            }

            // A RISER IS A DOT ON THE PAPER (an MEP run's rule only), judged
            // against the view, so a section decides it correctly too. And a
            // run shorter than the rule's minimum - MODEL length.
            var run = element.Location as LocationCurve;
            if (run != null && run.Curve != null)
            {
                bool skip = false;
                try
                {
                    var span = run.Curve.GetEndPoint(1) - run.Curve.GetEndPoint(0);
                    double full = span.GetLength();
                    double across = span.DotProduct(right), along = span.DotProduct(up);
                    if (ruleOnMepMap[r] && full > 1e-9 && Math.Sqrt(across * across + along * along) < full * 0.2)
                    {
                        verticalSkipped.Add(element.Id);
                        risersHere++;
                        skip = true;
                    }
                    else if (ruleMinFeet[r] > 0 && run.Curve.Length < ruleMinFeet[r])
                    {
                        tooShort.Add(element.Id);
                        shortHere++;
                        skip = true;
                    }
                }
                catch { }
                if (skip) continue;
            }

            var own = ownPoint(element, view);
            if (own == null)
            {
                because(element.Id, element.Category.Name + ": Revit gives no point or extent to put a tag at");
                notHere++;
                continue;
            }

            // MADE WITH NO LEADER, ON THE ELEMENT - so it can be measured. It is
            // moved, and given a leader if one is needed, once every head in
            // the view is known.
            IndependentTag made = null;
            try
            {
                made = IndependentTag.Create(doc, symbol.Id, view.Id, new Reference(element), false,
                                             TagOrientation.Horizontal, own);
            }
            catch (Exception failure)
            {
                var words = (failure.Message ?? "").Trim();
                if (words.Length > 90) words = words.Substring(0, 90) + "...";
                because(element.Id, element.Category.Name + ": Revit refused the tag - " + words);
                notHere++;
                continue;
            }
            if (made == null)
            {
                because(element.Id, element.Category.Name + ": Revit made no tag and gave no reason");
                notHere++;
                continue;
            }

            // THE READ-BACK (TAG_ELEMENTS_IN_VIEW lesson 3): Create is given a type
            // and does not reliably use it. Corrected and COUNTED; one that
            // will not take it is taken out again, never left in the wrong family.
            if (made.GetTypeId() != symbol.Id)
            {
                bool corrected = false;
                try { made.ChangeTypeId(symbol.Id); corrected = made.GetTypeId() == symbol.Id; } catch { }
                if (!corrected)
                {
                    try { doc.Delete(made.Id); } catch { }
                    because(element.Id, element.Category.Name + ": the tag came out as another type and would not "
                        + "change to '" + typeLabel(symbol) + "', so it was taken out again");
                    notHere++;
                    continue;
                }
                typeCorrections++;
            }

            heldTag(element.Id, symbol.Id);
            madeElementTags.Add(made);
            madeLeaderAllowed[made.Id] = ruleLeader[r];
            newTags.Add(made.Id);
            tagged++;
            placedHere++;
        }
    }

    // ---- MEASURE every head in the view, now that the new ones exist --------
    if (madeElementTags.Count > 0 || madeSpatialTags.Count > 0)
    {
        doc.Regenerate();

        var halfWidths = new List<double>();
        var halfHeights = new List<double>();
        var obstacles = new List<double[]>();
        var leaderedAt = new List<XYZ>();

        // EXISTING HEADS. Leaderless: the box IS the head. Leadered: the box
        // wraps the leader too (ARRANGE_TAGS' lesson), so only its head point
        // is kept and it is judged at the median size below.
        foreach (var existing in existingHeads)
        {
            if (leaderOf(existing))
            {
                var head = headOf(existing);
                if (head != null) leaderedAt.Add(head);
                continue;
            }
            var box = paperBox(existing, view);
            if (box == null) continue;
            obstacles.Add(box);
            halfWidths.Add(box[2]);
            halfHeights.Add(box[3]);
        }
        foreach (var made in madeSpatialTags)
        {
            var box = paperBox(made, view);
            if (box == null) continue;
            obstacles.Add(box);
            halfWidths.Add(box[2]);
            halfHeights.Add(box[3]);
        }

        // THE NEW HEADS, each leaderless and so measurable: its size, and where
        // its box sits against its head point.
        var baseHead = new Dictionary<ElementId, XYZ>();
        var shape = new Dictionary<ElementId, double[]>();   // {offset u, offset v, half w, half h}
        var unmeasured = new List<ElementId>();
        foreach (var made in madeElementTags)
        {
            var head = headOf(made);
            if (head == null) continue;
            baseHead[made.Id] = head;
            var box = paperBox(made, view);
            if (box == null) { unmeasured.Add(made.Id); continue; }
            shape[made.Id] = new[] { box[0] - head.DotProduct(right), box[1] - head.DotProduct(up), box[2], box[3] };
            halfWidths.Add(box[2]);
            halfHeights.Add(box[3]);
        }

        // THE MEDIAN, for every head that could not be measured. With nothing
        // measured at all, 20 x 5 paper mm - a size this card chose, and the
        // answer says how many heads were judged by it.
        Func<List<double>, double, double> medianOr = (numbers, fallback) =>
        {
            if (numbers.Count == 0) return fallback;
            var sorted = numbers.OrderBy(n => n).ToList();
            return sorted[sorted.Count / 2];
        };
        if (halfWidths.Count == 0) sizesGuessed += leaderedAt.Count + unmeasured.Count;
        leaderedJudged += leaderedAt.Count;
        double medianHalfWidth = medianOr(halfWidths, 10.0 * MmToFeet * scale);
        double medianHalfHeight = medianOr(halfHeights, 2.5 * MmToFeet * scale);
        foreach (var head in leaderedAt)
            obstacles.Add(new[] { head.DotProduct(right), head.DotProduct(up), medianHalfWidth, medianHalfHeight });
        foreach (var id in unmeasured) shape[id] = new[] { 0.0, 0.0, medianHalfWidth, medianHalfHeight };

        // ---- PLACE, CLEAR AT PLACE TIME ---------------------------------------
        // Top of the drawing first, left to right, so a run reads the same twice.
        // For each new head, the first spot whose box keeps `gap` from every
        // head already placed or already there: on the element; then a head's
        // own size away - above, below, right, left, the four diagonals - with
        // no leader; then, when the rule allows a leader, two and three sizes
        // out with a straight one. With nothing clear it stays on its element
        // and is named in `crowded` - never a silent overlap.
        var order = madeElementTags.Where(t => baseHead.ContainsKey(t.Id))
            .OrderByDescending(t => Math.Round(baseHead[t.Id].DotProduct(up), 6))
            .ThenBy(t => baseHead[t.Id].DotProduct(right)).ToList();
        var directions = new[] { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 },
                                 new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 }, new[] { -1, -1 } };
        var finalBox = new Dictionary<ElementId, double[]>();
        var wanted = new Dictionary<ElementId, XYZ>();

        foreach (var made in order)
        {
            var head = baseHead[made.Id];
            var s = shape[made.Id];
            double u0 = head.DotProduct(right), v0 = head.DotProduct(up);
            double stepU = 2 * s[2] + gapFeet, stepV = 2 * s[3] + gapFeet;

            Func<double, double, double[]> boxAt = (du, dv) => new[] { u0 + du + s[0], v0 + dv + s[1], s[2], s[3] };
            Func<double[], bool> clear = candidate => obstacles.All(o => !clash(candidate, o, gapFeet));

            double pickU = 0, pickV = 0;
            bool found = clear(boxAt(0, 0)), withLeader = false;
            for (int d = 0; !found && d < directions.Length; d++)
            {
                double du = directions[d][0] * stepU, dv = directions[d][1] * stepV;
                if (clear(boxAt(du, dv))) { pickU = du; pickV = dv; found = true; }
            }
            bool leaderAllowedHere;
            madeLeaderAllowed.TryGetValue(made.Id, out leaderAllowedHere);
            for (int ring = 2; !found && leaderAllowedHere && ring <= 3; ring++)
                for (int d = 0; !found && d < directions.Length; d++)
                {
                    double du = directions[d][0] * stepU * ring, dv = directions[d][1] * stepV * ring;
                    if (clear(boxAt(du, dv))) { pickU = du; pickV = dv; found = true; withLeader = true; }
                }
            if (!found) { crowded.Add(made.Id); crowdedHere.Add(made.Id); }

            if (pickU != 0 || pickV != 0)
            {
                var target = head + right * pickU + up * pickV;
                try
                {
                    if (withLeader) made.HasLeader = true;
                    made.TagHeadPosition = target;
                    wanted[made.Id] = target;
                    if (withLeader) { leadersDrawn++; leadersHere++; }
                }
                catch (Exception failure)
                {
                    if (firstRefusal == null) firstRefusal = failure.Message;
                    try { if (withLeader) made.HasLeader = false; } catch { }
                    pickU = 0; pickV = 0;
                    headsNotTaken++;
                    if (!crowdedHere.Contains(made.Id)) { crowded.Add(made.Id); crowdedHere.Add(made.Id); }
                }
            }
            var placedBox = boxAt(pickU, pickV);
            obstacles.Add(placedBox);
            finalBox[made.Id] = placedBox;
        }

        // ---- READ BACK where every new head went, and what it now touches ------
        doc.Regenerate();
        var existingOnly = new List<double[]>();
        foreach (var existing in existingHeads)
        {
            if (leaderOf(existing))
            {
                var head = headOf(existing);
                if (head != null) existingOnly.Add(new[] { head.DotProduct(right), head.DotProduct(up), medianHalfWidth, medianHalfHeight });
                continue;
            }
            var box = paperBox(existing, view);
            if (box != null) existingOnly.Add(box);
        }
        foreach (var made in madeSpatialTags)
        {
            var box = paperBox(made, view);
            if (box != null) finalBox[made.Id] = box;
        }
        foreach (var made in madeElementTags)
        {
            if (!shape.ContainsKey(made.Id)) continue;
            var head = headOf(made);
            if (head == null) continue;
            XYZ asked;
            if (wanted.TryGetValue(made.Id, out asked) && !head.IsAlmostEqualTo(asked)) headsNotTaken++;
            var s = shape[made.Id];
            finalBox[made.Id] = leaderOf(made)
                ? new[] { head.DotProduct(right) + s[0], head.DotProduct(up) + s[1], s[2], s[3] }
                : (paperBox(made, view) ?? new[] { head.DotProduct(right) + s[0], head.DotProduct(up) + s[1], s[2], s[3] });
        }
        var finalIds = finalBox.Keys.ToList();
        for (int i = 0; i < finalIds.Count; i++)
        {
            var mine = finalBox[finalIds[i]];
            bool touches = existingOnly.Any(o => clash(mine, o, -1e-6));
            for (int j = 0; !touches && j < finalIds.Count; j++)
                if (j != i && clash(mine, finalBox[finalIds[j]], -1e-6)) touches = true;
            if (touches && !crowdedHere.Contains(finalIds[i])) { crowded.Add(finalIds[i]); crowdedHere.Add(finalIds[i]); }
        }

        // A TAG CATEGORY TURNED OFF IN THE VIEW: placed, and not shown.
        foreach (var tagCategoryId in ruleTagCategory.Distinct())
        {
            bool hidden = false;
            try { hidden = view.GetCategoryHidden(tagCategoryId); } catch { }
            if (!hidden) continue;
            string name = "tags";
            try { name = Category.GetCategory(doc, tagCategoryId).Name; } catch { }
            hiddenWords.Add(name + " in \"" + view.Name + "\"");
        }
    }

    perView.Add("\"" + view.Name + "\": " + placedHere + " tag(s) placed (" + leadersHere + " with a straight leader, "
        + crowdedHere.Count + " crowded - no clear spot, named in crowded), " + alreadyHere + " already tagged, "
        + risersHere + " riser(s) skipped, " + shortHere + " too short, " + notHere + " not tagged"
        + (outsideCrop > 0 ? ", " + outsideCrop + " room(s), space(s) or zone(s) outside its crop left alone" : "")
        + (spatialNotes.Count > 0 ? "; " + string.Join("; ", spatialNotes.ToArray()) : "") + ".");
}

// ---- THE ANSWER --------------------------------------------------------------
var notTaggedWhy = new List<string>();
foreach (var reason in whyOrder.OrderByDescending(r => whyCount[r]))
    notTaggedWhy.Add(whyCount[reason] + " x " + reason);

if (refused.Length == 0)
{
    findings.Add(tagged + " tag(s) placed in " + working.Count + " view(s) by " + ruleCategory.Count + " rule(s): "
        + leadersDrawn + " drawn out on a straight leader, " + crowded.Count + " crowded (left on their element "
        + "with no clear spot, or touching another head when read back - named in crowded). Not tagged: "
        + alreadyTagged.Count + " already tagged, " + verticalSkipped.Count + " riser(s), " + tooShort.Count
        + " too short, " + notTagged.Count + " for the reasons in notTaggedWhy.");
    if (fromActive)
        findings.Add("No view was named, so the active view \"" + working[0].Name + "\" was tagged.");
    if (viewsLeft.Count > 0)
        findings.Add("Left out: " + string.Join("; ", viewsLeft.ToArray()) + ".");
    if (typeCorrections > 0)
        findings.Add(typeCorrections + " tag(s) came out of Revit as another type and were changed to the rule's "
            + "type - IndependentTag.Create does not always honour the type it is given.");
    if (sizesGuessed > 0)
        findings.Add(sizesGuessed + " tag head(s) could not be measured and were judged at 20 x 5 mm on the paper.");
    if (leaderedJudged > 0)
        findings.Add(leaderedJudged + " tag head(s) already in the views have a leader, and were kept clear of at the "
            + "median size of the leaderless heads - a leadered tag's box wraps its leader, so it cannot be measured.");
    if (headsNotTaken > 0)
        findings.Add(headsNotTaken + " tag head(s) did not go where they were sent - look for them in crowded.");
    if (hiddenWords.Count > 0)
        findings.Add("Turned off in the view, so the tags placed are in the model and NOT SHOWN: "
            + string.Join(", ", hiddenWords.ToArray()) + ".");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    findings.Add(refused);
}
