// NOT STANDALONE. Assumes `doc`, `views`, `scale`, `detailLevel` and
// `visualStyle` are in scope; leaves `changed` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// EACH PROPERTY IS SET ON ITS OWN. Not every view type carries every one - a
// schedule has no scale and no visual style - so setting them as a block means
// one unsupported property leaves the others unset and nothing says so.
//
// A VIEW UNDER A TEMPLATE WILL REFUSE, and that is the commonest thing to hit.
// It is reported as a reason, because the answer is not to try harder: it is to
// look at what the template controls.
//
// ANYTHING NOT GIVEN IS NOT TOUCHED. Writing a default into every property
// somebody left out would quietly restyle a whole drawing set.
//
// EVERY VALUE IS READ BACK. A view property can refuse without raising.

var changed = 0;
var findings = new List<string>();

if (views == null || views.Count == 0)
{
    findings.Add("No views were given");
}
else
{
    var wantDetail = string.IsNullOrEmpty(detailLevel) ? "" : detailLevel.Trim();
    var wantStyle = string.IsNullOrEmpty(visualStyle) ? "" : visualStyle.Trim();
    var wantScale = scale > 0;

    if (!wantScale && wantDetail.Length == 0 && wantStyle.Length == 0)
    {
        findings.Add("Nothing was asked for - give a scale, a detail level or a visual style. Anything "
            + "left out is deliberately not touched");
    }
    else
    {
        var scaleSet = 0;
        var detailSet = 0;
        var styleSet = 0;
        var refused = new List<string>();

        foreach (var view in views)
        {
            if (view == null) continue;
            if (view.IsTemplate)
            {
                refused.Add(string.Format("'{0}' is a template, not a view", view.Name));
                continue;
            }

            var touched = false;

            if (wantScale)
            {
                try
                {
                    view.Scale = scale;
                    if (view.Scale == scale) { scaleSet++; touched = true; }
                    else refused.Add(string.Format("'{0}': the scale did not take {1}", view.Name, scale));
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("'{0}' scale: {1}", view.Name, ex.Message));
                }
            }

            if (wantDetail.Length > 0)
            {
                try
                {
                    var wanted = ViewDetailLevel.Medium;
                    var known = true;
                    if (string.Equals(wantDetail, "Coarse", StringComparison.OrdinalIgnoreCase))
                        wanted = ViewDetailLevel.Coarse;
                    else if (string.Equals(wantDetail, "Medium", StringComparison.OrdinalIgnoreCase))
                        wanted = ViewDetailLevel.Medium;
                    else if (string.Equals(wantDetail, "Fine", StringComparison.OrdinalIgnoreCase))
                        wanted = ViewDetailLevel.Fine;
                    else known = false;

                    if (!known)
                        refused.Add(string.Format("'{0}': detail level must be Coarse, Medium or Fine - "
                            + "'{1}' is none of them", view.Name, wantDetail));
                    else
                    {
                        view.DetailLevel = wanted;
                        if (view.DetailLevel == wanted) { detailSet++; touched = true; }
                        else refused.Add(string.Format("'{0}': the detail level did not take", view.Name));
                    }
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("'{0}' detail level: {1}", view.Name, ex.Message));
                }
            }

            if (wantStyle.Length > 0)
            {
                try
                {
                    var matchedStyle = false;
                    foreach (DisplayStyle candidate in Enum.GetValues(typeof(DisplayStyle)))
                    {
                        if (!string.Equals(candidate.ToString(), wantStyle, StringComparison.OrdinalIgnoreCase))
                            continue;

                        view.DisplayStyle = candidate;
                        matchedStyle = true;
                        if (view.DisplayStyle == candidate) { styleSet++; touched = true; }
                        else refused.Add(string.Format("'{0}': the visual style did not take", view.Name));
                        break;
                    }

                    if (!matchedStyle)
                        refused.Add(string.Format("'{0}': no visual style is called '{1}'", view.Name, wantStyle));
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("'{0}' visual style: {1}", view.Name, ex.Message));
                }
            }

            if (touched) changed++;
        }

        findings.Add(string.Format("{0} of {1} view(s) changed in some way: scale set on {2}, detail "
            + "level on {3}, visual style on {4}. Each property was set on its own, so a view that "
            + "took one and refused another is counted here and named below",
            changed, views.Count, scaleSet, detailSet, styleSet));

        foreach (var failure in refused) findings.Add("Refused: " + failure);

        if (refused.Count > 0)
            findings.Add("A view controlled by a VIEW TEMPLATE refuses the properties that template "
                + "holds, and that is the commonest reason for a refusal here. "
                + "REPORT_VIEW_TEMPLATE_CONTROL says which properties those are");
    }
}
