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
// with no trace in the family.
//
// READ BACK, ALL OR NOTHING. Each form's setting is read again after it is
// written; one that does not read as asked fails the call and nothing is kept.

var findings = new List<string>();
var changed = new List<string>();
var notAFamily = false;
string refused = null;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

var targets = new List<GenericForm>();
var problems = new List<string>();
bool coarse = false, medium = false, fine = false, top = false, front = false, side = false;

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep" : "form";

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
        var f = element as GenericForm;
        if (f == null)
            problems.Add("\"" + (element.Name ?? element.UniqueId) + "\" (" + element.UniqueId + ") is not a form - "
                + "an extrusion, revolve, blend, sweep or swept blend.");
        else if (!targets.Any(t => t.Id == f.Id)) targets.Add(f);
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

    foreach (var word in (views ?? "").Split(',').Select(squash).Where(w => w.Length > 0))
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

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

if (refused == null)
{
    Func<bool, bool, bool, string> levels = (c, m, f) =>
        string.Join(", ", new[] { c ? "coarse" : null, m ? "medium" : null, f ? "fine" : null }.Where(s => s != null));
    Func<bool, bool, bool, string> kinds = (t, fr, s) =>
        string.Join(", ", new[] { t ? "plan" : null, fr ? "front/back" : null, s ? "left/right" : null }.Where(x => x != null));

    foreach (var f in targets)
    {
        var was = f.GetVisibility();
        var before = was == null ? "unknown" : levels(was.IsShownInCoarse, was.IsShownInMedium, was.IsShownInFine)
            + " | " + kinds(was.IsShownInTopBottom, was.IsShownInFrontBack, was.IsShownInLeftRight);
        try
        {
            var setting = new FamilyElementVisibility(FamilyElementVisibilityType.Model);
            setting.IsShownInCoarse = coarse;
            setting.IsShownInMedium = medium;
            setting.IsShownInFine = fine;
            setting.IsShownInTopBottom = top;
            setting.IsShownInFrontBack = front;
            setting.IsShownInLeftRight = side;
            f.SetVisibility(setting);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the visibility of the " + kindOf(f) + " " + f.UniqueId
                + ": " + ex.Message + " NOTHING from this call was kept.");
        }

        var now = f.GetVisibility();
        if (now == null || now.IsShownInCoarse != coarse || now.IsShownInMedium != medium || now.IsShownInFine != fine
            || now.IsShownInTopBottom != top || now.IsShownInFrontBack != front || now.IsShownInLeftRight != side)
            throw new InvalidOperationException("The " + kindOf(f) + " " + f.UniqueId + " does not read back as asked. "
                + "NOTHING from this call was kept.");

        var after = levels(coarse, medium, fine) + " | " + kinds(top, front, side);
        changed.Add(kindOf(f) + " " + f.UniqueId + ": " + before + " -> " + after);
    }

    findings.Add("Set " + changed.Count + " form(s) to show in " + levels(coarse, medium, fine) + ", in "
        + kinds(top, front, side) + " views: " + string.Join("; ", changed) + ".");
    findings.Add("This is what a PLACED family shows in a project's views; the Family Editor itself still shows "
        + "every form. There is no switch for a 3D view - it shows the form in the detail levels named.");
}

if (refused != null) findings.Add(refused);
