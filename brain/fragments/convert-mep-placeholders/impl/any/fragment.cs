// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `converted`, `fittingsMade`, `notPlaceholders`, `notConverted`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S CONVERT PLACEHOLDER: duct placeholders become real ducts and pipe
// placeholders real pipes, by the two calls Revit has for it, unchanged 2020
// to 2027 (read with tools/api-surface --members on 2026-11-01):
//   MechanicalUtils.ConvertDuctPlaceholders(doc, ICollection<ElementId>)
//   PlumbingUtils.ConvertPipePlaceholders(doc, ICollection<ElementId>)
// Each returns an ICollection<ElementId>. WHAT THAT COLLECTION HOLDS - only the
// new ducts or pipes, or their fittings too - and whether a converted element
// keeps its id or is replaced, is NOT KNOWN from the assemblies, so neither is
// assumed: every id returned is read and sorted by what it is, and every
// placeholder handed in is looked at again afterwards.
//
// ONLY PLACEHOLDERS ARE TOUCHED, AND EVERYTHING ELSE IS NAMED. Anything that is
// not a duct or a pipe, and every duct or pipe that is already real
// (Duct.IsPlaceholder / Pipe.IsPlaceholder false - the property is declared on
// Duct and on Pipe, not on MEPCurve), goes in `notPlaceholders` with its
// category, type name and id, and is left exactly as it was. Revit throws for
// a non-placeholder handed to either call, so this is also what keeps one
// wrong element from refusing the whole batch.
//
// ELEMENT OWNERSHIP IS A NORMAL OUTCOME (Article 12). On a workshared model a
// placeholder borrowed by somebody else is named in `notConverted` with who
// has it, and never taken.
//
// DUCT AND PIPE PLACEHOLDERS GO TO REVIT IN ONE CALL EACH, so placeholders
// joined to one another are converted together, as Revit's own button does.
// A call Revit refuses names every placeholder it carried in `notConverted`,
// with Revit's words, and the other discipline still runs.
//
// READ BACK after a regenerate: a placeholder handed in that is still a
// placeholder is named in `notConverted`; each real duct or pipe Revit
// returned is counted in `converted` with its length; anything else it
// returned - a fitting - in `fittingsMade`. Ids stay ElementId, never numbers.

var invariant = System.Globalization.CultureInfo.InvariantCulture;

var converted = new List<ElementId>();
var fittingsMade = new List<ElementId>();
var notPlaceholders = new List<string>();
var notConverted = new List<string>();
var findings = new List<string>();
string report = "";

Func<double, string> mm = feet => (feet * 304.8).ToString("0", invariant);
Func<Element, string> named = e =>
    (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";
Func<Element, string> lengthOf = e =>
{
    var line = e.Location as LocationCurve;
    return line == null || line.Curve == null ? "length unread" : mm(line.Curve.Length) + " mm";
};
Func<Element, bool?> placeholderState = e =>
{
    var asDuct = e as Duct;
    if (asDuct != null) return asDuct.IsPlaceholder;
    var asPipe = e as Pipe;
    if (asPipe != null) return asPipe.IsPlaceholder;
    return null;
};

// ---- sort what was handed in ------------------------------------------------------------
var ductIds = new List<ElementId>();
var pipeIds = new List<ElementId>();
var label = new Dictionary<ElementId, string>();
var seen = new HashSet<ElementId>();
var given = 0;

foreach (var e in elements ?? new List<Element>())
{
    if (e == null || !seen.Add(e.Id)) continue;
    given++;
    var state = placeholderState(e);
    if (state == null)
    {
        notPlaceholders.Add(named(e) + ": not a duct or pipe placeholder - only those can be converted, so it was left as it is");
        continue;
    }
    var isDuct = e is Duct;
    if (state == false)
    {
        notPlaceholders.Add(named(e) + ": already a real " + (isDuct ? "duct" : "pipe")
                          + ", not a placeholder - left as it is");
        continue;
    }
    if (doc.IsWorkshared)
    {
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
        {
            notConverted.Add(named(e) + ": borrowed by " + (string.IsNullOrEmpty(owner) ? "another user" : owner)
                           + " - left alone; ask them to relinquish it");
            continue;
        }
    }
    label[e.Id] = named(e) + ", " + lengthOf(e);
    (isDuct ? ductIds : pipeIds).Add(e.Id);
}

// ---- convert, one call per discipline ---------------------------------------------------
var returned = new List<ElementId>();
var failedIds = new HashSet<ElementId>();
Action<List<ElementId>, bool> convert = (ids, isDuct) =>
{
    if (ids.Count == 0) return;
    try
    {
        var back = isDuct
            ? MechanicalUtils.ConvertDuctPlaceholders(doc, ids)
            : PlumbingUtils.ConvertPipePlaceholders(doc, ids);
        if (back != null) returned.AddRange(back);
    }
    catch (Exception ex)
    {
        foreach (var id in ids)
        {
            failedIds.Add(id);
            notConverted.Add(label[id] + ": Revit refused to convert it - " + ex.Message);
        }
    }
};
convert(ductIds, true);
convert(pipeIds, false);

// ---- read back ----------------------------------------------------------------------------
var lines = new List<string>();
if (ductIds.Count + pipeIds.Count > failedIds.Count)
{
    doc.Regenerate();

    foreach (var id in returned.Distinct())
    {
        var made = doc.GetElement(id);
        if (made == null) continue;
        var state = placeholderState(made);
        if (state == false)
        {
            converted.Add(id);
            lines.Add((made is Duct ? "duct" : "pipe") + " id " + id + " " + lengthOf(made));
        }
        else if (state == true)
            findings.Add("id " + id + " came back from Revit's convert still a placeholder");
        else
            fittingsMade.Add(id);
    }

    // Every placeholder handed in, looked at again: gone (Revit replaced it),
    // real now (converted where it stood), or still a placeholder (not converted).
    foreach (var id in ductIds.Concat(pipeIds))
    {
        if (failedIds.Contains(id)) continue;
        var after = doc.GetElement(id);
        if (after == null) continue;
        var state = placeholderState(after);
        if (state == true)
            notConverted.Add(label[id] + ": still a placeholder after Revit's convert - Revit gave no reason");
        else if (state == false && !converted.Contains(id))
        {
            converted.Add(id);
            lines.Add((after is Duct ? "duct" : "pipe") + " id " + id + " " + lengthOf(after));
        }
    }
}

// ---- the answer ---------------------------------------------------------------------------
var placeholdersGiven = ductIds.Count + pipeIds.Count;
if (given == 0)
    report = "Nothing was handed in - select the duct or pipe placeholders to convert. Nothing was converted";
else if (placeholdersGiven == 0 && notConverted.Count == 0)
    report = "None of the " + given + " element(s) given is a duct or pipe placeholder - "
           + string.Join("; ", notPlaceholders.ToArray()) + ". Nothing was converted";
else
    report = placeholdersGiven + " placeholder(s) sent to Revit's convert (" + ductIds.Count + " duct, "
           + pipeIds.Count + " pipe); " + converted.Count + " real duct(s) or pipe(s) now stand"
           + (lines.Count > 0 ? ": " + string.Join("; ", lines.ToArray()) : "")
           + (fittingsMade.Count > 0 ? ". Revit also made " + fittingsMade.Count + " fitting(s): "
                + string.Join(", ", fittingsMade.Select(f => "id " + f).ToArray()) : "")
           + ". " + notConverted.Count + " not converted"
           + (notConverted.Count > 0 ? " - " + string.Join("; ", notConverted.ToArray()) : "")
           + ". " + notPlaceholders.Count + " left alone as not placeholders"
           + (notPlaceholders.Count > 0 ? " - " + string.Join("; ", notPlaceholders.ToArray()) : "");
