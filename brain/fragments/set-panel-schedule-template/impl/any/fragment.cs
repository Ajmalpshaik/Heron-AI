// NOT STANDALONE. Assumes `doc`, `elements`, `action`, `templateName`,
// `fromTemplate` and `panelName` are in scope, and leaves `changed`, `report`,
// `before`, `after`, `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// WHAT IT DOES. Two things from Revit's Manage > Panel Schedule Templates, one
// at a time - `action` says which:
//   make  - a new panel schedule template, a copy of one the model already has
//           (`fromTemplate`), under the modeller's name (`templateName`).
//   apply - the panel schedule of each panel selected or named (`panelName`)
//           switched to a template the model has - what Change Template does
//           on an open panel schedule.
//
// NEVER GUESSED (D-33). No template name is made up: a name that is not there
// exactly is refused with the model's templates listed, and one differing only
// in capitals is named, never taken. A panel name is matched exactly; two
// panels with one name are both named and neither is touched.
//
// NEVER OVERWRITES, RENAMES OR DELETES. A new name already used by a template,
// in any capitals, is skipped and named. A panel with no schedule view yet is
// skipped - making one is Create Panel Schedules' job, not this one's.
//
// READ BACK. A new template's name and kind, and each switched schedule's
// template, are read back from the model. A read-back that disagrees with
// what Revit accepted throws, so the caller's transaction rolls the whole
// change back rather than keeping half of it (Article 8).
//
// RELEASES. PanelScheduleTemplate.Create(Document, PanelScheduleType,
// PanelConfiguration, string), CopyFrom(Document, PanelScheduleTemplate),
// GetPanelScheduleType, GetTableData, IsValidType and
// IsValidPanelConfiguration; PanelScheduleData.PanelConfiguration;
// PanelScheduleView.GenerateInstanceFromTemplate(ElementId), GetTemplate,
// GetPanel and IsPanelScheduleTemplate are present 2020 to 2027
// (tools/api-surface --members, 2026-11-02).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var changed = 0;
var report = "";
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<PanelScheduleType, string> kindOf = t =>
    t == PanelScheduleType.Branch ? "branch panel"
    : t == PanelScheduleType.Switchboard ? "switchboard"
    : t == PanelScheduleType.Data ? "data panel"
    : "unknown kind";

// ---- The model's templates, read fresh whenever asked ----------------------
Func<List<PanelScheduleTemplate>> readTemplates = () =>
{
    var found = new List<PanelScheduleTemplate>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(PanelScheduleTemplate)))
    {
        var t = e as PanelScheduleTemplate;
        if (t != null) found.Add(t);
    }
    found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    return found;
};
Func<PanelScheduleTemplate, string> templateLine = t =>
{
    var kind = "unknown kind";
    try { kind = kindOf(t.GetPanelScheduleType()); } catch (Exception) { }
    return quoted(t.Name) + " (" + kind + (t.IsDefault ? ", default" : "") + ")";
};
var templates = readTemplates();
Func<string> templateNames = () => templates.Count == 0 ? "none"
    : string.Join(", ", templates.Select(t => quoted(t.Name)));
Func<string, PanelScheduleTemplate> exactly = n =>
    templates.FirstOrDefault(t => string.Equals(t.Name, n, StringComparison.Ordinal));
Func<string, PanelScheduleTemplate> anyCapitals = n =>
    templates.FirstOrDefault(t => string.Equals(t.Name, n, StringComparison.OrdinalIgnoreCase));
Func<string, string, string> notFound = (what, n) =>
{
    var near = anyCapitals(n);
    return "No panel schedule template is named " + quoted(n) + (what.Length > 0 ? " (" + what + ")" : "")
        + (near != null ? " - " + quoted(near.Name) + " differs only in capitals and is not taken; name it exactly" : "")
        + ". The model's panel schedule templates are: " + templateNames() + ".";
};

// ---- Which action ----------------------------------------------------------
var said = (action ?? "").Trim().ToLowerInvariant();
var first = said.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
string mode = null;
if (new[] { "make", "create", "new", "copy", "duplicate", "add" }.Contains(first)) mode = "make";
else if (new[] { "apply", "use", "set", "assign", "change", "switch" }.Contains(first)) mode = "apply";
else
    refused.Add("\"" + (action ?? "") + "\" is not one of the two things this does - make (a new template copied "
        + "from one the model has) or apply (switch panels' schedules to a template). Nothing was changed.");

var wanted = (templateName ?? "").Trim();
if (mode != null && wanted.Length == 0)
{
    refused.Add(mode == "make"
        ? "No name was given for the new panel schedule template - it is never made up. Nothing was changed."
        : "No template was named to switch to - it is never guessed. The model's panel schedule templates are: "
          + templateNames() + ".");
    mode = null;
}

// ---- make: a new template copied from one the model has ---------------------
if (mode == "make")
{
    foreach (var t in templates) before.Add(templateLine(t));
    var fromName = (fromTemplate ?? "").Trim();
    PanelScheduleTemplate source = null;
    if (fromName.Length == 0)
        refused.Add("No template was named to copy " + quoted(wanted) + " from - which one is never guessed. "
            + "The model's panel schedule templates are: " + templateNames() + ".");
    else
    {
        source = exactly(fromName);
        if (source == null) refused.Add(notFound("to copy from", fromName));
    }

    var taken = anyCapitals(wanted);
    if (source != null && taken != null)
    {
        skipped.Add("A panel schedule template is already named " + quoted(taken.Name)
            + " - left as it is, nothing made.");
        source = null;
    }

    if (source != null)
    {
        PanelScheduleType kind = PanelScheduleType.Unknown;
        PanelConfiguration layout = PanelConfiguration.TwoColumnsCircuitsAcross;
        var readable = true;
        try
        {
            kind = source.GetPanelScheduleType();
            layout = source.GetTableData().PanelConfiguration;
        }
        catch (Exception ex)
        {
            readable = false;
            refused.Add("The kind and layout of " + quoted(source.Name) + " could not be read: " + ex.Message
                + " Nothing was made.");
        }
        if (readable && (!PanelScheduleTemplate.IsValidType(kind)
                         || !PanelScheduleTemplate.IsValidPanelConfiguration(kind, layout)))
        {
            readable = false;
            refused.Add(quoted(source.Name) + " is a " + kindOf(kind) + " template laid out as " + layout
                + ", which Revit does not accept for a new template. Nothing was made.");
        }

        PanelScheduleTemplate made = null;
        if (readable)
        {
            try { made = PanelScheduleTemplate.Create(doc, kind, layout, wanted); }
            catch (Exception ex)
            {
                refused.Add("Revit would not make a panel schedule template named " + quoted(wanted) + ": "
                    + ex.Message + " Nothing was made.");
            }
            if (made == null && refused.Count == 0)
                refused.Add("Revit returned no template and raised no error. Nothing was made.");
        }

        if (made != null)
        {
            // The layout, columns and sections of the source - the "from".
            try { made.CopyFrom(doc, source); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("A template " + quoted(wanted) + " was started, but Revit would "
                    + "not copy " + quoted(source.Name) + " into it: " + ex.Message + " The whole change is rolled "
                    + "back, so nothing is kept.");
            }
            if (!string.Equals(made.Name, wanted, StringComparison.Ordinal))
            {
                var copiedName = made.Name;
                try { made.Name = wanted; }
                catch (Exception) { }
                if (!string.Equals(made.Name, wanted, StringComparison.Ordinal))
                    throw new InvalidOperationException("The copy of " + quoted(source.Name) + " reads back named "
                        + quoted(made.Name) + ", not " + quoted(wanted) + ", and could not be renamed. The whole "
                        + "change is rolled back, so nothing is kept.");
                findings.Add("Copying " + quoted(source.Name) + " carried its name over as " + quoted(copiedName)
                    + "; the new template was named back to " + quoted(wanted) + ".");
            }
            var madeKind = PanelScheduleType.Unknown;
            try { madeKind = made.GetPanelScheduleType(); } catch (Exception) { }
            if (madeKind != kind)
                throw new InvalidOperationException("The new template " + quoted(wanted) + " reads back as a "
                    + kindOf(madeKind) + " template, not a " + kindOf(kind) + " one like " + quoted(source.Name)
                    + ". The whole change is rolled back, so nothing is kept.");
            changed = 1;
            findings.Add("Made " + quoted(wanted) + ", a " + kindOf(kind) + " panel schedule template copied from "
                + quoted(source.Name) + ". It is under Panel Schedule Templates in the Project Browser.");
        }
    }

    templates = readTemplates();
    foreach (var t in templates) after.Add(templateLine(t));
    report = "Panel schedule templates before (" + before.Count + "): " + (before.Count == 0 ? "none" : string.Join("; ", before))
        + " | after (" + after.Count + "): " + (after.Count == 0 ? "none" : string.Join("; ", after));
}

// ---- apply: switch panels' schedules to a template ---------------------------
if (mode == "apply")
{
    var template = exactly(wanted);
    if (template == null) refused.Add(notFound("", wanted));

    var panelCategory = new ElementId(BuiltInCategory.OST_ElectricalEquipment);
    Func<Element, string> panelLabel = e =>
    {
        var mark = e.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
        var n = mark != null && mark.HasValue ? mark.AsString() : null;
        return string.IsNullOrEmpty(n)
            ? string.Format("'{0}' (id {1})", e.Name, e.Id)
            : string.Format("panel {0} (id {1})", n, e.Id);
    };

    var panels = new List<Element>();
    var seen = new HashSet<ElementId>();
    if (template != null && elements != null)
    {
        foreach (var e in elements)
        {
            if (e == null || !seen.Add(e.Id)) continue;
            if (e.Category == null || e.Category.Id != panelCategory)
            {
                skipped.Add(panelLabel(e) + " is not electrical equipment, so it has no panel schedule - left as it is.");
                continue;
            }
            panels.Add(e);
        }
    }
    var named = (panelName ?? "").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
    if (template != null && named.Count > 0)
    {
        var equipment = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ElectricalEquipment)
            .WhereElementIsNotElementType().ToElements();
        foreach (var n in named)
        {
            var hits = equipment.Where(e =>
            {
                var p = e.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
                return p != null && p.HasValue && string.Equals(p.AsString(), n, StringComparison.Ordinal);
            }).ToList();
            if (hits.Count == 0)
            {
                var near = equipment.FirstOrDefault(e =>
                {
                    var p = e.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
                    return p != null && p.HasValue && string.Equals(p.AsString(), n, StringComparison.OrdinalIgnoreCase);
                });
                refused.Add("No panel has the Panel Name " + quoted(n)
                    + (near != null ? " - " + panelLabel(near) + " differs only in capitals and is not taken" : "")
                    + ".");
                continue;
            }
            if (hits.Count > 1)
            {
                refused.Add(hits.Count + " panels share the Panel Name " + quoted(n) + " - "
                    + string.Join(", ", hits.Select(panelLabel)) + " - so neither is touched; select the one meant.");
                continue;
            }
            if (seen.Add(hits[0].Id)) panels.Add(hits[0]);
        }
    }
    if (template != null && panels.Count == 0 && refused.Count == 0)
        refused.Add("No panel was selected or named, so nothing was changed. Select the panels, or name them by "
            + "their Panel Name.");

    // Panel -> its schedule view, read fresh.
    Func<Dictionary<ElementId, PanelScheduleView>> readSchedules = () =>
    {
        var map = new Dictionary<ElementId, PanelScheduleView>();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(PanelScheduleView)))
        {
            var v = e as PanelScheduleView;
            if (v == null || v.IsPanelScheduleTemplate()) continue;
            ElementId p = null;
            try { p = v.GetPanel(); } catch (Exception) { p = null; }
            if (p == null || p == ElementId.InvalidElementId || map.ContainsKey(p)) continue;
            map[p] = v;
        }
        return map;
    };
    Func<ElementId, PanelScheduleTemplate> templateOf = id =>
        id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id) as PanelScheduleTemplate;

    var schedules = panels.Count > 0 ? readSchedules() : new Dictionary<ElementId, PanelScheduleView>();
    var lines = new List<string>();
    foreach (var panel in panels)
    {
        var label = panelLabel(panel);
        PanelScheduleView view;
        if (!schedules.TryGetValue(panel.Id, out view))
        {
            skipped.Add(label + " has no panel schedule yet, so there is nothing to switch - make one first "
                + "(Create Panel Schedules), choosing " + quoted(template.Name) + ".");
            continue;
        }
        var currentId = view.GetTemplate();
        var current = templateOf(currentId);
        var was = label + ": schedule " + quoted(view.Name) + " on " + (current == null ? "no template" : quoted(current.Name));
        before.Add(was);

        if (currentId == template.Id)
        {
            skipped.Add(label + " already uses " + quoted(template.Name) + " - left as it is.");
            after.Add(was);
            continue;
        }
        if (current != null)
        {
            var currentKind = PanelScheduleType.Unknown;
            var wantedKind = PanelScheduleType.Unknown;
            try { currentKind = current.GetPanelScheduleType(); wantedKind = template.GetPanelScheduleType(); }
            catch (Exception) { }
            if (currentKind != PanelScheduleType.Unknown && wantedKind != PanelScheduleType.Unknown && currentKind != wantedKind)
            {
                skipped.Add(quoted(template.Name) + " is a " + kindOf(wantedKind) + " template and " + label
                    + "'s schedule is a " + kindOf(currentKind) + " one - Revit takes only a template of the same "
                    + "kind. Left as it is.");
                after.Add(was);
                continue;
            }
        }

        try { view.GenerateInstanceFromTemplate(template.Id); }
        catch (Exception ex)
        {
            refused.Add(label + ": Revit refused to switch its schedule to " + quoted(template.Name) + " - " + ex.Message);
            after.Add(was);
            continue;
        }

        // Read back - from the view again, or the panel's schedule found afresh.
        var now = view.IsValidObject ? view : null;
        if (now == null) readSchedules().TryGetValue(panel.Id, out now);
        var nowId = now == null ? ElementId.InvalidElementId : now.GetTemplate();
        if (now == null || nowId != template.Id)
        {
            var reads = templateOf(nowId);
            throw new InvalidOperationException("Revit took the switch for " + label + " without an error, but its "
                + "schedule reads back on " + (now == null ? "no schedule at all" : reads == null ? "no template"
                : quoted(reads.Name)) + ", not " + quoted(template.Name) + ". The whole change is rolled back, so "
                + "nothing is kept.");
        }
        changed++;
        after.Add(label + ": schedule " + quoted(now.Name) + " on " + quoted(template.Name));
        lines.Add(label + "'s schedule " + quoted(now.Name) + " switched from "
            + (current == null ? "no template" : quoted(current.Name)) + " to " + quoted(template.Name) + ".");
    }
    findings.AddRange(lines);
    report = before.Count == 0 ? "No panel schedule was read, so none was switched."
        : "Before: " + string.Join("; ", before) + " | after: " + string.Join("; ", after);
}

findings.Insert(0, mode == "make"
    ? (changed == 1 ? "1 panel schedule template made." : "No panel schedule template made.")
    : mode == "apply"
        ? changed + " panel schedule(s) switched to " + quoted(wanted) + ", " + skipped.Count + " left as they were, "
          + refused.Count + " refused."
        : "Nothing was changed.");
foreach (var s in skipped) findings.Add(s);
foreach (var r in refused) findings.Add(r);
