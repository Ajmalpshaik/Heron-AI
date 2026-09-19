// NOT STANDALONE. Assumes `doc`, `view` and `onSheetsOnly` are in scope, and
// leaves `hidden`, `shown`, `unmatched` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE MARK IS NOT THE SECTION.
//
// What gets hidden in a plan is the MARKER element. The section view itself is
// a view and is not touched - hiding a view is a different and much worse
// thing. They share a name, which is confusing and is also the only link
// between them: there is no property on the marker that names its view.
//
// UNHIDE EVERYTHING FIRST, THEN HIDE.
//
// Without the reset, a mark hidden on an earlier run stays hidden after its
// section is finally placed on a sheet. The tool would only ever hide, never
// restore, and a drawing set would quietly lose marks over months with nobody
// able to say when it started.
//
// THIS IS ONE VIEW'S OVERRIDE AND NOTHING ELSE.
//
// It does not touch category visibility, view filters or worksets. A mark
// still invisible after this has been hidden by one of those, and unhiding
// here will never bring it back - which is worth knowing before an hour goes
// into it.

int hidden = 0;
int shown = 0;
var unmatched = new List<ElementId>();
string refused = "";

// A VIEW IS ON A SHEET WHEN A VIEWPORT POINTS AT IT, AND THAT IS THE ONLY
// READING THAT IS TRUE.
//
// This read was `BuiltInParameter.VIEWER_SHEET_NUMBER` is not empty, with a
// comment claiming it "gets the empty case right". IT DOES NOT. Revit fills
// that parameter with its placeholder `---` for a view that is on no sheet,
// and `---` is not empty - so every view read as PLACED and every mark was
// skipped. Measured 2026-09-13 on `1 - Mech` of `Project1 work_ajmal.al`:
// `markers 5`, `unmatched 0`, `hidden 0`, `shown 0` - all five marks found,
// all five matched to their views, and none acted on, in a model where
// `list-sheets` reported 0 sheets. FRAGMENT-ISSUES row 23.
//
// A viewport is Revit's own record of the placement rather than a string it
// formats for a schedule, so there is no placeholder to recognise and nothing
// to translate. The empty case comes out right for free: no viewport, not on
// a sheet.
var placedViewIds = new HashSet<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
{
    var viewport = element as Viewport;
    if (viewport == null) continue;
    try { placedViewIds.Add(viewport.ViewId); } catch { }
}

// Every section and elevation view, by name, with whether it is on a sheet.
// Revit keeps view names unique, so the name is a safe key.
var onSheet = new Dictionary<string, bool>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSection)))
{
    var section = element as ViewSection;
    if (section == null) continue;

    string name = "";
    try { name = section.Name ?? ""; } catch { }
    if (name.Length == 0) continue;

    onSheet[name] = placedViewIds.Contains(section.Id);
}

var markers = new List<Element>();
try
{
    foreach (var element in new FilteredElementCollector(doc, view.Id)
                 .OfCategory(BuiltInCategory.OST_Viewers)
                 .WhereElementIsNotElementType())
        markers.Add(element);
}
catch { refused = "The marks in '" + view.Name + "' could not be read."; }

if (refused.Length == 0)
{
    // The reset. Only elements that CAN be unhidden are passed, because the
    // call refuses a set containing anything it cannot act on.
    var toShow = new List<ElementId>();
    foreach (var marker in markers)
    {
        bool isHidden = false;
        try { isHidden = marker.IsHidden(view); } catch { }
        if (isHidden && marker.CanBeHidden(view)) toShow.Add(marker.Id);
    }

    if (toShow.Count > 0)
    {
        try { view.UnhideElements(toShow); shown = toShow.Count; }
        catch { refused = "Revit refused to unhide the marks in '" + view.Name + "'."; }
    }

    if (refused.Length == 0 && onSheetsOnly)
    {
        var toHide = new List<ElementId>();
        foreach (var marker in markers)
        {
            string name = "";
            try { name = marker.Name ?? ""; } catch { }

            if (name.Length == 0 || !onSheet.ContainsKey(name))
            {
                // An elevation marker, or a mark whose view could not be
                // matched by name. Left visible rather than hidden on a guess:
                // a missing mark on an issued drawing is the worse mistake.
                unmatched.Add(marker.Id);
                continue;
            }

            if (onSheet[name]) continue;
            if (!marker.CanBeHidden(view)) { unmatched.Add(marker.Id); continue; }
            toHide.Add(marker.Id);
        }

        if (toHide.Count > 0)
        {
            try { view.HideElements(toHide); hidden = toHide.Count; }
            catch { refused = "Revit refused to hide the marks in '" + view.Name + "'."; }
        }
    }
}
