// NOT STANDALONE. Assumes `doc`, `views`, `rules`, `dimensionType`, `offsetMm`,
// `minRunMm`, `skipVertical`, `skipDimensioned` and `chain` are in scope, and
// leaves `dimensioned`, `newDimensions`, `skipped`, `skippedWhy`, `perView`,
// `rulesRead`, `dimensionTypeUsed`, `refused` and `findings` behind.
//
// DIMENSION_VIEWS_BY_RULES, version 1 (2026-11-08). One pass over one plan or
// many: a RULE PER RUN CATEGORY says what each run is measured TO - grids,
// walls, columns, runs of the same service, runs of another service - on which
// side, and within what search band. Every straight run of those categories
// the plan shows gets ONE string across it, from the target(s) to the run's
// centreline, set along the part of the run the plan shows. Every run left
// without one is named with the reason.
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
// LESSONS STUDIED IN THE OWNER'S OWN ADD-IN (AJ-Tools, "Auto MEP Dimension"),
// written here in Heron's words - nothing of its code, names or layout:
//   - "Other services" means runs of ANOTHER service than the one measured,
//     and every service category is searched for them, not only the ones that
//     have a rule: what is dimensioned and what it is measured to are two
//     different lists. "Same service" is its own choice.
//   - A pipe's or duct's whole-element Reference is NOT a geometric reference:
//     NewDimension throws on it, and in one string that one bad reference
//     takes every good one down with it. It is never used here for a run.
//   - Two references at one position make a zero-length segment, which Revit
//     rejects - again taking the whole string. A target on the run's own line
//     is ignored.
//   - A grid is a usable end only when it runs PARALLEL to the run, and only
//     when it reaches the run's station along it - a grid square to the run
//     projects to a meaningless number that would win as "nearest".
//   - Linear dimensions are collected by CATEGORY: from Revit 2025 a linear
//     dimension is a LinearDimension, a subclass an exact-class collect misses.
//   - Whether a run is already dimensioned is a question about a dimension's
//     REFERENCES, never about where its string was drawn.
//   - A reference into a link carries the LINK INSTANCE as its ElementId; it
//     is never read as a host element of the same number.
//   - The crop box is read only when the crop is ON - off, Revit still hands
//     back an extent, and a stale one.
//   - The dimension line is read by Revit for position and direction only;
//     how far it runs past the last witness line is the dimension type's.
//   - Only a linear dimension type can draw a linear dimension.
//   - The run a string was drawn FOR is the one marked done - never every run
//     its string happens to touch, which retired runs nobody had dimensioned.
//   - Skip reasons are counted by reason, so the counts add up to the total.
//
// WHAT IS SAFER HERE, ON PURPOSE. No dimension type is ever the first one
// loaded: one is named, or the project's default linear type is used, or the
// call is refused before anything is drawn. A string Revit makes holding
// fewer references than asked, or of another type than asked and refusing to
// change, is taken out again and the run named - never a half string left.
// A run with separate segments is all or nothing. No dimension already in the
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
// executor makes the whole call one TransactionGroup, so every view in the
// list is ONE undo.

const double MmToFeet = 1.0 / 304.8;

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
//     run category | what to measure to | side | search band in mm
// e.g. "Ducts | grids, walls | both | 1500; Pipes | same-service | nearest".
// A LINE BREAK IS NOT A SEPARATOR, ON PURPOSE: revit_change hands a value over
// line by line and drops a line with no "=" in it, so a rule on a second line
// never arrives (TAG_VIEWS_BY_RULES, row 5b-1477). Text still holding a line
// break is refused rather than split, and `rulesRead` opens with the count.
//
// What to measure to: grids, walls, columns, same-service (runs of the rule's
// own category), other-service (runs of every OTHER run category), separated
// by commas. Required - there is no natural default.
// Side: nearest (left out; "one" says the same) - the single closest target,
// on whichever side it is; or both - the closest on each side.
// Search band: MODEL millimetres from the run's centreline, square to it.
// Left out, 3000 - a number this card chose, and rulesRead says it was used.
const double DefaultBandMm = 3000;
var ruleCategory = new List<Category>();
var ruleTargets = new List<HashSet<string>>();
var ruleBoth = new List<bool>();
var ruleBandFeet = new List<double>();
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
        ruleProblems.Add(said + " has " + parts.Length + " parts - a rule is at most four: run category | what to "
            + "measure to | side | search band in mm");
        continue;
    }
    var categoryText = parts[0].Trim();
    var targetText = parts.Length > 1 ? parts[1].Trim() : "";
    var sideText = parts.Length > 2 ? parts[2].Trim().ToLowerInvariant() : "";
    var bandText = parts.Length > 3 ? parts[3].Trim() : "";

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
        ruleProblems.Add(said + ": \"" + badTarget + "\" is not something this measures to - say grids, walls, "
            + "columns, same-service or other-service, separated by commas. Linked models are not read in this "
            + "version");
        continue;
    }
    if (targets.Count == 0)
    {
        ruleProblems.Add(said + ": say what " + category.Name + " are measured to - grids, walls, columns, "
            + "same-service or other-service - e.g. \"" + category.Name + " | grids, walls\"");
        continue;
    }

    bool both;
    if (sideText.Length == 0 || sideText == "nearest" || sideText == "one" || sideText == "one side") both = false;
    else if (sideText == "both" || sideText == "both sides") both = true;
    else
    {
        ruleProblems.Add(said + ": \"" + sideText + "\" is not a side - say nearest (the closest target, on "
            + "either side) or both (the closest on each side)");
        continue;
    }

    double bandMm = DefaultBandMm;
    if (bandText.Length > 0)
    {
        bandMm = readMm(bandText);
        if (double.IsNaN(bandMm) || bandMm <= 0)
        {
            ruleProblems.Add(said + ": \"" + bandText + "\" is not a search band - a length in model millimetres "
                + "from the run's centreline, above 0, e.g. 1500");
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
    ruleBandFeet.Add(bandMm * MmToFeet);
    rulesRead.Add(ruleCategory.Count + ". " + category.Name + " measured to " + string.Join(", ", targetWords.ToArray())
        + "; " + (both ? "both sides - the closest target on each" : "nearest - the closest target on either side")
        + "; search band " + bandMm + " mm" + (bandText.Length == 0 ? " (not given - this card's 3000)" : ""));
}

// ---- THE CALL-WIDE VALUES -----------------------------------------------------
var callProblems = new List<string>();

var chainWord = (chain ?? "").Trim().ToLowerInvariant();
bool separate;
if (chainWord.Length == 0 || chainWord == "one" || chainWord == "one string" || chainWord == "single") separate = false;
else if (chainWord == "separate" || chainWord == "segments" || chainWord == "separate segments") separate = true;
else
{
    separate = false;
    callProblems.Add("chain \"" + chain + "\" is not a chain style - say one (one string through every reference) "
        + "or separate (a dimension per gap)");
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
    refused = "No rules were given. Separate them with semicolons, each \"run category | what to measure to | side "
            + "| search band in mm\" - e.g. \"Ducts | grids, walls | both | 1500\". What to measure to is grids, walls, "
            + "columns, same-service or other-service; side is nearest (left out) or both.";
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

// Two directions are parallel when their unit vectors agree within about one
// degree, either way round.
Func<XYZ, XYZ, bool> parallel = (a, b) =>
{
    if (a == null || b == null || a.GetLength() < 1e-9 || b.GetLength() < 1e-9) return false;
    return Math.Abs(a.Normalize().DotProduct(b.Normalize())) > 0.9998;
};

int targetsOffCrop = 0;
int oneSideOnly = 0;
int offsetsShortened = 0;
int typeCorrections = 0;
int heldInLinks = 0;
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

    // THE DIMENSIONS ALREADY HERE, read from the WHOLE MODEL by the view that
    // OWNS them - this one, its primary, the primary's dependents - so one a
    // template, Visibility/Graphics or a temporary hide turns off still
    // counts. Collected by CATEGORY: from Revit 2025 a linear dimension is a
    // LinearDimension, which an exact-class collect would miss. What each
    // holds is read from its REFERENCES; a reference into a link carries the
    // link instance as its ElementId and is left out, never read as a host id.
    var owners = sharing(view);
    var heldRuns = new HashSet<ElementId>();
    var heldSets = new List<HashSet<ElementId>>();
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
                if (r.LinkedElementId != null && r.LinkedElementId != ElementId.InvalidElementId) { heldInLinks++; continue; }
                set.Add(r.ElementId);
            }
        }
        catch { continue; }
        foreach (var id in set) heldRuns.Add(id);
        heldSets.Add(set);
    }

    // THE TARGETS, gathered once per view - never once per run - and only what
    // the view SHOWS: Revit throws on a reference to something the view does
    // not show, and the throw takes the whole string.
    List<KeyValuePair<Grid, Line>> grids = null;
    List<Tuple<Element, Line, List<Tuple<Reference, XYZ, XYZ>>>> walls = null;
    List<Tuple<Element, double[], List<Tuple<Reference, XYZ, XYZ>>>> columns = null;
    List<KeyValuePair<Element, Line>> runsShown = null;
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

    Action gatherGrids = () =>
    {
        grids = new List<KeyValuePair<Grid, Line>>();
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
            if (drawn != null) grids.Add(new KeyValuePair<Grid, Line>(grid, drawn));
        }
    };

    Action gatherWalls = () =>
    {
        walls = new List<Tuple<Element, Line, List<Tuple<Reference, XYZ, XYZ>>>>();
        foreach (var wall in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Wall)).Cast<Wall>())
        {
            var located = wall.Location as LocationCurve;
            var axis = located == null ? null : located.Curve as Line;
            if (axis == null) continue;
            var faces = new List<Tuple<Reference, XYZ, XYZ>>();
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
                    faces.Add(Tuple.Create(reference, planar.FaceNormal, planar.Origin));
                }
            }
            if (faces.Count > 0) walls.Add(Tuple.Create((Element)wall, axis, faces));
        }
    };

    Action gatherColumns = () =>
    {
        columns = new List<Tuple<Element, double[], List<Tuple<Reference, XYZ, XYZ>>>>();
        var reading = new Options();
        reading.ComputeReferences = true;
        reading.View = view;
        foreach (var categoryId in columnIds)
            foreach (var column in new FilteredElementCollector(doc, view.Id).OfCategoryId(categoryId)
                                       .WhereElementIsNotElementType())
            {
                BoundingBoxXYZ box = null;
                try { box = column.get_BoundingBox(null); } catch { box = null; }
                if (box == null) continue;
                var faces = new List<Tuple<Reference, XYZ, XYZ>>();
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
                            faces.Add(Tuple.Create(planar.Reference, placed.OfVector(planar.FaceNormal).Normalize(),
                                                   placed.OfPoint(planar.Origin)));
                        }
                    }
                };
                try { walk(column.get_Geometry(reading), Transform.Identity); } catch { }
                if (faces.Count == 0) continue;
                columns.Add(Tuple.Create(column, new[] { box.Min.X, box.Min.Y, box.Min.Z, box.Max.X, box.Max.Y, box.Max.Z },
                                         faces));
            }
    };

    Action gatherRuns = () =>
    {
        runsShown = new List<KeyValuePair<Element, Line>>();
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
        var targets = ruleTargets[r];
        double band = ruleBandFeet[r];
        if (targets.Contains("grid") && grids == null) gatherGrids();
        if (targets.Contains("wall") && walls == null) gatherWalls();
        if (targets.Contains("column") && columns == null) gatherColumns();
        if ((targets.Contains("same") || targets.Contains("other")) && runsShown == null) gatherRuns();
        var targetWords = string.Join(" or ", targets.Select(t => t == "same" ? "same-service run"
            : t == "other" ? "other-service run" : t).ToArray());

        foreach (var element in new FilteredElementCollector(doc, view.Id).OfCategoryId(ruleCategory[r].Id)
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

            // THE PART OF THE RUN THE VIEW SHOWS. With the crop on, sampled in
            // the crop's own coordinates; the longest stretch inside is used.
            const int steps = 64;
            int bestStart = -1, bestLength = -1, runStart = -1;
            for (int i = 0; i <= steps; i++)
            {
                bool inside = false;
                try { inside = inCrop(line.Evaluate((double)i / steps, true)); } catch { inside = false; }
                if (!inside) { runStart = -1; continue; }
                if (runStart < 0) runStart = i;
                if (i - runStart > bestLength) { bestStart = runStart; bestLength = i - runStart; }
            }
            if (bestStart < 0)
            {
                because(element.Id, categoryName + ": no part of it inside the view's crop");
                continue;
            }

            if (skipDimensioned && heldRuns.Contains(element.Id))
            {
                because(element.Id, categoryName + ": already dimensioned in this view - a dimension owned by it "
                    + "holds the run, shown or hidden");
                continue;
            }

            var centreline = centrelineOf(element);
            if (centreline == null)
            {
                because(element.Id, categoryName + ": Revit gave no centreline reference to dimension to");
                continue;
            }

            // THE STATION: the middle of the visible stretch, moved `offsetMm`
            // (paper) along the run - off the middle, where a tag is put - and kept
            // inside the stretch.
            double t0 = (double)bestStart / steps, t1 = (double)(bestStart + bestLength) / steps;
            double stretch = (t1 - t0) * line.Length;
            double shift = offsetFeet;
            if (shift > stretch / 2)
            {
                shift = stretch / 2;
                if (offsetFeet > 0) offsetsShortened++;
            }
            double tStation = (t0 + t1) / 2 + (line.Length > 1e-9 ? shift / line.Length : 0);
            if (tStation > 1) tStation = 1;
            var station = line.Evaluate(tStation, true);
            var along = onPaper.Normalize();
            var across = viewDir.CrossProduct(along).Normalize();
            double stationAlong = station.DotProduct(along);
            const double tolerance = 1.0 * MmToFeet;   // 1 mm: closer than this is one position

            // A target counts when it is parallel, reaches this station along
            // the run, lies within the band and off the run's own line, and -
            // with the crop on - where the string would end is inside the crop.
            var candidates = new List<Tuple<double, Reference, ElementId>>();
            Func<double, double, bool> reaches = (low, high) =>
                stationAlong >= Math.Min(low, high) - tolerance && stationAlong <= Math.Max(low, high) + tolerance;
            Action<double, Reference, ElementId> offer = (offset, reference, id) =>
            {
                if (Math.Abs(offset) <= tolerance || Math.Abs(offset) > band) return;
                if (!inCrop(station + across.Multiply(offset))) { targetsOffCrop++; return; }
                candidates.Add(Tuple.Create(offset, reference, id));
            };
            // The face of an element nearer the run: both faces on one side, the
            // nearer taken; faces on both sides mean the run sits inside it.
            Action<List<Tuple<Reference, XYZ, XYZ>>, ElementId> offerNearFace = (faces, id) =>
            {
                double? bestOffset = null;
                Reference bestReference = null;
                int above = 0, below = 0;
                foreach (var face in faces)
                {
                    if (!parallel(face.Item2, across)) continue;
                    double offset = (face.Item3 - station).DotProduct(across);
                    if (offset > tolerance) above++;
                    else if (offset < -tolerance) below++;
                    if (bestOffset == null || Math.Abs(offset) < Math.Abs(bestOffset.Value))
                    {
                        bestOffset = offset;
                        bestReference = face.Item1;
                    }
                }
                if (bestReference == null || (above > 0 && below > 0)) return;
                offer(bestOffset.Value, bestReference, id);
            };

            if (targets.Contains("grid"))
                foreach (var pair in grids)
                {
                    var gridLine = pair.Value;
                    if (!parallel(gridLine.Direction, along)) continue;
                    if (!reaches(gridLine.GetEndPoint(0).DotProduct(along), gridLine.GetEndPoint(1).DotProduct(along))) continue;
                    offer((gridLine.GetEndPoint(0) - station).DotProduct(across), new Reference(pair.Key), pair.Key.Id);
                }
            if (targets.Contains("wall"))
                foreach (var wall in walls)
                {
                    if (!parallel(wall.Item2.Direction, along)) continue;
                    if (!reaches(wall.Item2.GetEndPoint(0).DotProduct(along), wall.Item2.GetEndPoint(1).DotProduct(along))) continue;
                    offerNearFace(wall.Item3, wall.Item1.Id);
                }
            if (targets.Contains("column"))
                foreach (var column in columns)
                {
                    var b = column.Item2;
                    double low = double.MaxValue, high = double.MinValue;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var p = new XYZ((corner & 1) == 0 ? b[0] : b[3], (corner & 2) == 0 ? b[1] : b[4],
                                        (corner & 4) == 0 ? b[2] : b[5]);
                        double at = p.DotProduct(along);
                        low = Math.Min(low, at); high = Math.Max(high, at);
                    }
                    if (!reaches(low, high)) continue;
                    offerNearFace(column.Item3, column.Item1.Id);
                }
            if (targets.Contains("same") || targets.Contains("other"))
                foreach (var pair in runsShown)
                {
                    var other = pair.Key;
                    if (other.Id == element.Id || other.Category == null) continue;
                    bool sameService = other.Category.Id == ruleCategory[r].Id;
                    if (sameService ? !targets.Contains("same") : !targets.Contains("other")) continue;
                    var otherLine = pair.Value;
                    if (!parallel(otherLine.Direction - viewDir.Multiply(otherLine.Direction.DotProduct(viewDir)), along)) continue;
                    if (!reaches(otherLine.GetEndPoint(0).DotProduct(along), otherLine.GetEndPoint(1).DotProduct(along))) continue;
                    double offset = (otherLine.GetEndPoint(0) - station).DotProduct(across);
                    if (Math.Abs(offset) <= tolerance || Math.Abs(offset) > band) continue;
                    var otherCentre = centrelineOf(other);
                    if (otherCentre == null) continue;
                    offer(offset, otherCentre.Reference, other.Id);
                }

            // THE SIDE(S): the closest target above the run and the closest below.
            Tuple<double, Reference, ElementId> plus = null, minus = null;
            foreach (var c in candidates)
            {
                if (c.Item1 > 0 && (plus == null || c.Item1 < plus.Item1)) plus = c;
                if (c.Item1 < 0 && (minus == null || c.Item1 > minus.Item1)) minus = c;
            }
            var chosen = new List<Tuple<double, Reference, ElementId>>();
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
                because(element.Id, categoryName + ": no " + targetWords + " within " + Math.Round(band / MmToFeet)
                    + " mm on either side (parallel to it, reaching it, and "
                    + "inside the crop)");
                continue;
            }
            bool oneSide = ruleBoth[r] && chosen.Count == 1;

            // THE STRINGS. One: every reference and the run's centreline, in
            // order across. Separate: one two-reference dimension per gap.
            var pieces = new List<List<Tuple<double, Reference, ElementId>>>();
            var self = Tuple.Create(0.0, centreline.Reference, element.Id);
            if (!separate || chosen.Count == 1)
            {
                var all = new List<Tuple<double, Reference, ElementId>>(chosen) { self };
                pieces.Add(all.OrderBy(p => p.Item1).ToList());
            }
            else
                foreach (var c in chosen) pieces.Add(new[] { c, self }.OrderBy(p => p.Item1).ToList());

            // THE SAME STRING TWICE is never drawn, whatever skipDimensioned
            // says: a piece holding exactly the elements of one already owned
            // by this view's family is left out.
            pieces = pieces.Where(p => !heldSets.Any(s => s.SetEquals(p.Select(q => q.Item3)))).ToList();
            if (pieces.Count == 0)
            {
                because(element.Id, categoryName + ": the same dimension is already in this view");
                continue;
            }

            var made = new List<Dimension>();
            string failure = null;
            foreach (var piece in pieces)
            {
                var from = station + across.Multiply(piece[0].Item1);
                var to = station + across.Multiply(piece[piece.Count - 1].Item1);
                var array = new ReferenceArray();
                foreach (var p in piece) array.Append(p.Item2);
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

                // THE READ-BACK: every reference asked for held, and the type
                // asked for - corrected and counted, or the string comes out.
                int heldCount = 0;
                try { heldCount = dimension.References == null ? 0 : dimension.References.Size; } catch { heldCount = 0; }
                if (heldCount < piece.Count)
                {
                    failure = "Revit kept " + heldCount + " of the " + piece.Count + " references asked for";
                    break;
                }
                if (dimension.GetTypeId() != chosenType.Id)
                {
                    bool corrected = false;
                    try { dimension.ChangeTypeId(chosenType.Id); corrected = dimension.GetTypeId() == chosenType.Id; } catch { }
                    if (!corrected) { failure = "the dimension came out as another type and would not change to '"
                                                + chosenType.Name + "'"; break; }
                    typeCorrections++;
                }
            }
            if (failure != null)
            {
                // ALL OR NOTHING per run: no half string, no one segment of two.
                foreach (var dimension in made) { try { doc.Delete(dimension.Id); } catch { } }
                because(element.Id, categoryName + ": " + failure + ", so nothing was left on it");
                continue;
            }

            foreach (var dimension in made)
            {
                newDimensions.Add(dimension.Id);
                stringsHere++;
                var set = new HashSet<ElementId>();
                try { foreach (Reference held in dimension.References) if (held != null) set.Add(held.ElementId); } catch { }
                heldSets.Add(set);
            }
            // THE RUN THIS WAS DRAWN FOR is marked done - never the targets it
            // touches, which may still need their own.
            heldRuns.Add(element.Id);
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
        findings.Add(oneSideOnly + " run(s) asked for both sides found a target on one side only, and were "
            + "dimensioned on that side.");
    if (targetsOffCrop > 0)
        findings.Add(targetsOffCrop + " target(s) inside the search band were passed over because the string to them "
            + "would end outside the view's crop.");
    if (offsetsShortened > 0)
        findings.Add(offsetsShortened + " run(s) show less than twice the offset inside the view, so their string sits "
            + "at the end of the part shown rather than " + offsetMm + " mm along it.");
    if (typeCorrections > 0)
        findings.Add(typeCorrections + " dimension(s) came out of Revit as another type and were changed to "
            + dimensionTypeUsed + ".");
    if (heldInLinks > 0)
        findings.Add(heldInLinks + " reference(s) of dimensions already in these views point into a linked model; "
            + "they were not read as runs of this one.");
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
