// NOT STANDALONE. Assumes `doc` is in scope; leaves `energyModel`,
// `analyticalSpaces`, `analyticalSurfaces`, `analyticalOpenings`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// ANALYZE > CREATE ENERGY MODEL, FROM THE MODEL'S OWN ENERGY SETTINGS, WHICH
// ARE READ AND NEVER CHANGED (SET_ENERGY_SETTINGS changes them).
// EnergyAnalysisDetailModel.Create builds the energy analytical model - the
// analytical spaces, surfaces and openings an energy analysis, a gbXML export
// or a loads run works from - and it is read back afterwards.
//
// PER RELEASE (tools/api-surface --members, 2026-11-02):
//   2027       Create(Document) - "the Mode set in Energy Settings will be
//              used"; Create(Document, options) is obsolete there.
//   2022-2026  Create(Document, options) with EnergyModelType.AnalysisMode,
//              which follows the Energy Settings' analysis mode.
//   2020-2021  Create(Document, options) with EnergyModelType.BuildingElement
//              - there is no AnalysisMode choice on these releases, so only a
//              model whose Energy Settings' mode is "building elements" is
//              built; any other mode is refused by name, never mapped onto one.
// The options' shading surfaces, mullions and simplified curtain systems are
// taken from the Energy Settings' own export complexity
// (IsExportShadingSurfacesEnabled, IsExportMullionsEnabled,
// IsExportSimplifiedCurtainSystemsEnabled), and the tier is Final - the
// complete model with its openings.
//
// AN ENERGY MODEL ALREADY THERE: named before the run. Whether Create replaces
// it or adds a second has NOT BEEN SEEN IN REVIT; the answer says which
// happened by asking Revit for its main model afterwards. Nothing is deleted
// here.
//
// REVIT'S REFUSAL - no rooms or spaces to build from, nothing bounded, a mode
// the model cannot be built in - is passed on in Revit's own words, and the
// call throws so the caller's transaction rolls back and nothing is kept.
//
// An ElementId is never read as a number.

var findings = new List<string>();
string refused = null;
string report = null;
var energyModel = ElementId.InvalidElementId;
var analyticalSpaces = 0;
var analyticalSurfaces = 0;
var analyticalOpenings = 0;

Func<object, string> words = e => System.Text.RegularExpressions.Regex.Replace(e.ToString(), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ").ToLowerInvariant();

EnergyDataSettings es = null;
if (doc.IsFamilyDocument)
    refused = "The document in front is a family. An energy model is built from a project, so NOTHING WAS MADE.";
else
{
    try
    {
#if REVIT2026_OR_GREATER
        es = EnergyDataSettings.GetEnergyDataSettings(doc);
#else
        es = EnergyDataSettings.GetFromDocument(doc);
#endif
    }
    catch (Exception ex) { refused = "Revit could not read this model's Energy Settings (" + ex.Message + "), so NOTHING WAS MADE."; }
    if (refused == null && es == null)
        refused = "This model has no Energy Settings to build an energy model from. Open Analyze > Energy Settings, then "
            + "ask again. NOTHING WAS MADE.";
}

string mode = "";
if (refused == null)
{
    try { mode = words(es.AnalysisType); } catch (Exception) { mode = "unknown"; }
#if REVIT2020 || REVIT2021
    if (es.AnalysisType != AnalysisMode.BuildingElements)
        refused = "This model's Energy Settings mode is \"" + mode + "\". On this Revit the API builds an energy model "
            + "from building elements only, so it is refused rather than built another way. Set the analysis mode to "
            + "building elements (SET_ENERGY_SETTINGS), or use Analyze > Create Energy Model. NOTHING WAS MADE.";
#endif
}

if (refused == null)
{
    EnergyAnalysisDetailModel old = null;
    try { old = EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel(doc); } catch (Exception) { old = null; }
    var oldId = old != null ? old.Id : ElementId.InvalidElementId;

    EnergyAnalysisDetailModel made;
    try
    {
#if REVIT2027_OR_GREATER
        made = EnergyAnalysisDetailModel.Create(doc);
#else
        var options = new EnergyAnalysisDetailModelOptions();
        options.Tier = EnergyAnalysisDetailModelTier.Final;
#if REVIT2020 || REVIT2021
        options.EnergyModelType = EnergyModelType.BuildingElement;
#else
        options.EnergyModelType = EnergyModelType.AnalysisMode;
#endif
        options.IncludeShadingSurfaces = es.IsExportShadingSurfacesEnabled;
        options.ExportMullions = es.IsExportMullionsEnabled;
        options.SimplifyCurtainSystems = es.IsExportSimplifiedCurtainSystemsEnabled;
        made = EnergyAnalysisDetailModel.Create(doc, options);
#endif
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to create the energy model: " + ex.Message + " - Revit's own "
            + "words, with Energy Settings mode \"" + mode + "\". It is usually the Energy Settings or the rooms or "
            + "spaces it builds from; Heron changes neither. NOTHING from this call was kept.");
    }
    if (made == null)
        throw new InvalidOperationException("Revit made no energy model and gave no reason. NOTHING from this call was kept.");

    energyModel = made.Id;
    try { analyticalSpaces = made.GetAnalyticalSpaces().Count; } catch (Exception) { analyticalSpaces = 0; }
    try { analyticalSurfaces = made.GetAnalyticalSurfaces().Count; } catch (Exception) { analyticalSurfaces = 0; }
    try { analyticalOpenings = made.GetAnalyticalOpenings().Count; } catch (Exception) { analyticalOpenings = 0; }

    EnergyAnalysisDetailModel mainAfter = null;
    try { mainAfter = EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel(doc); } catch (Exception) { mainAfter = null; }
    var isMain = mainAfter != null && mainAfter.Id == made.Id;
    var oldStill = oldId != ElementId.InvalidElementId && oldId != made.Id && doc.GetElement(oldId) != null;

    if (analyticalSpaces == 0)
        findings.Add("The energy model has NO analytical spaces. Revit built it but found nothing enclosed to analyse - "
            + "with mode \"" + mode + "\" check that the rooms or spaces are placed and bounded, or that the building "
            + "elements enclose a volume.");
    if (oldId != ElementId.InvalidElementId)
        findings.Add(oldId == made.Id
            ? "The energy model that was there (id " + oldId + ") was rebuilt in place."
            : oldStill
                ? "The energy model that was there (id " + oldId + ") IS STILL IN THE MODEL beside the new one - Revit "
                  + "added a second. Nothing was deleted here."
                : "The energy model that was there (id " + oldId + ") is gone - Revit replaced it.");
    if (!isMain)
        findings.Add("Revit does not name the new energy model as the model's main one"
            + (mainAfter == null ? " - it names none." : " - it names id " + mainAfter.Id + "."));

    report = "Energy model created (id " + made.Id + ", " + words(made.Tier) + ") from this model's Energy Settings, "
        + "unchanged - mode \"" + mode + "\": " + analyticalSpaces + " analytical space(s), " + analyticalSurfaces
        + " surface(s), " + analyticalOpenings + " opening(s)."
        + (oldId == ElementId.InvalidElementId ? " There was no energy model before." : "")
        + " Analyze > Energy Settings is where it is set up.";
    findings.Add(report);
}
if (refused != null) findings.Add(refused);
