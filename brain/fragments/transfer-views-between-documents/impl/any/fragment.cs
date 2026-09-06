// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`, `viewKind` and
// `nameContains` are in scope, and leaves `copied`, `clashed`, `weakened` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE COPY TAKES THE SHELL, NOT THE CONTENTS.
//
// A drafting view or a legend arrives with the right name, the right type and
// nothing drawn in it. Its contents are a SECOND copy - the elements that view
// owns, into the new view. Miss that and the transfer reports success and
// delivers blank views, which is discovered on a sheet.
//
// Schedules and view templates are definitions rather than drawn content, so
// one copy is the whole job for them.
//
// A DUPLICATE TYPE NAME CANNOT BE HANDLED FROM HERE.
//
// Revit resolves a clash through a handler that has to be a CLASS, and a
// fragment is compiled inside a single method - a class cannot be declared in
// one. So no handler is set: each view is copied on its own, a clash names
// that view, and the rest go through. A project with many clashes wants
// Revit's own Transfer Project Standards. This is a job the fragment shape
// structurally cannot finish, and saying so beats a workaround that half
// works.

var copied = new List<string>();
var clashed = new List<string>();
var weakened = new List<string>();
string refused = "";

string wantedTitle = (sourceDocumentTitle ?? "").Trim();
string kind = (viewKind ?? "").Trim().ToLowerInvariant();
string filter = (nameContains ?? "").Trim();

Document source = null;
var openTitles = new List<string>();
foreach (Document candidate in app.Documents)
{
    if (candidate == null || candidate.IsFamilyDocument) continue;
    string title = "";
    try { title = candidate.Title ?? ""; } catch { }
    openTitles.Add(title);
    if (candidate.Equals(doc)) continue;
    if (title.IndexOf(wantedTitle, StringComparison.OrdinalIgnoreCase) >= 0 && wantedTitle.Length > 0)
        source = candidate;
}

if (source == null)
{
    refused = "No open project matching '" + sourceDocumentTitle + "'. Both projects have to be open " +
              "in the same Revit - open now: " + string.Join(", ", openTitles.ToArray()) + ".";
}
else
{
    var wanted = new List<View>();

    foreach (var element in new FilteredElementCollector(source).OfClass(typeof(View)))
    {
        var candidate = element as View;
        if (candidate == null) continue;

        string name = "";
        try { name = candidate.Name ?? ""; } catch { }
        if (filter.Length > 0 && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

        bool take = false;

        if (kind == "templates") take = candidate.IsTemplate;
        else if (candidate.IsTemplate) take = false;
        else if (kind == "legends") take = candidate.ViewType == ViewType.Legend;
        else if (kind == "drafting") take = candidate.ViewType == ViewType.DraftingView;
        else if (kind == "schedules")
        {
            var schedule = candidate as ViewSchedule;
            // The two kinds Revit maintains itself are not project content.
            take = schedule != null &&
                   !schedule.IsTitleblockRevisionSchedule &&
                   !schedule.IsInternalKeynoteSchedule;
        }

        if (take) wanted.Add(candidate);
    }

    if (wanted.Count == 0)
        refused = "Nothing of kind '" + viewKind + "' matched in '" + source.Title + "'.";

    var options = new CopyPasteOptions();

    foreach (var view in wanted)
    {
        string name = "";
        try { name = view.Name ?? ""; } catch { }

        ICollection<ElementId> arrived = null;
        try
        {
            var one = new List<ElementId>();
            one.Add(view.Id);
            arrived = ElementTransformUtils.CopyElements(source, one, doc, Transform.Identity, options);
        }
        catch
        {
            // Nearly always a duplicate type name, which cannot be resolved
            // from inside a fragment.
            clashed.Add(name);
            continue;
        }

        if (arrived == null || arrived.Count == 0) { clashed.Add(name); continue; }

        // The contents, for the kinds that HAVE drawn content. Without this
        // second copy the view arrives empty and the transfer looks done.
        if (kind == "legends" || kind == "drafting")
        {
            var landed = doc.GetElement(arrived.First()) as View;
            if (landed != null)
            {
                var contents = new List<ElementId>();
                try
                {
                    foreach (var owned in new FilteredElementCollector(source, view.Id)
                                 .WhereElementIsNotElementType())
                        if (owned != null && owned.Id != view.Id) contents.Add(owned.Id);
                }
                catch { }

                if (contents.Count > 0)
                {
                    try
                    {
                        ElementTransformUtils.CopyElements(
                            view, contents, landed, Transform.Identity, options);
                    }
                    catch { clashed.Add(name + " (shell copied, contents refused)"); continue; }
                }
            }
        }

        // A VIEW TEMPLATE'S VALUE IS THE FILTERS IT CARRIES, and they are
        // separate elements. A template copied into a project that does not
        // have them arrives PRESENT, CORRECTLY NAMED, and controlling less
        // than it did - which looks identical in the template list and shows
        // up as the wrong things being visible on somebody's sheet.
        //
        // GetFilters() and NOT GetOrderedFilters(): the ordered one is absent
        // from Revit 2020 and present by 2024. Read off both assemblies rather
        // than assumed, because the ordered spelling is the obvious one to
        // reach for and it compiles on exactly one end.
        if (kind == "templates")
        {
            int wantedFilters = 0;
            int gotFilters = 0;
            bool counted = false;
            try
            {
                wantedFilters = view.GetFilters().Count;
                var landedTemplate = doc.GetElement(arrived.First()) as View;
                if (landedTemplate != null)
                {
                    gotFilters = landedTemplate.GetFilters().Count;
                    counted = true;
                }
            }
            catch { counted = false; }

            if (counted && gotFilters < wantedFilters)
            {
                weakened.Add(name + " (" + gotFilters.ToString() + " of " +
                             wantedFilters.ToString() + " filters - run " +
                             "TRANSFER_VIEW_FILTERS_BETWEEN_DOCUMENTS first)");
                continue;
            }
        }

        copied.Add(name);
    }
}
