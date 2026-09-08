// NOT STANDALONE. Assumes `doc`, `elements`, `systemType` and `panel` are in
// scope; leaves `created`, `circuitNumber`, `panelName`, `memberIds`, `refused`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// CREATE_ELECTRICAL_RUN IS CONTAINMENT, NOT A CIRCUIT. That one draws tray and
// conduit. This is the electrical relationship - what appears on the panel
// schedule. A model can have every tray drawn and no circuit in it.
//
// THE SYSTEM TYPE IS CHECKED, because a fire alarm device landing on a power
// circuit is a drawing that passes review and is wrong.

var memberIds = new List<ElementId>();
var refused = new List<string>();
var findings = new List<string>();
ElectricalSystem created = null;
var circuitNumber = "";
var panelName = "";

var wanted = (systemType ?? "").Trim().ToLowerInvariant();
var kind = ElectricalSystemType.UndefinedSystemType;
var known = true;

if (wanted == "power" || wanted == "power circuit") kind = ElectricalSystemType.PowerCircuit;
else if (wanted == "data") kind = ElectricalSystemType.Data;
else if (wanted == "fire alarm" || wanted == "firealarm") kind = ElectricalSystemType.FireAlarm;
else if (wanted == "security") kind = ElectricalSystemType.Security;
else if (wanted == "telephone") kind = ElectricalSystemType.Telephone;
else if (wanted == "nurse call" || wanted == "nursecall") kind = ElectricalSystemType.NurseCall;
else if (wanted == "controls") kind = ElectricalSystemType.Controls;
else if (wanted == "communication") kind = ElectricalSystemType.Communication;
else known = false;

var ids = new List<ElementId>();
foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;
    ids.Add(element.Id);
}

if (!known)
{
    refused.Add("\"" + systemType + "\" is not a system type this understands, so NOTHING WAS "
        + "CREATED. Use power, data, fire alarm, security, telephone, nurse call, controls or "
        + "communication. It is refused rather than treated as power, because a fire alarm device "
        + "on a power circuit is a drawing that passes review and is wrong.");
}
else if (ids.Count == 0)
{
    refused.Add("No devices were handed in, so NOTHING WAS CREATED. Run the selection first.");
}
else
{
    try
    {
        // Create takes IList<ElementId> and not ICollection<ElementId>. The
        // first attempt at this fragment assumed the latter and the compiler
        // refused it on every release - which is why the list is built as one.
        created = ElectricalSystem.Create(doc, ids, kind);

        if (created == null)
        {
            refused.Add("Revit returned no circuit and raised no error.");
        }
        else
        {
            try { circuitNumber = created.CircuitNumber ?? ""; } catch { }

            // ElementSet is not generic, so the loop variable carries its type.
            try
            {
                foreach (Element member in created.Elements)
                {
                    if (member != null && member.IsValidObject) memberIds.Add(member.Id);
                }
            }
            catch { }

            findings.Add("Created a " + wanted + " circuit"
                + (string.IsNullOrEmpty(circuitNumber) ? "" : " numbered " + circuitNumber)
                + " with " + memberIds.Count + " device(s) on it, from " + ids.Count + " handed in.");

            if (memberIds.Count != ids.Count)
            {
                findings.Add("NOT EVERY DEVICE JOINED IT: " + ids.Count + " were given and "
                    + memberIds.Count + " are on the circuit. Revit accepts only devices whose "
                    + "connectors suit the system type, and the ones it left out are not listed "
                    + "as an error anywhere else.");
            }

            if (panel != null && panel.IsValidObject)
            {
                try
                {
                    created.SelectPanel(panel);
                    panelName = created.PanelName ?? "";
                    findings.Add("Assigned to panel \"" + panelName + "\".");
                }
                catch (Exception ex)
                {
                    refused.Add("The circuit exists but Revit refused the panel: " + ex.Message
                        + ". IT IS UNASSIGNED - it will not appear on any panel schedule.");
                }
            }
            else
            {
                findings.Add("NO PANEL WAS GIVEN, so the circuit is unassigned. That is a real and "
                    + "valid state which Revit allows, and it is said out loud rather than left "
                    + "for somebody to notice on an empty panel schedule.");
            }
        }
    }
    catch (Exception ex)
    {
        refused.Add("Revit refused to create the circuit: " + ex.Message + ". NOTHING WAS CREATED. "
            + "Devices already on a circuit, and devices whose connectors do not suit the system "
            + "type, are the usual reasons.");
    }
}

foreach (var reason in refused) findings.Add(reason);
