// NOT STANDALONE. Assumes `doc`, `view`, `sunIntensity`, `shadowIntensity`,
// `silhouettes`, `smoothLines`, `transparency`, `hiddenLines`, `sketchyLines`,
// `depthCueing`, `background`, `backgroundImage`, `castShadows`,
// `ambientShadows`, `ambientLight` and `photographicExposure` are in scope;
// leaves `changed`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ONE VIEW'S GRAPHIC DISPLAY OPTIONS - the dialog behind the View Control
// Bar's visual style: the sun and shadow intensity, silhouette edges, smooth
// lines with anti-aliasing, transparency, hidden lines, sketchy lines, depth
// cueing and the background. Every input is a word or "keep". The visual style
// itself - Shaded, Realistic and the rest - is SET_VIEW_PROPERTIES'.
//
// WHERE REVIT'S API STOPS. Cast Shadows, Show Ambient Shadows, Ambient Light
// and Photographic Exposure have no member on any release 2020 to 2027 - read
// off every release's API reference on 2026-10-20. They are inputs here so
// that asking for one is answered in plain words, never silently dropped:
// each is named as the modeller's to tick in the dialog, by hand. A background
// cannot be set back to None either: Revit gives a way to make a sky, a
// gradient or an image background and no way to make none.
//
// READ BACK: every setting asked is read again; only one that reads as asked
// counts in `changed`.

var changed = 0;
var findings = new List<string>();
string refused = null;
var readBack = "";
var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
Func<string, bool> isKeep = text =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    return t.Length == 0 || t == "keep";
};
Func<string, double?> number = text =>
{
    double value;
    var t = (text ?? "").Trim().TrimEnd('%').Trim();
    return double.TryParse(t, System.Globalization.NumberStyles.Float, culture, out value) ? (double?)value : null;
};
Func<string, bool?> yesNo = text =>
{
    var t = squash(text);
    if (t == "yes" || t == "on" || t == "true") return true;
    if (t == "no" || t == "off" || t == "false") return false;
    return null;
};
// "jitter=5; extension=3" as key=value pairs, keys squashed.
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
Func<Color, string> showColour = c => c == null || !c.IsValid ? "none" : c.Red + "," + c.Green + "," + c.Blue;

if (view == null) refused = "No view was named, so NOTHING WAS CHANGED.";
else if (view.IsTemplate) refused = "'" + view.Name + "' is a view template; graphic display options are set on a view "
    + "here. NOTHING WAS CHANGED.";
else if (!view.HasDisplayStyle()) refused = "'" + view.Name + "' has no Graphic Display Options - Revit gives them to "
    + "views with a visual style. NOTHING WAS CHANGED.";

// ---- the four Revit gives no tool ----
var byHand = new List<string>();
foreach (var unreachable in new[] { new[] { castShadows, "Cast Shadows" }, new[] { ambientShadows, "Show Ambient Shadows" },
    new[] { ambientLight, "Ambient Light" }, new[] { photographicExposure, "Photographic Exposure" } })
    if (!isKeep(unreachable[0])) byHand.Add(unreachable[1]);
if (byHand.Count > 0)
    findings.Add("Revit's API has no member for " + string.Join(", ", byHand) + " on any release 2020 to 2027, so "
        + (byHand.Count == 1 ? "it was" : "they were") + " not set. Tick " + (byHand.Count == 1 ? "it" : "them")
        + " in the view's Graphic Display Options dialog (View Control Bar, visual style, Graphic Display Options), or "
        + "use the View Control Bar's Shadows button for Cast Shadows");

// ---- read every word first ----
int? wantSun = null, wantShadow = null, wantTransparency = null;
bool? wantSilhouettes = null, wantSmooth = null;
ElementId wantSilhouetteStyle = null;
string silhouetteStyleName = null;
ShowHiddenLinesValues? wantHidden = null;
bool? wantSketchy = null;
int? wantJitter = null, wantExtension = null;
bool? wantCueing = null;
int? wantStart = null, wantEnd = null, wantFade = null;
ViewDisplayBackground wantBackground = null;
var backgroundWord = "";

Func<string, string, int, int, int?> percent = (text, what, low, high) =>
{
    var n = number(text);
    if (n == null || n < low || n > high || Math.Abs(n.Value - Math.Round(n.Value)) > 1e-9)
    {
        refused = "'" + text + "' is not a " + what + " - a whole number " + low + " to " + high + ". NOTHING WAS CHANGED.";
        return null;
    }
    return (int)Math.Round(n.Value);
};

if (refused == null && !isKeep(sunIntensity)) wantSun = percent(sunIntensity, "sun intensity", 0, 100);
if (refused == null && !isKeep(shadowIntensity)) wantShadow = percent(shadowIntensity, "shadow intensity", 0, 100);
if (refused == null && !isKeep(transparency)) wantTransparency = percent(transparency, "transparency", 0, 100);
if (refused == null && !isKeep(silhouettes))
{
    var on = yesNo(silhouettes);
    if (on != null) wantSilhouettes = on;
    else
    {
        // A line style's name turns silhouettes on in that style.
        var key = squash(silhouettes);
        var lines = Category.GetCategory(doc, BuiltInCategory.OST_Lines);
        var styles = new List<KeyValuePair<string, ElementId>>();
        if (lines != null)
            foreach (Category sub in lines.SubCategories)
            {
                var style = sub.GetGraphicsStyle(GraphicsStyleType.Projection);
                if (style != null) styles.Add(new KeyValuePair<string, ElementId>(sub.Name, style.Id));
            }
        var match = styles.Where(s => squash(s.Key) == key).ToList();
        if (match.Count == 1)
        {
            wantSilhouettes = true;
            wantSilhouetteStyle = match[0].Value;
            silhouetteStyleName = match[0].Key;
        }
        else refused = "'" + silhouettes + "' is not on, off or a line style. The line styles: "
            + string.Join(", ", styles.Select(s => s.Key).OrderBy(n => n)) + ". NOTHING WAS CHANGED.";
    }
}
if (refused == null && !isKeep(smoothLines))
{
    wantSmooth = yesNo(smoothLines);
    if (wantSmooth == null) refused = "'" + smoothLines + "' is not yes or no for smooth lines with anti-aliasing. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(hiddenLines))
{
    var h = squash(hiddenLines);
    if (h == "none") wantHidden = ShowHiddenLinesValues.None;
    else if (h == "bydiscipline") wantHidden = ShowHiddenLinesValues.ByDiscipline;
    else if (h == "all") wantHidden = ShowHiddenLinesValues.All;
    else refused = "'" + hiddenLines + "' is not a Show Hidden Lines choice. Say None, By Discipline or All. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(sketchyLines))
{
    var on = yesNo(sketchyLines);
    var settings = pairs(sketchyLines);
    if (on != null) wantSketchy = on;
    else if (settings.Count > 0)
    {
        wantSketchy = true;
        foreach (var pair in settings)
        {
            if (pair.Key == "jitter") wantJitter = percent(pair.Value, "sketchy lines jitter", 0, 10);
            else if (pair.Key == "extension") wantExtension = percent(pair.Value, "sketchy lines extension", 0, 10);
            else refused = "'" + pair.Key + "' is not a sketchy lines setting. Say jitter= and extension=, 0 to 10. "
                + "NOTHING WAS CHANGED.";
            if (refused != null) break;
        }
    }
    else refused = "'" + sketchyLines + "' is not on, off or 'jitter=N; extension=N'. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(depthCueing))
{
    var on = yesNo(depthCueing);
    var settings = pairs(depthCueing);
    if (on != null) wantCueing = on;
    else if (settings.Count > 0)
    {
        wantCueing = true;
        foreach (var pair in settings)
        {
            if (pair.Key == "start") wantStart = percent(pair.Value, "depth cueing start", 0, 100);
            else if (pair.Key == "end") wantEnd = percent(pair.Value, "depth cueing end", 0, 100);
            else if (pair.Key == "fade" || pair.Key == "fadeto") wantFade = percent(pair.Value, "depth cueing fade", 0, 100);
            else refused = "'" + pair.Key + "' is not a depth cueing setting. Say start=, end= and fade=, 0 to 100. "
                + "NOTHING WAS CHANGED.";
            if (refused != null) break;
        }
    }
    else refused = "'" + depthCueing + "' is not on, off or 'start=N; end=N; fade=N'. NOTHING WAS CHANGED.";
    if (refused == null && (wantStart != null) != (wantEnd != null))
        refused = "Depth cueing takes its start and end together. NOTHING WAS CHANGED.";
    if (refused == null && wantStart != null && wantStart >= wantEnd)
        refused = "Depth cueing must start before it ends. NOTHING WAS CHANGED.";
    if (refused == null && !view.CanUseDepthCueing())
    {
        // The reason names the view's discipline (version 3, row 5b-1283):
        // Revit 2020 offered depth cueing in an elevation only once its View
        // Discipline was Architectural, measured 2026-10-20.
        var discipline = view.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE);
        var disciplineWord = discipline == null ? null : discipline.AsValueString();
        refused = (view.ViewType == ViewType.Elevation || view.ViewType == ViewType.Section) && !string.IsNullOrEmpty(disciplineWord)
            ? "'" + view.Name + "' cannot use depth cueing - its View Discipline is " + disciplineWord + ", and Revit 2020 offered "
                + "depth cueing in an elevation only once that was Architectural (Properties, View Discipline). NOTHING WAS CHANGED."
            : "'" + view.Name + "' cannot use depth cueing - Revit offers it in elevations and sections only. NOTHING WAS CHANGED.";
    }
}
if (refused == null && !isKeep(background))
{
    var raw = background.Trim();
    var b = squash(raw);
    if (b == "none")
        refused = "Revit's API gives a way to make a sky, gradient or image background and none to set it back to None "
            + "- choose None in the Graphic Display Options dialog by hand. NOTHING WAS CHANGED.";
    else if (b == "sky")
    {
        wantBackground = ViewDisplayBackground.CreateSky();
        backgroundWord = "Sky";
    }
    else if (b.StartsWith("gradient"))
    {
        var colours = pairs(raw.Substring(raw.ToLowerInvariant().IndexOf("gradient") + 8));
        var current = view.GetBackground();
        var sky = colours.ContainsKey("sky") ? colour(colours["sky"]) : current.SkyColor;
        var horizon = colours.ContainsKey("horizon") ? colour(colours["horizon"]) : current.BackgroundColor;
        var ground = colours.ContainsKey("ground") ? colour(colours["ground"]) : current.GroundColor;
        if (sky == null || horizon == null || ground == null || !sky.IsValid || !horizon.IsValid || !ground.IsValid)
            refused = "'" + background + "' needs three colours Heron can read: 'gradient sky=R,G,B; horizon=R,G,B; "
                + "ground=R,G,B'. NOTHING WAS CHANGED.";
        else
        {
            wantBackground = ViewDisplayBackground.CreateGradient(sky, horizon, ground);
            backgroundWord = "Gradient " + showColour(sky) + " / " + showColour(horizon) + " / " + showColour(ground);
        }
    }
    else if (b.StartsWith("image"))
    {
        var path = raw.Substring(raw.ToLowerInvariant().IndexOf("image") + 5).Trim().TrimStart(':').Trim().Trim('"');
        if (path.Length == 0 || !System.IO.File.Exists(path))
            refused = "The background image '" + path + "' is not a file on this PC. Revit takes a bmp, jpg, jpeg, png "
                + "or tif that exists. NOTHING WAS CHANGED.";
        else
        {
            var options = pairs(backgroundImage);
            var flags = ViewDisplayBackgroundImageFlags.None;
            var words = squash(backgroundImage);
            if (words.Contains("fittoscreen") || words.Contains("fit")) flags |= ViewDisplayBackgroundImageFlags.FitToScreen;
            if (words.Contains("fixedaspectratio") || words.Contains("aspect")) flags |= ViewDisplayBackgroundImageFlags.FixedAspectRatio;
            if (words.Contains("tile")) flags |= ViewDisplayBackgroundImageFlags.UseTiling;
            Func<string, UV> pairOf = text =>
            {
                var bits = (text ?? "").Split(',');
                var u = bits.Length == 2 ? number(bits[0]) : null;
                var v = bits.Length == 2 ? number(bits[1]) : null;
                return u == null || v == null ? null : new UV(u.Value, v.Value);
            };
            var offset = options.ContainsKey("offset") ? pairOf(options["offset"]) : new UV(0, 0);
            var scale = options.ContainsKey("scale") ? pairOf(options["scale"]) : new UV(1, 1);
            if (offset == null || scale == null)
                refused = "The image offset and scale are two numbers each - 'offset=0,0; scale=1,1'. NOTHING WAS CHANGED.";
            else
            {
                wantBackground = ViewDisplayBackground.CreateImage(path, flags, offset, scale);
                backgroundWord = "Image '" + path + "'";
            }
        }
    }
    else refused = "No background is called '" + background + "'. Say Sky, 'gradient sky=R,G,B; horizon=R,G,B; "
        + "ground=R,G,B' or 'image <file>'. NOTHING WAS CHANGED.";
}

var asked = (wantSun != null ? 1 : 0) + (wantShadow != null ? 1 : 0) + (wantTransparency != null ? 1 : 0)
    + (wantSilhouettes != null ? 1 : 0) + (wantSmooth != null ? 1 : 0) + (wantHidden != null ? 1 : 0)
    + (wantSketchy != null ? 1 : 0) + (wantCueing != null ? 1 : 0) + (wantBackground != null ? 1 : 0);
if (refused == null && asked == 0)
    refused = byHand.Count > 0
        ? "Only settings Revit's API cannot reach were asked for (" + string.Join(", ", byHand) + "). NOTHING WAS CHANGED."
        : "Nothing was asked for - give an intensity, silhouettes, smooth lines, transparency, hidden lines, sketchy "
            + "lines, depth cueing or a background. Anything left out is deliberately not touched. NOTHING WAS CHANGED.";

// ---- set ----
if (refused == null)
{
    Action<string, Action> attempt = (what, act) =>
    {
        try { act(); }
        catch (Exception ex) { findings.Add("Revit refused the " + what + ": " + ex.Message); }
    };
    if (wantSun != null) attempt("sun intensity", () => view.SunlightIntensity = wantSun.Value);
    if (wantShadow != null) attempt("shadow intensity", () => view.ShadowIntensity = wantShadow.Value);
    if (wantSilhouettes != null || wantSmooth != null || wantTransparency != null || wantHidden != null)
    {
        attempt("model display", () =>
        {
            var model = view.GetViewDisplayModel();
            if (wantSilhouettes != null) model.EnableSilhouettes = wantSilhouettes.Value;
            if (wantSilhouetteStyle != null) model.SilhouetteEdgesGStyleId = wantSilhouetteStyle;
            if (wantSmooth != null) model.SmoothEdges = wantSmooth.Value;
            if (wantTransparency != null) model.Transparency = wantTransparency.Value;
            if (wantHidden != null) model.ShowHiddenLines = wantHidden.Value;
            // A VIEW WITH NO SILHOUETTE LINE STYLE (version 2, row 5b-1279).
            // Measured 2026-10-20 on Revit 2020 (Group IB, IB5): a perspective
            // view FRAME_ELEMENTS_IN_CAMERA had just made held silhouette style
            // id -1, and SetViewDisplayModel refused smooth lines with "The
            // silhouette line style id is not a valid line style to apply to
            // the view." Given a line style first, the same call took. So a view
            // with none is given Wide Lines, Revit's own, and silhouettes are
            // left on or off as they were - it shows nowhere while they are off.
            if (!(doc.GetElement(model.SilhouetteEdgesGStyleId) is GraphicsStyle))
            {
                var lineStyles = Category.GetCategory(doc, BuiltInCategory.OST_Lines);
                GraphicsStyle wideLines = null;
                if (lineStyles != null)
                    foreach (Category sub in lineStyles.SubCategories)
                        if (sub.Name.Trim('<', '>') == "Wide Lines") wideLines = sub.GetGraphicsStyle(GraphicsStyleType.Projection);
                if (wideLines != null)
                {
                    model.SilhouetteEdgesGStyleId = wideLines.Id;
                    findings.Add("'" + view.Name + "' had no silhouette line style, and Revit takes no display change without one - "
                        + "it was given Wide Lines; silhouettes stay " + (model.EnableSilhouettes ? "on" : "off"));
                }
            }
            view.SetViewDisplayModel(model);
        });
    }
    if (wantSketchy != null)
    {
        attempt("sketchy lines", () =>
        {
            var sketchy = view.GetSketchyLines();
            sketchy.EnableSketchyLines = wantSketchy.Value;
            if (wantJitter != null) sketchy.Jitter = wantJitter.Value;
            if (wantExtension != null) sketchy.Extension = wantExtension.Value;
            view.SetSketchyLines(sketchy);
        });
    }
    if (wantCueing != null)
    {
        attempt("depth cueing", () =>
        {
            var cueing = view.GetDepthCueing();
            cueing.EnableDepthCueing = wantCueing.Value;
            if (wantStart != null) cueing.SetStartEndPercentages(wantStart.Value, wantEnd.Value);
            if (wantFade != null) cueing.FadeTo = wantFade.Value;
            view.SetDepthCueing(cueing);
        });
    }
    if (wantBackground != null) attempt("background", () => view.SetBackground(wantBackground));
}

// ---- read back ----
if (refused == null)
{
    var notTaken = new List<string>();
    Action<bool, string> count = (ok, what) => { if (ok) changed++; else notTaken.Add(what); };
    var model = view.GetViewDisplayModel();
    var sketchy = view.GetSketchyLines();
    // DEPTH CUEING IS READ ONLY WHERE THE VIEW OFFERS IT (version 2, row
    // 5b-1279). Measured 2026-10-20 on Revit 2020 (Group IB, IB5): on a
    // perspective 3D view GetDepthCueing threw "This view cannot use Depth
    // Cueing" in this read-back, though depth cueing was not asked for - every
    // change made was rolled back with it.
    var cueing = view.CanUseDepthCueing() ? view.GetDepthCueing() : null;
    var bg = view.GetBackground();
    var silhouetteStyle = doc.GetElement(model.SilhouetteEdgesGStyleId) as GraphicsStyle;

    if (wantSun != null) count(view.SunlightIntensity == wantSun.Value, "sun intensity " + wantSun);
    if (wantShadow != null) count(view.ShadowIntensity == wantShadow.Value, "shadow intensity " + wantShadow);
    if (wantTransparency != null) count(model.Transparency == wantTransparency.Value, "transparency " + wantTransparency);
    if (wantSilhouettes != null)
        count(model.EnableSilhouettes == wantSilhouettes.Value
              && (wantSilhouetteStyle == null || model.SilhouetteEdgesGStyleId == wantSilhouetteStyle),
            "silhouettes " + silhouettes);
    if (wantSmooth != null) count(model.SmoothEdges == wantSmooth.Value, "smooth lines " + smoothLines);
    if (wantHidden != null) count(model.ShowHiddenLines == wantHidden.Value, "hidden lines " + hiddenLines);
    if (wantSketchy != null)
        count(sketchy.EnableSketchyLines == wantSketchy.Value && (wantJitter == null || sketchy.Jitter == wantJitter)
              && (wantExtension == null || sketchy.Extension == wantExtension), "sketchy lines " + sketchyLines);
    if (wantCueing != null)
        count(cueing.EnableDepthCueing == wantCueing.Value && (wantStart == null || cueing.StartPercentage == wantStart)
              && (wantEnd == null || cueing.EndPercentage == wantEnd) && (wantFade == null || cueing.FadeTo == wantFade),
            "depth cueing " + depthCueing);
    if (wantBackground != null) count(bg.Type == wantBackground.Type
        && (bg.Type != ViewDisplayBackgroundType.Image || string.Equals(bg.ImagePath, wantBackground.ImagePath,
            StringComparison.OrdinalIgnoreCase)), "background " + backgroundWord);

    readBack = "'" + view.Name + "' (" + view.DisplayStyle + "): sun intensity " + view.SunlightIntensity
        + ", shadow intensity " + view.ShadowIntensity
        + "; silhouettes " + (model.EnableSilhouettes ? "on" + (silhouetteStyle != null ? " (" + silhouetteStyle.Name + ")" : "") : "off")
        + ", smooth lines " + (model.SmoothEdges ? "on" : "off") + ", transparency " + model.Transparency
        + ", hidden lines " + model.ShowHiddenLines
        + "; sketchy lines " + (sketchy.EnableSketchyLines ? "on, jitter " + sketchy.Jitter + ", extension " + sketchy.Extension : "off")
        + "; depth cueing " + (cueing == null ? "not offered in this view"
            : cueing.EnableDepthCueing ? "on, " + cueing.StartPercentage + "-" + cueing.EndPercentage
            + "%, fade " + cueing.FadeTo : "off")
        + "; background " + bg.Type
        + (bg.Type == ViewDisplayBackgroundType.Gradient ? " " + showColour(bg.SkyColor) + " / " + showColour(bg.BackgroundColor)
            + " / " + showColour(bg.GroundColor) : "")
        + (bg.Type == ViewDisplayBackgroundType.Image ? " '" + bg.ImagePath + "'" : "")
        + ". Cast Shadows, Ambient Shadows, Ambient Light and Photographic Exposure are not readable through Revit's API.";

    findings.Add(changed + " of " + asked + " display setting(s) read back as asked on '" + view.Name + "'"
        + (notTaken.Count == 0 ? "" : " - not taken: " + string.Join("; ", notTaken)));
}
if (refused != null) findings.Add(refused);
