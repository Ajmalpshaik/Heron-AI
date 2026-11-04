// NOT STANDALONE. Assumes `doc` is in scope, and leaves `imageCount`,
// `placedCount`, `report`, `names` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Revit's Manage Images list: every image in the model - a
// picture or a PDF page, imported or linked, and on 2021 on a rendering saved
// to the project - with its name, its file path and whether that path is kept
// relative or absolute, the PDF page, its pixel size and DPI and whether its
// file was found (2021 on), and how many times it is placed and in which views.
//
// IT NEVER CALLS AN IMAGE UNUSED. "Placed 0 times" is what this reads; whether
// an image is unused - also named by no image parameter of any element or
// type - is FIND_PURGEABLE_ELEMENTS' rule, and only that card says it.
//
// ONE STRING, ROWS JOINED BY "  ||  ", because a reply cuts a list to three
// entries and this is the answer. Nothing found is said in words.
//
// RELEASES (tools/api-surface --members, 2026-11-02). ImageType Path,
// PathType and PageNumber, ImageInstance and Element OwnerViewId and GetTypeId
// are the same 2020 to 2027. ImageType Source, Status, Resolution,
// WidthInPixels and HeightInPixels, and the enums ImageTypeSource and
// ImageTypeStatus, are 2021 on - 2020 says which it cannot read rather than
// guessing (IsLoadedFromFile and IsViewImage are on 2020 and 2021 only, and
// obsolete there).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var imageCount = 0;
var placedCount = 0;
var report = "";
var names = new List<string>();
var findings = new List<string>();

var culture = System.Globalization.CultureInfo.InvariantCulture;
var rows = new List<string>();

// Every placement, by the image it places.
var placements = new Dictionary<ElementId, List<string>>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImageInstance)))
{
    var instance = element as ImageInstance;
    if (instance == null) continue;
    var typeId = instance.GetTypeId();
    if (typeId == null || typeId == ElementId.InvalidElementId) continue;
    placedCount++;
    string where;
    var owner = doc.GetElement(instance.OwnerViewId) as View;
    where = owner == null ? "a view not found" : "\"" + owner.Name + "\"";
    List<string> list;
    if (!placements.TryGetValue(typeId, out list)) { list = new List<string>(); placements[typeId] = list; }
    list.Add(where);
}

var types = new List<ImageType>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImageType)))
{
    var one = element as ImageType;
    if (one != null) types.Add(one);
}
types.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));

var linkedCount = 0;
var missingCount = 0;
foreach (var image in types)
{
    imageCount++;
    var name = image.Name ?? "";
    names.Add("\"" + name + "\"");

    string file = "";
    try { file = image.Path ?? ""; } catch (Exception) { file = ""; }
    var isPdf = file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    var parts = new List<string>();

    var kind = "";
#if REVIT2020
    kind = "image (Revit 2020 does not say imported or linked)";
#else
    try
    {
        var source = image.Source;
        if (source == ImageTypeSource.Link) { kind = "linked"; linkedCount++; }
        else if (source == ImageTypeSource.Import) kind = "imported";
        else kind = "saved in the project (a rendering)";
    }
    catch (Exception) { kind = "source not read"; }
#endif
    parts.Add(kind);

    if (file != "")
    {
        string pathKind = "";
        try { pathKind = image.PathType == PathType.Relative ? ", relative path" : ""; } catch (Exception) { }
        parts.Add("file \"" + file + "\"" + pathKind);
    }
    else parts.Add("no file path");

    if (isPdf)
    {
        try { parts.Add("page " + image.PageNumber); } catch (Exception) { }
    }

#if !REVIT2020
    try
    {
        parts.Add(image.WidthInPixels + " x " + image.HeightInPixels + " px at "
            + Math.Round(image.Resolution).ToString("0", culture) + " DPI");
    }
    catch (Exception) { }
    try
    {
        var status = image.Status;
        if (status == ImageTypeStatus.FailedToLoad) { parts.Add("FILE NOT FOUND - could not be loaded"); missingCount++; }
        else if (status == ImageTypeStatus.Unloaded) parts.Add("unloaded");
        else if (status == ImageTypeStatus.Loaded) parts.Add("loaded from its file");
    }
    catch (Exception) { }
#endif

    List<string> views;
    if (placements.TryGetValue(image.Id, out views) && views.Count > 0)
    {
        var distinct = views.Distinct().ToList();
        parts.Add("placed " + views.Count + " time(s) in " + string.Join(", ", distinct.Take(6))
            + (distinct.Count > 6 ? " and " + (distinct.Count - 6) + " more view(s)" : ""));
    }
    else parts.Add("placed 0 times");

    rows.Add("\"" + name + "\" (" + image.Id + "): " + string.Join(", ", parts));
}

if (imageCount == 0)
{
    report = "No image or PDF page is in this model's Manage Images.";
    findings.Add(report);
}
else
{
    report = string.Join("  ||  ", rows);
    findings.Add(imageCount + " image(s) in Manage Images, " + placedCount + " placement(s) in views"
#if !REVIT2020
        + ", " + linkedCount + " linked" + (missingCount > 0 ? ", " + missingCount + " whose file was not found" : "")
#endif
        + ". Placed 0 times is not the same as unused - an image can be named by an image parameter "
        + "instead; the purge list answers which are unused.");
}
