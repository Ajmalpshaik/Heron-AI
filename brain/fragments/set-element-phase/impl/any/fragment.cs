// NOT STANDALONE. Assumes `doc`, `elements`, `createdPhase` and
// `demolishedPhase` are in scope, and leaves `created`, `demolished`,
// `refused`, `absent`, `unverified` and `phaseNotFound` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// WRITE_ELEMENT_PARAMETERS CANNOT DO THIS.
//
// Both phase parameters store an ElementId, not text. A fragment that writes a
// value cannot set one, and the phase NAME is not the thing being stored - it
// has to be resolved to the phase element first, which is why REPORT_PHASES is
// the step before this.
//
// CREATED AND DEMOLISHED ARE INDEPENDENT AND BOTH OPTIONAL.
//
// "Put these on New Construction" touches Created and must NOT clear
// Demolished. An empty name means LEAVE IT ALONE - a different instruction from
// "clear it".
//
// AN UNKNOWN PHASE NAME STOPS THE JOB BEFORE ANYTHING IS WRITTEN.
//
// Half a set on one phase and half untouched is worse than nothing done: the
// model looks partly correct and the failure is invisible in a plan.

var created = new List<ElementId>();
var demolished = new List<ElementId>();
var refused = new List<ElementId>();
var absent = new List<ElementId>();
var unverified = new List<ElementId>();
var phaseNotFound = new List<string>();

var byName = new Dictionary<string, ElementId>();
var phases = doc.Phases;
int phaseTotal = phases == null ? 0 : phases.Size;
for (int i = 0; i < phaseTotal; i++)
{
    var phase = phases.get_Item(i);
    if (phase == null) continue;
    string name = null;
    try { name = phase.Name; } catch { }
    if (!string.IsNullOrEmpty(name) && !byName.ContainsKey(name)) byName[name] = phase.Id;
}

bool wantCreated = !string.IsNullOrWhiteSpace(createdPhase);
bool wantDemolished = !string.IsNullOrWhiteSpace(demolishedPhase);

ElementId createdId = ElementId.InvalidElementId;
ElementId demolishedId = ElementId.InvalidElementId;

if (wantCreated && !byName.TryGetValue(createdPhase, out createdId)) phaseNotFound.Add(createdPhase);
if (wantDemolished && !byName.TryGetValue(demolishedPhase, out demolishedId)) phaseNotFound.Add(demolishedPhase);

// Resolved BEFORE the loop, and nothing is written if a name is unknown.
if (phaseNotFound.Count == 0 && (wantCreated || wantDemolished))
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        if (wantCreated)
        {
            var parameter = element.get_Parameter(BuiltInParameter.PHASE_CREATED);
            if (parameter == null || parameter.IsReadOnly) absent.Add(element.Id);
            else
            {
                bool accepted = false;
                try { accepted = parameter.Set(createdId); } catch { }

                // READ BACK. Revit refuses a demolition earlier than the
                // creation, and a count of attempts reports that as done.
                ElementId now = ElementId.InvalidElementId;
                try { now = parameter.AsElementId(); } catch { unverified.Add(element.Id); continue; }

                if (!accepted || now != createdId) refused.Add(element.Id);
                else created.Add(element.Id);
            }
        }

        if (wantDemolished)
        {
            var parameter = element.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED);
            if (parameter == null || parameter.IsReadOnly) { absent.Add(element.Id); continue; }

            bool accepted = false;
            try { accepted = parameter.Set(demolishedId); } catch { }

            ElementId now = ElementId.InvalidElementId;
            try { now = parameter.AsElementId(); } catch { unverified.Add(element.Id); continue; }

            if (!accepted || now != demolishedId) refused.Add(element.Id);
            else demolished.Add(element.Id);
        }
    }
}
