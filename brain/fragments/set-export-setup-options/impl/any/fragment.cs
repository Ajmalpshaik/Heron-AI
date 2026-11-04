// NOT STANDALONE. Assumes `doc`, `setupName`, `options` and `kind` are in
// scope, and leaves `done`, `changed`, `kindUsed`, `unchangedOptions`,
// `notKept` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ALL OR NOTHING. A setup's options come out of Revit as one object; every
// option asked for is applied to THAT object first, and the setup is written
// back once, only when every one of them was understood. One unknown name or
// one bad value refuses the call and Revit's setup is never touched.
//
// DWG AND DXF ARE ONE SETUP WITH TWO HALVES. "dwg" changes the DWG half - what
// the DWG dialog opens with - and "dxf" the DXF half of the same element.
// "Export views on sheets as external references" is MergedViews = false, and
// it is DWG's only: DXF has no such option.
//
// NAMES AND VALUES ARE THE MODELLER'S WORDS. "units=mm", "fileVersion=2013",
// "coordinates=shared". Enum values are matched against the enum's own names
// read at run time, spaces and case aside, so a refusal can list what Revit
// actually offers on this release.
//
// EVERY OPTION IS READ BACK after the setup is saved, from a fresh copy of its
// options, and one that does not read back as asked is named in `notKept`.

bool done = false;
string changed = "";
string kindUsed = "";
string unchangedOptions = "";
string notKept = "";
string refused = "";

string wantedName = (setupName ?? "").Trim();
string kindWord = (kind ?? "").Trim().ToLowerInvariant();
if (kindWord == "any" || kindWord == "all") kindWord = "";
if (kindWord == "dwg/dxf" || kindWord == "cad" || kindWord == "autocad") kindWord = "dwg";
bool dxfHalf = kindWord == "dxf";
string lookKind = dxfHalf ? "dwg" : kindWord;

Func<string, string> squash = text => new string((text ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
Func<string, bool?> yes = text =>
{
    string word = squash(text);
    if (word == "yes" || word == "true" || word == "on" || word == "1" || word == "y") return true;
    if (word == "no" || word == "false" || word == "off" || word == "0" || word == "n") return false;
    return null;
};
Func<bool, string> yesNo = value => value ? "yes" : "no";
// An enum value by the modeller's word: an alias first, then the enum's own
// names. Null when nothing matches.
Func<Type, string, Dictionary<string, string>, object> pick = (type, text, aliases) =>
{
    string word = squash(text);
    if (aliases != null && aliases.ContainsKey(word)) word = squash(aliases[word]);
    foreach (var name in Enum.GetNames(type))
        if (squash(name) == word) return Enum.Parse(type, name);
    return null;
};
Func<Type, string> offered = type => string.Join(", ", Enum.GetNames(type));

Func<string, List<string>> namesOf = k =>
{
    var list = new List<string>();
    try
    {
        if (k == "dwg") list.AddRange(ExportDWGSettings.ListNames(doc));
        else if (k == "dgn") list.AddRange(ExportDGNSettings.ListNames(doc));
#if !REVIT2020 && !REVIT2021
        else if (k == "pdf") list.AddRange(ExportPDFSettings.ListNames(doc));
#endif
    }
    catch { }
    list.Sort(StringComparer.OrdinalIgnoreCase);
    return list;
};

if (wantedName.Length == 0)
    refused = "No setup name was given.";
else if (kindWord == "print" || kindWord == "print setting" || kindWord == "printing")
    refused = "A print setting's paper size, orientation and fit are SET_PRINT_SETTINGS' to change. This changes "
        + "DWG/DXF, DGN and PDF export setups.";
else if (lookKind.Length > 0 && lookKind != "dwg" && lookKind != "dgn" && lookKind != "pdf")
    refused = "'" + kind + "' is not a kind of setup this changes. It changes DWG, DXF, DGN and PDF export setups. "
        + "IFC, Navisworks, DWF and image export keep no setup the Revit API can reach.";
else if ((options ?? "").Trim().Length == 0)
    refused = "No options were given - name them as name=value, e.g. units=millimeter; fileVersion=2013.";
#if REVIT2020 || REVIT2021
else if (lookKind == "pdf")
    refused = "PDF export setups need Revit 2022 or later - this Revit has no PDF export of its own.";
#endif

// ---- Which setup -----------------------------------------------------------
string found = "";
if (refused.Length == 0)
{
    var kinds = lookKind.Length > 0 ? new List<string> { lookKind } : new List<string> { "dwg", "dgn", "pdf" };
    var hits = new List<string>();
    var everyName = new List<string>();
    foreach (var k in kinds)
    {
        var list = namesOf(k);
        string label = k == "dwg" ? "DWG/DXF" : k.ToUpperInvariant();
        foreach (var one in list) everyName.Add(label + " '" + one + "'");
        var match = list.FirstOrDefault(one => string.Equals((one ?? "").Trim(), wantedName, StringComparison.OrdinalIgnoreCase));
        if (match != null) { hits.Add(k); found = match; }
    }
    if (hits.Count == 0)
        refused = "No " + (lookKind.Length > 0 ? (lookKind == "dwg" ? "DWG/DXF" : lookKind.ToUpperInvariant()) + " " : "")
            + "export setup called '" + wantedName + "'. "
            + (everyName.Count > 0
                ? "The setups there are: " + string.Join(", ", everyName.Take(30)) + (everyName.Count > 30 ? " and more." : ".")
                : "This project has none of that kind.")
            + " Nothing was changed.";
    else if (hits.Count > 1)
        refused = "'" + wantedName + "' is the name of a " + string.Join(" setup and a ", hits.Select(h => h.ToUpperInvariant()))
            + " setup - say which kind. Nothing was changed.";
    else lookKind = hits[0];
}

// ---- The options object, and the options this kind takes --------------------
ExportDWGSettings dwgSettings = null;
ExportDGNSettings dgnSettings = null;
object current = null;
if (refused.Length == 0)
{
    try
    {
        if (lookKind == "dwg")
        {
            dwgSettings = ExportDWGSettings.FindByName(doc, found);
            if (dwgSettings != null) current = dxfHalf ? (object)dwgSettings.GetDXFExportOptions() : dwgSettings.GetDWGExportOptions();
        }
        else if (lookKind == "dgn")
        {
            dgnSettings = ExportDGNSettings.FindByName(doc, found);
            if (dgnSettings != null) current = dgnSettings.GetDGNExportOptions();
        }
#if !REVIT2020 && !REVIT2021
        else if (lookKind == "pdf")
        {
            var pdfSettings = ExportPDFSettings.FindByName(doc, found);
            if (pdfSettings != null) current = pdfSettings.GetOptions();
        }
#endif
    }
    catch { }
    if (current == null) refused = "Revit would not hand back the options of '" + found + "'. Nothing was changed.";
    kindUsed = lookKind == "dwg" ? (dxfHalf ? "DXF" : "DWG") : lookKind.ToUpperInvariant();
}

var read = new Dictionary<string, Func<object, string>>();
var write = new Dictionary<string, Func<object, string, string>>();
Action<string, Func<object, string>, Func<object, string, string>> offer = (name, reader, writer) =>
{
    read[name] = reader;
    write[name] = writer;
};
// A yes/no option, in one line each below.
Action<string, Func<object, bool>, Action<object, bool>> offerYesNo = (name, get, set) => offer(name,
    o => yesNo(get(o)),
    (o, text) =>
    {
        var value = yes(text);
        if (value == null) return "'" + text + "' is not yes or no.";
        set(o, value.Value);
        return "";
    });
// An enum option: the setter is handed the parsed value.
Action<string, Type, Dictionary<string, string>, Func<object, object>, Action<object, object>> offerEnum =
    (name, type, aliases, get, set) => offer(name,
        o => get(o).ToString(),
        (o, text) =>
        {
            var value = pick(type, text, aliases);
            if (value == null) return "'" + text + "' is not one of " + offered(type) + ".";
            set(o, value);
            return "";
        });

if (refused.Length == 0 && current is BaseExportOptions)
{
    offerEnum("colors", typeof(ExportColorMode),
        new Dictionary<string, string> { { "index", "IndexColors" }, { "aci", "IndexColors" }, { "rgb", "TrueColor" }, { "perview", "TrueColorPerView" } },
        o => ((BaseExportOptions)o).Colors, (o, v) => ((BaseExportOptions)o).Colors = (ExportColorMode)v);
    offer("layerstandard", o => ((BaseExportOptions)o).LayerMapping ?? "",
        (o, text) => { ((BaseExportOptions)o).LayerMapping = text; return ""; });
    offerEnum("propertyoverrides", typeof(PropOverrideMode), null,
        o => ((BaseExportOptions)o).PropOverrides, (o, v) => ((BaseExportOptions)o).PropOverrides = (PropOverrideMode)v);
    offerYesNo("hidescopeboxes", o => ((BaseExportOptions)o).HideScopeBox, (o, v) => ((BaseExportOptions)o).HideScopeBox = v);
    offerYesNo("hidereferenceplanes", o => ((BaseExportOptions)o).HideReferencePlane, (o, v) => ((BaseExportOptions)o).HideReferencePlane = v);
    offerYesNo("hideunreferencedviewtags", o => ((BaseExportOptions)o).HideUnreferenceViewTags, (o, v) => ((BaseExportOptions)o).HideUnreferenceViewTags = v);
    offerYesNo("preservecoincidentlines", o => ((BaseExportOptions)o).PreserveCoincidentLines, (o, v) => ((BaseExportOptions)o).PreserveCoincidentLines = v);
}

if (refused.Length == 0 && current is ACADExportOptions)
{
    offer("fileversion",
        o =>
        {
            var version = ((ACADExportOptions)o).FileVersion;
            return version == ACADVersion.R2007 ? "2007" : version == ACADVersion.R2010 ? "2010"
                : version == ACADVersion.R2013 ? "2013" : "2018";
        },
        (o, text) =>
        {
            string word = squash(text);
            var acad = (ACADExportOptions)o;
            if (word.Contains("2007")) acad.FileVersion = ACADVersion.R2007;
            else if (word.Contains("2010")) acad.FileVersion = ACADVersion.R2010;
            else if (word.Contains("2013")) acad.FileVersion = ACADVersion.R2013;
            else if (word.Contains("2018")) acad.FileVersion = ACADVersion.R2018;
            else return "'" + text + "' is not one of 2007, 2010, 2013, 2018.";
            return "";
        });
    offerEnum("units", typeof(ExportUnit),
        new Dictionary<string, string>
        {
            { "mm", "Millimeter" }, { "millimetre", "Millimeter" }, { "millimetres", "Millimeter" }, { "millimeters", "Millimeter" },
            { "cm", "Centimeter" }, { "centimetre", "Centimeter" }, { "centimetres", "Centimeter" }, { "centimeters", "Centimeter" },
            { "m", "Meter" }, { "metre", "Meter" }, { "metres", "Meter" }, { "meters", "Meter" },
            { "in", "Inch" }, { "inches", "Inch" }, { "ft", "Foot" }, { "feet", "Foot" }
        },
        o => ((ACADExportOptions)o).TargetUnit, (o, v) => ((ACADExportOptions)o).TargetUnit = (ExportUnit)v);
    offer("coordinates", o => ((ACADExportOptions)o).SharedCoords ? "shared" : "internal",
        (o, text) =>
        {
            string word = squash(text);
            if (word == "shared") ((ACADExportOptions)o).SharedCoords = true;
            else if (word == "internal" || word == "projectinternal" || word == "project") ((ACADExportOptions)o).SharedCoords = false;
            else return "'" + text + "' is not shared or internal.";
            return "";
        });
    offerEnum("solids", typeof(SolidGeometry), new Dictionary<string, string> { { "acissolids", "ACIS" }, { "mesh", "Polymesh" } },
        o => ((ACADExportOptions)o).ExportOfSolids, (o, v) => ((ACADExportOptions)o).ExportOfSolids = (SolidGeometry)v);
    offerEnum("linescaling", typeof(LineScaling),
        new Dictionary<string, string> { { "view", "ViewScale" }, { "model", "ModelSpace" }, { "paper", "PaperSpace" } },
        o => ((ACADExportOptions)o).LineScaling, (o, v) => ((ACADExportOptions)o).LineScaling = (LineScaling)v);
    offerEnum("texttreatment", typeof(TextTreatment), null,
        o => ((ACADExportOptions)o).TextTreatment, (o, v) => ((ACADExportOptions)o).TextTreatment = (TextTreatment)v);
    offerYesNo("roomsandareasaspolylines", o => ((ACADExportOptions)o).ExportingAreas, (o, v) => ((ACADExportOptions)o).ExportingAreas = v);
    offerYesNo("marknonplotlayers", o => ((ACADExportOptions)o).MarkNonplotLayers, (o, v) => ((ACADExportOptions)o).MarkNonplotLayers = v);
    offer("nonplotsuffix", o => ((ACADExportOptions)o).NonplotSuffix ?? "",
        (o, text) => { ((ACADExportOptions)o).NonplotSuffix = text; return ""; });
}

if (refused.Length == 0 && current is DWGExportOptions)
{
    // "Export views on sheets as external references" is the dialog's tick
    // box; MergedViews is its opposite.
    offerYesNo("externalreferences", o => !((DWGExportOptions)o).MergedViews, (o, v) => ((DWGExportOptions)o).MergedViews = !v);
    offerYesNo("mergedviews", o => ((DWGExportOptions)o).MergedViews, (o, v) => ((DWGExportOptions)o).MergedViews = v);
}

if (refused.Length == 0 && current is DGNExportOptions)
{
    offer("fileversion", o => ((DGNExportOptions)o).FileVersion == DGNFileFormat.DGNVersion7 ? "V7" : "V8",
        (o, text) =>
        {
            string word = squash(text);
            if (word == "7" || word == "v7" || word == "dgnversion7") ((DGNExportOptions)o).FileVersion = DGNFileFormat.DGNVersion7;
            else if (word == "8" || word == "v8" || word == "dgnversion8") ((DGNExportOptions)o).FileVersion = DGNFileFormat.DGNVersion8;
            else return "'" + text + "' is not V7 or V8.";
            return "";
        });
    offerYesNo("externalreferences", o => !((DGNExportOptions)o).MergedViews, (o, v) => ((DGNExportOptions)o).MergedViews = !v);
    offerYesNo("mergedviews", o => ((DGNExportOptions)o).MergedViews, (o, v) => ((DGNExportOptions)o).MergedViews = v);
    offer("seedfile", o => ((DGNExportOptions)o).SeedName ?? "",
        (o, text) => { ((DGNExportOptions)o).SeedName = text; return ""; });
#if REVIT2020
    offerYesNo("masterunits", o => ((DGNExportOptions)o).MasterUnits, (o, v) => ((DGNExportOptions)o).MasterUnits = v);
#else
    offerYesNo("workingunits", o => ((DGNExportOptions)o).WorkingUnits, (o, v) => ((DGNExportOptions)o).WorkingUnits = v);
#endif
}

#if !REVIT2020 && !REVIT2021
if (refused.Length == 0 && current is PDFExportOptions)
{
    offerYesNo("combine", o => ((PDFExportOptions)o).Combine, (o, v) => ((PDFExportOptions)o).Combine = v);
    offerEnum("paperformat", typeof(ExportPaperFormat), null,
        o => ((PDFExportOptions)o).PaperFormat, (o, v) => ((PDFExportOptions)o).PaperFormat = (ExportPaperFormat)v);
    offerEnum("orientation", typeof(PageOrientationType), null,
        o => ((PDFExportOptions)o).PaperOrientation, (o, v) => ((PDFExportOptions)o).PaperOrientation = (PageOrientationType)v);
    offer("zoom",
        o => ((PDFExportOptions)o).ZoomType == ZoomType.FitToPage ? "fit" : ((PDFExportOptions)o).ZoomPercentage + "%",
        (o, text) =>
        {
            string word = squash(text);
            int percent;
            if (word == "fit" || word == "fittopage") ((PDFExportOptions)o).ZoomType = ZoomType.FitToPage;
            else if (int.TryParse(word, out percent) && percent > 0)
            {
                ((PDFExportOptions)o).ZoomType = ZoomType.Zoom;
                ((PDFExportOptions)o).ZoomPercentage = percent;
            }
            else return "'" + text + "' is not fit or a percentage.";
            return "";
        });
    offerEnum("colors", typeof(ColorDepthType),
        new Dictionary<string, string> { { "colour", "Color" }, { "greyscale", "GrayScale" }, { "black", "BlackLine" }, { "blackandwhite", "BlackLine" } },
        o => ((PDFExportOptions)o).ColorDepth, (o, v) => ((PDFExportOptions)o).ColorDepth = (ColorDepthType)v);
    offerEnum("rasterquality", typeof(RasterQualityType), null,
        o => ((PDFExportOptions)o).RasterQuality, (o, v) => ((PDFExportOptions)o).RasterQuality = (RasterQualityType)v);
    offer("quality", o => ((PDFExportOptions)o).ExportQuality.ToString(),
        (o, text) =>
        {
            string word = squash(text);
            if (word.All(char.IsDigit)) word = "dpi" + word;
            var value = pick(typeof(PDFExportQualityType), word, null);
            if (value == null) return "'" + text + "' is not one of " + offered(typeof(PDFExportQualityType)) + ".";
            ((PDFExportOptions)o).ExportQuality = (PDFExportQualityType)value;
            return "";
        });
    offerYesNo("alwaysuseraster", o => ((PDFExportOptions)o).AlwaysUseRaster, (o, v) => ((PDFExportOptions)o).AlwaysUseRaster = v);
    offerYesNo("hidecropboundaries", o => ((PDFExportOptions)o).HideCropBoundaries, (o, v) => ((PDFExportOptions)o).HideCropBoundaries = v);
    offerYesNo("hidereferenceplanes", o => ((PDFExportOptions)o).HideReferencePlane, (o, v) => ((PDFExportOptions)o).HideReferencePlane = v);
    offerYesNo("hidescopeboxes", o => ((PDFExportOptions)o).HideScopeBoxes, (o, v) => ((PDFExportOptions)o).HideScopeBoxes = v);
    offerYesNo("hideunreferencedviewtags", o => ((PDFExportOptions)o).HideUnreferencedViewTags, (o, v) => ((PDFExportOptions)o).HideUnreferencedViewTags = v);
    offerYesNo("maskcoincidentlines", o => ((PDFExportOptions)o).MaskCoincidentLines, (o, v) => ((PDFExportOptions)o).MaskCoincidentLines = v);
    offerYesNo("replacehalftone", o => ((PDFExportOptions)o).ReplaceHalftoneWithThinLines, (o, v) => ((PDFExportOptions)o).ReplaceHalftoneWithThinLines = v);
    offerYesNo("viewlinksinblue", o => ((PDFExportOptions)o).ViewLinksInBlue, (o, v) => ((PDFExportOptions)o).ViewLinksInBlue = v);
}
#endif

// The modeller's other words for an option name, squashed.
var aliases = new Dictionary<string, string>
{
    { "colours", "colors" }, { "colour", "colors" }, { "color", "colors" }, { "colordepth", "colors" },
    { "layermapping", "layerstandard" }, { "layersstandard", "layerstandard" },
    { "version", "fileversion" }, { "format", "fileversion" }, { "fileformat", "fileversion" },
    { "unit", "units" }, { "exportunits", "units" },
    { "coordinatesystem", "coordinates" }, { "coordinatesystembasis", "coordinates" }, { "coordinatebasis", "coordinates" },
    { "solid", "solids" }, { "exportofsolids", "solids" },
    { "exportviewsonsheetsasexternalreferences", "externalreferences" }, { "xref", "externalreferences" },
    { "xrefs", "externalreferences" }, { "viewsonsheetsasexternalreferences", "externalreferences" },
    { "hidescopebox", "hidescopeboxes" }, { "hidereferenceplane", "hidereferenceplanes" }, { "hiderefplanes", "hidereferenceplanes" },
    { "hideunreferenceviewtags", "hideunreferencedviewtags" },
    { "papersize", "paperformat" }, { "paper", "paperformat" }, { "exportquality", "quality" }, { "dpi", "quality" },
    { "seed", "seedfile" }, { "seedname", "seedfile" }, { "exportingareas", "roomsandareasaspolylines" }
};

// ---- Apply every option to the object, before Revit is written -------------
var changes = new List<string>();
var same = new List<string>();
var errors = new List<string>();
// The value each option should read back as - the LAST one asked, so
// "units=mm; units=cm" or two names for one option are judged on what was
// finally handed to Revit, not on a step on the way.
var expected = new Dictionary<string, string>();
var layerExpected = new List<Tuple<string, string, bool, string>>();

// The one layer row named, preferring the plain row over a special one
// (interior wall, exterior wall...) of the same category.
Func<ExportLayerTable, string, string, ExportLayerKey> rowOf = (table, category, sub) =>
{
    ExportLayerKey best = null;
    foreach (var key in table.GetKeys())
    {
        if (!string.Equals((key.CategoryName ?? "").Trim(), category, StringComparison.OrdinalIgnoreCase)) continue;
        if (!string.Equals((key.SubCategoryName ?? "").Trim(), sub, StringComparison.OrdinalIgnoreCase)) continue;
        if (best == null || key.SpecialType == SpecialType.Default) best = key;
    }
    return best;
};

if (refused.Length == 0)
{
    foreach (var piece in (options ?? "").Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
    {
        string text = piece.Trim();
        if (text.Length == 0) continue;
        int equals = text.IndexOf('=');
        if (equals <= 0) { errors.Add("'" + text + "' is not name=value."); continue; }
        string raw = text.Substring(0, equals).Trim();
        string value = text.Substring(equals + 1).Trim();
        string lower = raw.ToLowerInvariant();

        if (lower.StartsWith("layer:") || lower.StartsWith("cutlayer:"))
        {
            var layered = current as BaseExportOptions;
            bool cut = lower.StartsWith("cut");
            string where = raw.Substring(raw.IndexOf(':') + 1).Trim();
            string category = where;
            string sub = "";
            int colon = where.IndexOf(':');
            if (colon >= 0) { category = where.Substring(0, colon).Trim(); sub = where.Substring(colon + 1).Trim(); }

            if (layered == null) { errors.Add(raw + ": a PDF setup has no layer table."); continue; }
            if (value.Length == 0) { errors.Add(raw + ": no layer name was given."); continue; }
            try
            {
                var table = layered.GetExportLayerTable();
                var key = rowOf(table, category, sub);
                if (key == null)
                {
                    errors.Add(raw + ": the layer table has no row for '" + where + "' - name it as the table does, "
                        + "e.g. layer:Ducts or layer:Ducts:Insulation (LIST_EXPORT_SETUPS with this setup's name lists them).");
                    continue;
                }
                var info = table.GetExportLayerInfo(key);
                string before = (cut ? info.CutLayerName : info.LayerName) ?? "";
                if (string.Equals(before, value, StringComparison.Ordinal)) { same.Add(raw + " (" + value + ")"); continue; }
                if (cut) info.CutLayerName = value; else info.LayerName = value;
                table[key] = info;
                layered.SetExportLayerTable(table);
                changes.Add(raw + ": " + before + " -> " + value);
                layerExpected.Add(Tuple.Create(category, sub, cut, value));
            }
            catch (Exception failure) { errors.Add(raw + ": Revit would not change that layer row - " + failure.Message); }
            continue;
        }

        string name = squash(raw);
        if (aliases.ContainsKey(name)) name = aliases[name];
        if (name == "viewsonsheets")
        {
            // "viewsOnSheets=xref" or "=merged", the dialog's two choices.
            string word = squash(value);
            name = "externalreferences";
            value = word.Contains("xref") || word.Contains("external") || word.Contains("reference") ? "yes"
                : word.Contains("merge") ? "no" : value;
        }
        if (!write.ContainsKey(name))
        {
            errors.Add("'" + raw + "' is not an option this changes on a " + (kindUsed.Length > 0 ? kindUsed : "this") + " setup.");
            continue;
        }
        try
        {
            string before = read[name](current);
            string problem = write[name](current, value);
            if (problem.Length > 0) { errors.Add(raw + ": " + problem); continue; }
            string after = read[name](current);
            if (before == after) same.Add(raw + " (" + after + ")");
            else changes.Add(raw + ": " + before + " -> " + after);
            expected[name] = after;
        }
        catch (Exception failure) { errors.Add(raw + ": Revit would not take '" + value + "' - " + failure.Message); }
    }

    unchangedOptions = string.Join("; ", same);
    if (errors.Count > 0)
    {
        var names = write.Keys.ToList();
        names.Sort(StringComparer.Ordinal);
        refused = "Nothing was changed. " + string.Join(" ", errors) + " The options a " + kindUsed + " setup takes here: "
            + string.Join(", ", names) + (current is BaseExportOptions ? ", layer:<category>[:<subcategory>], cutlayer:<category>[:<subcategory>]" : "")
            + ".";
    }
    else if (changes.Count == 0)
        refused = "Every option asked is already set that way on '" + found + "' - nothing was changed.";
}

// ---- Write the setup once, and read every option back ----------------------
if (refused.Length == 0)
{
    object fresh = null;
    try
    {
        if (dwgSettings != null && dxfHalf)
        {
            dwgSettings.SetDXFExportOptions((DXFExportOptions)current);
            fresh = dwgSettings.GetDXFExportOptions();
        }
        else if (dwgSettings != null)
        {
            dwgSettings.SetDWGExportOptions((DWGExportOptions)current);
            fresh = dwgSettings.GetDWGExportOptions();
        }
        else if (dgnSettings != null)
        {
            dgnSettings.SetDGNExportOptions((DGNExportOptions)current);
            fresh = dgnSettings.GetDGNExportOptions();
        }
#if !REVIT2020 && !REVIT2021
        else
        {
            var pdfSettings = ExportPDFSettings.FindByName(doc, found);
            pdfSettings.SetOptions((PDFExportOptions)current);
            fresh = pdfSettings.GetOptions();
        }
#endif
    }
    catch (Exception failure)
    {
        refused = "Revit refused to save the options of '" + found + "': " + failure.Message + " Nothing was changed.";
    }

    if (refused.Length == 0)
    {
        var missed = new List<string>();
        if (fresh == null) missed.Add("the setup's options could not be read back");
        else
        {
            foreach (var one in expected)
            {
                string now = "";
                try { now = read[one.Key](fresh); } catch { now = "unreadable"; }
                if (now != one.Value) missed.Add(one.Key + " reads " + now + ", not " + one.Value);
            }
            var layered = fresh as BaseExportOptions;
            foreach (var one in layerExpected)
            {
                string now = "";
                try
                {
                    var table = layered.GetExportLayerTable();
                    var key = rowOf(table, one.Item1, one.Item2);
                    var info = table.GetExportLayerInfo(key);
                    now = (one.Item3 ? info.CutLayerName : info.LayerName) ?? "";
                }
                catch { now = "unreadable"; }
                if (now != one.Item4)
                    missed.Add((one.Item3 ? "cut layer " : "layer ") + one.Item1 + (one.Item2.Length > 0 ? ":" + one.Item2 : "")
                        + " reads '" + now + "', not '" + one.Item4 + "'");
            }
        }
        notKept = string.Join("; ", missed);
        changed = string.Join("; ", changes);
        done = missed.Count == 0;
    }
}
