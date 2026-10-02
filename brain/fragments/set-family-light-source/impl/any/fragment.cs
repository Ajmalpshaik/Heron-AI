// NOT STANDALONE. Assumes `doc`, `shape`, `distribution`, `intensity`,
// `colorTemperature` and `typeName` are in scope; leaves `lightReport`,
// `changed`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE LIGHT SOURCE OF A LIGHTING FIXTURE FAMILY: its SHAPE (point, line,
// rectangle, circle), how its light is DISTRIBUTED (spherical, hemispherical,
// spot, or a photometric web from an IES file), its initial INTENSITY and its
// COLOUR TEMPERATURE - the Light Source Definition and the types' photometric
// values, set through Revit's LightFamily and LightType.
//
// LIGHT SOURCE MUST ALREADY BE TICKED. GetLightFamily throws for a family that
// is not a light family document; the tick is in Family Category and Parameters
// and Heron does not set it - the refusal says where it is.
//
// THE LIGHTING CLASSES ARE REACHED BY NAME at run time: they live in a namespace
// the fragment executor does not import, and every one used here reads the same
// 2020 through 2027.
//
// UNITS ARE THE REFERENCE'S OWN WORDS: emit sizes in feet, spot angles in
// radians, a photometric web's tilt in DEGREES, an illuminance in lux at a
// distance in feet. A wattage is "a universal unit value" there, which names no
// unit, so a wattage is not taken - lumens, candelas or lux at a distance are.
//
// READ BACK, ALL OR NOTHING: the two styles and every type's shape,
// distribution, intensity and colour are read again, and what Type Properties
// shows is read beside them; an angle or intensity Type Properties shows
// differently from what was asked THROWS, and the host rolls the call back.

var findings = new List<string>();
var lightReport = "";
var changed = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var revitAssembly = typeof(Document).Assembly;
var lightingNamespace = typeof(Document).Namespace + ".Lighting.";
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
    (double)target.GetType().GetProperty(property).GetValue(target);

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

// ---------------------------------------------------------------------------
// WHAT WAS ASKED
// ---------------------------------------------------------------------------

var problems = new List<string>();
string shapeStyle = null;          // Point, Line, Rectangle, Circle
object[] shapeArgs = null;         // feet
string distributionStyle = null;   // Spherical, Hemispherical, Spot, PhotometricWeb
object[] distributionArgs = null;
string intensityKind = null;       // InitialFluxIntensity, InitialLuminousIntensity, InitialIlluminanceIntensity
object[] intensityArgs = null;
double? kelvin = null;
object light = null;
var chosen = new List<int>();
var typeNames = new List<string>();

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
        refused = "This family has no light source to define (" + reasonOf(ex) + "). In Family Category and "
            + "Parameters, a Lighting Fixtures family has a Light Source tick box - tick it, and run this again. "
            + "Heron does not tick it.";
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

    var i = (intensity ?? "").Trim().ToLowerInvariant();
    if (i.Length > 0)
    {
        var atAt = i.IndexOf(" at ", StringComparison.Ordinal);
        var amount = number(atAt < 0 ? i : i.Substring(0, atAt));
        if (i.EndsWith("w") || i.Contains(" w ") || i.Contains("lm/w"))
            problems.Add("A wattage is not taken: Revit's reference calls it \"a universal unit value\" and names no "
                + "unit, so Heron would be guessing. Give the luminous flux in lumens - \"1500 lm\" - or the "
                + "intensity in candelas or lux.");
        else if (!amount.HasValue || amount < 0)
            problems.Add("\"" + intensity.Trim() + "\" is not an intensity - \"1500 lm\", \"1200 cd\" or \"500 lx at 1000 "
                + "mm\".");
        else if (atAt < 0 && i.EndsWith("lm")) { intensityKind = "InitialFluxIntensity"; intensityArgs = new object[] { amount.Value }; }
        else if (atAt < 0 && i.EndsWith("cd")) { intensityKind = "InitialLuminousIntensity"; intensityArgs = new object[] { amount.Value }; }
        else if (atAt > 0 && i.Substring(0, atAt).Trim().EndsWith("lx"))
        {
            var distance = number(i.Substring(atAt + 4));
            if (!distance.HasValue || distance <= 0)
                problems.Add("\"" + intensity.Trim() + "\" needs the distance in millimetres - \"500 lx at 1000 mm\".");
            else
            {
                intensityKind = "InitialIlluminanceIntensity";
                intensityArgs = new object[] { distance.Value / 304.8, amount.Value };
            }
        }
        else problems.Add("\"" + intensity.Trim() + "\" does not say its unit - lm, cd, or lx at a distance.");
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

    if (shapeStyle == null && distributionStyle == null && intensityKind == null && !kelvin.HasValue && problems.Count == 0)
        problems.Add("Nothing was asked - a shape, a distribution, an intensity or a colour temperature.");

    // THE TYPES: every one, or the one named.
    var count = (int)call(light, "GetNumberOfLightTypes", new object[0]);
    for (var t = 0; t < count; t++) typeNames.Add((string)call(light, "GetLightTypeName", new object[] { t }));
    var wanted = (typeName ?? "").Trim();
    for (var t = 0; t < count; t++)
        if (wanted.Length == 0 || string.Equals(typeNames[t], wanted, StringComparison.OrdinalIgnoreCase)) chosen.Add(t);
    if (count == 0) problems.Add("This family has no types yet - SET_FAMILY_TYPE_VALUES makes the first one.");
    else if (chosen.Count == 0)
        problems.Add("No type is called \"" + wanted + "\" - this family's types: " + string.Join(", ", typeNames) + ".");

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// SET, THEN READ EVERYTHING BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    try
    {
        // The styles first: they are the family's, and each type's shape and
        // distribution must then be of that style.
        if (shapeStyle != null)
            call(light, "SetLightShapeStyle", new[] { Enum.Parse(lightingType("LightShapeStyle"), shapeStyle) });
        if (distributionStyle != null)
            call(light, "SetLightDistributionStyle", new[] { Enum.Parse(lightingType("LightDistributionStyle"), distributionStyle) });
        foreach (var t in chosen)
        {
            var data = call(light, "GetLightType", new object[] { t });
            if (shapeStyle != null) call(data, "SetLightShape", new[] { make(shapeStyle + "LightShape", shapeArgs) });
            if (distributionStyle != null)
                call(data, "SetLightDistribution", new[] { make(distributionStyle + "LightDistribution", distributionArgs) });
            if (intensityKind != null) call(data, "SetInitialIntensity", new[] { make(intensityKind, intensityArgs) });
            if (kelvin.HasValue) call(data, "SetInitialColor", new[] { make("CustomInitialColor", new object[] { kelvin.Value }) });
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the light source: " + reasonOf(ex) + " The call failed, and "
            + "Heron rolls the whole call back.");
    }
    doc.Regenerate();

    var fm = doc.FamilyManager;
    Func<string, BuiltInParameter, string> showing = (name, bip) =>
    {
        var type = fm.Types.Cast<FamilyType>().FirstOrDefault(ft => ft.Name == name);
        var p = fm.get_Parameter(bip);
        if (type == null || p == null || !type.HasValue(p)) return null;
        return type.AsValueString(p);
    };

    var again = call(lightingType("LightFamily"), "GetLightFamily", new object[] { doc });
    if (shapeStyle != null && call(again, "GetLightShapeStyle", new object[0]).ToString() != shapeStyle)
        throw new InvalidOperationException("The light shape reads " + call(again, "GetLightShapeStyle", new object[0])
            + ", not " + shapeStyle + ". The call failed, and Heron rolls the whole call back.");
    if (distributionStyle != null && call(again, "GetLightDistributionStyle", new object[0]).ToString() != distributionStyle)
        throw new InvalidOperationException("The light distribution reads " + call(again, "GetLightDistributionStyle",
            new object[0]) + ", not " + distributionStyle + ". The call failed, and Heron rolls the whole call back.");

    var rows = new List<string>();
    foreach (var t in chosen)
    {
        var name = typeNames[t];
        var data = call(again, "GetLightType", new object[] { t });
        var said = new List<string>();
        Action<bool, string> must = (ok, what) =>
        {
            if (!ok) throw new InvalidOperationException("Type \"" + name + "\" " + what + " The call failed, and Heron "
                + "rolls the whole call back.");
        };
        Func<double, double, bool> near = (a, b) => Math.Abs(a - b) <= Math.Max(1e-6, Math.Abs(b) * 1e-4);

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
        said.Add(shapeName.ToLowerInvariant()
            + (shapeName == "Line" ? " " + plain(number0f(lightShape, "EmitLength") * 304.8) + " mm"
            : shapeName == "Rectangle" ? " " + plain(number0f(lightShape, "EmitLength") * 304.8) + " x "
                + plain(number0f(lightShape, "EmitWidth") * 304.8) + " mm"
            : shapeName == "Circle" ? " " + plain(number0f(lightShape, "EmitDiameter") * 304.8) + " mm" : ""));

        var spread = call(data, "GetLightDistribution", new object[0]);
        var spreadName = spread.GetType().Name.Replace("LightDistribution", "");
        if (distributionStyle != null)
        {
            must(spreadName == distributionStyle, "reads a " + spreadName + " distribution, not " + distributionStyle + ".");
            if (distributionStyle == "Spot")
            {
                must(near(number0f(spread, "SpotBeamAngle"), (double)distributionArgs[0])
                    && near(number0f(spread, "SpotFieldAngle"), (double)distributionArgs[1])
                    && near(number0f(spread, "TiltAngle"), (double)distributionArgs[2]), "reads other spot angles.");
                // RADIANS ARE THE REFERENCE'S WORD; TYPE PROPERTIES IS THE TEST.
                var beamShown = showing(name, BuiltInParameter.FBX_LIGHT_SPOT_BEAM_ANGLE);
                var beam = shown(beamShown);
                if (beam.HasValue && beamShown.Contains("°"))
                    must(Math.Abs(beam.Value - (double)distributionArgs[0] * 180 / Math.PI) < 0.5,
                        "shows a Spot Beam Angle of " + beamShown + " for the " + plain((double)distributionArgs[0] * 180 / Math.PI)
                        + "° asked - the angle's unit is not the one Revit's reference names.");
            }
            if (distributionStyle == "PhotometricWeb")
            {
                must(string.Equals((string)spread.GetType().GetProperty("PhotometricWebFile").GetValue(spread),
                    (string)distributionArgs[0], StringComparison.OrdinalIgnoreCase), "reads another IES file.");
                // A web's tilt is read back too - in DEGREES, the reference's
                // unit for this one, where a spot's is in radians.
                must(near(number0f(spread, "TiltAngle"), (double)distributionArgs[1]), "reads another tilt.");
            }
        }
        said.Add(spreadName == "Spot"
            ? "spot, beam " + plain(number0f(spread, "SpotBeamAngle") * 180 / Math.PI) + "°, field "
                + plain(number0f(spread, "SpotFieldAngle") * 180 / Math.PI) + "°, tilt " + plain(number0f(spread, "TiltAngle") * 180 / Math.PI) + "°"
            : spreadName == "PhotometricWeb"
                ? "photometric web " + System.IO.Path.GetFileName((string)spread.GetType().GetProperty("PhotometricWebFile").GetValue(spread))
                    + ", tilt " + plain(number0f(spread, "TiltAngle")) + "°"
                : spreadName.ToLowerInvariant());

        var start = call(data, "GetInitialIntensity", new object[0]);
        if (intensityKind != null)
        {
            must(start.GetType().Name == intensityKind, "reads its intensity as " + start.GetType().Name + ", not as asked.");
            Tuple<string, BuiltInParameter, string> check = null;
            if (intensityKind == "InitialFluxIntensity")
            {
                must(near(number0f(start, "Flux"), (double)intensityArgs[0]), "reads another luminous flux.");
                check = Tuple.Create("lm", BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX, "Luminous Flux");
            }
            if (intensityKind == "InitialLuminousIntensity")
            {
                must(near(number0f(start, "Luminosity"), (double)intensityArgs[0]), "reads another luminous intensity.");
                check = Tuple.Create("cd", BuiltInParameter.FBX_LIGHT_LIMUNOUS_INTENSITY, "Luminous Intensity");
            }
            if (intensityKind == "InitialIlluminanceIntensity")
            {
                must(near(number0f(start, "Illuminance"), (double)intensityArgs[1])
                    && near(number0f(start, "Distance"), (double)intensityArgs[0]), "reads another illuminance.");
                check = Tuple.Create("lx", BuiltInParameter.FBX_LIGHT_ILLUMINANCE, "Illuminance");
            }
            var display = showing(name, check.Item2);
            var value = shown(display);
            var asked = intensityKind == "InitialIlluminanceIntensity" ? (double)intensityArgs[1] : (double)intensityArgs[0];
            if (value.HasValue && display.Trim().EndsWith(check.Item1))
                must(Math.Abs(value.Value - asked) <= Math.Max(0.5, asked * 0.005),
                    "shows " + check.Item3 + " " + display + " for the " + plain(asked) + " " + check.Item1 + " asked.");
            said.Add(check.Item3 + " " + (display ?? plain(asked) + " " + check.Item1));
        }

        if (kelvin.HasValue)
        {
            var colour = call(data, "GetInitialColor", new object[0]);
            must(colour.GetType().Name == "CustomInitialColor" && near(number0f(colour, "Temperature"), kelvin.Value),
                "reads another initial colour.");
            said.Add(plain(kelvin.Value) + " K");
        }

        rows.Add("\"" + name + "\": " + string.Join(", ", said));
    }

    if (shapeStyle != null) changed.Add("Light shape set to " + shapeStyle.ToLowerInvariant() + ".");
    if (distributionStyle != null) changed.Add("Light distribution set to " + (distributionStyle == "PhotometricWeb" ? "photometric web" : distributionStyle.ToLowerInvariant()) + ".");
    if (intensityKind != null) changed.Add("Initial intensity set on " + chosen.Count + " type(s).");
    if (kelvin.HasValue) changed.Add("Initial colour set to " + plain(kelvin.Value) + " K on " + chosen.Count + " type(s).");

    var at = ((Transform)call(again, "GetLightSourceTransform", new object[0])).Origin;
    lightReport = chosen.Count + " type(s), read back: " + string.Join("; ", rows) + ". The light source sits at X "
        + plain(at.X * 304.8) + ", Y " + plain(at.Y * 304.8) + ", Z " + plain(at.Z * 304.8) + " mm.";
    findings.Add(lightReport);
    findings.Add("What Type Properties shows for each value is what the modeller sees; BV18 compares it with what "
        + "was asked on a real fixture, and renders one copy.");
}

if (refused != null) findings.Add(refused);
