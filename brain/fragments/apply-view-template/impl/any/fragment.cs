// NOT STANDALONE. Assumes `doc`, `elements` and `templateName` are in scope, and
// leaves `applied`, `alreadyHad`, `notApplied` and `availableTemplates` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so forty views is one undo.
//
// WHY THIS FRAGMENT IS MOSTLY A READ-BACK.
//
// `view.ViewTemplateId = id` is a property set, and a property set that does
// not take is SILENT. There is no return value to check and no exception on the
// cases that matter. A template that does not suit the view, or a view whose
// template is not free to change, leaves the view exactly as it was while the
// line after the assignment counts a success.
//
// That is this project's defining failure - reporting the number that was ASKED
// FOR as the number that HAPPENED - and it is the third place it has been
// found: the move path returned normally and moved nothing, the parameter path
// returned TRUE and then snapped the value. So the only evidence used here is
// the template id read back off the view afterwards.
//
// FOUR OUTCOMES, kept apart because a person needs a different answer to each:
//
//   applied            it did not have this template and now does
//   alreadyHad         it already did. Not a failure, and NOT a change either -
//                      "applied to 12" when 8 already had it hides that the
//                      request altered 4 things, which is what somebody needs
//                      before deciding whether to undo
//   notApplied         the read-back disagrees with what was asked, or the
//                      element is not a view, or it is itself a template
//   availableTemplates only when the named template does not exist
//
// `notApplied` carries IDS rather than a count, for the same reason
// MOVE_ELEMENTS reports blocked ids: "three did not take" is not something a
// person can act on, and the ids are what they select in Revit to go and look.

var wantedName = (templateName ?? "").Trim();

int applied = 0;
int alreadyHad = 0;
var notApplied = new List<ElementId>();
var availableTemplates = new List<string>();

// Every template in the document, gathered once. Also the answer when the
// wanted one is not there - nearly every miss is a spelling or a renamed
// standard, and the list of real names is the reply to the next question.
var templates = new FilteredElementCollector(doc)
    .OfClass(typeof(View))
    .Cast<View>()
    .Where(v => v.IsTemplate)
    .ToList();

var template = templates.FirstOrDefault(
    v => string.Equals(v.Name, wantedName, StringComparison.OrdinalIgnoreCase));

if (template == null)
{
    // Not an exception: the caller asked for something reasonable that is not
    // here. Report what IS here and change nothing at all.
    availableTemplates = templates.Select(v => v.Name)
                                  .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                                  .ToList();
}
else
{
    foreach (var element in elements)
    {
        var view = element as View;

        // Not a view, or a template being handed a template. Both are the
        // caller pointing at the wrong thing, and both are reported by id
        // rather than silently passed over.
        if (view == null || view.IsTemplate)
        {
            notApplied.Add(element.Id);
            continue;
        }

        if (view.ViewTemplateId == template.Id)
        {
            alreadyHad++;
            continue;
        }

        try
        {
            view.ViewTemplateId = template.Id;
        }
        catch (Exception)
        {
            // Revit refused outright. That is a clean answer and it is recorded
            // the same way as a silent refusal - from the caller's side the
            // view does not have the template either way.
            notApplied.Add(view.Id);
            continue;
        }

        // THE READ-BACK. Everything above this line is the request; this is the
        // only evidence of what happened.
        if (view.ViewTemplateId == template.Id) applied++;
        else notApplied.Add(view.Id);
    }
}
