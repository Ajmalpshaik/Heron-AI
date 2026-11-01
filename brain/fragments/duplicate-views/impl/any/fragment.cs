// NOT STANDALONE. Assumes `doc`, `elements` and `duplicateOption` are in scope,
// and leaves `newViewIds`, `duplicated` and `notDuplicated` behind.
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
// A VIEW TEMPLATE IS NOT DUPLICATED HERE. Copying a template is
// DUPLICATE_VIEW_TEMPLATE's job - it is a different thing to make, with a
// different name and none of a view's placement - so a template handed in is
// reported in `notDuplicated` and never copied by accident. And a null in the
// set is skipped rather than read for an id it has not got.

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

if (optionRecognised)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var view = element as View;

        // Not a view, a view TEMPLATE (version 2, from DUPLICATE_VIEW), or
        // Revit says not this way. Each is reported by id rather than counted
        // away, because the caller needs to know WHICH.
        if (view == null || view.IsTemplate || !view.CanViewBeDuplicated(option))
        {
            notDuplicated.Add(element.Id);
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
        if (madeId != ElementId.InvalidElementId && doc.GetElement(madeId) is View)
        {
            newViewIds.Add(madeId);
            duplicated++;
        }
        else
        {
            notDuplicated.Add(view.Id);
        }
    }
}
else
{
    // Nothing is touched. Every view is reported as not duplicated, which is
    // exactly true, and the caller can tell this from a per-view refusal
    // because ALL of them are in the list and none was made.
    foreach (var element in elements) if (element != null) notDuplicated.Add(element.Id);
}
