// NOT STANDALONE. Assumes `doc`, `withAssets`, `withAllTabs`,
// `withLibraryAppearances` and `namePattern` are in scope; leaves
// `materialCount`, `materialList`, `materialClasses`, `materialIds`,
// `libraryAppearances` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ONE COLLECTOR OVER Material AND NOTHING ELSE. That is the whole cost: no
// element, type or face is read. Whether anything USES a material is
// FIND_UNUSED_MATERIALS' question, and answering it means reading every
// element, every type and every painted face - a walk this deliberately does
// not make, so a cheap list never quietly becomes a slow one. How long it
// takes on a large model is not measured (NEEDS-CHECKING CX6).
//
// `doc` AND ONLY IT. Whatever document the add-in hands in - through a chat,
// the model that chat is pinned to, which the answer names - and in a family
// that is the family's own materials: never the project it will be loaded
// into, and never another document open beside it.
//
// SORTED BY NAME, IGNORING CASE - meant to be the Material Browser's order,
// so the two can be compared line by line. That they agree is not measured
// (NEEDS-CHECKING CX5).
//
// THE ID IS PRINTED, NEVER READ AS A NUMBER. `ElementId.ToString()` is on
// every release; `IntegerValue` is deprecated at 2024 and gone by 2026.
//
// WHAT A PERSON MUST READ GOES IN A STRING. The add-in reports a collection as
// its count and its first three items, each cut at 60 characters
// (RevitFragment.Describe and ShortName); a string arrives whole. So the list
// and the count per class are strings, and each finding puts its words first.
//
// THE WORKING LISTS ARE INSIDE A BLOCK. The add-in reports every top-level
// variable a fragment leaves (RevitFragment.Report), so a helper list or the
// per-class tally declared out here would arrive on the answer beside the
// names above - the tally as "N entry(ies)", which the server flags as content
// NOT SENT.

var materialCount = 0;
var materialList = "";
var materialClasses = "";
var materialIds = new List<ElementId>();
var libraryAppearances = "";
var findings = new List<string>();

{
    var found = new List<Material>();

    // ONLY THE NAMES A PATTERN MATCHES, when one is given (version 3) - "TRG_*".
    // Left out it reads "all", and every material is listed as before.
    var pattern = (namePattern ?? "").Trim();
    System.Text.RegularExpressions.Regex wanted = null;
    if (pattern.Length > 0 && !string.Equals(pattern, "all", StringComparison.OrdinalIgnoreCase))
        wanted = new System.Text.RegularExpressions.Regex("^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    foreach (var material in new FilteredElementCollector(doc)
                                 .OfClass(typeof(Material))
                                 .Cast<Material>())
    {
        if (material != null && (wanted == null || wanted.IsMatch(material.Name))) found.Add(material);
    }

    // EVERY TAB (version 3, 2026-10-20). What each line gains, in the Material
    // Browser's own words: the Identity fields that hold anything, the Graphics
    // tab - shading colour, transparency, Use Render Appearance, and the four
    // surface and cut patterns with their colours - and the Appearance tab:
    // the asset's name, how many other materials share it, and for a Generic
    // one its colour, image, glossiness, highlights and transparency. Physical
    // and thermal come with them, as withAssets reads them. Read only.
    Func<Color, string> colourText = colour =>
        colour == null || !colour.IsValid ? "none" : colour.Red + "," + colour.Green + "," + colour.Blue;
    Func<ElementId, string> nameOf = id =>
    {
        if (id == null || id == ElementId.InvalidElementId) return "none";
        var element = doc.GetElement(id);
        return element == null ? "none" : element.Name;
    };
    Func<Asset, string, double?> numberIn = (asset, name) =>
    {
        var property = asset.FindByName(name);
        var d = property as AssetPropertyDouble;
        if (d != null) return d.Value;
        var f = property as AssetPropertyFloat;
        return f == null ? (double?)null : f.Value;
    };
    Func<double?, string> percent = value => value == null ? "?" : Math.Round(value.Value * 100).ToString();
    var appearanceUsers = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var each in new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>())
    {
        if (each.AppearanceAssetId == ElementId.InvalidElementId) continue;
        var key = each.AppearanceAssetId.ToString();
        int users;
        appearanceUsers.TryGetValue(key, out users);
        appearanceUsers[key] = users + 1;
    }
    Func<Material, string> allTabs = material =>
    {
        var parts = new List<string>();
        var identity = new List<string>();
        foreach (var field in new[] {
            Tuple.Create("description", BuiltInParameter.ALL_MODEL_DESCRIPTION),
            Tuple.Create("comments", BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS),
            Tuple.Create("manufacturer", BuiltInParameter.ALL_MODEL_MANUFACTURER),
            Tuple.Create("model", BuiltInParameter.ALL_MODEL_MODEL),
            Tuple.Create("cost", BuiltInParameter.ALL_MODEL_COST),
            Tuple.Create("URL", BuiltInParameter.ALL_MODEL_URL),
            Tuple.Create("keynote", BuiltInParameter.KEYNOTE_PARAM),
            Tuple.Create("mark", BuiltInParameter.ALL_MODEL_MARK) })
        {
            var p = material.get_Parameter(field.Item2);
            var text = p == null || !p.HasValue ? "" : p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
            if (!string.IsNullOrEmpty(text)) identity.Add(field.Item1 + " '" + text + "'");
        }
        var keywords = material.LookupParameter("Keywords");
        if (keywords != null && keywords.StorageType == StorageType.String && !string.IsNullOrEmpty(keywords.AsString()))
            identity.Add("keywords '" + keywords.AsString() + "'");
        parts.Add("identity " + (identity.Count == 0 ? "empty" : string.Join(", ", identity)));

        parts.Add(string.Format("graphics shading {0} at {1}% transparency{2}; surface {3} {4} / {5} {6}; cut {7} {8} / {9} {10}",
            colourText(material.Color), material.Transparency, material.UseRenderAppearanceForShading ? ", using the render appearance" : "",
            nameOf(material.SurfaceForegroundPatternId), colourText(material.SurfaceForegroundPatternColor),
            nameOf(material.SurfaceBackgroundPatternId), colourText(material.SurfaceBackgroundPatternColor),
            nameOf(material.CutForegroundPatternId), colourText(material.CutForegroundPatternColor),
            nameOf(material.CutBackgroundPatternId), colourText(material.CutBackgroundPatternColor)));

        var element = doc.GetElement(material.AppearanceAssetId) as AppearanceAssetElement;
        if (element == null) parts.Add("appearance NONE - draws flat grey in Realistic and renders");
        else
        {
            int users;
            appearanceUsers.TryGetValue(material.AppearanceAssetId.ToString(), out users);
            var shared = users > 1 ? ", shared with " + (users - 1) + " other material(s)" : "";
            var asset = element.GetRenderingAsset();
            var diffuse = asset == null ? null : asset.FindByName(Generic.GenericDiffuse);
            if (diffuse == null)
            {
                var schema = asset == null ? null : asset.FindByName(SchemaCommon.BaseSchema) as AssetPropertyString;
                parts.Add(string.Format("appearance '{0}' ({1}){2}", element.Name, schema == null ? "not Generic" : schema.Value, shared));
            }
            else
            {
                var image = "none";
                if (diffuse.NumberOfConnectedProperties > 0)
                {
                    var bitmap = diffuse.GetSingleConnectedAsset();
                    var source = bitmap == null ? null : bitmap.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) as AssetPropertyString;
                    image = source == null ? "a map" : "'" + source.Value + "'";
                }
                var colour = diffuse as AssetPropertyDoubleArray4d;
                var metal = asset.FindByName(Generic.GenericIsMetal) as AssetPropertyBoolean;
                parts.Add(string.Format("appearance '{0}' (Generic{1}) colour {2}, image {3}, glossiness {4}, {5}, transparency {6}",
                    element.Name, shared, colour == null ? "?" : colourText(colour.GetValueAsColor()), image,
                    percent(numberIn(asset, Generic.GenericGlossiness)),
                    metal == null ? "?" : metal.Value ? "metallic" : "non-metallic",
                    percent(numberIn(asset, Generic.GenericTransparency))));
            }
        }
        return string.Join("; ", parts);
    };

    found.Sort(delegate (Material a, Material b)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
    });

    var rows = new List<string>();
    var byClass = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    foreach (var material in found)
    {
        materialIds.Add(material.Id);

        // An empty class is a real state - a material made by hand and never
        // given one - and it is said rather than left blank or left out.
        var materialClass = (material.MaterialClass ?? "").Trim();
        var shown = materialClass.Length > 0 ? materialClass : "no class";

        var row = string.Format("{0} ({1}, id {2})", material.Name, shown, material.Id);

        // THE PHYSICAL AND THERMAL ASSETS, when asked (version 2, 2026-10-13,
        // the gap fill): every number each asset holds, as Revit formats it
        // in the project's own units - no unit is converted here, so none can
        // be converted wrongly. Names are Revit's, in the language it runs in.
        if (withAllTabs) row += "; " + allTabs(material);

        if (withAssets || withAllTabs)
        {
            var assets = new[] { Tuple.Create("physical", material.StructuralAssetId), Tuple.Create("thermal", material.ThermalAssetId) };
            foreach (var asset in assets)
            {
                var set = doc.GetElement(asset.Item2) as PropertySetElement;
                if (set == null) continue;
                var values = new List<string>();
                foreach (Parameter p in set.Parameters)
                {
                    if (p.StorageType != StorageType.Double || !p.HasValue) continue;
                    var text = p.AsValueString();
                    if (string.IsNullOrEmpty(text) || Math.Abs(p.AsDouble()) < 1e-12) continue;
                    values.Add(p.Definition.Name + " " + text);
                }
                values.Sort(StringComparer.OrdinalIgnoreCase);
                if (values.Count > 0) row += "; " + asset.Item1 + " \"" + set.Name + "\": " + string.Join(", ", values.Take(10));
            }
        }
        rows.Add(row);

        int already;
        byClass.TryGetValue(shown, out already);
        byClass[shown] = already + 1;
    }

    materialCount = found.Count;
    materialList = string.Join("  ||  ", rows);

    var classes = new List<string>();
    foreach (var pair in byClass) classes.Add(pair.Key + " " + pair.Value);
    materialClasses = string.Join(", ", classes);

    var kind = doc.IsFamilyDocument ? "family" : "project";

    if (wanted != null)
        findings.Add(materialCount == 0
            ? string.Format("No material matches '{0}' in this {1} - '{2}'", pattern, kind, doc.Title)
            : string.Format("{0} material(s) match '{1}' in this {2} - '{3}'", materialCount, pattern, kind, doc.Title));
    else
        findings.Add(materialCount == 0
            ? string.Format("No materials at all in this {0} - '{1}'", kind, doc.Title)
            : string.Format("{0} material(s) in this {1} - '{2}'", materialCount, kind, doc.Title));

    findings.Add("Listed used or not - unused ones: FIND_UNUSED_MATERIALS");

    // REVIT'S APPEARANCE LIBRARY - what SET_MATERIAL_APPEARANCE and
    // CREATE_STARTER_MATERIALS take by name. What it holds depends on the PC's
    // install, so it is read, never assumed. One line, sorted, each with its
    // kind.
    //
    // BY REVIT'S DESCRIPTION WHERE IT NAMES THE LOOK, AND BY ITS CODE ALWAYS
    // (version 4, row 5b-1272). Version 3 listed the UIName, measured
    // 2026-10-20 on Revit 2020 (Group IA, IA1) to be Revit's code for every
    // library asset - "ACADGen-001", "Prism-256" - while Title is empty there.
    // A Prism asset's description is OFTEN the Asset Browser's name in another
    // case - "Copper - polished" shows as "Copper - Polished" - and NOT ALWAYS:
    // Prism-152's "Glass - clear" shows as "Glass (Clear)" (seen by the owner
    // the same day). An older asset's description is a sentence its kind
    // shares ("Generic material." on 557), so it is listed by its code alone
    // and the finding says how many. Assets Revit marks Hidden (1,162 of
    // 3,088 there) are left out, counted - the tools still take one by code.
    if (withLibraryAppearances)
    {
        var shown = new List<string>();
        int byName = 0, byCode = 0, hiddenCount = 0;
        foreach (var asset in doc.Application.GetAssets(AssetType.Appearance) ?? new List<Asset>())
        {
            var hidden = asset.FindByName(SchemaCommon.Hidden) as AssetPropertyBoolean;
            if (hidden != null && hidden.Value) { hiddenCount++; continue; }
            var schema = asset.FindByName(SchemaCommon.BaseSchema) as AssetPropertyString;
            var description = asset.FindByName(SchemaCommon.Description) as AssetPropertyString;
            var code = asset.Name ?? "";
            var name = !string.IsNullOrEmpty(asset.Title) ? asset.Title
                : description != null && !string.IsNullOrEmpty(description.Value) && schema != null && schema.Value != null
                    && schema.Value.StartsWith("Prism", StringComparison.OrdinalIgnoreCase) ? description.Value
                : null;
            var assetKind = asset.FindByName(Generic.GenericDiffuse) != null ? "Generic" : schema == null ? "?" : schema.Value;
            if (name == null || string.Equals(name, code, StringComparison.OrdinalIgnoreCase)) { byCode++; shown.Add(code + " (" + assetKind + ")"); }
            else { byName++; shown.Add(name + " [" + code + "] (" + assetKind + ")"); }
        }
        shown.Sort(StringComparer.OrdinalIgnoreCase);
        libraryAppearances = string.Join("  ||  ", shown);
        findings.Add(string.Format("{0} appearance asset(s) in Revit's appearance library on this PC - {1} by Revit's description of the look, "
            + "most often the name the Asset Browser shows but not always, with Revit's code in [ ]; {2} by Revit's code alone, because this "
            + "Revit gives no other name for them; {3} Revit marks hidden are left out. SET_MATERIAL_APPEARANCE takes the description or the "
            + "code - the code is the sure one", shown.Count, byName, byCode, hiddenCount));
    }
}
