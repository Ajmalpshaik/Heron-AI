// NOT STANDALONE. Assumes `doc`, `elements`, `duplicateOption`, `newName`,
// `copyTemplates` and `templateName` are in scope, and leaves `newViewIds`,
// `duplicated`, `notDuplicated`, `newNames`, `templateCopyIds`, `copyName`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so forty views is one undo.
//
// WHY THE OPTION IS ASKED ABOUT PER VIEW.
//
// `CanViewBeDuplicated` takes the OPTION, not just the view, and the answer
// genuinely differs between them: a view can be duplicable plainly and refuse
// to become a dependent. Checking "can this be duplicated" without saying how
// answers a question nobody asked and turns a clean skip into an exception.
//
// WHY EACH VIEW IS TRIED ON ITS OWN.
//
// The version this was re-authored from wrapped the whole loop in one try and
// rolled the transaction back on any failure, so one awkward view discarded
// thirty-nine good duplicates. Here the transaction belongs to the CALLER, and
// an exception let out of this fragment would abort work it never saw - a
// rollback of somebody else's decision. So each view is tried alone and a
// failure is reported by id.
//
// WHY THE NEW ID IS RESOLVED BEFORE IT IS COUNTED.
//
// `Duplicate` hands back an ElementId. That is a better starting point than the
// move or the template case, where nothing at all comes back - but an id is
// still not a view until the document says so. Resolving it costs one lookup
// and keeps this fragment honest with the rest: what is counted is what
// happened, never what was asked for.
//
// VERSION 2 (2026-11-01) - DUPLICATE_VIEW MERGED IN, AND ITS GUARD WITH IT.
//
// A VIEW TEMPLATE IS NOT DUPLICATED ON THE PLAIN PATH. A template handed in
// with the views is reported in `notDuplicated` and never copied by accident.
// And a null in the set is skipped rather than read for an id it has not got.
//
// VERSION 3 (2026-11-01) - CREATE_LEGEND_VIEW AND DUPLICATE_VIEW_TEMPLATE
// MERGED IN, on the owner's instruction ("make sure there is no duplicate
// tools"). What each had that this lacked:
//
//   FROM CREATE_LEGEND_VIEW - `newName`. ONE view copied is renamed to it and
//   the name READ BACK, because Revit decides what a view ends up called; a
//   name Revit refuses (usually one already in use) is SAID, and the copy
//   keeps the name Revit gave it. With several views copied the name is not
//   applied - one name cannot go on two views - and RENAME_ELEMENTS is named.
//   A legend copied is told it is on no sheet yet and that a legend can sit on
//   many sheets at once, so placing it beats copying it again. And when
//   NOTHING is handed in and the project has no legend at all, the answer says
//   plainly that Revit's API cannot make the FIRST legend on any release - one
//   drawn by hand once unblocks every copy after it.
//
//   FROM DUPLICATE_VIEW_TEMPLATE - a VIEW TEMPLATE COPIED, ONLY WHEN ASKED.
//   `View.Duplicate` cannot copy a template and `CanViewBeDuplicated` answers
//   false for every template on every option, so a template is copied through
//   `ElementTransformUtils.CopyElements`, which works - verified on a real
//   template that came across carrying its scale and detail level. It is asked
//   for one of two ways: `templateName`, the template looked for by name among
//   the model's templates (a name that is not one is refused with every
//   template listed), or `copyTemplates` true, which copies the templates
//   handed in with `elements`. Neither said, a template handed in is refused
//   exactly as version 2 refused it - THE PLAIN DUPLICATE PATH STILL NEVER
//   COPIES A TEMPLATE. Template names must be unique, so Revit auto-suffixes
//   the copy ('Mechanical Plan' becomes 'Mechanical Plan1'); the name is READ
//   BACK, never predicted, and `newName` is NOT put on a template copy -
//   renaming it is RENAME_ELEMENTS, a separate call after the commit, with the
//   id handed back here. What comes back is checked to be a template.
//
//   `duplicateOption` LEFT OUT reads "none" - no way at all - so views handed
//   in are NOT duplicated and are all named in `notDuplicated`, exactly as an
//   unrecognised word was always treated. Never a default. A template copy
//   needs no option, which is why it may now be left out.

var option = ViewDuplicateOption.WithDetailing;
bool optionRecognised = true;

// Matched by name so that the caller needs no enum reference, and so an
// unfamiliar word is REPORTED rather than quietly becoming a default. Silently
// duplicating with detailing when somebody asked for a dependent view is the
// kind of wrong that only shows up much later.
var askedFor = (duplicateOption ?? "").Trim();
if (string.Equals(askedFor, "Duplicate", StringComparison.OrdinalIgnoreCase))
    option = ViewDuplicateOption.Duplicate;
else if (string.Equals(askedFor, "AsDependent", StringComparison.OrdinalIgnoreCase))
    option = ViewDuplicateOption.AsDependent;
else if (string.Equals(askedFor, "WithDetailing", StringComparison.OrdinalIgnoreCase))
    option = ViewDuplicateOption.WithDetailing;
else
    optionRecognised = false;

var newViewIds = new List<ElementId>();
var notDuplicated = new List<ElementId>();
int duplicated = 0;
var newNames = new List<string>();
var templateCopyIds = new List<ElementId>();
var copyName = "";
var findings = new List<string>();

Func<string, bool> given = text =>
{
    var t = (text ?? "").Trim();
    return t.Length > 0 && !string.Equals(t, "none", StringComparison.OrdinalIgnoreCase);
};
var wantedName = given(newName) ? newName.Trim() : "";
var wantedTemplate = given(templateName) ? templateName.Trim() : "";
var handed = new List<Element>();
if (elements != null) foreach (var element in elements) if (element != null) handed.Add(element);

// ---- VIEWS AND TEMPLATES HANDED IN ----------------------------------------

var plainViews = new List<View>();
var templatesToCopy = new List<View>();
foreach (var element in handed)
{
    var view = element as View;
    if (view != null && view.IsTemplate)
    {
        // THE PLAIN PATH NEVER COPIES A TEMPLATE (version 2). Only an explicit
        // copyTemplates sends one to the template copy below (version 3).
        if (copyTemplates) templatesToCopy.Add(view);
        else
        {
            notDuplicated.Add(element.Id);
            findings.Add(string.Format("'{0}' is a view TEMPLATE and was not duplicated - a template is copied "
                + "only when asked: name it in templateName, or set copyTemplates", view.Name));
        }
        continue;
    }
    if (view == null)
    {
        // Not a view - a duct, a level. Its id, never a guess at what was meant.
        notDuplicated.Add(element.Id);
        continue;
    }
    plainViews.Add(view);
}

if (plainViews.Count > 0 && !optionRecognised)
{
    // Nothing is touched. Every view is reported as not duplicated, which is
    // exactly true, and the caller can tell this from a per-view refusal
    // because ALL of them are in the list and none was made.
    foreach (var view in plainViews) notDuplicated.Add(view.Id);
    findings.Add(string.Format("'{0}' is not one of the three ways to duplicate - Duplicate, WithDetailing or "
        + "AsDependent - so no view was duplicated", askedFor));
}
else
{
    foreach (var view in plainViews)
    {
        // Revit says not this way. Reported by id rather than counted away,
        // because the caller needs to know WHICH.
        if (!view.CanViewBeDuplicated(option))
        {
            notDuplicated.Add(view.Id);
            continue;
        }

        ElementId madeId;
        try
        {
            madeId = view.Duplicate(option);
        }
        catch (Exception)
        {
            // Revit accepted the question and refused the act. One view's
            // problem, and it must not cost the others.
            notDuplicated.Add(view.Id);
            continue;
        }

        // THE READ-BACK. An id is not a view until the document agrees.
        var made = madeId != ElementId.InvalidElementId ? doc.GetElement(madeId) as View : null;
        if (made == null)
        {
            notDuplicated.Add(view.Id);
            continue;
        }

        newViewIds.Add(madeId);
        duplicated++;

        // A LEGEND (version 3, from CREATE_LEGEND_VIEW): the one view that can
        // sit on many sheets at once.
        if (view.ViewType == ViewType.Legend)
            findings.Add(string.Format("Legend '{0}' copied, carrying {1}. It is on no sheet yet - a legend can sit "
                + "on many sheets at once, so place it rather than copying it again", view.Name,
                option == ViewDuplicateOption.Duplicate ? "no contents - Revit's API can place no NEW legend "
                    + "component, so filling it stays a manual job" : "the source's contents"));
    }

    // THE NAME, ON ONE COPY ONLY (version 3, from CREATE_LEGEND_VIEW), and
    // READ BACK: Revit decides what a view ends up called.
    if (wantedName.Length > 0 && newViewIds.Count == 1)
    {
        var made = doc.GetElement(newViewIds[0]) as View;
        var namedOk = true;
        try { made.Name = wantedName; }
        catch (Exception) { namedOk = false; }
        var nowCalled = (doc.GetElement(newViewIds[0]) as View).Name;
        if (!namedOk || !string.Equals(nowCalled, wantedName, StringComparison.Ordinal))
            findings.Add(string.Format("The name '{0}' was refused, usually because it is already in use - the "
                + "copy is called '{1}'", wantedName, nowCalled));
        else findings.Add(string.Format("The copy is called '{0}' - read back", nowCalled));
    }
    else if (wantedName.Length > 0 && newViewIds.Count > 1)
    {
        findings.Add(string.Format("{0} views were copied and one name cannot go on all of them, so '{1}' was "
            + "not applied - RENAME_ELEMENTS names them, with the ids handed back", newViewIds.Count, wantedName));
    }

    foreach (var madeId in newViewIds)
    {
        var made = doc.GetElement(madeId) as View;
        newNames.Add(made == null ? "" : made.Name);
    }
}

// ---- A TEMPLATE COPIED, ONLY WHEN ASKED (version 3, DUPLICATE_VIEW_TEMPLATE) ----

if (wantedTemplate.Length > 0)
{
    View source = null;
    var available = new List<string>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        if (!candidate.IsTemplate) continue;
        available.Add(candidate.Name);
        if (source == null && string.Equals(candidate.Name, wantedTemplate, StringComparison.OrdinalIgnoreCase))
            source = candidate;
    }
    if (source == null)
        findings.Add(string.Format("No view template is called '{0}'. This model has: {1}", wantedTemplate,
            available.Count == 0 ? "none" : string.Join(", ", available.ToArray())));
    else if (!templatesToCopy.Any(t => t.Id == source.Id))
        templatesToCopy.Add(source);
}

foreach (var source in templatesToCopy)
{
    try
    {
        // View.Duplicate CANNOT COPY A TEMPLATE, AND THE GUARD LIES TOO:
        // CanViewBeDuplicated is false for every template on every option.
        var made = ElementTransformUtils.CopyElements(doc, new List<ElementId> { source.Id }, XYZ.Zero);
        if (made == null || made.Count == 0)
        {
            notDuplicated.Add(source.Id);
            findings.Add(string.Format("Copying the template '{0}' returned nothing. Nothing was created", source.Name));
            continue;
        }

        var copyId = made.First();
        var copy = doc.GetElement(copyId) as View;
        var called = copy == null ? "" : copy.Name;
        templateCopyIds.Add(copyId);
        copyName = copyName.Length == 0 ? called : copyName + ", " + called;

        findings.Add(string.Format("The template '{0}' was copied. Revit named the copy '{1}' (id {2}) - template "
            + "names must be unique, so it adds a suffix of its own. THE COPY IS NOT RENAMED HERE: its final name "
            + "is settled once this transaction commits, so renaming it is a SEPARATE call to RENAME_ELEMENTS, "
            + "after the commit, using that id", source.Name,
            string.IsNullOrEmpty(called) ? "(name not readable yet)" : called, copyId));

        if (copy != null && !copy.IsTemplate)
            findings.Add("What came back is NOT a template. That should not happen through this route, and the copy "
                + "should be looked at before it is used");
    }
    catch (Exception ex)
    {
        notDuplicated.Add(source.Id);
        findings.Add(string.Format("Copying the template '{0}' failed: {1}. View.Duplicate is not an alternative "
            + "here - it cannot copy a template at all", source.Name, ex.Message));
    }
}

// ---- NOTHING HANDED IN ------------------------------------------------------

if (handed.Count == 0 && wantedTemplate.Length == 0)
{
    // THE FIRST LEGEND (version 3, from CREATE_LEGEND_VIEW). Said only when
    // the project really has none.
    var anyLegend = false;
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        if (!candidate.IsTemplate && candidate.ViewType == ViewType.Legend) { anyLegend = true; break; }
    }
    findings.Add(anyLegend
        ? "No view was handed in, so nothing was duplicated"
        : "No view was handed in, so nothing was duplicated - and this project has NO LEGEND. Revit's API cannot "
          + "create the FIRST legend on any release: one has to be drawn by hand once (View, then Legends), and "
          + "every copy after that can come from here");
}
else if (wantedName.Length > 0 && newViewIds.Count == 0)
{
    findings.Add(string.Format("No view was duplicated, so the name '{0}' was not used", wantedName));
}

findings.Insert(0, string.Format("{0} view(s) duplicated, {1} view template(s) copied, {2} not duplicated",
    duplicated, templateCopyIds.Count, notDuplicated.Count));
