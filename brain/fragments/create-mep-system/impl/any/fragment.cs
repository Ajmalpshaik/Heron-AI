// NOT STANDALONE. Assumes `doc`, `elements`, `systemType`, `systemName` and
// `baseEquipment` are in scope; leaves `created`, `createdName`, `memberIds`,
// `baseEquipmentId`, `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ONE FRAGMENT FOR DUCT AND PIPE SYSTEMS. The system type decides which: a
// duct system type makes a mechanical system from HVAC connectors, a pipe
// system type a piping system from piping connectors. The rest of the job -
// which connector of each element joins, which element is the base equipment,
// what was refused - is the same for both, so it is one card and not two.
//
// A CONNECTOR JOINS ONLY WHEN IT SUITS THE SYSTEM. It must be of the system's
// domain, in no system already, and of the system's classification - a Supply
// Air terminal does not go on a Return Air system. A connector whose own
// classification is Global, Fitting or undefined suits any. Revit would refuse
// a wrong one at Add and refuse the WHOLE set with it, so each element is
// checked first and the ones that do not suit are named, not passed on.
//
// THE BASE EQUIPMENT MUST BE MECHANICAL EQUIPMENT - Revit's rule, in the API's
// own words. `baseEquipment` names it by Mark or element id among the elements
// handed in. Left empty, the one Mechanical Equipment instance among them is
// taken; with several, nothing is guessed and the call is refused naming them.
// With none, the system is made without one, which Revit allows, and that is
// said out loud.
//
// EVERYTHING IS READ BACK. Members are counted off the system after it is made,
// not off what was asked, and the base equipment off the system's own field.

var memberIds = new List<ElementId>();
var skipped = new List<ElementId>();
var findings = new List<string>();
string refused = null;
MEPSystem created = null;
var createdName = "";
ElementId baseEquipmentId = ElementId.InvalidElementId;

// Read once: which domain and classification this system type wants.
var isDuct = systemType is MechanicalSystemType;
var isPipe = systemType is PipingSystemType;
var wantedDomain = isDuct ? Domain.DomainHvac : Domain.DomainPiping;
var wantedClass = systemType == null ? "" : systemType.SystemClassification.ToString();

// The free connectors of one element that suit this system - its domain, in no
// system yet, and of a classification that matches or is open to any.
Func<Element, List<Connector>> suiting = element =>
{
    var fit = new List<Connector>();
    var instance = element as FamilyInstance;
    if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null)
        return fit;
    foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
    {
        if (c == null || c.Domain != wantedDomain) continue;
        if (c.ConnectorType != ConnectorType.End) continue;
        try { if (c.MEPSystem != null) continue; } catch { }
        string own = "";
        try { own = isDuct ? c.DuctSystemType.ToString() : c.PipeSystemType.ToString(); } catch { }
        var open = own == "" || own == "Global" || own == "Fitting" || own == "UndefinedSystemType";
        if (open || own == wantedClass) fit.Add(c);
    }
    return fit;
};

Func<Element, string> describe = element =>
{
    var mark = "";
    try
    {
        var p = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        if (p != null) mark = p.AsString() ?? "";
    }
    catch { }
    var kind = element.Category == null ? "element" : element.Category.Name;
    return kind + " " + element.Id + (mark == "" ? "" : " (Mark " + mark + ")");
};

var given = new List<Element>();
foreach (var element in elements)
{
    if (element != null && element.IsValidObject) given.Add(element);
}

if (systemType == null || (!isDuct && !isPipe))
{
    refused = "No duct or pipe system type was given, so NOTHING WAS MADE. Name one as it reads in "
        + "the Project Browser under Duct Systems or Piping Systems - Supply Air, Domestic Cold Water.";
}
else if (given.Count == 0)
{
    refused = "No elements were handed in, so NOTHING WAS MADE. Select the equipment and the "
        + "terminals or fixtures first.";
}
else
{
    // THE BASE EQUIPMENT, chosen before anything is made.
    Element equipment = null;
    var wanted = (baseEquipment ?? "").Trim();
    var candidates = new List<Element>();
    var equipmentCategory = new ElementId(BuiltInCategory.OST_MechanicalEquipment);
    foreach (var element in given)
    {
        if (element.Category == null || element.Category.Id != equipmentCategory) continue;
        candidates.Add(element);
    }

    if (wanted != "")
    {
        foreach (var element in candidates)
        {
            var mark = "";
            try
            {
                var p = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (p != null) mark = p.AsString() ?? "";
            }
            catch { }
            if (string.Equals(mark, wanted, StringComparison.OrdinalIgnoreCase)
                || element.Id.ToString() == wanted)
            {
                if (equipment != null)
                {
                    refused = "Two Mechanical Equipment instances answer to \"" + wanted + "\" - "
                        + describe(equipment) + " and " + describe(element) + " - so NOTHING WAS "
                        + "MADE. Give the element id instead.";
                    break;
                }
                equipment = element;
            }
        }
        if (refused == null && equipment == null)
        {
            refused = "No Mechanical Equipment among the elements handed in has the Mark or id \""
                + wanted + "\", so NOTHING WAS MADE. The base equipment must be one of the "
                + "elements selected, and Revit takes only Mechanical Equipment as base equipment.";
        }
    }
    else if (candidates.Count == 1)
    {
        equipment = candidates[0];
    }
    else if (candidates.Count > 1)
    {
        var names = new List<string>();
        foreach (var element in candidates) names.Add(describe(element));
        refused = candidates.Count + " Mechanical Equipment instances were handed in - "
            + string.Join(", ", names) + " - and nothing says which is the base equipment, so "
            + "NOTHING WAS MADE. Name it by its Mark or id.";
    }

    Connector baseConnector = null;
    if (refused == null && equipment != null)
    {
        var free = suiting(equipment);
        if (free.Count == 0)
        {
            refused = describe(equipment) + " has no free " + (isDuct ? "duct" : "pipe")
                + " connector of the " + wantedClass + " kind, so it cannot be the base "
                + "equipment of this system and NOTHING WAS MADE. Its connectors are all on "
                + "other systems, or of another kind.";
        }
        else
        {
            baseConnector = free[0];
            baseEquipmentId = equipment.Id;
        }
    }

    // THE MEMBERS: one suiting connector from each other element.
    var memberConnectors = new ConnectorSet();
    var asked = 0;
    if (refused == null)
    {
        foreach (var element in given)
        {
            if (equipment != null && element.Id == equipment.Id) continue;
            asked++;
            if (!(element is FamilyInstance))
            {
                skipped.Add(element.Id);
                findings.Add(describe(element) + " left out: ducts, pipes and fittings join a "
                    + "system through their connections, not by being added to it.");
                continue;
            }
            var free = suiting(element);
            if (free.Count == 0)
            {
                skipped.Add(element.Id);
                findings.Add(describe(element) + " left out: no free " + (isDuct ? "duct" : "pipe")
                    + " connector of the " + wantedClass + " kind - already on a system, or of "
                    + "another kind.");
                continue;
            }
            memberConnectors.Insert(free[0]);
        }

        if (memberConnectors.Size == 0 && baseConnector == null)
        {
            refused = "Not one element handed in has a free " + (isDuct ? "duct" : "pipe")
                + " connector of the " + wantedClass + " kind, so NOTHING WAS MADE. A second run "
                + "over elements already on a system finds exactly this, and makes nothing twice.";
        }
    }

    if (refused == null)
    {
        try
        {
            var name = (systemName ?? "").Trim();
            if (isDuct)
                created = name == "" ? MechanicalSystem.Create(doc, systemType.Id)
                                     : MechanicalSystem.Create(doc, systemType.Id, name);
            else
                created = name == "" ? PipingSystem.Create(doc, systemType.Id)
                                     : PipingSystem.Create(doc, systemType.Id, name);
        }
        catch (Exception ex)
        {
            refused = "Revit refused to make the system: " + ex.Message + ". NOTHING WAS MADE.";
        }
    }

    if (refused == null && created != null)
    {
        if (memberConnectors.Size > 0)
        {
            try { created.Add(memberConnectors); }
            catch (Exception ex)
            {
                findings.Add("Revit refused to add the members: " + ex.Message + ". The system "
                    + "exists with only its base equipment, if any.");
            }
        }

        if (baseConnector != null)
        {
            try
            {
                var mechanical = created as MechanicalSystem;
                var piping = created as PipingSystem;
                if (mechanical != null) mechanical.BaseEquipmentConnector = baseConnector;
                else if (piping != null) piping.BaseEquipmentConnector = baseConnector;
            }
            catch (Exception ex)
            {
                findings.Add("Revit refused " + describe(equipment) + " as base equipment: "
                    + ex.Message + ". The system has NO base equipment.");
                baseEquipmentId = ElementId.InvalidElementId;
            }
        }

        doc.Regenerate();

        // READ BACK - members off the system itself.
        try { createdName = created.Name ?? ""; } catch { }
        try
        {
            foreach (Element member in created.Elements)
            {
                if (member != null && member.IsValidObject) memberIds.Add(member.Id);
            }
        }
        catch { }

        var readBase = ElementId.InvalidElementId;
        try { if (created.BaseEquipment != null) readBase = created.BaseEquipment.Id; } catch { }
        if (baseEquipmentId != ElementId.InvalidElementId && readBase != baseEquipmentId)
        {
            findings.Add("The base equipment did not read back as " + describe(equipment)
                + " - Revit holds " + (readBase == ElementId.InvalidElementId ? "none" : readBase.ToString())
                + ". Trust what Revit holds.");
        }
        baseEquipmentId = readBase;

        findings.Add("Made " + (isDuct ? "duct" : "piping") + " system \"" + createdName + "\" of type \""
            + systemType.Name + "\" with " + memberIds.Count + " member(s) read back off it, from "
            + asked + " element(s) offered as members"
            + (baseEquipmentId == ElementId.InvalidElementId
                ? ", and NO base equipment - a valid state Revit allows, said here so it is not missed."
                : ", base equipment " + describe(doc.GetElement(baseEquipmentId)) + "."));
    }
}

if (refused != null) findings.Add(refused);
