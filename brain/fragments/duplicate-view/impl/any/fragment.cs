// NOT STANDALONE. Assumes `views` and `option` are in scope; leaves `created`
// and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE OPTION IS THE CALLER'S AND HAS NO DEFAULT (D-33), because the three are
// different jobs wearing similar names:
//
//   Duplicate        geometry only - no tags, no dimensions, no annotation
//   WithDetailing    geometry AND annotation, independent afterwards
//   AsDependent      a CHILD of this view. Crop and scope box are its own;
//                    everything else stays tied to the parent forever
//
// The third is not a copy in the sense anybody means by "copy", and picking it
// by accident produces a view that mysteriously refuses changes later. Choosing
// one here would be choosing it for every job.
//
// ASKED BEFORE ATTEMPTED. `CanViewBeDuplicated` is Revit's own answer, and it
// turns a schedule or a legend that cannot be duplicated this way into a
// reported outcome rather than an exception that abandons the batch.

var created = new List<ElementId>();
var refused = new List<ElementId>();

foreach (var view in views)
{
    if (view == null) continue;

    if (view.IsTemplate || !view.CanViewBeDuplicated(option))
    {
        refused.Add(view.Id);
        continue;
    }

    try
    {
        var copy = view.Duplicate(option);

        // InvalidElementId back is a refusal Revit did not throw for - the
        // silent no-op shape. Never counted as a created view.
        if (copy == null || copy == ElementId.InvalidElementId) refused.Add(view.Id);
        else created.Add(copy);
    }
    catch (Exception)
    {
        refused.Add(view.Id);
    }
}
