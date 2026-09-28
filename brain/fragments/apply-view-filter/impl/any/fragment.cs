// NOT STANDALONE. Assumes `doc`, `view`, `filter`, `overrides`, `visible`,
// `enabled`, `keepOtherSettings` and `solidFill` are in scope; leaves
// `applied`, `findings` and `summary` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ONE ROW OF THE FILTERS TAB. Visibility/Graphics puts a filter on one row:
// Enable Filter, Visibility, Projection/Surface Lines and Patterns,
// Transparency, Cut Lines and Patterns, Halftone. Every one of them is set
// here - the graphic ones arrive in `overrides`, already built by the add-in's
// parser; the two ticks and the solid fill are values of their own.
//
// EITHER KIND OF FILTER WORKS. A rule-based view filter and a saved selection
// filter share a base class, and a view does not care which it was given.
//
// ALREADY ON THE VIEW IS AN UPDATE, NOT A FAILURE. Adding it twice throws;
// somebody adjusting a colour wants the second call to take.
//
// A BLANK LEAVES THINGS AS THEY ARE. With keepOtherSettings blank or true,
// only what `overrides` sets is changed and everything else the filter already
// had on this view stays - so "halftone the return air filter" does not wipe
// its colours. keepOtherSettings=false is today's older behaviour: the
// override replaces the filter's whole override, which is how a colour is
// cleared or halftone switched back off. The same blank rule holds for
// `enabled` and `solidFill`.
//
// HALFTONE, TRANSPARENCY AND THE VISIBLE TICKS HAVE NO BLANK. Off, 0 and
// ticked are both Revit's default and a value, so in keep mode a default in
// `overrides` cannot switch one back. The summary names what was kept, and
// keepOtherSettings=false is the way back.
//
// CUT IS NOT STAMPED WHERE IT CANNOT SHOW. When none of the filter's
// categories can be cut, Revit greys the Cut columns out - and a cut value set
// through the API anyway is ACCEPTED, stored, read back faithfully and draws
// nothing (the owner's point, and FRAGMENT-ISSUES row 175 measured it on
// Ducts). So the cut half is left out and the categories are named.
//
// ENABLE FILTER ARRIVED AT REVIT 2021 - read off the 2020 and 2021 reference
// assemblies, not remembered. It is reached BY NAME at run time rather than
// under `#if REVIT2020`, because the add-in compiles this with no release
// symbols (FRAGMENT-ISSUES 5b-181) and a `#if` would name a member Revit 2020
// has not got. On 2020 a non-blank `enabled` refuses the whole call.
//
// THE VIEW DECIDES, NOT THE CALL. Everything is read back off the view after
// it is written, and a setting Revit did not keep is named.

var applied = false;
var findings = new List<string>();
var summary = "";

// ---- the pieces every step below uses ---------------------------------------

var blank = new OverrideGraphicSettings();
var solidLine = LinePatternElement.GetSolidPatternId();

Func<Color, string> rgb = c => c != null && c.IsValid
    ? string.Format("{0},{1},{2}", c.Red, c.Green, c.Blue) : "no override";

Func<ElementId, string> patternName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "no override";
    if (id == solidLine) return "Solid";
    var e = doc.GetElement(id);
    return e == null ? "pattern " + id : e.Name;
};

// THE ROW, IN THE DIALOG'S ORDER, as label and value. Compared before and
// after, so "what changed" is read off the view rather than off the request.
Func<OverrideGraphicSettings, List<KeyValuePair<string, string>>> row = s =>
{
    var r = new List<KeyValuePair<string, string>>();
    Action<string, string> add = (k, v) => r.Add(new KeyValuePair<string, string>(k, v));
    add("projection line pattern", patternName(s.ProjectionLinePatternId));
    add("projection line colour", rgb(s.ProjectionLineColor));
    add("projection line weight", s.ProjectionLineWeight == blank.ProjectionLineWeight
        ? "no override" : s.ProjectionLineWeight.ToString());
    add("surface foreground pattern", patternName(s.SurfaceForegroundPatternId));
    add("surface foreground colour", rgb(s.SurfaceForegroundPatternColor));
    add("surface foreground visible", s.IsSurfaceForegroundPatternVisible ? "ticked" : "UNTICKED");
    add("surface background pattern", patternName(s.SurfaceBackgroundPatternId));
    add("surface background colour", rgb(s.SurfaceBackgroundPatternColor));
    add("surface background visible", s.IsSurfaceBackgroundPatternVisible ? "ticked" : "UNTICKED");
    add("transparency", s.Transparency.ToString());
    add("cut line pattern", patternName(s.CutLinePatternId));
    add("cut line colour", rgb(s.CutLineColor));
    add("cut line weight", s.CutLineWeight == blank.CutLineWeight
        ? "no override" : s.CutLineWeight.ToString());
    add("cut foreground pattern", patternName(s.CutForegroundPatternId));
    add("cut foreground colour", rgb(s.CutForegroundPatternColor));
    add("cut foreground visible", s.IsCutForegroundPatternVisible ? "ticked" : "UNTICKED");
    add("cut background pattern", patternName(s.CutBackgroundPatternId));
    add("cut background colour", rgb(s.CutBackgroundPatternColor));
    add("cut background visible", s.IsCutBackgroundPatternVisible ? "ticked" : "UNTICKED");
    add("halftone", s.Halftone ? "on" : "off");
    add("detail level", s.DetailLevel == blank.DetailLevel ? "by view" : s.DetailLevel.ToString());
    return r;
};

// EVERYTHING `over` SETS, written onto a copy of `under`. What `over` leaves
// at Revit's default is taken from `under` - which is the whole of "a blank
// leaves it as it is".
Func<OverrideGraphicSettings, OverrideGraphicSettings, OverrideGraphicSettings> merge = (under, over) =>
{
    var m = new OverrideGraphicSettings(under);
    if (over.ProjectionLinePatternId != blank.ProjectionLinePatternId) m.SetProjectionLinePatternId(over.ProjectionLinePatternId);
    if (over.ProjectionLineColor.IsValid) m.SetProjectionLineColor(over.ProjectionLineColor);
    if (over.ProjectionLineWeight != blank.ProjectionLineWeight) m.SetProjectionLineWeight(over.ProjectionLineWeight);
    if (over.SurfaceForegroundPatternId != blank.SurfaceForegroundPatternId) m.SetSurfaceForegroundPatternId(over.SurfaceForegroundPatternId);
    if (over.SurfaceForegroundPatternColor.IsValid) m.SetSurfaceForegroundPatternColor(over.SurfaceForegroundPatternColor);
    if (over.IsSurfaceForegroundPatternVisible != blank.IsSurfaceForegroundPatternVisible) m.SetSurfaceForegroundPatternVisible(over.IsSurfaceForegroundPatternVisible);
    if (over.SurfaceBackgroundPatternId != blank.SurfaceBackgroundPatternId) m.SetSurfaceBackgroundPatternId(over.SurfaceBackgroundPatternId);
    if (over.SurfaceBackgroundPatternColor.IsValid) m.SetSurfaceBackgroundPatternColor(over.SurfaceBackgroundPatternColor);
    if (over.IsSurfaceBackgroundPatternVisible != blank.IsSurfaceBackgroundPatternVisible) m.SetSurfaceBackgroundPatternVisible(over.IsSurfaceBackgroundPatternVisible);
    if (over.Transparency != blank.Transparency) m.SetSurfaceTransparency(over.Transparency);
    if (over.CutLinePatternId != blank.CutLinePatternId) m.SetCutLinePatternId(over.CutLinePatternId);
    if (over.CutLineColor.IsValid) m.SetCutLineColor(over.CutLineColor);
    if (over.CutLineWeight != blank.CutLineWeight) m.SetCutLineWeight(over.CutLineWeight);
    if (over.CutForegroundPatternId != blank.CutForegroundPatternId) m.SetCutForegroundPatternId(over.CutForegroundPatternId);
    if (over.CutForegroundPatternColor.IsValid) m.SetCutForegroundPatternColor(over.CutForegroundPatternColor);
    if (over.IsCutForegroundPatternVisible != blank.IsCutForegroundPatternVisible) m.SetCutForegroundPatternVisible(over.IsCutForegroundPatternVisible);
    if (over.CutBackgroundPatternId != blank.CutBackgroundPatternId) m.SetCutBackgroundPatternId(over.CutBackgroundPatternId);
    if (over.CutBackgroundPatternColor.IsValid) m.SetCutBackgroundPatternColor(over.CutBackgroundPatternColor);
    if (over.IsCutBackgroundPatternVisible != blank.IsCutBackgroundPatternVisible) m.SetCutBackgroundPatternVisible(over.IsCutBackgroundPatternVisible);
    if (over.Halftone != blank.Halftone) m.SetHalftone(over.Halftone);
    if (over.DetailLevel != blank.DetailLevel) m.SetDetailLevel(over.DetailLevel);
    return m;
};

// The cut half of `source` put back onto `target` - how a cut value that
// cannot show is taken OUT of what is about to be written.
Action<OverrideGraphicSettings, OverrideGraphicSettings> cutFrom = (target, source) =>
{
    target.SetCutLinePatternId(source.CutLinePatternId);
    target.SetCutLineColor(source.CutLineColor.IsValid ? source.CutLineColor : Color.InvalidColorValue);
    target.SetCutLineWeight(source.CutLineWeight);
    target.SetCutForegroundPatternId(source.CutForegroundPatternId);
    target.SetCutForegroundPatternColor(source.CutForegroundPatternColor.IsValid
        ? source.CutForegroundPatternColor : Color.InvalidColorValue);
    target.SetCutForegroundPatternVisible(source.IsCutForegroundPatternVisible);
    target.SetCutBackgroundPatternId(source.CutBackgroundPatternId);
    target.SetCutBackgroundPatternColor(source.CutBackgroundPatternColor.IsValid
        ? source.CutBackgroundPatternColor : Color.InvalidColorValue);
    target.SetCutBackgroundPatternVisible(source.IsCutBackgroundPatternVisible);
};

Func<OverrideGraphicSettings, bool> setsCut = s =>
    s.CutLinePatternId != blank.CutLinePatternId || s.CutLineColor.IsValid
    || s.CutLineWeight != blank.CutLineWeight
    || s.CutForegroundPatternId != blank.CutForegroundPatternId || s.CutForegroundPatternColor.IsValid
    || s.IsCutForegroundPatternVisible != blank.IsCutForegroundPatternVisible
    || s.CutBackgroundPatternId != blank.CutBackgroundPatternId || s.CutBackgroundPatternColor.IsValid
    || s.IsCutBackgroundPatternVisible != blank.IsCutBackgroundPatternVisible;

// A tick typed as words: 1 on, 0 off, -1 blank, -2 not a tick at all.
Func<string, int> tick = text =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    if (t.Length == 0) return -1;
    if (t == "true" || t == "yes" || t == "on" || t == "ticked") return 1;
    if (t == "false" || t == "no" || t == "off" || t == "unticked") return 0;
    return -2;
};

// ENABLE FILTER, BY NAME. Null on a release that has not got it - see header.
var setEnabled = typeof(View).GetMethod("SetIsFilterEnabled", new[] { typeof(ElementId), typeof(bool) });
var getEnabled = typeof(View).GetMethod("GetIsFilterEnabled", new[] { typeof(ElementId) });

Func<string> enabledNow = () =>
{
    if (getEnabled == null) return "not in this Revit release";
    try { return (bool)getEnabled.Invoke(view, new object[] { filter.Id }) ? "ON" : "OFF"; }
    catch { return "could not be read"; }
};

// ---- what was asked, read before anything is written -------------------------

string refusal = null;

var wantEnabled = tick(enabled);
var keepText = (keepOtherSettings ?? "").Trim();
var keepTick = tick(keepText);
var keep = keepTick != 0;
var solidSurface = false;
var solidCut = false;

if (filter == null)
{
    refusal = "No filter was given - make one first with CREATE_VIEW_FILTER for a rule, or "
        + "CREATE_SELECTION_FILTER for a hand-picked list.";
}
else if (wantEnabled == -2)
{
    refusal = string.Format("enabled=\"{0}\" is not a tick. Type true to tick Enable Filter, false to "
        + "untick it, or leave it blank to leave it as it is.", enabled);
}
else if (keepTick == -2)
{
    refusal = string.Format("keepOtherSettings=\"{0}\" is not true or false. Leave it blank or true "
        + "to change only what the override sets; false to replace the filter's whole override.",
        keepOtherSettings);
}
else if (wantEnabled >= 0 && setEnabled == null)
{
    refusal = string.Format("Revit {0} has no Enable Filter tick - it arrived at Revit 2021 - so "
        + "enabled={1} cannot be set and NOTHING was changed. On this release every filter on a view "
        + "is always in effect; leave enabled blank.", doc.Application.VersionNumber, enabled.Trim());
}
else
{
    foreach (var word in (solidFill ?? "").ToLowerInvariant()
                 .Split(new[] { ',', ';', '+', ' ' }, StringSplitOptions.RemoveEmptyEntries))
    {
        if (word == "surface" || word == "projection") solidSurface = true;
        else if (word == "cut") solidCut = true;
        else if (word == "both") { solidSurface = true; solidCut = true; }
        else if (word == "none" || word == "and") { }
        else
        {
            refusal = string.Format("solidFill=\"{0}\" is not a half of the filter. Type surface, cut or "
                + "both - or leave it blank. It sets Revit's solid fill as the FOREGROUND pattern.",
                solidFill);
            break;
        }
    }
}

FillPatternElement solid = null;
if (refusal == null && (solidSurface || solidCut))
{
    // FOUND BY WHAT IT IS, never by an id - the same lookup SET_CATEGORY_SOLID_FILL
    // and the add-in's parser make. A drafting pattern, because every override
    // pattern slot says it must be one.
    solid = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement))
        .Cast<FillPatternElement>()
        .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill
                          && f.GetFillPattern().Target == FillPatternTarget.Drafting);
    if (solid == null)
        refusal = "This model has no solid drafting fill pattern, so solidFill has nothing to put on. "
            + "Every Revit template ships one; a model without it has had it purged. Nothing was changed.";
    else if (solidSurface && overrides.SurfaceForegroundPatternId != blank.SurfaceForegroundPatternId
             && overrides.SurfaceForegroundPatternId != solid.Id)
        refusal = string.Format("The override names surface foreground pattern '{0}' and solidFill asks for "
            + "solid - say it once. Nothing was changed.", patternName(overrides.SurfaceForegroundPatternId));
    else if (solidCut && overrides.CutForegroundPatternId != blank.CutForegroundPatternId
             && overrides.CutForegroundPatternId != solid.Id)
        refusal = string.Format("The override names cut foreground pattern '{0}' and solidFill asks for "
            + "solid - say it once. Nothing was changed.", patternName(overrides.CutForegroundPatternId));
}

if (refusal != null)
{
    findings.Add(refusal);
    summary = "NOTHING CHANGED. " + refusal;
}
else
{
    try
    {
        // ---- which of the filter's categories can be cut ----------------------
        var categoryIds = new List<ElementId>();
        var asRule = filter as ParameterFilterElement;
        var asSet = filter as SelectionFilterElement;
        if (asRule != null) categoryIds.AddRange(asRule.GetCategories());
        else if (asSet != null)
        {
            foreach (var id in asSet.GetElementIds())
            {
                var member = doc.GetElement(id);
                if (member != null && member.Category != null && !categoryIds.Contains(member.Category.Id))
                    categoryIds.Add(member.Category.Id);
            }
        }

        var cuttable = new List<string>();
        var notCuttable = new List<string>();
        foreach (var id in categoryIds)
        {
            Category category = null;
            try { category = Category.GetCategory(doc, id); } catch { category = null; }
            if (category == null) continue;
            if (category.IsCuttable) cuttable.Add(category.Name); else notCuttable.Add(category.Name);
        }

        // ---- the view as it stands ---------------------------------------------
        var alreadyOn = false;
        foreach (var id in view.GetFilters())
        {
            // ElementId to ElementId. Never as a number.
            if (id == filter.Id) { alreadyOn = true; break; }
        }

        var before = alreadyOn ? view.GetFilterOverrides(filter.Id) : blank;
        var beforeVisible = alreadyOn ? (view.GetFilterVisibility(filter.Id) ? "ON" : "OFF") : "";
        var beforeEnabled = alreadyOn ? enabledNow() : "";

        // ---- what will be written ----------------------------------------------
        // `asked` is the request alone, solid fill included; `wanted` is it
        // written over what stays.
        var asked = merge(blank, overrides);
        if (solidSurface) asked.SetSurfaceForegroundPatternId(solid.Id);
        if (solidCut) asked.SetCutForegroundPatternId(solid.Id);

        var baseline = keep && alreadyOn ? before : blank;
        var wanted = merge(baseline, asked);

        var cutNote = "";
        if (setsCut(asked))
        {
            if (cuttable.Count == 0)
            {
                // Taken OUT, not stamped. What the view already had stays in keep
                // mode; the caller's cut values are dropped.
                cutFrom(wanted, baseline);
                cutNote = categoryIds.Count == 0
                    ? "cut not applicable: this filter has no categories at all, so nothing it matches can be cut. The cut values given were NOT set."
                    : "cut not applicable to this filter's categories: " + string.Join(", ", notCuttable)
                      + ". None of them can be cut, so Revit greys the Cut Lines and Cut Patterns columns out "
                      + "for this filter. The cut values given were NOT set.";
                findings.Add(cutNote);
            }
            else if (notCuttable.Count > 0)
            {
                cutNote = "cut set; it shows only on " + string.Join(", ", cuttable) + ". "
                    + string.Join(", ", notCuttable) + " cannot be cut, so it does nothing to them.";
                findings.Add(cutNote);
            }
        }

        // ---- write ----------------------------------------------------------------
        if (!alreadyOn) view.AddFilter(filter.Id);
        view.SetFilterOverrides(filter.Id, wanted);
        view.SetFilterVisibility(filter.Id, visible);
        if (wantEnabled >= 0) setEnabled.Invoke(view, new object[] { filter.Id, wantEnabled == 1 });

        // ---- READ IT BACK OFF THE VIEW. The view is what decides, not the call. ---
        var stuck = false;
        foreach (var id in view.GetFilters()) if (id == filter.Id) { stuck = true; break; }

        applied = stuck;

        if (!stuck)
        {
            var why = string.Format("'{0}' was accepted by '{1}' and is not on it - a view template "
                + "usually controls the filters", filter.Name, view.Name);
            findings.Add(why);
            summary = "NOTHING KEPT. " + why + ".";
        }
        else
        {
            var after = view.GetFilterOverrides(filter.Id);
            var afterVisible = view.GetFilterVisibility(filter.Id) ? "ON" : "OFF";
            var afterEnabled = enabledNow();

            var rowBefore = row(before);
            var rowAfter = row(after);
            var rowWanted = row(wanted);
            var rowGiven = row(asked);
            var rowBlank = row(blank);

            var changed = new List<string>();
            var dropped = new List<string>();
            var kept = new List<string>();

            if (alreadyOn && beforeEnabled != afterEnabled)
                changed.Add("Enable Filter " + beforeEnabled + " -> " + afterEnabled);
            if (alreadyOn && beforeVisible != afterVisible)
                changed.Add("Visibility " + beforeVisible + " -> " + afterVisible);

            for (int i = 0; i < rowAfter.Count; i++)
            {
                var label = rowAfter[i].Key;
                if (alreadyOn && rowBefore[i].Value != rowAfter[i].Value)
                    changed.Add(label + " " + rowBefore[i].Value + " -> " + rowAfter[i].Value);
                if (rowWanted[i].Value != rowAfter[i].Value)
                    dropped.Add(label + " asked " + rowWanted[i].Value + ", reads " + rowAfter[i].Value);
                if (keep && alreadyOn && rowGiven[i].Value == rowBlank[i].Value
                    && rowAfter[i].Value != rowBlank[i].Value)
                    kept.Add(label + " " + rowAfter[i].Value);
            }

            var parts = new List<string>();
            parts.Add(string.Format("'{0}' {1} on '{2}'. {3}, Visibility {4}.",
                filter.Name, alreadyOn ? (changed.Count == 0 ? "was already there" : "was already there and was updated") : "is now", view.Name,
                getEnabled == null ? "No Enable Filter tick in this release"
                    : "Enable Filter " + afterEnabled + (wantEnabled < 0 ? " (left as it was)" : ""),
                afterVisible));

            if (!alreadyOn)
            {
                var set = new List<string>();
                foreach (var pair in rowAfter)
                    if (pair.Value != "no override" && pair.Value != "ticked" && pair.Value != "0"
                        && pair.Value != "off" && pair.Value != "by view")
                        set.Add(pair.Key + " " + pair.Value);
                parts.Add(set.Count == 0 ? "No graphic override." : "Set: " + string.Join("; ", set) + ".");
            }
            else
            {
                parts.Add(changed.Count == 0
                    ? "NOTHING CHANGED - the filter already had exactly these settings on this view."
                    : "Changed: " + string.Join("; ", changed) + ".");
            }

            if (kept.Count > 0)
                parts.Add("Kept as the filter already had them: " + string.Join("; ", kept)
                    + ". To switch one of those back, run again with keepOtherSettings=false and the whole override.");
            if (cutNote.Length > 0) parts.Add(cutNote);
            if (dropped.Count > 0)
            {
                var lost = "REVIT DID NOT KEEP: " + string.Join("; ", dropped) + ".";
                parts.Add(lost);
                findings.Add(lost);
            }

            findings.Insert(0, string.Format("'{0}' {1} on '{2}', and what it matches is {3}",
                filter.Name, alreadyOn ? (changed.Count == 0 ? "was already there" : "was already there and was updated") : "is now",
                view.Name, visible ? "visible" : "HIDDEN in this view"));

            // Only where cut can show - otherwise the missing cut colour is the
            // point, not a mistake.
            if (cuttable.Count > 0 && after.ProjectionLineColor.IsValid && !after.CutLineColor.IsValid)
            {
                var warn = "WARNING: the override sets a projection colour and NO cut colour. Revit "
                    + "derives neither from the other, so this will look right in plan and show the "
                    + "wrong colour the moment anything is cut";
                findings.Add(warn);
                parts.Add(warn + ".");
            }

            summary = string.Join(" ", parts);
        }
    }
    catch (Exception ex)
    {
        var reason = ex is System.Reflection.TargetInvocationException && ex.InnerException != null
            ? ex.InnerException.Message : ex.Message;
        var failed = string.Format("'{0}' could not be applied to '{1}': {2}", filter.Name, view.Name, reason);
        findings.Add(failed);
        summary = "NOTHING KEPT. " + failed;
    }
}
