// NOT STANDALONE. Assumes `doc`, `elements`, `phaseId` and `whichPhase` are in
// scope; leaves `changed`, `refused`, `readOnly` and `alreadySet` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// CREATED OR DEMOLISHED IS ASKED FOR, NEVER INFERRED. The two live in different
// parameters and setting the wrong one is how existing work disappears from
// every view at once. A phase named "Existing" tells you nothing about which
// field the user meant - putting an element ON the existing phase and
// demolishing it IN the existing phase are opposite instructions.
//
// PHASE_CREATED AND PHASE_DEMOLISHED ARE ElementId PARAMETERS, not strings.
// Setting them from a phase's NAME would fail on any project whose phases are
// named in another language, or renamed - which is most of them.
//
// A READ-ONLY PHASE PARAMETER IS NORMAL AND IS NOT A FAILURE. Elements inside a
// group, elements hosted by something else, and elements in a linked model all
// take their phase from what owns them. They are recorded separately from
// `refused` because there is nothing to fix on them - fixing means changing the
// group or the host.
//
// AN ELEMENT ALREADY ON THE PHASE IS COUNTED, NOT WRITTEN. Writing the same
// value back marks the element as modified for worksharing, which shows up as
// a change somebody else has to reconcile for no reason.

var changed = new List<ElementId>();
var refused = new List<ElementId>();
var readOnly = new List<ElementId>();
var alreadySet = new List<ElementId>();

var field = (whichPhase ?? "").Trim().ToLowerInvariant();

var wantDemolished = field.Contains("demolish") || field.Contains("demo");
var wantCreated = field.Contains("creat") || field.Contains("new") || field.Contains("built");

var phase = doc.GetElement(phaseId) as Phase;

if (phase == null || (!wantCreated && !wantDemolished) || (wantCreated && wantDemolished))
{
    // All of them together: the fault is in the request, not in any element,
    // and reporting each element individually sends somebody to look at the
    // elements.
    foreach (var element in elements)
    {
        if (element != null) refused.Add(element.Id);
    }
}
else
{
    var which = wantDemolished
        ? BuiltInParameter.PHASE_DEMOLISHED
        : BuiltInParameter.PHASE_CREATED;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var parameter = element.get_Parameter(which);

        // No such parameter at all - a datum, an annotation, a view. Phases do
        // not apply to it, which is different from being unable to change it.
        if (parameter == null) { refused.Add(element.Id); continue; }

        if (parameter.IsReadOnly) { readOnly.Add(element.Id); continue; }

        if (parameter.AsElementId() == phaseId) { alreadySet.Add(element.Id); continue; }

        if (parameter.Set(phaseId)) changed.Add(element.Id);
        else refused.Add(element.Id);
    }
}
