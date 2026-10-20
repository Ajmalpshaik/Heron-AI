// NOT STANDALONE. Assumes `doc`, `view`, `quality`, `customQuality`,
// `lightingAccuracy`, `output`, `lighting`, `lightGroups`, `background`,
// `backgroundImageFit`, `backgroundImageOffsetMm`, `exposure` and
// `regionRendering` are in scope; leaves `changed`, `readBack`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A 3D VIEW'S RENDERING SETTINGS - what Revit's Rendering dialog holds for the
// view: quality, output, lighting scheme, the artificial light groups, the
// background and the exposure. Every input is a word or "keep"; anything left
// out is not touched.
//
// EVERY WORD IS READ BEFORE ANYTHING IS SET. One word this cannot read refuses
// the whole call, so a dialog half-filled from a typo never reaches the view.
//
// THE PRINTER DPI IS FOUND, NOT ASSUMED. Revit names its printer levels Low to
// VeryHigh and gives the dots per inch of each in ResolutionValue; the level
// whose ResolutionValue is the DPI asked for is the one set.
//
// "BEST" IS REVIT'S VeryHigh. The API's quality levels are Draft, Medium, High,
// VeryHigh and Custom on every release 2020 to 2027.
//
// THE LIGHT GROUPS ARE REACHED BY NAME at run time, as SET_FAMILY_LIGHT_SOURCE
// reaches the lighting classes: they live in a namespace the fragment executor
// does not import. LightGroupManager reads the same 2020 through 2027.
//
// THIS DOES NOT RENDER. No Revit release lets an add-in run the render
// (OPEN_RENDER_DIALOG opens Revit's dialog for the modeller to press Render).
//
// READ BACK: every setting asked is read again off the view, and only one that
// reads as asked is counted in `changed`.

const double MmPerFoot = 304.8;
var changed = 0;
var findings = new List<string>();
string refused = null;
var readBack = "";
var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant()
        .Replace("colours", "colors").Replace("colour", "color");
Func<string, bool> isKeep = text =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    return t.Length == 0 || t == "keep";
};
Func<string, double?> number = text =>
{
    double value;
    return double.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, culture, out value)
        ? (double?)value : null;
};
Func<double, string> show = value => Math.Round(value, 3).ToString(culture);

// The lighting classes, by name (see above), and Revit's reason rather than
// reflection's wrapper when one throws.
var lightingNamespace = typeof(Document).Namespace + ".Lighting.";
Func<string, System.Type> lightingType = name => typeof(Document).Assembly.GetType(lightingNamespace + name);
Func<object, string, object[], object> call = (target, name, args) =>
{
    var kind = target as System.Type ?? target.GetType();
    var method = kind.GetMethods().FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
    if (method == null) throw new InvalidOperationException(kind.Name + "." + name + " is not in this release of Revit.");
    try { return method.Invoke(target is System.Type ? null : target, args); }
    catch (System.Reflection.TargetInvocationException ex)
    {
        throw new InvalidOperationException(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
    }
};
Func<object, string, object> property = (target, name) => target.GetType().GetProperty(name).GetValue(target);

// Revit's words for each setting, beside its API value.
var qualityWords = new Dictionary<string, RenderingQuality>
{
    { "draft", RenderingQuality.Draft }, { "medium", RenderingQuality.Medium },
    { "high", RenderingQuality.High }, { "best", RenderingQuality.VeryHigh },
    { "veryhigh", RenderingQuality.VeryHigh }, { "custom", RenderingQuality.Custom },
};
Func<RenderingQuality, string> qualityWord = q =>
    q == RenderingQuality.VeryHigh ? "Best" : q.ToString();
var lightingWords = new Dictionary<string, LightingSource>
{
    { "exteriorsunonly", LightingSource.ExteriorSun }, { "exteriorsun", LightingSource.ExteriorSun },
    { "exteriorsunandartificial", LightingSource.ExteriorSunAndArtificial },
    { "exteriorartificialonly", LightingSource.ExteriorArtificial },
    { "exteriorartificial", LightingSource.ExteriorArtificial },
    { "interiorsunonly", LightingSource.InteriorSun }, { "interiorsun", LightingSource.InteriorSun },
    { "interiorsunandartificial", LightingSource.InteriorSunAndArtificial },
    { "interiorartificialonly", LightingSource.InteriorArtificial },
    { "interiorartificial", LightingSource.InteriorArtificial },
};
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
var styleWords = new Dictionary<string, BackgroundStyle>
{
    { "skynoclouds", BackgroundStyle.SkyNoClouds }, { "skyveryfewclouds", BackgroundStyle.SkyVeryFewClouds },
    { "skyfewclouds", BackgroundStyle.SkyFewClouds }, { "skycloudy", BackgroundStyle.SkyCloudy },
    { "skyverycloudy", BackgroundStyle.SkyVeryCloudy }, { "transparent", BackgroundStyle.Transparent },
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
var fitWords = new Dictionary<string, BackgroundImageFit>
{
    { "originalsize", BackgroundImageFit.None }, { "none", BackgroundImageFit.None },
    { "stretch", BackgroundImageFit.Stretch }, { "fitwidth", BackgroundImageFit.Horizontal },
    { "horizontal", BackgroundImageFit.Horizontal }, { "fitheight", BackgroundImageFit.Vertical },
    { "vertical", BackgroundImageFit.Vertical },
};
Func<string, Color> colour = text =>
{
    var t = (text ?? "").Trim();
    if (t.StartsWith("#") && t.Length == 7)
    {
        try
        {
            return new Color(Convert.ToByte(t.Substring(1, 2), 16), Convert.ToByte(t.Substring(3, 2), 16),
                Convert.ToByte(t.Substring(5, 2), 16));
        }
        catch (FormatException) { return null; }
    }
    var parts = t.Split(',');
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
// What follows a leading word - "colour 255,0,0" gives "255,0,0".
Func<string, string, string> after = (text, word) =>
{
    var at = text.ToLowerInvariant().IndexOf(word);
    return at < 0 ? "" : text.Substring(at + word.Length).Trim().TrimStart(':').Trim().Trim('"');
};

var view3D = view as View3D;
if (view == null) refused = "No view was named, so NOTHING WAS CHANGED.";
else if (view.IsTemplate) refused = "'" + view.Name + "' is a view template; rendering settings are set on a 3D view. "
    + "NOTHING WAS CHANGED.";
else if (view3D == null) refused = "'" + view.Name + "' is not a 3D view, and only a 3D view has Rendering Settings. "
    + "NOTHING WAS CHANGED.";

// ---- read every word first ----
RenderingQuality? wantQuality = null;
RenderDuration? wantDuration = null;
var wantLevel = 0;
var wantTime = 0;
LightAndMaterialAccuracyMode? wantAccuracy = null;
ResolutionTarget? wantTarget = null;
var wantDpi = 0;
LightingSource? wantLighting = null;
BackgroundStyle? wantStyle = null;
Color wantColor = null;
string wantImage = null;
BackgroundImageFit? wantFit = null;
XYZ wantOffset = null;
var wantExposure = new Dictionary<string, double>();
bool? wantRegion = null;
var wantGroups = new List<KeyValuePair<object, bool>>();

if (refused == null && !isKeep(quality))
{
    RenderingQuality q;
    if (qualityWords.TryGetValue(squash(quality), out q)) wantQuality = q;
    else refused = "No render quality is called '" + quality + "'. Say Draft, Medium, High, Best or Custom - Revit's "
        + "API has these five on every release. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(customQuality))
{
    var c = squash(customQuality);
    var digits = new string(c.Where(char.IsDigit).ToArray());
    int n;
    if (c == "untilsatisfactory") wantDuration = RenderDuration.UntilSatisfactory;
    else if (c.StartsWith("level") && int.TryParse(digits, out n) && n >= 1 && n <= 40)
    { wantDuration = RenderDuration.ByLevel; wantLevel = n; }
    else if (c.StartsWith("time") && int.TryParse(digits, out n) && n >= 1 && n <= 32768)
    { wantDuration = RenderDuration.ByTime; wantTime = n; }
    else refused = "'" + customQuality + "' is not a custom quality. Say 'level N' (1 to 40), 'time N' (1 to 32768, "
        + "Revit's render time value) or 'until satisfactory'. NOTHING WAS CHANGED.";
    if (refused == null && wantQuality != null && wantQuality != RenderingQuality.Custom)
        refused = "A custom quality was given with quality '" + quality + "'. Revit takes a render level or time only "
            + "at Custom quality. NOTHING WAS CHANGED.";
    if (refused == null) wantQuality = RenderingQuality.Custom;
}
if (refused == null && !isKeep(lightingAccuracy))
{
    var a = squash(lightingAccuracy);
    if (a == "simplified") wantAccuracy = LightAndMaterialAccuracyMode.Simplified;
    else if (a == "advanced") wantAccuracy = LightAndMaterialAccuracyMode.Advanced;
    else refused = "'" + lightingAccuracy + "' is not a lighting and material accuracy. Say Simplified or Advanced. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(output))
{
    var o = squash(output);
    var digits = new string(o.Where(char.IsDigit).ToArray());
    int dpi;
    if (o == "screen") wantTarget = ResolutionTarget.Screen;
    else if (o == "printer") wantTarget = ResolutionTarget.Printer;
    else if (int.TryParse(digits, out dpi) && dpi > 0 && o.Replace("printer", "").Replace("dpi", "") == digits)
    { wantTarget = ResolutionTarget.Printer; wantDpi = dpi; }
    else refused = "'" + output + "' is not a render output. Say Screen, Printer, or a printer resolution such as "
        + "'300 DPI'. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(lighting))
{
    LightingSource l;
    if (lightingWords.TryGetValue(squash(lighting), out l)) wantLighting = l;
    else refused = "No lighting scheme is called '" + lighting + "'. Say Exterior or Interior, then Sun only, Sun and "
        + "Artificial, or Artificial only - for example 'Interior: Sun and Artificial'. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(background))
{
    var raw = background.Trim();
    var b = squash(raw);
    BackgroundStyle s;
    if (styleWords.TryGetValue(b, out s)) wantStyle = s;
    else if (b.StartsWith("color"))
    {
        wantColor = colour(after(raw, raw.ToLowerInvariant().Contains("colour") ? "colour" : "color"));
        if (wantColor == null) refused = "'" + background + "' names no colour Heron can read. Say 'colour R,G,B' "
            + "(0 to 255 each) or 'colour #RRGGBB'. NOTHING WAS CHANGED.";
        else wantStyle = BackgroundStyle.Color;
    }
    else if (b.StartsWith("image"))
    {
        wantImage = after(raw, "image");
        if (wantImage.Length == 0 || !System.IO.File.Exists(wantImage))
            refused = "The background image '" + wantImage + "' is not a file on this PC. Revit takes a bmp, jpg, jpeg, "
                + "png or tif that exists. NOTHING WAS CHANGED.";
        else wantStyle = BackgroundStyle.Image;
    }
    else refused = "No render background is called '" + background + "'. Say Sky: No Clouds, Sky: Very Few Clouds, "
        + "Sky: Few Clouds, Sky: Cloudy, Sky: Very Cloudy, 'colour R,G,B', 'image <file>' or Transparent. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(backgroundImageFit))
{
    BackgroundImageFit f;
    if (fitWords.TryGetValue(squash(backgroundImageFit), out f)) wantFit = f;
    else refused = "'" + backgroundImageFit + "' is not an image fit. Say Original Size, Stretch, Fit Width or Fit "
        + "Height. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(backgroundImageOffsetMm))
{
    var parts = backgroundImageOffsetMm.Split(',');
    var x = parts.Length == 2 ? number(parts[0]) : null;
    var y = parts.Length == 2 ? number(parts[1]) : null;
    if (x == null || y == null) refused = "'" + backgroundImageOffsetMm + "' is not an offset - two numbers in "
        + "millimetres, across then up, such as '0,500'. NOTHING WAS CHANGED.";
    else wantOffset = new XYZ(x.Value / MmPerFoot, y.Value / MmPerFoot, 0);
}
if (refused == null && !isKeep(exposure))
{
    // Revit's own ranges, from the API reference of every release 2020-2027.
    var ranges = new Dictionary<string, double[]>
    {
        { "value", new[] { 0.0, 21.0 } }, { "highlights", new[] { 0.0, 1.0 } }, { "shadows", new[] { 0.1, 4.0 } },
        { "saturation", new[] { 0.0, 5.0 } }, { "whitepoint", new[] { 1800.0, 15000.0 } },
    };
    foreach (var pair in exposure.Split(';'))
    {
        if (pair.Trim().Length == 0) continue;
        var bits = pair.Split('=');
        var key = bits.Length == 2 ? squash(bits[0]) : "";
        if (key == "ev" || key == "exposure" || key == "exposurevalue") key = "value";
        var value = bits.Length == 2 ? number(bits[1]) : null;
        if (!ranges.ContainsKey(key) || value == null)
        {
            refused = "'" + pair.Trim() + "' is not an exposure setting. Say value=, highlights=, shadows=, "
                + "saturation= or white point=, separated by ';'. NOTHING WAS CHANGED.";
            break;
        }
        if (value < ranges[key][0] || value > ranges[key][1])
        {
            refused = "Exposure " + key + " " + show(value.Value) + " is outside Revit's range, " + show(ranges[key][0])
                + " to " + show(ranges[key][1]) + ". NOTHING WAS CHANGED.";
            break;
        }
        wantExposure[key] = value.Value;
    }
}
if (refused == null && !isKeep(regionRendering))
{
    var r = squash(regionRendering);
    if (r == "yes" || r == "on" || r == "true") wantRegion = true;
    else if (r == "no" || r == "off" || r == "false") wantRegion = false;
    else refused = "'" + regionRendering + "' is not yes or no for region rendering. NOTHING WAS CHANGED.";
}

object manager = null;
var groups = new List<object>();
Func<object, string> groupName = g => (string)property(g, "Name");
if (refused == null && !isKeep(lightGroups))
{
    var managerType = lightingType("LightGroupManager");
    if (managerType == null)
        refused = "This Revit has no light group manager, so the light groups cannot be set. NOTHING WAS CHANGED.";
    else
    {
        try
        {
            manager = call(managerType, "GetLightGroupManager", new object[] { doc });
            foreach (var g in (System.Collections.IEnumerable)call(manager, "GetGroups", new object[0])) groups.Add(g);
        }
        catch (Exception ex) { refused = "Revit would not list the light groups: " + ex.Message + ". NOTHING WAS CHANGED."; }
    }
    foreach (var pair in refused != null ? new string[0] : lightGroups.Split(';'))
    {
        if (pair.Trim().Length == 0) continue;
        var bits = pair.Split('=');
        var state = bits.Length == 2 ? squash(bits[1]) : "";
        bool on;
        if (state == "on" || state == "yes") on = true;
        else if (state == "off" || state == "no") on = false;
        else
        {
            refused = "'" + pair.Trim() + "' is not 'group name=on' or 'group name=off'. NOTHING WAS CHANGED.";
            break;
        }
        var name = bits[0].Trim();
        if (string.Equals(name, "all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var g in groups) wantGroups.Add(new KeyValuePair<object, bool>(g, on));
            continue;
        }
        var match = groups.FirstOrDefault(g => string.Equals(groupName(g), name, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            refused = "No light group is called '" + name + "'. "
                + (groups.Count == 0 ? "This project has no light groups. "
                    : "The project's light groups: " + string.Join(", ", groups.Select(groupName)) + ". ")
                + "NOTHING WAS CHANGED.";
            break;
        }
        wantGroups.Add(new KeyValuePair<object, bool>(match, on));
    }
}
Func<object, bool> groupOn = g => (bool)call(manager, "IsLightGroupOn", new object[] { view.Id, property(g, "Id") });

var asked = (wantQuality != null ? 1 : 0) + (wantDuration != null ? 1 : 0) + (wantAccuracy != null ? 1 : 0)
    + (wantTarget != null ? 1 : 0) + (wantLighting != null ? 1 : 0) + (wantStyle != null ? 1 : 0)
    + (wantFit != null ? 1 : 0) + (wantOffset != null ? 1 : 0) + (wantExposure.Count > 0 ? 1 : 0)
    + (wantRegion != null ? 1 : 0) + wantGroups.Count;
if (refused == null && asked == 0)
    refused = "Nothing was asked for - give a quality, output, lighting, light groups, background, exposure or "
        + "region. Anything left out is deliberately not touched. NOTHING WAS CHANGED.";

// ---- set ----
if (refused == null)
{
    var settings = view3D.GetRenderingSettings();

    if (wantQuality != null || wantDuration != null || wantAccuracy != null)
    {
        try
        {
            var q = settings.GetRenderingQualitySettings();
            if (wantQuality != null) q.RenderingQuality = wantQuality.Value;
            if (wantDuration != null)
            {
                q.RenderDuration = wantDuration.Value;
                if (wantDuration == RenderDuration.ByLevel) q.RenderLevel = wantLevel;
                if (wantDuration == RenderDuration.ByTime) q.RenderTime = wantTime;
            }
            if (wantAccuracy != null) q.LightAndMaterialAccuracyMode = wantAccuracy.Value;
            settings.SetRenderingQualitySettings(q);
        }
        catch (Exception ex) { findings.Add("Revit refused the quality: " + ex.Message); }
    }

    if (wantTarget != null)
    {
        var targetBefore = settings.ResolutionTarget;
        var levelBefore = settings.PrinterResolution;
        try
        {
            settings.ResolutionTarget = wantTarget.Value;
            if (wantDpi > 0)
            {
                var offered = new List<string>();
                var found = false;
                foreach (PrinterResolution level in Enum.GetValues(typeof(PrinterResolution)))
                {
                    settings.PrinterResolution = level;
                    offered.Add(settings.ResolutionValue + " DPI");
                    if (settings.ResolutionValue == wantDpi) { found = true; break; }
                }
                if (!found)
                {
                    // Trying each level is not a change: the output goes back as it was.
                    settings.PrinterResolution = levelBefore;
                    settings.ResolutionTarget = targetBefore;
                    findings.Add("Revit's printer output offers " + string.Join(", ", offered) + " - not "
                        + wantDpi + " DPI, so the output was left as it was");
                }
            }
        }
        catch (Exception ex) { findings.Add("Revit refused the output: " + ex.Message); }
    }

    if (wantLighting != null)
    {
        try { settings.LightingSource = wantLighting.Value; }
        catch (Exception ex) { findings.Add("Revit refused the lighting scheme: " + ex.Message); }
    }

    if (wantStyle != null || wantFit != null || wantOffset != null)
    {
        try
        {
            if (wantStyle != null) settings.BackgroundStyle = wantStyle.Value;
            var bg = settings.GetBackgroundSettings();
            var colourBg = bg as ColorBackgroundSettings;
            var imageBg = bg as ImageBackgroundSettings;
            if (wantColor != null && colourBg != null) colourBg.Color = wantColor;
            if (wantImage != null && imageBg != null) imageBg.FilePath = wantImage;
            if ((wantFit != null || wantOffset != null) && imageBg == null)
                findings.Add("An image fit or offset was given and the background is not an image, so neither was set");
            if (wantFit != null && imageBg != null) imageBg.BackgroundImageFit = wantFit.Value;
            if (wantOffset != null && imageBg != null)
            {
                imageBg.OffsetWidth = wantOffset.X;
                imageBg.OffsetHeight = wantOffset.Y;
            }
            settings.SetBackgroundSettings(bg);
        }
        catch (Exception ex) { findings.Add("Revit refused the background: " + ex.Message); }
    }

    if (wantExposure.Count > 0)
    {
        try
        {
            var e = settings.GetRenderingImageExposureSettings();
            if (wantExposure.ContainsKey("value")) e.ExposureValue = wantExposure["value"];
            if (wantExposure.ContainsKey("highlights")) e.Highlights = wantExposure["highlights"];
            if (wantExposure.ContainsKey("shadows")) e.Shadows = wantExposure["shadows"];
            if (wantExposure.ContainsKey("saturation")) e.Saturation = wantExposure["saturation"];
            if (wantExposure.ContainsKey("whitepoint")) e.WhitePoint = wantExposure["whitepoint"];
            settings.SetRenderingImageExposureSettings(e);
        }
        catch (Exception ex) { findings.Add("Revit refused the exposure: " + ex.Message); }
    }

    if (wantRegion != null)
    {
        try { settings.UsesRegionRendering = wantRegion.Value; }
        catch (Exception ex) { findings.Add("Revit refused region rendering: " + ex.Message); }
    }

    try { view3D.SetRenderingSettings(settings); }
    catch (Exception ex)
    {
        refused = "Revit refused the rendering settings on '" + view.Name + "': " + ex.Message + " A view template that "
            + "controls Rendering Settings is the commonest reason. NOTHING WAS CHANGED.";
    }

    if (refused == null)
    {
        foreach (var want in wantGroups)
        {
            try { call(manager, "SetLightGroupOn", new object[] { view.Id, property(want.Key, "Id"), want.Value }); }
            catch (Exception ex) { findings.Add("Revit refused light group '" + groupName(want.Key) + "': " + ex.Message); }
        }
    }
}

// ---- read back ----
if (refused == null)
{
    var s = view3D.GetRenderingSettings();
    var q = s.GetRenderingQualitySettings();
    var e = s.GetRenderingImageExposureSettings();
    var bg = s.GetBackgroundSettings();
    var colourNow = bg as ColorBackgroundSettings;
    var image = bg as ImageBackgroundSettings;
    var notTaken = new List<string>();
    Action<bool, string> count = (ok, what) => { if (ok) changed++; else notTaken.Add(what); };

    if (wantQuality != null) count(q.RenderingQuality == wantQuality.Value, "quality " + qualityWord(wantQuality.Value));
    if (wantDuration != null)
        count(q.RenderDuration == wantDuration.Value
              && (wantDuration != RenderDuration.ByLevel || q.RenderLevel == wantLevel)
              && (wantDuration != RenderDuration.ByTime || q.RenderTime == wantTime), "custom quality " + customQuality);
    if (wantAccuracy != null) count(q.LightAndMaterialAccuracyMode == wantAccuracy.Value, "accuracy " + lightingAccuracy);
    if (wantTarget != null)
        count(s.ResolutionTarget == wantTarget.Value && (wantDpi == 0 || s.ResolutionValue == wantDpi), "output " + output);
    if (wantLighting != null) count(s.LightingSource == wantLighting.Value, "lighting " + lightingWord(wantLighting.Value));
    if (wantStyle != null)
    {
        var ok = s.BackgroundStyle == wantStyle.Value;
        if (ok && wantColor != null) ok = colourNow != null && colourNow.Color.Red == wantColor.Red
                                          && colourNow.Color.Green == wantColor.Green && colourNow.Color.Blue == wantColor.Blue;
        if (ok && wantImage != null) ok = image != null && string.Equals(image.FilePath, wantImage, StringComparison.OrdinalIgnoreCase);
        count(ok, "background " + background);
    }
    if (wantFit != null) count(image != null && image.BackgroundImageFit == wantFit.Value, "image fit " + backgroundImageFit);
    if (wantOffset != null)
        count(image != null && Math.Abs(image.OffsetWidth - wantOffset.X) < 1e-6 && Math.Abs(image.OffsetHeight - wantOffset.Y) < 1e-6,
            "image offset " + backgroundImageOffsetMm);
    if (wantExposure.Count > 0)
    {
        var ok = true;
        foreach (var pair in wantExposure)
        {
            var now = pair.Key == "value" ? e.ExposureValue : pair.Key == "highlights" ? e.Highlights
                : pair.Key == "shadows" ? e.Shadows : pair.Key == "saturation" ? e.Saturation : e.WhitePoint;
            if (Math.Abs(now - pair.Value) > 1e-6) ok = false;
        }
        count(ok, "exposure " + exposure);
    }
    if (wantRegion != null) count(s.UsesRegionRendering == wantRegion.Value, "region " + regionRendering);
    foreach (var want in wantGroups)
        count(groupOn(want.Key) == want.Value, "light group " + groupName(want.Key) + (want.Value ? " on" : " off"));

    var lines = new List<string>
    {
        "quality " + qualityWord(q.RenderingQuality)
            + (q.RenderingQuality == RenderingQuality.Custom
                ? " (" + (q.RenderDuration == RenderDuration.ByLevel ? "level " + q.RenderLevel
                    : q.RenderDuration == RenderDuration.ByTime ? "time " + q.RenderTime : "until satisfactory")
                    + ", " + q.LightAndMaterialAccuracyMode + ")" : ""),
        "output " + (s.ResolutionTarget == ResolutionTarget.Screen ? "Screen" : "Printer " + s.ResolutionValue + " DPI"),
        "lighting " + lightingWord(s.LightingSource),
        "background " + styleWord(s.BackgroundStyle)
            + (colourNow != null ? " " + colourNow.Color.Red + "," + colourNow.Color.Green + "," + colourNow.Color.Blue : "")
            + (image != null ? " '" + image.FilePath + "', fit " + image.BackgroundImageFit + ", offset "
                + Math.Round(image.OffsetWidth * MmPerFoot) + "," + Math.Round(image.OffsetHeight * MmPerFoot) + " mm" : ""),
        "exposure value " + show(e.ExposureValue) + ", highlights " + show(e.Highlights) + ", shadows "
            + show(e.Shadows) + ", saturation " + show(e.Saturation) + ", white point " + show(e.WhitePoint),
        "region " + (s.UsesRegionRendering ? "on" : "off"),
    };
    if (manager != null)
        lines.Add("light groups " + (groups.Count == 0 ? "none in the project"
            : string.Join(", ", groups.Select(g => groupName(g) + (groupOn(g) ? " on" : " off")))));
    readBack = "'" + view.Name + "': " + string.Join("; ", lines) + ".";

    findings.Add(changed + " of " + asked + " setting(s) read back as asked on '" + view.Name + "'"
        + (notTaken.Count == 0 ? "" : " - not taken: " + string.Join("; ", notTaken)));
}
if (refused != null) findings.Add(refused);
