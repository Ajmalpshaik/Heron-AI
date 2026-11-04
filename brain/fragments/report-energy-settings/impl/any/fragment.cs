// NOT STANDALONE. Assumes `doc` is in scope; leaves `energySettings`,
// `energyModelExists`, `settingsRead` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// ANALYZE > ENERGY SETTINGS, READ IN THE WORDS SET_ENERGY_SETTINGS TAKES, so a
// value read here can be named back to it as it is: building type, ground
// plane, project phase, analysis mode, export category, export complexity,
// include thermal properties, building envelope, building construction class,
// service type, report type, use current view only (Revit 2024 and later),
// sliver space tolerance, analytical grid cell size, core offset, divide
// perimeter zones, target percentage glazing, energy model, use heating credits.
//
// AND WHETHER THE MODEL HOLDS AN ENERGY ANALYTICAL MODEL - Revit's main one,
// EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel - with how many
// analytical spaces, surfaces and openings it has.
//
// Lengths are millimetres, 0.01 mm. A choice is printed in Revit's own words
// for it. The glazing percentage is printed as a percent, its form asked of
// Revit's own range check (see SET_ENERGY_SETTINGS).
//
// NOT READ, and the answer says so: the location (REPORT_LOCATION reads it),
// the building operating schedule, HVAC system, outdoor
// air and the shade / sill / skylight targets (obsolete in Revit 2027).

var findings = new List<string>();
var energySettings = "";
var energyModelExists = false;
var settingsRead = 0;
var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", culture) + " mm";
Func<bool, string> yesNo = b => b ? "yes" : "no";
Func<object, string> words = e => System.Text.RegularExpressions.Regex.Replace(e.ToString(), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ").ToLowerInvariant();
Func<ElementId, string> nameOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var e = doc.GetElement(id);
    return e == null ? "none" : "\"" + e.Name + "\"";
};

if (doc.IsFamilyDocument)
    findings.Add("The document in front is a family. Energy Settings are a project's, so nothing was read.");
else
{
    EnergyDataSettings es = null;
    try
    {
#if REVIT2026_OR_GREATER
        es = EnergyDataSettings.GetEnergyDataSettings(doc);
#else
        es = EnergyDataSettings.GetFromDocument(doc);
#endif
    }
    catch (Exception ex) { findings.Add("Revit gave no Energy Settings for " + doc.Title + ": " + ex.Message); }

    if (es != null)
    {
        var glazingAsPercent = false;
        try { glazingAsPercent = EnergyDataSettings.CheckRangeOfPercentageGlazing(50.0); } catch (Exception) { glazingAsPercent = false; }
        var category = "another category";
        if (es.ExportCategory == null || es.ExportCategory == ElementId.InvalidElementId) category = "none";
        else if (es.ExportCategory == new ElementId(BuiltInCategory.OST_Rooms)) category = "rooms";
        else if (es.ExportCategory == new ElementId(BuiltInCategory.OST_MEPSpaces)) category = "spaces";

        var parts = new List<string>
        {
            "building type " + words(es.BuildingType),
            "ground plane " + nameOf(es.GroundPlane),
            "project phase " + nameOf(es.ProjectPhase),
            "analysis mode " + words(es.AnalysisType),
            "export category " + category,
            "export complexity " + words(es.ExportComplexity),
            "include thermal properties " + yesNo(es.IncludeThermalProperties),
            "building envelope " + words(es.BuildingEnvelopeDeterminationMethod),
            "building construction class " + words(es.BuildingConstructionClass),
            "service type " + words(es.ServiceType),
            "report type " + words(es.ProjectReportType),
        };
#if REVIT2024_OR_GREATER
        parts.Add("use current view only " + yesNo(es.UseCurrentViewOnly));
#endif
        parts.Add("sliver space tolerance " + mm(es.SliverSpaceTolerance));
        parts.Add("analytical grid cell size " + mm(es.AnalyticalGridCellSize));
        parts.Add("core offset " + mm(es.CoreOffset));
        parts.Add("divide perimeter zones " + yesNo(es.DividePerimeter));
        parts.Add("target percentage glazing "
            + (glazingAsPercent ? es.PercentageGlazing : es.PercentageGlazing * 100.0).ToString("0.##", culture) + " %");
        parts.Add("energy model " + yesNo(es.EnergyModel));
        parts.Add("use heating credits " + yesNo(es.UseHeatingCredits));
        settingsRead = parts.Count;
        energySettings = "Energy Settings - " + string.Join("; ", parts);
    }
    else if (findings.Count == 0)
        findings.Add(doc.Title + " has no Energy Settings.");

    EnergyAnalysisDetailModel main = null;
    try { main = EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel(doc); }
    catch (Exception ex) { findings.Add("Revit would not say whether there is an energy analytical model: " + ex.Message); }
    if (main != null)
    {
        energyModelExists = true;
        int spaces = 0, surfaces = 0, openings = 0;
        try { spaces = main.GetAnalyticalSpaces().Count; } catch (Exception) { }
        try { surfaces = main.GetAnalyticalSurfaces().Count; } catch (Exception) { }
        try { openings = main.GetAnalyticalOpenings().Count; } catch (Exception) { }
        findings.Add("This model holds an energy analytical model (id " + main.Id + ", " + words(main.Tier) + "): "
            + spaces + " analytical space(s), " + surfaces + " surface(s), " + openings + " opening(s). It was made with "
            + "the settings of its day - a later change to Energy Settings does not rebuild it.");
    }
    else
        findings.Add("This model holds no energy analytical model (Analyze > Create Energy Model has not been run, or "
            + "it was removed).");
}

findings.Add("Not read here: the location, the building operating schedule, HVAC system, outdoor air and the shade, "
    + "sill and skylight targets. Analyze > Energy Settings shows them.");
