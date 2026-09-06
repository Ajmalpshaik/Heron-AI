// NOT STANDALONE. Assumes `doc` and `namePrefix` are in scope, and leaves
// `created`, `alreadyExisted` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// USER WORKSETS ONLY.
//
// Revit keeps worksets of its own for views, families and project standards.
// A view for each of those buries the four that matter under thirty that do
// not, and nobody asking this question means them.
//
// A NAME CLASH THROWS, SO THE NAMES ARE COLLECTED FIRST.
//
// Re-running this after one workset is added must not fail on the nineteen
// views that already exist. They are named as already there, which is also
// the honest answer to "did it do anything".
//
// NOT WORKSHARED IS NOT THE SAME AS NOTHING FOUND.
//
// Both produce no views. One is a file that cannot have user worksets at all
// and one is a job where nobody has made any - and the fix is different.

var created = new Dictionary<string, ElementId>();
var alreadyExisted = new List<string>();
string refused = "";

if (!doc.IsWorkshared)
{
    refused = "This model is not workshared, so it has no user worksets at all - which is not the " +
              "same as having none made yet.";
}
else
{
    ViewFamilyType threeDType = null;
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
    {
        var candidate = element as ViewFamilyType;
        if (candidate != null && candidate.ViewFamily == ViewFamily.ThreeDimensional)
        {
            threeDType = candidate;
            break;
        }
    }

    if (threeDType == null)
        refused = "This project has no 3D view family type, so no 3D view can be created in it.";
    else
    {
        var worksets = new FilteredWorksetCollector(doc)
            .OfKind(WorksetKind.UserWorkset)
            .ToWorksets()
            .ToList();

        if (worksets.Count == 0)
            refused = "This model is workshared and has no user worksets yet.";

        // Every existing view name, so a clash is skipped rather than thrown.
        var takenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
        {
            var view = element as View;
            if (view == null) continue;
            try { if (!string.IsNullOrEmpty(view.Name)) takenNames.Add(view.Name); } catch { }
        }

        foreach (var workset in worksets)
        {
            string name = (namePrefix ?? "") + workset.Name;

            if (takenNames.Contains(name)) { alreadyExisted.Add(name); continue; }

            View3D view = null;
            try { view = View3D.CreateIsometric(doc, threeDType.Id); }
            catch { continue; }
            if (view == null) continue;

            try { view.Name = name; }
            catch
            {
                // The name was taken by something the collector did not see.
                // The view exists either way and is reported under the workset.
                alreadyExisted.Add(name);
            }

            foreach (var other in worksets)
            {
                try
                {
                    view.SetWorksetVisibility(
                        other.Id,
                        other.Id == workset.Id ? WorksetVisibility.Visible : WorksetVisibility.Hidden);
                }
                catch { }
            }

            created[workset.Name] = view.Id;
            takenNames.Add(name);
        }
    }
}
