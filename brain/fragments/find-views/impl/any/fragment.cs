// NOT STANDALONE. Assumes `doc`, `viewType`, `nameContains`, `placement` and
// `withoutTemplate` are in scope, and leaves `elements`, `skippedTemplates`,
// `skippedNonDrawing`, `skippedInternal`, `skippedOtherType` (version 3),
// `matchedButExcluded`, `byType`, `placedCount`, `onSheets` and `findings`
// behind.
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
//
// VERSION 3 (2026-11-07, row 5b-1474) - `viewType` IS READ THE WAY A MODELLER
// TYPES IT, AND EVERY VIEW PASSED OVER IS ACCOUNTED FOR BY REASON.
//
//   Version 2 compared `viewType` with the enum's own spelling and nothing
//   else, so "Floor Plan" - the Project Browser's words - matched no view,
//   because the enum says FloorPlan; and "any" was taken as the name of a view
//   type, so it matched none either. On a real as-built model (2026-11-07,
//   Revit 2020) a floor plan that WAS on a sheet came back 0 both ways, while
//   this card's own placedOn listed it - the view was never excluded, it was
//   compared with a type that does not exist. And the promise above, that an
//   unrecognised name is REPORTED, was never kept: it matched nothing in
//   silence. Not a dependent view (it is still ViewType.FloorPlan) and not the
//   parentheses in the name (IndexOf takes them as text).
//
//   So: spaces, hyphens and capitals are ignored ("Floor Plan", "floor-plan",
//   "FLOORPLAN"), a plural is read as its singular ("sections"), the browser's
//   words map to the enum ("3D View" -> ThreeD, "Reflected Ceiling Plan" ->
//   CeilingPlan, "Structural Plan" -> EngineeringPlan, "Drafting View" ->
//   DraftingView), and "", "any" or "all" is no type at all. A word that is
//   still none of them is REFUSED, NOTHING RETURNED, with the words it takes.
//
//   `skippedOtherType` counts the drawings passed over for being another view
//   type, and the answer always says how many views were passed over and why,
//   each reason by name - templates, each kind of non-drawing view by its
//   type, another view type, the placement asked, a template held. When the
//   answer is empty, the views whose NAME matched are named with the reason
//   each was passed over, so "found it and refused it" names what it refused.

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

// VERSION 3 - THE VIEW TYPE AS A MODELLER TYPES IT. Letters and digits only,
// lower case: "Floor Plan", "floor-plan" and "FloorPlan" are one word.
Func<string, string> squash = s => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray())
    .ToLowerInvariant();
var typeWords = new Dictionary<string, string>();
foreach (var enumName in Enum.GetNames(typeof(ViewType))) typeWords[squash(enumName)] = enumName;
// The Project Browser's words, where they are not the enum's.
var browserWords = new Dictionary<string, string>
{
    { "3d", "ThreeD" }, { "3dview", "ThreeD" }, { "threedview", "ThreeD" },
    { "reflectedceilingplan", "CeilingPlan" }, { "rcp", "CeilingPlan" },
    { "structuralplan", "EngineeringPlan" },
    { "drafting", "DraftingView" },
    { "sectionview", "Section" }, { "elevationview", "Elevation" }, { "detailview", "Detail" },
    { "schedulequantities", "Schedule" }, { "schedulesquantities", "Schedule" },
};
foreach (var pair in browserWords) if (!typeWords.ContainsKey(pair.Key)) typeWords[pair.Key] = pair.Value;

var typeKey = squash(wantedType);
string resolvedType = null;     // the enum's name, or null for every type
bool typeUnknown = false;
if (typeKey == "" || typeKey == "any" || typeKey == "all" || typeKey == "allviews"
    || typeKey == "anyview" || typeKey == "views" || typeKey == "view")
    resolvedType = null;
else if (typeWords.ContainsKey(typeKey)) resolvedType = typeWords[typeKey];
else if (typeKey.EndsWith("s") && typeWords.ContainsKey(typeKey.Substring(0, typeKey.Length - 1)))
    resolvedType = typeWords[typeKey.Substring(0, typeKey.Length - 1)];
else typeUnknown = true;

bool schedulesAsked = resolvedType == "Schedule";

int skippedTemplates = 0;
int skippedNonDrawing = 0;
int skippedInternal = 0;
int matchedButExcluded = 0;
int skippedOtherType = 0;
// VERSION 3 - why each view was passed over, for the answer to say by name.
int skippedByPlacement = 0;
int skippedHasTemplate = 0;
int skippedTakesNoTemplate = 0;
var nonDrawingByType = new SortedDictionary<string, int>();
var refusedByName = new List<string>();   // views whose NAME matched, and why each was passed over
Action<View, string> refusedOne = (view, why) =>
{
    if (refusedByName.Count < 10) refusedByName.Add("'" + view.Name + "' (id " + view.Id + ") - " + why);
    else if (refusedByName.Count == 10) refusedByName.Add("and more");
};
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

if (typeUnknown)
{
    // VERSION 3 - REFUSED, nothing returned. Version 2 matched nothing in
    // silence, which read as "this model has no such view".
    findings.Add("viewType '" + viewType + "' is not a view type this reads, so NOTHING WAS RETURNED. "
        + "Say it as the Project Browser does - Floor Plan, Ceiling Plan, Structural Plan, Area Plan, "
        + "3D View, Section, Elevation, Detail View, Drafting View, Legend, Schedule - or leave it out, "
        + "or say any, for every view type.");
}
else if (wantedPlacement == "unknown")
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
            if (nameMatches) { matchedButExcluded++; refusedOne(v, "a view template"); }
            continue;
        }

        if (schedulesAsked && v is ViewSchedule)
        {
            // SCHEDULES, ASKED FOR (from FIND_SCHEDULES). Revit's own internal
            // ones, in angle brackets, are not schedules a person means.
            if (v.Name.StartsWith("<"))
            {
                skippedInternal++;
                if (nameMatches) { matchedButExcluded++; refusedOne(v, "one of Revit's internal schedules"); }
                continue;
            }
        }
        else if (notDrawings.Contains(v.ViewType))
        {
            skippedNonDrawing++;
            var kindName = v.ViewType.ToString();
            nonDrawingByType[kindName] = (nonDrawingByType.ContainsKey(kindName) ? nonDrawingByType[kindName] : 0) + 1;
            if (nameMatches)
            {
                matchedButExcluded++;
                refusedOne(v, "a " + kindName + ", not a drawing"
                    + (v.ViewType == ViewType.Schedule ? " - ask with viewType Schedule" : ""));
            }
            continue;
        }
        else if (schedulesAsked)
        {
            // Schedules were asked for and this is a drawing.
            skippedOtherType++;
            if (nameMatches) refusedOne(v, "a " + v.ViewType + ", and Schedule was asked");
            continue;
        }

        // VERSION 3: against the type READ, never the raw word - see the header.
        if (!schedulesAsked && resolvedType != null
            && !string.Equals(v.ViewType.ToString(), resolvedType, StringComparison.Ordinal))
        {
            skippedOtherType++;
            if (nameMatches) refusedOne(v, "a " + v.ViewType + ", and " + resolvedType + " was asked");
            continue;
        }

        if (!nameMatches) continue;

        bool onASheet = placedOn.ContainsKey(v.Id);
        if ((wantedPlacement == "unplaced" && onASheet) || (wantedPlacement == "placed" && !onASheet))
        {
            skippedByPlacement++;
            refusedOne(v, onASheet ? "on sheet " + placedOn[v.Id] + ", and unplaced was asked"
                                   : "on no sheet, and placed was asked");
            continue;
        }

        if (withoutTemplate)
        {
            // NONE OF THESE TAKES A VIEW TEMPLATE, and all are routinely on
            // sheets. (Schedules cannot reach here with withoutTemplate; a
            // sheet is in notDrawings; a legend is left out here.)
            if (v is ViewSheet || v is ViewSchedule || v.ViewType == ViewType.Legend)
            {
                skippedTakesNoTemplate++;
                refusedOne(v, "a " + v.ViewType + ", which takes no view template");
                continue;
            }
            ElementId templateId = ElementId.InvalidElementId;
            try { templateId = v.ViewTemplateId; } catch { }
            if (templateId != ElementId.InvalidElementId)
            {
                skippedHasTemplate++;
                refusedOne(v, "it has a view template");
                continue;
            }
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

// VERSION 3 - EVERY VIEW PASSED OVER, BY REASON, ON EVERY CALL THAT RAN. A
// bare zero cannot say whether the view is missing or was set aside, and
// which reason set it aside changes what the modeller asks next.
if (!typeUnknown && wantedPlacement != "unknown" && !(withoutTemplate && schedulesAsked))
{
    var reasons = new List<string>();
    if (skippedTemplates > 0) reasons.Add(skippedTemplates + " view template(s)");
    if (skippedNonDrawing > 0)
        reasons.Add(skippedNonDrawing + " that are not drawings ("
            + string.Join(", ", nonDrawingByType.Select(p => p.Key + " " + p.Value).ToArray()) + ")");
    if (skippedInternal > 0) reasons.Add(skippedInternal + " of Revit's internal schedules");
    if (skippedOtherType > 0)
        reasons.Add(skippedOtherType + " of another view type than "
            + (schedulesAsked ? "Schedule" : resolvedType ?? "asked"));
    if (skippedByPlacement > 0)
        reasons.Add(skippedByPlacement + " matching the name but " + (wantedPlacement == "unplaced"
            ? "on a sheet" : "on no sheet"));
    if (skippedHasTemplate > 0) reasons.Add(skippedHasTemplate + " that have a view template");
    if (skippedTakesNoTemplate > 0) reasons.Add(skippedTakesNoTemplate + " legend(s) that take no view template");

    findings.Add("Read viewType as " + (resolvedType ?? "any view type")
        + (wantedName.Length > 0 ? ", name containing '" + wantedName + "'" : "")
        + "; returned " + ordered.Count + "; passed over "
        + (reasons.Count == 0 ? "nothing" : string.Join("; ", reasons.ToArray())) + ".");

    if (ordered.Count == 0 && wantedName.Length > 0)
    {
        findings.Add(refusedByName.Count == 0
            ? "No view, template or schedule in this model has '" + wantedName + "' in its name."
            : "The name matched, but each was passed over: " + string.Join("; ", refusedByName.ToArray()));
    }
}
