// NOT STANDALONE. Assumes `doc` is in scope; leaves `catalogText`, `fileName`,
// `typeCount`, `leftOut`, `notAFamily` and `findings` behind.
//
// READS ONLY. Nothing is changed, and nothing is written to disk - the text
// comes back, and whoever asked saves it.
//
// A TYPE CATALOG - the text file beside a family, with the family's name and
// .txt, that makes Revit show "Specify Types" when the family is loaded, so a
// project takes only the sizes it needs out of fifty. This reads every type of
// the family open in the Family Editor and writes their TYPE values in the
// catalog's own format:
//
//   ,Width##LENGTH##MILLIMETERS,Height##LENGTH##MILLIMETERS,Description##OTHER##
//   600 x 400,600,400,Small box
//
// THE HEADER WORDS ARE REVIT'S, NEVER TYPED HERE. Each column's kind and unit
// word come from Revit's own "string used in type catalogs" for the
// parameter's kind and for the unit the family displays it in, so a length
// shown in millimetres is written in millimetres under the word Revit reads
// for millimetres. Revit renamed some unit words in 2021, which is exactly why
// they are asked for rather than written down.
//
// WHAT IS LEFT OUT, AND SAID: instance parameters (a catalog sets types),
// values a formula or a dimension drives (they follow by themselves), values
// that hold an element - a material, a nested type - whose catalog spelling is
// not documented, and a parameter blank in every type. Each is named.
//
// THE FILE NAME is the family's own path with .txt, when the family has been
// saved; otherwise its name, and a finding that it goes beside the .rfa.

var findings = new List<string>();
var catalogText = "";
var fileName = "";
var typeCount = 0;
var leftOut = "";
var notAFamily = false;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

// A value as a catalog cell: quoted when it holds the delimiter, a quote or a
// line break, with quotes doubled.
Func<string, string> cell = text =>
{
    var value = text ?? "";
    if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
    return "\"" + value.Replace("\"", "\"\"") + "\"";
};

// What a parameter's definition says it holds, read the way its release allows:
// a ForgeTypeId from 2021, the old UnitType before.
Func<FamilyParameter, object> specOf = p =>
{
    try
    {
        var definition = p.Definition;
        foreach (var name in new[] { "GetDataType", "GetSpecTypeId" })
        {
            var method = definition.GetType().GetMethod(name, System.Type.EmptyTypes);
            if (method != null) return method.Invoke(definition, null);
        }
        var property = definition.GetType().GetProperty("UnitType");
        return property == null ? null : property.GetValue(definition);
    }
    catch (Exception) { return null; }
};

// For a measured kind: the catalog's word for the kind, the catalog's word for
// the unit the family displays it in, and how to turn an internal value into
// that unit. Null for a kind that is not measured.
var units = doc.GetUnits();
Func<object, Tuple<string, string, Func<double, double>>> measuredAs = spec =>
{
    if (spec == null) return null;
    try
    {
        var specType = spec.GetType();
        if (specType.Name == "ForgeTypeId")
        {
            var forSpec = typeof(UnitUtils).GetMethod("GetTypeCatalogStringForSpec", flags, null, new[] { specType }, null);
            var forUnit = typeof(UnitUtils).GetMethod("GetTypeCatalogStringForUnit", flags, null, new[] { specType }, null);
            var convert = typeof(UnitUtils).GetMethod("ConvertFromInternalUnits", flags, null,
                new[] { typeof(double), specType }, null);
            var options = typeof(Units).GetMethod("GetFormatOptions", new[] { specType });
            if (forSpec == null || forUnit == null || convert == null || options == null) return null;
            var kindWord = forSpec.Invoke(null, new[] { spec }) as string;
            var format = options.Invoke(units, new[] { spec }) as FormatOptions;
            var unit = format == null ? null : format.GetType().GetMethod("GetUnitTypeId").Invoke(format, null);
            var unitWord = unit == null ? null : forUnit.Invoke(null, new[] { unit }) as string;
            if (string.IsNullOrEmpty(kindWord) || string.IsNullOrEmpty(unitWord)) return null;
            return Tuple.Create(kindWord, unitWord, (Func<double, double>)(v => (double)convert.Invoke(null, new[] { (object)v, unit })));
        }
        if (specType.IsEnum)
        {
            var forKind = typeof(UnitUtils).GetMethod("GetTypeCatalogString", flags, null, new[] { specType }, null);
            var options = typeof(Units).GetMethod("GetFormatOptions", new[] { specType });
            if (forKind == null || options == null) return null;
            var kindWord = forKind.Invoke(null, new[] { spec }) as string;
            var format = options.Invoke(units, new[] { spec }) as FormatOptions;
            var display = format == null ? null : format.GetType().GetProperty("DisplayUnits").GetValue(format);
            if (display == null) return null;
            var forUnit = typeof(UnitUtils).GetMethod("GetTypeCatalogString", flags, null, new[] { display.GetType() }, null);
            var convert = typeof(UnitUtils).GetMethod("ConvertFromInternalUnits", flags, null,
                new[] { typeof(double), display.GetType() }, null);
            var unitWord = forUnit == null ? null : forUnit.Invoke(null, new[] { display }) as string;
            if (string.IsNullOrEmpty(kindWord) || string.IsNullOrEmpty(unitWord) || convert == null) return null;
            return Tuple.Create(kindWord, unitWord, (Func<double, double>)(v => (double)convert.Invoke(null, new[] { (object)v, display })));
        }
    }
    catch (Exception) { }
    return null;
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A type catalog is read from the family - open it for editing first (OPEN_FAMILY_FOR_EDITING).");
}
else
{
    var fm = doc.FamilyManager;
    var types = new List<FamilyType>();
    foreach (FamilyType t in fm.Types) types.Add(t);
    types = types.Where(t => !string.IsNullOrEmpty(t.Name) && t.Name.Trim().Length > 0).OrderBy(t => t.Name).ToList();
    typeCount = types.Count;

    var skipped = new List<string>();
    // Each column: the parameter, its header, and how one type's value is written.
    var columns = new List<Tuple<FamilyParameter, string, Func<FamilyType, string>>>();
    foreach (FamilyParameter p in fm.GetParameters())
    {
        var name = p.Definition == null ? "" : p.Definition.Name;
        if (p.IsInstance) { skipped.Add(name + " (instance)"); continue; }
        if (p.IsReporting) { skipped.Add(name + " (reporting)"); continue; }
        if (p.IsDeterminedByFormula) { skipped.Add(name + " (formula)"); continue; }
        if (p.StorageType == StorageType.ElementId) { skipped.Add(name + " (holds an element)"); continue; }
        if (p.StorageType == StorageType.None) continue;
        var parameter = p;
        if (!types.Any(t => { try { return t.HasValue(parameter); } catch (Exception) { return false; } }))
        { skipped.Add(name + " (blank in every type)"); continue; }

        if (p.StorageType == StorageType.Double)
        {
            var measured = measuredAs(specOf(p));
            if (measured == null) { skipped.Add(name + " (a kind with no catalog word)"); continue; }
            columns.Add(Tuple.Create(p, name + "##" + measured.Item1 + "##" + measured.Item2, (Func<FamilyType, string>)(t =>
            {
                var v = t.HasValue(parameter) ? t.AsDouble(parameter) : null;
                return v.HasValue ? Math.Round(measured.Item3(v.Value), 6).ToString("0.######", invariant) : "";
            })));
        }
        else if (p.StorageType == StorageType.Integer)
            columns.Add(Tuple.Create(p, name + "##OTHER##", (Func<FamilyType, string>)(t =>
            {
                var v = t.HasValue(parameter) ? t.AsInteger(parameter) : null;
                return v.HasValue ? v.Value.ToString(invariant) : "";
            })));
        else
            columns.Add(Tuple.Create(p, name + "##OTHER##", (Func<FamilyType, string>)(t =>
                t.HasValue(parameter) ? (t.AsString(parameter) ?? "") : "")));
    }

    var lines = new List<string>();
    lines.Add("," + string.Join(",", columns.Select(c => cell(c.Item2))));
    foreach (var t in types)
        lines.Add(cell(t.Name.Trim()) + (columns.Count == 0 ? "" : "," + string.Join(",", columns.Select(c => cell(c.Item3(t))))));
    catalogText = string.Join("\r\n", lines) + "\r\n";
    leftOut = string.Join(", ", skipped);

    var saved = doc.PathName ?? "";
    var familyName = doc.OwnerFamily == null ? doc.Title : doc.OwnerFamily.Name;
    fileName = saved.Length > 0 ? System.IO.Path.ChangeExtension(saved, ".txt") : familyName + ".txt";

    findings.Add(typeCount + " type(s) and " + columns.Count + " column(s) read into a type catalog for \"" + familyName
        + "\" - nothing was written; save the text as " + fileName + ".");
    if (saved.Length == 0)
        findings.Add("The family has not been saved, so it has no folder yet. The catalog goes beside the .rfa, with "
            + "the same name and .txt.");
    if (typeCount < 2)
        findings.Add("The family has " + typeCount + " type(s). A catalog earns its place when there are many - make "
            + "them first with SET_FAMILY_TYPE_VALUES.");
    if (skipped.Count > 0)
        findings.Add(skipped.Count + " parameter(s) left out: " + leftOut + ".");
    findings.Add("Yes/No and whole-number values are written as numbers, under OTHER; how Revit reads each is "
        + "NEEDS-CHECKING BV10 - File > Export > Family Types in Revit writes its own catalog to compare against.");
}
