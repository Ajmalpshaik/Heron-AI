// NOT STANDALONE. Assumes `doc`, `views`, `rules`, `dimensionType`, `offsetMm`,
// `minRunMm`, `skipVertical`, `skipDimensioned` and `chain` are in scope, and
// leaves `dimensioned`, `newDimensions`, `skipped`, `skippedWhy`, `perView`,
// `rulesRead`, `dimensionTypeUsed`, `refused` and `findings` behind.
//
// DIMENSION_VIEWS_BY_RULES, version 1 (2026-11-08). One pass over one plan or
// many: a RULE PER RUN CATEGORY says what each run is dimensioned to - grids,
// walls, columns, runs of the same service, runs of another service - on which
// side, and how far across the run to reach. Every straight run of those
// categories the plan shows gets ONE string square to it, from the target(s)
// to the run's centreline, placed where run and target lie side by side on
// the part of the run the plan shows. Every run left without one is named,
// with the reason.
//
// RE-AUTHORED FROM HERON'S OWN CARDS, WHICH THIS DOES NOT CHANGE.
// CREATE_DIMENSION (FRG-VIEW-021, PROVEN) taught the fact the whole job turns
// on: a run's centreline is only a REFERENCE when the geometry is read with
// ComputeReferences AND IncludeNonVisibleObjects - read the ordinary way, a
// duct gives no line and no error, and nothing is ever drawn; that a
// dimension is READ BACK (its References, never Dimension.Curve, which throws
// on a dimension this new) and one holding fewer references than asked is
// taken out again; and that Revit's refusal is caught, never let out to abort
// the caller's transaction. CREATE_LINEAR_DIMENSION (FRG-VIEW-101, PROVEN)
// taught: a grid is referenced as the whole element, and only one the view
// SHOWS - Revit throws on a datum the view is not showing, and the throw
// takes the whole string; a family instance's faces are read through
// GetSymbolGeometry with the instance's transform, NOT GetInstanceGeometry,
// whose copies carry references Revit will not dimension to; a wall's faces
// come from HostObjectUtils.GetSideFaces. TAG_VIEWS_BY_RULES (FRG-VIEW-176,
// DRAFT) gave the shape: rules on ONE line separated by semicolons, each read
// in full before anything is drawn and one bad rule refusing the call; the
// views by name or the active one; "already done" read model-wide by the view
// that OWNS the annotation, its primary and the primary's dependents
// included, never from what the view shows; the crop read as its rectangle
// and only when it is on.
//
// WHAT THE OWNER'S OWN ADD-IN TAUGHT (his AJ-Tools, studied for lessons only:
// D-25 - none of its code, names, wording or layout is here):
//   - The categories a rule DIMENSIONS and the runs it may dimension TO are
//     separate questions. A run of another service is looked for in every run
//     category, whether or not that category has a rule; a run of the rule's
//     own service is a target only when the rule says same-service.
//   - Never hand NewDimension a run as a whole element. Revit rejects that as
//     not geometric, and the rejection loses every other reference in the
//     same string too.
//   - Two references at one spot across the run would make a segment of no
//     length, which Revit will not draw - so a target lying on the run's own
//     line is never used.
//   - A target must run alongside the run and share some of its length. A grid
//     crossing the run square-on has no single distance from it and would look
//     like the closest thing; it is never used.
//   - Existing dimensions are found by their category, not their class: from
//     Revit 2025 the API hands back a linear one as LinearDimension, which a
//     search for the Dimension class alone does not return.
//   - What an existing dimension measures is read from the elements it refers
//     to, never from where its line happens to sit on the drawing.
//   - A reference pointing into a linked model names the link instance, not
//     the element inside it; such a reference is never taken for one of this
//     model's elements.
//   - The crop rectangle is believed only while the crop is switched on.
//   - Where the dimension line is drawn only fixes its position and direction;
//     the extension past the last witness line is set by the dimension type.
//   - Linear strings take linear dimension types and nothing else.
//   - Reasons for leaving a run are tallied per reason, and the tallies sum to
//     the number left.
//
// ALREADY DIMENSIONED - THE OWNER'S RULE (2026-11-08). A run counts as
// already dimensioned in a view only when a dimension owned by that view's
// family holds the run TOGETHER WITH a reference of a kind its rule names - a
// grid, a wall, a column, a run of its own service or of another. A run that
// is held only as some other run's target, of a kind its own rule does not
// name, is dimensioned in its own right.
//
// WHAT IS SAFER HERE, ON PURPOSE. No dimension type is ever the first one
// loaded: one is named, or the project's default linear type is used, or the
// call is refused before anything is drawn. Every string is regenerated as
// soon as it is made and read again: one Revit has removed, one holding fewer
// references than asked, or one of another type that will not change, is
// taken out at once and its run named - so a bad string is removed before the
// call ends rather than left to fail the whole call when it is committed (the
// executor rolls the whole call back on any error Revit posts then). A run
// with chain separate is all or nothing. No dimension already in the
// model is moved, changed or deleted - not even one this card drew earlier.
//
// NOT DONE, AND SAID: targets inside a LINKED model (a link reference has to
// be rebuilt from Revit's undocumented stable representation before
// NewDimension takes it - not safe and simple enough for version 1); a
// section or elevation; a string through every run between the target and
// this run (only the nearest target on each side is held); the run's own
// width as a segment; arcs and flex runs; a crop that is not a rectangle,
// read as its rectangle.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16). The
// executor makes the whole call one TransactionGroup, so the whole call -
// every view in the list - is ONE undo. doc.Regenerate() is called inside the
// open transaction after each string; it needs no transaction of its own.

const double MmToFeet = 1.0 / 304.8;
const double Tolerance = 1.0 / 304.8;   // 1 mm: closer than this is one position
const double ParallelCos = 0.9998;     // about one degree

int dimensioned = 0;
var newDimensions = new List<ElementId>();
var skipped = new List<ElementId>();
var perView = new List<string>();
var rulesRead = new List<string>();
var findings = new List<string>();
string dimensionTypeUsed = "";
string refused = "";

// WHY EACH ONE IN `skipped` IS THERE, one line per reason with a count, so the
// counts add up to `skipped`. Per view too, for its perView line.
var whyCount = new Dictionary<string, int>();
var whyOrder = new List<string>();
Dictionary<string, int> viewWhy = null;
List<string> viewWhyOrder = null;
Action<ElementId, string> because = (id, reason) =>
{
    skipped.Add(id);
    int before;
    if (!whyCount.TryGetValue(reason, out before)) { before = 0; whyOrder.Add(reason); }
    whyCount[reason] = before + 1;
    if (viewWhy != null)
    {
        int here;
        if (!viewWhy.TryGetValue(reason, out here)) { here = 0; viewWhyOrder.Add(reason); }
        viewWhy[reason] = here + 1;
    }
};
string firstRefusal = null;

Func<string, string> folded = s =>
    string.Join(":", (s ?? "").Split(':').Select(p => p.Trim()).ToArray()).ToLowerInvariant();

// ---- THE RUN CATEGORIES ------------------------------------------------------
// Straight MEP runs only - what has a centreline to measure from. A flex run is
// a spline and an arc has no one direction across it.
var runCategoryIds = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_DuctCurves),
    new ElementId(BuiltInCategory.OST_PipeCurves),
    new ElementId(BuiltInCategory.OST_CableTray),
    new ElementId(BuiltInCategory.OST_Conduit),
};
var flexIds = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_FlexDuctCurves),
    new ElementId(BuiltInCategory.OST_FlexPipeCurves),
};
var columnIds = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_Columns),
    new ElementId(BuiltInCategory.OST_StructuralColumns),
};

// A category as the modeller types it - as Visibility/Graphics shows it,
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

// A length typed as "1500" or "1500mm"; NaN when it is not one.
Func<string, double> readMm = text =>
{
    var t = (text ?? "").Trim();
    if (t.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) t = t.Substring(0, t.Length - 2).Trim();
    double value;
    if (!double.TryParse(t, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out value)) return double.NaN;
    return value;
};

// ---- 1. THE RULES, read in full before anything is drawn --------------------
//
// Separated by SEMICOLONS, and by nothing else - each
//     run category | dimension to | side | reach in mm
// e.g. "Ducts | grids, walls | both | 1500; Pipes | same-service | nearest".
// A LINE BREAK IS NOT A SEPARATOR, ON PURPOSE: revit_change hands a value over
// line by line and drops a line with no "=" in it, so a rule on a second line
// never arrives (TAG_VIEWS_BY_RULES, row 5b-1477). Text still holding a line
// break is refused rather than split, and `rulesRead` opens with the count.
//
// Dimension to: grids, walls, columns, same-service (runs of the rule's own
// category), other-service (runs of every OTHER run category), separated by
// commas. Required - there is no natural default.
// Side: nearest (left out; "one" says the same) - the single closest target,
// on whichever side it is; or both - the closest on each side.
// Reach: MODEL millimetres ACROSS the run, from its centreline - how far a
// target may be. Left out, 3000 - a number this card chose, and rulesRead
// says it was used.
const double DefaultReachMm = 3000;
var ruleCategory = new List<Category>();
var ruleTargets = new List<HashSet<string>>();
var ruleBoth = new List<bool>();
var ruleReachFeet = new List<double>();
var ruleProblems = new List<string>();

var ruleText = rules ?? "";
int ruleNumber = 0;
bool brokenLines = ruleText.IndexOf('\n') >= 0 || ruleText.IndexOf('\r') >= 0;
foreach (var raw in (brokenLines ? new string[0] : ruleText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)))
{
    var line = raw.Trim();
    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
    ruleNumber++;
    var said = "rule " + ruleNumber + " (\"" + line + "\")";
    var parts = line.Split('|');
    if (parts.Length > 4)
    {
        ruleProblems.Add(said + " has " + parts.Length + " parts - a rule is at most four: run category | dimension "
            + "to | side | reach in mm");
        continue;
    }
    var categoryText = parts[0].Trim();
    var targetText = parts.Length > 1 ? parts[1].Trim() : "";
    var sideText = parts.Length > 2 ? parts[2].Trim().ToLowerInvariant() : "";
    var reachText = parts.Length > 3 ? parts[3].Trim() : "";

    var category = categoryNamed(categoryText);
    if (category == null)
    {
        ruleProblems.Add(said + ": no category is called \"" + categoryText + "\" in this model - type it as "
            + "Visibility/Graphics shows it: Ducts, Pipes, Cable Trays or Conduits");
        continue;
    }
    if (flexIds.Contains(category.Id))
    {
        ruleProblems.Add(said + ": " + category.Name + " are not straight - a flex run has no one direction to "
            + "measure across. Ducts, Pipes, Cable Trays or Conduits");
        continue;
    }
    if (!runCategoryIds.Contains(category.Id))
    {
        ruleProblems.Add(said + ": " + category.Name + " is not a run category - this dimensions straight Ducts, "
            + "Pipes, Cable Trays and Conduits. Fixtures and equipment are dimensioned between themselves by "
            + "putting a dimension through them");
        continue;
    }
    if (ruleCategory.Any(c => c.Id == category.Id))
    {
        ruleProblems.Add(said + ": " + category.Name + " has a rule already - one rule per category, so a run is "
            + "never dimensioned twice by one call");
        continue;
    }

    var targets = new HashSet<string>();
    string badTarget = null;
    foreach (var word in targetText.Split(','))
    {
        var w = word.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        if (w.Length == 0) continue;
        if (w == "grid" || w == "grids") targets.Add("grid");
        else if (w == "wall" || w == "walls") targets.Add("wall");
        else if (w == "column" || w == "columns") targets.Add("column");
        else if (w.StartsWith("same", StringComparison.Ordinal)) targets.Add("same");
        else if (w.StartsWith("other", StringComparison.Ordinal)) targets.Add("other");
        else { badTarget = word.Trim(); break; }
    }
    if (badTarget != null)
    {
        ruleProblems.Add(said + ": \"" + badTarget + "\" is not something this dimensions to - say grids, walls, "
            + "columns, same-service or other-service, separated by commas. Linked models are not read in this "
            + "version");
        continue;
    }
    if (targets.Count == 0)
    {
        ruleProblems.Add(said + ": say what " + category.Name + " are dimensioned to - grids, walls, columns, "
            + "same-service or other-service - e.g. \"" + category.Name + " | grids, walls\"");
        continue;
    }

    bool both;
    if (sideText.Length == 0 || sideText == "nearest" || sideText == "one") both = false;
    else if (sideText == "both") both = true;
    else
    {
        ruleProblems.Add(said + ": \"" + sideText + "\" is not a side - say nearest (the closest target, on "
            + "either side) or both (the closest on each side)");
        continue;
    }

    double reachMm = DefaultReachMm;
    if (reachText.Length > 0)
    {
        reachMm = readMm(reachText);
        if (double.IsNaN(reachMm) || reachMm <= 0)
        {
            ruleProblems.Add(said + ": \"" + reachText + "\" is not a reach - how far across the run a target may "
                + "be, in model millimetres from its centreline, above 0, e.g. 1500");
            continue;
        }
    }

    var targetWords = new List<string>();
    if (targets.Contains("grid")) targetWords.Add("grids");
    if (targets.Contains("wall")) targetWords.Add("walls");
    if (targets.Contains("column")) targetWords.Add("columns");
    if (targets.Contains("same")) targetWords.Add("other " + category.Name.ToLowerInvariant() + " (same service)");
    if (targets.Contains("other")) targetWords.Add("runs of another service");

    ruleCategory.Add(category);
    ruleTargets.Add(targets);
    ruleBoth.Add(both);
    ruleReachFeet.Add(reachMm * MmToFeet);
    rulesRead.Add(ruleCategory.Count + ". " + category.Name + " dimensioned to " + string.Join(", ", targetWords.ToArray())
        + "; " + (both ? "both sides - the closest target on each" : "nearest - the closest target on either side")
        + "; reach " + reachMm + " mm across the run" + (reachText.Length == 0 ? " (not given - this card's 3000)" : ""));
}

// ---- THE CALL-WIDE VALUES -----------------------------------------------------
var callProblems = new List<string>();

var chainWord = (chain ?? "").Trim().ToLowerInvariant();
bool separate;
if (chainWord.Length == 0 || chainWord == "one") separate = false;
else if (chainWord == "separate") separate = true;
else
{
    separate = false;
    callProblems.Add("chain \"" + chain + "\" is not a chain style - say one (the target(s) and the run in a "
        + "single string) or separate (each target its own dimension to the run)");
}
if (offsetMm < 0 || double.IsNaN(offsetMm))
    callProblems.Add("offsetMm " + offsetMm + " is not an offset - paper millimetres along the run from the middle "
        + "of the part the view shows, 0 or more");
if (minRunMm < 0 || double.IsNaN(minRunMm))
    callProblems.Add("minRunMm " + minRunMm + " is not a length - the shortest run, in model millimetres, 0 or more");

// THE DIMENSION TYPE: named, or the project's default LINEAR type - never the
// first one loaded. Only a linear (or linear fixed) type draws a linear string.
var linearTypes = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>()
    .Where(t =>
    {
        try { return t.StyleType == DimensionStyleType.Linear || t.StyleType == DimensionStyleType.LinearFixed; }
        catch { return false; }
    }).ToList();
var loadedWords = linearTypes.Count == 0 ? "none is loaded"
    : "loaded: " + string.Join(", ", linearTypes.Take(5).Select(t => "'" + t.Name + "'").ToArray())
      + (linearTypes.Count > 5 ? " and " + (linearTypes.Count - 5) + " more" : "");
DimensionType chosenType = null;
var typeText = (dimensionType ?? "").Trim();
if (typeText.Length == 0)
{
    ElementId defaultId = null;
    try { defaultId = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType); } catch { defaultId = null; }
    chosenType = defaultId == null || defaultId == ElementId.InvalidElementId ? null : doc.GetElement(defaultId) as DimensionType;
    if (chosenType == null)
        callProblems.Add("no dimension type is named and this project sets no default linear dimension type, so "
            + "none is chosen for you - name one (" + loadedWords + ")");
    else dimensionTypeUsed = "'" + chosenType.Name + "' (the project's default linear dimension type)";
}
else
{
    var hits = linearTypes.Where(t => folded(t.Name) == folded(typeText)
                                      || folded("Linear Dimension Style: " + t.Name) == folded(typeText)).ToList();
    if (hits.Count == 1)
    {
        chosenType = hits[0];
        dimensionTypeUsed = "'" + chosenType.Name + "' (named)";
    }
    else if (hits.Count > 1)
        callProblems.Add(hits.Count + " linear dimension types are called \"" + typeText + "\" - rename one in the "
            + "project, as this cannot tell them apart");
    else
    {
        var other = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>()
            .Where(t => folded(t.Name) == folded(typeText)).Select(t =>
            {
                try { return "'" + t.Name + "' (" + t.StyleType + ")"; } catch { return "'" + t.Name + "'"; }
            }).Take(2).ToList();
        callProblems.Add(other.Count > 0
            ? "\"" + typeText + "\" is " + string.Join(", ", other.ToArray()) + ", not a linear dimension type, and "
              + "cannot draw a linear string (" + loadedWords + ")"
            : "no linear dimension type is called \"" + typeText + "\" - the name has to match the Project Browser ("
              + loadedWords + ")");
    }
}

if (brokenLines)
    refused = "The rules arrive on more than one line, and a line break is not a separator here - a rule after "
            + "one can be lost on the way to Revit. Put every rule on ONE line, separated by semicolons: "
            + "\"Ducts | grids, walls | both | 1500; Pipes | same-service\".";
else if (ruleNumber == 0)
    refused = "No rules were given. Separate them with semicolons, each \"run category | dimension to | side | "
            + "reach in mm\" - e.g. \"Ducts | grids, walls | both | 1500\". Dimension to is grids, walls, columns, "
            + "same-service or other-service; side is nearest (left out) or both.";
else if (ruleProblems.Count > 0)
    refused = ruleProblems.Count + " of the " + ruleNumber + " rule(s) could not be read: "
            + string.Join("; ", ruleProblems.ToArray()) + ".";
// A value of the call that cannot be read is said too, beside any rule that
// could not be - so one answer names everything to fix.
if (callProblems.Count > 0)
    refused = (refused.Length > 0 ? refused + " And " : "") + string.Join("; ", callProblems.ToArray()) + ".";

rulesRead.Insert(0, ruleCategory.Count + " rule(s) read, of " + ruleNumber + " separated by semicolons"
    + (ruleCategory.Count > 0 ? ": " + string.Join(", ", ruleCategory.Select(c => c.Name).ToArray()) : "") + ".");

// ---- 2. THE VIEWS ------------------------------------------------------------
//
// None named is the ACTIVE view. Version 1 dimensions PLANS: a run seen in a
// section is measured up the drawing, which is not built here. A template,
// or any other kind of view, is left out by name. A dependent view is
// dimensioned like any other: "already dimensioned" below reads the
// dimensions OWNED by the view, its primary and the primary's dependents, so
// a view later in the list finds the strings an earlier one drew.
var given = new List<View>();
bool fromActive = false;
if (views != null) foreach (var v in views) if (v != null && !given.Any(g => g.Id == v.Id)) given.Add(v);
if (given.Count == 0)
{
    View active = null;
    try { active = doc.ActiveView; } catch { }
    if (active != null) { given.Add(active); fromActive = true; }
}

var planKinds = new[] { ViewType.FloorPlan, ViewType.CeilingPlan, ViewType.EngineeringPlan, ViewType.AreaPlan };
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
var viewsLeft = new List<string>();
foreach (var v in given)
{
    string leftWhy = null;
    if (v.IsTemplate) leftWhy = "is a view template, which holds no dimensions";
    else if (!planKinds.Contains(v.ViewType))
        leftWhy = "is a " + v.ViewType + " view - this version dimensions runs in plans (floor, ceiling, structural "
                + "or area plans) only";
    if (leftWhy != null)
    {
        viewsLeft.Add("\"" + v.Name + "\" " + leftWhy);
        perView.Add("\"" + v.Name + "\": not dimensioned - it " + leftWhy + ".");
        continue;
    }
    working.Add(v);
}

if (refused.Length == 0 && given.Count == 0)
    refused = "No view was named and there is no active view. Name the plans as the Project Browser shows them, "
            + "separated by commas.";
else if (refused.Length == 0 && working.Count == 0)
    refused = "None of the views can take these dimensions: " + string.Join("; ", viewsLeft.ToArray()) + ".";
if (refused.Length > 0) refused += " Nothing was drawn.";

// ---- helpers shared by every view -------------------------------------------

// WHAT KIND OF TARGET AN ELEMENT IS, for the "already dimensioned" rule:
// grid, wall, column, or run (with its category) - or nothing this reads.
var kindCache = new Dictionary<ElementId, string>();
var runCategoryOf = new Dictionary<ElementId, ElementId>();
Func<ElementId, string> kindOf = id =>
{
    string kind;
    if (kindCache.TryGetValue(id, out kind)) return kind;
    kind = "";
    Element found = null;
    try { found = doc.GetElement(id); } catch { found = null; }
    if (found is Grid) kind = "grid";
    else if (found is Wall) kind = "wall";
    else if (found != null && found.Category != null)
    {
        if (columnIds.Contains(found.Category.Id)) kind = "column";
        else if (runCategoryIds.Contains(found.Category.Id)) { kind = "run"; runCategoryOf[id] = found.Category.Id; }
    }
    kindCache[id] = kind;
    return kind;
};
// Does `partner` count, for a run of `category` under these targets?
Func<ElementId, HashSet<string>, ElementId, bool> namedByRule = (partner, targets, category) =>
{
    var kind = kindOf(partner);
    if (kind == "grid") return targets.Contains("grid");
    if (kind == "wall") return targets.Contains("wall");
    if (kind == "column") return targets.Contains("column");
    if (kind == "run")
    {
        var partnerCategory = runCategoryOf[partner];
        return partnerCategory == category ? targets.Contains("same") : targets.Contains("other");
    }
    return false;
};

// Points along every edge of a face, each edge on its own so one edge Revit
// cannot tessellate loses only itself.
Func<Face, Transform, List<XYZ>> edgePoints = (face, placed) =>
{
    var points = new List<XYZ>();
    EdgeArrayArray loops = null;
    try { loops = face.EdgeLoops; } catch { loops = null; }
    if (loops == null) return points;
    foreach (EdgeArray loop in loops)
        foreach (Edge edge in loop)
        {
            try { foreach (var p in edge.Tessellate()) points.Add(placed.OfPoint(p)); } catch { }
        }
    return points;
};

int offCropPairs = 0;
int oneSideOnly = 0;
int sidesApart = 0;
int offsetsShortened = 0;
int typeCorrections = 0;
int removedOnRegenerate = 0;
var linkedDimensions = new HashSet<ElementId>();
var hiddenIn = new List<string>();

// ---- 3. EVERY VIEW -----------------------------------------------------------
foreach (var view in (refused.Length == 0 ? working : new List<View>()))
{
    viewWhy = new Dictionary<string, int>();
    viewWhyOrder = new List<string>();
    int runsHere = 0, stringsHere = 0;

    int scale = 1;
    try { scale = Math.Max(1, view.Scale); } catch { }
    double offsetFeet = Math.Max(0, offsetMm) * MmToFeet * scale;
    double minRunFeet = Math.Max(0, minRunMm) * MmToFeet;
    var viewDir = view.ViewDirection.Normalize();

    // THE CROP, read only when it is on, as its rectangle.
    BoundingBoxXYZ crop = null;
    Transform toCrop = null;
    try
    {
        if (view.CropBoxActive) { crop = view.CropBox; toCrop = crop == null ? null : crop.Transform.Inverse; }
    }
    catch { crop = null; toCrop = null; }
    Func<XYZ, bool> inCrop = q =>
    {
        if (crop == null || toCrop == null) return true;
        var local = toCrop.OfPoint(q);
        return local.X >= crop.Min.X - 1e-6 && local.X <= crop.Max.X + 1e-6
            && local.Y >= crop.Min.Y - 1e-6 && local.Y <= crop.Max.Y + 1e-6;
    };
    // THE PART OF A LINE INSIDE THE CROP, exactly: the line clipped against the
    // crop rectangle in the crop's own coordinates, as the two parameters (0
    // to 1 along the line) where it enters and leaves. Null when no part is
    // inside. No crop: the whole line.
    Func<Line, double[]> clipped = line =>
    {
        if (crop == null || toCrop == null) return new[] { 0.0, 1.0 };
        var a = toCrop.OfPoint(line.GetEndPoint(0));
        var b = toCrop.OfPoint(line.GetEndPoint(1));
        double t0 = 0, t1 = 1;
        double dx = b.X - a.X, dy = b.Y - a.Y;
        var edges = new[]
        {
            new[] { -dx, a.X - crop.Min.X }, new[] { dx, crop.Max.X - a.X },
            new[] { -dy, a.Y - crop.Min.Y }, new[] { dy, crop.Max.Y - a.Y },
        };
        foreach (var e in edges)
        {
            double p = e[0], q = e[1] + 1e-6;
            if (Math.Abs(p) < 1e-12) { if (q < 0) return null; continue; }
            double t = q / p;
            if (p < 0) { if (t > t1) return null; if (t > t0) t0 = t; }
            else { if (t < t0) return null; if (t < t1) t1 = t; }
        }
        return t1 > t0 ? new[] { t0, t1 } : null;
    };

    // THE DIMENSIONS ALREADY HERE, read from the WHOLE MODEL by the view that
    // OWNS them - this one, its primary, the primary's dependents - so one a
    // template, Visibility/Graphics or a temporary hide turns off still
    // counts. Found by category (see the lesson above), read as Dimension. A
    // reference into a link is left out and the dimension counted once.
    var owners = sharing(view);
    var heldSets = new List<HashSet<ElementId>>();
    var setsHolding = new Dictionary<ElementId, List<HashSet<ElementId>>>();
    Action<HashSet<ElementId>> remember = set =>
    {
        heldSets.Add(set);
        foreach (var id in set)
        {
            List<HashSet<ElementId>> list;
            if (!setsHolding.TryGetValue(id, out list)) { list = new List<HashSet<ElementId>>(); setsHolding[id] = list; }
            list.Add(set);
        }
    };
    foreach (var existing in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Dimensions)
                                 .WhereElementIsNotElementType())
    {
        if (!owners.Contains(existing.OwnerViewId)) continue;
        var asDimension = existing as Dimension;
        if (asDimension == null) continue;
        var set = new HashSet<ElementId>();
        try
        {
            var held = asDimension.References;
            if (held == null) continue;
            foreach (Reference r in held)
            {
                if (r == null) continue;
                if (r.LinkedElementId != null && r.LinkedElementId != ElementId.InvalidElementId)
                {
                    linkedDimensions.Add(existing.Id);
                    continue;
                }
                set.Add(r.ElementId);
            }
        }
        catch { continue; }
        remember(set);
    }

    // THE TARGETS, gathered once per view - never once per run - and only what
    // the view SHOWS: Revit throws on a reference to something the view does
    // not show, and the throw takes the whole string. Each is a plane square
    // to the paper: {kind, element id, normal, a point on it, points marking
    // its extent, its reference}. A run's reference is read only when needed.
    var planes = new List<Tuple<string, ElementId, XYZ, XYZ, List<XYZ>, Reference>>();
    var runsShown = new List<KeyValuePair<Element, Line>>();
    bool gridsRead = false, wallsRead = false, columnsRead = false, runsRead = false;
    var centrelines = new Dictionary<ElementId, Line>();

    var runOptions = new Options();
    runOptions.ComputeReferences = true;
    runOptions.IncludeNonVisibleObjects = true;
    runOptions.View = view;

    // A RUN'S CENTRELINE, AS A REFERENCE (CREATE_DIMENSION's lesson): read
    // with ComputeReferences and IncludeNonVisibleObjects, or there is none.
    Func<Element, Line> centrelineOf = element =>
    {
        Line found;
        if (centrelines.TryGetValue(element.Id, out found)) return found;
        found = null;
        try
        {
            var geometry = element.get_Geometry(runOptions);
            if (geometry != null)
                foreach (var part in geometry)
                {
                    var asLine = part as Line;
                    if (asLine != null && asLine.Reference != null) { found = asLine; break; }
                }
        }
        catch { found = null; }
        centrelines[element.Id] = found;
        return found;
    };

    Action readGrids = () =>
    {
        gridsRead = true;
        foreach (var grid in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Grid)).Cast<Grid>())
        {
            Line drawn = null;
            try
            {
                var inView = grid.GetCurvesInView(DatumExtentType.ViewSpecific, view);
                if (inView != null && inView.Count > 0) drawn = inView[0] as Line;
            }
            catch { drawn = null; }
            if (drawn == null) drawn = grid.Curve as Line;
            if (drawn == null) continue;
            var normal = viewDir.CrossProduct(drawn.Direction);
            if (normal.GetLength() < 1e-9) continue;
            planes.Add(Tuple.Create("grid", grid.Id, normal.Normalize(), drawn.GetEndPoint(0),
                                    new List<XYZ> { drawn.GetEndPoint(0), drawn.GetEndPoint(1) }, new Reference(grid)));
        }
    };

    Action readWalls = () =>
    {
        wallsRead = true;
        foreach (var wall in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Wall)).Cast<Wall>())
            foreach (var side in new[] { ShellLayerType.Exterior, ShellLayerType.Interior })
            {
                IList<Reference> sideFaces = null;
                try { sideFaces = HostObjectUtils.GetSideFaces(wall, side); } catch { sideFaces = null; }
                if (sideFaces == null) continue;
                foreach (var reference in sideFaces)
                {
                    PlanarFace planar = null;
                    try { planar = wall.GetGeometryObjectFromReference(reference) as PlanarFace; } catch { planar = null; }
                    if (planar == null) continue;
                    var extent = edgePoints(planar, Transform.Identity);
                    if (extent.Count == 0) continue;
                    planes.Add(Tuple.Create("wall", wall.Id, planar.FaceNormal.Normalize(), planar.Origin, extent, reference));
                }
            }
    };

    Action readColumns = () =>
    {
        columnsRead = true;
        var reading = new Options();
        reading.ComputeReferences = true;
        reading.View = view;
        foreach (var categoryId in columnIds)
            foreach (var column in new FilteredElementCollector(doc, view.Id).OfCategoryId(categoryId)
                                       .WhereElementIsNotElementType())
            {
                var columnId = column.Id;
                Action<GeometryElement, Transform> walk = null;
                walk = (geometry, placed) =>
                {
                    if (geometry == null) return;
                    foreach (GeometryObject item in geometry)
                    {
                        var nested = item as GeometryInstance;
                        if (nested != null) { walk(nested.GetSymbolGeometry(), placed.Multiply(nested.Transform)); continue; }
                        var solid = item as Solid;
                        if (solid == null || solid.Faces.Size == 0) continue;
                        foreach (Face face in solid.Faces)
                        {
                            var planar = face as PlanarFace;
                            if (planar == null || planar.Reference == null) continue;
                            var extent = edgePoints(planar, placed);
                            if (extent.Count == 0) continue;
                            planes.Add(Tuple.Create("column", columnId, placed.OfVector(planar.FaceNormal).Normalize(),
                                                    placed.OfPoint(planar.Origin), extent, planar.Reference));
                        }
                    }
                };
                try { walk(column.get_Geometry(reading), Transform.Identity); } catch { }
            }
    };

    Action readRuns = () =>
    {
        runsRead = true;
        foreach (var categoryId in runCategoryIds)
            foreach (var other in new FilteredElementCollector(doc, view.Id).OfCategoryId(categoryId)
                                      .WhereElementIsNotElementType())
            {
                var located = other.Location as LocationCurve;
                var axis = located == null ? null : located.Curve as Line;
                if (axis != null) runsShown.Add(new KeyValuePair<Element, Line>(other, axis));
            }
    };

    for (int r = 0; r < ruleCategory.Count; r++)
    {
        var categoryName = ruleCategory[r].Name;
        var ruleCategoryId = ruleCategory[r].Id;
        var targets = ruleTargets[r];
        double reach = ruleReachFeet[r];
        if (targets.Contains("grid") && !gridsRead) readGrids();
        if (targets.Contains("wall") && !wallsRead) readWalls();
        if (targets.Contains("column") && !columnsRead) readColumns();
        if ((targets.Contains("same") || targets.Contains("other")) && !runsRead) readRuns();
        var targetWords = string.Join(" or ", targets.Select(t => t == "same" ? "same-service run"
            : t == "other" ? "other-service run" : t).ToArray());

        foreach (var element in new FilteredElementCollector(doc, view.Id).OfCategoryId(ruleCategoryId)
                                    .WhereElementIsNotElementType())
        {
            var located = element.Location as LocationCurve;
            var line = located == null ? null : located.Curve as Line;
            if (line == null)
            {
                because(element.Id, categoryName + ": not a straight run - an arc, or no location line");
                continue;
            }

            // THE DIRECTION ON THE PAPER. A run seen end-on is a dot in the plan
            // with no direction across it, whatever skipVertical says; with
            // skipVertical, a run steeper than about 78 degrees is skipped too.
            var direction = line.Direction;
            var onPaper = direction - viewDir.Multiply(direction.DotProduct(viewDir));
            double paperShare = onPaper.GetLength();
            if (paperShare < 0.01)
            {
                because(element.Id, categoryName + ": vertical - seen end-on in the plan, nothing to measure across");
                continue;
            }
            if (skipVertical && paperShare < 0.2)
            {
                because(element.Id, categoryName + ": vertical - steeper than about 78 degrees (skipVertical)");
                continue;
            }
            if (minRunFeet > 0 && line.Length < minRunFeet)
            {
                because(element.Id, categoryName + ": shorter than " + minRunMm + " mm");
                continue;
            }

            // THE PART OF THE RUN THE VIEW SHOWS - clipped to the crop exactly,
            // so a short piece inside it is found however short.
            var shown = clipped(line);
            if (shown == null || (shown[1] - shown[0]) * line.Length < Tolerance)
            {
                because(element.Id, categoryName + ": no part of it inside the view's crop");
                continue;
            }

            // ALREADY DIMENSIONED - the owner's rule: a dimension of this view's
            // family holds this run TOGETHER WITH a reference of a kind this
            // rule names.
            if (skipDimensioned)
            {
                List<HashSet<ElementId>> holding;
                bool held = setsHolding.TryGetValue(element.Id, out holding)
                    && holding.Any(set => set.Any(id => id != element.Id && namedByRule(id, targets, ruleCategoryId)));
                if (held)
                {
                    because(element.Id, categoryName + ": already dimensioned in this view to a " + targetWords
                        + " - a dimension owned by it holds both, shown or hidden");
                    continue;
                }
            }

            var centreline = centrelineOf(element);
            if (centreline == null)
            {
                because(element.Id, categoryName + ": Revit gave no centreline reference to dimension to");
                continue;
            }

            // ALONG THE RUN, in the paper's terms. `along` is the run's direction
            // on the paper, `across` square to it; a point on the run is found
            // from where it lies along it.
            var along = onPaper.Normalize();
            var across = viewDir.CrossProduct(along).Normalize();
            var start = line.GetEndPoint(0);
            double startAlong = start.DotProduct(along);
            double perAlong = direction.DotProduct(along);      // > 0.01 here
            Func<double, XYZ> onRun = at => start + direction.Multiply((at - startAlong) / perAlong);
            double shownLow = line.Evaluate(shown[0], true).DotProduct(along);
            double shownHigh = line.Evaluate(shown[1], true).DotProduct(along);
            if (shownLow > shownHigh) { var swap = shownLow; shownLow = shownHigh; shownHigh = swap; }

            // WHERE THE STRING WOULD LIKE TO BE: the middle of the part shown,
            // moved `offsetMm` (paper) along the run - off the middle, where a
            // tag is put - and kept inside the part shown.
            double shownHalf = (shownHigh - shownLow) / 2;
            double shift = offsetFeet;
            if (shift > shownHalf) { shift = shownHalf; if (offsetFeet > 0) offsetsShortened++; }
            double preferred = (shownLow + shownHigh) / 2 + shift;

            // A TARGET'S DISTANCE ACROSS THE RUN at a point along it: where the
            // line across the run from that point meets the target's plane -
            // exact for a target up to the parallel tolerance off square.
            Func<XYZ, XYZ, double, double> crossing = (normal, point, at) =>
            {
                var station = onRun(at);
                double facing = across.DotProduct(normal);
                return (point - station).DotProduct(normal) / facing;
            };

            // EVERY TARGET THAT RUNS ALONGSIDE: parallel, sharing a stretch of the
            // part of the run the view shows (its OVERLAP), within reach across
            // it and off its own line, and - with the crop on - with the
            // string's end inside the crop. Its station is the point of the
            // overlap nearest where the string would like to be.
            // {distance across, station along, overlap low, overlap high,
            //  normal, point, reference, element id}
            var candidates = new List<Tuple<double, double, double, double, XYZ, XYZ, Reference, Tuple<ElementId>>>();
            Func<XYZ, List<XYZ>, double[]> overlapOf = (normal, extent) =>
            {
                double low = double.MaxValue, high = double.MinValue;
                foreach (var p in extent) { double at = p.DotProduct(along); low = Math.Min(low, at); high = Math.Max(high, at); }
                low = Math.Max(low, shownLow); high = Math.Min(high, shownHigh);
                return high >= low - Tolerance ? new[] { low, Math.Max(low, high) } : null;
            };
            Func<XYZ, XYZ, double[], double[]> placeOn = (normal, point, overlap) =>
            {
                double at = Math.Min(Math.Max(preferred, overlap[0]), overlap[1]);
                double distance = crossing(normal, point, at);
                if (Math.Abs(distance) <= Tolerance || Math.Abs(distance) > reach) return null;
                if (!inCrop(onRun(at) + across.Multiply(distance))) { offCropPairs++; return null; }
                return new[] { distance, at };
            };

            // Planes - grids, wall faces, column faces. For a wall or a column the
            // face NEARER the run; faces on both sides mean the run is inside it.
            var byElement = new Dictionary<ElementId, List<Tuple<double, double, double[], XYZ, XYZ, Reference>>>();
            foreach (var plane in planes)
            {
                if (!targets.Contains(plane.Item1)) continue;
                if (Math.Abs(plane.Item3.DotProduct(across)) <= ParallelCos) continue;
                var overlap = overlapOf(plane.Item3, plane.Item5);
                if (overlap == null) continue;
                var placed = placeOn(plane.Item3, plane.Item4, overlap);
                if (placed == null) continue;
                List<Tuple<double, double, double[], XYZ, XYZ, Reference>> list;
                if (!byElement.TryGetValue(plane.Item2, out list))
                {
                    list = new List<Tuple<double, double, double[], XYZ, XYZ, Reference>>();
                    byElement[plane.Item2] = list;
                }
                list.Add(Tuple.Create(placed[0], placed[1], overlap, plane.Item3, plane.Item4, plane.Item6));
            }
            foreach (var pair in byElement)
            {
                var faces = pair.Value;
                if (faces.Any(f => f.Item1 > 0) && faces.Any(f => f.Item1 < 0)) continue;
                var nearest = faces.OrderBy(f => Math.Abs(f.Item1)).First();
                candidates.Add(Tuple.Create(nearest.Item1, nearest.Item2, nearest.Item3[0], nearest.Item3[1],
                                            nearest.Item4, nearest.Item5, nearest.Item6, pair.Key));
            }

            // Runs - of the same service or of another, by their centrelines.
            if (targets.Contains("same") || targets.Contains("other"))
                foreach (var pair in runsShown)
                {
                    var other = pair.Key;
                    if (other.Id == element.Id || other.Category == null) continue;
                    bool sameService = other.Category.Id == ruleCategoryId;
                    if (sameService ? !targets.Contains("same") : !targets.Contains("other")) continue;
                    var otherLine = pair.Value;
                    var normal = viewDir.CrossProduct(otherLine.Direction);
                    if (normal.GetLength() < 1e-9) continue;
                    normal = normal.Normalize();
                    if (Math.Abs(normal.DotProduct(across)) <= ParallelCos) continue;
                    var overlap = overlapOf(normal, new List<XYZ> { otherLine.GetEndPoint(0), otherLine.GetEndPoint(1) });
                    if (overlap == null) continue;
                    var placed = placeOn(normal, otherLine.GetEndPoint(0), overlap);
                    if (placed == null) continue;
                    var otherCentre = centrelineOf(other);
                    if (otherCentre == null) continue;
                    candidates.Add(Tuple.Create(placed[0], placed[1], overlap[0], overlap[1], normal,
                                                otherLine.GetEndPoint(0), otherCentre.Reference, other.Id));
                }

            // THE SIDE(S): the closest target above the run and the closest below.
            Tuple<double, double, double, double, XYZ, XYZ, Reference, Tuple<ElementId>> plus = null, minus = null;
            foreach (var c in candidates)
            {
                if (c.Item1 > 0 && (plus == null || c.Item1 < plus.Item1)) plus = c;
                if (c.Item1 < 0 && (minus == null || c.Item1 > minus.Item1)) minus = c;
            }
            var chosen = new List<Tuple<double, double, double, double, XYZ, XYZ, Reference, Tuple<ElementId>>>();
            if (ruleBoth[r])
            {
                if (minus != null) chosen.Add(minus);
                if (plus != null) chosen.Add(plus);
            }
            else
            {
                if (plus != null && (minus == null || plus.Item1 <= -minus.Item1)) chosen.Add(plus);
                else if (minus != null) chosen.Add(minus);
            }
            if (chosen.Count == 0)
            {
                because(element.Id, categoryName + ": no " + targetWords + " within " + Math.Round(reach / MmToFeet)
                    + " mm across it on either side (running alongside the part of it the view shows, with the "
                    + "string's end inside the crop)");
                continue;
            }

            // THE STRINGS, each {station along, [distance across, reference, id]}.
            // One string on both sides needs a station both targets share:
            // where their overlaps meet, both measured again there. Where they
            // do not meet, or one falls out of reach there, the closer side
            // alone is drawn and counted.
            var pieces = new List<Tuple<double, List<Tuple<double, Reference, ElementId>>>>();
            Func<double, Tuple<double, Reference, ElementId>> self = at => Tuple.Create(0.0, centreline.Reference, element.Id);
            if (chosen.Count == 2 && !separate)
            {
                double low = Math.Max(chosen[0].Item3, chosen[1].Item3), high = Math.Min(chosen[0].Item4, chosen[1].Item4);
                var shared = new List<Tuple<double, Reference, ElementId>>();
                double at = 0;
                if (high >= low - Tolerance)
                {
                    at = Math.Min(Math.Max(preferred, low), Math.Max(low, high));
                    foreach (var c in chosen)
                    {
                        double distance = crossing(c.Item5, c.Item6, at);
                        if (Math.Abs(distance) <= Tolerance || Math.Abs(distance) > reach
                            || !inCrop(onRun(at) + across.Multiply(distance))) { shared = null; break; }
                        shared.Add(Tuple.Create(distance, c.Item7, c.Rest.Item1));
                    }
                }
                else shared = null;
                if (shared == null)
                {
                    sidesApart++;
                    var closer = Math.Abs(chosen[0].Item1) <= Math.Abs(chosen[1].Item1) ? chosen[0] : chosen[1];
                    chosen = new List<Tuple<double, double, double, double, XYZ, XYZ, Reference, Tuple<ElementId>>> { closer };
                }
                else
                {
                    shared.Add(self(at));
                    pieces.Add(Tuple.Create(at, shared.OrderBy(p => p.Item1).ToList()));
                }
            }
            if (pieces.Count == 0)
                foreach (var c in chosen)
                    pieces.Add(Tuple.Create(c.Item2, new List<Tuple<double, Reference, ElementId>>
                        { Tuple.Create(c.Item1, c.Item7, c.Rest.Item1), self(c.Item2) }.OrderBy(p => p.Item1).ToList()));
            bool oneSide = ruleBoth[r] && pieces.Sum(p => p.Item2.Count - 1) == 1;

            // THE SAME STRING TWICE is never drawn, whatever skipDimensioned
            // says: a piece holding exactly the elements of one already owned
            // by this view's family is left out.
            pieces = pieces.Where(p => !heldSets.Any(s => s.SetEquals(p.Item2.Select(q => q.Item3)))).ToList();
            if (pieces.Count == 0)
            {
                because(element.Id, categoryName + ": the same dimension is already in this view");
                continue;
            }

            var made = new List<Dimension>();
            string failure = null;
            foreach (var piece in pieces)
            {
                var station = onRun(piece.Item1);
                var refs = piece.Item2;
                var from = station + across.Multiply(refs[0].Item1);
                var to = station + across.Multiply(refs[refs.Count - 1].Item1);
                var array = new ReferenceArray();
                foreach (var p in refs) array.Append(p.Item2);
                Dimension dimension = null;
                try { dimension = doc.Create.NewDimension(view, Line.CreateBound(from, to), array, chosenType); }
                catch (Exception error)
                {
                    var words = (error.Message ?? "").Trim();
                    if (words.Length > 90) words = words.Substring(0, 90) + "...";
                    if (firstRefusal == null) firstRefusal = error.Message;
                    failure = "Revit refused the dimension - " + words;
                    break;
                }
                if (dimension == null) { failure = "Revit made no dimension and gave no reason"; break; }
                made.Add(dimension);

                if (dimension.GetTypeId() != chosenType.Id)
                {
                    bool corrected = false;
                    try { dimension.ChangeTypeId(chosenType.Id); corrected = dimension.GetTypeId() == chosenType.Id; } catch { }
                    if (!corrected) { failure = "the dimension came out as another type and would not change to '"
                                                + chosenType.Name + "'"; break; }
                    typeCorrections++;
                }

                // REGENERATED AND READ AGAIN AT ONCE. A string Revit cannot keep -
                // a reference it drops, or the whole dimension removed - is found
                // here and taken out now, rather than posting an error at commit
                // that would roll the whole call back.
                try { doc.Regenerate(); }
                catch (Exception error)
                {
                    if (firstRefusal == null) firstRefusal = error.Message;
                    failure = "Revit could not regenerate the plan with the string in it";
                    break;
                }
                if (!dimension.IsValidObject)
                {
                    removedOnRegenerate++;
                    failure = "Revit removed the string when the plan was regenerated";
                    break;
                }
                int heldCount = 0;
                try { heldCount = dimension.References == null ? 0 : dimension.References.Size; } catch { heldCount = 0; }
                if (heldCount < refs.Count)
                {
                    failure = "Revit kept " + heldCount + " of the " + refs.Count + " references asked for";
                    break;
                }
            }
            if (failure != null)
            {
                // ALL OR NOTHING per run: no half string, no one segment of two.
                foreach (var dimension in made)
                {
                    try { if (dimension.IsValidObject) doc.Delete(dimension.Id); } catch { }
                }
                try { doc.Regenerate(); } catch { }
                because(element.Id, categoryName + ": " + failure + ", so nothing was left on it");
                continue;
            }

            foreach (var dimension in made)
            {
                newDimensions.Add(dimension.Id);
                stringsHere++;
                var set = new HashSet<ElementId>();
                try { foreach (Reference held in dimension.References) if (held != null) set.Add(held.ElementId); } catch { }
                remember(set);
            }
            if (oneSide) oneSideOnly++;
            dimensioned++;
            runsHere++;
        }
    }

    bool hidden = false;
    try { hidden = view.GetCategoryHidden(new ElementId(BuiltInCategory.OST_Dimensions)); } catch { hidden = false; }
    if (hidden && stringsHere > 0) hiddenIn.Add("\"" + view.Name + "\"");

    var reasonsHere = viewWhyOrder.OrderByDescending(k => viewWhy[k]).Select(k => viewWhy[k] + " x " + k).ToList();
    perView.Add("\"" + view.Name + "\": " + runsHere + " run(s) dimensioned with " + stringsHere + " dimension(s)"
        + (reasonsHere.Count > 0 ? "; left: " + string.Join("; ", reasonsHere.ToArray()) : "; none left") + ".");
}
viewWhy = null;

// ---- THE ANSWER --------------------------------------------------------------
var skippedWhy = new List<string>();
foreach (var reason in whyOrder.OrderByDescending(k => whyCount[k]))
    skippedWhy.Add(whyCount[reason] + " x " + reason);

if (refused.Length == 0)
{
    findings.Add(dimensioned + " run(s) dimensioned with " + newDimensions.Count + " dimension(s) of type "
        + dimensionTypeUsed + " in " + working.Count + " plan(s) by " + ruleCategory.Count + " rule(s); "
        + skipped.Count + " run(s) left, each named in skipped with the reason in skippedWhy.");
    if (fromActive)
        findings.Add("No view was named, so the active view \"" + working[0].Name + "\" was dimensioned.");
    if (viewsLeft.Count > 0)
        findings.Add("Left out: " + string.Join("; ", viewsLeft.ToArray()) + ".");
    if (oneSideOnly > 0)
        findings.Add(oneSideOnly + " run(s) asked for both sides were dimensioned on one side only - a target was "
            + "found on one side alone, or the two did not share a stretch of the run.");
    if (sidesApart > 0)
        findings.Add(sidesApart + " of those had a target on each side that did not run alongside the same stretch "
            + "of the run, so one string could not hold both - chain separate draws each on its own stretch.");
    if (offCropPairs > 0)
        findings.Add(offCropPairs + " time(s) a target within a run's reach was passed over for that run, because "
            + "the string to it would have ended outside the view's crop.");
    if (offsetsShortened > 0)
        findings.Add(offsetsShortened + " run(s) show less than twice the offset inside the view, so their string sits "
            + "at the end of the part shown rather than " + offsetMm + " mm along it.");
    if (typeCorrections > 0)
        findings.Add(typeCorrections + " dimension(s) came out of Revit as another type and were changed to "
            + dimensionTypeUsed + ".");
    if (removedOnRegenerate > 0)
        findings.Add(removedOnRegenerate + " string(s) were removed by Revit when the plan regenerated; each run is "
            + "in skipped.");
    if (linkedDimensions.Count > 0)
        findings.Add(linkedDimensions.Count + " dimension(s) already in these views refer into a linked model; those "
            + "references were not read as elements of this one.");
    if (hiddenIn.Count > 0)
        findings.Add("Dimensions are turned off in " + string.Join(", ", hiddenIn.ToArray()) + ", so the ones drawn "
            + "there are in the model and NOT SHOWN.");
    findings.Add("Not read in this version: grids, walls, columns and runs inside LINKED models.");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    findings.Add(refused);
}
