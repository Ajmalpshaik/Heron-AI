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
        catch (Exception)
        {
            refused.Add(element.Id);
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
        catch (Exception)
        {
            refused.Add(element.Id);
            continue;
        }

        if (element.GetTypeId() == wantedTypeId) changed++;
        else refused.Add(element.Id);
    }

    var typeElement = doc.GetElement(wantedTypeId);
    var typeName = typeElement == null ? "the type asked for" : "'" + typeElement.Name + "'";
    findings.Insert(0, string.Format("{0} element(s) changed to {1}{2}. {3} already had it, and {4} "
        + "were refused{5}", changed, typeName,
        source == null ? "" : string.Format(", taken from id {0}", source.Id),
        alreadyThatType, refused.Count,
        wrongCategory == 0 ? "" : string.Format(" - {0} of them of a different category from the source",
            wrongCategory)));

    if (changed > 0)
        findings.Add("Only the TYPE changed. Mark, comments, offsets and every other instance "
            + "parameter are exactly as they were - COPY_PARAMETER_VALUE is what moves those, "
            + "and it is a separate decision");
}
