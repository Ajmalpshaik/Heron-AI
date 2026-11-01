// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves
// `viewSettings`, `read`, `heldByTemplate`, `viewScales`, `scaled`, `noScale`,
// `cutPlanes`, `cutViewCount` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is changed. The write
// halves are SET_VIEW_PROPERTIES (scale, detail level, visual style, phase,
// phase filter and a plan's view range) and MOVE_VIEW_CUT_PLANE; a question
// about how a view is set must never reach them.
//
// VERSION 2 (2026-11-01) - READ_VIEW_SCALE AND REPORT_VIEW_CUT_PLANES MERGED
// IN, on the owner's instruction ("make sure there is no duplicate tools").
// What each had that this lacked:
//
//   FROM READ_VIEW_SCALE - `viewScales`, `scaled` and `noScale`, the block
//   right below, carried line for line so tests/test_read_view_scale.py pins it
//   here as it pinned it there. Each view's scale written 1 : N; NO NUMBER for
//   a kind not drawn to a ratio - a sheet, a schedule, a perspective, a
//   walkthrough, a rendering, a report - each named with the reason; and
//   whether the scale is the view's own or HELD BY ITS VIEW TEMPLATE, read off
//   the template's include list and named with the template. A view whose
//   scale its template holds counts once in `heldByTemplate`, however many of
//   its settings that template holds.
//
//   FROM REPORT_VIEW_CUT_PLANES - `cutPlanes` and `cutViewCount`: for each
//   section, elevation and detail view named - or, with NO view named, every
//   one in the model - a point on its cut plane in millimetres, the direction
//   it LOOKS, and its far clip, on or off and how deep, as Revit prints it. A
//   plan named has no cut plane here and is not listed.
//
// FIVE SETTINGS, READ AS PARAMETERS, IN REVIT'S OWN WORDS. Detail Level,
// Visual Style, Discipline, Phase and Phase Filter are each the view's own
// parameter, found by BuiltInParameter and printed with Parameter.AsValueString
// - "Fine", "Hidden Line", "Mechanical", "New Construction", "Show Complete" -
// under LabelUtils' label for it, so nothing is typed here and the reply reads
// like the Properties palette in whatever language Revit runs in.
//
// A PLAN'S VIEW RANGE IS READ TOO - top, cut plane, bottom and view depth,
// each as a level and an offset in millimetres, the way the View Range dialog
// shows them: "Cut plane: Level 1 + 1200 mm". A plane on Revit's own markers is
// named in the dialog's words - Associated Level, Level Above, Level Below,
// Unlimited. Whether a template holds it is read off PLAN_VIEW_RANGE in the
// template's include list, the same way as the five.
//
// A SETTING THIS KIND OF VIEW DOES NOT HAVE IS NAMED, NOT GUESSED. A sheet has
// no detail level; a schedule has a phase and nothing else. A parameter the
// view does not carry is listed as not on this kind of view.
//
// WHETHER A TEMPLATE HOLDS A SETTING is read off the TEMPLATE's include list,
// the rule REPORT_VIEW_TEMPLATE_CONTROL follows, ElementId to ElementId. No id
// is read as a number.
//
// ONE STRING, ROWS JOINED BY "  ||  ", because a list reaches the chat cut to
// three entries and the rows are the answer.

var findings = new List<string>();
var settingRows = new List<string>();
var read = 0;
var heldByTemplate = 0;
var viewSettings = "";
var named = views ?? new List<View>();

// ---- THE SCALE (version 2, from READ_VIEW_SCALE) ----------------------------
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

var scaled = 0;
var noScale = 0;
var viewScales = "";
var scaleHeld = new HashSet<ElementId>();
{
    var rows = new List<string>();

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

    var scaleSeen = new HashSet<ElementId>();
    foreach (var view in named)
    {
        if (view == null || !view.IsValidObject) continue;
        if (!scaleSeen.Add(view.Id)) continue;

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
                    scaleHeld.Add(view.Id);
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
}

// ---- THE FIVE SETTINGS AND A PLAN'S VIEW RANGE --------------------------------

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
    foreach (var view in named)
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

        // The view range, for a plan only.
        var plan = view as ViewPlan;
        if (plan != null)
        {
            try
            {
                var range = plan.GetViewRange();
                Func<ElementId, string> levelName = id =>
                {
                    if (id == PlanViewRange.Current) return "Associated Level";
                    if (id == PlanViewRange.LevelAbove) return "Level Above";
                    if (id == PlanViewRange.LevelBelow) return "Level Below";
                    if (id == PlanViewRange.Unlimited) return "Unlimited";
                    var level = doc.GetElement(id);
                    return level == null ? "(no level)" : level.Name;
                };
                var planes = new[]
                {
                    new KeyValuePair<string, PlanViewPlane>("Top", PlanViewPlane.TopClipPlane),
                    new KeyValuePair<string, PlanViewPlane>("Cut plane", PlanViewPlane.CutPlane),
                    new KeyValuePair<string, PlanViewPlane>("Bottom", PlanViewPlane.BottomClipPlane),
                    new KeyValuePair<string, PlanViewPlane>("View depth", PlanViewPlane.ViewDepthPlane),
                };
                var rangeParts = new List<string>();
                foreach (var plane in planes)
                {
                    var id = range.GetLevelId(plane.Value);
                    var where = levelName(id);
                    if (id != PlanViewRange.Unlimited)
                    {
                        var mm = Math.Round(range.GetOffset(plane.Value) * 304.8);
                        where += (mm < 0 ? " - " : " + ")
                            + Math.Abs(mm).ToString(System.Globalization.CultureInfo.InvariantCulture) + " mm";
                    }
                    rangeParts.Add(plane.Key + ": " + where);
                }
                var rangeText = "View Range = " + string.Join(", ", rangeParts.ToArray());
                if (template != null && canControl != null && free != null)
                {
                    Parameter onTemplate = null;
                    try { onTemplate = template.get_Parameter(BuiltInParameter.PLAN_VIEW_RANGE); }
                    catch (Exception) { }
                    if (onTemplate != null && canControl.Contains(onTemplate.Id) && !free.Contains(onTemplate.Id))
                    {
                        rangeText += " (held by its view template)";
                        held++;
                    }
                }
                parts.Add(rangeText);
            }
            catch (Exception ex)
            {
                findings.Add(label + ": its view range could not be read (" + ex.Message.TrimEnd('.') + ").");
            }
        }

        if (parts.Count == 0)
        {
            findings.Add(label + ": none of these settings is on this kind of view.");
            continue;
        }

        read++;
        // ONCE PER VIEW (version 2): a view whose SCALE its template holds was
        // counted above, and is not counted twice.
        if (held > 0 && !scaleHeld.Contains(view.Id)) heldByTemplate++;
        var row = label + " " + string.Join(", ", parts.ToArray());
        if (template != null) row += " - follows view template '" + template.Name + "'";
        if (absent.Count > 0) row += " - not on this kind of view: " + string.Join(", ", absent.ToArray());
        settingRows.Add(row);
    }

    viewSettings = string.Join("  ||  ", settingRows.ToArray());
}

// ---- WHERE EACH SECTION AND ELEVATION CUTS (version 2, REPORT_VIEW_CUT_PLANES) ----
//
// For every section, elevation and detail view named - or, with no view named,
// every one in the model - a point on its cut plane in millimetres, the
// direction it looks, and its far clip: on or off, and how deep, as Revit
// prints it.
//
// "LOOKING" IS THE OPPOSITE OF ViewDirection, and that is said here because
// it is the mistake to make: Revit's ViewDirection points from the model OUT
// TOWARDS the viewer. A section looking north has a ViewDirection of south.

var cutPlanes = "";
var cutViewCount = 0;
{
    const double MmPerFoot = 304.8;
    var lines = new List<string>();
    Func<double, string> mmOf = v => Math.Round(v * MmPerFoot).ToString(System.Globalization.CultureInfo.InvariantCulture);
    Func<double, string> unit = v => Math.Round(v, 3).ToString(System.Globalization.CultureInfo.InvariantCulture);

    var everyOne = named.Count(v => v != null) == 0;
    IEnumerable<View> asked = everyOne
        ? new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        : named.Where(v => v != null);

    var cutSeen = new HashSet<ElementId>();
    foreach (var view in asked)
    {
        if (view == null || !view.IsValidObject || view.IsTemplate) continue;
        if (view.ViewType != ViewType.Section && view.ViewType != ViewType.Elevation && view.ViewType != ViewType.Detail)
            continue;
        if (!cutSeen.Add(view.Id)) continue;

        cutViewCount++;
        var origin = view.Origin;
        var looking = view.ViewDirection.Negate();

        var farOn = "";
        var farDepth = "";
        try
        {
            var clip = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING);
            if (clip != null) farOn = clip.AsValueString() ?? "";
            var depth = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
            if (depth != null) farDepth = depth.AsValueString() ?? "";
        }
        catch { }

        lines.Add(view.ViewType + ": " + view.Name + " | cuts through " + mmOf(origin.X) + "," + mmOf(origin.Y) + ","
            + mmOf(origin.Z) + " mm | looking " + unit(looking.X) + "," + unit(looking.Y) + "," + unit(looking.Z)
            + " | far clip " + (farOn == "" ? "(not read)" : farOn) + (farDepth == "" ? "" : ", " + farDepth));
    }

    cutPlanes = string.Join("\n", lines);
    if (everyOne)
        findings.Add("No view was named, so the settings and the scale were read for none - and every section, "
            + "elevation and detail view in the model was listed for where it cuts: " + cutViewCount + ".");
    else if (cutViewCount > 0)
        findings.Add(cutViewCount + " section, elevation or detail view(s) read for where they cut.");
}

findings.Insert(0, "Read " + (scaled + noScale) + " view(s) for their scale: " + scaled + " with a scale, "
    + noScale + " with none. Nothing was changed.");
findings.Insert(0, read + " view(s) read; " + heldByTemplate + " of them have at least one setting held by "
    + "their view template, which is changed on the template, not the view.");
