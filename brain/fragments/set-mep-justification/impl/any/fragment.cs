// NOT STANDALONE. Assumes `doc`, `elements`, `horizontal` and `vertical` are in
// scope; leaves `set`, `unchanged`, `withoutJustification` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// VERSION 2 - THE TWO PARAMETERS ARE THE JUSTIFICATION CHOICES, NOT OFFSETS.
// Version 1 wrote a length in feet into `RBS_CURVE_HOR_OFFSET_PARAM` and
// `RBS_CURVE_VERT_OFFSET_PARAM`. Revit's own API names them "Horizontal
// Justification" and "Vertical Justification" (RevitAPI.xml, 2020 and 2024):
// the Center / Left / Right and Middle / Bottom / Top choices in Properties,
// stored as a whole number. A length written into a whole-number parameter is
// refused without a word, which is why only 0 mm ever "took" - it equalled
// the Center or Middle already there (rows 18, 45, 5b-1112, 5b-1397, 5b-1409).
//
// THE CHOICE IS FOUND BY REVIT'S OWN WORD, NEVER BY A GUESSED NUMBER. Which
// whole number means Top is not written in any reference this was checked
// against, so each number Revit accepts is tried and the one whose shown word
// is the word asked is kept. The number found is tried first on the next run.
// Nothing Revit shows matches the word: the old choice is put back and the
// run is refused with the words Revit does offer (a Revit in another
// language shows its own words, and is refused the same way, never guessed).
//
// A RUN ALREADY AT THE CHOICE IS COUNTED APART, never as set. Version 1 counted
// writing the value already there as a change (row 5b-1112).
//
// WHETHER CHANGING THE CHOICE MOVES A SEGMENT ALREADY DRAWN HAS NOT BEEN
// WATCHED. Revit's own Justification Editor is what lines up drawn runs; the
// parameter is what Properties changes. The reply says so every time.
//
// AN ELEMENT WITHOUT THE PARAMETERS IS COUNTED, NOT SKIPPED. Fittings,
// accessories and flex do not carry them.

Func<string, string> norm = s => (s ?? "").Trim().ToLowerInvariant();

var set = 0;
var unchanged = 0;
var withoutJustification = 0;
var findings = new List<string>();

var wantH = norm(horizontal);
var wantV = norm(vertical);
if (wantH == "" || wantH == "leave") wantH = null;
if (wantV == "" || wantV == "leave") wantV = null;
if (wantH == "centre") wantH = "center";
if (wantV == "centre" || wantV == "center") wantV = "middle";

var hWords = new[] { "center", "left", "right" };
var vWords = new[] { "middle", "bottom", "top" };

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given - hand in the ducts or pipes to set out. Nothing was changed.");
}
else if (wantH == null && wantV == null)
{
    findings.Add("No justification was asked for - give horizontal (Center, Left or Right) and/or "
        + "vertical (Middle, Bottom or Top). Nothing was changed.");
}
else if (wantH != null && Array.IndexOf(hWords, wantH) < 0)
{
    findings.Add("Horizontal justification '" + horizontal + "' is not one Revit offers - give Center, "
        + "Left or Right, or leave it out. Nothing was changed.");
}
else if (wantV != null && Array.IndexOf(vWords, wantV) < 0)
{
    findings.Add("Vertical justification '" + vertical + "' is not one Revit offers - give Middle, "
        + "Bottom or Top, or leave it out. Nothing was changed.");
}
else
{
    var refused = new List<string>();
    var notCurves = 0;
    var found = new Dictionary<string, int>();      // word asked -> the number Revit showed it for

    // Returns "set", "already" or a reason it could not. Leaves the parameter
    // as it was when it cannot.
    Func<Parameter, string, string, string> choose = (p, word, axis) =>
    {
        if (p == null) return "it carries no " + axis + " justification";
        if (p.IsReadOnly) return axis + " justification is read-only on it";
        if (p.StorageType != StorageType.Integer)
            return axis + " justification is stored as " + p.StorageType + ", not a choice";

        var before = p.AsInteger();
        if (norm(p.AsValueString()) == word) return "already";

        var tries = new List<int>();
        int known;
        if (found.TryGetValue(axis + ":" + word, out known)) tries.Add(known);
        for (var v = 0; v <= 5; v++) if (!tries.Contains(v) && v != before) tries.Add(v);

        var shown = new List<string>();
        foreach (var v in tries)
        {
            bool took;
            try { took = p.Set(v); } catch (Exception) { took = false; }
            if (!took) continue;
            var text = p.AsValueString() ?? "";
            if (text.Length > 0 && !shown.Contains(text)) shown.Add(text);
            if (norm(text) == word && p.AsInteger() == v)
            {
                found[axis + ":" + word] = v;
                return "set";
            }
        }

        try { p.Set(before); } catch (Exception) { }
        return axis + " justification has no choice Revit shows as '" + word + "'"
            + (shown.Count > 0 ? " - Revit offers " + string.Join(", ", shown) : "");
    };

    foreach (var element in elements)
    {
        if (element == null) continue;

        var curve = element as MEPCurve;
        if (curve == null) { notCurves++; continue; }

        var hp = curve.get_Parameter(BuiltInParameter.RBS_CURVE_HOR_OFFSET_PARAM);
        var vp = curve.get_Parameter(BuiltInParameter.RBS_CURVE_VERT_OFFSET_PARAM);
        if ((wantH == null || hp == null) && (wantV == null || vp == null))
        {
            withoutJustification++;
            continue;
        }

        var hOld = hp != null && hp.StorageType == StorageType.Integer ? hp.AsInteger() : 0;
        var vOld = vp != null && vp.StorageType == StorageType.Integer ? vp.AsInteger() : 0;
        var hResult = "already";
        var vResult = "already";
        try
        {
            if (wantH != null) hResult = choose(hp, wantH, "horizontal");
            if (wantV != null) vResult = choose(vp, wantV, "vertical");
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("id {0}: {1}", element.Id, ex.Message));
            continue;
        }

        var reasons = new List<string>();
        if (hResult != "set" && hResult != "already") reasons.Add(hResult);
        if (vResult != "set" && vResult != "already") reasons.Add(vResult);
        if (reasons.Count > 0)
        {
            // One axis taken and the other not is not half a run set: put the
            // taken one back, so a refused run is left exactly as it was.
            if (hResult == "set") { try { hp.Set(hOld); } catch (Exception) { } }
            if (vResult == "set") { try { vp.Set(vOld); } catch (Exception) { } }
            refused.Add(string.Format("id {0}: {1}", element.Id, string.Join("; ", reasons)));
        }
        else if (hResult == "set" || vResult == "set") set++;
        else unchanged++;
    }

    var asked = new List<string>();
    if (wantH != null) asked.Add("horizontal " + wantH);
    if (wantV != null) asked.Add("vertical " + wantV);

    findings.Add(string.Format("{0} run(s) set to {1}. {2} were already set that way and were not "
        + "changed. {3} carry no justification to set, and {4} of what was handed in are not duct, "
        + "pipe, conduit or cable tray runs",
        set, string.Join(" and ", asked), unchanged, withoutJustification, notCurves));

    foreach (var failure in refused) findings.Add("Refused: " + failure);

    findings.Add("This sets each run's Justification as Properties does. Whether a run already "
        + "drawn moves when it changes has not been watched on a model - Revit's Justification "
        + "Editor is what lines drawn runs up - so look at a size change in section before trusting "
        + "a batch of this");
}
