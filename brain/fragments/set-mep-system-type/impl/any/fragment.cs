// NOT STANDALONE. Assumes `doc`, `elements` and `systemType` are in scope;
// leaves `changed`, `changedIds`, `alreadyThat`, `notChanged`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// WHAT CAN TAKE A SYSTEM TYPE, AND HOW:
//
//   a duct or a pipe     Duct.SetSystemType / Pipe.SetSystemType - the API's
//                        own call for exactly this
//   a flex duct or pipe  no such call exists on any release, so the System
//                        Type parameter itself is written
//   a system             the system element's own type is changed, which is
//                        what the System Browser does
//
// Fittings, accessories and terminals take their system from what they are
// connected to, so they are named and left alone rather than written.
//
// A DUCT TYPE IS NEVER GIVEN A PIPE SYSTEM, and the other way round: the
// domain is checked before anything is written.
//
// ONLY THE MODEL SAYS IT CHANGED. Revit may hold the old type - a run joined
// to a system with base equipment can refuse, or carry the change to the
// whole connected network. So each element's System Type is read back after
// a regeneration, and only a read that equals the asked type counts.

var changed = 0;
var changedIds = new List<ElementId>();
var alreadyThat = new List<ElementId>();
var notChanged = new List<ElementId>();
var findings = new List<string>();
var lines = new List<string>();

var isDuctType = systemType is MechanicalSystemType;
var isPipeType = systemType is PipingSystemType;

Func<Element, ElementId> currentType = e =>
{
    try
    {
        var system = e as MEPSystem;
        if (system != null) return system.GetTypeId();
        var p = e.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
        if (p == null || p.AsElementId() == ElementId.InvalidElementId)
            p = e.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
        return p == null ? ElementId.InvalidElementId : p.AsElementId();
    }
    catch { return ElementId.InvalidElementId; }
};

Func<Element, string> label = e =>
    (e.Category == null ? "element" : e.Category.Name) + " " + e.Id;

var attempted = new List<Element>();

if (systemType == null || (!isDuctType && !isPipeType))
{
    lines.Add("No duct or pipe system type was given, so NOTHING WAS CHANGED. Name one as it "
        + "reads in the Project Browser - Return Air, Domestic Hot Water.");
}
else
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        if (currentType(element) == systemType.Id)
        {
            alreadyThat.Add(element.Id);
            continue;
        }

        var duct = element as Duct;
        var pipe = element as Pipe;
        var system = element as MEPSystem;
        var isDuctSide = duct != null || element is FlexDuct || element is MechanicalSystem;
        var isPipeSide = pipe != null || element is FlexPipe || element is PipingSystem;

        if (!isDuctSide && !isPipeSide)
        {
            notChanged.Add(element.Id);
            lines.Add(label(element) + ": left alone - a fitting, accessory, terminal or "
                + "equipment takes its system from what it is connected to.");
            continue;
        }
        if ((isDuctSide && !isDuctType) || (isPipeSide && !isPipeType))
        {
            notChanged.Add(element.Id);
            lines.Add(label(element) + ": left alone - \"" + systemType.Name + "\" is a "
                + (isDuctType ? "duct" : "pipe") + " system type and this is a "
                + (isDuctSide ? "duct" : "pipe") + ".");
            continue;
        }

        try
        {
            if (duct != null) duct.SetSystemType(systemType.Id);
            else if (pipe != null) pipe.SetSystemType(systemType.Id);
            else if (system != null) system.ChangeTypeId(systemType.Id);
            else
            {
                var p = element.get_Parameter(isDuctSide
                    ? BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM
                    : BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                if (p == null || p.IsReadOnly) throw new InvalidOperationException(
                    "its System Type parameter cannot be written");
                p.Set(systemType.Id);
            }
            attempted.Add(element);
        }
        catch (Exception ex)
        {
            notChanged.Add(element.Id);
            lines.Add(label(element) + ": Revit refused - " + ex.Message);
        }
    }

    doc.Regenerate();

    // READ BACK. Only a System Type that now equals the asked one counts.
    foreach (var element in attempted)
    {
        if (currentType(element) == systemType.Id)
        {
            changed++;
            changedIds.Add(element.Id);
        }
        else
        {
            notChanged.Add(element.Id);
            lines.Add(label(element) + ": asked, and its System Type still reads the old one - "
                + "Revit kept it, usually because the run belongs to a system it will not split.");
        }
    }

    findings.Add("System type \"" + systemType.Name + "\": " + changed + " changed and read back, "
        + alreadyThat.Count + " already that type, " + notChanged.Count + " not changed.");
    if (changed > 0)
        findings.Add("Revit can carry a system type along the whole connected run, so elements "
            + "beyond those handed in may have changed too. Check the System Browser.");
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
