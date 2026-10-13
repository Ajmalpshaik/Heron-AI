// NOT STANDALONE. Assumes `doc`, `hostType`, `layerIndex`, `widthMm`,
// `material`, `layerFunction` and `checkOnly` are in scope; leaves `applied`,
// `wasMm`, `wasLayer`, `buildUp`, `matches`, `differences`, `refused` and
// `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// VERSION 2 (2026-10-13): ONE LAYER'S WIDTH, MATERIAL AND FUNCTION - any of the
// three - and a read and a check. Version 1 changed the width and nothing else,
// so the material of a layer had no route at all: a house built that day got
// its colours by painting faces, which is not type data and does not schedule.
//
// EVERY NEW VALUE CAN BE BLANK, AND BLANK MEANS "LEAVE IT". The add-in refuses
// a request value that is absent unless it is an optional bool, so a new value
// nobody could leave blank would stop every caller that does not know it is
// there. widthMm 0 means the same: keep the width.
//
//   widthMm 0, material blank, layerFunction blank  -> READ the layer as it is
//   any of the three given                          -> CHANGE those, nothing else
//   checkOnly true                                  -> CHECK them, change nothing
//
// THE LAYER IS BY INDEX, AND THE INDEX IS REVIT'S - 0 is the outside face (the
// top, on a floor or a roof). REPORT_COMPOUND_STRUCTURE numbers its lines from
// 1, so its "1." is layer 0 here; buildUp numbers them as this file does.
// layerIndex -1 finds the layer by its FUNCTION instead, the word in
// layerFunction - which then FINDS the layer and is not written to it. Two
// layers with that function are refused by number rather than one chosen.
//
// A MATERIAL IS BY ITS NAME, and only one the project already holds: the exact
// name, or the same name in another case when only one matches. A name that is
// not there refuses the whole call, naming the closest that are - Revit's own
// SetMaterialId does not check that an id is a material at all, so this is
// the only check there is. No material is ever made here; CREATE_MATERIAL is
// that job. "<By Category>" or "none" clears it.
//
// NOTHING IS WRITTEN UNTIL EVERYTHING ASKED HAS BEEN CHECKED, so a refusal
// leaves the type exactly as it was. Revit's own IsValid is then asked about
// the changed build-up before it goes back on the type, because
// SetCompoundStructure documents no refusal of its own: a membrane given a
// thickness, or a deck on a wall, is refused in Revit's words - unless the type
// already carried that same complaint before this call.
//
// A WALL'S THICKNESS IS NOT A PARAMETER. It is the sum of its layers, which is
// why WRITE_ELEMENT_PARAMETERS cannot reach it, and a layer's material is not a
// parameter either.
//
// REVIT HANDS OUT A COPY. GetCompoundStructure returns a copy of the structure,
// so changing it in place does NOTHING until SetCompoundStructure puts it back.
// That silent no-op is the whole reason this file exists rather than three lines
// at a call site.
//
// IT IS A TYPE CHANGE. Every instance of this type moves. Said out loud in the
// findings rather than discovered afterwards.
//
// 304.8 appears only at the edges - converting in, and converting out.

const double MillimetresPerFoot = 304.8;

var applied = false;
var wasMm = 0.0;
string wasLayer = null;
string buildUp = null;
var matches = false;
string differences = null;
string refused = null;
var findings = new List<string>();

var materialAsked = (material ?? "").Trim();
var functionAsked = (layerFunction ?? "").Trim();
var byFunction = layerIndex == -1;

const string FunctionWords = "Structure, Substrate, Thermal/Air Layer (or Insulation), "
    + "Finish 1, Finish 2, Membrane Layer, Structural Deck";

// The words the Edit Assembly dialog shows, less the [n] priority.
Func<MaterialFunctionAssignment, string> functionShown = function =>
{
    switch (function)
    {
        case MaterialFunctionAssignment.Structure: return "Structure";
        case MaterialFunctionAssignment.Substrate: return "Substrate";
        case MaterialFunctionAssignment.Insulation: return "Thermal/Air Layer";
        case MaterialFunctionAssignment.Finish1: return "Finish 1";
        case MaterialFunctionAssignment.Finish2: return "Finish 2";
        case MaterialFunctionAssignment.Membrane: return "Membrane Layer";
        case MaterialFunctionAssignment.StructuralDeck: return "Structural Deck";
        default: return function.ToString();
    }
};

// The dialog's words or the API's, with case, spaces and the [n] ignored.
Func<string, MaterialFunctionAssignment?> functionNamed = text =>
{
    var bracket = text.IndexOf('[');
    if (bracket >= 0) text = text.Substring(0, bracket);
    var word = new string(text.ToLowerInvariant().Where(letter => char.IsLetterOrDigit(letter)).ToArray());
    switch (word)
    {
        case "structure": return MaterialFunctionAssignment.Structure;
        case "substrate": return MaterialFunctionAssignment.Substrate;
        case "insulation":
        case "thermal":
        case "thermalair":
        case "thermalairlayer": return MaterialFunctionAssignment.Insulation;
        case "finish1": return MaterialFunctionAssignment.Finish1;
        case "finish2": return MaterialFunctionAssignment.Finish2;
        case "membrane":
        case "membranelayer": return MaterialFunctionAssignment.Membrane;
        case "deck":
        case "structuraldeck": return MaterialFunctionAssignment.StructuralDeck;
        default: return null;
    }
};

Func<ElementId, string> materialShown = id =>
{
    if (ReferenceEquals(id, null) || id == ElementId.InvalidElementId) return "<By Category>";
    var held = doc.GetElement(id) as Material;
    return held == null ? "<By Category>" : held.Name;
};

Func<CompoundStructure, int, string> layerLine = (structureRead, index) => string.Format(
    "[{0}] {1}, {2:0.#} mm, {3}", index, functionShown(structureRead.GetLayerFunction(index)),
    structureRead.GetLayerWidth(index) * MillimetresPerFoot,
    materialShown(structureRead.GetMaterialId(index)));

// The whole build-up as ONE string, because a list reaches the reply cut to
// three lines of sixty characters, and the material name is what gets cut.
Func<CompoundStructure, int, string> wholeBuildUp = (structureRead, marked) =>
{
    var firstCore = -1;
    var lastCore = -1;
    try
    {
        firstCore = structureRead.GetFirstCoreLayerIndex();
        lastCore = structureRead.GetLastCoreLayerIndex();
    }
    catch (Exception)
    {
    }

    var lines = new List<string>();
    var total = 0.0;
    for (var index = 0; index < structureRead.LayerCount; index++)
    {
        total += structureRead.GetLayerWidth(index) * MillimetresPerFoot;
        lines.Add(layerLine(structureRead, index)
            + (index >= firstCore && index <= lastCore && firstCore >= 0 ? " (core)" : "")
            + (index == marked ? " <-- this layer" : ""));
    }
    return string.Format("'{0}: {1}', {2} layer(s) from the outside face, {3:0.#} mm in all: {4}",
        hostType.FamilyName, hostType.Name, structureRead.LayerCount, total,
        string.Join(" | ", lines));
};

// Revit's complaints about a build-up, as words. Keyed by the enum's NAME so no
// member is named in code - a member missing on one release would stop this
// compiling there.
var errorWords = new Dictionary<string, string>
{
    { "MembraneTooThick", "a Membrane Layer must have no thickness" },
    { "NonmembraneTooThin", "every layer but a membrane needs a thickness" },
    { "CoreTooThin", "the core is too thin" },
    { "ThinOuterLayer", "an outside layer is too thin" },
    { "InvalidMaterialId", "the material is not one Revit can use" },
    { "VarThickLayerCantBeZero", "a variable-thickness layer cannot be 0 mm" },
    { "DeckCantBoundAbove", "a Structural Deck cannot be the top layer here" },
    { "DeckCantBoundBelow", "a Structural Deck cannot be the bottom layer here" },
    { "BadShellOrder", "the layers are in an order Revit does not allow around the core" },
    { "BadShellsStructure", "the layers outside the core are not in an order Revit allows" },
};

Func<CompoundStructure, List<string>> complaints = structureRead =>
{
    var said = new List<string>();
    IDictionary<int, CompoundStructureError> errors;
    IDictionary<int, int> pairs;
    if (structureRead.IsValid(doc, out errors, out pairs)) return said;
    if (errors != null)
    {
        foreach (var error in errors)
        {
            string words;
            var name = error.Value.ToString();
            said.Add(string.Format("layer [{0}]: {1}", error.Key,
                errorWords.TryGetValue(name, out words) ? words : name));
        }
    }
    if (said.Count == 0) said.Add("the build-up is not valid");
    return said;
};

// Closest names first: the most WORDS shared with what was typed - "White" and
// "Render" in TRG_Material_White_Render - then the ones containing it, then the
// fewest letters away. Letters alone ranked a long name against short ones by
// length, which is no likeness at all.
Func<string, List<string>> wordsOf = text =>
{
    var words = new List<string>();
    var current = "";
    foreach (var letter in text.ToLowerInvariant())
    {
        if (char.IsLetterOrDigit(letter)) { current += letter; continue; }
        if (current.Length > 1) words.Add(current);
        current = "";
    }
    if (current.Length > 1) words.Add(current);
    return words;
};

Func<string, string, int> lettersApart = (first, second) =>
{
    first = first.ToLowerInvariant();
    second = second.ToLowerInvariant();
    var row = new int[second.Length + 1];
    for (var j = 0; j <= second.Length; j++) row[j] = j;
    for (var i = 1; i <= first.Length; i++)
    {
        var diagonal = row[0];
        row[0] = i;
        for (var j = 1; j <= second.Length; j++)
        {
            var above = row[j];
            row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1),
                diagonal + (first[i - 1] == second[j - 1] ? 0 : 1));
            diagonal = above;
        }
    }
    return row[second.Length];
};

CompoundStructure structure = null;
var label = hostType == null ? "" : string.Format("'{0}: {1}'", hostType.FamilyName, hostType.Name);
var target = -1;
MaterialFunctionAssignment? functionWanted = null;   // to WRITE - never set when it FINDS
ElementId materialWanted = null;                       // InvalidElementId clears it
var changeMaterial = false;                            // ElementId's == is Revit's; null is not asked of it

if (hostType == null)
{
    refused = "No type was given. A build-up belongs to a TYPE - a wall type, a "
        + "floor type, a roof type - never to one placed element";
}
else if (widthMm < 0)
{
    refused = string.Format(
        "A layer cannot be thinner than nothing, and {0} mm was asked for. 0 leaves "
        + "the width as it is", widthMm);
}
else
{
    try
    {
        structure = hostType.GetCompoundStructure();
    }
    catch (Exception)
    {
    }

    if (structure == null)
    {
        refused = string.Format(
            "{0} has no compound structure. A curtain wall is the usual case - "
            + "it is made of panels and mullions rather than layers, which is "
            + "REPORT_CURTAIN_ELEMENTS", label);
    }
}

// ---- which layer ----
if (refused == null)
{
    var layerList = string.Join(" | ", Enumerable.Range(0, structure.LayerCount)
        .Select(index => layerLine(structure, index)));

    if (byFunction)
    {
        var findBy = functionAsked.Length == 0 ? null : functionNamed(functionAsked);
        if (functionAsked.Length == 0)
        {
            refused = string.Format(
                "Layer -1 means 'find the layer by its function', and layerFunction is "
                + "blank. Give a function - {0} - or a layer number. {1} has: {2}",
                FunctionWords, label, layerList);
        }
        else if (findBy == null)
        {
            refused = string.Format("'{0}' is not a layer function. The functions are {1}",
                functionAsked, FunctionWords);
        }
        else
        {
            var hits = Enumerable.Range(0, structure.LayerCount)
                .Where(index => structure.GetLayerFunction(index) == findBy.Value).ToList();
            if (hits.Count == 1)
            {
                target = hits[0];
            }
            else if (hits.Count == 0)
            {
                refused = string.Format("{0} has no {1} layer. It has: {2}",
                    label, functionShown(findBy.Value), layerList);
            }
            else
            {
                refused = string.Format(
                    "{0} has {1} {2} layers - numbers {3} - so the function alone does not "
                    + "say which. Give the number in layerIndex. It has: {4}",
                    label, hits.Count, functionShown(findBy.Value),
                    string.Join(", ", hits), layerList);
            }
        }
    }
    else if (layerIndex < 0 || layerIndex >= structure.LayerCount)
    {
        refused = string.Format(
            "Layer {0} does not exist - {1} has {2} layer(s), numbered 0 (the outside "
            + "face) to {3}, and -1 finds one by its function. REPORT_COMPOUND_STRUCTURE "
            + "counts from 1, so its 1 is 0 here. It has: {4}",
            layerIndex, label, structure.LayerCount, structure.LayerCount - 1, layerList);
    }
    else
    {
        target = layerIndex;
        if (functionAsked.Length > 0)
        {
            functionWanted = functionNamed(functionAsked);
            if (functionWanted == null)
            {
                refused = string.Format(
                    "'{0}' is not a layer function. The functions are {1}; blank leaves "
                    + "the function as it is", functionAsked, FunctionWords);
            }
        }
    }
}

// ---- which material ----
if (refused == null && materialAsked.Length > 0)
{
    var plain = materialAsked.ToLowerInvariant();
    if (plain == "<by category>" || plain == "by category" || plain == "none")
    {
        materialWanted = ElementId.InvalidElementId;
        changeMaterial = true;
    }
    else
    {
        var all = new FilteredElementCollector(doc).OfClass(typeof(Material))
            .Cast<Material>().ToList();
        var exact = all.Where(each => each.Name == materialAsked).ToList();
        var anyCase = all.Where(each => string.Equals(each.Name, materialAsked,
            StringComparison.OrdinalIgnoreCase)).ToList();

        if (exact.Count == 1) materialWanted = exact[0].Id;
        else if (exact.Count == 0 && anyCase.Count == 1) materialWanted = anyCase[0].Id;
        else if (anyCase.Count > 1)
        {
            refused = string.Format(
                "'{0}' matches {1} materials when case is ignored - {2} - and none exactly. "
                + "Type one as Revit spells it", materialAsked, anyCase.Count,
                string.Join(", ", anyCase.Select(each => "'" + each.Name + "'")));
        }
        else
        {
            var askedWords = wordsOf(materialAsked);
            var closest = all
                .OrderByDescending(each => wordsOf(each.Name).Count(word => askedWords.Contains(word)))
                .ThenBy(each => each.Name.IndexOf(materialAsked, StringComparison.OrdinalIgnoreCase) >= 0
                    || materialAsked.IndexOf(each.Name, StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
                .ThenBy(each => lettersApart(each.Name, materialAsked))
                .Take(8)
                .Select(each => "'" + each.Name + "'");
            refused = string.Format(
                "There is no material '{0}' in this project, and none is made here. The "
                + "closest of the {1} it has: {2} - LIST_MATERIALS names every one. "
                + "CREATE_MATERIAL makes one; '<By Category>' clears the layer's material. "
                + "Nothing was changed",
                materialAsked, all.Count, string.Join(", ", closest));
        }
        changeMaterial = !ReferenceEquals(materialWanted, null);
    }
}

var widthChange = widthMm > 0;
var anythingAsked = widthChange || changeMaterial || functionWanted != null;

if (refused == null && checkOnly && !anythingAsked)
{
    refused = "checkOnly was asked with nothing to check against - give a width, a "
        + "material or a function. Nothing was changed";
}

// ---- read, check or change ----
if (refused == null)
{
    wasMm = structure.GetLayerWidth(target) * MillimetresPerFoot;
    wasLayer = layerLine(structure, target);

    if (checkOnly)
    {
        var differ = new List<string>();
        if (widthChange && Math.Abs(wasMm - widthMm) >= 0.5)
            differ.Add(string.Format("width is {0:0.#} mm, not {1:0.#} mm", wasMm, widthMm));
        if (changeMaterial && structure.GetMaterialId(target) != materialWanted)
            differ.Add(string.Format("material is '{0}', not '{1}'",
                materialShown(structure.GetMaterialId(target)), materialShown(materialWanted)));
        if (functionWanted != null && structure.GetLayerFunction(target) != functionWanted.Value)
            differ.Add(string.Format("function is {0}, not {1}",
                functionShown(structure.GetLayerFunction(target)), functionShown(functionWanted.Value)));

        matches = differ.Count == 0;
        differences = matches ? null : string.Join("; ", differ);
        buildUp = wholeBuildUp(structure, target);
        findings.Add(matches
            ? string.Format("CHECKED, nothing changed: layer {0} of {1} matches - {2}",
                target, label, wasLayer)
            : string.Format("CHECKED, nothing changed: layer {0} of {1} differs - {2}",
                target, label, differences));
    }
    else if (!anythingAsked)
    {
        buildUp = wholeBuildUp(structure, target);
        findings.Add(string.Format(
            "Nothing was asked to change, so nothing did. Layer {0} of {1} is {2}",
            target, label, wasLayer));
    }
    else
    {
        var before = complaints(hostType.GetCompoundStructure());
        var original = hostType.GetCompoundStructure();
        var written = false;
        try
        {
            if (widthChange) structure.SetLayerWidth(target, widthMm / MillimetresPerFoot);
            if (changeMaterial) structure.SetMaterialId(target, materialWanted);
            if (functionWanted != null) structure.SetLayerFunction(target, functionWanted.Value);

            var added = complaints(structure).Where(said => !before.Contains(said)).ToList();
            if (added.Count > 0)
            {
                refused = string.Format(
                    "Revit says the build-up would not be valid: {0}. Nothing was changed",
                    string.Join("; ", added));
            }
            else
            {
                // WITHOUT THIS LINE NOTHING HAPPENS - see the header.
                hostType.SetCompoundStructure(structure);
                written = true;

                // READ IT BACK off the type, not off the copy.
                var after = hostType.GetCompoundStructure();
                var stuck = new List<string>();
                if (after == null || target >= after.LayerCount)
                {
                    stuck.Add("the layer is not there any more");
                }
                else
                {
                    if (widthChange && Math.Abs(after.GetLayerWidth(target) * MillimetresPerFoot - widthMm) >= 0.5)
                        stuck.Add("the width");
                    if (changeMaterial && after.GetMaterialId(target) != materialWanted)
                        stuck.Add("the material");
                    if (functionWanted != null && after.GetLayerFunction(target) != functionWanted.Value)
                        stuck.Add("the function");
                }

                if (stuck.Count == 0)
                {
                    applied = true;
                    buildUp = wholeBuildUp(after, target);
                }
                else
                {
                    var readBack = after != null && target < after.LayerCount
                        ? layerLine(after, target) : "nothing";
                    hostType.SetCompoundStructure(original);
                    written = false;
                    refused = string.Format(
                        "Revit took the change and {0} did not hold - layer {1} read back as "
                        + "{2}. It was put back as it was: {3}",
                        string.Join(", ", stuck), target, readBack, wasLayer);
                }
            }
        }
        catch (Exception ex)
        {
            if (written)
            {
                try { hostType.SetCompoundStructure(original); }
                catch (Exception) { }
            }
            refused = string.Format("Revit refused the change: {0}. Nothing was changed", ex.Message);
        }

        if (applied)
        {
            var reach = "";
            try
            {
                if (hostType.Category != null)
                {
                    var placed = new FilteredElementCollector(doc)
                        .OfCategoryId(hostType.Category.Id).WhereElementIsNotElementType()
                        .Count(each => each.GetTypeId() == hostType.Id);
                    reach = string.Format(" - {0} placed in this model", placed);
                }
            }
            catch (Exception)
            {
            }

            findings.Add(string.Format(
                "Layer {0} of {1} was {2}, and is now {3}. THIS IS A TYPE CHANGE - every "
                + "instance of this type{4} has changed with it. If that was not wanted, "
                + "DUPLICATE_TYPE first and edit the copy",
                target, label, wasLayer, layerLine(hostType.GetCompoundStructure(), target), reach));
        }
    }
}

if (refused != null)
{
    findings.Add(refused);
}
