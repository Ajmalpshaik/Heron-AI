// NOT STANDALONE. Assumes `doc`, `namePrefix`, `starterPick` and
// `libraryLooks` are in scope; leaves `createdMaterialIds`, `starterReport`,
// `alreadyThere`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Each appearance is edited in
// an edit scope of its own, which commits into that transaction.
//
// THE OWNER'S "MATERIAL ADDING", 2026-10-20: one call adds a named set of
// materials that look right in every view - a shading colour for Shaded and
// Consistent Colours, and an appearance asset of the material's own for
// Realistic and a rendering. CREATE_MATERIAL makes a material with a shading
// colour and nothing else, which draws flat dark grey in Realistic (row
// 5b-1263); this is the set that does not.
//
// THE TWELVE, each named namePrefix + its words - "TRG_" by the owner's rule,
// with the words spelled out: Glass Clear, Steel Stainless Satin, Steel
// Galvanised, Aluminium Brushed, Concrete Smooth, Plaster Paint White, Floor
// Tile Grey, Wood Oak, Carpet Grey, Ceiling Tile White, Duct Galvanised, Pipe
// Copper. `starterPick` takes some of them by their words ("glass; copper"),
// or all.
//
// THE COLOURS, GLOSS AND REFLECTION BELOW ARE HERON'S STARTER VALUES, chosen to
// read as the material at a glance - not a standard and not a product. Each is
// changed afterwards like any other material: SET_MATERIAL_PROPERTIES for the
// shading colour and class, SET_MATERIAL_APPEARANCE for the look.
//
// THE APPEARANCE IS A GENERIC ASSET OF THE MATERIAL'S OWN, set from those
// values, unless `libraryLooks` names an appearance for it - "Wood Oak=<an
// asset name from Revit's appearance library>" - which LIST_MATERIALS with
// withLibraryAppearances lists. No library name is assumed here: what the
// library holds depends on the PC's install, and a name that is not there
// refuses the call before anything is made.
//
// A NAME ALREADY IN USE IS LEFT AS IT IS AND REPORTED, never duplicated and
// never changed - the project's own "TRG_Glass Clear" may be what somebody
// meant.
//
// READ BACK: every material made is read again - its name, shading colour,
// class, and that it holds an appearance asset of its own; a Generic one's
// colour too. One that does not fails the call and nothing is kept.

var createdMaterialIds = new List<ElementId>();
var starterReport = "";
var alreadyThere = new List<string>();
var refused = "";
var findings = new List<string>();

{
    var problems = new List<string>();
    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    Func<Color, string> colourText = colour =>
        colour == null || !colour.IsValid ? "none" : colour.Red + "," + colour.Green + "," + colour.Blue;

    // name, class, shading colour, shading transparency, glossiness, metallic,
    // reflectivity direct, oblique, transparency, refraction (0: leave it).
    // The appearance colour is the shading colour.
    var starters = new List<Tuple<string, string, Color, int, double[], bool>>
    {
        Tuple.Create("Glass Clear",          "Glass",    new Color(176, 205, 214), 80, new[] { 95.0, 8.0, 90.0, 90.0, 1.52 }, false),
        Tuple.Create("Steel Stainless Satin", "Metal",   new Color(186, 190, 193), 0,  new[] { 60.0, 50.0, 90.0, 0.0, 0.0 }, true),
        Tuple.Create("Steel Galvanised",     "Metal",    new Color(165, 170, 174), 0,  new[] { 40.0, 35.0, 70.0, 0.0, 0.0 }, true),
        Tuple.Create("Aluminium Brushed",    "Metal",    new Color(198, 201, 205), 0,  new[] { 55.0, 45.0, 85.0, 0.0, 0.0 }, true),
        Tuple.Create("Concrete Smooth",      "Concrete", new Color(190, 189, 182), 0,  new[] { 10.0, 4.0, 20.0, 0.0, 0.0 }, false),
        Tuple.Create("Plaster Paint White",  "Plaster",  new Color(243, 242, 237), 0,  new[] { 20.0, 5.0, 30.0, 0.0, 0.0 }, false),
        Tuple.Create("Floor Tile Grey",      "Ceramic",  new Color(150, 151, 153), 0,  new[] { 70.0, 10.0, 60.0, 0.0, 0.0 }, false),
        Tuple.Create("Wood Oak",             "Wood",     new Color(171, 128, 82),  0,  new[] { 35.0, 6.0, 40.0, 0.0, 0.0 }, false),
        Tuple.Create("Carpet Grey",          "Textile",  new Color(108, 110, 112), 0,  new[] { 0.0, 0.0, 5.0, 0.0, 0.0 }, false),
        Tuple.Create("Ceiling Tile White",   "Generic",  new Color(236, 236, 231), 0,  new[] { 5.0, 2.0, 10.0, 0.0, 0.0 }, false),
        Tuple.Create("Duct Galvanised",      "Metal",    new Color(172, 176, 180), 0,  new[] { 40.0, 35.0, 70.0, 0.0, 0.0 }, true),
        Tuple.Create("Pipe Copper",          "Metal",    new Color(184, 115, 51),  0,  new[] { 65.0, 60.0, 90.0, 0.0, 0.0 }, true),
    };

    // ---- 1. The prefix and which of the twelve.
    var prefix = namePrefix ?? "";
    if (prefix.IndexOfAny("{}[]|;<>:?\\`~".ToCharArray()) >= 0)
        problems.Add("the prefix '" + prefix + "' holds a character Revit refuses in a name ({}[]|;<>:?\\`~)");
    var picked = new List<Tuple<string, string, Color, int, double[], bool>>();
    var pickText = (starterPick ?? "").Trim();
    if (squash(pickText) == "" || squash(pickText) == "all") picked.AddRange(starters);
    else
        foreach (var token in pickText.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var words = token.Trim().ToLowerInvariant().Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) continue;
            var hits = starters.Where(each => words.All(word =>
                each.Item1.ToLowerInvariant().Split(' ').Any(part => part.StartsWith(word)))).ToList();
            if (hits.Count == 0)
                problems.Add("'" + token.Trim() + "' is none of the starter materials - " + string.Join(", ", starters.Select(each => each.Item1)));
            foreach (var hit in hits) if (!picked.Contains(hit)) picked.Add(hit);
        }

    // ---- 2. Library looks asked for, by the starter's words.
    var documentAssets = new FilteredElementCollector(doc).OfClass(typeof(AppearanceAssetElement))
        .Cast<AppearanceAssetElement>().ToList();
    IList<Asset> library = null;
    Func<IList<Asset>> libraryAssets = () => library ?? (library = doc.Application.GetAssets(AssetType.Appearance) ?? new List<Asset>());
    // THE ASSET BROWSER'S NAME, WHERE REVIT GIVES IT (version 2, row 5b-1272),
    // the rule SET_MATERIAL_APPEARANCE gives its reasons for: Title, else a
    // Prism asset's description, else Revit's code ("Plastic-024") - measured
    // 2026-10-20 on Revit 2020, where a library asset's Title is empty and its
    // UIName is the code. The code is matched as well, below.
    Func<Asset, string> shownName = asset =>
    {
        if (!string.IsNullOrEmpty(asset.Title)) return asset.Title;
        var description = asset.FindByName(SchemaCommon.Description) as AssetPropertyString;
        var baseSchema = asset.FindByName(SchemaCommon.BaseSchema) as AssetPropertyString;
        if (description != null && !string.IsNullOrEmpty(description.Value) && baseSchema != null && baseSchema.Value != null
            && baseSchema.Value.StartsWith("Prism", StringComparison.OrdinalIgnoreCase)) return description.Value;
        var ui = asset.FindByName(SchemaCommon.UIName) as AssetPropertyString;
        if (ui != null && !string.IsNullOrEmpty(ui.Value)) return ui.Value;
        return asset.Name ?? "";
    };
    Func<Asset, bool> isGeneric = asset => asset != null && asset.FindByName(Generic.GenericDiffuse) != null;
    Func<Asset, bool> plainImage = asset =>
    {
        var diffuse = asset == null ? null : asset.FindByName(Generic.GenericDiffuse);
        return diffuse != null && diffuse.NumberOfConnectedProperties == 0;
    };
    var looks = new Dictionary<string, object>(StringComparer.Ordinal);
    var looksText = (libraryLooks ?? "").Trim();
    if (squash(looksText) == "none") looksText = "";
    foreach (var piece in looksText.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var at = piece.IndexOf('=');
        if (at <= 0) { problems.Add("'" + piece.Trim() + "' is not 'Starter words=Appearance name'"); continue; }
        var who = piece.Substring(0, at).Trim();
        var look = piece.Substring(at + 1).Trim();
        var starter = picked.FirstOrDefault(each => squash(each.Item1) == squash(who) || squash(prefix + each.Item1) == squash(who));
        if (starter == null) { problems.Add("'" + who + "' in libraryLooks is not one of the starter materials picked"); continue; }
        var inDocument = documentAssets.FirstOrDefault(each => string.Equals(each.Name, look, StringComparison.OrdinalIgnoreCase));
        if (inDocument != null) { looks[starter.Item1] = inDocument; continue; }
        var inLibrary = libraryAssets().FirstOrDefault(each => string.Equals(shownName(each), look, StringComparison.OrdinalIgnoreCase)
            || string.Equals(each.Name, look, StringComparison.OrdinalIgnoreCase));
        if (inLibrary != null) looks[starter.Item1] = inLibrary;
        else problems.Add("no appearance asset is called '" + look + "' in this document or in Revit's appearance library ("
            + libraryAssets().Count + " assets) - LIST_MATERIALS with withLibraryAppearances lists them");
    }

    // ---- 3. A plain Generic asset to start each own appearance from.
    Asset genericBase = null;
    AppearanceAssetElement genericDocumentBase = null;
    if (picked.Any(each => !looks.ContainsKey(each.Item1)) && problems.Count == 0)
    {
        var plain = libraryAssets().Where(isGeneric).ToList();
        genericBase = plain.FirstOrDefault(each => string.Equals(shownName(each), "Generic", StringComparison.OrdinalIgnoreCase))
            ?? plain.FirstOrDefault(plainImage);
        if (genericBase == null)
            genericDocumentBase = documentAssets.FirstOrDefault(each => plainImage(each.GetRenderingAsset()) && isGeneric(each.GetRenderingAsset()))
                ?? documentAssets.FirstOrDefault(each => isGeneric(each.GetRenderingAsset()));
        if (genericBase == null && genericDocumentBase == null)
            problems.Add(string.Format("there is no plain Generic appearance to start from - Revit's appearance library returned {0} asset(s) "
                + "and this document holds no Generic one. Is the Autodesk Material Library installed?", libraryAssets().Count));
    }

    if (problems.Count > 0)
    {
        refused = string.Join("; ", problems) + ". Nothing was made";
        findings.Add(refused);
    }
    else
    {
        var materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
        var assetNames = new HashSet<string>(documentAssets.Select(each => each.Name), StringComparer.OrdinalIgnoreCase);
        Func<string, string> freeAssetName = wantedName =>
        {
            var name = wantedName;
            for (var n = 2; assetNames.Contains(name); n++) name = wantedName + " " + n;
            assetNames.Add(name);
            return name;
        };
        var lines = new List<string>();
        var wrong = new List<string>();

        foreach (var starter in picked)
        {
            var name = prefix + starter.Item1;
            var existing = materials.FirstOrDefault(each => string.Equals(each.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                alreadyThere.Add(existing.Name);
                lines.Add(existing.Name + ": already in this " + (doc.IsFamilyDocument ? "family" : "project") + " - left as it is");
                continue;
            }

            var material = doc.GetElement(Material.Create(doc, name)) as Material;
            if (material == null) throw new InvalidOperationException("Revit did not make the material '" + name + "'. Nothing was kept");
            material.Color = starter.Item3;
            material.Transparency = starter.Item4;
            material.MaterialClass = starter.Item2;
            material.UseRenderAppearanceForShading = false;

            AppearanceAssetElement asset;
            string route;
            object look;
            if (looks.TryGetValue(starter.Item1, out look))
            {
                var fromDocument = look as AppearanceAssetElement;
                asset = fromDocument != null ? fromDocument.Duplicate(freeAssetName(name))
                    : AppearanceAssetElement.Create(doc, freeAssetName(name), (Asset)look);
                route = "the appearance '" + (fromDocument != null ? fromDocument.Name : shownName((Asset)look)) + "'";
            }
            else
            {
                asset = genericBase != null ? AppearanceAssetElement.Create(doc, freeAssetName(name), genericBase)
                    : genericDocumentBase.Duplicate(freeAssetName(name));
                var tune = starter.Item5;
                using (var scope = new AppearanceAssetEditScope(doc))
                {
                    try
                    {
                        var editable = scope.Start(asset.Id);
                        Action<string, double> putNumber = (property, value) =>
                        {
                            var p = editable.FindByName(property);
                            var d = p as AssetPropertyDouble;
                            var f = p as AssetPropertyFloat;
                            if (d != null && d.IsValidValue(value)) d.Value = value;
                            else if (f != null && f.IsValidValue((float)value)) f.Value = (float)value;
                            else throw new InvalidOperationException(string.Format("Revit would not take {0} for '{1}'", value, property));
                        };
                        var diffuse = editable.FindByName(Generic.GenericDiffuse);
                        if (diffuse != null && diffuse.NumberOfConnectedProperties > 0) diffuse.RemoveConnectedAsset();
                        var colour = diffuse as AssetPropertyDoubleArray4d;
                        if (colour == null) throw new InvalidOperationException("the Generic appearance has no colour");
                        colour.SetValueAsColor(starter.Item3);
                        putNumber(Generic.GenericGlossiness, tune[0] / 100.0);
                        var metal = editable.FindByName(Generic.GenericIsMetal) as AssetPropertyBoolean;
                        if (metal != null) metal.Value = starter.Item6;
                        putNumber(Generic.GenericReflectivityAt0deg, tune[1] / 100.0);
                        putNumber(Generic.GenericReflectivityAt90deg, tune[2] / 100.0);
                        putNumber(Generic.GenericTransparency, tune[3] / 100.0);
                        if (tune[4] > 0) putNumber(Generic.GenericRefractionIndex, tune[4]);
                        scope.Commit(true);
                    }
                    catch (Exception failure)
                    {
                        if (scope.IsActive) scope.Cancel();
                        throw new InvalidOperationException("The appearance of '" + name + "' could not be set: " + failure.Message
                            + ". Nothing was kept", failure);
                    }
                }
                route = string.Format("its own Generic appearance - colour {0}, glossiness {1}, {2}{3}",
                    colourText(starter.Item3), tune[0], starter.Item6 ? "metallic" : "non-metallic",
                    tune[3] > 0 ? ", transparency " + tune[3] + ", refraction " + tune[4] : "");
            }
            material.AppearanceAssetId = asset.Id;
            createdMaterialIds.Add(material.Id);
            lines.Add(string.Format("{0}: made - {1}, shading {2}{3}, {4} ('{5}')", name, starter.Item2, colourText(starter.Item3),
                starter.Item4 > 0 ? " at " + starter.Item4 + "% transparency" : "", route, asset.Name));
        }

        // ---- 4. Read back.
        doc.Regenerate();
        foreach (var id in createdMaterialIds)
        {
            var material = doc.GetElement(id) as Material;
            var starter = material == null ? null : picked.FirstOrDefault(each => prefix + each.Item1 == material.Name);
            if (material == null || starter == null) { wrong.Add("a material made here is gone, or lost its name"); continue; }
            if (colourText(material.Color) != colourText(starter.Item3)) wrong.Add("'" + material.Name + "' shades " + colourText(material.Color));
            if ((material.MaterialClass ?? "") != starter.Item2) wrong.Add("'" + material.Name + "' has the class '" + material.MaterialClass + "'");
            var held = doc.GetElement(material.AppearanceAssetId) as AppearanceAssetElement;
            if (held == null) { wrong.Add("'" + material.Name + "' holds no appearance asset"); continue; }
            if (!looks.ContainsKey(starter.Item1))
            {
                var colour = held.GetRenderingAsset().FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d;
                if (colour == null || colourText(colour.GetValueAsColor()) != colourText(starter.Item3))
                    wrong.Add("'" + material.Name + "' appearance colour reads " + (colour == null ? "nothing" : colourText(colour.GetValueAsColor())));
            }
        }
        if (wrong.Count > 0)
            throw new InvalidOperationException("Not every starter material read back as made: " + string.Join("; ", wrong) + ". Nothing was kept");

        starterReport = string.Join("  ||  ", lines);
        findings.Add(string.Format("{0} starter material(s) made, {1} already there and left as they are",
            createdMaterialIds.Count, alreadyThere.Count));
        findings.Add("Shaded views draw each shading colour; Realistic views and renderings draw its appearance. The values are "
            + "Heron's starter values, not a standard - SET_MATERIAL_PROPERTIES and SET_MATERIAL_APPEARANCE change them");
        findings.Add("Nothing uses them yet - SET_ELEMENT_MATERIAL gives elements a material, SET_COMPOUND_LAYER_WIDTH a wall or floor layer");
    }
}
