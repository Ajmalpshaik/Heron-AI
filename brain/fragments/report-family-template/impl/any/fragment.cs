// NOT STANDALONE. Assumes `doc` is in scope; leaves `familyKind`,
// `templateReport`, `planeList`, `pointList`, `lineList`, `notAFamily` and
// `findings` behind.
//
// READS ONLY. Nothing is changed.
//
// WHAT KIND OF FAMILY IS OPEN - the question every family job starts from. The
// TEMPLATE decided it when the family was made and nothing later changes it: a
// wall-based family cannot become a face-based one, a line-based family takes
// its Length from two clicks, a pattern-based panel is driven by the points of
// its tile pattern, a profile is a closed 2D shape a sweep is drawn with. The
// method that builds each one differs, so the kind is READ here - from the
// family's own placement, its category and the host the template carries -
// never guessed from the family's name.
//
// WITH WHAT THE TEMPLATE ALREADY HOLDS: its named reference planes, each with
// where it sits, what it faces and its Is Reference; the host element (a wall, a
// ceiling, a floor, a roof); the built-in parameters (a line-based family's
// Length, a door's Width and Height); the family's own switches; the reference
// lines; the adaptive points with their placement numbers; a pattern-based
// panel's tile pattern and spacing; the views; and the types. Every later step
// names planes and points by what this reads.
//
// ONE LINE PER ANSWER, NOT A LIST. A list is cut to three entries before anyone
// reads it - the reason REPORT_FAMILY_PARAMETERS answers on one line too.

var findings = new List<string>();
var familyKind = "";
var templateReport = "";
var planeList = "";
var pointList = "";
var lineList = "";
var notAFamily = false;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);

// A parameter's value as the Properties palette shows it, or its stored text.
Func<Parameter, string> shown = p =>
{
    try
    {
        if (p == null) return "";
        var words = p.AsValueString();
        if (!string.IsNullOrEmpty(words)) return words;
        return p.StorageType == StorageType.String ? (p.AsString() ?? "") : "";
    }
    catch (Exception) { return ""; }
};
Func<Parameter, int?> stored = p =>
{
    try { return p == null || p.StorageType != StorageType.Integer ? (int?)null : p.AsInteger(); }
    catch (Exception) { return null; }
};

// The axis a plane faces: 0 for X (a left-right plane), 1 for Y (front-back), 2
// for Z (a height), -1 when it leans.
Func<XYZ, int> axisOf = n =>
    n == null ? -1 : Math.Abs(n.X) > 0.999 ? 0 : Math.Abs(n.Y) > 0.999 ? 1 : Math.Abs(n.Z) > 0.999 ? 2 : -1;
var axisLetters = new[] { "X", "Y", "Z" };

// Elements of one category the template carries, types left out.
Func<BuiltInCategory, int> countOf = bic =>
{
    try
    {
        return new FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType().GetElementCount();
    }
    catch (Exception) { return 0; }
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "What kind of family it is is read inside the family - open it for editing first "
        + "(OPEN_FAMILY_FOR_EDITING), or start one from a template (CREATE_FAMILY_DOCUMENT).");
}
else
{
    var family = doc.OwnerFamily;
    var category = family == null ? null : family.FamilyCategory;
    var categoryName = category == null ? "no category" : category.Name;
    var placement = FamilyPlacementType.Invalid;
    try { if (family != null) placement = family.FamilyPlacementType; }
    catch (Exception) { }

    Func<BuiltInCategory, bool> isCategory = bic => category != null && category.Id == new ElementId(bic);
    var isAnnotation = false;
    try { isAnnotation = category != null && category.CategoryType == CategoryType.Annotation; }
    catch (Exception) { }

    var curtainPanel = false;
    var adaptive = false;
    var conceptual = false;
    try { curtainPanel = family != null && family.IsCurtainPanelFamily; } catch (Exception) { }
    try { adaptive = family != null && AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(family); } catch (Exception) { }
    try { conceptual = family != null && family.IsConceptualMassFamily; } catch (Exception) { }
    // Reference points live only in a conceptual family - a mass, an adaptive
    // component, a pattern-based panel (Revit's remark on NewReferencePoint) -
    // so a curtain-panel family WITH them is the pattern-based kind, and one
    // without is the classic panel a curtain grid sizes. The flag alone is not
    // trusted to tell the two apart (BV1).
    var referencePoints = 0;
    try { referencePoints = new FilteredElementCollector(doc).OfClass(typeof(ReferencePoint)).GetElementCount(); }
    catch (Exception) { }

    var walls = countOf(BuiltInCategory.OST_Walls);
    var ceilings = countOf(BuiltInCategory.OST_Ceilings);
    var floors = countOf(BuiltInCategory.OST_Floors);
    var roofs = countOf(BuiltInCategory.OST_Roofs);

    var hostWords = family == null ? "" : shown(family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR));
    var planeSwitch = family == null ? null : stored(family.get_Parameter(BuiltInParameter.FAMILY_WORK_PLANE_BASED));

    // -----------------------------------------------------------------------
    // THE KIND, in the words a modeller uses for the template it came from.
    // Most specific first: a 2D family by its category, a conceptual one by
    // Revit's own flags, everything else by how it is placed.
    // -----------------------------------------------------------------------
    var hostOfTemplate = walls > 0 ? "wall" : ceilings > 0 ? "ceiling" : floors > 0 ? "floor" : roofs > 0 ? "roof" : "";
    // Each named category before the annotation test, which is the broad one:
    // whichever CategoryType Revit gives Detail Items or Profiles, they are
    // read as what they are.
    if (isCategory(BuiltInCategory.OST_TitleBlocks))
        familyKind = "a title block - 2D, placed on a sheet";
    else if (isCategory(BuiltInCategory.OST_DetailComponents))
        familyKind = placement == FamilyPlacementType.CurveBasedDetail
            ? "a line-based detail item - 2D, placed in a view with two clicks, its Length set by them"
            : "a detail item - 2D, placed in a view";
    else if (isCategory(BuiltInCategory.OST_ProfileFamilies))
        familyKind = "a profile - a closed 2D shape that a sweep, a wall sweep, a reveal, a slab edge, a railing or a "
            + "mullion is drawn with";
    else if (isAnnotation)
        familyKind = "an annotation family - 2D, drawn in a view: a tag, a symbol or a generic annotation";
    else if (isCategory(BuiltInCategory.OST_CurtainWallPanels) && referencePoints == 0)
        familyKind = "a curtain wall panel - the classic kind, sized by the curtain grid cell it is placed in, not by "
            + "parameters of its own; not pattern-based";
    // A Generic Model Pattern Based family is the same kind in another
    // category - Revit's remark on DividedSurface takes "a FamilySymbol element
    // from a Curtain Panel family" as a divided surface's type - so the
    // category is named rather than every one called a curtain panel (BV1).
    else if (curtainPanel && referencePoints > 0)
        familyKind = "a pattern-based " + (isCategory(BuiltInCategory.OST_CurtainWallPanels) ? "curtain panel"
                : "family of the " + (category == null ? "(no)" : category.Name) + " category")
            + " - its shape driven by the points of its tile pattern, so it fits each cell of a divided surface";
    else if (adaptive)
        familyKind = "an adaptive component - placed by clicking its adaptive points in order, its shape following them";
    else if (conceptual)
        familyKind = "a conceptual mass";
    else if (placement == FamilyPlacementType.CurveBased)
        familyKind = "line-based - placed with two clicks on a level, a work plane or a face, its Length set by them";
    else if (placement == FamilyPlacementType.CurveBasedDetail)
        familyKind = "a line-based detail item - 2D, placed in a view with two clicks";
    else if (placement == FamilyPlacementType.OneLevelBasedHosted)
        familyKind = hostOfTemplate.Length > 0
            ? hostOfTemplate + "-based - placed only on a " + hostOfTemplate + ", which the template carries"
            : "hosted - but no wall, ceiling, floor or roof was found in the template to say which";
    else if (placement == FamilyPlacementType.WorkPlaneBased)
        familyKind = planeSwitch == 1
            ? "work-plane-based - placed on a level, a reference plane or a face, and moving with it (the family's "
              + "Work Plane-Based switch is on)"
            : "face-based - placed on a face of a wall, floor, ceiling or other element, or on a work plane (its "
              + "placement comes from the template, not from the Work Plane-Based switch)";
    else if (placement == FamilyPlacementType.OneLevelBased)
        familyKind = "level-based - not hosted, placed on a level";
    else if (placement == FamilyPlacementType.TwoLevelsBased)
        familyKind = "two-level - placed between a base and a top level, the way a column is";
    else if (placement == FamilyPlacementType.CurveDrivenStructural)
        familyKind = "structural framing - a beam or brace drawn between two points";
    else if (placement == FamilyPlacementType.ViewBased)
        familyKind = "view-based - 2D, placed in a view";
    else
        familyKind = "not one Revit names - its placement reads " + placement;

    // -----------------------------------------------------------------------
    // THE FAMILY'S OWN SWITCHES, in Family Category and Parameters. Only those
    // this family's category carries; Revit's own words for each value.
    // -----------------------------------------------------------------------
    var switches = new List<Tuple<string, BuiltInParameter, bool>>
    {
        Tuple.Create("Host", BuiltInParameter.FAMILY_HOSTING_BEHAVIOR, false),
        Tuple.Create("Work Plane-Based", BuiltInParameter.FAMILY_WORK_PLANE_BASED, true),
        Tuple.Create("Always Vertical", BuiltInParameter.FAMILY_ALWAYS_VERTICAL, true),
        Tuple.Create("Cut with Voids When Loaded", BuiltInParameter.FAMILY_ALLOW_CUT_WITH_VOIDS, true),
        Tuple.Create("Shared", BuiltInParameter.FAMILY_SHARED, true),
        Tuple.Create("Room Calculation Point", BuiltInParameter.ROOM_CALCULATION_POINT, true),
        Tuple.Create("Maintain Annotation Orientation", BuiltInParameter.FAMILY_ELECTRICAL_MAINTAIN_ANNOTATION_ORIENTATION, true),
        Tuple.Create("Rotate with component", BuiltInParameter.FAMILY_ROTATE_WITH_COMPONENT, true),
        Tuple.Create("Keep text readable", BuiltInParameter.FAMILY_KEEP_TEXT_READABLE, true),
        Tuple.Create("Part Type", BuiltInParameter.FAMILY_CONTENT_PART_TYPE, false),
        Tuple.Create("Profile Usage", BuiltInParameter.FAM_PROFILE_USAGE, false),
        Tuple.Create("Material for Model Behavior", BuiltInParameter.FAMILY_STRUCT_MATERIAL_TYPE, false),
        Tuple.Create("Section Shape", BuiltInParameter.STRUCTURAL_SECTION_SHAPE, false),
    };
    // A structural family's two named settings, read as the enum's own name
    // where the palette's words come back blank. Section Shape's enum lives in
    // a namespace the executor does not import, so it is read from the Family
    // property that carries it, as SET_FAMILY_SETTINGS does.
    var enumOf = new Dictionary<BuiltInParameter, Type>
    {
        { BuiltInParameter.FAMILY_STRUCT_MATERIAL_TYPE, typeof(StructuralMaterialType) },
    };
    var sectionShapeProperty = typeof(Family).GetProperty("StructuralSectionShape");
    if (sectionShapeProperty != null && sectionShapeProperty.PropertyType.IsEnum)
        enumOf[BuiltInParameter.STRUCTURAL_SECTION_SHAPE] = sectionShapeProperty.PropertyType;
    var settingRows = new List<string>();
    if (family != null)
        foreach (var s in switches)
        {
            var row = family.get_Parameter(s.Item2);
            if (row == null) continue;
            var value = s.Item3 ? (stored(row) == 1 ? "Yes" : stored(row) == 0 ? "No" : shown(row)) : shown(row);
            var code = stored(row);
            if (value.Length == 0 && code.HasValue && enumOf.ContainsKey(s.Item2) && Enum.IsDefined(enumOf[s.Item2], code.Value))
                value = Enum.GetName(enumOf[s.Item2], code.Value);
            settingRows.Add(s.Item1 + " " + (value.Length == 0 ? "(blank)" : value));
        }

    // -----------------------------------------------------------------------
    // THE REFERENCE PLANES: name, the axis it faces, where it sits along it,
    // Is Reference, and Defines Origin. Read before any plane is named in a
    // later step - the template's own planes are pinned and are not moved.
    // -----------------------------------------------------------------------
    var planeRows = new List<Tuple<int, double, string>>();
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
        var name = named == null ? "" : (named.AsString() ?? "");
        var axis = axisOf(rp.Normal);
        double at = 0;
        try
        {
            var origin = rp.GetPlane().Origin;
            at = axis == 0 ? origin.X : axis == 1 ? origin.Y : axis == 2 ? origin.Z : 0;
        }
        catch (Exception) { }
        var isReference = shown(rp.get_Parameter(BuiltInParameter.ELEM_REFERENCE_NAME)
            ?? rp.get_Parameter(BuiltInParameter.ELEM_REFERENCE_NAME_2D_XZ));
        var origin2 = stored(rp.get_Parameter(BuiltInParameter.DATUM_PLANE_DEFINES_ORIGIN)) == 1;
        planeRows.Add(Tuple.Create(axis < 0 ? 3 : axis, at,
            (name.Length == 0 ? "(unnamed " + rp.UniqueId + ")" : name)
            + ": " + (axis < 0 ? "leaning" : "faces " + axisLetters[axis] + ", at " + axisLetters[axis] + " " + mm(at) + " mm")
            + (isReference.Length > 0 ? ", Is Reference " + isReference : "")
            + (origin2 ? ", defines origin" : "")
            + (rp.Pinned ? ", pinned" : "")));
    }
    planeList = string.Join("  ||  ", planeRows.OrderBy(r => r.Item1).ThenBy(r => r.Item2).Select(r => r.Item3));

    // -----------------------------------------------------------------------
    // REFERENCE LINES - the lines a part that swings about a pivot is built on
    // - and every line drawn THROUGH POINTS, which a pattern-based or adaptive
    // family builds its forms on: each with its id, so a form tool can name it.
    // -----------------------------------------------------------------------
    var referenceLines = 0;
    var lineRows = new List<string>();
    // A point by its placement number when it has one, else by its id.
    Func<ReferencePoint, string> pointName = rp =>
    {
        try
        {
            if (AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, rp.Id))
                return AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, rp.Id).ToString(invariant);
        }
        catch (Exception) { }
        return rp.UniqueId;
    };
    foreach (var c in new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).Cast<CurveElement>())
    {
        double length = 0;
        try { length = c.GeometryCurve == null ? 0 : c.GeometryCurve.Length; } catch (Exception) { }
        var model = c as ModelCurve;
        var byPoints = c as CurveByPoints;
        if (model != null)
        {
            bool isLine;
            try { isLine = model.IsReferenceLine; } catch (Exception) { isLine = false; }
            if (!isLine) continue;
            referenceLines++;
            lineRows.Add("reference line " + c.UniqueId + ", " + mm(length) + " mm");
        }
        else if (byPoints != null)
        {
            var through = new List<string>();
            try
            {
                var throughPoints = byPoints.GetPoints();
                for (var i = 0; i < throughPoints.Size; i++) through.Add(pointName(throughPoints.get_Item(i)));
            }
            catch (Exception) { }
            bool isLine;
            try { isLine = byPoints.IsReferenceLine; } catch (Exception) { isLine = false; }
            if (isLine) referenceLines++;
            lineRows.Add((isLine ? "reference" : "model") + " curve through " + string.Join(",", through) + " "
                + c.UniqueId + ", " + mm(length) + " mm");
        }
    }
    lineList = string.Join("  ||  ", lineRows);

    // -----------------------------------------------------------------------
    // POINTS - an adaptive or pattern-based family is driven by them. Each with
    // its id, its kind, its placement number and where it is.
    // -----------------------------------------------------------------------
    var pointRows = new List<Tuple<int, string>>();
    var plainPoints = 0;
    List<ReferencePoint> points;
    try { points = new FilteredElementCollector(doc).OfClass(typeof(ReferencePoint)).Cast<ReferencePoint>().ToList(); }
    catch (Exception) { points = new List<ReferencePoint>(); }
    foreach (var point in points)
    {
        bool isAdaptive = false, isPlacement = false, isHandle = false;
        var number = 0;
        try { isAdaptive = AdaptiveComponentFamilyUtils.IsAdaptivePoint(doc, point.Id); } catch (Exception) { }
        try { isPlacement = AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, point.Id); } catch (Exception) { }
        try { isHandle = AdaptiveComponentFamilyUtils.IsAdaptiveShapeHandlePoint(doc, point.Id); } catch (Exception) { }
        if (isPlacement)
            try { number = AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, point.Id); } catch (Exception) { }
        if (!isAdaptive && !isPlacement && !isHandle) { plainPoints++; continue; }
        var where = "";
        try
        {
            var at = point.Position;
            where = " at " + mm(at.X) + ", " + mm(at.Y) + ", " + mm(at.Z) + " mm";
        }
        catch (Exception) { }
        var kind = isPlacement ? "placement point " + number : isHandle ? "shape handle point" : "adaptive point";
        pointRows.Add(Tuple.Create(isPlacement ? number : 1000, kind + " " + point.UniqueId + where));
    }
    pointList = string.Join("  ||  ", pointRows.OrderBy(r => r.Item1).Select(r => r.Item2));

    // -----------------------------------------------------------------------
    // A PATTERN-BASED PANEL'S GRID: the tile pattern and its spacing.
    // -----------------------------------------------------------------------
    var grid = "";
    if (curtainPanel && referencePoints > 0)
    {
        try
        {
            grid = "tile pattern " + family.CurtainPanelTilePattern + ", spacing " + mm(family.CurtainPanelHorizontalSpacing)
                + " x " + mm(family.CurtainPanelVerticalSpacing) + " mm";
        }
        catch (Exception ex) { grid = "tile pattern unread: " + ex.Message; }
    }

    // -----------------------------------------------------------------------
    // THE BUILT-IN PARAMETERS the template starts with - a line-based family's
    // Length, a door's Width and Height - with their value in the current type.
    // -----------------------------------------------------------------------
    var manager = doc.FamilyManager;
    var current = manager.CurrentType;
    var builtIn = new List<string>();
    var ownParameters = 0;
    foreach (FamilyParameter p in manager.GetParameters())
    {
        var definition = p.Definition as InternalDefinition;
        var isBuiltIn = false;
        try { isBuiltIn = definition != null && definition.BuiltInParameter != BuiltInParameter.INVALID; }
        catch (Exception) { }
        if (!isBuiltIn) { ownParameters++; continue; }
        // BY STORAGE, as REPORT_FAMILY_PARAMETERS reads them: AsValueString is
        // null for text and for an element, which would show a filled Type
        // Comments or Description as blank.
        var value = "";
        try
        {
            if (current == null) value = "";
            else if (p.StorageType == StorageType.String) value = current.AsString(p) ?? "";
            else if (p.StorageType == StorageType.ElementId)
            {
                var id = current.AsElementId(p);
                var referenced = id == null ? null : doc.GetElement(id);
                value = referenced == null ? "" : referenced.Name;
            }
            else value = current.AsValueString(p) ?? "";
        }
        catch (Exception) { }
        builtIn.Add(p.Definition.Name + " [" + (p.IsInstance ? "instance" : "type") + (p.IsReporting ? ", reporting" : "")
            + "]" + (value.Length > 0 ? " = " + value : ""));
    }

    // The family's own views - not the browser panes and housekeeping views,
    // which are View elements too (FIND_VIEWS leaves the same ones out).
    var views = new List<string>();
    foreach (var v in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        if (v.IsTemplate) continue;
        if (v.ViewType == ViewType.ProjectBrowser || v.ViewType == ViewType.SystemBrowser
            || v.ViewType == ViewType.Internal || v.ViewType == ViewType.Undefined) continue;
        views.Add(v.Name + " (" + v.ViewType + ")");
    }

    var types = 0;
    foreach (FamilyType t in manager.Types) types++;

    var hosts = new List<string>();
    if (walls > 0) hosts.Add(walls + " wall(s)");
    if (ceilings > 0) hosts.Add(ceilings + " ceiling(s)");
    if (floors > 0) hosts.Add(floors + " floor(s)");
    if (roofs > 0) hosts.Add(roofs + " roof(s)");

    var parts = new List<string>
    {
        "Family \"" + (family == null ? doc.Title : family.Name) + "\"",
        "category " + categoryName,
        "kind: " + familyKind,
        "placement " + placement,
        "host elements in the template: " + (hosts.Count == 0 ? "none" : string.Join(", ", hosts)),
        "settings: " + (settingRows.Count == 0 ? "none this category carries" : string.Join(", ", settingRows)),
        "built-in parameters: " + (builtIn.Count == 0 ? "none" : string.Join(", ", builtIn)),
        "own parameters: " + ownParameters,
        "reference planes: " + planeRows.Count,
        "reference lines: " + referenceLines,
        "adaptive points: " + pointRows.Count + (plainPoints > 0 ? ", other reference points: " + plainPoints : ""),
        "types: " + types + (current == null ? ", none current" : ", current \"" + current.Name + "\""),
        "views: " + (views.Count == 0 ? "none" : string.Join(", ", views.Take(20)) + (views.Count > 20 ? ", ..." : "")),
    };
    if (grid.Length > 0) parts.Insert(5, grid);
    templateReport = string.Join(" | ", parts);

    findings.Add("\"" + (family == null ? doc.Title : family.Name) + "\" is " + familyKind + ". Category "
        + categoryName + "; " + planeRows.Count + " reference plane(s), " + referenceLines + " reference line(s), "
        + pointRows.Count + " adaptive point(s), " + types + " type(s).");
    if (hostWords.Length > 0)
        findings.Add("Family Category and Parameters reads Host \"" + hostWords + "\" - Revit's own word for how it is "
            + "placed. The template decided it; no setting changes a hosted family into another kind, and moving "
            + "one means starting again from the other template.");
    if (types == 0)
        findings.Add("The family has no type yet. A formula, and any value, needs one - SET_FAMILY_TYPE_VALUES "
            + "makes the first.");
}
