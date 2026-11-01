// NOT STANDALONE. Assumes `doc`, `views`, `scale`, `detailLevel`,
// `visualStyle`, `phaseId`, `phaseFilterId`, `topMm`, `cutMm` and `bottomMm`
// are in scope; leaves `changed`, `findings`, `visualStyleNote`, `wasScale`,
// `alreadyThatScale`, `alreadyThatDetailLevel`, `alreadyThatVisualStyle`,
// `refused`, `phaseSet`, `phaseFilterSet`, `alreadySet`, `templateControlled`,
// `unsupported`, `wasViewRange`, `wasTopMm`, `wasCutMm`, `wasBottomMm`,
// `viewRangeSet` and `alreadyThatViewRange` behind.
//
// VERSION 4 (2026-11-01) - SET_VIEW_PHASE AND SET_VIEW_RANGE MERGED IN, on the
// owner's instruction ("make sure there is no duplicate tools"). Both set a
// view property across views, refused what a template holds and read the value
// back - this card's job. What each had that this lacked:
//
//   FROM SET_VIEW_PHASE - `phaseId` and `phaseFilterId`. THIS CHANGES WHAT THE
//   DRAWING SHOWS, NOT THE BUILDING: SET_ELEMENT_PHASE moves an element to a
//   phase; this changes which phase a VIEW looks from. AN ID THAT IS NOT A
//   PHASE, OR NOT A PHASE FILTER, IS REFUSED BEFORE ANYTHING IS WRITTEN - an
//   element id parameter set to an id of the wrong kind is accepted by some
//   views and leaves a view pointing at nonsense. The two are SET SEPARATELY
//   BECAUSE THEY FAIL SEPARATELY - a schedule has a phase and no phase filter,
//   a drafting view and a legend have neither - and each is read back. A view
//   already so goes to `alreadySet`, never counted as set; one whose parameter
//   is read-only, or reads back unchanged, goes to `templateControlled` - a
//   template holding it is much the commonest reason, and a refusal of the
//   filter is a refusal whether or not the phase took (SET_VIEW_PHASE's own
//   fix of 2026-09-13); a view carrying neither goes to `unsupported`.
//
//   FROM SET_VIEW_RANGE - `topMm`, `cutMm` and `bottomMm`, a plan's view range
//   offsets from the level each plane is measured from, in millimetres. THE
//   RANGE IS READ BEFORE IT IS CHANGED, ALWAYS - `wasViewRange` for every plan,
//   and `wasTopMm`, `wasCutMm` and `wasBottomMm` for the first, SET_VIEW_RANGE's
//   three, so a caller can say what it was and put it back. A PLAN ONLY: a
//   section, an elevation or a 3D view has no view range and is refused by
//   name. READ BACK FROM THE VIEW: Revit rejects an incoherent range - a cut
//   above the top - without always throwing. No level is changed.
//
//   THE SAFER RULE OF EACH CONFLICT WAS KEPT. SET_VIEW_RANGE wrote all three
//   offsets on every call; here an offset left out (`keep`) is not touched,
//   this card's rule that anything not given is not touched. SET_VIEW_RANGE's
//   read mode (`apply` false) did not come: reading a view range is
//   REPORT_VIEW_SETTINGS', a read, as reading a scale is. A VIEW TEMPLATE
//   handed in is refused here as it always was - SET_VIEW_PHASE would have
//   tried to write to one.
//
// VERSION 3, REVIEW FIX OF 2026-11-01 - THE SAME MISCOUNT, FIXED FOR THE OTHER
// TWO. A view already at the detail level asked, or already in the visual
// style asked, was written again and counted as changed - "changed 12" over
// twelve views that were right already, the fault SET_VIEW_SCALE's rule fixed
// for scale. Each is now read first: already so, it is not written and not
// counted, and is counted instead in `alreadyThatDetailLevel` or
// `alreadyThatVisualStyle` - two new counts beside `alreadyThatScale`, not
// that one widened, so a caller of the scale count reads what it read. For
// "Shaded", already so means the style that keeps the view's own edges.
//
// VERSION 3 (2026-11-01) - SET_VIEW_SCALE MERGED IN, on the owner's
// instruction, and what it had that this lacked came with it:
//
//   wasScale          each view's scale BEFORE this ran, read before any write,
//                     so a batch can be put back by a caller that kept it
//   alreadyThatScale  a view already at the scale asked is NOT written and NOT
//                     counted as changed. Version 2 wrote the same number back
//                     and counted the view, so "changed 12" could mean twelve
//                     views already right
//   refused           the ids of the views that refused anything asked - a
//                     template, a property Revit would not take - each once, so
//                     a caller can act on WHICH, beside the sentences in findings
//   a view named twice is handled once, and counted once
//
// SET_VIEW_SCALE's read mode (`apply` false) did NOT come here: reading a
// view's scale is REPORT_VIEW_SETTINGS' job (READ_VIEW_SCALE's until 2026-11-01), a read, and a write card must not be
// the answer to "what scale is this view".
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
// Raytrace on Revit 2020, absent from 2021 to 2023, and Textures from 2024, so
// either name typed here would fail to compile somewhere in the range.
//
// "SHADED" LEAVES SHOW EDGES AS THE VIEW HAS IT. In Revit, Show Edges is its
// own tick box in Graphic Display Options, and the View Control Bar changes
// the style without touching it. The API folds the two together - Shading or
// ShadingWithEdges - so a view already showing edges keeps them, and "Shaded
// with Edges" asks for them outright. "REALISTIC" DOES NOT: it is also the
// API's own name, and it keeps the meaning version 1 gave it - Realistic with
// no edges - because otherwise no word could take edges off a realistic view.
//
// AN UNKNOWN WORD IS REFUSED ONCE, NOT ONCE PER VIEW, and the refusal lists
// every word this Revit takes. It is also left whole in `visualStyleNote`,
// because a finding is cut to sixty characters on its way back and the list is
// the part the caller needs.

var changed = 0;
var findings = new List<string>();
var visualStyleNote = "";
var wasScale = new Dictionary<ElementId, int>();
var alreadyThatScale = 0;
var alreadyThatDetailLevel = 0;
var alreadyThatVisualStyle = 0;
var refused = new List<ElementId>();
var phaseSet = new List<ElementId>();
var phaseFilterSet = new List<ElementId>();
var alreadySet = new List<ElementId>();
var templateControlled = new List<ElementId>();
var unsupported = new List<ElementId>();
var wasViewRange = "";
var wasTopMm = 0.0;
var wasCutMm = 0.0;
var wasBottomMm = 0.0;
var viewRangeSet = 0;
var alreadyThatViewRange = 0;

if (views == null || views.Count == 0)
{
    findings.Add("No views were given");
}
else
{
    // `keep` is what leaving either out reads as (version 4, so a call that
    // asks only for a phase or a view range need not type the other three).
    var wantDetail = string.IsNullOrEmpty(detailLevel) ? "" : detailLevel.Trim();
    var wantStyle = string.IsNullOrEmpty(visualStyle) ? "" : visualStyle.Trim();
    if (string.Equals(wantDetail, "keep", StringComparison.OrdinalIgnoreCase)) wantDetail = "";
    if (string.Equals(wantStyle, "keep", StringComparison.OrdinalIgnoreCase)) wantStyle = "";
    var wantScale = scale > 0;

    // VERSION 4, FROM SET_VIEW_PHASE. InvalidElementId - `none`, left out -
    // means leave that property alone. BOTH IDS ARE RESOLVED AND CHECKED
    // BEFORE ANYTHING IS WRITTEN: an id of the wrong kind is refused here and
    // never reaches a view.
    var wantPhase = phaseId != null && phaseId != ElementId.InvalidElementId;
    var wantPhaseFilter = phaseFilterId != null && phaseFilterId != ElementId.InvalidElementId;
    var phaseRefused = false;
    if (wantPhase && !(doc.GetElement(phaseId) is Phase))
    {
        findings.Add("The id given for the phase is not a Phase in this model. No phase is written - a view "
            + "pointing at something that is not a phase is worse than a view left alone. Nothing was changed.");
        phaseRefused = true;
    }
    if (wantPhaseFilter && !(doc.GetElement(phaseFilterId) is PhaseFilter))
    {
        findings.Add("The id given for the phase filter is not a Phase Filter in this model. No phase filter is "
            + "written. Nothing was changed.");
        phaseRefused = true;
    }

    // VERSION 4, FROM SET_VIEW_RANGE. Millimetres from the level each plane
    // is measured from; `keep`, or nothing, leaves that plane as it is.
    double? wantTop = null, wantCut = null, wantBottom = null;
    var rangeProblem = "";
    Func<string, string, double?> offsetOf = (text, plane) =>
    {
        var t = (text ?? "").Trim();
        if (t.Length == 0 || string.Equals(t, "keep", StringComparison.OrdinalIgnoreCase)) return null;
        double value;
        if (double.TryParse(t.Replace("mm", "").Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value) && !double.IsInfinity(value))
            return value;
        if (rangeProblem.Length == 0)
            rangeProblem = "\"" + t + "\" is not a " + plane + " offset - millimetres from the level it is measured "
                + "from, such as 1200, or keep. Nothing was changed.";
        return null;
    };
    wantTop = offsetOf(topMm, "top");
    wantCut = offsetOf(cutMm, "cut plane");
    wantBottom = offsetOf(bottomMm, "bottom");
    var wantRange = wantTop != null || wantCut != null || wantBottom != null;
    if (rangeProblem.Length > 0) findings.Add(rangeProblem);

    if (phaseRefused || rangeProblem.Length > 0)
    {
        // REFUSED BEFORE ANYTHING IS WRITTEN - on any property, so a call
        // that asked for a wrong id never half-changes a drawing set.
    }
    else if (!wantScale && wantDetail.Length == 0 && wantStyle.Length == 0 && !wantPhase && !wantPhaseFilter
        && !wantRange)
    {
        findings.Add("Nothing was asked for - give a scale, a detail level, a visual style, a phase, a phase "
            + "filter or a view range offset. Anything left out is deliberately not touched");
    }
    else
    {
        var scaleSet = 0;
        var detailSet = 0;
        var styleSet = 0;
        var refusals = new List<string>();

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
                keepEdges = pair[0] == "Shaded";
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

        // A VIEW NAMED TWICE IS HANDLED ONCE (version 3, from SET_VIEW_SCALE),
        // so it is set once, counted once and named once.
        var handled = new HashSet<ElementId>();
        var rangesBefore = new List<string>();
        var firstRangeRead = false;
        Func<ViewPlan, PlanViewPlane, double> offsetMm = (plan, plane) =>
            plan.GetViewRange().GetOffset(plane) * 304.8;
        var refusedOnce = new HashSet<ElementId>();

        foreach (var view in views)
        {
            if (view == null) continue;
            if (!handled.Add(view.Id)) continue;
            if (view.IsTemplate)
            {
                // Into the style's own list when a style was asked, so the whole
                // note says why this view did not take it.
                var notAView = string.Format("'{0}' is a template, not a view", view.Name);
                if (styleWanted != null) styleRefused.Add(notAView);
                else refusals.Add(notAView);
                if (refusedOnce.Add(view.Id)) refused.Add(view.Id);
                continue;
            }

            var touched = false;
            var refusalsBefore = refusals.Count + styleRefused.Count;

            if (wantScale)
            {
                // READ FIRST (version 3, from SET_VIEW_SCALE): the old scale is
                // kept for the caller, and a view already at the scale asked is
                // neither written nor counted as changed.
                var was = 0;
                var readable = true;
                try { was = view.Scale; }
                catch (Exception ex)
                {
                    readable = false;
                    refusals.Add(string.Format("'{0}' scale: Revit would not give its scale - {1}",
                        view.Name, ex.Message));
                }

                if (readable)
                {
                    wasScale[view.Id] = was;
                    if (was == scale)
                    {
                        alreadyThatScale++;
                    }
                    else
                    {
                        try
                        {
                            view.Scale = scale;
                            if (view.Scale == scale) { scaleSet++; touched = true; }
                            else refusals.Add(string.Format("'{0}': the scale did not take {1}", view.Name, scale));
                        }
                        catch (Exception ex)
                        {
                            refusals.Add(string.Format("'{0}' scale: {1}", view.Name, ex.Message));
                        }
                    }
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
                        refusals.Add(string.Format("'{0}': detail level must be Coarse, Medium or Fine - "
                            + "'{1}' is none of them", view.Name, wantDetail));
                    else if (view.DetailLevel == wanted)
                    {
                        // ALREADY SO (review fix): not written, not counted as changed.
                        alreadyThatDetailLevel++;
                    }
                    else
                    {
                        view.DetailLevel = wanted;
                        if (view.DetailLevel == wanted) { detailSet++; touched = true; }
                        else refusals.Add(string.Format("'{0}': the detail level did not take", view.Name));
                    }
                }
                catch (Exception ex)
                {
                    refusals.Add(string.Format("'{0}' detail level: {1}", view.Name, ex.Message));
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
                        if (edgesShown) target = DisplayStyle.ShadingWithEdges;
                    }

                    if (view.DisplayStyle == target)
                    {
                        // ALREADY SO (review fix): not written, not counted as
                        // changed - and said in the note with the others.
                        alreadyThatVisualStyle++;
                        var held = barWord(target) + " (already)";
                        styleReadBack[held] = (styleReadBack.ContainsKey(held) ? styleReadBack[held] : 0) + 1;
                    }
                    else
                    {
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
                }
                catch (Exception ex)
                {
                    styleRefused.Add(string.Format("'{0}' visual style: {1}", view.Name, ex.Message));
                }
            }

            // VERSION 4, FROM SET_VIEW_PHASE. Each property on its own, each
            // read back. A refusal of either is a refusal whether or not the
            // other went through.
            if (wantPhase || wantPhaseFilter)
            {
                var carriedNeither = true;
                var phaseHeld = false;
                var phaseAlready = false;
                var pairs = new List<KeyValuePair<BuiltInParameter, ElementId>>();
                if (wantPhase) pairs.Add(new KeyValuePair<BuiltInParameter, ElementId>(BuiltInParameter.VIEW_PHASE, phaseId));
                if (wantPhaseFilter)
                    pairs.Add(new KeyValuePair<BuiltInParameter, ElementId>(BuiltInParameter.VIEW_PHASE_FILTER, phaseFilterId));
                foreach (var pair in pairs)
                {
                    Parameter parameter = null;
                    try { parameter = view.get_Parameter(pair.Key); }
                    catch (Exception) { parameter = null; }
                    if (parameter == null) continue;
                    carriedNeither = false;
                    try
                    {
                        if (parameter.IsReadOnly) { phaseHeld = true; continue; }
                        if (parameter.AsElementId() == pair.Value) { phaseAlready = true; continue; }
                        parameter.Set(pair.Value);
                        if (parameter.AsElementId() == pair.Value)
                        {
                            if (pair.Key == BuiltInParameter.VIEW_PHASE) phaseSet.Add(view.Id);
                            else phaseFilterSet.Add(view.Id);
                            touched = true;
                        }
                        else phaseHeld = true;
                    }
                    catch (Exception ex)
                    {
                        phaseHeld = true;
                        refusals.Add(string.Format("'{0}' {1}: {2}", view.Name,
                            pair.Key == BuiltInParameter.VIEW_PHASE ? "phase" : "phase filter", ex.Message));
                    }
                }
                if (carriedNeither) unsupported.Add(view.Id);
                else
                {
                    if (phaseAlready && !alreadySet.Contains(view.Id)) alreadySet.Add(view.Id);
                    if (phaseHeld)
                    {
                        templateControlled.Add(view.Id);
                        refusals.Add(string.Format("'{0}': the phase or the phase filter was refused - written and "
                            + "read back unchanged, or read-only", view.Name));
                    }
                }
            }

            // VERSION 4, FROM SET_VIEW_RANGE. A plan only; read BEFORE it is
            // changed, and read back from the view afterwards.
            if (wantRange)
            {
                var plan = view as ViewPlan;
                if (plan == null)
                {
                    refusals.Add(string.Format("'{0}' ({1}) has no view range - a plan only; a section, an "
                        + "elevation and a 3D view have none", view.Name, view.ViewType));
                }
                else
                {
                    try
                    {
                        var wasTop = offsetMm(plan, PlanViewPlane.TopClipPlane);
                        var wasCut = offsetMm(plan, PlanViewPlane.CutPlane);
                        var wasBottom = offsetMm(plan, PlanViewPlane.BottomClipPlane);
                        if (!firstRangeRead)
                        {
                            wasTopMm = wasTop; wasCutMm = wasCut; wasBottomMm = wasBottom;
                            firstRangeRead = true;
                        }
                        var invariant = System.Globalization.CultureInfo.InvariantCulture;
                        rangesBefore.Add(string.Format(invariant, "'{0}': top {1:0.#} mm, cut plane {2:0.#} mm, "
                            + "bottom {3:0.#} mm", view.Name, wasTop, wasCut, wasBottom));

                        Func<double?, double, bool> same = (asked, was) => asked == null || Math.Abs(asked.Value - was) < 0.5;
                        if (same(wantTop, wasTop) && same(wantCut, wasCut) && same(wantBottom, wasBottom))
                        {
                            // ALREADY SO: not written, not counted as changed.
                            alreadyThatViewRange++;
                        }
                        else
                        {
                            var range = plan.GetViewRange();
                            if (wantTop != null) range.SetOffset(PlanViewPlane.TopClipPlane, wantTop.Value / 304.8);
                            if (wantCut != null) range.SetOffset(PlanViewPlane.CutPlane, wantCut.Value / 304.8);
                            if (wantBottom != null) range.SetOffset(PlanViewPlane.BottomClipPlane, wantBottom.Value / 304.8);
                            plan.SetViewRange(range);

                            // READ BACK from the view, not from the object handed in.
                            Func<double?, PlanViewPlane, bool> took = (asked, plane) =>
                                asked == null || Math.Abs(offsetMm(plan, plane) - asked.Value) < 1.0;
                            if (took(wantTop, PlanViewPlane.TopClipPlane) && took(wantCut, PlanViewPlane.CutPlane)
                                && took(wantBottom, PlanViewPlane.BottomClipPlane))
                            {
                                viewRangeSet++;
                                touched = true;
                            }
                            else refusals.Add(string.Format("'{0}': the view range did not read back as asked - Revit "
                                + "rejects an incoherent range, a cut plane above the top or a bottom above the cut, "
                                + "without always saying so", view.Name));
                        }
                    }
                    catch (Exception ex)
                    {
                        refusals.Add(string.Format("'{0}' view range: {1}", view.Name, ex.Message));
                    }
                }
            }

            if (touched) changed++;
            if (refusals.Count + styleRefused.Count > refusalsBefore && refusedOnce.Add(view.Id))
                refused.Add(view.Id);
        }

        findings.Add(string.Format("{0} of {1} view(s) changed in some way: scale set on {2}, detail "
            + "level on {3}, visual style on {4}. Each property was set on its own, so a view that "
            + "took one and refused another is counted here and named below",
            changed, handled.Count, scaleSet, detailSet, styleSet));

        // A VIEW ALREADY AT THE SCALE ASKED is said, never counted as set
        // (version 3) - "scale set on 0" beside twelve views already at 1:50
        // would otherwise read as twelve refusals.
        if (alreadyThatScale > 0)
            findings.Add(string.Format("{0} view(s) were already at 1:{1}, so their scale was left as "
                + "it was and not counted as changed", alreadyThatScale, scale));
        // And the same for the other two (review fix, 2026-11-01).
        if (alreadyThatDetailLevel > 0)
            findings.Add(string.Format("{0} view(s) were already at detail level {1}, so it was left as "
                + "it was and not counted as changed", alreadyThatDetailLevel, wantDetail));
        if (alreadyThatVisualStyle > 0)
            findings.Add(string.Format("{0} view(s) were already in the visual style asked, so it was "
                + "left as it was and not counted as changed", alreadyThatVisualStyle));

        // WHICH STYLE EACH VIEW READS BACK, whole, because "Shaded" can come
        // back with or without edges and the finding above is cut short.
        if (styleWanted != null)
        {
            visualStyleNote = "\"" + wantStyle + "\" read back on " + (styleSet + alreadyThatVisualStyle)
                + " of " + handled.Count + " view(s)"
                + (alreadyThatVisualStyle > 0 ? " (" + alreadyThatVisualStyle + " already so, not written)" : "")
                + (styleReadBack.Count == 0 ? "" : ": " + string.Join(", ",
                    styleReadBack.Select(e => e.Key + " on " + e.Value)))
                + (keepEdges ? ". Show Edges was left as each view had it - say \"" + barWord(styleWanted.Value)
                    + " with Edges\" to turn edges on" : "")
                + (styleRefused.Count == 0 ? "" : ". Refused: " + string.Join("; ", styleRefused))
                + ".";
        }

        // VERSION 4: the phase, the phase filter and the view range, said.
        if (wantPhase || wantPhaseFilter)
            findings.Add(string.Format("Phase set on {0} view(s), phase filter on {1}; {2} already as asked, {3} "
                + "refused (a VIEW TEMPLATE holding the property is much the commonest reason - REPORT_VIEW_TEMPLATE_"
                + "CONTROL says which), {4} carry no view phase at all - a drafting view, a legend, a sheet. This "
                + "changes what the DRAWING shows, never the building - SET_ELEMENT_PHASE moves elements",
                phaseSet.Count, phaseFilterSet.Count, alreadySet.Count, templateControlled.Count, unsupported.Count));
        if (wantRange)
        {
            wasViewRange = string.Join("  ||  ", rangesBefore.ToArray());
            findings.Add(string.Format("View range set on {0} plan(s), {1} already as asked. What it was BEFORE: {2}",
                viewRangeSet, alreadyThatViewRange, rangesBefore.Count == 0 ? "no plan was named" : wasViewRange));
        }

        refusals.AddRange(styleRefused);
        foreach (var failure in refusals) findings.Add("Refused: " + failure);

        if (refusals.Count > 0)
            findings.Add("A view controlled by a VIEW TEMPLATE refuses the properties that template "
                + "holds, and that is the commonest reason for a refusal here. "
                + "REPORT_VIEW_TEMPLATE_CONTROL says which properties those are");
    }
}
