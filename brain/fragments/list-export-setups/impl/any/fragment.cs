// NOT STANDALONE. Assumes `doc`, `kind` and `setupName` are in scope, and
// leaves `setups`, `setupCount`, `activeSetups`, `layerTable`, `findings` and
// `refused` behind.
//
// A READ. It opens no transaction and changes nothing - not the active setup,
// not the current print setting.
//
// FOUR KINDS, AND ONLY FOUR. DWG/DXF, DGN, PDF (2022 on) and print settings
// are the setups the Revit API reaches. IFC, Navisworks, DWF and image export
// keep nothing it can read, and a finding says so - a missing kind must not
// read as "this project has none" (docs/work-notes/investigations/
// 2026-11-01-export-setups.md, section 1).
//
// DWG AND DXF ARE ONE SETUP. One ExportDWGSettings element holds both the DWG
// and the DXF options, so "dxf" asks for the same list as "dwg". The options
// shown are the DWG half - what the DWG dialog opens with.
//
// EVERY SETUP IS FOUND THROUGH ITS OWN TYPE'S ListNames AND FindByName, the
// route the export dialogs use, and every read is guarded on its own: one
// option that throws costs that option, never the setup or the list.

string setups = "";
int setupCount = 0;
string activeSetups = "";
string layerTable = "";
var findings = new List<string>();
string refused = "";

string wantedKind = (kind ?? "").Trim().ToLowerInvariant();
if (wantedKind == "all" || wantedKind == "any" || wantedKind == "every") wantedKind = "";
if (wantedKind == "dxf" || wantedKind == "dwg/dxf" || wantedKind == "cad" || wantedKind == "autocad") wantedKind = "dwg";
if (wantedKind == "printing" || wantedKind == "print setting" || wantedKind == "print settings") wantedKind = "print";

string wantedName = (setupName ?? "").Trim();
if (string.Equals(wantedName, "all", StringComparison.OrdinalIgnoreCase)) wantedName = "";

if (wantedKind.Length > 0 && wantedKind != "dwg" && wantedKind != "dgn" && wantedKind != "pdf" && wantedKind != "print")
{
    refused = "'" + kind + "' is not a kind of setup this reads. It reads DWG/DXF, DGN, PDF and print setups. "
        + "IFC, Navisworks, DWF and image export keep no setup the Revit API can read.";
}

Func<bool, string> yesNo = value => value ? "yes" : "no";
Func<string, bool> asked = name => wantedName.Length == 0
    || string.Equals((name ?? "").Trim(), wantedName, StringComparison.OrdinalIgnoreCase);

var rows = new List<string>();
var actives = new List<string>();
// Every name seen, kind first, so a name that is not there can be answered
// with the ones that are.
var seen = new List<string>();
var counts = new List<string>();
int layerRowsShown = 0;
int layerRowsMore = 0;
var layerParts = new List<string>();
// The layer tables of the setups the name matched - read only when exactly one
// DWG/DXF or DGN setup matched, so two tables never run together in one string.
var layerSources = new List<BaseExportOptions>();

// One layer table, as rows - used for DWG and DGN alike, both of which keep
// it on BaseExportOptions.
Action<BaseExportOptions> readLayers = options =>
{
    try
    {
        var table = options.GetExportLayerTable();
        var keys = new List<ExportLayerKey>(table.GetKeys());
        keys.Sort((a, b) =>
        {
            int byCategory = string.Compare(a.CategoryName ?? "", b.CategoryName ?? "", StringComparison.OrdinalIgnoreCase);
            return byCategory != 0 ? byCategory
                : string.Compare(a.SubCategoryName ?? "", b.SubCategoryName ?? "", StringComparison.OrdinalIgnoreCase);
        });
        foreach (var key in keys)
        {
            if (layerRowsShown >= 80) { layerRowsMore++; continue; }
            string where = key.CategoryName ?? "";
            if (!string.IsNullOrEmpty(key.SubCategoryName)) where += " : " + key.SubCategoryName;
            if (key.SpecialType != SpecialType.Default) where += " (" + key.SpecialType + ")";
            string to = "";
            try
            {
                var info = table.GetExportLayerInfo(key);
                to = (info.LayerName ?? "") + " (colour " + info.ColorNumber + ")";
                if (!string.IsNullOrEmpty(info.CutLayerName)) to += ", cut " + info.CutLayerName;
            }
            catch { to = "unreadable"; }
            layerParts.Add(where + " -> " + to);
            layerRowsShown++;
        }
    }
    catch { layerParts.Add("the layer table could not be read"); }
};

Func<BaseExportOptions, string> layerCount = options =>
{
    try { return options.GetExportLayerTable().Count + " layer rows"; }
    catch { return "layer rows unreadable"; }
};

Func<BaseExportOptions, string> hides = options =>
{
    var parts = new List<string>();
    try { parts.Add("hide scope boxes " + yesNo(options.HideScopeBox)); } catch { }
    try { parts.Add("hide reference planes " + yesNo(options.HideReferencePlane)); } catch { }
    try { parts.Add("hide unreferenced view tags " + yesNo(options.HideUnreferenceViewTags)); } catch { }
    try { parts.Add("preserve coincident lines " + yesNo(options.PreserveCoincidentLines)); } catch { }
    return string.Join(", ", parts);
};

// ---- DWG/DXF ---------------------------------------------------------------
if (refused.Length == 0 && (wantedKind == "" || wantedKind == "dwg"))
{
    var names = new List<string>();
    try { names.AddRange(ExportDWGSettings.ListNames(doc)); } catch { findings.Add("Revit would not list the DWG/DXF export setups."); }
    names.Sort(StringComparer.OrdinalIgnoreCase);

    string activeName = "";
    try
    {
        var active = ExportDWGSettings.GetActivePredefinedSettings(doc);
        if (active != null) activeName = active.Name ?? "";
    }
    catch { }
    if (activeName.Length > 0) actives.Add("DWG/DXF: " + activeName);
    counts.Add(names.Count + " DWG/DXF");

    foreach (var name in names)
    {
        seen.Add("DWG/DXF '" + name + "'");
        if (!asked(name)) continue;

        string head = "DWG/DXF: " + name + (string.Equals(name, activeName, StringComparison.Ordinal) ? " (ACTIVE)" : "");
        DWGExportOptions options = null;
        try
        {
            var settings = ExportDWGSettings.FindByName(doc, name);
            if (settings != null) options = settings.GetDWGExportOptions();
        }
        catch { }

        if (options == null) { rows.Add(head + " - its options could not be read"); continue; }

        var parts = new List<string>();
        try
        {
            var version = options.FileVersion;
            parts.Add("AutoCAD " + (version == ACADVersion.R2007 ? "2007" : version == ACADVersion.R2010 ? "2010"
                : version == ACADVersion.R2013 ? "2013" : "2018") + " format");
        }
        catch { }
        try { parts.Add("units " + options.TargetUnit); } catch { }
        try { parts.Add(options.SharedCoords ? "Shared coordinates" : "Project Internal coordinates"); } catch { }
        try { parts.Add(options.MergedViews ? "views on sheets merged into one file" : "views on sheets as external references"); } catch { }
        try
        {
            string standard = (options.LayerMapping ?? "").Trim();
            parts.Add("layer standard " + (standard.Length > 0 ? standard : "(none named)") + ", " + layerCount(options));
        }
        catch { }
        try { parts.Add("solids " + (options.ExportOfSolids == SolidGeometry.ACIS ? "ACIS" : "polymesh")); } catch { }
        try
        {
            var colours = options.Colors;
            parts.Add("colours " + (colours == ExportColorMode.IndexColors ? "AutoCAD index"
                : colours == ExportColorMode.TrueColor ? "true colour (RGB)" : "true colour per view"));
        }
        catch { }
        try
        {
            var scaling = options.LineScaling;
            parts.Add("line scaling " + (scaling == LineScaling.ViewScale ? "view scale"
                : scaling == LineScaling.ModelSpace ? "model space" : "paper space"));
        }
        catch { }
        try { parts.Add("category properties " + options.PropOverrides); } catch { }
        try { parts.Add("text " + (options.TextTreatment == TextTreatment.Exact ? "exact" : "approximate")); } catch { }
        try { parts.Add("rooms and areas as polylines " + yesNo(options.ExportingAreas)); } catch { }
        string hidden = hides(options);
        if (hidden.Length > 0) parts.Add(hidden);

        rows.Add(head + " - " + string.Join(", ", parts));
        if (wantedName.Length > 0) layerSources.Add(options);
    }
}

// ---- DGN -------------------------------------------------------------------
if (refused.Length == 0 && (wantedKind == "" || wantedKind == "dgn"))
{
    var names = new List<string>();
    try { names.AddRange(ExportDGNSettings.ListNames(doc)); } catch { findings.Add("Revit would not list the DGN export setups."); }
    names.Sort(StringComparer.OrdinalIgnoreCase);

    string activeName = "";
    try
    {
        var active = ExportDGNSettings.GetActivePredefinedSettings(doc);
        if (active != null) activeName = active.Name ?? "";
    }
    catch { }
    if (activeName.Length > 0) actives.Add("DGN: " + activeName);
    counts.Add(names.Count + " DGN");

    foreach (var name in names)
    {
        seen.Add("DGN '" + name + "'");
        if (!asked(name)) continue;

        string head = "DGN: " + name + (string.Equals(name, activeName, StringComparison.Ordinal) ? " (ACTIVE)" : "");
        DGNExportOptions options = null;
        try
        {
            var settings = ExportDGNSettings.FindByName(doc, name);
            if (settings != null) options = settings.GetDGNExportOptions();
        }
        catch { }

        if (options == null) { rows.Add(head + " - its options could not be read"); continue; }

        var parts = new List<string>();
        try { parts.Add("DGN " + (options.FileVersion == DGNFileFormat.DGNVersion7 ? "V7" : "V8") + " format"); } catch { }
        try { parts.Add(options.MergedViews ? "views on sheets merged into one file" : "views on sheets as references"); } catch { }
#if REVIT2020
        try { parts.Add("master units " + yesNo(options.MasterUnits)); } catch { }
#else
        try { parts.Add("working units " + yesNo(options.WorkingUnits)); } catch { }
#endif
        try
        {
            string seed = (options.SeedName ?? "").Trim();
            parts.Add("seed file " + (seed.Length > 0 ? seed : "(none named)"));
        }
        catch { }
        try
        {
            string standard = (options.LayerMapping ?? "").Trim();
            parts.Add("level standard " + (standard.Length > 0 ? standard : "(none named)") + ", " + layerCount(options));
        }
        catch { }
        string hidden = hides(options);
        if (hidden.Length > 0) parts.Add(hidden);

        rows.Add(head + " - " + string.Join(", ", parts));
        if (wantedName.Length > 0) layerSources.Add(options);
    }
}

// ---- PDF (Revit 2022 and later) --------------------------------------------
if (refused.Length == 0 && (wantedKind == "" || wantedKind == "pdf"))
{
#if REVIT2020 || REVIT2021
    findings.Add("PDF export setups need Revit 2022 or later - this Revit has no PDF export of its own, so there are none to list.");
#else
    var names = new List<string>();
    try { names.AddRange(ExportPDFSettings.ListNames(doc)); } catch { findings.Add("Revit would not list the PDF export setups."); }
    names.Sort(StringComparer.OrdinalIgnoreCase);

    string activeName = "";
    try
    {
        var active = ExportPDFSettings.GetActivePredefinedSettings(doc);
        if (active != null) activeName = active.Name ?? "";
    }
    catch { }
    if (activeName.Length > 0) actives.Add("PDF: " + activeName);
    counts.Add(names.Count + " PDF");

    // The parameters a naming rule usually holds, matched by id equality -
    // never by reading an id as a number.
    var namingWords = new List<Tuple<ElementId, string>>();
    foreach (var builtIn in new[] { BuiltInParameter.SHEET_NUMBER, BuiltInParameter.SHEET_NAME,
                 BuiltInParameter.VIEW_NAME, BuiltInParameter.PROJECT_NUMBER, BuiltInParameter.PROJECT_NAME,
                 BuiltInParameter.SHEET_CURRENT_REVISION })
    {
        try { namingWords.Add(Tuple.Create(new ElementId(builtIn), LabelUtils.GetLabelFor(builtIn))); } catch { }
    }

    foreach (var name in names)
    {
        seen.Add("PDF '" + name + "'");
        if (!asked(name)) continue;

        string head = "PDF: " + name + (string.Equals(name, activeName, StringComparison.Ordinal) ? " (ACTIVE)" : "");
        PDFExportOptions options = null;
        try
        {
            var settings = ExportPDFSettings.FindByName(doc, name);
            if (settings != null) options = settings.GetOptions();
        }
        catch { }

        if (options == null) { rows.Add(head + " - its options could not be read"); continue; }

        var parts = new List<string>();
        try { parts.Add(options.Combine ? "one combined file" : "one file per sheet or view"); } catch { }
        try { parts.Add("paper " + options.PaperFormat); } catch { }
        try { parts.Add("orientation " + options.PaperOrientation); } catch { }
        try { parts.Add(options.ZoomType == ZoomType.FitToPage ? "fit to page" : "zoom " + options.ZoomPercentage + "%"); } catch { }
        try { parts.Add("colours " + options.ColorDepth); } catch { }
        try { parts.Add("raster quality " + options.RasterQuality); } catch { }
        try { parts.Add("quality " + options.ExportQuality); } catch { }
        try { parts.Add("hide crop boundaries " + yesNo(options.HideCropBoundaries)); } catch { }
        try { parts.Add("hide scope boxes " + yesNo(options.HideScopeBoxes)); } catch { }
        try { parts.Add("hide reference planes " + yesNo(options.HideReferencePlane)); } catch { }
        try { parts.Add("hide unreferenced view tags " + yesNo(options.HideUnreferencedViewTags)); } catch { }
        try
        {
            var rule = options.GetNamingRule();
            var fields = new List<string>();
            foreach (var field in rule)
            {
                string label = "a parameter";
                foreach (var word in namingWords)
                    if (field.ParamId == word.Item1) { label = word.Item2; break; }
                if (label == "a parameter")
                {
                    try
                    {
                        var shared = doc.GetElement(field.ParamId) as ParameterElement;
                        if (shared != null) label = shared.Name;
                    }
                    catch { }
                }
                fields.Add((field.Prefix ?? "") + "[" + label + "]" + (field.Suffix ?? "") + (field.Separator ?? ""));
            }
            parts.Add("file name " + (fields.Count > 0 ? string.Join("", fields) : "(no rule)"));
        }
        catch { }

        rows.Add(head + " - " + string.Join(", ", parts));
    }
#endif
}

// ---- Print settings --------------------------------------------------------
if (refused.Length == 0 && (wantedKind == "" || wantedKind == "print"))
{
    var settings = new List<PrintSetting>();
    try
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(PrintSetting)))
        {
            var setting = element as PrintSetting;
            if (setting != null) settings.Add(setting);
        }
    }
    catch { findings.Add("Revit would not list the print settings."); }
    settings.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase));

    // Read only: the current setting is asked for, never set.
    string currentName = "";
    try
    {
        var current = doc.PrintManager.PrintSetup.CurrentPrintSetting as PrintSetting;
        if (current != null) currentName = current.Name ?? "";
    }
    catch { }
    if (currentName.Length > 0) actives.Add("print: " + currentName);
    counts.Add(settings.Count + " print");

    foreach (var setting in settings)
    {
        string name = setting.Name ?? "";
        seen.Add("print '" + name + "'");
        if (!asked(name)) continue;

        string head = "print: " + name + (string.Equals(name, currentName, StringComparison.Ordinal) ? " (CURRENT)" : "");
        PrintParameters parameters = null;
        try { parameters = setting.PrintParameters; } catch { }
        if (parameters == null) { rows.Add(head + " - its parameters could not be read"); continue; }

        var parts = new List<string>();
        try { parts.Add("paper " + parameters.PaperSize.Name); } catch { parts.Add("paper not readable on this printer"); }
        try { parts.Add("orientation " + parameters.PageOrientation); } catch { }
        try { parts.Add(parameters.ZoomType == ZoomType.FitToPage ? "fit to page" : "zoom " + parameters.Zoom + "%"); } catch { }
        try { parts.Add("colours " + parameters.ColorDepth); } catch { }
        try { parts.Add("hidden lines " + (parameters.HiddenLineViews == HiddenLineViewsType.VectorProcessing ? "vector" : "raster")); } catch { }
        try { parts.Add("raster quality " + parameters.RasterQuality); } catch { }
        rows.Add(head + " - " + string.Join(", ", parts));
    }
}

if (layerSources.Count == 1) readLayers(layerSources[0]);
else if (layerSources.Count > 1)
    findings.Add("'" + wantedName + "' is the name of a DWG/DXF setup and a DGN setup - name the kind to see one layer table.");

if (refused.Length == 0 && wantedName.Length > 0 && rows.Count == 0)
{
    refused = "No " + (wantedKind.Length > 0 ? wantedKind.ToUpperInvariant() + " " : "") + "export setup called '"
        + wantedName + "'. " + (seen.Count > 0
            ? "The setups there are: " + string.Join(", ", seen.Take(30)) + (seen.Count > 30 ? " and " + (seen.Count - 30) + " more." : ".")
            : "This project has none of that kind.");
}

if (refused.Length == 0)
{
    setups = string.Join(" || ", rows);
    setupCount = rows.Count;
    activeSetups = string.Join("; ", actives);
    if (layerParts.Count > 0)
        layerTable = string.Join("; ", layerParts) + (layerRowsMore > 0 ? "; and " + layerRowsMore + " more rows" : "");

    findings.Insert(0, (wantedName.Length > 0 ? "Setup '" + wantedName + "'" : "Export setups") + " in "
        + (doc.Title ?? "this model") + ": " + string.Join(", ", counts) + ".");
    findings.Add("IFC, Navisworks, DWF and image export keep no setup the Revit API can read, so none is listed - "
        + "that is not the same as the project having none.");
}
else findings.Add(refused);
