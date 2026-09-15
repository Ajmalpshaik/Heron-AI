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

        // THE SAME DOCUMENT IS NOT THE SAME OBJECT. `app.Documents` and
        // `uidoc.Document` can hand back DIFFERENT managed wrappers around one
        // open file, so ReferenceEquals answers "no" for the model you are
        // standing in - and the already-active case then fell through to
        // RequestViewChange, which Revit refuses because the view is already
        // the one on screen. That is the refusal row 86 recorded in BOTH
        // directions. Compared by what identifies the file instead: its path
        // when it has one, and Equals when it has never been saved.
        var isCurrent = current != null && current.IsValidObject
            && (!string.IsNullOrEmpty(wantedDoc.PathName)
                    ? string.Equals(wantedDoc.PathName, current.PathName,
                                    StringComparison.OrdinalIgnoreCase)
                    : wantedDoc.Equals(current));

        if (isCurrent)
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
                    // THE REQUEST BELONGS TO THE TARGET'S UIDocument, NOT THIS
                    // ONE. `uidoc` wraps the document in FRONT, and its
                    // RequestViewChange takes a view of its OWN document - so
                    // handing it a view from the project being switched TO is
                    // refused every time, which is the other half of row 86.
                    // A UIDocument constructed for the target is the one that
                    // can be asked, and constructing one opens nothing.
                    new UIDocument(wantedDoc).RequestViewChange(chosen);
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
                    // REVIT'S OWN WORDS COME FIRST. The reply renders a list
                    // item short, so a refusal that opens with sixty
                    // characters of our own context arrives with the only
                    // sentence that matters cut off the end - which is how
                    // this one stayed unexplained from 2026-09-10 to today.
                    // REVIT'S OWN WORDS COME FIRST, AND THAT IS WHAT FINALLY
                    // EXPLAINED THIS. The reply renders a list item short, so a
                    // refusal opening with sixty characters of our own context
                    // arrives with the only sentence that matters cut off the
                    // end - and this one read as an unexplained "Revit refused"
                    // from 2026-09-10 until the order was reversed on
                    // 2026-09-14. What it actually says is:
                    //
                    //   "Changing the active view is not applicable to
                    //    inactive documents."
                    //
                    // RequestViewChange changes the view WITHIN the active
                    // document. It cannot bring another one forward, so the
                    // premise in this fragment's header - that changing the
                    // view is how Revit offers the switch - does not hold
                    // across documents.
                    findings.Add(ex.Message + " [" + ex.GetType().Name + "]"
                        + " -- asked for the view \"" + (chosen.Name ?? "")
                        + "\" in \"" + requestedTitle + "\". The active project is UNCHANGED and "
                        + "nothing in any model was altered.");
                }
            }
        }
    }
}
