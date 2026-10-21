// NOT STANDALONE. Assumes `doc` and `view` are in scope; leaves `setup`,
// `sectionsRead` and `findings` behind.
//
// NO TRANSACTION. It reads one view and changes nothing.
//
// EVERYTHING A RENDER OF THIS VIEW DEPENDS ON, IN ONE ANSWER - the four dialogs
// a modeller would otherwise open one by one:
//   Rendering        (a 3D view only) quality, output and its DPI, lighting
//                    scheme, artificial light groups, background, exposure,
//                    region
//   Sun Settings     study, date and time on the site's clock, preset, ground
//                    plane, the site it uses
//   Graphic Display  visual style, sun and shadow intensity, silhouettes,
//                    smooth lines, transparency, hidden lines, sketchy lines,
//                    depth cueing, background
//   Camera           (a 3D view) eye, direction, perspective, eye and target
//                    elevation, far clip, crop box
// and the settings Revit's API cannot read (Cast Shadows, Ambient Shadows,
// Ambient Light, Photographic Exposure) are named as such, never guessed.
//
// THE SUN'S TIME IS THE ONE REVIT'S DIALOG SHOWS (version 2, row 5b-1305).
// Revit hands StartDateAndTime and EndDateAndTime back as the site's clock
// turned to UTC by THIS PC's own time zone - not the site's - so it is turned
// back with the PC's zone (ToLocalTime), the rule row 5b-1282 found for
// GetSunset. Measured 2026-10-21 on Revit 2020, owner's laptop UTC+3, site
// Boston -5: the dialog said 10:00 AM, the raw time read 07:00 UTC, and
// version 1's SiteLocation.ConvertToProjectTime printed 02:00.
//
// THE LIGHT GROUPS ARE REACHED BY NAME at run time, as SET_FAMILY_LIGHT_SOURCE
// reaches the lighting classes: the fragment executor does not import their
// namespace.
//
// ONE STRING, SECTIONS JOINED BY "  ||  ", because a list reaches the chat cut
// to three entries (READ_VIEW_SCALE's reason).

const double MmPerFoot = 304.8;
var findings = new List<string>();
var setup = "";
var sectionsRead = 0;
var sections = new List<string>();
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> show = v => Math.Round(v, 3).ToString(culture);
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(culture);
Func<double, string> degrees = r => Math.Round(r * 180.0 / Math.PI, 2).ToString(culture);
Func<Color, string> showColour = c => c == null || !c.IsValid ? "none" : c.Red + "," + c.Green + "," + c.Blue;

if (view == null) findings.Add("No view was named, so nothing was read");
else if (view.IsTemplate) findings.Add("'" + view.Name + "' is a view template; name a view");
else
{
    var view3D = view as View3D;

    // ---- Rendering ----
    if (view3D != null)
    {
        try
        {
            var s = view3D.GetRenderingSettings();
            var q = s.GetRenderingQualitySettings();
            var e = s.GetRenderingImageExposureSettings();
            var bg = s.GetBackgroundSettings();
            var colourBg = bg as ColorBackgroundSettings;
            var imageBg = bg as ImageBackgroundSettings;
            var source = s.LightingSource;
            var lighting = (source == LightingSource.ExteriorSun || source == LightingSource.ExteriorSunAndArtificial
                            || source == LightingSource.ExteriorArtificial ? "Exterior: " : "Interior: ")
                + (source == LightingSource.ExteriorSun || source == LightingSource.InteriorSun ? "Sun only"
                    : source == LightingSource.ExteriorSunAndArtificial || source == LightingSource.InteriorSunAndArtificial
                        ? "Sun and Artificial" : "Artificial only");
            var text = "Rendering - quality " + (q.RenderingQuality == RenderingQuality.VeryHigh ? "Best" : q.RenderingQuality.ToString())
                + (q.RenderingQuality == RenderingQuality.Custom
                    ? " (" + (q.RenderDuration == RenderDuration.ByLevel ? "level " + q.RenderLevel
                        : q.RenderDuration == RenderDuration.ByTime ? "time " + q.RenderTime : "until satisfactory")
                        + ", " + q.LightAndMaterialAccuracyMode + ")" : "")
                + "; output " + (s.ResolutionTarget == ResolutionTarget.Screen ? "Screen" : "Printer " + s.ResolutionValue + " DPI")
                + "; lighting " + lighting
                + "; background " + s.BackgroundStyle
                + (colourBg != null ? " " + showColour(colourBg.Color) : "")
                + (imageBg != null ? " '" + imageBg.FilePath + "', fit " + imageBg.BackgroundImageFit + ", offset "
                    + mm(imageBg.OffsetWidth) + "," + mm(imageBg.OffsetHeight) + " mm" : "")
                + "; exposure value " + show(e.ExposureValue) + ", highlights " + show(e.Highlights) + ", shadows "
                + show(e.Shadows) + ", saturation " + show(e.Saturation) + ", white point " + show(e.WhitePoint)
                + "; region " + (s.UsesRegionRendering ? "on" : "off");

            var lightingNamespace = typeof(Document).Namespace + ".Lighting.";
            var managerType = typeof(Document).Assembly.GetType(lightingNamespace + "LightGroupManager");
            if (managerType != null)
            {
                try
                {
                    var manager = managerType.GetMethod("GetLightGroupManager").Invoke(null, new object[] { doc });
                    var isOn = managerType.GetMethod("IsLightGroupOn");
                    var names = new List<string>();
                    foreach (var g in (System.Collections.IEnumerable)managerType.GetMethod("GetGroups").Invoke(manager, new object[0]))
                    {
                        var name = (string)g.GetType().GetProperty("Name").GetValue(g);
                        var id = g.GetType().GetProperty("Id").GetValue(g);
                        names.Add(name + ((bool)isOn.Invoke(manager, new object[] { view.Id, id }) ? " on" : " off"));
                    }
                    text += "; light groups " + (names.Count == 0 ? "none in the project" : string.Join(", ", names));
                }
                catch (Exception ex)
                {
                    findings.Add("The light groups could not be read: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                }
            }
            sections.Add(text);
            sectionsRead++;
        }
        catch (Exception ex) { findings.Add("The rendering settings could not be read: " + ex.Message); }
    }
    else sections.Add("Rendering - '" + view.Name + "' is not a 3D view, and only a 3D view has Rendering Settings");

    // ---- Sun Settings ----
    SunAndShadowSettings sun = null;
    try { sun = view.SunAndShadowSettings; } catch (Exception) { sun = null; }
    if (sun != null)
    {
        try
        {
            // The site's clock, as the dialog shows it: the PC's zone taken off
            // again (row 5b-1305, the header).
            Func<DateTime, string> local = raw => DateTime.SpecifyKind(raw, DateTimeKind.Utc).ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm", culture);
            string preset = null;
            try { preset = sun.GetMatchingPreset(); } catch (Exception) { preset = null; }
            var ground = sun.UsesGroundPlane ? doc.GetElement(sun.GroundPlaneLevelId) as Level : null;
            var study = sun.SunAndShadowType == SunAndShadowType.StillImage ? "Still"
                : sun.SunAndShadowType == SunAndShadowType.OneDayStudy ? "Single Day"
                : sun.SunAndShadowType == SunAndShadowType.MultiDayStudy ? "Multi-Day" : "Lighting";
            sections.Add("Sun Settings - " + study + " study"
                + (sun.SunAndShadowType == SunAndShadowType.Lighting
                    ? ", azimuth " + degrees(sun.Azimuth) + "°, altitude " + degrees(sun.Altitude) + "°, "
                        + (sun.RelativeToView ? "relative to the view" : "relative to the model")
                    : ", " + local(sun.StartDateAndTime) + (sun.SunAndShadowType == SunAndShadowType.StillImage ? ""
                        : " to " + local(sun.EndDateAndTime) + (sun.SunriseToSunset ? " (sunrise to sunset)" : "")
                            + ", every " + sun.TimeInterval))
                + "; preset " + (string.IsNullOrEmpty(preset) ? "none matches" : "'" + preset + "'")
                + "; ground plane " + (ground != null ? "at " + ground.Name : "off")
                + "; site '" + sun.ProjectLocationName + "', latitude " + degrees(sun.Latitude) + "°, longitude "
                + degrees(sun.Longitude) + "°, time zone " + sun.TimeZone.ToString(culture)
                + (sun.UsesDST ? ", daylight saving in use at the site" : ""));
            sectionsRead++;
        }
        catch (Exception ex) { findings.Add("The sun settings could not be read: " + ex.Message); }
    }
    else sections.Add("Sun Settings - '" + view.Name + "' has none");

    // ---- Graphic Display Options ----
    if (view.HasDisplayStyle())
    {
        try
        {
            var model = view.GetViewDisplayModel();
            var sketchy = view.GetSketchyLines();
            var bg = view.GetBackground();
            var silhouette = doc.GetElement(model.SilhouetteEdgesGStyleId) as GraphicsStyle;
            var text = "Graphic Display - visual style " + view.DisplayStyle
                + "; sun intensity " + view.SunlightIntensity + ", shadow intensity " + view.ShadowIntensity
                + "; silhouettes " + (model.EnableSilhouettes ? "on" + (silhouette != null ? " (" + silhouette.Name + ")" : "") : "off")
                + ", smooth lines " + (model.SmoothEdges ? "on" : "off") + ", transparency " + model.Transparency
                + ", hidden lines " + model.ShowHiddenLines
                + "; sketchy lines " + (sketchy.EnableSketchyLines ? "on, jitter " + sketchy.Jitter + ", extension "
                    + sketchy.Extension : "off");
            if (view.CanUseDepthCueing())
            {
                var cueing = view.GetDepthCueing();
                text += "; depth cueing " + (cueing.EnableDepthCueing ? "on, " + cueing.StartPercentage + "-"
                    + cueing.EndPercentage + "%, fade " + cueing.FadeTo : "off");
            }
            text += "; background " + bg.Type
                + (bg.Type == ViewDisplayBackgroundType.Gradient ? " " + showColour(bg.SkyColor) + " / "
                    + showColour(bg.BackgroundColor) + " / " + showColour(bg.GroundColor) : "")
                + (bg.Type == ViewDisplayBackgroundType.Image ? " '" + bg.ImagePath + "'" : "")
                + "; Cast Shadows, Ambient Shadows, Ambient Light and Photographic Exposure are not readable through "
                + "Revit's API";
            sections.Add(text);
            sectionsRead++;
        }
        catch (Exception ex) { findings.Add("The graphic display options could not be read: " + ex.Message); }
    }
    else sections.Add("Graphic Display - '" + view.Name + "' has no visual style");

    // ---- Camera ----
    if (view3D != null)
    {
        try
        {
            var o = view3D.GetOrientation();
            Func<BuiltInParameter, string> length = which =>
            {
                var p = view3D.get_Parameter(which);
                return p == null || !p.HasValue ? "not given" : mm(p.AsDouble()) + " mm";
            };
            var farActive = view3D.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR);
            var text = "Camera - " + (view3D.IsPerspective ? "perspective" : "isometric") + ", eye "
                + mm(o.EyePosition.X) + "," + mm(o.EyePosition.Y) + "," + mm(o.EyePosition.Z) + " mm, looking "
                + show(o.ForwardDirection.X) + "," + show(o.ForwardDirection.Y) + "," + show(o.ForwardDirection.Z)
                + (view3D.IsPerspective ? ", eye elevation " + length(BuiltInParameter.VIEWER_EYE_ELEVATION)
                    + ", target elevation " + length(BuiltInParameter.VIEWER_TARGET_ELEVATION) : "")
                + ", far clip " + (farActive != null && farActive.AsInteger() == 1
                    ? "on at " + length(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR) : "off");
            if (view3D.CropBoxActive)
            {
                var crop = view3D.CropBox;
                text += ", crop box in the view's frame X " + mm(crop.Min.X) + " to " + mm(crop.Max.X) + ", Y "
                    + mm(crop.Min.Y) + " to " + mm(crop.Max.Y) + " mm";
            }
            else text += ", not cropped";
            sections.Add(text);
            sectionsRead++;
        }
        catch (Exception ex) { findings.Add("The camera could not be read: " + ex.Message); }
    }

    setup = "'" + view.Name + "' (" + view.ViewType + "):  ||  " + string.Join("  ||  ", sections);
    findings.Add(sectionsRead + " section(s) read for '" + view.Name + "'");
}
