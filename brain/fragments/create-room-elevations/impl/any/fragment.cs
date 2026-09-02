// NOT STANDALONE. Assumes `doc`, `planView`, `origin`, `slotCount` and `scale`
// are in scope; leaves `marker`, `created` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The origin is internal FEET.
//
// AN INTERIOR ELEVATION IS A MARKER WITH SLOTS, not a section. A section is a
// line and a depth cutting through the model; nothing about it produces the
// four-arrow symbol a drawing set expects.
//
// THE SLOTS ARE NOT ROTATED TO FACE THE WALLS. In a room square to the project
// axes that is right; in a rotated room the views look at corners. The marker
// is an ordinary element and rotating it afterwards turns its views with it -
// said in the report because it is invisible until the sheet is laid out.
//
// THE VIEW FAMILY TYPE IS FOUND BY WALKING ViewFamilyType AND COMPARING
// ViewFamily. An id typed in differs per template, and the wrong one gives a
// puzzling failure at creation rather than a clear one here.

Element marker = null;
var created = new List<Element>();
var refused = new List<string>();

ViewFamilyType elevationType = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
{
    var candidate = element as ViewFamilyType;
    if (candidate != null && candidate.ViewFamily == ViewFamily.Elevation) { elevationType = candidate; break; }
}

if (elevationType == null)
{
    refused.Add("this project has no Elevation view family type, so no elevation can be created. It is "
        + "a template setting, not a modelling problem");
}
else if (planView == null || !(planView is ViewPlan))
{
    refused.Add("the marker has to go in a PLAN view - a floor plan or a ceiling plan. The elevations "
        + "inherit its phase, which is why it is asked for rather than guessed");
}
else if (planView.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view TEMPLATE, not a plan anything can be placed in", planView.Name));
}
else
{
    ElevationMarker created_marker = null;
    try
    {
        created_marker = ElevationMarker.CreateElevationMarker(doc, elevationType.Id, origin, scale);
        marker = created_marker;
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("could not place the elevation marker - {0}", ex.Message));
    }

    if (created_marker != null)
    {
        var maximum = created_marker.MaximumViewCount;
        var wanted = slotCount;
        if (wanted > maximum)
        {
            refused.Add(string.Format("{0} elevations asked for; this marker holds {1}. Making {1}",
                wanted, maximum));
            wanted = maximum;
        }
        if (wanted < 1)
        {
            refused.Add("no elevations asked for - the marker was placed and left empty");
        }

        for (var index = 0; index < wanted; index++)
        {
            if (!created_marker.IsAvailableIndex(index))
            {
                refused.Add(string.Format("slot {0} is already taken", index));
                continue;
            }
            try
            {
                var view = created_marker.CreateElevation(doc, planView.Id, index);
                if (view != null) created.Add(view);
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("slot {0} - {1}", index, ex.Message));
            }
        }

        if (created.Count > 0)
        {
            refused.Add(string.Format("{0} elevation(s) made, pointing in the marker's DEFAULT "
                + "directions. They are not turned to face the walls - if this room is rotated, rotate "
                + "the marker and the views follow", created.Count));
        }
    }
}
