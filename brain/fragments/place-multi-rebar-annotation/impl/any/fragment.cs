// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `annotationType`,
// `style`, `direction`, `offsetMm` and `addLeader` are in scope, and leaves
// `placed`, `annotationId`, `dimensionId`, `tagId`, `dimensioned`, `notRebar`,
// `notVisible`, `notAligned`, `alreadyAnnotated`, `typesLoaded`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The annotation is one undo
// entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > MULTI-REBAR, ALIGNED OR LINEAR. MultiReferenceAnnotation.
// Create(Document, ElementId viewId, MultiReferenceAnnotationOptions) makes ONE
// annotation - a dimension across the bars and, when its type names a tag
// family, one tag - as children of it. Every member used here is present with
// the same signature 2020 to 2027 (tools/api-surface --members, 2026-11-01).
//
// ALIGNED IS DimensionStyleType.Linear AND LINEAR IS LinearFixed. Revit's own
// documentation, every release: AreReferencesValidForLinearDimension checks
// that "the references are valid for an aligned multi-reference annotation"
// when the style is Linear; LinearFixed keeps the dimension line on the view's
// horizontal or vertical (IsLinearFixedDimensionDirectionValid). Only those two
// are allowed (MultiReferenceAnnotationOptions.IsAllowedDimensionStyleType).
//
// THE ANNOTATION TYPE IS THE MODELLER'S AND IS NEVER GUESSED (D-33). It names
// the tag family and the dimension style. Left out, or a name no multi-rebar
// annotation type has: NOTHING IS PLACED and the loaded ones come back in
// `typesLoaded`, each with its reference category. Even one loaded type is not
// chosen for you. A name that matches two is refused rather than one picked.
//
// EACH BAR IS CHECKED BEFORE ANYTHING IS PLACED, and left with its reason:
//   notRebar          not of the type's reference category - Revit allows
//                     only Structural Rebar there (IsAllowedReferenceCategory)
//   notVisible        not drawn in this view, by the view's own collector -
//                     hidden, cropped out, not cut, of another model
//   alreadyAnnotated  already witnessed by a multi-rebar annotation's
//                     dimension in this view, so a re-run stacks nothing
//   notAligned        refused by REVIT'S OWN CHECK for this style and
//                     direction, bar by bar: an Aligned dimension wants bars
//                     that "appear as points in the view or be linear
//                     references which are perpendicular to the dimension
//                     line", a Linear one points only (Revit's text for Create)
// Then the bars left are checked TOGETHER by the same calls -
// ElementsMatchReferenceCategory, AreElementsValidForMultiReferenceAnnotation,
// AreReferencesValidFor(Linear|LinearFixed)Dimension, IsLinearFixedDimension-
// DirectionValid and, in a 3D view, Is3DViewValidForDimension - and if any says
// no, NOTHING IS PLACED and the failing check is named.
//
// WHERE THE DIMENSION LINE RUNS. `direction` horizontal or vertical is the
// view's own right or up. Left out, it runs along the spread of the first bar
// set handed in that is spread across the view - its shape-driven Normal
// (Rebar) or its Normal (RebarInSystem), laid into the view plane - and for
// Linear the nearer of the view's horizontal and vertical. With no set spread
// across the view, nothing is placed and the modeller is asked for one. The
// line sits beside the bars, `offsetMm` PAPER millimetres past their extent in
// the view (0 or left out: 10), times the view scale, / 304.8 to feet (D-20);
// the tag head as far again beyond it.
//
// EVERYTHING MADE IS READ BACK - its type, the dimension in this view
// witnessing the bars, the tag owned by this annotation - and one that reads
// wrong is removed again inside the same undo. No ElementId is built from a
// number or read as one.

const double MmPerFoot = 304.8;
const double DefaultPaperMm = 10.0;

var placed = 0;
ElementId annotationId = ElementId.InvalidElementId;
ElementId dimensionId = ElementId.InvalidElementId;
ElementId tagId = ElementId.InvalidElementId;
var dimensioned = new List<ElementId>();
var notRebar = new List<ElementId>();
var notVisible = new List<ElementId>();
var notAligned = new List<ElementId>();
var alreadyAnnotated = new List<ElementId>();
var typesLoaded = new List<string>();
var findings = new List<string>();
var lines = new List<string>();
string refused = "";

// How a type reads in the Properties palette - "Multi-Rebar Annotation: Standard".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { family = null; }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// A type is named as Revit shows it: its own name, or "Family: Name".
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

Func<ElementId, string> categoryName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "no category";
    var category = Category.GetCategory(doc, id);
    return category == null ? "an unknown category" : category.Name;
};

Func<ElementId, string> elementTypeLabel = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return null;
    return typeLabel(doc.GetElement(id) as ElementType);
};

// ---- what the model has, listed whether or not a type was named ------------

var annotationTypes = new FilteredElementCollector(doc)
    .OfClass(typeof(MultiReferenceAnnotationType))
    .Cast<MultiReferenceAnnotationType>()
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
if (annotationTypes.Count == 0)
{
    typesLoaded.Add("No multi-rebar annotation type is in this model - one has to exist (Annotate > "
        + "Multi-Rebar, its type properties) before Heron can place one.");
}
foreach (var t in annotationTypes)
{
    var tagLabel = elementTypeLabel(t.TagTypeId);
    var styleLabel = elementTypeLabel(t.DimensionStyleId);
    typesLoaded.Add("'" + typeLabel(t) + "' - reference category " + categoryName(t.ReferenceCategoryId)
        + "; tag " + (tagLabel == null ? "none set, so Revit places the dimension and no tag" : "'" + tagLabel + "'")
        + "; dimension style " + (styleLabel == null ? "none" : "'" + styleLabel + "'"));
}

// ---- the values, refused before anything is placed -------------------------

var typeText = (annotationType ?? "").Trim();
var styleText = (style ?? "").Trim().ToLowerInvariant();
var directionText = (direction ?? "").Trim().ToLowerInvariant();
if (directionText == "none") directionText = "";

DimensionStyleType styleType = DimensionStyleType.Linear;
string styleWord = "Aligned";
MultiReferenceAnnotationType chosenType = null;
var view3D = view as View3D;

if (view == null)
{
    refused = "No view was named. A multi-rebar annotation goes in a plan, section, elevation, detail view "
            + "or locked 3D view; name it as the Project Browser shows it. Nothing was placed.";
}
else if (view.IsTemplate)
{
    refused = "\"" + view.Name + "\" is a view template, and a template holds no annotation. Name the view "
            + "itself. Nothing was placed.";
}
else if (view.ViewType != ViewType.FloorPlan && view.ViewType != ViewType.CeilingPlan
      && view.ViewType != ViewType.EngineeringPlan && view.ViewType != ViewType.Section
      && view.ViewType != ViewType.Elevation && view.ViewType != ViewType.Detail
      && view.ViewType != ViewType.ThreeD)
{
    refused = "\"" + view.Name + "\" is a " + view.ViewType + " view. Revit places a multi-rebar annotation in "
            + "a plan, section, elevation, detail view or locked 3D view - name one. Nothing was placed.";
}
else if (view3D != null && view3D.IsPerspective)
{
    refused = "\"" + view.Name + "\" is a perspective view, and Revit places no multi-rebar annotation in one. "
            + "Nothing was placed.";
}
else if (view3D != null && !view3D.IsLocked)
{
    refused = "\"" + view.Name + "\" is a 3D view that is not locked, and Revit places a multi-rebar annotation "
            + "only in a locked 3D view - lock it first. Nothing was placed.";
}
else if (styleText.Length > 0 && styleText != "none" && styleText != "aligned" && styleText != "linear")
{
    refused = "'" + style + "' is not a multi-rebar annotation style. Say aligned (the dimension across the "
            + "bars) or linear (kept horizontal or vertical in the view). Nothing was placed.";
}
else if (directionText.Length > 0 && directionText != "horizontal" && directionText != "vertical")
{
    refused = "'" + direction + "' is not a direction for the dimension line. Say horizontal or vertical in "
            + "the view, or leave it out to run along the bars' own spread. Nothing was placed.";
}
else if (typeText.Length == 0 || typeText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    refused = "No multi-rebar annotation type was named, and one is never chosen for you - it decides the tag "
            + "family and the dimension style. The loaded ones are in typesLoaded. Nothing was placed.";
}
else
{
    if (styleText == "linear") { styleType = DimensionStyleType.LinearFixed; styleWord = "Linear"; }

    var hits = annotationTypes.Where(t => isNamed(t, typeText)).ToList();
    if (hits.Count == 1)
    {
        chosenType = hits[0];
    }
    else if (hits.Count > 1)
    {
        refused = hits.Count + " multi-rebar annotation types are called '" + typeText + "' - "
                + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                + " - so the name does not say which. Name it with its family. Nothing was placed.";
    }
    else
    {
        // A type of ANOTHER kind by that name - a rebar tag, a dimension style -
        // is named with its own category, so the modeller sees why it cannot do.
        var elsewhere = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .Cast<ElementType>()
            .Where(t => isNamed(t, typeText))
            .Select(t => "'" + typeLabel(t) + "' (" + (t.Category != null ? t.Category.Name : "no category") + ")")
            .Distinct()
            .Take(3)
            .ToList();
        refused = elsewhere.Count > 0
            ? "'" + typeText + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not a multi-rebar "
              + "annotation type. The loaded ones are in typesLoaded. Nothing was placed."
            : "No multi-rebar annotation type is called '" + typeText + "' - the name has to match the "
              + "Project Browser, capitals included. The loaded ones are in typesLoaded. Nothing was placed.";
    }
}

// Revit's own word on the style.
if (refused.Length == 0 && !new MultiReferenceAnnotationOptions(chosenType).IsAllowedDimensionStyleType(styleType))
{
    refused = "Revit does not allow the " + styleWord + " style for a multi-rebar annotation here. Nothing was placed.";
}

// ---- the bars, one by one --------------------------------------------------

var candidates = new List<Element>();
if (refused.Length == 0)
{
    var referenceCategory = chosenType.ReferenceCategoryId;

    // DRAWN IN THIS VIEW, by the view's own collector.
    var visibleHere = new HashSet<ElementId>(new FilteredElementCollector(doc, view.Id)
        .OfCategoryId(referenceCategory)
        .WhereElementIsNotElementType()
        .ToElementIds());

    // ALREADY WITNESSED by a multi-rebar annotation's dimension in this view.
    var witnessedHere = new HashSet<ElementId>();
    foreach (var existing in new FilteredElementCollector(doc)
        .OfClass(typeof(MultiReferenceAnnotation))
        .Cast<MultiReferenceAnnotation>())
    {
        if (existing.OwnerViewId != view.Id) continue;
        var existingDimension = doc.GetElement(existing.DimensionId) as Dimension;
        if (existingDimension == null) continue;
        foreach (Reference witnessedBy in existingDimension.References)
            if (witnessedBy != null) witnessedHere.Add(witnessedBy.ElementId);
    }

    var seen = new HashSet<ElementId>();
    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null || !seen.Add(element.Id)) continue;

        bool inThisModel = false;
        try { inThisModel = element.Document != null && element.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notVisible.Add(element.Id);
            lines.Add(element.Id + ": of another model - not drawn in this view. Left alone.");
            continue;
        }
        if (element.Category == null || element.Category.Id != referenceCategory)
        {
            notRebar.Add(element.Id);
            lines.Add(element.Id + " (" + (element.Category != null ? element.Category.Name : "no category")
                + "): not " + categoryName(referenceCategory) + ", which this annotation type dimensions. Left alone.");
            continue;
        }
        if (!visibleHere.Contains(element.Id))
        {
            notVisible.Add(element.Id);
            lines.Add(element.Id + ": not drawn in \"" + view.Name + "\" - hidden, cropped out or not cut by it. Left alone.");
            continue;
        }
        if (witnessedHere.Contains(element.Id))
        {
            alreadyAnnotated.Add(element.Id);
            lines.Add(element.Id + ": already dimensioned by a multi-rebar annotation in this view. Left alone.");
            continue;
        }
        candidates.Add(element);
    }

    if (candidates.Count == 0)
        refused = "None of the " + seen.Count + " element(s) handed in is a bar drawn in \"" + view.Name
                + "\" and not yet annotated there - see the reasons. Nothing was placed.";
}

// ---- the dimension line's direction ----------------------------------------

XYZ along = null;
XYZ planeNormal = null;
if (refused.Length == 0)
{
    planeNormal = view.ViewDirection.Normalize();
    var right = view.RightDirection.Normalize();
    var up = view.UpDirection.Normalize();

    if (directionText == "horizontal") along = right;
    else if (directionText == "vertical") along = up;
    else
    {
        foreach (var bar in candidates)
        {
            XYZ spread = null;
            var rebar = bar as Rebar;
            var inSystem = bar as RebarInSystem;
            try
            {
                if (rebar != null && rebar.IsRebarShapeDriven()) spread = rebar.GetShapeDrivenAccessor().Normal;
                else if (inSystem != null) spread = inSystem.Normal;
            }
            catch (Exception) { spread = null; }
            if (spread == null) continue;

            var inPlane = spread - planeNormal * spread.DotProduct(planeNormal);
            if (inPlane.GetLength() < 1e-6) continue;           // spread into the view, not across it
            along = inPlane.Normalize();
            lines.Add("The dimension line runs along the spread of bar set " + bar.Id + ".");
            break;
        }
        if (along != null && styleType == DimensionStyleType.LinearFixed)
            along = Math.Abs(along.DotProduct(right)) >= Math.Abs(along.DotProduct(up)) ? right : up;
        if (along == null)
            refused = "No bar set handed in is spread across \"" + view.Name + "\", so the dimension line has "
                    + "no direction of its own. Say horizontal or vertical. Nothing was placed.";
    }
}

// ---- where it goes ---------------------------------------------------------

XYZ origin = null;
XYZ tagHead = null;
if (refused.Length == 0)
{
    var across = planeNormal.CrossProduct(along).Normalize();
    var paperMm = offsetMm > 0 ? offsetMm : DefaultPaperMm;
    var offsetFeet = paperMm * view.Scale / MmPerFoot;

    // The bars' extent in the view, from each bar's box in it.
    double minAlong = double.MaxValue, maxAlong = double.MinValue, maxAcross = double.MinValue;
    XYZ sum = XYZ.Zero;
    int corners = 0;
    foreach (var bar in candidates)
    {
        var box = bar.get_BoundingBox(view) ?? bar.get_BoundingBox(null);
        if (box == null) continue;
        foreach (var x in new[] { box.Min.X, box.Max.X })
            foreach (var y in new[] { box.Min.Y, box.Max.Y })
                foreach (var z in new[] { box.Min.Z, box.Max.Z })
                {
                    var corner = box.Transform.OfPoint(new XYZ(x, y, z));
                    sum = sum + corner;
                    corners++;
                    minAlong = Math.Min(minAlong, corner.DotProduct(along));
                    maxAlong = Math.Max(maxAlong, corner.DotProduct(along));
                    maxAcross = Math.Max(maxAcross, corner.DotProduct(across));
                }
    }
    if (corners == 0)
    {
        refused = "Revit gave no extent for any of the bars in \"" + view.Name + "\", so there is nowhere to put "
                + "the dimension line. Nothing was placed.";
    }
    else
    {
        var centre = sum / corners;
        origin = centre
            + along * ((minAlong + maxAlong) / 2.0 - centre.DotProduct(along))
            + across * (maxAcross - centre.DotProduct(across) + offsetFeet);
        tagHead = origin + across * offsetFeet;
    }
}

// The options for a set of bars - the same values for every check and for the
// one placed.
Func<ICollection<ElementId>, MultiReferenceAnnotationOptions> optionsFor = ids =>
{
    var made = new MultiReferenceAnnotationOptions(chosenType);
    made.DimensionStyleType = styleType;
    made.SetElementsToDimension(ids);
    made.DimensionLineDirection = along;
    made.DimensionPlaneNormal = planeNormal;
    made.DimensionLineOrigin = origin;
    made.TagHeadPosition = tagHead;
    made.TagHasLeader = addLeader;
    return made;
};

// REVIT'S OWN CHECKS, in the order a refusal is easiest to act on. Null when
// every one passes.
Func<ICollection<ElementId>, string> whyRevitRefusesAsked = null;
Func<ICollection<ElementId>, string> whyRevitRefuses = ids =>
{
    // A check that throws is a refusal too, said in Revit's words.
    try { return whyRevitRefusesAsked(ids); }
    catch (Exception failure) { return "Revit refused it while checking - " + failure.Message; }
};
whyRevitRefusesAsked = ids =>
{
    var asked = optionsFor(ids);
    if (!asked.ElementsMatchReferenceCategory(ids)
        || !MultiReferenceAnnotation.AreElementsValidForMultiReferenceAnnotation(doc, asked))
        return "Revit's check says it is not of the annotation type's reference category";
    if (styleType == DimensionStyleType.Linear
        && !MultiReferenceAnnotation.AreReferencesValidForLinearDimension(doc, view.Id, asked))
        return "Revit's check for an Aligned annotation says it neither shows as points in this view nor runs square to the dimension line";
    if (styleType == DimensionStyleType.LinearFixed
        && !MultiReferenceAnnotation.IsLinearFixedDimensionDirectionValid(doc, view.Id, asked))
        return "Revit's check for a Linear annotation says the dimension line is not on the view's horizontal or vertical";
    if (styleType == DimensionStyleType.LinearFixed
        && !MultiReferenceAnnotation.AreReferencesValidForLinearFixedDimension(doc, view.Id, asked))
        return "Revit's check for a Linear annotation says it does not show as points in this view";
    if (view3D != null && !MultiReferenceAnnotation.Is3DViewValidForDimension(doc, view.Id, asked))
        return "Revit's check says this 3D view cannot carry it - a Linear annotation never can, an Aligned one not when the view looks along the work plane";
    return null;
};

// ---- bar by bar, then all together -----------------------------------------

var together = new List<ElementId>();
if (refused.Length == 0)
{
    foreach (var bar in candidates)
    {
        var why = whyRevitRefuses(new List<ElementId> { bar.Id });
        if (why == null) { together.Add(bar.Id); continue; }
        notAligned.Add(bar.Id);
        lines.Add(bar.Id + ": " + why + ". Left out of the annotation.");
    }
    if (together.Count == 0)
    {
        refused = "Every bar left was refused by Revit's own check for an " + styleWord + " annotation along this "
                + "direction - see the reasons. Nothing was placed.";
    }
    else
    {
        var whyAll = whyRevitRefuses(together);
        if (whyAll != null)
            refused = "The " + together.Count + " bar(s) pass one by one and not together: " + whyAll
                    + ". Hand in bars that are parallel in this view. Nothing was placed.";
    }
}

// ---- placed, and read back --------------------------------------------------

if (refused.Length == 0)
{
    MultiReferenceAnnotation made = null;
    string revitSaid = null;
    try { made = MultiReferenceAnnotation.Create(doc, view.Id, optionsFor(together)); }
    catch (Exception failure) { revitSaid = failure.Message; }

    if (made == null)
    {
        refused = "Revit placed no annotation" + (revitSaid == null ? "." : " - it said: " + revitSaid)
                + " Nothing was placed.";
    }
    else
    {
        try { doc.Regenerate(); } catch (Exception) { }
        string wrong = null;
        var witnessed = new HashSet<ElementId>();
        var dimension = doc.GetElement(made.DimensionId) as Dimension;
        IndependentTag tag = null;
        bool tagWanted = chosenType.TagTypeId != null && chosenType.TagTypeId != ElementId.InvalidElementId;
        if (made.GetTypeId() != chosenType.Id) wrong = "it carries another type";
        else if (dimension == null) wrong = "it has no dimension";
        else if (dimension.OwnerViewId != view.Id) wrong = "its dimension belongs to another view";
        else
        {
            foreach (Reference reference in dimension.References)
                if (reference != null && together.Contains(reference.ElementId)) witnessed.Add(reference.ElementId);
            if (witnessed.Count == 0) wrong = "its dimension witnesses none of the bars";
        }
        if (wrong == null && made.TagId != null && made.TagId != ElementId.InvalidElementId)
        {
            tag = doc.GetElement(made.TagId) as IndependentTag;
            if (tag == null || tag.MultiReferenceAnnotationId != made.Id) wrong = "its tag is not its own";
        }
        else if (wrong == null && tagWanted)
        {
            wrong = "its type names a tag family and no tag was made";
        }

        if (wrong != null)
        {
            // Ours, made a moment ago, and wrong - removed.
            try { doc.Delete(made.Id); } catch (Exception) { }
            refused = "The annotation read back wrong - " + wrong + " - and was removed. Nothing was placed.";
        }
        else
        {
            placed = 1;
            annotationId = made.Id;
            dimensionId = dimension.Id;
            tagId = tag != null ? tag.Id : ElementId.InvalidElementId;
            dimensioned.AddRange(together.Where(id => witnessed.Contains(id)));
            foreach (var missed in together.Where(id => !witnessed.Contains(id)))
                lines.Add(missed + ": handed to Revit and not witnessed by its dimension.");
            lines.Add("Annotation " + made.Id + " placed: dimension " + dimension.Id + " across "
                + dimensioned.Count + " bar(s), " + (tag != null ? "tag " + tag.Id : "no tag - the type names no tag family") + ".");

            bool hiddenHere = false;
            try { hiddenHere = view.GetCategoryHidden(new ElementId(BuiltInCategory.OST_MultiReferenceAnnotations)); }
            catch (Exception) { }
            if (hiddenHere)
                findings.Add("Multi-Rebar Annotations are turned off in \"" + view.Name + "\" - in its Visibility/"
                    + "Graphics or its view template - so the annotation placed is in the model and not shown there.");
        }
    }
}

// THE WHOLE ANSWER AS ONE LINE - every count in words.
findings.Insert(0, placed + " multi-rebar annotation(s) placed" + (view != null ? " in \"" + view.Name + "\"" : "")
    + (chosenType != null ? " as \"" + typeLabel(chosenType) + "\", " + styleWord : "")
    + ": " + dimensioned.Count + " bar(s) dimensioned. Left out: " + notRebar.Count + " not rebar, "
    + notVisible.Count + " not drawn in the view, " + alreadyAnnotated.Count + " already annotated here, "
    + notAligned.Count + " refused by Revit's check.");
if (refused.Length > 0) findings.Add(refused);
findings.AddRange(lines);
