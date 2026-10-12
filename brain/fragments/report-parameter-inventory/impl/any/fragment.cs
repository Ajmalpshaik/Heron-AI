// NOT STANDALONE. Assumes `doc`, `elements`, `sampleOnly`,
// `includeTypeParameters`, `fullRecord` and `includeTags` are in scope; leaves
// `findings`, `parameterNames`, `instanceCount`, `typeCount`, `elementsRead`,
// `record`, `sameNameTwice` and `tagsFound` behind.
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
// AsString is the fallback for text; the raw number is never invented.
//
// SAMPLING ONE ELEMENT IS THE DEFAULT. One element is representative of its
// type and answers fast; `sampleOnly` off unions across a mixed set, which is
// how variation between different families is found.
//
// A PARAMETER IS KNOWN BY WHAT IT IS, NEVER BY ITS NAME ALONE (version 2). A
// project can carry two shared parameters with the same name - seen 2026-10-12
// on Revit 2020, two called "Equipment Tag", GUID e133774c... filled and GUID
// cb0704fc... empty. Version 1 kept one line per NAME, so the second was
// dropped without a word and a by-name read showed the empty one. Each line is
// now keyed by the parameter's own id and name, and a shared one carries its
// GUID, so both are listed and told apart.
//
// THE RECORD IS ONE STRING ON PURPOSE. A Heron reply cuts a list to its first
// three items of 60 characters (RevitFragment.Describe), so `findings` alone
// cannot carry a full inventory to the chat. `record` is filled only when
// `fullRecord` is given true, because it is long: every parameter of every
// element read, plus where each stands and every tag on it. NOT CAPPED.

var findings = new List<string>();
var parameterNames = new List<string>();
var instanceCount = 0;
var typeCount = 0;
var elementsRead = 0;
var record = "";
var sameNameTwice = new List<string>();
var tagsFound = 0;

var seen = new HashSet<string>();
var mm = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<Parameter, string> kindOf = p =>
{
    var internalDef = p.Definition as InternalDefinition;
    if (internalDef != null && internalDef.BuiltInParameter != BuiltInParameter.INVALID)
        return "built-in";
    if (p.IsShared) return "shared";
    return "project/family (not shared)";
};

// The GUID of a shared parameter, empty for any other kind. Parameter.GUID
// throws on a parameter that is not shared, so IsShared is asked first.
Func<Parameter, string> guidOf = p =>
{
    try { return p.IsShared ? p.GUID.ToString() : ""; }
    catch (Exception) { return ""; }
};

Func<Parameter, string> valueOf = p =>
{
    try
    {
        if (!p.HasValue) return "<blank>";
        var shown = p.AsValueString();
        if (!string.IsNullOrEmpty(shown)) return shown;
        if (p.StorageType == StorageType.String) return p.AsString() ?? "<blank>";
        if (p.StorageType == StorageType.Integer) return p.AsInteger().ToString();
        if (p.StorageType == StorageType.ElementId)
        {
            var target = doc.GetElement(p.AsElementId());
            return target == null ? "<none>" : target.Name;
        }
        return "<no readable value>";
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

Func<XYZ, string> pointMm = point => point == null ? "?" : string.Format(invariant,
    "({0:0.#}, {1:0.#}, {2:0.#})", point.X * mm, point.Y * mm, point.Z * mm);

Func<XYZ, string> direction = vector => vector == null ? "?" : string.Format(invariant,
    "({0:0.###}, {1:0.###}, {2:0.###})", vector.X, vector.Y, vector.Z);

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
    return family.Length > 0 ? family + ": " + type.Name : type.Name;
};

Action<Element, string> readOne = null;
readOne = (element, whose) =>
{
    if (element == null) return;
    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter == null || parameter.Definition == null) continue;

        var name = parameter.Definition.Name;
        var key = whose + "|" + identityOf(parameter);
        if (!seen.Add(key)) continue;

        parameterNames.Add(name);
        if (whose == "instance") instanceCount++; else typeCount++;

        var guid = guidOf(parameter);
        findings.Add(string.Format("  {0,-40} {1,-9} {2,-28} {3}{4}{5}",
            name, parameter.StorageType, kindOf(parameter), valueOf(parameter),
            parameter.IsReadOnly ? "   [read-only]" : "",
            guid.Length > 0 ? "   [GUID " + guid + "]" : ""));
    }
};

// Every parameter on one element or type as record lines, and any name it
// carries more than once named in `sameNameTwice`.
Func<Element, string, List<string>> recordParameters = (element, whose) =>
{
    var lines = new List<string>();
    var byName = new Dictionary<string, List<string>>();
    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter == null || parameter.Definition == null) continue;
        var name = parameter.Definition.Name;
        var guid = guidOf(parameter);
        lines.Add(string.Format("    {0} | {1} | {2} | {3} | {4} | {5}",
            name, parameter.StorageType, kindOf(parameter),
            parameter.IsReadOnly ? "read-only" : "writable",
            guid.Length > 0 ? "GUID " + guid : "no GUID",
            valueOf(parameter)));

        List<string> guids;
        if (!byName.TryGetValue(name, out guids)) { guids = new List<string>(); byName[name] = guids; }
        guids.Add(guid.Length > 0 ? guid : kindOf(parameter));
    }
    // A name counts only when at least one of its parameters is not built in.
    // Revit itself carries a few built-in pairs on every element - two Levels,
    // two Categories - and listing those on each of 535 elements, measured
    // 2026-10-12, buried the one pair that mattered. A by-name read of a
    // built-in pair is a different question from carrying a value across.
    foreach (var pair in byName)
    {
        if (pair.Value.Count < 2 || pair.Value.TrueForAll(g => g == "built-in")) continue;
        sameNameTwice.Add(string.Format("{0} id {1}: '{2}' x{3} ({4})",
            whose, element.Id, pair.Key, pair.Value.Count, string.Join(", ", pair.Value)));
    }
    return lines;
};

foreach (var element in elements)
{
    if (element == null) continue;

    elementsRead++;
    if (elementsRead == 1 || !sampleOnly)
    {
        if (elementsRead == 1)
        {
            findings.Add(string.Format("INSTANCE parameters, from '{0}' (id {1}):",
                element.Name, element.Id));
        }
        readOne(element, "instance");
    }

    if (sampleOnly && elementsRead >= 1) break;
}

if (includeTypeParameters && elements.Count > 0)
{
    var first = elements[0];
    var type = first == null ? null : doc.GetElement(first.GetTypeId()) as ElementType;
    if (type == null)
    {
        findings.Add("TYPE parameters: this element has no type - a line, a group or a view does "
            + "not. That is not a fault");
    }
    else
    {
        findings.Add(string.Format("TYPE parameters, from '{0}: {1}':", type.FamilyName, type.Name));
        readOne(type, "type");

        if (!sampleOnly)
        {
            var seenTypes = new HashSet<ElementId>();
            seenTypes.Add(type.Id);
            foreach (var element in elements)
            {
                if (element == null) continue;
                var other = doc.GetElement(element.GetTypeId()) as ElementType;
                if (other == null || !seenTypes.Add(other.Id)) continue;
                readOne(other, "type");
            }
        }
    }
}

if (parameterNames.Count == 0)
{
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

if (fullRecord)
{
    var tagsOn = new Dictionary<string, List<IndependentTag>>();
    if (includeTags)
    {
        foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)))
        {
            var tag = candidate as IndependentTag;
            if (tag == null) continue;

            var targets = new List<ElementId>();
            try
            {
                var many = tag.GetType().GetMethod("GetTaggedLocalElementIds", Type.EmptyTypes);
                if (many != null)
                {
                    var all = many.Invoke(tag, null) as IEnumerable<ElementId>;
                    if (all != null) targets.AddRange(all);
                }
                else
                {
                    var single = tag.GetType().GetProperty("TaggedLocalElementId");
                    var one = single == null ? null : single.GetValue(tag, null) as ElementId;
                    if (one != null) targets.Add(one);
                }
            }
            catch (Exception) { }

            foreach (var id in targets)
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

    var lines = new List<string>();
    var typesWritten = new HashSet<string>();
    var recorded = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;
        if (sampleOnly && recorded >= 1) break;
        recorded++;

        var category = element.Category == null ? "no category" : element.Category.Name;
        var type = doc.GetElement(element.GetTypeId()) as ElementType;
        lines.Add(string.Format("ELEMENT id {0} | {1} | {2} | {3}",
            element.Id, category, type == null ? nameOf(element) : typeLabelOf(type),
            element.GetType().Name));

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
        }
        else if (curve != null && curve.Curve != null && curve.Curve.IsBound)
        {
            lines.Add("  location curve " + pointMm(curve.Curve.GetEndPoint(0)) + " -> "
                + pointMm(curve.Curve.GetEndPoint(1)) + " mm");
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

        // Every instance parameter.
        var instanceLines = recordParameters(element, "instance");
        lines.Add(string.Format("  INSTANCE parameters ({0}): name | storage | kind | read-only | GUID | value",
            instanceLines.Count));
        lines.AddRange(instanceLines);

        // Every type parameter, once per type.
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
                var typeLines = recordParameters(type, "type");
                lines.Add(string.Format("  TYPE '{0}' (id {1}) parameters ({2}):",
                    typeLabelOf(type), type.Id, typeLines.Count));
                lines.AddRange(typeLines);
            }
        }

        // Every tag on it.
        if (includeTags)
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
    }

    lines.Insert(0, string.Format("PARAMETER RECORD: {0} element(s){1}, type parameters {2}, tags {3}. "
        + "Parameters with the same name are told apart by GUID; compare by (name, GUID), never by "
        + "name alone. {4}",
        recorded, sampleOnly ? " (sampled - pass sampleOnly off for every element)" : "",
        includeTypeParameters ? "included" : "left out",
        includeTags ? tagsFound + " found" : "left out",
        sameNameTwice.Count == 0
            ? "No name is carried twice on any element or type read."
            : "A NAME CARRIED TWICE: " + string.Join("; ", sameNameTwice)));
    record = string.Join("\n", lines);
}
else
{
    // sameNameTwice is still owed without the record: one pass over what the
    // inventory read, cheap beside the read itself.
    var counted = 0;
    var typesCounted = new HashSet<string>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (sampleOnly && counted >= 1) break;
        counted++;
        recordParameters(element, "instance");
        if (!includeTypeParameters) continue;
        var type = doc.GetElement(element.GetTypeId()) as ElementType;
        if (type != null && typesCounted.Add(type.Id.ToString())) recordParameters(type, "type");
    }
}

if (sameNameTwice.Count > 0)
{
    findings.Insert(0, "A NAME CARRIED TWICE - read, copy and compare these by GUID, never by name: "
        + string.Join("; ", sameNameTwice));
}

findings.Insert(0, string.Format("{0} parameter(s): {1} on the instance, {2} on the type. Read from "
    + "{3} of {4} element(s){5}. The parameter GROUP is not reported - that member is gone by Revit "
    + "2027 and its replacement never existed on 2020{6}",
    parameterNames.Count, instanceCount, typeCount,
    sampleOnly ? 1 : elementsRead, elements.Count,
    sampleOnly ? " (sampled - pass sampleOnly off to union across a mixed set)" : "",
    fullRecord ? "" : ". Pass fullRecord true for every value with its GUID, the location and the "
        + "tags, as one string"));
