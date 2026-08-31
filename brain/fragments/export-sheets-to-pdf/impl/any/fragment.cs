// NOT STANDALONE. Assumes `doc`, `elements`, `exportFolder` and
// `combinedFileName` are in scope, and leaves `exported`, `notExported` and
// `folderProblem` behind.
//
// NO TRANSACTION - export is document I/O, not a model edit.
//
// THE PRINTER ROUTE IS NOT HERE, ON PURPOSE.
//
// The other way to reach a PDF is to drive a virtual printer through
// PrintManager and submit a print job. On a real office machine the installed
// printer list holds physical copiers alongside the PDF drivers, and selecting
// the wrong one spends somebody else's paper on a shared device. That is an
// outward effect that cannot be undone and that nobody asked for.
//
// So this fragment uses the export API only, and its declared release range is
// NARROWED to the releases that carry it rather than widened by a fallback that
// can print. Refusing an old release is honest. Quietly printing is not.
//
// The read-back is the filesystem, for the same reason as the DWG export: the
// export call returns a bool, and a bool is a claim about the call rather than
// about the drive.

var exported = new List<string>();
var notExported = new List<ElementId>();
string folderProblem = "";

var folder = (exportFolder ?? "").Trim();
var name = (combinedFileName ?? "").Trim();

var sheets = new List<ViewSheet>();
foreach (var element in elements)
{
    var sheet = element as ViewSheet;
    // A placeholder carries no titleblock and prints nothing. Reported by id
    // rather than dropped, so a short PDF set is explainable.
    if (sheet == null || sheet.IsPlaceholder) notExported.Add(element.Id);
    else sheets.Add(sheet);
}

if (folder.Length == 0) folderProblem = "no export folder was given";
else if (name.Length == 0) folderProblem = "no file name was given";

if (folderProblem.Length == 0)
{
    try
    {
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);
    }
    catch (Exception ex)
    {
        folderProblem = "cannot use folder '" + folder + "': " + ex.Message;
    }
}

if (folderProblem.Length > 0)
{
    foreach (var sheet in sheets) notExported.Add(sheet.Id);
}
else if (sheets.Count > 0)
{
    var options = new PDFExportOptions();
    options.Combine = true;
    options.FileName = name;

    var ids = new List<ElementId>();
    foreach (var sheet in sheets) ids.Add(sheet.Id);

    var expected = System.IO.Path.Combine(folder, name + ".pdf");

    bool threw = false;
    try
    {
        doc.Export(folder, ids, options);
    }
    catch (Exception)
    {
        threw = true;
    }

    // THE READ-BACK. A bool - or the absence of an exception - is a claim about
    // the call. This is the only evidence the set exists.
    if (!threw && System.IO.File.Exists(expected)) exported.Add(expected);
    else foreach (var sheet in sheets) notExported.Add(sheet.Id);
}
