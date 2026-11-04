// NOT STANDALONE. Assumes `doc`, `elements`, `service`, `palette` and `size`
// are in scope; leaves `changed`, `notChanged`, `notFabrication`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S CHANGE SERVICE AND CHANGE SIZE (Modify | Fabrication Parts) are one
// object, the fabrication network change service, unchanged 2020 to 2027
// (tools/api-surface --members, 2026-11-02): its constructor (Document),
// ChangeService(ISet<ElementId>, int serviceId, int paletteId) and the form
// with a bool that restricts Revit to that palette - the third int a group id
// to 2021, the same type - ChangeSize(ISet<ElementId>, ISet<
// FabricationPartSizeMap>), GetMapOfAllSizesForStraights(),
// GetElementsThatFailed() and GetStraightsThatWereNotChanged(). RevitAPI.xml
// says ChangeService and ChangeSize make the change themselves; ApplyChange is
// for changes set up beforehand, so it is not called.
//
// REACHED BY REFLECTION, AND WHY. Both classes live in the Fabrication
// namespace under Revit's DB namespace, which is not among the namespaces the
// executor imports (HeronFragmentImports), and naming it in full would put a
// vendor namespace in brain/, which check-structure refuses. So, as
// CONVERT_TO_FABRICATION does, each type is found beside FabricationPart in
// the same assembly and its members are called by name with the signatures
// read above. What Revit throws arrives wrapped in a
// TargetInvocationException, and Revit's own words are unwrapped from it.
//
// NEVER GUESSED. The service is matched to a service LOADED in this project by
// its whole name or abbreviation, ignoring capitals and nothing else - not
// named, not matched or matched twice changes nothing and the loaded services
// are listed. A palette named is matched by its name, else nothing changes and
// the service's palettes are listed; left out, the first palette not excluded
// is handed to Revit unrestricted. A size is width x depth or one diameter in
// mm, else nothing changes.
//
// HOW A SIZE REACHES REVIT. Change Size maps each straight's PRESENT size to a
// new one. Revit lists every straight size of every service
// (GetMapOfAllSizesForStraights); each straight handed in is matched in that
// list by its service, its shape and its end's width and depth (or diameter)
// within half a millimetre, and that entry is mapped to the size asked. A
// straight whose present size is not in Revit's list, or whose shape does not
// take the size given (one number for a rectangular straight), is named and
// not sent. Fittings go to Revit with the straights - which fittings Revit
// re-sizes to suit is Revit's, and is read back.
//
// NO FABRICATION CONFIGURATION: refused in Revit words - Systems > Fabrication
// > MEP Fabrication Settings - before anything is touched.
//
// ONLY FABRICATION PARTS ARE SENT. A design duct or pipe (Design to
// Fabrication first), a wall, equipment - each goes in `notFabrication` by
// name and is left as it was. ELEMENT OWNERSHIP IS A NORMAL OUTCOME (Article
// 12): a part borrowed by somebody else is named and never taken.
//
// WHAT REVIT DOES TO THE PARTS IS READ BACK, NOT ASSUMED. Nothing here deletes
// anything. The fabrication parts in the model are listed before; after a
// regenerate every part handed in is looked at again - gone (Revit re-made it)
// or still there with its service and size - and every fabrication part that
// was not in the model before is named as made by Revit.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER. Millimetres become
// feet by / 304.8 and lengths are said in mm (D-20).

var changed = new List<ElementId>();
var notChanged = new List<string>();
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
var wantService = asked(service);
var wantPalette = asked(palette);
var wantSize = asked(size);

// ---- what was handed in -------------------------------------------------------------------
var parts = new List<FabricationPart>();
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
    if (doc.IsWorkshared)
    {
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
        {
            notChanged.Add(named(e) + ": borrowed by " + (string.IsNullOrEmpty(owner) ? "another user" : owner)
                         + " - left alone; ask them to relinquish it");
            continue;
        }
    }
    parts.Add(fp);
}

// ---- the size asked -----------------------------------------------------------------------
double newWidth = 0, newDepth = 0;
var sizeRound = false;
var refused = "";
if (wantSize.Length > 0)
{
    var text = wantSize.ToLowerInvariant().Replace("mm", " ").Replace("Ø", " ").Replace("ø", " ")
        .Replace("dia", " ").Replace("×", "x").Replace("*", "x").Trim();
    var halves = text.Split('x');
    double a, b;
    if (halves.Length == 1 && double.TryParse(halves[0].Trim(), System.Globalization.NumberStyles.Float, invariant, out a) && a > 0)
    {
        newWidth = a / 304.8; newDepth = a / 304.8; sizeRound = true;
    }
    else if (halves.Length == 2
          && double.TryParse(halves[0].Trim(), System.Globalization.NumberStyles.Float, invariant, out a) && a > 0
          && double.TryParse(halves[1].Trim(), System.Globalization.NumberStyles.Float, invariant, out b) && b > 0)
    {
        newWidth = a / 304.8; newDepth = b / 304.8;
    }
    else
        refused = "The size " + quoted(wantSize) + " is not one this can read - give width x depth in mm for "
                + "rectangular or oval (\"400x300\"), or one diameter in mm for round (\"150\"). Nothing was changed";
}
var sizeWords = wantSize.Length == 0 ? "" : sizeRound ? mm(newWidth) + " mm diameter" : mm(newWidth) + " x " + mm(newDepth) + " mm";

// ---- the configuration, the service and the palette ----------------------------------------
var config = FabricationConfiguration.GetFabricationConfiguration(doc);
FabricationService chosen = null;
var paletteIndex = -1;
var paletteName = "";
if (refused.Length == 0 && config == null)
    refused = "This model has no fabrication configuration - choose one in Systems > Fabrication > MEP "
            + "Fabrication Settings and load a service. Nothing was changed";
else if (refused.Length == 0)
{
    var loaded = (config.GetAllLoadedServices() ?? new List<FabricationService>()).Where(s => s != null).ToList();
    var loadedNames = loaded.Count == 0 ? "none - services are loaded in MEP Fabrication Settings"
        : string.Join(", ", loaded.Select(s => quoted(s.Name)
            + (string.IsNullOrEmpty(s.Abbreviation) ? "" : " (" + s.Abbreviation + ")")).ToArray());
    Func<FabricationService, bool> same = s =>
        string.Equals((s.Name ?? "").Trim(), wantService, StringComparison.OrdinalIgnoreCase)
        || string.Equals((s.Abbreviation ?? "").Trim(), wantService, StringComparison.OrdinalIgnoreCase);

    if (wantService.Length == 0 && wantSize.Length == 0)
        refused = "Neither a service nor a size was named - Change Service needs the service to change to, Change "
                + "Size the new size. The services loaded in this project are: " + loadedNames + ". Nothing was changed";
    else if (wantService.Length > 0)
    {
        var matches = loaded.Where(same).ToList();
        if (matches.Count == 1)
            chosen = matches[0];
        else if (matches.Count > 1)
            refused = "More than one loaded service answers to " + quoted(wantService) + ": "
                    + string.Join(", ", matches.Select(s => quoted(s.Name)).ToArray())
                    + " - name it by its whole name. Nothing was changed";
        else
        {
            var inConfiguration = (config.GetAllServices() ?? new List<FabricationService>())
                .Where(s => s != null && same(s)).ToList();
            refused = (inConfiguration.Count > 0
                    ? quoted(inConfiguration[0].Name) + " is in the configuration but not loaded in this project - "
                      + "load it in Systems > Fabrication > MEP Fabrication Settings first"
                    : "No loaded service is called " + quoted(wantService))
                + ". The services loaded in this project are: " + loadedNames
                + ". The whole name or the abbreviation is needed. Nothing was changed";
        }
    }
    else if (wantPalette.Length > 0)
        refused = "A palette was named without a service - the palette belongs to the service changed to. Nothing was changed";

    if (chosen != null)
    {
#if REVIT2020 || REVIT2021
        var paletteCount = chosen.GroupCount;
#else
        var paletteCount = chosen.PaletteCount;
#endif
        var paletteNames = new List<string>();
        for (var p = 0; p < paletteCount; p++)
        {
#if REVIT2020 || REVIT2021
            var nameOf = chosen.GetGroupName(p);
            var excluded = chosen.IsGroupExcluded(p);
#else
            var nameOf = chosen.GetPaletteName(p);
            var excluded = chosen.IsPaletteExcluded(p);
#endif
            paletteNames.Add(quoted(nameOf) + (excluded ? " (excluded)" : ""));
            if (wantPalette.Length > 0)
            {
                if (paletteIndex < 0 && string.Equals((nameOf ?? "").Trim(), wantPalette, StringComparison.OrdinalIgnoreCase))
                { paletteIndex = p; paletteName = nameOf; }
            }
            else if (paletteIndex < 0 && !excluded)
            { paletteIndex = p; paletteName = nameOf; }
        }
        if (paletteIndex < 0)
            refused = (wantPalette.Length > 0
                    ? "The service " + quoted(chosen.Name) + " has no palette called " + quoted(wantPalette)
                    : "The service " + quoted(chosen.Name) + " has no palette that is not excluded")
                + ". Its palettes are: " + (paletteNames.Count == 0 ? "none" : string.Join(", ", paletteNames.ToArray()))
                + ". Nothing was changed";
    }
}

var fabricationNamespace = typeof(FabricationPart).Namespace + ".Fabrication";
var changerType = typeof(FabricationPart).Assembly.GetType(fabricationNamespace + ".FabricationNetworkChangeService");
var mapType = typeof(FabricationPart).Assembly.GetType(fabricationNamespace + ".FabricationPartSizeMap");
if (refused.Length == 0 && (changerType == null || mapType == null))
    refused = "This Revit's API has no fabrication Change Service where Revit 2020 to 2027 keep it - nothing was changed";

// ---- before -------------------------------------------------------------------------------
var before = new HashSet<ElementId>(new FilteredElementCollector(doc).OfClass(typeof(FabricationPart)).ToElementIds());
var label = new Dictionary<ElementId, string>();
var serviceBefore = new Dictionary<ElementId, int>();
var serviceNameBefore = new Dictionary<ElementId, string>();
var sizeBefore = new Dictionary<ElementId, string>();
foreach (var fp in parts)
{
    label[fp.Id] = named(fp);
    serviceBefore[fp.Id] = fp.ServiceId;
    serviceNameBefore[fp.Id] = fp.ServiceName;
    sizeBefore[fp.Id] = fp.Size ?? "";
}

// One call on a fresh change service; Revit's result in words, its failures read.
var said = new List<string>();
var failedIds = new HashSet<ElementId>();
var unchangedStraights = new HashSet<ElementId>();
var anySent = false;
Func<string, Func<object, object>, bool> run = (what, call) =>
{
    object changer = null;
    try
    {
        changer = Activator.CreateInstance(changerType, new object[] { doc });
        anySent = true;
        var result = call(changer);
        var word = result == null ? "" : result.ToString();
        if (word == "Success")
            said.Add(what + ": Revit reported success");
        else if (word == "PartialFailure")
        {
            said.Add(what + ": Revit reported a partial failure");
            var failed = changerType.GetMethod("GetElementsThatFailed", Type.EmptyTypes).Invoke(changer, null) as IEnumerable<ElementId>;
            foreach (var id in failed ?? new List<ElementId>()) failedIds.Add(id);
            var kept = changerType.GetMethod("GetStraightsThatWereNotChanged", Type.EmptyTypes).Invoke(changer, null) as IEnumerable<ElementId>;
            foreach (var id in kept ?? new List<ElementId>()) unchangedStraights.Add(id);
        }
        else if (word == "InvalidSelection")
            said.Add(what + ": Revit called the selection invalid and changed nothing");
        else if (word == "UserAborted")
            said.Add(what + ": Revit reported it was cancelled");
        else
            said.Add(what + ": Revit answered " + word);
        return true;
    }
    catch (System.Reflection.TargetInvocationException wrapped)
    {
        said.Add(what + ": Revit refused - " + (wrapped.InnerException ?? wrapped).Message);
        return false;
    }
    finally
    {
        var disposable = changer as IDisposable;
        if (disposable != null) disposable.Dispose();
    }
};

// ---- change service -----------------------------------------------------------------------
if (refused.Length == 0 && parts.Count > 0 && chosen != null)
{
    var ids = new HashSet<ElementId>(parts.Select(p => p.Id));
    var restrict = wantPalette.Length > 0;
    run("Change Service to " + quoted(chosen.Name) + ", palette " + quoted(paletteName) + (restrict ? " only" : ""),
        changer => restrict
            ? changerType.GetMethod("ChangeService", new[] { typeof(ISet<ElementId>), typeof(int), typeof(int), typeof(bool) })
                .Invoke(changer, new object[] { ids, chosen.ServiceId, paletteIndex, true })
            : changerType.GetMethod("ChangeService", new[] { typeof(ISet<ElementId>), typeof(int), typeof(int) })
                .Invoke(changer, new object[] { ids, chosen.ServiceId, paletteIndex }));
    doc.Regenerate();
}

// ---- change size --------------------------------------------------------------------------
if (refused.Length == 0 && parts.Count > 0 && wantSize.Length > 0)
{
    // What is in the model now: the parts handed in still there, and any part Revit made above.
    var now = new List<FabricationPart>();
    foreach (var fp in parts)
    {
        var again = doc.GetElement(fp.Id) as FabricationPart;
        if (again != null) now.Add(again);
    }
    foreach (var id in new FilteredElementCollector(doc).OfClass(typeof(FabricationPart)).ToElementIds())
        if (!before.Contains(id))
        {
            var made = doc.GetElement(id) as FabricationPart;
            if (made != null && !now.Contains(made)) now.Add(made);
        }

    object probe = null;
    var maps = new List<object>();
    try
    {
        probe = Activator.CreateInstance(changerType, new object[] { doc });
        var all = changerType.GetMethod("GetMapOfAllSizesForStraights", Type.EmptyTypes).Invoke(probe, null) as System.Collections.IEnumerable;
        foreach (var m in all ?? new object[0]) if (m != null) maps.Add(m);
    }
    catch (System.Reflection.TargetInvocationException wrapped)
    {
        said.Add("Change Size: Revit would not list its straight sizes - " + (wrapped.InnerException ?? wrapped).Message);
    }
    finally
    {
        var disposable = probe as IDisposable;
        if (disposable != null) disposable.Dispose();
    }

    Func<object, string, object> read = (m, property) => mapType.GetProperty(property).GetValue(m, null);
    var chosenMaps = new List<object>();
    var straights = 0;
    foreach (var fp in now)
    {
        if (!fp.IsAStraight()) continue;
        straights++;
        Connector end = null;
        foreach (Connector c in fp.ConnectorManager.Connectors) { end = c; break; }
        if (end == null)
        {
            notChanged.Add(named(fp) + ": a straight with no end Revit reports - its size could not be read, so it was not sent");
            continue;
        }
        var round = end.Shape == ConnectorProfileType.Round;
        if (round != sizeRound)
        {
            notChanged.Add(named(fp) + ": a " + end.Shape.ToString().ToLowerInvariant() + " straight - "
                + (round ? "it takes one diameter" : "it takes width x depth") + ", and " + quoted(wantSize) + " was given. Left at " + (fp.Size ?? ""));
            continue;
        }
        var width = round ? end.Radius * 2 : end.Width;
        var depth = round ? end.Radius * 2 : end.Height;
        object hit = null;
        foreach (var m in maps)
        {
            if ((int)read(m, "ServiceId") != fp.ServiceId) continue;
            if ((ConnectorProfileType)read(m, "ProfileType") != end.Shape) continue;
            if (Math.Abs((double)read(m, "WidthDiameter") - width) > 0.5 / 304.8) continue;
            if (!round && Math.Abs((double)read(m, "Depth") - depth) > 0.5 / 304.8) continue;
            hit = m; break;
        }
        if (hit == null)
        {
            notChanged.Add(named(fp) + ": its present size " + (fp.Size ?? "") + " on " + quoted(fp.ServiceName)
                + " is not among the straight sizes Revit lists for that service, so it was not sent");
            continue;
        }
        if (!chosenMaps.Contains(hit)) chosenMaps.Add(hit);
    }
    if (straights == 0)
        notChanged.Add("No straight among the parts - Change Size changes straights, and the fittings with them. No size was changed");
    else if (chosenMaps.Count > 0)
    {
        var mapSet = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(mapType));
        var add = mapSet.GetType().GetMethod("Add");
        foreach (var m in chosenMaps)
        {
            var originalDepth = (double)read(m, "Depth");
            var originalWidth = (double)read(m, "WidthDiameter");
            mapType.GetProperty("MappedServiceId").SetValue(m, read(m, "ServiceId"), null);
            mapType.GetProperty("MappedProfileType").SetValue(m, read(m, "ProfileType"), null);
            mapType.GetProperty("MappedWidthDiameter").SetValue(m, newWidth, null);
            // A round entry keeps Revit's own convention for its depth: equal to its diameter, or as it was.
            mapType.GetProperty("MappedDepth").SetValue(m,
                sizeRound ? (Math.Abs(originalDepth - originalWidth) < 1e-9 ? newWidth : originalDepth) : newDepth, null);
            add.Invoke(mapSet, new[] { m });
        }
        var ids = new HashSet<ElementId>(now.Select(p => p.Id));
        run("Change Size to " + sizeWords + " (" + chosenMaps.Count + " present straight size(s) mapped)",
            changer => changerType.GetMethod("ChangeSize", new[] { typeof(ISet<ElementId>), typeof(ISet<>).MakeGenericType(mapType) })
                .Invoke(changer, new object[] { ids, mapSet }));
        doc.Regenerate();
    }
}

// ---- read back ----------------------------------------------------------------------------
var lines = new List<string>();
var gone = new List<string>();
var madeLines = new List<string>();
Func<FabricationPart, string> describe = fp =>
    "on " + quoted(fp.ServiceName) + (string.IsNullOrEmpty(fp.Size) ? "" : ", " + fp.Size)
    + (fp.IsAStraight() ? ", straight " + mm(fp.CenterlineLength) + " mm" : "");
if (anySent)
{
    foreach (var fp0 in parts)
    {
        var id = fp0.Id;
        var fp = doc.GetElement(id) as FabricationPart;
        if (fp == null) { gone.Add(label[id]); continue; }
        var serviceMoved = fp.ServiceId != serviceBefore[id];
        var sizeMoved = (fp.Size ?? "") != sizeBefore[id];
        var was = "was on " + quoted(serviceNameBefore[id]) + (sizeBefore[id].Length == 0 ? "" : ", " + sizeBefore[id]);
        if (serviceMoved || sizeMoved)
        {
            changed.Add(id);
            lines.Add(label[id] + " " + was + ", now " + describe(fp));
        }
        else
        {
            var why = failedIds.Contains(id) ? "Revit reported it failed - no matching part in the service, a size out of range or a connection it could not make"
                    : unchangedStraights.Contains(id) ? "Revit left this straight unchanged - no matching part in the service or the size out of range"
                    : "it reads the same after";
            if (!notChanged.Any(n => n.StartsWith(label[id])))
                notChanged.Add(label[id] + ": " + why + " - still " + describe(fp));
        }
    }
    foreach (var id in new FilteredElementCollector(doc).OfClass(typeof(FabricationPart)).ToElementIds())
    {
        if (before.Contains(id)) continue;
        var fp = doc.GetElement(id) as FabricationPart;
        if (fp == null) continue;
        changed.Add(id);
        madeLines.Add("id " + id + " " + quoted(fp.Name) + " " + describe(fp));
    }
}

// ---- the answer ---------------------------------------------------------------------------
var asking = (chosen != null ? "the service " + quoted(chosen.Name) : "")
           + (chosen != null && wantSize.Length > 0 ? " and " : "")
           + (wantSize.Length > 0 ? "the size " + sizeWords : "");
if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
else if (given == 0)
    report = "Nothing was handed in - select the fabrication parts to change. Nothing was changed";
else if (parts.Count == 0)
    report = "None of the " + given + " element(s) given is a fabrication part that can be changed - "
           + string.Join("; ", notFabrication.Concat(notChanged).ToArray()) + ". Nothing was changed";
else
    report = "Asked for " + asking + " on " + parts.Count + " fabrication part(s). "
           + (said.Count > 0 ? string.Join(". ", said.ToArray()) : "Nothing was sent to Revit")
           + ". " + lines.Count + " part(s) read differently after"
           + (lines.Count > 0 ? ": " + string.Join("; ", lines.ToArray()) : "")
           + ". " + gone.Count + " of the parts handed in are no longer in the model (Revit re-made them)"
           + (gone.Count > 0 ? ": " + string.Join("; ", gone.ToArray()) : "")
           + ". Revit made " + madeLines.Count + " new fabrication part(s)"
           + (madeLines.Count > 0 ? ": " + string.Join("; ", madeLines.ToArray()) : "")
           + ". " + notChanged.Count + " not changed"
           + (notChanged.Count > 0 ? " - " + string.Join("; ", notChanged.ToArray()) : "")
           + ". " + notFabrication.Count + " left alone as not fabrication parts"
           + (notFabrication.Count > 0 ? " - " + string.Join("; ", notFabrication.ToArray()) : "");
