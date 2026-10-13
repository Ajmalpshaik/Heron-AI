// NOT STANDALONE. Assumes `doc`, `views`, `scale`, `detailLevel` and
// `visualStyle` are in scope; leaves `changed`, `findings` and
// `visualStyleNote` behind.
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
//
// THE VISUAL STYLE IS TAKEN IN THE WORDS REVIT SHOWS A MODELLER (version 2,
// FRAGMENT-ISSUES 5b-457). Version 1 matched Revit's API names only, so
// "Shaded" was refused - the API calls it Shading - while "Realistic" worked
// because there the two names happen to agree. The View Control Bar's words are
// read first, then the API's own names, so every word version 1 took still
// works. Matched by NAME at run time, never by a typed member: value 9 is
// Raytrace on Revit 2020 and Textures from 2021, so either name typed here
// would fail to compile on the other half of the range.
//
// "SHADED" AND "REALISTIC" LEAVE SHOW EDGES AS THE VIEW HAS IT. In Revit, Show
// Edges is its own tick box in Graphic Display Options, and the View Control
// Bar changes the style without touching it. The API folds the two together -
// Shading or ShadingWithEdges - so a view already showing edges keeps them,
// and "Shaded with Edges" asks for them outright.
//
// AN UNKNOWN WORD IS REFUSED ONCE, NOT ONCE PER VIEW, and the refusal lists
// every word this Revit takes. It is also left whole in `visualStyleNote`,
// because a finding is cut to sixty characters on its way back and the list is
// the part the caller needs.

var changed = 0;
var findings = new List<string>();
var visualStyleNote = "";

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

        // Revit's own names for a visual style ON THIS RELEASE that a view can
        // be SET to. Undefined is the absence of a style. Raytrace is refused by
        // Revit 2020 itself - "it is not permitted to directly assign the display
        // style to this value", measured 2026-10-13 - and 2021 on has no such
        // value, so neither is offered.
        var revitStyles = new Dictionary<string, DisplayStyle>(StringComparer.OrdinalIgnoreCase);
        foreach (DisplayStyle candidate in Enum.GetValues(typeof(DisplayStyle)))
        {
            var revitName = candidate.ToString();
            if (revitName == "Undefined" || revitName == "Raytrace" || revitStyles.ContainsKey(revitName))
                continue;
            revitStyles[revitName] = candidate;
        }

        // The View Control Bar's words, each beside Revit's name for the same
        // style. A word whose name this release does not have is not offered.
        var barWords = new[]
        {
            new[] { "Wireframe", "Wireframe" },
            new[] { "Hidden Line", "HLR" },
            new[] { "Shaded", "Shading" },
            new[] { "Shaded with Edges", "ShadingWithEdges" },
            new[] { "Consistent Colors", "FlatColors" },
            new[] { "Textures", "Textures" },
            new[] { "Realistic", "Realistic" },
            new[] { "Realistic with Edges", "RealisticWithEdges" },
        };

        // Spaces, capitals and the British spelling do not decide a match.
        Func<string, string> squash = text =>
            new string((text ?? "").Where(char.IsLetterOrDigit).ToArray())
                .ToLowerInvariant().Replace("colours", "colors");

        // A style read back, in the word a modeller sees for it.
        Func<DisplayStyle, string> barWord = style =>
        {
            foreach (var pair in barWords)
                if (string.Equals(pair[1], style.ToString(), StringComparison.Ordinal)) return pair[0];
            return style.ToString();
        };

        DisplayStyle? styleWanted = null;
        var keepEdges = false;
        var styleReadBack = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var styleRefused = new List<string>();

        if (wantStyle.Length > 0)
        {
            var key = squash(wantStyle);
            foreach (var pair in barWords)
            {
                if (squash(pair[0]) != key || !revitStyles.ContainsKey(pair[1])) continue;
                styleWanted = revitStyles[pair[1]];
                keepEdges = pair[0] == "Shaded" || pair[0] == "Realistic";
                break;
            }

            if (styleWanted == null)
            {
                foreach (var entry in revitStyles)
                {
                    if (squash(entry.Key) != key) continue;
                    styleWanted = entry.Value;
                    break;
                }
            }

            if (styleWanted == null)
            {
                var offered = "Say one of the words Revit's View Control Bar shows: "
                    + string.Join(", ", barWords.Where(p => revitStyles.ContainsKey(p[1])).Select(p => p[0]))
                    + " - or Revit's own name for one: " + string.Join(", ", revitStyles.Keys) + ".";

                visualStyleNote = key == "raytrace"
                    ? "Revit does not let a tool set a view to Ray Trace - its API refuses it on Revit 2020, "
                        + "because Ray Trace puts the view in a restricted mode, and has no value for it from "
                        + "2021 - so the visual style was not touched on any view. Where Revit offers Ray Trace, "
                        + "choose it in the View Control Bar by hand. " + offered
                    : "No visual style is called \"" + wantStyle + "\", so the visual style was not touched "
                        + "on any view. " + offered;
                findings.Add(visualStyleNote);
            }
        }

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

            if (styleWanted != null)
            {
                try
                {
                    var target = styleWanted.Value;
                    if (keepEdges)
                    {
                        var before = view.DisplayStyle;
                        var edgesShown = before == DisplayStyle.ShadingWithEdges
                                      || before == DisplayStyle.RealisticWithEdges;
                        if (edgesShown && target == DisplayStyle.Shading) target = DisplayStyle.ShadingWithEdges;
                        else if (edgesShown && target == DisplayStyle.Realistic) target = DisplayStyle.RealisticWithEdges;
                    }

                    view.DisplayStyle = target;
                    var now = view.DisplayStyle;
                    if (now == target)
                    {
                        styleSet++;
                        touched = true;
                        var shown = barWord(now);
                        styleReadBack[shown] = (styleReadBack.ContainsKey(shown) ? styleReadBack[shown] : 0) + 1;
                    }
                    else styleRefused.Add(string.Format("'{0}': the visual style did not take - asked {1}, "
                        + "reads {2}", view.Name, barWord(target), barWord(now)));
                }
                catch (Exception ex)
                {
                    styleRefused.Add(string.Format("'{0}' visual style: {1}", view.Name, ex.Message));
                }
            }

            if (touched) changed++;
        }

        findings.Add(string.Format("{0} of {1} view(s) changed in some way: scale set on {2}, detail "
            + "level on {3}, visual style on {4}. Each property was set on its own, so a view that "
            + "took one and refused another is counted here and named below",
            changed, views.Count, scaleSet, detailSet, styleSet));

        // WHICH STYLE EACH VIEW READS BACK, whole, because "Shaded" can come
        // back with or without edges and the finding above is cut short.
        if (styleWanted != null)
        {
            visualStyleNote = "\"" + wantStyle + "\" read back on " + styleSet + " of " + views.Count
                + " view(s)"
                + (styleReadBack.Count == 0 ? "" : ": " + string.Join(", ",
                    styleReadBack.Select(e => e.Key + " on " + e.Value)))
                + (keepEdges ? ". Show Edges was left as each view had it - say \"" + barWord(styleWanted.Value)
                    + " with Edges\" to turn edges on" : "")
                + (styleRefused.Count == 0 ? "" : ". Refused: " + string.Join("; ", styleRefused))
                + ".";
        }

        refused.AddRange(styleRefused);
        foreach (var failure in refused) findings.Add("Refused: " + failure);

        if (refused.Count > 0)
            findings.Add("A view controlled by a VIEW TEMPLATE refuses the properties that template "
                + "holds, and that is the commonest reason for a refusal here. "
                + "REPORT_VIEW_TEMPLATE_CONTROL says which properties those are");
    }
}
