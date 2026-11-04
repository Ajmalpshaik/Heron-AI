// NOT STANDALONE. Assumes `doc`, `elements`, `action`, `partition`,
// `toPartition`, `startAt`, `fromNumber`, `toNumber` and `reinforcement` are in
// scope, and leaves `changed`, `before`, `after`, `skipped`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// REVIT'S REBAR NUMBERING - Structure > Reinforcement > Reinforcement Numbers,
// done through the numbering schema Revit keeps for rebar, couplers or fabric
// sheets. One action, on NAMED partitions only:
//   start     ShiftNumbers   - the partition's numbers start at `startAt`,
//                              gaps kept
//   compact   RemoveGaps     - the partition's gaps closed; `partition` all
//                              closes each partition's gaps on its own
//   renumber  RemoveGaps, then ShiftNumbers when `startAt` is given
//   change    ChangeNumber   - `fromNumber` becomes `toNumber` in the partition
//   rename    MoveSequence   - the partition is called `toPartition` (new)
//   merge     AppendSequence / MergeSequences - the partitions in `partition`
//                              (separated by ;) go into `toPartition`
//   move      AssignElementsToSequence - the SELECTED bars, couplers or fabric
//                              sheets go into `partition`
//
// NEVER A GUESSED PARTITION. A name the model does not have is refused with
// the names it has; one that differs only in capitals is named, never taken.
// NEVER ACROSS PARTITIONS UNLESS ASKED: only the partitions named are touched,
// and `all` is taken by compact alone, which closes each partition's own gaps.
//
// OWNED BY ANOTHER USER IS SKIPPED AND NAMED (Article 12). A partition action
// renumbers every element in the partition, so one element another user owns
// skips the whole partition; a move skips just the bars owned.
//
// BEFORE AND AFTER. Each partition's numbers are read before and after, and
// `changed` counts the elements whose partition or number is different
// afterwards - read off the model, never taken from what was asked.
//
// RELEASES. Every NumberingSchema member here is present 2020 to 2027
// (tools/api-surface --members, 2026-11-01). 2027 marks the schema-type route,
// MoveSequence, AppendSequence, MergeSequences and AssignElementsToSequence
// obsolete - rebar, couplers and fabric only - and still ships them.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var changed = 0;
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

const string NoPartition = "(none)";
var act = (action ?? "").Trim().ToLowerInvariant();
var named = (partition ?? "").Trim();
var target = (toPartition ?? "").Trim();
if (target == NoPartition) target = "";
var kindWord = (reinforcement ?? "").Trim().ToLowerInvariant();

Func<string, string> shown = name => string.IsNullOrEmpty(name) ? "(no partition name)" : "\"" + name + "\"";
Func<Element, string> label = e => (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

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
Func<Element, ElementId, string> numberOf = (e, id) =>
{
    if (id == null || id == ElementId.InvalidElementId) return "";
    foreach (Parameter p in e.Parameters)
    {
        if (p != null && p.Id == id) return text(p);
    }
    return "";
};

var kindNames = new[] { "rebar", "rebar coupler", "fabric sheet" };
var kindTypes = new[]
{
    NumberingSchemaTypes.StructuralNumberingSchemas.Rebar,
    NumberingSchemaTypes.StructuralNumberingSchemas.RebarCoupler,
    NumberingSchemaTypes.StructuralNumberingSchemas.ReinforcementFabric
};
var kindCategories = new[] { BuiltInCategory.OST_Rebar, BuiltInCategory.OST_Coupler, BuiltInCategory.OST_FabricReinforcement };

var schemas = new NumberingSchema[3];
for (var k = 0; k < 3; k++)
{
    try { schemas[k] = NumberingSchema.GetNumberingSchema(doc, kindTypes[k]); }
    catch (Exception) { schemas[k] = null; }
}

// Every numbered element of one kind, and its partition and number now.
Func<int, List<Element>> membersOfKind = k =>
{
    var list = new List<Element>();
    foreach (var e in new FilteredElementCollector(doc).OfCategory(kindCategories[k]).WhereElementIsNotElementType())
    {
        if (hasPartition(e)) list.Add(e);
    }
    return list;
};
Func<Dictionary<ElementId, string>> snapshot = () =>
{
    var state = new Dictionary<ElementId, string>();
    for (var k = 0; k < 3; k++)
    {
        var parameterId = schemas[k] == null ? ElementId.InvalidElementId : schemas[k].NumberingParameterId;
        foreach (var e in membersOfKind(k)) state[e.Id] = partitionOf(e) + "\u0001" + numberOf(e, parameterId);
    }
    return state;
};

Func<int, string, string> numbersText = (k, name) =>
{
    try
    {
        var parts = new List<string>();
        foreach (var r in schemas[k].GetNumbers(name)) parts.Add(r.Low == r.High ? r.Low.ToString() : r.Low + "-" + r.High);
        return kindNames[k] + " partition " + shown(name) + ": " + (parts.Count == 0 ? "no numbers" : string.Join(", ", parts));
    }
    catch (Exception) { return kindNames[k] + " partition " + shown(name) + ": no numbers (not in the model)"; }
};
Func<int, string, int, bool> inUse = (k, name, number) =>
{
    foreach (var r in schemas[k].GetNumbers(name)) if (number >= r.Low && number <= r.High) return true;
    return false;
};

// Elements in these partitions another user owns. One is enough to skip a
// partition action, because it renumbers every element in the partition.
Func<int, IList<string>, List<string>> ownedIn = (k, names) =>
{
    var owned = new List<string>();
    if (!doc.IsWorkshared) return owned;
    foreach (var e in membersOfKind(k))
    {
        var p = partitionOf(e);
        if (!names.Contains(p)) continue;
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
            owned.Add(label(e) + " in partition " + shown(p) + " - owned by " + owner);
    }
    return owned;
};

var actions = new[] { "start", "compact", "renumber", "change", "rename", "merge", "move" };
var kind = -1;
if (kindWord == "rebar" || kindWord == "bar" || kindWord == "bars") kind = 0;
else if (kindWord == "coupler" || kindWord == "couplers" || kindWord == "rebar coupler" || kindWord == "rebar couplers") kind = 1;
else if (kindWord == "fabric" || kindWord == "fabric sheet" || kindWord == "fabric sheets") kind = 2;

if (Array.IndexOf(actions, act) < 0)
{
    refused.Add("\"" + action + "\" is not an action this takes. Choose one of: " + string.Join(", ", actions) + ".");
}
else if (act != "move" && kind < 0)
{
    refused.Add("\"" + reinforcement + "\" is not rebar, coupler or fabric - the three numberings Revit keeps.");
}
else if (act != "move" && schemas[kind] == null)
{
    refused.Add("This model has no " + kindNames[kind] + " numbering schema, so there is nothing to change.");
}
else if (named.Length == 0)
{
    refused.Add("No partition was named. Nothing is renumbered without one - REPORT_REBAR_NUMBERING lists them.");
}

var plan = new List<string>();     // the partitions this run will act on
if (refused.Count == 0 && act != "move")
{
    var schema = schemas[kind];
    var sequences = new List<string>(schema.GetNumberingSequences() ?? new List<string>());
    sequences.Sort(StringComparer.Ordinal);
    Func<string, string> unknown = name =>
    {
        var near = sequences.FindAll(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
        return "No " + kindNames[kind] + " partition is named " + shown(name) + ". "
            + (sequences.Count == 0 ? "This model has none." : "Its partitions are: " + string.Join(", ", sequences.ConvertAll(s => shown(s))) + ".")
            + (near.Count > 0 ? " " + shown(near[0]) + " differs only in capitals - it is not taken for it." : "");
    };

    if (string.Equals(named, "all", StringComparison.OrdinalIgnoreCase) && !sequences.Contains(named))
    {
        if (act == "compact") plan.AddRange(sequences);
        else refused.Add("\"all\" is taken by compact alone. " + act + " changes ONE named partition, so no partition "
            + "is renumbered that was not asked for.");
    }
    else if (act == "merge")
    {
        foreach (var piece in named.Split(';'))
        {
            var name = piece.Trim();
            if (name.Length == 0) continue;
            if (!sequences.Contains(name)) refused.Add(unknown(name));
            else if (!plan.Contains(name)) plan.Add(name);
        }
    }
    else if (!sequences.Contains(named)) refused.Add(unknown(named));
    else plan.Add(named);

    // Each action's own values, checked before anything changes.
    string message;
    if (refused.Count == 0)
    {
        if ((act == "start" || (act == "renumber" && startAt > 0))
            && (startAt < 1 || startAt > NumberingSchema.MaximumStartingNumber))
            refused.Add("startAt " + startAt + " is not a number Revit starts a partition at - it takes 1 to "
                + NumberingSchema.MaximumStartingNumber + ".");
        if (act == "change")
        {
            if (fromNumber < 1 || toNumber < 1)
                refused.Add("change needs fromNumber and toNumber, both 1 or more.");
            else if (!inUse(kind, named, fromNumber))
                refused.Add("No element in " + shown(named) + " has the number " + fromNumber + ". " + numbersText(kind, named) + ".");
            else if (inUse(kind, named, toNumber))
                refused.Add("The number " + toNumber + " is already used in " + shown(named) + ", so changing "
                    + fromNumber + " to it would give two different bars one number. " + numbersText(kind, named) + ".");
        }
        if (act == "rename" || act == "merge")
        {
            if (target.Length == 0)
                refused.Add(act + " needs toPartition - the partition's new name, or the one merged into.");
            else if (plan.Contains(target))
                refused.Add(shown(target) + " is both a partition merged and the one merged into.");
            else if (act == "rename" && sequences.Contains(target))
                refused.Add(shown(target) + " is already a partition. Renaming into it would merge the two - "
                    + "ask for merge to do that.");
            else if (!sequences.Contains(target) && !NumberingSchema.IsValidPartitionName(target, out message))
                refused.Add(shown(target) + " cannot be a partition name: " + message);
        }
        if (act == "merge" && plan.Count == 0 && refused.Count == 0)
            refused.Add("No partition to merge was named.");
    }

    if (refused.Count == 0)
    {
        // Ownership: one element another user owns skips the action.
        var touched = new List<string>(plan);
        if ((act == "merge") && sequences.Contains(target)) touched.Add(target);
        var owned = ownedIn(kind, touched);
        if (owned.Count > 0 && act != "compact")
        {
            skipped.AddRange(owned);
            refused.Add("Nothing was renumbered: " + owned.Count + " element(s) in " + string.Join(", ", touched.ConvertAll(s => shown(s)))
                + " are owned by another user, and this renumbers every element in the partition. Ask them to "
                + "relinquish, then run it again.");
        }
        else if (owned.Count > 0)
        {
            // compact all: skip just the partitions with owned elements.
            skipped.AddRange(owned);
            var keep = new List<string>();
            foreach (var name in plan)
            {
                if (ownedIn(kind, new List<string> { name }).Count == 0) keep.Add(name);
                else findings.Add("Partition " + shown(name) + " skipped - another user owns elements in it.");
            }
            plan = keep;
        }
    }

    if (refused.Count == 0)
    {
        var was = snapshot();
        foreach (var name in plan) before.Add(numbersText(kind, name));
        if ((act == "merge") && sequences.Contains(target)) before.Add(numbersText(kind, target));

        var did = new List<string>();
        try
        {
            if (act == "compact") foreach (var name in plan) { schema.RemoveGaps(name); did.Add(shown(name) + " gaps closed"); }
            else if (act == "start") { schema.ShiftNumbers(named, startAt); did.Add(shown(named) + " starts at " + startAt); }
            else if (act == "renumber")
            {
                schema.RemoveGaps(named);
                if (startAt > 0) schema.ShiftNumbers(named, startAt);
                did.Add(shown(named) + " gaps closed" + (startAt > 0 ? " and starting at " + startAt : ", first number kept"));
            }
            else if (act == "change")
            {
                var moved = schema.ChangeNumber(named, fromNumber, toNumber);
                did.Add(fromNumber + " became " + toNumber + " in " + shown(named) + " on " + (moved == null ? 0 : moved.Count) + " element(s)");
            }
            else if (act == "rename") { schema.MoveSequence(named, target); did.Add(shown(named) + " is now " + shown(target)); }
            else if (act == "merge")
            {
                if (sequences.Contains(target)) foreach (var name in plan) schema.AppendSequence(name, target);
                else schema.MergeSequences(plan, target);
                did.Add(string.Join(", ", plan.ConvertAll(s => shown(s))) + " merged into " + shown(target));
            }
        }
        catch (Exception ex)
        {
            refused.Add("Revit refused the " + act + ": " + ex.Message);
        }

        try { doc.Regenerate(); } catch (Exception) { }

        var afterNames = new List<string>();
        if (act == "rename" || act == "merge") afterNames.Add(target);
        else afterNames.AddRange(plan);
        foreach (var name in afterNames) after.Add(numbersText(kind, name));

        var now = snapshot();
        foreach (var pair in now) { string old; if (!was.TryGetValue(pair.Key, out old) || old != pair.Value) changed++; }
        if (did.Count > 0) findings.Add(string.Join("; ", did) + ". " + changed + " element(s) changed number or partition.");
    }
}

// ---- move: the selected elements into `partition` ---------------------------
if (refused.Count == 0 && act == "move")
{
    string message;
    var groups = new List<ElementId>[3] { new List<ElementId>(), new List<ElementId>(), new List<ElementId>() };
    var fromPartitions = new List<string>[3] { new List<string>(), new List<string>(), new List<string>() };
    if (elements == null || elements.Count == 0)
        refused.Add("No bars were selected, so nothing was moved to " + shown(named) + ".");
    else
    {
        foreach (var e in elements)
        {
            if (e == null) continue;
            var k = -1;
            for (var i = 0; i < 3; i++) if (e.Category != null && e.Category.Id == new ElementId(kindCategories[i])) k = i;
            if (k < 0 || !hasPartition(e) || schemas[k] == null)
            {
                refused.Add(label(e) + " - not a numbered bar, coupler or fabric sheet");
                continue;
            }
            var current = partitionOf(e);
            if (current == named) { findings.Add(label(e) + " is already in partition " + shown(named) + "."); continue; }
            if (doc.IsWorkshared)
            {
                string owner;
                if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
                {
                    skipped.Add(label(e) + " in partition " + shown(current) + " - owned by " + owner);
                    continue;
                }
            }
            groups[k].Add(e.Id);
            if (!fromPartitions[k].Contains(current)) fromPartitions[k].Add(current);
        }
    }

    var any = groups[0].Count + groups[1].Count + groups[2].Count > 0;
    var exists = false;
    for (var k = 0; k < 3; k++) if (groups[k].Count > 0 && schemas[k].GetNumberingSequences().Contains(named)) exists = true;
    if (any && !exists && !NumberingSchema.IsValidPartitionName(named, out message))
    {
        refused.Add(shown(named) + " cannot be a partition name: " + message);
        any = false;
    }

    if (any)
    {
        var was = snapshot();
        for (var k = 0; k < 3; k++)
        {
            if (groups[k].Count == 0) continue;
            foreach (var name in fromPartitions[k]) before.Add(numbersText(k, name));
            before.Add(numbersText(k, named));
        }
        for (var k = 0; k < 3; k++)
        {
            if (groups[k].Count == 0) continue;
            try { schemas[k].AssignElementsToSequence(new HashSet<ElementId>(groups[k]), named); }
            catch (Exception)
            {
                // One at a time, so one element Revit will not take is named and
                // the rest still go.
                foreach (var id in groups[k])
                {
                    try { schemas[k].AssignElementsToSequence(new HashSet<ElementId> { id }, named); }
                    catch (Exception ex) { refused.Add(label(doc.GetElement(id)) + " - Revit refused the move: " + ex.Message); }
                }
            }
        }
        try { doc.Regenerate(); } catch (Exception) { }
        for (var k = 0; k < 3; k++)
        {
            if (groups[k].Count == 0) continue;
            foreach (var name in fromPartitions[k]) after.Add(numbersText(k, name));
            after.Add(numbersText(k, named));
        }
        var now = snapshot();
        foreach (var pair in now) { string old; if (!was.TryGetValue(pair.Key, out old) || old != pair.Value) changed++; }
        findings.Add(changed + " element(s) moved to partition " + shown(named) + " and numbered there.");
    }
}

if (refused.Count > 0 && changed == 0) findings.Insert(0, "NOTHING WAS RENUMBERED. " + refused[0]);
if (skipped.Count > 0) findings.Add(skipped.Count + " element(s) skipped - owned by another user: " + string.Join("; ", skipped));
findings.AddRange(before.ConvertAll(s => "before: " + s));
findings.AddRange(after.ConvertAll(s => "after: " + s));
