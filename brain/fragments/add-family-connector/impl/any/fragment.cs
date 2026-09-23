// NOT STANDALONE. Assumes `doc`, `facePlane`, `system` and `sizeParameters` are
// in scope; leaves `connector`, `sizedBy`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A NEW CONNECTOR DOES NOT TAKE ITS FACE'S SIZE - it comes out at a generic one
// foot (reported from an earlier family build, twice: a duct port and a pipe
// stub). So a duct or pipe connector is refused without the parameters that
// size it, and they are ASSOCIATED to its own width, height or diameter so the
// port follows the family through every resize.
//
// THE FACE IS FOUND BY THE PLANE IT LIES ON. Exactly one solid face may lie on
// the plane named; the connector goes at that face's centre and points out of
// it, as Revit places it.
//
// THE SYSTEM NAME DECIDES DUCT, PIPE OR ELECTRICAL, matched to Revit's own enum
// names with the spaces taken out. The three enums are read at run time, so a
// system a later release adds is offered there with no change here.
//
// A CONNECTOR THAT CANNOT BE TIED TO ITS SIZE IS NOT LEFT BEHIND AT ONE FOOT.
// Any refusal after the checks THROWS, and the host rolls the call back.

var findings = new List<string>();
var connector = "";
var sizedBy = new List<string>();
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// "SupplyAir" read as "Supply Air", the way a modeller writes it.
Func<string, string> spaced = name =>
{
    var text = new System.Text.StringBuilder();
    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
        text.Append(name[i]);
    }
    return text.ToString();
};

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;

Func<XYZ, string> pointing = d =>
{
    var at = axisOf(d);
    if (at == 2) return d.Z > 0 ? "up" : "down";
    if (at == 0) return d.X > 0 ? "right (+X)" : "left (-X)";
    if (at == 1) return d.Y > 0 ? "back (+Y)" : "front (-Y)";
    return "(" + Math.Round(d.X, 3).ToString(invariant) + ", " + Math.Round(d.Y, 3).ToString(invariant)
        + ", " + Math.Round(d.Z, 3).ToString(invariant) + ")";
};

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

Func<FamilyParameter, bool> isLength = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Length", flags);
            var length = property == null ? null : property.GetValue(null);
            return spec != null && length != null && spec.Equals(length);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Length";
    }
    catch (Exception)
    {
        return false;
    }
};

// EVERY SYSTEM REVIT OFFERS A FAMILY CONNECTOR: (kind, name, value).
var systems = new List<Tuple<string, string, object>>();
var notOffered = new[] { "Fitting", "Global", "UndefinedSystemType", "Undefined" };
foreach (var value in Enum.GetValues(typeof(DuctSystemType)))
    if (!notOffered.Contains(value.ToString())) systems.Add(Tuple.Create("duct", value.ToString(), value));
foreach (var value in Enum.GetValues(typeof(PipeSystemType)))
    if (!notOffered.Contains(value.ToString())) systems.Add(Tuple.Create("pipe", value.ToString(), value));
foreach (var value in Enum.GetValues(typeof(ElectricalSystemType)))
    if (!notOffered.Contains(value.ToString())) systems.Add(Tuple.Create("electrical", value.ToString(), value));

Tuple<string, string, object> chosen = null;
PlanarFace face = null;
var sizes = new List<FamilyParameter>();
var faceName = (facePlane ?? "").Trim();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A connector "
        + "is added inside the family - open it first.";
}
else
{
    var fm = doc.FamilyManager;
    var problems = new List<string>();

    // THE SYSTEM.
    var said = squash(system);
    var systemMatches = systems.Where(s => squash(s.Item2) == said).ToList();
    if (said.Length == 0) problems.Add("No system was named - \"Supply Air\", \"Domestic Cold Water\", \"Power Balanced\".");
    else if (systemMatches.Count == 0)
    {
        var near = systems.Where(s => squash(s.Item2).Contains(said) || said.Contains(squash(s.Item2)))
            .Select(s => spaced(s.Item2) + " (" + s.Item1 + ")").Take(8).ToList();
        problems.Add("No connector system is called \"" + system.Trim() + "\"."
            + (near.Count > 0 ? " Close: " + string.Join(", ", near) + "." : " Revit's names are used - Supply Air, Return Air, Exhaust Air, Domestic Cold Water, Sanitary, Other Pipe, Power Balanced."));
    }
    else if (systemMatches.Count > 1)
        problems.Add("\"" + system.Trim() + "\" is a " + string.Join(" and a ", systemMatches.Select(s => s.Item1))
            + " system both - nothing was made rather than one being picked.");
    else chosen = systemMatches[0];

    // THE SIZES.
    var sizeNames = (sizeParameters ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    foreach (var name in sizeNames)
    {
        var p = fm.get_Parameter(name);
        if (p == null) problems.Add("\"" + name + "\" is not a parameter of this family - ADD_FAMILY_PARAMETERS makes one.");
        else if (!isLength(p)) problems.Add("\"" + name + "\" is not a length, so it cannot size a connector.");
        else sizes.Add(p);
    }

    if (chosen != null && problems.Count == 0)
    {
        if (chosen.Item1 == "duct" && sizes.Count != 1 && sizes.Count != 2)
            problems.Add("A duct connector needs its size: two length parameters for a rectangular port "
                + "(width, then height) or one for a round one (the diameter). Without them it stays at "
                + "Revit's one-foot placeholder, whatever face it is on.");
        if (chosen.Item1 == "pipe" && sizes.Count != 1)
            problems.Add("A pipe connector needs ONE length parameter - its diameter. Without it it stays at "
                + "Revit's one-foot placeholder.");
        if (chosen.Item1 == "electrical" && sizes.Count != 0)
            problems.Add("An electrical connector has no size, so no size parameters are taken.");
        if (sizes.Count > 0 && fm.CurrentType == null)
            problems.Add("This family has no type yet, so its size parameters hold nothing to check the "
                + "connector against. SET_FAMILY_TYPE_VALUES makes the first type.");
    }

    // THE FACE, by the plane it lies on.
    if (faceName.Length == 0) problems.Add("No face was named - the reference plane or level it lies on, \"Neck Top\".");
    else
    {
        var datums = new List<Tuple<int, double, string>>();
        foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
        {
            var at = axisOf(rp.Normal);
            if (at >= 0 && string.Equals(ownName(rp), faceName, StringComparison.OrdinalIgnoreCase))
                datums.Add(Tuple.Create(at, along(rp.GetPlane().Origin, at), ownName(rp)));
        }
        foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
            if (string.Equals(level.Name, faceName, StringComparison.OrdinalIgnoreCase))
                datums.Add(Tuple.Create(2, level.Elevation, level.Name));

        if (datums.Count == 0) problems.Add("No reference plane or level is called \"" + faceName + "\".");
        else if (datums.Count > 1) problems.Add("\"" + faceName + "\" names " + datums.Count + " planes - rename one first.");
        else
        {
            var datum = datums[0];
            var candidates = new List<PlanarFace>();
            var forms = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements()
                .OfType<GenericForm>().Where(f => f.IsSolid).ToList();
            foreach (var form in forms)
            {
                var geometry = form.get_Geometry(new Options { ComputeReferences = true });
                if (geometry == null) continue;
                foreach (GeometryObject piece in geometry)
                {
                    var solid = piece as Solid;
                    if (solid == null) continue;
                    foreach (Face each in solid.Faces)
                    {
                        var flat = each as PlanarFace;
                        if (flat != null && flat.Reference != null && axisOf(flat.FaceNormal) == datum.Item1
                            && Math.Abs(along(flat.Origin, datum.Item1) - datum.Item2) < halfMillimetre)
                            candidates.Add(flat);
                    }
                }
            }

            if (candidates.Count == 0)
                problems.Add("No solid face lies on \"" + datum.Item3 + "\". Build the geometry to it first - "
                    + "EXTRUDE_BETWEEN_PLANES locks a box to named planes.");
            else if (candidates.Count > 1)
                problems.Add(candidates.Count + " solid faces lie on \"" + datum.Item3 + "\" (facing "
                    + string.Join(", ", candidates.Select(c => pointing(c.FaceNormal))) + "), so which one "
                    + "the connector goes on is not clear. Name a plane only one face lies on - the top of a "
                    + "neck, not the plane a neck stands on.");
            else face = candidates[0];
        }
    }

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems);
}

if (refused == null)
{
    var fm = doc.FamilyManager;
    var round = chosen.Item1 != "electrical" && sizes.Count == 1;
    ConnectorElement made;

    try
    {
        if (chosen.Item1 == "duct")
            made = ConnectorElement.CreateDuctConnector(doc, (DuctSystemType)chosen.Item3,
                round ? ConnectorProfileType.Round : ConnectorProfileType.Rectangular, face.Reference);
        else if (chosen.Item1 == "pipe")
            made = ConnectorElement.CreatePipeConnector(doc, (PipeSystemType)chosen.Item3, face.Reference);
        else
            made = ConnectorElement.CreateElectricalConnector(doc, (ElectricalSystemType)chosen.Item3, face.Reference);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the " + spaced(chosen.Item2) + " connector on the "
            + "face lying on \"" + faceName + "\": " + ex.Message + " NOTHING from this call was kept.");
    }

    Action<BuiltInParameter, FamilyParameter, string> tie = (own, driver, role) =>
    {
        var ownParameter = made.get_Parameter(own);
        if (ownParameter == null || !fm.CanElementParameterBeAssociated(ownParameter))
            throw new InvalidOperationException("The connector's " + role + " cannot be tied to \""
                + driver.Definition.Name + "\" in this Revit, so it would have stayed at one foot. NOTHING "
                + "from this call was kept.");
        try
        {
            fm.AssociateElementParameterToFamilyParameter(ownParameter, driver);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not tie the connector's " + role + " to \""
                + driver.Definition.Name + "\": " + ex.Message + " NOTHING from this call was kept.");
        }
        sizedBy.Add("connector " + role + " <- " + driver.Definition.Name);
    };

    if (chosen.Item1 != "electrical")
    {
        if (round) tie(BuiltInParameter.CONNECTOR_DIAMETER, sizes[0], "diameter");
        else
        {
            tie(BuiltInParameter.CONNECTOR_WIDTH, sizes[0], "width");
            tie(BuiltInParameter.CONNECTOR_HEIGHT, sizes[1], "height");
        }
    }

    doc.Regenerate();

    // READ BACK - the connector's own size beside what its parameters hold.
    Func<Func<double>, double?> read = get => { try { return get(); } catch (Exception) { return null; } };
    var shape = "";
    var mismatched = new List<string>();

    Action<string, double?, FamilyParameter> compare = (role, actual, driver) =>
    {
        var wanted = fm.CurrentType.AsDouble(driver);
        if (!actual.HasValue || !wanted.HasValue || Math.Abs(actual.Value - wanted.Value) > halfMillimetre)
            mismatched.Add(role + " reads " + (actual.HasValue ? mm(actual.Value) + " mm" : "nothing")
                + " and " + driver.Definition.Name + " holds " + (wanted.HasValue ? mm(wanted.Value) + " mm" : "nothing"));
    };

    if (chosen.Item1 == "electrical") shape = "no size";
    else if (round)
    {
        var diameter = read(() => made.Radius * 2);
        shape = "round " + (diameter.HasValue ? mm(diameter.Value) : "?") + " mm";
        compare("the diameter", diameter, sizes[0]);
    }
    else
    {
        var width = read(() => made.Width);
        var height = read(() => made.Height);
        shape = "rectangular " + (width.HasValue ? mm(width.Value) : "?") + " x "
            + (height.HasValue ? mm(height.Value) : "?") + " mm";
        compare("the width", width, sizes[0]);
        compare("the height", height, sizes[1]);
    }

    var origin = made.Origin;
    connector = chosen.Item1 + " connector, " + spaced(chosen.Item2) + ", " + shape + ", at X " + mm(origin.X)
        + ", Y " + mm(origin.Y) + ", Z " + mm(origin.Z) + ", pointing " + pointing(made.Direction)
        + ", on the face lying on \"" + faceName + "\"";

    findings.Add("Made a " + connector + " - read back from the connector." + (sizedBy.Count > 0
        ? " Its size follows the family: " + string.Join("; ", sizedBy) + "."
        : ""));

    if (mismatched.Count > 0)
        findings.Add("THE CONNECTOR DOES NOT READ WHAT ITS PARAMETERS HOLD: " + string.Join("; ", mismatched)
            + ". Check which way round width and height sit on this face before trusting it.");

    findings.Add("Flow direction, description and electrical load are Revit's defaults - set them in "
        + "Properties. FLEX_FAMILY checks the connector follows its parameters through a resize.");
}

if (refused != null) findings.Add(refused);
