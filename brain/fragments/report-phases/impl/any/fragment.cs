// NOT STANDALONE. Assumes `doc` is in scope, and leaves `phaseNames`,
// `phaseIds` and `phaseCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE DISCOVERY STEP FOR EVERY PHASE JOB.
//
// Setting an element's phase needs an EXACT existing name, and so does renaming
// or deleting one. A near-miss - "Existing" against "Existing Construction" -
// fails, or matches something else. Nothing in Revit's own UI hands that list
// over in a form anything can use.
//
// THE ORDER IS INFORMATION, NOT PRESENTATION.
//
// Phases are sequential and Revit keeps them so. "The phase before this one"
// and "the last phase" are real ideas that only the order can answer. It is
// returned as Revit holds it and never sorted by name, which would put
// "Phase 10" between "Phase 1" and "Phase 2".
//
// `Phases` is a PhaseArray rather than a generic collection, so it is walked by
// index. That is also what preserves the order - converting it to a set would
// lose the one thing this fragment exists to report.

var phaseNames = new List<string>();
var phaseIds = new Dictionary<string, ElementId>();

var phases = doc.Phases;
int phaseCount = phases == null ? 0 : phases.Size;

for (int i = 0; i < phaseCount; i++)
{
    var phase = phases.get_Item(i);
    if (phase == null) continue;

    string name = null;
    try { name = phase.Name; } catch { }
    if (string.IsNullOrEmpty(name)) continue;

    phaseNames.Add(name);
    // Two phases cannot share a name in Revit, so the name is a safe key - and
    // it is the key every phase job actually has in hand.
    if (!phaseIds.ContainsKey(name)) phaseIds[name] = phase.Id;
}
