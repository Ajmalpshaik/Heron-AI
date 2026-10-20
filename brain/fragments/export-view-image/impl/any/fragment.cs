// NOT STANDALONE. Assumes `doc`, `views`, `folder`, `filePrefix`,
// `pixelWidth`, `fileType` and `dpi` are in scope; leaves `exported`,
// `filesWritten`, `fileReport` and `findings` behind.
//
// PUBLISH, NOT MODIFY. This leaves the model alone and puts files where other
// people can find them, which is a higher bar than changing a duct.
//
// NO TRANSACTION, AND THAT IS NOT AN OVERSIGHT. An export reads the model and
// writes a file; there is nothing to undo.
//
// REVIT NAMES THE FILES ITSELF. The prefix is a stem and Revit appends the view
// type and name, so the names are NOT predicted - version 2 READS the folder
// afterwards and lists the files that carry the prefix and were written by this
// run, each with its pixel size out of the file's own header. A predicted list
// that turned out wrong would be worse than none; a list read off the disk is
// evidence.
//
// A VIEW THAT CANNOT BE PRINTED IS NAMED, not dropped. A folder with fewer
// files than views and nothing saying which are missing is the failure.
//
// VERSION 2 (2026-10-20): the file type - PNG as before, JPG, TIFF, BMP or TGA
// - and the dots per inch (72, 150, 300 or 600). With a DPI and no pixel width
// the image is Revit's "Zoom to 100%" at that DPI; with a pixel width it fits
// that width as version 1 did, and the DPI is set beside it. Left out, both
// behave exactly as version 1: PNG, fitted to pixelWidth (1920 when not given).

var exported = 0;
var filesWritten = 0;
var fileReport = "";
var findings = new List<string>();
var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
var types = new Dictionary<string, ImageFileType>
{
    { "png", ImageFileType.PNG }, { "jpg", ImageFileType.JPEGLossless }, { "jpeg", ImageFileType.JPEGLossless },
    { "jpeglossless", ImageFileType.JPEGLossless }, { "jpegmedium", ImageFileType.JPEGMedium },
    { "jpgmedium", ImageFileType.JPEGMedium }, { "jpegsmallest", ImageFileType.JPEGSmallest },
    { "jpgsmallest", ImageFileType.JPEGSmallest }, { "tif", ImageFileType.TIFF }, { "tiff", ImageFileType.TIFF },
    { "bmp", ImageFileType.BMP }, { "tga", ImageFileType.TARGA }, { "targa", ImageFileType.TARGA },
};
var resolutions = new Dictionary<string, ImageResolution>
{
    { "72", ImageResolution.DPI_72 }, { "150", ImageResolution.DPI_150 }, { "300", ImageResolution.DPI_300 },
    { "600", ImageResolution.DPI_600 },
};
var typeWord = string.IsNullOrEmpty((fileType ?? "").Trim()) ? "png" : squash(fileType);
var dpiWord = squash(dpi).Replace("dpi", "");
var wantDpi = dpiWord.Length > 0 && dpiWord != "keep";

// The pixel size out of a file's own header - png, jpg, bmp, tga - or null.
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
        if (System.IO.Path.GetExtension(path).ToLowerInvariant() == ".tga" && read >= 18)
            return le16(12) + " x " + le16(14) + " px";
        return null;
    }
    catch (Exception) { return null; }
};

if (views == null || views.Count == 0)
{
    findings.Add("No views were given to export");
}
else if (string.IsNullOrEmpty(folder))
{
    findings.Add("No folder was given - an export has to be told where to put the files");
}
else if (!types.ContainsKey(typeWord))
{
    findings.Add("No image type is called '" + fileType + "'. Say PNG, JPG (or JPEG Medium, JPEG Smallest), TIFF, BMP "
        + "or TGA. Nothing was written");
}
else if (wantDpi && !resolutions.ContainsKey(dpiWord))
{
    findings.Add("Revit exports images at 72, 150, 300 or 600 DPI - not '" + dpi + "'. Nothing was written");
}
else
{
    var printable = new List<ElementId>();
    var refusedNames = new List<string>();

    foreach (var view in views)
    {
        if (view == null) continue;

        if (view.IsTemplate || !view.CanBePrinted)
        {
            refusedNames.Add(string.Format("'{0}'{1}", view.Name, view.IsTemplate ? " (a template)" : ""));
            continue;
        }

        printable.Add(view.Id);
    }

    if (printable.Count == 0)
    {
        findings.Add(string.Format("None of the {0} view(s) given can be exported as an image. "
            + "Templates cannot, and neither can some view types: {1}",
            views.Count, refusedNames.Count == 0 ? "" : string.Join(", ", refusedNames.ToArray())));
    }
    else
    {
        var prefix = string.IsNullOrEmpty(filePrefix) ? "view" : filePrefix;
        var zoomAt100 = wantDpi && pixelWidth <= 0;
        var width = pixelWidth > 0 ? pixelWidth : 1920;
        var type = types[typeWord];
        var started = DateTime.Now.AddSeconds(-2);

        try
        {
            System.IO.Directory.CreateDirectory(folder);

            var options = new ImageExportOptions();
            options.FilePath = System.IO.Path.Combine(folder, prefix);
            options.ExportRange = ExportRange.SetOfViews;
            options.HLRandWFViewsFileType = type;
            options.ShadowViewsFileType = type;
            if (wantDpi) options.ImageResolution = resolutions[dpiWord];
            if (zoomAt100)
            {
                options.ZoomType = ZoomFitType.Zoom;
                options.Zoom = 100;
            }
            else
            {
                options.ZoomType = ZoomFitType.FitToPage;
                options.PixelSize = width;
                options.FitDirection = FitDirectionType.Horizontal;
            }
            options.SetViewsAndSheets(printable);

            doc.ExportImage(options);
            exported = printable.Count;

            findings.Add(string.Format("{0} view(s) sent to Revit's image export as {1}, {2}, into '{3}' with the "
                + "prefix '{4}'", exported, type, zoomAt100 ? "zoomed to 100% at " + dpiWord + " DPI"
                    : width + " px wide" + (wantDpi ? " at " + dpiWord + " DPI" : ""), folder, prefix));

            // What landed on the disk, read back rather than predicted.
            var landed = new System.IO.DirectoryInfo(folder).GetFiles(prefix + "*")
                .Where(f => f.LastWriteTime >= started).OrderBy(f => f.Name).ToList();
            filesWritten = landed.Count;
            fileReport = landed.Count == 0
                ? "No file starting '" + prefix + "' was written to '" + folder + "' by this run"
                : string.Join("  ||  ", landed.Select(f => "'" + f.Name + "' " + (pixels(f.FullName) ?? "(pixel size not "
                    + "read from this kind of file)") + ", " + Math.Round(f.Length / 1024.0).ToString(culture) + " KB"));
            findings.Add(filesWritten + " file(s) read back from the folder" + (filesWritten < exported
                ? " - FEWER THAN THE VIEWS SENT; look in '" + folder + "'" : ""));
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("The export failed: {0}. Nothing was written, or only part of "
                + "it was - look in '{1}' before running it again", ex.Message, folder));
        }
    }

    if (refusedNames.Count > 0)
        findings.Add(string.Format("{0} view(s) could NOT be exported and are named so the file count "
            + "can be reconciled: {1}", refusedNames.Count, string.Join(", ", refusedNames.ToArray())));
}
