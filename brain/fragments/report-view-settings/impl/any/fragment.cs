// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves
// `viewSettings`, `read`, `heldByTemplate` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is changed. The write
// halves are SET_VIEW_PROPERTIES (detail level, visual style) and
// SET_VIEW_PHASE (phase, phase filter); a question about how a view is set
// must never reach them. The scale is READ_VIEW_SCALE's, and is not repeated
// here.
//
// FIVE SETTINGS, READ AS PARAMETERS, IN REVIT'S OWN WORDS. Detail Level,
// Visual Style, Discipline, Phase and Phase Filter are each the view's own
// parameter, found by BuiltInParameter and printed with Parameter.AsValueString
// - "Fine", "Hidden Line", "Mechanical", "New Construction", "Show Complete" -
// under LabelUtils' label for it, so nothing is typed here and the reply reads
// like the Properties palette in whatever language Revit runs in.
//
// A SETTING THIS KIND OF VIEW DOES NOT HAVE IS NAMED, NOT GUESSED. A sheet has
// no detail level; a schedule has a phase and nothing else. A parameter the
// view does not carry is listed as not on this kind of view.
//
// WHETHER A TEMPLATE HOLDS A SETTING is read off the TEMPLATE's include list,
// the rule READ_VIEW_SCALE and REPORT_VIEW_TEMPLATE_CONTROL follow, ElementId
// to ElementId. No id is read as a number.
//
// ONE STRING, ROWS JOINED BY "  ||  ", because a list reaches the chat cut to
// three entries and the rows are the answer.

var findings = new List<string>();
var rows = new List<string>();
var read = 0;
var heldByTemplate = 0;
var viewSettings = "";

{
    var settings = new[]
    {
        BuiltInParameter.VIEW_DETAIL_LEVEL,
        BuiltInParameter.MODEL_GRAPHICS_STYLE,
        BuiltInParameter.VIEW_DISCIPLINE,
        BuiltInParameter.VIEW_PHASE,
        BuiltInParameter.VIEW_PHASE_FILTER,
    };

    Func<BuiltInParameter, string> labelOf = builtIn =>
    {
        try { return LabelUtils.GetLabelFor(builtIn); }
        catch (Exception) { return builtIn.ToString(); }
    };

    Func<Parameter, string> valueOf = p =>
    {
        string text = null;
        try { text = p.AsValueString(); } catch { }
        if (string.IsNullOrWhiteSpace(text))
        {
            try { text = p.AsString(); } catch { }
        }
        return string.IsNullOrWhiteSpace(text) ? "(none)" : text.Trim();
    };

    var seen = new HashSet<ElementId>();
    foreach (var view in views ?? new List<View>())
    {
        if (view == null || !view.IsValidObject) continue;
        if (!seen.Add(view.Id)) continue;

        string name;
        try { name = view.Name; }
        catch (Exception) { name = "(a view whose name cannot be read)"; }
        var label = "'" + name + "' (" + view.ViewType + (view.IsTemplate ? ", a view template" : "") + ")";

        // The template this view follows, and which of its settings that
        // template's include list holds. Null sets mean "not read".
        View template = null;
        HashSet<ElementId> canControl = null;
        HashSet<ElementId> free = null;
        if (!view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
        {
            template = doc.GetElement(view.ViewTemplateId) as View;
            if (template != null)
            {
                try
                {
                    var mayControl = new HashSet<ElementId>(template.GetTemplateParameterIds());
                    var notIncluded = new HashSet<ElementId>(template.GetNonControlledTemplateParameterIds()
                                                             ?? new List<ElementId>());
                    canControl = mayControl;
                    free = notIncluded;
                }
                catch (Exception ex)
                {
                    canControl = null;
                    free = null;
                    findings.Add(label + ": the include list of its template '" + template.Name
                        + "' could not be read (" + ex.Message.TrimEnd('.') + "), so which settings it holds is unknown.");
                }
            }
        }

        var parts = new List<string>();
        var absent = new List<string>();
        var held = 0;
        foreach (var builtIn in settings)
        {
            Parameter parameter = null;
            try { parameter = view.get_Parameter(builtIn); }
            catch (Exception) { }
            if (parameter == null)
            {
                absent.Add(labelOf(builtIn));
                continue;
            }

            var text = parameter.Definition.Name + " = " + valueOf(parameter);
            if (template != null && canControl != null && free != null)
            {
                Parameter onTemplate = null;
                try { onTemplate = template.get_Parameter(builtIn); }
                catch (Exception) { }
                if (onTemplate != null && canControl.Contains(onTemplate.Id) && !free.Contains(onTemplate.Id))
                {
                    text += " (held by its view template)";
                    held++;
                }
            }
            parts.Add(text);
        }

        if (parts.Count == 0)
        {
            findings.Add(label + ": none of the five settings is on this kind of view.");
            continue;
        }

        read++;
        if (held > 0) heldByTemplate++;
        var row = label + " " + string.Join(", ", parts.ToArray());
        if (template != null) row += " - follows view template '" + template.Name + "'";
        if (absent.Count > 0) row += " - not on this kind of view: " + string.Join(", ", absent.ToArray());
        rows.Add(row);
    }

    viewSettings = string.Join("  ||  ", rows.ToArray());
    findings.Insert(0, read + " view(s) read; " + heldByTemplate + " of them have at least one setting held by "
        + "their view template, which is changed on the template, not the view. The scale is READ_VIEW_SCALE's.");
}
