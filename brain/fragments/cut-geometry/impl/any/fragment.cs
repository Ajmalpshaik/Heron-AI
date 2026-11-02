// NOT STANDALONE. Assumes `doc`, `elements`, `cutter` and `cut` are in scope;
// leaves `changed`, `alreadyRight`, `wouldNot`, `refused`, `pairs` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list.
//
// MODIFY > CUT GEOMETRY AND UNCUT GEOMETRY, IN A PROJECT. Every element handed
// in is paired with `cutter`, the ONE element that does the cutting. `cut` is
// cut (true) or uncut (false) - opposite instructions, so there is no default
// and any other word is refused before anything changes.
//
// WHICH ELEMENT CUTS WHICH IS NEVER GUESSED. The cutter is the element named
// as `cutter` - selected on its own and passed as "selected", as
// JOIN_GEOMETRY's target is - and never "the first of the selection": the
// add-in refuses `selected` with two or more selected, because a Revit
// selection has no order (RevitFragment.cs, OneElement). The elements to cut
// arrive the other way, from the chain or the selection.
//
// TWO CUTS, AND THE ELEMENTS DECIDE WHICH - NEVER A SETTING:
//   a VOID cut   the cutter is a family instance with an unattached void that
//                cuts when loaded (InstanceVoidCutUtils.IsVoidInstanceCuttingElement)
//                and the element is one Revit lets a void cut (CanBeCutWithVoid);
//   a SOLID cut  otherwise, one solid taking its shape out of the other
//                (SolidSolidCutUtils), asked first with CanElementCutElement so a
//                pair Revit will not cut is refused with Revit's own reason.
//
// ALREADY IN THE ASKED-FOR STATE IS NOT WORK. A pair already cut by the
// cutter (cut) or not cut at all (uncut) is listed in `alreadyRight` and
// never counted. A cut that already runs THE OTHER WAY - the element cutting
// the cutter - is refused when cutting, never turned round. Uncutting removes
// the cut between the two named elements whichever way it ran, and says which.
//
// EVERY PAIR IS READ BACK. The call returning is not the cut existing: each
// pair's state is read again afterwards and written to `pairs`; a call that
// left it unchanged is in `wouldNot`, never in `changed`.

var changed = 0;
var alreadyRight = new List<ElementId>();
var wouldNot = new List<string>();
var refused = new List<string>();
var pairs = new List<string>();
var findings = new List<string>();

Func<Element, string> describe = e => string.Format("{0}{1} (id {2})",
    e.Category != null ? e.Category.Name + " " : "", e.Name, e.Id);

Func<CutFailureReason, string> because = r =>
    r == CutFailureReason.CutAlreadyExists ? "it is already cut by the cutting element"
    : r == CutFailureReason.OppositeCutExists ? "the cut already runs THE OTHER WAY - this element cuts the cutting element"
    : r == CutFailureReason.CutNotAppropriateForElements ? "Revit says these two elements cannot be cut one by the other"
    : "Revit allows the cut";

var mode = (cut ?? "").Trim().ToLowerInvariant();
var cutWanted = mode == "true" || mode == "cut" || mode == "yes";
var uncutWanted = mode == "false" || mode == "uncut" || mode == "no";

if (!cutWanted && !uncutWanted)
{
    refused.Add(string.Format("cut was '{0}' - say cut (true) or uncut (false). They are opposite "
        + "instructions, so NOTHING WAS CHANGED", cut));
    findings.Add(refused[0]);
}
else if (cutter == null)
{
    refused.Add("No cutting element was named - select the ONE element that does the cutting and pass "
        + "\"selected\". Which element cuts which is never guessed");
    findings.Add(refused[0]);
}
else if (elements == null || elements.Count == 0)
{
    refused.Add("No elements to cut were handed in - nothing was changed");
    findings.Add(refused[0]);
}
else
{
    var cutterName = describe(cutter);
    var cutterIsVoid = false;
    var cutterSolid = false;
    string cutterProblem = null;
    try
    {
        cutterIsVoid = InstanceVoidCutUtils.IsVoidInstanceCuttingElement(cutter);
        cutterSolid = SolidSolidCutUtils.IsElementFromAppropriateContext(cutter)
            && SolidSolidCutUtils.IsAllowedForSolidCut(cutter);
    }
    catch (Exception ex)
    {
        cutterProblem = ex.Message;
    }

    if (cutterProblem != null)
    {
        refused.Add(string.Format("{0} could not be asked whether it can cut - Revit said: {1}. NOTHING WAS "
            + "CHANGED", cutterName, cutterProblem));
        findings.Add(refused[0]);
    }
    else if (cutWanted && !cutterIsVoid && !cutterSolid)
    {
        refused.Add(string.Format("{0} cannot cut anything: it is not a family with a void that cuts when "
            + "loaded, and Revit does not allow it in a cut between solids. NOTHING WAS CHANGED", cutterName));
        findings.Add(refused[0]);
    }
    else
    {
        var position = 0;
        foreach (var element in elements)
        {
            position++;
            if (element == null)
            {
                refused.Add(string.Format("element {0} of the set is missing, so it has no pair", position));
                continue;
            }
            if (element.Id == cutter.Id)
            {
                refused.Add(string.Format("{0}: the cutting element itself - an element never cuts itself",
                    element.Id));
                continue;
            }

            var name = describe(element);
            try
            {
                var voidPair = cutterIsVoid && InstanceVoidCutUtils.CanBeCutWithVoid(element);
                var solidPair = cutterSolid && SolidSolidCutUtils.IsElementFromAppropriateContext(element)
                    && SolidSolidCutUtils.IsAllowedForSolidCut(element);

                var voidBefore = voidPair && InstanceVoidCutUtils.InstanceVoidCutExists(element, cutter);
                var cutterCutsBefore = false;
                var solidBefore = solidPair
                    && SolidSolidCutUtils.CutExistsBetweenElements(cutter, element, out cutterCutsBefore);
                string route;

                if (cutWanted)
                {
                    if (voidBefore || (solidBefore && cutterCutsBefore))
                    {
                        alreadyRight.Add(element.Id);
                        pairs.Add(string.Format("{0}: already cut by {1} - left alone", name, cutterName));
                        continue;
                    }

                    if (voidPair)
                    {
                        route = "void";
                        InstanceVoidCutUtils.AddInstanceVoidCut(doc, element, cutter);
                    }
                    else if (solidPair)
                    {
                        CutFailureReason reason;
                        if (!SolidSolidCutUtils.CanElementCutElement(cutter, element, out reason))
                        {
                            var words = string.Format("{0}: not cut by {1} - {2} ({3})", name, cutterName,
                                because(reason), reason);
                            if (reason == CutFailureReason.CutAlreadyExists)
                            {
                                alreadyRight.Add(element.Id);
                                pairs.Add(words + " - left alone");
                            }
                            else if (reason == CutFailureReason.OppositeCutExists)
                            {
                                refused.Add(words + ". NOT turned round: uncut them first (cut false) and cut "
                                    + "again naming the other one as the cutter, if that is what is meant");
                                pairs.Add(words);
                            }
                            else
                            {
                                wouldNot.Add(words);
                                pairs.Add(words);
                            }
                            continue;
                        }
                        route = "solid";
                        SolidSolidCutUtils.AddCutBetweenSolids(doc, element, cutter);
                    }
                    else
                    {
                        var words = string.Format("{0}: Revit does not let {1} cut it - {2}", name, cutterName,
                            cutterIsVoid
                                ? "a void family cannot cut this element, and it is not one Revit cuts between solids"
                                : "it is not one Revit allows in a cut between solids");
                        wouldNot.Add(words);
                        pairs.Add(words);
                        continue;
                    }
                }
                else
                {
                    if (!voidBefore && !solidBefore)
                    {
                        alreadyRight.Add(element.Id);
                        pairs.Add(string.Format("{0}: not cut by {1} - left alone", name, cutterName));
                        continue;
                    }
                    if (voidBefore)
                    {
                        route = "void";
                        InstanceVoidCutUtils.RemoveInstanceVoidCut(doc, element, cutter);
                    }
                    else
                    {
                        route = cutterCutsBefore ? "solid" : "solid, which ran the other way - this element was cutting the cutting element";
                        SolidSolidCutUtils.RemoveCutBetweenSolids(doc, cutter, element);
                    }
                }

                // READ IT BACK. A call that returned is not a cut made or taken away.
                var voidAfter = voidPair && InstanceVoidCutUtils.InstanceVoidCutExists(element, cutter);
                var cutterCutsAfter = false;
                var solidAfter = solidPair
                    && SolidSolidCutUtils.CutExistsBetweenElements(cutter, element, out cutterCutsAfter);
                var nowCut = cutWanted ? (voidAfter || (solidAfter && cutterCutsAfter)) : (voidAfter || solidAfter);

                if (nowCut == cutWanted)
                {
                    changed++;
                    pairs.Add(string.Format("{0}: {1} {2} ({3} cut) - read back: {4}", name,
                        cutWanted ? "cut by" : "uncut from", cutterName, route, nowCut ? "cut" : "not cut"));
                }
                else
                {
                    var words = string.Format("{0}: the call was accepted and the read-back says it is still {1} "
                        + "by {2}", name, cutWanted ? "NOT cut" : "cut", cutterName);
                    wouldNot.Add(words);
                    pairs.Add(words);
                }
            }
            catch (Exception ex)
            {
                var words = string.Format("{0}: Revit refused - {1}", name, ex.Message);
                wouldNot.Add(words);
                pairs.Add(words);
            }
        }

        findings.Add(string.Format("{0} element(s) {1} {2}. {3} were already {4} and were left alone{5}{6}",
            changed, cutWanted ? "cut by" : "uncut from", cutterName, alreadyRight.Count,
            cutWanted ? "cut by it" : "not cut by it",
            wouldNot.Count == 0 ? "" : string.Format(", {0} could not be", wouldNot.Count),
            refused.Count == 0 ? "" : string.Format(", {0} refused", refused.Count)));
        var shown = 0;
        foreach (var line in pairs)
        {
            if (shown++ >= 50) { findings.Add(string.Format("... and {0} more pair(s)", pairs.Count - 50)); break; }
            findings.Add("Pair: " + line);
        }
        foreach (var reason in refused) findings.Add("Refused: " + reason);

        if (changed > 0)
            findings.Add("QUANTITIES HAVE MOVED. A cut element loses the cutter's shape from its volume and "
                + "material takeoff, so a schedule issued before this run now disagrees with the model");
    }
}
