// NOT STANDALONE. Assumes `doc`, `show` and `standard` are in scope, and
// leaves `tables`, `sizeCount`, `report`, `names` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Manage > MEP Settings > Mechanical Settings > Duct Settings >
// Rectangular, Oval and Round, and Electrical Settings > Conduit Settings >
// Size (one list per conduit standard) and Cable Tray Settings > Size. Every
// list, or only the ones `show` names (round; rectangular; oval; duct for all
// three; conduit; tray - several separated by ; or ,), with the conduit
// standards narrowed to names containing `standard`.
//
// NOT PIPE SEGMENTS. A pipe segment's sizes are REPORT_PIPE_SEGMENTS' - name
// one segment there and its whole size table comes back. Reading them again
// here would be a second tool for one question.
//
// EVERY SIZE IS READ IN REVIT'S FEET AND SHOWN IN MILLIMETRES, x 304.8 as plain
// arithmetic - never a units call, which is what changed shape at 2021.
//
// A SIZE THAT IS NOT IN THE SIZE LISTS IS STILL A SIZE. It stays in the table
// and never reaches the size dropdown, so it is shown and marked "(hidden)"
// rather than dropped - a missing number reads as a size nobody added.
//
// RELEASES. DuctSizeSettings.GetDuctSizeSettings and its DuctShape indexer,
// DuctSizes, MEPSize.NominalDiameter, InnerDiameter, OuterDiameter,
// UsedInSizeLists, UsedInSizing, ConduitSizeSettings.GetConduitSizeSettings and
// its standard-by-standard enumeration, ConduitSize.BendRadius and
// CableTraySizes.GetCableTraySizes carry the same signatures 2020 to 2027
// (tools/api-surface --members, 2026-11-01). One implementation for all eight.

var tables = 0;
var sizeCount = 0;
var report = "";
var names = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", invariant);
var needle = (standard ?? "").Trim();

// ---- Which lists -----------------------------------------------------------
var asked = (show ?? "").Trim().ToLowerInvariant();
var every = asked.Length == 0 || asked == "all";
bool wantRound = every, wantRect = every, wantOval = every, wantConduit = every, wantTray = every;
var unknownWords = new List<string>();
if (!every)
{
    foreach (var raw in asked.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var w = raw.Trim().Replace(" ", "").Replace("-", "");
        if (w.Length == 0) continue;
        if (w == "all") { wantRound = wantRect = wantOval = wantConduit = wantTray = true; }
        else if (w.StartsWith("round")) wantRound = true;
        else if (w.StartsWith("rect")) wantRect = true;
        else if (w.StartsWith("oval")) wantOval = true;
        else if (w.StartsWith("duct")) { wantRound = wantRect = wantOval = true; }
        else if (w.StartsWith("conduit")) wantConduit = true;
        else if (w.StartsWith("tray") || w.StartsWith("cabletray")) wantTray = true;
        else unknownWords.Add(raw.Trim());
    }
}
if (unknownWords.Count > 0)
    findings.Add("\"" + string.Join("\", \"", unknownWords) + "\" is not one of the size lists - round, "
        + "rectangular, oval (or duct for all three), conduit or tray. Nothing was guessed for it. "
        + "A pipe segment's sizes are REPORT_PIPE_SEGMENTS'.");
if (needle.Length > 0 && !wantConduit)
    findings.Add("A conduit standard (\"" + needle + "\") was named but the conduit sizes were not asked for, "
        + "so it narrowed nothing.");

// One size, in words: its nominal, and what keeps it out of the dropdown.
Func<MEPSize, string> plain = s =>
    mm(s.NominalDiameter) + (s.UsedInSizeLists ? "" : " (hidden)") + (s.UsedInSizing ? "" : " (not for sizing)");

var lines = new List<string>();

// ---- Duct Settings: Round, Rectangular, Oval -------------------------------
DuctSizeSettings ductSettings = null;
if (wantRound || wantRect || wantOval)
{
    try { ductSettings = DuctSizeSettings.GetDuctSizeSettings(doc); }
    catch (Exception ex) { findings.Add("Duct Settings could not be read: " + ex.Message); }
    if (ductSettings == null)
        findings.Add("This model has no duct size settings to read"
            + (doc.IsFamilyDocument ? " - it is a family; open a project." : "."));
}
var shapes = new List<KeyValuePair<DuctShape, string>>();
if (wantRound) shapes.Add(new KeyValuePair<DuctShape, string>(DuctShape.Round, "Round duct"));
if (wantRect) shapes.Add(new KeyValuePair<DuctShape, string>(DuctShape.Rectangular, "Rectangular duct"));
if (wantOval) shapes.Add(new KeyValuePair<DuctShape, string>(DuctShape.Oval, "Oval duct"));
if (ductSettings != null)
{
    foreach (var shape in shapes)
    {
        var sizes = new List<MEPSize>();
        try
        {
            var list = ductSettings[shape.Key];
            if (list != null) foreach (MEPSize s in list) if (s != null) sizes.Add(s);
        }
        catch (Exception ex)
        {
            findings.Add(shape.Value + " sizes could not be read: " + ex.Message);
            continue;
        }
        sizes.Sort((a, b) => a.NominalDiameter.CompareTo(b.NominalDiameter));
        tables++;
        sizeCount += sizes.Count;
        var hidden = sizes.Count(s => !s.UsedInSizeLists);
        lines.Add(shape.Value + " sizes (" + sizes.Count + "): "
            + (sizes.Count == 0 ? "none" : string.Join(", ", sizes.Select(plain)) + " mm")
            + (hidden > 0 ? " - " + hidden + " hidden from the size list" : ""));
    }
}

// ---- Conduit Settings: Size, one list per standard -------------------------
if (wantConduit)
{
    ConduitSizeSettings conduitSettings = null;
    try { conduitSettings = ConduitSizeSettings.GetConduitSizeSettings(doc); }
    catch (Exception ex) { findings.Add("Conduit Settings could not be read: " + ex.Message); }
    if (conduitSettings == null)
        findings.Add("This model has no conduit size settings to read"
            + (doc.IsFamilyDocument ? " - it is a family; open a project." : "."));
    else
    {
        var standards = new List<KeyValuePair<string, List<ConduitSize>>>();
        var everyStandard = new List<string>();
        try
        {
            foreach (var pair in conduitSettings)
            {
                everyStandard.Add(pair.Key ?? "");
                if (needle.Length > 0 && (pair.Key ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var sizes = new List<ConduitSize>();
                if (pair.Value != null) foreach (ConduitSize s in pair.Value) if (s != null) sizes.Add(s);
                sizes.Sort((a, b) => a.NominalDiameter.CompareTo(b.NominalDiameter));
                standards.Add(new KeyValuePair<string, List<ConduitSize>>(pair.Key ?? "", sizes));
            }
        }
        catch (Exception ex) { findings.Add("The conduit standards could not be read: " + ex.Message); }
        standards.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
        foreach (var standardSizes in standards)
        {
            tables++;
            sizeCount += standardSizes.Value.Count;
            names.Add("\"" + standardSizes.Key + "\" (conduit standard)");
            var hidden = standardSizes.Value.Count(s => !s.UsedInSizeLists);
            lines.Add("Conduit standard \"" + standardSizes.Key + "\" sizes (" + standardSizes.Value.Count + "): "
                + (standardSizes.Value.Count == 0 ? "none" : string.Join(", ", standardSizes.Value.Select(s =>
                    mm(s.NominalDiameter) + " (ID " + mm(s.InnerDiameter) + ", OD " + mm(s.OuterDiameter)
                    + ", bend radius " + mm(s.BendRadius) + ")" + (s.UsedInSizeLists ? "" : " (hidden)")
                    + (s.UsedInSizing ? "" : " (not for sizing)"))) + " mm")
                + (hidden > 0 ? " - " + hidden + " hidden from the size list" : ""));
        }
        if (needle.Length > 0 && standards.Count == 0)
            findings.Add("No conduit standard has a name containing \"" + needle + "\". The standards in this model: "
                + (everyStandard.Count == 0 ? "none" : "\"" + string.Join("\", \"", everyStandard) + "\"") + ".");
    }
}

// ---- Cable Tray Settings: Size ---------------------------------------------
if (wantTray)
{
    CableTraySizes traySizes = null;
    try { traySizes = CableTraySizes.GetCableTraySizes(doc); }
    catch (Exception ex) { findings.Add("Cable Tray Settings could not be read: " + ex.Message); }
    if (traySizes == null)
        findings.Add("This model has no cable tray size settings to read"
            + (doc.IsFamilyDocument ? " - it is a family; open a project." : "."));
    else
    {
        var sizes = new List<MEPSize>();
        try { foreach (MEPSize s in traySizes) if (s != null) sizes.Add(s); }
        catch (Exception ex) { findings.Add("The cable tray sizes could not be read: " + ex.Message); }
        sizes.Sort((a, b) => a.NominalDiameter.CompareTo(b.NominalDiameter));
        tables++;
        sizeCount += sizes.Count;
        var hidden = sizes.Count(s => !s.UsedInSizeLists);
        lines.Add("Cable tray sizes (" + sizes.Count + "): "
            + (sizes.Count == 0 ? "none" : string.Join(", ", sizes.Select(plain)) + " mm")
            + (hidden > 0 ? " - " + hidden + " hidden from the size list" : ""));
    }
}

report = string.Join(" | ", lines);
findings.Insert(0, tables + " size list(s) read, " + sizeCount + " size(s) in them"
    + (every ? "" : " - only the lists asked for were read")
    + (needle.Length > 0 && wantConduit ? ", conduit standards narrowed to \"" + needle + "\"" : "") + ".");
findings.AddRange(lines);
