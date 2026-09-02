// NOT STANDALONE. Assumes `elements` and `worksetId` are in scope; leaves
// `moved`, `alreadyThere` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A WORKSET ID IS AN INT, NOT AN ElementId. `ELEM_PARTITION_PARAM` is an
// integer parameter and `WorksetId` is its own type - it was untouched by
// 2024's move of ElementId to 64 bits. The int here is deliberate; changing it
// to an ElementId to "match the rest of the library" would be wrong.
//
// READ FIRST, WRITE, READ BACK. Three reasons, and they are different:
//
//   before   an element already on the target workset would be counted as
//            moved. "Moved 200" when 190 were already there is the
//            asked-for/happened failure again
//   write    the parameter is read-only on an element owned by another user,
//            and on a model that is not workshared at all
//   after    Set returning true is not evidence the value stored - this
//            repository has already been caught by exactly that on the
//            parameter path, where Revit returned TRUE and snapped the value

var moved = 0;
var alreadyThere = 0;
var refused = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    var parameter = element.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
    if (parameter == null || parameter.StorageType != StorageType.Integer)
    {
        // No workset parameter at all - a model that is not workshared, or an
        // element kind that carries none. Named, never silently skipped.
        refused.Add(element.Id);
        continue;
    }

    if (parameter.AsInteger() == worksetId)
    {
        alreadyThere++;
        continue;
    }

    if (parameter.IsReadOnly)
    {
        // The usual cause is another user owning the element. That is worth
        // knowing BEFORE the batch - READ_ELEMENT_OWNERSHIP is the fragment
        // that says so, and this one can only report the refusal it hits.
        refused.Add(element.Id);
        continue;
    }

    try
    {
        parameter.Set(worksetId);
    }
    catch (Exception)
    {
        refused.Add(element.Id);
        continue;
    }

    if (parameter.AsInteger() == worksetId) moved++;
    else refused.Add(element.Id);
}
