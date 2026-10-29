// NOT STANDALONE. Assumes `doc` is in scope; leaves `familyKind`,
// `templateReport`, `planeList`, `pointList`, `lineList`,
// `classificationCodes`, `notAFamily` and `findings` behind.
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
//
// VERSION 2 (2026-10-11): THE OMNICLASS NUMBER AND TITLE ("Classification
// Number" and "Classification Title" from Revit 2026) join the settings, and
// `classificationCodes` lists the codes the classification file gives THIS
// family's category - what a family build proposes the code from, and what
// SET_FAMILY_SETTINGS accepts. Or, plainly, why there are none.
//
// VERSION 3 (2026-10-12): THE LENGTH UNITS join templateReport - "units:
// Length = Millimeters (mm), rounding 1 - metric" - with a finding when they
// are not metric, so a family build's first step catches a template set to
// feet and inches without a call of its own.
//
// VERSION 4 (2026-10-29): EACH ADAPTIVE POINT'S ORIENTATION joins its row in
// `pointList`, after the position version 3 already gave - "placement point 1
// <id> at 0, 0, 0 mm, Orientation Host and Loop System (xyz)" - in the words
// SET_ADAPTIVE_POINT_ORIENTATION prints, from the same mapping, so what that
// write sets can be read back here. Only a PLACEMENT point is asked: Revit's
// reference (RevitAPI.xml, 2020 to 2027) says GetPointOrientationType throws
// for any other point, so a shape handle point reads "Orientation not read
// (shape handle point)" and no call is made for it - never a guessed value
// (D-52). One finding says the placement points' Orientations together. A
// family with no adaptive points prints none, as before. Nothing version 3
// answered changed.

var findings = new List<string>();
var familyKind = "";
var templateReport = "";
var planeList = "";
var pointList = "";
var lineList = "";
var classificationCodes = "";
var notAFamily = false;

// ---------------------------------------------------------------------------
// THE OMNICLASS NUMBER'S PARAMETER, AND THE CLASSIFICATION FILE (version 2)
// ---------------------------------------------------------------------------
//
// ITS BUILT-IN PARAMETER WAS RENAMED AT REVIT 2026 AND KEPT ITS NUMBER:
// OMNICLASS_CODE and OMNICLASS_DESCRIPTION up to 2025, CLASSIFICATION_CODE and
// CLASSIFICATION_DESCRIPTION from 2026, and neither pair exists on the other
// side. Naming either in code breaks the build on the other releases, so the
// one this Revit has is found by NAME at run time, and shown in Revit's own
// words - "OmniClass Number" on 2024, "Classification Number" on 2026.
var clsNewNames = Enum.IsDefined(typeof(BuiltInParameter), "CLASSIFICATION_CODE");
var clsCode = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter), clsNewNames ? "CLASSIFICATION_CODE" : "OMNICLASS_CODE");
var clsTitle = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter),
    Enum.IsDefined(typeof(BuiltInParameter), "CLASSIFICATION_DESCRIPTION") ? "CLASSIFICATION_DESCRIPTION" : "OMNICLASS_DESCRIPTION");
Func<BuiltInParameter, string, string> clsLabelFor = (clsBip, clsOtherwise) =>
{
    try
    {
        var clsSaid = LabelUtils.GetLabelFor(clsBip);
        return string.IsNullOrEmpty(clsSaid) ? clsOtherwise : clsSaid;
    }
    catch (Exception) { return clsOtherwise; }
};
var clsCodeLabel = clsLabelFor(clsCode, clsNewNames ? "Classification Number" : "OmniClass Number");
var clsTitleLabel = clsLabelFor(clsTitle, clsNewNames ? "Classification Title" : "OmniClass Title");

// Trimmed of every character char.IsWhiteSpace calls space - the 2020 to 2025
// file ends some titles in a non-breaking space.
Func<string, string> clsTidy = clsText =>
{
    var clsS = clsText ?? "";
    int clsFrom = 0, clsTo = clsS.Length;
    while (clsFrom < clsTo && char.IsWhiteSpace(clsS[clsFrom])) clsFrom++;
    while (clsTo > clsFrom && char.IsWhiteSpace(clsS[clsTo - 1])) clsTo--;
    return clsS.Substring(clsFrom, clsTo - clsFrom);
};

// THE CLASSIFICATION FILE - the list Revit's own OmniClass Number dialog
// offers. No API reads it; it is a plain file, one row per line, TAB between
// code, title, level and the BuiltInCategory it is filed under (that last one
// may be empty), and '#' starts a comment: "OmniClassTaxonomy.txt" up to 2025,
// "Classification_Taxonomy.txt" from 2026, the name that matches the parameter
// found above tried first, and the path actually read is said. ONLY THE
// PER-USER COPY IS READ - the one Revit itself reads, in
// Application.CurrentUsersDataFolderPath, which Revit fills from its own cache
// at start-up. That cache sits under a Windows special folder, and only
// HeronPaths may resolve one (tools/check-structure.py, docs/06 section 2).
var clsTried = new List<string>();
string clsPath = null;
string clsProblem = null;
var clsSample = false;
// code, title, the category column as written, the BuiltInCategory (boxed) or null
var clsRows = new List<Tuple<string, string, string, object>>();
Action clsLoad = () =>
{
    var clsFolders = new List<string>();
    try
    {
        var clsUser = doc.Application.CurrentUsersDataFolderPath;
        if (!string.IsNullOrEmpty(clsUser)) clsFolders.Add(clsUser);
    }
    catch (Exception) { }
    var clsNames = clsNewNames
        ? new[] { "Classification_Taxonomy.txt", "OmniClassTaxonomy.txt" }
        : new[] { "OmniClassTaxonomy.txt", "Classification_Taxonomy.txt" };
    foreach (var clsName in clsNames)
        foreach (var clsFolder in clsFolders)
        {
            if (clsPath != null) break;
            var clsCandidate = System.IO.Path.Combine(clsFolder, clsName);
            clsTried.Add(clsCandidate);
            if (System.IO.File.Exists(clsCandidate)) clsPath = clsCandidate;
        }
    if (clsPath == null) return;
    string clsAll;
    try
    {
        // THE 2020 TO 2025 FILE IS NOT UTF-8: it holds Windows-1252 bytes (an
        // en dash, an e-cedilla). Strict UTF-8 first; then code page 1252,
        // which .NET 8 and 10 refuse unless a code-page provider is
        // registered; then Latin-1, which every runtime has.
        var clsBytes = System.IO.File.ReadAllBytes(clsPath);
        try { clsAll = new System.Text.UTF8Encoding(false, true).GetString(clsBytes); }
        catch (Exception)
        {
            try { clsAll = System.Text.Encoding.GetEncoding(1252).GetString(clsBytes); }
            catch (Exception) { clsAll = System.Text.Encoding.GetEncoding(28591).GetString(clsBytes); }
        }
    }
    catch (Exception ex)
    {
        clsProblem = "the classification file " + clsPath + " could not be read: " + ex.Message;
        return;
    }
    clsSample = clsAll.IndexOf("sample Classification File", StringComparison.OrdinalIgnoreCase) >= 0;
    foreach (var clsLine in clsAll.Replace(((char)0xFEFF).ToString(), "").Split((char)10))
    {
        var clsTrimmed = clsTidy(clsLine);
        if (clsTrimmed.Length == 0 || clsTrimmed.StartsWith("#")) continue;
        var clsColumns = clsLine.Split((char)9);
        if (clsColumns.Length < 2) continue;
        var clsRowCode = clsTidy(clsColumns[0]);
        if (clsRowCode.Length == 0) continue;
        var clsRaw = clsColumns.Length > 3 ? clsTidy(clsColumns[3]) : "";
        object clsFiled = null;
        long clsNumber;
        if (clsRaw.Length > 0 && long.TryParse(clsRaw, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out clsNumber))
        {
            var clsEnum = Enum.ToObject(typeof(BuiltInCategory), clsNumber);
            if (Enum.IsDefined(typeof(BuiltInCategory), clsEnum)) clsFiled = clsEnum;
        }
        clsRows.Add(Tuple.Create(clsRowCode, clsTidy(clsColumns[1]), clsRaw, clsFiled));
    }
};
// Filed under this category: compared as ElementIds, never as numbers.
Func<Tuple<string, string, string, object>, ElementId, bool> clsFiledUnder = (clsRow, clsCategoryId) =>
    clsRow.Item4 != null && clsCategoryId != null && new ElementId((BuiltInCategory)clsRow.Item4).Equals(clsCategoryId);

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

    // THE OMNICLASS NUMBER AND TITLE (version 2), in Revit's own words, read
    // as text - beside the switches in Family Category and Parameters.
    var clsCodeRow = family == null ? null : family.get_Parameter(clsCode);
    var clsTitleRow = family == null ? null : family.get_Parameter(clsTitle);
    var clsCurrent = clsCodeRow == null ? "" : clsTidy(shown(clsCodeRow));
    if (clsCodeRow != null) settingRows.Add(clsCodeLabel + " " + (clsCurrent.Length == 0 ? "(blank)" : clsCurrent));
    if (clsTitleRow != null)
    {
        var clsTitleNow = clsTidy(shown(clsTitleRow));
        settingRows.Add(clsTitleLabel + " " + (clsTitleNow.Length == 0 ? "(blank)" : clsTitleNow));
    }

    // -----------------------------------------------------------------------
    // THE CODES THE CLASSIFICATION FILE GIVES THIS CATEGORY (version 2) - ONE
    // line, because a list reaches the reply cut to three; and where there are
    // none, why, in words - never an empty answer that reads as "none fit".
    // -----------------------------------------------------------------------
    if (category == null)
        classificationCodes = "This family has no category, so no " + clsCodeLabel + " can be matched to it.";
    else
    {
        clsLoad();
        if (clsPath == null)
            classificationCodes = "No classification file was found, so no " + clsCodeLabel + " can be proposed for "
                + categoryName + " - looked for " + string.Join(", ", clsTried) + ". Revit reads the office's own file "
                + "from the first of those paths.";
        else if (clsProblem != null)
            classificationCodes = "No " + clsCodeLabel + " can be proposed: " + clsProblem + ".";
        else
        {
            var clsMine = clsRows.Where(r => clsFiledUnder(r, category.Id)).ToList();
            if (clsMine.Count > 0)
                classificationCodes = string.Join(" || ", clsMine.Select(r => r.Item1 + " " + r.Item2))
                    + "  (" + clsMine.Count + " code(s) the classification file gives " + categoryName + ", read from "
                    + clsPath + ")";
            else if (clsSample)
                classificationCodes = "The classification file at " + clsPath + " is the SAMPLE Revit 2026 and later "
                    + "ship - a few example codes filed under no category - so out of the box no code is valid for "
                    + categoryName + " or any other category. The office's own classification file goes at that path.";
            else
                classificationCodes = "The classification file " + clsPath + " gives " + categoryName + " no code at all "
                    + "- of its " + clsRows.Count + " code(s), none is filed under that category - so none can be "
                    + "proposed.";
        }
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
    // its id, its kind, its placement number and where it is - and, version 4,
    // its Orientation.
    // -----------------------------------------------------------------------
    //
    // THE ORIENTATION IN REVIT'S PROPERTIES WORDS (version 4) - the mapping
    // SET_ADAPTIVE_POINT_ORIENTATION prints, copied exactly, so the two cards
    // say the same words for the same value. The words are docs/43's, SOURCED
    // and not yet seen on a screen: NEEDS-CHECKING JK5 checks them for both
    // cards at once. Each is named from the enum, never typed as a number.
    var oriShown = new Dictionary<AdaptivePointOrientationType, string>
    {
        { AdaptivePointOrientationType.ToInstance, "Instance (xyz)" },
        { AdaptivePointOrientationType.ToInstanceZthenHost, "Instance (z) then Host (xy)" },
        { AdaptivePointOrientationType.ToHost, "Host (xyz)" },
        { AdaptivePointOrientationType.ToHostAndLoopSystem, "Host and Loop System (xyz)" },
        { AdaptivePointOrientationType.ToGlobalXYZ, "Global (xyz)" },
        { AdaptivePointOrientationType.ToGlobalZthenHost, "Global (z) then Host (xy)" },
    };
    Func<AdaptivePointOrientationType, string> oriWords = o =>
        oriShown.ContainsKey(o) ? oriShown[o] : o.ToString() + " (a value Heron has no Properties words for)";
    // The placement points by the Orientation they read: words -> numbers.
    var oriByWords = new Dictionary<string, List<int>>();
    var oriUnread = new List<int>();
    var oriHandles = 0;
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
        // VERSION 4. Asked of a placement point only - Revit's reference says
        // the call throws for any other - and what Revit returns is printed;
        // when it refuses, its reason is, never a value Heron supposed.
        string orientationWords;
        if (isPlacement)
        {
            try
            {
                var oriNow = oriWords(AdaptiveComponentFamilyUtils.GetPointOrientationType(doc, point.Id));
                if (!oriByWords.ContainsKey(oriNow)) oriByWords[oriNow] = new List<int>();
                oriByWords[oriNow].Add(number);
                orientationWords = "Orientation " + oriNow;
            }
            catch (Exception ex)
            {
                oriUnread.Add(number);
                orientationWords = "Orientation not read - Revit gave none: " + ex.Message.TrimEnd('.');
            }
        }
        else if (isHandle)
        {
            oriHandles++;
            orientationWords = "Orientation not read (shape handle point)";
        }
        else orientationWords = "Orientation not read (not a placement point)";
        pointRows.Add(Tuple.Create(isPlacement ? number : 1000, kind + " " + point.UniqueId + where + ", " + orientationWords));
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

    // -----------------------------------------------------------------------
    // THE LENGTH UNITS (version 3) - how Project Units shows a length in this
    // family, so a build is told BEFORE it starts. Sizes typed in millimetres
    // build right in a template set to Feet and fractional inches, and every
    // dimension and value then shows in feet and inches (2026-10-12, Revit
    // 2020). The units API changed shape at 2021 and the add-in compiles a
    // fragment with no release symbols, so both shapes are reached by
    // reflection, as REPORT_PROJECT_UNITS reads them.
    // -----------------------------------------------------------------------
    var uniLine = "";
    var uniMetric = true;
    var uniName = "";
    try
    {
        var uniStatics = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
        var uniApi = typeof(Units).Assembly;
        var uniSpace = typeof(Units).Namespace + ".";
        var uniUnits = doc.GetUnits();
        var uniForge = uniApi.GetType(uniSpace + "ForgeTypeId");
        var uniSpecs = uniApi.GetType(uniSpace + "SpecTypeId");
        FormatOptions uniFormat;
        var uniSymbol = "";
        var uniId = "";
        if (uniForge != null && uniSpecs != null)
        {
            var uniOne = new[] { uniForge };
            var uniLength = uniSpecs.GetProperty("Length", uniStatics).GetValue(null, null);
            uniFormat = (FormatOptions)typeof(Units).GetMethod("GetFormatOptions", uniOne).Invoke(uniUnits, new[] { uniLength });
            var uniUnit = typeof(FormatOptions).GetMethod("GetUnitTypeId", Type.EmptyTypes).Invoke(uniFormat, null);
            uniId = (uniForge.GetProperty("TypeId").GetValue(uniUnit, null) as string) ?? "";
            uniName = (typeof(LabelUtils).GetMethod("GetLabelForUnit", uniStatics, null, uniOne, null)
                .Invoke(null, new[] { uniUnit }) as string) ?? "";
            var uniSym = typeof(FormatOptions).GetMethod("GetSymbolTypeId", Type.EmptyTypes).Invoke(uniFormat, null);
            if (uniSym != null && !(bool)uniForge.GetMethod("Empty", Type.EmptyTypes).Invoke(uniSym, null))
                uniSymbol = (typeof(LabelUtils).GetMethod("GetLabelForSymbol", uniStatics, null, uniOne, null)
                    .Invoke(null, new[] { uniSym }) as string) ?? "";
        }
        else
        {
            var uniType = uniApi.GetType(uniSpace + "UnitType");
            var uniDisplay = uniApi.GetType(uniSpace + "DisplayUnitType");
            var uniSymType = uniApi.GetType(uniSpace + "UnitSymbolType");
            uniFormat = (FormatOptions)typeof(Units).GetMethod("GetFormatOptions", new[] { uniType })
                .Invoke(uniUnits, new[] { Enum.Parse(uniType, "UT_Length") });
            var uniUnit = typeof(FormatOptions).GetProperty("DisplayUnits").GetValue(uniFormat, null);
            uniId = uniUnit.ToString();
            uniName = (typeof(LabelUtils).GetMethod("GetLabelFor", uniStatics, null, new[] { uniDisplay }, null)
                .Invoke(null, new[] { uniUnit }) as string) ?? "";
            var uniSym = typeof(FormatOptions).GetProperty("UnitSymbol").GetValue(uniFormat, null);
            if (uniSym != null && uniSym.ToString() != "UST_NONE")
                uniSymbol = (typeof(LabelUtils).GetMethod("GetLabelFor", uniStatics, null, new[] { uniSymType }, null)
                    .Invoke(null, new[] { uniSym }) as string) ?? "";
        }
        // METRIC IS READ FROM REVIT'S OWN ID FOR THE UNIT, never from its label,
        // which is translated: every metric length unit's id names a metre -
        // "millimeters-1.0.1", DUT_METERS_CENTIMETERS - and no imperial one does.
        uniMetric = uniId.IndexOf("meter", StringComparison.OrdinalIgnoreCase) >= 0;
        uniLine = "units: Length = " + uniName + (uniSymbol.Length > 0 ? " (" + uniSymbol + ")" : " (no symbol)")
            + ", rounding " + uniFormat.Accuracy.ToString("0.############", invariant)
            + (uniMetric ? " - metric" : " - IMPERIAL");
    }
    catch (Exception failure)
    {
        uniLine = "units: Length could not be read ("
            + (failure.InnerException ?? failure).Message.TrimEnd('.') + ") - REPORT_PROJECT_UNITS reads them";
    }

    var parts = new List<string>
    {
        "Family \"" + (family == null ? doc.Title : family.Name) + "\"",
        "category " + categoryName,
        "kind: " + familyKind,
        uniLine,
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
    if (!uniMetric)
        findings.Add("Length shows in " + uniName + ", not metric. Sizes typed in millimetres still build right, but "
            + "every dimension and value in this family will show in " + uniName + ". Say so before building: "
            + "SET_PROJECT_UNITS \"Length=Millimeters, 0 decimal places\" changes it (the Admin switch), or the "
            + "family is started again from a Metric template.");
    if (types == 0)
        findings.Add("The family has no type yet. A formula, and any value, needs one - SET_FAMILY_TYPE_VALUES "
            + "makes the first.");
    if (clsCodeRow != null)
        findings.Add(clsCodeLabel + " reads " + (clsCurrent.Length == 0 ? "(blank)" : clsCurrent) + ". The codes the "
            + "classification file gives " + categoryName + " are in classificationCodes; SET_FAMILY_SETTINGS \""
            + clsCodeLabel + "=<code>\" sets one of them.");
    // VERSION 4. The placement points' Orientation together, as Properties
    // shows it on each point - only where there are adaptive points, so a
    // family without them is told nothing new.
    if (oriByWords.Count > 0 || oriUnread.Count > 0 || oriHandles > 0)
    {
        var oriParts = oriByWords.OrderBy(w => w.Value.Min()).Select(w => "point(s) " + string.Join(", ",
            w.Value.OrderBy(n => n).Select(n => n.ToString(invariant))) + " " + w.Key).ToList();
        if (oriUnread.Count > 0)
            oriParts.Add("point(s) " + string.Join(", ", oriUnread.OrderBy(n => n).Select(n => n.ToString(invariant)))
                + " not read - Revit gave no Orientation, its reason in pointList");
        if (oriHandles > 0)
            oriParts.Add(oriHandles + " shape handle point(s) not read - Revit's API gives an Orientation to placement "
                + "points only");
        findings.Add("Orientation of the adaptive points, as Properties shows it on each: " + string.Join("; ", oriParts)
            + (oriByWords.Count > 0 ? ". SET_ADAPTIVE_POINT_ORIENTATION changes a placement point's." : "."));
    }
}
