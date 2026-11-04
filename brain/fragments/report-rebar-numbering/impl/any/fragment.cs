// NOT STANDALONE. Assumes `doc`, `elements` and `partition` are in scope, and
// leaves `partitions`, `numberedElements`, `report`, `partitionNames`,
// `selected`, `ownedByOthers` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// REVIT'S REBAR NUMBERING - Structure > Reinforcement > Reinforcement Numbers.
// Revit numbers rebar, rebar couplers and fabric sheets in three numbering
// schemas, and each schema in PARTITIONS - the Partition parameter on the bar.
// Identical bars in one partition share a number; the same bar in another
// partition is numbered on its own. This reads, for every partition of every
// schema: the numbers in use (NumberingSchema.GetNumbers, Revit's own ranges),
// the lowest and highest, the gaps between them, how many elements carry them,
// and - on a workshared model - which of those elements another user owns.
//
// NEVER A GUESSED PARTITION. `partition` absent reads as "all". A name the
// model does not have is answered with the names it does have; one that
// differs only in capitals is NAMED, never taken for the other.
//
// RELEASES. NumberingSchema.GetNumberingSchema(Document, NumberingSchemaType),
// NumberingSchemaTypes.StructuralNumberingSchemas.Rebar / RebarCoupler /
// ReinforcementFabric, GetNumberingSequences, GetNumbers, NumberingParameterId
// and IntegerRange.Low / High are present 2020 to 2027 (tools/api-surface
// --members, 2026-11-01). 2027 marks the schema-type route obsolete in favour
// of a schema name, and still ships it.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var partitions = 0;
var numberedElements = 0;
var report = "";
var partitionNames = new List<string>();
var selected = new List<string>();
var ownedByOthers = new List<string>();
var findings = new List<string>();

var wanted = (partition ?? "").Trim();
var everyPartition = wanted.Length == 0 || string.Equals(wanted, "all", StringComparison.OrdinalIgnoreCase);

Func<string, string> shown = name => string.IsNullOrEmpty(name) ? "(no partition name)" : "\"" + name + "\"";
Func<Element, string> label = e => (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

// A parameter's text whatever it is stored as - Revit's number parameters are
// read, never assumed to be a string or an integer.
Func<Parameter, string> text = p =>
{
    if (p == null) return "";
    try
    {
        if (p.StorageType == StorageType.String) return p.AsString() ?? "";
        if (p.StorageType == StorageType.Integer) return p.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return p.AsValueString() ?? "";
    }
    catch (Exception) { return ""; }
};

// The number parameter is the one the SCHEMA names - found by comparing ids.
Func<Element, ElementId, Parameter> parameterById = (e, id) =>
{
    if (id == null || id == ElementId.InvalidElementId) return null;
    foreach (Parameter p in e.Parameters)
    {
        if (p != null && p.Id == id) return p;
    }
    return null;
};

Func<Element, string> partitionOf = e =>
{
    try { return text(e.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)); }
    catch (Exception) { return ""; }
};
Func<Element, bool> hasPartition = e =>
{
    try { return e.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM) != null; }
    catch (Exception) { return false; }
};

Func<IList<IntegerRange>, string> rangesText = ranges =>
{
    var parts = new List<string>();
    foreach (var r in ranges) parts.Add(r.Low == r.High ? r.Low.ToString() : r.Low + "-" + r.High);
    return parts.Count == 0 ? "none" : string.Join(", ", parts);
};

// The three schemas, in the Reinforcement Numbers dialog's words.
var kindNames = new[] { "Rebar", "Rebar coupler", "Fabric sheet" };
var kindTypes = new[]
{
    NumberingSchemaTypes.StructuralNumberingSchemas.Rebar,
    NumberingSchemaTypes.StructuralNumberingSchemas.RebarCoupler,
    NumberingSchemaTypes.StructuralNumberingSchemas.ReinforcementFabric
};
var kindCategories = new[] { BuiltInCategory.OST_Rebar, BuiltInCategory.OST_Coupler, BuiltInCategory.OST_FabricReinforcement };

var lines = new List<string>();
var everyName = new List<string>();
var matchedWanted = false;
var schemaNumberParameter = new Dictionary<int, ElementId>();
var isWorkshared = doc.IsWorkshared;

for (var k = 0; k < kindNames.Length; k++)
{
    NumberingSchema schema = null;
    try { schema = NumberingSchema.GetNumberingSchema(doc, kindTypes[k]); }
    catch (Exception ex)
    {
        findings.Add(kindNames[k] + " numbering could not be read: " + ex.Message);
        continue;
    }
    if (schema == null)
    {
        findings.Add("This model has no " + kindNames[k].ToLowerInvariant() + " numbering schema.");
        continue;
    }
    schemaNumberParameter[k] = schema.NumberingParameterId;

    var sequences = new List<string>(schema.GetNumberingSequences() ?? new List<string>());
    sequences.Sort(StringComparer.Ordinal);

    // Elements of this schema's category, grouped by their Partition value.
    var byPartition = new Dictionary<string, List<Element>>(StringComparer.Ordinal);
    foreach (var e in new FilteredElementCollector(doc).OfCategory(kindCategories[k]).WhereElementIsNotElementType())
    {
        if (!hasPartition(e)) continue;
        var name = partitionOf(e);
        if (!byPartition.ContainsKey(name)) byPartition[name] = new List<Element>();
        byPartition[name].Add(e);
    }

    foreach (var name in sequences)
    {
        everyName.Add(shown(name) + " (" + kindNames[k].ToLowerInvariant() + ")");
        if (!everyPartition && !string.Equals(name, wanted, StringComparison.Ordinal)) continue;
        matchedWanted = true;
        partitions++;
        partitionNames.Add(shown(name) + " (" + kindNames[k].ToLowerInvariant() + ")");

        IList<IntegerRange> ranges = new List<IntegerRange>();
        try { ranges = schema.GetNumbers(name) ?? new List<IntegerRange>(); }
        catch (Exception ex) { findings.Add(kindNames[k] + " partition " + shown(name) + ": its numbers could not be read - " + ex.Message); }

        var members = byPartition.ContainsKey(name) ? byPartition[name] : new List<Element>();
        numberedElements += members.Count;

        var line = kindNames[k] + " partition " + shown(name) + ": numbers " + rangesText(ranges);
        if (ranges.Count > 0)
        {
            var lowest = ranges[0].Low;
            var highest = ranges[0].High;
            foreach (var r in ranges) { if (r.Low < lowest) lowest = r.Low; if (r.High > highest) highest = r.High; }
            var gaps = new List<string>();
            var missing = 0;
            for (var i = 1; i < ranges.Count; i++)
            {
                var from = ranges[i - 1].High + 1;
                var to = ranges[i].Low - 1;
                if (to < from) continue;
                missing += to - from + 1;
                gaps.Add(from == to ? from.ToString() : from + "-" + to);
            }
            line += " (lowest " + lowest + ", highest " + highest + ", "
                + (missing == 0 ? "no gaps" : missing + " number(s) unused between them: " + string.Join(", ", gaps)) + ")";
        }
        line += "; " + members.Count + " element(s) carry them";

        if (isWorkshared)
        {
            var owned = 0;
            foreach (var e in members)
            {
                string owner;
                if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
                {
                    owned++;
                    ownedByOthers.Add(label(e) + " in partition " + shown(name) + " - owned by " + owner);
                }
            }
            if (owned > 0) line += "; " + owned + " of them owned by another user, so a change to this partition would skip it";
        }
        lines.Add(line);
    }
}

report = string.Join(" | ", lines);

if (!everyPartition && !matchedWanted)
{
    var near = everyName.FindAll(n => n.StartsWith("\"" + wanted + "\"", StringComparison.OrdinalIgnoreCase));
    findings.Add("No partition is named \"" + wanted + "\" in this model. "
        + (everyName.Count == 0 ? "It has no numbering partitions at all."
            : "Its partitions are: " + string.Join(", ", everyName) + ".")
        + (near.Count > 0 ? " " + string.Join(", ", near) + " differs only in capitals - Heron does not take one for the other." : ""));
}
else if (everyName.Count == 0)
{
    findings.Add("This model has no rebar numbering partitions - no rebar, coupler or fabric sheet in it carries a number.");
}
else
{
    findings.Add(partitions + " partition(s) read, " + numberedElements + " numbered element(s) in them"
        + (everyPartition ? "" : ", of " + everyName.Count + " partition(s) in the model")
        + (ownedByOthers.Count > 0 ? "; " + ownedByOthers.Count + " element(s) owned by another user" : "")
        + ". Change them in Structure > Reinforcement > Reinforcement Numbers.");
    findings.AddRange(lines);
}

// ---- The selection: each element's partition and number --------------------
if (elements != null)
{
    foreach (var e in elements)
    {
        if (e == null) continue;
        var kind = -1;
        for (var k = 0; k < kindCategories.Length; k++)
        {
            if (e.Category != null && e.Category.Id == new ElementId(kindCategories[k])) kind = k;
        }
        if (!hasPartition(e))
        {
            selected.Add(label(e) + " - not numbered reinforcement, so it has no partition");
            continue;
        }
        var number = "";
        if (kind >= 0 && schemaNumberParameter.ContainsKey(kind)) number = text(parameterById(e, schemaNumberParameter[kind]));
        selected.Add(label(e) + " - partition " + shown(partitionOf(e))
            + (kind < 0 ? " (its bars carry the numbers)" : ", number " + (number.Length == 0 ? "none" : number)));
    }
    if (selected.Count > 0) findings.AddRange(selected);
}
