// NOT STANDALONE. Assumes `doc`, `elements`, `fabricSheetType`,
// `majorDirection`, `location` and `fabricAreaType` are in scope; leaves
// `placed`, `hostsReinforced`, `sheets`, `skipped`, `readBackWrong`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// FABRIC (MESH) OVER THE WHOLE HOST: a slab, a foundation slab or a wall, to
// its own boundary, inside its own cover, laid in sheets of the fabric sheet
// type asked. The sheet type, its direction and its face are the modeller's
// (D-33); a sheet type is never chosen and none is made here - a mesh is its
// wires, which are a design value.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;

var placed = new List<ElementId>();
var hostsReinforced = new List<ElementId>();
var sheets = 0;
var skipped = new List<string>();
var readBackWrong = new List<string>();
var findings = new List<string>();
var refused = "";
var lines = new List<string>();

var sheetTypes = new FilteredElementCollector(doc).OfClass(typeof(FabricSheetType)).Cast<FabricSheetType>().ToList();
var areaTypes = new FilteredElementCollector(doc).OfClass(typeof(FabricAreaType)).Cast<FabricAreaType>().ToList();

var sheetText = (fabricSheetType ?? "").Trim();
var sheetType = sheetTypes.FirstOrDefault(t => string.Equals(t.Name, sheetText, StringComparison.Ordinal));
if (sheetType == null)
    refused = "No Fabric Sheet Type is named '" + sheetText + "'. Loaded: "
        + (sheetTypes.Count == 0 ? "none - load or make the mesh sheet type the spec names" : string.Join(", ", sheetTypes.Select(t => "'" + t.Name + "'")))
        + ". Nothing was placed";

var where = (location ?? "").Trim().ToLowerInvariant();
var wantTop = where == "top" || where == "exterior";
if (refused.Length == 0 && !wantTop && where != "bottom" && where != "interior")
    refused = "'" + (location ?? "") + "' is not a face - say top or bottom for a slab, exterior or interior for a wall. Nothing was placed";

FabricAreaType areaType = null;
var areaTypeText = (fabricAreaType ?? "").Trim();
if (refused.Length == 0)
{
    if (areaTypeText.Length == 0 || areaTypeText.Equals("only", StringComparison.OrdinalIgnoreCase))
    {
        if (areaTypes.Count == 1) areaType = areaTypes[0];
        else if (areaTypes.Count > 1)
            refused = areaTypes.Count + " Fabric Area Types are loaded: " + string.Join(", ", areaTypes.Select(t => "'" + t.Name + "'"))
                + ". Say which, by its name. Nothing was placed";
    }
    else
    {
        areaType = areaTypes.FirstOrDefault(t => string.Equals(t.Name, areaTypeText, StringComparison.Ordinal));
        if (areaType == null)
            refused = "No Fabric Area Type is named '" + areaTypeText + "'. Loaded: "
                + (areaTypes.Count == 0 ? "none" : string.Join(", ", areaTypes.Select(t => "'" + t.Name + "'"))) + ". Nothing was placed";
    }
}
var direction = (majorDirection ?? "").Trim().ToLowerInvariant();

if (refused.Length == 0)
{
    foreach (var host in elements)
    {
        if (host == null) continue;
        var name = (host.Category == null ? "element" : host.Category.Name) + " '" + host.Name + "' (id " + host.Id + ")";
        var isWall = host is Wall;
        if (!(host is Floor) && !isWall)
        {
            skipped.Add(name + ": not a slab or a wall - fabric goes only on those");
            continue;
        }
        if (!RebarHostData.IsValidHost(host))
        {
            skipped.Add(name + ": not a rebar host - its type is not structural, or its material is not concrete");
            continue;
        }
        if (isWall != (where == "exterior" || where == "interior"))
        {
            skipped.Add(name + ": a " + (isWall ? "wall's faces are exterior and interior" : "slab's faces are top and bottom") + ", not " + where);
            continue;
        }
        var already = RebarHostData.GetRebarHostData(host).GetFabricAreasInHost().Count;
        if (already > 0)
        {
            skipped.Add(name + ": already holds " + already + " fabric area(s) - run again it would double it. REPORT_HOST_REBAR reads it");
            continue;
        }

        XYZ major = null;
        string directionProblem = null;
        if (isWall)
        {
            var curve = (host as Wall).Location as LocationCurve;
            var line = curve == null ? null : curve.Curve as Line;
            if (direction == "horizontal")
            {
                if (line == null) directionProblem = "a curved wall has no single horizontal direction";
                else major = line.Direction.Normalize();
            }
            else if (direction == "vertical") major = XYZ.BasisZ;
            else directionProblem = "a wall's major direction is horizontal or vertical, not '" + majorDirection + "'";
        }
        else
        {
            double degrees;
            if (direction == "x") major = XYZ.BasisX;
            else if (direction == "y") major = XYZ.BasisY;
            else if (double.TryParse(direction.Replace("deg", "").Trim(), styleFloat, invariant, out degrees))
                major = new XYZ(Math.Cos(degrees * Math.PI / 180), Math.Sin(degrees * Math.PI / 180), 0);
            else directionProblem = "a slab's major direction is x, y, or an angle in degrees from the model's x axis, not '" + majorDirection + "'";
        }
        if (directionProblem != null)
        {
            skipped.Add(name + ": " + directionProblem + " - nothing placed in it");
            continue;
        }

        if (areaType == null)
        {
            areaType = doc.GetElement(FabricAreaType.CreateDefaultFabricAreaType(doc)) as FabricAreaType;
            findings.Add("No Fabric Area Type was loaded, so Revit's default one was made" + (areaType == null ? "" : ": '" + areaType.Name + "'") + ".");
        }

        FabricArea made = null;
        string failure = null;
        try
        {
            made = FabricArea.Create(doc, host, major, areaType.Id, sheetType.Id);
            if (made == null) throw new InvalidOperationException("Revit made no fabric area");
            made.FabricLocation = wantTop ? FabricLocation.TopOrExternal : FabricLocation.BottomOrInternal;
            doc.Regenerate();
        }
        catch (Exception ex)
        {
            failure = ex.Message;
        }
        if (failure != null)
        {
            if (made != null && made.IsValidObject)
            {
                try { doc.Delete(made.Id); } catch (Exception) { }
            }
            skipped.Add(name + ": Revit refused the fabric - " + failure + " - it was removed again");
            continue;
        }

        // ---- read back --------------------------------------------------------------------
        var count = made.GetFabricSheetElementIds().Count;
        var onTop = made.FabricLocation == FabricLocation.TopOrExternal;
        var locationNow = onTop ? (isWall ? "exterior" : "top") : (isWall ? "interior" : "bottom");
        if (onTop != wantTop) readBackWrong.Add(name + ": the fabric reads back on the " + locationNow + " face");
        if (made.FabricSheetTypeId != sheetType.Id) readBackWrong.Add(name + ": the fabric reads back another sheet type");
        if (count == 0) readBackWrong.Add(name + ": the fabric area holds no sheets");
        sheets += count;
        placed.Add(made.Id);
        hostsReinforced.Add(host.Id);
        lines.Add(name + ": fabric area id " + made.Id + ", " + count + " sheet(s) of '" + sheetType.Name + "', " + locationNow
            + " face, major direction " + direction);
    }
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(placed.Count + " fabric area(s) holding " + sheets + " sheet(s) placed and read back in " + hostsReinforced.Count
        + " host(s); " + skipped.Count + " skipped" + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " read back other than asked" : "") + ".");
    findings.AddRange(skipped);
    findings.AddRange(readBackWrong);
}
