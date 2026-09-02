// NOT STANDALONE. Assumes `doc`, `views`, `baseLevel`, `topLevel` and
// `lookingDown` are in scope; leaves `changed`, `notPlanViews` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// BASE AND TOP ARE SET TOGETHER, ALWAYS. Writing the base alone leaves the old
// top in place, and the view then shows a RANGE nobody asked for - base at
// Level 2 with the top still at Level 5 is three storeys of ghost, which reads
// as a corrupted view rather than a wrong setting.
//
// CLEARING IS THE SAME CALL WITH NO LEVELS. That is what Revit itself does for
// an underlay of None, so there is no second path to get wrong.
//
// THE ORIENTATION IS NOT COSMETIC. Looking down shows what is BELOW the range;
// looking up shows what is above. The wrong one gives an underlay that looks
// empty, and empty reads as broken rather than as pointed the wrong way.
//
// ONLY PLAN VIEWS HAVE AN UNDERLAY, and anything else is NAMED rather than
// passed over. A silent skip on half a list is how somebody concludes the whole
// thing did nothing.

var changed = 0;
var notPlanViews = new List<string>();
var findings = new List<string>();

var clearing = baseLevel == null && topLevel == null;
var orientation = lookingDown ? UnderlayOrientation.LookingDown : UnderlayOrientation.LookingUp;

if (!clearing && (baseLevel == null || topLevel == null))
{
    // Half a range is the defect this fragment exists to prevent, so it is
    // refused rather than half-applied.
    findings.Add("An underlay needs BOTH a base level and a top level, or neither to clear it. One "
        + "on its own leaves the other where it was and shows a range nobody asked for");
}
else
{
    foreach (var view in views)
    {
        if (view == null) continue;

        var plan = view as ViewPlan;
        if (plan == null || plan.IsTemplate)
        {
            notPlanViews.Add(string.Format("{0} ({1})", view.Name,
                plan == null ? "not a plan view - only plans have an underlay" : "a view template"));
            continue;
        }

        try
        {
            if (clearing)
            {
                plan.SetUnderlayRange(ElementId.InvalidElementId, ElementId.InvalidElementId);
            }
            else
            {
                plan.SetUnderlayRange(baseLevel.Id, topLevel.Id);
                plan.SetUnderlayOrientation(orientation);
            }

            // READ IT BACK. The view is what says whether the setting took.
            var now = plan.GetUnderlayBaseLevel();
            var took = clearing ? now == ElementId.InvalidElementId : now == baseLevel.Id;

            if (took) changed++;
            else findings.Add(string.Format("'{0}': the call was accepted and the underlay did not "
                + "change - a view template usually controls it", plan.Name));
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("'{0}': {1}", view.Name, ex.Message));
        }
    }

    findings.Add(string.Format("{0} plan view(s) {1}{2}",
        changed,
        clearing ? "had the underlay cleared"
                 : string.Format("now show {0} to {1}, looking {2}",
                       baseLevel.Name, topLevel.Name, lookingDown ? "down" : "up"),
        notPlanViews.Count == 0 ? ""
            : string.Format(". {0} had no underlay to set: {1}", notPlanViews.Count,
                  string.Join("; ", notPlanViews))));
}
