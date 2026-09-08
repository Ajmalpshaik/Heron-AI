// NOT STANDALONE. Assumes `uidoc`, `doc` and `viewNames` are in scope; leaves
// `closed`, `closedNames`, `notOpen`, `keptBack`, `remaining` and `findings`
// behind.
//
// NO TRANSACTION, and it needs none. Nothing in the model changes.
//
// CLOSING A TAB IS NOT DELETING A VIEW. The views stay in the Project Browser.
// DELETE_ELEMENTS is what removes one, and it is MODIFY risk on purpose.
//
// IT WILL NEVER CLOSE THE LAST OPEN TAB. In Revit, closing the last open view of
// a project closes the PROJECT - and a project very likely has unsaved work in
// it. Losing somebody's model to tidy their tabs is not a trade worth making, so
// the request is honoured as far as it safely can be and then stops.

var closedNames = new List<string>();
var notOpen = new List<string>();
var keptBack = new List<string>();
var findings = new List<string>();
var closed = 0;
var remaining = 0;

var wanted = new List<string>();
if (viewNames != null)
{
    foreach (var name in viewNames)
    {
        if (!string.IsNullOrEmpty(name) && name.Trim().Length > 0) wanted.Add(name.Trim());
    }
}

// Tabs of THIS project only. Another open project's tabs are not visible here
// and are not touched.
var tabs = uidoc.GetOpenUIViews();
var openCount = tabs == null ? 0 : tabs.Count;
remaining = openCount;

var activeId = ElementId.InvalidElementId;
try
{
    var active = uidoc.ActiveView;
    if (active != null) activeId = active.Id;
}
catch { }

if (wanted.Count == 0)
{
    findings.Add("No view was named, so NOTHING WAS CLOSED. Name the tabs to close. "
        + openCount + " tab(s) are open in this project.");
}
else if (openCount == 0)
{
    findings.Add("This project has no open view tabs, so there was nothing to close.");
}
else
{
    // Name every open tab once, so a requested name can be answered without
    // asking Revit again per name.
    var byName = new List<KeyValuePair<string, UIView>>();
    foreach (var tab in tabs)
    {
        if (tab == null) continue;
        var view = doc.GetElement(tab.ViewId) as View;
        if (view == null || !view.IsValidObject) continue;
        byName.Add(new KeyValuePair<string, UIView>(view.Name ?? "", tab));
    }

    // Resolve every requested name to its open tab BEFORE closing any of them,
    // so the closing ORDER can be chosen rather than inherited from the order
    // the names were asked in. The order is the whole correctness of the guard
    // below, and it cannot be chosen while already half way through closing.
    var toClose = new List<KeyValuePair<string, UIView>>();

    foreach (var name in wanted)
    {
        var found = byName
            .Where(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (found.Count == 0)
        {
            // Not a failure. A tab already closed and a tab that never existed
            // both land here, and both mean "there is nothing to close".
            notOpen.Add(name);
            continue;
        }

        toClose.AddRange(found);
    }

    // THE ACTIVE TAB IS HANDLED LAST, AND THAT IS WHAT MAKES THE GUARD KEEP THE
    // RIGHT ONE. The floor below keeps back whatever it reaches once a single
    // tab is left, so which tab survives is decided entirely by this ordering.
    // Sorting the active one to the end leaves the user looking at the view they
    // were already in; without it the survivor is whichever name happened to
    // come last in the request, which is a view they did not choose.
    //
    // OrderBy is a STABLE sort, so every other tab keeps the order it was asked
    // in and only the active one moves.
    var ordered = toClose.OrderBy(p => p.Value.ViewId == activeId ? 1 : 0).ToList();

    foreach (var pair in ordered)
    {
        // THE FLOOR. One tab must survive, or Revit closes the project - and
        // that project may hold unsaved work.
        if (remaining <= 1)
        {
            keptBack.Add(pair.Key);
            continue;
        }

        try
        {
            pair.Value.Close();
            closed++;
            remaining--;
            closedNames.Add(pair.Key);
        }
        catch (Exception ex)
        {
            findings.Add("Revit refused to close the tab \"" + pair.Key + "\": " + ex.Message
                + ". It is still open and the view itself is untouched.");
        }
    }

    findings.Add("Closed " + closed + " tab(s). " + remaining + " still open. "
        + "THE VIEWS THEMSELVES ARE UNTOUCHED - every one is still in the Project Browser "
        + "and can be reopened.");
}

if (notOpen.Count > 0)
{
    findings.Add(notOpen.Count + " requested view(s) had no open tab and were left alone: "
        + string.Join(", ", notOpen.Select(n => "\"" + n + "\"").ToArray())
        + ". Already closed and never open look the same from here, and both mean there was "
        + "nothing to do.");
}

if (keptBack.Count > 0)
{
    findings.Add("KEPT " + keptBack.Count + " TAB(S) OPEN ON PURPOSE: "
        + string.Join(", ", keptBack.Select(n => "\"" + n + "\"").ToArray())
        + ". Closing the last open view of a project closes the PROJECT, and this one may hold "
        + "unsaved work. Close it yourself if that is really what you want.");
}
