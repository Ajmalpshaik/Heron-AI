// NOT STANDALONE. Assumes `doc`, `names`, `fromPath` and `page` are in scope,
// and leaves `reloaded`, `reloadedIds`, `skipped`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. One
// call is one undo entry.
//
// WHAT IT DOES. Revit's Manage > Manage Images, its two buttons:
//
//   Reload        each image named - or, with none named, every image loaded
//                 from a file - read again from its own file, with its own
//                 page and resolution (ImageType.Reload)
//   Reload From   ONE image named, read from another file (`fromPath`) and,
//                 for a PDF, another page (ImageType.ReloadFrom)
//
// Every placement of the image shows the reloaded file; nothing is placed,
// moved or deleted.
//
// NOTHING IS GUESSED. A name with no image is named with the names there are.
// The new file is the modeller's, read on the PC Revit runs on, so a path
// with no file is found here, at run time, and said in those words. A file
// Revit cannot read leaves the image as it was - Revit's own rule - and the
// image is named in `skipped`.
//
// LEFT OUT ARRIVES AS TEXT. `names` left out binds "all", and `fromPath` left
// out binds "none" (absent: all / none - row 5b-1432); both are read as left
// out.
//
// READ BACK, AFTER A REGENERATE: each image looked up again by id; on 2021 on
// its status and pixel size before and after, and one that reads Failed to
// load is NOT counted. Reload From reads back the image's file name and page.
//
// RELEASES (tools/api-surface --members, 2026-11-02). ImageType Reload,
// ReloadFrom(ImageTypeOptions), CanReload, Path, PathType and PageNumber, and
// ImageTypeOptions PageNumber, Resolution and IsValid are the same 2020 to
// 2027. ImageType Source, Status, Resolution, WidthInPixels and
// HeightInPixels, ImageTypeSource, ImageTypeStatus and the ImageTypeOptions
// constructor (string, bool, ImageTypeSource) are 2021 on. So 2020 reloads
// without reading status or size, and REFUSES Reload From: its image's DPI
// cannot be read, and loading the new file at Revit's default 72 DPI could
// change its size.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var reloaded = 0;
var reloadedIds = new List<ElementId>();
var skipped = new List<string>();
var report = "";
var findings = new List<string>();

var lines = new List<string>();
string refused = null;

var wanted = (names ?? "").Trim();
if (string.Equals(wanted, "all", StringComparison.OrdinalIgnoreCase)) wanted = "";
var newFile = (fromPath ?? "").Trim().Trim('"').Trim();
if (string.Equals(newFile, "none", StringComparison.OrdinalIgnoreCase)) newFile = "";

// ---- every image in Manage Images, by name
var images = new List<ImageType>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImageType)))
{
    var one = element as ImageType;
    if (one != null) images.Add(one);
}
images.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));
Func<string> namesHere = () =>
{
    if (images.Count == 0) return "Manage Images is empty.";
    var quoted = images.Take(12).Select(i => "\"" + (i.Name ?? "") + "\"").ToList();
    return "The images here are " + string.Join(", ", quoted)
        + (images.Count > 12 ? " and " + (images.Count - 12) + " more" : "") + ".";
};

// ---- which images
var chosen = new List<ImageType>();
var missingNames = new List<string>();
if (wanted == "") chosen.AddRange(images);
else
{
    foreach (var raw in wanted.Split(';'))
    {
        var name = raw.Trim().Trim('"').Trim();
        if (name == "") continue;
        var match = images.FirstOrDefault(i => string.Equals(i.Name ?? "", name, StringComparison.OrdinalIgnoreCase));
        if (match == null) { missingNames.Add(name); continue; }
        if (!chosen.Any(c => c.Id == match.Id)) chosen.Add(match);
    }
    foreach (var name in missingNames)
        lines.Add("No image is named \"" + name + "\" in Manage Images. " + namesHere());
}
if (images.Count == 0) refused = "No image or PDF page is in this model's Manage Images. NOTHING WAS RELOADED.";
else if (wanted != "" && chosen.Count == 0) refused = "None of the names given is an image here. NOTHING WAS RELOADED.";

// ---- Reload From: one image, a file that is there
var isPdf = false;
if (refused == null && newFile != "")
{
#if REVIT2020
    refused = "Revit 2020 cannot read an image's resolution, so Reload From here would load the new file at "
        + "Revit's default 72 DPI and could change its size. Use Manage Images > Reload From in Revit 2020. "
        + "NOTHING WAS RELOADED.";
#else
    var extension = "";
    try { extension = (System.IO.Path.GetExtension(newFile) ?? "").ToLowerInvariant(); }
    catch (Exception) { extension = ""; }
    isPdf = extension == ".pdf";
    var picture = extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp"
        || extension == ".tif" || extension == ".tiff";
    if (wanted == "" || chosen.Count != 1 || missingNames.Count > 0)
        refused = "Reload From takes ONE image, named exactly as Manage Images shows it - "
            + (wanted == "" ? "none was named" : chosen.Count + missingNames.Count + " were named") + ". "
            + namesHere() + " NOTHING WAS RELOADED.";
    else if (!isPdf && !picture)
        refused = "\"" + newFile + "\" is not a PDF, PNG, JPG, BMP or TIFF - the files Manage Images reloads from. "
            + "NOTHING WAS RELOADED.";
    else if (!System.IO.File.Exists(newFile))
        refused = "There is no file at \"" + newFile + "\" on the PC Revit runs on - the path is read there, "
            + "not where the chat is. NOTHING WAS RELOADED.";
    else if (isPdf && page < 0)
        refused = "Page " + page + " is not a page - PDF pages are counted from 1. NOTHING WAS RELOADED.";
#endif
}
else if (refused == null && page != 0)
    lines.Add("A page was given without a new file; Reload keeps each image's own page, so the page was not used.");

// ---- reload
if (refused == null)
{
    foreach (var image in chosen)
    {
        var name = image.Name ?? "";
        var label = "\"" + name + "\" (" + image.Id + ")";
        var file = "";
        try { file = image.Path ?? ""; } catch (Exception) { file = ""; }
        var before = "";
#if !REVIT2020
        try
        {
            if (image.Source == ImageTypeSource.Internal)
            {
                skipped.Add(label + ": saved in the project (a rendering), not loaded from a file - nothing to reload.");
                continue;
            }
        }
        catch (Exception) { }
        try { before = image.Status + ", " + image.WidthInPixels + " x " + image.HeightInPixels + " px"; }
        catch (Exception) { before = ""; }
#endif
        try
        {
            if (newFile == "")
            {
                if (file == "") { skipped.Add(label + ": has no file path - nothing to reload from."); continue; }
                if (!image.CanReload())
                {
                    skipped.Add(label + ": Revit cannot reload it from \"" + file + "\" - the file is not found, cannot be "
                        + "read, or has not got page " + image.PageNumber + ". Left as it was.");
                    continue;
                }
                image.Reload();
            }
            else
            {
#if !REVIT2020
                var keepPage = 1;
                try { keepPage = image.PageNumber; } catch (Exception) { keepPage = 1; }
                var wantPage = isPdf ? (page > 0 ? page : (keepPage > 0 ? keepPage : 1)) : 1;
                if (!isPdf && page > 1)
                    lines.Add("A page was given, but \"" + System.IO.Path.GetFileName(newFile) + "\" is a picture with "
                        + "one page; the page was not used.");
                var relative = false;
                try { relative = image.PathType == PathType.Relative; } catch (Exception) { }
                var options = new ImageTypeOptions(newFile, relative, image.Source);
                options.PageNumber = wantPage;
                try { options.Resolution = image.Resolution; } catch (Exception) { }
                if (!options.IsValid(doc))
                {
                    skipped.Add(label + ": Revit says \"" + newFile + "\", page " + wantPage + " is not an image it can load. "
                        + "Left as it was.");
                    continue;
                }
                image.ReloadFrom(options);
#endif
            }
        }
        catch (Exception ex)
        {
            skipped.Add(label + ": Revit refused - " + ex.Message + " Left as it was.");
            continue;
        }

        doc.Regenerate();
        var back = doc.GetElement(image.Id) as ImageType;
        if (back == null) { skipped.Add(label + ": did not read back after the reload."); continue; }
        var after = "";
#if !REVIT2020
        try
        {
            if (back.Status == ImageTypeStatus.FailedToLoad)
            {
                skipped.Add(label + ": Revit returned, but the image reads FAILED TO LOAD - not counted.");
                continue;
            }
        }
        catch (Exception) { }
        try { after = back.Status + ", " + back.WidthInPixels + " x " + back.HeightInPixels + " px"; }
        catch (Exception) { after = ""; }
#endif
        var backFile = "";
        try { backFile = back.Path ?? ""; } catch (Exception) { backFile = ""; }
        var backPage = 0;
        try { backPage = back.PageNumber; } catch (Exception) { backPage = 0; }
        if (newFile != "")
        {
            var sameName = string.Equals(System.IO.Path.GetFileName(backFile), System.IO.Path.GetFileName(newFile),
                StringComparison.OrdinalIgnoreCase);
            if (!sameName)
            {
                skipped.Add(label + ": Revit returned, but the image still reads \"" + backFile + "\" - not counted.");
                continue;
            }
        }
        reloaded++;
        reloadedIds.Add(back.Id);
        var fromWords = "\"" + (backFile == "" ? file : backFile) + "\""
            + (backFile.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? ", page " + backPage : "");
#if REVIT2020
        lines.Add("\"" + (back.Name ?? name) + "\" (" + back.Id + "): reloaded from " + fromWords
            + " - Revit 2020 does not say whether it loaded or its size.");
#else
        lines.Add("\"" + (back.Name ?? name) + "\" (" + back.Id + "): " + (newFile == "" ? "reloaded" : "reloaded from a new file")
            + ", " + fromWords + (after == "" ? "" : " - now " + after) + (before == "" ? "" : " (was " + before + ")") + ".");
#endif
    }
}

if (refused != null) lines.Insert(0, refused);
lines.AddRange(skipped);
report = lines.Count == 0 ? "Nothing to reload: no image here was loaded from a file." : string.Join("  ||  ", lines);
findings.Add(reloaded + " image(s) reloaded and read back; " + skipped.Count + " not reloaded"
    + (missingNames.Count > 0 ? "; " + missingNames.Count + " name(s) with no image" : "") + ".");
findings.AddRange(lines);
