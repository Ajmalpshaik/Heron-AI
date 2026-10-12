// NOT STANDALONE. Assumes `doc` and `onlyMissing` are in scope; leaves
// `textures`, `missing`, `materialCount` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE IMAGE FILES BEHIND EVERY MATERIAL'S APPEARANCE - its diffuse picture,
// bump, cutout and the rest - slot by slot, with the stored path and whether a
// file is there. A missing texture renders as a plain colour and travels with
// the model to every consultant, so the missing ones are listed apart.
//
// THE APPEARANCE ASSET IS READ WITHOUT NAMING ITS TYPES. Asset types live in a
// namespace the fragment contract does not supply (see the card), so every
// value is taken by `var`, and a bitmap property's text is read by its
// property name, Value. Nothing is changed: repathing or removing a texture
// needs that namespace, and is not offered here.
//
// A PATH WITH NO DRIVE OR SHARE is relative to Revit's own texture folders and
// the render appearance paths in Options; those are not readable here, so such
// a path is reported as relative, never as missing.

var lines = new List<string>();
var missing = new List<string>();
var findings = new List<string>();
var materialCount = 0;
var slotCount = 0;

Func<object, string> textOf = property =>
{
    try
    {
        var value = property.GetType().GetProperty("Value");
        return value == null ? null : value.GetValue(property, null) as string;
    }
    catch { return null; }
};

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Material)))
{
    var material = element as Material;
    if (material == null || material.AppearanceAssetId == ElementId.InvalidElementId) continue;
    var appearance = doc.GetElement(material.AppearanceAssetId) as AppearanceAssetElement;
    if (appearance == null) continue;

    var asset = appearance.GetRenderingAsset();
    if (asset == null) continue;
    var counted = false;

    for (var i = 0; i < asset.Size; i++)
    {
        var slot = asset.Get(i);
        if (slot == null || slot.NumberOfConnectedProperties == 0) continue;
        var connected = slot.GetSingleConnectedAsset();
        if (connected == null) continue;
        var bitmap = connected.FindByName("unifiedbitmap_Bitmap");
        if (bitmap == null) continue;
        var stored = textOf(bitmap);
        if (string.IsNullOrEmpty(stored)) continue;

        if (!counted) { materialCount++; counted = true; }
        foreach (var raw in stored.Split('|'))
        {
            var path = raw.Trim();
            if (path == "") continue;
            slotCount++;
            string state;
            var rooted = System.IO.Path.IsPathRooted(path);
            if (!rooted) state = "relative - looked up in Revit's texture folders, not checked here";
            else if (System.IO.File.Exists(path)) state = "found";
            else
            {
                state = "MISSING";
                missing.Add(material.Name + " / " + slot.Name + ": " + path);
            }
            if (!onlyMissing || state == "MISSING")
                lines.Add(material.Name + " | " + slot.Name + " | " + path + " | " + state);
        }
    }
}

var textures = string.Join("\n", lines);
findings.Add(materialCount + " material(s) use " + slotCount + " texture file(s); " + missing.Count
    + " stored with a full path where no file is.");
