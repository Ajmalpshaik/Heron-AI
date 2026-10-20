// NOT STANDALONE. Assumes `doc`, `view`, `preset`, `study`, `date`, `time`,
// `endDate`, `endTime`, `interval`, `sunriseToSunset`, `azimuthDeg`,
// `altitudeDeg`, `relativeToView` and `groundPlaneLevel` are in scope; leaves
// `changed`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ONE VIEW'S SUN SETTINGS - what Revit's Sun Settings dialog holds for it: the
// study (Still, Single Day, Multi-Day, Lighting), its date and time, the
// lighting sun's azimuth and altitude, the ground plane - or a preset the
// project already holds, copied onto the view. Every input is a word or "keep".
//
// TIMES ARE THE PROJECT'S CLOCK. A time typed here is read as the site's own
// time and turned to UTC by SiteLocation.ConvertFromProjectTime, the API's own
// conversion - which, its reference says, does not apply daylight saving. The
// read-back turns it back the same way and says when the site uses daylight
// saving, because Revit's dialog may then show it an hour on.
//
// THE SITE ITSELF - city, latitude, longitude, time zone - is
// SET_PROJECT_LOCATION's, an Admin change for the whole project, and is only
// read back here.
//
// A PRESET IS COPIED, NOT LINKED: its study, times, interval and lighting
// angles are written onto the view's own settings, and GetMatchingPreset must
// then name it - that is the read-back.
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
    var t = (text ?? "").Trim().TrimEnd('°').Trim();
    return double.TryParse(t, System.Globalization.NumberStyles.Float, culture, out value) ? (double?)value : null;
};
Func<string, bool?> yesNo = text =>
{
    var t = squash(text);
    if (t == "yes" || t == "on" || t == "true") return true;
    if (t == "no" || t == "off" || t == "false") return false;
    return null;
};
var dateFormats = new[] { "yyyy-MM-dd", "d MMMM yyyy", "d MMM yyyy", "MMMM d yyyy", "MMMM d, yyyy", "d MMMM", "d MMM",
    "MMMM d", "MMM d" };
var timeFormats = new[] { "H:mm", "HH:mm", "h:mm tt", "h:mmtt", "h tt", "htt" };
Func<string, DateTime?> readDate = text =>
{
    DateTime d;
    return DateTime.TryParseExact((text ?? "").Trim(), dateFormats, culture, System.Globalization.DateTimeStyles.None, out d)
        ? (DateTime?)d : null;
};
Func<string, TimeSpan?> readTime = text =>
{
    DateTime d;
    return DateTime.TryParseExact((text ?? "").Trim().ToUpperInvariant(), timeFormats, culture,
        System.Globalization.DateTimeStyles.None, out d) ? (TimeSpan?)d.TimeOfDay : null;
};
var site = doc.SiteLocation;
// UTC as Revit holds it, to the site's clock, and back.
Func<DateTime, DateTime> toProject = utc => site.ConvertToProjectTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
Func<DateTime, DateTime> toUtc = project =>
    DateTime.SpecifyKind(site.ConvertFromProjectTime(DateTime.SpecifyKind(project, DateTimeKind.Unspecified)), DateTimeKind.Utc);
Func<DateTime, string> showTime = t => t.ToString("yyyy-MM-dd HH:mm", culture);

var studyWords = new Dictionary<string, SunAndShadowType>
{
    { "still", SunAndShadowType.StillImage }, { "stillimage", SunAndShadowType.StillImage },
    { "singleday", SunAndShadowType.OneDayStudy }, { "oneday", SunAndShadowType.OneDayStudy },
    { "multiday", SunAndShadowType.MultiDayStudy }, { "lighting", SunAndShadowType.Lighting },
};
Func<SunAndShadowType, string> studyWord = s =>
    s == SunAndShadowType.StillImage ? "Still" : s == SunAndShadowType.OneDayStudy ? "Single Day"
    : s == SunAndShadowType.MultiDayStudy ? "Multi-Day" : "Lighting";
// Revit's interval names, matched by name because the seconds and the minute
// are values only from Revit 2024.
var intervalNames = new Dictionary<string, string>
{
    { "15seconds", "Seconds15" }, { "30seconds", "Seconds30" }, { "45seconds", "Seconds45" },
    { "1minute", "Minute" }, { "minute", "Minute" }, { "15minutes", "Minutes15" }, { "30minutes", "Minutes30" },
    { "45minutes", "Minutes45" }, { "1hour", "Hour" }, { "hour", "Hour" }, { "1day", "Day" }, { "day", "Day" },
    { "1week", "Week" }, { "week", "Week" }, { "1month", "Month" }, { "month", "Month" },
};

SunAndShadowSettings sun = null;
if (view == null) refused = "No view was named, so NOTHING WAS CHANGED.";
else if (view.IsTemplate) refused = "'" + view.Name + "' is a view template; sun settings are set on a view. "
    + "NOTHING WAS CHANGED.";
else
{
    try { sun = view.SunAndShadowSettings; } catch (Exception) { sun = null; }
    if (sun == null) refused = "'" + view.Name + "' has no sun settings - Revit gives them to views that can show "
        + "the sun, such as 3D views, plans and elevations. NOTHING WAS CHANGED.";
}

// ---- read every word first ----
SunAndShadowSettings wantPreset = null;
SunAndShadowType? wantStudy = null;
DateTime? wantDate = null;
TimeSpan? wantTime = null;
DateTime? wantEndDate = null;
TimeSpan? wantEndTime = null;
SunStudyTimeInterval? wantInterval = null;
bool? wantSunrise = null;
double? wantAzimuth = null;
double? wantAltitude = null;
bool? wantRelative = null;
var groundAsked = false;
Level wantGround = null;

if (refused == null && !isKeep(preset))
{
    var presets = new FilteredElementCollector(doc).OfClass(typeof(SunAndShadowSettings))
        .Cast<SunAndShadowSettings>().Where(p => p.OwnerViewId == ElementId.InvalidElementId).ToList();
    wantPreset = presets.FirstOrDefault(p => string.Equals(p.Name, preset.Trim(), StringComparison.OrdinalIgnoreCase));
    if (wantPreset == null) refused = "No sun preset is called '" + preset + "'. The project's presets: "
        + (presets.Count == 0 ? "none" : string.Join(", ", presets.Select(p => p.Name).OrderBy(n => n))) + ". "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(study))
{
    SunAndShadowType s;
    if (studyWords.TryGetValue(squash(study), out s)) wantStudy = s;
    else refused = "No sun study is called '" + study + "'. Say Still, Single Day, Multi-Day or Lighting. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(date))
{
    wantDate = readDate(date);
    if (wantDate == null) refused = "'" + date + "' is not a date Heron can read - say 2026-06-21 or 21 June 2026. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(time))
{
    wantTime = readTime(time);
    if (wantTime == null) refused = "'" + time + "' is not a time Heron can read - say 10:00, 14:30 or 10 am. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(endDate))
{
    wantEndDate = readDate(endDate);
    if (wantEndDate == null) refused = "'" + endDate + "' is not an end date Heron can read - say 2026-12-21. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(endTime))
{
    wantEndTime = readTime(endTime);
    if (wantEndTime == null) refused = "'" + endTime + "' is not an end time Heron can read - say 17:00. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(interval))
{
    // "15 mins", "1 hr", "one hour" all come to a number and Revit's unit.
    var said = squash(interval).Replace("one", "1");
    var digits = new string(said.TakeWhile(char.IsDigit).ToArray());
    var unit = said.Substring(digits.Length);
    unit = unit.StartsWith("sec") ? "seconds" : unit.StartsWith("min") ? (digits == "1" || digits == "" ? "minute" : "minutes")
        : unit.StartsWith("h") ? "hour" : unit.StartsWith("d") ? "day" : unit.StartsWith("w") ? "week"
        : unit.StartsWith("mo") ? "month" : unit;
    string revitName;
    if (intervalNames.TryGetValue((digits == "1" && unit != "minute" ? "" : digits) + unit, out revitName)
        || intervalNames.TryGetValue(digits + unit, out revitName))
    {
        foreach (SunStudyTimeInterval candidate in Enum.GetValues(typeof(SunStudyTimeInterval)))
            if (candidate.ToString() == revitName) wantInterval = candidate;
    }
    if (wantInterval == null)
        refused = "No time interval is called '" + interval + "' in this Revit. It takes: "
            + string.Join(", ", Enum.GetValues(typeof(SunStudyTimeInterval)).Cast<SunStudyTimeInterval>().Select(v => v.ToString()))
            + ". NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(sunriseToSunset))
{
    wantSunrise = yesNo(sunriseToSunset);
    if (wantSunrise == null) refused = "'" + sunriseToSunset + "' is not yes or no for sunrise to sunset. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(azimuthDeg))
{
    wantAzimuth = number(azimuthDeg);
    if (wantAzimuth == null || wantAzimuth < 0 || wantAzimuth > 360)
        refused = "'" + azimuthDeg + "' is not an azimuth - degrees, 0 to 360. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(altitudeDeg))
{
    wantAltitude = number(altitudeDeg);
    if (wantAltitude == null || wantAltitude < 0 || wantAltitude > 90)
        refused = "'" + altitudeDeg + "' is not an altitude - degrees above the horizon, 0 to 90. NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(relativeToView))
{
    wantRelative = yesNo(relativeToView);
    if (wantRelative == null) refused = "'" + relativeToView + "' is not yes or no for relative to view. "
        + "NOTHING WAS CHANGED.";
}
if (refused == null && !isKeep(groundPlaneLevel))
{
    groundAsked = true;
    if (squash(groundPlaneLevel) != "off" && squash(groundPlaneLevel) != "none")
    {
        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
        wantGround = levels.FirstOrDefault(l => string.Equals(l.Name, groundPlaneLevel.Trim(), StringComparison.OrdinalIgnoreCase));
        if (wantGround == null) refused = "No level is called '" + groundPlaneLevel + "'. The levels: "
            + string.Join(", ", levels.OrderBy(l => l.Elevation).Select(l => l.Name)) + ". NOTHING WAS CHANGED.";
        else if (!sun.IsGroundPlaneLevelValid(wantGround.Id))
            refused = "Revit will not take level '" + wantGround.Name + "' as the ground plane. NOTHING WAS CHANGED.";
    }
}
if ((wantAzimuth != null || wantAltitude != null || wantRelative != null) && refused == null
    && (wantStudy ?? (wantPreset != null ? wantPreset.SunAndShadowType : sun.SunAndShadowType)) != SunAndShadowType.Lighting)
    findings.Add("An azimuth, altitude or relative-to-view was given for a study that is not Lighting - Revit keeps them "
        + "and uses them only in a Lighting study");

var asked = (wantPreset != null ? 1 : 0) + (wantStudy != null ? 1 : 0) + (wantDate != null || wantTime != null ? 1 : 0)
    + (wantEndDate != null || wantEndTime != null ? 1 : 0) + (wantInterval != null ? 1 : 0) + (wantSunrise != null ? 1 : 0)
    + (wantAzimuth != null ? 1 : 0) + (wantAltitude != null ? 1 : 0) + (wantRelative != null ? 1 : 0)
    + (groundAsked ? 1 : 0);
if (refused == null && asked == 0)
    refused = "Nothing was asked for - give a preset, study, date, time, interval, lighting angle or ground plane. "
        + "Anything left out is deliberately not touched. NOTHING WAS CHANGED.";

// ---- set ----
DateTime askedStart = DateTime.MinValue;
DateTime askedEnd = DateTime.MinValue;
if (refused == null)
{
    Action<string, Action> attempt = (what, act) =>
    {
        try { act(); }
        catch (Exception ex) { findings.Add("Revit refused the " + what + ": " + ex.Message); }
    };

    if (wantPreset != null)
    {
        attempt("preset's study", () => sun.SunAndShadowType = wantPreset.SunAndShadowType);
        attempt("preset's start", () => sun.StartDateAndTime = DateTime.SpecifyKind(wantPreset.StartDateAndTime, DateTimeKind.Utc));
        attempt("preset's end", () => sun.EndDateAndTime = DateTime.SpecifyKind(wantPreset.EndDateAndTime, DateTimeKind.Utc));
        attempt("preset's sunrise to sunset", () => sun.SunriseToSunset = wantPreset.SunriseToSunset);
        if (sun.IsTimeIntervalValid(wantPreset.TimeInterval))
            attempt("preset's interval", () => sun.TimeInterval = wantPreset.TimeInterval);
        attempt("preset's azimuth", () => sun.Azimuth = wantPreset.Azimuth);
        attempt("preset's altitude", () => sun.Altitude = wantPreset.Altitude);
        attempt("preset's relative to view", () => sun.RelativeToView = wantPreset.RelativeToView);
    }
    if (wantStudy != null) attempt("study", () => sun.SunAndShadowType = wantStudy.Value);
    if (wantDate != null || wantTime != null)
    {
        var now = toProject(sun.StartDateAndTime);
        var day = wantDate ?? now.Date;
        // A date with no year ("21 June") keeps the year the view already has.
        if (wantDate != null && !System.Text.RegularExpressions.Regex.IsMatch(date, @"\d{4}"))
            day = new DateTime(now.Year, day.Month, day.Day);
        askedStart = day.Date + (wantTime ?? now.TimeOfDay);
        attempt("date and time", () => sun.StartDateAndTime = toUtc(askedStart));
    }
    if (wantEndDate != null || wantEndTime != null)
    {
        var now = toProject(sun.EndDateAndTime);
        var day = wantEndDate ?? now.Date;
        if (wantEndDate != null && !System.Text.RegularExpressions.Regex.IsMatch(endDate, @"\d{4}"))
            day = new DateTime(now.Year, day.Month, day.Day);
        askedEnd = day.Date + (wantEndTime ?? now.TimeOfDay);
        attempt("end date and time", () => sun.EndDateAndTime = toUtc(askedEnd));
    }
    if (wantInterval != null)
    {
        if (sun.IsTimeIntervalValid(wantInterval.Value)) attempt("interval", () => sun.TimeInterval = wantInterval.Value);
        else findings.Add("Revit will not take a " + wantInterval.Value + " interval for a " + studyWord(sun.SunAndShadowType)
            + " study");
    }
    if (wantSunrise != null) attempt("sunrise to sunset", () => sun.SunriseToSunset = wantSunrise.Value);
    if (wantAzimuth != null) attempt("azimuth", () => sun.Azimuth = wantAzimuth.Value * Math.PI / 180.0);
    if (wantAltitude != null) attempt("altitude", () => sun.Altitude = wantAltitude.Value * Math.PI / 180.0);
    if (wantRelative != null) attempt("relative to view", () => sun.RelativeToView = wantRelative.Value);
    if (groundAsked)
    {
        attempt("ground plane", () =>
        {
            sun.UsesGroundPlane = wantGround != null;
            if (wantGround != null) sun.GroundPlaneLevelId = wantGround.Id;
        });
    }
    doc.Regenerate();
}

// ---- read back ----
if (refused == null)
{
    var notTaken = new List<string>();
    Action<bool, string> count = (ok, what) => { if (ok) changed++; else notTaken.Add(what); };
    Func<double, double> degrees = r => Math.Round(r * 180.0 / Math.PI, 2);
    string matching = null;
    try { matching = sun.GetMatchingPreset(); } catch (Exception) { matching = null; }
    var start = toProject(sun.StartDateAndTime);
    var end = toProject(sun.EndDateAndTime);

    if (wantPreset != null) count(string.Equals(matching, wantPreset.Name, StringComparison.OrdinalIgnoreCase),
        "preset " + wantPreset.Name + (string.IsNullOrEmpty(matching) ? "" : " (Revit matches '" + matching + "')"));
    if (wantStudy != null) count(sun.SunAndShadowType == wantStudy.Value, "study " + studyWord(wantStudy.Value));
    if (wantDate != null || wantTime != null)
        count(Math.Abs((start - askedStart).TotalMinutes) < 1, "date and time " + showTime(askedStart) + " (reads "
            + showTime(start) + ")");
    if (wantEndDate != null || wantEndTime != null)
        count(Math.Abs((end - askedEnd).TotalMinutes) < 1, "end " + showTime(askedEnd) + " (reads " + showTime(end) + ")");
    if (wantInterval != null) count(sun.TimeInterval == wantInterval.Value, "interval " + wantInterval.Value);
    if (wantSunrise != null) count(sun.SunriseToSunset == wantSunrise.Value, "sunrise to sunset " + sunriseToSunset);
    if (wantAzimuth != null) count(Math.Abs(degrees(sun.Azimuth) - wantAzimuth.Value) < 0.01, "azimuth " + azimuthDeg);
    if (wantAltitude != null) count(Math.Abs(degrees(sun.Altitude) - wantAltitude.Value) < 0.01, "altitude " + altitudeDeg);
    if (wantRelative != null) count(sun.RelativeToView == wantRelative.Value, "relative to view " + relativeToView);
    if (groundAsked)
        count(sun.UsesGroundPlane == (wantGround != null) && (wantGround == null || sun.GroundPlaneLevelId == wantGround.Id),
            "ground plane " + groundPlaneLevel);

    var ground = sun.UsesGroundPlane ? doc.GetElement(sun.GroundPlaneLevelId) as Level : null;
    readBack = "'" + view.Name + "': " + studyWord(sun.SunAndShadowType) + " study"
        + (sun.SunAndShadowType == SunAndShadowType.Lighting
            ? ", azimuth " + degrees(sun.Azimuth) + "°, altitude " + degrees(sun.Altitude) + "°, "
                + (sun.RelativeToView ? "relative to the view" : "relative to the model")
            : ", " + showTime(start)
                + (sun.SunAndShadowType == SunAndShadowType.StillImage ? ""
                    : " to " + showTime(end) + (sun.SunriseToSunset ? " (sunrise to sunset)" : "") + ", every "
                        + sun.TimeInterval))
        + "; preset " + (string.IsNullOrEmpty(matching) ? "none matches" : "'" + matching + "'")
        + "; ground plane " + (ground != null ? "at " + ground.Name : "off")
        + "; site '" + sun.ProjectLocationName + "', latitude " + degrees(sun.Latitude) + "°, longitude "
        + degrees(sun.Longitude) + "°, time zone " + sun.TimeZone.ToString(culture)
        + (sun.UsesDST ? ", daylight saving in use - times are the site's clock without it, as Revit's API converts "
            + "them, so the Sun Settings dialog may show them an hour on" : "") + ".";

    findings.Add(changed + " of " + asked + " sun setting(s) read back as asked on '" + view.Name + "'"
        + (notTaken.Count == 0 ? "" : " - not taken: " + string.Join("; ", notTaken)));
}
if (refused != null) findings.Add(refused);
