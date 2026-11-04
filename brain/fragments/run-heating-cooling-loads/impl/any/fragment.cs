// NOT STANDALONE. Assumes `doc` and `reportName` are in scope, and leaves
// `reportView`, `coolingW`, `heatingW`, `analysisCompleted`, `noLoad`,
// `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT IT DOES. Revit's Analyze > Heating and Cooling Loads, from the API:
// a loads report view is made with ViewHVACLoadsReport.Create, the analysis
// is run on it with RunLoadsAnalysis, and every placed space's CALCULATED
// heating and cooling load is read back afterwards.
//
// REVIT 2027 ONLY, and that is the API, not a choice. ViewHVACLoadsReport -
// Create, RunLoadsAnalysis, IsAnalysisCompleted - is in RevitAPI 2027 and in
// no release 2020 to 2026 (tools/api-surface --members, 2026-11-02). Nothing
// else in any of those releases runs the analysis: EnergyDataSettings and
// EnergyAnalysisDetailModel set up and build the energy model and run no
// loads, and PostableCommand.HeatingAndCoolingLoads (2020, 2021, 2027) only
// opens Revit's own dialog for a person to press Calculate in.
//
// THE MODEL'S OWN ENERGY SETTINGS, NEVER CHANGED. EnergyDataSettings is read
// - to say in the report which building type, phase and report type the
// analysis ran with - and nothing on it is set. Settings Revit cannot run
// with are Revit's to name: RunLoadsAnalysis's own refusal is passed on in
// its own words, the host rolls the whole call back, and nothing is kept.
//
// REFUSED BEFORE ANYTHING IS MADE: a family; a model with no spaces, or with
// spaces none of which is placed - loads are calculated space by space; no
// Energy Settings to read; and a report name a view already has.
//
// CALCULATED VALUES, NOT A VERDICT. The figures are what Revit's analysis
// gave with this model's settings. Nothing here calls a load, a space or a
// design compliant, adequate or correct - that is the engineer's call.
//
// THE READING IS READ_SPACE_LOADS', COPIED - not called and not edited: the
// six figures read inside one guard, because Revit throws "Not Computed!"
// when Area and Volume Computations are off or a space is unbounded; a zero
// reported as a question, never as a load; a typed design figure, which
// overrides the calculated one in Revit, named beside it. ONE THING IS NOT
// COPIED: READ_SPACE_LOADS converts with a factor its own card says is
// assumed and unproven. Here Revit converts its own internal value, with
// UnitUtils.ConvertFromInternalUnits and UnitTypeId.Watts - nothing assumed.
//
// IS THE ANALYSIS FINISHED WHEN RunLoadsAnalysis RETURNS? NOT SEEN IN REVIT.
// IsAnalysisCompleted exists beside it, which suggests it may not be. So it
// is asked, and handed back as `analysisCompleted`: when Revit says no, the
// spaces' figures are NOT handed back as results of this run - they may be
// from before it - and the report says so.
//
// An ElementId is never read as a number.

string refused = null;
string report = null;
var findings = new List<string>();
var reportView = ElementId.InvalidElementId;
var coolingW = new Dictionary<ElementId, double>();
var heatingW = new Dictionary<ElementId, double>();
var noLoad = new List<ElementId>();
bool analysisCompleted = false;

var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> watts = w => w.ToString("0", culture) + " W";
Func<Space, string> labelOf = s => string.Format("{0} {1}", s.Number ?? "", s.Name ?? "").Trim();

var wanted = (reportName ?? "").Trim();
var placed = new List<Space>();
EnergyDataSettings settings = null;
string viewName = null;

if (doc.IsFamilyDocument)
{
    refused = "The document in front is a family. Heating and cooling loads are calculated on a project's spaces. "
        + "NOTHING WAS RUN.";
}

if (refused == null)
{
    var spaces = new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_MEPSpaces)
        .WhereElementIsNotElementType()
        .OfType<Space>()
        .ToList();
    foreach (var s in spaces)
    {
        double area = 0.0;
        try { area = s.Area; } catch (Exception) { area = 0.0; }
        if (area > 0.0) placed.Add(s);
    }

    if (spaces.Count == 0)
        refused = "This model has no spaces. Revit calculates heating and cooling loads space by space, so there is "
            + "nothing to calculate - a Room carries no load. Place spaces first (Analyze > Space, or make them from "
            + "the linked model's rooms), then ask again. NOTHING WAS RUN.";
    else if (placed.Count == 0)
        refused = "This model has " + spaces.Count + " space(s) and none of them is placed - each is in the schedule "
            + "with no area, so there is nothing to calculate. Place them in the model, then ask again. NOTHING WAS RUN.";
}

if (refused == null)
{
    try { settings = EnergyDataSettings.GetEnergyDataSettings(doc); }
    catch (Exception ex)
    {
        refused = "Revit could not read this model's Energy Settings: " + ex.Message
            + " Open Analyze > Energy Settings, then ask again. NOTHING WAS RUN.";
    }
    if (refused == null && settings == null)
        refused = "This model has no Energy Settings for the analysis to run with. Open Analyze > Energy Settings, "
            + "then ask again. Heron does not fill them in. NOTHING WAS RUN.";
}

// THE REPORT'S NAME. Given, it is used as given - and a name ANY view already
// has, of any kind, is refused rather than changed. Left out, the first of
// "Heating and Cooling Loads Report", "... 2", "... 3" no view has yet.
if (refused == null)
{
    var taken = new HashSet<string>(
        new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Select(v => v.Name ?? ""),
        StringComparer.OrdinalIgnoreCase);
    if (wanted.Length > 0)
    {
        if (taken.Contains(wanted))
            refused = "A view called \"" + wanted + "\" is already in this model. Name the loads report something "
                + "else. NOTHING WAS RUN - no report was made and no space's loads were changed.";
        else
            viewName = wanted;
    }
    else
    {
        viewName = "Heating and Cooling Loads Report";
        for (var n = 2; taken.Contains(viewName); n++) viewName = "Heating and Cooling Loads Report " + n;
    }
}

if (refused == null)
{
    // WHAT THE ANALYSIS RAN WITH - read, never set.
    string ran = "";
    try
    {
        var phase = doc.GetElement(settings.ProjectPhase);
        ran = string.Format("building type {0}, phase \"{1}\", report type {2}, use current view only {3}",
            settings.BuildingType, phase != null ? phase.Name : "(none)", settings.ProjectReportType,
            settings.UseCurrentViewOnly ? "on" : "off");
        if (settings.ProjectReportType == HVACLoadLoadsReportType.NoReport)
            findings.Add("The Energy Settings' report type is No Report. What Revit puts in the report view then "
                + "has not been seen here; the spaces' loads are read back either way.");
    }
    catch (Exception) { ran = "settings not readable in full"; }

    // BEFORE, so the report can say how many spaces' figures the run changed.
    var before = new Dictionary<ElementId, string>();
    foreach (var s in placed)
    {
        try { before[s.Id] = s.CalculatedHeatingLoad.ToString("R", culture) + "|" + s.CalculatedCoolingLoad.ToString("R", culture); }
        catch (Exception) { before[s.Id] = "unreadable"; }
    }

    ViewHVACLoadsReport made;
    try { made = ViewHVACLoadsReport.Create(doc, viewName); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to make the loads report view \"" + viewName + "\": "
            + ex.Message + " NOTHING from this call was kept.");
    }
    if (made == null)
        throw new InvalidOperationException("Revit made no loads report view and gave no reason. NOTHING from this "
            + "call was kept.");

    try { made.RunLoadsAnalysis(); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to run the heating and cooling loads: " + ex.Message
            + " - Revit's own words. It is usually something in Analyze > Energy Settings or in the spaces; Heron "
            + "changes neither. NOTHING from this call was kept - no report and no space's loads.");
    }

    try { analysisCompleted = made.IsAnalysisCompleted(); } catch (Exception) { analysisCompleted = false; }
    reportView = made.Id;

    int changed = 0;
    foreach (var space in placed)
    {
        var label = labelOf(space);
        double calcHeat, calcCool, designHeat, designCool;
        try
        {
            calcHeat = space.CalculatedHeatingLoad;
            calcCool = space.CalculatedCoolingLoad;
            designHeat = space.DesignHeatingLoad;
            designCool = space.DesignCoolingLoad;
        }
        catch (Exception)
        {
            noLoad.Add(space.Id);
            findings.Add(label + "  - NO LOAD READABLE. Revit refused the figures, which it does when the model's "
                + "Areas and Volumes computation is off or the space is unbounded. Turn on Area and Volume "
                + "Computations, or bound the space, and run the loads again.");
            continue;
        }

        var now = calcHeat.ToString("R", culture) + "|" + calcCool.ToString("R", culture);
        string was;
        if (before.TryGetValue(space.Id, out was) && was != now) changed++;

        if (!analysisCompleted) continue;

        var heatW = UnitUtils.ConvertFromInternalUnits(calcHeat, UnitTypeId.Watts);
        var coolW = UnitUtils.ConvertFromInternalUnits(calcCool, UnitTypeId.Watts);

        // A ZERO IS NOT A LOAD - READ_SPACE_LOADS' rule, copied.
        if (heatW <= 1e-6 && coolW <= 1e-6)
        {
            noLoad.Add(space.Id);
            findings.Add(label + "  - NO LOAD CALCULATED. The analysis gave this space nothing - usually it is "
                + "unbounded, has no volume, or is outside the phase or view the Energy Settings name.");
            continue;
        }

        heatingW[space.Id] = heatW;
        coolingW[space.Id] = coolW;

        var typed = "";
        if (designCool > 1e-9)
            typed += "; a typed Design Cooling Load of "
                + watts(UnitUtils.ConvertFromInternalUnits(designCool, UnitTypeId.Watts)) + " overrides it in Revit";
        if (designHeat > 1e-9)
            typed += "; a typed Design Heating Load of "
                + watts(UnitUtils.ConvertFromInternalUnits(designHeat, UnitTypeId.Watts)) + " overrides it in Revit";
        findings.Add(label + "  - calculated cooling " + watts(coolW) + ", calculated heating " + watts(heatW) + typed);
    }

    report = "Heating and cooling loads run. Loads report view \"" + viewName + "\" made (id " + made.Id
        + "). Ran with this model's Energy Settings, unchanged: " + ran + ". "
        + (analysisCompleted
            ? "Revit says the analysis completed. " + coolingW.Count + " of " + placed.Count + " placed space(s) "
              + "have a calculated load; " + noLoad.Count + " have none readable. "
            : "REVIT SAYS THE ANALYSIS HAS NOT COMPLETED. The spaces' figures are not handed back as results of "
              + "this run - they may be from before it. Open the report view to see what Revit shows. ")
        + changed + " space(s)' calculated figures changed with this run. These are calculated values from Revit's "
        + "analysis with these settings - not a design check, and not a statement that anything complies.";
}
