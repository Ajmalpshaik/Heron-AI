// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `tagType` and
// `addLeader` are in scope, and leaves `tagged`, `newTags`, `mismatched`,
// `alreadyTaggedHere`, `notTaggable`, `notPlaced`, `typesLoaded`, `refused`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One view of tags is one undo
// entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > MATERIAL TAG AND MULTI-CATEGORY TAG. Both are the tag
// every model category takes - an IndependentTag - made by
// IndependentTag.Create(doc, tagTypeId, viewId, reference, addLeader,
// orientation, point), present and unchanged 2020 to 2027 with its TagMode
// sibling (tools/api-surface --members, 2026-11-01). WHICH ONE IS DECIDED BY
// THE TAG TYPE THE MODELLER NAMES: a Material Tags type makes material tags, a
// Multi-Category Tags type multi-category tags. One call makes one kind.
//
// THE TAG TYPE IS NEVER GUESSED (D-33). Left out, or a name neither kind of tag
// type has: NOTHING IS PLACED and the loaded material and multi-category tag
// types come back in `typesLoaded`. A name that matches two is refused, and a
// tag type of another kind - a door tag, a room tag - is named with its own
// category, so the modeller sees why it cannot do.
//
// AN ELEMENT ALREADY CARRYING THAT KIND OF TAG IN THIS VIEW IS LEFT ALONE AND
// NAMED. Only that kind counts: a wall with a wall tag still takes a material
// tag. The tags are read from the whole model by the view that owns them, so
// a tag the view hides still counts and a re-run stacks nothing.
//
// A MATERIAL TAG READS A MATERIAL, so an element with none is named and left.
// Revit tags a material on the element itself where it can; where it refuses
// the element, the faces Revit draws in this view are tried, largest first,
// each carrying a material. WHICH OF THE TWO REVIT TAKES IS NOT KNOWN FROM THE
// ASSEMBLIES - NEEDS-CHECKING group KI measures it.
//
// EVERY TAG IS READ BACK - its kind, its type and the element it points at -
// and one that is wrong is removed again inside the same undo.
//
// IT PLACES TAGS AT THE ELEMENT AND DOES NOT LAY THEM OUT - TAG_ELEMENTS' rule.

var tagged = 0;
var newTags = new List<ElementId>();
var mismatched = new List<ElementId>();
var alreadyTaggedHere = new List<ElementId>();
var notTaggable = new List<ElementId>();
var notPlaced = new List<ElementId>();
var typesLoaded = new List<string>();
var findings = new List<string>();
string refused = "";

var materialTagsCategory = new ElementId(BuiltInCategory.OST_MaterialTags);
var multiTagsCategory = new ElementId(BuiltInCategory.OST_MultiCategoryTags);

// How a type reads in the Properties palette - "M_Material Tag: Material Name".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { family = null; }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// A type is named as Revit shows it: its own name, or "Family: Name" ("Family
// : Name" read the same).
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

// WHAT A TAG POINTS AT, on every release: the 2022-and-later method where
// Revit has it, the 2020/2021 property where it does not. Found by name,
// because the add-in compiles fragments with no release symbols (row 5b-181)
// - TAG_ROOMS' and OFFSET_TAGS_FROM_HOST's pattern.
Func<IndependentTag, List<ElementId>> tagTargets = t =>
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

Func<ElementId, List<ElementType>> typesOf = categoryId => new FilteredElementCollector(doc)
    .OfCategoryId(categoryId)
    .WhereElementIsElementType()
    .Cast<ElementType>()
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
var materialTagTypes = typesOf(materialTagsCategory);
var multiTagTypes = typesOf(multiTagsCategory);
Func<List<ElementType>, string> listOf = list => list.Count == 0
    ? "none loaded"
    : string.Join(", ", list.Select(t => "'" + typeLabel(t) + "'").ToArray());
typesLoaded.Add("Material tag types: " + listOf(materialTagTypes));
typesLoaded.Add("Multi-category tag types: " + listOf(multiTagTypes));

var typeText = (tagType ?? "").Trim();
ElementType chosenType = null;
bool forMaterial = false;

// ---- refused before anything is placed -------------------------------------
//
// Each of these cannot be right for ANY element, so it stops the whole call
// with one sentence and places nothing.

var viewTakesTags = view != null && !view.IsTemplate
    && (view.ViewType == ViewType.FloorPlan || view.ViewType == ViewType.CeilingPlan
        || view.ViewType == ViewType.EngineeringPlan || view.ViewType == ViewType.AreaPlan
        || view.ViewType == ViewType.Section || view.ViewType == ViewType.Elevation
        || view.ViewType == ViewType.Detail
        || (view.ViewType == ViewType.ThreeD && view is View3D && ((View3D)view).IsLocked));

if (view == null)
{
    refused = "No view was named. Name the plan, section or elevation the tags go in, as the "
            + "Project Browser shows it. Nothing was placed.";
}
else if (view.IsTemplate)
{
    refused = "\"" + view.Name + "\" is a view template, and a template holds no tags. Name the "
            + "view itself. Nothing was placed.";
}
else if (!viewTakesTags)
{
    refused = "\"" + view.Name + "\" is a " + view.ViewType + " view"
            + (view.ViewType == ViewType.ThreeD ? " that is not locked" : "")
            + ", and Revit places these tags only in a plan, a section, an elevation, a detail "
            + "view or a locked 3D view. Name one of those. Nothing was placed.";
}
else if (typeText.Length == 0 || typeText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    refused = "No tag type was named, and one is never chosen for you. Name a material tag type "
            + "for material tags or a multi-category tag type for multi-category tags - the "
            + "loaded ones are in typesLoaded. Nothing was placed.";
}
else
{
    var hits = materialTagTypes.Concat(multiTagTypes).Where(t => isNamed(t, typeText)).ToList();
    if (hits.Count == 1)
    {
        chosenType = hits[0];
        forMaterial = chosenType.Category != null && chosenType.Category.Id == materialTagsCategory;
    }
    else if (hits.Count > 1)
    {
        refused = hits.Count + " tag types are called '" + typeText + "' - "
                + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "' ("
                    + (t.Category == null ? "?" : t.Category.Name) + ")").ToArray())
                + " - so the name does not say which. Name it with its family. Nothing was placed.";
    }
    else
    {
        var elsewhere = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .Cast<ElementType>()
            .Where(t => t.Category != null && isNamed(t, typeText))
            .Select(t => "'" + typeLabel(t) + "' (" + t.Category.Name + ")")
            .Distinct()
            .Take(3)
            .ToList();
        refused = elsewhere.Count > 0
            ? "'" + typeText + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not a material "
              + "or multi-category tag type. The loaded ones are in typesLoaded. Nothing was placed."
            : "No material or multi-category tag type is called '" + typeText + "' - the name has "
              + "to match the Project Browser, capitals included. The loaded ones are in "
              + "typesLoaded. Nothing was placed.";
    }
}

if (refused.Length == 0)
{
    var kindWord = forMaterial ? "material" : "multi-category";

    // A family type must be active before Revit places it.
    var tagSymbol = chosenType as FamilySymbol;
    if (tagSymbol != null && !tagSymbol.IsActive) tagSymbol.Activate();

    // ---- the tags of this kind this view already shows ---------------------------
    //
    // A DEPENDENT VIEW SHARES ITS ANNOTATION with its primary and the primary's
    // other dependents - TAG_ROOMS' rule, NOT yet seen through this fragment.
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

    // OVER THE WHOLE MODEL, kept when this view owns them - so a tag the view
    // hides still counts. Only THIS kind: a door tag does not make a door
    // multi-category tagged.
    var taggedHere = new HashSet<ElementId>();
    foreach (IndependentTag existing in new FilteredElementCollector(doc)
                                            .OfClass(typeof(IndependentTag))
                                            .WhereElementIsNotElementType())
    {
        if (!viewsSharingTags.Contains(existing.OwnerViewId)) continue;
        bool sameKind = false;
        try { sameKind = forMaterial ? existing.IsMaterialTag : existing.IsMulticategoryTag; }
        catch { sameKind = false; }
        if (!sameKind) continue;
        try { foreach (var id in tagTargets(existing)) taggedHere.Add(id); } catch { }
    }

    var tagsCategory = forMaterial ? materialTagsCategory : multiTagsCategory;
    bool tagsHiddenHere = false;
    try { tagsHiddenHere = view.GetCategoryHidden(tagsCategory); } catch { }

    // Why each one not tagged was left, one line per reason with a count.
    var whyCount = new Dictionary<string, int>();
    Action<string> because = reason =>
    {
        int seen;
        whyCount[reason] = whyCount.TryGetValue(reason, out seen) ? seen + 1 : 1;
    };

    string firstRefusal = null;
    var samples = new List<string>();
    // Material tags Revit took only on a face, not on the element - the count
    // NEEDS-CHECKING group KI asks for.
    int onFace = 0;

    // The faces Revit draws for this element in this view, largest first, each
    // with a reference and a material - what a material tag can point at when
    // Revit will not take the element itself.
    Func<Element, List<Reference>> faceReferences = element =>
    {
        var faces = new List<KeyValuePair<double, Reference>>();
        try
        {
            var options = new Options();
            options.ComputeReferences = true;
            options.View = view;
            var geometry = element.get_Geometry(options);
            if (geometry == null) return new List<Reference>();
            var pending = new List<GeometryObject>();
            foreach (GeometryObject piece in geometry) pending.Add(piece);
            for (int i = 0; i < pending.Count && i < 500; i++)
            {
                var instance = pending[i] as GeometryInstance;
                if (instance != null)
                {
                    // An instance's own symbol geometry carries the references
                    // that point back into the family instance.
                    var inner = instance.GetSymbolGeometry();
                    if (inner != null) foreach (GeometryObject piece in inner) pending.Add(piece);
                    continue;
                }
                var solid = pending[i] as Solid;
                if (solid == null || solid.Faces == null) continue;
                foreach (Face face in solid.Faces)
                {
                    if (face == null || face.Reference == null) continue;
                    if (face.MaterialElementId == null
                        || face.MaterialElementId == ElementId.InvalidElementId) continue;
                    faces.Add(new KeyValuePair<double, Reference>(face.Area, face.Reference));
                }
            }
        }
        catch { }
        return faces.OrderByDescending(f => f.Key).Take(6).Select(f => f.Value).ToList();
    };

    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null) continue;

        // NOT A MODEL ELEMENT IN THIS MODEL - a type, a view, a tag, a line of
        // text, or one from a link. A tag tags model elements of this model.
        bool inThisModel = false;
        try { inThisModel = element.Document != null && element.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notTaggable.Add(element.Id);
            because("from another model");
            continue;
        }
        if (element is ElementType || element is View || element.Category == null
            || element.Category.CategoryType != CategoryType.Model)
        {
            notTaggable.Add(element.Id);
            because("not a model element (a type, a view, an annotation or a line)");
            continue;
        }

        // A ROOM, SPACE OR AREA TAKES ITS OWN KIND OF TAG - TAG_ROOMS and
        // TAG_AREAS - and has no material of its own.
        if (element is SpatialElement)
        {
            notTaggable.Add(element.Id);
            because("a room, space or area, which takes its own kind of tag - " + element.Category.Name);
            continue;
        }

        if (taggedHere.Contains(element.Id))
        {
            alreadyTaggedHere.Add(element.Id);
            continue;
        }

        // A MATERIAL TAG READS A MATERIAL. An element with none prints "?".
        if (forMaterial)
        {
            int materials = 0;
            try { materials = element.GetMaterialIds(false).Count; } catch { materials = 0; }
            if (materials == 0)
            {
                notTaggable.Add(element.Id);
                because("no material to tag - " + element.Category.Name);
                continue;
            }
        }

        // WHERE: the element's own point, the middle of its line, or the centre
        // of its extent in this view - TAG_ELEMENTS' order.
        XYZ point = null;
        var atCurve = element.Location as LocationCurve;
        if (atCurve != null && atCurve.Curve != null)
        {
            try { point = atCurve.Curve.Evaluate(0.5, true); } catch { point = null; }
        }
        if (point == null)
        {
            var atPoint = element.Location as LocationPoint;
            if (atPoint != null) point = atPoint.Point;
        }
        if (point == null)
        {
            BoundingBoxXYZ extent = null;
            try { extent = element.get_BoundingBox(view) ?? element.get_BoundingBox(null); }
            catch { extent = null; }
            if (extent != null) point = (extent.Min + extent.Max) * 0.5;
        }
        if (point == null)
        {
            notTaggable.Add(element.Id);
            because("no location or extent to put a tag at - " + element.Category.Name);
            continue;
        }

        // MAKE IT. The element first; for a material tag Revit refuses there,
        // the faces it draws in this view.
        IndependentTag made = null;
        string thisRefusal = null;
        var references = new List<Reference>();
        try { references.Add(new Reference(element)); } catch { }
        int onElementCount = references.Count;
        if (forMaterial) references.AddRange(faceReferences(element));
        bool madeOnFace = false;
        for (int r = 0; r < references.Count && made == null; r++)
        {
            try
            {
                made = IndependentTag.Create(doc, chosenType.Id, view.Id, references[r], addLeader,
                                             TagOrientation.Horizontal, point);
                madeOnFace = made != null && r >= onElementCount;
            }
            catch (Exception failure)
            {
                made = null;
                if (thisRefusal == null) thisRefusal = failure.Message;
            }
        }

        if (made == null)
        {
            if (firstRefusal == null) firstRefusal = thisRefusal;
            notPlaced.Add(element.Id);
            because("refused by Revit - " + element.Category.Name);
            continue;
        }

        // THE KIND AND THE TYPE ASKED FOR, READ BACK. Revit has been seen to
        // give a tag the document's default type instead of the one passed -
        // TAG_ELEMENTS_IN_VIEW measured it - so it is set again and asked.
        try
        {
            if (made.GetTypeId() != chosenType.Id) made.ChangeTypeId(chosenType.Id);
        }
        catch (Exception failure)
        {
            if (firstRefusal == null) firstRefusal = failure.Message;
        }
        bool rightKind = false;
        try { rightKind = forMaterial ? made.IsMaterialTag : made.IsMulticategoryTag; }
        catch { rightKind = false; }
        if (made.GetTypeId() != chosenType.Id || !rightKind)
        {
            // Ours, made a moment ago, and not what was asked for - removed.
            try { doc.Delete(made.Id); } catch { }
            notPlaced.Add(element.Id);
            because("Revit made a tag of another kind or type, removed again - "
                + element.Category.Name);
            continue;
        }

        // AND WHAT IT POINTS AT. A tag on the wrong element still prints.
        bool pointsRight = false;
        try { pointsRight = tagTargets(made).Contains(element.Id); } catch { pointsRight = false; }
        if (!pointsRight)
        {
            try { doc.Delete(made.Id); } catch { }
            mismatched.Add(element.Id);
            continue;
        }

        newTags.Add(made.Id);
        taggedHere.Add(element.Id);
        tagged++;
        if (madeOnFace) onFace++;

        // WHAT IT SAYS, for the first few - a material tag reading "?" is a
        // material with no description, and that is worth seeing in the answer.
        if (samples.Count < 5)
        {
            string text = null;
            try { text = made.TagText; } catch { text = null; }
            string name = null;
            try { name = element.Name; } catch { name = null; }
            samples.Add(element.Category.Name + " " + (name ?? "") + ": \""
                + (string.IsNullOrEmpty(text) ? "(blank)" : text) + "\"");
        }
    }

    // THE WHOLE ANSWER AS ONE LINE.
    findings.Add(tagged + " " + kindWord + " tag(s) placed in \"" + view.Name + "\" as \""
        + typeLabel(chosenType) + "\"" + (addLeader ? " with leaders" : "") + ". Not tagged: "
        + alreadyTaggedHere.Count + " already carrying a " + kindWord + " tag here, "
        + notTaggable.Count + " not taggable, " + notPlaced.Count + " refused by Revit, "
        + mismatched.Count + " read back on the wrong element and removed.");
    foreach (var pair in whyCount.OrderByDescending(p => p.Value))
        findings.Add(pair.Value + " x " + pair.Key);
    if (forMaterial && tagged > 0)
        findings.Add(onFace + " of the " + tagged + " material tag(s) were taken by Revit on a face "
            + "of the element rather than on the element itself.");
    if (samples.Count > 0)
        findings.Add("The tags read: " + string.Join("; ", samples.ToArray()));
    if (tagsHiddenHere && tagged > 0)
        findings.Add((forMaterial ? "Material Tags" : "Multi-Category Tags") + " are turned off in \""
            + view.Name + "\" - in its Visibility/Graphics or its view template - so the "
            + tagged + " tag(s) placed are in the model and not shown in that view.");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    findings.Add(refused);
}
