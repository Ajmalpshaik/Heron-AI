// NOT STANDALONE. Assumes `views`, `scale` and `apply` are in scope; leaves
// `wasScale`, `changed`, `alreadyThatScale` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION when `apply` is true (Golden Rule 16).
//
// `View.Scale` IS THE DENOMINATOR. 1:50 is the integer 50. Larger number,
// smaller drawing - which is the opposite of how "bigger scale" is said out
// loud, and worth stating where somebody will read it.
//
// READ FIRST, WRITE, READ BACK.
//
//   before   a view already at that scale would be counted as changed
//   write    a TEMPLATE commonly locks scale, and the setter throws
//   after    assigning does not always take, and an absence of exception is
//            not evidence - the standing lesson from the move and parameter
//            paths
//
// AND CHANGING SCALE CHANGES THE DRAWING. Annotation is sized in paper units,
// so every tag and dimension changes apparent size. On a view already on a
// sheet this is a visible change to an issued drawing.

var wasScale = new Dictionary<ElementId, int>();
var changed = 0;
var alreadyThatScale = 0;
var refused = new List<ElementId>();

foreach (var view in views)
{
    if (view == null) continue;
    if (wasScale.ContainsKey(view.Id)) continue;

    if (view.IsTemplate)
    {
        refused.Add(view.Id);
        continue;
    }

    int was;
    try
    {
        was = view.Scale;
    }
    catch (Exception)
    {
        refused.Add(view.Id);
        continue;
    }

    wasScale[view.Id] = was;

    if (!apply) continue;

    if (was == scale)
    {
        alreadyThatScale++;
        continue;
    }

    try
    {
        view.Scale = scale;
    }
    catch (Exception)
    {
        // Almost always a view template owning the scale. The fix is in the
        // template, not the view, and naming the refusal is what points there.
        refused.Add(view.Id);
        continue;
    }

    if (view.Scale == scale) changed++;
    else refused.Add(view.Id);
}
