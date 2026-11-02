// NOT STANDALONE. Assumes `doc`, `elements`, `connectionType` and `layout` are
// in scope; leaves `created`, `alreadyConnected`, `report`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, and
// the caller owns it. Each connection is placed inside a SubTransaction of its
// own within that transaction, so a connection Revit refuses - or one that
// reads back wrong - is put back alone.
//
// REVIT'S STEEL > CONNECTION. StructuralConnectionHandler.Create(doc, ids,
// typeId) - the one overload present 2020 to 2027 (tools/api-surface,
// 2026-11-01). RevitAPI.xml: the ids must be structural framing, structural
// columns, walls, floors or foundations, and "the first of given elements is
// set as primary one". Three other overloads exist and none is called: the one
// without a type was deprecated in 2021 and is gone from 2022, the one with
// input points is 2021 on, and THE ONE TAKING A TYPE NAME AS A STRING MAKES A
// CUSTOM TYPE AND DELETES THE ELEMENTS HANDED IN. The type is resolved to its
// ElementId here, always.
//
// THE TYPE IS NEVER GUESSED (D-33). `connectionType` names a connection type
// by its name or as "Family: Type". Left out it arrives as "list": nothing is
// placed and the model's connection types are named - even when there is
// only one.
//
// TWO LAYOUTS. `together` (the default, as Revit's own tool works): ONE
// connection joining every member handed in - and they must touch one
// another, each member's box within 50 mm of another's, or nothing is placed
// and the member touching none is named. `each`: one connection on EACH member
// alone - a base plate on each column. Anything else is named, nothing placed.
//
// THE PRIMARY MEMBER. Revit makes the first id the primary one, and a Revit
// selection has no order. So the order is set here, the supporting member
// first: columns, then framing, then foundations, walls and floors, each in
// the order handed in. The report names the primary. Whether every connection
// type wants the column as primary is not seen in Revit (NEEDS-CHECKING).
//
// NEVER PLACED TWICE, NEVER DELETED. Members a connection already joins - all
// of them, for `together` - are skipped and the connection named. A member
// alone already carrying a single-member connection at the end Revit picks is
// put back and named. Nothing here deletes a connection or a member.
//
// WHAT IS REPORTED IS READ BACK, before each connection is kept: after a
// regenerate it is still in the model, of the type asked, and joins every
// member asked.
//
// PLATES, BOLTS, WELDS AND CUTS are the connection type's own, made by Revit's
// Steel Connections; the Revit API does not reach them (the Advance Steel API
// does), so nothing here makes or edits one.

var created = new List<ElementId>();
var alreadyConnected = 0;
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was placed.
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
var touchTolerance = 50.0 / MillimetresPerFoot;   // D-20: millimetres to feet

var lines = new List<string>();
var skipped = new List<string>();
var problems = new List<string>();

Func<ElementId, string> describe = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "nothing";
    var e = doc.GetElement(id);
    if (e == null) return "element " + id + " (not in this model)";
    string cat = "", name = "";
    try { cat = e.Category == null ? "" : e.Category.Name + " "; } catch (Exception) { }
    try { name = e.Name ?? ""; } catch (Exception) { }
    return cat + "\"" + name + "\" (" + id + ")";
};

Func<Element, string> typeLabel = one =>
{
    if (one == null) return "no type";
    string name = "", family = "";
    try { name = one.Name ?? ""; } catch (Exception) { }
    try { family = ((ElementType)one).FamilyName ?? ""; } catch (Exception) { }
    return family.Length > 0 ? family + ": " + name : name;
};

// ---- the type ----------------------------------------------------------------

var allTypes = new FilteredElementCollector(doc).OfClass(typeof(StructuralConnectionHandlerType))
    .Cast<StructuralConnectionHandlerType>()
    .OrderBy(t => typeLabel(t), StringComparer.OrdinalIgnoreCase).ToList();
var typeChoices = allTypes.Count == 0
    ? "this model has no connection type loaded - load one first (Steel > Connection > Connection Settings)"
    : "the model's connection types are " + string.Join(", ", allTypes.Select(t => "\"" + typeLabel(t) + "\""));

var said = (connectionType ?? "").Trim();
var listOnly = said.Length == 0 || string.Equals(said, "list", StringComparison.OrdinalIgnoreCase);

StructuralConnectionHandlerType useType = null;
string problem = null;

if (listOnly)
{
    problem = "No connection type was named, so nothing was placed - " + typeChoices + ". Say which one to place";
}
else
{
    var matches = new List<StructuralConnectionHandlerType>();
    foreach (var one in allTypes)
    {
        string name = "", family = "";
        try { name = one.Name ?? ""; } catch (Exception) { }
        try { family = one.FamilyName ?? ""; } catch (Exception) { }
        if (string.Equals(name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + ": " + name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + " : " + name, said, StringComparison.OrdinalIgnoreCase))
            matches.Add(one);
    }
    if (matches.Count == 1) useType = matches[0];
    else if (matches.Count == 0)
        problem = string.Format("No connection type called \"{0}\" - {1}", said, typeChoices);
    else
        problem = string.Format("{0} connection types are called \"{1}\" - name it as \"Family: Type\"; {2}",
            matches.Count, said, typeChoices);
}

// ---- the layout ----------------------------------------------------------------

var how = (layout ?? "").Trim().ToLowerInvariant();
var each = false;
if (problem == null)
{
    if (how.Length == 0 || how == "together" || how == "one" || how == "between") each = false;
    else if (how == "each" || how == "per member" || how == "separately") each = true;
    else problem = string.Format("layout takes \"together\" - one connection joining every member handed in - or "
        + "\"each\" - one connection on each member alone. \"{0}\" is neither, so nothing was placed", layout);
}

// ---- the members -----------------------------------------------------------------

var allowed = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_StructuralColumns),
    new ElementId(BuiltInCategory.OST_StructuralFraming),
    new ElementId(BuiltInCategory.OST_StructuralFoundation),
    new ElementId(BuiltInCategory.OST_Walls),
    new ElementId(BuiltInCategory.OST_Floors),
};
var members = new List<Element>();
var seen = new HashSet<ElementId>();
foreach (var e in elements ?? new List<Element>())
{
    if (e == null || !seen.Add(e.Id)) continue;
    if (e is StructuralConnectionHandler)
    {
        skipped.Add(describe(e.Id) + ": is a connection already - left alone. Its type is changed with "
            + "CHANGE_ELEMENT_TYPE, never by placing another");
        continue;
    }
    var cat = e.Category == null ? ElementId.InvalidElementId : e.Category.Id;
    var rank = allowed.FindIndex(c => c == cat);
    if (rank < 0)
    {
        skipped.Add(describe(e.Id) + ": not a structural framing, column, wall, floor or foundation - Revit "
            + "connects only those, so it was not handed to Revit");
        continue;
    }
    members.Add(e);
}
// The supporting member first - Revit makes the first id the primary one.
members = members.Select((e, i) => new { e, i })
    .OrderBy(x => allowed.FindIndex(c => c == (x.e.Category == null ? ElementId.InvalidElementId : x.e.Category.Id)))
    .ThenBy(x => x.i).Select(x => x.e).ToList();

if (problem == null && members.Count == 0)
    problem = "Nothing handed in is a structural framing, column, wall, floor or foundation, so there is nothing "
        + "to connect";

// ---- the groups: one connection each -------------------------------------------------

var groups = new List<List<Element>>();
if (problem == null)
{
    if (each) foreach (var m in members) groups.Add(new List<Element> { m });
    else
    {
        // Together, the members must touch one another - one joint, not a guess.
        if (members.Count > 1)
        {
            var boxes = members.Select(m => { try { return m.get_BoundingBox(null); } catch (Exception) { return null; } }).ToList();
            Func<int, int, bool> touch = (a, b) =>
            {
                var p = boxes[a]; var q = boxes[b];
                if (p == null || q == null) return false;
                return p.Min.X <= q.Max.X + touchTolerance && q.Min.X <= p.Max.X + touchTolerance
                    && p.Min.Y <= q.Max.Y + touchTolerance && q.Min.Y <= p.Max.Y + touchTolerance
                    && p.Min.Z <= q.Max.Z + touchTolerance && q.Min.Z <= p.Max.Z + touchTolerance;
            };
            var reached = new HashSet<int> { 0 };
            var queue = new Queue<int>(); queue.Enqueue(0);
            while (queue.Count > 0)
            {
                var a = queue.Dequeue();
                for (var b = 0; b < members.Count; b++)
                    if (!reached.Contains(b) && touch(a, b)) { reached.Add(b); queue.Enqueue(b); }
            }
            if (reached.Count < members.Count)
            {
                var apart = Enumerable.Range(0, members.Count).Where(i => !reached.Contains(i))
                    .Select(i => describe(members[i].Id)).ToList();
                problem = string.Format("The {0} members handed in do not all meet: {1} touch{2} none of {3} - one "
                    + "connection joins one joint, so nothing was placed. Select the members of one joint, or say "
                    + "layout \"each\" for one connection on each member alone", members.Count,
                    string.Join(", ", apart), apart.Count == 1 ? "es" : "", describe(members[0].Id));
            }
        }
        if (problem == null) groups.Add(members);
    }
}

// ---- the connections already in the model ----------------------------------------------

var standing = new List<StructuralConnectionHandler>();
var standingIds = new Dictionary<ElementId, IList<ElementId>>();
if (problem == null)
{
    foreach (var c in new FilteredElementCollector(doc).OfClass(typeof(StructuralConnectionHandler))
                 .Cast<StructuralConnectionHandler>())
    {
        IList<ElementId> ids = null;
        try { ids = c.GetConnectedElementIds(); } catch (Exception) { ids = null; }
        standing.Add(c);
        standingIds[c.Id] = ids ?? new List<ElementId>();
    }
}

// ---- the connections ------------------------------------------------------------------

for (var g = 0; problem == null && g < groups.Count; g++)
{
    var group = groups[g];
    var ids = group.Select(m => m.Id).ToList();
    var where = string.Format("Connection {0} ({1})", g + 1, string.Join(", ", ids.Select(describe)));

    // All of these already joined by one connection: left as it is.
    if (ids.Count > 1)
    {
        var holder = standing.FirstOrDefault(c => ids.All(id => standingIds[c.Id].Any(x => x == id)));
        if (holder != null)
        {
            alreadyConnected++;
            skipped.Add(string.Format("{0}: already joined by connection {1}, type \"{2}\" - left as it was, not "
                + "doubled", where, holder.Id, typeLabel(doc.GetElement(holder.GetTypeId()))));
            continue;
        }
    }

    using (var attempt = new SubTransaction(doc))
    {
        try
        {
            attempt.Start();
            var made = StructuralConnectionHandler.Create(doc, ids, useType.Id);
            if (made == null)
            {
                attempt.RollBack();
                problems.Add(where + ": NOT placed - Revit returned no connection");
                continue;
            }

            // Read back BEFORE it is kept.
            doc.Regenerate();
            var now = doc.GetElement(made.Id) as StructuralConnectionHandler;
            if (now == null || !now.IsValidObject)
            {
                attempt.RollBack();
                problems.Add(where + ": Revit took the call but the connection is NOT in the model afterwards");
                continue;
            }
            if (now.GetTypeId() != useType.Id)
            {
                attempt.RollBack();
                problems.Add(string.Format("{0}: Revit made the connection of another type than \"{1}\", so it was "
                    + "taken back", where, typeLabel(useType)));
                continue;
            }
            IList<ElementId> joins = null;
            try { joins = now.GetConnectedElementIds(); } catch (Exception) { joins = null; }
            joins = joins ?? new List<ElementId>();
            var missing = ids.Where(id => !joins.Any(x => x == id)).ToList();
            if (missing.Count > 0)
            {
                attempt.RollBack();
                problems.Add(string.Format("{0}: Revit made a connection that does not join {1}, so it was taken back",
                    where, string.Join(", ", missing.Select(describe))));
                continue;
            }

            var end = "";
            if (ids.Count == 1)
            {
                var index = -1;
                try { index = now.SingleElementEndIndex; } catch (Exception) { index = -1; }
                // One member alone: a single-member connection already at this end is not doubled.
                var twin = standing.FirstOrDefault(c =>
                {
                    var cids = standingIds[c.Id];
                    if (cids.Count != 1 || cids[0] != ids[0]) return false;
                    try { return c.SingleElementEndIndex == index; } catch (Exception) { return false; }
                });
                if (twin != null)
                {
                    attempt.RollBack();
                    alreadyConnected++;
                    skipped.Add(string.Format("{0}: already carries connection {1}, type \"{2}\", at the end Revit "
                        + "picked (end index {3}) - left as it was, not doubled", where, twin.Id,
                        typeLabel(doc.GetElement(twin.GetTypeId())), index));
                    continue;
                }
                if (index >= 0) end = string.Format(", at the member's {0} (end index {1})", index == 0 ? "start" : "end", index);
            }

            var detailed = false;
            try { detailed = now.IsDetailed(); } catch (Exception) { detailed = false; }

            attempt.Commit();
            created.Add(now.Id);
            var parts = new List<string>();
            for (var i = 0; i < joins.Count; i++)
                parts.Add(describe(joins[i]) + (i == 0 && joins.Count > 1 ? " (primary)" : ""));
            lines.Add(string.Format("{0}: placed ({1}), type \"{2}\"{3}, joins {4}: {5}{6}", where, now.Id,
                typeLabel(useType), detailed ? " (detailed)" : "", joins.Count, string.Join(", ", parts), end));
        }
        catch (Exception ex)
        {
            try { attempt.RollBack(); } catch (Exception) { }
            var why = ex.Message ?? "";
            if (why.IndexOf("detailed structural connection service", StringComparison.OrdinalIgnoreCase) >= 0)
                why += " - this type is a DETAILED connection, and Revit's Steel Connections are not available "
                    + "in this Revit to generate it";
            problems.Add(where + ": NOT placed - Revit refused: " + why);
        }
    }
}

// ---- the answer -----------------------------------------------------------------

var all = new List<string>(lines);
all.AddRange(skipped);
all.AddRange(problems);
report = string.Join("\n", all.ToArray());

if (problem == null)
    findings.Add(string.Format("{0} connection(s) placed, {1} skipped ({2} already connected), {3} Revit would not "
        + "take, of {4} asked for, layout {5}", created.Count, skipped.Count, alreadyConnected, problems.Count,
        groups.Count, each ? "each - one per member" : "together - one joining every member"));
foreach (var one in skipped) findings.Add(one);
foreach (var one in problems) findings.Add(one);
if (created.Count > 0)
    findings.Add("The first member each connection joins is its primary one - the column, where there is one. Its "
        + "plates, bolts and welds are the type's own; Revit's API does not reach them. Nothing was deleted");

if (created.Count == 0)
{
    if (problem != null)
        refused = problem;
    else if (alreadyConnected > 0 && problems.Count == 0)
        refused = string.Format("Every one of the {0} connection(s) asked for is already there - the report names "
            + "each", groups.Count);
    else
        refused = string.Format("No connection was placed: Revit would not take {0} and {1} were skipped - the report "
            + "names each and why", problems.Count, skipped.Count);
    refused = refused.TrimEnd('.') + ". Nothing was created.";
    findings.Add(refused);
}
