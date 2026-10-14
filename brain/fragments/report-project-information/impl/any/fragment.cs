// NOT STANDALONE. Assumes `doc` is in scope; leaves `projectInfo`, `fields`,
// `notAProject` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// MANAGE > PROJECT INFORMATION, READ. The ten values Revit's dialog always
// shows, in the dialog's own order and under the dialog's own labels - each
// label is the parameter's Definition.Name, never typed here, so a German
// Revit answers in German - and then every other parameter a project has
// added to Project Information, by name. These are the values a title block's
// labels print on every sheet, which is why "what is the project number" is
// asked: the answer is what the sheets will say.
//
// THE TEN ARE FOUND BY BuiltInParameter, NOT BY NAME. Names are translated;
// the ids are not. All ten were read from the reference assemblies of every
// release 2020 to 2027 with tools/api-surface on 2026-10-14, as were
// ProjectInfo's matching properties and InternalDefinition.BuiltInParameter.
//
// AN EMPTY VALUE IS NAMED AS EMPTY, NEVER LEFT OUT. "No client name" and "the
// client name was not read" are different answers, and a title block that
// prints a blank is the reason somebody asked.
//
// ONE STRING, BECAUSE A LIST REACHES THE CHAT CUT TO THREE ITEMS (the reply's
// own rule), and the whole table is the answer.

var projectInfo = "";
var fields = 0;
var notAProject = false;
var findings = "";

{
    var notes = new List<string>();
    var info = doc.IsFamilyDocument ? null : doc.ProjectInformation;

    if (info == null)
    {
        notAProject = true;
        notes.Add("The document in front, \"" + doc.Title + "\", is a family, and Project "
            + "Information belongs to a project - nothing was read");
    }
    else
    {
        // AsValueString first: a number or a date-like parameter as the dialog
        // shows it. AsString for text, where AsValueString returns null.
        Func<Parameter, string> readValue = p =>
        {
            string text = null;
            try { text = p.AsValueString(); } catch { }
            if (string.IsNullOrWhiteSpace(text))
            {
                try { text = p.AsString(); } catch { }
            }
            return text;
        };

        // The dialog's order, top to bottom.
        var builtIns = new[]
        {
            BuiltInParameter.PROJECT_ORGANIZATION_NAME,
            BuiltInParameter.PROJECT_ORGANIZATION_DESCRIPTION,
            BuiltInParameter.PROJECT_BUILDING_NAME,
            BuiltInParameter.PROJECT_AUTHOR,
            BuiltInParameter.PROJECT_ISSUE_DATE,
            BuiltInParameter.PROJECT_STATUS,
            BuiltInParameter.CLIENT_NAME,
            BuiltInParameter.PROJECT_ADDRESS,
            BuiltInParameter.PROJECT_NAME,
            BuiltInParameter.PROJECT_NUMBER,
        };

        var lines = new List<string>();
        var empty = new List<string>();
        foreach (var id in builtIns)
        {
            var p = info.get_Parameter(id);
            if (p == null)
            {
                notes.Add("Revit gave no parameter for " + id + " on this model's Project Information");
                continue;
            }
            var label = p.Definition.Name;
            var value = readValue(p);
            if (string.IsNullOrWhiteSpace(value))
            {
                empty.Add(label);
                lines.Add(label + " = (empty)");
                continue;
            }
            // An address can run over several lines in the dialog; one line here.
            lines.Add(label + " = " + value.Replace("\r\n", ", ").Replace("\n", ", ").Replace("\r", ", ").Trim());
            fields++;
        }

        // Every other parameter on Project Information - project and shared
        // parameters a firm adds for its title block. Settings buttons such as
        // Energy Settings hold an element, not a value, and are left out.
        var others = new List<string>();
        var otherEmpty = 0;
        foreach (Parameter p in info.Parameters)
        {
            // Revit's own parameters - the ten above among them - are skipped;
            // what is left is what the project added.
            var internalDefinition = p.Definition as InternalDefinition;
            if (internalDefinition != null && internalDefinition.BuiltInParameter != BuiltInParameter.INVALID) continue;
            if (p.StorageType == StorageType.ElementId || p.StorageType == StorageType.None) continue;
            var value = readValue(p);
            if (string.IsNullOrWhiteSpace(value))
            {
                otherEmpty++;
                continue;
            }
            others.Add(p.Definition.Name + " = " + value.Replace("\r\n", ", ").Replace("\n", ", ").Replace("\r", ", ").Trim());
        }
        others.Sort(StringComparer.OrdinalIgnoreCase);
        lines.AddRange(others);
        fields += others.Count;

        projectInfo = string.Join("  ||  ", lines.ToArray());

        notes.Insert(0, "Project Information of '" + doc.Title + "': " + fields + " value(s) set - "
            + (builtIns.Length - empty.Count) + " of Revit's " + builtIns.Length + " and " + others.Count
            + " added parameter(s), each 'label = value' in Revit's own labels");
        if (empty.Count > 0)
            notes.Add("empty, so a title block label for it prints blank: " + string.Join(", ", empty.ToArray()));
        if (otherEmpty > 0)
            notes.Add(otherEmpty + " added parameter(s) have no value and are not listed");
    }

    findings = string.Join("; ", notes.ToArray()) + ".";
}
