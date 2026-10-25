// NOT STANDALONE. Assumes `doc`, `sourceView`, `targetViews` and
// `sunSettings` are in scope; leaves `viewsCopied`, `sunCopied`, `readBack`,
// `blockedByTemplate`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry:
// the template made, the copy onto every view and the template's removal are
// all inside the caller's transaction.
//
// WHY A VIEW TEMPLATE. Cast Shadows, Show Ambient Shadows, Ambient Light and
// Photographic Exposure have no member on any release 2020 to 2027 - read off
// every release's RevitAPI.xml on 2026-10-24, and SET_GRAPHIC_DISPLAY_OPTIONS
// says the same. But a view template carries the WHOLE Graphic Display Options
// dialog as one setting, GRAPHIC_DISPLAY_OPTIONS, and
// ApplyViewTemplateParameters copies what a template includes onto a view
// once, leaving no link. So the modeller ticks Cast Shadows once by hand and
// this spreads every tick in the dialog - those four included - to the rest.
//
// HOW. A source VIEW: CreateViewTemplate makes a template of it, its include
// list is narrowed to Graphic Display Options alone, it is applied to each
// target, then deleted - no template is left behind. A source TEMPLATE (one
// the project already has) is used as it is: its include list is narrowed for
// the copy and put back afterwards, read back. A source view GOVERNED by a
// template that includes Graphic Display Options shows that template's
// settings, so that template is the source.
//
// A TARGET GOVERNED BY A TEMPLATE THAT INCLUDES GRAPHIC DISPLAY OPTIONS WILL
// NOT TAKE THEM - Revit applies only what the target's own template leaves
// free. Such views are NAMED with their template, never counted.
//
// READ BACK. What the API can read - the visual style, sun and shadow
// intensity, silhouettes, smooth lines, transparency, hidden lines, sketchy
// lines and the background type - is read on each target and compared with
// the source; a view counts only when every one matches. The four with no
// member cannot be read: the answer says so and names the views to look at.
//
// THE SUN COMES TOO, UNLESS sunSettings IS "keep". The modeller asked on
// 2026-10-24 for every setting, the sun included. Whether a template carries
// a view's Sun Setting is not known from the API reference, so the source
// view's own sun is copied member by member - the study, its dates and times,
// sunrise to sunset, the interval, the angles, relative to view and the
// ground plane - and read back on each target. A template as the source has
// no sun of its own to give: said, not guessed.
//
// A TEMPLATE THAT CANNOT BE DELETED OR PUT BACK THROWS, so the caller's
// transaction rolls everything back - Article 8, nothing half-done kept.

var viewsCopied = 0;
var sunCopied = 0;
var readBack = "";
var blockedByTemplate = new List<string>();
string refused = null;
var findings = new List<string>();

var displayOptionsId = new ElementId(BuiltInParameter.GRAPHIC_DISPLAY_OPTIONS);

// What the API can read of one view's Graphic Display Options, as one line.
Func<View, string> readable = v =>
{
    try
    {
        var model = v.GetViewDisplayModel();
        var sketchy = v.GetSketchyLines();
        return "style " + v.DisplayStyle + ", sun " + v.SunlightIntensity + ", shadow " + v.ShadowIntensity
            + ", silhouettes " + (model.EnableSilhouettes ? "on" : "off") + ", smooth lines " + (model.SmoothEdges ? "on" : "off")
            + ", transparency " + model.Transparency + ", hidden lines " + model.ShowHiddenLines
            + ", sketchy " + (sketchy.EnableSketchyLines ? "on " + sketchy.Jitter + "/" + sketchy.Extension : "off")
            + ", background " + v.GetBackground().Type;
    }
    catch (Exception ex) { return "(not readable: " + ex.Message + ")"; }
};
var copySun = (sunSettings ?? "").Trim().ToLowerInvariant() != "keep";

// Does this template hold Graphic Display Options - included, not left free?
Func<View, bool> includesDisplayOptions = t =>
    t != null && t.GetTemplateParameterIds().Contains(displayOptionsId)
    && !t.GetNonControlledTemplateParameterIds().Contains(displayOptionsId);

// ---- the source ----
View template = null;
var madeTemplate = false;
var sourceWord = "";
if (sourceView == null) refused = "No source view was named, so NOTHING WAS CHANGED.";
else if (sourceView.IsTemplate)
{
    if (!sourceView.GetTemplateParameterIds().Contains(displayOptionsId))
        refused = "The view template '" + sourceView.Name + "' holds no Graphic Display Options. NOTHING WAS CHANGED.";
    else
    {
        template = sourceView;
        sourceWord = "the view template '" + sourceView.Name + "'";
    }
}
else if (!sourceView.HasDisplayStyle())
    refused = "'" + sourceView.Name + "' has no Graphic Display Options - Revit gives them to views with a visual style. "
        + "NOTHING WAS CHANGED.";
else
{
    var governing = doc.GetElement(sourceView.ViewTemplateId) as View;
    if (includesDisplayOptions(governing))
    {
        template = governing;
        sourceWord = "the view template '" + governing.Name + "', which governs '" + sourceView.Name + "'";
        findings.Add("'" + sourceView.Name + "' shows the Graphic Display Options of its view template '" + governing.Name
            + "', so those were copied");
    }
    else if (!sourceView.IsViewValidForTemplateCreation())
        refused = "Revit makes no view template from '" + sourceView.Name + "', so its Graphic Display Options cannot be "
            + "carried. NOTHING WAS CHANGED.";
    else sourceWord = "'" + sourceView.Name + "'";
}

// ---- the targets, each checked before anything is made ----
var targets = new List<View>();
if (refused == null)
{
    var skipped = new List<string>();
    foreach (var view in targetViews ?? new List<View>())
    {
        if (view == null) continue;
        if (view.Id == sourceView.Id || (template != null && view.Id == template.Id)) continue;
        if (view.IsTemplate) { skipped.Add("'" + view.Name + "' is a view template - name a view"); continue; }
        if (!view.HasDisplayStyle()) { skipped.Add("'" + view.Name + "' has no Graphic Display Options"); continue; }
        var own = doc.GetElement(view.ViewTemplateId) as View;
        if (includesDisplayOptions(own))
        {
            blockedByTemplate.Add("'" + view.Name + "' - its view template '" + own.Name + "' holds its Graphic Display "
                + "Options; copy onto that template's views by changing the template, or untick Graphic Display Options in it");
            continue;
        }
        if (!targets.Any(t => t.Id == view.Id)) targets.Add(view);
    }
    if (skipped.Count > 0) findings.Add("Not copied to: " + string.Join("; ", skipped));
    if (targets.Count == 0)
        refused = "No view named can take Graphic Display Options from " + sourceWord
            + (blockedByTemplate.Count > 0 ? " - every one is held by its own view template" : "") + ". NOTHING WAS CHANGED.";
}

// ---- copy ----
if (refused == null)
{
    if (template == null)
    {
        template = sourceView.CreateViewTemplate();
        madeTemplate = true;
    }
    var wasFree = template.GetNonControlledTemplateParameterIds().ToList();
    var allButDisplay = template.GetTemplateParameterIds().Where(id => id != displayOptionsId).ToList();
    template.SetNonControlledTemplateParameterIds(allButDisplay);

    var want = readable(madeTemplate ? sourceView : template);

    // ---- the sun, member by member ----
    SunAndShadowSettings sourceSun = null;
    if (copySun && !sourceView.IsTemplate)
    {
        try { sourceSun = sourceView.SunAndShadowSettings; } catch (Exception) { sourceSun = null; }
    }
    if (copySun && sourceSun == null)
        findings.Add(sourceView.IsTemplate
            ? "A view template has no Sun Setting of its own, so the sun was not copied - name a view as the source for that"
            : "'" + sourceView.Name + "' gives Revit's API no sun settings, so the sun was not copied");
    // Returns null when every sun member reads back as the source's; else why not.
    Func<View, string> copySunTo = view =>
    {
        SunAndShadowSettings sun = null;
        try { sun = view.SunAndShadowSettings; } catch (Exception) { sun = null; }
        if (sun == null) return "'" + view.Name + "' gives Revit's API no sun settings - its sun was not copied";
        if (sun.Id == sourceSun.Id) return null;
        var refusedBits = new List<string>();
        Action<string, Action> attempt = (what, act) =>
        {
            try { act(); }
            catch (Exception ex) { refusedBits.Add(what + " (" + ex.Message + ")"); }
        };
        attempt("study", () => sun.SunAndShadowType = sourceSun.SunAndShadowType);
        attempt("start", () => sun.StartDateAndTime = DateTime.SpecifyKind(sourceSun.StartDateAndTime, DateTimeKind.Utc));
        attempt("end", () => sun.EndDateAndTime = DateTime.SpecifyKind(sourceSun.EndDateAndTime, DateTimeKind.Utc));
        attempt("sunrise to sunset", () => sun.SunriseToSunset = sourceSun.SunriseToSunset);
        if (sun.IsTimeIntervalValid(sourceSun.TimeInterval)) attempt("interval", () => sun.TimeInterval = sourceSun.TimeInterval);
        attempt("azimuth", () => sun.Azimuth = sourceSun.Azimuth);
        attempt("altitude", () => sun.Altitude = sourceSun.Altitude);
        attempt("relative to view", () => sun.RelativeToView = sourceSun.RelativeToView);
        attempt("ground plane", () =>
        {
            sun.UsesGroundPlane = sourceSun.UsesGroundPlane;
            if (sourceSun.UsesGroundPlane && sun.IsGroundPlaneLevelValid(sourceSun.GroundPlaneLevelId))
                sun.GroundPlaneLevelId = sourceSun.GroundPlaneLevelId;
        });
        var differs = new List<string>();
        if (sun.SunAndShadowType != sourceSun.SunAndShadowType) differs.Add("study");
        if (sun.StartDateAndTime != sourceSun.StartDateAndTime) differs.Add("start");
        if (sourceSun.SunAndShadowType != SunAndShadowType.StillImage && sourceSun.SunAndShadowType != SunAndShadowType.Lighting)
        {
            if (sun.EndDateAndTime != sourceSun.EndDateAndTime) differs.Add("end");
            if (sun.SunriseToSunset != sourceSun.SunriseToSunset) differs.Add("sunrise to sunset");
            if (sun.TimeInterval != sourceSun.TimeInterval) differs.Add("interval");
        }
        if (sourceSun.SunAndShadowType == SunAndShadowType.Lighting)
        {
            if (Math.Abs(sun.Azimuth - sourceSun.Azimuth) > 1e-6) differs.Add("azimuth");
            if (Math.Abs(sun.Altitude - sourceSun.Altitude) > 1e-6) differs.Add("altitude");
            if (sun.RelativeToView != sourceSun.RelativeToView) differs.Add("relative to view");
        }
        if (sun.UsesGroundPlane != sourceSun.UsesGroundPlane
            || (sourceSun.UsesGroundPlane && sun.GroundPlaneLevelId != sourceSun.GroundPlaneLevelId)) differs.Add("ground plane");
        if (differs.Count == 0 && refusedBits.Count == 0) return null;
        return "'" + view.Name + "' did not take the whole sun of '" + sourceView.Name + "'"
            + (differs.Count > 0 ? " - reads differently: " + string.Join(", ", differs) : "")
            + (refusedBits.Count > 0 ? " - Revit refused: " + string.Join("; ", refusedBits) : "");
    };
    var lines = new List<string>();
    foreach (var view in targets)
    {
        var before = readable(view);
        try { view.ApplyViewTemplateParameters(template); }
        catch (Exception ex)
        {
            findings.Add("Revit would not copy the Graphic Display Options onto '" + view.Name + "': " + ex.Message);
            continue;
        }
        var after = readable(view);
        if (copySun && sourceSun != null)
        {
            var sunNote = copySunTo(view);
            if (sunNote == null) sunCopied++;
            else findings.Add(sunNote);
        }
        if (after == want)
        {
            viewsCopied++;
            lines.Add("'" + view.Name + "': " + after + (before == after ? " (readable settings were already the same)" : ""));
        }
        else findings.Add("'" + view.Name + "' did not read back as " + sourceWord + ": it reads " + after + ", the source "
            + want);
    }

    // ---- leave nothing behind ----
    if (madeTemplate)
    {
        var madeId = template.Id;
        doc.Delete(madeId);
        if (doc.GetElement(madeId) != null)
            throw new InvalidOperationException("The working view template could not be removed, so the whole copy was "
                + "taken back. NOTHING WAS CHANGED.");
    }
    else
    {
        template.SetNonControlledTemplateParameterIds(wasFree);
        var nowFree = template.GetNonControlledTemplateParameterIds();
        if (nowFree.Count != wasFree.Count || wasFree.Any(id => !nowFree.Contains(id)))
            throw new InvalidOperationException("The view template '" + template.Name + "' could not be put back as it was, "
                + "so the whole copy was taken back. NOTHING WAS CHANGED.");
    }

    readBack = "From " + sourceWord + " (" + want + ") to " + viewsCopied + " of " + targets.Count + " view(s)"
        + (lines.Count > 0 ? ": " + string.Join("; ", lines) : "") + ". Sun "
        + (!copySun ? "left as each view had it (sunSettings keep)"
            : sourceSun == null ? "not copied" : "copied to " + sunCopied + " of " + targets.Count + " view(s)") + ".";
    findings.Add(viewsCopied + " of " + targets.Count + " view(s) read back as " + sourceWord
        + ". Cast Shadows, Ambient Shadows, Ambient Light and Photographic Exposure travelled with the rest, but Revit's API "
        + "cannot read them - look at the views to see them");
}
if (blockedByTemplate.Count > 0) findings.Add(blockedByTemplate.Count + " view(s) held by their own view template");
if (refused != null) findings.Add(refused);
