// NOT STANDALONE. Assumes `doc`, `namePattern`, `requiredParameters` and
// `mustHaveConnectors` are in scope; leaves `findings`, `offStandard`,
// `notChecked`, `warned`, `familyReport` and `classificationReport` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO DOCUMENTS, TWO AUDITS, ONE QUESTION - IS THIS FAMILY BUILT RIGHT?
//
// IN A PROJECT it audits the family TYPES loaded there (version 1's audit, with
// one change since: a required name the type carries twice is a WARN, 5b-203).
// IN A FAMILY OPEN IN THE FAMILY EDITOR (version 2, 2026-10-04) it audits that
// family itself, BEFORE it is loaded anywhere: will it join its run, and will
// it follow the size of the pipe or duct it is put on? Each check answers
// PASS, FAIL or WARN with the fix in Revit words, and the whole answer is one
// string, `familyReport`, because a list reaches the reply cut to three.
//
// ---------------------------------------------------------------------------
// IN A PROJECT
// ---------------------------------------------------------------------------
//
// IT AUDITS TYPES, NOT INSTANCES. One badly built type placed two hundred times
// is ONE thing to fix; an instance-based report shows it as two hundred
// findings and buries everything else. The instance count beside each row is
// what makes it urgent.
//
// CONNECTORS ARE READ THROUGH A PLACED INSTANCE. A FamilySymbol does not expose
// them - there is no symbol.Connectors - so one instance per type is sampled. A
// type with NO instance placed is reported NOT CHECKED rather than passed, and
// that distinction is the difference between an audit and a guess.
//
// SYSTEM FAMILIES HAVE NO FAMILY FILE - duct types, wall types, pipe types.
// Counted and excluded, never silently skipped.
//
// A REQUIRED NAME THE TYPE CARRIES TWICE IS A WARN, NOT A PASS. A shared or
// project parameter can be bound beside a family's own one of the same name,
// and asked by name Revit then returns "the first one encountered", which its
// own reference says "is determined at random". So the parameters are COUNTED
// by name, never looked up: none is a FAIL, one is a pass, and two or more is
// the family document's WARN - present, but which one a schedule, a tag or a
// lookup by that name shows is Revit's pick (D-54 s3, FRAGMENT-ISSUES 5b-203).
//
// ---------------------------------------------------------------------------
// IN A FAMILY
// ---------------------------------------------------------------------------
//
// THE CHECKS GO BY CATEGORY AND PART TYPE, NEVER BY WHAT THE FAMILY IS CALLED.
// Pipe and duct accessories are INLINE: they break into their run. Pipe, duct,
// cable tray and conduit fittings are the run's own joints. Equipment,
// terminals, fixtures and devices sit at the end of a run. Every other
// category gets the name, parameter and tidiness checks only.
//
// 1. PLACEMENT. An inline accessory or a fitting that is Work Plane-Based will
//    not break into a pipe - measured 2026-10-04 on two PPR check valves that
//    would not connect until it was No. An inline accessory whose Part Type is
//    Normal will not break in either; a fitting whose Part Type is Normal is in
//    no routing preference group.
// 2. CONNECTORS. One pointing toward the family's own centre (FRAGMENT-ISSUES
//    5b-309); a pass-through pair not opposite on one line; a size no family
//    parameter drives; a size driven by a TYPE parameter, or by one with a
//    formula, which a pipe cannot change - so the family keeps its size when
//    the pipe is resized; and an inline accessory's System Classification not
//    Global (a WARN: some offices fix it on purpose).
// 2b. A DUCT OR PIPE FITTING'S OWN RULES (version 4, 2026-10-28, row 5b-1346).
//    Its run connectors against its Part Type - an elbow two, a tee three, a
//    cross four, a cap one (a FAIL: Revit places the part its Part Type names);
//    each one's System Classification Fitting, as Revit's own fittings' are
//    (a WARN); one primary, lying on the X axis, where Autodesk's help puts it
//    (a WARN); and an elbow's connector Angle driven by an INSTANCE family
//    parameter - by none is a WARN until NEEDS-CHECKING Group JN reads Revit's
//    own elbow, by a TYPE one a FAIL, as a size driven by a type one is.
// 3. TIDINESS. A family parameter nothing uses - no formula, label, array,
//    form field, nested family or connector reads it; a solid form shown at
//    Coarse or Medium where a nested family already draws that level.
//
// WHERE HERON HAS THE FIX, THE TOOL IS NAMED. SET_FAMILY_SETTINGS,
// SET_FAMILY_CONNECTOR_ROLES, DELETE_FAMILY_PARAMETERS and
// SET_FAMILY_FORM_VISIBILITY, and ADD_FAMILY_PARAMETERS version 2 (5b-308) for
// a size parameter to switch from type to instance with its formulas.
//
// ---------------------------------------------------------------------------
// CLASSIFICATION (version 3, 2026-10-11), IN BOTH DOCUMENTS
// ---------------------------------------------------------------------------
//
// THE OMNICLASS NUMBER ("Classification Number" from Revit 2026) against the
// classification file Revit's own dialog offers: a code the file does not hold,
// or files under another category or none, is wrong; a code the file gives
// this category is right. A category the file gives no code is not judged, and
// with no file nothing is judged - NOT CHECKED, never a pass.
//
// A BLANK ONE IS A WARN IN THE FAMILY and only COUNTED IN A PROJECT - there is
// no universal standard that every family carries one (D-33) - unless
// `requiredParameters` names it, which is the office saying it is required.
//
// EACH TYPE'S ASSEMBLY CODE against the document's Assembly Code table (not in
// it, or an entry for another category, is wrong; blank is fine - it is
// optional), and its KEYNOTE against the keynote table (a WARN, since a keynote
// file is the office's choice). An empty or absent table is NOT CHECKED. The
// fix for both is SET_FAMILY_TYPE_VALUES, which writes them as text.

var findings = new List<string>();
var offStandard = 0;
var notChecked = 0;
var warned = 0;
var familyReport = "";
var classificationReport = "";

// ITS BUILT-IN PARAMETERS WERE RENAMED AT REVIT 2026 AND KEPT THEIR NUMBERS:
// OMNICLASS_CODE, OMNICLASS_DESCRIPTION and UNIFORMAT_CODE up to 2025;
// CLASSIFICATION_CODE, CLASSIFICATION_DESCRIPTION and ASSEMBLY_CODE from 2026,
// and neither set exists on the other side. Naming either in code breaks the
// build on the other releases, so the one this Revit has is found by NAME at
// run time, and shown in Revit's own words.
var clsNewNames = Enum.IsDefined(typeof(BuiltInParameter), "CLASSIFICATION_CODE");
var clsCode = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter), clsNewNames ? "CLASSIFICATION_CODE" : "OMNICLASS_CODE");
var clsAssembly = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter),
    Enum.IsDefined(typeof(BuiltInParameter), "ASSEMBLY_CODE") ? "ASSEMBLY_CODE" : "UNIFORMAT_CODE");
Func<BuiltInParameter, string, string> clsLabelFor = (clsBip, clsOtherwise) =>
{
    try
    {
        var clsSaid = LabelUtils.GetLabelFor(clsBip);
        return string.IsNullOrEmpty(clsSaid) ? clsOtherwise : clsSaid;
    }
    catch (Exception) { return clsOtherwise; }
};
var clsCodeLabel = clsLabelFor(clsCode, clsNewNames ? "Classification Number" : "OmniClass Number");
var clsAssemblyLabel = clsLabelFor(clsAssembly, "Assembly Code");
var clsKeynoteLabel = clsLabelFor(BuiltInParameter.KEYNOTE_PARAM, "Keynote");

// Trimmed of every character char.IsWhiteSpace calls space - the 2020 to 2025
// file ends some titles in a non-breaking space.
Func<string, string> clsTidy = clsText =>
{
    var clsS = clsText ?? "";
    int clsFrom = 0, clsTo = clsS.Length;
    while (clsFrom < clsTo && char.IsWhiteSpace(clsS[clsFrom])) clsFrom++;
    while (clsTo > clsFrom && char.IsWhiteSpace(clsS[clsTo - 1])) clsTo--;
    return clsS.Substring(clsFrom, clsTo - clsFrom);
};

// THE CLASSIFICATION FILE - the list Revit's own OmniClass Number dialog
// offers. No API reads it; it is a plain file, one row per line, TAB between
// code, title, level and the BuiltInCategory it is filed under (that last one
// may be empty), and '#' starts a comment: "OmniClassTaxonomy.txt" up to 2025,
// "Classification_Taxonomy.txt" from 2026, the name that matches the parameter
// found above tried first, and the path actually read is said. ONLY THE
// PER-USER COPY IS READ - the one Revit itself reads, in
// Application.CurrentUsersDataFolderPath, which Revit fills from its own cache
// at start-up. That cache sits under a Windows special folder, and only
// HeronPaths may resolve one (tools/check-structure.py, docs/06 section 2).
var clsTried = new List<string>();
string clsPath = null;
string clsProblem = null;
var clsSample = false;
// code, title, the category column as written, the BuiltInCategory (boxed) or null
var clsRows = new List<Tuple<string, string, string, object>>();
Action clsLoad = () =>
{
    var clsFolders = new List<string>();
    try
    {
        var clsUser = doc.Application.CurrentUsersDataFolderPath;
        if (!string.IsNullOrEmpty(clsUser)) clsFolders.Add(clsUser);
    }
    catch (Exception) { }
    var clsNames = clsNewNames
        ? new[] { "Classification_Taxonomy.txt", "OmniClassTaxonomy.txt" }
        : new[] { "OmniClassTaxonomy.txt", "Classification_Taxonomy.txt" };
    foreach (var clsName in clsNames)
        foreach (var clsFolder in clsFolders)
        {
            if (clsPath != null) break;
            var clsCandidate = System.IO.Path.Combine(clsFolder, clsName);
            clsTried.Add(clsCandidate);
            if (System.IO.File.Exists(clsCandidate)) clsPath = clsCandidate;
        }
    if (clsPath == null) return;
    string clsAll;
    try
    {
        // THE 2020 TO 2025 FILE IS NOT UTF-8: it holds Windows-1252 bytes (an
        // en dash, an e-cedilla). Strict UTF-8 first; then code page 1252,
        // which .NET 8 and 10 refuse unless a code-page provider is
        // registered; then Latin-1, which every runtime has.
        var clsBytes = System.IO.File.ReadAllBytes(clsPath);
        try { clsAll = new System.Text.UTF8Encoding(false, true).GetString(clsBytes); }
        catch (Exception)
        {
            try { clsAll = System.Text.Encoding.GetEncoding(1252).GetString(clsBytes); }
            catch (Exception) { clsAll = System.Text.Encoding.GetEncoding(28591).GetString(clsBytes); }
        }
    }
    catch (Exception ex)
    {
        clsProblem = "the classification file " + clsPath + " could not be read: " + ex.Message;
        return;
    }
    clsSample = clsAll.IndexOf("sample Classification File", StringComparison.OrdinalIgnoreCase) >= 0;
    foreach (var clsLine in clsAll.Replace(((char)0xFEFF).ToString(), "").Split((char)10))
    {
        var clsTrimmed = clsTidy(clsLine);
        if (clsTrimmed.Length == 0 || clsTrimmed.StartsWith("#")) continue;
        var clsColumns = clsLine.Split((char)9);
        if (clsColumns.Length < 2) continue;
        var clsRowCode = clsTidy(clsColumns[0]);
        if (clsRowCode.Length == 0) continue;
        var clsRaw = clsColumns.Length > 3 ? clsTidy(clsColumns[3]) : "";
        object clsFiled = null;
        long clsNumber;
        if (clsRaw.Length > 0 && long.TryParse(clsRaw, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out clsNumber))
        {
            var clsEnum = Enum.ToObject(typeof(BuiltInCategory), clsNumber);
            if (Enum.IsDefined(typeof(BuiltInCategory), clsEnum)) clsFiled = clsEnum;
        }
        clsRows.Add(Tuple.Create(clsRowCode, clsTidy(clsColumns[1]), clsRaw, clsFiled));
    }
};
// Filed under this category: compared as ElementIds, never as numbers.
Func<Tuple<string, string, string, object>, ElementId, bool> clsFiledUnder = (clsRow, clsCategoryId) =>
    clsRow.Item4 != null && clsCategoryId != null && new ElementId((BuiltInCategory)clsRow.Item4).Equals(clsCategoryId);
Func<Tuple<string, string, string, object>, string> clsFiledWords = clsRow =>
{
    if (clsRow.Item3.Length == 0) return "no category";
    if (clsRow.Item4 == null) return "a category this Revit does not have (" + clsRow.Item3 + ")";
    var clsBic = (BuiltInCategory)clsRow.Item4;
    try
    {
        var clsFound = Category.GetCategory(doc, clsBic);
        if (clsFound != null) return clsFound.Name;
    }
    catch (Exception) { }
    try { return LabelUtils.GetLabelFor(clsBic); }
    catch (Exception) { return clsBic.ToString(); }
};

// THE DOCUMENT'S ASSEMBLY CODE AND KEYNOTE TABLES, by key. Either may be
// absent or empty - in a family document above all - and then a code cannot
// be checked: NOT CHECKED, never a pass.
var clsAssemblyTable = new Dictionary<string, ClassificationEntry>(StringComparer.Ordinal);
var clsKeynoteKeys = new HashSet<string>(StringComparer.Ordinal);
try
{
    var clsTable = AssemblyCodeTable.GetAssemblyCodeTable(doc);
    var clsEntries = clsTable == null ? null : clsTable.GetKeyBasedTreeEntries();
    if (clsEntries != null)
        foreach (var clsEntry in clsEntries)
        {
            var clsKeyed = clsEntry as ClassificationEntry;
            var clsKey = clsKeyed == null ? "" : clsTidy(clsKeyed.Key);
            if (clsKey.Length > 0 && !clsAssemblyTable.ContainsKey(clsKey)) clsAssemblyTable[clsKey] = clsKeyed;
        }
}
catch (Exception) { }
try
{
    var clsTable = KeynoteTable.GetKeynoteTable(doc);
    var clsEntries = clsTable == null ? null : clsTable.GetKeyBasedTreeEntries();
    if (clsEntries != null)
        foreach (var clsEntry in clsEntries)
        {
            var clsKey = clsEntry == null ? "" : clsTidy(clsEntry.Key);
            if (clsKey.Length > 0) clsKeynoteKeys.Add(clsKey);
        }
}
catch (Exception) { }
// An Assembly Code entry filed for ANOTHER category of this document: its
// name, or null - no category, a bad one, this one, or one the document does
// not hold is not judged.
Func<ClassificationEntry, ElementId, string> clsAssemblyOther = (clsEntry, clsCategoryId) =>
{
    try
    {
        if (clsEntry == null || clsCategoryId == null || clsEntry.HasBadCategoryId()) return null;
        var clsId = clsEntry.CategoryId;
        if (clsId == null || clsId == ElementId.InvalidElementId || clsId.Equals(clsCategoryId)) return null;
        var clsOther = Category.GetCategory(doc, clsId);
        return clsOther == null ? null : clsOther.Name;
    }
    catch (Exception) { return null; }
};
Func<List<string>, string> clsFirstTen = clsNames => clsNames.Count == 0 ? ""
    : " (" + string.Join(", ", clsNames.Take(10)) + (clsNames.Count > 10 ? ", and " + (clsNames.Count - 10) + " more" : "") + ")";

if (doc.IsFamilyDocument)
{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var halfMillimetre = 0.5 / 304.8;
    Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

    // "ValveBreaksInto" read as "Valve Breaks Into", the way a modeller says it.
    Func<string, string> spaced = name =>
    {
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < (name ?? "").Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
            text.Append(name[i]);
        }
        return text.ToString();
    };

    var family = doc.OwnerFamily;
    var fm = doc.FamilyManager;
    var category = family == null ? null : family.FamilyCategory;
    var categoryName = category == null ? "no category" : category.Name;
    var familyName = (doc.Title ?? "").EndsWith(".rfa", StringComparison.OrdinalIgnoreCase)
        ? doc.Title.Substring(0, doc.Title.Length - 4) : (doc.Title ?? "");

    // A CATEGORY BY ITS BuiltInCategory NAME, read on the running Revit, so a
    // name one release has not got is simply not this family's category.
    Func<string, bool> isCategory = name =>
    {
        if (category == null) return false;
        BuiltInCategory bic;
        if (!Enum.TryParse<BuiltInCategory>(name, out bic)) return false;
        return category.Id.Equals(new ElementId(bic));
    };
    var inlineAccessory = isCategory("OST_PipeAccessory") || isCategory("OST_DuctAccessory");
    var fitting = isCategory("OST_PipeFitting") || isCategory("OST_DuctFitting")
        || isCategory("OST_CableTrayFitting") || isCategory("OST_ConduitFitting");
    var endOfRun = new[]
    {
        "OST_MechanicalEquipment", "OST_PlumbingFixtures", "OST_PlumbingEquipment", "OST_DuctTerminal",
        "OST_Sprinklers", "OST_ElectricalEquipment", "OST_ElectricalFixtures", "OST_LightingFixtures",
        "OST_LightingDevices", "OST_CommunicationDevices", "OST_DataDevices", "OST_FireAlarmDevices",
        "OST_SecurityDevices", "OST_NurseCallDevices", "OST_TelephoneDevices", "OST_MechanicalControlDevices",
        "OST_MedicalEquipment", "OST_FireProtection",
    }.Any(n => isCategory(n));
    var mep = inlineAccessory || fitting || endOfRun;

    var rows = new List<string>();
    var passed = 0;
    Action<string, string> fail = (what, fix) =>
    {
        offStandard++;
        rows.Add("FAIL " + what + (string.IsNullOrEmpty(fix) ? "" : " FIX: " + fix));
    };
    Action<string, string> warn = (what, fix) =>
    {
        warned++;
        rows.Add("WARN " + what + (string.IsNullOrEmpty(fix) ? "" : " FIX: " + fix));
    };
    Action<string> pass = what => { passed++; rows.Add("PASS " + what); };
    Action<string> notRead = what => { notChecked++; rows.Add("NOT CHECKED " + what); };

    // ---- every family parameter, once
    var parameters = new List<FamilyParameter>();
    foreach (FamilyParameter p in fm.Parameters) parameters.Add(p);
    var parameterNames = parameters.Select(p => p.Definition.Name).ToList();

    // THE NAME AS A WHOLE TOKEN IN A FORMULA - DELETE_FAMILY_PARAMETERS' rule,
    // the same words: "Width" is not found inside "Width Offset".
    Func<string, string, bool> formulaUses = (formula, name) =>
    {
        if (string.IsNullOrEmpty(formula) || string.IsNullOrEmpty(name)) return false;
        var text = formula;
        foreach (var longer in parameterNames.Where(n => n.Length > name.Length
            && n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(n => n.Length))
        {
            var at = text.IndexOf(longer, StringComparison.OrdinalIgnoreCase);
            while (at >= 0)
            {
                text = text.Substring(0, at) + new string('#', longer.Length) + text.Substring(at + longer.Length);
                at = text.IndexOf(longer, at + longer.Length, StringComparison.OrdinalIgnoreCase);
            }
        }
        Func<char, bool> joins = c => char.IsLetterOrDigit(c) || c == '_' || c == '#';
        var from = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        while (from >= 0)
        {
            var end = from + name.Length;
            if ((from == 0 || !joins(text[from - 1])) && (end >= text.Length || !joins(text[end]))) return true;
            from = text.IndexOf(name, from + 1, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    };

    // Every parameter whose formula reads this one, directly or through
    // another - what has to change with it when it changes kind.
    Func<string, List<string>> dependents = name =>
    {
        var found = new List<string>();
        var frontier = new List<string> { name };
        while (frontier.Count > 0)
        {
            var next = parameters
                .Where(p => !found.Contains(p.Definition.Name) && p.Definition.Name != name)
                .Where(p => frontier.Any(f => formulaUses(p.Formula, f)))
                .Select(p => p.Definition.Name).ToList();
            found.AddRange(next);
            frontier = next;
        }
        return found;
    };

    // =======================================================================
    // NAME AND REQUIRED PARAMETERS - the project audit's two questions, asked
    // of the family itself
    // =======================================================================

    if (!string.IsNullOrEmpty(namePattern))
    {
        if (familyName.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) < 0)
            fail("Name: \"" + familyName + "\" does not contain \"" + namePattern + "\".",
                "rename the family file before it is loaded (RENAME_FAMILY renames it in a project).");
        else pass("Name contains \"" + namePattern + "\".");
    }
    foreach (var wanted in requiredParameters ?? new List<string>())
    {
        if (string.IsNullOrEmpty(wanted)) continue;
        var matches = parameters.Count(p => string.Equals(p.Definition.Name, wanted, StringComparison.Ordinal));
        if (matches == 0)
            fail("Required parameter \"" + wanted + "\" is not in this family.",
                "add it in Family Types (ADD_FAMILY_PARAMETERS).");
        else if (matches > 1)
            warn("Required parameter \"" + wanted + "\" is in this family " + matches + " times - a schedule or "
                + "tag reads whichever Revit finds first.", "keep one, delete the other.");
        else pass("Required parameter \"" + wanted + "\" is present.");
    }

    // =======================================================================
    // 1. PLACEMENT - the switches that stop a family joining its run
    // =======================================================================

    Func<BuiltInParameter, int?> setting = bip =>
    {
        try
        {
            var p = family == null ? null : family.get_Parameter(bip);
            return p == null || p.StorageType != StorageType.Integer ? (int?)null : p.AsInteger();
        }
        catch (Exception) { return null; }
    };
    var workPlaneBased = setting(BuiltInParameter.FAMILY_WORK_PLANE_BASED);
    var partValue = setting(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
    var partType = partValue.HasValue && Enum.IsDefined(typeof(PartType), partValue.Value)
        ? Enum.GetName(typeof(PartType), partValue.Value) : null;
    var partWords = partType == null ? "unread" : spaced(partType);

    // THE CONNECTORS, grouped by domain: a pass-through pair is two of one kind.
    var connectors = new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement))
        .Cast<ConnectorElement>().OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();
    Func<ConnectorElement, string> domainWords = c =>
        c.Domain == Domain.DomainHvac ? "duct" : c.Domain == Domain.DomainPiping ? "pipe"
        : c.Domain == Domain.DomainElectrical ? "electrical"
        : c.Domain == Domain.DomainCableTrayConduit ? "cable tray or conduit" : c.Domain.ToString();
    Func<ConnectorElement, string> label = c => domainWords(c) + " connector " + c.Id.ToString();
    var runDomain = connectors.Where(c => c.Domain == Domain.DomainHvac || c.Domain == Domain.DomainPiping
            || c.Domain == Domain.DomainCableTrayConduit)
        .GroupBy(c => c.Domain).OrderByDescending(g => g.Count()).FirstOrDefault();
    var runConnectors = runDomain == null ? new List<ConnectorElement>() : runDomain.ToList();

    // Part types that break into a run, by enum name - a name a release has
    // not got is simply never matched.
    var breaksInto = new[] { "BreaksInto", "ValveBreaksInto", "InlineSensor", "Damper" };
    var passThroughFittings = new[] { "Union", "Transition", "PipeFlange", "PipeMechanicalCoupling" };

    if (inlineAccessory || fitting)
    {
        if (!workPlaneBased.HasValue)
            notRead("Work Plane-Based: this family has no such setting to read.");
        else if (workPlaneBased.Value == 1)
            fail("Work Plane-Based is Yes on a " + categoryName + " family. A family that breaks into its run "
                + "must be No (Always Vertical Yes) - Work Plane-Based ones are placed on a plane, not in a pipe or "
                + "duct, and will not connect.",
                "Family Category and Parameters, Work Plane-Based off (SET_FAMILY_SETTINGS \"Work Plane-Based=No\").");
        else pass("Work Plane-Based is No.");
    }

    if (inlineAccessory)
    {
        if (partType == null)
            notRead("Part Type could not be read.");
        else if (breaksInto.Contains(partType))
            pass("Part Type is " + partWords + ".");
        else if (runConnectors.Count >= 2 && (partType == "Normal" || partType == "Undefined"))
            fail("Part Type is " + partWords + " on a " + categoryName + " family with " + runConnectors.Count + " "
                + domainWords(runConnectors[0]) + " connectors. An inline accessory breaks into its run only when its "
                + "Part Type is Breaks Into (Valve - Breaks Into for a valve) or In-line Sensor.",
                "Family Category and Parameters, Part Type (SET_FAMILY_SETTINGS \"Part Type=Valve Breaks Into\").");
        else
            warn("Part Type is " + partWords + " with " + runConnectors.Count + " run connector(s) - it will not "
                + "break into a pipe or duct. Right for an accessory that attaches to the outside of one; wrong for "
                + "one meant to sit in the run.",
                "if it should break in, SET_FAMILY_SETTINGS \"Part Type=Breaks Into\".");
    }
    else if (fitting)
    {
        if (partType == null)
            notRead("Part Type could not be read.");
        else if (partType == "Normal" || partType == "Undefined")
            fail("Part Type is " + partWords + " on a " + categoryName + " family. A fitting is offered by routing "
                + "preferences, and resized with its run, only by its Part Type - Elbow, Tee, Transition, Union, Cap.",
                "SET_FAMILY_SETTINGS \"Part Type=Elbow\" (or the part it is).");
        else pass("Part Type is " + partWords + ".");
    }

    // THE FAMILY'S OWN CENTRE: the middle of everything solid it draws -
    // its solid forms and its nested families. Voids are left out: they
    // cut, they do not draw. With nothing solid, the middle of the
    // connectors, when there are two or more.
    XYZ low = null, high = null;
    Action<BoundingBoxXYZ> grow = box =>
    {
        if (box == null) return;
        low = low == null ? box.Min : new XYZ(Math.Min(low.X, box.Min.X), Math.Min(low.Y, box.Min.Y), Math.Min(low.Z, box.Min.Z));
        high = high == null ? box.Max : new XYZ(Math.Max(high.X, box.Max.X), Math.Max(high.Y, box.Max.Y), Math.Max(high.Z, box.Max.Z));
    };
    var solidForms = new List<GenericForm>();
    var nestedInstances = new List<FamilyInstance>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        var form = element as GenericForm;
        var nested = element as FamilyInstance;
        // A nested annotation symbol draws in plan only; it is not the body.
        if (nested != null && nested.Category != null && nested.Category.CategoryType == CategoryType.Annotation) continue;
        try
        {
            if (form != null && form.IsSolid) { solidForms.Add(form); grow(form.get_BoundingBox(null)); }
            else if (nested != null) { nestedInstances.Add(nested); grow(nested.get_BoundingBox(null)); }
        }
        catch (Exception) { }
    }
    XYZ centre = null;
    var centreFrom = "";
    if (low != null) { centre = (low + high) / 2; centreFrom = "the middle of its solid forms and nested families"; }
    else if (connectors.Count >= 2)
    {
        var sum = XYZ.Zero;
        foreach (var c in connectors) sum = sum + c.Origin;
        centre = sum / connectors.Count;
        centreFrom = "the middle of its connectors";
    }

    // =======================================================================
    // 2. CONNECTORS
    // =======================================================================

    if (connectors.Count == 0)
    {
        if (mep || mustHaveConnectors)
            fail("No connectors. A " + categoryName + " family with none cannot join a system, carry flow or show "
                + "in the System Browser, while looking normal in every plan.",
                "ADD_FAMILY_CONNECTOR on each port face.");
    }
    else if (mep || mustHaveConnectors)
    {
        // ---- pointing inward
        foreach (var c in connectors)
        {
            XYZ origin = null, direction = null;
            try { origin = c.Origin; direction = c.Direction; }
            catch (Exception) { }
            if (centre == null || origin == null || direction == null)
            {
                notRead("Which way " + label(c) + " points: the family has no solid form to find its centre by.");
                continue;
            }
            var toCentre = centre - origin;
            if (toCentre.GetLength() < halfMillimetre * 2)
            {
                notRead("Which way " + label(c) + " points: it sits at the family's centre.");
                continue;
            }
            if (toCentre.Normalize().DotProduct(direction) > 0.5)
                fail(label(c) + " at X " + mm(origin.X) + ", Y " + mm(origin.Y) + ", Z " + mm(origin.Z) + " mm points "
                    + "INTO the family, toward " + centreFrom + ". A pipe or duct joined to it would run back through "
                    + "the body; the arrow must point out of the port.",
                    "SET_FAMILY_CONNECTOR_ROLES settings \"flip=yes\" on it (or the connector's Flip control).");
            else pass(label(c) + " points out of the family.");
        }

        // ---- a pass-through pair, opposite and on one line
        var pairExpected = (inlineAccessory && runConnectors.Count == 2)
            || (fitting && runConnectors.Count == 2 && partType != null && passThroughFittings.Contains(partType));
        if (pairExpected)
        {
            var a = runConnectors[0];
            var b = runConnectors[1];
            try
            {
                var opposite = a.Direction.DotProduct(b.Direction) < -0.9999;
                var offset = (b.Origin - a.Origin).CrossProduct(a.Direction).GetLength();
                if (!opposite)
                    fail("The two " + domainWords(a) + " connectors " + a.Id + " and " + b.Id + " are not opposite each "
                        + "other, so the family cannot sit in a straight run - a pipe or duct reaching one port meets "
                        + "the other at an angle.",
                        "flip the one pointing the wrong way (SET_FAMILY_CONNECTOR_ROLES \"flip=yes\"), or move it to "
                        + "the opposite face.");
                else if (offset > halfMillimetre)
                    warn("The two " + domainWords(a) + " connectors " + a.Id + " and " + b.Id + " are opposite but "
                        + mm(offset) + " mm off one line - the run steps sideways through the family. Right for an "
                        + "eccentric part; wrong for a straight-through valve.",
                        "line the two port faces up on one reference plane.");
                else pass("The two " + domainWords(a) + " connectors are opposite each other on one line.");
            }
            catch (Exception) { notRead("Whether the two run connectors line up: their directions could not be read."); }
        }

        // ---- size: associated, instance, not a formula
        var checkedDrivers = new List<string>();
        foreach (var c in connectors)
        {
            if (c.Domain == Domain.DomainElectrical) continue;
            var sizeParameters = new List<BuiltInParameter>();
            try
            {
                if (c.Shape == ConnectorProfileType.Round)
                    sizeParameters.AddRange(new[] { BuiltInParameter.CONNECTOR_DIAMETER, BuiltInParameter.CONNECTOR_RADIUS });
                else sizeParameters.AddRange(new[] { BuiltInParameter.CONNECTOR_WIDTH, BuiltInParameter.CONNECTOR_HEIGHT });
            }
            catch (Exception) { notRead("The size of " + label(c) + ": its shape could not be read."); continue; }

            var drivers = new List<FamilyParameter>();
            foreach (var bip in sizeParameters)
            {
                try
                {
                    var own = c.get_Parameter(bip);
                    var driver = own == null ? null : fm.GetAssociatedFamilyParameter(own);
                    if (driver != null) drivers.Add(driver);
                }
                catch (Exception) { }
            }
            // Round: diameter OR radius drives it. Rectangular: both sides must.
            var sized = c.Shape == ConnectorProfileType.Round ? drivers.Count > 0 : drivers.Count >= 2;
            var followsRun = inlineAccessory || fitting;
            if (!sized)
            {
                var what = label(c) + "'s size is driven by " + (drivers.Count == 0 ? "no family parameter"
                    : "only " + drivers[0].Definition.Name) + ", so it stays the size it was drawn at.";
                var fix = "associate the connector's " + (c.Shape == ConnectorProfileType.Round ? "Diameter" : "Width and Height")
                    + " with the family's size parameter (SET_FAMILY_CONNECTOR_ROLES settings \"sizeParameters=<the size parameter>\").";
                if (followsRun) fail(what + " A " + categoryName + " family has to take its pipe or duct's size.", fix);
                else warn(what + " Right only if every type of this " + categoryName + " has one fixed size.", fix);
                continue;
            }
            foreach (var driver in drivers)
            {
                var name = driver.Definition.Name;
                if (checkedDrivers.Contains(name)) continue;
                checkedDrivers.Add(name);
                if (!followsRun) { pass(label(c) + "'s size is driven by \"" + name + "\"."); continue; }

                var reading = dependents(name);
                var direct = parameters.Count(p => p.Definition.Name != name && formulaUses(p.Formula, name));
                if (!driver.IsInstance)
                    fail("\"" + name + "\", which sizes " + label(c) + ", is a TYPE parameter - so the family keeps its "
                        + "type's size and will NOT follow the pipe or duct it is placed on. " + reading.Count
                        + " formula(s) depend on it (" + direct + " directly)"
                        + (reading.Count > 0 ? ": " + string.Join(", ", reading.Take(12)) + (reading.Count > 12 ? ", ..." : "") : "")
                        + ".",
                        "switch it to Instance with every parameter whose formula reads it - a type formula cannot "
                        + "read an instance parameter: ADD_FAMILY_PARAMETERS switchExisting=true, instance=true, "
                        + "parameterNames=\"" + string.Join(", ", new[] { name }.Concat(reading)) + "\" (it orders "
                        + "the switch itself), or Family Types, each one, Modify, Instance.");
                else if (!string.IsNullOrEmpty(driver.Formula))
                    fail("\"" + name + "\", which sizes " + label(c) + ", is calculated by the formula " + driver.Formula
                        + " - so the pipe or duct cannot set it, and the family will not follow its size.",
                        "clear that formula (SET_FAMILY_FORMULA) and drive the other sizes from \"" + name + "\" instead.");
                else pass("\"" + name + "\", which sizes " + label(c) + ", is an instance parameter with no formula, "
                    + reading.Count + " formula(s) follow it.");
            }
        }

        // ---- system classification on an inline accessory
        if (inlineAccessory)
            foreach (var c in runConnectors)
            {
                string system = null;
                try { system = c.SystemClassification.ToString(); }
                catch (Exception) { }
                if (system == null) notRead("The System Classification of " + label(c) + " could not be read.");
                else if (system == "Global") pass(label(c) + " is Global.");
                else warn(label(c) + " is " + spaced(system) + ", not Global - the accessory only connects to that "
                    + "system. Right if your office fixes it on purpose; otherwise it should take the system of "
                    + "the pipe or duct it is put on.",
                    "SET_FAMILY_CONNECTOR_ROLES settings \"system=Global\".");
            }

        // ---- VERSION 4: A DUCT OR PIPE FITTING'S OWN RULES (row 5b-1346)
        //
        // Only the two categories routing preferences offer, and only their
        // own domain's connectors: a pipe fitting's run is pipe connectors.
        var pipeFitting = isCategory("OST_PipeFitting");
        var ductFitting = isCategory("OST_DuctFitting");
        if (pipeFitting || ductFitting)
        {
            var portDomain = pipeFitting ? Domain.DomainPiping : Domain.DomainHvac;
            var portWords = pipeFitting ? "pipe" : "duct";
            var ports = connectors.Where(c => c.Domain == portDomain).ToList();
            var strangers = connectors.Count - ports.Count;
            if (strangers > 0)
                warn("The " + categoryName + " family has " + strangers + " connector(s) that are not " + portWords
                    + " connectors. A " + portWords + " run joins only " + portWords + " ones; the others join nothing it lays.",
                    "REMOVE_FAMILY_CONNECTOR on each one that does not belong.");

            // THE COUNT ITS PART TYPE CALLS FOR. Only the parts whose count is
            // recorded (docs/43 section 10); the rest are NOT CHECKED, never
            // passed. Matched by enum name, so a release without one never
            // matches it.
            var portsFor = new Dictionary<string, int>
            {
                { "Elbow", 2 }, { "Tee", 3 }, { "Cross", 4 }, { "Transition", 2 }, { "Union", 2 },
                { "PipeMechanicalCoupling", 2 }, { "Cap", 1 }, { "Wye", 3 }, { "LateralTee", 3 }, { "LateralCross", 4 },
            };
            if (partType != null && portsFor.ContainsKey(partType))
            {
                var wanted = portsFor[partType];
                if (ports.Count == wanted)
                    pass("Part Type " + partWords + " with " + ports.Count + " " + portWords + " connector(s), as that part has.");
                else
                    fail("Part Type is " + partWords + " but the family has " + ports.Count + " " + portWords
                        + " connector(s) - that part has " + wanted + ". Revit offers and places it as the part its Part "
                        + "Type names, so a run reaching it finds " + (ports.Count > wanted ? "a port too many" : "a port missing") + ".",
                        ports.Count > wanted
                            ? "REMOVE_FAMILY_CONNECTOR on the extra one - or SET_FAMILY_SETTINGS \"Part Type=...\" if the Part Type is what is wrong."
                            : "ADD_FAMILY_CONNECTOR on the end face without one - or SET_FAMILY_SETTINGS \"Part Type=...\" if the Part Type is what is wrong.");
            }
            else if (partType != null && partType != "Normal" && partType != "Undefined")
                notRead("How many connectors a " + partWords + " has: Heron has no recorded rule for that Part Type "
                    + "(docs/43 section 10) - compare with Revit's own " + partWords + " through REPORT_FAMILY_CONNECTORS.");

            // EACH RUN CONNECTOR FITTING, as Revit's own fittings' are.
            foreach (var c in ports)
            {
                string system = null;
                try { system = c.SystemClassification.ToString(); }
                catch (Exception) { }
                if (system == null) notRead("The System Classification of " + label(c) + " could not be read.");
                else if (system == "Fitting") pass(label(c) + " is Fitting.");
                else warn(label(c) + " is " + spaced(system) + ", not Fitting. Revit's own fittings' connectors are Fitting "
                    + "(docs/43 section 10), the system that shows a connector's Angle.",
                    "SET_FAMILY_CONNECTOR_ROLES settings \"system=Fitting\".");
            }

            // ONE PRIMARY, LYING ON THE X AXIS.
            if (ports.Count > 0)
            {
                var primaries = new List<ConnectorElement>();
                foreach (var c in ports)
                {
                    try { if (c.IsPrimary) primaries.Add(c); }
                    catch (Exception) { }
                }
                if (primaries.Count != 1)
                    warn("The " + portWords + " connectors read " + primaries.Count + " primary - a fitting has one.",
                        "SET_FAMILY_CONNECTOR_ROLES primary=<the plane the X-axis end face lies on>.");
                else
                {
                    var primary = primaries[0];
                    XYZ along = null;
                    try { along = primary.Direction; }
                    catch (Exception) { }
                    if (along == null)
                        notRead("Which way the primary " + label(primary) + " points: its direction could not be read.");
                    else if (Math.Abs(along.X) > 0.9999)
                        pass(label(primary) + " is primary and lies on the X axis.");
                    else
                        warn(label(primary) + " is primary but points (" + Math.Round(along.X, 3).ToString(invariant) + ", "
                            + Math.Round(along.Y, 3).ToString(invariant) + ", " + Math.Round(along.Z, 3).ToString(invariant)
                            + ") - Autodesk's help puts a fitting's primary connector on the "
                            + "face lying on the X axis.",
                            "SET_FAMILY_CONNECTOR_ROLES primary=<the plane the X-axis end face lies on>.");
                }
            }

            // AN ELBOW'S CONNECTOR ANGLE, driven by an INSTANCE parameter.
            if (partType == "Elbow")
                foreach (var c in ports)
                {
                    Parameter own = null;
                    FamilyParameter driver = null;
                    try
                    {
                        own = c.get_Parameter(BuiltInParameter.CONNECTOR_ANGLE);
                        driver = own == null ? null : fm.GetAssociatedFamilyParameter(own);
                    }
                    catch (Exception) { }
                    if (own == null)
                        notRead("The Angle of " + label(c) + ": it has none to read - a connector shows its Angle only when its system is Fitting.");
                    else if (driver == null)
                        warn(label(c) + "'s Angle is driven by no family parameter. An elbow whose connector Angle does not "
                            + "follow the family's Angle is reported not to flex in a run (docs/43 section 10; NEEDS-CHECKING "
                            + "Group JN reads Revit's own elbow).",
                            "SET_FAMILY_CONNECTOR_ROLES settings \"angle=[Angle]\" - with Angle already an INSTANCE parameter, "
                            + "or the tool makes a type one.");
                    else if (!driver.IsInstance)
                        fail(label(c) + "'s Angle is driven by \"" + driver.Definition.Name + "\", a TYPE parameter - every elbow of "
                            + "a type keeps one angle, and the run it is placed in cannot turn it.",
                            "switch it to Instance with every parameter whose formula reads it: ADD_FAMILY_PARAMETERS "
                            + "switchExisting=true, instance=true, parameterNames=\"" + string.Join(", ",
                                new[] { driver.Definition.Name }.Concat(dependents(driver.Definition.Name))) + "\".");
                    else pass(label(c) + "'s Angle is driven by the instance parameter \"" + driver.Definition.Name + "\".");
                }
        }

    }

    // =======================================================================
    // 3b. A FORM DRAWN TWICE AT COARSE OR MEDIUM
    // =======================================================================
    //
    // A nested family that already draws Coarse or Medium - a simple
    // symbol - while a solid form of this family, in the same place, is
    // shown at that level too: both print, one on top of the other. The
    // nested family's drawing at each level is read from its geometry at
    // that detail level. Only forms inside the nested family's box count,
    // so a body that is merely beside a nested handwheel is not flagged.
    foreach (var nested in nestedInstances)
    {
        var levels = new List<string>();
        foreach (var level in new[] { ViewDetailLevel.Coarse, ViewDetailLevel.Medium })
        {
            var draws = false;
            try
            {
                var options = new Options { DetailLevel = level };
                var geometry = nested.get_Geometry(options);
                if (geometry != null)
                    foreach (var g in geometry)
                    {
                        var instance = g as GeometryInstance;
                        var parts = instance == null ? new List<GeometryObject> { g } : instance.GetInstanceGeometry().Cast<GeometryObject>().ToList();
                        if (parts.Any(part => (part is Solid && ((Solid)part).Volume > 0) || part is Curve)) { draws = true; break; }
                    }
            }
            catch (Exception) { }
            if (draws) levels.Add(level.ToString());
        }
        if (levels.Count == 0) continue;
        var box = nested.get_BoundingBox(null);
        if (box == null) continue;
        var nestedName = "";
        try { nestedName = nested.Symbol.Family.Name; }
        catch (Exception) { nestedName = nested.Name; }
        foreach (var form in solidForms)
        {
            FamilyElementVisibility seen = null;
            try { seen = form.GetVisibility(); }
            catch (Exception) { }
            if (seen == null) continue;
            var both = levels.Where(l => (l == "Coarse" && seen.IsShownInCoarse) || (l == "Medium" && seen.IsShownInMedium)).ToList();
            if (both.Count == 0) continue;
            var formBox = form.get_BoundingBox(null);
            if (formBox == null) continue;
            var inside = formBox.Min.X >= box.Min.X - halfMillimetre && formBox.Min.Y >= box.Min.Y - halfMillimetre
                && formBox.Min.Z >= box.Min.Z - halfMillimetre && formBox.Max.X <= box.Max.X + halfMillimetre
                && formBox.Max.Y <= box.Max.Y + halfMillimetre && formBox.Max.Z <= box.Max.Z + halfMillimetre;
            if (!inside) continue;
            warn(spaced(form.GetType().Name) + " " + form.Id + " is shown at " + string.Join(" and ", both)
                + " inside nested \"" + nestedName + "\", which already draws " + string.Join(" and ", levels)
                + " - both print there, one over the other.",
                "SET_FAMILY_FORM_VISIBILITY on " + form.Id + ": Fine only.");
        }
    }

    // =======================================================================
    // 3a. A FAMILY PARAMETER NOTHING USES
    // =======================================================================
    //
    // DELETE_FAMILY_PARAMETERS' list of what uses a parameter, read the same
    // way: a formula, a dimension or array label, or any element parameter
    // linked to it - a form's field, a nested family's parameter, a
    // connector's size. Revit's built-in parameters are never counted, and
    // neither is one the office REQUIRES. A SHARED one is a WARN: shared
    // parameters carry schedule and tag data that nothing in the family reads.
    var labelled = new HashSet<string>(StringComparer.Ordinal);
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        FamilyParameter labelOf = null;
        try
        {
            var dimension = element as Dimension;
            var array = element as BaseArray;
            if (dimension != null) labelOf = dimension.FamilyLabel;
            else if (array != null) labelOf = array.Label;
        }
        catch (Exception) { labelOf = null; }   // a spot dimension takes no label, and says so by throwing
        if (labelOf != null) labelled.Add(labelOf.Definition.Name);
    }
    var unused = new List<FamilyParameter>();
    foreach (var p in parameters)
    {
        var name = p.Definition.Name;
        var builtIn = p.Definition as InternalDefinition;
        if (builtIn != null && builtIn.BuiltInParameter != BuiltInParameter.INVALID) continue;
        if (requiredParameters != null && requiredParameters.Contains(name)) continue;
        if (labelled.Contains(name)) continue;
        if (parameters.Any(other => other.Definition.Name != name && formulaUses(other.Formula, name))) continue;
        var linked = 0;
        try { linked = p.AssociatedParameters.Size; }
        catch (Exception) { linked = 1; }
        if (linked > 0) continue;
        unused.Add(p);
    }
    var deadOnes = unused.Where(p => !p.IsShared).Select(p => "\"" + p.Definition.Name + "\"").ToList();
    var sharedOnes = unused.Where(p => p.IsShared).Select(p => "\"" + p.Definition.Name + "\"").ToList();
    if (deadOnes.Count > 0)
        fail(deadOnes.Count + " family parameter(s) nothing uses - no formula, label, array, form field, nested family "
            + "or connector reads them: " + string.Join(", ", deadOnes) + ". They show in Properties and change nothing.",
            "DELETE_FAMILY_PARAMETERS \"" + string.Join(", ", unused.Where(p => !p.IsShared).Select(p => p.Definition.Name)) + "\".");
    if (sharedOnes.Count > 0)
        warn(sharedOnes.Count + " SHARED parameter(s) nothing in the family uses: " + string.Join(", ", sharedOnes)
            + ". Shared parameters usually carry schedule or tag data, so they may be wanted.",
            "delete only the ones no schedule or tag reads (DELETE_FAMILY_PARAMETERS).");
    if (unused.Count == 0) pass("Every family parameter is used by something.");

    // =======================================================================
    // 4. CLASSIFICATION (version 3) - the OmniClass Number against the
    //    classification file; each type's Assembly Code and Keynote against
    //    this document's tables
    // =======================================================================
    var clsFirstRow = rows.Count;
    clsLoad();
    var clsCodeRow = family == null ? null : family.get_Parameter(clsCode);
    // An annotation family - a tag, a symbol - is not classified, and is not
    // judged on it, as in a project.
    var clsAnnotationFamily = false;
    try { clsAnnotationFamily = category != null && category.CategoryType == CategoryType.Annotation; }
    catch (Exception) { }
    if (clsCodeRow == null || clsCodeRow.StorageType != StorageType.String)
    {
        if (!clsAnnotationFamily) notRead(clsCodeLabel + ": this family holds none to read.");
    }
    else
    {
        var clsCurrent = clsTidy(clsCodeRow.AsString());
        var clsShown = clsCurrent.Length == 0 ? "(blank)" : clsCurrent;
        var clsMine = category == null ? new List<Tuple<string, string, string, object>>()
            : clsRows.Where(r => clsFiledUnder(r, category.Id)).ToList();
        var clsFix = "SET_FAMILY_SETTINGS \"" + clsCodeLabel + "=<code>\" with one of the classification file's "
            + clsMine.Count + " code(s) for " + categoryName + " - REPORT_FAMILY_TEMPLATE lists them in classificationCodes.";
        if (clsPath == null)
            notRead(clsCodeLabel + " " + clsShown + ": no classification file was found to check it against - looked for "
                + string.Join(", ", clsTried) + ".");
        else if (clsProblem != null)
            notRead(clsCodeLabel + " " + clsShown + ": " + clsProblem + ".");
        else if (clsSample)
            notRead(clsCodeLabel + " " + clsShown + ": the classification file at " + clsPath + " is the SAMPLE Revit 2026 "
                + "and later ship, which files no code under any category - the office's own file goes at that path.");
        else if (clsMine.Count == 0)
            notRead(clsCodeLabel + " " + clsShown + ": the classification file " + clsPath + " gives " + categoryName
                + " no code, so it is not judged here.");
        else if (clsCurrent.Length == 0)
            warn(clsCodeLabel + " is blank. The classification file gives " + categoryName + " " + clsMine.Count
                + " code(s); a schedule, an export or a library sorted by it will not place this family.", clsFix);
        else
        {
            var clsSame = clsRows.Where(r => string.Equals(r.Item1, clsCurrent, StringComparison.OrdinalIgnoreCase)).ToList();
            var clsHere = clsSame.FirstOrDefault(r => clsFiledUnder(r, category.Id));
            if (clsSame.Count == 0)
                fail(clsCodeLabel + " " + clsCurrent + " is not a code in the classification file " + clsPath + ".", clsFix);
            else if (clsHere != null)
                pass(clsCodeLabel + " " + clsHere.Item1 + " " + clsHere.Item2 + " is one of the classification file's codes "
                    + "for " + categoryName + ".");
            else
                fail(clsCodeLabel + " " + clsSame[0].Item1 + " " + clsSame[0].Item2 + " is filed under "
                    + clsFiledWords(clsSame[0]) + " in the classification file, not " + categoryName
                    + (clsSame[0].Item3.Length == 0 ? " - it fits no family." : "."), clsFix);
        }
    }

    // EACH TYPE'S ASSEMBLY CODE AND KEYNOTE, read through the family parameter
    // whose definition is the built-in one - found by its id, else by looking.
    Func<BuiltInParameter, FamilyParameter> clsFamilyParameter = clsWhich =>
    {
        FamilyParameter clsGot = null;
        try { clsGot = fm.get_Parameter(clsWhich); }
        catch (Exception) { clsGot = null; }
        if (clsGot != null) return clsGot;
        return parameters.FirstOrDefault(clsP =>
        {
            var clsDefinition = clsP.Definition as InternalDefinition;
            return clsDefinition != null && clsDefinition.BuiltInParameter == clsWhich;
        });
    };
    var clsTypes = new List<FamilyType>();
    foreach (FamilyType clsType in fm.Types) clsTypes.Add(clsType);
    Func<FamilyParameter, List<Tuple<string, string>>> clsGiven = clsParameter =>
    {
        var clsFilled = new List<Tuple<string, string>>();
        if (clsParameter == null) return clsFilled;
        foreach (var clsType in clsTypes)
        {
            var clsValue = "";
            try { clsValue = clsTidy(clsType.AsString(clsParameter)); }
            catch (Exception) { clsValue = ""; }
            if (clsValue.Length > 0) clsFilled.Add(Tuple.Create(clsType.Name, clsValue));
        }
        return clsFilled;
    };

    var clsAssemblyGiven = clsGiven(clsFamilyParameter(clsAssembly));
    if (clsAssemblyGiven.Count > 0 && clsAssemblyTable.Count == 0)
        notRead(clsAssemblyLabel + " of " + clsAssemblyGiven.Count + " type(s) ("
            + string.Join(", ", clsAssemblyGiven.Select(g2 => "\"" + g2.Item1 + "\" " + g2.Item2)) + "): this document "
            + "holds no Assembly Code table to check it against - a family document often holds none; the project it is "
            + "loaded into checks it.");
    else if (clsAssemblyGiven.Count > 0)
    {
        var clsGood = 0;
        foreach (var clsPair in clsAssemblyGiven)
        {
            var clsTypeFix = "SET_FAMILY_TYPE_VALUES typeName \"" + clsPair.Item1 + "\", values \"" + clsAssemblyLabel
                + "=<a code from the Assembly Code table>\".";
            ClassificationEntry clsFiledEntry;
            if (!clsAssemblyTable.TryGetValue(clsPair.Item2, out clsFiledEntry))
            {
                fail("Type \"" + clsPair.Item1 + "\": " + clsAssemblyLabel + " " + clsPair.Item2 + " is not in this "
                    + "document's Assembly Code table.", clsTypeFix);
                continue;
            }
            var clsElsewhere = clsAssemblyOther(clsFiledEntry, category == null ? null : category.Id);
            if (clsElsewhere != null)
                fail("Type \"" + clsPair.Item1 + "\": " + clsAssemblyLabel + " " + clsPair.Item2 + " ("
                    + clsFiledEntry.Description + ") is an entry for " + clsElsewhere + " in the Assembly Code table, not "
                    + categoryName + ".", clsTypeFix);
            else clsGood++;
        }
        if (clsGood > 0) pass(clsAssemblyLabel + " of " + clsGood + " type(s) is in the Assembly Code table.");
    }

    var clsKeynoteGiven = clsGiven(clsFamilyParameter(BuiltInParameter.KEYNOTE_PARAM));
    if (clsKeynoteGiven.Count > 0 && clsKeynoteKeys.Count == 0)
        notRead(clsKeynoteLabel + " of " + clsKeynoteGiven.Count + " type(s): this document has no keynote table loaded "
            + "to check it against.");
    else if (clsKeynoteGiven.Count > 0)
    {
        var clsKnown = 0;
        foreach (var clsPair in clsKeynoteGiven)
        {
            if (clsKeynoteKeys.Contains(clsPair.Item2)) { clsKnown++; continue; }
            warn("Type \"" + clsPair.Item1 + "\": " + clsKeynoteLabel + " " + clsPair.Item2 + " is not in this document's "
                + "keynote table, so a keynote tag shows it without its text.",
                "SET_FAMILY_TYPE_VALUES typeName \"" + clsPair.Item1 + "\", values \"" + clsKeynoteLabel
                + "=<a key from the keynote file>\".");
        }
        if (clsKnown > 0) pass(clsKeynoteLabel + " of " + clsKnown + " type(s) is in the keynote table.");
    }

    classificationReport = (rows.Count > clsFirstRow ? string.Join(" | ", rows.Skip(clsFirstRow)) : "Nothing to classify.")
        + " Classification file: " + (clsPath ?? "none found - looked for " + string.Join(", ", clsTried)) + ".";

    familyReport = "\"" + familyName + "\" (" + categoryName
        + (partType == null ? "" : ", Part Type " + partWords)
        + (workPlaneBased.HasValue ? ", Work Plane-Based " + (workPlaneBased.Value == 1 ? "Yes" : "No") : "")
        + ", " + connectors.Count + " connector(s)): " + offStandard + " FAIL, " + warned + " WARN, " + passed + " PASS, "
        + notChecked + " NOT CHECKED. " + string.Join(" | ", rows);
    findings.Add(familyReport);
    if (!mep)
        findings.Add("A " + categoryName + " family is not one this checks for joining a run - only its name, its "
            + "parameters" + (mustHaveConnectors ? " and that it has connectors" : "") + " were checked.");
    findings.Add("Checked in the family, before it is loaded. A copy already in a project keeps the old build until "
        + "the family is loaded there again.");
}
else
{
    var systemFamilies = 0;

    // One placed instance per type, gathered once. Doing it per type is the same
    // query run hundreds of times.
    var sampleByType = new Dictionary<ElementId, FamilyInstance>();
    var countByType = new Dictionary<ElementId, int>();

    foreach (var element in new FilteredElementCollector(doc)
        .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType())
    {
        var instance = element as FamilyInstance;
        if (instance == null) continue;
        var typeId = instance.GetTypeId();
        if (typeId == ElementId.InvalidElementId) continue;

        if (!sampleByType.ContainsKey(typeId)) sampleByType[typeId] = instance;
        countByType[typeId] = (countByType.ContainsKey(typeId) ? countByType[typeId] : 0) + 1;
    }

    // ---- CLASSIFICATION (version 3), gathered per type and said once, in
    // `classificationReport`. Each list holds "Family : Type" names.
    clsLoad();
    var clsFileUsable = clsPath != null && clsProblem == null && !clsSample;
    var clsRequired = requiredParameters != null && requiredParameters.Any(clsWanted => clsWanted != null
        && (string.Equals(clsWanted.Trim(), clsCodeLabel, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clsWanted.Trim(), "OmniClass Number", StringComparison.OrdinalIgnoreCase)
            || string.Equals(clsWanted.Trim(), "Classification Number", StringComparison.OrdinalIgnoreCase)));
    var clsBlank = new List<string>();
    var clsNotInFile = new List<string>();
    var clsOtherCategory = new List<string>();
    var clsNoCategory = new List<string>();
    var clsUnchecked = new List<string>();
    var clsUnjudged = 0;
    var clsAssemblyMissing = new List<string>();
    var clsAssemblyElsewhere = new List<string>();
    var clsAssemblyUnchecked = new List<string>();
    var clsKeynoteMissing = new List<string>();
    var clsKeynoteUnchecked = new List<string>();
    // Whether the file gives a category any code - asked once per category.
    var clsHasCodes = new Dictionary<ElementId, bool>();
    var clsFix = " - fix it in the family: OPEN_FAMILY_FOR_EDITING, SET_FAMILY_SETTINGS \"" + clsCodeLabel
        + "=<code>\", then LOAD_FAMILY back";
    // THE PARAMETER AN ELEMENT REALLY CARRIES. get_Parameter can hand back a
    // stand-in for a built-in the element does not hold: it reads empty, and a
    // write through it changes nothing (measured 2026-10-06 on a placed
    // diffuser's Offset from Host and Level). A type that does not carry the
    // family's OmniClass Number could read blank that way, so the element's
    // own Parameters are looked through first.
    Func<Element, BuiltInParameter, Parameter> clsCarried = (clsElement, clsBip) =>
    {
        if (clsElement == null) return null;
        foreach (Parameter clsP in clsElement.Parameters)
        {
            var clsDefinition = clsP.Definition as InternalDefinition;
            if (clsDefinition != null && clsDefinition.BuiltInParameter == clsBip) return clsP;
        }
        return null;
    };

    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)))
    {
        var symbol = element as FamilySymbol;
        if (symbol == null) continue;

        var family = symbol.Family;
        if (family == null) { systemFamilies++; continue; }

        // An in-place family belongs to one model and cannot be a library standard.
        var inPlace = false;
        try { inPlace = family.IsInPlace; }
        catch { }

        var placed = countByType.ContainsKey(symbol.Id) ? countByType[symbol.Id] : 0;
        var faults = new List<string>();
        var cautions = new List<string>();

        // ---- name
        if (!string.IsNullOrEmpty(namePattern)
            && family.Name.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) < 0)
        {
            faults.Add(string.Format("name does not contain '{0}'", namePattern));
        }

        // ---- required parameters, on the TYPE - counted, never looked up (see
        // the header: two by one name is a WARN, not a pass)
        foreach (var wanted in requiredParameters)
        {
            if (string.IsNullOrEmpty(wanted)) continue;
            var carried = symbol.GetParameters(wanted).Count;
            if (carried == 0) faults.Add(string.Format("no '{0}' parameter", wanted));
            else if (carried > 1)
                cautions.Add(string.Format("WARN '{0}' is on this type {1} times - asked for by that "
                    + "name, Revit picks one of them at random; keep one", wanted, carried));
        }

        // ---- connectors, through a placed instance only
        if (mustHaveConnectors)
        {
            if (placed == 0)
            {
                notChecked++;
                findings.Add(string.Format("{0} : {1}  - NOT CHECKED for connectors: no instance is placed, "
                    + "and a type does not expose its connectors. Place one and re-run",
                    family.Name, symbol.Name));
            }
            else
            {
                var sample = sampleByType[symbol.Id];
                var connectorCount = 0;
                try
                {
                    var manager = sample.MEPModel == null ? null : sample.MEPModel.ConnectorManager;
                    if (manager != null) connectorCount = manager.Connectors.Size;
                }
                catch { }

                if (connectorCount == 0)
                {
                    faults.Add("NO CONNECTORS - it cannot join a system, cannot carry flow and will never "
                        + "appear in a system browser, while looking normal in every plan");
                }
            }
        }

        // ---- classification (version 3). A keynote not in the table is a
        // WARN in the row and is NOT counted in `warned`, which counts only
        // the doubled required names.
        var clsNotes = new List<string>();
        var clsName = family.Name + " : " + symbol.Name;
        var clsCategory = family.FamilyCategory ?? symbol.Category;
        var clsAnnotation = false;
        try { clsAnnotation = clsCategory != null && clsCategory.CategoryType == CategoryType.Annotation; }
        catch (Exception) { }
        if (clsCategory != null && !clsHasCodes.ContainsKey(clsCategory.Id))
            clsHasCodes[clsCategory.Id] = clsRows.Any(r => clsFiledUnder(r, clsCategory.Id));
        var clsCategoryHasCodes = clsCategory != null && clsHasCodes[clsCategory.Id];

        // THE OMNICLASS NUMBER, from the type, else its family - the one each
        // CARRIES first, a plain lookup only when neither does. Whether a
        // loaded type exposes it is not known yet (NEEDS-CHECKING), so a
        // read that comes back empty-handed is NOT CHECKED - never blank -
        // and so is an empty value only the plain lookup gave, which may be a
        // stand-in.
        Parameter clsRead = null;
        var clsOnlyLookedUp = false;
        try
        {
            clsRead = clsCarried(symbol, clsCode) ?? clsCarried(family, clsCode);
            if (clsRead == null)
            {
                clsRead = symbol.get_Parameter(clsCode) ?? family.get_Parameter(clsCode);
                clsOnlyLookedUp = clsRead != null;
            }
        }
        catch (Exception) { clsRead = null; }
        if (clsAnnotation) clsUnjudged++;
        else if (clsRead == null || clsRead.StorageType != StorageType.String)
            clsUnchecked.Add(clsName + " - no value to read");
        else
        {
            var clsValue = clsTidy(clsRead.AsString());
            if (clsValue.Length == 0 && clsOnlyLookedUp)
                clsUnchecked.Add(clsName + " - reads blank, but neither the type nor its family carries the "
                    + clsCodeLabel + " in its own parameters, so the blank may not be the family's");
            else if (clsValue.Length == 0)
            {
                if (clsFileUsable && !clsCategoryHasCodes) clsUnjudged++;
                else clsBlank.Add(clsName);
                if (clsRequired) faults.Add(clsCodeLabel + " is blank, and requiredParameters names it" + clsFix);
            }
            else if (!clsFileUsable) clsUnchecked.Add(clsName + " - no classification file to check " + clsValue + " against");
            else if (!clsCategoryHasCodes) clsUnjudged++;
            else
            {
                var clsSame = clsRows.Where(r => string.Equals(r.Item1, clsValue, StringComparison.OrdinalIgnoreCase)).ToList();
                if (clsSame.Count == 0)
                {
                    clsNotInFile.Add(clsName + " " + clsValue);
                    faults.Add(clsCodeLabel + " " + clsValue + " is not in the classification file" + clsFix);
                }
                else if (!clsSame.Any(r => clsFiledUnder(r, clsCategory.Id)))
                {
                    var clsWhere = clsFiledWords(clsSame[0]);
                    if (clsSame[0].Item3.Length == 0) clsNoCategory.Add(clsName + " " + clsValue);
                    else clsOtherCategory.Add(clsName + " " + clsValue + " (" + clsWhere + ")");
                    faults.Add(clsCodeLabel + " " + clsValue + " " + clsSame[0].Item2 + " is filed under " + clsWhere
                        + " in the classification file, not " + clsCategory.Name + clsFix);
                }
            }
        }

        // THE ASSEMBLY CODE, against the project's Assembly Code table.
        var clsAssemblyValue = "";
        try
        {
            var clsAssemblyRow = symbol.get_Parameter(clsAssembly);
            clsAssemblyValue = clsAssemblyRow == null ? "" : clsTidy(clsAssemblyRow.AsString());
        }
        catch (Exception) { clsAssemblyValue = ""; }
        if (clsAssemblyValue.Length > 0)
        {
            ClassificationEntry clsFiledEntry;
            if (clsAssemblyTable.Count == 0) clsAssemblyUnchecked.Add(clsName);
            else if (!clsAssemblyTable.TryGetValue(clsAssemblyValue, out clsFiledEntry))
            {
                clsAssemblyMissing.Add(clsName + " " + clsAssemblyValue);
                faults.Add(clsAssemblyLabel + " " + clsAssemblyValue + " is not in the Assembly Code table - "
                    + "SET_FAMILY_TYPE_VALUES \"" + clsAssemblyLabel + "=<code>\" in the family");
            }
            else
            {
                var clsElsewhere = clsAssemblyOther(clsFiledEntry, clsCategory == null ? null : clsCategory.Id);
                if (clsElsewhere != null)
                {
                    clsAssemblyElsewhere.Add(clsName + " " + clsAssemblyValue + " (" + clsElsewhere + ")");
                    faults.Add(clsAssemblyLabel + " " + clsAssemblyValue + " is an entry for " + clsElsewhere
                        + " in the Assembly Code table - SET_FAMILY_TYPE_VALUES \"" + clsAssemblyLabel + "=<code>\" in "
                        + "the family");
                }
            }
        }

        // THE KEYNOTE, against the project's keynote table - a warning only.
        var clsKeynoteValue = "";
        try
        {
            var clsKeynoteRow = symbol.get_Parameter(BuiltInParameter.KEYNOTE_PARAM);
            clsKeynoteValue = clsKeynoteRow == null ? "" : clsTidy(clsKeynoteRow.AsString());
        }
        catch (Exception) { clsKeynoteValue = ""; }
        if (clsKeynoteValue.Length > 0)
        {
            if (clsKeynoteKeys.Count == 0) clsKeynoteUnchecked.Add(clsName);
            else if (!clsKeynoteKeys.Contains(clsKeynoteValue))
            {
                clsKeynoteMissing.Add(clsName + " " + clsKeynoteValue);
                clsNotes.Add("WARN " + clsKeynoteLabel + " " + clsKeynoteValue + " is not in the keynote table");
            }
        }

        if (faults.Count > 0) offStandard++;
        if (cautions.Count > 0) warned++;
        if (faults.Count > 0 || cautions.Count > 0 || clsNotes.Count > 0)
        {
            findings.Add(string.Format("{0} : {1}  ({2} placed{3})  - {4}",
                family.Name, symbol.Name, placed, inPlace ? ", IN-PLACE" : "",
                string.Join("; ", faults.Concat(cautions).Concat(clsNotes))));
        }
    }

    classificationReport = clsCodeLabel + ": " + clsBlank.Count + " blank" + clsFirstTen(clsBlank)
        + (clsRequired ? " - requiredParameters names it, so each is off standard"
            : " - counted, not off standard: no code is required unless requiredParameters names it")
        + "; " + clsNotInFile.Count + " not in the classification file" + clsFirstTen(clsNotInFile)
        + "; " + clsOtherCategory.Count + " filed under another category" + clsFirstTen(clsOtherCategory)
        + "; " + clsNoCategory.Count + " filed under no category" + clsFirstTen(clsNoCategory)
        + "; " + clsUnchecked.Count + " not checked" + clsFirstTen(clsUnchecked)
        + "; " + clsUnjudged + " not judged - an annotation, or a category the classification file gives no code. "
        + clsAssemblyLabel + ": " + clsAssemblyMissing.Count + " not in the Assembly Code table" + clsFirstTen(clsAssemblyMissing)
        + "; " + clsAssemblyElsewhere.Count + " an entry for another category" + clsFirstTen(clsAssemblyElsewhere)
        + "; " + clsAssemblyUnchecked.Count + " not checked" + clsFirstTen(clsAssemblyUnchecked)
        + (clsAssemblyTable.Count == 0 ? " - this project holds no Assembly Code table" : "") + ". "
        + clsKeynoteLabel + ": " + clsKeynoteMissing.Count + " not in the keynote table, a warning only"
        + clsFirstTen(clsKeynoteMissing)
        + "; " + clsKeynoteUnchecked.Count + " not checked" + clsFirstTen(clsKeynoteUnchecked)
        + (clsKeynoteKeys.Count == 0 ? " - no keynote file is loaded" : "") + ". "
        + "Classification file: " + (clsPath ?? "none found - looked for " + string.Join(", ", clsTried))
        + (clsSample ? " - the SAMPLE Revit 2026 and later ship, which files no code under any category" : "")
        + (clsProblem != null ? " - " + clsProblem : "") + ".";

    findings.Insert(0, string.Format("{0} family type(s) off standard, {1} not checked for connectors"
        + "{3}. {2} system family type(s) excluded - a duct type or a wall type has no family file and "
        + "cannot be audited this way", offStandard, notChecked, systemFamilies,
        warned > 0
            ? string.Format(", {0} WARNED for carrying a required parameter's name more than once", warned)
            : ""));
    findings.Add(classificationReport);
}
