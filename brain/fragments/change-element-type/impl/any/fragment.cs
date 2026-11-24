// NOT STANDALONE. Assumes `elements`, `newTypeId`, `typeFrom`, `doc` and
// `uidoc` are in scope; leaves `changed`, `alreadyThatType`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// VERSION 2 (2026-11-01) - MATCH_ELEMENT_TYPE MERGED IN. The type can now be
// TAKEN FROM AN ELEMENT as well as named - "make these the same as that one",
// pointed at rather than recited:
//
//   newTypeId   the type, named - version 1, line for line below.
//   typeFrom    "selected": the ONE element selected in Revit is the source,
//               and its type is the one the others take. Exactly one must be
//               selected - a Revit selection has no order, so one of three
//               would be a guess. Its type is read off it; a source with NO
//               type (a system element without one) is refused with the
//               reason. The source among the elements is skipped without
//               comment - it already is what it is being asked to become.
//
// ONE OR THE OTHER, NEVER BOTH, AND NEVER NEITHER. Both given is two answers
// to one question; neither is a type nobody named. Either is refused before
// anything changes.
//
// THE TWO MODES KEEP THEIR OWN CATEGORY RULE, ON PURPOSE. Taken from a
// source, an element of a DIFFERENT CATEGORY is refused by id and category,
// never changed - MATCH_ELEMENT_TYPE's rule: "make these like that wall" has
// no meaning for a duct. Named by id, a cross-category swap is still let
// through to Revit, because one is measured and wanted: three selected
// curtain panels became curtain wall doors here on 2026-10-13
// (brain/skills/building-shell.yaml, step 9).
//
// IT CHANGES THE TYPE AND NOTHING ELSE, and says so whenever anything
// changed. Mark, comments, offsets - every instance parameter - stay as they
// were. Revit's own Match Type tool copies more than that.
//
// READ THE TYPE FIRST, CHANGE IT, THEN READ IT BACK. Both reads earn their
// keep and they catch different things.
//
//   before   an element already on the target type would otherwise be counted
//            as changed. "Swapped 60" when 58 were already that type is the
//            asked-for/happened failure again
//   after    ChangeTypeId does not always take. It throws for some mismatches
//            and for others returns quietly having done nothing - the same
//            silent no-op shape as the move that moved nothing. The absence of
//            an exception is not evidence
//
// SUBMITTED ONE AT A TIME so a refusal is recorded and stepped over. A throw
// part-way through a bulk change leaves some elements swapped, some not, and
// reports neither.
//
// VERSION 3 (2026-11-24, FRAGMENT-ISSUES row 5b-2011) - EVERY REFUSAL SAYS WHY.
// On a real job model a work-plane-based exhaust louvre asked to become a type
// of a LEVEL-BASED louvre family came back "1 were refused" and nothing else,
// and the modeller was left guessing. Now each refused element carries one
// sentence built only from what Revit itself answers - never a guess:
//
//   - the wanted id is not a type at all, or another category's type
//   - Revit's own list of valid types for that element (IsValidType /
//     GetValidTypes) leaves the type out - and, where both are loadable
//     families, how each family is placed (FamilyPlacementType). A difference
//     in placement is named as the difference Heron found, not as Revit's
//     stated reason, and the way on is named: place the new family and carry
//     the values (the replace-in-place-family method, step 6)
//   - ChangeTypeId threw: Revit's own message, quoted
//   - Revit took the call and left the element on its old type: said so
//
// WHAT CHANGES IN BEHAVIOUR: nothing. The same elements are changed and the
// same are refused as in version 2; the checks are asked only AFTER a refusal,
// to explain it, so the cross-category swap by id that Revit does take
// (curtain panels to curtain wall doors, 2026-10-13) still goes to Revit.

var changed = 0;
var alreadyThatType = 0;
var refused = new List<ElementId>();
var findings = new List<string>();

var fromWord = (typeFrom ?? "").Trim().ToLowerInvariant();
var fromSource = fromWord.Length > 0 && fromWord != "none";
var named = newTypeId != null && newTypeId != ElementId.InvalidElementId;

Element source = null;
var wantedTypeId = newTypeId;
string stop = null;

// VERSION 3: how a family is placed, in plain words, from its FamilyPlacementType.
Func<Element, string> placedHow = delegate (Element e)
{
    var fi = e as FamilyInstance;
    var symbol = fi != null ? fi.Symbol : e as FamilySymbol;
    Family family = null;
    try { family = symbol == null ? null : symbol.Family; } catch (Exception) { family = null; }
    if (family == null) return null;
    switch (family.FamilyPlacementType)
    {
        case FamilyPlacementType.OneLevelBased: return "level-based (placed on a level)";
        case FamilyPlacementType.OneLevelBasedHosted: return "hosted (placed in a wall, floor, ceiling or roof)";
        case FamilyPlacementType.TwoLevelsBased: return "placed between two levels";
        case FamilyPlacementType.WorkPlaneBased: return "work-plane-based (placed on a work plane or face)";
        case FamilyPlacementType.CurveBased: return "line-based";
        case FamilyPlacementType.CurveBasedDetail: return "a line-based detail item";
        case FamilyPlacementType.CurveDrivenStructural: return "a structural framing or column family";
        case FamilyPlacementType.ViewBased: return "view-based (a detail or annotation)";
        case FamilyPlacementType.Adaptive: return "adaptive (placed by its points)";
        default: return "placed in a way Revit calls " + family.FamilyPlacementType;
    }
};

Func<Element, string> familyName = delegate (Element e)
{
    var fi = e as FamilyInstance;
    var symbol = fi != null ? fi.Symbol : e as FamilySymbol;
    try { return symbol == null || symbol.Family == null ? null : symbol.Family.Name; }
    catch (Exception) { return null; }
};

// VERSION 3: one sentence per refused element, from what Revit answers.
// `thrown` is ChangeTypeId's own message, or null when it did not throw.
Func<Element, string, string> whyRefused = delegate (Element element, string thrown)
{
    var wantedType = doc.GetElement(wantedTypeId);
    var wantedName = wantedType == null ? "the type asked for" : "'" + wantedType.Name + "'";
    var head = string.Format("{0} was refused: ", element.Id);

    if (wantedType == null || !(wantedType is ElementType))
        return head + string.Format("id {0} is not a type in this model, so nothing can take it. "
            + "Name a type - the one in the Type Selector", wantedTypeId);

    var ownCategory = element.Category == null ? null : element.Category.Name;
    var wantedCategory = wantedType.Category == null ? null : wantedType.Category.Name;

    bool? valid = null;
    var offered = -1;
    try { valid = element.IsValidType(wantedTypeId); } catch (Exception) { valid = null; }
    try { var list = element.GetValidTypes(); offered = list == null ? -1 : list.Count; } catch (Exception) { offered = -1; }

    if (valid == false)
    {
        var said = head + string.Format("Revit does not offer {0} as a valid type for it{1}", wantedName,
            offered >= 0 ? string.Format(" (Revit lists {0} type(s) as valid for it)", offered) : "");

        if (ownCategory != null && wantedCategory != null && ownCategory != wantedCategory)
            return said + string.Format(". It is a {0} and {1} is a {2} type", ownCategory, wantedName, wantedCategory);

        var ownHow = placedHow(element);
        var wantedHow = placedHow(wantedType);
        if (ownHow != null && wantedHow != null && ownHow != wantedHow)
            return said + string.Format(". Its family '{0}' is {1}, and '{2}' is {3} - the difference "
                + "Heron found between them, and the likely reason; Revit itself gave none. Place the new "
                + "family and carry the values across instead (the replace-in-place-family method, step 6)",
                familyName(element) ?? "?", ownHow, familyName(wantedType) ?? "?", wantedHow);

        if (ownHow != null && wantedHow != null)
            return said + string.Format(". Both families are {0} and the same category; Revit gave no other "
                + "reason. Place the new family and carry the values across instead (the "
                + "replace-in-place-family method, step 6)", ownHow);

        return said + ". Revit gave no other reason";
    }

    if (thrown != null)
        return head + "Revit refused the change and said: '" + thrown.Trim() + "'";

    return head + string.Format("Revit took the change and left it on its old type - {0} did not take, "
        + "and Revit gave no reason", wantedName);
};

if (fromSource && named)
{
    stop = "A type was NAMED and a source element was given as well - two answers to one question. "
        + "Name the type, or point at the element whose type the others should take. Nothing was changed";
}
else if (!fromSource && !named)
{
    stop = "No type was named and no source element was given. Name the type, or select the one "
        + "element whose type the others should take and say typeFrom=selected. Nothing was changed";
}
else if (fromSource)
{
    if (fromWord != "selected" && fromWord != "selection")
    {
        stop = "typeFrom takes \"selected\" - the one element selected in Revit whose type the others "
            + "should take - or is left out to name the type instead. \"" + typeFrom + "\" is neither. "
            + "Nothing was changed";
    }
    else
    {
        ICollection<ElementId> picked = null;
        try
        {
            if (uidoc != null && uidoc.Document != null && uidoc.Document.Equals(doc))
                picked = uidoc.Selection.GetElementIds();
        }
        catch (Exception) { picked = null; }

        if (picked == null || picked.Count == 0)
            stop = "typeFrom=selected means the one element selected in Revit, and NOTHING is selected "
                + "in this model. Click the element whose type the others should take and run this again. "
                + "Nothing was changed";
        else if (picked.Count != 1)
            stop = string.Format("typeFrom=selected means ONE element, and {0} are selected. A Revit "
                + "selection has no order, so taking one of them would be a guess - select just the source. "
                + "Nothing was changed", picked.Count);
        else
        {
            source = doc.GetElement(picked.First());
            if (source == null)
                stop = "The selected element is not in this model. Nothing was changed";
            else
            {
                wantedTypeId = source.GetTypeId();
                if (wantedTypeId == null || wantedTypeId == ElementId.InvalidElementId)
                    stop = string.Format("'{0}' (id {1}) has no type to take. A system element without a "
                        + "type cannot be the source of a match. Nothing was changed", source.Name, source.Id);
            }
        }
    }
}

if (stop != null)
{
    findings.Add(stop);
}
else
{
    var sourceCategoryId = source == null || source.Category == null
        ? ElementId.InvalidElementId : source.Category.Id;
    var wrongCategory = 0;
    // VERSION 3: why each element Revit refused was refused, in the order met.
    var reasons = new List<string>();

    foreach (var element in elements)
    {
        if (element == null) continue;

        // FROM A SOURCE: the source is already itself, and another category
        // cannot take its type (version 2, MATCH_ELEMENT_TYPE's rules).
        if (source != null)
        {
            if (element.Id == source.Id) continue;

            var categoryId = element.Category == null ? ElementId.InvalidElementId : element.Category.Id;
            if (categoryId != sourceCategoryId)
            {
                wrongCategory++;
                refused.Add(element.Id);
                findings.Add(string.Format("Refused: id {0} is a {1} and the source is a {2}",
                    element.Id,
                    element.Category == null ? "category-less element" : element.Category.Name,
                    source.Category == null ? "category-less element" : source.Category.Name));
                continue;
            }
        }

        ElementId was;
        try
        {
            was = element.GetTypeId();
        }
        catch (Exception ex)
        {
            refused.Add(element.Id);
            reasons.Add(string.Format("{0} was refused: Revit could not read its type and said: '{1}'",
                element.Id, ex.Message.Trim()));
            continue;
        }

        if (was == wantedTypeId)
        {
            alreadyThatType++;
            continue;
        }

        try
        {
            element.ChangeTypeId(wantedTypeId);
        }
        catch (Exception ex)
        {
            refused.Add(element.Id);
            reasons.Add(whyRefused(element, ex.Message));
            continue;
        }

        if (element.GetTypeId() == wantedTypeId) changed++;
        else
        {
            refused.Add(element.Id);
            reasons.Add(whyRefused(element, null));
        }
    }

    var typeElement = doc.GetElement(wantedTypeId);
    var typeName = typeElement == null ? "the type asked for" : "'" + typeElement.Name + "'";
    findings.Insert(0, string.Format("{0} element(s) changed to {1}{2}. {3} already had it, and {4} "
        + "were refused{5}", changed, typeName,
        source == null ? "" : string.Format(", taken from id {0}", source.Id),
        alreadyThatType, refused.Count,
        (wrongCategory == 0 ? "" : string.Format(" - {0} of them of a different category from the source",
            wrongCategory)) + (refused.Count == 0 ? "" : ". Each refusal and its reason follows")));

    // VERSION 3: every refusal by Revit, with its reason - the first 20 in
    // full, the rest counted, so one big batch cannot bury the summary.
    for (var i = 0; i < reasons.Count && i < 20; i++) findings.Add(reasons[i]);
    if (reasons.Count > 20)
        findings.Add(string.Format("{0} more refused - run on fewer elements to read each reason",
            reasons.Count - 20));

    if (changed > 0)
        findings.Add("Only the TYPE changed. Mark, comments, offsets and every other instance "
            + "parameter are exactly as they were - COPY_PARAMETER_VALUE is what moves those, "
            + "and it is a separate decision");
}
