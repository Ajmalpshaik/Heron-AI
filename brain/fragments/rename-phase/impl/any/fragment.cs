// NOT STANDALONE. Assumes `doc`, `phaseName` and `newName` are in scope;
// leaves `renamed`, `previousName` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A PHASE IS AN ELEMENT AND RENAME_ELEMENTS STILL CANNOT REACH IT. That
// fragment renames what a SELECTION hands it, and no selection in this library
// yields phases - they live in doc.Phases, reached from the document and from
// nowhere else. SELECT_BY_PHASE returns the elements standing IN a phase.
//
// `Phases` is a PhaseArray rather than a generic collection, so it is walked by
// index - the same reason REPORT_PHASES walks it that way.
//
// THE DUPLICATE CHECK IS EXACT-CASE ON PURPOSE, AND THAT IS THE CAUTIOUS
// DIRECTION RATHER THAN THE LAZY ONE. Revit throws on a duplicate phase name;
// whether it counts "design" and "Design" as the same name is NOT established
// here, and a check guessing that they are would refuse a rename Revit would
// have allowed. So only a name already taken EXACTLY is refused in advance, and
// anything else is offered to Revit - whose own refusal is reported verbatim
// rather than paraphrased into a guess.

var renamed = false;
string previousName = null;
var refused = new List<string>();

Phase target = null;
// One list, read two ways: it names the phases back to the caller when the
// one they asked for is not here, and it is the duplicate check when the new
// name is. Two lists holding the same strings is one list that can drift.
var existingNames = new List<string>();
var nearMatches = new List<string>();

var phases = doc.Phases;
int phaseCount = phases == null ? 0 : phases.Size;

for (int i = 0; i < phaseCount; i++)
{
    var phase = phases.get_Item(i);
    if (phase == null) continue;

    string name = null;
    try { name = phase.Name; } catch { }
    if (string.IsNullOrEmpty(name)) continue;

    existingNames.Add(name);

    // An exact match settles it. A case-only match is only a candidate, because
    // two of them would mean the request does not name one phase.
    if (string.Equals(name, phaseName, StringComparison.Ordinal))
    {
        target = phase;
    }
    else if (string.Equals(name, phaseName, StringComparison.OrdinalIgnoreCase))
    {
        nearMatches.Add(name);
        if (target == null && nearMatches.Count == 1) target = phase;
    }
}

if (phaseCount == 0)
{
    refused.Add("this model reports no phases at all, which should not happen in Revit - "
        + "nothing was changed");
}
else if (target == null)
{
    refused.Add(string.Format("no phase called '{0}'. This model has: {1}",
        phaseName, string.Join(", ", existingNames)));
}
else if (nearMatches.Count > 1 && !string.Equals(target.Name, phaseName, StringComparison.Ordinal))
{
    // Reached only when the name was given in the wrong case AND more than one
    // phase answers to it. Renaming whichever came first would be a coin toss.
    refused.Add(string.Format("'{0}' matches more than one phase apart from case ({1}) - "
        + "name it exactly and nothing has to be guessed. Nothing was changed",
        phaseName, string.Join(", ", nearMatches)));
}
else if (string.IsNullOrWhiteSpace(newName))
{
    refused.Add("no new name given - a phase cannot be renamed to nothing");
}
else if (string.Equals(target.Name, newName, StringComparison.Ordinal))
{
    refused.Add(string.Format("'{0}' is already its name - nothing to do", newName));
}
else if (existingNames.Contains(newName))
{
    refused.Add(string.Format("'{0}' is already the name of another phase. Revit refuses a "
        + "duplicate, and finding that out partway through is worse than being told now. "
        + "Nothing was changed", newName));
}
else
{
    var wasCalled = target.Name;

    try
    {
        target.Name = newName;
        renamed = true;
        previousName = wasCalled;
    }
    catch (Exception ex)
    {
        // Revit's own words. A workshared model whose Project Standards are not
        // editable refuses here, and so does a name holding a character Revit
        // will not take - two different problems that must not be flattened
        // into one invented sentence.
        refused.Add(string.Format("'{0}' could not be renamed to '{1}' - {2}",
            wasCalled, newName, ex.Message));
    }
}
