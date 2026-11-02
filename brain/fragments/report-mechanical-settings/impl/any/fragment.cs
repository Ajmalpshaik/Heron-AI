// NOT STANDALONE. Assumes `doc` and `show` are in scope; leaves `ductSettings`,
// `pipeSettings`, `settingsRead` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// MANAGE > MEP SETTINGS > MECHANICAL SETTINGS, DUCT SETTINGS AND PIPE SETTINGS,
// READ IN THE WORDS SET_MECHANICAL_SETTINGS TAKES, so a value read here can be
// named back to it as it is:
//   Duct Settings - use annotation scale for single line fittings, duct fitting
//     annotation size, air density, air dynamic viscosity, the rectangular,
//     round and oval duct size prefix, separator and suffix, duct connector
//     separator, flat on top / bottom, set up / down from center and bottom,
//     centerline, rise/drop annotation size.
//   Pipe Settings - use annotation scale for single line fittings, pipe fitting
//     annotation size, pipe size prefix and suffix, pipe connector separator,
//     flat on top / bottom, set up / down from center and bottom, centerline.
//
// `show` is duct, pipe or all (`absent: all`); a word that is none of them is
// named, never guessed. Annotation sizes are millimetres, 0.01 mm; air density
// kg/m3 and air dynamic viscosity Pa-s, converted by Revit's own UnitUtils.
// Text is printed in double quotes so a space at either end shows.
//
// NOT READ, and the answer says so: the rise/drop symbols (no member on
// DuctSettings or PipeSettings, 2020-2027; Revit holds them on each system
// type), fitting angles, pipe slopes, and the conversion, sizing and
// calculation pages.

var findings = new List<string>();
var ductSettings = "";
var pipeSettings = "";
var settingsRead = 0;
var culture = System.Globalization.CultureInfo.InvariantCulture;

var wanted = (show ?? "").Trim().ToLowerInvariant();
if (wanted == "none") wanted = "";
bool readDuct = false, readPipe = false;
if (wanted == "" || wanted == "all" || wanted == "both" || wanted == "everything") { readDuct = true; readPipe = true; }
else
{
    foreach (var word in wanted.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
    {
        if (word == "duct" || word == "ducts" || word == "ductwork") readDuct = true;
        else if (word == "pipe" || word == "pipes" || word == "piping") readPipe = true;
        else if (word == "and" || word == "settings") { }
        else findings.Add("\"" + word + "\" is not duct, pipe or all - not read.");
    }
    if (!readDuct && !readPipe) { readDuct = true; readPipe = true; findings.Add("Nothing named that this reads, so both pages are read."); }
}

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", culture) + " mm";
Func<bool, string> yesNo = b => b ? "yes" : "no";
Func<string, string> text = s => "\"" + (s ?? "") + "\"";
#if REVIT2020
Func<double, string> density = v => Math.Round(UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER), 4).ToString("0.####", culture) + " kg/m3";
Func<double, string> viscosity = v => UnitUtils.ConvertFromInternalUnits(v, DisplayUnitType.DUT_PASCAL_SECONDS).ToString("G6", culture) + " Pa-s";
#else
Func<double, string> density = v => Math.Round(UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.KilogramsPerCubicMeter), 4).ToString("0.####", culture) + " kg/m3";
Func<double, string> viscosity = v => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.PascalSeconds).ToString("G6", culture) + " Pa-s";
#endif

if (readDuct)
{
    DuctSettings ds = null;
    try { ds = DuctSettings.GetDuctSettings(doc); } catch (Exception ex) { findings.Add("Revit gave no Duct Settings: " + ex.Message); }
    if (ds != null)
    {
        double airViscosity;
#if REVIT2025_OR_GREATER
        airViscosity = ds.AirDynamicViscosity;
#else
        airViscosity = ds.AirViscosity;
#endif
        var parts = new List<string>
        {
            "duct use annotation scale for single line fittings " + yesNo(ds.UseAnnotationScaleForSingleLineFittings),
            "duct fitting annotation size " + mm(ds.FittingAnnotationSize),
            "air density " + density(ds.AirDensity),
            "air dynamic viscosity " + viscosity(airViscosity),
            "rectangular duct size separator " + text(ds.RectangularDuctSizeSeparator),
            "rectangular duct size suffix " + text(ds.RectangularDuctSizeSuffix),
            "round duct size prefix " + text(ds.RoundDuctSizePrefix),
            "round duct size suffix " + text(ds.RoundDuctSizeSuffix),
            "oval duct size separator " + text(ds.OvalDuctSizeSeparator),
            "oval duct size suffix " + text(ds.OvalDuctSizeSuffix),
            "duct connector separator " + text(ds.ConnectorSeparator),
            "duct flat on top " + text(ds.FlatOnTop),
            "duct flat on bottom " + text(ds.FlatOnBottom),
            "duct set up from center " + text(ds.SetUp),
            "duct set down from center " + text(ds.SetDown),
            "duct set up from bottom " + text(ds.SetUpFromBottom),
            "duct set down from bottom " + text(ds.SetDownFromBottom),
            "duct centerline " + text(ds.Centerline),
            "duct rise/drop annotation size " + mm(ds.RiseDropAnnotationSize),
        };
        settingsRead += parts.Count;
        ductSettings = "Duct Settings - " + string.Join("; ", parts);
    }
    else if (findings.Count == 0 || !findings.Last().StartsWith("Revit gave no Duct Settings"))
        findings.Add(doc.Title + " has no Duct Settings - Mechanical Settings are a project's.");
}

if (readPipe)
{
    PipeSettings ps = null;
    try { ps = PipeSettings.GetPipeSettings(doc); } catch (Exception ex) { findings.Add("Revit gave no Pipe Settings: " + ex.Message); }
    if (ps != null)
    {
        var parts = new List<string>
        {
            "pipe use annotation scale for single line fittings " + yesNo(ps.UseAnnotationScaleForSingleLineFittings),
            "pipe fitting annotation size " + mm(ps.FittingAnnotationSize),
            "pipe size prefix " + text(ps.SizePrefix),
            "pipe size suffix " + text(ps.SizeSuffix),
            "pipe connector separator " + text(ps.ConnectorSeparator),
            "pipe flat on top " + text(ps.FlatOnTop),
            "pipe flat on bottom " + text(ps.FlatOnBottom),
            "pipe set up from center " + text(ps.SetUp),
            "pipe set down from center " + text(ps.SetDown),
            "pipe set up from bottom " + text(ps.SetUpFromBottom),
            "pipe set down from bottom " + text(ps.SetDownFromBottom),
            "pipe centerline " + text(ps.Centerline),
        };
        settingsRead += parts.Count;
        pipeSettings = "Pipe Settings - " + string.Join("; ", parts);
    }
    else if (findings.Count == 0 || !findings.Last().StartsWith("Revit gave no Pipe Settings"))
        findings.Add(doc.Title + " has no Pipe Settings - Mechanical Settings are a project's.");
}

findings.Add("Not read here: the rise/drop symbols (Revit holds them on each duct and pipe system type - no Mechanical "
    + "Settings member reaches them on any release 2020-2027), fitting angles, pipe slopes, and the conversion, sizing "
    + "and calculation pages. Manage > MEP Settings > Mechanical Settings shows them.");
