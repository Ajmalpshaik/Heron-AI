// NOT STANDALONE. Assumes `doc`, `uidoc`, `forms`, `detailLevels` and `views`
// are in scope; leaves `changed`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE FORM'S VISIBILITY SETTINGS - the Family Element Visibility Settings
// dialog in the Family Editor: in which detail levels, and in which kinds of
// project view, a placed family shows this form. The 3D body hidden in a
// coarse plan; a detailed part shown only in Fine.
//
// TWO LISTS, BOTH NAMED IN FULL. Detail levels from coarse, medium and fine, or
// "all"; views from plan (Plan/RCP), front (Front/Back) and left (Left/Right),
// or "all". What is not named is switched OFF - so the call says the whole
// setting, never half of it - and a list naming nothing is refused: a form
// shown in no detail level, or in no view, would vanish from every project view
// with no trace in the family. Version 2: views "unchanged" - what an absent
// or empty list is read as - keeps a SYMBOLIC LINE's kinds of view and sets
// only its detail levels; for a form it is refused, as version 1 refused a
// form with no kinds of view named.
//
// SYMBOLIC LINES TOO (version 2, 2026-11-11, row 5b-1501). A symbolic line
// already drawn - a plan symbol's arrow, the 2D lines a plan shows in place of
// the 3D body - has the same dialog, read and written through its own
// GetVisibility / SetVisibility. ITS SETTING IS NOT A FORM'S: Revit gives a
// symbolic line the ViewSpecific kind of setting, where a form's is Model, so
// the setting is built with the kind the line ALREADY READS, never assumed.
// Its detail levels are set as a form's are - DRAW_FAMILY_SYMBOLIC_LINES sets
// them on the same kind of setting. Whether a ViewSpecific setting keeps the
// kinds of view at all is NOT MEASURED: when they are named they are asked
// for, read back, and anything else REFUSES the call, naming what Revit read,
// rather than guessing (NEEDS-CHECKING Group NF). Name "unchanged" to set only
// the detail levels.
//
// DETAIL LINES, FILLED REGIONS AND NESTED FAMILIES ARE REFUSED BY NAME. Revit's
// API has no visibility settings for any of them on any release 2020 to 2027
// - DetailCurve, FilledRegion and FamilyInstance carry no GetVisibility or
// SetVisibility - so they are set by hand in the Family Editor.
//
// READ BACK, ALL OR NOTHING. Each element's setting is read again after it is
// written; one that does not read as asked fails the call and nothing is kept.

var findings = new List<string>();
var changed = new List<string>();
var notAFamily = false;
string refused = null;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// Forms and symbolic lines, in the order named.
var targets = new List<Element>();
var problems = new List<string>();
bool coarse = false, medium = false, fine = false, top = false, front = false, side = false;
// VERSION 2: "unchanged" keeps each element's own kinds of view.
var keepViews = false;

Func<Element, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep"
    : f is SymbolicCurve ? "symbolic line" : "form";
Func<Element, FamilyElementVisibility> visibilityOf = e =>
{
    try
    {
        var form = e as GenericForm;
        if (form != null) return form.GetVisibility();
        var line = e as SymbolicCurve;
        return line == null ? null : line.GetVisibility();
    }
    catch (Exception) { return null; }
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A form's visibility is "
        + "set inside the family - open it first.";
}
else
{
    var said = (forms ?? "").Trim();
    var named = new List<Element>();
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the forms by their ids.");
        else
            foreach (var id in uidoc.Selection.GetElementIds())
            {
                var element = doc.GetElement(id);
                if (element != null) named.Add(element);
            }
    }
    else
        foreach (var part in said.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var element = doc.GetElement(part);
            if (element == null) problems.Add("No element in this family has the id \"" + part + "\".");
            else named.Add(element);
        }

    foreach (var element in named)
    {
        var label = "\"" + (element.Name ?? element.UniqueId) + "\" (" + element.UniqueId + ")";
        if (element is DetailCurve)
            problems.Add(label + " is a DETAIL line. Revit's API has no visibility settings for a detail line on any "
                + "release 2020 to 2027, so it is not changed here: it shows only in the view it is drawn in. Draw a "
                + "symbolic line instead (DRAW_FAMILY_SYMBOLIC_LINES) where it must follow the detail level.");
        else if (element is FilledRegion)
            problems.Add(label + " is a FILLED REGION. Revit's API has no visibility settings for a filled region on "
                + "any release 2020 to 2027 - set them by hand in the Family Editor.");
        else if (element is FamilyInstance)
            problems.Add(label + " is a NESTED FAMILY. Revit's API has no visibility settings for a nested family on "
                + "any release 2020 to 2027 - set them by hand in the Family Editor (Properties, Visibility/Graphics "
                + "Overrides), or set them on the forms inside the nested family.");
        else if (!(element is GenericForm) && !(element is SymbolicCurve))
            problems.Add(label + " is neither a form - an extrusion, revolve, blend, sweep or swept blend - nor a "
                + "symbolic line.");
        else if (!targets.Any(t => t.Id == element.Id)) targets.Add(element);
    }
    if (problems.Count == 0 && targets.Count == 0)
        problems.Add("No form was named. Name them by the ids the form tools gave back, commas between, or select "
            + "them and say \"selected\".");

    foreach (var word in (detailLevels ?? "").Split(',').Select(squash).Where(w => w.Length > 0))
    {
        if (word == "all") { coarse = true; medium = true; fine = true; }
        else if (word == "coarse") coarse = true;
        else if (word == "medium") medium = true;
        else if (word == "fine") fine = true;
        else problems.Add("\"" + word + "\" is not a detail level. Name coarse, medium and fine, or all.");
    }
    if (!coarse && !medium && !fine)
        problems.Add("No detail level was named. A form shown in none would vanish from every project view - name "
            + "coarse, medium or fine, or all.");

    var viewWords = (views ?? "").Split(',').Select(squash).Where(w => w.Length > 0).ToList();
    if (viewWords.Count == 1 && (viewWords[0] == "unchanged" || viewWords[0] == "asitis" || viewWords[0] == "keep"))
    {
        keepViews = true;
        // VERSION 1'S RULE KEPT FOR A FORM: its kinds of view are named in full.
        // "unchanged" - also what an empty or left-out `views` binds as - is for
        // symbolic lines only, so a form with no kinds of view named is refused
        // exactly as version 1 refused it.
        if (targets.Any(t => t is GenericForm))
            problems.Add("No kind of view was named. A form shown in none would vanish from plans and elevations - "
                + "name plan, front or left, or all. (\"unchanged\" keeps the kinds of view of a SYMBOLIC LINE only.)");
    }
    else
    {
        foreach (var word in viewWords)
        {
            if (word == "all") { top = true; front = true; side = true; }
            else if (word == "plan" || word == "planrcp" || word == "rcp" || word == "top" || word == "topbottom") top = true;
            else if (word == "front" || word == "back" || word == "frontback") front = true;
            else if (word == "left" || word == "right" || word == "leftright" || word == "side") side = true;
            else problems.Add("\"" + word + "\" is not a kind of view. Name plan, front and left, or all.");
        }
        if (!top && !front && !side)
            problems.Add("No kind of view was named. A form shown in none would vanish from plans and elevations - "
                + "name plan, front or left, or all.");
    }

    // A symbolic line's setting is read before anything is written: the kind
    // the new one is built with is the kind it already has.
    foreach (var t in targets.Where(x => x is SymbolicCurve))
        if (visibilityOf(t) == null)
            problems.Add("The symbolic line " + t.UniqueId + " gives no visibility setting to read, so the kind of "
                + "setting to write is not known.");

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

if (refused == null)
{
    Func<bool, bool, bool, string> levels = (c, m, f) =>
        string.Join(", ", new[] { c ? "coarse" : null, m ? "medium" : null, f ? "fine" : null }.Where(s => s != null));
    Func<bool, bool, bool, string> kinds = (t, fr, s) =>
        string.Join(", ", new[] { t ? "plan" : null, fr ? "front/back" : null, s ? "left/right" : null }.Where(x => x != null));
    // A setting's kinds of view - plan, front/back, left/right - or null when
    // Revit will not give them. A ViewSpecific setting may not have them at all,
    // so they are never read outside a guard.
    Func<FamilyElementVisibility, bool[]> viewKindsOf = v =>
    {
        if (v == null) return null;
        try { return new[] { v.IsShownInTopBottom, v.IsShownInFrontBack, v.IsShownInLeftRight }; }
        catch (Exception) { return null; }
    };
    Func<bool[], string> viewsSaid = k => k == null ? "kinds of view not readable"
        : (kinds(k[0], k[1], k[2]).Length == 0 ? "no kind of view" : kinds(k[0], k[1], k[2]));

    var lineKinds = new List<string>();
    foreach (var f in targets)
    {
        var was = visibilityOf(f);
        var before = was == null ? "unknown" : levels(was.IsShownInCoarse, was.IsShownInMedium, was.IsShownInFine)
            + " | " + viewsSaid(viewKindsOf(was));
        var line = f as SymbolicCurve;
        // "unchanged" reaches here for symbolic lines only (refused above for a
        // form): their kinds of view are neither written nor checked.
        var writeViews = line == null || !keepViews;
        // A form's setting is Model, as version 1 wrote it; a symbolic line's is
        // the kind it already reads.
        var kind = line != null ? was.VisibilityType : FamilyElementVisibilityType.Model;
        if (line != null && !lineKinds.Contains(kind.ToString())) lineKinds.Add(kind.ToString());
        try
        {
            var setting = new FamilyElementVisibility(kind);
            setting.IsShownInCoarse = coarse;
            setting.IsShownInMedium = medium;
            setting.IsShownInFine = fine;
            if (writeViews)
            {
                setting.IsShownInTopBottom = top;
                setting.IsShownInFrontBack = front;
                setting.IsShownInLeftRight = side;
            }
            if (line != null) line.SetVisibility(setting);
            else ((GenericForm)f).SetVisibility(setting);
        }
        catch (Exception ex)
        {
            if (line != null && writeViews)
                throw new InvalidOperationException("Revit would not take kinds of view for the symbolic line " + f.UniqueId
                    + " - its setting is of the " + kind + " kind, and whether that kind has kinds of view is not "
                    + "measured. Give views \"unchanged\" to set only its detail levels. NOTHING from this call was kept.");
            throw new InvalidOperationException("Revit refused the visibility of the " + kindOf(f) + " " + f.UniqueId
                + (line != null ? " (its setting is of the " + kind + " kind)" : "") + ": " + ex.Message
                + " NOTHING from this call was kept.");
        }

        var now = visibilityOf(f);
        if (now == null || now.IsShownInCoarse != coarse || now.IsShownInMedium != medium || now.IsShownInFine != fine)
            throw new InvalidOperationException("The " + kindOf(f) + " " + f.UniqueId + " does not read back at the detail "
                + "levels asked" + (now == null ? "" : " - it reads " + levels(now.IsShownInCoarse, now.IsShownInMedium,
                    now.IsShownInFine)) + ". NOTHING from this call was kept.");
        var readViews = viewKindsOf(now);
        if (writeViews && (readViews == null || readViews[0] != top || readViews[1] != front || readViews[2] != side))
            throw new InvalidOperationException("The " + kindOf(f) + " " + f.UniqueId + " reads " + (readViews == null
                    ? "its kinds of view as not readable" : "its kinds of view as \"" + viewsSaid(readViews) + "\"")
                + ", not \"" + kinds(top, front, side) + "\" as asked"
                + (line != null ? ". A symbolic line's setting is of the " + kind + " kind, and whether that kind keeps "
                    + "kinds of view is not measured, so this is refused rather than guessed - give views \"unchanged\" "
                    + "to set only its detail levels" : "") + ". NOTHING from this call was kept.");

        var after = levels(coarse, medium, fine) + " | " + viewsSaid(readViews);
        changed.Add(kindOf(f) + " " + f.UniqueId + ": " + before + " -> " + after);
    }

    findings.Add("Set " + changed.Count + " element(s) to show in " + levels(coarse, medium, fine)
        + (keepViews ? ", their kinds of view kept as they were" : ", in " + kinds(top, front, side) + " views")
        + ": " + string.Join("; ", changed) + ".");
    findings.Add("This is what a PLACED family shows in a project's views; the Family Editor itself still shows "
        + "every form. There is no switch for a 3D view - it shows the form in the detail levels named.");
    if (lineKinds.Count > 0)
        findings.Add("The symbolic lines' settings are of the " + string.Join(" and ", lineKinds) + " kind, as Revit read "
            + "them. A symbolic line shows only in views parallel to the plane it is drawn on, never in 3D.");
}

if (refused != null) findings.Add(refused);
