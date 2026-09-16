// NOT STANDALONE. Assumes `doc`, `phaseName` and `newName` are in scope;
// leaves `renamed`, `previousName` and `refused` behind.
//
// `refused` IS A STRING AND NOT A LIST, AND THE REASON IS THAT THE MESSAGE IS
// THE ANSWER. The checks below are one if/else chain, so at most ONE reason is
// ever produced - a list of at most one was the wrong shape for it. It also
// reaches the reader intact: RevitFragment.Describe returns a string whole and
// shortens every item INSIDE a list to 60 characters, which on 2026-09-16 cut
// Revit's own refusal off at "- Thi..." and hid the one fact the run existed
// to establish.
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
var refused = "";
// Which door the rename actually went through, empty until one works.
var route = "";
// What Element.Name said when it refused, kept so a later failure can
// report BOTH doors rather than only the one that spoke last.
var nameRefusal = "";

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
    refused = ("this model reports no phases at all, which should not happen in Revit - "
        + "nothing was changed");
}
else if (target == null)
{
    refused = (string.Format("no phase called '{0}'. This model has: {1}",
        phaseName, string.Join(", ", existingNames)));
}
else if (nearMatches.Count > 1 && !string.Equals(target.Name, phaseName, StringComparison.Ordinal))
{
    // Reached only when the name was given in the wrong case AND more than one
    // phase answers to it. Renaming whichever came first would be a coin toss.
    refused = (string.Format("'{0}' matches more than one phase apart from case ({1}) - "
        + "name it exactly and nothing has to be guessed. Nothing was changed",
        phaseName, string.Join(", ", nearMatches)));
}
else if (string.IsNullOrWhiteSpace(newName))
{
    refused = ("no new name given - a phase cannot be renamed to nothing");
}
else if (string.Equals(target.Name, newName, StringComparison.Ordinal))
{
    refused = (string.Format("'{0}' is already its name - nothing to do", newName));
}
else if (existingNames.Contains(newName))
{
    refused = (string.Format("'{0}' is already the name of another phase. Revit refuses a "
        + "duplicate, and finding that out partway through is worse than being told now. "
        + "Nothing was changed", newName));
}
else
{
    var wasCalled = target.Name;

    // TWO DOORS TO THE SAME STRING, AND THE OBVIOUS ONE IS LOCKED.
    //
    // `Element.Name` is the documented way to rename an element and it is
    // READ-ONLY ON A PHASE. Measured 2026-09-16 on PIPE, Revit 2020, where it
    // threw: "This element does not support assignment of a user-specified
    // name." That is why this is not simply `target.Name = newName` - the
    // fragment was written that way first and the model refused it.
    //
    // The phase's name also lives in the PHASE_NAME parameter, which is a
    // different door to the same string. It is tried second rather than first
    // so that any release where the plain setter DOES work takes the
    // documented route, and `route` reports which one answered.
    //
    // AND IT READS BACK BEFORE IT COUNTS ANYTHING. `.Set()` returning true is
    // not the value being kept: row 45 of docs/FRAGMENT-ISSUES.md is a
    // parameter that reported writable, threw nothing, accepted `.Set()` and
    // discarded the value every time. A write nobody read back is a claim.
    try
    {
        target.Name = newName;
        route = "Element.Name";
    }
    catch (Exception ex)
    {
        nameRefusal = ex.Message;

        var nameParameter = target.get_Parameter(BuiltInParameter.PHASE_NAME);

        if (nameParameter == null)
        {
            refused = (string.Format("'{0}' could not be renamed to '{1}' - {2} "
                + "The PHASE_NAME parameter is not on this phase either, so Heron has no "
                + "second door. Rename it by hand: Manage, Phases", wasCalled, newName, ex.Message));
        }
        else if (nameParameter.IsReadOnly)
        {
            refused = (string.Format("'{0}' could not be renamed to '{1}' - {2} "
                + "The PHASE_NAME parameter is read-only too. Rename it by hand: Manage, Phases",
                wasCalled, newName, ex.Message));
        }
        else
        {
            try
            {
                nameParameter.Set(newName);
                route = "PHASE_NAME parameter";
            }
            catch (Exception second)
            {
                refused = (string.Format("'{0}' could not be renamed to '{1}'. Element.Name said: "
                    + "{2} The PHASE_NAME parameter said: {3}", wasCalled, newName,
                    ex.Message, second.Message));
            }
        }
    }

    if (route.Length > 0)
    {
        // THE READ-BACK. Whatever door was used, the question is the same: does
        // the phase now answer to the new name? Only that makes `renamed` true.
        string nowCalled = null;
        try { nowCalled = target.Name; } catch { }

        if (string.Equals(nowCalled, newName, StringComparison.Ordinal))
        {
            renamed = true;
            previousName = wasCalled;
        }
        else
        {
            refused = (string.Format("'{0}' was written to '{1}' through {2} and Revit kept '{3}'. "
                + "The write was accepted and the value was not - nothing threw, so only reading "
                + "it back could show this{4}", wasCalled, newName, route,
                nowCalled == null ? "(unreadable)" : nowCalled,
                nameRefusal.Length > 0 ? ". Element.Name had already refused: " + nameRefusal : ""));
            route = "";
        }
    }
}
