// NOT STANDALONE. Assumes `doc`, `name`, `material`, `temperatureRating`,
// `insulation`, `maxSize` and `basedOn` are in scope, and leaves `added`,
// `before`, `after`, `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16). The new
// wire type is made inside a SubTransaction, so a value Revit will not take
// rolls the whole wire type back rather than leaving half of one behind.
//
// WHAT IT DOES. Manage > MEP Settings > Electrical Settings > Wiring Types:
// adds ONE wire type with the conductor material, temperature rating and
// insulation named. Nothing existing is changed, renamed or removed: a name
// already present, in any capitals, is SKIPPED and named. The wire types are
// read before and after, each with its values, and both are handed back.
//
// NAMES ARE MATCHED EXACTLY (D-33). A material, rating, insulation, size or
// base wire type that is not in the model by that exact name is refused, and
// the names the model has are listed - one that differs only in capitals is
// named, never taken.
//
// WHAT THE MODELLER DID NOT NAME. Insulation left out is the one insulation
// the rating offers when it offers exactly one, else refused with the list.
// The neutral multiplier, neutral required, neutral size and conduit are
// copied from `basedOn` - left out, the first wire type by name - and the
// report names which one, so it can be changed in the dialog. Max size left
// out is the base's when the new rating has that size, else the rating's
// largest by ampacity, named.
//
// RELEASES. To 2025, ElectricalSetting.WireMaterialTypes ->
// WireMaterialType.TemperatureRatings -> TemperatureRatingType.InsulationTypes
// and WireSizes, and ElectricalSetting.AddWireType(string, WireMaterialType,
// TemperatureRatingType, InsulationType, WireSize, double, bool, NeutralMode,
// WireConduitType). From 2026 (#if REVIT2026_OR_GREATER) those are obsolete
// (2026) or gone (2027): ConductorMaterial, TemperatureRating,
// InsulationMaterial and ConductorSize are flat lists read with their own
// Get...Ids and Get... statics, and the wire type is WireType.Duplicate of the
// base with WireMaterial, TemperatureRating and Insulation set as ElementIds
// and MaxSize as a size name. WireType.Conduit, NeutralMultiplier,
// NeutralRequired, NeutralSize and IsInUse, ElementType.Duplicate and
// SubTransaction are present 2020 to 2027 (tools/api-surface --members,
// 2026-11-02).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var added = 0;
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> num = v => Math.Round(v, 2).ToString("0.##", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
Func<string, string> given = s =>
{
    var t = (s ?? "").Trim();
    return string.Equals(t, "none", StringComparison.OrdinalIgnoreCase) ? "" : t;
};
var wantName = given(name);
var wantMaterial = given(material);
var wantRating = given(temperatureRating);
var wantInsulation = given(insulation);
var wantSize = given(maxSize);
var wantBase = given(basedOn);

// An exact name, or a refusal that lists what the model has.
Func<string, string, List<string>, bool> exact = (what, wanted, have) =>
{
    if (have.Any(h => string.Equals(h, wanted, StringComparison.Ordinal))) return true;
    var near = have.FirstOrDefault(h => string.Equals(h, wanted, StringComparison.OrdinalIgnoreCase));
    refused.Add("No " + what + " is named " + quoted(wanted) + ". "
        + (near != null ? quoted(near) + " differs only in capitals - Heron does not take one for the other. " : "")
        + "The ones here are: " + (have.Count == 0 ? "none" : string.Join(", ", have.Select(quoted))) + ".");
    return false;
};

ElectricalSetting settings = null;
try { settings = ElectricalSetting.GetElectricalSettings(doc); }
catch (Exception ex) { refused.Add("Electrical Settings could not be read: " + ex.Message); }
if (settings == null && refused.Count == 0)
    refused.Add("This model has no Electrical Settings" + (doc.IsFamilyDocument ? " - it is a family; open a project." : "."));

#if REVIT2026_OR_GREATER
Func<ElementId, string> materialName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    try { var m = ConductorMaterial.GetConductorMaterial(doc, id); return m == null ? "none" : m.Name; } catch (Exception) { return "none"; }
};
Func<ElementId, string> ratingName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    try { var r = TemperatureRating.GetTemperatureRating(doc, id); return r == null ? "none" : r.Name; } catch (Exception) { return "none"; }
};
Func<ElementId, string> insulationName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    try { var i = InsulationMaterial.GetInsulationMaterial(doc, id); return i == null ? "none" : i.Name; } catch (Exception) { return "none"; }
};
#endif

Func<WireType, string> line = w =>
{
    string mat = "", rating = "", ins = "", size = "";
    try
    {
#if REVIT2026_OR_GREATER
        mat = materialName(w.WireMaterial);
        rating = ratingName(w.TemperatureRating);
        ins = insulationName(w.Insulation);
        size = w.MaxSize ?? "";
#else
        mat = w.WireMaterial == null ? "none" : w.WireMaterial.Name;
        rating = w.TemperatureRating == null ? "none" : w.TemperatureRating.Name;
        ins = w.Insulation == null ? "none" : w.Insulation.Name;
        size = w.MaxSize == null ? "" : w.MaxSize.Size;
#endif
    }
    catch (Exception) { }
    var conduit = "";
    try { conduit = w.Conduit == null ? "" : w.Conduit.Name; } catch (Exception) { }
    return quoted(w.Name) + " (id " + w.Id.ToString() + ") " + mat + ", " + rating + ", " + ins
        + (size.Length > 0 ? ", max size " + size : "")
        + ", neutral multiplier " + num(w.NeutralMultiplier) + (w.NeutralRequired ? ", neutral required" : "")
        + ", neutral size " + (w.NeutralSize == NeutralMode.UnbalancedCurrent ? "by unbalanced current" : "as the hot conductor")
        + (conduit.Length > 0 ? ", conduit " + conduit : "");
};
var wires = new List<WireType>();
Action readWires = () =>
{
    wires.Clear();
    if (settings == null) return;
    foreach (WireType w in settings.WireTypes) if (w != null) wires.Add(w);
    wires.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
};

// ---- Refusals before anything is touched -----------------------------------
if (refused.Count == 0)
{
    if (wantName.Length == 0) refused.Add("No name was given for the new wire type - it is never made up.");
    if (wantMaterial.Length == 0) refused.Add("No conductor material was given - copper, aluminium or another the model has, by its exact name. Never guessed.");
    if (wantRating.Length == 0) refused.Add("No temperature rating was given - by its exact name, as Electrical Settings shows it. Never guessed.");
}
if (settings != null) { readWires(); before.AddRange(wires.Select(line)); }

string taken = null;
if (refused.Count == 0)
{
    taken = wires.Select(w => w.Name).FirstOrDefault(n => string.Equals(n, wantName, StringComparison.OrdinalIgnoreCase));
    if (taken != null) skipped.Add("A wire type is already named " + quoted(taken) + " - left as it is, nothing added.");
}

WireType baseType = null;
if (refused.Count == 0 && taken == null)
{
    if (wantBase.Length > 0)
    {
        if (exact("wire type", wantBase, wires.Select(w => w.Name).ToList()))
            baseType = wires.First(w => string.Equals(w.Name, wantBase, StringComparison.Ordinal));
    }
    else if (wires.Count == 0)
        refused.Add("The model has no wire type to copy the neutral and conduit settings from - add one in Electrical Settings first.");
    else
        baseType = wires[0];
}

WireType made = null;
var copiedSize = "";
if (refused.Count == 0 && taken == null && baseType != null)
{
#if REVIT2026_OR_GREATER
    Func<IList<ElementId>, Func<ElementId, string>, List<KeyValuePair<ElementId, string>>> named = (ids, nameOf) =>
    {
        var list = new List<KeyValuePair<ElementId, string>>();
        foreach (var id in ids ?? new List<ElementId>()) list.Add(new KeyValuePair<ElementId, string>(id, nameOf(id)));
        list.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        return list;
    };
    var materials = named(ConductorMaterial.GetConductorMaterialIds(doc), materialName);
    var ratings = named(TemperatureRating.GetTemperatureRatingIds(doc), ratingName);
    var insulations = named(InsulationMaterial.GetInsulationMaterialIds(doc), insulationName);
    var sizes = named(ConductorSize.GetConductorSizeIds(doc), id =>
    {
        try { var s = ConductorSize.GetConductorSize(doc, id); return s == null ? "" : s.Name; } catch (Exception) { return ""; }
    });
    ElementId materialId = null, ratingId = null, insulationId = null;
    if (exact("conductor material", wantMaterial, materials.Select(m => m.Value).ToList()))
        materialId = materials.First(m => m.Value == wantMaterial).Key;
    if (exact("temperature rating", wantRating, ratings.Select(r => r.Value).ToList()))
        ratingId = ratings.First(r => r.Value == wantRating).Key;
    if (wantInsulation.Length > 0)
    {
        if (exact("insulation", wantInsulation, insulations.Select(i => i.Value).ToList()))
            insulationId = insulations.First(i => i.Value == wantInsulation).Key;
    }
    else if (insulations.Count == 1) insulationId = insulations[0].Key;
    else refused.Add("No insulation was given, and the model has " + insulations.Count + " - name one: "
        + (insulations.Count == 0 ? "none" : string.Join(", ", insulations.Select(i => quoted(i.Value)))) + ". Never guessed.");
    var sizeText = wantSize;
    if (sizeText.Length > 0) exact("conductor size", sizeText, sizes.Select(s => s.Value).ToList());
    else { sizeText = baseType.MaxSize ?? ""; copiedSize = sizeText; }

    if (refused.Count == 0)
    {
        var sub = new SubTransaction(doc);
        try
        {
            sub.Start();
            made = baseType.Duplicate(wantName) as WireType;
            if (made == null) throw new InvalidOperationException("Revit made no wire type named " + quoted(wantName) + ".");
            made.WireMaterial = materialId;
            made.TemperatureRating = ratingId;
            made.Insulation = insulationId;
            if (sizeText.Length > 0) made.MaxSize = sizeText;
            sub.Commit();
        }
        catch (Exception ex)
        {
            try { if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack(); } catch (Exception) { }
            made = null;
            refused.Add("Revit would not add wire type " + quoted(wantName) + ": " + ex.Message + " Nothing was kept.");
        }
    }
#else
    var materials = new List<WireMaterialType>();
    foreach (WireMaterialType m in settings.WireMaterialTypes) if (m != null) materials.Add(m);
    WireMaterialType useMaterial = null;
    TemperatureRatingType useRating = null;
    InsulationType useInsulation = null;
    WireSize useSize = null;
    if (exact("conductor material", wantMaterial, materials.Select(m => m.Name).ToList()))
        useMaterial = materials.First(m => m.Name == wantMaterial);
    if (useMaterial != null)
    {
        var ratings = new List<TemperatureRatingType>();
        foreach (TemperatureRatingType r in useMaterial.TemperatureRatings) if (r != null) ratings.Add(r);
        if (exact("temperature rating for " + useMaterial.Name, wantRating, ratings.Select(r => r.Name).ToList()))
            useRating = ratings.First(r => r.Name == wantRating);
    }
    if (useRating != null)
    {
        var insulations = new List<InsulationType>();
        foreach (InsulationType i in useRating.InsulationTypes) if (i != null) insulations.Add(i);
        if (wantInsulation.Length > 0)
        {
            if (exact("insulation for " + useMaterial.Name + " " + useRating.Name, wantInsulation, insulations.Select(i => i.Name).ToList()))
                useInsulation = insulations.First(i => i.Name == wantInsulation);
        }
        else if (insulations.Count == 1) useInsulation = insulations[0];
        else refused.Add("No insulation was given, and " + useMaterial.Name + " " + useRating.Name + " has " + insulations.Count
            + " - name one: " + (insulations.Count == 0 ? "none" : string.Join(", ", insulations.Select(i => quoted(i.Name)))) + ". Never guessed.");

        var sizes = new List<WireSize>();
        foreach (WireSize s in useRating.WireSizes) if (s != null) sizes.Add(s);
        if (wantSize.Length > 0)
        {
            if (exact("wire size for " + useMaterial.Name + " " + useRating.Name, wantSize, sizes.Select(s => s.Size).ToList()))
                useSize = sizes.First(s => s.Size == wantSize);
        }
        else
        {
            var baseSize = baseType.MaxSize == null ? "" : baseType.MaxSize.Size;
            useSize = sizes.FirstOrDefault(s => s.Size == baseSize);
            if (useSize == null && sizes.Count > 0)
            {
                useSize = sizes.OrderByDescending(s => s.Ampacity).First();
                findings.Add("Max size " + quoted(useSize.Size) + " is the largest " + useRating.Name + " has - "
                    + quoted(baseType.Name) + "'s " + quoted(baseSize) + " is not one of its sizes.");
            }
            if (useSize == null) refused.Add(useMaterial.Name + " " + useRating.Name + " has no wire sizes to take a max size from.");
            else copiedSize = useSize.Size;
        }
    }

    if (refused.Count == 0)
    {
        var sub = new SubTransaction(doc);
        try
        {
            sub.Start();
            made = settings.AddWireType(wantName, useMaterial, useRating, useInsulation, useSize,
                baseType.NeutralMultiplier, baseType.NeutralRequired, baseType.NeutralSize, baseType.Conduit);
            if (made == null) throw new InvalidOperationException("Revit made no wire type named " + quoted(wantName) + ".");
            sub.Commit();
        }
        catch (Exception ex)
        {
            try { if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack(); } catch (Exception) { }
            made = null;
            refused.Add("Revit would not add wire type " + quoted(wantName) + ": " + ex.Message + " Nothing was kept.");
        }
    }
#endif
}

// ---- Read back -------------------------------------------------------------
if (settings != null) { readWires(); after.AddRange(wires.Select(line)); }
if (made != null)
{
    var back = wires.FirstOrDefault(w => w.Id == made.Id);
    if (back == null) refused.Add("The new wire type " + quoted(wantName) + " is not in the Wiring Types list read back.");
    else
    {
        added = 1;
        findings.Add("Added wire type " + line(back) + ". Its neutral settings and conduit are copied from " + quoted(baseType.Name)
            + (copiedSize.Length > 0 ? ", and its max size " + quoted(copiedSize) + " was not given" : "")
            + " - change them in Electrical Settings > Wiring Types if they are not wanted.");
    }
}
findings.Insert(0, (added > 0 ? "Added 1 wire type" : "Added nothing - "
    + (skipped.Count > 0 ? "the name is already here, skipped" : refused.Count > 0 ? "refused" : "no wire type"))
    + ". Wire types: " + before.Count + " before, " + after.Count + " after.");
findings.AddRange(skipped);
findings.AddRange(refused);
