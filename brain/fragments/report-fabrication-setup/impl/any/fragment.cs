// NOT STANDALONE. Assumes `doc`, `elements`, `show` and `service` are in scope,
// and leaves `configurationName`, `loadedServices`, `buttons`,
// `fabricationParts`, `report`, `names`, `notFabrication` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Revit's Systems > Fabrication > MEP Fabrication Settings: the
// fabrication configuration the project uses (its name, version, where it is
// kept, its units and the profile in use) and the services LOADED in this
// project out of all the configuration has - each with its abbreviation, its
// palettes and the buttons on each palette. And the fabrication parts handed
// in - each with its service, item number, size and, for a straight, its
// length. Every list, or only the ones `show` names (configuration; services;
// buttons; parts), with the services narrowed to a name or abbreviation
// containing `service`.
//
// WHAT "every list" READS. With nothing selected: the configuration, the
// loaded services with each palette's button count, every button by name when
// `service` narrows the services, and how many fabrication parts the model
// holds. With a selection: the configuration and the parts selected. Every
// button of every service is read only when `show` asks for buttons - a
// configuration can carry thousands.
//
// NO FABRICATION CONFIGURATION is said in Revit words - where one is chosen -
// and nothing else is read; never an empty list passed off as an answer.
//
// RELEASES (tools/api-surface --members, 2026-11-02). FabricationConfiguration
// .GetFabricationConfiguration (its parameter renamed in 2024, called by
// position), GetFabricationConfigurationInfo, GetAllLoadedServices,
// GetAllServices, GetAllUsedServices, GetProfile and HasValidConfiguration;
// FabricationConfigurationInfo Name, Version, Path, IsCloudConfiguration,
// IsLocked and UnitSystem; FabricationService Name, Abbreviation, ServiceId,
// FabricationSystemName, GetButtonCount and GetButton; FabricationServiceButton
// Name, Code, IsStraight, IsAHanger and IsExcluded; FabricationPart ServiceName,
// ServiceAbbreviation, ItemNumber, Size, SpoolName, CenterlineLength,
// IsAStraight and IsAHanger are present 2020 to 2027. A service's button sets
// are GROUPS to 2021 (GroupCount, GetGroupName, IsGroupExcluded) and PALETTES
// from 2022 (PaletteCount, GetPaletteName, IsPaletteExcluded) - each release
// takes its own branch (#if REVIT2020 || REVIT2021).
//
// UNITS. Lengths are Revit feet x 304.8, said in mm (D-20).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var configurationName = "";
var loadedServices = 0;
var buttons = 0;
var fabricationParts = 0;
var report = "";
var names = new List<string>();
var notFabrication = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> named = e =>
    (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

var picked = new List<Element>();
var seenIds = new HashSet<ElementId>();
foreach (var e in elements ?? new List<Element>())
    if (e != null && seenIds.Add(e.Id)) picked.Add(e);

// ---- Which lists -----------------------------------------------------------
var asked = (show ?? "").Trim().ToLowerInvariant();
var every = asked.Length == 0 || asked == "all";
var needle = (service ?? "").Trim();
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
if (string.Equals(needle, "none", StringComparison.OrdinalIgnoreCase)) needle = "";
bool wantConfiguration = every, wantServices = every && picked.Count == 0, wantButtons = false,
     wantParts = every && picked.Count > 0;
var unknownWords = new List<string>();
if (!every)
{
    foreach (var raw in asked.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var w = raw.Trim().Replace(" ", "").Replace("-", "");
        if (w.Length == 0) continue;
        if (w == "all") { wantConfiguration = wantServices = wantParts = true; }
        else if (w.StartsWith("config") || w == "setup" || w == "settings") wantConfiguration = true;
        else if (w.StartsWith("service")) wantServices = true;
        else if (w.StartsWith("button") || w.StartsWith("palette") || w.StartsWith("group")) { wantServices = true; wantButtons = true; }
        else if (w.StartsWith("part") || w.StartsWith("selection") || w.StartsWith("selected")) wantParts = true;
        else unknownWords.Add(raw.Trim());
    }
}
// A named service is a question about that service's buttons too.
if (wantServices && needle.Length > 0) wantButtons = true;
foreach (var w in unknownWords)
    findings.Add(quoted(w) + " is not one of the lists - configuration, services, buttons or parts. Nothing was read for it");

var lines = new List<string>();
var config = FabricationConfiguration.GetFabricationConfiguration(doc);

if (config == null)
{
    report = "This model has no fabrication configuration - one is chosen in Systems > Fabrication > MEP "
           + "Fabrication Settings. Without one there are no fabrication services or parts to read";
    findings.Add("No fabrication configuration is loaded in this model");
    foreach (var e in picked)
        notFabrication.Add(named(e) + ": not a fabrication part - this model has no fabrication configuration");
}
else
{
    // ---- Configuration -----------------------------------------------------
    var info = config.GetFabricationConfigurationInfo();
    configurationName = info == null ? "" : info.Name ?? "";
    if (!config.HasValidConfiguration())
        findings.Add("Revit says the fabrication configuration " + quoted(configurationName)
                   + " is not valid - reload it in MEP Fabrication Settings before relying on it");
    if (wantConfiguration && info != null)
    {
        var profile = config.GetProfile();
        lines.Add("Fabrication configuration " + quoted(configurationName)
                + ", version " + info.Version.ToString("0.##", invariant)
                + (info.IsCloudConfiguration ? ", from the cloud" : ", kept at " + quoted(info.Path))
                + ", " + info.UnitSystem + " units"
                + ", profile " + (string.IsNullOrEmpty(profile) ? "Global" : quoted(profile))
                + (info.IsLocked ? ", locked" : ""));
    }

    // ---- Services, palettes and buttons ------------------------------------
    var loaded = config.GetAllLoadedServices() ?? new List<FabricationService>();
    var all = config.GetAllServices() ?? new List<FabricationService>();
    var used = new HashSet<int>();
    foreach (var s in config.GetAllUsedServices() ?? new List<FabricationService>())
        if (s != null) used.Add(s.ServiceId);

    Func<FabricationService, bool> serviceWanted = s =>
        needle.Length == 0
        || (s.Name ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
        || string.Equals((s.Abbreviation ?? "").Trim(), needle, StringComparison.OrdinalIgnoreCase);

    if (wantServices)
    {
        var shown = loaded.Where(s => s != null && serviceWanted(s)).ToList();
        loadedServices = shown.Count;
        var block = new List<string>();
        foreach (var s in shown)
        {
            names.Add(quoted(s.Name) + " (service)");
            var palettes = new List<string>();
#if REVIT2020 || REVIT2021
            var paletteCount = s.GroupCount;
#else
            var paletteCount = s.PaletteCount;
#endif
            for (var p = 0; p < paletteCount; p++)
            {
#if REVIT2020 || REVIT2021
                var paletteName = s.GetGroupName(p);
                var paletteExcluded = s.IsGroupExcluded(p);
#else
                var paletteName = s.GetPaletteName(p);
                var paletteExcluded = s.IsPaletteExcluded(p);
#endif
                var count = s.GetButtonCount(p);
                buttons += count;
                var text = quoted(paletteName) + " " + count + " button(s)" + (paletteExcluded ? ", excluded" : "");
                if (wantButtons)
                {
                    var buttonNames = new List<string>();
                    for (var b = 0; b < count; b++)
                    {
                        var button = s.GetButton(p, b);
                        if (button == null) continue;
                        buttonNames.Add(quoted(button.Name)
                            + (string.IsNullOrEmpty(button.Code) ? "" : " code " + button.Code)
                            + (button.IsStraight ? ", straight" : "")
                            + (button.IsAHanger ? ", hanger" : "")
                            + (button.IsExcluded() ? ", excluded" : ""));
                    }
                    text += buttonNames.Count > 0 ? ": " + string.Join("; ", buttonNames.ToArray()) : "";
                }
                palettes.Add(text);
            }
            block.Add(quoted(s.Name)
                + (string.IsNullOrEmpty(s.Abbreviation) ? "" : " (" + s.Abbreviation + ")")
                + (string.IsNullOrEmpty(s.FabricationSystemName) ? "" : ", system " + quoted(s.FabricationSystemName))
                + (used.Contains(s.ServiceId) ? ", used by parts in this model" : ", not used by any part yet")
                + " - " + paletteCount + " palette(s)"
                + (palettes.Count > 0 ? ": " + string.Join(" | ", palettes.ToArray()) : ""));
        }
        lines.Add("Services loaded in this project: " + loaded.Count + " of the " + all.Count
                + " the configuration has" + (needle.Length > 0 ? ", " + shown.Count + " matching " + quoted(needle) : "")
                + (block.Count > 0 ? ". " + string.Join(". ", block.ToArray()) : ""));

        if (needle.Length > 0 && shown.Count == 0)
        {
            var notLoaded = all.Where(s => s != null && serviceWanted(s)
                                        && !loaded.Any(l => l != null && l.ServiceId == s.ServiceId)).ToList();
            if (notLoaded.Count > 0)
                findings.Add(string.Join(", ", notLoaded.Select(s => quoted(s.Name)).ToArray())
                    + " is in the configuration but not loaded in this project - load it in MEP Fabrication Settings");
            findings.Add("No loaded service has a name or abbreviation matching " + quoted(needle)
                + ". The loaded services are: "
                + (loaded.Count == 0 ? "none" : string.Join(", ", loaded.Where(s => s != null).Select(s => quoted(s.Name)).ToArray())));
        }
        else if (loaded.Count == 0)
            findings.Add("The configuration " + quoted(configurationName)
                + " is loaded but no service is - services are loaded in MEP Fabrication Settings");
    }

    // ---- Fabrication parts -------------------------------------------------
    var parts = new List<FabricationPart>();
    if (picked.Count > 0)
    {
        foreach (var e in picked)
        {
            var fp = e as FabricationPart;
            if (fp != null) { parts.Add(fp); continue; }
            var design = e is Duct || e is Pipe || e is FlexDuct || e is FlexPipe;
            notFabrication.Add(named(e) + ": not a fabrication part"
                + (design ? " - a design " + (e is Duct || e is FlexDuct ? "duct" : "pipe")
                          + "; Design to Fabrication is what turns one into fabrication parts" : ""));
        }
    }
    else if (wantParts || every)
    {
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(FabricationPart)).WhereElementIsNotElementType())
        {
            var fp = e as FabricationPart;
            if (fp != null) parts.Add(fp);
        }
    }

    if (wantParts)
    {
        fabricationParts = parts.Count;
        var partLines = new List<string>();
        var byService = new Dictionary<string, double>();
        var countByService = new Dictionary<string, int>();
        foreach (var fp in parts)
        {
            var serviceName = string.IsNullOrEmpty(fp.ServiceName) ? "no service" : fp.ServiceName;
            if (!countByService.ContainsKey(serviceName)) { countByService[serviceName] = 0; byService[serviceName] = 0; }
            countByService[serviceName]++;
            var straight = fp.IsAStraight();
            if (straight) byService[serviceName] += fp.CenterlineLength;
            partLines.Add("id " + fp.Id + " " + quoted(fp.Name)
                + ", service " + quoted(serviceName)
                + ", item number " + (string.IsNullOrEmpty(fp.ItemNumber) ? "none" : fp.ItemNumber)
                + (string.IsNullOrEmpty(fp.Size) ? "" : ", size " + fp.Size)
                + (straight ? ", straight " + mm(fp.CenterlineLength) + " mm long"
                            : fp.IsAHanger() ? ", hanger" : ", fitting")
                + (string.IsNullOrEmpty(fp.SpoolName) ? "" : ", spool " + quoted(fp.SpoolName)));
        }
        var totals = countByService.Keys.OrderBy(k => k).Select(k => quoted(k) + " " + countByService[k]
            + " part(s), straights " + mm(byService[k]) + " mm").ToArray();
        lines.Add("Fabrication parts " + (picked.Count > 0 ? "selected" : "in this model") + ": " + parts.Count
            + (totals.Length > 0 ? " - " + string.Join("; ", totals) : "")
            + (partLines.Count > 0 ? ". " + string.Join(". ", partLines.ToArray()) : ""));
        if (picked.Count > 0 && parts.Count == 0)
            findings.Add("None of the " + picked.Count + " element(s) selected is a fabrication part");
    }
    else if (every && picked.Count == 0)
        lines.Add("Fabrication parts in this model: " + parts.Count + " - ask for parts, or select some, to list them");

    report = string.Join(". ", lines.ToArray());
}
