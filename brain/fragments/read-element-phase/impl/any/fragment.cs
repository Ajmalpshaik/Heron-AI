// NOT STANDALONE. Assumes `elements` is in scope; leaves `createdPhase`,
// `demolishedPhase` and `noPhase` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// BOTH PHASES, BECAUSE ONE IS NOT AN ANSWER. An element created in "Existing"
// and an element created in "Existing" AND demolished in "Phase 1" read
// identically until the second is asked for - and they are opposite
// instructions on site: leave it, or take it out. Reading only the created
// phase is the mistake this fragment exists to avoid.
//
// READ THROUGH THE PARAMETER, NOT THROUGH CreatedPhaseId. The property is not
// present on every element kind, and reaching for it means an element that
// plainly has a phase reports none. The built-in parameter answers for
// anything phased at all.
//
// NOT DEMOLISHED IS THE NORMAL CASE and is absence, not a value. It stays out
// of `demolishedPhase` entirely rather than being recorded as "none", so a
// caller can ask whether the key is there instead of comparing strings against
// a word this fragment invented.

var createdPhase = new Dictionary<ElementId, string>();
var demolishedPhase = new Dictionary<ElementId, string>();
var noPhase = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;
    if (createdPhase.ContainsKey(element.Id) || noPhase.Contains(element.Id)) continue;

    var created = element.get_Parameter(BuiltInParameter.PHASE_CREATED);
    if (created == null || !created.HasValue
        || created.StorageType != StorageType.ElementId)
    {
        noPhase.Add(element.Id);
        continue;
    }

    var createdId = created.AsElementId();
    if (createdId == null || createdId == ElementId.InvalidElementId)
    {
        noPhase.Add(element.Id);
        continue;
    }

    var phase = element.Document.GetElement(createdId);
    if (phase == null)
    {
        noPhase.Add(element.Id);
        continue;
    }

    createdPhase[element.Id] = phase.Name;

    var demolished = element.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED);
    if (demolished == null || !demolished.HasValue
        || demolished.StorageType != StorageType.ElementId)
    {
        continue;
    }

    var demolishedId = demolished.AsElementId();
    if (demolishedId == null || demolishedId == ElementId.InvalidElementId) continue;

    var when = element.Document.GetElement(demolishedId);
    if (when != null) demolishedPhase[element.Id] = when.Name;
}
