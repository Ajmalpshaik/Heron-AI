// NOT STANDALONE. Assumes `doc`, `folder` and `sinceMinutes` are in scope;
// leaves `renderings`, `report` and `findings` behind.
//
// NO TRANSACTION. It reads the project and, when a folder is named, the files
// in it - nothing is written anywhere.
//
// WHAT A RENDER LEFT BEHIND, READ BACK. Revit's Render dialog keeps a finished
// render in two places, and this reads both:
//   - Save to Project makes a view under Renderings (ViewType Rendering); its
//     image's pixel size is read off the image type, as Revit's own
//     RASTER_SYMBOL_PIXELWIDTH and _PIXELHEIGHT parameters give it
//   - Export writes an image file; in the folder named, every png, jpg, jpeg,
//     bmp, tif, tiff and tga written in the last `sinceMinutes` minutes (all,
//     when left out) is listed newest first, with its pixel size read out of
//     the file's own header - png, jpg, bmp and tga; a tif's is not read
//
// NOTHING IS CALLED RENDERED THAT WAS NOT READ BACK. No Revit release lets an
// add-in run the render (OPEN_RENDER_DIALOG has the measurement), so this is the
// only evidence a render happened - and with nothing found it says so.
//
// ONE STRING, ROWS JOINED BY "  ||  ", because a list reaches the chat cut to
// three entries (READ_VIEW_SCALE's reason).

var findings = new List<string>();
var renderings = 0;
var report = "";
var rows = new List<string>();
var culture = System.Globalization.CultureInfo.InvariantCulture;

// ---- saved to the project ----
Func<Element, BuiltInParameter, string> value = (element, which) =>
{
    var p = element == null ? null : element.get_Parameter(which);
    if (p == null || !p.HasValue) return null;
    if (p.StorageType == StorageType.Integer) return p.AsInteger().ToString(culture);
    if (p.StorageType == StorageType.Double) return Math.Round(p.AsDouble()).ToString(culture);
    return p.AsValueString() ?? p.AsString();
};
var saved = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
    .Where(v => !v.IsTemplate && v.ViewType == ViewType.Rendering).OrderBy(v => v.Name).ToList();
foreach (var v in saved)
{
    Element image = null;
    var imageView = v as ImageView;
    if (imageView != null && imageView.ImageInstanceId != ElementId.InvalidElementId)
        image = doc.GetElement(imageView.ImageInstanceId);
    if (image == null)
    {
        try { image = new FilteredElementCollector(doc, v.Id).OfClass(typeof(ImageInstance)).FirstElement(); }
        catch (Exception) { image = null; }
    }
    var type = image == null ? null : doc.GetElement(image.GetTypeId());
    var width = value(type, BuiltInParameter.RASTER_SYMBOL_PIXELWIDTH);
    var height = value(type, BuiltInParameter.RASTER_SYMBOL_PIXELHEIGHT);
    rows.Add("saved rendering '" + v.Name + "'" + (width != null && height != null ? " " + width + " x " + height + " px"
        : image == null ? " (no image found in it)" : " (its pixel size is not given)"));
    renderings++;
}

// ---- exported to a folder ----
var where = (folder ?? "").Trim().Trim('"');
var lookInFolder = where.Length > 0 && !string.Equals(where, "none", StringComparison.OrdinalIgnoreCase);
double minutes = 0;
var since = DateTime.MinValue;
if (!string.IsNullOrEmpty((sinceMinutes ?? "").Trim()) && !string.Equals(sinceMinutes.Trim(), "all", StringComparison.OrdinalIgnoreCase))
{
    if (double.TryParse(sinceMinutes.Trim(), System.Globalization.NumberStyles.Float, culture, out minutes) && minutes > 0)
        since = DateTime.Now.AddMinutes(-minutes);
    else findings.Add("'" + sinceMinutes + "' is not a number of minutes, so every image in the folder was listed");
}

// The pixel size out of a file's own header, or null.
Func<string, string> pixels = path =>
{
    try
    {
        var head = new byte[64 * 1024];
        int read;
        using (var stream = System.IO.File.OpenRead(path)) read = stream.Read(head, 0, head.Length);
        Func<int, int> be16 = i => (head[i] << 8) | head[i + 1];
        Func<int, int> le16 = i => head[i] | (head[i + 1] << 8);
        Func<int, int> be32 = i => (head[i] << 24) | (head[i + 1] << 16) | (head[i + 2] << 8) | head[i + 3];
        Func<int, int> le32 = i => head[i] | (head[i + 1] << 8) | (head[i + 2] << 16) | (head[i + 3] << 24);
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (read >= 24 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
            return be32(16) + " x " + be32(20) + " px";
        if (read >= 26 && head[0] == 0x42 && head[1] == 0x4D)
            return le32(18) + " x " + Math.Abs(le32(22)) + " px";
        if (read >= 4 && head[0] == 0xFF && head[1] == 0xD8)
        {
            var i = 2;
            while (i + 9 < read)
            {
                if (head[i] != 0xFF) { i++; continue; }
                var marker = head[i + 1];
                if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                    return be16(i + 7) + " x " + be16(i + 5) + " px";
                i += 2 + be16(i + 2);
            }
            return null;
        }
        if (ext == ".tga" && read >= 18) return le16(12) + " x " + le16(14) + " px";
        return null;
    }
    catch (Exception) { return null; }
};

var files = 0;
if (lookInFolder)
{
    if (!System.IO.Directory.Exists(where))
        findings.Add("The folder '" + where + "' does not exist on this PC, so no image file was read");
    else
    {
        var kinds = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".tga" };
        var found = new System.IO.DirectoryInfo(where).GetFiles()
            .Where(f => kinds.Contains(f.Extension.ToLowerInvariant()) && f.LastWriteTime >= since)
            .OrderByDescending(f => f.LastWriteTime).ToList();
        foreach (var f in found)
        {
            var size = pixels(f.FullName);
            rows.Add("image file '" + f.Name + "' " + (size ?? "(pixel size not read from this kind of file)") + ", "
                + Math.Round(f.Length / 1024.0) + " KB, written " + f.LastWriteTime.ToString("yyyy-MM-dd HH:mm", culture));
            files++;
        }
        renderings += files;
    }
}

report = rows.Count == 0
    ? "Nothing rendered was read back: no view under Renderings in this project"
        + (lookInFolder ? " and no image file in '" + where + "'" + (since > DateTime.MinValue ? " from the last " + minutes
            + " minute(s)" : "") : "") + "."
    : string.Join("  ||  ", rows);
findings.Add(saved.Count + " saved rendering(s) in the project" + (lookInFolder ? ", " + files + " image file(s) in '"
    + where + "'" + (since > DateTime.MinValue ? " from the last " + minutes + " minute(s)" : "") : "")
    + (rows.Count == 0 ? " - nothing has been rendered as far as Revit and this folder show" : ""));
