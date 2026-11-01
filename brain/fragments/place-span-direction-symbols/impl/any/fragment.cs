// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `symbolType` are in
// scope, and leaves `placed`, `newSymbols`, `alreadyHere`, `notFloors`,
// `notStructural`, `otherLevel`, `notPlaced`, `typesLoaded`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One plan of symbols is one undo
// entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > SYMBOL > SPAN DIRECTION. SpanDirectionSymbol.Create(
// Document, ElementId viewId, LinkElementId floor, XYZ head, ElementId typeId)
// and GetDefaultPlacementPoint(Document, LinkElementId) - REVIT 2025 AND LATER:
// absent from the 2020 to 2024 assemblies, present 2025 to 2027
// (tools/api-surface --members, 2026-11-01). The card claims those three
// releases only, so they are named directly.
//
// ONLY IN A FLOOR PLAN OR A STRUCTURAL PLAN. Revit's own failure text: "Span
// Direction Symbol can only be placed in a Floor Plan View". Any other view -
// a ceiling plan, a section, a 3D view, a template - is refused BY NAME before
// anything is placed.
//
// A NON-STRUCTURAL FLOOR HAS NO SPAN DIRECTION (Revit's documentation of
// Floor.SpanDirectionAngle, every release). Its Structural box is read first;
// a floor whose span still cannot be read is named the same way.
//
// THE SYMBOL TYPE IS THE MODELLER'S AND IS NEVER GUESSED (D-33). One-way and
// two-way are different types drawing different spans. Left out, or a name no
// span direction type has: NOTHING IS PLACED and the loaded types come back in
// `typesLoaded`. A name that matches two is refused rather than one picked.
//
// ONE SYMBOL PER FLOOR PER PLAN. The floor's own GetSpanDirectionSymbolIds is
// read, and a symbol owned by this plan - or by a view sharing its annotation,
// its primary or a dependent - means the floor is already shown and is left
// alone and named. A re-run stacks nothing.
//
// AT REVIT'S OWN DEFAULT POINT - the middle of the top face's bounding box,
// GetDefaultPlacementPoint. Laying symbols out is another job.
//
// EVERY SYMBOL IS READ BACK - its type, its view, and that its floor now lists
// it - and one that is wrong is removed again inside the same undo, and its
// floor named. Each floor's span direction is read and reported in degrees.

const double DegreesPerRadian = 180.0 / Math.PI;

var placed = 0;
var newSymbols = new List<ElementId>();
var alreadyHere = new List<ElementId>();
var notFloors = new List<ElementId>();
var notStructural = new List<ElementId>();
var otherLevel = new List<ElementId>();
var notPlaced = new List<ElementId>();
var typesLoaded = new List<string>();
var lines = new List<string>();
var findings = new List<string>();
string refused = "";

var culture = System.Globalization.CultureInfo.InvariantCulture;
var symbolCategory = new ElementId(BuiltInCategory.OST_SpanDirectionSymbol);

// How a type reads in the Properties palette - "M_Span Direction: One Way Slab".
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

var symbolTypes = new FilteredElementCollector(doc)
    .OfCategoryId(symbolCategory)
    .WhereElementIsElementType()
    .Cast<ElementType>()
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
typesLoaded.Add("Span direction symbol types: " + (symbolTypes.Count == 0
    ? "none loaded - load a span direction family (Insert > Load Family, Annotations, Structural) first"
    : string.Join(", ", symbolTypes.Select(t => "'" + typeLabel(t) + "'").ToArray())));

var typeText = (symbolType ?? "").Trim();
ElementType chosenType = null;

// ---- refused before anything is placed -------------------------------------

var plan = view as ViewPlan;
if (view == null)
{
    refused = "No view was named. Span direction symbols go in a floor plan or a structural plan; name it "
            + "as the Project Browser shows it. Nothing was placed.";
}
else if (view.IsTemplate)
{
    refused = "\"" + view.Name + "\" is a view template, and a template holds no symbols. Name the plan "
            + "itself. Nothing was placed.";
}
else if (plan == null || (view.ViewType != ViewType.FloorPlan && view.ViewType != ViewType.EngineeringPlan))
{
    refused = "\"" + view.Name + "\" is a " + view.ViewType + " view. Revit places a span direction symbol "
            + "only in a floor plan or a structural plan - name one. Nothing was placed.";
}
else if (typeText.Length == 0 || typeText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    refused = "No span direction symbol type was named, and one is never chosen for you - one-way and "
            + "two-way draw different spans. The loaded ones are in typesLoaded. Nothing was placed.";
}
else
{
    var hits = symbolTypes.Where(t => isNamed(t, typeText)).ToList();
    if (hits.Count == 1)
    {
        chosenType = hits[0];
    }
    else if (hits.Count > 1)
    {
        refused = hits.Count + " span direction symbol types are called '" + typeText + "' - "
                + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                + " - so the name does not say which. Name it with its family. Nothing was placed.";
    }
    else
    {
        // A type of ANOTHER kind by that name - a tag, a footing span symbol -
        // is named with its own category, so the modeller sees why it cannot do.
        var elsewhere = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .Cast<ElementType>()
            .Where(t => t.Category != null && isNamed(t, typeText))
            .Select(t => "'" + typeLabel(t) + "' (" + t.Category.Name + ")")
            .Distinct()
            .Take(3)
            .ToList();
        refused = elsewhere.Count > 0
            ? "'" + typeText + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not a span direction "
              + "symbol type. The loaded ones are in typesLoaded. Nothing was placed."
            : "No span direction symbol type is called '" + typeText + "' - the name has to match the "
              + "Project Browser, capitals included. The loaded ones are in typesLoaded. Nothing was placed.";
    }
}

if (refused.Length == 0)
{
    var planLevel = view.GenLevel;

    // A family type must be active before Revit places it.
    var symbol = chosenType as FamilySymbol;
    if (symbol != null && !symbol.IsActive) symbol.Activate();

    // A DEPENDENT VIEW SHARES ITS ANNOTATION with its primary and the primary's
    // other dependents, so a floor shown in any of them is shown here too.
    // TAG_ROOMS' rule for tags, NOT yet seen for span symbols on a model.
    var viewsSharing = new HashSet<ElementId>();
    viewsSharing.Add(view.Id);
    try
    {
        foreach (var dependentId in view.GetDependentViewIds()) viewsSharing.Add(dependentId);
        var primaryId = view.GetPrimaryViewId();
        if (primaryId != null && primaryId != ElementId.InvalidElementId)
        {
            viewsSharing.Add(primaryId);
            var primary = doc.GetElement(primaryId) as View;
            if (primary != null)
                foreach (var siblingId in primary.GetDependentViewIds()) viewsSharing.Add(siblingId);
        }
    }
    catch { }

    bool hiddenHere = false;
    try { hiddenHere = view.GetCategoryHidden(symbolCategory); } catch { }

    string firstRefusal = null;
    var seen = new HashSet<ElementId>();

    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null || !seen.Add(element.Id)) continue;

        var floor = element as Floor;
        if (floor == null)
        {
            notFloors.Add(element.Id);
            continue;
        }

        bool inThisModel = false;
        try { inThisModel = floor.Document != null && floor.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notPlaced.Add(floor.Id);
            lines.Add(floor.Id + ": of another model - left alone.");
            continue;
        }

        string typeName = "floor";
        try { if (floor.FloorType != null) typeName = floor.FloorType.Name; } catch { }

        // STRUCTURAL FIRST - no span direction otherwise.
        var structural = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
        if (structural != null && structural.AsInteger() == 0)
        {
            notStructural.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): not structural - Revit keeps no span direction for it, "
                + "so there is nothing to show. Left alone.");
            continue;
        }
        double span;
        try { span = floor.SpanDirectionAngle; }
        catch (Exception failure)
        {
            notStructural.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): Revit has no span direction for it - " + failure.Message + " Left alone.");
            continue;
        }
        var spanText = Math.Round(span * DegreesPerRadian, 2).ToString("0.##", culture) + " deg";

        // ALREADY SHOWN in this plan, by a symbol of any span direction type.
        ICollection<ElementId> existing = null;
        try { existing = floor.GetSpanDirectionSymbolIds(); } catch { existing = null; }
        bool shownHere = false;
        if (existing != null)
            foreach (var symbolId in existing)
            {
                var owned = doc.GetElement(symbolId);
                if (owned != null && viewsSharing.Contains(owned.OwnerViewId)) { shownHere = true; break; }
            }
        if (shownHere)
        {
            alreadyHere.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): already shows a span direction symbol in this plan; span " + spanText + ".");
            continue;
        }

        // ANOTHER LEVEL. This plan is the drawing of its own level's floors.
        if (planLevel == null || floor.LevelId != planLevel.Id)
        {
            otherLevel.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): on another level than \"" + view.Name + "\" - left alone.");
            continue;
        }

        var floorLink = new LinkElementId(floor.Id);
        SpanDirectionSymbol made = null;
        try
        {
            var head = SpanDirectionSymbol.GetDefaultPlacementPoint(doc, floorLink);
            made = SpanDirectionSymbol.Create(doc, view.Id, floorLink, head, chosenType.Id);
        }
        catch (Exception failure)
        {
            if (firstRefusal == null) firstRefusal = failure.Message;
        }
        if (made == null)
        {
            notPlaced.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): Revit placed no symbol.");
            continue;
        }

        // READ BACK - the type, the view, and that the floor lists it.
        try { doc.Regenerate(); } catch { }
        string wrong = null;
        try
        {
            if (made.GetTypeId() != chosenType.Id) wrong = "it carries another type";
            else if (made.OwnerViewId != view.Id) wrong = "it belongs to another view";
            else if (!floor.GetSpanDirectionSymbolIds().Contains(made.Id)) wrong = "the floor does not list it";
        }
        catch (Exception failure) { wrong = "it could not be read back - " + failure.Message; }
        if (wrong != null)
        {
            // Ours, made a moment ago, and wrong - removed.
            try { doc.Delete(made.Id); } catch { }
            notPlaced.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): the symbol read back wrong - " + wrong + " - and was removed.");
            continue;
        }

        placed++;
        newSymbols.Add(made.Id);
        lines.Add(floor.Id + " (" + typeName + "): symbol " + made.Id + " placed; span " + spanText + " from the model's X axis.");
    }

    // THE WHOLE ANSWER AS ONE LINE - every count in words.
    findings.Add(placed + " span direction symbol(s) placed in \"" + view.Name + "\" as \"" + typeLabel(chosenType)
        + "\". Not placed: " + alreadyHere.Count + " already shown here, " + notFloors.Count + " not floors, "
        + notStructural.Count + " not structural, " + otherLevel.Count + " on another level, " + notPlaced.Count
        + " refused by Revit, read back wrong or of another model.");

    if (hiddenHere && placed > 0)
        findings.Add("Span Direction Symbols are turned off in \"" + view.Name + "\" - in its Visibility/Graphics "
            + "or its view template - so the " + placed + " symbol(s) placed are in the model and not shown in that plan.");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    lines.Add(refused);
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
