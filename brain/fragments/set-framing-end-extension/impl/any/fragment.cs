// NOT STANDALONE. Assumes `doc`, `elements`, `setting`, `whichEnd` and
// `lengthMm` are in scope; leaves `changed`, `ends`, `alreadyRight`, `refused`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list.
//
// STRUCTURAL FRAMING'S END SETTINGS, BY THE END. `setting` extension is Start
// Extension / End Extension - how far Revit draws the member past that end of
// its location line; cutback is Start Join Cutback / End Join Cutback - how far
// it stops short of the member it is joined to. Two settings, so there is no
// default, and any other word is refused before anything changes.
//
// WHICH END IS NEVER GUESSED. start is end 0 and end is end 1 of the
// location line; both is both; joined is each end Revit has joined to another
// member, read off the beam's own location line. A beam with no joined end is
// refused under joined, by name.
//
// MILLIMETRES IN, FEET TO REVIT: / 304.8 (D-20). Read back the same way.
//
// A GREYED-OUT PARAMETER IS REVIT'S ANSWER, NOT AN OBSTACLE. A join cutback at
// an end that is not joined is read-only in Revit; it is refused in words and
// joining is never turned on here - that is DISALLOW_JOIN's.
//
// READ BACK. Every end set is read again after one regeneration; a value Revit
// accepted but does not hold is refused, never counted.

var changed = 0;
var ends = new List<string>();
var alreadyRight = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

Func<Element, string> describe = e => string.Format("{0} '{1}' (id {2})",
    e.Category != null ? e.Category.Name : "element", e.Name, e.Id);
Func<double, string> mm = v => Math.Round(v, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " mm";

var what = new string((setting ?? "").ToLowerInvariant().Where(char.IsLetter).ToArray());
var isExtension = what == "extension" || what == "extend" || what == "endextension";
var isCutback = what == "cutback" || what == "joincutback";
var side = (whichEnd ?? "").Trim().ToLowerInvariant();
var sideKnown = side == "start" || side == "end" || side == "both" || side == "joined";

var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);

if (!isExtension && !isCutback)
{
    refused.Add(string.Format("setting was '{0}' - say extension or cutback. NOTHING WAS CHANGED", setting));
}
else if (!sideKnown)
{
    refused.Add(string.Format("whichEnd was '{0}' - say start, end, both, or joined (each end joined to "
        + "another member). Which end is never guessed. NOTHING WAS CHANGED", whichEnd));
}
else if (double.IsNaN(lengthMm) || double.IsInfinity(lengthMm))
{
    refused.Add("lengthMm is not a number - give the distance in millimetres. NOTHING WAS CHANGED");
}
else if (elements == null || elements.Count == 0)
{
    refused.Add("No beams or braces were handed in - nothing was changed");
}
else
{
    var pending = new List<Tuple<FamilyInstance, BuiltInParameter, string, double, string>>();

    foreach (var element in elements)
    {
        if (element == null) continue;
        var member = element as FamilyInstance;
        if (member == null || member.Category == null || member.Category.Id != framingCategory)
        {
            refused.Add(describe(element) + ": not structural framing - only beams and braces have an end "
                + "extension or a join cutback");
            continue;
        }

        var chosen = new List<int>();
        var meets = new Dictionary<int, string>();
        if (side == "start" || side == "both") chosen.Add(0);
        if (side == "end" || side == "both") chosen.Add(1);
        if (side == "joined")
        {
            var line = member.Location as LocationCurve;
            if (line == null)
            {
                refused.Add(describe(member) + ": has no location line, so which end is joined cannot be read - "
                    + "name start, end or both");
                continue;
            }
            for (int end = 0; end <= 1; end++)
            {
                try
                {
                    var array = line.get_ElementsAtJoin(end);
                    var others = new List<string>();
                    if (array != null)
                        foreach (Element other in array)
                            if (other != null && other.Id != member.Id) others.Add(describe(other));
                    if (others.Count > 0) { chosen.Add(end); meets[end] = string.Join(", ", others); }
                }
                catch { }
            }
            if (chosen.Count == 0)
            {
                refused.Add(describe(member) + ": neither end is joined to another member, so there is no "
                    + "joined end to set - nothing was changed on it");
                continue;
            }
        }

        foreach (var end in chosen)
        {
            var bip = isExtension
                ? (end == 0 ? BuiltInParameter.START_EXTENSION : BuiltInParameter.END_EXTENSION)
                : (end == 0 ? BuiltInParameter.START_JOIN_CUTBACK : BuiltInParameter.END_JOIN_CUTBACK);
            var label = (end == 0 ? "Start " : "End ") + (isExtension ? "Extension" : "Join Cutback");
            var where = describe(member) + ", " + label + (meets.ContainsKey(end) ? " (at " + meets[end] + ")" : "");

            var p = member.get_Parameter(bip);
            if (p == null || p.StorageType != StorageType.Double)
            {
                refused.Add(where + ": this member has no " + label + " - nothing was changed");
                continue;
            }
            if (p.IsReadOnly)
            {
                refused.Add(where + ": Revit has " + label + " greyed out on this member"
                    + (isCutback ? " - a join cutback only applies at an end joined to another member" : "")
                    + ". Nothing was changed; joining is not turned on here");
                continue;
            }

            var beforeMm = p.AsDouble() * 304.8;
            if (Math.Abs(beforeMm - lengthMm) < 0.05)
            {
                alreadyRight.Add(where + ": already " + mm(beforeMm) + " - left alone");
                continue;
            }

            bool took;
            try { took = p.Set(lengthMm / 304.8); }
            catch (Exception ex)
            {
                refused.Add(where + ": Revit would not take " + mm(lengthMm) + " - it stays " + mm(beforeMm)
                    + ". Revit said: " + ex.Message);
                continue;
            }
            if (!took)
            {
                refused.Add(where + ": Revit refused " + mm(lengthMm) + " - it stays " + mm(beforeMm));
                continue;
            }

            pending.Add(Tuple.Create(member, bip, label, beforeMm, where));
        }
    }

    if (pending.Count > 0)
    {
        doc.Regenerate();
        foreach (var item in pending)
        {
            var p = item.Item1.get_Parameter(item.Item2);
            var afterMm = p != null ? p.AsDouble() * 304.8 : double.NaN;
            if (!double.IsNaN(afterMm) && Math.Abs(afterMm - lengthMm) < 0.5)
            {
                changed++;
                ends.Add(string.Format("{0}: {1} -> {2}, read back", item.Item5, mm(item.Item4), mm(afterMm)));
            }
            else
            {
                refused.Add(string.Format("{0}: Revit took {1} but it reads {2}", item.Item5, mm(lengthMm),
                    double.IsNaN(afterMm) ? "nothing readable" : mm(afterMm)));
            }
        }
    }
}

findings.Add(string.Format("{0} end(s) set to {1} and read back; {2} already there; {3} refused",
    changed, mm(lengthMm), alreadyRight.Count, refused.Count));
findings.AddRange(refused);
