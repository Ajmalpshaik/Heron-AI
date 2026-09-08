// NOT STANDALONE. Assumes `uidoc`, `doc` and `viewName` are in scope; leaves
// `opened`, `viewBefore`, `requestedView`, `matches` and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing in the model changes.
//
// THE CHANGE IS REQUESTED, NOT CONFIRMED. RequestViewChange is performed by Revit
// when the current API context ends - after this fragment returns. Reading
// ActiveView back here would report the view from BEFORE the change and call it
// proof, so it is not read back at all.
//
// IT OPENS, IT NEVER CREATES. An unknown name is a refusal: creating a view for
// every typo would leave strays in somebody's project.

var matches = new List<string>();
var findings = new List<string>();
var opened = false;
var viewBefore = "";
var requestedView = "";

try
{
    var active = uidoc.ActiveView;
    if (active != null) viewBefore = active.Name ?? "";
}
catch { }

var asked = (viewName ?? "").Trim();

if (asked.Length == 0)
{
    findings.Add("No view was named, so NOTHING HAS BEEN SENT TO REVIT. "
        + "Name the view to open - FIND_VIEWS lists what this project has.");
}
else
{
    requestedView = asked;

    var named = new FilteredElementCollector(doc)
        .OfClass(typeof(View))
        .Cast<View>()
        .Where(v => v != null && v.IsValidObject
                 && string.Equals(v.Name ?? "", asked, StringComparison.OrdinalIgnoreCase))
        .ToList();

    foreach (var candidate in named)
    {
        matches.Add((candidate.Name ?? "") + "  -  " + candidate.ViewType
            + (candidate.IsTemplate ? "  [view template, cannot be opened]" : ""));
    }

    // A template shares the list and is the most likely wrong answer to
    // "open the ceiling plan", so it is named as the reason rather than left
    // to fail obscurely inside Revit.
    var openable = named
        .Where(v => !v.IsTemplate
                 && v.ViewType != ViewType.Internal
                 && v.ViewType != ViewType.ProjectBrowser
                 && v.ViewType != ViewType.SystemBrowser
                 && v.ViewType != ViewType.Undefined)
        .ToList();

    if (named.Count == 0)
    {
        findings.Add("This project has no view called \"" + asked + "\", so NOTHING HAS BEEN SENT "
            + "TO REVIT. Nothing was created either - this opens an existing view and never makes "
            + "one. FIND_VIEWS lists what is there.");
    }
    else if (openable.Count == 0)
    {
        findings.Add("\"" + asked + "\" names " + named.Count + " thing(s) in this project and none "
            + "of them can be opened - a view template is not a view you can put on screen. "
            + "NOTHING HAS BEEN SENT TO REVIT.");
    }
    else if (openable.Count > 1)
    {
        findings.Add(openable.Count + " openable views are called \"" + asked + "\", so NOTHING HAS "
            + "BEEN SENT TO REVIT. Picking one would be guessing which the user meant - they are "
            + "listed in `matches` with their types.");
    }
    else
    {
        var target = openable[0];

        if (!string.IsNullOrEmpty(viewBefore)
            && string.Equals(viewBefore, target.Name ?? "", StringComparison.OrdinalIgnoreCase))
        {
            opened = true;
            findings.Add("\"" + requestedView + "\" is already the view on screen. "
                + "Nothing was changed, and nothing needed to be.");
        }
        else
        {
            try
            {
                uidoc.RequestViewChange(target);
                opened = true;

                findings.Add("Asked Revit to open \"" + (target.Name ?? "") + "\" ("
                    + target.ViewType + ")"
                    + (string.IsNullOrEmpty(viewBefore) ? "." : ", leaving \"" + viewBefore + "\"."));

                findings.Add("THE CHANGE IS REQUESTED, NOT YET DONE. Revit performs it when this "
                    + "operation finishes, so nothing here can confirm what is on screen now.");
            }
            catch (Exception ex)
            {
                findings.Add("Revit refused to open \"" + (target.Name ?? "") + "\", so the view on "
                    + "screen is UNCHANGED: " + ex.Message + ". Nothing in the model was altered.");
            }
        }
    }
}
