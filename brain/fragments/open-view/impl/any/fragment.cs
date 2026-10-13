// NOT STANDALONE. Assumes `uidoc`, `doc` and `viewName` are in scope; leaves
// `opened`, `viewBefore`, `requestedView`, `matches` and `findings` behind.
//
// NO TRANSACTION, AND IT MUST RUN WITH NONE OPEN. Revit refuses to change the
// active view while a transaction is open: "Cannot change the active view of a
// modifiable document (with a transaction curently open)" - Revit's spelling,
// measured 2026-10-13 on Project1, Revit 2020 (row 5b-456). Every write
// fragment runs inside one, so version 1 was refused for EVERY view through
// revit_change - a sheet and a 3D view alike. The card declares the ambient
// `editScopeFailures` need, and the add-in runs a fragment declaring it inside
// the job's TransactionGroup with no transaction open (D-112, D-115). This
// opens none of its own and never touches that need.
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
    // Declared in here, not at the top: every top-level local comes back
    // in the reply, and a helper is not something to show a modeller.
    requestedView = asked;

    ElementId activeId = null;
    try { activeId = uidoc.ActiveView == null ? null : uidoc.ActiveView.Id; }
    catch { }

    // A view template, or a view kind that is never on screen, is not a view
    // anybody can open - only the rest are candidates.
    Func<View, bool> canOpen = v => !v.IsTemplate
        && v.ViewType != ViewType.Internal
        && v.ViewType != ViewType.ProjectBrowser
        && v.ViewType != ViewType.SystemBrowser
        && v.ViewType != ViewType.Undefined;

    Func<string, List<View>> viewsCalled = text => new FilteredElementCollector(doc)
        .OfClass(typeof(View))
        .Cast<View>()
        .Where(v => v != null && v.IsValidObject
                 && string.Equals(v.Name ?? "", text, StringComparison.OrdinalIgnoreCase))
        .ToList();

    var named = viewsCalled(asked);
    var openable = named.Where(canOpen).ToList();

    // "FloorPlan: Level 1", "DrawingSheet: Ground Floor Plan" - the spelling
    // the rest of the library takes for a view (the add-in's binder), and the
    // only way to say which of two views sharing a name is meant: a sheet named
    // like the plan it carries, a floor plan and a ceiling plan of one level.
    // The PLAIN NAME IS TRIED FIRST and wins when it names one openable view,
    // so a view genuinely called "FloorPlan: L2" stays reachable by its name.
    var mark = asked.IndexOf(':');
    var otherKinds = new List<string>();
    if (openable.Count != 1 && mark > 0)
    {
        var kind = asked.Substring(0, mark).Trim();
        var rest = asked.Substring(mark + 1).Trim();
        if (rest.Length > 0)
        {
            var sameName = viewsCalled(rest);
            var ofKind = sameName.Where(v => canOpen(v)
                && string.Equals(v.ViewType.ToString(), kind, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (ofKind.Count > 0)
            {
                named = sameName;
                openable = ofKind;
            }
            else if (named.Count == 0)
            {
                // The name is there under another kind - say which, rather
                // than "no view called" about a view sitting right there.
                otherKinds = sameName.Where(canOpen)
                    .Select(v => "\"" + v.ViewType + ": " + (v.Name ?? "") + "\"")
                    .Distinct()
                    .ToList();
            }
        }
    }

    foreach (var candidate in named)
    {
        matches.Add((candidate.Name ?? "") + "  -  " + candidate.ViewType
            + (candidate.IsTemplate ? "  [view template, cannot be opened]" : ""));
    }

    // A template shares the list and is the most likely wrong answer to
    // "open the ceiling plan", so it is named as the reason rather than left
    // to fail obscurely inside Revit.
    // WHAT TO TYPE INSTEAD COMES FIRST in the two refusals below that have
    // an answer: a finding reaches the chat cut to its first sixty or so
    // characters, and a hint at the end of one was never seen (row 5b-456).
    if (named.Count == 0 && otherKinds.Count > 0)
    {
        findings.Add("Say " + string.Join(" or ", otherKinds) + " - there is no \"" + asked
            + "\", so NOTHING HAS BEEN SENT TO REVIT and nothing was created.");
    }
    else if (named.Count == 0)
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
        var choices = openable
            .Select(v => "\"" + v.ViewType + ": " + (v.Name ?? "") + "\"")
            .Distinct()
            .ToList();
        findings.Add("Say " + string.Join(" or ", choices) + " - " + openable.Count
            + " openable views are called \"" + asked + "\", so NOTHING HAS BEEN SENT TO REVIT. "
            + "Picking one would be guessing which the user meant.");
    }
    else
    {
        var target = openable[0];

        // BY ELEMENT, NOT BY NAME: a sheet and the plan it carries may share a
        // name, and the plan being on screen does not mean the sheet is.
        if (activeId != null && activeId == target.Id)
        {
            opened = true;
            findings.Add("\"" + (target.Name ?? "") + "\" (" + target.ViewType + ") is already the "
                + "view on screen. Nothing was changed, and nothing needed to be.");
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
                // REVIT'S WORDS FIRST. A finding is shown cut short, and the
                // 2026-10-13 refusals arrived as "Revit refused to open ... so
                // the view on sc..." with the reason gone (row 5b-456, as row
                // 90 found for switch-active-project).
                findings.Add(ex.Message + " - so Revit did not open \"" + (target.Name ?? "")
                    + "\" (" + target.ViewType + ") and the view on screen is UNCHANGED. "
                    + "Nothing in the model was altered."
                    + (doc.IsModifiable
                        ? " A transaction was open around this run, and Revit changes no view "
                          + "inside one: the add-in did not run this with none open."
                        : ""));
            }
        }
    }
}
