// NOT STANDALONE. Assumes `doc`, `shape`, `distribution`, `intensity`,
// `colorTemperature`, `typeName`, `lightLossFactor`, `dimming`, `colorFilter`,
// `position` and `aim` are in scope; leaves `lightReport`, `changed`,
// `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE LIGHT SOURCE OF A LIGHTING FIXTURE FAMILY, in two halves Revit keeps
// apart:
//
//   THE FAMILY'S - one for every type: the SHAPE light leaves (point, line,
//   rectangle, circle, and its size), how it is DISTRIBUTED (spherical,
//   hemispherical, spot with its angles, or a photometric web from an IES file,
//   with its tilt), and where the light source SITS and which way it is AIMED.
//
//   EACH TYPE'S - the initial INTENSITY (lumens, candelas, lux at a distance, or
//   a wattage with its efficacy), the initial COLOUR TEMPERATURE, the LIGHT LOSS
//   FACTOR, the DIMMING colour shift and the COLOUR FILTER.
//
// REVIT'S REFERENCE SAYS WHY THEY ARE KEPT APART (LightType.SetLightShape and
// SetLightDistribution, RevitAPI.xml 2020 to 2027): "Light shape is a family
// element parameter. All other LightType properties and methods except for
// light distribution are family type parameters. Changes to family type
// parameters may not be committed in the same transaction with changes to
// family element parameters, otherwise the modifications to the family type
// may be lost." Version 1 did both in one transaction, and on 2026-10-21 on
// Revit 2020 a call with a shape, a distribution and 4500 lm read its intensity
// back as the default wattage (FRAGMENT-ISSUES 5b-1294). Heron runs a fragment
// in ONE transaction, so a call takes ONE half - the other half is refused with
// that reason and goes in a second call.
//
// EVERY FAMILY TYPE, NOT ONLY THE ONES REVIT'S LIGHTING API LISTS (5b-1295).
// LightFamily listed one light type for a family with two family types on Revit
// 2020, and the forum reports the same on other releases - one short, the first
// missing. No member on any release 2020 to 2027 makes a light type. So the
// types are the FAMILY's (FamilyManager.Types), and each is made the current
// type before its values are written - the current type is the one a family
// type value is written to. A type the lighting API does not list is written
// through a type it does list, with it current, and then has to PROVE it was
// reached: its own Type Properties values are read for every value asked, and
// every other type is checked untouched. A type that cannot be proved reached
// fails the call, and the refusal says the hand fix.
//
// THE LIGHTING CLASSES ARE REACHED BY NAME at run time: they live in a namespace
// the fragment executor does not import, and every one used here reads the same
// 2020 through 2027. So are the unit ids, which moved from DisplayUnitType
// (2020-2021) to UnitTypeId (2021 on).
//
// UNITS ARE THE REFERENCE'S OWN WORDS: emit sizes in feet, spot angles in
// radians, a photometric web's tilt in DEGREES, an illuminance in lux at a
// distance in feet, a loss factor as a plain number. A WATTAGE (BV18): the
// reference's constructor calls the wattage and efficacy "universal unit"
// values, while its Wattage and Efficacy properties say W and lm/W. They are
// written as W and lm/W, and Type Properties' own Wattage and Efficacy are then
// converted out of Revit's internal units and must read what was asked - or
// the call fails and nothing is kept.
//
// THE LIGHT SOURCE IS MOVED AND AIMED AS AN ELEMENT. No release has a setter
// for LightFamily's light source transform; its remark says the transform
// follows the light source as the modeller moves or rotates it in the Family
// Editor. So the one element of the Light Source category is moved and rotated,
// and the transform is read again: a light source that did not go where it was
// sent fails the call.
//
// READ BACK, ALL OR NOTHING: everything asked is read again - through the
// lighting API, through what Type Properties holds for each type, and through
// the light source transform - and anything that does not read as asked
// THROWS, and the host rolls the call back.

var findings = new List<string>();
var lightReport = "";
var changed = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace + ".";
var lightingNamespace = dbNamespace + "Lighting.";
Func<string, System.Type> lightingType = name => revitAssembly.GetType(lightingNamespace + name);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<string, double?> number = text =>
{
    double value;
    var t = (text ?? "").Trim().ToLowerInvariant();
    foreach (var unit in new[] { "mm", "lm", "cd", "lx", "k", "°" })
        if (t.EndsWith(unit)) t = t.Substring(0, t.Length - unit.Length).Trim();
    if (!double.TryParse(t, System.Globalization.NumberStyles.Float, invariant, out value)) return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

// Revit's reason, not reflection's wrapper.
Func<Exception, string> reasonOf = ex =>
    ex is System.Reflection.TargetInvocationException && ex.InnerException != null ? ex.InnerException.Message : ex.Message;

Func<object, string, object[], object> call = (target, name, args) =>
{
    var kind = target as System.Type ?? target.GetType();
    var method = kind.GetMethods().FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
    if (method == null) throw new InvalidOperationException(kind.Name + "." + name + " is not in this release of Revit.");
    return method.Invoke(target is System.Type ? null : target, args);
};
Func<string, object[], object> make = (name, args) => Activator.CreateInstance(lightingType(name), args);
Func<object, string, double> number0f = (target, property) =>
    Convert.ToDouble(target.GetType().GetProperty(property).GetValue(target), invariant);
Action<object, string, object> setProperty = (target, property, value) =>
    target.GetType().GetProperty(property).SetValue(target, value);
Func<double, double, bool> near = (a, b) => Math.Abs(a - b) <= Math.Max(1e-6, Math.Abs(b) * 1e-4);

// The first number Type Properties shows - or null where its separators could
// be read two ways ("1,500" is fifteen hundred in one locale and one and a half
// in another), so an ambiguous display is never compared, only reported.
Func<string, double?> shown = text =>
{
    var m = System.Text.RegularExpressions.Regex.Match(text ?? "", @"[-+]?\d[\d.,]*");
    if (!m.Success) return null;
    var digits = m.Value.TrimEnd('.', ',');
    var lastDot = digits.LastIndexOf('.');
    var lastComma = digits.LastIndexOf(',');
    if (lastDot >= 0 && lastComma >= 0)
        digits = lastDot > lastComma ? digits.Replace(",", "") : digits.Replace(".", "").Replace(',', '.');
    else if (lastDot >= 0 || lastComma >= 0)
    {
        var mark = lastDot >= 0 ? '.' : ',';
        var marks = digits.Count(c => c == mark);
        var after = digits.Length - digits.LastIndexOf(mark) - 1;
        if (marks > 1) digits = digits.Replace(mark.ToString(), "");
        else if (after == 3) return null;
        else digits = digits.Replace(mark, '.');
    }
    double value;
    return double.TryParse(digits, System.Globalization.NumberStyles.Float, invariant, out value) ? value : (double?)null;
};

// A value Revit holds in its internal units, in the unit named - UnitTypeId
// from 2021, DisplayUnitType on 2020 (and 2021, where both exist). Null where
// neither can be found: the caller then says it could not compare.
Func<double, string, string, double?> fromInternal = (value, unitName, oldUnitName) =>
{
    try
    {
        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
        var unitIds = revitAssembly.GetType(dbNamespace + "UnitTypeId");
        var forgeType = revitAssembly.GetType(dbNamespace + "ForgeTypeId");
        if (unitIds != null && forgeType != null)
        {
            var idProperty = unitIds.GetProperty(unitName, flags);
            var convert = typeof(UnitUtils).GetMethod("ConvertFromInternalUnits", flags, null,
                new[] { typeof(double), forgeType }, null);
            if (idProperty != null && convert != null)
                return (double)convert.Invoke(null, new object[] { value, idProperty.GetValue(null) });
        }
        var oldUnits = revitAssembly.GetType(dbNamespace + "DisplayUnitType");
        if (oldUnits != null && Enum.IsDefined(oldUnits, oldUnitName))
        {
            var convert = typeof(UnitUtils).GetMethod("ConvertFromInternalUnits", flags, null,
                new[] { typeof(double), oldUnits }, null);
            if (convert != null)
                return (double)convert.Invoke(null, new object[] { value, Enum.Parse(oldUnits, oldUnitName) });
        }
    }
    catch (Exception) { }
    return null;
};

// ---------------------------------------------------------------------------
// WHAT WAS ASKED
// ---------------------------------------------------------------------------

var problems = new List<string>();
string shapeStyle = null;          // Point, Line, Rectangle, Circle
object[] shapeArgs = null;         // feet
string distributionStyle = null;   // Spherical, Hemispherical, Spot, PhotometricWeb
object[] distributionArgs = null;
string intensityKind = null;       // InitialFluxIntensity, InitialLuminousIntensity, InitialIlluminanceIntensity, InitialWattageIntensity
object[] intensityArgs = null;     // as the constructor takes them - a wattage's is (efficacy, wattage)
double? kelvin = null;
string lossKind = null;            // BasicLossFactor, AdvancedLossFactor
object[] lossArgs = null;
double lossTotal = 1.0;
string dimmingName = null;         // a LightDimmingColor name
Color filterColour = null;
XYZ moveTo = null;                 // feet, family coordinates
string moveWords = null;
var turns = new List<Tuple<XYZ, double, string>>();   // axis, radians, as said
object light = null;
var fm = doc.IsFamilyDocument ? doc.FamilyManager : null;
var familyTypes = new List<FamilyType>();
var targets = new List<FamilyType>();
var lightNames = new List<string>();
Element source = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A light source is defined inside the lighting fixture family - open it first.";
}
else if (lightingType("LightFamily") == null)
{
    refused = "Revit's lighting classes could not be found in this release - nothing was changed.";
}
else
{
    try { light = call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc }); }
    catch (Exception ex)
    {
        refused = "This family has no light source to define (" + reasonOf(ex) + "). Light Source is not ticked in "
            + "Family Category and Parameters - SET_FAMILY_SETTINGS \"Light Source=yes\" ticks it (a Lighting Fixtures "
            + "family), then run this again. Nothing was changed.";
    }
}

if (refused == null && !notAFamily)
{
    var s = (shape ?? "").Trim().ToLowerInvariant();
    if (s.Length > 0)
    {
        var words = System.Text.RegularExpressions.Regex.Split(s, @"[\s×]+|(?<=\d)x(?=\d)|\sx\s")
            .Where(w => w.Length > 0 && w != "x" && w != "by").ToList();
        var sizes = words.Skip(1).Select(w => number(w)).ToList();
        var mmToFeet = 1 / 304.8;
        if (words[0] == "point" && sizes.Count == 0) { shapeStyle = "Point"; shapeArgs = new object[0]; }
        else if (words[0] == "line" && sizes.Count == 1 && sizes[0] > 0)
        { shapeStyle = "Line"; shapeArgs = new object[] { sizes[0].Value * mmToFeet }; }
        else if (words[0] == "rectangle" && sizes.Count == 2 && sizes.All(v => v > 0))
        { shapeStyle = "Rectangle"; shapeArgs = new object[] { sizes[0].Value * mmToFeet, sizes[1].Value * mmToFeet }; }
        else if (words[0] == "circle" && sizes.Count == 1 && sizes[0] > 0)
        { shapeStyle = "Circle"; shapeArgs = new object[] { sizes[0].Value * mmToFeet }; }
        else problems.Add("\"" + shape.Trim() + "\" is not a light shape - \"point\", \"line 1200\", \"rectangle "
            + "1200 x 600\" (length by width) or \"circle 200\", sizes in millimetres.");
    }

    var d = (distribution ?? "").Trim();
    if (d.Length > 0)
    {
        var lower = d.ToLowerInvariant();
        var tiltAt = lower.LastIndexOf(" tilt ", StringComparison.Ordinal);
        var tilt = tiltAt < 0 ? 0.0 : number(d.Substring(tiltAt + 6));
        var head = tiltAt < 0 ? d : d.Substring(0, tiltAt);
        if (tiltAt >= 0 && (!tilt.HasValue || tilt < -180 || tilt > 180))
            problems.Add("The tilt in \"" + d + "\" must be a number of degrees from -180 to 180.");
        else if (squash(head) == "spherical") { distributionStyle = "Spherical"; distributionArgs = new object[0]; }
        else if (squash(head) == "hemispherical") { distributionStyle = "Hemispherical"; distributionArgs = new object[0]; }
        else if (lower.StartsWith("spot"))
        {
            var angles = head.Substring(4).Split(new[] { ' ', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => number(w)).ToList();
            if (angles.Count != 2 || angles.Any(a => !a.HasValue || a < 0.01 || a > 160))
                problems.Add("\"" + d + "\" needs the beam and field angles in degrees, each from 0.01 to 160 - \"spot "
                    + "30 60\", \"spot 30 60 tilt 10\".");
            else
            {
                distributionStyle = "Spot";
                distributionArgs = new object[] { angles[0].Value * Math.PI / 180, angles[1].Value * Math.PI / 180,
                    tilt.Value * Math.PI / 180 };
            }
        }
        else if (lower.StartsWith("photometric web") || lower.StartsWith("web"))
        {
            var path = head.Substring(lower.StartsWith("web") ? 3 : 15).Trim().Trim('"');
            if (path.Length == 0 || !path.EndsWith(".ies", StringComparison.OrdinalIgnoreCase))
                problems.Add("\"" + d + "\" needs the IES file - \"web C:\\Lights\\downlight.ies\".");
            else if (!System.IO.File.Exists(path))
                problems.Add("No IES file is at \"" + path + "\" on this computer.");
            else { distributionStyle = "PhotometricWeb"; distributionArgs = new object[] { path, tilt.Value }; }
        }
        else problems.Add("\"" + d + "\" is not a light distribution - \"spherical\", \"hemispherical\", \"spot 30 60\" "
            + "or \"web <the .ies file>\".");
    }

    // THE INTENSITY: each number with its unit, and nothing else left over.
    var i = (intensity ?? "").Trim().ToLowerInvariant();
    if (i.Length > 0)
    {
        var pairs = System.Text.RegularExpressions.Regex.Matches(i,
                @"([-+]?\d+(?:\.\d+)?)\s*(lm\s*/\s*w|lm|cd|lx|w|mm)(?![a-z])")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => Tuple.Create(double.Parse(m.Groups[1].Value, invariant), m.Groups[2].Value.Replace(" ", "")))
            .ToList();
        var leftOver = System.Text.RegularExpressions.Regex.Replace(i,
            @"([-+]?\d+(?:\.\d+)?)\s*(lm\s*/\s*w|lm|cd|lx|w|mm)(?![a-z])|\bat\b|\band\b|\bwith\b|@|,|\s", "");
        Func<string, Tuple<double, string>> one = unit => pairs.FirstOrDefault(p => p.Item2 == unit);
        var units = pairs.Select(p => p.Item2).ToList();
        if (leftOver.Length > 0 || pairs.Count == 0 || units.Distinct().Count() != units.Count || pairs.Any(p => p.Item1 < 0))
            problems.Add("\"" + intensity.Trim() + "\" is not an intensity - \"1500 lm\", \"1200 cd\", \"500 lx at 1000 "
                + "mm\", or a wattage with its efficacy or its lumens: \"35.5 W at 127 lm/W\", \"4500 lm at 35.5 W\".");
        else if (one("w") != null)
        {
            var watts = one("w").Item1;
            var efficacy = one("lm/w") != null ? one("lm/w").Item1
                : one("lm") != null && watts > 0 ? one("lm").Item1 / watts : (double?)null;
            if (units.Any(u => u == "cd" || u == "lx" || u == "mm") || (one("lm/w") != null && one("lm") != null))
                problems.Add("\"" + intensity.Trim() + "\" mixes a wattage with another kind of intensity - give \"35.5 W "
                    + "at 127 lm/W\" or \"4500 lm at 35.5 W\".");
            else if (!efficacy.HasValue)
                problems.Add("\"" + intensity.Trim() + "\" is a wattage with nothing to say how much light it gives - "
                    + "Revit's wattage intensity is the wattage AND its efficacy. Give \"35.5 W at 127 lm/W\", or the "
                    + "lumens from the datasheet with it - \"4500 lm at 35.5 W\" - or the lumens alone.");
            else if (watts <= 0 || efficacy <= 0 || efficacy >= 1e10)
                problems.Add("\"" + intensity.Trim() + "\" needs a wattage above 0 and an efficacy above 0 lm/W.");
            else { intensityKind = "InitialWattageIntensity"; intensityArgs = new object[] { efficacy.Value, watts }; }
        }
        else if (units.Count == 1 && units[0] == "lm") { intensityKind = "InitialFluxIntensity"; intensityArgs = new object[] { one("lm").Item1 }; }
        else if (units.Count == 1 && units[0] == "cd") { intensityKind = "InitialLuminousIntensity"; intensityArgs = new object[] { one("cd").Item1 }; }
        else if (units.Count == 2 && one("lx") != null && one("mm") != null && i.Contains(" at "))
        {
            if (one("mm").Item1 <= 0)
                problems.Add("\"" + intensity.Trim() + "\" needs the distance in millimetres - \"500 lx at 1000 mm\".");
            else
            {
                intensityKind = "InitialIlluminanceIntensity";
                intensityArgs = new object[] { one("mm").Item1 / 304.8, one("lx").Item1 };
            }
        }
        else problems.Add("\"" + intensity.Trim() + "\" does not say its unit the way Revit takes it - lm, cd, lx at a "
            + "distance in mm, or W with its lm/W or its lm.");
    }

    var k = (colorTemperature ?? "").Trim();
    if (k.Length > 0)
    {
        kelvin = number(k);
        if (!kelvin.HasValue || kelvin < 1800 || kelvin > 20000)
        {
            problems.Add("\"" + k + "\" is not a colour temperature Revit takes - kelvin from 1800 to 20000, \"4000 K\".");
            kelvin = null;
        }
    }

    // THE LIGHT LOSS FACTOR: one number from 0 to 4, or "advanced" and the
    // seven factors in the order Revit's constructor takes them.
    var lf = (lightLossFactor ?? "").Trim().ToLowerInvariant();
    if (lf.Length > 0)
    {
        var parts = lf.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var advanced = parts.Count > 0 && parts[0] == "advanced";
        var values = parts.Skip(advanced ? 1 : 0).Select(p => number(p)).ToList();
        var highest = new[] { 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 2.0 };
        if (!advanced && values.Count == 1 && values[0].HasValue && values[0] >= 0 && values[0] <= 4)
        { lossKind = "BasicLossFactor"; lossArgs = new object[] { values[0].Value }; lossTotal = values[0].Value; }
        else if (advanced && values.Count == 7 && values.Select((v, n) => v.HasValue && v >= 0 && v <= highest[n]).All(ok => ok))
        {
            lossKind = "AdvancedLossFactor";
            lossArgs = values.Select(v => (object)v.Value).ToArray();
            lossTotal = values.Aggregate(1.0, (product, v) => product * v.Value);
        }
        else problems.Add("\"" + lightLossFactor.Trim() + "\" is not a light loss factor - one number from 0 to 4, "
            + "\"0.8\", or \"advanced\" and seven: ballast, lamp lumen depreciation, lamp tilt, luminaire dirt, "
            + "surface depreciation (each 0 to 1), temperature and voltage (each 0 to 2).");
    }

    var dm = squash(dimming);
    if (dm.Length > 0)
    {
        var names = lightingType("LightDimmingColor") == null ? new string[0] : Enum.GetNames(lightingType("LightDimmingColor"));
        dimmingName = names.FirstOrDefault(n => squash(n) == dm || (dm == "off" && n == "None"));
        if (dimmingName == null)
            problems.Add("\"" + dimming.Trim() + "\" is not a dimming colour shift Revit has - it has "
                + string.Join(", ", names) + ".");
    }

    var cf = (colorFilter ?? "").Trim();
    if (cf.Length > 0)
    {
        var rgb = new List<int>();
        var hex = cf.TrimStart('#');
        if (squash(cf) == "white") rgb = new List<int> { 255, 255, 255 };
        else if (cf.StartsWith("#") && hex.Length == 6 && hex.All(Uri.IsHexDigit))
            rgb = Enumerable.Range(0, 3).Select(n => Convert.ToInt32(hex.Substring(n * 2, 2), 16)).ToList();
        else
        {
            var parts = cf.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => number(p)).ToList();
            if (parts.Count == 3 && parts.All(p => p.HasValue && p >= 0 && p <= 255 && p == Math.Floor(p.Value)))
                rgb = parts.Select(p => (int)p.Value).ToList();
        }
        if (rgb.Count == 3) filterColour = new Color((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
        else problems.Add("\"" + cf + "\" is not a colour filter - red, green and blue from 0 to 255, \"255 240 200\", "
            + "or \"#FFF0C8\", or \"white\" for none.");
    }

    // WHERE THE LIGHT SOURCE GOES: a point in millimetres, family coordinates -
    // "x 0 y 0 z 2400", any of the three, or "0 0 2400" - or onto a reference
    // plane by name, "plane Bottom of Fixture offset -10", the offset along the
    // plane's normal turned to point up or along +X/+Y.
    var p0 = (position ?? "").Trim();
    if (p0.Length > 0)
    {
        var lowerP = p0.ToLowerInvariant();
        if (lowerP.StartsWith("plane "))
        {
            var rest = p0.Substring(6).Trim();
            var offsetAt = rest.ToLowerInvariant().LastIndexOf(" offset ", StringComparison.Ordinal);
            var offset = offsetAt < 0 ? 0.0 : number(rest.Substring(offsetAt + 8));
            var planeName = (offsetAt < 0 ? rest : rest.Substring(0, offsetAt)).Trim().Trim('"');
            var planes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
            var named = planes.Where(rp => string.Equals(rp.Name, planeName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!offset.HasValue) problems.Add("The offset in \"" + p0 + "\" must be a number of millimetres.");
            else if (named.Count != 1)
                problems.Add((named.Count == 0 ? "No reference plane is called \"" : "Two reference planes are called \"")
                    + planeName + "\" - the named ones here: " + string.Join(", ", planes.Select(rp => rp.Name)
                        .Where(n => !string.IsNullOrEmpty(n)).Distinct().Take(20)) + ".");
            else
            {
                var plane = named[0].GetPlane();
                var normal = plane.Normal.Normalize();
                var big = Math.Abs(normal.Z) > 0.5 ? normal.Z : Math.Abs(normal.X) >= Math.Abs(normal.Y) ? normal.X : normal.Y;
                if (big < 0) normal = normal.Negate();
                var at = ((Transform)call(light, "GetLightSourceTransform", new object[0])).Origin;
                var onPlane = at - normal.Multiply((at - plane.Origin).DotProduct(normal));
                moveTo = onPlane + normal.Multiply(offset.Value / 304.8);
                moveWords = "onto reference plane \"" + named[0].Name + "\"" + (offset.Value == 0 ? "" : ", " + plain(offset.Value) + " mm off it");
            }
        }
        else
        {
            var at = ((Transform)call(light, "GetLightSourceTransform", new object[0])).Origin;
            var named = System.Text.RegularExpressions.Regex.Matches(lowerP, @"([xyz])\s*=?\s*([-+]?\d+(?:\.\d+)?)")
                .Cast<System.Text.RegularExpressions.Match>().ToList();
            var bare = lowerP.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(w => number(w)).ToList();
            double? x = at.X * 304.8, y = at.Y * 304.8, z = at.Z * 304.8;
            var ok = false;
            if (named.Count > 0 && named.Select(m => m.Groups[1].Value).Distinct().Count() == named.Count
                && System.Text.RegularExpressions.Regex.Replace(lowerP, @"([xyz])\s*=?\s*([-+]?\d+(?:\.\d+)?)|mm|,|\s", "").Length == 0)
            {
                foreach (var m in named)
                {
                    var v = double.Parse(m.Groups[2].Value, invariant);
                    if (m.Groups[1].Value == "x") x = v; else if (m.Groups[1].Value == "y") y = v; else z = v;
                }
                ok = true;
            }
            else if (bare.Count == 3 && bare.All(v => v.HasValue)) { x = bare[0]; y = bare[1]; z = bare[2]; ok = true; }
            if (!ok) problems.Add("\"" + p0 + "\" is not a place for the light source - \"x 0 y 0 z 2400\" (any of the "
                + "three, millimetres, the family's own coordinates), \"0 0 2400\", or \"plane <reference plane name>\" with "
                + "\"offset -10\".");
            else
            {
                moveTo = new XYZ(x.Value / 304.8, y.Value / 304.8, z.Value / 304.8);
                moveWords = "to X " + plain(x.Value) + ", Y " + plain(y.Value) + ", Z " + plain(z.Value) + " mm";
            }
        }
    }

    // WHICH WAY IT POINTS: "tilt 15 about x", "rotate 90 about z", several
    // with semicolons between, in order - degrees, turned about an axis through
    // the light source parallel to the family's X, Y or Z.
    var a0 = (aim ?? "").Trim();
    if (a0.Length > 0)
        foreach (var piece in a0.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var m = System.Text.RegularExpressions.Regex.Match(piece.Trim().ToLowerInvariant(),
                @"^(tilt|rotate|turn)\s+([-+]?\d+(?:\.\d+)?)\s*(°|deg|degrees)?\s+(about|around)\s+([xyz])$");
            if (!m.Success || Math.Abs(double.Parse(m.Groups[2].Value, invariant)) > 360)
            {
                problems.Add("\"" + piece.Trim() + "\" is not a way to aim the light source - \"tilt 15 about x\", "
                    + "\"rotate 90 about z\", degrees up to 360, semicolons between.");
                continue;
            }
            var axisName = m.Groups[5].Value;
            var axis = axisName == "x" ? XYZ.BasisX : axisName == "y" ? XYZ.BasisY : XYZ.BasisZ;
            turns.Add(Tuple.Create(axis, double.Parse(m.Groups[2].Value, invariant) * Math.PI / 180,
                m.Groups[1].Value + " " + m.Groups[2].Value + "° about " + axisName.ToUpperInvariant()));
        }

    var familyWide = shapeStyle != null || distributionStyle != null || moveTo != null || turns.Count > 0;
    var perType = intensityKind != null || kelvin.HasValue || lossKind != null || dimmingName != null || filterColour != null;

    if (!familyWide && !perType && problems.Count == 0)
        problems.Add("Nothing was asked - a shape, a distribution, a position or an aim for the light source, or an "
            + "intensity, a colour temperature, a light loss factor, a dimming colour shift or a colour filter for "
            + "its types.");
    if (familyWide && perType)
        problems.Add("A light's shape, distribution, position and aim are the family's light source, and its intensity, "
            + "colour, loss factor, dimming and filter are each type's; Revit's reference says the second may be lost "
            + "when both change in one transaction - measured on Revit 2020, where a shape, a distribution and 4500 lm "
            + "together read back the default wattage. Ask for the shape, distribution, position and aim first, and the "
            + "type values in a second call.");

    // THE TYPES ARE THE FAMILY'S: every one, or the one named.
    foreach (FamilyType t in fm.Types)
        if (t != null && !string.IsNullOrWhiteSpace(t.Name)) familyTypes.Add(t);
    var wanted = (typeName ?? "").Trim();
    targets = familyTypes.Where(t => wanted.Length == 0 || string.Equals(t.Name.Trim(), wanted, StringComparison.OrdinalIgnoreCase)).ToList();
    var count = (int)call(light, "GetNumberOfLightTypes", new object[0]);
    for (var n = 0; n < count; n++) lightNames.Add((string)call(light, "GetLightTypeName", new object[] { n }));

    if (familyTypes.Count == 0) problems.Add("This family has no types yet - SET_FAMILY_TYPE_VALUES makes the first one.");
    else if (targets.Count == 0)
        problems.Add("No type is called \"" + wanted + "\" - this family's types: "
            + string.Join(", ", familyTypes.Select(t => t.Name)) + ".");
    else if (count == 0)
        problems.Add("Revit's lighting API lists no light type at all in this family, so there is nothing to write the "
            + "light through. In Family Types, set Initial Intensity and Initial Color for each type by hand.");

    // THE FAMILY'S HALF, FOR EVERY TYPE: a type named alone cannot have it.
    if (familyWide && targets.Count > 0 && targets.Count < familyTypes.Count)
        problems.Add("A light's shape, distribution, position and aim belong to the family's light source - Revit's "
            + "reference calls the shape and distribution \"a family element parameter\" - so every type takes them and "
            + "they cannot change for \"" + wanted + "\" alone. Leave the type empty to change them for all "
            + familyTypes.Count + " types. To vary one by type, link it to a family type parameter - LINK_FAMILY_PARAMETER "
            + "naming the Light Source element, or the button beside it in Properties with the light source selected - "
            + "then set that parameter per type with SET_FAMILY_TYPE_VALUES.");

    // A TYPE THE LIGHTING API DOES NOT LIST is reached only where Type
    // Properties holds a value to prove it by: the intensity, the colour and the
    // loss factor do; the dimming shift and the filter have no value Heron can
    // read for one type, so they are refused for such a type.
    var unlisted = targets.Where(t => !lightNames.Any(n => string.Equals(n, t.Name, StringComparison.OrdinalIgnoreCase))).ToList();
    if (perType && count > 0 && unlisted.Count > 0 && (dimmingName != null || filterColour != null))
        problems.Add("Revit's lighting API does not list " + string.Join(", ", unlisted.Select(t => "\"" + t.Name + "\""))
            + " among this family's light types (it lists " + string.Join(", ", lightNames) + "), and no call on any "
            + "release makes one. Its intensity, colour and loss factor can still be set and proved from its Type "
            + "Properties, but a dimming colour shift or a colour filter cannot be proved for it - set those by hand in "
            + "Family Types with that type chosen, or name a type the API lists.");

    if (moveTo != null || turns.Count > 0)
    {
        var sources = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_LightingFixtureSource)
            .WhereElementIsNotElementType().ToElements();
        if (sources.Count == 1) source = sources[0];
        else problems.Add("Revit gives the API " + sources.Count + " elements of the Light Source category in this family, "
            + "not one, so Heron cannot tell which to move. Move or rotate the light source by hand in the Family "
            + "Editor; this reports where it sits.");
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// THE FAMILY'S HALF: SET, THEN READ BACK
// ---------------------------------------------------------------------------

Func<string, BuiltInParameter, string> showing = (name, bip) =>
{
    var type = familyTypes.FirstOrDefault(ft => ft.Name == name);
    var p = fm == null ? null : fm.get_Parameter(bip);
    if (type == null || p == null || !type.HasValue(p)) return null;
    return type.AsValueString(p);
};
var rows = new List<string>();
Func<object, string> shapeSaid = lightShape =>
{
    var shapeName = lightShape.GetType().Name.Replace("LightShape", "");
    return shapeName.ToLowerInvariant()
        + (shapeName == "Line" ? " " + plain(number0f(lightShape, "EmitLength") * 304.8) + " mm"
        : shapeName == "Rectangle" ? " " + plain(number0f(lightShape, "EmitLength") * 304.8) + " x "
            + plain(number0f(lightShape, "EmitWidth") * 304.8) + " mm"
        : shapeName == "Circle" ? " " + plain(number0f(lightShape, "EmitDiameter") * 304.8) + " mm" : "");
};
Func<object, string> spreadSaid = spread =>
{
    var spreadName = spread.GetType().Name.Replace("LightDistribution", "");
    return spreadName == "Spot"
        ? "spot, beam " + plain(number0f(spread, "SpotBeamAngle") * 180 / Math.PI) + "°, field "
            + plain(number0f(spread, "SpotFieldAngle") * 180 / Math.PI) + "°, tilt " + plain(number0f(spread, "TiltAngle") * 180 / Math.PI) + "°"
        : spreadName == "PhotometricWeb"
            ? "photometric web " + (string)spread.GetType().GetProperty("PhotometricWebFile").GetValue(spread)
                + ", tilt " + plain(number0f(spread, "TiltAngle")) + "°"
            : spreadName.ToLowerInvariant();
};

if (refused == null && (shapeStyle != null || distributionStyle != null || moveTo != null || turns.Count > 0))
{
    Transform expected = null;
    try
    {
        // The styles first: they are the family's, and the shape and
        // distribution must then be of that style. One light type carries the
        // family's light source; every listed one is read back below.
        if (shapeStyle != null)
            call(light, "SetLightShapeStyle", new[] { Enum.Parse(lightingType("LightShapeStyle"), shapeStyle) });
        if (distributionStyle != null)
            call(light, "SetLightDistributionStyle", new[] { Enum.Parse(lightingType("LightDistributionStyle"), distributionStyle) });
        var carrier = call(light, "GetLightType", new object[] { 0 });
        if (shapeStyle != null) call(carrier, "SetLightShape", new[] { make(shapeStyle + "LightShape", shapeArgs) });
        if (distributionStyle != null)
            call(carrier, "SetLightDistribution", new[] { make(distributionStyle + "LightDistribution", distributionArgs) });
        doc.Regenerate();

        if (moveTo != null || turns.Count > 0)
        {
            var now = (Transform)call(light, "GetLightSourceTransform", new object[0]);
            expected = now;
            if (moveTo != null)
            {
                var shift = moveTo - now.Origin;
                if (shift.GetLength() > 1e-9) ElementTransformUtils.MoveElement(doc, source.Id, shift);
                expected = Transform.CreateTranslation(shift).Multiply(now);
                doc.Regenerate();
            }
            foreach (var turn in turns)
            {
                var pivot = ((Transform)call(call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc }),
                    "GetLightSourceTransform", new object[0])).Origin;
                ElementTransformUtils.RotateElement(doc, source.Id, Line.CreateBound(pivot, pivot + turn.Item1), turn.Item2);
                expected = Transform.CreateRotationAtPoint(turn.Item1, turn.Item2, pivot).Multiply(expected);
                doc.Regenerate();
            }
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the light source: " + reasonOf(ex) + " The call failed, and "
            + "Heron rolls the whole call back.");
    }

    var again = call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc });
    if (shapeStyle != null && call(again, "GetLightShapeStyle", new object[0]).ToString() != shapeStyle)
        throw new InvalidOperationException("The light shape reads " + call(again, "GetLightShapeStyle", new object[0])
            + ", not " + shapeStyle + ". The call failed, and Heron rolls the whole call back.");
    if (distributionStyle != null && call(again, "GetLightDistributionStyle", new object[0]).ToString() != distributionStyle)
        throw new InvalidOperationException("The light distribution reads " + call(again, "GetLightDistributionStyle",
            new object[0]) + ", not " + distributionStyle + ". The call failed, and Heron rolls the whole call back.");

    // EVERY LISTED LIGHT TYPE MUST READ THE FAMILY'S SHAPE AND DISTRIBUTION -
    // one that does not says the reference's "family element parameter" is
    // wrong here, and the call fails rather than leave the types apart.
    var listedNow = (int)call(again, "GetNumberOfLightTypes", new object[0]);
    for (var n = 0; n < listedNow; n++)
    {
        var name = (string)call(again, "GetLightTypeName", new object[] { n });
        var data = call(again, "GetLightType", new object[] { n });
        Action<bool, string> must = (ok, what) =>
        {
            if (!ok) throw new InvalidOperationException("Light type \"" + name + "\" " + what + " The call failed, and "
                + "Heron rolls the whole call back.");
        };
        var lightShape = call(data, "GetLightShape", new object[0]);
        var shapeName = lightShape.GetType().Name.Replace("LightShape", "");
        if (shapeStyle != null)
        {
            must(shapeName == shapeStyle, "reads a " + shapeName + " light shape, not a " + shapeStyle + ".");
            if (shapeStyle == "Line") must(near(number0f(lightShape, "EmitLength"), (double)shapeArgs[0]), "emits from a line of another length.");
            if (shapeStyle == "Rectangle")
                must(near(number0f(lightShape, "EmitLength"), (double)shapeArgs[0]) && near(number0f(lightShape, "EmitWidth"), (double)shapeArgs[1]),
                    "emits from a rectangle of another size.");
            if (shapeStyle == "Circle") must(near(number0f(lightShape, "EmitDiameter"), (double)shapeArgs[0]), "emits from a circle of another size.");
        }
        var spread = call(data, "GetLightDistribution", new object[0]);
        var spreadName = spread.GetType().Name.Replace("LightDistribution", "");
        if (distributionStyle != null)
        {
            must(spreadName == distributionStyle, "reads a " + spreadName + " distribution, not " + distributionStyle + ".");
            if (distributionStyle == "Spot")
                must(near(number0f(spread, "SpotBeamAngle"), (double)distributionArgs[0])
                    && near(number0f(spread, "SpotFieldAngle"), (double)distributionArgs[1])
                    && near(number0f(spread, "TiltAngle"), (double)distributionArgs[2]), "reads other spot angles.");
            if (distributionStyle == "PhotometricWeb")
            {
                must(string.Equals((string)spread.GetType().GetProperty("PhotometricWebFile").GetValue(spread),
                    (string)distributionArgs[0], StringComparison.OrdinalIgnoreCase), "reads another IES file.");
                // A web's tilt is read back too - in DEGREES, the reference's
                // unit for this one, where a spot's is in radians.
                must(near(number0f(spread, "TiltAngle"), (double)distributionArgs[1]), "reads another tilt.");
            }
        }
        if (n == 0) rows.Add("the family's light source: " + shapeSaid(lightShape) + ", " + spreadSaid(spread));
    }

    // RADIANS ARE THE REFERENCE'S WORD; TYPE PROPERTIES IS THE TEST - for every
    // family type that shows a Spot Beam Angle.
    if (distributionStyle == "Spot")
        foreach (var t in familyTypes)
        {
            var beamShown = showing(t.Name, BuiltInParameter.FBX_LIGHT_SPOT_BEAM_ANGLE);
            var beam = shown(beamShown);
            if (beam.HasValue && beamShown.Contains("°") && Math.Abs(beam.Value - (double)distributionArgs[0] * 180 / Math.PI) >= 0.5)
                throw new InvalidOperationException("Type \"" + t.Name + "\" shows a Spot Beam Angle of " + beamShown
                    + " for the " + plain((double)distributionArgs[0] * 180 / Math.PI) + "° asked - the angle's unit is not "
                    + "the one Revit's reference names. The call failed, and Heron rolls the whole call back.");
        }

    // THE LIGHT SOURCE WENT WHERE IT WAS SENT, AND POINTS THE WAY IT WAS TURNED.
    if (expected != null)
    {
        var after = (Transform)call(again, "GetLightSourceTransform", new object[0]);
        var tolerance = 0.5 / 304.8;
        if (!after.Origin.IsAlmostEqualTo(expected.Origin, tolerance)
            || !after.BasisX.IsAlmostEqualTo(expected.BasisX, 1e-6)
            || !after.BasisY.IsAlmostEqualTo(expected.BasisY, 1e-6)
            || !after.BasisZ.IsAlmostEqualTo(expected.BasisZ, 1e-6))
            throw new InvalidOperationException("The light source did not go where it was sent: it sits at X "
                + plain(after.Origin.X * 304.8) + ", Y " + plain(after.Origin.Y * 304.8) + ", Z " + plain(after.Origin.Z * 304.8)
                + " mm, not X " + plain(expected.Origin.X * 304.8) + ", Y " + plain(expected.Origin.Y * 304.8) + ", Z "
                + plain(expected.Origin.Z * 304.8) + " mm" + (turns.Count > 0 ? ", or points another way" : "")
                + ". It may be locked to a reference plane - unlock it, or move it by hand. The call failed, and Heron "
                + "rolls the whole call back.");
        if (moveTo != null) changed.Add("Light source moved " + moveWords + ".");
        foreach (var turn in turns) changed.Add("Light source turned " + turn.Item3 + ".");
    }

    if (shapeStyle != null) changed.Add("Light shape set to " + shapeStyle.ToLowerInvariant() + " for every type.");
    if (distributionStyle != null) changed.Add("Light distribution set to " + (distributionStyle == "PhotometricWeb" ? "photometric web" : distributionStyle.ToLowerInvariant()) + " for every type.");
}

// ---------------------------------------------------------------------------
// EACH TYPE'S HALF: EACH TYPE MADE CURRENT, WRITTEN, THEN PROVED
// ---------------------------------------------------------------------------

// What Type Properties holds for each light value, for proving a type reached
// and every other type untouched.
var typeRows = new[]
{
    BuiltInParameter.FBX_LIGHT_INITIAL_INTENSITY, BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX,
    BuiltInParameter.FBX_LIGHT_LIMUNOUS_INTENSITY, BuiltInParameter.FBX_LIGHT_ILLUMINANCE,
    BuiltInParameter.FBX_LIGHT_AT_A_DISTANCE, BuiltInParameter.FBX_LIGHT_WATTAGE, BuiltInParameter.FBX_LIGHT_EFFICACY,
    BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_CTRL, BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_TEMPERATURE,
    BuiltInParameter.FBX_LIGHT_LOSS_FACTOR_CTRL, BuiltInParameter.FBX_LIGHT_TOTAL_LIGHT_LOSS,
    BuiltInParameter.FBX_LIGHT_DIMMING_LIGHT_COLOR, BuiltInParameter.FBX_LIGHT_COLOR_FILTER,
};
Func<FamilyType, BuiltInParameter, string> heldText = (t, bip) =>
{
    try
    {
        var p = fm.get_Parameter(bip);
        if (p == null || !t.HasValue(p)) return null;
        var text = t.AsValueString(p);
        if (text != null) return text;
        if (p.StorageType == StorageType.Double) return Convert.ToString(t.AsDouble(p), invariant);
        if (p.StorageType == StorageType.Integer) return Convert.ToString(t.AsInteger(p), invariant);
        if (p.StorageType == StorageType.String) return t.AsString(p) ?? "";
    }
    catch (Exception) { return "unreadable"; }
    return null;
};
Func<FamilyType, string> snapshot = t => string.Join(" | ", typeRows.Select(bip => heldText(t, bip) ?? "-"));
// Every light value one type's Type Properties holds, in Revit's own words.
Func<FamilyType, string> typeProperties = t => string.Join(", ", typeRows
    .Select(bip =>
    {
        var text = heldText(t, bip);
        if (string.IsNullOrEmpty(text)) return null;
        string label;
        try { label = LabelUtils.GetLabelFor(bip); } catch (Exception) { label = bip.ToString(); }
        return label + " " + text;
    })
    .Where(x => x != null));
// One type's own value, in the unit named; null where it holds none.
Func<FamilyType, BuiltInParameter, string, string, double?> heldIn = (t, bip, unitName, oldUnitName) =>
{
    var p = fm.get_Parameter(bip);
    if (p == null || p.StorageType != StorageType.Double || !t.HasValue(p) || !t.AsDouble(p).HasValue) return null;
    return unitName == null ? t.AsDouble(p) : fromInternal(t.AsDouble(p).Value, unitName, oldUnitName);
};

if (refused == null && (intensityKind != null || kelvin.HasValue || lossKind != null || dimmingName != null || filterColour != null))
{
    var others = familyTypes.Where(t => !targets.Contains(t)).ToList();
    var othersBefore = others.ToDictionary(t => t.Name, t => snapshot(t));
    var original = fm.CurrentType;
    var reachedBy = new Dictionary<string, string>();

    foreach (var t in targets)
    {
        var name = t.Name;
        Action<bool, string> must = (ok, what) =>
        {
            if (!ok) throw new InvalidOperationException("Type \"" + name + "\" " + what + " The call failed, and Heron "
                + "rolls the whole call back.");
        };
        try
        {
            if (fm.CurrentType == null || fm.CurrentType.Name != name) fm.CurrentType = t;
            doc.Regenerate();
            var family = call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc });
            var names = new List<string>();
            var listed = (int)call(family, "GetNumberOfLightTypes", new object[0]);
            for (var n = 0; n < listed; n++) names.Add((string)call(family, "GetLightTypeName", new object[] { n }));
            var index = names.FindIndex(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            reachedBy[name] = index >= 0 ? "" : names.Count > 0 ? names[0] : null;
            if (reachedBy[name] == null)
                throw new InvalidOperationException("Revit's lighting API lists no light type at all with type \"" + name
                    + "\" current, so there is nothing to write its light through. Nothing was kept. By hand: Family Types, "
                    + "choose \"" + name + "\", and set its Initial Intensity and Initial Color there.");
            var data = call(family, "GetLightType", new object[] { index >= 0 ? index : 0 });

            if (intensityKind != null) call(data, "SetInitialIntensity", new[] { make(intensityKind, intensityArgs) });
            if (kelvin.HasValue) call(data, "SetInitialColor", new[] { make("CustomInitialColor", new object[] { kelvin.Value }) });
            if (lossKind != null) call(data, "SetLossFactor", new[] { make(lossKind, lossArgs) });
            if (dimmingName != null) setProperty(data, "DimmingColor", Enum.Parse(lightingType("LightDimmingColor"), dimmingName));
            if (filterColour != null) setProperty(data, "ColorFilter", filterColour);
            doc.Regenerate();

            // A LISTED TYPE, STILL CURRENT, IS READ BACK THROUGH THE LIGHTING API.
            if (index >= 0)
            {
                var back = call(call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc }), "GetLightType", new object[] { index });
                if (intensityKind != null)
                {
                    var start = call(back, "GetInitialIntensity", new object[0]);
                    must(start.GetType().Name == intensityKind, "reads its intensity as " + start.GetType().Name + ", not as asked.");
                    if (intensityKind == "InitialFluxIntensity") must(near(number0f(start, "Flux"), (double)intensityArgs[0]), "reads another luminous flux.");
                    if (intensityKind == "InitialLuminousIntensity") must(near(number0f(start, "Luminosity"), (double)intensityArgs[0]), "reads another luminous intensity.");
                    if (intensityKind == "InitialIlluminanceIntensity")
                        must(near(number0f(start, "Illuminance"), (double)intensityArgs[1]) && near(number0f(start, "Distance"), (double)intensityArgs[0]),
                            "reads another illuminance.");
                    if (intensityKind == "InitialWattageIntensity")
                        must(near(number0f(start, "Efficacy"), (double)intensityArgs[0]) && near(number0f(start, "Wattage"), (double)intensityArgs[1]),
                            "reads another wattage or efficacy.");
                }
                if (kelvin.HasValue)
                {
                    var colour = call(back, "GetInitialColor", new object[0]);
                    must(colour.GetType().Name == "CustomInitialColor" && near(number0f(colour, "Temperature"), kelvin.Value),
                        "reads another initial colour.");
                }
                if (lossKind != null)
                {
                    var loss = call(back, "GetLossFactor", new object[0]);
                    must(loss.GetType().Name == lossKind && near(number0f(loss, "LossFactorValue"), lossTotal),
                        "reads another light loss factor.");
                }
                if (dimmingName != null)
                    must(back.GetType().GetProperty("DimmingColor").GetValue(back).ToString() == dimmingName, "reads another dimming colour shift.");
                if (filterColour != null)
                {
                    var f = (Color)back.GetType().GetProperty("ColorFilter").GetValue(back);
                    must(f != null && f.Red == filterColour.Red && f.Green == filterColour.Green && f.Blue == filterColour.Blue,
                        "reads another colour filter.");
                }
            }
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the light values for type \"" + name + "\": " + reasonOf(ex)
                + " The call failed, and Heron rolls the whole call back.");
        }
    }

    // The modeller's current type back as it was.
    if (original != null && fm.CurrentType != null && fm.CurrentType.Name != original.Name)
    {
        var back = familyTypes.FirstOrDefault(t => t.Name == original.Name);
        if (back != null) fm.CurrentType = back;
    }
    doc.Regenerate();

    // EVERY TYPE ASKED FOR, PROVED FROM ITS OWN TYPE PROPERTIES - the one test
    // for a type the lighting API does not list, and a second for one it does.
    foreach (var t in targets)
    {
        var name = t.Name;
        var throughOther = reachedBy[name];
        Action<double?, double, string, string> held = (value, asked, unit, what) =>
        {
            if (value.HasValue && Math.Abs(value.Value - asked) <= Math.Max(0.5, Math.Abs(asked) * 0.005)) return;
            if (!value.HasValue && throughOther.Length == 0) return;   // a listed type the API already proved
            var shows = value.HasValue ? plain(value.Value) + " " + unit : "nothing";
            throw new InvalidOperationException(throughOther.Length > 0
                ? "Type \"" + name + "\" was not reached. Revit's lighting API does not list it among this family's light "
                  + "types (it lists " + string.Join(", ", lightNames) + "), and written with it current, through \""
                  + throughOther + "\", its " + what + " holds " + shows + ", not " + plain(asked) + " " + unit + ". No call on "
                  + "any release makes a light type. Nothing was kept. By hand: Family Types, choose \"" + name + "\", and "
                  + "set its Initial Intensity, Initial Color and Light Loss Factor there - or name a type the API lists."
                : "Type \"" + name + "\" holds " + shows + " for its " + what + " in Type Properties, not the " + plain(asked)
                  + " " + unit + " asked, while Revit's lighting API reads it as asked. The call failed, and Heron rolls the "
                  + "whole call back.");
        };
        if (intensityKind == "InitialFluxIntensity")
            held(heldIn(t, BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX, "Lumens", "DUT_LUMENS"), (double)intensityArgs[0], "lm", "Luminous Flux");
        if (intensityKind == "InitialLuminousIntensity")
            held(heldIn(t, BuiltInParameter.FBX_LIGHT_LIMUNOUS_INTENSITY, "Candelas", "DUT_CANDELAS"), (double)intensityArgs[0], "cd", "Luminous Intensity");
        if (intensityKind == "InitialIlluminanceIntensity")
            held(heldIn(t, BuiltInParameter.FBX_LIGHT_ILLUMINANCE, "Lux", "DUT_LUX"), (double)intensityArgs[1], "lx", "Illuminance");
        if (intensityKind == "InitialWattageIntensity")
        {
            // BV18: the reference names the wattage's unit two ways, so Type
            // Properties' own Wattage and Efficacy, out of internal units, are
            // the test - for every type, listed or not.
            var watts = heldIn(t, BuiltInParameter.FBX_LIGHT_WATTAGE, "Watts", "DUT_WATTS");
            var efficacy = heldIn(t, BuiltInParameter.FBX_LIGHT_EFFICACY, "LumensPerWatt", "DUT_LUMENS_PER_WATT");
            if (!watts.HasValue || !efficacy.HasValue)
                throw new InvalidOperationException("Type \"" + name + "\" gives no Wattage or Efficacy in Type Properties to "
                    + "check the " + plain((double)intensityArgs[1]) + " W asked against - and Revit's reference names a "
                    + "wattage's unit two ways (\"universal unit\" and W), so it is not kept unchecked. Nothing was kept; "
                    + "give the lumens instead.");
            if (Math.Abs(watts.Value - (double)intensityArgs[1]) > Math.Max(0.01, (double)intensityArgs[1] * 0.005)
                || Math.Abs(efficacy.Value - (double)intensityArgs[0]) > Math.Max(0.01, (double)intensityArgs[0] * 0.005))
                throw new InvalidOperationException("Type \"" + name + "\" holds " + plain(watts.Value) + " W at "
                    + plain(efficacy.Value) + " lm/W in Type Properties for the " + plain((double)intensityArgs[1]) + " W at "
                    + plain((double)intensityArgs[0]) + " lm/W asked - Revit's reference names a wattage's unit two ways, and "
                    + "this Revit did not take W. Nothing was kept; give the lumens instead (" + plain((double)intensityArgs[0]
                    * (double)intensityArgs[1]) + " lm).");
        }
        if (kelvin.HasValue)
            held(heldIn(t, BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_TEMPERATURE, "Kelvin", "DUT_KELVIN"), kelvin.Value, "K", "Initial Color Temperature");
        if (lossKind != null)
        {
            var total = heldIn(t, BuiltInParameter.FBX_LIGHT_TOTAL_LIGHT_LOSS, null, null);
            if (total.HasValue && Math.Abs(total.Value - lossTotal) > 0.0005 || !total.HasValue && throughOther.Length > 0)
                throw new InvalidOperationException((throughOther.Length > 0 ? "Type \"" + name + "\" was not reached - " : "Type \"" + name + "\" ")
                    + "holds a Total Light Loss Factor of " + (total.HasValue ? plain(total.Value) : "nothing") + ", not "
                    + plain(lossTotal) + ". The call failed, and Heron rolls the whole call back.");
        }

        var said = new List<string>();
        if (intensityKind != null)
        {
            var show = intensityKind == "InitialFluxIntensity" ? Tuple.Create(BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX, "Luminous Flux", "lm")
                : intensityKind == "InitialLuminousIntensity" ? Tuple.Create(BuiltInParameter.FBX_LIGHT_LIMUNOUS_INTENSITY, "Luminous Intensity", "cd")
                : intensityKind == "InitialIlluminanceIntensity" ? Tuple.Create(BuiltInParameter.FBX_LIGHT_ILLUMINANCE, "Illuminance", "lx")
                : Tuple.Create(BuiltInParameter.FBX_LIGHT_WATTAGE, "Wattage", "W");
            var display = showing(name, show.Item1);
            var asked = intensityKind == "InitialIlluminanceIntensity" || intensityKind == "InitialWattageIntensity"
                ? (double)intensityArgs[1] : (double)intensityArgs[0];
            var value = shown(display);
            if (value.HasValue && display.Trim().EndsWith(show.Item3) && Math.Abs(value.Value - asked) > Math.Max(0.5, asked * 0.005))
                throw new InvalidOperationException("Type \"" + name + "\" shows " + show.Item2 + " " + display + " for the "
                    + plain(asked) + " " + show.Item3 + " asked. The call failed, and Heron rolls the whole call back.");
            said.Add(show.Item2 + " " + (display ?? plain(asked) + " " + show.Item3));
            if (intensityKind == "InitialWattageIntensity")
                said.Add("Efficacy " + (showing(name, BuiltInParameter.FBX_LIGHT_EFFICACY) ?? plain((double)intensityArgs[0]) + " lm/W")
                    + ", Luminous Flux " + (showing(name, BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX) ?? "not shown"));
        }
        if (kelvin.HasValue) said.Add(showing(name, BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_TEMPERATURE) ?? plain(kelvin.Value) + " K");
        if (lossKind != null) said.Add("light loss factor " + plain(lossTotal));
        if (dimmingName != null) said.Add("dimming " + dimmingName.ToLowerInvariant());
        if (filterColour != null) said.Add("colour filter " + filterColour.Red + " " + filterColour.Green + " " + filterColour.Blue);
        rows.Add("\"" + name + "\"" + (throughOther.Length > 0 ? " (not listed by the lighting API; reached with it current)" : "")
            + ": " + string.Join(", ", said));
        var everything = typeProperties(t);
        if (everything.Length > 0) rows.Add("\"" + name + "\" Type Properties now: " + everything);
    }

    // AND EVERY TYPE NOT ASKED FOR IS AS IT WAS.
    foreach (var t in others)
    {
        var now = snapshot(t);
        if (now != othersBefore[t.Name])
            throw new InvalidOperationException("Writing the light values changed type \"" + t.Name + "\" too, which was not "
                + "asked for - its light values read [" + othersBefore[t.Name] + "] before and [" + now + "] after. The "
                + "family's light types do not keep apart here. The call failed, and Heron rolls the whole call back.");
    }

    if (intensityKind != null) changed.Add("Initial intensity set on " + targets.Count + " type(s).");
    if (kelvin.HasValue) changed.Add("Initial colour set to " + plain(kelvin.Value) + " K on " + targets.Count + " type(s).");
    if (lossKind != null) changed.Add("Light loss factor set to " + plain(lossTotal) + " on " + targets.Count + " type(s).");
    if (dimmingName != null) changed.Add("Dimming colour shift set to " + dimmingName.ToLowerInvariant() + " on " + targets.Count + " type(s).");
    if (filterColour != null) changed.Add("Colour filter set on " + targets.Count + " type(s).");
    if (intensityKind == "InitialWattageIntensity")
        findings.Add("BV18: Type Properties read the wattage back in watts, as the reference's Wattage property says - the "
            + "constructor's \"universal unit\" did not change it on this Revit.");
    if (reachedBy.Values.Any(v => v.Length > 0))
        findings.Add("Revit's lighting API does not list " + string.Join(", ", reachedBy.Where(r => r.Value.Length > 0)
            .Select(r => "\"" + r.Key + "\"")) + " among this family's light types; each was written with it current and "
            + "proved from its own Type Properties (5b-1295).");
}

if (refused == null)
{
    var final = call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc });
    // The family's half is read on every call, so a call for type values
    // still says the shape, the distribution - its IES file and tilt - and the
    // place the light leaves from.
    if (!rows.Any(r => r.StartsWith("the family's light source", StringComparison.Ordinal))
        && (int)call(final, "GetNumberOfLightTypes", new object[0]) > 0)
    {
        var first = call(final, "GetLightType", new object[] { 0 });
        rows.Insert(0, "the family's light source: " + shapeSaid(call(first, "GetLightShape", new object[0])) + ", "
            + spreadSaid(call(first, "GetLightDistribution", new object[0])));
    }
    var at = (Transform)call(final, "GetLightSourceTransform", new object[0]);
    lightReport = string.Join("; ", rows) + ". The light source sits at X " + plain(at.Origin.X * 304.8) + ", Y "
        + plain(at.Origin.Y * 304.8) + ", Z " + plain(at.Origin.Z * 304.8) + " mm, its Z axis pointing ("
        + Math.Round(at.BasisZ.X, 3).ToString(invariant) + ", " + Math.Round(at.BasisZ.Y, 3).ToString(invariant) + ", "
        + Math.Round(at.BasisZ.Z, 3).ToString(invariant) + ").";
    findings.Add(lightReport);
    findings.Add("What Type Properties shows for each value is what the modeller sees; the proof compares it with what "
        + "was asked on a real fixture, and renders one copy.");
}

if (refused != null) findings.Add(refused);
