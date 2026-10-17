// NOT STANDALONE. Assumes `doc`, `elements`, `sampleOnly`,
// `includeTypeParameters`, `fullRecord`, `includeTags`, `elementIds`,
// `linkName`, `maxChars` and `parameterFilter` are in scope; leaves `findings`,
// `parameterNames`, `instanceCount`, `typeCount`, `elementsRead`, `record`,
// `sameNameTwice`, `tagsFound` and `missing` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE PARAMETER GROUP IS DELIBERATELY NOT REPORTED. `Definition.ParameterGroup`
// is gone by Revit 2027 and its replacement `GetGroupTypeId` has never existed
// on 2020 - neither works at both ends, and this is one implementation for all
// eight releases. The group is a sorting convenience in a dialog; nothing about
// reading or writing a value needs it.
//
// THREE KINDS, AND ONLY THREE CAN HONESTLY BE TOLD APART. Revit carries no flag
// separating a family parameter from a project parameter once both are on an
// element, so the third bucket is named as the bucket it is rather than guessed
// into one of two.
//
// VALUES COME THROUGH AsValueString FIRST - Revit's own formatting in the
// project's units, so a duct size reads "450x250" and no units API is named.
// The record adds the RAW value beside it (version 3): the number Revit stores,
// in its internal units, labelled as such and never converted, because the
// conversion is exactly what a reader cannot check.
//
// A PARAMETER IS KNOWN BY WHAT IT IS, NEVER BY ITS NAME ALONE (version 2). A
// project can carry two shared parameters with the same name - seen 2026-10-12
// on Revit 2020, two called "Equipment Tag", GUID e133774c... filled and GUID
// cb0704fc... empty. Each line is keyed by the parameter's own id and name, and
// a shared one carries its GUID, so both are listed and told apart.
//
// THE RECORD IS ONE STRING ON PURPOSE. A Heron reply cuts a list to its first
// three items of 60 characters and a dictionary to its count
// (RevitFragment.Describe), so `findings` alone cannot carry a full description
// to the chat. A string is printed whole.
//
// VERSION 3 - ONE CALL THAT DESCRIBES UP TO 50 ELEMENTS FULLY, AND STOPS AT A
// SIZE IT SAYS. Version 2's record had no cap: 535 elements in one call came
// back as a 6.2 MB string (NEEDS-CHECKING DI3), and a 322,725-character
// hand-back was measured costing a session minutes of the model's own reading
// (FRAGMENT-ISSUES row 5b-407). So the record holds the first 50 elements and
// says "first 50 of N", and the whole string stops at `maxChars` - the card's
// default when it is left out - with a line naming every element not written.
// `parameterFilter` keeps only the parameters asked about.
//
// ELEMENT IDS TYPED AS DIGITS, MATCHED AS TEXT HERE, NOT BY THE BINDER.
// `elementIds` is a string on purpose. The add-in's typed-id rule (row 5b-441)
// binds an id LIST, refuses the whole call when one id names nothing, and looks
// only in the host - and this read has to do both of the other things: put an
// id that names nothing in `missing` and describe the rest, and find ids inside
// a LINKED model, where the same number names nothing or something else in the
// host. So the ids are matched here the binder's way - each element's own
// Id.ToString() compared with what was typed, one walk of the model - and no
// ElementId is ever built from a number: that constructor changed from int to
// long at Revit 2024 and nothing here carries a version #if (D-05).

var findings = new List<string>();
var parameterNames = new List<string>();
var instanceCount = 0;
var typeCount = 0;
var elementsRead = 0;
var record = "";
var sameNameTwice = new List<string>();
var tagsFound = 0;
var missing = new List<string>();

var mm = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var recordCap = 50;
var leastBudget = 5000;

Func<string, string, bool> sameWord = (a, b) =>
    string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);

Func<Element, string> nameOf = element =>
{
    try { return element.Name ?? ""; } catch (Exception) { return ""; }
};

Func<Element, string> typeLabelOf = element =>
{
    var type = element as ElementType;
    if (type == null) return nameOf(element);
    var family = "";
    try { family = type.FamilyName ?? ""; } catch (Exception) { }
    return family.Length > 0 ? family + ": " + nameOf(type) : nameOf(type);
};

Func<Document, string> titleOf = document =>
{
    try { return document == null ? "?" : (document.Title ?? "?"); } catch (Exception) { return "?"; }
};

// ---- which elements: ids typed, in this model or a link, or what was handed in ----

var typedText = (elementIds ?? "").Trim();
var idsTyped = typedText.Length > 0 && !sameWord(typedText, "none")
    && !sameWord(typedText, "selected") && !sameWord(typedText, "selection");

var typed = new List<string>();
var typedOnce = new HashSet<string>(StringComparer.Ordinal);
var typedTwice = 0;
if (idsTyped)
{
    foreach (var piece in typedText.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' },
                                          StringSplitOptions.RemoveEmptyEntries))
    {
        var part = piece.Trim();
        if (part.Length == 0) continue;

        var digits = true;
        foreach (var c in part)
        {
            if (c < '0' || c > '9') { digits = false; break; }
        }
        if (!digits)
        {
            // Not an id at all - named in `missing` rather than dropped, so a
            // name typed among ids shows as the thing that found nothing.
            missing.Add(part + " (not an id - digits only)");
            continue;
        }
        if (!typedOnce.Add(part)) { typedTwice++; continue; }
        typed.Add(part);
    }
}

// THE MODEL THE IDS ARE LOOKED UP IN. The host unless `linkName` names a placed
// link - by its id as Revit shows it, or any part of its name, ".rvt" or not.
// Several placements of ONE linked model are one document and are read once;
// placements of two different models matching the same words are refused
// rather than one picked, the way SELECT_FROM_LINK refuses them. Asked only
// with ids typed: a selection is always this model's own elements.
var linkWord = (linkName ?? "").Trim();
var linkAsked = linkWord.Length > 0 && !sameWord(linkWord, "host") && !sameWord(linkWord, "none");

Document readIn = doc;
RevitLinkInstance throughLink = null;
string linkProblem = null;
var placements = 0;

if (linkAsked && idsTyped)
{
    var placed = new List<string>();
    var matched = new List<RevitLinkInstance>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
    {
        var instance = candidate as RevitLinkInstance;
        if (instance == null) continue;

        var instanceName = nameOf(instance);
        placed.Add("'" + instanceName + "' (id " + instance.Id + ")");
        if (instance.Id.ToString() == linkWord
            || instanceName.IndexOf(linkWord, StringComparison.OrdinalIgnoreCase) >= 0)
            matched.Add(instance);
    }

    var loaded = new List<RevitLinkInstance>();
    var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var instance in matched)
    {
        Document linked = null;
        try { linked = instance.GetLinkDocument(); } catch (Exception) { }
        if (linked == null) continue;
        loaded.Add(instance);
        var path = "";
        try { path = linked.PathName ?? ""; } catch (Exception) { }
        models.Add(titleOf(linked) + "|" + path);
    }

    var everyLink = placed.Count == 0 ? "no links at all" : string.Join(", ", placed);
    if (matched.Count == 0)
    {
        linkProblem = string.Format("No placed link matches '{0}'. This model places: {1}",
            linkWord, everyLink);
    }
    else if (loaded.Count == 0)
    {
        linkProblem = string.Format("'{0}' matches a link that is present but NOT LOADED, so it has "
            + "no document to look the ids up in. Load it in Manage Links and ask again", linkWord);
    }
    else if (models.Count > 1)
    {
        linkProblem = string.Format("AMBIGUOUS: '{0}' matches {1} different linked models. Name one "
            + "exactly - by more of its name or by the link's id. This model places: {2}",
            linkWord, models.Count, everyLink);
    }
    else
    {
        throughLink = loaded[0];
        readIn = throughLink.GetLinkDocument();
        placements = loaded.Count;
    }
}

var targets = new List<Element>();
var handedIn = 0;
if (elements != null)
{
    foreach (var element in elements) if (element != null) handedIn++;
}

string source;
if (idsTyped)
{
    if (linkProblem != null)
    {
        // Nothing was looked up, so every id typed is one that described
        // nothing - in `missing`, and the reason first in the findings.
        missing.AddRange(typed);
        source = typed.Count + " id(s) typed, none looked up";
    }
    else
    {
        var slot = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < typed.Count; i++) slot[typed[i]] = i;

        var found = new Element[typed.Count];
        var left = typed.Count;
        if (left > 0)
        {
            // Element types too, in the one walk: an id an earlier answer
            // printed may be a type's. The walk stops once every id is found.
            var everything = new LogicalOrFilter(new ElementIsElementTypeFilter(false),
                                                 new ElementIsElementTypeFilter(true));
            foreach (var id in new FilteredElementCollector(readIn).WherePasses(everything).ToElementIds())
            {
                int at;
                if (!slot.TryGetValue(id.ToString(), out at) || found[at] != null) continue;
                found[at] = readIn.GetElement(id);
                if (found[at] != null && --left == 0) break;
            }
        }

        for (var i = 0; i < typed.Count; i++)
        {
            if (found[i] == null) missing.Add(typed[i]);
            else targets.Add(found[i]);
        }
        source = string.Format("{0} id(s) typed, {1} found in '{2}'", typed.Count, targets.Count,
            titleOf(readIn));
    }
}
else
{
    if (elements != null)
    {
        foreach (var element in elements) if (element != null) targets.Add(element);
    }
    source = targets.Count + " element(s) handed in - the selection or the step before";
}

if (linkProblem != null) findings.Add(linkProblem);
if (linkAsked && !idsTyped)
{
    findings.Add("linkName is read only with elementIds - the ids are the link's own. Nothing in a "
        + "link was read; what was handed in is described in this model");
}
if (idsTyped && handedIn > 0)
{
    findings.Add(string.Format("The {0} element(s) selected were NOT read - ids were typed, and the ids "
        + "decide", handedIn));
}
if (typedTwice > 0)
{
    findings.Add(string.Format("{0} id(s) typed more than once - each element is read once", typedTwice));
}
// Where the missing ones were looked for, in words - or that they were not
// looked for at all, because no link could be read.
var missingWhere = linkProblem != null
    ? "were NOT looked up - no link was read, see above"
    : "named no element in '" + titleOf(readIn) + "'";
if (missing.Count > 0)
{
    findings.Add(string.Format("{0} typed value(s) {1} and are in missing: {2}. An id belongs to one "
        + "model - it names nothing, or something else, in another one",
        missing.Count, missingWhere, string.Join(", ", missing.Take(20))
            + (missing.Count > 20 ? ", ..." : "")));
}

// ---- reading one parameter -------------------------------------------------

// The built-in parameter's own name - FAMILY_LEVEL_PARAM - or empty for a
// parameter that is not built in.
Func<Parameter, string> builtInOf = p =>
{
    try
    {
        var internalDef = p.Definition as InternalDefinition;
        if (internalDef == null) return "";
        var bip = internalDef.BuiltInParameter;
        return bip == BuiltInParameter.INVALID ? "" : bip.ToString();
    }
    catch (Exception) { return ""; }
};

// The GUID of a shared parameter, empty for any other kind. Parameter.GUID
// throws on a parameter that is not shared, so IsShared is asked first.
Func<Parameter, string> guidOf = p =>
{
    try { return p.IsShared ? p.GUID.ToString() : ""; }
    catch (Exception) { return ""; }
};

Func<Parameter, string> kindOf = p =>
{
    if (builtInOf(p).Length > 0) return "built-in";
    if (guidOf(p).Length > 0) return "shared";
    return "project/family (not shared)";
};

// The value as Revit shows it. An ElementId value is named from the
// parameter's OWN document - a linked element's level is in the link.
Func<Parameter, Document, string> valueOf = (p, owner) =>
{
    try
    {
        if (!p.HasValue) return "<blank>";
        var shown = p.AsValueString();
        if (!string.IsNullOrEmpty(shown)) return shown;
        if (p.StorageType == StorageType.String) return p.AsString() ?? "<blank>";
        if (p.StorageType == StorageType.Integer) return p.AsInteger().ToString(invariant);
        if (p.StorageType == StorageType.ElementId)
        {
            var target = owner.GetElement(p.AsElementId());
            return target == null ? "<none>" : nameOf(target);
        }
        return "<no readable value>";
    }
    catch (Exception) { return "<unreadable>"; }
};

// The value as Revit STORES it, by storage type, in Revit's internal units -
// feet, square feet, cubic feet, radians - never converted. An id is printed
// by ToString(), never read as a number.
Func<Parameter, string> rawOf = p =>
{
    try
    {
        if (!p.HasValue) return "none";
        if (p.StorageType == StorageType.Double) return p.AsDouble().ToString("R", invariant);
        if (p.StorageType == StorageType.Integer) return p.AsInteger().ToString(invariant);
        if (p.StorageType == StorageType.String)
        {
            var text = p.AsString();
            return text == null ? "none" : "\"" + text + "\"";
        }
        if (p.StorageType == StorageType.ElementId)
        {
            var id = p.AsElementId();
            return id == null ? "none" : "id " + id;
        }
        return "none";
    }
    catch (Exception) { return "<unreadable>"; }
};

// One key per parameter, not per name. The id tells two same-named shared
// parameters apart; the name is kept beside it because a family parameter that
// is not shared can carry no id of its own in a project.
Func<Parameter, string> identityOf = p =>
{
    var guid = guidOf(p);
    if (guid.Length > 0) return "guid:" + guid;
    var id = "";
    try { id = p.Id.ToString(); } catch (Exception) { }
    return "id:" + id + "|" + p.Definition.Name;
};

// Every parameter on one element, sorted by name ignoring case - and, for one
// name carried twice, by GUID and then built-in name, so the order is the same
// on every run and two records can be compared line by line.
Func<Element, List<Parameter>> parametersOf = element =>
{
    var all = new List<Parameter>();
    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter != null && parameter.Definition != null) all.Add(parameter);
    }
    all.Sort((a, b) =>
    {
        var order = StringComparer.OrdinalIgnoreCase.Compare(a.Definition.Name, b.Definition.Name);
        if (order != 0) return order;
        order = string.CompareOrdinal(guidOf(a), guidOf(b));
        return order != 0 ? order : string.CompareOrdinal(builtInOf(a), builtInOf(b));
    });
    return all;
};

// ---- the inventory, as versions 1 and 2 -----------------------------------

var seen = new HashSet<string>();
Action<Element, string> inventoryOf = (element, whose) =>
{
    if (element == null) return;
    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter == null || parameter.Definition == null) continue;

        var name = parameter.Definition.Name;
        if (!seen.Add(whose + "|" + identityOf(parameter))) continue;

        parameterNames.Add(name);
        if (whose == "instance") instanceCount++; else typeCount++;

        var guid = guidOf(parameter);
        findings.Add(string.Format("  {0,-40} {1,-9} {2,-28} {3}{4}{5}",
            name, parameter.StorageType, kindOf(parameter), valueOf(parameter, element.Document),
            parameter.IsReadOnly ? "   [read-only]" : "",
            guid.Length > 0 ? "   [GUID " + guid + "]" : ""));
    }
};

// Any name one element or type carries more than once, into `sameNameTwice`.
// A name counts only when at least one of its parameters is not built in:
// Revit carries a few built-in pairs on every element - two Levels, two
// Categories - and listing those on each of 535 elements, measured 2026-10-12,
// buried the one pair that mattered. Returns what it added.
Func<Element, string, List<string>> pairsOf = (element, whose) =>
{
    var added = new List<string>();
    var byName = new Dictionary<string, List<string>>();
    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter == null || parameter.Definition == null) continue;
        var name = parameter.Definition.Name;
        var guid = guidOf(parameter);
        List<string> guids;
        if (!byName.TryGetValue(name, out guids)) { guids = new List<string>(); byName[name] = guids; }
        guids.Add(guid.Length > 0 ? guid : kindOf(parameter));
    }
    foreach (var pair in byName)
    {
        if (pair.Value.Count < 2 || pair.Value.TrueForAll(g => g == "built-in")) continue;
        added.Add(string.Format("{0} id {1}: '{2}' x{3} ({4})",
            whose, element.Id, pair.Key, pair.Value.Count, string.Join(", ", pair.Value)));
    }
    sameNameTwice.AddRange(added);
    return added;
};

foreach (var element in targets)
{
    elementsRead++;
    if (elementsRead == 1)
    {
        findings.Add(string.Format("INSTANCE parameters, from '{0}' (id {1}):", nameOf(element), element.Id));
    }
    inventoryOf(element, "instance");
    if (sampleOnly) break;
}

if (includeTypeParameters && targets.Count > 0)
{
    var first = targets[0];
    var type = first.Document.GetElement(first.GetTypeId()) as ElementType;
    if (type == null)
    {
        findings.Add("TYPE parameters: this element has no type - a line, a group or a view does "
            + "not. That is not a fault");
    }
    else
    {
        findings.Add(string.Format("TYPE parameters, from '{0}':", typeLabelOf(type)));
        inventoryOf(type, "type");

        if (!sampleOnly)
        {
            var seenTypes = new HashSet<string>();
            seenTypes.Add(type.Id.ToString());
            foreach (var element in targets)
            {
                var other = element.Document.GetElement(element.GetTypeId()) as ElementType;
                if (other == null || !seenTypes.Add(other.Id.ToString())) continue;
                inventoryOf(other, "type");
            }
        }
    }
}

if (parameterNames.Count == 0)
{
    if (idsTyped)
        findings.Add(typed.Count > 0
            ? string.Format("None of the {0} id(s) typed named an element that could be read - see "
                + "missing", typed.Count)
            : "Nothing typed in elementIds was an element id - digits only, comma separated. See missing");
    else if (targets.Count == 0)
        findings.Add("Nothing was handed in - nothing selected and no element ids typed. Select the "
            + "elements, or type their ids in elementIds, digits only, comma separated");
    else
        findings.Add("No parameters could be read at all. That is not a normal state for a model "
            + "element - check what was handed in");
}

// ---- the full record, one string ----------------------------------------
//
// THE 2022 TAG SPLIT, LOOKED UP AT RUN TIME. No version #if: an add-in built
// before row 5b-181's fix compiles a fragment with no release symbols, so an
// #if always takes its #else. Each member is found by name on the tag in
// front of it - GetTaggedLocalElementIds and GetTaggedReferences/GetLeaderEnd
// from 2022, TaggedLocalElementId and LeaderEnd on 2020 and 2021 - the same
// reading COPY_TAGS_TO_REPLACEMENTS makes. TagHeadPosition, HasLeader and
// OwnerViewId are on every release.
//
// TAGS ARE READ IN THIS MODEL ONLY. A tag in the host on an element in a link
// tags a linked reference, not a local id, so for elements read through a link
// no tag is looked for - and the record says so rather than reporting none.

if (fullRecord)
{
    Func<XYZ, string> pointMm = point => point == null ? "?" : string.Format(invariant,
        "({0:0.#}, {1:0.#}, {2:0.#})", point.X * mm, point.Y * mm, point.Z * mm);

    Func<XYZ, string> direction = vector => vector == null ? "?" : string.Format(invariant,
        "({0:0.###}, {1:0.###}, {2:0.###})", vector.X, vector.Y, vector.Z);

    Transform toHost = null;
    if (throughLink != null)
    {
        try { toHost = throughLink.GetTotalTransform(); } catch (Exception) { toHost = null; }
    }

    // The filter: comma-separated words, any of which may appear anywhere in
    // a parameter's name or its built-in name, ignoring case. "*", "all" or
    // nothing keeps every parameter.
    var filterWords = new List<string>();
    var filterText = (parameterFilter ?? "").Trim();
    if (filterText.Length > 0 && filterText != "*" && !sameWord(filterText, "all"))
    {
        foreach (var word in filterText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Trim().Length > 0) filterWords.Add(word.Trim());
        }
    }
    Func<Parameter, bool> kept = p =>
    {
        if (filterWords.Count == 0) return true;
        var name = p.Definition.Name ?? "";
        var builtIn = builtInOf(p);
        foreach (var word in filterWords)
        {
            if (name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0
                || builtIn.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    };

    // One line per parameter kept: name | storage | kind | read-only | GUID |
    // built-in name | value as shown | raw. A text value Revit shows as it
    // stores it says "as shown" rather than printing itself twice.
    Func<Element, string, List<string>> parameterLines = (element, heading) =>
    {
        var all = parametersOf(element);
        var lines = new List<string>();
        foreach (var parameter in all)
        {
            if (!kept(parameter)) continue;
            var shown = valueOf(parameter, element.Document);
            var raw = rawOf(parameter);
            if (parameter.StorageType == StorageType.String && raw == "\"" + shown + "\"") raw = "as shown";
            var guid = guidOf(parameter);
            var builtIn = builtInOf(parameter);
            lines.Add(string.Format("    {0} | {1} | {2} | {3} | {4} | {5} | {6} | raw {7}",
                parameter.Definition.Name, parameter.StorageType, kindOf(parameter),
                parameter.IsReadOnly ? "read-only" : "writable",
                guid.Length > 0 ? "GUID " + guid : "-",
                builtIn.Length > 0 ? builtIn : "-",
                shown, raw));
        }
        lines.Insert(0, string.Format("  {0} ({1}{2}):", heading,
            filterWords.Count == 0 ? "" : lines.Count + " kept of ", all.Count));
        return lines;
    };

    // Which level an element is on. There is no one way to ask: a wall
    // answers only through LevelId, a duct or pipe only through its Reference
    // Level, an air terminal through "Level". LevelId first, then five
    // built-in parameters in turn until one holds a level - the same five
    // READ_ELEMENT_PARAMETERS asks, RBS_START_LEVEL_PARAM last because it is
    // the one that catches MEP curves - and the line says which one answered.
    var levelParameters = new[]
    {
        BuiltInParameter.FAMILY_LEVEL_PARAM,
        BuiltInParameter.SCHEDULE_LEVEL_PARAM,
        BuiltInParameter.LEVEL_PARAM,
        BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
        BuiltInParameter.RBS_START_LEVEL_PARAM,
    };
    Func<Element, string> levelOf = element =>
    {
        ElementId levelId = null;
        var via = "LevelId";
        try { levelId = element.LevelId; } catch (Exception) { levelId = null; }
        if (levelId == null || levelId == ElementId.InvalidElementId)
        {
            levelId = null;
            foreach (var bip in levelParameters)
            {
                try
                {
                    var parameter = element.get_Parameter(bip);
                    if (parameter == null || parameter.StorageType != StorageType.ElementId) continue;
                    var id = parameter.AsElementId();
                    if (id == null || id == ElementId.InvalidElementId) continue;
                    levelId = id;
                    via = bip.ToString();
                    break;
                }
                catch (Exception) { }
            }
        }
        if (levelId == null) return "  level: none - LevelId and the five level parameters are all empty";
        var level = element.Document.GetElement(levelId);
        return string.Format("  level '{0}' (id {1}, from {2})", level == null ? "?" : nameOf(level), levelId, via);
    };

    var tagsOn = new Dictionary<string, List<IndependentTag>>();
    var readTags = includeTags && throughLink == null;
    if (readTags)
    {
        foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)))
        {
            var tag = candidate as IndependentTag;
            if (tag == null) continue;

            var tagged = new List<ElementId>();
            try
            {
                var many = tag.GetType().GetMethod("GetTaggedLocalElementIds", Type.EmptyTypes);
                if (many != null)
                {
                    var all = many.Invoke(tag, null) as IEnumerable<ElementId>;
                    if (all != null) tagged.AddRange(all);
                }
                else
                {
                    var single = tag.GetType().GetProperty("TaggedLocalElementId");
                    var one = single == null ? null : single.GetValue(tag, null) as ElementId;
                    if (one != null) tagged.Add(one);
                }
            }
            catch (Exception) { }

            foreach (var id in tagged)
            {
                if (id == null || id == ElementId.InvalidElementId) continue;
                var key = id.ToString();
                if (!tagsOn.ContainsKey(key)) tagsOn[key] = new List<IndependentTag>();
                tagsOn[key].Add(tag);
            }
        }
    }

    Func<IndependentTag, XYZ> leaderEndOf = tag =>
    {
        try
        {
            var method = tag.GetType().GetMethod("GetLeaderEnd", new[] { typeof(Reference) });
            if (method != null)
            {
                var references = tag.GetType().GetMethod("GetTaggedReferences", Type.EmptyTypes);
                var list = references == null ? null : references.Invoke(tag, null) as IList<Reference>;
                if (list == null || list.Count == 0) return null;
                return method.Invoke(tag, new object[] { list[0] }) as XYZ;
            }
            var property = tag.GetType().GetProperty("LeaderEnd");
            return property == null ? null : property.GetValue(tag, null) as XYZ;
        }
        catch (Exception) { return null; }
    };

    // ---- each element as a block of lines, every block built whole ----

    var recording = new List<Element>();
    foreach (var element in targets)
    {
        if (recording.Count >= (sampleOnly ? 1 : recordCap)) break;
        recording.Add(element);
    }

    var blocks = new List<List<string>>();
    var typesWritten = new HashSet<string>();
    var anyWorkshared = false;

    foreach (var element in recording)
    {
        var owner = element.Document;
        var lines = new List<string>();

        var category = "no category";
        try { if (element.Category != null) category = element.Category.Name; } catch (Exception) { }
        var type = owner.GetElement(element.GetTypeId()) as ElementType;
        lines.Add(string.Format("ELEMENT id {0} | {1} | {2} | {3}",
            element.Id, category, type == null ? nameOf(element) : typeLabelOf(type),
            element.GetType().Name));

        var uniqueId = "?";
        try { uniqueId = element.UniqueId ?? "?"; } catch (Exception) { }
        lines.Add("  unique id " + uniqueId);

        lines.Add(levelOf(element));

        var ownerView = ElementId.InvalidElementId;
        try { ownerView = element.OwnerViewId; } catch (Exception) { }
        if (ownerView == null || ownerView == ElementId.InvalidElementId)
        {
            lines.Add("  owner view: none - a model element, not drawn in one view");
        }
        else
        {
            var view = owner.GetElement(ownerView);
            lines.Add(string.Format("  owner view '{0}' (id {1})", view == null ? "?" : nameOf(view), ownerView));
        }

        var workshared = false;
        try { workshared = owner.IsWorkshared; } catch (Exception) { }
        if (workshared)
        {
            anyWorkshared = true;
            var worksetName = "?";
            try
            {
                var workset = owner.GetWorksetTable().GetWorkset(element.WorksetId);
                if (workset != null) worksetName = workset.Name;
            }
            catch (Exception) { }
            lines.Add("  workset '" + worksetName + "'");
        }

        var placedLink = element as RevitLinkInstance;
        if (placedLink != null)
        {
            Document linked = null;
            try { linked = placedLink.GetLinkDocument(); } catch (Exception) { }
            lines.Add(linked == null
                ? "  link: NOT LOADED - no document to name"
                : "  link document '" + titleOf(linked) + "'");
        }

        // Where it stands.
        var location = element.Location;
        var point = location as LocationPoint;
        var curve = location as LocationCurve;
        if (point != null)
        {
            var rotation = "";
            try { rotation = string.Format(invariant, ", rotation {0:0.##} deg", point.Rotation * 180.0 / Math.PI); }
            catch (Exception) { }
            lines.Add("  location point " + pointMm(point.Point) + " mm" + rotation);
            if (toHost != null)
                lines.Add("  in this model's coordinates " + pointMm(toHost.OfPoint(point.Point)) + " mm");
        }
        else if (curve != null && curve.Curve != null && curve.Curve.IsBound)
        {
            var length = "";
            try { length = string.Format(invariant, ", length {0:0.#} mm", curve.Curve.Length * mm); }
            catch (Exception) { }
            lines.Add("  location curve " + pointMm(curve.Curve.GetEndPoint(0)) + " -> "
                + pointMm(curve.Curve.GetEndPoint(1)) + " mm" + length);
            if (toHost != null)
                lines.Add("  in this model's coordinates " + pointMm(toHost.OfPoint(curve.Curve.GetEndPoint(0)))
                    + " -> " + pointMm(toHost.OfPoint(curve.Curve.GetEndPoint(1))) + " mm");
        }
        else
        {
            lines.Add("  location: none - no point and no bound curve");
        }

        var box = element.get_BoundingBox(null);
        lines.Add(box == null
            ? "  bounding box: none"
            : "  bounding box min " + pointMm(box.Min) + " max " + pointMm(box.Max) + " mm");

        var instance = element as FamilyInstance;
        if (instance != null)
        {
            var facing = "?"; var hand = "?"; var flips = "";
            try { facing = direction(instance.FacingOrientation); } catch (Exception) { }
            try { hand = direction(instance.HandOrientation); } catch (Exception) { }
            try
            {
                flips = string.Format(" | facing flipped {0}, hand flipped {1}, mirrored {2}",
                    instance.FacingFlipped ? "yes" : "no", instance.HandFlipped ? "yes" : "no",
                    instance.Mirrored ? "yes" : "no");
            }
            catch (Exception) { }
            lines.Add("  facing " + facing + " | hand " + hand + flips);
        }

        // A name carried twice is named on the element it is on, before its
        // parameters, so a cut record still shows it for what was written.
        var pairs = pairsOf(element, "instance");
        if (type != null && includeTypeParameters && !typesWritten.Contains(type.Id.ToString()))
            pairs.AddRange(pairsOf(type, "type"));
        if (pairs.Count > 0) lines.Add("  A NAME CARRIED TWICE - compare by GUID: " + string.Join("; ", pairs));

        lines.AddRange(parameterLines(element, "INSTANCE parameters"));

        if (includeTypeParameters)
        {
            if (type == null)
            {
                lines.Add("  TYPE: this element has no type");
            }
            else if (!typesWritten.Add(type.Id.ToString()))
            {
                lines.Add(string.Format("  TYPE '{0}' (id {1}): listed above", typeLabelOf(type), type.Id));
            }
            else
            {
                lines.AddRange(parameterLines(type,
                    string.Format("TYPE '{0}' (id {1}) parameters", typeLabelOf(type), type.Id)));
            }
        }

        if (readTags)
        {
            List<IndependentTag> tags;
            if (!tagsOn.TryGetValue(element.Id.ToString(), out tags)) tags = new List<IndependentTag>();
            lines.Add(string.Format("  TAGS ({0}): tag id | view | tag type | head | leader", tags.Count));
            foreach (var tag in tags)
            {
                tagsFound++;
                var view = "";
                try { view = nameOf(doc.GetElement(tag.OwnerViewId)); } catch (Exception) { }
                var tagType = doc.GetElement(tag.GetTypeId()) as ElementType;
                var head = "?";
                try { head = pointMm(tag.TagHeadPosition) + " mm"; } catch (Exception) { }
                var leader = "?";
                try
                {
                    leader = tag.HasLeader ? "leader yes" : "leader no";
                    if (tag.HasLeader)
                    {
                        var end = leaderEndOf(tag);
                        if (end != null) leader += ", free end " + pointMm(end) + " mm";
                    }
                }
                catch (Exception) { }
                lines.Add(string.Format("    {0} | {1} (view id {2}) | {3} | {4} | {5}",
                    tag.Id, view, tag.OwnerViewId,
                    tagType == null ? "?" : typeLabelOf(tagType), head, leader));
            }
        }

        blocks.Add(lines);
    }

    // ---- the header, then the blocks until the budget is reached ----

    var budget = maxChars;
    var budgetNote = "";
    if (budget < leastBudget)
    {
        budgetNote = string.Format(" (maxChars {0} raised to the least, {1})", maxChars, leastBudget);
        budget = leastBudget;
    }

    var header = new List<string>();
    var notRecorded = targets.Count - recording.Count;
    header.Add(string.Format("PARAMETER RECORD: {0} element(s){1} | read in '{2}'{3} | from {4}",
        recording.Count,
        notRecorded <= 0 ? ""
            : sampleOnly ? string.Format(" - sampled, 1 of {0}; pass sampleOnly off for up to {1}",
                                         targets.Count, recordCap)
            : string.Format(" - FIRST {0} OF {1}; the {2} after them are not recorded, the next being "
                            + "id {3} - ask for them by id", recordCap, targets.Count, notRecorded,
                            targets[recording.Count].Id),
        titleOf(readIn),
        throughLink == null ? ""
            : string.Format(" through the link '{0}' (id {1}{2}) - every id here is the LINK's own, and "
                            + "locations are the link's own coordinates with this model's beside them",
                            nameOf(throughLink), throughLink.Id,
                            placements > 1 ? ", the first of " + placements + " placements" : ""),
        source));
    header.Add(string.Format("type parameters {0} | tags {1} | worksets {2} | parameters {3} | "
        + "budget {4} characters{5}",
        includeTypeParameters ? "included" : "left out",
        !includeTags ? "left out"
            : throughLink != null ? "not read - a host tag on a linked element is not a local id"
            : tagsFound + " found",
        anyWorkshared ? "named" : "not shown - not a workshared model",
        filterWords.Count == 0 ? "every one" : "only names containing '" + string.Join("', '", filterWords) + "'",
        budget, budgetNote));
    header.Add("COLUMNS: name | storage | kind | read-only | GUID | built-in name | value as Revit shows "
        + "it | raw - as Revit stores it, in INTERNAL UNITS (feet, square feet, cubic feet, radians), "
        + "never converted. '-' where there is none. Sorted by name, ignoring case. Points, lengths "
        + "and boxes in mm. Same-named parameters are told apart by GUID - compare by (name, GUID), "
        + "never by name alone");
    header.Add(sameNameTwice.Count == 0
        ? "No name is carried twice on any element or type recorded."
        : sameNameTwice.Count + " name(s) carried twice - named on each element's own block.");
    if (missing.Count > 0)
    {
        header.Add(string.Format("IN missing - {0}: {1}", missingWhere,
            string.Join(", ", missing.Take(20)) + (missing.Count > 20 ? ", and " + (missing.Count - 20) + " more" : "")));
    }
    if (linkProblem != null) header.Add(linkProblem);

    var written = new System.Text.StringBuilder();
    foreach (var line in header)
    {
        if (written.Length > 0) written.Append('\n');
        written.Append(line);
    }

    // The cut line is held back room for: at most one id per element left out,
    // so it never pushes the record past the budget it reports.
    var reserve = 400 + 16 * recording.Count;
    var limit = budget - reserve;
    var whole = 0;
    var cutAt = -1;
    var cutLines = 0;
    var totalChars = written.Length;
    foreach (var block in blocks) foreach (var line in block) totalChars += line.Length + 1;

    for (var b = 0; b < blocks.Count && cutAt < 0; b++)
    {
        var linesWritten = 0;
        foreach (var line in blocks[b])
        {
            if (written.Length + 1 + line.Length > limit) { cutAt = b; cutLines = linesWritten; break; }
            written.Append('\n').Append(line);
            linesWritten++;
        }
        if (cutAt < 0) whole++;
    }

    if (cutAt >= 0)
    {
        var notWritten = new List<string>();
        for (var b = cutAt + 1; b < recording.Count; b++) notWritten.Add(recording[b].Id.ToString());
        written.Append('\n').Append(string.Format(
            "RECORD CUT AT THE {0}-CHARACTER BUDGET: {1} of {2} element(s) written whole; element id {3} "
            + "cut after {4} of its {5} lines; not written: {6}. The whole record is about {7} characters - "
            + "ask again for the rest by id, with a parameterFilter, or with maxChars above {0}",
            budget, whole, recording.Count, recording[cutAt].Id, cutLines, blocks[cutAt].Count,
            notWritten.Count == 0 ? "nothing after it" : "ids " + string.Join(", ", notWritten),
            totalChars));
        findings.Add(string.Format("The record was CUT at {0} characters: {1} of {2} element(s) whole - "
            + "its last line names what was not written", budget, whole, recording.Count));
    }

    record = written.ToString();
}
else
{
    // sameNameTwice is still owed without the record: one pass over what the
    // inventory read, cheap beside the read itself.
    var counted = 0;
    var typesCounted = new HashSet<string>();
    foreach (var element in targets)
    {
        if (sampleOnly && counted >= 1) break;
        counted++;
        pairsOf(element, "instance");
        if (!includeTypeParameters) continue;
        var type = element.Document.GetElement(element.GetTypeId()) as ElementType;
        if (type != null && typesCounted.Add(type.Id.ToString())) pairsOf(type, "type");
    }
}

if (sameNameTwice.Count > 0)
{
    findings.Insert(0, "A NAME CARRIED TWICE - read, copy and compare these by GUID, never by name: "
        + string.Join("; ", sameNameTwice));
}

findings.Insert(0, string.Format("{0} parameter(s): {1} on the instance, {2} on the type. Read from "
    + "{3} of {4} element(s) in '{5}'{6} - {7}. The parameter GROUP is not reported - that member is "
    + "gone by Revit 2027 and its replacement never existed on 2020{8}",
    parameterNames.Count, instanceCount, typeCount,
    sampleOnly ? Math.Min(1, targets.Count) : elementsRead, targets.Count, titleOf(readIn),
    sampleOnly && targets.Count > 1 ? " (sampled - pass sampleOnly off to union across a mixed set)" : "",
    source,
    fullRecord ? "" : ". Pass fullRecord true for every value with its GUID, raw value, level, "
        + "workset, location and tags, as one string - up to " + recordCap + " elements"));
