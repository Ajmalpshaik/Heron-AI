// NOT STANDALONE. Assumes `doc`, `reportName`, `weatherFile` and
// `workflowFile` are in scope, and leaves `reportView`, `requested`,
// `analysisCompleted`, `reportContent`, `weatherFileUsed`, `workflowFileUsed`,
// `outputFolder`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT IT DOES. Revit's Analyze > Systems Analysis, from the API: a systems
// analysis report view is made with ViewSystemsAnalysisReport.Create, and the
// analysis is requested on it with RequestSystemsAnalysis. RevitAPI.xml: the
// request runs "in the background" - the call returns before the analysis has
// finished, so IsAnalysisCompleted and the report view's content are read and
// handed back as they stand, and the finished report is read in Revit's own
// report view.
//
// THE MODEL'S OWN ENERGY MODEL, SPACES, ZONES AND SYSTEMS. Nothing about them
// is set. A weather or workflow file is set on the request only when the
// modeller names one, and only after it is found on this PC. The output folder
// is never set: Revit's default, the system TEMP folder, is used - no shared
// location is written (Article 10).
//
// REFUSED BEFORE ANYTHING IS MADE: a family; a report name a view already has;
// a weather file that is not an existing .epw; a workflow file that is not an
// existing .osw. Revit's own refusal to make the view or to take the request -
// "A valid energy model is required", no weather station near the site - is
// passed on in its words and the whole call is rolled back: nothing is kept.
//
// CALCULATED VALUES, NOT A VERDICT. Nothing here calls a system or a design
// compliant, adequate or correct - that is the engineer's call.
//
// An ElementId is never read as a number.

string refused = null;
string report = null;
var findings = new List<string>();
ElementId reportView = null; // null, never InvalidElementId, when nothing was made: it reads as nothing
bool requested = false;
bool analysisCompleted = false;
string reportContent = "";
string weatherFileUsed = "";
string workflowFileUsed = "";
string outputFolder = "";

// An optional need left out arrives as the text "none" (row 5b-1432).
var wantedName = (reportName ?? "").Trim();
if (string.Equals(wantedName, "none", StringComparison.OrdinalIgnoreCase)) wantedName = "";
var weather = (weatherFile ?? "").Trim().Trim('"');
if (string.Equals(weather, "none", StringComparison.OrdinalIgnoreCase)) weather = "";
var workflow = (workflowFile ?? "").Trim().Trim('"');
if (string.Equals(workflow, "none", StringComparison.OrdinalIgnoreCase)) workflow = "";

string viewName = null;

if (doc.IsFamilyDocument)
    refused = "The document in front is a family. Systems Analysis runs on a project's spaces, zones and HVAC "
        + "systems. NOTHING WAS MADE.";

if (refused == null && weather.Length > 0
    && !(weather.EndsWith(".epw", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(weather)))
    refused = "weatherFile \"" + weather + "\" is not an EnergyPlus weather file (.epw) found on this PC. Leave it out "
        + "for Revit's own choice for the site's location. NOTHING WAS MADE.";

if (refused == null && workflow.Length > 0
    && !(workflow.EndsWith(".osw", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(workflow)))
    refused = "workflowFile \"" + workflow + "\" is not an OpenStudio workflow file (.osw) found on this PC. Leave it "
        + "out for Revit's default, \"HVAC Systems Loads and Sizing.osw\". NOTHING WAS MADE.";

if (refused == null)
{
    var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var view = element as View;
        if (view == null) continue;
        try { taken.Add(view.Name ?? ""); } catch (Exception) { }
    }
    if (wantedName.Length > 0)
    {
        if (taken.Contains(wantedName))
            refused = "A view is already named \"" + wantedName + "\". Name the report something no view has, or leave "
                + "the name out. NOTHING WAS MADE.";
        else
            viewName = wantedName;
    }
    else
    {
        const string Stem = "Systems Analysis Report";
        viewName = Stem;
        for (var n = 2; taken.Contains(viewName); n++) viewName = Stem + " " + n;
    }
}

if (refused == null)
{
    // What there is to analyse, said before the run - never a refusal of
    // Heron's: whether the model is fit to run is Revit's to say.
    var spaceCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_MEPSpaces)
        .WhereElementIsNotElementType().GetElementCount();
    EnergyAnalysisDetailModel energyModel = null;
    try { energyModel = EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel(doc); } catch (Exception) { energyModel = null; }
    findings.Add("The model has " + spaceCount + " space(s) and " + (energyModel == null
        ? "no energy analytical model yet - Revit says whether it can run without one (CREATE_ENERGY_MODEL builds it)"
        : "an energy analytical model") + ".");

    var earlier = ElementId.InvalidElementId;
    try { earlier = ViewSystemsAnalysisReport.GetLatestSystemsAnalysisReport(doc); } catch (Exception) { earlier = ElementId.InvalidElementId; }
    if (earlier != null && earlier != ElementId.InvalidElementId)
    {
        var earlierView = doc.GetElement(earlier) as View;
        findings.Add("An earlier systems analysis report is in the model: \"" + (earlierView != null ? earlierView.Name : "unnamed")
            + "\". It is left as it is.");
    }

    ViewSystemsAnalysisReport made = null;
    try { made = ViewSystemsAnalysisReport.Create(doc, viewName); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to make the systems analysis report view \"" + viewName + "\": "
            + ex.Message + " - Revit's own words. NOTHING from this call was kept.");
    }
    if (made == null)
        throw new InvalidOperationException("Revit made no systems analysis report view and gave no reason. NOTHING from "
            + "this call was kept.");

    var options = new SystemsAnalysisOptions();
    try
    {
        if (weather.Length > 0) options.WeatherFile = weather;
        if (workflow.Length > 0) options.WorkflowFile = workflow;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the file named: " + ex.Message + " - Revit's own words. NOTHING "
            + "from this call was kept.");
    }

    try { made.RequestSystemsAnalysis(options); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to start the systems analysis: " + ex.Message + " - Revit's own "
            + "words. A valid energy model, a site location near a weather station and the model's spaces and zones are "
            + "what it usually asks for; Heron changes none of them. NOTHING from this call was kept.");
    }

    requested = true;
    reportView = made.Id;
    try { analysisCompleted = made.IsAnalysisCompleted(); } catch (Exception) { analysisCompleted = false; }
    try { reportContent = made.GetReportContent() ?? ""; } catch (Exception ex) { findings.Add("The report view's content could not be read - " + ex.Message); }
    try { weatherFileUsed = made.WeatherFile ?? ""; } catch (Exception) { weatherFileUsed = ""; }
    try { workflowFileUsed = made.SystemsAnalysisWorkflowFile ?? ""; } catch (Exception) { workflowFileUsed = ""; }
    try { outputFolder = made.SystemsAnalysisOutputFolder ?? ""; } catch (Exception) { outputFolder = ""; }

    findings.Add("Weather file: " + (weatherFileUsed.Length > 0 ? weatherFileUsed : "not read")
        + (weather.Length > 0 ? " (as named)" : " (Revit's choice)") + "; workflow: "
        + (workflowFileUsed.Length > 0 ? workflowFileUsed : "not read") + "; results written to: "
        + (outputFolder.Length > 0 ? outputFolder : "not read") + ".");
    findings.Add(analysisCompleted
        ? "Revit says the analysis has completed - the report view \"" + viewName + "\" shows the result."
        : "THE ANALYSIS RUNS IN THE BACKGROUND and had not finished when the call returned. Open the report view \""
          + viewName + "\" in the Project Browser to read it when Revit has finished.");
    report = "Made the systems analysis report view \"" + viewName + "\" and started the analysis"
        + (analysisCompleted ? "; Revit says it has completed." : "; it runs in the background - read the result in that view.");
}

if (refused != null)
{
    report = refused;
    findings.Add(refused);
}