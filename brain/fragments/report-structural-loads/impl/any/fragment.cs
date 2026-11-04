// NOT STANDALONE. Assumes `doc`, `elements`, `show` and `loadCase` are in
// scope, and leaves `loadCount`, `caseCount`, `combinationCount`,
// `boundaryCount`, `report`, `names`, `noAnalytical` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Revit's Analyze > Loads and Boundary Conditions, and Analyze >
// Load Cases: the load natures, the load cases (name, number, nature,
// category, how many loads each holds), every point, line and area load with
// its values, load case and host, and every boundary condition with its host
// and fixity - and, from Analyze > Load Combinations, every load combination
// with its state, type, factored load cases and usages (2026-11-02). Every
// list, or only the ones `show` names (cases; natures; combinations; loads;
// boundary - several separated by ; or ,). The loads narrowed to one
// load case by `loadCase`; loads and boundary conditions narrowed to the
// elements handed in, through each one's analytical element.
//
// UNITS. Revit keeps forces in its own internal units. Each value is read
// through UnitUtils.ConvertFromInternalUnits - DisplayUnitType in 2020 (#if
// REVIT2020), UnitTypeId from 2021 - as kN, kN/m, kN/m2 (kPa) and kN-m; points
// are feet x 304.8, as plain arithmetic.
//
// THE HOST. A hosted load's HostElementId is an ANALYTICAL element. 2020 to
// 2022 it is the physical element's AnalyticalModel, whose GetElementId() is
// the beam, column, wall or floor; from 2023 the analytical model is separate
// elements, and AnalyticalToPhysicalAssociationManager.GetAssociatedElementId
// gives the physical element (REPORT_ANALYTICAL_MODEL reads the same
// association). Each load names both.
//
// A SELECTED ELEMENT WITH NO ANALYTICAL ELEMENT is named in noAnalytical - no
// load can be on it - never read as "it carries no load".
//
// RELEASES (tools/api-surface --members, 2026-11-01). LoadNature, LoadCase
// (Number, NatureId, SubcategoryId), LoadBase (HostElementId, IsHosted,
// IsReaction, LoadCaseId, LoadCaseName, OrientTo), PointLoad (Point,
// ForceVector, MomentVector), LineLoad (StartPoint, EndPoint, ForceVector1,
// ForceVector2, MomentVector1, IsUniform), AreaLoad (Area, ForceVector1-3,
// NumRefPoints) and BoundaryConditions (HostElementId, Point,
// GetBoundaryConditionsType, GetCurve) carry the same signatures 2020 to 2027,
// and so do LoadCombination (State, Type, GetComponents, GetUsageIds),
// LoadComponent (LoadCaseOrCombinationId, Factor) and LoadUsage (2026-11-02).
// AnalyticalModel and Element.GetAnalyticalModel are 2020-2022 only;
// AnalyticalToPhysicalAssociationManager 2023-2027 - hence the one branch.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var loadCount = 0;
var caseCount = 0;
var combinationCount = 0;
var boundaryCount = 0;
var report = "";
var names = new List<string>();
var noAnalytical = new List<ElementId>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> num = v => Math.Round(v, 3).ToString("0.###", invariant);
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<XYZ, string> at = p => p == null ? "?" : "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";

// ---- Units, the release's own way -----------------------------------------
Func<double, double> kN = v => v;
Func<double, double> kNm = v => v;
Func<double, double> kNperM = v => v;
Func<double, double> kNmPerM = v => v;
Func<double, double> kPa = v => v;
Func<double, double> m2 = v => v;
#if REVIT2020
kN = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILONEWTONS);
kNm = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILONEWTON_METERS);
kNperM = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILONEWTONS_PER_METER);
kNmPerM = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILONEWTON_METERS_PER_METER);
kPa = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILONEWTONS_PER_SQUARE_METER);
m2 = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_SQUARE_METERS);
#else
kN = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.Kilonewtons);
kNm = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.KilonewtonMeters);
kNperM = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.KilonewtonsPerMeter);
kNmPerM = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.KilonewtonMetersPerMeter);
kPa = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.KilonewtonsPerSquareMeter);
m2 = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.SquareMeters);
#endif
Func<XYZ, Func<double, double>, string, string> vec = (v, f, unit) => v == null ? "?"
    : "(" + num(f(v.X)) + ", " + num(f(v.Y)) + ", " + num(f(v.Z)) + ") " + unit;
Func<XYZ, bool> isZero = v => v == null || v.IsZeroLength();

// ---- Which lists -----------------------------------------------------------
var asked = (show ?? "").Trim().ToLowerInvariant();
var every = asked.Length == 0 || asked == "all";
bool wantNatures = every, wantCases = every, wantLoads = every, wantBoundary = every, wantCombinations = every;
var unknownWords = new List<string>();
if (!every)
{
    foreach (var raw in asked.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var w = raw.Trim().Replace(" ", "").Replace("-", "");
        if (w.Length == 0) continue;
        if (w == "all") { wantNatures = wantCases = wantLoads = wantBoundary = wantCombinations = true; }
        else if (w.StartsWith("nature")) wantNatures = true;
        else if (w.StartsWith("case") || w.StartsWith("loadcase")) wantCases = true;
        // Before "load", which "loadcombinations" also begins with.
        else if (w.StartsWith("comb") || w.StartsWith("loadcomb")) wantCombinations = true;
        else if (w.StartsWith("load")) wantLoads = true;
        else if (w.StartsWith("bound") || w.StartsWith("support")) wantBoundary = true;
        else unknownWords.Add(raw.Trim());
    }
}
if (unknownWords.Count > 0)
    findings.Add("\"" + string.Join("\", \"", unknownWords) + "\" is not one of the lists - cases, natures, "
        + "combinations, loads or boundary. Nothing was guessed for it.");

// ---- Analytical element <-> physical element -------------------------------
#if REVIT2020 || REVIT2021 || REVIT2022
Func<ElementId, ElementId> physicalOf = id =>
{
    var am = id == null ? null : doc.GetElement(id) as AnalyticalModel;
    if (am == null) return ElementId.InvalidElementId;
    try { return am.GetElementId(); } catch { return ElementId.InvalidElementId; }
};
Func<Element, ElementId> analyticalOf = e =>
{
    if (e is AnalyticalModel) return e.Id;
    AnalyticalModel am = null;
    try { am = e.GetAnalyticalModel(); } catch { }
    return am == null ? ElementId.InvalidElementId : am.Id;
};
#else
AnalyticalToPhysicalAssociationManager manager = null;
try { manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc); } catch { }
Func<ElementId, ElementId> physicalOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId || manager == null) return ElementId.InvalidElementId;
    try { return manager.GetAssociatedElementId(id) ?? ElementId.InvalidElementId; }
    catch { return ElementId.InvalidElementId; }
};
Func<Element, ElementId> analyticalOf = e =>
{
    if (e is AnalyticalElement) return e.Id;
    if (manager == null) return ElementId.InvalidElementId;
    try { return manager.GetAssociatedElementId(e.Id) ?? ElementId.InvalidElementId; }
    catch { return ElementId.InvalidElementId; }
};
#endif

Func<ElementId, string> describe = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "nothing";
    var e = doc.GetElement(id);
    if (e == null) return "element " + id;
    var cat = e.Category == null ? "" : e.Category.Name + " ";
    return cat + "\"" + e.Name + "\" (" + id + ")";
};
Func<ElementId, string> hostText = hostId =>
{
    if (hostId == null || hostId == ElementId.InvalidElementId) return "free - on no element";
    var phys = physicalOf(hostId);
    return phys == ElementId.InvalidElementId
        ? "on analytical " + describe(hostId) + ", tied to no physical element"
        : "on " + describe(phys) + " through analytical " + hostId;
};

// ---- The elements handed in, as analytical ids -----------------------------
var selected = (elements ?? new List<Element>()).Where(e => e != null).ToList();
var narrowed = selected.Count > 0;
var hosts = new HashSet<ElementId>();
foreach (var e in selected)
{
    var aid = analyticalOf(e);
    if (aid == ElementId.InvalidElementId) noAnalytical.Add(e.Id);
    else hosts.Add(aid);
}
if (noAnalytical.Count > 0)
    findings.Add(noAnalytical.Count + " of the " + selected.Count + " element(s) handed in have no analytical element, "
        + "so no load or boundary condition can be on them - not the same as carrying none: "
        + string.Join(", ", noAnalytical.Select(describe)) + ".");

var lines = new List<string>();

// ---- Load natures ----------------------------------------------------------
if (wantNatures)
{
    var natures = new FilteredElementCollector(doc).OfClass(typeof(LoadNature)).Cast<LoadNature>()
        .OrderBy(n => n.Name).ToList();
    lines.Add("Load natures (" + natures.Count + "): "
        + (natures.Count == 0 ? "none" : string.Join(", ", natures.Select(n => "\"" + n.Name + "\""))));
}

// ---- Load cases ------------------------------------------------------------
var cases = new FilteredElementCollector(doc).OfClass(typeof(LoadCase)).Cast<LoadCase>()
    .OrderBy(c => c.Number).ThenBy(c => c.Name).ToList();
var wantCaseName = (loadCase ?? "").Trim();
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
if (string.Equals(wantCaseName, "none", StringComparison.OrdinalIgnoreCase)) wantCaseName = "";
LoadCase onlyCase = null;
if (wantCaseName.Length > 0)
{
    onlyCase = cases.FirstOrDefault(c => c.Name == wantCaseName);
    if (onlyCase == null)
    {
        var near = cases.FirstOrDefault(c => string.Equals(c.Name, wantCaseName, StringComparison.OrdinalIgnoreCase));
        findings.Add(near != null
            ? "No load case is called exactly \"" + wantCaseName + "\"; \"" + near.Name + "\" differs only in capitals "
              + "and was NOT taken. Nothing was narrowed."
            : "No load case is called \"" + wantCaseName + "\". The load cases in this model: "
              + (cases.Count == 0 ? "none" : string.Join(", ", cases.Select(c => "\"" + c.Name + "\""))) + ".");
        wantLoads = false;
    }
}

// Every load in the model, once - counted per case below.
var allLoads = new List<LoadBase>();
allLoads.AddRange(new FilteredElementCollector(doc).OfClass(typeof(PointLoad)).Cast<LoadBase>());
allLoads.AddRange(new FilteredElementCollector(doc).OfClass(typeof(LineLoad)).Cast<LoadBase>());
allLoads.AddRange(new FilteredElementCollector(doc).OfClass(typeof(AreaLoad)).Cast<LoadBase>());

foreach (var c in cases) names.Add("\"" + c.Name + "\"");
caseCount = cases.Count;
if (wantCases)
{
    lines.Add("Load cases (" + cases.Count + "):" + (cases.Count == 0 ? " none" : ""));
    foreach (var c in cases)
    {
        string nature = "?", category = "?";
        try { var n = doc.GetElement(c.NatureId); nature = n == null ? "none" : n.Name; } catch { }
        try
        {
            var p = c.get_Parameter(BuiltInParameter.LOAD_CASE_SUBCATEGORY);
            category = p == null ? "?" : (p.AsValueString() ?? "?");
        }
        catch { }
        var held = allLoads.Count(l => l.LoadCaseId == c.Id);
        lines.Add("  " + c.Number + "  \"" + c.Name + "\" - nature " + nature + ", category " + category
            + ", " + held + " load(s)");
    }
}

// ---- Load combinations -----------------------------------------------------
// Counted always, like the cases; listed when asked. Each with its state,
// type, factored load cases (or combinations) and usages, as Analyze > Load
// Combinations shows them.
var combinationsHad = new FilteredElementCollector(doc).OfClass(typeof(LoadCombination)).Cast<LoadCombination>()
    .OrderBy(c => c.Name).ToList();
combinationCount = combinationsHad.Count;
if (wantCombinations)
{
    Func<ElementId, string> nameOf = id =>
    {
        if (id == null || id == ElementId.InvalidElementId) return "?";
        var e = doc.GetElement(id);
        return e == null ? "?" : e.Name;
    };
    lines.Add("Load combinations (" + combinationsHad.Count + "):" + (combinationsHad.Count == 0 ? " none" : ""));
    foreach (var c in combinationsHad)
    {
        var parts = new List<string>();
        try
        {
            foreach (var comp in c.GetComponents())
                parts.Add(num(comp.Factor) + " x \"" + nameOf(comp.LoadCaseOrCombinationId) + "\"");
        }
        catch { parts.Add("components unreadable"); }
        var usages = new List<string>();
        try { foreach (var id in c.GetUsageIds()) usages.Add("\"" + nameOf(id) + "\""); } catch { }
        string st = "?", ty = "?";
        try { st = c.State.ToString(); } catch { }
        try { ty = c.Type.ToString(); } catch { }
        lines.Add("  \"" + c.Name + "\" - " + st + ", " + ty + ": "
            + (parts.Count == 0 ? "no components" : string.Join(" + ", parts))
            + (usages.Count == 0 ? "" : "; usage " + string.Join(", ", usages)));
    }
    var usagesHad = new FilteredElementCollector(doc).OfClass(typeof(LoadUsage)).Cast<LoadUsage>()
        .OrderBy(u => u.Name).ToList();
    lines.Add("Load usages (" + usagesHad.Count + "): "
        + (usagesHad.Count == 0 ? "none" : string.Join(", ", usagesHad.Select(u => "\"" + u.Name + "\""))));
}

// ---- Loads -----------------------------------------------------------------
if (wantLoads)
{
    var shown = allLoads
        .Where(l => onlyCase == null || l.LoadCaseId == onlyCase.Id)
        .Where(l => !narrowed || hosts.Contains(l.HostElementId))
        .ToList();
    loadCount = shown.Count;
    var heading = "Loads (" + shown.Count + ")";
    if (onlyCase != null) heading += " in load case \"" + onlyCase.Name + "\"";
    if (narrowed) heading += " on the " + selected.Count + " element(s) handed in";
    lines.Add(heading + ":" + (shown.Count == 0 ? " none" : ""));
    foreach (var l in shown)
    {
        string what;
        if (l is PointLoad)
        {
            var p = (PointLoad)l;
            what = "Point load at " + at(p.Point) + ": force " + vec(p.ForceVector, kN, "kN")
                + (isZero(p.MomentVector) ? "" : ", moment " + vec(p.MomentVector, kNm, "kN-m"));
        }
        else if (l is LineLoad)
        {
            var p = (LineLoad)l;
            what = "Line load " + at(p.StartPoint) + " to " + at(p.EndPoint) + ": "
                + (p.IsUniform ? "uniform " + vec(p.ForceVector1, kNperM, "kN/m")
                               : "from " + vec(p.ForceVector1, kNperM, "kN/m") + " to " + vec(p.ForceVector2, kNperM, "kN/m"))
                + (isZero(p.MomentVector1) ? "" : ", moment " + vec(p.MomentVector1, kNmPerM, "kN-m/m"));
        }
        else
        {
            var p = (AreaLoad)l;
            var refs = p.NumRefPoints;
            what = "Area load of " + num(m2(p.Area)) + " m2: " + vec(p.ForceVector1, kPa, "kPa")
                + (refs > 1 ? ", " + vec(p.ForceVector2, kPa, "kPa") : "")
                + (refs > 2 ? ", " + vec(p.ForceVector3, kPa, "kPa") : "")
                + (refs > 1 ? " at its " + refs + " reference points" : "");
        }
        var caseName = "";
        try { caseName = l.LoadCaseName; } catch { }
        lines.Add("  " + l.Id + "  " + what + "; load case \"" + caseName + "\"; " + hostText(l.HostElementId)
            + "; oriented to " + l.OrientTo + (l.IsReaction ? "; a REACTION" : ""));
    }
}

// ---- Boundary conditions ---------------------------------------------------
if (wantBoundary)
{
    var bcs = new FilteredElementCollector(doc).OfClass(typeof(BoundaryConditions)).Cast<BoundaryConditions>()
        .Where(b => !narrowed || hosts.Contains(b.HostElementId))
        .ToList();
    boundaryCount = bcs.Count;
    lines.Add("Boundary conditions (" + bcs.Count + ")" + (narrowed ? " on the element(s) handed in" : "") + ":"
        + (bcs.Count == 0 ? " none" : ""));
    Func<BoundaryConditions, BuiltInParameter, string> val = (b, bip) =>
    {
        try { var p = b.get_Parameter(bip); return p == null ? null : p.AsValueString(); } catch { return null; }
    };
    foreach (var b in bcs)
    {
        var kind = BoundaryConditionsType.Point;
        try { kind = b.GetBoundaryConditionsType(); } catch { }
        string where = "";
        try
        {
            if (kind == BoundaryConditionsType.Point) where = " at " + at(b.Point);
            else if (kind == BoundaryConditionsType.Line)
            {
                var c = b.GetCurve();
                if (c != null) where = " from " + at(c.GetEndPoint(0)) + " to " + at(c.GetEndPoint(1));
            }
        }
        catch { }
        var preset = val(b, kind == BoundaryConditionsType.Area ? BuiltInParameter.BOUNDARY_PARAM_PRESET_AREA
            : kind == BoundaryConditionsType.Line ? BuiltInParameter.BOUNDARY_PARAM_PRESET_LINEAR
            : BuiltInParameter.BOUNDARY_PARAM_PRESET);
        var dof = new List<string>();
        Action<string, BuiltInParameter> add = (label, bip) => { var v = val(b, bip); if (!string.IsNullOrEmpty(v)) dof.Add(label + " " + v); };
        add("X", BuiltInParameter.BOUNDARY_DIRECTION_X);
        add("Y", BuiltInParameter.BOUNDARY_DIRECTION_Y);
        add("Z", BuiltInParameter.BOUNDARY_DIRECTION_Z);
        add("rot X", BuiltInParameter.BOUNDARY_DIRECTION_ROT_X);
        add("rot Y", BuiltInParameter.BOUNDARY_DIRECTION_ROT_Y);
        add("rot Z", BuiltInParameter.BOUNDARY_DIRECTION_ROT_Z);
        lines.Add("  " + b.Id + "  " + kind + where + ": " + (string.IsNullOrEmpty(preset) ? "" : preset)
            + (dof.Count == 0 ? "" : " (" + string.Join(", ", dof) + ")") + "; " + hostText(b.HostElementId));
    }
}

report = string.Join("\n", lines);
if (narrowed && wantLoads && loadCount == 0 && hosts.Count > 0)
    findings.Add("No load is hosted on the analytical element(s) of what was handed in"
        + (onlyCase != null ? " in load case \"" + onlyCase.Name + "\"" : "")
        + ". A free load drawn over an element is on no element, so it is not counted here.");
findings.Add("Read only - nothing in the model changed.");
