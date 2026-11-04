// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `tagType` are in
// scope, and leaves `tagged`, `newTags`, `mismatched`, `alreadyTaggedHere`,
// `notAreas`, `otherScheme`, `otherLevel`, `unplaced`, `notEnclosed`,
// `notPlaced`, `typesLoaded`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One area plan of tags is one
// undo entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > AREA TAG. An area tag is its own element, made by the
// document's own call for it - NewAreaTag(ViewPlan, Area, UV), present and
// unchanged 2020 to 2027 (tools/api-surface --members, 2026-11-01) - and NOT
// the tag every other category takes. TAG_ROOMS does the same for rooms and
// spaces with NewRoomTag and NewSpaceTag.
//
// AN AREA TAG GOES ONLY IN AN AREA PLAN. Revit draws areas in an area plan of
// their own scheme and nowhere else, and NewAreaTag takes a ViewPlan. Any other
// view - a floor plan, a section, a 3D view - is refused BY NAME before
// anything is placed, and so is a view template.
//
// THE AREAS ARE HANDED IN, NEVER LOOKED FOR IN THE VIEW. A collector scoped to
// a view returns only what Visibility/Graphics shows - TAG_ROOMS found 0 of a
// model's 7 rooms that way on 2026-09-19. FILTER_ELEMENTS_BY_CATEGORY, with
// Areas and the plan's level, collects them from the whole model; this one
// keeps the ones of the plan's own AREA SCHEME and level and names the rest.
//
// THE TAG TYPE IS THE MODELLER'S AND IS NEVER GUESSED (D-33). "Area Tag" and
// an office's own area tag with its name and area print different drawings.
// Left out, or a name no area tag type has: NOTHING IS PLACED and the loaded
// area tag types come back in `typesLoaded`. A name that matches two is
// refused rather than one picked.
//
// ONE TAG PER AREA PER PLAN, AND AN AREA ALREADY TAGGED IS LEFT ALONE AND
// NAMED. The area tags already in this plan are read from the whole model by
// the view that owns them, so a tag the plan hides still counts and a re-run
// stacks nothing - a duplicate would sit exactly on the first.
//
// EVERY TAG IS READ BACK - its type and the area it points at - and one that is
// wrong is removed again inside the same undo, and its area named.

var tagged = 0;
var newTags = new List<ElementId>();
var mismatched = new List<ElementId>();
var alreadyTaggedHere = new List<ElementId>();
var notAreas = new List<ElementId>();
var otherScheme = new List<ElementId>();
var otherLevel = new List<ElementId>();
var unplaced = new List<ElementId>();
var notEnclosed = new List<ElementId>();
var notPlaced = new List<ElementId>();
var typesLoaded = new List<string>();
var findings = new List<string>();
string refused = "";

var areaTagsCategory = new ElementId(BuiltInCategory.OST_AreaTags);

// How a type reads in the Properties palette - "M_Area Tag: Area Tag".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { family = null; }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// A type is named as Revit shows it: its own name, or "Family: Name" as the
// Properties palette writes it ("Family : Name" read the same).
Func<ElementType, string, bool> isNamed = (t, text) =>
{
    string own;
    try { own = t.Name; } catch { return false; }
    if (string.Equals(own, text, StringComparison.Ordinal)) return true;
    string family;
    try { family = t.FamilyName; } catch { return false; }
    if (string.IsNullOrEmpty(family)) return false;
    return string.Equals(family + ": " + own, text.Replace(" : ", ": "), StringComparison.Ordinal);
};

var areaTagTypes = new FilteredElementCollector(doc)
    .OfCategoryId(areaTagsCategory)
    .WhereElementIsElementType()
    .Cast<ElementType>()
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
typesLoaded.Add("Area tag types: " + (areaTagTypes.Count == 0
    ? "none loaded - load an area tag family (Insert > Load Family, Annotations) first"
    : string.Join(", ", areaTagTypes.Select(t => "'" + typeLabel(t) + "'").ToArray())));

var typeText = (tagType ?? "").Trim();
ElementType chosenType = null;

// ---- refused before anything is placed -------------------------------------
//
// Each of these cannot be right for ANY area, so it stops the whole call with
// one sentence and places nothing - rather than failing area by area.

var plan = view as ViewPlan;
if (view == null)
{
    refused = "No view was named. Area tags go in an area plan; name it as the Project Browser "
            + "shows it under Area Plans. Nothing was placed.";
}
else if (view.IsTemplate)
{
    refused = "\"" + view.Name + "\" is a view template, and a template holds no tags. Name the "
            + "area plan itself. Nothing was placed.";
}
else if (plan == null || view.ViewType != ViewType.AreaPlan)
{
    refused = "\"" + view.Name + "\" is a " + view.ViewType + " view, not an area plan. Revit "
            + "shows areas, and takes area tags, only in an area plan of their own scheme - name "
            + "one from the Project Browser's Area Plans. Nothing was placed.";
}
else if (typeText.Length == 0 || typeText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    refused = "No area tag type was named, and one is never chosen for you - the loaded ones are "
            + "in typesLoaded. Name the one to use. Nothing was placed.";
}
else
{
    var hits = areaTagTypes.Where(t => isNamed(t, typeText)).ToList();
    if (hits.Count == 1)
    {
        chosenType = hits[0];
    }
    else if (hits.Count > 1)
    {
        refused = hits.Count + " area tag types are called '" + typeText + "' - "
                + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                + " - so the name does not say which. Name it with its family. Nothing was placed.";
    }
    else
    {
        // A type of ANOTHER kind by that name - a room tag, a door tag - is
        // named with its own category, so the modeller sees why it cannot do.
        var elsewhere = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .Cast<ElementType>()
            .Where(t => t.Category != null && isNamed(t, typeText))
            .Select(t => "'" + typeLabel(t) + "' (" + t.Category.Name + ")")
            .Distinct()
            .Take(3)
            .ToList();
        refused = elsewhere.Count > 0
            ? "'" + typeText + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not an area tag "
              + "type, so it cannot tag an area. The loaded area tag types are in typesLoaded. "
              + "Nothing was placed."
            : "No area tag type is called '" + typeText + "' - the name has to match the Project "
              + "Browser, capitals included. The loaded ones are in typesLoaded. Nothing was placed.";
    }
}

if (refused.Length == 0)
{
    var planLevel = view.GenLevel;
    AreaScheme planScheme = null;
    try { planScheme = plan.AreaScheme; } catch { planScheme = null; }
    var schemeName = planScheme == null ? "(no scheme)" : planScheme.Name;

    // A family type must be active before Revit places it. Tag types normally
    // are; activating one that is not changes nothing else.
    var tagSymbol = chosenType as FamilySymbol;
    if (tagSymbol != null && !tagSymbol.IsActive) tagSymbol.Activate();

    // ---- the area tags this plan already shows ---------------------------------
    //
    // A DEPENDENT VIEW SHARES ITS ANNOTATION with its primary and the primary's
    // other dependents, so an area tagged in any of them is tagged here too.
    // TAG_ROOMS' rule, NOT yet seen through this fragment on a model.
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

    // BY CATEGORY OVER THE WHOLE MODEL, so a tag the plan hides still counts.
    var taggedHere = new HashSet<ElementId>();
    foreach (var existing in new FilteredElementCollector(doc)
                                 .OfCategoryId(areaTagsCategory)
                                 .WhereElementIsNotElementType())
    {
        if (!viewsSharingTags.Contains(existing.OwnerViewId)) continue;
        var existingTag = existing as AreaTag;
        if (existingTag == null) continue;
        try
        {
            if (existingTag.Area != null) taggedHere.Add(existingTag.Area.Id);
        }
        catch { }
    }

    bool tagsHiddenHere = false;
    try { tagsHiddenHere = view.GetCategoryHidden(areaTagsCategory); } catch { }

    string firstRefusal = null;

    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null) continue;

        // AN AREA, AND NOTHING ELSE. A room, a duct, a door - all here.
        var area = element as Area;
        if (area == null)
        {
            notAreas.Add(element.Id);
            continue;
        }

        // ONE FROM ANOTHER MODEL. Its id means something else in this one.
        bool inThisModel = false;
        try { inThisModel = area.Document != null && area.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notPlaced.Add(area.Id);
            continue;
        }

        // ANOTHER AREA SCHEME. A Gross Building area is not drawn in a Rentable
        // plan, and a tag on it there would label an area nobody can see.
        ElementId areaSchemeId = ElementId.InvalidElementId;
        try { if (area.AreaScheme != null) areaSchemeId = area.AreaScheme.Id; } catch { }
        if (planScheme == null || areaSchemeId != planScheme.Id)
        {
            otherScheme.Add(area.Id);
            continue;
        }

        // Also what stops one handed in twice from getting two tags: it joins
        // this set the moment its first tag is made.
        if (taggedHere.Contains(area.Id))
        {
            alreadyTaggedHere.Add(area.Id);
            continue;
        }

        // NOT PLACED - in a schedule, in no plan. Nowhere to put a tag.
        var placedAt = area.Location as LocationPoint;
        if (placedAt == null || placedAt.Point == null)
        {
            unplaced.Add(area.Id);
            continue;
        }

        // ANOTHER LEVEL. This plan is the drawing of its own level's areas.
        if (planLevel == null || area.LevelId != planLevel.Id)
        {
            otherLevel.Add(area.Id);
            continue;
        }

        // NO AREA - not enclosed by area boundary lines, or redundant.
        double size = 0;
        try { size = area.Area; } catch { size = 0; }
        if (size <= 0)
        {
            notEnclosed.Add(area.Id);
            continue;
        }

        // AT THE AREA'S OWN POINT. Laying tags out is another job.
        var point = placedAt.Point;
        AreaTag made = null;
        try
        {
            made = doc.Create.NewAreaTag(plan, area, new UV(point.X, point.Y));
        }
        catch (Exception failure)
        {
            if (firstRefusal == null) firstRefusal = failure.Message;
        }

        if (made == null)
        {
            notPlaced.Add(area.Id);
            continue;
        }

        // THE TYPE ASKED FOR, SET AND THEN READ BACK. The call makes the
        // project's default area tag; which type it carries is only known by
        // asking it afterwards.
        try
        {
            if (made.GetTypeId() != chosenType.Id) made.ChangeTypeId(chosenType.Id);
        }
        catch (Exception failure)
        {
            if (firstRefusal == null) firstRefusal = failure.Message;
        }

        if (made.GetTypeId() != chosenType.Id)
        {
            // Ours, made a moment ago, and of the wrong type - removed.
            try { doc.Delete(made.Id); } catch { }
            notPlaced.Add(area.Id);
            continue;
        }

        // AND WHAT IT POINTS AT, READ BACK. A tag on the wrong area still
        // prints a confident name and number.
        bool pointsRight = false;
        try { pointsRight = made.Area != null && made.Area.Id == area.Id; } catch { pointsRight = false; }
        if (!pointsRight)
        {
            try { doc.Delete(made.Id); } catch { }
            mismatched.Add(area.Id);
            continue;
        }

        newTags.Add(made.Id);
        taggedHere.Add(area.Id);
        tagged++;
    }

    // THE WHOLE ANSWER AS ONE LINE - a reply shows a list's first entries and
    // its count, and this line carries every count in words.
    findings.Add(tagged + " area tag(s) placed in \"" + view.Name + "\" (" + schemeName + ") as \""
        + typeLabel(chosenType) + "\". Not tagged: " + alreadyTaggedHere.Count + " already tagged "
        + "here, " + notAreas.Count + " not areas, " + otherScheme.Count + " of another area scheme, "
        + otherLevel.Count + " on another level, " + unplaced.Count + " not placed, "
        + notEnclosed.Count + " not enclosed, " + notPlaced.Count + " refused by Revit or of another "
        + "model, " + mismatched.Count + " read back on the wrong area and removed.");

    if (tagsHiddenHere && tagged > 0)
        findings.Add("Area Tags are turned off in \"" + view.Name + "\" - in its Visibility/Graphics "
            + "or its view template - so the " + tagged + " tag(s) placed are in the model and not "
            + "shown in that plan.");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    findings.Add(refused);
}
