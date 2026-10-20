// NOT STANDALONE. Assumes `doc`, `materialsNamed`, `appearance`,
// `appearanceValues` and `matchShading` are in scope; leaves
// `changedMaterialIds`, `appearanceReport`, `assetsMade`, `sharedBefore`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit's appearance edit scope
// commits INTO the open transaction - "EditScope cannot be closed, there is no
// opened transaction" is its own refusal without one - so the whole call is
// still one entry in the undo list.
//
// THE APPEARANCE TAB - what a Realistic view and a rendering draw a material
// with. A material CREATE_MATERIAL made carries a shading colour and no
// appearance asset, and draws flat dark grey in Realistic: measured
// 2026-10-20 on Revit 2020, Project1, a duct heater family (row 5b-1263).
//
// WHICH ASSET (`appearance`):
//   keep        edit the asset the material has - or, with none, give it a
//               plain Generic one of its own
//   generic     a new plain Generic asset of its own, whatever it had
//   from <name> a copy of that material's asset - "copy the appearance from
//               one material to many"
//   <a name>    an appearance asset by name - one this document already holds,
//               else one of Revit's appearance library: a Prism asset by the
//               name the asset browser shows ("Steel - Satin"), any asset by
//               Revit's code ("Prism-256", "Plastic-024") - on Revit 2020 an
//               older asset's browser name is not readable (row 5b-1272);
//               LIST_MATERIALS withLibraryAppearances lists both. A name
//               matching nothing refuses the call, naming the closest.
//
// EVERY MATERIAL GETS ITS OWN ASSET, NAMED AFTER IT, and an asset another
// material shares is NEVER edited: the material is given its own copy first,
// and the answer names who shared it. So a change here never ripples into a
// material nobody named.
//
// THE VALUES (`appearanceValues`, "Field=Value" with ';' between) are the
// Generic appearance's, by the words the Material Browser shows:
//   Colour, Image (a file path, or none), Image Size (mm, "1200" or
//   "1200 x 600"), Image Rotation (degrees), Image Offset (mm, "x,y"),
//   Image Fade, Glossiness, Highlights (metallic / non-metallic),
//   Reflectivity Direct, Reflectivity Oblique, Transparency, Refraction
//   (1.0 to 5, or air / water / glass), Translucency, Cutout (a file, or
//   none), Luminance (cd/m2), Filter Colour, Colour Temperature (K), Bump (a
//   file, or none), Bump Amount (-1000 to 1000), Tint (a colour, or none).
// Fade, glossiness, reflectivity, transparency and translucency are given as
// the browser shows them, 0 to 100, and stored as Revit's 0 to 1. Revit
// documents 0 to 1 for fade, transparency and translucency; for glossiness
// and reflectivity it documents no range, so that scale is NEEDS-CHECKING IA5.
// A value is set only on a Generic asset: a library Metal or Glazing asset
// has properties of its own, and is refused by name rather than half set.
//
// `matchShading` gives the appearance the material's own shading colour and
// transparency - the one input a family built with CREATE_MATERIAL needs to
// look the same in Realistic as it does in Shaded. A Colour or Transparency
// given in the values wins over it.
//
// AN IMAGE FILE GIVEN WITH A FULL PATH MUST BE THERE - a missing texture
// renders as a plain colour and travels with the model (REPORT_MATERIAL_TEXTURES).
// A path with no drive or share is looked up in Revit's own texture folders,
// which are not readable here, and is said to be.
//
// NOTHING IS WRITTEN UNTIL EVERY NAME AND VALUE HAS BEEN CHECKED. Then each
// material is given its asset and edited in an edit scope of its own (Revit
// allows one asset per scope), and every value is read back off the asset the
// material now has: one that does not read as asked fails the call and nothing
// from it is kept.

var changedMaterialIds = new List<ElementId>();
var appearanceReport = "";
var assetsMade = new List<string>();
var sharedBefore = "";
var refused = "";
var findings = new List<string>();

{
    var problems = new List<string>();
    var notes = new List<string>();
    var all = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>()
        .Where(each => each != null).ToList();
    var documentAssets = new FilteredElementCollector(doc).OfClass(typeof(AppearanceAssetElement))
        .Cast<AppearanceAssetElement>().ToList();

    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    Func<string, HashSet<string>> wordsOf = text => new HashSet<string>(
        System.Text.RegularExpressions.Regex.Split((text ?? "").ToLowerInvariant(), "[^a-z0-9]+")
            .Where(word => word.Length > 1));
    Func<string, IEnumerable<string>, string> closest = (said, names) =>
    {
        var asked = wordsOf(said);
        var bare = (said ?? "").Replace("*", "").Replace("?", "").Trim();
        var near = names.Distinct(StringComparer.OrdinalIgnoreCase)
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
    Func<string, double?> numberOf = text =>
    {
        double value;
        var said = (text ?? "").Trim().TrimEnd('%').Trim();
        if (double.TryParse(said, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value)) return value;
        return null;
    };

    // ---- the asset's own words, read without editing it.
    Func<Asset, string, string> textProperty = (asset, name) =>
    {
        var p = asset == null ? null : asset.FindByName(name) as AssetPropertyString;
        return p == null ? null : p.Value;
    };
    // THE ASSET BROWSER'S NAME, WHERE REVIT GIVES IT (version 2, row 5b-1272).
    // Measured 2026-10-20 on Revit 2020 (Group IA, IA1): a LIBRARY asset's
    // Title is empty and its UIName is Revit's code - "Prism-256",
    // "Plastic-024" - never the browser's name; only once an asset is in a
    // document does Title carry it ("Steel - Satin"). A Prism asset's
    // description is that name in another case ("Steel - satin"); an older
    // asset's description is a sentence its whole kind shares ("Metal.",
    // "Generic material." on 557 of them), so it is known by its code. The
    // code is taken as well wherever a name is matched.
    Func<Asset, string> shownName = asset =>
    {
        if (!string.IsNullOrEmpty(asset.Title)) return asset.Title;
        var description = textProperty(asset, SchemaCommon.Description);
        var baseSchema = textProperty(asset, SchemaCommon.BaseSchema) ?? "";
        if (!string.IsNullOrEmpty(description) && baseSchema.StartsWith("Prism", StringComparison.OrdinalIgnoreCase)) return description;
        var ui = textProperty(asset, SchemaCommon.UIName);
        if (!string.IsNullOrEmpty(ui)) return ui;
        return asset.Name ?? "";
    };
    Func<Asset, bool> isGeneric = asset => asset != null && asset.FindByName(Generic.GenericDiffuse) != null;
    Func<Asset, string> schemaOf = asset =>
    {
        var schema = textProperty(asset, SchemaCommon.BaseSchema);
        return string.IsNullOrEmpty(schema) ? (asset == null ? "none" : asset.Name) : schema;
    };
    Func<AssetProperty, string> imageOf = slot =>
    {
        if (slot == null || slot.NumberOfConnectedProperties == 0) return "none";
        var connected = slot.GetSingleConnectedAsset();
        var source = connected == null ? null : connected.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) as AssetPropertyString;
        return source == null ? "a map that is not an image" : (source.Value ?? "");
    };
    Func<AssetProperty, double?> doubleOf = property =>
    {
        var d = property as AssetPropertyDouble;
        if (d != null) return d.Value;
        var f = property as AssetPropertyFloat;
        if (f != null) return f.Value;
        return null;
    };

    IList<Asset> library = null;
    Func<IList<Asset>> libraryAssets = () => library ?? (library = doc.Application.GetAssets(AssetType.Appearance) ?? new List<Asset>());

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
                    problems.Add(string.Format("'{0}' matches {1} materials when case is ignored and none exactly - type it as Revit spells it",
                        said, hits.Count));
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

    // ---- 2. Which asset.
    var mode = "keep";
    var said0 = (appearance ?? "").Trim();
    var word0 = squash(said0);
    Material copyFrom = null;
    AppearanceAssetElement namedDocumentAsset = null;
    Asset namedLibraryAsset = null;
    if (word0 == "" || word0 == "keep" || word0 == "own" || word0 == "asitis") mode = "keep";
    else if (word0 == "generic" || word0 == "plaingeneric" || word0 == "newgeneric") mode = "generic";
    else if (said0.StartsWith("from ", StringComparison.OrdinalIgnoreCase) || said0.StartsWith("like ", StringComparison.OrdinalIgnoreCase))
    {
        mode = "copy";
        var sourceName = said0.Substring(5).Trim();
        var sources = all.Where(each => string.Equals(each.Name, sourceName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (sources.Count > 1) sources = sources.Where(each => each.Name == sourceName).ToList();
        if (sources.Count != 1)
            problems.Add(string.Format("no single material is called '{0}' to copy the appearance from - closest: {1}",
                sourceName, closest(sourceName, all.Select(each => each.Name))));
        else if (sources[0].AppearanceAssetId == ElementId.InvalidElementId
            || !(doc.GetElement(sources[0].AppearanceAssetId) is AppearanceAssetElement))
            problems.Add(string.Format("'{0}' has no appearance asset to copy", sources[0].Name));
        else copyFrom = sources[0];
    }
    else
    {
        mode = "named";
        var inDocument = documentAssets.Where(each => string.Equals(each.Name, said0, StringComparison.OrdinalIgnoreCase)).ToList();
        if (inDocument.Count >= 1) namedDocumentAsset = inDocument.FirstOrDefault(each => each.Name == said0) ?? inDocument[0];
        else
        {
            var inLibrary = libraryAssets().Where(each => string.Equals(shownName(each), said0, StringComparison.OrdinalIgnoreCase)
                || string.Equals(each.Name, said0, StringComparison.OrdinalIgnoreCase)).ToList();
            if (inLibrary.Count >= 1)
            {
                namedLibraryAsset = inLibrary[0];
                if (inLibrary.Count > 1)
                    notes.Add(string.Format("Revit's appearance library holds {0} assets called '{1}' - the first, from '{2}', was used",
                        inLibrary.Count, said0, inLibrary[0].LibraryName));
            }
            else
                problems.Add(string.Format("no appearance asset is called '{0}' - in this document closest: {1}; in Revit's appearance library ({2} assets) closest: {3}",
                    said0, closest(said0, documentAssets.Select(each => each.Name)), libraryAssets().Count,
                    closest(said0, libraryAssets().Select(shownName))));
        }
    }

    // ---- 3. Which values. Field, the number or text Revit will be given.
    var fieldOf = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "colour", "Colour" }, { "color", "Colour" }, { "diffuse", "Colour" },
        { "image", "Image" }, { "colourimage", "Image" }, { "texture", "Image" },
        { "imagesize", "Image Size" }, { "texturesize", "Image Size" }, { "imagescale", "Image Size" },
        { "imagerotation", "Image Rotation" }, { "imageangle", "Image Rotation" }, { "texturerotation", "Image Rotation" },
        { "imageoffset", "Image Offset" }, { "textureoffset", "Image Offset" },
        { "imagefade", "Image Fade" },
        { "glossiness", "Glossiness" }, { "gloss", "Glossiness" },
        { "highlights", "Highlights" },
        { "reflectivity", "Reflectivity Direct" }, { "reflectivitydirect", "Reflectivity Direct" },
        { "reflectivityoblique", "Reflectivity Oblique" },
        { "transparency", "Transparency" }, { "transparencyamount", "Transparency" },
        { "refraction", "Refraction" },
        { "translucency", "Translucency" },
        { "cutout", "Cutout" }, { "cutouts", "Cutout" },
        { "luminance", "Luminance" }, { "selfillumination", "Luminance" }, { "selfilluminationluminance", "Luminance" },
        { "filtercolour", "Filter Colour" }, { "filtercolor", "Filter Colour" }, { "selfilluminationcolour", "Filter Colour" },
        { "colourtemperature", "Colour Temperature" }, { "colortemperature", "Colour Temperature" },
        { "bump", "Bump" }, { "bumpmap", "Bump" },
        { "bumpamount", "Bump Amount" },
        { "tint", "Tint" }, { "tintcolour", "Tint" }, { "tintcolor", "Tint" },
    };
    var wanted = new Dictionary<string, object>(StringComparer.Ordinal);
    var valuesText = (appearanceValues ?? "").Trim();
    if (squash(valuesText) == "none") valuesText = "";
    foreach (var piece in valuesText.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var text = piece.Trim();
        if (text.Length == 0) continue;
        var at = text.IndexOf('=');
        if (at <= 0) { problems.Add("'" + text + "' is not Field=Value"); continue; }
        var key = text.Substring(0, at).Trim();
        var value = text.Substring(at + 1).Trim();
        string field;
        if (!fieldOf.TryGetValue(squash(key), out field))
        {
            problems.Add("'" + key + "' is not a Generic appearance field - Colour, Image, Image Size, Image Rotation, Image Offset, "
                + "Image Fade, Glossiness, Highlights, Reflectivity Direct, Reflectivity Oblique, Transparency, Refraction, "
                + "Translucency, Cutout, Luminance, Filter Colour, Colour Temperature, Bump, Bump Amount, Tint");
            continue;
        }
        if (wanted.ContainsKey(field)) { problems.Add("'" + field + "' is given twice"); continue; }
        var word = squash(value);
        if (field == "Colour" || field == "Filter Colour" || (field == "Tint" && word != "none"))
        {
            var colour = colourOf(value);
            if (colour == null) problems.Add(field + " '" + value + "' is not a colour - three numbers 0 to 255, '200,120,60', or '#C8783C'");
            else wanted[field] = colour;
        }
        else if (field == "Tint") wanted[field] = null;
        else if (field == "Image" || field == "Cutout" || field == "Bump")
        {
            if (word == "none" || word == "remove") { wanted[field] = ""; continue; }
            var path = value.Trim('"');
            if (System.IO.Path.IsPathRooted(path) && !System.IO.File.Exists(path))
                problems.Add(field + ": the file '" + path + "' is not there - a missing image renders as plain colour");
            else
            {
                wanted[field] = path;
                if (!System.IO.Path.IsPathRooted(path))
                    notes.Add(field + " '" + path + "' has no drive or share, so Revit looks it up in its own texture folders - not checked here");
            }
        }
        else if (field == "Image Size" || field == "Image Offset")
        {
            var parts = value.ToLowerInvariant().Replace("mm", "").Split(new[] { 'x', ',', '*', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var numbers = parts.Select(part => numberOf(part)).ToList();
            if (numbers.Count < 1 || numbers.Count > 2 || numbers.Any(n => n == null)
                || (field == "Image Size" && numbers.Any(n => n.Value <= 0)))
                problems.Add(field + " '" + value + "' is not " + (field == "Image Size" ? "one or two sizes in mm above 0 - '1200' or '1200 x 600'" : "one or two distances in mm - '100,50'"));
            else wanted[field] = new[] { numbers[0].Value, numbers.Count == 2 ? numbers[1].Value : (field == "Image Size" ? numbers[0].Value : 0.0) };
        }
        else if (field == "Highlights")
        {
            if (word == "metallic" || word == "metal") wanted[field] = true;
            else if (word == "nonmetallic" || word == "nonmetal") wanted[field] = false;
            else problems.Add("Highlights '" + value + "' is not metallic or non-metallic");
        }
        else if (field == "Refraction")
        {
            double? index = word == "air" ? 1.0 : word == "water" ? 1.33 : word == "glass" ? 1.52 : numberOf(value);
            if (index == null || index.Value < 0.01 || index.Value > 5)
                problems.Add("Refraction '" + value + "' is not a number from 0.01 to 5, or air, water or glass");
            else wanted[field] = index.Value;
        }
        else
        {
            var number = numberOf(value);
            var percent = field == "Image Fade" || field == "Glossiness" || field == "Reflectivity Direct"
                || field == "Reflectivity Oblique" || field == "Transparency" || field == "Translucency";
            if (number == null) { problems.Add(field + " '" + value + "' is not a number"); continue; }
            if (percent && (number.Value < 0 || number.Value > 100)) { problems.Add(field + " '" + value + "' is not 0 to 100"); continue; }
            if (field == "Image Rotation" && (number.Value < 0 || number.Value > 360)) { problems.Add("Image Rotation '" + value + "' is not 0 to 360 degrees"); continue; }
            if (field == "Bump Amount" && (number.Value < -1000 || number.Value > 1000)) { problems.Add("Bump Amount '" + value + "' is not -1000 to 1000"); continue; }
            if ((field == "Luminance" || field == "Colour Temperature") && number.Value < 0) { problems.Add(field + " '" + value + "' is below 0"); continue; }
            wanted[field] = percent ? number.Value / 100.0 : number.Value;
        }
    }
    var imageSettings = new[] { "Image Size", "Image Rotation", "Image Offset" };
    var editing = wanted.Count > 0 || matchShading;
    if (mode == "keep" && !editing && problems.Count == 0)
        problems.Add("nothing was asked - name an appearance asset, 'generic' or 'from <material>', give values, or matchShading");

    // ---- 4. Each material's base checked before anything changes. A value is
    // set only on a Generic asset.
    Asset genericBase = null;
    AppearanceAssetElement genericDocumentBase = null;
    Func<bool> findGeneric = () =>
    {
        if (genericBase != null || genericDocumentBase != null) return true;
        var plain = libraryAssets().Where(isGeneric).ToList();
        genericBase = plain.FirstOrDefault(each => string.Equals(shownName(each), "Generic", StringComparison.OrdinalIgnoreCase))
            ?? plain.FirstOrDefault(each => imageOf(each.FindByName(Generic.GenericDiffuse)) == "none");
        if (genericBase != null) return true;
        genericDocumentBase = documentAssets.FirstOrDefault(each =>
        {
            var asset = each.GetRenderingAsset();
            return isGeneric(asset) && imageOf(asset.FindByName(Generic.GenericDiffuse)) == "none";
        }) ?? documentAssets.FirstOrDefault(each => isGeneric(each.GetRenderingAsset()));
        return genericDocumentBase != null;
    };
    var needsGeneric = mode == "generic" || (mode == "keep" && targets.Any(each =>
        !(doc.GetElement(each.AppearanceAssetId) is AppearanceAssetElement)));
    if (needsGeneric && problems.Count == 0 && !findGeneric())
        problems.Add(string.Format("there is no plain Generic appearance to start from - Revit's appearance library returned {0} asset(s) "
            + "and this document holds no Generic one. Is the Autodesk Material Library installed?", libraryAssets().Count));
    if (editing && problems.Count == 0)
    {
        Asset fixedBase = mode == "copy" ? ((AppearanceAssetElement)doc.GetElement(copyFrom.AppearanceAssetId)).GetRenderingAsset()
            : mode == "named" ? (namedDocumentAsset != null ? namedDocumentAsset.GetRenderingAsset() : namedLibraryAsset)
            : null;
        if (fixedBase != null && !isGeneric(fixedBase))
            problems.Add(string.Format("'{0}' is a {1} appearance, not a Generic one, so its values are its own - set them in the Material "
                + "Browser, or give appearance 'generic'", mode == "copy" ? copyFrom.Name + "'s asset" : said0, schemaOf(fixedBase)));
        if (mode == "keep")
            foreach (var material in targets)
            {
                var element = doc.GetElement(material.AppearanceAssetId) as AppearanceAssetElement;
                var asset = element == null ? null : element.GetRenderingAsset();
                if (asset != null && !isGeneric(asset))
                    problems.Add(string.Format("'{0}' has the {1} appearance '{2}', not a Generic one - give appearance 'generic' to replace it, "
                        + "or set its own values in the Material Browser", material.Name, schemaOf(asset), element.Name));
            }
        if (imageSettings.Any(wanted.ContainsKey) && !wanted.ContainsKey("Image"))
            foreach (var material in targets)
            {
                var element = mode == "keep" ? doc.GetElement(material.AppearanceAssetId) as AppearanceAssetElement : null;
                var asset = element != null ? element.GetRenderingAsset() : fixedBase;
                if (asset == null || imageOf(asset.FindByName(Generic.GenericDiffuse)) == "none")
                    problems.Add(string.Format("'{0}' has no image to size, turn or move - give Image too", material.Name));
            }
        if (wanted.ContainsKey("Image") && (string)wanted["Image"] == "" && imageSettings.Any(wanted.ContainsKey))
            problems.Add("an image cannot be removed and sized in one call");
    }

    if (problems.Count > 0)
    {
        refused = string.Join("; ", problems) + ". Nothing was changed";
        findings.Add(refused);
    }
    else
    {
        var assetNames = new HashSet<string>(documentAssets.Select(each => each.Name), StringComparer.OrdinalIgnoreCase);
        Func<string, string> freeAssetName = wantedName =>
        {
            var name = wantedName;
            for (var n = 2; assetNames.Contains(name); n++) name = wantedName + " " + n;
            assetNames.Add(name);
            return name;
        };
        var shareNotes = new List<string>();
        // WHO SHARED WHAT, AS IT STOOD BEFORE THIS CALL. Counted live, the last
        // of several named materials sharing one asset would find it no longer
        // shared, edit it in place and keep an asset not named after it.
        var usersAtStart = new Dictionary<string, List<Material>>(StringComparer.Ordinal);
        foreach (var each in all)
        {
            if (each.AppearanceAssetId == ElementId.InvalidElementId) continue;
            List<Material> users;
            if (!usersAtStart.TryGetValue(each.AppearanceAssetId.ToString(), out users))
                usersAtStart[each.AppearanceAssetId.ToString()] = users = new List<Material>();
            users.Add(each);
        }
        // AN IMAGE'S SIZE AND OFFSET IN MILLIMETRES, read back off the asset -
        // the same unit split as the write below.
        Func<Asset, string, double?> millimetresOf = (bitmap, name) =>
        {
            var p = bitmap == null ? null : bitmap.FindByName(name) as AssetPropertyDistance;
            if (p == null) return null;
#if REVIT2020
            return UnitUtils.Convert(p.Value, p.DisplayUnitType, DisplayUnitType.DUT_MILLIMETERS);
#else
            return UnitUtils.Convert(p.Value, p.GetUnitTypeId(), UnitTypeId.Millimeters);
#endif
        };
        var lines = new List<string>();
        var wrong = new List<string>();

        Func<Asset, string> describe = asset =>
        {
            if (asset == null) return "no appearance asset";
            if (!isGeneric(asset)) return schemaOf(asset);
            var parts = new List<string>();
            foreach (var field in wanted.Keys.Concat(matchShading ? new[] { "Colour", "Transparency" } : new string[0]).Distinct())
            {
                if (field == "Colour") { var p = asset.FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d; parts.Add("Colour " + (p == null ? "?" : colourText(p.GetValueAsColor()))); }
                else if (field == "Image") parts.Add("Image " + imageOf(asset.FindByName(Generic.GenericDiffuse)));
                else if (field == "Cutout") parts.Add("Cutout " + imageOf(asset.FindByName(Generic.GenericCutoutOpacity)));
                else if (field == "Bump") parts.Add("Bump " + imageOf(asset.FindByName(Generic.GenericBumpMap)));
                else if (field == "Highlights") { var p = asset.FindByName(Generic.GenericIsMetal) as AssetPropertyBoolean; parts.Add("Highlights " + (p == null ? "?" : p.Value ? "metallic" : "non-metallic")); }
                else if (field == "Tint")
                {
                    var on = asset.FindByName(Generic.CommonTintToggle) as AssetPropertyBoolean;
                    var tint = asset.FindByName(Generic.CommonTintColor) as AssetPropertyDoubleArray4d;
                    parts.Add("Tint " + (on == null || !on.Value ? "none" : tint == null ? "on" : colourText(tint.GetValueAsColor())));
                }
                else if (field == "Filter Colour") { var p = asset.FindByName(Generic.GenericSelfIllumFilterMap) as AssetPropertyDoubleArray4d; parts.Add("Filter Colour " + (p == null ? "?" : colourText(p.GetValueAsColor()))); }
            }
            return parts.Count == 0 ? "Generic" : string.Join(", ", parts);
        };

        foreach (var material in targets)
        {
            var m = material;
            var oldElement = doc.GetElement(m.AppearanceAssetId) as AppearanceAssetElement;
            var oldAsset = oldElement == null ? null : oldElement.GetRenderingAsset();
            var wasName = oldElement == null ? "none" : oldElement.Name;
            var was = describe(oldAsset);

            // 4a. The asset this material will have - always one of its own.
            AppearanceAssetElement target = null;
            if (mode == "keep" && oldElement != null)
            {
                List<Material> users;
                usersAtStart.TryGetValue(oldElement.Id.ToString(), out users);
                var sharers = (users ?? new List<Material>()).Where(each => each.Id != m.Id).Select(each => each.Name).ToList();
                if (sharers.Count == 0) target = oldElement;
                else
                {
                    target = oldElement.Duplicate(freeAssetName(m.Name));
                    shareNotes.Add(string.Format("'{0}': its appearance '{1}' was shared with {2} - given its own copy, '{3}', and '{1}' was not touched",
                        m.Name, oldElement.Name, string.Join(", ", sharers.Take(6).Select(each => "'" + each + "'"))
                            + (sharers.Count > 6 ? " and " + (sharers.Count - 6) + " more" : ""), target.Name));
                }
            }
            else if (mode == "copy")
                target = ((AppearanceAssetElement)doc.GetElement(copyFrom.AppearanceAssetId)).Duplicate(freeAssetName(m.Name));
            else if (mode == "named" && namedDocumentAsset != null)
                target = namedDocumentAsset.Duplicate(freeAssetName(m.Name));
            else if (mode == "named")
                target = AppearanceAssetElement.Create(doc, freeAssetName(m.Name), namedLibraryAsset);
            var plainGeneric = false;
            if (target == null && mode != "copy" && mode != "named")
            {
                target = genericBase != null
                    ? AppearanceAssetElement.Create(doc, freeAssetName(m.Name), genericBase)
                    : genericDocumentBase.Duplicate(freeAssetName(m.Name));
                plainGeneric = true;
            }
            if (target.Id != m.AppearanceAssetId)
            {
                m.AppearanceAssetId = target.Id;
                assetsMade.Add(target.Name);
            }

            // 4b. The values, in an edit scope of this asset's own.
            var asked = new Dictionary<string, object>(wanted, StringComparer.Ordinal);
            if (matchShading)
            {
                if (!asked.ContainsKey("Colour") && m.Color != null && m.Color.IsValid) asked["Colour"] = new Color(m.Color.Red, m.Color.Green, m.Color.Blue);
                if (!asked.ContainsKey("Transparency")) asked["Transparency"] = m.Transparency / 100.0;
            }
            // A PLAIN GENERIC CARRIES NO IMAGE IT BORROWED: one copied from a
            // document asset may hold another material's texture, which would
            // draw over the colour asked for.
            var stripImage = plainGeneric && !asked.ContainsKey("Image");
            if (asked.Count > 0 || stripImage)
            {
                using (var scope = new AppearanceAssetEditScope(doc))
                {
                    try
                    {
                        var editable = scope.Start(target.Id);
                        Func<string, AssetProperty> slot = name =>
                        {
                            var found = editable.FindByName(name);
                            if (found == null) throw new InvalidOperationException(string.Format(
                                "the appearance of '{0}' has no '{1}' - it is not a whole Generic asset", m.Name, name));
                            return found;
                        };
                        Action<string, Color> putColour = (name, colour) =>
                        {
                            var p = slot(name) as AssetPropertyDoubleArray4d;
                            if (p == null || !p.IsValidValue(colour)) throw new InvalidOperationException(string.Format(
                                "Revit would not take the colour {0} for '{1}' on '{2}'", colourText(colour), name, m.Name));
                            p.SetValueAsColor(colour);
                        };
                        Action<string, double> putNumber = (name, value) =>
                        {
                            var p = slot(name);
                            var d = p as AssetPropertyDouble;
                            var f = p as AssetPropertyFloat;
                            if (d != null && d.IsValidValue(value)) d.Value = value;
                            else if (f != null && f.IsValidValue((float)value)) f.Value = (float)value;
                            else throw new InvalidOperationException(string.Format(
                                "Revit would not take {0} for '{1}' on '{2}'", value, name, m.Name));
                        };
                        Action<string, bool> putFlag = (name, value) =>
                        {
                            var p = slot(name) as AssetPropertyBoolean;
                            if (p == null) throw new InvalidOperationException(string.Format("'{0}' on '{1}' is not a yes / no", name, m.Name));
                            p.Value = value;
                        };
                        Func<string, string, Asset> putImage = (name, path) =>
                        {
                            var p = slot(name);
                            if (string.IsNullOrEmpty(path))
                            {
                                if (p.NumberOfConnectedProperties > 0) p.RemoveConnectedAsset();
                                return null;
                            }
                            var bitmap = p.NumberOfConnectedProperties > 0 ? p.GetSingleConnectedAsset() : null;
                            if (bitmap == null || !(bitmap.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) is AssetPropertyString))
                            {
                                if (p.NumberOfConnectedProperties > 0) p.RemoveConnectedAsset();
                                var schema = p.IsValidSchemaIdentifier("UnifiedBitmapSchema") ? "UnifiedBitmapSchema"
                                    : p.IsValidSchemaIdentifier("UnifiedBitmap") ? "UnifiedBitmap" : null;
                                if (schema == null) throw new InvalidOperationException(string.Format(
                                    "Revit offers no image map for '{0}' on '{1}'", name, m.Name));
                                p.AddConnectedAsset(schema);
                                bitmap = p.GetSingleConnectedAsset();
                            }
                            var source = bitmap == null ? null : bitmap.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) as AssetPropertyString;
                            if (source == null || !source.IsValidValue(path)) throw new InvalidOperationException(string.Format(
                                "Revit would not take the image '{0}' for '{1}' on '{2}'", path, name, m.Name));
                            source.Value = path;
                            return bitmap;
                        };
                        Action<Asset, string, double> putMillimetres = (bitmap, name, mm) =>
                        {
                            var p = bitmap.FindByName(name) as AssetPropertyDistance;
                            if (p == null) throw new InvalidOperationException(string.Format("the image on '{0}' has no '{1}'", m.Name, name));
#if REVIT2020
                            var value = UnitUtils.Convert(mm, DisplayUnitType.DUT_MILLIMETERS, p.DisplayUnitType);
#else
                            var value = UnitUtils.Convert(mm, UnitTypeId.Millimeters, p.GetUnitTypeId());
#endif
                            if (!p.IsValidValue(value)) throw new InvalidOperationException(string.Format(
                                "Revit would not take {0} mm for '{1}' on '{2}'", mm, name, m.Name));
                            p.Value = value;
                        };

                        Asset diffuseImage = null;
                        if (stripImage)
                        {
                            var diffuse = slot(Generic.GenericDiffuse);
                            if (diffuse.NumberOfConnectedProperties > 0) diffuse.RemoveConnectedAsset();
                        }
                        foreach (var pair in asked)
                        {
                            var v = pair.Value;
                            switch (pair.Key)
                            {
                                case "Colour": putColour(Generic.GenericDiffuse, (Color)v); break;
                                case "Image": diffuseImage = putImage(Generic.GenericDiffuse, (string)v); break;
                                case "Cutout": putImage(Generic.GenericCutoutOpacity, (string)v); break;
                                case "Bump": putImage(Generic.GenericBumpMap, (string)v); break;
                                case "Image Fade": putNumber(Generic.GenericDiffuseImageFade, (double)v); break;
                                case "Glossiness": putNumber(Generic.GenericGlossiness, (double)v); break;
                                case "Highlights": putFlag(Generic.GenericIsMetal, (bool)v); break;
                                case "Reflectivity Direct": putNumber(Generic.GenericReflectivityAt0deg, (double)v); break;
                                case "Reflectivity Oblique": putNumber(Generic.GenericReflectivityAt90deg, (double)v); break;
                                case "Transparency": putNumber(Generic.GenericTransparency, (double)v); break;
                                case "Refraction": putNumber(Generic.GenericRefractionIndex, (double)v); break;
                                case "Translucency": putNumber(Generic.GenericRefractionTranslucencyWeight, (double)v); break;
                                case "Luminance": putNumber(Generic.GenericSelfIllumLuminance, (double)v); break;
                                case "Filter Colour": putColour(Generic.GenericSelfIllumFilterMap, (Color)v); break;
                                case "Colour Temperature": putNumber(Generic.GenericSelfIllumColorTemperature, (double)v); break;
                                case "Bump Amount": putNumber(Generic.GenericBumpAmount, (double)v); break;
                                case "Tint":
                                    putFlag(Generic.CommonTintToggle, v != null);
                                    if (v != null) putColour(Generic.CommonTintColor, (Color)v);
                                    break;
                            }
                        }
                        if (imageSettings.Any(asked.ContainsKey))
                        {
                            var bitmap = diffuseImage ?? slot(Generic.GenericDiffuse).GetSingleConnectedAsset();
                            if (bitmap == null) throw new InvalidOperationException(string.Format("'{0}' has no image to size, turn or move", m.Name));
                            if (asked.ContainsKey("Image Size"))
                            {
                                var size = (double[])asked["Image Size"];
                                putMillimetres(bitmap, UnifiedBitmap.TextureRealWorldScaleX, size[0]);
                                putMillimetres(bitmap, UnifiedBitmap.TextureRealWorldScaleY, size[1]);
                            }
                            if (asked.ContainsKey("Image Offset"))
                            {
                                var offset = (double[])asked["Image Offset"];
                                putMillimetres(bitmap, UnifiedBitmap.TextureRealWorldOffsetX, offset[0]);
                                putMillimetres(bitmap, UnifiedBitmap.TextureRealWorldOffsetY, offset[1]);
                            }
                            if (asked.ContainsKey("Image Rotation"))
                            {
                                var angle = bitmap.FindByName(UnifiedBitmap.TextureWAngle) as AssetPropertyDouble;
                                var turn = (double)asked["Image Rotation"];
                                if (angle == null || !angle.IsValidValue(turn)) throw new InvalidOperationException(string.Format(
                                    "Revit would not take {0} degrees for the image angle on '{1}'", turn, m.Name));
                                angle.Value = turn;
                            }
                        }
                        scope.Commit(true);
                    }
                    catch (Exception failure)
                    {
                        if (scope.IsActive) scope.Cancel();
                        throw new InvalidOperationException(failure.Message + ". Nothing from this call was kept", failure);
                    }
                }
            }

            // 4c. Read back off the asset the material has now.
            var nowElement = doc.GetElement(m.AppearanceAssetId) as AppearanceAssetElement;
            var now = nowElement == null ? null : nowElement.GetRenderingAsset();
            if (nowElement == null || nowElement.Id != target.Id)
                wrong.Add("'" + m.Name + "' does not have the asset it was given");
            else
                foreach (var pair in asked)
                {
                    var v = pair.Value;
                    string seen = null, meant = null;
                    Func<string, double?> numberAt = name => doubleOf(now.FindByName(name));
                    switch (pair.Key)
                    {
                        case "Colour":
                        case "Filter Colour":
                            var colourSlot = now.FindByName(pair.Key == "Colour" ? Generic.GenericDiffuse : Generic.GenericSelfIllumFilterMap) as AssetPropertyDoubleArray4d;
                            seen = colourSlot == null ? "?" : colourText(colourSlot.GetValueAsColor());
                            meant = colourText((Color)v);
                            break;
                        case "Image": seen = imageOf(now.FindByName(Generic.GenericDiffuse)); meant = (string)v == "" ? "none" : (string)v; break;
                        case "Cutout": seen = imageOf(now.FindByName(Generic.GenericCutoutOpacity)); meant = (string)v == "" ? "none" : (string)v; break;
                        case "Bump": seen = imageOf(now.FindByName(Generic.GenericBumpMap)); meant = (string)v == "" ? "none" : (string)v; break;
                        case "Highlights":
                            var metal = now.FindByName(Generic.GenericIsMetal) as AssetPropertyBoolean;
                            seen = metal == null ? "?" : metal.Value.ToString(); meant = v.ToString();
                            break;
                        case "Tint":
                            var tintOn = now.FindByName(Generic.CommonTintToggle) as AssetPropertyBoolean;
                            seen = tintOn == null ? "?" : tintOn.Value ? "on" : "off"; meant = v == null ? "off" : "on";
                            break;
                        case "Image Size":
                        case "Image Offset":
                            var image = now.FindByName(Generic.GenericDiffuse);
                            var bitmapNow = image == null || image.NumberOfConnectedProperties == 0 ? null : image.GetSingleConnectedAsset();
                            var pair2 = (double[])v;
                            var x = millimetresOf(bitmapNow, pair.Key == "Image Size" ? UnifiedBitmap.TextureRealWorldScaleX : UnifiedBitmap.TextureRealWorldOffsetX);
                            var y = millimetresOf(bitmapNow, pair.Key == "Image Size" ? UnifiedBitmap.TextureRealWorldScaleY : UnifiedBitmap.TextureRealWorldOffsetY);
                            if (x == null || y == null || Math.Abs(x.Value - pair2[0]) > 1e-3 || Math.Abs(y.Value - pair2[1]) > 1e-3)
                            {
                                seen = x == null || y == null ? "?" : Math.Round(x.Value, 3) + " x " + Math.Round(y.Value, 3) + " mm";
                                meant = pair2[0] + " x " + pair2[1] + " mm";
                            }
                            break;
                        case "Image Rotation":
                            var imageSlot = now.FindByName(Generic.GenericDiffuse);
                            var bitmapTurned = imageSlot == null || imageSlot.NumberOfConnectedProperties == 0 ? null : imageSlot.GetSingleConnectedAsset();
                            var angleNow = bitmapTurned == null ? null : doubleOf(bitmapTurned.FindByName(UnifiedBitmap.TextureWAngle));
                            if (angleNow == null || Math.Abs(angleNow.Value - (double)v) > 1e-4)
                            {
                                seen = angleNow == null ? "?" : angleNow.Value + " degrees";
                                meant = (double)v + " degrees";
                            }
                            break;
                        default:
                            var names = new Dictionary<string, string>
                            {
                                { "Image Fade", Generic.GenericDiffuseImageFade }, { "Glossiness", Generic.GenericGlossiness },
                                { "Reflectivity Direct", Generic.GenericReflectivityAt0deg }, { "Reflectivity Oblique", Generic.GenericReflectivityAt90deg },
                                { "Transparency", Generic.GenericTransparency }, { "Refraction", Generic.GenericRefractionIndex },
                                { "Translucency", Generic.GenericRefractionTranslucencyWeight }, { "Luminance", Generic.GenericSelfIllumLuminance },
                                { "Colour Temperature", Generic.GenericSelfIllumColorTemperature }, { "Bump Amount", Generic.GenericBumpAmount },
                            };
                            var held = numberAt(names[pair.Key]);
                            if (held == null || Math.Abs(held.Value - (double)v) > 1e-4)
                            {
                                seen = held == null ? "?" : held.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                                meant = ((double)v).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            break;
                    }
                    if (seen != null && !string.Equals(seen, meant, StringComparison.OrdinalIgnoreCase))
                        wrong.Add(string.Format("'{0}' {1} reads {2}, not {3}", m.Name, pair.Key, seen, meant));
                }

            lines.Add(string.Format("{0}: '{1}' ({2}) -> '{3}' ({4})", m.Name, wasName, was,
                nowElement == null ? "none" : nowElement.Name, describe(now)));
            changedMaterialIds.Add(m.Id);
        }
        if (wrong.Count > 0)
            throw new InvalidOperationException("Not every appearance value read back as asked: " + string.Join("; ", wrong)
                + ". Nothing from this call was kept");

        appearanceReport = string.Join("  ||  ", lines);
        sharedBefore = string.Join("; ", shareNotes);
        findings.Add(string.Format("{0} material(s) given their appearance; {1} new appearance asset(s)",
            changedMaterialIds.Count, assetsMade.Count));
        if (shareNotes.Count > 0) findings.Add("Shared assets were not edited: " + sharedBefore);
        findings.AddRange(notes);
        findings.Add("A Realistic view and a rendering draw this appearance. A Shaded view still draws the shading colour unless "
            + "Use Render Appearance is on - SET_MATERIAL_PROPERTIES sets it");
        if (assetsMade.Count > 0)
            findings.Add("An appearance asset no material uses any more stays in the document until it is purged");
    }
}
