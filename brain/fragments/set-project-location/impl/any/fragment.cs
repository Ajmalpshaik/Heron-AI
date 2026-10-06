// NOT STANDALONE. Assumes `doc` and `settings` are in scope, and leaves
// `changed`, `alreadyThat`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts all of it back.
//
// WHERE THE PROJECT IS AND WHICH WAY IT FACES - Manage > Location and Manage >
// Position > Rotate True North - as one table of settings:
//
//   city=Doha, Qatar; true north=30 East
//   latitude=25.2854 N; longitude=51.5310 E; time zone=UTC+03:00
//
// A key named is set, a key left out (or left empty) is left alone, and an
// unknown key refuses the whole call by name. The keys: city, latitude,
// longitude, place name, time zone, daylight saving, true north.
//
// A CITY IS ONE FROM REVIT'S OWN LIST - the Location dialog's Default City
// List, read through Application.Cities - and brings its latitude, longitude,
// time zone and name. "Doha" finds "Doha, Qatar" when only one city in the
// list is called that; two of the same name are refused with both named. A
// time zone or place name given beside the city wins over the city's own.
//
// LATITUDE AND LONGITUDE GO TOGETHER. Half a move is refused - a site moved in
// latitude only is somewhere nobody asked for.
//
// THE ORDER IS REVIT'S, NOT A PREFERENCE. Setting latitude or longitude makes
// Revit pick a city it knows at those coordinates and work out a time zone
// (its API says so, and says the time zone can be wrong at a boundary), so
// the time zone and place name are written AFTER the coordinates, and every
// value is read back afterwards. A place name Revit left unchanged after the
// coordinates moved is said, because it then names somewhere else.
//
// DAYLIGHT SAVING CAN BE READ AND NOT SET. SunAndShadowSettings.UsesDST is
// get-only on every release 2020 to 2027 and nothing else holds it, so a
// daylight-saving value that differs from the model's refuses the call and
// says where Revit's own box is; one that already matches is "already that".
//
// TRUE NORTH IS IN THE SITE TAB'S WORDS - an angle East or West of project
// north - and it is MEASURED, never taken from the sign of Revit's angle,
// which the API documentation does not state. Revit is asked for the shared
// position of two points a hundred feet apart along project north (what
// Report Shared Coordinates gives), and the way that pair points says where
// true north is. The turn is made about the PROJECT BASE POINT, whose shared
// coordinates stay as they are - what typing Angle to True North on the
// project base point does - and both are read back: a turn that reads back
// the other way, or a base point whose shared position moved, throws, and the
// add-in rolls everything back. Nothing in the model moves; every shared
// coordinate a surveyor reads turns about the base point.
//
// COMPARED FIRST, READ BACK AFTER, the way SET_REVISION_SETTINGS does it. A
// setting already at the value asked is named in `alreadyThat` and not
// written. Revit refusing the FIRST write - nothing stored yet - is a refusal
// with Revit's own words and the plan it would have carried out, which is what
// makes a run with no transaction open a measurement. A later write failing
// after an earlier one went in throws, so the add-in rolls both back.

var changed = "";
var alreadyThat = "";
var findings = "";
var refused = "";

{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var floatStyle = System.Globalization.NumberStyles.Float;
    var degree = ((char)176).ToString();
    const double ToDegrees = 180.0 / Math.PI;
    // Half a millimetre in feet - how still the base point must stay.
    const double StillFeet = 0.0005 / 0.3048;
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
    Func<string, string> words = text =>
        string.Join(" ", (text ?? "").ToLowerInvariant().Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));

    // ---- the words a value is written in ------------------------------------
    Func<double, string> latitudeText = d =>
        Math.Abs(d).ToString("0.000000", invariant) + degree + (d < 0 ? " S" : " N");
    Func<double, string> longitudeText = d =>
        Math.Abs(d).ToString("0.000000", invariant) + degree + (d < 0 ? " W" : " E");
    Func<double, string> zoneText = hours =>
    {
        var minutes = (int)Math.Round(Math.Abs(hours) * 60.0);
        return "UTC" + (hours < 0 ? "-" : "+") + (minutes / 60).ToString("00", invariant) + ":"
             + (minutes % 60).ToString("00", invariant);
    };
    Func<double?, string> northText = east =>
    {
        if (!east.HasValue) return "could not be read";
        if (Math.Abs(east.Value) < 0.0005) return "0" + degree + " (project north is true north)";
        return Math.Abs(east.Value).ToString("0.###", invariant) + degree + (east.Value > 0 ? " East" : " West");
    };

    // ---- the readers ----------------------------------------------------------
    // A number of degrees with an optional side: "25.2854", "25.2854 N",
    // "51.531 deg E", "-33.9". `sides` is the letter that keeps the sign and
    // the letter that flips it - "NS" or "EW". A side AND a minus is refused:
    // "-25 S" says it twice, and which was meant is a guess.
    Func<string, string, double?> signedDegrees = (text, sides) =>
    {
        var t = (text ?? "").Trim().ToUpperInvariant().Replace(degree, " ");
        foreach (var word in new[] { "DEGREES", "DEGREE", "DEG" }) t = t.Replace(word, " ");
        t = t.Replace("NORTH", "N").Replace("SOUTH", "S").Replace("EAST", "E").Replace("WEST", "W").Trim();
        string side = null;
        if (t.Length > 0 && char.IsLetter(t[t.Length - 1])) { side = t.Substring(t.Length - 1); t = t.Substring(0, t.Length - 1).Trim(); }
        else if (t.Length > 0 && char.IsLetter(t[0])) { side = t.Substring(0, 1); t = t.Substring(1).Trim(); }
        double number;
        if (!double.TryParse(t, floatStyle, invariant, out number)) return null;
        if (side == null) return number;
        if (number < 0) return null;
        if (side == sides.Substring(0, 1)) return number;
        if (side == sides.Substring(1, 1)) return -number;
        return null;
    };
    // Hours from UTC: "3", "+3", "UTC+3", "UTC+03:00", "GMT-5", "+5:30", "5.5",
    // or the dialog's own "(UTC+03:00) Kuwait, Riyadh".
    Func<string, double?> hoursOf = text =>
    {
        var t = (text ?? "").Trim().ToUpperInvariant().Replace(" ", "");
        if (t.StartsWith("("))
        {
            var close = t.IndexOf(')');
            if (close < 0) return null;
            t = t.Substring(1, close - 1);
        }
        if (t.StartsWith("UTC") || t.StartsWith("GMT")) t = t.Substring(3);
        if (t.Length == 0) return 0.0;
        var sign = 1.0;
        if (t[0] == '+') t = t.Substring(1);
        else if (t[0] == '-') { sign = -1.0; t = t.Substring(1); }
        double hours = 0.0, minutes = 0.0;
        var colon = t.IndexOf(':');
        if (colon >= 0)
        {
            if (!double.TryParse(t.Substring(0, colon), System.Globalization.NumberStyles.None, invariant, out hours)) return null;
            if (!double.TryParse(t.Substring(colon + 1), System.Globalization.NumberStyles.None, invariant, out minutes)) return null;
            if (minutes >= 60.0) return null;
        }
        else if (!double.TryParse(t, floatStyle, invariant, out hours) || hours < 0) return null;
        return sign * (hours + minutes / 60.0);
    };
    // Degrees East of project north, West negative - "30 East", "30 deg W",
    // "0". A turn with no side is refused rather than guessed.
    Func<string, double?> eastOf = text =>
    {
        var t = (text ?? "").Trim().ToUpperInvariant().Replace(degree, " ");
        foreach (var word in new[] { "DEGREES", "DEGREE", "DEG" }) t = t.Replace(word, " ");
        t = t.Replace("EAST", "E").Replace("WEST", "W").Trim();
        string side = null;
        if (t.Length > 0 && char.IsLetter(t[t.Length - 1])) { side = t.Substring(t.Length - 1); t = t.Substring(0, t.Length - 1).Trim(); }
        double number;
        if (!double.TryParse(t, floatStyle, invariant, out number) || number < 0) return null;
        if (side == null) return number == 0.0 ? (double?)0.0 : null;
        if (side == "E") return number;
        if (side == "W") return -number;
        return null;
    };
    Func<string, bool?> yesOrNo = text =>
    {
        var t = squash(text);
        if (t == "yes" || t == "true" || t == "on" || t == "used" || t == "use" || t == "ticked") return true;
        if (t == "no" || t == "false" || t == "off" || t == "notused" || t == "dontuse" || t == "unticked") return false;
        return null;
    };

    // ---- what is asked ----------------------------------------------------------
    var keys = new Dictionary<string, string>
    {
        { "city", "city" },
        { "latitude", "latitude" }, { "lat", "latitude" },
        { "longitude", "longitude" }, { "long", "longitude" }, { "lon", "longitude" }, { "lng", "longitude" },
        { "placename", "place name" }, { "place", "place name" },
        { "timezone", "time zone" }, { "utcoffset", "time zone" }, { "utc", "time zone" },
        { "daylightsaving", "daylight saving" }, { "daylightsavings", "daylight saving" },
        { "daylightsavingtime", "daylight saving" }, { "daylightsavingstime", "daylight saving" },
        { "usedaylightsavingtime", "daylight saving" }, { "usedaylightsavingstime", "daylight saving" },
        { "dst", "daylight saving" },
        { "truenorth", "true north" }, { "angletotruenorth", "true north" },
        { "anglefromprojectnorthtotruenorth", "true north" }, { "rotatetruenorth", "true north" },
    };
    var keyList = "city, latitude, longitude, place name, time zone, daylight saving and true north";
    var asked = new Dictionary<string, string>();
    var problems = new List<string>();
    if (settings != null)
    {
        foreach (var pair in settings)
        {
            string setting;
            if (!keys.TryGetValue(squash(pair.Key), out setting))
            {
                problems.Add("'" + pair.Key + "' is not a site setting - the settings are " + keyList);
                continue;
            }
            var value = (pair.Value ?? "").Trim();
            if (value.Length == 0) continue;
            if (asked.ContainsKey(setting)) { problems.Add(setting + " is named twice - say it once"); continue; }
            asked[setting] = value;
        }
    }
    if (asked.Count == 0 && problems.Count == 0)
        problems.Add("nothing was asked for - name at least one of " + keyList);

    // ---- the model as it is ----------------------------------------------------
    SiteLocation site = null;
    ProjectLocation location = null;
    if (doc.IsFamilyDocument) problems.Add("'" + doc.Title + "' is a family, and a family has no site - open the project");
    else
    {
        try { site = doc.SiteLocation; } catch (Exception) { site = null; }
        try { location = doc.ActiveProjectLocation; } catch (Exception) { location = null; }
        if (site == null || location == null) problems.Add("the site of '" + doc.Title + "' could not be read");
    }

    // The turn is about the project base point; without one, the internal origin.
    var pivot = XYZ.Zero;
    var pivotName = "the internal origin (the project base point could not be read)";
    try
    {
        var projectBase = BasePoint.GetProjectBasePoint(doc);
        if (projectBase != null) { pivot = projectBase.Position; pivotName = "the project base point"; }
    }
    catch (Exception) { }

    // TRUE NORTH, MEASURED: Revit's shared position of two points along project
    // north, the second a hundred feet on. Degrees East of project north.
    Func<double?> trueNorthEast = () =>
    {
        try
        {
            var at = location.GetProjectPosition(pivot);
            var ahead = location.GetProjectPosition(pivot + XYZ.BasisY * 100.0);
            var east = ahead.EastWest - at.EastWest;
            var north = ahead.NorthSouth - at.NorthSouth;
            if (Math.Abs(east) + Math.Abs(north) < 1e-6) return null;
            var bearing = -Math.Atan2(east, north) * ToDegrees;
            if (bearing <= -180.0) bearing += 360.0;
            if (bearing > 180.0) bearing -= 360.0;
            return Math.Abs(bearing) < 1e-9 ? 0.0 : bearing;
        }
        catch (Exception) { return null; }
    };
    // Daylight saving, from the sun settings that use this project location.
    Func<bool?> daylightSaving = () =>
    {
        bool? any = null;
        try
        {
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(SunAndShadowSettings)))
            {
                var sun = element as SunAndShadowSettings;
                if (sun == null) continue;
                if (location != null && sun.ProjectLocationId == location.Id) return sun.UsesDST;
                if (any == null) any = sun.UsesDST;
            }
        }
        catch (Exception) { }
        return any;
    };
    Func<bool?, string> dstText = uses => uses.HasValue ? (uses.Value ? "used" : "not used") : "could not be read";

    double nowLat = 0, nowLon = 0, nowZone = 0;
    string nowPlace = "", nowStation = "";
    double? nowEast = null;
    bool? nowDst = null;
    if (problems.Count == 0 || site != null)
    {
        try
        {
            nowLat = site.Latitude * ToDegrees;
            nowLon = site.Longitude * ToDegrees;
            nowZone = site.TimeZone;
            nowPlace = site.PlaceName ?? "";
            try { nowStation = site.WeatherStationName ?? ""; } catch (Exception) { }
            nowEast = trueNorthEast();
            nowDst = daylightSaving();
        }
        catch (Exception) { }
    }

    // ---- what each value asks for -----------------------------------------------
    double? wantLat = null, wantLon = null, wantZone = null, wantEast = null;
    string wantPlace = null;
    City city = null;

    if (asked.ContainsKey("city") && site != null)
    {
        var said = asked["city"];
        var exact = new List<City>();
        var town = new List<City>();
        var near = new List<string>();
        var listRead = false;
        var listSize = 0;
        try
        {
            foreach (City one in doc.Application.Cities)
            {
                if (one == null) continue;
                listSize++;
                var name = one.Name ?? "";
                var comma = name.IndexOf(',');
                var first = comma < 0 ? name : name.Substring(0, comma);
                if (words(name) == words(said)) exact.Add(one);
                else if (words(first) == words(said)) town.Add(one);
                else if (words(name).Contains(words(said))) near.Add(name);
            }
            listRead = true;
        }
        catch (Exception failure) { problems.Add("Revit's city list could not be read: " + revitSaid(failure)); }
        var found = exact.Count > 0 ? exact : town;
        if (found.Count == 1) city = found[0];
        else if (found.Count > 1)
            problems.Add("Revit's city list has " + found.Count + " places called '" + said + "' - "
                + string.Join("; ", found.Select(c => c.Name).Take(12).ToArray()) + ". Name one in full");
        else if (listRead)
            problems.Add("Revit's city list (" + listSize + " places) has no '" + said + "'"
                + (near.Count > 0
                    ? " - names holding those words: " + string.Join("; ", near.Take(12).ToArray())
                        + (near.Count > 12 ? " and " + (near.Count - 12) + " more" : "")
                        + ". Name one of those, or give latitude and longitude instead"
                    : ". Give latitude and longitude instead"));
        if (city != null)
        {
            wantLat = city.Latitude * ToDegrees;
            wantLon = city.Longitude * ToDegrees;
            wantZone = city.TimeZone;
            wantPlace = city.Name;
        }
    }

    var hasLat = asked.ContainsKey("latitude");
    var hasLon = asked.ContainsKey("longitude");
    if (hasLat || hasLon)
    {
        if (asked.ContainsKey("city"))
            problems.Add("give a city or latitude and longitude, not both - a city brings its own");
        else if (!(hasLat && hasLon))
            problems.Add("latitude and longitude go together - give both, or a city");
        else
        {
            var lat = signedDegrees(asked["latitude"], "NS");
            var lon = signedDegrees(asked["longitude"], "EW");
            if (lat == null) problems.Add("latitude takes decimal degrees, North or South - '25.2854 N' or '-33.87', not '" + asked["latitude"] + "'");
            else if (Math.Abs(lat.Value) > 90.0) problems.Add("a latitude is at most 90 degrees North or South, not " + asked["latitude"]);
            if (lon == null) problems.Add("longitude takes decimal degrees, East or West - '51.5310 E' or '-0.1276', not '" + asked["longitude"] + "'");
            else if (Math.Abs(lon.Value) > 180.0) problems.Add("a longitude is at most 180 degrees East or West, not " + asked["longitude"]);
            if (lat != null && lon != null && Math.Abs(lat.Value) <= 90.0 && Math.Abs(lon.Value) <= 180.0)
            {
                wantLat = lat;
                wantLon = lon;
            }
        }
    }

    if (asked.ContainsKey("time zone"))
    {
        var hours = hoursOf(asked["time zone"]);
        if (hours == null) problems.Add("time zone takes hours from UTC - 'UTC+03:00', '+3' or '-5:30', not '" + asked["time zone"] + "'");
        else if (Math.Abs(hours.Value) > 12.0) problems.Add("Revit holds a time zone from UTC-12:00 to UTC+12:00, so '" + asked["time zone"] + "' cannot be set");
        else wantZone = hours;
    }

    if (asked.ContainsKey("place name")) wantPlace = asked["place name"];

    if (asked.ContainsKey("daylight saving"))
    {
        var wanted = yesOrNo(asked["daylight saving"]);
        if (wanted == null) problems.Add("daylight saving takes yes or no, not '" + asked["daylight saving"] + "'");
        else if (nowDst.HasValue && nowDst.Value == wanted.Value) alreadyThat = "daylight saving " + dstText(nowDst);
        else
            problems.Add("Revit's API can read daylight saving but not set it - tick or clear Use Daylight Savings time "
                + "in Manage > Location yourself. It is " + dstText(nowDst) + " now");
    }

    if (asked.ContainsKey("true north"))
    {
        var east = eastOf(asked["true north"]);
        if (east == null)
            problems.Add("true north takes an angle and East or West, the way the Location dialog's Site tab gives it - "
                + "'30 East' or '12.5 West', not '" + asked["true north"] + "'");
        else if (Math.Abs(east.Value) > 180.0) problems.Add("true north is at most 180 degrees East or West, not " + asked["true north"]);
        else if (nowEast == null && site != null) problems.Add("the angle to true north could not be read, so it is not turned");
        else wantEast = east;
    }

    // ---- the plan -------------------------------------------------------------
    var plan = new List<string>();
    var already = new List<string>();
    if (alreadyThat.Length > 0) already.Add(alreadyThat);
    var moveSite = false;
    var setZone = false;
    var setPlace = false;
    var turnNorth = false;
    if (problems.Count == 0)
    {
        if (wantLat.HasValue)
        {
            if (Math.Abs(wantLat.Value - nowLat) < 0.00005 && Math.Abs(wantLon.Value - nowLon) < 0.00005)
                already.Add("latitude " + latitudeText(nowLat) + ", longitude " + longitudeText(nowLon));
            else
            {
                moveSite = true;
                plan.Add("latitude " + latitudeText(nowLat) + " -> " + latitudeText(wantLat.Value)
                    + ", longitude " + longitudeText(nowLon) + " -> " + longitudeText(wantLon.Value));
            }
        }
        // WRITTEN WHENEVER THE SITE MOVES, even when it already matches:
        // Revit resets the time zone and the place name itself when the
        // coordinates change, so a value that matched before the move may
        // not after it. Read back either way.
        if (wantZone.HasValue)
        {
            setZone = moveSite;
            if (Math.Abs(wantZone.Value - nowZone) < 0.001) already.Add("time zone " + zoneText(nowZone));
            else { setZone = true; plan.Add("time zone " + zoneText(nowZone) + " -> " + zoneText(wantZone.Value)); }
        }
        if (wantPlace != null)
        {
            setPlace = moveSite;
            if (nowPlace == wantPlace) already.Add("place name '" + nowPlace + "'");
            else { setPlace = true; plan.Add("place name '" + nowPlace + "' -> '" + wantPlace + "'"); }
        }
        if (wantEast.HasValue)
        {
            if (Math.Abs(wantEast.Value - nowEast.Value) < 0.001) already.Add("true north " + northText(nowEast));
            else { turnNorth = true; plan.Add("true north " + northText(nowEast) + " -> " + northText(wantEast)); }
        }
    }

    // ---- the change -------------------------------------------------------------
    if (problems.Count > 0)
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was changed.";
    else if (plan.Count > 0)
    {
        var stored = false;
        var turnedAt = (ProjectPosition)null;
        try
        {
            if (moveSite)
            {
                site.Latitude = wantLat.Value / ToDegrees;
                stored = true;
                site.Longitude = wantLon.Value / ToDegrees;
            }
            if (setZone) { site.TimeZone = wantZone.Value; stored = true; }
            if (setPlace) { site.PlaceName = wantPlace; stored = true; }
            if (turnNorth)
            {
                // REVIT'S ANGLE RUNS WITH "EAST" - MEASURED, not assumed:
                // 2026-10-06, scratch Project2 on Revit 2024, an angle of -30
                // degrees written here read back from the two points as "30
                // West" and the run was rolled back by the check below. So a
                // positive angle is true north EAST of project north. It is
                // still checked on every run: a turn that reads back the
                // other way throws.
                var at = location.GetProjectPosition(pivot);
                turnedAt = new ProjectPosition(at.EastWest, at.NorthSouth, at.Elevation, wantEast.Value / ToDegrees);
                location.SetProjectPosition(pivot, turnedAt);
                stored = true;
            }
        }
        catch (Exception failure)
        {
            if (stored) throw;
            refused = "Revit refused the first change: " + revitSaid(failure) + ". It would have set "
                + string.Join("; ", plan.ToArray()) + ". Nothing was changed.";
        }

        if (refused.Length == 0)
        {
            // ---- read back, every value, and a wrong one undoes the lot ----
            var after = doc.SiteLocation;
            var backLat = after.Latitude * ToDegrees;
            var backLon = after.Longitude * ToDegrees;
            var backZone = after.TimeZone;
            var backPlace = after.PlaceName ?? "";
            var backEast = trueNorthEast();
            var wrong = new List<string>();
            var done = new List<string>();
            if (moveSite)
            {
                if (Math.Abs(backLat - wantLat.Value) > 0.00005 || Math.Abs(backLon - wantLon.Value) > 0.00005)
                    wrong.Add("the site reads back " + latitudeText(backLat) + ", " + longitudeText(backLon));
                else done.Add("latitude " + latitudeText(nowLat) + " -> " + latitudeText(backLat)
                    + ", longitude " + longitudeText(nowLon) + " -> " + longitudeText(backLon));
            }
            if (setZone)
            {
                if (Math.Abs(backZone - wantZone.Value) > 0.001) wrong.Add("the time zone reads back " + zoneText(backZone));
                else if (Math.Abs(backZone - nowZone) > 0.001) done.Add("time zone " + zoneText(nowZone) + " -> " + zoneText(backZone));
            }
            if (setPlace)
            {
                if (backPlace != wantPlace) wrong.Add("the place name reads back '" + backPlace + "'");
                else if (backPlace != nowPlace) done.Add("place name '" + nowPlace + "' -> '" + backPlace + "'");
            }
            if (turnNorth)
            {
                var stillAt = location.GetProjectPosition(pivot);
                var moved = Math.Sqrt(Math.Pow(stillAt.EastWest - turnedAt.EastWest, 2)
                    + Math.Pow(stillAt.NorthSouth - turnedAt.NorthSouth, 2)
                    + Math.Pow(stillAt.Elevation - turnedAt.Elevation, 2));
                if (backEast == null || Math.Abs(backEast.Value - wantEast.Value) > 0.01)
                    wrong.Add("true north reads back " + northText(backEast) + " - Revit's angle runs the other way "
                        + "from the one this tool writes, which is a defect in this tool");
                else if (moved > StillFeet)
                    wrong.Add("the shared position of " + pivotName + " moved by " + (moved * 304.8).ToString("0.#", invariant)
                        + " mm, where the turn was to be about it");
                else done.Add("true north " + northText(nowEast) + " -> " + northText(backEast) + ", turned about " + pivotName);
            }
            if (wrong.Count > 0)
                throw new InvalidOperationException("Revit did not keep what was asked: " + string.Join("; ", wrong.ToArray())
                    + ". Nothing is kept - the whole change is rolled back.");
            changed = string.Join("; ", done.ToArray());

            var notes = new List<string>();
            if (moveSite && !setPlace && backPlace == nowPlace)
                notes.Add("the place name still reads '" + backPlace + "', which names where the site WAS - Revit found no city "
                    + "of its own at the new coordinates; give place name to change it");
            // Measured 2026-10-06: Doha's coordinates alone made Revit name the
            // place 'DOHA INTL AIRPORT' itself - a name nobody asked for, said.
            else if (moveSite && !setPlace && backPlace != nowPlace)
                notes.Add("Revit named the place '" + backPlace + "' itself (it was '" + nowPlace + "'); give place name to "
                    + "call it something else");
            if (moveSite && !asked.ContainsKey("time zone") && city == null)
                notes.Add("Revit set the time zone to " + zoneText(backZone) + " from the new coordinates, and says that can be "
                    + "wrong near a boundary; give time zone if it is");
            string station = "";
            try { station = after.WeatherStationName ?? ""; } catch (Exception) { }
            // THE WEATHER STATION IS REVIT'S TO MOVE, AND IT MAY NOT. Its API
            // says it will try; measured 2026-10-06 on Project2 (Revit 2024),
            // Boston's station stayed after the site moved to Doha. The name
            // is get-only on every release, so it is said, never set.
            if (moveSite && station != nowStation)
                notes.Add("Revit moved the weather station from '" + nowStation + "' to '" + station + "'");
            else if (moveSite)
                notes.Add("Revit kept the weather station '" + station + "' from the old site - Revit's own energy "
                    + "analysis reads it; pick the nearest in Manage > Location, Weather tab. Heron cannot set it");
            if (notes.Count > 0) findings = string.Join("; ", notes.ToArray()) + ".  ||  ";
        }
    }

    // ---- what the model holds now, said every time ----------------------------
    if (site != null && location != null)
    {
        var now = doc.SiteLocation;
        string station = "";
        try { station = now.WeatherStationName ?? ""; } catch (Exception) { }
        double elevation = 0.0;
        try { elevation = now.Elevation; } catch (Exception) { }
        findings += "THE SITE NOW, READ BACK  ||  project location '" + location.Name + "' (the active one)"
            + "  ||  place name '" + (now.PlaceName ?? "") + "'"
            + "  ||  latitude " + latitudeText(now.Latitude * ToDegrees) + ", longitude " + longitudeText(now.Longitude * ToDegrees)
            + "  ||  time zone " + zoneText(now.TimeZone) + ", daylight saving " + dstText(daylightSaving())
            + "  ||  weather station '" + station + "', site elevation " + (elevation * 0.3048).ToString("0.###", invariant) + " m"
            + "  ||  true north " + northText(trueNorthEast()) + " of project north";
    }
    if (already.Count > 0) alreadyThat = string.Join("; ", already.ToArray());
}
