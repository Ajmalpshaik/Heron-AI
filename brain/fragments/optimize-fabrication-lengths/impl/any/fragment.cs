// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `optimised`, `notFabrication`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S OPTIMIZE LENGTHS (Modify | Fabrication Parts): the straights of a
// fabrication run re-cut to the lengths the service allows, by the one call
// Revit has for it - FabricationPart.OptimizeLengths(Document, ISet<ElementId>)
// returning the new and modified part ids. Present 2020 to 2027 with one
// change: its document parameter is named doc to 2023 and document from 2024
// (tools/api-surface --members, 2026-11-02) - called by position, so one line
// serves every release.
//
// ONLY FABRICATION PARTS ARE SENT. A design duct or pipe (Design to
// Fabrication first), a wall, equipment - each goes in `notFabrication` by
// name and is left as it was. RevitAPI.xml: the call throws when "there are
// no straight parts to optimize, or all parts are locked", so a selection
// with no straight is refused by name before Revit is asked.
//
// ELEMENT OWNERSHIP IS A NORMAL OUTCOME (Article 12): a part borrowed by
// somebody else on a workshared model is named and never taken.
//
// READ BACK after a regenerate: every straight's length is read before; after,
// each id Revit returns is read as a part with its length, and every part
// handed in is looked at again - gone (Revit replaced it) or still there. The
// total straight length of the run before and after is compared: Optimize
// Lengths re-cuts a run, it should not lengthen or shorten it, so a
// difference over 1 mm is a finding, never hidden.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER. Lengths are feet
// x 304.8, said in mm (D-20).

var optimised = new List<ElementId>();
var notFabrication = new List<string>();
var findings = new List<string>();
string report = "";

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> named = e =>
    (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

var given = 0;
var seen = new HashSet<ElementId>();
var ids = new List<ElementId>();
var beforeLength = new Dictionary<ElementId, double>();
var label = new Dictionary<ElementId, string>();
var borrowed = new List<string>();

foreach (var e in elements ?? new List<Element>())
{
    if (e == null || !seen.Add(e.Id)) continue;
    given++;
    var fp = e as FabricationPart;
    if (fp == null)
    {
        var design = e is Duct || e is Pipe;
        notFabrication.Add(named(e) + ": not a fabrication part"
            + (design ? " - a design " + (e is Duct ? "duct" : "pipe") + "; Design to Fabrication makes it fabrication first" : "")
            + ". Left as it is");
        continue;
    }
    if (doc.IsWorkshared)
    {
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
        {
            borrowed.Add(named(e) + ": borrowed by " + (string.IsNullOrEmpty(owner) ? "another user" : owner)
                       + " - left alone; ask them to relinquish it");
            continue;
        }
    }
    ids.Add(e.Id);
    label[e.Id] = named(e);
    if (fp.IsAStraight()) beforeLength[e.Id] = fp.CenterlineLength;
}

var refused = "";
if (given == 0)
    refused = "Nothing was handed in - select the fabrication parts of the run to optimise. Nothing was changed";
else if (ids.Count == 0)
    refused = "None of the " + given + " element(s) given is a fabrication part that can be optimised - "
            + string.Join("; ", notFabrication.Concat(borrowed).ToArray()) + ". Nothing was changed";
else if (beforeLength.Count == 0)
    refused = "None of the " + ids.Count + " fabrication part(s) given is a straight - Optimize Lengths re-cuts "
            + "straights only. Nothing was changed";

var returned = new List<ElementId>();
if (refused.Length == 0)
{
    try
    {
        var back = FabricationPart.OptimizeLengths(doc, new HashSet<ElementId>(ids));
        if (back != null) returned.AddRange(back);
    }
    // `catch (Exception)` because Revit's exceptions namespace is not among the
    // ones the executor imports, as in every other fragment here. Narrow in
    // REACH: it wraps the one call and nothing else, and says Revit's words.
    catch (Exception ex)
    {
        refused = "Revit refused to optimise the lengths - " + ex.Message + ". Nothing was changed";
    }
}

// ---- read back ----------------------------------------------------------------------------
var partLines = new List<string>();
var gone = 0;
double totalBefore = beforeLength.Values.Sum();
double totalAfter = 0;
var afterStraights = 0;
if (refused.Length == 0)
{
    doc.Regenerate();
    var counted = new HashSet<ElementId>();
    foreach (var id in returned.Distinct())
    {
        var fp = doc.GetElement(id) as FabricationPart;
        if (fp == null) continue;
        optimised.Add(id);
        if (fp.IsAStraight() && counted.Add(id))
        {
            totalAfter += fp.CenterlineLength;
            afterStraights++;
            partLines.Add("id " + id + " straight " + mm(fp.CenterlineLength) + " mm");
        }
    }
    foreach (var id in beforeLength.Keys)
    {
        var fp = doc.GetElement(id) as FabricationPart;
        if (fp == null) { gone++; continue; }
        if (counted.Add(id))
        {
            totalAfter += fp.CenterlineLength;
            afterStraights++;
        }
    }
    if (Math.Abs(totalAfter - totalBefore) * 304.8 > 1.0)
        findings.Add("The straights handed in totalled " + mm(totalBefore) + " mm before and the straights read after total "
            + mm(totalAfter) + " mm - Optimize Lengths should keep the run's length; look at the run");
    if (returned.Count == 0)
        findings.Add("Revit returned no new or changed part - the straights may already be at the lengths the service allows");
}

if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
else
    report = "Optimize Lengths on " + ids.Count + " fabrication part(s), " + beforeLength.Count + " of them straights totalling "
           + mm(totalBefore) + " mm. Revit returned " + optimised.Count + " new or changed part(s)"
           + (partLines.Count > 0 ? ": " + string.Join("; ", partLines.ToArray()) : "")
           + ". After: " + afterStraights + " straight(s) totalling " + mm(totalAfter) + " mm; "
           + gone + " of the straights handed in are no longer in the model (Revit replaced them)"
           + (borrowed.Count > 0 ? ". Not optimised - " + string.Join("; ", borrowed.ToArray()) : "")
           + ". " + notFabrication.Count + " left alone as not fabrication parts"
           + (notFabrication.Count > 0 ? " - " + string.Join("; ", notFabrication.ToArray()) : "");
foreach (var b in borrowed) findings.Add(b);
