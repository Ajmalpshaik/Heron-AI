// NOT STANDALONE. Assumes `doc`, `viewType`, `nameContains`, `placement` and
// `withoutTemplate` are in scope, and leaves `elements`, `skippedTemplates`,
// `skippedNonDrawing`, `skippedInternal`, `matchedButExcluded`, `byType`,
// `placedCount`, `onSheets` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THIS IS NOT ONE COLLECTOR LINE.
//
// "Every View in the document" is not what anyone means by "the views". Revit
// models several things as View that a person never would:
//
//   IsTemplate            a view template is a View. Apply a template to one and
//                         you have edited the template, not a drawing
//   ViewType.Schedule     a schedule is a View - returned ONLY when asked for by
//                         viewType Schedule (version 2), never by default
//   ViewType.DrawingSheet a sheet is a View - FIND_SHEETS is the way to those
//   ProjectBrowser        the browser panes are real View elements in the model
//   SystemBrowser
//   Internal, Undefined   Revit's own housekeeping views
//
// Handing any of those back to an action that duplicates, exports or applies a
// template is not a near miss; it is an edit to the wrong kind of object.
//
// WHY THE EXCLUSIONS ARE COUNTED RATHER THAN JUST DROPPED.
//
// A name filter that matches only a template returns zero either way. But
// "there is no view called Level 3" and "there IS one and it is a template"
// need opposite responses from the user, and a bare zero cannot tell them
// apart. `matchedButExcluded` is that distinction: it counts the things this
// fragment threw away that WOULD have matched the name asked for. A zero
// result with a non-zero matchedButExcluded means "found it, refused it".
//
// `viewType` is matched by NAME rather than as a typed enum, deliberately. A
// caller passing "FloorPlan" needs no enum reference, a release that adds a
// view type needs no change here, and an unrecognised name is REPORTED rather
// than quietly matching nothing at all.
//
// VERSION 2 (2026-11-01) - FIND_SCHEDULES, FIND_UNPLACED_VIEWS AND
// FIND_VIEWS_WITHOUT_TEMPLATE MERGED IN, AND WHAT THEY HAD THAT THIS LACKED:
//
//   SCHEDULES, WHEN ASKED FOR (from FIND_SCHEDULES). viewType "Schedule"
//   returns the real schedules - ViewSchedule, the same class that card
//   collected - and NOTHING ELSE; any other viewType, or none, still counts a
//   schedule in skippedNonDrawing and never returns one. A schedule template
//   is counted in skippedTemplates, and Revit's own internal schedules - named
//   in angle brackets, a revision schedule the common one - in
//   `skippedInternal`. They are recognised by NAME because Revit exposes no
//   property separating them, which is why the count is reported: a user
//   schedule somebody named with a bracket is VISIBLE as excluded, never
//   silently missing.
//
//   `placement` (from FIND_UNPLACED_VIEWS) - "unplaced" keeps the views on no
//   sheet, "placed" the ones on a sheet; left out ("any") keeps both. A view
//   is ON A SHEET when a Viewport shows it, OR a schedule placement
//   (ScheduleSheetInstance) shows it, OR a sheet's own GetAllPlacedViews names
//   it - all three read, because a schedule is not placed through a viewport:
//   read viewports alone and every schedule reports as unplaced, and a
//   tidy-up built on that offers to delete the drawing set's schedules. Not
//   one of the three is trusted alone. `placedCount` is how many views ARE on
//   a sheet. IT REPORTS AND DELETES NOTHING: an unplaced view today is on a
//   sheet tomorrow.
//
//   `withoutTemplate` (from FIND_VIEWS_WITHOUT_TEMPLATE) - true keeps only the
//   views with no view template. Sheets, schedules and legends are left out
//   whatever viewType says: none takes a view template, and all are routinely
//   on sheets - the loudest rows in a report about something they cannot
//   have. The views ON SHEETS come FIRST, and `findings` names each with its
//   sheet number; the ones on no sheet are counted, not listed - a project
//   has hundreds of working views and naming them buries the rows that
//   matter.
//
//   `byType` - how many of the answer are of each view type, on every call.
//   Twenty unplaced 3D views and eight sections are different conversations.
//   `onSheets` - the answer's views that are on a sheet.

var wantedType = (viewType ?? "").Trim();
var wantedName = (nameContains ?? "").Trim();

// "any" is what a left-out placement arrives as (the card's `absent: any`).
var wantedPlacement = (placement ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "");
if (wantedPlacement == "" || wantedPlacement == "any" || wantedPlacement == "all") wantedPlacement = "any";
else if (wantedPlacement == "unplaced" || wantedPlacement == "notonasheet" || wantedPlacement == "notonsheet"
         || wantedPlacement == "onnosheet") wantedPlacement = "unplaced";
else if (wantedPlacement == "placed" || wantedPlacement == "onasheet" || wantedPlacement == "onsheet"
         || wantedPlacement == "onsheets") wantedPlacement = "placed";
else wantedPlacement = "unknown";

bool schedulesAsked = string.Equals(wantedType, "Schedule", StringComparison.OrdinalIgnoreCase);

int skippedTemplates = 0;
int skippedNonDrawing = 0;
int skippedInternal = 0;
int matchedButExcluded = 0;
var byType = new Dictionary<string, int>();
var onSheets = new List<ElementId>();
var findings = new List<string>();

// The view types that are never a drawing a person would act on. A schedule
// among them unless schedules were asked for, above.
var notDrawings = new HashSet<ViewType>
{
    ViewType.Schedule,
    ViewType.DrawingSheet,
    ViewType.ProjectBrowser,
    ViewType.SystemBrowser,
    ViewType.Internal,
    ViewType.Undefined,
};

// view id -> the sheet number showing it, by all three routes. Built once, on
// every call - placedCount, onSheets and byType are answered every time -
// because asking per view would be a pass over every sheet for every view.
var placedOn = new Dictionary<ElementId, string>();
{
    var sheetNumber = new Dictionary<ElementId, string>();
    foreach (var sheet in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>())
    {
        if (sheet == null) continue;
        bool sheetTemplate = false;
        try { sheetTemplate = sheet.IsTemplate; } catch { }
        if (sheetTemplate) continue;
        string number = "";
        try { number = sheet.SheetNumber ?? ""; } catch { }
        sheetNumber[sheet.Id] = number;
        ICollection<ElementId> shown = null;
        try { shown = sheet.GetAllPlacedViews(); } catch { shown = null; }
        if (shown == null) continue;
        foreach (var viewId in shown)
        {
            if (viewId == null || viewId == ElementId.InvalidElementId) continue;
            if (!placedOn.ContainsKey(viewId)) placedOn[viewId] = number;
        }
    }

    Func<ElementId, string> numberOf = sheetId =>
    {
        string number;
        return sheetId != null && sheetNumber.TryGetValue(sheetId, out number) ? number : "";
    };

    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
    {
        var viewport = element as Viewport;
        if (viewport == null) continue;
        try { if (!placedOn.ContainsKey(viewport.ViewId)) placedOn[viewport.ViewId] = numberOf(viewport.SheetId); }
        catch { }
    }

    // A SCHEDULE IS NOT PLACED THROUGH A VIEWPORT.
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)))
    {
        var instance = element as ScheduleSheetInstance;
        if (instance == null) continue;
        try { if (!placedOn.ContainsKey(instance.ScheduleId)) placedOn[instance.ScheduleId] = numberOf(instance.OwnerViewId); }
        catch { }
    }
}
int placedCount = placedOn.Count;

var found = new List<View>();

if (wantedPlacement == "unknown")
{
    // REFUSED, nothing returned: a misspelt placement answering every view
    // would read as "all of these are unplaced".
    findings.Add("placement '" + placement + "' is not one this reads - leave it out, or say "
        + "unplaced or placed. NOTHING WAS RETURNED.");
}
else if (withoutTemplate && schedulesAsked)
{
    // Schedules are left out of the no-template test whatever viewType says.
    findings.Add("A schedule takes no view template here, so 'schedules without a template' "
        + "has no answer - the no-template test leaves sheets, schedules and legends out. "
        + "NOTHING WAS RETURNED.");
}
else
{
    foreach (var v in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        // Name match first, so that an excluded view can still be recognised
        // as having matched what was asked for. Doing this after the
        // exclusions would make matchedButExcluded impossible to compute.
        bool nameMatches = wantedName.Length == 0
            || v.Name.IndexOf(wantedName, StringComparison.OrdinalIgnoreCase) >= 0;

        if (v.IsTemplate)
        {
            skippedTemplates++;
            if (nameMatches) matchedButExcluded++;
            continue;
        }

        if (schedulesAsked && v is ViewSchedule)
        {
            // SCHEDULES, ASKED FOR (from FIND_SCHEDULES). Revit's own internal
            // ones, in angle brackets, are not schedules a person means.
            if (v.Name.StartsWith("<"))
            {
                skippedInternal++;
                if (nameMatches) matchedButExcluded++;
                continue;
            }
        }
        else if (notDrawings.Contains(v.ViewType))
        {
            skippedNonDrawing++;
            if (nameMatches) matchedButExcluded++;
            continue;
        }
        else if (schedulesAsked)
        {
            // Schedules were asked for and this is a drawing.
            continue;
        }

        if (!schedulesAsked && wantedType.Length > 0
            && !string.Equals(v.ViewType.ToString(), wantedType, StringComparison.OrdinalIgnoreCase))
            continue;

        if (!nameMatches) continue;

        bool onASheet = placedOn.ContainsKey(v.Id);
        if (wantedPlacement == "unplaced" && onASheet) continue;
        if (wantedPlacement == "placed" && !onASheet) continue;

        if (withoutTemplate)
        {
            // NONE OF THESE TAKES A VIEW TEMPLATE, and all are routinely on
            // sheets. (Schedules cannot reach here with withoutTemplate; a
            // sheet is in notDrawings; a legend is left out here.)
            if (v is ViewSheet || v is ViewSchedule || v.ViewType == ViewType.Legend) continue;
            ElementId templateId = ElementId.InvalidElementId;
            try { templateId = v.ViewTemplateId; } catch { }
            if (templateId != ElementId.InvalidElementId) continue;
        }

        found.Add(v);
    }
}

// Ordering is by name, and it is a PLAIN string sort: "Level 10" sorts before
// "Level 2". That is Revit's own browser behaviour for equally-named views and
// it is left alone here on purpose - a natural-order sort is a presentation
// choice, and inventing one inside a filter would make two fragments disagree
// about what "the third view" means. WITH withoutTemplate, the views on
// sheets come first (version 2) - they are the drawings being issued.
var ordered = withoutTemplate
    ? found.OrderBy(v => placedOn.ContainsKey(v.Id) ? 0 : 1)
           .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList()
    : found.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();

var elements = ordered.Cast<Element>().ToList();

int working = 0;
foreach (var v in ordered)
{
    string kind = v.ViewType.ToString();
    if (byType.ContainsKey(kind)) byType[kind] = byType[kind] + 1;
    else byType[kind] = 1;

    if (placedOn.ContainsKey(v.Id))
    {
        onSheets.Add(v.Id);
        if (withoutTemplate)
            findings.Add(string.Format("{0}  - ON SHEET {1}, no view template", v.Name, placedOn[v.Id]));
    }
    else working++;
}

if (withoutTemplate && working > 0)
{
    // Counted, not listed. These are working views and a project has hundreds;
    // naming them all buries the rows that matter.
    findings.Add(string.Format(
        "and {0} view(s) with no template that are on no sheet - normal for working "
        + "views, and not listed here because naming them buries the ones above",
        working));
}

if (wantedPlacement == "unplaced")
{
    findings.Add(placedCount == 0
        ? "Nothing in this model is on a sheet, so every view is on no sheet - that is a model "
          + "with no sheets filled, not a list of views to delete."
        : ordered.Count + " view(s) are on no sheet, against " + placedCount + " that are placed. "
          + "Nothing was deleted - an unplaced view today is on a sheet tomorrow."
          + (schedulesAsked ? "" : " Schedules are not in this list; ask with viewType Schedule for them."));
}
