// NOT STANDALONE. Assumes `doc`, `elements` and `templateName` are in scope;
// leaves `created`, `alreadyScheduled`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A PANEL HAS ONE SCHEDULE. Every panel schedule view in the model is read
// first and keyed by the panel it shows, so a panel that already has one is
// reported by that view's name and skipped - a second attempt is not tried and
// left to fail.
//
// ONLY ELECTRICAL EQUIPMENT IS A PANEL. Anything else is named and skipped,
// so one light fitting in the selection does not stop the panels.

var created = new List<View>();
var alreadyScheduled = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var wantedTemplate = (templateName ?? "").Trim();
Element template = null;
var go = true;

if (elements == null || elements.Count == 0)
{
    refused.Add("No panels were handed in, so NOTHING WAS CREATED. Select the electrical "
        + "equipment to schedule.");
    go = false;
}

if (go && wantedTemplate.Length > 0)
{
    var names = new List<string>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(PanelScheduleTemplate)))
    {
        names.Add(candidate.Name);
        if (string.Equals(candidate.Name, wantedTemplate, StringComparison.Ordinal))
            template = candidate;
    }
    if (template == null)
    {
        refused.Add("No panel schedule template is called '" + wantedTemplate + "', so NOTHING WAS "
            + "CREATED. The templates in this model: "
            + (names.Count == 0 ? "none" : string.Join(", ", names))
            + ". Leave the name empty to let Revit use its default for each panel.");
        go = false;
    }
}

if (go)
{
    // Panel -> the name of the schedule view that already shows it.
    var existing = new Dictionary<ElementId, string>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(PanelScheduleView)))
    {
        var view = element as PanelScheduleView;
        if (view == null || view.IsPanelScheduleTemplate()) continue;
        ElementId panelId = null;
        try { panelId = view.GetPanel(); }
        catch { panelId = null; }
        if (panelId == null || panelId == ElementId.InvalidElementId) continue;
        if (!existing.ContainsKey(panelId)) existing[panelId] = view.Name;
    }

    var panelCategory = new ElementId(BuiltInCategory.OST_ElectricalEquipment);
    var seen = new HashSet<ElementId>();

    foreach (var element in elements)
    {
        if (element == null || !seen.Add(element.Id)) continue;

        var label = string.Format("'{0}' (id {1})", element.Name, element.Id);
        var mark = element.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
        if (mark != null && mark.HasValue && !string.IsNullOrEmpty(mark.AsString()))
            label = string.Format("panel {0} (id {1})", mark.AsString(), element.Id);

        if (element.Category == null || element.Category.Id != panelCategory)
        {
            refused.Add(label + " is not electrical equipment, so it is not a panel - skipped.");
            continue;
        }

        string had;
        if (existing.TryGetValue(element.Id, out had))
        {
            alreadyScheduled.Add(label + ": " + had);
            continue;
        }

        try
        {
            var view = template == null
                ? PanelScheduleView.CreateInstanceView(doc, element.Id)
                : PanelScheduleView.CreateInstanceView(doc, template.Id, element.Id);
            if (view == null)
            {
                refused.Add(label + ": Revit returned no schedule view and raised no error.");
                continue;
            }
            created.Add(view);
            existing[element.Id] = view.Name;
            findings.Add("Made '" + view.Name + "' for " + label
                + (template == null ? ", on Revit's default template." : ", on '" + template.Name + "'."));
        }
        catch (Exception ex)
        {
            refused.Add(label + ": Revit refused - " + ex.Message + (template == null ? ""
                : " A template of a different configuration from the panel's (branch panel, "
                + "switchboard, data) is the usual cause."));
        }
    }

    findings.Insert(0, string.Format("{0} panel schedule view(s) made, {1} panel(s) already had one, "
        + "{2} refused or skipped.", created.Count, alreadyScheduled.Count, refused.Count));
    foreach (var had in alreadyScheduled)
        findings.Add("Already scheduled, left alone - " + had);
}

foreach (var reason in refused) findings.Add(reason);
