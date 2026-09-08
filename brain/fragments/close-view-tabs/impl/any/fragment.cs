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

        foreach (var pair in found)
        {
            // THE GUARD. One tab must survive, or Revit closes the project.
            if (remaining <= 1)
            {
                keptBack.Add(pair.Key);
                continue;
            }

            // Keep the active tab back in preference to any other, so the user
            // is left looking at the view they were already in.
            if (pair.Value.ViewId == activeId && remaining <= wanted.Count && remaining <= 2)
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
