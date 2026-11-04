// NOT STANDALONE. Assumes `doc`, `path`, `view`, `page`, `resolution`,
// `point` and `anchor` are in scope, and leaves `imageInstanceId`,
// `imageTypeId`, `reusedType`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. The
// image and its placement are made inside one SubTransaction, so a placement
// Revit refuses takes the image it had just loaded away with it - nothing is
// left half made in Manage Images.
//
// WHAT IT DOES. Revit's Insert > Import PDF and Insert > Import Image: one
// page of a PDF, or a PNG, JPG, BMP or TIFF picture, loaded into Manage Images
// as an IMPORTED image (ImageTypeSource.Import - never a link) and placed once
// in a view. An image already in Manage Images from the same file - and, for a
// PDF, the same page - is placed again rather than loaded a second time.
//
// THE PATH IS THE MODELLER'S. It is read on the PC Revit runs on, so whether
// the file is there is only known here, at run time: no file at that path is
// said in those words and nothing is made. Revit's own refusal - a page the
// PDF has not got, a picture it cannot read - is passed on in Revit's words.
//
// THE VIEW. Left out, the view in front. Revit places images in plans,
// sections, elevations, drafting views, legends and sheets; a 3D view, a
// schedule or a template is refused by name before anything is loaded
// (ImageInstance.IsValidView decides, and the refusal names the view type).
//
// THE POINT. "x,y" in millimetres along the view's right and up from the
// project's internal origin as seen in that view - on a sheet, from the
// sheet's origin. Left out, 0,0. Millimetres become feet by / 304.8 (D-20).
// `anchor` says which point of the image sits there: centre (the default),
// top left, top right, bottom left or bottom right.
//
// READ BACK, AFTER A REGENERATE: the image and its type looked up again by id,
// the view it is in, its width and height in mm, where its centre landed in
// the view in mm, and whether it is drawn in the background or foreground.
//
// RELEASES (tools/api-surface --members, 2026-11-02). ImageType.Create(
// Document, ImageTypeOptions), ImageInstance.Create(Document, View, ElementId,
// ImagePlacementOptions), ImageInstance.IsValidView, Width, Height, DrawLayer
// and GetLocation, ImagePlacementOptions(XYZ, BoxPlacement), BoxPlacement,
// ImageTypeOptions PageNumber, Resolution and IsValid, and ImageType Path and
// PageNumber are the same 2020 to 2027. The ImageTypeOptions constructor is
// NOT: 2020 has only (string, bool) - no ImageTypeSource, the enum is 2021 on
// - and 2022 on has only (string, bool, ImageTypeSource). ImageType Source,
// Resolution, WidthInPixels and HeightInPixels are 2021 on, so 2020 reads
// none of them.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

ElementId imageInstanceId = ElementId.InvalidElementId;
ElementId imageTypeId = ElementId.InvalidElementId;
var reusedType = false;
var report = "";
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
var lines = new List<string>();
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot).ToString("0", culture);

Func<View, string> viewWords = v =>
{
    switch (v.ViewType)
    {
        case ViewType.ThreeD: return "a 3D view";
        case ViewType.Schedule: return "a schedule";
        case ViewType.PanelSchedule: return "a panel schedule";
        case ViewType.ColumnSchedule: return "a graphical column schedule";
        case ViewType.Walkthrough: return "a walkthrough";
        case ViewType.Rendering: return "a saved rendering";
        case ViewType.Report: return "a report";
        default: return "a " + v.ViewType + " view";
    }
};

// ---- the view
var target = view ?? doc.ActiveView;
if (target == null)
    refused = "No view was named and Revit has no view in front, so NOTHING WAS IMPORTED. Name a plan, section, "
        + "elevation, drafting view, legend or sheet.";
else if (target.IsTemplate)
    refused = "\"" + target.Name + "\" is a view template, and an image is placed in a view, not a template. "
        + "NOTHING WAS IMPORTED.";
else if (!ImageInstance.IsValidView(target))
    refused = "\"" + target.Name + "\" is " + viewWords(target) + ", and Revit does not place images there. "
        + "Name a plan, section, elevation, drafting view, legend or sheet. NOTHING WAS IMPORTED.";

// ---- the file
var file = (path ?? "").Trim().Trim('"').Trim();
var extension = "";
var isPdf = false;
if (refused == null)
{
    if (file == "")
        refused = "No file was named. The full path of the PDF or picture is needed - it is never guessed. "
            + "NOTHING WAS IMPORTED.";
    else
    {
        try { extension = (System.IO.Path.GetExtension(file) ?? "").ToLowerInvariant(); }
        catch (Exception) { extension = ""; }
        isPdf = extension == ".pdf";
        var picture = extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp"
            || extension == ".tif" || extension == ".tiff";
        if (!isPdf && !picture)
            refused = "\"" + file + "\" is not a PDF, PNG, JPG, BMP or TIFF - the files Revit's Import PDF and "
                + "Import Image take. NOTHING WAS IMPORTED.";
        else if (!System.IO.File.Exists(file))
            refused = "There is no file at \"" + file + "\" on the PC Revit runs on - the path is read there, "
                + "not where the chat is. NOTHING WAS IMPORTED.";
    }
}

// ---- the page and resolution
if (refused == null && isPdf && page < 1)
    refused = "Page " + page + " is not a page - PDF pages are counted from 1. NOTHING WAS IMPORTED.";
if (refused == null && resolution < 0)
    refused = "A resolution of " + resolution + " DPI is not one. NOTHING WAS IMPORTED.";
if (refused == null && !isPdf && page != 1)
    lines.Add("A page was given, but \"" + System.IO.Path.GetFileName(file) + "\" is a picture with one page; "
        + "the page was not used.");

// ---- the point and anchor
var at = XYZ.Zero;
var corner = BoxPlacement.Center;
if (refused == null)
{
    var parts = (point ?? "").Replace(';', ',').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    double x = 0, y = 0;
    var spoken = (point ?? "").Trim();
    if (spoken != "" && (parts.Length != 2
        || !double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, culture, out x)
        || !double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, culture, out y)))
        refused = "\"" + point + "\" is not a point. Give it as x,y in millimetres - 0,0 or 1200,-450. "
            + "NOTHING WAS IMPORTED.";
    else
    {
        // The internal origin as seen in this view: its projection onto the
        // view's plane, then x along the view's right and y along its up.
        var normal = target.ViewDirection;
        var plane = normal.Multiply(normal.DotProduct(target.Origin));
        at = plane + target.RightDirection.Multiply(x / MillimetresPerFoot)
                   + target.UpDirection.Multiply(y / MillimetresPerFoot);
    }

    var word = (anchor ?? "").Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");
    while (word.Contains("  ")) word = word.Replace("  ", " ");
    if (refused == null)
    {
        if (word == "" || word == "centre" || word == "center" || word == "middle") corner = BoxPlacement.Center;
        else if (word == "top left") corner = BoxPlacement.TopLeft;
        else if (word == "top right") corner = BoxPlacement.TopRight;
        else if (word == "bottom left") corner = BoxPlacement.BottomLeft;
        else if (word == "bottom right") corner = BoxPlacement.BottomRight;
        else refused = "\"" + anchor + "\" is not centre, top left, top right, bottom left or bottom right. "
            + "NOTHING WAS IMPORTED.";
    }
}

// ---- an image already loaded from the same file and page
ImageType existing = null;
if (refused == null)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImageType)))
    {
        var candidate = element as ImageType;
        if (candidate == null) continue;
        string candidatePath;
        try { candidatePath = (candidate.Path ?? "").Trim(); } catch (Exception) { continue; }
        if (!string.Equals(candidatePath, file, StringComparison.OrdinalIgnoreCase)) continue;
#if !REVIT2020
        if (candidate.Source == ImageTypeSource.Internal) continue;
#endif
        if (isPdf)
        {
            int candidatePage;
            try { candidatePage = candidate.PageNumber; } catch (Exception) { continue; }
            if (candidatePage != page) continue;
        }
        existing = candidate;
        break;
    }
}

// ---- make it
if (refused == null)
{
    var sub = new SubTransaction(doc);
    sub.Start();
    try
    {
        ImageType type = existing;
        if (type == null)
        {
#if REVIT2020
            var options = new ImageTypeOptions(file, false);
#else
            var options = new ImageTypeOptions(file, false, ImageTypeSource.Import);
#endif
            if (isPdf) options.PageNumber = page;
            if (resolution > 0) options.Resolution = resolution;
            if (!options.IsValid(doc))
                throw new InvalidOperationException("Revit says it cannot read "
                    + (isPdf ? "page " + page + " of \"" + file + "\"" : "\"" + file + "\"")
                    + " as an image" + (isPdf ? " - the PDF may have fewer pages" : ""));
            type = ImageType.Create(doc, options);
        }
        else
        {
            reusedType = true;
            if (resolution > 0)
                lines.Add("\"" + existing.Name + "\" was already in Manage Images and was placed again; its own "
                    + "resolution was kept, not " + resolution + " DPI - Manage Images reloads it at another.");
        }
        if (type == null) throw new InvalidOperationException("Revit returned no image");

        var instance = ImageInstance.Create(doc, target, type.Id, new ImagePlacementOptions(at, corner));
        if (instance == null) throw new InvalidOperationException("Revit returned no placed image");
        doc.Regenerate();

        // READ BACK, by id, before anything is kept.
        var placed = doc.GetElement(instance.Id) as ImageInstance;
        var loaded = doc.GetElement(type.Id) as ImageType;
        if (placed == null || loaded == null)
            throw new InvalidOperationException("the image was not found again after Revit made it");
        var inView = doc.GetElement(placed.OwnerViewId) as View;

        sub.Commit();
        imageInstanceId = placed.Id;
        imageTypeId = loaded.Id;

        var centre = placed.GetLocation(BoxPlacement.Center);
        var fromOrigin = centre - target.ViewDirection.Multiply(target.ViewDirection.DotProduct(centre));
        var detail = "";
#if !REVIT2020
        try
        {
            detail = ", " + loaded.WidthInPixels + " x " + loaded.HeightInPixels + " px at "
                + Math.Round(loaded.Resolution).ToString("0", culture) + " DPI, "
                + (loaded.Source == ImageTypeSource.Link ? "linked" : "imported");
        }
        catch (Exception) { detail = ""; }
#endif
        lines.Insert(0, (reusedType ? "Placed again" : "Imported") + " \"" + loaded.Name + "\" (image " + loaded.Id
            + (isPdf ? ", page " + page : "") + detail + ") into \"" + (inView == null ? target.Name : inView.Name)
            + "\" as placed image " + placed.Id + ": " + mm(placed.Width) + " x " + mm(placed.Height)
            + " mm, centre at " + mm(fromOrigin.DotProduct(target.RightDirection)) + ","
            + mm(fromOrigin.DotProduct(target.UpDirection)) + " mm in the view, drawn in the "
            + (placed.DrawLayer == DrawLayer.Background ? "background" : "foreground") + ".");
        findings.Add((reusedType ? "1 image already in Manage Images placed again" : "1 image imported and placed")
            + " in \"" + (inView == null ? target.Name : inView.Name) + "\", read back by id.");
    }
    catch (Exception ex)
    {
        if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack();
        imageInstanceId = ElementId.InvalidElementId;
        imageTypeId = ElementId.InvalidElementId;
        reusedType = false;
        refused = "Revit refused to import \"" + System.IO.Path.GetFileName(file) + "\""
            + (isPdf ? " page " + page : "") + " into \"" + target.Name + "\": " + ex.Message
            + ". NOTHING WAS IMPORTED - anything made on the way was taken back.";
    }
}

if (refused != null) lines.Insert(0, refused);
report = string.Join(" | ", lines);
findings.AddRange(lines);
