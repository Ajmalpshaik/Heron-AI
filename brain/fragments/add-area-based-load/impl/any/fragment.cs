// NOT STANDALONE. Assumes `doc`, `elements`, `loadType`, `density`,
// `loadClassification` and `name` are in scope, and leaves `typesAdded`,
// `loadsAdded`, `areasAssigned`, `before`, `after`, `skipped`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Revit 2023's electrical analytical model, Analyze > Area Based
// Load. Two halves, either or both in one run:
//   - THE TYPE. `loadType` is an area-based load type by its exact name. When
//     the model has none of that name and `density` is given, it is made with
//     that load density in W/m2 and, if named, that load classification. One
//     already present is used as it is: its density and classification are
//     never changed - a different `density` is named, not written.
//   - THE LOADS. With spaces (or electrical load areas) selected, the
//     electrical load area at each space is put in an area-based load of that
//     type on the space's level - one per level and phase, named `name` (left
//     out, the type's name, with the level's when the spaces span levels). An
//     area-based load already of that name on that level and of that type is
//     added to; one of another type is skipped and named, never retyped. A
//     load area already in it is skipped.
//
// ELECTRICAL LOAD AREAS. A space's load area is the one whose location point
// lies inside the space (Space.IsPointInSpace, at the space's mid height).
// When the space's level and phase have NO load areas at all, they are made
// for that level and phase - what Revit's own command does, the whole level
// at once - and the count is reported. When the level has load areas but
// none at a space, that space is refused and named: more are never added
// beside the ones the modeller has.
//
// NEVER DELETES, NEVER DISCONNECTS, NEVER RETYPES. Connecting an area-based
// load to the node upstream of it is ADD_ELECTRICAL_ANALYTICAL_NODE's.
//
// UNITS. W/m2 through UnitUtils with UnitTypeId.WattsPerSquareMeter, checked
// valid for SpecTypeId.ElectricalPowerDensity on the release that runs; if it
// is not, nothing is written. Read back the same way; loads in W and VA.
//
// RELEASES. AreaBasedLoadType (Create, LoadDensity, ApparentPowerDensity,
// PowerFactor, LoadClassification), Zone.CreateAreaBasedLoad and
// GetDomainData, AreaBasedLoadData (AreaBasedLoadType, AddElectricalLoadArea,
// GetElectricalLoadAreas, TrueLoad, ApparentLoad), ElectricalLoadAreaData
// (CreateElectricalLoadAreas, GetAreaBasedLoadIds) and SpatialElement
// .GetSpatialElementDomainData are 2023 to 2027 and absent before
// (tools/api-surface --members, 2026-11-02) - the card claims 2023 on.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var typesAdded = 0;
var loadsAdded = 0;
var areasAssigned = 0;
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
var wantType = given(loadType);
var wantClass = given(loadClassification);
var wantName = given(name);
var wantDensity = double.IsNaN(density) ? 0.0 : density;
var picked = (elements ?? new List<Element>()).Where(e => e != null).ToList();

// ---- The unit, checked against Revit's own ---------------------------------
var unitOk = false;
try { unitOk = UnitUtils.IsValidUnit(SpecTypeId.ElectricalPowerDensity, UnitTypeId.WattsPerSquareMeter); } catch (Exception) { }
Func<double, string> perM2 = v => num(UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.WattsPerSquareMeter)) + " W/m2";
Func<double, string> vaPerM2 = v => num(UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.VoltAmperesPerSquareMeter)) + " VA/m2";

// ---- What the model has -----------------------------------------------------
var types = new List<AreaBasedLoadType>();
var classes = new List<ElectricalLoadClassification>();
var loads = new List<Zone>();
Action readModel = () =>
{
    types.Clear(); classes.Clear(); loads.Clear();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(AreaBasedLoadType)))
        if (e is AreaBasedLoadType t) types.Add(t);
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ElectricalLoadClassification)))
        if (e is ElectricalLoadClassification c) classes.Add(c);
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Zone)))
    {
        var z = e as Zone;
        if (z == null) continue;
        ZoneElementDomainData d = null;
        try { d = z.GetDomainData(); } catch (Exception) { }
        if (d is AreaBasedLoadData) loads.Add(z);
    }
    types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    loads.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
};
Func<ElementId, string> nameOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var e = doc.GetElement(id);
    return e == null ? "none" : e.Name;
};
Func<AreaBasedLoadType, string> typeLine = t =>
    quoted(t.Name) + " (area-based load type, id " + t.Id.ToString() + ") " + perM2(t.LoadDensity) + ", "
    + vaPerM2(t.ApparentPowerDensity) + ", power factor " + num(t.PowerFactor) + ", load classification " + quoted(nameOf(t.LoadClassification));
Func<Zone, string> loadLine = z =>
{
    var d = z.GetDomainData() as AreaBasedLoadData;
    var areas = d == null ? 0 : d.GetElectricalLoadAreas().Count;
    return quoted(z.Name) + " (area-based load, id " + z.Id.ToString() + ") on " + quoted(nameOf(z.LevelId))
        + ", type " + quoted(d == null ? "none" : nameOf(d.AreaBasedLoadType)) + ", " + areas + " load area(s), "
        + (d == null ? "" : num(UnitUtils.ConvertFromInternalUnits(d.TrueLoad, UnitTypeId.Watts)) + " W, "
            + num(UnitUtils.ConvertFromInternalUnits(d.ApparentLoad, UnitTypeId.VoltAmperes)) + " VA");
};
Func<List<string>> listNow = () => types.Select(typeLine).Concat(loads.Select(loadLine)).ToList();

// ---- Refusals before anything is touched -----------------------------------
if (doc.IsFamilyDocument) refused.Add("This is a family - area-based loads live in a project.");
if (wantType.Length == 0)
    refused.Add("No area-based load type was named - by its exact name, or a new name with its load density in W/m2. Never guessed.");
if (wantDensity < 0 || wantDensity > 10000) refused.Add(num(wantDensity) + " W/m2 is not a load density.");
if (!unitOk) refused.Add("Revit does not take W/m2 for an electrical power density on this release - nothing is written until that is looked at.");
var didRead = false;
if (refused.Count == 0) { readModel(); before.AddRange(listNow()); didRead = true; }

// ---- The type ----------------------------------------------------------------
AreaBasedLoadType useType = null;
if (refused.Count == 0)
{
    useType = types.FirstOrDefault(t => string.Equals(t.Name, wantType, StringComparison.Ordinal));
    var near = types.FirstOrDefault(t => string.Equals(t.Name, wantType, StringComparison.OrdinalIgnoreCase));
    if (useType != null)
    {
        if (wantDensity > 0 && Math.Abs(UnitUtils.ConvertFromInternalUnits(useType.LoadDensity, UnitTypeId.WattsPerSquareMeter) - wantDensity) > 0.005)
            skipped.Add("Area-based load type " + quoted(useType.Name) + " is already here at " + perM2(useType.LoadDensity)
                + " - left as it is, not changed to " + num(wantDensity) + " W/m2.");
        else if (picked.Count == 0)
            skipped.Add("Area-based load type " + quoted(useType.Name) + " is already here - left as it is.");
    }
    else if (near != null)
        refused.Add("No area-based load type is named " + quoted(wantType) + ". " + quoted(near.Name)
            + " differs only in capitals - Heron does not take one for the other, nor makes a second.");
    else if (wantDensity <= 0)
        refused.Add("No area-based load type is named " + quoted(wantType) + ", and no load density was given to make one. The ones here are: "
            + (types.Count == 0 ? "none" : string.Join(", ", types.Select(t => quoted(t.Name)))) + ".");
    else
    {
        ElectricalLoadClassification useClass = null;
        if (wantClass.Length > 0)
        {
            useClass = classes.FirstOrDefault(c => string.Equals(c.Name, wantClass, StringComparison.Ordinal));
            if (useClass == null)
            {
                var nearClass = classes.FirstOrDefault(c => string.Equals(c.Name, wantClass, StringComparison.OrdinalIgnoreCase));
                refused.Add("No load classification is named " + quoted(wantClass) + ". "
                    + (nearClass != null ? quoted(nearClass.Name) + " differs only in capitals - Heron does not take one for the other. " : "")
                    + "The ones here are: " + (classes.Count == 0 ? "none" : string.Join(", ", classes.Select(c => quoted(c.Name)).OrderBy(n => n))) + ".");
            }
        }
        if (refused.Count == 0)
        {
            try
            {
                useType = AreaBasedLoadType.Create(doc, wantType);
                if (useType == null) refused.Add("Revit made no area-based load type named " + quoted(wantType) + ".");
                else
                {
                    useType.LoadDensity = UnitUtils.ConvertToInternalUnits(wantDensity, UnitTypeId.WattsPerSquareMeter);
                    if (useClass != null) useType.LoadClassification = useClass.Id;
                    typesAdded++;
                    findings.Add("Added " + typeLine(useType) + "."
                        + (useClass == null ? " Its load classification is Revit's own default - name one to set it." : ""));
                }
            }
            catch (Exception ex) { useType = null; refused.Add("Revit would not add area-based load type " + quoted(wantType) + ": " + ex.Message); }
        }
    }
}

// ---- The loads -------------------------------------------------------------
if (refused.Count == 0 && useType != null && picked.Count > 0)
{
    ElementId viewPhase = ElementId.InvalidElementId;
    try { var p = doc.ActiveView == null ? null : doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE); if (p != null) viewPhase = p.AsElementId(); }
    catch (Exception) { }
    Func<Element, ElementId> phaseOf = e =>
    {
        var p = e.get_Parameter(BuiltInParameter.ROOM_PHASE_ID);
        var id = p == null ? null : p.AsElementId();
        return id == null || id == ElementId.InvalidElementId ? viewPhase : id;
    };
    Func<SpatialElement, bool> isLoadArea = s =>
    {
        try { return s.GetSpatialElementDomainData() is ElectricalLoadAreaData; } catch (Exception) { return false; }
    };
    Func<ElementId, ElementId, List<SpatialElement>> loadAreasOn = (levelId, phaseId) =>
    {
        var list = new List<SpatialElement>();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)))
        {
            var s = e as SpatialElement;
            if (s == null || s.LevelId != levelId || !isLoadArea(s)) continue;
            if (phaseId != ElementId.InvalidElementId && phaseOf(s) != phaseId) continue;
            list.Add(s);
        }
        return list;
    };

    // Each picked element becomes (level, phase, load area).
    var wanted = new List<Tuple<ElementId, ElementId, SpatialElement, string>>();
    var madeFor = new HashSet<string>();
    foreach (var e in picked)
    {
        var label = (e.Name ?? "") + " (id " + e.Id.ToString() + ")";
        if (e is SpatialElement asArea && isLoadArea(asArea))
        {
            wanted.Add(Tuple.Create(asArea.LevelId, phaseOf(asArea), asArea, label));
            continue;
        }
        var space = e as Space;
        if (space == null) { skipped.Add(label + " is not a space or an electrical load area - left out."); continue; }
        if (space.Area <= 0) { skipped.Add("Space " + label + " is not placed or not enclosed - left out."); continue; }
        var phaseId = phaseOf(space);
        if (phaseId == ElementId.InvalidElementId) { refused.Add("Space " + label + ": its phase could not be read."); continue; }
        var key = space.LevelId.ToString() + "|" + phaseId.ToString();
        var onLevel = loadAreasOn(space.LevelId, phaseId);
        if (onLevel.Count == 0 && !madeFor.Contains(key))
        {
            madeFor.Add(key);
            try
            {
                var made = ElectricalLoadAreaData.CreateElectricalLoadAreas(doc, space.LevelId, phaseId);
                findings.Add("Level " + quoted(nameOf(space.LevelId)) + " had no electrical load areas in phase " + quoted(nameOf(phaseId))
                    + " - Revit made " + (made == null ? 0 : made.Count) + " there, the whole level at once.");
            }
            catch (Exception ex) { refused.Add("Revit would not make electrical load areas on " + quoted(nameOf(space.LevelId)) + ": " + ex.Message); continue; }
            onLevel = loadAreasOn(space.LevelId, phaseId);
        }
        var bb = space.get_BoundingBox(null);
        var z = bb == null ? 0.0 : (bb.Min.Z + bb.Max.Z) / 2.0;
        SpatialElement at = null;
        foreach (var a in onLevel)
        {
            var lp = a.Location as LocationPoint;
            if (lp == null) continue;
            if (space.IsPointInSpace(new XYZ(lp.Point.X, lp.Point.Y, z))) { at = a; break; }
        }
        if (at == null) refused.Add("Space " + label + " has no electrical load area inside it - place one there in Revit; Heron adds none beside the ones the level has.");
        else wanted.Add(Tuple.Create(space.LevelId, phaseId, at, label));
    }

    // One area-based load per level and phase.
    var groups = wanted.GroupBy(w => w.Item1.ToString() + "|" + w.Item2.ToString()).ToList();
    foreach (var g in groups)
    {
        var levelId = g.First().Item1;
        var phaseId = g.First().Item2;
        var loadName = (wantName.Length > 0 ? wantName : useType.Name) + (groups.Count > 1 ? " - " + nameOf(levelId) : "");
        var existing = loads.FirstOrDefault(z => z.LevelId == levelId && string.Equals(z.Name, loadName, StringComparison.OrdinalIgnoreCase));
        AreaBasedLoadData data = null;
        Zone zone = existing;
        if (existing != null)
        {
            data = existing.GetDomainData() as AreaBasedLoadData;
            if (data == null || data.AreaBasedLoadType != useType.Id)
            {
                skipped.Add("Area-based load " + quoted(existing.Name) + " on " + quoted(nameOf(levelId)) + " is already here as type "
                    + quoted(data == null ? "none" : nameOf(data.AreaBasedLoadType)) + " - left as it is, never retyped; "
                    + g.Count() + " space(s) not assigned. Name another load.");
                continue;
            }
        }
        else
        {
            try
            {
                zone = Zone.CreateAreaBasedLoad(doc, loadName, levelId, phaseId);
                data = zone == null ? null : zone.GetDomainData() as AreaBasedLoadData;
                if (data == null) { refused.Add("Revit made no area-based load named " + quoted(loadName) + "."); continue; }
                data.AreaBasedLoadType = useType.Id;
                loadsAdded++;
            }
            catch (Exception ex) { refused.Add("Revit would not add area-based load " + quoted(loadName) + ": " + ex.Message); continue; }
        }
        var inIt = data.GetElectricalLoadAreas();
        foreach (var w in g)
        {
            if (inIt.Contains(w.Item3.Id)) { skipped.Add(w.Item4 + ": its load area is already in " + quoted(zone.Name) + "."); continue; }
            try
            {
                data.AddElectricalLoadArea(w.Item3.Id);
                inIt = data.GetElectricalLoadAreas();
                if (inIt.Contains(w.Item3.Id)) areasAssigned++;
                else refused.Add(w.Item4 + ": Revit did not keep its load area in " + quoted(zone.Name) + ".");
            }
            catch (Exception ex) { refused.Add(w.Item4 + ": Revit would not add its load area to " + quoted(zone.Name) + ": " + ex.Message); }
        }
    }
}
else if (refused.Count == 0 && useType != null && typesAdded > 0)
    findings.Add("No spaces were selected, so the type was made and placed nowhere - select spaces and ask again to assign it.");

// ---- Read back -------------------------------------------------------------
if (didRead)
{
    try { readModel(); after.AddRange(listNow()); } catch (Exception ex) { findings.Add("The area-based loads could not be read back: " + ex.Message); }
}
foreach (var z in loads)
{
    var line = loadLine(z);
    if (!before.Contains(line)) findings.Add("Now: " + line + ".");
}
findings.Insert(0, "Area-based load types added " + typesAdded + ", area-based loads added " + loadsAdded + ", load areas assigned "
    + areasAssigned + (refused.Count > 0 ? "; some refused" : "") + ". Types and loads: " + before.Count + " before, " + after.Count + " after.");
findings.AddRange(skipped);
findings.AddRange(refused);
