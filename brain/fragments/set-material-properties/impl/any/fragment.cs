// NOT STANDALONE. Assumes `doc`, `materialsNamed` and `fieldValues` are in
// scope; leaves `changedMaterialIds`, `materialReport`, `fieldsChanged`,
// `alreadyAsAsked`, `sharedAssets`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE MATERIAL BROWSER'S IDENTITY, GRAPHICS, PHYSICAL AND THERMAL TABS, AS
// "Field=Value" PAIRS. One call writes any of them on one material or on every
// material a name pattern matches - "TRG_*" - which is the bulk change by name.
// The Appearance tab is SET_MATERIAL_APPEARANCE's: an appearance asset is
// edited through its own edit scope, and that is a different job.
//
// MATERIALS ARE NAMED, NEVER GUESSED. Each name in `materialsNamed` (`;`
// between, because a material's name may hold a comma) is the exact name, the
// same name in another case when only one material has it, or a pattern with
// `*` and `?`. A name matching nothing refuses the whole call, naming the
// closest the document holds; nothing is ever made here - CREATE_MATERIAL
// and CREATE_STARTER_MATERIALS make materials.
//
// THE FIELDS, by the words the Material Browser shows them with:
//   Identity  Name (one material only), Description, Class, Comments,
//             Keywords, Manufacturer, Model, Cost, URL, Keynote, Mark
//   Graphics  Shading Colour, Transparency (0 to 100), Use Render Appearance
//             (yes / no), and Surface / Cut Foreground / Background Pattern
//             and Colour. "Surface Pattern" and "Cut Pattern" - the names a
//             material had before Revit 2019 split each into a foreground and
//             a background - mean the foreground. Every release Heron builds
//             for, 2020 to 2027, has all four.
//   Physical  "Physical Asset=<name>" gives the material a physical asset
//             the document already holds, "none" takes it off;
//             "Physical: Density=7850" writes one of its values.
//   Thermal   the same, "Thermal Asset=" and "Thermal: Thermal Conductivity=".
//
// AN IDENTITY FIELD IS FOUND BY REVIT'S OWN PARAMETER FIRST, then by the
// field's name as typed. Keywords has no parameter of Revit's own on any
// release (BuiltInParameter read off the shipped assemblies 2026-10-20), so
// it is found by its name or refused saying so - NEEDS-CHECKING Group IA.
//
// AN ASSET VALUE IS WRITTEN IN THE PROJECT'S OWN UNITS, through Revit's own
// parameter of that name on the asset (SetValueString) - the units LIST_MATERIALS
// prints them in, so no unit is converted here and none can be converted
// wrongly. The name is Revit's, in the language it runs in.
//
// AN ASSET SHARED WITH ANOTHER MATERIAL IS NEVER EDITED. A value written into
// it would change every material that shares it, so this material is given
// its own copy first, named after it, and the answer names who shared it. A
// material with no asset at all is given a new one when a value is asked of it.
//
// NOTHING IS WRITTEN UNTIL EVERY NAME AND VALUE HAS BEEN CHECKED, so a refusal
// leaves every material as it was. Then everything is written, the document
// regenerated once, and every field read back: one that does not read as asked
// fails the call and nothing from it is kept.
//
// IN A PROJECT OR A FAMILY - whichever document Heron is handed. A family's
// materials are its own.

var changedMaterialIds = new List<ElementId>();
var materialReport = "";
var fieldsChanged = 0;
var alreadyAsAsked = 0;
var sharedAssets = "";
var refused = "";
var findings = new List<string>();

{
    var problems = new List<string>();
    var all = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>()
        .Where(each => each != null).ToList();

    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    Func<string, HashSet<string>> wordsOf = text => new HashSet<string>(
        System.Text.RegularExpressions.Regex.Split((text ?? "").ToLowerInvariant(), "[^a-z0-9]+")
            .Where(word => word.Length > 1));
    Func<string, IEnumerable<string>, string> closest = (said, names) =>
    {
        var asked = wordsOf(said);
        var bare = (said ?? "").Replace("*", "").Replace("?", "").Trim();
        var near = names
            .Select(name => new { name, score = wordsOf(name).Count(word => asked.Contains(word)) })
            .Where(each => each.score > 0 || (bare.Length > 2
                && each.name.IndexOf(bare, StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderByDescending(each => each.score)
            .ThenBy(each => each.name, StringComparer.OrdinalIgnoreCase)
            .Take(8).Select(each => "'" + each.name + "'").ToList();
        return near.Count == 0 ? "nothing close" : string.Join(", ", near);
    };
    Func<Color, string> colourText = colour =>
        colour == null || !colour.IsValid ? "none" : colour.Red + "," + colour.Green + "," + colour.Blue;
    Func<string, Color> colourOf = text =>
    {
        var said = (text ?? "").Trim();
        byte r, g, b;
        if (said.StartsWith("#") && said.Length == 7)
        {
            try
            {
                return new Color(Convert.ToByte(said.Substring(1, 2), 16),
                    Convert.ToByte(said.Substring(3, 2), 16), Convert.ToByte(said.Substring(5, 2), 16));
            }
            catch (Exception) { return null; }
        }
        var parts = said.Split(new[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 && byte.TryParse(parts[0], out r) && byte.TryParse(parts[1], out g)
            && byte.TryParse(parts[2], out b)) return new Color(r, g, b);
        return null;
    };
    Func<string, bool?> yesNo = text =>
    {
        var word = squash(text);
        if (word == "yes" || word == "true" || word == "on" || word == "1" || word == "ticked") return true;
        if (word == "no" || word == "false" || word == "off" || word == "0" || word == "unticked") return false;
        return null;
    };

    // ---- 1. Which materials.
    var targets = new List<Material>();
    var namedText = (materialsNamed ?? "").Trim();
    if (namedText.Length == 0) problems.Add("no material was named - give a name, several with ';' between, or a pattern such as 'TRG_*'");
    foreach (var token in namedText.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var said = token.Trim();
        if (said.Length == 0) continue;
        List<Material> hits;
        if (said.IndexOf('*') >= 0 || said.IndexOf('?') >= 0)
        {
            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(said)
                .Replace("\\*", ".*").Replace("\\?", ".") + "$";
            var match = new System.Text.RegularExpressions.Regex(pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            hits = all.Where(each => match.IsMatch(each.Name)).ToList();
        }
        else
        {
            hits = all.Where(each => each.Name == said).ToList();
            if (hits.Count == 0)
            {
                hits = all.Where(each => string.Equals(each.Name, said, StringComparison.OrdinalIgnoreCase)).ToList();
                if (hits.Count > 1)
                {
                    problems.Add(string.Format("'{0}' matches {1} materials when case is ignored ({2}) and none exactly - type it as Revit spells it",
                        said, hits.Count, string.Join(", ", hits.Select(each => "'" + each.Name + "'"))));
                    continue;
                }
            }
        }
        if (hits.Count == 0)
        {
            problems.Add(string.Format("no material matches '{0}' in this {1} - closest: {2}. LIST_MATERIALS names every one",
                said, doc.IsFamilyDocument ? "family" : "project", closest(said, all.Select(each => each.Name))));
            continue;
        }
        foreach (var hit in hits)
            if (!targets.Any(each => each.Id == hit.Id)) targets.Add(hit);
    }

    // ---- 2. Which fields. Field, the value as typed, and the asset value's
    // own name for "Physical: X" and "Thermal: X".
    var fieldOf = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "name", "Name" }, { "rename", "Name" },
        { "description", "Description" }, { "class", "Class" }, { "materialclass", "Class" },
        { "comments", "Comments" }, { "comment", "Comments" },
        { "keywords", "Keywords" }, { "keyword", "Keywords" },
        { "manufacturer", "Manufacturer" }, { "model", "Model" }, { "cost", "Cost" },
        { "url", "URL" }, { "keynote", "Keynote" }, { "mark", "Mark" },
        { "shadingcolour", "Shading Colour" }, { "shadingcolor", "Shading Colour" },
        { "colour", "Shading Colour" }, { "color", "Shading Colour" },
        { "transparency", "Transparency" }, { "shadingtransparency", "Transparency" },
        { "userenderappearance", "Use Render Appearance" },
        { "surfacepattern", "Surface Foreground Pattern" }, { "surfaceforegroundpattern", "Surface Foreground Pattern" },
        { "surfacepatterncolour", "Surface Foreground Colour" }, { "surfacepatterncolor", "Surface Foreground Colour" },
        { "surfaceforegroundcolour", "Surface Foreground Colour" }, { "surfaceforegroundcolor", "Surface Foreground Colour" },
        { "surfacebackgroundpattern", "Surface Background Pattern" },
        { "surfacebackgroundcolour", "Surface Background Colour" }, { "surfacebackgroundcolor", "Surface Background Colour" },
        { "cutpattern", "Cut Foreground Pattern" }, { "cutforegroundpattern", "Cut Foreground Pattern" },
        { "cutpatterncolour", "Cut Foreground Colour" }, { "cutpatterncolor", "Cut Foreground Colour" },
        { "cutforegroundcolour", "Cut Foreground Colour" }, { "cutforegroundcolor", "Cut Foreground Colour" },
        { "cutbackgroundpattern", "Cut Background Pattern" },
        { "cutbackgroundcolour", "Cut Background Colour" }, { "cutbackgroundcolor", "Cut Background Colour" },
        { "physicalasset", "Physical Asset" }, { "structuralasset", "Physical Asset" },
        { "thermalasset", "Thermal Asset" },
    };
    var asked = new List<Tuple<string, string, string>>();
    foreach (var piece in (fieldValues ?? "").Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var text = piece.Trim();
        if (text.Length == 0) continue;
        var at = text.IndexOf('=');
        if (at <= 0)
        {
            problems.Add("'" + text + "' is not Field=Value");
            continue;
        }
        var key = text.Substring(0, at).Trim();
        var value = text.Substring(at + 1).Trim();
        var colon = key.IndexOf(':');
        if (colon > 0)
        {
            var side = squash(key.Substring(0, colon));
            var own = key.Substring(colon + 1).Trim();
            if ((side == "physical" || side == "structural" || side == "thermal") && own.Length > 0)
            {
                asked.Add(Tuple.Create(side == "thermal" ? "Thermal:" : "Physical:", value, own));
                continue;
            }
        }
        string field;
        if (!fieldOf.TryGetValue(squash(key), out field))
        {
            problems.Add("'" + key + "' is not a field this sets - Identity: Name, Description, Class, Comments, Keywords, "
                + "Manufacturer, Model, Cost, URL, Keynote, Mark; Graphics: Shading Colour, Transparency, Use Render "
                + "Appearance, Surface / Cut Foreground / Background Pattern and Colour; Physical Asset, Thermal Asset, "
                + "'Physical: <value name>', 'Thermal: <value name>'. The Appearance tab is SET_MATERIAL_APPEARANCE");
            continue;
        }
        if (asked.Any(each => each.Item1 == field))
        {
            problems.Add("'" + field + "' is given twice");
            continue;
        }
        asked.Add(Tuple.Create(field, value, (string)null));
    }
    if (asked.Count == 0 && problems.Count == 0) problems.Add("no Field=Value was given - nothing to set");
    // WHICH ASSET BEFORE ITS VALUES: "Physical: Density=7850; Physical Asset=Steel"
    // would otherwise write 7850 into one asset and then point the material at
    // another. The order of everything else is kept.
    asked = asked.Where(each => each.Item1 == "Physical Asset" || each.Item1 == "Thermal Asset")
        .Concat(asked.Where(each => each.Item1 != "Physical Asset" && each.Item1 != "Thermal Asset")).ToList();

    // ---- 3. Each value checked once, before any material is touched.
    var patterns = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement))
        .Cast<FillPatternElement>().ToList();
    var sets = new FilteredElementCollector(doc).OfClass(typeof(PropertySetElement))
        .Cast<PropertySetElement>().ToList();
    var patternIds = new Dictionary<string, ElementId>(StringComparer.Ordinal);
    var colours = new Dictionary<string, Color>(StringComparer.Ordinal);
    var assetIds = new Dictionary<string, ElementId>(StringComparer.Ordinal);
    foreach (var item in asked)
    {
        var field = item.Item1;
        var value = item.Item2;
        if (field == "Name")
        {
            if (targets.Count != 1)
                problems.Add("Name renames ONE material, and " + targets.Count + " were named");
            else if (value.Length == 0 || value.IndexOfAny("{}[]|;<>:?\\`~".ToCharArray()) >= 0)
                problems.Add("'" + value + "' cannot be a material's name - blank, or a character Revit refuses ({}[]|;<>:?\\`~)");
            else if (!string.Equals(targets[0].Name, value, StringComparison.Ordinal)
                && all.Any(each => each.Id != targets[0].Id && string.Equals(each.Name, value, StringComparison.OrdinalIgnoreCase)))
                problems.Add("a material is already called '" + value + "'");
        }
        else if (field == "Shading Colour" || field.EndsWith(" Colour"))
        {
            var colour = colourOf(value);
            if (colour == null) problems.Add(field + " '" + value + "' is not a colour - three numbers 0 to 255, '200,120,60', or '#C8783C'");
            else colours[field] = colour;
        }
        else if (field == "Transparency")
        {
            int amount;
            if (!int.TryParse(value.TrimEnd('%').Trim(), out amount) || amount < 0 || amount > 100)
                problems.Add("Transparency '" + value + "' is not a whole number 0 to 100");
        }
        else if (field == "Use Render Appearance")
        {
            if (yesNo(value) == null) problems.Add("Use Render Appearance '" + value + "' is not yes or no");
        }
        else if (field.EndsWith(" Pattern"))
        {
            var word = squash(value);
            if (word == "none" || word.Length == 0) { patternIds[field] = ElementId.InvalidElementId; continue; }
            var cut = field.StartsWith("Cut");
            var hits = patterns.Where(each => string.Equals(each.Name, value, StringComparison.OrdinalIgnoreCase)).ToList();
            if (hits.Count == 0 && (word == "solidfill" || word == "solid"))
                hits = patterns.Where(each => { var fill = each.GetFillPattern(); return fill != null && fill.IsSolidFill; }).ToList();
            // A CUT PATTERN MUST BE A DRAFTING ONE - Revit's own rule for both cut
            // fields, which throws on a model pattern - so a model pattern of
            // that name is refused here, by name, rather than by Revit mid-write.
            // A surface pattern may be either; when a drafting and a model
            // pattern share a name, the model one is taken and the answer gives
            // its id.
            if (cut)
            {
                var drafting = hits.Where(each => each.GetFillPattern() != null && each.GetFillPattern().Target == FillPatternTarget.Drafting).ToList();
                if (hits.Count > 0 && drafting.Count == 0)
                {
                    problems.Add(field + ": '" + value + "' is a model pattern, and a cut pattern must be a drafting one - closest drafting: "
                        + closest(value, patterns.Where(each => each.GetFillPattern() != null
                            && each.GetFillPattern().Target == FillPatternTarget.Drafting).Select(each => each.Name)));
                    continue;
                }
                hits = drafting;
            }
            else if (hits.Count > 1)
            {
                var preferred = hits.Where(each => each.GetFillPattern() != null && each.GetFillPattern().Target == FillPatternTarget.Model).ToList();
                if (preferred.Count > 0) hits = preferred;
            }
            if (hits.Count == 0)
                problems.Add(field + ": no fill pattern is called '" + value + "' - closest: "
                    + closest(value, patterns.Select(each => each.Name)) + ". '<Solid fill>' is solid, 'none' clears it; CREATE_FILL_PATTERN makes one");
            else patternIds[field] = hits[0].Id;
        }
        else if (field == "Physical Asset" || field == "Thermal Asset")
        {
            var word = squash(value);
            if (word == "none" || word.Length == 0) { assetIds[field] = ElementId.InvalidElementId; continue; }
            var physical = field == "Physical Asset";
            var kind = sets.Where(each =>
            {
                try { return physical ? each.GetStructuralAsset() != null : each.GetThermalAsset() != null; }
                catch (Exception) { return false; }
            }).ToList();
            var hits = kind.Where(each => string.Equals(each.Name, value, StringComparison.OrdinalIgnoreCase)).ToList();
            if (hits.Count == 1) assetIds[field] = hits[0].Id;
            else if (hits.Count > 1) problems.Add(field + ": " + hits.Count + " assets are called '" + value + "' - which is meant cannot be told");
            else problems.Add(field + ": this document holds no " + (physical ? "physical" : "thermal") + " asset called '" + value
                + "' - closest: " + closest(value, kind.Select(each => each.Name))
                + ". Revit's own asset library is not reachable from here for physical and thermal assets, only for appearance");
        }
    }

    if (problems.Count > 0)
    {
        refused = string.Join("; ", problems) + ". Nothing was changed";
        findings.Add(refused);
    }
    else
    {
        // ---- 4. Write. Each plan: material, field, read now, write.
        var identityParameters = new Dictionary<string, BuiltInParameter[]>(StringComparer.Ordinal)
        {
            { "Description", new[] { BuiltInParameter.ALL_MODEL_DESCRIPTION } },
            { "Comments", new[] { BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, BuiltInParameter.ALL_MODEL_TYPE_COMMENTS } },
            { "Keywords", new BuiltInParameter[0] },
            { "Manufacturer", new[] { BuiltInParameter.ALL_MODEL_MANUFACTURER } },
            { "Model", new[] { BuiltInParameter.ALL_MODEL_MODEL } },
            { "Cost", new[] { BuiltInParameter.ALL_MODEL_COST } },
            { "URL", new[] { BuiltInParameter.ALL_MODEL_URL } },
            { "Keynote", new[] { BuiltInParameter.KEYNOTE_PARAM } },
            { "Mark", new[] { BuiltInParameter.ALL_MODEL_MARK } },
        };
        Func<Material, string, Parameter> identityParameter = (material, field) =>
        {
            foreach (var builtIn in identityParameters[field])
            {
                var found = material.get_Parameter(builtIn);
                if (found != null) return found;
            }
            return material.LookupParameter(field);
        };
        Func<Parameter, string> parameterText = p =>
        {
            if (p == null || !p.HasValue) return "";
            if (p.StorageType == StorageType.String) return p.AsString() ?? "";
            return p.AsValueString() ?? "";
        };
        Func<ElementId, string> elementName = id =>
        {
            if (id == null || id == ElementId.InvalidElementId) return "none";
            var element = doc.GetElement(id);
            return element == null ? "none" : element.Name;
        };
        // A pattern by its name AND id: a drafting and a model pattern can
        // share a name, and the name alone would read one as the other.
        Func<ElementId, string> patternText = id =>
            id == null || id == ElementId.InvalidElementId || doc.GetElement(id) == null ? "none" : elementName(id) + " (id " + id + ")";

        // Physical and thermal asset names in use, so a copy gets a new one.
        var setNames = new HashSet<string>(sets.Select(each => each.Name), StringComparer.OrdinalIgnoreCase);
        Func<string, string> freeSetName = wanted =>
        {
            var name = wanted;
            for (var n = 2; setNames.Contains(name); n++) name = wanted + " " + n;
            setNames.Add(name);
            return name;
        };
        var shareNotes = new List<string>();
        var ownSets = new Dictionary<string, PropertySetElement>(StringComparer.Ordinal);
        // WHO SHARED WHAT, AS IT STOOD BEFORE THIS CALL. Counted live, the last
        // of several named materials sharing one asset would find it no longer
        // shared and edit it in place.
        var usersAtStart = new Dictionary<string, List<Material>>(StringComparer.Ordinal);
        foreach (var each in all)
            foreach (var id in new[] { each.StructuralAssetId, each.ThermalAssetId })
            {
                if (id == null || id == ElementId.InvalidElementId) continue;
                List<Material> users;
                if (!usersAtStart.TryGetValue(id.ToString(), out users)) usersAtStart[id.ToString()] = users = new List<Material>();
                users.Add(each);
            }
        var givenOwnAsset = new HashSet<string>(StringComparer.Ordinal);
        Func<Material, bool, PropertySetElement> ownSet = (material, physical) =>
        {
            var key = material.Id + (physical ? "/physical" : "/thermal");
            PropertySetElement own;
            if (ownSets.TryGetValue(key, out own)) return own;
            var aspect = physical ? MaterialAspect.Structural : MaterialAspect.Thermal;
            var id = physical ? material.StructuralAssetId : material.ThermalAssetId;
            var set = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id) as PropertySetElement;
            if (set == null)
            {
                var name = freeSetName(material.Name);
                set = physical
                    ? PropertySetElement.Create(doc, new StructuralAsset(name, StructuralAssetClass.Generic))
                    : PropertySetElement.Create(doc, new ThermalAsset(name, ThermalMaterialType.Solid));
                material.SetMaterialAspectByPropertySet(aspect, set.Id);
                givenOwnAsset.Add(material.Id.ToString());
                shareNotes.Add(string.Format("'{0}' had no {1} asset - given a new one, '{2}'",
                    material.Name, physical ? "physical" : "thermal", name));
            }
            else
            {
                List<Material> users;
                usersAtStart.TryGetValue(set.Id.ToString(), out users);
                var sharers = (users ?? new List<Material>()).Where(each => each.Id != material.Id).Select(each => each.Name).ToList();
                if (sharers.Count > 0)
                {
                    var name = freeSetName(material.Name);
                    var copy = set.Duplicate(doc, name);
                    material.SetMaterialAspectByPropertySet(aspect, copy.Id);
                    givenOwnAsset.Add(material.Id.ToString());
                    shareNotes.Add(string.Format("'{0}': its {1} asset '{2}' was shared with {3} - given its own copy, '{4}', and '{2}' was not touched",
                        material.Name, physical ? "physical" : "thermal", set.Name,
                        string.Join(", ", sharers.Take(6).Select(each => "'" + each + "'")) + (sharers.Count > 6 ? " and " + (sharers.Count - 6) + " more" : ""),
                        name));
                    set = copy;
                }
            }
            ownSets[key] = set;
            return set;
        };
        Func<PropertySetElement, string, Parameter> setParameter = (set, own) =>
        {
            var hits = new List<Parameter>();
            foreach (Parameter p in set.Parameters)
                if (p.Definition != null && string.Equals(p.Definition.Name, own, StringComparison.OrdinalIgnoreCase)) hits.Add(p);
            if (hits.Count == 1) return hits[0];
            var names = new List<string>();
            foreach (Parameter p in set.Parameters)
                if (p.Definition != null && !p.IsReadOnly) names.Add(p.Definition.Name);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            throw new InvalidOperationException(string.Format(
                "The asset '{0}' has {1} value called '{2}' - it has: {3}. Nothing from this call was kept",
                set.Name, hits.Count == 0 ? "no" : "more than one", own, string.Join(", ", names.Distinct())));
        };

        var before = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var written = new List<Tuple<Material, string, Func<string>, string>>();

        foreach (var material in targets)
        {
            var m = material;
            lines[m.Id.ToString()] = new List<string>();
            foreach (var item in asked)
            {
                var field = item.Item1;
                var value = item.Item2;
                var own = item.Item3;
                var label = own == null ? field : field + " " + own;
                Func<string> read;
                Action write;
                string wanted;

                if (field == "Name")
                {
                    read = () => m.Name;
                    write = () => m.Name = value;
                    wanted = value;
                }
                else if (field == "Class")
                {
                    // An existing class in another case is written as the
                    // document spells it - "metal" becomes "Metal", never a
                    // second class beside it.
                    var spelled = all.Select(each => each.MaterialClass ?? "")
                        .FirstOrDefault(each => string.Equals(each, value, StringComparison.OrdinalIgnoreCase)) ?? value;
                    read = () => m.MaterialClass ?? "";
                    write = () => m.MaterialClass = spelled;
                    wanted = spelled;
                }
                else if (identityParameters.ContainsKey(field))
                {
                    var p = identityParameter(m, field);
                    if (p == null)
                        throw new InvalidOperationException(string.Format(
                            "Revit offers no '{0}' on the material '{1}' - not as a parameter of its own and not by that name. Set it in the "
                            + "Material Browser's Identity tab; nothing from this call was kept", field, m.Name));
                    if (p.IsReadOnly)
                        throw new InvalidOperationException(string.Format(
                            "'{0}' on the material '{1}' is read-only. Nothing from this call was kept", field, m.Name));
                    read = () => parameterText(p);
                    write = () =>
                    {
                        bool ok;
                        if (p.StorageType == StorageType.String) ok = p.Set(value);
                        else if (p.StorageType == StorageType.Integer)
                        {
                            int whole;
                            ok = int.TryParse(value, out whole) && p.Set(whole);
                        }
                        else ok = p.SetValueString(value);
                        if (!ok)
                            throw new InvalidOperationException(string.Format(
                                "Revit would not take '{0}' for '{1}' on '{2}'. Nothing from this call was kept", value, field, m.Name));
                    };
                    wanted = p.StorageType == StorageType.String ? value : null;
                }
                else if (field == "Shading Colour")
                {
                    var colour = colours[field];
                    read = () => colourText(m.Color);
                    write = () => m.Color = colour;
                    wanted = colourText(colour);
                }
                else if (field == "Transparency")
                {
                    var amount = int.Parse(value.TrimEnd('%').Trim());
                    read = () => m.Transparency.ToString();
                    write = () => m.Transparency = amount;
                    wanted = amount.ToString();
                }
                else if (field == "Use Render Appearance")
                {
                    var on = yesNo(value) == true;
                    read = () => m.UseRenderAppearanceForShading ? "yes" : "no";
                    write = () => m.UseRenderAppearanceForShading = on;
                    wanted = on ? "yes" : "no";
                }
                else if (field.EndsWith(" Pattern"))
                {
                    var id = patternIds[field];
                    Func<ElementId> get;
                    Action<ElementId> put;
                    if (field == "Surface Foreground Pattern") { get = () => m.SurfaceForegroundPatternId; put = x => m.SurfaceForegroundPatternId = x; }
                    else if (field == "Surface Background Pattern") { get = () => m.SurfaceBackgroundPatternId; put = x => m.SurfaceBackgroundPatternId = x; }
                    else if (field == "Cut Foreground Pattern") { get = () => m.CutForegroundPatternId; put = x => m.CutForegroundPatternId = x; }
                    else { get = () => m.CutBackgroundPatternId; put = x => m.CutBackgroundPatternId = x; }
                    read = () => patternText(get());
                    write = () => put(id);
                    wanted = patternText(id);
                }
                else if (field.EndsWith(" Colour"))
                {
                    var colour = colours[field];
                    Func<Color> get;
                    Action<Color> put;
                    if (field == "Surface Foreground Colour") { get = () => m.SurfaceForegroundPatternColor; put = x => m.SurfaceForegroundPatternColor = x; }
                    else if (field == "Surface Background Colour") { get = () => m.SurfaceBackgroundPatternColor; put = x => m.SurfaceBackgroundPatternColor = x; }
                    else if (field == "Cut Foreground Colour") { get = () => m.CutForegroundPatternColor; put = x => m.CutForegroundPatternColor = x; }
                    else { get = () => m.CutBackgroundPatternColor; put = x => m.CutBackgroundPatternColor = x; }
                    read = () => colourText(get());
                    write = () => put(colour);
                    wanted = colourText(colour);
                }
                else if (field == "Physical Asset" || field == "Thermal Asset")
                {
                    var physical = field == "Physical Asset";
                    var id = assetIds[field];
                    read = () => elementName(physical ? m.StructuralAssetId : m.ThermalAssetId);
                    write = () =>
                    {
                        var aspect = physical ? MaterialAspect.Structural : MaterialAspect.Thermal;
                        if (id == ElementId.InvalidElementId) m.ClearMaterialAspect(aspect);
                        else m.SetMaterialAspectByPropertySet(aspect, id);
                    };
                    wanted = elementName(id);
                }
                else
                {
                    // "Physical: X" or "Thermal: X" - a value of the asset.
                    var physical = field == "Physical:";
                    read = () =>
                    {
                        var id = physical ? m.StructuralAssetId : m.ThermalAssetId;
                        var set = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id) as PropertySetElement;
                        if (set == null) return "no asset";
                        try { return parameterText(setParameter(set, own)); }
                        catch (InvalidOperationException) { return "not on its asset"; }
                    };
                    write = () =>
                    {
                        var p = setParameter(ownSet(m, physical), own);
                        if (p.IsReadOnly)
                            throw new InvalidOperationException(string.Format(
                                "'{0}' on the {1} asset of '{2}' is read-only. Nothing from this call was kept",
                                own, physical ? "physical" : "thermal", m.Name));
                        bool ok;
                        if (p.StorageType == StorageType.String) ok = p.Set(value);
                        else if (p.StorageType == StorageType.Integer)
                        {
                            int whole;
                            ok = int.TryParse(value, out whole) && p.Set(whole);
                        }
                        else ok = p.SetValueString(value);
                        if (!ok)
                            throw new InvalidOperationException(string.Format(
                                "Revit would not take '{0}' for '{1}' on the {2} asset of '{3}' - a number in the project's own units, as LIST_MATERIALS "
                                + "shows them. Nothing from this call was kept", value, own, physical ? "physical" : "thermal", m.Name));
                    };
                    wanted = null;
                }

                var was = read();
                before[m.Id + "|" + label] = was;
                if (wanted != null && string.Equals(was, wanted, StringComparison.Ordinal))
                {
                    alreadyAsAsked++;
                    lines[m.Id.ToString()].Add(label + " already '" + was + "'");
                    continue;
                }
                write();
                // A number Revit formats - a cost, an asset value - is held to
                // what Revit took just now, so a later change to it in this call
                // (another asset pointed at) fails the read-back.
                written.Add(Tuple.Create(m, label, read, wanted ?? read()));
            }
        }

        // ---- 5. One regeneration, then every field read back.
        doc.Regenerate();
        var wrong = new List<string>();
        foreach (var each in written)
        {
            var now = each.Item3();
            var was = before[each.Item1.Id + "|" + each.Item2];
            if (each.Item4 != null && !string.Equals(now, each.Item4, StringComparison.Ordinal))
            {
                wrong.Add(string.Format("'{0}' {1} reads '{2}', not '{3}'", each.Item1.Name, each.Item2, now, each.Item4));
                continue;
            }
            if (string.Equals(now, was, StringComparison.Ordinal))
            {
                alreadyAsAsked++;
                lines[each.Item1.Id.ToString()].Add(each.Item2 + " already '" + was + "'");
                continue;
            }
            fieldsChanged++;
            lines[each.Item1.Id.ToString()].Add(each.Item2 + " '" + was + "' -> '" + now + "'");
            if (!changedMaterialIds.Contains(each.Item1.Id)) changedMaterialIds.Add(each.Item1.Id);
        }
        if (wrong.Count > 0)
            throw new InvalidOperationException("Not every field read back as asked: " + string.Join("; ", wrong)
                + ". Nothing from this call was kept");
        // A material given an asset of its own has changed, even where the
        // value asked was already the shared asset's.
        foreach (var material in targets)
            if (givenOwnAsset.Contains(material.Id.ToString()) && !changedMaterialIds.Contains(material.Id))
                changedMaterialIds.Add(material.Id);

        materialReport = string.Join("  ||  ", targets.Select(each => each.Name + ": " + string.Join(", ", lines[each.Id.ToString()])));
        sharedAssets = string.Join("; ", shareNotes);

        findings.Add(string.Format("{0} field(s) changed on {1} of the {2} material(s) named; {3} already held the value asked",
            fieldsChanged, changedMaterialIds.Count, targets.Count, alreadyAsAsked));
        if (shareNotes.Count > 0) findings.Add("Shared assets were not edited: " + sharedAssets);
        if (asked.Any(each => each.Item1 == "Shading Colour" || each.Item1 == "Transparency"))
            findings.Add("Shading colour and transparency are what a Shaded view draws. A Realistic view and a rendering draw the "
                + "appearance asset - SET_MATERIAL_APPEARANCE");
        findings.Add("Every element using these materials draws with them now - one Undo takes the whole call back");
    }
}
