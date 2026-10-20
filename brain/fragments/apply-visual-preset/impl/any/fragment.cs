// NOT STANDALONE. Assumes `doc`, `view`, `preset`, `renderFor` and `sunTime`
// are in scope; leaves `changed`, `alreadySo`, `skipped`, `readBack`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A WHOLE LOOK IN ONE CALL. A preset names the settings a visualiser sets
// together - visual style, display lighting, edges, sun, render lighting,
// background, exposure, render quality and output - and this sets every one
// the view does not already have, then reads them all back.
//
// NOTHING HERE IS NEW TO HERON. Each setting is written with the same Revit
// members the single tool for it calls - SET_VIEW_PROPERTIES,
// SET_GRAPHIC_DISPLAY_OPTIONS, SET_SUN_SETTINGS and SET_RENDER_SETTINGS - and
// read with the same members REPORT_RENDER_SETTINGS reads.
//
// A SETTING THE VIEW OR REVIT'S API CANNOT TAKE IS SKIPPED AND NAMED, NEVER
// FAKED. A floor plan has no Rendering Settings; only an elevation or a
// section has depth cueing; Cast Shadows and Ambient Shadows have no member on
// any release 2020 to 2027 (row 5b-1267), so they are named as the modeller's
// to tick.
//
// THE PRESETS, AND HOW EACH SETTING IS READ, ARE THE SHARED BLOCK BELOW - the
// same text as REPORT_VISUAL_PRESET's, so what the read says a preset would do
// is what this does.
//
// READ BACK: only a setting that reads as the preset says after the call is
// counted in `changed`.

var changed = 0;
var alreadySo = 0;
var skipped = 0;
var readBack = "";
string refused = null;
var findings = new List<string>();

// ==== SHARED BLOCK - begin ====
// The same text, byte for byte, in APPLY_VISUAL_PRESET and
// REPORT_VISUAL_PRESET - tests/test_visual_presets.py holds the two together,
// because two copies of a preset is how one of them goes stale. Everything in
// this block only READS the model.
//
// THE PRESETS are Heron's starting looks (D-33) - what a visualiser tries
// first, never a standard; the picture is the judge. Each setting is the word
// Heron's single tool for it takes, read with the members that tool reads:
//   Visual Style                                    SET_VIEW_PROPERTIES
//   Sun Intensity, Shadow Intensity, Silhouettes,
//   Smooth Lines, Sketchy Lines, Depth Cueing,
//   Display Background                              SET_GRAPHIC_DISPLAY_OPTIONS
//   Sun                                             SET_SUN_SETTINGS
//   Render Quality, Render Output, Render Lighting,
//   Render Background, Exposure                     SET_RENDER_SETTINGS
//   Cast Shadows, Ambient Shadows                   none - no member on any
//                                                   release 2020 to 2027: by hand
// "by purpose" is renderFor's: check is Draft on Screen, issue is High at
// 300 DPI. Sun "still sunset-30" is half an hour before Revit's own sunset
// for the site on the view's date (SunAndShadowSettings.GetSunset);
// "lighting 135/35 relative to view" lights from the top right whatever the
// site, 225/35 from the top left. EXPOSURE: a higher value is a darker image.
var presetTable = new List<KeyValuePair<string, string[][]>>
{
    new KeyValuePair<string, string[][]>("Exterior Day", new[]
    {
        new[] { "Visual Style", "Realistic" }, new[] { "Sun Intensity", "80" }, new[] { "Shadow Intensity", "50" },
        new[] { "Silhouettes", "off" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Display Background", "sky" }, new[] { "Sun", "still 10:00" },
        new[] { "Render Quality", "by purpose" }, new[] { "Render Output", "by purpose" },
        new[] { "Render Lighting", "Exterior: Sun only" }, new[] { "Render Background", "Sky: Few Clouds" },
        new[] { "Exposure", "value=14; white point=6500" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
    new KeyValuePair<string, string[][]>("Exterior Dusk", new[]
    {
        new[] { "Visual Style", "Realistic" }, new[] { "Sun Intensity", "40" }, new[] { "Shadow Intensity", "70" },
        new[] { "Silhouettes", "off" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Display Background", "gradient sky=40,55,100; horizon=235,160,100; ground=70,70,70" },
        new[] { "Sun", "still sunset-30" },
        new[] { "Render Quality", "by purpose" }, new[] { "Render Output", "by purpose" },
        new[] { "Render Lighting", "Exterior: Sun and Artificial" }, new[] { "Render Background", "Sky: Very Few Clouds" },
        new[] { "Exposure", "value=11; white point=6500" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
    new KeyValuePair<string, string[][]>("Interior Day", new[]
    {
        new[] { "Visual Style", "Realistic" }, new[] { "Sun Intensity", "60" }, new[] { "Shadow Intensity", "40" },
        new[] { "Silhouettes", "off" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Display Background", "sky" }, new[] { "Sun", "still 10:00" },
        new[] { "Render Quality", "by purpose" }, new[] { "Render Output", "by purpose" },
        new[] { "Render Lighting", "Interior: Sun and Artificial" }, new[] { "Render Background", "Sky: Few Clouds" },
        new[] { "Exposure", "value=10; white point=6500" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
    new KeyValuePair<string, string[][]>("Interior Night", new[]
    {
        new[] { "Visual Style", "Realistic" }, new[] { "Sun Intensity", "30" }, new[] { "Shadow Intensity", "30" },
        new[] { "Silhouettes", "off" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Display Background", "gradient sky=20,25,40; horizon=40,45,70; ground=30,30,30" },
        new[] { "Render Quality", "by purpose" }, new[] { "Render Output", "by purpose" },
        new[] { "Render Lighting", "Interior: Artificial only" }, new[] { "Render Background", "colour 20,25,40" },
        new[] { "Exposure", "value=8; white point=4000" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
    new KeyValuePair<string, string[][]>("Equipment Close-up", new[]
    {
        new[] { "Visual Style", "Realistic" }, new[] { "Sun Intensity", "80" }, new[] { "Shadow Intensity", "40" },
        new[] { "Silhouettes", "off" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Display Background", "gradient sky=235,235,235; horizon=250,250,250; ground=205,205,205" },
        new[] { "Sun", "lighting 135/35 relative to view" },
        new[] { "Render Quality", "by purpose" }, new[] { "Render Output", "by purpose" },
        new[] { "Render Lighting", "Exterior: Sun only" }, new[] { "Render Background", "colour 245,245,245" },
        new[] { "Exposure", "value=13; white point=6500" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
    new KeyValuePair<string, string[][]>("Clay", new[]
    {
        new[] { "Visual Style", "Hidden Line" }, new[] { "Sun Intensity", "80" }, new[] { "Shadow Intensity", "60" },
        new[] { "Silhouettes", "on" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Display Background", "gradient sky=255,255,255; horizon=255,255,255; ground=235,235,235" },
        new[] { "Sun", "lighting 135/35 relative to view" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
    new KeyValuePair<string, string[][]>("Presentation Elevation", new[]
    {
        new[] { "Visual Style", "Shaded with Edges" }, new[] { "Sun Intensity", "80" }, new[] { "Shadow Intensity", "60" },
        new[] { "Silhouettes", "on" }, new[] { "Smooth Lines", "on" }, new[] { "Sketchy Lines", "off" },
        new[] { "Depth Cueing", "start=0; end=100; fade=50" },
        new[] { "Sun", "lighting 225/35 relative to view" },
        new[] { "Cast Shadows", "on" }, new[] { "Ambient Shadows", "on" },
    }),
};
// What each preset is made for, said when the view is another kind.
var madeFor = new Dictionary<string, string>
{
    { "Exterior Day", "3d" }, { "Exterior Dusk", "3d" }, { "Interior Day", "3d" }, { "Interior Night", "3d" },
    { "Equipment Close-up", "3d" }, { "Clay", "3d elevation section" }, { "Presentation Elevation", "elevation section" },
};
var byHandKeys = new[] { "Cast Shadows", "Ambient Shadows" };

var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant()
        .Replace("colours", "colors").Replace("colour", "color");
Func<double, string> show = value => Math.Round(value, 3).ToString(culture);
Func<double, string> degrees = radians => Math.Round(radians * 180.0 / Math.PI).ToString(culture);
Func<Color, string> showColour = c => c == null || !c.IsValid ? "none" : c.Red + "," + c.Green + "," + c.Blue;
// "start=0; end=100" as key=value pairs, keys squashed.
Func<string, Dictionary<string, string>> pairs = text =>
{
    var found = new Dictionary<string, string>();
    foreach (var part in (text ?? "").Split(';'))
    {
        var bits = part.Split(new[] { '=' }, 2);
        if (bits.Length == 2) found[squash(bits[0])] = bits[1].Trim();
    }
    return found;
};
Func<string, Color> colour = text =>
{
    var parts = (text ?? "").Trim().Split(',');
    if (parts.Length != 3) return null;
    var rgb = new byte[3];
    for (var i = 0; i < 3; i++)
    {
        byte b;
        if (!byte.TryParse(parts[i].Trim(), out b)) return null;
        rgb[i] = b;
    }
    return new Color(rgb[0], rgb[1], rgb[2]);
};

// ---- the inputs, every one read before anything else ----
string inputProblem = null;
var forIssue = false;
var purposeWord = squash(renderFor);
if (purposeWord == "issue") forIssue = true;
else if (purposeWord.Length > 0 && purposeWord != "check")
    inputProblem = "'" + renderFor + "' is not what the render is for. Say check (Draft quality on Screen) or issue "
        + "(High quality at 300 DPI). NOTHING WAS CHANGED.";
TimeSpan? askedTime = null;
var timeWord = (sunTime ?? "").Trim();
if (inputProblem == null && timeWord.Length > 0 && squash(timeWord) != "preset")
{
    DateTime t;
    if (DateTime.TryParseExact(timeWord.ToUpperInvariant(), new[] { "H:mm", "HH:mm", "h:mm tt", "h:mmtt", "h tt", "htt" },
            culture, System.Globalization.DateTimeStyles.None, out t))
        askedTime = t.TimeOfDay;
    else inputProblem = "'" + sunTime + "' is not a time Heron can read - say 10:00, 15:30 or 3 pm. NOTHING WAS CHANGED.";
}
Func<string, string> presetNamed = word =>
{
    foreach (var entry in presetTable) if (squash(entry.Key) == squash(word)) return entry.Key;
    return null;
};
var presetList = string.Join(", ", presetTable.Select(p => p.Key));

// ---- the view ----
if (inputProblem == null && view == null) inputProblem = "No view was named. NOTHING WAS CHANGED.";
else if (inputProblem == null && view.IsTemplate)
    inputProblem = "'" + view.Name + "' is a view template; a preset is given to a view. NOTHING WAS CHANGED.";
var view3D = inputProblem == null ? view as View3D : null;
var hasDisplay = inputProblem == null && view.HasDisplayStyle();
SunAndShadowSettings sun = null;
if (inputProblem == null)
{
    try { sun = view.SunAndShadowSettings; } catch (Exception) { sun = null; }
}
var takesBackground = inputProblem == null && (view.ViewType == ViewType.ThreeD || view.ViewType == ViewType.Elevation
    || view.ViewType == ViewType.Section);
var viewKind = inputProblem == null ? (view.ViewType == ViewType.ThreeD ? "3d" : view.ViewType == ViewType.Elevation
    ? "elevation" : view.ViewType == ViewType.Section ? "section" : view.ViewType.ToString().ToLowerInvariant()) : "";
var site = doc.SiteLocation;
Func<DateTime, DateTime> toProject = utc => site.ConvertToProjectTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
Func<DateTime, DateTime> toUtc = project =>
    DateTime.SpecifyKind(site.ConvertFromProjectTime(DateTime.SpecifyKind(project, DateTimeKind.Unspecified)), DateTimeKind.Utc);
var noDisplay = inputProblem == null ? "'" + view.Name + "' has no Graphic Display Options - Revit gives them to views with "
    + "a visual style" : "";
var noRender = inputProblem == null ? "'" + view.Name + "' is not a 3D view, and only a 3D view has Rendering Settings" : "";

// ---- the words each setting is read in ----
// Visual styles by NAME at run time, as SET_VIEW_PROPERTIES finds them: value 9
// is Raytrace on 2020 and Textures from 2024, so no member is typed here.
var revitStyles = new Dictionary<string, DisplayStyle>(StringComparer.OrdinalIgnoreCase);
foreach (DisplayStyle candidate in Enum.GetValues(typeof(DisplayStyle)))
{
    var revitName = candidate.ToString();
    if (revitName == "Undefined" || revitName == "Raytrace" || revitStyles.ContainsKey(revitName)) continue;
    revitStyles[revitName] = candidate;
}
var barWords = new[]
{
    new[] { "Wireframe", "Wireframe" }, new[] { "Hidden Line", "HLR" }, new[] { "Shaded", "Shading" },
    new[] { "Shaded with Edges", "ShadingWithEdges" }, new[] { "Consistent Colors", "FlatColors" },
    new[] { "Textures", "Textures" }, new[] { "Realistic", "Realistic" },
    new[] { "Realistic with Edges", "RealisticWithEdges" },
};
Func<DisplayStyle, string> barWord = style =>
{
    foreach (var pair in barWords) if (pair[1] == style.ToString()) return pair[0];
    return style.ToString();
};
Func<RenderingQuality, string> qualityWord = q => q == RenderingQuality.VeryHigh ? "Best" : q.ToString();
Func<LightingSource, string> lightingWord = l =>
{
    switch (l)
    {
        case LightingSource.ExteriorSun: return "Exterior: Sun only";
        case LightingSource.ExteriorSunAndArtificial: return "Exterior: Sun and Artificial";
        case LightingSource.ExteriorArtificial: return "Exterior: Artificial only";
        case LightingSource.InteriorSun: return "Interior: Sun only";
        case LightingSource.InteriorSunAndArtificial: return "Interior: Sun and Artificial";
        default: return "Interior: Artificial only";
    }
};
Func<BackgroundStyle, string> styleWord = s =>
{
    switch (s)
    {
        case BackgroundStyle.SkyNoClouds: return "Sky: No Clouds";
        case BackgroundStyle.SkyVeryFewClouds: return "Sky: Very Few Clouds";
        case BackgroundStyle.SkyFewClouds: return "Sky: Few Clouds";
        case BackgroundStyle.SkyCloudy: return "Sky: Cloudy";
        case BackgroundStyle.SkyVeryCloudy: return "Sky: Very Cloudy";
        default: return s.ToString();
    }
};

// ---- one setting as the view holds it: { value, why it was skipped } ----
Func<string, string[]> settingNow = key =>
{
    try
    {
        if (key == "Sun")
        {
            if (sun == null) return new[] { "", "'" + view.Name + "' has no sun settings" };
            if (sun.SunAndShadowType == SunAndShadowType.StillImage)
                return new[] { "still " + toProject(sun.StartDateAndTime).ToString("HH:mm", culture), null };
            if (sun.SunAndShadowType == SunAndShadowType.Lighting)
                return new[] { "lighting " + degrees(sun.Azimuth) + "/" + degrees(sun.Altitude) + " relative to "
                    + (sun.RelativeToView ? "view" : "model"), null };
            return new[] { (sun.SunAndShadowType == SunAndShadowType.OneDayStudy ? "single day" : "multi-day") + " study", null };
        }
        if (key.StartsWith("Render") || key == "Exposure")
        {
            if (view3D == null) return new[] { "", noRender };
            var s = view3D.GetRenderingSettings();
            if (key == "Render Quality") return new[] { qualityWord(s.GetRenderingQualitySettings().RenderingQuality), null };
            if (key == "Render Output")
                return new[] { s.ResolutionTarget == ResolutionTarget.Screen ? "Screen" : s.ResolutionValue + " DPI", null };
            if (key == "Render Lighting") return new[] { lightingWord(s.LightingSource), null };
            if (key == "Render Background")
            {
                var colourBg = s.GetBackgroundSettings() as ColorBackgroundSettings;
                return new[] { s.BackgroundStyle == BackgroundStyle.Color && colourBg != null ? "colour " + showColour(colourBg.Color)
                    : styleWord(s.BackgroundStyle), null };
            }
            var e = s.GetRenderingImageExposureSettings();
            return new[] { "value=" + show(e.ExposureValue) + "; white point=" + show(e.WhitePoint), null };
        }
        if (!hasDisplay) return new[] { "", noDisplay };
        if (key == "Visual Style") return new[] { barWord(view.DisplayStyle), null };
        if (key == "Sun Intensity") return new[] { view.SunlightIntensity.ToString(culture), null };
        if (key == "Shadow Intensity") return new[] { view.ShadowIntensity.ToString(culture), null };
        if (key == "Silhouettes") return new[] { view.GetViewDisplayModel().EnableSilhouettes ? "on" : "off", null };
        if (key == "Smooth Lines") return new[] { view.GetViewDisplayModel().SmoothEdges ? "on" : "off", null };
        if (key == "Sketchy Lines") return new[] { view.GetSketchyLines().EnableSketchyLines ? "on" : "off", null };
        if (key == "Depth Cueing")
        {
            if (!view.CanUseDepthCueing())
                return new[] { "", "'" + view.Name + "' cannot use depth cueing - Revit offers it in elevations and sections" };
            var cueing = view.GetDepthCueing();
            return new[] { cueing.EnableDepthCueing ? "start=" + cueing.StartPercentage + "; end=" + cueing.EndPercentage
                + "; fade=" + cueing.FadeTo : "off", null };
        }
        if (key == "Display Background")
        {
            if (!takesBackground)
                return new[] { "", "Revit sets a display background on 3D views, sections and elevations only" };
            var bg = view.GetBackground();
            if (bg.Type == ViewDisplayBackgroundType.SunAndClouds) return new[] { "sky", null };
            if (bg.Type == ViewDisplayBackgroundType.Gradient)
                return new[] { "gradient sky=" + showColour(bg.SkyColor) + "; horizon=" + showColour(bg.BackgroundColor)
                    + "; ground=" + showColour(bg.GroundColor), null };
            if (bg.Type == ViewDisplayBackgroundType.Image) return new[] { "image " + bg.ImagePath, null };
            return new[] { "none", null };
        }
        return new[] { "", "Heron reads no setting called " + key };
    }
    catch (Exception ex) { return new[] { "", "Revit would not read it: " + ex.Message }; }
};

// ---- what the preset wants on THIS view: { value, why it cannot be worked out } ----
DateTime? duskUtc = null;
Func<string, string, string[]> settingWant = (key, value) =>
{
    if (value == "by purpose")
        return new[] { key == "Render Quality" ? (forIssue ? "High" : "Draft") : (forIssue ? "300 DPI" : "Screen"), null };
    if (key != "Sun" || !value.StartsWith("still")) return new[] { value, null };
    if (askedTime != null) return new[] { "still " + DateTime.MinValue.Add(askedTime.Value).ToString("HH:mm", culture), null };
    if (!value.EndsWith("sunset-30")) return new[] { value, null };
    if (sun == null) return new[] { "", "'" + view.Name + "' has no sun settings" };
    try
    {
        var sunset = sun.GetSunset(toProject(sun.StartDateAndTime).Date);
        duskUtc = DateTime.SpecifyKind(sunset, DateTimeKind.Utc).AddMinutes(-30);
        return new[] { "still " + toProject(duskUtc.Value).ToString("HH:mm", culture), null };
    }
    catch (Exception ex) { return new[] { "", "Revit would not give the sunset for the site: " + ex.Message }; }
};

// ---- a preset against the view, one row per setting:
// { setting, now, preset's value, same | differs | skipped | by hand, why } ----
Func<string, List<string[]>> compare = name =>
{
    var rows = new List<string[]>();
    foreach (var setting in presetTable.First(p => p.Key == name).Value)
    {
        var key = setting[0];
        if (byHandKeys.Contains(key))
        {
            rows.Add(new[] { key, "", setting[1], "by hand", "Revit's API has no member for it on any release 2020 to 2027" });
            continue;
        }
        var now = settingNow(key);
        var want = settingWant(key, setting[1]);
        if (now[1] != null || want[1] != null) rows.Add(new[] { key, now[0], want[0], "skipped", now[1] ?? want[1] });
        else rows.Add(new[] { key, now[0], want[0], squash(now[0]) == squash(want[0]) ? "same" : "differs", "" });
    }
    return rows;
};
Func<string[], string> describe = row =>
    row[3] == "same" ? row[0] + " " + row[1] + " (as the preset)"
    : row[3] == "differs" ? row[0] + " " + row[1] + " -> " + row[2]
    : row[3] == "by hand" ? row[0] + " " + row[2] + " - by hand, " + row[4]
    : row[0] + " skipped - " + row[4];

// ---- the materials on what the view shows that have no render appearance ----
// Read as LIST_MATERIALS reads an appearance: a material whose appearance
// asset is missing draws flat grey in Realistic and in a render. Materials
// Revit names on each element; a face drawn By Category is not counted.
var materialsSeen = new Dictionary<string, Material>();
var noAppearance = new List<string>();
var materialsRead = false;
if (hasDisplay)
{
    try
    {
        foreach (var e in new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType())
        {
            if (e.Category == null || e.Category.CategoryType != CategoryType.Model) continue;
            ICollection<ElementId> ids;
            try { ids = e.GetMaterialIds(false); } catch (Exception) { continue; }
            foreach (var id in ids)
            {
                var m = doc.GetElement(id) as Material;
                if (m != null && !materialsSeen.ContainsKey(id.ToString())) materialsSeen[id.ToString()] = m;
            }
        }
        noAppearance = materialsSeen.Values.Where(m => m.AppearanceAssetId == ElementId.InvalidElementId
                || !(doc.GetElement(m.AppearanceAssetId) is AppearanceAssetElement))
            .Select(m => m.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        materialsRead = true;
    }
    catch (Exception ex) { findings.Add("The materials on the view could not be read: " + ex.Message); }
}
var materialsLine = !materialsRead ? "Materials not read"
    : "Materials " + materialsSeen.Count + " on what the view shows, " + noAppearance.Count + " with no render appearance"
        + (noAppearance.Count == 0 ? "" : " (" + string.Join(", ", noAppearance.Take(12))
            + (noAppearance.Count > 12 ? " and " + (noAppearance.Count - 12) + " more" : "")
            + ") - they draw flat grey; SET_MATERIAL_APPEARANCE gives them one");
// ==== SHARED BLOCK - end ====

// ---- the preset asked for ----
string presetName = null;
if (inputProblem != null) refused = inputProblem;
else if (squash(preset).Length == 0)
    refused = "No preset was named. The presets: " + presetList + ". NOTHING WAS CHANGED.";
else
{
    presetName = presetNamed(preset);
    if (presetName == null)
        refused = "No visual preset is called '" + preset + "'. The presets: " + presetList + ". NOTHING WAS CHANGED.";
}

List<string[]> before = null;
if (refused == null)
{
    before = compare(presetName);
    if (before.All(r => r[3] == "skipped" || r[3] == "by hand"))
        refused = "'" + view.Name + "' takes none of " + presetName + "'s settings - " + before.First(r => r[3] == "skipped")[4]
            + ". NOTHING WAS CHANGED.";
}

// ---- set what differs, each with the members its own tool uses ----
if (refused == null)
{
    var whatRevitSaid = new Dictionary<string, string>();
    Action<string, Action> attempt = (what, act) =>
    {
        try { act(); }
        catch (Exception ex) { whatRevitSaid[what] = ex.Message; }
    };
    foreach (var row in before.Where(r => r[3] == "differs"))
    {
        var key = row[0];
        var want = row[2];
        if (key == "Visual Style")
            attempt(key, () =>
            {
                var pair = barWords.First(p => squash(p[0]) == squash(want));
                if (!revitStyles.ContainsKey(pair[1])) throw new InvalidOperationException("this Revit has no " + want + " style");
                view.DisplayStyle = revitStyles[pair[1]];
            });
        else if (key == "Sun Intensity") attempt(key, () => view.SunlightIntensity = int.Parse(want, culture));
        else if (key == "Shadow Intensity") attempt(key, () => view.ShadowIntensity = int.Parse(want, culture));
        else if (key == "Silhouettes" || key == "Smooth Lines")
            attempt(key, () =>
            {
                var model = view.GetViewDisplayModel();
                if (key == "Silhouettes") model.EnableSilhouettes = want == "on";
                else model.SmoothEdges = want == "on";
                view.SetViewDisplayModel(model);
            });
        else if (key == "Sketchy Lines")
            attempt(key, () =>
            {
                var sketchy = view.GetSketchyLines();
                sketchy.EnableSketchyLines = want == "on";
                view.SetSketchyLines(sketchy);
            });
        else if (key == "Depth Cueing")
            attempt(key, () =>
            {
                var cueing = view.GetDepthCueing();
                var values = pairs(want);
                cueing.EnableDepthCueing = want != "off";
                if (values.Count > 0)
                {
                    cueing.SetStartEndPercentages(int.Parse(values["start"], culture), int.Parse(values["end"], culture));
                    cueing.FadeTo = int.Parse(values["fade"], culture);
                }
                view.SetDepthCueing(cueing);
            });
        else if (key == "Display Background")
            attempt(key, () =>
            {
                if (want == "sky") view.SetBackground(ViewDisplayBackground.CreateSky());
                else
                {
                    var colours = pairs(want.Substring("gradient".Length));
                    view.SetBackground(ViewDisplayBackground.CreateGradient(colour(colours["sky"]), colour(colours["horizon"]),
                        colour(colours["ground"])));
                }
            });
        else if (key == "Sun")
            attempt(key, () =>
            {
                if (want.StartsWith("lighting"))
                {
                    var angles = want.Substring("lighting".Length).Trim().Split(' ')[0].Split('/');
                    sun.SunAndShadowType = SunAndShadowType.Lighting;
                    sun.Azimuth = double.Parse(angles[0], culture) * Math.PI / 180.0;
                    sun.Altitude = double.Parse(angles[1], culture) * Math.PI / 180.0;
                    sun.RelativeToView = want.EndsWith("relative to view");
                }
                else
                {
                    // The view keeps its own date; only the time is the preset's.
                    var day = toProject(sun.StartDateAndTime).Date;
                    var at = askedTime == null && duskUtc != null ? duskUtc.Value
                        : toUtc(day + TimeSpan.ParseExact(want.Substring("still".Length).Trim(), "hh\\:mm", culture));
                    sun.SunAndShadowType = SunAndShadowType.StillImage;
                    sun.StartDateAndTime = at;
                }
                doc.Regenerate();
            });
        else if (key.StartsWith("Render") || key == "Exposure")
            attempt(key, () =>
            {
                var settings = view3D.GetRenderingSettings();
                if (key == "Render Quality")
                {
                    var q = settings.GetRenderingQualitySettings();
                    q.RenderingQuality = want == "High" ? RenderingQuality.High : RenderingQuality.Draft;
                    settings.SetRenderingQualitySettings(q);
                }
                else if (key == "Render Output")
                {
                    if (want == "Screen") settings.ResolutionTarget = ResolutionTarget.Screen;
                    else
                    {
                        // The DPI is FOUND, as SET_RENDER_SETTINGS finds it: the
                        // printer level whose ResolutionValue reads it.
                        var dpi = int.Parse(new string(want.Where(char.IsDigit).ToArray()), culture);
                        var levelBefore = settings.PrinterResolution;
                        var offered = new List<string>();
                        var found = false;
                        settings.ResolutionTarget = ResolutionTarget.Printer;
                        foreach (PrinterResolution level in Enum.GetValues(typeof(PrinterResolution)))
                        {
                            settings.PrinterResolution = level;
                            offered.Add(settings.ResolutionValue + " DPI");
                            if (settings.ResolutionValue == dpi) { found = true; break; }
                        }
                        if (!found)
                        {
                            settings.PrinterResolution = levelBefore;
                            throw new InvalidOperationException("its printer output offers " + string.Join(", ", offered)
                                + ", not " + dpi + " DPI");
                        }
                    }
                }
                else if (key == "Render Lighting")
                {
                    var source = Enum.GetValues(typeof(LightingSource)).Cast<LightingSource>()
                        .First(l => squash(lightingWord(l)) == squash(want));
                    settings.LightingSource = source;
                }
                else if (key == "Render Background")
                {
                    if (want.StartsWith("colour"))
                    {
                        settings.BackgroundStyle = BackgroundStyle.Color;
                        var bg = (ColorBackgroundSettings)settings.GetBackgroundSettings();
                        bg.Color = colour(want.Substring("colour".Length));
                        settings.SetBackgroundSettings(bg);
                    }
                    else settings.BackgroundStyle = Enum.GetValues(typeof(BackgroundStyle)).Cast<BackgroundStyle>()
                        .First(b => squash(styleWord(b)) == squash(want));
                }
                else
                {
                    var values = pairs(want);
                    var exposure = settings.GetRenderingImageExposureSettings();
                    exposure.ExposureValue = double.Parse(values["value"], culture);
                    exposure.WhitePoint = double.Parse(values["whitepoint"], culture);
                    settings.SetRenderingImageExposureSettings(exposure);
                }
                view3D.SetRenderingSettings(settings);
            });
    }

    // ---- read back ----
    var after = compare(presetName);
    var notTaken = new List<string>();
    for (var i = 0; i < before.Count; i++)
    {
        if (before[i][3] == "same") alreadySo++;
        else if (before[i][3] == "skipped") skipped++;
        else if (before[i][3] == "differs")
        {
            if (after[i][3] == "same") changed++;
            else notTaken.Add(before[i][0] + " (reads " + after[i][1] + ")"
                + (whatRevitSaid.ContainsKey(before[i][0]) ? " - Revit: " + whatRevitSaid[before[i][0]] : ""));
        }
    }
    readBack = "'" + view.Name + "' (" + view.ViewType + ") against " + presetName + ":  ||  "
        + string.Join("  ||  ", after.Select(describe)) + "  ||  " + materialsLine;

    var differed = before.Count(r => r[3] == "differs");
    findings.Add(changed + " of " + differed + " setting(s) changed and read back as " + presetName + " says on '" + view.Name
        + "'; " + alreadySo + " already so, " + skipped + " skipped"
        + (notTaken.Count == 0 ? "" : " - not taken: " + string.Join("; ", notTaken)));
    var byHand = before.Where(r => r[3] == "by hand").Select(r => r[0]).ToList();
    if (byHand.Count > 0)
        findings.Add(presetName + " also wants " + string.Join(" and ", byHand) + " on. Revit's API has no member for "
            + (byHand.Count == 1 ? "it" : "them") + " on any release 2020 to 2027, so Heron did not set "
            + (byHand.Count == 1 ? "it" : "them") + ": tick Shadows on the View Control Bar, and Show Ambient Shadows in "
            + "Graphic Display Options");
    foreach (var row in before.Where(r => r[3] == "skipped")) findings.Add(row[0] + " skipped - " + row[4]);
    if (notTaken.Count > 0 && view.ViewTemplateId != ElementId.InvalidElementId)
        findings.Add("'" + view.Name + "' has a view template; a setting the template controls is refused - change it "
            + "in the template, or take the template off the view");
    if (!madeFor[presetName].Split(' ').Contains(viewKind))
        findings.Add(presetName + " is made for " + (madeFor[presetName] == "3d" ? "a 3D view"
            : madeFor[presetName] == "elevation section" ? "an elevation or a section" : "a 3D view, an elevation or a section")
            + "; '" + view.Name + "' is a " + view.ViewType + " view");
    if (view3D != null && !view3D.IsPerspective && before.Any(r => r[0] == "Render Lighting"))
        findings.Add("'" + view.Name + "' is an isometric 3D view - a render looks real only in perspective; "
            + "FRAME_ELEMENTS_IN_CAMERA makes a perspective camera of the selection");
    if (askedTime != null && !before.Any(r => r[0] == "Sun" && r[2].StartsWith("still")))
        findings.Add("sunTime " + sunTime + " was not used - " + presetName + " lights by "
            + (before.Any(r => r[0] == "Sun") ? "a lighting study relative to the view" : "artificial light alone"));
    if (materialsRead && noAppearance.Count > 0 && before.Any(r => r[0] == "Visual Style" && squash(r[2]) == "realistic"))
        findings.Add(materialsLine);
}
if (refused != null) findings.Add(refused);
