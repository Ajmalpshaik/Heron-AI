// NOT STANDALONE. Assumes `doc`, `elements`, `button`, `service`, `spacing`
// and `attachToStructure` are in scope; leaves `hangers`, `notPlaced`,
// `notFabrication`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S FABRICATION HANGERS: a hanger button of a service placed on a
// fabrication straight by FabricationPart.CreateHanger(Document,
// FabricationServiceButton, ElementId hostId, Connector hostConnector, double
// distance, bool attachToStructure) - the form that finds the button's
// condition for the host itself - present and unchanged 2020 to 2027
// (tools/api-surface --members, 2026-11-02). RevitAPI.xml: the distance is in
// feet from the connector given; hangers go only on straight, horizontal
// segments; the call throws for a distance out of range or a host it cannot
// hang. FabricationPart and its button classes are in Revit's DB namespace,
// which the executor imports, so nothing here needs reflection.
//
// THE BUTTON IS NEVER GUESSED. It is matched by its exact name, ignoring
// capitals and nothing else, among the HANGER buttons of the service: each
// straight's own service, or the loaded service named. Not named or not
// found: nothing is placed and that service's hanger buttons are listed. A
// button of that name that is not a hanger is refused by name. Two hanger
// buttons of the same name on different palettes: the first not excluded is
// used and which palette is said.
//
// WHERE THEY GO. Each straight's length L is read; with no spacing, one hanger
// at L/2. With a spacing s, n = the fewest hangers with no more than s
// between them, n = ceil(L / s), at (i + 1/2) L / n from the straight's first
// end - so the gap from the last hanger of one straight to the first of the
// next is no more than s either.
//
// NO FABRICATION CONFIGURATION: refused in Revit words - Systems > Fabrication
// > MEP Fabrication Settings - before anything is touched.
//
// ONLY FABRICATION STRAIGHTS ARE HUNG. A design duct or pipe (Design to
// Fabrication first), a wall - `notFabrication`. A fitting or a hanger handed
// in - `notPlaced`, left as it was. A straight Revit refuses (sloped, vertical,
// out of range) is named with Revit's words. ELEMENT OWNERSHIP IS A NORMAL
// OUTCOME (Article 12): a straight borrowed by somebody else is named and
// never taken.
//
// READ BACK after a regenerate: each hanger as a fabrication hanger, with its
// name, service, host read from its hosted info, and distance along the host.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER. Millimetres become
// feet by / 304.8 and lengths are said in mm (D-20).

var hangers = new List<ElementId>();
var notPlaced = new List<string>();
var notFabrication = new List<string>();
var findings = new List<string>();
string report = "";

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> named = e =>
    (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
Func<string, string> asked = t =>
{
    var s = (t ?? "").Trim();
    return string.Equals(s, "none", StringComparison.OrdinalIgnoreCase) ? "" : s;
};
var wantButton = asked(button);
var wantService = asked(service);
var spacingFeet = spacing > 0 ? spacing / 304.8 : 0;

// ---- what was handed in -------------------------------------------------------------------
var straights = new List<FabricationPart>();
var seen = new HashSet<ElementId>();
var given = 0;
foreach (var e in elements ?? new List<Element>())
{
    if (e == null || !seen.Add(e.Id)) continue;
    given++;
    var fp = e as FabricationPart;
    if (fp == null)
    {
        var design = e is Duct || e is Pipe;
        notFabrication.Add(named(e) + ": not a fabrication part"
            + (design ? " - a design " + (e is Duct ? "duct" : "pipe") + "; Design to Fabrication makes it fabrication first" : "")
            + ". Left as it is");
        continue;
    }
    if (fp.IsAHanger()) { notPlaced.Add(named(e) + ": already a hanger - left as it is"); continue; }
    if (!fp.IsAStraight()) { notPlaced.Add(named(e) + ": a fitting, not a straight - hangers are put on straights here. Left as it is"); continue; }
    if (doc.IsWorkshared)
    {
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
        {
            notPlaced.Add(named(e) + ": borrowed by " + (string.IsNullOrEmpty(owner) ? "another user" : owner)
                        + " - left alone; ask them to relinquish it");
            continue;
        }
    }
    straights.Add(fp);
}

// ---- the configuration and the service the button comes from -------------------------------
var refused = "";
var config = FabricationConfiguration.GetFabricationConfiguration(doc);
FabricationService serviceNamed = null;
if (config == null)
    refused = "This model has no fabrication configuration - choose one in Systems > Fabrication > MEP "
            + "Fabrication Settings and load a service. No hanger was placed";
else if (spacing < 0)
    refused = "A spacing of " + spacing.ToString("0", invariant) + " mm cannot be used - give the most from one hanger "
            + "to the next in mm, or leave it out for one in the middle. No hanger was placed";
else if (wantService.Length > 0)
{
    var loaded = (config.GetAllLoadedServices() ?? new List<FabricationService>()).Where(s => s != null).ToList();
    var loadedNames = loaded.Count == 0 ? "none - services are loaded in MEP Fabrication Settings"
        : string.Join(", ", loaded.Select(s => quoted(s.Name)
            + (string.IsNullOrEmpty(s.Abbreviation) ? "" : " (" + s.Abbreviation + ")")).ToArray());
    Func<FabricationService, bool> same = s =>
        string.Equals((s.Name ?? "").Trim(), wantService, StringComparison.OrdinalIgnoreCase)
        || string.Equals((s.Abbreviation ?? "").Trim(), wantService, StringComparison.OrdinalIgnoreCase);
    var matches = loaded.Where(same).ToList();
    if (matches.Count == 1)
        serviceNamed = matches[0];
    else if (matches.Count > 1)
        refused = "More than one loaded service answers to " + quoted(wantService) + ": "
                + string.Join(", ", matches.Select(s => quoted(s.Name)).ToArray())
                + " - name it by its whole name. No hanger was placed";
    else
    {
        var inConfiguration = (config.GetAllServices() ?? new List<FabricationService>())
            .Where(s => s != null && same(s)).ToList();
        refused = (inConfiguration.Count > 0
                ? quoted(inConfiguration[0].Name) + " is in the configuration but not loaded in this project - "
                  + "load it in Systems > Fabrication > MEP Fabrication Settings first"
                : "No loaded service is called " + quoted(wantService))
            + ". The services loaded in this project are: " + loadedNames
            + ". The whole name or the abbreviation is needed. No hanger was placed";
    }
}

// The hanger button of one service, or why there is none - with that service's hanger buttons.
var buttonOf = new Dictionary<int, FabricationServiceButton>();
var whyNot = new Dictionary<int, string>();
var paletteOf = new Dictionary<int, string>();
Action<FabricationService> find = s =>
{
    if (s == null || buttonOf.ContainsKey(s.ServiceId) || whyNot.ContainsKey(s.ServiceId)) return;
#if REVIT2020 || REVIT2021
    var paletteCount = s.GroupCount;
#else
    var paletteCount = s.PaletteCount;
#endif
    var hangerNames = new List<string>();
    FabricationServiceButton hit = null;
    var hitPalette = "";
    var sameNameNotHanger = false;
    var sameNameCount = 0;
    for (var p = 0; p < paletteCount; p++)
    {
#if REVIT2020 || REVIT2021
        var paletteName = s.GetGroupName(p);
#else
        var paletteName = s.GetPaletteName(p);
#endif
        var count = s.GetButtonCount(p);
        for (var b = 0; b < count; b++)
        {
            var candidate = s.GetButton(p, b);
            if (candidate == null) continue;
            var isName = wantButton.Length > 0
                && string.Equals((candidate.Name ?? "").Trim(), wantButton, StringComparison.OrdinalIgnoreCase);
            if (!candidate.IsAHanger)
            {
                if (isName) sameNameNotHanger = true;
                continue;
            }
            hangerNames.Add(quoted(candidate.Name) + " (palette " + quoted(paletteName) + ")");
            if (!isName) continue;
            sameNameCount++;
            if (hit == null || (hit.IsExcluded() && !candidate.IsExcluded()))
            { hit = candidate; hitPalette = paletteName; }
        }
    }
    var list = hangerNames.Count == 0 ? "none" : string.Join(", ", hangerNames.ToArray());
    if (hit != null)
    {
        buttonOf[s.ServiceId] = hit;
        paletteOf[s.ServiceId] = hitPalette;
        if (sameNameCount > 1)
            findings.Add(sameNameCount + " hanger buttons on " + quoted(s.Name) + " are called " + quoted(wantButton)
                       + " - the one on palette " + quoted(hitPalette) + " was used");
    }
    else if (wantButton.Length == 0)
        whyNot[s.ServiceId] = "no hanger button was named. The hanger buttons of " + quoted(s.Name) + " are: " + list;
    else if (sameNameNotHanger)
        whyNot[s.ServiceId] = "the button " + quoted(wantButton) + " on " + quoted(s.Name) + " is not a hanger. Its hanger buttons are: " + list;
    else
        whyNot[s.ServiceId] = quoted(s.Name) + " has no hanger button called " + quoted(wantButton) + ". Its hanger buttons are: " + list;
};

// ---- place --------------------------------------------------------------------------------
var made = new List<ElementId>();
var plan = new Dictionary<ElementId, string>();
var hostOf = new Dictionary<ElementId, ElementId>();
var distanceOf = new Dictionary<ElementId, double>();
if (refused.Length == 0)
{
    foreach (var fp in straights)
    {
        var from = serviceNamed ?? config.GetService(fp.ServiceId);
        if (from == null)
        {
            notPlaced.Add(named(fp) + ": its service " + quoted(fp.ServiceName) + " is not in the configuration - no hanger");
            continue;
        }
        find(from);
        if (!buttonOf.ContainsKey(from.ServiceId))
        {
            notPlaced.Add(named(fp) + ": " + whyNot[from.ServiceId]);
            continue;
        }
        var hanger = buttonOf[from.ServiceId];
        Connector end = null;
        foreach (Connector c in fp.ConnectorManager.Connectors) { end = c; break; }
        var length = fp.CenterlineLength;
        if (end == null || length <= 0)
        {
            notPlaced.Add(named(fp) + ": a straight with no end or no length Revit reports - no hanger");
            continue;
        }
        var n = spacingFeet > 0 ? Math.Max(1, (int)Math.Ceiling(length / spacingFeet - 1e-9)) : 1;
        var placedHere = 0;
        var refusedHere = new List<string>();
        for (var i = 0; i < n; i++)
        {
            var distance = (i + 0.5) * length / n;
            try
            {
                var h = FabricationPart.CreateHanger(doc, hanger, fp.Id, end, distance, attachToStructure);
                if (h == null) { refusedHere.Add("at " + mm(distance) + " mm Revit made nothing"); continue; }
                made.Add(h.Id);
                hostOf[h.Id] = fp.Id;
                distanceOf[h.Id] = distance;
                placedHere++;
            }
            catch (Exception ex)
            {
                refusedHere.Add("at " + mm(distance) + " mm Revit refused - " + ex.Message);
            }
        }
        plan[fp.Id] = named(fp) + ", " + mm(length) + " mm long: " + placedHere + " of " + n + " hanger(s) from "
                    + quoted(hanger.Name) + " on " + quoted(from.Name);
        if (refusedHere.Count > 0)
            notPlaced.Add(named(fp) + ": " + placedHere + " of " + n + " hanger(s) placed - " + string.Join("; ", refusedHere.ToArray()));
    }
}

// ---- read back ----------------------------------------------------------------------------
var lines = new List<string>();
if (made.Count > 0)
{
    doc.Regenerate();
    foreach (var id in made)
    {
        var h = doc.GetElement(id) as FabricationPart;
        if (h == null) { findings.Add("hanger id " + id + " Revit returned is not in the model after a regenerate"); continue; }
        hangers.Add(id);
        var info = h.GetHostedInfo();
        var host = info == null ? ElementId.InvalidElementId : info.HostId;
        lines.Add("id " + id + " " + quoted(h.Name) + " on " + quoted(h.ServiceName)
            + ", " + mm(distanceOf[id]) + " mm along id " + hostOf[id]
            + (host == hostOf[id] ? "" : host == ElementId.InvalidElementId ? " - Revit reports it hosted on nothing" : " - Revit reports it hosted on id " + host)
            + (h.IsAHanger() ? "" : " - Revit does not call it a hanger"));
    }
}

// ---- the answer ---------------------------------------------------------------------------
if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
else if (given == 0)
    report = "Nothing was handed in - select the fabrication straights to hang. No hanger was placed";
else if (straights.Count == 0)
    report = "None of the " + given + " element(s) given is a fabrication straight that can take a hanger - "
           + string.Join("; ", notFabrication.Concat(notPlaced).ToArray()) + ". No hanger was placed";
else
    report = (spacingFeet > 0 ? "Hangers at no more than " + mm(spacingFeet) + " mm apart" : "One hanger in the middle")
           + " of each of " + straights.Count + " fabrication straight(s)"
           + (attachToStructure ? ", attached to the structure above" : "") + ". "
           + (plan.Count > 0 ? string.Join("; ", plan.Values.ToArray()) + ". " : "")
           + hangers.Count + " hanger(s) made" + (lines.Count > 0 ? ": " + string.Join("; ", lines.ToArray()) : "")
           + ". " + notPlaced.Count + " not placed"
           + (notPlaced.Count > 0 ? " - " + string.Join("; ", notPlaced.ToArray()) : "")
           + ". " + notFabrication.Count + " left alone as not fabrication parts"
           + (notFabrication.Count > 0 ? " - " + string.Join("; ", notFabrication.ToArray()) : "");
