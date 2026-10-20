// NOT STANDALONE. Assumes `doc` is in scope; leaves `barTypes`, `hookTypes`,
// `coverTypes`, `shapes`, `systemTypes`, `listing` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT A STRUCTURAL SPEC'S WORDS CAN BE MATCHED TO. Every placing rebar tool
// takes a bar type by its exact name or a diameter in millimetres, a hook and
// a cover by name - and refuses anything else naming what this read lists.
//
// TWO DIAMETERS FROM 2022. RebarBarType.BarDiameter exists 2020-2022 and is
// obsolete at 2022; BarNominalDiameter and BarModelDiameter arrive at 2022.
// Read by #if, the release symbols the add-in hands every fragment.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);
Func<RebarBarType, double> nominalOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarNominalDiameter;
#endif
};
Func<RebarBarType, double> modelOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarModelDiameter;
#endif
};
Func<RebarStyle, string> styleWord = s => s == RebarStyle.StirrupTie ? "stirrup/tie" : "standard";

var findings = new List<string>();
var sections = new List<string>();

// ---- bar types ---------------------------------------------------------------
var bars = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>()
    .OrderBy(t => nominalOf(t)).ThenBy(t => t.Name, StringComparer.Ordinal).ToList();
var barTypes = bars.Count;
var barWords = new List<string>();
foreach (var t in bars)
{
    var text = "'" + t.Name + "' " + mm(nominalOf(t)) + " mm";
    if (Math.Abs(modelOf(t) - nominalOf(t)) > 1e-9) text += " (model " + mm(modelOf(t)) + " mm)";
    text += ", bend diameters: standard " + mm(t.StandardBendDiameter) + ", hook "
        + mm(t.StandardHookBendDiameter) + ", stirrup/tie " + mm(t.StirrupTieBendDiameter) + " mm";
    barWords.Add(text);
}
sections.Add("Rebar Bar Types (" + barTypes + "): " + (barTypes == 0 ? "none" : string.Join("; ", barWords)));

// ---- hook types --------------------------------------------------------------
var hooks = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>()
    .OrderBy(h => h.Name, StringComparer.Ordinal).ToList();
var hookTypes = hooks.Count;
sections.Add("Rebar Hook Types (" + hookTypes + "): " + (hookTypes == 0 ? "none" : string.Join("; ",
    hooks.Select(h => "'" + h.Name + "' " + Math.Round(h.HookAngle * 180.0 / Math.PI, 1).ToString("0.#", invariant)
        + " deg, extension " + h.StraightLineMultiplier.ToString("0.##", invariant) + " x bar diameter, for "
        + styleWord(h.Style) + " bars"))));

// ---- cover types -------------------------------------------------------------
var covers = new FilteredElementCollector(doc).OfClass(typeof(RebarCoverType)).Cast<RebarCoverType>()
    .OrderBy(c => c.CoverDistance).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();
var coverTypes = covers.Count;
sections.Add("Rebar Cover Types (" + coverTypes + "): " + (coverTypes == 0 ? "none" : string.Join("; ",
    covers.Select(c => "'" + c.Name + "' " + mm(c.CoverDistance) + " mm"))));

// ---- shapes ------------------------------------------------------------------
var shapeList = new FilteredElementCollector(doc).OfClass(typeof(RebarShape)).Cast<RebarShape>()
    .OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
var shapes = shapeList.Count;
var standardShapes = shapeList.Where(s => s.RebarStyle == RebarStyle.Standard).Select(s => s.Name).ToList();
var tieShapes = shapeList.Where(s => s.RebarStyle == RebarStyle.StirrupTie).Select(s => s.Name).ToList();
sections.Add("Rebar Shapes (" + shapes + "): " + (shapes == 0 ? "none" :
    standardShapes.Count + " standard [" + string.Join(", ", standardShapes) + "], "
    + tieShapes.Count + " stirrup/tie [" + string.Join(", ", tieShapes) + "]"));

// ---- reinforcement system types ----------------------------------------------
Func<Type, List<string>> namesOf = kind => new FilteredElementCollector(doc).OfClass(kind)
    .Cast<Element>().Select(e => e.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
var areaNames = namesOf(typeof(AreaReinforcementType));
var pathNames = namesOf(typeof(PathReinforcementType));
var fabricAreaNames = namesOf(typeof(FabricAreaType));
var sheets = new FilteredElementCollector(doc).OfClass(typeof(FabricSheetType)).Cast<FabricSheetType>()
    .OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
var systemTypes = areaNames.Count + pathNames.Count + fabricAreaNames.Count + sheets.Count;
Func<List<string>, string> quoted = names => names.Count == 0 ? "none" : string.Join(", ", names.Select(n => "'" + n + "'"));
sections.Add("Area Reinforcement Types (" + areaNames.Count + "): " + quoted(areaNames));
sections.Add("Path Reinforcement Types (" + pathNames.Count + "): " + quoted(pathNames));
sections.Add("Fabric Area Types (" + fabricAreaNames.Count + "): " + quoted(fabricAreaNames));
var sheetWords = new List<string>();
foreach (var s in sheets)
{
    string major = "", minor = "";
    try { major = mm(s.MajorSpacing); } catch (Exception) { }
    try { minor = mm(s.MinorSpacing); } catch (Exception) { }
    sheetWords.Add("'" + s.Name + "'" + (major.Length > 0 ? " wires at " + major + " x " + minor + " mm" : ""));
}
sections.Add("Fabric Sheet Types (" + sheets.Count + "): " + (sheets.Count == 0 ? "none" : string.Join("; ", sheetWords)));

// ---- the setting that decides how a new bar finds its shape -------------------
try
{
    var settings = ReinforcementSettings.GetReinforcementSettings(doc);
    if (settings != null)
        sections.Add("Reinforcement Settings: rebar shape defines hooks " + (settings.RebarShapeDefinesHooks ? "yes" : "no"));
}
catch (Exception ex) { findings.Add("The reinforcement settings could not be read: " + ex.Message); }

var listing = string.Join(" | ", sections);

findings.Add(barTypes + " bar type(s), " + hookTypes + " hook type(s), " + coverTypes + " cover type(s), "
    + shapes + " rebar shape(s) and " + systemTypes + " area/path/fabric type(s) in " + doc.Title + ".");
if (barTypes == 0)
    findings.Add("No Rebar Bar Type is loaded, so no bar can be placed. CREATE_REBAR_BAR_TYPE makes one from the "
        + "spec's diameter and bend diameters, or load them from a structural template.");
if (shapes == 0)
    findings.Add("No Rebar Shape is loaded. Revit makes a new bar's shape from the shapes already in the model, so "
        + "placing bars will fail until the rebar shape families are loaded (a structural template has them).");
