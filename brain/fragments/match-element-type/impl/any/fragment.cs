// NOT STANDALONE. Assumes `doc`, `source` and `elements` are in scope; leaves
// `changed` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so the whole set is one undo.
//
// THE TYPE COMES FROM AN ELEMENT, NOT A NAME. That is the difference from
// CHANGE_ELEMENT_TYPE: a modeller points at the wall that is correct far more
// often than they can recite what it is called.
//
// IT CHANGES THE TYPE AND NOTHING ELSE. Mark, comments, offsets - everything
// somebody typed on each element - are left exactly as they were. Revit's own
// match tool copies more than that, and expecting this to is how a mark gets
// overwritten across twelve elements at once.
//
// THE TYPE IS READ BACK OFF EACH ELEMENT. ChangeTypeId returning is not the
// type having taken; some elements refuse quietly, and a count that assumed
// success would report twelve where nine happened.

var changed = 0;
var findings = new List<string>();

if (source == null)
{
    findings.Add("No source element was given - point at the one whose type the others should take");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given to change");
}
else
{
    var wantedTypeId = source.GetTypeId();

    if (wantedTypeId == ElementId.InvalidElementId)
    {
        findings.Add(string.Format("'{0}' (id {1}) has no type to take. A system element without a "
            + "type cannot be the source of a match", source.Name, source.Id));
    }
    else
    {
        var sourceCategoryId = source.Category == null ? ElementId.InvalidElementId : source.Category.Id;
        var sourceTypeName = "";
        var sourceType = doc.GetElement(wantedTypeId);
        if (sourceType != null) sourceTypeName = sourceType.Name;

        var alreadyRight = 0;
        var wrongCategory = 0;
        var refused = new List<string>();

        foreach (var element in elements)
        {
            if (element == null) continue;
            if (element.Id == source.Id) continue;   // the source is already itself

            var categoryId = element.Category == null ? ElementId.InvalidElementId : element.Category.Id;
            if (categoryId != sourceCategoryId)
            {
                wrongCategory++;
                refused.Add(string.Format("id {0} is a {1} and the source is a {2}",
                    element.Id,
                    element.Category == null ? "category-less element" : element.Category.Name,
                    source.Category == null ? "category-less element" : source.Category.Name));
                continue;
            }

            if (element.GetTypeId() == wantedTypeId) { alreadyRight++; continue; }

            try
            {
                element.ChangeTypeId(wantedTypeId);

                // READ IT BACK - some elements refuse quietly.
                var fresh = doc.GetElement(element.Id);
                if (fresh != null && fresh.GetTypeId() == wantedTypeId) changed++;
                else refused.Add(string.Format("id {0}: the call returned and the type did not change",
                    element.Id));
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("id {0}: {1}", element.Id, ex.Message));
            }
        }

        findings.Add(string.Format("{0} element(s) changed to '{1}'. {2} already had it, and {3} are "
            + "of a different category and were refused",
            changed,
            string.IsNullOrEmpty(sourceTypeName) ? "the source type" : sourceTypeName,
            alreadyRight, wrongCategory));

        foreach (var failure in refused) findings.Add("Refused: " + failure);

        if (changed > 0)
            findings.Add("Only the TYPE changed. Mark, comments, offsets and every other instance "
                + "parameter are exactly as they were - COPY_PARAMETER_VALUE is what moves those, "
                + "and it is a separate decision");
    }
}
