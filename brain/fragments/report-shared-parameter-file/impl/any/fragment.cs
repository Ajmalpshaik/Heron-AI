// NOT STANDALONE. Assumes `doc`, `nameContains`, `groupContains` and
// `compareWithDocument` are in scope; leaves `parameterCount`,
// `parameterList`, `matchedByGuid`, `guidMismatches`, `notShared`,
// `comparison`, `sharedFile`, `fileState`, `fileExists`, `fileReadOnly`,
// `opensInRevit`, `groups`, `inFile`, `comparedWith` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none. The setting Revit holds for
// the shared parameter file (Manage > Shared Parameters) is READ and never
// set; the file is opened by Revit only when it is already on disk, so a path
// to nothing is never created; nothing in the model is touched.
//
// WHY: three tools touch that setting and all three write - they set it, or
// write the file. A modeller asking "which shared parameter file is Revit
// using" or "is this name in it, with which GUID" had nothing to answer them,
// and Revit.ini holds the SAVED setting, not the one Revit is using.
//
// THE DATA TYPE IS THE ONE THING THAT MOVED ACROSS THE RANGE: ParameterType
// up to 2022, GetDataType() - a ForgeTypeId - from 2022. One #if picks, in
// `dataTypeOf` below; nothing else here differs between releases.
//
// THE CLASSIC MISTAKE IS A NAME WITH ANOTHER GUID. A tag or a schedule finds
// a shared parameter by its GUID, never by its name. A family parameter that
// carries the file's name with a different GUID - made from another file, or
// typed again by hand - or that is not shared at all, looks right in Family
// Types and is invisible to every tag built on the office's parameter. So
// compareWithDocument matches by GUID first and names each name-only match.
//
// EVERY LIST COMES BACK AS ONE STRING. A list is abbreviated to three entries
// before a reader sees it, and a file of two hundred parameters would then
// read as three.

var findings = new List<string>();
var parameterCount = 0;
var parameterList = "";
var matchedByGuid = 0;
var guidMismatches = 0;
var notShared = 0;
var comparison = "";
var sharedFile = "";
var fileState = "";
var fileExists = false;
var fileReadOnly = false;
var opensInRevit = false;
var groups = "";
var inFile = 0;
var comparedWith = "";

// "all", or nothing, is no narrowing. Several parts with commas or semicolons
// between: a name holding any one of them, ignoring case.
Func<string, List<string>> partsOf = text =>
{
    var said = (text ?? "").Trim();
    if (said.Length == 0 || string.Equals(said, "all", StringComparison.OrdinalIgnoreCase))
        return new List<string>();
    return said.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(p => p.Trim())
        .Where(p => p.Length > 0 && !string.Equals(p, "all", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
};
var nameParts = partsOf(nameContains);
var groupParts = partsOf(groupContains);

Func<string, List<string>, bool> holdsAny = (value, parts) =>
    parts.Count == 0 || parts.Any(p => (value ?? "").IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// WHAT A DEFINITION HOLDS - the label the Shared Parameters dialog shows, then
// Revit's own name for it in brackets, so two that read alike can be told
// apart.
Func<Definition, string> dataTypeOf = definition =>
{
    try
    {
#if REVIT2020 || REVIT2021
        var type = definition.ParameterType;
        string label = null;
        try { label = LabelUtils.GetLabelFor(type); }
        catch (Exception) { label = null; }
        return (string.IsNullOrEmpty(label) ? type.ToString() : label) + " [" + type + "]";
#else
        var spec = definition.GetDataType();
        if (spec == null || string.IsNullOrEmpty(spec.TypeId)) return "no data type [empty]";
        string label = null;
        try
        {
            if (SpecUtils.IsSpec(spec))
                label = LabelUtils.GetLabelForSpec(spec);
            else if (Category.IsBuiltInCategory(spec))
                label = "Family Type: " + LabelUtils.GetLabelFor(Category.GetBuiltInCategory(spec));
        }
        catch (Exception) { label = null; }
        return (string.IsNullOrEmpty(label) ? spec.TypeId : label) + " [" + spec.TypeId + "]";
#endif
    }
    catch (Exception)
    {
        return "a data type Revit did not name";
    }
};

// ---------------------------------------------------------------------------
// THE SETTING - what Revit is using now
// ---------------------------------------------------------------------------
try { sharedFile = doc.Application.SharedParametersFilename ?? ""; }
catch (Exception ex)
{
    sharedFile = "";
    findings.Add("Revit did not say which shared parameter file it is set to (" + ex.Message + ").");
}

DefinitionFile file = null;
if (sharedFile.Trim().Length == 0)
{
    fileState = "none set";
    findings.Add("Revit has no shared parameter file set - Manage > Shared Parameters is empty. There is nothing "
        + "to list. Browse to the office's file there first.");
}
else
{
    try { fileExists = System.IO.File.Exists(sharedFile); }
    catch (Exception) { fileExists = false; }

    if (!fileExists)
    {
        fileState = "set, but the file is not there";
        findings.Add("Revit is set to the shared parameter file \"" + sharedFile + "\", and that file is not there - "
            + "moved, renamed, or on a drive this PC cannot reach. Nothing was opened, so nothing was created.");
    }
    else
    {
        try { fileReadOnly = new System.IO.FileInfo(sharedFile).IsReadOnly; }
        catch (Exception) { fileReadOnly = false; }

        string whyNot = null;
        try { file = doc.Application.OpenSharedParameterFile(); }
        catch (Exception ex) { file = null; whyNot = ex.Message; }
        opensInRevit = file != null;

        if (file == null)
        {
            fileState = "on disk, " + (fileReadOnly ? "marked read-only" : "not marked read-only")
                + ", and Revit could not open it";
            findings.Add("Revit is set to \"" + sharedFile + "\" and the file is there, but Revit could not open it as "
                + "a shared parameter file" + (whyNot == null ? "" : " (" + whyNot + ")") + ". Open it in Notepad: it "
                + "should start with a *META line, then *GROUP and *PARAM lines.");
        }
    }
}

// ---------------------------------------------------------------------------
// THE FILE - every group, every parameter
// ---------------------------------------------------------------------------
// (group name, definition), in the file's order.
var everything = new List<Tuple<string, ExternalDefinition>>();
if (file != null)
{
    var groupLines = new List<string>();
    var groupCount = 0;
    foreach (DefinitionGroup group in file.Groups)
    {
        groupCount++;
        var held = 0;
        foreach (Definition definition in group.Definitions)
        {
            var external = definition as ExternalDefinition;
            if (external == null) continue;
            everything.Add(Tuple.Create(group.Name, external));
            held++;
        }
        groupLines.Add(group.Name + " (" + held + ")");
    }
    groups = string.Join("  ||  ", groupLines);
    inFile = everything.Count;
    fileState = "on disk, " + (fileReadOnly ? "marked read-only" : "not marked read-only") + ", opens in Revit - "
        + groupCount + " group(s), " + inFile + " parameter(s)";
    findings.Add("Revit is set to the shared parameter file \"" + sharedFile + "\" now - the path Manage > Shared "
        + "Parameters shows. " + groupCount + " group(s), " + inFile + " parameter(s); "
        + (fileReadOnly
            ? "the file is marked read-only, so New Parameter in that dialog cannot write to it."
            : "the file is not marked read-only (whether this Windows user may write to it is not tested - that "
                + "would mean writing)."));
}

var listed = everything
    .Where(e => holdsAny(e.Item2.Name, nameParts) && holdsAny(e.Item1, groupParts))
    .OrderBy(e => e.Item1, StringComparer.OrdinalIgnoreCase)
    .ThenBy(e => e.Item2.Name, StringComparer.OrdinalIgnoreCase)
    .ToList();
parameterCount = listed.Count;

Func<Tuple<string, ExternalDefinition>, string> lineOf = entry =>
{
    var definition = entry.Item2;
    var visible = true;
    var editable = true;
    var hideEmpty = false;
    var description = "";
    try { visible = definition.Visible; } catch (Exception) { }
    try { editable = definition.UserModifiable; } catch (Exception) { }
    try { hideEmpty = definition.HideWhenNoValue; } catch (Exception) { }
    try { description = (definition.Description ?? "").Trim(); } catch (Exception) { }
    return definition.Name
        + " | GUID " + definition.GUID
        + " | " + dataTypeOf(definition)
        + " | group " + entry.Item1
        + " | " + (visible ? "visible" : "HIDDEN (Visible off)")
        + (editable ? "" : " | not user modifiable")
        + (hideEmpty ? " | hidden when it has no value" : "")
        + " | " + (description.Length > 0 ? "description: " + description : "no description");
};
parameterList = string.Join("  ||  ", listed.Select(lineOf));

if (file != null)
{
    if (nameParts.Count > 0 || groupParts.Count > 0)
        findings.Add(parameterCount + " of " + inFile + " parameter(s) listed"
            + (nameParts.Count > 0 ? ", names holding " + string.Join(" or ", nameParts.Select(p => "\"" + p + "\"")) : "")
            + (groupParts.Count > 0 ? ", in groups holding " + string.Join(" or ", groupParts.Select(p => "\"" + p + "\"")) : "")
            + ".");

    // EACH NAME ASKED FOR, ANSWERED BY ITSELF - "is it in the file, and with
    // which GUID" is the question, and a list of near names does not answer it.
    foreach (var part in nameParts)
    {
        var exact = everything.Where(e => e.Item2.Name == part).ToList();
        if (exact.Count == 1)
        {
            findings.Add("\"" + part + "\" is in the file, under " + exact[0].Item1 + ", GUID " + exact[0].Item2.GUID
                + ", holding " + dataTypeOf(exact[0].Item2) + ".");
            continue;
        }
        if (exact.Count > 1)
        {
            findings.Add("\"" + part + "\" is in the file " + exact.Count + " times: "
                + string.Join("; ", exact.Select(e => "under " + e.Item1 + ", GUID " + e.Item2.GUID)) + ". A family "
                + "made from one is a different parameter from the other - which one the office means cannot be told "
                + "from the file.");
            continue;
        }
        var otherCase = everything.Where(e => string.Equals(e.Item2.Name, part, StringComparison.OrdinalIgnoreCase)).ToList();
        if (otherCase.Count > 0)
        {
            findings.Add("\"" + part + "\" is not in the file with those capitals; "
                + string.Join("; ", otherCase.Select(e => "\"" + e.Item2.Name + "\" is, under " + e.Item1 + ", GUID "
                    + e.Item2.GUID)) + ".");
            continue;
        }
        var holding = everything.Where(e => e.Item2.Name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(e => e.Item2.Name).Distinct().ToList();
        if (holding.Count > 0)
        {
            findings.Add("No parameter in the file is named exactly \"" + part + "\"; " + holding.Count + " name(s) "
                + "hold it: " + string.Join(", ", holding.Take(10)) + (holding.Count > 10 ? ", ..." : "") + ".");
            continue;
        }
        var close = everything.Where(e => squash(part).Length > 3
                && (squash(e.Item2.Name).Contains(squash(part)) || squash(part).Contains(squash(e.Item2.Name))
                    && squash(e.Item2.Name).Length > 3))
            .Select(e => e.Item2.Name + " (" + e.Item1 + ")").Distinct().Take(10).ToList();
        findings.Add("\"" + part + "\" is not in the shared parameter file \"" + sharedFile + "\" - no name holds it."
            + (close.Count > 0 ? " Close: " + string.Join(", ", close) + "." : ""));
    }

    // ONE NAME, TWO GUIDs, ANYWHERE IN WHAT WAS LISTED - two parameters to
    // every tag and schedule, under one name.
    var twice = listed.GroupBy(e => e.Item2.Name)
        .Where(g => g.Select(e => e.Item2.GUID).Distinct().Count() > 1)
        .Select(g => "\"" + g.Key + "\" (" + string.Join(", ", g.Select(e => e.Item1 + " " + e.Item2.GUID)) + ")")
        .ToList();
    if (twice.Count > 0)
        findings.Add(twice.Count + " name(s) are in the file more than once with different GUIDs: "
            + string.Join("; ", twice) + ".");
}

// ---------------------------------------------------------------------------
// THE DOCUMENT IN FRONT - by GUID first, then by name
// ---------------------------------------------------------------------------
if (compareWithDocument && file == null)
    findings.Add("Not compared with the document: there is no shared parameter file open to compare it with.");
else if (compareWithDocument)
{
    // (name, GUID or null when not shared, what it is)
    var carried = new List<Tuple<string, Guid?, string>>();
    if (doc.IsFamilyDocument)
    {
        comparedWith = doc.Title + " (family)";
        var manager = doc.FamilyManager;
        if (manager != null)
            foreach (FamilyParameter parameter in manager.GetParameters())
            {
                if (parameter == null) continue;
                var name = "";
                try { name = parameter.Definition == null ? "" : parameter.Definition.Name; } catch (Exception) { }
                Guid? guid = null;
                try { if (parameter.IsShared) guid = parameter.GUID; } catch (Exception) { guid = null; }
                var scope = "";
                try { scope = parameter.IsInstance ? "instance" : "type"; } catch (Exception) { }
                carried.Add(Tuple.Create(name, guid, (guid.HasValue ? "a shared " : "a family ") + scope + " parameter"));
            }
    }
    else
    {
        comparedWith = doc.Title + " (project)";
        // OfClass(ParameterElement) and OfClass(SharedParameterElement) both,
        // joined by id: whether the first also hands back the second's
        // elements is not asked of it.
        var seen = new HashSet<string>();
        var elements = new List<ParameterElement>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(SharedParameterElement)).ToElements()
            .Concat(new FilteredElementCollector(doc).OfClass(typeof(ParameterElement)).ToElements()))
        {
            var parameterElement = element as ParameterElement;
            if (parameterElement == null || parameterElement is GlobalParameter) continue;
            if (!seen.Add(parameterElement.UniqueId)) continue;
            elements.Add(parameterElement);
        }
        foreach (var parameterElement in elements)
        {
            var name = "";
            try
            {
                var definition = parameterElement.GetDefinition();
                name = definition == null ? parameterElement.Name : definition.Name;
            }
            catch (Exception) { name = parameterElement.Name; }
            var shared = parameterElement as SharedParameterElement;
            Guid? guid = null;
            try { if (shared != null) guid = shared.GuidValue; } catch (Exception) { guid = null; }
            carried.Add(Tuple.Create(name, guid, guid.HasValue ? "a shared parameter in the project"
                : "a project parameter, not shared"));
        }
    }

    var lines = new List<string>();
    var narrowed = nameParts.Count > 0 || groupParts.Count > 0;
    var notCarried = 0;
    foreach (var entry in listed)
    {
        var definition = entry.Item2;
        var byGuid = carried.Where(c => c.Item2.HasValue && c.Item2.Value == definition.GUID).ToList();
        var byName = carried.Where(c => string.Equals(c.Item1, definition.Name, StringComparison.OrdinalIgnoreCase)
            && !(c.Item2.HasValue && c.Item2.Value == definition.GUID)).ToList();

        if (byGuid.Count > 0)
        {
            matchedByGuid++;
            var renamed = byGuid.Where(c => c.Item1 != definition.Name).Select(c => c.Item1).Distinct().ToList();
            lines.Add(definition.Name + ": carried by its GUID " + definition.GUID + " - " + byGuid[0].Item3
                + (renamed.Count > 0 ? ", named \"" + string.Join("\", \"", renamed) + "\" there" : ""));
        }
        foreach (var other in byName)
        {
            if (other.Item2.HasValue)
            {
                guidMismatches++;
                lines.Add(definition.Name + ": the document's \"" + other.Item1 + "\" is " + other.Item3 + " with GUID "
                    + other.Item2.Value + " - the file's is " + definition.GUID + ". A tag or schedule built on the "
                    + "file's parameter will not read it");
            }
            else
            {
                notShared++;
                lines.Add(definition.Name + ": the document's \"" + other.Item1 + "\" is " + other.Item3 + " - not "
                    + "shared, so a project cannot schedule or tag it as the file's parameter");
            }
        }
        if (byGuid.Count == 0 && byName.Count == 0)
        {
            notCarried++;
            if (narrowed) lines.Add(definition.Name + ": not in the document");
        }
    }

    // Shared parameters the document carries that this file has not got at
    // all - from another file. Named only when the name asked for holds them.
    var fileGuids = new HashSet<Guid>(everything.Select(e => e.Item2.GUID));
    var foreign = carried.Where(c => c.Item2.HasValue && !fileGuids.Contains(c.Item2.Value)
            && holdsAny(c.Item1, nameParts))
        .ToList();
    foreach (var other in foreign.Where(o => !listed.Any(e => string.Equals(e.Item2.Name, o.Item1,
        StringComparison.OrdinalIgnoreCase))))
        lines.Add(other.Item1 + ": in the document as " + other.Item3 + " with GUID " + other.Item2.Value
            + " - that GUID is not in this file");

    comparison = string.Join("  ||  ", lines);
    findings.Add("Compared with " + comparedWith + ": " + matchedByGuid + " of " + parameterCount + " listed "
        + "parameter(s) carried by their GUID, " + guidMismatches + " carried under the file's name with another GUID, "
        + notShared + " under the file's name but not shared, " + notCarried + " not in it"
        + (foreign.Count > 0 ? "; " + foreign.Count + " shared parameter(s) in it come from another file" : "")
        + ".");
}

findings.Add("Nothing was changed - not the setting, not the file, not the model.");
