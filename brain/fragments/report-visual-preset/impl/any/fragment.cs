// NOT STANDALONE. Assumes `doc`, `view`, `preset`, `renderFor` and `sunTime`
// are in scope; leaves `wouldChange`, `materialsWithoutAppearance`,
// `presetsCompared`, `comparison`, `refused` and `findings` behind.
//
// NO TRANSACTION. It reads one view and changes nothing.
//
// HOW A VIEW'S LOOK STANDS AGAINST A VISUAL PRESET, BEFORE ANYTHING IS SET.
// With a preset named, every setting of it - the view's value now beside the
// preset's, and whether it is already so, would differ, cannot be taken by
// this view, or is the modeller's to tick by hand. With none named ("all"),
// one line per preset: how many of its settings this view would take
// differently - so the closest look can be chosen. Either way, the materials
// on what the view shows that have no render appearance, which draw flat
// grey whatever the preset.
//
// THE PRESETS, AND HOW EACH SETTING IS READ, ARE THE SHARED BLOCK BELOW - the
// same text as APPLY_VISUAL_PRESET's, so what this says a preset would do is
// what that one does.
//
// ONE STRING, LINES JOINED BY "  ||  ", because a list reaches the chat cut to
// three entries (READ_VIEW_SCALE's reason).

var wouldChange = 0;
var materialsWithoutAppearance = 0;
var presetsCompared = 0;
var comparison = "";
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
// A STILL SUN'S TIME is the dialog's clock, which Revit holds turned to UTC by
// the PC's OWN time zone (row 5b-1306, version 3): the site's zone, which
// version 2 used, wrote "still 10:00" as 18:00 on a laptop at UTC+3 with the
// site in Boston - the sun below the horizon - and read 10:00 back as 02:00.
Func<DateTime, DateTime> toProject = utc =>
    DateTime.SpecifyKind(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime(), DateTimeKind.Unspecified);
Func<DateTime, DateTime> toUtc = project =>
    DateTime.SpecifyKind(DateTime.SpecifyKind(project, DateTimeKind.Local).ToUniversalTime(), DateTimeKind.Utc);
// WHY A VIEW TAKES NO DEPTH CUEING (version 2, row 5b-1283). Measured
// 2026-10-20 on Revit 2020: every elevation of the metric Systems template -
// View Discipline Mechanical or Electrical - answered CanUseDepthCueing
// false, whatever its Far Clipping or visual style, and true once its View
// Discipline was Architectural. The reason names the discipline the view has;
// the discipline is the modeller's to change, never this tool's.
Func<View, string> noCueingWhy = cueView =>
{
    var discipline = cueView.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE);
    var disciplineWord = discipline == null ? null : discipline.AsValueString();
    if ((cueView.ViewType == ViewType.Elevation || cueView.ViewType == ViewType.Section) && !string.IsNullOrEmpty(disciplineWord))
        return "'" + cueView.Name + "' cannot use depth cueing - its View Discipline is " + disciplineWord
            + ", and Revit 2020 offered depth cueing in an elevation only once that was Architectural (Properties, View Discipline)";
    return "'" + cueView.Name + "' cannot use depth cueing - Revit offers it in elevations and sections only";
};
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
                return new[] { "", noCueingWhy(view) };
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
string sunsetSaid = null;
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
        // THE DATE IS HANDED OVER AS UTC (version 2, row 5b-1282). Revit
        // refused the project-time date, whose Kind is Unspecified: "Revit
        // does not accept input DateTime objects if they are of kind
        // DateTypeKind.Unspecified" (Revit 2020, Group IC's IC6). Only the
        // date part is used, so the day stays the view's own.
        var sunset = sun.GetSunset(DateTime.SpecifyKind(toProject(sun.StartDateAndTime).Date, DateTimeKind.Utc));
        // WHAT REVIT 2020 HANDS BACK IS THE SITE'S SUNSET IN PROJECT TIME, TURNED
        // TO UTC BY THIS PC'S OWN TIME ZONE (row 5b-1282). Measured 2026-10-20
        // on the owner's laptop (Arabian Standard Time, +3), site Boston, time
        // zone -5, 21 June: GetSunset gave 16:22 UTC and GetSunrise 01:10 UTC -
        // Boston's 19:22 and 04:10 standard time, each less the PC's 3 hours,
        // not the true 00:24 and 09:10 UTC. So the PC's own zone is taken off
        // again (ToLocalTime) to give the sunset in project time, and the
        // half hour is taken from that.
        var sunsetProject = DateTime.SpecifyKind(DateTime.SpecifyKind(sunset, DateTimeKind.Utc).ToLocalTime(), DateTimeKind.Unspecified);
        var duskProject = sunsetProject.AddMinutes(-30);
        duskUtc = toUtc(duskProject);
        sunsetSaid = "Dusk is half an hour before Revit's sunset for the site on " + sunsetProject.ToString("d MMMM yyyy", culture)
            + ": " + sunsetProject.ToString("HH:mm", culture) + " project time";
        return new[] { "still " + duskProject.ToString("HH:mm", culture), null };
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

// ---- which presets ----
var asked = new List<string>();
if (inputProblem != null) refused = inputProblem.Replace("NOTHING WAS CHANGED.", "Nothing was read.");
else if (squash(preset).Length == 0 || squash(preset) == "all") asked.AddRange(presetTable.Select(p => p.Key));
else
{
    var named = presetNamed(preset);
    if (named == null) refused = "No visual preset is called '" + preset + "'. The presets: " + presetList + ". Nothing was read.";
    else asked.Add(named);
}

if (refused == null)
{
    var lines = new List<string>();
    if (asked.Count == 1)
    {
        var rows = compare(asked[0]);
        wouldChange = rows.Count(r => r[3] == "differs");
        lines.Add(asked[0] + " on '" + view.Name + "' (" + view.ViewType + "): " + wouldChange + " setting(s) would differ, "
            + rows.Count(r => r[3] == "same") + " already so, " + rows.Count(r => r[3] == "skipped") + " this view cannot take, "
            + rows.Count(r => r[3] == "by hand") + " by hand");
        lines.AddRange(rows.Select(describe));
    }
    else
    {
        // The closest preset first: the fewest settings that would differ.
        var counted = asked.Select(name =>
        {
            var rows = compare(name);
            return new
            {
                Name = name,
                Differ = rows.Where(r => r[3] == "differs").Select(r => r[0]).ToList(),
                Same = rows.Count(r => r[3] == "same"),
                Skipped = rows.Count(r => r[3] == "skipped"),
            };
        }).OrderBy(c => c.Differ.Count).ThenBy(c => asked.IndexOf(c.Name)).ToList();
        wouldChange = counted[0].Differ.Count;
        lines.Add("'" + view.Name + "' (" + view.ViewType + ") against every preset, closest first");
        foreach (var c in counted)
            lines.Add(c.Name + ": " + c.Differ.Count + " would differ" + (c.Differ.Count == 0 ? "" : " (" + string.Join(", ", c.Differ) + ")")
                + ", " + c.Same + " already so" + (c.Skipped == 0 ? "" : ", " + c.Skipped + " this view cannot take"));
    }
    presetsCompared = asked.Count;
    materialsWithoutAppearance = noAppearance.Count;
    lines.Add(materialsLine);
    comparison = string.Join("  ||  ", lines);
    findings.Add(presetsCompared + " preset(s) compared on '" + view.Name + "'; " + wouldChange + " setting(s) would differ"
        + (asked.Count == 1 ? "" : " for the closest"));
    if (view.ViewTemplateId != ElementId.InvalidElementId)
        findings.Add("'" + view.Name + "' has a view template - a setting the template controls cannot be given to the "
            + "view itself");
}
if (refused == null && sunsetSaid != null) findings.Add(sunsetSaid);
if (refused != null) findings.Add(refused);
