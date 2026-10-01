// NOT STANDALONE. Assumes `doc`, `family`, `type`, `point` and `host` are in
// scope; leaves `instanceId`, `placed`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FAMILY INSIDE A FAMILY - a motor in an air handler, a handle on a valve, a
// fixing on a bracket: one type of a family already loaded into the family
// open in the Family Editor, placed in it. PLACE_FAMILY_INSTANCES cannot do
// this - it places through the project's creation object, which Revit's own
// remarks say throws in a family document.
//
// HOW IT SITS DEPENDS ON THE NESTED FAMILY, never on a guess:
//
//   WORK PLANE-BASED - on the reference plane or level named as its host, the
//   way a nested family is best made: it moves with that plane. The point
//   must lie ON that plane.
//
//   LEVEL-BASED - on the level named. The API has a call for that inside a
//   family only from Revit 2024 (it was the project's alone before); it is
//   found by reflection, and on an earlier release the one call for a family
//   with no host is tried, which Revit's remarks say refuses a level-based
//   family - so the answer there says to make the nested family Work
//   Plane-Based (Family Category and Parameters, in the nested family), load it
//   again, and place it on a plane.
//
//   Any other kind - line-based, view-based, adaptive, hosted - is refused by
//   name.
//
// THE POINT IS IN MILLIMETRES FROM THE FAMILY'S ORIGIN, "X,Y,Z", and is READ
// BACK from the placed family's location; one that does not read back where it
// was asked fails the call, and the host rolls the whole call back.

var findings = new List<string>();
var instanceId = "";
var placed = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};
Func<XYZ, string> where = p => mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + " mm";

Family chosenFamily = null;
FamilySymbol symbol = null;
XYZ at = null;
Element hostElement = null;
var hostName = "";
var placement = FamilyPlacementType.Invalid;
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A nested family is "
        + "placed inside the family - open it first; in a project, PLACE_FAMILY_INSTANCES places families.";
}
else
{
    // ---- the family and its type -----------------------------------------
    var own = doc.OwnerFamily == null ? ElementId.InvalidElementId : doc.OwnerFamily.Id;
    var loaded = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>()
        .Where(f => f.Id != own).ToList();
    var saidFamily = (family ?? "").Trim();
    var matches = loaded.Where(f => string.Equals(f.Name, saidFamily, StringComparison.OrdinalIgnoreCase)).ToList();
    if (saidFamily.Length == 0) problems.Add("No family was named.");
    else if (matches.Count == 0)
        problems.Add("No family called \"" + saidFamily + "\" is loaded into this family. Loaded: "
            + (loaded.Count == 0 ? "none" : string.Join(", ", loaded.Select(f => f.Name).OrderBy(n => n).Take(25)))
            + ". LOAD_FAMILY - or Revit's Load Family - loads it in first.");
    else chosenFamily = matches[0];

    if (chosenFamily != null)
    {
        var types = chosenFamily.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol)
            .Where(s => s != null).ToList();
        var saidType = (type ?? "").Trim();
        symbol = types.FirstOrDefault(s => string.Equals(s.Name, saidType, StringComparison.OrdinalIgnoreCase));
        if (symbol == null)
            problems.Add("\"" + chosenFamily.Name + "\" has no type called \"" + saidType + "\". Its types: "
                + string.Join(", ", types.Select(s => s.Name).OrderBy(n => n)) + ".");
        placement = chosenFamily.FamilyPlacementType;
    }

    // ---- the point --------------------------------------------------------
    var parts = (point ?? "").Split(',').Select(p => p.Trim()).ToList();
    double x, y, z;
    if (parts.Count != 3
        || !double.TryParse(parts[0], System.Globalization.NumberStyles.Float, invariant, out x)
        || !double.TryParse(parts[1], System.Globalization.NumberStyles.Float, invariant, out y)
        || !double.TryParse(parts[2], System.Globalization.NumberStyles.Float, invariant, out z))
        problems.Add("The point \"" + (point ?? "") + "\" is not \"X,Y,Z\" in millimetres from the family's origin.");
    else at = new XYZ(x / 304.8, y / 304.8, z / 304.8);

    // ---- the host -----------------------------------------------------------
    hostName = (host ?? "").Trim();
    var isNone = string.Equals(hostName, "none", StringComparison.OrdinalIgnoreCase);
    if (hostName.Length == 0)
        problems.Add("No host was named - the reference plane or level it sits on, or \"none\".");
    else if (!isNone)
    {
        foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
            if (string.Equals(ownName(rp), hostName, StringComparison.OrdinalIgnoreCase))
            {
                if (hostElement != null) { problems.Add("\"" + hostName + "\" names two planes - rename one first."); break; }
                hostElement = rp;
            }
        foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
            if (string.Equals(level.Name, hostName, StringComparison.OrdinalIgnoreCase))
            {
                if (hostElement != null) { problems.Add("\"" + hostName + "\" names more than one plane or level."); break; }
                hostElement = level;
            }
        if (hostElement == null)
            problems.Add("No reference plane or level is called \"" + hostName + "\".");
    }

    // ---- what the nested family's kind asks of them ------------------------
    if (chosenFamily != null && symbol != null && at != null && problems.Count == 0)
    {
        if (placement == FamilyPlacementType.WorkPlaneBased)
        {
            if (hostElement == null)
                problems.Add("\"" + chosenFamily.Name + "\" is Work Plane-Based, so it sits on a plane - name the "
                    + "reference plane or level, not \"none\".");
            else
            {
                Plane surface = hostElement is ReferencePlane ? ((ReferencePlane)hostElement).GetPlane()
                    : Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, ((Level)hostElement).Elevation));
                var off = (at - surface.Origin).DotProduct(surface.Normal);
                if (Math.Abs(off) > halfMillimetre)
                    problems.Add("The point " + where(at) + " is " + mm(Math.Abs(off)) + " mm off \"" + hostName + "\" - a "
                        + "Work Plane-Based family sits ON its plane. Give a point on it.");
            }
        }
        else if (placement == FamilyPlacementType.OneLevelBased)
        {
            if (!(hostElement is Level))
                problems.Add("\"" + chosenFamily.Name + "\" is level-based, so it sits on a LEVEL - name one, such as "
                    + "Ref. Level" + (hostElement != null ? ", not the reference plane \"" + hostName + "\"" : "") + ".");
            // ON the level: what an offset above it reads back as is not in
            // Revit's remarks, so it is not asked of the call.
            else if (Math.Abs(at.Z - ((Level)hostElement).Elevation) > halfMillimetre)
                problems.Add("A level-based family is placed ON its level, and Z " + mm(at.Z) + " mm is not \"" + hostName
                    + "\" at " + mm(((Level)hostElement).Elevation) + " mm. Give Z at the level; its offset is a "
                    + "parameter of the placed family.");
        }
        else
            problems.Add("\"" + chosenFamily.Name + "\" is placed " + placement + " - this places only Work "
                + "Plane-Based and level-based families in a family.");
    }

    if (problems.Count > 0) refused = "Nothing was placed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// PLACE, THEN READ IT BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    FamilyInstance instance = null;
    var how = "";
    try
    {
        if (placement == FamilyPlacementType.WorkPlaneBased)
        {
            var reference = hostElement is ReferencePlane ? ((ReferencePlane)hostElement).GetReference()
                : ((Level)hostElement).GetPlaneReference();
            var normal = hostElement is ReferencePlane ? ((ReferencePlane)hostElement).Normal : XYZ.BasisZ;
            // A direction IN the plane for the family's own X - the model's X
            // unless the plane faces along it.
            var across = Math.Abs(normal.X) > 0.9999 ? XYZ.BasisY : XYZ.BasisX;
            across = (across - normal * across.DotProduct(normal)).Normalize();
            instance = doc.FamilyCreate.NewFamilyInstance(reference, at, across, symbol);
            how = "on \"" + hostName + "\"";
        }
        else
        {
            var level = (Level)hostElement;
            // From 2024 a family's creation object takes a level; before, only
            // the project's did. Reached by reflection so 2020 to 2023 compile.
            var withLevel = doc.FamilyCreate.GetType().GetMethod("NewFamilyInstance",
                new[] { typeof(XYZ), typeof(FamilySymbol), typeof(Level), typeof(StructuralType) });
            if (withLevel != null)
            {
                instance = (FamilyInstance)withLevel.Invoke(doc.FamilyCreate,
                    new object[] { at, symbol, level, StructuralType.NonStructural });
                how = "on the level \"" + level.Name + "\"";
            }
            else
            {
                instance = doc.FamilyCreate.NewFamilyInstance(at, symbol,
                    StructuralType.NonStructural);
                how = "at the point, with no level (this release has no family call that takes one)";
            }
        }
    }
    catch (Exception ex)
    {
        var inner = ex is System.Reflection.TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
        throw new InvalidOperationException("Revit would not place \"" + chosenFamily.Name + " : " + symbol.Name + "\" "
            + "at " + where(at) + ": " + inner.Message
            + (placement == FamilyPlacementType.OneLevelBased
                ? " A level-based family is placed inside a family through the API only from Revit 2024. Make the "
                  + "nested family Work Plane-Based (Family Category and Parameters, in that family), load it again, "
                  + "and place it on a plane."
                : "")
            + " The call failed, and Heron rolls the whole call back.");
    }
    if (instance == null)
        throw new InvalidOperationException("Revit placed nothing. The call failed, and Heron rolls the whole call back.");

    doc.Regenerate();

    // READ BACK: the type, and where it stands.
    if (instance.Symbol == null || instance.Symbol.Id != symbol.Id)
        throw new InvalidOperationException("The placed family is not of the type \"" + symbol.Name + "\". The call "
            + "failed, and Heron rolls the whole call back.");
    var location = instance.Location as LocationPoint;
    if (location == null)
        throw new InvalidOperationException("The placed family has no point to read back. The call failed, and Heron "
            + "rolls the whole call back.");
    if (location.Point.DistanceTo(at) > halfMillimetre)
        throw new InvalidOperationException("The placed family stands at " + where(location.Point) + ", not "
            + where(at) + ". The call failed, and Heron rolls the whole call back.");

    instanceId = instance.UniqueId;
    placed = "\"" + chosenFamily.Name + " : " + symbol.Name + "\" placed " + how + " at " + where(location.Point)
        + " - read back. Its id is " + instanceId + ".";
    findings.Add(placed);
    findings.Add("Next: LOCK_NESTED_FAMILY_TO_PLANES locks its own reference planes to this family's, so it moves "
        + "with them; LINK_FAMILY_PARAMETER links its instance parameters, LINK_NESTED_TYPE_PARAMETER its type's.");
}

if (refused != null) findings.Add(refused);
