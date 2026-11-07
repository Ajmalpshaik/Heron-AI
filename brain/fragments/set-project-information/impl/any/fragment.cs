// NOT STANDALONE. Assumes `doc` and `values` are in scope; leaves `written`,
// `changes`, `alreadyThat`, `parameterNames`, `notAProject`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list, and a fragment that opened its
// own would add a second.
//
// MANAGE > PROJECT INFORMATION, WRITTEN. `values` is one or more
// "Parameter Name = value" pairs, split by "|" or by line breaks - "Project
// Number=QA-2026-114", "Project Address=12 Corniche Road|Building Name=Tower
// B". The ProjectInfo element is the ONLY element touched: no other element
// is looked up, and nothing is selected.
//
// A NAME IS MATCHED EXACTLY, BY Element.GetParameters(name) - Revit's own
// labels for its ten ("Project Number", "Client Name", "Project Address",
// "Building Name", "Project Status", "Project Issue Date", "Author",
// "Organization Name", "Organization Description", "Project Name") and the
// name of any project or shared parameter bound to Project Information. A
// name that matches none is refused and `parameterNames` lists every name
// Project Information carries; never a near name guessed (D-33). A name two
// parameters share is refused too - a shared parameter can be bound beside
// a built-in of the same label, and Revit's lookup by name would pick one
// (the rule WRITE_ELEMENT_PARAMETERS keeps, FRAGMENT-ISSUES 5b-203).
//
// ALL OR NOTHING. Every pair is checked before anything is written; one
// refused pair - an unknown name, a read-only parameter, one driven by a
// global parameter, a value of the wrong kind - and nothing at all is
// written, with every reason named. Revit refusing a write that passed the
// checks throws, so the operation rolls back whole (Article 8).
//
// TYPE-CORRECT, NEVER A UNIT GUESSED. Text is written as typed (an empty
// value empties it). A Yes/No takes Yes/No, True/False, On/Off, 1/0,
// Ticked/Unticked. A whole number takes digits. Any other number goes
// through Parameter.SetValueString, which parses in the PROJECT's own units
// exactly as the dialog does - the units the modeller sees; no factor is
// applied here. A parameter holding another element (a material, a level, a
// type) is refused by name.
//
// READ BACK. After one regeneration every value is read again: `changes`
// gives each one before and after as Revit shows it. Text and whole numbers
// are compared exactly; a number is compared as the stored value Revit
// parsed straight after the set against the stored value after the
// regeneration. A value already holding what was asked is `alreadyThat`
// and not counted as written.
//
// Every member used was read from the reference assemblies of 2020 to 2027
// with tools/api-surface on 2026-11-02 - see the card's compatibility-note.

var written = 0;
var changes = "";
var alreadyThat = new List<string>();
var parameterNames = "";
var notAProject = false;
string refused = null;
var findings = new List<string>();

{
    var info = doc.IsFamilyDocument ? null : doc.ProjectInformation;

    if (info == null)
    {
        notAProject = true;
        refused = "The document in front, \"" + doc.Title + "\", is a family, and Project Information "
            + "belongs to a project - nothing was written.";
    }
    else
    {
        // Yes/No is an Integer underneath, and only the definition says it is a
        // tick box: GetDataType() against SpecTypeId.Boolean.YesNo from 2022,
        // ParameterType.YesNo on 2020 and 2021. Asked by reflection because the
        // add-in compiles fragments with no release symbols (5b-181) - the way
        // WRITE_ELEMENT_PARAMETERS asks it.
        var yesNoSpecs = typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Boolean");
        var yesNoProperty = yesNoSpecs == null ? null : yesNoSpecs.GetProperty("YesNo",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var yesNoSpec = yesNoProperty == null ? null : yesNoProperty.GetValue(null, null);

        Func<Parameter, bool> isYesNo = p =>
        {
            if (p.StorageType != StorageType.Integer) return false;
            try
            {
                var definition = p.Definition;
                if (definition == null) return false;
                var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
                if (getDataType != null && yesNoSpec != null)
                {
                    var spec = getDataType.Invoke(definition, null);
                    if (spec == null) return false;
                    var nameEquals = spec.GetType().GetMethod("NameEquals", new[] { yesNoSpec.GetType() });
                    return nameEquals != null
                        ? (bool)nameEquals.Invoke(spec, new[] { yesNoSpec })
                        : spec.Equals(yesNoSpec);
                }
                var parameterType = definition.GetType().GetProperty("ParameterType");
                var kind = parameterType == null ? null : parameterType.GetValue(definition, null);
                return kind != null && kind.ToString() == "YesNo";
            }
            catch (Exception)
            {
                return false;
            }
        };

        // As the dialog shows it: AsValueString for a number, a date-like or a
        // Yes/No value, AsString for text. "(empty)" when nothing is there.
        Func<Parameter, string> shown = p =>
        {
            string text = null;
            try { text = p.AsValueString(); } catch (Exception) { }
            if (string.IsNullOrWhiteSpace(text))
            {
                try { text = p.AsString(); } catch (Exception) { }
            }
            if (string.IsNullOrWhiteSpace(text)) return "(empty)";
            return text.Replace("\r\n", ", ").Replace("\n", ", ").Replace("\r", ", ").Trim();
        };

        // Every name Project Information carries that holds a value, sorted,
        // read-only ones marked - what to choose from when a name matched none.
        Func<string> listNames = () =>
        {
            var names = new List<string>();
            foreach (Parameter p in info.Parameters)
            {
                if (p.Definition == null) continue;
                if (p.StorageType == StorageType.None) continue;
                var label = p.Definition.Name;
                if (p.IsReadOnly) label += " (read-only)";
                if (!names.Contains(label)) names.Add(label);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", names.ToArray());
        };

        var ticked = new[] { "yes", "true", "on", "1", "ticked" };
        var unticked = new[] { "no", "false", "off", "0", "unticked" };

        // ---- 1. Parse the pairs. --------------------------------------------
        var problems = new List<string>();
        var askedNames = new List<string>();
        var askedValues = new List<string>();
        foreach (var piece in (values ?? "").Split(new[] { '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = piece.Trim();
            if (pair.Length == 0) continue;
            var equals = pair.IndexOf('=');
            if (equals <= 0)
            {
                problems.Add("'" + pair + "' is not 'Parameter Name = value'");
                continue;
            }
            var name = pair.Substring(0, equals).Trim();
            var value = pair.Substring(equals + 1).Trim();
            if (askedNames.Contains(name))
            {
                problems.Add("'" + name + "' is asked for twice - say which value it should hold");
                continue;
            }
            askedNames.Add(name);
            askedValues.Add(value);
        }
        if (askedNames.Count == 0 && problems.Count == 0)
            problems.Add("no 'Parameter Name = value' pair was given");

        // ---- 2. Check every pair before anything is written. ----------------
        var targets = new List<Parameter>();
        var kinds = new List<string>();     // "text", "yesno", "integer", "number"
        var whole = new List<int>();        // the integer asked, for "yesno" and "integer"
        var unknownName = false;
        for (var i = 0; i < askedNames.Count; i++)
        {
            var name = askedNames[i];
            var value = askedValues[i];
            var matches = info.GetParameters(name);
            if (matches == null || matches.Count == 0)
            {
                problems.Add("Project Information has no parameter named exactly '" + name + "'");
                unknownName = true;
                continue;
            }
            if (matches.Count > 1)
            {
                problems.Add("Project Information carries " + matches.Count + " parameters named '" + name
                    + "' - Revit would pick one of them, so neither is written");
                continue;
            }
            var p = matches[0];
            // VERSION 2 (row 5b-1476): UserModifiable is asked of a SHARED
            // parameter only - it is the lock a shared parameter's definition
            // can carry. Version 1 asked it of every parameter, and Revit's own
            // Project Name and Client Name were refused as read-only on Test for
            // Issue, Revit 2024, 2026-11-07, though both are typed into in the
            // Project Information dialog.
            if (p.IsReadOnly || (p.IsShared && !p.UserModifiable))
            {
                problems.Add("'" + name + "' is read-only - Revit fills it, or it is locked, and it cannot be typed into");
                continue;
            }
            ElementId global = null;
            try { global = p.GetAssociatedGlobalParameter(); } catch (Exception) { global = null; }
            if (global != null && global != ElementId.InvalidElementId)
            {
                var driver = doc.GetElement(global);
                problems.Add("'" + name + "' is driven by the global parameter '"
                    + (driver == null ? "?" : driver.Name) + "' - change that global, or take '" + name
                    + "' off it, instead");
                continue;
            }

            var kind = "";
            var number = 0;
            switch (p.StorageType)
            {
                case StorageType.String:
                    kind = "text";
                    break;
                case StorageType.Integer:
                    if (isYesNo(p))
                    {
                        var word = value.ToLowerInvariant();
                        if (Array.IndexOf(ticked, word) >= 0) { kind = "yesno"; number = 1; }
                        else if (Array.IndexOf(unticked, word) >= 0) { kind = "yesno"; number = 0; }
                        else problems.Add("'" + name + "' is a Yes/No - '" + value
                            + "' is not one of Yes, No, True, False, On, Off, 1, 0, Ticked, Unticked");
                    }
                    else if (int.TryParse(value, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out number))
                    {
                        kind = "integer";
                    }
                    else problems.Add("'" + name + "' holds a whole number - '" + value + "' is not one");
                    break;
                case StorageType.Double:
                    if (value.Length == 0)
                        problems.Add("'" + name + "' holds a number - an empty value is not one, and no zero is assumed");
                    else kind = "number";
                    break;
                case StorageType.ElementId:
                    problems.Add("'" + name + "' holds a reference to another element (a material, a level or a type), "
                        + "which this does not write");
                    break;
                default:
                    problems.Add("'" + name + "' holds no value that can be typed - it is a button in the dialog");
                    break;
            }
            if (kind.Length == 0) continue;
            targets.Add(p);
            kinds.Add(kind);
            whole.Add(number);
        }

        if (problems.Count > 0)
        {
            refused = "NOTHING WAS WRITTEN to Project Information: " + string.Join("; ", problems.ToArray()) + ".";
            if (unknownName)
            {
                parameterNames = listNames();
                findings.Add("Names Project Information carries, matched exactly: " + parameterNames);
            }
        }
        else
        {
            // ---- 3. Write. ----------------------------------------------------
            var before = new List<string>();
            var parsed = new List<double>();   // the stored number straight after SetValueString
            var same = new List<bool>();
            for (var i = 0; i < targets.Count; i++)
            {
                var p = targets[i];
                var value = askedValues[i];
                before.Add(shown(p));
                parsed.Add(0);
                same.Add(false);
                var ok = true;
                switch (kinds[i])
                {
                    case "text":
                        if ((p.AsString() ?? "") == value) { same[i] = true; continue; }
                        ok = p.Set(value);
                        break;
                    case "yesno":
                    case "integer":
                        if (p.HasValue && p.AsInteger() == whole[i]) { same[i] = true; continue; }
                        ok = p.Set(whole[i]);
                        break;
                    case "number":
                        var hadValue = p.HasValue;
                        var was = p.AsDouble();
                        ok = p.SetValueString(value);
                        if (ok)
                        {
                            parsed[i] = p.AsDouble();
                            if (hadValue && Math.Abs(parsed[i] - was) <= Math.Abs(was) * 1e-9 + 1e-12) same[i] = true;
                        }
                        break;
                }
                if (!ok)
                    throw new InvalidOperationException("Revit would not take '" + value + "' for '" + askedNames[i]
                        + "' on Project Information - nothing is kept. A number is read in the project's own units, "
                        + "as the dialog reads it.");
            }

            doc.Regenerate();

            // ---- 4. Read every value back. ------------------------------------
            var rows = new List<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                var p = info.get_Parameter(targets[i].Definition);
                if (p == null)
                {
                    findings.Add("'" + askedNames[i] + "' could not be read back after the write - not counted as written");
                    continue;
                }
                var after = shown(p);
                bool holds;
                switch (kinds[i])
                {
                    case "text":
                        holds = (p.AsString() ?? "") == askedValues[i];
                        break;
                    case "number":
                        holds = Math.Abs(p.AsDouble() - parsed[i]) <= Math.Abs(parsed[i]) * 1e-9 + 1e-12;
                        break;
                    default:
                        holds = p.AsInteger() == whole[i];
                        break;
                }
                if (same[i] && holds)
                {
                    alreadyThat.Add(askedNames[i]);
                    rows.Add(askedNames[i] + ": already " + after + " - not changed");
                    continue;
                }
                if (!holds)
                {
                    findings.Add("'" + askedNames[i] + "' reads " + after + " after the write, not what was asked ('"
                        + askedValues[i] + "') - not counted as written");
                    rows.Add(askedNames[i] + ": " + before[i] + " -> " + after + " (NOT what was asked)");
                    continue;
                }
                written++;
                rows.Add(askedNames[i] + ": " + before[i] + " -> " + after);
                if (kinds[i] == "number")
                    findings.Add("'" + askedNames[i] + "' was read as '" + askedValues[i]
                        + "' in the project's own units, and Revit shows it as " + after);
            }

            changes = string.Join("  ||  ", rows.ToArray());
            findings.Insert(0, "Project Information of '" + doc.Title + "': " + written + " value(s) written and read back, "
                + alreadyThat.Count + " already held what was asked. Nothing but Project Information was touched; "
                + "a title block label showing any of them prints the new value");
        }
    }
}
