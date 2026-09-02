// NOT STANDALONE. Assumes `doc`, `elements`, `insulationTypeId` and
// `thicknessMm` are in scope; leaves `insulated`, `replaced` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// EXISTING INSULATION IS REMOVED FIRST. Revit accepts a second layer on an
// already-insulated duct without complaint, and the run then occupies a size
// nobody drew and no clash check knows about. Removing first makes the outcome
// the same whether the element was bare or already wrapped, which is what
// somebody re-running this on a mixed selection expects.
//
// DUCT AND PIPE ARE DIFFERENT TYPES, ONE SELECTION. A modeller selects a run
// and does not sort it by discipline first, so both are handled and anything
// that is neither is refused BY NAME rather than skipped.
//
// MILLIMETRES TO FEET IS ARITHMETIC (D-20): 1 ft = 304.8 mm exactly. Nothing
// for 2021's unit rewrite to reach.
//
// THE COUNT IS TAKEN FROM WHAT EXISTS AFTERWARDS, never from the input list.
// `Create` returning an object is not evidence the insulation is on the
// element at the thickness asked for - the same reason the parameter path
// reads back after Revit returns TRUE.

var insulated = 0;
var replaced = 0;
var refused = new List<ElementId>();

const double MillimetresPerFoot = 304.8;
var thickness = thicknessMm / MillimetresPerFoot;

foreach (var element in elements)
{
    if (element == null) continue;

    // Short names on purpose. The harness supplies the Mechanical and Plumbing
    // usings, and a FULLY-QUALIFIED type name inside brain/ breaks the adapter
    // boundary (docs/16 s4) - which check-structure.py catches, and did.
    //
    // It matches on TEXT, so the sentence above cannot spell the namespace it
    // is warning about without failing the check itself. Left as prose rather
    // than teaching the checker to parse comments: a blunt rule that catches
    // its own explanation costs one rewording, and comment-aware parsing costs
    // a parser that can be wrong.
    var isDuct = element is Duct;
    var isPipe = element is Pipe;

    if (!isDuct && !isPipe)
    {
        // A fitting, an accessory, a piece of equipment. Named rather than
        // skipped - "it did not insulate" is invisible otherwise, and the
        // fittings between insulated ducts are exactly what gets missed.
        refused.Add(element.Id);
        continue;
    }

    try
    {
        var existing = InsulationLiningBase.GetInsulationIds(doc, element.Id);
        if (existing != null && existing.Count > 0)
        {
            doc.Delete(existing);
            replaced++;
        }

        if (isDuct) DuctInsulation.Create(doc, element.Id, insulationTypeId, thickness);
        else PipeInsulation.Create(doc, element.Id, insulationTypeId, thickness);
    }
    catch (Exception)
    {
        refused.Add(element.Id);
        continue;
    }

    // READ BACK from the element, not from the call's return value.
    var now = InsulationLiningBase.GetInsulationIds(doc, element.Id);
    if (now != null && now.Count > 0) insulated++;
    else refused.Add(element.Id);
}
