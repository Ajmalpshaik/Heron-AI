// NOT STANDALONE. Assumes `doc` and `parameterNames` are in scope; leaves
// `deleted`, `blocked`, `notFound`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ===========================================================================
// IT DELETES ONLY WHAT NOTHING ELSE LEANS ON, AND IT NEVER CASCADES
// ===========================================================================
//
// A family parameter is rarely alone. Another parameter's formula reads it, a
// dimension is labelled with it, a form's Extrusion End or Visible or Material
// is linked to it, a nested family's parameter or a connector's size follows
// it, an array's count is it. Taking it out from under any of those changes
// the family's shape or behaviour somewhere the modeller is not looking. So
// every one is looked for FIRST, and a parameter with any of them is refused
// BY NAME with what still uses it - the modeller unhooks that by hand, or with
// the tool that hooked it, and asks again. Nothing is unhooked here.
//
// A parameter whose OWN formula is set, and which nothing else uses, can go:
// its formula reads others, nobody reads it.
//
// A formula user that is ITSELF in the list does not block - asking for both
// is asking for both - and the users are removed first so Revit never sees a
// formula pointing at a parameter that has gone.
//
// ALL OR NOTHING. One refused name refuses the whole call before the first
// removal, and a removal Revit refuses afterwards THROWS so the host rolls the
// call back: a list half-deleted is harder to see than one not touched.

var findings = new List<string>();
var deleted = new List<string>();
var blocked = new List<string>();
var notFound = new List<string>();
var notAFamily = false;
string refused = null;

// "Cap R Start", "cap r start" and "CapRStart" are near each other; used only
// to OFFER names, never to pick one.
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<string, string, int> distance = (a, b) =>
{
    var row = new int[b.Length + 1];
    for (var j = 0; j <= b.Length; j++) row[j] = j;
    for (var i = 1; i <= a.Length; i++)
    {
        var diagonal = row[0];
        row[0] = i;
        for (var j = 1; j <= b.Length; j++)
        {
            var above = row[j];
            row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1),
                diagonal + (a[i - 1] == b[j - 1] ? 0 : 1));
            diagonal = above;
        }
    }
    return row[b.Length];
};

// THE NAME AS A WHOLE TOKEN IN A FORMULA. "Width" must not be found inside
// "Width Offset", so every LONGER parameter name that contains it is blanked
// out of the formula first; then the name counts only with no letter, digit
// or underscore touching either end. Case is ignored on purpose: a refusal
// that is one too cautious costs a sentence, a deletion that broke a formula
// costs the family.
Func<string, string, IEnumerable<string>, bool> formulaUses = (formula, name, allNames) =>
{
    if (string.IsNullOrEmpty(formula) || string.IsNullOrEmpty(name)) return false;
    var text = formula;
    foreach (var longer in allNames.Where(n => n.Length > name.Length
        && n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(n => n.Length))
    {
        var at = text.IndexOf(longer, StringComparison.OrdinalIgnoreCase);
        while (at >= 0)
        {
            text = text.Substring(0, at) + new string('#', longer.Length) + text.Substring(at + longer.Length);
            at = text.IndexOf(longer, at + longer.Length, StringComparison.OrdinalIgnoreCase);
        }
    }
    Func<char, bool> joins = c => char.IsLetterOrDigit(c) || c == '_' || c == '#';
    var from = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
    while (from >= 0)
    {
        var end = from + name.Length;
        var before = from == 0 || !joins(text[from - 1]);
        var after = end >= text.Length || !joins(text[end]);
        if (before && after) return true;
        from = text.IndexOf(name, from + 1, StringComparison.OrdinalIgnoreCase);
    }
    return false;
};

// ---------------------------------------------------------------------------
// CHECK EVERYTHING BEFORE THE FIRST REMOVAL
// ---------------------------------------------------------------------------

var names = new List<string>();
if (parameterNames != null)
    foreach (var raw in parameterNames)
    {
        var trimmed = raw == null ? "" : raw.Trim();
        if (trimmed.Length > 0 && !names.Contains(trimmed, StringComparer.Ordinal)) names.Add(trimmed);
    }

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so it has "
        + "no family parameters to delete. Nothing was changed. To work on a family, open it first.";
}
else if (names.Count == 0)
{
    refused = "No parameter names were given - comma separated, \"Cap R Start, Body Material\".";
}

var fm = doc.IsFamilyDocument ? doc.FamilyManager : null;
var targets = new Dictionary<string, FamilyParameter>(StringComparer.Ordinal);

if (refused == null)
{
    var all = new List<FamilyParameter>();
    foreach (FamilyParameter p in fm.Parameters) all.Add(p);
    var allNames = all.Select(p => p.Definition.Name).ToList();

    // FOUND BY ITS EXACT NAME, through FamilyManager.Parameters. A near name
    // is offered, never taken: deleting the parameter next to the one meant
    // is the mistake this tool must not make.
    foreach (var name in names)
    {
        var match = all.FirstOrDefault(p => string.Equals(p.Definition.Name, name, StringComparison.Ordinal));
        if (match != null) { targets[name] = match; continue; }

        var key = squash(name);
        var near = allNames
            .Where(n => squash(n) == key
                || (key.Length > 2 && squash(n).Contains(key))
                || (squash(n).Length > 2 && key.Contains(squash(n)))
                || distance(n.ToLowerInvariant(), name.ToLowerInvariant()) <= Math.Max(2, name.Length / 4))
            .OrderBy(n => distance(n.ToLowerInvariant(), name.ToLowerInvariant()))
            .Take(6)
            .ToList();
        notFound.Add("\"" + name + "\" is not a parameter of this family"
            + (near.Count > 0 ? " - near names: " + string.Join(", ", near.Select(n => "\"" + n + "\"")) : "")
            + ".");
    }

    // WHAT LABELS A PARAMETER: dimensions, and arrays whose count it is. One
    // pass over the family's elements - a family is small - with `as`, so no
    // class filter has to be trusted with an abstract type.
    var dimensionsOf = new Dictionary<string, int>(StringComparer.Ordinal);
    var arraysOf = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        FamilyParameter label = null;
        var dimension = element as Dimension;
        var array = element as BaseArray;
        try
        {
            if (dimension != null) label = dimension.FamilyLabel;
            else if (array != null) label = array.Label;
        }
        catch (Exception) { label = null; }   // a spot dimension takes no label, and says so by throwing
        if (label == null) continue;
        var labelName = label.Definition.Name;
        var counts = dimension != null ? dimensionsOf : arraysOf;
        counts[labelName] = (counts.ContainsKey(labelName) ? counts[labelName] : 0) + 1;
    }

    foreach (var entry in targets)
    {
        var name = entry.Key;
        var p = entry.Value;
        var reasons = new List<string>();

        // BUILT-IN: the category's own, which Revit owns and will not let go.
        var internalDefinition = p.Definition as InternalDefinition;
        if (internalDefinition != null && internalDefinition.BuiltInParameter != BuiltInParameter.INVALID)
            reasons.Add("it is one of Revit's built-in parameters for this category, which cannot be deleted");

        var formulaUsers = all
            .Where(other => !ReferenceEquals(other, p) && !other.Id.Equals(p.Id))
            .Where(other => !targets.ContainsKey(other.Definition.Name))
            .Where(other => formulaUses(other.Formula, name, allNames))
            .Select(other => "\"" + other.Definition.Name + "\" = " + other.Formula)
            .ToList();
        if (formulaUsers.Count > 0)
            reasons.Add("another parameter's formula uses it: " + string.Join("; ", formulaUsers)
                + " - change or clear that formula first (SET_FAMILY_FORMULA)");

        if (dimensionsOf.ContainsKey(name))
            reasons.Add("it labels " + dimensionsOf[name] + " dimension(s) - take the label off first "
                + "(select the dimension, Label: <None>)");

        if (arraysOf.ContainsKey(name))
            reasons.Add("it is the count of " + arraysOf[name] + " array(s) - take that label off first");

        // LINKED: a form's Extrusion Start/End, Visible or Material, a nested
        // family's parameter, a connector's size - every element parameter
        // whose "Associate Family Parameter" button points here.
        var nested = new List<string>();
        var connectors = new List<string>();
        var forms = new List<string>();
        foreach (Parameter linked in p.AssociatedParameters)
        {
            var owner = linked.Element;
            var field = linked.Definition == null ? "a parameter" : "\"" + linked.Definition.Name + "\"";
            var ownerName = owner == null ? "an element" : (owner.Name ?? "").Trim();
            if (owner is FamilyInstance || owner is ElementType)
                nested.Add(field + " of nested " + (ownerName.Length > 0 ? "\"" + ownerName + "\"" : "family"));
            else if (owner != null && owner.GetType().Name == "ConnectorElement")
                connectors.Add(field + " of a connector");
            else
                forms.Add(field + " of " + (owner == null || owner.Category == null
                    ? "an element" : owner.Category.Name + (ownerName.Length > 0 ? " \"" + ownerName + "\"" : "")));
        }
        if (connectors.Count > 0)
            reasons.Add("it sizes a connector: " + string.Join("; ", connectors)
                + " - unlink it in the connector's properties first");
        if (nested.Count > 0)
            reasons.Add("a nested family's parameter is linked to it: " + string.Join("; ", nested)
                + " - unlink it with the small button beside that parameter first");
        if (forms.Count > 0)
            reasons.Add("a form field is linked to it: " + string.Join("; ", forms)
                + " - unlink it with the small button beside that field first");

        if (reasons.Count > 0)
            blocked.Add("\"" + name + "\" was NOT deleted, because " + string.Join("; and ", reasons) + ".");
    }

    if (notFound.Count > 0 || blocked.Count > 0)
    {
        refused = "Nothing was deleted - every name is checked before the first one goes, and "
            + (notFound.Count + blocked.Count) + " of " + names.Count + " could not go. "
            + string.Join(" ", notFound.Concat(blocked))
            + " Nothing was unhooked: deleting a parameter never takes what uses it along with it.";
    }
}

// ---------------------------------------------------------------------------
// DELETE, USERS FIRST, THEN READ BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var allNames = new List<string>();
    foreach (FamilyParameter p in fm.Parameters) allNames.Add(p.Definition.Name);

    // A parameter goes once no parameter LEFT IN THE LIST reads it. Revit
    // formulas cannot loop, so this always finishes; the guard is for the day
    // it meets one that does.
    var left = new List<string>(targets.Keys);
    var order = new List<string>();
    while (left.Count > 0)
    {
        var free = left.Where(n => !left.Any(user => user != n
            && formulaUses(targets[user].Formula, n, allNames))).ToList();
        if (free.Count == 0)
            throw new InvalidOperationException("The formulas among " + string.Join(", ", left)
                + " read each other in a circle, so none could go first. Nothing was deleted.");
        order.AddRange(free);
        left.RemoveAll(n => free.Contains(n));
    }

    foreach (var name in order)
    {
        var p = targets[name];
        var what = (p.IsInstance ? "instance" : "type")
            + (p.IsShared ? ", shared" : "")
            + (string.IsNullOrEmpty(p.Formula) ? "" : ", its own formula " + p.Formula);
        try
        {
            fm.RemoveParameter(p);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused to delete \"" + name + "\": " + ex.Message
                + " NOTHING from this call was kept - the parameters in it are deleted together or "
                + "not at all.");
        }
        deleted.Add(name + " (" + what + ")");
    }

    // READ BACK BY NAME. The call returning is not the family being without it.
    var stillThere = new List<string>();
    foreach (FamilyParameter p in fm.Parameters)
        if (targets.ContainsKey(p.Definition.Name)) stillThere.Add(p.Definition.Name);
    if (stillThere.Count > 0)
        throw new InvalidOperationException("Revit said it deleted " + string.Join(", ", stillThere)
            + " and the family still has them, so nothing from this call was kept.");

    findings.Add("Deleted " + deleted.Count + " parameter(s), and read back that the family no longer "
        + "has them: " + string.Join("; ", deleted) + ".");
    findings.Add("One Undo puts them all back, with their values in every type.");
}

if (refused != null) findings.Add(refused);
