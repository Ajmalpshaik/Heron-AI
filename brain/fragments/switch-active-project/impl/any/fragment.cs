// NOT STANDALONE. Assumes `uidoc`, `app`, `target` and `viewName` are in scope;
// leaves `switched`, `activeBefore`, `requestedTitle`, `viewUsed`, `candidates`
// and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing in any model changes - this acts on
// the session.
//
// THE SWITCH IS REQUESTED, NOT CONFIRMED. RequestViewChange is performed by Revit
// when the current API context ends, which is after this fragment returns. So
// nothing here can read back the new active document: doing so would report the
// state from BEFORE the switch and call it proof. Verification is a separate read
// afterwards - REPORT_OPEN_DOCUMENTS.
//
// A TITLE IS NOT AN IDENTIFIER. Two open projects can be called "Project1", so
// the path is matched first and two matches is a refusal rather than a guess.

var candidates = new List<string>();
var findings = new List<string>();
var switched = false;
var activeBefore = "";
var requestedTitle = "";
var viewUsed = "";

var current = uidoc.Document;
if (current != null && current.IsValidObject) activeBefore = current.Title;

var open = new List<Document>();
foreach (Document candidate in app.Documents)
{
    if (candidate == null || !candidate.IsValidObject) continue;
    if (candidate.IsLinked || candidate.IsFamilyDocument) continue;   // no tab to switch to
    open.Add(candidate);
    candidates.Add((candidate.Title ?? "")
        + "  -  "
        + (string.IsNullOrEmpty(candidate.PathName) ? "(never saved)" : candidate.PathName));
}

var wanted = (target ?? "").Trim();

if (wanted.Length == 0)
{
    findings.Add("No project was named, so NOTHING HAS BEEN SENT TO REVIT. "
        + "Name the project by its full path, or by its title when only one has that title. "
        + open.Count + " project(s) are open.");
}
else
{
    // Path first. It is the only identifier that is always unique, and matching
    // it first is what stops two same-named projects deciding the answer.
    var matches = open
        .Where(d => !string.IsNullOrEmpty(d.PathName)
                 && string.Equals(d.PathName, wanted, StringComparison.OrdinalIgnoreCase))
        .ToList();

    var matchedOn = "path";

    if (matches.Count == 0)
    {
        matches = open
            .Where(d => string.Equals(d.Title ?? "", wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();
        matchedOn = "title";
    }

    if (matches.Count == 0)
    {
        findings.Add("No open project matches \"" + wanted + "\", so NOTHING HAS BEEN SENT TO REVIT. "
            + "Only projects already open are candidates - this never opens a file from disk. "
            + "The open ones are listed in `candidates`.");
    }
    else if (matches.Count > 1)
    {
        findings.Add(matches.Count + " open projects match \"" + wanted + "\" by " + matchedOn
            + ", so NOTHING HAS BEEN SENT TO REVIT. Picking one would be choosing somebody's model "
            + "for them. Name the full path instead - the candidates are listed with theirs.");
    }
    else
    {
        var wantedDoc = matches[0];
        requestedTitle = wantedDoc.Title ?? "";

        if (ReferenceEquals(wantedDoc, current))
        {
            switched = true;
            findings.Add("\"" + requestedTitle + "\" is already the active project. "
                + "Nothing was changed, and nothing needed to be.");
        }
        else
        {
            // RequestViewChange needs a view, and a view belongs to one document -
            // so a view in the target project is what brings that project forward.
            View chosen = null;

            var openable = new FilteredElementCollector(wantedDoc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v != null && v.IsValidObject && !v.IsTemplate
                         && v.ViewType != ViewType.Internal
                         && v.ViewType != ViewType.ProjectBrowser
                         && v.ViewType != ViewType.SystemBrowser
                         && v.ViewType != ViewType.Undefined)
                .ToList();

            var asked = (viewName ?? "").Trim();

            if (asked.Length > 0)
            {
                chosen = openable
                    .Where(v => string.Equals(v.Name ?? "", asked, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(v => v.Name)
                    .FirstOrDefault();

                if (chosen == null)
                {
                    findings.Add("\"" + requestedTitle + "\" has no openable view called \"" + asked
                        + "\", so NOTHING HAS BEEN SENT TO REVIT. Leave the view name empty to let "
                        + "this choose one, or name a view that exists in that project.");
                }
            }
            else if (openable.Count > 0)
            {
                // AN ASSUMPTION IS NOT A CHOICE - so the view is picked in a
                // stated order and then REPORTED, rather than picked silently.
                // Plans first because that is where a modeller expects to land.
                var order = new List<ViewType>
                {
                    ViewType.FloorPlan, ViewType.EngineeringPlan, ViewType.CeilingPlan,
                    ViewType.ThreeD, ViewType.Section, ViewType.Elevation
                };

                foreach (var type in order)
                {
                    chosen = openable.Where(v => v.ViewType == type).OrderBy(v => v.Name).FirstOrDefault();
                    if (chosen != null) break;
                }

                if (chosen == null) chosen = openable.OrderBy(v => v.Name).FirstOrDefault();
            }

            if (chosen == null && asked.Length == 0)
            {
                findings.Add("\"" + requestedTitle + "\" has no view that can be opened, so NOTHING "
                    + "HAS BEEN SENT TO REVIT. A project with no openable view cannot be switched to, "
                    + "because switching IS opening one of its views.");
            }
            else if (chosen != null)
            {
                try
                {
                    uidoc.RequestViewChange(chosen);
                    switched = true;
                    viewUsed = chosen.Name ?? "";

                    findings.Add("Asked Revit to switch from \"" + activeBefore + "\" to \""
                        + requestedTitle + "\", landing on the view \"" + viewUsed + "\""
                        + (asked.Length > 0 ? " as named." : " - CHOSEN BY THIS FRAGMENT, not by you.")
                        + " Matched on " + matchedOn + ".");

                    findings.Add("THE SWITCH IS REQUESTED, NOT YET DONE. Revit performs it when this "
                        + "operation finishes, so nothing here can confirm it landed. Read the open "
                        + "documents again to check before writing anything into that project.");
                }
                catch (Exception ex)
                {
                    findings.Add("Revit refused to change to the view \"" + (chosen.Name ?? "")
                        + "\" in \"" + requestedTitle + "\", so the active project is UNCHANGED: "
                        + ex.Message + ". Nothing in any model was altered.");
                }
            }
        }
    }
}
