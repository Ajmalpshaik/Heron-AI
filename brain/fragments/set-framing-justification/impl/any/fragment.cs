// NOT STANDALONE. Assumes `doc`, `elements`, `yzJustification`, `whichEnd`,
// `yJustification`, `zJustification`, `yOffsetMm`, `zOffsetMm`,
// `startLevelOffsetMm` and `endLevelOffsetMm` are in scope; leaves `changed`,
// `settings`, `alreadyRight`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list.
//
// STRUCTURAL FRAMING'S JUSTIFICATION, AS PROPERTIES SHOWS IT. yz Justification
// is Uniform (one y and one z for the whole member) or Independent (a start and
// an end of each). y Justification is Origin, Left, Center or Right; z
// Justification Origin, Top, Center or Bottom; y / z Offset Value move the
// member off that line. Start / End Level Offset raise or lower each end from
// its reference level. Every value left out is left as it is, and at least one
// must be given.
//
// THE CHOICES ARE STORED AS WHOLE NUMBERS and the numbers are not on any page
// read here, so the number written is checked against the word Revit shows for
// it afterwards. If Revit shows another of the choice's own words, the value is
// put back and refused - never left as a guess.
//
// WHICH END IS NEVER GUESSED. A Uniform member has one y and one z, so `whichEnd`
// start or end on it is refused, naming yzJustification independent; an
// Independent member takes start, end or both (left out, both).
//
// MILLIMETRES IN, FEET TO REVIT: / 304.8 (D-20). Read back the same way, after
// one regeneration; a value Revit took but does not hold is refused, never
// counted. Start / End Extension and Join Cutback are SET_FRAMING_END_EXTENSION's
// and are never touched here.

var changed = 0;
var settings = new List<string>();
var alreadyRight = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

Func<string, string> clean = s =>
{
    var t = (s ?? "").Trim();
    // An optional need left out with `absent: none` arrives as the text
    // "none" (row 5b-1432): read it as not given.
    if (string.Equals(t, "none", StringComparison.OrdinalIgnoreCase)) t = "";
    return new string(t.ToLowerInvariant().Where(char.IsLetter).ToArray());
};
Func<Element, string> describe = e => string.Format("{0} '{1}' (id {2})",
    e.Category != null ? e.Category.Name : "element", e.Name, e.Id);
Func<double, string> mm = v => Math.Round(v, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " mm";

var yzWord = clean(yzJustification);
var endWord = clean(whichEnd);
if (endWord == "") endWord = "both";
var yWord = clean(yJustification);
if (yWord == "centre") yWord = "center";
var zWord = clean(zJustification);
if (zWord == "centre") zWord = "center";

// Revit's words for each choice, in the order of the number assumed for them.
var yzChoices = new[] { "uniform", "independent" };
var yChoices = new[] { "left", "center", "origin", "right" };
var zChoices = new[] { "top", "center", "origin", "bottom" };

Func<double, bool> given = v => !double.IsNaN(v);
Func<double, bool> bad = v => double.IsInfinity(v);

var anything = yzWord != "" || yWord != "" || zWord != "" || given(yOffsetMm) || given(zOffsetMm)
    || given(startLevelOffsetMm) || given(endLevelOffsetMm);

var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);
var members = new List<FamilyInstance>();

if (!anything)
    refused.Add("No setting was given - name a yz, y or z justification, a y or z offset, or a start or end "
        + "level offset. NOTHING WAS CHANGED");
else if (yzWord != "" && Array.IndexOf(yzChoices, yzWord) < 0)
    refused.Add(string.Format("yzJustification was '{0}' - say uniform or independent. NOTHING WAS CHANGED",
        yzJustification));
else if (endWord != "start" && endWord != "end" && endWord != "both")
    refused.Add(string.Format("whichEnd was '{0}' - say start, end or both. NOTHING WAS CHANGED", whichEnd));
else if (yWord != "" && Array.IndexOf(yChoices, yWord) < 0)
    refused.Add(string.Format("yJustification was '{0}' - say origin, left, center or right. NOTHING WAS CHANGED",
        yJustification));
else if (zWord != "" && Array.IndexOf(zChoices, zWord) < 0)
    refused.Add(string.Format("zJustification was '{0}' - say origin, top, center or bottom. NOTHING WAS CHANGED",
        zJustification));
else if (bad(yOffsetMm) || bad(zOffsetMm) || bad(startLevelOffsetMm) || bad(endLevelOffsetMm))
    refused.Add("An offset is not a number - give it in millimetres. NOTHING WAS CHANGED");
else if (elements == null || elements.Count == 0)
    refused.Add("No beams or braces were handed in - nothing was changed");
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var member = element as FamilyInstance;
        if (member == null || member.Category == null || member.Category.Id != framingCategory)
        {
            refused.Add(describe(element) + ": not structural framing - only beams and braces have a "
                + "yz justification");
            continue;
        }
        members.Add(member);
    }
}

// What was written and is still to be read back: member, parameter, label,
// what it was, what was asked (an int choice or millimetres), and whether the
// value is a choice.
var pending = new List<Tuple<FamilyInstance, BuiltInParameter, string, string, double, bool>>();

// One choice written and checked against Revit's own word. True when written.
Func<FamilyInstance, BuiltInParameter, string, string[], string, bool> setChoice = (member, bip, label, choices, word) =>
{
    var where = describe(member) + ", " + label;
    var p = member.get_Parameter(bip);
    if (p == null || p.StorageType != StorageType.Integer)
    {
        refused.Add(where + ": this member has no " + label + " - nothing was changed");
        return false;
    }
    var wanted = Array.IndexOf(choices, word);
    var before = p.AsInteger();
    var beforeText = p.AsValueString() ?? before.ToString();
    if (before == wanted)
    {
        alreadyRight.Add(where + ": already " + beforeText + " - left alone");
        return false;
    }
    if (p.IsReadOnly)
    {
        refused.Add(where + ": Revit has " + label + " greyed out on this member - it stays " + beforeText);
        return false;
    }
    try
    {
        if (!p.Set(wanted))
        {
            refused.Add(where + ": Revit refused " + word + " - it stays " + beforeText);
            return false;
        }
    }
    catch (Exception ex)
    {
        refused.Add(where + ": Revit would not take " + word + " - it stays " + beforeText + ". Revit said: "
            + ex.Message);
        return false;
    }
    var shown = (p.AsValueString() ?? "").ToLowerInvariant().Replace("centre", "center");
    var otherWord = choices.FirstOrDefault(c => c != word && shown.Contains(c));
    if (shown != "" && !shown.Contains(word) && otherWord != null)
    {
        try { p.Set(before); } catch { }
        refused.Add(where + ": Revit shows '" + p.AsValueString() + "' for the number Heron took to mean "
            + word + ", so it was put back to " + beforeText + " - this release numbers the choices "
            + "differently, and nothing is guessed");
        return false;
    }
    pending.Add(Tuple.Create(member, bip, label, beforeText, (double)wanted, true));
    return true;
};

// One length written in millimetres. True when written.
Func<FamilyInstance, BuiltInParameter, string, double, bool> setLength = (member, bip, label, valueMm) =>
{
    var where = describe(member) + ", " + label;
    var p = member.get_Parameter(bip);
    if (p == null || p.StorageType != StorageType.Double)
    {
        refused.Add(where + ": this member has no " + label + " - nothing was changed");
        return false;
    }
    var beforeMm = p.AsDouble() * 304.8;
    if (Math.Abs(beforeMm - valueMm) < 0.05)
    {
        alreadyRight.Add(where + ": already " + mm(beforeMm) + " - left alone");
        return false;
    }
    if (p.IsReadOnly)
    {
        refused.Add(where + ": Revit has " + label + " greyed out on this member - it stays " + mm(beforeMm));
        return false;
    }
    try
    {
        if (!p.Set(valueMm / 304.8))
        {
            refused.Add(where + ": Revit refused " + mm(valueMm) + " - it stays " + mm(beforeMm));
            return false;
        }
    }
    catch (Exception ex)
    {
        refused.Add(where + ": Revit would not take " + mm(valueMm) + " - it stays " + mm(beforeMm)
            + ". Revit said: " + ex.Message);
        return false;
    }
    pending.Add(Tuple.Create(member, bip, label, mm(beforeMm), valueMm, false));
    return true;
};

// FIRST PASS: yz Justification, then one regeneration, because it decides
// which of Revit's y and z settings the member shows.
if (yzWord != "")
{
    var any = false;
    foreach (var member in members)
        any |= setChoice(member, BuiltInParameter.YZ_JUSTIFICATION, "yz Justification", yzChoices, yzWord);
    if (any) doc.Regenerate();
}

var wantsYz = yWord != "" || zWord != "" || given(yOffsetMm) || given(zOffsetMm);
foreach (var member in members)
{
    if (wantsYz)
    {
        var yz = member.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION);
        var independent = yz != null && yz.AsInteger() == 1;
        if (!independent && endWord != "both")
        {
            refused.Add(describe(member) + ": its yz Justification is Uniform, so both ends share one y and z - "
                + "say yzJustification independent to set the " + endWord + " alone. Its y and z were not changed");
        }
        else
        {
            var ends = new List<int>();
            if (!independent) ends.Add(-1);
            else
            {
                if (endWord == "start" || endWord == "both") ends.Add(0);
                if (endWord == "end" || endWord == "both") ends.Add(1);
            }
            foreach (var end in ends)
            {
                var prefix = end == 0 ? "Start " : end == 1 ? "End " : "";
                if (yWord != "")
                    setChoice(member, end == 0 ? BuiltInParameter.START_Y_JUSTIFICATION
                        : end == 1 ? BuiltInParameter.END_Y_JUSTIFICATION : BuiltInParameter.Y_JUSTIFICATION,
                        prefix + "y Justification", yChoices, yWord);
                if (zWord != "")
                    setChoice(member, end == 0 ? BuiltInParameter.START_Z_JUSTIFICATION
                        : end == 1 ? BuiltInParameter.END_Z_JUSTIFICATION : BuiltInParameter.Z_JUSTIFICATION,
                        prefix + "z Justification", zChoices, zWord);
                if (given(yOffsetMm))
                    setLength(member, end == 0 ? BuiltInParameter.START_Y_OFFSET_VALUE
                        : end == 1 ? BuiltInParameter.END_Y_OFFSET_VALUE : BuiltInParameter.Y_OFFSET_VALUE,
                        prefix + "y Offset Value", yOffsetMm);
                if (given(zOffsetMm))
                    setLength(member, end == 0 ? BuiltInParameter.START_Z_OFFSET_VALUE
                        : end == 1 ? BuiltInParameter.END_Z_OFFSET_VALUE : BuiltInParameter.Z_OFFSET_VALUE,
                        prefix + "z Offset Value", zOffsetMm);
            }
        }
    }
    if (given(startLevelOffsetMm))
        setLength(member, BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION, "Start Level Offset", startLevelOffsetMm);
    if (given(endLevelOffsetMm))
        setLength(member, BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION, "End Level Offset", endLevelOffsetMm);
}

// READ BACK, after one regeneration.
if (pending.Count > 0)
{
    doc.Regenerate();
    foreach (var item in pending)
    {
        var where = describe(item.Item1) + ", " + item.Item3;
        var p = item.Item1.get_Parameter(item.Item2);
        if (p == null)
        {
            refused.Add(where + ": nothing readable after the change");
            continue;
        }
        if (item.Item6)
        {
            if (p.AsInteger() == (int)item.Item5)
            {
                changed++;
                settings.Add(string.Format("{0}: {1} -> {2}, read back", where, item.Item4,
                    p.AsValueString() ?? p.AsInteger().ToString()));
            }
            else
                refused.Add(string.Format("{0}: Revit took it but it reads {1}", where,
                    p.AsValueString() ?? p.AsInteger().ToString()));
        }
        else
        {
            var afterMm = p.AsDouble() * 304.8;
            if (Math.Abs(afterMm - item.Item5) < 0.5)
            {
                changed++;
                settings.Add(string.Format("{0}: {1} -> {2}, read back", where, item.Item4, mm(afterMm)));
            }
            else
                refused.Add(string.Format("{0}: Revit took {1} but it reads {2}", where, mm(item.Item5), mm(afterMm)));
        }
    }
}

findings.Add(string.Format("{0} setting(s) changed and read back on {1} member(s); {2} already there; {3} refused",
    changed, members.Count, alreadyRight.Count, refused.Count));
findings.AddRange(settings);
findings.AddRange(refused);
