// NOT STANDALONE. Assumes `doc`, `viewType` and `nameContains` are in scope, and
// leaves `elements`, `skippedTemplates`, `skippedNonDrawing` and
// `matchedButExcluded` behind.
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
//   ViewType.Schedule     a schedule is a View
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

var wantedType = (viewType ?? "").Trim();
var wantedName = (nameContains ?? "").Trim();

int skippedTemplates = 0;
int skippedNonDrawing = 0;
int matchedButExcluded = 0;

// The view types that are never a drawing a person would act on.
var notDrawings = new HashSet<ViewType>
{
    ViewType.Schedule,
    ViewType.DrawingSheet,
    ViewType.ProjectBrowser,
    ViewType.SystemBrowser,
    ViewType.Internal,
    ViewType.Undefined,
};

var found = new List<Element>();

foreach (var v in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
{
    // Name match first, so that an excluded view can still be recognised as
    // having matched what was asked for. Doing this after the exclusions would
    // make matchedButExcluded impossible to compute.
    bool nameMatches = wantedName.Length == 0
        || v.Name.IndexOf(wantedName, StringComparison.OrdinalIgnoreCase) >= 0;

    if (v.IsTemplate)
    {
        skippedTemplates++;
        if (nameMatches) matchedButExcluded++;
        continue;
    }
    if (notDrawings.Contains(v.ViewType))
    {
        skippedNonDrawing++;
        if (nameMatches) matchedButExcluded++;
        continue;
    }

    if (wantedType.Length > 0
        && !string.Equals(v.ViewType.ToString(), wantedType, StringComparison.OrdinalIgnoreCase))
        continue;

    if (!nameMatches) continue;

    found.Add(v);
}

// Ordering is by name, and it is a PLAIN string sort: "Level 10" sorts before
// "Level 2". That is Revit's own browser behaviour for equally-named views and
// it is left alone here on purpose - a natural-order sort is a presentation
// choice, and inventing one inside a filter would make two fragments disagree
// about what "the third view" means.
var elements = found.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                    .Cast<Element>().ToList();
