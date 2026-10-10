// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves `viewScales`,
// `scaled`, `noScale`, `heldByTemplate` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is changed. The write half
// is SET_VIEW_SCALE, and a question about a drawing's scale must never reach it.
//
// `View.Scale` IS THE DENOMINATOR. 1 : 50 is the integer 50, and it is printed
// "1 : 50" so nobody has to remember which way round it goes.
//
// A NUMBER IS PRINTED ONLY FOR A KIND OF VIEW THAT IS DRAWN AT A SCALE.
// Revit's documentation of View.Scale calls it "meaningless for perspective
// views" on every release 2020 to 2027, and a sheet, a schedule or a report
// has no View Scale row in the Properties palette. Reading the property anyway
// can still hand back a number, and a number for a sheet is a confident wrong
// answer. So the KIND decides first, from an ALLOW list - forced, because
// ViewType value 121 is spelt PresureLossReport to 2026 and PressureLossReport
// on 2027, so a list of the kinds WITHOUT a scale cannot name it on every
// release. Any kind not on the list is reported as having no scale, by its
// type.
//
// WHETHER A TEMPLATE HOLDS THE SCALE is read off the TEMPLATE's include list,
// the rule REPORT_VIEW_TEMPLATE_CONTROL follows, by comparing the template's
// own scale parameters to it ElementId to ElementId. No id is read as a number.

var findings = new List<string>();
var rows = new List<string>();
var scaled = 0;
var noScale = 0;
var heldByTemplate = 0;
var viewScales = "";

// Which of these a template's include list carries for "View Scale" is not
// documented, so all three are compared. A parameter the template does not
// have is simply skipped.
var scaleParameters = new[]
{
    BuiltInParameter.VIEW_SCALE_PULLDOWN_METRIC,
    BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL,
    BuiltInParameter.VIEW_SCALE,
};

// "holds", "free", or "" when the template's list cannot say.
Func<View, string> templateOnScale = template =>
{
    ICollection<ElementId> mayControl;
    ICollection<ElementId> notIncluded;
    try
    {
        mayControl = template.GetTemplateParameterIds();
        notIncluded = template.GetNonControlledTemplateParameterIds();
    }
    catch (Exception) { return ""; }
    if (mayControl == null) return "";

    var free = new HashSet<ElementId>(notIncluded ?? new List<ElementId>());
    var canControl = new HashSet<ElementId>(mayControl);
    var listed = false;
    foreach (var builtIn in scaleParameters)
    {
        Parameter parameter = null;
        try { parameter = template.get_Parameter(builtIn); }
        catch (Exception) { }
        if (parameter == null) continue;
        if (!canControl.Contains(parameter.Id)) continue;
        if (!free.Contains(parameter.Id)) return "holds";
        listed = true;
    }
    return listed ? "free" : "";
};

// Null when this kind of view is drawn at a scale; otherwise why it is not.
Func<View, string> noScaleBecause = view =>
{
    var kind = view.ViewType;
    if (kind == ViewType.FloorPlan || kind == ViewType.CeilingPlan
        || kind == ViewType.EngineeringPlan || kind == ViewType.AreaPlan
        || kind == ViewType.Elevation || kind == ViewType.Section
        || kind == ViewType.Detail || kind == ViewType.DraftingView
        || kind == ViewType.Legend)
        return null;

    if (kind == ViewType.ThreeD)
    {
        var perspective = false;
        try
        {
            var three = view as View3D;
            perspective = three != null && three.IsPerspective;
        }
        catch (Exception) { }
        return perspective
            ? "a perspective 3D view has none. Revit's own documentation calls the "
              + "scale of a perspective view meaningless, so no number is given"
            : null;
    }

    if (kind == ViewType.Walkthrough)
        return "a walkthrough is a perspective and has none";
    if (kind == ViewType.DrawingSheet)
        return "a sheet has no scale of its own. Each view placed on it has its own - "
             + "name those views to read theirs";
    if (kind == ViewType.Schedule || kind == ViewType.ColumnSchedule
        || kind == ViewType.PanelSchedule)
        return "a schedule is a table, not a drawing, and has none";
    if (kind == ViewType.Rendering)
        return "a rendered image is a picture, not a drawing, and has none";

    return "its kind, " + kind + ", is not one of the kinds drawn at a scale (plans, "
         + "elevations, sections, details, drafting views, legends, and 3D views that "
         + "are not perspective)";
};

var seen = new HashSet<ElementId>();
foreach (var view in views ?? new List<View>())
{
    if (view == null || !view.IsValidObject) continue;
    if (!seen.Add(view.Id)) continue;

    string name;
    try { name = view.Name; }
    catch (Exception) { name = "(a view whose name cannot be read)"; }
    var label = "'" + name + "' (" + view.ViewType + ")";

    var why = noScaleBecause(view);
    if (why != null)
    {
        noScale++;
        findings.Add(label + ": no scale - " + why + ".");
        continue;
    }

    int scale;
    try { scale = view.Scale; }
    catch (Exception ex)
    {
        noScale++;
        findings.Add(label + ": no scale - Revit would not give one: " + ex.Message);
        continue;
    }
    if (scale <= 0)
    {
        noScale++;
        findings.Add(label + ": no scale - Revit gave "
            + scale.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ", which is not one.");
        continue;
    }

    var ratio = "1 : " + scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
    string whose;

    if (view.IsTemplate)
    {
        var applies = templateOnScale(view);
        whose = applies == "holds"
            ? "a view template, and every view following it is drawn at this scale"
            : applies == "free"
                ? "a view template that leaves View Scale free, so the views following "
                  + "it keep their own"
                : "a view template; whether it includes View Scale could not be read";
    }
    else if (view.ViewTemplateId == ElementId.InvalidElementId)
    {
        whose = "its own - no view template";
    }
    else
    {
        View template = null;
        try { template = doc.GetElement(view.ViewTemplateId) as View; }
        catch (Exception) { }

        if (template == null)
        {
            whose = "it follows a view template that cannot be read back, so whether that "
                  + "template holds the scale is not known";
            findings.Add(label + ": its view template cannot be read back from the document.");
        }
        else
        {
            string templateName;
            try { templateName = template.Name; }
            catch (Exception) { templateName = "(unnamed)"; }

            var held = templateOnScale(template);
            if (held == "holds")
            {
                whose = "held by its view template '" + templateName + "' - change it there";
                heldByTemplate++;
            }
            else if (held == "free")
            {
                whose = "its own - its view template '" + templateName
                      + "' leaves View Scale free";
            }
            else
            {
                whose = "it follows the view template '" + templateName
                      + "', and whether that template holds View Scale could not be read";
                findings.Add(label + ": the include list of '" + templateName
                    + "' could not say whether it holds View Scale.");
            }
        }
    }

    scaled++;
    rows.Add(label + " " + ratio + " - " + whose);
}

viewScales = string.Join("  ||  ", rows.ToArray());

findings.Insert(0, "Read " + (scaled + noScale) + " view(s): " + scaled + " with a scale, "
    + noScale + " with none" + (heldByTemplate > 0
        ? ", " + heldByTemplate + " held by a view template" : "")
    + ". Nothing was changed.");
